using DevExpress.XtraEditors;
using System;
using System.Windows.Forms;

namespace KpiApplication.Utils
{
    public static class MessageBoxHelper
    {
        public static void ShowError(string message, Exception ex = null)
        {
            string fullMessage = ex == null ? message : $"{message}: {ex.Message}";
            XtraMessageBox.Show(fullMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void ShowWarning(string message)
        {
            XtraMessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void ShowInfo(string message)
        {
            XtraMessageBox.Show(message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static DialogResult ShowConfirm(string message, string caption = "Confirm")
        {
            return XtraMessageBox.Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }
    }
}


