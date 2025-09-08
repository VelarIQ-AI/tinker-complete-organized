# 🚀 Deployment Guide - Tinker Genie

## 📋 **Production Deployment Checklist**

### **Pre-Deployment Requirements**
- [ ] **Server Setup** - Ubuntu 20.04+ or CentOS 8+
- [ ] **Domain Configuration** - DNS pointing to server
- [ ] **SSL Certificate** - Let's Encrypt or commercial cert
- [ ] **Database Setup** - PostgreSQL 13+
- [ ] **Redis Setup** - Redis 6+
- [ ] **Weaviate Setup** - Vector database

---

## 🔧 **Backend Deployment**

### **1. Install .NET Runtime**
```bash
# Ubuntu/Debian
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-8.0
```

### **2. Build and Deploy API**
```bash
# Build the application
cd backend/TinkerGenie.API
dotnet publish -c Release -o /var/www/tinker-api

# Set permissions
sudo chown -R www-data:www-data /var/www/tinker-api
sudo chmod -R 755 /var/www/tinker-api
```

### **3. Create Systemd Service**
```bash
sudo nano /etc/systemd/system/tinker-api.service
```

```ini
[Unit]
Description=Tinker Genie API
After=network.target

[Service]
Type=notify
User=www-data
Group=www-data
WorkingDirectory=/var/www/tinker-api
ExecStart=/usr/bin/dotnet /var/www/tinker-api/TinkerGenie.API.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=tinker-api
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```

### **4. Start API Service**
```bash
sudo systemctl daemon-reload
sudo systemctl enable tinker-api
sudo systemctl start tinker-api
sudo systemctl status tinker-api
```

---

## 🌐 **Frontend Deployment**

### **1. Deploy PWA Files**
```bash
# Copy frontend files
sudo cp -r frontend/pwa/* /var/www/html/
sudo chown -R www-data:www-data /var/www/html/
sudo chmod -R 755 /var/www/html/
```

### **2. Configure Nginx**
```bash
sudo nano /etc/nginx/sites-available/tinker-genie
```

```nginx
server {
    listen 80;
    server_name tinker.twobrain.ai;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name tinker.twobrain.ai;

    ssl_certificate /etc/letsencrypt/live/tinker.twobrain.ai/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/tinker.twobrain.ai/privkey.pem;

    # Security headers
    add_header X-Frame-Options DENY;
    add_header X-Content-Type-Options nosniff;
    add_header X-XSS-Protection "1; mode=block";
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

    # Frontend
    location / {
        root /var/www/html;
        index index.html;
        try_files $uri $uri/ /index.html;
        
        # PWA caching
        location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg)$ {
            expires 1y;
            add_header Cache-Control "public, immutable";
        }
    }

    # API proxy
    location /api/ {
        proxy_pass http://localhost:5000/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

### **3. Enable Site**
```bash
sudo ln -s /etc/nginx/sites-available/tinker-genie /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

---

## 🗄️ **Database Setup**

### **1. Install PostgreSQL**
```bash
sudo apt-get install postgresql postgresql-contrib
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

### **2. Create Database**
```bash
sudo -u postgres psql
```

```sql
CREATE DATABASE TinkerGenie;
CREATE USER tinker_user WITH ENCRYPTED PASSWORD 'secure_password_here';
GRANT ALL PRIVILEGES ON DATABASE TinkerGenie TO tinker_user;
\q
```

### **3. Run Migrations**
```bash
cd backend/TinkerGenie.API
dotnet ef database update
```

---

## 🔴 **Redis Setup**

### **1. Install Redis**
```bash
sudo apt-get install redis-server
sudo systemctl start redis-server
sudo systemctl enable redis-server
```

### **2. Configure Redis**
```bash
sudo nano /etc/redis/redis.conf
```

```conf
# Security
requirepass your_redis_password_here
bind 127.0.0.1

# Memory
maxmemory 256mb
maxmemory-policy allkeys-lru
```

### **3. Restart Redis**
```bash
sudo systemctl restart redis-server
```

---

## 🔍 **Weaviate Setup**

### **1. Docker Installation**
```bash
# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/download/v2.20.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
```

### **2. Weaviate Docker Compose**
```yaml
# docker-compose.yml
version: '3.4'
services:
  weaviate:
    command:
    - --host
    - 0.0.0.0
    - --port
    - '8080'
    - --scheme
    - http
    image: semitechnologies/weaviate:1.21.2
    ports:
    - 8080:8080
    restart: on-failure:0
    environment:
      QUERY_DEFAULTS_LIMIT: 25
      AUTHENTICATION_ANONYMOUS_ACCESS_ENABLED: 'true'
      PERSISTENCE_DATA_PATH: '/var/lib/weaviate'
      DEFAULT_VECTORIZER_MODULE: 'none'
      ENABLE_MODULES: 'text2vec-openai,generative-openai'
      CLUSTER_HOSTNAME: 'node1'
