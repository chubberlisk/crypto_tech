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
    public class HarvestGateway : IDeveloperRetriever, ITimeSheetRetriever
    {
        private readonly HttpClient _client;
        private string _token;

        public HarvestGateway(string address, string token)
        {
            _client = new HttpClient { BaseAddress = new Uri(address) };
            _token = token;
        }
        
        private async Task<JObject> GetApiResponse(string address)
        {
            var response = await _client.GetAsync(address);
            return JObject.Parse(await response.Content.ReadAsStringAsync());
        }

        public IList<Developer> RetrieveDevelopers()
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
 
            var apiResponse = GetApiResponse("/api/v2/users").Result;
            var users = apiResponse["users"];
            return users.Select(developer => new Developer
                {
                    Id = (int) developer["id"],
                    FirstName = developer["first_name"].ToString(),
                    LastName = developer["last_name"].ToString(),
                    Email = developer["email"].ToString()
                }
            ).ToList(); 
        }

        public IEnumerable<TimeSheet> RetrieveTimeSheets()
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
 
            var apiResponse = GetApiResponse("/api/v2/time_entries").Result;
            var users = apiResponse["time_entries"];
            return users.Select(timeEntry => new TimeSheet
                {
                    Id = (int)timeEntry["id"],
                    TimeSheetDate = timeEntry["spent_date"].ToString(),
                    User = new User
                    {
                        Id = (int)timeEntry["user"]["id"],
                        Name = timeEntry["user"]["name"].ToString()
                    },
                    Hours = (float)timeEntry["hours"],
                    CreatedAt = DateTime.Parse(timeEntry["created_at"].ToString()),
                    UpdatedAt = DateTime.Parse(timeEntry["updated_at"].ToString())
                }
            ).ToList(); 
        }
    }
}