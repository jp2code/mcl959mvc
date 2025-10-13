using System.Text.RegularExpressions;

namespace mcl959mvc.Services;

public record FaqEntry(string Id, string Question, string Answer, string[] Keywords);

public interface IFaqService
{
    FaqEntry? FindBest(string? userQuestion);
    IEnumerable<FaqEntry> All { get; }
}

public class FaqService : IFaqService
{
    private readonly List<FaqEntry> _faqs = new()
    {
        new("membership_requirements",
            "Who can join?",
            "Honorably discharged or active duty Marines (and qualifying FMF Corpsmen) may apply. Annual dues are currently $40. Click on the Members page to get more details.",
            new[]{ "join","membership","apply","dues","requirements"}),

        new("meetings_schedule",
            "When are meetings?",
            "Meetings are held at the building on the 4th Tuesday of each month (except December for Toys for Tots). See the Events page for our next meeting date.",
            new[]{ "meeting","schedule","when","tuesday"}),

        new("application_form",
            "Where is the membership application?",
            "Download it from the Member page or by going here: /info/Membership-Application-Form-2023.pdf",
            new[]{ "application","form","pdf","download"}),

        new("officers",
            "Who are the current officers?",
            "See the Officers section on the Members page (Roster > Officers list).",
            new[]{ "officer","leadership","commandant","web sergeant"}),

        new("account_creation",
            "How do I create an account?",
            "Use Register, confirm your email by following the link sent to your registered email address, and log in.",
            new[]{ "create","account","register","sign up","login" })
    };

    public IEnumerable<FaqEntry> All => _faqs;

    public FaqEntry? FindBest(string? userQuestion)
    {
        if (string.IsNullOrWhiteSpace(userQuestion)) return null;
        var text = userQuestion.ToLowerInvariant();
        var scores = _faqs
            .Select(f => new {
                Entry = f,
                Score = f.Keywords.Count(k => Regex.IsMatch(text, $@"\b{Regex.Escape(k)}\b"))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Entry.Question.Length)
            .ToList();
        return scores.FirstOrDefault()?.Entry;
    }
}
