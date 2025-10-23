using System.ComponentModel.DataAnnotations;

namespace AlawnehEway.Models
{
    public enum ChangeRequestType
    {
        ReturnToPending = 1,
        UpdateDetails = 2
    }

    public enum ChangeRequestStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }

    public class RemittanceChangeRequest
    {
        public int Id { get; set; }
        [Required]
        public int RemittanceId { get; set; }
        public Remittance? Remittance { get; set; }

        [Required]
        public ChangeRequestType RequestType { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // بيانات مقترحة للتعديل (اختيارية عند UpdateDetails)
        public string? ProposedCountry { get; set; }
        public decimal? ProposedAmount { get; set; }
        public string? ProposedReason { get; set; }
        public string? ProposedPurpose { get; set; }

        public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
    }
}


