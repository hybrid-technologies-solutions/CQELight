using CQELight.Abstractions.IoC.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQELight.TestFramework.IoC
{
    /// <summary>
    /// A fully in memory test scope factory
    /// </summary>
    public class TestScopeFactory : IScopeFactory
    {
        #region Members

        private readonly IScope? _providedScope;

        #endregion

        #region Properties

        /// <summary>
        /// Collection of instances to manage.
        /// </summary>
        public Dictionary<Type, object> Instances { get; }
            = new Dictionary<Type, object>();

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new <see cref="TestScopeFactory"/> instance.
        /// </summary>
        public TestScopeFactory()
        {
        }

        /// <summary>
        /// Creates a new <see cref="TestScopeFactory"/> instance with a definition of <see cref="IScope"/>.
        /// </summary>
        /// <param name="providedScope">Scope to use when factory is called.</param>
        public TestScopeFactory(IScope providedScope)
        {
            _providedScope = providedScope;
        }

        #endregion

        #region IScopeFactory methods

        /// <summary>
        /// Create a new scope.
        /// </summary>
        /// <returns>New instance of scope.</returns>
        public IScope CreateScope()
            => _providedScope ?? new TestScope(Instances.ToDictionary(k => k.Key, v => v.Value));

        #endregion

    }
}
