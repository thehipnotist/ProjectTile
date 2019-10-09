﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

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
            BackgroundText = "ProjectTile was created by Mark Adrian Johnson in 2019 to learn and demonstrate his programming skills."
                + newLine + "The front end is written in Visual C# with WPF forms. The back end is a scripted SQL Server database."
                + newLine + "All of the code will be made available on GitHub soon.";

            SystemText = "ProjectTile is designed to simulate an in-house system for managing small software implementation or improvement projects, most of which would be on behalf of clients "
                + "(software customers)."
                + newLine + "It allows multiple Entities to be set up to represent different parts of the business - mainly to allow some 'dummy' data to be created in a Sample company"
                + " - although Products are currently 'global' to all Entities."
                + newLine + "Staff can be assigned to multiple Entities. Clients are per Entity, although it is possible to copy them (and their contacts, but not products) to another Entity.";

            AcknowledgementsText = "Many thanks to all of the people on various forums (StackOverflow, Microsoft etc.) who had previously asked questions that I needed to ask, to the people "
                + " who took the time to answer, and - most of all - to the ones who gave the answers I needed."
                + newLine + "Also thanks to the lovely Monica Sudiwala for her patience, and for keeping me sane.";

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
            if (Globals.MyStaffID > 0) { PageFunctions.ShowTilesPage(); }
            else { PageFunctions.ShowLoginPage(PageFunctions.LogIn); }
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {

        }



    } // class
} // namespace