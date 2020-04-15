namespace CQELight.DAL.Interfaces
{
    /// <summary>
    /// Base common interface for all kind of persitable entities.
    /// </summary>
    public interface IPersistableEntity
    {
        /// <summary>
        /// Check if key is defined or not.
        /// Although this can be done by reflection, the choice
        /// to force to implement it is purely for performance.
        /// </summary>
        /// <returns>True if key is defined, false otherwise.</returns>
        bool IsKeySet();

        /// <summary>
        /// Get key value.
        /// Although this can be done by reflection, the choice
        /// to force to implement it is purely for performance.
        /// </summary>
        /// <returns>Value of the key boxed in a object.</returns>
        object? GetKeyValue();
    }
}
