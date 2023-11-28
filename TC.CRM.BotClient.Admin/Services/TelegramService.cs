using TC.CRM.Bot.Admin.Entities;
using TC.CRM.BotClient.Admin.Persistence;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;

namespace TC.CRM.BotClient.Admin.Services
{
    public interface ITelegramService
    {
        Task Сonfirmation(long chatId, string text);
        Task Notification(string text, long fromChat);
        Task ShowOpenQuestions(long chatId);
        Task ShowCloseQuestions(long chatId);
        Task AnswerQuestion(long chatId, string answer);
    }

    public class TelegramService : ITelegramService
    {

        private readonly ITelegramBotClient _botClient;
        private readonly IRepositoryAdminBot _repositoryAdminBot;

        public TelegramService(ITelegramBotClient botClient, IRepositoryAdminBot repositoryAdminBot)
        {
            _botClient = botClient;
            _repositoryAdminBot = repositoryAdminBot;
        }

        /// <summary>
        /// Спрашиваем дальнейшие действия после получения текста для рассылки
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task Сonfirmation(long chatId, string text)
        {
            string message = $"Ваш текст для рассылки:\n{text}";

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Редактировать", callbackData: "/edit"),
                    InlineKeyboardButton.WithCallbackData(text: "Запустить рассылку", callbackData: "/launch")
                }
            });

            await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: inlineKeyboard);
        }


        /// <summary>
        /// Отправляем текст рассылки и фотографию/видео если есть, по всем группам
        /// </summary>
        /// <param name="text"></param>
        /// <param name="fromChat"></param>
        /// <returns></returns>
        public async Task Notification(string text, long fromChat)
        {
            // Получаем список чатов (групп), в которых бот является участником
            var chatIds = await _repositoryAdminBot.GetChatsId();

            // Отправляем сообщение в каждую группу
            foreach (var chatId in chatIds)
            {
                if (ImageState.Id != -1) // -1 стоит по дефолту для стабильной проверки
                {
                    await _botClient.ForwardMessageAsync(chatId, fromChat, ImageState.Id, disableNotification: true);
                }
                await _botClient.SendTextMessageAsync(chatId, text);
            }
            ImageState.Id = -1; // "Обнуляем"
            await _botClient.SendTextMessageAsync(fromChat, "Рассылка отправлена");
        }

        /// <summary>
        /// Показывает все открытые вопросы
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        public async Task ShowOpenQuestions(long chatId)
        {
            var data = await _repositoryAdminBot.ShowOpenQuestions();

            await _botClient.SendTextMessageAsync(chatId, "Список открытых вопросов");

            if (data.Count == 0)
            {
                await _botClient.SendTextMessageAsync(chatId, "Нет открытых вопросов");
            }

            foreach (var item in data)
            {
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(text: "Ответить", callbackData: $"/answer_question?{item.Id}"),
                        InlineKeyboardButton.WithCallbackData(text: "Забросить", callbackData: $"/quit_question?{item.Id}"),
                    }
                });

                var chatData = await _repositoryAdminBot.GetChatsData(item.ChatId);

                string mess = $"Группа: {chatData.ChatName}\nКлиент: @{item.ClientLogin}\n\nТема:\n{item.Title}\nВопрос: {item.Description}";

                await _botClient.SendTextMessageAsync(chatId, mess, replyMarkup: inlineKeyboard);
            }
        }

        /// <summary>
        /// Показывает все закрытые вопросы
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        public async Task ShowCloseQuestions(long chatId)
        {
            var data = await _repositoryAdminBot.ShowCloseQuestions();

            await _botClient.SendTextMessageAsync(chatId, "Список закрытых вопросов");

            if (data.Count == 0)
            {
                await _botClient.SendTextMessageAsync(chatId, "Нет закрытых вопросов");
            }

            foreach (var item in data)
            {
                var chatData = await _repositoryAdminBot.GetChatsData(item.ChatId);

                string status = item.Status == 2 ? "Заброшено" : item.Status == 3 ? "Решено" : "Статус не опознан";
                string mess = $"Статус: {status}\nГруппа: {chatData.ChatName}\nКлиент: @{item.ClientLogin}\n\nТема:\n{item.Title}\nВопрос: {item.Description}\nОтвет: {item.Answer}";

                await _botClient.SendTextMessageAsync(chatId, mess);
            }
        }

        /// <summary>
        /// Отправляет сообщение с ответом в группу где был задан вопрос
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="answer"></param>
        /// <returns></returns>
        public async Task AnswerQuestion(long chatId, string answer)
        {
            var question = await _repositoryAdminBot.AnswerQuestion(chatId, answer);

            string mess = $"@{question.ClientLogin}\n\nОператор ответил на ваш вопрос.\nВаш вопрос:\n\n{question.Description}\n\nОтвет оператора:\n\n{answer}";

            await _botClient.SendTextMessageAsync(question.ChatId, mess);
        }

    }
}
