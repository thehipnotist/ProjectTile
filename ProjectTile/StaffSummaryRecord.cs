using System;

namespace ProjectTile
{
    public class StaffSummaryRecord : Globals
    {
        public int ID { get; set; }
        public string EmployeeID { get; set; }
        public string UserID { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public string RoleCode  { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? LeaveDate { get; set; }
        public bool Active { get; set; }
        public int DefaultEntity  { get; set; }

        public string StaffName
        {
            get { return FirstName + " " + Surname; }
        }

        public string RoleDescription 
        {
            get { return StaffFunctions.GetRoleDescription(RoleCode); }             
        }

        public string DefaultEntityName
        {
            get { return EntityFunctions.GetEntityName(DefaultEntity); }
        }
    }
}
