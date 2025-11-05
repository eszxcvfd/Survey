using System.Security.Cryptography;
using System.Text;

namespace Survey.Services
{
    /// <summary>
    /// Password hasher implementation using PBKDF2 (Password-Based Key Derivation Function 2)
    /// PBKDF2 is recommended by NIST and is designed to be computationally expensive
    /// to defend against brute-force attacks
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        // PBKDF2 configuration
        private const int SaltSize = 32; // 256 bits
        private const int HashSize = 32; // 256 bits
        private const int Iterations = 100000; // OWASP recommendation (100,000+ iterations)
        private readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        /// <summary>
        /// Hashes a password using PBKDF2 with a cryptographically random salt
        /// </summary>
        public string Hash(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password cannot be null or empty", nameof(password));
            }

            // Generate a cryptographically random salt
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            // Hash the password with PBKDF2
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password: Encoding.UTF8.GetBytes(password),
                salt: salt,
                iterations: Iterations,
                hashAlgorithm: Algorithm,
                outputLength: HashSize
            );

            // Combine salt and hash for storage
            // Format: base64(salt):base64(hash)
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Verifies a password against a stored hash using constant-time comparison
        /// </summary>
        public bool Verify(string storedHash, string providedPassword)
        {
            if (string.IsNullOrEmpty(storedHash))
            {
                throw new ArgumentException("Stored hash cannot be null or empty", nameof(storedHash));
            }

            if (string.IsNullOrEmpty(providedPassword))
            {
                return false;
            }

            try
            {
                // Extract salt and hash from stored hash
                string[] parts = storedHash.Split(':');
                if (parts.Length != 2)
                {
                    return false;
                }

                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] hash = Convert.FromBase64String(parts[1]);

                // Hash the provided password with the same salt
                byte[] providedHash = Rfc2898DeriveBytes.Pbkdf2(
                    password: Encoding.UTF8.GetBytes(providedPassword),
                    salt: salt,
                    iterations: Iterations,
                    hashAlgorithm: Algorithm,
                    outputLength: HashSize
                );

                // Use constant-time comparison to prevent timing attacks
                return CryptographicOperations.FixedTimeEquals(hash, providedHash);
            }
            catch
            {
                // If any error occurs during verification, return false
                return false;
            }
        }
    }
}