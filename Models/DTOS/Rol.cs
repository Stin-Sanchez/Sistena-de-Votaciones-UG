using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.DTOS
{
    /// <summary>
    /// MODELO: Rol - Define los Roles de Usuario en el Sistema
    /// 
    /// Responsabilidades:
    /// - Representar los diferentes tipos de usuarios (Admin, Estudiante, Votante, etc.)
    /// - Servir como catálogo de roles reutilizable
    /// 
    /// NORMALIZACIÓN:
    /// - Separado de la tabla Usuarios (normalizando Many-to-One)
    /// - Un rol puede tener múltiples usuarios
    /// 
    /// RELACIÓN BD:
    /// Tabla: roles (id_rol, nombre)
    /// 
    /// HERENCIA Y COMPOSICIÓN:
    /// - Usuario.Rol -> Rol (relación de navegación)
    /// - Esta clase NO hereda de Persona (los roles pueden asignarse a cualquier entidad)
    /// 
    /// NOTA: Esta clase NO tiene lógica de negocio.
    /// Solo almacena datos. La validación ocurre en Service.
    /// </summary>
    public class Rol
    {
        // ==================== PROPIEDADES ====================

        /// <summary>
        /// ID Único del Rol en la Base de Datos
        /// PK en tabla roles
        /// Generado automáticamente (auto_increment)
        /// </summary>
        public int IdRol { get; set; }

        /// <summary>
        /// Nombre Descriptivo del Rol
        /// Ej: "Administrador", "Estudiante", "Votante", "Jurado"
        /// Longitud esperada: 3-50 caracteres
        /// NO puede estar vacío o duplicado
        /// </summary>
        public string Nombre { get; set; }

        // ==================== RELACIONES DE NAVEGACIÓN ====================

        /// <summary>
        /// Colección de Usuarios que tienen este Rol
        /// Relación One-to-Many (Un rol múltiples usuarios)
        /// Usado para carga lazy en consultas complejas
        /// </summary>
        public List<Usuario> Usuarios { get; set; } = new List<Usuario>();

        // ==================== CONSTRUCTORES ====================

        /// <summary>
        /// Constructor Vacío
        /// Usado por reflexión del ORM o inicialización manual
        /// </summary>
        public Rol()
        {
        }

        /// <summary>
        /// Constructor Parametrizado
        /// Inicializa los campos principales del rol
        /// 
        /// Uso: Generalmente en DAO al mapear resultados de BD
        /// </summary>
        /// <param name="idRol">ID generado en BD</param>
        /// <param name="nombre">Nombre descriptivo del rol</param>
        public Rol(int idRol, string nombre)
        {
            this.IdRol = idRol;
            this.Nombre = nombre;
        }

        // ==================== MÉTODOS ====================

        /// <summary>
        /// Obtiene una representación en string del rol
        /// Usado para logs, debugging y UI
        /// </summary>
        /// <returns>String con el nombre del rol</returns>
        public override string ToString()
        {
            return $"{Nombre}";
        }
    }
}