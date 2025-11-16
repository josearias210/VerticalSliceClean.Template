# Production Deployment - Quick Start

## 🎯 Overview

Complete production setup for VPS deployment with:
- ✅ Multi-stage Dockerfile (optimized build)
- ✅ Docker Compose for production (API + SQL Server + Caddy HTTPS)
- ✅ GitHub Actions CI/CD (separate build/release jobs)
- ✅ Automated deployments via SSH
- ✅ Auto-SSL with Caddy
- ✅ Database backups
- ✅ Health checks & restart policies

## 📁 Files Created

### Docker & Deployment
- ✅ `src/Acme.AppHost/Dockerfile` - Multi-stage build (SDK → runtime)
- ✅ `.dockerignore` - Exclude unnecessary files from Docker context
- `docker-compose.production.yml` - Production stack (API + SQL + Caddy)
- `Caddyfile` - Reverse proxy with auto-HTTPS
- `.env.production.example` - Environment variables template

### CI/CD
- `.github/workflows/deploy.yml` - GitHub Actions pipeline:
  - **Build job**: Compile, test, build Docker image, push to GHCR
  - **Deploy job**: SSH to VPS, backup DB, deploy new version

### Documentation
- `docs/DEPLOYMENT.md` - Complete deployment guide

## 🚀 Quick Setup (5 minutes)

### 1. Configure GitHub Secrets

Go to **Settings > Secrets and variables > Actions**, add:

**Required:**
```
VPS_HOST=123.45.67.89
VPS_USERNAME=root
VPS_SSH_KEY=<paste private SSH key>
VPS_DOMAIN=api.yourdomain.com
SQL_SA_PASSWORD=<strong password 32+ chars>
JWT_SECRET_KEY=<generate with: openssl rand -base64 32>
ADMIN_EMAIL=admin@yourdomain.com
ADMIN_PASSWORD=<strong password>
CORS_ORIGIN_1=https://yourdomain.com
DB_NAME=MyCompanyDbDb
```

### 2. Setup VPS (one-time)

```bash
# SSH to VPS
ssh root@your-vps-ip

# Install Docker
curl -fsSL https://get.docker.com | sh
apt install docker-compose-plugin -y

# Create app directory
mkdir -p /opt/acme
cd /opt/acme

# Configure firewall
ufw allow 22/tcp && ufw allow 80/tcp && ufw allow 443/tcp
ufw enable
```

### 3. Point Domain to VPS

Add DNS A record:
```
Type: A
Name: api
Value: YOUR_VPS_IP
```

### 4. Deploy

```bash
# Push to main branch (triggers auto-deploy)
git add .
git commit -m "Setup production deployment"
git push origin main
```

GitHub Actions will:
1. ✅ Build & test code
2. ✅ Build Docker image
3. ✅ Push to GitHub Container Registry
4. ✅ Deploy to VPS via SSH
5. ✅ Start services with Caddy auto-HTTPS

## 📊 What Gets Deployed

### Services Running on VPS

| Service | Port | Resources | Purpose |
|---------|------|-----------|---------|
| **API** | 8080 | 512 MB / 1 CPU | .NET API |
| **SQL Server** | 1433 | 2 GB / 2 CPU | Database (Express) |
| **Caddy** | 80, 443 | 256 MB / 0.5 CPU | Reverse proxy + HTTPS |

**Total: ~2.7 GB RAM** (fits in 4 GB VPS with room for OS)

### Architecture

```
Internet → Caddy (HTTPS) → API → SQL Server
              ↓
          Auto-SSL
        (Let's Encrypt)
```

## 🔄 Deployment Flow

### Automated (GitHub Actions)

```mermaid
Push to main
    ↓
Build Job:
  - Compile .NET
  - Run tests
  - Build Docker image
  - Push to ghcr.io
    ↓
Deploy Job:
  - Create .env from secrets
  - SCP files to VPS
  - SSH to VPS:
    - Backup database
    - Pull new image
    - Restart services
    - Verify health
```

