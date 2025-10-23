using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlawnehEway.Models
{
    public class Remittance
    {
        public int Id { get; set; }

        [Required]
        public int SenderId { get; set; }

        [Required]
        public int BeneficiaryId { get; set; }

        [ForeignKey(nameof(SenderId))]
        public Party? Sender { get; set; }

        [ForeignKey(nameof(BeneficiaryId))]
        public Party? Beneficiary { get; set; }

        // معرف الصندوق المرسل (User)
        public int? SenderUserId { get; set; }

        // معرف الصندوق المستقبل (User)
        public int? ReceiverUserId { get; set; }

        [ForeignKey(nameof(SenderUserId))]
        public User? SenderUser { get; set; }

        [ForeignKey(nameof(ReceiverUserId))]
        public User? ReceiverUser { get; set; }

        [Required]
        [MaxLength(100)]
        public string Country { get; set; } = string.Empty; // الدولة المستقبلة

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } // مبلغ الحوالة بالدينار الأردني

        [Column(TypeName = "decimal(18,2)")]
        public decimal Fee { get; set; } // العمولة المحسوبة

        [MaxLength(100)]
        public string Reason { get; set; } = string.Empty; // سبب الحوالة

        [MaxLength(100)]
        public string Purpose { get; set; } = string.Empty; // الغاية من الحوالة

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // رقم مرجعي فريد للحوالة (مثال: ddMMyyyy + 5 أرقام)
        [MaxLength(32)]
        public string Reference { get; set; } = string.Empty;

        // الحالة: "Payment pending" أو "Paid"
        [MaxLength(50)]
        public string Status { get; set; } = "Payment pending";

        // تاريخ السحب/الدفع
        public DateTime? PaidAt { get; set; }
    }
}


