#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright � 2013 Karsten Nikolai Strand
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
using System.Linq;
using System.Reflection;

using Pomona.Common.TypeSystem;
using Pomona.Internals;
using Pomona.Queries;
using Pomona.RequestProcessing;

namespace Pomona
{
    public class DataSourceRootNode : PathNode
    {
        private static MethodInfo queryMethod =
            ReflectionHelper.GetMethodDefinition<IPomonaDataSource>(x => x.Query<object>());

        private readonly IPomonaDataSource dataSource;


        public DataSourceRootNode(ITypeMapper typeMapper, IPomonaDataSource dataSource)
            : base(typeMapper, null, "/")
        {
            if (dataSource == null)
                throw new ArgumentNullException("dataSource");
            this.dataSource = dataSource;
        }


        protected override IMappedType OnGetType()
        {
            return null;
        }


        public override object Value
        {
            get { return this.dataSource; }
        }


        public override PathNode GetChildNode(string name)
        {
            var type = ((TypeMapper)TypeMapper).TransformedTypes.OfType<ResourceType>().FirstOrDefault(
                x =>
                    x.IsUriBaseType && x.IsRootResource
                    && string.Equals(x.UriRelativePath, name, StringComparison.InvariantCultureIgnoreCase));

            if (type == null)
                throw new ResourceNotFoundException("Unable to locate root resource.");

            var queryable = queryMethod.MakeGenericMethod(type.MappedTypeInstance).Invoke(this.dataSource, null);
            return CreateNode(TypeMapper, this, name, queryable, type);
        }


        public override IQueryExecutor GetQueryExecutor()
        {
            return this.dataSource as IQueryExecutor ?? base.GetQueryExecutor();
        }


        protected override IQueryableResolver GetQueryableResolver()
        {
            return new DataSourceQueryableResolver(this.dataSource);
        }


        protected override IPomonaRequestProcessor OnGetRequestProcessor(PomonaRequest request)
        {
            return new DataSourceRequestProcessor(this.dataSource);
        }
    }
}