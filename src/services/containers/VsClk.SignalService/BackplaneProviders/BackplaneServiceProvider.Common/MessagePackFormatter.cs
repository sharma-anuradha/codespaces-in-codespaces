// <copyright file="MessagePackFormatter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
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
            var sequenceReader = new SequenceReader<byte>(contentBuffer);

            byte b;
            sequenceReader.TryRead(out b);
            JsonRpcType rpcType = (JsonRpcType)b;

            switch (rpcType)
            {
                case JsonRpcType.Request:
                    long id;
                    Assumes.True(sequenceReader.TryReadBigEndian(out id), "requestId");
                    int argumentCount;
                    Assumes.True(sequenceReader.TryReadBigEndian(out argumentCount), "argumentCount");

                    var method = Encoding.UTF8.GetString(ReadBuffer(ref sequenceReader));
                    var argumentBuffers = new List<byte[]>();
                    for (int index = 0; index < argumentCount; ++index)
                    {
                        var argumentBuffer = ReadBuffer(ref sequenceReader);
                        argumentBuffers.Add(argumentBuffer);
                    }

                    return new JsonRpcRequestMessagePack(id == -1 ? null : (object)id, method, argumentBuffers.ToArray());
                case JsonRpcType.Result:
                    long resultId;
                    Assumes.True(sequenceReader.TryReadBigEndian(out resultId), "resultId");

                    return new JsonRpcResultMessagePack(resultId, ReadBuffer(ref sequenceReader));
                case JsonRpcType.Error:
                    return MessagePackSerializer.Deserialize<JsonRpcError>(ReadBuffer(ref sequenceReader), resolver);
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
            if (message is JsonRpcRequest jsonRpcRequest)
            {
                Span<byte> stackSpan = stackalloc byte[1 + sizeof(long) + sizeof(int)];
                stackSpan[0] = (byte)JsonRpcType.Request;
                BinaryPrimitives.WriteInt64BigEndian(stackSpan.Slice(1, sizeof(long)), jsonRpcRequest.Id != null ? Convert.ToInt64(jsonRpcRequest.Id) : -1);
                BinaryPrimitives.WriteInt32BigEndian(stackSpan.Slice(1 + sizeof(long), sizeof(int)), jsonRpcRequest.ArgumentCount);
                bufferWriter.Write(stackSpan);

                byte[] methodBytes = Encoding.UTF8.GetBytes(jsonRpcRequest.Method);
                WriteBuffer(bufferWriter, methodBytes);

                foreach (var argument in jsonRpcRequest.ArgumentsList)
                {
                    var buffer = MessagePackSerializer.Serialize(argument, resolver);
                    WriteBuffer(bufferWriter, buffer);
                }
            }
            else if (message is JsonRpcResult jsonRpcResult)
            {
                Span<byte> stackSpan = stackalloc byte[sizeof(long) + 1];
                stackSpan[0] = (byte)JsonRpcType.Result;
                BinaryPrimitives.WriteInt64BigEndian(stackSpan.Slice(1, sizeof(long)), Convert.ToInt64(jsonRpcResult.Id));
                bufferWriter.Write(stackSpan);
                var buffer = MessagePackSerializer.Serialize(jsonRpcResult.Result, resolver);
                WriteBuffer(bufferWriter, buffer);
            }
            else if (message is JsonRpcError jsonRpcError)
            {
                Span<byte> stackSpan = stackalloc byte[1];
                stackSpan[0] = (byte)JsonRpcType.Error;
                bufferWriter.Write(stackSpan);
                var buffer = MessagePackSerializer.Serialize(jsonRpcError, resolver);
                WriteBuffer(bufferWriter, buffer);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static byte[] ReadBuffer(ref SequenceReader<byte> sequenceReader)
        {
            int length;
            Assumes.True(sequenceReader.TryReadBigEndian(out length));

            Span<byte> spanBuffer = stackalloc byte[length];
            Assumes.True(sequenceReader.TryCopyTo(spanBuffer), $"Read buffer length:{length}");
            sequenceReader.Advance(length);
            return spanBuffer.ToArray();
        }

        private static void WriteBuffer(IBufferWriter<byte> bufferWriter, byte[] buffer)
        {
            Span<byte> spanLength = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(spanLength, buffer.Length);
            bufferWriter.Write(spanLength);
            bufferWriter.Write(new ReadOnlySpan<byte>(buffer));
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
