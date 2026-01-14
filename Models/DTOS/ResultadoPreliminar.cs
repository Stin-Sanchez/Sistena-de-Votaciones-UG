using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.DTOS
{
    public class ResultadoPreliminarDTO
    {
        public int CandidataId { get; set; }
        public string Nombre { get; set; }
        public string FacultadNombre { get; set; }
        public int Votos { get; set; }
        public string UrlFoto { get; set; }
        public int Posicion { get; set; }

        public string TipoCandidatura { get; set; }
    }
}
