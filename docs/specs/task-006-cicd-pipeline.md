# TASK-006: CI/CD Pipeline with GitHub Enterprise

## Overview
**Priority**: Medium  
**Dependencies**: TASK-005  
**Estimated Effort**: 2-3 days  
**Phase**: Deployment & DevOps

## Description
Set up complete CI/CD pipeline using GitHub Actions for automated building, testing, and deployment to internal Docker infrastructure using GitHub Enterprise. After this task, the complete automated deployment workflow will be functional.

## Acceptance Criteria
- [ ] Create GitHub Actions workflow for automated builds
- [ ] Implement automated testing pipeline (unit and integration tests)
- [ ] Set up Docker image building and pushing to internal registry
- [ ] Configure automated deployment to internal Docker server
- [ ] Implement proper environment management (dev, staging, production)
- [ ] Set up automated database migration execution
- [ ] Configure security scanning and vulnerability checks
- [ ] Implement rollback procedures and deployment gates
- [ ] Deployment pipeline is fully automated and functional

## GitHub Actions Workflows

### 1. Main CI/CD Workflow
**File: `.github/workflows/ci-cd.yml`**
```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]
  workflow_dispatch:

env:
  REGISTRY: your-internal-registry.com
  IMAGE_NAME: visitortracking
  DOTNET_VERSION: '10.0.x'

jobs:
  # Job 1: Code Quality and Linting
  code-quality:
    name: Code Quality Check
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Format check
      run: dotnet format --verify-no-changes --verbosity diagnostic
      
    - name: Build
      run: dotnet build --no-restore --configuration Release
      
  # Job 2: Unit Tests
  unit-tests:
    name: Unit Tests
    runs-on: ubuntu-latest
    needs: code-quality
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Run unit tests
      run: |
        dotnet test --no-restore --configuration Release \
          --logger "trx;LogFileName=test-results.trx" \
          --collect:"XPlat Code Coverage" \
          --results-directory ./TestResults
      
    - name: Upload test results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: test-results
        path: ./TestResults
        
    - name: Generate test report
      uses: dorny/test-reporter@v1
      if: always()
      with:
        name: Unit Test Results
        path: "./TestResults/*.trx"
        reporter: dotnet-trx
        
  # Job 3: Security Scan
  security-scan:
    name: Security Scanning
    runs-on: ubuntu-latest
    needs: code-quality
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
    
    - name: Run Trivy vulnerability scanner
      uses: aquasecurity/trivy-action@master
      with:
        scan-type: 'fs'
        scan-ref: '.'
        format: 'sarif'
        output: 'trivy-results.sarif'
        
    - name: Upload Trivy results to GitHub Security
      uses: github/codeql-action/upload-sarif@v2
      if: always()
      with:
        sarif_file: 'trivy-results.sarif'
        
  # Job 4: Build Docker Image
  build-image:
    name: Build Docker Image
    runs-on: ubuntu-latest
    needs: [unit-tests, security-scan]
    if: github.ref == 'refs/heads/main' || github.ref == 'refs/heads/develop'
    
    outputs:
      image-tag: ${{ steps.meta.outputs.tags }}
      image-digest: ${{ steps.build.outputs.digest }}
      
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
    
    - name: Set up Docker Buildx
      uses: docker/setup-buildx-action@v3
      
    - name: Login to Container Registry
      uses: docker/login-action@v3
      with:
        registry: ${{ env.REGISTRY }}
        username: ${{ secrets.REGISTRY_USERNAME }}
        password: ${{ secrets.REGISTRY_PASSWORD }}
        
    - name: Extract metadata
      id: meta
      uses: docker/metadata-action@v5
      with:
        images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
        tags: |
          type=ref,event=branch
          type=ref,event=pr
          type=sha,prefix={{branch}}-
          type=raw,value=latest,enable={{is_default_branch}}
          type=semver,pattern={{version}}
          type=semver,pattern={{major}}.{{minor}}
          
    - name: Build and push Docker image
      id: build
      uses: docker/build-push-action@v5
      with:
        context: .
        push: true
        tags: ${{ steps.meta.outputs.tags }}
        labels: ${{ steps.meta.outputs.labels }}
        cache-from: type=gha
        cache-to: type=gha,mode=max
        build-args: |
          BUILD_DATE=${{ github.event.repository.updated_at }}
          VCS_REF=${{ github.sha }}
          VERSION=${{ steps.meta.outputs.version }}
          
    - name: Scan Docker image
      uses: aquasecurity/trivy-action@master
      with:
        image-ref: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }}
        format: 'sarif'
        output: 'docker-trivy-results.sarif'
        
    - name: Upload Docker scan results
      uses: github/codeql-action/upload-sarif@v2
      if: always()
      with:
        sarif_file: 'docker-trivy-results.sarif'
        
  # Job 5: Deploy to Staging
  deploy-staging:
    name: Deploy to Staging
    runs-on: ubuntu-latest
    needs: build-image
    if: github.ref == 'refs/heads/develop'
    environment:
      name: staging
      url: https://staging.visitortracking.company.com
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
    
    - name: Deploy to staging server
      uses: appleboy/ssh-action@v1.0.0
      with:
        host: ${{ secrets.STAGING_HOST }}
        username: ${{ secrets.STAGING_USER }}
        key: ${{ secrets.STAGING_SSH_KEY }}
        port: ${{ secrets.STAGING_PORT }}
        script: |
          cd /opt/visitortracking
          
          # Pull latest image
          docker login ${{ env.REGISTRY }} -u ${{ secrets.REGISTRY_USERNAME }} -p ${{ secrets.REGISTRY_PASSWORD }}
          export IMAGE_TAG=${{ needs.build-image.outputs.image-tag }}
          
          # Backup database
          timestamp=$(date +%Y%m%d_%H%M%S)
          cp data/visitors.db data/visitors.db.backup.$timestamp
          
          # Deploy with docker-compose
          docker-compose -f docker-compose.staging.yml pull
          docker-compose -f docker-compose.staging.yml up -d
          
          # Wait for health check
          timeout 300 bash -c 'until curl -f http://localhost:8080/health; do sleep 5; done'
          
    - name: Run smoke tests
      run: |
        sleep 10
        curl -f https://staging.visitortracking.company.com/health || exit 1
        
    - name: Notify deployment
      uses: 8398a7/action-slack@v3
      if: always()
      with:
        status: ${{ job.status }}
        text: "Staging deployment ${{ job.status }}"
        webhook_url: ${{ secrets.SLACK_WEBHOOK }}
        
  # Job 6: Deploy to Production
  deploy-production:
    name: Deploy to Production
    runs-on: ubuntu-latest
    needs: build-image
    if: github.ref == 'refs/heads/main'
    environment:
      name: production
      url: https://visitortracking.company.com
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
    
    - name: Deploy to production server
      uses: appleboy/ssh-action@v1.0.0
      with:
        host: ${{ secrets.PRODUCTION_HOST }}
        username: ${{ secrets.PRODUCTION_USER }}
        key: ${{ secrets.PRODUCTION_SSH_KEY }}
        port: ${{ secrets.PRODUCTION_PORT }}
        script: |
          cd /opt/visitortracking
          
          # Pull latest image
          docker login ${{ env.REGISTRY }} -u ${{ secrets.REGISTRY_USERNAME }} -p ${{ secrets.REGISTRY_PASSWORD }}
          export IMAGE_TAG=${{ needs.build-image.outputs.image-tag }}
          
          # Create backup
          timestamp=$(date +%Y%m%d_%H%M%S)
          cp data/visitors.db data/backups/visitors.db.backup.$timestamp
          
          # Deploy with zero-downtime
          docker-compose -f docker-compose.prod.yml pull
          docker-compose -f docker-compose.prod.yml up -d --no-deps --build
          
          # Wait for health check
          timeout 300 bash -c 'until curl -f http://localhost:8080/health; do sleep 5; done'
          
          # Remove old images
          docker image prune -f
          
    - name: Verify deployment
      run: |
        sleep 15
        curl -f https://visitortracking.company.com/health || exit 1
        
    - name: Notify deployment success
      uses: 8398a7/action-slack@v3
      if: success()
      with:
        status: success
        text: "✅ Production deployment successful - Version: ${{ github.sha }}"
        webhook_url: ${{ secrets.SLACK_WEBHOOK }}
        
    - name: Notify deployment failure
      uses: 8398a7/action-slack@v3
      if: failure()
      with:
        status: failure
        text: "❌ Production deployment failed - Manual intervention required"
        webhook_url: ${{ secrets.SLACK_WEBHOOK }}
```

