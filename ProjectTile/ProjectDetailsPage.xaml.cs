using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ProjectTile
{
    /// <summary>
    /// Interaction logic for ProjectDetailsPage.xaml
    /// </summary>
    public partial class ProjectDetailsPage : Page
    {
        // ---------------------------------------------------------- //
        // -------------------- Global Variables -------------------- //
        // ---------------------------------------------------------- //

        // --------- Global/page parameters --------- //

        string pageMode;
        bool fromProjectPage = (Globals.ProjectSourcePage != Globals.TilesPageName);
        int originalManagerID = 0;
        int originalStageNumber = 0;

        // ------------ Current variables ----------- //

        // ------------- Current records ------------ //

        private ProjectProxy thisProjectProxy = null;

        // ---------------------------------------------------------- //
        // -------------------- Page Management --------------------- //
        // ---------------------------------------------------------- //

        // ---------- Initialize and Load ----------- //

        public ProjectDetailsPage()
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
                closeDetailsPage(false, false);
            }            
            
            if (pageMode==PageFunctions.View) { setUpViewMode(); }
            else // Amend or new
            {
                if (!Globals.MyPermissions.Allow("ActivateProjects"))
                {
                    StageCombo.IsEnabled = NextButton.IsEnabled = false;
                    StageCombo.ToolTip = NextButton.ToolTip = "Your current permissions do not allow updating the project stage";
                }
                try
                {
                    ProjectFunctions.SetFullTypeList();
                    TypeCombo.ItemsSource = ProjectFunctions.FullTypeList;
                    ProjectFunctions.SetFullStageList();
                    StageCombo.ItemsSource = ProjectFunctions.FullStageList;
                }
                catch (Exception generalException) { MessageFunctions.Error("Error populating drop-down lists", generalException); }

                if (pageMode == PageFunctions.New) { setUpNewMode(); }
                else if (pageMode == PageFunctions.Amend && Globals.SelectedProjectProxy != null)  // Just to be sure
                {
                    setUpAmendMode();
                }
                else
                {
                    MessageFunctions.Error("Error: no project selected.", null);
                    closeDetailsPage(false, false);
                }
            }
            
            this.DataContext = thisProjectProxy;                        
        }

        // ---------------------------------------------------------- //
        // -------------------- Data Management --------------------- //
        // ---------------------------------------------------------- //  

        // -------------- Data updates -------------- //

        private void refreshManagerCombo(bool anyActive)
        {
            try
            {
                int currentManagerID = (thisProjectProxy.ProjectManager != null)? thisProjectProxy.ProjectManager.ID : 0;
                ProjectFunctions.SetPMOptionsList(anyActive, currentManagerID);
                ManagerCombo.ItemsSource = ProjectFunctions.PMOptionsList;
                if (currentManagerID > 0) { displaySelectedManager(currentManagerID); }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating the drop-down list of Project Managers", generalException); }
        }
        
        private void refreshClientCombo()
        {
            try
            {
                int currentClientID = (thisProjectProxy.Client != null) ? thisProjectProxy.Client.ID : 0;
                ProjectFunctions.SetClientOptionsList(currentClientID);
                ClientCombo.ItemsSource = ProjectFunctions.ClientOptionsList;
                if (currentClientID != 0) { displaySelectedClient(currentClientID); }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating the drop-down list of clients", generalException); }
        }

        // --------- Other/shared functions --------- // 

        private void setUpViewMode()
        {
            try
            {
                ClientCombo.IsReadOnly = ProjectName.IsReadOnly = TypeCombo.IsReadOnly = true;
                ManagerCombo.IsReadOnly = StageCombo.IsReadOnly = ProjectSummary.IsReadOnly = true;
                StartDate.IsEnabled = false; // This cannot be read-only so an inner style trigger makes it appear read-only
                ProjectCode.IsEnabled = true; 
                ProjectCode.IsReadOnly = true;
                CommitButton.Visibility = NextButton.Visibility = NonPMsCheckBox.Visibility = SearchButton.Visibility = Visibility.Hidden;
                CancelButtonText.Text = "Close";
                PageHeader.Content = "View Project Details";
                HeaderImage2.SetResourceReference(Image.SourceProperty, "ViewIcon");
                Instructions.Content = "This page is read-only; values can be selected but not changed.";
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error displaying project details", generalException);
                closeDetailsPage(false, false);
            }

            if (Globals.SelectedProjectProxy != null) // Just to be sure (for view mode)
            {
                try
                {
                    thisProjectProxy = Globals.SelectedProjectProxy;
                    TypeCombo.Items.Add(thisProjectProxy.Type);
                    StageCombo.Items.Add(thisProjectProxy.Stage);
                    ManagerCombo.Items.Add(thisProjectProxy.ProjectManager);
                    if (thisProjectProxy.Client != null && thisProjectProxy.Client.ID > 0) { ClientCombo.Items.Add(thisProjectProxy.Client); }
                    else
                    {
                        ClientCombo.Items.Add(Globals.NoClient);
                        ClientCombo.SelectedIndex = 0;
                    }
                    PageFunctions.ShowFavouriteButton();
                    ProjectFunctions.ToggleFavouriteButton(true);
                }
                catch (Exception generalException) { MessageFunctions.Error("Error setting current project details", generalException); }
            }
        }

        private void setUpNewMode()
        {
            thisProjectProxy = new ProjectProxy();
            thisProjectProxy.EntityID = Globals.CurrentEntityID;
            PageHeader.Content = "Create New Project";
            HeaderImage2.SetResourceReference(Image.SourceProperty, "AddIcon");
            Instructions.Content = "Fill in the details as required and then click 'Save' to create the record.";
            if (fromProjectPage) { bool usedFilters = ProjectFunctions.PopulateFromFilters(ref thisProjectProxy); }
            else { BackButton.Visibility = Visibility.Hidden; }
            refreshManagerCombo(false);
            refreshClientCombo();
            updateProjectStage(0);
        }

        private void setUpAmendMode()
        {
            thisProjectProxy = Globals.SelectedProjectProxy;
            originalManagerID = thisProjectProxy.ProjectManager.ID;
            originalStageNumber = thisProjectProxy.StageNumber;
            displaySelectedType();
            displaySelectedStage();
            refreshManagerCombo(false);
            refreshClientCombo();
            PageFunctions.ShowFavouriteButton();
            ProjectFunctions.ToggleFavouriteButton(true);
        }

        private void displaySelectedClient(int currentClientID)
        {
            // Necessary because the binding won't find the record in the list automatically
            try
            {
                ClientProxy selectedClient = ProjectFunctions.GetClientInOptionsList(currentClientID);
                ClientCombo.SelectedIndex = ProjectFunctions.ClientOptionsList.IndexOf(selectedClient);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error selecting current client", generalException); }
        }

        private void displaySelectedManager(int currentManagerID)
        {
            try
            {
                StaffProxy selectedManager = ProjectFunctions.GetPMInOptionsList(currentManagerID);
                ManagerCombo.SelectedIndex = ProjectFunctions.PMOptionsList.IndexOf(selectedManager);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error selecting current Project Manager", generalException); }
        }

        private void displaySelectedStage()
        {
            try
            {
                ProjectStages selectedStage = ProjectFunctions.GetStageByID(thisProjectProxy.StageID); // Gets it from FullStageList, so it is picked up
                StageCombo.SelectedIndex = ProjectFunctions.FullStageList.IndexOf(selectedStage);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error selecting current project stage", generalException); }
        }

        private void displaySelectedType()
        {
            try
            {
                ProjectTypes selectedType = ProjectFunctions.FullTypeList.FirstOrDefault(tl => tl.TypeCode == thisProjectProxy.Type.TypeCode);
                TypeCombo.SelectedIndex = ProjectFunctions.FullTypeList.IndexOf(selectedType);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error selecting current project type", generalException); }
        }

        private void updateProjectStage(int newStageNumber)
        {
            thisProjectProxy.Stage = ProjectFunctions.GetStageByNumber(newStageNumber);
            displaySelectedStage();
        }

        private void closeDetailsPage(bool closeAll, bool checkFirst)
        {
            if (checkFirst && pageMode != PageFunctions.View)
            {
                bool doClose = MessageFunctions.WarningYesNo("Are you sure you want to close this page? Any changes you have made will be lost.");
                if (!doClose) { return; }
            }
            bool closeFully = closeAll ? true : !fromProjectPage;
            if (closeFully) { ProjectFunctions.ReturnToTilesPage(); }
            else { ProjectFunctions.ReturnToProjectPage(); }
        }

        // ---------- Links to other pages ---------- //	

        public void OpenClientLookup()
        {
            try
            {
                NormalGrid.Visibility = Visibility.Hidden;
                LookupFrame.Visibility = Visibility.Visible;
                LookupFrame.Navigate(new Uri("ClientPage.xaml?Mode=Lookup", UriKind.RelativeOrAbsolute));
                ClientFunctions.SelectClientForProject += SelectProjectClient;
                ClientFunctions.CancelProjectClientSelection += CancelClientLookup;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error setting up client selection", generalException); }
        }

        public void CloseClientLookup()
        {
            LookupFrame.Content = null;
            LookupFrame.Visibility = Visibility.Hidden;
            NormalGrid.Visibility = Visibility.Visible;
        }

        public void SelectProjectClient()
        {
            try
            {
                CloseClientLookup();
                thisProjectProxy.Client = ClientFunctions.SelectedProjectClient;
                refreshClientCombo();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing client selection", generalException); }
        }

        public void CancelClientLookup()
        {
            CloseClientLookup();
        }

        // ---------------------------------------------------------- //
        // -------------------- Event Management -------------------- //
        // ---------------------------------------------------------- //  

        // -------- Control-specific events --------- //

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            closeDetailsPage(true, true);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            closeDetailsPage(false, true);
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            if (pageMode == PageFunctions.New) 
            {
                bool success = ProjectFunctions.SaveNewProject(ref thisProjectProxy);
                if (success) { closeDetailsPage(false, false); }
            }
            else
            {
                bool managerChanged = (thisProjectProxy.ProjectManager.ID != originalManagerID);
                bool success = ProjectFunctions.SaveProjectChanges(thisProjectProxy, managerChanged, originalStageNumber);
                if (success) { closeDetailsPage(false, false); }
            }
        }

        private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void ClientCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void StageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StageCombo.SelectedItem != null && pageMode != PageFunctions.View)
            {
                int newStage = thisProjectProxy.StageNumber;
                NextButton.IsEnabled = (!ProjectFunctions.IsLastStage(newStage));
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int newStage = thisProjectProxy.StageNumber + 1;
                updateProjectStage(newStage);
                NextButton.IsEnabled = (!ProjectFunctions.IsLastStage(newStage));
            }
            catch (Exception generalException) { MessageFunctions.Error("Error moving to the next stage", generalException); }
        }

        private void ManagerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void NonPMsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            refreshManagerCombo(true);
        }

        private void NonPMsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            refreshManagerCombo(false);
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            OpenClientLookup();
        }
 
    }
}
