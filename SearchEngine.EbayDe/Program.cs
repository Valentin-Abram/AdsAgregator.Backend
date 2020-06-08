using AdsAgregator.CommonModels.Enums;
using AdsAgregator.CommonModels.Models;
using AdsAgregator.DAL.Database;
using Tables = AdsAgregator.DAL.Database.Tables;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using AdsAgregator.DAL.Database.Tables;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;
using HtmlAgilityPack;
using SearchEngine.Utilities;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
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
                while (true)
                {
                    SearchEngine.MakeSearch().GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                goto Begin;
            }

            Console.ReadLine();
            Console.ReadLine();
        }

    }

    static class SearchEngine
    {
        public static List<Search> Searches { get; set; } = new List<Search>();
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

                Searches.Add(new Search(item, owner));
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
        public static async Task MakeSearch()
        {
            await UpdateSearchList();

            foreach (var item in Searches)
            {
                await item.ProcessSearch();
            }
        }

    }




    class Search
    {
        private EbayDeParser _searchClient;
        private ApplicationUser _user;
        private string apiUrl = "https://adsagregatorbackend20200429222631.azurewebsites.net/api/";


        public Tables.SearchItem Searchitem { get; set; }

        public Search(Tables.SearchItem item, ApplicationUser owner)
        {
            this.Searchitem = item;
            _searchClient = new EbayDeParser { _searchUrl = Searchitem.Url };
            this._user = owner;
        }

        public void Update(Tables.SearchItem searchItem)
        {
            this.Searchitem = searchItem;
        }


        public async Task ProcessSearch()
        {
            var ads = new List<AdModel>();
            try
            {
                ads = await _searchClient.GetAds();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"|||||||||||||| Got {ads.Count} ads from page ||||||||||||||||||");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

            if (ads?.Count == 0)
                return;

            var list = new List<Ad>();

            foreach (var resultItem in ads)
            {
                list.Add(new Ad
                {
                    OwnerId = Searchitem.OwnerId,
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

            if (ads.Count > 0)
            {
                await PostAds(_user.Id.ToString(), list);
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

    }

    public enum SearchEngineStatus
    {
        On = 1,
        Off = 2
    }


    public class EbayDeParser
    {
        public string _searchUrl;

        public async Task<List<AdModel>> GetAds()
        {
            var data = await RequestHtmlData(_searchUrl);
            var items = GetDataFromHtml(data);

            return items;
        }

        private async Task<string> RequestHtmlData(string url)
        {
            string res = string.Empty;

            try
            {

                IWebDriver browser;

                var options = new ChromeOptions();
                options.PageLoadStrategy = PageLoadStrategy.Eager;


                browser = new ChromeDriver(options);

                browser.Navigate().GoToUrl(url);

                res = browser.PageSource;

                if (res.Contains("Access denied"))
                    browser.Navigate().GoToUrl("https://www.ebay-kleinanzeigen.de/");

                ActLikeHuman(browser);

                browser.Close();

                Thread.Sleep(120000);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

            return res;
        }

        private Task<bool> ActLikeHuman(IWebDriver driver)
        {
            try
            {
                var random = new Random();

                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

                var steps = random.Next(3, 5);

                //js.ExecuteScript("var button = document.getElementById('gdpr-consent-accept-button'); if(button != null){button.click();}");


                //if (random.Next(1, 4) % 2 == 0)
                //    js.ExecuteScript("document.getElementsByClassName('cBox--resultList')[0].click()");

                for (int i = 0; i < steps; i++)
                {
                    js.ExecuteScript($"window.scroll(0, {random.Next(0, i * 1000)})");

                    Thread.Sleep(random.Next(1000, 2000));
                }

                // js.ExecuteScript("document.getElementsByClassName('cBox--resultList')[0].click()");
            }
            catch (Exception ex)
            {
                return Task.FromResult(false);
            }


            return Task.FromResult(true);

        }


        private List<AdModel> GetDataFromHtml(string rawHtml)
        {
            var document = new HtmlDocument();
            document.LoadHtml(rawHtml);

            IEnumerable<HtmlNode> nodes =
                document.DocumentNode.Descendants(0)
                .Where(n => n.HasClass("lazyload-item") && !n.HasClass("badge-topad"));


            if (nodes is null | nodes.Count() == 0)
                return new List<AdModel>();

            var list = new List<AdModel>();

            foreach (var item in nodes)
            {
                list.Add(ParseAdItem(item));
            }

            return list;
        }

        private AdModel ParseAdItem(HtmlNode node)
        {
            var document = new HtmlDocument();
            document.LoadHtml(node.InnerHtml);

            var adId = node.Descendants(0)
                .FirstOrDefault(n => n.ChildAttributes("data-adid").Count() > 0)
                ?.GetAttributeValue("data-adid", null);

            var imageLink = node.Descendants(0)
                .FirstOrDefault(n => n.ChildAttributes("data-imgsrc").Count() > 0)
                ?.GetAttributeValue("data-imgsrc", null);

            var adHref = node.Descendants(0)
                .FirstOrDefault(n => n.HasClass("ellipsis"));

            var adTitle = adHref.InnerText;

            var adLink = $"https://www.ebay-kleinanzeigen.de {adHref.GetAttributeValue("href", null)}";

            var carInfo = node.Descendants(0)
                .Where(n => n.HasClass("simpletag"))
                .Select(n => n.InnerText);

            var PriceAndLocation = node.Descendants(0)
                .FirstOrDefault(n => n.HasClass("aditem-details"))
                .InnerText
                .Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();


            for (int i = 0; i < PriceAndLocation.Count; i++)
            {
                PriceAndLocation[i] = PriceAndLocation[i].Trim();
            }

            var adDateCreated = node.Descendants(0)
                .FirstOrDefault(n => n.HasClass("aditem-addon"))
                .InnerText;

            adDateCreated = string.Join(" ", adDateCreated.RemoveEscapes()
                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            var model = new AdModel()
            {
                ProviderAdId = adId,
                AdTitle = adTitle,
                CarInfo = string.Join(" ", carInfo),
                ImageLink = imageLink,
                PriceInfo = string.Join(" ", PriceAndLocation.Take(PriceAndLocation.Count - 2)),
                AdSource = AdSource.Ebay,
                AddressInfo = string.Join(" ", PriceAndLocation.Skip(PriceAndLocation.Count - 2).Take(2)),
                CreatedAtInfo = adDateCreated,
                AdLink = adLink
            };


            return model;
        }

    }
}
