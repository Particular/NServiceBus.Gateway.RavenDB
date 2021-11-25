namespace NServiceBus.Gateway.AcceptanceTests
{
    using Raven.Client.ServerWide;
    using System.Collections.Generic;

    public class GatewayEndpointWithThreeNodesReplica : GatewayEndpoint
    {
        protected override DatabaseTopology GetDatabaseTopology()
        {
            return new DatabaseTopology()
            {
                ReplicationFactor = 3,
                Members = new List<string> { "A", "B", "C" }
            };
        }
    }
}
