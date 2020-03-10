using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.Events.Interfaces;
using System;

namespace CQELight.Abstractions.IoC.Interfaces
{
    /// <summary>
    /// Definition of a scope to resolve thing during a determined lifetime.
    /// </summary>
    public interface IScope : IDisposable, ITypeResolver, IRequiredTypeResolver, IEventContext, ICommandContext
    {
        /// <summary>
        /// Indicates if scope is disposed or not.
        /// </summary>
        bool IsDisposed { get; }
        /// <summary>
        /// Create a whole new scope with all current's scope registration.
        /// </summary>
        /// <param name="typeRegisterAction">Specific child registration..</param>
        /// <returns>Child scope.</returns>
        IScope CreateChildScope(Action<ITypeRegister> typeRegisterAction = null);
    }
}
