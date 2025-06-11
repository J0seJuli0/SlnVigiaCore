using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrjVigiaCore.DAO;
using PrjVigiaCore.Services;
using System.Data;
using System.Data.SqlClient;

namespace PrjVigiaCore.Controllers
{
    [Route("Campos")]
    public class CamposController : Controller
    {
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly string cad_cn;

        public CamposController(IConfiguration configuration, ITokenService tokenService)
        {
            _tokenService = tokenService;
            _configuration = configuration;
            cad_cn = configuration.GetConnectionString("cn1")!;
        }

        [HttpGet("ListarCampos")]
        public async Task<IActionResult> ListarCampos()
        {
            try
            {
                List<dynamic> campos = new List<dynamic>();
                using var conn = new SqlConnection(cad_cn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SP_LISTAR_CAMPOS_MONITOREO", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    dynamic campo = new System.Dynamic.ExpandoObject();
                    campo.ID_CAMPO = reader["ID_CAMPO"] != DBNull.Value ? reader["ID_CAMPO"] : null;
                    campo.CAMPO = reader["CAMPO"] != DBNull.Value ? reader["CAMPO"] : null;
                    campo.TIPO_CAMPO = reader["TIPO_CAMPO"] != DBNull.Value ? reader["TIPO_CAMPO"] : null;
                    campo.ESTADO = reader["ESTADO"] != DBNull.Value ? reader["ESTADO"] : null;
                    campos.Add(campo);
                }
                ViewBag.TotalRegistros = campos.Count();
                return View(campos);
            }
            catch (Exception)
            {
                ViewBag.Error = "¡Ups! Algo salió mal. Intentalo de nuevo mas tarde.";
                return View(new List<dynamic>());
            }
        }

        [HttpGet("ListarCamposClientes")]
        public async Task<IActionResult> ListarCamposClientes()
        {
            try
            {
                List<dynamic> campos = new List<dynamic>();
                using var conn = new SqlConnection(cad_cn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SP_LISTAR_CLIENTE_CAMPOS", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    dynamic campo = new System.Dynamic.ExpandoObject();
                    campo.ID_CLIENTE = reader["ID_CLIENTE"] != DBNull.Value ? reader["ID_CLIENTE"] : null;
                    campo.NOMBRE = reader["NOMBRE"] != DBNull.Value ? reader["NOMBRE"] : null;
                    campo.ID_CAMPO = reader["ID_CAMPO"] != DBNull.Value ? reader["ID_CAMPO"] : null;
                    campo.CAMPO = reader["CAMPO"] != DBNull.Value ? reader["CAMPO"] : null;
                    campo.ESTADO = reader["ESTADO"] != DBNull.Value ? reader["ESTADO"] : null;
                    campos.Add(campo);
                }
                ViewBag.TotalRegistros = campos.Count();

                return View(campos);
            }
            catch (Exception)
            {
                ViewBag.Error = "¡Ups! Algo salió mal. Intentalo de nuevo mas tarde.";
                return View(new List<dynamic>());
            }
        }


        [HttpGet("ListarComponentes")]
        public async Task<IActionResult> ListarComponentes()
        {
            try
            {
                List<dynamic> componentes = new List<dynamic>();
                using var conn = new SqlConnection(cad_cn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SP_LISTAR_COMPONENTES", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    dynamic componente = new System.Dynamic.ExpandoObject();
                    componente.ID_COMPONENTE = reader["ID_COMPONENTE"] != DBNull.Value ? reader["ID_COMPONENTE"] : null;
                    componente.COMPONENTE = reader["COMPONENTE"] != DBNull.Value ? reader["COMPONENTE"] : null;
                    componente.DESCRIPCION = reader["DESCRIPCION"] != DBNull.Value ? reader["DESCRIPCION"] : null;
                    componente.ID_SERVER = reader["ID_SERVER"] != DBNull.Value ? reader["ID_SERVER"] : null;
                    componente.ALIAS = reader["ALIAS"] != DBNull.Value ? reader["ALIAS"] : null;
                    componente.IP_SERVER = reader["IP_SERVER"] != DBNull.Value ? reader["IP_SERVER"] : null;
                    componente.ESTADO = reader["ESTADO"] != DBNull.Value ? reader["ESTADO"] : null;
                    componentes.Add(componente);
                }
                ViewBag.TotalRegistros = componentes.Count();

                return View(componentes);
            }
            catch (Exception)
            {
                ViewBag.Error = "¡Ups! Algo salió mal. Intentalo de nuevo mas tarde.";
                return View(new List<dynamic>());
            }
        }


