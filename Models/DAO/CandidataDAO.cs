using MySql.Data.MySqlClient;
using SIVUG.Models.DTOS;
using SIVUG.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.DAO
{
    public class CandidataDAO
    {
        // El constructor ya no necesita instanciar la conexión porque usamos el Singleton dentro del método
        public CandidataDAO() { }

        public List<Candidata> ObtenerActivas()
        {
            List<Candidata> lista = new List<Candidata>();

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    // CORRECCIÓN: Agregué c.id_carrera y c.nombre al SELECT
                    string query = @"
                SELECT 
                    can.id_candidata, 
                    p.nombres, 
                    p.apellidos,
                    c.id_carrera,               -- FALTABA ESTO
                    c.nombre AS nombre_carrera, -- FALTABA ESTO
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
                                // Concatenamos para mostrar nombre completo
                                string nombreCompleto = reader.GetString("nombres") + " " + reader.GetString("apellidos");

                                // Creamos el objeto Candidata
                                Candidata candidata = new Candidata
                                {
                                    CandidataId = reader.GetInt32("id_candidata"),
                                    // Mapeamos propiedades heredadas de Persona/Estudiante si es necesario
                                    Id = reader.GetInt32("id_candidata"),
                                    Nombres = nombreCompleto,
                                    ImagenPrincipal = reader.IsDBNull(reader.GetOrdinal("url_foto")) ? "" : reader.GetString("url_foto"),
                                    Activa = reader.GetBoolean("activa"),

                                    // --- MAPEO DE LA NUEVA COLUMNA ---
                                    // Convertimos el entero de la BD (1 o 2) al Enum TipoVoto
                                    tipoCandidatura = (TipoVoto)reader.GetInt32("tipo_candidatura"),

                                    // Propiedad heredada de Estudiante
                                    IdCarrera = reader.GetInt32("id_carrera"),

                                    // Objeto anidado Carrera
                                    Carrera = new Carrera
                                    {
                                        Id = reader.GetInt32("id_carrera"),
                                        Nombre = reader.GetString("nombre_carrera"),
                                        IdFacultad = reader.GetInt32("id_facultad"),
                                        // Objeto anidado Facultad
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
                    Console.WriteLine("Error al obtener candidatas: " + ex.Message);
                }
            }
            return lista;
        }


        // Verifica si un estudiante ya está registrado como candidata activa
        public bool ExisteCandidataActivaPorEstudiante(int estudianteId)
        {
            // LÓGICA CORREGIDA:
            // Como Candidata HEREDA de Estudiante, comparten el mismo ID.
            // No hace falta unir por nombres (que es inseguro), solo buscamos el ID.
            string query = "SELECT COUNT(*) FROM candidatas WHERE id_candidata = @EstudianteId AND activa = 1";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@EstudianteId", estudianteId);

                        // Convertimos a int porque MySQL devuelve Int64 (long) en los Count
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al verificar candidata: " + ex.Message);
                    return false;
                }
            }
        }

        // Promueve a un estudiante a candidata
        public bool InsertarCandidato(int estudianteId, Candidata candidata)
        {
            // LÓGICA CORREGIDA:
            // 1. Insertamos el 'id_estudiante' como 'id_candidata' (Relación 1 a 1).
            // 2. No guardamos Nombre ni Facultad aquí (ya están en tablas Persona/Carrera).
            // 3. 'TipoCandidatura' se eliminó del diseño (lo define el Voto).
            string query = @"INSERT INTO candidatas (id_candidata, url_foto, activa) 
                             VALUES (@Id, @UrlFoto, 1)";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        // La clave primaria es el mismo ID del estudiante seleccionado
                        cmd.Parameters.AddWithValue("@Id", estudianteId);

                        cmd.Parameters.AddWithValue("@UrlFoto",
                            string.IsNullOrEmpty(candidata.ImagenPrincipal) ? (object)DBNull.Value : candidata.ImagenPrincipal);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al registrar candidata: " + ex.Message);
                    return false;
                }
            }
        }

        // En SIVUG.Models.DAO.CandidataDAO

        public Candidata ObtenerPorIdUsuario(int idUsuario)
        {
            string query = "SELECT * FROM Candidatas WHERE id_candidata = @idCandidata";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {


                conn.Open();

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@idCandidata", idUsuario);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                          
                            return new Candidata
                            {
                                CandidataId = reader.GetInt32(reader.GetOrdinal("id_candidata")),
                               
                                
                            };
                        }


                        return null;
                    }
                }
            }
        }
             

        public bool ActualizarEstadoCandidato(int candidataId, bool activa)
        {
            // Ajustado a nombres de columna MySQL (snake_case)
            string query = @"UPDATE candidatas SET activa = @Activa WHERE id_candidata = @CandidataId";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CandidataId", candidataId);
                        // MySQL trata los booleanos como 1 o 0
                        cmd.Parameters.AddWithValue("@Activa", activa);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al actualizar estado: " + ex.Message);
                    return false;
                }
            }
        }
    }
}

