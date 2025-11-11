namespace NServiceBus
{
    using Gateway;
    using Gateway.RavenDB;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Operations.Expiration;
    using Raven.Client.ServerWide.Commands;
    using Settings;
    using System;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using Raven.Client.ServerWide.Operations;

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
            ArgumentNullException.ThrowIfNull(documentStoreFactory);

            this.documentStoreFactory = documentStoreFactory;
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

        /// <inheritdoc />
        protected override void EnableFeature(SettingsHolder settings) => settings.EnableFeature<RavenGatewayDeduplication>();

        readonly Func<IServiceProvider, IReadOnlySettings, IDocumentStore> documentStoreFactory;

        class RavenGatewayDeduplication : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                var configuration = context.Settings.Get<RavenGatewayDeduplicationConfiguration>();

                context.Services.AddSingleton<IGatewayDeduplicationStorage>(sp =>
                {
                    var documentStore = configuration.documentStoreFactory(sp, sp.GetRequiredService<IReadOnlySettings>());

                    EnsureCompatibleServerVersion(documentStore);
                    EnsureClusterConfiguration(documentStore, configuration.EnableClusterWideTransactions);
                    EnableExpirationFeature(documentStore, configuration.FrequencyToRunDeduplicationDataCleanup);

                    return new RavenGatewayDeduplicationStorage(documentStore, configuration.DeduplicationDataTimeToLive, configuration.EnableClusterWideTransactions);
                });
            }

            static void EnableExpirationFeature(IDocumentStore documentStore, long frequencyToRunDeduplicationDataCleanup)
            {
                documentStore.Maintenance.Send(new ConfigureExpirationOperation(new ExpirationConfiguration
                {
                    Disabled = false,
                    DeleteFrequencyInSec = frequencyToRunDeduplicationDataCleanup
                }));
            }

            static void EnsureClusterConfiguration(IDocumentStore store, bool clusterWideTransactionsEnabled)
            {
                using var s = store.OpenSession();
                var getTopologyCmd = new GetDatabaseTopologyCommand();
                s.Advanced.RequestExecutor.Execute(getTopologyCmd, s.Advanced.Context);

                if (getTopologyCmd.Result.Nodes.Count > 1 && !clusterWideTransactionsEnabled)
                {
                    throw new InvalidOperationException("The RavenDB cluster contains multiple nodes. To safely operate in multi-node environments, enable cluster-wide transactions.");
                }
            }

            static void EnsureCompatibleServerVersion(IDocumentStore documentStore)
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
        }
    }
}