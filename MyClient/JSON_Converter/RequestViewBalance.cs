using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClient.JSON_Converter
{
	public class RequestViewBalance : RequestBase
	{
		public override string Type { get; } = "VIEW_BALANCE";
	}
}
