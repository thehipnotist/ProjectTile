using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ProjectTile
{
    public class ActionProxy: Globals, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        //private DateTime? nextStageTargetStart = null;
        

        public int ID { get; set; }
        public CombinedTeamMember Owner { get; set; }
        public string ActionCode { get; set; }
        public Projects Project { get; set; }
        public DateTime LoggedDate { get; set; }
        public DateTime? TargetCompletion { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string ShortDescription { get; set; }        
        public TeamProxy LoggedBy { get; set; }
        public int StatusNumber
        {
            get { return StatusNumber; }
            set
            {
                //StatusDescription = ProjectFunctions.GetCompletedDescription(value);
            } 
        }
        public string Notes { get; set; }
        public ProjectStages LinkedStage
        {
            get;
            set;
            //get { return LinkedStage; }
            //set 
            //{ 
            //    //if (Project != null && StageNumber >= 0)
            //    //{
            //    //    nextStageTargetStart = ProjectFunctions.GetHistoryDate(Project.ID, StageNumber, true); 
            //    //}
            //}
        }
        
        //public int StageNumber 
        //{
        //    get { return (LinkedStage != null) ? LinkedStage.StageNumber : -1; }
        //}
        //public DateTime? EffectiveDue 
        //{
        //    get { return targetCompletion ?? nextStageTargetStart; }
        //    set { targetCompletion = value; } 
        //}

        public DateTime? EffectiveDue
        {
            get 
            {
                if (TargetCompletion != null) { return TargetCompletion; }
                return ProjectFunctions.EffectiveStageEndDate(Project.ID, LinkedStage.StageNumber); 
            }
            set { TargetCompletion = value; }
        }

        //public string StatusDescription 
        //{
        //    get { return StatusDescription; }
        //    set { StatusNumber = ProjectFunctions.GetCompletedCode(value); } 
        //}

        //public string LoggedByName
        //{
        //    get { return LoggedBy.StaffName; }
        //    set { // Not worked this out yet! 
        //    }
        //}

    } // class
} // namespace
