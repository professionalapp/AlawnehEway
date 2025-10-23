using System.ComponentModel.DataAnnotations.Schema;

namespace AlawnehEway.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;

        // حقول تسجيل الدخول
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User"; // User, Admin
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // حقول إدارة رصيد الصندوق
        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0; // الرصيد الحالي

        [Column(TypeName = "decimal(18,2)")]
        public decimal InitialBalance { get; set; } = 0; // الرصيد الأولي عند إنشاء الصندوق

        public DateTime? LastBalanceUpdate { get; set; } // آخر تحديث للرصيد
    }
}
