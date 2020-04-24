﻿using CQELight.Abstractions.Events;
using CQELight.Abstractions.Saga.Interfaces;
using System;

namespace CQELight.Abstractions.Saga
{
    /// <summary>
    /// Specific event for saga completion.
    /// This is event is published when a saga call the MarkAsComplete method.
    /// By handling this particular event, you're sure that you're in a final state of the saga, which
    /// may results of differents kind of end-events.
    /// </summary>
    /// <typeparam name="T">Type de saga complétée.</typeparam>
    public class SagaFinishedEvent<T> : BaseDomainEvent
        where T : class, ISaga
    {
        #region Properties

        /// <summary>
        /// Instance of ended saga.
        /// </summary>
        public T Saga { get; protected set; }

        #endregion

        #region Ctor

        /// <summary>
        /// Creation of a new event of type SagaFinishedEvent, with a saga instance.
        /// </summary>
        /// <param name="saga">Instance of finished saga.</param>
        public SagaFinishedEvent(T saga)
        {
            Saga = saga ?? throw new ArgumentNullException(nameof(saga));
            if(!saga.Completed)
            {
                throw new InvalidOperationException("SagaFinishedEvent.ctor() : Cannot create a finished event with an uncomplete saga.");
            }
        }

        #endregion

    }
}
