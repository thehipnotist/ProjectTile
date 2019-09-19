using System;

namespace ProjectTile
{
    public class ProjectSummaryRecord : Globals
    {
        public int ProjectID { get; set; }
        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }
        public string ProjectSummary { get; set; }
        public string TypeCode { get; set; }
        public string TypeName { get; set; }
        public string TypeDescription { get; set; }
        public int EntityID { get; set; }
        public int? ClientID { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public int PMStaffID { get; set; }
        public string PMStaffName { get; set; }
        public int StageCode { get; set; }
        public string StageName { get; set; }
        public string StageDescription { get; set; }
        public string ProjectStatus { get; set; }
        public DateTime? StartDate { get; set; }
    }
}
