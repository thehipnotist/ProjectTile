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

            refreshClientCombo();
            //refreshMainProjectGrid(); // Can probably remove later
        }



        // ---------------------- //
        // -- Data Management --- //
        // ---------------------- //

        // Data updates //
        private void refreshMainProjectGrid()
        {
            try
            {
                int clientID = (ProjectFunctions.SelectedClientSummary != null)? ProjectFunctions.SelectedClientSummary.ID : 0;
                int managerID = (ProjectFunctions.SelectedPMSummary != null)? ProjectFunctions.SelectedPMSummary.ID : 0;
                
                bool success = ProjectFunctions.SetProjectGridList(ProjectFunctions.StatusFilter.All, clientID, managerID);
                if (success)
                {
                    // To do: functionality to retain current or selected project and (re)select it
                    ProjectDataGrid.ItemsSource = ProjectFunctions.ProjectGridList;
                }
                else
                {
                    // What happens if it doesn't work? Already throwing an error...
                }

            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating project grid data", generalException); }	
        }

        private void refreshClientCombo()
        {
            try
            {
                ClientGridRecord currentRecord = (ProjectFunctions.SelectedClientSummary != null)? ProjectFunctions.SelectedClientSummary: null;
                if (currentRecord == null && ClientCombo.SelectedItem != null) { currentRecord = (ClientGridRecord) ClientCombo.SelectedItem; }
                
                ProjectFunctions.SetClientComboList();
                ClientCombo.ItemsSource = ProjectFunctions.ClientComboList;

                ClientCombo.SelectedItem = (currentRecord != null) ? currentRecord : ProjectFunctions.DefaultClientSummary;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating client combo list", generalException); }	
        }

        private void refreshPMCombo()
        {
            ProjectFunctions.SetPMComboList();
            // PMCombo.ItemsSource = ProjectFunctions.PMComboList;
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
            // Handle null selections
        }

        private void ClientCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ClientCombo.SelectedItem == null) { } //ProjectFunctions.SelectedClientSummary = ProjectFunctions.DefaultClientSummary; }
                else
                {
                    ProjectFunctions.SelectedClientSummary = (ClientGridRecord)ClientCombo.SelectedItem;
                    refreshMainProjectGrid();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing client combination selection", generalException); }	
        }



    } // class
} // namespace
