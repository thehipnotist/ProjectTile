using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace ProjectTile
{
    public class ProjectFunctions : Globals
    {
        // ---------------------------------------------------------- //
        // -------------------- Global Variables -------------------- //
        // ---------------------------------------------------------- //

        // ------------------ Lists ----------------- //

        public static List<ProjectSummaryRecord> FullProjectList;
        public static List<ProjectSummaryRecord> ProjectGridList;
        public static List<string> StatusFilterList;
        public static List<ProjectStages> FullStageList;
        public static List<ProjectTypes> FullTypeList;
        public static List<StaffSummaryRecord> FullPMsList;
        public static List<StaffSummaryRecord> PMFilterList;
        public static List<StaffSummaryRecord> PMOptionsList;
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
                                   //join c in existingPtDb.Clients on pj.ClientID equals c.ID
                                   //    into GroupJoin
                                   //from sc in GroupJoin.DefaultIfEmpty()
                                   //join pt in existingPtDb.ProjectTeams on pj.ID equals pt.ProjectID
                                   //join s in existingPtDb.Staff on pt.StaffID equals s.ID
                                   //join sr in existingPtDb.StaffRoles on s.RoleCode equals sr.RoleCode
                                   //join de in existingPtDb.Entities on s.DefaultEntity equals de.ID
                                   join ps in existingPtDb.ProjectStages on pj.StageCode equals ps.StageCode
                                   join t in existingPtDb.ProjectTypes on pj.TypeCode equals t.TypeCode
                                   where pj.EntityID == CurrentEntityID
                                       //&& (pt.ProjectRoleCode == ProjectManagerRole)
                                   select new ProjectSummaryRecord
                                   {
                                       ProjectID = pj.ID,
                                       ProjectCode = pj.ProjectCode,
                                       ProjectName = pj.ProjectName,
                                       ProjectSummary = pj.ProjectSummary,
                                       Type = t,
                                       EntityID = pj.EntityID,
                                       //Client = Globals.NoClient,
                                       //ProjectManager = new StaffSummaryRecord
                                       //{
                                       //    ID = (int)s.ID,
                                       //    UserID = s.UserID,
                                       //    StaffName = s.FirstName + " " + s.Surname,
                                       //    RoleCode = s.RoleCode,
                                       //    RoleDescription = sr.RoleDescription,
                                       //    StartDate = (DateTime?)DbFunctions.TruncateTime(s.StartDate),
                                       //    LeaveDate = (DateTime?)DbFunctions.TruncateTime(s.LeaveDate),
                                       //    ActiveUser = (bool)s.Active,
                                       //    DefaultEntity = de.EntityName
                                       //},
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
                            && ( inStatus == ProjectStatusFilter.All
                                    || (inStatus == ProjectStatusFilter.Current && fpl.Stage.StageCode <= LiveStage)
                                    || (inStatus == ProjectStatusFilter.Open && fpl.Stage.StageCode >= StartStage && fpl.Stage.StageCode <= LiveStage)
                                    || (inStatus == ProjectStatusFilter.InProgress && fpl.Stage.ProjectStatus == InProgressStatus)
                                    || (inStatus == ProjectStatusFilter.Closed && fpl.Stage.ProjectStatus == ClosedStatus)
                                )
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

        public static bool PopulateFromFilters(ref ProjectSummaryRecord thisSummary)
        {
            try
            {
                string question = "";
                StaffSummaryRecord manager = SelectedPMSummary;
                ClientSummaryRecord client = SelectedClientSummary;

                bool useManager = (manager.ID != 0 && manager.ActiveUser);
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
                         join sr in existingPtDb.StaffRoles on s.RoleCode equals sr.RoleCode
                         join se in existingPtDb.StaffEntities on s.ID equals se.StaffID
                         join de in existingPtDb.Entities on s.DefaultEntity equals de.ID
                         where se.EntityID == CurrentEntityID 
                            && (s.RoleCode == ProjectManagerRole || currentManagers.Contains(s.ID)) 
                         orderby new { s.FirstName, s.Surname, s.UserID }
                         select new StaffSummaryRecord
                         {
                             ID = (int)s.ID,
                             UserID = s.UserID,
                             StaffName = s.FirstName + " " + s.Surname,
                             RoleCode = s.RoleCode,
                             RoleDescription = sr.RoleDescription,
                             StartDate = (DateTime?)DbFunctions.TruncateTime(s.StartDate),
                             LeaveDate = (DateTime?)DbFunctions.TruncateTime(s.LeaveDate),
                             ActiveUser = (bool)s.Active,
                             DefaultEntity = de.EntityName
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
                comboList.Add(AnyPM);
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
                    managerList = FullPMsList.Where(fpl => fpl.ActiveUser || fpl.ID == currentManagerID).ToList();
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
                                && pt.ProjectRoleCode == ProjectManagerRole 
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
                DateTime today = DateTime.Today;
                DateTime yesterday = today.AddDays(-1);
                List<ProjectTeams> eligiblePMs = CurrentAndFuturePMs(projectID, yesterday);
                
                if (eligiblePMs.Count == 0) { return null; } // Shouldn't happen, but just in case
                else if (eligiblePMs.Count == 1) { currentPMRecord = eligiblePMs.First(); }
                else
                {
                    List<ProjectTeams> possiblePMs = eligiblePMs
                            .Where(ep => (ep.FromDate == null || ep.FromDate <= today) 
                                && (ep.ToDate == null || ep.ToDate >= today))
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
                int stage = (summary.Stage != null) ? summary.Stage.StageCode : -1;
                bool isUnderway = (summary.Stage != null && stage > StartStage && stage != CancelledStage);
                Projects existingProjectRecord = null;
                ClientSummaryRecord client = summary.Client ?? null;
                bool internalProject = (type == InternalProjectType);

                try
                {
                    if (client == null)
                    { errorDetails = ". Please select a client record from the drop-down list. If this is an internal project, select '" + NoRecord + "'|No Client Selected"; }
                    else if (!internalProject && !client.ActiveClient)
                    { errorDetails = ", as the selected client record is not active.|Inactive Client"; }
                    else if (summary.ProjectName == null || summary.ProjectName == "")
                    { errorDetails = ". Please enter a name for the project.|No Project Name"; }
                    else if (type == "")
                    { errorDetails = ". Please select a project type from the drop-down list.|No Type Selected"; }
                    else if (internalProject && client.ID != NoID)
                    {
                        errorDetails = ". Projects for clients cannot be internal. Please choose a different "
                            + "project type if the client selection is correct, or select '" + NoRecord + "' from the client drop-down for an internal project.|Client and Project Type Mismatch";
                    }
                    else if (!internalProject && client.ID == NoID)
                    {
                        errorDetails = ". Projects without clients must use the 'Internal project' type. "
                            + "Please choose the correct project type if this is an internal project, or select a client from the client drop-down.|Client and Project Type Mismatch";
                    }
                    else if (isUnderway && summary.StartDate == null)
                    {
                        errorDetails = ", as no start date has been set. Please enter a start date or keep the project in the 'Initation' stage."
                            + "|No Start Date";
                    }
                    else if (summary.ProjectManager == null)
                    {
                        errorDetails = ". Please select a Project Manager from the drop-down list. Try the 'Any' option if the required record is not listed, "
                            + "otherwise check that their account is active.|No Project Manager Selected";
                    }
                    else if (!summary.ProjectManager.ActiveUser)
                    { errorDetails = ", as the selected Project Manager is not an active user. Ask an administrator for help if required." + "|Inactive Project Manager"; }
                    else if (stage < 0)
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
                                    p => p.ID != summary.ProjectID
                                    && (p.ClientID == client.ID || (internalProject && (p.ClientID == null || p.ClientID <= 0)))
                                    ).ToList();
                                liveClientProducts = existingPtDb.ClientProducts.Where(
                                    cp => cp.ClientID == client.ID && cp.Live == true).ToList();
                                if (summary.ProjectID > 0)
                                {
                                    existingProjectRecord = existingPtDb.Projects.Where(p => p.ID == summary.ProjectID).FirstOrDefault();
                                    linkedProjectProducts = existingPtDb.ProjectProducts.Where(pp => pp.ProjectID == summary.ProjectID).ToList();
                                    //existingManagerID = existingPtDb.ProjectTeams
                                    //    .Where(pt => pt.ProjectID == summary.ProjectID && pt.ProjectRoleCode == ProjectManagerRole)
                                    //    .OrderByDescending(pt => pt.FromDate)
                                    //    .Select(pt => pt.StaffID).FirstOrDefault();
                                }
                            }

                            if (otherClientProjects.Exists(p => p.ProjectName == summary.ProjectName))
                            { errorDetails = ", as another project exists for the same client with the same name. Please change the project name."; }
                            else if (otherClientProjects.Exists(p => p.ProjectSummary == summary.ProjectSummary))
                            { errorDetails = ", as another project exists for the same client with the same project summary. Please change the summary details."; }
                            if (internalProject)
                            { errorDetails = errorDetails.Replace("another project exists for the same client", "another internal project exists"); }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error performing project validation against the database", generalException);
                            return false;
                        }
                    }

                    if (errorDetails != "")
                    {
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
                    if (managerChanged && summary.ProjectManager.RoleCode != ProjectManagerRole)
                    { queryDetails = queryDetails + "\n" + "Its Project Manager is not normally a Project Manager by role."; }
                    if (isUnderway && linkedProjectProducts == null)
                    { queryDetails = queryDetails + "\n" + "It is marked as underway, but has no linked products."; }
                    if (summary.StartDate == null && stage != CancelledStage) { queryDetails = queryDetails + "\n" + "It has no predicted start date at present."; }
                    if (summary.StartDate > DateTime.Today.AddDays(365)) { queryDetails = queryDetails + "\n" + "It starts more than a year in the future."; }
                    if ((existingProjectRecord == null || existingProjectRecord.StartDate == null || existingProjectRecord.StartDate > summary.StartDate)
                        && summary.StartDate < DateTime.Today.AddDays(-365)) { queryDetails = queryDetails + "\n" + "It starts more than a year in the past."; }
                    if (existingProjectRecord != null && existingProjectRecord.StageCode > stage)
                    { queryDetails = queryDetails + "\n" + "The new stage is less advanced than the previous one."; }
                    if (!internalProject)
                    {
                        if (liveClientProducts.Count == 0 && type != NewSiteType && type != TakeOnType)
                        { queryDetails = queryDetails + "\n" + "The project type indicates a change to an existing product, but this client has no Live products."; }
                        else if (liveClientProducts.Count != 0 && (type == NewSiteType))
                        { queryDetails = queryDetails + "\n" + "The project type indicates a brand new installation for a new client, but this client already has one or more Live products."; }
                    }

                    //else if (thisSummary. ) { queryDetails = queryDetails + "\n" + "."; }

                    // If there are products, type fits the products selected?? Or handle this when editing products
                    // Query if jumping a number of steps

                    if (queryDetails != "")
                    {
                        queryMessage = "Are you sure the details are correct? This project has one or more queries:\n" + queryDetails;
                        return MessageFunctions.WarningYesNo(queryMessage);
                    }
                    else if (stage == CancelledStage && existingProjectRecord != null && existingProjectRecord.StageCode != CancelledStage)
                    {
                        queryMessage = "Are you sure you wish to cancel this project?";
                        return MessageFunctions.WarningYesNo(queryMessage);
                    }
                    else if (summary.ProjectID == 0)
                    {
                        queryMessage = "Are you sure you wish to create this project? Project records cannot be deleted, although they can be cancelled.";
                        return MessageFunctions.WarningYesNo(queryMessage);
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

        public static bool ConvertSummaryToProject(ProjectSummaryRecord projectSummary, ref Projects project)
        {
            try
            {
                project.ID = projectSummary.ProjectID;
                project.EntityID = projectSummary.EntityID;
                project.ProjectCode = projectSummary.ProjectCode;
                project.TypeCode = projectSummary.Type.TypeCode;
                project.ProjectName = projectSummary.ProjectName;                
                project.StartDate = projectSummary.StartDate;
                project.StageCode = projectSummary.Stage.StageCode;
                project.ProjectSummary = projectSummary.ProjectSummary;
                if (projectSummary.Client.ID != NoID) { project.ClientID = projectSummary.Client.ID; } // 'No client' is null in the database)

                return true;
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error converting project summary to project record", generalException);
                return false;
            }
        }

        public static bool CreateProject(ref ProjectSummaryRecord projectSummary)
        {
            if (!ValidateProject(projectSummary, false, true)) { return false; }
            
            try
            {
                Projects thisProject = new Projects();
                bool converted = ConvertSummaryToProject(projectSummary, ref thisProject);
                if (!converted || thisProject == null) { return false; } // Errors should be thrown by the conversion
                ProjectTeams addPM = new ProjectTeams
                {
                    ProjectID = 0, // To be set below when the project is saved
                    StaffID = projectSummary.ProjectManager.ID,
                    ProjectRoleCode = ProjectManagerRole,
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

        public static bool SaveProjectChanges(ProjectSummaryRecord projectSummary, bool managerChanged)
        {
            if (!ValidateProject(projectSummary, true, managerChanged)) { return false; }

            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    Projects thisProject = existingPtDb.Projects.Where(p => p.ID == projectSummary.ProjectID).FirstOrDefault();
                    if (thisProject == null)
                    {
                        MessageFunctions.Error("Error saving project amendments to the database: no matching project found.", null);
                        return false;
                    }
                    bool converted = ConvertSummaryToProject(projectSummary, ref thisProject);
                    if (!converted) { return false; } // Errors should be thrown by the conversion

                    if (managerChanged)
                    {
                        DateTime today = DateTime.Today;
                        DateTime yesterday = today.AddDays(-1);
                        DateTime oneMonthAgo = today.AddMonths(-1);

                        ProjectTeams newPMRecord = new ProjectTeams
                        {
                            ProjectID = projectSummary.ProjectID,
                            StaffID = projectSummary.ProjectManager.ID,
                            ProjectRoleCode = ProjectManagerRole,
                            FromDate = (DateTime.Today < thisProject.StartDate) ? DateTime.Today : thisProject.StartDate
                        };

                        List<ProjectTeams> existingPMs = CurrentAndFuturePMs(projectSummary.ProjectID, yesterday);
                        if (existingPMs.Count == 0) { existingPtDb.ProjectTeams.Add(newPMRecord); } // Shouldn't happen, but just in case
                        else
                        {
                            ProjectTeams lastPMRecord = existingPMs.First();
                            lastPMRecord = existingPtDb.ProjectTeams.Find(lastPMRecord.ID); // Get it from the database again so we can amend/remove it

                            if (lastPMRecord.FromDate != null && lastPMRecord.FromDate > oneMonthAgo) // To do: also ask if the project is in the early stages
                            {
                                Staff lastPM = StaffFunctions.GetStaffMember(lastPMRecord.StaffID);
                                string lastPMName = lastPM.FirstName + " " + lastPM.Surname;
                                DateTime fromDateTime = (DateTime) lastPMRecord.FromDate;
                                string fromDate = fromDateTime.ToString("dd MMMM yyyy");
                                bool overwrite = MessageFunctions.WarningYesNo("A Project Manager history record exists for " + lastPMName + " starting on " + fromDate 
                                    + ". Should it be overwritten? Selecting 'Yes' will replace that record; 'No' will retain it in the history.", "Replace Project Manager Record?");
                                if (overwrite) 
                                {
                                    existingPtDb.ProjectTeams.Remove(lastPMRecord);
                                    existingPtDb.ProjectTeams.Add(newPMRecord);
                                }
                                else if (lastPMRecord.FromDate > today)
                                {
                                    DateTime toDate = (DateTime)lastPMRecord.FromDate;
                                    toDate = toDate.AddDays(-1);
                                    newPMRecord.ToDate = toDate;
                                    existingPtDb.ProjectTeams.Add(newPMRecord);
                                }
                                else
                                {
                                    lastPMRecord.ToDate = yesterday;
                                    newPMRecord.FromDate = today;
                                    existingPtDb.ProjectTeams.Add(newPMRecord);
                                }
                            }
                            else
                            {
                                lastPMRecord.ToDate = yesterday;
                                newPMRecord.FromDate = today;
                                existingPtDb.ProjectTeams.Add(newPMRecord);
                            }
                        }
                    }

                    existingPtDb.SaveChanges();
                    SelectedProjectSummary = projectSummary;

                    // To Do: if going Live, update products if relevant - check with user first!


                    // To Do: add congratulations if Live or Closed!
                    MessageFunctions.SuccessMessage("Project amendments saved successfully.", "Changes Saved");
                    return true;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error saving project amendments to the database", generalException);
                return false;
            }		            
        }

    } // class
} // namespace
