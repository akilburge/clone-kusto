using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;

namespace CloneKusto.Services
{
    public class KustoClientProvider : IKustoClientProvider
    {
        private readonly string _hostAddress;
        private readonly string _applicationClientId;
        private readonly string _applicationKey;
        private readonly string _applicationCertificateThumbprint;
        private readonly string _authority;

        public KustoClientProvider(
            string hostAddress,
            string applicationClientId,
            string applicationKey,
            string applicationCertificateThumbprint,
            string authority)
        {
            _hostAddress = hostAddress;
            _applicationClientId = applicationClientId;
            _applicationKey = applicationKey;
            _applicationCertificateThumbprint = applicationCertificateThumbprint;
            _authority = authority;
        }

        public ICslAdminProvider GetClient(string database)
        {
            var builder = new KustoConnectionStringBuilder(_hostAddress, database);

            if (!string.IsNullOrEmpty(_applicationKey))
            {
                builder = builder.WithAadApplicationKeyAuthentication(_applicationClientId, _applicationKey, _authority);
            }
            else if (!string.IsNullOrEmpty(_applicationCertificateThumbprint))
            {
                builder = builder.WithAadApplicationThumbprintAuthentication(_applicationClientId, _applicationCertificateThumbprint, _authority);
            }

            return KustoClientFactory.CreateCslAdminProvider(builder);
        }
    }
}