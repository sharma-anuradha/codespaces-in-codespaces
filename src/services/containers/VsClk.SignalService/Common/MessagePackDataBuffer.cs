// <copyright file="MessagePackDataBuffer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using MessagePack;
using MessagePack.Resolvers;

namespace Microsoft.VsCloudKernel.SignalService.Common
{
    /// <summary>
    /// A simple structure to hold and serialize/deserialize an instance type using the MessagePack namespace.
    /// </summary>
    /// <typeparam name="T">Type of the instance to buffer.</typeparam>
    public class MessagePackDataBuffer<T>
    {
        private byte[] dataBuffer;

        public MessagePackDataBuffer()
        {
        }

        public MessagePackDataBuffer(T data)
        {
            Data = data;
        }

        /// <summary>
        /// Gets or sets the deserialized/serialized value based on th edata buffer content.
        /// </summary>
        public T Data
        {
            get
            {
                return this.dataBuffer == null ? default(T) : MessagePackSerializer.Deserialize<T>(this.dataBuffer, ContractlessStandardResolver.Instance);
            }

            set
            {
                this.dataBuffer = MessagePackSerializer.Serialize(value, ContractlessStandardResolver.Instance);
            }
        }

        public T GetAndSet(Func<T, T> changeCallback)
        {
            var newValue = changeCallback(Data);
            Data = newValue;
            return newValue;
        }

        public T GetAndSet(Action<T> changeCallback)
        {
            var data = Data;
            changeCallback(data);
            Data = data;
            return data;
        }
    }
}
