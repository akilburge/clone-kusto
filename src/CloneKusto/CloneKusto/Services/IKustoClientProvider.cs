using Kusto.Data.Common;

namespace CloneKusto.Services
{
    public interface IKustoClientProvider
    {
        ICslAdminProvider GetClient(string database);
    }
}
