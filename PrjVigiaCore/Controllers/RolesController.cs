using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace PrjVigiaCore.Controllers
{
    [Authorize]
    public class RolesController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string cad_cn;

        public RolesController(IConfiguration configuration)
        {
            _configuration = configuration;
            cad_cn = configuration.GetConnectionString("cn1")!;
        }

        [AuthorizeMenu]
        [HttpGet]
        public async Task<IActionResult> ListarRoles()
        {
            try
            {
                List<dynamic> roles = new List<dynamic>();

                using var conn = new SqlConnection(cad_cn);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("SP_LISTAR_ROLES", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    dynamic rol = new System.Dynamic.ExpandoObject();
                    rol.ID_ROL = reader["ID_ROL"] != DBNull.Value ? reader["ID_ROL"] : null;
                    rol.ROL = reader["ROL"] != DBNull.Value ? reader["ROL"] : null;
                    rol.DESCRIPCION = reader["DESCRIPCION"] != DBNull.Value ? reader["DESCRIPCION"] : null;
                    rol.ESTADO = reader["ESTADO"] != DBNull.Value ? reader["ESTADO"] : null;
                    rol.FECHA_REGISTRO = reader["FECHA_REGISTRO"] != DBNull.Value ? (DateTime?)reader["FECHA_REGISTRO"] : null;
                    rol.FECHA_MODIFICACION = reader["FECHA_MODIFICACION"] != DBNull.Value ? (DateTime?)reader["FECHA_MODIFICACION"] : null;

                    roles.Add(rol);
                }

                ViewBag.TotalRegistros = roles.Count;

                return View(roles);
            }
            catch (Exception)
            {
                ViewBag.Error = "¡Ups! Algo salió mal. Inténtalo de nuevo más tarde.";
                return View(new List<dynamic>());
            }
        }


        [HttpPost]
        [AuthorizeMenu]
        public async Task<IActionResult> RegistrarRol(
            [FromForm] string idRol,
            [FromForm] string rol,
            [FromForm] string descripcion)
        {
            try
            {
                // Validaciones básicas
                if (string.IsNullOrEmpty(idRol) ||
                    string.IsNullOrEmpty(rol) ||
                    string.IsNullOrEmpty(descripcion))
                {
                    return Json(new { success = false, message = "Todos los campos son obligatorios" });
                }

                using (SqlConnection cnn = new SqlConnection(cad_cn))
                {
                    await cnn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("SP_REGISTRAR_ROLES", cnn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetros según el procedimiento
                        cmd.Parameters.AddWithValue("@ID_ROL", idRol);
                        cmd.Parameters.AddWithValue("@ROL", rol);
                        cmd.Parameters.AddWithValue("@DESCRIPCION", descripcion);

                        // Executar y leer resultado
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                int success = Convert.ToInt32(reader["Success"]);
                                string message = reader["Mensaje"].ToString();

                                return Json(new
                                {
                                    success = success == 1,
                                    message = message
                                });
                            }
                        }
                    }
                }

                // Si no se obtuvo resultado
                return Json(new { success = false, message = "¡Ups! Algo salió mal. Inténtalo de nuevo más tarde." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al registrar el Rol." });
            }
        }


        [AuthorizeMenu]
        [HttpGet]
        public async Task<IActionResult> ListarPermisos()
        {
            try
            {
                List<dynamic> permisos = new List<dynamic>();

                using var conn = new SqlConnection(cad_cn);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("SP_LISTAR_PERMISOS", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    dynamic permiso = new System.Dynamic.ExpandoObject();

                    permiso.ID_ROL = reader["ID_ROL"] != DBNull.Value ? reader["ID_ROL"] : null;
                    permiso.ROL = reader["ROL"] != DBNull.Value ? reader["ROL"] : null;
                    permiso.ID_MENU = reader["ID_MENU"] != DBNull.Value ? reader["ID_MENU"] : null;
                    permiso.NOMBRE_MENU = reader["NOMBRE"] != DBNull.Value ? reader["NOMBRE"] : null;
                    permiso.FECHA_CREACION = reader["FECHA_CREACION"] != DBNull.Value ? (DateTime?)reader["FECHA_CREACION"] : null;
                    permiso.FECHA_MODIFICACION = reader["FECHA_MODIFICACION"] != DBNull.Value ? (DateTime?)reader["FECHA_MODIFICACION"] : null;

                    permisos.Add(permiso);
                }

                ViewBag.TotalRegistros = permisos.Count;

                return View(permisos);
            }
            catch (Exception)
            {
                ViewBag.Error = "¡Ups! Algo salió mal. Inténtalo de nuevo más tarde.";
                return View(new List<dynamic>());
            }
        }


        [HttpPost]
        public async Task<IActionResult> RegistrarPermiso(
            [FromForm] string idRol,
            [FromForm] string idMenu)
        {
            try
            {
                if (string.IsNullOrEmpty(idRol) || string.IsNullOrEmpty(idMenu))
                {
                    return Json(new { success = false, message = "Todos los campos son obligatorios" });
                }

                using (SqlConnection cnn = new SqlConnection(cad_cn))
                {
                    await cnn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("SP_REGISTRAR_PERMISOS", cnn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetros según el procedimiento
                        cmd.Parameters.AddWithValue("@ID_ROL", idRol);
                        cmd.Parameters.AddWithValue("@ID_MENU", idMenu);

                        // Executar y leer resultado
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                int success = Convert.ToInt32(reader["Success"]);
                                string message = reader["Mensaje"].ToString();

                                return Json(new
                                {
                                    success = success == 1,
                                    message = message
                                });
                            }
                        }
                    }
                }

                return Json(new { success = false, message = "¡Ups! Algo salió mal. Inténtalo de nuevo más tarde." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al registrar el permiso." });
            }
        }

        [HttpGet]
        public IActionResult ObtenerMenusPorRol(string idRol)
        {
            try
            {
                var menus = new List<dynamic>();
                using (SqlConnection cnn = new SqlConnection(cad_cn))
                {
                    cnn.Open();

                    string query = @"SELECT 
                                m.ID_MENU,
                                m.NOMBRE
                            FROM 
                                MENU m
                            LEFT JOIN PERMISOS_ROL pr
                                ON pr.ID_MENU = m.ID_MENU
                                AND pr.ID_ROL = @idRol
                            WHERE 
                                pr.ID_ROL IS NULL";

                    using (SqlCommand cmd = new SqlCommand(query, cnn))
                    {
                        cmd.Parameters.AddWithValue("@idRol", idRol);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                menus.Add(new
                                {
                                    idMenu = reader["ID_MENU"].ToString(),
                                    nombre = reader["NOMBRE"].ToString()
                                });
                            }
                        }
                    }
                }
                return Json(menus);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error al cargar los menús: " + ex.Message });
            }
        }
    }
}
