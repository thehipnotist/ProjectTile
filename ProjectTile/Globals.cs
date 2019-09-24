using System.Text.RegularExpressions;

namespace ProjectTile
{
    public class Globals
    {                   
        // Current login and security details
        public static Staff CurrentUser;
        public static int CurrentStaffID;
        public static string CurrentStaffName = "";
        public static string CurrentUserID;
        
        public static int CurrentEntityID;
        public static string CurrentEntityName = "";
        public static Entities CurrentEntity;

        public static int DefaultEntityID;
        public static string DefaultEntityName = "";

        public static TableSecurity MyPermissions;

        // Common enumerations
        public enum ClientProductStatus { Added = 1, New = 2, InProgress = 3, Live = 4, Updates = 5, Inactive = 6, Retired = 7 }
        public enum ProjectStatusFilter { All, Current, Open, InProgress, Closed }
        
        // Additional records for all/any/none
        public const string AllRecords = "<All>";
        public const string AnyRecord = "<Any or None>";
        public const string NoRecord = "<None (Internal Only)>";
        public const int NoID = -1;
        public static ClientSummaryRecord AnyClient = new ClientSummaryRecord { ID = 0, ClientCode = "ANY", ClientName = AnyRecord, EntityID = CurrentEntityID };
        public static ClientSummaryRecord NoClient = new ClientSummaryRecord { ID = NoID, ClientCode = "NONE", ClientName = NoRecord, EntityID = CurrentEntityID };
        public static StaffSummaryRecord AnyPM = new StaffSummaryRecord { ID = 0, StaffName = AllRecords };

        // Standard universal strings
        public const string ManagerRole = "AM";
        public const string ProjectManagerRole = "PM";
        public const string DbUserPrefix = "ProT_";

        // Default records
        public static ClientSummaryRecord DefaultClientSummary = AnyClient;
        public static StaffSummaryRecord DefaultPMSummary = AnyPM;
        public static ProjectStatusFilter DefaultStatusFilter = ProjectStatusFilter.Current;

        // Selected records affecting multiple pages
        public static Clients SelectedClient = null;
        public static Staff SelectedStaffMember = null;

        public static ClientSummaryRecord SelectedClientSummary = DefaultClientSummary;
        public static StaffSummaryRecord SelectedPMSummary = DefaultPMSummary;
        public static ProjectSummaryRecord SelectedProjectSummary = null;

        public static ProjectStatusFilter SelectedStatusFilter = ProjectStatusFilter.Current;

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

        // Project stage/status
        public const int StartStage = 2;
        public const int LiveStage = 11;
        public const int ClosedStage = 15;
        public const int CancelledStage = 99;
        public const string InProgressStatus = "In Progress";
        public const string ClosedStatus = "Closed";

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
            ProjectSourcePage = TilesPageName;
            ProjectSourceMode = PageFunctions.None;
        }

    } // class
} // namespace
