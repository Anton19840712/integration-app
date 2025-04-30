using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
	const int Port = 5018;

	static readonly string Message = "{\"globalId\":\"2e910bd5e1724367891d06053e537727\",\"nEmergencyCardId\":\"123123456\",\"dtCreate\":\"2024-11-26T18:31:01+03:00\",\"nCallTypeId\":\"5\",\"nCardSyntheticState\":\"2\",\"nCard01SyntheticState\":\"2\",\"nCard02SyntheticState\":\"2\",\"nCard03SyntheticState\":\"2\",\"nCard04SyntheticState\":\"2\",\"nCardCommServSyntheticState\":\"6\",\"nCardATSyntheticState\":\"2\",\"lWithCall\":\"0\",\"strCreator\":\"Оператор112(2111209)\",\"strAddressLevel1\":\"Санкт-Петербургг\",\"strAddressLevel2\":\"Санкт-Петербургг\",\"strStreet\":\"ЗАГОРОДНЫЙпроспект\",\"strAddressString\":\"Санкт-Петербургг,Санкт-ПетербурггПерекресток:10-ЯКРАСНОАРМЕЙСКАЯулица,ЗАГОРОДНЫЙпроспект\",\"strBuilding\":\"52\",\"strRoom\":\"113\",\"strAdditionalLocationInfo\":\"ТочноНаВатутина\",\"strEntrance\":\"4\",\"strEntranceCode\":\"552\",\"strStoreys\":\"5\",\"nFloor\":\"3\",\"strIncidentDescription\":\"ТестНовыхПолей\",\"nIncidentTypeID\":\"78\",\"strIncidentType\":\"Драка\",\"strCallerContactPhone\":\"89319999174\",\"strDeclarantName\":\"Александр\",\"strDeclarantLastName\":\"Липанов\",\"strDeclarantMiddleName\":\"Игоревич\",\"strDeclarantBuildingNumber\":\"3\",\"strDeclarantAddressString\":\"Санкт-Петербургг,Песочныйп,10-Йквартал\",\"strDeclarantAddressLevel1\":\"Санкт-Петербургг\",\"strDeclarantAddressLevel2\":\"Песочныйп\",\"strDeclarantStreet\":\"10-Йквартал\",\"strDeclarantAdditionalLocationInfo\":\"ОнТочноВидел\",\"strDeclarantCorps\":\"1\",\"strDeclarantFlat\":\"22\",\"geoLatitude\":\"59.92057\",\"geoLongitude\":\"30.32978\",\"declarantGeoLatitude\":\"60.12391\",\"declarantGeoLongitude\":\"30.16912\",\"strLanguage\":\"Английский\",\"lNear\":\"1\",\"strKm\":\"1\",\"nCasualties\":\"11\",\"lHumanThreat\":\"1\",\"nCityid\":10,\"control\":\"1\",\"lTestCard\":\"1\",\"nPossession\":\"432\",\"strStructure\":\"Строение222\",\"nLocalDistrictId\":\"1\",\"strRoad\":\"ДорогаДолгая\",\"nMeter\":\"99\",\"lChs\":\"1\",\"strAdditionalInfo\":\"Чтотослучилось,номынезнаемчто\",\"dtDeclarantDateOfBirth\":\"1996-07-02T01:00:00+04:00\",\"strAddressStrip\":\"улватутина,д11\",\"nDeclarantStatusId\":\"1\"}";

	static async Task Main()
	{
		Console.Title = "outside tcp server";
		var listener = new TcpListener(IPAddress.Any, Port);
		listener.Start();
		Console.WriteLine($"Сервер запущен на порту {Port}");

		while (true)
		{
			TcpClient client = await listener.AcceptTcpClientAsync();
			Console.WriteLine("Клиент подключен");
			_ = Task.Run(() => HandleClientAsync(client));
		}
	}

	static async Task HandleClientAsync(TcpClient client)
	{
		try
		{
			using (client)
			using (var stream = client.GetStream())
			{
				int messageCount = 1;
				byte[] buffer = new byte[1024];

				while (client.Connected)
				{
					// Проверяем, жив ли клиент
					if (stream.DataAvailable)
					{
						int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
						if (bytesRead == 0) break; // Клиент закрыл соединение
						Console.WriteLine($"Получено сообщение: {Encoding.UTF8.GetString(buffer, 0, bytesRead)}");
					}

					//string message = $"Test message {messageCount}";
					byte[] data = Encoding.UTF8.GetBytes(Message);
					await stream.WriteAsync(data, 0, data.Length);
					Console.WriteLine($"Отправлено сообщение номер {messageCount}");

					messageCount++;
					await Task.Delay(2000);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка: {ex.Message}");
		}
		finally
		{
			Console.WriteLine("Клиент отключился");
		}
	}
}
