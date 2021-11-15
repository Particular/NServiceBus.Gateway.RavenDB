namespace NServiceBus
{
    using Gateway;
    using Gateway.RavenDB;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Operations.Expiration;
    using Raven.Client.ServerWide.Commands;
    using Settings;
    using System;

    /// <summary>
    /// Configures the deduplication storage.
    /// </summary>
    public class RavenGatewayDeduplicationConfiguration : GatewayDeduplicationConfiguration
    {
        /// <summary>
        /// Initialize a new instance of the RavenDB gateway deduplication configuration
        /// </summary>
        public RavenGatewayDeduplicationConfiguration(Func<IServiceProvider, IReadOnlySettings, IDocumentStore> documentStoreFactory)
        {
            Guard.AgainstNull(nameof(documentStoreFactory), documentStoreFactory);

            this.documentStoreFactory = documentStoreFactory;
        }

        /// <inheritdoc />
        public override void Setup(IReadOnlySettings settings)
        {
            this.settings = settings;

            base.Setup(settings);
        }

        /// <inheritdoc />
        public override IGatewayDeduplicationStorage CreateStorage(IServiceProvider builder)
        {
            var documentStore = documentStoreFactory(builder, settings);

            if (DisableClusterWideTransactions)
            {
                // Currently do not support running without cluister-wide TX and clusters with more than one node.
                EnsureClusterConfiguration(documentStore);
            }
            EnableExpirationFeature(documentStore, FrequencyToRunDeduplicationDataCleanup);

            return new RavenGatewayDeduplicationStorage(documentStore, DeduplicationDataTimeToLive, !DisableClusterWideTransactions);
        }

        static void EnableExpirationFeature(IDocumentStore documentStore, long frequencyToRunDeduplicationDataCleanup)
        {
            documentStore.Maintenance.Send(new ConfigureExpirationOperation(new ExpirationConfiguration
            {
                Disabled = false,
                DeleteFrequencyInSec = frequencyToRunDeduplicationDataCleanup
            }));
        }

        static void EnsureClusterConfiguration(IDocumentStore store)
        {
            using (var s = store.OpenSession())
            {
                var getTopologyCmd = new GetDatabaseTopologyCommand();
                s.Advanced.RequestExecutor.Execute(getTopologyCmd, s.Advanced.Context);

                if (getTopologyCmd.Result.Nodes.Count != 1)
                {
                    throw new InvalidOperationException("RavenDB Persistence does not support RavenDB clusters with more than one Leader/Member node. Only clusters with a single Leader and (optionally) Watcher nodes are supported.");
                }
            }
        }

        /// <summary>
        /// The time to keep deduplication information, default value is 7 days
        /// </summary>
        public TimeSpan DeduplicationDataTimeToLive { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Frequency, in seconds, at which to run the cleanup of deduplication data.
        /// </summary>
        public long FrequencyToRunDeduplicationDataCleanup { get; set; } = 600;

        /// <summary>
        /// Disables Cluster-wide transactions support to improve performance when runing against a single node.
        /// Cluster-wide transactions cannot be disabled when runnign agains a cluster with more than one node.
        /// </summary>
        public bool DisableClusterWideTransactions { get; set; }

        IReadOnlySettings settings;
        readonly Func<IServiceProvider, IReadOnlySettings, IDocumentStore> documentStoreFactory;
    }
}
