using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.Nodejs;
using Amazon.CDK.AWS.S3;
using aws_cdk_csharp.Common.Constructs;
using aws_cdk_csharp.Common.Props;
using Constructs;
using System.Collections.Generic;
using System.IO;

namespace ExampleApp
{
    public class ExampleAppStack : Stack
    {
        internal ExampleAppStack(Construct scope, string id, MicroServiceStackProps props = null) : base(scope, id, props)
        {
            ICertificate distributionCertificate = props.IsProd && !string.IsNullOrEmpty(props.WebProps.ProdCertificateArn) ?
                Certificate.FromCertificateArn(this, $"{props.StackObjectPrefix}-ProdCertificate", props.WebProps.ProdCertificateArn)
                : null;

            var domainNames = props.IsProd ? new string[] { props.WebProps.ProdDomainName } : new string[] { };

            var targetVpc = Vpc.FromLookup(this, "vpc-lookup", new VpcLookupOptions() { VpcId = props.VpcId });

            //WEB TIER
            var webBucket = Bucket.FromBucketArn(this, $"{ props.StackObjectPrefix}-WebBucket", props.BucketArn);

            var distribution = new Distribution(this, $"{ props.StackObjectPrefix }-Distribution", new DistributionProps()
            {
                Certificate = distributionCertificate,
                Comment = $"Distribution ({props.StackNameLowerCasePrefix}) with two origins: static (app) resources & API cluster",
                DefaultBehavior = new BehaviorOptions()
                {
                    CachePolicy = CachePolicy.CACHING_OPTIMIZED, //Easier for debugging
                    Origin = new S3Origin(webBucket, new S3OriginProps() { OriginPath = "/" }),
                    ViewerProtocolPolicy = ViewerProtocolPolicy.HTTPS_ONLY,
                },
                DefaultRootObject = "index.html",
                DomainNames = domainNames,
                // non-existing physical routes should be redirected to index.html, to be handled by the angular app
                // both 403 & 404 are required
                ErrorResponses = new ErrorResponse[]
                {
                    new ErrorResponse(){
                        HttpStatus= 404,
                        ResponseHttpStatus= 200,
                        ResponsePagePath= "/index.html",
                    },
                },
                HttpVersion = HttpVersion.HTTP2,
                MinimumProtocolVersion = SecurityPolicyProtocol.TLS_V1_2_2021,
                PriceClass = PriceClass.PRICE_CLASS_100,
            });

            // API TIER
            // Let API app create DB
            var apiTier = new ApiConstruct(this, "api", props, targetVpc);

            var apiOrigin = Fn.ParseDomainName(apiTier._apiGateway.Url);

            var cachePolicy = new CachePolicy(this, $"{ props.StackObjectPrefix}-ApiCachePolicy", new CachePolicyProps()
            {
                Comment = "API cache policy with very short TTL and pass-thru Authorization header & query strings.",
                DefaultTtl = Duration.Seconds(1),
                EnableAcceptEncodingGzip = true,
                HeaderBehavior = CacheHeaderBehavior.AllowList("Authorization"),
                MaxTtl = Duration.Seconds(1),
                MinTtl = Duration.Seconds(0),
                QueryStringBehavior = CacheQueryStringBehavior.All(),
            });

            var apiHttpOrigin = new HttpOrigin(apiOrigin, new HttpOriginProps()
            {
                OriginPath = $"/{ apiTier._apiGateway.DeploymentStage.StageName}",
                OriginSslProtocols = new OriginSslPolicy[] { OriginSslPolicy.TLS_V1_2 },
                ProtocolPolicy = OriginProtocolPolicy.HTTPS_ONLY,
            });

            distribution.AddBehavior("api/*", apiHttpOrigin, new AddBehaviorOptions()
            {
                AllowedMethods = AllowedMethods.ALLOW_ALL,
                CachePolicy = cachePolicy,
                ViewerProtocolPolicy = ViewerProtocolPolicy.HTTPS_ONLY,
            });

            if (props.ApiProps.SwaggerEnabled.ToLower() == "true")
            {
                distribution.AddBehavior("swagger/*", apiHttpOrigin, new AddBehaviorOptions()
                {
                    AllowedMethods = AllowedMethods.ALLOW_ALL,
                    CachePolicy = cachePolicy,
                    ViewerProtocolPolicy = ViewerProtocolPolicy.HTTPS_ONLY,
                });
            }

            // create cronjob lambda function
            var cronjobLambda = new NodejsFunction(this, $"{ props.StackObjectPrefix}-SftpCronJob", new NodejsFunctionProps()
            {
                MemorySize = 1024,
                Timeout = Duration.Seconds(15),
                //Environment
                Environment = new Dictionary<string, string> {
                    { "MY_SECRET", "hello world" },
                },
                Runtime = Runtime.NODEJS_14_X,
                Handler = "handler",
                Entry = Path.Join(Directory.GetCurrentDirectory(), "/src/App/lambdajob/index.ts"),
                Description = "Test Lambda Job"
            });
        }
    }
}