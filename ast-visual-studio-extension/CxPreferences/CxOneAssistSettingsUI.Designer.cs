using System;
using System.Drawing;
using System.Windows.Forms;

namespace ast_visual_studio_extension.CxPreferences
{
    partial class CxOneAssistSettingsUI
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.ascaGroupBox = new System.Windows.Forms.GroupBox();
            this.ascaCheckBox = new System.Windows.Forms.CheckBox();
            this.ossGroupBox = new System.Windows.Forms.GroupBox();
            this.ossCheckBox = new System.Windows.Forms.CheckBox();
            this.secretsGroupBox = new System.Windows.Forms.GroupBox();
            this.secretsCheckBox = new System.Windows.Forms.CheckBox();
            this.containersGroupBox = new System.Windows.Forms.GroupBox();
            this.containersCheckBox = new System.Windows.Forms.CheckBox();
            this.iacGroupBox = new System.Windows.Forms.GroupBox();
            this.iacCheckBox = new System.Windows.Forms.CheckBox();
            this.containersToolGroupBox = new System.Windows.Forms.GroupBox();
            this.lblContainersTool = new System.Windows.Forms.Label();
            this.cmbContainersTool = new System.Windows.Forms.ComboBox();
            this.mcpGroupBox = new System.Windows.Forms.GroupBox();
            this.lblMcpStatus = new System.Windows.Forms.Label();
            this.lblMcpDescription = new System.Windows.Forms.Label();
            this.lnkInstallMcp = new System.Windows.Forms.LinkLabel();
            this.lnkEditMcp = new System.Windows.Forms.LinkLabel();
            this.spacer1 = new System.Windows.Forms.Panel();
            this.spacer2 = new System.Windows.Forms.Panel();
            this.spacer3 = new System.Windows.Forms.Panel();
            this.spacer4 = new System.Windows.Forms.Panel();
            this.spacer5 = new System.Windows.Forms.Panel();
            this.spacer6 = new System.Windows.Forms.Panel();
            this.ascaGroupBox.SuspendLayout();
            this.ossGroupBox.SuspendLayout();
            this.secretsGroupBox.SuspendLayout();
            this.containersGroupBox.SuspendLayout();
            this.iacGroupBox.SuspendLayout();
            this.containersToolGroupBox.SuspendLayout();
            this.mcpGroupBox.SuspendLayout();
            this.SuspendLayout();

            // ascaGroupBox
            this.ascaGroupBox.Controls.Add(this.ascaCheckBox);
            this.ascaGroupBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.ascaGroupBox.Location = new System.Drawing.Point(0, 0);
            this.ascaGroupBox.Name = "ascaGroupBox";
            this.ascaGroupBox.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.ascaGroupBox.Size = new System.Drawing.Size(100, 48);
            this.ascaGroupBox.TabIndex = 0;
            this.ascaGroupBox.TabStop = false;
            this.ascaGroupBox.Text = "Checkmarx AI Secure Coding Assistant (ASCA): Activate ASCA";

            // ascaCheckBox
            this.ascaCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.ascaCheckBox.AutoSize = false;
            this.ascaCheckBox.Location = new System.Drawing.Point(8, 20);
            this.ascaCheckBox.Name = "ascaCheckBox";
            this.ascaCheckBox.Size = new System.Drawing.Size(200, 22);
            this.ascaCheckBox.TabIndex = 0;
            this.ascaCheckBox.Text = "Scan your file as you code";
            this.ascaCheckBox.UseVisualStyleBackColor = true;
            this.ascaCheckBox.CheckedChanged += new System.EventHandler(this.AscaCheckBox_CheckedChanged);

            // spacer1
            this.spacer1.Dock = System.Windows.Forms.DockStyle.Top;
            this.spacer1.Name = "spacer1";
            this.spacer1.Size = new System.Drawing.Size(100, 8);
            this.spacer1.TabIndex = 10;

