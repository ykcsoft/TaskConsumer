using Hangfire;
using Hangfire.PostgreSql;
using Npgsql;
using System;
using System.Threading;
using System.Threading.Tasks;
using taskscheduleykc;

class Program
{
    static async Task Main(string[] args)
    {
        string connectionString = ConsumerConfig.PostgresConnectionString;

        // Önce tabloyu kontrol edip oluştur
        await EnsureTableExists(connectionString);

        var postgresOptions = new PostgreSqlStorageOptions
        {
            PrepareSchemaIfNecessary = true,
            QueuePollInterval = TimeSpan.FromSeconds(10)
        };

        GlobalConfiguration.Configuration
            .UsePostgreSqlStorage(connectionString, postgresOptions);

        using var server = new BackgroundJobServer();

        Console.WriteLine("Hangfire server başlatıldı ve job'lar çalışmaya başladı...");

        // RabbitMQ Consumer
        var taskConsumer = new TaskConsumer();

        await taskConsumer.InitializeAsync();

        taskConsumer.StartListening();

        Console.WriteLine("TaskConsumer başarıyla başlatıldı.");
        Console.WriteLine("RabbitMQ kuyruğu dinleniyor...");
        Console.WriteLine("Uygulama sürekli çalışma modunda.");

        // Docker container'ın kapanmasını engeller
        await Task.Delay(Timeout.InfiniteTimeSpan);
    }

    private static async Task EnsureTableExists(string connectionString)
    {
        try
        {
            using var connection = new NpgsqlConnection(connectionString);

            await connection.OpenAsync();

            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS JobDtoScheduleWarn (
                    ScheduleWarnId VARCHAR(255) PRIMARY KEY,
                    JobId VARCHAR(255) NOT NULL
                );";

            using var command = new NpgsqlCommand(createTableQuery, connection);

            await command.ExecuteNonQueryAsync();

            Console.WriteLine("JobDtoScheduleWarn tablosu kontrol edildi.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Tablo kontrol edilirken hata oluştu: {ex.Message}");
            throw;
        }
    }
}