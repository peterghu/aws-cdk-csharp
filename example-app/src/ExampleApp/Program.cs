using Amazon.CDK;
using aws_cdk_csharp.Common.Constants;
using aws_cdk_csharp.Common.Props;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ExampleApp
{
    internal sealed class Program
    {
        private const string APPLICATION_NAME_TAG = "ExampleAppApi";
        private const string ANGULAR_BUILD_LOCATION = "../../src/example_app";
        private const string API_ENV_DB_PREFIX = "EXAMPLE_DB";

        // set a special prefix for human created resources like database name, ECR repo
        private const string STACK_NAME_KEBAB_CASE_PREFIX = "example";

        public static void Main(string[] args)
        {
            var app = new App();

            bool.TryParse(app.Node.TryGetContext("debug")?.ToString(), out var debug);

            if (debug)
            {
                Debugger.Launch();
            }

            // Primer stack sets up S3 bucket
            new ExampleAppPrimerStack(app, "ExampleAppPrimerStackDev", GetDevStackProps());
            new ExampleAppPrimerStack(app, "ExampleAppPrimerStackProd", GetProdStackProps());

            new ExampleAppStack(app, "ExampleAppStackDev", GetDevStackProps());
            //new ExampleAppStack(app, "ExampleAppStackProd", GetProdStackProps());

            app.Synth();
        }

        private static MicroServiceStackProps GetDevStackProps()
        {
            var environmentConstants = new EnvironmentConstantsManager().GetDev();
            var props = ApplyDefaultPropertiesForEnvironment(environmentConstants);

            // --- Override Defaults --

            return props;
        }

        private static MicroServiceStackProps GetProdStackProps()
        {
            var environmentConstants = new EnvironmentConstantsManager().GetProd();
            var props = ApplyDefaultPropertiesForEnvironment(environmentConstants);

            props.ApiProps.FargateDesiredServiceCount = 2;  // Prod should get more resources
            props.ApiProps.SwaggerEnabled = "false";

            return props;
        }

        private static MicroServiceStackProps ApplyDefaultPropertiesForEnvironment(EnvironmentConstants EnvironmentConstants)
        {
            string stackNameEnvPrefix = $"{STACK_NAME_KEBAB_CASE_PREFIX}-{EnvironmentConstants.EnvironmentShortName.ToLower()}";

            var props = new MicroServiceStackProps()
            {
                StackObjectPrefix = $"{EnvironmentConstants.EnvironmentShortName}",
                StackNameLowerCasePrefix = stackNameEnvPrefix,
                VpcId = EnvironmentConstants.VPCId,
                BucketArn = $"arn:aws:s3:::{stackNameEnvPrefix}-app-bucket", // default value but override if needed in calling method
                Env = new Amazon.CDK.Environment
                {
                    Account = EnvironmentConstants.AwsAccountNumber,
                    Region = EnvironmentConstants.AWS_REGION,
                },
                Tags = new Dictionary<string, string>()
                {
                    { "Application", APPLICATION_NAME_TAG },
                    { "Environment", EnvironmentConstants.EnvironmentShortName }
                },
                ApiProps = new ApiProps()
                {
                    AppSecurityGroup = EnvironmentConstants.AppSecurityGroup,
                    AppSubnetIds = EnvironmentConstants.AppSubnets,
                    ApiSecretsName = $"{APPLICATION_NAME_TAG}{EnvironmentConstants.EnvironmentShortName}Secrets",
                    ApiDatabaseNameEnvironmentVarPrefix = API_ENV_DB_PREFIX,
                    ConfigureS3Bucket = false,
                    DistributionDomainVanityName = null,
                    EcrRepositoryArn = $"arn:aws:ecr:ca-central-1:{EnvironmentConstants.AwsAccountNumber}:repository/{stackNameEnvPrefix}", // Good as long as the repo follows the naming convention in the bootstrap
                    EcrImageHash = "latest",
                    FargateTaskExecuteCommandEnabled = false,
                    FargateDesiredServiceCount = 1,
                    FargateTaskCpuUnits = 512,
                    FargateApiRamLimitMiB = 1024,
                    ServicePort = 5000,
                    SwaggerEnabled = "true",
                    ApiEnvironmentVariables = new List<Tuple<string, string>>()
                    {
                        new Tuple<string, string>("AWS_LOGGING_ENABLED", "true"),
                    },
                    ApiEnvironmentSecrets = new List<Tuple<string, string>>()
                    {
                        new Tuple<string, string>("MY_SECRET", "MY_SECRET")
                    }
                },
                DatabaseProps = new DatabaseProps()
                {
                    DbName = stackNameEnvPrefix
                },
                WebProps = new WebProps() // not needed for backend only apps
                {
                    AngularDistPath = ANGULAR_BUILD_LOCATION,
                    ProdCertificateArn = null,
                    ProdDomainName = ""
                }
            };
            return props;
        }
    }
}