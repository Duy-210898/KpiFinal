using DevExpress.XtraEditors;
using System;
using System.Windows.Forms;
using KpiApplication.Common; // Để dùng Lang.*

namespace KpiApplication.Utils
{
    public static class MessageBoxHelper
    {
        public static void ShowError(string message, Exception ex = null)
        {
            string fullMessage = ex == null ? message : $"{message}: {ex.Message}";
            XtraMessageBox.Show(fullMessage, Lang.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void ShowWarning(string message)
        {
            XtraMessageBox.Show(message, Lang.Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        public static bool ConfirmFileOverwrite(string fileName)
        {
            var result = XtraMessageBox.Show(
                string.Format(Lang.FileExistsConfirm, fileName),
                Lang.Confirm,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            return result == DialogResult.Yes;
        }

        public static void ShowInfo(string message)
        {
            XtraMessageBox.Show(message, Lang.Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static DialogResult ShowConfirm(string message, string caption = null)
        {
            return XtraMessageBox.Show(message, caption ?? Lang.Confirm, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }
        public static DialogResult ShowConfirmYesNoCancel(string message, string caption = null)
        {
            return XtraMessageBox.Show(message, caption ?? Lang.Confirm, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        }
    }
}
