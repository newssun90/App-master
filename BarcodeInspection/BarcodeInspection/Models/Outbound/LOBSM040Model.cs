using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BarcodeInspection.Models.Outbound
{
    public class LOBSM040Model
    {
        [JsonProperty("slipno")]
        public string Slipno { get; set; }

        [JsonProperty("lbbrcd")]
        public string Lbbrcd { get; set; }

        [JsonProperty("prodcd")]
        public string Prodcd { get; set; }

        [JsonProperty("prodnm")]
        public string Prodnm { get; set; }

        [JsonProperty("ordqty")]
        public string Ordqty { get; set; }

        [JsonProperty("dlvycd")]
        public string Dlvycd { get; set; }

        [JsonProperty("dlvynm")]
        public string Dlvynm { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

    }
}
