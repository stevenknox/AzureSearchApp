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
7. Index Media
8. Index Generic
9. Processes Media

Select option to continue:");

            ResetColor();

            switch (ReadLine())
            {
                case "1" : Search.All();
                break;
                case "2" : TextSearch.Create().Search();
                break;
                case "3" : FileSearch.Create().Search();
                break;
                case "4" : MediaSearch.Create().Search();
                break;
                case "5" : GenericSearch.Create().Search();
                break;
                case "6" : await TextSearch.Create().Index();
                break;
                case "7" : await MediaSearch.Create().Index();
                break;
                case "8" : await GenericSearch.Create().Index();
                break;
                case "9" : await MediaSearch.Create().IndexMediaAssets();
                break;
                case "10" : await FileSearch.Create().UploadFileToStorage();
                break;
                default: WriteLine("Invalid option selected");
                break;
            }

            ReadKey();
        }

    }
}
