using System;
using System.Windows;
using System.Collections;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Threading;

namespace ProjectTile
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private CancellationTokenSource cancelMessageTokenSource = null;
        private bool setFavouriteMode = false;
        TableSecurity myPermissions;
        
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
                HideMessage();
                subscribeToDelegates();
                PageFunctions.ShowLoginPage(PageFunctions.LogIn);                
            }
            catch (Exception generalException) { MessageFunctions.Error("Error loading page", generalException); }
        }

        private void subscribeToDelegates()
        {
            PageFunctions.UpdateDetailsBlock += UpdateDetailsBlock;
            PageFunctions.MenuSecurity += MenuSecurity;
            PageFunctions.HideMessage += HideMessage;
            PageFunctions.CloseApplication += Close;
            PageFunctions.DisplayMessage += DisplayMessage;
            PageFunctions.ToggleMainMenus += ToggleMainMenus;
            PageFunctions.ToggleSideButtons += ToggleSideButtons;
            PageFunctions.ShowFavouriteButton += ShowFavouriteButton;
            PageFunctions.ToggleFavouriteButton += ToggleFavouriteButton;
        }

        // Menu settings //
        public void MenuSecurity()
        {
            try
            {
                myPermissions = Globals.MyPermissions;
                
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
        
        public void ToggleMainMenus(bool show)
        {
            if (show)
            {
                MainMenu.Visibility = LoginMenu.Visibility = Visibility.Visible;
            }
            else
            {
                MainMenu.Visibility = LoginMenu.Visibility = Visibility.Hidden;
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

        public void ToggleSideButtons(bool TilesPage)
        {
            if (TilesPage)
            {
                if (Globals.MyPermissions.Allow("ViewProjects") || Globals.MyPermissions.Allow("EditProjects")) { ProjectButton.Visibility = FavouriteButton.Visibility = Visibility.Visible; }
                FavouriteButton.IsEnabled = (Globals.FavouriteProjectID > 0);
                FavouriteButtonText.Text = "Main Project";
                setFavouriteMode = false;
            }
            else
            {
                ProjectButton.Visibility = FavouriteButton.Visibility = Visibility.Hidden;
            }
        }

        public void ShowFavouriteButton() // Allows other pages to show this for setting the favourite
        {
            FavouriteButton.Visibility = Visibility.Visible;
            FavouriteButtonText.Text = "Set Main";
            setFavouriteMode = true;
            ToggleFavouriteButton(false);
        }

        public void ToggleFavouriteButton(bool enable)
        {
            FavouriteButton.IsEnabled = enable;
        }

        public void DisplayMessage(string message, string caption, int seconds, bool success)
        {
            if (cancelMessageTokenSource != null) { cancelMessageTokenSource.Cancel(); } // Stop any existing 'wait' that will result in the new message being cleared early
            HideMessage();

            cancelMessageTokenSource = new CancellationTokenSource();
            CancellationToken cancelMessageToken = cancelMessageTokenSource.Token;

            Action act1 = new Action(() => { ShowMessage(message, caption, success); });
            Action act3 = new Action(() => { EndMessage(cancelMessageToken); });

            Task task1 = Task.Factory.StartNew(() => { this.Dispatcher.BeginInvoke(act1, DispatcherPriority.Send); });
            Task task2 = task1.ContinueWith((async) => { Wait(seconds); }, TaskContinuationOptions.OnlyOnRanToCompletion);
            Task task3 = task2.ContinueWith((antecedent) => this.Dispatcher.BeginInvoke(act3, DispatcherPriority.Background), TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private void ShowMessage(string message, string caption, bool success)
        {
            if (success)
            {
                SuccessImage.Visibility = Visibility.Visible;
                Globals.InfoMessageDisplaying = false;
            }
            else
            {
                InfoImage.Visibility = Visibility.Visible;
                Globals.InfoMessageDisplaying = true;
            }
            CaptionBlock.Text = caption;
            ContentBlock.Text = message;
            CaptionBlock.Visibility = ContentBlock.Visibility = Visibility.Visible;

        }

        private void Wait(int seconds)
        {
            Thread.Sleep(seconds * 1000);
        }

        private void EndMessage(CancellationToken cancelHide)
        {
            if (!cancelHide.IsCancellationRequested) { HideMessage(); }
        }

        public void HideMessage()
        {
            CaptionBlock.Visibility = ContentBlock.Visibility = Visibility.Hidden;
            InfoImage.Visibility = SuccessImage.Visibility = Visibility.Collapsed;
            CaptionBlock.Text = ContentBlock.Text = "";
            Globals.InfoMessageDisplaying = false;
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
            double frameTargetWidth = Main.ActualWidth - 16;
            double frameTargetHeight = Main.ActualHeight - 69;
            double borderTargetHeight = Main.ActualHeight - 69;
            double roundTargetHeight = Main.ActualHeight - 149;
            double roundTargetWidth = Main.ActualWidth - 161;

            MainFrame.Width = frameTargetWidth;
            MainFrame.Height = frameTargetHeight;
            FullBorder.Height = borderTargetHeight;
            FullBorder.Width = frameTargetWidth;
            RoundOffBorder.Height = roundTargetHeight;
            RoundOffBorder.Width = roundTargetWidth;

            //MessageBox.Show(Main.ActualWidth.ToString() + ", " + Main.ActualHeight.ToString());

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
            PageFunctions.ShowProjectContactsPage();
        }

        private void ProjectProduct_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProjectProductsPage();
        }

        private void ProjectButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProjectTeamsPage(pageMode: "", selectedStaffID: Globals.MyStaffID);
        }

        private void HelpMenu_About_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowAboutPage();
        }

        private void HelpMenu_FAQ_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowFAQPage();
        }

        private void FavouriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (setFavouriteMode) 
            { 
                bool success = ProjectFunctions.SetFavourite();
                if (success) { FavouriteButton.IsEnabled = false; }
            }
            else
            {
                ProjectFunctions.OpenFavourite();
            }
        }


    } // class
} // namespace
