using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using BCrypt.Net;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;

namespace PrjVigiaCore.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string cad_cn;

        public UsuariosController(IConfiguration configuration)
        {
            _configuration = configuration;
            cad_cn = configuration.GetConnectionString("cn1")!;
        }

        [HttpGet]
        public IActionResult RegistrarUsuario()
        {
            return View();  
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarUsuario([FromBody] dynamic empleadoData)
        {
            try
            {
                // Generar ID de empleado con 8 dígitos comenzando con E
                string idEmpleado = "E" + GenerateRandomDigits(7);

                using var conn = new SqlConnection(cad_cn);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("SP_REGISTRO_EMPLEADO", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Hash de la contraseña con BCrypt 
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword((string)empleadoData.password);

                // Convertir fecha de nacimiento de string a Date
                DateTime fechaNacimiento;
                if (!DateTime.TryParse((string)empleadoData.fechaNacimiento, out fechaNacimiento))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Formato de fecha de nacimiento inválido"
                    });
                }

                // Agregar todos los parámetros necesarios
                cmd.Parameters.AddWithValue("@ID_EMPLEADO", idEmpleado);
                cmd.Parameters.AddWithValue("@DNI", (string)empleadoData.dni);
                cmd.Parameters.AddWithValue("@NOMBRES", (string)empleadoData.nombres);
                cmd.Parameters.AddWithValue("@APE_PAT", (string)empleadoData.apellidoPaterno);
                cmd.Parameters.AddWithValue("@APE_MAT", (string)empleadoData.apellidoMaterno);
                cmd.Parameters.AddWithValue("@FECHA_NACIMIENTO", fechaNacimiento);
                cmd.Parameters.AddWithValue("@GENERO", (string)empleadoData.genero);
                cmd.Parameters.AddWithValue("@DIRECCION", (string)empleadoData.direccion);
                cmd.Parameters.AddWithValue("@DEPARTAMENTO", (string)empleadoData.departamento);
                cmd.Parameters.AddWithValue("@PROVINCIA", (string)empleadoData.provincia);
                cmd.Parameters.AddWithValue("@DISTRITO", (string)empleadoData.distrito);
                cmd.Parameters.AddWithValue("@ESTADO_CIVIL", (string)empleadoData.estadoCivil);
                cmd.Parameters.AddWithValue("@IMAGE_PATH", (string)empleadoData.imagePath ?? "");
                cmd.Parameters.AddWithValue("@EMAIL", (string)empleadoData.email);
                cmd.Parameters.AddWithValue("@USUARIO", (string)empleadoData.usuario);
                cmd.Parameters.AddWithValue("@CONTRASENIA", hashedPassword);
                cmd.Parameters.AddWithValue("@ID_ROL", (string)empleadoData.idRol);
                cmd.Parameters.AddWithValue("@ID_GRUPO", (string)empleadoData.idGrupo);
                cmd.Parameters.AddWithValue("@TIPO", (int)empleadoData.tipo);

                using var reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();

                var resultado = reader.GetBoolean(0);
                var mensaje = reader.GetString(1);
                var idGenerado = reader.GetString(2);

                return Ok(new
                {
                    success = resultado,
                    message = mensaje,
                    idEmpleado = idGenerado
                });
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error en base de datos: {sqlEx.Message}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error interno: {ex.Message}"
                });
            }
        }

        [AuthorizeMenu]
        [HttpGet]
        public async Task<IActionResult> ListarUsuarios()
        {
            try
            {
                List<dynamic> empleados = new List<dynamic>();

                using var conn = new SqlConnection(cad_cn);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("SP_LISTAR_EMPLEADOS", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    dynamic emp = new System.Dynamic.ExpandoObject();
                    emp.ID_EMPLEADO = reader["ID_EMPLEADO"] != DBNull.Value ? reader["ID_EMPLEADO"] : null;
                    emp.DNI = reader["DNI"] != DBNull.Value ? reader["DNI"] : null;
                    emp.USUARIO = reader["USUARIO"] != DBNull.Value ? reader["USUARIO"] : null;
                    emp.NOMBRE_COMPLETO = reader["NOMBRE_COMPLETO"] != DBNull.Value ? reader["NOMBRE_COMPLETO"] : null;
                    emp.EDAD = reader["EDAD"] != DBNull.Value ? reader["EDAD"] : null;
                    emp.GENERO = reader["GENERO"] != DBNull.Value ? reader["GENERO"] : null;
                    emp.IMAGE_PATH = reader["IMAGE_PATH"] != DBNull.Value ? reader["IMAGE_PATH"].ToString() : string.Empty;
                    emp.GRUPOS_ASIGNADOS = reader["GRUPOS_ASIGNADOS"] != DBNull.Value ? reader["GRUPOS_ASIGNADOS"] : null;
                    emp.EMAIL = reader["EMAIL"] != DBNull.Value ? reader["EMAIL"] : null;

                    empleados.Add(emp);
                }
                ViewBag.TotalRegistros = empleados.Count();

                return View(empleados);
            }
            catch (Exception)
            {
                ViewBag.Error = "¡Ups! Algo salió mal. Intentalo de nuevo mas tarde.";
                return View(new List<dynamic>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarEmpleado(
        [FromForm] string idEmpleado,
        [FromForm] string dni,
        [FromForm] string nombres,
        [FromForm] string apePat,
        [FromForm] string apeMat,
        [FromForm] string fechaNacimiento,
        [FromForm] string genero,
        [FromForm] string telefono,
        [FromForm] string direccion,
        [FromForm] string departamento,
        [FromForm] string provincia,
        [FromForm] string distrito,
        [FromForm] string estadoCivil,
        [FromForm] string usuario,
        [FromForm] string email,
        [FromForm] string contrasenia,
        [FromForm] string idRol,
        [FromForm] string idGrupo,
        [FromForm] int tipo,
        [FromForm] IFormFile imagen)
        {
            try
            {
                // Validaciones básicas
                if (string.IsNullOrEmpty(dni) || string.IsNullOrEmpty(nombres) ||
                    string.IsNullOrEmpty(apePat) || string.IsNullOrEmpty(telefono) ||
                    string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(email) ||
                    string.IsNullOrEmpty(contrasenia) || string.IsNullOrEmpty(idRol) ||
                    string.IsNullOrEmpty(idGrupo))
                {
                    return Json(new { success = false, message = "Todos los campos obligatorios deben ser completados" });
                }

                string contraseniaHash = BCrypt.Net.BCrypt.HashPassword(contrasenia);

                // Procesar imagen
                string imagePath = null;
                if (imagen != null && imagen.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "empleados");
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

                    // Crear nombre de archivo usando el ID del empleado
                    var fileName = $"{idEmpleado}{extension}";
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

                    imagePath = $"/images/empleados/{fileName}";
                }

                // Obtener usuario de sesión
                var usuarioRegistro = HttpContext.Session.GetString("ID_Usuario");
                if (string.IsNullOrEmpty(usuarioRegistro))
                {
                    return Json(new { success = false, message = "Sesión expirada. Por favor inicie sesión nuevamente." });
                }

                // Ejecutar SP
                using (SqlConnection cnn = new SqlConnection(cad_cn))
                {
                    await cnn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("SP_REGISTRO_EMPLEADO", cnn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetros para EMPLEADO
                        cmd.Parameters.AddWithValue("@ID_EMPLEADO", idEmpleado);
                        cmd.Parameters.AddWithValue("@DNI", dni);
                        cmd.Parameters.AddWithValue("@NOMBRES", nombres);
                        cmd.Parameters.AddWithValue("@APE_PAT", apePat);
                        cmd.Parameters.AddWithValue("@APE_MAT", apeMat);
                        cmd.Parameters.AddWithValue("@FECHA_NACIMIENTO", string.IsNullOrEmpty(fechaNacimiento) ? DBNull.Value : (object)DateTime.Parse(fechaNacimiento));
                        cmd.Parameters.AddWithValue("@GENERO", string.IsNullOrEmpty(genero) ? DBNull.Value : (object)genero);
                        cmd.Parameters.AddWithValue("@TELEFONO", telefono);
                        cmd.Parameters.AddWithValue("@DIRECCION", string.IsNullOrEmpty(direccion) ? DBNull.Value : (object)direccion);
                        cmd.Parameters.AddWithValue("@DEPARTAMENTO", string.IsNullOrEmpty(departamento) ? DBNull.Value : (object)departamento);
                        cmd.Parameters.AddWithValue("@PROVINCIA", string.IsNullOrEmpty(provincia) ? DBNull.Value : (object)provincia);
                        cmd.Parameters.AddWithValue("@DISTRITO", string.IsNullOrEmpty(distrito) ? DBNull.Value : (object)distrito);
                        cmd.Parameters.AddWithValue("@ESTADO_CIVIL", string.IsNullOrEmpty(estadoCivil) ? DBNull.Value : (object)estadoCivil);
                        cmd.Parameters.AddWithValue("@IMAGE_PATH", string.IsNullOrEmpty(imagePath) ? DBNull.Value : (object)imagePath);

                        // Parámetros para USUARIOS_SISTEMA
                        cmd.Parameters.AddWithValue("@USUARIO", usuario);
                        cmd.Parameters.AddWithValue("@EMAIL", email);
                        cmd.Parameters.AddWithValue("@CONTRASENIA", contraseniaHash);
                        cmd.Parameters.AddWithValue("@ID_ROL", idRol);

                        // Parámetros para DESTINATARIOS
                        cmd.Parameters.AddWithValue("@ID_GRUPO", idGrupo);
                        cmd.Parameters.AddWithValue("@TIPO", tipo);

                        // Parámetros de salida
                        SqlParameter resultadoParam = new SqlParameter("@RESULTADO", SqlDbType.Bit);
                        resultadoParam.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(resultadoParam);

                        SqlParameter mensajeParam = new SqlParameter("@MENSAJE", SqlDbType.VarChar, 200);
                        mensajeParam.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(mensajeParam);

                        await cmd.ExecuteNonQueryAsync();

                        bool resultado = (bool)resultadoParam.Value;
                        string mensaje = mensajeParam.Value.ToString();

                        return Json(new
                        {
                            success = resultado,
                            message = mensaje,
                            idEmpleado = idEmpleado
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al registrar el usuario: " + ex.Message });
            }
        }


        [HttpGet]
        public IActionResult ObtenerRoles()
        {
            try
            {
                var roles = new List<dynamic>();
                using (SqlConnection cnn = new SqlConnection(cad_cn))
                {
                    cnn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT ID_ROL, ROL FROM ROLES", cnn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                roles.Add(new
                                {
                                    idRol = reader["ID_ROL"].ToString(),
                                    nombre = reader["ROL"].ToString()
                                });
                            }
                        }
                    }
                }
                return Json(roles);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ObtenerGrupos()
        {
            try
            {
                var grupos = new List<dynamic>();
                using (SqlConnection cnn = new SqlConnection(cad_cn))
                {
                    cnn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT ID_GRUPO, NOM_GRUPO FROM GRUPO_DESTINA WHERE ESTADO = 1", cnn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                grupos.Add(new
                                {
                                    idGrupo = reader["ID_GRUPO"].ToString(),
                                    nombre = reader["NOM_GRUPO"].ToString()
                                });
                            }
                        }
                    }
                }
                return Json(grupos);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DetallesUsuario(string id)
        {
            using (SqlConnection cnn = new SqlConnection(cad_cn))
            {
                await cnn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand("SP_PERFIL", cnn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@ID_EMPLEADO", SqlDbType.VarChar, 8) { Value = id });

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var employeeData = new
                            {
                                IdEmpleado = reader["ID_EMPLEADO"].ToString(),
                                DNI = reader["DNI"].ToString(),
                                Usuario = reader["USUARIO"].ToString(),
                                Email = reader["EMAIL"].ToString(),
                                Nombres = reader["NOMBRES"].ToString(),
                                ApePat = reader["APE_PAT"].ToString(),
                                ApeMat = reader["APE_MAT"].ToString(),
                                NombreCompleto = reader["NOMBRE_COMPLETO"].ToString(),
                                FechaNacimiento = Convert.ToDateTime(reader["FECHA_NACIMIENTO"]).ToString("dd/MM/yyyy"),
                                Edad = reader["EDAD"].ToString(),
                                Genero = reader["GENERO"].ToString(),
                                GeneroDesc = reader["GENERO_DESC"].ToString(),
                                EstadoCivil = reader["ESTADO_CIVIL"].ToString(),
                                DireccionCompleta = reader["DIRECCION_COMPLETA"].ToString(),
                                ImagePath = reader["IMAGE_PATH"].ToString(),
                                GruposAsignados = reader["GRUPOS_ASIGNADOS"].ToString(),
                                FechaRegistro = reader["FECHA_REGISTRO"].ToString(),
                                FechaActualizacion = reader["FECHA_ACTUALIZACION"].ToString()
                            };
                            return Json(employeeData);
                        }
                        else
                        {
                            return NotFound("No se encontraron datos del empleado.");
                        }
                    }
                }
            }
        }

        private string GenerateRandomDigits(int length)
        {
            var random = new Random();
            var sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                sb.Append(random.Next(0, 9));
            }
            return sb.ToString();
        }

  
        private string HashPasswordSHA256(string password)
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
    }
}