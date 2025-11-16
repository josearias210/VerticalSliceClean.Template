# Deployment Guide - VPS Production

## 📋 Prerequisites

- Ubuntu 22.04+ VPS (DigitalOcean, Hetzner, etc.) with 2 vCPU / 4 GB RAM minimum
- Domain name pointing to your VPS IP
- SSH access with key-based authentication
- Docker and Docker Compose installed on VPS

## 🚀 Initial VPS Setup

### 1. Connect to VPS

```bash
ssh root@your-vps-ip
```

### 2. Install Docker and Docker Compose

```bash
# Update system
apt update && apt upgrade -y

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# Install Docker Compose
apt install docker-compose-plugin -y

# Verify installation
docker --version
docker compose version
```

### 3. Create deployment user (optional, recommended)

```bash
# Create user
adduser deployer
usermod -aG docker deployer
usermod -aG sudo deployer

# Switch to deployer
su - deployer
```

### 4. Create application directory

```bash
sudo mkdir -p /opt/acme
sudo chown -R $USER:$USER /opt/acme
cd /opt/acme
```

### 5. Configure firewall

```bash
# Allow SSH
sudo ufw allow 22/tcp

# Allow HTTP/HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 443/udp  # HTTP/3

# Enable firewall
sudo ufw enable
sudo ufw status
```

## 🔐 GitHub Secrets Configuration

Go to your GitHub repository: **Settings > Secrets and variables > Actions > New repository secret**

Add the following secrets:

### Required Secrets

| Secret Name | Description | Example |
|------------|-------------|---------|
| `VPS_HOST` | VPS IP address or hostname | `123.45.67.89` |
| `VPS_USERNAME` | SSH username | `deployer` or `root` |
| `VPS_SSH_KEY` | Private SSH key | Full content of `~/.ssh/id_rsa` |
| `VPS_PORT` | SSH port (optional) | `22` |
| `VPS_DOMAIN` | Your domain | `api.yourdomain.com` |
| `DB_NAME` | Database name | `MyCompanyDbDb` |
| `SQL_SA_PASSWORD` | SQL Server SA password | Strong password (32+ chars) |
| `JWT_SECRET_KEY` | JWT signing key | Generate with `openssl rand -base64 32` |
| `ADMIN_EMAIL` | Admin user email | `admin@yourdomain.com` |
| `ADMIN_PASSWORD` | Admin user password | Strong password (16+ chars) |
| `CORS_ORIGIN_1` | Primary frontend URL | `https://yourdomain.com` |
| `CORS_ORIGIN_2` | Secondary frontend URL | `https://www.yourdomain.com` |

### Optional Secrets (with defaults)

| Secret Name | Default | Description |
|------------|---------|-------------|
| `DB_APPLY_MIGRATIONS` | `false` | Apply migrations on startup |
| `DB_SEED_ROLES` | `true` | Seed roles on startup |
| `DB_SEED_ADMIN` | `false` | Seed admin user on startup |
| `JWT_ISSUER` | `Acme` | JWT issuer claim |
| `JWT_AUDIENCE` | `Acme.Api` | JWT audience claim |
| `JWT_ACCESS_TOKEN_MINUTES` | `15` | Access token lifetime |
| `JWT_REFRESH_TOKEN_DAYS` | `7` | Refresh token lifetime |
| `LOG_LEVEL` | `Information` | Logging level |

### Generate SSH Key for GitHub Actions

If you don't have an SSH key on your VPS:

```bash
# On VPS
ssh-keygen -t ed25519 -C "github-actions" -f ~/.ssh/github_actions
cat ~/.ssh/github_actions.pub >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys

# Copy private key content (paste this in VPS_SSH_KEY secret)
cat ~/.ssh/github_actions
```

### Generate JWT Secret Key

```bash
# Generate secure random key
openssl rand -base64 32
```

Copy the output and paste it in `JWT_SECRET_KEY` secret.

## 🌐 Domain Configuration

### DNS Records

Point your domain to your VPS:

