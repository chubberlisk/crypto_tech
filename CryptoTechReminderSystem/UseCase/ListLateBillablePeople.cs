using System.Linq;
using CryptoTechReminderSystem.Boundary;

namespace CryptoTechReminderSystem.UseCase
{
    public class ListLateBillablePeople
    {
        private readonly IGetLateBillablePeople _getLateBillablePeople;
        private readonly ISendReminder _sendReminder;

        public ListLateBillablePeople(IGetLateBillablePeople getLateBillablePeople, ISendReminder sendReminder)
        {
            _getLateBillablePeople = getLateBillablePeople;
            _sendReminder = sendReminder;
        }

        public void Execute(ListLateBillablePeopleRequest listLateBillablePeopleRequest)
        {
            var lateBillablePeople = _getLateBillablePeople.Execute();
            string text;

            if (!lateBillablePeople.BillablePeople.Any())
            {
                text = listLateBillablePeopleRequest.NoLateBillablePeopleMessage;
            } else
            {
                text = lateBillablePeople.BillablePeople.Aggregate(
                    listLateBillablePeopleRequest.LateBillablePeopleMessage, 
                    (current, billablePerson) => current + $"\n• <@{billablePerson.Id}>"
                );
            }

            _sendReminder.Execute(new SendReminderRequest
            {
                Text = text,
                Channel = listLateBillablePeopleRequest.Channel
            });
        }
    }
}
