using System.Collections.Generic;

namespace aws_cdk_csharp.Common.Constants
{
    public abstract class AwsCloudConstants
    {
        public string AWS_REGION = "ca-central-1";
    }

    public class EnvironmentConstants : AwsCloudConstants
    {
        public string EnvironmentShortName { get; set; }
        public string AwsAccountNumber { get; set; }
        public string AppSecurityGroup { get; set; }
        public string DataSecurityGroup { get; set; }
        public List<string> AppSubnets { get; set; }
        public List<string> DatabaseSubnets { get; set; }
        public string VPCId { get; set; }
        public string DatabaseSecretName { get; set; }
    }

    public class EnvironmentConstantsManager
    {
        public EnvironmentConstants GetDev()
        {
            // FILL IN
            var environmentConstants = new EnvironmentConstants()
            {
                EnvironmentShortName = "Dev",
                AppSecurityGroup = "",
                DataSecurityGroup = "",
                AppSubnets = new List<string>() { },
                DatabaseSubnets = new List<string> { },
                AwsAccountNumber = "",
                VPCId = "",
                DatabaseSecretName = "",
            };

            return environmentConstants;
        }

        public EnvironmentConstants GetProd()
        {
            var environmentConstants = new EnvironmentConstants()
            {
                EnvironmentShortName = "Prod",
                AppSecurityGroup = "",
                DataSecurityGroup = "",
                AppSubnets = new List<string>() { },
                DatabaseSubnets = new List<string> { },
                AwsAccountNumber = "",
                VPCId = "",
                DatabaseSecretName = "",
            };

            return environmentConstants;
        }
    }
}