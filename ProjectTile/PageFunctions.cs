﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Linq;
using System.Collections;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace ProjectTile
{
    public class PageFunctions : Globals
    {
        private static MainWindow winMain = (MainWindow)App.Current.MainWindow;
        private static Frame mainFrame = winMain.MainFrame;
        private static UriKind uriDefault = UriKind.RelativeOrAbsolute;

        public delegate void MainWindowDelegate();
        public static MainWindowDelegate UpdateDetailsBlock;
        public static MainWindowDelegate HideMessage;
        public static MainWindowDelegate CloseApplication;
        public static MainWindowDelegate ShowFavouriteButton;
        public static MainWindowDelegate MenuSecurity;

        public delegate void MainBooleanDelegate(bool someBoolean);
        public static MainBooleanDelegate ToggleMainMenus;
        public static MainBooleanDelegate ToggleSideButtons;
        public static MainBooleanDelegate ToggleFavouriteButton;

        public delegate void MainMessageDelegate(string message, string caption, int seconds, bool success);
        public static MainMessageDelegate DisplayMessage;

        // Create variables for each page mode, to avoid any accidental mis-typing etc.
        public const string View = "View";
        public const string New = "New";
        public const string Amend = "Amend";
        public const string LogIn = "LogIn";
        public const string PassChange = "PassChange";
        public const string Switch = "Switch";
        public const string Default = "Default";
        public const string None = "None";
        public const string Lookup = "Lookup";

        // Page changes //
        public static string ThisPageName()
        {
            try
            {
                string mainFrameSource = mainFrame.Content.ToString();
                mainFrameSource = mainFrameSource.Replace("ProjectTile.", "");
                return mainFrameSource;
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error determining page", generalException);
                return "";
            }
        }
        
        public static void ChangePage(string newPageSource)
        {                
            if (newPageSource != TilesPageURI)
            {
                try
                {
                    string mainFrameSource = ThisPageName();
                    string newPageName = newPageSource.Replace(".xaml", "");
                    ToggleSideButtons(false);

                    if (mainFrameSource != TilesPageName && mainFrameSource == newPageName)
                    {
                        var dInv1 = winMain.Dispatcher.Invoke(new Action(() => { navigate(TilesPageURI); })); // Open the tiles page in between to reset
                        var task1 = Task.Factory.StartNew(() => dInv1);
                        var dInv2 = winMain.Dispatcher.Invoke(new Action(() => { navigate(newPageSource); }));
                        Task task2 = task1.ContinueWith((antecedent) => dInv2);
                    }
                    else { navigate(newPageSource); }
                }
                catch (Exception generalException) { MessageFunctions.Error("Error changing page", generalException); }
            }
            else
            {
                try 
                { 
                    navigate(newPageSource);
                    ToggleSideButtons(true);
                }
                catch (Exception generalException) { MessageFunctions.Error("Error changing page", generalException); }
            }
        }

        private static void navigate(string pageSource)
        {
            try
            {
                mainFrame.Content = null;                
                mainFrame.Navigate(new Uri(pageSource, uriDefault));
                mainFrame.NavigationService.RemoveBackEntry();
                GC.Collect();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error navigating to page location '" + pageSource + "'", generalException); }
        }

        // Individual pages //
        public static void ShowTilesPage()
        {
            ChangePage(TilesPageURI);
            ResetProjectParameters();
            ResetClientParameters();
            ResetProjectParameters();
        }

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
            ChangePage("StaffPage.xaml?Mode=" + pageMode + ",StaffID=" + selectedStaffID.ToString());
        }

        public static void ShowStaffDetailsPage(string pageMode, int selectedStaffID)
        {
            ChangePage("StaffDetailsPage.xaml?Mode=" + pageMode + ",StaffID=" + selectedStaffID.ToString());
        }

        public static void ShowStaffEntitiesPage(int selectedStaffID = 0, bool viewOnly = false, string sourcePageMode = "View")
        {
            string pageMode; // Mode is based on viewOnly or permissions; sourcePageMode tells us what the previous screen was
            if (viewOnly) { pageMode = View; }
            else { pageMode = MyPermissions.Allow("EditStaffEntities") ? Amend : View; }
            ChangePage("StaffEntitiesPage.xaml?Mode=" + pageMode + ",StaffID=" + selectedStaffID.ToString() + ",SourceMode=" + sourcePageMode);
        }

        public static void ShowProductPage(string pageMode)
        {
            ChangePage("ProductPage.xaml?Mode=" + pageMode);
        }

        public static void ShowClientPage(string pageMode)
        {
            ChangePage("ClientPage.xaml?Mode=" + pageMode);
        }

        public static void ShowClientContactPage(int contactID = 0)
        {
            string pageMode;  // Mode is based on viewOnly or permissions; sourcePageMode tells us what the previous screen was
            if (ClientSourceMode == View) { pageMode = View; }
            else { pageMode = MyPermissions.Allow("EditClientStaff") ? Amend : View; }
            ChangePage("ClientContactPage.xaml?Mode=" + pageMode + ",ContactID=" + contactID.ToString());
        }

        public static void ShowContactDetailsPage(int contactID = 0)
        {
            string pageMode = (contactID == 0) ? New : Amend;
            ChangePage("ContactDetailsPage.xaml?Mode=" + pageMode + ",ContactID=" + contactID.ToString());
        }

        public static void ShowClientProductsPage(int selectedProductID = 0)
        {
            string pageMode; // Mode is based on viewOnly or permissions; sourcePageMode tells us what the previous screen was
            if (ClientSourceMode == View) { pageMode = View; }
            else { pageMode = MyPermissions.Allow("EditClientProducts") ? Amend : View; }
            ChangePage("ClientProductsPage.xaml?Mode=" + pageMode + ",ProductID=" + selectedProductID.ToString());
        }

        public static void ShowProjectPage(string pageMode, string sourcePage = "")
        {
            if (pageMode == PageFunctions.Amend && !MyPermissions.Allow("EditProjects")) { pageMode = PageFunctions.View; }
            ProjectSourcePage = (sourcePage != "") ? sourcePage : "ProjectPage";
            ChangePage("ProjectPage.xaml?Mode=" + pageMode);
        }

        public static void ShowProjectDetailsPage(string pageMode = "")
        {
            if (pageMode == "")
            {
                if (ProjectSourceMode == View) { pageMode = View; }
                else { pageMode = MyPermissions.Allow("EditProjects") ? Amend : View; }
            }
            ChangePage("ProjectDetailsPage.xaml?Mode=" + pageMode);
        }

        public static void ShowProjectTeamsPage(string pageMode = "", int selectedStaffID = 0, string sourcePage = "")
        {
            if (pageMode == "")
            {
                if (ProjectSourceMode == View) { pageMode = View; }
                else { pageMode = MyPermissions.Allow("EditProjectTeams") ? Amend : View; }
            }
            if (sourcePage != "") { ProjectSourcePage = sourcePage; }
            ChangePage("ProjectTeamsPage.xaml?Mode=" + pageMode + ",StaffID=" + selectedStaffID.ToString());
        }

        public static void ShowProjectProductsPage(string pageMode = "", int selectedProductID = 0, string sourcePage = "")
        {
            if (pageMode == "")
            {
                if (ProjectSourceMode == View) { pageMode = View; }
                else { pageMode = MyPermissions.Allow("EditProjectProducts") ? Amend : View; }
            }
            if (sourcePage != "") { ProjectSourcePage = sourcePage; }
            ChangePage("ProjectProductsPage.xaml?Mode=" + pageMode + ",ProductID=" + selectedProductID.ToString());
        }

        public static void ShowProjectContactsPage(string pageMode = "", int selectedContactID = 0, string sourcePage = "")
        {
            if (pageMode == "")
            {
                if (ProjectSourceMode == View) { pageMode = View; }
                else { pageMode = MyPermissions.Allow("EditClientTeams") ? Amend : View; }
            }
            if (sourcePage != "") { ProjectSourcePage = sourcePage; }
            ChangePage("ProjectContactsPage.xaml?Mode=" + pageMode + ",ContactID=" + selectedContactID.ToString());
        }

        public static void ShowStageHistoryPage(string pageMode = "")
        {
            if (pageMode == "" || pageMode == Amend) { pageMode = MyPermissions.Allow("EditStageHistory") ? Amend : View; }
            ChangePage("StageHistoryPage.xaml?Mode=" + pageMode);
        }

        public static void ShowTimelinePage(int projectID, string pageMode = "")
        {
            if (pageMode == "" || pageMode == Amend) { pageMode = MyPermissions.Allow("EditStageHistory") ? Amend : View; }
            ChangePage("TimelinePage.xaml?Mode=" + pageMode + ",ProjectID=" + projectID.ToString());
        }

        public static void ShowActionsPage(string pageMode = "", int staffID = 0)
        {
            if (pageMode == "" || pageMode == Amend) { pageMode = MyPermissions.Allow("EditActions") ? Amend : View; }
            ChangePage("ActionsPage.xaml?Mode=" + pageMode + ",StaffID=" + staffID.ToString());
        }

        public static void ShowAboutPage()
        {
            ChangePage("AboutPage.xaml?Mode=View");
        }

        public static void ShowFAQPage()
        {
            ChangePage("FAQPage.xaml?Mode=View");
        }

        public static void ShowAuditPage()
        {
            ChangePage("AuditPage.xaml?Mode=View");
        }

        public static void ShowErrorPage()
        {
            ChangePage("ErrorPage.xaml?Mode=View");
        }

        // Page initialisation //
        public static string pageParameter(Page currentPage, string paramName)
        {
            try
            {                             
                string paramValue = "";
                string originalString = currentPage.NavigationService.CurrentSource.OriginalString;

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

        // Utilities //
        public static bool SqlInputOK(string inputText, bool mandatory, string fieldName, string fieldTitle = "", string invalidCharacters = "")
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
                return false;
            }           
            else if (invalidCharacters != "")
            {
                List<char> inputChars = inputText.ToCharArray().ToList();
                List<char> invalidChars = invalidCharacters.ToCharArray().ToList();

                foreach (char badChar in invalidChars)
                {
                    if (inputChars.Contains(badChar))
                    {
                        MessageFunctions.InvalidMessage(fieldName + " cannot contain the character '" + badChar.ToString() + "'. Please remove it and try again.", "Invalid Character");
                        return false;
                    }
                }
            }
            return true;
        }

    } // class
} // namespace
