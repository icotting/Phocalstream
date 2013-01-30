using Phocalstream_Shared.Data.Model.Photo;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Policy;
using System.Web;

namespace Phocalstream_Web.Models.ViewModels
{
    public class HomeViewModel
    {
        public ICollection<Collection> Collections { get; set; }
    }
}