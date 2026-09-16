using HotelManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX
        // =========================================================

        // GET: Bookings
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

            // Default check-in date = today
            booking.CheckInDate = checkInDate.HasValue
                ? DateOnly.FromDateTime(checkInDate.Value)
                : DateOnly.FromDateTime(DateTime.Today);

            // Default check-out date = tomorrow
            booking.CheckOutDate = checkOutDate.HasValue
                ? DateOnly.FromDateTime(checkOutDate.Value)
                : DateOnly.FromDateTime(DateTime.Today.AddDays(1));

            // Preselect customer if supplied
            if (customerId.HasValue)
            {
                booking.CustomerId = customerId.Value;
            }

            // Preselect room if supplied
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

        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            // Navigation properties are selected using IDs.
            // They should not be validated as required form fields.
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
            else
            {
                // -----------------------------------------------------
                // IMPORTANT:
                // Do not allow rooms marked as unavailable to be booked.
                // -----------------------------------------------------

                if (!room.IsAvailable)
                {
                    ModelState.AddModelError(
                        nameof(Booking.RoomId),
                        "This room is currently unavailable and cannot be booked.");
                }
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

            // Booking starts as confirmed/pending check-in
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

        // GET: Bookings/Edit/5
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

            // Do not allow editing after check-in
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

        // POST: Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Booking booking)
        {
            if (id != booking.Id)
            {
                return NotFound();
            }

            // Navigation properties should not be required
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
            else
            {
                // Do not allow an unavailable room to be selected
                if (!room.IsAvailable)
                {
                    ModelState.AddModelError(
                        nameof(Booking.RoomId),
                        "This room is currently unavailable and cannot be booked.");
                }
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

            // Do not allow editing after check-in
            if (existingBooking.CheckIn != null)
            {
                TempData["Error"] =
                    "A booking cannot be edited after the guest has checked in.";

                return RedirectToAction(nameof(Index));
            }

            // ---------------------------------------------------------
            // Update editable fields
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

        // GET: Bookings/Delete/5
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

        // POST: Bookings/Delete/5
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

            // Do not delete a booking that has already been checked in
            if (booking.CheckIn != null)
            {
                TempData["Error"] =
                    "A booking cannot be deleted after the guest has checked in.";

                return RedirectToAction(nameof(Index));
            }

            // Delete related bill if one exists
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

        // POST: Bookings/Cancel
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

            // Cannot cancel after check-in
            if (booking.CheckIn != null)
            {
                TempData["Error"] =
                    "A booking cannot be cancelled after the guest has checked in.";

                return RedirectToAction(nameof(Index));
            }

            // Since Status is NotMapped, this only changes the
            // current object and is not persisted to the database.
            // Therefore, we delete the booking for this schema.
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
            // Load customers
            ViewBag.Customers = await _context.Customers
                .OrderBy(c => c.FullName)
                .ToListAsync();

            // IMPORTANT:
            // Only rooms marked as available are shown
            // in the booking form.
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