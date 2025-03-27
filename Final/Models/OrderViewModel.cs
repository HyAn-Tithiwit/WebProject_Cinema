using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Final.Models
{
	public class OrderViewModel
	{
        public int OrderID { get; set; }
        private DateTime _orderDate = DateTime.Now;
        public DateTime OrderDate
        {
            get { return _orderDate; }
            set
            {
                // Kiểm tra nếu giá trị nhỏ hơn 1753-01-01, gán lại DateTime.Now
                if (value < new DateTime(1753, 1, 1))
                    _orderDate = DateTime.Now;
                else
                    _orderDate = value;
            }
        }
        public string PaymentStatus { get; set; }
        public double TotalAmount { get; set; }
        public List<OrderDetailViewModel> OrderDetails { get; set; }
    }
}