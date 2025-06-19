using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace navajo_dotnet.Domain;

[Table("secrets")]
public class Secret
{
    [Key]
    [Column("id")]
    public Guid Id { get; protected set; }
    
    [Required]
    [MinLength(1)]
    [MaxLength(10000)]
    [Column("secret")]
    public byte[] Value { get; protected set; }
    
    [Required]
    [Column("nonce")]
    public byte[] Nonce { get; protected set; }
    
    [Required]
    [Column("claimed")]
    public bool Claimed { get; protected set; }
    
    [Required]
    [Column("expires_at")]
    public DateTimeOffset ExpiresAt { get; protected set; } 
    
    [Required]
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; protected set; }
    
    protected Secret() { }

    public Secret(string value, string nonce)
    {
        Id = Guid.NewGuid();
        Value = System.Text.Encoding.UTF8.GetBytes(value);
        Nonce = System.Text.Encoding.UTF8.GetBytes(nonce);
        Claimed = false;
        ExpiresAt = DateTime.UtcNow.AddHours(1);
        CreatedAt = DateTime.UtcNow;
    }

    public bool IsExpired()
    {
        return DateTime.UtcNow > ExpiresAt;
    }

    public bool IsClaimed()
    {
        return Claimed;
    }
    
    public void MarkAsClaimed()
    {
        Claimed = true;
    }
}