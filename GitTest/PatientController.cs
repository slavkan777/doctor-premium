using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using AutoMapper;
using DoctorPremium.Services.Interfaces;
using DoctorPremium.DAL.Model;
using DoctorPremium.Web.Helpers;
using DoctorPremium.Web.Models;
using Google.Apis.Drive.v2.Data;
using log4net;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Org.BouncyCastle.Asn1.Ocsp;

namespace DoctorPremium.Web.Controllers
{
	[Authorize(Roles = "Doctor")]
	public class PatientController : BaseController
    {
        private IPatientService _patientService;
        private IPatientDocumentService _patientDocumentService;
		private ICountryService _countryService;
		private ICityService _cityService;
		private IUserInfoService _UserInfoService;
		private CountryAndCityHelper _countryAndCityHelper;
        private IScheduleService _scheduleService;

		public CountryAndCityHelper CountryAndCityHelper
		{
			get
			{
				return _countryAndCityHelper ?? new CountryAndCityHelper(_countryService, _cityService);
			}
		}

		public PatientController(IPatientService patientService, IPatientDocumentService patientDocumentService, ICountryService countryService, ICityService cityService, IUserInfoService userInfoService,IScheduleService scheduleService)
        {
            this._patientService = patientService;
            this._patientDocumentService = patientDocumentService;
            this._scheduleService = scheduleService;
			this._countryService = countryService;
			this._cityService = cityService;
			this._UserInfoService = userInfoService;
        }

        public ActionResult Edit(int? id, int? ScheduleRecordID)
        {
            PatientEditViewModel model = new PatientEditViewModel();

            if (id == null || id == 0)
            {
                model.CreateNewDentalCards();
	            UserInfo userInfo = _UserInfoService.GetUserInfo(Int32.Parse(User.Identity.GetUserId())); 
				if (userInfo != null)
	            {
		            model.CountryId = userInfo.CountryId;
		            model.CityId = userInfo.CityId;
	            }
            }
            else
            {
                Patient patientInfo = _patientService.GetPatientInfo((int)id, Int32.Parse(User.Identity.GetUserId()));
                ViewBag.Debt = patientInfo.Debt;
                if (patientInfo == null)
                {
                    throw new HttpException(404, "Patient not found"); // TODO: need not found page
                }
                else
                {
                    model.FromEntity(patientInfo);
                }
            }
            model.ScheduleRecordId = ScheduleRecordID ?? 0;
	        FillCountryAndCity(model);

            return View(model);

        }

        [HttpPost]
        public async Task<ActionResult> Edit(PatientEditViewModel model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                string newFileName;

                if (model.PatientId == 0)
                {
                    Patient patient = model.ToEntity(model, new Patient());
                    patient.UserId = Int32.Parse(User.Identity.GetUserId());

                    Patient newPatient = _patientService.Save(patient);
                    model.PatientId = newPatient.PatientId;
                    String newPhoto = SavePatientPhoto(model.UserPhoto, model.PatientId, model.Photo);
                    if (!String.IsNullOrEmpty(newPhoto))
                    {
                        model.Photo = newPhoto;
                    }
                    
                    if (model.ScheduleRecordId > 0)
                    {
                         ScheduleRecord record = _scheduleService.GetScheduleRecord(Int32.Parse(User.Identity.GetUserId()), model.ScheduleRecordId);
                        if (record != null)
                        {
                            record.PatientId = newPatient.PatientId;
                            _scheduleService.Save(record);
                        }
                    }
                    _patientService.Save(newPatient);
                }
                else
                {

                    Patient oldPatientInfo = _patientService.GetPatientInfo(model.PatientId, Int32.Parse(User.Identity.GetUserId()));
                    if (oldPatientInfo == null)
                    {
                        throw new HttpException(404, "Patient not found"); // TODO: need not found patient page
                    }
                    else
                    {
                        Patient patient = model.ToEntity(model, oldPatientInfo);
                        String newPhoto = SavePatientPhoto(model.UserPhoto, model.PatientId, model.Photo);
                        if (!String.IsNullOrEmpty(newPhoto))
                        {
                            patient.Photo = newPhoto;
                        }
                        _patientService.Save(patient);
                    }

                    //return RedirectToAction("List");
                }

                return RedirectToAction("Review", new { id = model.PatientId, });
            }
			FillCountryAndCity(model);
            return View(model);
        }

