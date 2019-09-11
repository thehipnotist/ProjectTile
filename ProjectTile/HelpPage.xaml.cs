using System;
using System.Collections.Generic;
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
    public partial class HelpPage : Page
    {
        /* ----------------------
           -- Global Variables --
           ---------------------- */   

        /* Global/page parameters */
        string pageMode;

        /* Current variables */

        /* Current records */


        /* ----------------------
           -- Page Management ---
           ---------------------- */

        /* Initialize and Load */
        public HelpPage()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Page));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string originalString = NavigationService.CurrentSource.OriginalString;
                pageMode = PageFunctions.pageParameter(originalString, "Mode");
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
                
                MainText.Text = "The front end is written in Visual C# with WPF forms. The back end is a scripted SQL Server database.\n\n"
                    + "All of the code will be made available on GitHub soon.";
            }
            
        }



        /* ----------------------
           -- Data Management ---
           ---------------------- */

        /* Data updates */

        /* Data retrieval */

        /* Other/shared functions */


        /* ----------------------
           -- Event Management ---
           ---------------------- */

        /* Generic (shared) control events */

        /* Control-specific events */





        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowTilesPage();
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {

        }



    } // class
} // namespace
