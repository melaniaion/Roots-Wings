using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Roots_Wings.Data;
using Roots_Wings.Helpers;
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

            // ── Next session ──
            var nextSession = db.TherapySessions
                .Where(s => s.UserId == userId && s.SessionDate >= now)
                .OrderBy(s => s.SessionDate)
                .FirstOrDefault();

            // ── Countdown, resolved here so the view only prints ──
            string countNum = "";
            string countUnit = "";
            string dashOffset = "339.29";

            if (nextSession != null)
            {
                var hours = (nextSession.SessionDate - now).TotalHours;

                if (hours <= 1)
                {
                    countNum = "now";
                    countUnit = "";
                }
                else if (hours < 24)
                {
                    int h = (int)Math.Ceiling(hours);
                    countNum = h.ToString();
                    countUnit = h == 1 ? "hour" : "hours";
                }
                else
                {
                    int d = (int)Math.Ceiling((nextSession.SessionDate - now).TotalDays);
                    countNum = d.ToString();
                    countUnit = d == 1 ? "day" : "days";
                }

                // The ring fills over the final 14 days — a visual approach,
                // not a metric. Far out = nearly empty, session day = full.
                double daysOut = (nextSession.SessionDate - now).TotalDays;
                double fill = Math.Clamp(1 - (daysOut / 14.0), 0.04, 1);
                dashOffset = (339.29 * (1 - fill)).ToString("0.##",
                    System.Globalization.CultureInfo.InvariantCulture);
            }

            // ── Top 3 most recently added goals ──
            var goals = db.Goals
                .Include(g => g.Milestones)
                .Where(g => g.UserId == userId && g.CompletedAt == null)
                .OrderByDescending(g => g.CreatedAt)
                .Take(3)
                .ToList();

            // ── Most recent journal entry ──
            var lastEntry = db.JournalEntries
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefault();

            string entryWhen = "";
            string entryText = "";
            if (lastEntry != null)
            {
                var stamp = (lastEntry.UpdatedAt ?? lastEntry.CreatedAt).ToLocal();
                var localNow = now.ToLocal();
                bool edited = lastEntry.UpdatedAt.HasValue;

                // Compare calendar dates, not elapsed hours — an entry from 23:00 yesterday
                // read at 08:00 today is 9 hours old but still "Yesterday".
                int days = (int)(localNow.Date - stamp.Date).TotalDays;
                string time = stamp.ToString("HH:mm");

                entryWhen = days == 0 ? "Today, " + time
                          : days == 1 ? "Yesterday, " + time
                          : stamp.ToString("d MMM") + ", " + time;

                if (edited) entryWhen = "Edited " + entryWhen.ToLower();
                entryText = Excerpt(lastEntry.Content, 150);
            }

            ViewBag.UserName = user != null ? user.Name : "";
            ViewBag.Today = now.ToLocal().ToString("dddd, d MMMM yyyy");

            int hour = now.ToLocal().Hour;
            ViewBag.Greeting = hour < 12 ? "Good morning"
                             : hour < 18 ? "Good afternoon"
                             : "Good evening";

            string sessionDate = "";
            if (nextSession != null)
                sessionDate = nextSession.SessionDate.ToLocal().ToString("d MMMM yyyy, HH:mm");

            ViewBag.NextSession = nextSession;
            ViewBag.SessionDate = sessionDate;
            ViewBag.CountNum = countNum;
            ViewBag.CountUnit = countUnit;
            ViewBag.DashOffset = dashOffset;
            ViewBag.HasPrep = nextSession != null && !string.IsNullOrWhiteSpace(nextSession.PrepNotes);

            ViewBag.Goals = goals;
            ViewBag.LastEntry = lastEntry;
            ViewBag.EntryWhen = entryWhen;
            ViewBag.EntryText = entryText;

            return View();
        }

        private static string Excerpt(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Length <= n) return s;
            int cut = s.LastIndexOf(' ', Math.Min(n, s.Length - 1));
            if (cut < 40) cut = n;
            return s.Substring(0, cut) + "...";
        }
    }
}