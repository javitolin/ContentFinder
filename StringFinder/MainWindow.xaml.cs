using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using log4net;
using StringFinder.Finders;

namespace StringFinder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MainWindow));

        // User Input
        private string _searchDirectory;
        private string _searchTermText;
        private bool _isRegex;
        private string _fileExtensionText;
        private string _outputDirectoryRoot;

        // Private Members
        private List<IFinder> _iFinders;
        private Dictionary<string, IFinder> _iFindersDictionary;
        private Dictionary<IFinder, List<string>> _iFinderToFileName;
        private int _totalNumberOfFilesToSearch;
        private int _totalNumberOfFilesSoFar = 0;

        public MainWindow()
        {
            Logger.Info("Starting new instance of Content Finder");
            InitializeComponent();
            progressBar.Visibility = Visibility.Hidden;
            _iFindersDictionary = new Dictionary<string, IFinder>();
            _iFinderToFileName = new Dictionary<IFinder, List<string>>();
        }

        [STAThread]
        public async void Run()
        {
            //Create IFinder List
            _iFinders = new List<IFinder>
            {
                new WordFinder(_isRegex),
                new ExcelFinder(_isRegex),
                new TxtFinder(_isRegex)
            };

            await Task.Run(() =>
            {
                try
                {
                    //Get total count of files to show progress
                    _totalNumberOfFilesToSearch =
                        Directory.GetFiles(_searchDirectory, "*", SearchOption.AllDirectories).Length;
                    SearchFiles(_searchDirectory); //Search all files and divide into dictionary

                    Logger.Info("Finished separating files into dictonaries");
                    _totalNumberOfFilesSoFar = 0; //Restart counter

                    Dispatcher.Invoke(() => { progressBar.Value = 0; });

                    RunFinders(_searchTermText, _outputDirectoryRoot); //Run the finders according to the extension
                    MessageBox.Show("Finished!");
                }
                catch (Exception e1)
                {
                    Logger.Error("Error caught running main process", e1);
                    MessageBox.Show(
                        "An error has occurred. Please try again. If the error persist, please contant an Administrator");
                }
            });

            progressBar.Value = 0;
            progressBar.Visibility = Visibility.Hidden;
        }

        private void RunFinders(string searchRegexText, string outputDirectoryRoot)
        {
            foreach (KeyValuePair<IFinder, List<string>> keyValuePair in _iFinderToFileName)
            {
                IFinder finder = keyValuePair.Key;
                List<string> filesToSearchIn = keyValuePair.Value;
                foreach (string filename in filesToSearchIn)
                {
                    bool? textFound = false;

                    Dispatcher.Invoke(() => { textFound = finder?.Search(filename, searchRegexText); });
                    if (textFound.HasValue && textFound.Value)
                    {
                        finder.FileFound(filename, outputDirectoryRoot);
                    }

                    _totalNumberOfFilesSoFar++;
                    Dispatcher.Invoke(() =>
                    {
                        progressBar.Value = ((double) _totalNumberOfFilesSoFar / _totalNumberOfFilesToSearch) * 100;
                    });
                }
            }
        }

        private void SearchFiles(string searchDirectory)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(searchDirectory);
            Regex fileExtensionRegex = new Regex(_fileExtensionText);
            foreach (FileInfo fileInfo in directoryInfo.GetFiles())
            {
                string extension = fileInfo.Extension.Replace(".","");
                if (!fileExtensionRegex.IsMatch(extension)) continue;

                IFinder finder = GetFinder(extension);
                if (!_iFinderToFileName.ContainsKey(finder))
                {
                    _iFinderToFileName.Add(finder, new List<string>());
                }

                _iFinderToFileName[finder].Add(fileInfo.FullName);

                _totalNumberOfFilesSoFar++;
                Dispatcher.Invoke(() =>
                {
                    progressBar.Value = ((double)_totalNumberOfFilesSoFar / _totalNumberOfFilesToSearch) * 100;
                });

                Logger.Debug($"Search so far {_totalNumberOfFilesSoFar} of {_totalNumberOfFilesToSearch}");
            }

            foreach (DirectoryInfo directory in directoryInfo.GetDirectories())
            {
                // Continue to next directory
                SearchFiles(directory.FullName);
            }
        }

        private IFinder GetFinder(string extension)
        {
            if (!_iFindersDictionary.ContainsKey(extension))
            {
                IFinder suitableIFinder = _iFinders.FirstOrDefault(m => m.CanSearch(extension));
                _iFindersDictionary.Add(extension, suitableIFinder);
            }

            return _iFindersDictionary[extension];
        }

        private void inputDirectoryBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    inputDirectoryTxt.Text = dialog.SelectedPath;
                }
            }
        }

        private void outputDirectoryBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    outputDirectoryTxt.Text = dialog.SelectedPath;
                }
            }
        }

        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(inputDirectoryTxt.Text))
            {
                Logger.Error("Please enter input directory");
                MessageBox.Show("Please enter input directory");
                return;
            }

            if (string.IsNullOrWhiteSpace(outputDirectoryTxt.Text))
            {
                Logger.Error("Please enter output directory");
                MessageBox.Show("Please enter output directory");
                return;
            }

            if (string.IsNullOrWhiteSpace(searchTermTxt.Text))
            {
                Logger.Error("Please enter search term");
                MessageBox.Show("Please enter search term");
                return;
            }

            if (string.IsNullOrWhiteSpace(fileExtensionsTxt.Text))
            {
                Logger.Error("Please enter file extensions, in Regex-type");
                MessageBox.Show("Please enter file extensions, in Regex-type");
                return;
            }

            //All good
            _searchDirectory = inputDirectoryTxt.Text;
            _outputDirectoryRoot = outputDirectoryTxt.Text;
            _searchTermText = searchTermTxt.Text;
            _fileExtensionText = fileExtensionsTxt.Text;
            _isRegex = regexChkBox.IsChecked.Value;

            if (!Directory.Exists(_searchDirectory))
            {
                Logger.Error("Input directory doesn't exist!");
                MessageBox.Show("Input directory doesn't exist!");
                return;
            }

            progressBar.Visibility = Visibility.Visible;

            SetAllElements(false);
            try
            {
                Logger.Info("Starting main run");
                Run();
            }
            catch (Exception e1)
            {
                Logger.Error("Caught exception running Run function", e1);
            }
            finally
            {
                SetAllElements(true);
            }

        }

        private void SetAllElements(bool isEnabled)
        {
            inputDirectoryTxt.IsEnabled = isEnabled;
            inputDirectoryBtn.IsEnabled = isEnabled;
            searchTermTxt.IsEnabled = isEnabled;
            regexChkBox.IsEnabled = isEnabled;
            fileExtensionsTxt.IsEnabled = isEnabled;
            outputDirectoryTxt.IsEnabled = isEnabled;
            outputDirectoryBtn.IsEnabled = isEnabled;
            progressBar.IsEnabled = isEnabled;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var iFinder in _iFinders)
            {
                iFinder.Close();
            }
        }
    }
}
