using System;
using System.Threading.Tasks;
using static System.Console;
using static System.ConsoleColor;
using AzureSearch;
using System.IO;
using Newtonsoft.Json;

namespace ConsoleApp
{
    class Program
    {
        internal readonly static string AzureCredentialsPath = $"{Environment.CurrentDirectory}/../.azure-credentials";

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

            var apiKey = File.ReadAllText($"{AzureCredentialsPath}/search.private-azure-key"); 
            var mediaServicesAuth =  JsonConvert.DeserializeObject<MediaServicesAuth>(File.ReadAllText($"{AzureCredentialsPath}/media.private-azure-key")); 
            var storageKey = File.ReadAllText($"{AzureCredentialsPath}/storage.private-azure-key");
            var rootPath = "../AzureSearch";
            switch (ReadLine())
            {
                case "1":
                    SearchService
                            .Create(apiKey, storageKey, mediaServicesAuth)
                            .StartSearch();
                    break;
                case "2":
                    await TextSearch
                            .Create(apiKey, rootPath)
                            .Index();
                    await FileSearch
                            .Create(apiKey, storageKey, rootPath)
                            .Index();
                    await MediaSearch
                            .Create(apiKey, mediaServicesAuth, rootPath)
                            .Index();
                    break;
                case "3":
                    await TextSearch
                            .Create(apiKey, rootPath)
                            .Index();
                    break;
                case "4":
                    await FileSearch
                            .Create(apiKey, storageKey, rootPath)
                            .Index();
                    break;
                case "5":
                    await MediaSearch
                            .Create(apiKey, mediaServicesAuth, rootPath)
                            .Index();
                    break;
                case "6":
                    await MediaSearch
                            .Create(apiKey, mediaServicesAuth, rootPath)
                            .AnalyseMediaAssets();
                    break;
                case "7":
                    await FileSearch
                            .Create(apiKey, storageKey, rootPath)
                            .UploadFileToStorage();
                    break;
                case "99":
                    await FileSearch
                            .Create(apiKey, storageKey, rootPath)
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
