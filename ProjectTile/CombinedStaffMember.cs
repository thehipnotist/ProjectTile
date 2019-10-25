using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectTile
{
    public class CombinedStaffMember
    {
        public StaffProxy StaffMember { get; set; }
        public ContactProxy ClientContact { get; set; }

        public string FullName
        {
            get
            {
                if (StaffMember != null) { return StaffMember.StaffName; }
                else if (ClientContact != null) { return ClientContact.ContactName; }
                else { return ""; }
            }
        }

        public int ClientID
        {
            get { return (ClientContact != null) ? ClientContact.ClientID : 0; }
        }

        public string ClientCode
        {
            get { return (ClientID > 0) ? ClientFunctions.GetClientByID(ClientID).ClientCode : ""; }
        }

        public string NameAndClient
        {
            get
            {
                if (ClientContact != null && ClientID > 0) { return FullName + " (" + ClientCode + " )"; }
                else { return FullName; }
            }
        }

    } // class
} // namespace
