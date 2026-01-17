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
        public Comentario()
        {
        }

        public Comentario(string contenido, Estudiante estudiante, Foto fotoComentada)
        {
            // Validacion basica (opcional pero recomendada)
            if (string.IsNullOrWhiteSpace(contenido))
                throw new ArgumentException("El comentario no puede estar vacío.");

            this.Contenido = contenido;
            this.FechaComentario = DateTime.Now;

            // Asignamos las relaciones de objetos
            this.Estudiante = estudiante;
            this.FotoComentada = fotoComentada;

            // TRUCO PRO: Si los objetos ya tienen ID, asignamos las FK automáticamente
            if (estudiante != null) this.EstudianteId = estudiante.Id;
            if (fotoComentada != null) this.FotoId = fotoComentada.Id;
        }

        public long Id { get; set; }
        public string Contenido { get; set; }
        public DateTime FechaComentario { get; set; }

        // --- CLAVES FORÁNEAS (Muy útiles para SQL) ---
        public long EstudianteId { get; set; }
        public long FotoId { get; set; }

        // --- RELACIONES DE NAVEGACIÓN ---
        public Estudiante Estudiante { get; set; }
        public Foto FotoComentada { get; set; }
    }
}

