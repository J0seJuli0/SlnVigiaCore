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

        // GET: ClientesController/Details/5
        public ActionResult Details(int id)
        {
            return View();
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
