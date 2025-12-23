using Microsoft.Extensions.Configuration;
using Azure.AI.Inference;
using Azure;
using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;




// var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
// if (string.IsNullOrEmpty(githubToken))
// {
//     var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
//     githubToken = config["GITHUB_TOKEN"];
// }

// IChatClient chatClient =
//     new ChatCompletionsClient(
//         endpoint: new Uri("https://models.github.ai/inference"),
//         new AzureKeyCredential(githubToken))
//         .AsIChatClient("gpt-4o-mini");

// Azure OpenAI configuration
var endpoint = new Uri(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"));
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL_DEPLOYMENT");
// Try to get API key from Key Vault first, then fall back to environment variable
string? apiKeyValue = null;
var keyVaultUri = Environment.GetEnvironmentVariable("KEYVAULT_URI");

// Log environment/config values for debugging
Console.WriteLine($"AZURE_OPENAI_ENDPOINT: {endpoint}");
Console.WriteLine($"AZURE_OPENAI_MODEL_DEPLOYMENT: {deploymentName}");
Console.WriteLine($"KEYVAULT_URI: {keyVaultUri}");

if (!string.IsNullOrEmpty(keyVaultUri))
{
    try
    {
        Console.WriteLine($"Fetching secret from Key Vault: {keyVaultUri}");
        var secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
        var secret = await secretClient.GetSecretAsync("azure-myopenai-ch-learn-key");
        apiKeyValue = secret.Value.Value;
        Console.WriteLine("Successfully retrieved secret from Key Vault.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Key Vault error: {ex.Message}");
    }
}

if (string.IsNullOrEmpty(apiKeyValue))
{
    Console.WriteLine("Error: Could not get API key from Key Vault");
    return;
}

var apiKey = new ApiKeyCredential(apiKeyValue);


IChatClient  chatClient = new AzureOpenAIClient(
    endpoint,
    apiKey)
.GetChatClient(deploymentName)
.AsIChatClient();

// images
string imgRunningShoes = "running-shoes.jpg";
string imgCarLicense = "license.jpg";
string imgReceipt = "german-receipt.jpg";

// prompts
var promptDescribe = "Describe the image";
var promptAnalyze = "How many red shoes are in the picture? and what other shoes colors are there?";
var promptOcr = "What is the text in this picture? Is there a theme for this?";
var promptReceipt = "I bought the coffee and the sausage. Make sure you identify the coffee and sausage in receipt. How much do I owe? Add a 18% tip.";

// prompts
string systemPrompt = @"You are a useful assistant that describes images using a direct style.";
var prompt = promptReceipt;
string imageFileName = imgReceipt;
string image = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName, "images", imageFileName);


List<ChatMessage> messages =
[
    new ChatMessage(Microsoft.Extensions.AI.ChatRole.System, systemPrompt),
    new ChatMessage(Microsoft.Extensions.AI.ChatRole.User, prompt),
];

// read the image bytes, create a new image content part and add it to the messages
AIContent aic = new DataContent(File.ReadAllBytes(image), "image/jpeg");
var message = new ChatMessage(Microsoft.Extensions.AI.ChatRole.User, [aic]);
    messages.Add(message);

// send the messages to the assistant
var response = await chatClient.GetResponseAsync(messages);
Console.WriteLine($"Prompt: {prompt}");
Console.WriteLine($"Image: {imageFileName}");
Console.WriteLine($"Response: {response.Text}");

// If you need to use .GetChatClient or .AsIChatClient, check the latest SDK docs for the correct extension method or usage.
