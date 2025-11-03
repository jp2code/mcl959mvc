using mcl959mvc.Classes;
using mcl959mvc.Data;
using mcl959mvc.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace mcl959mvc.Controllers;

public class RosterController : Mcl959MemberController
{
    private readonly Mcl959DbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public RosterController(
        Mcl959DbContext context,
        UserManager<ApplicationUser> userManager,
        IOptions<SmtpSettings> smptOptions,
        ILogger<Controller> logger,
        IWebHostEnvironment webHostEnvironment)
        : base(userManager, logger, smptOptions)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<IActionResult> Index()
    {
        await CheckUserIdentity();
        var allMembers = await _context.Roster
            .ToListAsync();
        var pagedRoster = allMembers
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ThenBy(m => m.MemberNumber)
            .ToList();
        var officePositions = await _context.MemberRanks.OrderByDescending(r => r.NumericRank).ToListAsync();
        var officers = new List<OfficerModel>();

        foreach (var rank in officePositions)
        {
            var member = allMembers.FirstOrDefault(m => m.MemberNumber == rank.MemberNumber);
            if (member != null)
            {
                var phone = "private";
                var email = "private";
                if (member.WebsiteDisplay == 1)
                {
                    if (!string.IsNullOrEmpty(member.PersonalPhone)) {
                        phone = member.PersonalPhone;
                    }
                    if (!string.IsNullOrEmpty(member.PersonalEmail))
                    {
                        email = member.PersonalEmail;
                    }
                }
                else if (member.WebsiteDisplay == 2)
                {
                    if (!string.IsNullOrEmpty(member.WorkPhone))
                    {
                        phone = member.WorkPhone;
                    }
                    if (!string.IsNullOrEmpty(member.WorkEmail))
                    {
                        email = member.WorkEmail;
                    }
                }
                officers.Add(new OfficerModel
                {
                    Position = rank.DisplayRank,
                    DisplayName = $"{member.DisplayName}",
                    MemberNumber = member.MemberNumber,
                    Phone = phone,
                    Email = email
                });
            }
        }
        var viewModel = new RosterIndexViewModel
        {
            AllMembers = allMembers,
            PagedRoster = pagedRoster,
            Officers = officers,
        };
        return View(viewModel);
    }
    // GET: Roster/Details/225510
    public async Task<IActionResult> Details(string memberNumber, int? id)
    {
        if (string.IsNullOrEmpty(memberNumber) && (id == null)) return NotFound("Member Number or ID not provided.");
        var member = await _context.Roster.FirstOrDefaultAsync(x => x.MemberNumber == memberNumber || x.Id == id);
        if (member == null) return NotFound($"Member with ID {id} not found.");
        member.HasPhoto = HasPhoto(member);
        return View(member);
    }

