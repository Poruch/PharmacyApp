using System;
using System.Security.Cryptography;

namespace PharmacyApp.Services
{
    public static class PasswordHelper
    {
        private const int Iterations = 10000;
        private const int SaltSize = 32; 
        private const int HashSize = 32; 

        public static string GenerateSalt()
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(salt);
        }

        public static string HashPassword(string password, string saltBase64)
        {
            byte[] salt = Convert.FromBase64String(saltBase64);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(HashSize);
                return Convert.ToBase64String(hash);
            }
        }

        public static bool VerifyPassword(string password, string saltBase64, string hashBase64)
        {
            string computedHash = HashPassword(password, saltBase64);
            return computedHash == hashBase64;
        }

        public static bool IsPasswordComplex(string password)
        {
            return password.Length >= 6 &&
                   System.Text.RegularExpressions.Regex.IsMatch(password, @"\d") &&
                   System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Za-z]");
        }
    }
}