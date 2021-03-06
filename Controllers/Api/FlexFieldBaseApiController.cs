using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GoNorth.Data.Project;
using GoNorth.Services.Timeline;
using GoNorth.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using GoNorth.Data.User;
using GoNorth.Data.FlexFieldDatabase;
using GoNorth.Services.ImplementationStatusCompare;

namespace GoNorth.Controllers.Api
{
    /// <summary>
    /// Flex Field Base Api Controller Api controller
    /// </summary>
    public abstract class FlexFieldBaseApiController<T> : Controller where T:FlexFieldObject
    {
        /// <summary>
        /// Folder Request data
        /// </summary>
        public class FolderRequest
        {
            /// <summary>
            /// Folder Name
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Description
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// Parent Folder Id
            /// </summary>
            public string ParentId { get; set; }
        };

        /// <summary>
        /// Folder Query Result
        /// </summary>
        public class FolderQueryResult
        {
            /// <summary>
            /// Name of the folder
            /// </summary>
            public string FolderName { get; set; }

            /// <summary>
            /// Id of the parent folder
            /// </summary>
            public string ParentId { get; set; }

            /// <summary>
            /// true if there are more folders to query, else false
            /// </summary>
            public bool HasMore { get; set; }

            /// <summary>
            /// Folders
            /// </summary>
            public IList<FlexFieldFolder> Folders { get; set; }
        }


        /// <summary>
        /// Flex Field Object Query Result
        /// </summary>
        public class FlexFieldObjectQueryResult
        {
            /// <summary>
            /// true if there are more objects to query, else false
            /// </summary>
            public bool HasMore { get; set; }

            /// <summary>
            /// Objects
            /// </summary>
            public IList<T> FlexFieldObjects { get; set; }
        }


        /// <summary>
        /// Event used for the folder created event
        /// </summary>
        protected abstract TimelineEvent FolderCreatedEvent { get; }

        /// <summary>
        /// Event used for the folder deleted event
        /// </summary>
        protected abstract TimelineEvent FolderDeletedEvent { get; }

        /// <summary>
        /// Event used for the folder updated event
        /// </summary>
        protected abstract TimelineEvent FolderUpdatedEvent { get; }


        /// <summary>
        /// Event used for the template created event
        /// </summary>
        protected abstract TimelineEvent TemplateCreatedEvent { get; }

        /// <summary>
        /// Event used for the template deleted event
        /// </summary>
        protected abstract TimelineEvent TemplateDeletedEvent { get; }

        /// <summary>
        /// Event used for the template updated event
        /// </summary>
        protected abstract TimelineEvent TemplateUpdatedEvent { get; }

        /// <summary>
        /// Event used for the template fields distributed event
        /// </summary>
        protected abstract TimelineEvent TemplateFieldsDistributedEvent { get; }

        /// <summary>
        /// Event used for the flex field template image updated event
        /// </summary>
        protected abstract TimelineEvent TemplateImageUploadEvent { get; }


        /// <summary>
        /// Event used for the flex field object created event
        /// </summary>
        protected abstract TimelineEvent ObjectCreatedEvent { get; }

        /// <summary>
        /// Event used for the flex field object deleted event
        /// </summary>
        protected abstract TimelineEvent ObjectDeletedEvent { get; }
        
        /// <summary>
        /// Event used for the flex field object updated event
        /// </summary>
        protected abstract TimelineEvent ObjectUpdatedEvent { get; }

        /// <summary>
        /// Event used for the flex field object image updated event
        /// </summary>
        protected abstract TimelineEvent ObjectImageUploadEvent { get; }        


        /// <summary>
        /// Object Folder Db Service
        /// </summary>
        private readonly IFlexFieldFolderDbAccess _folderDbAccess;

        /// <summary>
        /// Flex Field Object Template Db Service
        /// </summary>
        private readonly IFlexFieldObjectDbAccess<T> _templateDbAccess;

        /// <summary>
        /// Object Db Service
        /// </summary>
        protected readonly IFlexFieldObjectDbAccess<T> _objectDbAccess;

        /// <summary>
        /// Project Db Service
        /// </summary>
        protected readonly IProjectDbAccess _projectDbAccess;

