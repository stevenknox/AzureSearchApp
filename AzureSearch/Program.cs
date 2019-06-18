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

1. Search All
2. Search Text
3. Search Files
4. Search Media
5. Search Generic

-----MANAGE-----

6. Index Text
7. Index Files
8. Index Media
9. Index Generic

-----CONTENT-----

10. Upload and Analyse Media
11. Upload File

-----DELETE-----

99. Delete Indexes, Sources and Skills

Select option to continue:");

            ResetColor();

            switch (ReadLine())
            {
                case "1":
                    Search.All();
                    break;
                case "2":
                    TextSearch
                        .Create()
                        .Search();
                    break;
                case "3":
                    FileSearch
                        .Create()
                        .Search();
                    break;
                case "4":
                    MediaSearch
                        .Create()
                        .Search();
                    break;
                case "5":
                    GenericSearch
                        .Create()
                        .Search();
                    break;
                case "6":
                    await TextSearch
                            .Create()
                            .Index();
                    break;
                case "7":
                    await FileSearch
                            .Create()
                            .Index();
                    break;
                case "8":
                    await MediaSearch
                            .Create()
                            .Index();
                    break;
                case "9":
                    await GenericSearch
                            .Create()
                            .Index();
                    break;
                case "10":
                    await MediaSearch
                            .Create()
                            .AnalyseMediaAssets();
                    break;
                case "11":
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
