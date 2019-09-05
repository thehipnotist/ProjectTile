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
    /// Interaction logic for PageTemplate.xaml
    /// </summary>
    public partial class PageTemplate : Page
    {
        /* ----------------------
           -- Global Variables --
           ---------------------- */   

        /* Global/page parameters */

        /* Current variables */

        /* Current records */


        /* ----------------------
           -- Page Management ---
           ---------------------- */

        /* Initialize and Load */
        public PageTemplate()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

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
            // To do: check for changes if appropriate

            PageFunctions.ShowTilesPage();
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {

        }



    } // class
} // namespace
