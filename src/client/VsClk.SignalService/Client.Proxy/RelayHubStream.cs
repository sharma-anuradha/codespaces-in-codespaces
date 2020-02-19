// <copyright file="RelayHubStream.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// A stream implementation based on an IRelayHubProxy instance.
    /// </summary>
    public class RelayHubStream : Stream
    {
        private IRelayHubProxy relayHubProxy;
        private CancellationTokenSource closeCts = new CancellationTokenSource();
        private byte[] overflowData;
        private bool isClosed;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayHubStream"/> class.
        /// </summary>
        /// <param name="relayHubProxy">A relay hub proxy instance.</param>
        /// <param name="targetParticipantId">The participant Id.</param>
        /// <param name="streamId">The stream Id.</param>
        /// <param name="relayStreamProvider">The relay stream provider.</param>
        public RelayHubStream(IRelayHubProxy relayHubProxy, string targetParticipantId, string streamId, IRelayStreamProvider relayStreamProvider)
        {
            this.relayHubProxy = Requires.NotNull(relayHubProxy, nameof(relayHubProxy));
            Requires.NotNullOrEmpty(targetParticipantId, nameof(targetParticipantId));
            Requires.NotNullOrEmpty(streamId, nameof(streamId));

            TargetParticipantId = targetParticipantId;
            StreamId = streamId;
            RelayStreamProvider = Requires.NotNull(relayStreamProvider, nameof(relayStreamProvider));

            Attach();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayHubStream"/> class.
        /// </summary>
        /// <param name="relayHubProxy">A relay hub proxy instance.</param>
        /// <param name="targetParticipantId">The participant Id.</param>
        /// <param name="streamId">The stream Id.</param>
        public RelayHubStream(IRelayHubProxy relayHubProxy, string targetParticipantId, string streamId)
            : this(relayHubProxy, targetParticipantId, streamId, new DefaultRelayStreamProvider())
        {
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

        private IRelayStreamProvider RelayStreamProvider { get; }

        private string StreamId { get; }

        private string TargetParticipantId { get; }

        private CancellationToken CloseToken => this.closeCts.Token;

        /// <summary>
        /// re join the hub to re-open the closed stream.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task ReJoinAsync(CancellationToken cancellationToken)
        {
            if (!this.isClosed)
            {
                throw new InvalidOperationException("RelayHubStream hasn't beeen closed");
            }

            await this.relayHubProxy.ReJoinAsync(default, cancellationToken);
            Attach();
        }

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

            return relayHubProxy.SendDataAsync(
                SendOption.None,
                new string[] { TargetParticipantId },
                StreamId,
                RelayStreamProvider.EncodeHubData(data),
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
                        var data = await RelayStreamProvider.ReadDataAsync(cts.Token);
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

        private void Attach()
        {
            this.relayHubProxy.ReceiveData += OnReceiveData;
            this.relayHubProxy.ParticipantChanged += OnParticipantChanged;
            this.relayHubProxy.Disconnected += OnDisconnected;
        }

        private void Detach()
        {
            this.relayHubProxy.ReceiveData -= OnReceiveData;
            this.relayHubProxy.ParticipantChanged -= OnParticipantChanged;
            this.relayHubProxy.Disconnected -= OnDisconnected;
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            CloseIfInternal();
        }

        private void OnReceiveData(object sender, ReceiveDataEventArgs e)
        {
            if (e.FromParticipant.Id == TargetParticipantId && e.Type == StreamId)
            {
                RelayStreamProvider.HandleReceivedData(e.Data);
            }
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
            if (this.relayHubProxy != null)
            {
                Detach();
            }

            this.closeCts.Cancel();
        }
    }
}