### 2. Database Migration Workflow
**File: `.github/workflows/db-migration.yml`**
```yaml
name: Database Migration

on:
  workflow_dispatch:
    inputs:
      environment:
        description: 'Target environment'
        required: true
        default: 'staging'
        type: choice
        options:
          - staging
          - production
      action:
        description: 'Migration action'
        required: true
        default: 'update'
        type: choice
        options:
          - update
          - rollback

jobs:
  migrate:
    name: Run Database Migration
    runs-on: ubuntu-latest
    environment: ${{ github.event.inputs.environment }}
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '10.0.x'
        
    - name: Install EF Core tools
      run: dotnet tool install --global dotnet-ef
      
    - name: Run migrations
      if: github.event.inputs.action == 'update'
      run: |
        dotnet ef database update --connection "${{ secrets.DATABASE_CONNECTION_STRING }}"
        
    - name: Rollback migration
      if: github.event.inputs.action == 'rollback'
      run: |
        # Get previous migration name
        dotnet ef migrations list --connection "${{ secrets.DATABASE_CONNECTION_STRING }}"
        # Rollback to previous
        dotnet ef database update 0 --connection "${{ secrets.DATABASE_CONNECTION_STRING }}"
        
    - name: Notify result
      uses: 8398a7/action-slack@v3
      if: always()
      with:
        status: ${{ job.status }}
        text: "Database migration ${{ github.event.inputs.action }} on ${{ github.event.inputs.environment }}: ${{ job.status }}"
        webhook_url: ${{ secrets.SLACK_WEBHOOK }}
```

