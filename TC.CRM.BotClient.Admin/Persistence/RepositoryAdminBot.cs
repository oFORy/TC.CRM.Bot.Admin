using Microsoft.EntityFrameworkCore;
using TC.CRM.BotClient.Admin.Persistance;
using TC.CRM.BotClient.Admin.Entities;
using Telegram.Bot.Types;
using static TC.CRM.BotClient.Admin.Bot.HandleUpdateService;

namespace TC.CRM.BotClient.Admin.Persistence
{
    public interface IRepositoryAdminBot
    {
        Task<List<long>> GetChatsId();
        Task<bool> CheckIfAdminAsync(long chatId);
        Task<bool> NewsletterStateAdd(long chatId, BotState botState);
        Task<NewsletterState> CheckNewsletterState(long chatId);
        Task RemoveNewsletterState(long chatId);
        Task<List<Question>> ShowOpenQuestions();
        Task<List<Question>> ShowCloseQuestions();
        Task<Question> ShowSingleQuestion(int questId);
        Task<GroupBot> GetChatsData(long chatId);
        Task AnswerAdminData(BotState botState, int questionId, long adminId);
        Task RemoveAnswerState(long adminId);
        Task<Question> AnswerQuestion(long adminId, string answer);
        Task QuitQuestion(int questionId);
    }


    public class RepositoryAdminBot : IRepositoryAdminBot
    {
        private readonly BotDbContext _dbContext;
        public RepositoryAdminBot(BotDbContext botDbContext)
        {
            _dbContext = botDbContext;
        }

        public async Task<bool> CheckIfAdminAsync(long chatId)
        {
            var admin = await _dbContext.BotAdmins.FirstOrDefaultAsync(u => u.TelegramId == chatId);
            if (admin != null)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Проверка бд стейта рассылки
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        public async Task<NewsletterState> CheckNewsletterState(long chatId)
        {
            return await _dbContext.NewsletterStates.FirstOrDefaultAsync(u => u.ChatId == chatId);
        }

        /// <summary>
        /// Отправляет все id чатов
        /// </summary>
        /// <returns></returns>
        public async Task<List<long>> GetChatsId()
        {
            return await _dbContext.GroupsBot.Select(g => g.ChatId).ToListAsync();
        }

        /// <summary>
        /// Создать стейт в бд для рассылки
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="botState"></param>
        /// <returns></returns>
        public async Task<bool> NewsletterStateAdd(long chatId, BotState botState)
        {
            await _dbContext.NewsletterStates.AddAsync(new NewsletterState { ChatId = chatId, State = botState });
            return await _dbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Удалить использованный стейт бд
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        public async Task RemoveNewsletterState(long chatId)
        {
            var userState = await _dbContext.NewsletterStates.FirstOrDefaultAsync(u => u.ChatId == chatId);
            if (userState != null)
            {
                _dbContext.NewsletterStates.Remove(userState);
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Показать все открытые вопросы
        /// </summary>
        /// <returns></returns>
        public async Task<List<Question>> ShowOpenQuestions()
        {
            return await _dbContext.Questions.Where(u => u.Status == 1).ToListAsync();
        }

        /// <summary>
        /// Показать все закрытые вопросы
        /// </summary>
        /// <returns></returns>
        public async Task<List<Question>> ShowCloseQuestions()
        {
            return await _dbContext.Questions.Where(u => u.Status == 2 || u.Status == 3).ToListAsync();
        }

        /// <summary>
        /// Показать полную информацию о выбранном вопросе
        /// </summary>
        /// <param name="questId"></param>
        /// <returns></returns>
        public async Task<Question> ShowSingleQuestion(int questId)
        {
            return await _dbContext.Questions.FirstOrDefaultAsync(u => u.Id == questId);
        }

        /// <summary>
        /// Отправляет всю информацию об одном чате
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        public async Task<GroupBot> GetChatsData(long chatId)
        {
            return await _dbContext.GroupsBot.FirstOrDefaultAsync(g => g.ChatId == chatId);
        }

        /// <summary>
        /// Обрабатывает ответ на вопрос и записывает ответ в бд
        /// </summary>
        /// <param name="adminId"></param>
        /// <param name="answer"></param>
        /// <returns></returns>
        public async Task<Question> AnswerQuestion(long adminId, string answer)
        {
            var answerState = await _dbContext.AdminAnswers.FirstOrDefaultAsync(u => u.AdminId == adminId);

            var question = await _dbContext.Questions.Where(g => g.Id == answerState.QuestionId).FirstOrDefaultAsync();
            if (question != null)
            {
                question.Answer = answer;
                question.Status = 3;
                await _dbContext.SaveChangesAsync();
            }

            return question;
        }

        /// <summary>
        /// Стейт для администратора в бд
        /// </summary>
        /// <param name="botState"></param>
        /// <param name="questionId"></param>
        /// <param name="adminId"></param>
        /// <returns></returns>
        public async Task AnswerAdminData(BotState botState, int questionId, long adminId)
        {
            var ans = new AdminAnswer
            {
                BotState = botState,
                QuestionId = questionId,
                AdminId = adminId
            };
            await _dbContext.AdminAnswers.AddAsync(ans);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Удалить использованных стейт
        /// </summary>
        /// <param name="adminId"></param>
        /// <returns></returns>
        public async Task RemoveAnswerState(long adminId)
        {
            var answerState = await _dbContext.AdminAnswers.FirstOrDefaultAsync(u => u.AdminId == adminId);
            if (answerState != null)
            {
                _dbContext.AdminAnswers.Remove(answerState);
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Забросить вопрос
        /// </summary>
        /// <param name="questionId"></param>
        /// <returns></returns>
        public async Task QuitQuestion(int questionId)
        {
            var question = await _dbContext.Questions.Where(g => g.Id == questionId).FirstOrDefaultAsync();
            if (question != null)
            {
                question.Status = 2;
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
