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
    /// Interaction logic for ActionsPage.xaml
    /// </summary>
    public partial class ActionsPage : Page
    {
        // ---------------------------------------------------------- //
        // -------------------- Global Variables -------------------- //
        // ---------------------------------------------------------- //

        // --------- Global/page parameters --------- // 

        string pageMode;
        bool pageLoaded = false;
        const string defaultInstructions = "The top filters refer to the project, the bottom ones to the action and its owner.";
        const string defaultHeader = "Project Actions";
        bool projectSelected = false;
        bool clientSelected = false;

        // ------------ Current variables ----------- // 

        DateTime fromDate = Globals.InfiniteDate;
        DateTime toDate = Globals.StartOfTime;
        string nameLike = "";
        bool exactName = false;
        int completed = 0;
        CombinedStaffMember selectedPerson = null;

        // ------------- Current records ------------ //



        // ------------------ Lists ----------------- //
        List<string> completedList = null;
        List<ActionProxy> actionList = null;
        List<TeamProxy> loggedByList = null;
        List<ProjectStages> stageList = null;
        List<CombinedTeamMember> ownerList = null;

        // ---------------------------------------------------------- //
        // -------------------- Page Management --------------------- //
        // ---------------------------------------------------------- //

        // ---------- Initialize and Load ----------- //

        public ActionsPage()
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

            //if (pageMode == PageFunctions.Amend) 
            //{
            //    HeaderImage2.SetResourceReference(Image.SourceProperty, "AmendIcon");
            //}


            refreshClientCombo();
            refreshStatusCombo();
            FromDate.SelectedDate = fromDate = Globals.StartOfMonth;
            ToDate.SelectedDate = toDate = Globals.Today;
            ProjectFunctions.SetActionStatusOptions();
            refreshCompletedList();

            Instructions.Content = defaultInstructions;

            pageLoaded = true;
            refreshActionsGrid();
        }



        // ---------------------------------------------------------- //
        // -------------------- Data Management --------------------- //
        // ---------------------------------------------------------- //  

        // ------------- Data retrieval ------------- // 		


        private void refreshActionsGrid()
        {
            try
            {
                if (!pageLoaded) { return; }
                int clientID = clientSelected ? Globals.SelectedClientProxy.ID : 0;
                int projectID = projectSelected ? Globals.SelectedProjectProxy.ProjectID : 0;
                actionList = ProjectFunctions.ActionsList(clientID, Globals.SelectedStatusFilter, projectID, fromDate, toDate, selectedPerson, completed); // TODO: Replace/alternate selectedPerson with non-exact name

                if (projectSelected)
                {
                    ActionDataGrid.IsReadOnly = false;
                    loggedByList = ProjectFunctions.GetInternalTeam(projectID);
                    stageList = ProjectFunctions.GetProjectHistoryStages(projectID);
                    ownerList = ProjectFunctions.CombinedTeamList(projectID);
                }
                else
                {
                    ActionDataGrid.IsReadOnly = true;
                    loggedByList = actionList.Select(al => al.LoggedBy).Distinct().ToList();
                    //stageList = ProjectFunctions.FullStageList;
                    stageList = actionList.Select(al => al.LinkedStage).Distinct().ToList();
                    ownerList = actionList.Select(al => al.Owner).Distinct().ToList();
                }

                
                LoggedByColumn.ItemsSource = loggedByList;
                StageColumn.ItemsSource = stageList;
                OwnerColumn.ItemsSource = ownerList;

                ActionDataGrid.ItemsSource = actionList;

                
                
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating the actions data grid", generalException); }
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
                ProjectFunctions.SetProjectFilterList(Globals.SelectedStatusFilter, false, clientID, false);
                ProjectCombo.ItemsSource = ProjectFunctions.ProjectFilterList;
                selectProject(currentRecord.ProjectID);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating projects drop-down list", generalException); }
        }

        private void refreshNamesList()
        {
            try
            {
                exactName = false;
                string nameLike = NameLike.Text;
                if (nameLike == "") { PossibleNames.Visibility = Visibility.Hidden; }
                else
                {
                    int projectClientID = clientSelected ? Globals.SelectedClientProxy.ID : 0;
                    int projectID = projectSelected ? Globals.SelectedProjectProxy.ProjectID : 0;
                    PossibleNames.Visibility = Visibility.Visible;
                    List<CombinedStaffMember> teamDropList = ProjectFunctions.CombinedStaffList(nameLike: nameLike, clientID: projectClientID, projectID: projectID);
                    PossibleNames.ItemsSource = teamDropList;
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing name change", generalException); }
        }

        private void refreshCompletedList()
        {
            try
            {
                completedList = ProjectFunctions.ActionCompletedList(true);
                CompleteCombo.ItemsSource = completedList;
                CompleteCombo.SelectedValue = Globals.AllRecords;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating action status drop-down list", generalException); }
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

        private void setCurrentClient(ClientProxy clientProxy = null)
        {
            try
            {
                if (clientProxy == null || clientProxy.ID <= 0)
                {
                    Globals.SelectedClientProxy = null;                   
                    clientSelected = false;
                }
                else
                {
                    Globals.SelectedClientProxy = clientProxy;                    
                    clientSelected = true;
                }
                togglePageHeader();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing project client selection", generalException); }
        }

        private void toggleProjectMode(bool specificProject)
        {
            projectSelected = 
                //ProjectButton.IsEnabled = 
                specificProject;
            //toggleEditButtons(projectSelected);
            //ProjectButtonText.Text = (!projectSelected) ? "Set Project" : "All Projects";
            //toggleProjectSearchButton();
            //toggleProjectColumns();
            togglePageHeader();
        }

        private void togglePageHeader()
        {
            if (projectSelected)
            {
                PageHeader.Content = defaultHeader + " for Project " + Globals.SelectedProjectProxy.ProjectCode + " (" + Globals.SelectedProjectProxy.ProjectName + ")";
            }
            else if (clientSelected)
            {
                PageHeader.Content = defaultHeader + " for Client " + Globals.SelectedClientProxy.ClientCode + " (" + Globals.SelectedClientProxy.ClientName + ")";
            }
            else { PageHeader.Content = defaultHeader; }
        }

        private void chooseCombinedStaffMember()
        {
            try
            {
                selectedPerson = (CombinedStaffMember) PossibleNames.SelectedItem;
                NameLike.Text = selectedPerson.FullName;
                exactName = true;
                nameFilter();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing contact name selection", generalException); }
        }

        private void nameFilter()
        {
            try
            {
                PossibleNames.Visibility = Visibility.Hidden;
                nameLike = NameLike.Text;
                if (nameLike == "") 
                { 
                    exactName = false;
                    selectedPerson = null;
                }
                //toggleContactNameColumn();
                refreshActionsGrid();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error updating filters for contact name selection change", generalException); }
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
                else
                {
                    setCurrentClient((ClientProxy)ClientCombo.SelectedItem);
                    //refreshActionsGrid();
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
                    string selection = StatusCombo.SelectedItem.ToString();
                    selection = selection.Replace(" ", "");
                    Globals.SelectedStatusFilter = (Globals.ProjectStatusFilter)Enum.Parse(typeof(Globals.ProjectStatusFilter), selection);
                    refreshActionsGrid();
                    refreshProjectCombo();
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
                    setCurrentClient(Globals.SelectedProjectProxy.Client ?? null);
                    toggleProjectMode(Globals.SelectedProjectProxy != Globals.AllProjects);
                    refreshActionsGrid();                    
                }
                catch (Exception generalException) { MessageFunctions.Error("Error processing project selection", generalException); }
            }
        }

        private void FromDate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (FromDate.SelectedDate != null)
            {
                fromDate = (DateTime)FromDate.SelectedDate;
                if (toDate != null && toDate < fromDate) { ToDate.SelectedDate = fromDate; }
            }
            else { fromDate = Globals.InfiniteDate; }
            refreshActionsGrid();
        }

        private void ToDate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ToDate.SelectedDate != null)
            {
                toDate = (DateTime)ToDate.SelectedDate;
                if (fromDate != null && fromDate > toDate) { FromDate.SelectedDate = toDate; }
            }
            else { toDate = Globals.StartOfTime; }
            refreshActionsGrid();
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
            if (PossibleNames.SelectedItem != null) { chooseCombinedStaffMember(); }
        }

        private void CompleteCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CompleteCombo.SelectedValue != null)
            {
                string value = (string) CompleteCombo.SelectedValue;
                completed = ProjectFunctions.GetCompletedCode(value);
                refreshActionsGrid();
            }
        }

        private void ActionDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // To do: check for changes if appropriate

            PageFunctions.ShowTilesPage();
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {

        }


    } // class
} // namespace