            // ossGroupBox
            this.ossGroupBox.Controls.Add(this.ossCheckBox);
            this.ossGroupBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.ossGroupBox.Location = new System.Drawing.Point(0, 0);
            this.ossGroupBox.Name = "ossGroupBox";
            this.ossGroupBox.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.ossGroupBox.Size = new System.Drawing.Size(100, 48);
            this.ossGroupBox.TabIndex = 1;
            this.ossGroupBox.TabStop = false;
            this.ossGroupBox.Text = "Checkmarx Open Source Realtime Scanner (OSS-Realtime): Activate OSS-Realtime";

            // ossCheckBox
            this.ossCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.ossCheckBox.AutoSize = false;
            this.ossCheckBox.Location = new System.Drawing.Point(8, 20);
            this.ossCheckBox.Name = "ossCheckBox";
            this.ossCheckBox.Size = new System.Drawing.Size(200, 22);
            this.ossCheckBox.TabIndex = 0;
            this.ossCheckBox.Text = "Scans your manifest files as you code";
            this.ossCheckBox.UseVisualStyleBackColor = true;
            this.ossCheckBox.CheckedChanged += new System.EventHandler(this.OssCheckBox_CheckedChanged);

            // spacer2
            this.spacer2.Dock = System.Windows.Forms.DockStyle.Top;
            this.spacer2.Name = "spacer2";
            this.spacer2.Size = new System.Drawing.Size(100, 8);
            this.spacer2.TabIndex = 11;

            // secretsGroupBox
            this.secretsGroupBox.Controls.Add(this.secretsCheckBox);
            this.secretsGroupBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.secretsGroupBox.Location = new System.Drawing.Point(0, 0);
            this.secretsGroupBox.Name = "secretsGroupBox";
            this.secretsGroupBox.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.secretsGroupBox.Size = new System.Drawing.Size(100, 48);
            this.secretsGroupBox.TabIndex = 2;
            this.secretsGroupBox.TabStop = false;
            this.secretsGroupBox.Text = "Checkmarx Secret Detection Realtime Scanner: Activate Secret Detection Realtime";

            // secretsCheckBox
            this.secretsCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.secretsCheckBox.AutoSize = false;
            this.secretsCheckBox.Location = new System.Drawing.Point(8, 20);
            this.secretsCheckBox.Name = "secretsCheckBox";
            this.secretsCheckBox.Size = new System.Drawing.Size(200, 22);
            this.secretsCheckBox.TabIndex = 0;
            this.secretsCheckBox.Text = "Scans your files for potential secrets and credentials as you code";
            this.secretsCheckBox.UseVisualStyleBackColor = true;
            this.secretsCheckBox.CheckedChanged += new System.EventHandler(this.SecretsCheckBox_CheckedChanged);

            // spacer3
            this.spacer3.Dock = System.Windows.Forms.DockStyle.Top;
            this.spacer3.Name = "spacer3";
            this.spacer3.Size = new System.Drawing.Size(100, 8);
            this.spacer3.TabIndex = 12;

            // containersGroupBox
            this.containersGroupBox.Controls.Add(this.containersCheckBox);
            this.containersGroupBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.containersGroupBox.Location = new System.Drawing.Point(0, 0);
            this.containersGroupBox.Name = "containersGroupBox";
            this.containersGroupBox.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.containersGroupBox.Size = new System.Drawing.Size(100, 48);
            this.containersGroupBox.TabIndex = 3;
            this.containersGroupBox.TabStop = false;
            this.containersGroupBox.Text = "Checkmarx Containers Realtime Scanner: Activate Containers Realtime";

            // containersCheckBox
            this.containersCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.containersCheckBox.AutoSize = false;
            this.containersCheckBox.Location = new System.Drawing.Point(8, 20);
            this.containersCheckBox.Name = "containersCheckBox";
            this.containersCheckBox.Size = new System.Drawing.Size(200, 22);
            this.containersCheckBox.TabIndex = 0;
            this.containersCheckBox.Text = "Scans your Docker files and container configurations as you code";
            this.containersCheckBox.UseVisualStyleBackColor = true;
            this.containersCheckBox.CheckedChanged += new System.EventHandler(this.ContainersCheckBox_CheckedChanged);

            // spacer4
            this.spacer4.Dock = System.Windows.Forms.DockStyle.Top;
            this.spacer4.Name = "spacer4";
            this.spacer4.Size = new System.Drawing.Size(100, 8);
            this.spacer4.TabIndex = 13;

