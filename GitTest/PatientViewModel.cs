using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AutoMapper;
using DoctorPremium.DAL.Model;
using DoctorPremium.Web.Helpers;
using System.Net;
using Microsoft.Ajax.Utilities;
using Resources;

namespace DoctorPremium.Web.Models
{
	public class PatientEditViewModel
	{
		public PatientEditViewModel()
		{
			genderItems = GenderHelper.GetGenderListItems();
			countryItems = new List<SelectListItem>();
			cityItems = new List<SelectListItem>();
		}

		public Patient ToEntity(PatientEditViewModel model, Patient oldPatient)
		{
			Patient newPatient = oldPatient;
			newPatient.AdditionalInfo = model.AdditionalInfo != null ? model.AdditionalInfo.Trim() : null;
			newPatient.Address = model.Address != null ? model.Address.Trim() : null;
			newPatient.AlergoStatus = model.AlergoStatus != null ? model.AlergoStatus.Trim() : null;
			newPatient.Anamnez = model.Anamnez != null ? model.Anamnez.Trim() : null;
			newPatient.AnyContact = model.AnyContact != null ? model.AnyContact.Trim() : null;
			newPatient.CityId = model.CityId;
			newPatient.Comment = model.Comment != null ? model.Comment.Trim() : null;
			newPatient.CountryId = model.CountryId;
			newPatient.Email = model.Email != null ? model.Email.Trim() : null;
			newPatient.FirstName = model.FirstName != null ? model.FirstName.Trim() : null;
			newPatient.HomePhone = model.HomePhone != null ? model.HomePhone.Trim() : null;
			newPatient.ImmunoStatus = model.ImmunoStatus != null ? model.ImmunoStatus.Trim() : null;
			newPatient.LastName = model.LastName != null ? model.LastName.Trim() : null;
			newPatient.MobilePhone = model.MobilePhone != null ? model.MobilePhone.Trim() : null;
			newPatient.SurName = model.SurName != null ? model.SurName.Trim() : null;
			newPatient.WorkPlace = model.WorkPlace != null ? model.WorkPlace.Trim() : null;
			newPatient.BirthDate = model.BirthDate ?? new DateTime();
			newPatient.IsMale = model.IsMale ?? false;
			newPatient.IsVIP = model.IsVIP;
			newPatient.Photo = model.Photo;
			newPatient.CommentDentalCard = model.CommentDentalCard;
			//Mapper.CreateMap<PatientEditViewModel, Patient>()
			//	.ForMember(dto => dto.UserId, opt => opt.Ignore())
			//	.ForMember(dto => dto.PatientId, opt => opt.Ignore())
			//	.ForMember(dto => dto.CardNumber, opt => opt.Ignore())
			//	.ForMember(dto => dto.CreateDate, opt => opt.Ignore())
			//	.ForMember(dto => dto.IsDeleted, opt => opt.Ignore());
			//newPatient = Mapper.Map(this, newPatient);
			if (model.DentalCards != null && model.DentalCards.Any())
			{
				ICollection<DentalCard> dentalItems = new Collection<DentalCard>();
				//Mapper.CreateMap<DentalCardModel, DentalCard>()
				//	.ForMember(dto => dto.PatientId,  opt => opt.UseValue(model.PatientId));
				//dentalItems = Mapper.Map<IList<DentalCardModel>, ICollection<DentalCard>>(model.DentalCards);
				foreach (var card in model.DentalCards)
				{
					if (!(String.IsNullOrEmpty(card.Description) && !card.IsCheck && card.DentalCardId == 0))
					{
						Mapper.CreateMap<DentalCardModel, DentalCard>()
							.ForMember(dto => dto.PatientId, opt => opt.UseValue(model.PatientId));
						dentalItems.Add(Mapper.Map<DentalCardModel, DentalCard>(card));
					}
				}

				if (newPatient.DentalCards == null || newPatient.DentalCards.Count == 0)
				{
					newPatient.DentalCards = dentalItems;
				}
				else
				{
					DentalCard card;
					foreach (var item in dentalItems)
					{
						card = newPatient.DentalCards.FirstOrDefault(x => x.DentalCardId == item.DentalCardId);
						if (card != null)
						{
							card.Description = item.Description;
							card.IsCheck = item.IsCheck;
						}
						else
						{
							newPatient.DentalCards.Add(item);
						}
					}
				}
			}

			return newPatient;
		}

