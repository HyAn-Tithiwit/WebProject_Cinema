using Final.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;

namespace Final.Controllers
{
    public class MovieController : Controller
    {
        DataClassesDataContext db = new DataClassesDataContext();

        // GET: Movie/Index
        [HttpGet]
        public ActionResult Index(int? pageNowShowing, int? pageComingSoon)
        {
            DateTime today = DateTime.Today;

            var nowShowing = db.Movies
                .Where(m => m.ReleaseDate <= today)
                .OrderByDescending(m => m.ReleaseDate)
                .ToPagedList(pageNowShowing ?? 1, 4);

            var comingSoon = db.Movies
                .Where(m => m.ReleaseDate > today)
                .OrderBy(m => m.ReleaseDate)
                .ToPagedList(pageComingSoon ?? 1, 4);

            var firstMovie = nowShowing.FirstOrDefault();
            var trailerLink = firstMovie != null ? firstMovie.TrailerLink : "";

            var viewModel = new HomeIndexViewModel
            {
                NowShowing = nowShowing,
                ComingSoon = comingSoon,
                TrailerLink = trailerLink
            };

            return View(viewModel);
        }

        public ActionResult Show(string category, int? page)
        {
            int pageSize = 8;
            int pageNumber = (page ?? 1);
            IQueryable<Movy> movies;
            DateTime today = DateTime.Now;

            if (category == "NowShowing")
            {
                movies = db.Movies.Where(m => m.ReleaseDate <= today).OrderBy(m => m.Title);
            }
            else if (category == "ComingSoon")
            {
                movies = db.Movies.Where(m => m.ReleaseDate > today).OrderBy(m => m.Title);
            }
            else
            {
                return HttpNotFound();
            }

            ViewBag.Category = category;
            return View(movies.ToPagedList(pageNumber, pageSize));
        }

