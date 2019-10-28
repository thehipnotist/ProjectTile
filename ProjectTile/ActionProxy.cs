using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ProjectTile
{
    public class ActionProxy: Globals //, INotifyPropertyChanged
    {
        //public event PropertyChangedEventHandler PropertyChanged;        

        private int statusNumber;
        private string statusDescription;        
        
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
            get { return statusNumber; }
            set 
            {
                statusNumber = value;
                statusDescription = ProjectFunctions.GetCompletedDescription(value); 
            } 
        }

        public string StatusDescription
        {
            get { return statusDescription; }
            set 
            {
                statusDescription = value;
                statusNumber = ProjectFunctions.GetCompletedKey(value); 
            }
        }
        public string Notes { get; set; }
        public ProjectStages LinkedStage
        {
            get;
            set;
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

        //public string LoggedByName
        //{
        //    get { return LoggedBy.StaffName; }
        //    set { // Not worked this out yet! 
        //    }
        //}

    } // class
} // namespace
