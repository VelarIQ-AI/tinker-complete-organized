# 🔌 API Documentation - Tinker Genie

## 📋 **API Overview**

The Tinker Genie API is a RESTful web service built with .NET Core 8.0, providing authentication, chat functionality, user management, and coaching services.

**Base URL:** `https://tinker.twobrain.ai/api`  
**Authentication:** JWT Bearer Token  
**Content-Type:** `application/json`

---

## 🔐 **Authentication Endpoints**

### **POST /auth/login**
Authenticate user with username and password.

**Request:**
```json
{
  "username": "string",
  "password": "string"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "12345",
  "userName": "Leighton",
  "message": "Login successful"
}
```

**Response (401 Unauthorized):**
```json
{
  "success": false,
  "message": "Invalid credentials"
}
```

**Example:**
```bash
curl -X POST https://tinker.twobrain.ai/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"Leighton","password":"TinkerGenie2025!"}'
```

---

### **POST /auth/google**
Authenticate user with Google OAuth token.

**Request:**
```json
{
  "token": "google_oauth_token_here"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "12345",
  "userName": "John Doe",
  "message": "Google login successful"
}
```

---

## 💬 **Chat Endpoints**

### **POST /chat**
Send message to AI and get response.

**Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request:**
```json
{
  "message": "I need help with my gym's marketing strategy",
  "conversationId": "optional-conversation-id"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "response": "Let's dive into your marketing strategy. First, what's your current member acquisition cost?",
  "conversationId": "conv-12345",
  "messageId": "msg-67890",
  "timestamp": "2025-01-20T20:05:00Z"
}
```

**Example:**
```bash
curl -X POST https://tinker.twobrain.ai/api/chat \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"message":"How do I increase member retention?"}'
```

---

### **GET /chat/conversations**
Get user's conversation history.

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "conversations": [
    {
      "id": "conv-12345",
      "title": "Marketing Strategy Discussion",
      "lastMessage": "Let's dive into your marketing strategy...",
      "timestamp": "2025-01-20T20:05:00Z",
      "messageCount": 15
    }
  ]
}
```

---

### **GET /chat/conversations/{conversationId}/messages**
Get messages from specific conversation.

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "messages": [
    {
      "id": "msg-67890",
      "content": "How do I increase member retention?",
      "type": "user",
      "timestamp": "2025-01-20T20:05:00Z"
    },
    {
      "id": "msg-67891",
      "content": "Great question! Member retention starts with...",
      "type": "assistant",
      "timestamp": "2025-01-20T20:05:15Z"
    }
  ]
}
```

---

## 👤 **User Management Endpoints**

### **GET /user/profile**
Get current user's profile information.

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "user": {
    "id": "12345",
    "username": "Leighton",
    "email": "leighton@twobrain.ai",
    "firstName": "Leighton",
    "lastName": "Bingham",
    "role": "Admin",
    "createdAt": "2025-01-01T00:00:00Z",
    "lastLoginAt": "2025-01-20T20:00:00Z"
  }
}
```

---

### **PUT /user/profile**
Update user profile information.

**Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request:**
```json
{
  "firstName": "Leighton",
  "lastName": "Bingham",
  "email": "leighton@twobrain.ai"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Profile updated successfully"
}
```

---

## ⚙️ **Preferences Endpoints**

### **GET /preferences**
Get user's preferences and settings.

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "preferences": {
    "theme": "dark",
    "notifications": true,
    "voiceEnabled": true,
    "promptTime": "09:00",
    "timezone": "America/New_York",
    "language": "en-US"
  }
}
```

---

### **POST /preferences**
Update user preferences.

**Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request:**
```json
{
  "theme": "dark",
  "notifications": true,
  "voiceEnabled": true,
  "promptTime": "09:00",
  "timezone": "America/New_York"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Preferences updated successfully"
}
```

---

## 📚 **Curriculum Endpoints**

### **GET /curriculum/today**
Get today's coaching prompt and curriculum.

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "prompt": {
    "id": "prompt-12345",
    "title": "Building Your Leadership Foundation",
    "content": "Today we're focusing on the core principles that make great gym owners...",
    "category": "Leadership",
    "difficulty": "Intermediate",
    "estimatedTime": "15 minutes",
    "date": "2025-01-20"
  }
}
```

---

### **GET /curriculum/history**
Get user's curriculum completion history.

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "history": [
    {
      "promptId": "prompt-12345",
      "title": "Building Your Leadership Foundation",
      "completedAt": "2025-01-20T20:05:00Z",
      "score": 85,
      "feedback": "Great insights on leadership principles!"
    }
  ]
}
```

