using CQELight.Abstractions.DDD;
using CQELight.Abstractions.EventStore.Interfaces;
using CQELight.Tools.Extensions;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace CQELight.Extensions
{
    /// <summary>
    /// Extensions for IEventSourceAggregate objects.
    /// </summary>
    public static class IEventSourcedAggregateExtensions
    {
        #region Static members

        private static readonly ConcurrentDictionary<Type, (PropertyInfo PropertyInfos, FieldInfo FieldInfos)> _stateInfosByType
            = new ConcurrentDictionary<Type, (PropertyInfo, FieldInfo)>();

        #endregion

        #region Public static methods

        /// <summary>
        /// Retrieve by reflection the aggregate state serialized.
        /// </summary>
        /// <param name="aggregate"></param>
        /// <returns></returns>
        public static string GetSerializedState(this IEventSourcedAggregate aggregate)
        {
            if (aggregate == null)
            {
                throw new ArgumentNullException(nameof(aggregate));
            }
            var aggregateType = aggregate.GetType();
            var (PropertyInfos, FieldInfos) = _stateInfosByType.GetOrAdd(aggregateType, _ =>
             {
                 PropertyInfo stateProp = aggregateType.GetAllProperties().FirstOrDefault(p => p.PropertyType.IsSubclassOf(typeof(AggregateState)));
                 FieldInfo stateField = aggregateType.GetAllFields().FirstOrDefault(f => f.FieldType.IsSubclassOf(typeof(AggregateState)));
                 return (stateProp, stateField);
             });

            AggregateState? state = null;
            if (PropertyInfos != null)
            {
                state = PropertyInfos.GetValue(aggregate) as AggregateState;
            }
            else if (FieldInfos != null)
            {
                state = FieldInfos.GetValue(aggregate) as AggregateState;
            }

            if (state == null)
            {
                throw new InvalidOperationException("IEventSourcedAggregateExtensions.GetSerializedState() : State cannot be retrieved from aggregate.");
            }
            return state.Serialize();
        }

        #endregion

    }
}
