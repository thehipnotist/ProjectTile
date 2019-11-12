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

        private int completedNumber;
        private string completedDescription;
        private CombinedTeamMember owner;
        private DateTime? targetCompletion;
        private string shortDescription = "";
        private ProjectStages linkedStage;
        private string notes = "";
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
        public int CompletedNumber
        {
            get { return completedNumber; }
            set 
            {
                completedNumber = value;
                completedDescription = ProjectFunctions.GetCompletedDescription(value);
                handleUpdate("CompletedNumber");
            } 
        }
        public string CompletedDescription
        {
            get { return completedDescription; }
            set
            {
                completedDescription = value;
                completedNumber = ProjectFunctions.GetCompletedKey(value);
                handleUpdate("CompletedDescription");
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
                OnPropertyChanged("Overdue");
            }
        }
        public DateTime? EffectiveDue
        {
            get
            {
                if (targetCompletion != null) { return targetCompletion; }
                else if (LinkedStage != null && LinkedStage.StageNumber >= 0) { return ProjectFunctions.StageEndDate(Project.ID, LinkedStage.StageNumber); }
                else { return null; }                
            }
            set 
            { 
                if (!Globals.LoadingActions) { TargetCompletion = value; }
                OnPropertyChanged("EffectiveDue");
                OnPropertyChanged("Overdue");
            }
        }

        public bool Overdue
        {
            get { return (CompletedNumber != 3 && EffectiveDue != null && EffectiveDue < Today); }
        }

        public bool ClientAction
        {
            get { return (Owner != null && Owner.ClientTeamMember != null); }
        }

        private void handleUpdate(string propertyName)
        {
            try
            {
                if (Globals.LoadingActions) { return; }
                if (actionCode == "")
                {
                    Created = true;
                    OnPropertyChanged("Created");
                    int projectID = Globals.SelectedProjectProxy.ProjectID;
                    Project = ProjectFunctions.GetProject(projectID);
                    OnPropertyChanged("Project");
                    ActionCode = ProjectFunctions.ActionCode(projectID, Globals.Today);
                    LoggedBy = ProjectFunctions.LoggedByList.FirstOrDefault(it => it.StaffMember.ID == MyStaffID);
                    OnPropertyChanged("LoggedBy");
                    CompletedDescription = "Not Started";                 
                    LoggedDate = Globals.Today;
                    OnPropertyChanged("LoggedDate");
                    ProjectFunctions.ActionsChanged();

                    if (Owner == null && SelectedOwner != null) // Try to use the owner from the filter, if they are in the team
                        { 
                            if (ProjectFunctions.OwnerList.Exists(ol => ol.ClientTeamMember != null && SelectedOwner.ClientContact != null 
                            && ol.ClientTeamMember.ContactID == SelectedOwner.ClientContact.ID))
                        {
                            Owner = ProjectFunctions.OwnerList.FirstOrDefault(ol =>  ol.ClientTeamMember != null && SelectedOwner.ClientContact != null 
                                && ol.ClientTeamMember.ContactID == SelectedOwner.ClientContact.ID);
                        }
                        else if (ProjectFunctions.OwnerList.Exists(ol => ol.InternalTeamMember != null && SelectedOwner.StaffMember != null 
                            && ol.InternalTeamMember.StaffID == SelectedOwner.StaffMember.ID))
                        {
                            Owner = ProjectFunctions.OwnerList.FirstOrDefault(ol => ol.InternalTeamMember != null && SelectedOwner.StaffMember != null
                                && ol.InternalTeamMember.StaffID == SelectedOwner.StaffMember.ID);
                        }
                    }
                }
                else if (!Created && !Updated)
                {
                    Updated = true;
                    OnPropertyChanged("Updated");
                    if (LoggedDate != Globals.Today) { UpdatedDate = Globals.Today; }
                    ProjectFunctions.ActionsChanged();
                }
                OnPropertyChanged(propertyName);                
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing action update", generalException); }	
        }

        public bool Validate()
        {
            string invalidMessage = "";
            ProjectStages projectStage = ProjectFunctions.GetStageByID(Project.StageID);

            if (ActionCode == "") { invalidMessage = "Please ensure all new actions are registered by focusing on a different row or column.|Incomplete Data"; }
            else if (ShortDescription == "") { invalidMessage = "Please enter a description for action " + ActionCode + ".|Missing Description"; }
            else if (ProjectFunctions.ActionList.Exists(al => al.ActionCode != ActionCode && al.Project.ID == Project.ID && al.ShortDescription == ShortDescription))
            {
                invalidMessage = "Another action exists on this project with the same description as action " + ActionCode + ". Please provide a more specific description."
                    + "|Duplicate Description";
            }
            else if (Owner == null) { invalidMessage = "Please select an owner for action " + ActionCode + ".|Missing Owner"; }
            else if ((LinkedStage == null || LinkedStage.StageNumber == NoID) && TargetCompletion == null)
            {
                invalidMessage = "Please either link action " + ActionCode + " to a stage (meaning it is due before the end of that stage) or specify a target due date. "
                    + "Incomplete actions can only be linked to the current stage (" + projectStage.StageName + ") or later.|Missing Due Date/Stage"; 
            }
            else if (CompletedNumber < 3 && LinkedStage != null && LinkedStage.StageNumber >= 0 && LinkedStage.StageNumber < projectStage.StageNumber) 
            {
                invalidMessage = "Action " + ActionCode + " cannot be linked to the " + LinkedStage.StageName + " stage as the project is already in a later stage, "
                    + "and it is not marked as completed. Please choose the current stage (" + projectStage.StageName + ") or a later stage, if the action is not complete."
                    + "|Invalid Linked Stage"; 
            }

            if (invalidMessage != "")
            {
                MessageFunctions.SplitInvalid(invalidMessage);
                return false;
            }
            else { return true; }
        }

        public void ConvertToAction(ref Actions action)
        {
            action.ProjectID = Project.ID;
            action.ActionCode = ActionCode;
            action.InternalOwner = ClientAction ? null : (int?)Owner.InternalTeamMember.ID;
            action.ClientOwner = ClientAction ? (int?)Owner.ClientTeamMember.ID : null;
            action.LoggedBy = LoggedBy.ID;
            action.LoggedDate = LoggedDate;
            action.ShortDescription = ShortDescription;
            action.StatusCode = ProjectFunctions.GetActionStatusCode(CompletedNumber);
            action.StageID = (LinkedStage != null) ? (int?)LinkedStage.ID : null;
            action.TargetCompletion = TargetCompletion;
            action.UpdatedDate = UpdatedDate;
            action.Notes = Notes;
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
