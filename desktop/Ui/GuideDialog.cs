using System.Drawing.Drawing2D;
using System.Reflection;

namespace AceHeadsetFix.Ui;

/// <summary>
/// Step-by-step instructions, with reference pictures, for switching USB debugging on in a shared-mode headset.
/// Mirrors the guide on the web page.
/// </summary>
public sealed class GuideDialog : Form
{
    readonly Panel _scroller;
    readonly RoundedButton _close;

    public GuideDialog(Icon? icon)
    {
        Text = "How to turn on USB debugging";
        Icon = icon;
        ShowIcon = icon is not null;
        Font = Theme.Body();
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(Scaled(680), Scaled(720));
        MinimumSize = new Size(Scaled(480), Scaled(420));
        DoubleBuffered = true;

        _scroller = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = new Padding(0) };
        _scroller.Controls.Add(BuildSteps());
        DarkWindow.ApplyToScrollBars(_scroller);
        _close = BuildCloseButton();
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
        CenterToParent();
        ActiveControl = _close;
        _scroller.AutoScrollPosition = Point.Empty;
    }

    int Scaled(int value) => (int)Math.Round(value * DeviceDpi / 96f);

    TableLayoutPanel BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(Scaled(24), Scaled(18), Scaled(12), Scaled(18)),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        AddRow(layout, BuildIntro(), SizeType.AutoSize);
        AddRow(layout, _scroller, SizeType.Percent, 100f);
        AddRow(layout, BuildFooter(), SizeType.AutoSize);
        return layout;
    }

    static void AddRow(TableLayoutPanel layout, Control control, SizeType sizeType, float size = 0f)
    {
        layout.RowStyles.Add(new RowStyle(sizeType, size));
        layout.Controls.Add(control, 0, layout.RowStyles.Count - 1);
    }

    Label BuildIntro() => new()
    {
        Text = "This takes about a minute and is done in the headset, so put it on. The headset turns this setting off again when it " +
               "restarts, so repeat these steps if this app cannot find your headset later.",
        ForeColor = Theme.Info,
        AutoSize = true,
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 0, Scaled(12), Scaled(14)),
    };

    /// <summary>
    /// Two columns: the step number badge and the step body. Docked to the top of the scroller so its width follows the
    /// window and the text wraps instead of scrolling sideways.
    /// </summary>
    TableLayoutPanel BuildSteps()
    {
        var steps = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0, 0, Scaled(16), 0),
        };
        steps.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scaled(44)));
        steps.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        var number = 0;
        foreach (var step in Steps)
        {
            number++;
            steps.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var row = steps.RowStyles.Count - 1;
            steps.Controls.Add(BuildBadge(number), 0, row);
            steps.Controls.Add(BuildStepBody(step), 1, row);
        }

        return steps;
    }

    Control BuildBadge(int number)
    {
        var size = Scaled(30);
        return new StepBadge
        {
            Text = number.ToString(),
            Font = Theme.Body(10f, FontStyle.Bold),
            Width = size,
            Height = size,
            Margin = new Padding(0, Scaled(4), 0, 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
        };
    }

    Control BuildStepBody(Step step)
    {
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Margin = new Padding(0, 0, 0, Scaled(18)),
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        AddRow(body, new Label
        {
            Text = step.Title,
            Font = Theme.Body(11.5f, FontStyle.Bold),
            ForeColor = Theme.Text,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, Scaled(4), 0, Scaled(4)),
        }, SizeType.AutoSize);
        AddRow(body, new Label
        {
            Text = step.Text,
            ForeColor = Theme.Info,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
        }, SizeType.AutoSize);

        if (step.ImageResource is null)
            return body;

        var image = LoadImage(step.ImageResource);
        if (image is not null)
        {
            var width = Scaled(520);
            AddRow(body, new PictureBox
            {
                Image = image,
                SizeMode = PictureBoxSizeMode.Zoom,
                Width = width,
                Height = width * image.Height / image.Width,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Margin = new Padding(0, Scaled(10), 0, 0),
            }, SizeType.AutoSize);
        }
        if (step.Caption is not null)
        {
            AddRow(body, new Label
            {
                Text = step.Caption,
                ForeColor = Theme.MutedText,
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, Scaled(4), 0, 0),
            }, SizeType.AutoSize);
        }
        return body;
    }

    RoundedButton BuildCloseButton()
    {
        var close = new RoundedButton
        {
            Text = "Close",
            Fill = Theme.Secondary,
            HoverFill = Theme.SecondaryHover,
            PressedFill = Theme.SecondaryPressed,
            Width = Scaled(110),
            Height = Scaled(36),
            CornerRadius = Scaled(8),
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 0, Scaled(12), 0),
            DialogResult = DialogResult.OK,
        };
        CancelButton = close;
        AcceptButton = close;
        return close;
    }

    Control BuildFooter()
    {
        var path = new Label
        {
            Text = "Headset path: Settings > Developer > USB Debugging",
            ForeColor = Theme.MutedText,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, Scaled(8), 0, 0),
        };
        var footer = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 2, Margin = new Padding(0, Scaled(10), 0, 0) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.Controls.Add(path, 0, 0);
        footer.Controls.Add(_close, 1, 0);
        return footer;
    }

    static Image? LoadImage(string resourceName)
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            return stream is null ? null : Image.FromStream(stream);
        }
        catch
        {
            return null;
        }
    }

    sealed class StepBadge : Control
    {
        public StepBadge()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent?.BackColor ?? Theme.Background);
            using var brush = new SolidBrush(Theme.Accent);
            e.Graphics.FillEllipse(brush, 0, 0, Width - 1, Height - 1);
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    sealed record Step(string Title, string Text, string? ImageResource = null, string? Caption = null);

    static readonly Step[] Steps =
    [
        new("Open Settings",
            "Press the Meta button on your controller, then select Settings."),
        new("Select Developer",
            "In the menu on the left, scroll down and select Developer."),
        new("Turn on USB Debugging",
            "Turn on the USB Debugging switch."),
        new("Connect the headset",
            "Plug the headset into this computer with a USB cable, then click \"Check and fix my headset\" in this app. " +
            "When the headset asks \"Allow USB debugging?\", select \"Always allow from this computer\", then Allow.",
            "guide.allow-usb-debugging.jpg",
            "Select \"Always allow from this computer\", then \"Allow\". The message in your headset may look slightly different, and it only appears while the headset is worn."),
    ];
}
