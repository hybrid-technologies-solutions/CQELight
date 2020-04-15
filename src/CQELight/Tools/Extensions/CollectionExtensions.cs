using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CQELight.Tools.Extensions
{
    /// <summary>
    /// Bunch of extensions methods for collections.
    /// </summary>
    public static class CollectionExtensions
    {
        #region Public static methods

        /// <summary>
        /// Do an action on each member of a enumerable collection.
        /// </summary>
        /// <typeparam name="T">Type of enumerable collection objectS.</typeparam>
        /// <param name="collection">Instance of the collection.</param>
        /// <param name="allowParallel">Flag that indicates if actions can be parallelized.</param>
        /// <param name="action">Aaction to perform</param>
        public static void DoForEach<T>(this IEnumerable<T> collection, Action<T> action, bool allowParallel)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (action == null)
                return;
            if (allowParallel)
            {
                var tasks = new List<Task>();

                foreach (var item in collection)
                {
                    tasks.Add(Task.Run(() => action(item)));
                }

                Task.WaitAll(tasks.ToArray());
            }
            else
            {
                foreach (var item in collection)
                {
                    action(item);
                }
            }
        }
        /// <summary>
        /// Do an action on each member of a enumerable collection.
        /// </summary>
        /// <typeparam name="T">Type of enumerable collection objectS.</typeparam>
        /// <param name="collection">Instance of the collection.</param>
        /// <param name="action">Aaction to perform</param>
        public static void DoForEach<T>(this IEnumerable<T> collection, Action<T> action)
            => DoForEach(collection, action, false);

        /// <summary>
        /// Do an asynchronous action on each member of a enumerable collection.
        /// </summary>
        /// <typeparam name="T">Type of enumerable collection objectS.</typeparam>
        /// <param name="collection">Instance of the collection.</param>
        /// <param name="action">Aaction to perform</param>
        public static Task DoForEachAsync<T>(this IEnumerable<T> collection, Func<T, Task> action)
            => DoForEachAsync(collection, action, false);

        /// <summary>
        /// Do an asynchronous action on each member of a enumerable collection.
        /// </summary>
        /// <typeparam name="T">Type of enumerable collection objectS.</typeparam>
        /// <param name="collection">Instance of the collection.</param>
        /// <param name="allowParallel">Flag to indicates if action can be parallelized or not.</param>
        /// <param name="action">Aaction to perform</param>
        public static async Task DoForEachAsync<T>(this IEnumerable<T> collection, Func<T, Task> action, bool allowParallel)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }
            if (action == null)
            {
                return;
            }
            var tasks = new List<Task>();
            foreach (var item in collection)
            {
                if (allowParallel)
                {
                    tasks.Add(action(item));
                }
                else
                {
                    await action(item).ConfigureAwait(false);
                }
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a collection filtered where any element is not equals to default value (such as null for objects).
        /// </summary>
        /// <param name="collection">Collection to filter.</param>
        /// <returns>Filtered collection.</returns>
        public static IEnumerable<T>? WhereNotNull<T>(this IEnumerable<T>? collection)
        {
            if (collection != null)
            {
                return collection.Where(c => !EqualityComparer<T>.Default.Equals(c, default));
            }
            return collection;
        }

        #endregion

    }
}
