using Swashbuckle.AspNetCore.SwaggerUI;
// Make the ADO.NET classes available
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using System.Data;


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

// Redirect all incoming HTTP requests to HTTPS
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

app.MapDelete("/deleteList", deleteList)
.WithName("ListDelete");


static async Task<IResult> login(LoginDto loginValues)
{
    // Use the database connection object returned from the DBConnectionUtility class
    using (SqlConnection conn = await DBConnectionUtility.GetConnectionAsync())
    {

        // Make the query string using the parameters from loginValues
        const string queryText = @"
            SELECT UserPassword
            FROM dbo.AppUser
            WHERE Username = @Username;";


        // Create executable sql object to use against the database
        using SqlCommand cmd = new SqlCommand(queryText, conn);

        // Add the queryParam value to the object
        cmd.Parameters.AddWithValue("@Username", loginValues.username);

        // Execute the query 
        try
        {
            // Check if there are any rows with matching username and password
            var storedHashObj = await cmd.ExecuteScalarAsync(); // Get the value of the first column and row of the result set

            // If nothing was returned then no such user exists
            if (storedHashObj is null || storedHashObj == DBNull.Value)
            {
                Console.WriteLine($"Login attempt for {loginValues.username}: Fail (no such user)");
                return Results.Ok(false);
            }

            // Ensure that the returned hashed password is a string
            var storedHash = (string)storedHashObj;

            // Verify the string the user passed matches the password returned from the database
            bool verify = PasswordHasher.VerifyPassword(loginValues.password, storedHash);

            if (verify == true)
            {
                Console.WriteLine($"Login attempt for {loginValues.username}: Successfull");
            }
            else
            {
                Console.WriteLine($"Login attempt for {loginValues.username}: Failed (password is incorrect)");
            }
            return Results.Ok(verify);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during login: " + ex.Message);
            return Results.Problem("Error during login: " + ex.Message);
        }

    }
}



