using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CQELight.TestFramework.IoC
{
    /// <summary>
    /// A fully in memory test scope.
    /// </summary>
    public class TestScope : IScope
    {
        #region Members

        private bool _disposed = false;
        private readonly ReadOnlyDictionary<Type, object> _instances;
        private readonly TypeEqualityComparer _typeComparer;

        #endregion

        #region Ctor

        public TestScope(Dictionary<Type, object> instances)
        {
            _instances = new ReadOnlyDictionary<Type, object>(instances);
            _typeComparer = new TypeEqualityComparer();
        }

        #endregion

        #region IScope

        public bool IsDisposed => _disposed;

        public IScope CreateChildScope(Action<ITypeRegister> typeRegisterAction = null)
            => this;

        public void Dispose()
            => _disposed = true;

        public T Resolve<T>(params IResolverParameter[] parameters) where T : class
            => _instances.FirstOrDefault(t => _typeComparer.Equals(t.Key, typeof(T))).Value as T;

        public object Resolve(Type type, params IResolverParameter[] parameters)
            => _instances.FirstOrDefault(t => _typeComparer.Equals(t.Key, type)).Value;

        public IEnumerable<T> ResolveAllInstancesOf<T>() where T : class
            => _instances.Where(t => _typeComparer.Equals(t.Key, typeof(T))).Select(v => v.Value as T);

        public IEnumerable ResolveAllInstancesOf(Type type)
            => _instances.Where(t => _typeComparer.Equals(t.Key, type)).Select(v => v.Value);

        public T ResolveRequired<T>(params IResolverParameter[] parameters) where T : class
            => (T)_instances.First(t => _typeComparer.Equals(t.Key, typeof(T))).Value;

        public object ResolveRequired(Type type, params IResolverParameter[] parameters)
            => _instances.First(t => _typeComparer.Equals(t.Key, type)).Value;

        #endregion
    }
}
