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
    
    public partial class Actions
    {
        public int ID { get; set; }
        public string ActionCode { get; set; }
        public int ProjectID { get; set; }
        public System.DateTime LoggedDate { get; set; }
        public Nullable<System.DateTime> TargetCompletion { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
        public string ShortDescription { get; set; }
        public Nullable<bool> StatusCode { get; set; }
        public Nullable<int> LoggedBy { get; set; }
        public Nullable<int> InternalOwner { get; set; }
        public Nullable<int> ClientOwner { get; set; }
        public string Notes { get; set; }
        public Nullable<int> StageID { get; set; }
    
        public virtual ProjectTeams ProjectTeams { get; set; }
        public virtual Projects Projects { get; set; }
    }
}
