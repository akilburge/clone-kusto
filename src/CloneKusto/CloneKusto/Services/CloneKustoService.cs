using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CloneKusto.Services
{
    public class CloneKustoService : ICloneKustoService
    {
        private readonly IKustoClientProvider _clientProvider;
        private readonly ILogger<CloneKustoService> _log;

        public CloneKustoService(
            IKustoClientProvider clientProvider,
            ILogger<CloneKustoService> log)
        {
            _clientProvider = clientProvider;
            _log = log;
        }

        public async Task CloneAsync(CloneOptions options, CancellationToken cancellationToken)
        {
            foreach (var database in options.DatabaseNames)
            {
                var client = _clientProvider.GetClient(database);

                throw new System.NotImplementedException();
            }
        }
    }
}