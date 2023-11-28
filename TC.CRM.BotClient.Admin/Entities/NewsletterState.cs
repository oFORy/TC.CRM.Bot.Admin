using static TC.CRM.BotClient.Admin.Bot.HandleUpdateService;

namespace TC.CRM.BotClient.Admin.Entities
{
    public class NewsletterState
    {
        public int Id { get; set; }
        public long ChatId { get; set; }
        public BotState State { get; set; }
    }
}
