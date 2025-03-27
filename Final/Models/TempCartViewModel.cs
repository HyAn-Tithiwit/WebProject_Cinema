using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Final.Models
{
	public class TempCartViewModel
	{
        public int ItemID { get; set; }
        public int ReservationID { get; set; }
        public string SessionID { get; set; }
        public int? CustomerID { get; set; }
        public int ScreeningID { get; set; }
        public int MovieID { get; set; }
        public string MovieTitle { get; set; }
        public int CinemaID { get; set; }
        public string CinemaName { get; set; }
        public int? SeatID { get; set; }
        public string SeatNumber { get; set; }
        public int? TicketTypeID { get; set; }
        public string TicketTypeName { get; set; }
        public double Price { get; set; }
        public string ItemType { get; set; }
        public int? FoodID { get; set; }
        public int Quantity { get; set; }
        public DateTime? ReservedUntil { get; set; }
        public DateTime? ScreeningTime { get; set; }  // ✅ Cho phép null

        // ✅ Thêm ExpirationTime để quản lý thời gian hết hạn của TempCart
        public DateTime? ExpirationTime { get; set; }
        public string FoodName { get; set; }
        public int FoodQuantity { get; set; }
    }
}