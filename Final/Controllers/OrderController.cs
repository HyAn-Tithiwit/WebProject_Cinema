using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using Final.Models;
using static System.Collections.Specialized.BitVector32;

namespace Final.Controllers
{
    public class OrderController : Controller
    {
        private DataClassesDataContext db = new DataClassesDataContext();

        // Action hiển thị giỏ hàng (Cart) cho đơn hàng đã tạo (nếu có)
        public ActionResult Cart()
        {
            int? customerId = Session["CustomerID"] as int?;
            string sessionId = Session.SessionID;
            List<CartItemViewModel> cartItems = new List<CartItemViewModel>();

            if (customerId == null || customerId == 0)
            {
                // Lấy giỏ hàng từ TemporaryReservations (chưa đăng nhập)
                cartItems = db.TemporaryReservations
                              .Where(tr => tr.SessionID == sessionId)
                              .Select(tr => new CartItemViewModel
                              {
                                  ItemType = tr.ItemType,
                                  Quantity = (int)tr.Quantity,
                                  Price = (double)tr.Price,
                                  MovieTitle = tr.MovieTitle,
                                  CinemaName = tr.CinemaName,
                                  SeatNumber = tr.SeatNumber,
                                  FoodName = tr.FoodID.HasValue
                                             ? db.Foods.FirstOrDefault(f => f.FoodID == tr.FoodID).FoodName
                                             : "",
                                  ScreeningTime = tr.ScreeningTime
                              })
                              .ToList();
            }
            else
            {
                // Nếu có giỏ hàng tạm khi đăng nhập, hiển thị nó
                var tempReservations = db.TemporaryReservations.Where(tc => tc.SessionID == sessionId).ToList();
                if (tempReservations.Any())
                {
                    cartItems = tempReservations.Select(tr => new CartItemViewModel
                    {
                        ItemType = tr.ItemType,
                        Quantity = (int)tr.Quantity,
                        Price = (double)tr.Price,
                        MovieTitle = tr.MovieTitle,
                        CinemaName = tr.CinemaName,
                        SeatNumber = tr.SeatNumber,
                        FoodName = tr.FoodID.HasValue
                                   ? db.Foods.FirstOrDefault(f => f.FoodID == tr.FoodID).FoodName
                                   : "",
                        ScreeningTime = tr.ScreeningTime
                    }).ToList();
                }
                else
                {
                    // Nếu không có giỏ hàng tạm, lấy từ OrderDetails
                    var pendingOrder = db.Orders.FirstOrDefault(o => o.CustomerID == customerId && o.PaymentStatus == "Pending");
                    if (pendingOrder != null)
                    {
                        cartItems = db.OrderDetails
                                      .Where(od => od.OrderID == pendingOrder.OrderID)
                                      .Select(od => new CartItemViewModel
                                      {
                                          ItemType = od.ItemType,
                                          Quantity = od.Quantity,
                                          Price = od.Price,
                                          MovieTitle = db.Movies.FirstOrDefault(m => m.MovieID == od.MovieID).Title,
                                          CinemaName = db.Cinemas.FirstOrDefault(c => c.CinemaID == od.CinemaID).Name,
                                          SeatNumber = db.Seats.FirstOrDefault(s => s.SeatID == od.SeatID).SeatNumber,
                                          FoodName = od.FoodID.HasValue
                                                     ? db.Foods.FirstOrDefault(f => f.FoodID == od.FoodID).FoodName
                                                     : "",
                                          ScreeningTime = od.ScreeningTime
                                      })
                                      .ToList();
                    }
                }
            }

            return View(cartItems);
        }
        public ActionResult TemporaryCart()
        {
            string sessionId = Session.SessionID;

            var cartItems = db.TemporaryReservations
                .Where(tr => tr.SessionID == sessionId && tr.ExpirationTime > DateTime.Now)
                .Select(tr => new TempCartViewModel
                {
                    ReservationID = tr.ReservationID,
                    MovieTitle = tr.MovieTitle,
                    CinemaName = tr.CinemaName,
                    SeatNumber = tr.SeatID != null ? db.Seats.Where(s => s.SeatID == tr.SeatID).Select(s => s.SeatNumber).FirstOrDefault() : "-",
                    TicketTypeName = tr.TicketTypeID != null ? db.TicketTypes.Where(t => t.TicketTypeID == tr.TicketTypeID).Select(t => t.TypeName).FirstOrDefault() : "-",
                    Price = tr.Price,
                    ScreeningTime = tr.ScreeningTime,
                    FoodName = tr.FoodID != null ? db.Foods.Where(f => f.FoodID == tr.FoodID).Select(f => f.FoodName).FirstOrDefault() : "-",
                    FoodQuantity = tr.FoodQuantity ?? 0,
                    ExpirationTime = tr.ExpirationTime
                })
                .ToList();

            return View(cartItems);
        }


