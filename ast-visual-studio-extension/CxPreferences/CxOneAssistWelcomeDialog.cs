using ast_visual_studio_extension.CxExtension.Resources;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

using System.Reflection;
using System.Windows.Forms;
using ast_visual_studio_extension.CxExtension.Utils;
using ast_visual_studio_extension.CxPreferences.Resources;

namespace ast_visual_studio_extension.CxPreferences
{
    internal class CxOneAssistWelcomeDialog : Form
    {
        private static readonly Size PreferredDialogSize = new Size(1400, 850);

        private Color _background;
        private Color _surface;
        private Color _cardSurface;
        private Color _border;
        private Color _text;
        private Color _secondaryText;
        private Color _accent;
        private Color _accentHover;
        private Color _buttonText;
        private Color _heroBase;
        private Color _heroStroke;
        private Color _codeFrame;
        private Color _issuePanel;
        private Color _danger;
        private Color _dangerText;

        private readonly CxOneAssistSettingsModule _settings;
        private readonly bool _mcpEnabled;
        private CheckBox _enableAllScannersCheckBox;

        public CxOneAssistWelcomeDialog(CxOneAssistSettingsModule settings, bool mcpEnabled)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _mcpEnabled = mcpEnabled;
            InitializeThemePalette();

            var assembly = typeof(CxOneAssistWelcomeDialog).Assembly;
            using (var stream = assembly.GetManifestResourceStream(CxConstants.ICON_CX_LOGO_ICO))
            {
                if (stream != null)
                {
                    this.Icon = new Icon(stream);
                }
            }
            Text = "Checkmarx";
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = PreferredDialogSize;
            MinimumSize = new Size(1400, 850);
            BackColor = _background;
            Font = new Font("Segoe UI", 2F, FontStyle.Regular, GraphicsUnit.Point);
            ForeColor = _text;

            var root = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _background,
                Padding = new Padding(24, 18, 24, 14)
            };

            var contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = _background,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var leftColumn = BuildLeftColumn();
            leftColumn.Dock = DockStyle.Fill;

            var rightColumn = BuildHeroPanel();
            rightColumn.Dock = DockStyle.Fill;

