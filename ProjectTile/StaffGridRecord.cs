using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectTile
{
    public class StaffGridRecord
    {
        public int ID {get; set; }
        public string UserID {get; set; }
        public string StaffName {get; set; }
        public string RoleDescription {get; set; }
        public DateTime? StartDate {get; set; }
        public DateTime? LeaveDate {get; set; }
        public bool ActiveUser {get; set; }
        public string DefaultEntity {get; set; }        
    }
}
