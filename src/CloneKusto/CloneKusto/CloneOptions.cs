using System.Collections.Generic;
using CommandLine;

namespace CloneKusto
{
    public class CloneOptions
    {
        [Option('d', "databases", Required = true, Separator = ',', HelpText = "The name(s) of the database(s) to pull")]
        public IEnumerable<string> DatabaseNames { get; set; }

        [Option('o', "output", HelpText = "Location to place the generated output. The default is the current directory.")]
        public string OutputDirectory { get; set; }

        [Option('u', "clusteruri", HelpText = "The URI of the Kusto cluster")]
        public string ClusterUri { get; set; }

        [Option("auth-clientid", HelpText = "The Azure AD application client ID to authenticate as")]
        public string ApplicationClientId { get; set; }

        [Option("auth-tenantid", HelpText = "The Azure AD tenant ID to be used for authentication")]
        public string TenantId { get; set; }

        [Option("auth-certthumbprint", HelpText = "The thumbprint of the certificate to be used for authentication")]
        public string ApplicationCertificateThumbprint { get; set; }

        [Option("auth-clientsecret", HelpText = "The secret to be used for authentication")]
        public string ApplicationClientSecret { get; set; }

        [Option("verbose", HelpText = "Enable verbose logging")]
        public bool Verbose { get; set; }
    }
}
