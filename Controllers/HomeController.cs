using HotelManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Dashboard statistics
            ViewBag.TotalRooms = await _context.Rooms.CountAsync();

            ViewBag.AvailableRooms = await _context.Rooms
                .CountAsync(r => r.IsAvailable);

            ViewBag.TotalBookings = await _context.Bookings.CountAsync();

            ViewBag.TotalCustomers = await _context.Customers.CountAsync();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}