using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using BCrypt.Net;
using System.Text;
using System.Security.Cryptography;

namespace PrjVigiaCore.Controllers
{
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
                    emp.DIRECCION_COMPLETA = reader["DIRECCION_COMPLETA"] != DBNull.Value ? reader["DIRECCION_COMPLETA"] : null;
                    emp.IMAGE_PATH = reader["IMAGE_PATH"] != DBNull.Value ? reader["IMAGE_PATH"].ToString() : string.Empty;
                    emp.GRUPOS_ASIGNADOS = reader["GRUPOS_ASIGNADOS"] != DBNull.Value ? reader["GRUPOS_ASIGNADOS"] : null;
                    emp.EMAIL = reader["EMAIL"] != DBNull.Value ? reader["EMAIL"] : null;

                    empleados.Add(emp);
                }

                return View(empleados);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "¡Ups! Algo salió mal. Intentalo de nuevo mas tarde.";
                return View(new List<dynamic>());
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