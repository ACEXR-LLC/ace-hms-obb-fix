using System.Drawing.Drawing2D;

namespace AceHeadsetFix.Ui;

public sealed class CardPanel : Panel
{
    public int CornerRadius { get; set; } = 12;

    public CardPanel()
    {
        BackColor = Theme.Card;
        DoubleBuffered = true;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? Theme.Background);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedRectangle(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius);
        using var fill = new SolidBrush(BackColor);
        using var border = new Pen(Theme.CardBorder);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
    }

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
