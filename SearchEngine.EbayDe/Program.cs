using System;

namespace SearchEngine.EbayDe
{
    class Program
    {
        static void Main(string[] args)
        {
            Begin:
            try
            {
                EbayDeSearchEngine.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                goto Begin;
            }

            Console.ReadLine();
        }

    }
}
