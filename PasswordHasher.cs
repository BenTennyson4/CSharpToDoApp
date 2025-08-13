using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

public static class PasswordHasher
{
    // Generates a hashed password string: base64(salt + hash)
    public static string HashPassword(string password)
    {
        // Generate a 128-bit salt using a sequence of 
        // cryptographically strong random bytes
        byte[] salt = RandomNumberGenerator.GetBytes(128 / 8); // divide by 8 to convert bits to bytes

        // Derive a 256-bit subkey using HMACSHA256 with 100,000 iterations
        byte[] hash = KeyDerivation.Pbkdf2(
            password: password!,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8
        );

        // Combine salt + hash
        byte[] result = new byte[salt.Length + hash.Length];
        Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
        Buffer.BlockCopy(hash, 0, result, salt.Length, hash.Length);

        // Convert to base64 string to store in DB
        return Convert.ToBase64String(result);
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        byte[] fullHash = Convert.FromBase64String(storedHash);
        byte[] salt = new byte[16];
        Buffer.BlockCopy(fullHash, 0, salt, 0, salt.Length);

        // Derive a 256-bit subkey using HMACSHA256 with 100,000 iterations
        byte[] hash = KeyDerivation.Pbkdf2(
            password: password!,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8
        );

        // Compare the computed hash to the stored hash
        for (int i = 0; i < hash.Length; i++)
        {
            if (fullHash[i + salt.Length] != hash[i])
                return false;
        }
        return true;
    }
}