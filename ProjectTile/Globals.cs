using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        
        // Additional records for all/any/none
        public const string AllRecords = "<All>";
        public const string AnyRecord = "<Any or None>";
        public const string NoRecord = "<None (Internal Only)>";
        public const int NoID = -1;

        // Standard universal strings
        public const string ManagerRole = "AM";
        public const string ProjectManagerRole = "PM";
        public const string DbUserPrefix = "ProT_";

        // Selected records affecting multiple pages
        public static Clients SelectedClient = null;
        public static Staff SelectedStaffMember = null;

        // Common enumerations
        public enum ClientProductStatus { Added = 1, New = 2, InProgress = 3, Live = 4, Updates = 5, Inactive = 6, Retired = 7 }
        public enum ProjectStatus { All, Current, Open, InProgress, Closed }

        // Page references
        public static string TilesPageName = "TilesPage";
        public const string TilesPageURI = "TilesPage.xaml";

        // Formats
        public static Regex ClientVersionFormat = new Regex("^[0-9]{0,2}[.]{0,1}[0-9]{0,1}$");
        public static Regex LatestVersionFormat = new Regex("^[0-9]{0,2}[.]{0,1}[0-9]{0,2}$");

        // Project stage/status
        public const int StartStage = 2;
        public const int LiveStage = 11;
        public const int ClosedStage = 15;
        public const string InProgressStatus = "In Progress";
        public const string ClosedStatus = "Closed";

    } // class
} // namespace
