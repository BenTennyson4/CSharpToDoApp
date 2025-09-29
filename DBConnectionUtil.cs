using Microsoft.Data.SqlClient;

public class DBConnectionUtility
{
    public static async Task<SqlConnection> GetConnectionAsync()
    {
        // Load configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var dbSettings = configuration.GetSection("DBConnection");

        // Choose the server depending on environment (set DOTNET_RUNNING_IN_DOCKER=true in Docker Compose)
        var server = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_DOCKER") == "true"
            ? dbSettings["Server_Docker"]
            : dbSettings["Server_Host"];

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = dbSettings["Database"],
            IntegratedSecurity = bool.Parse(dbSettings["IntegratedSecurity"] ?? "False"),
            Encrypt = bool.Parse(dbSettings["Encrypt"] ?? "True"),
            TrustServerCertificate = bool.Parse(dbSettings["TrustServerCertificate"] ?? "True")
        };

        if (!string.IsNullOrEmpty(dbSettings["UserID"]))
        {
            builder.UserID = dbSettings["UserID"];
            builder.Password = dbSettings["Password"];
            builder.IntegratedSecurity = false;
        }

        var conn = new SqlConnection(builder.ConnectionString);
        await conn.OpenAsync();
        return conn;
    }
}