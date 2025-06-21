using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace navajo_dotnet.Domain;

[Table("secrets")]
public class Secret
{
    [Key]
    [Column("id")]
    public Guid Id { get; init; }
    
    [Required]
    [MinLength(1)]
    [MaxLength(10000)]
    [Column("secret")]
    public byte[] Value { get; init; }
    
    [Required]
    [Column("nonce")]
    public byte[] Nonce { get; init; }
    
    [Required]
    [Column("claimed")]
    public bool Claimed { get; private set; }
    
    [Required]
    [Column("expires_at")]
    public DateTimeOffset ExpiresAt { get; init; } 
    
    [Required]
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
    
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

    /// <summary>
    /// Determines if the secret has expired based on the current system time and its expiration timestamp.
    /// </summary>
    /// <returns>
    /// Returns true if the secret is expired; otherwise, false.
    /// </returns>
    public bool IsExpired()
    {
        return DateTime.UtcNow > ExpiresAt;
    }

    /// <summary>
    /// Determines if the secret has been claimed.
    /// </summary>
    /// <returns>
    /// Returns true if the secret has been claimed; otherwise, false.
    /// </returns>
    public bool IsClaimed()
    {
        return Claimed;
    }

    /// <summary>
    /// Marks the secret as claimed.
    /// </summary>
    /// <remarks>
    /// This method updates the `Claimed` property of the secret to `true`.
    /// If the secret has already been claimed, an <see cref="InvalidOperationException"/> will be thrown.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the secret has already been claimed.
    /// </exception>
    public void MarkAsClaimed()
    {
        if (IsClaimed())
        {
            throw new InvalidOperationException("Secret has already been claimed.");
        }
        Claimed = true;
    }
}