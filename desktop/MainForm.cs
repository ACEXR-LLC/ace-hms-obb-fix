using System.Diagnostics;
using System.Reflection;
using AceHeadsetFix.Reporting;
using AceHeadsetFix.Ui;

namespace AceHeadsetFix;

public sealed class MainForm : Form
{
    const string FixButtonText = "Check and fix my headset";
    const string StopButtonText = "Stop";

    static readonly string AppDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AceHeadsetFix");

    readonly RoundedButton _fixButton;
    readonly RichTextBox _results;
    readonly LinkLabel _logLink;
    FixLog? _log;
    CancellationTokenSource? _running;

    public MainForm()
    {
        Text = "Ace Headset Fix";
        Font = Theme.Body();
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(Scaled(760), Scaled(700));
        MinimumSize = new Size(Scaled(600), Scaled(560));
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;
        Icon = LoadApplicationIcon();

        _fixButton = BuildFixButton();
        _results = BuildResultsBox();
        _logLink = BuildLogLink();
        Controls.Add(BuildLayout());
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        DarkWindow.ApplyToTitleBar(Handle);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        var workingArea = Screen.FromControl(this).WorkingArea;
        Size = new Size(Math.Min(Width, workingArea.Width - 40), Math.Min(Height, workingArea.Height - 40));
        CenterToScreen();
        ActiveControl = _fixButton;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _running?.Cancel();
        base.OnFormClosing(e);
    }

    int Scaled(int value) => (int)Math.Round(value * DeviceDpi / 96f);

