using mcl959mvc.Classes;
using mcl959mvc.Data;
using mcl959mvc.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;

namespace mcl959mvc.Controllers;

public class RosterController : Mcl959MemberController
{
    private readonly Mcl959DbContext _context;
    private readonly SmtpSettings _smtpSettings;

    public RosterController(Mcl959DbContext context, UserManager<ApplicationUser> userManager, IOptions<SmtpSettings> smptOptions)
        : base(userManager)
    {
        _context = context;
        _smtpSettings = smptOptions.Value ?? throw new ArgumentNullException(nameof(smptOptions));
    }

    public async Task<IActionResult> Index()
    {
        await CheckUserIdentity();
        var allMembers = await _context.Roster.ToListAsync();
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
    public async Task<IActionResult> Details(string memberNumber)
    {
        if (string.IsNullOrEmpty(memberNumber)) return NotFound();
        var member = await _context.Roster.FirstOrDefaultAsync(x => x.MemberNumber == memberNumber);
        if (member == null) return NotFound();
        return View(member);
    }

    // GET: Roster/Create
    public async Task<IActionResult> Create()
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        return View();
    }

    // POST: Roster/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Roster member)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        if (ModelState.IsValid)
        {
            _context.Add(member);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(member);
    }

    // GET: Roster/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        if (id == null) return NotFound();
        var member = await _context.Roster.FindAsync(id);
        if (member == null) return NotFound();
        return View(member);
    }

    // POST: Roster/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Roster member)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        if (id != member.Id) return NotFound();
        if (ModelState.IsValid)
        {
            _context.Update(member);
            await _context.SaveChangesAsync();
            // Redirect to Details with the same id after saving
            return RedirectToAction(nameof(Details), new { id = member.Id });
        }
        return View(member);
    }

    // GET: Roster/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        if (id == null) return NotFound();
        var member = await _context.Roster.FindAsync(id);
        if (member == null) return NotFound();
        return View(member);
    }

    // POST: Roster/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await CheckUserIdentity();
        if (!IsAdmin) return Forbid();
        var member = await _context.Roster.FindAsync(id);
        if (member != null)
        {
            _context.Roster.Remove(member);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Memorial(int? id)
    {
        if (id == null) return NotFound();

        var member = await _context.Roster.FindAsync(id);
        if (member == null || member.DiedOn == null) return NotFound();
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
            .Where(c => c.TableSource == "Memorial" && c.ParentId == memorial.Id)
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
            DiedOn = (DateTime)member.DiedOn
        };
        return View(viewModel);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(CommentsModel item)
    {
        await CheckUserIdentity();
        if (!IsRegistered) return Forbid();

        item.TimeStamp = DateTime.UtcNow;
        item.TableSource = "Memorial";
        _context.Comments.Add(item);
        await _context.SaveChangesAsync();
        var regarding = $"{UserEmail}";
        var roster = await _context.Roster.FindAsync(item.ParentId);
        if (roster != null)
        {
            regarding = $"{roster.DisplayName} ({roster.MemberNumber})";
        }
        var emailMessage = $@"
The following comment was added to the memorial for {regarding}:
<blockquote>{item.Message}</blockquote>";
        await EmailTool.SendEmailAsync(
            _smtpSettings,
            item.UserId, UserEmail, string.Empty,
            $"Comment on Memorial for {regarding}",
            emailMessage);
        // Redirect back to the memorial page
        return RedirectToAction("Memorial", "Roster");
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id)
    {
        await CheckUserIdentity();
        if (!IsRegistered) return Forbid();
        var comment = await _context.Comments.FindAsync(id);
        if (comment == null) return NotFound();
        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
        // Redirect back to the memorial page
        return RedirectToAction("Memorial", new { id = comment.ParentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMemorialDescription(int id, string description, bool save)
    {
        var memorial = await _context.Memorial.FindAsync(id);
        if (memorial == null) return NotFound();
        if (save)
        {
            memorial.Description = description;
            // Optionally, convert Editor.js JSON to HTML for display
            // memorial.DescriptionHtml = Tools.JsonToHtml(description);
            await _context.SaveChangesAsync();
            var regarding = $"{memorial.RosterId}";
            var roster = await _context.Roster.FindAsync(memorial.RosterId);
            if (roster != null)
            {
                regarding = $"{roster.DisplayName} ({roster.MemberNumber})";
            }
            var emailMessage = $@"
The following comment was added to the memorial for {regarding}:
<blockquote>{description}</blockquote>";
            await EmailTool.SendEmailAsync(
                _smtpSettings,
                UserEmail, UserEmail, string.Empty,
                $"Comment on Memorial for {regarding}",
                emailMessage);
        }
        return RedirectToAction("Memorial", new { id = memorial.RosterId });
    }

}