		public void FromEntity(Patient patient)
		{
			Mapper.CreateMap<Patient,PatientEditViewModel>()
			.ForMember(dto => dto.DentalCards, opt => opt.Ignore());
			Mapper.Map(patient, this);
			if (this.PatientId == 0)
			{
				for (int i = 0; i < 32; i++)
				{
					this.DentalCards.Add(new DentalCardModel(){OrderNumber = (byte)(i+1)});
				}
			}
			else
			{
                if (patient.DentalCards == null || patient.DentalCards.Count == 0)
                {
                    CreateNewDentalCards();
                }
                else
                {
                    Mapper.CreateMap<DentalCard, DentalCardModel>();
                    this.DentalCards = Mapper.Map<ICollection<DentalCard>, IList<DentalCardModel>>(patient.DentalCards).OrderBy(m => m.OrderNumber).ToList();
                }

                if (this.DentalCards.Count < 32)
                {
                    for (int i = 1; i < 33; i++)
                    {
                        if (this.DentalCards.Count < 32 && this.DentalCards.FirstOrDefault(x => x.OrderNumber == i) == null) //i < this.DentalCards.Count 
                        {
                            this.DentalCards.Add(new DentalCardModel() { OrderNumber = (byte)i });
                        }
                    }
                    this.DentalCards = this.DentalCards.OrderBy(x => x.OrderNumber).ToList();
                }
			}
		}

		public void CreateNewDentalCards()
		{
			this.DentalCards = new List<DentalCardModel>();
			for (int i = 0; i < 32; i++)
			{
				this.DentalCards.Add(new DentalCardModel() { OrderNumber = (byte)(i + 1) });
			}
		}

		public int PatientId { get; set; }

        public int UserId { get; set; }

		[Display(Name = "Номер карточки")]
        public string CardNumber { get; set; }

        //[StringLength(80)]
        //[Required(ErrorMessageResourceType = typeof(Schedule), ErrorMessageResourceName = "TitleRequired")]
        //[Display(Name = "Title", ResourceType = typeof(Schedule))]




        [StringLength(64)]
        [Required(ErrorMessageResourceType = typeof(PatientInfo), ErrorMessageResourceName = "AddLast")]
        [Display(Name = "Last", ResourceType = typeof(PatientInfo))]
        public string LastName { get; set; }



        [StringLength(64)]
        [Required(ErrorMessageResourceType = typeof(PatientInfo), ErrorMessageResourceName = "AddName")]
        [Display(Name = "Name", ResourceType = typeof(PatientInfo))]
         public string FirstName { get; set; }

		[StringLength(64)] 
        [Display(Name = "Middle", ResourceType = typeof(PatientInfo))]
        public string SurName { get; set; }

        public string Photo { get; set; }

        [Required(ErrorMessageResourceType = typeof(PatientInfo), ErrorMessageResourceName = "AddGender")]
        [Display(Name = "Gender", ResourceType = typeof(PatientInfo))]
        public bool? IsMale { get; set; }

        [Required(ErrorMessageResourceType = typeof(PatientInfo), ErrorMessageResourceName = "AddBirthDate")]
        [Display(Name = "BirthDate", ResourceType = typeof(PatientInfo))]
        public System.DateTime? BirthDate { get; set; }


        [Display(Name = "CountryId", ResourceType = typeof(PatientInfo))]
        public int? CountryId { get; set; }


        [Display(Name = "CityId", ResourceType = typeof(PatientInfo))]
		public int? CityId { get; set; }


        //[Required(ErrorMessageResourceType = typeof(PatientInfo), ErrorMessageResourceName = "Address")]
        
     
		[StringLength(200)]
        [Display(Name = "Address", ResourceType = typeof(PatientInfo))]
        public string Address { get; set; }

