using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Final.Models
{
	public class HomeIndexViewModel
	{
        public IPagedList<Movy> NowShowing { get; set; }  // Danh sách phim đang chiếu
        public IPagedList<Movy> ComingSoon { get; set; }  // Danh sách phim sắp chiếu
        public string TrailerLink { get; set; } // Link YouTube dạng embed
    }
    public class MovieDetailViewModel
    {
        public int MovieID { get; set; }
        public string Title { get; set; }
        public string Genre { get; set; }
        public int Duration { get; set; }
        public string Description { get; set; }
        public string AgeRating { get; set; }
        public string Image { get; set; }
        public string TrailerLink { get; set; }
        public DateTime? ReleaseDate { get; set; }

        // Các danh sách dữ liệu liên quan
        public List<CinemaViewModel> Cinemas { get; set; }
        public List<ScreeningViewModel> Screenings { get; set; }
        public List<SeatViewModel> Seats { get; set; }
        public List<FoodViewModel> Foods { get; set; }
        public List<TicketTypeViewModel> TicketTypes { get; set; }

        // Thêm thuộc tính mới để lưu ID của rạp đã chọn (nếu cần)
        public int? SelectedCinemaID { get; set; }
    }

    public class TicketTypeViewModel
    {
        public int TicketTypeID { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
    }

    // Model Rạp
    public class CinemaViewModel
    {
        public int CinemaID { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
    }

    // Model Lịch chiếu
    public class ScreeningViewModel
    {
        public int ScreeningID { get; set; }
        public int CinemaID { get; set; }
        public DateTime StartTime { get; set; } 
    }

    public class SeatViewModel
    {
        public int SeatID { get; set; }
        public string SeatNumber { get; set; }
        public string Status { get; set; } // "Available", "Selected", "Sold"
    }

    public class FoodViewModel
    {
        public int FoodID { get; set; }
        public string FoodName { get; set; }
        public double Price { get; set; }
    }

    public class BookingViewModel
    {
        public int MovieID { get; set; }
        public int CinemaID { get; set; }
        public int ScreeningID { get; set; }
        public List<int> SelectedSeats { get; set; } = new List<int>();
        public List<int> SelectedFoods { get; set; } = new List<int>();
    }
    public class FoodOrder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public int FoodId { get; set; }
        public int? FoodID { get; internal set; }
        [Required]
        public int Quantity { get; set; }
    }

}