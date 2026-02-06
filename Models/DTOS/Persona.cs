using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.DTOS
{
    namespace SIVUG.Models.DTOS
    {
        /// <summary>
        /// MODELO: Persona - Clase Base para Entidades Personales
        /// 
        /// Responsabilidades:
        /// - Representar atributos comunes a todas las personas en el sistema
        /// - Servir como clase base para herencia (Estudiante hereda de Persona)
        /// 
        /// ESTRUCTURA DE HERENCIA:
        /// Persona (base)
        ///   ├─ Estudiante
        ///   │   └─ Candidata
        ///   └─ Otros roles (Administrativo, etc.)
        /// 
        /// RELACIÓN BD:
        /// Tabla: personas (id_persona, dni, nombres, apellidos, edad)
        /// 
        /// NOTA: Esta clase NO tiene lógica de negocio.
        /// Solo almacena datos. La validación ocurre en Service.
        /// </summary>
        public class Persona
        {
            // ==================== PROPIEDADES ====================

            /// <summary>
            /// ID Único de la Persona en la Base de Datos
            /// PK en tabla personas
            /// Generado automáticamente (auto_increment)
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// Primer Nombre de la Persona
            /// Longitud esperada: 3-50 caracteres
            /// No puede estar vacío
            /// </summary>
            public string Nombres { get; set; }

            /// <summary>
            /// Apellidos de la Persona
            /// Puede contener uno o más apellidos separados por espacios
            /// Longitud esperada: 3-50 caracteres
            /// </summary>
            public string Apellidos { get; set; }

            /// <summary>
            /// Número de Identificación (Cédula/DNI)
            /// Identificador único nacional
            /// NO puede repetirse en la BD
            /// Formato: Depende del país (ej: 8 a 10 dígitos)
            /// </summary>
            public string DNI { get; set; }

            /// <summary>
            /// Edad de la Persona
            /// Tipo: byte (0-255)
            /// Validación: >= 17 años para estudiantes universitarios
            /// </summary>
            public byte Edad { get; set; }

            // ==================== CONSTRUCTORES ====================

            /// <summary>
            /// Constructor Vacío
            /// Usado por reflexión del ORM o inicialización manual
            /// </summary>
            /// 

            /// <summary>
            /// ID del Usuario dueño de este Perfil
            /// FK a tabla usuarios
            /// OBLIGATORIO - Una Persona SIEMPRE pertenece a UN Usuario
            /// 
            /// PATRÓN ONE-TO-ONE:
            /// - Una Persona tiene exactamente UN Usuario (padre)
            /// - Un Usuario puede tener UNA Persona (hijo)
            /// - El FK está en tabla personas (lado hijo)
            /// </summary>

            public int IdUsuario { get; set; }


            /// <summary>
            /// Objeto de Navegación: Usuario dueño de este Perfil
            /// Relación One-to-One (Esta Persona pertenece a UN Usuario)
            /// 
            /// IMPORTANTE: Este es el lado HIJO de la relación
            /// El Usuario es el padre (posee la Persona)
            /// 
            /// Usado para:
            /// - Acceder a credenciales desde datos personales
            /// - Ejemplo: persona.Usuario.NombreUsuario, persona.Usuario.Contraseña
            /// - Acceder al Rol del usuario
            /// - Ejemplo: persona.Usuario.Rol.Nombre
            /// </summary>
            public Usuario Usuario { get; set; }



            public Persona()
            {
            }

            /// <summary>
            /// Constructor Parametrizado Completo
            /// Inicializa todos los campos incluyendo usuario
            /// 
            /// Uso: Generalmente en DAO al mapear resultados de BD
            /// </summary>
            /// <param name="id">ID generado en BD</param>
            /// <param name="nombres">Nombre de la persona</param>
            /// <param name="apellidos">Apellidos de la persona</param>
            /// <param name="dNI">Número de identificación</param>
            /// <param name="edad">Edad actual</param>
            /// <param name="idUsuario">ID del usuario dueño de este perfil</param>
            public Persona(int id, string nombres, string apellidos, string dNI, byte edad, int idUsuario)
            {
                this.Id = id;
                Nombres = nombres;
                Apellidos = apellidos;
                DNI = dNI;
                Edad = edad;
                IdUsuario = idUsuario;
            }

            /// <summary>
            /// Constructor con Navegación Completa
            /// Inicializa datos personales e incluye usuario como objeto
            /// 
            /// Uso: Cuando se carga relación completa de BD
            /// </summary>
            /// <param name="id">ID de la persona</param>
            /// <param name="nombres">Nombre</param>
            /// <param name="apellidos">Apellidos</param>
            /// <param name="dNI">DNI</param>
            /// <param name="edad">Edad</param>
            /// <param name="idUsuario">ID del usuario asociado</param>
            /// <param name="usuario">Objeto Usuario completo</param>
            public Persona(int id, string nombres, string apellidos, string dNI, byte edad, int idUsuario, Usuario usuario)
            {
                this.Id = id;
                Nombres = nombres;
                Apellidos = apellidos;
                DNI = dNI;
                Edad = edad;
                IdUsuario = idUsuario;
                Usuario = usuario;
            }



            // ==================== MÉTODOS ====================

            /// <summary>
            /// Obtiene el nombre completo concatenando nombres y apellidos
            /// 
            /// Formato: "Nombres Apellidos"
            /// Uso: Mostrar en grillas, reportes, diálogos
            /// </summary>
            /// <returns>String con nombres y apellidos concatenados</returns>
            public string GetNombreCompleto()
            {
                return $"{Nombres} {Apellidos}";
            }

            /// <summary>
            /// Obtiene información completa incluyendo usuario
            /// Formato: "Nombres Apellidos (NombreUsuario)"
            /// </summary>
            /// <returns>String con datos personales y usuario</returns>
            public string GetInfoCompleta()
            {
                if (Usuario != null)
                {
                    return $"{GetNombreCompleto()} ({Usuario.NombreUsuario})";
                }
                return GetNombreCompleto();
            }
        }
    }
}
    