### Manual (if needed)

```bash
# On local machine
scp docker-compose.production.yml root@vps:/opt/acme/
scp Caddyfile root@vps:/opt/acme/

# On VPS
cd /opt/acme
cp .env.production.example .env
nano .env  # Fill values
docker compose -f docker-compose.production.yml up -d
```

## 🔐 Security Features

- ✅ Non-root container user
- ✅ HTTPS auto-configured (Caddy)
- ✅ Secrets via environment variables
- ✅ Firewall configured (only 22, 80, 443)
- ✅ Database backups before each deploy
- ✅ Health checks for all services
- ✅ Security headers (HSTS, CSP, X-Frame-Options)

## 📈 Resource Optimization

**Compared to Aspire + Seq + Jaeger:**
- ❌ Before: 4.5 GB RAM (Seq 512 MB + Jaeger 512 MB + Dashboard 300 MB)
- ✅ After: 2.7 GB RAM (only essentials)
- **Savings: ~1.8 GB RAM = fits in cheaper VPS**

**VPS Recommendations:**
- Minimum: 2 vCPU / 4 GB RAM → $24/month (DigitalOcean)
- Recommended: 4 vCPU / 8 GB RAM → $48/month (room to grow)

## 🧪 Testing Deployment

### Local test (before VPS)

```bash
# Build image locally
cd src
docker build -f Acme.AppHost/Dockerfile -t acme-api:local .

# Test with docker-compose
cd ..
cp .env.production.example .env
nano .env  # Fill test values
docker compose -f docker-compose.production.yml up
```

### Verify production

```bash
# Check health
curl https://api.yourdomain.com/health

# Check Swagger (if enabled)
open https://api.yourdomain.com/swagger

# Check logs on VPS
ssh root@vps
docker logs acme-api-prod
```

## 🐛 Common Issues

### 1. "Permission denied" on SSH

**Fix:** Ensure VPS_SSH_KEY secret contains full private key including headers:
```
-----BEGIN OPENSSH PRIVATE KEY-----
...key content...
-----END OPENSSH PRIVATE KEY-----
```

### 2. Database connection failed

**Fix:** Check SQL_SA_PASSWORD is correct:
```bash
docker logs acme-sqlserver-prod
```

### 3. HTTPS not working

**Fix:** Wait 2-5 minutes for Let's Encrypt. Check:
```bash
docker logs acme-caddy-prod
# Ensure DNS is propagated: dig api.yourdomain.com
```

### 4. API health check failing

**Fix:** Increase startup time in docker-compose.production.yml:
```yaml
healthcheck:
  start_period: 60s  # Increase if needed
```

## 📚 Next Steps

1. **Monitor first deployment:**
   ```bash
   # Watch GitHub Actions
   # Check VPS logs
   ssh root@vps "cd /opt/acme && docker compose logs -f"
   ```

2. **Setup automated backups:**
   ```bash
   # Add to crontab (see docs/DEPLOYMENT.md)
   ```

3. **Configure React frontend:**
   - Deploy to Cloudflare Pages / Netlify / Vercel (free)
   - Point to `https://api.yourdomain.com`

4. **Optional: Add observability:**
   - Uncomment Seq in docker-compose if needed
   - Set SEQ_URL environment variable

## 📞 Support

- Full guide: `docs/DEPLOYMENT.md`
- Troubleshooting: See "Troubleshooting" section in DEPLOYMENT.md
- Issues: https://github.com/josearias210/Acme/issues

## ✅ Deployment Checklist

- [ ] GitHub secrets configured
- [ ] VPS setup (Docker installed, ports open)
- [ ] DNS pointing to VPS
- [ ] First deployment completed
- [ ] Health check passing
- [ ] HTTPS working
- [ ] Database migrations applied
- [ ] Admin user created
- [ ] Automated backups configured
- [ ] Monitoring setup (optional)

**You're ready to deploy!** 🚀

Push to `main` branch and watch GitHub Actions deploy automatically.
