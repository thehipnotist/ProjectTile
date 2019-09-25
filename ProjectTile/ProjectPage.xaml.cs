using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Interaction logic for ProjectPage.xaml
    /// </summary>
    public partial class ProjectPage : Page
    {
        // ---------------------- //
        // -- Global Variables -- //
        // ---------------------- //   

        // Global/page parameters //
        string pageMode;
        bool viewOnly = false;

        // Current variables //     

        // Current records //


        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //

        // Initialize and Load //
        public ProjectPage()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Page));
            KeepAlive = false;
            ensureCurrentRecordDisplayed();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                pageMode = PageFunctions.pageParameter(this, "Mode");
                Globals.ProjectSourceMode = pageMode;
                //Globals.ProjectSourcePage = "ProjectPage"; // Now set in Page Functions beforehand
                if (pageMode == PageFunctions.View || !Globals.MyPermissions.Allow("EditProjects")) { viewOnly = true; }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                closePage();
            }

            if (pageMode == PageFunctions.View) { AddButton.Visibility = Visibility.Hidden; }
            else
            {
                PageHeader.Content = "Amend or Manage Projects";
                Instructions.Content = "Use filters to restrict results and column headers to sort them, then choose the required option.";
            }

            try
            {
                if (Globals.ProjectSourcePage == "ProjectPage") { BackButton.Visibility = Visibility.Hidden; }
                refreshClientCombo();
                refreshPMsCombo();
                if (ProjectFunctions.PMFilterList.Exists(ssr => ssr.ID == Globals.CurrentStaffID)) { PMsCombo.SelectedItem = ProjectFunctions.PMFilterList.First(ssr => ssr.ID == Globals.CurrentStaffID); }
                refreshStatusCombo();
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error refreshing filters", generalException);
                closePage();
            }

        }

        // ---------------------- //
        // -- Data Management --- //
        // ---------------------- //

        // Data updates //
        private void ensureCurrentRecordDisplayed()
        {
            ProjectSummaryRecord currentRecord = (Globals.SelectedProjectSummary != null) ? Globals.SelectedProjectSummary : null;
            int clientID = (Globals.SelectedClientSummary != null) ? ProjectFunctions.SelectedClientSummary.ID : 0;
            int managerID = (Globals.SelectedPMSummary != null) ? ProjectFunctions.SelectedPMSummary.ID : 0;
            Globals.ProjectStatusFilter statusFilter = Globals.SelectedStatusFilter;  

            if (currentRecord != null) // Reset filters if necessary to show the selected record
            {
                if (clientID != 0 && clientID != currentRecord.Client.ID)
                {
                    Globals.SelectedClientSummary = Globals.AnyClient;

                }
                if (managerID != 0 && managerID != currentRecord.ProjectManager.ID)
                {
                    Globals.SelectedPMSummary = Globals.AnyPM;
                }
                if (!ProjectFunctions.IsInFilter(statusFilter, currentRecord.Stage))
                {
                    Globals.SelectedStatusFilter = Globals.ProjectStatusFilter.All;
                }
            }
        }

        private void refreshMainProjectGrid()
        {
            try
            {                
                ProjectSummaryRecord currentRecord = (Globals.SelectedProjectSummary != null) ? Globals.SelectedProjectSummary : null;

                int clientID = (Globals.SelectedClientSummary != null)? ProjectFunctions.SelectedClientSummary.ID : 0;
                int managerID = (Globals.SelectedPMSummary != null) ? ProjectFunctions.SelectedPMSummary.ID : 0;
                Globals.ProjectStatusFilter statusFilter = Globals.SelectedStatusFilter;               

                bool success = ProjectFunctions.SetProjectGridList(statusFilter, clientID, managerID);
                if (success)
                {                   
                    ProjectDataGrid.ItemsSource = ProjectFunctions.ProjectGridList;
                    if (currentRecord != null && ProjectFunctions.ProjectGridList.Exists(pgl => pgl.ProjectID == currentRecord.ProjectID) )
                    {
                        ProjectDataGrid.SelectedItem = ProjectFunctions.ProjectGridList.First(pgl => pgl.ProjectID == currentRecord.ProjectID);
                        ProjectDataGrid.ScrollIntoView(ProjectDataGrid.SelectedItem);

                    }
                    else if (ProjectFunctions.ProjectGridList.Count == 1)
                    {
                        ProjectDataGrid.SelectedItem = ProjectFunctions.ProjectGridList.ElementAt(0);                        
                    }
                }
                else
                {
                    // What happens if it doesn't work? Already throwing an error...
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating project grid data", generalException); }	
        }

        private void refreshClientCombo()
        {
            try
            {
                ClientSummaryRecord currentRecord = (Globals.SelectedClientSummary != null) ? Globals.SelectedClientSummary : Globals.DefaultClientSummary;     
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
                Globals.ProjectStatusFilter currentFilter = (Globals.SelectedStatusFilter != null) ? Globals.SelectedStatusFilter : Globals.DefaultStatusFilter;
                string currentName = ProjectFunctions.StatusFilterName(currentFilter);
                if (ProjectFunctions.StatusFilterList == null) { ProjectFunctions.SetProjectStatusFilter(); }
                StatusCombo.ItemsSource = ProjectFunctions.StatusFilterList;
                StatusCombo.SelectedItem = currentName;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating status drop-down list", generalException); }	
        }

        private void refreshPMsCombo()
        {
            try
            {
                StaffSummaryRecord currentRecord = (Globals.SelectedPMSummary != null) ? Globals.SelectedPMSummary : Globals.DefaultPMSummary;            
                ProjectFunctions.SetPMFilterList();
                PMsCombo.ItemsSource = ProjectFunctions.PMFilterList;
                if (ProjectFunctions.PMFilterList.Exists(pcl => pcl.ID == currentRecord.ID))
                {
                    PMsCombo.SelectedItem = ProjectFunctions.PMFilterList.First(pcl => pcl.ID == currentRecord.ID);
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating Project Managers drop-down list", generalException); }	
        }

        // Data retrieval //

        // Other/shared functions //
        private void toggleGridColumn(DataGridColumn column, bool show)
        {
            column.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void toggleProjectButtons(bool projectSelected)
        {
            CommitButton.IsEnabled = projectSelected;
        }

        private void closePage()
        {
            ProjectFunctions.ReturnToTilesPage();
        }

        // ---------------------- //
        // -- Event Management -- //
        // ---------------------- //

        // Generic (shared) control events //

        // Control-specific events //

        private void ProjectDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ProjectDataGrid.SelectedItem == null)
                {
                    Globals.SelectedProjectSummary = null;
                    toggleProjectButtons(false);
                }
                else
                {
                    Globals.SelectedProjectSummary = (ProjectSummaryRecord)ProjectDataGrid.SelectedItem;
                    toggleProjectButtons(true);
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing project selection", generalException); }	
        }

        private void ClientCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ClientCombo.SelectedItem == null) { } // Won't be for long
                else
                {
                    Globals.SelectedClientSummary = (ClientSummaryRecord)ClientCombo.SelectedItem;
                    toggleGridColumn(ClientCodeColumn, (Globals.SelectedClientSummary.ID == 0));
                    toggleGridColumn(ClientNameColumn, (Globals.SelectedClientSummary.ID == 0));
                    refreshMainProjectGrid();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing client selection", generalException); }	
        }

        private void PMsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {             
                if (PMsCombo.SelectedItem == null) { } // Won't be for long
                else
                {
                    Globals.SelectedPMSummary = (StaffSummaryRecord)PMsCombo.SelectedItem;
                    toggleGridColumn(ProjectManagerColumn, (Globals.SelectedPMSummary.ID == 0));
                    refreshMainProjectGrid();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing Account Manager selection", generalException); }	
        }

        private void StatusCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (StatusCombo.SelectedItem != null)
                {
                    string selection = StatusCombo.SelectedItem.ToString();
                    selection = selection.Replace(" ", "");
                    Globals.SelectedStatusFilter = (Globals.ProjectStatusFilter) Enum.Parse(typeof(Globals.ProjectStatusFilter), selection);
                    refreshMainProjectGrid();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing status filter selection", generalException); }	
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            closePage();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProjectDetailsPage(PageFunctions.New);
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            string inputMode = viewOnly ? PageFunctions.View : PageFunctions.Amend;
            PageFunctions.ShowProjectDetailsPage(inputMode);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Globals.ProjectSourcePage == "ClientPage") { ProjectFunctions.ReturnToClientPage(pageMode); }
            else { MessageFunctions.Error("Error returning to previous page: no page history.", null); }
        }


    } // class
} // namespace
