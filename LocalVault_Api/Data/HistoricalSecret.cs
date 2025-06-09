using System.ComponentModel.DataAnnotations;

namespace LocalVault_Api.Data;

public class HistoricalSecret
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Key { get; set; }
    [Required]
    public string Value { get; set; }
    [Required]
    public DateTime SecretCreateDate { get; set; }
    
    public int SecretId { get; set; }
    [Required]
    public DateTime SecretEndDate { get; set; }
}