using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;
using System.Windows.Threading;

namespace ProjectTile
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        /* ----------------------
           -- Global Variables --
           ---------------------- */

        /* Global/page parameters */
        MainWindow winMain = (MainWindow)App.Current.MainWindow;
        string pageMode;

        /* ----------------------
           -- Page Management ---
           ---------------------- */

        /* Initialize and Load */

        public LoginPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UserID.Focus();
            CapsLockLabel.Visibility = Visibility.Hidden;

            try
            {
                string originalString = NavigationService.CurrentSource.OriginalString;
                pageMode = PageFunctions.pageParameter(originalString, "Mode"); //, ref winMain
            }
            catch (Exception generalException)
            {
                MessageFunctions.ErrorMessage("Error retrieving query details: " + generalException.Message);
                PageFunctions.ShowTilesPage();
            }

            if (pageMode == "LogIn")
            {
                try
                {
                    NewPassword.Visibility = Visibility.Hidden;
                    NewPasswordLabel.Visibility = Visibility.Hidden;
                    ConfirmPassword.Visibility = Visibility.Hidden;
                    ConfirmLabel.Visibility = Visibility.Hidden;
                    if (LoginFunctions.CurrentStaffID > 0)    // already logged in, aiming to change login
                    {
                        PageHeader.Content = "Log In as a Different User";
                        Welcome.Content = "Please enter a UserID and password to change your login.";
                        RightHeader.Visibility = Visibility.Hidden;
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.ErrorMessage("Error setting initial login view: " + generalException.Message);
                    PageFunctions.ShowTilesPage();
                }
            }
            else if (pageMode == "PassChange")
            {
                try
                {
                    CommitButtonText.Text = "Change";
                    PageHeader.Content = "Change Your ProjectTile Password";
                    Welcome.Content = "Please enter your existing password, and your new password twice.";
                    UserID.IsEnabled = false;
                    UserID.Text = LoginFunctions.CurrentUserID;
                    RightHeader.Visibility = Visibility.Hidden;
                    PleaseWaitLabel.Content = "Attempting password change - please wait...";
                }
                catch (Exception generalException)
                {
                    MessageFunctions.ErrorMessage("Error setting screen components: " + generalException.Message);
                    PageFunctions.ShowTilesPage();
                }
            }

        }

        /* ----------------------
           -- Data Management ---
           ---------------------- */

        /* Shared functions */        
        private void check_CapsLock()
        {           
            try
            {
                if (Keyboard.IsKeyToggled(Key.CapsLock)) { CapsLockLabel.Visibility = Visibility.Visible; }
                else { CapsLockLabel.Visibility = Visibility.Hidden; }
            }
            catch (Exception generalException)
            {
                MessageFunctions.ErrorMessage("Caps lock error: " + generalException.Message);
            }
        }

        /* ----------------------
           -- Event Management ---
           ---------------------- */

        /* Control-specific events */
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
            else if (password == "")
            {
                string strContext = (pageMode == "PassChange") ? "existing " : "";
                MessageFunctions.InvalidMessage("Please enter your " + strContext + "password in the 'Password' text box.", "Password Required");
                return;
            }

            // Show the 'please wait' message and queue the next activity for afterwards
            Action a = new Action(  () => { PleaseWaitLabel.Visibility = Visibility.Visible; }  );
            this.Dispatcher.Invoke(a, DispatcherPriority.Send);

            Action b = new Action(  () =>
            {
                Thread.Sleep(100); // Ensure this starts after the 'please wait' message

                if (!LoginFunctions.CheckPassword(userID, password))
                {
                    MessageFunctions.InvalidMessage("Incorrect existing username or password. Please check and try again.", "Incorrect Login");
                }
                else if (pageMode == "LogIn") { LoginFunctions.AttemptLogin(userID, password); }
                else if (pageMode == "PassChange")
                {
                    bool success = LoginFunctions.ChangeLoginDetails(LoginFunctions.CurrentStaffID, userID, NewPassword.Password, ConfirmPassword.Password);
                    if (success) 
                    {
                        MessageFunctions.SuccessMessage("Your password has been changed successfully.", "Password Changed");
                        PageFunctions.ShowTilesPage();
                    }
                }

                PleaseWaitLabel.Visibility = Visibility.Hidden;
 
            }   );

            this.Dispatcher.Invoke(b, DispatcherPriority.ApplicationIdle);
            
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
            if (LoginFunctions.CurrentStaffID > 0)  { PageFunctions.ShowTilesPage(); }
            else { winMain.Close(); } // not yet logged in, so close application
        }

    } // class
} // namespace
