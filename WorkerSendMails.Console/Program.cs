using NLog;
using Resend;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

class Program
{
    //static void Main()
    //{
    //    LogManager.Setup()
    //        .LoadConfigurationFromFile("NLog.config");

    //    Console.WriteLine("comienza el job");
    //    var logger = LogManager.GetLogger("FinalJobLogger");

    //    try
    //    {
    //        logger.Info("Job iniciado");
    //        logger.Info("Procesando datos...");
    //    }
    //    catch (Exception ex)
    //    {
    //        logger.Error(ex, "Error en el job");
    //        throw;
    //    }
    //    finally
    //    {
    //        Console.WriteLine("termina el job");
    //        logger.Info("Job finalizado"); // 📬 mail
    //        LogManager.Shutdown();
    //    }
    //}
    static async Task Main()
    {
            var apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine("❌ Falta RESEND_API_KEY");
                return;
            }

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                from = "onboarding@resend.dev",
                to = new[] { "andresfriasmedrib@gmail.com" },
                subject = "📬 Test Resend desde consola",
                html = "<strong>Mail enviado correctamente desde consola .NET</strong>"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await http.PostAsync("https://api.resend.com/emails", content);

            var body = await response.Content.ReadAsStringAsync();

            Console.WriteLine(response.IsSuccessStatusCode
                ? "✅ Mail enviado correctamente"
                : $"❌ Error: {body}");
        }
    }