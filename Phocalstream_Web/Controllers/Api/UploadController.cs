using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web;
using System.Threading;
using System.Web.UI;
using System.IO;
using Phocalstream_Service.Service;
using Microsoft.Practices.Unity;
using Phocalstream_Shared.Service;
using Phocalstream_Shared.Data.Model.Photo;
using Phocalstream_Shared.Data;
using System.Threading.Tasks;

namespace Phocalstream_Web.Controllers.Api
{
    public class UploadController : ApiController
    {
        [Dependency]
        public IPhotoService PhotoService { get; set; }

        [Dependency]
        public IEntityRepository<User> UserRepository { get; set; }

        [HttpGet]
        [HttpPost]
        public HttpResponseMessage Upload(long selectedCollectionID)
        {
            // Get a reference to the file that our jQuery sent.  Even with multiple files, they will all be their own request and be the 0 index
            HttpPostedFile file = HttpContext.Current.Request.Files[0];

            var temp = Path.Combine(PathManager.GetUserCollectionPath(), "Temp");
            if (!Directory.Exists(temp))
            {
                Directory.CreateDirectory(temp);
            }

            var path = Path.Combine(PathManager.GetUserCollectionPath(), "Temp", Path.GetFileName(file.FileName));
            file.SaveAs(path);

            User user = UserRepository.First(u => u.ProviderID == this.User.Identity.Name);
            PhotoService.ProcessUserPhoto(path, user, selectedCollectionID);

            // Now we need to wire up a response so that the calling script understands what happened
            HttpContext.Current.Response.ContentType = "text/plain";
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            var result = new { name = file.FileName };

            HttpContext.Current.Response.Write(serializer.Serialize(result));
            HttpContext.Current.Response.StatusCode = 200;

            // For compatibility with IE's "done" event we need to return a result as well as setting the context.response
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
