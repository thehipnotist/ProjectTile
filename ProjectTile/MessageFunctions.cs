using System;
using System.Collections.Generic;
using System.Windows;

namespace ProjectTile
{
    public class MessageFunctions : Globals
    {
//        private static MainWindow winMain = (MainWindow)App.Current.MainWindow;
        public static List<string> queryList = new List<string>();
        
        public static void Error(string customMessage, Exception exp, string caption = "Error")
        {
            try
            {
                string message = customMessage.Replace(DbUserPrefix, "");
                string innerException = "";

                if (exp != null)
                {
                    if (exp.InnerException != null) { innerException = exp.InnerException.ToString(); }
                    logError(customMessage, exp.GetType().ToString(), exp.Message, exp.TargetSite.ToString(), innerException);

                    message = message + ": " + exp.Message;

                    if (message.Substring(message.Length - 1, 1) != ".") { message = message + "."; }
                    message = message.Replace(": : ", ": ");
                    message = message + "\n\nPlease contact your system administrator.";

                    if (innerException != "")
                    {
                        MessageBoxResult answer = MessageBox.Show(message + " Would you like to view the full error?", caption, MessageBoxButton.YesNo,
                            MessageBoxImage.Error);
                        if (answer == MessageBoxResult.Yes)
                        {
                            MessageBox.Show(innerException, "Inner Exception", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else { MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error); }
                }
                else
                {
                    logError(customMessage, "Custom");
                    if (message.Substring(message.Length - 1, 1) != ".") { message = message + "."; }
                    message = message + "\n\nPlease contact your system administrator.";
                    MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch
            {
                MessageBox.Show(customMessage, caption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void logError(string customMessage, string errorType, string expMessage = "", string targetSite = "", string innerException = "")
        {
            try
            {
                ErrorLog newError = new ErrorLog()
                {
                    CustomMessage = customMessage,
                    ExceptionMessage = expMessage,
                    ExceptionType = errorType,
                    TargetSite = targetSite,
                    LoggedAt = DateTime.Now,
                    LoggedBy = MyUserID,
                    InnerException = innerException
                };

                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    existingPtDb.ErrorLog.Add(newError);
                    existingPtDb.SaveChanges();
                }
            }
            catch // (Exception e)
            {
                // MessageBox.Show(e.Message + ": " + e.InnerException.ToString());
                // Do nothing - no point throwing another error!
            }
        }

        public static void SuccessMessage(string message, string caption)
        {
            PageFunctions.DisplayMessage(message, caption, 10, true);
        }

        public static void InfoMessage(string message, string caption)
        {
            PageFunctions.DisplayMessage(message, caption, 10, false);
        }

        public static void CancelInfoMessage()
        {
            if (InfoMessageDisplaying) { PageFunctions.HideMessage(); }
        }

        public static void InvalidMessage(string message, string caption)
        {
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Stop);
        }

        public static void SplitInvalid(string combinedMessage)
        {
            try
            {
                int intPipePosition = combinedMessage.IndexOf("|");
                if (intPipePosition < 0) { InvalidMessage(combinedMessage, "Please try again"); } // Shouldn't happen but just in case
                else
                {
                    int intCaptionLength = combinedMessage.Length - intPipePosition - 1;
                    InvalidMessage(combinedMessage.Substring(0, intPipePosition), combinedMessage.Substring(intPipePosition + 1, intCaptionLength));
                }
            }
            catch { InvalidMessage(combinedMessage, "Please try again"); }
        }

        public static bool ConfirmOKCancel(string message, string caption = "Continue?")
        {
            MessageBoxResult answer = MessageBox.Show(message, caption, MessageBoxButton.OKCancel, MessageBoxImage.Question);
            return (answer == MessageBoxResult.OK);
        }

        public static bool QuestionYesNo(string message, string caption)
        {
            MessageBoxResult answer = MessageBox.Show(message, caption, MessageBoxButton.YesNo, MessageBoxImage.Asterisk);
            return (answer == MessageBoxResult.Yes);
        }

        public static bool WarningYesNo(string message, string caption = "")
        {
            if (caption == "") { caption = "Are You Sure?"; }
            MessageBoxResult answer = MessageBox.Show(message, caption, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            return (answer == MessageBoxResult.Yes);
        }

        public static void ClearQuery()
        {
            queryList.Clear();
        }

        public static void AddQuery(string message)
        { 
            if (message.ToUpper().IndexOf("ALSO") < 0) { message = "Also, " + FirstLetterCase(message, false); }
            if (!message.EndsWith(".")) { message = message + "."; }
            queryList.Add(message);
        }

        public static string FirstLetterCase(string message, bool upper)
        {
            string firstLetter = message.Substring(0, 1);
            firstLetter = upper? firstLetter.ToUpper() : firstLetter.ToLower();
            string remainder = message.Substring(1, message.Length - 1);
            return firstLetter + remainder;
        }

        public static string RemoveAlso(string message)
        {
            message = message.Replace("Also, ", "").Replace("also ", "");
            return FirstLetterCase(message, true);
        }

        public static bool AskQuery(string mainQuery = "", string caption = "")
        {
            if (mainQuery == "" & queryList.Count == 0) { return true; }

            string isCorrect = " Was this intended?";
            string areIntentional = " Also, are the following intentional?" + "\n";
            string fullMessage = mainQuery;

            if (queryList.Count == 0) { } // Nothing else required, just show the query
            else if (queryList.Count == 1) 
            { 
                string query = queryList[0];
                if (fullMessage == "") 
                { 
                    query = RemoveAlso(query);
                }
                fullMessage = fullMessage + query + isCorrect; 
            }
            else
            {
                if (fullMessage == "") { areIntentional = areIntentional.Replace(" Also, are", "Are"); }
                fullMessage = fullMessage + areIntentional;
                foreach (string query in queryList)
                {
                    fullMessage = fullMessage + "\n" + RemoveAlso(query);
                }
            }

            ClearQuery();
            return MessageFunctions.WarningYesNo(fullMessage, caption); 
        }

    } // class
} // namespace
