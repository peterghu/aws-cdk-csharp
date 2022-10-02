using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Deployment;
using Constructs;
using aws_cdk_csharp.Common.Props;

namespace aws_cdk_csharp.Common.Constructs
{
    public class WebConstruct : Construct
    {
        public readonly Bucket _bucket;

        public WebConstruct(Construct scope, string id, MicroServiceStackProps props) : base(scope, id)
        {
            _bucket = new Bucket(scope, $"{props.StackObjectPrefix}-AppBucket", new BucketProps()
            {
                // let CDK generate a name for us according to best practices
                //BucketName = "",
                AutoDeleteObjects = true,
                BlockPublicAccess = Amazon.CDK.AWS.S3.BlockPublicAccess.BLOCK_ALL,
                Encryption = BucketEncryption.S3_MANAGED,
                RemovalPolicy = RemovalPolicy.DESTROY,
                Versioned = false,

                Cors = new CorsRule[] {
                        new CorsRule(){
                            AllowedHeaders = new [] { "*" },
                            AllowedMethods = new [] { HttpMethods.GET, HttpMethods.PUT, HttpMethods.HEAD },
                            AllowedOrigins = new [] { "" },
                            ExposedHeaders = { },
                        }
                    }
                
            });

            // TODO: expose this bucket instance and then we can set the CloudFront origin later in the stack?

            new BucketDeployment(scope, $"{props.StackObjectPrefix}-WebDeployment", new BucketDeploymentProps()
            {
                DestinationBucket = _bucket,
                RetainOnDelete = false,
                StorageClass = Amazon.CDK.AWS.S3.Deployment.StorageClass.STANDARD,
                Sources = new ISource[] { Source.Asset(props.WebProps.AngularDistPath) }
            });
        }
    }
}