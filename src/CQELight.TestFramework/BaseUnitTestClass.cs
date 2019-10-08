using CQELight.Dispatcher;
using CQELight.IoC;
using CQELight.TestFramework.IoC;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;

namespace CQELight.TestFramework
{
    /// <summary>
    /// Base class for unit testing classes.
    /// </summary>
    public class BaseUnitTestClass
    {
        #region Members

        protected readonly TestScopeFactory _testFactory;

        #endregion

        #region Ctor

        protected BaseUnitTestClass(bool disableIoc)
        {

            UnitTestTools.IsInUnitTestMode = true;
            UnitTestTools.IsInIntegrationTestMode = GetType().Assembly.GetName().Name.Contains(".Integration.");

            if (!UnitTestTools.IsInIntegrationTestMode && !disableIoc)
            {
                _testFactory = new TestScopeFactory();
                DIManager.Init(_testFactory);
            }
        }

        protected BaseUnitTestClass()
            :this(false)
        {
        }

        #endregion

        #region Protected methods

        protected void CleanRegistrationInDispatcher()
        {
            if (UnitTestTools.IsInIntegrationTestMode)
            {
                CoreDispatcher.CleanRegistrations();
            }
        }

        protected void AddRegistrationFor<T>(object instance)
        {
            if (UnitTestTools.IsInIntegrationTestMode)
            {
                throw new InvalidOperationException("BaseUnitTestClass.AddRegistrationFor() : Cannot add registration into your IoC container. " +
                    "You have to manage it in your test initialization.");
            }
            _testFactory.Instances[typeof(T)] = instance;
        }

        protected void DisableIoC()
        {
            DIManager.IsInit = false;
            DIManager._scopeFactory = null;
        }

        #endregion

    }
}
