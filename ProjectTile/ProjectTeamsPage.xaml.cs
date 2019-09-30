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
        string staffIDString;
        int staffID;

        // ------------ Current variables ----------- // 

        string nameLike = "";
        bool exactName = false;
        string defaultInstructions = "The top filters refer to the project, the bottom ones to the team member and their role in the project.";

        // ------------- Current records ------------ //

        TeamSummaryRecord selectedTeamRecord = null;

        // ------------------ Lists ----------------- //
        
        List<StaffSummaryRecord> staffDropList;
        List<StaffSummaryRecord> staffComboList;


        // ---------------------------------------------------------- //
        // -------------------- Page Management --------------------- //
        // ---------------------------------------------------------- //

        // ---------- Initialize and Load ----------- //

        public ProjectTeamsPage()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Page));
            KeepAlive = false;
            ProjectButton.Margin = CommitButton.Margin;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                pageMode = PageFunctions.pageParameter(this, "Mode");
                staffIDString = PageFunctions.pageParameter(this, "StaffID");
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
            Int32.TryParse(staffIDString, out staffID);
            if (staffID > 0) { chooseStaffName(staffID); }
            toggleEditMode(false);
        }

        // ---------------------------------------------------------- //
        // -------------------- Data Management --------------------- //
        // ---------------------------------------------------------- //  

        // ------------- Data retrieval ------------- // 		

        private void refreshTeamDataGrid()
        {
            try
            {
                TeamSummaryRecord currentRecord = selectedTeamRecord ?? null;
                ProjectSummaryRecord currentProjectSummary = (Globals.SelectedProjectSummary != null) ? Globals.SelectedProjectSummary : null;
                Globals.ProjectStatusFilter statusFilter = Globals.SelectedStatusFilter;
                string projectRoleCode = Globals.SelectedProjectRole.RoleCode;
                Globals.TeamTimeFilter timeFilter = Globals.SelectedTeamTimeFilter;

                bool success = ProjectFunctions.SetTeamsGridList(statusFilter, projectRoleCode, timeFilter, currentProjectSummary.ProjectID, nameLike, exactName);
                if (success)
                {
                    TeamDataGrid.ItemsSource = ProjectFunctions.TeamsGridList;
                    if (currentRecord != null && ProjectFunctions.TeamsGridList.Exists(tgl => tgl.ID == currentRecord.ID)) 
                    {
                        TeamDataGrid.SelectedItem = ProjectFunctions.TeamsGridList.First(tgl => tgl.ID == currentRecord.ID); 
                    }
                    else if (ProjectFunctions.TeamsGridList.Count == 1) { TeamDataGrid.SelectedItem = ProjectFunctions.TeamsGridList.ElementAt(0); }
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating project team grid data", generalException); }
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
                    toggleEditMode(true);
                }
                else { toggleEditMode(false); }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating projects drop-down list", generalException); }
        }
        
        private void refreshProjectRoleCombo()
        {
            try
            {
                ProjectRoles currentRecord = (Globals.SelectedProjectRole != null) ? Globals.SelectedProjectRole : Globals.DefaultProjectRole;
                ProjectFunctions.SetRolesFilterList(nameLike, exactName);
                MainRoleCombo.ItemsSource = ProjectFunctions.RolesFilterList;
                if (!ProjectFunctions.RolesFilterList.Exists(rfl => rfl.RoleCode == currentRecord.RoleCode)) { currentRecord = Globals.AllRoles; }
                MainRoleCombo.SelectedItem = ProjectFunctions.RolesFilterList.First(rfl => rfl.RoleCode == currentRecord.RoleCode);
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


        // -------------- Data updates -------------- // 



        // --------- Other/shared functions --------- // 

        private void chooseStaffName(int staffID = 0)
        {
            StaffSummaryRecord selectedStaff = (staffID != 0) ? StaffFunctions.GetStaffSummary(staffID) : (StaffSummaryRecord)PossibleNames.SelectedItem;
            NameLike.Text = selectedStaff.StaffName;
            exactName = true;
            nameFilter();
        }

        private void nameFilter()
        {
            PossibleNames.Visibility = Visibility.Hidden;
            nameLike = NameLike.Text;
            refreshProjectRoleCombo();
            toggleStaffNameColumn();
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

        private void toggleEditButtons(bool selection)
        {
            if (!selection) { AddButton.Visibility = AmendButton.Visibility = Visibility.Hidden; }
            else
            {
                AddButton.Visibility = Globals.MyPermissions.Allow("AddProjectTeams") ? Visibility.Visible : Visibility.Hidden;
                AmendButton.Visibility = Globals.MyPermissions.Allow("EditProjectTeams") ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private void toggleProjectMode(bool projectSelected)
        {
            toggleEditButtons(projectSelected);
            ProjectButton.Visibility = (!projectSelected) ? Visibility.Visible : Visibility.Hidden;
            toggleProjectSearchButton(projectSelected);
            toggleProjectColumns(projectSelected);
        }

        private void toggleProjectSearchButton(bool projectSelected)
        {
            ProjectSearchButton.Visibility = (!projectSelected) ? Visibility.Visible : Visibility.Hidden;
            double changeWidth = projectSelected ? 0 : (ProjectSearchButton.Width + 15);
            ProjectCombo.Width = ProjectCombo.MaxWidth - changeWidth;
            Thickness projectMargin = ProjectCombo.Margin;
            projectMargin.Right = 15 + changeWidth;
            ProjectCombo.Margin = projectMargin;
        }

        private void toggleProjectColumns(bool projectSelected)
        {
            ProjectCodeColumn.Visibility = ProjectNameColumn.Visibility = projectSelected ? Visibility.Collapsed : Visibility.Visible;
        }

        private void toggleStaffNameColumn()
        {
            StaffNameColumn.Visibility = (exactName && nameLike != "") ? Visibility.Collapsed : Visibility.Visible;
        }

        private void toggleRoleColumn()
        {
            RoleDescriptionColumn.Visibility = (Globals.SelectedProjectRole != null && Globals.SelectedProjectRole != Globals.AllRoles) ? Visibility.Collapsed : Visibility.Visible;
        }
        
        private void toggleEditMode(bool editing)
        {
            try
            {
                AmendmentGrid.Visibility = CommitButton.Visibility = editing ? Visibility.Visible : Visibility.Hidden;
                StatusLabel.Visibility = StatusCombo.Visibility = (!editing) ? Visibility.Visible : Visibility.Hidden;
                NameLikeLabel.Visibility = NameLike.Visibility = MainRoleLabel.Visibility = MainRoleCombo.Visibility = StatusLabel.Visibility;
                toggleEditButtons(!editing && ProjectCombo.SelectedItem != null && ProjectCombo.SelectedItem != Globals.AllProjects);
                TeamDataGrid.Width = editing ? TeamDataGrid.MinWidth : TeamDataGrid.MaxWidth;
                TimeGroup.HorizontalAlignment = editing ? HorizontalAlignment.Left : HorizontalAlignment.Right;
                ProjectCombo.IsEnabled = TeamDataGrid.IsEnabled = !editing;
                if (!editing) { Instructions.Content = defaultInstructions; } // Otherwise set up later depending on the mode
            }
            catch (Exception generalException) { MessageFunctions.Error("Error displaying required controls", generalException); }	
        }

        private void setUpEdit(bool amendExisting)
        {
            try
            {
                toggleEditMode(true);
                staffComboList = StaffFunctions.GetStaffGridData(activeOnly: true, nameContains: "", roleDescription: "", entityID: Globals.CurrentEntityID);            
                StaffCombo.ItemsSource = staffComboList;
                if (amendExisting)
                {
                    if (selectedTeamRecord == null) 
                    {
                        MessageFunctions.Error("Error setting up amendment: no project team member record selected.", null);
                        toggleEditMode(false);
                    }
                
                    staffID = selectedTeamRecord.StaffMember.ID;
                    if (!staffComboList.Exists(scl => scl.ID == staffID)) { staffComboList.Add(StaffFunctions.GetStaffSummary(staffID)); }
                    StaffCombo.SelectedItem = staffComboList.FirstOrDefault(scl => scl.ID == staffID);
                    //TODO: set up other controls once added
                    Instructions.Content = "Amend the details as required and then click 'Save' to commit them.";
                }
                else
                {
                    Instructions.Content = "Insert the details as required and then click 'Save' to commit them.";
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error setting up amendment", generalException); }	
        }

        // ---------- Links to other pages ---------- //		

        public void OpenProjectLookup()
        {
            try
            {
                NormalGrid.Visibility = Visibility.Hidden;
                LookupFrame.Visibility = Visibility.Visible;
                LookupFrame.Navigate(new Uri("ProjectPage.xaml?Mode=Lookup", UriKind.RelativeOrAbsolute));
                ProjectFunctions.SelectProjectForTeam += SelectTeamProject;
                ProjectFunctions.CancelTeamProjectSelection += CancelProjectLookup;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error setting up client selection", generalException); }
        }

        public void CloseProjectLookup()
        {
            LookupFrame.Content = null;
            LookupFrame.Visibility = Visibility.Hidden;
            NormalGrid.Visibility = Visibility.Visible;
        }

        public void SelectTeamProject()
        {
            try
            {
                CloseProjectLookup();
                Globals.SelectedProjectSummary = ProjectFunctions.SelectedTeamProject;
                refreshProjectCombo();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing client selection", generalException); }
        }

        public void CancelProjectLookup()
        {
            CloseProjectLookup();
            ProjectCombo.SelectedItem = ProjectFunctions.ProjectFilterList.First(pfl => pfl.ProjectCode == Globals.AllProjects.ProjectCode);
        }

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

        private void MainRoleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainRoleCombo.SelectedItem == null) { } // Do nothing - won't be for long             
            else
            {
                Globals.SelectedProjectRole = (ProjectRoles)MainRoleCombo.SelectedItem;
                toggleRoleColumn();
                refreshTeamDataGrid();
            }
        }

        private void ProjectCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectCombo.SelectedItem == null) { } // Do nothing - won't be for long             
            else
            {
                ProjectSummaryRecord selectedProject = (ProjectSummaryRecord)ProjectCombo.SelectedItem;
                if (selectedProject == Globals.SearchProjects) { OpenProjectLookup(); }
                else
                {
                    Globals.SelectedProjectSummary = selectedProject;
                    refreshTeamDataGrid();
                    toggleProjectMode(selectedProject != Globals.AllProjects);
                    //TODO: Adjust status filter if project isn't in it - works OK for now, as the filter is set globally...
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
            setUpEdit(true); 
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            setUpEdit(false); 
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

        private void ProjectButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTeamRecord != null) // Just in case
            {                
                ProjectCombo.SelectedItem = ProjectFunctions.ProjectFilterList.First(pfl => pfl.ProjectCode == selectedTeamRecord.Project.ProjectCode);
                NameLike.Text = ""; // Always show all team members for the project at this point
                nameFilter(); // Implement the above
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

        private void ProjectSearch_Click(object sender, RoutedEventArgs e)
        {
            OpenProjectLookup();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (AmendmentGrid.Visibility == Visibility.Visible) { toggleEditMode(false); }
            else { ProjectFunctions.ReturnToTilesPage(); }
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Save changes
            toggleEditMode(false);
        }

        private void StaffSearch_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Set up staff search
        }

    } // class
} // namespace
