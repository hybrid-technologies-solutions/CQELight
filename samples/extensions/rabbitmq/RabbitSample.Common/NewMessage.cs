using CQELight.Abstractions.Events;
using System;

namespace RabbitSample.Common
{
    public class NewMessage : BaseDomainEvent
    {
        #region Properties

        public string Payload { get; set; }

        #endregion
    }
}
