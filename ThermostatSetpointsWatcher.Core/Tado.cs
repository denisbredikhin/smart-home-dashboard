using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ThermostatSetpointsWatcher;

public class Room
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("setting")]
    public RoomSetting Setting { get; set; }
}

public class RoomSetting
{
    [JsonPropertyName("power")]
    public string Power { get; set; }

    [JsonPropertyName("temperature")]
    public Temperature Temperature { get; set; }
}

public class Temperature
{
    [JsonPropertyName("value")]
    public double? Value { get; set; }
}
public class Tado
{
    public static async Task<List<Room>> GetRooms()
    {
        var token = await GetAccessToken();
        return await CallRoomsEndpoint(token);
    }

    private static async Task<string> GetAccessToken()
    {
        using var httpClient = new HttpClient();
        
        var url = "https://auth.tado.com/oauth/token";
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", "public-api-preview"),
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("client_secret", "4HJGRffVR8xb3XdEUQpjgZ1VplJi6Xgw"),
            new KeyValuePair<string, string>("username", "denis.bredikhin@gmail.com"),
            new KeyValuePair<string, string>("password", ""),
            new KeyValuePair<string, string>("scope", "home.user")
        });

        try
        {
            var response = await httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ошибка при получении токена: {response.StatusCode} - {response.ReasonPhrase}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(json);
            var accessToken = jsonDoc.RootElement.GetProperty("access_token").GetString();

            return accessToken;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении токена: {ex.Message}");
            return null;
        }
    }

    private static async Task<List<Room>> CallRoomsEndpoint(string accessToken)
    {
        using var httpClient = new HttpClient();

        var url = "https://hops.tado.com/homes/1632093/rooms";
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ошибка при вызове эндпоинта /rooms: {response.StatusCode} - {response.ReasonPhrase}");
                return null;
            }

            var result = await response.Content.ReadAsStringAsync();
            var rooms = JsonSerializer.Deserialize<List<Room>>(result);

            return rooms;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при вызове эндпоинта: {ex.Message}");
            return null;
        }
    }
}