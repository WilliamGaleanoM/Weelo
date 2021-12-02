using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Parser;
using System.Linq.Expressions;
using System.Linq.MyDynamic;
using System.Reflection;
using System.Threading.Tasks;

namespace PruebaEmpleados.Helpers
{
    public static class Helpers
    {
        public class Filtro
        {
            public string index;
            public string ColName;
            public string cFilter;
        }

        public class OrderFiltro
        {
            public string index;
            public string ColName;
            public string ShortOrder;
        }

        public static async Task<string> RenderViewAsync<TModel>(this Controller controller, string viewName, TModel model, bool partial = false)
        {
            if (string.IsNullOrEmpty(viewName))
            {
                viewName = controller.ControllerContext.ActionDescriptor.ActionName;
            }

            controller.ViewData.Model = model;

            using (var writer = new StringWriter())
            {
                IViewEngine viewEngine = controller.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
                ViewEngineResult viewResult = viewEngine.GetView(viewName,viewName, !partial);

                if (viewResult.Success == false)
                {
                    return $"A view with the name {viewName} could not be found";
                }

                ViewContext viewContext = new ViewContext(
                    controller.ControllerContext,
                    viewResult.View,
                    controller.ViewData,
                    controller.TempData,
                    writer,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);

                return writer.GetStringBuilder().ToString();
            }
        }

