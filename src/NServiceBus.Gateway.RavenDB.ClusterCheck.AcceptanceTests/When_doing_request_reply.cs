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
        public void Is_should_throw()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                var context = await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>()
                    .Run();
            });
        }

        public class Context : ScenarioContext{ }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<GatewayEndpoint>();
            }
        }
    }
}