    // POST: Roster/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Roster")] Roster member)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        // Set Name before validation
        member.Name = member.GetFullName();
        member.CreatedDate = DateTime.Now;
        ModelState.Clear();
        TryValidateModel(member);
        if (ModelState.IsValid)
        {
            _context.Add(member);
            member.HasPhoto = HasPhoto(member);
            await _context.SaveChangesAsync();
            await SendEmailAsync(UserEmail, UserEmail, string.Empty,
                $"Roster Member Created: {member.Name} ({member.MemberNumber})",
                $"The member '{member.Name}' (ID: {member.Id}) was CREATED by {UserEmail}.");
            if (IsAjaxRequest(Request))
            {
                return Json(new { success = true }); // let the client close the modal & reload
            }
            return RedirectToAction(nameof(Index));
        } else
        {
            ViewBag.PopupType = PopupType.Details;
            ViewBag.IsAdmin = IsAdmin;
            return PartialView("_RosterPopup", member);
        }
    }

    // POST: Roster/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(Prefix = "Roster")] Roster member)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        if (id != member.Id)
        {
            ModelState.AddModelError(string.Empty, "Mismatched roster id.");
            ViewBag.PopupType = PopupType.Edit;
            ViewBag.IsAdmin = IsAdmin;
            return PartialView("_RosterPopup", member);
        }
        // Fetch the existing entity
        var existingMember = await _context.Roster.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        if (existingMember == null) return NotFound($"Member with ID {id} not found.");

        // Preserve CreatedDate
        member.CreatedDate = existingMember.CreatedDate;

        // Set Name before validation
        member.Name = member.GetFullName();
        if (member.MemberNumber == null)
        {
            member.MemberNumber = "";
        }
        ModelState.Clear();
        TryValidateModel(member);
        if (ModelState.IsValid)
        {
            _context.Update(member);
            await _context.SaveChangesAsync();
            await SendEmailAsync(UserEmail, UserEmail, string.Empty,
                $"Roster Member Edited: {member.Name} ({member.MemberNumber})",
                $"The member '{member.Name}' (ID: {member.Id}) was EDITED by {UserEmail}.");
            if (IsAjaxRequest(Request))
            {
                return Json(new { success = true }); // let the client close the modal & reload
            }
            return RedirectToAction(nameof(Index));
        } else
        {
            ViewBag.PopupType = PopupType.Edit;
            ViewBag.IsAdmin = IsAdmin;
            return PartialView("_RosterPopup", member);
        }
    }

    // POST: Roster/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        var member = await _context.Roster.FindAsync(id);
        if (member == null)
        {
            ModelState.AddModelError(string.Empty, "Roster member not found.");
            ViewBag.PopupType = PopupType.Details;
            ViewBag.IsAdmin = IsAdmin;
            return PartialView("_RosterPopup", member);
        }
        _context.Roster.Remove(member);
        await _context.SaveChangesAsync();
        await SendEmailAsync(UserEmail, UserEmail, string.Empty,
            $"Roster Member Deleted: {member.Name} ({member.MemberNumber})",
            $"The member '{member.Name}' (ID: {member.Id}) was DELETED by {UserEmail}.");
        if (IsAjaxRequest(Request))
        {
            return Json(new { success = true }); // let the client close the modal & reload
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Memorial(int? id)
    {
        if (id == null) return NotFound("Member ID is required.");

        var member = await _context.Roster.FindAsync(id);
        if (member == null || member.DiedOn == null) return NotFound($"Deceased member with ID {id} not found.");
        // Find or create the memorial record
        var memorial = await _context.Memorial
            .FirstOrDefaultAsync(m => m.RosterId == member.Id);

        if (memorial == null)
        {
            memorial = new MemorialModel { RosterId = member.Id, TimeStamp = DateTime.UtcNow };
            _context.Memorial.Add(memorial);
            await _context.SaveChangesAsync();
        }
        // Get comments for this memorial
        var comments = await _context.Comments
            .Where(c => c.TableSource == "Memorial" && c.ParentId == member.Id)
            .ToListAsync();

        if (string.IsNullOrEmpty(memorial.Description))
        {
            memorial.Description = $@"
We do not have any memorial information on file for {member.DisplayName}.
Please add your fond memories in the comments.

If you are the immediate family or have an obituary from the funeral home,
please contact us so that the web sergeant can update this page.";
        }
        var viewModel = new MemorialViewModel
        {
            Memorial = memorial,
            Comments = comments,
            DisplayName = $"{member.DisplayName}",
            DiedOn = (DateTime)member.DiedOn,
            HasPhoto = HasPhoto(member)
        };
        return View(viewModel);
    }

    private async Task<MemorialViewModel?> BuildMemorialVmAsync(int rosterId)
    {
        var member = await _context.Roster.FindAsync(rosterId);
        if (member == null || member.DiedOn == null) return null;

        var memorial = await _context.Memorial.FirstOrDefaultAsync(m => m.RosterId == member.Id);
        if (memorial == null)
        {
            memorial = new MemorialModel { RosterId = member.Id, TimeStamp = DateTime.UtcNow };
            _context.Memorial.Add(memorial);
            await _context.SaveChangesAsync();
        }

        var comments = await _context.Comments
            .Where(c => c.TableSource == "Memorial" && c.ParentId == member.Id)
            .OrderByDescending(c => c.TimeStamp)
            .ToListAsync();

        if (string.IsNullOrEmpty(memorial.Description))
        {
            memorial.Description = $@"
We do not have any memorial information on file for {member.DisplayName}.
Please add your fond memories in the comments.

If you are the immediate family or have an obituary from the funeral home,
please contact us so that the web sergeant can update this page.";
        }

        member.HasPhoto = HasPhoto(member);
        return new MemorialViewModel
        {
            Memorial = memorial,
            Comments = comments,
            DisplayName = $"{member.DisplayName}",
            DiedOn = (DateTime)member.DiedOn,
            HasPhoto = member.HasPhoto
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(CommentsModel item)
    {
        await CheckUserIdentity();
        if (!IsRegistered) return Forbid();

        // Optional: guard against accidental double-posts within a short window
        var recentDuplicate = await _context.Comments.AnyAsync(c =>
            c.TableSource == "Memorial" &&
            c.ParentId == item.ParentId &&
            c.UserId == item.UserId &&
            c.Message == item.Message &&
            EF.Functions.DateDiffSecond(c.TimeStamp, DateTime.UtcNow) < 5);
        if (!recentDuplicate)
        {
            item.TimeStamp = DateTime.UtcNow;
            _context.Comments.Add(item);
            await _context.SaveChangesAsync();
            await SendEmailAsync(UserEmail, UserEmail, string.Empty,
                $"Comment Added",
                $"The member '{UserEmail}' added the following comment to {item.ParentId}: {item.Message}.");
            if (IsAjaxRequest(Request))
            {
                var vm = await BuildMemorialVmAsync(item.ParentId);
                return PartialView("_MemorialPopup", vm);
            }
        }
        return RedirectToAction(nameof(Popup), new { popupType = PopupType.Memorial, id = item.ParentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id, int parentId)
    {
        await CheckUserIdentity();
        if (!IsRegistered) return Forbid();

        var comment = await _context.Comments.FindAsync(id);
        if (comment != null)
        {
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
        }

        if (IsAjaxRequest(Request))
        {
            var vm = await BuildMemorialVmAsync(parentId);
            if (vm == null) return NotFound($"Member with ID {parentId} not found.");
            return PartialView("_MemorialPopup", vm);
        }
        return RedirectToAction(nameof(Popup), new { popupType = PopupType.Memorial, id = parentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMemorialDescription(int id, string description, bool save)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();

        var memorial = await _context.Memorial.FindAsync(id);
        if (memorial == null) return NotFound($"Member with ID {id} not found.");

        if (save)
        {
            memorial.Description = description;
            await _context.SaveChangesAsync();
            var regarding = $"{memorial.RosterId}";
            var roster = await _context.Roster.FindAsync(memorial.RosterId);
            if (roster != null)
            {
                regarding = $"{roster.DisplayName} ({roster.MemberNumber})";
            }
            var emailMessage = $@"
The memorial for {regarding} was edited as follows:
<blockquote>{description}</blockquote>";
            await SendEmailAsync(UserEmail, UserEmail, string.Empty,
                $"Memorial Edited: {regarding}",
                emailMessage);
        }

        if (IsAjaxRequest(Request))
        {
            var vm = await BuildMemorialVmAsync(memorial.RosterId);
            if (vm == null) return NotFound($"Member with ID {memorial.RosterId} not found.");
            return PartialView("_MemorialPopup", vm);
        }
        return RedirectToAction(nameof(Popup), new { popupType = PopupType.Memorial, id = memorial.RosterId });
    }

    public async Task<IActionResult> SaveMemorial([Bind(Prefix = "MemorialVM.Memorial")] MemorialModel memorial) {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        var item = await _context.Memorial.FindAsync(memorial.Id);
        if (item == null) return NotFound($"Member with ID {memorial.Id} not found.");
        item.Description = memorial.Description;
        await _context.SaveChangesAsync();
        var regarding = $"{memorial.RosterId}";
        var roster = await _context.Roster.FindAsync(memorial.RosterId);
        if (roster != null)
        {
            regarding = $"{roster.DisplayName} ({roster.MemberNumber})";
        }
        var emailMessage = $@"
The memorial for {regarding} was saved as follows:
<blockquote>{item.Description}</blockquote>";
        await SendEmailAsync(UserEmail, UserEmail, string.Empty,
            $"Memorial Saved: {regarding}",
            emailMessage);
        return RedirectToAction(nameof(Index));
    }

    private bool HasPhoto(Roster member)
    {
        var result = false;
        if (member != null)
        {
            var photoFile = Path.Combine(_webHostEnvironment.WebRootPath, "photos", $"{member.Id}.jpg");
            result = System.IO.File.Exists(photoFile);
        }
        return result;
    }

    public async Task<IActionResult> Popup(PopupType popupType, int? id)
    {
        if (!IsAdmin)
        {
            if ((popupType != PopupType.Details) && (popupType != PopupType.Memorial))
            {
                return Forbid();
            }
        }
        var member = new Roster();
        ViewBag.PopupType = popupType;
        ViewBag.IsAdmin = IsAdmin;

        switch (popupType)
        {
            case PopupType.Memorial:
                if (id == null) return BadRequest("id is required.");
                member = await _context.Roster.FindAsync(id);
                if (member == null || member.DiedOn == null) return NotFound($"Deceased member with ID {id} not found.");
                var memorial = await _context.Memorial
                    .FirstOrDefaultAsync(m => m.RosterId == member.Id);
                if (memorial == null)
                {
                    memorial = new MemorialModel { RosterId = member.Id, TimeStamp = DateTime.UtcNow };
                    _context.Memorial.Add(memorial);
                    await _context.SaveChangesAsync();
                }
                var comments = await _context.Comments
                    .Where(c => c.TableSource == "Memorial" && c.ParentId == member.Id)
                    .OrderByDescending(c => c.TimeStamp)
                    .ToListAsync();
                if (string.IsNullOrEmpty(memorial.Description))
                {
                    memorial.Description = $@"
We do not have any memorial information on file for {member.DisplayName}.
Please add your fond memories in the comments.

If you are the immediate family or have an obituary from the funeral home,
please contact us so that the web sergeant can update this page.";
                }
                member.HasPhoto = HasPhoto(member);
                var model = new MemorialViewModel
                {
                    Memorial = memorial,
                    Comments = comments,
                    DisplayName = $"{member.DisplayName}",
                    DiedOn = (DateTime)member.DiedOn,
                    HasPhoto = member.HasPhoto
                };
                return PartialView("_MemorialPopup", model);
            case PopupType.Create:
                ViewBag.Mode = "Create";
                break;
            case PopupType.Edit:
            case PopupType.Delete:
            case PopupType.Details:
                if (id == null) return BadRequest("id is required.");
                member = await _context.Roster.FindAsync(id);
                if (member == null) return NotFound($"Member with ID {id} not found.");
                member.HasPhoto = HasPhoto(member);
                ViewBag.Mode = (popupType == PopupType.Edit) ? "Edit" :
                              (popupType == PopupType.Delete) ? "Delete" : "Details";
                break;
        }

        return PartialView("_RosterPopup", member);
    }
}