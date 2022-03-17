using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ValenciaBot;

public class TelegramBot : IDisposable
{
    private TelegramBotClient _client;
    private CancellationTokenSource _cancellationToken = new();

    private string _subscribersFilePath;

    private HashSet<long> _subscribers = new();

    public TelegramBot(string token, string subscribersFilePath)
    {
        _client = new TelegramBotClient(token);
        _subscribersFilePath = Path.GetFullPath(subscribersFilePath);
        LoadSubscribers();
        _client.StartReceiving(HandleUpdateAsync, HandleErrorAsync, null, _cancellationToken.Token);
    }

    private void LoadSubscribers()
    {
        if(System.IO.File.Exists(_subscribersFilePath) == false)
            return;
        string[] lines = System.IO.File.ReadAllLines(_subscribersFilePath);
        foreach (string line in lines)
            _subscribers.Add(long.Parse(line));
    }
    private bool AddSubscriber(long id)
    {
        bool added = _subscribers.Add(id);
        if(added)
            System.IO.File.AppendAllText(_subscribersFilePath, $"{id}\n");
        return added;
    }

    public async void SendMessageToSubscribers(string message)
    {
        foreach(var subscriber in _subscribers)
            await _client.SendTextMessageAsync(subscriber, message);
    }

    public void Dispose() => _cancellationToken.Cancel();

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var handler = update.Type switch
        {
            UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
            _ => UnknownUpdateHandlerAsync(botClient, update)
        };

        try
        {
            await handler;
        }
        catch(Exception exception)
        {
            await HandleErrorAsync(botClient, exception, cancellationToken);
        }
    }


    private async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
    {
        bool added = AddSubscriber(message.Chat.Id);
        string replyMessage = added ? "You have been subscribed!" : "You are already subscribed!";
        await _client.SendTextMessageAsync(message.Chat.Id, replyMessage);
    }

    private static Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
    {
        Console.WriteLine($"Unknown update type: {update.Type}");
        return Task.CompletedTask;
    }
}
