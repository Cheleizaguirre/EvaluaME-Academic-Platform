using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Security.Claims;

namespace Mudul.Helpers
{
    public static class LayoutHelper
    {
        // Diccionario que mapea controladores con clases de fondo
        private static readonly Dictionary<string, string> _controllerBackgrounds =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
        { "Teacher", "bg-info" },
        { "Student", "bg-lightblue" },
        { "Coordinator", "bg-olive" },
        { "Admin", "bg-navy" }
        };

        public static string IsActive(this IHtmlHelper htmlHelper, string action, string controller, object routeParams = null)
        {
            var routeData = htmlHelper.ViewContext.RouteData.Values;
            var currentController = routeData["controller"]?.ToString();
            var currentAction = routeData["action"]?.ToString();

            bool isActive =
                string.Equals(controller, currentController, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(action, currentAction, StringComparison.OrdinalIgnoreCase);

            // Si hay parámetros adicionales, verificarlos también
            if (routeParams != null)
            {
                var paramDict = HtmlHelper.AnonymousObjectToHtmlAttributes(routeParams);
                foreach (var param in paramDict)
                {
                    if (!routeData.ContainsKey(param.Key) || routeData[param.Key]?.ToString() != param.Value?.ToString())
                    {
                        isActive = false;
                        break;
                    }
                }
            }

            if (!isActive)
                return string.Empty;

            // Obtener el rol del usuario autenticado
            var user = htmlHelper.ViewContext.HttpContext.User;
            var userRole = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            // Asignar clase de fondo en base al rol
            if (!string.IsNullOrEmpty(userRole) && _controllerBackgrounds.TryGetValue(userRole, out string backgroundClass))
            {
                return $"active {backgroundClass}";
            }

            return "active";
        }

    }
}
