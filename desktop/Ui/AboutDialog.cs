using System.Reflection;

namespace AceHeadsetFix.Ui;

public sealed class AboutDialog : Form
{
    public AboutDialog(Icon? icon)
    {
        Text = "About Ace Headset Fix";
        Icon = icon;
        ShowIcon = icon is not null;
        Font = Theme.Body();
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(Scaled(560), Scaled(360));
        Controls.Add(BuildLayout());
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        DarkWindow.ApplyToTitleBar(Handle);
    }

    int Scaled(int value) => (int)Math.Round(value * DeviceDpi / 96f);

    TableLayoutPanel BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(Scaled(24), Scaled(18), Scaled(24), Scaled(18)),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        AddRow(layout, BuildHeading(), SizeType.AutoSize);
        AddRow(layout, BuildBody(), SizeType.Percent, 100f);
        AddRow(layout, BuildFooter(), SizeType.AutoSize);
        return layout;
    }

    static void AddRow(TableLayoutPanel layout, Control control, SizeType sizeType, float size = 0f)
    {
        layout.RowStyles.Add(new RowStyle(sizeType, size));
        layout.Controls.Add(control, 0, layout.RowStyles.Count - 1);
    }

    Label BuildHeading() => new()
    {
        Text = "Why does this happen?",
        Font = Theme.Body(13f, FontStyle.Bold),
        ForeColor = Theme.Text,
        AutoSize = true,
        Margin = new Padding(0, 0, 0, Scaled(10)),
    };

    TextBox BuildBody()
    {
        var body = new TextBox
        {
            Text = Explanations.Why,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Theme.Background,
            ForeColor = Theme.Info,
            BorderStyle = BorderStyle.None,
            Dock = DockStyle.Fill,
            TabStop = false,
            Margin = new Padding(0, 0, 0, Scaled(12)),
        };
        DarkWindow.ApplyToScrollBars(body);
        return body;
    }

    Control BuildFooter()
    {
        var version = new Label
        {
            Text = $"Ace Headset Fix {ApplicationVersion()}",
            ForeColor = Theme.MutedText,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, Scaled(8), 0, 0),
        };
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
            Margin = new Padding(0),
            DialogResult = DialogResult.OK,
        };
        CancelButton = close;
        AcceptButton = close;
        var footer = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 2 };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.Controls.Add(version, 0, 0);
        footer.Controls.Add(close, 1, 0);
        return footer;
    }

    static string ApplicationVersion() =>
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        ?? string.Empty;
}
