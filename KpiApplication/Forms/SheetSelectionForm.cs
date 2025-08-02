using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using KpiApplication.Common;

namespace KpiApplication.Forms
{
    public partial class SheetSelectionForm : DevExpress.XtraEditors.XtraForm
    {
        public string SelectedSheet { get; private set; }

        public SheetSelectionForm(List<string> sheetNames)
        {
            InitializeComponent();
            ApplyLocalizedText();
            sheetNames.Reverse();
            this.KeyPreview = true;

            // Sử dụng DevExpress ComboBoxEdit thay cho ComboBox
            cbxSheet.Properties.Items.AddRange(sheetNames);
            cbxSheet.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        }
        private void ApplyLocalizedText()
        {
            layoutControlItem1.Text = Lang.SheetSelect;
        }


        private void buttonOK_Click(object sender, EventArgs e)
        {
            SelectedSheet = cbxSheet.SelectedItem?.ToString();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void SheetSelectionForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }
    }
}
