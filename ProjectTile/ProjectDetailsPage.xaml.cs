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
using System.ComponentModel;
using System.Collections.ObjectModel;

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

        MainWindow winMain = (MainWindow)App.Current.MainWindow;
        string pageMode;
        bool fromProjectPage = (Globals.ProjectSourcePage != Globals.TilesPageName);

        // ------------ Current variables ----------- //

        // ------------- Current records ------------ //

        private ProjectSummaryRecord thisProjectSummary = null;

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
                    StageCombo.IsEnabled = false;
                    StageCombo.ToolTip = "Your current permissions do not allow updating the project stage";
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
                else if (pageMode == PageFunctions.Amend && Globals.SelectedProjectSummary != null)  // Just to be sure
                {
                    setUpAmendMode();
                }
                else
                {
                    MessageFunctions.Error("Error: no project selected.", null);
                    closeDetailsPage(false, false);
                }
            }
            
            this.DataContext = thisProjectSummary;                        
        }

        // ---------------------------------------------------------- //
        // -------------------- Data Management --------------------- //
        // ---------------------------------------------------------- //  

        // -------------- Data updates -------------- //

        private void refreshManagerCombo(bool anyActive)
        {
            try
            {
                int currentManagerID = (thisProjectSummary.ProjectManager != null)? thisProjectSummary.ProjectManager.ID : 0;
                ProjectFunctions.SetPMOptionsList(anyActive, currentManagerID);
                ManagerCombo.ItemsSource = ProjectFunctions.PMOptionsList;
                if (currentManagerID > 0)
                {
                    try
                    {
                        StaffSummaryRecord selectedManager = ProjectFunctions.GetPMInOptionsList(currentManagerID);
                        ManagerCombo.SelectedIndex = ProjectFunctions.PMOptionsList.IndexOf(selectedManager);
                    }
                    catch (Exception generalException) { MessageFunctions.Error("Error selecting current Project Manager", generalException); }
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating the drop-down list of Project Managers", generalException); }
        }
        
        private void refreshClientCombo()
        {
            try
            {
                int currentClientID = (thisProjectSummary.Client != null) ? thisProjectSummary.Client.ID : 0;
                ProjectFunctions.SetClientOptionsList(currentClientID);
                ClientCombo.ItemsSource = ProjectFunctions.ClientOptionsList;
                if (currentClientID != 0)
                {
                    try
                    {
                        ClientSummaryRecord selectedClient = ProjectFunctions.GetClientInOptionsList(currentClientID);
                        ClientCombo.SelectedIndex = ProjectFunctions.ClientOptionsList.IndexOf(selectedClient);
                    }
                    catch (Exception generalException) { MessageFunctions.Error("Error selecting current client", generalException); }
                }
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
                ProjectCode.IsEnabled = true; ProjectCode.IsReadOnly = true;
                CommitButton.Visibility = NextButton.Visibility = NonPMs_CheckBox.Visibility = SearchButton.Visibility = Visibility.Hidden;
                CancelButtonText.Text = "Close";
                PageHeader.Content = "View Project Details";
                Instructions.Content = "This page is read-only; values can be selected but not changed.";
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error displaying project details", generalException);
                closeDetailsPage(false, false);
            }

            if (Globals.SelectedProjectSummary != null) // Just to be sure
            {
                try
                {
                    thisProjectSummary = Globals.SelectedProjectSummary;
                    TypeCombo.Items.Add(thisProjectSummary.Type);
                    StageCombo.Items.Add(thisProjectSummary.Stage);
                    ManagerCombo.Items.Add(thisProjectSummary.ProjectManager);
                    if (thisProjectSummary.Client != null && thisProjectSummary.Client.ID > 0) { ClientCombo.Items.Add(thisProjectSummary.Client); }
                    else
                    {
                        ClientCombo.Items.Add(Globals.NoClient);
                        ClientCombo.SelectedIndex = 0;
                    }
                }
                catch (Exception generalException) { MessageFunctions.Error("Error setting current project details", generalException); }
            }
        }

        private void setUpNewMode()
        {
            thisProjectSummary = new ProjectSummaryRecord();
            PageHeader.Content = "Create New Project";
            Instructions.Content = "Fill in the details as required and then click 'Save' to create the record.";
            thisProjectSummary.Stage = ProjectFunctions.GetStageByCode(0);
            if (fromProjectPage)
            {
                // To do: set up certain fields as with an amendment based on current selection - combine those parts into a single function?
            }
            else { BackButton.Visibility = Visibility.Hidden; }
            refreshManagerCombo(false);
            refreshClientCombo();
        }

        private void displaySelectedStage()
        {
            try
            {
                // Necessary because the binding won't find the record in the list automatically
                ProjectStages selectedStage = ProjectFunctions.GetStageByCode(thisProjectSummary.Stage.StageCode);
                StageCombo.SelectedIndex = ProjectFunctions.FullStageList.IndexOf(selectedStage);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error selecting current project stage", generalException); }
        }

        private void setUpAmendMode()
        {
            thisProjectSummary = Globals.SelectedProjectSummary;
            try
            {
                // For some reason this is necessary initially (alternatively can add the project's type first, then all others, then sort)
                ProjectTypes selectedType = ProjectFunctions.FullTypeList.FirstOrDefault(tl => tl.TypeCode == thisProjectSummary.Type.TypeCode);
                TypeCombo.SelectedIndex = ProjectFunctions.FullTypeList.IndexOf(selectedType);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error selecting current project type", generalException); }
            displaySelectedStage();
            refreshManagerCombo(false);
            refreshClientCombo();
        }

        private void closeDetailsPage(bool closeAll, bool checkFirst)
        {
            if (checkFirst && pageMode != PageFunctions.View)
            {
                bool doClose = MessageFunctions.QuestionYesNo("Are you sure you want to close this page? Any changes you have made will be lost.");
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
                thisProjectSummary.Client = ClientFunctions.SelectedProjectClient;
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
            MessageBox.Show(thisProjectSummary.Type.TypeCode.ToString() + " " + thisProjectSummary.Type.TypeName);
        }

        private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // To do - validate if it is an internal project bu the client is selected, or vice versa if 'no client' is selected
        }

        private void ClientCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // To do - validation from above
        }

        private void StageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StageCombo.SelectedItem != null && pageMode != PageFunctions.View)
            {
                int newStage = thisProjectSummary.Stage.StageCode;
                
                // To do - validate and process the logic of the selection change - possibly only at saving stage though, e.g. 
                //    - if now Live then the product should be Live
                //    - if beyond pre-project, should have all of the details
                
                NextButton.IsEnabled = (!ProjectFunctions.IsLastStage(newStage));
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int newStage = thisProjectSummary.Stage.StageCode + 1;
                thisProjectSummary.Stage = ProjectFunctions.GetStageByCode(newStage);
                displaySelectedStage();
                NextButton.IsEnabled = (!ProjectFunctions.IsLastStage(newStage));
            }
            catch (Exception generalException) { MessageFunctions.Error("Error moving to the next stage", generalException); }
        }

        private void ManagerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void NonPMs_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            refreshManagerCombo(true);
        }

        private void NonPMs_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            refreshManagerCombo(false);
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            OpenClientLookup();
        }
 
    }
}