```

### **3. Start Weaviate**
```bash
docker-compose up -d
```

---

## 🔐 **SSL Certificate**

### **1. Install Certbot**
```bash
sudo apt-get install certbot python3-certbot-nginx
```

### **2. Obtain Certificate**
```bash
sudo certbot --nginx -d tinker.twobrain.ai
```

### **3. Auto-Renewal**
```bash
sudo crontab -e
# Add this line:
0 12 * * * /usr/bin/certbot renew --quiet
```

---

## 📊 **Monitoring & Logging**

### **1. Application Logs**
```bash
# API logs
sudo journalctl -u tinker-api -f

# Nginx logs
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log
```

### **2. Health Checks**
```bash
# API health
curl http://localhost:5000/health

# Frontend availability
curl -I https://tinker.twobrain.ai
```

### **3. System Monitoring**
```bash
# Install monitoring tools
sudo apt-get install htop iotop nethogs

# Check system resources
htop
df -h
free -h
```

---

## 🔄 **Backup Strategy**

### **1. Database Backup**
```bash
#!/bin/bash
# backup-db.sh
DATE=$(date +%Y%m%d_%H%M%S)
pg_dump -h localhost -U tinker_user TinkerGenie > /backups/db_backup_$DATE.sql
```

### **2. File Backup**
```bash
#!/bin/bash
# backup-files.sh
DATE=$(date +%Y%m%d_%H%M%S)
tar -czf /backups/files_backup_$DATE.tar.gz /var/www/html /var/www/tinker-api
```

### **3. Automated Backups**
```bash
sudo crontab -e
# Add these lines:
0 2 * * * /scripts/backup-db.sh
0 3 * * * /scripts/backup-files.sh
```

---

## 🚨 **Troubleshooting**

### **Common Issues**

**1. API Not Starting**
```bash
# Check logs
sudo journalctl -u tinker-api -n 50

# Check port
sudo netstat -tlnp | grep :5000

# Check permissions
ls -la /var/www/tinker-api
```

**2. Database Connection Failed**
```bash
# Test connection
psql -h localhost -U tinker_user -d TinkerGenie

# Check PostgreSQL status
sudo systemctl status postgresql
```

**3. Nginx Configuration Error**
```bash
# Test configuration
sudo nginx -t

# Check error logs
sudo tail -f /var/log/nginx/error.log
```

**4. SSL Certificate Issues**
```bash
# Check certificate
sudo certbot certificates

# Renew certificate
sudo certbot renew --dry-run
```

---

## 📈 **Performance Optimization**

### **1. Database Optimization**
```sql
-- Add indexes for better performance
CREATE INDEX idx_conversations_user_id ON GenieConversations(UserId);
CREATE INDEX idx_messages_conversation_id ON ConversationMessages(ConversationId);
CREATE INDEX idx_users_username ON Users(Username);
```

### **2. Redis Configuration**
```conf
# Optimize for performance
tcp-keepalive 300
timeout 0
tcp-backlog 511
```

### **3. Nginx Optimization**
```nginx
# Add to nginx.conf
gzip on;
gzip_vary on;
gzip_min_length 1024;
gzip_types text/plain text/css application/json application/javascript text/xml application/xml application/xml+rss text/javascript;

# Enable caching
proxy_cache_path /var/cache/nginx levels=1:2 keys_zone=api_cache:10m max_size=10g inactive=60m use_temp_path=off;
```

---

## ✅ **Post-Deployment Verification**

### **Checklist**
- [ ] **Frontend loads** - https://tinker.twobrain.ai
- [ ] **API responds** - https://tinker.twobrain.ai/api/health
- [ ] **Login works** - Test with valid credentials
- [ ] **Chat functions** - Send test message
- [ ] **Database connected** - Check user data
- [ ] **Redis working** - Check caching
- [ ] **SSL valid** - Check certificate
- [ ] **Monitoring active** - Logs flowing

### **Load Testing**
```bash
# Install Apache Bench
sudo apt-get install apache2-utils

# Test frontend
ab -n 1000 -c 10 https://tinker.twobrain.ai/

# Test API
ab -n 100 -c 5 -H "Authorization: Bearer YOUR_JWT_TOKEN" https://tinker.twobrain.ai/api/chat
```

---

**🎯 Deployment Complete!** Your Tinker Genie application is now live and production-ready.
