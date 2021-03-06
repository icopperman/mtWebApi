﻿using System;
using System.Collections.Generic;
using System.Linq;
using f=System.Net.Http.Formatting;
using p = WebApiContrib.Formatting.Jsonp;
using System.Web.Http;

namespace WebApplication4
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            config.EnableCors();

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            f.MediaTypeFormatterCollection mtfc = config.Formatters;
            
            f.XmlMediaTypeFormatter xmtf =  mtfc.XmlFormatter;

            bool rc = mtfc.Remove(xmtf);
            var jsonp = new p.JsonpMediaTypeFormatter(mtfc.JsonFormatter);
            mtfc.Insert(0, jsonp);
 
            int i = 0;
        }
    }
}