		[StringLength(16)]
        [Display(Name = "HomePhone", ResourceType = typeof(PatientInfo))] 
        public string HomePhone { get; set; }

		[StringLength(16)]
        [Display(Name = "MobilePhone", ResourceType = typeof(PatientInfo))] 
        public string MobilePhone { get; set; }

		[EmailAddress]
		[StringLength(80)]
		[Display(Name = "Email")]
        public string Email { get; set; }

		[StringLength(200)]
        [Display(Name = "AnyContact", ResourceType = typeof(PatientInfo))]  
        public string AnyContact { get; set; }

		[StringLength(200)] 
        [Display(Name = "WorkPlace", ResourceType = typeof(PatientInfo))]  
        public string WorkPlace { get; set; }

		[StringLength(4000)]
        [Display(Name = "Comment", ResourceType = typeof(PatientInfo))]  
        public string Comment { get; set; }

		[StringLength(500)]
        [Display(Name = "Anamnez", ResourceType = typeof(PatientInfo))] 
        public string Anamnez { get; set; }

		[StringLength(500)]
        [Display(Name = "AlergoStatus", ResourceType = typeof(PatientInfo))] 
        public string AlergoStatus { get; set; }

		[StringLength(500)]
        [Display(Name = "ImmunoStatus", ResourceType = typeof(PatientInfo))] 
        public string ImmunoStatus { get; set; }

		[StringLength(500)]
        [Display(Name = "AdditionalInfo", ResourceType = typeof(PatientInfo))]
        public string AdditionalInfo { get; set; }

		[StringLength(400)]
        [Display(Name = "CommentDentalCard", ResourceType = typeof(PatientInfo))] 
		public string CommentDentalCard { get; set; }


        [Required(ErrorMessageResourceType = typeof(PatientInfo), ErrorMessageResourceName = "IsVIP")]
        [Display(Name = "IsVIP", ResourceType = typeof(PatientInfo))]
     	public bool IsVIP { get; set; }
        public System.DateTime CreateDateUtc { get; set; }

        [Display(Name = "DentalChart", ResourceType = typeof(PatientInfo))]
        public string Blore { get; set; }


        public Nullable<System.DateTime> UpdateDate { get; set; }


		public List<SelectListItem> genderItems;
		public List<SelectListItem> countryItems;
		public List<SelectListItem> cityItems;
		public IList<DentalCardModel> DentalCards{ get; set; }

		public HttpPostedFileBase UserPhoto { get; set; }

       [Display(Name = "Balance", ResourceType = typeof(PatientInfo))]
        public int Debt { get; set; }
		// public virtual ICollection<PatientVisit> PatientVisits { get; set; }

        public int ScheduleRecordId { get; set; }
	}

	public class PatientListViewModel
	{
		public PatientListViewModel()
		{
		}

		public int PatientId { get; set; }

		//public int UserId { get; set; }

		public string CardNumber { get; set; }

		public string FullName { get; set; }

		//public string LastName { get; set; }

		//public string FirstName { get; set; }

		//public string SurName { get; set; }

		public System.DateTime? BirthDate { get; set; }

		//public string HomePhone { get; set; }

		//public string MobilePhone { get; set; }

		public string Phones { get; set; }

		//public string Email { get; set; }

		//public string Comment { get; set; }

		public int VisitCount { get; set; }

		public int Debt { get; set; }

		public bool IsVip { get; set; }

		public DateTime? LastVisit { get; set; }
        public bool? IsMale { get; set; }
	}

	public class DentalCardModel
	{
		public int DentalCardId { get; set; }
		public byte OrderNumber { get; set; }

		[StringLength(20)]
		public string Description { get; set; }
		public bool IsCheck { get; set; }
	}

	public class PatientDocumentViewModel
	{
		public int PatientDocumentId { get; set; }
		public int PatientId { get; set; }
		public string FileName { get; set; }
		public string Description { get; set; }
		public System.DateTime CreateDateUtc { get; set; }
	}
}