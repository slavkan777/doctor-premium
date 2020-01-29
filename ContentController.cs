using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Gambling.DAL.Entities.Enums;
using Gambling.API.Models.ContentModels;
using Gambling.DAL.Data;
using Newtonsoft.Json;
using System.Net;
using System.IO;

namespace Gambling.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ContentController : ControllerBase
    {
        public IConfiguration Configuration { get; private set; }
        public GamblingDbContext GamblingDbContext { get; private set; }
        public IMapper Mapper { get; private set; }

        public ContentController(IConfiguration configuration,
            GamblingDbContext gamblingDbContext,
            IMapper mapper)
        {
            Configuration = configuration;
            GamblingDbContext = gamblingDbContext;
            Mapper = mapper;
        }

        /// <summary>
        /// API Method to get HomePage content
        /// </summary>
        [HttpGet("HomePage")]
        public HomePageDataContentModel HomePage()
        {
            HomePageDataContentModel result = new HomePageDataContentModel();

            try
            {
                var banners = GamblingDbContext.Banners;
                var categories = GamblingDbContext.Categories
                    .Include(cat => cat.CategoryGames)
                    .ThenInclude(cg => cg.Game)
                    .OrderBy(cat => cat.Priority)
                    .ToList();

                foreach (var item in categories)
                {
                    item.CategoryGames = item.CategoryGames
                        .Where(cg => cg.IsOnHomePage)
                        .OrderBy(cg => cg.Priority)
                        .Take(15)
                        .ToList();
                }

                GetContent(result, banners, categories, GamblingType.Casino);
                GetContent(result, banners, categories, GamblingType.LiveCasino);
            }
            catch
            {
                result.AddDefaultError();
            }

            return result;
        }

        [HttpGet("Countries")]
        public CountriesContentModel GetCountries()
        {
            CountriesContentModel result = new CountriesContentModel();

            try
            {
                var webClient = new WebClient();
                var path = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "Data/countries.json");
                var json = webClient.DownloadString(path);
                result.Content = JsonConvert
                    .DeserializeObject<List<CountryModel>>(json);
            }
            catch
            {
                result.AddDefaultError();
            }

            return result;
        }

        private void GetContent(HomePageDataContentModel result,
            DbSet<DAL.Entities.Content.Banner> banners,
            List<DAL.Entities.Gaming.Category> categories,
            GamblingType gamblingType)
        {
            var bannerModels = Mapper.Map<List<BannerModel>>(
                                    banners.Where(x => x.GamblingType == gamblingType)
                                    .OrderBy(x => x.Priority)
                                    .ToList());

            for (int i = 0; i < bannerModels.Count; i++)
            {
                bannerModels[i].Num = i + 1;
            }

            result.Content.Add(new GamblingTypeDataModel
            {
                GamblingType = gamblingType.ToString(),
                Banners = bannerModels,
                HomePageCategories = Mapper.Map<IEnumerable<HomePageCategoryModel>>(
                    categories.Where(x => x.GamblingType == gamblingType && x.IsOnHomePage)),
                Categories = Mapper.Map<IEnumerable<DefaultCategoryModel>>(
                    categories.Where(x => x.GamblingType == gamblingType))
            });
        }
    }
}
