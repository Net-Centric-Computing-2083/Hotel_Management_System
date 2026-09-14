using HotelManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class CheckInOutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CheckInOutController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CheckInOut
        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                .Include(b => b.CheckIn)
                    .ThenInclude(c => c!.CheckOut)
                .OrderByDescending(b => b.Id)
                .ToListAsync();

            return View(bookings);
        }

        // POST: CheckInOut/CheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.CheckIn)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction(nameof(Index));
            }

            // Prevent duplicate check-in
            if (booking.CheckIn != null)
            {
                TempData["Error"] =
                    "This booking has already been checked in.";

                return RedirectToAction(nameof(Index));
            }

            // Check room availability
            if (!booking.Room.IsAvailable)
            {
                TempData["Error"] =
                    "This room is currently unavailable.";

                return RedirectToAction(nameof(Index));
            }

            var checkIn = new CheckIn
            {
                BookingId = booking.Id,
                CheckInDateTime = DateTime.Now
            };

            _context.CheckIns.Add(checkIn);

            // Room becomes occupied
            booking.Room.IsAvailable = false;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Booking #{booking.Id} checked in successfully.";

            return RedirectToAction(nameof(Index));
        }

        // POST: CheckInOut/CheckOut
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.CheckIn)
                    .ThenInclude(c => c!.CheckOut)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction(nameof(Index));
            }

            if (booking.CheckIn == null)
            {
                TempData["Error"] =
                    "This booking has not been checked in yet.";

                return RedirectToAction(nameof(Index));
            }

            if (booking.CheckIn.CheckOut != null)
            {
                TempData["Error"] =
                    "This booking has already been checked out.";

                return RedirectToAction(nameof(Index));
            }

            var checkOut = new CheckOut
            {
                CheckInId = booking.CheckIn.Id,
                CheckOutDateTime = DateTime.Now
            };

            _context.CheckOuts.Add(checkOut);

            // Room becomes available again
            booking.Room.IsAvailable = true;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Booking #{booking.Id} checked out successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: CheckInOut/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                .Include(b => b.CheckIn)
                    .ThenInclude(c => c!.CheckOut)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }
    }
}