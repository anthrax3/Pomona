﻿#region License

// Pomona is open source software released under the terms of the LICENSE specified in the
// project's repository, or alternatively at http://pomona.io/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;

using Critters.Client;

using Mono.Cecil;

using NUnit.Framework;

using Pomona.Common;
using Pomona.Common.Web;

namespace Pomona.SystemTests.ClientCompatibility
{
    /// <summary>
    /// These tests checks client compatibility with old generated clients.
    /// 
    /// This was introduced during transition to the async HttpClient.
    /// </summary>
    [TestFixture]
    public class ClientCompatibilityTests
    {
        [Test]
        public void All_references_from_old_assembly_are_valid()
        {
            var exceptions = new List<AssertionException>();
            try
            {
                var clientAssembly = AssemblyDefinition.ReadAssembly(typeof(CritterClient).Assembly.CodeBaseAbsolutePath());
                var pomonaCommonModule = AssemblyDefinition.ReadAssembly(typeof(IPomonaClient).Assembly.CodeBaseAbsolutePath()).MainModule;

                var pomonaCommonAssemblyName = "Pomona.Common";
                foreach (
                    var typeReference in clientAssembly.MainModule.GetTypeReferences().Where(x => x.Scope.Name == pomonaCommonAssemblyName))
                {
                    var resolved = pomonaCommonModule.MetadataResolver.Resolve(typeReference);
                    Assert.That(resolved, Is.Not.Null,
                                $"Required type {typeReference} needed for backwards compatibility is missing from {pomonaCommonAssemblyName}.");
                }

                foreach (
                    var memberReference in
                        clientAssembly.MainModule.GetMemberReferences().Where(x => x.DeclaringType.Scope.Name == pomonaCommonAssemblyName))
                {
                    var methodReference = memberReference as MethodReference;
                    var fieldReference = memberReference as FieldReference;
                    if (methodReference != null)
                    {
                        var resolved = pomonaCommonModule.MetadataResolver.Resolve(methodReference);
                        Assert.That(resolved, Is.Not.Null,
                                    $"Required method {methodReference} needed for backwards compatibility is missing from {pomonaCommonAssemblyName}.");
                    }
                    else if (fieldReference != null)
                    {
                        var resolved = pomonaCommonModule.MetadataResolver.Resolve(fieldReference);
                        Assert.That(resolved, Is.Not.Null,
                                    $"Required method {fieldReference} needed for backwards compatibility is missing from {pomonaCommonAssemblyName}.");
                    }
                    else
                    {
                        Assert.Fail(
                            $"Don't know how to check member reference {memberReference} of type {memberReference.GetType().FullName}");
                    }
                }
            }
            catch (AssertionException ex)
            {
                exceptions.Add(ex);
            }

            if (exceptions.Count > 0)
                throw new AggregateException("One or more assertions failed", exceptions);
        }


        [Test]
        public void Ctor_taking_uri_does_not_throw_exception()
        {
            Assert.DoesNotThrow(() => new CritterClient("http://test"));
        }


        [Test]
        public void Ctor_taking_webclient_does_not_throw_exception()
        {
            Assert.DoesNotThrow(() => new CritterClient("http://test", new HttpWebClient()));
        }
    }
}