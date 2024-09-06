using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using Azure.AI.OpenAI.Images;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using OpenAI.Chat;
using OpenAI.Images;
using System; // Added for Environment.GetEnvironmentVariable
using System.Collections.Generic;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Register the AzureOpenAIClient as a singleton
builder.Services.AddSingleton(sp =>
{
 string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
 AzureKeyCredential credential = new(Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY"));
 return new AzureOpenAIClient(new Uri(endpoint), credential);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
 app.UseExceptionHandler("/Error");
 app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapFallbackToFile("Index.cshtml");

// Chat endpoint
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
     Authentication = DataSourceAuthentication.FromApiKey(Environment.GetEnvironmentVariable("SEARCH_KEY")) // Replace with your Azure AI Search admin key
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

// Image generation endpoint
app.MapPost("/generate-image", async (HttpContext context) =>
{
 string userPrompt = await new StreamReader(context.Request.Body).ReadToEndAsync();

 var client = context.RequestServices.GetRequiredService<AzureOpenAIClient>();
 var imageClient = client.GetImageClient("dall-e-3"); // Replace with your desired deployment

 // GenerateImageAsync now directly returns GeneratedImage (or throws an exception)
 try
 {
     GeneratedImage image = await imageClient.GenerateImageAsync(userPrompt, new()
     {
         Quality = GeneratedImageQuality.Standard,
         Size = GeneratedImageSize.W1024xH1024,
         ResponseFormat = GeneratedImageFormat.Uri
     });

     await context.Response.WriteAsync($"Image URL: {image.ImageUri}");
 }
 catch (Exception ex)
 {
     // Handle the exception, e.g., log the error and send an error response
     Console.WriteLine($"Image generation failed: {ex.Message}");
     await context.Response.WriteAsync("Image generation failed.");
 }
});

app.Run();