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
        MainWindow winMain = (MainWindow)App.Current.MainWindow;
        string pageMode;
        bool pageSuccess = false;

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
            UserID.Focus();
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
                    NewPassword.Visibility = Visibility.Hidden;
                    NewPasswordLabel.Visibility = Visibility.Hidden;
                    ConfirmPassword.Visibility = Visibility.Hidden;
                    ConfirmLabel.Visibility = Visibility.Hidden;
                    if (Globals.CurrentStaffID > 0)    // already logged in, aiming to change login
                    {
                        PageHeader.Content = "Log In as a Different User";
                        Welcome.Content = "Please enter a UserID and password to change your login.";
                        RightHeader.Visibility = Visibility.Hidden;
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error setting initial login view", generalException);
                    PageFunctions.ShowTilesPage();
                }
            }
            else if (pageMode == PageFunctions.PassChange)
            {
                try
                {
                    CommitButtonText.Text = "Change";
                    PageHeader.Content = "Change Your ProjectTile Password";
                    HeaderImage2.SetResourceReference(Image.SourceProperty, "PasswordIcon");
                    Welcome.Content = "Please enter your existing password, and your new password twice.";
                    UserID.IsEnabled = false;
                    UserID.Text = Globals.CurrentUserID;
                    RightHeader.Visibility = Visibility.Hidden;
                    PleaseWaitLabel.Content = "Attempting password change - please wait...";
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error setting screen components", generalException);
                    PageFunctions.ShowTilesPage();
                }
            }

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

        private void toggleWaitMessage(bool display)
        {
            Visibility showHide = display ? Visibility.Visible : Visibility.Hidden;
            PleaseWaitLabel.Visibility = showHide;
            //SpinEllipse.Visibility = showHide;
            //SpinRectangle.Visibility = showHide;

            //MessageBox.Show(pageSuccess.ToString());
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
            else if (password == "")
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
            Action a = new Action (  () => { toggleWaitMessage(true); });

            Action b = new Action (  () =>
                {
                    if (!LoginFunctions.CheckPassword(userID, password))
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
                        bool success = LoginFunctions.ChangeLoginDetails(Globals.CurrentStaffID, userID, NewPassword.Password, ConfirmPassword.Password);
                        if (success) 
                        {
                            MessageFunctions.SuccessMessage("Your password has been changed successfully.", "Password Changed");
                            pageSuccess = true;
                        }
                    }
                }   
            );

            Action c = new Action (  () =>
                {
                    if (pageSuccess)
                    {
                        LoginFunctions.CompleteLogIn();
                    }
                    toggleWaitMessage(false);
                }
            );

            Task task1 = Task.Factory.StartNew(() => { this.Dispatcher.BeginInvoke(a, DispatcherPriority.Send); Thread.Sleep(100); });
            Task task2 = task1.ContinueWith((async) => { winMain.Dispatcher.BeginInvoke(b, DispatcherPriority.SystemIdle); Thread.Sleep(100); }, TaskContinuationOptions.OnlyOnRanToCompletion);
            Task task3 = task2.ContinueWith((antecedent) => this.Dispatcher.BeginInvoke(c, DispatcherPriority.Background), TaskContinuationOptions.OnlyOnRanToCompletion);            
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
            if (Globals.CurrentStaffID > 0) { PageFunctions.ShowTilesPage(); }
            else { winMain.Close(); } // not yet logged in, so close application
        }

    } // class
} // namespace
