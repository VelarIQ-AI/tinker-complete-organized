using TinkerGenie.API.Models;
using Dapper;
using Npgsql;

namespace TinkerGenie.API.Services
{
    public class LeadershipService : ILeadershipService
    {
        private readonly string _connectionString;
        private readonly ILogger<LeadershipService> _logger;

        public LeadershipService(IConfiguration configuration, ILogger<LeadershipService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public async Task<DailyPrompt?> GetDailyPromptAsync(string userId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                
                // Get user current day
                var currentDay = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT current_day FROM user_data WHERE user_id = @userId", 
                    new { userId });

                if (currentDay == 0) currentDay = 1;

                // Get daily prompt for current day
                var prompt = await connection.QueryFirstOrDefaultAsync<DailyPrompt>(
                    @"SELECT day_number as DayNumber, title as Title, prompt_text as PromptText, 
                             fill_in_blanks_jsonb as FillInBlanks, task_instructions as TaskInstructions,
                             estimated_time_minutes as EstimatedTimeMinutes
                      FROM leadership_daily_prompts 
                      WHERE day_number = @dayNumber AND is_active = true",
                    new { dayNumber = currentDay });

                return prompt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily prompt for user {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> CompletePromptAsync(string userId, int dayNumber, Dictionary<string, string> responses, string? reflectionNotes = null)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                
                // Insert completion record
                await connection.ExecuteAsync(
                    @"INSERT INTO user_prompt_deliveries (user_id, day_number, completed_at, responses_jsonb, reflection_notes)
                      VALUES (@userId, @dayNumber, @completedAt, @responses, @reflectionNotes)
                      ON CONFLICT (user_id, day_number) DO UPDATE SET
                      completed_at = @completedAt, responses_jsonb = @responses, reflection_notes = @reflectionNotes",
                    new { 
                        userId, 
                        dayNumber, 
                        completedAt = DateTime.UtcNow,
                        responses = System.Text.Json.JsonSerializer.Serialize(responses),
                        reflectionNotes 
                    });

                // Update user current day
                await connection.ExecuteAsync(
                    "UPDATE user_data SET current_day = @nextDay WHERE user_id = @userId",
                    new { nextDay = dayNumber + 1, userId });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing prompt for user {UserId}, day {DayNumber}", userId, dayNumber);
                return false;
            }
        }

        public async Task<List<string>> GenerateFollowUpQuestionsAsync(int dayNumber, Dictionary<string, string> responses)
        {
            // This would integrate with OpenAI to generate personalized follow-up questions
            // For now, return generic questions
            return new List<string>
            {
                "How did this exercise make you feel?",
                "What was the most challenging part?",
                "How will you apply this in your business?"
            };
        }

        public async Task<UserProgress> GetUserProgressAsync(string userId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                
                var progress = await connection.QueryFirstOrDefaultAsync<UserProgress>(
                    @"SELECT current_day as CurrentDay, 
                             (SELECT COUNT(*) FROM user_prompt_deliveries WHERE user_id = @userId) as CompletedDays,
                             0 as CurrentStreak,
                             (SELECT MAX(completed_at) FROM user_prompt_deliveries WHERE user_id = @userId) as LastCompleted
                      FROM user_data WHERE user_id = @userId",
                    new { userId });

                return progress ?? new UserProgress { CurrentDay = 1 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user progress for {UserId}", userId);
                return new UserProgress { CurrentDay = 1 };
            }
        }

        public async Task<bool> UpdateUserPreferencesAsync(string userId, UserPreferences preferences)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                
                await connection.ExecuteAsync(
                    @"INSERT INTO user_preferences (user_id, communication_style, prompt_delivery_time, timezone, journey_paused)
                      VALUES (@userId, @communicationStyle, @deliveryTime, @timezone, @journeyPaused)
                      ON CONFLICT (user_id) DO UPDATE SET
                      communication_style = @communicationStyle, prompt_delivery_time = @deliveryTime, 
                      timezone = @timezone, journey_paused = @journeyPaused",
                    new { 
                        userId, 
                        preferences.CommunicationStyle,
                        deliveryTime = preferences.PromptDeliveryTime,
                        preferences.Timezone,
                        journeyPaused = preferences.JourneyPaused
                    });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating preferences for user {UserId}", userId);
                return false;
            }
        }

        public async Task<List<DailyPrompt>> GetMissedPromptsAsync(string userId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                
                var missedPrompts = await connection.QueryAsync<DailyPrompt>(
                    @"SELECT ldp.day_number as DayNumber, ldp.title as Title, ldp.prompt_text as PromptText,
                             ldp.fill_in_blanks_jsonb as FillInBlanks, ldp.task_instructions as TaskInstructions,
                             ldp.estimated_time_minutes as EstimatedTimeMinutes
                      FROM leadership_daily_prompts ldp
                      LEFT JOIN user_prompt_deliveries upd ON ldp.day_number = upd.day_number AND upd.user_id = @userId
                      WHERE upd.user_id IS NULL AND ldp.is_active = true
                      ORDER BY ldp.day_number",
                    new { userId });

                return missedPrompts.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting missed prompts for user {UserId}", userId);
                return new List<DailyPrompt>();
            }
        }

        public async Task<bool> SendNudgeAsync(string userId, string nudgeType)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                
                // Insert nudge record
                await connection.ExecuteAsync(
                    "INSERT INTO user_nudges (user_id, nudge_type, sent_at) VALUES (@userId, @nudgeType, @sentAt)",
                    new { userId, nudgeType, sentAt = DateTime.UtcNow });

                // This would integrate with notification service
                _logger.LogInformation("Nudge sent to user {UserId}: {NudgeType}", userId, nudgeType);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending nudge to user {UserId}", userId);
                return false;
            }
        }
    }
}
