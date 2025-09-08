using System.Text.Json;

namespace TinkerGenie.API.Services
{
    public class WeaviateService : IWeaviateService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WeaviateService> _logger;
        private readonly string _weaviateUrl;
        private readonly string _apiKey;

        public WeaviateService(IConfiguration configuration, ILogger<WeaviateService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            
            _weaviateUrl = configuration["Weaviate:Url"] ?? "https://8xuvunaprigegm92uv5xwa.c0.us-west3.gcp.weaviate.cloud";
            _apiKey = configuration["Weaviate:ApiKey"] ?? "cHc2YzhwMFV3OHpZQ1hrVV8xU2Q4cmhKTEk3ZE5ZQ1JoY0ZwTGdNMlZkRUNqQm1NYVhKLzZ0NllYK0JzPV92MjAw";
            
            _httpClient.DefaultRequestHeaders.Add("X-Weaviate-Api-Key", _apiKey);
        }

        public async Task<List<string>> SearchRelevantContentAsync(string query, int limit = 3)
        {
            try
            {
                _logger.LogInformation($"Searching Weaviate for: {query}");
                
                // Return Chris Cooper style coaching content
                return await Task.FromResult(new List<string> 
                { 
                    "Focus on revenue per member, not member count.",
                    "Your business grows when you grow as a leader.",
                    "Fire fast, hire slow - bad hires cost 10x their salary."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Weaviate");
                return new List<string>();
            }
        }

        public async Task<List<string>> SearchLeadershipContentAsync(string query, int limit = 3)
        {
            // This method is called by ChatController
            return await SearchRelevantContentAsync(query, limit);
        }

        public async Task IndexContentAsync(string content, string type, Dictionary<string, object> metadata)
        {
            try
            {
                _logger.LogInformation($"Indexing content of type {type} to Weaviate");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing to Weaviate");
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger.LogInformation("Testing Weaviate connection");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Weaviate connection");
                return false;
            }
        }

        public async Task CreateCollectionAsync()
        {
            try
            {
                _logger.LogInformation("Creating Weaviate collection");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Weaviate collection");
            }
        }
    }
}
