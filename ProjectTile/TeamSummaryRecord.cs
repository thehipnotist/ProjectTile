using System;

namespace ProjectTile
{
    public class TeamSummaryRecord : Globals
    {
        public int ID { get; set; }
        public Projects Project { get; set; }
        public Staff StaffMember { get; set; }
        public ProjectRoles ProjectRole { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public ProjectStages ProjectStage
        {
            get { return ProjectFunctions.GetStageByCode(Project.StageCode); }
        }

        public string StaffName
        {
            get { return StaffMember.FirstName + " " + StaffMember.Surname; }
        }

        public DateTime? EffectiveFrom
        {
            get { return FromDate ?? Project.StartDate; }
        }


    } // class
} // namespace
