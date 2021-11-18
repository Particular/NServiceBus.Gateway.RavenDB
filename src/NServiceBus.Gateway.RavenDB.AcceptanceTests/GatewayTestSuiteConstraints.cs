namespace NServiceBus.Gateway.AcceptanceTests
{
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using Raven.Client.Documents;
    using Raven.Client.ServerWide;
    using Raven.Client.ServerWide.Operations;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Threading;

    public partial class GatewayTestSuiteConstraints
    {
        public Task<GatewayDeduplicationConfiguration> ConfigureDeduplicationStorage(string endpointName, EndpointConfiguration configuration, RunSettings settings)
        {
            var ravenGatewayDeduplicationConfiguration = new RavenGatewayDeduplicationConfiguration((builder, _) =>
            {
                var databaseName = Guid.NewGuid().ToString();
                var documentStore = GetInitializedDocumentStore(databaseName);

                documentStore.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(databaseName)));

                try
                {
                    semaphoreSlim.Wait();
                    databases.Add(databaseName);
                }
                finally
                {
                    semaphoreSlim.Release();
                }

                return documentStore;
            });

            var gatewaySettings = configuration.Gateway(ravenGatewayDeduplicationConfiguration);
            configuration.GetSettings().Set(gatewaySettings);

            return Task.FromResult<GatewayDeduplicationConfiguration>(ravenGatewayDeduplicationConfiguration);
        }

        public async Task Cleanup()
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                foreach (var databaseName in databases)
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
                                storeForDeletion.Maintenance.Server.Send(new DeleteDatabasesOperation(storeForDeletion.Database, hardDelete: true));
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

                databases.Clear();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        static DocumentStore GetInitializedDocumentStore(string defaultDatabase)
        {
            var url = Environment.GetEnvironmentVariable("RavenSingleNodeUrl");
            if (url == null)
            {
                throw new Exception("RavenDB URL must be specified in an environment variable named RavenSingleNodeUrl.");
            }

            var documentStore = new DocumentStore
            {
                Urls = new[] { url },
                Database = defaultDatabase
            };

            documentStore.Initialize();

            return documentStore;
        }

        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        List<string> databases = new List<string>();
    }
}