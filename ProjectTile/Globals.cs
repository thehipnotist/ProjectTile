﻿using System;
using System.Text.RegularExpressions;

namespace ProjectTile
{
    public class Globals
    {                   
        // Current login and security details
        public static Staff MyStaffRecord;
        public static int MyStaffID;
        public static string MyName = "";
        public static string MyUserID;
        
        public static int CurrentEntityID;
        public static string CurrentEntityName = "";
        public static Entities CurrentEntity;
        public static int MyDefaultEntityID;
        public static string MyDefaultEntityName = "";

        public static TableSecurity MyPermissions;
        public static int FavouriteProjectID = 0;
        public const string DbUserPrefix = "ProT_";

        // Status variables
        public static bool InfoMessageDisplaying = false;
        public static bool LoadingActions = false;

        // Common enumerations
        public enum ClientProductStatus { Added = 1, New = 2, InProgress = 3, Live = 4, Updates = 5, Inactive = 6, Retired = 7 }
        public enum ProjectStatusFilter { All, Current, Open, InProgress, Closed }
        public enum TeamTimeFilter { All, Future, Current }
        public enum TimelineType { Target, Actual, Effective }
        
        // Additional records for all/any/none
        public const string AllRecords = "<All>";
        public const string AnyRecord = "<Any or None>";
        public const string NoRecord = "<None (Internal Only)>";
        public const string SearchRecords = "<Search...>";
        public const int NoID = -1;
        public const string AllCodes = "!!";
        public const string PleaseSelect = "<Please select...>";

        public static Entities AllEntities = new Entities { ID = 0, EntityName = AllRecords, EntityDescription = AllRecords };
        public static ProjectProxy SearchProjects = new ProjectProxy { ProjectID = -1, ProjectCode = SearchRecords, ProjectName = SearchRecords };
        public static ProjectProxy AllProjects = new ProjectProxy { ProjectID = 0, ProjectCode = AllRecords, ProjectName = AllRecords };
        public static ClientProxy AnyClient = new ClientProxy { ID = 0, ClientCode = "ANY", ClientName = AnyRecord, EntityID = CurrentEntityID, ActiveClient = false };
        public static ClientProxy NoClient = new ClientProxy { ID = NoID, ClientCode = "NONE", ClientName = NoRecord, EntityID = CurrentEntityID, ActiveClient = true };
        public static StaffProxy AllPMs = new StaffProxy { ID = 0, FirstName = AllRecords, Surname = "", Active = false };
        public static StaffRoles AllStaffRoles = new StaffRoles { RoleCode = AllCodes, RoleDescription = AllRecords };
        public static ProjectRoles AllProjectRoles = new ProjectRoles { RoleCode = AllCodes, RoleDescription = AllRecords };
        public static ClientTeamRoles AllClientRoles = new ClientTeamRoles { RoleCode = AllCodes, RoleDescription = AllRecords };
        public static StaffProxy AllStaff = new StaffProxy { ID = 0, FirstName = AllRecords, Surname = "", Active = true, UserID = AllCodes };
        public static ProjectStages NoStage = new ProjectStages { ID = NoID, StageNumber = NoID, StageName = "", StageDescription = "" };
        public static ProjectStages AllStages = new ProjectStages { ID = NoID, StageNumber = NoID, StageName = "<All Stages>", StageDescription = "<All Stages>" };

        // Project roles
        public const string AccountManagerCode = "AM";
        public const string TechnicalManagerCode = "TM";
        public const string SponsorCode = "PS";
        public const string ProjectManagerCode = "PM";
        public const string SeniorConsultantCode = "SC";
        public const string OurTechLeadCode = "TL";
        public const string ClientTechLeadCode = "IL";
        public const string OtherRoleCode = "OT";
        public const string IntegrationConsultCode = "IC";
        public const string ApplicationConsultCode = "AC";
        public const string TechnicalConsultCode = "TC";
        public static string[] ProjectRoleHeirarchy = { SponsorCode, ProjectManagerCode, SeniorConsultantCode, OurTechLeadCode, ClientTechLeadCode, ApplicationConsultCode, TechnicalConsultCode, 
                                                          IntegrationConsultCode, OtherRoleCode };
        public static string[] KeyInternalRoles = { SponsorCode, ProjectManagerCode, SeniorConsultantCode, OurTechLeadCode };
        public static string[] KeyClientRoles = { SponsorCode, ProjectManagerCode, ClientTechLeadCode };

        // Project types
        public const string InternalProjectCode = "IP";
        public const string NewSiteCode = "NS";
        public const string TakeOnCode = "TS";
        public const string AddSystemCode = "AS";
        public static string[] NewProductTypeCodes = { NewSiteCode, AddSystemCode };       

