using System.ComponentModel.DataAnnotations;

namespace LocalVault_Api.Data;

public class LocalSecret
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Key { get; set; }
    [Required]
    public string Value { get; set; }
    [Required]
    public DateTime CreateDate { get; set; }
}