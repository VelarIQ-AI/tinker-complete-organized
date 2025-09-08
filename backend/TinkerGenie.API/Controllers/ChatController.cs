using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;
using Npgsql;
using NpgsqlTypes;
using TinkerGenie.API.Services;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authorization;

namespace TinkerGenie.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChatController> _logger;
        private readonly IWeaviateService? _weaviateService;
        private readonly IConnectionMultiplexer? _redis;
        private readonly string _openAiApiKey;
        private readonly string _connectionString;

        public ChatController(
            IConfiguration configuration, 
            ILogger<ChatController> logger, 
            IConnectionMultiplexer? redis = null,
            IWeaviateService? weaviateService = null)
        {
            _logger = logger;
            _weaviateService = weaviateService;
            _redis = redis;
            _httpClient = new HttpClient();
            _openAiApiKey = configuration["OpenAI:ApiKey"] ?? "";
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _openAiApiKey);
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value ?? request.UserId ?? "anonymous";
                var firstName = User.FindFirst("firstName")?.Value ?? request.UserName ?? "Leader";
                var businessName = User.FindFirst("businessName")?.Value ?? request.BusinessName ?? "your business";
                
                // Get user context from database
                var userContext = await GetUserContext(userId);
                var conversationHistory = await GetConversationHistory(userId, request.ConversationId);
                
                // Get today's daily prompt if appropriate
                var dailyPrompt = await GetDailyPrompt(userContext.CurrentDay, userId);
                
                // Search knowledge base for relevant content (with caching)
                var relevantContent = await GetRelevantKnowledgeBase(request.Message);
                
                // Build comprehensive AI prompt with leadership curriculum context
                var aiPrompt = BuildLeadershipPrompt(request.Message, userContext, conversationHistory, dailyPrompt, relevantContent);
                
                // Get AI response
                var aiResponse = await GetAIResponse(aiPrompt);
                
                // Save the conversation
                var conversationId = await SaveConversation(userId, request.Message, aiResponse, request.ConversationId);
                
                // Track user activity and advance their journey if needed
                await TrackUserActivity(userId, userContext.CurrentDay);
                
                return Ok(new ChatResponse
                {
                    Response = aiResponse,
                    ConversationId = conversationId,
                    CurrentDay = userContext.CurrentDay,
                    DailyPrompt = dailyPrompt,
                    UserProgress = $"Day {userContext.CurrentDay} of 180 - Leadership Development Journey"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in chat endpoint for user: {UserId}", request.UserId);
                return Ok(new ChatResponse
                {
                    Response = "I'm experiencing some technical difficulties, but I'm still here to help with your leadership journey. What would you like to discuss?",
                    ConversationId = Guid.NewGuid().ToString()
                });
            }
        }

        private async Task<UserContext> GetUserContext(string userId)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                
                var cmd = new NpgsqlCommand(@"
                    SELECT 
                        COALESCE(up.first_name, u.username, 'Leader') as first_name,
                        COALESCE(up.business_name, 'Your Business') as business_name,
                        COALESCE(up.current_day, 1) as current_day,
                        COALESCE(up.communication_style, 'balanced') as communication_style,
                        COALESCE(up.preferred_response_length, 'medium') as preferred_response_length,
                        COALESCE(up.journey_paused, false) as journey_paused,
                        COALESCE(up.timezone, 'America/New_York') as timezone
                    FROM users u
                    LEFT JOIN user_profiles up ON u.id::text = up.user_id::text
                    WHERE u.id::text = $1 OR u.email = $1", conn);
                    
                cmd.Parameters.AddWithValue(userId);
                
                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new UserContext
                    {
                        FirstName = reader.GetString(0),
                        BusinessName = reader.GetString(1),
                        CurrentDay = reader.GetInt32(2),
                        CommunicationStyle = reader.GetString(3),
                        PreferredResponseLength = reader.GetString(4),
                        JourneyPaused = reader.GetBoolean(5),
                        Timezone = reader.GetString(6)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user context for {UserId}", userId);
            }

            return new UserContext { FirstName = "Leader", CurrentDay = 1 };
        }

        private async Task<List<ConversationMessage>> GetConversationHistory(string userId, string? conversationId, int limit = 10)
        {
            var messages = new List<ConversationMessage>();
            
            if (string.IsNullOrEmpty(conversationId))
                return messages;

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                
                var cmd = new NpgsqlCommand(@"
                    SELECT sender, message_text, created_at
                    FROM conversation_messages cm
                    JOIN genie_conversations gc ON cm.conversation_id = gc.id
                    WHERE gc.id::text = $1 AND gc.user_id::text = $2
                    ORDER BY cm.created_at DESC
                    LIMIT $3", conn);
                    
                cmd.Parameters.AddWithValue(conversationId);
                cmd.Parameters.AddWithValue(userId);
                cmd.Parameters.AddWithValue(limit);
                
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    messages.Add(new ConversationMessage
                    {
                        Sender = reader.GetString(0),
                        Content = reader.GetString(1),
                        CreatedAt = reader.GetDateTime(2)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation history");
            }

            return messages.OrderBy(m => m.CreatedAt).ToList();
        }

        private async Task<DailyPrompt?> GetDailyPrompt(int currentDay, string userId)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                
                // Check if we've already delivered today's prompt
                var checkCmd = new NpgsqlCommand(@"
                    SELECT id FROM user_prompt_deliveries
                    WHERE user_id::text = $1 AND day_number = $2 
                    AND DATE(delivered_at) = CURRENT_DATE", conn);
                    
                checkCmd.Parameters.AddWithValue(userId);
                checkCmd.Parameters.AddWithValue(currentDay);
                
                var alreadyDelivered = await checkCmd.ExecuteScalarAsync() != null;
                
                if (alreadyDelivered)
                    return null;
                
                // Get the prompt for this day
                var promptCmd = new NpgsqlCommand(@"
                    SELECT prompt_title, prompt_text, 
                           COALESCE(fill_in_blanks_jsonb, '[]'::jsonb),
                           COALESCE(follow_up_questions::jsonb, '[]'::jsonb)
                    FROM leadership_daily_prompts
                    WHERE day_number = $1 AND is_active = true
                    ORDER BY version DESC
                    LIMIT 1", conn);
                
                promptCmd.Parameters.AddWithValue(currentDay);
                
                await using var reader = await promptCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var fillInBlanks = new List<string>();
                    var followUpQuestions = new List<string>();
                    
                    if (!reader.IsDBNull(2))
                    {
                        var fillInBlanksJson = reader.GetString(2);
                        fillInBlanks = JsonSerializer.Deserialize<List<string>>(fillInBlanksJson) ?? new List<string>();
                    }
                    
                    if (!reader.IsDBNull(3))
                    {
                        var followUpJson = reader.GetString(3);
                        followUpQuestions = JsonSerializer.Deserialize<List<string>>(followUpJson) ?? new List<string>();
                    }
                    
                    return new DailyPrompt
                    {
                        PromptTitle = reader.GetString(0),
                        PromptText = reader.GetString(1),
                        FillInBlanks = fillInBlanks,
                        FollowUpQuestions = followUpQuestions
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily prompt for day {Day}", currentDay);
            }

            return null;
        }

        private async Task<List<string>> GetRelevantKnowledgeBase(string query)
        {
            var content = new List<string>();

            // Try Redis cache first
            if (_redis?.IsConnected == true)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    var cacheKey = $"knowledge:{query.GetHashCode()}";
                    var cachedContent = await db.StringGetAsync(cacheKey);
                    
                    if (cachedContent.HasValue)
                    {
                        return JsonSerializer.Deserialize<List<string>>(cachedContent!) ?? new List<string>();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Redis cache error");
                }
            }

            // Try Weaviate search
            if (_weaviateService != null)
            {
                try
                {
                    content = await _weaviateService.SearchRelevantContentAsync(query, 3);
                    
                    // Cache results if we have Redis
                    if (_redis?.IsConnected == true && content.Any())
                    {
                        try
                        {
                            var db = _redis.GetDatabase();
                            var cacheKey = $"knowledge:{query.GetHashCode()}";
                            await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(content), TimeSpan.FromHours(1));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error caching knowledge base results");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Weaviate search error");
                }
            }

            // Fallback: use default leadership content
            if (!content.Any())
            {
                content = new List<string>
                {
                    "Leadership is about creating a clear vision and helping others achieve it.",
                    "Effective leaders focus on developing their team members' strengths.",
                    "Building trust through consistent actions is fundamental to leadership.",
                    "Great leaders ask powerful questions that help people discover solutions.",
                    "Leadership development is a continuous journey of self-improvement and learning."
                };
            }

            return content;
        }

        private string BuildLeadershipPrompt(string userMessage, UserContext userContext, 
            List<ConversationMessage> conversationHistory, DailyPrompt? dailyPrompt, List<string> knowledgeBase)
        {
            var prompt = new StringBuilder();
            
            prompt.AppendLine($"You are TinkerGenie, an AI leadership development coach created by Chris Cooper of Two-Brain Business.");
            prompt.AppendLine($"You help gym owners and entrepreneurs develop their leadership skills through a structured 180-day personal development journey.");
            prompt.AppendLine();
            
            // User context
            prompt.AppendLine($"USER CONTEXT:");
            prompt.AppendLine($"- Name: {userContext.FirstName}");
            prompt.AppendLine($"- Business: {userContext.BusinessName}");
            prompt.AppendLine($"- Leadership Journey Day: {userContext.CurrentDay} of 180");
            prompt.AppendLine($"- Communication Style: {userContext.CommunicationStyle}");
            prompt.AppendLine($"- Preferred Response Length: {userContext.PreferredResponseLength}");
            prompt.AppendLine();

            // Daily prompt context
            if (dailyPrompt != null)
            {
                prompt.AppendLine($"TODAY'S LEADERSHIP PROMPT (Day {userContext.CurrentDay}):");
                prompt.AppendLine($"Title: {dailyPrompt.PromptTitle}");
                prompt.AppendLine($"Content: {dailyPrompt.PromptText}");
                if (dailyPrompt.FillInBlanks.Any())
                {
                    prompt.AppendLine($"Reflection Exercise: {string.Join(", ", dailyPrompt.FillInBlanks)}");
                }
                prompt.AppendLine();
            }

            // Knowledge base context
            if (knowledgeBase.Any())
            {
                prompt.AppendLine("RELEVANT LEADERSHIP CONTENT:");
                foreach (var content in knowledgeBase)
                {
                    prompt.AppendLine($"- {content}");
                }
                prompt.AppendLine();
            }

            // Conversation history
            if (conversationHistory.Any())
            {
                prompt.AppendLine("RECENT CONVERSATION:");
                foreach (var msg in conversationHistory.TakeLast(4))
                {
                    prompt.AppendLine($"{msg.Sender}: {msg.Content}");
                }
                prompt.AppendLine();
            }

            // Response guidelines
            prompt.AppendLine("RESPONSE GUIDELINES:");
            prompt.AppendLine("- Focus on leadership development and personal growth");
            prompt.AppendLine("- Reference Chris Cooper's Two-Brain Business methodology when relevant");
            prompt.AppendLine("- Provide actionable advice for gym owners and entrepreneurs");
            prompt.AppendLine($"- Keep responses {userContext.PreferredResponseLength} length");
            prompt.AppendLine("- Ask follow-up questions to deepen reflection");
            prompt.AppendLine("- Encourage progress in their 180-day leadership journey");
            prompt.AppendLine();
            
            prompt.AppendLine($"USER MESSAGE: {userMessage}");

            return prompt.ToString();
        }

        private async Task<string> GetAIResponse(string prompt)
        {
            try
            {
                var request = new
                {
                    model = "gpt-4",
                    messages = new[]
                    {
                        new { role = "system", content = prompt },
                        new { role = "user", content = "Please provide your leadership coaching response." }
                    },
                    max_tokens = 500,
                    temperature = 0.7
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);
                    return responseObj.GetProperty("choices")[0]
                                     .GetProperty("message")
                                     .GetProperty("content").GetString() ?? 
                           "I'm here to support your leadership development. What specific challenge would you like to work on today?";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API");
            }

            return "I'm experiencing some technical difficulties, but I'm still here to help with your leadership journey. What would you like to discuss today?";
        }

        private async Task<string> SaveConversation(string userId, string userMessage, string aiResponse, string? conversationId)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                
                // Create or get conversation
                if (string.IsNullOrEmpty(conversationId))
                {
                    var createConvCmd = new NpgsqlCommand(@"
                        INSERT INTO genie_conversations (id, user_id, title, status, message_count, started_at, last_message_at)
                        VALUES (gen_random_uuid(), $1::uuid, $2, 'active', 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                        RETURNING id::text", conn);
                    
                    createConvCmd.Parameters.AddWithValue(Guid.TryParse(userId, out var userGuid5) ? userGuid5 : Guid.NewGuid());
                    createConvCmd.Parameters.AddWithValue($"Leadership Chat - {DateTime.UtcNow:yyyy-MM-dd}");
                    
                    conversationId = await createConvCmd.ExecuteScalarAsync() as string ?? Guid.NewGuid().ToString();
                }

                // Save user message
                var saveUserCmd = new NpgsqlCommand(@"
                    INSERT INTO conversation_messages (id, conversation_id, user_id, sender, message_text, content, created_at)
                    VALUES (gen_random_uuid(), $1::uuid, $2::uuid, 'user', $3, $3, CURRENT_TIMESTAMP)", conn);
                
                saveUserCmd.Parameters.AddWithValue(Guid.Parse(conversationId));
                saveUserCmd.Parameters.AddWithValue(Guid.TryParse(userId, out var userGuid4) ? userGuid4 : Guid.NewGuid());
                saveUserCmd.Parameters.AddWithValue(userMessage);
                
                await saveUserCmd.ExecuteNonQueryAsync();

                // Save AI response
                var saveAiCmd = new NpgsqlCommand(@"
                    INSERT INTO conversation_messages (id, conversation_id, user_id, sender, message_text, content, created_at)
                    VALUES (gen_random_uuid(), $1::uuid, $2::uuid, 'genie', $3, $3, CURRENT_TIMESTAMP)", conn);
                
                saveAiCmd.Parameters.AddWithValue(Guid.Parse(conversationId));
                saveAiCmd.Parameters.AddWithValue(Guid.TryParse(userId, out var userGuid6) ? userGuid6 : Guid.NewGuid());
                saveAiCmd.Parameters.AddWithValue(aiResponse);
                
                await saveAiCmd.ExecuteNonQueryAsync();

                // Update conversation stats
                var updateConvCmd = new NpgsqlCommand(@"
                    UPDATE genie_conversations 
                    SET message_count = message_count + 2, last_message_at = CURRENT_TIMESTAMP
                    WHERE id = $1::uuid", conn);
                
                updateConvCmd.Parameters.AddWithValue(Guid.Parse(conversationId));
                await updateConvCmd.ExecuteNonQueryAsync();

                return conversationId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving conversation");
                return Guid.NewGuid().ToString();
            }
        }

        private async Task TrackUserActivity(string userId, int currentDay)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                
                var cmd = new NpgsqlCommand(@"
                    INSERT INTO user_activity_log (id, user_id, activity_type, activity_data, created_at)
                    VALUES (gen_random_uuid(), $1::uuid, 'chat_interaction', 
                            $2::jsonb, CURRENT_TIMESTAMP)
                    ON CONFLICT DO NOTHING", conn);
                    
                cmd.Parameters.AddWithValue(Guid.TryParse(userId, out var userGuid4) ? userGuid4 : Guid.NewGuid());
                cmd.Parameters.AddWithValue(JsonSerializer.Serialize(new { 
                    day = currentDay, 
                    timestamp = DateTime.UtcNow,
                    type = "leadership_chat"
                }));
                
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking user activity");
            }
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetMetrics()
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                
                var cmd = new NpgsqlCommand(@"
                    SELECT 
                        COUNT(*) as total_conversations,
                        COUNT(DISTINCT DATE(started_at)) as active_days,
                        MAX(last_message_at) as last_activity,
                        AVG(message_count) as avg_messages_per_conversation
                    FROM genie_conversations 
                    WHERE user_id = $1::uuid", conn);
                    
                cmd.Parameters.AddWithValue(Guid.TryParse(userId, out var userGuid4) ? userGuid4 : Guid.NewGuid());
                
                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return Ok(new
                    {
                        totalConversations = reader.GetInt64(0),
                        activeDays = reader.GetInt64(1),
                        lastActivity = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                        avgMessagesPerConversation = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metrics");
            }

            return Ok(new { totalConversations = 0, activeDays = 0 });
        }
    }

    // Request/Response Models
    public class ChatRequest
    {
        public string Message { get; set; } = "";
        public string? ConversationId { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? BusinessName { get; set; }
        public int? CurrentDay { get; set; }
    }

    public class ChatResponse
    {
        public string Response { get; set; } = "";
        public string ConversationId { get; set; } = "";
        public int CurrentDay { get; set; }
        public DailyPrompt? DailyPrompt { get; set; }
        public string? UserProgress { get; set; }
    }

    public class UserContext
    {
        public string FirstName { get; set; } = "";
        public string BusinessName { get; set; } = "";
        public int CurrentDay { get; set; } = 1;
        public string CommunicationStyle { get; set; } = "balanced";
        public string PreferredResponseLength { get; set; } = "medium";
        public bool JourneyPaused { get; set; } = false;
        public string Timezone { get; set; } = "America/New_York";
    }

    public class ConversationMessage
    {
        public string Sender { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class DailyPrompt
    {
        public string PromptTitle { get; set; } = "";
        public string PromptText { get; set; } = "";
        public List<string> FillInBlanks { get; set; } = new();
        public List<string> FollowUpQuestions { get; set; } = new();
    }
}
