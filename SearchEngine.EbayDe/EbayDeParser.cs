using AdsAgregator.CommonModels.Enums;
using AdsAgregator.CommonModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using HtmlAgilityPack;
using SearchEngine.Utilities;

namespace SearchEngine.EbayDe
{
    public static class EbayDeParser
    {
        public static List<AdModel> GetDataFromHtml(string rawHtml)
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

        private static AdModel ParseAdItem(HtmlNode node)
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

            var Price = node.Descendants(0)
                .FirstOrDefault(n => n.HasClass("aditem-main--middle--price"))
                .InnerText;

            var address= node.Descendants(0)
                .FirstOrDefault(n => n.HasClass("aditem-main--top--left"))
                .InnerText;


            var adDateCreated = node.Descendants(0)
                .FirstOrDefault(n => n.HasClass("aditem-main--top--right"))
                .InnerText;


            adDateCreated = string.Join(" ", adDateCreated.RemoveEscapes()
                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            var model = new AdModel()
            {
                ProviderAdId = adId,
                AdTitle = adTitle,
                CarInfo = string.Join(" ", carInfo),
                ImageLink = imageLink == null ? imageLink : imageLink.Replace(" ", string.Empty),
                PriceInfo = Price.Trim(),
                AdSource = AdSource.Ebay,
                AddressInfo = address.Trim(),
                CreatedAtInfo = adDateCreated.Trim(),
                AdLink = adLink == null ? adLink : adLink.Replace(" ", string.Empty)
            };


            return model;
        }
    }
}
