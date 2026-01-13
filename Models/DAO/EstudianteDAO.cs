using MySql.Data.MySqlClient;
using SIVUG.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.DAO
{
    internal class EstudianteDAO
    {
     
      

        public void Guardar(Estudiante estudiante)
        {
            using (var conexion = ConexionDB.GetInstance().GetConnection())
            {
                conexion.Open();

                // Usamos transacción porque son 2 tablas
                using (var transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        // ---------------------------------------------------
                        // 1. INSERTAR EN TABLA PADRE (PERSONAS)
                        // ---------------------------------------------------
                        string sqlPersona = "INSERT INTO personas (dni, nombres, apellidos, edad) VALUES (@dni, @nom, @ape, @edad)";

                        using (var cmd = new MySqlCommand(sqlPersona, conexion, transaccion))
                        {
                            // Leemos directo del DTO
                            cmd.Parameters.AddWithValue("@dni", estudiante.DNI);
                            cmd.Parameters.AddWithValue("@nom", estudiante.Nombres);
                            cmd.Parameters.AddWithValue("@ape", estudiante.Apellidos);
                            cmd.Parameters.AddWithValue("@edad", estudiante.Edad);
                            cmd.ExecuteNonQuery();

                            // *** TRUCO: Actualizamos el ID del DTO con el ID autogenerado ***
                            estudiante.Id = (int)cmd.LastInsertedId;
                        }

                        // ---------------------------------------------------
                        // 2. INSERTAR EN TABLA HIJA (ESTUDIANTES)
                        // ---------------------------------------------------
                        string sqlEstudiante = @"INSERT INTO estudiantes (id_estudiante, matricula, carrera, semestre, facultad, ha_votado_reina, ha_votado_fotogenia) 
                                                 VALUES (@id, @mat, @car, @sem, @fac, @votoR, @votoF)";

                        using (var cmd = new MySqlCommand(sqlEstudiante, conexion, transaccion))
                        {
                            // Usamos el ID que acabamos de obtener en el paso anterior
                            cmd.Parameters.AddWithValue("@id", estudiante.Id);
                            cmd.Parameters.AddWithValue("@mat", estudiante.Matricula);
                            cmd.Parameters.AddWithValue("@car", estudiante.Carrera);
                            cmd.Parameters.AddWithValue("@sem", estudiante.Semestre);
                            cmd.Parameters.AddWithValue("@fac", estudiante.Facultad);
                            cmd.Parameters.AddWithValue("@votoR", estudiante.HavotadoReina);
                            cmd.Parameters.AddWithValue("@votoF", estudiante.HavotadoFotogenia);
                            cmd.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                    }
                    catch (Exception)
                    {
                        transaccion.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}

