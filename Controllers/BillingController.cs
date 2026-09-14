using HotelManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class BillsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BillsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Bills
        public async Task<IActionResult> Index()
        {
            var bills = await _context.Bills
                .Include(b => b.Booking)
                    .ThenInclude(booking => booking.Customer)
                .Include(b => b.Booking)
                    .ThenInclude(booking => booking.Room)
                .OrderByDescending(b => b.Id)
                .ToListAsync();

            return View(bills);
        }

        // GET: Bills/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bill = await _context.Bills
                .Include(b => b.Booking)
                    .ThenInclude(booking => booking.Customer)
                .Include(b => b.Booking)
                    .ThenInclude(booking => booking.Room)
                        .ThenInclude(room => room.RoomType)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bill == null)
            {
                return NotFound();
            }

            CalculateBillDetails(bill);

            return View(bill);
        }

        // GET: Bills/Create
        public async Task<IActionResult> Create(int? bookingId)
        {
            ViewBag.Bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .Where(b => !_context.Bills.Any(
                    bill => bill.BookingId == b.Id))
                .OrderByDescending(b => b.Id)
                .ToListAsync();

            if (bookingId.HasValue)
            {
                ViewBag.SelectedBookingId = bookingId.Value;
            }

            return View();
        }

        // POST: Bills/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int bookingId,
            decimal additionalCharge = 0)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction(nameof(Index));
            }

            // Prevent duplicate bills
            var existingBill = await _context.Bills
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (existingBill != null)
            {
                TempData["Error"] =
                    "A bill already exists for this booking.";

                return RedirectToAction(nameof(Details),
                    new { id = existingBill.Id });
            }

            if (additionalCharge < 0)
            {
                ModelState.AddModelError(
                    "additionalCharge",
                    "Additional charge cannot be negative.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Bookings = await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Room)
                    .Where(b => !_context.Bills.Any(
                        bill => bill.BookingId == b.Id))
                    .OrderByDescending(b => b.Id)
                    .ToListAsync();

                ViewBag.SelectedBookingId = bookingId;

                return View();
            }

            var numberOfNights =
                booking.CheckOutDate.DayNumber -
                booking.CheckInDate.DayNumber;

            if (numberOfNights <= 0)
            {
                TempData["Error"] =
                    "Invalid booking dates.";

                return RedirectToAction(nameof(Index));
            }

            decimal roomCharge =
                numberOfNights * booking.Room.Price;

            decimal totalAmount =
                roomCharge + additionalCharge;

            var bill = new Bill
            {
                BookingId = booking.Id,
                BillDate = DateTime.Now,
                TotalAmount = totalAmount,
                IsPaid = false,

                // These properties are NotMapped.
                RoomCharge = roomCharge,
                AdditionalCharge = additionalCharge,
                Tax = 0
            };

            _context.Bills.Add(bill);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Bill created successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = bill.Id });
        }

        // POST: Bills/MarkPaid/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bill == null)
            {
                return NotFound();
            }

            if (bill.IsPaid)
            {
                TempData["Error"] =
                    "This bill has already been paid.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            bill.IsPaid = true;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Payment marked as paid successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // POST: Bills/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bill == null)
            {
                return NotFound();
            }

            if (bill.IsPaid)
            {
                TempData["Error"] =
                    "A paid bill cannot be deleted.";

                return RedirectToAction(nameof(Index));
            }

            _context.Bills.Remove(bill);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Bill deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // Calculate values that are not stored in the database.
        private void CalculateBillDetails(Bill bill)
        {
            if (bill.Booking == null)
            {
                return;
            }

            int numberOfNights =
                bill.Booking.CheckOutDate.DayNumber -
                bill.Booking.CheckInDate.DayNumber;

            if (numberOfNights < 0)
            {
                numberOfNights = 0;
            }

            bill.RoomCharge =
                numberOfNights * bill.Booking.Room.Price;

            bill.Tax = 0;

            bill.AdditionalCharge =
                bill.TotalAmount - bill.RoomCharge;

            if (bill.AdditionalCharge < 0)
            {
                bill.AdditionalCharge = 0;
            }
        }
    }
}