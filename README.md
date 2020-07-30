# S3 Drip Feed Proxy

## Objective

To proxy Amazon S3 buckets so that the response can be chunked; this is useful for clients that have low memory/CPU.

## Pre-Requisites

- [Docker](https://www.docker.com/)

- [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core)

- An IDE - VS Code will do fine

That's pretty much it.

## Configuration

### To run locally

- Create a .env file to pass environment variables to the Docker container.

AWS_ACCESS_KEY_ID=XYZ
AWS_SECRET_ACCESS_KEY=XYZ
AWS_REGION=XYZ
AWS_DEFAULT_REGION=XYZ
S3_BUCKET=XYZ

- The S3 bucket that you want to proxy should be defined in S3_BUCKET.

- run.sh builds the Docker image and runs it. You might need to chmod +x it.

### To run this on AWS using Fargate

- Create your Fargate cluster

- Create your ECR repository; follow the push commands to tag and push the image 

- Create a task definition pointing to your ECR repository

- A comprehensive set of instructions is available [here](https://github.com/aws-samples/amazon-ecs-fargate-aspnetcore)

- **Make sure that the IAM role that you are running the task with has at least read permissions on the S3 bucket you specified.**