using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models
{
    // Representa una fotografía dentro de un álbum de candidata
    public class Foto
    {
       
        //Identificador único de la foto(generado por BD)
        public long Id { get; set; }

        //Ruta física o URL donde se almacena la imagen
        public string RutaArchivo { get; set; }

        //Descripción o pie de foto
        public string Descripcion { get; set; }

        public DateTime FechaSubida { get; set; }

        /*
         *Relacion Many to One con Albun
         *Muchas fotos podrian estar  en un solo album
         */
        public Album Album { get; set; }

        //Constructor por defecto
        public Foto()
        {
        }

        //Constructor parametrizado
        public Foto(string rutaArchivo, string descripcion, Album album)
        {
            RutaArchivo = rutaArchivo;
            Descripcion = descripcion;
            Album = album;
        }

    }
}
