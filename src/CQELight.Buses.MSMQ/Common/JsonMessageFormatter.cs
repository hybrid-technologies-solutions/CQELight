﻿using CQELight.Tools.Extensions;
using System;
using System.IO;
using System.Messaging;
using System.Text;

namespace CQELight.Buses.MSMQ.Common
{
    // Credit : https://gist.github.com/jchadwick/2430984
    internal class JsonMessageFormatter : IMessageFormatter
    {

        #region Public methods

        public bool CanRead(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var stream = message.BodyStream;

            return stream?.CanRead == true
                && stream?.Length > 0;
        }

        public object Clone() => new JsonMessageFormatter();

        public object Read(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (!CanRead(message))
            {
                return null;
            }

            using (var reader = new StreamReader(message.BodyStream, Encoding.UTF8))
            {
                var json = reader.ReadToEnd();
                return json.FromJson();
            }
        }

        public void Write(Message message, object obj)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            string json = obj.ToJson(true);

            var messageBytes = Encoding.UTF8.GetBytes(json);
            if (messageBytes.Length > 4 * 1024 * 1024)
            {
                throw new InvalidOperationException("Message exceed MSMQ max size.");
            }
            message.BodyStream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            //Need to reset the body type, in case the same message
            //is reused by some other formatter.
            message.BodyType = 0;
        }

        #endregion

    }
}
