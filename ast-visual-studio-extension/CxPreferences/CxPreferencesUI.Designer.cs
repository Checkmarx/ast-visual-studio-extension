

using ast_visual_studio_extension.CxCLI;

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
            this.ascaGroupBox = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.ascaCheckBox = new System.Windows.Forms.CheckBox();
            this.ascaLabel = new System.Windows.Forms.Label();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.ascaGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.tbApiKey);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Location = new System.Drawing.Point(9, 50);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox2.Size = new System.Drawing.Size(1138, 71);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Credentials";
            // 
            // tbApiKey
            // 
            this.tbApiKey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbApiKey.Location = new System.Drawing.Point(204, 26);
            this.tbApiKey.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tbApiKey.Name = "tbApiKey";
            this.tbApiKey.Size = new System.Drawing.Size(903, 22);
            this.tbApiKey.TabIndex = 1;
            this.tbApiKey.UseSystemPasswordChar = true;
            this.tbApiKey.TextChanged += new System.EventHandler(this.OnApiKeyChange);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(36, 26);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(53, 16);
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
            this.groupBox3.Location = new System.Drawing.Point(9, 132);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox3.Size = new System.Drawing.Size(1138, 105);
            this.groupBox3.TabIndex = 10;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Additional settings";
            // 
            // AdditionalParametersHelPage
            // 
            this.AdditionalParametersHelPage.AutoSize = true;
            this.AdditionalParametersHelPage.Location = new System.Drawing.Point(204, 66);
            this.AdditionalParametersHelPage.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.AdditionalParametersHelPage.Name = "AdditionalParametersHelPage";
            this.AdditionalParametersHelPage.Size = new System.Drawing.Size(287, 16);
            this.AdditionalParametersHelPage.TabIndex = 2;
            this.AdditionalParametersHelPage.TabStop = true;
            this.AdditionalParametersHelPage.Text = "CLI command that supports a set of global flags";
            this.AdditionalParametersHelPage.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.AdditionalParametersHelPage_LinkClicked);
            // 
            // tbAdditionalParameters
            // 
            this.tbAdditionalParameters.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbAdditionalParameters.Location = new System.Drawing.Point(204, 20);
            this.tbAdditionalParameters.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tbAdditionalParameters.Name = "tbAdditionalParameters";
            this.tbAdditionalParameters.Size = new System.Drawing.Size(903, 22);
            this.tbAdditionalParameters.TabIndex = 1;
            this.tbAdditionalParameters.TextChanged += new System.EventHandler(this.OnAdditionalParametersChange);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(36, 26);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(139, 16);
            this.label5.TabIndex = 0;
            this.label5.Text = "Additional parameters";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(11, 377);
            this.button1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(165, 27);
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
            this.lblValidationResult.Location = new System.Drawing.Point(213, 382);
            this.lblValidationResult.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.lblValidationResult.Name = "lblValidationResult";
            this.lblValidationResult.ReadOnly = true;
            this.lblValidationResult.Size = new System.Drawing.Size(930, 15);
            this.lblValidationResult.TabIndex = 11;
            // 
            // helpPage
            // 
            this.helpPage.AutoSize = true;
            this.helpPage.Location = new System.Drawing.Point(13, 21);
            this.helpPage.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.helpPage.Name = "helpPage";
            this.helpPage.Size = new System.Drawing.Size(308, 16);
            this.helpPage.TabIndex = 12;
            this.helpPage.TabStop = true;
            this.helpPage.Text = "Checkmarx One Visual Studio Extension help page";
            this.helpPage.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.HelpPage_LinkClicked);
            // 
            // ascaGroupBox
            // 
            this.ascaGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ascaGroupBox.Controls.Add(this.label1);
            this.ascaGroupBox.Controls.Add(this.ascaCheckBox);
            this.ascaGroupBox.Controls.Add(this.ascaLabel);
            this.ascaGroupBox.Location = new System.Drawing.Point(9, 244);
            this.ascaGroupBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.ascaGroupBox.Name = "ascaGroupBox";
            this.ascaGroupBox.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ascaGroupBox.Size = new System.Drawing.Size(1138, 90);
            this.ascaGroupBox.TabIndex = 13;
            this.ascaGroupBox.TabStop = false;
            this.ascaGroupBox.Text = "Checkmarx AI Secure Coding Assistant (ASCA):";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.ForeColor = System.Drawing.Color.Green;
            this.label1.Location = new System.Drawing.Point(223, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(257, 16);
            this.label1.TabIndex = 12;
            this.label1.Text = "AI Secure Coding Assistant Engine started";
            // 
            // ascaCheckBox
            // 
            this.ascaCheckBox.AutoSize = true;
            this.ascaCheckBox.Location = new System.Drawing.Point(16, 20);
            this.ascaCheckBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ascaCheckBox.Name = "ascaCheckBox";
            this.ascaCheckBox.Size = new System.Drawing.Size(193, 20);
            this.ascaCheckBox.TabIndex = 0;
            this.ascaCheckBox.Text = "Scans your file as you code";
            this.ascaCheckBox.UseVisualStyleBackColor = true;
            this.ascaCheckBox.CheckedChanged += new System.EventHandler(this.AscaCheckBox_CheckedChanged);
            // 
            // ascaLabel
            // 
            this.ascaLabel.AutoSize = true;
            this.ascaLabel.BackColor = System.Drawing.Color.Transparent;
            this.ascaLabel.Location = new System.Drawing.Point(312, 0);
            this.ascaLabel.Name = "ascaLabel";
            this.ascaLabel.Size = new System.Drawing.Size(94, 16);
            this.ascaLabel.TabIndex = 1;
            this.ascaLabel.Text = "Activate ASCA";
            // 
            // CxPreferencesUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Controls.Add(this.helpPage);
            this.Controls.Add(this.lblValidationResult);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.ascaGroupBox);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "CxPreferencesUI";
            this.Size = new System.Drawing.Size(1147, 487);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ascaGroupBox.ResumeLayout(false);
            this.ascaGroupBox.PerformLayout();
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
        private System.Windows.Forms.GroupBox ascaGroupBox;
        private System.Windows.Forms.CheckBox ascaCheckBox;
        private System.Windows.Forms.Label ascaLabel;
        private System.Windows.Forms.Label label1;
    }
}
