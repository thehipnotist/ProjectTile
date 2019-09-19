using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace ProjectTile
{
    public class ProjectFunctions
    {
        // Globals
        public const string ProjectManagerRole = "PM";
        private static int thisEntityID = EntityFunctions.CurrentEntityID;

        // Stage/status
        public enum StatusFilter { All, Current, Open, InProgress, Closed }
        public const int StartStage = 2;
        public const int LiveStage = 11;
        public const int ClosedStage = 15;
        public const string InProgressStatus = "In Progress";
        public const string ClosedStatus = "Closed";

        // Lists
        public static List<ProjectSummaryRecord> FullProjectList;
        public static List<ProjectSummaryRecord> ProjectGridList;
        public static List<StaffGridRecord> FullPMsList;
        public static List<StaffGridRecord> PMComboList;
        public static List<ClientGridRecord> FullClientList = ClientFunctions.ClientGridList(false, "", 0, thisEntityID);
        public static List<ClientGridRecord> ClientComboList;

        // Additional records (any, all or none)
        public static ClientGridRecord AnyClient = new ClientGridRecord { ID = 0, ClientCode = "ANY", ClientName = PageFunctions.AnyRecord, EntityID = thisEntityID };
        public static ClientGridRecord NoClient = new ClientGridRecord { ID = PageFunctions.NoID, ClientCode = "NONE", ClientName = PageFunctions.NoRecord, EntityID = thisEntityID };
        public static StaffGridRecord AnyPM = new StaffGridRecord { ID = 0, StaffName = PageFunctions.AllRecords };

        // Default records
        public static ClientGridRecord DefaultClientSummary = AnyClient;
        public static StaffGridRecord DefaultPMSummary = AnyPM;

        // Selected records
        public static ClientGridRecord SelectedClientSummary; //= DefaultClientSummary;
        public static StaffGridRecord SelectedPMSummary; //= DefaultPMSummary;

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
                         where pj.EntityID == thisEntityID
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
                             ClientID = (sc == null) ? PageFunctions.NoID : sc.ID,
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

        public static bool SetProjectGridList(StatusFilter inStatus, int clientID = 0, int ourManagerID = 0)
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
                            && ( inStatus == StatusFilter.All
                                    || (inStatus == StatusFilter.Current && fpl.StageCode <= LiveStage)
                                    || (inStatus == StatusFilter.Open && fpl.StageCode >= StartStage && fpl.StageCode <= LiveStage) 
                                    || (inStatus == StatusFilter.InProgress && fpl.ProjectStatus == InProgressStatus) 
                                    || (inStatus == StatusFilter.Closed && fpl.ProjectStatus == ClosedStatus)
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
            List<StaffGridRecord> managersList;
            
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
                         where se.EntityID == thisEntityID 
                            && (s.RoleCode == ProjectManagerRole || currentManagers.Contains(s.ID)) 
                         orderby new { s.FirstName, s.Surname, s.UserID }
                         select new StaffGridRecord
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
                List<StaffGridRecord> comboList = FullPMsList;
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
                List<ClientGridRecord> comboList = FullClientList;
                comboList.Add(AnyClient);
                comboList.Add(NoClient);
                ClientComboList = comboList;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving data for client drop-down list", generalException); }
        }

    } // class
} // namespace
