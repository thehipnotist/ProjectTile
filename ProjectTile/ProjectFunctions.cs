using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectTile
{
    public class ProjectFunctions
    {
        public static List<ProjectSummaryRecord> ProjectGridList;
        public const string ProjectManagerRole = "PM";
        public static int SelectedClientID = 0;
        public static int SelectedPMStaffID = 0;

        public const int StartStage = 2;
        public const int LiveStage = 11;

        // Data retrieval
        public static bool SetProjectGridList(int entityID, bool openOnly, int clientID = 0, int ourManagerID = 0)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    ProjectGridList = 
                        (from pj in existingPtDb.Projects
                            join c in existingPtDb.Clients on pj.ClientID equals c.ID
                                into GroupJoin from sc in GroupJoin.DefaultIfEmpty()
                            join pt in existingPtDb.ProjectTeams on pj.ID equals pt.ProjectID
                            join s in existingPtDb.Staff on pt.StaffID equals s.ID
                            join ps in existingPtDb.ProjectStages on pj.StageCode equals ps.StageCode
                            join t in existingPtDb.ProjectTypes on pj.TypeCode equals t.TypeCode
                        where pj.EntityID == entityID
                            && (clientID == 0 || sc.ID == clientID)
                            && (pt.ProjectRoleCode == ProjectManagerRole)
                            && (ourManagerID == 0 || s.ID == ourManagerID)
                            && (!openOnly || (pj.StageCode >= StartStage && pj.StageCode <= LiveStage ) ) 
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
                            ClientID = (sc == null)? 0 : sc.ID,
                            ClientCode = (sc == null)? "" : sc.ClientCode,
                            ClientName = (sc == null)? "" : sc.ClientName,
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
                MessageFunctions.Error("Error retrieving project grid data", generalException);
                return false;
            }		

        }

    }
}
