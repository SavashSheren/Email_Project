using Email_Project.Context;
using Email_Project.Dtos;
using Email_Project.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Globalization;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading.Tasks;

namespace Email_Project.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly EmailContext _context;

        public ProfileController(UserManager<AppUser> userManager, EmailContext context)
        {
            this._userManager = userManager;
            _context = context;

        }

        public async Task<IActionResult> Index()
        {

            var values = await _userManager.FindByNameAsync(User.Identity.Name);
            UserEditDto userEditDto = new UserEditDto();
            userEditDto.Name = values.Name;
            userEditDto.Surname = values.Surname;
            userEditDto.ImageUrl = values.ImageUrl;
            userEditDto.Email = values.Email;

            return View(userEditDto);
        }

        [HttpPost]
        public async Task<IActionResult> Index(UserEditDto userEditDto)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            user.Name = userEditDto.Name;
            user.Surname = userEditDto.Surname;
            user.Email = userEditDto.Email;
            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, userEditDto.Password);

            var resource = Directory.GetCurrentDirectory();
            var extension = Path.GetExtension(userEditDto.Image.FileName);
            var ImageName = Guid.NewGuid() + extension;
            var savelocation = resource + "/wwwroot/Images/" + ImageName;
            var stream = new FileStream(savelocation, FileMode.Create);
            await userEditDto.Image.CopyToAsync(stream);
            user.ImageUrl = ImageName;


            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Message");
            }
            else
            {
                return View();
            }
        }
        public async Task<IActionResult> AdminProfile()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null) return RedirectToAction("Login", "Account");

            var userMail = user.Email;

            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            // -----------------------------
            // KPI COUNTS (THIS MONTH)
            // -----------------------------
            ViewBag.IncomingCount = await _context.Messages.CountAsync(x =>
                x.ReceiverEmail == userMail &&
                !x.IsTrash &&
                x.SendDate >= monthStart && x.SendDate < nextMonthStart);

            ViewBag.OutgoingCount = await _context.Messages.CountAsync(x =>
                x.SenderEmail == userMail &&
                !x.IsTrash &&
                x.SendDate >= monthStart && x.SendDate < nextMonthStart);

            ViewBag.UnreadCount = await _context.Messages.CountAsync(x =>
                x.ReceiverEmail == userMail &&
                !x.IsTrash &&
                !x.IsRead);

            ViewBag.UnreadInfo = ((int)(ViewBag.UnreadCount ?? 0)) > 0 ? "Needs attention" : "All clear";

            // -----------------------------
            // TOP CONTACT (THIS MONTH)
            // -----------------------------
            var topContact = await _context.Messages
                .Where(x =>
                    x.ReceiverEmail == userMail &&
                    !x.IsTrash &&
                    x.SendDate >= monthStart && x.SendDate < nextMonthStart)
                .GroupBy(x => x.SenderEmail)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            ViewBag.TopContact = string.IsNullOrWhiteSpace(topContact) ? "No contacts yet" : topContact;

            // -----------------------------
            // TOP CATEGORIES (ALWAYS 4 ROWS, THIS MONTH) ✅ ONLY ONCE
            // -----------------------------
            var rawCategoryCounts = await _context.Messages
                .Where(x =>
                    x.ReceiverEmail == userMail &&
                    !x.IsTrash &&
                    x.CategoryId != null &&
                    x.SendDate >= monthStart && x.SendDate < nextMonthStart)
                .GroupBy(x => x.CategoryId!.Value)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToListAsync();

            var total = rawCategoryCounts.Sum(x => x.Count);

            // DB category map
            var categoryMap = await _context.Categories
                .ToDictionaryAsync(c => c.CategoryId, c => c.CategoryName);

            // Always 4 rows (fill missing with 0), pick top 4 by Count
            var top4 = categoryMap
                .Select(c => new
                {
                    Name = c.Value,
                    Count = rawCategoryCounts.FirstOrDefault(x => x.CategoryId == c.Key)?.Count ?? 0
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Name)
                .Take(4)
                .Select(x => new
                {
                    Name = x.Name,
                    Percentage = total == 0 ? 0 : (int)Math.Round((double)x.Count / total * 100)
                })
                .ToList();

            ViewBag.TopCategories = top4;

            // TopCategory label (from THIS MONTH data)
            var topCatName = top4.FirstOrDefault()?.Name;
            ViewBag.TopCategory = string.IsNullOrWhiteSpace(topCatName) ? "General" : topCatName;

            // -----------------------------
            // TRAFFIC (DAILY / WEEKLY / MONTHLY)
            // -----------------------------
            var twelveMonthsAgo = now.AddMonths(-12).Date;

            var trafficMessages = await _context.Messages
                .Where(x =>
                    !x.IsTrash &&
                    x.SendDate >= twelveMonthsAgo &&
                    (x.ReceiverEmail == userMail || x.SenderEmail == userMail))
                .Select(x => new
                {
                    x.SendDate,
                    x.ReceiverEmail,
                    x.SenderEmail
                })
                .ToListAsync();

            bool IsReceived(string receiver) =>
                string.Equals(receiver, userMail, StringComparison.OrdinalIgnoreCase);

            bool IsSent(string sender) =>
                string.Equals(sender, userMail, StringComparison.OrdinalIgnoreCase);

            // DAILY (LAST 30 DAYS)
            var dayPoints = Enumerable.Range(0, 30)
                .Select(i => now.Date.AddDays(-29 + i))
                .ToList();

            var dailyLabels = dayPoints
                .Select(d => d.ToString("dd MMM", CultureInfo.InvariantCulture))
                .ToList();

            var dailyReceived = dayPoints
                .Select(d => trafficMessages.Count(m => IsReceived(m.ReceiverEmail) && m.SendDate.Date == d))
                .ToList();

            var dailySent = dayPoints
                .Select(d => trafficMessages.Count(m => IsSent(m.SenderEmail) && m.SendDate.Date == d))
                .ToList();

            // WEEKLY (LAST 12 WEEKS)
            static DateTime StartOfWeekMonday(DateTime dt)
            {
                int diff = (7 + (int)dt.DayOfWeek - (int)DayOfWeek.Monday) % 7;
                return dt.Date.AddDays(-diff);
            }

            var weekStarts = Enumerable.Range(0, 12)
                .Select(i => StartOfWeekMonday(now.Date).AddDays(-7 * (11 - i)))
                .ToList();

            var weeklyLabels = weekStarts
                .Select(w => w.ToString("dd MMM", CultureInfo.InvariantCulture))
                .ToList();

            var weeklyReceived = weekStarts
                .Select(ws => trafficMessages.Count(m =>
                    IsReceived(m.ReceiverEmail) &&
                    m.SendDate.Date >= ws && m.SendDate.Date < ws.AddDays(7)))
                .ToList();

            var weeklySent = weekStarts
                .Select(ws => trafficMessages.Count(m =>
                    IsSent(m.SenderEmail) &&
                    m.SendDate.Date >= ws && m.SendDate.Date < ws.AddDays(7)))
                .ToList();

            // MONTHLY (LAST 12 MONTHS)
            var monthStarts = Enumerable.Range(0, 12)
                .Select(i => new DateTime(now.Year, now.Month, 1).AddMonths(-(11 - i)))
                .ToList();

            var monthlyLabels = monthStarts
                .Select(ms => ms.ToString("MMM yyyy", CultureInfo.InvariantCulture))
                .ToList();

            var monthlyReceived = monthStarts
                .Select(ms => trafficMessages.Count(m =>
                    IsReceived(m.ReceiverEmail) &&
                    m.SendDate >= ms && m.SendDate < ms.AddMonths(1)))
                .ToList();

            var monthlySent = monthStarts
                .Select(ms => trafficMessages.Count(m =>
                    IsSent(m.SenderEmail) &&
                    m.SendDate >= ms && m.SendDate < ms.AddMonths(1)))
                .ToList();

            ViewBag.TrafficDaily = JsonSerializer.Serialize(new { labels = dailyLabels, received = dailyReceived, sent = dailySent });
            ViewBag.TrafficWeekly = JsonSerializer.Serialize(new { labels = weeklyLabels, received = weeklyReceived, sent = weeklySent });
            ViewBag.TrafficMonthly = JsonSerializer.Serialize(new { labels = monthlyLabels, received = monthlyReceived, sent = monthlySent });

            return View(user);
        }



    }
}
