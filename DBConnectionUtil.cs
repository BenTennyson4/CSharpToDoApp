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

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = dbSettings["Server"],
            InitialCatalog = dbSettings["Database"],
            IntegratedSecurity = bool.Parse(dbSettings["IntegratedSecurity"] ?? "True"),
            Encrypt = bool.Parse(dbSettings["Encrypt"] ?? "False"),
            TrustServerCertificate = bool.Parse(dbSettings["TrustServerCertificate"] ?? "True")
        };

        // If there is a UserID provided, then try to use SQL ServerAuthentication instead of Windows authentication
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