            // iacGroupBox
            this.iacGroupBox.Controls.Add(this.iacCheckBox);
            this.iacGroupBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.iacGroupBox.Location = new System.Drawing.Point(0, 0);
            this.iacGroupBox.Name = "iacGroupBox";
            this.iacGroupBox.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.iacGroupBox.Size = new System.Drawing.Size(100, 48);
            this.iacGroupBox.TabIndex = 4;
            this.iacGroupBox.TabStop = false;
            this.iacGroupBox.Text = "Checkmarx IAC Realtime Scanner: Activate IAC Realtime";

            // iacCheckBox
            this.iacCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.iacCheckBox.AutoSize = false;
            this.iacCheckBox.Location = new System.Drawing.Point(8, 20);
            this.iacCheckBox.Name = "iacCheckBox";
            this.iacCheckBox.Size = new System.Drawing.Size(200, 22);
            this.iacCheckBox.TabIndex = 0;
            this.iacCheckBox.Text = "Scans your Infrastructure as Code files as you code";
            this.iacCheckBox.UseVisualStyleBackColor = true;
            this.iacCheckBox.CheckedChanged += new System.EventHandler(this.IacCheckBox_CheckedChanged);

            // spacer5
            this.spacer5.Dock = System.Windows.Forms.DockStyle.Top;
            this.spacer5.Name = "spacer5";
            this.spacer5.Size = new System.Drawing.Size(100, 8);
            this.spacer5.TabIndex = 14;

            // containersToolGroupBox
            this.containersToolGroupBox.Controls.Add(this.cmbContainersTool);
            this.containersToolGroupBox.Controls.Add(this.lblContainersTool);
            this.containersToolGroupBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.containersToolGroupBox.Location = new System.Drawing.Point(0, 0);
            this.containersToolGroupBox.Name = "containersToolGroupBox";
            this.containersToolGroupBox.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.containersToolGroupBox.Size = new System.Drawing.Size(100, 76);
            this.containersToolGroupBox.TabIndex = 5;
            this.containersToolGroupBox.TabStop = false;
            this.containersToolGroupBox.Text = "Checkmarx IAC Realtime Scanner: Containers Management Tool";

            // lblContainersTool
            this.lblContainersTool.AutoSize = true;
            this.lblContainersTool.Location = new System.Drawing.Point(8, 20);
            this.lblContainersTool.Name = "lblContainersTool";
            this.lblContainersTool.TabIndex = 0;
            this.lblContainersTool.Text = "Select the Containers Management Tool to use for IaC scanning.";

            // cmbContainersTool
            this.cmbContainersTool.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbContainersTool.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbContainersTool.FormattingEnabled = true;
            this.cmbContainersTool.Items.AddRange(new object[] { "docker", "podman" });
            this.cmbContainersTool.Location = new System.Drawing.Point(8, 44);
            this.cmbContainersTool.Name = "cmbContainersTool";
            this.cmbContainersTool.Size = new System.Drawing.Size(20, 24);
            this.cmbContainersTool.TabIndex = 1;
            this.cmbContainersTool.SelectedIndexChanged += new System.EventHandler(this.CmbContainersTool_SelectedIndexChanged);
            this.spacer6.Dock = System.Windows.Forms.DockStyle.Top;
            this.spacer6.Height = 8;

            // --- MCP Group (FIXED SECTION) ---
            this.mcpGroupBox.Text = "Checkmarx: MCP";
            this.mcpGroupBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.mcpGroupBox.AutoSize = true;
            this.mcpGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.mcpGroupBox.Padding = new Padding(8, 4, 8, 8);

            TableLayoutPanel mcpLayout = new TableLayoutPanel();
            mcpLayout.ColumnCount = 1;
            mcpLayout.RowCount = 4;
            mcpLayout.Dock = DockStyle.Top;
            mcpLayout.AutoSize = true;
            // CRITICAL: Force column to 100% width
            mcpLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            this.lblMcpDescription.AutoSize = true;
            this.lblMcpDescription.Text = "The Model Context Protocol (MCP) provides advanced contextual analysis for secure coding.";
            this.lblMcpDescription.Dock = DockStyle.Fill;
            this.lblMcpDescription.Margin = new Padding(0, 0, 5, 5);

