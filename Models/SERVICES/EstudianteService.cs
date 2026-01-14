using SIVUG.Models.DAO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SIVUG.Models.SERVICES
{
    public class EstudianteService
    {

        private EstudianteDAO _dao;

        public EstudianteService()
        {
            _dao = new EstudianteDAO();
        }

        public void RegistrarEstudiante(Estudiante estudiante)
        {
            // Validaciones de Negocio
            if (estudiante.Edad < 17)
                throw new Exception("El estudiante es menor de edad para la universidad.");

            if (string.IsNullOrEmpty(estudiante.DNI))
                throw new Exception("El DNI es obligatorio.");

            // Sin mapeos: Pasamos el DTO directo al DAO
            _dao.Guardar(estudiante);
        }

        public Estudiante ValidarEstudiante(string cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula))
                throw new ArgumentException("La cédula es requerida");

            return _dao.ObtenerPorCedula(cedula);
        }
    }
}
