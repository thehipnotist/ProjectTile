﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;

namespace ProjectTile
{
    public class ProjectFunctions : Globals
    {
        // ---------------------------------------------------------- //
        // -------------------- Global Variables -------------------- //
        // ---------------------------------------------------------- //

        public static ProjectProxy SelectedTeamProject = null;
        public delegate void ReturnToTeamsDelegate();
        public static ReturnToTeamsDelegate SelectProjectForTeam;
        public static ReturnToTeamsDelegate CancelTeamProjectSelection;
        public static Dictionary<int, string> ActionStatusOptions = new Dictionary<int, string>();
        public static int ActionCounter = 0;
        public delegate void ActionsChangedDelegate();
        public static ActionsChangedDelegate ActionsChanged;

        // ------------------ Lists ----------------- //

        public static List<ProjectProxy> FullProjectList;
        public static List<ProjectProxy> ProjectGridList;
        public static List<ProjectProxy> ProjectFilterList;
        public static List<string> StatusFilterList;
        public static List<ProjectStages> FullStageList;
        public static List<ProjectTypes> FullTypeList;
        public static List<StaffProxy> FullPMsList;
        public static List<StaffProxy> PMFilterList;
        public static List<StaffProxy> PMOptionsList;
        public static List<TeamProxy> FullTeamsList;
        public static List<TeamProxy> TeamsGridList;
        public static List<TeamProxy> LoggedByList = null;
        public static List<ProjectRoles> FullRolesList;
        public static List<ProjectRoles> RolesFilterList;
        public static List<ClientProxy> FullClientList;
        public static List<ClientProxy> ClientFilterList;
        public static List<ClientProxy> ClientOptionsList;
        public static List<ProjectContactProxy> FullContactsList;
        public static List<ProjectContactProxy> ContactsGridList;
        public static List<ClientTeamRoles> FullClientRolesList;
        public static List<ClientTeamRoles> ClientRolesFilterList;

        public static List<Projects> ProjectsNotForProduct;
        public static List<ProjectProductProxy> ProjectsForProduct;
        public static List<int> ProjectIDsToAdd = new List<int>();
        public static List<int> ProjectIDsToRemove = new List<int>();
        public static List<int> ProjectIDsToUpdate = new List<int>();

        public static List<ClientProductProxy> ProductsNotForProject;
        public static List<ProjectProductProxy> ProductsForProject;
        public static List<int> ProductIDsToAdd = new List<int>();
        public static List<int> ProductIDsToRemove = new List<int>();
        public static List<int> ProductIDsToUpdate = new List<int>();

        public static List<int> StageDatesChanged = new List<int>();
        public static List<ActionProxy> ActionList = new List<ActionProxy>();
        public static List<CombinedTeamMember> OwnerList = null;

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

        public static void ReturnToStaffPage(string pageMode, int selectedStaffID = 0)
        {
            ResetProjectParameters();
            PageFunctions.ShowStaffPage(pageMode, selectedStaffID);
        }

        public static void ReturnToProjectPage()
        {
            PageFunctions.ShowProjectPage(ProjectSourceMode, ProjectSourcePage);
        }

        public static void ReturnToStageHistoryPage()
        {
            PageFunctions.ShowStageHistoryPage(ProjectSourceMode);
        }

        public static void ReturnToSourcePage(string pageMode, int selectedID = 0)
        {
            switch(ProjectSourcePage)
            {
                case "TilesPage":
                    ReturnToTilesPage();
                    break;
                case "StaffPage":
                    ReturnToStaffPage(pageMode, selectedID);
                    break;
                case "ClientPage":
                    ReturnToClientPage(pageMode);
                    break;
                case "ProjectPage":
                    ReturnToProjectPage();
                    break;
                case "StageHistoryPage":
                    ReturnToStageHistoryPage();
                    break;
                default:
                    ReturnToProjectPage();
                    break;
            }
        }

