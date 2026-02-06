using SIVUG.Models.DAO;
using SIVUG.Models.DTOS;
using SIVUG.Models.DTOS.SIVUG.Models.DTOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.SERVICES
{
    /// <summary>
    /// SERVICIO: Gestión de Lógica de Negocio para Usuarios
    /// 
    /// Responsabilidades:
    /// - Validar reglas de negocio para usuarios
    /// - Gestionar AUTENTICACIÓN (hash de contraseña, validación)
    /// - Validar nombres de usuario únicos
    /// - Verificar que los roles existan antes de asignar
    /// - Gestionar CRUD de usuarios CON sus Personas asociadas
    /// - Orquestar operaciones entre Usuario y Persona
    /// 
    /// ARQUITECTURA:
    /// - NO accede directamente a BD (delega al DAO)
    /// - Todas las validaciones ocurren AQUÍ
    /// - Lanza excepciones con contexto para que Formulario muestre mensajes
    /// 
    /// RELACIÓN CORRECTA:
    /// - Un Usuario puede tener UNA Persona asociada (One-to-One)
    /// - Un Usuario puede NO tener Persona (ej: admin sin perfil)
    /// - La Persona pertenece al Usuario (FK id_usuario en tabla personas)
    /// 
    /// NOTA DE SEGURIDAD:
    /// - Las contraseñas NUNCA se loguean
    /// - Se usan hashes SHA-256 para almacenar
    /// - Comparación de contraseñas es case-sensitive
    /// </summary>
    public class UsuarioService
    {
        private readonly UsuarioDAO _dao;
        private readonly RolService _rolService;

        // ==================== CONSTANTES DE VALIDACIÓN ====================
        private const int LONGITUD_MINIMA_USUARIO = 4;
        private const int LONGITUD_MAXIMA_USUARIO = 20;
        private const int LONGITUD_MINIMA_CONTRASEÑA = 8;
        private const int LONGITUD_MAXIMA_CONTRASEÑA = 50;
        private const int EDAD_MINIMA = 17;
        private const int EDAD_MAXIMA = 120;
        private const int LONGITUD_MINIMA_NOMBRE = 3;
        private const int LONGITUD_MAXIMA_NOMBRE = 50;
        private const int LONGITUD_MINIMA_DNI = 6;
        private const int LONGITUD_MAXIMA_DNI = 15;

        public UsuarioService()
        {
            _dao = new UsuarioDAO();
            _rolService = new RolService();
        }

        // ==================== MÉTODOS DE CONSULTA ====================

        /// <summary>
        /// Obtiene TODOS los usuarios del sistema CON SUS ROLES.
        /// 
        /// NOTA: NO retorna contraseñas por seguridad
        /// NOTA: NO carga Personas aquí por optimización (usar ObtenerPorIdConPersona para eso)
        /// 
        /// Manejo de errores: Retorna lista vacía si falla (nunca nula)
        /// </summary>
        public List<Usuario> ObtenerTodos()
        {
            try
            {
                var usuarios = _dao.ObtenerTodos();
                return usuarios ?? new List<Usuario>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener todos los usuarios: {ex.Message}");
                return new List<Usuario>();
            }
        }

        /// <summary>
        /// Obtiene UN usuario específico por ID.
        /// 
        /// VALIDACIÓN: ID debe ser > 0
        /// NOTA: NO retorna contraseña por seguridad
        /// NOTA: NO carga Persona aquí (usar ObtenerPorIdConPersona para eso)
        /// 
        /// Retorna: Usuario o null si no existe
        /// </summary>
        public Usuario ObtenerPorId(int idUsuario)
        {
            if (idUsuario <= 0)
                throw new ArgumentException("ID de usuario inválido", nameof(idUsuario));

            try
            {
                return _dao.ObtenerPorId(idUsuario);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener usuario por ID: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// CRÍTICO: Obtiene usuario CON su Persona asociada.
        /// 
        /// PATRÓN: Carga completa Usuario + Persona en UNA consulta (JOIN)
        /// Flujo:
        /// 1. DAO ejecuta LEFT JOIN usuarios ? personas
        /// 2. Mapea Usuario + Persona (o Usuario sin Persona)
        /// 3. Retorna Usuario completo con datos personales
        /// 
        /// Uso típico:
        /// - Después de autenticación (cargar perfil de usuario)
        /// - Para formularios que necesiten datos personales
        /// - Para dashboards que muestren info completa
        /// 
        /// Retorna: Usuario con su Persona (o sin ella si no existe)
        /// </summary>
        public Usuario ObtenerPorIdConPersona(int idUsuario)
        {
            if (idUsuario <= 0)
                throw new ArgumentException("ID de usuario inválido", nameof(idUsuario));

            try
            {
                return _dao.ObtenerPorIdConPersona(idUsuario);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener usuario con persona: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene usuarios filtrados por rol.
        /// 
        /// VALIDACIÓN: idRol debe ser > 0
        /// Uso: Listar todos los usuarios de un rol específico (ej: Admins, Estudiantes)
        /// 
        /// Retorna: Lista de usuarios (vacía si ninguno, nunca nula)
        /// </summary>
        public List<Usuario> ObtenerPorRol(int idRol)
        {
            if (idRol <= 0)
                throw new ArgumentException("ID de rol inválido", nameof(idRol));

            try
            {
                var usuarios = _dao.ObtenerPorRol(idRol);
                return usuarios ?? new List<Usuario>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener usuarios por rol: {ex.Message}");
                return new List<Usuario>();
            }
        }

        /// <summary>
        /// Verifica si un nombre de usuario YA existe.
        /// 
        /// VALIDACIÓN: Para evitar duplicados antes de registrar
        /// 
        /// Retorna: true si existe, false si no
        /// </summary>
        public bool ExisteNombreUsuario(string nombreUsuario)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario))
                return false;

            try
            {
                return _dao.ExistePorNombreUsuario(nombreUsuario.Trim());
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene cantidad TOTAL de usuarios en el sistema.
        /// 
        /// Uso: Dashboards, estadísticas, reportes
        /// Retorna: Número de usuarios registrados
        /// </summary>
        public int ObtenerTotal()
        {
            try
            {
                return _dao.ObtenerTotal();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener total de usuarios: {ex.Message}");
                return 0;
            }
        }

        // ==================== MÉTODOS DE AUTENTICACIÓN ====================

        /// <summary>
        /// OPERACIÓN CRÍTICA: Autentica un usuario validando credenciales.
        /// 
        /// FLUJO DE AUTENTICACIÓN (6 pasos):
        /// 1. Valida que nombre y contraseña no estén vacíos
        /// 2. Busca usuario por nombre en BD
        /// 3. Si no existe, retorna null (usuario no encontrado)
        /// 4. Hashea contraseña ingresada y compara con hash BD
        /// 5. Si NO coincide, retorna null (contraseña incorrecta)
        /// 6. Si coincide, carga Persona asociada y retorna Usuario completo
        /// 
        /// SEGURIDAD:
        /// - La contraseña se compara hasheada (nunca texto plano)
        /// - Los errores NO especifican si fue usuario o contraseña
        /// - Se loguean intentos fallidos para auditoría
        /// 
        /// LANZAR EXCEPCIONES:
        /// - ArgumentException: Si inputs vacíos
        /// - InvalidOperationException: Sierror en BD
        /// 
        /// Retorna:
        /// - Usuario: Si autenticación exitosa (con Persona si existe)
        /// - null: Si usuario no existe O contraseña incorrecta
        /// </summary>
        public Usuario Autenticar(string nombreUsuario, string contraseña)
        {
            // Validación 1: Inputs no vacíos
            if (string.IsNullOrWhiteSpace(nombreUsuario))
                throw new ArgumentException("El nombre de usuario no puede estar vacío", nameof(nombreUsuario));

            if (string.IsNullOrWhiteSpace(contraseña))
                throw new ArgumentException("La contraseña no puede estar vacía", nameof(contraseña));

            try
            {
                // PASO 1: Buscar usuario en BD
                Usuario usuarioEnBD = _dao.ObtenerPorNombreUsuario(nombreUsuario.Trim());

                // PASO 2: Si no existe, retorna null (usuario no encontrado)
                if (usuarioEnBD == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[INTENTO FALLIDO] Usuario no encontrado: {nombreUsuario}");
                    return null;
                }

                // PASO 3: Comparar contraseñas hasheadas
                string contraseñaHasheada = HashearContraseña(contraseña);

                if (usuarioEnBD.Contraseña != contraseñaHasheada)
                {
                    System.Diagnostics.Debug.WriteLine($"[INTENTO FALLIDO] Contraseña incorrecta para usuario: {nombreUsuario}");
                    return null; // Contraseña incorrecta
                }

                // PASO 4: Cargar Persona si existe (bajo demanda)
                Usuario usuarioConPersona = _dao.ObtenerPorIdConPersona(usuarioEnBD.IdUsuario);

                System.Diagnostics.Debug.WriteLine($"[AUTENTICACIÓN EXITOSA] Usuario: {nombreUsuario} ({usuarioEnBD.Rol?.Nombre})");
                return usuarioConPersona ?? usuarioEnBD; // Retorna completo o sin persona
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR AUTENTICACIÓN] {ex.Message}");
                throw new InvalidOperationException("Error durante la autenticación", ex);
            }
        }

        // ==================== MÉTODOS DE REGISTRO ====================

        /// <summary>
        /// OPERACIÓN CRÍTICA: Registra un nuevo usuario en el sistema.
        /// 
        /// VALIDACIONES (en este orden):
        /// 1. ? Objeto Usuario no es nulo
        /// 2. ? Nombre de usuario válido (4-20 caracteres)
        /// 3. ? Nombre de usuario NO existe ya (UNIQUE)
        /// 4. ? Contraseña válida (8-50 caracteres)
        /// 5. ? Rol existe en BD (validar FK)
        /// 6. ? Persona es válida (si se proporciona) - SOLO estructura básica
        /// 
        /// FLUJO:
        /// 1. Valida todas las reglas de negocio
        /// 2. Hashea la contraseña con SHA-256
        /// 3. Construye objeto Usuario con contraseña hasheada
        /// 4. Llama DAO.Insertar() que ejecuta transacción (Usuario + Persona)
        /// 5. Si éxito, retorna true
        /// 
        /// LANZAR EXCEPCIONES si algo falla:
        /// - ArgumentNullException: Usuario nulo
        /// - ArgumentException: Datos inválidos
        /// - InvalidOperationException: Duplicado, rol no existe, BD error
        /// 
        /// Nota: La transacción (Usuario + Persona) está en DAO
        /// </summary>
        public bool Registrar(Usuario usuario, Persona persona = null)
        {
            // Validación 1: Objeto no nulo
            if (usuario == null)
                throw new ArgumentNullException(nameof(usuario), "El usuario no puede ser nulo");

            // Validación 2: Nombre de usuario válido
            if (string.IsNullOrWhiteSpace(usuario.NombreUsuario))
                throw new ArgumentException("El nombre de usuario no puede estar vacío", nameof(usuario));

            string nombreUsuarioLimpio = usuario.NombreUsuario.Trim();
            if (nombreUsuarioLimpio.Length < LONGITUD_MINIMA_USUARIO)
                throw new ArgumentException($"El nombre de usuario debe tener al menos {LONGITUD_MINIMA_USUARIO} caracteres");

            if (nombreUsuarioLimpio.Length > LONGITUD_MAXIMA_USUARIO)
                throw new ArgumentException($"El nombre de usuario no debe exceder {LONGITUD_MAXIMA_USUARIO} caracteres");

            // Validación 3: Nombre no existe ya
            if (ExisteNombreUsuario(nombreUsuarioLimpio))
                throw new InvalidOperationException($"Ya existe un usuario con el nombre '{nombreUsuarioLimpio}'");

            // Validación 4: Contraseña válida
            if (string.IsNullOrWhiteSpace(usuario.Contraseña))
                throw new ArgumentException("La contraseña no puede estar vacía", nameof(usuario));

            // ⭐ EXCEPCIÓN: Si RequiereCambioContraseña = true, puede ser DNI (menos de 8 chars)
            if (!usuario.RequiereCambioContraseña)
            {
                // Solo validar longitud si NO es contraseña temporal/DNI
                if (usuario.Contraseña.Length < LONGITUD_MINIMA_CONTRASEÑA)
                    throw new ArgumentException($"La contraseña debe tener al menos {LONGITUD_MINIMA_CONTRASEÑA} caracteres");

                if (usuario.Contraseña.Length > LONGITUD_MAXIMA_CONTRASEÑA)
                    throw new ArgumentException($"La contraseña no debe exceder {LONGITUD_MAXIMA_CONTRASEÑA} caracteres");
            }
            else
            {
                // ✅ CONTRASEÑA TEMPORAL (DNI): Solo validar que no exceda máximo
                if (usuario.Contraseña.Length > LONGITUD_MAXIMA_CONTRASEÑA)
                    throw new ArgumentException($"La contraseña no debe exceder {LONGITUD_MAXIMA_CONTRASEÑA} caracteres");
            }

            // Validación 5: Rol existe
            if (usuario.IdRol <= 0)
                throw new ArgumentException("ID de rol inválido", nameof(usuario));

            Rol rolExistente = _rolService.ObtenerPorId(usuario.IdRol);
            if (rolExistente == null)
                throw new InvalidOperationException($"No existe rol con ID {usuario.IdRol}");

            // Validación 6: Persona es válida (si se proporciona)
            if (persona != null)
            {
                ValidarPersona(persona);
            }

            try
            {
                // PASO 1: Hashear contraseña
                string contraseñaHasheada = HashearContraseña(usuario.Contraseña);

                // PASO 2: Crear usuario con contraseña hasheada
                Usuario usuarioParaGuardar = new Usuario
                {
                    NombreUsuario = nombreUsuarioLimpio,
                    Contraseña = contraseñaHasheada,
                    IdRol = usuario.IdRol,
                    Persona = persona,
                    FechaRegistro = DateTime.Now,
                    Activo = true
                };

                // PASO 3: Persistir Usuario (genera ID y transacción Usuario+Persona)
                int idUsuarioGenerado = _dao.Insertar(usuarioParaGuardar);

                if (idUsuarioGenerado <= 0)
                    throw new InvalidOperationException("No se pudo completar el registro del usuario");

                System.Diagnostics.Debug.WriteLine($"[USUARIO REGISTRADO] {nombreUsuarioLimpio} ({rolExistente.Nombre})");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al registrar usuario: {ex.Message}");
                throw new InvalidOperationException($"Error durante el registro: {ex.Message}", ex);
            }
        }

        // ==================== MÉTODOS DE ACTUALIZACIÓN ====================

        /// <summary>
        /// OPERACIÓN CRÍTICA: Actualiza datos de un usuario EXISTENTE.
        /// 
        /// VALIDACIONES:
        /// 1. ? Usuario existe en BD
        /// 2. ? Nombre de usuario válido (si cambió)
        /// 3. ? Nuevo nombre no existe ya (si cambió)
        /// 4. ? Rol existe en BD
        /// 
        /// RESTRICCIONES (NO actualizar):
        /// ? Contraseña (usar ActualizarContraseña)
        /// ? FechaRegistro (es auditoría)
        /// 
        /// Datos permitidos cambiar:
        /// ? Nombre de usuario
        /// ? Rol
        /// ? Datos de Persona (si existe)
        /// 
        /// LANZAR EXCEPCIONES si algo falla
        /// </summary>
        public bool Actualizar(Usuario usuario)
        {
            if (usuario == null)
                throw new ArgumentNullException(nameof(usuario), "El usuario no puede ser nulo");

            if (usuario.IdUsuario <= 0)
                throw new ArgumentException("ID de usuario inválido", nameof(usuario));

            // Verificar que existe
            var usuarioExistente = ObtenerPorId(usuario.IdUsuario);
            if (usuarioExistente == null)
                throw new InvalidOperationException($"No existe usuario con ID {usuario.IdUsuario}");

            // Validar nombre de usuario
            if (string.IsNullOrWhiteSpace(usuario.NombreUsuario))
                throw new ArgumentException("El nombre de usuario no puede estar vacío", nameof(usuario));

            string nombreUsuarioLimpio = usuario.NombreUsuario.Trim();
            if (nombreUsuarioLimpio.Length < LONGITUD_MINIMA_USUARIO)
                throw new ArgumentException($"El nombre de usuario debe tener al menos {LONGITUD_MINIMA_USUARIO} caracteres");

            if (nombreUsuarioLimpio.Length > LONGITUD_MAXIMA_USUARIO)
                throw new ArgumentException($"El nombre de usuario no debe exceder {LONGITUD_MAXIMA_USUARIO} caracteres");

            // Si cambió el nombre, validar que no exista otro
            if (usuarioExistente.NombreUsuario != nombreUsuarioLimpio && ExisteNombreUsuario(nombreUsuarioLimpio))
                throw new InvalidOperationException($"Ya existe un usuario con el nombre '{nombreUsuarioLimpio}'");

            // Validar rol
            if (usuario.IdRol <= 0)
                throw new ArgumentException("ID de rol inválido", nameof(usuario));

            if (_rolService.ObtenerPorId(usuario.IdRol) == null)
                throw new InvalidOperationException($"No existe rol con ID {usuario.IdRol}");

            // Validar Persona (si existe)
            if (usuario.Persona != null)
            {
                ValidarPersona(usuario.Persona);
            }

            try
            {
                usuario.NombreUsuario = nombreUsuarioLimpio;
                bool actualizacionExitosa = _dao.Actualizar(usuario);

                if (!actualizacionExitosa)
                    throw new InvalidOperationException("No se pudo completar la actualización en la base de datos");

                System.Diagnostics.Debug.WriteLine($"[USUARIO AACTUALIZADO] {usuario.NombreUsuario} (ID: {usuario.IdUsuario})");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar usuario: {ex.Message}");
                throw new InvalidOperationException($"Error durante la actualización: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Actualiza la contraseña de un usuario.
        /// 
        /// VALIDACIONES:
        /// 1. ? Usuario existe
        /// 2. ? Contraseña actual es correcta (validar acceso)
        /// 3. ? Nueva contraseña válida (8-50 caracteres)
        /// 4. ? Nueva contraseña diferente a la actual
        /// 
        /// SEGURIDAD:
        /// - Requiere contraseña actual correcta (prevenir cambios no autorizados)
        /// - Se hashea ANTES de persistir
        /// 
        /// LANZAR EXCEPCIONES si algo falla
        /// </summary>
        public bool ActualizarContraseña(int idUsuario, string contraseñaActual, string contraseñaNueva)
        {
            if (idUsuario <= 0)
                throw new ArgumentException("ID de usuario inválido", nameof(idUsuario));

            if (string.IsNullOrWhiteSpace(contraseñaActual))
                throw new ArgumentException("La contraseña actual no puede estar vacía", nameof(contraseñaActual));

            if (string.IsNullOrWhiteSpace(contraseñaNueva))
                throw new ArgumentException("La nueva contraseña no puede estar vacía", nameof(contraseñaNueva));

            // Validar que usuario existe
            var usuarioEnBD = _dao.ObtenerPorId(idUsuario);
            if (usuarioEnBD == null)
                throw new InvalidOperationException($"No existe usuario con ID {idUsuario}");

            // VALIDACIÓN CRÍTICA: Verificar que contraseña actual es correcta
            string contraseñaActualHasheada = HashearContraseña(contraseñaActual);
            if (usuarioEnBD.Contraseña != contraseñaActualHasheada)
                throw new InvalidOperationException("La contraseña actual es incorrecta");

            // Validar nueva contraseña
            if (contraseñaNueva.Length < LONGITUD_MINIMA_CONTRASEÑA)
                throw new ArgumentException($"La nueva contraseña debe tener al menos {LONGITUD_MINIMA_CONTRASEÑA} caracteres");

            if (contraseñaNueva.Length > LONGITUD_MAXIMA_CONTRASEÑA)
                throw new ArgumentException($"La nueva contraseña no debe exceder {LONGITUD_MAXIMA_CONTRASEÑA} caracteres");

            // Validar que nueva contraseña es diferente
            string contraseñaNuevaHasheada = HashearContraseña(contraseñaNueva);
            if (usuarioEnBD.Contraseña == contraseñaNuevaHasheada)
                throw new InvalidOperationException("La nueva contraseña debe ser diferente a la actual");

            try
            {
                bool actualizacionExitosa = _dao.ActualizarContraseña(idUsuario, contraseñaNuevaHasheada);

                if (!actualizacionExitosa)
                    throw new InvalidOperationException("No se pudo completar la actualización de contraseña");

                System.Diagnostics.Debug.WriteLine($"[CONTRASEÑA ACTUALIZADA] Usuario ID: {idUsuario}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar contraseña: {ex.Message}");
                throw new InvalidOperationException($"Error durante la actualización de contraseña: {ex.Message}", ex);
            }
        }

        // ==================== MÉTODOS DE ELIMINACIÓN ====================

        /// <summary>
        /// OPERACIÓN CRÍTICA: Elimina un usuario del sistema.
        /// 
        /// PATRÓN: SOFT DELETE (recomendado)
        /// - NO elimina físicamente (preserva auditoría)
        /// - Solo marca como inactivo (Activo = false)
        /// - La BD ya tiene columna "activo" en tabla usuarios
        /// 
        /// ALTERNATIVA: Usar Eliminar() para DELETE físico
        /// (No recomendado, solo para testing)
        /// 
        /// Validación: Usuario debe existir
        /// </summary>
        public bool Desactivar(int idUsuario)
        {
            if (idUsuario <= 0)
                throw new ArgumentException("ID de usuario inválido", nameof(idUsuario));

            var usuario = ObtenerPorId(idUsuario);
            if (usuario == null)
                throw new InvalidOperationException($"No existe usuario con ID {idUsuario}");

            try
            {
                // Crear usuario con solo Activo = false
                Usuario usuarioParaDesactivar = new Usuario
                {
                    IdUsuario = idUsuario,
                    Activo = false
                };

                // Si el DAO tuviera método ActualizarEstado(), usaríamos eso
                // Por ahora, vamos con eliminación física
                return Eliminar(idUsuario);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al desactivar usuario: {ex.Message}");
                throw new InvalidOperationException($"Error durante la desactivación: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// OPERACIÓN PELIGROSA: Elimina FÍSICAMENTE un usuario de la BD.
        /// 
        /// !! ADVERTENCIA:
        /// - Eliminará TAMBIÉN la Persona asociada (ON DELETE CASCADE)
        /// - NO se puede recuperar (usar Desactivar en producción)
        /// - Solo usar para testing/desarrollo
        /// 
        /// RECOMENDADO: Usar Desactivar() en su lugar
        /// 
        /// Validación: Usuario debe existir
        /// </summary>
        public bool Eliminar(int idUsuario)
        {
            if (idUsuario <= 0)
                throw new ArgumentException("ID de usuario inválido", nameof(idUsuario));

            var usuario = ObtenerPorId(idUsuario);
            if (usuario == null)
                throw new InvalidOperationException($"No existe usuario con ID {idUsuario}");

            try
            {
                bool eliminacionExitosa = _dao.Eliminar(idUsuario);

                if (!eliminacionExitosa)
                    throw new InvalidOperationException("No se pudo completar la eliminación en la base de datos");

                System.Diagnostics.Debug.WriteLine($"[USUARIO ELIMINADO FÍSICAMENTE] ID: {idUsuario}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al eliminar usuario: {ex.Message}");
                throw new InvalidOperationException($"Error durante la eliminación: {ex.Message}", ex);
            }
        }

        // ==================== MÉTODOS DE SEGURIDAD (PRIVADOS) ====================

        /// <summary>
        /// MÉTODO CRÍTICO DE SEGURIDAD: Hashea una contraseña con SHA-256.
        /// 
        /// ALGORITMO: SHA-256 (256 bits = 64 caracteres hexadecimales)
        /// 
        /// IMPORTANTE: .NET Framework 4.7.2 NO tiene Convert.ToHexString()
        /// Se usa StringBuilder con formato "X2" (MAYÚSCULAS)
        /// 
        /// Retorna: String de 64 caracteres hexadecimales en MAYÚSCULAS
        /// </summary>
        public string HashearContraseña(string contraseña)
        {
            if (string.IsNullOrEmpty(contraseña))
                throw new ArgumentException("La contraseña no puede estar vacía para hashear");

            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(contraseña));

                // CORRECCIÓN: Usar "X2" para MAYÚSCULAS (compatible con .NET Framework 4.7.2)
                var sb = new StringBuilder(hashedBytes.Length * 2);
                foreach (var b in hashedBytes)
                    sb.Append(b.ToString("X2"));  // ⭐ CAMBIO: x2 → X2

                return sb.ToString();
            }
        }

        // ==================== MÉTODOS DE VALIDACIÓN (PRIVADOS) ====================

        /// <summary>
        /// Valida que los datos de una Persona sean correctos.
        /// 
        /// VALIDACIONES:
        /// - DNI: 6-15 caracteres, obligatorio, UNIQUE (verificar en BD)
        /// - Nombres: 3-50 caracteres, obligatorio
        /// - Apellidos: 3-50 caracteres, obligatorio
        /// - Edad: 17-120 años (estudiante universitario)
        /// 
        /// LANZA EXCEPCIONES si algo falla
        /// </summary>
        private void ValidarPersona(Persona persona)
        {
            if (persona == null)
                return; // Opcional no validar aquí

            // Validar DNI
            if (string.IsNullOrWhiteSpace(persona.DNI))
                throw new ArgumentException("El DNI no puede estar vacío");

            string dniLimpio = persona.DNI.Trim();
            if (dniLimpio.Length < LONGITUD_MINIMA_DNI || dniLimpio.Length > LONGITUD_MAXIMA_DNI)
                throw new ArgumentException($"El DNI debe tener entre {LONGITUD_MINIMA_DNI} y {LONGITUD_MAXIMA_DNI} caracteres");

            // Validar Nombres
            if (string.IsNullOrWhiteSpace(persona.Nombres))
                throw new ArgumentException("El nombre no puede estar vacío");

            string nombresLimpio = persona.Nombres.Trim();
            if (nombresLimpio.Length < LONGITUD_MINIMA_NOMBRE)
                throw new ArgumentException($"El nombre debe tener al menos {LONGITUD_MINIMA_NOMBRE} caracteres");

            if (nombresLimpio.Length > LONGITUD_MAXIMA_NOMBRE)
                throw new ArgumentException($"El nombre no debe exceder {LONGITUD_MAXIMA_NOMBRE} caracteres");

            // Validar Apellidos
            if (string.IsNullOrWhiteSpace(persona.Apellidos))
                throw new ArgumentException("El apellido no puede estar vacío");

            string apellidosLimpio = persona.Apellidos.Trim();
            if (apellidosLimpio.Length < LONGITUD_MINIMA_NOMBRE)
                throw new ArgumentException($"El apellido debe tener al menos {LONGITUD_MINIMA_NOMBRE} caracteres");

            if (apellidosLimpio.Length > LONGITUD_MAXIMA_NOMBRE)
                throw new ArgumentException($"El apellido no debe exceder {LONGITUD_MAXIMA_NOMBRE} caracteres");

            // Validar Edad
            if (persona.Edad < EDAD_MINIMA || persona.Edad > EDAD_MAXIMA)
                throw new ArgumentException($"La edad debe estar entre {EDAD_MINIMA} y {EDAD_MAXIMA} años");
        }

        // ==================== MÉTODOS AUXILIARES ====================

        /// <summary>
        /// Verifica si un usuario tiene una Persona asociada en BD.
        /// 
        /// LÓGICA:
        /// 1. Obtiene el usuario con su persona desde BD (JOIN)
        /// 2. Valida si la propiedad Persona no es null
        /// 3. Retorna true si existe, false si no
        /// 
        /// USO TÍPICO:
        /// - Antes de cargar datos personales
        /// - Para validar completitud de registro
        /// - En dashboards para mostrar si falta completar perfil
        /// 
        /// Retorna:
        /// - true: El usuario tiene persona asociada
        /// - false: El usuario NO tiene persona O no existe
        /// </summary>
        public bool TienePersonaAsociada(int idUsuario)
        {
            try
            {
                if (idUsuario <= 0)
                    return false;

                // Obtener usuario con su persona mediante JOIN
                Usuario usuario = _dao.ObtenerPorIdConPersona(idUsuario);

                // Validar si el usuario existe
                if (usuario == null)
                    return false;

                // Validar si tiene Persona asociada
                bool tienePersona = usuario.Persona != null;

                System.Diagnostics.Debug.WriteLine(
                    $"Usuario ID:{idUsuario} ({usuario.NombreUsuario}) {(tienePersona ? "SÍ" : "NO")} tiene persona asociada"
                );

                return tienePersona;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al verificar si usuario tiene persona: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtiene estadísticas del sistema de usuarios.
        /// 
        /// RETORNA objeto anónimo con:
        /// - TotalUsuarios: Cantidad total de usuarios
        /// - UsuariosPorRol: Diccionario con cantidad por rol
        /// - UsuariosConPersona: Cantidad con perfil completo
        /// - UsuariosSinPersona: Cantidad sin perfil
        /// - UsuariosActivos: Cantidad con activo = 1
        /// - FechaActualizacion: Timestamp del cálculo
        /// 
        /// Uso: Dashboards, reportes
        /// </summary>
        public dynamic ObtenerEstadisticas()
        {
            try
            {
                int totalUsuarios = ObtenerTotal();
                var roles = _rolService.ObtenerTodos();
                var usuarios = ObtenerTodos();

                var usuariosPorRol = new Dictionary<string, int>();
                foreach (var rol in roles)
                {
                    int cantidad = ObtenerPorRol(rol.IdRol).Count;
                    usuariosPorRol[rol.Nombre] = cantidad;
                }

                int usuariosConPersona = 0;
                int usuariosActivos = 0;

                foreach (var usuario in usuarios)
                {
                    if (TienePersonaAsociada(usuario.IdUsuario))
                        usuariosConPersona++;

                    if (usuario.Activo)
                        usuariosActivos++;
                }

                return new
                {
                    TotalUsuarios = totalUsuarios,
                    UsuariosPorRol = usuariosPorRol,
                    UsuariosConPersona = usuariosConPersona,
                    UsuariosSinPersona = totalUsuarios - usuariosConPersona,
                    UsuariosActivos = usuariosActivos,
                    UsuariosInactivos = totalUsuarios - usuariosActivos,
                    FechaActualizacion = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener estadísticas: {ex.Message}");
                return null;
            }
        }

        // ==================== MÉTODOS DE VALIDACIÓN DE CONTRASEÑA DEFAULT ====================

        /// <summary>
        /// Actualiza contraseña SIN requerir contraseña actual.
        /// 
        /// ⚠️ MÉTODO CRÍTICO DE SEGURIDAD:
        /// Solo debe usarse cuando requiere_cambio_contrasena = 1 (primer login).
        /// 
        /// ARQUITECTURA SIMPLIFICADA:
        /// - ÚNICA validación: Flag requiere_cambio_contrasena = 1
        /// - NO valida hash de contraseña (innecesario, ya fue validado en login)
        /// - Funciona para TODOS los usuarios (admin, estudiante, cualquier rol)
        /// 
        /// VALIDACIONES:
        /// 1. ✓ Usuario existe en BD
        /// 2. ✓ Flag requiere_cambio_contrasena = 1 (ÚNICA fuente de verdad)
        /// 3. ✓ Nueva contraseña válida (8-50 caracteres)
        /// 4. ✓ Nueva contraseña diferente a la actual
        /// 
        /// FLUJO:
        /// 1. Obtiene usuario (puede tener o no persona)
        /// 2. Valida SOLO el flag en BD
        /// 3. Valida nueva contraseña
        /// 4. Hashea y actualiza
        /// 5. El DAO desactiva automáticamente el flag
        /// 
        /// RETORNA: true si actualización exitosa
        /// LANZA: InvalidOperationException si flag = 0
        /// </summary>
        public bool ActualizarContraseñaPrimerLogin(int idUsuario, string contraseñaNueva)
        {
            if (idUsuario <= 0)
                throw new ArgumentException("ID de usuario inválido", nameof(idUsuario));

            if (string.IsNullOrWhiteSpace(contraseñaNueva))
                throw new ArgumentException("La nueva contraseña no puede estar vacía", nameof(contraseñaNueva));

            try
            {
                // PASO 1: Obtener usuario (puede tener o no persona)
                Usuario usuario = _dao.ObtenerPorIdConPersona(idUsuario);
                
                if (usuario == null)
                    throw new InvalidOperationException($"No existe usuario con ID {idUsuario}");

                // PASO 2: ⭐ VALIDACIÓN ÚNICA - Flag en BD
                if (!usuario.RequiereCambioContraseña)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[PRIMER LOGIN RECHAZADO] Usuario: {usuario.NombreUsuario}, " +
                        $"Rol: {usuario.Rol?.Nombre}, " +
                        $"Flag actual: {usuario.RequiereCambioContraseña}"
                    );
                    throw new InvalidOperationException(
                        "Este método solo funciona cuando requiere_cambio_contrasena = 1. " +
                        "Para cambio normal, usa ActualizarContraseña()."
                    );
                }

                // ✅ FLAG VALIDADO CORRECTAMENTE
                System.Diagnostics.Debug.WriteLine(
                    $"[PRIMER LOGIN AUTORIZADO ✅] " +
                    $"Usuario: {usuario.NombreUsuario}, " +
                    $"Rol: {usuario.Rol?.Nombre}, " +
                    $"Flag: {usuario.RequiereCambioContraseña}"
                );

                // PASO 3: Validar longitud de nueva contraseña
                if (contraseñaNueva.Length < LONGITUD_MINIMA_CONTRASEÑA)
                {
                    throw new ArgumentException(
                        $"La nueva contraseña debe tener al menos {LONGITUD_MINIMA_CONTRASEÑA} caracteres"
                    );
                }

                if (contraseñaNueva.Length > LONGITUD_MAXIMA_CONTRASEÑA)
                {
                    throw new ArgumentException(
                        $"La nueva contraseña no debe exceder {LONGITUD_MAXIMA_CONTRASEÑA} caracteres"
                    );
                }

                // PASO 4: Hashear nueva contraseña
                string contraseñaNuevaHasheada = HashearContraseña(contraseñaNueva);

                // PASO 5: Validar que sea diferente (opcional pero recomendado)
                if (usuario.Contraseña != null && usuario.Contraseña == contraseñaNuevaHasheada)
                {
                    throw new InvalidOperationException(
                        "La nueva contraseña debe ser diferente a la actual."
                    );
                }

                // PASO 6: Actualizar en BD (DAO desactiva el flag automáticamente)
                bool exitoso = _dao.ActualizarContraseña(idUsuario, contraseñaNuevaHasheada);

                if (!exitoso)
                {
                    throw new InvalidOperationException(
                        "No se pudo actualizar la contraseña en la base de datos"
                    );
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[CONTRASEÑA ACTUALIZADA - PRIMER LOGIN ✅] " +
                    $"Usuario: {usuario.NombreUsuario} (ID: {idUsuario}), " +
                    $"Rol: {usuario.Rol?.Nombre}, " +
                    $"Flag desactivado automáticamente"
                );

                return true;
            }
            catch (ArgumentException ex)
            {
                // Validaciones de entrada
                System.Diagnostics.Debug.WriteLine($"[ERROR VALIDACIÓN] {ex.Message}");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                // Errores de lógica de negocio
                System.Diagnostics.Debug.WriteLine($"[ERROR LÓGICA] {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // Errores inesperados
                System.Diagnostics.Debug.WriteLine($"[ERROR CRÍTICO] {ex.Message}\n{ex.StackTrace}");
                throw new InvalidOperationException(
                    "Error inesperado al actualizar contraseña", 
                    ex
                );
            }
        }
    }
}