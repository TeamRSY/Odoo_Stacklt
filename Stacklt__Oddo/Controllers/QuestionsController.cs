using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stacklt_Odoo.Data;
using Stacklt_Odoo.Models;
using Stacklt_Odoo.Models.ViewModels;
using System.Security.Claims;

namespace Stacklt_Odoo.Controllers
{
    public class QuestionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuestionsController> _logger;

        public QuestionsController(ApplicationDbContext context, ILogger<QuestionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Questions
        public async Task<IActionResult> Index(string? search, string? tag, string? sort = "newest")
        {
            var query = _context.Questions
                .Include(q => q.User)
                .Include(q => q.Answers)
                .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(q => q.Title.Contains(search) || q.Description.Contains(search));
            }

            // Apply tag filter
            if (!string.IsNullOrEmpty(tag))
            {
                query = query.Where(q => q.QuestionTags.Any(qt => qt.Tag.TagName.Contains(tag)));
            }

            // Apply sorting
            query = sort switch
            {
                "oldest" => query.OrderBy(q => q.CreatedAt),
                "most-answers" => query.OrderByDescending(q => q.Answers.Count),
                "unanswered" => query.Where(q => !q.Answers.Any()).OrderByDescending(q => q.CreatedAt),
                _ => query.OrderByDescending(q => q.CreatedAt) // newest
            };

            var questions = await query.ToListAsync();

            ViewBag.Search = search;
            ViewBag.Tag = tag;
            ViewBag.Sort = sort;

            return View(questions);
        }

        // GET: Questions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.User)
                .Include(q => q.Answers)
                .ThenInclude(a => a.User)
                .Include(q => q.Answers)
                .ThenInclude(a => a.Votes)
                .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
                .Include(q => q.AcceptedAnswer)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
            {
                return NotFound();
            }

            return View(question);
        }

        // GET: Questions/Ask
        [Authorize(Policy = "RequireUserRole")]
        public IActionResult Ask()
        {
            return View(new QuestionViewModel());
        }

        // POST: Questions/Ask
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireUserRole")]
        public async Task<IActionResult> Ask(QuestionViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

                var question = new Question
                {
                    UserId = userId,
                    Title = model.Title,
                    Description = model.Description,
                    CreatedAt = DateTime.Now
                };

                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                // Add tags
                if (model.Tags != null && model.Tags.Any())
                {
                    foreach (var tagName in model.Tags)
                    {
                        if (!string.IsNullOrWhiteSpace(tagName))
                        {
                            var tag = await _context.Tags.FirstOrDefaultAsync(t => t.TagName.ToLower() == tagName.ToLower());
                            if (tag == null)
                            {
                                tag = new Tag { TagName = tagName.Trim() };
                                _context.Tags.Add(tag);
                                await _context.SaveChangesAsync();
                            }

                            var questionTag = new QuestionTag
                            {
                                QuestionId = question.QuestionId,
                                TagId = tag.TagId
                            };

                            _context.QuestionTags.Add(questionTag);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Details), new { id = question.QuestionId });
            }

            return View(model);
        }

        // POST: Questions/AcceptAnswer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireUserRole")]
        public async Task<IActionResult> AcceptAnswer(int questionId, int answerId)
        {
            var question = await _context.Questions
                .Include(q => q.User)
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);

            if (question == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            // Only the question owner can accept an answer
            if (question.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            question.AcceptedAnswerId = answerId;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = questionId });
        }

        // GET: Questions/Edit/5
        [Authorize(Policy = "RequireUserRole")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            if (question.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var viewModel = new QuestionViewModel
            {
                QuestionId = question.QuestionId,
                Title = question.Title,
                Description = question.Description,
                Tags = question.QuestionTags.Select(qt => qt.Tag.TagName).ToList()
            };

            return View(viewModel);
        }

        // POST: Questions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireUserRole")]
        public async Task<IActionResult> Edit(int id, QuestionViewModel model)
        {
            if (id != model.QuestionId)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            if (question.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                question.Title = model.Title;
                question.Description = model.Description;

                // Remove existing tags
                _context.QuestionTags.RemoveRange(question.QuestionTags);

                // Add new tags
                if (model.Tags != null && model.Tags.Any())
                {
                    foreach (var tagName in model.Tags)
                    {
                        if (!string.IsNullOrWhiteSpace(tagName))
                        {
                            var tag = await _context.Tags.FirstOrDefaultAsync(t => t.TagName.ToLower() == tagName.ToLower());
                            if (tag == null)
                            {
                                tag = new Tag { TagName = tagName.Trim() };
                                _context.Tags.Add(tag);
                                await _context.SaveChangesAsync();
                            }

                            var questionTag = new QuestionTag
                            {
                                QuestionId = question.QuestionId,
                                TagId = tag.TagId
                            };

                            _context.QuestionTags.Add(questionTag);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Question updated successfully!";
                return RedirectToAction(nameof(Details), new { id = question.QuestionId });
            }

            return View(model);
        }

        // POST: Questions/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireUserRole")]
        public async Task<IActionResult> Delete(int id)
        {
            var question = await _context.Questions
                .Include(q => q.User)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            if (question.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // First, clear the AcceptedAnswerId to break the circular dependency
            question.AcceptedAnswerId = null;
            await _context.SaveChangesAsync();

            // Delete related data (answers, votes, notifications, question tags)
            var answers = await _context.Answers.Where(a => a.QuestionId == id).ToListAsync();
            var answerIds = answers.Select(a => a.AnswerId).ToList();
            
            // Delete votes for answers
            if (answerIds.Any())
            {
                var votes = await _context.Votes.Where(v => answerIds.Contains(v.AnswerId)).ToListAsync();
                _context.Votes.RemoveRange(votes);
                await _context.SaveChangesAsync();
            }

            // Delete answers
            _context.Answers.RemoveRange(answers);
            await _context.SaveChangesAsync();

            // Delete question tags
            var questionTags = await _context.QuestionTags.Where(qt => qt.QuestionId == id).ToListAsync();
            _context.QuestionTags.RemoveRange(questionTags);
            await _context.SaveChangesAsync();

            // Delete notifications related to this question
            var notifications = await _context.Notifications
                .Where(n => n.Message.Contains($"answered your question: {question.Title}"))
                .ToListAsync();
            _context.Notifications.RemoveRange(notifications);
            await _context.SaveChangesAsync();

            // Finally, delete the question
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Question deleted successfully!";
            return RedirectToAction("Index", "Home");
        }
    }
} 