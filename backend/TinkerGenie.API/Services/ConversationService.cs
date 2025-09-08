using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace TinkerGenie.API.Services
{
    public class ConversationService : IConversationService
    {
        private readonly ILogger<ConversationService> _logger;
        
        public ConversationService(ILogger<ConversationService> logger)
        {
            _logger = logger;
        }
        
        public async Task<Guid> SaveConversation(string userId, string userMessage, string aiResponse)
        {
            // TODO: Save to database
            var conversationId = Guid.NewGuid();
            _logger.LogInformation($"Saved conversation {conversationId} for user {userId}");
            return await Task.FromResult(conversationId);
        }
        
        public async Task<List<ConversationMessage>> GetConversationHistory(string userId, int limit = 10)
        {
            // TODO: Get from database
            return await Task.FromResult(new List<ConversationMessage>());
        }
    }
}
