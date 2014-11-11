using Microsoft.DeepZoomTools;
using Microsoft.Practices.Unity;
using Phocalstream_Service.Service;
using Phocalstream_Shared;
using Phocalstream_Shared.Data;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Service;
using Phocalstream_Web.Application;
using Phocalstream_Web.Application.Data;
using Phocalstream_Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Data.Entity;
using Phocalstream_Shared.Data.Model.View;
using Phocalstream_Web.Models.ViewModels;

namespace Phocalstream_Web.Controllers
{
    public class SearchController : Controller
    {
        [Dependency]
        public IEntityRepository<Collection> CollectionRepository { get; set; }

        [Dependency]
        public IEntityRepository<Photo> PhotoRepository { get; set; }

        [Dependency]
        public IEntityRepository<User> UserRepository { get; set; }

        [Dependency]
        public IPhotoRepository PhotoRepo { get; set; }

        [Dependency]
        public IUnitOfWork Unit { get; set; }

        [Dependency]
        public IPhotoService PhotoService { get; set; }

        [Dependency]
        public ISearchService SearchService { get; set; }

        [Dependency]
        public ICollectionService CollectionService { get; set; }


        public ActionResult Index(int e = 0)
        {
            SearchModel model = new SearchModel();

            Collection first = CollectionRepository.Find(c => c.Type == CollectionType.SITE).OrderBy(c => Guid.NewGuid()).First();
            model.BackgroundImageID = first.CoverPhoto == null ? PhotoRepo.GetSiteDetails(first.Site).LastPhotoID : first.CoverPhoto.ID;

            model.AvailableTags = PhotoService.GetTagNames();
            model.SiteNames = SearchService.GetSiteNames();

            Phocalstream_Shared.Data.Model.Photo.User User = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            if (User != null)
            {
                UserCollectionList userCollectionModel = new UserCollectionList();
                userCollectionModel.User = User;
                userCollectionModel.Collections = CollectionRepository.Find(c => c.Owner.ID == User.ID && c.Type == CollectionType.USER, c => c.Photos).ToList();
                model.UserCollections = userCollectionModel;
            }


            if (e == 1)
            {
                ViewBag.Message = "Zero photos matched those parameters, please try again.";
            }
            else if (e == 2)
            {
                ViewBag.Message = "Please enter at least one (1) search parameter.";
            }

            return View(model);
        }
        
        public ActionResult TagSearch(string tag)
        {
            if (String.IsNullOrWhiteSpace(tag))
            {
                return RedirectToAction("Index", "Home", new { e = 2 });
            }
            else
            {
                SearchModel model = new SearchModel();
                model.Tags = tag;

                return RedirectToAction("AdvancedSearch", model);
            }
        }

        public ActionResult KnockoutAdvancedSearch(string hours, string months, string sites, string tags, string dates)
        {
            SearchModel model = new SearchModel();

            model.Sites = sites;
            model.Tags = tags;
            model.Dates = dates;
            model.Hours = hours;
            model.Months = months;

            return RedirectToAction("AdvancedSearch", model);
        }

        public ActionResult AdvancedSearch(SearchModel model)
        {
            //check if the model is empty
            if (model.IsEmpty())
            {
                return RedirectToAction("Index", new { e = 2 });
            }

            var collectionName = model.CreateCollectionName();
            var containerID = collectionName.GetHashCode().ToString();

            //check if the model exists
            Collection existingCollection = CollectionRepository.Find(c => c.ContainerID == containerID).FirstOrDefault();
            if (existingCollection != null)
            {
                return RedirectToAction("SearchResult", new { collectionID = existingCollection.ID });
            }
            //else, execute the search
            else
            {
                SearchMatches result = SearchService.Search(model);

                //if search yielded result, do proceed
                if (result.Ids.Count > 0)
                {
                    //save the collection
                    Collection c = new Collection() {
                        Name = collectionName,
                        ContainerID = containerID,
                        Type = CollectionType.SEARCH,
                        Status = CollectionStatus.PROCESSING,
                        Photos = result.Matches
                    };
                    CollectionRepository.Insert(c);
                    Unit.Commit();

                    /*
                     * Since we are currently not showing a pivotview of search results, 
                     * these manifests do not need to be generated.
                     * 
                    string searchPath = SearchService.ValidateAndGetSearchPath();
                    CollectionService.GenerateCollectionManifest(PhotoService.GetFileNames(result.Matches), 
                        Path.Combine(searchPath, containerID.ToString(), "collection.dzc"));
                    PhotoService.GeneratePivotManifest(searchPath, containerID.ToString(), String.Join(",", result.Ids.ToArray()), CollectionType.SEARCH);
                    */

                    return RedirectToAction("SearchResults", new { collectionID = c.ID });
                }
                //else, redirect back to search page
                else
                {
                    return RedirectToAction("Index", new { e = 1 });
                }
            }
        }

