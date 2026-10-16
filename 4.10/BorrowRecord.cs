
using System;

namespace LabEquipmentCheckout
{
    public class BorrowRecord
    {
        public string BorrowId { get; set; } = "";
        public string StudentId { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string Equipment { get; set; } = "";
        public int Quantity { get; set; } = 1;
        public DateTime BorrowDate { get; set; } = DateTime.Today;
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(7);
        public DateTime? ActualReturnDate { get; set; }
        public string Status { get; set; } = "Đang mượn";
    }
}
