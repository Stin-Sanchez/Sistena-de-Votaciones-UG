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
    public class EstudianteDAO
    {



        public void Guardar(Estudiante estudiante)
        {
            using (var conexion = ConexionDB.GetInstance().GetConnection())
            {
                conexion.Open();
                using (var transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        // 1. INSERTAR PERSONA
                        string sqlPersona = "INSERT INTO personas (dni, nombres, apellidos, edad) VALUES (@dni, @nom, @ape, @edad)";

                        using (var cmd = new MySqlCommand(sqlPersona, conexion, transaccion))
                        {
                            cmd.Parameters.AddWithValue("@dni", estudiante.DNI);
                            cmd.Parameters.AddWithValue("@nom", estudiante.Nombres);
                            cmd.Parameters.AddWithValue("@ape", estudiante.Apellidos);
                            cmd.Parameters.AddWithValue("@edad", estudiante.Edad);
                            cmd.ExecuteNonQuery();
                            estudiante.Id = (int)cmd.LastInsertedId;
                        }

                        // 2. INSERTAR ESTUDIANTE (CORREGIDO: Quitamos las columnas de votos)
                        // Ya no guardamos ha_votado_reina ni ha_votado_fotogenia
                        string sqlEstudiante = @"INSERT INTO estudiantes 
                                       (id_estudiante, matricula, semestre, id_carrera) 
                                       VALUES 
                                       (@id, @mat, @sem, @idCar)";

                        using (var cmd = new MySqlCommand(sqlEstudiante, conexion, transaccion))
                        {
                            cmd.Parameters.AddWithValue("@id", estudiante.Id);
                            cmd.Parameters.AddWithValue("@mat", estudiante.Matricula);
                            cmd.Parameters.AddWithValue("@sem", estudiante.Semestre);
                            cmd.Parameters.AddWithValue("@idCar", estudiante.IdCarrera);

                            // ELIMINAMOS ESTAS LÍNEAS PORQUE YA NO EXISTEN EN BD:
                            // cmd.Parameters.AddWithValue("@votoR", ...);
                            // cmd.Parameters.AddWithValue("@votoF", ...);

                            cmd.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        // ... tu manejo de errores
                        throw;
                    }
                }
            }
        }

        public Estudiante ObtenerPorCedula(string cedula)
        {
            Estudiante estudiante = null;

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    string query = @"
            SELECT 
                e.id_estudiante, 
                p.dni, 
                p.nombres, 
                p.apellidos,
                c.id_carrera,              
                c.nombre AS nombre_carrera,
                f.id_facultad, 
                f.nombre as nombre_facultad,
                -- CORRECCIÓN CLAVE: Buscamos por TEXTO ('Reina', 'Fotogenia')
                -- para coincidir con lo que guarda tu VotoDAO.
                (SELECT COUNT(*) FROM votos v WHERE v.id_estudiante = e.id_estudiante AND v.tipo_voto = 'Reina') as voto_reina,
                (SELECT COUNT(*) FROM votos v WHERE v.id_estudiante = e.id_estudiante AND v.tipo_voto = 'Fotogenia') as voto_fotogenia
            FROM estudiantes e
            INNER JOIN personas p ON e.id_estudiante = p.id_persona
            INNER JOIN carreras c ON e.id_carrera = c.id_carrera
            INNER JOIN facultades f ON c.id_facultad = f.id_facultad
            WHERE p.dni = @Cedula";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Cedula", cedula);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                estudiante = new Estudiante
                                {
                                    Id = reader.GetInt32("id_estudiante"),
                                    DNI = reader.GetString("dni"),
                                    Nombres = reader.GetString("nombres"),
                                    Apellidos = reader.GetString("apellidos"),
                                    IdCarrera = reader.GetInt32("id_carrera"),

                                    // Asignamos las banderas: Si el conteo > 0, es TRUE
                                    HavotadoReina = reader.GetInt32("voto_reina") > 0,
                                    HavotadoFotogenia = reader.GetInt32("voto_fotogenia") > 0,

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
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al buscar estudiante: " + ex.Message);
                }
            }
            return estudiante;
        }


        // Método para llenar la tabla principal con datos leíbles
        public List<Estudiante> ObtenerTodosDetallado()
        {
            var lista = new List<Estudiante>();
            using (var conexion = ConexionDB.GetInstance().GetConnection())
            {
                conexion.Open();
                // Hacemos JOIN para traer los NOMBRES de carrera y facultad, no solo los IDs
                string sql = @"
            SELECT 
                e.id_estudiante, e.matricula, e.semestre, 
                p.dni, p.nombres, p.apellidos, p.edad,   -- AQUÍ ESTABA EL ERROR (antes decia e.dni)
                c.id_carrera, c.nombre AS nombre_carrera,
                f.id_facultad, f.nombre AS nombre_facultad
            FROM estudiantes e
            INNER JOIN personas p ON e.id_estudiante = p.id_persona
            INNER JOIN carreras c ON e.id_carrera = c.id_carrera
            INNER JOIN facultades f ON c.id_facultad = f.id_facultad";

                using (var cmd = new MySqlCommand(sql, conexion))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Reconstruimos el objeto completo
                        var estudiante = new Estudiante
                        {
                            Id = reader.GetInt32("id_estudiante"),
                            Matricula = reader.GetString("matricula"),
                            Semestre = reader.GetByte("semestre"),
                            // Heredados de Persona
                            DNI = reader.GetString("dni"),
                            Nombres = reader.GetString("nombres"),
                            Apellidos = reader.GetString("apellidos"),
                            Edad = reader.GetByte("edad"),

                            // Llenamos la relación completa para poder filtrar después
                            IdCarrera = reader.GetInt32("id_carrera"),
                            Carrera = new Carrera
                            {
                                Id = reader.GetInt32("id_carrera"),
                                Nombre = reader.GetString("nombre_carrera"),
                                IdFacultad = reader.GetInt32("id_facultad"),
                                // Llenamos también la facultad dentro de la carrera
                                Facultad = new Facultad
                                {
                                    Id = reader.GetInt32("id_facultad"),
                                    Nombre = reader.GetString("nombre_facultad")
                                }
                            }
                        };
                        lista.Add(estudiante);
                    }
                }
            }
            return lista;
        }
    }
    }
