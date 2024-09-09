# Azure OpenAI Integration with ASP.NET Core

This project demonstrates how to integrate Azure OpenAI services with an ASP.NET Core web application. It includes endpoints for chat completion and image generation using Azure OpenAI.

# Azure OpenAI Integration with ASP.NET Core

This project demonstrates how to integrate Azure OpenAI services with an ASP.NET Core web application. It includes a combined endpoint for chat completion and image generation using Azure OpenAI.

## Prerequisites

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- Azure subscription with OpenAI and Azure AI Search services enabled
- Environment variables set for `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_KEY`, `SEARCH_KEY`, `AZURE_SEARCH_ENDPOINT`, and `AZURE_SEARCH_INDEX_NAME`

## Setup

1. Clone the repository:
   ```sh
   git clone <repository-url>
   cd <repository-directory>
   ```

2. Set the required environment variables:
   ```sh
    setx AZURE_OPENAI_ENDPOINT "<your-azure-openai-endpoint>"
    setx AZURE_OPENAI_KEY "<your-azure-openai-key>"
    setx SEARCH_KEY "<your-azure-ai-search-key>"
    setx AZURE_SEARCH_ENDPOINT "<your-azure-search-endpoint>"
    setx AZURE_SEARCH_INDEX_NAME "<your-azure-search-index-name>"
   ```

3. Restore the dependencies and run the application:
   ```sh
   dotnet restore
   dotnet run
   ```

## Endpoints

### Chat Completion

- **URL:** `/chat`
- **Method:** `POST`
- **Description:** Accepts a user message and returns a chat completion response.
- **Request Body:** Plain text containing the user's message.
- **Response:** Plain text containing the chat completion response.

### Image Generation

- **URL:** `/generate-image`
- **Method:** `POST`
- **Description:** Accepts a user prompt and returns a generated image URL.
- **Request Body:** Plain text containing the user's prompt.
- **Response:** Plain text containing the URL of the generated image.

## Code Overview

### 

Program.cs



- **Service Registration:**
  ```csharp
  builder.Services.AddSingleton(sp =>
  {
      string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
      AzureKeyCredential credential = new(Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY"));
      return new AzureOpenAIClient(new Uri(endpoint), credential);
  });
  ```

- **Chat Endpoint:**
  ```csharp
    app.MapPost("/chat", async (HttpContext context) =>
    {
        string userMessage = await new StreamReader(context.Request.Body).ReadToEndAsync();

        var client = context.RequestServices.GetRequiredService<AzureOpenAIClient>();
        var chatClient = client.GetChatClient("gpt-4o-mini-2024-07-08"); // Replace with your desired deployment

        ChatCompletionOptions options = new();
        string searchEndpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_ENDPOINT");
        options.AddDataSource(new AzureSearchChatDataSource()
        {
            Endpoint = new Uri(searchEndpoint), // Set the endpoint property
            IndexName = Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX_NAME"), // Replace with your Azure AI Search index name
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
    await context.Response.WriteAsync(response);
    });
  ```

- **Image Generation Endpoint:**
  ```csharp
  app.MapPost("/generate-image", async (HttpContext context) =>
  {
      string userPrompt = await new StreamReader(context.Request.Body).ReadToEndAsync();
      var client = context.RequestServices.GetRequiredService<AzureOpenAIClient>();
      var imageClient = client.GetImageClient("dall-e-3");

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
          Console.WriteLine($"Image generation failed: {ex.Message}");
          await context.Response.WriteAsync("Image generation failed.");
      }
  });
  ```

## License

This project is licensed under the MIT License. See the LICENSE file for details.