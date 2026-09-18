using HotelManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class BillsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private const int PageSize = 6;

        public BillsController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // INDEX
        // =========================================================

        // GET: Bills
        public async Task<IActionResult> Index(
            string? search,
            string? paymentStatus,
            DateTime? billDateFrom,
            DateTime? billDateTo,
            string? sortOrder,
            int page = 1)
        {
            if (page < 1)
            {
                page = 1;
            }

            var query = _context.Bills
                .Include(b => b.Booking)
                    .ThenInclude(booking => booking.Customer)
                .Include(b => b.Booking)
                    .ThenInclude(booking => booking.Room)
                        .ThenInclude(room => room.RoomType)
                .AsQueryable();


            // =====================================================
            // SEARCH
            // =====================================================

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(b =>
                    b.Booking.Customer.FullName.Contains(search) ||
                    b.Booking.Room.RoomNumber.Contains(search));
            }


            // =====================================================
            // PAYMENT STATUS
            // =====================================================

            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                switch (paymentStatus)
                {
                    case "Paid":
                        query = query.Where(b => b.IsPaid);
                        break;

                    case "Unpaid":
                        query = query.Where(b => !b.IsPaid);
                        break;
                }
            }


            // =====================================================
            // BILL DATE FILTER
            // =====================================================

            if (billDateFrom.HasValue)
            {
                query = query.Where(b =>
                    b.BillDate >= billDateFrom.Value);
            }

            if (billDateTo.HasValue)
            {
                var endDate = billDateTo.Value.Date.AddDays(1);

                query = query.Where(b =>
                    b.BillDate < endDate);
            }


            // =====================================================
            // SORTING
            // =====================================================

            ViewBag.CurrentSort = sortOrder;

            query = sortOrder switch
            {
                "customer_asc" =>
                    query.OrderBy(b =>
                        b.Booking.Customer.FullName),

                "customer_desc" =>
                    query.OrderByDescending(b =>
                        b.Booking.Customer.FullName),

                "room_asc" =>
                    query.OrderBy(b =>
                        b.Booking.Room.RoomNumber),

                "room_desc" =>
                    query.OrderByDescending(b =>
                        b.Booking.Room.RoomNumber),

                "amount_asc" =>
                    query.OrderBy(b =>
                        b.TotalAmount),

                "amount_desc" =>
                    query.OrderByDescending(b =>
                        b.TotalAmount),

                "date_asc" =>
                    query.OrderBy(b =>
                        b.BillDate),

                "date_desc" =>
                    query.OrderByDescending(b =>
                        b.BillDate),

                "paid_first" =>
                    query.OrderByDescending(b =>
                        b.IsPaid),

                "unpaid_first" =>
                    query.OrderBy(b =>
                        b.IsPaid),

                "oldest" =>
                    query.OrderBy(b => b.Id),

                _ =>
                    query.OrderByDescending(b => b.Id)
            };


            // =====================================================
            // PAGINATION
            // =====================================================

            var totalItems = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(
                totalItems / (double)PageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var bills = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();


            // =====================================================
            // CALCULATE DISPLAY VALUES
            // =====================================================

            foreach (var bill in bills)
            {
                CalculateBillDetails(bill);
            }


            // =====================================================
            // VIEW DATA
            // =====================================================

            ViewBag.Search = search;
            ViewBag.PaymentStatus = paymentStatus;

            ViewBag.BillDateFrom = billDateFrom;
            ViewBag.BillDateTo = billDateTo;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = PageSize;


            return View(bills);
        }


        // =========================================================
        // DETAILS
        // =========================================================

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


        // =========================================================
        // CREATE - GET
        // =========================================================

        // GET: Bills/Create
        public async Task<IActionResult> Create(int? bookingId)
        {
            ViewBag.Bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .Where(b =>
                    !_context.Bills.Any(
                        bill => bill.BookingId == b.Id))
                .OrderByDescending(b => b.Id)
                .ToListAsync();

            if (bookingId.HasValue)
            {
                ViewBag.SelectedBookingId = bookingId.Value;
            }

            return View();
        }


        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int bookingId,
            decimal additionalCharge = 0)
        {
            // -----------------------------------------------------
            // Validate additional charge
            // -----------------------------------------------------

            if (additionalCharge < 0)
            {
                ModelState.AddModelError(
                    "additionalCharge",
                    "Additional charge cannot be negative.");
            }


            // -----------------------------------------------------
            // Get booking
            // -----------------------------------------------------

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b =>
                    b.Id == bookingId);

            if (booking == null)
            {
                TempData["Error"] =
                    "Booking not found.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // Prevent duplicate bill
            // -----------------------------------------------------

            var existingBill = await _context.Bills
                .FirstOrDefaultAsync(b =>
                    b.BookingId == bookingId);

            if (existingBill != null)
            {
                TempData["Error"] =
                    "A bill already exists for this booking.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = existingBill.Id });
            }


            // -----------------------------------------------------
            // Validate booking dates
            // -----------------------------------------------------

            var numberOfNights =
                booking.CheckOutDate.DayNumber -
                booking.CheckInDate.DayNumber;

            if (numberOfNights <= 0)
            {
                TempData["Error"] =
                    "Invalid booking dates. Check-out must be after check-in.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // Return form if validation failed
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                ViewBag.Bookings = await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Room)
                    .Where(b =>
                        !_context.Bills.Any(
                            bill => bill.BookingId == b.Id))
                    .OrderByDescending(b => b.Id)
                    .ToListAsync();

                ViewBag.SelectedBookingId = bookingId;

                return View();
            }


            // -----------------------------------------------------
            // Calculate charges
            // -----------------------------------------------------

            decimal roomCharge =
                numberOfNights * booking.Room.Price;

            decimal tax = 0;

            decimal totalAmount =
                roomCharge +
                tax +
                additionalCharge;


            // -----------------------------------------------------
            // Create bill
            // -----------------------------------------------------

            var bill = new Bill
            {
                BookingId = booking.Id,

                BillDate = DateTime.Now,

                TotalAmount = totalAmount,

                IsPaid = false,

                RoomCharge = roomCharge,

                Tax = tax,

                AdditionalCharge = additionalCharge
            };

            _context.Bills.Add(bill);

            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"Bill #{bill.Id} created successfully.";


            return RedirectToAction(
                nameof(Details),
                new { id = bill.Id });
        }


        // =========================================================
        // MARK PAID
        // =========================================================

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


        // =========================================================
        // DELETE
        // =========================================================

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


            // -----------------------------------------------------
            // Paid bills cannot be deleted
            // -----------------------------------------------------

            if (bill.IsPaid)
            {
                TempData["Error"] =
                    "A paid bill cannot be deleted.";

                return RedirectToAction(nameof(Index));
            }


            _context.Bills.Remove(bill);

            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"Bill #{id} deleted successfully.";


            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // BILL CALCULATION
        // =========================================================

        private void CalculateBillDetails(Bill bill)
        {
            if (bill.Booking == null ||
                bill.Booking.Room == null)
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


            // Room charge
            bill.RoomCharge =
                numberOfNights *
                bill.Booking.Room.Price;


            // Current system does not store tax
            // in the database.
            bill.Tax = 0;


            // Additional charge is derived
            // from the stored total.
            bill.AdditionalCharge =
                bill.TotalAmount -
                bill.RoomCharge -
                bill.Tax;


            if (bill.AdditionalCharge < 0)
            {
                bill.AdditionalCharge = 0;
            }
        }
    }
}