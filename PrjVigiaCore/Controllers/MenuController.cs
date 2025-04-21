using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using PrjVigiaCore.DAO;

[Route("Menu")]
public class MenuController : Controller
{
    private readonly IConfiguration _configuration;

    private readonly string cad_cn;

    public MenuController(IConfiguration configuration)
    {
        _configuration = configuration;
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
        string tipoUsuario = "1"; // Por defecto cliente
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
                        string? idClientCrypt = null;

                        if (!string.IsNullOrEmpty(idCliente))
                        {
                            idClientCrypt = CryptoHelper.Encrypt(idCliente);
                        }
                        menus.Add(new
                        {
                            idMenu = reader["ID_MENU"].ToString(),
                            idCliente = idClientCrypt,
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

}
