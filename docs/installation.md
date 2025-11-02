# Installation Guide: Running Visitor Tracking System with Docker

This guide explains how to run the Visitor Tracking System using Docker, leveraging the pre-built image published to GitHub Container Registry (ghcr) for this repository. It also covers how to update your container when a new version is released.

## Prerequisites

- Docker installed on your system ([Download Docker](https://docs.docker.com/get-docker/))
- Access to GitHub Container Registry (ghcr.io)
- (Optional) GitHub account with access to the repository if the image is private

## Pulling and Running the Docker Image

The Docker image is published to GitHub Container Registry (ghcr) under this repository. Replace `<OWNER>` and `<REPO>` with the actual owner and repository name (e.g., `mprattinger/visitor`).

### 1. Authenticate to ghcr (if image is private)

```pwsh
# Login to GitHub Container Registry
# Replace <USERNAME> with your GitHub username
# You will be prompted for a Personal Access Token (PAT) with 'read:packages' scope
$ ghcrUser = "<USERNAME>"
$ ghcrToken = "<YOUR_GITHUB_PAT>"
docker login ghcr.io -u $ghcrUser --password-stdin <<< $ghcrToken
```

### 2. Pull the Docker Image

```pwsh
# Replace <OWNER> and <REPO> with the correct values
# Replace <TAG> with the desired version (e.g., 'latest' or a release tag)
docker pull ghcr.io/<OWNER>/<REPO>:<TAG>
```

### 3. Run the Container

```pwsh
# Run the container, mapping port 8080 (or as configured in Dockerfile)
docker run -d -p 8080:8080 --name visitor-app ghcr.io/<OWNER>/<REPO>:<TAG>
```

- The application will be accessible at http://localhost:8080
- You can override configuration by mounting your own config files or using environment variables as needed

## Updating the Container to a New Version

When a new version of the Docker image is published:

1. Stop and remove the existing container:
   ```pwsh
   docker stop visitor-app
   docker rm visitor-app
   ```
2. Pull the new image:
   ```pwsh
   docker pull ghcr.io/<OWNER>/<REPO>:<NEW_TAG>
   ```
3. Start a new container with the updated image:
   ```pwsh
   docker run -d -p 8080:8080 --name visitor-app ghcr.io/<OWNER>/<REPO>:<NEW_TAG>
   ```

## Troubleshooting

- Ensure Docker is running and you have network access to ghcr.io
- If you encounter authentication errors, verify your GitHub PAT and login
- Check container logs for errors:
  ```pwsh
  docker logs visitor-app
  ```
- For database persistence, consider mounting a volume for the SQLite database:
  ```pwsh
  docker run -d -p 8080:8080 -v $(pwd)/data:/app/data --name visitor-app ghcr.io/<OWNER>/<REPO>:<TAG>
  ```

## Notes

- The default configuration uses SQLite; see `appsettings.json` for details
- For advanced configuration, refer to the main project README
- For Dockerfile details, see `src/Visitor.Web/Dockerfile`

---

For further help, open an issue in this repository or contact the maintainers.
