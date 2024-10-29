using OpenCvSharp.CPlusPlus;
using ScannerWinService.ComponentCalsses;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors; 
using WIA;

namespace ScannerWinService.ScannerApi
{
    public class ScanApiController : ApiController
    {

        public List<ScannerProprtyArray> IlistArrayImages = new List<ScannerProprtyArray>();

        [EnableCors(origins: "*", headers: "*", methods: "*")]
        [System.Web.Http.HttpGet]

        public HttpResponseMessage ScannerPrint(string ScannerName)
        {
            string Imagebase64String = string.Empty; 

            ApiResult oApi = new ApiResult();
            try
            {
                // Create a DeviceManager instance
                var deviceManager = new DeviceManager();

                // Create an empty variable to store the scanner instance
                DeviceInfo firstScannerAvailable = null;

                //Device Not Found
                if (deviceManager.DeviceInfos.Count == 0)
                {
                    oApi.ErrorMessage = ConstatntERROR.DEVICE_NOT_FOUND_MSG;
                    oApi.ErrorCode = ConstatntERROR.DEVICE_NOTFOUND_CODE;
                    oApi.IsError = true;
                    return Request.CreateResponse(HttpStatusCode.OK, oApi, Configuration.Formatters.JsonFormatter);
                }

                foreach (DeviceInfo deviceInfo in deviceManager.DeviceInfos)
                {
                    if (deviceInfo.Type == WiaDeviceType.ScannerDeviceType && deviceInfo.Properties["Name"].get_Value().ToString() == ScannerName)
                    {
                        firstScannerAvailable = deviceInfo;
                        break;
                    }
                }
               

                // Connect to the first available scanner
                var device = firstScannerAvailable.Connect();

                // Select the scanner
                var scannerItem = device.Items[1];

                // Retrieve a image in JPEG format and store it into a variable
                var imageFile = (ImageFile)scannerItem.Transfer(FormatID.wiaFormatJPEG);

                // Save the image in some path with filename
                var imageBytes = (byte[])imageFile.FileData.get_BinaryData();
                 if(imageBytes != null)
                {
                    var NewImageCompress = compressImage(byteArrayToImage(imageBytes), ConstatntERROR.IMAGE_WIDTH, ConstatntERROR.IMAGE_HEIGHT, ConstatntERROR.IMAGE_QUALITY);


                    using (MemoryStream m = new MemoryStream())
                    {
                        NewImageCompress.Save(m, NewImageCompress.RawFormat);
                        byte[] imageBytesResult = m.ToArray();

                        // Convert byte[] to Base64 String
                        Imagebase64String = Convert.ToBase64String(imageBytesResult);

                    }


                }


            }
            catch (Exception ex)
            {
                oApi.ErrorCode = ConstatntERROR.ErrorExpstion;
                oApi.ErrorMessage = ConstatntERROR.ErrorExpstion_MSG;
                oApi.IsError = true;
                Logger.WriteLog(ex.Message);
                return Request.CreateResponse(HttpStatusCode.OK, oApi, Configuration.Formatters.JsonFormatter);
            }

            oApi.Data = Imagebase64String;
            oApi.ErrorMessage = ConstatntERROR.SUCESS_SCAN_MSG;
            oApi.ErrorCode = ConstatntERROR.SUCESS_SCAN_CODE;
            oApi.IsError = false;

            return Request.CreateResponse(HttpStatusCode.OK, oApi, Configuration.Formatters.JsonFormatter);


        }


        //private static Bitmap CropBackgroundColor(Bitmap bmp)
        //{
        //    // الحصول على لون الخلفية من الزاوية العلوية اليسرى للصورة
        //    Color backgroundColor = bmp.GetPixel(0, 0);

        //    int xMin = bmp.Width, xMax = 0, yMin = bmp.Height, yMax = 0;
        //    int tolerance = 20; // تخفيض التفاوت لزيادة الدقة
        //    int edgeMargin = 15; // زيادة الهامش للحفاظ على الحواف

        //    for (int y = 0; y < bmp.Height; y++)
        //    {
        //        for (int x = 0; x < bmp.Width; x++)
        //        {
        //            Color color = bmp.GetPixel(x, y);

