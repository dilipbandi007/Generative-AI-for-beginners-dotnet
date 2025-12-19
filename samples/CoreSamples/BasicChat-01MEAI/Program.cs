using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Azure;
using Azure.AI.Inference;
using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

/*var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
if(string.IsNullOrEmpty(githubToken))
{
    var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
    githubToken = config["GITHUB_TOKEN"];
}

IChatClient client = new ChatCompletionsClient(
        endpoint: new Uri("https://models.github.ai/inference"),
        new AzureKeyCredential(githubToken))
        .AsIChatClient("Phi-4-mini-instruct");*/

// Azure OpenAI configuration
var endpoint = new Uri(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"));
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

IChatClient client = new AzureOpenAIClient(
    endpoint,
    apiKey)
.GetChatClient(deploymentName)
.AsIChatClient();


// here we're building the prompt
StringBuilder prompt = new StringBuilder();
prompt.AppendLine("What is the distance from Earth to Moon?");

// send the prompt to the model and wait for the text completion
var response = await client.GetResponseAsync(prompt.ToString());

// display the response
Console.WriteLine(response.Text);
