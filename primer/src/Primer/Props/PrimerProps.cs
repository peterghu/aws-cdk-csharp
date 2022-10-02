using Amazon.CDK;
using System.Collections.Generic;

namespace aws_cdk_csharp.Primer.Props
{
    public class PrimerProps : StackProps
    {
        public bool IsProd { get; set; }
        public string StackObjectPrefix { get; set; }
        public string VpcId { get; set; }

        //public string DatabaseClusterDefaultDbName { get; set; }
        public bool DatabaseClusterDeleteProtection { get; set; }

        public string DatabaseUserName { get; set; }
        public string BastionIpForDbIngress { get; set; }
        public int DatabaseServerInstances { get; set; }
        public int DefaultPostgresPort { get; set; }
        public int DatabaseBackupRetentionInDays { get; set; }
        public string DatabaseSecurityGroup { get; set; }
        public List<string> DatabaseSubnetIds { get; set; }

        public PrimerProps()
        {
            StackObjectPrefix = "";
            DatabaseSubnetIds = new List<string>();
            DatabaseClusterDeleteProtection = true; // Err on the side of caution
        }
    }
}