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

        public DateTime? EffectiveFrom // This is two-way to allow bindings
        {
            get { return FromDate ?? Project.StartDate; }
            set { ToDate = value; }
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

        public string DefaultRole()
        {
            if (StaffRoleCode == AccountManagerCode && StaffID == ClientFunctions.GetAccountManagerID(ClientID)) { return ProjectSponsorCode; }
            else if (StaffRoleCode == TechnicalManagerCode) { return TechnicalLeadCode; }
            else if (ProjectFunctions.GetRole(StaffRoleCode) != null) { return StaffRoleCode; }
            else { return ""; }
        }

        public bool ValidateTeamRecord(TeamSummaryRecord savedVersion)
        {
            if (savedVersion == null) { savedVersion = new TeamSummaryRecord(); } // Prevents having to check for null each time
            
            string errorMessage = "";
            bool nameChanged = (savedVersion.StaffID != StaffID);
            bool roleChanged = (savedVersion.RoleCode != RoleCode);

            if (FromDate == null) { errorMessage = "Please enter a date from which this user was part of the team."; }
            else if (EffectiveTo < FromDate) { errorMessage = "The 'To' date cannot be after the 'From' date."; }
            else if (StaffMember == null) { errorMessage = "Please choose a staff member from the list, or use the 'Search' function."; }
            else if (ProjectRole == null) { errorMessage = "Please select a project role for the staff member"; }
            else if (savedVersion.IsHistoric && (nameChanged || roleChanged)) { errorMessage = "Past team members cannot be amended, except to change their dates."; }
            else if (!StaffMember.Active && FromDate <= Today && EffectiveTo > Today) { errorMessage = "The selected staff member is inactive, so cannot be part of the current project team."; }
            
            // Prevent: 
            //  Changes that leave gaps in essential roles

            // Query: 
            //  Duplication of 'one only' roles - need to set this, ideally in the database
            //  Silly dates - before project start, after project end, or ones that just don't fit
            //  Gaps in essential roles
            //  Re-use of the same project team member
            //  Changes some time after the start date
            //  Project roles that don't seem to fit the user's role

            return false;
        }



    } // class
} // namespace
