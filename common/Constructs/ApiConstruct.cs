using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.SecretsManager;
using aws_cdk_csharp.Common.Props;
using Constructs;
using System.Collections.Generic;
using EcsProtocol = Amazon.CDK.AWS.ECS.Protocol;
using EcsSecret = Amazon.CDK.AWS.ECS.Secret;
using ElbProtocol = Amazon.CDK.AWS.ElasticLoadBalancingV2.Protocol;

namespace aws_cdk_csharp.Common.Constructs
{
    public class ApiConstruct : Construct
    {
        private FargateService _fargateService;
        public readonly RestApi _apiGateway;
        private readonly string _fargateServiceArn;
        private readonly List<string> _taskRoles;

        public ApiConstruct(Construct scope, string id, MicroServiceStackProps props, IVpc targetVpc) : base(scope, id)
        {
            string stageName = props.StackObjectPrefix.Replace("/ -/g", "");

            var cluster = new Cluster(scope, $"{ props.StackObjectPrefix }-Cluster", new ClusterProps
            {
                EnableFargateCapacityProviders = true,
                ContainerInsights = false,
                Vpc = targetVpc,
            });

            var taskRole = new Role(scope, $"{props.StackObjectPrefix}-ApiTaskRole", new RoleProps()
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
                Description = "Role assumed by the task, and by extension, the container instance."
            });

            // Grant the task access to CloudWatch Logging so the code can log.
            taskRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("CloudWatchFullAccess"));

            // Update S3 bucket policies
            if (props.ApiProps.ConfigureS3Bucket)
            {
                // needs to be a different id compared to the webBucket previously
                var docBucket = Bucket.FromBucketArn(scope, $"{props.StackObjectPrefix}-DocBucket", props.BucketArn);

                taskRole.AddToPolicy(
                    new PolicyStatement(
                        new PolicyStatementProps()
                        {
                            Actions = new string[] {
                                "s3:GetObject",
                                "s3:ListBucket"
                            },
                            Effect = Effect.ALLOW,
                            Resources = new string[] { docBucket.BucketArn, docBucket.ArnForObjects("*") }
                        }
                    )
                );
            }

            var logDriver = new AwsLogDriver(new AwsLogDriverProps()
            {
                // logRetention not necessary
                Mode = AwsLogDriverMode.NON_BLOCKING,
                StreamPrefix = $"{props.StackObjectPrefix}-",
                // LogGroup = logGroup
            });

            // NEED valid ECR ARN here
            var ecrRepo = Repository.FromRepositoryArn(scope, $"{props.StackObjectPrefix}-EcrRepo", props.ApiProps.EcrRepositoryArn);

            var taskDefinition = new FargateTaskDefinition(scope, $"{props.StackObjectPrefix}-ApiTaskDefinition", new FargateTaskDefinitionProps
            {
                Cpu = props.ApiProps.FargateTaskCpuUnits,
                MemoryLimitMiB = props.ApiProps.FargateApiRamLimitMiB,
                TaskRole = taskRole,
            });

            Dictionary<string, string> environmentVariables = GetEnvironmentVariables(props);
            Dictionary<string, EcsSecret> environmentVarSecrets = GetEnvironmentVariableSecrets(scope, props);

            taskDefinition.AddContainer($"{props.StackObjectPrefix}-ApiContainer", new ContainerDefinitionOptions()
            {
                Environment = environmentVariables,
                Secrets = environmentVarSecrets,
                // see: https://docs.aws.amazon.com/AmazonECS/latest/APIReference/API_HealthCheck.html
                // healthCheck: { command: ['CMD-SHELL', 'curl -f http://localhost/ || exit 1'] },

                Image = ContainerImage.FromEcrRepository(ecrRepo, props.ApiProps.EcrImageHash),
                Logging = logDriver,
                PortMappings = new PortMapping[] {
                    new PortMapping()
                    {
                        ContainerPort = props.ApiProps.ServicePort,
                        Protocol = EcsProtocol.TCP,
                    }
                }
            });

            _taskRoles = new List<string> { taskDefinition.TaskRole.RoleArn };

            if (taskDefinition.ExecutionRole != null)
            {
                _taskRoles.Add(taskDefinition.ExecutionRole.RoleArn);
            }

            var serviceSecurityGroup = new SecurityGroup(scope, $"{props.StackObjectPrefix}-TaskSecurityGroup", new SecurityGroupProps()
            {
                Description = "API security group with ingress rules for the VPC app subnets.",
                Vpc = targetVpc,
            });

