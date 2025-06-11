using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace PrjVigiaCore.Controllers
{
    public class ClientesController : Controller
    {

        private readonly IConfiguration _configuration;
        private readonly string cad_cn;

        public ClientesController(IConfiguration configuration)
        {
            _configuration = configuration;
            cad_cn = configuration.GetConnectionString("cn1")!;
        }


        [HttpGet]
        public async Task<IActionResult> ListarClientes()
        {
            try
            {
                List<dynamic> clientes = new List<dynamic>();
                using var conn = new SqlConnection(cad_cn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SP_LISTAR_CLIENTES", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    dynamic cliente = new System.Dynamic.ExpandoObject();
                    cliente.ID_CLIENTE = reader["ID_CLIENTE"] != DBNull.Value ? reader["ID_CLIENTE"] : null;
                    cliente.ID_GRUPO = reader["ID_GRUPO"] != DBNull.Value ? reader["ID_GRUPO"] : null;
                    cliente.NOMBRE = reader["NOMBRE"] != DBNull.Value ? reader["NOMBRE"] : null;
                    cliente.IMAGEN = reader["IMAGEN"] != DBNull.Value ? reader["IMAGEN"].ToString() : string.Empty;
                    cliente.NOM_GRUPO = reader["NOM_GRUPO"] != DBNull.Value ? reader["NOM_GRUPO"] : null;
                    cliente.ESTADO = reader["ESTADO"] != DBNull.Value ? reader["ESTADO"] : null;
                    clientes.Add(cliente);
                }
                ViewBag.TotalRegistros = clientes.Count();

                return View(clientes);
            }
            catch (Exception)
            {
                ViewBag.Error = "¡Ups! Algo salió mal. Intentalo de nuevo mas tarde.";
                return View(new List<dynamic>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarCliente(
            [FromForm] string idCliente,
            [FromForm] string idGrupo,
            [FromForm] string nombre,
            [FromForm] IFormFile imagen)
        {
            try
            {
                // Validaciones básicas
                if (string.IsNullOrEmpty(idGrupo) || string.IsNullOrEmpty(nombre))
                {
                    return Json(new { success = false, message = "El grupo y el nombre son campos obligatorios" });
                }

                // Procesar imagen
                string imagePath = null;
                if (imagen != null && imagen.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "clientes");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();

                    // Validar extensiones permitidas
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    if (!allowedExtensions.Contains(extension))
                    {
                        return Json(new { success = false, message = "Formato de imagen no válido. Use JPG, JPEG, PNG o GIF." });
                    }

                    // Crear nombre de archivo usando el ID del cliente
                    var fileName = $"{idCliente}{extension}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    // Eliminar imagen anterior si existe
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imagen.CopyToAsync(fileStream);
                    }

                    imagePath = $"/images/clientes/{fileName}";
                }

                // Ejecutar SP
                using (SqlConnection cnn = new SqlConnection(cad_cn))
                {
                    await cnn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("SP_REGISTRAR_CLIENTE", cnn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetros para CLIENTE
                        cmd.Parameters.AddWithValue("@ID_CLIENTE", idCliente);
                        cmd.Parameters.AddWithValue("@ID_GRUPO", idGrupo);
                        cmd.Parameters.AddWithValue("@NOMBRE", nombre);
                        cmd.Parameters.AddWithValue("@IMAGEN", string.IsNullOrEmpty(imagePath) ? DBNull.Value : (object)imagePath);

                        await cmd.ExecuteNonQueryAsync();

                        return Json(new
                        {
                            success = true,
                            message = "Cliente registrado exitosamente",
                            idCliente = idCliente
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al registrar el cliente: " + ex.Message });
            }
        }

        // GET: ClientesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ClientesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ClientesController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ClientesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ClientesController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ClientesController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