        [HttpPost]
        public ActionResult RemoveItem(int id)
        {
            var item = db.OrderDetails.FirstOrDefault(od => od.OrderDetailID == id);
            if (item != null)
            {
                db.OrderDetails.DeleteOnSubmit(item);
                db.SubmitChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // Action chuyển giỏ hàng tạm thời thành đơn hàng khi người dùng đã đăng nhập
        [HttpPost]
        public ActionResult Checkout()
        {
            int? customerId = Session["CustomerID"] as int?;
            if (customerId == null || customerId == 0)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thanh toán!" });
            }

            string sessionId = Session.SessionID;

            // Retrieve temporary reservations from the guest cart or by customer ID
            var tempReservations = db.TemporaryReservations
                                     .Where(tc => tc.SessionID == sessionId || (customerId != null && tc.CustomerID == customerId))
                                     .ToList();

            if (!tempReservations.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng trống!" });
            }

            if (db.Connection.State == System.Data.ConnectionState.Closed)
            {
                db.Connection.Open();
            }

            using (var transaction = db.Connection.BeginTransaction())
            {
                try
                {
                    db.Transaction = transaction;

                    // Look for an existing pending order
                    var existingOrder = db.Orders.FirstOrDefault(o => o.CustomerID == customerId && o.PaymentStatus == "Pending");

                    if (existingOrder == null)
                    {
                        existingOrder = new Order
                        {
                            CustomerID = customerId.Value,
                            OrderDate = DateTime.Now,
                            PaymentStatus = "Pending"
                        };
                        db.Orders.InsertOnSubmit(existingOrder);
                        db.SubmitChanges();
                    }

                    // Transfer data from TemporaryReservations into OrderDetails
                    foreach (var temp in tempReservations)
                    {
                        db.OrderDetails.InsertOnSubmit(new OrderDetail
                        {
                            OrderID = existingOrder.OrderID,
                            ScreeningTime = temp.ScreeningTime,
                            SeatID = temp.SeatID,
                            FoodID = temp.FoodID,
                            TicketTypeID = temp.TicketTypeID,
                            ItemType = temp.ItemType,
                            Quantity = (int)temp.Quantity,
                            Price = (double)temp.Price,
                            MovieID = temp.MovieID,
                            CinemaID = temp.CinemaID,
                            MovieTitle = temp.MovieTitle,
                            CinemaName = temp.CinemaName,
                            SeatNumber = temp.SeatNumber,
                            TicketTypeName = temp.TicketTypeName
                        });

                        // If this is a ticket, update the corresponding seat status to "Sold"
                        if (temp.SeatID.HasValue)
                        {
                            var seat = db.Seats.FirstOrDefault(s => s.SeatID == temp.SeatID);
                            if (seat != null)
                            {
                                seat.Status = "Sold";
                            }
                        }
                    }

                    db.SubmitChanges();

                    // Finalize the order: update its PaymentStatus to "Completed"
                    existingOrder.PaymentStatus = "Completed";
                    db.SubmitChanges();

                    // Delete the temporary reservations now that they've been transferred
                    db.TemporaryReservations.DeleteAllOnSubmit(tempReservations);
                    db.SubmitChanges();

                    // *** Xóa các đơn hàng pending cũ (không phải đơn hàng vừa tạo) ***
                    var pendingOrders = db.Orders
                        .Where(o => o.CustomerID == customerId && o.PaymentStatus == "Pending" && o.OrderID != existingOrder.OrderID)
                        .ToList();
                    if (pendingOrders.Any())
                    {
                        db.Orders.DeleteAllOnSubmit(pendingOrders);
                        db.SubmitChanges();
                    }

                    transaction.Commit();

                    return Json(new { success = true, message = "Thanh toán thành công! Đơn hàng đã được tạo. Mã đơn hàng: " + existingOrder.OrderID });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
                }
                finally
                {
                    db.Connection.Close();
                }
            }
        }




        // Action hiển thị lịch sử đơn hàng (Order History)
        public ActionResult OrderHistory()
        {
            if (Session["CustomerID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int customerId = (int)Session["CustomerID"];

            var orderData = (from o in db.Orders
                             where o.CustomerID == customerId
                             join od in db.OrderDetails on o.OrderID equals od.OrderID
                             join m in db.Movies on od.MovieID equals m.MovieID into mGroup
                             from movie in mGroup.DefaultIfEmpty()
                             join c in db.Cinemas on od.CinemaID equals c.CinemaID into cGroup
                             from cinema in cGroup.DefaultIfEmpty()
                             join s in db.Seats on od.SeatID equals s.SeatID into sGroup
                             from seat in sGroup.DefaultIfEmpty()
                             join tt in db.TicketTypes on od.TicketTypeID equals tt.TicketTypeID into ttGroup
                             from ticketType in ttGroup.DefaultIfEmpty()
                             join f in db.Foods on od.FoodID equals f.FoodID into fGroup
                             from food in fGroup.DefaultIfEmpty()
                             orderby o.OrderDate descending
                             select new
                             {
                                 o.OrderID,
                                 o.OrderDate,
                                 o.PaymentStatus,
                                 od.OrderDetailID,
                                 od.ItemType,
                                 od.Quantity,
                                 od.Price,
                                 MovieTitle = movie != null ? movie.Title : "",
                                 CinemaName = cinema != null ? cinema.Name : "",
                                 SeatNumber = seat != null ? seat.SeatNumber : "",
                                 ScreeningTime = od.ScreeningTime,
                                 TicketTypeName = ticketType != null ? ticketType.TypeName : "",
                                 FoodName = food != null ? food.FoodName : ""
                             }).ToList();

            var orderViewModels = orderData.GroupBy(o => new { o.OrderID, o.OrderDate, o.PaymentStatus })
                                           .Select(g => new OrderViewModel
                                           {
                                               OrderID = g.Key.OrderID,
                                               OrderDate = g.Key.OrderDate ?? DateTime.MinValue,
                                               PaymentStatus = g.Key.PaymentStatus,
                                               OrderDetails = g.Select(od => new OrderDetailViewModel
                                               {
                                                   OrderDetailID = od.OrderDetailID,
                                                   ItemType = od.ItemType,
                                                   Quantity = od.Quantity,
                                                   Price = (decimal)od.Price,
                                                   MovieTitle = od.MovieTitle,
                                                   CinemaName = od.CinemaName,
                                                   SeatNumber = od.SeatNumber,
                                                   ScreeningTime = od.ScreeningTime ?? DateTime.MinValue,
                                                   TicketTypeName = od.TicketTypeName,
                                                   FoodName = od.FoodName
                                               }).ToList(),
                                               TotalAmount = (double)g.Sum(od => (decimal)od.Price * od.Quantity)
                                           }).ToList();

            return View(orderViewModels);
        }
    }
}
