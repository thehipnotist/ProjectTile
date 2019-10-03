using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ProjectTile
{
    public class ProjectFunctions : Globals
    {
        // ---------------------------------------------------------- //
        // -------------------- Global Variables -------------------- //
        // ---------------------------------------------------------- //

        public static ProjectSummaryRecord SelectedTeamProject = null;
        public delegate void ReturnToTeamsDelegate();
        public static ReturnToTeamsDelegate SelectProjectForTeam;
        public static ReturnToTeamsDelegate CancelTeamProjectSelection;

        // ------------------ Lists ----------------- //

        public static List<ProjectSummaryRecord> FullProjectList;
        public static List<ProjectSummaryRecord> ProjectGridList;
        public static List<ProjectSummaryRecord> ProjectFilterList;
        public static List<string> StatusFilterList;
        public static List<ProjectStages> FullStageList;
        public static List<ProjectTypes> FullTypeList;
        public static List<StaffSummaryRecord> FullPMsList;
        public static List<StaffSummaryRecord> PMFilterList;
        public static List<StaffSummaryRecord> PMOptionsList;
        public static List<TeamSummaryRecord> FullTeamsList;
        public static List<TeamSummaryRecord> TeamsGridList;
        public static List<ProjectRoles> FullRolesList;
        public static List<ProjectRoles> RolesFilterList;
        public static List<ClientSummaryRecord> FullClientList;
        public static List<ClientSummaryRecord> ClientFilterList;
        public static List<ClientSummaryRecord> ClientOptionsList;

        // ---------------------------------------------------------- //
        // -------------------- Page Management --------------------- //
        // ---------------------------------------------------------- //

        // --------------- Navigation --------------- // 	

        public static void ReturnToTilesPage()
        {
            ResetProjectParameters();
            PageFunctions.ShowTilesPage();
        }

        public static void ReturnToClientPage(string pageMode)
        {
            ResetProjectParameters();
            PageFunctions.ShowClientPage(pageMode);
        }

        public static void ReturnToProjectPage()
        {
            PageFunctions.ShowProjectPage(ProjectSourceMode, ProjectSourcePage);
        }

        public static void SelectTeamProject(ProjectSummaryRecord selectedRecord)
        {
            try
            {
                SelectedTeamProject = selectedRecord;
                SelectProjectForTeam();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error handling project selection", generalException); }
        }

        public static void BackToTeam()
        {
            CancelTeamProjectSelection();
        }

        // --------------- Page setup --------------- //
        
        public static Visibility BackButtonVisibility()
        {
            return (ProjectSourcePage != Globals.TilesPageName)? Visibility.Visible : Visibility.Hidden;
        }

        // ---------------------------------------------------------- //
        // -------------------- Data Management --------------------- //
        // ---------------------------------------------------------- //  

        // ------------- Data retrieval ------------- //

        public static bool SetFullProjectList()
        {
            try
            {
                List<ProjectSummaryRecord> projectList = null;
                
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    projectList = (from pj in existingPtDb.Projects
                                   join ps in existingPtDb.ProjectStages on pj.StageCode equals ps.StageCode
                                   join t in existingPtDb.ProjectTypes on pj.TypeCode equals t.TypeCode
                                   where pj.EntityID == CurrentEntityID
                                   orderby (new { ps.StageCode, pj.StartDate })
                                   select new ProjectSummaryRecord
                                   {
                                       ProjectID = pj.ID,
                                       ProjectCode = pj.ProjectCode,
                                       ProjectName = pj.ProjectName,
                                       ProjectSummary = pj.ProjectSummary,
                                       Type = t,
                                       EntityID = pj.EntityID,
                                       Stage = ps,
                                       StartDate = pj.StartDate
                                   }
                        ).ToList();
                }

                foreach (ProjectSummaryRecord thisProject in projectList)
                {
                    thisProject.Client = GetProjectClientSummary(thisProject.ProjectID);
                    thisProject.ProjectManager = GetCurrentPM(thisProject.ProjectID);
                }

                FullProjectList = projectList;
                return true;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving full project data", generalException);
                return false;
            }
        }

        public static bool IsInFilter(ProjectStatusFilter inStatus, ProjectStages stageRecord)
        {
            int stage = stageRecord.StageCode;
            string status = stageRecord.ProjectStatus;
            
            return ( inStatus == ProjectStatusFilter.All
                || (inStatus == ProjectStatusFilter.Current && stage < CompletedStage)
                || (inStatus == ProjectStatusFilter.Open && stage >= StartStage && stage < CompletedStage)
                || (inStatus == ProjectStatusFilter.InProgress && status == InProgressStatus)
                || (inStatus == ProjectStatusFilter.Closed && status == ClosedStatus) );
        }
        
        public static bool SetProjectGridList(ProjectStatusFilter inStatus, int clientID = 0, int ourManagerID = 0)
        {
            try
            {
                bool success = SetFullProjectList();
                if (success)
                {
                    ProjectGridList = 
                        (from fpl in FullProjectList
                        where  (clientID == 0 || fpl.Client.ID == clientID)
                            &&  (ourManagerID == 0 || fpl.ProjectManager.ID == ourManagerID)
                            && IsInFilter(inStatus, fpl.Stage)
                        select fpl
                        ).ToList();

                    return true;
                }
                else { return false; }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving project grid data", generalException);
                return false;
            }
        }

        public static void SetProjectFilterList(ProjectStatusFilter inStatus)
        {
            try
            {
                bool success = SetFullProjectList();
                if (success)
                {
                    ProjectFilterList =
                        (from fpl in FullProjectList
                         where IsInFilter(inStatus, fpl.Stage)
                         select fpl
                        ).ToList();
                    ProjectFilterList.Insert(0, SearchProjects);
                    ProjectFilterList.Add(AllProjects);
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving project drop-down data", generalException); }
        }

        public static bool PopulateFromFilters(ref ProjectSummaryRecord thisSummary)
        {
            try
            {
                string question = "";
                StaffSummaryRecord manager = SelectedPMSummary;
                ClientSummaryRecord client = SelectedClientSummary;

                bool useManager = (manager.ID != 0 && manager.Active);
                bool useClient = (client.ID != 0 && client.ActiveClient);
                if (useManager)
                {
                    question = question + "Project Manager";
                    if (useClient) { question = question + " and client"; }
                }
                else if (useClient) { question = question + "client"; }
                else { return false; }

                question = "Use the selected " + question + " details from the filter?";
                if (useClient && useManager) { question = question.Replace("filter", "filters"); }
                bool autoPopulate = MessageFunctions.QuestionYesNo(question, "Use Filter Details?");
                if (!autoPopulate) { return false; }

                if (useClient) { thisSummary.Client = client; }
                if (useManager) { thisSummary.ProjectManager = manager; }
                return true;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving project details from filters. This will not prevent the creation of the project, however", generalException);
                return false;
            }		
        }

        public static Projects GetProject(int projectID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return existingPtDb.Projects.Find(projectID);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving project with ID " + projectID.ToString(), generalException);
                return null;
            }	            
        }

        // Stages and statuses
        public static string StatusFilterName(string asString) // Overloaded - string input version
        {
            return (asString == "InProgress") ? "In Progress" : asString;
        }

        public static string StatusFilterName(ProjectStatusFilter asType = ProjectStatusFilter.All) // Overloaded - type input version
        {
            string asString = asType.ToString();
            return StatusFilterName(asString);
        }
        
        public static void SetProjectStatusFilter()
        {
            string[] filterArray = Enum.GetNames(typeof(ProjectStatusFilter)).ToArray();
            for (int i = 0; i < filterArray.Length - 1; i++)
            {
                filterArray[i] = StatusFilterName(filterArray[i]);
            }
            StatusFilterList = filterArray.ToList();
        }

        public static void SetFullStageList()
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    FullStageList = (from ps in existingPtDb.ProjectStages
                                     orderby ps.StageCode
                                     select (ProjectStages)ps).ToList();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving list of project stages", generalException); }
        }

        public static ProjectStages GetStageByCode(int stageCode)
        {
            try
            {
                SetFullStageList();
                ProjectStages thisStage = FullStageList.FirstOrDefault(tl => tl.StageCode == stageCode);
                if (thisStage != null) { return thisStage; }
                else 
                { 
                    MessageFunctions.Error("Error retrieving project stage with code " + stageCode.ToString() + ": no matching stage exists.", null);
                    return null;
                }
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error retrieving project stage with code " + stageCode.ToString(), generalException);
                return null;
            }
        }

        public static bool IsLastStage(int stageCode)
        {
            try
            {
                if (stageCode == CancelledStage) { return true; }
                
                SetFullStageList();
                int lastStageCode = FullStageList.Where(tl => tl.StageCode != CancelledStage).OrderByDescending(fsl => fsl.StageCode).FirstOrDefault().StageCode;
                if (lastStageCode > 0) { return (stageCode == lastStageCode); }
                else
                {
                    MessageFunctions.Error("Error comparing to the last project stage: could not identify the last stage.", null);
                    return false;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error comparing to the last project stage", generalException);
                return false;
            }
        }

        // Project types
        public static void SetFullTypeList()
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    FullTypeList = (from pt in existingPtDb.ProjectTypes
                                            select pt).ToList();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving list of project types", generalException); }
        }

        public static ProjectTypes GetTypeFromName(string typeName)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return existingPtDb.ProjectTypes.FirstOrDefault(pt => pt.TypeName == typeName);
                }
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error retrieving project type matching name '" + typeName + "'", generalException);
                return null;
            }
        }

        // Project Managers (internal)
        public static void SetFullPMsList()
        {
            List<int> currentManagers;
            List<StaffSummaryRecord> managersList;
            
            try
            {
                SetFullProjectList();
                currentManagers = (from fpl in FullProjectList
                                   select fpl.ProjectManager.ID).Distinct().ToList();               
                
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    managersList =
                        (from s in existingPtDb.Staff
                         join se in existingPtDb.StaffEntities on s.ID equals se.StaffID
                         where se.EntityID == CurrentEntityID 
                            && (s.RoleCode == ProjectManagerCode || currentManagers.Contains(s.ID)) 
                         orderby new { s.FirstName, s.Surname, s.UserID }
                         select new StaffSummaryRecord
                         {
                             ID = (int)s.ID,
                             EmployeeID = s.EmployeeID,
                             FirstName = s.FirstName,
                             Surname = s.Surname,
                             RoleCode = s.RoleCode,
                             StartDate = (DateTime?)DbFunctions.TruncateTime(s.StartDate),
                             LeaveDate = (DateTime?)DbFunctions.TruncateTime(s.LeaveDate),
                             UserID = s.UserID,
                             Active = (bool)s.Active,
                             DefaultEntity = (int)s.DefaultEntity
                         }
                         ).Distinct().ToList();
                }
                FullPMsList = managersList;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving details of Project Managers", generalException);
            }
        }

        public static void SetPMFilterList()
        {
            try
            {
                SetFullPMsList();
                List<StaffSummaryRecord> comboList = FullPMsList;
                comboList.Add(AllPMs);
                PMFilterList = comboList;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving data for Project Manager filter", generalException); }
        }

        public static void SetPMOptionsList(bool anyActiveUser, int currentManagerID = 0)
        {
            try
            {
                List<StaffSummaryRecord> managerList = null;
                if (anyActiveUser) { managerList = StaffFunctions.GetStaffGridData(activeOnly: true, nameContains: "", roleDescription: AllRecords, entityID: CurrentEntityID); }
                else
                {
                    SetFullPMsList();
                    managerList = FullPMsList.Where(fpl => fpl.Active || fpl.ID == currentManagerID).ToList();
                }
                if (currentManagerID > 0  && !managerList.Exists(pol => pol.ID == currentManagerID))
                {                    
                    StaffSummaryRecord thisManager = StaffFunctions.GetStaffSummary(currentManagerID);
                    managerList.Add(thisManager);                    
                }
                PMOptionsList = managerList.OrderBy(pol => pol.StaffName).ToList();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving data for Project Managers drop-down list", generalException); }
        }

        public static StaffSummaryRecord GetPMInOptionsList(int managerID)
        {
            try
            {
                StaffSummaryRecord thisPM = PMOptionsList.Where(pol => pol.ID == managerID).FirstOrDefault();
                if (thisPM != null) { return thisPM; }
                else
                {
                    MessageFunctions.Error("Error retrieving Project Manager details with ID " + managerID.ToString() + ": no matching record found.", null);
                    return null;
                }
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error retrieving Project Manager details with ID " + managerID.ToString(), generalException);
                return null;
            }
        }

        public static List<ProjectTeams> CurrentAndFuturePMs(int projectID, DateTime upTo)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {            
                    return existingPtDb.ProjectTeams.OrderByDescending(pt => pt.FromDate)
                            .Where(pt => pt.ProjectID == projectID 
                                && pt.ProjectRoleCode == ProjectManagerCode 
                                && (pt.ToDate == null || pt.ToDate >= upTo))
                            .OrderByDescending(pt => new {pt.FromDate, pt.ID})
                            .ToList();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving list of current or future Project Managers", generalException);
                return null;
            }	
        }

        public static StaffSummaryRecord GetCurrentPM(int projectID)
        {
            try
            {
                ProjectTeams currentPMRecord = null;
                StaffSummaryRecord currentPM = null;
                List<ProjectTeams> eligiblePMs = CurrentAndFuturePMs(projectID, Yesterday);
                
                if (eligiblePMs.Count == 0) { return null; } // Shouldn't happen, but just in case
                else if (eligiblePMs.Count == 1) { currentPMRecord = eligiblePMs.First(); }
                else
                {
                    List<ProjectTeams> possiblePMs = eligiblePMs
                            .Where(ep => (ep.FromDate == null || ep.FromDate <= Today) 
                                && (ep.ToDate == null || ep.ToDate >= Today))
                            .OrderByDescending(ep => ep.FromDate)
                            .ToList();
                    currentPMRecord = possiblePMs.FirstOrDefault();                    
                }

                currentPM = StaffFunctions.GetStaffSummary(currentPMRecord.StaffID);
                if (currentPM == null) 
                {
                    MessageFunctions.Error("Error retrieving current Project Manager record for project with ID " + projectID.ToString() + ": no matching staff member found.", null);
                    return null;
                }
                else { return currentPM; }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving current Project Manager record for project with ID " + projectID.ToString(), generalException);
                return null;
            }	
        }

        // Project staff in general
        public static ProjectRoles GetRole(string roleCode)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return existingPtDb.ProjectRoles.Where(pr => pr.RoleCode == roleCode).FirstOrDefault();
                }
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error retrieving project role", generalException);
                return null;
            }	
        }
        
        public static int RolePosition(string roleCode)
        {
            int position = Array.IndexOf(ProjectRoleHeirarchy, roleCode);
            return (position >= 0)? position : ProjectRoleHeirarchy.Length;
        }
        
        public static void SetFullRolesList()
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    List<ProjectRoles> rolesList = existingPtDb.ProjectRoles.ToList();
                    FullRolesList = rolesList.OrderBy(rl => RolePosition(rl.RoleCode)).ToList();
				}
            }
            catch (Exception generalException) { MessageFunctions.Error("Error listing project roles", generalException); }	
        }

        public static bool HasEverHadRole(string projectRoleCode, string nameLike, bool exact)
        {
            try
            {
                nameLike = nameLike.ToUpper();
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    ProjectTeams firstResult = (from pt in existingPtDb.ProjectTeams
                                                join s in existingPtDb.Staff on pt.StaffID equals s.ID
                                                where pt.ProjectRoleCode == projectRoleCode 
                                                    && (nameLike == "" 
                                                        || (exact && (s.FirstName + " " + s.Surname).ToUpper() == nameLike) 
                                                        || (!exact && (s.FirstName + " " + s.Surname).ToUpper().Contains(nameLike)))
                                                select pt
                                                ).FirstOrDefault();
                    return (firstResult != null);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error checking project role history for the name filter", generalException);
                return false;
            }	
        }
        
        public static void SetRolesFilterList(string nameLike = "", bool exactName = false)
        {
            try
            {
                SetFullRolesList();
                RolesFilterList = FullRolesList.Where(frl => nameLike == "" || HasEverHadRole(frl.RoleCode, nameLike, exactName)).ToList();
                RolesFilterList.Add(AllRoles);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error setting list of possible roles", generalException); }	
        }

        public static void SetFullTeamsList()
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    FullTeamsList = (from pt in existingPtDb.ProjectTeams
                                     join pj in existingPtDb.Projects on pt.ProjectID equals pj.ID
                                     join s in existingPtDb.Staff on pt.StaffID equals s.ID
                                     join pr in existingPtDb.ProjectRoles on pt.ProjectRoleCode equals pr.RoleCode
                                     select new TeamSummaryRecord
                                     {
                                         ID = pt.ID,
                                         Project = pj,
                                         StaffMember = new StaffSummaryRecord
                                             {
                                                 ID = (int)s.ID,
                                                 EmployeeID = s.EmployeeID,
                                                 FirstName = s.FirstName,
                                                 Surname = s.Surname,
                                                 RoleCode = s.RoleCode,
                                                 StartDate = (DateTime?)DbFunctions.TruncateTime(s.StartDate),
                                                 LeaveDate = (DateTime?)DbFunctions.TruncateTime(s.LeaveDate),
                                                 UserID = s.UserID,
                                                 Active = (bool)s.Active,
                                                 DefaultEntity = (int)s.DefaultEntity
                                             },
                                         ProjectRole = pr,
                                         FromDate = pt.FromDate,
                                         ToDate = pt.ToDate
                                     }
                                     ).ToList();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving full list of project team members", generalException); }
        }

        public static bool SetTeamsGridList(ProjectStatusFilter inStatus, string teamRoleCode, TeamTimeFilter timeFilter, int projectID = 0, string nameContains = "", bool exact = false)
        {
            try
            {
                nameContains = nameContains.ToUpper();
                SetFullTeamsList();
                TeamsGridList = FullTeamsList.Where(ftl => ((projectID == 0 && IsInFilter(inStatus, ftl.ProjectStage)) 
                                                    || (projectID != 0 && ftl.Project.ID == projectID ))
                                                && (nameContains == "" 
                                                    || (exact && ftl.StaffMember.StaffName.ToUpper() == nameContains) 
                                                    || (!exact && ftl.StaffMember.StaffName.ToUpper().Contains(nameContains)))
                                                && (teamRoleCode == AllCodes 
                                                    || ftl.ProjectRole.RoleCode == teamRoleCode)
                                                && (timeFilter == TeamTimeFilter.All 
                                                    || IsInTimeFilter(timeFilter, ftl))
                                                ).ToList();

                if (projectID > 0) { TeamsGridList = TeamsGridList.OrderBy(tgl => RolePosition(tgl.ProjectRole.RoleCode)).OrderBy(tgl => tgl.EffectiveFrom).ToList(); }
                return true;
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error retrieving project team members to display", generalException);
                return false;
            }
        }

        public static bool IsInTimeFilter(TeamTimeFilter timeFilter, TeamSummaryRecord teamMembership)
        {
            DateTime? fromDate = teamMembership.FromDate;
            DateTime? toDate = teamMembership.ToDate;

            if (timeFilter == TeamTimeFilter.All) { return true; }
            else if (toDate != null && toDate < Today) { return false; }
            else if (timeFilter == TeamTimeFilter.Current && fromDate != null && fromDate > Today) { return false; }
            else { return true; };
        }

        public static bool IsCurrentRole(TeamSummaryRecord teamMembership)
        {
            return IsInTimeFilter(TeamTimeFilter.Current, teamMembership);
        }

        public static bool? HasDuplicateRecord(TeamSummaryRecord thisRecord, bool byRole)
        {
            try
            {
                SetFullTeamsList();
                List<TeamSummaryRecord> otherInstances = FullTeamsList
                    .Where(ftl => ftl.ID != thisRecord.ID
                        && ftl.Project.ID == thisRecord.Project.ID
                        && ((byRole && ftl.RoleCode == thisRecord.RoleCode) || (!byRole && ftl.StaffID == thisRecord.StaffID)))
                    .ToList();

                if (otherInstances.Count == 0) { return false; }
                else
                {
                    foreach (TeamSummaryRecord thisInstance in otherInstances)
                    {
                        if (thisInstance.RoleCode == thisRecord.RoleCode) // Required if by staff member, as returns null if found (but only) with a different role
                        {
                            if (thisInstance.EffectiveFrom > thisRecord.EffectiveTo || thisInstance.EffectiveTo < thisRecord.EffectiveFrom) { } // No overlap                            
                            else { return true; }
                        }
                    }
                }
                return null; // If haven't returned already, there is another instance but not a duplicate
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error checking for duplicate or overlapping records", generalException);
                return null;
            }
        }

        public static bool Subsumes(TeamSummaryRecord thisRecord)
        {
            try
            {
                SetFullTeamsList();
                return FullTeamsList.Exists(ftl => ftl.ID != thisRecord.ID
                        && ftl.Project.ID == thisRecord.Project.ID
                        && ftl.RoleCode == thisRecord.RoleCode
                        && ((ftl.FromDate != null && ftl.FromDate >= thisRecord.EffectiveFrom) ||
                            ((ftl.FromDate == null || ftl.FromDate == thisRecord.Project.StartDate) && thisRecord.EffectiveFrom == thisRecord.Project.StartDate))
                        && ((ftl.ToDate != null && ftl.ToDate <= thisRecord.EffectiveTo) || (ftl.ToDate == null && thisRecord.ToDate == null)));
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error checking for concurrent records", generalException);
                return false;
            }
        }

        public static ProjectTeams GetPredecessor(TeamSummaryRecord currentRecord)
        {
            try
            {                
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return existingPtDb.ProjectTeams
                        .Where(pt => pt.ID != currentRecord.ID
                            && pt.ProjectID == currentRecord.Project.ID
                            && pt.ProjectRoleCode == currentRecord.RoleCode
                            && (pt.FromDate == null || pt.FromDate == currentRecord.Project.StartDate || pt.FromDate < currentRecord.EffectiveFrom))
                        .OrderByDescending(pt => pt.ToDate ?? InfiniteDate)
                        .OrderByDescending(pt => pt.FromDate ?? StartOfTime)
                        .FirstOrDefault();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving predecessor information", generalException);
                return null;
            }	
        }

        public static ProjectTeams GetSuccessor(TeamSummaryRecord currentRecord)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return existingPtDb.ProjectTeams
                        .Where(pt => pt.ID != currentRecord.ID
                            && pt.ProjectID == currentRecord.Project.ID
                            && pt.ProjectRoleCode == currentRecord.RoleCode
                            && (pt.ToDate == null || pt.ToDate > currentRecord.EffectiveTo))
                        .OrderBy(pt => pt.FromDate ?? StartOfTime)
                        .OrderBy(pt => pt.ToDate ?? InfiniteDate)
                        .FirstOrDefault();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving successor information", generalException);
                return null;
            }
        }

        // Clients
        public static void SetFullClientList()
        {
            FullClientList = ClientFunctions.ClientGridList(false, "", 0, CurrentEntityID);
        }
        
        public static void SetClientFilterList()
        {
            try
            {
                if (ClientFilterList != null) { ClientFilterList.Clear(); }
                SetFullClientList();
                List<ClientSummaryRecord> comboList = FullClientList;
                comboList.Add(AnyClient);
                comboList.Insert(0, NoClient);
                ClientFilterList = comboList;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving data for client filter", generalException); }
        }

        public static void SetClientOptionsList(int currentClientID = 0)
        {
            try
            {
                if (ClientOptionsList != null) { ClientOptionsList.Clear(); }
                SetFullClientList();
                List<ClientSummaryRecord> comboList = FullClientList.Where(fcl => fcl.ActiveClient || fcl.ID == currentClientID).ToList();
                comboList.Insert(0, NoClient);
                ClientOptionsList = comboList;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving data for client drop-down list", generalException); }
        }

        public static ClientSummaryRecord GetClientInOptionsList(int clientID)
        {
            try
            {
                ClientSummaryRecord thisClient = ClientOptionsList.Where(col => col.ID == clientID).FirstOrDefault();
                if (thisClient != null) { return thisClient; }
                else
                {
                    MessageFunctions.Error("Error retrieving client details with ID " + clientID.ToString() + ": no matching record found.", null);
                    return null;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving client details with ID " + clientID.ToString(), generalException);
                return null;
            }
        }

        public static ClientSummaryRecord GetProjectClientSummary(int projectID)
        {
            ClientSummaryRecord noClient = new ClientSummaryRecord { ID = NoID, ClientCode = "", ClientName = "", EntityID = CurrentEntityID };
            
            if (projectID == 0) { return noClient; }
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    Projects thisProject = existingPtDb.Projects.Where(p => p.ID == projectID).FirstOrDefault();
                    if (thisProject == null)
                    {
                        MessageFunctions.Error("Error retrieving project client details for project ID " + projectID.ToString() + ": no project found.", null);
                        return NoClient;
                    }
                    
                    int? clientID = thisProject.ClientID;
                    if (clientID == null || clientID == 0) { return noClient; }
                    else { return ClientFunctions.GetClientSummary((int) clientID); }
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving project client details for project ID " + projectID.ToString(), generalException);
                return noClient;
            }	
        }

        // -------------- Data updates -------------- // 

        public static bool IsGoLive(int originalStage, int newStage)
        {
            return (newStage >= LiveStage && newStage != CancelledStage && originalStage < LiveStage);
        }

        public static bool IsLiveReversal(int originalStage, int newStage)
        {
            return (originalStage >= LiveStage && originalStage != CancelledStage && newStage < LiveStage);
        }

        public static string MissingTeamMembers(List<string> currentTeam, bool clientTeam)
        {
            string result = "";
            string[] keyRoles = clientTeam ? KeyClientRoles : KeyInternalRoles;
            foreach (string role in keyRoles.Reverse())
            {
                if (!currentTeam.Contains(role))
                {
                    string description = GetRole(role).RoleDescription;
                    if (result == "") { result = description; }
                    else if (!result.Contains("and")) { result = description + " and " + result; }
                    else { result = description + ", " + result; }
                }
            }
            return result;
        }
        
        public static bool ValidateProject(ProjectSummaryRecord summary, bool amendExisting, bool managerChanged)
        {
            try
            {                
                string errorDetails = "";
                string errorMessage = "";
                string queryDetails = "";
                string queryMessage = "";
                List<Projects> otherClientProjects = null;
                List<ProjectProducts> linkedProjectProducts = null;
                List<ClientProducts> liveClientProducts = null;
                string type = (summary.Type != null) ? summary.Type.TypeCode : "";
                int stage = (summary.Stage != null) ? summary.StageID : -1;
                bool isUnderway = (summary.Stage != null && stage > StartStage && !summary.IsCancelled);
                Projects existingProjectRecord = null;
                ClientSummaryRecord client = summary.Client ?? null;
                List<string> currentInternalRoles = null;
                string missingInternalRoles = "";
                List<string> currentClientRoles = null;
                string missingClientRoles = "";
                int projectID = summary.ProjectID;

                try
                {
                    if (client == null)
                    { errorDetails = ". Please select a client record from the drop-down list. If this is an internal project, select '" + NoRecord + "'|No Client Selected"; }
                    else if (!summary.IsInternal && !client.ActiveClient)
                    { errorDetails = ", as the selected client record is not active.|Inactive Client"; }
                    else if (summary.ProjectName == null || summary.ProjectName == "")
                    { errorDetails = ". Please enter a name for the project.|No Project Name"; }
                    else if (type == "")
                    { errorDetails = ". Please select a project type from the drop-down list.|No Type Selected"; }
                    else if (summary.IsInternal && client.ID != NoID)
                    {
                        errorDetails = ". Projects for clients cannot be internal. Please choose a different "
                            + "project type if the client selection is correct, or select '" + NoRecord + "' from the client drop-down for an internal project.|Client and Project Type Mismatch";
                    }
                    else if (!summary.IsInternal && client.ID == NoID)
                    {
                        errorDetails = ". Projects without clients must use the 'Internal project' type. "
                            + "Please choose the correct project type if this is an internal project, or select a client from the client drop-down.|Client and Project Type Mismatch";
                    }
                    else if (isUnderway && summary.StartDate == null)
                    {
                        errorDetails = ", as no start date has been set. Please enter a start date or keep the project in the 'Initation' stage."
                            + "|No Start Date";
                    }
                    else if (isUnderway && summary.StartDate > Today)
                    {
                        errorDetails = ", as the start date is in the future. Please change the start date or keep the project in the 'Initation' stage."
                            + "|No Start Date";
                    }
                    else if (summary.ProjectManager == null)
                    {
                        errorDetails = ". Please select a Project Manager from the drop-down list. Try the 'Any' option if the required record is not listed, "
                            + "otherwise check that their account is active.|No Project Manager Selected";
                    }
                    else if (!summary.ProjectManager.Active)
                    { errorDetails = ", as the selected Project Manager is not an active user. Ask an administrator for help if required." + "|Inactive Project Manager"; }
                    else if (summary.Stage == null)
                    { errorDetails = ". Please select a project stage from the drop-down list.|No Stage Selected"; }
                    else if (summary.ProjectSummary == null || summary.ProjectSummary == "")
                    { errorDetails = ". Please enter a summary description of the project.|No Project Summary"; }
                    else
                    {
                        try
                        {
                            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                            using (existingPtDb)
                            {
                                otherClientProjects = existingPtDb.Projects.Where(
                                    p => p.ID != projectID
                                    && (p.ClientID == client.ID || (summary.IsInternal && (p.ClientID == null || p.ClientID <= 0)))
                                    ).ToList();
                                liveClientProducts = existingPtDb.ClientProducts.Where(
                                    cp => cp.ClientID == client.ID && cp.Live == true).ToList();
                                if (!summary.IsNew)
                                {
                                    existingProjectRecord = existingPtDb.Projects.Where(p => p.ID == projectID).FirstOrDefault();
                                    linkedProjectProducts = existingPtDb.ProjectProducts.Where(pp => pp.ProjectID == projectID).ToList();
                                    currentInternalRoles = existingPtDb.ProjectTeams.Where(pt => pt.ProjectID == projectID 
                                        && (pt.FromDate == null || pt.FromDate <= Today)
                                        && (pt.ToDate == null || pt.ToDate >= Today)).Select(pt => pt.ProjectRoleCode).ToList();
                                    currentClientRoles = existingPtDb.ClientTeams.Where(ct => ct.ProjectID == projectID
                                        && (ct.FromDate == null || ct.FromDate <= Today)
                                        && (ct.ToDate == null || ct.ToDate >= Today)).Select(ct => ct.ClientTeamRoleCode).ToList(); 
                                }
                            }
                            missingInternalRoles = MissingTeamMembers(currentInternalRoles, false);
                            missingClientRoles = MissingTeamMembers(currentClientRoles, true);
                            if (otherClientProjects.Exists(p => p.ProjectName == summary.ProjectName))
                            { errorDetails = ", as another project exists for the same client with the same name. Please change the project name."; }
                            else if (otherClientProjects.Exists(p => p.ProjectSummary == summary.ProjectSummary))
                            { errorDetails = ", as another project exists for the same client with the same project summary. Please change the summary details."; }
                            else if (isUnderway && missingInternalRoles != "")
                            {
                                errorDetails = ", as the project does not have a current (internal) " + missingInternalRoles + ". Key roles must be filled throughout the project. "
                                + "Please keep the project in  'Initation' stage, then use 'Project Teams (Staff)' to complete the project team."; 
                            }
                            else if (isUnderway && missingClientRoles != "")
                            {
                                errorDetails = ", as the client's project team does not have a current " + missingClientRoles + ". Key roles must be filled throughout the project. "
                                + "Please keep the project in 'Initation' stage until the required roles are set in 'Project (Client) Contacts'.";
                            }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error performing project validation against the database", generalException);
                            return false;
                        }
                    }

                    if (errorDetails != "")
                    {
                        if (summary.IsInternal) { errorDetails = errorDetails.Replace("another project exists for the same client", "another internal project exists"); }
                        if (amendExisting) { errorMessage = "Could not save changes" + errorDetails; }
                        else { errorMessage = "Could not save project" + errorDetails; }
                        MessageFunctions.SplitInvalid(errorMessage);
                        return false;
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error validating essential project rules", generalException);
                    return false;
                }

                try
                {
                    int originalStage = (existingProjectRecord == null) ? 0 : existingProjectRecord.StageCode;
                    bool goLive = IsGoLive(originalStage, stage);
                    string goLiveQuery = goLive? "This will set the project to Live, and update all linked products appropriately." : "";
                    bool reversal = goLive? false : IsLiveReversal(originalStage, stage);
                    string reversalQuery = reversal? "This will reverse the 'Go-Live' action, and all status and version changes to linked products." : "";
                    
                    if (managerChanged && summary.ProjectManager.RoleCode != ProjectManagerCode)
                    { queryDetails = queryDetails + "\n" + "The Project Manager is also not normally a Project Manager by role."; }
                    if (isUnderway && linkedProjectProducts == null)
                    { queryDetails = queryDetails + "\n" + "The project stage also indicates that the project is underway, but it has no linked products."; }
                    if (summary.StartDate == null && !summary.IsCancelled) { queryDetails = queryDetails + "\n" + "The project also has no predicted start date at present."; } // Only projects not yet underway
                    if (summary.StartDate > DateTime.Today.AddYears(1)) { queryDetails = queryDetails + "\n" + "The project also starts more than a year in the future."; }
                    if ((existingProjectRecord == null || existingProjectRecord.StartDate == null || existingProjectRecord.StartDate > summary.StartDate)
                        && summary.StartDate < DateTime.Today.AddYears(-1)) { queryDetails = queryDetails + "\n" + "The project also starts more than a year in the past."; }
                    if (originalStage > stage && !reversal) // Live reversals are handled separately
                    { queryDetails = queryDetails + "\n" + "The new stage is also less advanced than the previous one."; }
                    if (!summary.IsNew && stage - originalStage > 4 && !summary.IsCancelled) { queryDetails = queryDetails + "\n" + "The project has moved through several stages."; }
                    if (!summary.IsInternal)
                    {
                        if (liveClientProducts.Count == 0 && type != NewSiteCode)
                        { queryDetails = queryDetails + "\n" + "The project type also indicates a change to an existing product, but this client has no Live products."; }
                        else if (liveClientProducts.Count != 0 && (type == NewSiteCode))
                        { queryDetails = queryDetails + "\n" + "The project type also indicates a brand new installation for a new client, but this client already has one or more Live products."; }
                    }

                    string isCorrect = " Is this correct?";
                    if (queryDetails != "")
                    {
                        string otherQueries = "Are the following also intentional?\n" + queryDetails.Replace("also ","");
                        if (queryDetails.Count(qd => qd == '\n') == 1) { otherQueries = queryDetails.Replace("\n", ""); }

                        if (goLive) { queryMessage = goLiveQuery + " " + otherQueries; }
                        else if (reversal) { queryMessage = reversalQuery + " " + otherQueries; }
                        else 
                        { 
                            queryMessage = otherQueries.Replace("also ", "");
                            
                        }
                        if (queryMessage.Count(qd => qd == '\n') == 0) { queryMessage = queryMessage + isCorrect; }
                        
                        return MessageFunctions.WarningYesNo(queryMessage);
                    }
                    else if (summary.IsCancelled && existingProjectRecord != null && existingProjectRecord.StageCode != CancelledStage)
                    {
                        queryMessage = "Are you sure you wish to cancel this project?";
                        return MessageFunctions.ConfirmOKCancel(queryMessage);
                    }
                    else if (summary.IsNew)
                    {
                        queryMessage = "Are you sure you wish to create this project? Project records cannot be deleted, although they can be cancelled.";
                        return MessageFunctions.ConfirmOKCancel(queryMessage);
                    }
                    else if (goLive)
                    {
                        return MessageFunctions.ConfirmOKCancel(goLiveQuery + isCorrect);
                    }
                    else if (reversal)
                    {
                        return MessageFunctions.ConfirmOKCancel(reversalQuery + isCorrect);
                    }
                    else { return true; }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error checking for project queries", generalException);
                    return false;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error validating project", generalException);
                return false;
            }		
        }

        public static bool SaveNewProject(ref ProjectSummaryRecord projectSummary)
        {
            if (!ValidateProject(projectSummary, false, true)) { return false; }            
            try
            {
                Projects thisProject = new Projects();
                bool converted = projectSummary.ConvertToProject(ref thisProject);
                if (!converted || thisProject == null) { return false; } // Errors should be thrown by the conversion
                ProjectTeams addPM = new ProjectTeams
                {
                    ProjectID = 0, // Set below when the project is saved
                    StaffID = projectSummary.ProjectManager.ID,
                    ProjectRoleCode = ProjectManagerCode,
                    FromDate = (DateTime.Today < thisProject.StartDate) ? DateTime.Today : thisProject.StartDate
                };
                
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    existingPtDb.Projects.Add(thisProject);
                    existingPtDb.SaveChanges();
                    projectSummary.ProjectID = thisProject.ID;
                    SelectedProjectSummary = projectSummary;

                    addPM.ProjectID = thisProject.ID;
                    existingPtDb.ProjectTeams.Add(addPM);
                    existingPtDb.SaveChanges();
                    
                    MessageFunctions.SuccessMessage("New project saved successfully. You can now add products and project team members.", "Project Created");
                    return true;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error saving new project to the database", generalException);
                return false;
            }		            
        }

        public static bool SaveProjectChanges(ProjectSummaryRecord projectSummary, bool managerChanged, int originalStage)
        {
                   
            if (!ValidateProject(projectSummary, true, managerChanged)) { return false; }
            try
            {
                int projectID = projectSummary.ProjectID;
                
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    Projects thisProject = existingPtDb.Projects.Where(p => p.ID == projectID).FirstOrDefault();
                    if (thisProject == null)
                    {
                        MessageFunctions.Error("Error saving project amendments to the database: no matching project found.", null);
                        return false;
                    }
                    bool converted = projectSummary.ConvertToProject(ref thisProject);
                    if (!converted) { return false; } // Errors should be thrown by the conversion

                    if (managerChanged)
                    {
                        try
                        {
                            ProjectTeams newPMRecord = new ProjectTeams
                            {
                                ProjectID = projectID,
                                StaffID = projectSummary.ProjectManager.ID,
                                ProjectRoleCode = ProjectManagerCode,
                                FromDate = (DateTime.Today < thisProject.StartDate) ? DateTime.Today : thisProject.StartDate
                            };

                            List<ProjectTeams> existingPMs = CurrentAndFuturePMs(projectID, Yesterday);
                            if (existingPMs.Count == 0) { existingPtDb.ProjectTeams.Add(newPMRecord); } // Shouldn't happen, but just in case
                            else
                            {
                                ProjectTeams lastPMRecord = existingPMs.First();
                                lastPMRecord = existingPtDb.ProjectTeams.Find(lastPMRecord.ID); // Get it from the database again so we can amend/remove it

                                if (( lastPMRecord.FromDate != null && lastPMRecord.FromDate > OneMonthAgo) || projectSummary.StageID <= StartStage)
                                {
                                    string lastPMName = StaffFunctions.GetStaffName(lastPMRecord.StaffID);
                                    DateTime fromDateTime = (DateTime)lastPMRecord.FromDate;
                                    string fromDate = fromDateTime.ToString("dd MMMM yyyy");
                                    bool overwrite = MessageFunctions.WarningYesNo("A Project Manager history record exists for " + lastPMName + " starting on " + fromDate
                                        + ". Should it be overwritten? Selecting 'Yes' will replace that record; 'No' will retain it in the history.", "Replace Project Manager Record?");
                                    if (overwrite)
                                    {
                                        existingPtDb.ProjectTeams.Remove(lastPMRecord);
                                        existingPtDb.ProjectTeams.Add(newPMRecord);
                                    }
                                    else if (lastPMRecord.FromDate > Today)
                                    {
                                        DateTime toDate = (DateTime)lastPMRecord.FromDate;
                                        toDate = toDate.AddDays(-1);
                                        newPMRecord.ToDate = toDate;
                                        existingPtDb.ProjectTeams.Add(newPMRecord);
                                    }
                                    else
                                    {
                                        lastPMRecord.ToDate = Yesterday;
                                        newPMRecord.FromDate = Today;
                                        existingPtDb.ProjectTeams.Add(newPMRecord);
                                    }
                                }
                                else
                                {
                                    lastPMRecord.ToDate = Yesterday;
                                    newPMRecord.FromDate = Today;
                                    existingPtDb.ProjectTeams.Add(newPMRecord);
                                }
                            }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error processing change of Project Manager", generalException);
                            return false;
                        }
                    }

                    bool goLive = IsGoLive(originalStage, projectSummary.StageID);
                    bool reversal = goLive ? false : IsLiveReversal(originalStage, projectSummary.StageID);
                    bool completion = (projectSummary.StageID == CompletedStage && originalStage != CompletedStage);                    
                    if (goLive || reversal)
                    {
                        try
                        {
                                var linkedProducts = from p in existingPtDb.Projects
                                                     join pp in existingPtDb.ProjectProducts on p.ID equals pp.ProjectID
                                                     join cp in existingPtDb.ClientProducts on pp.ProductID equals cp.ProductID
                                                     where p.ID == projectID && cp.ClientID == p.ClientID
                                                     select new { ProjectProducts = pp, ClientProducts = cp };
                                                    
                                foreach (var product in linkedProducts)
                                {
                                    if (goLive)
                                    {
                                        product.ClientProducts.Live = true;
                                        product.ClientProducts.ProductVersion = product.ProjectProducts.NewVersion;
                                    }
                                    else
                                    {
                                        if (Array.IndexOf(NewProductTypeCodes, projectSummary.Type.TypeCode) >= 0) { product.ClientProducts.Live = false; }
                                        if (product.ClientProducts.ProductVersion == product.ProjectProducts.NewVersion) 
                                        { 
                                            product.ClientProducts.ProductVersion = product.ProjectProducts.OldVersion; 
                                        }
                                    }                                                                      
                                }
                            }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error updating linked products", generalException);
                            return false;
                        }
                    }	

                    existingPtDb.SaveChanges();
                    SelectedProjectSummary = projectSummary;
                    string congratulations = "";
                    if (completion) { congratulations = " Congratulations on completing the project."; }
                    else if (goLive) { congratulations = " Congratulations on going Live.";  }
                    
                    MessageFunctions.SuccessMessage("Project amendments saved successfully." + congratulations, "Changes Saved");
                    return true;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error saving project amendments to the database", generalException);
                return false;
            }		            
        }

        // Project Teams (updates)
        
        public static bool updateOtherInstances(TeamSummaryRecord newRecord)
        {
            try
            {
                int predecessorID = (GetPredecessor(newRecord) != null)? GetPredecessor(newRecord).ID : 0;
                int successorID =  (GetSuccessor(newRecord) != null)? GetSuccessor(newRecord).ID : 0;
                
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {                    
                    List<ProjectTeams> otherInstances = existingPtDb.ProjectTeams
                        .Where(pt => pt.ID != newRecord.ID
                            && pt.ProjectID == newRecord.Project.ID
                            && pt.ProjectRoleCode == newRecord.RoleCode)
                        .ToList();

                    if (otherInstances.Count == 0) { return true; }
                    else
                    {
                        DateTime dayBefore = newRecord.EffectiveFrom.AddDays(-1);
                        DateTime dayAfter = newRecord.EffectiveTo.AddDays(1);                    
                    
                        foreach (ProjectTeams current in otherInstances)
                        {
                            DateTime currentFrom = current.FromDate ?? StartOfTime;
                            DateTime currentTo = current.ToDate ?? InfiniteDate;

                            if (currentFrom >= dayAfter || currentTo <= dayBefore) // No overlap - but may need to change to close the gap
                            {
                                if ( current.ID == predecessorID && currentTo < dayBefore) { current.ToDate = dayBefore; }
                                else if (current.ID == successorID && currentFrom > dayAfter) { current.FromDate = dayAfter; }
                            }
                            else if (newRecord.FromDate != null && newRecord.ToDate != null && currentFrom <= dayBefore && currentTo >= dayAfter) // Split encompassing record
                            {
                                ProjectTeams additional = new ProjectTeams
                                {
                                    ProjectID = current.ProjectID,
                                    StaffID = current.StaffID,
                                    ProjectRoleCode = current.ProjectRoleCode,
                                    FromDate = dayAfter,
                                    ToDate = current.ToDate
                                };
                                current.ToDate = dayBefore;
                                existingPtDb.ProjectTeams.Add(additional);
                            }
                            else if (newRecord.FromDate != null && currentFrom <= dayBefore && currentTo > dayBefore && (current.ToDate == null || currentTo < dayAfter))
                            { current.ToDate = dayBefore; } // Curtail predecessor
                            else if (newRecord.ToDate != null && (current.FromDate == null || currentFrom > dayBefore) && currentFrom < dayAfter && currentTo >= dayAfter)
                            { current.FromDate = dayAfter; } // Delay successor
                            else if ((currentFrom > dayBefore || current.FromDate == null) && (currentTo < dayAfter || current.ToDate == null)) // Remove subsumed/replaced record
                            {
                                existingPtDb.ProjectTeams.Remove(current);
                            }
                        }
                    }
                    existingPtDb.SaveChanges();
                    return true;
                }
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error updating affected predecessors and/or successors", generalException);
                return false;
            }
        }

        public static bool SaveProjectTeamChanges(TeamSummaryRecord currentVersion, TeamSummaryRecord savedVersion)
        {
            if (!currentVersion.ValidateTeamRecord(savedVersion)) { return false; }

            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    ProjectTeams thisTeam = existingPtDb.ProjectTeams.Where(pt => pt.ID == savedVersion.ID).FirstOrDefault();
                    if (thisTeam == null)
                    {
                        MessageFunctions.Error("Error saving changes to project team member details: no matching record found.", null);
                        return false;
                    }
                    currentVersion.ConvertToProjectTeam(ref thisTeam);
                    updateOtherInstances(currentVersion);
                    existingPtDb.SaveChanges();
                    MessageFunctions.SuccessMessage("Your changes have been saved successfully.", "Team Membership Amended");
                    return true;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error saving changes to project team member details", generalException);
                return false;
            }	
        }

        public static int SaveNewProjectTeam(TeamSummaryRecord newRecord)
        {
            if (!newRecord.ValidateTeamRecord(null)) { return 0; }
            ProjectTeams thisTeam = new ProjectTeams();
            newRecord.ConvertToProjectTeam(ref thisTeam);

            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    existingPtDb.ProjectTeams.Add(thisTeam);
                    updateOtherInstances(newRecord);
                    existingPtDb.SaveChanges();
                    MessageFunctions.SuccessMessage("New project team member added successfully.", "Team Member Added");
                    return thisTeam.ID;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error creating new project team member", generalException);
                return 0;
            }
        }

    } // class
} // namespace
