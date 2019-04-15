using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using FluentAssertions;
using CryptoTechReminderSystem.Boundary;
using CryptoTechReminderSystem.Gateway;
using CryptoTechReminderSystem.UseCase;
using FluentSim;
using Newtonsoft.Json.Linq;

namespace CryptoTechReminderSystem.AcceptanceTest
{
    public class CryptoTechReminderSystemTests
    {
        private const string SlackApiAddress = "http://localhost:8009/";
        private const string HarvestApiAddress = "http://localhost:8010/";
        private const string SlackApiUsersPath = "api/users.list";
        private const string SlackApiPostMessagePath = "api/chat.postMessage";
        private const string HarvestApiUsersPath = "/api/v2/users";
        private static FluentSimulator _slackApi;
        private static FluentSimulator _harvestApi;
        private static HarvestGateway _harvestGateway;
        private static SlackGateway _slackGateway;
        private static RemindDeveloper _remindDeveloper;
        
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

        private static void HandleSetUp()
        {
            _slackApi = new FluentSimulator(SlackApiAddress);
            _slackGateway = new SlackGateway(SlackApiAddress,"xxxx-xxxxxxxxx-xxxx");
            _harvestApi = new FluentSimulator(HarvestApiAddress);
            _harvestGateway = new HarvestGateway(HarvestApiAddress, "xxxx-xxxxxxxxx-xxxx");
            _remindDeveloper = new RemindDeveloper(_slackGateway);
            
            var slackGetUsersResponse = File.ReadAllText(
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "../../../SlackUsersExampleResponse.json"
                )
            );

            _slackApi.Get("/" + SlackApiUsersPath).Responds(slackGetUsersResponse);

            _slackApi.Post("/" + SlackApiPostMessagePath).Responds("{{\"ok\": true}}");
            
            var harvestGetUsersResponse = File.ReadAllText(
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "../../../HarvestUsersExampleResponse.json"
                )
            );
            
            _harvestApi.Get(HarvestApiUsersPath).Responds(harvestGetUsersResponse);
            
            var harvestGetTimeEntriesResponse = File.ReadAllText(
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "../../../HarvestTimeEntriesExampleResponse.json"
                )
            );
            
            _harvestApi.Get("/api/v2/time_entries").Responds(harvestGetTimeEntriesResponse);
            
            _slackApi.Start();
            _harvestApi.Start();
        }

        [SetUp]
        public void Setup()
        {
            HandleSetUp();
        }

        [TearDown]
        public void TearDown()
        {
            _harvestApi.Stop();
            _slackApi.Stop();
        }

        [Test]
        public void CanRemindLateDevelopersAtTenThirtyOnFriday()
        {                      
            var getLateDevelopers = new GetLateDevelopers(_slackGateway, _harvestGateway, _harvestGateway);
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
            _slackApi.ReceivedRequests.Count.Should().Be(4);
            
            for (var i = 1; i < 4; i++)
            {
                _slackApi.ReceivedRequests[i].Url.Should().Be(SlackApiAddress + SlackApiPostMessagePath);
            }
        }
        
        
        [TestFixture]
        public class BetweenTenThirtyAndOneThirty
        {
            [OneTimeSetUp]
            public void Init()
            {
                HandleSetUp();
            }
            
            [Test]
            [TestCase(10, 45, 0)]
            [TestCase(11, 00, 4)]
            [TestCase(11, 15, 4)]
            [TestCase(11, 30, 8)]
            [TestCase(11, 45, 8)]
            [TestCase(12, 00, 12)]
            [TestCase(12, 15, 12)]
            [TestCase(12, 30, 16)]
            [TestCase(12, 45, 16)]
            [TestCase(13, 00, 20)]
            [TestCase(13, 15, 20)]
            public void CanRemindLateDevelopersEveryHalfHourUntilOneThirty(int hour, int minute, int expectedCount)
            {                      
                var getLateDevelopers = new GetLateDevelopers(_slackGateway, _harvestGateway, _harvestGateway);
                var clock = new ClockStub(
                    new DateTimeOffset(
                        new DateTime(2019, 03, 01, hour, minute, 0)
                    )
                );

                var remindLateDevelopers = new RemindLateDevelopers(getLateDevelopers, _remindDeveloper, clock);

                remindLateDevelopers.Execute(new RemindLateDevelopersRequest
                    {
                        Message = "Please make sure your timesheet is submitted by 13:30 on Friday."
                    }
                );
               
                _slackApi.ReceivedRequests.Count.Should().Be(expectedCount);
            }  
        }

        [Test]
        public void CanRemindLearnTechChannelAtOneThirty()
        {
            var getLateDevelopers = new GetLateDevelopers(_slackGateway, _harvestGateway, _harvestGateway);
            
            var clock = new ClockStub(
                new DateTimeOffset(
                    new DateTime(2019, 03, 01, 13, 30, 0)
                )
            );

            var shameLateDevelopers = new ShameLateDevelopers(getLateDevelopers, _remindDeveloper, clock);

            const string message = "These are the people yet to submit time sheets:";
            const string channel = "CHBUZLJT1";
            
            shameLateDevelopers.Execute(new ShameLateDevelopersRequest
                {
                    Message = message,
                    Channel = channel
                }
            );

            JObject.Parse(_slackApi.ReceivedRequests.Last().RequestBody)["channel"]
                .ToString().Should().Be(channel);
            JObject.Parse(_slackApi.ReceivedRequests.Last().RequestBody)["text"]
                .ToString().Should().Be($"{message}\n• <@W123AROB>\n• <@W345ABAT>\n• <@W345ALFR>"); 
        }
    }
}
