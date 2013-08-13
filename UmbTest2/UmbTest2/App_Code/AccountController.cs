using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using umbraco.cms.businesslogic.member;

namespace Koiak.StandardWebsite
{
    /// <summary>
    /// Summary description for ContactController
    /// </summary>
    public class AccountSurfaceController : Umbraco.Web.Mvc.SurfaceController
    {
        [HttpPost]
        public ActionResult LoginForm(LoginModel model)
        {
            //model not valid, do not save, but return current umbraco page
            if (!ModelState.IsValid)
            {
                //Perhaps you might want to add a custom message to the TempData or ViewBag
                //which will be available on the View when it renders (since we're not 
                //redirecting)          
                return CurrentUmbracoPage();
            }

            // Login
            if (Membership.ValidateUser(model.Username, model.Password))
            {
                FormsAuthentication.SetAuthCookie(model.Username, false);
                return Redirect("/client-area.aspx");
            }
            else
            {
                ModelState.AddModelError("Username", "Username is not valid");
                return CurrentUmbracoPage();
            }
        }
    }

    public class LoginModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)] 
        public string Password { get; set; }
    }
}