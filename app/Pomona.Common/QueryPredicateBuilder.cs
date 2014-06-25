﻿#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright © 2014 Karsten Nikolai Strand
// 
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// ----------------------------------------------------------------------------

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using Pomona.Common.Internals;
using Pomona.Common.Linq;

namespace Pomona.Common
{
    public class QueryPredicateBuilder : ExpressionVisitor
    {
        protected static readonly ReadOnlyDictionary<ExpressionType, string> binaryExpressionNodeDict;
        private static readonly Type[] enumUnderlyingTypes = { typeof(byte), typeof(int), typeof(long) };

        private static readonly HashSet<Type> nativeTypes = new HashSet<Type>(TypeUtils.GetNativeTypes());

        private static readonly HashSet<char> validSymbolCharacters =
            new HashSet<char>("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_");

        private readonly ParameterExpression thisParameter;
        private LambdaExpression rootLambda;


        static QueryPredicateBuilder()
        {
            var binExprDict = new Dictionary<ExpressionType, string>
            {
                { ExpressionType.AndAlso, "and" },
                { ExpressionType.OrElse, "or" },
                { ExpressionType.Equal, "eq" },
                { ExpressionType.NotEqual, "ne" },
                { ExpressionType.GreaterThan, "gt" },
                { ExpressionType.GreaterThanOrEqual, "ge" },
                { ExpressionType.LessThan, "lt" },
                { ExpressionType.LessThanOrEqual, "le" },
                { ExpressionType.Subtract, "sub" },
                { ExpressionType.Add, "add" },
                { ExpressionType.Multiply, "mul" },
                { ExpressionType.Divide, "div" },
                { ExpressionType.Modulo, "mod" }
            };

            binaryExpressionNodeDict = new ReadOnlyDictionary<ExpressionType, string>(binExprDict);
        }


        public QueryPredicateBuilder(ParameterExpression thisParameter = null)
        {
            this.thisParameter = thisParameter;
        }


        protected LambdaExpression RootLambda
        {
            get { return this.rootLambda; }
        }

        protected ParameterExpression ThisParameter
        {
            get
            {
                return this.thisParameter
                       ?? (this.rootLambda != null ? this.rootLambda.Parameters.FirstOrDefault() : null);
            }
        }


        public static string Create(LambdaExpression lambda)
        {
            return new QueryPredicateBuilder().Visit(lambda).ToString();
        }


        public static string Create<T>(Expression<Func<T, bool>> lambda)
        {
            return Create((LambdaExpression)lambda);
        }


        public static string Create<T, TResult>(Expression<Func<T, TResult>> lambda)
        {
            return Create((LambdaExpression)lambda);
        }


        public override Expression Visit(Expression node)
        {
            if (node == null)
                return null;
            var visitedNode = base.Visit(node) as PomonaExtendedExpression;
            if (visitedNode == null)
                return NotSupported(node, node.NodeType + " not supported server side.");

            return visitedNode;
        }


        protected virtual Expression VisitRootLambda<T>(Expression<T> node)
        {
            var visitedBody = Visit(node.Body);
            while (visitedBody is QuerySegmentParenScopeExpression)
                visitedBody = ((QuerySegmentParenScopeExpression)visitedBody).Value;
            return visitedBody;
        }


        protected override Expression VisitBinary(BinaryExpression node)
        {
            string opString;
            if (!binaryExpressionNodeDict.TryGetValue(node.NodeType, out opString))
                return NotSupported(node, "BinaryExpression NodeType " + node.NodeType + " not yet handled.");

            // Detect comparison with enum

            var left = node.Left;
            var right = node.Right;

            left = FixBinaryComparisonConversion(left, right);
            right = FixBinaryComparisonConversion(right, left);

            TryDetectAndConvertEnumComparison(ref left, ref right, true);
            TryDetectAndConvertNullableEnumComparison(ref left, ref right, true);

            return Scope(Nodes(Visit(left), " ", opString, " ", Visit(right)));
        }


        protected override Expression VisitConditional(ConditionalExpression node)
        {
            return Format("iif({0},{1},{2})",
                Visit(node.Test),
                Visit(node.IfTrue),
                Visit(node.IfFalse));
        }


        protected override Expression VisitConstant(ConstantExpression node)
        {
            var valueType = node.Type;
            var value = node.Value;
            return Terminal(GetEncodedConstant(valueType, value));
        }


        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (node.Parameters.Count != 1)
                return NotSupported(node, "Only supports one parameter in lambda expression for now.");

