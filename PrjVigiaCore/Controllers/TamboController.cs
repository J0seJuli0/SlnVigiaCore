using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using PrjVigiaCore.Models;

namespace PrjVigiaCore.Controllers
{
    public class TamboController : Controller
    {
        public IActionResult FormTambo()
        {
            return View();
        }


        [HttpPost]
        public IActionResult EnviarCorreo(TamboMonitoreo model)
        {
            if (model == null)
            {   
                return BadRequest("El modelo es nulo.");
            }

            try
            {
                string pdfPath = GenerarPDF(model);
                bool enviado = EnviarCorreoConAdjunto(pdfPath, model);

                return enviado ? Ok("Correo enviado correctamente.") : StatusCode(500, "Error al enviar el correo.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        private string GenerarPDF(TamboMonitoreo model)
        {
            string uniqueFileName = $"COMPLEXLESS - MONITOREO TAMBO #{model.Fecha}.pdf";
            string pdfPath = Path.Combine("wwwroot/temp", uniqueFileName);
            Directory.CreateDirectory("wwwroot/temp");

            using (FileStream stream = new FileStream(pdfPath, FileMode.Create))
            {
                Document pdfDoc = new Document(PageSize.A4, 56, 56, 37, 37);
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();

                PdfPTable headerTable = new PdfPTable(2) { WidthPercentage = 100 };
                headerTable.SetWidths(new float[] { 20, 80 });

                string imagePath = Path.Combine("wwwroot/img", "logo.png");
                if (System.IO.File.Exists(imagePath))
                {
                    Image logo = Image.GetInstance(imagePath);
                    logo.ScaleToFit(125, 125);
                    PdfPCell imageCell = new PdfPCell(logo)
                    {
                        Border = PdfPCell.NO_BORDER,
                        VerticalAlignment = Element.ALIGN_MIDDLE
                    };
                    headerTable.AddCell(imageCell);
                }
                else
                {
                    headerTable.AddCell(new PdfPCell() { Border = PdfPCell.NO_BORDER });
                }

                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.BLACK);
                Font responFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.BLACK);
                PdfPCell titleCell = new PdfPCell(new Phrase("Gestión Plataforma Cloud", titleFont))
                {
                    Border = PdfPCell.NO_BORDER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                };
                headerTable.AddCell(titleCell);

                pdfDoc.Add(headerTable);
                pdfDoc.Add(new Paragraph("\n"));

                Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.WHITE);
                Font textFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);

                pdfDoc.Add(new Paragraph($"Asunto: TAMBO - # {model.Fecha} - Monitoreo Servidores Tambo", textFont));
                pdfDoc.Add(new Paragraph("Reciba un cordial saludo, queremos mantenerlo informado acerca del seguimiento del monitoreo:", textFont));

                pdfDoc.Add(new Paragraph("\n"));
                pdfDoc.Add(new Paragraph("Cuenta: Tambo", textFont));
                pdfDoc.Add(new Paragraph($"Fecha de monitoreo: {model.Fecha}", textFont));
                DateTime horaMonitoreo = DateTime.Parse(model.Hora);
                pdfDoc.Add(new Paragraph($"Hora de monitoreo: {horaMonitoreo.ToString("hh:mm tt")}", textFont));

                pdfDoc.Add(new Paragraph("\n"));

                AgregarCuadroServidor(pdfDoc, "Servidor PROD 1: 10.180.11.111", model.Server1Estado, model.Server1Fecha, model.Server1Disco, model.Server1NFS, model.Server1CPU, model.Server1Servicios, model.Server1Comentario, headerFont, textFont);
                AgregarCuadroServidor(pdfDoc, "Servidor PROD 2: 10.180.11.112", model.Server2Estado, model.Server2Fecha, model.Server2Disco, model.Server2NFS, model.Server2CPU, model.Server2Servicios, model.Server2Comentario, headerFont, textFont);
                AgregarCuadroServidor(pdfDoc, "Servidor  NGINX 10.180.11.30", model.Server3Estado, model.Server3Fecha, model.Server3Disco, "N/A", model.Server3CPU, "-", model.Server3Comentario, headerFont, textFont);

                pdfDoc.Add(new Paragraph("\n"));
                pdfDoc.Add(new Paragraph("Agradecemos la atención prestada.", textFont));
                pdfDoc.Add(new Paragraph("Saludos Cordiales", textFont));
                pdfDoc.Add(new Paragraph($"{model.Responsable} | Gestión Plataforma Cloud", responFont));

                pdfDoc.Close();
                writer.Close();
            }

            return pdfPath;
        }

        private void AgregarCuadroServidor(Document doc, string servidor, string estado, string fecha, string disco, string nfs, string cpu, string servicios, string comentarios, Font headerFont, Font textFont)
        {
            PdfPTable table = new PdfPTable(2) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 30, 70 });

            BaseColor headerColor = new BaseColor(0, 102, 204);
            PdfPCell headerCell = new PdfPCell(new Phrase(servidor, headerFont))
            {
                BackgroundColor = headerColor,
                Colspan = 2,
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 5
            };
            table.AddCell(headerCell);

            AgregarFila(table, "Estado", estado, textFont);
            AgregarFila(table, "Fecha y Hora del Servidor", fecha, textFont);
            AgregarFila(table, "% Uso de Disco Duro", $"{disco}%", textFont);
            AgregarFila(table, "% NFS", $"{nfs}%", textFont);
            AgregarFila(table, "% Uso del CPU", $"{cpu}%", textFont);
            AgregarFila(table, "Servicios Factus Activos", servicios, textFont);
            AgregarFila(table, "Comentarios", comentarios, textFont);

            table.HorizontalAlignment = Element.ALIGN_CENTER;

            doc.Add(table);
            doc.Add(new Paragraph("\n"));
        }

        private void AgregarFila(PdfPTable table, string columna, string valor, Font textFont)
        {
            table.AddCell(new PdfPCell(new Phrase(columna, textFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase(valor, textFont)) { Padding = 5 });
        }

        private bool EnviarCorreoConAdjunto(string pdfPath, TamboMonitoreo model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Destinatario))
                {
                    Console.WriteLine("Error: No se recibieron destinatarios.");
                    return false;
                }

                string remitente = "julio.sanchez@complexless.com";
                string claveRemitente = "nwwj gats gdnq rufn";

                MailMessage mensaje = new MailMessage
                {
                    From = new MailAddress(remitente),
                    Subject = $"COMPLEXLESS - MONITOREO TAMBO # {model.Fecha}",
                    Body = $"Estimados,\n\nAprovechamos en saludarlos y a la vez compartimos el reporte de monitoreo del día {model.Fecha}.\n\nSaludos.\n\nGestión Plataforma Cloud"
                };

                string[] destinatarios = model.Destinatario.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (destinatarios.Length == 0)
                {
                    return false;
                }

                foreach (string destinatario in destinatarios)
                {
                    mensaje.To.Add(destinatario.Trim());
                }

                mensaje.Attachments.Add(new Attachment(pdfPath));

                SmtpClient smtp = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(remitente, claveRemitente),
                    EnableSsl = true
                };

                smtp.Send(mensaje);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar correo: {ex.Message}");
                return false;
            }
        }
    }
}
