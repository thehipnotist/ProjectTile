using System;
using System.Text.RegularExpressions;

namespace ProjectTile
{
    public class Globals
    {                   
        // Current login and security details
        public static Staff MyStaffRecord;
        public static int MyStaffID;
        public static string MayName = "";
        public static string MyUserID;
        
        public static int CurrentEntityID;
        public static string CurrentEntityName = "";
        public static Entities CurrentEntity;
        public static int MyDefaultEntityID;
        public static string MyDefaultEntityName = "";

        public static TableSecurity MyPermissions;
        public const string DbUserPrefix = "ProT_";

        // Common enumerations
        public enum ClientProductStatus { Added = 1, New = 2, InProgress = 3, Live = 4, Updates = 5, Inactive = 6, Retired = 7 }
        public enum ProjectStatusFilter { All, Current, Open, InProgress, Closed }
        public enum TeamTimeFilter { All, Future, Current }
        
        // Additional records for all/any/none
        public const string AllRecords = "<All>";
        public const string AnyRecord = "<Any or None>";
        public const string NoRecord = "<None (Internal Only)>";
        public const string SearchRecords = "<Search...>";
        public const int NoID = -1;
        public const string AllCodes = "!!";

        public static ProjectSummaryRecord SearchProjects = new ProjectSummaryRecord { ProjectID = -1, ProjectCode = SearchRecords, ProjectName = SearchRecords };
        public static ProjectSummaryRecord AllProjects = new ProjectSummaryRecord { ProjectID = 0, ProjectCode = AllRecords, ProjectName = AllRecords };
        public static ClientSummaryRecord AnyClient = new ClientSummaryRecord { ID = 0, ClientCode = "ANY", ClientName = AnyRecord, EntityID = CurrentEntityID, ActiveClient = false };
        public static ClientSummaryRecord NoClient = new ClientSummaryRecord { ID = NoID, ClientCode = "NONE", ClientName = NoRecord, EntityID = CurrentEntityID, ActiveClient = true };
        public static StaffSummaryRecord AllPMs = new StaffSummaryRecord { ID = 0, FirstName = AllRecords, Surname = "", Active = false };
        public static ProjectRoles AllRoles = new ProjectRoles { RoleCode = AllCodes, RoleDescription = AllRecords };        

        // Project roles
        public const string AccountManagerCode = "AM";
        public const string TechnicalManagerCode = "TM";
        public const string SponsorCode = "PS";
        public const string ProjectManagerCode = "PM";
        public const string SeniorConsultantCode = "SC";
        public const string TechnicalLeadCode = "TL";
        public const string OtherRoleCode = "OT";
        public const string IntegrationConsultCode = "IC";
        public const string ApplicationConsultCode = "AC";
        public const string TechnicalConsultCode = "TC";
        public static string[] ProjectRoleHeirarchy = { SponsorCode, ProjectManagerCode, SeniorConsultantCode, TechnicalLeadCode, ApplicationConsultCode, TechnicalConsultCode, 
                                                          IntegrationConsultCode, OtherRoleCode };
        public static string[] KeyInternalRoles = { SponsorCode, ProjectManagerCode, SeniorConsultantCode, TechnicalLeadCode };
        public static string[] KeyClientRoles = { SponsorCode, ProjectManagerCode, TechnicalLeadCode };

        // Project types
        public const string InternalProjectCode = "IP";
        public const string NewSiteCode = "NS";
        public const string TakeOnCode = "TS";
        public const string AddSystemCode = "AS";
        public static string[] NewProductTypeCodes = { NewSiteCode, AddSystemCode };       

        // Project stage/status
        public const int StartStage = 1; // First stage of 'In Progress'
        public const int LiveStage = 12; // First stage of 'Live'
        public const int CompletedStage = 15; // First stage of 'Closed'
        public const int CancelledStage = 99;
        public const string InProgressStatus = "In Progress";
        public const string ClosedStatus = "Closed";

        // Default records
        public static ClientSummaryRecord DefaultClientSummary = AnyClient;
        public static StaffSummaryRecord DefaultPMSummary = AllPMs;
        public static ProjectStatusFilter DefaultStatusFilter = ProjectStatusFilter.Current;
        public static ProjectRoles DefaultProjectRole = AllRoles;
        public static ProjectSummaryRecord DefaultProjectSummary = AllProjects;
        public static TeamTimeFilter DefaultTeamTimeFilter = TeamTimeFilter.Future;

        // Selected records affecting multiple pages
        public static Clients SelectedClient = null;
        public static Staff SelectedStaffMember = null;

        public static ClientSummaryRecord SelectedClientSummary = DefaultClientSummary;
        public static StaffSummaryRecord SelectedPMSummary = DefaultPMSummary;
        public static ProjectSummaryRecord SelectedProjectSummary = DefaultProjectSummary;

        public static ProjectStatusFilter SelectedStatusFilter = DefaultStatusFilter;
        public static ProjectRoles SelectedProjectRole = DefaultProjectRole;
        public static TeamTimeFilter SelectedTeamTimeFilter = DefaultTeamTimeFilter;

        // Page references
        public static string TilesPageName = "TilesPage";
        public const string TilesPageURI = "TilesPage.xaml";
        public static string ClientSourcePage = TilesPageName;
        public static string ClientSourceMode = PageFunctions.None;
        public static string ProjectSourcePage = TilesPageName;
        public static string ProjectSourceMode = PageFunctions.None;

        // Formats
        public static Regex ClientVersionFormat = new Regex("^[0-9]{0,2}[.]{0,1}[0-9]{0,1}$");
        public static Regex LatestVersionFormat = new Regex("^[0-9]{0,2}[.]{0,1}[0-9]{0,2}$");

        // Dates
        public static DateTime Today = DateTime.Today;
        public static DateTime Yesterday = Today.AddDays(-1);
        public static DateTime OneMonthAgo = Today.AddMonths(-1);
        public static DateTime StartOfTime = Today.AddYears(-999);
        public static DateTime InfiniteDate = Today.AddYears(999);

        // Shared methods
        public static void ResetClientParameters()
        {
            SelectedClient = null;
            ClientSourcePage = TilesPageName;
            ClientSourceMode = PageFunctions.None;
        }        
        
        public static void ResetProjectParameters()
        {
            SelectedClientSummary = DefaultClientSummary;
            SelectedPMSummary = DefaultPMSummary;
            SelectedStatusFilter = DefaultStatusFilter;
            SelectedProjectSummary = DefaultProjectSummary;
            SelectedProjectRole = DefaultProjectRole;
            SelectedTeamTimeFilter = DefaultTeamTimeFilter;
            ProjectSourcePage = TilesPageName;
            ProjectSourceMode = PageFunctions.None;
        }

    } // class
} // namespace
