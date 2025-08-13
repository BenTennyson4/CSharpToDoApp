using Swashbuckle.AspNetCore.SwaggerUI;
// Make the ADO.NET classes available
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable static file serving
app.UseDefaultFiles();    // Looks for index.html or default.html
app.UseStaticFiles();

app.UseHttpsRedirection();

app.MapPost("/login", async ([FromBody] LoginDto values) => await login(values))
.WithName("Login");

app.MapPost("/createAccount", async ([FromBody] AccountCredentialsDto values) => await createAccount(values))
.WithName("AccountCreator");

app.MapPut("/putList", async ([FromBody] ListInputDto values) => await putList(values))
.WithName("ListInsert");

app.MapPut("/putTasks", async ([FromBody] List<TaskInputDto> queryValues) => await putTasks(queryValues))
.WithName("TaskInsert");

app.MapGet("/getList", getToDoList)
.WithName("TaskRetriever");


static async Task<IResult> login(LoginDto values)
{
    string connectionString = "Server=LAPTOP-EJD37JMV,1433;Database=C#ToDoApp;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;";
    using (SqlConnection conn = new SqlConnection(connectionString))
    {
        await conn.OpenAsync(); // Open the connection to the database

        // Make the query string using the parameters from values
        string queryText = @"SELECT * FROM AppUser WHERE Username = @Username AND UserPassword = @UserPassword";

        // Create executable sql object to use against the database
        using SqlCommand cmd = new SqlCommand(queryText, conn);

        // Add the queryParam value to the object
        cmd.Parameters.AddWithValue("@Username", values.username);
        cmd.Parameters.AddWithValue("@UserPassword", values.password);

        // Execute the query 
        try
        {
            int mathCount = (int)await cmd.ExecuteScalarAsync(); // Get the num of rows with matching username and password
            bool userExists = mathCount > 0;

            Console.WriteLine($"Login attempt for {values.username}: {(userExists ? "Success" : "Fail")}");
            return Results.Ok(userExists);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error inserting task: " + ex.Message);
            return Results.Problem("Error inserting task: " + ex.Message);
        }

    }
}



static async Task<IResult> createAccount(AccountCredentialsDto values)
{
    string connectionString = "Server=LAPTOP-EJD37JMV,1433;Database=C#ToDoApp;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;";
    using (SqlConnection conn = new SqlConnection(connectionString))
    {
        await conn.OpenAsync(); // Open the connection to the database

        // Make the query string using the parameters from values
        string queryText = @"INSERT INTO AppUser (UserPassword, Username) VALUES (@UserPassword, @Username)";

        // Create executable sql object to use against the database
        using SqlCommand cmd = new SqlCommand(queryText, conn);

        // Hash the password
        string hashedPass = PasswordHasher.HashPassword(values.password);

        // Add the queryParam value to the object
        cmd.Parameters.AddWithValue("@Username", values.username);
        cmd.Parameters.AddWithValue("@UserPassword", hashedPass);

        // Execute the query 
        try
        {
            int rowsAffected = await cmd.ExecuteNonQueryAsync();

            Console.WriteLine($"Rows affected: {rowsAffected}");
            return rowsAffected > 0
                ? Results.Ok("Account values inserted successfully.") : Results.Problem("The account values were not inserted.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error inserting task: " + ex.Message);
            return Results.Problem("Error inserting task: " + ex.Message);
        }

    }
}



static async Task<IResult> putList(ListInputDto values)
{
    Console.WriteLine("Recieved List info");
    Console.WriteLine($"ListID: {values.listID}");
    Console.WriteLine($"UserID: {values.userID}");

    // Create a connection string containing servername, databasename, and authentication method
    // Note: try to use config file for managing db connection credentials 
    string connectionString = "Server=LAPTOP-EJD37JMV,1433;Database=C#ToDoApp;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;";

    using (SqlConnection conn = new SqlConnection(connectionString))
    {
        conn.Open();

        string queryText = "INSERT INTO List (ListID, UserID) VALUES (@ListID, @UserID)";

        using SqlCommand cmd = new SqlCommand(queryText, conn);
        // Add the queryParam value to the object
        cmd.Parameters.AddWithValue("@ListID", values.listID);
        cmd.Parameters.AddWithValue("@UserID", values.userID);

        // Execute the query 
        try
        {
            int rowsAffected = await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"Rows affected: {rowsAffected}");
            return rowsAffected > 0
                ? Results.Ok("List values inserted successfully.") : Results.Problem("The list values were not inserted.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error inserting list values: " + ex.Message);
            return Results.Problem("Error inserting list values: " + ex.Message);
        }
    }
}


