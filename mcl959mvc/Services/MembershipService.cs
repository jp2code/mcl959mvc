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
        ArgumentNullException.ThrowIfNull(user);
        var normalizedEmail = user.NormalizedEmail;
        var roster = await _mcl959Context.Roster
            .FirstOrDefaultAsync(r =>
                r.PersonalEmail != null && r.PersonalEmail.ToUpper() == normalizedEmail ||
                r.WorkEmail != null && r.WorkEmail.ToUpper() == normalizedEmail
            );
        if (roster != null)
        {
            user.IsMember = true;
            if (!roster.Authenticated)
            {
                roster.Authenticated = true;
                _mcl959Context.Roster.Update(roster);
                await _mcl959Context.SaveChangesAsync();
            }
            var rank = await _mcl959Context.MemberRanks
                .FirstOrDefaultAsync(r => r.MemberNumber == roster.MemberNumber);
            if (rank != null)
            {
                user.IsAdmin = rank.DisplayRank.In([
                    "Commandant",
                    "Paymaster",
                    "Web Sergeant",
                ]);
            }
        }
        return roster;
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