        [HttpGet]
        public JsonResult SearchMovies(string query)
        {
            var movies = db.Movies
                .Where(m => m.Title.Contains(query))
                .Select(m => new
                {
                    m.MovieID,
                    m.Title,
                    m.Image
                })
                .Take(5)
                .ToList();

            return Json(movies, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Detail(int id)
        {
            var movie = db.Movies
                .Where(m => m.MovieID == id)
                .Select(m => new MovieDetailViewModel
                {
                    MovieID = m.MovieID,
                    Title = m.Title,
                    Genre = m.Genre,
                    Duration = m.Duration ?? 0,
                    AgeRating = m.AgeRating,
                    Description = m.Description,
                    ReleaseDate = m.ReleaseDate,
                    Image = m.Image,
                    TrailerLink = m.TrailerLink,
                    Cinemas = db.Cinemas
                        .Where(c => db.Screenings.Any(s => s.MovieID == id && s.Room.RoomID != null && s.Room.CinemaID == c.CinemaID))
                        .Select(c => new CinemaViewModel
                        {
                            CinemaID = c.CinemaID,
                            Name = c.Name,
                            Address = c.Address
                        }).ToList(),
                    Foods = db.Foods
                        .Select(f => new FoodViewModel
                        {
                            FoodID = f.FoodID,
                            FoodName = f.FoodName,
                            Price = f.Price
                        }).ToList(),
                    TicketTypes = db.TicketTypes
                        .Select(t => new TicketTypeViewModel
                        {
                            TicketTypeID = t.TicketTypeID,
                            Name = t.TypeName,
                            Price = t.DefaultPrice
                        }).ToList()
                })
                .FirstOrDefault();

            if (movie == null)
            {
                return HttpNotFound();
            }

            return View(movie);
        }

        public ActionResult CinemaListPartial()
        {
            var cinemas = db.Cinemas.ToList();
            return PartialView(cinemas);
        }

        public ActionResult More(int id)
        {
            var movie = db.Movies
                .Where(m => m.MovieID == id)
                .Select(m => new MovieDetailViewModel
                {
                    MovieID = m.MovieID,
                    Title = m.Title,
                    Genre = m.Genre,
                    Duration = m.Duration ?? 0,
                    AgeRating = m.AgeRating,
                    Description = m.Description,
                    ReleaseDate = m.ReleaseDate,
                    Image = m.Image,
                    TrailerLink = m.TrailerLink,
                    Cinemas = db.Cinemas
                        .Where(c => db.Screenings.Any(s => s.MovieID == id && s.Room.RoomID != null && s.Room.CinemaID == c.CinemaID))
                        .Select(c => new CinemaViewModel
                        {
                            CinemaID = c.CinemaID,
                            Name = c.Name,
                            Address = c.Address
                        }).ToList(),
                    Foods = db.Foods
                        .Select(f => new FoodViewModel
                        {
                            FoodID = f.FoodID,
                            FoodName = f.FoodName,
                            Price = f.Price
                        }).ToList(),
                    TicketTypes = db.TicketTypes
                        .Select(t => new TicketTypeViewModel
                        {
                            TicketTypeID = t.TicketTypeID,
                            Name = t.TypeName,
                            Price = t.DefaultPrice
                        }).ToList()
                })
                .FirstOrDefault();

            if (movie == null)
            {
                return HttpNotFound();
            }

            return View(movie);
        }

        public JsonResult GetCinemas()
        {
            var cinemas = db.Cinemas
                .Select(c => new { c.CinemaID, c.Name })
                .ToList();
            return Json(cinemas, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetMoviesByCinema(int cinemaId)
        {
            var movies = db.Screenings
                .Where(s => s.Room.CinemaID == cinemaId)
                .Select(s => new { s.Movy.MovieID, s.Movy.Title })
                .Distinct()
                .ToList();
            return Json(movies, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetDatesByMovie(int movieId)
        {
            var dates = db.Screenings
                .Where(s => s.MovieID == movieId)
                .Select(s => s.StartTime.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();
            return Json(dates.Select(d => d.ToString("yyyy-MM-dd")), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetShowtimesByDate(int movieId, string date)
        {
            DateTime selectedDate = DateTime.Parse(date);
            var showtimes = db.Screenings
                .Where(s => s.MovieID == movieId && s.StartTime.Date == selectedDate)
                .Select(s => s.StartTime.ToString("HH:mm"))
                .ToList();
            return Json(showtimes, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetScreenings(int cinemaId, int movieId)
        {
            var screenings = db.Screenings
                .Join(db.Rooms, s => s.RoomID, r => r.RoomID, (s, r) => new { s, r })
                .Where(sr => sr.s.MovieID == movieId && sr.r.CinemaID == cinemaId)
                .Select(sr => new
                {
                    sr.s.ScreeningID,
                    sr.s.StartTime  // Retrieve StartTime as DateTime
                })
                .ToList()  // Execute the query and load data into memory
                .Select(sr => new
                {
                    ScreeningID = sr.ScreeningID,
                    StartTime = sr.StartTime.ToString("HH:mm")  // Format in C# after query execution
                })
                .ToList();

            return Json(screenings, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSeats(int screeningId)
        {
            var screening = db.Screenings.FirstOrDefault(sc => sc.ScreeningID == screeningId);
            if (screening == null)
            {
                return Json(new { error = "Screening not found" }, JsonRequestBehavior.AllowGet);
            }

            var seats = db.Seats
                .Where(s => s.RoomID == screening.RoomID)
                .Select(s => new
                {
                    s.SeatID,
                    s.SeatNumber,
                    s.RowNumber,
                    s.ColumnNumber,
                    s.Status
                })
                .ToList();

            return Json(seats, JsonRequestBehavior.AllowGet);
        }

        // Updated: Fix seat status handling
        public JsonResult GetSeatsMatrix(int screeningId)
        {
            var screening = db.Screenings.FirstOrDefault(scr => scr.ScreeningID == screeningId);
            if (screening == null)
            {
                return Json(new { error = "Screening not found" }, JsonRequestBehavior.AllowGet);
            }

            // Pre-fetch booked seats from OrderDetails
            var bookedSeats = db.OrderDetails
                .Where(od => od.ScreeningID == screeningId)
                .Select(od => od.SeatID)
                .ToList();

            // Pre-fetch active temporary reservations
            var reservedSeats = db.TemporaryReservations
                .Where(tr => tr.ScreeningID == screeningId && tr.ExpirationTime > DateTime.Now)
                .Select(tr => tr.SeatID)
                .ToList();

            // Fetch seats with updated status
            var seats = db.Seats
                .Where(s => s.RoomID == screening.RoomID)
                .Select(s => new
                {
                    s.SeatID,
                    s.SeatNumber,
                    s.RowNumber,
                    s.ColumnNumber,
                    Status = bookedSeats.Contains(s.SeatID) ? "Sold" :
                             reservedSeats.Contains(s.SeatID) ? "Reserved" : "Available"
                })
                .ToList();

            return Json(seats, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetTicketTypes()
        {
            var ticketTypes = db.TicketTypes
                .Select(t => new
                {
                    t.TicketTypeID,
                    t.TypeName,
                    t.DefaultPrice
                }).ToList();

            return Json(ticketTypes, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult ConfirmBooking(
    int screeningId,
    List<int> seats,
    List<FoodOrder> foods,
    int? ticketTypeId,
    int? customerId)
        {
            seats = seats ?? new List<int>();
            foods = foods ?? new List<FoodOrder>();

            // Validate ticket type
            if (!ticketTypeId.HasValue)
            {
                return Json(new { success = false, message = "Loại vé không được xác định!" });
            }

            // Fetch screening details
            var screening = db.Screenings.FirstOrDefault(s => s.ScreeningID == screeningId);
            if (screening == null)
            {
                return Json(new { success = false, message = "Không tìm thấy suất chiếu!" });
            }

            // Validate screening time
            if (screening.StartTime < (DateTime)System.Data.SqlTypes.SqlDateTime.MinValue ||
                screening.StartTime > (DateTime)System.Data.SqlTypes.SqlDateTime.MaxValue)
            {
                return Json(new { success = false, message = "Thời gian chiếu không hợp lệ!" });
            }
            // Get the room details from the screening
            var room = db.Rooms.FirstOrDefault(r => r.RoomID == screening.RoomID);
            if (room == null)
            {
                return Json(new { success = false, message = "Room not found!" });
            }
            // Get the cinema details from the room
            var cinema = db.Cinemas.FirstOrDefault(c => c.CinemaID == room.CinemaID);
            if (cinema == null)
            {
                return Json(new { success = false, message = "Cinema not found!" });
            }

            // Get the movie details from the screening
            var movie = db.Movies.FirstOrDefault(m => m.MovieID == screening.MovieID);
            if (movie == null)
            {
                return Json(new { success = false, message = "Movie not found!" });
            }
            // Check for already booked or reserved seats
            var bookedSeats = db.OrderDetails
                .Where(od => od.ScreeningID == screeningId && seats.Contains((int)od.SeatID))
                .Select(od => od.SeatID)
                .ToList();

            var reservedSeats = db.TemporaryReservations
                .Where(tr => tr.ScreeningID == screeningId && tr.ExpirationTime > DateTime.Now && seats.Contains((int)tr.SeatID))
                .Select(tr => tr.SeatID)
                .ToList();

            var unavailableSeats = bookedSeats.Union(reservedSeats).Distinct().ToList();
            if (unavailableSeats.Any())
            {
                return Json(new { success = false, message = "Một số ghế đã được đặt hoặc đang được giữ: " + string.Join(", ", unavailableSeats) });
            }

            string sessionId = (customerId == null || customerId == 0) ? Session.SessionID : null;

            // Ensure database connection is open
            if (db.Connection.State == System.Data.ConnectionState.Closed)
            {
                db.Connection.Open();
            }

            using (var transaction = db.Connection.BeginTransaction())
            {
                try
                {
                    db.Transaction = transaction;

                    DateTime now = DateTime.Now;
                    DateTime expirationTime = now.AddMinutes(5);

                    var ticketType = db.TicketTypes.FirstOrDefault(tt => tt.TicketTypeID == ticketTypeId.Value);
                    if (ticketType == null)
                    {
                        return Json(new { success = false, message = "Loại vé không hợp lệ!" });
                    }

                    if (customerId != null && customerId != 0)
                    {
                        // Authenticated user: Create an order
                        var order = new Order
                        {
                            CustomerID = customerId.Value,
                            ScreeningID = screeningId,
                            OrderDate = now,
                            PaymentStatus = "Pending"
                        };
                        db.Orders.InsertOnSubmit(order);
                        db.SubmitChanges();

                        foreach (var seatId in seats)
                        {
                            db.OrderDetails.InsertOnSubmit(new OrderDetail
                            {
                                OrderID = order.OrderID,
                                ItemType = "Ticket",
                                SeatID = seatId,
                                TicketTypeID = ticketTypeId.Value,
                                Price = ticketType.DefaultPrice,
                                Quantity = 1
                            });
                        }

                        foreach (var food in foods.Where(f => f.Quantity > 0))
                        {
                            var foodItem = db.Foods.FirstOrDefault(f => f.FoodID == food.FoodId);
                            if (foodItem != null)
                            {
                                db.OrderDetails.InsertOnSubmit(new OrderDetail
                                {
                                    OrderID = order.OrderID,
                                    ItemType = "Food",
                                    FoodID = food.FoodId,
                                    Quantity = food.Quantity,
                                    Price = foodItem.Price * food.Quantity
                                });
                            }
                        }

                        db.SubmitChanges();
                        transaction.Commit();
                        return Json(new { success = true, message = "Đặt vé thành công! Đơn hàng đã được tạo." });
                    }
                    else
                    {
                        // Guest user: Create temporary reservations
                        if (movie == null)
                        {
                            return Json(new { success = false, message = "Không tìm thấy thông tin phim!" });
                        }

                        foreach (var seatId in seats)
                        {
                            db.TemporaryReservations.InsertOnSubmit(new TemporaryReservation
                            {
                                SessionID = sessionId,
                                ScreeningID = screeningId,
                                SeatID = seatId,
                                TicketTypeID = ticketTypeId.Value,
                                Price = ticketType.DefaultPrice,
                                ItemType = "Ticket",
                                Quantity = 1,
                                ExpirationTime = expirationTime,
                                ScreeningTime = screening.StartTime,
                                MovieTitle = movie.Title,      // Movie title from Movies table
                                CinemaName = cinema.Name       // Cinema name from Cinemas table
                            });

                            db.SubmitChanges();
                        }

                        foreach (var food in foods.Where(f => f.Quantity > 0))
                        {
                            var foodItem = db.Foods.FirstOrDefault(f => f.FoodID == food.FoodId);
                            if (foodItem != null)
                            {
                                db.TemporaryReservations.InsertOnSubmit(new TemporaryReservation
                                {
                                    SessionID = sessionId,
                                    ScreeningID = screeningId,
                                    FoodID = food.FoodId,
                                    Quantity = food.Quantity,
                                    Price = foodItem.Price * food.Quantity,
                                    ItemType = "Food",
                                    ExpirationTime = expirationTime,
                                    ScreeningTime = screening.StartTime,
                                    MovieTitle = movie.Title // Required field to prevent NULL error
                                });
                            }
                        }

                        db.SubmitChanges();
                        transaction.Commit();
                        return Json(new { success = true, message = "Chọn vé đã được lưu vào giỏ hàng tạm thời (5 phút)!" });
                    }
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
    }
}