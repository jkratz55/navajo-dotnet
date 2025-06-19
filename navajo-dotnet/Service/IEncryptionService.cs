namespace navajo_dotnet.Service;

public interface IEncryptionService
{
    (string cipherText, string iv) Encrypt(string value);
    string Decrypt(string value, string iv);
}