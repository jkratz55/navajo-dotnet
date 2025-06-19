using System.Security.Cryptography;

namespace navajo_dotnet.Service;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    
    public AesEncryptionService(IConfiguration configuration)
    {
        var aesKey = configuration["AES_ENCRYPTION_KEY"] 
            ?? throw new Exception("AES_ENCRYPTION_KEY is not set");
        _key = Convert.FromBase64String(aesKey);
    }
    
    public (string cipherText, string iv) Encrypt(string value)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        var iv = aes.IV;
        
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(value);
            sw.Flush();
        }

        var ciphertext = Convert.ToBase64String(ms.ToArray());
        var ivBase64 = Convert.ToBase64String(iv);

        return (ciphertext, ivBase64);

    }

    public string Decrypt(string base64EncodedValue, string base64EncodedIv)
    {
        var ciphertext = Convert.FromBase64String(base64EncodedValue);
        var iv = Convert.FromBase64String(base64EncodedIv);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(ciphertext);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}