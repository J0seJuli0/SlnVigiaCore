using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using BCrypt.Net;
using System.Security.Cryptography;
using System.Text;

namespace PrjMonitoreoCPLX.Controllers
{
    public class LoginController : Controller
    {

        private readonly IConfiguration _configuration;

        private readonly string cad_cn;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
            cad_cn = configuration.GetConnectionString("cn1")!;
        }

        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("Email") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Ingrese su correo y contraseña";
                return View();
            }

            string idRol;
            string idUsu;
            string passHash;

            int resultado = AccesoSistema(email, out idRol, out idUsu, out passHash);

            if (resultado == 1 && BCrypt.Net.BCrypt.Verify(password, passHash))
            {
                HttpContext.Session.SetString("Email", email);
                HttpContext.Session.SetString("ID_Rol", idRol);
                HttpContext.Session.SetString("ID_Usuario", idUsu);
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.Error = "Credenciales incorrectas";
                return View();
            }
        }


        private int AccesoSistema(string email, out string idRol, out string idUsu, out string passHash)
        {
            idRol = string.Empty;
            idUsu = string.Empty;
            passHash = string.Empty;

            using (SqlConnection conn = new SqlConnection(cad_cn))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SP_ACCESO_MONITOREO", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.VarChar, 100) { Value = email });

                    SqlParameter outputResultado = new SqlParameter("@Resultado", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    SqlParameter outputRol = new SqlParameter("@ID_Rol", SqlDbType.VarChar, 8) { Direction = ParameterDirection.Output };
                    SqlParameter outputUsu = new SqlParameter("@ID_USUARIO", SqlDbType.VarChar, 8) { Direction = ParameterDirection.Output };
                    SqlParameter outputContrasenia = new SqlParameter("@Contrasenia", SqlDbType.VarChar, 1000) { Direction = ParameterDirection.Output };

                    cmd.Parameters.Add(outputResultado);
                    cmd.Parameters.Add(outputRol);
                    cmd.Parameters.Add(outputUsu);
                    cmd.Parameters.Add(outputContrasenia);

                    cmd.ExecuteNonQuery();

                    int resultado = (int)outputResultado.Value;
                    idRol = outputRol.Value != DBNull.Value ? outputRol.Value.ToString()! : string.Empty;
                    idUsu = outputUsu.Value != DBNull.Value ? outputUsu.Value.ToString()! : string.Empty;
                    passHash = outputContrasenia.Value != DBNull.Value ? outputContrasenia.Value.ToString()! : string.Empty;

                    return resultado;
                }
            }
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete(".AspNetCore.Cookies");

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToAction("Login");
        }


        public IActionResult RegistrarUsuario()
        {
            return View();
        }


        [HttpPost]
        public IActionResult RegistrarUsuario([FromBody] Dictionary<string, string> data)
        {
            try
            {   
                using (SqlConnection conn = new SqlConnection(cad_cn))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SP_REGISTRAR_USUARIO", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Encriptar la contraseña
                        string contraseniaEncriptada = BCrypt.Net.BCrypt.HashPassword(data["contrasenia"]);

                        cmd.Parameters.AddWithValue("@USUARIO", data["usuario"]);
                        cmd.Parameters.AddWithValue("@EMAIL", data["email"]);
                        cmd.Parameters.AddWithValue("@CONTRASENIA", contraseniaEncriptada);
                        cmd.Parameters.AddWithValue("@ID_ROL", data["idRol"]);
                        cmd.Parameters.AddWithValue("@NOMBRES", data["nombres"]);
                        cmd.Parameters.AddWithValue("@APE_PAT", data["apePat"]);
                        cmd.Parameters.AddWithValue("@APE_MAT", data["apeMat"]);

                        SqlParameter resultado = new SqlParameter("@RESULTADO", SqlDbType.Int);
                        resultado.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(resultado);

                        cmd.ExecuteNonQuery();

                        if ((int)cmd.Parameters["@RESULTADO"].Value == 1)
                        {
                            return Json(new { success = true, message = "Usuario registrado correctamente." });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Error al registrar el usuario." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in hashedBytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public async Task<IActionResult> PerfilUsuario()
        {
            string idEmpleado = HttpContext.Session.GetString("ID_Usuario")!;
            if (string.IsNullOrEmpty(idEmpleado))
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection cnn = new SqlConnection(cad_cn))
            {

                await cnn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand("SP_PERFIL", cnn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@ID_EMPLEADO", SqlDbType.VarChar, 8) { Value = idEmpleado });

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            ViewBag.IdEmpleado = reader["ID_EMPLEADO"].ToString();
                            ViewBag.Nombres = reader["NOMBRES"].ToString();
                            ViewBag.ApePat = reader["APE_PAT"].ToString();
                            ViewBag.ApeMat = reader["APE_MAT"].ToString();
                            ViewBag.Rol = reader["ROL"].ToString();
                            ViewBag.TipoUsuario = Convert.ToInt32(reader["TIPO_USU"]);
                            ViewBag.Estado = reader["ESTADO"].ToString();
                        }
                        else
                        {
                            return NotFound("No se encontraron datos del empleado.");
                        }
                    }
                }
                return View();
            }
        }

        public ActionResult Inicio()
        {
            if (HttpContext.Session.GetString("Email") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }
    }
}