            this.lnkInstallMcp.AutoSize = true;
            this.lnkInstallMcp.Text = "Install MCP";
            this.lnkInstallMcp.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LnkInstallMcp_LinkClicked);

            this.lnkEditMcp.AutoSize = true;
            this.lnkEditMcp.Text = "Edit in mcp.json";
            this.lnkEditMcp.LinkClicked += new LinkLabelLinkClickedEventHandler(this.LnkEditMcp_LinkClicked);

            this.lblMcpStatus.AutoSize = true;
            this.lblMcpStatus.Text = "";

            mcpLayout.Controls.Add(this.lblMcpDescription, 0, 0);
            mcpLayout.Controls.Add(this.lnkInstallMcp, 0, 1);
            mcpLayout.Controls.Add(this.lnkEditMcp, 0, 2);
            mcpLayout.Controls.Add(this.lblMcpStatus, 0, 3);
            this.mcpGroupBox.Controls.Add(mcpLayout);

            // Responsive Wrapping Logic: Updates MaximumSize when the container resizes
            this.mcpGroupBox.Layout += (s, e) => {
                int paddingWidth = 25;
                int targetWidth = mcpGroupBox.DisplayRectangle.Width - paddingWidth;
                if (targetWidth > 0 && lblMcpDescription.MaximumSize.Width != targetWidth)
                {
                    lblMcpDescription.MaximumSize = new Size(targetWidth, 0);
                }
            };

            // --- Main Control Setup ---
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.mcpGroupBox);
            this.Controls.Add(this.spacer6);
            this.Controls.Add(this.containersToolGroupBox);
            this.Controls.Add(this.spacer5);
            this.Controls.Add(this.iacGroupBox);
            this.Controls.Add(this.spacer4);
            this.Controls.Add(this.containersGroupBox);
            this.Controls.Add(this.spacer3);
            this.Controls.Add(this.secretsGroupBox);
            this.Controls.Add(this.spacer2);
            this.Controls.Add(this.ossGroupBox);
            this.Controls.Add(this.spacer1);
            this.Controls.Add(this.ascaGroupBox);
            this.Name = "CxOneAssistSettingsUI";
            this.Size = new System.Drawing.Size(400, 620);

            this.ascaGroupBox.ResumeLayout(false);
            this.ossGroupBox.ResumeLayout(false);
            this.secretsGroupBox.ResumeLayout(false);
            this.containersGroupBox.ResumeLayout(false);
            this.iacGroupBox.ResumeLayout(false);
            this.containersToolGroupBox.ResumeLayout(false);
            this.mcpGroupBox.ResumeLayout(false);
            this.mcpGroupBox.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.GroupBox ascaGroupBox;
        private System.Windows.Forms.CheckBox ascaCheckBox;
        private System.Windows.Forms.GroupBox ossGroupBox;
        private System.Windows.Forms.CheckBox ossCheckBox;
        private System.Windows.Forms.GroupBox secretsGroupBox;
        private System.Windows.Forms.CheckBox secretsCheckBox;
        private System.Windows.Forms.GroupBox containersGroupBox;
        private System.Windows.Forms.CheckBox containersCheckBox;
        private System.Windows.Forms.GroupBox iacGroupBox;
        private System.Windows.Forms.CheckBox iacCheckBox;
        private System.Windows.Forms.GroupBox containersToolGroupBox;
        private System.Windows.Forms.Label lblContainersTool;
        private System.Windows.Forms.ComboBox cmbContainersTool;
        private System.Windows.Forms.GroupBox mcpGroupBox;
        private System.Windows.Forms.Label lblMcpStatus;
        private System.Windows.Forms.Label lblMcpDescription;
        private System.Windows.Forms.LinkLabel lnkInstallMcp;
        private System.Windows.Forms.LinkLabel lnkEditMcp;
        private System.Windows.Forms.Panel spacer1;
        private System.Windows.Forms.Panel spacer2;
        private System.Windows.Forms.Panel spacer3;
        private System.Windows.Forms.Panel spacer4;
        private System.Windows.Forms.Panel spacer5;
        private System.Windows.Forms.Panel spacer6;
    }
}