using System;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Helper
{
    /// <summary>
    /// Quản lý logo và nội dung branding dùng chung cho giao diện.
    /// Logo được đọc từ thư mục chạy ứng dụng để có thể thay đổi mà không sửa Designer/resx.
    /// </summary>
    public static class BrandingService
    {
        private const string DefaultLogoRelativePath = @"Assets\logo.png";
        private const string DefaultHomeTitle = "WELCOME TO DONG GIANG FACTORY";
        private const string DefaultCompanyName = "ĐÔNG GIANG";

        public static string LogoPath
        {
            get
            {
                string configuredPath = ConfigurationManager.AppSettings["BrandingLogoPath"] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(configuredPath))
                {
                    configuredPath = DefaultLogoRelativePath;
                }

                configuredPath = configuredPath.Trim();
                if (Path.IsPathRooted(configuredPath))
                {
                    return configuredPath;
                }

                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuredPath);
            }
        }

        public static string HomeTitle
        {
            get
            {
                string configuredTitle = ConfigurationManager.AppSettings["BrandingHomeTitle"] ?? string.Empty;
                return string.IsNullOrWhiteSpace(configuredTitle)
                    ? DefaultHomeTitle
                    : configuredTitle.Trim();
            }
        }

        public static string CompanyName
        {
            get
            {
                string configuredName = ConfigurationManager.AppSettings["BrandingCompanyName"] ?? string.Empty;
                return string.IsNullOrWhiteSpace(configuredName)
                    ? DefaultCompanyName
                    : configuredName.Trim();
            }
        }

        /// <summary>
        /// Áp branding cho các control được truyền vào.
        /// Control nào không cần thay đổi thì để null.
        /// Nếu file logo không tồn tại hoặc không đọc được, ảnh/icon do Designer gán sẵn sẽ được giữ nguyên.
        /// </summary>
        public static void Apply(PictureBox logoControl = null, Form form = null, Label homeTitleLabel = null, Label companyNameLabel = null)
        {
            if (homeTitleLabel != null)
            {
                homeTitleLabel.Text = HomeTitle;
            }

            if (companyNameLabel != null)
            {
                companyNameLabel.Text = CompanyName;
            }

            Image sourceLogo = LoadLogoImage();
            if (sourceLogo == null)
            {
                return;
            }

            using (sourceLogo)
            {
                if (logoControl != null)
                {
                    Image oldImage = logoControl.Image;

                    // logo.png có thể có độ phân giải rất lớn. Zoom giúp ảnh luôn
                    // co vừa PictureBox thay vì chỉ hiển thị góc trên-trái của ảnh.
                    logoControl.SizeMode = PictureBoxSizeMode.Zoom;
                    logoControl.Image = new Bitmap(sourceLogo);

                    if (oldImage != null)
                    {
                        oldImage.Dispose();
                    }
                }

                if (form != null)
                {
                    Icon icon = CreateIcon(sourceLogo);
                    if (icon != null)
                    {
                        form.Icon = icon;
                    }
                }
            }
        }

        private static Image LoadLogoImage()
        {
            try
            {
                if (!File.Exists(LogoPath))
                {
                    Debug.WriteLine("BrandingService: Không tìm thấy logo: " + LogoPath);
                    return null;
                }

                // Clone ảnh để không giữ khóa file logo.png sau khi load.
                using (FileStream stream = new FileStream(LogoPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (Image source = Image.FromStream(stream))
                {
                    return new Bitmap(source);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BrandingService: Không thể load logo. " + ex.Message);
                return null;
            }
        }

        private static Icon CreateIcon(Image source)
        {
            IntPtr iconHandle = IntPtr.Zero;

            try
            {
                using (Bitmap iconBitmap = new Bitmap(32, 32))
                using (Graphics graphics = Graphics.FromImage(iconBitmap))
                {
                    graphics.Clear(Color.Transparent);
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    float scale = Math.Min(32f / source.Width, 32f / source.Height);
                    int width = Math.Max(1, (int)Math.Round(source.Width * scale));
                    int height = Math.Max(1, (int)Math.Round(source.Height * scale));
                    int x = (32 - width) / 2;
                    int y = (32 - height) / 2;

                    graphics.DrawImage(source, x, y, width, height);

                    iconHandle = iconBitmap.GetHicon();
                    using (Icon temporaryIcon = Icon.FromHandle(iconHandle))
                    {
                        return (Icon)temporaryIcon.Clone();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BrandingService: Không thể tạo icon từ logo. " + ex.Message);
                return null;
            }
            finally
            {
                if (iconHandle != IntPtr.Zero)
                {
                    DestroyIcon(iconHandle);
                }
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}
