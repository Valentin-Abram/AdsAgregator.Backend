using CommonEnums = AdsAgregator.CommonModels.Enums;
using AdsAgregator.DAL.Database;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AdsAgregator.DAL.Database.Tables;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading;

namespace SearchEngine.EbayDe
{
    public class EbayDeSearchEngine
    {
        private AppDbContext dbContext = new AppDbContext();
        private string apiUrl = "http://localhost:51878";

        public EbayDeSearchEngine()
        {


        }


 
       

        public async Task ProcessSearch()
        {
            var searchItems = await GetActiveSearches();
    
            var browser = GetChromeBrowser();

            foreach (var item in searchItems)
            {
                browser.Navigate().GoToUrl(item.Url);

                var content = browser.PageSource;
                var resultList = EbayDeParser.GetDataFromHtml(content);

                var list = new List<Ad>();

                foreach (var resultItem in resultList)
                {
                    list.Add(new Ad
                    {
                        OwnerId = item.OwnerId,
                        AddressInfo = resultItem.AddressInfo,
                        AdLink = resultItem.AdLink,
                        AdSource = resultItem.AdSource,
                        AdTitle = resultItem.AdTitle,
                        CarInfo = resultItem.CarInfo,
                        CreatedAtInfo = resultItem.CreatedAtInfo,
                        Email = resultItem.Email,
                        ImageLink = resultItem.ImageLink,
                        Phone = resultItem.Phone,
                        PriceInfo = resultItem.PriceInfo,
                        ProviderAdId = resultItem.ProviderAdId
                    });
                }

                var postResults =
                    PostAdsToDb(list);

                browser.Quit();

                if (list.Count == 0)
                    return;
                else if (list.Count > 0)
                    await Task.WhenAll(postResults);
            }

        }



        private async Task<HttpStatusCode> PostAds(string userId, List<Ad> ads)
        {
            var httpClient = new HttpClient();
            var parameters = new Dictionary<string, string>()
                {
                    { "userId", userId },
                    { "ads", JsonConvert.SerializeObject(ads)},
                };

            var encodedContent = new FormUrlEncodedContent(parameters);

            var response = await httpClient.PostAsync($"{apiUrl}/ads/postads", encodedContent);

            return response.StatusCode;
        }

        private async Task<int> PostAdsToDb(List<Ad> ads)
        {
            var dbContext = new AppDbContext();

            foreach (var item in ads)
            {
                var result = await dbContext
                    .Ads
                    .Where(ad =>
                        ad.ProviderAdId == item.ProviderAdId && ad.OwnerId == item.OwnerId && ad.AdSource == item.AdSource)
                    .FirstOrDefaultAsync();

                if (result is null)
                {
                    dbContext.Ads.Add(item);
                }

            }

            return await dbContext.SaveChangesAsync();
        }

        private async Task<List<SearchItem>> GetActiveSearches()
        {
            return await dbContext.SearchItems
                .Where(si => si.AdSource == CommonEnums.AdSource.Ebay && si.IsActive)
                .ToListAsync();
        }


        private IWebDriver GetChromeBrowser()
        {
            var options = new ChromeOptions();
            options.PageLoadStrategy = PageLoadStrategy.Eager;

            return new ChromeDriver(options);
        }

        private Task<bool> ActLikeHuman(IWebDriver driver)
        {
            try
            {
                var random = new Random();

                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

                js.ExecuteScript("var button = document.getElementById('gdpr-consent-accept-button'); if(button != null){button.click();}");

                var steps = random.Next(3, 5);

                if (random.Next(1, 4) % 2 == 0)
                    js.ExecuteScript("document.getElementsByClassName('cBox--resultList')[0].click()");

                for (int i = 0; i < steps; i++)
                {
                    js.ExecuteScript($"window.scroll(0, {random.Next(0, i * 1000)})");

                    Thread.Sleep(random.Next(1000, 2000));
                }

                js.ExecuteScript("document.getElementsByClassName('cBox--resultList')[0].click()");
            }
            catch (Exception ex)
            {
                return Task.FromResult(false);
            }


            return Task.FromResult(true);

        }

        private List<SearchItem> RandomizeSearchList(List<SearchItem> searchItems)
        {
            var random = new Random();
            var steps = random.Next(5, 50);

            for (int i = 0; i < steps; i++)
            {
                var p1 = random.Next(0, searchItems.Count - 1);
                var p2 = random.Next(0, searchItems.Count - 1);

                var a = searchItems[p1];
                var b = searchItems[p2];

                searchItems[p1] = b;
                searchItems[p2] = a;
            }

            return searchItems;
        }
    }
}
