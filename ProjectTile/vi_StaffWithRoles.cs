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
    
    public partial class vi_StaffWithRoles
    {
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
        public string RoleDescription { get; set; }
    }
}
