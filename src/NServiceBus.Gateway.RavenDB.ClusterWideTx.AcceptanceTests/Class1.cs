namespace NServiceBus.Gateway.AcceptanceTests
{
    using AcceptanceTesting.Support;
    using System;
    using System.Threading.Tasks;

    public class GatewayEndpoint : GatewayEndpointWithNoStorage
    {
        public override Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            return base.GetConfiguration(runDescriptor, endpointCustomizationConfiguration, configuration =>
            {
                var current = new GatewayTestSuiteConstraints();

                current.ConfigureDeduplicationStorage(
                    endpointCustomizationConfiguration.CustomEndpointName,
                    configuration,
                    runDescriptor.Settings)
                    .GetAwaiter().GetResult();

                runDescriptor.OnTestCompleted(_ => current.Cleanup());

                configurationBuilderCustomization(configuration);
            });
        }
    }
}