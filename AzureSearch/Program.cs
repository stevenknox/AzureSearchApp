using System;
using System.Threading.Tasks;
using static System.Console;
using static System.ConsoleColor;

namespace AzureSearch
{
    class Program
    {
        static async Task Main(string[] args)
        {
            WriteLine(@"

       _                            ____                  _ _   _           
      / \    _____   _ _ __ ___    / ___|___   __ _ _ __ (_) |_(_)_   _____ 
     / _ \  |_  / | | | '__/ _ \  | |   / _ \ / _` | '_ \| | __| \ \ / / _ \
    / ___ \  / /| |_| | | |  __/  | |__| (_) | (_| | | | | | |_| |\ V /  __/
   /_/   \_\/___|\__,_|_|  \___|   \____\___/ \__, |_| |_|_|\__|_| \_/ \___|
                                              |___/                         
                        ____                      _     
                       / ___|  ___  __ _ _ __ ___| |__  
                       \___ \ / _ \/ _` | '__/ __| '_ \ 
                        ___) |  __/ (_| | | | (__| | | |
                       |____/ \___|\__,_|_|  \___|_| |_|                          
");
            try
            {
                await Run();
            }
            catch (Exception ex)
            {
                ForegroundColor = Red;
                WriteLine($"Something went wrong - {ex.ToString()}");
                ResetColor();
                WriteLine($"Press any key to return to main menu");
                ReadKey();
                await Run();
            }
            
        }

        private static async Task Run()
        {
            ForegroundColor = Cyan;
            WriteLine($@"
-----SEARCH-----

1. Search

-----MANAGE-----

2. Index All
3. Index Text/Objects
4. Index Files
5. Index Media

-----CONTENT-----

6. Upload and Analyse Media
7. Upload File

-----DELETE-----

99. Delete Indexes, Sources and Skills

Select option to continue:");

            ResetColor();

            switch (ReadLine())
            {
                case "1":
                    SearchService
                            .Create()
                            .StartSearch();
                    break;
                case "2":
                    await TextSearch
                            .Create()
                            .Index();
                    await FileSearch
                            .Create()
                            .Index();
                    await MediaSearch
                            .Create()
                            .Index();
                    break;
                case "3":
                    await TextSearch
                            .Create()
                            .Index();
                    break;
                case "4":
                    await FileSearch
                            .Create()
                            .Index();
                    break;
                case "5":
                    await MediaSearch
                            .Create()
                            .Index();
                    break;
                case "6":
                    await MediaSearch
                            .Create()
                            .AnalyseMediaAssets();
                    break;
                case "7":
                    await FileSearch
                            .Create()
                            .UploadFileToStorage();
                    break;
                case "99":
                    await FileSearch
                            .Create()
                            .Delete();
                    break;
                default:
                    WriteLine("Invalid option selected");
                    break;
            }

            WriteLine($"Press any key to return to main menu");
            ReadKey();
            await Run();
        }
    }
}
