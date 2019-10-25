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
    /// Interaction logic for ActionsPage.xaml
    /// </summary>
    public partial class ActionsPage : Page
    {
        // ---------------------------------------------------------- //
        // -------------------- Global Variables -------------------- //
        // ---------------------------------------------------------- //

        // --------- Global/page parameters --------- // 

        string pageMode;
        const string defaultInstructions = "The top filters refer to the project, the bottom ones to the action and its owner.";
        const string defaultHeader = "Project Actions";
        bool projectSelected = false;
        bool clientSelected = false;

        // ------------ Current variables ----------- // 



        // ------------- Current records ------------ //



        // ------------------ Lists ----------------- //



        // ---------------------------------------------------------- //
        // -------------------- Page Management --------------------- //
        // ---------------------------------------------------------- //

        // ---------- Initialize and Load ----------- //

        public ActionsPage()
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


            refreshClientCombo();
            refreshStatusCombo();

            Instructions.Content = defaultInstructions;


        }



        // ---------------------------------------------------------- //
        // -------------------- Data Management --------------------- //
        // ---------------------------------------------------------- //  

        // ------------- Data retrieval ------------- // 		


        private void refreshActionsGrid()
        {
            




        }

        private void refreshClientCombo()
        {
            try
            {
                ClientProxy currentRecord = (Globals.SelectedClientProxy != null) ? Globals.SelectedClientProxy : Globals.DefaultClientProxy;
                ProjectFunctions.SetClientFilterList();
                ClientCombo.ItemsSource = ProjectFunctions.ClientFilterList;
                if (ProjectFunctions.ClientFilterList.Exists(ccl => ccl.ID == currentRecord.ID))
                {
                    ClientCombo.SelectedItem = ProjectFunctions.ClientFilterList.First(ccl => ccl.ID == currentRecord.ID);
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating client drop-down list", generalException); }
        }

        private void refreshStatusCombo()
        {
            try
            {
                Globals.ProjectStatusFilter currentFilter = Globals.SelectedStatusFilter;
                string currentName = ProjectFunctions.StatusFilterName(currentFilter);
                if (ProjectFunctions.StatusFilterList == null) { ProjectFunctions.SetProjectStatusFilter(); }
                StatusCombo.ItemsSource = ProjectFunctions.StatusFilterList;
                StatusCombo.SelectedItem = currentName;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating status drop-down list", generalException); }
        }

        private void refreshProjectCombo()
        {
            try
            {
                int clientID = (Globals.SelectedClientProxy != null)? Globals.SelectedClientProxy.ID : 0;
                ProjectProxy currentRecord = (Globals.SelectedProjectProxy != null) ? Globals.SelectedProjectProxy : Globals.DefaultProjectProxy;
                ProjectFunctions.SetProjectFilterList(Globals.SelectedStatusFilter, false, clientID);
                ProjectCombo.ItemsSource = ProjectFunctions.ProjectFilterList;
                selectProject(currentRecord.ProjectID);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating projects drop-down list", generalException); }
        }

        // -------------- Data updates -------------- // 



        // --------- Other/shared functions --------- // 

        private void selectProject(int projectID)
        {
            try
            {
                if (ProjectFunctions.ProjectFilterList.Exists(pfl => pfl.ProjectID == projectID))
                {
                    ProjectCombo.SelectedItem = ProjectFunctions.ProjectFilterList.First(pfl => pfl.ProjectID == projectID);
                }
                else ProjectCombo.SelectedItem = ProjectFunctions.ProjectFilterList.First(pfl => pfl.ProjectID == 0);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error selecting current project in the list", generalException); }
        }

        private void setCurrentClient(Clients client, ClientProxy clientProxy = null)
        {
            try
            {
                if (client == null && (clientProxy == null || clientProxy.ID <= 0))
                {
                    Globals.SelectedClientProxy = null;
                    
                    //if (!projectSelected) { MessageFunctions.CancelInfoAlert(); }
                    clientSelected = false;
                }
                else
                {
                    if (clientProxy == null) { clientProxy = ClientFunctions.GetClientProxy(client.ID); }
                    Globals.SelectedClientProxy = clientProxy;                    
                    //if (!projectSelected)
                    //{
                    //    MessageFunctions.InfoAlert("This effectively sets the current client to " + clientProxy.ClientName + " until the name filter is changed/cleared "
                    //        + " or a different project is selected (the projects drop-down list is unaffected)", "Client " + clientProxy.ClientCode + " selected");
                    //}
                    clientSelected = true;
                }
                togglePageHeader();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing project client selection", generalException); }
        }

        private void toggleProjectMode(bool specificProject)
        {
            projectSelected = 
                //ProjectButton.IsEnabled = 
                specificProject;
            //toggleEditButtons(projectSelected);
            //ProjectButtonText.Text = (!projectSelected) ? "Set Project" : "All Projects";
            //toggleProjectSearchButton();
            //toggleProjectColumns();
            togglePageHeader();
        }

        private void togglePageHeader()
        {
            if (projectSelected)
            {
                PageHeader.Content = defaultHeader + " for Project " + Globals.SelectedProjectProxy.ProjectCode + " (" + Globals.SelectedProjectProxy.ProjectName + ")";
            }
            else if (clientSelected)
            {
                PageHeader.Content = defaultHeader + " for Client " + Globals.SelectedClientProxy.ClientCode + " (" + Globals.SelectedClientProxy.ClientName + ")";
            }
            else { PageHeader.Content = defaultHeader; }
        }

        // ---------- Links to other pages ---------- //		



        // ---------------------------------------------------------- //
        // -------------------- Event Management -------------------- //
        // ---------------------------------------------------------- //  

        // ---- Generic (shared) control events ----- // 		   



        // -------- Control-specific events --------- // 





        private void ClientCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ClientCombo.SelectedItem == null) { } // Won't be for long
                else
                {
                    //Globals.SelectedClientProxy = (ClientProxy)ClientCombo.SelectedItem;
                    setCurrentClient(null, (ClientProxy)ClientCombo.SelectedItem ?? null);
                    //refreshActionsGrid();
                    refreshProjectCombo();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing client selection", generalException); }	
        }

        private void StatusCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (StatusCombo.SelectedItem != null)
                {
                    string selection = StatusCombo.SelectedItem.ToString();
                    selection = selection.Replace(" ", "");
                    Globals.SelectedStatusFilter = (Globals.ProjectStatusFilter)Enum.Parse(typeof(Globals.ProjectStatusFilter), selection);
                    //refreshActionsGrid();
                    refreshProjectCombo();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing status filter selection", generalException); }	
        }

        private void ProjectCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectCombo.SelectedItem == null) { } // Do nothing - won't be for long             
            else
            {
                try
                {
                    Globals.SelectedProjectProxy = (ProjectProxy)ProjectCombo.SelectedItem;                  
                    refreshActionsGrid();
                    toggleProjectMode(Globals.SelectedProjectProxy != Globals.AllProjects);
                }
                catch (Exception generalException) { MessageFunctions.Error("Error processing project selection", generalException); }
            }
        }

        private void FromDate_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void ToDate_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void NameLike_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void NameLike_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void NameLike_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void PossibleNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void CompleteCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ActionDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // To do: check for changes if appropriate

            PageFunctions.ShowTilesPage();
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {

        }


    } // class
} // namespace
