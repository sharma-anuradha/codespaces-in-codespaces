// <copyright file="SequenceRelayStreamProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// A sequence based relay data provider.
    /// </summary>
    public class SequenceRelayStreamProvider : IRelayStreamProvider
    {
        private readonly TaskCompletionSource<byte[]> firstDataTcs = new TaskCompletionSource<byte[]>();
        private SequenceDataReader<byte[]> sequenceDataReader;
        private int writeSequence;

        /// <inheritdoc/>
        public async Task<byte[]> ReadDataAsync(CancellationToken cancellationToken)
        {
            var data = this.sequenceDataReader != null ? await this.sequenceDataReader.ReadNextMessageAsync(cancellationToken) : await SequenceDataReader<byte[]>.GetValueAsync(this.firstDataTcs, cancellationToken);
            return data;
        }

        /// <inheritdoc/>
        public byte[] EncodeHubData(byte[] data)
        {
            var nextSequence = Interlocked.Increment(ref this.writeSequence);
            return EncodeHubData(nextSequence, data);
        }

        /// <inheritdoc/>
        public void HandleReceivedData(byte[] data)
        {
            var messageInfo = DecodeHubData(data);
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"->RelayHubStream.OnReceiveData sequence:{messageInfo.Item1} data-length:{messageInfo.Item2.Length}");
#endif
            if (this.sequenceDataReader == null)
            {
                this.sequenceDataReader = new SequenceDataReader<byte[]>(messageInfo.Item1);
                this.firstDataTcs.TrySetResult(messageInfo.Item2);
            }
            else
            {
                this.sequenceDataReader.NextMessage(messageInfo.Item1, messageInfo.Item2);
            }
        }

        private static byte[] EncodeHubData(int sequence, byte[] data)
        {
            using (var ms = new MemoryStream())
            {
                var bw = new BinaryWriter(ms);
                bw.Write(sequence);
                bw.Write(data);
                bw.Flush();
                return ms.ToArray();
            }
        }

        private static (int, byte[]) DecodeHubData(byte[] hubData)
        {
            using (var ms = new MemoryStream(hubData))
            {
                var br = new BinaryReader(ms);
                var sequence = br.ReadInt32();
                return (sequence, br.ReadBytes(hubData.Length - (int)ms.Position));
            }
        }

        /// <summary>
        /// Class to retrieve messages that comes without a sequence.
        /// </summary>
        /// <typeparam name="TValue">Type of data to read in sequence.</typeparam>
        private class SequenceDataReader<TValue>
        {
            private readonly ConcurrentDictionary<int, TaskCompletionSource<TValue>> messages = new ConcurrentDictionary<int, TaskCompletionSource<TValue>>();
            private int readSequence;

            public SequenceDataReader(int readSequence)
            {
                this.readSequence = readSequence;
            }

            public static Task<TValue> GetValueAsync(TaskCompletionSource<TValue> tcs, CancellationToken cancellationToken)
            {
                using (cancellationToken.Register(() => tcs.TrySetCanceled()))
                {
                    return tcs.Task;
                }
            }

            public void NextMessage(int sequenceId, TValue value)
            {
                messages.GetOrAdd(sequenceId, (key) => new TaskCompletionSource<TValue>()).TrySetResult(value);
            }

            public async Task<TValue> ReadNextMessageAsync(CancellationToken cancellationToken)
            {
                var nextReadSequence = Interlocked.Increment(ref this.readSequence);
                var tcs = messages.GetOrAdd(nextReadSequence, (key) => new TaskCompletionSource<TValue>());
                var value = await GetValueAsync(tcs, cancellationToken);
                messages.TryRemove(nextReadSequence, out tcs);
                return value;
            }
        }
    }
}
