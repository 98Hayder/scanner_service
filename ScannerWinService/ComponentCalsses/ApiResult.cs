using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScannerWinService.ComponentCalsses
{
   public class ApiResult
    {
        public object Data { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public Boolean IsError { get; set; }
    }
}
