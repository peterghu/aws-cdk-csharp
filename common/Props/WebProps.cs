using System;
using System.Collections.Generic;
using System.Text;

namespace aws_cdk_csharp.Common.Props
{
    public class WebProps
    {
        public string AngularDistPath { get; set; }
        public string ProdCertificateArn { get; set; }
        public string ProdDomainName { get; set; }
    }
}