            // from our VPC, find the subnets for our appSubnetIds, and use them to add rules to our securityGroup
            foreach (var subnet in targetVpc.IsolatedSubnets)
            {
                if (props.ApiProps.AppSubnetIds.Contains(subnet.SubnetId))
                {
                    serviceSecurityGroup.AddIngressRule(
                        Peer.Ipv4(subnet.Ipv4CidrBlock),
                        Port.Tcp(props.ApiProps.ServicePort),
                        $"{ props.StackObjectPrefix} API ingress rule."
                    );
                }
            }

            // Secret name must be correct or else this will fail
            _fargateService = new FargateService(scope, $"{props.StackObjectPrefix}-ApiFargateService", new FargateServiceProps()
            {
                CircuitBreaker = new DeploymentCircuitBreaker() { Rollback = props.ApiProps.FargateCircuitBreaker },
                Cluster = cluster,
                DesiredCount = props.ApiProps.FargateDesiredServiceCount,
                EnableECSManagedTags = true,
                EnableExecuteCommand = props.ApiProps.FargateTaskExecuteCommandEnabled,
                PlatformVersion = FargatePlatformVersion.VERSION1_4,
                PropagateTags = PropagatedTagSource.SERVICE,
                SecurityGroups = new ISecurityGroup[] {
                   SecurityGroup.FromSecurityGroupId(scope, $"{props.StackObjectPrefix}-AppSecurityGroup", props.ApiProps.AppSecurityGroup),
                   serviceSecurityGroup,
                },
                TaskDefinition = taskDefinition,
                VpcSubnets = new SubnetSelection()
                {
                    SubnetFilters = new SubnetFilter[] { SubnetFilter.ByIds(props.ApiProps.AppSubnetIds.ToArray()) },
                },
            });

            _fargateServiceArn = _fargateService.ServiceArn;

            var loadbalancer = new NetworkLoadBalancer(scope, $"{props.StackObjectPrefix}-NLB", new NetworkLoadBalancerProps()
            {
                CrossZoneEnabled = true,
                InternetFacing = false,
                Vpc = targetVpc,
                VpcSubnets = new SubnetSelection()
                {
                    SubnetFilters = new SubnetFilter[] { SubnetFilter.ByIds(props.ApiProps.AppSubnetIds.ToArray()) }
                }
            });

            var listener = new NetworkListener(scope, $"{props.StackObjectPrefix}-NLBListener", new NetworkListenerProps()
            {
                LoadBalancer = loadbalancer,
                Port = props.ApiProps.ServicePort,
            });

            listener.AddTargets($"{props.StackObjectPrefix}-ApiServiceTarget", new AddNetworkTargetsProps()
            {
                DeregistrationDelay = Amazon.CDK.Duration.Seconds(20),
                Port = props.ApiProps.ServicePort,
                Protocol = ElbProtocol.TCP,
                Targets = new INetworkLoadBalancerTarget[] { _fargateService },
            });

            var vpcLink = new VpcLink(scope, $"{props.StackObjectPrefix}-VpcLink", new VpcLinkProps()
            {
                Description = "VPC link for API Gateway integration.",
                Targets = new INetworkLoadBalancer[] { loadbalancer },
            });

            // API gateway uses VpcLink to reach the NLB, the NetworkListener, and ultimately the Fargate service
            _apiGateway = new RestApi(scope, $"{props.StackObjectPrefix}-RestApi", new RestApiProps()
            {
                // treat multipart form data as binary, for file uploads that pass through the API (i.e. the stamps)
                BinaryMediaTypes = new string[] { "multipart/*" },

                CloudWatchRole = false,
                DeployOptions = new StageOptions()
                {
                    // don't cache API calls
                    CachingEnabled = false,
                    StageName = stageName,
                },
                Description = "REST API Gateway",
                EndpointConfiguration = new EndpointConfiguration()
                {
                    Types = new EndpointType[] { EndpointType.REGIONAL },
                },
                MinimumCompressionSize = props.ApiProps.ApiGatewayMinCompressionSize,
                RestApiName = $"{props.StackNameLowerCasePrefix}-api",
            });