        [HttpPost]
        public ActionResult UploadDocuments(int patientId, String[] titles)//, IEnumerable<HttpPostedFileBase> files
        {
            //var files = Request.Files;
            List<PatientDocument> documents;
            if (Request.Files != null && Request.Files.Count > 0)
            {
                documents = new List<PatientDocument>();
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    var upFile = Request.Files[i];
                    if (upFile.ContentLength > 0)
                    {
                        String fileName = SavePatientDocument(upFile, patientId);
                        if (!String.IsNullOrEmpty(fileName))
                        {
                            documents.Add(new PatientDocument()
                            {
                                PatientId = patientId,
                                FileName = fileName,
                                Description = titles[i],
								CreateDateUtc = DateTime.UtcNow
                            });
                        }
                    }
                }
                if (documents.Count > 0)
                {
                    _patientDocumentService.SaveList(documents);
                }
            }
            return RedirectToAction("Review", new { id = patientId, });//View("PatientDocumentList");//
        }

        public ActionResult PatientDocumentList(int patientId)
        {
            List<PatientDocument> documents = _patientDocumentService.GetListDocumentsForPatient(patientId);
            List<PatientDocumentViewModel> models = new List<PatientDocumentViewModel>();
            if (documents.Count > 0)
            {
                Mapper.CreateMap<PatientDocument, PatientDocumentViewModel>();
                models = Mapper.Map<List<PatientDocument>, List<PatientDocumentViewModel>>(documents);
            }
            return PartialView(models);
        }

        private string SavePatientPhoto(HttpPostedFileBase photo, int patientId, string oldFileName)
        {
            String fileName = "";
            try
            {
                if (photo != null && photo.ContentLength > 0 && photo.ContentType.Contains("image"))
                {
                    fileName = ImageNameHelper.GetFileNameForPatientPhoto(patientId, photo.FileName);
                    var path = Path.Combine(Server.MapPath("~/Photos/"),
                                        System.IO.Path.GetFileName(fileName ?? "image"));
                    if (!String.IsNullOrEmpty(oldFileName))
                    {
                        DeletePatientPhoto(Path.Combine(Server.MapPath("~/Photos/"), System.IO.Path.GetFileName(oldFileName)));
                    }
                    photo.SaveAs(path);
                    return fileName;
                }
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(typeof(MvcApplication)).Error(String.Format("Ошибка сохранения файла {0}. patientid={1}", fileName, patientId), ex);
            }

            return null;
        }

        private string SavePatientDocument(HttpPostedFileBase file, int patientId)
        {
            String fileName = "";
            try
            {
                if (file != null && file.ContentLength > 0)
                {
                    fileName = ImageNameHelper.GetFileNameForPatientDoc(patientId, file.FileName);
                    var path = Path.Combine(Server.MapPath("~/UploadFiles/"),
                                        System.IO.Path.GetFileName(fileName));
                    //DeletePatientPhoto(Path.Combine(Server.MapPath("~/Photos/"), System.IO.Path.GetFileName(oldFileName)));
                    file.SaveAs(path);
                    return fileName;
                }
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(typeof(MvcApplication)).Error(String.Format("Ошибка сохранения файла {0}. patientid={1}", fileName, patientId), ex);
            }

            return null;
        }

        private void DeletePatientPhoto(String path)
        {
            if (!String.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }

        public ActionResult List()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ListPatients(DataTableParamModel param)
        {
            try
            {

                string searchString = HttpContext.Request["search[value]"];
                int recordsTotal = 0;
                int recordsFiltered = 0;
                List<Patient> patients = _patientService.GetListToTable(Int32.Parse(User.Identity.GetUserId()), param.Start,
                    param.Length, searchString, param.Order.ToList()[0].Column, param.Order.ToList()[0].Dir, out recordsTotal, out recordsFiltered);
                
                List<PatientListViewModel> items = new List<PatientListViewModel>();

                if (patients.Count > 0)
                {
                    Mapper.CreateMap<Patient, PatientListViewModel>()
                        .ForMember(d => d.FullName,
                            opt => opt.MapFrom(s => FullNameHelper.GetFullName(s.LastName, s.FirstName, s.SurName))) 
                            .ForMember(d => d.Phones, opt => opt.MapFrom(s => s.MobilePhone + (s.MobilePhone != null ? ", ":"") + s.HomePhone))
                        .ForMember(d => d.VisitCount, opt => opt.MapFrom(s => s.PatientVisits.Count))
                        .ForMember(d => d.IsMale, opt => opt.MapFrom(s => (s.IsMale )))
                        .ForMember(d => d.LastVisit, opt => opt.MapFrom(s => s.PatientVisits.Count > 0 ? s.PatientVisits.OrderBy(x => x.VisitDate).Last().VisitDate : new DateTime?()));
                    items = Mapper.Map<List<Patient>, List<PatientListViewModel>>(patients);
                }

                return Json(new
                {
                    draw = param.Draw,
                    recordsTotal = recordsTotal,
                    recordsFiltered = recordsFiltered,
                    data = items
                },
                    JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }


        public ActionResult Review(int? id)
        {
            PatientEditViewModel model = new PatientEditViewModel();
            if (id != null && id != 0)
            {
                Patient patientInfo = _patientService.GetPatientInfo((int)id, Int32.Parse(User.Identity.GetUserId()));
                ViewBag.Debt = patientInfo.Debt;
                if (patientInfo == null)
                {
                    throw new HttpException(404, "Patient not found"); // TODO: need not found patient page
                }
                else
                {
                    model.FromEntity(patientInfo);
					FillCountryAndCity(model);
                }
            }
            else
            {
                throw new HttpException(404, "Page not found");

            }
            return View(model);
        }


        [HttpPost]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DeleteDataPatient(int id)
        {
            bool success = false;
            if (id != 0)
            {
                Patient patientInfo = _patientService.GetPatientInfo((int)id, Int32.Parse(User.Identity.GetUserId()));
                if (patientInfo == null)
                {
                    throw new HttpException(404, "Patient not found"); // TODO: need not found patient page
                }
                else
                {
                    _patientService.Delete(patientInfo);
                    success = true;
                }
            }
            else
            {
                throw new HttpException(404, "Page not found");

            }
            var v = new { success = success, error = "" };
            return Json(v);
        }

		[NonAction]
		private void FillCountryAndCity(PatientEditViewModel model)
		{
			model.countryItems.AddRange(CountryAndCityHelper.GetCountryDropList());
			if (model.CountryId != null)
			{
				model.cityItems.AddRange(CountryAndCityHelper.GetCityDropList((int)model.CountryId));
			}
		}
    }
}
