using System;
using System.IO;
using System.Linq;
using CryptoTechReminderSystem.Gateway;
using FluentAssertions;
using FluentSim;
using NUnit.Framework;

namespace CryptoTechReminderSystem.Test.Gateway
{
    [TestFixture]
    public class HarvestGatewayTests
    {
        private const string Address = "http://localhost:8050/";
        private const string Token = "xxxx-xxxxxxxxx-xxxx";
        private const string HarvestAccountId = "123456";
        private const string UserAgent = "The Wolves";
        private const string DeveloperRoles = 
            @"Software Engineer, Senior Software Engineer, Senior Engineer, Lead Engineer, 
            Delivery Manager, SRE, Consultant, Delivery Principal";
        
        private static FluentSimulator _harvestApi;
        private static HarvestGateway _harvestGateway;
        
        [TestFixture]
        public class CanRequestDevelopers
        {
            [SetUp]
            public void Setup()
            {
                _harvestApi = new FluentSimulator(Address);
                _harvestGateway = new HarvestGateway(Address, Token, HarvestAccountId, UserAgent, DeveloperRoles);
                
                var json = File.ReadAllText(
                    Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "../../../Gateway/HarvestUsersExampleResponse.json"
                    )
                );
                
                _harvestApi.Get("/api/v2/users").Responds(json);
                _harvestApi.Start();
            }

            [TearDown]
            public void TearDown()
            {
                _harvestApi.Stop();
            }

            [Test]
            public void CanSendOneRequestAtATime()
            {
                _harvestGateway.RetrieveDevelopers();
               
                _harvestApi.ReceivedRequests.Count.Should().Be(1);
            }
            
            [Test]
            [TestCase("Authorization", "Bearer " + Token)]
            [TestCase("Harvest-Account-Id", HarvestAccountId)]
            [TestCase("User-Agent", UserAgent)]
            public void CanGetDevelopersWithHeaders(string header, string expected)
            {
                _harvestGateway.RetrieveDevelopers();
                
                _harvestApi.ReceivedRequests.First().Headers[header].Should().Be(expected);
            }
            
            [Test]
            public void CanOnlyGetActiveDevelopers()
            {
                var response = _harvestGateway.RetrieveDevelopers();
                
                response.First().FirstName.Should().Be("Dick");
                response.Count.Should().Be(6);
            }
        }

        [TestFixture]
        public class CanRequestTimeSheets
        {
            private const string ApiTimeSheetPath = "api/v2/time_entries";
            private DateTimeOffset _defaultDateFrom;
            private DateTimeOffset _defaultDateTo;

            [SetUp]
            public void Setup()
            {
                _harvestApi = new FluentSimulator(Address);
                _harvestGateway = new HarvestGateway(Address, Token, HarvestAccountId, UserAgent, DeveloperRoles);
                _defaultDateFrom = new DateTimeOffset(
                    new DateTime(2019, 04, 08)
                );
                _defaultDateTo = new DateTimeOffset(
                    new DateTime(2019, 04, 12)
                );
            }

            private void SetUpTimeSheetApiEndpoint(string dateFrom, string dateTo)
            {
                var jsonPageOne = File.ReadAllText(
                    Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "../../../Gateway/HarvestTimeEntriesApiEndpointPageOne.json"
                    )
                );

                _harvestApi.Get($"/{ApiTimeSheetPath}")
                    .WithParameter("from", dateFrom)
                    .WithParameter("to", dateTo)
                    .WithParameter("page", "1")
                    .Responds(jsonPageOne);
                
                var jsonPageTwo = File.ReadAllText(
                    Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "../../../Gateway/HarvestTimeEntriesApiEndpointPageTwo.json"
                    )
                );

                _harvestApi.Get($"/{ApiTimeSheetPath}")
                    .WithParameter("from", dateFrom)
                    .WithParameter("to", dateTo)
                    .WithParameter("page", "2")
                    .Responds(jsonPageTwo);
                
                _harvestApi.Start();
            }
            
            [TearDown]
            public void TearDown()
            {
                _harvestApi.Stop();
            }
            
            [Test]
            public void CanSendOneRequestAtATime()
            {
                SetUpTimeSheetApiEndpoint("2019-04-08", "2019-04-12");

                _harvestGateway.RetrieveTimeSheets(_defaultDateFrom, _defaultDateTo);
               
                _harvestApi.ReceivedRequests.Count.Should().Be(2);
            }
            
            [Test]
            [TestCase("Authorization", "Bearer " + Token)]
            [TestCase("Harvest-Account-Id", HarvestAccountId)]
            [TestCase("User-Agent", UserAgent)]
            public void CanGetTimeSheetsWithHeaders(string header, string expected)
            {
                SetUpTimeSheetApiEndpoint("2019-04-08", "2019-04-12");
                
                _harvestGateway.RetrieveTimeSheets(_defaultDateFrom, _defaultDateTo);

                _harvestApi.ReceivedRequests.First().Headers[header].Should().Be(expected);
            }
            
            [Test]
            [TestCase("08", "12")]
            [TestCase("14", "19")]
            [TestCase("01", "05")]
            public void CanRequestTimeSheetsWithAStartingAndEndingDate(string dayFrom, string dayTo)
            {
                SetUpTimeSheetApiEndpoint($"2019-04-{dayFrom}", $"2019-04-{dayTo}");

                var dateFrom = new DateTimeOffset(
                    new DateTime(2019, 04, int.Parse(dayFrom))
                );
               
                var dateTo = new DateTimeOffset(
                    new DateTime(2019, 04, int.Parse(dayTo))
                );
                
                _harvestGateway.RetrieveTimeSheets(dateFrom, dateTo);

                _harvestApi.ReceivedRequests.First().Url.Should().Be(
                    $"{Address}{ApiTimeSheetPath}?from=2019-04-{dayFrom}&to=2019-04-{dayTo}&page=1"
                );
            }

            [Test]
            [TestCase(1782975)]
            [TestCase(1782974)]
            public void CanGetATimeSheet(int expectedUserId)
            {
                SetUpTimeSheetApiEndpoint("2019-04-08", "2019-04-12");

                var response = _harvestGateway.RetrieveTimeSheets(_defaultDateFrom, _defaultDateTo);

                response.Any(entry => entry.UserId == expectedUserId).Should().BeTrue();
            }
            
            [Test]
            public void CanGetAllTimeSheets()
            {
                SetUpTimeSheetApiEndpoint("2019-04-08", "2019-04-12");

                var response = _harvestGateway.RetrieveTimeSheets(_defaultDateFrom, _defaultDateTo);

                response.Count.Should().Be(6);
            }
            
            [Test]
            public void CanGetAllTimeSheetProperties()
            {
                SetUpTimeSheetApiEndpoint("2019-04-08", "2019-04-12");

                var response = _harvestGateway.RetrieveTimeSheets(_defaultDateFrom, _defaultDateTo);
                
                response.First().Id.Should().Be(456709345);
                response.First().UserId.Should().Be(1782975);
                response.First().Hours.Should().Be(7.0);
            }
        }
    }
}
