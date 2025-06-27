using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data;
using System.Threading.Tasks;

public class AuthorizeMenuAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        if (EsPeticionAutorizada(httpContext))
        {
            await next();
            return;
        }

        // 2. Bloquear si no hay sesión
        var idRol = httpContext.Session.GetString("ID_Rol");
        if (string.IsNullOrEmpty(idRol))
        {
            context.Result = new UnauthorizedResult(); // 401
            return;
        }

        // 3. Verificar acceso en BD con manejo de errores
        try
        {
            var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
            bool tieneAcceso = await VerificarAccesoEnBD(idRol, httpContext.Request.Path, config);

            if (!tieneAcceso)
            {
                context.Result = new ForbidResult(); // 403
                return;
            }
        }
        catch (Exception ex) when (ex is SqlException || ex is InvalidOperationException)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            return;
        }

        await next();
    }

    private bool EsPeticionAutorizada(HttpContext httpContext)
    {
        return httpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
               !string.IsNullOrEmpty(httpContext.Request.Headers["Authorization"]);
    }

    private async Task<bool> VerificarAccesoEnBD(string idRol, string path, IConfiguration config)
    {
        await using var conn = new SqlConnection(config.GetConnectionString("cn1"));
        await conn.OpenAsync();

        // Usar EXISTS para mejor performance
        await using var cmd = new SqlCommand(
            @"SELECT CASE WHEN EXISTS (
                SELECT 1 
                FROM PERMISOS_ROL pm
                INNER JOIN MENU m ON pm.ID_Menu = m.ID_Menu
                WHERE pm.ID_ROL = @ID_Rol AND m.URL = @Ruta
            ) THEN 1 ELSE 0 END",
            conn);

        cmd.Parameters.AddWithValue("@ID_Rol", idRol);
        cmd.Parameters.AddWithValue("@Ruta", path);

        return (int)await cmd.ExecuteScalarAsync() == 1;
    }
}