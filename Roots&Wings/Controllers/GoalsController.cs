using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Roots_Wings.Data;
using Roots_Wings.Models;

namespace Roots_Wings.Controllers
{
    [Authorize]
    public class GoalsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const int MaxMilestonesPerGoal = 6;

        public GoalsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Goals
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            // Active goals remain fluid on the board
            var activeGoals = await _context.Goals
                .Where(g => g.UserId == userId && g.CompletedAt == null)
                .Include(g => g.Milestones.OrderBy(m => m.Order))
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

          
            var winsShelf = await _context.Goals
                .Where(g => g.UserId == userId && g.CompletedAt != null)
                .Include(g => g.Milestones.OrderBy(m => m.Order))
                .OrderByDescending(g => g.CompletedAt)
                .ToListAsync();

            ViewBag.WinsShelf = winsShelf;

            return View(activeGoals);
        }


        // GET: /Goals/Completed
        public async Task<IActionResult> Completed()
        {
            var userId = _userManager.GetUserId(User);

            // Pulls every single past win for the deep archive page
            var allWins = await _context.Goals
                .Where(g => g.UserId == userId && g.CompletedAt != null)
                .Include(g => g.Milestones.OrderBy(m => m.Order))
                .OrderByDescending(g => g.CompletedAt)
                .ToListAsync();

            return View(allWins);
        }

        // GET: /Goals/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            var goal = await _context.Goals
                .Include(g => g.Milestones.OrderBy(m => m.Order))
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null)
            {
                return NotFound();
            }

            ViewBag.MaxMilestones = MaxMilestonesPerGoal;
            return View(goal);
        }

        // GET: /Goals/Create
        public IActionResult Create()
        {
            ViewBag.MaxMilestones = MaxMilestonesPerGoal;
            return View(new Goal());
        }

        // POST: /Goals/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Goal newGoal, string[] milestoneDescriptions)
        {
            // Set by the app, not the form
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Milestones");

            var cleanedMilestones = (milestoneDescriptions ?? Array.Empty<string>())
                .Select(d => d?.Trim())
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .ToList();

            // A goal with no milestones has nothing to check off and nothing
            // that could ever move it to the Wins Shelf, so it isn't a valid goal yet.
            if (cleanedMilestones.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Add at least one milestone to get started.");
            }

            if (cleanedMilestones.Count > MaxMilestonesPerGoal)
            {
                ModelState.AddModelError(string.Empty,
                    $"A goal can hold up to {MaxMilestonesPerGoal} milestones. If it needs more, it's probably two or more goals :)");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.MilestoneDescriptions = cleanedMilestones;
                ViewBag.MaxMilestones = MaxMilestonesPerGoal;
                return View(newGoal);
            }

            var userId = _userManager.GetUserId(User);

            newGoal.UserId = userId;
            newGoal.CreatedAt = DateTime.UtcNow;
            newGoal.CompletedAt = null;
            newGoal.Milestones = cleanedMilestones
                .Select((description, index) => new Milestone
                {
                    Description = description,
                    Order = index
                })
                .ToList();

            _context.Goals.Add(newGoal);
            await _context.SaveChangesAsync();

            TempData["Flash"] = "Goal created successfully";
            TempData["FlashType"] = "success";

            return RedirectToAction(nameof(Index));
        }

        // GET: /Goals/Edit/5
        // Only Title and Motivation are editable here; milestones have their own actions.
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);

            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null)
            {
                return NotFound();
            }

            return View(goal);
        }

        // POST: /Goals/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Goal model)
        {
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Milestones");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null)
            {
                return NotFound();
            }

            goal.Title = model.Title;
            goal.Motivation = model.Motivation;

            await _context.SaveChangesAsync();


            TempData["Flash"] = "Changes saved successfully";
            TempData["FlashType"] = "success";

            return RedirectToAction(nameof(Details), new { id = goal.Id });
        }

        // POST: /Goals/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);

            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null)
            {
                return NotFound();
            }

            // Milestones cascade-delete via the FK configured in ApplicationDbContext.
            _context.Goals.Remove(goal);
            await _context.SaveChangesAsync();

            TempData["Flash"] = "Goal deleted successfully";
            TempData["FlashType"] = "success";

            return RedirectToAction(nameof(Index));
        }

        // POST: /Goals/AddMilestone/5  (id = goal id)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMilestone(int id, string description)
        {
            var userId = _userManager.GetUserId(User);

            var goal = await _context.Goals
                .Include(g => g.Milestones)
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null)
            {
                return NotFound();
            }

            // A completed goal is a closed record on the Wins Shelf.
            // Reopening it with a fresh, unfinished milestone would contradict
            // its own "done" status, so this is blocked rather than allowed.
            if (goal.CompletedAt != null)
            {
                TempData["GoalError"] = "This goal is already complete and can't take new milestones.";
                return RedirectToAction(nameof(Details), new { id = goal.Id });
            }

            // Check number of milestones, shouldn't exceed the max
            if (goal.Milestones.Count >= MaxMilestonesPerGoal)
            {
                TempData["GoalError"] = $"This goal already holds {MaxMilestonesPerGoal} milestones, which is the most it can take.";
                return RedirectToAction(nameof(Details), new { id = goal.Id });
            }

            description = description?.Trim();

            if (string.IsNullOrWhiteSpace(description) || description.Length < 2 || description.Length > 200)
            {
                TempData["GoalError"] = "A milestone needs to be between 2 and 200 characters.";
                return RedirectToAction(nameof(Details), new { id = goal.Id });
            }

            int nextOrder = goal.Milestones.Any() ? goal.Milestones.Max(m => m.Order) + 1 : 0;

            goal.Milestones.Add(new Milestone
            {
                Description = description,
                Order = nextOrder
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = goal.Id });
        }

        // POST: /Goals/EditMilestone/5  (id = milestone id)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMilestone(int id, string description)
        {
            var userId = _userManager.GetUserId(User);

            var goal = await _context.Goals
                .Include(g => g.Milestones)
                .FirstOrDefaultAsync(g => g.UserId == userId && g.Milestones.Any(m => m.Id == id));

            if (goal == null)
            {
                return NotFound();
            }

            description = description?.Trim();

            if (string.IsNullOrWhiteSpace(description) || description.Length < 2 || description.Length > 200)
            {
                TempData["GoalError"] = "A milestone needs to be between 2 and 200 characters.";
                return RedirectToAction(nameof(Details), new { id = goal.Id });
            }

            // Renaming only changes text, never CompletedAt, so it's safe to allow
            // even on a completed goal -- unlike AddMilestone, it can't reopen one.
            var milestone = goal.Milestones.First(m => m.Id == id);
            milestone.Description = description;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = goal.Id });
        }

        // POST: /Goals/ToggleMilestone/5  (id = milestone id)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMilestone(int id)
        {
            var userId = _userManager.GetUserId(User);

            // One query brings back the goal and every milestone on it, scoped
            // to the current user, so ownership and completion math both work
            // off data already in memory -- no per-item follow-up queries.
            var goal = await _context.Goals
                .Include(g => g.Milestones)
                .FirstOrDefaultAsync(g => g.UserId == userId && g.Milestones.Any(m => m.Id == id));

            if (goal == null)
            {
                return NotFound();
            }

            var milestone = goal.Milestones.First(m => m.Id == id);
            milestone.CompletedAt = milestone.CompletedAt.HasValue ? null : DateTime.UtcNow;

            RecalculateGoalCompletion(goal);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = goal.Id });
        }

        // POST: /Goals/DeleteMilestone/5  (id = milestone id)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMilestone(int id)
        {
            var userId = _userManager.GetUserId(User);

            var goal = await _context.Goals
                .Include(g => g.Milestones)
                .FirstOrDefaultAsync(g => g.UserId == userId && g.Milestones.Any(m => m.Id == id));

            if (goal == null)
            {
                return NotFound();
            }

            var milestone = goal.Milestones.First(m => m.Id == id);
            goal.Milestones.Remove(milestone);
            _context.Milestones.Remove(milestone);

            RecalculateGoalCompletion(goal);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = goal.Id });
        }

        // A goal is complete only when it has milestones and every one of them
        // is checked off. Runs entirely against the already-loaded collection,
        // so it never issues its own query, and it works in both directions:
        // finishing the last milestone completes the goal, unchecking one later
        // reopens it.
        private static void RecalculateGoalCompletion(Goal goal)
        {
            bool allMilestonesDone = goal.Milestones.Any() && goal.Milestones.All(m => m.CompletedAt.HasValue);

            if (allMilestonesDone && goal.CompletedAt == null)
            {
                goal.CompletedAt = DateTime.UtcNow;
            }
            else if (!allMilestonesDone && goal.CompletedAt != null)
            {
                goal.CompletedAt = null;
            }
        }
    }
}