using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using PrjVigiaCore.Services;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[Route("Menu")]
public class MenuController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ITokenService _tokenService;
    private readonly string cad_cn;

    public MenuController(IConfiguration configuration, ITokenService tokenService)
    {
        _configuration = configuration;
        _tokenService = tokenService;
        cad_cn = configuration.GetConnectionString("cn1")!;
    }

    [HttpGet("ObtenerMenus")]
    public IActionResult ObtenerMenus()
    {
        string? idRol = HttpContext.Session.GetString("ID_Rol");

        if (string.IsNullOrEmpty(idRol))
        {
            return Unauthorized(new { message = "No tienes permisos para acceder." });
        }

        // Obtener el tipo de usuario
        string tipoUsuario = "1";
        using (SqlConnection conn = new SqlConnection(cad_cn))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand("SELECT TIPO_USU FROM ROLES WHERE ID_ROL = @ID_ROL", conn))
            {
                cmd.Parameters.AddWithValue("@ID_ROL", idRol);
                object result = cmd.ExecuteScalar();
                if (result != null)
                {
                    tipoUsuario = result.ToString()!;
                }
            }
        }

        List<object> menus = new List<object>();
        using (SqlConnection conn = new SqlConnection(cad_cn))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand("SP_OBTENER_MENUS", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_ROL", idRol);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string? idCliente = reader["ID_CLIENTE"].ToString();
                        string? token = null;

                        if (!string.IsNullOrEmpty(idCliente))
                        {
                            token = _tokenService.GenerarToken(idCliente);
                        }
                        menus.Add(new
                        {
                            idMenu = reader["ID_MENU"].ToString(),
                            token, // Enviamos token en lugar del ID
                            nombre = reader["NOMBRE"].ToString(),
                            url = reader["URL"] != DBNull.Value ? reader["URL"].ToString() : "#",
                            icono = reader["ICONO"] != DBNull.Value ? reader["ICONO"].ToString() : "",
                            orden = Convert.ToInt32(reader["ORDEN"]),
                            idPadre = reader["ID_PADRE"] != DBNull.Value ? reader["ID_PADRE"].ToString() : null
                        });
                    }
                }
            }
        }

        return Json(new { tipoUsuario, menus });
    }

    [AuthorizeMenu]
    [HttpGet("ListarMenus")]
    public async Task<IActionResult> ListarMenusModulo()
    {
        try
        {
            List<dynamic> menus = new List<dynamic>();

            using var conn = new SqlConnection(cad_cn);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("SP_LISTAR_MODULO_MENUS", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                dynamic menu = new System.Dynamic.ExpandoObject();

                menu.ID_MENU = reader["ID_MENU"] != DBNull.Value ? reader["ID_MENU"] : null;
                menu.NOMBRE_CLIENTE = reader["NOMBRE_CLIENTE"] != DBNull.Value ? reader["NOMBRE_CLIENTE"] : null;
                menu.NOMBRE_MENU = reader["NOMBRE_MENU"] != DBNull.Value ? reader["NOMBRE_MENU"] : null;
                menu.URL = reader["URL"] != DBNull.Value ? reader["URL"].ToString() : string.Empty;
                menu.ICONO = reader["ICONO"] != DBNull.Value ? reader["ICONO"].ToString() : string.Empty;
                menu.ORDEN = reader["ORDEN"] != DBNull.Value ? reader["ORDEN"] : 0;
                menu.NOMBRE_MENU_PADRE = reader["NOMBRE_MENU_PADRE"] != DBNull.Value ? reader["NOMBRE_MENU_PADRE"] : null;
                menu.ESTADO = reader["ESTADO"] != DBNull.Value ? reader["ESTADO"] : false;

                menus.Add(menu);
            }

            ViewBag.TotalRegistros = menus.Count;

            return View("ListarMenus", menus);
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"¡Ups! Algo salió mal. Inténtalo de nuevo más tarde.";
            return View("ListarMenus", new List<dynamic>());
        }
    }

}