using System;
using System.Collections.Generic;

namespace aws_cdk_csharp.Common.Props
{
    public class ApiProps
    {
        public string AppSecurityGroup { get; set; }
        public List<string> AppSubnetIds { get; set; }
        public string ApiSecretsName { get; set; }
        public string ApiDatabaseNameEnvironmentVarPrefix { get; set; }

        public List<Tuple<string, string>> ApiEnvironmentSecrets { get; set; }
        public List<Tuple<string, string>> ApiEnvironmentVariables { get; set; }
        public bool ConfigureS3Bucket { get; set; }
        public bool FargateTaskExecuteCommandEnabled { get; set; }
        public int FargateDesiredServiceCount { get; set; }
        public int FargateTaskCpuUnits { get; set; }
        public int FargateApiRamLimitMiB { get; set; }
        public bool FargateCircuitBreaker { get; set; }
        public int ServicePort { get; set; }
        public string EcrRepositoryArn { get; set; }
        public string DistributionDomainVanityName { get; set; }
        public string EcrImageHash { get; set; }
        public string AspNetEnvironment { get; set; }
        public int ApiGatewayMinCompressionSize { get; set; }
        public string SwaggerEnabled { get; set; }

        public ApiProps()
        {
            AppSubnetIds = new List<string>();
            FargateTaskExecuteCommandEnabled = false;
            FargateDesiredServiceCount = 1;
            FargateTaskCpuUnits = 512; // 1024
            FargateApiRamLimitMiB = 1024; // 2048
            FargateCircuitBreaker = false;
            ServicePort = 5000;
            AspNetEnvironment = "development"; // staging
            ApiGatewayMinCompressionSize = 10 * 1024;
            SwaggerEnabled = "false";
            ConfigureS3Bucket = false;
        }
    }
}