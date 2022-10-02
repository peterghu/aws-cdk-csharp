using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;
using aws_cdk_csharp.Primer.Constructs;
using aws_cdk_csharp.Primer.Props;

namespace aws_cdk_csharp.Primer
{
    public class PrimerStack : Stack
    {
        internal PrimerStack(Construct scope, string id, PrimerProps props) : base(scope, id, props)
        {
            var targetVpc = Vpc.FromLookup(this, "vpc-lookup", new VpcLookupOptions() { VpcId = props.VpcId });

            // DB TIER
            var dbTier = new PrimerDbConstruct(this, "db", props, targetVpc);

            new CfnOutput(this, $"{props.StackObjectPrefix}-DbClusterSecretArn", new CfnOutputProps()
            {
                Description = "DB Cluster Secret ARN",
                Value = dbTier._dbCluster.Secret.SecretArn
            });

            new CfnOutput(this, $"{props.StackObjectPrefix}-DbClusterSecretName", new CfnOutputProps()
            {
                Description = "DB Cluster Secret Name",
                Value = dbTier._dbCluster.Secret.SecretName
            });
        }
    }
}