using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stacklt_Odoo.Data;
using Stacklt_Odoo.Models;

namespace Stacklt_Odoo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private const int PageSize = 5;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string? filter = "newest", int page = 1)
        {
            try
            {
                var query = _context.Questions
                    .Include(q => q.User)
                    .Include(q => q.Answers)
                    .Include(q => q.QuestionTags)
                    .ThenInclude(qt => qt.Tag)
                    .AsQueryable();

                // Search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(q => q.Title.Contains(search) || q.Description.Contains(search));
                }

                // Filter
                switch (filter)
                {
                    case "unanswered":
                        query = query.Where(q => !q.Answers.Any());
                        break;
                    case "newest":
                        query = query.OrderByDescending(q => q.CreatedAt);
                        break;
                    case "oldest":
                        query = query.OrderBy(q => q.CreatedAt);
                        break;
                    default:
                        query = query.OrderByDescending(q => q.CreatedAt);
                        break;
                }

                // Pagination
                var totalQuestions = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalQuestions / (double)PageSize);
                var questions = await query
                    .Skip((page - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.Search = search;
                ViewBag.Filter = filter;

                return View(questions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading questions");
                return View(new List<Question>());
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Test action to verify database connection
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                // Test if we can connect to the database
                var userCount = await _context.Users.CountAsync();
                var questionCount = await _context.Questions.CountAsync();
                var tagCount = await _context.Tags.CountAsync();
                var answerCount = await _context.Answers.CountAsync();
                var voteCount = await _context.Votes.CountAsync();
                var notificationCount = await _context.Notifications.CountAsync();

                var result = new
                {
                    Success = true,
                    Message = "Database connection successful!",
                    UserCount = userCount,
                    QuestionCount = questionCount,
                    TagCount = tagCount,
                    AnswerCount = answerCount,
                    VoteCount = voteCount,
                    NotificationCount = notificationCount,
                    ConnectionString = _context.Database.GetConnectionString()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                var result = new
                {
                    Success = false,
                    Message = $"Database connection failed: {ex.Message}",
                    Error = ex.ToString()
                };

                return Json(result);
            }
        }

        // Test action to add sample data
        public async Task<IActionResult> AddTestData()
        {
            try
            {
                // Check if test user exists
                var testUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "testuser");
                if (testUser == null)
                {
                    // Create test user
                    testUser = new User
                    {
                        Username = "testuser",
                        Email = "test@example.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                        Role = "User",
                        CreatedAt = DateTime.Now
                    };
                    _context.Users.Add(testUser);
                    await _context.SaveChangesAsync();
                }

                // Create test tags
                var tags = new[] { "csharp", "aspnet", "javascript", "html", "css" };
                foreach (var tagName in tags)
                {
                    var tag = await _context.Tags.FirstOrDefaultAsync(t => t.TagName == tagName);
                    if (tag == null)
                    {
                        tag = new Tag { TagName = tagName };
                        _context.Tags.Add(tag);
                    }
                }
                await _context.SaveChangesAsync();

                // Create test question
                var testQuestion = new Question
                {
                    UserId = testUser.UserId,
                    Title = "How to create a Q&A platform with ASP.NET Core?",
                    Description = "<p>I'm building a Q&A platform similar to Stack Overflow. Can anyone help me with the basic structure?</p>",
                    CreatedAt = DateTime.Now
                };
                _context.Questions.Add(testQuestion);
                await _context.SaveChangesAsync();

                // Add tags to question
                var csharpTag = await _context.Tags.FirstOrDefaultAsync(t => t.TagName == "csharp");
                var aspnetTag = await _context.Tags.FirstOrDefaultAsync(t => t.TagName == "aspnet");
                
                if (csharpTag != null)
                {
                    _context.QuestionTags.Add(new QuestionTag { QuestionId = testQuestion.QuestionId, TagId = csharpTag.TagId });
                }
                if (aspnetTag != null)
                {
                    _context.QuestionTags.Add(new QuestionTag { QuestionId = testQuestion.QuestionId, TagId = aspnetTag.TagId });
                }
                await _context.SaveChangesAsync();

                // Create test answer
                var testAnswer = new Answer
                {
                    QuestionId = testQuestion.QuestionId,
                    UserId = testUser.UserId,
                    Description = "<p>You can start by creating Entity Framework models and controllers. Here's a basic example...</p>",
                    CreatedAt = DateTime.Now
                };
                _context.Answers.Add(testAnswer);
                await _context.SaveChangesAsync();

                var result = new
                {
                    Success = true,
                    Message = "Test data added successfully!",
                    TestUser = testUser.Username,
                    TestQuestion = testQuestion.Title,
                    TestAnswer = "Answer created"
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                var result = new
                {
                    Success = false,
                    Message = $"Error adding test data: {ex.Message}",
                    Error = ex.ToString()
                };

                return Json(result);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
