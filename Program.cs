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
using DotNetEnv;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
 serverOptions.ListenAnyIP(5062); // Listen on any IP address on port 5062
});

// Key Vault Configuration
string keyVaultName = "kv-genai-demo-swd"; // Your Key Vault name
string endpointSecretName = "AZURE-OPENAI-ENDPOINT"; // Azure OpenAI Endpoint
string apiKeySecretName = "AZURE-OPENAI-KEY"; //New secret for Azure OpenAI
string searchEndpointSecretName = "AZURE-SEARCH-ENDPOINT"; //Azure Search Endpoint
string searchIndexNameSecretName = "AZURE-SEARCH-INDEX-NAME"; // New secret for index name
string searchKeySecretName = "SEARCH-KEY"; // New secret for search key

// Get secrets from Key Vault 
var credential = new DefaultAzureCredential();
var client = new SecretClient(new Uri($"https://{keyVaultName}.vault.azure.net/"), credential);

string endpoint = (await client.GetSecretAsync(endpointSecretName)).Value.Value; // Get endpoint from Key Vault
string apiKey = (await client.GetSecretAsync(apiKeySecretName)).Value.Value; // Get API key from Key Vault
string searchEndpoint = (await client.GetSecretAsync(searchEndpointSecretName)).Value.Value; // Get search endpoint from Key Vault
string searchIndexName = (await client.GetSecretAsync(searchIndexNameSecretName)).Value.Value; // Get index name from Key Vault
string searchKey = (await client.GetSecretAsync(searchKeySecretName)).Value.Value;           // Get search key from Key Vault



// Register the AzureOpenAIClient as a singleton
builder.Services.AddSingleton(sp =>
{
 AzureKeyCredential azureKeyCredential = new(apiKey);
 return new AzureOpenAIClient(new Uri(endpoint), azureKeyCredential);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
 app.UseExceptionHandler("/Error");
 app.UseHsts();
}

//app.UseHttpsRedirection();
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
    var chatClient = client.GetChatClient("gpt-4o-mini-deployment"); // Replace with your desired deployment
    
 ChatCompletionOptions options = new();
 options.AddDataSource(new AzureSearchChatDataSource()
 {
     Endpoint = new Uri(searchEndpoint),
     IndexName = searchIndexName, // Use index name from Key Vault
     Authentication = DataSourceAuthentication.FromApiKey(searchKey) // Use search key from Key Vault
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