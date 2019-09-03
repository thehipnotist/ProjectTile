using System;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ProjectTile
{
    public class PageFunctions
    {
        private static MainWindow winMain = (MainWindow)App.Current.MainWindow;
        private static Frame mainFrame = winMain.MainFrame;

        private static UriKind uriDefault = UriKind.RelativeOrAbsolute;
        private static string TilesPage = "TilesPage.xaml";
        public static string AllRecords = "<All>";

        /* Page changes */
        public static void ChangePage(string newPageSource)
        {                
            if (newPageSource != TilesPage) // && !blnFirstLoad)
            {
                try
                {
                    string mainFrameSource = mainFrame.Content.ToString();
                    mainFrameSource = mainFrameSource.Replace("ProjectTile.", "");
                    string mewPageName = newPageSource.Replace(".xaml", "");

                    if (mainFrameSource != "TilesPage" && mainFrameSource == mewPageName)
                    {
                        var dInv1 = winMain.Dispatcher.Invoke(new Action(() => { mainFrame.Navigate(new Uri(TilesPage, uriDefault)); }));
                        var task1 = Task.Factory.StartNew(() => dInv1);
                        var dInv2 = winMain.Dispatcher.Invoke(new Action(() => { mainFrame.Navigate(new Uri(newPageSource, uriDefault)); }));
                        Task task2 = task1.ContinueWith((antecedent) => dInv2);
                    }
                    else { mainFrame.Navigate(new Uri(newPageSource, uriDefault)); }
                }
                catch (Exception generalException) { MessageFunctions.ErrorMessage("Error changing page: " + generalException.Message); }
            }
            else
            {
                try { mainFrame.Navigate(new Uri(newPageSource, uriDefault)); }
                catch (Exception generalException) { MessageFunctions.ErrorMessage("Error changing page: " + generalException.Message); }
            }
        }

        public static void ShowTilesPage() { ChangePage(TilesPage); }

        public static void ShowEntityPage(string pageMode)
        {
            ChangePage("EntityPage.xaml?Mode=" + pageMode);
        }

        public static void ShowLoginPage(string pageMode)
        {
            ChangePage("LoginPage.xaml?Mode=" + pageMode);
        }

        public static void ShowStaffPage(string pageMode, int selectedStaffID = 0)
        {
            ChangePage("StaffPage.xaml?Mode=" + pageMode + ",SelectedID=" + selectedStaffID.ToString());
        }

        public static void ShowStaffDetailsPage(string pageMode, int selectedStaffID)
        {
            ChangePage("StaffDetailsPage.xaml?Mode=" + pageMode + ",SelectedID=" + selectedStaffID.ToString());
        }

        public static void ShowStaffEntitiesPage(int selectedStaffID = 0, bool viewOnly = false, string sourcePageMode = "View")
        {
            string pageMode;
            if (viewOnly) { pageMode = "View"; }
            else { pageMode = LoginFunctions.MyPermissions.Allow("EditStaffEntities") ? "Amend" : "View"; }

            ChangePage("StaffEntitiesPage.xaml?Mode=" + pageMode + ",SelectedID=" + selectedStaffID.ToString() + ",SourceMode=" + sourcePageMode);
        }

        /* Page loads */
        public static string pageParameter(string originalString, string paramName) //, ref MainWindow winMain
        {                        
            try
            {                
                string paramValue = "";
                
                string[] originalStringArray = originalString.Split('?', ','); // NB: NavigationService.CurrentSource.Query will not work as it is a relative URL
                foreach (string part in originalStringArray)
                {
                    if (part.StartsWith(paramName + "=")) { paramValue = part.Replace(paramName + "=", ""); }
                }

                if (paramValue != "") { return paramValue; }
                else
                {
                    MessageFunctions.ErrorMessage("No page parameter called " + paramName + " found.");
                    ShowTilesPage();
                    return null;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.ErrorMessage("Error retrieving page parameter " + paramName + ": " + generalException.Message);
                ShowTilesPage();
                return null;
            }
        }
    }
}