            if (this.rootLambda == null)
            {
                try
                {
                    this.rootLambda = (Expression<T>)(new PreBuildVisitor().Visit(node));
                    return VisitRootLambda((Expression<T>)this.rootLambda);
                }
                finally
                {
                    this.rootLambda = null;
                }
            }
            else
            {
                var param = node.Parameters[0];
                var predicateBuilder = new QueryPredicateBuilder(ThisParameter);
                return Format("{0}:{1}", param.Name, predicateBuilder.Visit(node));
            }
        }


        protected override Expression VisitListInit(ListInitExpression node)
        {
            return NotSupported(node, "ListInitExpression not supported by Linq provider.");
        }


        protected override Expression VisitMember(MemberExpression node)
        {
            Expression odataExpression;
            if (TryMapKnownOdataFunction(
                node.Member,
                Enumerable.Repeat(node.Expression, 1),
                out odataExpression))
                return odataExpression;

            if (node.Expression != ThisParameter)
                return Format("{0}.{1}", Visit(node.Expression), GetMemberName(node.Member));
            return Terminal(GetMemberName(node.Member));
        }


        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            return NotSupported(node, node.NodeType + " not supported server side.");
        }


        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.UniqueToken() == OdataFunctionMapping.EnumerableContainsMethod.UniqueToken())
                return Format("{0} in {1}", Visit(node.Arguments[1]), Visit(node.Arguments[0]));

            if (node.Method.UniqueToken() == OdataFunctionMapping.ListContainsMethod.UniqueToken())
                return Format("{0} in {1}", Visit(node.Arguments[0]), Visit(node.Object));

            if (node.Method.UniqueToken() == OdataFunctionMapping.DictStringStringGetMethod.UniqueToken())
            {
                var quotedKey = Visit(node.Arguments[0]);
                //var key = DecodeQuotedString(quotedKey);
                /* 
                if (ContainsOnlyValidSymbolCharacters(key))
                    return string.Format("{0}.{1}", Build(callExpr.Object), key);*/
                return Format("{0}[{1}]", Visit(node.Object), quotedKey);
            }
            if (node.Method.UniqueToken() == OdataFunctionMapping.SafeGetMethod.UniqueToken())
            {
                var constantKeyExpr = node.Arguments[1] as ConstantExpression;
                if (constantKeyExpr != null && constantKeyExpr.Type == typeof(string) &&
                    IsValidSymbolString((string)constantKeyExpr.Value))
                    return Format("{0}.{1}", Visit(node.Arguments[0]), constantKeyExpr.Value);
            }

            Expression odataExpression;

            // Include this (object) parameter as first argument if not null!
            var args = node.Object != null
                ? Enumerable.Repeat(node.Object, 1).Concat(node.Arguments)
                : node.Arguments;

            if (
                !TryMapKnownOdataFunction(node.Method, args, out odataExpression))
            {
                return NotSupported(node,
                    "Don't know what to do with method " + node.Method.Name + " declared in "
                    + node.Method.DeclaringType.FullName);
            }

            return odataExpression;
        }


        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            var elements = node.Expressions.Select(Visit).Cast<object>().ToArray();
            var format = "["
                         + string.Join(",", Enumerable.Range(0, elements.Length).Select(x => string.Concat("{", x, "}")))
                         + "]";
            return Format(format, elements);
        }


        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == ThisParameter)
                return Terminal("this");
            return Terminal(node.Name);
        }


        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.TypeIs:
                    var typeOperand = node.TypeOperand;
                    //if (!typeOperand.IsInterface || !typeof (IClientResource).IsAssignableFrom(typeOperand))
                    //{
                    //    throw new InvalidOperationException(
                    //        typeOperand.FullName
                    //        + " is not an interface and/or does not implement type IClientResource.");
                    //}
                    var jsonTypeName = GetExternalTypeName(typeOperand);
                    if (node.Expression == ThisParameter)
                        return Format("isof({0})", jsonTypeName);
                    else
                    {
                        return Format("isof({0},{1})",
                            Visit(node.Expression),
                            Visit(Expression.Constant(typeOperand)));
                    }

                    // TODO: Proper typename resolving

                default:
                    throw new NotImplementedException(
                        "Don't know how to handle TypeBinaryExpression with NodeType " + node.NodeType);
            }
        }


        protected override Expression VisitUnary(UnaryExpression unaryExpression)
        {
            switch (unaryExpression.NodeType)
            {
                case ExpressionType.Not:
                    return Format("not ({0})", Visit(unaryExpression.Operand));

                case ExpressionType.TypeAs:
                    return Scope(Format("{0} as {1}",
                        Visit(unaryExpression.Operand),
                        GetExternalTypeName(unaryExpression.Type)));

                case ExpressionType.Convert:
                    if (unaryExpression.Operand.Type.IsEnum)
                        return Visit(unaryExpression.Operand);

                    if (unaryExpression.Operand == ThisParameter)
                    {
                        return Format("cast({0})", GetExternalTypeName(unaryExpression.Type));
                        // throw new NotImplementedException("Only know how to cast `this` to something else");
                    }
                    else
                    {
                        return Format("cast({0},{1})",
                            Visit(unaryExpression.Operand),
                            GetExternalTypeName(unaryExpression.Type));
                    }

                default:
                    return NotSupported(unaryExpression,
                        "NodeType " + unaryExpression.NodeType + " in UnaryExpression not yet handled.");
            }
        }


        internal static QuerySegmentExpression Format(string format, params object[] args)
        {
            return new QueryFormattedSegmentExpression(format, args);
        }


        internal static QuerySegmentListExpression Nodes(params object[] args)
        {
            return new QuerySegmentListExpression(args);
        }


        internal static QuerySegmentListExpression Nodes(IEnumerable<object> args)
        {
            return new QuerySegmentListExpression(args);
        }


        internal static QuerySegmentExpression Scope(QuerySegmentExpression value)
        {
            return new QuerySegmentParenScopeExpression(value);
        }


        internal static QuerySegmentExpression Terminal(string value)
        {
            return new QueryTerminalSegmentExpression(value);
        }


        private static string DateTimeToString(DateTime dt)
        {
            var roundedDt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind);
            if (roundedDt == dt)
            {
                if (dt.Kind == DateTimeKind.Utc)
                    return dt.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
                return dt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
            }
            return dt.ToString("O");
        }


        private static string DecodeQuotedString(string quotedString)
        {
            if (quotedString.Length < 2 || quotedString[0] != '\'' || quotedString[quotedString.Length - 1] != '\'')
                throw new ArgumentException("Quoted string needs to be enclosed with the character '");

            // TODO: decode url encoded string
            return quotedString.Substring(1, quotedString.Length - 2);
        }


        private static string DoubleToString(double value)
        {
            // We must always include . to make sure number gets interpreted as double and not int.
            // Yeah, there's probably a more elegant way to do this, but don't care about finding it out right now.
            // This should work.
            return value != (long)value
                ? value.ToString("R", CultureInfo.InvariantCulture)
                : string.Format(CultureInfo.InvariantCulture, "{0}.0", (long)value);
        }


        private static Type GetFuncInExpression(Type t)
        {
            Type[] typeArgs;
            if (t.TryExtractTypeArguments(typeof(IQueryable<>), out typeArgs))
                return typeof(IEnumerable<>).MakeGenericType(typeArgs[0]);
            return t.TryExtractTypeArguments(typeof(Expression<>), out typeArgs) ? typeArgs[0] : t;
        }


        private static string GetMemberName(MemberInfo member)
        {
            // Do it JSON style camelCase
            return member.Name.Substring(0, 1).ToLower() + member.Name.Substring(1);
        }


        private static object GetMemberValue(object obj, MemberInfo member)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            if (member == null)
                throw new ArgumentNullException("member");
            var propertyInfo = member as PropertyInfo;
            if (propertyInfo != null)
                return propertyInfo.GetValue(obj, null);

            var fieldInfo = member as FieldInfo;
            if (fieldInfo != null)
                return fieldInfo.GetValue(obj);

            throw new InvalidOperationException("Don't know how to get value from member of type " + member.GetType());
        }


        private static bool IsValidSymbolString(string text)
        {
            var containsOnlyValidSymbolCharacters = text.All(x => validSymbolCharacters.Contains(x));
            return text.Length > 0 && (!char.IsNumber(text[0])) && containsOnlyValidSymbolCharacters;
        }


        private static NotSupportedByProviderExpression NotSupported(Expression node, string message)
        {
            return new NotSupportedByProviderExpression(node, new NotSupportedException(message));
        }


        private static void ReplaceQueryableMethodWithCorrespondingEnumerableMethod(ref MemberInfo member,
            ref IEnumerable<Expression>
                arguments)
        {
            var firstArg = arguments.First();
            Type[] queryableTypeArgs;
            var method = member as MethodInfo;
            if (method != null && method.IsStatic &&
                firstArg.Type.TryExtractTypeArguments(typeof(IQueryable<>), out queryableTypeArgs))
            {
                // Try to find matching method taking IEnumerable instead
                var wantedArgs =
                    method.GetGenericMethodDefinition()
                        .GetParameters()
                        .Select(x => GetFuncInExpression(x.ParameterType))
                        .ToArray();

                var enumerableMethod =
                    typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .Where(x => x.Name == method.Name)
                        .Select(x => new { parameters = x.GetParameters(), mi = x })
                        .Where(x => x.parameters.Length == wantedArgs.Length &&
                                    x.parameters
                                        .Select(y => y.ParameterType)
                                        .Zip(wantedArgs, (y, z) => y.IsGenericallyEquivalentTo(z))
                                        .All(y => y))
                        .Select(x => x.mi)
                        .FirstOrDefault();

                if (enumerableMethod != null)
                {
                    arguments =
                        arguments.Select(x => x.NodeType == ExpressionType.Quote ? ((UnaryExpression)x).Operand : x);
                    if (enumerableMethod.IsGenericMethodDefinition)
                        member = enumerableMethod.MakeGenericMethod(((MethodInfo)member).GetGenericArguments());
                    else
                        member = enumerableMethod;
                }
            }
        }


        private string EncodeString(string text, string prefix = "")
        {
            // TODO: IMPORTANT! Proper encoding!!
            var sb = new StringBuilder();
            sb.Append(prefix);
            sb.Append('\'');

            foreach (var c in text)
            {
                if (c == '\'')
                    sb.Append("''");
                else
                    sb.Append(c);
            }

            sb.Append('\'');
            return sb.ToString();
        }


        private Expression FixBinaryComparisonConversion(Expression expr, Expression other)
        {
            var otherIsNull = other.NodeType == ExpressionType.Constant && ((ConstantExpression)other).Value == null;
            if (expr.NodeType == ExpressionType.TypeAs && ((UnaryExpression)expr).Operand.Type == typeof(object)
                && !otherIsNull)
                return ((UnaryExpression)expr).Operand;
            else if (expr.NodeType == ExpressionType.Convert && expr.Type.IsNullable() &&
                     Nullable.GetUnderlyingType(expr.Type) == ((UnaryExpression)expr).Operand.Type)
                return ((UnaryExpression)expr).Operand;
            return expr;
        }


        private string GetEncodedConstant(Type valueType, object value)
        {
            if (value == null)
                return "null";
            Type enumerableElementType;
            if (valueType != typeof(string) && valueType.TryGetEnumerableElementType(out enumerableElementType))
            {
                // This handles arrays in constant expressions
                var elements = string
                    .Join(
                        ",",
                        ((IEnumerable)value)
                            .Cast<object>()
                            .Select(x => GetEncodedConstant(enumerableElementType, x)));

                return string.Format("[{0}]", elements);
            }

            if (valueType.IsEnum)
                return EncodeString(value.ToString());
            switch (Type.GetTypeCode(valueType))
            {
                case TypeCode.Char:
                    // Note: char will be interpreted as string on other end.
                    return EncodeString(value.ToString());
                case TypeCode.String:
                    return EncodeString((string)value);
                case TypeCode.Int32:
                    return value.ToString();
                case TypeCode.DateTime:
                    return string.Format("datetime'{0}'", DateTimeToString((DateTime)value));
                case TypeCode.Double:
                    return DoubleToString((double)value);
                case TypeCode.Single:
                    return ((float)value).ToString("R", CultureInfo.InvariantCulture) + "f";
                case TypeCode.Decimal:
                    return ((decimal)value).ToString(CultureInfo.InvariantCulture) + "m";
                case TypeCode.Object:
                    if (value is Guid)
                        return string.Format("guid'{0}'", ((Guid)value));
                    if (value is Type)
                        return GetExternalTypeName((Type)value);
                    break;
                case TypeCode.Boolean:
                    return ((bool)value) ? "true" : "false";
                default:
                    break;
            }
            throw new NotImplementedException(
                "Don't know how to send constant of type " + valueType.FullName + " yet..");
        }


        private string GetExternalTypeName(Type typeOperand)
        {
            var postfixSymbol = string.Empty;
            if (typeOperand.UniqueToken() == typeof(Nullable<>).UniqueToken())
            {
                typeOperand = Nullable.GetUnderlyingType(typeOperand);
                postfixSymbol = "?";
            }

            string typeName;

            if (nativeTypes.Contains(typeOperand))
                typeName = string.Format("{0}{1}", typeOperand.Name, postfixSymbol);
            else
            {
                var resourceInfoAttribute =
                    typeOperand.GetCustomAttributes(typeof(ResourceInfoAttribute), false).
                        OfType<ResourceInfoAttribute>().First();
                typeName = resourceInfoAttribute.JsonTypeName;
            }
            return EncodeString(typeName, "t");
        }


        private void TryDetectAndConvertEnumComparison(ref Expression left, ref Expression right, bool tryAgainSwapped)
        {
            var unaryLeft = left as UnaryExpression;
            var underlyingType = left.Type;
            if (enumUnderlyingTypes.Contains(underlyingType) && unaryLeft != null
                && left.NodeType == ExpressionType.Convert &&
                unaryLeft.Operand.Type.IsEnum)
            {
                if (right.Type == underlyingType && right.NodeType == ExpressionType.Constant)
                {
                    var rightConstant = (ConstantExpression)right;
                    left = unaryLeft.Operand;
                    right = Expression.Constant(Enum.ToObject(unaryLeft.Operand.Type, rightConstant.Value),
                        unaryLeft.Operand.Type);
                    return;
                }
            }

            if (tryAgainSwapped)
                TryDetectAndConvertEnumComparison(ref right, ref left, false);
        }


        private void TryDetectAndConvertNullableEnumComparison(ref Expression left,
            ref Expression right,
            bool tryAgainSwapped)
        {
            if (left.Type != right.Type || !left.Type.IsNullable())
                return;
            var leftConvert = left.NodeType == ExpressionType.Convert ? (UnaryExpression)left : null;
            var rightConvert = left.NodeType == ExpressionType.Convert ? (UnaryExpression)right : null;
            var rightConstant = rightConvert != null ? rightConvert.Operand as ConstantExpression : null;
            var enumType = rightConstant != null && rightConstant.Type.IsEnum ? rightConstant.Type : null;

            if (leftConvert != null && rightConvert != null && rightConstant != null && enumType != null)
            {
                left = leftConvert.Operand;
                right = rightConstant;
                return;
            }

            if (tryAgainSwapped)
                TryDetectAndConvertNullableEnumComparison(ref right, ref left, false);
        }


        private bool TryMapKnownOdataFunction(
            MemberInfo member,
            IEnumerable<Expression> arguments,
            out Expression odataExpression)
        {
            ReplaceQueryableMethodWithCorrespondingEnumerableMethod(ref member, ref arguments);

            OdataFunctionMapping.MemberMapping memberMapping;
            if (!OdataFunctionMapping.TryGetMemberMapping(member, out memberMapping))
            {
                odataExpression = null;
                return false;
            }

            var odataArguments = arguments.Select(Visit).Cast<object>().ToArray();
            var callFormat = memberMapping.PreferredCallStyle == OdataFunctionMapping.MethodCallStyle.Chained
                ? memberMapping.ChainedCallFormat
                : memberMapping.StaticCallFormat;

            odataExpression = Format(callFormat, odataArguments);
            return true;
        }

        #region Nested type: PreBuildVisitor

        private class PreBuildVisitor : EvaluateClosureMemberVisitor
        {
            private static readonly MethodInfo concatMethod;


            static PreBuildVisitor()
            {
                concatMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });
            }


            protected override Expression VisitBinary(BinaryExpression node)
            {
                // Constant folding
                var left = Visit(node.Left);
                var right = Visit(node.Right);
                if (left.NodeType == ExpressionType.Constant && right.NodeType == ExpressionType.Constant
                    && left.Type == right.Type && IsFoldedType(left.Type)
                    && (node.Method == null || node.Method.DeclaringType == left.Type))
                {
                    return
                        Expression.Constant(
                            Expression.Lambda(node, Enumerable.Empty<ParameterExpression>()).Compile().DynamicInvoke(
                                null),
                            node.Type);
                }

                if (node.NodeType == ExpressionType.Add && left.Type == typeof(string)
                    && right.Type == typeof(string))
                    return Expression.Call(concatMethod, left, right);
                return base.VisitBinary(node);
            }


            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                var baseNode = base.VisitMethodCall(node);
                if (baseNode is MethodCallExpression)
                {
                    var mNode = baseNode as MethodCallExpression;
                    if (mNode.Arguments.Where(x => x != null).Any(x => x.NodeType != ExpressionType.Constant))
                        return baseNode;

                    object instance = null;

                    if (mNode.Object != null)
                    {
                        var objectConstExpr = mNode.Object as ConstantExpression;
                        if (objectConstExpr == null)
                            return baseNode;

                        instance = objectConstExpr.Value;
                    }

                    var invokeArgs = mNode.Arguments.Cast<ConstantExpression>().Select(x => x.Value);
                    return Expression.Constant(mNode.Method.Invoke(instance, invokeArgs.ToArray()), mNode.Type);
                }

                return baseNode;
            }


            private bool IsFoldedType(Type type)
            {
                return type == typeof(int) || type == typeof(decimal);
            }
        }

        #endregion
    }
}