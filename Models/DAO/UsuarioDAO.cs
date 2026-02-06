using MySql.Data.MySqlClient;
using SIVUG.Models.DTOS;
using SIVUG.Models.DTOS.SIVUG.Models.DTOS;
using SIVUG.Util;
using System;
using System.Collections.Generic;

namespace SIVUG.Models.DAO
{
    /// <summary>
    /// DAO: Acceso a Datos para Usuarios
    /// 
    /// Responsabilidades EXCLUSIVAS:
    /// - Operaciones CRUD en tabla 'usuarios'
    /// - Operaciones CRUD en tabla 'personas' (relación directa)
    /// - Mapeo de resultados SQL a objetos C# (Usuario + Persona)
    /// - Usar SIEMPRE parámetros @name para prevenir SQL Injection
    /// - Usar Singleton ConexionDB.GetInstance() para obtener conexión
    /// 
    /// IMPORTANTE: Este DAO NO contiene lógica de negocio.
    /// Toda validación debe estar en UsuarioService
    /// 
    /// NOTA DE SEGURIDAD:
    /// - Las contraseñas se almacenan hasheadas en BD
    /// - NUNCA se retornan contraseñas en consultas (excepto para validar)
    /// - El hasheo/validación ocurre en Service
    /// 
    /// RELACIÓN CORRECTA:
    /// - Usuario TIENE Persona (la Persona pertenece al Usuario)
    /// - FK id_usuario está en tabla personas (lado hijo)
    /// - Flujo: INSERT usuario ? INSERT persona con id_usuario
    /// </summary>
    public class UsuarioDAO
    {
        /// <summary>
        /// Obtiene TODOS los usuarios del sistema CON SUS ROLES.
        /// 
        /// QUERY: JOIN con tabla roles para obtener nombre del rol
        /// NOTA: NO carga Personas aquí por optimización
        /// ORDEN: Por nombre de usuario ascendente
        /// 
        /// Flujo de Mapeo:
        /// 1. Lee cada fila del resultado
        /// 2. Crea objeto Usuario con datos completos
        /// 3. Anida objeto Rol con ID y nombre
        /// 4. Agrega a lista
        /// 
        /// Retorna: Lista de usuarios (nunca nulo, vacío si falla)
        /// </summary>
        public List<Usuario> ObtenerTodos()
        {
            List<Usuario> lista = new List<Usuario>();

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            u.id_usuario, 
                            u.username,
                            u.activo,
                            u.fecha_registro,
                            u.requiere_cambio_contrasena,
                            u.id_rol,
                            r.id_rol,
                            r.nombre AS nombre_rol
                        FROM usuarios u
                        LEFT JOIN roles r ON u.id_rol = r.id_rol
                        ORDER BY u.nombre_usuario ASC";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Usuario usuario = new Usuario
                                {
                                    IdUsuario = reader.GetInt32("id_usuario"),
                                    NombreUsuario = reader.GetString("username"),
                                    IdRol = reader.GetInt32("id_rol"),
                                    Activo = reader.GetInt32("activo") == 1,
                                    FechaRegistro = reader.GetDateTime("fecha_registro"),
                                    RequiereCambioContraseña = reader.GetInt32("requiere_cambio_contrasena") == 1,
                                    Rol = new Rol
                                    {
                                        IdRol = reader.GetInt32("id_rol"),
                                        Nombre = reader.IsDBNull(reader.GetOrdinal("nombre_rol"))
                                            ? "Sin Rol"
                                            : reader.GetString("nombre_rol")
                                    }
                                    // Persona se carga bajo demanda con ObtenerPorIdConPersona()
                                };

                                lista.Add(usuario);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al obtener usuarios: {ex.Message}");
                }
            }

