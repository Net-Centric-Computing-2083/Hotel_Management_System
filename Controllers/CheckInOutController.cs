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
            var pendingCheckIns = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .Where(b =>
                    b.Status == "Confirmed" &&
                    b.CheckInDate.Date <= DateTime.Today)
                .OrderBy(b => b.CheckInDate)
                .ToListAsync();

            var activeCheckIns = await _context.CheckIns
                .Include(c => c.Booking)
                    .ThenInclude(b => b.Customer)
                .Include(c => c.Booking)
                    .ThenInclude(b => b.Room)
                .Where(c => c.Booking!.Status == "CheckedIn")
                .OrderBy(c => c.CheckInTime)
                .ToListAsync();

            ViewBag.PendingCheckIns = pendingCheckIns;
            ViewBag.ActiveCheckIns = activeCheckIns;

            return View();
        }

        // POST: CheckInOut/ProcessCheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckIn(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction(nameof(Index));
            }

            if (booking.Status != "Confirmed")
            {
                TempData["Error"] =
                    "Only confirmed bookings can be checked in.";

                return RedirectToAction(nameof(Index));
            }

            if (booking.CheckInDate.Date > DateTime.Today)
            {
                TempData["Error"] =
                    "The check-in date has not arrived yet.";

                return RedirectToAction(nameof(Index));
            }

            bool alreadyCheckedIn = await _context.CheckIns
                .AnyAsync(c => c.BookingId == bookingId);

            if (alreadyCheckedIn)
            {
                TempData["Error"] =
                    "This booking has already been checked in.";

                return RedirectToAction(nameof(Index));
            }

            if (booking.Room == null)
            {
                TempData["Error"] = "Room information not found.";
                return RedirectToAction(nameof(Index));
            }

            if (booking.Room.Status == "Maintenance")
            {
                TempData["Error"] =
                    "This room is under maintenance.";

                return RedirectToAction(nameof(Index));
            }

            var checkIn = new CheckIn
            {
                BookingId = booking.Id,
                CheckInTime = DateTime.Now
            };

            booking.Status = "CheckedIn";
            booking.Room.Status = "Occupied";

            _context.CheckIns.Add(checkIn);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Guest {booking.Customer?.FullName} checked in successfully.";

            return RedirectToAction(nameof(Index));
        }

        // POST: CheckInOut/ProcessCheckOut
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckOut(int checkInId)
        {
            var checkIn = await _context.CheckIns
                .Include(c => c.Booking)
                    .ThenInclude(b => b.Customer)
                .Include(c => c.Booking)
                    .ThenInclude(b => b.Room)
                        .ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(c => c.Id == checkInId);

            if (checkIn == null || checkIn.Booking == null)
            {
                TempData["Error"] =
                    "Check-in record not found.";

                return RedirectToAction(nameof(Index));
            }

            var booking = checkIn.Booking;

            if (booking.Status != "CheckedIn")
            {
                TempData["Error"] =
                    "This booking is not currently checked in.";

                return RedirectToAction(nameof(Index));
            }

            bool alreadyCheckedOut = await _context.CheckOuts
                .AnyAsync(c => c.BookingId == booking.Id);

            if (alreadyCheckedOut)
            {
                TempData["Error"] =
                    "This booking has already been checked out.";

                return RedirectToAction(nameof(Index));
            }

            if (booking.Room == null ||
                booking.Room.RoomType == null)
            {
                TempData["Error"] =
                    "Room or room type information is missing.";

                return RedirectToAction(nameof(Index));
            }

            // Calculate nights
            int nights =
                (booking.CheckOutDate.Date -
                 booking.CheckInDate.Date).Days;

            if (nights <= 0)
                nights = 1;

            decimal roomCharge =
                nights * booking.Room.RoomType.PricePerNight;

            decimal tax = roomCharge * 0.13m;

            decimal additionalCharge = 0m;

            decimal totalAmount =
                roomCharge +
                additionalCharge +
                tax;

            // Create checkout
            var checkOut = new CheckOut
            {
                BookingId = booking.Id,
                CheckOutTime = DateTime.Now
            };

            // Create bill
            var bill = new Bill
            {
                BookingId = booking.Id,
                RoomCharge = roomCharge,
                AdditionalCharge = additionalCharge,
                Tax = tax,
                TotalAmount = totalAmount,
                PaymentDate = DateTime.Now,
                IsPaid = false
            };

            booking.Status = "CheckedOut";
            booking.Room.Status = "Available";

            _context.CheckOuts.Add(checkOut);
            _context.Bills.Add(bill);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Check-out completed and bill generated.";

            return RedirectToAction(
                "Invoice",
                "Billing",
                new { id = bill.Id });
        }
    }
}