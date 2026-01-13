using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models
{
    // Representa un comentario realizado por un estudiante en una foto
   public class Comentario
    {
        //Constructor por defecto
        public Comentario()
        {
        }

        //Constructor parametrizado 
        public Comentario( string contenido, Estudiante estudiante, Foto fotoComentada)
        {
        
            this.Contenido = contenido;
            this.FechaComentario = DateTime.Now;
            this.Estudiante = estudiante;
            this.FotoComentada = fotoComentada;
        }
        //Identificador único del comentario(generado por BD)
        public long Id { get; set; }

        //Texto del comentario
        public string Contenido { get; set; }

        //Fecha y hora en el que se publico el comentario
        public DateTime FechaComentario { get; set; }

        /*
         * Relacion Many to One con estudiante 
         * Un estudiante puede comentar N comentarios , y dichos comentarios
         * le pertenecen a un solo estudiante
         */
        public Estudiante Estudiante { get; set; }

        /*
         * Relacion Many to One con Foto 
         * Una foto puede ser comentada N veces y dichos comentarios
         * le pertenecen unicamente a dicha foto 
         */
        public Foto FotoComentada { get; set; }
    }
}
