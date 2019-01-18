﻿using CQELight.TestFramework;
using MS = Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using System.Linq;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Bootstrapping.Notifications;

namespace CQELight.IoC.Microsoft.Extensions.DependencyInjection.Tests
{
    public class MicrosoftScopeTests : BaseUnitTestClass
    {

        #region Ctor & members
        private interface IScopeTest { string Data { get; } }
        private class ScopeTest : IScopeTest
        {
            public string Data { get; }

            public ScopeTest()
            {
                Data = "ctor";
            }
            public ScopeTest(string data)
            {
                Data = data;
            }
        }
        public MicrosoftScopeTests()
        {
            bootstrapper = new Bootstrapper();
        }

        private Bootstrapper bootstrapper;
        private IEnumerable<BootstrapperNotification> Bootstrapp(MS.IServiceCollection services)
            => bootstrapper.UseMicrosoftDependencyInjection(services).Bootstrapp();

        #endregion

        #region CreateChildScope

        [Fact]
        public void CreateChildScope_CustomScopeRegistration_TypeRegistration_AsExpected()
        {
            Bootstrapp(new ServiceCollection());

            using (var s = DIManager.BeginScope())
            {
                var i = s.Resolve<IScopeTest>();
                i.Should().BeNull();
                using (var sChild = s.CreateChildScope())
                {
                    i = sChild.Resolve<IScopeTest>();
                    i.Should().BeNull();
                }
                using (var sChild = s.CreateChildScope(e => e.RegisterType<ScopeTest>()))
                {
                    i = sChild.Resolve<IScopeTest>();
                    i.Should().NotBeNull();
                    i.Data.Should().Be("ctor");
                }
            }
        }

        [Fact]
        public void CreateChildScope_CustomScopeRegistration_InstanceRegistration_AsExpected()
        {
            Bootstrapp(new ServiceCollection());

            using (var s = DIManager.BeginScope())
            {
                var i = s.Resolve<IScopeTest>();
                i.Should().BeNull();
                using (var sChild = s.CreateChildScope())
                {
                    i = sChild.Resolve<IScopeTest>();
                    i.Should().BeNull();
                }
                using (var sChild = s.CreateChildScope(e => e.Register(new ScopeTest("instance"))))
                {
                    i = sChild.Resolve<IScopeTest>();
                    i.Should().NotBeNull();
                    i.Data.Should().Be("instance");
                }
            }
        }

        #endregion

        #region Parameters

        private interface IParameterResolving { string Data { get; } }
        private class ParameterResolving : IParameterResolving
        {
            public ParameterResolving(string data)
            {
                Data = data;
            }

            public string Data { get; }
        }

        [Fact]
        public void Resolve_TypeParameter_Should_Throw_NotSupported()
        {
            var builder = new ServiceCollection();
            bootstrapper.AddIoCRegistration(new TypeRegistration(typeof(ParameterResolving), typeof(ParameterResolving)));
            Bootstrapp(builder);

            using (var s = DIManager.BeginScope())
            {
                Assert.Throws<NotSupportedException>(() => s.Resolve<IParameterResolving>(new TypeResolverParameter(typeof(string), "test")));
            }
        }

        [Fact]
        public void Resolve_NameParameter_Should_Throw_NotSupported()
        {
            var builder = new ServiceCollection();
            bootstrapper.AddIoCRegistration(new TypeRegistration(typeof(ParameterResolving), typeof(ParameterResolving)));
            Bootstrapp(builder);

            using (var s = DIManager.BeginScope())
            {
                Assert.Throws<NotSupportedException>(() => s.Resolve<IParameterResolving>(new NameResolverParameter("data", "name_test")));
            }
        }

        private interface Multiple { }
        private class MultipleOne : Multiple { }
        private class MultipleTwo : Multiple { }

        [Fact]
        public void ResolveAllInstancesOf_Generic()
        {
            var builder = new ServiceCollection();
            bootstrapper.AddIoCRegistration(new TypeRegistration(typeof(MultipleOne), typeof(Multiple)));
            bootstrapper.AddIoCRegistration(new TypeRegistration(typeof(MultipleTwo), typeof(Multiple)));
            Bootstrapp(builder);

            using (var s = DIManager.BeginScope())
            {
                var data = s.ResolveAllInstancesOf<Multiple>();
                data.Should().HaveCount(2);
                data.Any(t => t.GetType() == typeof(MultipleOne)).Should().BeTrue();
                data.Any(t => t.GetType() == typeof(MultipleTwo)).Should().BeTrue();
            }
        }

        [Fact]
        public void ResolveAllInstancesOf_NonGeneric()
        {
            var builder = new ServiceCollection();
            bootstrapper.AddIoCRegistration(new TypeRegistration(typeof(MultipleOne), typeof(Multiple)));
            bootstrapper.AddIoCRegistration(new TypeRegistration(typeof(MultipleTwo), typeof(Multiple)));
            Bootstrapp(builder);

            using (var s = DIManager.BeginScope())
            {
                var data = s.ResolveAllInstancesOf(typeof(Multiple)).Cast<Multiple>();
                data.Should().HaveCount(2);
                data.Any(t => t.GetType() == typeof(MultipleOne)).Should().BeTrue();
                data.Any(t => t.GetType() == typeof(MultipleTwo)).Should().BeTrue();
            }
        }

        #endregion

        #region AutoRegisterType

        private interface IAutoTest { }
        private interface IAutoTestSingle { }
        private class AutoTest : IAutoTest, IAutoRegisterType { }
        private class AutoTestSingle : IAutoTestSingle, IAutoRegisterTypeSingleInstance { }
        private class InternalCtor : IAutoRegisterType { internal InternalCtor() { } }
        private class InternalCtorSingle : IAutoRegisterTypeSingleInstance { internal InternalCtorSingle() { } }


        [Fact]
        public void AutoRegisterType_AsExpected()
        {
            Bootstrapp(new ServiceCollection());

            using (var s = DIManager.BeginScope())
            {
                var result = s.Resolve<IAutoTest>();
                var result2 = s.Resolve<IAutoTest>();
                result.Should().NotBeNull();
                result2.Should().NotBeNull();
                ReferenceEquals(result, result2).Should().BeFalse();
            }
        }


        [Fact]
        public void AutoRegisterTypeSingleInstance_AsExpected()
        {
            Bootstrapp(new ServiceCollection());

            using (var s = DIManager.BeginScope())
            {
                var result = s.Resolve<IAutoTestSingle>();
                var result2 = s.Resolve<IAutoTestSingle>();
                result.Should().NotBeNull();
                result2.Should().NotBeNull();
                ReferenceEquals(result, result2).Should().BeTrue();
            }
        }

        [Fact]
        public void AutoRegisterType_Should_NotFind_InternalCtor_And_ReturnsError()
        {
            var notifs = Bootstrapp(new ServiceCollection());

            notifs.Any().Should().BeTrue();
            notifs.Any(n => n.Type == BootstrapperNotificationType.Error).Should().BeTrue();
        }

        #endregion

    }
}
