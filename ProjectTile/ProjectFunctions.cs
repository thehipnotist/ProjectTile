using System;
using System.Collections.Generic;
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
        public static List<StaffSummaryRecord> FullPMsList;
        public static List<StaffSummaryRecord> PMComboList;
        public static List<ClientSummaryRecord> FullClientList = ClientFunctions.ClientGridList(false, "", 0, CurrentEntityID);
        public static List<ClientSummaryRecord> ClientComboList;

        // Additional records (any, all or none)
        public static ClientSummaryRecord AnyClient = new ClientSummaryRecord { ID = 0, ClientCode = "ANY", ClientName = AnyRecord, EntityID = CurrentEntityID };
        public static ClientSummaryRecord NoClient = new ClientSummaryRecord { ID = NoID, ClientCode = "NONE", ClientName = NoRecord, EntityID = CurrentEntityID };
        public static StaffSummaryRecord AnyPM = new StaffSummaryRecord { ID = 0, StaffName = AllRecords };

        // Default records
        public static ClientSummaryRecord DefaultClientSummary = AnyClient;
        public static StaffSummaryRecord DefaultPMSummary = AnyPM;

        // Selected records
        public static ClientSummaryRecord SelectedClientSummary; //= DefaultClientSummary;
        public static StaffSummaryRecord SelectedPMSummary; //= DefaultPMSummary;

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
                             TypeCode = pj.TypeCode,
                             TypeName = t.TypeName,
                             TypeDescription = t.TypeDescription,
                             EntityID = pj.EntityID,
                             ClientID = (sc == null) ? NoID : sc.ID,
                             ClientCode = (sc == null) ? "" : sc.ClientCode,
                             ClientName = (sc == null) ? "" : sc.ClientName,
                             PMStaffID = s.ID,
                             PMStaffName = s.FirstName + " " + s.Surname,
                             StageCode = pj.StageCode,
                             StageName = ps.StageName,
                             StageDescription = ps.StageDescription,
                             ProjectStatus = ps.ProjectStatus,
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

        public static bool SetProjectGridList(ProjectStatus inStatus, int clientID = 0, int ourManagerID = 0)
        {
            try
            {
                bool success = SetFullProjectList();
                if (success)
                {
                    ProjectGridList = 
                        (from fpl in FullProjectList
                        where  (clientID == 0 || fpl.ClientID == clientID)
                            &&  (ourManagerID == 0 || fpl.PMStaffID == ourManagerID)
                            && ( inStatus == ProjectStatus.All
                                    || (inStatus == ProjectStatus.Current && fpl.StageCode <= LiveStage)
                                    || (inStatus == ProjectStatus.Open && fpl.StageCode >= StartStage && fpl.StageCode <= LiveStage) 
                                    || (inStatus == ProjectStatus.InProgress && fpl.ProjectStatus == InProgressStatus) 
                                    || (inStatus == ProjectStatus.Closed && fpl.ProjectStatus == ClosedStatus)
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

        // Project Managers (internal)
        public static void SetFullPMsList()
        {
            List<int> currentManagers;
            List<StaffSummaryRecord> managersList;
            
            try
            {

                currentManagers = (from fpl in FullProjectList
                                   select fpl.PMStaffID).Distinct().ToList();               
                
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

        public static void SetPMComboList()
        {
            try
            {
                List<StaffSummaryRecord> comboList = FullPMsList;
                comboList.Add(AnyPM);
                PMComboList = comboList;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving data for Project Managers drop-down list", generalException); }
        }

        // Clients
        public static void SetClientComboList()
        {
            try
            {
                List<ClientSummaryRecord> comboList = FullClientList;
                comboList.Add(AnyClient);
                comboList.Add(NoClient);
                ClientComboList = comboList;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving data for client drop-down list", generalException); }
        }

    } // class
} // namespace
