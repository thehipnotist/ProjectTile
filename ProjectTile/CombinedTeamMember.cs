using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectTile
{
    public class CombinedTeamMember
    {
        public int ProjectID { get; set; }
        public TeamProxy InternalTeamMember { get; set; }
        public ProjectContactProxy ClientTeamMember { get; set; }

        public string FullName 
        {
            get 
            {
                if (InternalTeamMember != null && InternalTeamMember.StaffMember != null) { return InternalTeamMember.StaffMember.StaffName; }
                else if (ClientTeamMember != null && ClientTeamMember.Contact != null) { return ClientTeamMember.Contact.ContactName; }
                else { return ""; }
            }
        }

    } // class
} // namespace