        //            // التحقق من الفرق بين اللون الحالي ولون الخلفية
        //            if (Math.Abs(color.R - backgroundColor.R) > tolerance ||
        //                Math.Abs(color.G - backgroundColor.G) > tolerance ||
        //                Math.Abs(color.B - backgroundColor.B) > tolerance)
        //            {
        //                if (x < xMin) xMin = x;
        //                if (x > xMax) xMax = x;
        //                if (y < yMin) yMin = y;
        //                if (y > yMax) yMax = y;
        //            }
        //        }
        //    }

        //    // إضافة هامش لعدم قص الحواف بشكل زائد
        //    xMin = Math.Max(0, xMin - edgeMargin);
        //    yMin = Math.Max(0, yMin - edgeMargin);
        //    xMax = Math.Min(bmp.Width - 1, xMax + edgeMargin);
        //    yMax = Math.Min(bmp.Height - 1, yMax + edgeMargin);

        //    // التأكد من اقتصاص الصورة بناءً على الحدود الجديدة
        //    if (xMax > xMin && yMax > yMin)
        //    {
        //        Rectangle cropArea = new Rectangle(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
        //        return bmp.Clone(cropArea, bmp.PixelFormat);
        //    }

        //    return bmp;
        //}

        private static Bitmap CropBackgroundColor(Bitmap bmp)
        {
            // تحديد لون الخلفية من الزاوية العلوية اليسرى
            Color backgroundColor = bmp.GetPixel(0, 0);

            int xMin = bmp.Width, xMax = 0, yMin = bmp.Height, yMax = 0;
            int tolerance = 15; // تفاوت أقل للحفاظ على الدقة

            // التحقق من حواف الصورة وتحديد الإطار الخارجي للورقة فقط
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color color = bmp.GetPixel(x, y);

                    // مقارنة اللون الحالي بلون الخلفية للتأكد من تطابق اللون مع الخلفية
                    if (Math.Abs(color.R - backgroundColor.R) > tolerance ||
                        Math.Abs(color.G - backgroundColor.G) > tolerance ||
                        Math.Abs(color.B - backgroundColor.B) > tolerance)
                    {
                        // تحديث الحدود عند العثور على لون مختلف عن الخلفية
                        if (x < xMin) xMin = x;
                        if (x > xMax) xMax = x;
                        if (y < yMin) yMin = y;
                        if (y > yMax) yMax = y;
                    }
                }
            }

            // إضافة هامش صغير لتجنب القطع المفرط للحواف
            int margin = 5;
            xMin = Math.Max(0, xMin - margin);
            yMin = Math.Max(0, yMin - margin);
            xMax = Math.Min(bmp.Width - 1, xMax + margin);
            yMax = Math.Min(bmp.Height - 1, yMax + margin);

