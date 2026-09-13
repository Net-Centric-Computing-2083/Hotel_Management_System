using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManagementSystem.Controllers
{
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Rooms
        public async Task<IActionResult> Index()
        {
            var rooms = _context.Rooms
                .Include(r => r.RoomType)
                .OrderBy(r => r.RoomNumber);

            return View(await rooms.ToListAsync());
        }

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

                TempData["Success"] = "Room created successfully.";

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

        // GET: Rooms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var room = await _context.Rooms.FindAsync(id);

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
        public async Task<IActionResult> Edit(int id, Room room)
        {
            if (id != room.RoomId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(room);

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Room updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Rooms.Any(e => e.RoomId == room.RoomId))
                        return NotFound();

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

        // GET: Rooms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(m => m.RoomId == id);

            if (room == null)
                return NotFound();

            return View(room);
        }

        // POST: Rooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);

            if (room != null)
            {
                _context.Rooms.Remove(room);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}