        public static void SelectTeamProject(ProjectProxy selectedRecord)
        {
            try
            {
                SelectedTeamProject = selectedRecord;
                SelectProjectForTeam();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error handling project selection", generalException); }
        }

        // --------------- Page setup --------------- //
        
        public static Visibility BackButtonVisibility(string thisPage = "")
        {
            return (ProjectSourcePage != TilesPageName && ProjectSourcePage != thisPage)? Visibility.Visible : Visibility.Hidden;
        }

        public static string BackButtonTooltip()
        {
            switch (ProjectSourcePage)
            {
                case "TilesPage":
                    return "Return to Main Menu";
                case "StaffPage":
                    return "Return to staff list";
                case "ClientPage":
                    return "Return to clients list";
                case "ProjectPage":
                    return "Return to projects list";
                default:
                    return "Return to projects list";
            }
        }

        public static void ToggleFavouriteButton(bool enableIfMatch)
        {
            PageFunctions.ToggleFavouriteButton(enableIfMatch && SelectedProjectProxy.ProjectID != FavouriteProjectID);
        }

        // ---------------------------------------------------------- //
        // -------------------- Data Management --------------------- //
        // ---------------------------------------------------------- //  

        // ------------- Data retrieval ------------- //

        public static bool SetFullProjectList()
        {
            try
            {
                List<ProjectProxy> projectList = null;
                
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    projectList = (from pj in existingPtDb.Projects
                                   join ps in existingPtDb.ProjectStages on pj.StageID equals ps.ID
                                   join t in existingPtDb.ProjectTypes on pj.TypeCode equals t.TypeCode
                                   where pj.EntityID == CurrentEntityID
                                   orderby (new { ps.StageNumber, pj.StartDate })
                                   select new ProjectProxy
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

                foreach (ProjectProxy thisProject in projectList)
                {
                    thisProject.Client = GetProjectClientProxy(thisProject.ProjectID);
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
            int stageNumber = stageRecord.StageNumber;
            string status = stageRecord.ProjectStatus;
            
            return ( inStatus == ProjectStatusFilter.All
                || (inStatus == ProjectStatusFilter.Current && stageNumber < CompletedStage)
                || (inStatus == ProjectStatusFilter.Open && stageNumber >= StartStage && stageNumber < CompletedStage)
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

        public static void SetProjectFilterList(ProjectStatusFilter inStatus, bool includeInternals, int clientID = 0, bool includeSearch = true)
        {
            try
            {
                bool success = SetFullProjectList();
                if (success)
                {
                    ProjectFilterList =
                        (from fpl in FullProjectList
                         where IsInFilter(inStatus, fpl.Stage)
                            && ((clientID == 0 && (includeInternals || (fpl.Client != null && fpl.Client.ID > 0))
                                || (clientID == -1 && includeInternals && (fpl.Client == null || fpl.Client.ID <= 0))
                                || (clientID > 0 && fpl.Client != null && fpl.Client.ID == clientID)))
                         orderby fpl.ProjectCode                      
                         select fpl
                        ).ToList();
                    if (includeSearch) { ProjectFilterList.Insert(0, SearchProjects); }
                    ProjectFilterList.Add(AllProjects);
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving project drop-down data", generalException); }
        }

        public static bool PopulateFromFilters(ref ProjectProxy thisProxy)
        {
            try
            {
                string question = "";
                StaffProxy manager = SelectedPMProxy;
                ClientProxy client = SelectedClientProxy;

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

                if (useClient) { thisProxy.Client = client; }
                if (useManager) { thisProxy.ProjectManager = manager; }
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

        public static ProjectProxy GetProjectProxy(int projectID)
        {
            try
            {
                bool success = SetFullProjectList();
                if (success)
                {
                    return FullProjectList.Where(fpl => fpl.ProjectID == projectID).FirstOrDefault();
                }
                else
                {
                    MessageFunctions.Error("Error retrieving project summary data: could not retrieve projects list.", null);
                    return null;
                }
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error retrieving project summary data", generalException);
                return null;
            }
        }

        public static Entities ProjectEntity(int projectID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return (from p in existingPtDb.Projects
                            join e in existingPtDb.Entities on p.EntityID equals e.ID
                            where p.ID == projectID
                            select e)
                            .FirstOrDefault();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error checking the Entity for project ID " + projectID, generalException);
                return null;
            }	
        }

        public static void OpenFavourite()
        {
            if (FavouriteProjectID == 0)
            {
                MessageFunctions.Error("Error opening favourate project: no favourite set.", null);
                return;
            }
            
            Entities requiredEntity = ProjectEntity(FavouriteProjectID);            
            if (requiredEntity.ID != CurrentEntityID)
            {
                bool switchEntity = MessageFunctions.ConfirmOKCancel("This project is in Entity '" + requiredEntity.EntityName + "', which is not the current Entity. Switch to " 
                    + requiredEntity.EntityName + "?", "Change Entity?");
                if (switchEntity) { EntityFunctions.SwitchEntity(ref requiredEntity); }
                else { return; }
            }

            SelectedProjectProxy = GetProjectProxy(FavouriteProjectID);
            PageFunctions.ShowProjectPage(PageFunctions.Amend);
        }

        public static bool SetFavourite()
        {
            if (SelectedProjectProxy == null)
            {
                MessageFunctions.Error("Error setting main project: no project selected.", null);
                return false;
            }
            else 
            {
                try
                {
                    ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                    using (existingPtDb)
                    {
                        Staff currentStaffMember = existingPtDb.Staff.FirstOrDefault(s => s.ID == MyStaffID);
                        if (currentStaffMember != null)
                        {
                            currentStaffMember.MainProject = SelectedProjectProxy.ProjectID;
                            existingPtDb.SaveChanges();
                        }
                        else
                        {
                            MessageFunctions.Error("Error setting main project: staff record not found.", null);
                            return false;
                        }
                    }
                    FavouriteProjectID = SelectedProjectProxy.ProjectID;
                    MessageFunctions.SuccessAlert("Project " + SelectedProjectProxy.ProjectCode + " has been set as your main project.", "Main Project selected");
                    return true;
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error saving main project selection to the database", generalException);
                    return false;
                }
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
                                     orderby ps.StageNumber
                                     select (ProjectStages)ps).ToList();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving list of project stages", generalException); }
        }

        public static List<ProjectStages> StageFilterList()
        {
            SetFullStageList();
            List<ProjectStages> returnList = FullStageList;
            returnList.Add(AllStages);
            return returnList;
        }

        public static ProjectStages GetStageByNumber(int stageNumber)
        {
            try
            {
                SetFullStageList();
                ProjectStages thisStage = FullStageList.FirstOrDefault(tl => tl.StageNumber == stageNumber);
                if (thisStage != null) { return thisStage; }
                else 
                { 
                    MessageFunctions.Error("Error retrieving project stage with number " + stageNumber.ToString() + ": no matching stage exists.", null);
                    return null;
                }
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error retrieving project stage with number " + stageNumber.ToString(), generalException);
                return null;
            }
        }

        public static ProjectStages GetStageByID(int stageID)
        {
            try
            {
                SetFullStageList();
                ProjectStages thisStage = FullStageList.FirstOrDefault(tl => tl.ID == stageID);
                if (thisStage != null) { return thisStage; }
                else
                {
                    MessageFunctions.Error("Error retrieving project stage with ID " + stageID.ToString() + ": no matching stage exists.", null);
                    return null;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving project stage with ID " + stageID.ToString(), generalException);
                return null;
            }
        }

        public static int GetStageNumber(int stageID)
        {
            try
            {
                ProjectStages thisStage = GetStageByID(stageID);
                if (thisStage != null) { return thisStage.StageNumber; }
                else
                {
                    MessageFunctions.Error("Error retrieving project stage number with ID " + stageID.ToString() + ": no matching stage exists.", null);
                    return 0;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving project stage with ID " + stageID.ToString(), generalException);
                return 0;
            }
        }

        public static bool IsLastStage(int stageNumber)
        {
            try
            {
                if (stageNumber == CancelledStage) { return true; }
                
                SetFullStageList();
                int lastStageNumber = FullStageList.Where(tl => tl.StageNumber != CancelledStage).OrderByDescending(fsl => fsl.StageNumber).FirstOrDefault().StageNumber;
                if (lastStageNumber > 0) { return (stageNumber == lastStageNumber); }
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

        public static ProjectStages ProjectCurrentStage(int projectID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    int stageID = existingPtDb.Projects.Where(p => p.ID == projectID).Select(p => p.StageID).FirstOrDefault();        
                    return GetStageByID(stageID);
                }                
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving current stage for project ID " + projectID.ToString(), generalException);
                return null;
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

        public static ProjectTypes GetProjectType(string typeCode)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return existingPtDb.ProjectTypes.FirstOrDefault(pt => pt.TypeCode == typeCode);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving project type matching code '" + typeCode + "'", generalException);
                return null;
            }
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
            List<StaffProxy> managersList;
            
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
                         select new StaffProxy
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
                List<StaffProxy> comboList = FullPMsList;
                comboList.Add(AllPMs);
                PMFilterList = comboList;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving data for Project Manager filter", generalException); }
        }

        public static void SetPMOptionsList(bool anyActiveUser, int currentManagerID = 0)
        {
            try
            {
                List<StaffProxy> managerList = null;
                if (anyActiveUser) { managerList = StaffFunctions.GetStaffList(activeOnly: true, nameContains: "", roleCode: "", entityID: CurrentEntityID); }
                else
                {
                    SetFullPMsList();
                    managerList = FullPMsList.Where(fpl => fpl.Active || fpl.ID == currentManagerID).ToList();
                }
                if (currentManagerID > 0  && !managerList.Exists(pol => pol.ID == currentManagerID))
                {                    
                    StaffProxy thisManager = StaffFunctions.GetStaffProxy(currentManagerID);
                    managerList.Add(thisManager);                    
                }
                PMOptionsList = managerList.OrderBy(pol => pol.StaffName).ToList();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving data for Project Managers drop-down list", generalException); }
        }

        public static StaffProxy GetPMInOptionsList(int managerID)
        {
            try
            {
                StaffProxy thisPM = PMOptionsList.Where(pol => pol.ID == managerID).FirstOrDefault();
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

        public static StaffProxy GetCurrentPM(int projectID)
        {
            try
            {
                ProjectTeams currentPMRecord = null;
                StaffProxy currentPM = null;
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

                currentPM = StaffFunctions.GetStaffProxy(currentPMRecord.StaffID);
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
        public static ProjectRoles GetInternalRole(string roleCode)
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

        public static void SetRolesFilterList(string nameLike = "", bool exactName = false)
        {
            try
            {
                SetFullRolesList();
                RolesFilterList = FullRolesList.Where(frl => nameLike == "" || HasEverHadRole(frl.RoleCode, nameLike, exactName)).ToList();
                RolesFilterList.Add(AllProjectRoles);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error setting list of possible roles", generalException); }
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
                                                        || (exact && (s.FullName).ToUpper() == nameLike)
                                                        || (!exact && (s.FullName).ToUpper().Contains(nameLike)))
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
                                     where pj.EntityID == CurrentEntityID
                                     select new TeamProxy
                                     {
                                         ID = pt.ID,
                                         Project = pj,
                                         StaffMember = new StaffProxy
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
                TeamsGridList = FullTeamsList.Where(ftl => ((projectID == 0 && IsInFilter(inStatus, ftl.Stage)) 
                                                    || (projectID != 0 && ftl.Project.ID == projectID ))
                                                && (nameContains == "" 
                                                    || (exact && ftl.StaffMember.StaffName.ToUpper() == nameContains) 
                                                    || (!exact && ftl.StaffMember.StaffName.ToUpper().Contains(nameContains)))
                                                && (teamRoleCode == AllCodes 
                                                    || ftl.ProjectRole.RoleCode == teamRoleCode)
                                                && (timeFilter == TeamTimeFilter.All 
                                                    || IsInTimeFilter(timeFilter, ftl.FromDate, ftl.ToDate))
                                                ).ToList();

                TeamsGridList = TeamsGridList.OrderBy(tgl => tgl.EffectiveFrom).OrderBy(tgl => RolePosition(tgl.ProjectRole.RoleCode)).OrderBy(tgl => tgl.Project.ID).ToList();
                return true;
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error retrieving project team members to display", generalException);
                return false;
            }
        }

        public static TeamProxy GetTeamMember(int projectTeamID)
        {
            SetFullTeamsList();
            return FullTeamsList.FirstOrDefault(ftl => ftl.ID == projectTeamID);
        }

        public static List<TeamProxy> GetInternalTeam(int projectID)
        {
            try
            {
                SetFullTeamsList();
                return FullTeamsList.Where(ftl => ftl.Project.ID == projectID).OrderBy(ftl => ftl.StaffName).ToList();                
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving list of internal project team members", generalException);
                return null;
            }
        }

        public static bool IsInTimeFilter(TeamTimeFilter timeFilter, DateTime? fromDate, DateTime? toDate)
        {
            if (timeFilter == TeamTimeFilter.All) { return true; }
            else if (toDate != null && toDate < Today) { return false; }
            else if (timeFilter == TeamTimeFilter.Current && fromDate != null && fromDate > Today) { return false; }
            else { return true; };
        }

        public static bool IsCurrentRole(TeamProxy teamMembership)
        {
            return IsInTimeFilter(TeamTimeFilter.Current, teamMembership.FromDate, teamMembership.ToDate);
        }

        public static bool? DuplicateTeamMember(TeamProxy thisRecord, bool byRole)
        {
            try
            {
                SetFullTeamsList();
                List<TeamProxy> otherInstances = FullTeamsList
                    .Where(ftl => ftl.ID != thisRecord.ID
                        && ftl.Project.ID == thisRecord.Project.ID
                        && ((byRole && ftl.RoleCode == thisRecord.RoleCode) || (!byRole && ftl.StaffID == thisRecord.StaffID)))
                    .ToList();

                if (otherInstances.Count == 0) { return false; }
                else
                {
                    foreach (TeamProxy thisInstance in otherInstances)
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
                MessageFunctions.Error("Error checking for duplicate or overlapping project team records", generalException);
                return null;
            }
        }

        public static bool SubsumesStaff(TeamProxy thisRecord)
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
                MessageFunctions.Error("Error checking for concurrent project team records", generalException);
                return false;
            }
        }

        public static ProjectTeams GetStaffPredecessor(TeamProxy currentRecord)
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

        public static ProjectTeams GetStaffSuccessor(TeamProxy currentRecord)
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
                List<ClientProxy> comboList = FullClientList;
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
                List<ClientProxy> comboList = FullClientList.Where(fcl => fcl.ActiveClient || fcl.ID == currentClientID).ToList();
                comboList.Insert(0, NoClient);
                ClientOptionsList = comboList;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving data for client drop-down list", generalException); }
        }

        public static ClientProxy GetClientInOptionsList(int clientID)
        {
            try
            {
                ClientProxy thisClient = ClientOptionsList.Where(col => col.ID == clientID).FirstOrDefault();
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

        public static ClientProxy GetProjectClientProxy(int projectID)
        {
            ClientProxy noClient = new ClientProxy { ID = NoID, ClientCode = "", ClientName = "", EntityID = CurrentEntityID };
            
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
                    else { return ClientFunctions.GetClientProxy((int) clientID); }
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving project client details for project ID " + projectID.ToString(), generalException);
                return noClient;
            }	
        }

        // Client teams (linked contacts)

        public static ClientTeamRoles GetClientRole(string roleCode)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return existingPtDb.ClientTeamRoles.Where(pr => pr.RoleCode == roleCode).FirstOrDefault();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving client role", generalException);
                return null;
            }
        }

        public static void SetFullClientRolesList()
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    List<ClientTeamRoles> rolesList = existingPtDb.ClientTeamRoles.ToList();
                    FullClientRolesList = rolesList.OrderBy(rl => RolePosition(rl.RoleCode)).ToList();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error listing client roles", generalException); }
        }

        public static void SetClientRolesFilterList(int clientID, string nameLike = "", bool exactName = false)
        {
            try
            {
                SetFullClientRolesList();
                ClientRolesFilterList = FullClientRolesList.Where(frl => nameLike == "" || HasEverHadClientRole(frl.RoleCode, nameLike, exactName, clientID)).ToList();
                ClientRolesFilterList.Add(AllClientRoles);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error setting list of possible client roles", generalException); }
        }

        public static bool HasEverHadClientRole(string clientRoleCode, string nameLike, bool exact, int clientID)
        {
            try
            {
                nameLike = nameLike.ToUpper();
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    ClientTeams firstResult = (from ct in existingPtDb.ClientTeams
                                               join cs in existingPtDb.ClientStaff on ct.ClientStaffID equals cs.ID
                                               where ct.ClientTeamRoleCode == clientRoleCode
                                               && (clientID == 0 || cs.ClientID == clientID)    
                                               && (nameLike == ""
                                                       || (exact && (cs.FullName).ToUpper() == nameLike)
                                                       || (!exact && (cs.FullName).ToUpper().Contains(nameLike)))                                                   
                                               select ct
                                                ).FirstOrDefault();
                    return (firstResult != null);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error checking client role history for the name filter", generalException);
                return false;
            }
        }

        public static void SetFullContactsList()
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    FullContactsList = (from ct in existingPtDb.ClientTeams
                                           join pj in existingPtDb.Projects on ct.ProjectID equals pj.ID
                                           join cs in existingPtDb.ClientStaff on ct.ClientStaffID equals cs.ID
                                           join ctr in existingPtDb.ClientTeamRoles on ct.ClientTeamRoleCode equals ctr.RoleCode
                                           where pj.EntityID == CurrentEntityID
                                           select new ProjectContactProxy
                                           {
                                               ID = ct.ID,
                                               Project = pj,
                                               Contact = new ContactProxy
                                               {
                                                   ID = cs.ID,
                                                   ContactName = cs.FullName,
                                                   JobTitle = cs.JobTitle,
                                                   PhoneNumber = cs.PhoneNumber,
                                                   Email = cs.Email,
                                                   Active = cs.Active
                                               },
                                               TeamRole = ctr,
                                               FromDate = ct.FromDate,
                                               ToDate = ct.ToDate
                                           }
                                     ).ToList();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving full list of client team members", generalException); }
        }

        public static ProjectContactProxy GetProjectContact(int projectContactID)
        {
            SetFullContactsList();
            return FullContactsList.FirstOrDefault(fcl => fcl.ID == projectContactID);
        }
        
        public static List<Projects> projectsRequiringContacts()
        {
            try
            {
                List<Projects> returnProjects = new List<Projects>();
                
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    List<Projects> nonInternalProjects = existingPtDb.Projects.Where(p => p.ClientID != null && p.ClientID > 0 && p.EntityID == CurrentEntityID).ToList();
                    foreach (Projects p in nonInternalProjects)
                    {
                        if (existingPtDb.ClientTeams.FirstOrDefault(ct => ct.ProjectID == p.ID) == null) { returnProjects.Add(p); }
                    }
                }
                return returnProjects;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error finding projects that have clients but no linked contacts", generalException);
                return null;
            }		
        }

        public static ProjectContactProxy DummyContact(Projects thisProject)
        {
            try
            {
                return new ProjectContactProxy
                {
                    ID = NoID,
                    Project = thisProject,
                    Contact = new ContactProxy
                    {
                        ID = NoID,
                        ContactName = "<None Added Yet>",
                        JobTitle = "",
                        PhoneNumber = "",
                        Email = "",
                        Active = false
                    },
                    TeamRole = null,
                    FromDate = null,
                    ToDate = null
                };
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error creating dummy contact for project " + thisProject.ProjectCode, generalException);
                return null;
            }	
        }
        
        public static bool SetContactsGridList(ProjectStatusFilter inStatus, string roleCode, TeamTimeFilter timeFilter, int clientID = 0, int projectID = 0, 
            string nameContains = "", bool exact = false)
        {
            try
            {
                nameContains = nameContains.ToUpper();
                SetFullContactsList();
                ContactsGridList = FullContactsList.Where(ftl => ((projectID == 0 && IsInFilter(inStatus, ftl.Stage))
                                                    || (projectID != 0 && ftl.Project.ID == projectID))
                                                && (clientID == 0 
                                                    || ftl.ClientID == clientID )
                                                && (nameContains == ""
                                                    || (exact && ftl.Contact.ContactName.ToUpper() == nameContains)
                                                    || (!exact && ftl.Contact.ContactName.ToUpper().Contains(nameContains)))
                                                && (roleCode == AllCodes
                                                    || ftl.TeamRole.RoleCode == roleCode)
                                                && (timeFilter == TeamTimeFilter.All
                                                    || IsInTimeFilter(timeFilter, ftl.FromDate, ftl.ToDate))
                                                ).ToList();

                if (roleCode == AllCodes && nameContains == "" && projectID == 0) // When not filtered, need an empty record so we can select the project
                {
                    List<Projects> additionalProjects = projectsRequiringContacts();
                    foreach (Projects p in additionalProjects)
                    {
                        ProjectStages stage = GetStageByID(p.StageID);
                        if (IsInFilter(inStatus, stage))
                        {
                            ProjectContactProxy dummy = DummyContact(p);
                            ContactsGridList.Add(dummy);
                        }
                    }
                }

                ContactsGridList = ContactsGridList.OrderBy(pcl => pcl.EffectiveFrom).OrderBy(pcl => RolePosition(pcl.RoleCode)).OrderBy(pcl => pcl.Project.ID).ToList();
                return true;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving client team members to display", generalException);
                return false;
            }
        }

        public static bool? DuplicateProjectContact(ProjectContactProxy thisRecord, bool byRole)
        {
            try
            {
                SetFullContactsList();
                List<ProjectContactProxy> otherInstances = FullContactsList
                    .Where(ftl => ftl.ID != thisRecord.ID
                        && ftl.Project.ID == thisRecord.Project.ID
                        && ((byRole && ftl.RoleCode == thisRecord.RoleCode) || (!byRole && ftl.ContactID == thisRecord.ContactID)))
                    .ToList();

                if (otherInstances.Count == 0) { return false; }
                else
                {
                    foreach (ProjectContactProxy thisInstance in otherInstances)
                    {
                        if (thisInstance.RoleCode == thisRecord.RoleCode) // Required if by contact, as returns null if found (but only) with a different role
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
                MessageFunctions.Error("Error checking for duplicate or overlapping client team records", generalException);
                return null;
            }
        }

        public static bool SubsumesContact(ProjectContactProxy thisRecord)
        {
            try
            {
                SetFullContactsList();
                return FullContactsList.Exists(ftl => ftl.ID != thisRecord.ID
                        && ftl.Project.ID == thisRecord.Project.ID
                        && ftl.RoleCode == thisRecord.RoleCode
                        && ((ftl.FromDate != null && ftl.FromDate >= thisRecord.EffectiveFrom) ||
                            ((ftl.FromDate == null || ftl.FromDate == thisRecord.Project.StartDate) && thisRecord.EffectiveFrom == thisRecord.Project.StartDate))
                        && ((ftl.ToDate != null && ftl.ToDate <= thisRecord.EffectiveTo) || (ftl.ToDate == null && thisRecord.ToDate == null)));
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error checking for concurrent client team records", generalException);
                return false;
            }
        }

        public static ClientTeams GetContactPredecessor(ProjectContactProxy currentRecord)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return existingPtDb.ClientTeams
                        .Where(ct => ct.ID != currentRecord.ID
                            && ct.ProjectID == currentRecord.Project.ID
                            && ct.ClientTeamRoleCode == currentRecord.RoleCode
                            && (ct.FromDate == null || ct.FromDate == currentRecord.Project.StartDate || ct.FromDate < currentRecord.EffectiveFrom))
                        .OrderByDescending(ct => ct.ToDate ?? InfiniteDate)
                        .OrderByDescending(ct => ct.FromDate ?? StartOfTime)
                        .FirstOrDefault();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving client predecessor information", generalException);
                return null;
            }
        }

        public static ClientTeams GetContactSuccessor(ProjectContactProxy currentRecord)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return existingPtDb.ClientTeams
                        .Where(ct => ct.ID != currentRecord.ID
                            && ct.ProjectID == currentRecord.Project.ID
                            && ct.ClientTeamRoleCode == currentRecord.RoleCode
                            && (ct.ToDate == null || ct.ToDate > currentRecord.EffectiveTo))
                        .OrderBy(ct => ct.FromDate ?? StartOfTime)
                        .OrderBy(ct => ct.ToDate ?? InfiniteDate)
                        .FirstOrDefault();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving client successor information", generalException);
                return null;
            }
        }

        public static List<ProjectContactProxy> GetClientTeam(int projectID)
        {
            SetFullContactsList();
            return FullContactsList.Where(fcl => fcl.Project.ID == projectID).ToList();
        }

        // Combined teams
        public static List<CombinedStaffMember> CombinedStaffList(string nameLike, int clientID, int projectID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    List<CombinedStaffMember> combinedList = new List<CombinedStaffMember>();
                    List<int> projectStaffIDs = null;
                    List<int> projectContactIDs = null;
                    if (projectID > 0)
                    {
                        projectStaffIDs = existingPtDb.ProjectTeams.Where(pt => pt.ProjectID == projectID).Select(pt => pt.StaffID).ToList();
                        projectContactIDs = existingPtDb.ClientTeams.Where(ct => ct.ProjectID == projectID).Select(ct => ct.ClientStaffID).ToList();
                    }

                    List<StaffProxy> allStaff = StaffFunctions.GetStaffList(activeOnly: false, nameContains: nameLike, roleCode: "", entityID: CurrentEntityID);
                    List<StaffProxy> relevantStaff = allStaff.Where(s => projectID <= 0 || projectStaffIDs.Contains(s.ID)).ToList();
                    foreach (StaffProxy staffMember in relevantStaff)
                    {
                        CombinedStaffMember person = new CombinedStaffMember { StaffMember = staffMember, ClientContact = null };
                        combinedList.Add(person);
                    }

                    List<ContactProxy> allContacts = ClientFunctions.ContactGridList(contactContains: nameLike, activeOnly: false, clientID: clientID, includeJob: false);
                    List<ContactProxy> relevantContacts = allContacts.Where(c => projectID <= 0 || projectContactIDs.Contains(c.ID)).ToList();
                    foreach (ContactProxy contact in relevantContacts)
                    {
                        CombinedStaffMember person = new CombinedStaffMember { StaffMember = null, ClientContact = contact };
                        combinedList.Add(person);
                    }

                    return combinedList;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving list of combined staff members", generalException);
                return null;
            }
        }

        public static CombinedStaffMember GetCombinedStaffMember(int? staffID, int? contactID)
        {
            try
            {
                List<CombinedStaffMember> allStaffMembers = CombinedStaffList("", 0, 0);
                if (staffID > 0) { return allStaffMembers.FirstOrDefault(asm => asm.StaffMember.ID == staffID); }
                else if (contactID > 0) { return allStaffMembers.FirstOrDefault(asm => asm.ClientContact.ID == contactID); }
                else
                {
                    MessageFunctions.Error("Error retrieving combined staff member: no internal or client staff ID provided.", null);
                    return null;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving combined staff member", generalException);
                return null;
            }
        }

        public static List<CombinedTeamMember> CombinedTeamList(int projectID)
        {
            try
            {
                List<CombinedTeamMember> combinedList = new List<CombinedTeamMember>();                
                List<TeamProxy> internalTeam = GetInternalTeam(projectID);
                List<ProjectContactProxy> clientTeam = GetClientTeam(projectID);

                foreach (TeamProxy teamMember in internalTeam)
                {
                    CombinedTeamMember person = new CombinedTeamMember { ProjectID = projectID, InternalTeamMember = teamMember, ClientTeamMember = null };
                    combinedList.Add(person);
                }

                foreach (ProjectContactProxy contact in clientTeam)
                {
                    CombinedTeamMember person = new CombinedTeamMember { ProjectID = projectID, InternalTeamMember = null, ClientTeamMember = contact };
                    combinedList.Add(person);
                }

                return combinedList;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving list of combined project team members", generalException);
                return null;
            }
        }

        // Products
        public static List<ProjectProxy> ProjectGridListByProduct(bool activeOnly, string nameContains, int productID, int entityID) 
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    List<int> projectIDList =
                        (from pj in existingPtDb.Projects
                                join ps in existingPtDb.ProjectStages on pj.StageID equals ps.ID
                                join pp in existingPtDb.ProjectProducts on pj.ID equals pp.ProjectID
                                    into GroupJoin from spp in GroupJoin.DefaultIfEmpty()
                                //join pd in existingPtDb.Products on pp.ProductID equals pd.ID
                            where (pj.EntityID == entityID
                                && (!activeOnly || ps.StageNumber < LiveStage)
                                && (nameContains == "" || pj.ProjectName.Contains(nameContains))
                                && (productID == 0 || spp.ProductID == productID))
                            orderby (new { ps.StageNumber, pj.StartDate })
                            select pj.ID).ToList();

                    SetFullProjectList();
                    return FullProjectList.Where(fpl => projectIDList.Contains(fpl.ProjectID)).ToList();
				}
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving list of projects by product", generalException);
                return null;
            }		
        }

        public static List<ProjectProductProxy> ProjectsWithProduct(bool activeOnly, int productID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    List<ProjectProductProxy> projectProducts =
                        (from pd in existingPtDb.Products
                         join pp in existingPtDb.ProjectProducts on pd.ID equals pp.ProductID
                         join pj in existingPtDb.Projects on pp.ProjectID equals pj.ID
                         join ps in existingPtDb.ProjectStages on pj.StageID equals ps.ID
                         where (productID == 0 || (productID > 0 && pd.ID == productID)) 
                            && (!activeOnly || ps.StageNumber < LiveStage) 
                            && pj.EntityID == CurrentEntityID
                         orderby pj.ProjectName
                         select new ProjectProductProxy
                         {
                            ID = pp.ID,
                            Project = pj,
                            Product = pd,
                            OldVersion = (pp.OldVersion == null)? 0 : (decimal) pp.OldVersion,
                            NewVersion = pp.NewVersion,
                            JustAdded = false
                         }
                        ).ToList();

                    return projectProducts;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error listing projects with product ID " + productID.ToString(), generalException);
                return null;
            }
        }

        public static List<Projects> ProjectsWithoutProduct(bool activeOnly, int productID)
        {
            try
            {
                List<int> projectIDsWithProduct = ProjectsWithProduct(false, productID).Select(pwp => (int)pwp.ProjectID).ToList();
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return (from pj in existingPtDb.Projects
                            join ps in existingPtDb.ProjectStages on pj.StageID equals ps.ID
                            join cp in existingPtDb.ClientProducts on pj.ClientID equals cp.ClientID
                                into GroupJoin from scp in GroupJoin.DefaultIfEmpty()
                            where (!activeOnly || ps.StageNumber < LiveStage) 
                                && !projectIDsWithProduct.Contains(pj.ID) && pj.EntityID == CurrentEntityID
                                && (pj.ClientID == null || pj.ClientID <= 0 || pj.ClientID == scp.ClientID)
                                && (scp == null || scp.ProductID == productID)
                            orderby pj.ProjectCode
                            select pj).Distinct().ToList();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error listing projects without product ID " + productID.ToString(), generalException);
                return null;
            }
        }

        public static List<ProjectProductProxy> LinkedProducts(int projectID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    List<ProjectProductProxy> projectProducts =
                        (from pd in existingPtDb.Products
                         join pp in existingPtDb.ProjectProducts on pd.ID equals pp.ProductID
                         join pj in existingPtDb.Projects on pp.ProjectID equals pj.ID
                         where pj.ID == projectID
                         orderby pd.ProductName
                         select new ProjectProductProxy
                         {
                            ID = pp.ID,
                            Project = pj,
                            Product = pd,
                            OldVersion = (pp.OldVersion == null)? 0 : (decimal) pp.OldVersion,
                            NewVersion = pp.NewVersion,
                            JustAdded = false
                         }
                        ).ToList();

                    return projectProducts;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error listing products for project ID " + projectID.ToString(), generalException);
                return null;
            }
        }

        public static ProjectProductProxy GetProjectProduct(int projectProductID)
        {
            return ProjectsWithProduct(false, 0).FirstOrDefault(pwp => pwp.ID == projectProductID);
        }

        public static ClientProductProxy DummyClientProduct(Products thisProduct) // For internal projects, where there is no client
        {
            return new ClientProductProxy
            {
                ID = 0,
                ClientID = 0,
                ClientName = "",
                ClientEntityID = CurrentEntityID,
                ActiveClient = true,
                ProductID = thisProduct.ID,
                ProductName = thisProduct.ProductName,
                LatestVersion = Math.Floor((decimal)thisProduct.LatestVersion * 10) / 10,
                Live = true,
                StatusID = ClientProductStatus.Live,
                ClientVersion = Math.Floor((decimal)thisProduct.LatestVersion * 10) / 10
            };
        }
        
        public static List<ClientProductProxy> UnlinkedProducts(int projectID)
        {
            try
            {
                List<int> productIDsForProject = LinkedProducts(projectID).Select(lp => (int)lp.ProductID).ToList();

                int clientID = GetProjectClientProxy(projectID).ID;

                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    if (clientID > 0)
                    {
                        return (from pd in existingPtDb.Products
                                join cp in existingPtDb.ClientProducts on pd.ID equals cp.ProductID
                                join c in existingPtDb.Clients on cp.ClientID equals c.ID
                                where !productIDsForProject.Contains(pd.ID) && c.ID == clientID
                                orderby pd.ProductName
                                select new ClientProductProxy
                                {
                                    ID = cp.ID,
                                    ClientID = c.ID,
                                    ClientName = c.ClientName,
                                    ClientEntityID = c.EntityID,
                                    ActiveClient = c.Active,
                                    ProductID = pd.ID,
                                    ProductName = pd.ProductName,
                                    LatestVersion = Math.Floor((decimal)pd.LatestVersion * 10) / 10,
                                    Live = (bool)cp.Live,
                                    StatusID = (cp.Live == true) ? ClientProductStatus.Live : ClientProductStatus.New,
                                    ClientVersion = Math.Floor((decimal)cp.ProductVersion * 10) / 10
                                }
                                ).ToList();                        
                    }
                    else
                    {
                        List<Products> unlinkedProducts = 
                                (from pd in existingPtDb.Products
                                where !productIDsForProject.Contains(pd.ID)
                                orderby pd.ProductName
                                select pd
                                ).ToList();
                        
                        List<ClientProductProxy> dummyRecords = unlinkedProducts.Select(pd => DummyClientProduct(pd)).ToList();
                        return dummyRecords;
                    }
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error listing products not linked to project ID " + projectID.ToString(), generalException);
                return null;
            }
        }

        // Stage history (timelines)
        public static int MaxNonCancelledStage()
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return existingPtDb.ProjectStages.Where(ps => ps.StageNumber != CancelledStage).Select(ps => ps.StageNumber).Max();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving maximum stage number", generalException);
                return 0;
            }
        }            
        
        public static List<StageHistory> GetProjectStageHistory(int projectID)
        {
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {
                return existingPtDb.StageHistory.Where(sh => sh.ProjectID == projectID).ToList();
            }
        }

        public static List<ProjectStages> GetProjectHistoryStages(int projectID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return (from sh in existingPtDb.StageHistory
                            join ps in existingPtDb.ProjectStages on sh.StageID equals ps.ID
                            where sh.ProjectID == projectID
                            select ps
                            ).ToList();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving stages for project history", generalException);
                return null;
            }	
        }

        public static TimelineProxy GetProjectTimeline(int projectID, TimelineType type)
        {
            ProjectStages stage = null;
            int maxNonCancelledStage = 0;
            List<StageHistory> stageHistory = null;
            try
            {
                stage = ProjectCurrentStage(projectID);
                stageHistory = GetProjectStageHistory(projectID);
                maxNonCancelledStage = MaxNonCancelledStage();

                TimelineProxy timeline = new TimelineProxy();
                timeline.ProjectID = projectID;
                timeline.Stage = stage;
                timeline.TimeType = type;
                for (int i=0; i<=maxNonCancelledStage; i++)
                {
                    ProjectStages thisStage = GetStageByNumber(i);
                    if (thisStage == null) { continue; }
                    StageHistory thisHistory = stageHistory.FirstOrDefault(sh => sh.StageID == thisStage.ID);
                    if (thisHistory == null) { timeline.DateHash.Add(i, null); }
                    else if (type == TimelineType.Target) { timeline.DateHash.Add(i, thisHistory.TargetStart ?? null); }  
                    else if (type == TimelineType.Actual || thisStage.StageNumber < stage.StageNumber) { timeline.DateHash.Add(i, thisHistory.ActualStart ?? null); }
                    else { timeline.DateHash.Add(i, thisHistory.EffectiveStart ?? null); }
                }
                
                ProjectStages cancelledStage = GetStageByNumber(CancelledStage);
                StageHistory cancelledHistory = stageHistory.FirstOrDefault(sh => sh.StageID == cancelledStage.ID);
                if (cancelledHistory == null) { timeline.DateHash.Add(CancelledStage, null); }
                else if (type == TimelineType.Target) { timeline.DateHash.Add(CancelledStage, cancelledHistory.TargetStart ?? null); }
                else if (type == TimelineType.Actual) { timeline.DateHash.Add(CancelledStage, cancelledHistory.ActualStart ?? null); }                                
                else { timeline.DateHash.Add(CancelledStage, cancelledHistory.EffectiveStart ?? null); }

                timeline.StoreOriginalValues(); // shallow copy DateHash to InitialDates for comparison

                return timeline;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving project timeline details", generalException);
                return null;
            }
        }

        public static StageHistory GetHistoryRecord(int historyID) // Overloaded below
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return existingPtDb.StageHistory.FirstOrDefault(sh => sh.ID == historyID);                    
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving stage history details with ID " + historyID.ToString(), generalException);
                return null;
            }
        }

