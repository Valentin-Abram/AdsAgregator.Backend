using AdsAgregator.CommonModels.Enums;
using AdsAgregator.DAL.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace SearchEngine.EbayDe
{
    static class EbayDeSearchEngine
    {
        private static Timer _timer;
        private static int INTERVAL = 10000;

        public static List<EbayDeSearch> Searches { get; set; } = new List<EbayDeSearch>();


        private static async Task UpdateSearchList()
        {
            var dbContext = new AppDbContext();

            var searchItemsFromDb = await dbContext
                .SearchItems
                .Where(s => s.IsActive == true && s.AdSource == AdSource.Ebay)
                .ToListAsync();


            var newItems = searchItemsFromDb
                .Where(sdb => Searches.Select(s => s.Searchitem.Id).Contains(sdb.Id) == false);


            foreach (var item in newItems)
            {
                var owner = await dbContext.Users.FindAsync(item.OwnerId);

                Searches.Add(new EbayDeSearch(item, owner));
            }

            var itemsToRemove = Searches
                .Where(s => searchItemsFromDb.Select(ni => ni.Id).Contains(s.Searchitem.Id) == false);

            foreach (var item in itemsToRemove)
            {
                Searches.Remove(item);
            }

            foreach (var item in searchItemsFromDb)
            {
                var itemToUpdate = Searches.FirstOrDefault(s => s.Searchitem.Id == item.Id);
                itemToUpdate.Update(item);
            }

        }

        public static async void Start()
        {
            await UpdateSearchList();
            await MakeSearch();

            _timer = new Timer(INTERVAL);
            _timer.Elapsed += OnTimerClick;
            _timer.Start();

        }

        public static void Stop()
        {
            _timer?.Stop();
        }

        private static async void OnTimerClick(object sender, ElapsedEventArgs e)
        {
            await UpdateSearchList();
            try
            {
                await MakeSearch();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static async Task MakeSearch()
        {
            var taskList = new List<Task>();

            foreach (var item in Searches)
            {
                taskList.Add(Task.Run(async () => await item.ProcessSearch()));
            }

            await Task.WhenAll(taskList);
        }

      
    }
}
