using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace SimpleTGBot;

using Helldivers2API.Data.Models;
using Helldivers2API.Data.Models.Extensions;
using Helldivers2API.Data.Models.Interfaces;
using System.Diagnostics;


using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

public class TelegramBot
{
    // Токен TG-бота. Можно получить у @BotFather
    private const string BotToken = "7109270091:AAEa5Yf-6WKkuwBk8nMEuxT6QcKRf9zNwSA";
    public user curr_user;
    public List<user> all_users;
    public string[] message_not_match;
    public string[] quit_settings;
    /// <summary>
    /// Инициализирует и обеспечивает работу бота до нажатия клавиши Esc
    /// </summary>
    /// 
    ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
        {
            new KeyboardButton[] { "Mission","Online" },
            new KeyboardButton[] { "Statistic","Planets" },
            new KeyboardButton[] { "Settings" },
        })
    {
        ResizeKeyboard = true
    };
    ReplyKeyboardMarkup settingsMarkup = new(new[]
        {
            new KeyboardButton[] { "Bugs"},
            new KeyboardButton[] { "Automatons"},
            new KeyboardButton[] { "Back"},
        })
    {
        ResizeKeyboard = true
    };
    public async Task Run()
    {
        // Если вам нужно хранить какие-то данные во время работы бота (массив информации, логи бота,
        // историю сообщений для каждого пользователя), то это всё надо инициализировать в этом методе.
        // TODO: Инициализация необходимых полей
        
        // Инициализируем наш клиент, передавая ему токен.
        var botClient = new TelegramBotClient(BotToken);

        // Служебные вещи для организации правильной работы с потоками
        using CancellationTokenSource cts = new CancellationTokenSource();
        
        // Разрешённые события, которые будет получать и обрабатывать наш бот.
        // Будем получать только сообщения. При желании можно поработать с другими событиями.
        ReceiverOptions receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = new [] { UpdateType.Message }
        };

        // Привязываем все обработчики и начинаем принимать сообщения для бота
        botClient.StartReceiving(
            updateHandler: OnMessageReceived,
            pollingErrorHandler: OnErrorOccured,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        using (var sr = new StreamReader("user_data.json"))
        {
            var obj = sr.ReadToEnd();
            if (obj.Length == 0)
            {
                all_users = new List<user>();
            }
            else all_users = JsonSerializer.Deserialize<user[]>(obj).ToList();
        }
        message_not_match = new string[] {
            "Ministry of truth is watching you, soldier.",
            "Welcome to the planet Hoxxes, Miner! \n Wait, wrong chat, forget about it.",
            "For every minute of unscheduled rest, the opportunity to submit an application for C1-PERM form is postponed for a year",
            "If you have any information about Karl, contact your Democracy officer.",
            "Remember: Freedom!",
            "Don’t drink and drive!"

        };
        // Проверяем что токен верный и получаем информацию о боте
        var me = await botClient.GetMeAsync(cancellationToken: cts.Token);
        Console.WriteLine($"Бот @{me.Username} запущен.\nДля остановки нажмите клавишу Esc...");

        // Ждём, пока будет нажата клавиша Esc, тогда завершаем работу бота
        while (Console.ReadKey().Key != ConsoleKey.Escape){}

        // Отправляем запрос для остановки работы клиента.
        cts.Cancel();
    }

    /// <summary>
    /// Обработчик события получения сообщения.
    /// </summary>
    /// <param name="botClient">Клиент, который получил сообщение</param>
    /// <param name="update">Событие, произошедшее в чате. Новое сообщение, голос в опросе, исключение из чата и т. д.</param>
    /// <param name="cancellationToken">Служебный токен для работы с многопоточностью</param>
    async Task OnMessageReceived(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {

        // Работаем только с сообщениями. Остальные события игнорируем
        var message = update.Message;
        if (message is null)
        {
            return;
        }
        // Будем обрабатывать только текстовые сообщения.
        // При желании можно обрабатывать стикеры, фото, голосовые и т. д.
        //
        // Обратите внимание на использованную конструкцию. Она эквивалентна проверке на null, приведённой выше.
        // Подробнее об этом синтаксисе: https://medium.com/@mattkenefick/snippets-in-c-more-ways-to-check-for-null-4eb735594c09
        if (message.Text is not { } messageText)
        {
            return;
        }
        // Получаем ID чата, в которое пришло сообщение. Полезно, чтобы отличать пользователей друг от друга.
        var chatId = message.Chat.Id;
        user curr_user = new user(chatId, true, true);
        if (all_users.Any(user => user.id == chatId))
        {
            curr_user = all_users.Find(x => x.id == chatId);
        }
        else
        {
            all_users.Add(curr_user);
            using (var sr = new StreamWriter("user_data.json"))
            {
                sr.Write(JsonSerializer.Serialize<user[]>(all_users.ToArray()));
            }

        }
        // Печатаем на консоль факт получения сообщения
        Console.WriteLine($"Получено сообщение в чате {chatId}: '{messageText}'");
        var hdClient = Helldivers2API.Joel.Instance.SetWarId(801);
        if (message.Text is "/start") 
        { 
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Im HellDivers 2 Assistant.\n There you can receive ingame information.\n (updates ~5 minutes)",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }
        else if (message.Text is "Statistic")
        {
            await botClient.SendTextMessageAsync(chatId: chatId, text: $"""
                Missions won: {hdClient.GetWarStats().GalaxyStats.MissionsWon}
                Missions lost: {hdClient.GetWarStats().GalaxyStats.MissionsLost}
                Bugs killed: {hdClient.GetWarStats().GalaxyStats.BugKills}
                Automatons killed: {hdClient.GetWarStats().GalaxyStats.AutomatonKills}
                Allies killed: {hdClient.GetWarStats().GalaxyStats.Friendlies}
                ??Illuminaty?? killed: {hdClient.GetWarStats().GalaxyStats.IlluminateKills}
                """, cancellationToken: cancellationToken);
        }
        else if (message.Text is "Mission")
        {
            
            var assignments = hdClient.GetAssignments();
            if (assignments.Length == 0)
            {
                await botClient.SendTextMessageAsync(chatId: chatId, text: $"""
                Theres no information about current mission.
                Please await instructions from central command.
                """, cancellationToken: cancellationToken);
            }
            foreach (var assignment in assignments) 
            {
                foreach (var x in assignment.Progress)
                {
                    
                }
                await botClient.SendTextMessageAsync(chatId: chatId, text: $"""
                Current main mission:
                {assignment.Brief}
                {assignment.Description}
                Progress: {assignment.Progress[0]}
                """, cancellationToken: cancellationToken);
            }
        }
        else if (message.Text is "Planets")
        {
            var planets = hdClient.GetCampaignPlanets();
            var factions = hdClient.GetFactions();
            if (planets.Length == 0)
            {
                await botClient.SendTextMessageAsync(chatId: chatId, text: $"""
                There is no available planets to dive
                """, cancellationToken: cancellationToken);
            }
            foreach (var item in planets)
            {
                if((curr_user.bugs &&(item.Owner()?.Name == "Terminids"))|| (curr_user.automatons && (item.Owner()?.Name == "Automaton"))||item.Owner()?.Name == "Humans")
                {
                    if (item.Environments.Length == 0)
                        await botClient.SendTextMessageAsync(chatId: chatId, text: $"""
                 Planet: {item.Name}
                Progress: {item.PlanetState().Progress * 100}
                Current state: {item.PlanetState().State}
                Owner: {item.Owner().Name}
                """, cancellationToken: cancellationToken);
                    else
                    {
                        var conditions = "";
                        foreach (var cond in item.Environments)
                        {
                            conditions += cond?.Name ?? "no effect";
                        }
                        await botClient.SendTextMessageAsync(chatId: chatId, text: $"""
                Planet: {item.Name}
                Owner: {item.Owner().Name}
                Current state: {item.PlanetState().State}
                Progress: {item.PlanetState().Progress * 100}
                Active Helldivers: {item.PlayerCount() ?? 0}
                Effects: {conditions}
                """, cancellationToken: cancellationToken);
                    }
                }
            }
        }
        else if (message.Text is "Online")
        {
            int players_count = 0;
            foreach (var planet in hdClient.GetPlanets())
            {
                players_count += planet.PlayerCount()??0;
            }
            await botClient.SendTextMessageAsync(chatId: chatId, text: $"""
                Players online: {players_count}
                """, cancellationToken: cancellationToken);
        }
        else if (message.Text == "Settings")
        {
            await botClient.SendTextMessageAsync(chatId: chatId, text: $"""
            Its your settings:
            {(curr_user.bugs? "You receive information about planets with Bugs":"You not receive information about planets with Bugs")}
            {(curr_user.automatons ? "You receive information about planets with Automatons" : "You not receive information about planets with Automatons")}
            """, replyMarkup: settingsMarkup, cancellationToken: cancellationToken);
        }
        else if (message.Text == "Back")
        {
            await botClient.SendTextMessageAsync(chatId: chatId, text: $"""
            Keep your information updated, soldier!
            """, replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);
        }
        else if (message.Text == "Bugs")
        {
            if (curr_user.bugs == true)
            {
                await botClient.SendTextMessageAsync(chatId: chatId, text: $"""
            You  will no longer receive bugs planet info
            """, replyMarkup: settingsMarkup, cancellationToken: cancellationToken);
                curr_user.bugs = false;
                all_users.Find(user => user.id == curr_user.id).bugs = false;
                using (var sr = new StreamWriter("user_data.json"))
                {
                    sr.Write(JsonSerializer.Serialize<user[]>(all_users.ToArray()));
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: chatId, text: $"""
            Now, you will receive bugs planet info
            """, replyMarkup: settingsMarkup, cancellationToken: cancellationToken);
                curr_user.bugs = true;
                all_users.Find(user => user.id == curr_user.id).bugs = true;
                using (var sr = new StreamWriter("user_data.json"))
                {
                    sr.Write(JsonSerializer.Serialize<user[]>(all_users.ToArray()));
                }
            }
        }
        else if (message.Text == "Automatons")
        {
            if (curr_user.automatons == true)
            {
                await botClient.SendTextMessageAsync(chatId: chatId, text: $"""
            You  will no longer receive Automatons planet info
            """, replyMarkup: settingsMarkup, cancellationToken: cancellationToken);
                curr_user.automatons = false;
                all_users.Find(user => user.id == curr_user.id).automatons = false;
                using (var sr = new StreamWriter("user_data.json"))
                {
                    sr.Write(JsonSerializer.Serialize<user[]>(all_users.ToArray()));
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: chatId, text: $"""
            Now, you will receive Automatons planet info
            """, replyMarkup: settingsMarkup, cancellationToken: cancellationToken);
                curr_user.automatons = true;
                all_users.Find(user => user.id == curr_user.id).automatons = true;
                using (var sr = new StreamWriter("user_data.json"))
                {
                    sr.Write(JsonSerializer.Serialize<user[]>(all_users.ToArray()));
                }
            }
        }
        else if (Regex.IsMatch(messageText, @"\\b[Ii]{1}lluminat\\w*\\b"))
        {
            await botClient.SendTextMessageAsync(chatId: chatId, text: $"""
            The Illuminati does not exist, we are already in contact with your democracy officer
            please remain where you are.
            """, replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);
        }
        else 
        {
            Random rn = new Random();
            await botClient.SendTextMessageAsync(chatId: chatId, text: message_not_match[rn.Next(0, message_not_match.Length)]
            , replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);
        }
        
        //await botClient.SendTextMessageAsync(chatId: chatId, text: $"""
        //""", cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обработчик исключений, возникших при работе бота
    /// </summary>
    /// <param name="botClient">Клиент, для которого возникло исключение</param>
    /// <param name="exception">Возникшее исключение</param>
    /// <param name="cancellationToken">Служебный токен для работы с многопоточностью</param>
    /// <returns></returns>
    Task OnErrorOccured(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        // В зависимости от типа исключения печатаем различные сообщения об ошибке
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        
        // Завершаем работу
        return Task.CompletedTask;
    }
}