using Email_Project.Context;
using Email_Project.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;


namespace Email_Project.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;

        public MessageController(UserManager<AppUser> userManager, EmailContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        private async Task GetSidebarCounts()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var userMail = user.Email;

            var users = await _userManager.FindByNameAsync(User.Identity.Name);
            ViewBag.UserFullName = users.Name + " " + users.Surname;
          
            ViewBag.UnreadMessages = await _context.Messages
                .CountAsync(x => x.ReceiverEmail == userMail && x.IsRead == false && x.IsStatus == true && x.IsTrash == false);

           
            ViewBag.ImportantMessages = await _context.Messages
                .CountAsync(x => x.IsImportant == true && x.IsStatus == true && x.IsTrash == false && (x.ReceiverEmail == userMail || x.SenderEmail == userMail));

           
            ViewBag.TrashCount = await _context.Messages
                .CountAsync(x => x.IsTrash == true && (x.ReceiverEmail == userMail || x.SenderEmail == userMail));

            ViewBag.ArchiveCount = await _context.Messages
                .CountAsync(x => x.IsStatus == false && x.IsTrash == false && (x.ReceiverEmail == userMail || x.SenderEmail == userMail));

            
            ViewBag.EducationCount = await _context.Messages.CountAsync(x => x.ReceiverEmail == userMail && x.CategoryId == 1 && x.IsTrash == false);
            ViewBag.SocialCount = await _context.Messages.CountAsync(x => x.ReceiverEmail == userMail && x.CategoryId == 2 && x.IsTrash == false);
            ViewBag.PromotionCount = await _context.Messages.CountAsync(x => x.ReceiverEmail == userMail && x.CategoryId == 3 && x.IsTrash == false);
            ViewBag.FinanceCount = await _context.Messages.CountAsync(x => x.ReceiverEmail == userMail && x.CategoryId == 4 && x.IsTrash == false);
        }
        public async Task<IActionResult> Index() 
        {
            
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var userMail = user.Email;

            var messages = await _context.Messages
        .Where(x => x.ReceiverEmail == userMail && x.IsStatus == true && x.IsTrash == false)
        .OrderBy(x => x.IsRead)
        .ThenByDescending(x => x.SendDate)
        .ToListAsync();

            ViewBag.UnreadMessages = messages.Count(x => !x.IsRead);
            ViewBag.TotalMessages = messages.Count();
            ViewBag.ImportantMessages = messages.Count(x => x.IsImportant);
            ViewBag.FinanceMessages = messages.Count(x => x.CategoryId == 2);

            return View(messages);
        }

        public async Task<IActionResult> SentMessages()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var userMail = user.Email;

            
            var values = await _context.Messages
                .Where(x => x.SenderEmail == userMail && x.IsStatus == true)
                .OrderByDescending(x => x.SendDate)
                .ToListAsync();

           
            ViewBag.TotalCount = values.Count;

            return View("Index", values); 
        }

        [HttpGet]
        public IActionResult CreateMessage()
        {
            ViewBag.Receiver = TempData["ReplyReceiver"];
            ViewBag.Subject = TempData["ReplySubject"];
            ViewBag.Content = TempData["ReplyContent"];
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage(Message message)
        {
            
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user != null) message.SenderEmail = user.Email;

            message.SendDate = DateTime.Now;
            message.IsStatus = true;
            message.IsRead = false;
            message.IsImportant = false;
            message.IsTrash = false;

            
            if (!string.IsNullOrEmpty(message.MessageDetail))
            {
                
                string cleanedText = System.Text.RegularExpressions.Regex.Replace(message.MessageDetail, "<.*?>|&nbsp;", " ");

               
                message.MessageDetail = cleanedText.Trim();
            }

            
            var categories = _context.Categories.ToList();
            if (categories.Any())
            {
                message.CategoryId = categories.First().CategoryId; 

                
                string searchContent = ((message.Subject ?? "") + " " + (message.MessageDetail ?? "")).ToLower();

                foreach (var cat in categories)
                {
                    if (!string.IsNullOrEmpty(cat.Keywords))
                    {
                        var keywordList = cat.Keywords.Split(',');
                        if (keywordList.Any(k => searchContent.Contains(k.Trim().ToLower())))
                        {
                            message.CategoryId = cat.CategoryId;
                            break;
                        }
                    }
                }
            }

           
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return RedirectToAction("SentMessages", "Message");
        }

        public async Task<IActionResult> MessageDetail(int id)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var userMail = user.Email;

            var value = await _context.Messages
                                       .Include(x => x.Category)
                                       .FirstOrDefaultAsync(x => x.MessageId == id);

            if (value == null)
            {
                return RedirectToAction("Index");
            }

    
            value.IsRead = true;
            _context.Update(value);
            await _context.SaveChangesAsync();

            ViewBag.TotalCount = await _context.Messages.CountAsync(x => x.ReceiverEmail == userMail && x.IsStatus == true);
            ViewBag.UnreadCount = await _context.Messages.CountAsync(x => x.ReceiverEmail == userMail && x.IsRead == false);
            ViewBag.ImportantCount = await _context.Messages.CountAsync(x => x.ReceiverEmail == userMail && x.IsImportant == true);
            ViewData["ShowReplyButtons"] = "true";
            ViewData["SenderMail"] = value.SenderEmail;
            ViewData["Subject"] = value.Subject;
            ViewData["OldMessage"] = value.MessageDetail;
            
            TempData["ReplyReceiver"] = value.SenderEmail;
            TempData["ReplySubject"] = "Re: " + value.Subject;
            TempData["ReplyContent"] = value.MessageDetail;
            TempData.Keep();
            return View(value);
        }


        public async Task<IActionResult> Starred()
        {
            
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var userMail = user.Email;

            var values = await _context.Messages
        .Where(x => x.IsImportant == true && x.IsStatus == true && (x.ReceiverEmail == userMail || x.SenderEmail == userMail))
        .OrderByDescending(x => x.SendDate)
        .ToListAsync();

            
            ViewBag.TotalCount = values.Count;


            return View("Index", values);
        }
        [HttpPost]
        public async Task<JsonResult> ToggleStar(int id)
        {
            
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                return Json(new { success = false, message = "User authentication failed." });
            }
            var userMail = user.Email;

           
            var message = await _context.Messages
                .FirstOrDefaultAsync(x => (x.ReceiverEmail == userMail || x.SenderEmail == userMail)
                                   && x.MessageId == id && x.IsStatus == true);

            if (message != null)
            {
               
                message.IsImportant = !message.IsImportant;

                _context.Update(message);
                await _context.SaveChangesAsync();

                return Json(new { success = true, isImportant = message.IsImportant });
            }

            
            return Json(new { success = false, message = "Unauthorized action or message not found." });
        }

        
        public async Task<IActionResult> Archive()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var values = await _context.Messages
                .Where(x => x.IsStatus == false && x.IsTrash == false && (x.ReceiverEmail == user.Email || x.SenderEmail == user.Email))
                .OrderByDescending(x => x.SendDate)
                .ToListAsync();

            ViewBag.TotalCount = values.Count;
            return View("Index", values);
        }

       
        [HttpPost]
        public async Task<JsonResult> BulkMoveToArchive(List<int> ids) 
        {
            if (ids == null || !ids.Any())
                return Json(new { success = false, message = "No messages selected." });

            var messages = await _context.Messages.Where(x => ids.Contains(x.MessageId)).ToListAsync();

            foreach (var msg in messages)
            {
                msg.IsStatus = false;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<JsonResult> BulkRestoreFromArchive(List<int> ids)
        {
            if (ids == null || !ids.Any())
                return Json(new { success = false, message = "Any Message Didin't Choose!" });

            var messages = await _context.Messages.Where(x => ids.Contains(x.MessageId)).ToListAsync();

            foreach (var msg in messages)
            {
                msg.IsStatus = true; 
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        
        public async Task<IActionResult> Trash()
        {
            
            var trashMessages = await _context.Messages
                .Where(x => x.IsTrash == true)
                .OrderByDescending(x => x.SendDate)
                .ToListAsync();

            return View(trashMessages);
        }

        
        [HttpPost]
        public async Task<JsonResult> RestoreFromTrash(List<int> ids)
        {
            if (ids == null || !ids.Any()) return Json(new { success = false });

            var messages = await _context.Messages.Where(x => ids.Contains(x.MessageId)).ToListAsync();
            foreach (var msg in messages)
            {
                msg.IsTrash = false;
                msg.IsStatus = true; 
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        
        [HttpPost]
        public async Task<JsonResult> PermanentDelete(List<int> ids)
        {
            if (ids == null || !ids.Any()) return Json(new { success = false });

            var messages = await _context.Messages.Where(x => ids.Contains(x.MessageId)).ToListAsync();
            _context.Messages.RemoveRange(messages);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        [HttpPost]
        public async Task<JsonResult> MoveToTrash(List<int> ids)
        {
            if (ids == null || !ids.Any()) return Json(new { success = false, message = "Hiç mesaj seçilmedi." });

            var messages = await _context.Messages.Where(x => ids.Contains(x.MessageId)).ToListAsync();
            foreach (var msg in messages)
            {
                msg.IsTrash = true; 
                                   
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }


        [HttpPost]
        public async Task<JsonResult> MarkAsRead(List<int> ids)
        {
            if (ids == null || !ids.Any())
                return Json(new { success = false, message = "Mesaj seçilmedi." });

            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var userMail = user.Email;

           
            var messages = await _context.Messages
                .Where(x => ids.Contains(x.MessageId) && x.ReceiverEmail == userMail)
                .ToListAsync();

            if (messages.Any())
            {
                foreach (var msg in messages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Mesajlar bulunamadı." });
        }

        [HttpPost]
        public async Task<JsonResult> MarkAsUnread(List<int> ids)
        {
            if (ids == null || !ids.Any())
                return Json(new { success = false });

            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var userMail = user.Email;

            var messages = await _context.Messages
                .Where(x => ids.Contains(x.MessageId) && x.ReceiverEmail == userMail)
                .ToListAsync();

            foreach (var msg in messages)
            {
                msg.IsRead = false;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        [Route("Message/Category/{id}")]
        public async Task<IActionResult> Category(string id) 
        {
            await GetSidebarCounts(); 

            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var userMail = user.Email;

          
            var messages = await _context.Messages
                .Include(x => x.Category)
                .Where(x => x.ReceiverEmail == userMail &&
                            x.Category.CategoryName == id &&
                            x.IsStatus == true &&
                            x.IsTrash == false)
                .OrderByDescending(x => x.SendDate)
                .ToListAsync();

            ViewBag.CategoryName = id; 

            
            return View("Index", messages);
        }
        public async Task<IActionResult> Search(string q)
        {
            await GetSidebarCounts();
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            var messages = await _context.Messages
                .Where(x => x.ReceiverEmail == user.Email &&
                           (x.Subject.Contains(q) || x.MessageDetail.Contains(q))) 
                .OrderByDescending(x => x.SendDate)
                .ToListAsync();

            return View("Index", messages); 
        }
    }
}