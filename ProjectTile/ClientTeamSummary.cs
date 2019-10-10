using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectTile
{
    class ClientTeamSummary : Globals
    {
        public int ID { get; set; }
        public Projects Project { get; set; }
        public ContactSummaryRecord Contact { get; set; }
        public ClientTeamRoles ClientRole { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public ProjectStages ProjectStage
        {
            get { return ProjectFunctions.GetStageByCode(Project.StageCode); }
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

        public int ContactID
        {
            get { return (Contact == null) ? 0 : Contact.ID; }
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
            get { return (ClientRole == null) ? "" : ClientRole.RoleCode; }
        }

        public bool HasKeyRole
        {
            get
            {
                return (Array.IndexOf(Globals.KeyClientRoles, RoleCode) >= 0);
            }
        }

        public ClientTeams Predecessor()
        {
            return ProjectFunctions.GetContactPredecessor(this) ?? null; // Latest record (by 'to' date, then 'from' date) that starts earlier
        }

        public ClientTeams Successor()
        {
            return ProjectFunctions.GetContactSuccessor(this) ?? null; // Earliest record (by 'from' date, then 'to' date) that ends later
        }
      
        public bool IsDuplicate()
        {
            return (ProjectFunctions.DuplicateProjectContact(this, byRole: false) == true);            
        }

        public bool AlreadyOnProject()
        {
            return (ProjectFunctions.DuplicateProjectContact(this, byRole: false) != false); // Expect null if on project but not a duplicate, i.e. different role or dates
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

        public ClientTeamSummary ShallowCopy()
        {
            return (ClientTeamSummary) this.MemberwiseClone();
        }

        public bool RoleOverlap()
        {
            return (ProjectFunctions.DuplicateProjectContact(this, byRole: true) == true);
        }

        public bool ValidateTeamRecord(ClientTeamSummary savedVersion)
        {
            bool contactChanged = true;
            bool roleChanged = true;
            bool startChanged = true;
            bool amendment = false;
            ClientTeams predecessor = Predecessor();

            if (savedVersion == null) { savedVersion = new ClientTeamSummary(); } // Prevents having to check for null each time
            else
            {
                amendment = true;
                contactChanged = (savedVersion.ContactID != ContactID);
                roleChanged = (savedVersion.RoleCode != RoleCode);
                startChanged = (savedVersion.FromDate != FromDate);
            }
            try
            {
                string errorMessage = "";
                if (Contact == null)
                { errorMessage = "Please choose a contact from the list, or use the 'Search' function.|No Contact Selected"; }
                else if (ClientRole == null)
                { errorMessage = "Please select a project role for the contact member.|No Role Selected"; }
                else if (FromDate == null)
                { errorMessage = "Please enter a date from which this user is (or was) part of the team.|No Start Date"; }
                else if (EffectiveTo < FromDate)
                { errorMessage = "The 'To' date cannot be after the 'From' date.|Invalid Dates"; }
                else if (amendment && (contactChanged || roleChanged) && savedVersion.IsHistoric)
                { errorMessage = "Past team members cannot be amended, except to change their dates.|Historic Record"; }
                else if (!Contact.Active && FromDate <= Today && EffectiveTo > Today)
                { errorMessage = Contact.ContactName + " is inactive and cannot be part of the current project team now, but can be added for a future date.|Inactive User"; }
                else if (IsDuplicate())
                { errorMessage = Contact.ContactName + " already has the same role in the project during this period. Please check the existing record.|Duplicate Record"; }
                else if (HasKeyRole && predecessor == null && FromDate > Project.StartDate)
                {
                    errorMessage = ClientRole.RoleDescription + " is a key role, and is not covered at the start of the project. Please adjust the 'from' date, then (if appropriate) "
                      + "add an initial " + ClientRole.RoleDescription + " afterwards; the date of this record will then be adjusted automatically.|Key Role Not Covered";
                }
                else if (HasKeyRole && Successor() == null & ToDate != null)
                {
                    errorMessage = ClientRole.RoleDescription + " is a key role, and must always have a current record. Please leave the 'to' date blank, then (if appropriate) "
                      + "add a subsequent " + ClientRole.RoleDescription + " afterwards; the date of this record will then be adjusted automatically.|Key Role Not Covered";
                }
                else if (amendment && roleChanged && savedVersion.HasKeyRole)
                {
                    errorMessage = "The existing role (" + savedVersion.ClientRole.RoleDescription + ") is a key role, and must always be filled during the project. Please ensure "
                      + "continuity in that role (e.g. by selecting an alternative contact for this record) before changing/adding this user's new project role.|Key Role Not Covered";
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
                { MessageFunctions.AddQuery(Contact.ContactName + " is already a client team member in a different capacity, or at a different time."); }
                if (amendment && (contactChanged || roleChanged) && Project.StartDate < Today.AddMonths(-1))
                { MessageFunctions.AddQuery("This change is more than a month after the start of the project, which suggests an addition rather than amendment is required."); }
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
                        MessageFunctions.AddQuery("Projects can only have one " + ClientRole.RoleDescription + " at a time, and this project already has another " + ClientRole.RoleDescription
                          + " throughout this period. The existing record will automatically be split into 'before' and 'after' sections.");
                    }
                    else if (ProjectFunctions.SubsumesContact(this)) // Opposite scenario of above
                    {
                        MessageFunctions.AddQuery("Projects can only have one " + ClientRole.RoleDescription + " at a time, and this period entirely covers an existing " + ClientRole.RoleDescription
                          + " record. That record will therefore be automatically deleted, and other existing records' dates adjusted to avoid overlaps.");
                    }
                    else if (RoleOverlap()) // Only if not throwing above - i.e. there is at least one overlap, but no complete replacement or split
                    {
                        MessageFunctions.AddQuery("Projects can only have one " + ClientRole.RoleDescription + " at a time, and this project already has another " + ClientRole.RoleDescription
                          + " during part of this period. Existing records' dates will be automatically adjusted to avoid overlaps.");
                    }
                    if (predecessor != null && predecessor.ToDate != null && EffectiveFrom.AddDays(-1) > predecessor.ToDate)
                    {
                        MessageFunctions.AddQuery(ClientRole.RoleDescription + " is a key role, but this leaves a gap after the previous incumbent, so that record will be extended automatically. "
                            + "If that is not correct please adjust the 'from' date, or (after saving) add another interim record in between; existing dates will be adjusted to fit.");
                    }
                    if (Successor() != null && Successor().FromDate != null && EffectiveTo.AddDays(1) < Successor().FromDate)
                    {
                        MessageFunctions.AddQuery(ClientRole.RoleDescription + " is a key role, but this leaves a gap to the next incumbent, so that record will be extended automatically. "
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

        public bool ConvertToClientTeam(ref ClientTeams saveTeam)
        {
            try
            {
                saveTeam.ProjectID = Project.ID;
                saveTeam.ClientStaffID = ContactID;
                saveTeam.ClientTeamRoleCode = RoleCode;
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
