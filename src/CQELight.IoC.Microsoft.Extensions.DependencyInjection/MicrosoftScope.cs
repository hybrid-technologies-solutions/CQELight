using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Implementations.IoC;
using CQELight.IoC.Exceptions;
using CQELight.Tools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CQELight.IoC.Microsoft.Extensions.DependencyInjection
{
    internal class MicrosoftScope : DisposableObject, IScope
    {
        #region Members

        private readonly IServiceScope scope;
        private readonly IServiceCollection services;

        #endregion

        #region Ctor

        public MicrosoftScope(IServiceScope scope, IServiceCollection services)
        {
            this.scope = scope;
            this.services = services;
        }

        ~MicrosoftScope() => Dispose(false);

        #endregion

        #region IScope methods

        public bool IsDisposed { get; private set; }

        public IScope CreateChildScope(Action<ITypeRegister>? typeRegisterAction = null)
        {
            if (typeRegisterAction == null)
            {
                return new MicrosoftScope(
                                scope,
                                services);
            }
            var childrenCollection = services.Clone();

            var typeRegister = new TypeRegister();
            typeRegisterAction(typeRegister);
            MicrosoftRegistrationHelper.RegisterContextTypes(childrenCollection, typeRegister);

            return new MicrosoftScope(
                childrenCollection.BuildServiceProvider().CreateScope(),
                childrenCollection);
        }

        public T? Resolve<T>(params IResolverParameter[] parameters) where T : class
        {
            if (parameters.Length > 0)
            {
                throw new NotSupportedException("Microsoft.Extensions.DependencyInjection doesn't officially supports parameters injection during runtime. You should register parameters retrieving via a factory or change to another IoC container provider that supports parameters injection at runtime.");
            }
            return scope.ServiceProvider.GetService<T>();
        }

        public object? Resolve(Type type, params IResolverParameter[] parameters)
        {
            if (parameters.Length > 0)
            {
                throw new NotSupportedException("Microsoft.Extensions.DependencyInjection doesn't officially supports parameters injection during runtime. You should register parameters retrieving via a factory or change to another IoC container provider that supports parameters injection at runtime.");
            }
            return scope.ServiceProvider.GetService(type);
        }

        public IEnumerable<T>? ResolveAllInstancesOf<T>() where T : class => scope.ServiceProvider.GetServices<T>();

        public IEnumerable? ResolveAllInstancesOf(Type type) => scope.ServiceProvider.GetServices(type);

        public T ResolveRequired<T>(params IResolverParameter[] parameters) where T : class
            => (T)ResolveRequired(typeof(T), parameters);

        public object ResolveRequired(Type type, params IResolverParameter[] parameters)
        {
            try
            {
                return scope.ServiceProvider.GetRequiredService(type);
            }
            catch (InvalidOperationException e)
            {
                throw new IoCResolutionException($"Unable to retrieve an instance of required service of type {type.FullName}", e);
            }
        }

        #endregion

        #region Overriden methods

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            try
            {
                scope.Dispose();
            }
            catch
            {
                //No throw on disposal
            }
            base.Dispose(disposing);
        }

        #endregion

    }
}
