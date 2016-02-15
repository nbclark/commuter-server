using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace MobileSrc.Commuter.Server
{
    public partial class LiveTile : System.Web.UI.Page
    {
        private static System.Drawing.Text.PrivateFontCollection _FontCollection = null;
        static LiveTile()
        {
            var ttf = String.Format(
                @"{0}\images\segoeui.ttf",
                HttpRuntime.AppDomainAppPath);

            _FontCollection = new System.Drawing.Text.PrivateFontCollection();
            _FontCollection.AddFontFile(ttf);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Weekday Work_Backroads_35_min_Sunday_8:30PM
            var commuteName = Request["commute"];// parts[0];
            var routeName = Request["route"];//parts[1];
            var timeDuration = Request["duration"];//parts[2];
            var timeInterval = Request["interval"];//parts[3];
            var dayString = Request["day"];//parts[4];
            var colorString = Request["color"];//parts[5];

            var backgroundImage = String.Format(
                @"{0}\images\overlay.png",
                HttpRuntime.AppDomainAppPath,
                colorString);

            var lightImage = String.Format(
                @"{0}\images\light.png",
                HttpRuntime.AppDomainAppPath,
                "green");

            using (Image overlayBmp = Bitmap.FromFile(backgroundImage))
            {
                using (Bitmap bmp = new Bitmap(overlayBmp.Width, overlayBmp.Height))
                {
                    using (Graphics gfx = Graphics.FromImage(bmp))
                    {
                        //gfx.Clear(Color.FromArgb(Int32.Parse(colorString.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber)));

                        gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                        gfx.SmoothingMode = SmoothingMode.AntiAlias;
                        StringFormat format = new StringFormat();
                        format.FormatFlags = StringFormatFlags.NoFontFallback;
                        format.Alignment = StringAlignment.Center;
                        format.LineAlignment = StringAlignment.Center;

                        gfx.DrawImage(overlayBmp, new Point(0, 0));

                        using (SolidBrush brush = new SolidBrush(Color.White))
                        {
                            using (Font font = new Font(_FontCollection.Families[0], 30.0f))
                            {
                                format.Alignment = StringAlignment.Near;
                                format.LineAlignment = StringAlignment.Near;
                                gfx.DrawString(commuteName.ToLower(), font, brush, new RectangleF(0, bmp.Height - 72, bmp.Width + 50, 60), format);
                            }
                            using (Font font = new Font(_FontCollection.Families[0], 22.0f))
                            {
                                format.Alignment = StringAlignment.Near;
                                format.LineAlignment = StringAlignment.Near;
                                gfx.DrawString(routeName.ToLower(), font, brush, new RectangleF(44, 60, bmp.Width, 60), format);
                            }
                            using (Font font = new Font(_FontCollection.Families[0], 24.0f))
                            {
                                format.Alignment = StringAlignment.Center;
                                format.LineAlignment = StringAlignment.Near;
                                gfx.DrawString(timeDuration, font, brush, new RectangleF(bmp.Width - 71, 0, 70, 54), format);
                            }
                            using (Font font = new Font(_FontCollection.Families[0], 10.0f))
                            {
                                format.Alignment = StringAlignment.Center;
                                format.LineAlignment = StringAlignment.Far;
                                gfx.DrawString(timeInterval, font, brush, new RectangleF(bmp.Width - 71, 0, 70, 60), format);
                            }
                            using (Font font = new Font(_FontCollection.Families[0], 10.0f))
                            {
                                format.Alignment = StringAlignment.Far;
                                format.LineAlignment = StringAlignment.Near;
                                gfx.DrawString(dayString, font, brush, new RectangleF(0, bmp.Height - 18, bmp.Width, 20), format);
                            }
                            using (Image light = Bitmap.FromFile(lightImage))
                            {
                                gfx.DrawImage(light, new Rectangle(7, 7, 33, 80));
                            }
                        }
                    }
                    Response.Clear();
                    Response.ClearHeaders();
                    Response.ContentType = "image/png";
                    Response.AddHeader("Content-Type", "image/png");

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        bmp.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                        //memoryStream.WriteTo(Response.OutputStream);

                        Response.BinaryWrite(memoryStream.ToArray());
                        Response.Flush();
                    }
                    Response.End();
                }
            }
        }
    }
}