    TableLayoutPanel BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(Scaled(28), Scaled(20), Scaled(28), Scaled(20)),
            BackColor = Theme.Background,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        AddRow(layout, BuildHeader(), SizeType.AutoSize);
        AddRow(layout, BuildParagraph(Explanations.Intro, Theme.MutedText, 0, Scaled(16)), SizeType.AutoSize);
        AddRow(layout, _fixButton, SizeType.AutoSize);
        AddRow(layout, WrapInCard(_results), SizeType.Percent, 100f);
        AddRow(layout, BuildFooter(), SizeType.AutoSize);
        return layout;
    }

    static void AddRow(TableLayoutPanel layout, Control control, SizeType sizeType, float size = 0f)
    {
        layout.RowStyles.Add(new RowStyle(sizeType, size));
        layout.Controls.Add(control, 0, layout.RowStyles.Count - 1);
    }

    Control BuildHeader()
    {
        var header = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, Scaled(10)),
        };
        var logo = LoadLogo();
        if (logo is not null)
        {
            var logoHeight = Scaled(30);
            header.Controls.Add(new PictureBox
            {
                Image = logo,
                SizeMode = PictureBoxSizeMode.Zoom,
                Height = logoHeight,
                Width = logoHeight * logo.Width / logo.Height,
                Margin = new Padding(0, Scaled(4), Scaled(14), 0),
            });
        }
        header.Controls.Add(new Label
        {
            Text = "Headset Fix",
            Font = Theme.Body(19f, FontStyle.Bold),
            ForeColor = Theme.Text,
            AutoSize = true,
            Margin = new Padding(0),
        });
        return header;
    }

    static Icon? LoadApplicationIcon()
    {
        try
        {
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
            return null;
        }
    }

    static Image? LoadLogo()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("logo-white.png");
            return stream is null ? null : Image.FromStream(stream);
        }
        catch
        {
            return null;
        }
    }

    static Label BuildParagraph(string text, Color color, int topMargin, int bottomMargin) => new()
    {
        Text = text,
        ForeColor = color,
        AutoSize = true,
        Dock = DockStyle.Fill,
        Margin = new Padding(0, topMargin, 0, bottomMargin),
    };

    RoundedButton BuildFixButton()
    {
        var button = new RoundedButton
        {
            Text = FixButtonText,
            Font = Theme.Body(12.5f, FontStyle.Bold),
            Height = Scaled(52),
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, Scaled(16)),
            CornerRadius = Scaled(10),
        };
        button.Click += async (_, _) => await OnFixButtonClickedAsync();
        return button;
    }

    RichTextBox BuildResultsBox()
    {
        var box = new RichTextBox
        {
            ReadOnly = true,
            BackColor = Theme.Card,
            ForeColor = Theme.MutedText,
            BorderStyle = BorderStyle.None,
            Dock = DockStyle.Fill,
            DetectUrls = true,
            Text = "Results will appear here.",
        };
        DarkWindow.ApplyToScrollBars(box);
        return box;
    }

    CardPanel WrapInCard(Control content)
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(Scaled(14), Scaled(12), Scaled(14), Scaled(12)),
            Margin = new Padding(0, 0, 0, Scaled(16)),
            MinimumSize = new Size(0, Scaled(170)),
            CornerRadius = Scaled(12),
        };
        card.Controls.Add(content);
        return card;
    }

    LinkLabel BuildLogLink()
    {
        var link = new LinkLabel
        {
            Text = "Open the log folder",
            AutoSize = true,
            Enabled = false,
            LinkColor = Theme.Link,
            ActiveLinkColor = Theme.AccentHover,
            DisabledLinkColor = Theme.Disabled,
            LinkBehavior = LinkBehavior.HoverUnderline,
            Margin = new Padding(Scaled(18), Scaled(9), 0, 0),
        };
        link.LinkClicked += (_, _) => OpenExternally(_log?.LogDirectory ?? AppDataDirectory);
        return link;
    }

    Control BuildFooter()
    {
        var copyButton = new RoundedButton
        {
            Text = "Copy results",
            Fill = Theme.Secondary,
            HoverFill = Theme.SecondaryHover,
            PressedFill = Theme.SecondaryPressed,
            Width = Scaled(130),
            Height = Scaled(36),
            CornerRadius = Scaled(8),
            Margin = new Padding(0),
        };
        copyButton.Click += (_, _) => CopyResults();
        var aboutButton = new RoundedButton
        {
            Text = "About",
            Fill = Theme.Secondary,
            HoverFill = Theme.SecondaryHover,
            PressedFill = Theme.SecondaryPressed,
            Width = Scaled(100),
            Height = Scaled(36),
            CornerRadius = Scaled(8),
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0),
        };
        aboutButton.Click += (_, _) => ShowAbout();
        var guideButton = new RoundedButton
        {
            Text = "How to turn on USB debugging",
            Fill = Theme.Secondary,
            HoverFill = Theme.SecondaryHover,
            PressedFill = Theme.SecondaryPressed,
            Width = Scaled(230),
            Height = Scaled(36),
            CornerRadius = Scaled(8),
            Margin = new Padding(Scaled(12), 0, 0, 0),
        };
        guideButton.Click += (_, _) => ShowGuide();
        var supportLink = new LinkLabel
        {
            Text = "Contact Ace support",
            AutoSize = true,
            LinkColor = Theme.Link,
            ActiveLinkColor = Theme.AccentHover,
            LinkBehavior = LinkBehavior.HoverUnderline,
            Margin = new Padding(Scaled(18), Scaled(9), 0, 0),
        };
        supportLink.LinkClicked += (_, _) => OpenExternally(Explanations.SupportUrl);
        var left = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0), Anchor = AnchorStyles.Left };
        left.Controls.Add(copyButton);
        left.Controls.Add(guideButton);
        left.Controls.Add(supportLink);
        left.Controls.Add(_logLink);
        var footer = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 2 };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.Controls.Add(left, 0, 0);
        footer.Controls.Add(aboutButton, 1, 0);
        return footer;
    }

    void ShowGuide()
    {
        using var guide = new GuideDialog(Icon);
        guide.ShowDialog(this);
    }

    void ShowAbout()
    {
        using var about = new AboutDialog(Icon);
        about.ShowDialog(this);
    }

    async Task OnFixButtonClickedAsync()
    {
        if (_running is not null)
        {
            _fixButton.Enabled = false;
            _running.Cancel();
            return;
        }
        await RunFixAsync();
    }

    async Task RunFixAsync()
    {
        _running = new CancellationTokenSource();
        _fixButton.Text = StopButtonText;
        _fixButton.Fill = Theme.Secondary;
        _fixButton.HoverFill = Theme.SecondaryHover;
        _fixButton.PressedFill = Theme.SecondaryPressed;
        _results.Clear();
        _results.ForeColor = Theme.Text;
        _log = new FixLog(AppDataDirectory);
        _logLink.Enabled = true;
        _results.LinkClicked += OnResultsLinkClicked;
        var status = new Progress<StatusLine>(AppendStatus);
        var fixer = new HeadsetFixer(AppDataDirectory, _log, status, _running.Token);
        try
        {
            var summary = await Task.Run(fixer.RunAsync);
            AppendSummary(summary);
        }
        catch (OperationCanceledException)
        {
            _log.Write("Stopped by the user");
            AppendStatus(new StatusLine(StepOutcome.Info, "Stopped. Click the button to start again."));
        }
        catch (Exception exception)
        {
            _log.Write("Unhandled: " + exception);
            AppendStatus(new StatusLine(StepOutcome.Failed, "Something unexpected went wrong: " + exception.Message, "Please send the log file to Ace support."));
            AppendSummary(new FixSummary { Problems = 1 });
        }
        finally
        {
            _results.LinkClicked -= OnResultsLinkClicked;
            _running.Dispose();
            _running = null;
            _fixButton.Text = FixButtonText;
            _fixButton.Fill = Theme.Accent;
            _fixButton.HoverFill = Theme.AccentHover;
            _fixButton.PressedFill = Theme.AccentPressed;
            _fixButton.Enabled = true;
        }
    }

    void AppendStatus(StatusLine line)
    {
        _log?.WriteStatus(line);
        var (glyph, color, bold) = line.Outcome switch
        {
            StepOutcome.Working => ("…", Theme.Working, false),
            StepOutcome.Passed => ("✔", Theme.Success, false),
            StepOutcome.Failed => ("✖", Theme.Failure, true),
            StepOutcome.Instruction => ("➜", Theme.Instruction, true),
            _ => ("•", Theme.Info, false),
        };
        AppendText($"{glyph}  {line.Text}{Environment.NewLine}", color, bold);
        if (line.Detail is not null)
            AppendText($"      {line.Detail.Replace(Environment.NewLine, Environment.NewLine + "      ")}{Environment.NewLine}", Theme.MutedText, false);
        _results.ScrollToCaret();
    }

    void AppendSummary(FixSummary summary)
    {
        AppendText(Environment.NewLine, Theme.Text, false);
        AppendText(summary.Headline + Environment.NewLine, summary.Problems > 0 ? Theme.Failure : Theme.Success, true);
        _results.ScrollToCaret();
    }

    void AppendText(string text, Color color, bool bold)
    {
        _results.SelectionStart = _results.TextLength;
        _results.SelectionLength = 0;
        _results.SelectionColor = color;
        _results.SelectionFont = bold ? new Font(_results.Font, FontStyle.Bold) : _results.Font;
        _results.AppendText(text);
    }

    void CopyResults()
    {
        if (_results.TextLength > 0)
            Clipboard.SetText(_results.Text);
    }

    static void OnResultsLinkClicked(object? sender, LinkClickedEventArgs e)
    {
        if (e.LinkText is not null)
            OpenExternally(e.LinkText);
    }

    static void OpenExternally(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch
        {
        }
    }
}
