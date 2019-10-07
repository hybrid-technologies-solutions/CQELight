using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.Buses.RabbitMQ
{
    internal static class Consts
    {

        #region Consts

        internal static readonly string CONST_CQE_EXCHANGE_NAME = "cqelight_global_exchange";

        internal static readonly string CONST_DEAD_LETTER_QUEUE_PREFIX = "cqe_dlq_";

        public static readonly string CONST_DEAD_LETTER_EXCHANGE_NAME = "cqelight_dead_letter_exchange";
        public static readonly string CONST_DEAD_LETTER_QUEUE_NAME = "cqelight_dead_letter_queue";

        public readonly static string CONST_DEAD_LETTER_EXCHANGE_RABBIT_KEY = "x-dead-letter-exchange";

        #endregion

    }
}
