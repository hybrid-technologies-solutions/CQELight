using System;

namespace CQELight.Buses.InMemory
{
    /// <summary>
    /// Attribute for defining that a specific handler is critical,
    /// meaning that if this handler failed, the next ones shouldn't be invoked.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CriticalHandlerAttribute : Attribute
    {
    }
}
