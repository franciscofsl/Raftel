using System.Security.Cryptography;
using System.Text;
using Raftel.Domain.Abstractions;

namespace Raftel.Domain.ValueObjects;

/// <summary>
/// Represents an encrypted string value object that provides secure storage and retrieval of sensitive data.
/// The string is encrypted when stored and only decrypted in memory when explicitly accessed.
/// </summary>
public sealed record EncryptedString
{
    private const int MaxLength = 2000; // Allows for encrypted connection strings
    private const string DefaultEncryptionKey = "RaftelDefaultKey12345678901234567890"; // 32 bytes for AES-256
    
    private readonly string _encryptedValue;
    private readonly string _encryptionKey;

    /// <summary>
    /// Initializes a new instance of the EncryptedString with an already encrypted value.
    /// This constructor is used for persistence scenarios.
    /// </summary>
    /// <param name="encryptedValue">The already encrypted value.</param>
    /// <param name="encryptionKey">The encryption key used for decryption.</param>
    private EncryptedString(string encryptedValue, string encryptionKey)
    {
        _encryptedValue = encryptedValue;
        _encryptionKey = encryptionKey;
    }

    /// <summary>
    /// Creates a new EncryptedString from a plain text value.
    /// </summary>
    /// <param name="plainTextValue">The plain text value to encrypt.</param>
    /// <param name="encryptionKey">Optional encryption key. If not provided, uses default key.</param>
    /// <returns>A Result containing the EncryptedString or an error.</returns>
    public static Result<EncryptedString> Create(string plainTextValue, string encryptionKey = null)
    {
        if (plainTextValue is null)
        {
            return Result.Failure<EncryptedString>(new Error("EncryptedString.NullValue", "Plain text value cannot be null"));
        }

        if (string.IsNullOrEmpty(plainTextValue))
        {
            return Result.Success(new EncryptedString(string.Empty, encryptionKey ?? DefaultEncryptionKey));
        }

        var keyToUse = encryptionKey ?? DefaultEncryptionKey;
        
        if (keyToUse.Length < 32)
        {
            return Result.Failure<EncryptedString>(new Error("EncryptedString.InvalidKey", "Encryption key must be at least 32 characters long"));
        }

        try
        {
            var encryptedValue = Encrypt(plainTextValue, keyToUse);
            
            if (encryptedValue.Length > MaxLength)
            {
                return Result.Failure<EncryptedString>(new Error("EncryptedString.TooLong", $"Encrypted value exceeds maximum length of {MaxLength} characters"));
            }

            return Result.Success(new EncryptedString(encryptedValue, keyToUse));
        }
        catch (Exception ex)
        {
            return Result.Failure<EncryptedString>(new Error("EncryptedString.EncryptionFailed", $"Failed to encrypt value: {ex.Message}"));
        }
    }

    /// <summary>
    /// Creates an EncryptedString from an already encrypted database value.
    /// Used during persistence layer operations.
    /// </summary>
    /// <param name="encryptedValue">The encrypted value from the database.</param>
    /// <param name="encryptionKey">Optional encryption key. If not provided, uses default key.</param>
    /// <returns>A Result containing the EncryptedString or an error.</returns>
    public static Result<EncryptedString> FromEncrypted(string encryptedValue, string encryptionKey = null)
    {
        if (encryptedValue is null)
        {
            return Result.Failure<EncryptedString>(new Error("EncryptedString.NullValue", "Encrypted value cannot be null"));
        }

        var keyToUse = encryptionKey ?? DefaultEncryptionKey;
        
        if (keyToUse.Length < 32)
        {
            return Result.Failure<EncryptedString>(new Error("EncryptedString.InvalidKey", "Encryption key must be at least 32 characters long"));
        }

        return Result.Success(new EncryptedString(encryptedValue, keyToUse));
    }

    /// <summary>
    /// Decrypts and returns the plain text value.
    /// This is the only method that exposes the decrypted value in memory.
    /// </summary>
    /// <returns>The decrypted plain text value.</returns>
    public string GetDecryptedValue()
    {
        if (string.IsNullOrEmpty(_encryptedValue))
        {
            return string.Empty;
        }

        try
        {
            return Decrypt(_encryptedValue, _encryptionKey);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to decrypt value: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Returns the encrypted value for persistence.
    /// This method is used by Entity Framework for database storage.
    /// </summary>
    /// <returns>The encrypted value.</returns>
    public string GetEncryptedValue() => _encryptedValue;

    /// <summary>
    /// Checks if the encrypted string has a value (is not null or empty).
    /// </summary>
    /// <returns>True if the encrypted string has a value, false otherwise.</returns>
    public bool HasValue() => !string.IsNullOrEmpty(_encryptedValue);

    /// <summary>
    /// Implicit conversion to string returns the encrypted value for persistence.
    /// </summary>
    /// <param name="encryptedString">The EncryptedString to convert.</param>
    public static implicit operator string(EncryptedString encryptedString) => encryptedString._encryptedValue;

    /// <summary>
    /// Encrypts a plain text value using AES-256 encryption.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt.</param>
    /// <param name="key">The encryption key.</param>
    /// <returns>The encrypted value as a base64 string.</returns>
    private static string Encrypt(string plainText, string key)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key[..32]); // Use first 32 characters for AES-256
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        
        // Write IV first
        ms.Write(aes.IV, 0, aes.IV.Length);
        
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var writer = new StreamWriter(cs))
        {
            writer.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// Decrypts an encrypted value using AES-256 decryption.
    /// </summary>
    /// <param name="cipherText">The encrypted value as a base64 string.</param>
    /// <param name="key">The decryption key.</param>
    /// <returns>The decrypted plain text value.</returns>
    private static string Decrypt(string cipherText, string key)
    {
        var buffer = Convert.FromBase64String(cipherText);
        
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key[..32]); // Use first 32 characters for AES-256
        
        // Extract IV from the beginning of the buffer
        var iv = new byte[aes.BlockSize / 8];
        Array.Copy(buffer, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cs);
        
        return reader.ReadToEnd();
    }
}