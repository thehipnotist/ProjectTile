using System;

namespace ProjectTile
{
    public class TeamSummaryRecord : Globals
    {
        public int ID { get; set; }
        public Projects Project { get; set; }
        public StaffSummaryRecord StaffMember { get; set; }
        public ProjectRoles ProjectRole { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public ProjectStages ProjectStage
        {
            get { return ProjectFunctions.GetStageByCode(Project.StageCode); }
        }

        public DateTime? EffectiveFrom
        {
            get { return FromDate ?? Project.StartDate; }
        }

        public DateTime EffectiveTo // Used for easy comparison
        {
            get { return ToDate ?? InfiniteDate; }
        }

        public bool IsHistoric
        {
            get { return (EffectiveTo < Today); }
        }

        public int StaffID
        {
            get { return (StaffMember == null) ? 0 : StaffMember.ID; }
        }

        public int ClientID
        {
            get 
            {
                if (Project == null) { return 0; }
                else { return Project.ClientID ?? 0; }
            }
        }

        public string RoleCode
        {
            get { return (ProjectRole == null) ? "" : ProjectRole.RoleCode; }
        }

        public string StaffRoleCode
        {
            get { return (StaffMember == null) ? "" : StaffMember.RoleCode;  }
        }

        public bool HasKeyRole
        {
            get
            {
                return (Array.IndexOf(Globals.KeyRoles, RoleCode) >= 0);
            }
        }

        public DateTime? SuggestedStart()
        {
            try
            {
                ProjectTeams predecessor = ProjectFunctions.GetPredecessor(this) ?? null;
                if (predecessor == null) { return (DateTime)Project.StartDate; }
                else if (predecessor.ToDate != null)
                {
                    DateTime toDate = (DateTime)predecessor.ToDate;
                    return toDate.AddDays(1);
                }
                else { return Today; }
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error suggesting start date", generalException);
                return Today;
            }
        }

        public string DefaultRole()
        {
            if (StaffRoleCode == AccountManagerCode && StaffID == ClientFunctions.GetAccountManagerID(ClientID)) { return SponsorCode; }
            else if (StaffRoleCode == TechnicalManagerCode) { return TechnicalLeadCode; }
            else if (ProjectFunctions.GetRole(StaffRoleCode) != null) { return StaffRoleCode; }
            else { return ""; }
        }

        public TeamSummaryRecord ShallowCopy()
        {
            return (TeamSummaryRecord) this.MemberwiseClone();
        }

        public bool ValidateTeamRecord(TeamSummaryRecord savedVersion)
        {
            if (savedVersion == null) { savedVersion = new TeamSummaryRecord(); } // Prevents having to check for null each time
            
            string errorMessage = "";
            bool nameChanged = (savedVersion.StaffID != StaffID);
            bool roleChanged = (savedVersion.RoleCode != RoleCode);

            if (StaffMember == null) { errorMessage = "Please choose a staff member from the list, or use the 'Search' function.|No Staff Member"; }
            else if (ProjectRole == null) { errorMessage = "Please select a project role for the staff member.|No Role Selected"; }            
            else if (FromDate == null) { errorMessage = "Please enter a date from which this user is (or was) part of the team.|No Start Date"; }
            else if (EffectiveTo < FromDate) { errorMessage = "The 'To' date cannot be after the 'From' date.|Invalid Dates"; }
            else if (savedVersion.IsHistoric && (nameChanged || roleChanged)) { errorMessage = "Past team members cannot be amended, except to change their dates.|Historic Record"; }
            else if (!StaffMember.Active && FromDate <= Today && EffectiveTo > Today) { errorMessage = "The selected staff member is inactive, so cannot be part of the current project team.|Inactive User"; }
            
            if (errorMessage != "")
            {
                MessageFunctions.SplitInvalid(errorMessage);
                return false;
            }

            // Query: 
            //  Duplication of 'one only' roles - need to set this, ideally in the database
            //  Silly dates - before project start, after project end, or ones that just don't fit
            //  Gaps in essential roles - will need to update the other records
            //  Re-use of the same project team member
            //  Changes some time after the start date
            //  Project roles that don't seem to fit the user's role

            return true;
        }



    } // class
} // namespace
