using System.Drawing.Drawing2D;

namespace AceHeadsetFix.Ui;

public sealed class RoundedButton : Button
{
    bool _hovering;
    bool _pressed;

    public Color Fill { get; set; } = Theme.Accent;
    public Color HoverFill { get; set; } = Theme.AccentHover;
    public Color PressedFill { get; set; } = Theme.AccentPressed;
    public int CornerRadius { get; set; } = 10;

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        ForeColor = Theme.Text;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovering = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovering = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        _pressed = true;
        Invalidate();
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var graphics = pevent.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Parent?.BackColor ?? Theme.Background);
        using var path = RoundedRectangle(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius);
        using var brush = new SolidBrush(CurrentFill());
        graphics.FillPath(brush, path);
        TextRenderer.DrawText(graphics, Text, Font, ClientRectangle, Enabled ? ForeColor : Theme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    Color CurrentFill() =>
        !Enabled ? Theme.Disabled :
        _pressed ? PressedFill :
        _hovering ? HoverFill :
        Fill;

    static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
