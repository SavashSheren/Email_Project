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

           
            ViewBag.IncomingCount = await _context.Messages.CountAsync(x =>
                x.ReceiverEmail == userMail &&
                x.SendDate >= monthStart && x.SendDate < nextMonthStart);

            ViewBag.OutgoingCount = await _context.Messages.CountAsync(x =>
                x.SenderEmail == userMail &&
                x.SendDate >= monthStart && x.SendDate < nextMonthStart);

            ViewBag.UnreadCount = await _context.Messages.CountAsync(x =>
                x.ReceiverEmail == userMail && !x.IsRead && !x.IsTrash);

            var topContact = await _context.Messages
                .Where(x => x.ReceiverEmail == userMail &&
                            x.SendDate >= monthStart && x.SendDate < nextMonthStart &&
                            !x.IsTrash)
                .GroupBy(x => x.SenderEmail)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            ViewBag.TopContact = string.IsNullOrWhiteSpace(topContact) ? "No contacts yet" : topContact;

            var topCatId = await _context.Messages
                .Where(x => x.ReceiverEmail == userMail &&
                            x.SendDate >= monthStart && x.SendDate < nextMonthStart &&
                            x.CategoryId != null &&
                            !x.IsTrash)
                .GroupBy(x => x.CategoryId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            ViewBag.TopCategory = (topCatId ?? 0) switch
            {
                1 => "Education",
                2 => "Social",
                3 => "Promotion",
                4 => "Finance",
                _ => "General"
            };

            ViewBag.UnreadInfo = (ViewBag.UnreadCount ?? 0) > 0 ? "Needs attention" : "All clear";

            var twelveMonthsAgo = now.AddMonths(-12).Date;

            var messages = await _context.Messages
                .Where(x => !x.IsTrash &&
                            x.SendDate >= twelveMonthsAgo &&
                            (x.ReceiverEmail == userMail || x.SenderEmail == userMail))
                .Select(x => new { x.SendDate, x.ReceiverEmail, x.SenderEmail })
                .ToListAsync();

            bool IsReceived(dynamic m) => string.Equals((string)m.ReceiverEmail, userMail, StringComparison.OrdinalIgnoreCase);
            bool IsSent(dynamic m) => string.Equals((string)m.SenderEmail, userMail, StringComparison.OrdinalIgnoreCase);

          
            var dayLabels = Enumerable.Range(0, 30)
                .Select(i => now.Date.AddDays(-29 + i))
                .ToList();

            var dailyLabels = dayLabels.Select(d => d.ToString("dd MMM", CultureInfo.InvariantCulture)).ToList();
            var dailyReceived = dayLabels.Select(d => messages.Count(m => IsReceived(m) && ((DateTime)m.SendDate).Date == d)).ToList();
            var dailySent = dayLabels.Select(d => messages.Count(m => IsSent(m) && ((DateTime)m.SendDate).Date == d)).ToList();

           
            DateTime StartOfWeek(DateTime dt)
            {
            
                int diff = (7 + (int)dt.DayOfWeek - (int)DayOfWeek.Monday) % 7;
                return dt.Date.AddDays(-diff);
            }

            var weekStarts = Enumerable.Range(0, 12)
                .Select(i => StartOfWeek(now.Date).AddDays(-7 * (11 - i)))
                .ToList();

            var weeklyLabels = weekStarts.Select(w => w.ToString("dd MMM", CultureInfo.InvariantCulture)).ToList();
            var weeklyReceived = weekStarts.Select(ws =>
                messages.Count(m => IsReceived(m) &&
                    ((DateTime)m.SendDate).Date >= ws && ((DateTime)m.SendDate).Date < ws.AddDays(7)))
                .ToList();

            var weeklySent = weekStarts.Select(ws =>
                messages.Count(m => IsSent(m) &&
                    ((DateTime)m.SendDate).Date >= ws && ((DateTime)m.SendDate).Date < ws.AddDays(7)))
                .ToList();

            
            var monthStarts = Enumerable.Range(0, 12)
                .Select(i => new DateTime(now.Year, now.Month, 1).AddMonths(-(11 - i)))
                .ToList();

            var monthlyLabels = monthStarts.Select(ms => ms.ToString("MMM yyyy", CultureInfo.InvariantCulture)).ToList();
            var monthlyReceived = monthStarts.Select(ms =>
                messages.Count(m => IsReceived(m) &&
                    ((DateTime)m.SendDate) >= ms && ((DateTime)m.SendDate) < ms.AddMonths(1)))
                .ToList();

            var monthlySent = monthStarts.Select(ms =>
                messages.Count(m => IsSent(m) &&
                    ((DateTime)m.SendDate) >= ms && ((DateTime)m.SendDate) < ms.AddMonths(1)))
                .ToList();

            
            ViewBag.TrafficDaily = JsonSerializer.Serialize(new { labels = dailyLabels, received = dailyReceived, sent = dailySent });
            ViewBag.TrafficWeekly = JsonSerializer.Serialize(new { labels = weeklyLabels, received = weeklyReceived, sent = weeklySent });
            ViewBag.TrafficMonthly = JsonSerializer.Serialize(new { labels = monthlyLabels, received = monthlyReceived, sent = monthlySent });

            return View(user);
        }    

    }
}
