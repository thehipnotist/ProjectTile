using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ProjectTile
{
    /// <summary>
    /// Interaction logic for AboutPage.xaml
    /// </summary>
    public partial class AboutPage : Page, INotifyPropertyChanged
    {
        // ---------------------- //
        // -- Global Variables -- //
        // ---------------------- //   

        // Global/page parameters //
        string newLine = "\n";
        public event PropertyChangedEventHandler PropertyChanged;

        string versionText = "";
        public string VersionText
        {
            get { return versionText; }
            set
            {
                versionText = value;
                OnPropertyChanged("VersionText");
            }
        }

        string backgroundText = "";
        public string BackgroundText
        {
            get { return backgroundText; }
            set
            {
                backgroundText = value;
                OnPropertyChanged("BackgroundText");
            }
        }

        string systemText = "";
        public string SystemText
        {
            get { return systemText; }
            set
            {
                systemText = value;
                OnPropertyChanged("SystemText");
            }
        }

        string acknowledgementsText = "";
        public string AcknowledgementsText
        {
            get { return acknowledgementsText; }
            set
            {
                acknowledgementsText = value;
                OnPropertyChanged("AcknowledgementsText");
            }
        }

        string historyText = "";
        public string HistoryText
        {
            get { return historyText; }
            set
            {
                historyText = value;
                OnPropertyChanged("HistoryText");
            }
        }

        // Current variables //

        // Current records //

        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //

        // Initialize and Load //
        public AboutPage()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Page));
            KeepAlive = false;

            this.DataContext = this;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            VersionText = "This version is written in Visual C# 2013 and SQL Server 2014. It requires Windows with .NET 4.5 or above and a SQL Server instance (2012 or above).";

            BackgroundText = "ProjectTile was created by Mark Adrian Johnson in 2019 to learn and demonstrate his programming skills."
                + newLine + "The front end is written in Visual C# with WPF forms. The back end is a scripted SQL Server database.";
            Run gitHub1 = new Run("All of the code is available on GitHub at ");
            Run gitHub2 = new Run("github.com/thehipnotist/ProjectTile");
            Run gitHub3 = new Run(".");
            Hyperlink gitHubLink = new Hyperlink(gitHub2);
            //gitHubLink.NavigateUri = new Uri("https://github.com/thehipnotist/ProjectTile");
            gitHubLink.Click += requestGitHubLink;
            GitHubText.Inlines.Add(gitHub1);
            GitHubText.Inlines.Add(gitHubLink);
            GitHubText.Inlines.Add(gitHub3);

            SystemText = "ProjectTile is designed to simulate an in-house system for managing small software implementation or improvement projects, most of which would be on behalf of clients "
                + "(software customers)."
                + newLine + "It allows multiple Entities to be set up to represent different parts of the business - mainly to allow some 'dummy' data to be created in a Sample company "
                + "- although Products are currently 'global' to all Entities."
                + newLine + "Staff can be assigned to multiple Entities. Clients are per Entity, although it is possible to copy them (and their contacts, but not products) to another Entity.";

            AcknowledgementsText = "Many thanks to all of the people on various forums (StackOverflow, Microsoft etc.) who had previously asked questions that I needed to ask, to the people"
                + " who took the time to answer, and - most of all - to the ones who gave the answers I needed!"
                + newLine + "I used a combination of the Microsoft Visual C# Step by Step guide and Programming with Mosh to get started. Also thanks to the lovely Monica Sudiwala for her "
                + "patience as I wrote this, and for keeping me sane.";

            Run getInTouch1 = new Run("Please see the ");
            Run getInTouch2 = new Run("Frequently Asked Questions");
            Run getInTouch3 = new Run(" page for contact details.");
            Hyperlink getInTouchLink = new Hyperlink(getInTouch2);
            getInTouchLink.Click += openFAQs;
            GetInTouchText.Inlines.Add(getInTouch1);
            GetInTouchText.Inlines.Add(getInTouchLink);
            GetInTouchText.Inlines.Add(getInTouch3);

            HistoryText = "A summary of the key changes is as follows:"
                + newLine
                + newLine + "Version 1.00          23/10/2019          Initial version"
                + newLine + "Version 1.10          24/10/2019          Added stage history audit"
                + newLine + "Version 1.20          28/10/2019          Added project actions, menu icons, version history and GitHub link"; 
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

        private void requestGitHubLink(object sender, RoutedEventArgs e)
        {
            Uri gitHubUri = new Uri("https://github.com/thehipnotist/ProjectTile");
            Process.Start(new ProcessStartInfo(gitHubUri.AbsoluteUri));
            e.Handled = true;
        }

        // ---------------------- //
        // -- Event Management -- //
        // ---------------------- //

        // Generic (shared) control events //
        private void openFAQs(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowFAQPage();
        }

        // Control-specific events //
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (Globals.MyStaffID > 0) { PageFunctions.ShowTilesPage(); }
            else { PageFunctions.ShowLoginPage(PageFunctions.LogIn); }
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {

        }



    } // class
} // namespace
