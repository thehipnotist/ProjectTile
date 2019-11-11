using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProjectTile
{
    /// <summary>
    /// InterStageHistory logic for StageHistoryPage.xaml
    /// </summary>
    public partial class StageHistoryPage : Page
    {
        // ---------------------------------------------------------- //
        // -------------------- Global Variables -------------------- //
        // ---------------------------------------------------------- //

        // --------- Global/page parameters --------- // 

        string pageMode;        

        // ------------ Current variables ----------- // 

        DateTime fromDate = Globals.InfiniteDate;
        DateTime toDate = Globals.StartOfTime;
        private string statusDescription;
        private int stageNumber = -1;

        // ------------- Current records ------------ //

        Globals.TimelineType currentType = Globals.TimelineType.Effective;

        // ------------------ Lists ----------------- //
        
        private List<ProjectStages> stageList;
        private List<StageHistoryProxy> stageHistoryList;

        // ---------------------------------------------------------- //
        // -------------------- Page Management --------------------- //
        // ---------------------------------------------------------- //

        // ---------- Initialize and Load ----------- //

        public StageHistoryPage()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Page));
            KeepAlive = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                pageMode = PageFunctions.pageParameter(this, "Mode");
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                PageFunctions.ShowTilesPage();
            }

            refreshClientCombo();
            refreshStatusCombo();
            refreshStageCombo();
            FromDate.SelectedDate = fromDate = Globals.StartOfTime;
            ToDate.SelectedDate = toDate = Globals.OneMonthAhead;            
            PageFunctions.ShowFavouriteButton();
        }

        // ---------------------------------------------------------- //
        // -------------------- Data Management --------------------- //
        // ---------------------------------------------------------- //  

        // ------------- Data retrieval ------------- // 		
        
        private void refreshHistoryDataGrid()
        {
            int clientID = (Globals.SelectedClientProxy != null) ? Globals.SelectedClientProxy.ID : 0;
            int projectID = (Globals.SelectedProjectProxy != null) ? Globals.SelectedProjectProxy.ProjectID : 0;

            stageHistoryList = ProjectFunctions.StageHistoryList(clientID: clientID, statusFilter: Globals.SelectedStatusFilter, projectID: projectID, timelineType: currentType,
                fromDate: fromDate, toDate: toDate, stageNumber: stageNumber);

            StageHistoryDataGrid.ItemsSource = stageHistoryList;
        }        
        
        private void refreshClientCombo()
        {
            try
            {
                ClientProxy currentRecord = (Globals.SelectedClientProxy != null) ? Globals.SelectedClientProxy : Globals.DefaultClientProxy;
                ProjectFunctions.SetClientFilterList();
                ClientCombo.ItemsSource = ProjectFunctions.ClientFilterList;
                if (ProjectFunctions.ClientFilterList.Exists(ccl => ccl.ID == currentRecord.ID))
                {
                    ClientCombo.SelectedItem = ProjectFunctions.ClientFilterList.First(ccl => ccl.ID == currentRecord.ID);
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating client drop-down list", generalException); }
        }

        private void refreshStatusCombo()
        {
            try
            {
                Globals.ProjectStatusFilter currentFilter = Globals.SelectedStatusFilter;
                string currentName = ProjectFunctions.StatusFilterName(currentFilter);
                if (ProjectFunctions.StatusFilterList == null) { ProjectFunctions.SetProjectStatusFilter(); }
                StatusCombo.ItemsSource = ProjectFunctions.StatusFilterList;
                StatusCombo.SelectedItem = currentName;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating project status drop-down list", generalException); }
        }

        private void refreshProjectCombo()
        {
            try
            {
                int clientID = (Globals.SelectedClientProxy != null)? Globals.SelectedClientProxy.ID : 0;
                ProjectProxy currentRecord = (Globals.SelectedProjectProxy != null) ? Globals.SelectedProjectProxy : Globals.DefaultProjectProxy;
                ProjectFunctions.SetProjectFilterList(Globals.SelectedStatusFilter, true, clientID, false);
                ProjectCombo.ItemsSource = ProjectFunctions.ProjectFilterList;
                selectProject(currentRecord.ProjectID);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating projects drop-down list", generalException); }
        }

        private void refreshStageCombo()
        {
            stageList = ProjectFunctions.StageFilterList();
            StageCombo.ItemsSource = stageList;
            //StageCombo.SelectedItem = stageList.FirstOrDefault(sl => sl.ID == Globals.AllStages.ID);
            StageCombo.SelectedItem = Globals.AllStages;
        }

        // -------------- Data updates -------------- // 



        // --------- Other/shared functions --------- // 

        private void selectProject(int projectID)
        {
            try
            {
                if (ProjectFunctions.ProjectFilterList.Exists(pfl => pfl.ProjectID == projectID))
                {
                    ProjectCombo.SelectedItem = ProjectFunctions.ProjectFilterList.First(pfl => pfl.ProjectID == projectID);
                }
                else ProjectCombo.SelectedItem = ProjectFunctions.ProjectFilterList.First(pfl => pfl.ProjectID == 0);
                refreshHistoryDataGrid();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error selecting current project in the list", generalException); }
        }

        private void changeTimelineType(Globals.TimelineType type)
        {
            try
            {
                if (ProjectFunctions.FullStageList == null || ProjectFunctions.FullStageList.Count() == 0) { return; } // Too early
                else if (type == currentType) { return; } // Cancelling the earlier change
                else
                {
                    currentType = type;
                    refreshHistoryDataGrid();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error changing timeline type", generalException); }
        }

        //private void setTimelineType(Globals.TimelineType type)
        //{
        //    switch (type)
        //    {
        //        case Globals.TimelineType.Actual: ActualRadio.IsChecked = true; break;
        //        case Globals.TimelineType.Effective: EffectiveRadio.IsChecked = true; break;
        //        case Globals.TimelineType.Target: TargetRadio.IsChecked = true; break;
        //        default: EffectiveRadio.IsChecked = true; break;
        //    }
        //}

        // ---------- Links to other pages ---------- //		



        // ---------------------------------------------------------- //
        // -------------------- Event Management -------------------- //
        // ---------------------------------------------------------- //  

        // ---- Generic (shared) control events ----- // 		   



        // -------- Control-specific events --------- // 

        private void ClientCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ClientCombo.SelectedItem == null) { } // Won't be for long
                else
                {
                    Globals.SelectedClientProxy = (ClientProxy)ClientCombo.SelectedItem;
                    refreshProjectCombo();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing client selection", generalException); }	
        }

        private void StatusCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (StatusCombo.SelectedItem != null)
                {
                    statusDescription = StatusCombo.SelectedItem.ToString();
                    string selection = statusDescription.Replace(" ", "");
                    Globals.SelectedStatusFilter = (Globals.ProjectStatusFilter)Enum.Parse(typeof(Globals.ProjectStatusFilter), selection);
                    refreshProjectCombo();
                    refreshHistoryDataGrid();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing status filter selection", generalException); }	
        }

        private void ProjectCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectCombo.SelectedItem == null) { } // Do nothing - won't be for long             
            else 
            {
                try
                {
                    Globals.SelectedProjectProxy = (ProjectProxy)ProjectCombo.SelectedItem;
                    refreshHistoryDataGrid();
                }
                catch (Exception generalException) { MessageFunctions.Error("Error processing project selection", generalException); }
            }
        }

        private void FromDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FromDate.SelectedDate != null)
            {
                if (FromDate.SelectedDate != fromDate)
                {
                    fromDate = (DateTime)FromDate.SelectedDate;
                    if (toDate != null && toDate < fromDate) { ToDate.SelectedDate = fromDate; }
                    else { refreshHistoryDataGrid(); }
                }
            }
            else { fromDate = Globals.StartOfTime; }            
        }

        private void ToDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ToDate.SelectedDate != null)
            {
                if (ToDate.SelectedDate != toDate)
                {
                    toDate = (DateTime)ToDate.SelectedDate;
                    if (fromDate != null && fromDate > toDate) { FromDate.SelectedDate = toDate; }
                    else { refreshHistoryDataGrid(); }
                }
            }
            else { toDate = Globals.InfiniteDate; }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowTilesPage();
        }

        private void StageHistoryDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void TargetRadio_Checked(object sender, RoutedEventArgs e)
        {
            changeTimelineType(Globals.TimelineType.Target);
        }

        private void ActualRadio_Checked(object sender, RoutedEventArgs e)
        {
            changeTimelineType(Globals.TimelineType.Actual);
        }

        private void EffectiveRadio_Checked(object sender, RoutedEventArgs e)
        {
            changeTimelineType(Globals.TimelineType.Effective);
        }

        private void StageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (StageCombo.SelectedItem != null)
                {
                    ProjectStages stageFilter = StageCombo.SelectedItem as ProjectStages;
                    stageNumber = (stageFilter != null) ? stageFilter.StageNumber : -1;
                    refreshHistoryDataGrid();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error handling stage selection", generalException); }
        }

    } // class
} // namespace
