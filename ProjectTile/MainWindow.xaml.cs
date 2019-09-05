using System;
using System.Windows;
using System.Collections;

namespace ProjectTile
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /* ----------------------
           -- Page Management ---
           ---------------------- */       

        /* Initialize and Load */
        public MainWindow()
        {
            InitializeComponent();
            MainMenu.Visibility = Visibility.Hidden;
        }

        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                PageFunctions.ShowLoginPage("LogIn");
            }
            catch (Exception generalException) { MessageFunctions.Error("Error loading page", generalException); }
        }

        /* Menu settings */
        public void MenuSecurity(ref TableSecurity myPermissions)
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

                // More to come...
            }
            catch (Exception generalException) { MessageFunctions.Error("Error setting menu permissions", generalException); }
        }
        
        public void toggleMainMenu(bool Show)
        {
            if (Show)
            {
                MainMenu.Visibility = Visibility.Visible;
            }
            else
            {
                MainMenu.Visibility = Visibility.Hidden;
            }
        }

        /* ----------------------
           -- Data Management ---
           ---------------------- */   

        /* Data updates */
        public void UpdateDetailsBlock()
        {
            try
            {
                DetailsBlock.Text =
                    "UserID: " + LoginFunctions.CurrentUserID + "\n"
                    + "Name: " + LoginFunctions.CurrentStaffName + "\n"
                    + "\n"
                    + "Entity: " + EntityFunctions.CurrentEntityName + "\n"
                    + "Default: " + EntityFunctions.DefaultEntityName;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error refreshing current details", generalException);
            }
        }

        /* Other/shared functions */
        public bool ConfirmClosure()
        {
            return MessageFunctions.QuestionYesNo("Are you sure you want to exit?", "Close ProjectTile Application?");
        }
        
        /* ----------------------
           -- Event Management ---
           ---------------------- */  

        /* Control-specific events */
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!ConfirmClosure()) { e.Cancel = true; } // Cancel closure if not sure
        }

        private void Main_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double targetWidth = Main.ActualWidth - 15;
            double targetHeight = Main.ActualHeight - 69;

            MainFrame.Width = targetWidth > MainFrame.MinWidth ? targetWidth : MainFrame.MinWidth;
            MainFrame.Height = targetHeight > MainFrame.MinHeight ? targetHeight : MainFrame.MinHeight;
        }

        private void ChangeEntity_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowEntityPage("Switch");
        }

        private void NewEntity_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowEntityPage("New");
        }

        private void AmendEntity_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowEntityPage("Amend");
        }

        private void DefaultEntity_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowEntityPage("Default");
        }

        private void AdminMenu_Login_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowLoginPage("LogIn");
        }

        private void AdminMenu_Password_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowLoginPage("PassChange");
        }

        private void AdminMenu_Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ViewStaff_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowStaffPage("View");
        }


        private void AmendStaff_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowStaffPage("Amend");
        }


        private void NewStaff_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowStaffDetailsPage("New", 0);
        }

        private void StaffEntities_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowStaffEntitiesPage();
        }

        private void ViewProduct_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProductPage("View");
        }

        private void NewProduct_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProductPage("New");
        }

        private void AmendProduct_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProductPage("Amend");
        }

        private void ViewClient_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowClientPage("View");
        }

        private void NewClient_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowClientPage("New");
        }

        private void AmendClient_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowClientPage("Amend");
        }

    } // class
} // namespace
