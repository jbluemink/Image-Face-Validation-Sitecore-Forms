using System.Collections.Generic;
using System.Web;
using Sitecore.Diagnostics;
using Sitecore.ExperienceForms.Data;
using Sitecore.ExperienceForms.Data.Entities;
using Sitecore.ExperienceForms.Models;
using Sitecore.ExperienceForms.Mvc;
using Sitecore.ExperienceForms.Mvc.Models.Fields;
using Sitecore.ExperienceForms.Mvc.Pipelines.ExecuteSubmit;
using Sitecore.Mvc.Pipelines;

namespace ImageFaceValidation.Pipelines
{
    public class FixAlreadyUploadedFiles : MvcPipelineProcessor<ExecuteSubmitActionsEventArgs>
    {
        private readonly IFormRenderingContext _formRenderingContext;
        private readonly IFileStorageProvider _fileStorageProvider;

        public FixAlreadyUploadedFiles(
            IFormRenderingContext formRenderingContext,
            IFileStorageProvider fileStorageProvider)
        {
            Assert.ArgumentNotNull((object) formRenderingContext, nameof(formRenderingContext));
            Assert.ArgumentNotNull((object) fileStorageProvider, nameof(fileStorageProvider));
            this._formRenderingContext = formRenderingContext;
            this._fileStorageProvider = fileStorageProvider;
        }

        public override void Process(ExecuteSubmitActionsEventArgs args)
        {
            Assert.ArgumentNotNull((object) args, nameof(args));
            if (args.FormSubmitContext?.Fields == null)
                return;
            IList<IViewModel> fields = args.FormSubmitContext?.Fields;
            if (fields == null)
                return;
            foreach (IViewModel viewModel in (IEnumerable<IViewModel>) fields)
            {
                if (viewModel is FileUploadViewModel fileUploadViewModel)
                {
                    if (fileUploadViewModel.Files != null)
                    {
                        List<StoredFileInfo> storedFileInfoList = GetFaceFileUpload(fileUploadViewModel.Files);
                        if (storedFileInfoList != null)
                        {
                            fileUploadViewModel.Value = storedFileInfoList;
                        }

                    }
                }
            }

        }


        private List<StoredFileInfo> GetFaceFileUpload(List<HttpPostedFileBase> fileList)
        {
            if (_formRenderingContext.PostedFields != null)
            {
                foreach (var postedFieldField in _formRenderingContext.PostedFields)
                {
                    if (postedFieldField.Name == "Face")
                    {
                        var fileField = (FileUploadViewModel)postedFieldField;
                        if (fileField.Files == fileList)
                        {
                            return fileField.Value;
                        }
                    }
                }
            }

            return null;
        }
    }
}
