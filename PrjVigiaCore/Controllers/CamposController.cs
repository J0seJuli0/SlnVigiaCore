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




    }
}
