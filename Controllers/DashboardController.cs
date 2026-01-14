using SIVUG.Models.DTOS;
using SIVUG.Models.SERVICES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SIVUG.Controllers
{
    public class DashboardController
    {
        private readonly VotacionService service;

        public DashboardController()
        {
            service = new VotacionService();
        }

        public DashboardDTO ObtenerDatosDashboard()
        {
            try
            {
                return service.ObtenerDatosDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar dashboard: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
    }
}
