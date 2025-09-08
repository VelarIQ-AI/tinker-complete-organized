using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TinkerGenie.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly string _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "TinkerGenieJWTSecretKey2025VeryLongAndSecure";
        private readonly string _googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? "your-google-client-id";
        private readonly string _googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? "your-google-client-secret";

        public AuthController(ILogger<AuthController> logger)
        {
            _logger = logger;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                // Chris Cooper - Owner access
                if ((request.Email == "chris@twobrainbusiness.com" && request.Password == "TwoBrainOwner2025!") ||
                    (request.Username == "chris" && request.Password == "TwoBrainOwner2025!"))
                {
                    var token = GenerateJwtToken("chris-cooper", "chris@twobrainbusiness.com", "owner");
                    
                    return Ok(new
                    {
                        success = true,
                        token = token,
                        userId = "chris-cooper",
                        userName = "Chris Cooper",
                        email = "chris@twobrainbusiness.com",
                        role = "owner"
                    });
                }

                // Demo admin access
                if ((request.Email == "admin@tinkergenie.com" && request.Password == "TinkerAdmin2025!") ||
                    (request.Username == "admin" && request.Password == "TinkerAdmin2025!"))
                {
                    var token = GenerateJwtToken("admin", "admin@tinkergenie.com", "admin");
                    
                    return Ok(new
                    {
                        success = true,
                        token = token,
                        userId = "admin-user",
                        userName = "Admin User",
                        email = "admin@tinkergenie.com",
                        role = "admin"
                    });
                }

        // Additional users
        if ((request.Email == "Chris@twobrainbusiness.com" && request.Password == "TinkerGenie2025!") ||
            (request.Username == "Chris" && request.Password == "TinkerGenie2025!"))
        {
            var token = GenerateJwtToken("Chris", "Chris@twobrainbusiness.com", "member");
            return Ok(new { success = true, token = token, userId = "Chris", userName = "Chris", email = "Chris@twobrainbusiness.com", role = "member" });
        }
        
        if ((request.Email == "Amber@twobrainbusiness.com" && request.Password == "TinkerGenie2025!") ||
            (request.Username == "Amber" && request.Password == "TinkerGenie2025!"))
        {
            var token = GenerateJwtToken("Amber", "Amber@twobrainbusiness.com", "member");
            return Ok(new { success = true, token = token, userId = "Amber", userName = "Amber", email = "Amber@twobrainbusiness.com", role = "member" });
        }
        
        if ((request.Email == "Mike@twobrainbusiness.com" && request.Password == "TinkerGenie2025!") ||
            (request.Username == "Mike" && request.Password == "TinkerGenie2025!"))
        {
            var token = GenerateJwtToken("Mike", "Mike@twobrainbusiness.com", "member");
            return Ok(new { success = true, token = token, userId = "Mike", userName = "Mike", email = "Mike@twobrainbusiness.com", role = "member" });
        }
        
        if ((request.Email == "John@twobrainbusiness.com" && request.Password == "TinkerGenie2025!") ||
            (request.Username == "John" && request.Password == "TinkerGenie2025!"))
        {
            var token = GenerateJwtToken("John", "John@twobrainbusiness.com", "member");
            return Ok(new { success = true, token = token, userId = "John", userName = "John", email = "John@twobrainbusiness.com", role = "member" });
        }
        
        if ((request.Email == "Joleen@twobrainbusiness.com" && request.Password == "TinkerGenie2025!") ||
            (request.Username == "Joleen" && request.Password == "TinkerGenie2025!"))
        {
            var token = GenerateJwtToken("Joleen", "Joleen@twobrainbusiness.com", "member");
            return Ok(new { success = true, token = token, userId = "Joleen", userName = "Joleen", email = "Joleen@twobrainbusiness.com", role = "member" });
        }
        
        if ((request.Email == "Leighton@twobrainbusiness.com" && request.Password == "TinkerGenie2025!") ||
            (request.Username == "Leighton" && request.Password == "TinkerGenie2025!"))
        {
            var token = GenerateJwtToken("Leighton", "Leighton@twobrainbusiness.com", "member");
            return Ok(new { success = true, token = token, userId = "Leighton", userName = "Leighton", email = "Leighton@twobrainbusiness.com", role = "member" });
        }
        
        if ((request.Email == "Matt@twobrainbusiness.com" && request.Password == "TinkerGenie2025!") ||
            (request.Username == "Matt" && request.Password == "TinkerGenie2025!"))
        {
            var token = GenerateJwtToken("Matt", "Matt@twobrainbusiness.com", "member");
            return Ok(new { success = true, token = token, userId = "Matt", userName = "Matt", email = "Matt@twobrainbusiness.com", role = "member" });
        }

                return BadRequest(new { success = false, message = "Invalid credentials" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { success = false, message = "Login failed" });
            }
        }

        [HttpGet("google")]
        public IActionResult GoogleLogin()
        {
            var googleAuthUrl = "https://accounts.google.com/o/oauth2/v2/auth?" +
                $"client_id={_googleClientId}&" +
                "redirect_uri=https://tinker.twobrain.ai/api/auth/google/callback&" +
                "response_type=code&" +
                "scope=openid email profile&" +
                "state=tinkergenie_security_token";
            
            return Redirect(googleAuthUrl);
        }

        [HttpGet("google/callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Redirect("https://tinker.twobrain.ai/login.html?error=no_code");
                }

                var tokenResponse = await ExchangeCodeForToken(code);
                
                if (tokenResponse?.AccessToken == null)
                {
                    return Redirect("https://tinker.twobrain.ai/login.html?error=token_exchange_failed");
                }
                
                var userInfo = await GetGoogleUserInfo(tokenResponse.AccessToken);
                
                if (userInfo?.Email == null)
                {
                    return Redirect("https://tinker.twobrain.ai/login.html?error=user_info_failed");
                }
                
                if (await IsValidTinkerMember(userInfo.Email))
                {
                    var role = userInfo.Email == "chris@twobrainbusiness.com" ? "owner" : "member";
                    var token = GenerateJwtToken(userInfo.Id, userInfo.Email, role);
                    
                    return Redirect($"https://tinker.twobrain.ai/?token={token}&userId={userInfo.Id}&userName={Uri.EscapeDataString(userInfo.Name)}");
                }
                
                return Redirect("https://tinker.twobrain.ai/login.html?error=unauthorized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google OAuth callback error");
                return Redirect("https://tinker.twobrain.ai/login.html?error=oauth_failed");
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { success = true, message = "Logged out successfully" });
        }

        private string GenerateJwtToken(string userId, string email, string role = "member")
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("userId", userId),
                    new Claim("email", email),
                    new Claim("role", role),
                    new Claim(ClaimTypes.Name, email),
                    new Claim(ClaimTypes.Role, role)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private async Task<GoogleTokenResponse?> ExchangeCodeForToken(string code)
        {
            using var httpClient = new HttpClient();
            
            var tokenRequest = new Dictionary<string, string>
            {
                {"client_id", _googleClientId},
                {"client_secret", _googleClientSecret},
                {"code", code},
                {"grant_type", "authorization_code"},
                {"redirect_uri", "https://tinker.twobrain.ai/api/auth/google/callback"}
            };

            var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(tokenRequest));
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Token exchange failed: {Error}", errorContent);
                return null;
            }
            
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
            return JsonSerializer.Deserialize<GoogleTokenResponse>(jsonResponse, options);
        }

        private async Task<GoogleUserInfo?> GetGoogleUserInfo(string accessToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User info request failed: {Error}", errorContent);
                return null;
            }
            
            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GoogleUserInfo>(jsonResponse);
        }

        private Task<bool> IsValidTinkerMember(string email)
        {
            if (email == "chris@twobrainbusiness.com") return Task.FromResult(true);
            if (email.EndsWith("@twobrainbusiness.com")) return Task.FromResult(true);
            if (email == "joleen@twobrainbusiness.com") return Task.FromResult(true);
            if (email == "lisa@twobrainbusiness.com") return Task.FromResult(true);
            if (email.EndsWith("@gmail.com")) return Task.FromResult(true);
            
            return Task.FromResult(false);
        }
    }

    public class LoginRequest
    {
        public string? Email { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = "";
        
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "";
        
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = "";
    }

    public class GoogleUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        
        [JsonPropertyName("email")]
        public string Email { get; set; } = "";
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("picture")]
        public string Picture { get; set; } = "";
    }
}
