using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace AtomGroupCore.Controllers
{
    [ApiController]
    public class AtomImagesController : ControllerBase
    {
        [HttpGet]
        [Route("api/atomimages/{*fileName}")]
        //Cache set to one hour - without more info on usage im unsure what the best duration would be for Atom
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "res","ext","bgcolour","text" })]
        public IActionResult GetImage(int res = 0, string filename = "", string ext = "", string bgcolour = "", string text = "")
        {

            var img_path = "/product_images/" + filename;
            img_path = AppDomain.CurrentDomain.GetData("ContentRootPath") + img_path;
            string[] list_valid_extensions = { "jpeg", "gif", "png" };

            //validation
            if (!System.IO.File.Exists(img_path))
            {
                return NotFound("Image not found");
            }
            if (res == 0)
            {
                return NotFound("Invalid or missing resolution");
            }
            if (ext == null || list_valid_extensions.Contains(ext) == false)
            {
                return NotFound("Invalid or missing file extension");
            }

            var image = System.IO.File.OpenRead(img_path);

            using (Bitmap bitmap = (Bitmap)Image.FromFile(img_path))
            {
                MemoryStream ms = new MemoryStream(ConvertImage(bitmap, res, text, bgcolour, ext));
                return new FileStreamResult(ms, new MediaTypeHeaderValue("image/"+ext));
            }

        }

        private static byte[] ConvertImage(Image imageToConvert, int res, string text, string bgcolour, string ext)
        {
            using (var ms = new MemoryStream())
            {

                Bitmap bmp = new Bitmap(imageToConvert);

                //Watermark
                Font font = new Font("Arial", 20);
                Color color = Color.FromName("gray");
                Point point = new Point(bmp.Width / 2, bmp.Height / 2);
                SolidBrush brush = new SolidBrush(color);
                Graphics graphics = Graphics.FromImage(bmp);
                StringFormat string_format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                graphics.DrawString(text, font, brush, point, string_format);
                font.Dispose();
                brush.Dispose();                
                graphics.Dispose();

                //Resolution
                bmp.SetResolution(res, res);

                //background Colour
                //No validation for colour name as any invalid colour results a white background
                if (bgcolour != null)
                {
                    bmp = AddBackground(bmp, Color.FromName(bgcolour));
                }

                switch (ext.ToLower())
                {
                    case "jpeg":
                        bmp.Save(ms, ImageFormat.Jpeg);
                        break;

                    case "png":
                        bmp.Save(ms, ImageFormat.Png);
                        break;

                    case "gif":
                        bmp.Save(ms, ImageFormat.Gif);
                        break;

                }

                bmp.Dispose();

                return ms.ToArray();
            }
        }

        private static Bitmap AddBackground(Bitmap bmp, Color bgcolour)
        {
            Bitmap bmp_new = new Bitmap(bmp.Width, bmp.Height);
            using (Graphics G = Graphics.FromImage(bmp_new))
            {
                G.Clear(bgcolour);
                G.DrawImage(bmp, new Rectangle(Point.Empty, bmp.Size));
            }
            return bmp_new;
        }

    }
}
