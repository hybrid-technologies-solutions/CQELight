using CQELight.Tools;
using System;
using System.Reflection;

namespace CQELight.Buses.InMemory.Events
{
    sealed class EventHandlingInfos
    {
        #region Properties


        public MethodInfo HandlerMethod { get; }
        public object HandlerInstance { get; }
        public HandlerPriority HandlerPriority { get; }

        #endregion

        #region Ctor

        public EventHandlingInfos(MethodInfo handlerMethod, object handlerInstance,
            HandlerPriority handlerPriority)
        {
            HandlerMethod = handlerMethod ?? throw new ArgumentNullException(nameof(handlerMethod));
            HandlerInstance = handlerInstance ?? throw new ArgumentNullException(nameof(handlerInstance));
            HandlerPriority = handlerPriority;
        }

        #endregion

        #region Overriden methods

        public override bool Equals(object obj)
        {
            if (obj is EventHandlingInfos infos)
            {
                return infos.HandlerMethod.ToString() == HandlerMethod.ToString()
                    && new TypeEqualityComparer().Equals(infos.HandlerMethod.ReflectedType, HandlerMethod.ReflectedType);
            }
            return false;
        }

        public override int GetHashCode()
            => HandlerMethod.ToString().GetHashCode();

        #endregion

    }
}
