using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Azure;
using Azure.AI.Inference;
using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using VectorDataAI;

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

List<CloudService> cloudServices = new List<CloudService>
{
    new() {
        Key = 0,
        Name = "Azure App Service",
        Description = "Host .NET, Java, Node.js, and Python web applications and APIs in a fully managed Azure service. You only need to deploy your code to Azure. Azure takes care of all the infrastructure management like high availability, load balancing, and autoscaling."
    },
    new() {
        Key = 1,
        Name = "Azure Service Bus",
        Description = "A fully managed enterprise message broker supporting both point to point and publish-subscribe integrations. It's ideal for building decoupled applications, queue-based load leveling, or facilitating communication between microservices."
    },
    new() {
        Key = 2,
        Name = "Azure Blob Storage",
        Description = "Azure Blob Storage allows your applications to store and retrieve files in the cloud. Azure Storage is highly scalable to store massive amounts of data and data is stored redundantly to ensure high availability."
    },
    new() {
        Key = 3,
        Name = "Microsoft Entra ID",
        Description = "Manage user identities and control access to your apps, data, and resources."
    },
    new() {
        Key = 4,
        Name = "Azure Key Vault",
        Description = "Store and access application secrets like connection strings and API keys in an encrypted vault with restricted access to make sure your secrets and your application aren't compromised."
    },
    new() {
        Key = 5,
        Name = "Azure AI Search",
        Description = "Information retrieval at scale for traditional and conversational search applications, with security and options for AI enrichment and vectorization."
    }
};

// Use only AZURE_OPENAI_MODEL_DEPLOYMENT for embedding deployment
IEmbeddingGenerator<string, Embedding<float>> generator =
    new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
        .GetEmbeddingClient(deploymentName: deploymentName)
        .AsIEmbeddingGenerator();

// Create and populate the vector store.
var vectorStore = new InMemoryVectorStore();
VectorStoreCollection<int, CloudService> cloudServicesStore =
    vectorStore.GetCollection<int, CloudService>("cloudServices");
await cloudServicesStore.EnsureCollectionExistsAsync();

foreach (CloudService service in cloudServices)
{
    service.Vector = await generator.GenerateVectorAsync(service.Description);
    await cloudServicesStore.UpsertAsync(service);
}

// Convert a search query to a vector
// and search the vector store.
string query = "Which Azure service should I use to store my Word documents?";
ReadOnlyMemory<float> queryEmbedding = await generator.GenerateVectorAsync(query);

IAsyncEnumerable<VectorSearchResult<CloudService>> results =
    cloudServicesStore.SearchAsync(queryEmbedding, top: 1);

await foreach (VectorSearchResult<CloudService> result in results)
{
    Console.WriteLine($"Name: {result.Record.Name}");
    Console.WriteLine($"Description: {result.Record.Description}");
    Console.WriteLine($"Vector match score: {result.Score}");
}
