using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace MyClient.JSON_Converter
{
	public class RequestDeposit : RequestBase
	{
		public override string Type { get;} = "DEPOSIT";
		public Decimal Sum { get; set; } = 0;
	}
}
