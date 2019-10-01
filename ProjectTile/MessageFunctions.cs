using System;
using System.Windows;

namespace ProjectTile
{
    public class MessageFunctions : Globals
    {
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
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
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
                int intCaptionLength = combinedMessage.Length - intPipePosition - 1;
                InvalidMessage(combinedMessage.Substring(0, intPipePosition), combinedMessage.Substring(intPipePosition + 1, intCaptionLength));
            }
            catch
            {
                InvalidMessage(combinedMessage, "Please try again");
            }
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

        public static bool WarningYesNo(string message, string caption = "Are You Sure?")
        {
            MessageBoxResult answer = MessageBox.Show(message, caption, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            return (answer == MessageBoxResult.Yes);
        }

    } // class
} // namespace