            _apiGateway.Root.AddProxy(new ProxyResourceOptions()
            {
                AnyMethod = true,
                DefaultIntegration = new Integration(new IntegrationProps()
                {
                    IntegrationHttpMethod = "ANY",
                    Options = new IntegrationOptions()
                    {
                        ConnectionType = ConnectionType.VPC_LINK,
                        VpcLink = vpcLink,
                        RequestParameters = new Dictionary<string, string>()
                        {
                            ["integration.request.path.proxy"] = "method.request.path.proxy",
                        }
                    },
                    Type = IntegrationType.HTTP_PROXY,
                    Uri = $"http://{loadbalancer.LoadBalancerDnsName}:{props.ApiProps.ServicePort}/" + "{proxy}",
                }),
                DefaultMethodOptions = new MethodOptions()
                {
                    RequestParameters = new Dictionary<string, bool>()
                    {
                        ["method.request.path.proxy"] = true,
                    }
                },
            });
        }

        private Dictionary<string, EcsSecret> GetEnvironmentVariableSecrets(Construct scope, MicroServiceStackProps props)
        {
            var apiSecretsName = props.ApiProps.ApiSecretsName;
            ISecret apiSecrets = Amazon.CDK.AWS.SecretsManager.Secret.FromSecretNameV2(scope, apiSecretsName, apiSecretsName);

            var dbSecretName = props.DatabaseProps?.DbSecretName;
            ISecret dbSecret = dbSecretName != null ? Amazon.CDK.AWS.SecretsManager.Secret.FromSecretNameV2(scope, dbSecretName, dbSecretName) : null;

            var environmentVarSecrets = BuildCommonSecrets(dbSecret, props.ApiProps.ApiDatabaseNameEnvironmentVarPrefix);

            var secretEnvVars = props.ApiProps.ApiEnvironmentSecrets;

            if (secretEnvVars != null)
            {
                foreach (var secretEnvVar in secretEnvVars)
                {
                    // Update/override common value
                    environmentVarSecrets[secretEnvVar.Item1] = EcsSecret.FromSecretsManager(apiSecrets, secretEnvVar.Item2);
                }
            }

            return environmentVarSecrets;
        }

        private Dictionary<string, string> GetEnvironmentVariables(MicroServiceStackProps props)
        {
            var environmentVariables = BuildCommonEnvironmentVariables(props.ApiProps, props.DatabaseProps);

            // Set task definition environment variables specific to this api
            var customEnvVars = props.ApiProps.ApiEnvironmentVariables;
            if (customEnvVars != null)
            {
                foreach (var apiEnvVar in customEnvVars)
                {
                    // Update/override common value
                    environmentVariables[apiEnvVar.Item1] = apiEnvVar.Item2;
                }
            }

            return environmentVariables;
        }

        private Dictionary<string, string> BuildCommonEnvironmentVariables(ApiProps apiProps, DatabaseProps dbProps)
        {
            var dict = new Dictionary<string, string>()
            {
                ["ASPNETCORE_ENVIRONMENT"] = apiProps.AspNetEnvironment,
                ["ASPNETCORE_URLS"] = $"http://*:{apiProps.ServicePort}",
                ["AWS_DEFAULT_REGION"] = "ca-central-1",
                ["AWS_ECR_IMAGE_HASH"] = apiProps.EcrImageHash,
                ["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "false",
                ["SWAGGER_ENABLED"] = apiProps.SwaggerEnabled,
                ["TZ"] = "America/Edmonton",
            };

            if (!string.IsNullOrEmpty(apiProps.ApiDatabaseNameEnvironmentVarPrefix)
                && !string.IsNullOrEmpty(dbProps?.DbName))
            {
                dict[$"{apiProps.ApiDatabaseNameEnvironmentVarPrefix}_NAME"] = dbProps.DbName;
            }

            return dict;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, EcsSecret> BuildCommonSecrets(ISecret dbSecret, string apiDatabaseNameEnvironmentVarPrefix)
        {
            var dict = new Dictionary<string, EcsSecret>();

            if (!string.IsNullOrEmpty(apiDatabaseNameEnvironmentVarPrefix) && dbSecret != null)
            {
                dict = new Dictionary<string, EcsSecret>()
                {
                    [$"{apiDatabaseNameEnvironmentVarPrefix}_SERVER"] = EcsSecret.FromSecretsManager(dbSecret, "host"),
                    [$"{apiDatabaseNameEnvironmentVarPrefix}_SERVER_PORT"] = EcsSecret.FromSecretsManager(dbSecret, "port"),
                    [$"{apiDatabaseNameEnvironmentVarPrefix}_USER"] = EcsSecret.FromSecretsManager(dbSecret, "username"),
                    [$"{apiDatabaseNameEnvironmentVarPrefix}_PASSWORD"] = EcsSecret.FromSecretsManager(dbSecret, "password")
                };
            }

            return dict;
        }
    }
}