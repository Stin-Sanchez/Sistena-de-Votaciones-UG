using MySql.Data.MySqlClient;
using SIVUG.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.DAO
{
    public class ComentarioDao
    {
        public void Guardar(Comentario comentario)
        {
            // Validamos que los objetos relacionados existan para evitar errores de NullReference
            if (comentario.Estudiante == null || comentario.FotoComentada == null)
            {
                throw new Exception("El comentario debe tener un Estudiante y una Foto asociados.");
            }

            using (var conexion = ConexionDB.GetInstance().GetConnection())
            {
                conexion.Open();
                using (var transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        string sql = @"INSERT INTO comentarios 
                                       (contenido, fecha_comentario, id_estudiante, id_foto) 
                                       VALUES 
                                       (@contenido, @fecha, @idEstudiante, @idFoto)";

                        using (var cmd = new MySqlCommand(sql, conexion, transaccion))
                        {
                            // Asignación de parámetros
                            cmd.Parameters.AddWithValue("@contenido", comentario.Contenido);
                            cmd.Parameters.AddWithValue("@fecha", comentario.FechaComentario);

                            // Extraemos los IDs de las propiedades de navegación
                            cmd.Parameters.AddWithValue("@idEstudiante", comentario.Estudiante.Id);
                            cmd.Parameters.AddWithValue("@idFoto", comentario.FotoComentada.Id);

                            cmd.ExecuteNonQuery();

                            // Recuperamos el ID generado y lo asignamos al objeto
                            comentario.Id = cmd.LastInsertedId;
                        }

                        // Si todo sale bien, confirmamos los cambios
                        transaccion.Commit();
                    }
                    catch (Exception ex)
                    {
                        // Si algo falla, revertimos todo
                        transaccion.Rollback();
                        throw new Exception("Error al guardar el comentario: " + ex.Message);
                    }
                }
            }
        }



        public List<Comentario> ObtenerPorFotoId(long fotoId)
        {
            List<Comentario> lista = new List<Comentario>();

            using (var conexion = ConexionDB.GetInstance().GetConnection())
            {
                conexion.Open();

        
                //Storage Procedure que realiza select hacia la base de datos y obtiene los comentarios de una foto junto con los datos del estudiante que hizo el comentario
                string sp = "sp_obtener_foto_comentarios_estudiante";

                using (var cmd = new MySqlCommand(sp, conexion))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    //Le pasamos el id de la foto como parametro al Storage Procedure , el nombre del parametro es @p_id_foto en el storage procedure
                    cmd.Parameters.AddWithValue("@p_id_foto", fotoId);

                    using (var reader = cmd.ExecuteReader())
                    {
                      
                        while (reader.Read())
                        {
                            // 1. Reconstruir Estudiante
                            Estudiante estudiante = new Estudiante
                            {
                                Id = reader.GetInt32("id_estudiante"),
                                Matricula = reader.GetString("matricula"),
                                // Asignamos datos de persona
                                Nombres = reader.GetString("nombres"),
                                Apellidos = reader.GetString("apellidos"),
                                // Asumo que foto_perfil puede ser nula o una ruta string
                                // Si es BLOB, la lectura es diferente. Asumo ruta por ahora.
                                FotoPerfilRuta = reader.IsDBNull(reader.GetOrdinal("ruta_foto_perfil")) ? null : reader.GetString("ruta_foto_perfil")
                            };

                            // 2. Reconstruir Comentario
                            Comentario comentario = new Comentario
                            {
                                Id = reader.GetInt64("id_comentario"),
                                Contenido = reader.GetString("contenido"),
                                FechaComentario = reader.GetDateTime("fecha_comentario"),
                                Estudiante = estudiante, // Asignamos el objeto completo
                                EstudianteId = estudiante.Id,
                                FotoId = fotoId
                            };

                            lista.Add(comentario);
                        }
                    }
                }
            }
            return lista;
        }
    }
}
