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
				//Console.WriteLine(result);

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
			var tenantId = ConfigurationManager.AppSettings["tenantId"];
			var clientId = ConfigurationManager.AppSettings["clientId"];
			var secret = ConfigurationManager.AppSettings["appSecret"];
			var ClientSecretCredential = new ClientSecretCredential(
				tenantId, clientId, secret);
			var graphClient = new GraphServiceClient(ClientSecretCredential, scopes);
			await ListRegisteredDevices(graphClient);
		}
	}
}