---

## 🏋️ **WOD (Workout of the Day) Endpoints**

### **GET /wod/today**
Get today's workout recommendation.

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "wod": {
    "id": "wod-12345",
    "title": "Strength & Conditioning",
    "description": "Focus on compound movements and metabolic conditioning",
    "exercises": [
      {
        "name": "Deadlift",
        "sets": 5,
        "reps": 5,
        "weight": "bodyweight + 50%"
      }
    ],
    "duration": "45 minutes",
    "difficulty": "Intermediate"
  }
}
```

---

## 🧪 **Test Endpoints**

### **GET /test/health**
Health check endpoint for monitoring.

**Response (200 OK):**
```json
{
  "status": "healthy",
  "timestamp": "2025-01-20T20:05:00Z",
  "version": "1.0.0",
  "services": {
    "database": "connected",
    "redis": "connected",
    "weaviate": "connected",
    "openai": "connected"
  }
}
```

---

### **GET /test/auth**
Test authentication (requires valid JWT).

**Headers:**
```
Authorization: Bearer {jwt_token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Authentication successful",
  "userId": "12345",
  "username": "Leighton"
}
```

---

## 🔒 **Authentication & Security**

### **JWT Token Structure**
```json
{
  "sub": "12345",
  "username": "Leighton",
  "role": "Admin",
  "iat": 1642694400,
  "exp": 1642780800
}
```

### **Token Usage**
Include JWT token in Authorization header:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### **Token Expiration**
- **Access Token:** 24 hours
- **Refresh Token:** 30 days (if implemented)

---

## 📊 **Response Formats**

### **Success Response**
```json
{
  "success": true,
  "data": { ... },
  "message": "Operation completed successfully"
}
```

### **Error Response**
```json
{
  "success": false,
  "error": "Error description",
  "code": "ERROR_CODE",
  "details": { ... }
}
```

---

## 🚨 **Error Codes**

| Code | Status | Description |
|------|--------|-------------|
| `AUTH_REQUIRED` | 401 | Authentication required |
| `INVALID_CREDENTIALS` | 401 | Invalid username/password |
| `TOKEN_EXPIRED` | 401 | JWT token expired |
| `ACCESS_DENIED` | 403 | Insufficient permissions |
| `NOT_FOUND` | 404 | Resource not found |
| `VALIDATION_ERROR` | 400 | Request validation failed |
| `RATE_LIMITED` | 429 | Too many requests |
| `SERVER_ERROR` | 500 | Internal server error |

---

## 🔄 **Rate Limiting**

### **Limits**
- **Authentication:** 10 requests/minute
- **Chat:** 60 requests/minute
- **General API:** 100 requests/minute

### **Headers**
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1642694400
```

---

## 📝 **Request/Response Examples**

### **Complete Chat Flow**
```bash
# 1. Login
TOKEN=$(curl -s -X POST https://tinker.twobrain.ai/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"Leighton","password":"TinkerGenie2025!"}' | \
  jq -r '.token')

# 2. Send chat message
curl -X POST https://tinker.twobrain.ai/api/chat \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"message":"How do I improve my gym retention rate?"}'

# 3. Get conversation history
curl -X GET https://tinker.twobrain.ai/api/chat/conversations \
  -H "Authorization: Bearer $TOKEN"
```

---

## 🛠️ **Development Tools**

### **Postman Collection**
Import the API collection for easy testing:
```json
{
  "info": {
    "name": "Tinker Genie API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "auth": {
    "type": "bearer",
    "bearer": [
      {
        "key": "token",
        "value": "{{jwt_token}}",
        "type": "string"
      }
    ]
  }
}
```

### **OpenAPI/Swagger**
Access interactive API documentation:
```
https://tinker.twobrain.ai/api/swagger
```

---

## 🔍 **Monitoring & Logging**

### **Request Logging**
All API requests are logged with:
- Timestamp
- User ID
- Endpoint
- Response time
- Status code

### **Error Tracking**
Errors are tracked and include:
- Stack trace
- Request context
- User information
- Environment details

---

**🎯 API Documentation Complete!** Use these endpoints to integrate with the Tinker Genie platform.
