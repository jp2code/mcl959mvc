using mcl959mvc.Data;
using mcl959mvc.Models;
using Microsoft.EntityFrameworkCore;

namespace mcl959mvc.Services;

public class MembershipService
{
    private static readonly string[] AdminRanks = { "Commandant", "Paymaster", "Web Sergeant" };
    private readonly Mcl959DbContext _mcl959Context;

    public MembershipService(Mcl959DbContext appContext)
    {
        _mcl959Context = appContext;
    }

    // Recomputes transient flags; nothing persisted except roster.Authenticated
    public async Task MapToRoster(ApplicationUser user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.EmailConfirmed)
        {
            var normalized = user.NormalizedEmail;
            var rosterList = await _mcl959Context.Roster
                .Where(r =>
                    (!string.IsNullOrEmpty(r.PersonalEmail) && r.PersonalEmail.ToUpper() == normalized) ||
                    (!string.IsNullOrEmpty(r.WorkEmail) && r.WorkEmail.ToUpper() == normalized))
                .ToListAsync(ct);
            foreach (var roster in rosterList)
            {
                user.IsMember = true;
                if (!roster.Authenticated)
                {
                    roster.Authenticated = true;
                    _mcl959Context.Roster.Update(roster);
                }
                var ranks = await _mcl959Context.MemberRanks
                    .Where(mr => mr.MemberNumber == roster.MemberNumber)
                    .Select(mr => mr.DisplayRank)
                    .ToListAsync(ct);
                user.IsAdmin = ranks.Any(rank => AdminRanks.Contains(rank));
                if (user.IsAdmin)
                {
                    break;
                }
            }
        }
    }

}
