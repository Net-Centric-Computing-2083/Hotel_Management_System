using HotelManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class CheckInOutController : Controller
    {
        private readonly ApplicationDbContext _context;

        private const int PageSize = 6;

        public CheckInOutController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX
        // =========================================================

        // GET: CheckInOut
        public async Task<IActionResult> Index(
            string? search,
            string? status,
            DateOnly? checkInDate,
            DateOnly? checkOutDate,
            string? sortOrder,
            int page = 1)
        {
            if (page < 1)
            {
                page = 1;
            }

            var query = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                .Include(b => b.CheckIn)
                    .ThenInclude(c => c!.CheckOut)
                .AsQueryable();

            // =====================================================
            // SEARCH
            // =====================================================

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(b =>
                    b.Customer.FullName.Contains(search) ||
                    b.Room.RoomNumber.Contains(search));
            }

            // =====================================================
            // CHECK-IN DATE FILTER
            // =====================================================

            if (checkInDate.HasValue)
            {
                query = query.Where(b =>
                    b.CheckInDate >= checkInDate.Value);
            }

            // =====================================================
            // CHECK-OUT DATE FILTER
            // =====================================================

            if (checkOutDate.HasValue)
            {
                query = query.Where(b =>
                    b.CheckOutDate <= checkOutDate.Value);
            }

            // =====================================================
            // STATUS FILTER
            //
            // Ready for Check-In:
            // No CheckIn record
            //
            // Checked In:
            // CheckIn exists but CheckOut does not
            //
            // Checked Out:
            // CheckIn and CheckOut both exist
            // =====================================================

            if (!string.IsNullOrWhiteSpace(status))
            {
                switch (status)
                {
                    case "Ready":
                        query = query.Where(b =>
                            b.CheckIn == null);
                        break;

                    case "Checked In":
                        query = query.Where(b =>
                            b.CheckIn != null &&
                            b.CheckIn.CheckOut == null);
                        break;

                    case "Checked Out":
                        query = query.Where(b =>
                            b.CheckIn != null &&
                            b.CheckIn.CheckOut != null);
                        break;
                }
            }

            // =====================================================
            // SORTING
            // =====================================================

            ViewBag.CurrentSort = sortOrder;

            query = sortOrder switch
            {
                "customer_asc" =>
                    query.OrderBy(b => b.Customer.FullName),

                "customer_desc" =>
                    query.OrderByDescending(b => b.Customer.FullName),

                "room_asc" =>
                    query.OrderBy(b => b.Room.RoomNumber),

                "room_desc" =>
                    query.OrderByDescending(b => b.Room.RoomNumber),

                "checkin_asc" =>
                    query.OrderBy(b => b.CheckInDate),

                "checkin_desc" =>
                    query.OrderByDescending(b => b.CheckInDate),

                "checkout_asc" =>
                    query.OrderBy(b => b.CheckOutDate),

                "checkout_desc" =>
                    query.OrderByDescending(b => b.CheckOutDate),

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

            var bookings = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // =====================================================
            // VIEW DATA
            // =====================================================

            ViewBag.Search = search;
            ViewBag.Status = status;

            ViewBag.CheckInDate = checkInDate;
            ViewBag.CheckOutDate = checkOutDate;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = PageSize;

            return View(bookings);
        }


        // =========================================================
        // CHECK-IN
        // =========================================================

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

            // -----------------------------------------------------
            // Prevent duplicate check-in
            // -----------------------------------------------------

            if (booking.CheckIn != null)
            {
                TempData["Error"] =
                    "This booking has already been checked in.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Check room
            // -----------------------------------------------------

            if (booking.Room == null)
            {
                TempData["Error"] =
                    "The room associated with this booking could not be found.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Check room availability
            // -----------------------------------------------------

            if (!booking.Room.IsAvailable)
            {
                TempData["Error"] =
                    "This room is currently unavailable.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Create Check-In
            // -----------------------------------------------------

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


        // =========================================================
        // CHECK-OUT
        // =========================================================

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

            // -----------------------------------------------------
            // Must be checked in first
            // -----------------------------------------------------

            if (booking.CheckIn == null)
            {
                TempData["Error"] =
                    "This booking has not been checked in yet.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Prevent duplicate checkout
            // -----------------------------------------------------

            if (booking.CheckIn.CheckOut != null)
            {
                TempData["Error"] =
                    "This booking has already been checked out.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Check room
            // -----------------------------------------------------

            if (booking.Room == null)
            {
                TempData["Error"] =
                    "The room associated with this booking could not be found.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // Create Check-Out
            // -----------------------------------------------------

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


        // =========================================================
        // DETAILS
        // =========================================================

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