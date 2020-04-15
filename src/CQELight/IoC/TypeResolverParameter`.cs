namespace CQELight.IoC
{
    /// <summary>
    /// Generic type resolver parameter.
    /// </summary>
    /// <typeparam name="T">Parameter type.</typeparam>
    public class TypeResolverParameter<T> : TypeResolverParameter
    {
        #region Ctor

        /// <summary>
        /// Creates a new TypeResolverParameter with generic parameter type.
        /// </summary>
        /// <param name="value">Value of parameter.</param>
        public TypeResolverParameter(T value)
            : base(typeof(T), value)
        {
        }

        #endregion
    }
}
