namespace Survey.Services
{
    /// <summary>
    /// Interface for password hashing operations using secure algorithms
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Hashes a password using PBKDF2 with a random salt
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <returns>Hash string containing salt and hash separated by colon</returns>
        string Hash(string password);

        /// <summary>
        /// Verifies a password against a stored hash
        /// </summary>
        /// <param name="storedHash">Hash string from database (format: salt:hash)</param>
        /// <param name="providedPassword">Plain text password to verify</param>
        /// <returns>True if password matches, false otherwise</returns>
        bool Verify(string storedHash, string providedPassword);
    }
}