namespace TC.CRM.BotClient.Admin.Entities
{
    public class Question
    {
        public int Id { get; set; }
        public long ChatId { get; set; }
        public long ClientId { get; set; }
        public string? ClientLogin { get; set; }
        public int MessageId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Answer { get; set; }
        public int Status { get; set; } // 1 - открыт 2 - заброшен 3 - решен
    }
}
