namespace NServiceBus.Gateway.RavenDB
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Raven.Client.Documents;

    class RavenGatewayDeduplicationStorage : IGatewayDeduplicationStorage
    {
        public RavenGatewayDeduplicationStorage(IDocumentStore documentStore, TimeSpan deduplicationDataTimeToLive)
        {
            this.documentStore = documentStore;
            this.deduplicationDataTimeToLive = deduplicationDataTimeToLive;
        }

        public bool SupportsDistributedTransactions => false;

        public async Task<IDeduplicationSession> CheckForDuplicate(string messageId, ContextBag context, CancellationToken cancellationToken = default)
        {
            var session = documentStore.OpenAsyncSession();
            var isDuplicate = await session.LoadAsync<GatewayMessage>(MessageIdHelper.EscapeMessageId(messageId), cancellationToken).ConfigureAwait(false) != null;

            return new RavenDeduplicationSession(session, isDuplicate, messageId, deduplicationDataTimeToLive);
        }

        readonly IDocumentStore documentStore;
        readonly TimeSpan deduplicationDataTimeToLive;
    }
}
