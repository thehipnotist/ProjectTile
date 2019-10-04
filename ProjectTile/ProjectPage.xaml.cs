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
        ProjectSummaryRecord selectedProject = Globals.SelectedProjectSummary ?? null;

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
                if (pageMode != PageFunctions.Lookup) { PageFunctions.ShowFavouriteButton(); }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                closePage(true);
            }

            if (pageMode == PageFunctions.View) 
            { 
                AddButton.Visibility = CommitButton.Visibility = Visibility.Hidden;
                AmendImage.SetResourceReference(Image.SourceProperty, "ViewIcon");
            }
            else if (pageMode == PageFunctions.Amend)
            {
                PageHeader.Content = "Amend or Manage Projects";
                Instructions.Content = "Use filters to restrict results and column headers to sort them, then choose the required option.";
                HeaderImage2.SetResourceReference(Image.SourceProperty, "AmendIcon");
                CommitButton.Visibility = Visibility.Hidden;
            }
            else if (pageMode == PageFunctions.Lookup)
            {
                PageHeader.Content = "Select Project";
                Instructions.Content = "Use filters to restrict results and column headers to sort them, then choose the desired project.";
                HeaderImage2.SetResourceReference(Image.SourceProperty, "SearchIcon");
                AddButton.Visibility = AmendButton.Visibility = BackButton.Visibility = MoreButton.Visibility =  Visibility.Hidden;
                CommitButton.Margin = AmendButton.Margin;
                CancelButtonText.Text = "Cancel";
            }

            try
            {
                if (Globals.ProjectSourcePage == "ProjectPage") { BackButton.Visibility = Visibility.Hidden; }
                refreshClientCombo();
                refreshPMsCombo();
                if (ProjectFunctions.PMFilterList.Exists(ssr => ssr.ID == Globals.MyStaffID)) { PMsCombo.SelectedItem = ProjectFunctions.PMFilterList.First(ssr => ssr.ID == Globals.MyStaffID); }
                refreshStatusCombo();
                toggleMoreButton();
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error refreshing filters", generalException);
                closePage(true);
            }
        }

        // ---------------------- //
        // -- Data Management --- //
        // ---------------------- //

        // Data updates //
        private void ensureCurrentRecordDisplayed()
        {
            try
            {
                ProjectSummaryRecord currentRecord = (selectedProject != null && selectedProject.ProjectID > 0 ) ? selectedProject : null;
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
                        Globals.SelectedPMSummary = Globals.AllPMs;
                    }
                    if (!ProjectFunctions.IsInFilter(statusFilter, currentRecord.Stage))
                    {
                        Globals.SelectedStatusFilter = Globals.ProjectStatusFilter.All;
                    }
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error displaying the current project record", generalException); }
        }

        private void toggleMoreButton()
        {
            bool allowTeams = ( Globals.MyPermissions.Allow("ViewProjectTeams") || Globals.MyPermissions.Allow("EditProjectTeams") );
            bool allowContacts = (Globals.MyPermissions.Allow("ViewClientTeams") || Globals.MyPermissions.Allow("EditClientTeams") ) ;
            bool allowProducts = (Globals.MyPermissions.Allow("ViewProjectProducts") || Globals.MyPermissions.Allow("EditProjectTeams") );
            
            TeamMenu.Visibility = allowTeams ? Visibility.Visible : Visibility.Collapsed;
            ContactMenu.Visibility = allowContacts ? Visibility.Visible : Visibility.Collapsed;
            ProductMenu.Visibility = allowProducts ? Visibility.Visible : Visibility.Collapsed;
            
            if (!allowTeams && !allowContacts && !allowProducts)
            {
                MoreButton.Visibility = Visibility.Hidden;
                MoreMenu.Visibility = Visibility.Hidden;
            }
        }

        private void refreshMainProjectGrid()
        {
            try
            {                
                ProjectSummaryRecord currentRecord = (selectedProject != null) ? selectedProject : null;
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
                    closePage(true);
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
                Globals.ProjectStatusFilter currentFilter = Globals.SelectedStatusFilter;
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
            AmendButton.IsEnabled = MoreButton.IsEnabled = projectSelected;
        }

        private void closePage(bool goBack)
        {
            if (pageMode == PageFunctions.Lookup) { ProjectFunctions.CancelTeamProjectSelection(); }  
            else if (goBack)
            {
                if (Globals.ProjectSourcePage == "ClientPage") { ProjectFunctions.ReturnToClientPage(pageMode); }
                else { ProjectFunctions.ReturnToTilesPage(); }
            }                       
            else { ProjectFunctions.ReturnToTilesPage(); }
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
                    selectedProject = null;
                    toggleProjectButtons(false);
                    ProjectFunctions.ToggleFavouriteButton(false);
                }
                else
                {
                    selectedProject = (ProjectSummaryRecord)ProjectDataGrid.SelectedItem;
                    toggleProjectButtons(true);                    
                }
                if (pageMode != PageFunctions.Lookup) // Otherwise don't set this as may cancel selection later
                { 
                    Globals.SelectedProjectSummary = selectedProject;
                    ProjectFunctions.ToggleFavouriteButton(selectedProject != null);
                } 
            }
            catch (Exception generalException) { MessageFunctions.Error("Error displaying project selection", generalException); }	
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
            closePage(false);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProjectDetailsPage(PageFunctions.New);
        }

        private void AmendButton_Click(object sender, RoutedEventArgs e)
        {
            string inputMode = viewOnly ? PageFunctions.View : PageFunctions.Amend;
            PageFunctions.ShowProjectDetailsPage(inputMode);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            closePage(true);
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            MoreMenu.IsSubmenuOpen = true;
        }

        private void TeamMenu_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProjectTeamsPage();
        }

        private void ContactMenu_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Add this once the page is built
        }

        private void ProductMenu_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Add this once the page is built
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectFunctions.SelectTeamProject(selectedProject);
        }


    } // class
} // namespace
