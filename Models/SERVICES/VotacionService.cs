using SIVUG.Models.DAO;
using SIVUG.Models.DTOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.SERVICES
{
    public class VotacionService
    {
        private readonly VotoDAO _votoDAO;
        private readonly CarreraDAO _dashboardDAO; // Para el método del dashboard

        public VotacionService()
        {
            _votoDAO = new VotoDAO();
            _dashboardDAO = new CarreraDAO();
        }

        /* * Lógica de Negocio: Registrar Voto
         * 1. Recibe los objetos completos o IDs.
         * 2. Valida reglas (¿Ya votó?).
         * 3. Crea el objeto Voto.
         * 4. Llama al DAO.
         */
        public bool RegistrarVoto(Estudiante estudiante, Candidata candidata, TipoVoto tipo)
        {
            try
            {
                // REGLA DE NEGOCIO 1: Validar datos básicos
                if (estudiante == null || candidata == null)
                {
                    throw new ArgumentException("Datos de votación incompletos.");
                }

                // REGLA DE NEGOCIO 2: Verificar si la candidata está activa (opcional, pero recomendada)
                if (!candidata.Activa)
                {
                    throw new InvalidOperationException("No se puede votar por una candidata inactiva.");
                }

                // REGLA DE NEGOCIO 3: Verificar si el estudiante ya votó en esa categoría
                // Llamamos al DAO solo para consultar
                if (_votoDAO.YaVoto(estudiante.Id, tipo.ToString()))
                {
                    // Aquí decides: Retornar false o lanzar excepción. 
                    // Lanzar excepción suele ser más limpio para que la UI muestre el mensaje.
                    throw new InvalidOperationException($"El estudiante ya emitió su voto para {tipo}.");
                }

                // Si pasa las validaciones, construimos el objeto
                Voto nuevoVoto = new Voto(candidata, estudiante, tipo);

                // Llamamos al DAO para persistir
                _votoDAO.Insertar(nuevoVoto);

                return true;
            }
            catch (Exception ex)
            {
                // Aquí podrías loguear el error antes de re-lanzarlo
                Console.WriteLine("Error en servicio de votación: " + ex.Message);
                throw; // Re-lanzamos para que el Formulario muestre el mensaje de error
            }
        }

        // El método del dashboard se delega al DAO correcto
        public DashboardDTO ObtenerDatosDashboard()
        {
            return _dashboardDAO.ObtenerDatosDashboard();
        }
    }
}
