using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Roots_Wings.Data;
using Roots_Wings.Models;

namespace Roots_Wings.Controllers
{
    [Authorize]
    public class TodayController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;

        public TodayController(ApplicationDbContext context,
                               UserManager<ApplicationUser> userManager)
        {
            db = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var user = await db.Users.FindAsync(userId);
            var now = DateTime.UtcNow;

            var lastSession = db.TherapySessions
                .Where(s => s.UserId == userId && s.SessionDate < now)
                .OrderByDescending(s => s.SessionDate)
                .FirstOrDefault();

            var nextSession = db.TherapySessions
                .Where(s => s.UserId == userId && s.SessionDate >= now)
                .OrderBy(s => s.SessionDate)
                .FirstOrDefault();

            // ── Progress across the gap between two sessions ──
            int percent = 0;
            if (nextSession != null)
            {
                var start = lastSession?.SessionDate ?? nextSession.SessionDate.AddDays(-7);
                var span = (nextSession.SessionDate - start).TotalSeconds;
                var done = (now - start).TotalSeconds;
                var p = span <= 0 ? 1.0 : Math.Clamp(done / span, 0, 1);
                percent = (int)Math.Round(p * 100);
            }

            // Ring: r=54, circumference = 2*PI*54 = 339.29
            double dashOffset = 339.29 * (1 - (percent / 100.0));

            // ── Countdown, resolved here so the view does no logic ──
            string countNum = "—";
            string countUnit = "";
            if (nextSession != null)
            {
                var hours = (nextSession.SessionDate - now).TotalHours;
                if (hours <= 1) { countNum = "now"; countUnit = ""; }
                else if (hours < 24) { countNum = ((int)Math.Ceiling(hours)).ToString(); countUnit = "hours"; }
                else
                {
                    int d = (int)Math.Ceiling((nextSession.SessionDate - now).TotalDays);
                    countNum = d.ToString();
                    countUnit = d == 1 ? "day" : "days";
                }
            }

            // ── Interval material ──
            var intervalStart = lastSession != null ? lastSession.SessionDate : DateTime.MinValue;

            var entries = db.JournalEntries
                .Include(e => e.Tags)
                .Where(e => e.UserId == userId && e.CreatedAt > intervalStart)
                .OrderByDescending(e => e.CreatedAt)
                .ToList();

            JournalEntry featured = entries
                .Where(e => e.Content.Length > 180)
                .OrderByDescending(e => e.Content.Length)
                .FirstOrDefault();

            if (featured == null)
                featured = entries.FirstOrDefault();

            // Themes: plain Dictionary, no anonymous types, no dynamic.
            var themes = entries
                .SelectMany(e => e.Tags)
                .GroupBy(t => t.Name)
                .Where(g => g.Count() > 1)
                .OrderByDescending(g => g.Count())
                .Take(4)
                .ToDictionary(g => g.Key, g => g.Count());

            // ── Growth in this interval ──
            var goals = db.Goals
                .Include(g => g.Milestones)
                .Where(g => g.UserId == userId && g.CompletedAt == null)
                .ToList();

            var goal = goals
                .Where(g => g.Milestones.Any(m => m.CompletedAt == null))
                .OrderByDescending(g => g.Milestones.Count(m => m.CompletedAt != null))
                .FirstOrDefault();

            int grown = goals
                .SelectMany(g => g.Milestones)
                .Count(m => m.CompletedAt.HasValue && m.CompletedAt.Value > intervalStart);

            int bloomed = db.Goals
                .Count(g => g.UserId == userId
                         && g.CompletedAt.HasValue
                         && g.CompletedAt.Value > intervalStart);

            // ── Everything typed, so the view just prints ──
            ViewBag.UserName = user != null ? user.Name : "";
            ViewBag.Today = now.ToString("dddd, d MMMM");
            ViewBag.Greeting = now.Hour < 12 ? "Good morning"
                             : now.Hour < 18 ? "Good afternoon"
                             : "Good evening";
            ViewBag.Line = Lines[now.DayOfYear % Lines.Length];
            ViewBag.Prompt = Prompts[(now.DayOfYear * 7) % Prompts.Length];

            ViewBag.LastSession = lastSession;
            ViewBag.NextSession = nextSession;
            ViewBag.Percent = percent;
            ViewBag.DashOffset = dashOffset.ToString("0.##",
                System.Globalization.CultureInfo.InvariantCulture);
            ViewBag.CountNum = countNum;
            ViewBag.CountUnit = countUnit;

            // Human-readable "when the last session was", built here not in Razor.
            string sinceLabel = "";
            if (lastSession != null)
            {
                int d = (int)Math.Floor((now - lastSession.SessionDate).TotalDays);
                sinceLabel = d == 0 ? "earlier today" : d == 1 ? "yesterday" : d + " days ago";
            }
            ViewBag.SinceLabel = sinceLabel;

            ViewBag.Entries = entries;
            ViewBag.EntryCount = entries.Count;
            ViewBag.Featured = featured;
            ViewBag.Themes = themes;
            ViewBag.Goal = goal;
            ViewBag.Grown = grown;
            ViewBag.Bloomed = bloomed;

            ViewBag.NeedsCapture = lastSession != null
                && (now - lastSession.SessionDate).TotalHours <= 72
                && string.IsNullOrWhiteSpace(lastSession.PostNotes);

            ViewBag.Agenda = SplitAgenda(nextSession != null ? nextSession.PrepNotes : null);

            // Excerpts done here — no helper methods in the view.
            ViewBag.ThreadText = lastSession != null ? Excerpt(lastSession.PostNotes, 300) : "";
            ViewBag.ThreadLong = lastSession != null
                && !string.IsNullOrWhiteSpace(lastSession.PostNotes)
                && lastSession.PostNotes.Length > 300;
            ViewBag.FeaturedText = featured != null ? Excerpt(featured.Content, 240) : "";

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAgendaItem(int sessionId, string item)
        {
            var userId = _userManager.GetUserId(User);
            var session = db.TherapySessions
                .FirstOrDefault(s => s.Id == sessionId && s.UserId == userId);

            if (session == null) return NotFound();
            if (string.IsNullOrWhiteSpace(item)) return RedirectToAction("Index");

            var lines = SplitAgenda(session.PrepNotes);

            // Capped at 3 — CBT agenda-setting guidance holds that more than
            // three items can't be covered properly in one session.
            if (lines.Count >= 3)
            {
                TempData["TodayNote"] = "Three is enough to cover well in one session.";
                return RedirectToAction("Index");
            }

            lines.Add(item.Trim());
            session.PrepNotes = string.Join("\n", lines);
            await db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAgendaItem(int sessionId, int index)
        {
            var userId = _userManager.GetUserId(User);
            var session = db.TherapySessions
                .FirstOrDefault(s => s.Id == sessionId && s.UserId == userId);

            if (session == null) return NotFound();

            var lines = SplitAgenda(session.PrepNotes);
            if (index >= 0 && index < lines.Count)
            {
                lines.RemoveAt(index);
                session.PrepNotes = lines.Count > 0 ? string.Join("\n", lines) : null;
                await db.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        private static List<string> SplitAgenda(string notes)
        {
            if (string.IsNullOrWhiteSpace(notes)) return new List<string>();
            return notes
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToList();
        }

        private static string Excerpt(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Length <= n) return s;
            int cut = s.LastIndexOf(' ', Math.Min(n, s.Length - 1));
            if (cut < 40) cut = n;   // no early space — just hard-cut
            return s.Substring(0, cut) + "…";
        }

        private static readonly string[] Lines = new[]
        {
            "Roots grow in the dark. That still counts as growing.",
            "You don't have to feel ready to be doing the work.",
            "Nothing blooms all year. Rest is part of it.",
            "The quiet weeks are still weeks you showed up for.",
            "Slow is a direction, not a failure.",
            "You can be a work in progress and enough at the same time.",
            "Some things only make sense once you've said them out loud.",
            "Being tired of it doesn't mean you're bad at it.",
            "You've survived every hard day so far. That's the whole record.",
            "Growth rarely feels like growth while it's happening.",
            "The fact that it still hurts means it still matters.",
            "You can hold two true things at once.",
            "Small is not the same as insignificant.",
            "Tending something takes longer than planting it.",
        };

        private static readonly string[] Prompts = new[]
        {
            "What did you avoid this week, and what was underneath it?",
            "What are you tired of explaining?",
            "What do you keep almost saying out loud?",
            "Where did you say yes when you meant no?",
            "What are you carrying that isn't yours?",
            "What have you been putting off feeling?",
            "Who took up the most room in your head this week?",
            "What changed that you haven't let yourself notice?",
            "What felt heavier than it should have — and why that?",
            "Where were you most yourself this week?",
        };
    }
}