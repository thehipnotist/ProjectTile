using System.Windows;

namespace ProjectTile
{
    public class MessageFunctions
    {
        public static void ErrorMessage(string message, string caption = "Error")
        {
            MessageBox.Show(message + " Please contact your system administrator.", caption, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void SuccessMessage(string message, string caption = "Record changed")
        {
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void InvalidMessage(string message, string caption = "Please try again")
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
                InvalidMessage(combinedMessage);
            }
        }

        public static bool QuestionYesNo(string message, string caption = "Continue?")
        {
            MessageBoxResult answer = MessageBox.Show(message, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return (answer == MessageBoxResult.Yes);
        }

    } // class
} // namespace
