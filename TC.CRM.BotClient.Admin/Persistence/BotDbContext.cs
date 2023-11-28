
using Microsoft.EntityFrameworkCore;
using TC.CRM.BotClient.Admin.Entities;
using Telegram.Bot.Types;

namespace TC.CRM.BotClient.Admin.Persistance
{
    public class BotDbContext : DbContext
    {
        public BotDbContext(DbContextOptions<BotDbContext> options)
            : base(options)
        {
            //Database.EnsureDeleted();
            //Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }

        //protected override void OnConfiguring(DbContextOptionsBuilder option) => option.UseSqlite(Environment.GetEnvironmentVariable("DB_CS"));

        public DbSet<GroupBot> GroupsBot { get; set; }
        public DbSet<BotAdmin> BotAdmins { get; set; }
        public DbSet<NewsletterState> NewsletterStates { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<AdminAnswer> AdminAnswers { get; set; }
        

    }
}
