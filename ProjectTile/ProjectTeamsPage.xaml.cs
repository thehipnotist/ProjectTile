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
    /// Interaction logic for ProjectTeamsPage.xaml
    /// </summary>
    public partial class ProjectTeamsPage : Page
    {
        // ---------------------------------------------------------- //
        // -------------------- Global Variables -------------------- //
        // ---------------------------------------------------------- //

        // --------- Global/page parameters --------- // 

        string pageMode;

        // ------------ Current variables ----------- // 

        string nameLike = "";
        bool exactName = false;
        TeamSummaryRecord selectedTeamRecord = null;

        // ------------- Current records ------------ //



        // ------------------ Lists ----------------- //
        
        List<StaffSummaryRecord> staffDropList;



        // ---------------------------------------------------------- //
        // -------------------- Page Management --------------------- //
        // ---------------------------------------------------------- //

        // ---------- Initialize and Load ----------- //

        public ProjectTeamsPage()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Page));
            KeepAlive = false;
            AddButton.Margin = ProjectButton.Margin;
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

            refreshProjectRoleCombo();
            refreshStatusCombo();
            refreshProjectCombo();
            setTeamTimeRadio();
        }



        // ---------------------------------------------------------- //
        // -------------------- Data Management --------------------- //
        // ---------------------------------------------------------- //  

        // ------------- Data retrieval ------------- // 		

        private void refreshTeamDataGrid()
        {
            try
            {
                ProjectSummaryRecord currentProjectSummary = (Globals.SelectedProjectSummary != null) ? Globals.SelectedProjectSummary : null;
                Globals.ProjectStatusFilter statusFilter = Globals.SelectedStatusFilter;
                //TODO: Adjust status filter if project isn't in it
                string projectRoleCode = Globals.SelectedProjectRole.RoleCode;
                Globals.TeamTimeFilter timeFilter = Globals.SelectedTeamTimeFilter;

                bool success = ProjectFunctions.SetTeamsGridList(statusFilter, projectRoleCode, timeFilter, currentProjectSummary.ProjectID, nameLike, exactName);
                if (success)
                {
                    TeamDataGrid.ItemsSource = ProjectFunctions.TeamsGridList;
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating contact grid data", generalException); }
        }
        
        private void refreshProjectCombo()
        {
            try
            {
                ProjectSummaryRecord currentRecord = (Globals.SelectedProjectSummary != null) ? Globals.SelectedProjectSummary : Globals.DefaultProjectSummary;
                ProjectFunctions.SetProjectFilterList(Globals.SelectedStatusFilter);
                ProjectCombo.ItemsSource = ProjectFunctions.ProjectFilterList;
                if (ProjectFunctions.ProjectFilterList.Exists(pfl => pfl.ProjectCode == currentRecord.ProjectCode))
                {
                    ProjectCombo.SelectedItem = ProjectFunctions.ProjectFilterList.First(pfl => pfl.ProjectCode == currentRecord.ProjectCode);
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating projects drop-down list", generalException); }
        }
        
        private void refreshProjectRoleCombo()
        {
            try
            {
                ProjectRoles currentRecord = (Globals.SelectedProjectRole != null) ? Globals.SelectedProjectRole : Globals.DefaultProjectRole;
                ProjectFunctions.SetRolesFilterList(nameLike, exactName);
                RoleCombo.ItemsSource = ProjectFunctions.RolesFilterList;
                if (!ProjectFunctions.RolesFilterList.Exists(rfl => rfl.RoleCode == currentRecord.RoleCode)) { currentRecord = Globals.AllRoles; }
                RoleCombo.SelectedItem = ProjectFunctions.RolesFilterList.First(rfl => rfl.RoleCode == currentRecord.RoleCode);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating project roles drop-down list", generalException); }
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

        private void refreshNamesList()
        {
            exactName = false;
            string nameLike = NameLike.Text;
            if (nameLike == "") { PossibleNames.Visibility = Visibility.Hidden; }
            else
            {
                PossibleNames.Visibility = Visibility.Visible;
                staffDropList = StaffFunctions.GetStaffGridData(activeOnly: false, nameContains: nameLike, roleDescription: "", entityID: Globals.CurrentEntityID);
                PossibleNames.ItemsSource = staffDropList;
            }
        }

        private void chooseStaffName()
        {
            StaffSummaryRecord selectedStaff = (StaffSummaryRecord)PossibleNames.SelectedItem;
            NameLike.Text = selectedStaff.StaffName;
            exactName = true;
            nameFilter();
        }

        private void nameFilter()
        {
            PossibleNames.Visibility = Visibility.Hidden;
            nameLike = NameLike.Text;
            refreshProjectRoleCombo();
            refreshTeamDataGrid();    
        }

        private void setTeamTimeRadio()
        {
            switch (Globals.SelectedTeamTimeFilter)
            {
                case Globals.TeamTimeFilter.All: AllRadio.IsChecked = true; break;
                case Globals.TeamTimeFilter.Future: FutureRadio.IsChecked = true; break;
                case Globals.TeamTimeFilter.Current: CurrentRadio.IsChecked = true; break;
                default: FutureRadio.IsChecked = true; break;
            }
        }

        private void teamTimeChanged(Globals.TeamTimeFilter option)
        {
            Globals.SelectedTeamTimeFilter = option;
            refreshTeamDataGrid();
        }

        private void toggleProjectMode(bool projectSelected)
        {
            AddButton.Visibility = AmendButton.Visibility = projectSelected ? Visibility.Visible : Visibility.Hidden;
            ProjectButton.Visibility = (!projectSelected) ? Visibility.Visible : Visibility.Hidden;
        }

        // -------------- Data updates -------------- // 



        // --------- Other/shared functions --------- // 



        // ---------- Links to other pages ---------- //		



        // ---------------------------------------------------------- //
        // -------------------- Event Management -------------------- //
        // ---------------------------------------------------------- //  

        // ---- Generic (shared) control events ----- // 		   



        // -------- Control-specific events --------- // 



        private void StatusCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (StatusCombo.SelectedItem != null)
                {
                    string selection = StatusCombo.SelectedItem.ToString();
                    selection = selection.Replace(" ", "");
                    Globals.SelectedStatusFilter = (Globals.ProjectStatusFilter)Enum.Parse(typeof(Globals.ProjectStatusFilter), selection);
                    refreshProjectCombo();
                    refreshTeamDataGrid();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing status filter selection", generalException); }	            
        }

        private void RoleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoleCombo.SelectedItem == null) { } // Do nothing - won't be for long             
            else
            {
                Globals.SelectedProjectRole = (ProjectRoles)RoleCombo.SelectedItem;
                refreshTeamDataGrid();
            }
        }

        private void ProjectCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectCombo.SelectedItem == null) { } // Do nothing - won't be for long             
            else
            {
                ProjectSummaryRecord selectedProject = (ProjectSummaryRecord)ProjectCombo.SelectedItem;
                if (selectedProject == Globals.SearchProjects) { } //TODO: write search function
                else
                {
                    Globals.SelectedProjectSummary = selectedProject;
                    refreshTeamDataGrid();
                    toggleProjectMode(selectedProject != Globals.AllProjects);
                }
            }
        }

        private void NameLike_LostFocus(object sender, RoutedEventArgs e)
        {
            nameFilter();
        }

        private void NameLike_KeyUp(object sender, KeyEventArgs e)
        {
            refreshNamesList();
        }

        private void NameLike_GotFocus(object sender, RoutedEventArgs e)
        {
            refreshNamesList();
        }

        private void PossibleNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PossibleNames.SelectedItem != null) { chooseStaffName(); }
        }

        private void AmendButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AllRadio_Checked(object sender, RoutedEventArgs e)
        {
            teamTimeChanged(Globals.TeamTimeFilter.All);
        }

        private void FutureRadio_Checked(object sender, RoutedEventArgs e)
        {
            teamTimeChanged(Globals.TeamTimeFilter.Future);
        }

        private void CurrentRadio_Checked(object sender, RoutedEventArgs e)
        {
            teamTimeChanged(Globals.TeamTimeFilter.Current);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectFunctions.ReturnToTilesPage();
        }

        private void ProjectButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTeamRecord != null) // Just in case
            {                
                ProjectCombo.SelectedItem = ProjectFunctions.ProjectFilterList.First(pfl => pfl.ProjectCode == selectedTeamRecord.Project.ProjectCode);
            }
        }

        private void TeamDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TeamDataGrid.SelectedItem == null)
            {
                selectedTeamRecord = null;
                AmendButton.IsEnabled = false;
            }
            else
            {
                selectedTeamRecord = (TeamSummaryRecord)TeamDataGrid.SelectedItem;
                if (Globals.SelectedProjectSummary == null || Globals.SelectedProjectSummary == Globals.AllProjects) { ProjectButton.IsEnabled = true; }
                AmendButton.IsEnabled = true;
            }
        }

    } // class
} // namespace
