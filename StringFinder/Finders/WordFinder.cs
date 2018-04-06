using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using log4net;
using Microsoft.Office.Interop.Word;
using Application = Microsoft.Office.Interop.Word.Application;

namespace StringFinder.Finders
{
    class WordFinder : IFinder
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(WordFinder));
        Application application = new Application();

        public WordFinder(bool isRegex)
        {
            string wordRegex = @"(\bdoc\b)|(\bdocx\b)";
            Logger.Debug($"Word regex: {wordRegex}");
            _extensionRegex = new Regex(wordRegex, RegexOptions.IgnoreCase);
            _searchRegex = isRegex;
        }

        public override bool Search(string fullFileName, string searchTerm)
        {
            Document document = null;
            Regex searchRegex = new Regex(searchTerm, RegexOptions.IgnoreCase);

            try
            {
                document = application.Documents.Open(fullFileName);
                document.ActiveWindow.Selection.WholeStory();
                document.ActiveWindow.Selection.Copy();
                IDataObject data = Clipboard.GetDataObject();
                if (data?.GetData(DataFormats.Text) == null)
                {
                    return false;
                }

                string currentWordFile = data.GetData(DataFormats.Text).ToString();
                return searchRegex.IsMatch(currentWordFile);
            }
            catch (Exception e1)
            {
                Logger.Error($"Error searching for {searchTerm} in {fullFileName}", e1);
                return false;
            }
            finally
            {
                document?.Close();
            }
        }

        public override void Close()
        {
            if (application != null)
            {
                //quit and release
                application.Quit();
                Marshal.ReleaseComObject(application);
            }
        }
    }
}
