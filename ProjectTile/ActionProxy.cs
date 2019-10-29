using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ProjectTile
{
    public class ActionProxy: Globals, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;        

        private int statusNumber;
        private string statusDescription;
        private CombinedTeamMember owner;
        private DateTime? targetCompletion;
        private string shortDescription;
        private ProjectStages linkedStage;
        private string notes;
        private DateTime? updatedDate;
        private string actionCode = "";

        public bool Created = false;
        public bool Updated = false;
        
        public int ID { get; set; }
        public CombinedTeamMember Owner
        {
            get { return owner; }
            set 
            { 
                owner = value;
                handleUpdate("Owner");
            } 
        }
        public string ActionCode 
        {
            get { return actionCode; }
            set 
            { 
                actionCode = value;
                OnPropertyChanged("ActionCode");
            } 
        }
        public Projects Project { get; set; }
        public DateTime LoggedDate { get; set; }
        public DateTime? TargetCompletion 
        {
            get { return targetCompletion; }
            set
            {
                targetCompletion = value;
                handleUpdate("TargetCompletion");
            } 
        }
        public DateTime? UpdatedDate 
        {
            get { return updatedDate; } 
            set
            {
                updatedDate = value;
                OnPropertyChanged("UpdatedDate");
            }
        }
        public string ShortDescription 
        {
            get { return shortDescription; }
            set 
            { 
                shortDescription = value;
                handleUpdate("ShortDescription");
            } 
        }        
        public TeamProxy LoggedBy { get; set; }
        public int StatusNumber
        {
            get { return statusNumber; }
            set 
            {
                statusNumber = value;
                statusDescription = ProjectFunctions.GetCompletedDescription(value);
                handleUpdate("StatusNumber");
            } 
        }
        public string StatusDescription
        {
            get { return statusDescription; }
            set
            {
                statusDescription = value;
                statusNumber = ProjectFunctions.GetCompletedKey(value);
                handleUpdate("StatusDescription");
            }
        }
        public string Notes 
        {
            get { return notes; } 
            set
            {
                notes = value;
                handleUpdate("Notes");
            }
        }
        public ProjectStages LinkedStage
        {
            get { return linkedStage; }
            set
            {
                linkedStage = value;
                handleUpdate("LinkedStage");
                OnPropertyChanged("EffectiveDue"); // Show updated effective date
            }
        }
        public DateTime? EffectiveDue
        {
            get
            {
                if (targetCompletion != null) { return targetCompletion; }
                else if (LinkedStage != null) { return ProjectFunctions.EffectiveStageEndDate(Project.ID, LinkedStage.StageNumber); }
                else { return null; }                
            }
            set 
            { 
                if (!Globals.LoadingActions) { TargetCompletion = value; }
                OnPropertyChanged("EffectiveDue");
            }
        }

        private void handleUpdate(string propertyName)
        {
            try
            {
                if (Globals.LoadingActions) { return; }
                if (actionCode == "")
                {
                    Created = true;
                    int projectID = Globals.SelectedProjectProxy.ProjectID;
                    Project = ProjectFunctions.GetProject(projectID);
                    OnPropertyChanged("Project");
                    ActionCode = ProjectFunctions.ActionCode(projectID, Globals.Today);
                    LoggedBy = ProjectFunctions.LoggedByList.FirstOrDefault(it => it.StaffMember.ID == MyStaffID);
                    OnPropertyChanged("LoggedBy");
                    StatusDescription = "Not Started";                 
                    LoggedDate = Globals.Today;
                    OnPropertyChanged("LoggedDate");
                    ProjectFunctions.ActionsChanged();
                }
                else if (!Created && !Updated)
                {
                    Updated = true;
                    if (LoggedDate != Globals.Today) { UpdatedDate = Globals.Today; }
                    ProjectFunctions.ActionsChanged();
                }
                OnPropertyChanged(propertyName);                
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing action update", generalException); }	
        }

        protected void OnPropertyChanged(string eventName)
        {
            try
            {
                PropertyChangedEventHandler thisHandler = PropertyChanged;
                if (thisHandler != null)
                {
                    thisHandler(this, new PropertyChangedEventArgs(eventName));
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error handling changed property", generalException); }
        }

    } // class
} // namespace
