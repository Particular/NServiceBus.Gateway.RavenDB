namespace NServiceBus.Gateway.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NUnit.Framework;
    using Raven.Client.Documents;
    using Raven.Client.ServerWide;
    using Raven.Client.ServerWide.Operations;

    public abstract class GatewayEndpoint : IEndpointSetupTemplate
    {
        public Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var endpointConfiguration = new EndpointConfiguration(endpointCustomizationConfiguration.EndpointName);

            endpointConfiguration.TypesToIncludeInScan(endpointCustomizationConfiguration.GetTypesScopedByTestClass());

            endpointConfiguration.Recoverability()
                .Delayed(delayed => delayed.NumberOfRetries(0))
                .Immediate(immediate => immediate.NumberOfRetries(0));

            var storageDir = Path.Combine(NServiceBusAcceptanceTest.StorageRootDir, TestContext.CurrentContext.Test.ID);

            endpointConfiguration.EnableInstallers();

            endpointConfiguration.UseTransport<LearningTransport>()
                .StorageDirectory(storageDir);

            endpointConfiguration.RegisterComponentsAndInheritanceHierarchy(runDescriptor);

            var ravenGatewayDeduplicationConfiguration = new RavenGatewayDeduplicationConfiguration((builder, _) =>
            {
                databaseName = Guid.NewGuid().ToString();
                var documentStore = GetInitializedDocumentStore(databaseName);

                var databaseRecord = new DatabaseRecord(databaseName)
                {
                    Topology = GetDatabaseTopology()
                };

                documentStore.Maintenance.Server.Send(new CreateDatabaseOperation(databaseRecord));

                return documentStore;
            });

            var gatewaySettings = endpointConfiguration.Gateway(ravenGatewayDeduplicationConfiguration);
            endpointConfiguration.GetSettings().Set(gatewaySettings);

            runDescriptor.OnTestCompleted(_ => Cleanup());

            configurationBuilderCustomization(endpointConfiguration);

            return Task.FromResult(endpointConfiguration);
        }

        protected abstract DatabaseTopology GetDatabaseTopology();

        public async Task Cleanup()
        {
            // Periodically the delete will throw an exception because Raven has the database locked
            // To solve this we have a retry loop with a delay
            var triesLeft = 3;

            while (triesLeft-- > 0)
            {
                try
                {
                    // We are using a new store because the global one is disposed of before cleanup
                    using (var storeForDeletion = GetInitializedDocumentStore(databaseName))
                    {
                        await storeForDeletion.Maintenance.Server.SendAsync(new DeleteDatabasesOperation(storeForDeletion.Database, hardDelete: true));
                        break;
                    }
                }
                catch
                {
                    if (triesLeft == 0)
                    {
                        throw;
                    }

                    await Task.Delay(250);
                }
            }

            Console.WriteLine("Deleted '{0}' database", databaseName);
        }

        static DocumentStore GetInitializedDocumentStore(string defaultDatabase)
        {
            var urls = Environment.GetEnvironmentVariable("CommaSeparatedRavenClusterUrls") ?? "http://localhost:8081,http://localhost:8082,http://localhost:8083";

            var documentStore = new DocumentStore
            {
                Urls = urls.Split(','),
                Database = defaultDatabase
            };

            documentStore.Initialize();

            return documentStore;
        }

        string databaseName;
    }
}