namespace NServiceBus.Gateway.RavenDB
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Raven.Client;
    using Raven.Client.Documents.Session;

    sealed class RavenDeduplicationSession : IDeduplicationSession
    {
        public bool IsDuplicate { get; }

        public async Task MarkAsDispatched(CancellationToken cancellationToken = default)
        {
            if (!IsDuplicate)
            {
                var timeReceived = DateTime.UtcNow;
                var expiry = timeReceived + deduplicationDataTimeToLive;

                var gatewayMessage = new GatewayMessage()
                {
                    Id = MessageIdHelper.EscapeMessageId(messageId),
                    TimeReceived = timeReceived
                };

                await session.StoreAsync(gatewayMessage, cancellationToken).ConfigureAwait(false);
                session.Advanced.GetMetadataFor(gatewayMessage)[Constants.Documents.Metadata.Expires] = expiry;
                await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public RavenDeduplicationSession(IAsyncDocumentSession session, bool isDuplicate, string messageId, TimeSpan deduplicationDataTimeToLive)
        {
            this.session = session;
            IsDuplicate = isDuplicate;
            this.messageId = messageId;
            this.deduplicationDataTimeToLive = deduplicationDataTimeToLive;
        }

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    session?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        bool disposedValue = false;
        readonly IAsyncDocumentSession session;
        readonly string messageId;
        readonly TimeSpan deduplicationDataTimeToLive;
    }
}
