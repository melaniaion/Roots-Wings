using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Roots_Wings.Data;
using Roots_Wings.Models;

namespace Roots_Wings.Controllers
{
    [Authorize]
    public class JournalEntriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public JournalEntriesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Journal
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var entries = await _context.JournalEntries
                .Where(e => e.UserId == userId)
                .Include(e => e.Tags)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return View(entries);
        }

        // GET: /Journal/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            var entry = await _context.JournalEntries
                .Include(e => e.Tags)
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (entry == null)
            {
                return NotFound();
            }

            return View(entry);
        }

        // GET: /Journal/Create
        public async Task<IActionResult> Create()
        {
            // Pass all predefined tags to the view so the user can pick from them
            ViewBag.AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync();

            return View(new JournalEntry());
        }

        // POST: /Journal/Create
        // selectedTagIds comes from the checkboxes in the form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JournalEntry model, List<int> selectedTagIds)
        {
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Tags");

            if (!ModelState.IsValid)
            {
                ViewBag.AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync();
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            model.UserId = userId;
            model.CreatedAt = DateTime.UtcNow;

            _context.JournalEntries.Add(model);
            await _context.SaveChangesAsync();

            // Attach the selected tags after the entry has an Id
            if (selectedTagIds != null && selectedTagIds.Any())
            {
                var tags = await _context.Tags
                    .Where(t => selectedTagIds.Contains(t.Id))
                    .ToListAsync();

                foreach (var tag in tags)
                {
                    model.Tags.Add(tag);
                }

                await _context.SaveChangesAsync();
            }

            TempData["Flash"] = "Page added successfully";
            TempData["FlashType"] = "success";

            return RedirectToAction(nameof(Index));
        }

        // GET: /Journal/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);

            // Include Tags so we know which ones are already selected
            var entry = await _context.JournalEntries
                .Include(e => e.Tags)
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (entry == null)
            {
                return NotFound();
            }

            ViewBag.AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync();

            // Pass the already-selected tag IDs so the view can pre-check the boxes
            ViewBag.SelectedTagIds = entry.Tags.Select(t => t.Id).ToList();

            return View(entry);
        }

        // POST: /Journal/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, JournalEntry model, List<int> selectedTagIds)
        {
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Tags");

            if (!ModelState.IsValid)
            {
                ViewBag.AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync();
                ViewBag.SelectedTagIds = selectedTagIds ?? new List<int>();
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            // Must Include Tags here so EF Core can track and update the relationship
            var entry = await _context.JournalEntries
                .Include(e => e.Tags)
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (entry == null)
            {
                return NotFound();
            }

            entry.Title = model.Title;
            entry.Content = model.Content;
            entry.UpdatedAt = DateTime.UtcNow;

            // Replace tags: clear old ones, add the newly selected ones
            entry.Tags.Clear();

            if (selectedTagIds != null && selectedTagIds.Any())
            {
                var tags = await _context.Tags
                    .Where(t => selectedTagIds.Contains(t.Id))
                    .ToListAsync();

                foreach (var tag in tags)
                {
                    entry.Tags.Add(tag);
                }
            }

            await _context.SaveChangesAsync();

            TempData["Flash"] = "Changes saved successfully";
            TempData["FlashType"] = "success"; 

            return RedirectToAction(nameof(Details), new { id = entry.Id });
        }

        // POST: /Journal/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);

            var entry = await _context.JournalEntries
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (entry == null)
            {
                return NotFound();
            }

            _context.JournalEntries.Remove(entry);
            await _context.SaveChangesAsync();

            TempData["Flash"] = "Page deleted successfully";
            TempData["FlashType"] = "success";

            return RedirectToAction(nameof(Index));
        }
    }
}