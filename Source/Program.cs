using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public static class Program
{
    private static bool canStop;
    private static TelegramBotClient bot;
    private static List<ChatMessage> chatHistory = new();
    private static IChatClient chatClient;

    private static async Task Main(string[] args)
    {
        // Your token
        var token = "";

        using var cts = new CancellationTokenSource();

        bot = new TelegramBotClient(token, cancellationToken: cts.Token);
        var me = await bot.GetMe();
        bot.OnMessage += OnMessage;

        if (canStop) cts.Cancel(); 
        
        await Task.Delay(-1);
    }

    private static async Task OnMessage(Message msg, UpdateType type)
    {
        if (msg.Text == "/start")
        {
            var sent = await bot.SendMessage(msg.Chat, "Choose the model", replyMarkup: new string[]
            {
                "deepseek-r1:7b",
                "qwen2.5:3b"
            });
        }

        else if (msg.Text == null) return;

        else if (msg.Text == "deepseek-r1:7b" || msg.Text == "qwen2.5:3b") 
        { 
            InitOllama(msg.Text);
            await bot.SendMessage(msg.Chat, $"Hi! I'm {msg.Text}! How can I help you today?");
        }

        else if(msg.Text == "/stop") canStop = true;

        else
        {
            var response = await SendOllamaMessage(msg.Text);
            await bot.SendMessage(msg.Chat, response);
        }
    }

    private static async Task<string> SendOllamaMessage(string message)
    {
        chatHistory.Add(new(ChatRole.User, message));

        var chatRespone = "";
        await foreach (var item in chatClient.GetStreamingResponseAsync(chatHistory)) chatRespone += item.Text;
        
        chatHistory.Add(new(ChatRole.Assistant, chatRespone));

        return chatRespone;
    }

    private static void InitOllama(string modelName)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddChatClient(new OllamaChatClient(new Uri("http://localhost:11434"), modelName));

        var app = builder.Build();

        chatClient = app.Services.GetRequiredService<IChatClient>();
    }
}