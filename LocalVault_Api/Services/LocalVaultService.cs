using System.Data;
using Dapper;
using LocalVault_Api.Configurations;
using LocalVault_Api.Contracts;
using LocalVault_Api.Data;
using Microsoft.Data.Sqlite;
using OneApp_minimalApi.Interfaces;

namespace LocalVault_Api.Services;

public class LocalVaultService : ILocalVaultService
{
    private readonly ILogger<LocalVaultService> _logger; // Logger for logging information and errors
    private readonly string _connectionString;
    private readonly string _secretValue;
    private readonly IConfiguration _configuration;
    private const string MasterKeySecretName = "MASTERKEY_SECRET";

    public LocalVaultService(IConfiguration configuration, ILogger<LocalVaultService> logger)
    {
        _configuration = configuration;

        _logger = logger;
        //for debug only
        _secretValue = _configuration.GetSection("IsDev").Value != null
            ? configuration[MasterKeySecretName]
            : Environment.GetEnvironmentVariable(MasterKeySecretName);

        _connectionString = configuration.GetConnectionString("LocalVaultConnection");

        if (string.IsNullOrEmpty(_connectionString))
        {
            _logger.LogWarning("Connection string not set for LocalVaultService");
        }

        if (string.IsNullOrEmpty(_secretValue))
        {
            _logger.LogWarning("MasterKey is not set for LocalVaultService");
        }

        SQLitePCL.Batteries_V2.Init(); // This ensures SQLiteCipher is ready
    }

