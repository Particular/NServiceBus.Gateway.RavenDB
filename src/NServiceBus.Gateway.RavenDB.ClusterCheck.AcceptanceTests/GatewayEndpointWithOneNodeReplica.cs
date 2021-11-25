﻿namespace NServiceBus.Gateway.AcceptanceTests
{
    using Raven.Client.ServerWide;
    using System.Collections.Generic;

    public class GatewayEndpointWithOneNodeReplica : GatewayEndpoint
    {
        protected override DatabaseTopology GetDatabaseTopology()
        {
            return new DatabaseTopology()
            {
                Members = new List<string> { "A" }
            };
        }
    }
}
