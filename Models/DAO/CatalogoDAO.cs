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
    /// DAO: Acceso a Datos para CATÁLOGOS (Habilidades, Pasatiempos, Aspiraciones)
    /// 
    /// Responsabilidades:
    /// - Gestionar tabla 'catalogos' (almacén maestro)
    /// - Gestionar tabla 'candidata_detalles' (relación M-M)
    /// - Implementar patrón UPSERT (buscar o insertar si no existe)
    /// - Vincular perfiles a candidatas
    /// 
    /// Patrón UPSERT:
    /// Para evitar duplicados, si "Liderazgo" ya existe en HABILIDAD,
    /// se reutiliza ese ID en lugar de crear uno nuevo
    /// </summary>
    public class CatalogoDAO
    {
        /// <summary>
        /// Obtiene lista de catálogos de UN TIPO ESPECÍFICO.
        /// 
        /// Uso: Llenar ComboBox de habilidades, pasatiempos, aspiraciones
        /// Filtro: WHERE Tipo = @tipo (HABILIDAD, PASATIEMPO, ASPIRACION)
        /// 
        /// Retorna: List<CatalogoDTO> con Id, Nombre, Tipo
        /// Nunca nulo (retorna lista vacía si no hay registros)
        /// </summary>
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


        /// <summary>
        /// Asigna TODOS los detalles a una candidata (registro inicial).
        /// 
        /// Flujo:
        /// 1. LIMPIAR: DELETE de candidata_detalles para esta candidata
        ///    (evita duplicados si se re-ejecuta)
        /// 2. GUARDAR: Habilidades, Pasatiempos, Aspiraciones
        /// 
        /// NOTA: Se llama UNA sola vez al registrar (no en ediciones posteriores)
        /// Para editar, usar ActualizarDetalles()
        /// </summary>
        public void AsignarDetalles(int idCandidata, List<string> habilidades, List<string> pasatiempos, List<string> aspiraciones)
        {
            System.Diagnostics.Debug.WriteLine($"[CATALOGO] AsignarDetalles -> CandidataId: {idCandidata}");

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        string queryLimpiar = "DELETE FROM candidata_detalles WHERE id_candidata = @id";
                        using (var cmd = new MySqlCommand(queryLimpiar, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@id", idCandidata);
                            int rows = cmd.ExecuteNonQuery();
                            System.Diagnostics.Debug.WriteLine($"[CATALOGO] Limpieza candidata_detalles -> filas: {rows}");
                        }

                        GuardarLista(idCandidata, habilidades, "HABILIDAD", conn, tx);
                        GuardarLista(idCandidata, pasatiempos, "PASATIEMPO", conn, tx);
                        GuardarLista(idCandidata, aspiraciones, "ASPIRACION", conn, tx);

                        tx.Commit();
                        System.Diagnostics.Debug.WriteLine("[CATALOGO] Commit OK");
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        System.Diagnostics.Debug.WriteLine($"[CATALOGO] ERROR AsignarDetalles: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Actualiza los detalles de una candidata EXISTENTE.
        /// 
        /// Idéntico a AsignarDetalles():
        /// - Limpia detalles anteriores
        /// - Re-inserta todos los nuevos
        /// 
        /// Uso: En FormEditarCandidata después de actualizar candidata
        /// </summary>
        public void ActualizarDetalles(int idCandidata, List<string> habilidades, List<string> pasatiempos, List<string> aspiraciones)
        {
            // Reutiliza AsignarDetalles (limpia + re-inserta)
            AsignarDetalles(idCandidata, habilidades, pasatiempos, aspiraciones);
        }

        /// <summary>
        /// Guarda UNA LISTA de items asociados a una candidata.
        /// 
        /// Flujo UPSERT en bucle:
        /// FOREACH item en lista:
        ///   1. Obtener o insertar el catálogo (ObtenerOInsertar)
        ///   2. Vincular a candidata_detalles (INSERT)
        /// 
        /// Ejemplo con habilidades = ["Liderazgo", "Comunicación"]:
        /// - ObtenerOInsertar("Liderazgo", "HABILIDAD") → ID 45
        /// - INSERT candidata_detalles (candidata_id=1, catalogo_id=45)
        /// - ObtenerOInsertar("Comunicación", "HABILIDAD") → ID 67
        /// - INSERT candidata_detalles (candidata_id=1, catalogo_id=67)
        /// </summary>
        private void GuardarLista(int idCandidata, List<string> items, string tipo, MySqlConnection conn, MySqlTransaction tx)
        {
            if (items == null || items.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[CATALOGO] GuardarLista -> {tipo} vacío");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[CATALOGO] GuardarLista -> {tipo}, items: {items.Count}");

            string queryLink = "INSERT INTO candidata_detalles (Id_Candidata, Id_Catalogo) VALUES (@cand, @cat)";

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item))
                    continue;

                int idCatalogo = ObtenerOInsertar(item, tipo, conn, tx);
                System.Diagnostics.Debug.WriteLine($"[CATALOGO] {tipo} '{item}' -> IdCatalogo: {idCatalogo}");

                if (idCatalogo <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[CATALOGO] ❌ IdCatalogo inválido para '{item}'");
                    continue;
                }

                using (var cmd = new MySqlCommand(queryLink, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@cand", idCandidata);
                    cmd.Parameters.AddWithValue("@cat", idCatalogo);
                    int rows = cmd.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine($"[CATALOGO] Link insert -> filas: {rows} (cand: {idCandidata}, cat: {idCatalogo})");
                }
            }
        }

        /// <summary>
        /// Recupera TODOS los detalles de una candidata.
        /// 
        /// Query: JOIN entre catalogos y candidata_detalles
        /// Retorna: List<CatalogoDTO> con Id, Nombre, TIPO
        /// 
        /// El Tipo permite filtrar en C# (Where tipo == "HABILIDAD", etc.)
        /// 
        /// Ejemplo para candidata ID=1:
        /// Retorna:
        /// - CatalogoDTO { Id=45, Nombre="Liderazgo", Tipo="HABILIDAD" }
        /// - CatalogoDTO { Id=67, Nombre="Comunicación", Tipo="HABILIDAD" }
        /// - CatalogoDTO { Id=123, Nombre="Lectura", Tipo="PASATIEMPO" }
        /// 
        /// Luego en Service/Form: filtrar con LINQ
        /// var habilidades = lista.Where(x => x.Tipo == "HABILIDAD").Select(x => x.Nombre).ToList();
        /// </summary>
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

        private int ObtenerOInsertar(string nombre, string tipo, MySqlConnection conn, MySqlTransaction tx)
        {
            nombre = nombre.Trim();

            string queryBuscar = "SELECT IdCatalogo FROM catalogos WHERE Nombre = @nombre AND Tipo = @tipo";
            string queryInsert = "INSERT INTO catalogos (Nombre, Tipo) VALUES (@nombre, @tipo); SELECT LAST_INSERT_ID();";

            using (var cmd = new MySqlCommand(queryBuscar, conn, tx))
            {
                cmd.Parameters.AddWithValue("@nombre", nombre);
                cmd.Parameters.AddWithValue("@tipo", tipo);
                var result = cmd.ExecuteScalar();

                if (result != null)
                {
                    int idExistente = Convert.ToInt32(result);
                    System.Diagnostics.Debug.WriteLine($"[CATALOGO] Encontrado -> {tipo} '{nombre}' Id: {idExistente}");
                    return idExistente;
                }
            }

            using (var cmd = new MySqlCommand(queryInsert, conn, tx))
            {
                cmd.Parameters.AddWithValue("@nombre", nombre);
                cmd.Parameters.AddWithValue("@tipo", tipo);
                int idNuevo = Convert.ToInt32(cmd.ExecuteScalar());
                System.Diagnostics.Debug.WriteLine($"[CATALOGO] Insertado -> {tipo} '{nombre}' Id: {idNuevo}");
                return idNuevo;
            }

           
        }
    }
}
