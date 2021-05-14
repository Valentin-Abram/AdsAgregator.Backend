using CommonEnums = AdsAgregator.CommonModels.Enums;
using AdsAgregator.DAL.Database;
using AdsAgregator.DAL.Database.Tables;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using System.Threading;

namespace SearchEngine.MobileDe
{
    public class MobileDeSearchEngine
    {
        private AppDbContext dbContext = new AppDbContext();
        private IWebDriver browser;
        private string apiUrl = "https://adsagregator.azurewebsites.net/api/";

        public MobileDeSearchEngine()
        {
            var options = new ChromeOptions();
            options.PageLoadStrategy = PageLoadStrategy.Eager;

            browser = new ChromeDriver(options);

        }

        private async Task<List<SearchItem>> GetActiveSearches()
        {
            return await dbContext.SearchItems
                .Where(si => si.AdSource == CommonEnums.AdSource.MobileDe && si.IsActive == true)
                .ToListAsync();
        }

        public async Task ProcessSearch()
        {
            try
            {

                var searchItems = await GetActiveSearches();

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


                foreach (var item in searchItems)
                {

                    browser.Navigate().GoToUrl(item.Url);

                    //var fakeHuman = Task.CompletedTask; //ActLikeHuman(browser);

                    var content = browser.PageSource;
                    var resultList = await MobileDeParser.GetDataFromHtml(content);

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
                        PostAds(item.OwnerId.ToString(), list);



                    if (list.Count == 0)
                    {
                        browser.Quit();
                        browser = new ChromeDriver();

                        return;
                    }

                    browser.Quit();

                    var options = new ChromeOptions();
                    options.PageLoadStrategy = PageLoadStrategy.Eager;
                    browser = new ChromeDriver(options);

                    if(list.Count > 0)
                    await Task.WhenAll(postResults);
                }
            }
            catch (Exception ex)
            {

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                this.browser.Close();
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
    }
}
