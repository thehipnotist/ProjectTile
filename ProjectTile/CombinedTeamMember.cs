using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectTile
{
    public class CombinedTeamMember
    {
        public Projects Project { get; set; }
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

        //public int ClientID
        //{
        //    get { return (Project != null) ? (Project.ClientID ?? 0) : 0; }
        //}

        //public string ClientCode
        //{
        //    get { return (ClientID > 0)? ClientFunctions.GetClientByID(ClientID).ClientCode : ""; }
        //}

        //public string NameAndClient
        //{
        //    get
        //    {
        //        if (ClientTeamMember != null && ClientID > 0) { return FullName + " (" + ClientCode + " )"; }
        //        else { return FullName; }
        //    }
        //}

    } // class
} // namespace
