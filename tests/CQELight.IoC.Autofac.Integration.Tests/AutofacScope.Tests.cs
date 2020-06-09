using Autofac;
using Autofac.Core;
using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.IoC.Attributes;
using CQELight.IoC.Exceptions;
using CQELight.TestFramework;
using FluentAssertions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CQELight.IoC.Autofac.Integration.Tests
{
    public class AutofacScopeTests : BaseUnitTestClass
    {
        #region Ctor & members

        private string[] excludedDLLs = new[] { "xunit", "nCrunch", "FluentAssertions" };

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

        private interface IParameterResolving { string Data { get; } }
        private class ParameterResolving : IParameterResolving
        {
            public ParameterResolving(string data)
            {
                Data = data;
            }

            public string Data { get; }
        }

        private interface Multiple { }
        private class MultipleOne : Multiple { }
        private class MultipleTwo : Multiple { }

        public AutofacScopeTests()
        {

        }

        private void Bootstrapp(ContainerBuilder builder)
        {
            new Bootstrapper().UseAutofacAsIoC(builder, excludedDLLs).Bootstrapp();
        }

        #endregion

        #region CreateChildScope

        [Fact]
        public void AutofacScope_CreateChildScope_CustomScopeRegistration_TypeRegistration_AsExpected()
        {
            Bootstrapp(new ContainerBuilder());

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
        public void AutofacScope_CreateChildScope_CustomScopeRegistration_InstanceRegistration_AsExpected()
        {
            Bootstrapp(new ContainerBuilder());

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

        [Fact]
        public void AutofacScope_Resolve_TypeParameter()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ParameterResolving>().AsImplementedInterfaces();
            Bootstrapp(builder);

            using (var s = DIManager.BeginScope())
            {
                var i = s.Resolve<IParameterResolving>(new TypeResolverParameter(typeof(string), "test"));
                i.Data.Should().Be("test");
            }
        }

        [Fact]
        public void AutofacScope_Resolve_NameParameter()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ParameterResolving>().AsImplementedInterfaces();
            Bootstrapp(builder);

            using (var s = DIManager.BeginScope())
            {
                var i = s.Resolve<IParameterResolving>(new NameResolverParameter("data", "name_test"));
                i.Data.Should().Be("name_test");
            }
        }

        [Fact]
        public void AutofacScope_ResolveAllInstancesOf_Generic()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<MultipleOne>().AsImplementedInterfaces();
            builder.RegisterType<MultipleTwo>().AsImplementedInterfaces();
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
        public void AutofacScope_ResolveAllInstancesOf_NonGeneric()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<MultipleOne>().AsImplementedInterfaces();
            builder.RegisterType<MultipleTwo>().AsImplementedInterfaces();
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
        public void AutofacScope_AutoRegisterType_AsExpected()
        {
            Bootstrapp(new ContainerBuilder());

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
        public void AutofacScope_AutoRegisterTypeSingleInstance_AsExpected()
        {
            Bootstrapp(new ContainerBuilder());

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
        public void AutofacSope_AutoRegisterType_Should_Find_InternalCtor()
        {
            Bootstrapp(new ContainerBuilder());

            using (var s = DIManager.BeginScope())
            {
                var result = s.Resolve<InternalCtor>();
                result.Should().NotBeNull();
                var result2 = s.Resolve<InternalCtorSingle>();
                result2.Should().NotBeNull();
            }
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
            new Bootstrapper().UseAutofacAsIoC(new ContainerBuilder()).AddIoCRegistration(registration).Bootstrapp();

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
            new Bootstrapper().UseAutofacAsIoC(new ContainerBuilder()).AddIoCRegistration(registration).Bootstrapp();

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
            new Bootstrapper().UseAutofacAsIoC(new ContainerBuilder()).AddIoCRegistration(registration).Bootstrapp();

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
            new Bootstrapper().UseAutofacAsIoC(new ContainerBuilder()).AddIoCRegistration(registration).Bootstrapp();

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
            new Bootstrapper().UseAutofacAsIoC(new ContainerBuilder()).AddIoCRegistration(registration).Bootstrapp();

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
            new Bootstrapper().UseAutofacAsIoC(new ContainerBuilder()).AddIoCRegistration(registration).Bootstrapp();

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
            new Bootstrapper().UseAutofacAsIoC(new ContainerBuilder()).AddIoCRegistration(registration).Bootstrapp();

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
            var registration = new TypeRegistration<ClassA>(true, RegistrationLifetime.Singleton);
            new Bootstrapper().UseAutofacAsIoC(new ContainerBuilder()).AddIoCRegistration(registration).Bootstrapp();

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

        public class PrivateCtorClass
        {
            private PrivateCtorClass()
            {

            }
        }

        [Fact]
        public void Registration_With_Mode_Should_Be_Considered_OnlyPublic()
        {
            var registration = new TypeRegistration<PrivateCtorClass>(true, RegistrationLifetime.Transient, TypeResolutionMode.OnlyUsePublicCtors);
            new Bootstrapper().UseAutofacAsIoC().AddIoCRegistration(registration).Bootstrapp();

            using (var scope = DIManager.BeginScope())
            {
                Assert.Throws<DependencyResolutionException>(() => scope.Resolve<PrivateCtorClass>());
            }
        }

        [Fact]
        public void Registration_With_Mode_Should_Be_Considered_Full()
        {
            var registration = new TypeRegistration<PrivateCtorClass>(true, RegistrationLifetime.Transient, TypeResolutionMode.Full);
            new Bootstrapper().UseAutofacAsIoC().AddIoCRegistration(registration).Bootstrapp();

            using (var scope = DIManager.BeginScope())
            {
                scope.Resolve<PrivateCtorClass>().Should().NotBeNull();
            }
        }

        [DefineTypeResolutionMode(TypeResolutionMode.OnlyUsePublicCtors)]
        public class PrivateCtorClassAutoRegister : IAutoRegisterType
        {
            private PrivateCtorClassAutoRegister()
            {

            }
        }

        [Fact]
        public void Registration_With_Mode_AsAttribute_Should_Be_Considered_OnlyPublicCtors()
        {
            new Bootstrapper().UseAutofacAsIoC().Bootstrapp();

            using (var scope = DIManager.BeginScope())
            {
                Assert.Throws<DependencyResolutionException>(() => scope.Resolve<PrivateCtorClassAutoRegister>());
            }
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
                .UseAutofacAsIoC()
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

        #region ParameterResolver

        private class ParameterTestClass
        {
            public ParameterTestClass(string value, DateTime date, int data)
            {
                Value = value;
                Date = date;
                Data = data;
            }

            public string Value { get; private set; }
            public DateTime Date { get; private set; }
            public int Data { get; private set; }
        }

        [Fact]
        public void ParameterResolver_Should_Inject_Data_AsRequired()
        {
            var registration = new TypeRegistration<ParameterTestClass>(typeof(ParameterTestClass));
            new Bootstrapper().UseAutofacAsIoC(new ContainerBuilder()).AddIoCRegistration(registration).Bootstrapp();

            using (var scope = DIManager.BeginScope())
            {
                var c = scope.Resolve<ParameterTestClass>(
                    new TypeResolverParameter<string>("my string value"),
                    new TypeResolverParameter(typeof(int), 42),
                    new NameResolverParameter("date", DateTime.Today)
                    );
                c.Should().NotBeNull();
                c.Value.Should().Be("my string value");
                c.Data.Should().Be(42);
                c.Date.Should().BeSameDateAs(DateTime.Today);
            }
        }

        #endregion

        #region AutoRegisterHandler

        private class EventAutoReg : BaseDomainEvent { }
        private class CommandAutoReg : ICommand { }
        private class EventHandlerAutoReg : IDomainEventHandler<EventAutoReg>
        {
            public Task<Result> HandleAsync(EventAutoReg domainEvent, IEventContext context = null)
            {
                throw new NotImplementedException();
            }
        }
        private class CommandHandlerAutoReg : ICommandHandler<CommandAutoReg>
        {
            public Task<Result> HandleAsync(CommandAutoReg command, ICommandContext context = null)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void Event_And_Command_Handlers_Should_Be_AutoRegister()
        {
            Bootstrapp(new ContainerBuilder());

            using (var s = DIManager.BeginScope())
            {
                var result = s.Resolve<IDomainEventHandler<EventAutoReg>>();
                result.Should().NotBeNull();
                var result2 = s.Resolve<ICommandHandler<CommandAutoReg>>();
                result2.Should().NotBeNull();
            }
        }

        #endregion

        #region ResolveRequired

        [Fact]
        public void ResolveRequired_Should_Throws_IfTypeIsNotRegistered()
        {
            var scope = new AutofacScope(new ContainerBuilder().Build());
            Assert.Throws<IoCResolutionException>(() => scope.ResolveRequired<ParameterTestClass>());
            Assert.Throws<IoCResolutionException>(() => scope.ResolveRequired(typeof(ParameterTestClass)));
        }

        #endregion

        #region IScope Resolvable

        [Fact]
        public void IScope_Should_Be_Resolvable()
        {
            new Bootstrapper().UseAutofacAsIoC().Bootstrapp();

            using (var scope = DIManager.BeginScope())
            {
                var innerScope = scope.Resolve<IScope>();
                innerScope.Should().NotBeNull();
                innerScope.Should().BeOfType<AutofacScope>();
            }
        }

        #endregion

    }
}