    private IDbConnection CreateConnection()
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            _logger.LogWarning($"Connection string not set");
            return null;
        }


        if (string.IsNullOrEmpty(_secretValue))
        {
            _logger.LogWarning("MasterKey is not set");
            return null;
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _connectionString, // Path to your SQLite database file
            Mode = SqliteOpenMode.ReadWriteCreate // Allow reading and writing to the DB
        }.ToString();

        var connection = new SqliteConnection(connectionString);
        connection.Open();

        // Set the encryption key (PRAGMA key) after opening the connection
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA key = '{_secretValue}';"; // Set your encryption password
        command.ExecuteNonQuery();

        return connection; // Return the opened, encrypted connection ready for Dapper
    }

    public async Task<bool> CreateDatabase()
    {
        using var connection = CreateConnection();
        try
        {
            string sql1 =
                "CREATE TABLE IF NOT EXISTS LocalSecret (Id INTEGER PRIMARY KEY, key TEXT,value TEXT, CreateDate DATETIME)";
            connection.Execute(sql1);

            string sql2 =
                "CREATE TABLE IF NOT EXISTS HistoricalSecret (Id INTEGER PRIMARY KEY, SecretId INTEGER, key TEXT,value TEXT, SecretCreateDate DATETIME, SecretEndDate DATETIME)";
            connection.Execute(sql2);

            _logger.LogInformation("Database created");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating database: {ex.Message}");
            return false;
        }
    }

    public async Task<List<SecretsListDto>> GetListSecrets()
    {
        try
        {
            using var connection = CreateConnection();
            string sql = $"SELECT * From LocalSecret";

            var result = connection.QueryMultiple(sql, null).Read<LocalSecret>();

            _logger.LogInformation($"{result.Count()} retrieved secrets");
            return new List<SecretsListDto>(result.Select(Mapper.FromModelToSecretListDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching secrets: {ex.Message}");
            return null;
        }
    }

    public async Task<SecretResponseDTO> GetSecret(string key)
    {
        try
        {
            using var connection = CreateConnection();
            string sql = $"SELECT * From LocalSecret Where key=@key";

            var result = connection.QueryFirstOrDefault<LocalSecret>(sql, new { key });
            _logger.LogInformation($"Retrieved secret");

            if (result == null)
            {
                _logger.LogWarning("No secret returned");
                return null;
            }

            _logger.LogInformation($"Local Secret retrieved");
            return Mapper.FromModelToSecretResponseDto(result);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error retrieving secret: {e.Message}");
            return null;
        }
    }

    public async Task<SecretResponseDTO> StoreSecret(SecretRequestDTO secret)
    {
        try
        {
            using var connection = CreateConnection();
            string sql = $"INSERT INTO LocalSecret(Key,Value,CreateDate) VALUES (@Key,@Value,@CreateDate)";

            var CreateDate = DateTime.Now;
            var result = connection.Execute(sql, new { secret.Key, secret.Value, CreateDate });
            if (result > 0)
            {
                _logger.LogInformation("New secret added");
                return await GetSecret(secret.Key);
            }

            _logger.LogInformation("No secret(s) added: Unable to store the Local Secret");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding secret: {ex.Message}");
            return null;
        }
    }

    public async Task<SecretResponseDTO> UpdateSecret(int id, SecretRequestDTO secret)
    {
        try
        {
            using var connection = CreateConnection();
            //get the secret
            var storedSecret =
                connection.QueryFirstOrDefault<LocalSecret>($"SELECT * From LocalSecret Where Id=@id", new { id });


            string sql = $"UPDATE LocalSecret SET Key=@Key, Value=@Value WHERE Id=@Id";

            var result = connection.Execute(sql, new { secret.Key, secret.Value, storedSecret.Id });

            if (result > 0)
            {
                _logger.LogInformation("Secret updated");
                return  await GetSecret(storedSecret.Key);
            }
            else
            {
                _logger.LogInformation("No secret(s) updated: Unable to update the Local Secret");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error update secret: {ex.Message}");
            return null;
        }
    }

    public async Task<SecretResponseDTO> UpdateSecret(int id, SecretRequestDTO secret, bool passwordChange)
    {
        try
        {
            using var connection = CreateConnection();
            //get the secret
            var storedSecret =
                connection.QueryFirstOrDefault<LocalSecret>($"SELECT * From LocalSecret Where Id=@id", new { id });


            string sql = $"UPDATE LocalSecret SET Key=@Key, Value=@Value WHERE Id=@Id";

            var result = connection.Execute(sql, new { secret.Key, secret.Value, storedSecret.Id });
            if (result == 0)
            {
                _logger.LogInformation("No secret(s) updated");
                return null;
            }

            _logger.LogInformation("Secret updated");
            // Add in history password 
            HistoryPasswordAdd(storedSecret);
            _logger.LogInformation($"Local Secret updated");
            return await GetSecret(storedSecret.Key);
               
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error update secret: {ex.Message}");
            return null;
        }
    }

    private void HistoryPasswordAdd(LocalSecret storedSecret)
    {
        try
        {
            using var connection = CreateConnection();
            string sql =
                $"INSERT INTO HistoricalSecret(SecretId,Key,Value,SecretCreateDate,SecretEndDate) VALUES (@Id,@Key,@Value,@CreateDate, @SecretEndDate)";

            var SecretEndDate = DateTime.Now;
            var CreateDate = DateTime.Now;
            var result = connection.Execute(sql,
                new { storedSecret.Id, storedSecret.Key, storedSecret.Value, storedSecret.CreateDate, SecretEndDate });
            if (result > 0)
            {
                _logger.LogInformation("History secret added");
                return;
            }

            _logger.LogInformation("No history secret(s) added");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding history secret: {ex.Message}");
        }
    }

    public async Task<List<HistoricalSecret>> GetHistorySecretList()
    {
        try
        {
            using var connection = CreateConnection();
            string sql = $"SELECT * From HistoricalSecret ORDER BY SecretEndDate DESC";

            var result = connection.QueryMultiple(sql, null).Read<HistoricalSecret>();
            _logger.LogInformation($"Retrieved history secret");

            if (result == null)
            {
                _logger.LogWarning("No history secret returned");
                return null;
            }

            _logger.LogInformation("Retrieved history secret");
            return [..result.ToList()];
        }
        catch (Exception e)
        {
            _logger.LogError($"Error retrieving history secret: {e.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteSecret(int id)
    {
        try
        {
            using var connection = CreateConnection();
            //get the secret
            var storedSecret =
                connection.QueryFirstOrDefault<LocalSecret>($"SELECT * From LocalSecret Where Id=@id", new { id });

            string sql = $"DELETE LocalSecret WHERE Id=@Id";

            var result = connection.Execute(sql, new { storedSecret.Id });
            if (result > 0)
            {
                _logger.LogInformation("Secret deleted");
                return true;
            }
            else
            {
                _logger.LogInformation("No secret(s) deleted");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error delete secret: {ex.Message}");
            return false;
        }
    }
}