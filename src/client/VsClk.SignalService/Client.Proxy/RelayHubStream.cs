using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// A stream implementation based on an IRelayHubProxy instance.
    /// </summary>
    public class RelayHubStream : Stream
    {
        private readonly IRelayHubProxy relayHubProxy;
        private readonly string participantId;
        private readonly string streamId;
        private readonly TaskCompletionSource<byte[]> firstDataTcs = new TaskCompletionSource<byte[]>();
        private SequenceDataReader<byte[]> sequenceDataReader;
        private int writeSequence;

        private CancellationTokenSource closeCts = new CancellationTokenSource();
        private byte[] overflowData;
        private bool isClosed;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayHubStream"/> class.
        /// </summary>
        /// <param name="relayHubProxy">A relay hub proxy instance.</param>
        /// <param name="participantId">The participant Id.</param>
        /// <param name="streamId">The stream Id.</param>
        public RelayHubStream(IRelayHubProxy relayHubProxy, string participantId, string streamId)
        {
            this.relayHubProxy = Requires.NotNull(relayHubProxy, nameof(relayHubProxy));
            Requires.NotNullOrEmpty(participantId, nameof(participantId));
            Requires.NotNullOrEmpty(streamId, nameof(streamId));

            this.participantId = participantId;
            this.streamId = streamId;

            relayHubProxy.ReceiveData += OnReceiveData;
            relayHubProxy.ParticipantChanged += OnParticipantChanged;
        }

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        /// <inheritdoc/>
        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        private CancellationToken CloseToken => closeCts.Token;

        /// <inheritdoc/>
        public override void Flush()
        {
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Close()
        {
            CloseIfInternal();
            base.Close();
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // Validate all arguments
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (offset < 0 || offset >= buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            if (count < 0 || offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (count == 0)
            {
                return Task.CompletedTask;
            }

            var data = new byte[count];
            Buffer.BlockCopy(buffer, offset, data, 0, count);

            var nextSequence = Interlocked.Increment(ref this.writeSequence);
            return relayHubProxy.SendDataAsync(
                SendOption.None,
                new string[] { participantId },
                streamId,
                EncodeHubData(nextSequence, data),
                cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int totalBytes;
            if (overflowData != null)
            {
                overflowData = ReturnData(overflowData, buffer, offset, count, out totalBytes);
            }
            else
            {
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, CloseToken))
                {
                    try
                    {
                        var data = this.sequenceDataReader != null ? await this.sequenceDataReader.ReadNextMessageAsync(cancellationToken) : await SequenceDataReader<byte[]>.GetValueAsync(this.firstDataTcs, cancellationToken);
                        overflowData = ReturnData(data, buffer, offset, count, out totalBytes);
                    }
                    catch (OperationCanceledException ex)
                    {
                        OperationCanceledException operationCanceledException = ex;
                        if (operationCanceledException.CancellationToken == CloseToken)
                        {
                            throw new ObjectDisposedException("Stream is disposed", operationCanceledException);
                        }

                        throw operationCanceledException;
                    }
                }
            }

            return totalBytes;
        }

        private static byte[] ReturnData(byte[] data, byte[] buffer, int offset, int count, out int totalBytes)
        {
            totalBytes = Math.Min(data.Length, count);
            Buffer.BlockCopy(data, 0, buffer, offset, totalBytes);
            if (data.Length > count)
            {
                System.Diagnostics.Debug.WriteLine($"overflow data length:{data.Length - count}");

                byte[] array = new byte[data.Length - count];
                Buffer.BlockCopy(data, count, array, 0, array.Length);
                return array;
            }

            return null;
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

        private void OnReceiveData(object sender, ReceiveDataEventArgs e)
        {
            if (e.FromParticipant.Id == participantId && e.Type == streamId)
            {
                var messageInfo = DecodeHubData(e.Data);
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"->RelayHubStream.OnReceiveData uniqueId:{e.UniqueId} sequence:{messageInfo.Item1} data-length:{messageInfo.Item2.Length}");
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
        }

        private void OnParticipantChanged(object sender, ParticipantChangedEventArgs e)
        {
            if (e.ChangeType == ParticipantChangeType.Removed && e.Participant.Id == participantId)
            {
                CloseIfInternal();
            }
        }

        private void CloseIfInternal()
        {
            if (!isClosed)
            {
                isClosed = true;
                CloseInternal();
            }
        }

        private void CloseInternal()
        {
            this.relayHubProxy.ReceiveData -= OnReceiveData;
            this.relayHubProxy.ParticipantChanged -= OnParticipantChanged;
            this.closeCts.Cancel();
        }

        /// <summary>
        /// Class to retrieve messages that comes without a sequence
        /// </summary>
        /// <typeparam name="TValue">Type of data to read in sequence</typeparam>
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
