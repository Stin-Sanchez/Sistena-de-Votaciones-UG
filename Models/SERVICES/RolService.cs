using SIVUG.Models.DAO;
using SIVUG.Models.DTOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.SERVICES
{
    /// <summary>
    /// SERVICIO: Gestión de Lógica de Negocio para Roles
    /// 
    /// Responsabilidades:
    /// - Validar reglas de negocio para roles (nombres únicos, no vacíos)
    /// - Verificar integridad antes de eliminar (validar si hay usuarios asignados)
    /// - Gestionar CRUD de roles
    /// - Orquestar operaciones entre múltiples DAOs
    /// 
    /// NOTA: Este servicio NO accede directamente a la BD,
    /// todas las operaciones se delegan al RolDAO
    /// </summary>
    public class RolService
    {
        private readonly RolDAO _dao;

        // ==================== CONSTANTES DE VALIDACIÓN ====================
        private const int LONGITUD_MINIMA_NOMBRE = 3;
        private const int LONGITUD_MAXIMA_NOMBRE = 50;

        public RolService()
        {
            _dao = new RolDAO();
        }

        // ==================== MÉTODOS DE CONSULTA ====================

        /// <summary>
        /// Obtiene TODOS los roles del sistema.
        /// 
        /// Uso: Combo boxes, listados, asignación de roles
        /// 
        /// Manejo de errores: Retorna lista vacía si falla (nunca nula)
        /// </summary>
        public List<Rol> ObtenerTodos()
        {
            try
            {
                var roles = _dao.ObtenerTodos();
                return roles ?? new List<Rol>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener todos los roles: {ex.Message}");
                return new List<Rol>();
            }
        }

        /// <summary>
        /// Obtiene UN rol específico por ID.
        /// 
        /// Validación: ID debe ser > 0
        /// Retorna: Rol o null si no existe
        /// </summary>
        public Rol ObtenerPorId(int idRol)
        {
            if (idRol <= 0)
                throw new ArgumentException("ID de rol inválido", nameof(idRol));

            try
            {
                return _dao.ObtenerPorId(idRol);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener rol por ID: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene UN rol específico por nombre.
        /// 
        /// Validación: Nombre no puede estar vacío
        /// Retorna: Rol o null si no existe
        /// 
        /// Uso: Obtener ID de rol por nombre descriptivo
        /// </summary>
        public Rol ObtenerPorNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre del rol no puede estar vacío", nameof(nombre));

            try
            {
                return _dao.ObtenerPorNombre(nombre.Trim());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener rol por nombre: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Verifica si un rol existe por ID.
        /// 
        /// Retorna: true si existe, false si no
        /// </summary>
        public bool Existe(int idRol)
        {
            if (idRol <= 0)
                return false;

            try
            {
                return ObtenerPorId(idRol) != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica si un nombre de rol YA existe.
        /// 
        /// Validación: Para evitar duplicados
        /// Retorna: true si existe, false si no
        /// </summary>
        public bool ExisteNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return false;

            try
            {
                return _dao.ExistePorNombre(nombre.Trim());
            }
            catch
            {
                return false;
            }
        }

        // ==================== MÉTODOS DE REGISTRO ====================

        /// <summary>
        /// OPERACIÓN CRÍTICA: Registra un nuevo rol en el sistema.
        /// 
        /// VALIDACIONES (en este orden):
        /// 1. ? Objeto Rol no es nulo
        /// 2. ? Nombre no está vacío
        /// 3. ? Nombre tiene longitud válida (3-50 caracteres)
        /// 4. ? Nombre NO existe ya (prevenir duplicados)
        /// 
        /// Flujo:
        /// 1. Valida todas las reglas de negocio
        /// 2. Llama RolDAO.Insertar()
        /// 3. Retorna true si éxito, false si falla
        /// 
        /// Nota: Lanza excepciones si las validaciones fallan
        /// </summary>
        public bool Registrar(Rol rol)
        {
            // Validación 1: Objeto no nulo
            if (rol == null)
                throw new ArgumentNullException(nameof(rol), "El rol no puede ser nulo");

            // Validación 2: Nombre no vacío
            if (string.IsNullOrWhiteSpace(rol.Nombre))
                throw new ArgumentException("El nombre del rol no puede estar vacío", nameof(rol));

            // Validación 3: Longitud válida
            string nombreLimpio = rol.Nombre.Trim();
            if (nombreLimpio.Length < LONGITUD_MINIMA_NOMBRE)
                throw new ArgumentException($"El nombre del rol debe tener al menos {LONGITUD_MINIMA_NOMBRE} caracteres");

            if (nombreLimpio.Length > LONGITUD_MAXIMA_NOMBRE)
                throw new ArgumentException($"El nombre del rol no debe exceder {LONGITUD_MAXIMA_NOMBRE} caracteres");

            // Validación 4: No existe duplicado
            if (ExisteNombre(nombreLimpio))
                throw new InvalidOperationException($"Ya existe un rol con el nombre '{nombreLimpio}'");

            try
            {
                rol.Nombre = nombreLimpio;
                bool registroExitoso = _dao.Insertar(rol);

                if (!registroExitoso)
                    throw new InvalidOperationException("No se pudo completar el registro en la base de datos");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al registrar rol: {ex.Message}");
                throw new InvalidOperationException($"Error durante el registro: {ex.Message}", ex);
            }
        }

        // ==================== MÉTODOS DE ACTUALIZACIÓN ====================

        /// <summary>
        /// OPERACIÓN CRÍTICA: Actualiza un rol existente.
        /// 
        /// VALIDACIONES:
        /// 1. ? Rol existe en BD
        /// 2. ? Nombre no está vacío
        /// 3. ? Nombre tiene longitud válida (3-50 caracteres)
        /// 4. ? Nuevo nombre NO existe ya (evitar duplicados)
        /// 
        /// Restricción: NO se puede cambiar nombre a uno ya existente
        /// </summary>
        public bool Actualizar(Rol rol)
        {
            if (rol == null)
                throw new ArgumentNullException(nameof(rol), "El rol no puede ser nulo");

            if (rol.IdRol <= 0)
                throw new ArgumentException("ID de rol inválido", nameof(rol));

            // Verificar que existe
            var rolExistente = ObtenerPorId(rol.IdRol);
            if (rolExistente == null)
                throw new InvalidOperationException($"No existe rol con ID {rol.IdRol}");

            // Validar nombre
            if (string.IsNullOrWhiteSpace(rol.Nombre))
                throw new ArgumentException("El nombre del rol no puede estar vacío", nameof(rol));

            string nombreLimpio = rol.Nombre.Trim();
            if (nombreLimpio.Length < LONGITUD_MINIMA_NOMBRE)
                throw new ArgumentException($"El nombre del rol debe tener al menos {LONGITUD_MINIMA_NOMBRE} caracteres");

            if (nombreLimpio.Length > LONGITUD_MAXIMA_NOMBRE)
                throw new ArgumentException($"El nombre del rol no debe exceder {LONGITUD_MAXIMA_NOMBRE} caracteres");

            // Si cambió el nombre, verificar que no exista otro con ese nombre
            if (rolExistente.Nombre != nombreLimpio && ExisteNombre(nombreLimpio))
                throw new InvalidOperationException($"Ya existe un rol con el nombre '{nombreLimpio}'");

            try
            {
                rol.Nombre = nombreLimpio;
                bool actualizacionExitosa = _dao.Actualizar(rol);

                if (!actualizacionExitosa)
                    throw new InvalidOperationException("No se pudo completar la actualización en la base de datos");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar rol: {ex.Message}");
                throw new InvalidOperationException($"Error durante la actualización: {ex.Message}", ex);
            }
        }

        // ==================== MÉTODOS DE ELIMINACIÓN ====================

        /// <summary>
        /// OPERACIÓN CRÍTICA: Elimina un rol del sistema.
        /// 
        /// VALIDACIÓN CRÍTICA:
        /// - NO se puede eliminar si hay usuarios asignados a ese rol
        /// - Esto preserva la integridad referencial
        /// 
        /// Flujo:
        /// 1. Valida que el rol existe
        /// 2. Verifica que NO hay usuarios con este rol
        /// 3. Si pasa validaciones, elimina
        /// 4. Lanza excepción si algo falla
        /// </summary>
        public bool Eliminar(int idRol)
        {
            if (idRol <= 0)
                throw new ArgumentException("ID de rol inválido", nameof(idRol));

            // Verificar que existe
            var rol = ObtenerPorId(idRol);
            if (rol == null)
                throw new InvalidOperationException($"No existe rol con ID {idRol}");

            // VALIDACIÓN CRÍTICA: Verificar que no hay usuarios asignados
            int cantidadUsuarios = _dao.ObtenerCantidadUsuarios(idRol);
            if (cantidadUsuarios > 0)
                throw new InvalidOperationException(
                    $"No se puede eliminar el rol '{rol.Nombre}' porque hay {cantidadUsuarios} usuario(s) asignado(s)");

            try
            {
                bool eliminacionExitosa = _dao.Eliminar(idRol);

                if (!eliminacionExitosa)
                    throw new InvalidOperationException("No se pudo completar la eliminación en la base de datos");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al eliminar rol: {ex.Message}");
                throw new InvalidOperationException($"Error durante la eliminación: {ex.Message}", ex);
            }
        }

        // ==================== MÉTODOS AUXILIARES ====================

        /// <summary>
        /// Obtiene estadísticas de roles del sistema.
        /// 
        /// Retorna objeto anónimo con:
        /// - TotalRoles
        /// - RolesConUsuarios
        /// - RolesSinUsuarios
        /// </summary>
        public dynamic ObtenerEstadisticas()
        {
            try
            {
                var roles = ObtenerTodos();
                int totalRoles = roles.Count;

                int rolesConUsuarios = 0;
                int rolesSinUsuarios = 0;

                foreach (var rol in roles)
                {
                    int cantUsuarios = _dao.ObtenerCantidadUsuarios(rol.IdRol);
                    if (cantUsuarios > 0)
                        rolesConUsuarios++;
                    else
                        rolesSinUsuarios++;
                }

                return new
                {
                    TotalRoles = totalRoles,
                    RolesConUsuarios = rolesConUsuarios,
                    RolesSinUsuarios = rolesSinUsuarios
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener estadísticas de roles: {ex.Message}");
                return null;
            }
        }
    }
}