        /// <summary>
        /// Flex Field Object Tag Db Access
        /// </summary>
        private readonly IFlexFieldObjectTagDbAccess _tagDbAccess;

        /// <summary>
        /// Image Access
        /// </summary>
        private readonly IFlexFieldObjectImageAccess _imageAccess;

        /// <summary>
        /// Implementation status comparer
        /// </summary>
        protected readonly IImplementationStatusComparer _implementationStatusComparer;

        /// <summary>
        /// Timeline Service
        /// </summary>
        private readonly ITimelineService _timelineService;

        /// <summary>
        /// User Manager
        /// </summary>
        private readonly UserManager<GoNorthUser> _userManager;

        /// <summary>
        /// Logger
        /// </summary>
        protected readonly ILogger _logger;

        /// <summary>
        /// Localizer
        /// </summary>
        protected readonly IStringLocalizer _localizer;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="folderDbAccess">Folder Db Access</param>
        /// <param name="templateDbAccess">Template Db Access</param>
        /// <param name="objectDbAccess">Object Db Access</param>
        /// <param name="projectDbAccess">Project Db Access</param>
        /// <param name="tagDbAccess">Tag Db Access</param>
        /// <param name="imageAccess">Image Access</param>
        /// <param name="userManager">User Manager</param>
        /// <param name="implementationStatusComparer">Implementation Status Comparer</param>
        /// <param name="timelineService">Timeline Service</param>
        /// <param name="logger">Logger</param>
        /// <param name="localizerFactory">Localizer Factory</param>
        public FlexFieldBaseApiController(IFlexFieldFolderDbAccess folderDbAccess, IFlexFieldObjectDbAccess<T> templateDbAccess, IFlexFieldObjectDbAccess<T> objectDbAccess, IProjectDbAccess projectDbAccess, IFlexFieldObjectTagDbAccess tagDbAccess, 
                                          IFlexFieldObjectImageAccess imageAccess, UserManager<GoNorthUser> userManager, IImplementationStatusComparer implementationStatusComparer, ITimelineService timelineService, ILogger<FlexFieldBaseApiController<T>> logger, 
                                          IStringLocalizerFactory localizerFactory)
        {
            _folderDbAccess = folderDbAccess;
            _templateDbAccess = templateDbAccess;
            _objectDbAccess = objectDbAccess;
            _projectDbAccess = projectDbAccess;
            _tagDbAccess = tagDbAccess;
            _imageAccess = imageAccess;
            _userManager = userManager;
            _implementationStatusComparer = implementationStatusComparer;
            _timelineService = timelineService;
            _logger = logger;
            _localizer = localizerFactory.Create(this.GetType());
        }

        /// <summary>
        /// Returns folders
        /// </summary>
        /// <param name="parentId">Parent Id of the folder which children should be requested, null or "" to retrieve root folders</param>
        /// <param name="start">Start of the page</param>
        /// <param name="pageSize">Page Size</param>
        /// <returns>Folders</returns>
        [HttpGet]
        public async Task<IActionResult> Folders(string parentId, int start, int pageSize)
        {
            string folderName = string.Empty;
            string parentFolderId = string.Empty;
            Task<List<FlexFieldFolder>> queryTask;
            Task<int> countTask;
            if(string.IsNullOrEmpty(parentId))
            {
                GoNorthProject project = await _projectDbAccess.GetDefaultProject();
                queryTask = _folderDbAccess.GetRootFoldersForProject(project.Id, start, pageSize);
                countTask = _folderDbAccess.GetRootFolderCount(project.Id);
            }
            else
            {
                FlexFieldFolder folder = await _folderDbAccess.GetFolderById(parentId);
                parentFolderId = folder.ParentFolderId;
                folderName = folder.Name;
                queryTask = _folderDbAccess.GetChildFolders(parentId, start, pageSize);
                countTask = _folderDbAccess.GetChildFolderCount(parentId);
            }

            Task.WaitAll(queryTask, countTask);

            FolderQueryResult queryResult = new FolderQueryResult();
            queryResult.FolderName = folderName;
            queryResult.ParentId = parentFolderId;
            queryResult.Folders = queryTask.Result;
            queryResult.HasMore = start + queryResult.Folders.Count < countTask.Result;
            return Ok(queryResult);
        }

