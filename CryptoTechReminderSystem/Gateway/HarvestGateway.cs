using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CryptoTechReminderSystem.DomainObject;
using Newtonsoft.Json.Linq;

namespace CryptoTechReminderSystem.Gateway
{
    public class HarvestGateway : IHarvestDeveloperRetriever, ITimeSheetRetriever
    {
        private readonly HttpClient _client;
        private readonly string[] _developerRoles;
       
        private static string ToHarvestApiString(DateTimeOffset date)
        {
            return date.ToString("yyyy-MM-dd");
        }
        
        private static string[] CreateRoleArray(string roles)
        {
            return roles.Split(',').Select(role => role.Trim()).ToArray();
        }
        
        public HarvestGateway(string address, string token, string accountId, string userAgent, string roles)
        {
            _developerRoles = CreateRoleArray(roles);
            _client = new HttpClient { BaseAddress = new Uri(address) };
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Add("Harvest-Account-Id",accountId);
            _client.DefaultRequestHeaders.Add("User-Agent",userAgent);
        }
        
        private async Task<JObject> GetApiResponse(string address)
        {
            var response = await _client.GetAsync(address);
            return JObject.Parse(await response.Content.ReadAsStringAsync());
        }

        public IList<HarvestDeveloper> RetrieveDevelopers()
        {
            var response = GetApiResponse("/api/v2/users");
            
            response.Wait();
            
            var apiResponse = response.Result;
            var users = apiResponse["users"];

            var activeDevelopers = users.Where(user => (bool)user["is_active"] && IsDeveloper(user));
            return activeDevelopers.Select(developer => new HarvestDeveloper
                {
                    Id = (int) developer["id"],
                    FirstName = developer["first_name"].ToString(),
                    LastName = developer["last_name"].ToString(),
                    Email = developer["email"].ToString()
                }
            ).ToList();
        }
        
        private bool IsDeveloper(JToken user)
        {
            return user["roles"].ToArray().Any(role => _developerRoles.Contains(role.ToString()));
        }

        public IList<TimeSheet> RetrieveTimeSheets(DateTimeOffset dateFrom, DateTimeOffset dateTo)
        {
            var address = $"/api/v2/time_entries?from={ToHarvestApiString(dateFrom)}&to={ToHarvestApiString(dateTo)}";
            
            var response = GetApiResponse(address);
            
            response.Wait();
            
            var apiResponse = response.Result;
            var timeSheets = apiResponse["time_entries"];
            return timeSheets.Select(timeSheet => new TimeSheet
                {
                    Id = (int)timeSheet["id"],
                    TimeSheetDate = timeSheet["spent_date"].ToString(),
                    UserId = (int)timeSheet["user"]["id"],
                    Hours = (float)timeSheet["hours"]
                }
            ).ToList(); 
        }
    }
}
