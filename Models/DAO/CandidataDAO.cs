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
    /// DAO: Acceso a Datos para Candidatas
    /// 
    /// Responsabilidades EXCLUSIVAS:
    /// - Construir y ejecutar queries SQL contra tabla 'candidatas'
    /// - Mapear resultados de BD a objetos C# (Candidata)
    /// - Usar SIEMPRE parámetros @name para prevenir SQL Injection
    /// - Usar Singleton ConexionDB.GetInstance() para obtener conexión
    /// 
    /// IMPORTANTE: Este DAO NO contiene lógica de negocio.
    /// Toda validación debe estar en CandidataService
    /// </summary>
    public class CandidataDAO
    {
        public CandidataDAO() { }

        /// <summary>
        /// Obtiene TODAS las candidatas ACTIVAS con datos relacionados.
        /// 
        /// QUERY CRÍTICA: Usa 4 INNER JOINs
        /// - personas: para nombres, apellidos, edad
        /// - estudiantes: para validar relación
        /// - carreras: para nombre de carrera
        /// - facultades: para nombre de facultad
        /// 
        /// FILTRO: WHERE can.activa = 1 (Soft Delete - solo activas)
        /// ORDEN: por nombres ascendente (para UI)
        /// 
        /// Flujo de Mapeo:
        /// 1. Lee cada fila del resultado
        /// 2. Crea objeto Candidata con TODOS los datos
        /// 3. Anida objeto Carrera con Facultad dentro
        /// 4. Agrega a lista (nunca nulo - retorna lista vacía si falla)
        /// </summary>
        public List<Candidata> ObtenerActivas()
        {
            List<Candidata> lista = new List<Candidata>();

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    string query = @"
                SELECT 
                    can.id_candidata, 
                    p.nombres, 
                    p.apellidos,
                    p.dni,
                    p.edad,
                    c.id_carrera,
                    c.nombre AS nombre_carrera, 
                    f.id_facultad,
                    f.nombre AS nombre_facultad,
                    can.tipo_candidatura,
                    can.url_foto, 
                    can.activa
                FROM candidatas can
                INNER JOIN personas p ON can.id_candidata = p.id_persona
                INNER JOIN estudiantes e ON can.id_candidata = e.id_estudiante
                INNER JOIN carreras c ON e.id_carrera = c.id_carrera
                INNER JOIN facultades f ON c.id_facultad = f.id_facultad
                WHERE can.activa = 1
                ORDER BY p.nombres ASC";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Mapeo de datos: BD → Objeto C#
                                // Nota: Se mapea CandidataId dos veces (id_candidata) para compatibilidad
                                string nombreCompleto = reader.GetString("nombres") + " " + reader.GetString("apellidos");

                                Candidata candidata = new Candidata
                                {
                                    CandidataId = reader.GetInt32("id_candidata"),
                                    Id = reader.GetInt32("id_candidata"),
                                    Nombres = reader.GetString("nombres"),
                                    Apellidos = reader.GetString("apellidos"),
                                    DNI = reader.GetString("dni"),
                                    Edad = reader.GetByte("edad"),
                                    // Manejo de NULL para url_foto (imagen opcional)
                                    ImagenPrincipal = reader.IsDBNull(reader.GetOrdinal("url_foto")) ? "" : reader.GetString("url_foto"),
                                    Activa = reader.GetBoolean("activa"),
                                    // Conversión: INT en BD → ENUM en C#
                                    tipoCandidatura = (TipoVoto)reader.GetInt32("tipo_candidatura"),
                                    IdCarrera = reader.GetInt32("id_carrera"),
                                    // Objeto anidado: Carrera con Facultad dentro
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

                                lista.Add(candidata);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log a consola pero retorna lista vacía (nunca nulo)
                    Console.WriteLine("Error al obtener candidatas activas: " + ex.Message);
                }
            }
            return lista;
        }

        /// <summary>
        /// Verifica si un estudiante YA está registrado como candidata ACTIVA.
        /// 
        /// VALIDACIÓN CRÍTICA para regla de negocio:
        /// Solo UNA candidata activa por estudiante
        /// 
        /// Query: Cuenta registros donde id_candidata = estudiante ID Y activa = 1
        /// Retorna: true si existe, false si no
        /// </summary>
        public bool ExisteCandidataActivaPorEstudiante(int estudianteId)
        {
            string query = "SELECT COUNT(*) FROM candidatas WHERE id_candidata = @EstudianteId AND activa = 1";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        // Parámetro @EstudianteId previene SQL Injection
                        cmd.Parameters.AddWithValue("@EstudianteId", estudianteId);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al verificar candidata activa: " + ex.Message);
                    return false;
                }
            }
        }

        /// <summary>
        /// OPERACIÓN CRÍTICA: Inserta nueva candidata en BD.
        /// 
        /// Datos insertados:
        /// - id_candidata: ID del estudiante (PK, también FK)
        /// - url_foto: Ruta a imagen (OPCIONAL - NULL allowed)
        /// - activa: Siempre 1 al registrar
        /// - tipo_candidatura: INT (1=Reina, 2=Fotogenia)
        /// 
        /// NOTA: El ID NO se genera aquí. Usa el ID del estudiante.
        /// Los datos de la persona deben existir previamente.
        /// </summary>
        public bool InsertarCandidato(int estudianteId, Candidata candidata)
        {
            string query = @"INSERT INTO candidatas (id_candidata, url_foto, activa, tipo_candidatura) 
                             VALUES (@Id, @UrlFoto, 1, @TipoCandidatura)";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", estudianteId);
                        // Manejo de NULL: si no hay imagen, envía DBNull
                        cmd.Parameters.AddWithValue("@UrlFoto",
                            string.IsNullOrEmpty(candidata.ImagenPrincipal) ? (object)DBNull.Value : candidata.ImagenPrincipal);
                        // Conversión: ENUM → INT para BD
                        cmd.Parameters.AddWithValue("@TipoCandidatura", 
                            (int)(candidata.tipoCandidatura == TipoVoto.Reina ? TipoVoto.Reina : TipoVoto.Fotogenia));

                        // ExecuteNonQuery retorna número de filas afectadas
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0; // true si al menos una fila se insertó
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al registrar candidata: " + ex.Message);
                    return false;
                }
            }
        }

        /// <summary>
        /// Obtiene UNA candidata por ID con TODOS sus datos relacionados.
        /// 
        /// Similar a ObtenerActivas() pero filtra por ID específico
        /// NO filtra por activa = 1 (obtiene incluso inactivas)
        /// 
        /// Uso: Editar, visualizar detalles de candidata específica
        /// Retorna: Candidata o null si no existe
        /// </summary>
        public Candidata ObtenerPorId(int candidataId)
        {
            string query = @"
                SELECT 
                    can.id_candidata, 
                    p.nombres, 
                    p.apellidos,
                    p.dni,
                    p.edad,
                    c.id_carrera,
                    c.nombre AS nombre_carrera, 
                    f.id_facultad,
                    f.nombre AS nombre_facultad,
                    can.tipo_candidatura,
                    can.url_foto, 
                    can.activa
                FROM candidatas can
                INNER JOIN personas p ON can.id_candidata = p.id_persona
                INNER JOIN estudiantes e ON can.id_candidata = e.id_estudiante
                INNER JOIN carreras c ON e.id_carrera = c.id_carrera
                INNER JOIN facultades f ON c.id_facultad = f.id_facultad
                WHERE can.id_candidata = @Id";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", candidataId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            // Si hay resultado, mapear y retornar
                            // Si no hay, retorna null
                            if (reader.Read())
                            {
                                return new Candidata
                                {
                                    CandidataId = reader.GetInt32("id_candidata"),
                                    Id = reader.GetInt32("id_candidata"),
                                    Nombres = reader.GetString("nombres"),
                                    Apellidos = reader.GetString("apellidos"),
                                    DNI = reader.GetString("dni"),
                                    Edad = reader.GetByte("edad"),
                                    ImagenPrincipal = reader.IsDBNull(reader.GetOrdinal("url_foto")) ? "" : reader.GetString("url_foto"),
                                    Activa = reader.GetBoolean("activa"),
                                    tipoCandidatura = (TipoVoto)reader.GetInt32("tipo_candidatura"),
                                    IdCarrera = reader.GetInt32("id_carrera"),
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
                            }
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al obtener candidata por ID: " + ex.Message);
                    return null;
                }
            }
        }

        /// <summary>
        /// PATRÓN CRÍTICO: Recupera candidata usando ID de ESTUDIANTE después de insertar.
        /// 
        /// Flujo de uso:
        /// 1. InsertarCandidato(estudianteId, ...) - genera candidata en BD
        /// 2. ObtenerPorIdUsuario(estudianteId) - recupera los datos incluyendo CandidataId
        /// 3. Usar CandidataId para guardar detalles en CANDIDATA_DETALLES
        /// 
        /// Retorna: Candidata con datos básicos (sin Carrera/Facultad por optimizar)
        /// O null si no existe
        /// </summary>
        public Candidata ObtenerPorIdUsuario(int idUsuario)
        {
            string query = @"
        SELECT 
            can.id_candidata, 
            p.nombres, 
            p.apellidos,
            p.dni,
            p.edad,
            can.activa
        FROM personas p
        INNER JOIN candidatas can ON can.id_candidata = p.id_persona
        WHERE p.id_usuario = @idUsuario";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@idUsuario", idUsuario);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Candidata
                                {
                                    CandidataId = reader.GetInt32("id_candidata"),
                                    Id = reader.GetInt32("id_candidata"),
                                    Nombres = reader.GetString("nombres"),
                                    Apellidos = reader.GetString("apellidos"),
                                    DNI = reader.GetString("dni"),
                                    Edad = reader.GetByte("edad"),
                                    Activa = reader.GetBoolean("activa")
                                };
                            }
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al obtener candidata por ID usuario: " + ex.Message);
                    return null;
                }
            }
        }

        /// <summary>
        /// OPERACIÓN CRÍTICA: Actualiza datos de candidata EXISTENTE.
        /// 
        /// Actualiza DOS tablas en UNA misma conexión (sin transacción explícita):
        /// 1. personas: nombres, apellidos, edad
        /// 2. candidatas: url_foto, tipo_candidatura
        /// 
        /// VALIDACIÓN: El Service se encarga de validar cambios
        /// Este DAO solo persiste lo que recibe
        /// </summary>
        public bool ActualizarCandidata(Candidata candidata)
        {
            if (candidata == null)
                return false;

            // Query 1: Actualizar datos heredados de Persona
            string queryPersona = @"UPDATE personas 
                                    SET nombres = @Nombres, 
                                        apellidos = @Apellidos, 
                                        edad = @Edad 
                                    WHERE id_persona = @Id";

            // Query 2: Actualizar datos específicos de Candidata
            string queryCandidata = @"UPDATE candidatas 
                                      SET url_foto = @UrlFoto, 
                                          tipo_candidatura = @TipoCandidatura 
                                      WHERE id_candidata = @Id";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();

                    // PASO 1: Actualizar tabla personas
                    using (var cmd = new MySqlCommand(queryPersona, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nombres", candidata.Nombres ?? "");
                        cmd.Parameters.AddWithValue("@Apellidos", candidata.Apellidos ?? "");
                        cmd.Parameters.AddWithValue("@Edad", candidata.Edad);
                        cmd.Parameters.AddWithValue("@Id", candidata.Id);
                        cmd.ExecuteNonQuery(); // Ejecuta sin verificar filas afectadas
                    }

                    // PASO 2: Actualizar tabla candidatas
                    using (var cmd = new MySqlCommand(queryCandidata, conn))
                    {
                        cmd.Parameters.AddWithValue("@UrlFoto", 
                            string.IsNullOrEmpty(candidata.ImagenPrincipal) ? (object)DBNull.Value : candidata.ImagenPrincipal);
                        cmd.Parameters.AddWithValue("@TipoCandidatura", (int)candidata.tipoCandidatura);
                        cmd.Parameters.AddWithValue("@Id", candidata.CandidataId);
                        cmd.ExecuteNonQuery();
                    }

                    return true; // Si ambas queries completaron sin error, retorna true
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al actualizar candidata: " + ex.Message);
                    return false;
                }
            }
        }

        /// <summary>
        /// PATRÓN SOFT DELETE: Cambia estado activa = 0 (NO elimina físicamente).
        /// 
        /// Ventajas:
        /// ✓ Preserva historial y auditoría
        /// ✓ No afecta foreign keys
        /// ✓ Permite recuperar candidatas si es necesario
        /// 
        /// Flujo:
        /// - BtnEliminar → EliminarCandidata() → ActualizarEstadoCandidata(false)
        /// - Después, ObtenerActivas() no retorna esta candidata (WHERE activa = 1)
        /// </summary>
        public bool ActualizarEstadoCandidato(int candidataId, bool activa)
        {
            string query = @"UPDATE candidatas SET activa = @Activa WHERE id_candidata = @CandidataId";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CandidataId", candidataId);
                        // Conversión: bool → INT (1 o 0)
                        cmd.Parameters.AddWithValue("@Activa", activa);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al actualizar estado de candidata: " + ex.Message);
                    return false;
                }
            }
        }
    }
}

