using Autofac;
using Autofac.Core;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Implementations.IoC;
using CQELight.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CQELight.IoC.Autofac
{
    internal class AutofacScope : DisposableObject, IScope
    {
        #region Members

        private static MethodInfo s_GetAllInstancesMethod;
        private readonly IComponentContext componentContext;

        #endregion

        #region Properties

        private static MethodInfo GetAllInstancesMethod
        {
            get
            {
                if (s_GetAllInstancesMethod == null)
                {
                    s_GetAllInstancesMethod = Array.Find(typeof(AutofacScope).GetMethods(), m => m.Name == nameof(AutofacScope.ResolveAllInstancesOf) && m.IsGenericMethod);
                }
                return s_GetAllInstancesMethod;
            }
        }

        /// <summary>
        /// Current Id of the scope
        /// </summary>
        public Guid Id { get; protected set; }

        /// <summary>
        /// Indicates if scope is disposed or not.
        /// </summary>
        public bool IsDisposed { get; private set; }

        #endregion

        #region Ctor

        internal AutofacScope(ILifetimeScope scope)
        {
            this.componentContext = scope;
            Id = Guid.NewGuid();
        }

        internal AutofacScope(IComponentContext context)
        {
            this.componentContext = context;
            Id = Guid.NewGuid();
        }

        ~AutofacScope()
        {
            Dispose(false);
        }

        #endregion

        #region IScope methods

        /// <summary>
        /// Create a whole new scope with all current's scope registration.
        /// </summary>
        /// <param name="typeRegisterAction">Specific child registration..</param>
        /// <returns>Child scope.</returns>
        public IScope CreateChildScope(Action<ITypeRegister> typeRegisterAction = null)
        {
            if (componentContext is ILifetimeScope scope)
            {
                Action<ContainerBuilder> act = null;
                if (typeRegisterAction != null)
                {
                    var typeRegister = new TypeRegister();
                    typeRegisterAction.Invoke(typeRegister);
                    act += b => AutofacTools.RegisterContextTypes(b, typeRegister);
                }
                if (act != null)
                {
                    return new AutofacScope(scope.BeginLifetimeScope(act));
                }
                return new AutofacScope(scope.BeginLifetimeScope());
            }
            else
            {
                throw new InvalidOperationException("Autofac cannot create a child scope from IComponentContext. Parent scope should be created with the ctor that takes an ILifeTimeScope parameter");
            }
        }

        #endregion

        #region ITypeResolver

        /// <summary>
        /// Resolve instance of type.
        /// </summary>
        /// <typeparam name="T">Instance of type we want to resolve.</typeparam>
        /// <param name="parameters">Parameters for resolving.</param>
        /// <returns></returns>
        public T Resolve<T>(params IResolverParameter[] parameters) where T : class
            => componentContext.ResolveOptional<T>(GetParams(parameters));

        /// <summary>
        /// Resolve instance of type.
        /// </summary>
        /// <param name="type">Type we want to resolve instance.</param>
        /// <param name="parameters">Parameters for resolving.</param>
        /// <returns>Instance of resolved type.</returns>
        public object Resolve(Type type, params IResolverParameter[] parameters)
            => componentContext.ResolveOptional(type, GetParams(parameters));

        /// <summary>
        /// Retrieve all instances of a specific type from IoC container.
        /// </summary>
        /// <typeparam name="T">Excepted types.</typeparam>
        /// <returns>Collection of implementations for type.</returns>
        public IEnumerable<T> ResolveAllInstancesOf<T>() where T : class
            => componentContext.ResolveOptional<IEnumerable<T>>();

        /// <summary>
        /// Retrieve all instances of a specific type from IoC container.
        /// </summary>
        /// <param name="t">Typeo of elements we want.</param>
        /// <returns>Collection of implementations for type.</returns>
        public IEnumerable ResolveAllInstancesOf(Type t)
            => GetAllInstancesMethod.MakeGenericMethod(t).Invoke(this, null) as IEnumerable;

        #endregion

        #region Private methods

        private IEnumerable<Parameter> GetParams(IResolverParameter[] parameters)
        {
            var @params = new List<Parameter>();
            foreach (var par in parameters)
            {
                if (par is NameResolverParameter namePar)
                {
                    @params.Add(new NamedParameter(namePar.Name, namePar.Value));
                }
                else if (par is TypeResolverParameter typePar)
                {
                    @params.Add(new TypedParameter(typePar.Type, typePar.Value));
                }
            }
            return @params;
        }

        #endregion
    }
}
