using AdsAgregator.CommonModels.Models;
using Tables = AdsAgregator.DAL.Database.Tables;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdsAgregator.DAL.Database.Tables;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.Configuration;

namespace SearchEngine.EbayDe
{
    class EbayDeSearch
    {
        private EbayDeParser _searchClient;
        private ApplicationUser _user;
        private string apiUrl = ConfigurationManager.AppSettings["Api:url"];


        public Tables.SearchItem Searchitem { get; set; }

        public EbayDeSearch(Tables.SearchItem item, ApplicationUser owner)
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

            Console.WriteLine($"Last search responce was in {DateTime.Now.ToLongTimeString()}");

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
}
