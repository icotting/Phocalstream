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

        // Enable both Get and Post so that our jquery call can send data, and get a status
        [HttpGet]
        [HttpPost]
        async public Task<HttpResponseMessage> Upload()
        {
            // Check if the request contains multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            string root = HttpContext.Current.Server.MapPath("~/App_Data");
            var provider = new MultipartFormDataStreamProvider(root);

            try
            {
                // Read the form data.
                await Request.Content.ReadAsMultipartAsync(provider);

                // This illustrates how to get the file names.
                foreach (MultipartFileData file in provider.FileData)
                {
                    Console.WriteLine(file.Headers.ContentDisposition.FileName);
                    Console.WriteLine("Server file path: " + file.LocalFileName);
                }
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (System.Exception e)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }
    }
}
