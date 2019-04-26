using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace BarcodeInspection.Models.Outbound
{
    public class LOBSM030Model
    {
        [JsonProperty("compky")]
        public string Compky { get; set; }

        [JsonProperty("wareky")]
        public string Wareky { get; set; }

        [JsonProperty("rqshpd")]
        public string Rqshpd { get; set; }

        [JsonProperty("dlwrky")]
        public string Dlwrky { get; set; }

        [JsonProperty("ruteky")]
        public string Ruteky { get; set; }

        [JsonProperty("dlvycd")]
        public string Dlvycd { get; set; }

        [JsonProperty("dlvynm")]
        public string Dlvynm { get; set; }

        [JsonProperty("lbl_count")]
        public string LblCount { get; set; }

    }
}
