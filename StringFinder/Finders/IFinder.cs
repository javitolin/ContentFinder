using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using log4net;

namespace StringFinder.Finders
{
    public abstract class IFinder
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(IFinder));

        protected Regex _extensionRegex;
        protected bool _searchRegex;

        /// <summary>
        /// Search for the @searchRegex in the files in @fileFullNamesList, output to @outputDirectoryRoot
        /// </summary>
        /// <param name="fullFileName">File to search in</param>
        /// <param name="searchTerm">The string to search for</param>
        /// <returns>True if the @searchRegex was found. False otherwise</returns>
        public abstract bool Search(string fullFileName, string searchTerm);

        /// <summary>
        /// Close the session
        /// </summary>
        public abstract void Close();

        /// <summary>
        /// Returns the iFinder name
        /// </summary>
        /// <returns></returns>
        public abstract string GetName();

        /// <summary>
        /// </summary>
        /// <param name="fileExtension"></param>
        /// <returns>Returns true if this class can search this type of extension</returns>
        public bool CanSearch(string fileExtension)
        {
            return _extensionRegex.IsMatch(fileExtension);
        }

        public void FileFound(string fullFileName, string outputDirectoryRoot)
        {
            try
            {
                if (!Directory.Exists(outputDirectoryRoot))
                {
                    Directory.CreateDirectory(outputDirectoryRoot);
                }

                string fileName = Path.GetFileName(fullFileName);
                string copyFileName = Path.Combine(outputDirectoryRoot, fileName);
                if (File.Exists(copyFileName))
                {
                    Logger.Warn($"File {copyFileName} already exists. Not copying");
                }
                else
                {
                    File.Copy(fullFileName, copyFileName);
                }


                Logger.Info($"File {fullFileName} copied to directory {outputDirectoryRoot}");
            }
            catch (Exception e1)
            {
                Logger.Error($"Error copying file {fullFileName} to directory {outputDirectoryRoot}. Please copy manually", e1);
            }
        }
    }
}