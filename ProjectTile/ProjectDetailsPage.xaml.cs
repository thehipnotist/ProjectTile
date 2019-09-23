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
        // ---------------------- //
        // -- Global Variables -- //
        // ---------------------- //

        // Global/page parameters //

        MainWindow winMain = (MainWindow)App.Current.MainWindow;
        string pageMode;
        bool fromProjectPage = (Globals.ProjectSourcePage != Globals.TilesPageName);

        // Current variables //

        // Current records //
        private ProjectSummaryRecord thisProjectSummary = null;

        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //

        // Initialize and Load //
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
                closePage(false, false);
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

                    int currentManagerID = (Globals.SelectedProjectSummary != null) ? Globals.SelectedProjectSummary.ProjectManager.ID : 0;
                    ProjectFunctions.SetPMOptionsList(currentManagerID);
                    ManagerCombo.ItemsSource = ProjectFunctions.PMOptionsList;
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
                    closePage(false, false);
                }
            }
            
            this.DataContext = thisProjectSummary;
            ClientCombo.ItemsSource = ProjectFunctions.ClientComboList;            
        }

        // ---------------------- //
        // -- Data Management --- //
        // ---------------------- //

        // Data updates //
        private void setUpViewMode()
        {
            try
            {
                ClientCombo.IsEnabled = ProjectName.IsEnabled = TypeCombo.IsEnabled = StartDate.IsEnabled = false;
                ManagerCombo.IsEnabled = StageCombo.IsEnabled = ProjectSummary.IsEnabled = false;
                CommitButton.Visibility = NextButton.Visibility = Visibility.Hidden;
                CancelButtonText.Text = "Close";
                PageHeader.Content = "View Project Details";
                Instructions.Content = "";
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error displaying project details", generalException);
                closePage(false, false);
            }

            if (Globals.SelectedProjectSummary != null) // Just to be sure
            {
                try
                {
                    thisProjectSummary = Globals.SelectedProjectSummary;
                    TypeCombo.Items.Add(thisProjectSummary.ProjectType);
                    StageCombo.Items.Add(thisProjectSummary.ProjectStage);
                    ManagerCombo.Items.Add(thisProjectSummary.ProjectManager);
                }
                catch (Exception generalException) { MessageFunctions.Error("Error setting current project details", generalException); }
            }
        }

        private void setUpNewMode()
        {
            thisProjectSummary = new ProjectSummaryRecord();
            PageHeader.Content = "Create New Project";
            Instructions.Content = "Fill in the details as required and then click 'Save' to create the record.";
            thisProjectSummary.ProjectStage = ProjectFunctions.GetStageByCode(0);
            if (fromProjectPage)
            {
                // To do: set up certain fields as with an amendment based on current selection - combine those parts into a single function?
            }
            else { BackButton.Visibility = Visibility.Hidden; }       
        }

        private void setUpAmendMode()
        {
            thisProjectSummary = Globals.SelectedProjectSummary;            
            try
            {                      
                // For some reason this is necessary initially (alternatively can add the project's type first, then all others, then sort)
                ProjectTypes selectedType = ProjectFunctions.FullTypeList.FirstOrDefault(tl => tl.TypeCode == thisProjectSummary.ProjectType.TypeCode);
                TypeCombo.SelectedIndex = ProjectFunctions.FullTypeList.IndexOf(selectedType);          
            }
            catch (Exception generalException) { MessageFunctions.Error("Error selecting current project type", generalException); }
            try
            {
                // As above)
                ProjectStages selectedStage = ProjectFunctions.GetStageByCode(thisProjectSummary.ProjectStage.StageCode);
                StageCombo.SelectedIndex = ProjectFunctions.FullStageList.IndexOf(selectedStage);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error selecting current project stage", generalException); }
            try
            {
                // As above)
                StaffSummaryRecord selectedManager = ProjectFunctions.GetPMSummary(thisProjectSummary.ProjectManager.ID);                
                ManagerCombo.SelectedIndex = ProjectFunctions.PMOptionsList.IndexOf(selectedManager);
                //MessageBox.Show(ProjectFunctions.PMOptionsList.IndexOf(selectedManager).ToString());
            }
            catch (Exception generalException) { MessageFunctions.Error("Error selecting current Project Manager", generalException); }
        }

        private void closePage(bool closeAll, bool checkFirst)
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

        // Shared functions //

        // ---------------------- //
        // -- Event Management -- //
        // ---------------------- //

        // Control-specific events //

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            closePage(true, true);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            closePage(false, true);
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(thisProjectSummary.ProjectType.TypeCode.ToString() + " " + thisProjectSummary.ProjectType.TypeName);
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
                int newStage = thisProjectSummary.ProjectStage.StageCode;
                
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
                int newStage = thisProjectSummary.ProjectStage.StageCode + 1;
                thisProjectSummary.ProjectStage = ProjectFunctions.GetStageByCode(newStage);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error moving to the next stage", generalException); }
        }

        private void ManagerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
 
    }
}
