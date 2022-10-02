aws ecr create-repository --repository-name example-app-dev --profile account-dev
aws ecr get-login-password --region ca-central-1 --profile account-dev | docker login --username AWS --password-stdin <FILL IN>.dkr.ecr.ca-central-1.amazonaws.com

# Create image from Dockerfile location or desired base directory
Set-Location ../../src/backend/
docker build . -t example-app-dev -f ./example-app/Dockerfile
Set-Location ../../aws/example-app/
docker tag example-app-dev:latest <FILL IN>.dkr.ecr.ca-central-1.amazonaws.com/example-app-dev:latest
docker push <FILL IN>.dkr.ecr.ca-central-1.amazonaws.com/example-app-dev:latest