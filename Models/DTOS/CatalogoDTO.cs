using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.DTOS
{
    public  class CatalogoDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Tipo { get; set; } // "HABILIDAD", "PASATIEMPO", "ASPIRACION"
    }
}
