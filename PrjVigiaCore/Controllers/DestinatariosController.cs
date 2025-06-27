using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace PrjVigiaCore.Controllers
{
    [Authorize]
    public class DestinatariosController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string cad_cn;

        public DestinatariosController(IConfiguration configuration)
        {
            _configuration = configuration;
            cad_cn = configuration.GetConnectionString("cn1")!;
        }

        [AuthorizeMenu]
        [HttpGet]
        public async Task<IActionResult> GrupoDestinatarios()
        {
            try
            {
                List<dynamic> grupos = new List<dynamic>();
                using var conn = new SqlConnection(cad_cn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SP_LISTAR_GRUPO_DESTINATARIOS", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    dynamic grupo = new System.Dynamic.ExpandoObject();
                    grupo.ID_GRUPO = reader["ID_GRUPO"] != DBNull.Value ? reader["ID_GRUPO"] : null;
                    grupo.NOM_GRUPO = reader["NOM_GRUPO"] != DBNull.Value ? reader["NOM_GRUPO"] : null;
                    grupo.DESCRIPCION = reader["DESCRIPCION"] != DBNull.Value ? reader["DESCRIPCION"] : null;
                    grupo.ESTADO = reader["ESTADO"] != DBNull.Value ? reader["ESTADO"] : null;
                    grupo.FECHA_REGISTRO = reader["FECHA_REGISTRO"] != DBNull.Value ? reader["FECHA_REGISTRO"] : null;
                    grupos.Add(grupo);
                }
                ViewBag.TotalRegistros = grupos.Count();

                return View(grupos);
            }
            catch (Exception)
            {
                ViewBag.Error = "¡Ups! Algo salió mal. Inténtalo de nuevo más tarde.";
                return View(new List<dynamic>());
            }
        }


        [HttpPost]
        public async Task<IActionResult> RegistrarGrupo(
            [FromForm] string idGrupo,
            [FromForm] string nombre,
            [FromForm] string descripcion)
        {
            try
            {
                // Validaciones básicas
                if (string.IsNullOrEmpty(idGrupo) || string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(descripcion))
                {
                    return Json(new { success = false, message = "Todos los campos son obligatorios" });
                }

                using (SqlConnection cnn = new SqlConnection(cad_cn))
                {
                    await cnn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("SP_REGISTRAR_GRUPO_DESTINA", cnn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetros requeridos por el SP
                        cmd.Parameters.AddWithValue("@ID_GRUPO", idGrupo);
                        cmd.Parameters.AddWithValue("@NOM_GRUPO", nombre);
                        cmd.Parameters.AddWithValue("@DESCRIPCION", descripcion);

                        // Ejecutar y leer resultado
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
                return Json(new { success = false, message = "¡Ups! Algo salió mas. Intentalo de nuevo mas tarde." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al registrar el grupo." });
            }
        }

        [AuthorizeMenu]
        [HttpGet]
        public async Task<IActionResult> ListarDestinatarios()
        {
            try
            {
                List<dynamic> destinatarios = new List<dynamic>();

                using var conn = new SqlConnection(cad_cn);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("SP_LISTAR_MODULO_DESTINATARIOS", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    dynamic destinatario = new System.Dynamic.ExpandoObject();
                    destinatario.ID_EMPLEADO = reader["ID_EMPLEADO"] != DBNull.Value ? reader["ID_EMPLEADO"] : null;
                    destinatario.NOMBRE_COMPLETO = reader["NOMBRE_COMPLETO"] != DBNull.Value ? reader["NOMBRE_COMPLETO"] : null;
                    destinatario.EMAIL = reader["EMAIL"] != DBNull.Value ? reader["EMAIL"] : null;
                    destinatario.NOM_GRUPOS = reader["NOM_GRUPOS"] != DBNull.Value ? reader["NOM_GRUPOS"] : null;
                    destinatario.ESTADO = reader["ESTADO"] != DBNull.Value ? reader["ESTADO"] : null;

                    destinatarios.Add(destinatario);
                }

                ViewBag.TotalRegistros = destinatarios.Count();

                return View(destinatarios);
            }
            catch (Exception)
            {
                ViewBag.Error = "¡Ups! Algo salió mal. Inténtalo de nuevo más tarde.";
                return View(new List<dynamic>());
            }
        }


        [HttpPost]
        public async Task<IActionResult> RegistrarDestinatario(
            [FromForm] string idDestinatario,
            [FromForm] string nombres,
            [FromForm] string apePat,
            [FromForm] string apeMat,
            [FromForm] string email,
            [FromForm] string idGrupo)
        {
            try
            {
                // Validaciones básicas
                if (string.IsNullOrEmpty(idDestinatario) ||
                    string.IsNullOrEmpty(nombres) ||
                    string.IsNullOrEmpty(apePat) ||
                    string.IsNullOrEmpty(apeMat) ||
                    string.IsNullOrEmpty(email) ||
                    string.IsNullOrEmpty(idGrupo))
                {
                    return Json(new { success = false, message = "Todos los campos son obligatorios" });
                }

                using (SqlConnection cnn = new SqlConnection(cad_cn))
                {
                    await cnn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("SP_REGISTRAR_DESTINATARIOS", cnn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetros requeridos por el SP
                        cmd.Parameters.AddWithValue("@ID_DESTINATARIO", idDestinatario);
                        cmd.Parameters.AddWithValue("@NOMBRES", nombres);
                        cmd.Parameters.AddWithValue("@APE_PAT", apePat);
                        cmd.Parameters.AddWithValue("@APE_MAT", apeMat);
                        cmd.Parameters.AddWithValue("@EMAIL", email);
                        cmd.Parameters.AddWithValue("@ID_GRUPO", idGrupo);

                        // Ejecutar y leer resultado
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
            catch (Exception)
            {
                return Json(new { success = false, message = "Error al registrar el destinatario." });
            }
        }


        [HttpGet]
        public async Task<IActionResult> BuscarEmpleado(string idEmpleado)
        {
            try
            {
                using (SqlConnection cnn = new SqlConnection(cad_cn))
                {
                    await cnn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("SP_BUSCAR_EMPLEADO", cnn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ID_EMPLEADO", idEmpleado);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return Json(new
                                {
                                    success = true,
                                    data = new
                                    {
                                        idEmpleado = reader["ID_EMPLEADO"].ToString(),
                                        nombres = reader["NOMBRES"].ToString(),
                                        apellidoPaterno = reader["APE_PAT"].ToString(),
                                        apellidoMaterno = reader["APE_MAT"].ToString(),
                                        email = reader["EMAIL"].ToString()
                                    }
                                });
                            }
                            else
                            {
                                return Json(new { success = false, message = "Empleado no encontrado" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al buscar empleado."});
            }
        }


        [HttpGet]
        public IActionResult ObtenerGrupos(string idEmpleado)
        {
            try
            {
                var grupos = new List<dynamic>();
                using (SqlConnection cnn = new SqlConnection(cad_cn))
                {
                    cnn.Open();

                    // Consulta para obtener los grupos que NO tiene asignados el empleado
                    string query = @"
                        SELECT g.ID_GRUPO, g.NOM_GRUPO 
                        FROM GRUPO_DESTINA g
                        WHERE g.ESTADO = 1
                        AND NOT EXISTS (
                            SELECT 1 FROM DESTINATARIOS d
                            WHERE d.ID_GRUPO = g.ID_GRUPO
                            AND d.ID_DESTINATARIO = @idEmpleado
                            AND d.ESTADO = 1
                        )";

                    using (SqlCommand cmd = new SqlCommand(query, cnn))
                    {
                        cmd.Parameters.AddWithValue("@idEmpleado", idEmpleado);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                grupos.Add(new
                                {
                                    idGrupo = reader["ID_GRUPO"].ToString(),
                                    nombre = reader["NOM_GRUPO"].ToString()
                                });
                            }
                        }
                    }
                }
                return Json(grupos);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error al obtener los grupos." });
            }
        }

    }
}
