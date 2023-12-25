using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Configuration;
using RestSharp;
using RestSharp.Authenticators;
using System.Xml;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace SentinelOne
{ 

    class Program
    {
        public static int MAX_PAGE = 1000;
        public class Application
        {
            public string ApplicationName { get; set; }
            public string VendorName { get; set; }
            public string Version { get; set; }
            public DateTime InstalledDate { get; set; }
        }

        /* This list all the Hosts*/
        public static async Task ListDevices(RestClient client)
		{ 
            try
            {
                string nextCursor = null;
                do
                {
                    var request = new RestRequest();
                    request.AddHeader("Authorization", $"ApiToken {ConfigurationManager.AppSettings["ApiToken"]}");
                    request.Resource = "web/api/v2.1/agents";
                    request.AddParameter("limit", MAX_PAGE);

                    if (!string.IsNullOrEmpty(nextCursor))
                    {
                        request.AddParameter("cursor", nextCursor);
                    }

                    var result = await client.GetAsync(request);

                    if (!result.IsSuccessful || result.Content == null)
                    {
                        Console.WriteLine("Error retrieving data or end of data reached");
                        return;
                    }

                    var jsonData = JObject.Parse(result.Content);
                    var computers = jsonData["data"] as JArray;

                    if (computers != null)
                    {
                        foreach (var computer in computers)
                        {
                            // Extract and print the 'computerName'
                            var computerName = computer["computerName"]?.ToString();
                            var localip = computer["lastIpToMgmt"]?.ToString();
                            var osType = computer["osType"]?.ToString();
                            Console.WriteLine("Hostname: {0}, OS: {1}, IP: {2}", computerName, osType, localip);
                        }
                    }
                    nextCursor = jsonData["pagination"]?["nextCursor"]?.ToString();
                } while (!string.IsNullOrEmpty(nextCursor));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /* Prints all applications per hostname
         * :parameter - osTypePrefix - if the parameter is empty string, we print all applications for all the devices otherwise only print the applications for the given OS
         */
        public static async Task ListApplicationsPerDevice(RestClient client,string osTypePrefix = "mac")
        {
            try
            {
                var compToAppDict = new Dictionary<string, List<Application>>();
                string nextCursor = null;
                do
                {
                    var request = new RestRequest();
                    request.AddHeader("Authorization", $"ApiToken {ConfigurationManager.AppSettings["ApiToken"]}");
                    request.Resource = "web/api/v2.1/installed-applications";
                    request.AddParameter("limit", MAX_PAGE);

                    if (!string.IsNullOrEmpty(nextCursor))
                    {
                        request.AddParameter("cursor", nextCursor);
                    }

                    var result = await client.GetAsync(request);

                    if (!result.IsSuccessful || result.Content == null)
                    {
                        Console.WriteLine("Error retrieving data or end of data reached");
                        return;
                    }

                    var jsonData = JObject.Parse(result.Content);
                    var data = jsonData["data"] as JArray;

                    if (data != null)
                    {
                        foreach (var appJson in data)
                        {
                            var computerName = appJson["agentComputerName"]?.ToString();
                            var osType = appJson["osType"]?.ToString();
                            string key = $"{osType}-{computerName}";

                            if (!compToAppDict.ContainsKey(key))
                            {
                                compToAppDict[key] = new List<Application>();
                            }

                            var app = new Application
                            {
                                ApplicationName = appJson["name"]?.ToString(),
                                VendorName = appJson["publisher"]?.ToString(),
                                Version = appJson["version"]?.ToString(),
                                InstalledDate = DateTime.Parse(appJson["installedAt"]?.ToString())
                            };
                            compToAppDict[key].Add(app);
                        }
                    }
                    nextCursor = jsonData["pagination"]?["nextCursor"]?.ToString();

                } while (!string.IsNullOrEmpty(nextCursor));
            
                foreach (string os_comp in compToAppDict.Keys)
				{
                    if (!string.IsNullOrEmpty(osTypePrefix) && !os_comp.StartsWith(osTypePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
					}
                    int indexOfHyphen = os_comp.IndexOf('-');
                    string comp_name = os_comp.Substring(indexOfHyphen + 1);
                    Console.WriteLine("##### Printing Applications for Hostname: {0}", comp_name);
                    Console.WriteLine("{0, -20},{1,-15},{2,-15},{3, -15}", "ApplicationName", "VendorName", "Version", "InstalledDate");
                    foreach (var app in compToAppDict[os_comp])
					{
                        Console.WriteLine("{0, -20},{1,-15},{2,-15},{3, -15}", app.ApplicationName, app.VendorName, app.Version,app.InstalledDate.ToString());
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static async Task Main(string[] args)
        {
            var options = new RestClientOptions(ConfigurationManager.AppSettings["ManagementURL"]);
            try
            {
                var client = new RestClient(options);
                Console.WriteLine("----------------------------Devices---------------------");
                await ListDevices(client);
                await ListApplicationsPerDevice(client);
                Console.WriteLine("------------------------End--------------");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