static async Task<IResult> createAccount(AccountCredentialsDto values)
{
    // Use the database connection object returned from the DBConnectionUtility class
    using (SqlConnection conn = await DBConnectionUtility.GetConnectionAsync())
    {
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

    // Use the database connection object returned from the DBConnectionUtility class
    using (SqlConnection conn = await DBConnectionUtility.GetConnectionAsync())
    {
        // Only insert if the ListID doesn't already exist
        string queryText = @"
            IF NOT EXISTS (SELECT 1 FROM List WHERE ListID = @ListID)
            BEGIN
                INSERT INTO List (ListID, UserID) VALUES (@ListID, @UserID)
            END
        ";

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
    if (queryValues == null || queryValues.Count == 0)
        return Results.BadRequest("No tasks provided.");

    // Upsert the tasks from the saved list into the Task table
    try
    {
        // Create the connection. The connection will close automatically when the closing block is exited 
        SqlConnection conn = await DBConnectionUtility.GetConnectionAsync();

        // Begin a transaction on the database connection to ensure that subsequent database operations 
        // do not read uncommitted data.
        using var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted);

        // Pre-prepare UPDATE and INSERT commands (reused for each item)
        const string updateSql = @"
        UPDATE dbo.Task
        SET Priority=@Priority, TaskText=@TaskText, TaskName=@TaskName, Completed=@Completed
        WHERE ListID=@ListID AND ListPos=@ListPos;
        ";

        const string insertSql = @"
        INSERT INTO dbo.Task (Completed, Priority, TaskText, TaskName, ListID, ListPos, TaskCreatedAt)
        VALUES (@Completed, @Priority, @TaskText, @TaskName, @ListID, @ListPos, SYSUTCDATETIME());
        ";


        using var updateCmd = new SqlCommand(updateSql, conn, tx);
        updateCmd.Parameters.Add("@Priority", SqlDbType.Int);
        updateCmd.Parameters.Add("@TaskText", SqlDbType.NVarChar, 4000);
        updateCmd.Parameters.Add("@TaskName", SqlDbType.NVarChar, 200);
        updateCmd.Parameters.Add("@Completed", SqlDbType.Bit);
        updateCmd.Parameters.Add("@ListID", SqlDbType.Int);
        updateCmd.Parameters.Add("@ListPos", SqlDbType.Int);

        using var insertCmd = new SqlCommand(insertSql, conn, tx);
        insertCmd.Parameters.Add("@Completed", SqlDbType.Bit);
        insertCmd.Parameters.Add("@Priority", SqlDbType.Int);
        insertCmd.Parameters.Add("@TaskText", SqlDbType.NVarChar, 4000);
        insertCmd.Parameters.Add("@TaskName", SqlDbType.NVarChar, 200);
        insertCmd.Parameters.Add("@ListID", SqlDbType.Int);
        insertCmd.Parameters.Add("@ListPos", SqlDbType.Int);

        int updated = 0, inserted = 0;

        foreach (var task in queryValues)
        {
            // UPDATE first
            updateCmd.Parameters["@Priority"].Value = task.priority;
            updateCmd.Parameters["@TaskText"].Value = (object?)task.taskText ?? DBNull.Value;
            updateCmd.Parameters["@TaskName"].Value = (object?)task.taskName ?? DBNull.Value;
            updateCmd.Parameters["@Completed"].Value = task.completed;
            updateCmd.Parameters["@ListID"].Value = task.listID;
            updateCmd.Parameters["@ListPos"].Value = task.listPosition;

            int rows = await updateCmd.ExecuteNonQueryAsync();

            if (rows == 0)
            {
                // Not found -> INSERT
                insertCmd.Parameters["@Completed"].Value = task.completed;
                insertCmd.Parameters["@Priority"].Value = task.priority;
                insertCmd.Parameters["@TaskText"].Value = (object?)task.taskText ?? DBNull.Value;
                insertCmd.Parameters["@TaskName"].Value = (object?)task.taskName ?? DBNull.Value;
                insertCmd.Parameters["@ListID"].Value = task.listID;
                insertCmd.Parameters["@ListPos"].Value = task.listPosition;

                await insertCmd.ExecuteNonQueryAsync();
                inserted++;
            }
            else
            {
                updated++;
            }
        }

        // To make this transaction a “true sync”: delete DB rows for this list that aren’t in the payload
        var incomingPositions = queryValues.Select(x => x.listPosition).ToHashSet();
        if (incomingPositions.Count > 0)
        {
            // Build NOT IN (@P0,@P1,...) safely
            var paramNames = incomingPositions.Select((_, i) => $"@P{i}").ToArray();
            var deleteSql = $@"
                DELETE FROM dbo.Task
                WHERE ListID = @ListID
                AND ListPos NOT IN ({string.Join(",", paramNames)});
            ";

            using var deleteCmd = new SqlCommand(deleteSql, conn, tx);
             int listId = queryValues[0].listID;
            deleteCmd.Parameters.AddWithValue("@ListID", listId);

            int i = 0;
            foreach (var pos in incomingPositions)
                deleteCmd.Parameters.Add(paramNames[i++], SqlDbType.Int).Value = pos;

            await deleteCmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();

        return Results.Ok(new { updated, inserted });
    }
    catch (Exception ex)
    {
        return Results.Problem("Save failed: " + ex.Message);

    }
}



static async Task<IResult> getToDoList([AsParameters] GetQueryParameters queryParameter)
{ 
        // Use the database connection object returned from the DBConnectionUtility class
    using (SqlConnection conn = await DBConnectionUtility.GetConnectionAsync())
    {
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



static async Task<IResult> deleteList()
{
    // Use the database connection object returned from the DBConnectionUtility class
    using (SqlConnection conn = await DBConnectionUtility.GetConnectionAsync())
    {
        // First delete the tasks from the Task table
        // Delete the tasks related to the list from the Task table
        string deleteStatementTask = "DELETE FROM Task WHERE ListID = @ListID";

        SqlCommand cmd1 = new SqlCommand(deleteStatementTask, conn);

        // Get the date for the listID
        DateTime dateTime = DateTime.Now;
        int day = dateTime.Day;
        int month = dateTime.Month;
        int year = dateTime.Year;

        string date = $"{month}{day}{year}";

        cmd1.Parameters.AddWithValue("@ListID", date);

        try
        {
            int rowsAffected = await cmd1.ExecuteNonQueryAsync();
            Console.WriteLine($"Rows affected: {rowsAffected}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error deleting the list from List table" + ex.Message);
            return Results.Problem("Error deleting the list from the List table" + ex.Message);
        }

        // Delete statement for deleting the list from the List table
        string deleteStatementList = "DELETE FROM List WHERE ListID = @ListID";

        SqlCommand cmd2 = new SqlCommand(deleteStatementList, conn);

        cmd2.Parameters.AddWithValue("@ListID", date);

        try
        {
            int rowsAffected = await cmd2.ExecuteNonQueryAsync();
            Console.WriteLine($"Rows affected: {rowsAffected}");
            return Results.Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error deleting the list from List table" + ex.Message);
            return Results.Problem("Error deleting the list from the List table" + ex.Message);
        }

    }
}



app.Run();


public record ListRecord(int ListID, int UserID, bool ListCompleted, DateTime ListCreatedAt);
public record TaskRecord(int TaskID, bool Completed, int Priority, string TaskText,
    string TaskName, int ListID, int ListPos, DateTime TaskCreatedAt);
record GetQueryParameters(DateTime Date);
record TaskInputDto(int taskID, int priority, string taskText, string taskName, bool completed, int listID, int listPosition);
record LoginDto(string username, string password);
record ListInputDto(int listID, int userID);
record AccountCredentialsDto(string username, string password);