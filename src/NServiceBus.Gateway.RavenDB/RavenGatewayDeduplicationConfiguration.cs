namespace NServiceBus
{
    using Gateway;
    using Gateway.RavenDB;
    using ObjectBuilder;
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

            EnsureClusterConfiguration(documentStore);
            EnableExpirationFeature(documentStore, FrequencyToRunDeduplicationDataCleanup);

            return new RavenGatewayDeduplicationStorage(documentStore, DeduplicationDataTimeToLive);
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

                // Currently do not support clusters.
                if (getTopologyCmd.Result.Nodes.Count != 1)
                {
                    throw new InvalidOperationException("RavenDB Persistence does not support database groups with multiple database nodes. Only single-node configurations are supported.");
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

        ReadOnlySettings settings;
        readonly Func<IBuilder, ReadOnlySettings, IDocumentStore> documentStoreFactory;
    }
}
