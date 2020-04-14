// <copyright file="RelayHubStream.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// A stream implementation based on an IRelayHubProxy instance.
    /// </summary>
    public class RelayHubStream : Stream
    {
        private readonly TraceSource trace;
        private readonly List<byte[]> writeQueue = new List<byte[]>();
        private readonly BufferBlock<byte[]> bufferBlock = new BufferBlock<byte[]>();
        private SequenceRelayDataHubProxy sequenceReader;

        private CancellationTokenSource closeCts = new CancellationTokenSource();
        private Buffer readBuffer;
        private int readBufferOffset;
        private bool isClosed;
        private int nextMessageId;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayHubStream"/> class.
        /// </summary>
        /// <param name="relayHubProxy">A relay hub proxy instance.</param>
        /// <param name="targetParticipantId">The participant Id.</param>
        /// <param name="streamId">The stream Id.</param>
        /// <param name="trace">Optional trace.</param>
        public RelayHubStream(IRelayHubProxy relayHubProxy, string targetParticipantId, string streamId, TraceSource trace = null)
        {
            RelayHubProxy = Requires.NotNull(relayHubProxy, nameof(relayHubProxy));
            Requires.NotNullOrEmpty(targetParticipantId, nameof(targetParticipantId));
            Requires.NotNullOrEmpty(streamId, nameof(streamId));

            this.trace = trace;
            TargetParticipantId = targetParticipantId;
            StreamId = streamId;

            Attach();
        }

        /// <summary>
        /// Event fired when the stream is closed.
        /// </summary>
        public event EventHandler<EventArgs> Closed;

        /// <summary>
        /// Gets a value indicating whether the hub stream is closed.
        /// </summary>
        public bool IsClosed => this.isClosed;

        /// <summary>
        /// Gets the IRelayHubProxy underlying proxy.
        /// </summary>
        public IRelayHubProxy RelayHubProxy { get; }

        /// <summary>
        /// Gets the target participant id.
        /// </summary>
        public string TargetParticipantId { get; }

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

        private string StreamId { get; }

        private CancellationToken CloseToken => this.closeCts.Token;

        /// <summary>
        /// Gets the pending flushed data.
        /// </summary>
        /// <param name="nextMessageSequence">The next message id.</param>
        /// <returns>Buffer data.</returns>
        public byte[] GetFlushedData(out int nextMessageSequence)
        {
            var flushedData = Combine(this.writeQueue.ToArray());
            this.writeQueue.Clear();
            nextMessageSequence = Interlocked.Increment(ref this.nextMessageId);

            return flushedData;
        }

        /// <inheritdoc/>
        public override void Flush()
        {
        }

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            int nextSequenceId;
            var flushedData = GetFlushedData(out nextSequenceId);

            return RelayHubProxy.SendDataAsync(
                SendOption.None,
                new string[] { TargetParticipantId },
                StreamId,
                flushedData,
                RelayHubMessageProperties.CreateMessageSequence(nextSequenceId),
                HubMethodOption.Send,
                cancellationToken);
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
            if (this.isClosed)
            {
                throw new ObjectDisposedException(nameof(RelayHubStream));
            }

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
            System.Buffer.BlockCopy(buffer, offset, data, 0, count);

            this.trace?.Verbose($"->WriteAsync buffer.Length:{buffer.Length} offset:{offset} count:{count}");
            this.writeQueue.Add(data);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (this.isClosed)
            {
                throw new ObjectDisposedException(nameof(RelayHubStream));
            }

            this.trace?.Verbose($"-> ReadAsync buffer.Length:{buffer.Length} offset:{offset} count:{count}");

            if (this.readBuffer.Count == 0)
            {
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, CloseToken))
                {
                    try
                    {
                        this.trace?.Verbose($"-> ReadDataAsync");
                        var dataRead = await this.bufferBlock.ReceiveAsync(cts.Token).ConfigureAwait(false);
                        this.readBuffer = dataRead;
                        this.trace?.Verbose($"<- ReadDataAsync length:{this.readBuffer.Count}");
                        this.readBufferOffset = 0;
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

            return ReadFromBuffer(buffer, offset, count);
        }

        private static byte[] Combine(byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }

            return rv;
        }

        private int ReadFromBuffer(byte[] buffer, int offset, int count)
        {
            int available = this.readBuffer.Count - this.readBufferOffset;
            if (count >= available)
            {
                // Fully consume the read buffer.
                this.readBuffer.Slice(this.readBufferOffset, available).CopyTo(buffer, offset);
                this.readBuffer = Buffer.Empty;
                return available;
            }
            else
            {
                // Partially consume the read buffer.
                this.readBuffer.Slice(this.readBufferOffset, count).CopyTo(buffer, offset);
                this.readBufferOffset += count;
                return count;
            }
        }

        private void Attach()
        {
            this.sequenceReader = new SequenceRelayDataHubProxy(
                RelayHubProxy,
                (e) => e.Data != null && e.FromParticipant.Id == TargetParticipantId && e.Type == StreamId);
            this.sequenceReader.ReceiveData += OnReceiveData;
            RelayHubProxy.ParticipantChanged += OnParticipantChanged;
            RelayHubProxy.Disconnected += OnDisconnected;
        }

        private void Detach()
        {
            this.sequenceReader.ReceiveData += OnReceiveData;
            this.sequenceReader.Dispose();
            RelayHubProxy.ParticipantChanged -= OnParticipantChanged;
            RelayHubProxy.Disconnected -= OnDisconnected;
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            CloseIfInternal();
        }

        private void OnReceiveData(object sender, ReceiveDataEventArgs e)
        {
            this.trace?.Verbose($"OnReceiveData length:{e.Data.Length}");

            var copy = new byte[e.Data.Length];
            System.Buffer.BlockCopy(e.Data, 0, copy, 0, e.Data.Length);
            this.bufferBlock.Post(copy);
        }

        private void OnParticipantChanged(object sender, ParticipantChangedEventArgs e)
        {
            if (e.ChangeType == ParticipantChangeType.Removed && e.Participant.Id == TargetParticipantId)
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
            Closed?.Invoke(this, EventArgs.Empty);
            if (RelayHubProxy != null)
            {
                Detach();
            }

            this.closeCts.Cancel();
        }
    }
}