            // اقتصاص الصورة حسب الإطار الجديد
            if (xMax > xMin && yMax > yMin)
            {
                Rectangle cropArea = new Rectangle(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
                return bmp.Clone(cropArea, bmp.PixelFormat);
            }

            // في حال لم يتم العثور على حواف مميزة، تُعاد الصورة الأصلية
            return bmp;
        }


         
[EnableCors(origins: "*", headers: "*", methods: "*")]
    [System.Web.Http.HttpGet]
    public HttpResponseMessage ScannerFeederPrint(string ScannerName)
    {
        string Imagebase64String = string.Empty;
        int IdScan = 0;
        ApiResult oApi = new ApiResult();

        try
        {
            // Create a DeviceManager instance
            var deviceManager = new DeviceManager();

            // Create an empty variable to store the scanner instance
            DeviceInfo firstScannerAvailable = null;

            // Device Not Found
            if (deviceManager.DeviceInfos.Count == 0)
            {
                oApi.ErrorMessage = ConstatntERROR.DEVICE_NOT_FOUND_MSG;
                oApi.ErrorCode = ConstatntERROR.DEVICE_NOTFOUND_CODE;
                oApi.IsError = true;
                return Request.CreateResponse(HttpStatusCode.OK, oApi, Configuration.Formatters.JsonFormatter);
            }

            // Loop through the list of devices to choose the first available
            foreach (DeviceInfo deviceInfo in deviceManager.DeviceInfos)
            {
                if (deviceInfo.Type == WiaDeviceType.ScannerDeviceType && deviceInfo.Properties["Name"].get_Value().ToString() == ScannerName)
                {
                    firstScannerAvailable = deviceInfo;
                    break;
                }
            }

            // Connect to the first available scanner
            var device = firstScannerAvailable.Connect();

            foreach (Property prop in device.Properties)
            {
                // Activate feeder if supported
                if (prop.PropertyID == 3088) // Property ID for document handling select
                {
                    prop.set_Value(1); // Set feeder option (Assuming 1 represents the feeder)
                }
            }

            // Select the scanner
            var scannerItem = device.Items[1];

            // Loop to scan documents until the feeder is empty
            while (true)
            {
                try
                {
                    // Attempt to retrieve a new image from the feeder
                    var imageFile = (ImageFile)scannerItem.Transfer(FormatID.wiaFormatJPEG);

                    // Save the image to a byte array
                    var imageBytes = (byte[])imageFile.FileData.get_BinaryData();
                    if (imageBytes != null)
                    {
                        using (MemoryStream ms = new MemoryStream(imageBytes))
                        using (Bitmap originalImage = new Bitmap(ms))
                        {
                            // قص المساحات البيضاء من الصورة
                            Rectangle cropArea = GetCropArea(originalImage);
                            using (Bitmap croppedImage = originalImage.Clone(cropArea, originalImage.PixelFormat))
                            using (MemoryStream croppedMs = new MemoryStream())
                            {
                                croppedImage.Save(croppedMs, ImageFormat.Png);
                                byte[] croppedImageBytes = croppedMs.ToArray();

                                // تحويل الصورة المقتصة إلى Base64
                                Imagebase64String = Convert.ToBase64String(croppedImageBytes);

                                IlistArrayImages.Add(new ScannerProprtyArray
                                {
                                    id = IdScan++,
                                    image = Imagebase64String,
                                    zoomed = false,
                                    selected = false
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle the exception when no more documents are available in the feeder
                    if (IlistArrayImages.Count > 0)
                    {
                        oApi.Data = IlistArrayImages;
                        oApi.ErrorMessage = ConstatntERROR.SUCESS_SCAN_MSG;
                        oApi.ErrorCode = ConstatntERROR.SUCESS_SCAN_CODE;
                        oApi.IsError = false;

                        return Request.CreateResponse(HttpStatusCode.OK, oApi, Configuration.Formatters.JsonFormatter);
                    }
                    else
                    {
                        oApi.Data = null;
                        oApi.ErrorMessage = ConstatntERROR.ErrorNoMOrePaper_MSG;
                        oApi.ErrorCode = ConstatntERROR.ErrorExpstion;
                        oApi.IsError = true;
                        return Request.CreateResponse(HttpStatusCode.OK, oApi, Configuration.Formatters.JsonFormatter);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            oApi.ErrorCode = ConstatntERROR.ErrorExpstion;
            oApi.ErrorMessage = ConstatntERROR.ErrorExpstion_MSG;
            oApi.IsError = true;
            Logger.WriteLog(ex.Message);
            return Request.CreateResponse(HttpStatusCode.OK, oApi, Configuration.Formatters.JsonFormatter);
        }

        oApi.Data = IlistArrayImages;
        oApi.ErrorMessage = ConstatntERROR.SUCESS_SCAN_MSG;
        oApi.ErrorCode = ConstatntERROR.SUCESS_SCAN_CODE;
        oApi.IsError = false;

        return Request.CreateResponse(HttpStatusCode.OK, "", Configuration.Formatters.JsonFormatter);
    }

    // دالة لتحديد منطقة القص
    static Rectangle GetCropArea(Bitmap image)
    {
        int minX = image.Width, minY = image.Height, maxX = 0, maxY = 0;

        // البحث عن الحواف غير البيضاء
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Color pixelColor = image.GetPixel(x, y);
                if (!IsWhite(pixelColor)) // إذا لم يكن البكسل أبيض
                {
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }
        }

        // تحديد منطقة القص
        return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    // دالة للتحقق مما إذا كان البكسل أبيض (مع السماح بهامش صغير)
    static bool IsWhite(Color color)
    {
        return color.R > 240 && color.G > 240 && color.B > 240;
    }







    [EnableCors(origins: "*", headers: "*", methods: "*")]
        [System.Web.Http.HttpGet]
        public HttpResponseMessage GetScanners()
        {
            ApiResult oApi = new ApiResult();

            var deviceManager = new DeviceManager();
            var scannerNames = new List<string>();

            foreach (DeviceInfo deviceInfo in deviceManager.DeviceInfos)
            {
                if (deviceInfo.Type == WiaDeviceType.ScannerDeviceType) // تحقق من أن الجهاز هو ماسح ضوئي
                {
                    scannerNames.Add(deviceInfo.Properties["Name"].get_Value().ToString());
                }
            }

            oApi.Data = scannerNames;
            oApi.ErrorMessage = "Getting Scanner Name List";
            oApi.IsError = false;

            return Request.CreateResponse(HttpStatusCode.OK, oApi, Configuration.Formatters.JsonFormatter);
        }


        public Image byteArrayToImage(byte[] bytesArr)
        {
            using (MemoryStream memstr = new MemoryStream(bytesArr))
            {
                Image img = Image.FromStream(memstr);
                return img;
            }
        }


        public Image compressImage(Image imagePar, int newWidth, int newHeight,
                            int newQuality)   // set quality to 1-100, eg 50
        {
            using (Image image = imagePar)
            using (Image memImage = new Bitmap(image, newWidth, newHeight))
            {
                ImageCodecInfo myImageCodecInfo;
                System.Drawing.Imaging.Encoder myEncoder;
                EncoderParameter myEncoderParameter;
                EncoderParameters myEncoderParameters;
                myImageCodecInfo = GetEncoderInfo("image/jpeg");
                myEncoder = System.Drawing.Imaging.Encoder.Quality;
                myEncoderParameters = new EncoderParameters(1);
                myEncoderParameter = new EncoderParameter(myEncoder, newQuality);
                myEncoderParameters.Param[0] = myEncoderParameter;

                MemoryStream memStream = new MemoryStream();
                memImage.Save(memStream, myImageCodecInfo, myEncoderParameters);
                Image newImage = Image.FromStream(memStream);
                ImageAttributes imageAttributes = new ImageAttributes();
                using (Graphics g = Graphics.FromImage(newImage))
                {
                    g.InterpolationMode =
                      System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;  //**
                    g.DrawImage(newImage, new Rectangle(Point.Empty, newImage.Size), 0, 0,
                      newImage.Width, newImage.Height, GraphicsUnit.Pixel, imageAttributes);
                }
                return newImage;
            }
        }
   
        public static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo ici in encoders)
                if (ici.MimeType == mimeType) return ici;

            return null;
        }


        public static string Compress(string uncompressedString)
        {
            byte[] compressedBytes;

            using (var uncompressedStream = new MemoryStream(Encoding.UTF8.GetBytes(uncompressedString)))
            {
                using (var compressedStream = new MemoryStream())
                {
                    // setting the leaveOpen parameter to true to ensure that compressedStream will not be closed when compressorStream is disposed
                    // this allows compressorStream to close and flush its buffers to compressedStream and guarantees that compressedStream.ToArray() can be called afterward
                    // although MSDN documentation states that ToArray() can be called on a closed MemoryStream, I don't want to rely on that very odd behavior should it ever change
                    using (var compressorStream = new DeflateStream(compressedStream, CompressionLevel.Fastest, true))
                    {
                        uncompressedStream.CopyTo(compressorStream);
                    }

                    // call compressedStream.ToArray() after the enclosing DeflateStream has closed and flushed its buffer to compressedStream
                    compressedBytes = compressedStream.ToArray();
                }
            }

            return Convert.ToBase64String(compressedBytes);
        }


        public static string Decompress(string compressedString)
        {
            byte[] decompressedBytes;

            var compressedStream = new MemoryStream(Convert.FromBase64String(compressedString));

            using (var decompressorStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
            {
                using (var decompressedStream = new MemoryStream())
                {
                    decompressorStream.CopyTo(decompressedStream);

                    decompressedBytes = decompressedStream.ToArray();
                }
            }

            return Encoding.UTF8.GetString(decompressedBytes);
        }
    }
}
