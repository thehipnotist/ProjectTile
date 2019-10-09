using System;
using System.ComponentModel;
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
            addQuestion("Question", "Answer");
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
