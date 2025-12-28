using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.ComponentModel;

namespace ClinicManagement
{
    // Custom Rounded RoundedTextBox control with 10px radius and subtle drop shadow
    public class RoundedTextBox : UserControl
    {
        private TextBox innerTextBox = new TextBox();
        private Color _borderColor = Color.Gray;
        private Color _borderFocusColor = Color.FromArgb(0, 180, 170); // Teal default
        private int _borderRadius = 10;
        private string _placeholderText = "";
        private Color _placeholderColor = Color.DarkGray;
        private Color _textColor = Color.Black;
        private bool _isPlaceholderActive = false;
        private bool _isFocused = false;

        // Extra pixels to make the inner TextBox slightly larger than the padded area
        private const int INSET_OFFSET = 2;

        public RoundedTextBox()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.BackColor = Color.White;
            this.Padding = new Padding(10, 8, 10, 8); 
            this.Size = new Size(250, 40);

            innerTextBox.BorderStyle = BorderStyle.None;
            innerTextBox.BackColor = Color.White;
            innerTextBox.ForeColor = _textColor;
            innerTextBox.Font = this.Font;

            // Events
            innerTextBox.Enter += InnerTextBox_Enter;
            innerTextBox.Leave += InnerTextBox_Leave;
            innerTextBox.TextChanged += InnerTextBox_TextChanged;
            innerTextBox.KeyDown += InnerTextBox_KeyDown; // Forward key events

            // Initial layout
            UpdateInnerTextBoxLayout();
            this.Controls.Add(innerTextBox);

            this.Resize += (s, e) => UpdateInnerTextBoxLayout();
            this.Load += RoundedTextBox_Load;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (string.IsNullOrEmpty(innerTextBox.Text) && !string.IsNullOrEmpty(_placeholderText))
            {
                SetPlaceholder();
            }
        }

        private void RoundedTextBox_Load(object sender, EventArgs e)
        {
            // Already handled in OnHandleCreated, but keeping for safety if loaded via secondary means
            if (string.IsNullOrEmpty(innerTextBox.Text) && !string.IsNullOrEmpty(_placeholderText))
            {
                SetPlaceholder();
            }
        }

        // Properties
        [Category("Appearance")]
        public Color BorderColor { get { return _borderColor; } set { _borderColor = value; this.Invalidate(); } }

        [Category("Appearance")]
        public Color BorderFocusColor { get { return _borderFocusColor; } set { _borderFocusColor = value; } }

        [Category("Appearance")]
        public int BorderRadius { get { return _borderRadius; } set { _borderRadius = value; this.Invalidate(); } }

        [Category("Appearance")]
        public string PlaceholderText 
        { 
            get { return _placeholderText; } 
            set 
            { 
                _placeholderText = value; 
                if (string.IsNullOrEmpty(innerTextBox.Text)) SetPlaceholder(); 
            } 
        }

        [Category("Appearance")]
        public Color PlaceholderColor { get { return _placeholderColor; } set { _placeholderColor = value; } }

        [Category("Appearance")]
        public override Color ForeColor 
        { 
            get { return _textColor; } 
            set 
            { 
                _textColor = value; 
                if (!_isPlaceholderActive) innerTextBox.ForeColor = value; 
            } 
        }

        public override string Text
        {
            get 
            { 
                if (_isPlaceholderActive) return "";
                return innerTextBox.Text; 
            }
            set 
            { 
                innerTextBox.Text = value; 
                if (string.IsNullOrEmpty(value) && !this.ContainsFocus)
                {
                    SetPlaceholder();
                }
                else
                {
                    RemovePlaceholder();
                    innerTextBox.Text = value; // Re-set to ensure it's not empty
                }
            }
        }
        
         public bool Multiline
        {
            get { return innerTextBox.Multiline; }
            set
            {
                innerTextBox.Multiline = value;
                if (!value)
                {
                    this.Height = innerTextBox.PreferredHeight + this.Padding.Top + this.Padding.Bottom;
                }
                UpdateInnerTextBoxLayout();
            }
        }

        public new bool ReadOnly
        {
            get { return innerTextBox.ReadOnly; }
            set { innerTextBox.ReadOnly = value; }
        }

         public override Font Font
        {
            get { return base.Font; }
            set
            {
                base.Font = value;
                if (innerTextBox != null)
                {
                    innerTextBox.Font = value;
                     if (!innerTextBox.Multiline)
                    {
                        this.Height = innerTextBox.PreferredHeight + this.Padding.Top + this.Padding.Bottom;
                    }
                    UpdateInnerTextBoxLayout();
                }
            }
        }

        // Events Handling
        private void InnerTextBox_Enter(object sender, EventArgs e)
        {
            _isFocused = true;
            this.Invalidate(); // Repaint for focus border
            RemovePlaceholder();
        }

        private void InnerTextBox_Leave(object sender, EventArgs e)
        {
            _isFocused = false;
            this.Invalidate(); // Repaint for normal border
            SetPlaceholder();
        }

         private void InnerTextBox_TextChanged(object sender, EventArgs e)
        {
             OnTextChanged(e);
        }

        private void InnerTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(e);
        }

        private void SetPlaceholder()
        {
            if (string.IsNullOrEmpty(innerTextBox.Text) && !string.IsNullOrEmpty(_placeholderText))
            {
                _isPlaceholderActive = true;
                innerTextBox.Text = _placeholderText;
                innerTextBox.ForeColor = _placeholderColor;
            }
        }

        private void RemovePlaceholder()
        {
            if (_isPlaceholderActive)
            {
                _isPlaceholderActive = false;
                innerTextBox.Text = "";
                innerTextBox.ForeColor = _textColor;
            }
        }


        // Layout Logic
        private void UpdateInnerTextBoxLayout()
        {
            int innerX = this.Padding.Left - INSET_OFFSET;
            int innerY = this.Padding.Top - INSET_OFFSET;
            int innerW = this.Width - this.Padding.Left - this.Padding.Right + (INSET_OFFSET * 2);

            innerTextBox.Location = new Point(innerX, innerY);
            innerTextBox.Width = innerW;

            if (innerTextBox.Multiline)
            {
                innerTextBox.Height = this.Height - this.Padding.Top - this.Padding.Bottom + (INSET_OFFSET * 2);
            }
            else
            {
                // Ensure it's centered vertically if single line
                 int targetHeight = innerTextBox.PreferredHeight;
                 int verticalPadding = (this.Height - targetHeight) / 2;
                 // Refine Y if simple calculation isn't enough, but usually Padding handles it.
                 // Actually relying on Padding is better. 
                 innerTextBox.Location = new Point(innerX, this.Padding.Top); 
            }
        }

        // Painting
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int radius = _borderRadius;
            Rectangle rect = this.ClientRectangle;
            rect.Inflate(0, 0); // exact bounds
            // Adjust for pen width
            rect.Width -= 1;
            rect.Height -= 1;

            using (GraphicsPath path = RoundedRect(rect, radius))
            using (Pen borderPen = new Pen(_isFocused ? _borderFocusColor : _borderColor, _isFocused ? 2 : 1))
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Draw Border
                g.DrawPath(borderPen, path);
            }
        }

        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
        
        // Expose Inner Properties
        public TextBox Inner => innerTextBox;

        public void Clear()
        {
            innerTextBox.Clear();
            // Also reset placeholder state if needed logic exists, but innerTextBox.Clear() usually just sets text to empty string
            // Our Text setter handles placeholder logic, but Clear() on innerTextBox might bypass it if not careful.
            // Let's safe-guard:
            this.Text = ""; 
        }
    }
}