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
    public partial class ProjectDetailsPage : Page, INotifyPropertyChanged
    {
        // ---------------------- //
        // -- Global Variables -- //
        // ---------------------- //

        // Global/page parameters //

        MainWindow winMain = (MainWindow)App.Current.MainWindow;
        string pageMode;
        public event PropertyChangedEventHandler PropertyChanged;

        // Current variables //

        // Current records //
        private ProjectSummaryRecord thisProjectSummary = null;
        public ProjectSummaryRecord ThisProjectSummary
        {
            get { return thisProjectSummary; }
            set 
            { 
                thisProjectSummary = ThisProjectSummary;
            }
        }

        // Data context //
        //private ObservableCollection<ProjectSummaryRecord> thisProject = new ObservableCollection<ProjectSummaryRecord>();
        //public ObservableCollection<ProjectSummaryRecord> ThisProject
        //{
        //    get { return thisProject; }
        //    set 
        //    {
        //        MessageBox.Show("Hi");
        //        thisProject = value;
        //        OnPropertyChanged("ThisProject");
        //    }
        //}   

        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //

        // Initialize and Load //
        public ProjectDetailsPage()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Page));
            KeepAlive = false;


            if (Globals.SelectedProjectSummary != null) { thisProjectSummary = Globals.SelectedProjectSummary; }
//            ThisProject.Add(thisProjectSummary);
          this.DataContext = ThisProjectSummary;
//            this.DataContext = ThisProject;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
//            if (Globals.SelectedProjectSummary != null) { ThisProjectSummary = Globals.SelectedProjectSummary; }
            ThisProjectSummary.ProjectName = "Testy test";
            OnPropertyChanged("ThisProjectSummary.ProjectName");
            
            try
            {
                pageMode = PageFunctions.pageParameter(this, "Mode");
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                ProjectFunctions.ReturnToProjectPage();
            }

            if (pageMode == PageFunctions.New)
            {
                PageHeader.Content = "Create New Project";
                Instructions.Content = "Fill in the details as required and then click 'Save' to create the record.";
                BackButton.Visibility = Visibility.Hidden;          
            }            
            else 
            {


//                if (Globals.SelectedProjectSummary != null)
                {
                    //ProjectFunctions.setThisProject();                   
                    
//                }
//                else
//                {
 //                   MessageFunctions.Error("Load error: no project loaded.", null);
 //                   ProjectFunctions.ReturnToProjectPage();
                }

                if (pageMode == PageFunctions.View) { } // disable controls and submit button

            }
        }

        // ---------------------- //
        // -- Data Management --- //
        // ---------------------- //

        // Data updates //


        // Shared functions //
        protected void OnPropertyChanged(string eventName)
        {
            try
            {
                MessageBox.Show("Hello");
                PropertyChangedEventHandler thisHandler = PropertyChanged;
                if (thisHandler != null)
                {
                    thisHandler(this, new PropertyChangedEventArgs(eventName));
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error handling changed property", generalException); }
        }

        // ---------------------- //
        // -- Event Management -- //
        // ---------------------- //

        // Control-specific events //

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectFunctions.ReturnToTilesPage();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {            
            ProjectFunctions.ReturnToProjectPage();
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(ThisProjectSummary.ProjectName);
        }
 
    }
}
