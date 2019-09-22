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
//        public event PropertyChangedEventHandler PropertyChanged;

        // Current variables //

        // Current records //
        private ProjectSummaryRecord thisProjectSummary = null;
        public ProjectSummaryRecord ThisProjectSummary
        {
            get { return thisProjectSummary; }
            set 
            { 
                thisProjectSummary = ThisProjectSummary;
                //OnPropertyChanged("ThisProjectSummary");
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


//            ProjectFunctions.SetTypeNameList();
//            TypeCombo.ItemsSource = ProjectFunctions.TypeNameList;



//            this.DataContext = ThisProject;




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
                ProjectFunctions.ReturnToTilesPage();
            }
            
            
            if (pageMode==PageFunctions.View)
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
                    ProjectFunctions.ReturnToTilesPage();
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
                catch (Exception generalException) { MessageFunctions.Error("Error displaying project type name", generalException); }

                if (pageMode == PageFunctions.Amend && Globals.SelectedProjectSummary != null)  // Just to be sure
                {
                    try
                    {
                        thisProjectSummary = Globals.SelectedProjectSummary;
                        ProjectTypes selectedType = ProjectFunctions.FullProjectTypeList.FirstOrDefault(tl => tl.TypeCode == ThisProjectSummary.ProjectType.TypeCode);
                        TypeCombo.SelectedIndex = ProjectFunctions.FullProjectTypeList.IndexOf(selectedType);
                    }
                    catch (Exception generalException) { MessageFunctions.Error("Error selecting current project type", generalException); }
                }
            }
            
            this.DataContext = ThisProjectSummary;




//            ThisProjectSummary.ProjectName = "Testy test";
            //OnPropertyChanged("ProjectName");

            ClientCombo.ItemsSource = ProjectFunctions.ClientComboList;
            
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
//        protected void OnPropertyChanged(string eventName)
 //       {
  //          try
   //         {
    //            MessageBox.Show("Hello");
     //           PropertyChangedEventHandler thisHandler = PropertyChanged;
      ///          if (thisHandler != null)
        //        {
         //           thisHandler(this, new PropertyChangedEventArgs(eventName));
          //      }
           // }
            //catch (Exception generalException) { MessageFunctions.Error("Error handling changed property", generalException); }
        //}

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
            MessageBox.Show(ThisProjectSummary.ProjectType.TypeCode.ToString() + " " + ThisProjectSummary.ProjectType.TypeName);
        }

        private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
//            try
//            {
//                if (TypeCombo.SelectedItem != null)
//                {
//                    //int selectedIndex = TypeCombo.SelectedIndex;
//                    //ProjectTypes selectedType = (ProjectTypes)TypeCombo.SelectedItem;
//                    //ThisProjectSummary.TypeDescription = selectedType.TypeDescription;
//                }
//            }
//            catch (Exception generalException) { MessageFunctions.Error("Error processing project type selection", generalException); }
        }
 
    }
}
