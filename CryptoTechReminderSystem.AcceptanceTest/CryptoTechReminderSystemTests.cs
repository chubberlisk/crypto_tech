using System;
using System.IO;
using NUnit.Framework;
using FluentAssertions;
using CryptoTechReminderSystem.Boundary;
using CryptoTechReminderSystem.Gateway;
using CryptoTechReminderSystem.UseCase;
using FluentSim;
using Newtonsoft.Json;

namespace CryptoTechReminderSystem.AcceptanceTest
{
    public class CryptoTechReminderSystemTests
    {
        private const string SlackApiAddress = "http://localhost:8009/";
        private const string HarvestApiAddress = "http://localhost:8010/";
        private const string SlackApiUsersPath = "api/users.list";
        private const string SlackApiPostMessagePath = "api/chat.postMessage";
        private const string HarvestApiUsersPath = "/api/v2/users";
        private FluentSimulator _slackApi;
        private FluentSimulator _harvestApi;
        private HarvestGateway _harvestGateway;
        private SlackGateway _slackGateway;
        private RemindDeveloper _remindDeveloper;
        
        private class ClockStub : IClock
        {
            private DateTimeOffset _currentDateTime;

            public ClockStub(DateTimeOffset dateTime)
            {
                _currentDateTime = dateTime;
            }

            public DateTimeOffset Now()
            {
                return _currentDateTime;
            }

            public void AddSpecifiedMinutes(int minutes)
            {
                _currentDateTime = _currentDateTime.AddMinutes(minutes);
            }
            
            
        }

        [SetUp]
        public void Setup()
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
        
        [Test]
        public void CanRemindLateDevelopersEveryHalfHourUntilOneThirty()
        {                      
            var getLateDevelopers = new GetLateDevelopers(_slackGateway, _harvestGateway, _harvestGateway);
            var clock = new ClockStub(
                new DateTimeOffset(
                    new DateTime(2019, 03, 01, 11, 0, 0)
                )
            );

            var remindLateDevelopers = new RemindLateDevelopers(getLateDevelopers, _remindDeveloper, clock);

            remindLateDevelopers.Execute(new RemindLateDevelopersRequest
                {
                    Message = "Please make sure your timesheet is submitted by 13:30 on Friday."
                }
            );
               
            _slackApi.ReceivedRequests.Count.Should().Be(4);

            // 11:15
            clock.AddSpecifiedMinutes(15);
            
            remindLateDevelopers = new RemindLateDevelopers(getLateDevelopers, _remindDeveloper, clock);
            remindLateDevelopers.Execute(new RemindLateDevelopersRequest
                {
                    Message = "Please make sure your timesheet is submitted by 13:30 on Friday."
                }
            );
            
            _slackApi.ReceivedRequests.Count.Should().Be(4);
            
            // 11:30
            clock.AddSpecifiedMinutes(15);
            
            remindLateDevelopers = new RemindLateDevelopers(getLateDevelopers, _remindDeveloper, clock);
            remindLateDevelopers.Execute(new RemindLateDevelopersRequest
                {
                    Message = "Please make sure your timesheet is submitted by 13:30 on Friday."
                }
            );
            
            _slackApi.ReceivedRequests.Count.Should().Be(8);
            
            // 11:45 
            clock.AddSpecifiedMinutes(15);
            
            remindLateDevelopers = new RemindLateDevelopers(getLateDevelopers, _remindDeveloper, clock);
            remindLateDevelopers.Execute(new RemindLateDevelopersRequest
                {
                    Message = "Please make sure your timesheet is submitted by 13:30 on Friday."
                }
            );
            
            _slackApi.ReceivedRequests.Count.Should().Be(8);

            // 12:00
            clock.AddSpecifiedMinutes(15);
            
            remindLateDevelopers = new RemindLateDevelopers(getLateDevelopers, _remindDeveloper, clock);
            remindLateDevelopers.Execute(new RemindLateDevelopersRequest
                {
                    Message = "Please make sure your timesheet is submitted by 13:30 on Friday."
                }
            );
            
            _slackApi.ReceivedRequests.Count.Should().Be(12);

            // 12:15 
            clock.AddSpecifiedMinutes(15);
            
            remindLateDevelopers = new RemindLateDevelopers(getLateDevelopers, _remindDeveloper, clock);
            remindLateDevelopers.Execute(new RemindLateDevelopersRequest
                {
                    Message = "Please make sure your timesheet is submitted by 13:30 on Friday."
                }
            );
            
            _slackApi.ReceivedRequests.Count.Should().Be(12);
            
            // 12:30 
            clock.AddSpecifiedMinutes(15);
            
            remindLateDevelopers = new RemindLateDevelopers(getLateDevelopers, _remindDeveloper, clock);
            remindLateDevelopers.Execute(new RemindLateDevelopersRequest
                {
                    Message = "Please make sure your timesheet is submitted by 13:30 on Friday."
                }
            );
            
            _slackApi.ReceivedRequests.Count.Should().Be(16);

            // 12:45
            clock.AddSpecifiedMinutes(15);
            
            remindLateDevelopers = new RemindLateDevelopers(getLateDevelopers, _remindDeveloper, clock);
            remindLateDevelopers.Execute(new RemindLateDevelopersRequest
                {
                    Message = "Please make sure your timesheet is submitted by 13:30 on Friday."
                }
            );
            
            _slackApi.ReceivedRequests.Count.Should().Be(16);

            // 13:00
            clock.AddSpecifiedMinutes(15);
            
            remindLateDevelopers = new RemindLateDevelopers(getLateDevelopers, _remindDeveloper, clock);
            remindLateDevelopers.Execute(new RemindLateDevelopersRequest
                {
                    Message = "Please make sure your timesheet is submitted by 13:30 on Friday."
                }
            );
            
            _slackApi.ReceivedRequests.Count.Should().Be(20);
            
            
            // 13:15
            clock.AddSpecifiedMinutes(15);
            
            remindLateDevelopers = new RemindLateDevelopers(getLateDevelopers, _remindDeveloper, clock);
            remindLateDevelopers.Execute(new RemindLateDevelopersRequest
                {
                    Message = "Please make sure your timesheet is submitted by 13:30 on Friday."
                }
            );
            
            _slackApi.ReceivedRequests.Count.Should().Be(20);
            
            
            // 13:30
            clock.AddSpecifiedMinutes(15);
            
            remindLateDevelopers = new RemindLateDevelopers(getLateDevelopers, _remindDeveloper, clock);
            remindLateDevelopers.Execute(new RemindLateDevelopersRequest
                {
                    Message = "Please make sure your timesheet is submitted by 13:30 on Friday."
                }
            );
            
            _slackApi.ReceivedRequests.Count.Should().Be(24);
        }
    }
}
