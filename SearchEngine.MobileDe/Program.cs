using CommonEnums = AdsAgregator.CommonModels.Enums;
using CommonModels = AdsAgregator.CommonModels.Models;
using AdsAgregator.DAL.Database;
using AdsAgregator.DAL.Database.Tables;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using System.Threading;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Remote;

namespace SearchEngine.MobileDe
{
    class Program
    {
        static void Main(string[] args)
        {
        Begin:
            try
            {
                int counter = 0;

                var client = new MobileDeSearchEngine();

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
                        client = new MobileDeSearchEngine();
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



    public class MobileDeSearchEngine
    {
        private AppDbContext dbContext;
        private string apiUrl = "https://adsagregatorbackend20200429222631.azurewebsites.net/api/";

        public MobileDeSearchEngine()
        {
            dbContext = new AppDbContext();
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

              
                foreach (var item in searchItems)
                {

                    var options = new ChromeOptions();
                    options.PageLoadStrategy = PageLoadStrategy.Eager;
                    IWebDriver browser = new ChromeDriver(options);

                    browser.Navigate().GoToUrl(item.Url);

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


                    await Task.WhenAll(postResults);

                     browser.Close();
                }

            }
            catch (Exception ex)
            {

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
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

    public static class MobileDeParser
    {
        public static async Task<List<CommonModels.AdModel>> GetDataFromHtml(string rawHtml)
        {
            var document = new HtmlDocument();
            document.LoadHtml(rawHtml);

            IEnumerable<HtmlNode> nodes =
                document.DocumentNode.Descendants(0)
                .Where(n => n.HasClass("cBox-body--resultitem") || n.HasClass("cBox-body--eyeCatcher"));


            if (nodes is null | nodes.Count() == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("====================== UPDATE COOKIE ============================");
                Console.ResetColor();
            }

            var list = new List<CommonModels.AdModel>();


            foreach (var item in nodes)
            {
                list.Add(ParseAdItem(item));
            }

            return list;
        }

        private static CommonModels.AdModel ParseAdItem(HtmlNode node)
        {
            var document = new HtmlDocument();
            document.LoadHtml(node.InnerHtml);

            var adId = node.Descendants(0)
                .FirstOrDefault(n => n.ChildAttributes("data-ad-id").Count() > 0)
                ?.GetAttributeValue("data-ad-id", null);

            var imageLink = string.Empty;

            var adHref = node.Descendants(0)
                .FirstOrDefault(n => n.ChildAttributes("href").Count() > 0)
                ?.GetAttributeValue("href", null);


            var headlineBlock = node.Descendants(0)
                .FirstOrDefault(n => n.HasClass("headline-block"));

            var adTitle = headlineBlock.ChildNodes?.Count == 3 ?
                headlineBlock.ChildNodes[1].InnerText
                : headlineBlock.ChildNodes[0].InnerText;



            var adLink = adHref;

            var carInfo = node.Descendants(0)
                .FirstOrDefault(n => n.HasClass("rbt-regMilPow")).InnerText;

            var locationInfo = string.Empty; //node.Descendants(0)
                                             //    .Where(n => n.HasClass("g-col-10")).Last().InnerText;

            var priceInfo = node.Descendants(0)
                .FirstOrDefault(n => n.HasClass("price-block")).InnerText;



            var adDateCreated = string.Empty;


            var model = new CommonModels.AdModel()
            {
                ProviderAdId = HttpUtility.HtmlDecode(adId),
                AdTitle = HttpUtility.HtmlDecode(adTitle),
                CarInfo = HttpUtility.HtmlDecode(carInfo),
                ImageLink = HttpUtility.HtmlDecode(imageLink),
                PriceInfo = HttpUtility.HtmlDecode(priceInfo),
                AdSource = CommonEnums.AdSource.MobileDe,
                AddressInfo = HttpUtility.HtmlDecode(locationInfo),
                CreatedAtInfo = HttpUtility.HtmlDecode(adDateCreated),
                AdLink = adLink
            };


            return model;
        }
    }
}
