using MyClient.JSON_Converter;
using MyPrivate.Data;
using MyPrivate.Data.Entitys;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
int port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 5000;
//int port = 5000;
TcpListener tcpListener = new TcpListener(IPAddress.Any, port); 
const int MinIntervalMs = 5000; 
const int MaxConcurrentClients = 100; 
ConcurrentDictionary<IPEndPoint, DateTime> clientLastAccess = new(); 
ConcurrentBag<IPEndPoint> bannedClients = new(); 
SemaphoreSlim semaphoreSlim = new SemaphoreSlim(MaxConcurrentClients);
IPEndPoint iP;
DateTime now;
tcpListener.Start();
TcpClient client;
Console.WriteLine($"Сервер запущено на {tcpListener.LocalEndpoint}. Очікуємо клієнтів...");
while (true)
{
    try
    {
        await semaphoreSlim.WaitAsync();

        client = await tcpListener.AcceptTcpClientAsync(); 

        
        iP = client.Client.RemoteEndPoint as IPEndPoint; 
        if (iP == null)
        {
            Console.WriteLine("Не вдалося отримати IP-адресу клієнта.");
            client.Close();
            semaphoreSlim.Release(); 
            continue; 
        }
        else if (bannedClients.Contains(iP)) 
        {
            Console.WriteLine($"Спроба підключення від заблокованого клієнта: {iP}");
            client.Close();
            semaphoreSlim.Release();
            continue; 
        }
        if (clientLastAccess.ContainsKey(iP))
        {
            now = DateTime.UtcNow;

            if (clientLastAccess.TryGetValue(iP, out DateTime lastAccess))
            {

                if ((now - lastAccess).TotalMilliseconds < MinIntervalMs)
                {

                    Console.WriteLine("Спробу підключення відхилено через обмеження мінімального інтервалу.");

                    client.Close();

                    semaphoreSlim.Release(); 

                    continue; 
                }

            }

        }
        else
        {
            clientLastAccess.TryAdd(iP, DateTime.UtcNow); 

        }
        _ = HandleClientAsync(client).ContinueWith(_ => semaphoreSlim.Release()); 
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Помилка: {ex.Message}");
    }
}
async Task HandleClientAsync(TcpClient client)
{

    using NetworkStream networkStream = client.GetStream();

    ContextATM context;
    try
    {
        context = new ContextATM();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Помилка підключення до бази: {ex.Message}");
        await networkStream.WriteAsync(Encoding.UTF8.GetBytes("DB error"));
        return;
    }

    var json_options = new System.Text.Json.JsonSerializerOptions();
    json_options.Converters.Add(new MyClient.JSON_Converter.RequestBaseConverter());
    int tryes = 0;
    MyClient.JSON_Converter.RequestBase? request = null;

    try
    {
        Console.WriteLine("Client connected: " + client.Client.RemoteEndPoint);

        UserEntity? user = null;
        bool isAuthenticated = false; 
        long currentcardnumber = 0;
        while (true)
        {
            byte[] buffer = new byte[4096];
            int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                Console.WriteLine("Клієнт відключився: " + client.Client.RemoteEndPoint);
                break;
            }
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Array.Clear(buffer, 0, buffer.Length);
            Console.WriteLine($"Отримано повідомлення від користувача {client.Client.RemoteEndPoint}: {message}");
            if (!message.TrimStart().StartsWith("{"))
            {
                Console.WriteLine("Непідтримуваний запит (не JSON), ігноруємо.");
                client.Close();
                return;
            }
            request = System.Text.Json.JsonSerializer.Deserialize<RequestBase>(message, json_options);
            

            if (request == null)
            {
                Console.WriteLine("Отримано нульовий запит, обробка пропущена.");
                continue;
            }
            else
            {
                if (request is RequestCardCheck request1)
                {
                    currentcardnumber = request1.NumberCard;
                    user = context.Users.FirstOrDefault(u => u.CardNumber == currentcardnumber);
                    if (user != null)
                    {
                        var response = new RequestResponseMessage
                        {
                            Comment = "Номер картки існує. Будь ласка, введіть PIB та PIN-код",
                            PassCode = 1945
                        };
                        byte[] responseBuffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response, json_options));
                        await networkStream.WriteAsync(responseBuffer);
                    }
                    else
                    {
                        var response = new RequestResponseMessage
                        {
                            Comment = "Такої карти не існує",
                            PassCode = 1789
                        };
                        byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                        await networkStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                    }
                }
                else if (request is RequestAuthOrReg request2)
                {
                    if (user != null)
                    {

                        Console.WriteLine($"Обробка авторизації для користувача: {user.FirstName} {user.LastName}");
                        if ((user.FirstName.Equals(request2.FirstName)) && (user.FatherName.Equals(request2.FatherName)) && (user.LastName.Equals(request2.LastName) && (user.PinCode == request2.PinCode)))
                        {
                            isAuthenticated = true;
                            var response = new RequestResponseMessage
                            {
                                Comment = "Авторизація успішна.",
                                PassCode = 1945
                            };
                            byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                            await networkStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                        }
                        else
                        {
                            tryes++;
                            if (tryes >= 3)
                            {
                                bannedClients.Add(client.Client.RemoteEndPoint as IPEndPoint);
                                Console.WriteLine($"Клієнт {client.Client.RemoteEndPoint as IPEndPoint} заблокований через велику кількість невдалих спроб автентифікації.");
                                var response = new RequestResponseMessage
                                {
                                    Comment = "Вас забанили через велику кількість невдалих спроб автентифікації.",
                                    PassCode = 1918
                                };
                                byte[] responseBuffer1 = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                                await networkStream.WriteAsync(responseBuffer1, 0, responseBuffer1.Length);
                                break;
                            }
                            else
                            {
                                var response = new RequestResponseMessage
                                {
                                    Comment = "Помилка автентифікації",
                                    PassCode = 1939
                                };
                                byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                                await networkStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                            }
                        }
                    }
                    else
                    {
                        var new_user = new UserEntity
                        {
                            CardNumber = currentcardnumber,
                            FirstName = request2.FirstName,
                            LastName = request2.LastName,
                            FatherName = request2.FatherName,
                            PinCode = request2.PinCode,

                        };
                        context.Users.Add(new_user);
                        context.SaveChanges();
                        var balance = new BalanceEntity
                        {
                            UserId = new_user.Id,
                            Amount = 0
                        };
                        context.Balances.Add(balance);
                        context.SaveChanges();

                        user = new_user;
                        isAuthenticated = true;

                        var response = new RequestResponseMessage
                        {
                            Comment = "Користувача зареєстровано успішно",
                            PassCode = 1945
                        };
                        byte[] responseBuffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response, json_options));
                        await networkStream.WriteAsync(responseBuffer);
                    }
                }
                else if (request is RequestWithdraw request3)
                {
                    if (user != null && isAuthenticated == true)
                    {
                        var balance = context.Balances.FirstOrDefault(c => c.UserId == user.Id);
                        if (balance != null)
                        {
                            if (balance.Amount > 0 && balance.Amount > request3.Sum)
                            {
                                balance.Amount -= request3.Sum;
                                context.SaveChanges();
                                var response = new RequestResponseMessage
                                {
                                    Comment = "Транзакція успішна",
                                    PassCode = 1945
                                };
                                byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                                await networkStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                            }
                            else
                            {
                                var response = new RequestResponseMessage
                                {
                                    Comment = "Недостатньо коштів на рахунку",
                                    PassCode = 1939
                                };
                                byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                                await networkStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                            }
                        }
                    }
                }
                else if (request is RequestDeposit request4)
                {
                    if (user != null && isAuthenticated == true)
                    {
                        var balance = context.Balances.FirstOrDefault(c => c.UserId == user.Id);
                        if (balance != null)
                        {
                            if (request4.Sum > 100000)
                            {
                                var response = new RequestResponseMessage
                                {
                                    PassCode = 1111,
                                    Comment = "Сума поповнення перевищує ліміт 100 000 ₴"
                                };
                                byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                                await networkStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                            }
                            else
                            {
                                var response = new RequestResponseMessage
                                {
                                    Comment = "Депозит успішний.",
                                    PassCode = 1945
                                };
                                balance.Amount += request4.Sum;
                                context.SaveChanges();
                                byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                                await networkStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                                
                            }

                                
                            
                        }
                    }
                }
                else if (request is RequestViewBalance request5)
                {
                    if (user != null && isAuthenticated == true)
                    {
                        var balance = context.Balances.FirstOrDefault(c => c.UserId == user.Id);
                        if (balance != null)
                        {
                            var response = new RequestResponseMessage
                            {
                                Comment = $"На вашому рахунку: {balance.Amount}",
                                PassCode = 1945
                            };
                            byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                            await networkStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Невідомий тип запиту: {request.Type}");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Помилка клієнта: {ex.Message}");
    }
    finally
    {
        client.Close();
        Console.WriteLine($"Клієнт {client.Client.RemoteEndPoint} відєднався");
    }
}