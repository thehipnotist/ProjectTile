using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ProjectTile
{
    /// <summary>
    /// Interaction logic for FAQPage.xaml
    /// </summary>
    public partial class FAQPage : Page
    {
        // ---------------------- //
        // -- Global Variables -- //
        // ---------------------- //   

        // Global/page parameters //
        string pageMode;
        string newLine = "\n";
        FlowDocument thisDoc = new FlowDocument();          


        // Current variables //

        // Current records //

        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //

        // Initialize and Load //
        public FAQPage()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Page));
            KeepAlive = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                pageMode = PageFunctions.pageParameter(this, "Mode");
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                PageFunctions.ShowTilesPage();
            }

            thisDoc.SetResourceReference(FontFamilyProperty, "MainFont");
            thisDoc.FontSize = 12;
            thisDoc.PagePadding = new Thickness(0, 0, 15, 15);
            MainText.Document = thisDoc;

            addTileQuestions();
            addFunctionalityQuestions();
            addDevelopmentQuestions();
            addYourQuestions();
            
        }





        // ---------------------- //
        // -- Data Management --- //
        // ---------------------- //

        // Data updates //

        // Data retrieval //

        // Other/shared functions //
        private void addHeader(string headerText)
        {
            try
            {
                Paragraph para = new Paragraph();
                Span header = new Span();
                header.SetResourceReference(StyleProperty, "HeaderSpan");
                header.Inlines.Add(headerText);
                para.Inlines.Add(header);
                thisDoc.Blocks.Add(para);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error adding header text", generalException); }
        }
        
        private void addQuestion(string questionText, string answerText)
        {
            try
            {
                Paragraph para = new Paragraph();
                Span question = new Span();
                question.SetResourceReference(StyleProperty, "QuestionSpan");
                question.Inlines.Add(questionText + newLine);
                para.Inlines.Add(question);
                Span answer = new Span();
                answer.SetResourceReference(StyleProperty, "AnswerSpan");
                answer.Inlines.Add(answerText);
                para.Inlines.Add(answer);
                thisDoc.Blocks.Add(para);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error adding question", generalException); }
        }

        private void addTileQuestions()
        {
            addHeader("Tiles Page Questions");
            addQuestion("How can I stop other tiles from popping up unexpectedly when I want to click on a tile?",
                "Click on the main tile to 'pin' it, i.e. keep it expanded. For example if you want to add a new product, click the main 'Products' tile first, "
                + "then on 'Add Product'. See below for how to 'un-pin'");

            addQuestion("I've expanded a tile and now it's stuck - how do I reset it?",
                "Click anywhere in the background within the border around the tiles. Alternatively, if you want to expand another tile and can see it, you can "
                + "simply click on that tile instead.");      
        }

        private void addFunctionalityQuestions()
        {
            addHeader("General Functionality Questions");
            addQuestion("Why doesn't this include some standard Project Management functionality?", "The purpose of ProjectTile was to learn and demonstrate coding "
                + "skills, not to show my knowledge of Project Management. With infinite spare time I would add more functionality, but this is not designed as a "
                + "commercial application. I had to stop developing somewhere!");
            addQuestion("What is a key role?", "It is a project role that should always be filled during the project, and by only one person at a time - this avoids "
                + "ambiguity or gaps when working with the project, e.g. creating project documents that name the important personnel. This applies to the internal "
                + "Project Sponsor and Project Manager, for example - and the same roles for a client, if one is involved.");
            addQuestion("How do I manage (internal or client) project teams, when I can't overlap or delete key roles, or leave gaps?", "The functionality is designed "
                + "so that you can always get the result you need, though you may have to amend existing records first. For example, to effectively clear an existing record, "
                + "you can either amend it (change person) or create a new one that effectively replaces it (starts earlier and ends later, or at the same time). "
                + "If completely stuck, create a new record that lasts the full project duration so it clears all other records, then you can add others if needed "
                + "that last for part of the timespan, and the existing record will be truncated appropriately for them to fit.");
        }

        private void addDevelopmentQuestions()
        {
            addHeader("Development Questions");
            addQuestion("Did you write it all yourself?", "Very nearly (apart from code generated automatically by Visual Studio, naturally). Of course as this was "
                + "a learning exercise, I followed examples from books, websites etc., but I would rewrite any code in my own way to make sure I understood it rather "
                + "than simply directly 'lifting' the text. The one exception is the use of RNGCryptoServiceProvider with byte variables (to generate temporary "
                + "passwords for SSO users) where I followed an example from a forum very closely.");
            addQuestion("Did you try to follow MVVM principles?", "Not directly, as it didn't seem to fit well with the class structure created by the Entity Framework. "
                + "Also, I wanted to make the code reasonably 'portable' so that the business logic could (in theory) be plugged into a non-WPF application."
                + "I have used some of the binding techniques and followed some of the suggested structure, but haven't tried to fully implement MVVM."
                + "Hopefully I can pick this up in future with some further guidance.");
            addQuestion("Why doesn't this use the latest technology?", "I decided to build something that should work even on older machines, so that anyone can try it. "
                + "Sometimes that has meant disregarding the latest solutions as they wouldn't work in my version of C# or .NET, e.g. some asynchronous display "
                + "methods and using string interpolation.");
        }

        private void addYourQuestions()
        {
            addHeader("Your Questions");           
            try
            {
                Paragraph para = new Paragraph();
                Span question = new Span();
                question.SetResourceReference(StyleProperty, "QuestionSpan");
                question.Inlines.Add("How do I get in touch to find out more?" + newLine);
                para.Inlines.Add(question);
                
                Span answer1 = new Span();
                answer1.SetResourceReference(StyleProperty, "AnswerSpan");
                answer1.Inlines.Add("Email ");
                
                //Span hyperSpan = new Span();
                //Run hyperText = new Run("mrmarkajohnson@yahoo.co.uk");                
                Hyperlink hyperLink = new Hyperlink();
                hyperLink.Inlines.Add("mrmarkajohnson@yahoo.co.uk");
                hyperLink.NavigateUri = new Uri("mailto:mrmarkajohnson@yahoo.co.uk?subject=Regarding%20ProjectTile");
                //hyperLink.RequestNavigate += new System.Windows.Navigation.RequestNavigateEventHandler(requestNavigation);
                //hyperLink.Click += requestEmail;
                //hyperSpan.Inlines.Add(hyperText);
                
                Span answer2 = new Span();
                answer2.SetResourceReference(StyleProperty, "AnswerSpan");
                answer2.Inlines.Add(" with any further questions, comments or to get in touch regarding job opportunities.");
                
                para.Inlines.Add(answer1);
                para.Inlines.Add(hyperLink);
                para.Inlines.Add(answer2);
                thisDoc.Blocks.Add(para);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error adding question", generalException); }

        }


        // ---------------------- //
        // -- Event Management -- //
        // ---------------------- //

        // Generic (shared) control events //
        //private void requestNavigation(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        //{
        //    Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
        //    e.Handled = true;
        //}

        //private void requestEmail(object sender, RoutedEventArgs e)
        //{
        //    Uri mailUri = new Uri("mailto:mrmarkajohnson@yahoo.co.uk");
        //    Process.Start(new ProcessStartInfo(mailUri.AbsoluteUri));
        //    e.Handled = true;
        //}

        // Control-specific events //
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (Globals.MyStaffID > 0) { PageFunctions.ShowTilesPage(); }
            else { PageFunctions.ShowLoginPage(PageFunctions.LogIn); }
        }


    } // class
} // namespace
