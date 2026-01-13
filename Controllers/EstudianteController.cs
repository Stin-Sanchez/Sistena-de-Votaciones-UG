using SIVUG.Models;
using SIVUG.Models.DAO;
using SIVUG.Models.SERVICES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SIVUG.Controllers
{
    public  class EstudianteController
    {
        private EstudianteService _service;
        public EstudianteController()
        {
            _service = new EstudianteService();
        }

  
        public bool Guardar(Estudiante nuevoEstudiante)
        {
            try
            {
                _service.RegistrarEstudiante(nuevoEstudiante);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                return false;
            }
        }
    }
}

