﻿using Implem.Libraries.Utilities;
using System.Web;
using System.Web.Routing;
namespace Implem.Pleasanter.Libraries.Requests
{
    public static class Routes
    {
        public static string Controller()
        {
            return RouteTable.Routes.Count != 0
                ? Url.RouteData("controller").ToString().ToLower()
                : StackTraces.Class();
        }

        public static string Action()
        {
            return RouteTable.Routes.Count != 0
                ? Url.RouteData("action").ToString().ToLower()
                : StackTraces.Method();
        }

        public static string Method()
        {
            return HttpContext.Current.Request.HttpMethod.ToLower();
        }
    }
}