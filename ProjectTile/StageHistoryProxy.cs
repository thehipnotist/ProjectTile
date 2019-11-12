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
        public bool? IsTarget
        {
            get
            {
                if (Type == TimelineType.Target || Stage.StageNumber > CurrentStageNumber) { return true; }
                else if (Type == TimelineType.Actual || Stage.StageNumber <= CurrentStageNumber) { return false; }
                else { return null; }
            }
        }
        
        public DateTime? EndDate
        {
            get 
            { 
                return (Project != null)? ProjectFunctions.StageEndDate(Project.ID, Stage.StageNumber, IsTarget) : null; 
            }
        }

        public int CurrentStageNumber
        {
            get
            {
                return (Project != null) ? ProjectFunctions.ProjectCurrentStage(Project.ID).StageNumber : 0;
            }
        }
        public string CurrentStageName
        {
            get
            {
                return (Project != null) ? ProjectFunctions.ProjectCurrentStage(Project.ID).StageName : "";
            }
        }

    }
}