        [HttpGet("ListarServidores/{token?}")]
        public async Task<IActionResult> ListarServidores([FromQuery] string token = null)
        {
            try
            {
                string idCliente= null;

                if (!string.IsNullOrEmpty(token))
                {
                    idCliente = _tokenService.ObtenerIdCliente(token);
                    if (idCliente == null)
                    {
                        return BadRequest("Token inválido o expirado");
                    }
                }

                List<dynamic> servidores = new List<dynamic>();
                using var conn = new SqlConnection(cad_cn);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("SP_LISTAR_SERVIDORES", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                var param = cmd.Parameters.Add("@ID_CLIENTE", SqlDbType.VarChar, 8);
                param.Value = !string.IsNullOrEmpty(idCliente) ? idCliente : (object)DBNull.Value;

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    dynamic servidor = new System.Dynamic.ExpandoObject();
                    servidor.ID_SERVER = reader["ID_SERVER"] != DBNull.Value ? reader["ID_SERVER"] : null;
                    servidor.NOMBRE = reader["NOMBRE"] != DBNull.Value ? reader["NOMBRE"] : null;
                    servidor.ALIAS = reader["ALIAS"] != DBNull.Value ? reader["ALIAS"] : null;
                    servidor.IP_SERVER = reader["IP_SERVER"] != DBNull.Value ? reader["IP_SERVER"] : null;
                    servidor.ESTADO = reader["ESTADO"] != DBNull.Value ? reader["ESTADO"] : null;
                    servidores.Add(servidor);
                }
                ViewBag.TotalRegistros = servidores.Count();

                ViewBag.IdClienteFiltro = idCliente;
                ViewData["Title"] = string.IsNullOrEmpty(idCliente) ?
                    "Listado de Servidores" :
                    $"Servidores del Cliente {idCliente}";

                return View(servidores);
            }
            catch (Exception)
            {
                ViewBag.Error = "¡Ups! Algo salió mal. Intentalo de nuevo mas tarde.";
                return View(new List<dynamic>());
            }
        }


        [HttpPost("RegistrarCampo")]
        public async Task<IActionResult> RegistrarCampo(
            [FromForm] string idCampo,
            [FromForm] string campo,
            [FromForm] string tipoDato,
            [FromForm] byte tipoCampo)
        {
            try
            {
                if (string.IsNullOrEmpty(idCampo) || string.IsNullOrEmpty(campo) || string.IsNullOrEmpty(tipoDato))
                {
                    return Json(new { success = false, message = "Todos los campos obligatorios deben ser completados" });
                }

                // Validar tipo de dato
                var tiposValidos = new[] { "int", "string", "decimal", "date"};
                if (!tiposValidos.Contains(tipoDato.ToLower()))
                {
                    return Json(new { success = false, message = "Tipo de dato no válido" });
                }

                // Validar tipo de campo
                if (tipoCampo < 1 || tipoCampo > 2)
                {
                    return Json(new { success = false, message = "Tipo de campo no válido" });
                }

                // Ejecutar SP
                using (SqlConnection cnn = new SqlConnection(cad_cn))
                {
                    await cnn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("SP_REGISTRAR_CAMPO", cnn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetros para CAMPOS_MONITOREO
                        cmd.Parameters.AddWithValue("@ID_CAMPO", idCampo);
                        cmd.Parameters.AddWithValue("@CAMPO", campo);
                        cmd.Parameters.AddWithValue("@TIPO_DATO", tipoDato);
                        cmd.Parameters.AddWithValue("@TIPO_CAMPO", tipoCampo);

                        await cmd.ExecuteNonQueryAsync();

                        return Json(new
                        {
                            success = true,
                            message = "Campo registrado exitosamente",
                            idCampo = idCampo
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al registrar el campo."});
            }
        }

        [HttpPost("RegistrarCampoCliente")]
        public async Task<IActionResult> RegistrarCampoCliente(
            [FromForm] string idCliente,
            [FromForm] string idCampo)
        {
            try
            {
                if (string.IsNullOrEmpty(idCliente) || string.IsNullOrEmpty(idCampo))
                {
                    return Json(new { success = false, message = "Todos los campos son obligatorios." });
                }

                using (SqlConnection cnn = new SqlConnection(cad_cn))
                {
                    await cnn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("SP_REGISTRAR_CAMPO_CLIENTE", cnn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ID_CLIENTE", idCliente);
                        cmd.Parameters.AddWithValue("@ID_CAMPO", idCampo);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                bool success = reader.GetInt32(reader.GetOrdinal("Success")) == 1;
                                string mensaje = reader.GetString(reader.GetOrdinal("Mensaje"));

                                return Json(new { success, message = mensaje });
                            }
                        }
                    }
                }

                return Json(new { success = false, message = "No se pudo registrar el campo." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error al asignar los campos. "});
            }
        }


        [HttpGet("ObtenerClientes")]
        public IActionResult ObtenerClientes()
        {
            try
            {
                var cliente = new List<dynamic>();
                using (SqlConnection cnn = new SqlConnection(cad_cn))
                {
                    cnn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT ID_CLIENTE, NOMBRE FROM CLIENTE WHERE ESTADO = 1", cnn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                cliente.Add(new
                                {
                                    idClie = reader["ID_CLIENTE"].ToString(),
                                    nombre = reader["NOMBRE"].ToString()
                                });
                            }
                        }
                    }
                }
                return Json(cliente);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error al cargar los clientes. " });
            }
        }


