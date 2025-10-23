using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MyClient.JSON_Converter;
using MyClient;


Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

int port = 5000;
string serverIp = IPAddress.Loopback.ToString();
//string serverIp = "18.185.184.246";
TcpClient myClient = new TcpClient();

try
{
	await myClient.ConnectAsync(IPAddress.Parse(serverIp), port);
    using NetworkStream networkStream = myClient.GetStream();

	Console.WriteLine("Ви підключились до банкомату. ");
	var options = new JsonSerializerOptions();
	options.Converters.Add(new RequestBaseConverter());

    Console.Write("Бажаєте увійти чи зареєструватись? (login / register): ");
    string mode = Console.ReadLine()?.ToLower();

    long cardNumber = 0;
    ServerResponse? response1 = null;

    if (mode == "login")
    {
        Console.Write("Введіть номер картки: ");
        if (!long.TryParse(Console.ReadLine(), out cardNumber))
        {
            Console.WriteLine("Некоректний номер.");
            return;
        }

        var check = new RequestCardCheck { NumberCard = cardNumber };
        response1 = await RequestAsync(networkStream, check, options);

        if (response1?.PassCode == 1789)
        {
            Console.WriteLine("Картку не знайдено. Зареєструватись? (yes / no): ");
            string res = Console.ReadLine();
            if (res?.ToLower() != "yes") return;
            mode = "register";
        }
        else if (response1?.PassCode != 1945)
        {
            Console.WriteLine("Сервер відповів помилкою або заблокував вас.");
            return;
        }
    }
    if (mode == "register")
    {
        var rnd = new Random();
        int a = 10000000, b = 99999999;
        do
        {
            cardNumber = long.Parse($"{rnd.Next(a, b)}{rnd.Next(a, b)}");
            response1 = await RequestAsync(networkStream, new RequestCardCheck { NumberCard = cardNumber }, options);
        }
        while (response1?.PassCode == 1945);

        Console.WriteLine($"Ваш новий номер картки: {cardNumber} обовязково запишіть його");

        Console.Write("Ім’я: ");
        string fname = Console.ReadLine();
        Console.Write("Прізвище: ");
        string lname = Console.ReadLine();
        Console.Write("По-батькові: ");
        string patr = Console.ReadLine();
        Console.Write("PIN-код: ");
        if (!long.TryParse(Console.ReadLine(), out long pin))
        {
            Console.WriteLine("Невірний PIN.");
            return;
        }

        var register = new RequestAuthOrReg
        {
            FirstName = fname,
            LastName = lname,
            FatherName = patr,
            PinCode = pin
        };
        var regResp = await RequestAsync(networkStream, register, options);
        PrintResponse(regResp);
        if (regResp?.PassCode != 1945) return;
    }

    // Авторизація
    for (int attempt = 1; attempt <= 3; attempt++)
    {
        Console.Write("Ім’я: ");
        string fname = Console.ReadLine();
        Console.Write("Прізвище: ");
        string lname = Console.ReadLine();
        Console.Write("По-батькові: ");
        string patr = Console.ReadLine();
        Console.Write("PIN-код: ");
        if (!long.TryParse(Console.ReadLine(), out long pin))
        {
            Console.WriteLine("Невірний PIN.");
            return;
        }

        var auth = new RequestAuthOrReg
        {
            FirstName = fname,
            LastName = lname,
            FatherName = patr,
            PinCode = pin
        };
        var authResp = await RequestAsync(networkStream, auth, options);

        if (authResp?.PassCode == 1945)
        {
            Console.WriteLine("Авторизація успішна.");
            break;
        }
        else if (authResp?.PassCode == 1918 || authResp?.PassCode == 1914)
        {
            Console.WriteLine("Вас заблоковано сервером.");
            return;
        }
        else if (attempt == 3)
        {
            Console.WriteLine("Вичерпано спроби.");
            return;
        }
        else
        {
            Console.WriteLine("Дані невірні. Спроба #" + attempt);
        }
    }


    while (true)
	{
		Console.WriteLine("\nОберіть запит банкомату:");
		Console.WriteLine("1 - Зняти кошти");
		Console.WriteLine("2 - Поповнити рахунок");
		Console.WriteLine("3 - Переглянути баланс");
		Console.WriteLine("4 - Вихід");
		Console.Write("->_ ");
		string choice = Console.ReadLine();

		if (choice == "4") break;

		else if (choice == "1")
		{
			Console.Write("Сума зняття: ");
			if (decimal.TryParse(Console.ReadLine(), out decimal sum))
			{
				var request = new RequestWithdraw { Sum = sum };
				var resp = await RequestAsync(networkStream, request, options);
				PrintResponse(resp);
			}
			else Console.WriteLine("Невірна сума.");
		}
		else if (choice == "2")
		{
			Console.Write("Сума поповнення: ");
			if (decimal.TryParse(Console.ReadLine(), out decimal sum))
			{
				var request = new RequestDeposit { Sum = sum };
				var resp = await RequestAsync(networkStream, request, options);
				PrintResponse(resp);
			}
			else Console.WriteLine("Невірна сума.");
		}
		else if (choice == "3")
		{
			var request = new RequestViewBalance();
			var resp = await RequestAsync(networkStream, request, options);
			PrintResponse(resp);
		}
		else
		{
			Console.WriteLine("Ви ввели невірну команду. Спробуйте ще раз");
		}


	}
}
catch (Exception ex)
{
	Console.WriteLine($"Помилка з'єднання: {ex.Message}");
}
finally
{
	myClient.Close();
	Console.WriteLine("З'єднання з банкоматом завершено.");
}

static async Task<ServerResponse> RequestAsync(NetworkStream stream, RequestBase request, JsonSerializerOptions options)
{
	string jsonrequest = JsonSerializer.Serialize(request, options);
	byte[] data = Encoding.UTF8.GetBytes(jsonrequest);
	await stream.WriteAsync(data, 0, data.Length);
	await stream.FlushAsync();

	byte[] buffer = new byte[4096];
	int bytesread = await stream.ReadAsync(buffer, 0, buffer.Length);
	string jsonresponse = Encoding.UTF8.GetString(buffer, 0, bytesread);
	try
	{
		return JsonSerializer.Deserialize<ServerResponse>(jsonresponse);
	}
	catch
	{
		return null;
	}
}
static void PrintResponse(ServerResponse? response)
{
	if (response == null)
	{
		Console.WriteLine("Банкомат не надіслав відповідь.");
		return;
	}

	Console.WriteLine($"\n{response.Comment}");
	switch (response.PassCode)
	{
		case 1945:
			Console.WriteLine("Операція успішна.");
			break;
		case 1939:
			Console.WriteLine("Операція неуспішна.");
			break;
		case 1918:
			Console.WriteLine("Вас забанено за несанкціонований доступ.");
			break;
		case 1914:
			Console.WriteLine("Вас забанено за порушення послідовності авторизації.");
			break;
		case 1789:
			Console.WriteLine("В базі даних немає такого номеру картки");
			break;
        case 1111:
            Console.WriteLine("Cума поповнення перевищує ліміт 100 000 ₴");
            break;
        default:
			Console.WriteLine("Банкомат надіслав невідомий код відповіді.");
			break;
	}
}