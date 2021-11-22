namespace NServiceBus.Gateway.RavenDB
{
    using Extensibility;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Session;
    using System;
    using System.Threading.Tasks;

    class RavenGatewayDeduplicationStorage : IGatewayDeduplicationStorage
    {
        public RavenGatewayDeduplicationStorage(IDocumentStore documentStore, TimeSpan deduplicationDataTimeToLive, bool useClusterWideTransactions)
        {
            this.documentStore = documentStore;
            this.deduplicationDataTimeToLive = deduplicationDataTimeToLive;
            this.useClusterWideTransactions = useClusterWideTransactions;
        }

        public bool SupportsDistributedTransactions => false;

        public async Task<IDeduplicationSession> CheckForDuplicate(string messageId, ContextBag context)
        {
            var options = new SessionOptions()
            {
                TransactionMode = useClusterWideTransactions ? TransactionMode.ClusterWide : TransactionMode.SingleNode
            };

            var session = documentStore.OpenAsyncSession(options);

            // Optimistic concurrency is incompatible with cluster-wide transactions
            session.Advanced.UseOptimisticConcurrency = !useClusterWideTransactions;

            var isDuplicate = await session.LoadAsync<GatewayMessage>(MessageIdHelper.EscapeMessageId(messageId)).ConfigureAwait(false) != null;

            return new RavenDeduplicationSession(session, isDuplicate, messageId, deduplicationDataTimeToLive);
        }

        readonly IDocumentStore documentStore;
        readonly TimeSpan deduplicationDataTimeToLive;
        readonly bool useClusterWideTransactions;
    }
}
