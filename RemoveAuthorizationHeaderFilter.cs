// using System.Linq;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc.Authorization;
// using Microsoft.OpenApi.Models;
// using Swashbuckle.AspNetCore.SwaggerGen;

// public class RemoveAuthorizationHeaderParameterFilter : IOperationFilter
// {
//     public void Apply(OpenApiOperation operation, OperationFilterContext context)
//     {
//         var authHeaderParameter = operation.Parameters.SingleOrDefault(p => p.Name == "Authorization");
//         if (authHeaderParameter != null)
//         {
//             operation.Parameters.Remove(authHeaderParameter);
//         }
//     }
// }
