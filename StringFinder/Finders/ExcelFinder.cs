using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using log4net;
using Excel = Microsoft.Office.Interop.Excel;

namespace StringFinder.Finders
{
    public class ExcelFinder : IFinder
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ExcelFinder));
        Excel.Application xlApp = new Excel.Application();

        public ExcelFinder(bool isRegex)
        {
            string wordRegex = @"(\bxls\b)|(\bxlsx\b)";
            Logger.Debug($"Excel regex: {wordRegex}");
            _extensionRegex = new Regex(wordRegex, RegexOptions.IgnoreCase);
            _searchRegex = isRegex;
        }

        public override bool Search(string fullFileName, string searchTerm)
        {
            Excel.Workbook xlWorkbook = null;
            Regex searchRegex = new Regex(searchTerm, RegexOptions.IgnoreCase);

            try
            {
                //Create COM Objects. Create a COM object for everything that is referenced
                xlWorkbook = xlApp.Workbooks.Open(fullFileName);
                bool isFound = false;
                for (int sheetIndex = 1; sheetIndex <= xlWorkbook.Sheets.Count && !isFound; sheetIndex++) //Actually starts from 1
                {
                    Excel._Worksheet xlWorksheet = xlWorkbook.Sheets[sheetIndex];
                    Excel.Range xlRange = xlWorksheet.UsedRange;

                    //Really ugly stuff here.
                    for (int rowIndex = 1; rowIndex <= xlRange.Rows.Count && !isFound; rowIndex++)
                    {
                        for (int columnIndex = 1; columnIndex <= xlRange.Columns.Count && !isFound; columnIndex++)
                        {
                            if (xlRange.Cells[rowIndex, columnIndex] != null 
                                && xlRange.Cells[rowIndex, columnIndex].Value2 != null)
                            {
                                string cellValue = xlRange.Cells[rowIndex, columnIndex].Value2.ToString();
                                //This cell contains something
                                if (_searchRegex)
                                {
                                    if (searchRegex.IsMatch(cellValue))
                                    {
                                        isFound = true;
                                    }
                                }
                                else
                                {
                                    if (cellValue.ToLower().Contains(searchTerm.ToLower()))
                                    {
                                        isFound = true;
                                    }
                                }
                            }
                        }
                    }

                    Marshal.ReleaseComObject(xlRange);
                    Marshal.ReleaseComObject(xlWorksheet);
                }

                return isFound;
            }
            catch (Exception e1)
            {
                Logger.Error($"Error searching text {searchTerm} in file {fullFileName}", e1);
                return false;
            }

            finally
            {
                if (xlWorkbook != null)
                {
                    //close and release
                    xlWorkbook.Close();
                    Marshal.ReleaseComObject(xlWorkbook);
                }
            }
        }

        public override void Close()
        {
            if (xlApp != null)
            {
                //quit and release
                xlApp.Quit();
                Marshal.ReleaseComObject(xlApp);
            }
        }
    }
}