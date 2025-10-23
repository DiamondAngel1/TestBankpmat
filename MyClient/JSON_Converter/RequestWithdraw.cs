using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClient.JSON_Converter
{
    public class RequestWithdraw : RequestBase
    {

        public override string Type { get; } = "WITHDRAW"; // Default type for this request
        public Decimal Sum { get; set; } = 0; // Default value for sum
    }
}
