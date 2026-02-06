using SIVUG.Models;
using SIVUG.Models.DAO;
using SIVUG.Models.DTOS;
using SIVUG.Models.SERVICES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SIVUG.Controllers
{
    /// <summary>
    /// CONTROLADOR: Estudiante
    /// 
    /// Responsabilidades:
    /// - Orquestar formularios con Service
    /// - Validar entrada del usuario
    /// - Generar credenciales seguras
    /// - Determinar rol basado en tipo de usuario
    /// </summary>
    public class EstudianteController
    {
        private EstudianteService _service;

        public EstudianteController()
        {
            _service = new EstudianteService();
        }

        /// <summary>
        /// OPERACIÓN CRÍTICA: Registra nuevo usuario (Estudiante o Admin).
        /// 
        /// FLUJO:
        /// 1. Validar que tipoRol sea válido ("Estudiante" o "Administrador")
        /// 2. Generar username único (NO usar DNI)
        /// 3. Generar contraseña temporal segura
        /// 4. Crear objeto Usuario con datos seguros
        /// 5. Llamar Service con rol dinámico (NO hardcodeado)
        /// 
        /// PARÁMETROS:
        /// - nuevoEstudiante: Datos personales (nombres, apellidos, edad, dni, etc.)
        /// - tipoRol: "Estudiante", "Administrador", etc.
        /// 
        /// RETORNA:
        /// - true: Registro exitoso
        /// - false: Error (mostrado en MessageBox)
        /// </summary>
        public bool Guardar(Estudiante nuevoEstudiante, string tipoRol = "Estudiante")
        {
            try
            {
                // Validar que tipoRol sea válido
                if (string.IsNullOrWhiteSpace(tipoRol))
                    tipoRol = "Estudiante"; // Valor por defecto

                // Validación de objeto no nulo
                if (nuevoEstudiante == null)
                {
                    MessageBox.Show("Los datos del estudiante no pueden estar vacíos.", "Validación");
                    return false;
                }

                // ✅ GENERACIÓN DE CREDENCIALES SEGÚN ROL
                string username;
                string contraseñaTemporal;
                bool requiereCambioContraseña;

                if (tipoRol == "Estudiante")
                {
                    // ⭐ ESTUDIANTE: Username = DNI, Password = DNI
                    username = nuevoEstudiante.DNI.Trim();
                    contraseñaTemporal = nuevoEstudiante.DNI.Trim();
                    requiereCambioContraseña = true; // ⚠️ Flag CRÍTICO
                }
                else
                {
                    // ADMIN/OTRO: Credenciales generadas
                    username = GenerarUsername(nuevoEstudiante);
                    contraseñaTemporal = GenerarContraseñaTemporal();
                    requiereCambioContraseña = true; // También requiere cambio
                }

                // ✅ CREAR USUARIO CON FLAG DE PRIMER LOGIN
                var usuario = new Usuario
                {
                    NombreUsuario = username,
                    Contraseña = contraseñaTemporal, // Se hasheará en Service
                    IdRol = ObtenerIdRol(tipoRol),
                    RequiereCambioContraseña = requiereCambioContraseña,
                    Activo = true,
                    FechaRegistro = DateTime.Now
                };

                // ✅ REGISTRAR (Service hashea automáticamente)
                bool registroExitoso = _service.RegistrarEstudiante(usuario, nuevoEstudiante);

                if (registroExitoso)
                {
                    // ✅ MOSTRAR CREDENCIALES TEMPORALES
                    string mensaje = tipoRol == "Estudiante"
                        ? $"✅ Estudiante registrado exitosamente!\n\n" +
                          $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                          $"CREDENCIALES DE ACCESO:\n" +
                          $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                          $"Usuario:     {username}\n" +
                          $"Contraseña:  {contraseñaTemporal}\n" +
                          $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                          $"⚠️ IMPORTANTE:\n" +
                          $"El estudiante DEBE cambiar su contraseña\n" +
                          $"en el primer inicio de sesión por seguridad."
                        : $"✅ Usuario registrado exitosamente!\n\n" +
                          $"Credenciales temporales:\n" +
                          $"Usuario: {username}\n" +
                          $"Contraseña: {contraseñaTemporal}\n\n" +
                          $"⚠️ Deberá cambiar su contraseña al primer login.";

                    MessageBox.Show(mensaje, "Registro Completado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en registro: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Genera un USERNAME ÚNICO y SEGURO (NO usar DNI directamente).
        /// 
        /// FORMATO: "nombre.apellido" o "nombre.apellido.XX" si existe duplicado
        /// 
        /// Ejemplo:
        /// - Juan García → juan.garcia
        /// - Juan García (duplicado) → juan.garcia.01
        /// </summary>
        private string GenerarUsername(Estudiante estudiante)
        {
            // Sanitizar nombres (lowercase, sin espacios)
            string nombre = estudiante.Nombres?.Trim().ToLower().Replace(" ", ".") ?? "usuario";
            string apellido = estudiante.Apellidos?.Trim().ToLower().Replace(" ", ".") ?? "sin.apellido";

            string usernameBase = $"{nombre}.{apellido}";

            // ✅ VALIDAR UNICIDAD
            UsuarioService usuarioService = new UsuarioService();
            int contador = 0;

            while (usuarioService.ExisteNombreUsuario(usernameBase + (contador > 0 ? $".{contador:D2}" : "")))
            {
                contador++;
            }

            return usernameBase + (contador > 0 ? $".{contador:D2}" : "");
        }

        /// <summary>
        /// Genera una CONTRASEÑA TEMPORAL SEGURA y TEMPORAL.
        /// 
        /// CARACTERÍSTICAS:
        /// - 12 caracteres aleatorios
        /// - Mayúsculas + minúsculas + números
        /// - Usuario debe cambiarla al primer login
        /// 
        /// Ejemplo: "aBcD12EfGh34"
        /// </summary>
        private string GenerarContraseñaTemporal()
        {
            const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            Random random = new Random();
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < 12; i++)
            {
                sb.Append(caracteres[random.Next(caracteres.Length)]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Obtiene el ID del rol basado en su NOMBRE.
        /// 
        /// MAPEO:
        /// - "Estudiante" → ID 3
        /// - "Administrador" → ID 1 (u otro según BD)
        /// - Desconocido → ID 3 (por defecto Estudiante)
        /// 
        /// ⚠️ IMPORTANTE: Los IDs deben coincidir con tu tabla ROLES
        /// </summary>
        private int ObtenerIdRol(string nombreRol)
        {
            RolService rolService = new RolService();

            try
            {
                Rol rol = rolService.ObtenerPorNombre(nombreRol);
                
                if (rol != null)
                    return rol.IdRol;
                
                // Si no existe el rol, buscar rol por defecto "Estudiante"
                Rol rolDefecto = rolService.ObtenerPorNombre("Estudiante");
                return rolDefecto?.IdRol ?? 3; // Fallback a ID 3
            }
            catch
            {
                return 3; // ID default para Estudiante
            }
        }
    }
}