```
Type: A
Name: api (or @)
Value: YOUR_VPS_IP
TTL: 3600
```

Wait for DNS propagation (5-30 minutes). Verify with:

```bash
dig api.yourdomain.com +short
```

### Caddy Auto-HTTPS

Caddy will automatically obtain and renew SSL certificates from Let's Encrypt. No manual configuration needed!

## 📦 Manual Deployment (First Time)

Before pushing to GitHub, test manual deployment:

### 1. Copy files to VPS

```bash
# From local machine
scp docker-compose.production.yml deployer@your-vps-ip:/opt/acme/
scp Caddyfile deployer@your-vps-ip:/opt/acme/
scp .env.production.example deployer@your-vps-ip:/opt/acme/.env
```

### 2. Configure .env on VPS

```bash
# On VPS
cd /opt/acme
nano .env

# Fill in all values (see .env.production.example)
# IMPORTANT: Change all passwords and secrets!
```

### 3. Log in to GitHub Container Registry

```bash
# On VPS
echo "YOUR_GITHUB_TOKEN" | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```

**Note:** Generate a GitHub Personal Access Token (classic) with `read:packages` scope at:
https://github.com/settings/tokens

### 4. Start services

```bash
# Pull images
docker compose -f docker-compose.production.yml pull

# Start services
docker compose -f docker-compose.production.yml up -d

# Check logs
docker compose -f docker-compose.production.yml logs -f
```

### 5. Verify deployment

```bash
# Check running containers
docker compose -f docker-compose.production.yml ps

# Check API health
curl http://localhost:8080/health

# Check via domain
curl https://api.yourdomain.com/health
```

## 🔄 Automated Deployment via GitHub Actions

Once manual deployment works, GitHub Actions will handle future deployments:

### Trigger Deployment

**Option 1: Push to main/master branch**
```bash
git add .
git commit -m "Deploy to production"
git push origin main
```

**Option 2: Create a release tag**
```bash
git tag -a v0.1.0 -m "Release version 0.1.0"
git push origin v0.1.0
```

### Monitor Deployment

1. Go to GitHub: **Actions** tab
2. Watch **Build and Deploy** workflow
3. Check logs for each step

### Deployment Flow

1. **Build Job**:
   - Checkout code
   - Restore & build .NET solution
   - Run tests
   - Build Docker image
   - Push to ghcr.io

2. **Deploy Job**:
   - Create .env with secrets
   - Copy files to VPS via SCP
   - SSH to VPS
   - Backup database
   - Pull new image
   - Restart services
   - Verify health

## 📊 Monitoring and Maintenance

### View logs

```bash
# All services
docker compose -f docker-compose.production.yml logs -f

# Specific service
docker logs -f acme-api-prod
docker logs -f acme-sqlserver-prod
docker logs -f acme-caddy-prod

# Serilog file logs
tail -f /opt/acme/api_logs/app-*.log
tail -f /opt/acme/api_logs/errors-*.log
```

### Check service status

```bash
docker compose -f docker-compose.production.yml ps

# Check health
docker inspect acme-api-prod --format='{{.State.Health.Status}}'
```

### Restart services

```bash
# Restart API only
docker compose -f docker-compose.production.yml restart api

# Restart all
docker compose -f docker-compose.production.yml restart
```

### Database backup

```bash
# Manual backup
docker exec acme-sqlserver-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YOUR_SQL_PASSWORD" -C \
  -Q "BACKUP DATABASE [MyCompanyDbDb] TO DISK='/var/opt/mssql/backups/manual_backup_$(date +%Y%m%d_%H%M%S).bak'"

# Download backup
docker cp acme-sqlserver-prod:/var/opt/mssql/backups/manual_backup_*.bak ./
```

### Automated daily backup (cron)

