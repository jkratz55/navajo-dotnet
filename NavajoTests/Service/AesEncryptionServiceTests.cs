using System.Text;
using Microsoft.Extensions.Configuration;
using navajo_dotnet.Service;

namespace NavajoTests.Service;

public class AesEncryptionServiceTests
{
    // This is a randomly generated key for testing purposes. Do not use this key for anything
    // other than unit tests.
    private const string Key = "MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDE=";
    
    private readonly IEncryptionService _encryptionService;

    public AesEncryptionServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AES_ENCRYPTION_KEY"] = Key
            })
            .Build();

        _encryptionService = new AesEncryptionService(configuration);
    }

    [Theory]
    [InlineData("Hello, World!")]
    [InlineData("")]
    [InlineData("12345678901234567890")] // Test longer string
    [InlineData("!@#$%^&*()")] // Test special characters
    public void EncryptDecrypt_ShouldReturnOriginalValue(string originalValue)
    {
        // Arrange + Act
        var (cipherText, iv) = _encryptionService.Encrypt(originalValue);
        var decryptedValue = _encryptionService.Decrypt(cipherText, iv);

        // Assert
        Assert.Equal(originalValue, decryptedValue);
    }

    [Fact]
    public void Constructor_WithMissingKey_ShouldThrowException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>())
            .Build();

        // Act + Assert
        var exception = Assert.Throws<Exception>(() => new AesEncryptionService(configuration));
        Assert.Equal("AES_ENCRYPTION_KEY is not set", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidKey_ShouldThrowException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AES_ENCRYPTION_KEY"] = "not-a-valid-base64-string"
            })
            .Build();

        // Act + Assert
        Assert.Throws<FormatException>(() => new AesEncryptionService(configuration));
    }

    [Fact]
    public void Decrypt_WithInvalidCipherText_ShouldThrowException()
    {
        // Arrange
        var (_, iv) = _encryptionService.Encrypt("test");

        // Act + Assert
        Assert.Throws<FormatException>(() => 
            _encryptionService.Decrypt("not-a-valid-base64-string", iv));
    }

    [Fact]
    public void Decrypt_WithInvalidIV_ShouldThrowException()
    {
        // Arrange
        var (cipherText, _) = _encryptionService.Encrypt("test");

        // Act + Assert
        Assert.Throws<FormatException>(() => 
            _encryptionService.Decrypt(cipherText, "not-a-valid-base64-string"));
    }

    [Fact]
    public void DifferentEncryptions_ShouldProduceDifferentResults()
    {
        // Arrange
        var value = "Hello, World!";

        // Act
        var (cipherText1, iv1) = _encryptionService.Encrypt(value);
        var (cipherText2, iv2) = _encryptionService.Encrypt(value);

        // Assert
        Assert.NotEqual(cipherText1, cipherText2); // Different because of random IV
        Assert.NotEqual(iv1, iv2); // IVs should be different
    }

    [Fact]
    public void EncryptedValue_ShouldBeDifferentFromOriginal()
    {
        // Arrange
        var originalValue = "Hello, World!";

        // Act
        var (cipherText, _) = _encryptionService.Encrypt(originalValue);
        var decodedCipherText = Convert.FromBase64String(cipherText);

        // Assert
        Assert.NotEqual(originalValue, cipherText);
        Assert.NotEqual(originalValue, Encoding.UTF8.GetString(decodedCipherText));
    }

}