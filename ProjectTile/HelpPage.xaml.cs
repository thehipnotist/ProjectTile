using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for HelpPage.xaml
    /// </summary>
    public partial class HelpPage : Page, INotifyPropertyChanged
    {
        // ---------------------- //
        // -- Global Variables -- //
        // ---------------------- //   

        // Global/page parameters //
        string pageMode;
        string newLine = "\n";
        public event PropertyChangedEventHandler PropertyChanged;

        string backgroundText = "";
        public string BackgroundText
        {
            get
            {
                return backgroundText;
            }
            set
            {
                backgroundText = value;
                OnPropertyChanged("BackgroundText");
            }
        }

        string systemText = "";
        public string SystemText
        {
            get
            {
                return systemText;
            }
            set
            {
                systemText = value;
                OnPropertyChanged("SystemText");
            }
        }

        // Current variables //

        // Current records //

        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //

        // Initialize and Load //
        public HelpPage()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Page));
            KeepAlive = false;

            BackgroundText = "ProjectTile was created by Mark Adrian Johnson in 2019 to learn and demonstrate his programming skills."
               + newLine + "The front end is written in Visual C# with WPF forms. The back end is a scripted SQL Server database."
                + newLine + "All of the code will be made available on GitHub soon.";

            SystemText = "ProjectTile is designed as a system for managing small software implementation or improvement projects, most of which would be on behalf of clients (software customers)."
               + newLine + "It allows multiple Entities to be set up to represent different parts of the business - mainly to allow some 'dummy' data to be created in a Sample company"
               + " - although Products are currently 'global' to all Entities."
               + newLine + "Staff can be assigned to multiple Entities. Clients are per Entity, although it is possible to copy them (and their contacts, but not products) to another Entity.";

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

            if (pageMode == PageFunctions.About)
            {
                CommitButton.Visibility = Visibility.Hidden;
                CancelButtonText.Text = "Close";

                //BackgroundText = "Testing";
            }
            
        }



        // ---------------------- //
        // -- Data Management --- //
        // ---------------------- //

        // Data updates //

        // Data retrieval //

        // Other/shared functions //
        protected void OnPropertyChanged(string eventName)
        {
            try
            {
                PropertyChangedEventHandler thisHandler = PropertyChanged;
                if (thisHandler != null)
                {
                    thisHandler(this, new PropertyChangedEventArgs(eventName));
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error handling changed property", generalException); }
        }

        // ---------------------- //
        // -- Event Management -- //
        // ---------------------- //

        // Generic (shared) control events //

        // Control-specific events //
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (Globals.CurrentStaffID > 0) { PageFunctions.ShowTilesPage(); }
            else { PageFunctions.ShowLoginPage(PageFunctions.LogIn); }
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {

        }



    } // class
} // namespace
