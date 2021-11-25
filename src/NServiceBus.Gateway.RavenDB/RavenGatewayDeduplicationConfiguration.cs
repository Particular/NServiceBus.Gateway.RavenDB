namespace NServiceBus
{
    using Gateway;
    using Gateway.RavenDB;
    using ObjectBuilder;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Operations.Expiration;
    using Raven.Client.ServerWide.Commands;
    using Raven.Client.ServerWide.Operations;
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
        public RavenGatewayDeduplicationConfiguration(Func<IBuilder, ReadOnlySettings, IDocumentStore> documentStoreFactory)
        {
            Guard.AgainstNull(nameof(documentStoreFactory), documentStoreFactory);

            this.documentStoreFactory = documentStoreFactory;
        }

        /// <inheritdoc />
        public override void Setup(ReadOnlySettings settings)
        {
            this.settings = settings;

            base.Setup(settings);
        }

        /// <inheritdoc />
        public override IGatewayDeduplicationStorage CreateStorage(IBuilder builder)
        {
            var documentStore = documentStoreFactory(builder, settings);

            EnsureCompatibleServerVersion(documentStore);
            EnsureClusterConfiguration(documentStore);
            EnableExpirationFeature(documentStore, FrequencyToRunDeduplicationDataCleanup);

            return new RavenGatewayDeduplicationStorage(documentStore, DeduplicationDataTimeToLive, EnableClusterWideTransactions);
        }

        static void EnableExpirationFeature(IDocumentStore documentStore, long frequencyToRunDeduplicationDataCleanup)
        {
            documentStore.Maintenance.Send(new ConfigureExpirationOperation(new ExpirationConfiguration
            {
                Disabled = false,
                DeleteFrequencyInSec = frequencyToRunDeduplicationDataCleanup
            }));
        }

        void EnsureClusterConfiguration(IDocumentStore store)
        {
            using (var s = store.OpenSession())
            {
                var getTopologyCmd = new GetDatabaseTopologyCommand();
                s.Advanced.RequestExecutor.Execute(getTopologyCmd, s.Advanced.Context);

                if (getTopologyCmd.Result.Nodes.Count > 1 && !EnableClusterWideTransactions)
                {
                    throw new Exception($"The configured database is replicated across multiple nodes, in order to continue, use {nameof(EnableClusterWideTransactions)} on the gateway configuration.");
                }
            }
        }

        void EnsureCompatibleServerVersion(IDocumentStore documentStore)
        {
            var requiredVersion = new Version(5, 2);
            var serverVersion = documentStore.Maintenance.Server.Send(new GetBuildNumberOperation());
            var fullVersion = new Version(serverVersion.FullVersion);

            if (fullVersion.Major < requiredVersion.Major ||
                (fullVersion.Major == requiredVersion.Major && fullVersion.Minor < requiredVersion.Minor))
            {
                throw new Exception($"We detected that the server is running on version {serverVersion.FullVersion}. RavenDB persistence requires RavenDB server 5.2 or higher");
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
        /// Enables Cluster-wide transactions support. Cluster-wide transactions must
        /// be enabled when running against a cluster with more than one node.
        /// </summary>
        public bool EnableClusterWideTransactions { get; set; }

        ReadOnlySettings settings;
        readonly Func<IBuilder, ReadOnlySettings, IDocumentStore> documentStoreFactory;
    }
}
