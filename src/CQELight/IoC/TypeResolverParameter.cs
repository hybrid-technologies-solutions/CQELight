using CQELight.Abstractions.IoC.Interfaces;
using System;

namespace CQELight.IoC
{
    /// <summary>
    /// Parameter for resolving by type.
    /// </summary>
    public class TypeResolverParameter : IResolverParameter
    {
        #region Properties

        /// <summary>
        /// Type of parameter.
        /// </summary>
        public Type Type { get; }
        /// <summary>
        /// Value of parameter.
        /// </summary>
        public object? Value { get; }

        #endregion

        #region Ctor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="type">Type of parameter.</param>
        /// <param name="value">Value of parameter.</param>
        public TypeResolverParameter(Type type, object? value)
        {
            Value = value;
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        #endregion

    }
}
