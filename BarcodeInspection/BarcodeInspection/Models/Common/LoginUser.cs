using System;
using System.Collections.Generic;
using System.Text;

namespace BarcodeInspection.Models.Common
{
    class LoginUser
    {
        public LoginUser()
        {

        }

        public string Compky { get; set; }
        public string Compnm { get; set; }
        public string Wareky { get; set; }
        public string Warenm { get; set; }
        public string Userid { get; set; }
        public string Passwd { get; set; }
        public string Auth_Token { get; set; }

    }
}
