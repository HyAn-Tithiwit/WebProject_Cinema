using Final.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Final.Controllers
{
    public class PlaceController : Controller
    {
        DataClassesDataContext db = new DataClassesDataContext();

        // GET: Place
        public ActionResult Detail(int id)
        {
            var cinema = db.Cinemas.FirstOrDefault(c => c.CinemaID == id);
            if (cinema == null)
                return HttpNotFound();

            // Trả về view hiển thị thông tin rạp
            return View(cinema);
        }
        // Tab 1: Phim đang chiếu
        // Action Phim đang chiếu (NowShowing)
        // Action Phim đang chiếu (NowShowing)
        public ActionResult NowShowing(int cinemaID, int? page)
        {
            ViewBag.Category = "NowShowing";
            ViewBag.CinemaID = cinemaID;

            var now = DateTime.Now;

            // Lấy tất cả các suất chiếu đã bắt đầu (đang chiếu) tại rạp theo cinemaID
            var screenings = db.Screenings
                .Where(s => s.Room.CinemaID == cinemaID && s.StartTime <= now)
                .ToList();

            // Lấy danh sách Movie duy nhất
            var movies = screenings
                .Select(s => s.MovieID)
                .Distinct()
                .Join(db.Movies, movID => movID, mov => mov.MovieID, (movID, mov) => mov)
                .ToList();

            int pageNumber = page ?? 1;   // Trang hiện tại
            int pageSize = 8;             // Số phim mỗi trang
            var pagedMovies = movies.ToPagedList(pageNumber, pageSize);

            return PartialView("NowShowing", pagedMovies);
        }

        // Action Phim sắp chiếu (ComingSoon)
        public ActionResult ComingSoon(int? page)
        {
            int pageSize = 8;
            int pageNumber = page ?? 1;
            DateTime today = DateTime.Now;

            // Lấy các phim có ReleaseDate > today (phim sắp chiếu)
            IQueryable<Movy> movies = db.Movies.Where(m => m.ReleaseDate > today).OrderBy(m => m.Title);

            ViewBag.Category = "ComingSoon";
            return View(movies.ToPagedList(pageNumber, pageSize));
        }


        // Tab 4: Bảng vé
        // Minh hoạ trả về thông tin giá vé, loại vé (TicketTypes), ...
        public ActionResult TicketPrice(int cinemaID)
        {
            // Tuỳ chỉnh logic lấy dữ liệu
            // Ví dụ: trả về danh sách TicketTypes
            var ticketTypes = db.TicketTypes.ToList();
            return PartialView(ticketTypes);
        }
    }
}