using LocalVault_Api.Contracts;
using LocalVault_Api.Data;


namespace OneApp_minimalApi.Interfaces;

public interface ILocalVaultService
{
    Task<bool> CreateDatabase();
    
    Task<List<SecretsListDto>> GetListSecrets();
    Task<SecretResponseDTO> GetSecret(string key);
    
    Task<SecretResponseDTO> StoreSecret(SecretRequestDTO secret);
    Task<SecretResponseDTO> UpdateSecret(int id, SecretRequestDTO secret);
    Task<SecretResponseDTO> UpdateSecret(int id, SecretRequestDTO secret, bool passwordChange);
    Task<bool> DeleteSecret(int id);
    Task<List<HistoricalSecret>> GetHistorySecretList();


}