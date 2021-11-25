namespace NServiceBus.Gateway.RavenDB
{
    using Raven.Client;
    using Raven.Client.Documents.Session;
    using System;
    using System.Threading.Tasks;

    sealed class RavenDeduplicationSession : IDeduplicationSession
    {
        public bool IsDuplicate { get; }

        public async Task MarkAsDispatched()
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

                await session.StoreAsync(gatewayMessage).ConfigureAwait(false);
                if (useClusterWideTransactions)
                {
                    // normally RavenDB automatically creates the compare exchange value for us. Unfortunately the expiry metadata is not automatically set
                    // so we have create the compare exchange value manually. Using the same prefix as RavenDB server to make sure when the bug is fixed we can
                    // just switch back to using StoreAsync only as well as removing the ugly if here.
                    var compareExchangeValue = session.Advanced.ClusterTransaction.CreateCompareExchangeValue<string>($"rvn-atomic/{gatewayMessage.Id}", null);
                    compareExchangeValue.Metadata[Constants.Documents.Metadata.Expires] = expiry;
                }
                session.Advanced.GetMetadataFor(gatewayMessage)[Constants.Documents.Metadata.Expires] = expiry;
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public RavenDeduplicationSession(IAsyncDocumentSession session, bool isDuplicate, string messageId, TimeSpan deduplicationDataTimeToLive, bool useClusterWideTransactions)
        {
            this.session = session;
            IsDuplicate = isDuplicate;
            this.messageId = messageId;
            this.deduplicationDataTimeToLive = deduplicationDataTimeToLive;
            this.useClusterWideTransactions = useClusterWideTransactions;
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
        readonly bool useClusterWideTransactions;
    }
}
