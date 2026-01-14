using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.DTOS
{
    public class Facultad
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int VotosEmitidos { get; set; }
        public decimal PorcentajeParticipacion { get; set; }
        public int TotalEstudiantes { get; set; }

        public override string ToString()
        {
            return Nombre; // Esto ayuda a que el ComboBox muestre el nombre y no "SIVUG.Models.Facultad"
        }
    }
}
