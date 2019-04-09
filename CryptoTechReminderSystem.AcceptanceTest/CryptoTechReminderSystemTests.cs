using System;
using System.IO;
using NUnit.Framework;
using FluentAssertions;
using System.Linq;
using CryptoTechReminderSystem.Boundary;
using CryptoTechReminderSystem.Gateway;
using CryptoTechReminderSystem.UseCase;
using FluentSim;
using Newtonsoft.Json;
using static Newtonsoft.Json.JsonConvert;

namespace CryptoTechReminderSystem.AcceptanceTest
{
    public class CryptoTechReminderSystemTests
    {
        private FluentSimulator _slackApi;
        private FluentSimulator _harvestApi;
        private IMessageSenderAndRetriever _slackGateway;
        private ITimesheetAndDeveloperRetriever _harvestGateway;
        private RemindDeveloper _remindDeveloper;

        private class ClockStub : IClock
        {
            private readonly DateTimeOffset _currentDateTime;

            public ClockStub(DateTimeOffset dateTime)
            {
                _currentDateTime = dateTime;
            }

            public DateTimeOffset Now()
            {
                return _currentDateTime;
            }
        }

        private class SlackPostMessageResponse
        {
            [JsonProperty("ok")]
            public bool IsOk;
        }
        
        private static PostMessageRequest GetRequest(ReceivedRequest receivedRequest)
        {
            return DeserializeObject<PostMessageRequest>(receivedRequest.RequestBody);
        }

        private void GivenSlackRespondsWithOk()
        {
            var slackPostMessageResponse = new SlackPostMessageResponse
            {
                IsOk = true
            };

            _slackApi.Post("/api/chat.postMessage").Responds(slackPostMessageResponse);
        }

        private void WhenWeRemindUser(string channel, string text)
        {
            _remindDeveloper.Execute(new RemindDeveloperRequest
            {
                Channel = channel,
                Text = text
            });
        }

        private void ThenMessageHasBeenPostedToSlack(string userId)
        {
            var receivedRequest = _slackApi.ReceivedRequests.First();

            receivedRequest.Url.Should().Be(
                "http://localhost:8009/api/chat.postMessage"
            );
            receivedRequest.Headers["Authorization"].Should().Be(
                "Bearer xxxx-xxxxxxxxx-xxxx"
            );
            GetRequest(receivedRequest).Channel.Should().Be(userId);
            GetRequest(receivedRequest).Text.Should().Be(
                "Please make sure your timesheet is submitted by 13:30 on Friday."
            );
        }

        [SetUp]
        public void Setup()
        {
            _slackApi = new FluentSimulator("http://localhost:8009/");
            _slackGateway = new SlackGateway("http://localhost:8009/","xxxx-xxxxxxxxx-xxxx");
            _harvestApi = new FluentSimulator("http://localhost:8010/");
            _harvestGateway = new HarvestGateway("http://localhost:8010/", "xxxx-xxxxxxxxx-xxxx");
            _remindDeveloper = new RemindDeveloper(_slackGateway);
            _slackApi.Start();
            _harvestApi.Start();
        }

        [TearDown]
        public void TearDown()
        {
            _slackApi.Stop();
            _harvestApi.Stop();
        }

        [Ignore("because")]
        public void CanRemindAUser()
        {
            GivenSlackRespondsWithOk();

            WhenWeRemindUser("U172L982", "Please make sure your timesheet is submitted by 13:30 on Friday.");

            ThenMessageHasBeenPostedToSlack("U172L982");
        }

        [Ignore("because")]
        public void CanGetDevelopersFromHarvest()
        {
            var harvestGetUsersResponse = File.ReadAllText(
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "../../../HarvestUsersExampleResponse.json"
                )
            );
            
            var getDevelopers = new GetLateDevelopers(_slackGateway, _harvestGateway);
            
            _harvestApi.Get("/api/v2/users").Responds(harvestGetUsersResponse);

            var response = getDevelopers.Execute();
            
            var receivedRequest = _harvestApi.ReceivedRequests.First();

            receivedRequest.Url.Should().Be("http://localhost:8010/api/v2/users");
            receivedRequest.Headers["Authorization"].Should().Be("Bearer xxxx-xxxxxxxxx-xxxx");
            //response.Developers.First().FirstName.Should().Be("Bruce");
            //response.Developers.First().LastName.Should().Be("Wayne");
            //response.Developers.First().Email.Should().Be("batman@gotham.com");
        }

        [Test]
        public void CanRemindLateDevelopersAtTenThirtyOnFriday()
        {            
            var slackGetUsersResponse = File.ReadAllText(
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "../../../SlackUsersExampleResponse.json"
                )
            );
            
            _slackApi.Get("/api/users.list").Responds(slackGetUsersResponse);
            
            var slackPostMessageResponse = new SlackPostMessageResponse
            {
                IsOk = true
            };

            _slackApi.Post("/api/chat.postMessage").Responds(slackPostMessageResponse);
            
            var harvestGetUsersResponse = File.ReadAllText(
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "../../../HarvestUsersExampleResponse.json"
                )
            );
            
            _harvestApi.Get("/api/v2/users").Responds(harvestGetUsersResponse);
            
            var harvestGetTimeEntriesResponse = File.ReadAllText(
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "../../../HarvestTimeEntriesExampleResponse.json"
                )
            );
            
            _harvestApi.Get("/api/v2/time_entries").Responds(harvestGetTimeEntriesResponse);
            
            var getLateDevelopers = new GetLateDevelopers(_slackGateway, _harvestGateway);
            var clock = new ClockStub(
                new DateTimeOffset(
                    new DateTime(2019, 03, 01, 10, 30, 0)
                )
            );
            
            var remindLateDevelopers = new RemindLateDevelopers(getLateDevelopers, _remindDeveloper, clock);

            remindLateDevelopers.Execute(new RemindLateDevelopersRequest
                {
                    Message = "Please make sure your timesheet is submitted by 13:30 on Friday."
                }
            );

            _slackApi.ReceivedRequests.Count.Should().Be(3);
        }
    }
}