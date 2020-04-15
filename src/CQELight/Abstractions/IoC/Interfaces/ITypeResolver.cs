using System;
using System.Collections;
using System.Collections.Generic;

namespace CQELight.Abstractions.IoC.Interfaces
{
    /// <summary>
    /// Contract interface to resolve objects for their type.
    /// </summary>
    public interface ITypeResolver
    {
        /// <summary>
        /// Retrieve a specific type instance for IoC container.
        /// </summary>
        /// <typeparam name="T">Type of object to retrieve</typeparam>
        /// <param name="parameters">Parameters to help with resolution.</param>
        /// <returns>Founded instances</returns>
        T? Resolve<T>(params IResolverParameter[] parameters) where T : class;
        /// <summary>
        /// Retrieve an instance of an object type for IoC container.
        /// </summary>
        /// <param name="type">Type of object to retrieve.</param>
        /// <param name="parameters">Parameters to help with resolution.</param>
        /// <returns>Founded instances</returns>
        object? Resolve(Type type, params IResolverParameter[] parameters);
        /// <summary>
        /// Retrieve all instances of a specific type from IoC container.
        /// </summary>
        /// <typeparam name="T">Excepted types.</typeparam>
        /// <returns>Collection of implementations for type.</returns>
        IEnumerable<T> ResolveAllInstancesOf<T>() where T : class;
        /// <summary>
        /// Retrieve all instances of a specific type from IoC container.
        /// </summary>
        /// <param name="t">Typeo of elements we want.</param>
        /// <returns>Collection of implementations for type.</returns>
        IEnumerable ResolveAllInstancesOf(Type t);
    }
}
