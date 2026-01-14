using SIVUG.Models.DAO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.SERVICES
{
    // ========== SERVICIO DE CANDIDATAS (Agregar a la capa SERVICES) ==========
    public class CandidataService
    {
        private readonly CandidataDAO dao;

        public CandidataService()
        {
            dao = new CandidataDAO();
        }

        public bool EsCandidataActiva(int estudianteId)
        {
            return dao.ExisteCandidataActivaPorEstudiante(estudianteId);
        }

        public bool RegistrarCandidato(int estudianteId, Candidata candidata)
        {
            return dao.InsertarCandidato(estudianteId, candidata);
        }

        public List<Candidata> ObtenerCandidatasActivas()
        {
            return dao.ObtenerActivas();
        }
    }

}
