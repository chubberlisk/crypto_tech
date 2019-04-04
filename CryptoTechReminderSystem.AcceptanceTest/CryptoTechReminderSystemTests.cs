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
        private FluentSimulator _fluentSimulator;
        private IMessageSender _slackGateway;
        private RemindDeveloper _remindDeveloper;
        
        public class SlackPostMessageResponse
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

            _fluentSimulator.Post("/api/chat.postMessage").Responds(slackPostMessageResponse);
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
            var receivedRequest = _fluentSimulator.ReceivedRequests.First();

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
            _fluentSimulator = new FluentSimulator(
                "http://localhost:8009/"
            );
            _slackGateway = new SlackGateway(
                "http://localhost:8009/",
                "xxxx-xxxxxxxxx-xxxx"
            );
            _remindDeveloper = new RemindDeveloper(_slackGateway);
            _fluentSimulator.Start();
        }

        [TearDown]
        public void TearDown()
        {
            _fluentSimulator.Stop();
        }

        [Test]
        public void CanRemindAUser()
        {
            GivenSlackRespondsWithOk();

            WhenWeRemindUser("U172L982", "Please make sure your timesheet is submitted by 13:30 on Friday.");

            ThenMessageHasBeenPostedToSlack("U172L982");
        }

        [Test]
        public void CanGetDevelopersFromHarvest()
        {
            var harvestGetUsersResponse = File.ReadAllText(
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "../../../HarvestUsersExampleResponse.json"
                )
            );
            
            var getDevelopers = new GetLateDevelopers(new HarvestGateway(
                "http://localhost:8009/",
                "xxxx-xxxxxxxxx-xxxx"
            ));
            
            _fluentSimulator.Get("/api/v2/users").Responds(harvestGetUsersResponse);

            var response = getDevelopers.Execute();
            
            var receivedRequest = _fluentSimulator.ReceivedRequests.First();

            receivedRequest.Url.Should().Be(
                "http://localhost:8009/api/v2/users"
            );
            receivedRequest.Headers["Authorization"].Should().Be(
                "Bearer xxxx-xxxxxxxxx-xxxx"
            );

            response.First().Id.Should().Be(1782974);
            response.First().FirstName.Should().Be("Robert");
            response.First().LastName.Should().Be("Martin");
            response.First().Email.Should().Be("robert.martin@cleancoders.com");
        }
    }
}