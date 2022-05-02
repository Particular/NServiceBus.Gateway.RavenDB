namespace NServiceBus.Gateway.AcceptanceTests
{
    using System;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using NUnit.Framework;

    public class When_running_against_a_cluster : NServiceBusAcceptanceTest
    {
        [Test]
        public void It_should_throw_when_database_is_replicated()
        {
            Assert.ThrowsAsync<Exception>(async () =>
            {
                var context = await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithThreeNodesReplica>()
                    .Run();
            });
        }

        [Test]
        public void It_should_not_throw_when_database_is_not_replicated()
        {
            Assert.DoesNotThrowAsync(async () =>
            {
                var context = await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithOneNodeReplica>()
                    .Run();
            });
        }

        public class Context : ScenarioContext { }

        public class EndpointWithThreeNodesReplica : EndpointConfigurationBuilder
        {
            public EndpointWithThreeNodesReplica()
            {
                EndpointSetup<GatewayEndpointWithThreeNodesReplica>((c, runDescriptor) =>
                {
                    var gatewaySettings = c.GetSettings().Get<GatewaySettings>();
                    gatewaySettings.AddReceiveChannel("http://localhost:25999/SiteA/");
                });
            }
        }

        public class EndpointWithOneNodeReplica : EndpointConfigurationBuilder
        {
            public EndpointWithOneNodeReplica()
            {
                EndpointSetup<GatewayEndpointWithOneNodeReplica>((c, runDescriptor) =>
                {
                    var gatewaySettings = c.GetSettings().Get<GatewaySettings>();
                    gatewaySettings.AddReceiveChannel("http://localhost:25999/SiteA/");
                });
            }
        }
    }
}