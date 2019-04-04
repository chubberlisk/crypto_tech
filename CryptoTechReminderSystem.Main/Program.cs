﻿using System;
using System.Collections.Generic;
using CryptoTechReminderSystem.Boundary;
using CryptoTechReminderSystem.Gateway;
using CryptoTechReminderSystem.UseCase;
using dotenv.net;

namespace CryptoTechReminderSystem.Main
{
    class Program
    {
        static void Main(string[] args)
        {    
            DotEnv.Config();
            
            var remindDeveloper = new RemindDeveloper(
                new SlackGateway(
                    "https://slack.com/",
                    Environment.GetEnvironmentVariable("SLACK_TOKEN")
                )
            );
            
            IList<string> idsList = new List<string>()
            {
                "UH3DVNTH7", 
                "UH9NYEWSC", 
                "UHC4PEXM2", 
                "UHCABC886", 
                "UHCFC7Z61", 
                "UHDJJA52S"
            };

            foreach (var id in idsList)
            {
                remindDeveloper.Execute(new RemindDeveloperRequest
                {
                    Channel = id,
                    Text = "Please make sure your timesheet is submitted by 13:30 on Friday."
                });
            }
        }
    }
}