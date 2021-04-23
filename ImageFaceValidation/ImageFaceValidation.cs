using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Web;
using System.Web.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;
using Sitecore.ExperienceForms.Data;
using Sitecore.ExperienceForms.Data.Entities;
using Sitecore.ExperienceForms.Models;
using Sitecore.ExperienceForms.Mvc;
using Sitecore.ExperienceForms.Mvc.Models.Fields;
using Sitecore.ExperienceForms.Mvc.Models.Validation;
using ImageFaceValidation_SitecoreFormsML.Model;
using Microsoft.ML;

namespace ImageFaceValidation
{
    public class ImageFaceValidation : ValidationElement<string>
    {
        private IFormRenderingContext _formRenderingContext;
        private IFileStorageProvider _fileStorageProvider;
        private ValidationDataModel _validationItem;

        public ImageFaceValidation(ValidationDataModel validationItem) : base(validationItem)
        {
            this._validationItem = validationItem;
            this._fileStorageProvider = ServiceLocator.ServiceProvider.GetService<IFileStorageProvider>();
            this._formRenderingContext = ServiceLocator.ServiceProvider.GetService<IFormRenderingContext>(); ;
        }

        public override IEnumerable<ModelClientValidationRule> ClientValidationRules
        {
            get
            {
                var clientValidationRule = new ModelClientValidationRule
                {
                    ErrorMessage = FormatMessage(Title),
                    ValidationType = "regex"
                };
              
                yield return clientValidationRule;
            }
        }

        public string Title { get; set; }

        public override ValidationResult Validate(object value)
        {
            if (value != null)
            {
                List<HttpPostedFileBase> httpPostedFileBaseList = value as List<HttpPostedFileBase>;
                if (httpPostedFileBaseList != null)
                {
                    
                    List<StoredFileInfo> storedFiledList = new List<StoredFileInfo>();

                    foreach (HttpPostedFileBase httpPostedFileBase in httpPostedFileBaseList)
                    {
                        if (httpPostedFileBase == null)
                        {
                            return new ValidationResult(FormatMessage(Title));
                        }


                        MemoryStream ms = new MemoryStream();
                        httpPostedFileBase.InputStream.CopyTo(ms);
                        //mmm model input is filename... need to store on disk
                        string tempFileName = System.IO.Path.GetTempFileName();
                        httpPostedFileBase.SaveAs(tempFileName);



                        // Create single instance of sample data from first line of dataset for model input
                        ModelInput sampleData = new ModelInput()
                        {
                            ImageSource = tempFileName
                        };

                        // Make a single prediction on the sample data and print results
                        var predictionResult = ConsumeModel.Predict(sampleData);
                        

                        if (predictionResult.Score[0] < 0.9)
                        {
                            File.Delete(tempFileName);
                            return new ValidationResult(FormatMessage(Title));
                        }

                        using (var uploadFileStream = System.IO.File.Open(tempFileName, FileMode.Open))
                        {
                            var fileId = this._fileStorageProvider.StoreFile(uploadFileStream, httpPostedFileBase.FileName);
                            storedFiledList.Add(new StoredFileInfo()
                            {
                                FileId = fileId,
                                FileName = httpPostedFileBase.FileName
                            });
                        }
                        File.Delete(tempFileName);

                    }
                   
                    // Add the stored image to the postedFileList
                    List<IViewModel> postedFieldList = new List<IViewModel>();
                    postedFieldList.Add(new FileUploadViewModel()
                    {
                        AllowSave = false,
                        Name = "Face",
                        ItemId = Guid.NewGuid().ToString(),
                        Value = storedFiledList,
                        Files = httpPostedFileBaseList
                    });
                    _formRenderingContext.StorePostedFields(postedFieldList);
                }
            }
            return ValidationResult.Success;
        }

        public override void Initialize(object validationModel)
        {
            base.Initialize(validationModel);

            var obj = validationModel as StringInputViewModel;
            if (obj != null)
            {
                Title = obj.Title;
            }
        }

 
    }
}

