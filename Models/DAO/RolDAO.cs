using MySql.Data.MySqlClient;
using SIVUG.Models.DTOS;
using SIVUG.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.DAO
{
    /// <summary>
    /// DAO: Acceso a Datos para Roles
    /// 
    /// Responsabilidades EXCLUSIVAS:
    /// - Operaciones CRUD en tabla 'roles'
    /// - Mapeo de resultados SQL a objetos C# (Rol)
    /// - Usar SIEMPRE parámetros @name para prevenir SQL Injection
    /// - Usar Singleton ConexionDB.GetInstance() para obtener conexión
    /// 
    /// IMPORTANTE: Este DAO NO contiene lógica de negocio.
    /// Toda validación debe estar en RolService
    /// </summary>
    public class RolDAO
    {
        public RolDAO() { }

        /// <summary>
        /// Obtiene TODOS los roles del sistema.
        /// 
        /// QUERY: SELECT * FROM roles
        /// ORDEN: Por nombre ascendente
        /// 
        /// Flujo de Mapeo:
        /// 1. Lee cada fila del resultado
        /// 2. Crea objeto Rol con ID y nombre
        /// 3. Agrega a lista
        /// 
        /// Retorna: Lista de roles (nunca nulo, vacío si falla)
        /// </summary>
        public List<Rol> ObtenerTodos()
        {
            List<Rol> lista = new List<Rol>();

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    string query = "sp_roles_obtener_todos";
                    

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;

                        using (var reader = cmd.ExecuteReader())

                        {
                            while (reader.Read())
                            {
                                Rol rol = new Rol
                                {
                                    IdRol = reader.GetInt32("id_rol"),
                                    Nombre = reader.GetString("nombre")
                                };

                                lista.Add(rol);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al obtener roles: {ex.Message}");
                }
            }

            return lista;
        }

        /// <summary>
        /// Obtiene UN rol específico por ID.
        /// 
        /// QUERY: SELECT * FROM roles WHERE id_rol = @IdRol
        /// 
        /// Retorna: Objeto Rol o null si no existe
        /// </summary>
        public Rol ObtenerPorId(int idRol)
        {
            string query = "SELECT id_rol, nombre FROM roles WHERE id_rol = @IdRol";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdRol", idRol);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Rol
                                {
                                    IdRol = reader.GetInt32("id_rol"),
                                    Nombre = reader.GetString("nombre")
                                };
                            }

                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al obtener rol por ID: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Obtiene UN rol específico por nombre.
        /// 
        /// QUERY: SELECT * FROM roles WHERE nombre = @Nombre
        /// 
        /// Validación: La búsqueda es case-sensitive según BD
        /// Retorna: Objeto Rol o null si no existe
        /// 
        /// Uso: Obtener ID de rol por nombre (ej: "Administrador")
        /// </summary>
        public Rol ObtenerPorNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return null;

            string query = "SELECT id_rol, nombre FROM roles WHERE nombre = @Nombre";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", nombre.Trim());
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Rol
                                {
                                    IdRol = reader.GetInt32("id_rol"),
                                    Nombre = reader.GetString("nombre")
                                };
                            }

                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al obtener rol por nombre: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Verifica si un rol ya existe por nombre.
        /// 
        /// VALIDACIÓN: Para evitar duplicados antes de insertar
        /// 
        /// Retorna: true si existe, false si no
        /// </summary>
        public bool ExistePorNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return false;

            string query = "SELECT COUNT(*) FROM roles WHERE nombre = @Nombre";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", nombre.Trim());
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al verificar existencia de rol: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// OPERACIÓN CRÍTICA: Inserta un nuevo rol en BD.
        /// 
        /// INSERT: INSERT INTO roles (nombre) VALUES (@Nombre)
        /// 
        /// El ID se genera automáticamente (auto_increment)
        /// 
        /// Retorna: true si se insertó exitosamente, false si algo falla
        /// </summary>
        public bool Insertar(Rol rol)
        {
            if (rol == null || string.IsNullOrWhiteSpace(rol.Nombre))
                return false;

            string query = "INSERT INTO roles (nombre) VALUES (@Nombre)";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", rol.Nombre.Trim());
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al insertar rol: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// OPERACIÓN CRÍTICA: Actualiza un rol existente.
        /// 
        /// UPDATE: UPDATE roles SET nombre = @Nombre WHERE id_rol = @IdRol
        /// 
        /// Validación: El rol debe existir previamente
        /// 
        /// Retorna: true si se actualizó exitosamente, false si algo falla
        /// </summary>
        public bool Actualizar(Rol rol)
        {
            if (rol == null || rol.IdRol <= 0 || string.IsNullOrWhiteSpace(rol.Nombre))
                return false;

            string query = "UPDATE roles SET nombre = @Nombre WHERE id_rol = @IdRol";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdRol", rol.IdRol);
                        cmd.Parameters.AddWithValue("@Nombre", rol.Nombre.Trim());
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al actualizar rol: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// OPERACIÓN CRÍTICA: Elimina un rol de la BD.
        /// 
        /// DELETE: DELETE FROM roles WHERE id_rol = @IdRol
        /// 
        /// ADVERTENCIA: Antes de eliminar, verificar que NO hay usuarios asignados
        /// Si hay usuarios con este rol, la operación fallará (FK constraint)
        /// 
        /// Retorna: true si se eliminó, false si algo falla (incluyendo FK violation)
        /// </summary>
        public bool Eliminar(int idRol)
        {
            if (idRol <= 0)
                return false;

            string query = "DELETE FROM roles WHERE id_rol = @IdRol";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdRol", idRol);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
                catch (MySqlException ex) when (ex.Number == 1451) // FK constraint error
                {
                    System.Diagnostics.Debug.WriteLine($"No se puede eliminar rol: hay usuarios asignados a este rol");
                    return false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar rol: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Obtiene cantidad de usuarios asignados a un rol.
        /// 
        /// QUERY: SELECT COUNT(*) FROM usuarios WHERE id_rol = @IdRol
        /// 
        /// Uso: Validar si se puede eliminar un rol
        /// Retorna: Número de usuarios, 0 si ninguno
        /// </summary>
        public int ObtenerCantidadUsuarios(int idRol)
        {
            if (idRol <= 0)
                return 0;

            string query = "SELECT COUNT(*) FROM usuarios WHERE id_rol = @IdRol";

            using (var conn = ConexionDB.GetInstance().GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IdRol", idRol);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al obtener cantidad de usuarios: {ex.Message}");
                    return 0;
                }
            }
        }
    }
}