using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace AlawnehEway.Models
{
    public enum ExchangeType
    {
        Buy = 1,    // شراء (الصندوق يشتري العملة من العميل)
        Sell = 2    // بيع (الصندوق يبيع العملة للعميل)
    }

    public class CurrencyExchange
    {
        public int Id { get; set; }

        // نوع العملية: شراء أو بيع
        public ExchangeType Type { get; set; }

        // العملة 
        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "USD";

        // المبلغ بالعملة 
        [Precision(18, 2)]
        [Range(0.01, double.MaxValue)]
        public decimal ForeignAmount { get; set; }

        // سعر الصرف المستخدم
        [Precision(18, 5)]
        [Range(0.00001, double.MaxValue)]
        public decimal ExchangeRate { get; set; }

        // المبلغ بالدينار الأردني
        [Precision(18, 2)]
        public decimal JodAmount { get; set; }

        // الربح/الخسارة من العملية
        [Precision(18, 2)]
        public decimal Profit { get; set; } = 0;

        // رقم الهوية للعميل
        [MaxLength(50)]
        public string? CustomerNationalId { get; set; }

        // اسم العميل
        [MaxLength(200)]
        public string? CustomerName { get; set; }

        // رقم هاتف العميل
        [MaxLength(20)]
        public string? CustomerPhone { get; set; }

        // معرف الصندوق الذي قام بالعملية
        public int CashierId { get; set; }
        public User? Cashier { get; set; }

        // تاريخ العملية
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ملاحظات
        [MaxLength(500)]
        public string? Notes { get; set; }

        // الدولة (اختياري - للربط مع أسعار الصرف)
        [MaxLength(100)]
        public string? Country { get; set; }

        // رقم مرجعي فريد لعملية الصرف (مثال: CE-ddMMyyyy-HHmmss)
        [MaxLength(32)]
        public string Reference { get; set; } = string.Empty;
    }
}