```bash
# Add to crontab
crontab -e

# Backup at 2 AM daily
0 2 * * * cd /opt/acme && docker exec acme-sqlserver-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YOUR_SQL_PASSWORD" -C -Q "BACKUP DATABASE [MyCompanyDbDb] TO DISK='/var/opt/mssql/backups/daily_backup_\$(date +\%Y\%m\%d).bak'" > /var/log/db_backup.log 2>&1

# Clean old backups (keep 30 days)
0 3 * * * find /var/lib/docker/volumes/acme_sqlserver_backups/_data -name "*.bak" -mtime +30 -delete
```

### Update configuration

```bash
# Edit .env
nano /opt/acme/.env

# Apply changes
docker compose -f docker-compose.production.yml up -d
```

### Clean up old images

```bash
# Remove unused images (frees disk space)
docker image prune -af --filter "until=168h"

# Remove unused volumes (CAREFUL!)
docker volume prune -f
```

## 🐛 Troubleshooting

### API not starting

```bash
# Check logs
docker logs acme-api-prod

# Common issues:
# - Database not ready: wait 30s and check again
# - Connection string wrong: check .env SQL_SA_PASSWORD
# - Migrations failing: set DB_APPLY_MIGRATIONS=false temporarily
```

### Database connection failed

```bash
# Check SQL Server logs
docker logs acme-sqlserver-prod

# Test connection
docker exec -it acme-sqlserver-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YOUR_SQL_PASSWORD" -C -Q "SELECT @@VERSION"
```

### HTTPS not working

```bash
# Check Caddy logs
docker logs acme-caddy-prod

# Common issues:
# - DNS not propagated: wait and check with `dig yourdomain.com`
# - Port 80/443 blocked: check firewall `sudo ufw status`
# - Rate limit hit: wait 1 hour or use staging server
```

### Out of disk space

```bash
# Check disk usage
df -h

# Clean Docker
docker system prune -af --volumes

# Clean logs
sudo journalctl --vacuum-time=7d
```

## 🔒 Security Checklist

- [ ] SSH key-based authentication only (disable password login)
- [ ] Firewall enabled (only ports 22, 80, 443 open)
- [ ] Strong SQL Server password (32+ characters)
- [ ] Strong JWT secret key (32+ characters)
- [ ] Strong admin password (16+ characters)
- [ ] .env file has 600 permissions (`chmod 600 .env`)
- [ ] GitHub secrets properly configured
- [ ] Database backups automated
- [ ] SSL certificates auto-renewed (Caddy handles this)
- [ ] Non-root user for application (Dockerfile uses appuser)

## 📈 Scaling Considerations

### Upgrade VPS resources

If you need more capacity:

1. Upgrade VPS to 4 vCPU / 8 GB RAM
2. Update resource limits in `docker-compose.production.yml`:
   ```yaml
   api:
     deploy:
       resources:
         limits:
           cpus: '2.0'
           memory: 1G
   
   sqlserver:
     deploy:
       resources:
         limits:
           cpus: '3.0'
           memory: 4G
   ```
3. Restart services: `docker compose up -d`

### Add observability (optional)

If you need logging/tracing later:

```bash
# Uncomment Seq/Jaeger in docker-compose.production.yml
# Set environment variables:
SEQ_URL=http://seq:80
OTEL_ENDPOINT=http://jaeger:4317

# Restart
docker compose -f docker-compose.production.yml up -d seq jaeger
```

## 📞 Support

For issues or questions:
- GitHub Issues: https://github.com/josearias210/Acme/issues
- Email: admin@yourdomain.com

## 📝 Useful Commands Reference

```bash
# Start all services
docker compose -f docker-compose.production.yml up -d

# Stop all services
docker compose -f docker-compose.production.yml down

# Restart API only
docker compose -f docker-compose.production.yml restart api

# View logs
docker compose -f docker-compose.production.yml logs -f api

# Pull latest images
docker compose -f docker-compose.production.yml pull

# Check health
curl http://localhost:8080/health

# Database backup
docker exec acme-sqlserver-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "PASSWORD" -C \
  -Q "BACKUP DATABASE [DB] TO DISK='/var/opt/mssql/backups/backup.bak'"

# Clean Docker
docker system prune -af
```
