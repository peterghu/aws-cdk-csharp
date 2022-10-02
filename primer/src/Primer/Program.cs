using Amazon.CDK;
using aws_cdk_csharp.Common.Constants;
using aws_cdk_csharp.Primer.Props;
using Environment = Amazon.CDK.Environment;

namespace aws_cdk_csharp.Primer
{
    internal sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            new PrimerStack(app, "PrimerStackDev", GetDevStackProps());
            //new PrimerStack(app, "PrimerStackUat", GetUatStackProps());

            app.Synth();
        }

        private static PrimerProps GetDevStackProps()
        {
            var awsEnvironmentConstants = new EnvironmentConstantsManager().GetDev();

            var props = new PrimerProps()
            {
                StackObjectPrefix = "primer-dev",
                VpcId = awsEnvironmentConstants.VPCId,
                Env = new Environment
                {
                    Account = awsEnvironmentConstants.AwsAccountNumber,
                    Region = awsEnvironmentConstants.AWS_REGION,
                },
                DatabaseClusterDeleteProtection = false,
                DatabaseUserName = "clusterAdmin",
                DatabaseServerInstances = 1,
                DefaultPostgresPort = 5432,
                DatabaseSubnetIds = awsEnvironmentConstants.DatabaseSubnets,
                DatabaseSecurityGroup = awsEnvironmentConstants.DataSecurityGroup,
                DatabaseBackupRetentionInDays = 1,
            };

            return props;
        }

        // Could have extra versions for UAT, PROD, etc...
    }
}