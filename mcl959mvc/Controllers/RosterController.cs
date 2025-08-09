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

    // GET: Roster
    //public async Task<IActionResult> Index()
    //{
    //    return View(await _context.Roster.ToListAsync());
    //}
    public async Task<IActionResult> Index(string sortOrder, int page = 1)
    {
        await CheckUserIdentity();
        int pageSize = 20;
        var query = _context.Roster.AsQueryable();

        // Sorting
        ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
        switch (sortOrder)
        {
            case "name_desc":
                query = query.OrderByDescending(r => r.Name);
                break;
            default:
                query = query.OrderBy(r => r.Name);
                break;
        }

        // Paging
        int totalCount = await query.CountAsync();
        var pagedList = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewData["CurrentSort"] = sortOrder;
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewData["Oath"] = @"
I, <i>[insert your name]</i>, in the presence of Almighty God, and the members of the Marine Corps League here assembled,<br/>
being fully aware of the symbols, motto, principles and purposes of the Marine Corps League,<br/>
do solemnly swear or affirm that I will uphold and defend the Constitution and Laws of the United States of America and of the Marine Corps League.<br/>
<br/>
I will never knowingly wrong, deceive or defraud the League to the value of anything.<br/>
<br/>
I will never knowingly wrong or injure or permit any member or any member's family to be wronged or injured if to prevent the same is within my power.<br/>
<br/>
I will never propose for membership one known to me to be unqualified or unworthy to become a member of the League.<br/>
<br/>
I further promise to govern my conduct in the League's affairs and in my personal life in a manner becoming a decent and honorable person<br/>
and will never knowingly bring discredit to the League, so help me God.<br/>
<br/>";

        return View(pagedList);
    }
    // GET: Roster/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var member = await _context.Roster.FindAsync(id);
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