using System;
using System.Collections.Generic;
using System.Data.Common;
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
        public ActionResult TemporaryCart(string sessionId = null)
        {
            // Nếu không có sessionId từ client, kiểm tra danh sách trong bảng TemporaryReservations
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = db.TemporaryReservations
                              .OrderByDescending(tr => tr.ExpirationTime) // Lấy session mới nhất
                              .Select(tr => tr.SessionID)
                              .FirstOrDefault();
            }

            if (string.IsNullOrEmpty(sessionId))
            {
                Console.WriteLine("[ERROR] Không tìm thấy SessionID hợp lệ trong database!");
                return View(new List<TempCartViewModel>()); // Trả về danh sách trống
            }

            Console.WriteLine($"[TemporaryCart] Sử dụng SessionID: {sessionId}");

            // Truy vấn tất cả vé và đồ ăn chưa hết hạn trong giỏ hàng tạm thời
            var cartItems = db.TemporaryReservations
                .Where(tr => tr.SessionID == sessionId && tr.ExpirationTime > DateTime.Now)
                .Select(tr => new TempCartViewModel
                {
                    ReservationID = tr.ReservationID,
                    MovieTitle = tr.MovieTitle,
                    CinemaName = tr.CinemaName,

                    // Lấy số ghế nếu có, nếu không thì hiển thị "-"
                    SeatNumber = tr.SeatID.HasValue
                        ? db.Seats.Where(s => s.SeatID == tr.SeatID).Select(s => s.SeatNumber).FirstOrDefault() ?? "-"
                        : "-",

                    // Lấy loại vé nếu có
                    TicketTypeName = tr.TicketTypeID.HasValue
                        ? db.TicketTypes.Where(t => t.TicketTypeID == tr.TicketTypeID).Select(t => t.TypeName).FirstOrDefault() ?? "-"
                        : "-",

                    // Giá vé hoặc đồ ăn
                    Price = tr.Price,

                    // Thời gian suất chiếu
                    ScreeningTime = tr.ScreeningTime,

                    // Lấy tên đồ ăn nếu có
                    FoodName = tr.FoodID.HasValue
                        ? db.Foods.Where(f => f.FoodID == tr.FoodID).Select(f => f.FoodName).FirstOrDefault() ?? "-"
                        : "-",

                    ItemType = tr.ItemType, // "Ticket" hoặc "Food"
                    Quantity = tr.Quantity ?? 1, // Số lượng vé
                    FoodQuantity = tr.FoodQuantity ?? 0, // Số lượng đồ ăn

                    ExpirationTime = tr.ExpirationTime
                })
                .ToList();

            Console.WriteLine($"[TemporaryCart] Tìm thấy {cartItems.Count} mục trong giỏ hàng.");

            // In log để kiểm tra
            foreach (var item in cartItems)
            {
                Console.WriteLine($"[Item] ID={item.ReservationID}, Type={item.ItemType}, Name={item.MovieTitle ?? item.FoodName}, Qty={item.Quantity}, FoodQty={item.FoodQuantity}");
            }

            return View(cartItems);
        }

        public ActionResult GetLatestSessionId()
        {
            var latestSessionId = db.TemporaryReservations
                .Where(tr => tr.ExpirationTime > DateTime.Now)
                .OrderByDescending(tr => tr.ExpirationTime)
                .Select(tr => tr.SessionID)
                .FirstOrDefault();

            return Json(new { sessionId = latestSessionId }, JsonRequestBehavior.AllowGet);
        }

        // Action chuyển giỏ hàng tạm thời thành đơn hàng khi người dùng đã đăng nhập
        public ActionResult Checkout(string sessionId = null)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = db.TemporaryReservations
                            .Where(tr => tr.ExpirationTime > DateTime.Now)
                            .OrderByDescending(tr => tr.ExpirationTime)
                            .Select(tr => tr.SessionID)
                            .FirstOrDefault();

                if (string.IsNullOrEmpty(sessionId))
                {
                    sessionId = Session.SessionID;
                }
            }

            int? customerId = Session["CustomerID"] as int?;
            if (!customerId.HasValue || customerId == 0)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thanh toán!" });
            }

            var tempReservations = db.TemporaryReservations
                .Where(tr => tr.SessionID == sessionId && tr.ExpirationTime > DateTime.Now)
                .ToList();

            if (!tempReservations.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng trống hoặc đã hết hạn!" });
            }

            // Mở connection và bắt đầu transaction
            if (db.Connection.State == System.Data.ConnectionState.Closed)
            {
                db.Connection.Open();
            }

            DbTransaction transaction = db.Connection.BeginTransaction();
            db.Transaction = transaction;

            try
            {
                var order = new Order
                {
                    CustomerID = customerId.Value,
                    OrderDate = DateTime.Now,
                    PaymentStatus = "Pending",
                    ScreeningID = tempReservations.First().ScreeningID,
                    TotalAmount = 0,
                    Email = db.Customers
                            .Where(c => c.CustomerID == customerId.Value)
                            .Select(c => c.Email)
                            .FirstOrDefault()
                };

                db.Orders.InsertOnSubmit(order);
                db.SubmitChanges();

                decimal totalAmount = 0;

                foreach (var temp in tempReservations)
                {
                    decimal itemTotal = (decimal)temp.Price * (temp.Quantity ?? 1);

                    var orderDetail = new OrderDetail
                    {
                        OrderID = order.OrderID,
                        ItemType = temp.ItemType,
                        Quantity = temp.Quantity ?? 1,
                        Price = (double)itemTotal,
                        MovieID = temp.MovieID,
                        CinemaID = temp.CinemaID,
                        SeatID = temp.SeatID,
                        ScreeningID = temp.ScreeningID,
                        ScreeningTime = temp.ScreeningTime,
                        TicketTypeID = temp.TicketTypeID,
                        FoodID = temp.FoodID,
                        MovieTitle = temp.MovieTitle,
                        CinemaName = temp.CinemaName,
                        SeatNumber = temp.SeatNumber,
                        TicketTypeName = temp.TicketTypeName,
                        FoodName = temp.FoodName,
                        FoodQuantity = temp.FoodQuantity ?? 0
                    };

                    db.OrderDetails.InsertOnSubmit(orderDetail);
                    totalAmount += itemTotal;

                    if (temp.ItemType == "Ticket" && temp.SeatID.HasValue)
                    {
                        var seat = db.Seats.FirstOrDefault(s => s.SeatID == temp.SeatID);
                        if (seat != null)
                        {
                            seat.Status = "Sold";
                        }
                    }
                }

                order.TotalAmount = (double)totalAmount;
                order.PaymentStatus = "Completed";
                db.SubmitChanges();

                db.TemporaryReservations.DeleteAllOnSubmit(tempReservations);
                db.SubmitChanges();

                // Commit transaction nếu mọi thứ thành công
                transaction.Commit();

                return Json(new
                {
                    success = true,
                    message = $"Thanh toán thành công! Mã đơn hàng: {order.OrderID}",
                    orderId = order.OrderID
                });
            }
            catch (Exception ex)
            {
                // Rollback transaction nếu có lỗi
                transaction.Rollback();
                return Json(new
                {
                    success = false,
                    message = $"Lỗi hệ thống: {ex.Message}"
                });
            }
            finally
            {
                // Đóng connection và giải phóng transaction
                db.Transaction = null;
                if (db.Connection.State == System.Data.ConnectionState.Open)
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
                                 FoodName = food != null ? food.FoodName : "",
                                 FoodQuantity = od.FoodQuantity // Add this
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
                                                   ScreeningTime = od.ScreeningTime,
                                                   TicketTypeName = od.TicketTypeName,
                                                   FoodName = od.FoodName,
                                                   FoodQuantity = od.FoodQuantity // Now available
                                               }).ToList(),
                                               TotalAmount = (double)g.Sum(od => (decimal)od.Price * od.Quantity)
                                           }).ToList();

            return View(orderViewModels);
        }
    }
}
