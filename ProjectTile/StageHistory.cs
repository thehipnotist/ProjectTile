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
    
    public partial class StageHistory
    {
        public int ID { get; set; }
        public int ProjectID { get; set; }
        public int StageID { get; set; }
        public Nullable<System.DateTime> TargetStart { get; set; }
        public Nullable<System.DateTime> ActualStart { get; set; }
        public Nullable<System.DateTime> EffectiveStart { get; set; }
    
        public virtual Projects Projects { get; set; }
        public virtual ProjectStages ProjectStages { get; set; }
    }
}
