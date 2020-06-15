using System.Threading;
using System.Threading.Tasks;

namespace CloneKusto.Services
{
    public interface ICloneKustoService
    {
        Task CloneAsync(CloneOptions options, CancellationToken cancellationToken);
    }
}
