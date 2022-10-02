using Amazon.CDK;
using Constructs;
using aws_cdk_csharp.Common.Constructs;
using aws_cdk_csharp.Common.Props;

namespace ExampleApp
{
    public class ExampleAppPrimerStack : Stack
    {
        internal ExampleAppPrimerStack(Construct scope, string id, MicroServiceStackProps props = null) : base(scope, id, props)
        {
            // only for web apps with frontends that require S3 bucket creation
            var webTier = new WebConstruct(this, "web", props);

            new CfnOutput(this, $"{props.StackObjectPrefix}-AppBucketArn", new CfnOutputProps()
            {
                Description = "App bucket ARN",
                Value = webTier._bucket.BucketArn
            });

        }
    }
}