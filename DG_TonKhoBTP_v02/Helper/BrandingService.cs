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
    /// Quản lý logo, icon và nội dung branding dùng chung cho giao diện.
    /// Ảnh được đọc từ thư mục chạy ứng dụng để có thể thay đổi mà không sửa Designer/resx.
    /// </summary>
    public static class BrandingService
    {
        private const string DefaultMainLogoRelativePath = @"Assets\main-logo.png";
        private const string DefaultHomeLogoRelativePath = @"Assets\homepage-logo.png";
        private const string DefaultIconRelativePath = @"Assets\app-icon.png";
        private const string DefaultHomeTitle = "WELCOME TO DONG GIANG FACTORY";
        private const string DefaultCompanyName = "ĐÔNG GIANG";

        public static string MainLogoPath
        {
            get { return GetConfiguredPath("BrandingMainLogoPath", DefaultMainLogoRelativePath); }
        }

        public static string HomeLogoPath
        {
            get { return GetConfiguredPath("BrandingHomeLogoPath", DefaultHomeLogoRelativePath); }
        }

        public static string IconPath
        {
            get { return GetConfiguredPath("BrandingIconPath", DefaultIconRelativePath); }
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
        /// Control hoặc đường dẫn nào không cần thay đổi thì để null.
        /// logoPath và iconPath độc lập, vì vậy PictureBox và Form.Icon có thể dùng hai ảnh khác nhau.
        /// Nếu một file ảnh không tồn tại hoặc không đọc được, ảnh/icon do Designer gán sẵn cho control đó sẽ được giữ nguyên.
        /// </summary>
        public static void Apply(
            PictureBox logoControl = null,
            Form form = null,
            Label homeTitleLabel = null,
            string logoPath = null,
            string iconPath = null)
        {
            if (homeTitleLabel != null)
            {
                homeTitleLabel.Text = HomeTitle;
            }

            //if (companyNameLabel != null)
            //{
            //    companyNameLabel.Text = CompanyName;
            //}

            if (logoControl != null && !string.IsNullOrWhiteSpace(logoPath))
            {
                using (Image sourceLogo = LoadImage(logoPath))
                {
                    if (sourceLogo != null)
                    {
                        Image oldImage = logoControl.Image;

                        // Ảnh có thể có độ phân giải rất lớn. Zoom giúp ảnh luôn
                        // co vừa PictureBox thay vì chỉ hiển thị góc trên-trái của ảnh.
                        logoControl.SizeMode = PictureBoxSizeMode.Zoom;
                        logoControl.Image = new Bitmap(sourceLogo);

                        if (oldImage != null)
                        {
                            oldImage.Dispose();
                        }
                    }
                }
            }

            if (form != null && !string.IsNullOrWhiteSpace(iconPath))
            {
                using (Image sourceIcon = LoadImage(iconPath))
                {
                    if (sourceIcon != null)
                    {
                        Icon icon = CreateIcon(sourceIcon);
                        if (icon != null)
                        {
                            form.Icon = icon;
                        }
                    }
                }
            }
        }

        private static string GetConfiguredPath(string appSettingKey, string defaultRelativePath)
        {
            string configuredPath = ConfigurationManager.AppSettings[appSettingKey] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                configuredPath = defaultRelativePath;
            }

            return ResolvePath(configuredPath);
        }

        private static string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            path = path.Trim();
            if (Path.IsPathRooted(path))
            {
                return path;
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }

        private static Image LoadImage(string imagePath)
        {
            string resolvedPath = ResolvePath(imagePath);

            try
            {
                if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
                {
                    Debug.WriteLine("BrandingService: Không tìm thấy ảnh: " + resolvedPath);
                    return null;
                }

                // Clone ảnh để không giữ khóa file sau khi load.
                using (FileStream stream = new FileStream(resolvedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (Image source = Image.FromStream(stream))
                {
                    return new Bitmap(source);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BrandingService: Không thể load ảnh '" + resolvedPath + "'. " + ex.Message);
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
                Debug.WriteLine("BrandingService: Không thể tạo icon từ ảnh. " + ex.Message);
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
