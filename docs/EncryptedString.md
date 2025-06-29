# EncryptedString Value Object Documentation

## Overview

The `EncryptedString` Value Object provides secure storage and retrieval of sensitive string data, particularly designed for storing database connection strings in the Tenant entity. It ensures that sensitive data is always stored encrypted in the database and only decrypted in memory when explicitly needed.

## Features

- **AES-256 Encryption**: Uses industry-standard AES-256 encryption with unique initialization vectors (IVs)
- **Secure Storage**: Connection strings are always stored encrypted in the database
- **Memory-Only Decryption**: Decryption only occurs in memory when explicitly requested
- **Configurable Keys**: Supports custom encryption keys for enhanced security
- **Entity Framework Integration**: Seamlessly integrates with EF Core for persistence

## Usage

### Creating an EncryptedString

```csharp
// Create from plain text (encrypts automatically)
var result = EncryptedString.Create("Server=localhost;Database=MyDb;User Id=sa;Password=secret;");
if (result.IsSuccess)
{
    var encryptedString = result.Value;
}

// Create with custom encryption key
var customKeyResult = EncryptedString.Create(connectionString, "MyCustomEncryptionKey123456789012");
```

### Creating from Encrypted Database Value

```csharp
// For Entity Framework conversions (used internally)
var result = EncryptedString.FromEncrypted(encryptedDatabaseValue);
```

### Retrieving Values

```csharp
var encryptedString = EncryptedString.Create("my connection string").Value;

// Get decrypted value (only time plaintext is in memory)
string plainText = encryptedString.GetDecryptedValue();

// Get encrypted value for storage (used by Entity Framework)
string encryptedValue = encryptedString.GetEncryptedValue();

// Check if has value
bool hasValue = encryptedString.HasValue();
```

## Tenant Entity Integration

The `Tenant` entity now includes an optional `ConnectionString` property of type `EncryptedString`:

```csharp
// Create tenant with connection string
var tenant = Tenant.Create(
    name: "My Tenant",
    code: "TENANT_CODE",
    description: "Description",
    connectionString: "Server=localhost;Database=TenantDb;User Id=sa;Password=secure;"
).Value;

// Get the decrypted connection string for database operations
string connectionString = tenant.GetConnectionString();
```

### Application Layer

The `CreateTenantCommand` has been updated to include an optional connection string:

```csharp
var command = new CreateTenantCommand(
    Name: "Tenant Name",
    Code: "TENANT_CODE", 
    Description: "Description",
    ConnectionString: "Server=localhost;Database=TenantDb;User Id=sa;Password=secure;"
);
```

## Security Considerations

1. **Encryption Key Management**: In production, use a secure key management system instead of the default key
2. **Key Rotation**: Implement key rotation strategies for enhanced security
3. **Memory Management**: The plaintext value is only exposed when `GetDecryptedValue()` is called
4. **Transport Security**: Always use secure connections (HTTPS/TLS) when transmitting encrypted data

## Entity Framework Configuration

The `EncryptedString` is automatically configured for Entity Framework Core in `TenantConfiguration`:

```csharp
builder.Property(x => x.ConnectionString)
    .HasConversion(
        encryptedString => encryptedString != null ? encryptedString.GetEncryptedValue() : null,
        value => value != null ? EncryptedString.FromEncrypted(value, null).Value : null
    )
    .HasMaxLength(2000)
    .IsRequired(false);
```

This ensures that:
- Encrypted values are stored in the database
- Values are properly converted during persistence and retrieval
- The column allows for encrypted string size (up to 2000 characters)
- The property is optional (nullable)

## Database Migration

When upgrading existing databases, add a migration to include the new `ConnectionString` column:

```sql
ALTER TABLE Tenant ADD ConnectionString nvarchar(2000) NULL;
```

## Best Practices

1. Always use `GetConnectionString()` on the Tenant entity instead of directly accessing the encrypted value
2. Minimize the time plaintext values spend in memory
3. Use custom encryption keys in production environments
4. Regularly audit and rotate encryption keys
5. Consider using hardware security modules (HSMs) for key storage in high-security environments