        public ActionResult SearchResults(int collectionID)
        {
            SearchResults model = new SearchResults();

            model.Collection = CollectionRepository.First(col => col.ID == collectionID);

            model.PhotoIdList = PhotoRepo.GetPhotoIdsForCollection(collectionID);
            model.PhotoCount = PhotoRepo.GetPhotoCountForCollection(collectionID);

            Phocalstream_Shared.Data.Model.Photo.User User = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            if (User != null)
            {
                UserCollectionList userCollectionModel = new UserCollectionList();
                userCollectionModel.User = User;
                userCollectionModel.Collections = CollectionRepository.Find(c => c.Owner.ID == User.ID && c.Type == CollectionType.USER, c => c.Photos).ToList();
                model.UserCollections = userCollectionModel;
            }

            int photosPerPage = 150;
            model.Partial = GetPartialModel(collectionID, 0, photosPerPage);

            return View(model);
        }

        public ActionResult SearchResultPartial(long collectionID, int index, int photosPerPage)
        {
            SearchResultPartial model = GetPartialModel(collectionID, index, photosPerPage);
            model.CollectionID = collectionID;
            return PartialView("_SearchGridPartial", model);
        }

        private SearchResultPartial GetPartialModel(long collectionID, int index, int photosPerPage)
        {
            int photoCount = PhotoRepo.GetPhotoCountForCollection(collectionID);
            var pages = Math.Ceiling((Double) photoCount / (Double) photosPerPage);

            SearchResultPartial partial = new SearchResultPartial();
            partial.CollectionID = collectionID;
            partial.Index = index;
            partial.TotalPages = Convert.ToInt32(pages);
            partial.PhotosPerPage = photosPerPage;

            var startIndex = index * photosPerPage;
            var endIndex = (index + 1) * photosPerPage;

            // there are more photos than this is asking for, so create a partial with PhotosPerPage images
            if (photoCount > endIndex)
            {
                partial.Photos = PhotoRepo.GetPhotoRangeForCollection(collectionID, (startIndex), photosPerPage);
                partial.Description = "Showing photos " + (startIndex + 1).ToString() + " to " + (startIndex + photosPerPage).ToString();
            }
            else
            {
                var remainingCount = photoCount - startIndex;
                partial.Photos = PhotoRepo.GetPhotoRangeForCollection(collectionID, (startIndex), remainingCount);
                partial.Description = "Showing photos " + (startIndex + 1).ToString() + " to " + (startIndex + remainingCount).ToString();
            }

            return partial;
        }

        public ActionResult List()
        {
            SearchList model = new SearchList();

            model.SearchPath = PathManager.GetSearchPath();

            List<Collection> collections = CollectionRepository.Find(c => c.Type == CollectionType.SEARCH).ToList();

            model.Collections = collections;

            return View(model);
        }

        public ActionResult DeleteSearch(long collectionID)
        {
            SearchService.DeleteSearch(collectionID);
            return RedirectToAction("List", "Search");
        }

        public ActionResult DeleteAllSearches()
        {
            SearchService.DeleteAllSearches();
            return RedirectToAction("List", "Search");
        }
    }
}
