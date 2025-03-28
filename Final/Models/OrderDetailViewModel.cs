using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Final.Models
{
    public class OrderDetailViewModel
    {
        public int OrderDetailID { get; set; }
        public string ItemType { get; set; } // "Seat" hoặc "Food"
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public int? FoodQuantity { get; set; } // Ensure this is included

        // Nếu là vé xem phim
        public string MovieTitle { get; set; }
        public string CinemaName { get; set; }
        public string SeatNumber { get; set; }
        public DateTime? ScreeningTime { get; set; }
        public string TicketTypeName { get; set; }

        // Nếu là đồ ăn
        public string FoodName { get; set; }
    }

}