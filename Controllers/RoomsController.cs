using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManagementSystem.Models;

namespace HotelManagementSystem.Controllers
{
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private const int PageSize = 6;

        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX - ROOMS
        // Pagination + Sorting + Filtering
        // =========================================================

        // GET: Rooms
        public async Task<IActionResult> Index(
            string? search,
            int? roomTypeId,
            string? availability,
            string? sortOrder,
            int page = 1)
        {
            if (page < 1)
                page = 1;

            // Start with all rooms
            var query = _context.Rooms
                .Include(r => r.RoomType)
                .AsQueryable();

            // ---------------------------------------------------------
            // SEARCH BY ROOM NUMBER
            // ---------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(r =>
                    r.RoomNumber != null &&
                    r.RoomNumber.Contains(search));
            }

            // ---------------------------------------------------------
            // FILTER BY ROOM TYPE
            // ---------------------------------------------------------

            if (roomTypeId.HasValue)
            {
                query = query.Where(r =>
                    r.RoomTypeId == roomTypeId.Value);
            }

            // ---------------------------------------------------------
            // FILTER BY CURRENT AVAILABILITY
            // ---------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(availability))
            {
                if (availability.Equals(
                    "available",
                    StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => r.IsAvailable);
                }
                else if (availability.Equals(
                    "unavailable",
                    StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => !r.IsAvailable);
                }
            }

            // ---------------------------------------------------------
            // SORTING
            // ---------------------------------------------------------

            ViewBag.CurrentSort = sortOrder;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentRoomType = roomTypeId;
            ViewBag.CurrentAvailability = availability;

            query = sortOrder switch
            {
                "room_desc" =>
                    query.OrderByDescending(r => r.RoomNumber),

                "price_asc" =>
                    query.OrderBy(r => r.Price),

                "price_desc" =>
                    query.OrderByDescending(r => r.Price),

                "type_asc" =>
                    query.OrderBy(r => r.RoomType!.Name)
                         .ThenBy(r => r.RoomNumber),

                "type_desc" =>
                    query.OrderByDescending(r => r.RoomType!.Name)
                         .ThenBy(r => r.RoomNumber),

                "availability" =>
                    query.OrderByDescending(r => r.IsAvailable)
                         .ThenBy(r => r.RoomNumber),

                _ =>
                    query.OrderBy(r => r.RoomNumber)
            };

            // ---------------------------------------------------------
            // PAGINATION
            // ---------------------------------------------------------

            var totalRooms = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(
                totalRooms / (double)PageSize);

            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            var rooms = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // ---------------------------------------------------------
            // DATA FOR FILTER DROPDOWNS
            // ---------------------------------------------------------

            ViewBag.RoomTypes = await _context.RoomTypes
                .OrderBy(rt => rt.Name)
                .ToListAsync();

            // Pagination information
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRooms = totalRooms;

            return View(rooms);
        }


        // =========================================================
        // FILTER ROOMS BY DATE
        // =========================================================

        // GET: Rooms/Filter
        [HttpGet]
        public async Task<IActionResult> Filter(
            DateOnly? checkInDate,
            DateOnly? checkOutDate,
            string? search,
            int? roomTypeId,
            string? availability,
            string? sortOrder,
            int page = 1)
        {
            if (page < 1)
                page = 1;

            // ---------------------------------------------------------
            // Validate dates
            // ---------------------------------------------------------

            if (!checkInDate.HasValue ||
                !checkOutDate.HasValue)
            {
                return RedirectToAction(nameof(Index), new
                {
                    search,
                    roomTypeId,
                    availability,
                    sortOrder,
                    page
                });
            }

            if (checkInDate.Value >= checkOutDate.Value)
            {
                TempData["Error"] =
                    "Check-out date must be after check-in date.";

                return RedirectToAction(nameof(Index), new
                {
                    search,
                    roomTypeId,
                    availability,
                    sortOrder,
                    page
                });
            }

            // ---------------------------------------------------------
            // Find rooms available for selected dates
            // ---------------------------------------------------------

            var query = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r =>
                    // Room must be physically available
                    r.IsAvailable &&

                    // No booking overlaps requested dates
                    !_context.Bookings.Any(b =>
                        b.RoomId == r.RoomId &&
                        b.CheckInDate < checkOutDate.Value &&
                        b.CheckOutDate > checkInDate.Value
                    )
                )
                .AsQueryable();

            // ---------------------------------------------------------
            // Additional search
            // ---------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(r =>
                    r.RoomNumber != null &&
                    r.RoomNumber.Contains(search));
            }

            // ---------------------------------------------------------
            // Room type filter
            // ---------------------------------------------------------

            if (roomTypeId.HasValue)
            {
                query = query.Where(r =>
                    r.RoomTypeId == roomTypeId.Value);
            }

            // ---------------------------------------------------------
            // Sorting
            // ---------------------------------------------------------

            query = sortOrder switch
            {
                "room_desc" =>
                    query.OrderByDescending(r => r.RoomNumber),

                "price_asc" =>
                    query.OrderBy(r => r.Price),

                "price_desc" =>
                    query.OrderByDescending(r => r.Price),

                "type_asc" =>
                    query.OrderBy(r => r.RoomType!.Name)
                         .ThenBy(r => r.RoomNumber),

                "type_desc" =>
                    query.OrderByDescending(r => r.RoomType!.Name)
                         .ThenBy(r => r.RoomNumber),

                _ =>
                    query.OrderBy(r => r.RoomNumber)
            };

            // ---------------------------------------------------------
            // Pagination
            // ---------------------------------------------------------

            var totalRooms = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(
                totalRooms / (double)PageSize);

            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            var rooms = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // ---------------------------------------------------------
            // ViewBag data
            // ---------------------------------------------------------

            ViewBag.RoomTypes = await _context.RoomTypes
                .OrderBy(rt => rt.Name)
                .ToListAsync();

            ViewBag.Filtered = true;

            ViewBag.CheckInDate = checkInDate.Value;
            ViewBag.CheckOutDate = checkOutDate.Value;

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentRoomType = roomTypeId;
            ViewBag.CurrentAvailability = availability;
            ViewBag.CurrentSort = sortOrder;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRooms = totalRooms;

            return View("Index", rooms);
        }


        // =========================================================
        // CREATE
        // =========================================================

        // GET: Rooms/Create
        public IActionResult Create()
        {
            ViewBag.RoomTypeId = new SelectList(
                _context.RoomTypes,
                "RoomTypeId",
                "Name"
            );

            return View();
        }


        // POST: Rooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Room room)
        {
            if (ModelState.IsValid)
            {
                _context.Add(room);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Room created successfully.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.RoomTypeId = new SelectList(
                _context.RoomTypes,
                "RoomTypeId",
                "Name",
                room.RoomTypeId
            );

            return View(room);
        }


        // =========================================================
        // EDIT
        // =========================================================

        // GET: Rooms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var room = await _context.Rooms
                .FindAsync(id);

            if (room == null)
                return NotFound();

            ViewBag.RoomTypeId = new SelectList(
                _context.RoomTypes,
                "RoomTypeId",
                "Name",
                room.RoomTypeId
            );

            return View(room);
        }


        // POST: Rooms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Room room)
        {
            if (id != room.RoomId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(room);

                    await _context.SaveChangesAsync();

                    TempData["Success"] =
                        "Room updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Rooms.Any(
                        e => e.RoomId == room.RoomId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.RoomTypeId = new SelectList(
                _context.RoomTypes,
                "RoomTypeId",
                "Name",
                room.RoomTypeId
            );

            return View(room);
        }


        // =========================================================
        // DELETE
        // =========================================================

        // GET: Rooms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(
                    m => m.RoomId == id);

            if (room == null)
                return NotFound();

            return View(room);
        }


        // POST: Rooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomId == id);

            if (room == null)
            {
                return NotFound();
            }

            // Prevent deletion if the room has bookings
            var hasBookings = await _context.Bookings
                .AnyAsync(b => b.RoomId == id);

            if (hasBookings)
            {
                TempData["Error"] =
                    "This room cannot be deleted because it has existing bookings.";

                return RedirectToAction(nameof(Index));
            }

            _context.Rooms.Remove(room);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Room deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}