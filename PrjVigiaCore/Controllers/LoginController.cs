using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PrjMonitoreoCPLX.Controllers
{
    public class LoginController : Controller
    {

        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;
        private readonly string cad_cn;

        public LoginController(IConfiguration configuration, IMemoryCache memoryCache)
        {
            _configuration = configuration;
            _memoryCache = memoryCache;
            cad_cn = configuration.GetConnectionString("cn1")!;
        }

        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("Email") != null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult>Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Ingrese su correo y contraseña";
                return View();
            }

            string idRol;
            string idUsu;
            string passHash;
            string nombreUsuario;

            int resultado = AccesoSistema(email, out idRol, out idUsu, out passHash, out nombreUsuario);

            if (resultado == 1 && BCrypt.Net.BCrypt.Verify(password, passHash))
            {
                HttpContext.Session.SetString("Email", email);
                HttpContext.Session.SetString("ID_Rol", idRol);
                HttpContext.Session.SetString("ID_Usuario", idUsu);
                HttpContext.Session.SetString("NombreUsu", nombreUsuario);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, nombreUsuario),
                    new Claim(ClaimTypes.Email, email),
                    new Claim("ID_Rol", idRol),
                    new Claim("ID_Usuario", idUsu)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

                return RedirectToAction("Index", "Dashboard");
            }
            else
            {
                ViewBag.Error = "Credenciales incorrectas";
                return View();
            }
        }


        private int AccesoSistema(string email, out string idRol, out string idUsu, out string passHash, out string nombreUsuario)
        {
            idRol = string.Empty;
            idUsu = string.Empty;
            passHash = string.Empty;
            nombreUsuario = string.Empty;

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
                    SqlParameter outputNombre = new SqlParameter("@NOMBRES", SqlDbType.VarChar, 1000) { Direction = ParameterDirection.Output };
                    
                    cmd.Parameters.Add(outputResultado);
                    cmd.Parameters.Add(outputRol);
                    cmd.Parameters.Add(outputUsu);
                    cmd.Parameters.Add(outputContrasenia);
                    cmd.Parameters.Add(outputNombre);

                    cmd.ExecuteNonQuery();

                    int resultado = (int)outputResultado.Value;
                    idRol = outputRol.Value != DBNull.Value ? outputRol.Value.ToString()! : string.Empty;
                    idUsu = outputUsu.Value != DBNull.Value ? outputUsu.Value.ToString()! : string.Empty;
                    passHash = outputContrasenia.Value != DBNull.Value ? outputContrasenia.Value.ToString()! : string.Empty;
                    nombreUsuario = outputNombre.Value != DBNull.Value ? outputNombre.Value.ToString()! : string.Empty;

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
                            ViewBag.DNI = reader["DNI"].ToString();
                            ViewBag.Usuario = reader["USUARIO"].ToString();
                            ViewBag.Email = reader["EMAIL"].ToString();
                            ViewBag.Nombres = reader["NOMBRES"].ToString();
                            ViewBag.ApePat = reader["APE_PAT"].ToString();
                            ViewBag.ApeMat = reader["APE_MAT"].ToString();
                            ViewBag.NombreCompleto = reader["NOMBRE_COMPLETO"].ToString();
                            ViewBag.FechaNacimiento = reader["FECHA_NACIMIENTO"].ToString();
                            ViewBag.Edad = reader["EDAD"].ToString();
                            ViewBag.Genero = reader["GENERO"].ToString();
                            ViewBag.GeneroDesc = reader["GENERO_DESC"].ToString();
                            ViewBag.EstadoCivil = reader["ESTADO_CIVIL"].ToString();
                            ViewBag.DireccionCompleta = reader["DIRECCION_COMPLETA"].ToString();
                            ViewBag.ImagePath = reader["IMAGE_PATH"].ToString();
                            ViewBag.GruposAsignados = reader["GRUPOS_ASIGNADOS"].ToString();
                            ViewBag.FechaRegistro = reader["FECHA_REGISTRO"].ToString();
                            ViewBag.FechaActualizacion = reader["FECHA_ACTUALIZACION"].ToString();
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

        ////////////////////////////////////////////////////RESET PASSWORD//////////////////////////////////////////////////////////////////////////////

        [HttpGet]
        public IActionResult RequestResetPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RequestResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Por favor ingrese su correo electrónico";
                return View();
            }

            // Verificar si el email existe en la base de datos
            bool emailExists = await CheckEmailExistsAsync(email);

            if (!emailExists)
            {
                // Por seguridad, no revelamos si el email existe o no
                return RedirectToAction("ResetPasswordSent");
            }

            // Generar código de verificación (6 dígitos)
            var verificationCode = new Random().Next(100000, 999999).ToString();

            // Guardar código en cache con expiración (15 minutos)
            _memoryCache.Set($"ResetPwd_{email}", verificationCode, TimeSpan.FromMinutes(15));

            // Enviar email con el código
            await SendVerificationEmail(email, verificationCode);

            return RedirectToAction("ResetPasswordSent", new { email = email });
        }

        [HttpGet]
        public IActionResult ResetPasswordSent(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string verificationCode, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(verificationCode) ||
                string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "Todos los campos son requeridos";
                ViewBag.Email = email;
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Las contraseñas no coinciden";
                ViewBag.Email = email;
                return View();
            }

            // Verificar el código de verificación
            var cacheKey = $"ResetPwd_{email}";
            if (!_memoryCache.TryGetValue(cacheKey, out string storedCode) ||
                storedCode != verificationCode)
            {
                ViewBag.Error = "Código de verificación inválido o expirado";
                ViewBag.Email = email;
                return View();
            }

            // Cambiar la contraseña en la base de datos
            bool result = await UpdatePasswordInDatabase(email, newPassword);

            if (result)
            {
                // Limpiar el código de cache
                _memoryCache.Remove(cacheKey);

                // Enviar notificación de cambio de contraseña
                await SendPasswordChangedNotification(email);

                return RedirectToAction("ResetPasswordSuccess");
            }

            ViewBag.Error = "Ocurrió un error al restablecer la contraseña";
            ViewBag.Email = email;
            return View();
        }


        private async Task SendPasswordChangedNotification(string email)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var logoUrl = "https://www.complexless.com/wp-content/uploads/2021/05/logoweb.png";

            using (var client = new SmtpClient(smtpSettings["Host"]))
            {
                client.Port = int.Parse(smtpSettings["Port"]);
                client.Credentials = new NetworkCredential(
                    smtpSettings["Username"],
                    smtpSettings["Password"]);
                client.EnableSsl = bool.Parse(smtpSettings["EnableSsl"]);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpSettings["FromAddress"], smtpSettings["FromName"]),
                    Subject = "Tu contraseña ha sido cambiada - Complexless",
                    Body = BuildPasswordChangedEmailBody(logoUrl),
                    IsBodyHtml = true,
                    Priority = MailPriority.High
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
            }
        }

        private string BuildPasswordChangedEmailBody(string logoUrl)
        {
            return $@"
            <!DOCTYPE html>
            <html lang='es'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Contraseña Actualizada</title>
                <style>
                    body {{
                        font-family: 'Arial', sans-serif;
                        line-height: 1.6;
                        color: #333;
                        max-width: 600px;
                        margin: 0 auto;
                        padding: 20px;
                    }}
                    .email-container {{
                        border: 1px solid #e0e0e0;
                        border-radius: 8px;
                        overflow: hidden;
                    }}
                    .header {{
                        background-color: #1d3335;
                        padding: 20px;
                        text-align: center;
                    }}
                    .logo {{
                        max-width: 200px;
                        height: auto;
                    }}
                    .content {{
                        padding: 30px;
                        background-color: #ffffff;
                    }}
                    .success-message {{
                        background-color: #f0f8ff;
                        border-left: 4px solid #1845ad;
                        padding: 15px;
                        margin: 20px 0;
                    }}
                    .footer {{
                        background-color: #f9f9f9;
                        padding: 15px;
                        text-align: center;
                        font-size: 12px;
                        color: #777;
                    }}
                </style>
            </head>
            <body>
                <div class='email-container'>
                    <div class='header'>
                        <img src='{logoUrl}' alt='Logo Complexless' class='logo'>
                    </div>
    
                    <div class='content'>
                        <h2>Notificación de seguridad</h2>
                
                        <div class='success-message'>
                            <p>Tu contraseña ha sido cambiada exitosamente.</p>
                        </div>
                
                        <p>Si no realizaste este cambio, por favor contacta inmediatamente al equipo de soporte.</p>
                
                        <p>Fecha y hora del cambio: <strong>{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}</strong></p>
                    </div>
    
                    <div class='footer'>
                        <p>© {DateTime.Now.Year} Complexless. Todos los derechos reservados.</p>
                        <p>Este es un mensaje automático, por favor no respondas a este correo.</p>
                    </div>
                </div>
            </body>
            </html>";
        }


        [HttpGet]
        public IActionResult ResetPasswordSuccess()
        {
            return View();
        }

        private async Task<bool> CheckEmailExistsAsync(string email)
        {
            using (SqlConnection conn = new SqlConnection(cad_cn))
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(1) FROM USUARIOS_SISTEMA WHERE EMAIL = @Email", conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    int count = (int)await cmd.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }

        private async Task<bool> UpdatePasswordInDatabase(string email, string newPassword)
        {
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            using (SqlConnection conn = new SqlConnection(cad_cn))
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand(@"
                    UPDATE USUARIOS_SISTEMA 
                    SET CONTRASENIA = @Password, FECHA_MODIFICACION = GETDATE() 
                    WHERE EMAIL = @Email;
            
                    UPDATE EMPLEADO 
                    SET FECHA_ACT = GETDATE() 
                    WHERE ID_EMPLEADO = (SELECT ID_USUARIO FROM USUARIOS_SISTEMA WHERE EMAIL = @Email);", conn))
                {
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);
                    cmd.Parameters.AddWithValue("@Email", email);
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }


        private async Task SendVerificationEmail(string email, string verificationCode)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");

            using (var client = new SmtpClient(smtpSettings["Host"]))
            {
                client.Port = int.Parse(smtpSettings["Port"]);
                client.Credentials = new NetworkCredential(
                    smtpSettings["Username"],
                    smtpSettings["Password"]);
                client.EnableSsl = bool.Parse(smtpSettings["EnableSsl"]);

                // URL de la imagen (puede ser una URL absoluta o base64)
                var logoUrl = "https://www.complexless.com/wp-content/uploads/2021/05/logoweb.png";

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpSettings["FromAddress"], smtpSettings["FromName"]),
                    Subject = "Código de verificación para restablecer contraseña - Complexless",
                    Body = BuildEmailBody(verificationCode, logoUrl),
                    IsBodyHtml = true,
                    Priority = MailPriority.High
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
            }
        }

        private string BuildEmailBody(string verificationCode, string logoUrl)
        {
            return $@"
            <!DOCTYPE html>
            <html lang='es'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Código de Verificación</title>
                <style>
                    body {{
                        font-family: 'Arial', sans-serif;
                        line-height: 1.6;
                        color: #333;
                        max-width: 200px;
                        margin: 0 auto;
                        padding: 20px;
                    }}
                    .email-container {{
                        border: 1px solid #e0e0e0;
                        border-radius: 8px;
                        overflow: hidden;
                    }}
                    .header {{
                        background-color: #1d3335;
                        padding: 20px;
                        text-align: center;
                    }}
                    .logo {{
                        max-width: 300px;
                        height: auto;
                    }}
                    .content {{
                        padding: 30px;
                        background-color: #ffffff;
                    }}
                    .code-container {{
                        background-color: #f5f5f5;
                        border-radius: 4px;
                        padding: 15px;
                        text-align: center;
                        margin: 20px 0;
                        font-size: 24px;
                        font-weight: bold;
                        color: #1845ad;
                    }}
                    .footer {{
                        background-color: #f9f9f9;
                        padding: 15px;
                        text-align: center;
                        font-size: 12px;
                        color: #777;
                    }}
                    .button {{
                        display: inline-block;
                        padding: 10px 20px;
                        background-color: #23a2f6;
                        color: white !important;
                        text-decoration: none;
                        border-radius: 4px;
                        margin: 15px 0;
                    }}
                </style>
            </head>
            <body>
                <div class='email-container'>
                    <div class='header'>
                        <img src='{logoUrl}' alt='Logo de la empresa' class='logo'>
                    </div>
            
                    <div class='content'>
                        <h2>Restablecimiento de contraseña</h2>
                        <p>Hemos recibido una solicitud para restablecer tu contraseña. Utiliza el siguiente código de verificación:</p>
                
                        <div class='code-container'>
                            {verificationCode}
                        </div>
                
                        <p>Este código es válido por <strong>15 minutos</strong>. Si no solicitaste este cambio, puedes ignorar este mensaje.</p>

                    </div>
            
                    <div class='footer'>
                        <p>© {DateTime.Now.Year} Complexless. Todos los derechos reservados.</p>
                        <p>Este es un mensaje automático, por favor no respondas a este correo.</p>
                    </div>
                </div>
            </body>
            </html>";
        }

    }
}
