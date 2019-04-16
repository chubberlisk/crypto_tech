using System;
using CryptoTechReminderSystem.Boundary;

namespace CryptoTechReminderSystem.UseCase
{
    public class RemindLateDevelopers
    {
        private IGetLateDevelopers _getLateDevelopers;
        private ISendReminder _sendReminder;
        private IClock _clock;

        public RemindLateDevelopers(IGetLateDevelopers getLateDevelopers, ISendReminder sendReminder, IClock clock)
        {
            _getLateDevelopers = getLateDevelopers;
            _sendReminder = sendReminder;
            _clock = clock;
        }

        private bool IsHalfHourInterval()
        {
            return _clock.Now().ToUnixTimeSeconds() % 1800 == 0;
        }

        private bool IsBeforeTwoPm()
        {
            return _clock.Now().Hour < 14;
        }
        public void Execute(RemindLateDevelopersRequest remindLateDevelopersRequest)
        {
            if(IsHalfHourInterval() && IsBeforeTwoPm() && _clock.Now().DayOfWeek == DayOfWeek.Friday)
            {
                var lateDevelopers = _getLateDevelopers.Execute();
                foreach (var lateDeveloper in lateDevelopers.Developers)
                {
                    _sendReminder.Execute(new SendReminderRequest
                    {
                        Channel = lateDeveloper,
                        Text = remindLateDevelopersRequest.Message
                    });
                }
            }
        }
    }
}