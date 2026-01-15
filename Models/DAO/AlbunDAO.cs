using MySql.Data.MySqlClient;
using SIVUG.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.DAO
{
    public class AlbumDAO
    {
        // 1. OBTENER ÁLBUMES DE UNA CANDIDATA
        public List<Album> ObtenerPorCandidata(int idCandidata)
        {
            var lista = new List<Album>();

            // Usamos tu Singleton para obtener la conexión
            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    // Asumo que tu tabla se llama 'albumes' y la FK 'id_candidata'
                    string query = "SELECT id_album, titulo, descripcion FROM albumes WHERE id_candidata = @id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idCandidata);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lista.Add(new Album
                                {
                                    Id = Convert.ToInt64(reader["id_album"]),
                                    Titulo = reader["titulo"].ToString(),
                                    Descripcion = reader["descripcion"].ToString(),
                                    // Solo asignamos el ID de la candidata para referencia
                                    Candidata = new Candidata { CandidataId = idCandidata }
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al obtener álbumes: " + ex.Message);
                }
            }
            return lista;
        }

        // 2. GUARDAR ÁLBUM Y SUS FOTOS (TRANSACCIONAL)
        public bool GuardarAlbumCompleto(Album album, List<Foto> fotosParaAgregar)
        {
            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                conn.Open();

                // Iniciamos transacción para asegurar que se guarde Álbum + Fotos o nada
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        long albumId = album.Id;

                        // --- PASO A: INSERTAR O ACTUALIZAR EL ÁLBUM ---
                        if (albumId == 0) // Es Nuevo
                        {
                            string queryAlbum = @"INSERT INTO albumes (titulo, descripcion, id_candidata,fecha_creacion) 
                                                  VALUES (@tit, @desc, @candId, @fecha)";

                            using (var cmd = new MySqlCommand(queryAlbum, conn))
                            {
                                cmd.Transaction = transaction; // Vincular a transacción
                                cmd.Parameters.AddWithValue("@tit", album.Titulo);
                                cmd.Parameters.AddWithValue("@desc", album.Descripcion);
                                cmd.Parameters.AddWithValue("@candId", album.Candidata.CandidataId);
                                cmd.Parameters.AddWithValue("@fecha", album.FechaCreacion);

                                cmd.ExecuteNonQuery();
                                albumId = cmd.LastInsertedId; // ID generado por MySQL
                                album.Id = albumId; // Actualizamos el objeto en memoria
                            }
                        }
                        else // Ya existe, actualizamos
                        {
                            string queryUpdate = "UPDATE albumes SET titulo = @tit, descripcion = @desc , fecha_creacion= @fecha WHERE id_album = @id";
                            using (var cmd = new MySqlCommand(queryUpdate, conn))
                            {
                                cmd.Transaction = transaction;
                                cmd.Parameters.AddWithValue("@tit", album.Titulo);
                                cmd.Parameters.AddWithValue("@desc", album.Descripcion);
                                cmd.Parameters.AddWithValue("@fecha", album.FechaCreacion);

                                cmd.Parameters.AddWithValue("@id", album.Id);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // --- PASO B: INSERTAR LAS FOTOS NUEVAS ---
                        if (fotosParaAgregar != null && fotosParaAgregar.Count > 0)
                        {
                            // Asumo tabla 'fotos' con columnas 'ruta_archivo', 'descripcion', 'id_album'
                            string queryFoto = @"INSERT INTO fotos (ruta_archivo, descripcion, id_album,fecha_subida) 
                                                 VALUES (@ruta, @desc, @albId, @fecha)";

                            foreach (var foto in fotosParaAgregar)
                            {
                                using (var cmdFoto = new MySqlCommand(queryFoto, conn))
                                {
                                    cmdFoto.Transaction = transaction; // ¡Importante!
                                    cmdFoto.Parameters.AddWithValue("@ruta", foto.RutaArchivo);

                                    // Validamos si descripción es nula
                                    object descVal = string.IsNullOrEmpty(foto.Descripcion) ? (object)DBNull.Value : foto.Descripcion;
                                    cmdFoto.Parameters.AddWithValue("@desc", descVal);

                                    cmdFoto.Parameters.AddWithValue("@albId", albumId);
                                    cmdFoto.Parameters.AddWithValue("@fecha", foto.FechaSubida);

                                    cmdFoto.ExecuteNonQuery();
                                }
                            }
                        }

                        // Si todo salió bien, confirmamos cambios
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // Si algo falló, deshacemos todo
                        transaction.Rollback();
                        Console.WriteLine("Error transacción álbum: " + ex.Message);
                        throw; // Re-lanzamos para que la UI muestre el error
                    }
                }
            }
        }
                

                public List<Foto> ObtenerFotosPorAlbum(long idAlbum)
        {
            var listaFotos = new List<Foto>();

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    // Consultamos la tabla 'fotos' filtrando por el álbum seleccionado
                    string query = "SELECT id_foto, ruta_archivo, descripcion FROM fotos WHERE id_album = @idAlbum";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@idAlbum", idAlbum);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Reconstruimos el objeto Foto
                                var foto = new Foto
                                {
                                    Id = Convert.ToInt64(reader["id_foto"]),
                                    RutaArchivo = reader["ruta_archivo"].ToString(),
                                    Descripcion = reader["descripcion"].ToString(),
                                    // No necesitamos cargar todo el objeto Album aquí, 
                                    // solo saber que pertenecen a este ID.
                                    Album = new Album { Id = idAlbum }
                                };
                                listaFotos.Add(foto);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Manejo básico de errores (puedes usar un logger)
                    Console.WriteLine("Error al obtener fotos del álbum: " + ex.Message);
                }
            }
            return listaFotos;
        }
    }
        }
    

