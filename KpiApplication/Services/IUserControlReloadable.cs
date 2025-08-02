using System.Threading.Tasks;

namespace KpiApplication.Common
{
    public interface IUserControlReloadable
    {
        Task ReloadDataAsync();
    }
}
