using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Roots_Wings.Data;
using Roots_Wings.DTOs;
using Roots_Wings.Helpers;
using Roots_Wings.Models;
using Roots_Wings.Services;

namespace Roots_Wings.Controllers
{
    [Authorize]
    public class TherapySessionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAiSummaryService _aiSummaryService;
        private readonly IPdfExportService _pdfExportService;

        public TherapySessionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAiSummaryService aiSummaryService, IPdfExportService pdfExportService)
        {
            _context = context;
            _userManager = userManager;
            _aiSummaryService = aiSummaryService;
            _pdfExportService = pdfExportService;
        }

        // GET: /Sessions
        public async Task<IActionResult> Index(int? month, int? year)
        {
            var userId = _userManager.GetUserId(User);

            var today = DateTime.Today;

            // Use query params if provided, otherwise default to current month
            int displayMonth = month ?? today.Month;
            int displayYear = year ?? today.Year;

            // Guard against invalid values someone might type in the URL
            if (displayMonth < 1 || displayMonth > 12)
                displayMonth = today.Month;

            ViewBag.DisplayMonth = displayMonth;
            ViewBag.DisplayYear = displayYear;

            var sessions = await _context.TherapySessions
                .Where(s => s.UserId == userId)
                .OrderBy(s => s.SessionDate)
                .ToListAsync();

            return View(sessions);
        }

        // GET: /Sessions/Details/5
        public async Task<IActionResult> Details(int id, int rangeDays = 7)
        {
            var userId = _userManager.GetUserId(User);

            var session = await _context.TherapySessions
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (session == null)
            {
                return NotFound();
            }

            var rangeStart = session.SessionDate.AddDays(-rangeDays);
            var rangeEnd = session.SessionDate.AddDays(rangeDays);

            // Journal entries written close to this session date
            var linkedEntries = await _context.JournalEntries
                .Where(e => e.UserId == userId
                    && e.CreatedAt >= rangeStart
                    && e.CreatedAt <= rangeEnd)
                .Include(e => e.Tags)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync();

            
            ViewBag.LinkedEntries = linkedEntries;
            ViewBag.SelectedRangeDays = rangeDays;
            ViewBag.SessionHasPassed = session.SessionDate < DateTime.UtcNow;

            return View(session);
        }

        // GET: /Sessions/Create
        public IActionResult Create()
        {
            return View(new TherapySession { SessionDate = DateTime.Today.AddDays(1).AddHours(10) });
        }

        // POST: /Sessions/Create
        [HttpPost]
        [ValidateAntiForgeryToken] // For CSRF protection
        public async Task<IActionResult> Create(TherapySession newSession)
        {
            // These fields are set by the app, not the form
            // So we remove them from validation
            ModelState.Remove("UserId");
            ModelState.Remove("User");

            if (!TherapyTypeOptions.IsValid(newSession.TherapyType))
            {
                ModelState.AddModelError("TherapyType", "Please choose a valid therapy type.");
            }

            if (!ModelState.IsValid)
            {
                return View(newSession);
            }

            var userId = _userManager.GetUserId(User);
            var sessionDateUtc = newSession.SessionDate.ToUtc();

            // Check if the user already has a session on this calendar day
            var dayStart = sessionDateUtc.Date;
            var dayEnd = dayStart.AddDays(1);

            bool dayAlreadyTaken = await _context.TherapySessions
                .AnyAsync(s => s.UserId == userId
                            && s.SessionDate >= dayStart
                            && s.SessionDate < dayEnd);

            if (dayAlreadyTaken)
            {
                ModelState.AddModelError("SessionDate", "You already have a session on this day.");
                return View(newSession);
            }

           
            newSession.UserId = userId;
            newSession.CreatedAt = DateTime.UtcNow;
            newSession.SessionDate = sessionDateUtc;
            _context.TherapySessions.Add(newSession);

            await _context.SaveChangesAsync();

            TempData["Flash"] = "Session added successfully";
            TempData["FlashType"] = "success";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Sessions/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);

            var session = await _context.TherapySessions
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (session == null)
            {
                return NotFound();
            }

            return View(session);
        }

        // POST: /Sessions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TherapySession model)
        {
            ModelState.Remove("UserId");
            ModelState.Remove("User");

            if (!TherapyTypeOptions.IsValid(model.TherapyType))
            {
                ModelState.AddModelError("TherapyType", "Please choose a valid therapy type.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            var session = await _context.TherapySessions
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (session == null)
            {
                return NotFound();
            }

            var sessionDateUtc = model.SessionDate.ToUtc();

            // Check if the new date conflicts with another session on that day
            // Excluding the current session from the check
            var dayStart = sessionDateUtc.Date;
            var dayEnd = dayStart.AddDays(1);

            bool dayAlreadyTaken = await _context.TherapySessions
                .AnyAsync(s => s.UserId == userId
                            && s.Id != id
                            && s.SessionDate >= dayStart
                            && s.SessionDate < dayEnd);

            if (dayAlreadyTaken)
            {
                ModelState.AddModelError("SessionDate", "You already have a session on this day.");
                return View(model);
            }

            session.Title = model.Title;
            session.TherapistName = model.TherapistName;
            session.PrepNotes = model.PrepNotes;
            session.PostNotes = model.PostNotes;
            session.SessionDate = sessionDateUtc;
            session.TherapyType = model.TherapyType;

            await _context.SaveChangesAsync();

            TempData["Flash"] = "Changes saved successfully";
            TempData["FlashType"] = "success";

            return RedirectToAction(nameof(Details), new { id = session.Id });
        }

        // POST: /Sessions/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);

            var session = await _context.TherapySessions
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (session == null)
            {
                return NotFound();
            }

            _context.TherapySessions.Remove(session);
            await _context.SaveChangesAsync();

            TempData["Flash"] = "Session deleted successfully";
            TempData["FlashType"] = "success";

            return RedirectToAction(nameof(Index));
        }

        // GET: /Sessions/SummariseConsent/5?rangeDays=7
        public async Task<IActionResult> SummariseConsent(int id, int rangeDays = 7)
        {
            var userId = _userManager.GetUserId(User);

            var session = await _context.TherapySessions
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (session == null) return NotFound();

            // Count how many entries will be included
            var rangeStart = session.SessionDate.AddDays(-rangeDays);
            var rangeEnd = session.SessionDate.AddDays(rangeDays);

            var entryCount = await _context.JournalEntries
                .CountAsync(e => e.UserId == userId
                              && e.CreatedAt >= rangeStart
                              && e.CreatedAt <= rangeEnd);

            ViewBag.EntryCount = entryCount;
            ViewBag.RangeDays = rangeDays;
            ViewBag.SessionTitle = session.Title;

            return View(session);
        }

        // POST: /Sessions/Summarise/5
        // Called from the consent page form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Summarise(int id, int rangeDays = 7)
        {
            var userId = _userManager.GetUserId(User);

            var session = await _context.TherapySessions
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (session == null) return NotFound();

            var rangeStart = session.SessionDate.AddDays(-rangeDays);
            var rangeEnd = session.SessionDate.AddDays(rangeDays);

            var entries = await _context.JournalEntries
                        .Where(e => e.UserId == userId
                                 && e.CreatedAt >= rangeStart
                                 && e.CreatedAt <= rangeEnd)
                        .Include(e => e.Tags)
                        .OrderBy(e => e.CreatedAt)
                        .ToListAsync();

            // Call the AI
            SummaryResult summary;
            try
            {
                summary = await _aiSummaryService.SummariseEntriesAsync(entries, session);
            }
            catch
            {
                TempData["Flash"] = "The summary couldn't be generated right now. Please try again in a moment.";
                TempData["FlashType"] = "error";
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.Summary = summary;
            ViewBag.SessionId = id;
            ViewBag.RangeDays = rangeDays;
            return View(session);
        }

        // POST: /Sessions/ExportPdf/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportPdf(
            int id,
            int rangeDays,
            string[] includeBlocks,       // the checked toggles from the form
            string summaryOverview,
            string summaryThemes,
            string summaryDiscuss)
        {
            var userId = _userManager.GetUserId(User);

            var session = await _context.TherapySessions
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (session == null) return NotFound();

            // Rebuild the summary DTO from the posted hidden fields
            var summary = new SummaryResult
            {
                Overall = summaryOverview,
                WhatKeptComingUp = summaryThemes,
                MightBringUp = summaryDiscuss
            };

            var pdfBytes = _pdfExportService.GenerateSummaryPdf(
                session, summary, includeBlocks ?? Array.Empty<string>());

            // Stream the file directly to the browser as a download
            var fileName = $"therapy-summary-{session.SessionDate:yyyy-MM-dd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}