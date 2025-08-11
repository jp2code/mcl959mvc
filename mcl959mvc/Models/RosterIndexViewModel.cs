namespace mcl959mvc.Models;

public class RosterIndexViewModel
{
    private List<Roster> _allMembers;
    public RosterIndexViewModel()
    {
        AllMembers = new List<Roster>();
        PagedRoster = new List<Roster>();
        Officers = new List<OfficerModel>();
    }
    public List<Roster> AllMembers
    {
        get { return _allMembers; }
        set {
            _allMembers = value;
            if (LivingMembers != null)
            {
                LivingMembers.Clear();
            }
            LivingMembers = new List<Roster>(_allMembers.Where(m => m.DiedOn == null).OrderBy(m => m.LastName).ThenBy(m => m.FirstName).ThenBy(m => m.MemberNumber));
            if (DeceasedMembers != null)
            {
                DeceasedMembers.Clear();
            }
            DeceasedMembers = new List<Roster>(_allMembers.Where(m => m.DiedOn != null).OrderBy(m => m.DiedOn));
        }
    }
    public List<Roster> LivingMembers { get; private set; } // Sorted list for display
    public List<Roster> DeceasedMembers { get; private set; } // Sorted list for display
    public List<Roster> PagedRoster { get; set; } // Only paged for admin table
    public List<OfficerModel> Officers { get; set; }
    public string Oath { get; private set; } = @"
I, <i>[insert your name]</i>, in the presence of Almighty God, and the members of the Marine Corps League here assembled,
being fully aware of the symbols, motto, principles and purposes of the Marine Corps League,
do solemnly swear or affirm that I will uphold and defend the Constitution and Laws of the United States of America and of the Marine Corps League.
<br/><br/>
I will never knowingly wrong, deceive or defraud the League to the value of anything.
<br/><br/>
I will never knowingly wrong or injure or permit any member or any member's family to be wronged or injured if to prevent the same is within my power.
<br/><br/>
I will never propose for membership one known to me to be unqualified or unworthy to become a member of the League.
<br/><br/>
I further promise to govern my conduct in the League's affairs and in my personal life in a manner becoming a decent and honorable person
and will never knowingly bring discredit to the League, so help me God.
<br/><br/>";
}
