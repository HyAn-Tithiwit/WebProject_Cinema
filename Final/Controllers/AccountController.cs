using Final.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Final.Controllers
{
    public class AccountController : Controller
    {
        private DataClassesDataContext db = new DataClassesDataContext();

        // GET: Register
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        // POST: Register
        [HttpPost]
        public ActionResult Register([Bind(Include = "FullName,Email,Phone,PasswordHash")] Customer model, string ConfirmPassword)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra email đã tồn tại chưa
                if (db.Customers.Any(c => c.Email == model.Email))
                {
                    ViewBag.Error = "Email đã tồn tại!";
                    return View(model);
                }

                // Kiểm tra xác nhận mật khẩu
                if (model.PasswordHash != ConfirmPassword)
                {
                    ViewBag.Error = "Mật khẩu xác nhận không khớp!";
                    return View(model);
                }

                // Thêm khách hàng mới vào database
                db.Customers.InsertOnSubmit(model);
                db.SubmitChanges();

                // Đăng nhập tự động sau khi đăng ký
                Session["CustomerID"] = model.CustomerID;
                Session["CustomerName"] = model.FullName;
                Session["IsLoggedIn"] = true;

                // Cập nhật các temp cart của khách (SessionID "0") cho người dùng vừa đăng ký
                var guestItems = db.TemporaryReservations.Where(tr => tr.SessionID == "0").ToList();
                foreach (var item in guestItems)
                {
                    item.CustomerID = model.CustomerID;
                    item.SessionID = Session.SessionID; // Gán lại SessionID hiện tại
                }
                db.SubmitChanges();

                return RedirectToAction("Index", "Movie");
            }
            return View(model);
        }


        // GET: Login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            // Retrieve the customer based on the provided email and password.
            var user = db.Customers.FirstOrDefault(c => c.Email == username && c.PasswordHash == password);

            if (user != null)
            {
                // Set session variables for logged in user.
                Session["CustomerID"] = user.CustomerID;
                Session["CustomerName"] = user.FullName;
                Session["IsLoggedIn"] = true;

                // Update temporary reservations that were created under the guest cart ("0").
                // Change their SessionID to the current session and assign the CustomerID.
                var guestItems = db.TemporaryReservations.Where(tr => tr.SessionID == "0").ToList();
                foreach (var item in guestItems)
                {
                    item.CustomerID = user.CustomerID;
                    item.SessionID = Session.SessionID; // Update session id for consistency.
                }
                db.SubmitChanges();

                return RedirectToAction("Index", "Movie");
            }

            ViewBag.Message = "Sai tài khoản hoặc mật khẩu!";
            return View();
        }

        // Logout action
        public ActionResult Logout()
        {
            Session.Clear();
            Session["IsLoggedIn"] = false;
            return RedirectToAction("Login", "Account");
        }
    }
}
