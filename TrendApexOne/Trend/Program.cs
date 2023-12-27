using System;
using System.Threading.Tasks;
using System.Configuration;
using RestSharp;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Trend
{ 
    class Program
    {
        static string CreateChecksum(string httpMethod, string rawUrl, string headers, string requestBody)
        {
            string stringToHash = httpMethod.ToUpper() + "|" + rawUrl.ToLower() + "|" + headers + "|" + requestBody;
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));
                return Convert.ToBase64String(bytes);
            }
        }

        static string CreateJwtToken(string applicationId, string apiKey, string httpMethod, string rawUrl, string headers, string requestBody, DateTime? issuedAt = null, string algorithm = "HS256", string version = "V1")
        {
            if (issuedAt == null)
            {
                issuedAt = DateTime.UtcNow;
            }

            var payload = new JwtPayload
        {
            { "appid", applicationId },
            { "iat", new DateTimeOffset(issuedAt.Value).ToUnixTimeSeconds() },
            { "version", version },
            { "checksum", CreateChecksum(httpMethod, rawUrl, headers, requestBody) }
        };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(apiKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(credentials);

            var token = new JwtSecurityToken(header, payload);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static async Task Main(string[] args)
        {
            var productAgentAPIPath = "/WebApp/API/v2/AgentResource/ProductAgents";
            try
            {
                var client = new RestClient(new RestClientOptions(ConfigurationManager.AppSettings["SERVER_HOST"]));
               
                var request = new RestRequest();
                var token = CreateJwtToken(ConfigurationManager.AppSettings["APPLICATION_ID"], ConfigurationManager.AppSettings["API_KEY"], "GET", productAgentAPIPath, "", "");
                request.AddHeader("Authorization", $"Bearer {token}");
                request.Resource = productAgentAPIPath;

                var result = await client.GetAsync(request);
                var jsonData = JObject.Parse(result.Content);
                var computers = jsonData["result_content"] as JArray;
                foreach (var computer in computers)
                {
                    Console.WriteLine("Hostname: {0}, OS: {1}, IP: {2}", computer["endpointHost"]?.ToString(), computer["platform"]?.ToString(), computer["endpointIP"]?.ToString());
                }
                Console.WriteLine("------------------------End--------------");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
