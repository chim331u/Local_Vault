using Dapper;
using LocalVault_Api.Data;
using LocalVault_Api.Model;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Adding the database context
builder.Services.AddSingleton<DapperContext>(provider =>
    new DapperContext(builder.Configuration));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.MapGet("/CreateDatabase", (DapperContext _dapperContext) =>
    {
        var result = 0;

        using var connection = _dapperContext.CreateConnection();
        try
        {
            string sql2 = "CREATE TABLE IF NOT EXISTS Secret (Id INTEGER PRIMARY KEY, key TEXT,value TEXT, CreateDate DATETIME)";

            connection.Execute(sql2);
        }
        catch (Exception ex)
        {
        }

        return true;
    })
    .WithName("CreateDatabase")
    .WithOpenApi();

app.MapGet("/GetSecret", (string key, DapperContext _dapperContext) =>
{
    var result = new Secret();

    using (var connection = _dapperContext.CreateConnection())
    {
        string sql = $"SELECT * From Secret Where key=@key";

        result = connection.QueryFirstOrDefault<Secret>(sql, new { key });
    }

    return result;
});

app.MapPost("/StoreSecret", (SecretRequest request_data, DapperContext _dapperContext) =>
{
    var result = 0;

    request_data.CreateDate = DateTime.UtcNow;
    
    using (var connection = _dapperContext.CreateConnection())
    {
        try
        {
            string sql = $"INSERT INTO Secret(Key,Value,CreateDate) VALUES (@Key,@Value,@CreateDate)";

            result = connection.Execute(sql, new { request_data.Key, request_data.Value, request_data.CreateDate });
            
            Console.WriteLine("Record added successfully.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            
        }

    }

    return Results.Ok($"Added {result} record(s) successfully.");
});



app.Run();
