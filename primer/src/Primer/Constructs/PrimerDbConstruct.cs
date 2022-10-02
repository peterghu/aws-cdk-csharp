using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.SecretsManager;
using Constructs;
using aws_cdk_csharp.Primer.Props;
using System.Text.Json;
using InstanceProps = Amazon.CDK.AWS.RDS.InstanceProps;

namespace aws_cdk_csharp.Primer.Constructs
{
    public class PrimerDbConstruct : Construct
    {
        public DatabaseCluster _dbCluster { get; set; }
        public Secret _secret { get; set; }

        public PrimerDbConstruct(Construct scope, string id, PrimerProps props, IVpc targetVpc) : base(scope, id)
        {
            var jsonStringTemplate = JsonSerializer.Serialize(new { username = props.DatabaseUserName });

            _secret = new Secret(scope, $"{props.StackObjectPrefix}-DatabaseSecrets", new SecretProps
            {
                Description = "Populated with values for DB HOST, PORT, DB name, username & password.",
                GenerateSecretString = new SecretStringGenerator
                {
                    ExcludePunctuation = true,
                    SecretStringTemplate = jsonStringTemplate,
                    GenerateStringKey = "password"
                }
            });

            var snFilter = SubnetFilter.ByIds(props.DatabaseSubnetIds.ToArray());

            SubnetFilter[] arr = new[] { snFilter };

            var subnetGroup = new SubnetGroup(scope, $"{props.StackObjectPrefix}-DataSubnetGroup", new SubnetGroupProps
            {
                Description = "Subnet group for database cluster.",
                Vpc = targetVpc,
                VpcSubnets = new SubnetSelection() { SubnetFilters = arr }
            });

            // EC2 Bastion instance
            var centralEc2AccessSecurityGroup = new SecurityGroup(scope, $"{props.StackObjectPrefix}-CentralEC2AccessSG", new SecurityGroupProps()
            {
                Description = "Security group to grant inbound access to DB from an EC2 instance in the Central (tools) account.",
                Vpc = targetVpc
            });

            //centralBastionIp
            centralEc2AccessSecurityGroup.AddIngressRule(Peer.Ipv4($"{props.BastionIpForDbIngress}/32"),
                                                         Port.Tcp(props.DefaultPostgresPort),
                                                         $"{props.StackObjectPrefix}-Postgres ingress rule.");

            _dbCluster = new DatabaseCluster(this, $"{props.StackObjectPrefix}-DbCluster", new DatabaseClusterProps()
            {
                Engine = DatabaseClusterEngine.AuroraPostgres(new AuroraPostgresClusterEngineProps { Version = AuroraPostgresEngineVersion.VER_13_4 }),
                Backup = new BackupProps() { Retention = Duration.Days(props.DatabaseBackupRetentionInDays) },
                DeletionProtection = props.DatabaseClusterDeleteProtection,
                //DefaultDatabaseName - OPTIONAL, not needed if other projects create the DB
                SubnetGroup = subnetGroup,
                Credentials = Credentials.FromSecret(_secret),
                InstanceProps = new InstanceProps()
                {
                    EnablePerformanceInsights = true,
                    //  ref: https://docs.aws.amazon.com/AmazonRDS/latest/AuroraUserGuide/Concepts.DBInstanceClass.html
                    //InstanceType = InstanceType.Of(InstanceClass.T3, InstanceSize.SMALL),
                    // 7 days (default) is free: https://aws.amazon.com/rds/performance-insights/pricing/
                    PerformanceInsightRetention = PerformanceInsightRetention.DEFAULT,
                    PubliclyAccessible = false,
                    SecurityGroups = new[] {
                        SecurityGroup.FromSecurityGroupId(scope, $"{props.StackObjectPrefix}-DbSecurityGroup", props.DatabaseSecurityGroup),
                        centralEc2AccessSecurityGroup
                    },
                    Vpc = targetVpc
                },
                Instances = props.DatabaseServerInstances,
                StorageEncrypted = true,
            });
        }
    }
}