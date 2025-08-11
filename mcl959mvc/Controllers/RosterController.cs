using mcl959mvc.Data;
using mcl959mvc.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mcl959mvc.Controllers;

public class RosterController : Mcl959MemberController
{
    private readonly Mcl959DbContext _context;

    public RosterController(Mcl959DbContext context, UserManager<ApplicationUser> userManager)
        : base(userManager)
    {
        _context = context;
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
                    DisplayName = $"{member.FirstName} {member.LastName}",
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
        var member = _context.Roster.FirstOrDefault(x => x.MemberNumber == memberNumber);
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
            return RedirectToAction(nameof(Index));
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

}