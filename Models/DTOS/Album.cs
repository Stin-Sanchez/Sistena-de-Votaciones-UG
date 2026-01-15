using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models
{
    // Representa un álbum de fotos de una candidata
   public  class Album
    {
        //Identificador único del álbum
        public long Id { get; set; }

        //Título del álbum (ej: "Fotos oficiales", "Sesión de playa"
        public string Titulo { get; set; }

        /*Relacion Many to one con candidata
         * Muchos albunes le pertenecen a una sola candidata
         */
        public Candidata Candidata { get; set; }

        //Descripción del contenido del álbum
        public string Descripcion { get; set; }

        public DateTime FechaCreacion { get; set; }

        /*
          Factory method que crea una nueva Foto asociada a este álbum.
          NO valida ni persiste datos - eso es responsabilidad de la capa de servicio.
         
         <param name="rutaArchivo">Ruta física o URL donde se almacena la imagen</param>
         <param name="descripcion">Descripción o pie de foto (opcional)</param>
         <returns>Nuevo objeto Foto sin persistir</returns>

        NOTA IMPORTANTE:
            La capa de servicio debe:
            1. Validar que el archivo existe y es una imagen válida
            2. Verificar el tamaño y formato del archivo
            3. Persistir la foto en BD
            4. Opcionalmente: validar límite de fotos por álbum
            5. Opcionalmente: generar thumbnails o procesar la imagenmodels
         * 
         */

        public Foto AgregarFoto(string rutaArchivo, string descripcion = "")
        {
            return new Foto
            {
                RutaArchivo = rutaArchivo,
                Descripcion = descripcion,
                Album = this  // Asocia automáticamente esta foto al álbum actual
            };


        }
    }
}
