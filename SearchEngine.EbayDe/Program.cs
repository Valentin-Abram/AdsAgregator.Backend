using System;
using System.Threading;

namespace SearchEngine.EbayDe
{
    class Program
    {
        static void Main(string[] args)
        {
        Begin:
            try
            {
                int counter = 0;

                var client = new EbayDeSearchEngine();

                while (true)
                {
                    client
                        .ProcessSearch()
                        .GetAwaiter()
                        .GetResult();

                    counter++;
                    if (counter % 10 == 0)
                    {
                        Thread.Sleep(20000);
                        client = new EbayDeSearchEngine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                goto Begin;
            }
        }

    }
}
