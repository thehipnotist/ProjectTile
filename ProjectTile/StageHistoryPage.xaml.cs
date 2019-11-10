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

        // ------------- Current records ------------ //



        // ------------------ Lists ----------------- //


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
            
            FromDate.SelectedDate = fromDate = Globals.StartOfTime;
            ToDate.SelectedDate = toDate = Globals.OneMonthAhead;            
            PageFunctions.ShowFavouriteButton();
        }

        // ---------------------------------------------------------- //
        // -------------------- Data Management --------------------- //
        // ---------------------------------------------------------- //  

        // ------------- Data retrieval ------------- // 		
        
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
            }
            catch (Exception generalException) { MessageFunctions.Error("Error selecting current project in the list", generalException); }
        }

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
                else if ((ClientProxy)ClientCombo.SelectedItem != Globals.SelectedClientProxy)
                {
                    //
                    refreshProjectCombo();
                }
                else { ClientCombo.SelectedItem = Globals.SelectedClientProxy; }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing client selection", generalException); }	
        }

        private void StatusCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (StatusCombo.SelectedItem != null)
                {
                    if (StatusCombo.SelectedItem.ToString() != statusDescription)
                    {
                        statusDescription = StatusCombo.SelectedItem.ToString();
                        string selection = statusDescription.Replace(" ", "");
                        Globals.SelectedStatusFilter = (Globals.ProjectStatusFilter)Enum.Parse(typeof(Globals.ProjectStatusFilter), selection);
                        //
                        refreshProjectCombo();
                    }
                    else { StatusCombo.SelectedItem = statusDescription; }
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing status filter selection", generalException); }	
        }

        private void ProjectCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectCombo.SelectedItem == null) { } // Do nothing - won't be for long             
            else if ( Globals.SelectedProjectProxy != (ProjectProxy)ProjectCombo.SelectedItem)
            {
                try
                {
                    Globals.SelectedProjectProxy = (ProjectProxy)ProjectCombo.SelectedItem;
                    //                 
                }
                catch (Exception generalException) { MessageFunctions.Error("Error processing project selection", generalException); }
            }
            else { ProjectCombo.SelectedItem = Globals.SelectedProjectProxy; }
        }

        private void FromDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FromDate.SelectedDate != null)
            {
                if (FromDate.SelectedDate != fromDate)
                {
                    fromDate = (DateTime)FromDate.SelectedDate;
                    if (toDate != null && toDate < fromDate) { ToDate.SelectedDate = fromDate; }
                    else { ; } //
                }
                else { FromDate.SelectedDate = fromDate; }
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
                    else { ; } //
                }
                else { ToDate.SelectedDate = toDate; }
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
    } // class
} // namespace
