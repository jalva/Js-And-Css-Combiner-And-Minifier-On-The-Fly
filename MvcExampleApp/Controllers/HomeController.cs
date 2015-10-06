using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JsAndCssCombiner.InterceptorFilterImplementation;

namespace MvcCombinerTestApp.Controllers
{
    [HandleError]
    public class HomeController : Controller
    {
        [Combiner(false)]
        public ActionResult Index()
        {
            ViewData["Message"] = "Welcome to ASP.NET MVC!";

            return View();
        }

        public ActionResult About()
        {
            return View();
        }
    }
}
