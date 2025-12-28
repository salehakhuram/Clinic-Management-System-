using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class CardPanel : Panel
{
    public CardPanel()
    {
        this.BackColor = Color.White;
        this.DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        int radius = 20;
        var rect = this.ClientRectangle;
        var path = RoundedPath(rect, radius);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        // Shadow
        using (SolidBrush shadow = new SolidBrush(Color.FromArgb(45, 0, 0, 0)))
        {
            Rectangle shadowRect = new Rectangle(rect.X + 4, rect.Y + 4, rect.Width, rect.Height);
            e.Graphics.FillPath(shadow, RoundedPath(shadowRect, radius));
        }

        // Light 3D gradient
        using (LinearGradientBrush brush = new LinearGradientBrush(rect, Color.White, Color.Gainsboro, 90F))
        {
            e.Graphics.FillPath(brush, path);
        }
    }

    private GraphicsPath RoundedPath(Rectangle rect, int radius)
    {
        int d = radius * 2;
        GraphicsPath path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
