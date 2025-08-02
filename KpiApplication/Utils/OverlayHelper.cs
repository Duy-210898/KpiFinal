using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class OverlayHelper
{
    private Panel overlayPanel;
    private Label overlayText;
    private Timer overlayTimer;
    private Control parent;

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect,
        int nBottomRect, int nWidthEllipse, int nHeightEllipse);

    public OverlayHelper(Control parentControl)
    {
        parent = parentControl ?? throw new ArgumentNullException(nameof(parentControl));
        InitOverlay();
    }

    private void InitOverlay()
    {
        overlayText = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 16)
        };

        overlayPanel = new Panel
        {
            Size = new Size(250, 80),
            BackColor = Color.FromArgb(50, 50, 50, 50),
            Visible = false,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(10)
        };

        overlayPanel.Controls.Add(overlayText);
        overlayPanel.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, overlayPanel.Width, overlayPanel.Height, 20, 20));

        parent.Controls.Add(overlayPanel);
        overlayPanel.BringToFront();

        overlayTimer = new Timer { Interval = 1000 };
        overlayTimer.Tick += (s, e) =>
        {
            overlayPanel.Visible = false;
            overlayTimer.Stop();
        };
    }

    public void Show(string text)
    {
        overlayText.Text = text;
        overlayPanel.Location = new Point(
            (parent.Width - overlayPanel.Width) / 2,
            (parent.Height - overlayPanel.Height) / 2);

        overlayPanel.Visible = true;
        overlayPanel.BringToFront();
        overlayTimer.Stop();
        overlayTimer.Start();
    }
}
