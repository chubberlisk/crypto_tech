using Newtonsoft.Json;

namespace CryptoTechReminderSystem.DomainObject
{
    public class Developer
    {
        [JsonProperty("first_name")] 
        public string FirstName { get; set; }
        [JsonProperty("last_name")] 
        public string LastName { get; set; }
        [JsonProperty("email")] 
        public string Email { get; set; }
        [JsonProperty("hours")]
        public int Hours { get; set; }
    }
}