### 3. Rollback Workflow
**File: `.github/workflows/rollback.yml`**
```yaml
name: Rollback Deployment

on:
  workflow_dispatch:
    inputs:
      environment:
        description: 'Environment to rollback'
        required: true
        type: choice
        options:
          - staging
          - production
      version:
        description: 'Version/tag to rollback to'
        required: true
        type: string

jobs:
  rollback:
    name: Rollback to Previous Version
    runs-on: ubuntu-latest
    environment: ${{ github.event.inputs.environment }}
    
    steps:
    - name: Rollback deployment
      uses: appleboy/ssh-action@v1.0.0
      with:
        host: ${{ secrets[format('{0}_HOST', github.event.inputs.environment)] }}
        username: ${{ secrets[format('{0}_USER', github.event.inputs.environment)] }}
        key: ${{ secrets[format('{0}_SSH_KEY', github.event.inputs.environment)] }}
        script: |
          cd /opt/visitortracking
          
          # Pull specified version
          export IMAGE_TAG=${{ github.event.inputs.version }}
          docker-compose -f docker-compose.${{ github.event.inputs.environment }}.yml pull
          
          # Deploy previous version
          docker-compose -f docker-compose.${{ github.event.inputs.environment }}.yml up -d
          
          # Wait for health check
          timeout 300 bash -c 'until curl -f http://localhost:8080/health; do sleep 5; done'
          
    - name: Notify rollback
      uses: 8398a7/action-slack@v3
      with:
        status: ${{ job.status }}
        text: "🔄 Rollback to version ${{ github.event.inputs.version }} on ${{ github.event.inputs.environment }}: ${{ job.status }}"
        webhook_url: ${{ secrets.SLACK_WEBHOOK }}
```

## Repository Configuration

### Required Secrets
Configure these in GitHub Settings → Secrets and variables → Actions:

```bash
# Container Registry
REGISTRY_USERNAME=your-registry-username
REGISTRY_PASSWORD=your-registry-password

# Staging Environment
STAGING_HOST=staging-server.company.com
STAGING_USER=deploy
STAGING_SSH_KEY=-----BEGIN PRIVATE KEY-----...
STAGING_PORT=22

# Production Environment
PRODUCTION_HOST=prod-server.company.com
PRODUCTION_USER=deploy
PRODUCTION_SSH_KEY=-----BEGIN PRIVATE KEY-----...
PRODUCTION_PORT=22

# Database
DATABASE_CONNECTION_STRING=Data Source=...

# Notifications
SLACK_WEBHOOK=https://hooks.slack.com/services/...
```

### Environment Protection Rules
Configure in GitHub Settings → Environments:

**Staging Environment:**
- No required reviewers
- Allow administrators to bypass

**Production Environment:**
- Required reviewers: 1-2 people
- Deployment branches: main only
- Environment secrets configured

## Deployment Scripts

