using System;

namespace ProjectTile
{
    public class StageHistoryProxy : Globals
    {
        public int ID { get; set; }
        public Projects Project { get; set; }
        public ProjectStages Stage { get; set; }
        public TimelineType Type { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate
        {
            get 
            { 
                return (Project != null)? ProjectFunctions.EffectiveStageEndDate(Project.ID, Stage.StageNumber) : null; 
            }
        }
        public string CurrentStageName
        {
            get
            {
                return (Project != null) ? ProjectFunctions.ProjectCurrentStage(Project.ID).StageName : null;
            }
        }

    }
}
