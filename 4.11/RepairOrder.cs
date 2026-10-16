
using System;

namespace RepairServiceTracker
{
    public class RepairOrder
    {
        public string OrderId { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string DeviceType { get; set; } = "";
        public string FaultDescription { get; set; } = "";
        public DateTime ReceivedDate { get; set; } = DateTime.Today;
        public decimal EstimatedCost { get; set; }
        public string Status { get; set; } = "Mới tiếp nhận";
        public string ImagePath { get; set; } = "";
    }
}