        // Project stage/status
        public const int StartStage = 1; // First stage of 'In Progress'
        public const int LiveStage = 13; // First stage of 'Live'
        public const int CompletedStage = 16; // First stage of 'Closed'
        public const int CancelledStage = 99;
        public const string InProgressStatus = "In Progress";
        public const string LiveStatus = "Live";
        public const string ClosedStatus = "Closed";

        // Dates
        public static DateTime Today = DateTime.Today;
        public static DateTime Yesterday = Today.AddDays(-1);
        public static DateTime OneMonthAgo = Today.AddMonths(-1);
        public static DateTime OneMonthAhead = Today.AddMonths(1);
        public static DateTime StartOfTime = new DateTime(2000, 1, 1);
        public static DateTime InfiniteDate = DateTime.MaxValue;
        public static DateTime StartOfMonth = new DateTime(Today.Year, Today.Month, 1);
        public static DateTime StartOfYear = new DateTime(Today.Year, 1, 1);
        public static DateTime EndOfYear = new DateTime(Today.Year, 12, 31);

        // Default records
        public static ClientProxy DefaultClientProxy = AnyClient;
        public static StaffProxy DefaultPMProxy = AllPMs;
        public static ProjectStatusFilter DefaultStatusFilter = ProjectStatusFilter.Current;
        public static ProjectRoles DefaultProjectRole = AllProjectRoles;
        public static ClientTeamRoles DefaultClientRole = AllClientRoles;

        public static ProjectProxy DefaultProjectProxy = AllProjects;
        public static TeamTimeFilter DefaultTeamTimeFilter = TeamTimeFilter.Future;
        public static ProjectStages DefaultStage = AllStages;
        public static TimelineType DefaultTimelineType = TimelineType.Effective;
        public static DateTime DefaultFromDate = (StartOfYear < OneMonthAgo)? StartOfYear : OneMonthAgo;
        public static DateTime DefaultToDate = (EndOfYear > OneMonthAhead)? EndOfYear : OneMonthAhead;

        // Selected records affecting multiple pages
        public static Clients SelectedClient = null;
        public static Staff SelectedStaffMember = null;
        public static CombinedStaffMember SelectedOwner = null;

        public static ClientProxy SelectedClientProxy = DefaultClientProxy;
        public static StaffProxy SelectedPMProxy = DefaultPMProxy;
        public static ProjectProxy SelectedProjectProxy = DefaultProjectProxy;

        public static ProjectStatusFilter SelectedStatusFilter = DefaultStatusFilter;
        public static ProjectRoles SelectedProjectRole = DefaultProjectRole;
        public static ClientTeamRoles SelectedClientRole = DefaultClientRole;
        public static TeamTimeFilter SelectedTeamTimeFilter = DefaultTeamTimeFilter;

        public static ProjectStages SelectedStage = DefaultStage;
        public static TimelineType SelectedTimelineType = DefaultTimelineType;
        public static DateTime SelectedFromDate = DefaultFromDate;
        public static DateTime SelectedToDate = DefaultToDate;
        public static StageHistory SelectedHistory = null;

        // Page references
        public static string TilesPageName = "TilesPage";
        public const string TilesPageURI = "TilesPage.xaml";
        public static string ProductSourcePage = TilesPageName; 
        public static string ClientSourcePage = TilesPageName;
        public static string ClientSourceMode = PageFunctions.None;
        public static string ProjectSourcePage = TilesPageName;
        public static string ProjectSourceMode = PageFunctions.None;

        // Formats
        public static Regex ClientVersionFormat = new Regex("^[0-9]{0,2}[.]{0,1}[0-9]{0,1}$");
        public static Regex LatestVersionFormat = new Regex("^[0-9]{0,2}[.]{0,1}[0-9]{0,2}$");

        // Shared methods
        public static void ResetProductParameters()
        {
            Globals.ProductSourcePage = TilesPageName;
        }
        
        public static void ResetClientParameters()
        {
            SelectedClient = null;
            ClientSourcePage = TilesPageName;
            ClientSourceMode = PageFunctions.None;
        }        
        
        public static void ResetProjectParameters()
        {
            SelectedClientProxy = DefaultClientProxy;
            SelectedPMProxy = DefaultPMProxy;
            SelectedStatusFilter = DefaultStatusFilter;
            SelectedProjectProxy = DefaultProjectProxy;
            SelectedProjectRole = DefaultProjectRole;
            SelectedClientRole = DefaultClientRole;

            ProjectSourcePage = TilesPageName;
            ProjectSourceMode = PageFunctions.None;

            SelectedTeamTimeFilter = DefaultTeamTimeFilter;
            SelectedStage = DefaultStage;
            SelectedTimelineType = DefaultTimelineType;
            SelectedFromDate = DefaultFromDate;
            SelectedToDate = DefaultToDate;
            SelectedHistory = null;
        }

    } // class
} // namespace
