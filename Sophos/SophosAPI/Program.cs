using System;
using System.Threading.Tasks;
using System.Configuration;
using RestSharp;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace SophosAPIClient
{ 
    class Program
    {

        public static async Task<string> GetToken()
		{
            var token = "";
            try
            {
                var client = new RestClient(new RestClientOptions("https://id.sophos.com/api/v2/oauth2/token"));
                var request = new RestRequest();
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("grant_type", "client_credentials");
                request.AddParameter("client_id", ConfigurationManager.AppSettings["ClientID"]);
                request.AddParameter("client_secret", ConfigurationManager.AppSettings["ClientSecret"]);
                request.AddParameter("scope", "token");
                var result = await client.PostAsync(request);
                if (!result.IsSuccessful || result.Content == null)
                {
                    Console.WriteLine("Error retrieving posting Client credentials");
                    return "";
                }

                var jsonData = JObject.Parse(result.Content);
                var message = jsonData["message"]?.ToString();
                if (!message.Equals("OK"))
                {
                    Console.WriteLine("Failed to retrieve OAuth2 token");
                    return "";
                }
                token = jsonData["access_token"]?.ToString(); //Given for one hour
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return token;
        }

        public static async Task<List<Tuple<string,string>>> GetTenants(RestClient client1,  string token, string orgID)
        {
            var client = new RestClient(new RestClientOptions("https://api.central.sophos.com"));
            var tenantUrlList = new List<Tuple<string, string>>();
            var pageParam = "pageTotal";
            try
            {
                var request = new RestRequest();
                request.AddHeader("Authorization", $"Bearer {token}");
                request.AddHeader("X-Organization-ID", orgID);
                request.Resource = "organization/v1/tenants";
                request.AddParameter(pageParam, "true");
                do
                {
                    var result = await client.GetAsync(request);
                    var jsonData = JObject.Parse(result.Content);

                    var current = (int)jsonData["pages"]?["current"];
                    var total = (int)jsonData["pages"]?["total"];


                    var tenants = jsonData["items"] as JArray;
                    foreach (var tenant in tenants)
                    {
                        var tenantId = tenant["id"]?.ToString();
                        var baseUrl = tenant["apiHost"]?.ToString();
                        tenantUrlList.Add(new Tuple<string,string>(tenantId, baseUrl));
                    }
                    if (current == total)
					{
                        break;
					}
                    current += 1;
                    request.Parameters.RemoveParameter(pageParam);
                    pageParam = "page";
                    request.AddParameter("page", current.ToString());
                } while (true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return tenantUrlList;
        }


        /* This list all the Hosts (NETBIOS) , does not include Virtual Hosts*/
        public static async Task ListDevices(string token, List<Tuple<string, string>> tenants, DateTime timeFrom)
        {
            foreach (var tenant in tenants)
			{
				try
				{
                    var client = new RestClient(new RestClientOptions(tenant.Item2));
                    var request = new RestRequest();
                    request.AddHeader("Authorization", $"Bearer {token}");
                    request.AddHeader("X-Tenant-ID", tenant.Item1);
                    request.Resource = "endpoint/v1/endpoints";
                    
                    var result = await client.GetAsync(request);
                    if (!result.IsSuccessful || result.Content == null)
                    {
                        Console.WriteLine("Error retrieving data for tenantID: {0}", tenant.Item1);
                        continue;
                    }
                    var jsonData = JObject.Parse(result.Content);
                    var computers = jsonData["items"] as JArray;
                    foreach(var computer in computers)
					{
                        var lastSeenAt = DateTime.Parse(computer["lastSeenAt"]?.ToString());
                        if (lastSeenAt >= timeFrom)
                        {
                            Console.WriteLine("Hostname: {0}, OS: {1}", computer["hostname"]?.ToString(), computer["os"]?["platform"]?.ToString());
                        }
                    }

                }
                catch (Exception e)
				{
                    Console.WriteLine(e.Message);
                }
			}
        }

        public static async Task Main(string[] args)
        {
            var baseUrl = "https://api.central.sophos.com";
            try
            {
                /* To access the temp token */
                var token = await GetToken();
                if (string.IsNullOrEmpty(token))
				{
                    return;
				}

                /* To access the ID of the organization that correlates to the token */
                var client = new RestClient(new RestClientOptions(baseUrl));
               
                var request = new RestRequest();
                request.AddHeader("Authorization", $"Bearer {token}");
                request.Resource = "whoami/v1";
                var result = await client.GetAsync(request);
                var jsonData = JObject.Parse(result.Content);
                var orgID = jsonData["id"]?.ToString();
                var orgType = jsonData["idType"]?.ToString();

                List<Tuple<string, string>> tenants = null;
                if (string.Equals(orgType, "organization", StringComparison.OrdinalIgnoreCase))
				{
                    tenants = await GetTenants(client, token, orgID);
				}
				else
				{   //The Org ID is not really an organization , but a tenant with its own baseURL
                    var baseUrlFromJson = jsonData["apiHosts"]?["dataRegion"]?.ToString();
                    if (!string.IsNullOrEmpty(baseUrlFromJson))
					{
                        baseUrl = baseUrlFromJson;

                    }
                    tenants = new List<Tuple<string, string>>();
                    tenants.Add(new Tuple<string, string>(orgID, baseUrl));
				}
                /* Will print only devices that were active during the past 2 weeks */
                DateTime twoWeeksAgo = DateTime.UtcNow.AddDays(-14);
                Console.WriteLine("----------------------------Devices---------------------");
                await ListDevices(token, tenants, twoWeeksAgo);
                Console.WriteLine("------------------------End--------------");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
