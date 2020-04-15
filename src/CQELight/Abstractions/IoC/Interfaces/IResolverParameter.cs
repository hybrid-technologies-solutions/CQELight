namespace CQELight.Abstractions.IoC.Interfaces
{
    /// <summary>
    /// Contract interface for parameters during a resolve operation.
    /// </summary>
    public interface IResolverParameter
    {
        /// <summary>
        /// Gets the value of the parameter.
        /// </summary>
        object? Value { get; }
    }
}
