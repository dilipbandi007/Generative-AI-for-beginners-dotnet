using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using System.ClientModel;
using System.ComponentModel;
using Azure;
using Azure.AI.Inference;
using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;



/*var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var endpoint = config["endpoint"];
var apiKey = new ApiKeyCredential(config["apikey"]);
var deploymentName = "gpt-4.1-mini";

IChatClient client = new AzureOpenAIClient(new Uri(endpoint), apiKey)
    .GetChatClient(deploymentName)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build(); */


// Azure OpenAI configuration
var endpoint = new Uri(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"));
 Console.WriteLine($"Fetching endpoint: {endpoint}");

var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL_DEPLOYMENT");

// Try to get API key from Key Vault first, then fall back to environment variable
string? apiKeyValue = null;
var keyVaultUri = Environment.GetEnvironmentVariable("KEYVAULT_URI");

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


var chatOptions = new ChatOptions
{
    Tools = [AIFunctionFactory.Create(GetWeather)],
    ModelId = deploymentName
};

IChatClient client = new AzureOpenAIClient(
    endpoint,
    apiKey)
.GetChatClient(deploymentName)
.AsIChatClient()
.AsBuilder()
.UseFunctionInvocation()
.Build();

var funcCallingResponseOne = await client.GetResponseAsync("What is today's date?", chatOptions);
var funcCallingResponseTwo = await client.GetResponseAsync("Why don't you tell me about today's temperature?", chatOptions);
var funcCallingResponseThree = await client.GetResponseAsync("Should I bring an umbrella with me today?", chatOptions);

Console.WriteLine($"Response 1: {funcCallingResponseOne}");
Console.WriteLine($"Response 2: {funcCallingResponseTwo}");
Console.WriteLine($"Response 3: {funcCallingResponseThree}");

[Description("Get the weather")]
static string GetWeather()
{
    var temperature = Random.Shared.Next(5, 20);
    var condition = Random.Shared.Next(0, 1) == 0 ? "sunny" : "rainy";
    return $"The weather is {temperature} degree C and {condition}";
}