using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Net;
using System.Text.Json;
using iTextSharp.text;
using iTextSharp.text.pdf;
using PrjVigiaCore.DAO;
using System.Collections.Generic;

namespace PrjVigiaCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MonitoreoController : Controller
    {
        private readonly string _connectionString;

        public MonitoreoController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("cn1")!;
        }

        // Vista para monitoreo
        [HttpGet("campos")]
        public IActionResult Campos() => View();

        // Obtener lista de campos del cliente
        [HttpGet("campos/{idCliente}")]
        public async Task<IActionResult> ObtenerCampos(string idCliente)
        {
            try
            {
                string idClientDes = CryptoHelper.Decrypt(idCliente);
                var resultados = new List<dynamic>();

                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("SP_OBTENER_CAMPOS", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ID_CLIENTE", idClientDes);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    resultados.Add(new
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
                        Imagen = reader["IMAGEN"] != DBNull.Value ? Convert.ToBase64String((byte[])reader["IMAGEN"]) : null,
                        TipoImagen = reader["TIPO_IMAGE"]?.ToString()
                    });
                }

                return Ok(resultados);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = $"Error al obtener campos: {ex.Message}" });
            }
        }

        // Registrar monitoreo
        [HttpPost("registrar")]
        public IActionResult Registrar([FromBody] JsonElement datos)
        {
            try
            {
                string idMon = GenerarIdMon();

                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                foreach (var item in datos.EnumerateArray())
                {
                    var idCampo = item.GetProperty("idCampo").GetString();
                    var idServer = item.GetProperty("idServer").GetString();
                    var valor = item.GetProperty("valor").GetString();
                    var tipoCampo = item.GetProperty("tipoCampo").GetString();
                    var idCliente = CryptoHelper.Decrypt(item.GetProperty("idCliente").GetString());

                    using var cmd = new SqlCommand("SP_REGISTRO_MONITOREO", conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.AddWithValue("@ID_MON", idMon);
                    cmd.Parameters.AddWithValue("@ID_CLIENTE", idCliente);
                    cmd.Parameters.AddWithValue("@ID_SERVER", idServer);
                    cmd.Parameters.AddWithValue("@ID_CAMPO", idCampo);
                    cmd.Parameters.AddWithValue("@VALOR", string.IsNullOrEmpty(valor) ? DBNull.Value : valor);
                    cmd.Parameters.AddWithValue("@TIPO_CAMPO", tipoCampo);
                    cmd.ExecuteNonQuery();
                }

                var datosSP = ObtenerDatosDesdeSP(idMon);
                var destinatarios = datosSP.FirstOrDefault(d => d.CAMPO == "Destinatarios")?.VALOR;

                if (string.IsNullOrEmpty(destinatarios))
                    return NotFound(new { mensaje = "No se encontraron destinatarios." });

                bool enviado = EnviarCorreoMonitoreo(idMon, destinatarios);

                return Ok(new
                {
                    mensaje = enviado
                        ? $"Monitoreo registrado y correo enviado. ID_MON: {idMon}"
                        : "Monitoreo registrado, pero no se pudo enviar el correo.",
                    idMon
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = $"Error al registrar monitoreo: {ex.Message}" });
            }
        }

        // ========== MÉTODOS PRIVADOS ==========

        private string GenerarIdMon() => $"M{DateTime.Now:yyMMddHHmmss}";

        private List<dynamic> ObtenerDatosDesdeSP(string idMon)
        {
            var datos = new List<dynamic>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SP_ENVIO_CORREO", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ID_MON", idMon);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                datos.Add(new
                {
                    ID_MON = reader["ID_MON"].ToString(),
                    NOMBRE = reader["NOMBRE"].ToString(),
                    ALIAS = reader["ALIAS"].ToString(),
                    CAMPO = reader["CAMPO"].ToString(),
                    VALOR = reader["VALOR"].ToString(),
                    TIPO_CAMPO = Convert.ToInt32(reader["TIPO_CAMPO"])
                });
            }

            return datos;
        }

        private string GenerarPDFDinamico(List<dynamic> datos)
        {
            string cliente = datos.FirstOrDefault()?.NOMBRE ?? "Desconocido";
            string nombreArchivo = $"Monitoreo_{cliente}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            string ruta = Path.Combine("wwwroot/temp", nombreArchivo);

            Directory.CreateDirectory("wwwroot/temp");

            using var fs = new FileStream(ruta, FileMode.Create);
            var doc = new Document(PageSize.A4);
            PdfWriter.GetInstance(doc, fs);
            doc.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            var textFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

            doc.Add(new Paragraph($"Gesttión Plataforma Cloud"));
            doc.Add(new Paragraph($"Asunto: {cliente} - {DateTime.Now} - Monitoreo Servidores {cliente}", titleFont));
            doc.Add(new Paragraph($"Fecha: {DateTime.Now}", textFont));
            doc.Add(new Paragraph("\n"));

            foreach (var grupo in datos.GroupBy(x => x.ALIAS))
            {
                doc.Add(new Paragraph($"{grupo.Key}", headerFont));
                var table = new PdfPTable(2) { WidthPercentage = 100 };
                table.SetWidths(new float[] { 40, 60 });

                foreach (var item in grupo)
                {
                    table.AddCell(new PdfPCell(new Phrase(item.CAMPO, textFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                    table.AddCell(new Phrase(item.VALOR ?? "-", textFont));
                }

                doc.Add(table);
                doc.Add(new Paragraph("\n"));
            }

            var responsable = datos.FirstOrDefault(x => x.CAMPO.ToString().ToLower().Contains("Responsable"))?.VALOR ?? "No especificado";
            var responsable = datos.FirstOrDefault(x => x.CAMPO.ToString().ToLower().Contains("responsable"))?.VALOR ?? "No especificado";

            doc.Add(new Paragraph("Agradecemos la atención prestada.", textFont));
            doc.Add(new Paragraph("Saludos Cordiales", textFont));
            doc.Add(new Paragraph("\n"));
            doc.Add(new Paragraph($"{responsable} | Gestión Plataforma Cloud", textFont));
            doc.Close();
            return ruta;
        }

        private bool EnviarCorreoConPDF(string rutaPDF, string destinatarios)
        {
            try
            {
                string remitente = "julio.sanchez@complexless.com";
                string clave = "nwwj gats gdnq rufn";

                var mensaje = new MailMessage
                {
                    From = new MailAddress(remitente),
                    Subject = "Reporte de Monitoreo",
                    Body = "Adjunto encontrará su reporte de monitoreo.\n\nSaludos,\nGestión Plataforma Cloud"
                };

                foreach (var email in destinatarios.Split(',', ';'))
                {
                    if (!string.IsNullOrWhiteSpace(email))
                        mensaje.To.Add(email.Trim());
                }

                mensaje.Attachments.Add(new Attachment(rutaPDF));

                using var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential(remitente, clave),
                    EnableSsl = true
                };

                smtp.Send(mensaje);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar correo: " + ex.Message);
                return false;
            }
        }

        private bool EnviarCorreoMonitoreo(string idMon, string destinatarios)
        {
            var datos = ObtenerDatosDesdeSP(idMon);
            if (!datos.Any()) return false;

            string pdfPath = GenerarPDFDinamico(datos);
            return EnviarCorreoConPDF(pdfPath, destinatarios);
        }
    }
}
