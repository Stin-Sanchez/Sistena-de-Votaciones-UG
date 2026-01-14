using MySql.Data.MySqlClient;
using SIVUG.Models.DTOS;
using SIVUG.Util;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.DAO
{
    public class FacultadDAO
    {
        // CREATE
        public void Insertar(Facultad facultad)
        {
            using (var conexion = ConexionDB.GetInstance().GetConnection())
            {
                conexion.Open();
                string sql = "INSERT INTO facultades (nombre) VALUES (@nom)";
                using (var cmd = new MySqlCommand(sql, conexion))
                {
                    cmd.Parameters.AddWithValue("@nom", facultad.Nombre);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // READ (Para llenar el primer ComboBox)
        public List<Facultad> ObtenerTodas()
        {
            var lista = new List<Facultad>();
            using (var conexion = ConexionDB.GetInstance().GetConnection())
            {
                conexion.Open();
                string sql = "SELECT id_facultad, nombre FROM facultades ORDER BY nombre ASC";
                using (var cmd = new MySqlCommand(sql, conexion))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Facultad
                        {
                            Id = reader.GetInt32("id_facultad"),
                            Nombre = reader.GetString("nombre")
                        });
                    }
                }
            }
            return lista;
        }

        // UPDATE
        public void Actualizar(Facultad facultad)
        {
            using (var conexion = ConexionDB.GetInstance().GetConnection())
            {
                conexion.Open();
                string sql = "UPDATE facultades SET nombre = @nom WHERE id_facultad = @id";
                using (var cmd = new MySqlCommand(sql, conexion))
                {
                    cmd.Parameters.AddWithValue("@nom", facultad.Nombre);
                    cmd.Parameters.AddWithValue("@id", facultad.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // DELETE (Físico por ahora)
        public void Eliminar(int id)
        {
            using (var conexion = ConexionDB.GetInstance().GetConnection())
            {
                conexion.Open();
                // OJO: Esto fallará si la facultad tiene carreras asignadas (por la FK)
                string sql = "DELETE FROM facultades WHERE id_facultad = @id";
                using (var cmd = new MySqlCommand(sql, conexion))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<Facultad> ObtenerProgresoVotacion()
        {
            List<Facultad> lista = new List<Facultad>();

            string query = @"
    SELECT 
        f.id_facultad, 
        f.nombre,
        COUNT(DISTINCT e.id_estudiante) as TotalEstudiantes,
        -- CAMBIO CLAVE: Contamos estudiantes únicos que han votado (v.id_estudiante), 
        -- no los votos (v.id_voto).
        COUNT(DISTINCT v.id_estudiante) as EstudiantesQueVotaron
    FROM facultades f
    INNER JOIN carreras c ON f.id_facultad = c.id_facultad
    INNER JOIN estudiantes e ON c.id_carrera = e.id_carrera
    LEFT JOIN votos v ON e.id_estudiante = v.id_estudiante
    GROUP BY f.id_facultad, f.nombre
    ORDER BY f.nombre ASC";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int total = Convert.ToInt32(reader["TotalEstudiantes"]);
                                // Ahora esta variable representa PERSONAS, no votos.
                                int votantes = Convert.ToInt32(reader["EstudiantesQueVotaron"]);

                                lista.Add(new Facultad
                                {
                                    Id = reader.GetInt32("id_facultad"),
                                    Nombre = reader.GetString("nombre"),
                                    TotalEstudiantes = total,

                                    // Si quieres mostrar en la UI cuántos han votado (ej: 5/8), usa 'votantes'.
                                    // Si tu propiedad se llama obligatoriamente 'VotosEmitidos', asígnale 'votantes'
                                    // para que la barra de progreso tenga sentido semántico.
                                    VotosEmitidos = votantes,

                                    // Cálculo del porcentaje basado en PARTICIPACIÓN real
                                    PorcentajeParticipacion = total > 0 ?
                                        Math.Round((decimal)votantes / total * 100, 1) : 0
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error calculando progreso: " + ex.Message);
                }
            }
            return lista;
        }
    }
}
