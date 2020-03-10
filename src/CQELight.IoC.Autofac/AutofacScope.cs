using Autofac;
using Autofac.Core;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Implementations.IoC;
using CQELight.IoC.Exceptions;
using CQELight.Tools;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace CQELight.IoC.Autofac
{
    internal class AutofacScope : DisposableObject, IScope
    {
        #region Members

        private static MethodInfo? s_GetAllInstancesMethod;
        private readonly IComponentContext componentContext;
        private readonly ILogger<AutofacScope>? logger;

        #endregion

        #region Properties

        private static MethodInfo GetAllInstancesMethod 
            => s_GetAllInstancesMethod ??= Array.Find(typeof(AutofacScope).GetMethods(), m => m.Name == nameof(ResolveAllInstancesOf) && m.IsGenericMethod);

        /// <summary>
        /// Current Id of the scope
        /// </summary>
        public Guid Id { get; protected set; }

        /// <summary>
        /// Indicates if scope is disposed or not.
        /// </summary>
        public bool IsDisposed { get; }

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

        internal AutofacScope(ILifetimeScope scope, ILogger<AutofacScope> logger)
            : this(scope)
        {
            this.logger = logger;
        }

        internal AutofacScope(IComponentContext context, ILogger<AutofacScope> logger)
            : this(context)
        {
            this.logger = logger;
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
        public IScope CreateChildScope(Action<ITypeRegister>? typeRegisterAction = null)
        {
            if (componentContext is ILifetimeScope scope)
            {
                Action<ContainerBuilder>? act = null;
                if (typeRegisterAction != null)
                {
                    logger?.LogDebug("Adding some custom registration in child scope");
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
            logger?.LogError("Autofac cannot create a child scope from IComponentContext. Parent scope should be created with the ctor that takes an ILifeTimeScope parameter");
            throw new InvalidOperationException("Autofac cannot create a child scope from IComponentContext. Parent scope should be created with the ctor that takes an ILifeTimeScope parameter");
        }

        #endregion

        #region ITypeResolver

        /// <summary>
        /// Resolve instance of type.
        /// </summary>
        /// <typeparam name="T">Instance of type we want to resolve.</typeparam>
        /// <param name="parameters">Parameters for resolving.</param>
        /// <returns>Instance of T if any, null otherwise</returns>
        public T Resolve<T>(params IResolverParameter[] parameters) where T : class
            => componentContext.ResolveOptional<T>(GetParams(parameters));

        /// <summary>
        /// Resolve instance of type.
        /// </summary>
        /// <param name="type">Type we want to resolve instance.</param>
        /// <param name="parameters">Parameters for resolving.</param>
        /// <returns>Instance of resolved type.</returns>
        public object? Resolve(Type type, params IResolverParameter[] parameters)
            => componentContext.ResolveOptional(type, GetParams(parameters));

        /// <summary>
        /// Retrieve all instances of a specific type from IoC container.
        /// </summary>
        /// <typeparam name="T">Excepted types.</typeparam>
        /// <returns>Collection of implementations for type.</returns>
        public IEnumerable<T>? ResolveAllInstancesOf<T>() where T : class
            => componentContext.ResolveOptional<IEnumerable<T>>();

        /// <summary>
        /// Retrieve all instances of a specific type from IoC container.
        /// </summary>
        /// <param name="type">Typeof of elements we want.</param>
        /// <returns>Collection of implementations for type.</returns>
        public IEnumerable? ResolveAllInstancesOf(Type type)
            => GetAllInstancesMethod.MakeGenericMethod(type).Invoke(this, null) as IEnumerable;

        /// <inheritdoc />
        public T ResolveRequired<T>(params IResolverParameter[] parameters) where T : class
            => (T) ResolveRequired(typeof(T), parameters);

        /// <inheritdoc />
        public object ResolveRequired(Type type, params IResolverParameter[] parameters)
        {
            try
            {
                return componentContext.Resolve(type, GetParams(parameters));
            }
            catch (DependencyResolutionException dpExc)
            {
                logger?.LogError(dpExc, $"Unable to resolve {type.AssemblyQualifiedName}");
                throw new IoCResolutionException($"Unable to resolve {type.AssemblyQualifiedName}", dpExc);
            }
        }

        #endregion

        #region Private methods

        private IEnumerable<Parameter> GetParams(IEnumerable<IResolverParameter> parameters)
        {
            var @params = new List<Parameter>();
            foreach (var par in parameters)
            {
                switch (par)
                {
                    case NameResolverParameter namePar:
                        @params.Add(new NamedParameter(namePar.Name, namePar.Value));
                        break;
                    case TypeResolverParameter typePar:
                        @params.Add(new TypedParameter(typePar.Type, typePar.Value));
                        break;
                    default:
                        logger?.LogWarning($"Unable to find parameter type for resolution for instance {par.GetType().FullName}");
                        break;
                }
            }
            return @params;
        }

        #endregion
    }
}
