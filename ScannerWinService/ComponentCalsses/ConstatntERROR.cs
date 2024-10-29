using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScannerWinService.ComponentCalsses
{
   public class ConstatntERROR
    { 
        /// <summary>
        /// ERROR MESSAGE
        /// </summary>

        public const string DEVICE_NOT_FOUND_MSG = "الماسح الضوئي غير متصل بجهاز الكمبيوتر";
        public const string SUCESS_SCAN_MSG = "تمت عملية المسح الضوئي بنجاح";
        public const string ErrorExpstion_MSG = "حصل خطا بالنظام ، يرجى مراجعة مدير النطام";
        public const string ErrorNoMOrePaper_MSG = "لقد نفذ ورق المغذي ،الرجاء وضع الورق والمحاولة مرى اخرى";


        /// <summary>
        /// ERRRO CODE
        /// </summary>
        public const string ErrorExpstion = "100";
        public const string DEVICE_NOTFOUND_CODE = "101";
        public const string SUCESS_SCAN_CODE = "200";

        // Configration Image
        public const int IMAGE_HEIGHT = 3000;
        public const int IMAGE_WIDTH = 3000;
        public const int IMAGE_QUALITY = 100;

    }
}
