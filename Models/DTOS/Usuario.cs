using SIVUG.Models.DTOS.SIVUG.Models.DTOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.DTOS
{
    /// <summary>
    /// MODELO: Usuario - Credenciales de Acceso al Sistema
    /// 
   
    /// 
    /// NOTA IMPORTANTE:
    /// - NO almacena contraseña en texto plano (debe hashearse en Service)
    /// - La validación de credenciales ocurre en la capa de servicio

    /// </summary>
    public class Usuario
    {
        // ==================== PROPIEDADES ====================

        /// <summary>
        /// ID Único del Usuario en la Base de Datos
        /// PK en tabla usuarios
        /// Generado automáticamente (auto_increment)
        /// </summary>
        public int IdUsuario { get; set; }

        /// <summary>
        /// Nombre de Usuario (identificador de login)
        /// Ej: "admin", "jdoe", "fgutierrez"
        /// Longitud esperada: 3-20 caracteres
        /// NO puede estar vacío o duplicado (UNIQUE en BD)
        /// </summary>
        public string NombreUsuario { get; set; }

        /// <summary>
        /// Contraseña Hasheada del Usuario
        /// IMPORTANTE: NUNCA almacenar en texto plano
        /// Debe hashearse usando bcrypt, SHA-256 u otro algoritmo seguro en la capa de Service
        /// 
        /// NOTA DE SEGURIDAD:
        /// - La contraseña debe validarse en Service antes de persistir
        /// - Cumplir política de complejidad (mayúsculas, números, caracteres especiales)
        /// - NO exponerse en logs o mensajes de error
        /// </summary>
        public string Contraseña { get; set; }

        // ==================== RELACIONES DE NAVEGACIÓN ====================

        /// <summary>
        /// ID del Rol asignado al Usuario
        /// FK a tabla roles
        /// Determina los permisos y funcionalidades disponibles
        /// </summary>
        public int IdRol { get; set; }

        /// <summary>
        /// Objeto de Navegación: Rol del Usuario
        /// Relación Many-to-One (Múltiples usuarios un rol)
        /// Usado para cargar la información del rol (nombre, permisos, etc.)
        /// </summary>
        public Rol Rol { get; set; }


        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public bool Activo { get; set; } = true;

        /// <summary>
        /// Objeto de Navegación: Persona asociada al Usuario
        /// Relación One-to-One (Un Usuario puede tener UNA Persona)
        /// 
        /// IMPORTANTE: La Persona pertenece al Usuario, NO al revés
        /// La FK id_usuario está en tabla personas
        /// 
        /// NULL si el usuario NO tiene perfil de persona (ej: admin del sistema)
        /// 
        /// Uso: Acceder a datos personales desde autenticación
        /// Ejemplo: usuario.Persona.Nombres, usuario.Persona.DNI
        /// </summary>
        public Persona Persona { get; set; }


        // ==================== CONSTRUCTORES ====================

        /// <summary>
        /// Constructor Vacío
        /// Usado por reflexión del ORM o inicialización manual
        /// </summary>
        public Usuario()
        {
        }

        /// <summary>
        /// Constructor Parametrizado Básico
        /// Inicializa credenciales y rol
        /// 
        /// Uso: Crear usuario con rol sin asociar a persona
        /// Ej: Administrador del sistema sin perfil de persona
        /// </summary>
        /// <param name="nombreUsuario">Nombre de usuario (login)</param>
        /// <param name="contraseña">Contraseña hasheada</param>
        /// <param name="idRol">ID del rol a asignar</param>
        public Usuario(string nombreUsuario, string contraseña, int idRol)
        {
            this.NombreUsuario = nombreUsuario;
            this.Contraseña = contraseña;
            this.IdRol = idRol;
            this.FechaRegistro= DateTime.Now;
            this.Activo= true;

        }

        /// <summary>
        /// Constructor Parametrizado Completo
        /// Inicializa credenciales, rol y persona
        /// 
        /// Uso: Crear usuario con perfil de persona completo
        /// Ej: Estudiante con usuario y datos personales
        /// </summary>
        /// <param name="nombreUsuario">Nombre de usuario (login)</param>
        /// <param name="contraseña">Contraseña hasheada</param>
        /// <param name="idRol">ID del rol a asignar</param>
        /// <param name="persona">Persona asociada (con FK id_usuario ya establecida)</param>
        public Usuario(string nombreUsuario, string contraseña, int idRol, Persona persona)
        {
            this.NombreUsuario = nombreUsuario;
            this.Contraseña = contraseña;
            this.IdRol = idRol;
            this.Persona = persona;
        }


        // ==================== MÉTODOS ====================

        /// <summary>
        /// Obtiene una representación en string del usuario
        /// Usado para logs, debugging y mensajes
        /// Formato: "NombreUsuario (NombreRol)"
        /// </summary>
        /// <returns>String con información del usuario</returns>
        public string GetInfoCompleta()
        {
            if (Rol != null)
            {
                return $"{NombreUsuario} ({Rol.Nombre})";
            }
            return NombreUsuario;
        }

        /// <summary>
        /// Obtiene el nombre completo si tiene persona asociada
        /// Si no tiene persona, retorna el nombre de usuario
        /// </summary>
        /// <returns>Nombre completo de la persona o nombre de usuario</returns>
        public string GetNombreCompleto()
        {
            if (Persona != null)
            {
                return Persona.GetNombreCompleto();
            }
            return NombreUsuario;
        }

        /// <summary>
        /// Verifica si el usuario tiene una persona (perfil) asociada
        /// </summary>
        /// <returns>true si tiene persona, false si no</returns>
        public bool TienePersona()
        {
            return Persona != null;
        }


        /// <summary>
        /// Flag que indica si el usuario debe cambiar su contraseña en el próximo login.
        /// 
        /// CASOS DE USO:
        /// - Estudiantes registrados por Admin con contraseña default (DNI)
        /// - Usuarios con contraseñas temporales
        /// - Contraseñas comprometidas que requieren cambio inmediato
        /// 
        /// FLUJO:
        /// 1. Admin registra estudiante → RequiereCambioContraseña = true
        /// 2. Estudiante hace login → Sistema detecta flag
        /// 3. FormCambiarContraseña se abre en modo obligatorio
        /// 4. Al cambiar contraseña → RequiereCambioContraseña = false
        /// </summary>
        public bool RequiereCambioContraseña { get; set; }
    }
}