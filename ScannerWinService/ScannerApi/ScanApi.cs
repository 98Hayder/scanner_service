using ScannerWinService.ComponentCalsses;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        [System.Web.Http.HttpGet]

        public HttpResponseMessage ScannerPrint()
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

                // Loop through the list of devices to choose the first available
                for (int i = 1; i <= deviceManager.DeviceInfos.Count; i++)
                {
                    // Skip the device if it's not a scanner
                    if (deviceManager.DeviceInfos[i].Type != WiaDeviceType.ScannerDeviceType)
                    {
                        continue;
                    }

                    firstScannerAvailable = deviceManager.DeviceInfos[i];

                    break;
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
