using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Util
{
    public class ConexionBaseDatos
    {

        //Instancia estática (el Singleton)
        private static ConexionBaseDatos instancia;

        //Objeto que guarda la conexión real a MySQL
        private MySqlConnection connection;

        //Constructor PRIVADO
        private ConexionBaseDatos()
        {
            try
            {
                Console.WriteLine("--- Intentando conectar al servidor local ---");


                string servidor = "localhost";
                string baseDatos = "tu_base_de_datos";
                string usuario = "tu_usuario";
                string password = "tu_contraseña";

                string cadenaConexion = $"Server={servidor};Database={baseDatos};Uid={usuario};Pwd={password};";

                connection = new MySqlConnection(cadenaConexion);
                connection.Open();

                // SI LLEGA AQUI, ES QUE CONECTÓ
                Console.WriteLine("\n CONEXIÓN EXITOSA: La base de datos está lista.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(" ERROR DE CONEXIÓN: " + ex.Message);
            }
        }

        //  Método estático para obtener la instancia (Singleton)
        public static ConexionBaseDatos GetInstance()
        {
            if (instancia == null)
            {
                instancia = new ConexionBaseDatos();
            }
            return instancia;
        }

        //  Método auxiliar para  usar la conexión en las consultas
        public MySqlConnection GetConnection()
        {
            return connection;
        }
    }
}

