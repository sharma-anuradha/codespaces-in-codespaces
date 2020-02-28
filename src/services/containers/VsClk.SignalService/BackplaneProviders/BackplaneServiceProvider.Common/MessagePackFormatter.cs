// <copyright file="MessagePackFormatter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using MessagePack;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Json rpc formatter for interact with the backplane service.
    /// Note: this class was optimized to cover the scenario and types required by our backplane service.
    /// </summary>
    internal class MessagePackFormatter : IJsonRpcMessageFormatter
    {
        private static IFormatterResolver resolver = MessagePack.Resolvers.ContractlessStandardResolver.Instance;

        private enum JsonRpcType
        {
            Request = 0,
            Result = 1,
            Error = 2,
        }

        /// <inheritdoc/>
        public JsonRpcMessage Deserialize(ReadOnlySequence<byte> contentBuffer)
        {
            var stream = new MemoryStream(contentBuffer.ToArray());
            var binaryReader = new BinaryReader(stream);
            stream.Position = 0;
            JsonRpcType rpcType = (JsonRpcType)binaryReader.ReadByte();

            switch (rpcType)
            {
                case JsonRpcType.Request:
                    var id = binaryReader.ReadInt64();
                    var method = binaryReader.ReadString();
                    var argumentCount = binaryReader.ReadInt32();
                    var argumentBuffers = new List<byte[]>();
                    for (int index = 0; index < argumentCount; ++index)
                    {
                        argumentBuffers.Add(ReadBuffer(binaryReader));
                    }

                    return new JsonRpcRequestMessagePack(id, method, argumentBuffers.ToArray());
                case JsonRpcType.Result:
                    return new JsonRpcResultMessagePack(binaryReader.ReadInt64(), ReadBuffer(binaryReader));
                case JsonRpcType.Error:
                    return MessagePackSerializer.Deserialize<JsonRpcError>(ReadBuffer(binaryReader), resolver);
                default:
                    throw new NotSupportedException();
            }
        }

        /// <inheritdoc/>
        public object GetJsonText(JsonRpcMessage message)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public void Serialize(IBufferWriter<byte> bufferWriter, JsonRpcMessage message)
        {
            var stream = new MemoryStream();
            var binaryWriter = new BinaryWriter(stream);

            if (message is JsonRpcRequest jsonRpcRequest)
            {
                binaryWriter.Write((byte)JsonRpcType.Request);
                binaryWriter.Write(jsonRpcRequest.Id != null ? Convert.ToInt64(jsonRpcRequest.Id) : 0);
                binaryWriter.Write(jsonRpcRequest.Method);
                binaryWriter.Write(jsonRpcRequest.ArgumentCount);

                foreach (var argument in jsonRpcRequest.ArgumentsList)
                {
                    var buffer = MessagePackSerializer.Serialize(argument, resolver);
                    WriteBuffer(binaryWriter, buffer);
                }
            }
            else if (message is JsonRpcResult jsonRpcResult)
            {
                binaryWriter.Write((byte)JsonRpcType.Result);
                binaryWriter.Write(Convert.ToInt64(jsonRpcResult.Id));
                var buffer = MessagePackSerializer.Serialize(jsonRpcResult.Result, resolver);
                WriteBuffer(binaryWriter, buffer);
            }
            else if (message is JsonRpcError jsonRpcError)
            {
                binaryWriter.Write((byte)JsonRpcType.Error);
                var buffer = MessagePackSerializer.Serialize(jsonRpcError, resolver);
                WriteBuffer(binaryWriter, buffer);
            }
            else
            {
                throw new NotSupportedException();
            }

            var bytes = stream.ToArray();
            bufferWriter.Write(new ReadOnlySpan<byte>(bytes));
        }

        private static void WriteBuffer(BinaryWriter binaryWriter, byte[] buffer)
        {
            binaryWriter.Write((short)buffer.Length);
            binaryWriter.Write(buffer);
        }

        private static byte[] ReadBuffer(BinaryReader binaryReader)
        {
            short size = binaryReader.ReadInt16();
            return binaryReader.ReadBytes(size);
        }

        /// <summary>
        /// Custom JsonRpcRequest class to use the message pack serializer on each argument.
        /// </summary>
        private class JsonRpcRequestMessagePack : JsonRpcRequest
        {
            private readonly byte[][] argumentBuffers;

            internal JsonRpcRequestMessagePack(object id, string method, byte[][] argumentBuffers)
            {
                Id = id;
                Method = method;
                ArgumentsList = new object[argumentBuffers.Length];
                this.argumentBuffers = argumentBuffers;
            }

            public override bool TryGetArgumentByNameOrIndex(string name, int position, Type typeHint, out object value)
            {
                value = MessagePackSerializer.NonGeneric.Deserialize(typeHint, this.argumentBuffers[position], resolver);
                return true;
            }

            public override ArgumentMatchResult TryGetTypedArguments(ReadOnlySpan<System.Reflection.ParameterInfo> parameters, Span<object> typedArguments)
            {
                return base.TryGetTypedArguments(parameters, typedArguments);
            }
        }

        /// <summary>
        /// Custom JsonRpcResult class to use the message pack serializer on the result.
        /// </summary>
        private class JsonRpcResultMessagePack : JsonRpcResult
        {
            private readonly byte[] resultBuffer;

            internal JsonRpcResultMessagePack(object id, byte[] resultBuffer)
            {
                Id = id;
                this.resultBuffer = resultBuffer;
            }

            public override T GetResult<T>()
            {
                var result = MessagePackSerializer.Deserialize<T>(this.resultBuffer, resolver);
                return result;
            }
        }
    }
}
