using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OllamaSharp;
using OllamaSharp.Models;
using OpenAI.RealtimeConversation;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

#pragma warning disable
await Call();
//await ImageCall();
Console.ReadLine();


async Task ImageCall()
{

    var builder = Kernel.CreateBuilder();
    var modelId = "llama3.2-vision";
    var endpoint = new Uri("http://localhost:11434");
    builder.Services.AddOllamaChatCompletion(modelId, endpoint);

    var kernel = builder.Build();

    var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
    var chatHistory = new ChatHistory();
    chatHistory.AddUserMessage(new ChatMessageContentItemCollection{
         new TextContent("Does this image contain violence, gore, or adult content? Please answer with 'yes' or 'no' only."),
         new ImageContent(File.ReadAllBytes("pig1.jpg"),"image/jpeg")
    });

    try
    {
        var result = chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory);
        await foreach (var message in result)
        {
            Console.Write($"{message.Content}");
        }
        //ChatMessageContent chatResult = await chatCompletionService.GetChatMessageContentAsync(input, settings, kernel);
        //Console.Write($"\n>>> Result: {chatResult}\n\n> ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}\n\n> ");
    }

}


async Task Call()
{

    var builder = Kernel.CreateBuilder();
    var modelId = "qwen2.5";
    var endpoint = new Uri("http://localhost:11434");
    builder.Services.AddOllamaChatCompletion(modelId, endpoint);

    builder.Plugins.AddFromType<TimePlugin>();
    builder.Plugins.AddFromType<OrderPlugin>();
    var kernel = builder.Build();

    var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
    var settings = new OllamaPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };

    Console.Write(">>> ");

    string? input = "获取订单编号为SN0000111的订单总额？";
     // var input = "获取当前时间的订单总额？";
    //var input = "获取当前时间";
    //input = "介绍一下自己吧";
    Console.WriteLine(input);

    try
    {
        //var result = chatCompletionService.GetStreamingChatMessageContentsAsync(input, settings, kernel);
        //await foreach (var message in result)
        //{
        //    Console.Write($"\n>>> Result: {message.Content}\n\n> ");
        //}
        ChatMessageContent chatResult = await chatCompletionService.GetChatMessageContentAsync(input, settings, kernel);
        Console.Write($"\n>>> Result: {chatResult}\n\n> ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}\n\n> ");
    }

}

async Task Call1()
{
    var ollamaApiClient = new OllamaApiClient(new Uri("http://localhost:11434"), "phi4-mini:latest");
    var builder = Kernel.CreateBuilder();
    builder.Services.AddScoped<IChatCompletionService>(_ => ollamaApiClient.AsChatCompletionService());
    var kernel = builder.Build();
    var chatService = kernel.GetRequiredService<IChatCompletionService>();
    var settings = new OllamaPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
    kernel.Plugins.AddFromType<TimePlugin>();


    kernel.Plugins.AddFromFunctions("time_plugin",
    [
        KernelFunctionFactory.CreateFromMethod(
        method: () => DateTime.Now,
        functionName: "get_time",
        description: "获取当前时间"
    ),
    ]);

    var history = new ChatHistory();
    history.AddSystemMessage("你是一个知识渊博的助手");
    while (true)
    {
        Console.Write("用户：");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            break;
        }
        //history.AddUserMessage(input);
        var response = chatService.GetStreamingChatMessageContentsAsync(input);
        //var response = chatService.GetStreamingChatMessageContentsAsync(history, settings, kernel);
        var content = "";
        var role = AuthorRole.Assistant;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("助手：");
        await foreach (var message in response)
        {
            Console.Write($"{message.Content}");
            content += message.Content;
            role = message.Role.Value;
        }
        Console.WriteLine();
        Console.ResetColor();
        //history.AddMessage(role, content);
    }
}

public class TimePlugin
{
    [KernelFunction]
    [Description("获取当前时间")]
    public DateTime GetCurrentTime()
    {
        Console.WriteLine(DateTime.Now);
        return DateTime.Now;
    }
}
public class OrderPlugin
{
    [KernelFunction]
    [Description("按单号获取订单总额")]
    public decimal GetOrderAmountByNo([Description("订单编号")] string orderNo)
    {
        Console.WriteLine($"订单编号：{orderNo}，订单：12345.67");
        return 12345.67m;
    }
    [KernelFunction]
    [Description("按时间获取订单总额")]
    public decimal GetOrderAmountByTime([Description("时间")] DateTime time)
    {
        Console.WriteLine($"订单时间：{time}，订单：76543.21");
        return 76543.21m;
    }
}

