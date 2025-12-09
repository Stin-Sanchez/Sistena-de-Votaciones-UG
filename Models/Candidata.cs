using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models
{
    /* Representa una estudiante candidata en el concurso
    Hereda de Estudiante y agrega información específica del perfil de candidata */
    class Candidata : Estudiante
    {

       
        // Lista de pasatiempos e intereses de la candidata
     
        public List<string> Pasatiempos { get; set; } = new List<string>();

       
        // Habilidades y talentos destacados
     
        public List<string> Habilidades { get; set; } = new List<string>();

       
        // Aspiraciones profesionales y personales
        
        public List<string> Aspiraciones { get; set; } = new List<string>();

      
        // Ruta de la imagen principal/perfil de la candidata
       
        public string ImagenPrincipal { get; set; }

        //Constructor por defecto - inicializa listas vacías
        public Candidata() { }

        
        /// Crea una nueva candidata con su perfil completo
       
        public Candidata(string matricula, string nombres, string apellidos,
                         List<string> pasatiempos = null,
                         List<string> habilidades = null,
                         List<string> aspiraciones = null,
                         string imagenPrincipal = null)
            : base(matricula, "", 0, "") // Inicializa propiedades de Estudiante
        {
            this.Nombres = nombres;
            this.Apellidos = apellidos;
            this.Pasatiempos = pasatiempos ?? new List<string>();
            this.Habilidades = habilidades ?? new List<string>();
            this.Aspiraciones = aspiraciones ?? new List<string>();
            this.ImagenPrincipal = imagenPrincipal;
        }


    }
}
