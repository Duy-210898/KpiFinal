namespace KpiApplication.Controls
{
    partial class ucProductionSchedules
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.gridProductionSchedules = new DevExpress.XtraGrid.GridControl();
            this.dgvProductionSchedules = new DevExpress.XtraGrid.Views.Grid.GridView();
            ((System.ComponentModel.ISupportInitialize)(this.gridProductionSchedules)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvProductionSchedules)).BeginInit();
            this.SuspendLayout();
            // 
            // gridProductionSchedules
            // 
            this.gridProductionSchedules.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridProductionSchedules.Location = new System.Drawing.Point(0, 0);
            this.gridProductionSchedules.MainView = this.dgvProductionSchedules;
            this.gridProductionSchedules.Name = "gridProductionSchedules";
            this.gridProductionSchedules.Size = new System.Drawing.Size(904, 568);
            this.gridProductionSchedules.TabIndex = 0;
            this.gridProductionSchedules.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.dgvProductionSchedules});
            // 
            // dgvProductionSchedules
            // 
            this.dgvProductionSchedules.GridControl = this.gridProductionSchedules;
            this.dgvProductionSchedules.Name = "dgvProductionSchedules";
            // 
            // ucProductionSchedules
            // 
            this.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.Appearance.Options.UseBackColor = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gridProductionSchedules);
            this.Name = "ucProductionSchedules";
            this.Size = new System.Drawing.Size(904, 568);
            ((System.ComponentModel.ISupportInitialize)(this.gridProductionSchedules)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvProductionSchedules)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridProductionSchedules;
        private DevExpress.XtraGrid.Views.Grid.GridView dgvProductionSchedules;
    }
}
