using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace AlawnehEway.Models
{
    public class FxExchangeRate
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "USD";

        [Required]
        [Precision(18, 5)]
        [Range(0.00001, double.MaxValue, ErrorMessage = "سعر الصرف يجب أن يكون أكبر من صفر")]
        public decimal BuyRate { get; set; } // سعر الشراء (الصندوق يشتري العملة)

        [Required]
        [Precision(18, 5)]
        [Range(0.00001, double.MaxValue, ErrorMessage = "سعر الصرف يجب أن يكون أكبر من صفر")]
        public decimal SellRate { get; set; } // سعر البيع (الصندوق يبيع العملة)

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastModifiedAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // معرف الصندوق الذي أضاف/عدل السعر
        public int? CashierId { get; set; }
        public User? Cashier { get; set; }
    }
}
