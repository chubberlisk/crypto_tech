using System.Collections.Generic;
using System.Linq;
using CryptoTechReminderSystem.Boundary;
using CryptoTechReminderSystem.Gateway;

namespace CryptoTechReminderSystem.UseCase
{
    public class GetLateDevelopers
    {
        private readonly ISlackDeveloperRetriever _slackDeveloperRetriever;
        private readonly ITimesheetAndDeveloperRetriever _harvestGateway;

        public GetLateDevelopers(ISlackDeveloperRetriever slackDeveloperRetriever, ITimesheetAndDeveloperRetriever harvestGateway)
        {
            _slackDeveloperRetriever = slackDeveloperRetriever;
            _harvestGateway = harvestGateway;
        }

        public GetLateDevelopersResponse Execute()
        {
            var harvestGetDevelopersResponse = _harvestGateway.RetrieveDevelopers();
            var slackGetDevelopersResponse = _slackDeveloperRetriever.RetrieveDevelopers();
            var harvestGetTimesheetsResponse = _harvestGateway.RetrieveTimeSheets();
            var getLateDevelopersResponse = new GetLateDevelopersResponse
            {
                Developers = new List<string>()
            };
            foreach (var harvestDeveloper in harvestGetDevelopersResponse)
            {
                var timeSheetForDeveloper = harvestGetTimesheetsResponse.Where(sheet => sheet.UserId == harvestDeveloper.Id);
                var sumOfHours = timeSheetForDeveloper.Sum(timeSheet => timeSheet.Hours);
                if (sumOfHours < 35)
                {
                    var slackLateDeveloper = slackGetDevelopersResponse.Single(developer => developer.Email == harvestDeveloper.Email);
                    getLateDevelopersResponse.Developers.Add(slackLateDeveloper.Id);
                }
            }
            return getLateDevelopersResponse;
        }
    }
}