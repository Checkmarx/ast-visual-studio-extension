

namespace ast_visual_studio_extension.CxPreferences
{
    partial class CxPreferencesUI
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
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tbApiKey = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.AdditionalParametersHelPage = new System.Windows.Forms.LinkLabel();
            this.tbAdditionalParameters = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.lblValidationResult = new System.Windows.Forms.TextBox();
            this.helpPage = new System.Windows.Forms.LinkLabel();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.tbApiKey);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Location = new System.Drawing.Point(11, 63);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox2.Size = new System.Drawing.Size(1279, 89);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Credentials";
            // 
            // tbApiKey
            // 
            this.tbApiKey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbApiKey.Location = new System.Drawing.Point(232, 26);
            this.tbApiKey.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tbApiKey.Name = "tbApiKey";
            this.tbApiKey.Size = new System.Drawing.Size(1036, 26);
            this.tbApiKey.TabIndex = 1;
            this.tbApiKey.UseSystemPasswordChar = true;
            this.tbApiKey.TextChanged += new System.EventHandler(this.OnApiKeyChange);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(39, 31);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(63, 20);
            this.label4.TabIndex = 0;
            this.label4.Text = "API key";
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.AdditionalParametersHelPage);
            this.groupBox3.Controls.Add(this.tbAdditionalParameters);
            this.groupBox3.Controls.Add(this.label5);
            this.groupBox3.Location = new System.Drawing.Point(11, 164);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox3.Size = new System.Drawing.Size(1279, 131);
            this.groupBox3.TabIndex = 10;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Additional settings";
            // 
            // AdditionalParametersHelPage
            // 
            this.AdditionalParametersHelPage.AutoSize = true;
            this.AdditionalParametersHelPage.Location = new System.Drawing.Point(228, 80);
            this.AdditionalParametersHelPage.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.AdditionalParametersHelPage.Name = "AdditionalParametersHelPage";
            this.AdditionalParametersHelPage.Size = new System.Drawing.Size(354, 20);
            this.AdditionalParametersHelPage.TabIndex = 2;
            this.AdditionalParametersHelPage.TabStop = true;
            this.AdditionalParametersHelPage.Text = "Checkmarx AST additional parameters help page";
            this.AdditionalParametersHelPage.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.AdditionalParametersHelPage_LinkClicked);
            // 
            // tbAdditionalParameters
            // 
            this.tbAdditionalParameters.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbAdditionalParameters.Location = new System.Drawing.Point(232, 31);
            this.tbAdditionalParameters.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tbAdditionalParameters.Name = "tbAdditionalParameters";
            this.tbAdditionalParameters.Size = new System.Drawing.Size(1036, 26);
            this.tbAdditionalParameters.TabIndex = 1;
            this.tbAdditionalParameters.TextChanged += new System.EventHandler(this.OnAdditionalParametersChange);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(39, 31);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(164, 20);
            this.label5.TabIndex = 0;
            this.label5.Text = "Additional parameters";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(11, 321);
            this.button1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(186, 34);
            this.button1.TabIndex = 8;
            this.button1.Text = "Validate connection";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.OnValidateConnection);
            // 
            // lblValidationResult
            // 
            this.lblValidationResult.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValidationResult.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lblValidationResult.Location = new System.Drawing.Point(240, 321);
            this.lblValidationResult.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.lblValidationResult.Multiline = true;
            this.lblValidationResult.Name = "lblValidationResult";
            this.lblValidationResult.ReadOnly = true;
            this.lblValidationResult.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.lblValidationResult.Size = new System.Drawing.Size(1046, 288);
            this.lblValidationResult.TabIndex = 11;
            // 
            // helpPage
            // 
            this.helpPage.AutoSize = true;
            this.helpPage.Location = new System.Drawing.Point(15, 26);
            this.helpPage.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.helpPage.Name = "helpPage";
            this.helpPage.Size = new System.Drawing.Size(368, 20);
            this.helpPage.TabIndex = 12;
            this.helpPage.TabStop = true;
            this.helpPage.Text = "Checkmarx AST Visual Studio Extension help page";
            this.helpPage.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.HelpPage_LinkClicked);
            // 
            // CxPreferencesUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Controls.Add(this.helpPage);
            this.Controls.Add(this.lblValidationResult);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.button1);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "CxPreferencesUI";
            this.Size = new System.Drawing.Size(1290, 609);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox tbApiKey;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox tbAdditionalParameters;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox lblValidationResult;
        private System.Windows.Forms.LinkLabel helpPage;
        private System.Windows.Forms.LinkLabel AdditionalParametersHelPage;
    }
}
