using static TC.CRM.BotClient.Admin.Bot.HandleUpdateService;

namespace TC.CRM.BotClient.Admin.Entities
{
    public class AdminAnswer
    {
        public int Id { get; set; }
        public BotState BotState { get; set; }
        public long AdminId { get; set; }
        public int QuestionId { get; set; }
    }
}
