using MySql.Data.MySqlClient;
using SIVUG.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.DAO
{
    public class VotoDAO
    {
        // Método 1: Solo verifica si existe (Consulta)
        public bool YaVoto(int idEstudiante, string tipoVoto)
        {
            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                conn.Open();
                // Verificamos usando la restricción de estudiante + tipo
                string query = "SELECT COUNT(*) FROM votos WHERE id_estudiante = @idEst AND tipo_voto = @tipo";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@idEst", idEstudiante);
                    cmd.Parameters.AddWithValue("@tipo", tipoVoto);

                    long count = (long)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        // Método 2: Solo inserta (Comando)
        public void Insertar(Voto voto)
        {
            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    string query = @"INSERT INTO votos (id_estudiante, id_candidata, tipo_voto, fecha_votacion) 
                                     VALUES (@idEst, @idCan, @tipo, @fecha)";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@idEst", voto.Sufragador.Id);
                        cmd.Parameters.AddWithValue("@idCan", voto.Candidata.CandidataId);
                        // Convertimos el Enum a String porque en BD es un ENUM('Reina','Fotogenia')
                        cmd.Parameters.AddWithValue("@tipo", voto.Tipo.ToString());
                        cmd.Parameters.AddWithValue("@fecha", voto.FechaVotacion);

                        cmd.ExecuteNonQuery();
                    }
                }
                catch (MySqlException ex)
                {
                    // Error 1062 es "Duplicate entry" (por si falló la validación previa)
                    if (ex.Number == 1062)
                    {
                        throw new Exception("El estudiante ya ha realizado un voto para esta categoría.");
                    }
                    throw ex;
                }
            }
        }
    }
}