### Server Setup Script
**File: `scripts/setup-server.sh`**
```bash
#!/bin/bash
# Run this on the deployment server

set -e

echo "Setting up VisitorTracking deployment environment..."

# Create application directory
sudo mkdir -p /opt/visitortracking
sudo chown $USER:$USER /opt/visitortracking
cd /opt/visitortracking

# Create data directories
mkdir -p data logs backups

# Set permissions
chmod 755 data logs backups

# Install Docker if not present
if ! command -v docker &> /dev/null; then
    curl -fsSL https://get.docker.com -o get-docker.sh
    sudo sh get-docker.sh
    sudo usermod -aG docker $USER
fi

# Install Docker Compose
if ! command -v docker-compose &> /dev/null; then
    sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    sudo chmod +x /usr/local/bin/docker-compose
fi

echo "✓ Server setup complete"
echo "Please copy docker-compose files to /opt/visitortracking"
```

### Health Check Script
**File: `scripts/wait-for-health.sh`**
```bash
#!/bin/bash

HOST=${1:-localhost}
PORT=${2:-8080}
TIMEOUT=${3:-300}

echo "Waiting for $HOST:$PORT to become healthy..."

for i in $(seq 1 $TIMEOUT); do
    if curl -f "http://$HOST:$PORT/health" > /dev/null 2>&1; then
        echo "✓ Service is healthy!"
        exit 0
    fi
    
    if [ $((i % 10)) -eq 0 ]; then
        echo "Still waiting... (${i}s / ${TIMEOUT}s)"
    fi
    
    sleep 1
done

echo "✗ Service failed to become healthy within ${TIMEOUT} seconds"
exit 1
```

## Testing Requirements

### Workflow Testing Checklist
- [ ] Code quality checks pass on sample code
- [ ] Unit tests execute successfully
- [ ] Security scans complete without critical issues
- [ ] Docker image builds successfully
- [ ] Image is pushed to registry
- [ ] Staging deployment works
- [ ] Health checks pass after deployment
- [ ] Production deployment requires approval
- [ ] Rollback workflow functions correctly
- [ ] Notifications are sent properly

### Manual Testing
1. Create a test PR to verify CI pipeline
2. Merge to develop branch to test staging deployment
3. Merge to main branch to test production deployment
4. Test rollback procedure with a known-good version

## Monitoring and Alerting

### Health Check Monitoring
Set up external monitoring service to check:
- Application health endpoint
- Database connectivity
- Response time metrics

### Alert Configuration
Configure alerts for:
- Deployment failures
- Health check failures
- High error rates
- Performance degradation

## Documentation

### Deployment Runbook
**File: `docs/deployment-runbook.md`**
```markdown
# Deployment Runbook

## Standard Deployment
1. Create PR with changes
2. Wait for CI checks to pass
3. Merge to develop → auto-deploys to staging
4. Test on staging environment
5. Merge to main → requires approval → deploys to production

## Emergency Hotfix
1. Create hotfix branch from main
2. Make minimal required changes
3. Create PR directly to main
4. Get expedited approval
5. Deploy to production

## Rollback Procedure
1. Go to Actions → Rollback Deployment
2. Select environment
3. Enter previous working version tag
4. Run workflow
5. Verify health checks

## Troubleshooting
- Check GitHub Actions logs
- SSH to server: `ssh deploy@server.company.com`
- View logs: `docker-compose logs -f`
- Check health: `curl http://localhost:8080/health`
```

## Definition of Done
- [ ] Complete CI/CD pipeline is functional and tested
- [ ] Automated deployments work reliably to both staging and production
- [ ] Proper environment management with appropriate gates and approvals
- [ ] Security checks are integrated and failing builds on critical vulnerabilities
- [ ] Database migrations can be executed via workflow
- [ ] Rollback procedures are documented and tested
- [ ] Monitoring and alerting are configured
- [ ] All secrets and configurations are properly managed
- [ ] Documentation covers standard operations and troubleshooting
- [ ] Team members are trained on deployment procedures

## Security Considerations
- [ ] SSH keys are properly secured
- [ ] Registry credentials are stored as secrets
- [ ] Production deployments require approval
- [ ] Security scanning is mandatory
- [ ] Secrets are not exposed in logs
- [ ] Access to production is restricted

## Dependencies for Next Tasks
- All feature development tasks (TASK-007 through TASK-015) can now be deployed automatically
- Production releases are ready for user story implementation
- Continuous deployment enables rapid iteration