        public static StageHistory GetHistoryRecord(int projectID, int stageNumber) // Overloads above
        {
            try
            {
                int stageID = GetStageByNumber(stageNumber).ID;
                
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return existingPtDb.StageHistory.FirstOrDefault(sh => sh.ProjectID == projectID && sh.StageID == stageID);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving stage history details for project ID " + projectID.ToString() + " and stage number " + stageNumber.ToString(), generalException);
                return null;
            }
        }

        public static DateTime? GetStageStartDate(int projectID, int stageNumber, bool? target)
        {
            try
            {
                ProjectStages thisStage = GetStageByNumber(stageNumber);
                if (thisStage == null)
                {
                    MessageFunctions.Error("Stage number " + stageNumber.ToString() + " not found.", null);
                    return null;
                }
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    StageHistory thisHistory = existingPtDb.StageHistory.FirstOrDefault(sh => sh.ProjectID == projectID && sh.StageID == thisStage.ID);
                    if (thisHistory == null) { return null; }
                    else if (target == true) { return thisHistory.TargetStart; }
                    else if (target == false) { return thisHistory.ActualStart; }
                    else { return thisHistory.EffectiveStart; }
                }                             
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving stage history details for stage " + stageNumber.ToString(), generalException);
                return null;
            }
        }

        public static DateTime? StageEndDate(int projectID, int stageNumber, bool? target = null)
        {
            int nextStage = stageNumber;
            DateTime? nextStageStart = null;
            while (nextStageStart == null && nextStage < CancelledStage)
            {
                nextStage = (nextStage == MaxNonCancelledStage()) ? CancelledStage : nextStage + 1;
                nextStageStart = GetStageStartDate(projectID, nextStage, target);
            }
            if (nextStageStart == null) { return null; }
            else { return ((DateTime)nextStageStart).AddDays(-1); }
        }

        public static List<StageHistoryProxy> StageHistoryList(int clientID, ProjectStatusFilter statusFilter, int projectID, TimelineType timelineType, 
                                                                DateTime fromDate, DateTime toDate, int stageNumber)
        {
            try
            {
                bool actual = (timelineType == TimelineType.Actual);
                bool target = (timelineType == TimelineType.Target);
                bool effective = (timelineType == TimelineType.Effective);

                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    List<StageHistory> initialList;
                    if (projectID > 0) { initialList = existingPtDb.StageHistory.Where(sh => sh.ProjectID == projectID).ToList(); }
                    else
                    {
                        SetProjectFilterList(statusFilter, true, clientID, false);
                        List<int> projectIDs = ProjectFilterList.Select(pfl => pfl.ProjectID).ToList();

                        initialList = (from sh in existingPtDb.StageHistory
                                       where projectIDs.Contains(sh.ProjectID)
                                       select sh)
                                       .ToList();
                    }

                    int stageID = (stageNumber < 0) ? stageNumber : GetStageByNumber(stageNumber).ID;
                    DateTime maxDate = toDate.AddDays(1);
                    List<StageHistory> filteredList = initialList.Where(il => (stageNumber < 0 || il.StageID == stageID) &&
                        (
                            (actual && il.ActualStart != null && il.ActualStart >= fromDate && il.ActualStart < maxDate)
                            || (target && il.TargetStart != null && il.TargetStart >= fromDate && il.TargetStart < maxDate)
                            || (effective && il.EffectiveStart != null && il.EffectiveStart >= fromDate && il.EffectiveStart < maxDate)
                        )
                    ).ToList();

                    List<StageHistoryProxy> returnList = new List<StageHistoryProxy>();
                    foreach (StageHistory sh in filteredList)
                    {
                        Projects thisProject = GetProject(sh.ProjectID);
                        ProjectStages thisStage = GetStageByID(sh.StageID);
                        StageHistoryProxy thisProxy = new StageHistoryProxy
                        {
                            ID = sh.ID,
                            Project = thisProject,
                            Stage = thisStage,
                            Type = timelineType,
                            StartDate = actual? sh.ActualStart : (target? sh.TargetStart : sh.EffectiveStart)
                        };
                        returnList.Add(thisProxy);
                    };

                    return returnList;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving stage history details for the set filters", generalException);
                return null;
            }
        }

        // Actions
        public static void SetActionStatusOptions()
        {
            try
            {
                if (ActionStatusOptions.Count == 0)
                {                    
                    ActionStatusOptions.Add(3, "Yes");                    
                    ActionStatusOptions.Add(2, "Partly");
                    ActionStatusOptions.Add(1, "Not Started");
                    ActionStatusOptions.Add(0, AllRecords);
                    ActionStatusOptions.Add(-1, "No");
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error setting action status options", generalException); }	
        }

        public static List<string> ActionCompletedList(bool includeAll, bool includeNo)
        {
            try
            {
                List<string> optionsList = new List<string>();
                foreach (var entry in ActionStatusOptions.Values)
                {
                    if (!includeAll && entry == AllRecords) {}
                    else if (!includeNo && entry == "No" ) {}
                    else { optionsList.Add(entry.ToString()); };
                }
                return optionsList;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving action status options", generalException);
                return null;
            }	
        }

        public static int GetCompletedKey(string value)
        {
            return ActionStatusOptions.Keys.FirstOrDefault(k => ActionStatusOptions[k] == value);
        }

        public static string GetCompletedDescription(int key)
        {
            return ActionStatusOptions[key];
        }

        public static string ActionCode(int projectID, DateTime date)
        {
            try
            {
                if (date == null || projectID == 0) { return ""; }

                ActionCounter++;               
                string datePart = date.ToString("yyMMdd.");              
                List<ActionProxy> sameDayActions = ActionList.Where(al => 
                        al.Project.ID == projectID 
                        && al.ActionCode.Length >= datePart.Length
                        && al.ActionCode.Substring(0, datePart.Length) == datePart)
                    .OrderBy(al => al.ActionCode).ToList();
                foreach (ActionProxy a in sameDayActions) // Use this instead of a simple count, just in case of any deletions
                {                    
                    string countPart = a.ActionCode.Replace(datePart, "");
                    int count = 0;
                    Int32.TryParse(countPart, out count);
                    if (count == 0) { ActionCounter++; } // Simply increment if couldn't parse the string, just in case
                    else { ActionCounter = Math.Max(ActionCounter, count + 1); }
                }
				if (ActionCounter > 99)
                {
                    MessageFunctions.InvalidMessage("The maximum number of actions logged per project each day is 99, to preserve the ActionCode format. "
                        + "Please cancel your changes and log any further actions on another day.", "Maximum Limit Reached");
                    return "";
                }                
                return datePart + ActionCounter.ToString("D2");
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error setting action code", generalException);
                return "";
            }	            
        }

        public static bool StageFitsDates(int projectID, int stageNumber, DateTime fromDate, DateTime toDate)
        {
            DateTime? stageEnd = StageEndDate(projectID, stageNumber);
            if (stageEnd == null) { return true; } // Shouldn't happen, but if no stage end is set the action should not be lost
            else 
            {
                DateTime deadline = (DateTime)stageEnd;
                return (deadline >= fromDate && deadline <= toDate);
            }
        }

        public static CombinedTeamMember GetOwner(int projectID, int? internalTeamID, int? clientTeamID)
        {
            try
            {
                TeamProxy internalOwner = (internalTeamID != null) ? GetTeamMember((int)internalTeamID) : null;
                ProjectContactProxy clientOwner = (clientTeamID != null) ? GetProjectContact((int)clientTeamID) : null;
                return new CombinedTeamMember { ProjectID = projectID, InternalTeamMember = internalOwner, ClientTeamMember = clientOwner };
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error retrieving action owner", generalException);
                return null;
            }
        }

        public static bool HasProjectActions(TeamProxy teamMember)// Overloaded below
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    Actions firstAction = existingPtDb.Actions.FirstOrDefault(a => a.ProjectID == teamMember.Project.ID && a.InternalOwner == teamMember.ID);
                    return (firstAction != null);
				}
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error checking project actions for team member", generalException);
                return false;
            }	            
        }

        public static bool HasProjectActions(ProjectContactProxy clientTeamMember)// Overloaded above
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    Actions firstAction = existingPtDb.Actions.FirstOrDefault(a => a.ProjectID == clientTeamMember.Project.ID && a.ClientOwner == clientTeamMember.ID);
                    return (firstAction != null);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error checking project actions for client team member", generalException);
                return false;
            }
        }

        public static bool HasLoggedActions(TeamProxy teamMember) 
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    Actions firstAction = existingPtDb.Actions.FirstOrDefault(a => a.ProjectID == teamMember.Project.ID && a.LoggedBy == teamMember.ID);
                    return (firstAction != null);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error checking project actions logged by team member", generalException);
                return false;
            }
        }

        public static bool? GetActionStatusCode(int statusNumber)
        {
            try
            {
                if (statusNumber == -1) { return false; }
                else if (statusNumber == 1) { return null; }
                else if (statusNumber == 2) { return false; }                
                else if (statusNumber == 3) { return true; }
                else { return null; }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error converting action status to boolean", generalException);
                return null;
            }		
        }

        public static int GetActionStatusNumber(bool? statusCode)
        {
            try
            {
                if (statusCode == null) { return 1; }
                else if (statusCode == false) { return 2; }
                else { return 3; }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error converting action status to integer", generalException);
                return 0;
            }		
        }

        public static void SetActionsList(int clientID, ProjectStatusFilter filterCode, int projectID, DateTime fromDate, DateTime toDate, CombinedStaffMember owner, int statusNumber)
        {
            try
            {                
                DateTime maxDate = toDate.AddDays(1);
                bool? statusCode = GetActionStatusCode(statusNumber);                
                
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    var actionsWithStage = (from a in existingPtDb.Actions
                                            join p in existingPtDb.Projects on a.ProjectID equals p.ID
                                            join ps in existingPtDb.ProjectStages on a.StageID equals ps.ID
                                                into GroupJoin from gps in GroupJoin.DefaultIfEmpty()
                                            where (a.ProjectID == projectID 
                                                    || (projectID <= 0 && p.EntityID == CurrentEntityID && (clientID == 0 || (clientID == -1 && p.ClientID == null) || p.ClientID == clientID)))
                                                && (a.TargetCompletion == null || (a.TargetCompletion >= fromDate && a.TargetCompletion < maxDate))
                                                && (statusNumber == 0 || a.StatusCode == statusCode || (statusNumber == -1 && a.StatusCode != true))
                                            select new { Action = a, Stage = gps ?? null, Project  = p }
                                            ).ToList();
                    //if (actionsWithStage.Count == 0) { return null; }

                    if (projectID == 0 && filterCode != ProjectStatusFilter.All)
                    {
                        var filteredActions = actionsWithStage.Where(aws => IsInFilter(filterCode, GetStageByID(aws.Project.StageID)));
                        actionsWithStage = filteredActions.ToList();
                    }
                    if (actionsWithStage.Count > 0 && owner != null)
                    {
                        bool internalOwner = (owner != null && owner.StaffMember != null);
                        List<int> ownerTeamIDs = internalOwner ?
                            existingPtDb.ProjectTeams.Where(pt => pt.StaffID == owner.StaffMember.ID).Select(pt => pt.ID).ToList() :
                            existingPtDb.ClientTeams.Where(ct => ct.ClientStaffID == owner.ClientContact.ID).Select(ct => ct.ID).ToList();

                        var filteredActions = actionsWithStage.Where(aws =>
                            (internalOwner && aws.Action.InternalOwner != null && ownerTeamIDs.Contains((int)aws.Action.InternalOwner))
                            || (!internalOwner && aws.Action.ClientOwner != null && ownerTeamIDs.Contains((int)aws.Action.ClientOwner))
                            ).ToList();
                        actionsWithStage = filteredActions.ToList();
                    }
                    
                    var dateFilteredActions = actionsWithStage.Where(aws =>   aws.Action.TargetCompletion != null // already filtered above
                                                                             || (aws.Stage != null && StageFitsDates(aws.Project.ID, aws.Stage.StageNumber, fromDate, toDate) ));

                    ActionList = dateFilteredActions.Select(dfa => new ActionProxy
                        {
                            ID = dfa.Action.ID,
                            Owner = GetOwner(dfa.Project.ID, dfa.Action.InternalOwner, dfa.Action.ClientOwner),
                            ActionCode = dfa.Action.ActionCode,
                            Project = dfa.Project,
                            LoggedDate = dfa.Action.LoggedDate,
                            TargetCompletion = dfa.Action.TargetCompletion,
                            UpdatedDate = dfa.Action.UpdatedDate,
                            ShortDescription = dfa.Action.ShortDescription,
                            LoggedBy = GetTeamMember((int)dfa.Action.LoggedBy),
                            CompletedNumber = GetActionStatusNumber(dfa.Action.StatusCode),
                            Notes = dfa.Action.Notes,
                            LinkedStage = dfa.Stage ?? null
                        }
                        ).ToList();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving list of project actions", generalException);
                ActionList = null;
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
            try
            {
                string result = "";
                string[] keyRoles = clientTeam ? KeyClientRoles : KeyInternalRoles;
                foreach (string role in keyRoles.Reverse())
                {
                    if (!currentTeam.Contains(role))
                    {
                        string description = clientTeam? GetClientRole(role).RoleDescription : GetInternalRole(role).RoleDescription;
                        if (result == "") { result = description; }
                        else if (!result.Contains("and")) { result = description + " and " + result; }
                        else { result = description + ", " + result; }
                    }
                }
                return result;
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error identifying missing roles in the " + (clientTeam? "client's" : "internal") + " project team", generalException);
                return "[Undetermined]";
            }
        }

        public static string ListKeyRoles(bool clientTeam)
        {
            List<string> dummyTeam = new List<string>();
            return MissingTeamMembers(dummyTeam, clientTeam);
        }
        
        public static string FindMissingRoles(int projectID, bool clientTeam)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    if (!clientTeam)
                    {
                        List<string> currentInternalRoles = existingPtDb.ProjectTeams.Where(
                                            pt => pt.ProjectID == projectID
                                            && (pt.FromDate == null || pt.FromDate <= Today)
                                            && (pt.ToDate == null || pt.ToDate >= Today)).Select(pt => pt.ProjectRoleCode).ToList();
                        return MissingTeamMembers(currentInternalRoles, false);
                    }
                    else
                    {
                        List<string> currentClientRoles = existingPtDb.ClientTeams.Where(
                            ct => ct.ProjectID == projectID
                            && (ct.FromDate == null || ct.FromDate <= Today)
                            && (ct.ToDate == null || ct.ToDate >= Today)).Select(ct => ct.ClientTeamRoleCode).ToList();
                        return MissingTeamMembers(currentClientRoles, true);
                    }
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error checking for missing roles", generalException);
                return null;
            }
        }

        public static bool ValidateProject(ProjectProxy proxy, bool amendExisting, bool managerChanged)
        {
            try
            {                
                string errorDetails = "";
                string errorMessage = "";
                string queryDetails = "";
                string queryMessage = "";
                List<Projects> otherClientProjects = null;
                int countLinkedProducts = 0;
                int countLinkedContacts = 0;
                int countLiveClientProducts = 0;
                string type = (proxy.Type == null) ? "" : proxy.Type.TypeCode;
                int stageNumber = (proxy.Stage != null) ? proxy.StageNumber : -1;
                bool isUnderway = (stageNumber > StartStage && !proxy.IsCancelled);
                Projects existingProjectRecord = null;
                ClientProxy client = proxy.Client ?? null;
                bool clientAdded = false;
                bool clientRemoved = false;
                bool clientChanged = false;
                List<string> currentInternalRoles = new List<string>();                
                string missingInternalRoles = "";
                List<string> currentClientRoles = new List<string>();
                string missingClientRoles = "";
                int projectID = proxy.ProjectID;
                int countDueActions = 0;

                try
                {
                    if (client == null)
                    { errorDetails = ". Please select a client record from the drop-down list. If this is an internal project, select '" + NoRecord + "'|No Client Selected"; }
                    else if (!proxy.IsInternal && !client.ActiveClient)
                    { errorDetails = ", as the selected client record is not active.|Inactive Client"; }
                    else if (proxy.ProjectName == null || proxy.ProjectName == "")
                    { errorDetails = ". Please enter a name for the project.|No Project Name"; }
                    else if (type == "")
                    { errorDetails = ". Please select a project type from the drop-down list.|No Type Selected"; }
                    else if (proxy.IsInternal && client.ID != NoID)
                    {
                        errorDetails = ". Projects for clients cannot be internal. Please choose a different "
                            + "project type if the client selection is correct, or select '" + NoRecord + "' from the client drop-down for an internal project.|Client and Project Type Mismatch";
                    }
                    else if (!proxy.IsInternal && client.ID == NoID)
                    {
                        errorDetails = ". Projects without clients must use the 'Internal project' type. "
                            + "Please choose the correct project type if this is an internal project, or select a client from the client drop-down.|Client and Project Type Mismatch";
                    }
                    else if (isUnderway && proxy.StartDate == null)
                    {
                        errorDetails = ", as no start date has been set. Please enter a start date or keep the project in the 'Initiation' stage."
                            + "|No Start Date";
                    }
                    else if (isUnderway && proxy.StartDate > Today)
                    {
                        errorDetails = ", as the start date is in the future. Please change the start date or keep the project in the 'Initiation' stage."
                            + "|No Start Date";
                    }
                    else if (proxy.ProjectManager == null)
                    {
                        errorDetails = ". Please select a Project Manager from the drop-down list. Try the 'Any' option if the required record is not listed, "
                            + "otherwise check that their account is active.|No Project Manager Selected";
                    }
                    else if (!proxy.ProjectManager.Active)
                    { errorDetails = ", as the selected Project Manager is not an active user. Ask an administrator for help if required." + "|Inactive Project Manager"; }
                    else if (proxy.Stage == null)
                    { errorDetails = ". Please select a project stage from the drop-down list.|No Stage Selected"; }
                    else if (proxy.ProjectSummary == null || proxy.ProjectSummary == "")
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
                                    && (p.ClientID == client.ID || (proxy.IsInternal && (p.ClientID == null || p.ClientID <= 0)))
                                    ).ToList();
                                countLiveClientProducts = existingPtDb.ClientProducts.Where(cp => cp.ClientID == client.ID && cp.Live == true).Count();
                                if (!proxy.IsNew)
                                {
                                    existingProjectRecord = existingPtDb.Projects.Where(p => p.ID == projectID).FirstOrDefault();
                                    countLinkedProducts = existingPtDb.ProjectProducts.Where(pp => pp.ProjectID == projectID).Count();
                                    countLinkedContacts = existingPtDb.ClientTeams.Where(ct => ct.ProjectID == projectID).Count();
                                    countDueActions = (from a in existingPtDb.Actions
                                                       join ps in existingPtDb.ProjectStages on a.StageID equals ps.ID
                                                       where a.ProjectID == projectID
                                                            && a.StatusCode != true
                                                            && ps.StageNumber < stageNumber
                                                       select a)
                                                        .Count();
                                    currentInternalRoles = existingPtDb.ProjectTeams.Where(
                                        pt => pt.ProjectID == projectID 
                                        && (pt.FromDate == null || pt.FromDate <= Today)
                                        && (pt.ToDate == null || pt.ToDate >= Today)).Select(pt => pt.ProjectRoleCode).ToList();
                                    missingInternalRoles = MissingTeamMembers(currentInternalRoles, false);
                                    currentClientRoles = existingPtDb.ClientTeams.Where(
                                        ct => ct.ProjectID == projectID
                                        && (ct.FromDate == null || ct.FromDate <= Today)
                                        && (ct.ToDate == null || ct.ToDate >= Today)).Select(ct => ct.ClientTeamRoleCode).ToList();
                                    missingClientRoles = proxy.IsInternal ? "" : MissingTeamMembers(currentClientRoles, true);
                                    clientAdded = ((existingProjectRecord.ClientID ?? 0) == 0 && client != null && client.ID > 0);
                                    clientRemoved = ((existingProjectRecord.ClientID ?? 0) > 0 && (client == null || client.ID <= 0));
                                    clientChanged = ((existingProjectRecord.ClientID ?? 0) > 0 && client != null && client.ID != existingProjectRecord.ClientID);
                                }
                                else
                                {
                                    missingInternalRoles = "project team";
                                    missingClientRoles = "project team";
                                }
                            }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error performing project validation against the database", generalException);
                            return false;
                        }
                        try
                        {
                            if (otherClientProjects.Exists(p => p.ProjectName == proxy.ProjectName))
                            { errorDetails = ", as another project exists for the same client with the same name. Please change the project name."; }
                            else if (otherClientProjects.Exists(p => p.ProjectSummary == proxy.ProjectSummary))
                            { errorDetails = ", as another project exists for the same client with the same project summary. Please change the summary details."; }
                            else if (isUnderway && missingInternalRoles != "")
                            {
                                errorDetails = ", as the project does not have a current (internal) " + missingInternalRoles + ". Key roles must be filled throughout the project. "
                                + "Please keep the project in 'Initiation' stage, then use 'Project Teams (Staff)' to complete the project team.|Key Role Not Covered"; 
                            }
                            else if (isUnderway && missingClientRoles != "")
                            {
                                errorDetails = ", as the project does not have a current client " + missingClientRoles + ". Key roles must be filled throughout the project. "
                                + "Please keep the project in 'Initiation' stage until the required roles are set in 'Project (Client) Contacts'.|Key Role Not Covered";
                            }
                            else if (clientChanged && countLinkedContacts > 0)
                            {
                                errorDetails = ". The client cannot be changed, as client contacts have already been added. If you are sure this change is necessary, please "
                                    + "first remove all client contacts from the project via 'Project Contacts'.|Cannot Change Client";
                            }
                            else if (clientRemoved && countLinkedContacts > 0)
                            {
                                errorDetails = ". The client cannot be removed, as client contacts have already been added. If you are sure this change is necessary, please "
                                    + "first remove all client contacts from the project via 'Project Contacts'.|Cannot Remove Client";
                            }
                            else if (clientAdded && countLinkedProducts > 0)
                            {
                                errorDetails = ". A client cannot be added, as products have been linked to the project (this may cause version/compatibility errors). If this change is "
                                    + "necessary, please first clear all linked products from the project via 'Project Products'.|Cannot Add Client";
                            }
                            else if (clientChanged && countLinkedProducts > 0)
                            {
                                errorDetails = ". The client cannot be changed, as products have been linked to the project (this may cause version/compatibility errors). If this change is "
                                    + "necessary, please first clear all linked products from the project via 'Project Products'.|Cannot Change Client";
                            }
                            else if (clientRemoved && countLinkedProducts > 0)
                            {
                                errorDetails = ". The client cannot be removed, as products have been linked to the project (this may cause version/compatibility errors). If this change is "
                                    + "necessary, please first clear all linked products from the project via 'Project Products'.|Cannot Remove Client";
                            }
                            else if (countDueActions > 0 && !proxy.IsCancelled)
                            {
                                errorDetails = ". This project has open actions linked to earlier stages that must be completed before the project can progress to the selected stage. Please "
                                    + "reset the project stage (or cancel) and review the project's linked actions.|Outstanding Actions";
                            }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error performing secondary project validation", generalException);
                            return false;
                        }
                    }
                    if (errorDetails != "")
                    {
                        if (proxy.IsInternal) { errorDetails = errorDetails.Replace("another project exists for the same client", "another internal project exists"); }
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
                    int originalStage = (existingProjectRecord == null) ? 0 : ProjectFunctions.GetStageNumber(existingProjectRecord.StageID);
                    bool goLive = IsGoLive(originalStage, stageNumber);
                    string goLiveQuery = goLive? "This will set the project to Live, and update all linked products appropriately." : "";
                    bool reversal = goLive? false : IsLiveReversal(originalStage, stageNumber);
                    string reversalQuery = reversal? "This will reverse the 'Go-Live' action, and all status and version changes to linked products." : "";
                    
                    if (managerChanged && proxy.ProjectManager.RoleCode != ProjectManagerCode)
                    { queryDetails = queryDetails + "\n" + "The Project Manager is also not normally a Project Manager by role."; }
                    if (isUnderway && countLinkedProducts == 0)
                    { queryDetails = queryDetails + "\n" + "The project stage also indicates that the project is underway, but it has no linked products."; }
                    if (proxy.StartDate == null && !proxy.IsCancelled) { queryDetails = queryDetails + "\n" + "The project also has no predicted start date at present."; } // Only projects not yet underway
                    if (proxy.StartDate > DateTime.Today.AddYears(1)) { queryDetails = queryDetails + "\n" + "The project also starts more than a year in the future."; }
                    if ((existingProjectRecord == null || existingProjectRecord.StartDate == null || existingProjectRecord.StartDate > proxy.StartDate)
                        && proxy.StartDate < DateTime.Today.AddYears(-1)) { queryDetails = queryDetails + "\n" + "The project also starts more than a year in the past."; }
                    if (originalStage > stageNumber && !reversal) // Live reversals are handled separately
                    { queryDetails = queryDetails + "\n" + "The new stage is also less advanced than the previous one."; }
                    if (!proxy.IsNew && stageNumber - originalStage > 4 && !proxy.IsCancelled) { queryDetails = queryDetails + "\n" + "The project has moved through several stages."; }
                    if (!proxy.IsInternal)
                    {
                        if (countLiveClientProducts == 0 && type != NewSiteCode && type != AddSystemCode)
                        { queryDetails = queryDetails + "\n" + "The project type also indicates a change to an existing product, but this client has no Live products."; }
                        else if (countLiveClientProducts > 0 && (type == NewSiteCode))
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
                    else if (proxy.IsCancelled && existingProjectRecord != null && ProjectFunctions.GetStageNumber(existingProjectRecord.StageID) != CancelledStage)
                    {
                        queryMessage = "Are you sure you wish to cancel this project?";
                        return MessageFunctions.ConfirmOKCancel(queryMessage);
                    }
                    else if (proxy.IsNew)
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

        public static bool SaveNewProject(ref ProjectProxy projectProxy)
        {
            if (!ValidateProject(projectProxy, false, true)) { return false; }            
            try
            {
                Projects thisProject = new Projects();
                bool converted = projectProxy.ConvertToProject(ref thisProject);
                if (!converted || thisProject == null) { return false; } // Errors should be thrown by the conversion
                ProjectTeams addPM = new ProjectTeams
                {
                    ProjectID = 0, // Set below when the project is saved
                    StaffID = projectProxy.ProjectManager.ID,
                    ProjectRoleCode = ProjectManagerCode,
                    FromDate = (DateTime.Today < thisProject.StartDate) ? DateTime.Today : thisProject.StartDate
                };

                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    existingPtDb.Projects.Add(thisProject);
                    UpdateHistoryForStatus(thisProject, true);
                    existingPtDb.SaveChanges();
                    projectProxy.ProjectID = thisProject.ID;
                    SelectedProjectProxy = projectProxy;

                    addPM.ProjectID = thisProject.ID;
                    existingPtDb.ProjectTeams.Add(addPM);
                    existingPtDb.SaveChanges();
                    
                    MessageFunctions.SuccessAlert("New project saved successfully. You can now add products and project team members.", "Project Created");
                    
                    return true;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error saving new project to the database", generalException);
                return false;
            }		            
        }

        public static bool SaveProjectChanges(ProjectProxy projectProxy, bool managerChanged, int originalStageNumber)
        {                 
            if (!ValidateProject(projectProxy, true, managerChanged)) { return false; }
            try
            {
                int projectID = projectProxy.ProjectID;
                
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    Projects thisProject = existingPtDb.Projects.Where(p => p.ID == projectID).FirstOrDefault();
                    if (thisProject == null)
                    {
                        MessageFunctions.Error("Error saving project amendments to the database: no matching project found.", null);
                        return false;
                    }
                    bool converted = projectProxy.ConvertToProject(ref thisProject);
                    if (!converted) { return false; } // Errors should be thrown by the conversion

                    if (managerChanged)
                    {
                        try
                        {
                            ProjectTeams newPMRecord = new ProjectTeams
                            {
                                ProjectID = projectID,
                                StaffID = projectProxy.ProjectManager.ID,
                                ProjectRoleCode = ProjectManagerCode,
                                FromDate = (DateTime.Today < thisProject.StartDate) ? DateTime.Today : thisProject.StartDate
                            };

                            List<ProjectTeams> existingPMs = CurrentAndFuturePMs(projectID, Yesterday);
                            if (existingPMs.Count == 0) { existingPtDb.ProjectTeams.Add(newPMRecord); } // Shouldn't happen, but just in case
                            else
                            {
                                ProjectTeams lastPMRecord = existingPMs.First();
                                lastPMRecord = existingPtDb.ProjectTeams.Find(lastPMRecord.ID); // Get it from the database again so we can amend/remove it

                                if (( lastPMRecord.FromDate != null && lastPMRecord.FromDate > OneMonthAgo) || projectProxy.StageID <= StartStage)
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

                    string congratulations = "";
                    bool stageChanged = (originalStageNumber != projectProxy.StageNumber);

                    if (stageChanged)
                    {
                        bool stageManaged = HandleStageChanges(projectProxy, originalStageNumber, out congratulations) && UpdateHistoryForStatus(thisProject, false); 
                        if (!stageManaged) { return false; }
                    }
                    existingPtDb.SaveChanges();
                    SelectedProjectProxy = projectProxy;                                    
                    
                    MessageFunctions.SuccessAlert("Project amendments saved successfully." + congratulations, "Changes Saved");
                    return true;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error saving project amendments to the database", generalException);
                return false;
            }		            
        }

        private static bool HandleStageChanges(ProjectProxy projectProxy, int originalStageNumber, out string congratulations)
        {
            congratulations = "";
            bool goLive = IsGoLive(originalStageNumber, projectProxy.StageNumber);
            bool reversal = goLive ? false : IsLiveReversal(originalStageNumber, projectProxy.StageNumber);
            bool completion = (projectProxy.StageNumber == CompletedStage && originalStageNumber != CompletedStage);
            if (goLive || reversal) 
            { 
                bool productsUpdated = UpdateLinkedProducts(projectProxy, goLive);
                if (!productsUpdated) { return false; }
            }
            if (completion) { congratulations = " Congratulations on completing the project."; }
            else if (goLive) { congratulations = " Congratulations on going Live."; }
            return true;
        }

        private static bool UpdateLinkedProducts(ProjectProxy projectProxy, bool isGoLive)
        {
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {                
                // TODO: handle internal projects - should we update linked master versions?
                try
                {
                    var linkedProducts = from p in existingPtDb.Projects
                                            join pp in existingPtDb.ProjectProducts on p.ID equals pp.ProjectID
                                            join cp in existingPtDb.ClientProducts on pp.ProductID equals cp.ProductID
                                            where p.ID == projectProxy.ProjectID && cp.ClientID == p.ClientID
                                            select new { ProjectProducts = pp, ClientProducts = cp };

                    foreach (var product in linkedProducts)
                    {
                        if (isGoLive)
                        {
                            product.ClientProducts.Live = true;
                            product.ClientProducts.ProductVersion = product.ProjectProducts.NewVersion;
                        }
                        else
                        {
                            if (Array.IndexOf(NewProductTypeCodes, projectProxy.Type.TypeCode) >= 0) { product.ClientProducts.Live = false; }
                            if (product.ClientProducts.ProductVersion == product.ProjectProducts.NewVersion)
                            {
                                product.ClientProducts.ProductVersion = product.ProjectProducts.OldVersion;
                            }
                        }
                    }
                    existingPtDb.SaveChanges();
                    return true;
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error updating linked products", generalException);
                    return false;
                }                
            }
        }

        // Project Teams (updates)
        
        public static bool UpdateOtherInstances(TeamProxy newRecord)
        {
            try
            {
                int predecessorID = (GetStaffPredecessor(newRecord) != null)? GetStaffPredecessor(newRecord).ID : 0;
                int successorID =  (GetStaffSuccessor(newRecord) != null)? GetStaffSuccessor(newRecord).ID : 0;
                
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

        public static bool SaveProjectTeamChanges(TeamProxy currentVersion, TeamProxy savedVersion)
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
                    if (currentVersion.HasKeyRole) { UpdateOtherInstances(currentVersion); }
                    existingPtDb.SaveChanges();
                    MessageFunctions.SuccessAlert("Your changes have been saved successfully.", "Team Membership Amended");
                    return true;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error saving changes to project team member details", generalException);
                return false;
            }	
        }

        public static int SaveNewProjectTeam(TeamProxy newRecord)
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
                    if (newRecord.HasKeyRole) { UpdateOtherInstances(newRecord); }
                    existingPtDb.SaveChanges();
                    MessageFunctions.SuccessAlert("New project team member added successfully.", "Team Member Added");
                    return thisTeam.ID;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error creating new project team member", generalException);
                return 0;
            }
        }

        public static bool RemoveTeamEntry(TeamProxy unwantedRecord)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    if (unwantedRecord.HasKeyRole)
                    {
                        MessageFunctions.Error("Error removing project team member: this record has a key role", null);
                        return false;
                    }
                    if (HasProjectActions(unwantedRecord))
                    {
                        MessageFunctions.InvalidMessage("This team member has actions on the project, and cannot be removed unless they are moved to another team member.","Actions Logged");
                        return false;
                    }
                    if (HasLoggedActions(unwantedRecord))
                    {
                        MessageFunctions.InvalidMessage("This team member has logged actions on the project, and cannot be removed.", "Actions Logged");
                        return false;
                    }
                    if (!MessageFunctions.ConfirmOKCancel("Are you sure you want to remove this record from the project?", "Remove Project Team Entry?")) { return false; }
                    ProjectTeams thisTeam = existingPtDb.ProjectTeams.Where(pt => pt.ID == unwantedRecord.ID).FirstOrDefault();                              
                    existingPtDb.ProjectTeams.Remove(thisTeam);
                    existingPtDb.SaveChanges();
                    MessageFunctions.SuccessAlert("Team member removed successfully.", "Team Member Removed");
                    return true;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error removing project team member", generalException);
                return false;
            }
        }

        // Client Teams (updates)

        public static bool UpdateOtherClientInstances(ProjectContactProxy newRecord)
        {
            try
            {
                int predecessorID = (GetContactPredecessor(newRecord) != null) ? GetContactPredecessor(newRecord).ID : 0;
                int successorID = (GetContactSuccessor(newRecord) != null) ? GetContactSuccessor(newRecord).ID : 0;

                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    List<ClientTeams> otherInstances = existingPtDb.ClientTeams
                        .Where(ct => ct.ID != newRecord.ID
                            && ct.ProjectID == newRecord.Project.ID
                            && ct.ClientTeamRoleCode == newRecord.RoleCode)
                        .ToList();

                    if (otherInstances.Count == 0) { return true; }
                    else
                    {
                        DateTime dayBefore = newRecord.EffectiveFrom.AddDays(-1);
                        DateTime dayAfter = newRecord.EffectiveTo.AddDays(1);

                        foreach (ClientTeams current in otherInstances)
                        {
                            DateTime currentFrom = current.FromDate ?? StartOfTime;
                            DateTime currentTo = current.ToDate ?? InfiniteDate;

                            if (currentFrom >= dayAfter || currentTo <= dayBefore) // No overlap - but may need to change to close the gap
                            {
                                if (current.ID == predecessorID && currentTo < dayBefore) { current.ToDate = dayBefore; }
                                else if (current.ID == successorID && currentFrom > dayAfter) { current.FromDate = dayAfter; }
                            }
                            else if (newRecord.FromDate != null && newRecord.ToDate != null && currentFrom <= dayBefore && currentTo >= dayAfter) // Split encompassing record
                            {
                                ClientTeams additional = new ClientTeams
                                {
                                    ProjectID = current.ProjectID,
                                    ClientStaffID = current.ClientStaffID,
                                    ClientTeamRoleCode = current.ClientTeamRoleCode,
                                    FromDate = dayAfter,
                                    ToDate = current.ToDate
                                };
                                current.ToDate = dayBefore;
                                existingPtDb.ClientTeams.Add(additional);
                            }
                            else if (newRecord.FromDate != null && currentFrom <= dayBefore && currentTo > dayBefore && (current.ToDate == null || currentTo < dayAfter))
                            { current.ToDate = dayBefore; } // Curtail predecessor
                            else if (newRecord.ToDate != null && (current.FromDate == null || currentFrom > dayBefore) && currentFrom < dayAfter && currentTo >= dayAfter)
                            { current.FromDate = dayAfter; } // Delay successor
                            else if ((currentFrom > dayBefore || current.FromDate == null) && (currentTo < dayAfter || current.ToDate == null)) // Remove subsumed/replaced record
                            {
                                existingPtDb.ClientTeams.Remove(current);
                            }
                        }
                    }
                    existingPtDb.SaveChanges();
                    return true;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error updating affected client predecessors and/or successors", generalException);
                return false;
            }
        }

        public static bool SaveProjectContactChanges(ProjectContactProxy currentVersion, ProjectContactProxy savedVersion)
        {
            if (!currentVersion.Validate(savedVersion)) { return false; }

            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    ClientTeams thisTeam = existingPtDb.ClientTeams.Where(ct => ct.ID == savedVersion.ID).FirstOrDefault();
                    if (thisTeam == null)
                    {
                        MessageFunctions.Error("Error saving changes to client team member details: no matching record found.", null);
                        return false;
                    }
                    currentVersion.ConvertToClientTeam(ref thisTeam);
                    if (currentVersion.HasKeyRole) { UpdateOtherClientInstances(currentVersion); }
                    existingPtDb.SaveChanges();
                    MessageFunctions.SuccessAlert("Your changes have been saved successfully.", "Team Membership Amended");
                    return true;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error saving changes to client team member details", generalException);
                return false;
            }
        }

        public static int SaveNewProjectContact(ProjectContactProxy newRecord)
        {
            if (!newRecord.Validate(null)) { return 0; }
            ClientTeams thisTeam = new ClientTeams();
            newRecord.ConvertToClientTeam(ref thisTeam);

            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    existingPtDb.ClientTeams.Add(thisTeam);
                    if (newRecord.HasKeyRole) { UpdateOtherClientInstances(newRecord); }
                    existingPtDb.SaveChanges();
                    MessageFunctions.SuccessAlert("New client team member added successfully.", "Team Member Added");
                    return thisTeam.ID;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error creating new client team member", generalException);
                return 0;
            }
        }

        public static bool RemoveProjectContact(ProjectContactProxy unwantedRecord)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    if (unwantedRecord.HasKeyRole)
                    {
                        MessageFunctions.Error("Error removing client team member: this record has a key role", null);
                        return false;
                    }
                    if (HasProjectActions(unwantedRecord))
                    {
                        MessageFunctions.InvalidMessage("This team member has actions on the project, and cannot be removed unless they are moved to another team member.", "Actions Logged");
                        return false;
                    }
                    if (!MessageFunctions.ConfirmOKCancel("Are you sure you want to remove this record from the project?", "Remove Client Team Entry?")) { return false; }
                    ClientTeams thisTeam = existingPtDb.ClientTeams.Where(ct => ct.ID == unwantedRecord.ID).FirstOrDefault();
                    existingPtDb.ClientTeams.Remove(thisTeam);
                    existingPtDb.SaveChanges();
                    MessageFunctions.SuccessAlert("Team member removed successfully.", "Team Member Removed");
                    return true;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error removing client team member", generalException);
                return false;
            }
        }	

        // Project Products (updates)
        public static decimal GetCurrentVersion(Projects thisProject, Products thisProduct)
        {
            try
            {                               
                if (thisProject.ClientID == null || thisProject.ClientID <= 0)
                {
                    return Math.Floor((thisProduct.LatestVersion ?? 0) * 10) / 10;
                }
                else
                {                                       
                    ClientProducts thisRecord = ClientFunctions.GetClientProduct((int)thisProject.ClientID, thisProduct.ID);
                    return thisRecord.ProductVersion ?? 0;
                }
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error calculating current version", generalException);
                return 0;           
            }
        }

        public static decimal SuggestNewVersion(Projects thisProject, Products thisProduct)
        {
            try
            {
                if (thisProject.TypeCode.StartsWith("U")) { return Math.Floor((thisProduct.LatestVersion ?? 0) * 10) / 10; }
                else { return GetCurrentVersion(thisProject, thisProduct); }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error calculating new version", generalException);
                return 0;
            }
        }

        public static bool ToggleProductProjects(List<Projects> affectedProjects, bool addition, Products thisProduct)
        {
            try
            {
                int productID = thisProduct.ID;
                string productName = thisProduct.ProductName;

                foreach (Projects thisRecord in affectedProjects)
                {
                    int projectID = thisRecord.ID;
                    Projects thisProject = GetProject(projectID);
                    //bool canChange = addition ? true : CanRemoveProjectProduct(ref thisProject, productID);

                    //if (!canChange) { return false; }
                    if (addition)
                    {
                        try
                        {
                            ProjectProductProxy addRecord = new ProjectProductProxy
                            {
                                Project = thisProject,
                                Product = thisProduct,
                                OldVersion = GetCurrentVersion(thisProject, thisProduct),
                                NewVersion = SuggestNewVersion(thisProject, thisProduct),
                                JustAdded = true
                            };
                            ProjectsForProduct.Add(addRecord);
                            ProjectsNotForProduct.Remove(thisRecord);

                            if (ProjectIDsToRemove.Contains(projectID)) { ProjectIDsToRemove.Remove(projectID); }
                            else { ProjectIDsToAdd.Add(projectID); }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error adding product " + productName + " to project " + thisProject.ProjectName, generalException);
                            return false;
                        }
                    }
                    else
                    {
                        try
                        {
                            ProjectsNotForProduct.Add(thisRecord);
                            ProjectProductProxy removeRecord = ProjectsForProduct.FirstOrDefault(cps => cps.ProjectID == projectID && cps.ProductID == thisProduct.ID);
                            ProjectsForProduct.Remove(removeRecord);

                            if (ProjectIDsToAdd.Contains(projectID)) { ProjectIDsToAdd.Remove(projectID); }
                            else { ProjectIDsToRemove.Add(projectID); }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error removing product " + productName + " from project " + thisProject.ProjectName, generalException);
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception generalException)
            {
                string change = addition ? "addition" : "removal";
                MessageFunctions.Error("Error processing " + change + "", generalException);
                return false;
            }
        }

        public static bool ToggleProjectProducts(List<ClientProductProxy> affectedClientProducts, bool addition, ProjectProxy thisProjectProxy)
        {
            try
            {
                int projectID = thisProjectProxy.ProjectID;
                Projects thisProject = new Projects();
                thisProjectProxy.ConvertToProject(ref thisProject);

                foreach (ClientProductProxy thisRecord in affectedClientProducts)
                {
                    int productID = thisRecord.ProductID;
                    Products thisProduct = ProductFunctions.GetProductByID(productID);
                    //bool canChange = addition ? true : CanRemoveProjectProduct(ref thisProject, productID);

                    //if (!canChange) { return false; }
                    if (addition)
                    {
                        try
                        {
                            ProjectProductProxy addRecord = new ProjectProductProxy
                            {
                                Project = thisProject,
                                Product = thisProduct,
                                OldVersion = thisRecord.ClientVersion,
                                NewVersion = SuggestNewVersion(thisProject, thisProduct),
                                JustAdded = true
                            };
                            ProductsForProject.Add(addRecord);
                            ProductsNotForProject.Remove(thisRecord);

                            if (ProductIDsToRemove.Contains(productID)) { ProductIDsToRemove.Remove(productID); }
                            else { ProductIDsToAdd.Add(productID); }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error adding " + thisProduct.ProductName + " to project " + thisProject.ProjectName, generalException);
                            return false;
                        }
                    }
                    else
                    {
                        try
                        {
                            ProductsNotForProject.Add(thisRecord);
                            ProjectProductProxy removeRecord = ProductsForProject.FirstOrDefault(pfp => pfp.ProjectID == thisProject.ID && pfp.ProductID == productID);
                            
                            ProductsForProject.Remove(removeRecord);

                            if (ProductIDsToAdd.Contains(productID)) { ProductIDsToAdd.Remove(productID); }
                            else { ProductIDsToRemove.Add(productID); }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error removing " + thisProduct.ProductName + " from project " + thisProject.ProjectName, generalException);
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception generalException)
            {
                string change = addition ? "addition" : "removal";
                MessageFunctions.Error("Error processing " + change + "", generalException);
                return false;
            }
        }

        public static bool IgnoreAnyChanges()
        {
            if (ProjectIDsToAdd.Count > 0 || ProjectIDsToRemove.Count > 0 || ProjectIDsToUpdate.Count > 0
                || ProductIDsToAdd.Count > 0 || ProductIDsToRemove.Count > 0 || ProductIDsToUpdate.Count > 0)
            {
                return MessageFunctions.WarningYesNo("This will undo any changes you made since you last saved. Continue?");
            }
            else
            {
                return true;
            }
        }

        public static void ClearAnyChanges()
        {
            ProjectIDsToAdd.Clear();
            ProjectIDsToRemove.Clear();
            ProjectIDsToUpdate.Clear();
            ProductIDsToAdd.Clear();
            ProductIDsToRemove.Clear();
            ProductIDsToUpdate.Clear();
        }

        public static bool AmendOldVersion(ProjectProductProxy thisRecord, string version, bool byProject)
        {
            decimal versionNumber;
            bool carryOn = false;

            if (!Decimal.TryParse(version, out versionNumber))
            {
                MessageFunctions.Error("Cannot update the version: the given number is not a decimal.", null);
                return false;
            }
            else if (versionNumber == thisRecord.OldVersion) { return false; }
            else if (thisRecord.LatestVersion < versionNumber && thisRecord.ClientID > 0)
            {
                MessageFunctions.InvalidMessage("The entered version number is higher than the latest product version. Please try again.", "Invalid version");
                return false;
            }
            else if (thisRecord.LatestVersion > versionNumber && thisRecord.ClientID == 0)
            {
                MessageFunctions.InvalidMessage("The entered version number is lower than the latest product version. Please try again.", "Invalid version");
                return false;
            }
            else if (thisRecord.NewVersion < versionNumber)
            {
                carryOn = MessageFunctions.WarningYesNo("The entered version number is higher than the target version. Is this correct?");
            }
            else if (thisRecord.OldVersion > versionNumber && !thisRecord.JustAdded)
            {
                carryOn = MessageFunctions.WarningYesNo("The entered version number is lower than the one saved previously. Is this correct?");
            }
            else if (!thisRecord.JustAdded)
            {
                carryOn = MessageFunctions.ConfirmOKCancel("Update the project's source version of this product? This is not immediately saved, so it can be undone using the 'Back' button.");
            }
            else { carryOn = true; }

            if (!carryOn) { return false; }
            else
            {
                thisRecord.OldVersion = versionNumber;
                queueProjectProductUpdate(thisRecord, byProject);
                return true;
            }
        }

        public static bool AmendNewVersion(ProjectProductProxy thisRecord, string version, bool byProject)
        {
            decimal versionNumber;
            bool carryOn = false;

            if (!Decimal.TryParse(version, out versionNumber))
            {
                MessageFunctions.Error("Cannot update the version: the given number is not a decimal.", null);
                return false;
            }
            else if (versionNumber == thisRecord.NewVersion) { return false; }
            else if (thisRecord.LatestVersion < versionNumber && thisRecord.ClientID > 0)
            {
                MessageFunctions.InvalidMessage("The entered version number is higher than the latest product version. Please try again.", "Invalid version");
                return false;
            }
            else if (thisRecord.LatestVersion > versionNumber && thisRecord.ClientID == 0)
            {
                MessageFunctions.InvalidMessage("The entered version number is lower than the latest product version. Please try again.", "Invalid version");
                return false;
            }
            else if (thisRecord.OldVersion > versionNumber)
            {
                carryOn = MessageFunctions.WarningYesNo("The entered version number is lower than the current version. Is this correct?");
            }
            else if (thisRecord.NewVersion > versionNumber && !thisRecord.JustAdded)
            {
                carryOn = MessageFunctions.WarningYesNo("The entered version number is lower than the one saved previously. Is this correct?");
            }
            else if (!thisRecord.JustAdded)
            {
                carryOn = MessageFunctions.ConfirmOKCancel("Update the project's target version of this product? This is not immediately saved, so it can be undone using the 'Back' button.");
            }
            else { carryOn = true; }

            if (!carryOn) { return false; }
            else
            {
                thisRecord.NewVersion = versionNumber;
                queueProjectProductUpdate(thisRecord, byProject);
                return true;
            }
        }

        private static void queueProjectProductUpdate(ProjectProductProxy thisRecord, bool byProject)
        {
            if (byProject)
            {
                int productID = thisRecord.ProductID;
                if (!ProductIDsToAdd.Contains(productID) && !ProductIDsToUpdate.Contains(productID))
                {
                    ProductIDsToUpdate.Add(productID);
                }
            }
            else
            {
                int projectID = thisRecord.ProjectID;
                if (!ProjectIDsToAdd.Contains(projectID) && !ProjectIDsToUpdate.Contains(projectID))
                {
                    ProjectIDsToUpdate.Add(projectID);
                }
            }
        }

        public static bool SaveProductProjectChanges(int productID)
        {
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {
                try
                {
                    List<ProjectProducts> recordsToRemove = (from pp in existingPtDb.ProjectProducts
                                                             where pp.ProductID == productID && ProjectIDsToRemove.Contains((int)pp.ProjectID)
                                                             select pp).ToList();
                    foreach (ProjectProducts removePP in recordsToRemove)
                    {
                        existingPtDb.ProjectProducts.Remove(removePP);
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error deleting project links with product ID " + productID.ToString(), generalException);
                    return false;
                }

                try
                {
                    foreach (int addProjectID in ProjectIDsToAdd)
                    {
                        ProjectProductProxy proxyRecord = ProjectsForProduct.FirstOrDefault(cfp => cfp.ProductID == productID && cfp.ProjectID == addProjectID);
                        if (proxyRecord == null)
                        {
                            MessageFunctions.Error("Error saving new project links with product ID " + productID.ToString() + ": no matching display record found", null);
                            return false;
                        }
                        else
                        {
                            ProjectProducts pp = new ProjectProducts
                            {
                                ProductID = productID,
                                ProjectID = addProjectID,
                                OldVersion = proxyRecord.OldVersion,
                                NewVersion = proxyRecord.NewVersion
                            };
                            existingPtDb.ProjectProducts.Add(pp);
                        }
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error saving project links with product ID " + productID.ToString(), generalException);
                    return false;
                }

                try
                {
                    List<ProjectProducts> recordsToUpdate = (from pp in existingPtDb.ProjectProducts
                                                             where pp.ProductID == productID && ProjectIDsToUpdate.Contains((int)pp.ProjectID)
                                                             select pp).ToList();
                    foreach (ProjectProducts updatePP in recordsToUpdate)
                    {
                        ProjectProductProxy proxyRecord = ProjectsForProduct.FirstOrDefault(cfp => cfp.ID == updatePP.ID && cfp.ProductID == updatePP.ProductID
                            && cfp.ProjectID == updatePP.ProjectID);
                        if (proxyRecord == null)
                        {
                            MessageFunctions.Error("Error updating project links with product ID " + productID.ToString() + ": no matching display record found", null);
                            return false;
                        }
                        else
                        {
                            updatePP.OldVersion = proxyRecord.OldVersion;
                            updatePP.NewVersion = proxyRecord.NewVersion;
                        }
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error saving updates to project links with product ID " + productID.ToString(), generalException);
                    return false;
                }

                existingPtDb.SaveChanges();
                ClearAnyChanges();
                return true;
            }
        }

        public static bool SaveProjectProductChanges(int projectID)
        {
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {
                try
                {
                    List<ProjectProducts> recordsToRemove = (from pp in existingPtDb.ProjectProducts
                                                             where pp.ProjectID == projectID && ProductIDsToRemove.Contains((int)pp.ProductID)
                                                             select pp).ToList();
                    foreach (ProjectProducts removePP in recordsToRemove)
                    {
                        existingPtDb.ProjectProducts.Remove(removePP);
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error deleting products from project ID " + projectID.ToString(), generalException);
                    return false;
                }

                try
                {
                    foreach (int addProductID in ProductIDsToAdd)
                    {
                        ProjectProductProxy proxyRecord = ProductsForProject.FirstOrDefault(cfp => cfp.ProductID == addProductID && cfp.ProjectID == projectID);
                        if (proxyRecord == null)
                        {
                            MessageFunctions.Error("Error saving new product links with project ID " + projectID.ToString() + ": no matching display record found", null);
                            return false;
                        }
                        else
                        {
                            ProjectProducts pp = new ProjectProducts
                            {
                                ProductID = addProductID,
                                ProjectID = projectID,
                                OldVersion = proxyRecord.OldVersion,
                                NewVersion = proxyRecord.NewVersion
                            };
                            existingPtDb.ProjectProducts.Add(pp);
                        }
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error saving product links with project ID " + projectID.ToString(), generalException);
                    return false;
                }

                try
                {
                    List<ProjectProducts> recordsToUpdate = (from pp in existingPtDb.ProjectProducts
                                                             where pp.ProjectID == projectID && ProductIDsToUpdate.Contains((int)pp.ProductID)
                                                             select pp).ToList();
                    foreach (ProjectProducts updatePP in recordsToUpdate)
                    {
                        ProjectProductProxy proxyRecord = ProductsForProject.FirstOrDefault(cfp => cfp.ID == updatePP.ID && cfp.ProductID == updatePP.ProductID
                            && cfp.ProjectID == updatePP.ProjectID);
                        if (proxyRecord == null)
                        {
                            MessageFunctions.Error("Error updating product links with project ID " + projectID.ToString() + ": no matching display record found", null);
                            return false;
                        }
                        else
                        {
                            updatePP.OldVersion = proxyRecord.OldVersion;
                            updatePP.NewVersion = proxyRecord.NewVersion;
                        }
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error saving updates to product links with project ID " + projectID.ToString(), generalException);
                    return false;
                }

                existingPtDb.SaveChanges();
                ClearAnyChanges();
                return true;
            }
        }

        // Project timeline (stage history) updates

        public static bool ProcessDateChange(int affectedStageNo, int projectStageNo, DateTime? newDate, bool isTarget)
        {
            if (isTarget)
            {
                if (affectedStageNo <= projectStageNo)
                {
                    MessageFunctions.InvalidMessage("You cannot change the target date of previous stages.", "Previous Stage");
                    return false;
                }
            }            
            else
            {
                if (affectedStageNo == projectStageNo && newDate == null)
                {
                    MessageFunctions.InvalidMessage("The actual start date of the current stage cannot be null.", "Missing Start Date");
                    return false;
                }
                else if (affectedStageNo == projectStageNo && newDate > Today)
                {
                    MessageFunctions.InvalidMessage("The actual start date of the current stage cannot be in the future.", "Future Date Selected");
                    return false;
                }
                else if (affectedStageNo < projectStageNo && newDate > Today)
                {
                    MessageFunctions.InvalidMessage("The actual start date of previous stages cannot be in the future.", "Future Date Selected");
                    return false;
                }
                else if (affectedStageNo > projectStageNo && newDate != null)
                {
                    MessageFunctions.InvalidMessage("The actual start date of future stages cannot be set manually.", "Past Date Selected");
                    return false;
                }
            }

            QueueDateChange(affectedStageNo);
            return true;
        }

        public static void QueueDateChange(int StageNumber)
        {
            if (!StageDatesChanged.Contains(StageNumber)) { StageDatesChanged.Add(StageNumber); }
        }

        public static void ClearHistoryChanges()
        {
            StageDatesChanged.Clear();
        }

        public static bool UpdateHistoryForStatus(Projects thisProject, bool isNew)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    if (isNew)
                    {
                        StageHistory firstStage = new StageHistory
                        {
                            ProjectID = thisProject.ID,
                            StageID = thisProject.StageID,
                            TargetStart = Today,
                            ActualStart = Today
                        };
                        existingPtDb.StageHistory.Add(firstStage);
                    }
                    else
                    {
                        int newStageNumber = GetStageNumber(thisProject.StageID);
                        ProjectStages currentStage = ProjectCurrentStage(thisProject.ID);
                        SetFullStageList();
                        foreach (ProjectStages thisStage in FullStageList)
                        {
                            if (thisStage.StageNumber <= currentStage.StageNumber && thisStage.StageNumber < newStageNumber) { continue; }
                            else
                            {
                                StageHistory stageHist = existingPtDb.StageHistory.FirstOrDefault(sh => sh.ProjectID == thisProject.ID && sh.StageID == thisStage.ID);
                                if (thisStage.StageNumber == newStageNumber)
                                {                                    
                                    if (stageHist == null)
                                    {
                                        stageHist = new StageHistory
                                        {
                                            ProjectID = thisProject.ID,
                                            StageID = thisProject.StageID,
                                            TargetStart = null,
                                            ActualStart = Today
                                        };
                                        existingPtDb.StageHistory.Add(stageHist);
                                    }
                                    else { stageHist.ActualStart = (DateTime?)Today; }
                                }
                                else if (stageHist != null && stageHist.ActualStart != null) { stageHist.ActualStart = null; } 
                            }
                        }
                    }
                    existingPtDb.SaveChanges();
                    return true;
				}
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error updating project stage history to match new status", generalException);
                return false;
            }	
        }

        public static bool UpdateHistory(TimelineProxy thisTimeline, bool stageChanged)
        {
            try
            {
                ProjectProxy projectProxy = GetProjectProxy(thisTimeline.ProjectID);
                string congratulations = "";
                if (stageChanged)
                {
                    projectProxy.Stage = GetStageByID(thisTimeline.Stage.ID);
                    if (!ValidateProject(projectProxy, true, false)) { return false; }
                }

                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    try
                    {
                        if (stageChanged)
                        {
                            Projects thisProject = existingPtDb.Projects.Where(p => p.ID == thisTimeline.ProjectID).FirstOrDefault();
                            if (thisProject == null)
                            {
                                MessageFunctions.Error("Error saving project amendments to the database: no matching project found.", null);
                                return false;
                            }
                            int originalStageNumber = ProjectCurrentStage(thisTimeline.ProjectID).StageNumber;
                            thisProject.StageID = thisTimeline.StageID;
                            bool stageManaged = HandleStageChanges(projectProxy, originalStageNumber, out congratulations);                            
                            if (!stageManaged) { return false; }
                            existingPtDb.SaveChanges();
                        }
                    }
                    catch (Exception generalException)
                    {
                        MessageFunctions.Error("Error updating project stage", generalException);
                        return false;
                    }

                    int currentStage = GetStageNumber(thisTimeline.StageID);
                    SetFullStageList();
                    foreach (ProjectStages thisStage in FullStageList)
                    {
                        if (!StageDatesChanged.Contains(thisStage.StageNumber)) { continue; }
                        bool target = (thisTimeline.TimeType == TimelineType.Target || (thisTimeline.TimeType == TimelineType.Effective && thisStage.StageNumber > currentStage));                        

                        DateTime? newDate = (DateTime?) thisTimeline.DateHash[thisStage.StageNumber];
                        StageHistory stageHist = existingPtDb.StageHistory.FirstOrDefault(sh => sh.ProjectID == thisTimeline.ProjectID && sh.StageID == thisStage.ID);
                        if (stageHist == null)
                        {
                            stageHist = new StageHistory
                            {
                                ProjectID = thisTimeline.ProjectID,
                                StageID = thisStage.ID,
                                TargetStart = target? newDate : null,
                                ActualStart = target? null : newDate
                            };
                            existingPtDb.StageHistory.Add(stageHist);
                        }
                        else if (target) 
                        { 
                            stageHist.TargetStart = newDate;
                            if (thisStage.StageNumber > currentStage) { stageHist.ActualStart = null; } // Also nullify any unwanted actuals
                        }
                        else if (newDate <= Today) { stageHist.ActualStart = newDate; } // Don't overwrite with target date if stage moved backwards
                        else { stageHist.ActualStart = null; }
                    }
                    existingPtDb.SaveChanges();
                    ClearHistoryChanges();
                    MessageFunctions.SuccessAlert("Changes saved successfully. You can make further changes in this screen, or use the 'Back' or 'Close' buttons to exit." + congratulations, 
                        "Timeline Changes Saved");
                    return true;
                }
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error updating project stage history", generalException);
                return false;
            }
        }

        // Actions
        public static bool SaveActions()
        {
            try
            {
                List<ActionProxy> changedActions = ActionList.Where(al => al.Created || al.Updated).ToList();
                foreach (ActionProxy thisAction in changedActions)
                {
                    if (!thisAction.Validate()) { return false; }
                }

                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    foreach (ActionProxy thisProxy in changedActions)
                    {
                        try 
                        {
                            if (thisProxy.Created)
                            {
                                Actions newAction = new Actions();
                                thisProxy.ConvertToAction(ref newAction);
                                existingPtDb.Actions.Add(newAction);
                            }
                            else
                            {
                                int projectID = thisProxy.Project.ID;
                                Actions existingAction = existingPtDb.Actions.FirstOrDefault(a => a.ID == thisProxy.ID && a.ProjectID == projectID);
                                if (existingAction == null)
                                {
                                    MessageFunctions.Error("Error saving changes to action " + thisProxy.ActionCode + ": could not find action with ID " + thisProxy.ID.ToString()
                                        + " on project ID " + projectID.ToString(), null);
                                    return false;
                                }
                                thisProxy.ConvertToAction(ref existingAction);
                            }
                        }
                        catch (Exception generalException) { MessageFunctions.Error("Error saving action " + thisProxy.ActionCode, generalException); }	
                    }
                    existingPtDb.SaveChanges();
                }
                MessageFunctions.SuccessAlert("Actions saved successfully. You can now make further changes, select another project to edit (click 'All Projects' to view all) "
                    + "or close the page.","Action Changes Saved");
                return true;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error saving new or amended actions", generalException);
                return false;
            }
        }

    } // class
} // namespace
