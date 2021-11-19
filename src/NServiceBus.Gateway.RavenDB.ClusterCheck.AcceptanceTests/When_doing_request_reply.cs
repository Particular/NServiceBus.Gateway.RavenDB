namespace NServiceBus.Gateway.AcceptanceTests
{
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using NUnit.Framework;
    using System;
    using System.Threading.Tasks;

    public class When_running_against_a_cluster : NServiceBusAcceptanceTest
    {
        [Test]
        public void Is_should_throw_when_database_is_replicated()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                var context = await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithThreeNodesReplica>()
                    .Run();
            });
        }

        [Test]
        public void Is_should_not_throw_when_database_is_not_replicated()
        {
            Assert.DoesNotThrowAsync(async () =>
            {
                var context = await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithOneNodeReplica>()
                    .Run();
            });
        }

        public class Context : ScenarioContext{ }

        public class EndpointWithThreeNodesReplica : EndpointConfigurationBuilder
        {
            public EndpointWithThreeNodesReplica()
            {
                EndpointSetup<GatewayEndpointWithThreeNodesReplica>();
            }
        }

        public class EndpointWithOneNodeReplica : EndpointConfigurationBuilder
        {
            public EndpointWithOneNodeReplica()
            {
                EndpointSetup<GatewayEndpointWithOneNodeReplica>();
            }
        }
    }
}