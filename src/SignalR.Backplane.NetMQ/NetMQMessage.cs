namespace Signalr.Backplane.NetMQ
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.AspNet.SignalR.Messaging;

    internal class NetMQMessage
    {
        private readonly long _messageId;
        private readonly ScaleoutMessage _scaleoutMessage;

        public NetMQMessage(long messageId, IList<Message> messages)
        {
            _messageId = messageId;
            _scaleoutMessage = new ScaleoutMessage(messages);
        }

        public NetMQMessage(long messageId, ScaleoutMessage message)
        {
            _messageId = messageId;
            _scaleoutMessage = message;
        }

        public long MessageId
        {
            get { return _messageId; }
        }

        public ScaleoutMessage ScaleoutMessage
        {
            get { return _scaleoutMessage; }
        }

        public byte[] GetBytes()
        {
            byte[] binaryId = BitConverter.GetBytes(MessageId);
            using(var memoryStream = new MemoryStream())
            {
                byte[] messagesBytes = ScaleoutMessage.ToBytes();
                memoryStream.Write(binaryId, 0, binaryId.Length);
                memoryStream.Write(messagesBytes, 0, messagesBytes.Length);

                return memoryStream.ToArray();
            }
        }

        public static NetMQMessage FromBytes(byte[] bytes)
        {
            using(var r = new BinaryReader(new MemoryStream(bytes)))
            {
                long messageId = r.ReadInt64();
                byte[] messageBytes = r.ReadBytes(bytes.Length - 8);
                ScaleoutMessage scaleoutMessage = ScaleoutMessage.FromBytes(messageBytes);

                return new NetMQMessage(messageId, scaleoutMessage);
            }
        }
    }
}