static async Task<IResult> putTasks(List<TaskInputDto> queryValues)
{
    int successCount = 0;
    int failCount = 0;
    List<string> errors = new();

    for (int i = 0; i < queryValues.Count; i++)
    {
        Console.WriteLine("Received task:");
        Console.WriteLine($"Priority: {queryValues[i].priority}");
        Console.WriteLine($"TaskText: {queryValues[i].taskText}");
        Console.WriteLine($"TaskName: {queryValues[i].taskName}");
        Console.WriteLine($"ListID: {queryValues[i].listID}");
        Console.WriteLine($"ListID: {queryValues[i].listPosition}");

        // Create a connection string containing servername, databasename, and authentication method
        // Note: try to use config file for managing db connection credentials 
        string connectionString = "Server=LAPTOP-EJD37JMV,1433;Database=C#ToDoApp;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;";

        // Create the connection. The connection will close automatically when the closing block is exited 
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open(); // Open the connection to the database

            // Insert the ToDoItem using the date query parameter
            string queryText = @"INSERT INTO Task (Priority, TaskText, TaskName, ListID, ListPos)
            VALUES (@priority, @taskText, @taskName, @listID, @listPosition)";

            // Prepare the query for execution
            // Create an object that will be executed against the database
            using SqlCommand cmd = new SqlCommand(queryText, conn);
            // Add the queryParam value to the object
            cmd.Parameters.AddWithValue("@priority", queryValues[i].priority);
            cmd.Parameters.AddWithValue("@taskText", queryValues[i].taskText);
            cmd.Parameters.AddWithValue("@taskName", queryValues[i].taskName);
            cmd.Parameters.AddWithValue("@listID", queryValues[i].listID);
            cmd.Parameters.AddWithValue("@listPosition", queryValues[i].listPosition);

            // Execute the query 
            try
            {
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                Console.WriteLine($"Rows affected: {rowsAffected}");
                if (rowsAffected > 0)
                    successCount++;
                else
                    failCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error inserting task: " + ex.Message);
                return Results.Problem("Error inserting task: " + ex.Message);
            }
        }
    }
    if (failCount == 0)
        return Results.Ok($"{successCount} tasks inserted successfully.");
    else
        return Results.Problem($"{successCount} tasks inserted, {failCount} failed. Errors: {string.Join("; ", errors)}");
    
}


static async Task<IResult> getToDoList([AsParameters] GetQueryParameters queryParameter)
{
    // Create a connection string containing servername, databasename, and authentication method
    // Note: try to use config file for managing credentials 
    string connectionString = "Server=LAPTOP-EJD37JMV\\SQLEXPRESS;Database=C#ToDoApp;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;";

    // Create the connection. The connection will close automatically when the closing block is exited 
    using (SqlConnection conn = new SqlConnection(connectionString))
    {
        conn.Open(); // Open the connection to the database

        // Get the ToDoList using the date query parameter obtained from the url
        const string listQuery = @"
            SELECT TOP (1) ListID, UserID, ListCompleted, ListCreatedAt
            FROM dbo.List
            WHERE CAST(ListCreatedAt AS date) = @Date
            ORDER BY ListCreatedAt DESC;";

        // Prepare the query for execution
        // Create an object that will be executed against my database
        using SqlCommand cmd1 = new SqlCommand(listQuery, conn);
        // Add the queryParam value to the object
        cmd1.Parameters.AddWithValue("@Date", queryParameter.Date.Date); // Ensure only date part of the queryParam is used

        // Execute the query and store the results
        using SqlDataReader listReader = await cmd1.ExecuteReaderAsync();
        if (!await listReader.ReadAsync())
        {
            // No list found for that date → return empty set
            return Results.Ok(Array.Empty<TaskRecord>());
        }

        int listID = listReader.GetInt32(0);
        int userID = listReader.GetInt32(1);

        listReader.Close();

        // Create the query to return all of the user's tasks related to the list
        const string tasksQuery = @"
            SELECT TaskID, Completed, Priority, TaskText, TaskName, ListID, ListPos, TaskCreatedAt
            FROM dbo.Task
            WHERE ListID = @ListID
            ORDER BY ListPos;";


        using SqlCommand cmd2 = new SqlCommand(tasksQuery, conn);
        cmd2.Parameters.AddWithValue("ListID", listID);
        cmd2.Parameters.AddWithValue("UserID", userID);

        using SqlDataReader taskReader = await cmd2.ExecuteReaderAsync();

        // Store the results in a list of type TaskRecord
        List<TaskRecord> tasks = new();
        while (await taskReader.ReadAsync())
        {
            tasks.Add(new TaskRecord(
                taskReader.GetInt32(0),
                taskReader.GetBoolean(1),
                taskReader.GetInt32(2),
                taskReader.GetString(3),
                taskReader.GetString(4),
                taskReader.GetInt32(5),
                taskReader.GetInt32(6),
                taskReader.GetDateTime(7)
            ));
        }
        taskReader.Close();
        return Results.Ok(tasks);
    }

}


app.Run();


public record ListRecord(int ListID, int UserID, bool ListCompleted, DateTime ListCreatedAt);
public record TaskRecord(int TaskID, bool Completed, int Priority, string TaskText,
    string TaskName, int ListID, int ListPos, DateTime TaskCreatedAt);
record GetQueryParameters(DateTime Date);
record TaskInputDto(int taskID, int priority, string taskText, string taskName, int listID, int listPosition);
record LoginDto(string username, string password);
record ListInputDto(int listID, int userID);
record AccountCredentialsDto(string username, string password);