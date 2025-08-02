using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KpiApplication.Services
{
    public interface ILoadingService
    {
        Task ShowLoadingAsync(string caption, string description, Func<Task> loadFunc);
    }
}
