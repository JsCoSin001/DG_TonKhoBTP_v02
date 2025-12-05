using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.Component
{
    public class CustomButton:Button
    {
        private int _borderRadius = 71;
        public int BorderRadius
        {
            get => _borderRadius;
            set
            {
                _borderRadius = value;
                UpdateRegion();
                Invalidate(); // vẽ lại
            }
        }

        private void UpdateRegion()
        {
            var path = new GraphicsPath();
            int r = _borderRadius;

            // THỰC TẾ: d nên là đường kính, không phải bán kính:
            int d = r * 2;

            path.StartFigure();
            path.AddArc(new Rectangle(0, 0, d, d), 180, 90);
            path.AddArc(new Rectangle(Width - d, 0, d, d), 270, 90);
            path.AddArc(new Rectangle(Width - d, Height - d, d, d), 0, 90);
            path.AddArc(new Rectangle(0, Height - d, d, d), 90, 90);
            path.CloseFigure();

            this.Region = new Region(path);
        }



        public CustomButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRegion();
        }
    }
}
