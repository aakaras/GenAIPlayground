using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Environment;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddSingleton(sp =>
{
 string endpoint = GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
 AzureKeyCredential credential = new(GetEnvironmentVariable("AZURE_OPENAI_KEY"));
 return new AzureOpenAIClient(new Uri(endpoint), credential);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
 app.UseExceptionHandler("/Error");
 // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
 app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapFallbackToFile("Index.cshtml"); // Handle default route

app.MapPost("/chat", async (HttpContext context) =>
{
 string userMessage = await new StreamReader(context.Request.Body).ReadToEndAsync();

 var client = context.RequestServices.GetRequiredService<AzureOpenAIClient>();
 var chatClient = client.GetChatClient("gpt-4o-mini-2024-07-08"); // Replace with your desired deployment

 ChatCompletionOptions options = new();
 options.AddDataSource(new AzureSearchChatDataSource()
 {
     Endpoint = new Uri("https://whoamisearch.search.windows.net"), // Replace with your Azure AI Search endpoint
     IndexName = "profile2", // Replace with your Azure AI Search index name
     Authentication = DataSourceAuthentication.FromApiKey(GetEnvironmentVariable("SEARCH_KEY")) // Replace with your Azure AI Search admin key
 });

 ChatCompletion completion = await chatClient.CompleteChatAsync(
     new List<ChatMessage>()
     {
         new UserChatMessage(userMessage)
     },
     options
 );

 AzureChatMessageContext onYourDataContext = completion.GetAzureMessageContext();

 string response = completion.Content[0].Text;
 if (onYourDataContext?.Intent is not null)
 {
     //response += $"\nIntent: {onYourDataContext.Intent}";
 }
 foreach (AzureChatCitation citation in onYourDataContext?.Citations ?? new List<AzureChatCitation>())
 {
     //response += $"\nCitation: {citation.Content}";
 }

 await context.Response.WriteAsync(response);
});

app.Run();