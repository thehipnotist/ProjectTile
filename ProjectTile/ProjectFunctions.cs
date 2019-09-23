﻿using System;
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
        // Lists
        public static List<ProjectSummaryRecord> FullProjectList;
        public static List<ProjectSummaryRecord> ProjectGridList;
        public static List<ClientSummaryRecord> FullClientList;
        public static List<ClientSummaryRecord> ClientComboList;
        public static List<string> StatusFilterList;
        public static List<ProjectStages> FullStageList;
        public static List<ProjectTypes> FullTypeList;
        public static List<StaffSummaryRecord> FullPMsList;
        public static List<StaffSummaryRecord> PMFilterList;
        public static List<StaffSummaryRecord> PMOptionsList;

        // Data retrieval
        public static bool SetFullProjectList()
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    FullProjectList =
                        (from pj in existingPtDb.Projects
                         join c in existingPtDb.Clients on pj.ClientID equals c.ID
                             into GroupJoin
                         from sc in GroupJoin.DefaultIfEmpty()
                         join pt in existingPtDb.ProjectTeams on pj.ID equals pt.ProjectID
                         join s in existingPtDb.Staff on pt.StaffID equals s.ID
                         join sr in existingPtDb.StaffRoles on s.RoleCode equals sr.RoleCode
                         join de in existingPtDb.Entities on s.DefaultEntity equals de.ID
                         join ps in existingPtDb.ProjectStages on pj.StageCode equals ps.StageCode
                         join t in existingPtDb.ProjectTypes on pj.TypeCode equals t.TypeCode
                         where pj.EntityID == CurrentEntityID
                             && (pt.ProjectRoleCode == ProjectManagerRole)
                         select new ProjectSummaryRecord
                         {
                             ProjectID = pj.ID,
                             ProjectCode = pj.ProjectCode,
                             ProjectName = pj.ProjectName,
                             ProjectSummary = pj.ProjectSummary,
                             ProjectType = t,
                             EntityID = pj.EntityID,
                             ClientID = (sc == null) ? NoID : sc.ID,
                             ClientCode = (sc == null) ? "" : sc.ClientCode,
                             ClientName = (sc == null) ? "" : sc.ClientName,
                             ProjectManager = new StaffSummaryRecord
                             {
                                 ID = (int)s.ID,
                                 UserID = (string)s.UserID,
                                 StaffName = (string)s.FirstName + " " + s.Surname,
                                 RoleDescription = (string)sr.RoleDescription,
                                 StartDate = (DateTime?)DbFunctions.TruncateTime(s.StartDate),
                                 LeaveDate = (DateTime?)DbFunctions.TruncateTime(s.LeaveDate),
                                 ActiveUser = (bool)s.Active,
                                 DefaultEntity = (string)de.EntityName
                             },
                             //PMStaffID = s.ID,
                             //PMStaffName = s.FirstName + " " + s.Surname,
                             ProjectStage = ps,
                             StartDate = pj.StartDate
                         }
                        ).ToList();

                    return true;
                }
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
                        where  (clientID == 0 || fpl.ClientID == clientID)
                            &&  (ourManagerID == 0 || fpl.ProjectManager.ID == ourManagerID)
                            && ( inStatus == ProjectStatusFilter.All
                                    || (inStatus == ProjectStatusFilter.Current && fpl.ProjectStage.StageCode <= LiveStage)
                                    || (inStatus == ProjectStatusFilter.Open && fpl.ProjectStage.StageCode >= StartStage && fpl.ProjectStage.StageCode <= LiveStage)
                                    || (inStatus == ProjectStatusFilter.InProgress && fpl.ProjectStage.ProjectStatus == InProgressStatus)
                                    || (inStatus == ProjectStatusFilter.Closed && fpl.ProjectStage.ProjectStatus == ClosedStatus)
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
                SetFullStageList();
                int lastStageCode = FullStageList.OrderByDescending(fsl => fsl.StageCode).FirstOrDefault().StageCode;
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
                             UserID = (string)s.UserID,
                             StaffName = (string)s.FirstName + " " + s.Surname,
                             RoleDescription = (string)sr.RoleDescription,
                             StartDate = (DateTime?)DbFunctions.TruncateTime(s.StartDate),
                             LeaveDate = (DateTime?)DbFunctions.TruncateTime(s.LeaveDate),
                             ActiveUser = (bool)s.Active,
                             DefaultEntity = (string)de.EntityName
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
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving data for Project Managers drop-down list", generalException); }
        }

        public static void SetPMOptionsList(int currentManagerID = 0)
        {
            try
            {
                SetFullPMsList();
                PMOptionsList = FullPMsList.Where(fpl => fpl.ActiveUser || fpl.ID == currentManagerID).ToList();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving data for Project Managers drop-down list", generalException); }
        }

        public static StaffSummaryRecord GetPMSummary(int managerID)
        {
            try
            {
                //SetFullPMsList();
                StaffSummaryRecord thisPM = PMOptionsList.Where(fpl => fpl.ID == managerID).FirstOrDefault();
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

        // Clients
        public static void SetFullClientList()
        {
            FullClientList = ClientFunctions.ClientGridList(false, "", 0, CurrentEntityID);
        }
        
        public static void SetClientComboList()
        {
            try
            {
                if (ClientComboList != null) { ClientComboList.Clear(); }
                SetFullClientList();
                List<ClientSummaryRecord> comboList = FullClientList;
                comboList.Add(AnyClient);
                comboList.Insert(0, NoClient);
                ClientComboList = comboList;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving data for client drop-down list", generalException); }
        }

        // Navigation
        public static void ReturnToTilesPage()
        {
            ResetProjectParameters();
            PageFunctions.ShowTilesPage();
        }

        public static void ReturnToProjectPage()
        {
            PageFunctions.ShowProjectPage(ProjectSourceMode);
        }

    } // class



} // namespace
