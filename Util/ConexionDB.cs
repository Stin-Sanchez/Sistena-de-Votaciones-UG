using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Util
{
    public class ConexionDB
    {
        // Instancia estática (Singleton)
        private static ConexionDB instancia;

      
        // Guardamos la cadena de texto para crear conexiones.
        private string connectionString;

        // Constructor PRIVADO
        private ConexionDB()
        {
            connectionString = Credenciales.ConnectionString;
        }

        public static ConexionDB GetInstance()
        {
            if (instancia == null)
            {
                instancia = new ConexionDB();
            }
            return instancia;
        }

       
        // Este método  crea una NUEVA instancia cada vez que se llama.
        public MySqlConnection GetConnection()
        {
            try
            {
                // Retornamos un objeto NUEVO con la configuración lista
                Console.WriteLine("Creando nueva conexion factory");
                return new MySqlConnection(connectionString);
           
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creando conexión: " + ex.Message);
                return null;
            }
        }
    }
}



