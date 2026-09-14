using HotelManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Controllers
{
    public class RoomTypesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomTypesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: RoomTypes
        public async Task<IActionResult> Index()
        {
            var roomTypes = await _context.RoomTypes
                .Include(r => r.Rooms)
                .OrderBy(r => r.Name)
                .ToListAsync();

            return View(roomTypes);
        }

        // GET: RoomTypes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var roomType = await _context.RoomTypes
                .Include(r => r.Rooms)
                .FirstOrDefaultAsync(r => r.RoomTypeId == id);

            if (roomType == null)
                return NotFound();

            return View(roomType);
        }

        // GET: RoomTypes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RoomTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomType roomType)
        {
            if (!ModelState.IsValid)
                return View(roomType);

            _context.RoomTypes.Add(roomType);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Room type created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: RoomTypes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var roomType = await _context.RoomTypes
                .FirstOrDefaultAsync(r => r.RoomTypeId == id);

            if (roomType == null)
                return NotFound();

            return View(roomType);
        }

        // POST: RoomTypes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            RoomType roomType)
        {
            if (id != roomType.RoomTypeId)
                return NotFound();

            if (!ModelState.IsValid)
                return View(roomType);

            try
            {
                _context.Update(roomType);
                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Room type updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoomTypeExists(roomType.RoomTypeId))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: RoomTypes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var roomType = await _context.RoomTypes
                .Include(r => r.Rooms)
                .FirstOrDefaultAsync(r => r.RoomTypeId == id);

            if (roomType == null)
                return NotFound();

            return View(roomType);
        }

        // POST: RoomTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var roomType = await _context.RoomTypes
                .Include(r => r.Rooms)
                .FirstOrDefaultAsync(r => r.RoomTypeId == id);

            if (roomType == null)
                return NotFound();

            // Don't delete a room type that is being used by rooms.
            if (roomType.Rooms != null && roomType.Rooms.Any())
            {
                TempData["Error"] =
                    "This room type cannot be deleted because rooms are using it.";

                return RedirectToAction(nameof(Index));
            }

            _context.RoomTypes.Remove(roomType);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Room type deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private bool RoomTypeExists(int id)
        {
            return _context.RoomTypes
                .Any(e => e.RoomTypeId == id);
        }
    }
}