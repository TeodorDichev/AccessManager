using AccessManager.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace AccessManager.Services
{
    public class PasswordService
    {
        private readonly PasswordHasher<object> _hasher = new();

        public string HashPassword(User user, string? password)
        {
            if(string.IsNullOrEmpty(password))
            {
                return string.Empty;
            }

            return _hasher.HashPassword(user, password);
        }

        public bool VerifyPassword(User user, string password, string storedHash)
        {
            var result = _hasher.VerifyHashedPassword(user, storedHash, password);
            return result == PasswordVerificationResult.Success;
        }
    }
}
