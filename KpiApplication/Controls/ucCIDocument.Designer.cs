namespace KpiApplication.Controls
{
    partial class ucCIDocument
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
            this.components = new System.ComponentModel.Container();
            this.splitContainerControl1 = new DevExpress.XtraEditors.SplitContainerControl();
            this.layoutControl1 = new DevExpress.XtraLayout.LayoutControl();
            this.listBoxLayoutFile = new DevExpress.XtraEditors.ListBoxControl();
            this.btnExportFile = new DevExpress.XtraEditors.SimpleButton();
            this.btnDelete = new DevExpress.XtraEditors.SimpleButton();
            this.btnAddNew = new DevExpress.XtraEditors.SimpleButton();
            this.listBoxMachineList = new DevExpress.XtraEditors.ListBoxControl();
            this.lookUpModelName = new DevExpress.XtraEditors.LookUpEdit();
            this.Root = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItem1 = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItem1 = new DevExpress.XtraLayout.EmptySpaceItem();
            this.layoutControlItem3 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem4 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem6 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem5 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem2 = new DevExpress.XtraLayout.LayoutControlItem();
            this.pictureViewer = new DevExpress.XtraEditors.PictureEdit();
            this.pdfViewer = new DevExpress.XtraPdfViewer.PdfViewer();
            this.lblFileName = new DevExpress.XtraEditors.LabelControl();
            this.toolTipController1 = new DevExpress.Utils.ToolTipController(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel1)).BeginInit();
            this.splitContainerControl1.Panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel2)).BeginInit();
            this.splitContainerControl1.Panel2.SuspendLayout();
            this.splitContainerControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).BeginInit();
            this.layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.listBoxLayoutFile)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.listBoxMachineList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lookUpModelName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem6)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureViewer.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // splitContainerControl1
            // 
            this.splitContainerControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerControl1.Location = new System.Drawing.Point(0, 0);
            this.splitContainerControl1.Name = "splitContainerControl1";
            // 
            // splitContainerControl1.Panel1
            // 
            this.splitContainerControl1.Panel1.Controls.Add(this.layoutControl1);
            this.splitContainerControl1.Panel1.Text = "Panel1";
            // 
            // splitContainerControl1.Panel2
            // 
            this.splitContainerControl1.Panel2.Controls.Add(this.pictureViewer);
            this.splitContainerControl1.Panel2.Controls.Add(this.pdfViewer);
            this.splitContainerControl1.Panel2.Controls.Add(this.lblFileName);
            this.splitContainerControl1.Panel2.Text = "Panel2";
            this.splitContainerControl1.Size = new System.Drawing.Size(1016, 554);
            this.splitContainerControl1.SplitterPosition = 313;
            this.splitContainerControl1.TabIndex = 1;
            // 
            // layoutControl1
            // 
            this.layoutControl1.Controls.Add(this.listBoxLayoutFile);
            this.layoutControl1.Controls.Add(this.btnExportFile);
            this.layoutControl1.Controls.Add(this.btnDelete);
            this.layoutControl1.Controls.Add(this.btnAddNew);
            this.layoutControl1.Controls.Add(this.listBoxMachineList);
            this.layoutControl1.Controls.Add(this.lookUpModelName);
            this.layoutControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutControl1.Location = new System.Drawing.Point(0, 0);
            this.layoutControl1.Name = "layoutControl1";
            this.layoutControl1.Root = this.Root;
            this.layoutControl1.Size = new System.Drawing.Size(313, 554);
            this.layoutControl1.TabIndex = 0;
            this.layoutControl1.Text = "layoutControl1";
            // 
            // listBoxLayoutFile
            // 
            this.listBoxLayoutFile.Location = new System.Drawing.Point(12, 268);
            this.listBoxLayoutFile.Name = "listBoxLayoutFile";
            this.listBoxLayoutFile.Size = new System.Drawing.Size(289, 196);
            this.listBoxLayoutFile.StyleController = this.layoutControl1;
            this.listBoxLayoutFile.TabIndex = 11;
            this.listBoxLayoutFile.UseDisabledStatePainter = false;
            // 
            // btnExportFile
            // 
            this.btnExportFile.Location = new System.Drawing.Point(158, 494);
            this.btnExportFile.Name = "btnExportFile";
            this.btnExportFile.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Light;
            this.btnExportFile.Size = new System.Drawing.Size(143, 22);
            this.btnExportFile.StyleController = this.layoutControl1;
            this.btnExportFile.TabIndex = 10;
            this.btnExportFile.Text = "Download file";
            this.btnExportFile.Visible = false;
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(158, 520);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Light;
            this.btnDelete.Size = new System.Drawing.Size(143, 22);
            this.btnDelete.StyleController = this.layoutControl1;
            this.btnDelete.TabIndex = 9;
            this.btnDelete.Text = "Delete file";
            this.btnDelete.Visible = false;
            // 
            // btnAddNew
            // 
            this.btnAddNew.Location = new System.Drawing.Point(158, 468);
            this.btnAddNew.Name = "btnAddNew";
            this.btnAddNew.Size = new System.Drawing.Size(143, 22);
            this.btnAddNew.StyleController = this.layoutControl1;
            this.btnAddNew.TabIndex = 7;
            this.btnAddNew.Text = "Add new file";
            // 
            // listBoxMachineList
            // 
            this.listBoxMachineList.Location = new System.Drawing.Point(12, 52);
            this.listBoxMachineList.Name = "listBoxMachineList";
            this.listBoxMachineList.Size = new System.Drawing.Size(289, 196);
            this.listBoxMachineList.StyleController = this.layoutControl1;
            this.listBoxMachineList.TabIndex = 6;
            this.listBoxMachineList.UseDisabledStatePainter = false;
            // 
            // lookUpModelName
            // 
            this.lookUpModelName.Location = new System.Drawing.Point(82, 12);
            this.lookUpModelName.Name = "lookUpModelName";
            this.lookUpModelName.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.lookUpModelName.Size = new System.Drawing.Size(219, 20);
            this.lookUpModelName.StyleController = this.layoutControl1;
            this.lookUpModelName.TabIndex = 4;
            // 
            // Root
            // 
            this.Root.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.Root.GroupBordersVisible = false;
            this.Root.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItem1,
            this.emptySpaceItem1,
            this.layoutControlItem3,
            this.layoutControlItem4,
            this.layoutControlItem6,
            this.layoutControlItem5,
            this.layoutControlItem2});
            this.Root.Name = "Root";
            this.Root.Size = new System.Drawing.Size(313, 554);
            this.Root.TextVisible = false;
            // 
            // layoutControlItem1
            // 
            this.layoutControlItem1.Control = this.lookUpModelName;
            this.layoutControlItem1.Location = new System.Drawing.Point(0, 0);
            this.layoutControlItem1.Name = "layoutControlItem1";
            this.layoutControlItem1.Size = new System.Drawing.Size(293, 24);
            this.layoutControlItem1.Text = "Model Name";
            // 
            // emptySpaceItem1
            // 
            this.emptySpaceItem1.Location = new System.Drawing.Point(0, 456);
            this.emptySpaceItem1.Name = "emptySpaceItem1";
            this.emptySpaceItem1.Size = new System.Drawing.Size(146, 78);
            // 
            // layoutControlItem3
            // 
            this.layoutControlItem3.ContentVertAlignment = DevExpress.Utils.VertAlignment.Top;
            this.layoutControlItem3.Control = this.listBoxMachineList;
            this.layoutControlItem3.Location = new System.Drawing.Point(0, 24);
            this.layoutControlItem3.Name = "layoutControlItem3";
            this.layoutControlItem3.Size = new System.Drawing.Size(293, 216);
            this.layoutControlItem3.Text = "Machine List";
            this.layoutControlItem3.TextLocation = DevExpress.Utils.Locations.Top;
            // 
            // layoutControlItem4
            // 
            this.layoutControlItem4.Control = this.btnAddNew;
            this.layoutControlItem4.Location = new System.Drawing.Point(146, 456);
            this.layoutControlItem4.Name = "layoutControlItem4";
            this.layoutControlItem4.Size = new System.Drawing.Size(147, 26);
            this.layoutControlItem4.TextVisible = false;
            // 
            // layoutControlItem6
            // 
            this.layoutControlItem6.Control = this.btnDelete;
            this.layoutControlItem6.Location = new System.Drawing.Point(146, 508);
            this.layoutControlItem6.Name = "layoutControlItem6";
            this.layoutControlItem6.Size = new System.Drawing.Size(147, 26);
            this.layoutControlItem6.TextVisible = false;
            // 
            // layoutControlItem5
            // 
            this.layoutControlItem5.Control = this.btnExportFile;
            this.layoutControlItem5.Location = new System.Drawing.Point(146, 482);
            this.layoutControlItem5.Name = "layoutControlItem5";
            this.layoutControlItem5.Size = new System.Drawing.Size(147, 26);
            this.layoutControlItem5.TextVisible = false;
            // 
            // layoutControlItem2
            // 
            this.layoutControlItem2.Control = this.listBoxLayoutFile;
            this.layoutControlItem2.CustomizationFormText = " ";
            this.layoutControlItem2.Location = new System.Drawing.Point(0, 240);
            this.layoutControlItem2.Name = "layoutControlItem2";
            this.layoutControlItem2.Size = new System.Drawing.Size(293, 216);
            this.layoutControlItem2.Text = "Layout File";
            this.layoutControlItem2.TextLocation = DevExpress.Utils.Locations.Top;
            // 
            // pictureViewer
            // 
            this.pictureViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureViewer.Location = new System.Drawing.Point(0, 32);
            this.pictureViewer.Name = "pictureViewer";
            this.pictureViewer.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.pictureViewer.Size = new System.Drawing.Size(693, 522);
            this.pictureViewer.TabIndex = 3;
            this.pictureViewer.Visible = false;
            // 
            // pdfViewer
            // 
            this.pdfViewer.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.pdfViewer.Appearance.Options.UseBackColor = true;
            this.pdfViewer.CursorMode = DevExpress.XtraPdfViewer.PdfCursorMode.HandTool;
            this.pdfViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pdfViewer.Location = new System.Drawing.Point(0, 32);
            this.pdfViewer.Name = "pdfViewer";
            this.pdfViewer.NavigationPaneInitialSelectedPage = DevExpress.XtraPdfViewer.PdfNavigationPanePage.Thumbnails;
            this.pdfViewer.NavigationPaneInitialVisibility = DevExpress.XtraPdfViewer.PdfNavigationPaneVisibility.Visible;
            this.pdfViewer.NavigationPanePageVisibility = DevExpress.XtraPdfViewer.PdfNavigationPanePageVisibility.Thumbnails;
            this.pdfViewer.NavigationPaneWidth = 230;
            this.pdfViewer.Size = new System.Drawing.Size(693, 522);
            this.pdfViewer.TabIndex = 2;
            this.pdfViewer.ZoomMode = DevExpress.XtraPdfViewer.PdfZoomMode.PageLevel;
            // 
            // lblFileName
            // 
            this.lblFileName.Appearance.Options.UseTextOptions = true;
            this.lblFileName.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.lblFileName.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            this.lblFileName.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.lblFileName.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblFileName.Location = new System.Drawing.Point(0, 0);
            this.lblFileName.Name = "lblFileName";
            this.lblFileName.Size = new System.Drawing.Size(693, 32);
            this.lblFileName.TabIndex = 1;
            // 
            // ucCIDocument
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainerControl1);
            this.Name = "ucCIDocument";
            this.Size = new System.Drawing.Size(1016, 554);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel1)).EndInit();
            this.splitContainerControl1.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1.Panel2)).EndInit();
            this.splitContainerControl1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1)).EndInit();
            this.splitContainerControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).EndInit();
            this.layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.listBoxLayoutFile)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.listBoxMachineList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lookUpModelName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem6)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem5)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureViewer.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.SplitContainerControl splitContainerControl1;
        private DevExpress.XtraLayout.LayoutControl layoutControl1;
        private DevExpress.XtraEditors.SimpleButton btnExportFile;
        private DevExpress.XtraEditors.SimpleButton btnDelete;
        private DevExpress.XtraEditors.SimpleButton btnAddNew;
        private DevExpress.XtraEditors.ListBoxControl listBoxMachineList;
        private DevExpress.XtraEditors.LookUpEdit lookUpModelName;
        private DevExpress.XtraLayout.LayoutControlGroup Root;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem1;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem1;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem3;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem4;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem6;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem5;
        private DevExpress.XtraEditors.PictureEdit pictureViewer;
        private DevExpress.XtraPdfViewer.PdfViewer pdfViewer;
        private DevExpress.XtraEditors.LabelControl lblFileName;
        private DevExpress.XtraEditors.ListBoxControl listBoxLayoutFile;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem2;
        private DevExpress.Utils.ToolTipController toolTipController1;
    }
}
