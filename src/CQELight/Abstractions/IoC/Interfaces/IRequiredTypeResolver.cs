using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.Abstractions.IoC.Interfaces
{
    /// <summary>
    /// Contract interface for resolving required type (meaning it has to be registered)
    /// </summary>
    public interface IRequiredTypeResolver
    {
        /// <summary>
        /// Retrieve a specific type instance for IoC container.
        /// </summary>
        /// <typeparam name="T">Type of object to retrieve</typeparam>
        /// <param name="parameters">Parameters to help with resolution.</param>
        /// <returns>Founded instances</returns>
        T ResolveRequired<T>(params IResolverParameter[] parameters) where T : class;
        /// <summary>
        /// Retrieve an instance of an object type for IoC container.
        /// </summary>
        /// <param name="type">Type of object to retrieve.</param>
        /// <param name="parameters">Parameters to help with resolution.</param>
        /// <returns>Founded instances</returns>
        object ResolveRequired(Type type, params IResolverParameter[] parameters);
    }
}
