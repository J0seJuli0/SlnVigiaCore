using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PrjVigiaCore.DAO;
using PrjVigiaCore.Models;
using PrjVigiaCore.Services;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Dynamic;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Xceed.Words.NET;

namespace PrjVigiaCore.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MonitoreoController : Controller
    {
        private readonly ITokenService _tokenService;
        private readonly string _connectionString;

        public MonitoreoController(IConfiguration configuration, ITokenService tokenService)
        {
            _tokenService = tokenService;
            _connectionString = configuration.GetConnectionString("cn1")!;
        }

        [AuthorizeMenu]
        [HttpGet("Listar")]
        public IActionResult Listar(string? token = null)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Login");
            }

            try
            {
                string idCliente = null;

                if (!string.IsNullOrEmpty(token))
                {
                    idCliente = _tokenService.ObtenerIdCliente(token);
                    if (idCliente == null)
                    {
                        return BadRequest("Token inválido o expirado");
                    }
                }
                DataTable datos = ObtenerMonitoreosPivoteado(idCliente);
                return View("ListarMonitoreos", datos);
            }
            catch (Exception)
            {
                ViewBag.Error = "¡Ups! Algo salió mal. Intentalo de nuevo mas tarde.";
                return View("ListarMonitoreos", new DataTable());
            }
        }

        [HttpGet("ObtenerMonitoreosPivoteado")]
        public DataTable ObtenerMonitoreosPivoteado(string? idCliente = null)
        {
            DataTable resultado = new DataTable();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SP_LISTAR_MONITOREOS_PIVOTEADO", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Parámetro de entrada (nullable)
                    cmd.Parameters.AddWithValue("@idCliente", (object?)idCliente ?? DBNull.Value);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(resultado);
                    }
                }
            }

            return resultado;
        }


        [HttpGet("validar-token")]
        public IActionResult ValidarToken([FromQuery] string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { success = false, message = "Token no proporcionado" });
                }

                var idCliente = _tokenService.ObtenerIdCliente(token);
                if (idCliente == null)
                {
                    return Unauthorized(new { success = false, message = "Token inválido o expirado" });
                }

                return Ok(new { success = true, idCliente });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error interno del servidor" });
            }
        }

        [AuthorizeMenu]
        [HttpGet("campos")]
        public IActionResult Campos()
        {
            return View();
        }


        [AuthorizeMenu]
        [HttpGet("campos/{clienteId}")]
        public async Task<IActionResult> ObtenerCampos(string clienteId)
        {
            try
            {
                if (string.IsNullOrEmpty(clienteId))
                {
                    return BadRequest("Código de cliente no proporcionado");
                }
                var resultados = new List<dynamic>();

                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("SP_OBTENER_CAMPOS", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ID_CLIENTE", clienteId);

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
                        IdGrupo = reader["ID_GRUPO"].ToString(),
                        Imagen = reader["IMAGEN"]?.ToString()
                    });
                }

                return Ok(resultados);
            }
            catch (Exception)
            {
                return BadRequest(new { mensaje = "Error al obtener campos." });
            }
        }


        [AuthorizeMenu]
        [HttpGet("componente/{idServer}")]
        public async Task<IActionResult> ObtenerComponentes(string idServer)
        {
            if (string.IsNullOrWhiteSpace(idServer))
                return BadRequest(new { mensaje = "!Ups! Algo Salió Mal." });
            {

                try
                {
                    var resultados = new List<dynamic>();

                    using var cnn = new SqlConnection(_connectionString);
                    using var cmd = new SqlCommand("SP_LISTAR_COMPONENTES_FORM", cnn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.AddWithValue("@ID_SERVER", idServer);

                    await cnn.OpenAsync();
                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        resultados.Add(new
                        {
                            IdServer = reader["ID_SERVER"].ToString(),
                            Componente = reader["COMPONENTE"].ToString(),
                            FechaRegistro = Convert.ToDateTime(reader["FECHA_REGISTRO"])
                        });
                    }

                    return Ok(resultados);
                }
                catch (Exception)
                {
                    return BadRequest(new { mensaje = "No pudimos cargar los componentes en este momento. Por favor, inténtalo de nuevo más tarde." });
                }

            }
        }


        [AuthorizeMenu]
        [HttpGet("destinatarios/{idGrupo}")]
        public async Task<IActionResult> ObtenerDestinatarios(string idGrupo)
        {
            try
            {
                var destinatarios = new List<dynamic>();

                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("SP_LISTAR_DESTINATARIOS", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ID_GRUPO", idGrupo); 

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    destinatarios.Add(new
                    {
                        Email = reader["EMAIL"].ToString()
                    });
                }

                return Ok(destinatarios);
            }
            catch (Exception)
            {
                return BadRequest(new { mensaje = "Oops, hubo un problema al cargar tus destinatarios. Danos unos minutos mientras lo resolvemos :)" });
            }
        }


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
                    var idCliente = item.GetProperty("idCliente").GetString();

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
                    success = true,
                    status = enviado ? "complete" : "partial",
                    mensaje = enviado
                        ? $"¡Listo! Hemos registrado tu monitoreo y notificado a los destinatarios."
                        : $"Hemos guardado tu monitoreo (Referencia: {idMon}), pero hubo un problema al enviar los correos",
                    detalles = new
                    {
                        idMonitoreo = idMon,
                        notificacionEnviada = enviado,
                        fecha = DateTime.Now.ToString("dd 'de' MMMM 'del' yyyy")
                    },
                    acciones = enviado
                    ? Array.Empty<string>()
                    : new[] {
                        "1. Verificar la lista de correos destinatarios",
                        "2. Contactar a soporte técnico si el problema persiste"
                    }
                });
            }
            catch (Exception ex)
            {
                string mensajes = ex switch
                {
                    SqlException => "Ocurrió un problema con la base de datos. Estamos trabajando para solucionarlo.",

                    IOException ioEx when ioEx.Message.Contains("already exists") ||
                                        ioEx.Message.Contains("ya existe") =>
                                        "El archivo ya existe. Por favor, comunicate con soporte para verificar el problema.",

                    UnauthorizedAccessException => "No tienes permiso para realizar esta acción.",

                    _ => "Ocurrió un error inesperado. Por favor, inténtalo más tarde."
                };

                return BadRequest(new
                {
                    success = false,
                    mensaje = mensajes,
                });
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
            string fecha = datos.FirstOrDefault(x => x.CAMPO.ToString().ToUpper() == "FECHA")?.VALOR?.ToString() ?? DateTime.Now.ToString("yyyyMMdd");
            string hora = datos.FirstOrDefault(x => x.CAMPO.ToString().ToUpper() == "HORA")?.VALOR?.ToString()?.Trim().Replace(":", "") ?? DateTime.Now.ToString("HHmmss");
            string nombreArchivo = $"MONITOREO_{cliente}_#{fecha}_{hora}.pdf";
            string ruta = Path.Combine("wwwroot/temp", nombreArchivo);
            Directory.CreateDirectory("wwwroot/temp");
            using var fs = new FileStream(ruta, FileMode.Create);
            var doc = new Document(PageSize.A4, 56, 56, 37, 37);
            PdfWriter.GetInstance(doc, fs);
            doc.Open();
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.BLACK);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            var textFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var tableHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.WHITE);
            var headerTable = new PdfPTable(2)
            {
                WidthPercentage = 100
            };
            string imagePath = Path.Combine("wwwroot", "img", "logo.png");
            if (System.IO.File.Exists(imagePath))
            {
                var logo = Image.GetInstance(imagePath);
                logo.ScaleToFit(125, 125);
                var cellogo = new PdfPCell(logo)
                {
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_LEFT,
                    VerticalAlignment = Element.ALIGN_MIDDLE
                };
                headerTable.AddCell(cellogo);
            }
            else
            {
                headerTable.AddCell(new PdfPCell(new Phrase("")) { Border = Rectangle.NO_BORDER });
            }
            var textderecha = new PdfPCell(new Phrase("Gestión Plataforma Cloud", titleFont))
            {
                Border = Rectangle.NO_BORDER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                HorizontalAlignment = Element.ALIGN_RIGHT
            };
            headerTable.AddCell(textderecha);
            doc.Add(headerTable);
            doc.Add(new Paragraph("\n"));
            doc.Add(new Paragraph($"Asunto: {cliente} - {fecha} - Monitoreo Servidores {cliente}", textFont));
            doc.Add(new Paragraph($"Reciban un cordial saludo, queremos mantenerlos informados acerca del seguimiento del monitoreo:", textFont));
            doc.Add(new Paragraph("\n"));
            doc.Add(new Paragraph($"Cuenta: {cliente}", textFont));
            doc.Add(new Paragraph($"Fecha de Monitoreo: {fecha}", textFont));
            if (DateTime.TryParseExact(hora, "HHmmss", null, System.Globalization.DateTimeStyles.None, out DateTime horaDateTime))
            {
                string horaFormateada = horaDateTime.ToString("hh:mm tt");
                doc.Add(new Paragraph($"Hora de Monitoreo: {horaFormateada}"));
            }
            else
            {
                string horaSistem = DateTime.Now.ToString("hh:mm tt");
                doc.Add(new Paragraph($"Hora de Monitoreo: {horaSistem}", textFont));
            }
            doc.Add(new Paragraph("\n"));
            foreach (var grupo in datos.Where(x => x.ALIAS != "1").GroupBy(x => x.ALIAS))
            {
                // Creamos la tabla
                var table = new PdfPTable(2)
                {
                    WidthPercentage = 100,
                    SpacingBefore = 10f,
                    SpacingAfter = 10f
                };
                table.SetWidths(new float[] { 40, 60 });

                // Agregamos una celda para el titulo
                var aliasHeaderCell = new PdfPCell(new Phrase(grupo.Key.ToString(), tableHeaderFont))
                {
                    Colspan = 2,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    BackgroundColor = new BaseColor(41, 128, 185),
                    Padding = 8f,
                    MinimumHeight = 30f
                };
                table.AddCell(aliasHeaderCell);

                

                // Agregamos los datos en las filas
                foreach (var item in grupo)
                {
                    var campoCell = new PdfPCell(new Phrase(item.CAMPO, textFont))
                    {
                        Padding = 5f,
                        BorderColor = BaseColor.LIGHT_GRAY
                    };
                    table.AddCell(campoCell);

                    string valorTexto = item.VALOR?.ToString() ?? "-";
                    if (item.CAMPO.ToString().Contains("%") && !valorTexto.EndsWith("%") && valorTexto != "-")
                    {
                        valorTexto += "%";
                    }

                    var valorCell = new PdfPCell(new Phrase(valorTexto, textFont))
                    {
                        Padding = 5f,
                        BorderColor = BaseColor.LIGHT_GRAY
                    };
                    table.AddCell(valorCell);
                }

                doc.Add(table);
            }
            doc.Add(new Paragraph("\n"));
            var responsable = datos.FirstOrDefault(x => x.CAMPO.ToString().ToLower().Contains("responsable"))?.VALOR ?? "Grupo Cloud";
            doc.Add(new Paragraph("Agradecemos la atención prestada.", textFont));
            doc.Add(new Paragraph("Saludos Cordiales", textFont));
            doc.Add(new Paragraph($"{responsable} | Gestión Plataforma Cloud", titleFont));
            doc.Close();
            return ruta;
        }

        private bool EnviarCorreoConPDF(string rutaPDF, string destinatarios, string cliente, string fecha)
        {
            try
            {
                string remitente = "julio.sanchez@complexless.com";
                string clave = "nwwj gats gdnq rufn";

                var mensaje = new MailMessage
                {
                    From = new MailAddress(remitente),
                    Subject = $"COMPLEXLESS - MONITOREO {cliente} #{fecha}",
                    Body = $"Estimados,\n\nAprovechamos en saludarlos y a la vez compartimos el reporte de monitoreo del día {fecha}.\n\nSaludos.\n\nGestión Plataforma Cloud"
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

            string cliente = datos.FirstOrDefault()?.NOMBRE ?? "Desconocido";
            string fecha = datos.FirstOrDefault(x => x.CAMPO.ToString().ToUpper() == "FECHA")?.VALOR?.ToString() ?? DateTime.Now.ToString("yyyyMMdd");

            string pdfPath = GenerarPDFDinamico(datos);
            return EnviarCorreoConPDF(pdfPath, destinatarios, cliente, fecha);
        }

        [AuthorizeMenu]
        [HttpGet("FormularioSES")]
        public IActionResult FormularioSES()
        {
            dynamic model = new ExpandoObject();
            var clientes = new List<dynamic>();

            using (SqlConnection cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                string query = "SELECT ID_CLIENTE, NOMBRE, IMAGEN FROM CLIENTE WHERE ESTADO = 1";
                using (SqlCommand cmd = new SqlCommand(query, cn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            clientes.Add(new
                            {
                                Value = reader["ID_CLIENTE"].ToString(),
                                Text = reader["NOMBRE"].ToString(),
                                Imagen = reader["IMAGEN"]?.ToString() ?? string.Empty
                            });
                        }
                    }
                }
            }

            model.Clientes = clientes;
            model.Fecha = DateTime.Now.Date;
            model.Hora = DateTime.Now.TimeOfDay;
            model.Destinatarios = string.Empty;
            model.Responsable = string.Empty;
            model.PorcentajeRebote = 0m;

            return View(model);
        }




        [HttpGet("ObtenerDestinatariosPorCliente")]
        public async Task<IActionResult> ObtenerDestinatariosPorCliente(string idCliente)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 1. Obtener el ID_GRUPO del cliente
                    var grupoQuery = @"SELECT ID_GRUPO FROM CLIENTE 
                             WHERE ID_CLIENTE = @IdCliente AND ESTADO = 1";

                    string idGrupo;
                    using (var command = new SqlCommand(grupoQuery, connection))
                    {
                        command.Parameters.AddWithValue("@IdCliente", idCliente);
                        var result = await command.ExecuteScalarAsync();
                        idGrupo = result?.ToString();
                    }

                    if (string.IsNullOrEmpty(idGrupo))
                    {
                        return Json(new { success = false, message = "Grupo no encontrado" });
                    }

                    // 2. Obtener los destinatarios activos del grupo
                    var destinatariosQuery = @"SELECT EMAIL FROM DESTINATARIOS 
                                      WHERE ID_GRUPO = @IdGrupo AND ESTADO = 1";

                    var emails = new List<string>();
                    using (var command = new SqlCommand(destinatariosQuery, connection))
                    {
                        command.Parameters.AddWithValue("@IdGrupo", idGrupo);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                emails.Add(reader["EMAIL"].ToString());
                            }
                        }
                    }

                    return Json(new { success = true, emails = emails });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Agregar esta clase al inicio de tu controlador o en un archivo separado
        public class FormularioSESModel
        {
            public string ClienteSeleccionado { get; set; }
            public string Fecha { get; set; }
            public string Hora { get; set; }
            public string Destinatarios { get; set; }
            public string Responsable { get; set; }
            public decimal PorcentajeRebote { get; set; }
        }

        // Reemplazar el método ProcesarFormularioSES con este:
        [HttpPost("ProcesarFormularioSES")]
        public IActionResult ProcesarFormularioSES([FromForm] FormularioSESModel modelo)
        {
            try
            {
                // Obtener nombre del cliente
                string nombreCliente = ObtenerNombreCliente(modelo.ClienteSeleccionado);

                string pdfPath = GenerarPDFSES(
                    nombreCliente,
                    modelo.Fecha,
                    modelo.Hora,
                    modelo.Responsable,
                    modelo.PorcentajeRebote.ToString(),
                    modelo.Destinatarios
                );

                //modificamos el cliente antes de pasarlo
                string clienteCorreo = $"SES {nombreCliente} - Porcentaje de Rebote {modelo.PorcentajeRebote}%";

                // enviamos el correo
                bool enviado = EnviarCorreoConPDF(pdfPath, modelo.Destinatarios, clienteCorreo, modelo.Fecha);


                if (enviado)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Formulario enviado correctamente y correo electrónico enviado a los destinatarios"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Formulario guardado pero hubo un problema al enviar el correo electrónico"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error al procesar el formulario: {ex.Message}"
                });
            }
        }

        private string ObtenerNombreCliente(string idCliente)
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                string query = "SELECT NOMBRE FROM CLIENTE WHERE ID_CLIENTE = @IdCliente";
                using (var cmd = new SqlCommand(query, cn))
                {
                    cmd.Parameters.AddWithValue("@IdCliente", idCliente);
                    return cmd.ExecuteScalar()?.ToString() ?? "Cliente Desconocido";
                }
            }
        }

        private string GenerarPDFSES(string cliente, string fecha, string hora, string responsable, string porcentajeRebote, string destinatarios)
        {
            // Generar ID único de sesión
            string idSesion = $"SES{DateTime.Now:yyMMddHHmmss}";

            // Crear nombre del archivo con formato específico para SES
            string nombreArchivo = $"MONITOREO_{cliente}-{idSesion}_#{fecha.Replace("/", "").Replace("-", "")}.pdf";
            string ruta = Path.Combine("wwwroot/temp", nombreArchivo);
            Directory.CreateDirectory("wwwroot/temp");

            using var fs = new FileStream(ruta, FileMode.Create);
            var doc = new Document(PageSize.A4, 56, 56, 37, 37);
            PdfWriter.GetInstance(doc, fs);
            doc.Open();

            // Definir fuentes
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.BLACK);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            var textFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var tableHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.WHITE);

            // Crear encabezado con logo
            var headerTable = new PdfPTable(2)
            {
                WidthPercentage = 100
            };

            string imagePath = Path.Combine("wwwroot", "img", "logo.png");
            if (System.IO.File.Exists(imagePath))
            {
                var logo = Image.GetInstance(imagePath);
                logo.ScaleToFit(125, 125);
                var cellogo = new PdfPCell(logo)
                {
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_LEFT,
                    VerticalAlignment = Element.ALIGN_MIDDLE
                };
                headerTable.AddCell(cellogo);
                Console.WriteLine($"Ruta imagen: {imagePath} - Existe: {System.IO.File.Exists(imagePath)}");
            }
            else
            {
                var emptyCell = new PdfPCell(new Phrase(" "))
                {
                    Border = Rectangle.NO_BORDER
                };
                headerTable.AddCell(emptyCell);
            }


            var textderecha = new PdfPCell(new Phrase("Gestión Plataforma Cloud", titleFont))
            {
                Border = Rectangle.NO_BORDER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                HorizontalAlignment = Element.ALIGN_RIGHT
            };
            headerTable.AddCell(textderecha);
            doc.Add(headerTable);

            // Contenido del documento
            doc.Add(new Paragraph("\n"));
            doc.Add(new Paragraph($"Asunto: {cliente} - {fecha} - Reporte SES", textFont));
            doc.Add(new Paragraph($"Reciban un cordial saludo, queremos mantenerlos informados acerca del monitoreo de SES:", textFont));
            doc.Add(new Paragraph("\n"));

            // Información básica
            doc.Add(new Paragraph($"Cuenta: {cliente}", textFont));
            doc.Add(new Paragraph($"Fecha de Monitoreo: {fecha}", textFont));
            doc.Add(new Paragraph($"Hora de Monitoreo: {hora}", textFont));
            doc.Add(new Paragraph("\n"));

            // Tabla de métricas SES
            var table = new PdfPTable(2)
            {
                WidthPercentage = 100,
                SpacingBefore = 10f,
                SpacingAfter = 10f
            };
            table.SetWidths(new float[] { 40, 60 });

            // Encabezado de la tabla
            var headerCell = new PdfPCell(new Phrase("MONITOREO SES", tableHeaderFont))
            {
                Colspan = 2,
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                BackgroundColor = new BaseColor(41, 128, 185),
                Padding = 8f,
                MinimumHeight = 30f
            };
            table.AddCell(headerCell);

            // Datos de la tabla
            // Intentar parsear el string a decimal
            decimal reboteDecimal = 0;
            decimal.TryParse(porcentajeRebote, out reboteDecimal);

            string estadoServicio = "Estable";
            string colorEstado = "Verde";

            if (reboteDecimal >= 10)
            {
                estadoServicio = "Cuenta en Riesgo";
                colorEstado = "Rojo";
            }
            else if (reboteDecimal > 5)
            {
                estadoServicio = "Advertencia";
                colorEstado = "Amarillo";
            }

            var metricas = new Dictionary<string, string>
            {
                { "Cuenta", $"{cliente}"},
                { "Porcentaje de Rebote", $"{porcentajeRebote}%" },
                { "Estado del Servicio", estadoServicio }
            };


            foreach (var metrica in metricas)
            {
                var campoCell = new PdfPCell(new Phrase(metrica.Key, textFont))
                {
                    Padding = 5f,
                    BorderColor = BaseColor.LIGHT_GRAY
                };
                table.AddCell(campoCell);

                var valorFont = textFont;

                var valorCell = new PdfPCell(new Phrase(metrica.Value, valorFont))
                {
                    Padding = 5f,
                    BorderColor = BaseColor.LIGHT_GRAY
                };

                if (metrica.Key == "Estado del Servicio")
                {
                    if (colorEstado == "Rojo")
                    {
                        valorCell.BackgroundColor = BaseColor.RED;
                        valorFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
                        valorCell.Phrase = new Phrase(metrica.Value, valorFont);
                    }
                    else if (colorEstado == "Amarillo")
                    {
                        valorCell.BackgroundColor = new BaseColor(255, 215, 0);
                    }
                    else if (colorEstado == "Verde")
                    {
                        valorCell.BackgroundColor = BaseColor.GREEN;
                    }
                }

                table.AddCell(valorCell);
            }


            doc.Add(table);

            // Pie del documento
            doc.Add(new Paragraph("\n"));
            doc.Add(new Paragraph("Agradecemos la atención prestada.", textFont));
            doc.Add(new Paragraph("Saludos Cordiales", textFont));
            doc.Add(new Paragraph($"{responsable} | Gestión Plataforma Cloud", titleFont));

            doc.Close();
            return ruta;
        }
    }
}
