using NLog;
using Resend;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Npgsql;
using System.Globalization;
using System;

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
        try
        {
            var connString =
            $"Host={Environment.GetEnvironmentVariable("DB_HOST")};" +
            $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
            $"Username={Environment.GetEnvironmentVariable("DB_USER")};" +
            $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};" +
            $"SSL Mode=VerifyFull;" +
            $"Channel Binding=Require;";

            using var conn = new NpgsqlConnection(connString);
            conn.Open();

            using var cmd = new NpgsqlCommand(
             @"SELECT 
             b.""Id"",
             b.""valorInicial"",
             b.""Mes"",
             b.""Anio"",
             r.""nombreRubro"" AS rubro,
             b.""ValorGastado"",
             b.""CreateDate"",
             u.""UserName"" AS user
             FROM ""Budget"" b
             JOIN ""RubroType"" r ON r.""Id"" = b.""RubroTypeId""
             JOIN ""Users"" u ON u.""Id"" = b.""CreateByUserId""
             ORDER BY b.""Anio"" DESC, b.""Mes"" DESC;",
            conn);

            using var reader = cmd.ExecuteReader();

            var sb = new StringBuilder();

            sb.AppendLine("<h3>📊 Presupuesto</h3>");
            sb.AppendLine("<table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse;'>");
            sb.AppendLine("<tr style='background-color:#f2f2f2;'>");
            sb.AppendLine("<th>Mes</th>");
            sb.AppendLine("<th>Año</th>");
            sb.AppendLine("<th>Rubro</th>");
            sb.AppendLine("<th>Valor Inicial</th>");
            sb.AppendLine("<th>Gastado</th>");
            sb.AppendLine("<th>DIsponible</th>");
            sb.AppendLine("<th>Fecha creado</th>");
            sb.AppendLine("<th>Creado por</th>");
            sb.AppendLine("</tr>");

            while (reader.Read())
            {
                var initialValue = reader.GetDecimal(1);
                var month = reader.GetInt32(2);
                var year = reader.GetInt32(3);
                var category = reader.GetString(4);
                var valueSpent = reader.GetDecimal(5);
                var createDate = reader.GetDateTime(6);
                var createByUser = reader.GetString(7);
                var available = initialValue - valueSpent;

                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{MonthName(month)}</td>");
                sb.AppendLine($"<td>{year}</td>");
                sb.AppendLine($"<td>{category}</td>");
                sb.AppendLine($"<td>{initialValue.ToString("C2", new CultureInfo("es-AR"))}</td>");
                sb.AppendLine($"<td>{valueSpent.ToString("C2", new CultureInfo("es-AR"))}</td>");
                sb.AppendLine($"<td>{available.ToString("C2", new CultureInfo("es-AR"))}</td>");
                sb.AppendLine($"<td>{createDate:dd/MM/yyyy}</td>");
                sb.AppendLine($"<td>{createByUser}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");

            var response = await SendMailAsync(sb.ToString());

            var body = await response.Content.ReadAsStringAsync();

            Console.WriteLine(response.IsSuccessStatusCode
                ? "✅ Mail enviado correctamente"
                : $"❌ Error: {body}");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"error durante el proceso de envio, mensaje de error: {ex.Message}");
        }
    }
       
    static async Task<HttpResponseMessage> SendMailAsync(string html)
    {
        //var apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY");
        string apiKey = "re_Rw9XDzbt_pzPR3QPokMxz7g9WnSTbfKD4";
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Falta RESEND_API_KEY");
        }

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            from = "onboarding@resend.dev",
            to = new[] { "andresfriasmedrib@gmail.com" },
            subject = "📬 Test Resend desde consola",
            html = html
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        return await http.PostAsync("https://api.resend.com/emails", content);
    }

    public static string MonthName(int month)
    {
        var culture = new CultureInfo("es-AR");
        return culture.TextInfo.ToTitleCase(
            culture.DateTimeFormat.GetMonthName(month)
        );
    } 
}