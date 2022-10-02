using Amazon.CDK;

namespace aws_cdk_csharp.Common.Props
{
    public class MicroServiceStackProps : StackProps
    {
        public bool IsProd { get; set; }

        public string VpcId { get; set; }

        /// <summary>
        /// for CDK generated IDs
        /// </summary>
        public string StackObjectPrefix { get; set; }

        /// <summary>
        // for human generated resources
        /// </summary>
        public string StackNameLowerCasePrefix { get; set; }

        public string BucketArn { get; set; }
        public DatabaseProps DatabaseProps { get; set; }
        public ApiProps ApiProps { get; set; }
        public WebProps WebProps { get; set; }
    }
}