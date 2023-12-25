using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Configuration;
using RestSharp;
using RestSharp.Authenticators;
using System.Xml;

namespace QualysAPIClient
{ 

    class Program
    {   
        /* This list all the Hosts (NETBIOS) , does not include Virtual Hosts*/
        public static async Task ListDevices(RestClient client)
		{
            try
            {
                var request = new RestRequest();
                request.AddHeader("X-Requested-With", "RestSharp");
                request.Resource = "api/2.0/fo/asset/host/";
                request.AddParameter("action", "list");
                request.AddParameter("truncation_limit",0);

                var result = await client.PostAsync(request);
                if (!result.IsSuccessful)
                {
                    Console.WriteLine("Response is not Successful");
                    return;
                }
				
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(result.Content);

                /*Handling an Error for the API response - according to documentation of v2 api the error message will appear in the Text tag under RESPONSE*/
                XmlNodeList errorText = xml.SelectNodes("/HOST_LIST_OUTPUT/RESPONSE/TEXT");
                if (errorText.Count > 0)
				{
                    Console.WriteLine("Name: {0}", errorText[0].InnerText);
                    return;
				}
                //printing all the hosts
                XmlNodeList xnList = xml.GetElementsByTagName("HOST");    
                foreach (XmlNode xn in xnList)
                {
                    Console.WriteLine("Hostname: {0}, OS: {1}, IP: {2}", xn["NETBIOS"].InnerText, xn["OS"].InnerText, xn["IP"].InnerText);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        // Critical (90-100),  High (70-89), Medium (40-69), Low (1-39)
        public static async Task ListVulnerableDevices(RestClient client, int qds_min)
        {
            try
            {
                var request = new RestRequest();
                request.AddHeader("X-Requested-With", "RestSharp");
                request.Resource = "api/2.0/fo/asset/host/vm/detection/";
                request.AddParameter("action", "list");
                request.AddParameter("truncation_limit", 0);
                request.AddParameter("show_qds", 1);
                request.AddParameter("qds_min", qds_min);

                var result = await client.PostAsync(request);
                if (!result.IsSuccessful)
                {
                    Console.WriteLine("Response is not Successful");
                    return;
                }
				
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(result.Content);

                /*Handling an Error for the API response - according to documentation of v2 api the error message will appear in the Text tag under RESPONSE*/
                XmlNodeList errorText = xml.SelectNodes("/HOST_LIST_OUTPUT/RESPONSE/TEXT");
                if (errorText.Count > 0)
                {
                    Console.WriteLine("Name: {0}", errorText[0].InnerText);
                    return;
                }
                //printing all the hosts
                XmlNodeList xnList = xml.DocumentElement.SelectNodes("//HOST");
                foreach (XmlNode xn in xnList)
                {
                    Console.WriteLine("Name: {0}, {1}, {2}", xn["NETBIOS"].InnerText, xn["OS"].InnerText, xn["IP"].InnerText);
                    XmlNodeList dnodes = xn.SelectNodes("//DETECTION");
                    foreach (XmlNode node in dnodes)
					{
                        var qds = int.Parse(node["QDS"].InnerText);
                        if (qds >= qds_min)
                        {

                            Console.WriteLine("QDS: {0}, STATUS: {1}, DESC: {2}", node["QDS"].InnerText, node["STATUS"].InnerText, node["RESULTS"].InnerText);
                        }
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
            var options = new RestClientOptions(ConfigurationManager.AppSettings["server"]);
            options.Authenticator = new HttpBasicAuthenticator(ConfigurationManager.AppSettings["username"], ConfigurationManager.AppSettings["password"]);
            try
            {
                var client = new RestClient(options);
                Console.WriteLine("----------------------------Devices---------------------");
                await ListDevices(client);
                Console.WriteLine("------------------------Vulnerable Devices--------------");
                // Critical (90-100),  High (70-89), Medium (40-69), Low (1-39)
                await ListVulnerableDevices(client,70);
                Console.WriteLine("------------------------End--------------");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
