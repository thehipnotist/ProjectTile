using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Linq;
using System.Collections;
using System.Globalization;
using System.Threading;

namespace ProjectTile
{
    public class PageFunctions
    {
        private static MainWindow winMain = (MainWindow)App.Current.MainWindow;
        private static Frame mainFrame = winMain.MainFrame;

        private static UriKind uriDefault = UriKind.RelativeOrAbsolute;
        private static string TilesPage = "TilesPage.xaml";
        public static string AllRecords = "<All>";
        
        public static string InvalidString = "%INVALID%";
        static Dictionary<string, string> charSwitch = new Dictionary<string, string>();

        public PageFunctions()
        {
            charSwitch["'"] = "''";
        }

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
                catch (Exception generalException) { MessageFunctions.Error("Error changing page", generalException); }
            }
            else
            {
                try { mainFrame.Navigate(new Uri(newPageSource, uriDefault)); }
                catch (Exception generalException) { MessageFunctions.Error("Error changing page", generalException); }
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

        public static void ShowProductPage(string pageMode)
        {
            ChangePage("ProductPage.xaml?Mode=" + pageMode);
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
                    MessageFunctions.Error("No page parameter called " + paramName + " found.", null);
                    ShowTilesPage();
                    return null;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving page parameter " + paramName + "", generalException);
                ShowTilesPage();
                return null;
            }
        }

        /* Utilities */
        public static string SqlInput(string inputText, bool mandatory, string fieldName, string fieldTitle = "", string invalidCharacters = "")
        {

            if (fieldTitle == "")
            {
                CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
                TextInfo textInfo = cultureInfo.TextInfo;
                fieldTitle = textInfo.ToTitleCase(fieldName);
            }
            
            if (mandatory && inputText == "")
            {
                MessageFunctions.InvalidMessage(fieldName + "s cannot be blank. Please enter a value in the '" + fieldTitle + "' field and try again.", "No " + fieldTitle + " Entered");
                return InvalidString;
            }
            
            if (invalidCharacters != "")
            {
                List<char> inputChars = inputText.ToCharArray().ToList();
                List<char> invalidChars = invalidCharacters.ToCharArray().ToList();

                foreach (char badChar in invalidChars)
                {
                    if (inputChars.Contains(badChar))
                    {
                        MessageFunctions.InvalidMessage(fieldName + " cannot contain the character '" + badChar.ToString() + "'. Please remove it and try again.", "Invalid Character");
                        return InvalidString;
                    }
                }
            }

            return inputText;
            //return FormatSqlInput(inputText);
        }

        public static string FormatSqlOutput(string inputText)
        {
            return inputText.Replace("''", "'");
        }

        public static string FormatSqlInput(string inputText)
        {
            string outputText = inputText.Replace("'", "''");
            outputText = outputText.Replace("''''", "''"); // avoid unnecessary replacement of two single-quotes             
            return outputText;
        }

    } // class
} // namespace
