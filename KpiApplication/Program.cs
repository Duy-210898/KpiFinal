using DevExpress.LookAndFeel;
using DevExpress.UserSkins;
using DevExpress.XtraEditors;
using KpiApplication.Common;
using KpiApplication.Utils;
using System;
using System.Configuration;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace KpiApplication
{
    internal static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [STAThread]
        static void Main()
        {
            bool isNewInstance;
            using (Mutex mutex = new Mutex(true, "KpiApplication_UniqueMutex", out isNewInstance))
            {
                if (!isNewInstance)
                {
                    try
                    {
                        // 👉 Nếu form chính của bạn có Text = Lang.MainTitle thì dùng dòng này
                        IntPtr hWnd = FindWindow(null, Lang.MainTitle);
                        if (hWnd != IntPtr.Zero)
                        {
                            SetForegroundWindow(hWnd);
                        }
                        else
                        {
                            MessageBoxHelper.ShowInfo(Lang.ApplicationAlreadyRunning);
                        }
                    }
                    catch
                    {
                        MessageBoxHelper.ShowInfo(Lang.ApplicationAlreadyRunning);
                    }
                    return;
                }

                // 🌐 Thiết lập ngôn ngữ
                string selectedCulture = KpiApplication.Properties.Settings.Default.AppCulture ?? "en";
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(selectedCulture);
                Thread.CurrentThread.CurrentCulture = new CultureInfo(selectedCulture);


               
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // 🎨 DevExpress skin
                BonusSkins.Register();
                string lastSkinName = ConfigurationManager.AppSettings["LastSkinName"];
                if (string.IsNullOrEmpty(lastSkinName))
                    lastSkinName = "WXI";
                 
                UserLookAndFeel.Default.SetSkinStyle(lastSkinName);
                WindowsFormsSettings.DefaultFont = new System.Drawing.Font("Arial", 9);

                // 🚀 Chạy form chính
                Application.Run(new Login());
            }
        }
    }
}
