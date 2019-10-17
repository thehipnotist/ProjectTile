using System;

namespace ProjectTile
{
    public class TeamProxy : Globals
    {
        public int ID { get; set; }
        public Projects Project { get; set; }
        public StaffProxy StaffMember { get; set; }
        public ProjectRoles ProjectRole { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public ProjectStages Stage
        {
            get { return (Project == null)? null : ProjectFunctions.GetStageByID(Project.StageID); }
        }

        public DateTime EffectiveFrom
        {
            get { return FromDate ?? Project.StartDate ?? StartOfTime; }
        }

        public DateTime EffectiveTo // Used for easy comparison
        {
            get { return ToDate ?? InfiniteDate; }
        }

        public bool IsHistoric
        {
            get { return (EffectiveTo < Today); }
        }

        public bool IsFuture
        {
            get { return (EffectiveFrom > Today); }
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
                return (Array.IndexOf(Globals.KeyInternalRoles, RoleCode) >= 0);
            }
        }

        public ProjectTeams Predecessor()
        {
            return ProjectFunctions.GetStaffPredecessor(this) ?? null; // Latest record (by 'to' date, then 'from' date) that starts earlier
        }

        public ProjectTeams Successor()
        {
            return ProjectFunctions.GetStaffSuccessor(this) ?? null; // Earliest record (by 'from' date, then 'to' date) that ends later
        }
      
        public bool IsDuplicate()
        {
            return (ProjectFunctions.DuplicateTeamMember(this, byRole: false) == true);            
        }

        public bool AlreadyOnProject()
        {
            return (ProjectFunctions.DuplicateTeamMember(this, byRole: false) != false); // Expect null if on project but not a duplicate, i.e. different role or dates
        }

