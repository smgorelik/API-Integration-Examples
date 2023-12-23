using System;
using System.Threading.Tasks;
using Microsoft.Graph;
using Azure.Identity;
using System.Configuration;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Graph.Models;
using System.Collections.Generic;

namespace GraphClient
{
	class Program
	{
		// ListRegisteredApps - requires Application.Read.All permissions
		public static async Task CheckTokenExpiry(GraphServiceClient graphClient)
		{
			try
			{
				var apps = await graphClient.Applications.GetAsync((requestConfiguration) =>
				{
					/*https://learn.microsoft.com/en-us/graph/aad-advanced-queries?tabs=http#device-properties
					  https://learn.microsoft.com/en-us/graph/api/resources/device?view=graph-rest-1.0*/

					//Not including  - Workplace (indicates bring your own personal devices)
					requestConfiguration.QueryParameters.Filter = $"appId eq '{ConfigurationManager.AppSettings["clientId"]}'";
					requestConfiguration.QueryParameters.Top = 1;
					requestConfiguration.QueryParameters.Select = new string[] { "appId", "DisplayName", "CreatedDateTime","PasswordCredentials"};
				});
				
				if (apps.Value.Count > 0)
				{
					var app = apps.Value[0];
					Console.WriteLine("{0,-30} {1,10}", app.DisplayName, app.AppId);
					var creds = app.PasswordCredentials;
					var token = creds.Find(cred => cred.KeyId == new Guid(ConfigurationManager.AppSettings["SecretId"]));
					Console.WriteLine("{0,-30} {1,10}", "Remaining days before token expires", (token.EndDateTime - DateTimeOffset.UtcNow).Value.Days);
				}
				Console.WriteLine("-----------------------------------------");
			}
			catch (ODataError odataError)
			{
				Console.WriteLine(odataError.Error.Message);
			}
		}

		// ListRegisteredDevices - requires Device.Read.All permissions	
		public static async Task ListRegisteredDevices(GraphServiceClient graphClient)
		{
			var devices = new List<Device>();
			try
			{
				var result = await graphClient.Devices.GetAsync((requestConfiguration) =>
				{
					/*https://learn.microsoft.com/en-us/graph/aad-advanced-queries?tabs=http#device-properties
					  https://learn.microsoft.com/en-us/graph/api/resources/device?view=graph-rest-1.0*/

					//Not including  - Workplace (indicates bring your own personal devices)
					requestConfiguration.QueryParameters.Filter = "TrustType eq 'AzureAd' or TrustType eq 'ServerAd'";
					requestConfiguration.QueryParameters.Select = new string[] { "id", "DisplayName", "OperatingSystem", "ProfileType", "TrustType" };
				});

				var pageIterator = PageIterator<Device, DeviceCollectionResponse>.CreatePageIterator(graphClient,result,
						(device) =>
						{
							devices.Add(device);						
							return true;
						});

				await pageIterator.IterateAsync();

				Console.WriteLine("Computers list number: " + devices.Count);
				Console.WriteLine("{0,-15} {1,-10} {2,-10}", "DisplayName", "OperatingSystem", "Type");
				foreach (Device dv in devices)
				{
					Console.WriteLine("{0,-20} {1,10} {2,-10}", dv.DisplayName, dv.OperatingSystem, dv.TrustType);
				}
				/*
				 * You can do what ever you want with this devices now
				 */
				Console.WriteLine("-----------------------------------------");
			}
			catch (ODataError odataError)
			{
				Console.WriteLine(odataError.Error.Message);
			}
		}
		public static async Task Main(string[] args)
		{
			string[] scopes = { "https://graph.microsoft.com/.default" };
			var ClientSecretCredential = new ClientSecretCredential(
				ConfigurationManager.AppSettings["tenantId"], ConfigurationManager.AppSettings["clientId"], ConfigurationManager.AppSettings["SecretValue"]);
			var graphClient = new GraphServiceClient(ClientSecretCredential, scopes);
			await CheckTokenExpiry(graphClient);
			await ListRegisteredDevices(graphClient);
		}
	}
}
