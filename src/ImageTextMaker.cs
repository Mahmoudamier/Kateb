using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace ImageTextMaker
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0 && string.Equals(args[0], "--selftest", StringComparison.OrdinalIgnoreCase))
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                using (Bitmap bmp = new Bitmap(1280, 720))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                        g.Clear(Color.FromArgb(32, 31, 31));

                    ImageTextRenderer.Render(
                        bmp,
                        "هل تنفع التخلية والإبانة في كل الأمور ؟ مثال التحرش .. - أرى التعبير للناس عن موقف حصل مؤلماً أكثر من الموقف نفسه.. فهل يمكن للوقت أن يحل محل التخلية؟",
                        30f,
                        false,
                        true);

                    bmp.Save(Path.Combine(desktop, "selftest.png"), ImageFormat.Png);
                }
                return;
            }

            Application.Run(new MainForm());
        }
    }

    public static class ImageTextRenderer
    {
        private static PrivateFontCollection s_fonts;

        private static FontFamily GetFontFamily()
        {
            if (s_fonts != null && s_fonts.Families.Length > 0) return s_fonts.Families[0];

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string path = Path.Combine(baseDir, "fonts", "Tajawal-Regular.ttf");
            if (!File.Exists(path)) path = Path.Combine(baseDir, "Tajawal-Regular.ttf");

            if (File.Exists(path))
            {
                try
                {
                    s_fonts = new PrivateFontCollection();
                    s_fonts.AddFontFile(path);
                    if (s_fonts.Families.Length > 0) return s_fonts.Families[0];
                }
                catch
                {
                    s_fonts = null;
                }
            }

            return new FontFamily("Segoe UI");
        }

        public static void Render(Bitmap bmp, string text, float requestedSize, bool auto, bool showPanel)
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.CompositingQuality = CompositingQuality.HighQuality;

                StringFormat sfMeasure = new StringFormat(StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoWrap);
                sfMeasure.Alignment = StringAlignment.Near;
                sfMeasure.LineAlignment = StringAlignment.Center;

                StringFormat sfDraw = new StringFormat(StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoWrap);
                sfDraw.Alignment = StringAlignment.Center;
                sfDraw.LineAlignment = StringAlignment.Center;

                FontFamily family = GetFontFamily();
                float size = auto ? Math.Max(16f, Math.Min(120f, bmp.Height * 0.0417f)) : requestedSize;
                float maxTextW = bmp.Width * 0.80f;
                float maxPanelH = bmp.Height * 0.86f;

                Font font = null;
                List<string> lines = null;
                float lineH = 0f;
                float padY = 0f;

                try
                {
                    for (int guard = 0; guard < 100; guard++)
                    {
                        if (font != null) font.Dispose();
                        font = new Font(family, size, FontStyle.Regular, GraphicsUnit.Pixel);
                        lineH = size * 1.68f;
                        padY = size * 1.28f;
                        lines = WrapText(g, text, font, maxTextW, sfMeasure);

                        float panelH = lines.Count * lineH + padY * 2f;
                        if (panelH <= maxPanelH || size <= 11f) break;
                        size = Math.Max(11f, size * 0.94f);
                    }

                    float panelW = Math.Min(bmp.Width * 0.94f, maxTextW + size * 4.8f);
                    float panelH2 = lines.Count * lineH + padY * 2f;
                    float panelX = (bmp.Width - panelW) / 2f;
                    float panelY = (bmp.Height - panelH2) / 2f;

                    using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(248, 248, 245)))
                    using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
                    {
                        if (showPanel)
                        {
                            using (GraphicsPath path = RoundedRect(panelX, panelY, panelW, panelH2, Math.Min(size, 30f)))
                            using (SolidBrush fill = new SolidBrush(Color.FromArgb(180, 8, 8, 9)))
                            using (Pen pen = new Pen(Color.FromArgb(75, 255, 255, 255), 1.5f))
                            {
                                g.FillPath(fill, path);
                                g.DrawPath(pen, path);
                            }
                        }

                        for (int i = 0; i < lines.Count; i++)
                        {
                            float y = panelY + padY / 2f + i * lineH;
                            RectangleF box = new RectangleF(panelX, y, panelW, lineH);
                            RectangleF shadow = new RectangleF(panelX + size * 0.05f, y + size * 0.07f, panelW, lineH);
                            g.DrawString(lines[i], font, shadowBrush, shadow, sfDraw);
                            g.DrawString(lines[i], font, textBrush, box, sfDraw);
                        }
                    }
                }
                finally
                {
                    if (font != null) font.Dispose();
                    sfMeasure.Dispose();
                    sfDraw.Dispose();
                }
            }
        }

        private static GraphicsPath RoundedRect(float x, float y, float w, float h, float r)
        {
            GraphicsPath p = new GraphicsPath();
            float d = 2f * r;
            p.AddArc(x, y, d, d, 180, 90);
            p.AddArc(x + w - d, y, d, d, 270, 90);
            p.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            p.AddArc(x, y + h - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private static List<string> WrapText(Graphics g, string text, Font font, float maxWidth, StringFormat sf)
        {
            List<string> result = new List<string>();
            string[] paras = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            foreach (string para in paras)
            {
                if (para.Length == 0)
                {
                    result.Add("");
                    continue;
                }

                List<string> tokens = Tokenize(para);
                string current = "";

                foreach (string tok in tokens)
                {
                    if (char.IsWhiteSpace(tok[0]))
                    {
                        current += tok;
                        continue;
                    }

                    string candidate = current + tok;
                    if (g.MeasureString(candidate, font, 100000, sf).Width > maxWidth && current.Trim().Length > 0)
                    {
                        result.Add(current);
                        current = tok;

                        if (g.MeasureString(current, font, 100000, sf).Width > maxWidth)
                        {
                            string chunk = "";
                            foreach (char ch in current)
                            {
                                string next = chunk + ch;
                                if (g.MeasureString(next, font, 100000, sf).Width > maxWidth && chunk.Length > 0)
                                {
                                    result.Add(chunk);
                                    chunk = ch.ToString();
                                }
                                else
                                {
                                    chunk = next;
                                }
                            }
                            current = chunk;
                        }
                    }
                    else
                    {
                        current = candidate;
                    }
                }

                result.Add(current);
            }

            return result;
        }

        private static List<string> Tokenize(string s)
        {
            List<string> tokens = new List<string>();
            int i = 0;
            while (i < s.Length)
            {
                bool ws = char.IsWhiteSpace(s[i]);
                int j = i;
                while (j < s.Length && char.IsWhiteSpace(s[j]) == ws) j++;
                tokens.Add(s.Substring(i, j - i));
                i = j;
            }
            return tokens;
        }
    }
}
