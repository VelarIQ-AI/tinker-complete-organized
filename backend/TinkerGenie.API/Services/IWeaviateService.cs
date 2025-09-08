using System.Collections.Generic;
using System.Threading.Tasks;

namespace TinkerGenie.API.Services
{
    public interface IWeaviateService
    {
        Task<List<string>> SearchRelevantContentAsync(string query, int limit = 3);
        Task<List<string>> SearchLeadershipContentAsync(string query, int limit = 3);
        Task IndexContentAsync(string content, string type, Dictionary<string, object> metadata);
        Task<bool> TestConnectionAsync();
        Task CreateCollectionAsync();
    }
}
