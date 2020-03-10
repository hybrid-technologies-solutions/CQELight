using CQELight.TestFramework;
using System;
using System.Collections.Generic;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using System.Linq;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Bootstrapping.Notifications;
using CQELight.IoC.Exceptions;

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
        private IEnumerable<BootstrapperNotification> Bootstrapp(IServiceCollection services)
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

        #region TypeRegistration

        private interface InterfaceA { }
        private interface InterfaceB { }
        private class ClassA : InterfaceA, InterfaceB { }

        [Fact]
        public void TypeRegistration_Should_OnlyMatch_RegisteredTypes()
        {
            var registration = new TypeRegistration(typeof(ClassA), typeof(InterfaceA));
            new Bootstrapper().UseMicrosoftDependencyInjection(new ServiceCollection()).AddIoCRegistration(registration).Bootstrapp();

            using (var scope = DIManager.BeginScope())
            {
                scope.Resolve<ClassA>().Should().NotBeNull();
                scope.Resolve<InterfaceA>().Should().NotBeNull();
                scope.Resolve<InterfaceB>().Should().BeNull();
            }

        }

        [Fact]
        public void TypeRegistration_RegisterForEverything_Should_Match_AllPossibleCombination()
        {
            var registration = new TypeRegistration(typeof(ClassA), true);
            new Bootstrapper().UseMicrosoftDependencyInjection(new ServiceCollection()).AddIoCRegistration(registration).Bootstrapp();

            using (var scope = DIManager.BeginScope())
            {
                scope.Resolve<ClassA>().Should().NotBeNull();
                scope.Resolve<InterfaceA>().Should().NotBeNull();
                scope.Resolve<InterfaceB>().Should().NotBeNull();
            }
        }

        [Fact]
        public void GenericTypeRegistration_Should_Work_Like_StandardOne()
        {
            var registration = new TypeRegistration<ClassA>(typeof(InterfaceA));
            new Bootstrapper().UseMicrosoftDependencyInjection(new ServiceCollection()).AddIoCRegistration(registration).Bootstrapp();

            using (var scope = DIManager.BeginScope())
            {
                scope.Resolve<ClassA>().Should().NotBeNull();
                scope.Resolve<InterfaceA>().Should().NotBeNull();
                scope.Resolve<InterfaceB>().Should().BeNull();
            }
        }

        [Fact]
        public void GenericTypeRegistration_RegisterForEverything_Should_Work_Like_StandardOne()
        {
            var registration = new TypeRegistration<ClassA>(true);
            new Bootstrapper().UseMicrosoftDependencyInjection(new ServiceCollection()).AddIoCRegistration(registration).Bootstrapp();

            using (var scope = DIManager.BeginScope())
            {
                scope.Resolve<ClassA>().Should().NotBeNull();
                scope.Resolve<InterfaceA>().Should().NotBeNull();
                scope.Resolve<InterfaceB>().Should().NotBeNull();
            }
        }

        public interface IGenericInterface<T> { }
        public class GenericClass<T> : IGenericInterface<T> { }

        [Fact]
        public void GenericTypeRegistration_Handle_Generic()
        {
            var registration = new TypeRegistration(typeof(GenericClass<>), typeof(IGenericInterface<>));
            new Bootstrapper().UseMicrosoftDependencyInjection(new ServiceCollection()).AddIoCRegistration(registration).Bootstrapp();

            using (var scope = DIManager.BeginScope())
            {
                var instance = scope.Resolve<IGenericInterface<int>>();
                instance.Should().NotBeNull();
                instance.Should().BeOfType<GenericClass<int>>();
            }
        }

        [Fact]
        public void Registration_Should_Respect_SpecifiedLifeTime_Transient()
        {
            var registration = new TypeRegistration<ClassA>(true, RegistrationLifetime.Transient);
            new Bootstrapper().UseMicrosoftDependencyInjection(new ServiceCollection()).AddIoCRegistration(registration).Bootstrapp();

            using (var scope = DIManager.BeginScope())
            {
                var classA1 = scope.Resolve<ClassA>();
                var classA2 = scope.Resolve<ClassA>();
                ReferenceEquals(classA1, classA2).Should().BeFalse();
            }
        }

        [Fact]
        public void Registration_Should_Respect_SpecifiedLifeTime_Scoped()
        {
            var registration = new TypeRegistration<ClassA>(true, RegistrationLifetime.Scoped);
            new Bootstrapper().UseMicrosoftDependencyInjection(new ServiceCollection()).AddIoCRegistration(registration).Bootstrapp();

            using (var scope = DIManager.BeginScope())
            {
                var classA1 = scope.Resolve<ClassA>();
                var classA2 = scope.Resolve<ClassA>();
                ReferenceEquals(classA1, classA2).Should().BeTrue();
            }
            ClassA outsideClass = null;

            using (var scope = DIManager.BeginScope())
            {
                outsideClass = scope.Resolve<ClassA>();
            }

            using (var scope = DIManager.BeginScope())
            {
                var classA1 = scope.Resolve<ClassA>();
                ReferenceEquals(classA1, outsideClass).Should().BeFalse();
            }
        }

        [Fact]
        public void Registration_Should_Respect_SpecifiedLifeTime_Singleton()
        {
            var registration = new TypeRegistration<ClassA>(RegistrationLifetime.Singleton);
            var collection = new ServiceCollection();
            new Bootstrapper().AddIoCRegistration(registration).UseMicrosoftDependencyInjection(collection).Bootstrapp();

            ClassA classA1 = null;
            ClassA classA2 = null;
            using (var scope = DIManager.BeginScope())
            {
                classA1 = scope.Resolve<ClassA>();
            }
            using (var scope = DIManager.BeginScope())
            {
                classA2 = scope.Resolve<ClassA>();
            }
            ReferenceEquals(classA1, classA2).Should().BeTrue();
        }

        private interface IInnerInterface { }
        private class InnerClass : IInnerInterface { }

        private class ComplexClass
        {
            public readonly IInnerInterface inner;
            public readonly string data;

            public ComplexClass(
                IInnerInterface inner,
                string data)
            {
                this.inner = inner;
                this.data = data;
            }
        }

        [Fact]
        public void ScopedFactoryRegistration_Should_ResolveFromScope()
        {
            new Bootstrapper()
                .UseMicrosoftDependencyInjection(new ServiceCollection())
                .AddIoCRegistration(new TypeRegistration<InnerClass>(true))
                .AddIoCRegistration(new FactoryRegistration(scope => new ComplexClass(scope.Resolve<IInnerInterface>(), "data"), typeof(ComplexClass)))
                .Bootstrapp();

            using (var scope = DIManager.BeginScope())
            {
                var complexClass = scope.Resolve<ComplexClass>();
                complexClass.Should().NotBeNull();
                complexClass.inner.Should().NotBeNull();
                complexClass.data.Should().Be("data");
            }
        }
        #endregion

        #region ResolveRequired

        [Fact]
        public void ResolveRequired_Should_Throws_IfTypeIsNotRegistered()
        {
            var services = new ServiceCollection();
            var scope = new MicrosoftScope(services.BuildServiceProvider().CreateScope(), services);
            Assert.Throws<IoCResolutionException>(() => scope.ResolveRequired<ComplexClass>());
            Assert.Throws<IoCResolutionException>(() => scope.ResolveRequired(typeof(ComplexClass)));
        }

        #endregion
    }
}
