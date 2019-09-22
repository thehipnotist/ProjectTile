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
        bool openOnly = false;        

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
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                pageMode = PageFunctions.pageParameter(this, "Mode");
                Globals.ProjectSourceMode = pageMode;
                if (pageMode == PageFunctions.View || !Globals.MyPermissions.Allow("EditProjects")) { viewOnly = true; }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                ProjectFunctions.ReturnToTilesPage();
            }

            refreshClientCombo();
            refreshPMsCombo();
            if (ProjectFunctions.PMComboList.Exists(ssr => ssr.ID == Globals.CurrentStaffID)) { PMsCombo.SelectedItem = ProjectFunctions.PMComboList.First(ssr => ssr.ID == Globals.CurrentStaffID); }
            refreshStatusCombo();
        }



        // ---------------------- //
        // -- Data Management --- //
        // ---------------------- //

        // Data updates //
        private void refreshMainProjectGrid()
        {
            try
            {
                ProjectSummaryRecord currentRecord = (Globals.SelectedProjectSummary != null) ? Globals.SelectedProjectSummary : null;
//                if (currentRecord != null) { MessageBox.Show(currentRecord.ProjectName); }
                
                int clientID = (Globals.SelectedClientSummary != null)? ProjectFunctions.SelectedClientSummary.ID : 0;
                int managerID = (Globals.SelectedPMSummary != null) ? ProjectFunctions.SelectedPMSummary.ID : 0;

                bool success = ProjectFunctions.SetProjectGridList(Globals.SelectedStatusFilter, clientID, managerID);
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
                ProjectFunctions.SetClientComboList();
                ClientCombo.ItemsSource = ProjectFunctions.ClientComboList;
                if (ProjectFunctions.ClientComboList.Exists(ccl => ccl.ID == currentRecord.ID))
                {
                    ClientCombo.SelectedItem = ProjectFunctions.ClientComboList.First(ccl => ccl.ID == currentRecord.ID);
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
                ProjectFunctions.SetPMComboList();
                PMsCombo.ItemsSource = ProjectFunctions.PMComboList;
                if (ProjectFunctions.PMComboList.Exists(pcl => pcl.ID == currentRecord.ID))
                {
                    PMsCombo.SelectedItem = ProjectFunctions.PMComboList.First(pcl => pcl.ID == currentRecord.ID);
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

        // ---------------------- //
        // -- Event Management -- //
        // ---------------------- //

        // Generic (shared) control events //

        // Control-specific events //





        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectFunctions.ReturnToTilesPage();
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            string inputMode = viewOnly? PageFunctions.View : PageFunctions.Amend;  
            PageFunctions.ShowProjectDetailsPage(inputMode);
        }

        private void ProjectDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ProjectDataGrid.SelectedItem == null)
                {
                    Globals.SelectedProjectSummary = null;
                    CommitButton.IsEnabled = false;
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



    } // class
} // namespace
