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
    /// Interaction logic for TimelinePage.xaml
    /// </summary>
    public partial class TimelinePage : Page
    {
        // ---------------------------------------------------------- //
        // -------------------- Global Variables -------------------- //
        // ---------------------------------------------------------- //

        // --------- Global/page parameters --------- // 

        string pageMode;
        bool fromProjectPage = (Globals.ProjectSourcePage != Globals.TilesPageName);

        // ------------ Current variables ----------- // 

        TimelineProxy currentTimeline = new TimelineProxy();
        bool stageChanged = false;

        // ------------- Current records ------------ //



        // ------------------ Lists ----------------- //
        List<Label> dateLabels = new List<Label>();
        List<DatePicker> datePickers = new List<DatePicker>();

        // ---------------------------------------------------------- //
        // -------------------- Page Management --------------------- //
        // ---------------------------------------------------------- //

        // ---------- Initialize and Load ----------- //

        public TimelinePage()
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

            createControlArrays();
            refreshTimeData();
            refreshStageCombo();
            displaySelectedStage();
            
        }

        // ---------------------------------------------------------- //
        // -------------------- Data Management --------------------- //
        // ---------------------------------------------------------- //  

        // ------------- Data retrieval ------------- // 		

        private void refreshTimeData()
        {
            currentTimeline = ProjectFunctions.GetProjectTimeline(Globals.SelectedProjectProxy.ProjectID);
            this.DataContext = currentTimeline;
            highlightDateStatuses();
        }

        private void refreshStageCombo()
        {
            ProjectFunctions.SetFullStageList();
            StageCombo.ItemsSource = ProjectFunctions.FullStageList;            
        }

        // -------------- Data updates -------------- // 

        private void updateProjectStage(int newStageNumber)
        {
            currentTimeline.Stage = ProjectFunctions.GetStageByNumber(newStageNumber);            
            displaySelectedStage();
            showStageChange(newStageNumber);
        }

        private void showStageChange(int newStage)
        {
            stageChanged = true;
            NextButton.IsEnabled = (!ProjectFunctions.IsLastStage(newStage));
            updateAffectedDates();
            highlightDateStatuses();
        }

        private void updateAffectedDates()
        {
            //TODO: work out which dates to update
        }

        // --------- Other/shared functions --------- // 

        private void displaySelectedStage()
        {
            try
            {
                ProjectStages selectedStage = ProjectFunctions.GetStageByID(currentTimeline.StageID); // Gets it from FullStageList, so it is picked up
                StageCombo.SelectedIndex = ProjectFunctions.FullStageList.IndexOf(selectedStage);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error selecting current project stage", generalException); }
        }

        private void closePage(bool closeAll, bool checkFirst)
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

        private void createControlArrays()
        {
            dateLabels.Add(PreProjectLabel);
            dateLabels.Add(InitiationLabel);
            dateLabels.Add(TechPrepLabel);
            dateLabels.Add(DesignLabel);
            dateLabels.Add(InstallationLabel);
            dateLabels.Add(DataMigrationLabel);
            dateLabels.Add(ConfigurationLabel);
            dateLabels.Add(SystemTestLabel);
            dateLabels.Add(TrainAdminLabel);
            dateLabels.Add(UserTestLabel);
            dateLabels.Add(TrainUsersLabel);
            dateLabels.Add(LivePrepLabel);
            dateLabels.Add(GoLiveLabel);
            dateLabels.Add(LiveRunningLabel);
            dateLabels.Add(PostLiveLabel);
            dateLabels.Add(ClosureLabel);
            dateLabels.Add(CompletedLabel);
            dateLabels.Add(CancelledLabel);

            datePickers.Add(PreProject);
            datePickers.Add(Initiation);
            datePickers.Add(TechPrep);
            datePickers.Add(Design);
            datePickers.Add(Installation);
            datePickers.Add(DataMigration);
            datePickers.Add(Configuration);
            datePickers.Add(SystemTest);
            datePickers.Add(TrainAdmin);
            datePickers.Add(UserTest);
            datePickers.Add(TrainUsers);
            datePickers.Add(LivePrep);
            datePickers.Add(GoLive);
            datePickers.Add(LiveRunning);
            datePickers.Add(PostLive);
            datePickers.Add(Closure);
            datePickers.Add(Completed);
            datePickers.Add(Cancelled);
        }

        private void highlightDateStatuses()
        {
            int stageNumber = currentTimeline.StageNumber;
            
            for (int i=0; i<dateLabels.Count; i++)
            {
                dateLabels[i].FontWeight = datePickers[i].FontWeight = (i <= stageNumber) ? FontWeights.Bold : FontWeights.Normal;
                dateLabels[i].FontStyle = datePickers[i].FontStyle = (i <= stageNumber) ? FontStyles.Normal : FontStyles.Italic;
            }
        }

        // ---------- Links to other pages ---------- //		



        // ---------------------------------------------------------- //
        // -------------------- Event Management -------------------- //
        // ---------------------------------------------------------- //  

        // ---- Generic (shared) control events ----- // 		   



        // -------- Control-specific events --------- // 

        private void StageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StageCombo.SelectedItem != null && pageMode != PageFunctions.View)
            {
                int newStage = currentTimeline.StageNumber;
                showStageChange(newStage);
            }
        }
        
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int newStage = currentTimeline.StageNumber + 1;
                updateProjectStage(newStage);                
            }
            catch (Exception generalException) { MessageFunctions.Error("Error moving to the next stage", generalException); }
        }

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

        }



    } // class
} // namespace
