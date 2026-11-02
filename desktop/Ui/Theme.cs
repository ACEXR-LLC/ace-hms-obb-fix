namespace AceHeadsetFix.Ui;

public static class Theme
{
    public static readonly Color Background = Color.FromArgb(18, 20, 24);
    public static readonly Color Card = Color.FromArgb(27, 30, 37);
    public static readonly Color CardBorder = Color.FromArgb(42, 46, 56);
    public static readonly Color Text = Color.FromArgb(236, 238, 242);
    public static readonly Color MutedText = Color.FromArgb(154, 160, 171);
    public static readonly Color Accent = Color.FromArgb(240, 49, 79);
    public static readonly Color AccentHover = Color.FromArgb(255, 77, 105);
    public static readonly Color AccentPressed = Color.FromArgb(201, 37, 63);
    public static readonly Color Secondary = Color.FromArgb(38, 42, 51);
    public static readonly Color SecondaryHover = Color.FromArgb(48, 53, 63);
    public static readonly Color SecondaryPressed = Color.FromArgb(30, 33, 40);
    public static readonly Color Disabled = Color.FromArgb(60, 64, 74);
    public static readonly Color Link = Color.FromArgb(90, 176, 255);
    public static readonly Color Success = Color.FromArgb(61, 214, 140);
    public static readonly Color Failure = Color.FromArgb(255, 92, 108);
    public static readonly Color Instruction = Color.FromArgb(90, 176, 255);
    public static readonly Color Working = Color.FromArgb(138, 144, 160);
    public static readonly Color Info = Color.FromArgb(213, 217, 224);

    public static Font Body(float size = 10f, FontStyle style = FontStyle.Regular) => new("Segoe UI", size, style);
}
