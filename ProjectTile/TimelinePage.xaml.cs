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
        bool stagesLoaded = false;
        bool viewOnly = false;

        // ------------ Current variables ----------- // 

        TimelineProxy currentTimeline = new TimelineProxy();
        //Globals.TimelineType currentType = Globals.TimelineType.Effective;
        bool stageChanged = false;
        int currentStageNumber = 0;
        int focusStage = 0;

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
            if (pageMode == PageFunctions.View)
            {
                viewOnly = true;
                CommitButton.Visibility = NextButton.Visibility = Visibility.Hidden;
                StageCombo.IsEnabled = false;
            }

            PageHeader.Content = "Project Timeline for Project " + Globals.SelectedProjectProxy.ProjectCode;
            createControlArrays();
            refreshStageCombo();
            setTimelineType(Globals.SelectedTimelineType);
            refreshTimeData();
            MessageFunctions.InfoAlert("Effective date is the actual date for previous stages, and the target date for future ones. If targets are out of date, this can mean that "
                + "future stages show an earlier (target) date than historic ones.", "Please note");
        }

        // ---------------------------------------------------------- //
        // -------------------- Data Management --------------------- //
        // ---------------------------------------------------------- //  

        // ------------- Data retrieval ------------- // 		

        private void refreshTimeData()
        {
            try
            {
                currentTimeline = ProjectFunctions.GetProjectTimeline(Globals.SelectedProjectProxy.ProjectID, Globals.SelectedTimelineType);
                this.DataContext = currentTimeline;
                currentStageNumber = currentTimeline.StageNumber;
                displaySelectedStage();
                if (Globals.SelectedHistory != null) { focusStage = ProjectFunctions.GetStageNumber(Globals.SelectedHistory.StageID); }
                formatDatePickers();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error displaying project status history timeline", generalException); }	
        }

        private void refreshStageCombo()
        {
            ProjectFunctions.SetFullStageList();
            StageCombo.ItemsSource = ProjectFunctions.FullStageList;
            if (!stagesLoaded) { stagesLoaded = true; }
        }

        private void changeTimelineType(Globals.TimelineType type)
        {
            try
            {
                if (ProjectFunctions.FullStageList == null || ProjectFunctions.FullStageList.Count() == 0) { return; } // Too early
                else if (type == Globals.SelectedTimelineType) { return; } // Cancelling the earlier change
                else if (checkForChanges())
                {
                    Globals.SelectedTimelineType = type;
                    clearChanges();
                    refreshTimeData();
                }
                else { setTimelineType(Globals.SelectedTimelineType); }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error changing timeline type", generalException); }	
        }

        private bool checkForChanges()
        {
            bool changesExist = (stageChanged || ProjectFunctions.StageDatesChanged.Count > 0);
            if (!changesExist) { return true; }
            bool isOK = MessageFunctions.WarningYesNo("This will undo all unsaved changes, including changes to the project's current stage. Is this correct?", "Override changes?");
            return isOK;
        }

        private void setTimelineType(Globals.TimelineType type)
        {
            switch (type)
            {
                case Globals.TimelineType.Actual: ActualRadio.IsChecked = true; break;
                case Globals.TimelineType.Effective: EffectiveRadio.IsChecked = true; break;
                case Globals.TimelineType.Target: TargetRadio.IsChecked = true; break;
                default: EffectiveRadio.IsChecked = true; break;
            }
        }

        // -------------- Data updates -------------- // 

        private void updateProjectStage(int newStageNumber, bool alreadyUpdated)
        {
            try
            {
                if (newStageNumber != currentStageNumber) // Allows looping round again if undoing the change
                {
                    bool confirm = MessageFunctions.ConfirmOKCancel("This will update the start dates of any future-dated stages that are now complete or in progress. Are you sure? "
                        + "If the results are not as expected, use the 'Back' or 'Close' button afterwards to undo all changes.");
                    if (!confirm)
                    {
                        if (alreadyUpdated) { currentTimeline.Stage = ProjectFunctions.GetStageByNumber(currentStageNumber); }
                        return;
                    }
                    currentStageNumber = newStageNumber;
                    if (!alreadyUpdated)
                    {
                        currentTimeline.Stage = ProjectFunctions.GetStageByNumber(newStageNumber);
                        displaySelectedStage();
                    }
                    stageChanged = true;
                    updateAffectedDates();
                    formatDatePickers();
                    NextButton.IsEnabled = (!ProjectFunctions.IsLastStage(newStageNumber));
                    CommitButton.IsEnabled = true;                    
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error updating current project stage", generalException); }	     
        }

        private void updateAffectedDates()
        {
            try
            {
                if (Globals.SelectedTimelineType == Globals.TimelineType.Target) { return; }
                for (int i = 0; i < ProjectFunctions.MaxNonCancelledStage(); i++) { updateDate(i); }
                updateDate(Globals.CancelledStage);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error updating affected project dates", generalException); }
        }

        private void updateDate(int i)
        {
            try
            {
                int position = (i == Globals.CancelledStage) ? 99 : i; 
                
                DateTime thisDate = (currentTimeline.DateHash[i] == null) ? Globals.InfiniteDate : (DateTime)currentTimeline.DateHash[i];
                if (i < currentTimeline.StageNumber && thisDate > Globals.Today) 
                { 
                    datePickers[position].SelectedDate = null;
                    ProjectFunctions.QueueDateChange(i);
                }
                else if (i == currentTimeline.StageNumber && !thisDate.Equals(Globals.Today)) 
                { 
                    datePickers[position].SelectedDate = Globals.Today;
                    ProjectFunctions.QueueDateChange(i);
                }
                else if (i > currentTimeline.StageNumber && thisDate <= Globals.Today) 
                {                    
                    datePickers[position].SelectedDate = (Globals.SelectedTimelineType == Globals.TimelineType.Effective)? 
                        ProjectFunctions.GetStageStartDate(Globals.SelectedProjectProxy.ProjectID, i, true) :
                        datePickers[position].SelectedDate = null;                     
                    ProjectFunctions.QueueDateChange(i);
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error updating project date for stage number " + i.ToString(), generalException); }
        }        

        // --------- Other/shared functions --------- // 

        private void displaySelectedStage()
        {
            try
            {
                if (currentTimeline == null || ProjectFunctions.FullStageList == null || !stagesLoaded ) { return; }
                ProjectStages selectedStage = ProjectFunctions.GetStageByID(currentTimeline.StageID); // Gets it from FullStageList, so it is picked up
                if (selectedStage != null)
                {
                    int selectedIndex = ProjectFunctions.FullStageList.IndexOf(selectedStage);
                    if (selectedIndex >= 0) { StageCombo.SelectedIndex = ProjectFunctions.FullStageList.IndexOf(selectedStage); }
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error selecting current project stage", generalException); }
        }

        private void closePage(bool closeAll, bool checkFirst)
        {
            if (checkFirst && pageMode != PageFunctions.View)
            {
                bool doClose = checkForChanges();
                if (!doClose) { return; }
            }
            clearChanges();
            MessageFunctions.CancelInfoAlert();
            bool closeFully = closeAll ? true : !fromProjectPage;
            if (closeFully) { ProjectFunctions.ReturnToTilesPage(); }
            else { ProjectFunctions.ReturnToSourcePage(pageMode); }
        }

        public void clearChanges()
        {
            stageChanged = false;            
            ProjectFunctions.ClearHistoryChanges();
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

        private void formatDatePickers()
        {
            try
            {
                if (datePickers.Count <= 0) { return;  }
                int stageNumber = currentTimeline.StageNumber;
                bool actual = (Globals.SelectedTimelineType == Globals.TimelineType.Actual);
                bool effective = (Globals.SelectedTimelineType == Globals.TimelineType.Effective);
                bool target = (Globals.SelectedTimelineType == Globals.TimelineType.Target);
                int cancelledPosition = datePickers.Count - 1;                

                for (int i = 0; i < cancelledPosition; i++)
                {
                    dateLabels[i].FontWeight = datePickers[i].FontWeight = (actual || (effective && i <= stageNumber)) ? FontWeights.Bold : FontWeights.Normal;
                    dateLabels[i].FontStyle = datePickers[i].FontStyle = (actual || (effective && i <= stageNumber)) ? FontStyles.Normal : FontStyles.Italic;
                    datePickers[i].IsEnabled = (!viewOnly && (actual && i <= stageNumber || effective || target && i > stageNumber));                    
                }
                dateLabels[cancelledPosition].FontWeight = datePickers[cancelledPosition].FontWeight = (actual || (effective && stageNumber == Globals.CancelledStage)) ?
                    FontWeights.Bold : FontWeights.Normal;
                dateLabels[cancelledPosition].FontStyle = datePickers[cancelledPosition].FontStyle = (actual || (effective && stageNumber == Globals.CancelledStage)) ?
                    FontStyles.Normal : FontStyles.Italic;
                datePickers[cancelledPosition].IsEnabled = (!viewOnly && stageNumber == Globals.CancelledStage);

                if (focusStage == Globals.CancelledStage) { datePickers[cancelledPosition].Focus(); }
                else { datePickers[focusStage].Focus(); }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error highlighing date statuses", generalException); }	
        }

        // ---------- Links to other pages ---------- //		



        // ---------------------------------------------------------- //
        // -------------------- Event Management -------------------- //
        // ---------------------------------------------------------- //  

        // ---- Generic (shared) control events ----- // 		   

        private void CheckDate(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(sender is DatePicker)) { return; }
                DatePicker thisPicker = (DatePicker)sender;
                int? position = datePickers.IndexOf(thisPicker);
                if (position == null)
                {
                    MessageFunctions.Error("Error processing date change: could not find the relevant date picker in the list", null);
                    return;
                }
                int stageNumber = (position == datePickers.Count - 1) ? 99 : (int)position;
                DateTime? newDate = (DateTime?)thisPicker.SelectedDate;
                DateTime? oldDate = (DateTime?)currentTimeline.InitialDates[stageNumber];

                if (newDate == (DateTime?)oldDate) { return; } // No change
                bool target = (Globals.SelectedTimelineType == Globals.TimelineType.Target 
                    || (Globals.SelectedTimelineType == Globals.TimelineType.Effective && stageNumber > currentTimeline.StageNumber));
                bool dateOK = ProjectFunctions.ProcessDateChange(stageNumber, currentTimeline.StageNumber, newDate, target);
                if (dateOK) 
                { 
                    currentTimeline.InitialDates[stageNumber] = newDate; // Update for future comparisons
                    CommitButton.IsEnabled = true;
                    Globals.SelectedHistory = ProjectFunctions.GetHistoryRecord(Globals.SelectedProjectProxy.ProjectID, stageNumber);
                } 
                else { thisPicker.SelectedDate = oldDate; } // Undo change
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing date change", generalException); }	
        }

        // -------- Control-specific events --------- // 

        private void StageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (StageCombo.SelectedItem != null && currentTimeline != null && pageMode != PageFunctions.View)
                {
                    int newStage = currentTimeline.StageNumber;
                    updateProjectStage(newStage, true);
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error handling stage selection", generalException); }	
        }
        
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int newStage = currentTimeline.StageNumber + 1;
                updateProjectStage(newStage, false);                
            }
            catch (Exception generalException) { MessageFunctions.Error("Error moving to the next stage", generalException); }
        }

        private void TargetRadio_Checked(object sender, RoutedEventArgs e)
        {
            changeTimelineType(Globals.TimelineType.Target);
        }

        private void ActualRadio_Checked(object sender, RoutedEventArgs e)
        {
            changeTimelineType(Globals.TimelineType.Actual);
        }

        private void EffectiveRadio_Checked(object sender, RoutedEventArgs e)
        {
            changeTimelineType(Globals.TimelineType.Effective);
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
            Globals.SelectedProjectProxy.Stage = ProjectFunctions.GetStageByNumber(currentStageNumber);
            bool success = ProjectFunctions.UpdateHistory(currentTimeline, stageChanged);
            if (success) { clearChanges(); }
            else if (stageChanged) { Globals.SelectedProjectProxy.Stage = ProjectFunctions.ProjectCurrentStage(Globals.SelectedProjectProxy.ProjectID); }
        }

    } // class
} // namespace
