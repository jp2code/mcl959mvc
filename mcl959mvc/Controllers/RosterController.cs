using mcl959mvc.Classes;
using mcl959mvc.Controllers.Filters;
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

    private string? FindPhotoPath(int id)
    {
        var photosFolder = Path.Combine(_webHostEnvironment.WebRootPath, "photos");
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        foreach (var item in allowed)
        {
            var file = Path.Combine(photosFolder, $"{id}{item}");
            if (System.IO.File.Exists(file))
            {
                return $"/photos/{id}{item}";
            }
        }
        return null;
    }

    [HttpGet]
    [Route("Roster/{id:int?}", Name = "MemberById")]
    [Route("Roster")]
    [Route("Roster/Index")]
    public async Task<IActionResult> Index(int? id)
    {
        var allMembers = await _context.Roster
            .ToListAsync();
        foreach (var member in allMembers)
        {
            var photoPath = FindPhotoPath(member.Id);
            member.HasPhoto = !String.IsNullOrEmpty(photoPath);
        }
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
                    if (!string.IsNullOrEmpty(member.PersonalPhone))
                    {
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

        if (id.HasValue && allMembers.Any(m => m.Id == id.Value))
        {
            // There is not anything for Member Details, only Memorial Details
            SetAutoOpenPopup("Roster", "Memorial", id.Value);
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
        if (string.IsNullOrEmpty(memberNumber) && (id == null))
        {
            return NotFound("Member Number or ID not provided.");
        }

        var member = await _context.Roster.FirstOrDefaultAsync(x => x.MemberNumber == memberNumber || x.Id == id);
        if (member == null)
        {
            return NotFound($"Member with ID {id} not found.");
        }

        member.HasPhoto = HasPhoto(member);
        return View(member);
    }

    // POST: Roster/Create
    [HttpPost, ValidateAntiForgeryToken, RequireAdmin]
    public async Task<IActionResult> Create(Roster member)
    {
        // Compute derived values
        member.Name = member.GetFullName();
        if (string.IsNullOrEmpty(member.DisplayName))
        {
            member.DisplayName = $"{member.FirstName} {member.LastName}";
            ModelState.Remove(nameof(Roster.DisplayName));
        }
        member.CreatedDate = DateTime.Now;

        // Remove only the fields we changed so their validation will be re-run
        ModelState.Remove(nameof(Roster.Name));
        ModelState.Remove(nameof(Roster.CreatedDate));

        // Re-validate the model (only necessary because we changed values server-side)
        if (!TryValidateModel(member))
        {
            var errors = ModelState
                .Where(kvp => kvp.Value.Errors.Count > 0)
                .Select(kvp => new { Key = kvp.Key, Errors = kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray() })
                .ToList();
            _logger.LogInformation("Validation failed: {@errors}", errors);
            ViewBag.PopupType = PopupType.Create;
            ViewBag.Mode = "Create";
            ViewBag.IsAdmin = IsAdmin;
            ViewBag.PhotoPath = "/photos/mcl959bw.jpg"; // default photo for new members
            return PartialView("_RosterPopup", member);
        }

        _context.Add(member);
        member.HasPhoto = HasPhoto(member);
        await _context.SaveChangesAsync();
        await SendEmailAsync(UserEmail,
            $"Roster Member Created: {member.Name} ({member.MemberNumber})",
            $"The member '{member.Name}' (ID: {member.Id}) was CREATED by {UserEmail}.", false);
        if (IsAjaxRequest(Request))
        {
            return Json(new { success = true });
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: Roster/Edit/5
    [HttpPost, ValidateAntiForgeryToken, RequireAdmin]
    public async Task<IActionResult> Edit(int id, Roster member)
    {
        if (id != member.Id)
        {
            ModelState.AddModelError(string.Empty, "Mismatched roster id.");
            ViewBag.PopupType = PopupType.Edit;
            ViewBag.PhotoPath = "/photos/mcl959bw.jpg";
            return PartialView("_RosterPopup", member);
        }

        var existingMember = await _context.Roster.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        if (existingMember == null)
        {
            return NotFound($"Member with ID {id} not found.");
        }

        member.CreatedDate = existingMember.CreatedDate ?? DateTime.UtcNow;
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
            await SendEmailAsync(UserEmail,
                $"Roster Member Edited: {member.Name} ({member.MemberNumber})",
                $"The member '{member.Name}' (ID: {member.Id}) was EDITED by {UserEmail}.", false);
            if (IsAjaxRequest(Request))
            {
                return Json(new { success = true });
            }
            return RedirectToAction(nameof(Index));
        }
        var errors = ModelState
            .Where(kvp => kvp.Value.Errors.Count > 0)
            .Select(kvp => new { Key = kvp.Key, Errors = kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray() })
            .ToList();
        _logger.LogInformation("Validation failed: {@errors}", errors);
        ViewBag.PopupType = PopupType.Edit;
        ViewBag.Mode = "Edit";
        ViewBag.IsAdmin = IsAdmin;
        // Set the photo path for this specific member
        ViewBag.PhotoPath = FindPhotoPath(member.Id) ?? "/photos/mcl959bw.jpg";
        return PartialView("_RosterPopup", member);
    }

    // POST: Roster/Delete/5
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, RequireAdmin]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
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
        await SendEmailAsync(UserEmail,
            $"Roster Member Deleted: {member.Name} ({member.MemberNumber})",
            $"The member '{member.Name}' (ID: {member.Id}) was DELETED by {UserEmail}.", false);
        if (IsAjaxRequest(Request))
        {
            return Json(new { success = true }); // let the client close the modal & reload
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<MemorialViewModel?> BuildMemorialVmAsync(int rosterId)
    {
        var member = await _context.Roster.FindAsync(rosterId);
        if (member == null || member.DiedOn == null)
        {
            return null;
        }

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

        // member.HasPhoto = HasPhoto(member);
        return new MemorialViewModel
        {
            Memorial = memorial,
            Comments = comments,
            DisplayName = $"{member.DisplayName}",
            DiedOn = (DateTime)member.DiedOn,
            HasPhoto = member.HasPhoto
        };
    }

    [HttpPost, ValidateAntiForgeryToken, RequireRegistered]
    public async Task<IActionResult> AddComment(CommentsModel item)
    {
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
            var memorialUrl = $"{Url.Action("Index", "Roster", new { id = item.ParentId }, Request.Scheme)}";
            var memberName = item.Id.ToString();
            var member = await _context.Roster.FindAsync(item.ParentId);
            if (member != null)
            {
                memberName = $"<a href=\"{memorialUrl}\">{member.DisplayName} ({member.MemberNumber})</a>";
            }

            var emailMessage = $@"
The member '{UserEmail}' added the following comment to the memorial for {memberName}:

<blockquote>{item.Message}</blockquote>

Visit {memorialUrl} for details.
";
            await SendEmailAsync(UserEmail,
                $"MCL959 Memorial Comment Added",
                emailMessage, false);
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
        if (!IsAdmin)
        {
            return ForbidAjax();
        }

        var comment = await _context.Comments.FindAsync(id);
        if (comment != null)
        {
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
        }

        if (IsAjaxRequest(Request))
        {
            var vm = await BuildMemorialVmAsync(parentId);
            if (vm == null)
            {
                return NotFound($"Member with ID {parentId} not found.");
            }
            return PartialView("_MemorialPopup", vm);
    }
        return RedirectToAction(nameof(Popup), new { popupType = PopupType.Memorial, id = parentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMemorialDescription(int id, string description, bool save)
    {
        if (!IsAdmin)
        {
            return ForbidAjax();
        }

        var memorial = await _context.Memorial.FindAsync(id);
        if (memorial == null)
        {
            return NotFound($"Member with ID {id} not found.");
        }

        if (save)
        {
            memorial.Description = description;
            await _context.SaveChangesAsync();
            var memorialUrl = $"{Url.Action("Index", "Roster", new { id = memorial.RosterId }, Request.Scheme)}";
            var memberName = $"{id}";
            var member = await _context.Roster.FindAsync(memorial.RosterId);
            if (member != null)
            {
                memberName = $"<a href=\"{memorialUrl}\">{member.DisplayName} ({member.MemberNumber})</a>";
            }
            await SendEmailAsync(UserEmail,
                $"MCL959 Memorial Edited",
                $@"
The member '{UserEmail}' edited the memorial for {memberName} as follows:

<blockquote>{description}</blockquote>

Visit {memorialUrl} for details.
", false);
        }

        if (IsAjaxRequest(Request))
        {
            var vm = await BuildMemorialVmAsync(memorial.RosterId);
            if (vm == null)
            {
                return NotFound($"Member with ID {memorial.RosterId} not found.");
            }
            return PartialView("_MemorialPopup", vm);
        }

        return RedirectToAction(nameof(Popup), new { popupType = PopupType.Memorial, id = memorial.RosterId });
    }

    public async Task<IActionResult> SaveMemorial([Bind(Prefix = "MemorialVM.Memorial")] MemorialModel memorial)
    {
        if (!IsAdmin)
        {
            return ForbidAjax();
        }

        var item = await _context.Memorial.FindAsync(memorial.Id);
        if (item == null)
        {
            return NotFound($"Member with ID {memorial.Id} not found.");
        }

        item.Description = memorial.Description;
        await _context.SaveChangesAsync();
        var memorialUrl = $"{Url.Action("Index", "Roster", new { id = memorial.RosterId }, Request.Scheme)}";
        var memberName = $"{memorial.Id}";
        var roster = await _context.Roster.FindAsync(memorial.RosterId);
        if (roster != null)
        {
            memberName = $"<a href=\"{memorialUrl}\">{roster.DisplayName} ({roster.MemberNumber})</a>";
        }

        var emailMessage = $@"
The member '{UserEmail}' saved a memorial for {memberName} as follows:

<blockquote>{item.Description}</blockquote>

Visit {memorialUrl} for details.";
        await SendEmailAsync(UserEmail,
            $"MCL959 Memorial Saved",
            emailMessage, false);
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
        if (!IsAdmin && popupType is not PopupType.Details and not PopupType.Memorial)
        {
            return ForbidAjax();
        }

        var member = new Roster();
        ViewBag.PopupType = popupType;

        switch (popupType)
        {
            case PopupType.Memorial:
            {
                if (id == null)
                {
                    return BadRequest("Member ID is required.");
                }

                member = await _context.Roster.FindAsync(id);
                if (member == null || member.DiedOn == null)
                {
                    return NotFound($"Deceased member with ID {id} not found.");
                }

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
                // Set photo path for this specific member shown in memorial popup
                ViewBag.PhotoPath = FindPhotoPath(member.Id) ?? "/photos/mcl959bw.jpg";
                return PartialView("_MemorialPopup", model);
            }

            case PopupType.Create:
            {
                ViewBag.Mode = "Create";
                ViewBag.PhotoPath = "/photos/mcl959bw.jpg";
                break;
            }

            case PopupType.Edit:
            case PopupType.Delete:
            case PopupType.Details:
            {
                if (id == null)
                {
                    return BadRequest("id is required.");
                }

                member = await _context.Roster.FindAsync(id);
                if (member == null)
                {
                    return NotFound($"Member with ID {id} not found.");
                }

                member.HasPhoto = HasPhoto(member);
                // Set photo path for this specific member shown in roster popup
                ViewBag.PhotoPath = FindPhotoPath(member.Id) ?? "/photos/mcl959bw.jpg";
                ViewBag.Mode = (popupType == PopupType.Edit) ? "Edit" :
                               (popupType == PopupType.Delete) ? "Delete" : "Details";
                break;
            }
        }

        return PartialView("_RosterPopup", member);
    }
}