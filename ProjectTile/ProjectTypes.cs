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
    
    public partial class ProjectTypes
    {
        public ProjectTypes()
        {
            this.Projects = new HashSet<Projects>();
        }
    
        public string TypeCode { get; set; }
        public string TypeName { get; set; }
        public string TypeDescription { get; set; }
    
        public virtual ICollection<Projects> Projects { get; set; }
    }
}
