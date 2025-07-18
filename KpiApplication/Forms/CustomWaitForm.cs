﻿using DevExpress.XtraWaitForm;
using System;

namespace KpiApplication.Forms
{
    public partial class CustomWaitForm : WaitForm
    {
        public CustomWaitForm()
        {
            InitializeComponent();
            this.progressPanel1.AutoHeight = true;
            this.progressPanel1.AutoWidth = true;
        }

        #region Overrides

        public override void SetCaption(string caption)
        {
            base.SetCaption(caption);
            this.progressPanel1.Caption = caption;
        }
        public override void SetDescription(string description)
        {
            base.SetDescription(description);
            this.progressPanel1.Description = description;
        }
        public override void ProcessCommand(Enum cmd, object arg)
        {
            base.ProcessCommand(cmd, arg);
        }

        #endregion

        public enum WaitFormCommand
        {
        }
    }
}