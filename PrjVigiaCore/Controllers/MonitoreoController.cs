using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using PrjVigiaCore.DAO;
using System.Text.Json;

namespace PrjVigiaCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MonitoreoController : Controller
    {
        private readonly IConfiguration _configuration;

        private readonly string cad_cn;
        public MonitoreoController(IConfiguration configuration)
        {
            _configuration = configuration;
            cad_cn = configuration.GetConnectionString("cn1")!;
        }

        [HttpGet("campos")]
        public ActionResult Campos()
        {
            return View();
        }


        [HttpGet("campos/{idCliente}")]
        public async Task<IActionResult> ObtenerCampos(string idCliente)
        {
            try
            {
                string idClientDes = CryptoHelper.Decrypt(idCliente);

                var resultados = new List<dynamic>();

                using (SqlConnection conn = new SqlConnection(cad_cn))
                {
                    using (var command = new SqlCommand("SP_OBTENER_CAMPOS", conn))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ID_CLIENTE", idClientDes);

                        await conn.OpenAsync();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                byte[] imageBytes = reader["IMAGEN"] != DBNull.Value ? (byte[])reader["IMAGEN"] : null!;
                                string imageBase64 = imageBytes != null ? Convert.ToBase64String(imageBytes) : null!;
                                string tipoImagen = reader["TIPO_IMAGE"] != DBNull.Value ? reader["TIPO_IMAGE"].ToString() : null;
                                var campo = new
                                {
                                    IdCampo = reader["ID_CAMPO"].ToString(),
                                    Campo = reader["CAMPO"].ToString(),
                                    Alias = reader["ALIAS"].ToString(),
                                    IdServer = reader["ID_SERVER"].ToString(),
                                    TipoDato = reader["TIPO_DATO"].ToString(),
                                    TipoCampo = reader["TIPO_CAMPO"].ToString(),
                                    Estado = Convert.ToInt32(reader["ESTADO"]),
                                    Cliente = reader["CLIENTE"].ToString(),
                                    IdCliente = reader["ID_CLIENTE"].ToString(),
                                    Imagen = imageBase64,
                                    TipoImagen = tipoImagen,
                                };

                                resultados.Add(campo);
                            }
                        }
                    }
                }

                return Ok(resultados);
            }
            catch (Exception ex)
            {
                return BadRequest("Error al obtener los campos: " + ex.Message);
            }
        }


        [HttpPost("registrar")]
        public IActionResult Registrar([FromBody] JsonElement datos)
        {
            try
            {
                using var conn = new SqlConnection(cad_cn);
                conn.Open();

                // Generar ID_MON único
                string idMon = GenerarIdMon();

                foreach (var item in datos.EnumerateArray())
                {
                    var idCampo = item.GetProperty("idCampo").GetString();
                    var idServer = item.GetProperty("idServer").GetString();
                    var valor = item.GetProperty("valor").GetString();
                    var tipoCampo = item.GetProperty("tipoCampo").GetString();

                    using var cmd = new SqlCommand("SP_REGISTRO_MONITOREO", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID_MON", idMon);
                    cmd.Parameters.AddWithValue("@ID_SERVER", idServer);
                    cmd.Parameters.AddWithValue("@ID_CAMPO", idCampo);
                    cmd.Parameters.AddWithValue("@VALOR", string.IsNullOrEmpty(valor) ? DBNull.Value : valor);
                    cmd.Parameters.AddWithValue("@TIPO_CAMPO", tipoCampo);

                    cmd.ExecuteNonQuery();
                }

                return Content($"Monitoreo registrado con ID_MON: {idMon}");
            }
            catch (Exception ex)
            {
                return BadRequest("Error al registrar: " + ex.Message);
            }
        }

        private string GenerarIdMon()
        {
            var now = DateTime.Now;
            return "M" + now.ToString("yyMMddHHmmss");
        }

    }
}
