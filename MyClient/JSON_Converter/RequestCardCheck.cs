using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClient.JSON_Converter
{
    public class RequestCardCheck : RequestBase
    {
        public override string Type { get; } = "CARD_CHECK"; // Default type for this request
        public long NumberCard { get; set; } = 0; // Default value for card number
    }
}
