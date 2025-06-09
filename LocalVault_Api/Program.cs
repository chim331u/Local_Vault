using LocalVault_Api.Contracts;
using LocalVault_Api.Services;
using Microsoft.OpenApi.Models;
using OneApp_minimalApi.Interfaces;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1",
        new OpenApiInfo { Title = "Local Vault API", Version = "v1", Description = "Local Vault minimal API" });

    // // Set the comments path for the Swagger JSON and UI.
    // var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    // var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    // c.IncludeXmlComments(xmlPath);
});


// Adding the database context
// builder.Services.AddSingleton<DapperContext>(provider =>
//     new DapperContext(builder.Configuration));

builder.Services.AddScoped<ILocalVaultService, LocalVaultService>();

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

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");


//************** ENDPOINTS ******************//
app.MapGet("/CreateDatabase", (ILocalVaultService _service) =>
    {
        var result = _service.CreateDatabase();
        return Results.Ok(result);
    })
    .WithName("CreateDatabase")
    .WithOpenApi();
        
        
        /// <summary>
        /// Endpoint to mangage secrets in a local valut.
        /// </summary>
        app.MapGet("/GetSecret", async (string Key, ILocalVaultService _service) =>
        {
            var result = await _service.GetSecret(Key);

            if (result == null)
            {
                return Results.NotFound(result);
            }
            
            return Results.Ok(result);
        })
        .WithOpenApi().WithName("GetSecret");
        
        /// <summary>
        /// Endpoint to mangage secrets in a local valut.
        /// </summary>
        app.MapGet("/GetSecretList", async (ILocalVaultService _service) =>
        {
            var result = await _service.GetListSecrets();

            if (result == null)
            {
                return Results.NotFound(result);
            }
            
            return Results.Ok(result);
        }).WithOpenApi().WithName("GetSecretList");

        app.MapPost("/AddSecret", async (SecretRequestDTO secret, ILocalVaultService _service) =>
        {
            var result = await _service.StoreSecret(secret);
            
            if (result == null)
            {
                return Results.BadRequest(result);
            }
            
            return Results.Ok(result);

        }).WithOpenApi().WithName("AddSecret");
        
        app.MapPut("/UpdateSecret/{id:int}", async (int id, SecretRequestDTO secret, ILocalVaultService _service) =>
        {
            var result = await _service.UpdateSecret(id, secret);
            
            if (result == null)
            {
                return Results.BadRequest(result);
            }
            
            return Results.Ok(result);

        }).WithOpenApi().WithName("UpdateSecret");
        
        app.MapPut("/ChangeSecretPsw/{id:int}", async (int id, SecretRequestDTO secret, ILocalVaultService _service) =>
        {
            var result = await _service.UpdateSecret(id, secret, true);
            
            if (result == null)
            {
                return Results.BadRequest(result);
            }
            
            return Results.Ok(result);

        }).WithName("ChangeSecretPsw").WithOpenApi();
        
        app.MapDelete("/DeleteSecret/{id:int}", async (int id, ILocalVaultService _service) =>
        {
            var result = await _service.DeleteSecret(id);
            
            if (!result)
            {
                return Results.NotFound(result);
            }
            
            return Results.NoContent();
        }).WithOpenApi().WithName("DeleteSecret");

        /// <summary>
        /// Endpoint to mangage secrets in a local valut.
        /// </summary>
        app.MapGet("/GetHistoricalSecretsList", async (ILocalVaultService _service) =>
        {
            var result = await _service.GetHistorySecretList();

            if (result == null)
            {
                return Results.NotFound(result);
            }
            
            return Results.Ok(result);
        }).WithOpenApi().WithName("GetHistoricalSecretsList");



app.Run();
