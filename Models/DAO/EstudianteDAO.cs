using MySql.Data.MySqlClient;
using SIVUG.Models.DTOS;
using SIVUG.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.DAO
{
    /// <summary>
    /// DAO: Acceso a Datos para Estudiantes
    /// 
    /// Responsabilidades EXCLUSIVAS:
    /// - Operaciones CRUD en tablas: usuarios, personas, estudiantes
    /// - Mapeo de resultados SQL a objetos C# (Estudiante con Usuario y Persona)
    /// - Usar parámetros @name para prevenir SQL Injection
    /// - Usar Singleton ConexionDB para obtener conexión
    /// 
    /// IMPORTANTE: No contiene validaciones de negocio.
    /// Toda validación está en EstudianteService.
    /// 
    /// NUEVA ARQUITECTURA:
    /// - Estudiante hereda de Persona
    /// - Persona pertenece a Usuario (FK id_usuario en tabla personas)
    /// - Estudiante es especialización de Persona
    /// 
    /// FLUJO DE REGISTRO:
    /// 1. Crear Usuario (credenciales)
    /// 2. Crear Persona (datos personales + FK id_usuario)
    /// 3. Crear Estudiante (datos académicos + FK id_persona)
    /// </summary>
    public class EstudianteDAO
    {
        private readonly UsuarioDAO _usuarioDAO;

        public EstudianteDAO()
        {
            _usuarioDAO = new UsuarioDAO();
        }

        /// <summary>
        /// OPERACIÓN CRÍTICA: Registra nuevo estudiante en BD.
        /// 
        /// FLUJO TRANSACCIONAL (3 pasos):
        /// 1. INSERT en tabla 'usuarios' (credenciales)
        ///    - username, password_hash, id_rol, activo, fecha_registro
        ///    - Se obtiene ID generado automáticamente
        /// 
        /// 2. INSERT en tabla 'personas' (datos personales)
        ///    - dni, nombres, apellidos, edad, id_usuario (FK)
        ///    - Se obtiene ID generado automáticamente
        /// 
        /// 3. INSERT en tabla 'estudiantes' (datos académicos)
        ///    - id_estudiante (PK = id_persona), matricula, semestre, id_carrera, ruta_foto_perfil
        ///    - Usa el ID de persona como PK
        /// 
        /// Si algún INSERT falla:
        /// - Se ejecuta ROLLBACK automático
        /// - Toda la transacción se revierte
        /// - Excepción se propaga al Service
        /// 
        /// NOTA ARQUITECTÓNICA:
        /// - NO se guardan ha_votado_reina ni ha_votado_fotogenia en tabla estudiantes
        /// - Los votos se rastrean en tabla 'votos' (separación de concerns)
        /// - La contraseña viene HASHEADA del Service
        /// </summary>
        public int RegistrarEstudianteConUsuario(Usuario usuario, Estudiante estudiante)
        {
            if (usuario == null || estudiante == null)
                throw new ArgumentNullException("Usuario y Estudiante no pueden ser nulos");

            using (var conexion = ConexionDB.GetInstance().GetConnection())
            {
                conexion.Open();
                using (var transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        // ========== PASO 1: INSERTAR USUARIO ==========
                        string sqlUsuario = @"INSERT INTO usuarios (username, password_hash, id_rol, activo, fecha_registro) 
                                            VALUES (@username, @password_hash, @id_rol, @activo, @fecha_registro);
                                            SELECT LAST_INSERT_ID();";

                        int idUsuarioGenerado;
                        using (var cmd = new MySqlCommand(sqlUsuario, conexion, transaccion))
                        {
                            cmd.Parameters.AddWithValue("@username", usuario.NombreUsuario.Trim());
                            cmd.Parameters.AddWithValue("@password_hash", usuario.Contraseña);
                            cmd.Parameters.AddWithValue("@id_rol", usuario.IdRol);
                            cmd.Parameters.AddWithValue("@activo", usuario.Activo ? 1 : 0);
                            cmd.Parameters.AddWithValue("@fecha_registro", usuario.FechaRegistro);

                            idUsuarioGenerado = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        if (idUsuarioGenerado <= 0)
                        {
                            transaccion.Rollback();
                            throw new InvalidOperationException("No se pudo generar ID de usuario");
                        }

                        // ========== PASO 2: INSERTAR PERSONA ==========
                        string sqlPersona = @"INSERT INTO personas (dni, nombres, apellidos, edad, id_usuario) 
                                            VALUES (@dni, @nombres, @apellidos, @edad, @id_usuario);
                                            SELECT LAST_INSERT_ID();";

                        int idPersonaGenerado;
                        using (var cmd = new MySqlCommand(sqlPersona, conexion, transaccion))
                        {
                            cmd.Parameters.AddWithValue("@dni", estudiante.DNI.Trim());
                            cmd.Parameters.AddWithValue("@nombres", estudiante.Nombres.Trim());
                            cmd.Parameters.AddWithValue("@apellidos", estudiante.Apellidos.Trim());
                            cmd.Parameters.AddWithValue("@edad", estudiante.Edad);
                            cmd.Parameters.AddWithValue("@id_usuario", idUsuarioGenerado);

                            idPersonaGenerado = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        if (idPersonaGenerado <= 0)
                        {
                            transaccion.Rollback();
                            throw new InvalidOperationException("No se pudo generar ID de persona");
                        }

                        // ========== PASO 3: INSERTAR ESTUDIANTE ==========
                        // La PK de estudiante es el ID de persona
                        string sqlEstudiante = @"INSERT INTO estudiantes 
                                               (id_estudiante, matricula, semestre, id_carrera, ruta_foto_perfil) 
                                               VALUES 
                                               (@id_estudiante, @matricula, @semestre, @id_carrera, @ruta_foto_perfil)";

                        using (var cmd = new MySqlCommand(sqlEstudiante, conexion, transaccion))
                        {
                            cmd.Parameters.AddWithValue("@id_estudiante", idPersonaGenerado);
                            cmd.Parameters.AddWithValue("@matricula", estudiante.Matricula.Trim());
                            cmd.Parameters.AddWithValue("@semestre", estudiante.Semestre);
                            cmd.Parameters.AddWithValue("@id_carrera", estudiante.IdCarrera);
                            // Manejo de NULL: ruta_foto_perfil es OPCIONAL
                            cmd.Parameters.AddWithValue("@ruta_foto_perfil",
                                string.IsNullOrEmpty(estudiante.FotoPerfilRuta) ? (object)DBNull.Value : estudiante.FotoPerfilRuta);

                            int rowsAffected = cmd.ExecuteNonQuery();
                            if (rowsAffected <= 0)
                            {
                                transaccion.Rollback();
                                throw new InvalidOperationException("No se pudo insertar registro de estudiante");
                            }
                        }

                        // ========== CONFIRMACIÓN ==========
                        // Si los 3 INSERTs completaron sin error, confirma transacción
                        transaccion.Commit();
                        System.Diagnostics.Debug.WriteLine($"[ESTUDIANTE REGISTRADO] {estudiante.Nombres} {estudiante.Apellidos} (Usuario: {usuario.NombreUsuario})");
                        return idPersonaGenerado;
                    }
                    catch (MySqlException ex) when (ex.Number == 1062) // Duplicate entry
                    {
                        transaccion.Rollback();
                        System.Diagnostics.Debug.WriteLine($"Duplicado: Usuario o DNI ya existe - {ex.Message}");
                        throw new InvalidOperationException($"El usuario o DNI ya existe en el sistema", ex);
                    }
                    catch (Exception ex)
                    {
                        // Si algo falló, revierte todos los INSERTs
                        transaccion.Rollback();
                        System.Diagnostics.Debug.WriteLine($"Error al registrar estudiante: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Busca un estudiante por CÉDULA (DNI) CON USUARIO y DATOS COMPLETOS.
        /// 
        /// QUERY CRÍTICA: Usa 6 JOINs para traer datos de:
        /// - usuarios: username, id_rol, activo (credenciales)
        /// - personas: dni, nombres, apellidos, edad (datos personales)
        /// - estudiantes: matricula, semestre, id_carrera, foto (datos académicos)
        /// - carreras: nombre de carrera
        /// - facultades: nombre de facultad
        /// - votos (subqueries): para verificar si ya votó
        /// 
        /// VALIDACIÓN DE VOTACIÓN:
        /// Subqueries cuentan votos por tipo:
        /// - voto_reina: COUNT(*) WHERE tipo_voto = 'Reina'
        /// - voto_fotogenia: COUNT(*) WHERE tipo_voto = 'Fotogenia'
        /// 
        /// En C#: Si conteo > 0, asigna flag como TRUE
        /// 
        /// Retorna: Estudiante con Usuario, Persona, Carrera y Facultad, o NULL si no existe
        /// </summary>
        public Estudiante ObtenerPorCedula(string cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula))
                return null;

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            -- USUARIO
                            u.id_usuario,
                            u.username,
                            u.id_rol,
                            u.activo,
                            -- PERSONA
                            p.id_persona,
                            p.dni,
                            p.nombres,
                            p.apellidos,
                            p.edad,
                            -- ESTUDIANTE
                            e.id_estudiante,
                            e.matricula,
                            e.semestre,
                            e.id_carrera,
                            e.ruta_foto_perfil,
                            -- CARRERA
                            c.id_carrera,
                            c.nombre AS nombre_carrera,
                            c.id_facultad,
                            -- FACULTAD
                            f.id_facultad,
                            f.nombre AS nombre_facultad,
                            -- VOTOS (subqueries)
                            (SELECT COUNT(*) FROM votos v WHERE v.id_estudiante = e.id_estudiante AND v.tipo_voto = 'Reina') as voto_reina,
                            (SELECT COUNT(*) FROM votos v WHERE v.id_estudiante = e.id_estudiante AND v.tipo_voto = 'Fotogenia') as voto_fotogenia
                        FROM usuarios u
                        INNER JOIN personas p ON u.id_usuario = p.id_usuario
                        INNER JOIN estudiantes e ON p.id_persona = e.id_estudiante
                        INNER JOIN carreras c ON e.id_carrera = c.id_carrera
                        INNER JOIN facultades f ON c.id_facultad = f.id_facultad
                        WHERE p.dni = @Cedula";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Cedula", cedula.Trim());

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Mapeo completo: Usuario + Persona + Estudiante
                                var estudiante = new Estudiante
                                {
                                    // PROPIEDADES DE PERSONA (heredadas)
                                    Id = reader.GetInt32("id_persona"),
                                    DNI = reader.GetString("dni"),
                                    Nombres = reader.GetString("nombres"),
                                    Apellidos = reader.GetString("apellidos"),
                                    Edad = reader.GetByte("edad"),
                                    IdUsuario = reader.GetInt32("id_usuario"),

                                    // PROPIEDADES DE ESTUDIANTE
                                    Matricula = reader.GetString("matricula"),
                                    Semestre = reader.GetByte("semestre"),
                                    IdCarrera = reader.GetInt32("id_carrera"),
                                    FotoPerfilRuta = reader.IsDBNull(reader.GetOrdinal("ruta_foto_perfil"))
                                        ? null
                                        : reader.GetString("ruta_foto_perfil"),

                                    // PATRÓN CRÍTICO: Conversión de conteos a flags booleanos
                                    HavotadoReina = reader.GetInt32("voto_reina") > 0,
                                    HavotadoFotogenia = reader.GetInt32("voto_fotogenia") > 0,

                                    // RELACIÓN: Carrera con Facultad anidada
                                    Carrera = new Carrera
                                    {
                                        Id = reader.GetInt32("id_carrera"),
                                        Nombre = reader.GetString("nombre_carrera"),
                                        IdFacultad = reader.GetInt32("id_facultad"),
                                        Facultad = new Facultad
                                        {
                                            Id = reader.GetInt32("id_facultad"),
                                            Nombre = reader.GetString("nombre_facultad")
                                        }
                                    }
                                };

                                // RELACIÓN: Usuario con Rol anidado
                                estudiante.Usuario = new Usuario
                                {
                                    IdUsuario = reader.GetInt32("id_usuario"),
                                    NombreUsuario = reader.GetString("username"),
                                    IdRol = reader.GetInt32("id_rol"),
                                    Activo = reader.GetInt32("activo") == 1
                                };

                                return estudiante;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al buscar estudiante por cédula: {ex.Message}");
                }
            }
            return null;
        }

        /// <summary>
        /// Obtiene TODOS los estudiantes con DATOS COMPLETOS.
        /// 
        /// Query: 6 INNER JOINs para traer:
        /// - usuario: credenciales y estado
        /// - persona: datos personales
        /// - estudiante: datos académicos
        /// - carrera: información de carrera
        /// - facultad: información de facultad
        /// 
        /// Uso: Listado completo en grillas, dropdowns, reportes
        /// Retorna: List<Estudiante> (nunca nulo, lista vacía si no hay)
        /// </summary>
        public List<Estudiante> ObtenerTodosDetallado()
        {
            var lista = new List<Estudiante>();

            using (var conexion = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conexion.Open();
                    string sql = @"
                        SELECT 
                            -- USUARIO
                            u.id_usuario,
                            u.username,
                            u.id_rol,
                            u.activo,
                            -- PERSONA
                            p.id_persona,
                            p.dni,
                            p.nombres,
                            p.apellidos,
                            p.edad,
                            -- ESTUDIANTE
                            e.id_estudiante,
                            e.matricula,
                            e.semestre,
                            e.id_carrera,
                            e.ruta_foto_perfil,
                            -- CARRERA
                            c.id_carrera,
                            c.nombre AS nombre_carrera,
                            c.id_facultad,
                            -- FACULTAD
                            f.id_facultad,
                            f.nombre AS nombre_facultad
                        FROM usuarios u
                        INNER JOIN personas p ON u.id_usuario = p.id_usuario
                        INNER JOIN estudiantes e ON p.id_persona = e.id_estudiante
                        INNER JOIN carreras c ON e.id_carrera = c.id_carrera
                        INNER JOIN facultades f ON c.id_facultad = f.id_facultad
                        ORDER BY p.nombres ASC";

                    using (var cmd = new MySqlCommand(sql, conexion))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Reconstruir objeto completo con todas las relaciones
                            var estudiante = new Estudiante
                            {
                                // PROPIEDADES DE PERSONA
                                Id = reader.GetInt32("id_persona"),
                                DNI = reader.GetString("dni"),
                                Nombres = reader.GetString("nombres"),
                                Apellidos = reader.GetString("apellidos"),
                                Edad = reader.GetByte("edad"),
                                IdUsuario = reader.GetInt32("id_usuario"),

                                // PROPIEDADES DE ESTUDIANTE
                                Matricula = reader.GetString("matricula"),
                                Semestre = reader.GetByte("semestre"),
                                IdCarrera = reader.GetInt32("id_carrera"),
                                FotoPerfilRuta = reader.IsDBNull(reader.GetOrdinal("ruta_foto_perfil"))
                                    ? null
                                    : reader.GetString("ruta_foto_perfil"),

                                // RELACIÓN: Carrera con Facultad anidada
                                Carrera = new Carrera
                                {
                                    Id = reader.GetInt32("id_carrera"),
                                    Nombre = reader.GetString("nombre_carrera"),
                                    IdFacultad = reader.GetInt32("id_facultad"),
                                    Facultad = new Facultad
                                    {
                                        Id = reader.GetInt32("id_facultad"),
                                        Nombre = reader.GetString("nombre_facultad")
                                    }
                                },

                                // RELACIÓN: Usuario con Rol anidada
                                Usuario = new Usuario
                                {
                                    IdUsuario = reader.GetInt32("id_usuario"),
                                    NombreUsuario = reader.GetString("username"),
                                    IdRol = reader.GetInt32("id_rol"),
                                    Activo = reader.GetInt32("activo") == 1
                                }
                            };

                            lista.Add(estudiante);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al obtener todos los estudiantes: {ex.Message}");
                }
            }

            return lista;
        }

       
       

        /// <summary>
        /// Obtiene un estudiante específico por ID (id_persona).
        /// 
        /// Uso: Buscar estudiante por su PK
        /// Retorna: Estudiante con Usuario, Persona, Carrera y Facultad, o NULL si no existe
        /// </summary>
        public Estudiante ObtenerPorId(int idEstudiante)
        {
            if (idEstudiante <= 0)
                return null;

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            u.id_usuario,
                            u.username,
                            u.id_rol,
                            u.activo,
                            p.id_persona,
                            p.dni,
                            p.nombres,
                            p.apellidos,
                            p.edad,
                            e.id_estudiante,
                            e.matricula,
                            e.semestre,
                            e.id_carrera,
                            e.ruta_foto_perfil,
                            c.id_carrera,
                            c.nombre AS nombre_carrera,
                            c.id_facultad,
                            f.id_facultad,
                            f.nombre AS nombre_facultad
                        FROM usuarios u
                        INNER JOIN personas p ON u.id_usuario = p.id_usuario
                        INNER JOIN estudiantes e ON p.id_persona = e.id_estudiante
                        INNER JOIN carreras c ON e.id_carrera = c.id_carrera
                        INNER JOIN facultades f ON c.id_facultad = f.id_facultad
                        WHERE p.id_persona = @IdEstudiante";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdEstudiante", idEstudiante);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var estudiante = new Estudiante
                                {
                                    Id = reader.GetInt32("id_persona"),
                                    DNI = reader.GetString("dni"),
                                    Nombres = reader.GetString("nombres"),
                                    Apellidos = reader.GetString("apellidos"),
                                    Edad = reader.GetByte("edad"),
                                    IdUsuario = reader.GetInt32("id_usuario"),
                                    Matricula = reader.GetString("matricula"),
                                    Semestre = reader.GetByte("semestre"),
                                    IdCarrera = reader.GetInt32("id_carrera"),
                                    FotoPerfilRuta = reader.IsDBNull(reader.GetOrdinal("ruta_foto_perfil"))
                                        ? null
                                        : reader.GetString("ruta_foto_perfil"),

                                    Carrera = new Carrera
                                    {
                                        Id = reader.GetInt32("id_carrera"),
                                        Nombre = reader.GetString("nombre_carrera"),
                                        IdFacultad = reader.GetInt32("id_facultad"),
                                        Facultad = new Facultad
                                        {
                                            Id = reader.GetInt32("id_facultad"),
                                            Nombre = reader.GetString("nombre_facultad")
                                        }
                                    },

                                    Usuario = new Usuario
                                    {
                                        IdUsuario = reader.GetInt32("id_usuario"),
                                        NombreUsuario = reader.GetString("username"),
                                        IdRol = reader.GetInt32("id_rol"),
                                        Activo = reader.GetInt32("activo") == 1
                                    }
                                };

                                return estudiante;
                            }
                        }
                    }
                }

                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al obtener estudiante por ID: {ex.Message}");
                }
            }

            return null;
        }
    }
}
