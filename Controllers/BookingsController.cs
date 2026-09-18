using HotelManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private const int PageSize = 6;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX
        // =========================================================

        // GET: Bookings
        public async Task<IActionResult> Index(
            string? search,
            string? status,
            DateOnly? checkInDate,
            DateOnly? checkOutDate,
            string? sortOrder,
            int page = 1)
        {
            if (page < 1)
                page = 1;

            var query = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                .Include(b => b.CheckIn)
                    .ThenInclude(c => c!.CheckOut)
                .AsQueryable();

            // ---------------------------------------------------------
            // SEARCH
            // ---------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(b =>
                    b.Customer.FullName.Contains(search) ||
                    b.Room.RoomNumber.Contains(search));
            }

            // ---------------------------------------------------------
            // DATE FILTER
            // ---------------------------------------------------------

            if (checkInDate.HasValue)
            {
                query = query.Where(b =>
                    b.CheckInDate >= checkInDate.Value);
            }

            if (checkOutDate.HasValue)
            {
                query = query.Where(b =>
                    b.CheckOutDate <= checkOutDate.Value);
            }

            // ---------------------------------------------------------
            // STATUS FILTER
            //
            // Confirmed  = no check-in
            // Checked In = check-in exists, checkout does not
            // Checked Out = checkout exists
            // ---------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(status))
            {
                switch (status)
                {
                    case "Confirmed":
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

            // ---------------------------------------------------------
            // SORTING
            // ---------------------------------------------------------

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

            // ---------------------------------------------------------
            // PAGINATION
            // ---------------------------------------------------------

            var totalItems = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(
                totalItems / (double)PageSize);

            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            var bookings = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // ---------------------------------------------------------
            // VIEW DATA
            // ---------------------------------------------------------

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
        // DETAILS
        // =========================================================

        // GET: Bookings/Details/5
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
                .Include(b => b.Bill)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }


        // =========================================================
        // CREATE - GET
        // =========================================================

        // GET: Bookings/Create
        public async Task<IActionResult> Create(
            int? customerId,
            int? roomId,
            DateTime? checkInDate,
            DateTime? checkOutDate)
        {
            var booking = new Booking();

            booking.CheckInDate = checkInDate.HasValue
                ? DateOnly.FromDateTime(checkInDate.Value)
                : DateOnly.FromDateTime(DateTime.Today);

            booking.CheckOutDate = checkOutDate.HasValue
                ? DateOnly.FromDateTime(checkOutDate.Value)
                : DateOnly.FromDateTime(DateTime.Today.AddDays(1));

            if (customerId.HasValue)
            {
                booking.CustomerId = customerId.Value;
            }

            if (roomId.HasValue)
            {
                booking.RoomId = roomId.Value;
            }

            await LoadCreateData();

            return View(booking);
        }


        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            ModelState.Remove(nameof(Booking.Customer));
            ModelState.Remove(nameof(Booking.Room));
            ModelState.Remove(nameof(Booking.Bill));
            ModelState.Remove(nameof(Booking.CheckIn));

            // ---------------------------------------------------------
            // Validate dates
            // ---------------------------------------------------------

            if (booking.CheckInDate >= booking.CheckOutDate)
            {
                ModelState.AddModelError(
                    nameof(Booking.CheckOutDate),
                    "Check-out date must be after check-in date.");
            }

            // ---------------------------------------------------------
            // Check customer
            // ---------------------------------------------------------

            var customerExists = await _context.Customers
                .AnyAsync(c => c.Id == booking.CustomerId);

            if (!customerExists)
            {
                ModelState.AddModelError(
                    nameof(Booking.CustomerId),
                    "Please select a valid customer.");
            }

            // ---------------------------------------------------------
            // Get room
            // ---------------------------------------------------------

            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomId == booking.RoomId);

            if (room == null)
            {
                ModelState.AddModelError(
                    nameof(Booking.RoomId),
                    "Please select a valid room.");
            }
            else if (!room.IsAvailable)
            {
                ModelState.AddModelError(
                    nameof(Booking.RoomId),
                    "This room is currently unavailable and cannot be booked.");
            }

            // ---------------------------------------------------------
            // Check overlapping booking
            // ---------------------------------------------------------

            var alreadyBooked = await _context.Bookings
                .AnyAsync(b =>
                    b.RoomId == booking.RoomId &&
                    b.CheckInDate < booking.CheckOutDate &&
                    b.CheckOutDate > booking.CheckInDate);

            if (alreadyBooked)
            {
                ModelState.AddModelError(
                    nameof(Booking.RoomId),
                    "This room is already booked for the selected dates.");
            }

            // ---------------------------------------------------------
            // Return form if validation failed
            // ---------------------------------------------------------

            if (!ModelState.IsValid)
            {
                await LoadCreateData();
                return View(booking);
            }

            // ---------------------------------------------------------
            // Create booking
            // ---------------------------------------------------------

            booking.BookingDate = DateTime.Now;
            booking.Status = "Confirmed";

            _context.Bookings.Add(booking);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Booking #{booking.Id} created successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // EDIT - GET
        // =========================================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.CheckIn)
                    .ThenInclude(c => c!.CheckOut)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.CheckIn != null)
            {
                TempData["Error"] =
                    "A booking cannot be edited after the guest has checked in.";

                return RedirectToAction(nameof(Index));
            }

            await LoadCreateData();

            return View(booking);
        }


        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Booking booking)
        {
            if (id != booking.Id)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(Booking.Customer));
            ModelState.Remove(nameof(Booking.Room));
            ModelState.Remove(nameof(Booking.Bill));
            ModelState.Remove(nameof(Booking.CheckIn));

            // ---------------------------------------------------------
            // Validate dates
            // ---------------------------------------------------------

            if (booking.CheckInDate >= booking.CheckOutDate)
            {
                ModelState.AddModelError(
                    nameof(Booking.CheckOutDate),
                    "Check-out date must be after check-in date.");
            }

            // ---------------------------------------------------------
            // Check customer
            // ---------------------------------------------------------

            var customerExists = await _context.Customers
                .AnyAsync(c => c.Id == booking.CustomerId);

            if (!customerExists)
            {
                ModelState.AddModelError(
                    nameof(Booking.CustomerId),
                    "Please select a valid customer.");
            }

            // ---------------------------------------------------------
            // Check room
            // ---------------------------------------------------------

            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomId == booking.RoomId);

            if (room == null)
            {
                ModelState.AddModelError(
                    nameof(Booking.RoomId),
                    "Please select a valid room.");
            }
            else if (!room.IsAvailable)
            {
                ModelState.AddModelError(
                    nameof(Booking.RoomId),
                    "This room is currently unavailable and cannot be booked.");
            }

            // ---------------------------------------------------------
            // Check overlapping bookings
            // ---------------------------------------------------------

            var overlappingBooking = await _context.Bookings
                .AnyAsync(b =>
                    b.Id != booking.Id &&
                    b.RoomId == booking.RoomId &&
                    b.CheckInDate < booking.CheckOutDate &&
                    b.CheckOutDate > booking.CheckInDate);

            if (overlappingBooking)
            {
                ModelState.AddModelError(
                    nameof(Booking.RoomId),
                    "This room is already booked for the selected dates.");
            }

            // ---------------------------------------------------------
            // Return form if validation failed
            // ---------------------------------------------------------

            if (!ModelState.IsValid)
            {
                await LoadCreateData();
                return View(booking);
            }

            // ---------------------------------------------------------
            // Find existing booking
            // ---------------------------------------------------------

            var existingBooking = await _context.Bookings
                .Include(b => b.CheckIn)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (existingBooking == null)
            {
                return NotFound();
            }

            if (existingBooking.CheckIn != null)
            {
                TempData["Error"] =
                    "A booking cannot be edited after the guest has checked in.";

                return RedirectToAction(nameof(Index));
            }

            // ---------------------------------------------------------
            // Update
            // ---------------------------------------------------------

            existingBooking.CustomerId = booking.CustomerId;
            existingBooking.RoomId = booking.RoomId;
            existingBooking.CheckInDate = booking.CheckInDate;
            existingBooking.CheckOutDate = booking.CheckOutDate;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Booking #{existingBooking.Id} updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // DELETE - GET
        // =========================================================

        public async Task<IActionResult> Delete(int? id)
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


        // =========================================================
        // DELETE - POST
        // =========================================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.CheckIn)
                    .ThenInclude(c => c!.CheckOut)
                .Include(b => b.Bill)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.CheckIn != null)
            {
                TempData["Error"] =
                    "A booking cannot be deleted after the guest has checked in.";

                return RedirectToAction(nameof(Index));
            }

            if (booking.Bill != null)
            {
                _context.Bills.Remove(booking.Bill);
            }

            _context.Bookings.Remove(booking);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Booking #{id} deleted successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // CANCEL BOOKING
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.CheckIn)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction(nameof(Index));
            }

            if (booking.CheckIn != null)
            {
                TempData["Error"] =
                    "A booking cannot be cancelled after the guest has checked in.";

                return RedirectToAction(nameof(Index));
            }

            _context.Bookings.Remove(booking);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Booking #{bookingId} cancelled successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // HELPER METHODS
        // =========================================================

        private async Task LoadCreateData()
        {
            ViewBag.Customers = await _context.Customers
                .OrderBy(c => c.FullName)
                .ToListAsync();

            ViewBag.Rooms = await _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.IsAvailable)
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();
        }


        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(b => b.Id == id);
        }
    }
}