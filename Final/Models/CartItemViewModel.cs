using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Final.Models
{
	public class CartItemViewModel
    {
        public int OrderDetailID { get; set; } // ID của chi tiết đơn hàng (nếu có)
        public string ItemType { get; set; }   // "Ticket" hoặc "Food"
        public int Quantity { get; set; }      // Số lượng
        public double Price { get; set; }      // Giá mỗi đơn vị

        // Thông tin vé xem phim
        public string MovieTitle { get; set; }    // Tên phim
        public string CinemaName { get; set; }    // Tên rạp
        public string SeatNumber { get; set; }    // Số ghế
        public DateTime? ScreeningTime { get; set; } // Thời gian chiếu
        public string TicketTypeName { get; set; }   // Loại vé

        // ID liên kết với bảng dữ liệu gốc
        public int? MovieID { get; set; }
        public int? CinemaID { get; set; }
        public int? SeatID { get; set; }
        public int? ScreeningID { get; set; }
        public int? TicketTypeID { get; set; }

        // Thông tin đồ ăn
        public int? FoodID { get; set; } // ID đồ ăn
        public string FoodName { get; set; } // Tên đồ ăn
    }
}