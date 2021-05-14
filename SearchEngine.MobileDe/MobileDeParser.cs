using CommonEnums = AdsAgregator.CommonModels.Enums;
using CommonModels = AdsAgregator.CommonModels.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace SearchEngine.MobileDe
{
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
