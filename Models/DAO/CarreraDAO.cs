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
    public class CarreraDAO
    {
        // CREATE
        public void Insertar(Carrera carrera)
        {
            using (var conexion = ConexionDB.GetInstance().GetConnection())
            {
                conexion.Open();
                string sql = "INSERT INTO carreras (nombre, id_facultad) VALUES (@nom, @idFac)";
                using (var cmd = new MySqlCommand(sql, conexion))
                {
                    cmd.Parameters.AddWithValue("@nom", carrera.Nombre);
                    cmd.Parameters.AddWithValue("@idFac", carrera.IdFacultad);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public DashboardDTO ObtenerDatosDashboard()
        {
            DashboardDTO dashboard = new DashboardDTO();

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                conn.Open();

                // ---------------------------------------------------------
                // 1. ESTADÍSTICAS GENERALES (Sin cambios)
                // ---------------------------------------------------------
                string queryStats = @"
            SELECT 
                COUNT(DISTINCT e.id_estudiante) as TotalEstudiantes,
                COUNT( v.id_voto) as VotosEmitidos,
                COUNT(DISTINCT v.id_estudiante) as EstudiantesQueVotaron,
                (SELECT COUNT(*) FROM candidatas WHERE activa = 1) as CandidatasActivas
            FROM estudiantes e
            LEFT JOIN votos v ON e.id_estudiante = v.id_estudiante";

                using (var cmd = new MySqlCommand(queryStats, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int total = Convert.ToInt32(reader["TotalEstudiantes"]);
                            int votos = reader["VotosEmitidos"] != DBNull.Value ? Convert.ToInt32(reader["VotosEmitidos"]) : 0;

                            // NUEVO: Obtenemos la cantidad real de participantes
                            int participantes = reader["EstudiantesQueVotaron"] != DBNull.Value ? Convert.ToInt32(reader["EstudiantesQueVotaron"]) : 0;

                            dashboard.TotalEstudiantes = total;
                            dashboard.VotosEmitidos = votos; // Seguimos mostrando 10 votos totales

                            // CORRECCIÓN: Calculamos % usando participantes vs total, no votos vs total
                            dashboard.PorcentajeVotacion = total > 0 ?
                                Math.Round((decimal)participantes / total * 100, 1) : 0;

                            dashboard.CandidatasActivas = Convert.ToInt32(reader["CandidatasActivas"]);
                        }
                    }
                }

                string queryTop3 = @"
    SELECT 
        c.id_candidata, 
        p.nombres, 
        p.apellidos, 
        f.nombre as nombre_facultad,
        COUNT(v.id_voto) as Votos, 
        c.url_foto,
        -- NUEVO: Convertimos el número 1 o 2 en texto para que coincida con tus Tabs
        CASE 
            WHEN v.tipo_voto = 1 THEN 'Reina'
            WHEN v.tipo_voto = 2 THEN 'Fotogenia'
            ELSE 'Desconocido'
        END as tipo_texto
    FROM candidatas c
    INNER JOIN personas p ON c.id_candidata = p.id_persona
    INNER JOIN estudiantes e ON c.id_candidata = e.id_estudiante
    INNER JOIN carreras car ON e.id_carrera = car.id_carrera
    INNER JOIN facultades f ON car.id_facultad = f.id_facultad
    -- Usamos INNER JOIN votos para traer solo las que han recibido votos
    -- (O LEFT JOIN si quieres mostrar ceros, pero para el Top 3 mejor INNER)
    INNER JOIN votos v ON c.id_candidata = v.id_candidata
    WHERE c.activa = 1
    -- Agrupamos también por tipo_voto para separar los conteos
    GROUP BY c.id_candidata, p.nombres, p.apellidos, f.nombre, c.url_foto, v.tipo_voto
    ORDER BY Votos DESC
    ";
                // NOTA: Quité el LIMIT 3. Traemos todas y el C# filtra las 3 mejores de cada grupo.

                dashboard.Top3Candidatas = new List<ResultadoPreliminarDTO>();

                using (var cmd = new MySqlCommand(queryTop3, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        // int posicion = 1; // Ya no usamos posición fija aquí porque depende del tab
                        while (reader.Read())
                        {
                            string nombreCompleto = reader.GetString("nombres") + " " + reader.GetString("apellidos");

                            dashboard.Top3Candidatas.Add(new ResultadoPreliminarDTO
                            {
                                CandidataId = reader.GetInt32("id_candidata"),
                                Nombre = nombreCompleto,
                                FacultadNombre = reader.GetString("nombre_facultad"),
                                Votos = Convert.ToInt32(reader["Votos"]),
                                UrlFoto = reader.IsDBNull(reader.GetOrdinal("url_foto")) ? "" : reader.GetString("url_foto"),

                                // --- ASIGNACIÓN CLAVE QUE FALTABA ---
                                // Esto permite que el Formulario sepa en qué pestaña ponerla
                                TipoCandidatura = reader.GetString("tipo_texto")
                            });
                        }
                    }
                }
            }

            // 3. PROGRESO POR FACULTAD (Sin cambios)
            FacultadDAO facultadDAO = new FacultadDAO();
            dashboard.ProgresoFacultades = facultadDAO.ObtenerProgresoVotacion();

            return dashboard;
        }


        // READ - FILTRO POR FACULTAD (Efecto Cascada)
        public List<Carrera> ObtenerPorIdFacultad(int idFacultad)
        {
            var lista = new List<Carrera>();
            using (var conexion = ConexionDB.GetInstance().GetConnection())
            {
                conexion.Open();
                string sql = "SELECT id_carrera, nombre, id_facultad FROM carreras WHERE id_facultad = @idFac ORDER BY nombre ASC";

                using (var cmd = new MySqlCommand(sql, conexion))
                {
                    cmd.Parameters.AddWithValue("@idFac", idFacultad);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Carrera
                            {
                                Id = reader.GetInt32("id_carrera"),
                                Nombre = reader.GetString("nombre"),
                                IdFacultad = reader.GetInt32("id_facultad")
                            });
                        }
                    }
                }
            }
            return lista;
        }

        // READ ALL (Por si necesitas listar todo sin filtro)
        public List<Carrera> ObtenerTodas()
        {
            var lista = new List<Carrera>();
            using (var conexion = ConexionDB.GetInstance().GetConnection())
            {
                conexion.Open();
                string sql = "SELECT * FROM carreras";
                // Nota: Aquí podrías hacer un JOIN si quisieras traer el nombre de la facultad también
                using (var cmd = new MySqlCommand(sql, conexion))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Carrera
                        {
                            Id = reader.GetInt32("id_carrera"),
                            Nombre = reader.GetString("nombre"),
                            IdFacultad = reader.GetInt32("id_facultad")
                        });
                    }
                }
            }
            return lista;
        }

        // UPDATE
        public void Actualizar(Carrera carrera)
        {
            using (var conexion = ConexionDB.GetInstance().GetConnection())
            {
                conexion.Open();
                string sql = "UPDATE carreras SET nombre = @nom, id_facultad = @idFac WHERE id_carrera = @id";
                using (var cmd = new MySqlCommand(sql, conexion))
                {
                    cmd.Parameters.AddWithValue("@nom", carrera.Nombre);
                    cmd.Parameters.AddWithValue("@idFac", carrera.IdFacultad);
                    cmd.Parameters.AddWithValue("@id", carrera.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // DELETE
        public void Eliminar(int id)
        {
            using (var conexion = ConexionDB.GetInstance().GetConnection())
            {
                conexion.Open();
                string sql = "DELETE FROM carreras WHERE id_carrera = @id";
                using (var cmd = new MySqlCommand(sql, conexion))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
