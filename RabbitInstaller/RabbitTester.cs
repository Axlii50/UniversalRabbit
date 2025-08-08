using System.Net.Http.Headers;

namespace RabbitInstaller
{
    class RabbitTester
    {
        public static async Task TestRabbitAsync(string host = "localhost", int port = 15672, string user = "guest", string pass = "guest")
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri($"http://{host}:{port}/api/");
            var byteArray = System.Text.Encoding.ASCII.GetBytes($"{user}:{pass}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            try
            {
                var response = await client.GetAsync("overview");
                if (response.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[SUCCESS] RabbitMQ działa i panel WWW jest dostępny!");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[WARNING] RabbitMQ odpowiada, ale status: {(int)response.StatusCode} {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] RabbitMQ nie działa lub brak dostępu: {ex.Message}");
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }
}