            return lista;
        }

        /// <summary>
        /// Obtiene UN usuario específico por ID CON SU ROL.
        /// 
        /// QUERY: SELECT * FROM usuarios JOIN roles WHERE id_usuario = @IdUsuario
        /// NOTA: NO carga Persona aquí por optimización
        /// 
        /// Retorna: Objeto Usuario o null si no existe
        /// </summary>
        public Usuario ObtenerPorId(int idUsuario)
        {
            string query = @"
                SELECT 
                    u.id_usuario, 
                    u.username, 
                    u.id_rol,
                    u.activo,
                    u.fecha_registro,
                    u.requiere_cambio_contrasena,
                    r.id_rol,
                    r.nombre AS nombre_rol
                FROM usuarios u
                LEFT JOIN roles r ON u.id_rol = r.id_rol
                WHERE u.id_usuario = @IdUsuario";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Usuario
                                {
                                    IdUsuario = reader.GetInt32("id_usuario"),
                                    NombreUsuario = reader.GetString("username"),
                                    IdRol = reader.GetInt32("id_rol"),
                                    Activo = reader.GetInt32("activo") == 1,
                                    FechaRegistro = reader.GetDateTime("fecha_registro"),
                                    RequiereCambioContraseña = reader.GetInt32("requiere_cambio_contrasena") == 1,
                                    Rol = new Rol
                                    {
                                        IdRol = reader.GetInt32("id_rol"),
                                        Nombre = reader.IsDBNull(reader.GetOrdinal("nombre_rol"))
                                            ? "Sin Rol"
                                            : reader.GetString("nombre_rol")
                                    }
                                };
                            }

                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al obtener usuario por ID: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// CRÍTICO: Obtiene usuario por nombre de usuario para AUTENTICACIÓN.
        /// 
        /// QUERY: SELECT * FROM usuarios WHERE nombre_usuario = @NombreUsuario
        /// 
        /// NOTA ESPECIAL: SÍ retorna contraseña (hasheada) para validar en Service
        /// Esta es la ÚNICA excepción donde se obtiene la contraseña
        /// 
        /// Retorna: Objeto Usuario completo o null si no existe
        /// </summary>
        public Usuario ObtenerPorNombreUsuario(string nombreUsuario)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario))
                return null;

            string query = @"
                SELECT 
                    u.id_usuario, 
                    u.username, 
                    u.password_hash,
                    u.id_rol,
                    u.activo,
                    u.fecha_registro,
                    u.requiere_cambio_contrasena,
                    r.id_rol,
                    r.nombre AS nombre_rol
                FROM usuarios u
                LEFT JOIN roles r ON u.id_rol = r.id_rol
                WHERE u.username = @NombreUsuario";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@NombreUsuario", nombreUsuario.Trim());
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Usuario
                                {
                                    IdUsuario = reader.GetInt32("id_usuario"),
                                    NombreUsuario = reader.GetString("username"),
                                    Contraseña = reader.GetString("password_hash"),
                                    IdRol = reader.GetInt32("id_rol"),
                                    Activo = reader.GetInt32("activo") == 1,
                                    FechaRegistro = reader.GetDateTime("fecha_registro"),
                                    RequiereCambioContraseña = reader.GetInt32("requiere_cambio_contrasena") == 1,
                                    Rol = new Rol
                                    {
                                        IdRol = reader.GetInt32("id_rol"),
                                        Nombre = reader.IsDBNull(reader.GetOrdinal("nombre_rol"))
                                            ? "Sin Rol"
                                            : reader.GetString("nombre_rol")
                                    }
                                };
                            }

                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al obtener usuario por nombre: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Obtiene Usuario CON su Persona asociada.
        /// 
        /// PATRÓN: Carga Usuario y luego Persona bajo demanda mediante JOIN
        /// Flujo:
        /// 1. JOIN usuarios con personas
        /// 2. Mapea ambas entidades en un solo query
        /// 3. Retorna Usuario completo con Persona anidada
        /// 
        /// Retorna: Usuario con su Persona (o sin ella si no existe)
        /// </summary>
        public Usuario ObtenerPorIdConPersona(int idUsuario)
        {
            string query = @"
                SELECT 
                    u.id_usuario, 
                    u.username,
                    u.password_hash, 
                    u.id_rol,
                    u.activo,
                    u.fecha_registro,
                    u.requiere_cambio_contrasena,
                    r.nombre AS nombre_rol,
                    p.id_persona,
                    p.dni,
                    p.nombres,
                    p.apellidos,
                    p.edad
                FROM usuarios u
                LEFT JOIN roles r ON u.id_rol = r.id_rol
                LEFT JOIN personas p ON u.id_usuario = p.id_usuario
                WHERE u.id_usuario = @IdUsuario";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Usuario usuario = new Usuario
                                {
                                    IdUsuario = reader.GetInt32("id_usuario"),
                                    NombreUsuario = reader.GetString("username"),
                                    IdRol = reader.GetInt32("id_rol"),
                                    Activo = reader.GetInt32("activo") == 1,
                                    FechaRegistro = reader.GetDateTime("fecha_registro"),
                                    RequiereCambioContraseña = reader.GetInt32("requiere_cambio_contrasena") == 1,
                                    Rol = new Rol
                                    {
                                        IdRol = reader.GetInt32("id_rol"),
                                        Nombre = reader.IsDBNull(reader.GetOrdinal("nombre_rol"))
                                            ? "Sin Rol"
                                            : reader.GetString("nombre_rol")
                                    }
                                };

                                // Cargar Persona si existe
                                if (!reader.IsDBNull(reader.GetOrdinal("id_persona")))
                                {
                                    usuario.Persona = new Persona
                                    {
                                        Id = reader.GetInt32("id_persona"),
                                        DNI = reader.GetString("dni"),
                                        Nombres = reader.GetString("nombres"),
                                        Apellidos = reader.GetString("apellidos"),
                                        Edad = (byte)reader.GetInt32("edad"),
                                        IdUsuario = reader.GetInt32("id_usuario")
                                    };
                                }

                                return usuario;
                            }

                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al obtener usuario con persona: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Verifica si un nombre de usuario YA EXISTE en BD.
        /// 
        /// VALIDACIÓN: Para evitar duplicados antes de insertar
        /// 
        /// Retorna: true si existe, false si no
        /// </summary>
        public bool ExistePorNombreUsuario(string nombreUsuario)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario))
                return false;

            string query = "SELECT COUNT(*) FROM usuarios WHERE username = @NombreUsuario";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@NombreUsuario", nombreUsuario.Trim());
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al verificar existencia de usuario: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// OPERACIÓN CRÍTICA: Inserta un nuevo usuario CON su persona en BD.
        /// 
        /// FLUJO TRANSACCIONAL:
        /// 1. BEGIN TRANSACTION
        /// 2. INSERT INTO usuarios (nombre_usuario, contraseña, id_rol)
        /// 3. Obtener LAST_INSERT_ID() ? id_usuario generado
        /// 4. Si hay Persona: INSERT INTO personas (dni, nombres, apellidos, edad, id_usuario)
        /// 5. COMMIT si todo OK, ROLLBACK si falla
        /// 
        /// IMPORTANTE:
        /// - La contraseña debe estar HASHEADA (service lo hace)
        /// - Usa TRANSACCIÓN para garantizar atomicidad
        /// - Si falla Persona, se revierte Usuario también
        /// 
        /// Retorna: ID del usuario insertado, o -1 si falla
        /// </summary>
        public int Insertar(Usuario usuario)
        {
            if (usuario == null || string.IsNullOrWhiteSpace(usuario.NombreUsuario)
                || string.IsNullOrWhiteSpace(usuario.Contraseña) || usuario.IdRol <= 0)
                return -1;

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // PASO 1: Insertar Usuario
                        string queryUsuario = @"INSERT INTO usuarios (username, password_hash, id_rol,activo,fecha_registro,requiere_cambio_contrasena) 
                                               VALUES (@NombreUsuario, @Contraseña, @IdRol, @Activo, @FechaRegistro, @RequiereCambioContraseña);
                                               SELECT LAST_INSERT_ID();";

                        int idUsuarioGenerado;
                        using (var cmd = new MySqlCommand(queryUsuario, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@NombreUsuario", usuario.NombreUsuario.Trim());
                            cmd.Parameters.AddWithValue("@Contraseña", usuario.Contraseña);
                            cmd.Parameters.AddWithValue("@IdRol", usuario.IdRol);
                            cmd.Parameters.AddWithValue("@Activo", usuario.Activo);
                            cmd.Parameters.AddWithValue("@FechaRegistro", usuario.FechaRegistro);
                            cmd.Parameters.AddWithValue("@RequiereCambioContraseña", usuario.RequiereCambioContraseña ? 1 : 0);

                            idUsuarioGenerado = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        if (idUsuarioGenerado <= 0)
                        {
                            transaction.Rollback();
                            return -1;
                        }

                        // PASO 2: Insertar Persona (si existe)
                        if (usuario.Persona != null)
                        {
                            string queryPersona = @"INSERT INTO personas (dni, nombres, apellidos, edad, id_usuario) 
                                                   VALUES (@Dni, @Nombres, @Apellidos, @Edad, @IdUsuario)";

                            using (var cmd = new MySqlCommand(queryPersona, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@Dni", usuario.Persona.DNI);
                                cmd.Parameters.AddWithValue("@Nombres", usuario.Persona.Nombres);
                                cmd.Parameters.AddWithValue("@Apellidos", usuario.Persona.Apellidos);
                                cmd.Parameters.AddWithValue("@Edad", usuario.Persona.Edad);
                                cmd.Parameters.AddWithValue("@IdUsuario", idUsuarioGenerado);

                                int rowsAffected = cmd.ExecuteNonQuery();
                                if (rowsAffected <= 0)
                                {
                                    System.Diagnostics.Debug.WriteLine("Advertencia: Persona no se insertó");
                                }
                            }
                        }

                        // COMMIT: Todo salió bien
                        transaction.Commit();
                        return idUsuarioGenerado;
                    }
                    catch (MySqlException ex) when (ex.Number == 1062) // Duplicate entry
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"Usuario o DNI ya existe: {ex.Message}");
                        return -1;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"Error al insertar usuario: {ex.Message}");
                        return -1;
                    }
                }
            }
        }

        /// <summary>
        /// OPERACIÓN CRÍTICA: Actualiza un usuario EXISTENTE con su Persona.
        /// 
        /// FLUJO TRANSACCIONAL:
        /// 1. BEGIN TRANSACTION
        /// 2. UPDATE usuarios SET nombre_usuario, id_rol WHERE id_usuario
        /// 3. UPDATE personas SET dni, nombres, apellidos, edad WHERE id_usuario
        /// 4. COMMIT si todo OK, ROLLBACK si falla
        /// 
        /// NOTA: NO actualiza contraseña (usar ActualizarContraseña)
        /// 
        /// Puede actualizar:
        /// ? Nombre de usuario (si no existe otro con ese nombre)
        /// ? Rol (id_rol)
        /// ? Datos de Persona (dni, nombres, apellidos, edad)
        /// ? Contraseña (usar método específico)
        /// 
        /// Retorna: true si se actualizó, false si algo falla
        /// </summary>
        public bool Actualizar(Usuario usuario)
        {
            if (usuario == null || usuario.IdUsuario <= 0 || string.IsNullOrWhiteSpace(usuario.NombreUsuario))
                return false;

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // PASO 1: Actualizar Usuario
                        string queryUsuario = @"UPDATE usuarios 
                                               SET username = @NombreUsuario, 
                                                   id_rol = @IdRol,
                                                    activo = @Activo   
                                               WHERE id_usuario = @IdUsuario";

                        using (var cmd = new MySqlCommand(queryUsuario, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@IdUsuario", usuario.IdUsuario);
                            cmd.Parameters.AddWithValue("@NombreUsuario", usuario.NombreUsuario.Trim());
                            cmd.Parameters.AddWithValue("@IdRol", usuario.IdRol);
                            cmd.Parameters.AddWithValue("@Activo", usuario.Activo ? 1 : 0);

                            int rowsAffected = cmd.ExecuteNonQuery();
                            if (rowsAffected <= 0)
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }

                        // PASO 2: Actualizar Persona (si existe)
                        if (usuario.Persona != null)
                        {
                            string queryPersona = @"UPDATE personas 
                                                   SET dni = @Dni, 
                                                       nombres = @Nombres, 
                                                       apellidos = @Apellidos, 
                                                       edad = @Edad 
                                                   WHERE id_usuario = @IdUsuario";

                            using (var cmd = new MySqlCommand(queryPersona, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@Dni", usuario.Persona.DNI);
                                cmd.Parameters.AddWithValue("@Nombres", usuario.Persona.Nombres);
                                cmd.Parameters.AddWithValue("@Apellidos", usuario.Persona.Apellidos);
                                cmd.Parameters.AddWithValue("@Edad", usuario.Persona.Edad);
                                cmd.Parameters.AddWithValue("@IdUsuario", usuario.IdUsuario);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        // COMMIT: Todo salió bien
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"Error al actualizar usuario: {ex.Message}");
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Actualiza SOLO la contraseña de un usuario.
        /// 
        /// UPDATE: UPDATE usuarios SET contraseña = @Contraseña WHERE id_usuario = @IdUsuario
        /// 
        /// IMPORTANTE: La contraseña debe estar HASHEADA (service lo hace)
        /// 
        /// Retorna: true si se actualizó, false si algo falla
        /// </summary>
        public bool ActualizarContraseña(int idUsuario, string contraseñaHasheada)
        {
            if (idUsuario <= 0 || string.IsNullOrWhiteSpace(contraseñaHasheada))
                return false;

            string query = @"UPDATE usuarios SET password_hash = @Contraseña,requiere_cambio_contrasena = 0 WHERE id_usuario = @IdUsuario";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                        cmd.Parameters.AddWithValue("@Contraseña", contraseñaHasheada);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[CONTRASEÑA ACTUALIZADA] Usuario ID: {idUsuario}, Flag desactivado "
                            );
                        }

                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al actualizar contraseña: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// OPERACIÓN CRÍTICA: Elimina un usuario de la BD con su Persona.
        /// 
        /// FLUJO TRANSACCIONAL:
        /// 1. BEGIN TRANSACTION
        /// 2. DELETE FROM personas WHERE id_usuario = @IdUsuario (primero hijo)
        /// 3. DELETE FROM usuarios WHERE id_usuario = @IdUsuario (luego padre)
        /// 4. COMMIT si todo OK, ROLLBACK si falla
        /// 
        /// ALTERNATIVA RECOMENDADA (SOFT DELETE):
        /// - Agregar columna "activo BOOLEAN DEFAULT 1" en usuarios
        /// - UPDATE usuarios SET activo = 0 WHERE id_usuario = @IdUsuario
        /// - Preserva integridad referencial y auditoría
        /// 
        /// Retorna: true si se eliminó, false si algo falla
        /// </summary>
        public bool Eliminar(int idUsuario)
        {
            if (idUsuario <= 0)
                return false;

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // PASO 1: Eliminar Persona primero (FK constraint)
                        string queryPersona = "DELETE FROM personas WHERE id_usuario = @IdUsuario";
                        using (var cmd = new MySqlCommand(queryPersona, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            cmd.ExecuteNonQuery();
                        }

                        // PASO 2: Eliminar Usuario
                        string queryUsuario = "DELETE FROM usuarios WHERE id_usuario = @IdUsuario";
                        using (var cmd = new MySqlCommand(queryUsuario, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected <= 0)
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }

                        // COMMIT: Todo salió bien
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"Error al eliminar usuario: {ex.Message}");
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Obtiene cantidad total de usuarios en el sistema.
        /// 
        /// Uso: Dashboards, estadísticas
        /// Retorna: Número de usuarios registrados
        /// </summary>
        public int ObtenerTotal()
        {
            string query = "SELECT COUNT(*) FROM usuarios";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al obtener total de usuarios: {ex.Message}");
                    return 0;
                }
            }
        }

        /// <summary>
        /// Obtiene usuarios filtrados por rol.
        /// 
        /// QUERY: SELECT * FROM usuarios WHERE id_rol = @IdRol
        /// 
        /// Uso: Listar todos los usuarios de un rol específico (ej: Admins)
        /// Retorna: Lista de usuarios (vacía si ninguno)
        /// </summary>
        public List<Usuario> ObtenerPorRol(int idRol)
        {
            if (idRol <= 0)
                return new List<Usuario>();

            List<Usuario> lista = new List<Usuario>();
            string query = @"
                SELECT 
                    u.id_usuario, 
                    u.username, 
                    u.id_rol,
                    u.activo,
                    u.fecha_registro,
                    u.requiere_cambio_contrasena,
                    r.id_rol,
                    r.nombre AS nombre_rol
                FROM usuarios u
                LEFT JOIN roles r ON u.id_rol = r.id_rol
                WHERE u.id_rol = @IdRol
                ORDER BY u.username ASC";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdRol", idRol);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Usuario usuario = new Usuario
                                {
                                    IdUsuario = reader.GetInt32("id_usuario"),
                                    NombreUsuario = reader.GetString("nombre_usuario"),
                                    IdRol = reader.GetInt32("id_rol"),
                                    Activo = reader.GetInt32("activo") == 1,
                                    FechaRegistro = reader.GetDateTime("fecha_registro"),
                                    RequiereCambioContraseña = reader.GetInt32("requiere_cambio_contrasena") == 1,
                                    Rol = new Rol
                                    {
                                        IdRol = reader.GetInt32("id_rol"),
                                        Nombre = reader.IsDBNull(reader.GetOrdinal("nombre_rol"))
                                            ? "Sin Rol"
                                            : reader.GetString("nombre_rol")
                                    }
                                };

                                lista.Add(usuario);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al obtener usuarios por rol: {ex.Message}");
                }
            }

            return lista;
        }
    }

}