            contentLayout.Controls.Add(leftColumn, 0, 0);
            contentLayout.Controls.Add(rightColumn, 1, 0);

            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = _background
            };

            var footerDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = _border
            };

            var closeButton = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(48, 117, 238),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 36),

                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),

                TextAlign = ContentAlignment.MiddleCenter,   // ✅ FIX: center text
                ImageAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(0),                    // ✅ remove offset
                UseVisualStyleBackColor = false
            };

            closeButton.FlatAppearance.BorderSize = 1;
            closeButton.FlatAppearance.BorderColor = Color.FromArgb(40, 102, 212);
            closeButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(38, 97, 201);
            closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(65, 130, 244);

            void PositionCloseButton()
            {
                closeButton.Location = new Point(
                    footer.ClientSize.Width - closeButton.Width - 14,
                    (footer.ClientSize.Height - closeButton.Height) / 2   // ✅ vertical center
                );
            }
            footer.Resize += (_, __) => PositionCloseButton();

            footer.Controls.Add(footerDivider);
            footer.Controls.Add(closeButton);
            PositionCloseButton();

            AcceptButton = closeButton;
            CancelButton = closeButton;

            root.Controls.Add(contentLayout);
            root.Controls.Add(footer);

            Controls.Add(root);
        }

        private Panel BuildLeftColumn()
        {
            var leftPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = _background,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(24, 24, 24, 24),
                AutoSize = false
            };
            leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Title
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Subtitle
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Feature card
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Bullets

            var title = new Label
            {
                AutoSize = true,
                ForeColor = _text,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point),
                Text = "Welcome to Checkmarx",
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 8)
            };

            var subtitle = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                ForeColor = Color.FromArgb(45, 52, 70),
                Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point),
                Text = "Checkmarx offers immediate threat detection and assists you in preventing vulnerabilities before they arise.",
                MaximumSize = new Size(800, 0),
                MinimumSize = new Size(300, 0),
                TextAlign = ContentAlignment.TopLeft,
                Margin = new Padding(0, 0, 0, 12)
            };

            var featureCard = BuildFeatureCard();
            featureCard.Dock = DockStyle.Top;
            featureCard.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            featureCard.Margin = new Padding(0, 0, 0, 16);
            featureCard.MinimumSize = new Size(400, 0);

            // Responsive width update on resize
            leftPanel.Resize += (s, e) =>
            {
                int availWidth = leftPanel.Width - leftPanel.Padding.Horizontal - 10;
                subtitle.MaximumSize = new Size(availWidth, 0);
                featureCard.Width = availWidth;
            };

            var bulletsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent,
                AutoSize = true
            };
            string[] bullets = {
                "Run SAST, SCA, IaC, Containers and Secrets scans.",
                "Create a new Checkmarx branch from your local workspace.",
                "Preview or rescan before committing.",
                "Triage & fix issues directly in the editor."
            };
            foreach (var bullet in bullets)
            {
                var bulletLabel = new Label
                {
                    AutoSize = true,
                    ForeColor = _text,
                    Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                    Text = "• " + bullet,
                    Margin = new Padding(0, 0, 0, 2)
                };
                bulletsPanel.Controls.Add(bulletLabel);
            }

            leftPanel.Controls.Add(title, 0, 0);
            leftPanel.Controls.Add(subtitle, 0, 1);
            leftPanel.Controls.Add(featureCard, 0, 2);
            leftPanel.Controls.Add(bulletsPanel, 0, 3);

            return leftPanel;
        }

        private Label CreateBullet(string text, int y)
        {
            return new Label
            {
                AutoSize = false,
                ForeColor = _text,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                Text = "• " + text,
                Location = new Point(0, y),
                Size = new Size(530, 34)
            };
        }

        private Panel BuildHeroPanel()
        {
            var wrapper = new Panel
            {
                BackColor = _background,
                Padding = new Padding(12, 22, 8, 8)
            };

            Image image = LoadEmbeddedImage(CxConstants.ICON_WELCOME_SCANNER);

            if (image != null)
            {
                var heroImage = new PictureBox
                {
                    Dock = DockStyle.Top,                // ✅ TOP ALIGN
                    Image = image,
                    SizeMode = PictureBoxSizeMode.Zoom,  // keeps aspect ratio
                    BackColor = Color.Transparent,
                    Height = 350                        // ✅ control visible size
                };

                wrapper.Controls.Add(heroImage);
            }
            else
            {
                MessageBox.Show($"Image failed to load: {CxConstants.ICON_WELCOME_SCANNER}");
            }

            return wrapper;
        }
        private string BuildSvgHostHtml(string svgMarkup)
        {
            return "<!DOCTYPE html><html><head><meta http-equiv='X-UA-Compatible' content='IE=Edge' />"
                + "<style>html,body{margin:0;padding:0;overflow:hidden;background:#F0F4FA;width:100%;height:100%;}"
                + "svg{width:100%;height:100%;display:block;}</style></head><body>"
                + svgMarkup
                + "</body></html>";
        }

        private Image LoadEmbeddedImage(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            string resourcePath = $"ast_visual_studio_extension.CxPreferences.Resources.{fileName}";

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream != null)
                {
                    return Image.FromStream(stream);
                }
            }

            // Debug helper (optional)
            string[] allResources = assembly.GetManifestResourceNames();
            MessageBox.Show("Available resources:\n" + string.Join("\n", allResources));

            return null;
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void InitializeThemePalette()
        {
            _background = Color.White;
            _surface = Color.White;
            _cardSurface = Color.FromArgb(237, 237, 237); // VS default grey
            _border = Color.FromArgb(190, 199, 214);
            _text = Color.FromArgb(35, 43, 56);
            _secondaryText = Color.FromArgb(74, 86, 104);
            _accent = Color.FromArgb(43, 116, 255);
            _accentHover = Color.FromArgb(58, 130, 255);
            _buttonText = _accent;
            _heroBase = Color.FromArgb(110, 122, 218);
            _heroStroke = Color.FromArgb(30, 36, 48);
            _codeFrame = Color.FromArgb(245, 240, 244);
            _issuePanel = Color.FromArgb(241, 243, 248);
            _danger = Color.FromArgb(197, 79, 79);
            _dangerText = Color.FromArgb(255, 238, 238);
        }

        private void ApplyScannerState()
        {
            if (!_mcpEnabled)
                return;

            if (_enableAllScannersCheckBox.Checked)
            {
                _settings.EnableAllRealtimeScanners();
            }
            else
            {
                _settings.DisableAllRealtimeScanners();
            }
            _settings.SaveCurrentSettingsAsUserPreferences();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (DialogResult != DialogResult.OK && DialogResult != DialogResult.Cancel)
                return;

            if (!_mcpEnabled)
                return;

            // Apply final state (in case it wasn't already applied by CheckedChanged)
            ApplyScannerState();

            // Explicitly persist settings to registry
            _settings.PersistSettings();
        }

        private Panel BuildFeatureCard()
        {
            var featureCard = new TableLayoutPanel
            {
                BackColor = _cardSurface,
                Padding = new Padding(14),
                Margin = new Padding(0, 0, 0, 16),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 1,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            featureCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var checkPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8)
            };

            _enableAllScannersCheckBox = new CheckBox
            {
                AutoSize = true,
                Checked = _mcpEnabled,
                Enabled = _mcpEnabled,
                BackColor = Color.Transparent,
                ForeColor = _text,
                Margin = new Padding(0, 3, 6, 0) // slight vertical alignment tweak
            };

            var checkboxLabel = new Label
            {
                AutoSize = true,
                ForeColor = _text,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                Text = "Code Smarter with Checkmarx One Assist",
                Enabled = _mcpEnabled,
                Margin = new Padding(0, 2, 0, 0)
            };

            checkPanel.Controls.Add(_enableAllScannersCheckBox);
            checkPanel.Controls.Add(checkboxLabel);

            featureCard.Controls.Add(checkPanel, 0, 0);

            var details = new[]
            {
                "Get instant security feedback as you code.",
                "See suggested fixes for vulnerabilities across open source, config, and code.",
                "Fix faster with intelligent, context-aware remediation inside your IDE.",
                _mcpEnabled
                    ? "Checkmarx MCP Installed automatically - no need for manual integration"
                    : "Checkmarx MCP is not enabled for this tenant."
            };
            for (int i = 0; i < details.Length; i++)
            {
                var detailLabel = new Label
                {
                    AutoSize = true,
                    ForeColor = _secondaryText,
                    Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                    Text = "• " + details[i],
                    Margin = new Padding(0, i == 0 ? 8 : 2, 0, 2)
                };
                featureCard.RowCount++;
                featureCard.Controls.Add(detailLabel, 0, featureCard.RowCount - 1);
            }

            if (!_mcpEnabled)
            {
                Image errorImage = LoadEmbeddedImage(CxConstants.ICON_CX_AI_ERROR);

                if (errorImage != null)
                {
                    var picture = new PictureBox
                    {
                        Image = errorImage,
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Dock = DockStyle.Top,
                        Height = 180, // adjust as needed
                        Margin = new Padding(0, 10, 0, 0),
                        BackColor = Color.Transparent
                    };

                    featureCard.RowCount++;
                    featureCard.Controls.Add(picture, 0, featureCard.RowCount - 1);
                }
                else
                {
                    MessageBox.Show($"Failed to load {CxConstants.ICON_CX_AI_ERROR}");
                }
            }

            // Add border paint event
            featureCard.Paint += (_, e) =>
            {
                using (var pen = new Pen(_border, 1))
                {
                    var rect = featureCard.ClientRectangle;
                    rect.Width -= 1;
                    rect.Height -= 1;
                    e.Graphics.DrawRectangle(pen, rect);
                }
            };

            return featureCard;
        }
    }
}

