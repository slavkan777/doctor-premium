using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Web;
using System.Web.Mvc;
using DoctorPremium.DAL.Model;
using DoctorPremium.Web.Helpers;
using DoctorPremium.Web.Models;
using DoctorPrmium.Services.Interfaces;
using Microsoft.AspNet.Identity;

namespace DoctorPremium.Web.Controllers
{
    public class HomeController : BaseController
    {
        private IRightMoneyDataService _RightMoneyService;
        private IHelpDataService _HelpDataService;

        public HomeController(IRightMoneyDataService RightMoneyService, IHelpDataService HelpDataService)
        {
            this._RightMoneyService = RightMoneyService;
            this._HelpDataService = HelpDataService;
        }

        public ActionResult Index(string value)
        {
            /*if (value != "1")
            {
             * 
                if (User.IsInRole("Doctor"))
                {
                    return RedirectToAction("Index", "Schedule");
                }
            }*/

            switch (LanguageHelper.GetCurrentLanguage())
            {
                case "ru":
                    return View("Index");
                default:
                    return View("IndexEn");
            }
        }

        public ActionResult PublicDoctorSite()
        {
            return View();
        }

        [ChildActionOnly]
        public ActionResult LayouLeftManyPartial()
        {
            RightMoneyDataModels model = new RightMoneyDataModels();
            if (User.Identity.IsAuthenticated)
            {

                model.FromEntity(_RightMoneyService.GetMoneyInfo(Int32.Parse(User.Identity.GetUserId())));

            }
            return PartialView(model);

        }

        [ChildActionOnly]
        public ActionResult RightHelpsPartial()
        {
            List<HelpDataModels> models = new List<HelpDataModels>();
            string[] path = HttpContext.Request.RawUrl.Split('/');
            string URL = path.Length > 1 ? path[1] : "";
            string lang = LanguageHelper.GetCurrentLanguage();
            int LanguageId = (lang == "ru" ? 1 : 2);
            HelpDataModels model = new HelpDataModels();
            var queryHelp = _HelpDataService.GetHelpInfo(URL, LanguageId);
            foreach (var item in queryHelp)
            {
                model.FromEntity(item);
                models.Add(model);
            }
            return PartialView(models);
        }


        public ActionResult Help()
        {
            switch (LanguageHelper.GetCurrentLanguage())
            {
                case "ru":
                    return View("Help");
                default:
                    return View("HelpEn");
            }
        }

        [HttpPost]
        public ActionResult ChangeCulture(string lang)
        {
            string returnUrl = Request.UrlReferrer.PathAndQuery;
            if (!LanguageHelper.IsRelevantLanguage(lang))
            {
                lang = "en";
            }
            // Сохраняем выбранную культуру в куки
            HttpCookie cookie = Request.Cookies["lang"];
            if (cookie != null)
                cookie.Value = lang;   // если куки уже установлено, то обновляем значение
            else
            {
                cookie = new HttpCookie("lang");
                cookie.HttpOnly = false;
                cookie.Value = lang;
                cookie.Expires = DateTime.UtcNow.AddYears(1);
            }

            LanguageHelper.ClearCurrentLanguage();

            if (lang != LanguageHelper.GetDefaultLanguage())
            {
                if (!returnUrl.Contains("/ru"))
                {
                    returnUrl = string.Concat('/', lang, returnUrl);
                }
            }
            else
            {
                if (Request.RequestContext.RouteData.Values["lang"] != null)
                {
                    returnUrl = returnUrl.Replace("/" + Request.RequestContext.RouteData.Values["lang"].ToString(), "");
                }
            }
            Response.Cookies.Add(cookie);
            return Redirect(!string.IsNullOrEmpty(returnUrl) ? returnUrl : "/");
        }
    }
}