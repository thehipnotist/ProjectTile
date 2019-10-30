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
        string defaultInstructions = "The top filters refer to the project, the bottom ones to the actions.";
        const string defaultHeader = "Project Actions";
        bool projectSelected = false;
        bool clientSelected = false;
        bool canEdit = false;
        bool editing = false;
        bool changesMade = false;
        bool firstRefresh = true;

        // ------------ Current variables ----------- // 

        DateTime fromDate = Globals.InfiniteDate;
        DateTime toDate = Globals.StartOfTime;
        string nameLike = "";
        int completed = -1;
        ActionProxy selectedAction = null;

        // ------------- Current records ------------ //

        CombinedStaffMember selectedPerson = null;

        // ------------------ Lists ----------------- //
        List<string> completedFilterList = null;
        List<int> projectTeamStaffIDs = null;
        List<ProjectStages> stageList = null;
        List<CombinedTeamMember> ownerList = null;
        List<string> completedOptions = null;

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
                canEdit = (pageMode != PageFunctions.View && Globals.MyPermissions.Allow("EditActions"));                
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                PageFunctions.ShowTilesPage();
            }

            ProjectButton.Margin = CommitButton.Margin;
            refreshClientCombo();
            refreshStatusCombo();
            FromDate.SelectedDate = fromDate = Globals.StartOfTime;
            ToDate.SelectedDate = toDate = Globals.OneMonthAhead;            
            setCompletedLists();

            pageLoaded = true;
            refreshActionsGrid();
            ProjectFunctions.ActionsChanged += actionsAmended;
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
                Globals.LoadingActions = true;
                ActionProxy currentAction = selectedAction ?? null;
                NotesButton.IsEnabled = true;
                NotesBox.Visibility = Visibility.Hidden;

                int clientID = Globals.SelectedClientProxy.ID;
                int projectID = projectSelected ? Globals.SelectedProjectProxy.ProjectID : 0;
                projectTeamStaffIDs = projectSelected ? ProjectFunctions.GetInternalTeam(projectID).Select(git => git.StaffID).ToList() : null;
                bool inTeam = (projectSelected && projectTeamStaffIDs.Contains(Globals.MyStaffID));
                bool isOld = Globals.SelectedProjectProxy.IsOld;
                ProjectFunctions.ActionCounter = 0;
                ProjectCodeColumn.Visibility = projectSelected ? Visibility.Collapsed : Visibility.Visible;                

                ProjectFunctions.SetActionsList(clientID, Globals.SelectedStatusFilter, projectID, fromDate, toDate, selectedPerson, completed);

                if (canEdit && projectSelected && !isOld && inTeam)
                {
                    bool setUp = setUpEditing(projectID);
                    if (!setUp) { return; }
                }
                else { setUpReadOnly(inTeam, isOld); }
                
                LoggedByColumn.ItemsSource = ProjectFunctions.LoggedByList;
                StageColumn.ItemsSource = stageList;
                OwnerColumn.ItemsSource = ownerList;

                ActionDataGrid.ItemsSource = ProjectFunctions.ActionList;
                if (currentAction != null && currentAction.ID > 0 && ProjectFunctions.ActionList.Exists(al => al.ID == currentAction.ID))
                {
                    ActionDataGrid.SelectedItem = ProjectFunctions.ActionList.FirstOrDefault(al => al.ID == currentAction.ID);
                    ActionDataGrid.ScrollIntoView(ActionDataGrid.SelectedItem);
                }
                Globals.LoadingActions = firstRefresh = false;                
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating the actions data grid", generalException); }
        }

        private bool setUpReadOnly(bool inTeam, bool isOld)
        {
            try
            {

                ProjectButtonText.Text = projectSelected ? "All Projects" : "Set Project";
                ProjectFunctions.LoggedByList = ProjectFunctions.ActionList.Select(al => al.LoggedBy).Distinct().ToList();
                stageList = ProjectFunctions.ActionList.Select(al => al.LinkedStage).Distinct().ToList();
                ownerList = ProjectFunctions.ActionList.Select(al => al.Owner).Distinct().ToList();
                //Instructions.Content = defaultInstructions;                
                
                if (editing || firstRefresh)
                {
                    editing = false;
                    ActionDataGrid.IsReadOnly = true;
                    ActionDataGrid.BorderThickness = new Thickness(1);
                    ProjectButton.Visibility = Visibility.Visible;                    
                    CommitButton.Visibility = Visibility.Hidden;
                    NotesButtonText.Text = "View Notes";
                    NotesButton.IsEnabled = false;
                }

                if (!projectSelected && canEdit)
                {
                    Instructions.Content = defaultInstructions + (canEdit ? " Choose a project to edit." : "");
                    MessageFunctions.InfoAlert("If you are a team member of an active project, you can edit its actions by selecting the project; the table becomes italic with a thicker border. "
                                                + "Further instructions will then appear.", "'Read-only' Mode");
                }
                else
                {
                    Instructions.Content = defaultInstructions;
                    if (projectSelected && canEdit)
                    {
                        if (isOld)
                        {
                            string caption = (Globals.SelectedProjectProxy.IsCancelled) ? "Project Cancelled" : "Project Completed";
                            MessageFunctions.InfoAlert("This project is closed, so its actions cannot be edited.", caption);
                        }
                        else if (!inTeam)
                        {
                            MessageFunctions.InfoAlert("Only members of the project team can edit the actions for this project. Please speak to the Project Manager if you require access.",
                                "'Read-only' Mode");
                        }
                    }
                }
                return true;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error setting up actions view", generalException);
                return false;
            }	
        }

        private bool setUpEditing(int projectID)
        {
            try
            {                
                ProjectFunctions.LoggedByList = ProjectFunctions.GetInternalTeam(projectID);
                stageList = ProjectFunctions.GetProjectHistoryStages(projectID);
                ownerList = ProjectFunctions.CombinedTeamList(projectID);

                foreach (ActionProxy action in ProjectFunctions.ActionList) // Required for the initial values to display when in 'project mode'
                {
                    int? stageID = action.LinkedStage.ID;
                    if (stageID != null) { action.LinkedStage = stageList.Where(sl => sl.ID == stageID).FirstOrDefault(); }

                    int loggedByID = action.LoggedBy.ID;
                    action.LoggedBy = ProjectFunctions.LoggedByList.Where(lbl => lbl.ID == loggedByID).FirstOrDefault();

                    bool internalOwner = (action.Owner.InternalTeamMember != null);
                    int ownerID = internalOwner ? action.Owner.InternalTeamMember.ID : action.Owner.ClientTeamMember.ID;
                    action.Owner = ownerList.Where(ol => (internalOwner && ol.InternalTeamMember.ID == ownerID)
                                                      || (!internalOwner && ol.ClientTeamMember != null && ol.ClientTeamMember.ID == ownerID))
                                                      .FirstOrDefault();
                }

                if (!editing || firstRefresh)
                {
                    editing = true;
                    ProjectButtonText.Text = "All Projects";
                    ActionDataGrid.IsReadOnly = false;
                    ActionDataGrid.BorderThickness = new Thickness(3);
                    Instructions.Content = "Edit rows by double-clicking cells in italics (others are updated automatically).";
                    MessageFunctions.InfoAlert("To add new rows, scroll to the bottom of the table and edit the first empty row. Each action must have an 'owner' from the project team, "
                        + "and either a linked stage or a target completion date.", "'Direct Edit' Mode");
                }
                return true;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error setting up action editing", generalException);
                return false;
            }
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

        private void refreshNamesList()
        {
            try
            {
                selectedPerson = null;
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

        private void setCompletedLists()
        {
            try
            {
                ProjectFunctions.SetActionStatusOptions();
                
                completedFilterList = ProjectFunctions.ActionCompletedList(true, true);
                CompleteCombo.ItemsSource = completedFilterList;
                CompleteCombo.SelectedValue = "No";

                completedOptions = ProjectFunctions.ActionCompletedList(false, false);
                CompleteColumn.ItemsSource = completedOptions;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating action status drop-down list", generalException); }
        }

        private void actionsAmended()
        {
            if (!changesMade)
            {
                ProjectButton.Visibility = Visibility.Hidden;
                CommitButton.Visibility = Visibility.Visible;
                CancelButtonText.Text = "Cancel";
                changesMade = true;
                //ActionDataGrid.S
            }
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

        private void setCurrentClient(ClientProxy clientProxy)
        {
            try
            {
                Globals.SelectedClientProxy = clientProxy;
                clientSelected = (clientProxy != null && clientProxy.ID > 0);
                togglePageHeader();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing project client selection", generalException); }
        }

        private void toggleProjectMode(bool specificProject)
        {
            projectSelected = specificProject;
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
                    selectedPerson = null;
                    refreshActionsGrid();
                }
                else if (selectedPerson == null) { NameLike.Text = ""; }
                else { refreshActionsGrid(); }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error updating filters for contact name selection change", generalException); }
        }

        private void resetChanges()
        {
            changesMade = false;
            CommitButton.Visibility = Visibility.Hidden;
            ProjectButton.Visibility = Visibility.Visible;
            CancelButtonText.Text = "Close";
            refreshActionsGrid();
        }

        private bool ignoreChanges()
        {
            if (!editing || !changesMade) { return true; }
            else 
            {
                bool ignore = MessageFunctions.WarningYesNo("This will clear all unsaved changes you have made. Continue?", "Undo Unsaved Changes?");
                if (ignore) { resetChanges(); }
                return ignore;
            }
        }

        private void hideNotes()
        {
            string notes = NotesBox.Text;
            selectedAction.Notes = notes;
            NotesBox.Visibility = Visibility.Hidden;            
            NotesButtonText.Text = (notes != "") ? "Edit Notes" : "Add Notes";
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
                else if (ignoreChanges())
                {
                    setCurrentClient((ClientProxy)ClientCombo.SelectedItem);
                    refreshActionsGrid();
                    refreshProjectCombo();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing client selection", generalException); }	
        }

        private void StatusCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (StatusCombo.SelectedItem != null && ignoreChanges())
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
            else if ( Globals.SelectedProjectProxy != (ProjectProxy)ProjectCombo.SelectedItem && ignoreChanges())
            {
                try
                {
                    Globals.SelectedProjectProxy = (ProjectProxy)ProjectCombo.SelectedItem;
                    toggleProjectMode(Globals.SelectedProjectProxy != Globals.AllProjects);
                    refreshActionsGrid();                    
                }
                catch (Exception generalException) { MessageFunctions.Error("Error processing project selection", generalException); }
            }
            else { ProjectCombo.SelectedItem = Globals.SelectedProjectProxy; }
        }

        private void FromDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FromDate.SelectedDate != null)
            {
                if (FromDate.SelectedDate != fromDate && ignoreChanges())
                {
                    fromDate = (DateTime)FromDate.SelectedDate;
                    if (toDate != null && toDate < fromDate) { ToDate.SelectedDate = fromDate; }
                    else { refreshActionsGrid(); }
                }
                else { FromDate.SelectedDate = fromDate; }
            }
            else { fromDate = Globals.StartOfTime; }            
        }

        private void ToDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ToDate.SelectedDate != null)
            {
                if (ToDate.SelectedDate != toDate && ignoreChanges())
                {
                    toDate = (DateTime)ToDate.SelectedDate;
                    if (fromDate != null && fromDate > toDate) { FromDate.SelectedDate = toDate; }
                    else { refreshActionsGrid(); }
                }
                else { ToDate.SelectedDate = toDate; }
            }
            else { toDate = Globals.InfiniteDate; }
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
            if (PossibleNames.SelectedItem != null && ignoreChanges()) { chooseCombinedStaffMember(); }
        }

        private void CompleteCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CompleteCombo.SelectedValue != null && ignoreChanges())
            {
                string value = (string) CompleteCombo.SelectedValue;
                completed = ProjectFunctions.GetCompletedKey(value);
                refreshActionsGrid();
            }
        }

        private void ActionDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActionDataGrid.SelectedItem == null) { selectedAction = null; }
            else
            {
                selectedAction = ActionDataGrid.SelectedItem as ActionProxy;
                bool notesExist = (selectedAction != null && selectedAction.Notes != null && selectedAction.Notes != "");
                if (editing) { NotesButtonText.Text = notesExist? "Edit Notes" : "Add Notes"; }
                else { NotesButton.IsEnabled = notesExist; }
            }
            ProjectButton.IsEnabled =  (projectSelected || selectedAction != null);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ignoreChanges()) { return; }            
            else { PageFunctions.ShowTilesPage(); }
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: save changes

            resetChanges();
        }

        private void ProjectButton_Click(object sender, RoutedEventArgs e)
        {
            if (projectSelected)
            {
                try
                {
                    //if (!rolesCheck()) { return; }
                    ProjectCombo.SelectedItem = ProjectFunctions.ProjectFilterList.First(pfl => pfl.ProjectID == 0);
                }
                catch (Exception generalException) { MessageFunctions.Error("Error processing return to all projects", generalException); }
            }
            else if (ActionDataGrid.SelectedItem == null) { return; }
            else { ProjectCombo.SelectedItem = ProjectFunctions.ProjectFilterList.First(pfl => pfl.ProjectCode == selectedAction.Project.ProjectCode); }
        }

        private void NotesButton_Click(object sender, RoutedEventArgs e)
        {
            if (NotesBox.IsVisible) { hideNotes(); }
            else 
            {
                NotesBox.Visibility = Visibility.Visible;
                NotesBox.Text = selectedAction.Notes;
                NotesButtonText.Text = "Hide Notes";
                NotesBox.Focus();
            }        
        }

        private void NotesBox_LostFocus(object sender, RoutedEventArgs e)
        {
            hideNotes();
        }


    } // class
} // namespace
