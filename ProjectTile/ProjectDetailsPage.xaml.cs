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
    public partial class ProjectDetailsPage : Page //, INotifyPropertyChanged
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
            else
            {
                if (!Globals.MyPermissions.Allow("ActivateProjects"))
                {
                    StageCombo.IsEnabled = false;
                    StageCombo.ToolTip = "Your current permissions do not allow updating the project stage";
                }

                try
                {
                    ProjectFunctions.SetFullProjectTypeList();
                    TypeCombo.ItemsSource = ProjectFunctions.FullProjectTypeList;
                }
                catch (Exception generalException) { MessageFunctions.Error("Error creating the list of available project types", generalException); }


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
                CommitButton.Visibility = Visibility.Hidden;
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
                }
                catch (Exception generalException) { MessageFunctions.Error("Error selecting current project type", generalException); }
            }
        }

        private void setUpNewMode()
        {
            thisProjectSummary = new ProjectSummaryRecord();
            PageHeader.Content = "Create New Project";
            Instructions.Content = "Fill in the details as required and then click 'Save' to create the record.";
            if (fromProjectPage)
            {
                // To do: set up certain fields as with an amendment based on current selection - combine those parts into a single function?
            }
            else { BackButton.Visibility = Visibility.Hidden; }       
        }

        private void setUpAmendMode()
        {
            try
            {
                thisProjectSummary = Globals.SelectedProjectSummary;
                ProjectTypes selectedType = ProjectFunctions.FullProjectTypeList.FirstOrDefault(tl => tl.TypeCode == thisProjectSummary.ProjectType.TypeCode);
                TypeCombo.SelectedIndex = ProjectFunctions.FullProjectTypeList.IndexOf(selectedType);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error selecting current project type", generalException); }
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

//            try
//            {
//                if (TypeCombo.SelectedItem != null)
//                {
//                }
//            }
//            catch (Exception generalException) { MessageFunctions.Error("Error processing project type selection", generalException); }
        }

        private void ClientCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // To do - validation from above
        }
 
    }
}
