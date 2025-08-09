using mcl959mvc.Classes;
using mcl959mvc.Data;
using mcl959mvc.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace mcl959mvc.Services;

public class MembershipService
{
    private readonly ApplicationDbContext _identityContext;
    private readonly Mcl959DbContext _mcl959Context;

    public MembershipService(ApplicationDbContext identityContext, Mcl959DbContext appContext)
    {
        _identityContext = identityContext;
        _mcl959Context = appContext;
    }

    public async Task<Roster?> FindRosterMember(ApplicationUser user)
    {
        Roster result = null;
        ArgumentNullException.ThrowIfNull(user);
        var normalizedEmail = user.NormalizedEmail;
        var rosterList = await _mcl959Context.Roster
            .Where(r =>
                (!string.IsNullOrEmpty(r.PersonalEmail) && r.PersonalEmail.ToUpper() == normalizedEmail) ||
                (!string.IsNullOrEmpty(r.WorkEmail) && r.WorkEmail.ToUpper() == normalizedEmail)
            ).ToListAsync();
        foreach (var roster in rosterList)
        {
            result = roster;
            user.IsMember = true;
            if (!roster.Authenticated)
            {
                roster.Authenticated = true;
                _mcl959Context.Roster.Update(roster);
                await _mcl959Context.SaveChangesAsync();
            }
            var ranks = await _mcl959Context.MemberRanks
                .Where(r => r.MemberNumber == roster.MemberNumber).ToListAsync();
            foreach (var rank in ranks)
            {
                user.IsAdmin = rank.DisplayRank.In([
                    "Commandant",
                    "Paymaster",
                    "Web Sergeant",
                ]);
                if (user.IsAdmin)
                {
                    break;
                }
            }
            if (user.IsAdmin)
            {
                break;
            }
        }
        return result;
    }

    public async Task UpgradeRegisteredToMemberAsync()
    {
        var confirmedUsers = await _identityContext.Users
            .Where(u => u.EmailConfirmed)
            .ToListAsync();
        foreach (var user in confirmedUsers)
        {
            var normalizedEmail = user.NormalizedEmail;
            var roster = await _mcl959Context.Roster
                .FirstOrDefaultAsync(r =>
                    r.PersonalEmail != null && r.PersonalEmail.ToUpper() == normalizedEmail ||
                    r.WorkEmail != null && r.WorkEmail.ToUpper() == normalizedEmail
                );
            if (roster != null)
            {
                user.IsMember = true;
                roster.Authenticated = true;
                _mcl959Context.Roster.Update(roster);
            }
        }
        await _mcl959Context.SaveChangesAsync();
    }
}
