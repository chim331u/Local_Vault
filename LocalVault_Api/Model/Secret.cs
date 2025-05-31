namespace LocalVault_Api.Model;

public class Secret
{
    public int Id { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
    public DateTime CreateDate { get; set; }
}

public class SecretRequest
{
    public string Key { get; set; }
    public string Value { get; set; } 
    public DateTime CreateDate { get; set; }
}