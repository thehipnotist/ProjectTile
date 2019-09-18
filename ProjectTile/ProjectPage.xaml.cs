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
    /// Interaction logic for ProjectPage.xaml
    /// </summary>
    public partial class ProjectPage : Page
    {
        // ---------------------- //
        // -- Global Variables -- //
        // ---------------------- //   

        // Global/page parameters //
        string pageMode;

        // Current variables //
        bool openOnly = false;        

        // Current records //


        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //

        // Initialize and Load //
        public ProjectPage()
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
                PageFunctions.ShowTilesPage(); // To do: replace with a 'back' method that resets everything
            }

            refreshMainProjectGrid();
        }



        // ---------------------- //
        // -- Data Management --- //
        // ---------------------- //

        // Data updates //
        private void refreshMainProjectGrid()
        {
            try
            {
                bool success = ProjectFunctions.SetProjectGridList(EntityFunctions.CurrentEntityID, openOnly, ProjectFunctions.SelectedClientID, ProjectFunctions.SelectedPMStaffID);
                if (success)
                {
                    ProjectDataGrid.ItemsSource = ProjectFunctions.ProjectGridList;
                }
                else
                {
                    // What happens if it doesn't work? Already throwing an error...
                }

            }
            catch (Exception generalException) { MessageFunctions.Error("Error opulating project grid data", generalException); }	
        }

        // Data retrieval //

        // Other/shared functions //


        // ---------------------- //
        // -- Event Management -- //
        // ---------------------- //

        // Generic (shared) control events //

        // Control-specific events //





        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // To do: check for changes if appropriate

            PageFunctions.ShowTilesPage();
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ProjectDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }



    } // class
} // namespace
