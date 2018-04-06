using System;
using System.IO;
using System.Text.RegularExpressions;
using log4net;

namespace StringFinder.Finders
{
    public class TxtFinder : IFinder
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TxtFinder));

        public TxtFinder(bool isRegex)
        {
            string wordRegex = @".*"; //ALL
            Logger.Debug($"Txt regex: {wordRegex}");
            _extensionRegex = new Regex(wordRegex, RegexOptions.IgnoreCase);
            _searchRegex = isRegex;
        }

        public override bool Search(string fullFileName, string searchTerm)
        {
            try
            {
                string allText = File.ReadAllText(fullFileName);
                if (_searchRegex)
                {
                    Regex searchRegex = new Regex(searchTerm);
                    return searchRegex.IsMatch(allText);
                }
                else
                {
                    return allText.ToLower().Contains(searchTerm.ToLower());
                }
            }
            catch (Exception e1)
            {
                Logger.Error($"Error reading file {fullFileName}", e1);
                return false;
            }
        }

        public override void Close()
        {
            //Not needed
        }
    }
}