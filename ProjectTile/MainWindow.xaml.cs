﻿using System;
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
            Style = (Style)FindResource(typeof(Window));
            toggleMainMenus(false);
        }

        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                PageFunctions.ShowLoginPage(PageFunctions.LogIn);
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
        
        public void toggleMainMenus(bool Show)
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
            string loseChanges = "";
            try
            {
                string thisPage = PageFunctions.thisPageName();
                if (thisPage != "TilesPage" && thisPage != "LoginPage") { loseChanges = " Any unsaved changes you have made would be lost."; }
            }
            catch 
            { 
                // Do nothing as it's not worth raising an error when the user wants to leave
            }
            return MessageFunctions.QuestionYesNo("Are you sure you want to exit?" + loseChanges, "Close ProjectTile Application?");
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
            PageFunctions.ShowClientPage(PageFunctions.View);
        }

        private void NewClient_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowClientPage(PageFunctions.New);
        }

        private void AmendClient_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowClientPage(PageFunctions.Amend);
        }

        private void ExitMenu_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HelpMenu_About_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowHelpPage(PageFunctions.About);
        }

    } // class
} // namespace