        [HttpPost("RegistrarComponente")]
        public async Task<IActionResult> RegistrarComponente(
         [FromForm] string idComponente,
         [FromForm] string componente,
         [FromForm] string descripcion,
         [FromForm] string idServer)
        {
            try
            {
                if (string.IsNullOrEmpty(idComponente) ||
                    string.IsNullOrEmpty(componente) ||
                    string.IsNullOrEmpty(descripcion) ||
                    string.IsNullOrEmpty(idServer))
                {
                    return Json(new { success = false, message = "Todos los campos son obligatorios." });
                }

                using (SqlConnection cnn = new SqlConnection(cad_cn))
                {
                    await cnn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("SP_REGISTRAR_COMPONENTE", cnn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ID_COMPONENTE", idComponente);
                        cmd.Parameters.AddWithValue("@COMPONENTE", componente);
                        cmd.Parameters.AddWithValue("@DESCRIPCION", descripcion);
                        cmd.Parameters.AddWithValue("@ID_SERVER", idServer);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                bool success = reader.GetInt32(reader.GetOrdinal("Success")) == 1;
                                string mensaje = reader.GetString(reader.GetOrdinal("Mensaje"));

                                return Json(new { success, message = mensaje });
                            }
                        }
                    }
                }

                return Json(new { success = false, message = "No se pudo registrar el componente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al registrar el componente." });
            }
        }


        [HttpGet("ObtenerServidores")]
        public IActionResult ObtenerServidores()
        {
            try
            {
                var cliente = new List<dynamic>();
                using (SqlConnection cnn = new SqlConnection(cad_cn))
                {
                    cnn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT ID_SERVER, ALIAS FROM SERVIDORES WHERE ESTADO = 1", cnn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                cliente.Add(new
                                {
                                    idServer = reader["ID_SERVER"].ToString(),
                                    nombre = reader["ALIAS"].ToString()
                                });
                            }
                        }
                    }
                }
                return Json(cliente);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error al cargar los clientes." });
            }
        }

        [HttpGet("ObtenerCamposPorCliente")]
        public IActionResult ObtenerCamposPorCliente(string idCliente)
        {
            try
            {
                var campos = new List<dynamic>();
                using (SqlConnection cnn = new SqlConnection(cad_cn))
                {
                    cnn.Open();
                    
                    string query = @"SELECT 
                                cm.ID_CAMPO,
                                cm.CAMPO
                            FROM CAMPOS_MONITOREO cm
                            LEFT JOIN CLIENTE_CAMPOS cc ON 
                                cm.ID_CAMPO = cc.ID_CAMPO AND 
                                cc.ID_CLIENTE = @idCliente
                            WHERE 
                                cm.ESTADO = 1 AND
                                cc.ID_CLIENTE IS NULL
                            ORDER BY cm.CAMPO";

                    using (SqlCommand cmd = new SqlCommand(query, cnn))
                    {
                        cmd.Parameters.AddWithValue("@idCliente", idCliente);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                campos.Add(new
                                {
                                    idCampo = reader["ID_CAMPO"].ToString(),
                                    nombre = reader["CAMPO"].ToString()
                                });
                            }
                        }
                    }
                }
                return Json(campos);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error al cargar los campos." });
            }
        }

        [HttpPost("RegistrarServidores")]
        public async Task<IActionResult> RegistrarServidores(
            [FromForm] string idServer,
            [FromForm] string idCliente,
            [FromForm] string alias,
            [FromForm] string ipServer)
        {
            try
            {
                // Validación de campos obligatorios
                if (string.IsNullOrEmpty(idServer) ||
                    string.IsNullOrEmpty(idCliente) ||
                    string.IsNullOrEmpty(alias) ||
                    string.IsNullOrEmpty(ipServer))
                {
                    return Json(new { success = false, message = "Todos los campos son obligatorios." });
                }

                using (SqlConnection cnn = new SqlConnection(cad_cn))
                {
                    await cnn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("SP_REGISTRAR_SERVIDORES", cnn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@ID_SERVER", idServer);
                        cmd.Parameters.AddWithValue("@ID_CLIENTE", idCliente);
                        cmd.Parameters.AddWithValue("@ALIAS", alias);
                        cmd.Parameters.AddWithValue("@IP_SERVER", ipServer);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                bool success = reader.GetInt32(reader.GetOrdinal("Success")) == 1;
                                string mensaje = reader.GetString(reader.GetOrdinal("Mensaje"));

                                return Json(new { success, message = mensaje });
                            }
                        }
                    }
                }

                return Json(new { success = false, message = "No se pudo registrar el servidor." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al registrar el servidor." });
            }
        }

    }
}
