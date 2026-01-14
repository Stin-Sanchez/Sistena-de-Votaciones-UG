using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.DTOS
{
    public class Carrera
    {
        public int Id { get; set; }
        public string Nombre { get; set; }

        // Relación: FK hacia Facultad
        public int IdFacultad { get; set; }

        // (Opcional) Objeto de navegación si quisieras acceder a datos de la facultad
        public Facultad Facultad { get; set; }

        public override string ToString()
        {
            return Nombre;
        }
    }
}