        /// <summary>
        /// Creates a new folder
        /// </summary>
        /// <param name="folder">Folder to create</param>
        /// <returns>Result</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFolder([FromBody]FolderRequest folder)
        {
            if(string.IsNullOrEmpty(folder.Name))
            {
                return StatusCode((int)HttpStatusCode.BadRequest);
            }

            try
            {
                GoNorthProject project = await _projectDbAccess.GetDefaultProject();
                FlexFieldFolder newFolder = new FlexFieldFolder {
                    ProjectId = project.Id,
                    ParentFolderId = folder.ParentId,
                    Name = folder.Name,
                    Description = folder.Description
                };
                newFolder = await _folderDbAccess.CreateFolder(newFolder);
                await _timelineService.AddTimelineEntry(FolderCreatedEvent, folder.Name);
                return Ok(newFolder.Id);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Could not create folder {0}", folder.Name);
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Deletes a folder
        /// </summary>
        /// <param name="id">Id of the folder</param>
        /// <returns>Result Status Code</returns>
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFolder(string id)
        {
            FlexFieldFolder folder = await _folderDbAccess.GetFolderById(id);
            bool isFolderEmpty = await IsFolderEmpty(folder);
            if(!isFolderEmpty)
            {
                _logger.LogInformation("Attempted to delete non empty folder {0}.", folder.Name);
                return StatusCode((int)HttpStatusCode.InternalServerError, _localizer["FolderNotEmpty"].Value);
            }

            await _folderDbAccess.DeleteFolder(folder);
            _logger.LogInformation("Folder was deleted.");

            _imageAccess.CheckAndDeleteUnusedImage(folder.ImageFile);

            await _timelineService.AddTimelineEntry(FolderDeletedEvent, folder.Name);
            return Ok(id);
        }

        /// <summary>
        /// Checks if a folder is empty
        /// </summary>
        /// <param name="folder">Folder to check</param>
        /// <returns>True if the folder is empty, else false</returns>
        private async Task<bool> IsFolderEmpty(FlexFieldFolder folder)
        {
            int childFolderCount = await _folderDbAccess.GetChildFolderCount(folder.Id);
            if(childFolderCount > 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Updates a folder 
        /// </summary>
        /// <param name="id">Folder Id</param>
        /// <param name="folder">Update folder data</param>
        /// <returns>Result Status Code</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFolder(string id, [FromBody]FolderRequest folder)
        {
            FlexFieldFolder loadedFolder = await _folderDbAccess.GetFolderById(id);
            loadedFolder.Name = folder.Name;
            loadedFolder.Description = folder.Description;

            await _folderDbAccess.UpdateFolder(loadedFolder);
            _logger.LogInformation("Folder was updated.");
            await _timelineService.AddTimelineEntry(FolderUpdatedEvent, folder.Name);

            return Ok(id);
        }

        
        /// <summary>
        /// Uploads an image to a flex field folder
        /// </summary>
        /// <param name="id">Id of the flex field folder</param>
        /// <returns>Image Name</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFolderImage(string id)
        {
            // Validate Date
            string validateResult = this.ValidateImageUploadData();
            if(validateResult != null)
            {
                return StatusCode((int)HttpStatusCode.BadRequest, _localizer[validateResult]);
            }

            IFormFile uploadFile = Request.Form.Files[0];
            FlexFieldFolder targetFolder = await _folderDbAccess.GetFolderById(id);
            if(targetFolder == null)
            {
                return StatusCode((int)HttpStatusCode.BadRequest, _localizer["CouldNotUploadImage"]);
            }

            // Save Image
            string objectImageFile = string.Empty;
            try
            {
                using(Stream imageStream = _imageAccess.CreateFlexFieldObjectImage(uploadFile.FileName, out objectImageFile))
                {
                    uploadFile.CopyTo(imageStream);
                }

                string oldImageFile = targetFolder.ImageFile;
                targetFolder.ImageFile = objectImageFile;

                await _folderDbAccess.UpdateFolder(targetFolder);

                if(!string.IsNullOrEmpty(oldImageFile))
                {
                    _imageAccess.CheckAndDeleteUnusedImage(oldImageFile);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Could not upload image");
                return StatusCode((int)HttpStatusCode.InternalServerError, _localizer["CouldNotUploadImage"]);
            }

            return Ok(objectImageFile);
        }


        /// <summary>
        /// Returns a flex field template by its id
        /// </summary>
        /// <param name="id">Id of the template</param>
        /// <returns>Flex Field Template</returns>
        [HttpGet]
        public async Task<IActionResult> FlexFieldTemplate(string id)
        {
            T template = await _templateDbAccess.GetFlexFieldObjectById(id);
            return Ok(template);
        }

        /// <summary>
        /// Returns entry templates
        /// </summary>
        /// <param name="start">Start of the page</param>
        /// <param name="pageSize">Page Size</param>
        /// <returns>EntryTemplates</returns>
        [HttpGet]
        public async Task<IActionResult> FlexFieldTemplates(int start, int pageSize)
        {
            GoNorthProject project = await _projectDbAccess.GetDefaultProject();
            Task<List<T>> queryTask;
            Task<int> countTask;
            queryTask = _templateDbAccess.GetFlexFieldObjectsInRootFolderForProject(project.Id, start, pageSize);
            countTask = _templateDbAccess.GetFlexFieldObjectsInRootFolderCount(project.Id);
            Task.WaitAll(queryTask, countTask);

            FlexFieldObjectQueryResult queryResult = new FlexFieldObjectQueryResult();
            queryResult.FlexFieldObjects = queryTask.Result;
            queryResult.HasMore = start + queryResult.FlexFieldObjects.Count < countTask.Result;
            return Ok(queryResult);
        }

        /// <summary>
        /// Creates a new flex field template
        /// </summary>
        /// <param name="template">Template to create</param>
        /// <returns>Result</returns>
        protected async Task<IActionResult> BaseCreateFlexFieldTemplate(T template)
        {
            if(string.IsNullOrEmpty(template.Name))
            {
                return StatusCode((int)HttpStatusCode.BadRequest);
            }

            template.ParentFolderId = string.Empty;

            if(template.Tags == null)
            {
                template.Tags = new List<string>();
            }

            try
            {
                GoNorthProject project = await _projectDbAccess.GetDefaultProject();
                template.ProjectId = project.Id;

                template = await RunAdditionalUpdates(template, template);

                await this.SetModifiedData(_userManager, template);

                template = await _templateDbAccess.CreateFlexFieldObject(template);
                await AddNewTags(template.Tags);
                await _timelineService.AddTimelineEntry(TemplateCreatedEvent, template.Name, template.Id);
                return Ok(template);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Could not create flex field template {0}", template.Name);
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Deletes a flex field template
        /// </summary>
        /// <param name="id">Id of the template</param>
        /// <returns>Result Status Code</returns>
        protected async Task<IActionResult> BaseDeleteFlexFieldTemplate(string id)
        {
            T template = await _templateDbAccess.GetFlexFieldObjectById(id);
            await _templateDbAccess.DeleteFlexFieldObject(template);
            _logger.LogInformation("Template was deleted.");

            await RemoveUnusedTags(template.Tags);

            if(!string.IsNullOrEmpty(template.ImageFile))
            {
                _imageAccess.CheckAndDeleteUnusedImage(template.ImageFile);
            }

            await _timelineService.AddTimelineEntry(TemplateDeletedEvent, template.Name);
            return Ok(id);
        }

        /// <summary>
        /// Updates a flex field template 
        /// </summary>
        /// <param name="id">Template Id</param>
        /// <param name="template">Update template data</param>
        /// <returns>Result Status Code</returns>
        protected async Task<IActionResult> BaseUpdateFlexFieldTemplate(string id, T template)
        {
            T loadedTemplate = await _templateDbAccess.GetFlexFieldObjectById(id);
            List<string> oldTags = loadedTemplate.Tags;
            if(oldTags == null)
            {
                oldTags = new List<string>();
            }
            if(template.Tags == null)
            {
                template.Tags = new List<string>();
            }

            loadedTemplate.Name = template.Name;
            loadedTemplate.Fields = template.Fields;
            loadedTemplate.Tags = template.Tags;
    
            template = await RunAdditionalUpdates(template, loadedTemplate);

            await this.SetModifiedData(_userManager, loadedTemplate);

            await _templateDbAccess.UpdateFlexFieldObject(loadedTemplate);
            _logger.LogInformation("Template was updated.");

            await AddNewTags(template.Tags.Except(oldTags, StringComparer.OrdinalIgnoreCase).ToList());
            await RemoveUnusedTags(oldTags.Except(template.Tags, StringComparer.OrdinalIgnoreCase).ToList());
            _logger.LogInformation("Tags were updated.");

            await _timelineService.AddTimelineEntry(TemplateUpdatedEvent, loadedTemplate.Name, loadedTemplate.Id);

            return Ok(loadedTemplate);
        }

        /// <summary>
        /// Distributes the fields of a template
        /// </summary>
        /// <param name="id">Template Id</param>
        /// <returns>Task</returns>
        protected async Task<IActionResult> BaseDistributeFlexFieldTemplateFields(string id)
        {
            T template = await _templateDbAccess.GetFlexFieldObjectById(id);
            if(template == null)
            {
                return StatusCode((int)HttpStatusCode.BadRequest);
            }

            List<T> flexFieldObjects = await _objectDbAccess.GetFlexFieldObjectsByTemplate(id);
            foreach(T curObject in flexFieldObjects)
            {
                List<FlexField> newFields = template.Fields.Where(f => !curObject.Fields.Any(nf => nf.Name == f.Name)).ToList();
                if(newFields.Count > 0)
                {
                    curObject.IsImplemented = false;
                }
                curObject.Fields.AddRange(newFields);

                FlexFieldApiUtil.SetFieldIdsForNewFields(curObject.Fields);

                await _objectDbAccess.UpdateFlexFieldObject(curObject);
            }

            await _timelineService.AddTimelineEntry(TemplateFieldsDistributedEvent, template.Name, template.Id);

            return Ok(id);
        }


        /// <summary>
        /// Returns an flex field object by its id
        /// </summary>
        /// <param name="id">Id of the flex field object</param>
        /// <returns>Flex field object</returns>
        [HttpGet]
        public async Task<IActionResult> FlexFieldObject(string id)
        {
            T flexFieldObject = await _objectDbAccess.GetFlexFieldObjectById(id);
            flexFieldObject = StripObject(flexFieldObject);
            return Ok(flexFieldObject);
        }

        /// <summary>
        /// Strips an object based on the rights of a user
        /// </summary>
        /// <param name="flexFieldObject">Flex field object to strip</param>
        /// <returns>Stripped object</returns>
        protected virtual T StripObject(T flexFieldObject)
        {
            return flexFieldObject;
        }

        /// <summary>
        /// Returns flex field objects
        /// </summary>
        /// <param name="parentId">Id of the folder which flex field objects should be retrieved, null or "" to retrieve from root folders</param>
        /// <param name="start">Start of the page</param>
        /// <param name="pageSize">Page Size</param>
        /// <returns>Flex Field Objects</returns>
        [HttpGet]
        public async Task<IActionResult> FlexFieldObjects(string parentId, int start, int pageSize)
        {
            Task<List<T>> queryTask;
            Task<int> countTask;
            if(string.IsNullOrEmpty(parentId))
            {
                GoNorthProject project = await _projectDbAccess.GetDefaultProject();
                queryTask = _objectDbAccess.GetFlexFieldObjectsInRootFolderForProject(project.Id, start, pageSize);
                countTask = _objectDbAccess.GetFlexFieldObjectsInRootFolderCount(project.Id);
            }
            else
            {
                queryTask = _objectDbAccess.GetFlexFieldObjectsInFolder(parentId, start, pageSize);
                countTask = _objectDbAccess.GetFlexFieldObjectsInFolderCount(parentId);
            }

            Task.WaitAll(queryTask, countTask);

            FlexFieldObjectQueryResult queryResult = new FlexFieldObjectQueryResult();
            queryResult.FlexFieldObjects = queryTask.Result;
            queryResult.HasMore = start + queryResult.FlexFieldObjects.Count < countTask.Result;
            return Ok(queryResult);
        }

        /// <summary>
        /// Searches flex field objects
        /// </summary>
        /// <param name="searchPattern">Search Pattern</param>
        /// <param name="start">Start of the page</param>
        /// <param name="pageSize">Page Size</param>
        /// <returns>Flex field objects</returns>
        [HttpGet]
        public async Task<IActionResult> SearchFlexFieldObjects(string searchPattern, int start, int pageSize)
        {
            GoNorthProject project = await _projectDbAccess.GetDefaultProject();

            Task<List<T>> queryTask;
            Task<int> countTask;
            queryTask = _objectDbAccess.SearchFlexFieldObjects(project.Id, searchPattern, start, pageSize);
            countTask = _objectDbAccess.SearchFlexFieldObjectsCount(project.Id, searchPattern);
            Task.WaitAll(queryTask, countTask);

            FlexFieldObjectQueryResult queryResult = new FlexFieldObjectQueryResult();
            queryResult.FlexFieldObjects = queryTask.Result;
            queryResult.HasMore = start + queryResult.FlexFieldObjects.Count < countTask.Result;
            return Ok(queryResult);
        }

        /// <summary>
        /// Resolves the names for a list of flex field objects
        /// </summary>
        /// <param name="objectIds">Flex field object Ids</param>
        /// <returns>Resolved names</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveFlexFieldObjectNames([FromBody]List<string> objectIds)
        {
            List<T> objectNames = await _objectDbAccess.ResolveFlexFieldObjectNames(objectIds);
            return Ok(objectNames);
        }

        /// <summary>
        /// Creates a new flex field object
        /// </summary>
        /// <param name="flexFieldObject">Flex Field object to create</param>
        /// <returns>Result</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFlexFieldObject([FromBody]T flexFieldObject)
        {
            if(string.IsNullOrEmpty(flexFieldObject.Name))
            {
                return StatusCode((int)HttpStatusCode.BadRequest);
            }

            FlexFieldApiUtil.SetFieldIdsForNewFields(flexFieldObject.Fields);

            if(flexFieldObject.Tags == null)
            {
                flexFieldObject.Tags = new List<string>();
            }

            try
            {
                GoNorthProject project = await _projectDbAccess.GetDefaultProject();
                flexFieldObject.ProjectId = project.Id;

                flexFieldObject = await RunAdditionalUpdates(flexFieldObject, flexFieldObject);

                await this.SetModifiedData(_userManager, flexFieldObject);

                flexFieldObject = await _objectDbAccess.CreateFlexFieldObject(flexFieldObject);
                await AddNewTags(flexFieldObject.Tags);
                await _timelineService.AddTimelineEntry(ObjectCreatedEvent, flexFieldObject.Name, flexFieldObject.Id);
                return Ok(flexFieldObject);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Could not create flex field object {0}", flexFieldObject.Name);
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Checks if a object is referenced before a delete
        /// </summary>
        /// <param name="id">Id</param>
        /// <returns>Empty string if no references exists, error string if references exists</returns>
        protected abstract Task<string> CheckObjectReferences(string id);

        /// <summary>
        /// Deletes additional depencendies for a flex field object
        /// </summary>
        /// <param name="flexFieldObject">Flex field object to delete</param>
        /// <returns>Task</returns>
        protected abstract Task DeleteAdditionalFlexFieldObjectDependencies(T flexFieldObject);

        /// <summary>
        /// Deletes a flex field object
        /// </summary>
        /// <param name="id">Id of the flex field object</param>
        /// <returns>Result Status Code</returns>
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFlexFieldObject(string id)
        {
            // Check references
            string referenceError = await CheckObjectReferences(id);
            if(!string.IsNullOrEmpty(referenceError))
            {
                return StatusCode((int)HttpStatusCode.BadRequest, referenceError);
            }

            // Delete Object and dialog
            T flexFieldObject = await _objectDbAccess.GetFlexFieldObjectById(id);
            await _objectDbAccess.DeleteFlexFieldObject(flexFieldObject);
            _logger.LogInformation("Flex Field was deleted.");

            await DeleteAdditionalFlexFieldObjectDependencies(flexFieldObject);

            await RemoveUnusedTags(flexFieldObject.Tags);

            if(!string.IsNullOrEmpty(flexFieldObject.ImageFile))
            {
                _imageAccess.CheckAndDeleteUnusedImage(flexFieldObject.ImageFile);
            }

            await _timelineService.AddTimelineEntry(ObjectDeletedEvent, flexFieldObject.Name);
            return Ok(id);
        }

        /// <summary>
        /// Runs additional updates on a flex field object
        /// </summary>
        /// <param name="flexFieldObject">Flex Field Object</param>
        /// <param name="loadedFlexFieldObject">Loaded Flex Field Object</param>
        /// <returns>Updated flex field object</returns>
        protected abstract Task<T> RunAdditionalUpdates(T flexFieldObject, T loadedFlexFieldObject);

        /// <summary>
        /// Compares an object with the implementation snapshot
        /// </summary>
        /// <param name="flexFieldObject">Flex field object for compare</param>
        /// <returns>CompareResult Result</returns>
        protected abstract Task<CompareResult> CompareObjectWithImplementationSnapshot(T flexFieldObject);

        /// <summary>
        /// Sets the not implemented flag for an object on a relevant change
        /// </summary>
        /// <param name="newState">New State of the object</param>
        private async Task SetNotImplementedFlagOnChange(T newState)
        {
            if(!newState.IsImplemented)
            {
                return;
            }

            CompareResult result = await CompareObjectWithImplementationSnapshot(newState);
            if(result.CompareDifference != null && result.CompareDifference.Count > 0)
            {
                newState.IsImplemented = false;
            }
        }

        /// <summary>
        /// Updates a flex field object 
        /// </summary>
        /// <param name="id">Flex field object Id</param>
        /// <param name="flexFieldObject">Update flex field object data</param>
        /// <returns>Result Status Code</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFlexFieldObject(string id, [FromBody]T flexFieldObject)
        {
            T loadedFlexFieldObject = await _objectDbAccess.GetFlexFieldObjectById(id);
            
            List<string> oldTags = loadedFlexFieldObject.Tags;
            if(oldTags == null)
            {
                oldTags = new List<string>();
            }
            if(flexFieldObject.Tags == null)
            {
                flexFieldObject.Tags = new List<string>();
            }

            FlexFieldApiUtil.SetFieldIdsForNewFields(flexFieldObject.Fields);

            loadedFlexFieldObject.Name = flexFieldObject.Name;
            loadedFlexFieldObject.Fields = flexFieldObject.Fields;
            loadedFlexFieldObject.Tags = flexFieldObject.Tags;

            loadedFlexFieldObject = await RunAdditionalUpdates(flexFieldObject, loadedFlexFieldObject);

            await this.SetModifiedData(_userManager, loadedFlexFieldObject);

            await SetNotImplementedFlagOnChange(loadedFlexFieldObject);

            await _objectDbAccess.UpdateFlexFieldObject(loadedFlexFieldObject);
            _logger.LogInformation("Flex field object was updated.");

            await AddNewTags(flexFieldObject.Tags.Except(oldTags, StringComparer.OrdinalIgnoreCase).ToList());
            await RemoveUnusedTags(oldTags.Except(flexFieldObject.Tags, StringComparer.OrdinalIgnoreCase).ToList());
            _logger.LogInformation("Tags were updated.");

            await _timelineService.AddTimelineEntry(ObjectUpdatedEvent, loadedFlexFieldObject.Name, loadedFlexFieldObject.Id);

            return Ok(loadedFlexFieldObject);
        }


        /// <summary>
        /// Uploads an image to a flex field object
        /// </summary>
        /// <param name="id">Id of the flex field object</param>
        /// <returns>Image Name</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FlexFieldImageUpload(string id)
        {
            return await UploadImage(_objectDbAccess, ObjectImageUploadEvent, id);
        }

        /// <summary>
        /// Uploads an image to a flex field template
        /// </summary>
        /// <param name="id">Id of the template</param>
        /// <returns>Image Name</returns>
        protected async Task<IActionResult> BaseFlexFieldTemplateImageUpload(string id)
        {
            return await UploadImage(_templateDbAccess, TemplateImageUploadEvent, id);
        }

        /// <summary>
        /// Uploads an image to a flex field object or template
        /// </summary>
        /// <param name="dbAccess">Db access to use (tempate or object)</param>
        /// <param name="timelineEvent">Timeline Event to use</param>
        /// <param name="id">Id of the flex field object</param>
        /// <returns>Image Name</returns>
        private async Task<IActionResult> UploadImage(IFlexFieldObjectDbAccess<T> dbAccess, TimelineEvent timelineEvent, string id)
        {
            // Validate Date
            string validateResult = this.ValidateImageUploadData();
            if(validateResult != null)
            {
                return StatusCode((int)HttpStatusCode.BadRequest, _localizer[validateResult]);
            }

            IFormFile uploadFile = Request.Form.Files[0];
            T targetFlexFieldObject = await dbAccess.GetFlexFieldObjectById(id);
            if(targetFlexFieldObject == null)
            {
                return StatusCode((int)HttpStatusCode.BadRequest, _localizer["CouldNotUploadImage"]);
            }

            // Save Image
            string objectImageFile = string.Empty;
            try
            {
                using(Stream imageStream = _imageAccess.CreateFlexFieldObjectImage(uploadFile.FileName, out objectImageFile))
                {
                    uploadFile.CopyTo(imageStream);
                }

                string oldImageFile = targetFlexFieldObject.ImageFile;
                targetFlexFieldObject.ImageFile = objectImageFile;
                targetFlexFieldObject.IsImplemented = false;

                await this.SetModifiedData(_userManager, targetFlexFieldObject);

                await dbAccess.UpdateFlexFieldObject(targetFlexFieldObject);

                if(!string.IsNullOrEmpty(oldImageFile))
                {
                    _imageAccess.CheckAndDeleteUnusedImage(oldImageFile);
                }

                await _timelineService.AddTimelineEntry(timelineEvent, targetFlexFieldObject.Name, targetFlexFieldObject.Id);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Could not upload image");
                return StatusCode((int)HttpStatusCode.InternalServerError, _localizer["CouldNotUploadImage"]);
            }

            return Ok(objectImageFile);
        }

        /// <summary>
        /// Returns a flex field object image
        /// </summary>
        /// <param name="imageFile">Image File</param>
        /// <returns>Flex Field Image</returns>
        [HttpGet]
        public IActionResult FlexFieldObjectImage(string imageFile)
        {
            if(string.IsNullOrEmpty(imageFile))
            {
                return StatusCode((int)HttpStatusCode.BadRequest);
            }

            string fileExtension = Path.GetExtension(imageFile);
            string mimeType = this.GetImageMimeTypeForExtension(fileExtension);
            if(string.IsNullOrEmpty(mimeType))
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            Stream imageStream = _imageAccess.OpenFlexFieldObjectImage(imageFile);
            return File(imageStream, mimeType);
        }


        /// <summary>
        /// Returns all flex field object Tags
        /// </summary>
        /// <returns>All flex field object Tags</returns>
        public async Task<IActionResult> FlexFieldObjectTags()
        {
            List<string> allTags = await _tagDbAccess.GetAllTags();
            return Ok(allTags);
        }

        /// <summary>
        /// Adds new tags
        /// </summary>
        /// <param name="tagsToCheck">Tags to check</param>
        /// <returns>Task</returns>
        private async Task AddNewTags(List<string> tagsToCheck)
        {
            if(tagsToCheck == null || tagsToCheck.Count == 0)
            {
                return;
            }

            List<string> existingTags = await _tagDbAccess.GetAllTags();
            if(existingTags == null)
            {
                existingTags = new List<string>();
            }

            List<string> newTags = tagsToCheck.Except(existingTags, StringComparer.OrdinalIgnoreCase).ToList();
            foreach(string curNewTag in newTags)
            {
                await _tagDbAccess.AddTag(curNewTag);
            }
        }

        /// <summary>
        /// Removes unused tags
        /// </summary>
        /// <param name="tagsToCheck">Tags to check</param>
        /// <returns>Task</returns>
        private async Task RemoveUnusedTags(List<string> tagsToCheck)
        {
            if(tagsToCheck == null || tagsToCheck.Count == 0)
            {
                return;
            }

            foreach(string curDeleteTag in tagsToCheck)
            {
                Task<bool> objectUsingTag = _objectDbAccess.AnyFlexFieldObjectUsingTag(curDeleteTag);
                Task<bool> templateUsingTag = _templateDbAccess.AnyFlexFieldObjectUsingTag(curDeleteTag);
                Task.WaitAll(objectUsingTag, templateUsingTag);
                if(objectUsingTag.Result || templateUsingTag.Result)
                {
                    continue;
                }

                await _tagDbAccess.DeleteTag(curDeleteTag);
            }
        }

    }
}