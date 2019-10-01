using System;
using System.Windows;
using System.Collections;
using System.Windows.Controls;

namespace ProjectTile
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //       

        // Initialize and Load //
        public MainWindow()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Window));
            ToggleMainMenus(false);
            ToggleSideButtons(false);
        }

        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                PageFunctions.ShowLoginPage(PageFunctions.LogIn);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error loading page", generalException); }
        }

        // Menu settings //
        public void MenuSecurity(ref TableSecurity myPermissions) // NB: the reference is needed or an error is thrown
        {
            try
            {                
                NewEntity.Visibility = myPermissions.ShowOrCollapse("AddEntities");
                AmendEntity.Visibility = myPermissions.ShowOrCollapse("EditEntities");

                ViewStaff.Visibility = myPermissions.ShowOrCollapse("ViewStaff");
                NewStaff.Visibility = myPermissions.ShowOrCollapse("AddStaff");
                AmendStaff.Visibility = myPermissions.ShowOrCollapse("EditStaff");
                StaffEntities.Visibility = myPermissions.ShowOrCollapse("ViewStaffEntities");

                ViewProduct.Visibility = myPermissions.ShowOrCollapse("ViewProducts");
                NewProduct.Visibility = myPermissions.ShowOrCollapse("AddProducts");
                AmendProduct.Visibility = myPermissions.ShowOrCollapse("EditProducts");

                ViewClient.Visibility = myPermissions.ShowOrCollapse("ViewClients");
                NewClient.Visibility = myPermissions.ShowOrCollapse("AddClients");
                AmendClient.Visibility = myPermissions.ShowOrCollapse("EditClients");
                ClientContact.Visibility = myPermissions.ShowOrCollapse("ViewClientStaff");
                ClientProduct.Visibility = myPermissions.ShowOrCollapse("ViewClientProducts");

                ViewProject.Visibility = myPermissions.ShowOrCollapse("ViewProjects");
                NewProject.Visibility = myPermissions.ShowOrCollapse("AddProjects");
                AmendProject.Visibility = myPermissions.ShowOrCollapse("EditProjects");
                ProjectStaff.Visibility = myPermissions.ShowOrCollapse("ViewProjectTeams");
                ProjectContact.Visibility = myPermissions.ShowOrCollapse("ViewClientTeams");
                ProjectProduct.Visibility = myPermissions.ShowOrCollapse("ViewProjectProducts");

                // More to come...

                //ToggleMenuItem(StaffMenu);
                StaffMenu.IsEnabled = subMenuItemsVisible(StaffMenu);
                ProductMenu.IsEnabled = subMenuItemsVisible(ProductMenu);
                ClientMenu.IsEnabled = subMenuItemsVisible(ClientMenu);
                ProjectMenu.IsEnabled = subMenuItemsVisible(ProjectMenu);

            }
            catch (Exception generalException) { MessageFunctions.Error("Error setting menu permissions", generalException); }
        }
        
        private bool subMenuItemsVisible(MenuItem smi)
        {
            Visibility vi = Visibility.Visible;            
            foreach (MenuItem ssmi in smi.Items) 
            {
                if (ssmi.Visibility == vi) { return true; }
            }
            return false;
        }
        
        public void ToggleMainMenus(bool Show)
        {
            if (Show)
            {
                MainMenu.Visibility = LoginMenu.Visibility = Visibility.Visible;
            }
            else
            {
                MainMenu.Visibility = LoginMenu.Visibility = Visibility.Hidden;
            }
        }

        public void ToggleSideButtons(bool Show)
        {
            if (Show)
            {
                ProjectButton.Visibility = Visibility.Visible;
            }
            else
            {
                ProjectButton.Visibility = Visibility.Hidden;
            }
        }

        // ---------------------- //
        // -- Data Management --- //
        // ---------------------- //   

        // Data updates //
        public void UpdateDetailsBlock()
        {
            try
            {
                DetailsBlock.Text =
                    "UserID: " + Globals.MyUserID + "\n"
                    + "Name: " + Globals.MayName + "\n"
                    + "\n"
                    + "Entity: " + Globals.CurrentEntityName + "\n"
                    + "Default: " + Globals.MyDefaultEntityName;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error refreshing current details", generalException);
            }
        }

        // Other/shared functions //
        public bool ConfirmClosure()
        {
            try
            {
                string thisPage = PageFunctions.ThisPageName();
                string question = "Are you sure you want to exit?";
                if (thisPage != Globals.TilesPageName && thisPage != "LoginPage") 
                { 
                    question = question + " Any unsaved changes you have made would be lost.";
                    return MessageFunctions.WarningYesNo(question, "Close ProjectTile Application?");
                }
                else
                {
                    return MessageFunctions.ConfirmOKCancel(question, "Close ProjectTile Application?");
                }
            }
            catch 
            { 
                // Do nothing as it's not worth raising an error when the user wants to leave
                return true;
            }            
        }
        
        // ---------------------- //
        // -- Event Management -- //
        // ---------------------- //  

        // Control-specific events //
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!ConfirmClosure()) { e.Cancel = true; } // Cancel closure if not sure
        }

        private void Main_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double targetWidth = Main.ActualWidth - 15;
            double targetHeight = Main.ActualHeight - 69;

            MainFrame.Width = Math.Max(targetWidth, MainFrame.MinWidth);
            MainFrame.Height = Math.Max(targetHeight, MainFrame.MinHeight);
        }

        private void ChangeEntity_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowEntityPage(PageFunctions.Switch);
        }

        private void NewEntity_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowEntityPage(PageFunctions.New);
        }

        private void AmendEntity_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowEntityPage(PageFunctions.Amend);
        }

        private void DefaultEntity_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowEntityPage(PageFunctions.Default);
        }

        private void LoginMenu_Login_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowLoginPage(PageFunctions.LogIn);
        }

        private void LoginMenu_Password_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowLoginPage(PageFunctions.PassChange);
        }

        private void LoginMenu_Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ViewStaff_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowStaffPage(PageFunctions.View);
        }

        private void AmendStaff_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowStaffPage(PageFunctions.Amend);
        }

        private void NewStaff_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowStaffDetailsPage(PageFunctions.New, 0);
        }

        private void StaffEntities_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowStaffEntitiesPage();
        }

        private void ViewProduct_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProductPage(PageFunctions.View);
        }

        private void NewProduct_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProductPage(PageFunctions.New);
        }

        private void AmendProduct_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProductPage(PageFunctions.Amend);
        }

        private void ViewClient_Click(object sender, RoutedEventArgs e)
        {
            Globals.ResetClientParameters();
            PageFunctions.ShowClientPage(PageFunctions.View);
        }

        private void NewClient_Click(object sender, RoutedEventArgs e)
        {
            Globals.ResetClientParameters();
            PageFunctions.ShowClientPage(PageFunctions.New);
        }

        private void AmendClient_Click(object sender, RoutedEventArgs e)
        {
            Globals.ResetClientParameters();
            PageFunctions.ShowClientPage(PageFunctions.Amend);
        }

        private void ClientContact_Click(object sender, RoutedEventArgs e)
        {
            Globals.ResetClientParameters();
            PageFunctions.ShowClientContactPage();
        }

        private void ClientProduct_Click(object sender, RoutedEventArgs e)
        {
            Globals.ResetClientParameters();
            PageFunctions.ShowClientProductsPage();
        }

        private void HelpMenu_About_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowHelpPage(PageFunctions.About);
        }

        private void ExitMenu_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ViewProject_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProjectPage(PageFunctions.View);
        }

        private void NewProject_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProjectDetailsPage(PageFunctions.New);
        }

        private void AmendProject_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProjectPage(PageFunctions.Amend);
        }

        private void ProjectStaff_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProjectTeamsPage();
        }

        private void ProjectContact_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ProjectProduct_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ProjectButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProjectTeamsPage(pageMode: "", selectedStaffID: Globals.MyStaffID);
        }


    } // class
} // namespace
