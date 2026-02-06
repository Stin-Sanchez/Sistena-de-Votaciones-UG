using SIVUG.Models.DAO;
using SIVUG.Models.DTOS;
using SIVUG.Models.DTOS.SIVUG.Models.DTOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.SERVICES
{
    /// <summary>
    /// SERVICIO: Gestión de Lógica de Negocio para Estudiantes
    /// 
    /// Responsabilidades:
    /// - Validar reglas de negocio específicas de estudiantes (edad, matrícula, carrera)
    /// - Orquestar registro de estudiante CON usuario
    /// - Buscar estudiantes por cédula o ID
    /// - DELEGAR autenticación a UsuarioService (NO hacer aquí)
    /// - Gestionar datos académicos (votación, carrera, semestre)
    /// 
    /// ARQUITECTURA:
    /// - Usa EstudianteDAO para acceso a BD
    /// - Usa UsuarioService para autenticación (composición)
    /// - Valida SOLO lógica de negocio de estudiante
    /// 
    /// REGLAS DE NEGOCIO:
    /// - Edad mínima: 17 años (estudiante universitario)
    /// - Edad máxima: 120 años (validación básica)
    /// - DNI es obligatorio y UNIQUE
    /// - Matrícula es obligatoria y UNIQUE
    /// - Semestre: 1-10 (típicamente)
    /// - Carrera: debe existir en BD
    /// 
    /// IMPORTANTE:
    /// - NO hacer autenticación aquí
    /// - Usar UsuarioService.Autenticar() en FormLogin
    /// - Este Service solo maneja datos de estudiante
    /// </summary>
    public class EstudianteService
    {
        private readonly EstudianteDAO _dao;
        private readonly UsuarioService _usuarioService;
        private readonly RolService _rolService;

        // ==================== CONSTANTES DE VALIDACIÓN ====================
        private const int EDAD_MINIMA = 17;
        private const int EDAD_MAXIMA = 120;
        private const byte SEMESTRE_MINIMO = 1;
        private const byte SEMESTRE_MAXIMO = 10;
        private const int LONGITUD_MINIMA_MATRICULA = 5;
        private const int LONGITUD_MAXIMA_MATRICULA = 30;

        public EstudianteService()
        {
            _dao = new EstudianteDAO();
            _usuarioService = new UsuarioService();
            _rolService = new RolService();
        }

        // ==================== MÉTODOS DE CONSULTA ====================

        /// <summary>
        /// Busca un estudiante por CÉDULA (DNI).
        /// 
        /// FLUJO:
        /// 1. Valida que cédula no esté vacía
        /// 2. Llama EstudianteDAO.ObtenerPorCedula()
        /// 3. Retorna Estudiante completo con Usuario, Carrera y Facultad
        /// 
        /// RETORNA:
        /// - Estudiante con todos sus datos relacionados
        /// - null si no existe
        /// 
        /// Uso: Búsqueda en FormRegistroCandidatas, FormRegistroVotos, etc.
        /// </summary>
        public Estudiante ObtenerPorCedula(string cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula))
                throw new ArgumentException("La cédula es requerida", nameof(cedula));

            try
            {
                Estudiante estudiante = _dao.ObtenerPorCedula(cedula.Trim());

                if (estudiante != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ESTUDIANTE ENCONTRADO] Cédula: {cedula}, Nombres: {estudiante.Nombres}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ESTUDIANTE NO ENCONTRADO] Cédula: {cedula}");
                }

                return estudiante;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al buscar estudiante por cédula: {ex.Message}");
                throw new InvalidOperationException($"Error al buscar estudiante: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtiene un estudiante por su ID (id_persona).
        /// 
        /// FLUJO:
        /// 1. Valida que ID sea válido (> 0)
        /// 2. Llama EstudianteDAO.ObtenerPorId()
        /// 3. Retorna Estudiante completo
        /// 
        /// Uso: Búsqueda por ID en dashboards, formularios específicos
        /// </summary>
        public Estudiante ObtenerPorId(int idEstudiante)
        {
            if (idEstudiante <= 0)
                throw new ArgumentException("ID de estudiante inválido", nameof(idEstudiante));

            try
            {
                return _dao.ObtenerPorId(idEstudiante);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener estudiante por ID: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene TODOS los estudiantes del sistema con datos completos.
        /// 
        /// FLUJO:
        /// 1. Llama EstudianteDAO.ObtenerTodosDetallado()
        /// 2. Retorna lista con Usuario, Carrera y Facultad anidados
        /// 
        /// NOTA: Operación potencialmente costosa si hay muchos estudiantes
        /// Considerar agregar paginación en producción
        /// 
        /// Retorna: List<Estudiante> (nunca nulo, vacío si no hay)
        /// </summary>
        public List<Estudiante> ObtenerTodos()
        {
            try
            {
                var estudiantes = _dao.ObtenerTodosDetallado();
                return estudiantes ?? new List<Estudiante>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener todos los estudiantes: {ex.Message}");
                return new List<Estudiante>();
            }
        }

        // ==================== MÉTODOS DE REGISTRO ====================

        /// <summary>
        /// OPERACIÓN CRÍTICA: Registra un nuevo estudiante CON usuario en el sistema.
        /// 
        /// FLUJO TRANSACCIONAL (3 pasos):
        /// 1. ✓ Validar Usuario (credenciales)
        /// 2. ✓ Validar Estudiante (datos académicos)
        /// 3. ✓ Crear Usuario + Persona + Estudiante en BD (una transacción)
        /// 
        /// VALIDACIONES DE USUARIO:
        /// - Username: 4-20 caracteres, único
        /// - Contraseña: 8-50 caracteres
        /// - Rol: Debe existir en BD (típicamente "Estudiante")
        /// 
        /// VALIDACIONES DE ESTUDIANTE:
        /// - Edad: 17-120 años
        /// - DNI: 6-15 caracteres, único
        /// - Nombres/Apellidos: 3-50 caracteres
        /// - Matrícula: 5-30 caracteres, única
        /// - Semestre: 1-10
        /// - Carrera: Debe existir en BD
        /// 
        /// FLUJO:
        /// 1. Valida todos los datos
        /// 2. Obtiene rol "Estudiante" de BD
        /// 3. Hashea contraseña del usuario ⭐ CRÍTICO
        /// 4. Llama EstudianteDAO.RegistrarEstudianteConUsuario()
        /// 5. Transacción persiste: Usuario → Persona → Estudiante
        /// 
        /// LANZAR EXCEPCIONES si algo falla
        /// 
        /// Retorna: true si registro exitoso
        /// </summary>
        public bool RegistrarEstudiante(Usuario usuario, Estudiante estudiante)
        {
            // Validación 1: Objetos no nulos
            if (usuario == null)
                throw new ArgumentNullException(nameof(usuario), "Usuario no puede ser nulo");

            if (estudiante == null)
                throw new ArgumentNullException(nameof(estudiante), "Estudiante no puede ser nulo");

            // Validación 2: Usuario válido (delegado a UsuarioService)
            ValidarUsuarioEstudiante(usuario);

            // Validación 3: Estudiante válido
            ValidarEstudiante(estudiante);

            try
            {
                // PASO 1: Obtener rol "Estudiante"
                Rol rolEstudiante = _rolService.ObtenerPorNombre("Estudiante");
                if (rolEstudiante == null)
                    throw new InvalidOperationException("No existe rol 'Estudiante' en el sistema");

                usuario.IdRol = rolEstudiante.IdRol;

                // ⭐⭐⭐ PASO 2 CRÍTICO: HASHEAR CONTRASEÑA ⭐⭐⭐
                // La contraseña viene en texto plano del Controller
                // Debemos hashearla ANTES de enviarla al DAO
                string contraseñaHasheada = _usuarioService.HashearContraseña(usuario.Contraseña);

                // PASO 3: Crear usuario con contraseña hasheada Y FLAG PRESERVADO
                Usuario usuarioParaGuardar = new Usuario
                {
                    NombreUsuario = usuario.NombreUsuario,
                    Contraseña = contraseñaHasheada,
                    IdRol = usuario.IdRol,
                    Persona = usuario.Persona,
                    FechaRegistro = DateTime.Now,
                    Activo = true,
                    RequiereCambioContraseña = usuario.RequiereCambioContraseña
                };

                System.Diagnostics.Debug.WriteLine(
                    $"[REGISTRO] Usuario: {usuarioParaGuardar.NombreUsuario}, " +
                    $"RequiereCambio: {usuarioParaGuardar.RequiereCambioContraseña}, " +
                    $"Hash: {contraseñaHasheada.Substring(0, 16)}..."
                );

                // PASO 4: Persistir en BD (transacción de 3 pasos)
                int idEstudianteGenerado = _dao.RegistrarEstudianteConUsuario(usuarioParaGuardar, estudiante);

                if (idEstudianteGenerado <= 0)
                    throw new InvalidOperationException("No se pudo completar el registro del estudiante");

                System.Diagnostics.Debug.WriteLine($"[ESTUDIANTE REGISTRADO] {estudiante.Nombres} {estudiante.Apellidos} (Usuario: {usuarioParaGuardar.NombreUsuario})");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al registrar estudiante: {ex.Message}");
                throw new InvalidOperationException($"Error durante el registro: {ex.Message}", ex);
            }

        }

        // ==================== MÉTODOS DE AUTENTICACIÓN ====================

        /// <summary>
        /// ⭐ PUNTO CRÍTICO: AUTENTICACIÓN DE ESTUDIANTE
        /// 
        /// ⚠️ IMPORTANTE: USAR AQUÍ UsuarioService.Autenticar()
        /// 
        /// FLUJO CORRECTO:
        /// 1. FormLogin llama EstudianteService.AutenticarEstudiante(username, contraseña)
        /// 2. Valida inputs
        /// 3. DELEGA a UsuarioService.Autenticar(username, contraseña)
        /// 4. Service retorna Usuario CON Persona (que es Estudiante)
        /// 5. Validar que es Estudiante (rol = "Estudiante")
        /// 6. Retornar Estudiante completo
        /// 
        /// ❌ NUNCA hacer autenticación aquí directamente
        /// ❌ NUNCA comparar contraseñas en este método
        /// ❌ NUNCA acceder a BD directamente para autenticar
        /// 
        /// RAZÓN: La seguridad y hasheo es responsabilidad de UsuarioService
        /// Este Service solo valida que sea estudiante
        /// 
        /// Retorna: Estudiante autenticado, o null si credenciales inválidas
        /// </summary>
        public Estudiante AutenticarEstudiante(string username, string contraseña)
        {
            // Validación 1: Inputs no vacíos
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("El nombre de usuario no puede estar vacío", nameof(username));

            if (string.IsNullOrWhiteSpace(contraseña))
                throw new ArgumentException("La contraseña no puede estar vacía", nameof(contraseña));

            try
            {
                // ⭐⭐⭐ PUNTO CRÍTICO: DELEGAR A UsuarioService ⭐⭐⭐
                Usuario usuarioAutenticado = _usuarioService.Autenticar(username.Trim(), contraseña);

                // Si retorna null, credenciales inválidas
                if (usuarioAutenticado == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTENTICACIÓN FALLIDA] Usuario: {username}");
                    return null;
                }

                // Validar que el usuario NO tiene Persona (no es estudiante aún)
                // O si tiene, verificar que sea Estudiante
                if (usuarioAutenticado.Persona == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Usuario {username} no tiene perfil de persona");
                    throw new InvalidOperationException("El usuario no tiene perfil de estudiante");
                }

                // Cast: Persona a Estudiante (Estudiante hereda de Persona)
                if (!(usuarioAutenticado.Persona is Estudiante))
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Usuario {username} no es estudiante");
                    throw new InvalidOperationException("El usuario no es un estudiante registrado");
                }

                Estudiante estudiante = usuarioAutenticado.Persona as Estudiante;
                estudiante.Usuario = usuarioAutenticado;

                System.Diagnostics.Debug.WriteLine($"[AUTENTICACIÓN EXITOSA] Estudiante: {estudiante.Nombres} ({username})");
                return estudiante;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR AUTENTICACIÓN] {ex.Message}");
                throw new InvalidOperationException("Error durante la autenticación de estudiante", ex);
            }
        }

        // ==================== MÉTODOS DE VALIDACIÓN (PRIVADOS) ====================

        /// <summary>
        /// Valida que un Usuario sea válido para registro de estudiante.
        /// 
        /// VALIDACIONES:
        /// 1. ✓ NombreUsuario: 4-20 caracteres
        /// 2. ✓ Contraseña: 8-50 caracteres
        /// 3. ✓ No estar vacíos
        /// 
        /// NOTA: No validar duplicados aquí (eso lo hace UsuarioService)
        /// 
        /// LANZAR EXCEPCIONES si algo falla
        /// </summary>
        private void ValidarUsuarioEstudiante(Usuario usuario)
        {
            if (usuario == null)
                throw new ArgumentNullException(nameof(usuario));

            // Validar username
            if (string.IsNullOrWhiteSpace(usuario.NombreUsuario))
                throw new ArgumentException("El nombre de usuario no puede estar vacío");

            string usernameLimpio = usuario.NombreUsuario.Trim();
            if (usernameLimpio.Length < 4 || usernameLimpio.Length > 20)
                throw new ArgumentException("El nombre de usuario debe tener entre 4 y 20 caracteres");

            // Validar contraseña
            if (string.IsNullOrWhiteSpace(usuario.Contraseña))
                throw new ArgumentException("La contraseña no puede estar vacía");

            if (usuario.Contraseña.Length < 8 || usuario.Contraseña.Length > 50)
                throw new ArgumentException("La contraseña debe tener entre 8 y 50 caracteres");
        }

        /// <summary>
        /// Valida que un Estudiante tenga datos académicos válidos.
        /// 
        /// VALIDACIONES:
        /// 1. ✓ Edad: 17-120 años
        /// 2. ✓ DNI: 6-15 caracteres, no vacío
        /// 3. ✓ Nombres/Apellidos: 3-50 caracteres
        /// 4. ✓ Matrícula: 5-30 caracteres, no vacío
        /// 5. ✓ Semestre: 1-10
        /// 6. ✓ Carrera: ID válido (> 0)
        /// 
        /// LANZAR EXCEPCIONES si algo falla
        /// </summary>
        private void ValidarEstudiante(Estudiante estudiante)
        {
            if (estudiante == null)
                throw new ArgumentNullException(nameof(estudiante));

            // Validar Edad
            if (estudiante.Edad < EDAD_MINIMA || estudiante.Edad > EDAD_MAXIMA)
                throw new ArgumentException($"La edad debe estar entre {EDAD_MINIMA} y {EDAD_MAXIMA} años");

            // Validar DNI
            if (string.IsNullOrWhiteSpace(estudiante.DNI))
                throw new ArgumentException("El DNI no puede estar vacío");

            string dniLimpio = estudiante.DNI.Trim();
            if (dniLimpio.Length < 6 || dniLimpio.Length > 15)
                throw new ArgumentException("El DNI debe tener entre 6 y 15 caracteres");

            // Validar Nombres
            if (string.IsNullOrWhiteSpace(estudiante.Nombres))
                throw new ArgumentException("El nombre no puede estar vacío");

            string nombresLimpio = estudiante.Nombres.Trim();
            if (nombresLimpio.Length < 3 || nombresLimpio.Length > 50)
                throw new ArgumentException("El nombre debe tener entre 3 y 50 caracteres");

            // Validar Apellidos
            if (string.IsNullOrWhiteSpace(estudiante.Apellidos))
                throw new ArgumentException("El apellido no puede estar vacío");

            string apellidosLimpio = estudiante.Apellidos.Trim();
            if (apellidosLimpio.Length < 3 || apellidosLimpio.Length > 50)
                throw new ArgumentException("El apellido debe tener entre 3 y 50 caracteres");

            // Validar Matrícula
            if (string.IsNullOrWhiteSpace(estudiante.Matricula))
                throw new ArgumentException("La matrícula no puede estar vacía");

            string matriculaLimpia = estudiante.Matricula.Trim();
            if (matriculaLimpia.Length < LONGITUD_MINIMA_MATRICULA || matriculaLimpia.Length > LONGITUD_MAXIMA_MATRICULA)
                throw new ArgumentException($"La matrícula debe tener entre {LONGITUD_MINIMA_MATRICULA} y {LONGITUD_MAXIMA_MATRICULA} caracteres");

            // Validar Semestre
            if (estudiante.Semestre < SEMESTRE_MINIMO || estudiante.Semestre > SEMESTRE_MAXIMO)
                throw new ArgumentException($"El semestre debe estar entre {SEMESTRE_MINIMO} y {SEMESTRE_MAXIMO}");

            // Validar Carrera
            if (estudiante.IdCarrera <= 0)
                throw new ArgumentException("ID de carrera inválido");
        }

        // ==================== MÉTODOS AUXILIARES ====================

        /// <summary>
        /// Verifica si el estudiante ya votó en una categoría específica.
        /// 
        /// RETORNA:
        /// - true: Si el estudiante ya tiene un voto registrado en esa categoría
        /// - false: Si no ha votado
        /// 
        /// Uso: Validar antes de permitir votación
        /// </summary>
        public bool YaVoto(Estudiante estudiante, string tipoVoto)
        {
            if (estudiante == null)
                return false;

            if (string.IsNullOrWhiteSpace(tipoVoto))
                return false;

            // Validar tipo de voto válido
            if (tipoVoto != "Reina" && tipoVoto != "Fotogenia")
                throw new ArgumentException("Tipo de voto inválido. Debe ser 'Reina' o 'Fotogenia'");

            try
            {
                if (tipoVoto == "Reina")
                    return estudiante.HavotadoReina;
                else
                    return estudiante.HavotadoFotogenia;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene estadísticas de estudiantes del sistema.
        /// 
        /// RETORNA objeto anónimo con:
        /// - TotalEstudiantes
        /// - EstudiantesPorCarrera
        /// - EstudiantesActivosPorSemestre
        /// - EdadPromedio
        /// 
        /// Uso: Dashboards, reportes
        /// </summary>
        public dynamic ObtenerEstadisticas()
        {
            try
            {
                var estudiantes = ObtenerTodos();

                if (estudiantes.Count == 0)
                    return new { TotalEstudiantes = 0 };

                return new
                {
                    TotalEstudiantes = estudiantes.Count,
                    EdadPromedio = estudiantes.Average(e => e.Edad),
                    EdadMinima = estudiantes.Min(e => e.Edad),
                    EdadMaxima = estudiantes.Max(e => e.Edad),
                    SemestrePromedio = estudiantes.Average(e => e.Semestre),
                    FechaActualizacion = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener estadísticas: {ex.Message}");
                return null;
            }
        }
    }
}
