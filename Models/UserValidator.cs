using System;
using System.Text.RegularExpressions;

namespace API_TICKET_APPLICATION.Models
{
    public static class UserValidator
    {
        public static (bool IsValid, string? ErrorMessage) ValidateRegistration(User user, string password)
        {
            if (user == null) return (false, "Dữ liệu người dùng không được để trống");

            if (string.IsNullOrWhiteSpace(user.FullName))
                return (false, "Họ tên không được để trống");

            if (string.IsNullOrWhiteSpace(user.Email))
                return (false, "Email không được để trống");

            if (!IsValidEmail(user.Email))
                return (false, "Định dạng email không hợp lệ");

            if (string.IsNullOrWhiteSpace(password))
                return (false, "Mật khẩu không được để trống");

            if (password.Length < 6)
                return (false, "Mật khẩu phải có ít nhất 6 ký tự");

            return (true, null);
        }

        public static (bool IsValid, string? ErrorMessage) ValidateLogin(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Email không được để trống");

            if (string.IsNullOrWhiteSpace(password))
                return (false, "Mật khẩu không được để trống");

            return (true, null);
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
