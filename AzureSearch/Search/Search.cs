using System;
using System.Diagnostics;
using System.Linq;
using static System.Console;
using static System.ConsoleColor;

namespace AzureSearch
{
    public class Search
    {
        private static readonly int indexOffset = 1;
        public static void All()
        {
            WriteLine($"Enter Your Search Query:");

            var query = ReadLine();

            var textResults = TextSearch.Create()
                                        .Search(query)
                                        .ToCombinedSearch();

            var fileResults = FileSearch.Create()
                                        .Search(query)
                                        .ToCombinedSearch();

            var mediaResults = MediaSearch.Create()
                                          .Search(query)
                                          .ToCombinedSearch();

            var allResults = textResults.Concat(fileResults)
                                        .Concat(mediaResults)
                                        .ApplyIndex(startingIndex: indexOffset)
                                        .ToList();

            ForegroundColor = Green;

            var resultsCount = allResults.Count;

            WriteLine($"{Environment.NewLine}{resultsCount} results found for : {query}{Environment.NewLine}");

            ResetColor();

            var table = allResults.ToStringTable(
                u => u.Id,
                u => u.Name,
                u => u.Type
            );

            WriteLine(table);

            WriteLine($"{Environment.NewLine}Enter an ID from the results to view, or enter 0 to start a new search");

            var input = ReadLine();

            if (Int32.TryParse(input, out int inputOption))
            {
                if (inputOption == 0)
                {
                    All();
                }
                else
                {
                    CombinedSearch item = allResults[inputOption - indexOffset];

                    if (item.SearchType == SearchType.File)
                    {
                        var file = FileSearch.Create().DownloadFile($"{Environment.CurrentDirectory}/Downloads", item.Name);

                        WriteLine($"Opening File from {file}");

                        try
                        {
                            Process.Start(@"cmd.exe ", $@"/c ""{file}""");
                        }
                        catch (System.ComponentModel.Win32Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                    else if (item.SearchType == SearchType.Media)
                    {
                        WriteLine($"Launching Video");

                        try
                        {
                            Process.Start(@"cmd.exe ",$@"/c ""{Environment.CurrentDirectory}/Media/Output/{item.Details}""");
                        }
                        catch (System.ComponentModel.Win32Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }


                    }
                    else if (item.SearchType == SearchType.Text)
                    {
                        WriteLine(item.Details);
                    }

                    All();
                }
            }
            else
            {
                All();
            }

        }
    }
}