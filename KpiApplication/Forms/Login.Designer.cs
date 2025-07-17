namespace KpiApplication
{
    partial class Login
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.btnExit = new DevExpress.XtraEditors.SimpleButton();
            this.btnLogin = new DevExpress.XtraEditors.SimpleButton();
            this.label12 = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.btnGuestLogin = new DevExpress.XtraEditors.SimpleButton();
            this.lblChangePassword = new DevExpress.XtraEditors.LabelControl();
            this.lblShowPassword = new DevExpress.XtraEditors.LabelControl();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.White;
            this.label2.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(425, 272);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(88, 21);
            this.label2.TabIndex = 30;
            this.label2.Text = "Password:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.White;
            this.label1.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(426, 202);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 21);
            this.label1.TabIndex = 29;
            this.label1.Text = "Username:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnExit
            // 
            this.btnExit.Appearance.Font = new System.Drawing.Font("Times New Roman", 9.75F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Italic | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExit.Appearance.ForeColor = System.Drawing.Color.Black;
            this.btnExit.Appearance.Options.UseFont = true;
            this.btnExit.Appearance.Options.UseForeColor = true;
            this.btnExit.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnExit.Location = new System.Drawing.Point(727, 12);
            this.btnExit.LookAndFeel.SkinMaskColor = System.Drawing.Color.DarkOrange;
            this.btnExit.LookAndFeel.SkinName = "Seven Classic";
            this.btnExit.LookAndFeel.UseDefaultLookAndFeel = false;
            this.btnExit.Name = "btnExit";
            this.btnExit.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Light;
            this.btnExit.Size = new System.Drawing.Size(60, 27);
            this.btnExit.TabIndex = 24;
            this.btnExit.TabStop = false;
            this.btnExit.Text = "Thoát";
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // btnLogin
            // 
            this.btnLogin.Appearance.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnLogin.Appearance.ForeColor = System.Drawing.Color.Black;
            this.btnLogin.Appearance.Options.UseFont = true;
            this.btnLogin.Appearance.Options.UseForeColor = true;
            this.btnLogin.Location = new System.Drawing.Point(581, 313);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(129, 29);
            this.btnLogin.TabIndex = 25;
            this.btnLogin.Text = "Login";
            this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);
            // 
            // label12
            // 
            this.label12.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label12.AutoSize = true;
            this.label12.BackColor = System.Drawing.Color.Transparent;
            this.label12.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.ForeColor = System.Drawing.SystemColors.Control;
            this.label12.Location = new System.Drawing.Point(594, 481);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(193, 15);
            this.label12.TabIndex = 27;
            this.label12.Text = "Developed by the ME  Department";
            // 
            // txtPassword
            // 
            this.txtPassword.BackColor = System.Drawing.Color.White;
            this.txtPassword.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtPassword.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtPassword.ForeColor = System.Drawing.Color.Black;
            this.txtPassword.Location = new System.Drawing.Point(521, 272);
            this.txtPassword.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(156, 22);
            this.txtPassword.TabIndex = 23;
            // 
            // txtUsername
            // 
            this.txtUsername.BackColor = System.Drawing.Color.White;
            this.txtUsername.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtUsername.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtUsername.ForeColor = System.Drawing.Color.Black;
            this.txtUsername.Location = new System.Drawing.Point(522, 202);
            this.txtUsername.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(173, 22);
            this.txtUsername.TabIndex = 22;
            // 
            // btnGuestLogin
            // 
            this.btnGuestLogin.Appearance.Font = new System.Drawing.Font("Times New Roman", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Italic | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnGuestLogin.Appearance.ForeColor = System.Drawing.Color.Black;
            this.btnGuestLogin.Appearance.Options.UseFont = true;
            this.btnGuestLogin.Appearance.Options.UseForeColor = true;
            this.btnGuestLogin.Location = new System.Drawing.Point(435, 314);
            this.btnGuestLogin.Name = "btnGuestLogin";
            this.btnGuestLogin.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Light;
            this.btnGuestLogin.Size = new System.Drawing.Size(129, 29);
            this.btnGuestLogin.TabIndex = 25;
            this.btnGuestLogin.Text = "Login as guest";
            this.btnGuestLogin.Click += new System.EventHandler(this.btnGuestLogin_Click);
            // 
            // lblChangePassword
            // 
            this.lblChangePassword.Location = new System.Drawing.Point(521, 368);
            this.lblChangePassword.Name = "lblChangePassword";
            this.lblChangePassword.Size = new System.Drawing.Size(86, 13);
            this.lblChangePassword.TabIndex = 31;
            this.lblChangePassword.Text = "Change password";
            this.lblChangePassword.Click += new System.EventHandler(this.lblChangePassword_Click);
            // 
            // lblShowPassword
            // 
            this.lblShowPassword.Location = new System.Drawing.Point(684, 280);
            this.lblShowPassword.Name = "lblShowPassword";
            this.lblShowPassword.Size = new System.Drawing.Size(26, 13);
            this.lblShowPassword.TabIndex = 32;
            this.lblShowPassword.Text = "Show";
            this.lblShowPassword.MouseDown += new System.Windows.Forms.MouseEventHandler(this.lblShowPassword_MouseDown);
            this.lblShowPassword.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lblShowPassword_MouseUp);
            // 
            // Login
            // 
            this.AcceptButton = this.btnLogin;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayoutStore = System.Windows.Forms.ImageLayout.Stretch;
            this.BackgroundImageStore = global::KpiApplication.Properties.Resources.Background;
            this.CancelButton = this.btnExit;
            this.ClientSize = new System.Drawing.Size(796, 518);
            this.Controls.Add(this.lblShowPassword);
            this.Controls.Add(this.lblChangePassword);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnGuestLogin);
            this.Controls.Add(this.btnLogin);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.txtUsername);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.LookAndFeel.SkinMaskColor = System.Drawing.Color.Blue;
            this.LookAndFeel.SkinName = "Seven Classic";
            this.LookAndFeel.UseDefaultLookAndFeel = false;
            this.Name = "Login";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Login";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmLogin_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private DevExpress.XtraEditors.SimpleButton btnExit;
        private DevExpress.XtraEditors.SimpleButton btnLogin;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.TextBox txtUsername;
        private DevExpress.XtraEditors.SimpleButton btnGuestLogin;
        private DevExpress.XtraEditors.LabelControl lblChangePassword;
        private DevExpress.XtraEditors.LabelControl lblShowPassword;
    }
}