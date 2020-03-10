using Autofac.Core.Activators.Reflection;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace CQELight.IoC.Autofac
{
    /// <summary>
    /// Class to help Autofac finding every constructor, even those who are not public.
    /// </summary>
    public class FullConstructorFinder : IConstructorFinder
    {
        #region Members

        private static readonly ConcurrentDictionary<Type, ConstructorInfo[]> s_DefaultPublicConstructorsCache
            = new ConcurrentDictionary<Type, ConstructorInfo[]>();

        #endregion

        #region IConstructorFinder

        /// <summary>
        /// Get all constructor that can be instantiated.
        /// </summary>
        /// <param name="targetType">Target type.</param>
        /// <returns>Array of available constructors.</returns>
        public ConstructorInfo[] FindConstructors(Type targetType)
            =>
            s_DefaultPublicConstructorsCache.GetOrAdd(targetType, t => t.GetTypeInfo().DeclaredConstructors.Where(c => !c.IsStatic).ToArray());

        #endregion
    }
}
