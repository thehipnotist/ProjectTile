//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ProjectTile
{
    using System;
    using System.Collections.Generic;
    
    public partial class Staff
    {
        public Staff()
        {
            this.Clients = new HashSet<Clients>();
            this.ProjectTeams = new HashSet<ProjectTeams>();
            this.StaffEntities = new HashSet<StaffEntities>();
        }
    
        public int ID { get; set; }
        public string EmployeeID { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public string RoleCode { get; set; }
        public System.DateTime StartDate { get; set; }
        public Nullable<System.DateTime> LeaveDate { get; set; }
        public string UserID { get; set; }
        public string Passwd { get; set; }
        public byte[] PasswordHash { get; set; }
        public bool Active { get; set; }
        public Nullable<int> DefaultEntity { get; set; }
        public string FullName { get; set; }
        public Nullable<int> MainProject { get; set; }
        public bool SingleSignon { get; set; }
        public string OSUser { get; set; }
    
        public virtual ICollection<Clients> Clients { get; set; }
        public virtual Entities Entities { get; set; }
        public virtual ICollection<ProjectTeams> ProjectTeams { get; set; }
        public virtual ICollection<StaffEntities> StaffEntities { get; set; }
        public virtual StaffRoles StaffRoles { get; set; }
        public virtual Projects Projects { get; set; }
    }
}
