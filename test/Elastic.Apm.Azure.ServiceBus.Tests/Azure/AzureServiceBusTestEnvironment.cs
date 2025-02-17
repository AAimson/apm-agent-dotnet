// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using Azure.Messaging.ServiceBus;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.Azure;
using Elastic.Apm.Tests.Utilities.Terraform;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Azure.ServiceBus.Tests.Azure
{
	[CollectionDefinition("AzureServiceBus")]
	public class AzureServiceBusTestEnvironmentCollection : ICollectionFixture<AzureServiceBusTestEnvironment>
	{
	}

	/// <summary>
	/// A test environment for Azure Service Bus that deploys and configures an Azure Service Bus namespace
	/// in a given region and location
	/// </summary>
	/// <remarks>
	/// Resource name rules
	/// https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules
	/// </remarks>
	public class AzureServiceBusTestEnvironment : IDisposable
	{
		private readonly TerraformResources _terraform;
		private readonly Dictionary<string, string> _variables;

		public AzureServiceBusTestEnvironment(IMessageSink messageSink)
		{
			var solutionRoot = SolutionPaths.Root;
			var terraformResourceDirectory = Path.Combine(solutionRoot, "build", "terraform", "azure", "service_bus");
			var credentials = AzureCredentials.Instance;

			// don't try to run terraform if not authenticated.
			if (credentials is Unauthenticated)
				return;

			_terraform = new TerraformResources(terraformResourceDirectory, credentials, messageSink);

			var resourceGroupName = AzureResources.CreateResourceGroupName("service-bus-test");
			_variables = new Dictionary<string, string>
			{
				["resource_group"] = resourceGroupName,
				["servicebus_namespace"] = "dotnet-" + Guid.NewGuid()
			};

			_terraform.Init();
			_terraform.Apply(_variables);

			ServiceBusConnectionString = _terraform.Output("connection_string");
			ServiceBusConnectionStringProperties = ServiceBusConnectionStringProperties.Parse(ServiceBusConnectionString);
		}

		public string ServiceBusConnectionString { get; }

		public ServiceBusConnectionStringProperties ServiceBusConnectionStringProperties { get; }

		public void Dispose() => _terraform?.Destroy(_variables);
	}
}
