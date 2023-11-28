using Npgsql.Replication.PgOutput.Messages;
using TC.CRM.Bot.Admin.Entities;
using TC.CRM.BotClient.Admin.Persistence;
using TC.CRM.BotClient.Admin.Services;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TC.CRM.BotClient.Admin.Bot;

public class HandleUpdateService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<HandleUpdateService> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IRepositoryAdminBot _repositoryAdminBot;

    public enum BotState
    {
        None,
        WaitForQuestion,
        WaitAnswer
    }


    public HandleUpdateService(ITelegramBotClient botClient, ILogger<HandleUpdateService> logger, ITelegramService telegramService, IRepositoryAdminBot repositoryAdminBot)
    {
        _botClient = botClient;
        _logger = logger;
        _telegramService = telegramService;
        _repositoryAdminBot = repositoryAdminBot;
    }

    public async Task EchoAsync(Update update)
    {
        try
        {
            _logger.LogInformation(update.Type.ToString());

            // Проверяем, является ли отправитель администратором
            bool isAdmin = await _repositoryAdminBot.CheckIfAdminAsync(update.Type == UpdateType.Message ? update.Message!.From.Id : update.CallbackQuery!.From.Id);
            if (isAdmin)
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        var message = update.Message!;
                        await BotOnMessageReceived(message);
                        break;
                    case UpdateType.CallbackQuery:
                        var callbackQuery = update.CallbackQuery;
                        await ProcessCallbackQuery(callbackQuery);
                        break;
                    default:
                        await UnknownUpdateHandlerAsync(update);
                        break;
                };
            }
            else if (update.Message.Chat.Type == ChatType.Private && isAdmin) // отправляем сообщдение только в приватный чат, чтобы не спамить в группе
                await _botClient.SendTextMessageAsync(update.Message!.Chat.Id, "Вы не являетесь администратором");
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(exception);
        }
    }


    private async Task BotOnMessageReceived(Message message)
    {
        _logger.LogInformation($"Receive message type: {message.Type}");

        Message sentMessage = new Message();

        if (message.Type == MessageType.Text)
        {
            switch (message.Text!.Split(' ')[0])
            {
                case "/start":
                    sentMessage = await SendFirstMessage(message);
                    break;
                default:
                    await CheckMessage(message);
                    break;
            }
        }
        else if (message.Type == MessageType.Photo || message.Type == MessageType.Video)
        {
            // Есть сообщение является фотографией или видео, то записываем её id в локальный стейт
            ImageState.Id = message.MessageId;
        }

        _logger.LogInformation($"The message was sent with id: {sentMessage.MessageId}");
    }


    /// <summary>
    /// Метод обработки колбеков
    /// </summary>
    /// <param name="callbackQuery"></param>
    /// <returns></returns>
    private async Task ProcessCallbackQuery(CallbackQuery callbackQuery)
    {
        var chatId = callbackQuery.Message.Chat.Id;

        switch (callbackQuery.Data)
        {
            case "/newsletter":
                // Устанавливаем состояние "WaitForQuestion"
                await _repositoryAdminBot.NewsletterStateAdd(chatId, BotState.WaitForQuestion);

                // Отправляем сообщение с просьбой ввести вопрос
                await _botClient.SendTextMessageAsync(chatId, "Введите текст рассылки:");
                break;

            case "/edit":
                // Устанавливаем состояние "WaitForQuestion"
                await _repositoryAdminBot.NewsletterStateAdd(chatId, BotState.WaitForQuestion);

                // Отправляем сообщение с просьбой ввести вопрос
                await _botClient.SendTextMessageAsync(chatId, "Введите текст рассылки:");
                break;

            case "/launch":
                var lastUserMessage = callbackQuery.Message.Text; // берем последнее сообщение (но последнее было от бота)
                lastUserMessage = lastUserMessage.Split(":\n")[1]; // поэтому парсим сообщение бота и берем оттуда сообщение пользователя
                
                await _telegramService.Notification(lastUserMessage, chatId);
                await _repositoryAdminBot.RemoveNewsletterState(chatId); // удаляем использованных стейт
                break;

            case "/show_open_questions":
                await _telegramService.ShowOpenQuestions(chatId);
                break;
            case "/show_close_questions":
                await _telegramService.ShowCloseQuestions(chatId);
                break;

            case string s when System.Text.RegularExpressions.Regex.IsMatch(s, "^/answer_question"): // гибкий колбек, можно передавать необходимые параметры в колбек кнопки
                var test = callbackQuery.Data.Split('?');
                if (test.Length > 1)
                {
                    await _botClient.SendTextMessageAsync(chatId, "Введите текст ответа:");

                    await _repositoryAdminBot.NewsletterStateAdd(chatId, BotState.WaitAnswer);
                    var questionId = test[1];

                    await _repositoryAdminBot.AnswerAdminData(BotState.WaitAnswer, int.Parse(questionId), callbackQuery.From.Id);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "Неизвестная команда. Выберите пункт меню.");
                }
                break;

            case string s when System.Text.RegularExpressions.Regex.IsMatch(s, "^/quit_question"):
                var quit = callbackQuery.Data.Split('?');
                if (quit.Length > 1)
                {

                    var questionId = quit[1];

                    await _repositoryAdminBot.QuitQuestion(int.Parse(questionId));
                    await _botClient.SendTextMessageAsync(chatId, "Вопрос заброшен");
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "Неизвестная команда. Выберите пункт меню.");
                }
                break;

            default:
                await _botClient.SendTextMessageAsync(chatId, "Неизвестная команда. Выберите пункт меню.");
                break;
        }
    }



    /// <summary>
    /// Анализ сообщения
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private async Task CheckMessage(Message message)
    {
        long chatId = message.Chat.Id;

        var userState = await _repositoryAdminBot.CheckNewsletterState(chatId);

        if (userState != null)
        {
            switch (userState.State)
            {
                case BotState.WaitForQuestion:
                    await _telegramService.Сonfirmation(message.Chat.Id, message.Text);

                    // сброс состояние
                    await _repositoryAdminBot.RemoveNewsletterState(chatId);
                    break;
                case BotState.WaitAnswer:
                    await _telegramService.AnswerQuestion(message.Chat.Id, message.Text);

                    // сброс состояния
                    await _repositoryAdminBot.RemoveNewsletterState(chatId);
                    await _repositoryAdminBot.RemoveAnswerState(message.From.Id);
                    await _botClient.SendTextMessageAsync(chatId, "Вы ответили на вопрос");
                    break;
            }
        }
    }

    /// <summary>
    /// Отправляем сообщение с кнопками для команды start
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private async Task<Message> SendFirstMessage(Message message)
    {
        string usage = "Привет" + (message?.From?.FirstName != null ? ", " + message?.From?.FirstName + "! " : "! ") +
                       "Бот администрации goods-china.ru\n";

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Cоздать рассылку", callbackData: "/newsletter")
    },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Открытые вопросы", callbackData: "/show_open_questions")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Закрытые вопросы", callbackData: "/show_close_questions")
                }
            });

        return await _botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                     text: usage,
                                                     replyMarkup: inlineKeyboard);
    }



    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogInformation("Unknown update type: {updateType}", update.Type);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Обработка ошибок
    /// </summary>
    /// <param name="exception"></param>
    /// <returns></returns>
    public Task HandleErrorAsync(Exception exception)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage} | StackTrace: {StackTrace}", ErrorMessage, exception.StackTrace);
        return Task.CompletedTask;
    }
}
