using CQELight.Abstractions.DDD;
using System;

namespace CQELight.TestFramework.Extensions
{
    /// <summary>
    /// Collection of extensions methods for testing upon Aggregates
    /// </summary>
    public static class AggregateExtensions
    {
        #region Public static methods

        /// <summary>
        /// Clear all domain events from an aggregate root that could have been generated.
        /// </summary>
        /// <typeparam name="T">Type of key of the aggregate.</typeparam>
        /// <param name="aggregate">Instance of aggregate.</param>
        public static void ClearDomainEvents<T>(this AggregateRoot<T> aggregate)
        {
            if (aggregate == null)
            {
                throw new ArgumentNullException(nameof(aggregate));
            }
            aggregate.ClearDomainEvents();
        }

        #endregion

    }
}
