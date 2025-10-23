namespace AlawnehEway.Models
{
    public enum PartyType
    {
        Sender = 1,
        Beneficiary = 2
    }

    public class Party
    {
        public int Id { get; set; }
        public string NationalId { get; set; } = string.Empty; // رقم المعرف
        public string NameAr { get; set; } = string.Empty; // الاسم بالعربية
        public string NameEn { get; set; } = string.Empty; // الاسم بالإنجليزية
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string Address { get; set; } = string.Empty;
        public PartyType Type { get; set; }
        public DateTime? LastModifiedAt { get; set; } // تاريخ آخر تعديل
    }
}


