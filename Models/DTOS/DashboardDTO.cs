using SIVUG.Models.DAO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.DTOS
{
    public class DashboardDTO
    {
        public int TotalEstudiantes { get; set; }
        public int VotosEmitidos { get; set; }
        public decimal PorcentajeVotacion { get; set; }
        public int CandidatasActivas { get; set; }
        public List<Facultad> ProgresoFacultades { get; set; }
        public List<ResultadoPreliminarDTO> Top3Candidatas { get; set; }
    }
}
