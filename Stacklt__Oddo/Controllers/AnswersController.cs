using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stacklt_Odoo.Data;
using Stacklt_Odoo.Models;
using System.Security.Claims;

namespace Stacklt_Odoo.Controllers
{
    public class AnswersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AnswersController> _logger;

        public AnswersController(ApplicationDbContext context, ILogger<AnswersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // POST: Answers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireUserRole")]
        public async Task<IActionResult> Create(int questionId, string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                TempData["Error"] = "Answer cannot be empty.";
                return RedirectToAction("Details", "Questions", new { id = questionId });
            }

            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            var answer = new Answer
            {
                QuestionId = questionId,
                UserId = userId,
                Description = description,
                CreatedAt = DateTime.Now
            };

            _context.Answers.Add(answer);
            await _context.SaveChangesAsync();

            // Add notification for question owner
            var question = await _context.Questions
                .Include(q => q.User)
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);

            if (question != null && question.UserId != userId)
            {
                var notification = new Notification
                {
                    UserId = question.UserId,
                    Message = $"{User.Identity?.Name} answered your question: {question.Title}",
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Your answer has been posted successfully!";
            return RedirectToAction("Details", "Questions", new { id = questionId });
        }

        // POST: Answers/Vote
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireUserRole")]
        public async Task<IActionResult> Vote(int answerId, bool isUpvote)
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            var existingVote = await _context.Votes
                .FirstOrDefaultAsync(v => v.AnswerId == answerId && v.UserId == userId);

            if (existingVote != null)
            {
                // Update existing vote
                existingVote.VoteType = isUpvote;
                existingVote.CreatedAt = DateTime.Now;
            }
            else
            {
                // Create new vote
                var vote = new Vote
                {
                    AnswerId = answerId,
                    UserId = userId,
                    VoteType = isUpvote,
                    CreatedAt = DateTime.Now
                };

                _context.Votes.Add(vote);
            }

            await _context.SaveChangesAsync();

            // Return vote count for AJAX updates
            var voteCount = await _context.Votes
                .Where(v => v.AnswerId == answerId)
                .Select(v => v.VoteType ? 1 : -1)
                .SumAsync();

            return Json(new { success = true, voteCount = voteCount });
        }

        // GET: Answers/Edit/5
        [Authorize(Policy = "RequireUserRole")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var answer = await _context.Answers
                .Include(a => a.Question)
                .FirstOrDefaultAsync(a => a.AnswerId == id);

            if (answer == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            if (answer.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return View(answer);
        }

        // POST: Answers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireUserRole")]
        public async Task<IActionResult> Edit(int id, string description)
        {
            var answer = await _context.Answers
                .Include(a => a.Question)
                .FirstOrDefaultAsync(a => a.AnswerId == id);

            if (answer == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            if (answer.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                ModelState.AddModelError("Description", "Answer cannot be empty.");
                return View(answer);
            }

            answer.Description = description;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your answer has been updated successfully!";
            return RedirectToAction("Details", "Questions", new { id = answer.QuestionId });
        }
    }
} 