using DevExpress.XtraSplashScreen;
using KpiApplication.Forms;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KpiApplication.Utils
{
    public static class AsyncLoaderHelper
    {
        private static string GetDefaultDescription(string caption)
        {
            switch (caption)
            {
                case "Loading...": return "Please wait while the data is loading...";
                case "Exporting...": return "Please wait while the data is being exported...";
                case "Importing...": return "Please wait while the data is being imported...";
                default: return "Please wait while the operation is in progress...";
            }
        }

        private static void ShowSplash(Control control, string caption, string description)
        {
            var form = control.FindForm();
            SplashScreenManager.ShowForm(form, typeof(CustomWaitForm), true, true, false);
            SplashScreenManager.Default.SetWaitFormCaption(caption);
            SplashScreenManager.Default.SetWaitFormDescription(description);
        }

        private static void CloseSplash()
        {
            SplashScreenManager.CloseForm();
        }

        /// <summary>
        /// Load dữ liệu async kèm splash và disable control hoặc form (có kết quả)
        /// </summary>
        public static async Task LoadDataWithSplashAsync<T>(
            Control control,
            Func<T> loadDataFunc,
            Action<T> afterLoadAction,
            string caption = "Loading...",
            string description = null,
            bool disableForm = true)
        {
            var form = control.FindForm();
            if (string.IsNullOrEmpty(description))
                description = GetDefaultDescription(caption);

            try
            {
                if (disableForm && form != null)
                    form.Enabled = false;
                else
                    control.Enabled = false;

                ShowSplash(control, caption, description);

                var data = await Task.Run(loadDataFunc);
                control.Invoke(new MethodInvoker(() => afterLoadAction(data)));
            }
            finally
            {
                CloseSplash();
                if (disableForm && form != null)
                    form.Enabled = true;
                else
                    control.Enabled = true;
            }
        }

        /// <summary>
        /// Load dữ liệu async kèm splash và disable control hoặc form (không trả về kết quả)
        /// </summary>
        public static async Task LoadDataWithSplashAsync(
            Control control,
            Func<Task> loadDataFuncAsync,
            string caption = "Loading...",
            string description = null,
            bool disableForm = true)
        {
            var form = control.FindForm();
            if (string.IsNullOrEmpty(description))
                description = GetDefaultDescription(caption);

            try
            {
                if (disableForm && form != null)
                    form.Enabled = false;
                else
                    control.Enabled = false;

                ShowSplash(control, caption, description);

                await loadDataFuncAsync();
            }
            finally
            {
                CloseSplash();
                if (disableForm && form != null)
                    form.Enabled = true;
                else
                    control.Enabled = true;
            }
        }
    }
}
