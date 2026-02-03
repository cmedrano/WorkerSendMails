using NLog;

class Program
{
    static void Main()
    {
        LogManager.Setup()
            .LoadConfigurationFromFile("NLog.config");

        var logger = LogManager.GetLogger("FinalJobLogger");

        try
        {
            logger.Info("Job iniciado");
            logger.Info("Procesando datos...");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error en el job");
            throw;
        }
        finally
        {
            logger.Info("Job finalizado"); // 📬 mail
            LogManager.Shutdown();
        }
    }
}
