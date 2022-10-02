# aws-cdk-csharp

Example infrastructure setup for a full stack app in Angular, C#/.NET with the AWS CDK in C#. Extendable for more app stacks with a common namespace that can be reused.

Creates these resources:

- RDS DB Cluster
- Secrets
- API Gateway
- S3
- CloudFront Origin and Distribution
- Lambda

Steps:

1. Run bootstrap-ecr-dev.ps1 to create the ECR repo.
2. Run `PrimerStackDev` to create the RDS cluster. This can be shared among multiple apps if they have common DB provider.
3. Run `ExampleAppPrimerStackDev` to create the S3 bucket.
4. Manually create a secret in the Secrets Manager that will be referenced by the API.
5. Run `ExampleAppStackDev` to create the everything else.
