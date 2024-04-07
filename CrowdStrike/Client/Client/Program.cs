using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Configuration;
using RestSharp;
using RestSharp.Authenticators;
using System.Xml;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace CSAPIClient
{
    class Program
    {

        public static async Task<string> GetToken()
        {
            var token = "";
            try
            {
                var client = new RestClient(new RestClientOptions(ConfigurationManager.AppSettings["BaseURL"] + "/oauth2/token"));
                var request = new RestRequest();
                request.AddHeader("accept", "application/json");
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("client_id", ConfigurationManager.AppSettings["ClientID"]);
                request.AddParameter("client_secret", ConfigurationManager.AppSettings["ClientSecret"]);

                var result = await client.PostAsync(request);
                if (!result.IsSuccessful || result.Content == null)
                {
                    Console.WriteLine("Error retrieving posting Client credentials");
                    return "";
                }

                var jsonData = JObject.Parse(result.Content);
                token = jsonData["access_token"]?.ToString(); //Given for one hour
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return token;
        }


        public static async Task<List<string>> QueryAPI(string token, string api)
        {
            List<string> ids = new List<string>();
            int offset = 0;
            int limit = 100;
            try
            {
                while (true)
                {
                    var client = new RestClient(new RestClientOptions(ConfigurationManager.AppSettings["BaseURL"]));
                    var request = new RestRequest();
                    request.AddHeader("Authorization", $"Bearer {token}");
                    request.AddHeader("Accept", "application/json");
                    request.AddHeader("limit", limit);
                    request.AddHeader("offset", offset);
                    request.Resource = api;

                    var result = await client.GetAsync(request);
                    if (!result.IsSuccessful || result.Content == null)
                    {
                        Console.WriteLine("Error retrieving data for Query API: {0}", api);
                        return null;
                    }
                    offset += limit;
                    var jsonData = JObject.Parse(result.Content);                  
                    var resources = jsonData["resources"] as JArray;
                    foreach (var resource in resources)
                    {
                        ids.Add(resource?.ToString());
                    }
                    var total = jsonData["meta"]?["pagination"]?["total"]?.ToObject<int>();
                    if (offset >= total)
                        return ids;
                    System.Threading.Thread.Sleep(1000);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }      
        }

        
        public static async Task PrintDeviceInfo(string token, List<string> ids, DateTime timeFrom, string api)
        {
            try
            {
                var client = new RestClient(new RestClientOptions(ConfigurationManager.AppSettings["BaseURL"]));
                var request = new RestRequest();
                request.AddHeader("Authorization", $"Bearer {token}");
                request.AddHeader("Accept", "application/json");

                var body = new { ids = ids };
                request.AddJsonBody(body);

                request.Resource = api;

                var result = await client.PostAsync(request);
                if (!result.IsSuccessful || result.Content == null)
                {
                    Console.WriteLine("Error retrieving data for Query API: {0}", api);
                    return;
                }

                var jsonData = JObject.Parse(result.Content);
                var resources = jsonData["resources"] as JArray;
                foreach (var resource in resources)
                {
                    var lastSeenAt = DateTime.Parse(resource["last_seen"]?.ToString());
                    if (lastSeenAt >= timeFrom)
                    {
                        Console.WriteLine("Hostname: {0}, OS: {1}, IP: {2}", resource["hostname"]?.ToString(), resource["os_version"]?.ToString(), resource["local_ip"]?.ToString()); 
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
        }


        public static async Task Main(string[] args)
        {
            try
            {
                /* To access the temp token */
                var token = await GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    return;
                }

                DateTime twoWeeksAgo = DateTime.UtcNow.AddDays(-14);
                Console.WriteLine("----------------------------Devices---------------------");
                List<string> device_ids = await QueryAPI(token, "devices/queries/devices/v1");
                List<string> user_ids = await QueryAPI(token, "user-management/queries/users/v1");
                await PrintDeviceInfo(token, device_ids, twoWeeksAgo, "devices/entities/devices/v2");
                Console.WriteLine("------------------------End--------------");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