        public DateTime? SuggestedStart()
        {
            try
            {
                if (Predecessor() == null) { return (DateTime)Project.StartDate; }
                else if (Predecessor().ToDate != null)
                {
                    DateTime toDate = (DateTime)Predecessor().ToDate;
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
            if (StaffRoleCode == AccountManagerCode) { return SponsorCode; } // Only passed back as a suggestion if it is the client's Account Manager
            else if (StaffRoleCode == TechnicalManagerCode) { return OurTechLeadCode; }
            else if (ProjectFunctions.GetInternalRole(StaffRoleCode) != null) { return StaffRoleCode; }
            else { return ""; }
        }
        
        public string SuggestedRole()
        {
            if (StaffRoleCode == AccountManagerCode && StaffID != ClientFunctions.GetAccountManagerID(ClientID)) { return ""; }            
            else { return DefaultRole(); }
        }

        public bool IsUnusualRole()
        {
            if (RoleCode == DefaultRole() || RoleCode == OtherRoleCode) { return false; }
            if (RoleCode == IntegrationConsultCode && StaffFunctions.GetRoleDescription(StaffRoleCode).Contains("Consultant")) { return false; }
            if (RoleCode == ApplicationConsultCode && StaffRoleCode == SeniorConsultantCode) { return false; }
            if (ProjectRole.RoleDescription.Contains("Technical") && StaffFunctions.GetRoleDescription(StaffRoleCode).Contains("Technical")) { return false; }
            return true;
        }

        public TeamProxy ShallowCopy()
        {
            return (TeamProxy) this.MemberwiseClone();
        }

        public bool RoleOverlap()
        {
            return (ProjectFunctions.DuplicateTeamMember(this, byRole: true) == true);
        }

        public bool ValidateTeamRecord(TeamProxy savedVersion)
        {
            bool staffChanged = true;
            bool roleChanged = true;
            bool startChanged = true;
            bool amendment = false;
            ProjectTeams predecessor = Predecessor();

            if (savedVersion == null) { savedVersion = new TeamProxy(); } // Prevents having to check for null each time
            else
            {
                amendment = true;
                staffChanged = (savedVersion.StaffID != StaffID);
                roleChanged = (savedVersion.RoleCode != RoleCode);
                startChanged = (savedVersion.FromDate != FromDate);
            }
            try
            {
                string errorMessage = "";
                if (StaffMember == null)
                { errorMessage = "Please choose a staff member from the list, or use the 'Search' function.|No Staff Member"; }
                else if (ProjectRole == null)
                { errorMessage = "Please select a project role for the staff member.|No Role Selected"; }
                else if (FromDate == null)
                { errorMessage = "Please enter a date from which this user is (or was) part of the team.|No Start Date"; }
                else if (EffectiveTo < FromDate)
                { errorMessage = "The 'To' date cannot be after the 'From' date.|Invalid Dates"; }
                else if (amendment && (staffChanged || roleChanged) && savedVersion.IsHistoric)
                { errorMessage = "Past team members cannot be amended, except to change their dates.|Historic Record"; }
                else if (!StaffMember.Active && FromDate <= Today && EffectiveTo > Today)
                { errorMessage = StaffMember.StaffName + " is inactive and cannot be part of the current project team now, but can be added for a future date.|Inactive User"; }
                else if (IsDuplicate())
                { errorMessage = StaffMember.StaffName + " already has the same role in the project during this period. Please check the existing record.|Duplicate Record"; }
                else if (HasKeyRole && predecessor == null && FromDate > Project.StartDate)
                {
                    errorMessage = ProjectRole.RoleDescription + " is a key role, and is not covered at the start of the project. Please adjust the 'from' date, then (if appropriate) "
                      + "add an initial " + ProjectRole.RoleDescription + " afterwards; the date of this record will then be adjusted automatically.|Key Role Not Covered";
                }
                else if (HasKeyRole && Successor() == null & ToDate != null)
                {
                    errorMessage = ProjectRole.RoleDescription + " is a key role, and must always have a current record. Please leave the 'to' date blank, then (if appropriate) "
                      + "add a subsequent " + ProjectRole.RoleDescription + " afterwards; the date of this record will then be adjusted automatically.|Key Role Not Covered";
                }
                else if (amendment && roleChanged && savedVersion.HasKeyRole)
                {
                    errorMessage = "The existing role (" + savedVersion.ProjectRole.RoleDescription + ") is a key role, and must always be filled during the project. Please ensure "
                      + "continuity in that role (e.g. by selecting an alternative staff member for this record) before changing/adding this user's new project role.|Key Role Not Covered";
                }
                if (errorMessage != "")
                {
                    MessageFunctions.SplitInvalid(errorMessage);
                    return false;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error validating project team details", generalException);
                return false;
            }

            try
            {
                MessageFunctions.ClearQuery();
                if (AlreadyOnProject())
                { MessageFunctions.AddQuery(StaffMember.StaffName + " is already a project team member in a different capacity, or at a different time."); }
                if (amendment && (staffChanged || roleChanged) && Project.StartDate < Today.AddMonths(-1))
                { MessageFunctions.AddQuery("This change is more than a month after the start of the project, which suggests an addition rather than amendment is required."); }
                if ((staffChanged || roleChanged) && IsUnusualRole())
                { MessageFunctions.AddQuery("The role of " + ProjectRole.RoleDescription + " appears unusual for this user, given their main job role."); }
                if (startChanged && FromDate < Project.StartDate)
                { MessageFunctions.AddQuery("This role starts before the project's official start date (which may be correct if involved in initialising the project)."); }
                if (startChanged && FromDate > Today.AddYears(1))
                { MessageFunctions.AddQuery("This role starts more than a year in the future."); }
                if (HasKeyRole)
                {
                    if (predecessor != null
                        && ((FromDate != null && (predecessor.FromDate == null || predecessor.FromDate < FromDate)))
                        && ((ToDate != null && (predecessor.ToDate == null || predecessor.ToDate > ToDate)))
                        )
                    {
                        MessageFunctions.AddQuery("Projects can only have one internal " + ProjectRole.RoleDescription + " at a time, and this project already has another " + ProjectRole.RoleDescription
                          + " throughout this period. The existing record will automatically be split into 'before' and 'after' sections.");
                    }
                    else if (ProjectFunctions.SubsumesStaff(this)) // Opposite scenario of above
                    {
                        MessageFunctions.AddQuery("Projects can only have one internal " + ProjectRole.RoleDescription + " at a time, and this period entirely covers an existing " + ProjectRole.RoleDescription
                          + " record. That record will therefore be automatically deleted, and other existing records' dates adjusted to avoid overlaps.");
                    }
                    else if (RoleOverlap()) // Only if not throwing above - i.e. there is at least one overlap, but no complete replacement or split
                    {
                        MessageFunctions.AddQuery("Projects can only have one internal " + ProjectRole.RoleDescription + " at a time, and this project already has another " + ProjectRole.RoleDescription
                          + " during part of this period. Existing records' dates will be automatically adjusted to avoid overlaps.");
                    }
                    if (predecessor != null && predecessor.ToDate != null && EffectiveFrom.AddDays(-1) > predecessor.ToDate)
                    {
                        MessageFunctions.AddQuery(ProjectRole.RoleDescription + " is a key role, but this leaves a gap after the previous incumbent, so that record will be extended automatically. "
                            + "If that is not correct please adjust the 'from' date, or (after saving) add another interim record in between; existing dates will be adjusted to fit.");
                    }
                    if (Successor() != null && Successor().FromDate != null && EffectiveTo.AddDays(1) < Successor().FromDate)
                    {
                        MessageFunctions.AddQuery(ProjectRole.RoleDescription + " is a key role, but this leaves a gap to the next incumbent, so that record will be extended automatically. "
                            + "If that is not correct please adjust the 'to' date, or (after saving) add another interim record in between; existing dates will be adjusted to fit.");
                    }
                }
                return MessageFunctions.AskQuery("");
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error validating project team details", generalException);
                return false;
            }
		    finally
            {
                MessageFunctions.ClearQuery();
            }
        }

        public bool ConvertToProjectTeam(ref ProjectTeams saveTeam)
        {
            try
            {
                saveTeam.ProjectID = Project.ID;
                saveTeam.StaffID = StaffID;
                saveTeam.ProjectRoleCode = RoleCode;
                saveTeam.FromDate = FromDate;
                saveTeam.ToDate = ToDate;
                
                return true;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error converting summary to project team record", generalException);
                return false;
            }	            
        }

    } // class
} // namespace
