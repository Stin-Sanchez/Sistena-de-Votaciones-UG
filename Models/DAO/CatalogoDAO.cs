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
    public class CatalogoDAO
    {
        // 1. Obtener lista para llenar los autocompletados
        public List<CatalogoDTO> ObtenerPorTipo(string tipo)
        {
            var lista = new List<CatalogoDTO>();
            string query = "SELECT IdCatalogo, Nombre FROM catalogos WHERE Tipo = @tipo";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@tipo", tipo);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lista.Add(new CatalogoDTO
                                {
                                    Id = reader.GetInt32(0),
                                    Nombre = reader.GetString(1),
                                    Tipo = tipo
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al obtener catálogos por tipo: " + ex.Message);
                }
            }
            return lista;
        }

        // 2. Método Maestro: Guarda o recupera el ID de una etiqueta
        public int ObtenerOInsertar(string nombre, string tipo)
        {
            nombre = nombre.Trim(); // Limpiar espacios

            string queryBuscar = "SELECT   IdCatalogo FROM catalogos WHERE Nombre = @nombre AND Tipo = @tipo";
            string queryInsert = "INSERT INTO catalogos (Nombre, Tipo) VALUES (@nombre, @tipo); SELECT LAST_INSERT_ID();";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();

                    // A. Intentar buscar si ya existe
                    using (var cmd = new MySqlCommand(queryBuscar, conn))
                    {
                        cmd.Parameters.AddWithValue("@nombre", nombre);
                        cmd.Parameters.AddWithValue("@tipo", tipo);
                        var result = cmd.ExecuteScalar();

                        if (result != null) return Convert.ToInt32(result);
                    }

                    // B. Si no existe, insertar
                    using (var cmd = new MySqlCommand(queryInsert, conn))
                    {
                        cmd.Parameters.AddWithValue("@nombre", nombre);
                        cmd.Parameters.AddWithValue("@tipo", tipo);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al obtener o insertar catálogo: " + ex.Message);
                    return -1; // Valor de error
                }
            }
        }

        // 3. Vincular a la candidata
        public void AsignarDetalles(int idCandidata, List<string> habilidades, List<string> pasatiempos, List<string> aspiraciones)
        {
            // Primero limpiamos los anteriores para evitar duplicados al editar
            string queryLimpiar = "DELETE FROM candidata_detalles WHERE id_candidata = @id";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(queryLimpiar, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idCandidata);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al limpiar detalles de candidata: " + ex.Message);
                    return;
                }
            }

            // Guardamos cada lista
            GuardarLista(idCandidata, habilidades, "HABILIDAD");
            GuardarLista(idCandidata, pasatiempos, "PASATIEMPO");
            GuardarLista(idCandidata, aspiraciones, "ASPIRACION");
        }

        private void GuardarLista(int idCandidata, List<string> items, string tipo)
        {
            if (items == null) return;

            string queryLink = "INSERT INTO candidata_detalles (Id_Candidata, Id_Catalogo) VALUES (@cand, @cat)";

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item)) continue;

                int idCatalogo = ObtenerOInsertar(item, tipo); // Magia: Busca o Crea

                if (idCatalogo <= 0) continue; // Si hubo error, saltar

                using (var conn = ConexionDB.GetInstance().GetConnection())
                {
                    try
                    {
                        conn.Open();
                        using (var cmd = new MySqlCommand(queryLink, conn))
                        {
                            cmd.Parameters.AddWithValue("@cand", idCandidata);
                            cmd.Parameters.AddWithValue("@cat", idCatalogo);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error al vincular catálogo con candidata: " + ex.Message);
                    }
                }
            }
        }

        // 4. Recuperar para el Perfil
        public List<CatalogoDTO> ObtenerDeCandidata(int idCandidata)
        {
            var lista = new List<CatalogoDTO>();
            string query = @"SELECT c.IdCatalogo, c.Nombre, c.Tipo 
                     FROM catalogos c 
                     INNER JOIN candidata_detalles d ON c.IdCatalogo = d.Id_Catalogo 
                     WHERE d.Id_Candidata = @id";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idCandidata);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lista.Add(new CatalogoDTO
                                {
                                    Id = reader.GetInt32(0),
                                    Nombre = reader.GetString(1),
                                    Tipo = reader.GetString(2)
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al obtener detalles de candidata: " + ex.Message);
                }
            }
            return lista;

        }
    }
}