        /// <summary>
        /// Determines whether the specified HTTP request is an AJAX request.
        /// </summary>
        /// 
        /// <returns>
        /// true if the specified HTTP request is an AJAX request; otherwise, false.
        /// </returns>
        /// <param name="request">The HTTP request.</param><exception cref="T:System.ArgumentNullException">The <paramref name="request"/> parameter is null (Nothing in Visual Basic).</exception>
        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Headers != null)
                return request.Headers["X-Requested-With"] == "XMLHttpRequest";
            return false;
        }

        public static async Task<string> RenderViewToString(this Controller controller, String viewPath, object model = null, bool partial = false)
        {
            controller.ViewData.Model = model;
            using (var writer = new StringWriter())
            {

                IViewEngine viewEngine = controller.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
                ViewEngineResult viewResult = viewEngine.GetView(viewPath, viewPath, !partial);  

                ViewContext viewContext = new ViewContext(
                   controller.ControllerContext,
                   viewResult.View,
                   controller.ViewData,
                   controller.TempData,
                   writer,
                   new HtmlHelperOptions()
               );

                await viewResult.View.RenderAsync(viewContext);

                return writer.GetStringBuilder().ToString();
            }
        }

        public static IEnumerable<Filtro> Filtros_Request(this HttpRequest Request)
        {

            var busquedas = Request.Query.Where(f => f.Key.Contains("[search][value]") && (f.Value.ToString() ?? "") != "");
            var index_filtros = (from s in busquedas                         
                                 let col = Request.Query.Where(q => q.Key == "columns[" + int.Parse(s.Key.Substring(8, 3).Replace("[", "").Replace("]", "")) + "][name]").Select(v => v.Value).FirstOrDefault()
                                 select new Filtro() { index = s.Key, ColName = col, cFilter = s.Value }).ToList();

            return index_filtros;
        }

        public static IQueryable<T> WhereLike<T>(this IQueryable<T> source, String Name, String value, bool UpperCompare = false)
        {
            Type model = typeof(T);
            ParameterExpression param = Expression.Parameter(typeof(T), "m");
            PropertyInfo key = model.GetProperty(Name);
            MemberExpression lhs = Expression.MakeMemberAccess(param, key);

            if (lhs.Type.Name == "Int32")
            {
                return DynamicQueryable.Where(source, Name + " = @0", int.Parse(value.Replace("%", ""))).AsQueryable();
            }
            if (lhs.Type.Name == "Boolean")
            {
                bool var = false;
                value = value.Replace("%", "");
                try
                {
                    var = value.ToBoolean();
                }
                catch (Exception)
                {
                    Boolean.TryParse(value, out var);
                }

                return DynamicQueryableExtensions.Where(source, Name + " = @0", var).AsQueryable();
            }
            if (lhs.Type.Name == "DateTime")
            {
                DateTime var = DateTime.Now;
                value = value.Replace("%", "");
                try
                {
                    var = Convert.ToDateTime(value);
                }
                catch (Exception)
                {
                    DateTime.TryParse(value, out var);
                }
                return DynamicQueryableExtensions.Where(source, Name + ".Date = @0", var.Date).AsQueryable();
            }
            Expression<Func<T, String>> lambda = Expression.Lambda<Func<T, String>>(lhs, param);
            return source.Where(BuildLikeExpression(lambda, value, UpperCompare));
        }
        public static bool ToBoolean(this string value)
        {
            switch (value.ToLower())
            {
                case "true":
                case "verdadero":
                case "verdad":
                    return true;
                case "s":
                case "y":
                case "si":
                    return true;
                case "t":
                    return true;
                case "1":
                    return true;
                case "0":
                    return false;
                case "false":
                    return false;
                case "f":
                    return false;
                default:
                    return false;
            }
        }

        public static IQueryable<T> WhereLike<T>(this IQueryable<T> source, Expression<Func<T, String>> valueSelector, String value, bool UpperCompare = false)
        {
            return source.Where(BuildLikeExpression(valueSelector, value, UpperCompare));
        }
        public static Expression<Func<T, Boolean>> BuildLikeExpression<T>(Expression<Func<T, String>> valueSelector, String value, bool UpperCompare = false)
        {
            if (valueSelector == null)
                throw new ArgumentNullException("valueSelector");
            value = value.Replace("*", "%");        // this allows us to use '%' or '*' for our wildcard
            value = UpperCompare ? value.ToUpper() : value;
            if (value.Trim('%').Contains("%"))
            {
                Expression myBody = null;
                ParsedLike myParse = Parse(value);
                Type stringType = typeof(String);
                if (myParse.startwith != null)
                {
                    myBody = Expression.Call(valueSelector.Body, stringType.GetMethod("StartsWith", new Type[] { stringType }), Expression.Constant(myParse.startwith));
                }
                foreach (String contains in myParse.contains)
                {
                    if (myBody == null)
                    {
                        myBody = Expression.Call(valueSelector.Body, stringType.GetMethod("Contains", new Type[] { stringType }), Expression.Constant(contains));
                    }
                    else
                    {
                        Expression myInner = Expression.Call(valueSelector.Body, stringType.GetMethod("Contains", new Type[] { stringType }), Expression.Constant(contains));
                        myBody = Expression.And(myBody, myInner);
                    }
                }
                if (myParse.endwith != null)
                {
                    if (myBody == null)
                    {
                        myBody = Expression.Call(valueSelector.Body, stringType.GetMethod("EndsWith", new Type[] { stringType }), Expression.Constant(myParse.endwith));
                    }
                    else
                    {
                        Expression myInner = Expression.Call(valueSelector.Body, stringType.GetMethod("EndsWith", new Type[] { stringType }), Expression.Constant(myParse.endwith));
                        myBody = Expression.And(myBody, myInner);
                    }
                }
                return Expression.Lambda<Func<T, Boolean>>(myBody, valueSelector.Parameters.Single());
            }
            else
            {
                Type stringType = typeof(String);
                Expression myBody;

                if (UpperCompare)
                {
                    Expression myBody1 = Expression.Call(valueSelector.Body, "ToUpper", null);
                    myBody = Expression.Call(myBody1, GetLikeMethod(value), Expression.Constant(value.Trim('%')));
                }
                else
                {
                    myBody = Expression.Call(valueSelector.Body, GetLikeMethod(value), Expression.Constant(value.Trim('%')));
                }


                return Expression.Lambda<Func<T, Boolean>>(myBody, valueSelector.Parameters.Single());
            }
        }
        private static MethodInfo GetLikeMethod(String value)
        {
            Type stringType = typeof(String);

            if (value.EndsWith("%") && value.StartsWith("%"))
            {
                return stringType.GetMethod("Contains", new Type[] { stringType });
            }
            else if (value.EndsWith("%"))
            {
                return stringType.GetMethod("StartsWith", new Type[] { stringType });
            }
            else
            {
                return stringType.GetMethod("EndsWith", new Type[] { stringType });
            }
        }
        private class ParsedLike
        {
            public String startwith { get; set; }
            public String endwith { get; set; }
            public String[] contains { get; set; }
        }
        private static ParsedLike Parse(String inValue)
        {
            ParsedLike myParse = new ParsedLike();
            String work = inValue;
            Int32 loc;
            if (!work.StartsWith("%"))
            {
                work = work.TrimStart('%');
                loc = work.IndexOf("%");
                myParse.startwith = work.Substring(0, loc);
                work = work.Substring(loc + 1);
            }
            if (!work.EndsWith("%"))
            {
                loc = work.LastIndexOf('%');
                myParse.endwith = work.Substring(loc + 1);
                if (loc == -1)
                    work = String.Empty;
                else
                    work = work.Substring(0, loc);
            }
            myParse.contains = work.Split(new[] { '%' }, StringSplitOptions.RemoveEmptyEntries);
            return myParse;
        }
        public static Expression<Func<TSource, Tkey>> GetPropertyExpression<TSource, Tkey>(this IQueryable<TSource> source, string propertyName)
        {
            if (typeof(TSource).GetProperty(propertyName, BindingFlags.IgnoreCase |
                BindingFlags.Public | BindingFlags.Instance) == null)
            {
                return null;
            }
            var paramterExpression = Expression.Parameter(typeof(TSource));
            return (Expression<Func<TSource, Tkey>>)
                Expression.Lambda(Expression.PropertyOrField(
                    paramterExpression, propertyName), paramterExpression);
        }

    }
}
