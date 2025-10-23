using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClient.JSON_Converter
{
    public class RequestResponseMessage : RequestBase
    {
        public override string Type { get;} = "MESSAGE"; // Default type for this request
        public string Comment { get; set; } = string.Empty; // Default comment for type 0 requests

        public Int16 PassCode { get; set; } = 0; // Default passcode for type 0 requests//1945 - GOOD//1939 - BAD
    }
}
