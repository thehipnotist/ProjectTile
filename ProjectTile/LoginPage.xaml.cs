using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ProjectTile
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        // ---------------------- //
        // -- Global Variables -- //
        // ---------------------- //

        // Global/page parameters //
        string pageMode;
        bool pageSuccess = false;
        string ssoID = "";
        bool canSSO = false;

        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //

        // Initialize and Load //

        public LoginPage()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Page));
            KeepAlive = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            manageLoad();
        }

        // ---------------------- //
        // -- Data Management --- //
        // ---------------------- //

        // Shared functions //        
        private void check_CapsLock()
        {           
            try
            {
                if (Keyboard.IsKeyToggled(Key.CapsLock)) { CapsLockLabel.Visibility = Visibility.Visible; }
                else { CapsLockLabel.Visibility = Visibility.Hidden; }
            }
            catch (Exception generalException) {  MessageFunctions.Error("Caps lock error", generalException); }
        }


        private void manageLoad()
        {
            try
            {
                Action a = new Action(() => { toggleWaitMessage(true); });
                Action b = new Action(() => { loadContent(); });
                Action c = new Action(() => { completeLoad(); });

                Task task1 = Task.Factory.StartNew(() => { this.Dispatcher.BeginInvoke(a, DispatcherPriority.Send); Thread.Sleep(100); });
                Task task2 = task1.ContinueWith((async) => { this.Dispatcher.BeginInvoke(b, DispatcherPriority.SystemIdle); Thread.Sleep(100); }, TaskContinuationOptions.OnlyOnRanToCompletion);
                Task task3 = task2.ContinueWith((antecedent) => this.Dispatcher.BeginInvoke(c, DispatcherPriority.Background), TaskContinuationOptions.OnlyOnRanToCompletion);    
            }
            catch (Exception generalException) { MessageFunctions.Error("Error loading login page", generalException); }                
        }

        private void loadContent()
        {            
            CapsLockLabel.Visibility = Visibility.Hidden;
            try
            {
                pageMode = PageFunctions.pageParameter(this, "Mode");
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                PageFunctions.ShowTilesPage();
            }
            
            if (pageMode == PageFunctions.LogIn) 
            {
                try
                {
                    ssoID = LoginFunctions.SingleSignonID();
                    canSSO = (ssoID != "");
                    if (Globals.MyStaffID > 0)    // already logged in, aiming to change login
                    {
                        PageHeader.Content = "Log In as a Different User";
                        if (canSSO && ssoID != Globals.MyUserID)
                        {
                            Welcome.Content = "Please use single sign-on or enter a UserID and password to change your login.";
                            SSOCheckBox.IsChecked = true;
                        }
                        else
                        {
                            Welcome.Content = "Please enter a UserID and password to change your login.";
                            SSOCheckBox.IsEnabled = false;
                        }
                        RightHeader.Visibility = Visibility.Hidden;
                    }
                    else if (canSSO)
                    {
                        SSOCheckBox.IsChecked = true;
                        Welcome.Content = "Please log in via single sign-on, or un-check the 'Single Sign-on' option and enter a UserID and password.";
                    }
                    else { SSOCheckBox.IsEnabled = false; }
                }
            catch (Exception generalException) { MessageFunctions.Error("Error setting initial login view", generalException); }       
            }
            else if (pageMode == PageFunctions.PassChange)
            {
                try
                {
                    PasswordLabel.Visibility = Password.Visibility = Visibility.Visible;
                    NewPassword.Visibility = NewPasswordLabel.Visibility = ConfirmPassword.Visibility = ConfirmLabel.Visibility = Visibility.Visible;
                    NewPasswordLabel.Margin = SSOLabel.Margin;
                    NewPassword.Margin = SSOCheckBox.Margin;
                    CommitButtonText.Text = "Change";
                    PageHeader.Content = "Change Your ProjectTile Password";
                    HeaderImage2.SetResourceReference(Image.SourceProperty, "PasswordIcon");
                    Welcome.Content = "Please enter your existing password, and your new password twice.";
                    UserID.IsEnabled = false;
                    UserID.Text = Globals.MyUserID;
                    RightHeader.Visibility = Visibility.Hidden;
                    PleaseWaitLabel.Content = 
                    SSOLabel.Visibility = SSOCheckBox.Visibility = Visibility.Hidden;
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error setting screen components", generalException);
                    PageFunctions.ShowTilesPage();
                }
            }
        }

        private void completeLoad()
        {            
            toggleWaitMessage(false);
            UserID.Focus();
            PleaseWaitLabel.Content = (pageMode == PageFunctions.PassChange)? "Attempting password change - please wait..." : "Attempting login - please wait...";
        }        

        private void toggleSSO(bool useIt)
        {
            PasswordLabel.Visibility = Password.Visibility = useIt? Visibility.Hidden : Visibility.Visible;            
            UserID.Text = useIt ? ssoID : "";
        }

        private void manageCommit(string userID, string password)
        {
            Action a = new Action(() => { toggleWaitMessage(true); });
            Action b = new Action(() => { commitAction(userID, password); });
            Action c = new Action(() => { completeAction(); });

            Task task1 = Task.Factory.StartNew(() => { this.Dispatcher.BeginInvoke(a, DispatcherPriority.Send); Thread.Sleep(100); });
            Task task2 = task1.ContinueWith((async) => { this.Dispatcher.BeginInvoke(b, DispatcherPriority.SystemIdle); Thread.Sleep(100); }, TaskContinuationOptions.OnlyOnRanToCompletion);
            Task task3 = task2.ContinueWith((antecedent) => this.Dispatcher.BeginInvoke(c, DispatcherPriority.Background), TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private void toggleWaitMessage(bool display)
        {
            Visibility showHide = display ? Visibility.Visible : Visibility.Hidden;
            PleaseWaitLabel.Visibility = showHide;
            //SpinEllipse.Visibility = showHide;
            //SpinRectangle.Visibility = showHide;
        }

        private void commitAction(string userID, string password)
        {
            if (pageMode == PageFunctions.LogIn && SSOCheckBox.IsChecked == true && LoginFunctions.SingleSignon(userID)) { pageSuccess = true; }
            else if (!LoginFunctions.CheckPassword(userID, password))
            {
                MessageFunctions.InvalidMessage("Incorrect existing username or password. Please check and try again.", "Incorrect Login");
            }
            else if (pageMode == PageFunctions.LogIn)
            {
                LoginFunctions.AttemptLogin(userID, password);
                pageSuccess = true;
            }
            else if (pageMode == PageFunctions.PassChange)
            {
                bool success = LoginFunctions.ChangeLoginDetails(Globals.MyStaffID, userID, NewPassword.Password, ConfirmPassword.Password);
                if (success)
                {
                    MessageFunctions.SuccessMessage("Your password has been changed successfully.", "Password Changed");
                    pageSuccess = true;
                }
            }
        }

        private void completeAction()
        {
            if (pageSuccess) { LoginFunctions.CompleteLogIn(); }
            toggleWaitMessage(false);
        }

        // ---------------------- //
        // -- Event Management -- //
        // ---------------------- //

        // Control-specific events //
        private void LogInButton_Click(object sender, RoutedEventArgs e)
        {
           
            string userID = UserID.Text;
            string password = Password.Password;

            // First check that the details are correct
            if (userID == "")
            {
                MessageFunctions.InvalidMessage("Please enter your User ID in the 'UserID' text box.", "User ID Required");
                return;
            }
            else if (SSOCheckBox.IsChecked == false && password == "")
            {
                string strContext = (pageMode == PageFunctions.PassChange) ? "existing " : "";
                MessageFunctions.InvalidMessage("Please enter your " + strContext + "password in the 'Password' text box.", "Password Required");
                return;
            }

            /*
            try
            {
                DoubleAnimation spinAnimation = new DoubleAnimation(0, 365, new Duration(TimeSpan.FromSeconds(1)));
                Storyboard.SetTargetName(spinAnimation, "RectangleSpin");
                Storyboard.SetTargetProperty(spinAnimation, new PropertyPath(RotateTransform.AngleProperty));             
                spinStoryboard = new Storyboard();
                spinStoryboard.RepeatBehavior = RepeatBehavior.Forever;
                spinStoryboard.Children.Add(spinAnimation);
                spinStoryboard.Begin(this, true);
            }
            catch (Exception generalException) { MessageFunctions.Error("Animation error", generalException); }
            */ 

            // Show the 'please wait' message and queue the next activity for afterwards
            manageCommit(userID, password);           
        }

        private void Password_Changed(object sender, RoutedEventArgs e)
        {
            //check_CapsLock(); // Removed as this can cause errors on load and isn't needed
        }

        private void Password_KeyUp(object sender, KeyEventArgs e)
        {
            check_CapsLock();
        }

        private void Password_GotFocus(object sender, RoutedEventArgs e)
        {
            check_CapsLock();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (Globals.MyStaffID > 0) { PageFunctions.ShowTilesPage(); }
            else { PageFunctions.CloseApplication(); } // not yet logged in, so close application
        }

        private void SSOCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            toggleSSO(true);
        }

        private void SSOCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            toggleSSO(false);
        }

    } // class
} // namespace
