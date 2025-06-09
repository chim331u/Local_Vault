using LocalVault_Api.Contracts;
using LocalVault_Api.Data;

namespace LocalVault_Api.Configurations;

/// <summary>
/// Provides mapping methods for converting between models and DTOs.
/// </summary>
public static class Mapper
{
    
    public static LocalSecret LocalSecretRequestDtoToModel(SecretRequestDTO secretDto)
    {
        return new LocalSecret()
            {Key = secretDto.Key, Value = secretDto.Value};
    }

    public static SecretResponseDTO FromModelToSecretResponseDto(LocalSecret secret)
    {
        return new SecretResponseDTO()
        {
            Id = secret.Id, Key = secret.Key, Value = secret.Value
        };
    }
    public static SecretsListDto FromModelToSecretListDto(LocalSecret secret)
    {
        return new SecretsListDto()
        {
            Id = secret.Id, Key = secret.Key
        };
    }
}