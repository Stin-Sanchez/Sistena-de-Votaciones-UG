    using SIVUG.Models.DAO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.SERVICES
{
    /// <summary>
    /// SERVICIO: Gestión de Lógica de Negocio para Candidatas
    /// 
    /// Responsabilidades:
    /// - Validar reglas de negocio (edad, nombres, unicidad de candidatura)
    /// - Verificar que solo exista UNA candidata activa por estudiante
    /// - Validar cambios de datos (NO permitir cambio de tipo de candidatura)
    /// - Orquestar operaciones entre múltiples DAOs
    /// 
    /// NOTA: Este servicio NO accede directamente a la BD,
    /// todas las operaciones se delegan al CandidataDAO
    /// </summary>
    public class CandidataService
    {
        private readonly CandidataDAO _dao;

        // ==================== CONSTANTES DE VALIDACIÓN ====================
        private const int EDAD_MINIMA = 18;
        private const int EDAD_MAXIMA = 60;
        private const int LONGITUD_MINIMA_NOMBRES = 3;
        private const int LONGITUD_MAXIMA_NOMBRES = 100;

        public CandidataService()
        {
            _dao = new CandidataDAO();
        }

        // ==================== MÉTODOS DE CONSULTA ====================

        /// <summary>
        /// Verifica si un estudiante ya está registrado como candidata activa.
        /// 
        /// REGLA DE NEGOCIO CRÍTICA: 
        /// Solo puede haber UNA candidata por estudiante en estado ACTIVO.
        /// Esta validación es fundamental para la integridad del sistema de votación.
        /// 
        /// Flujo:
        /// 1. Valida que el ID sea válido (> 0)
        /// 2. Consulta CandidataDAO.ExisteCandidataActivaPorEstudiante()
        /// 3. Retorna true si existe, false si no
        /// </summary>
        public bool EsCandidataActiva(int estudianteId)
        {
            if (estudianteId <= 0)
                return false;

            return _dao.ExisteCandidataActivaPorEstudiante(estudianteId);
        }

        /// <summary>
        /// Obtiene TODAS las candidatas activas del sistema.
        /// 
        /// Uso: Llenar grillas, combobox, listado general
        /// Nota: Solo retorna candidatas con activa = 1 (Soft Delete)
        /// 
        /// Manejo de errores: Si la BD falla, retorna lista vacía (nunca nula)
        /// </summary>
        public List<Candidata> ObtenerCandidatasActivas()
        {
            try
            {
                var candidatas = _dao.ObtenerActivas();
                return candidatas ?? new List<Candidata>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener candidatas activas: {ex.Message}");
                return new List<Candidata>();
            }
        }

        /// <summary>
        /// Busca una candidata por su ID.
        /// 
        /// Uso: Abrir detalles, editar, eliminar una candidata específica
        /// Valida: ID debe ser > 0, retorna null si no existe
        /// </summary>
        public Candidata ObtenerCandidataPorId(int candidataId)
        {
            if (candidataId <= 0)
                return null;

            try
            {
                return _dao.ObtenerPorId(candidataId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener candidata por ID: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Recupera candidata usando ID de estudiante (ID de usuario).
        /// 
        /// PATRÓN CRÍTICO: Se usa DESPUÉS de insertar candidata nueva.
        /// Flujo:
        /// 1. Insertar en tabla CANDIDATAS (genera ID AUTO_INCREMENT)
        /// 2. Usar ObtenerPorIdUsuario(idEstudiante) para recuperar el ID generado
        /// 3. Usar ese ID para guardar detalles en CANDIDATA_DETALLES
        /// 
        /// Esto es necesario porque el ID se genera automáticamente en BD
        /// y C# no tiene forma de obtenerlo después de INSERT.
        /// </summary>
        public Candidata ObtenerCandidataPorIdUsuario(int idUsuario)
        {
            if (idUsuario <= 0)
                return null;

            try
            {
                return _dao.ObtenerPorIdUsuario(idUsuario);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener candidata por ID usuario: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Filtra candidatas activas por tipo (Reina O Fotogenia).
        /// 
        /// Uso: Mostrar solo candidatas de una categoría específica
        /// Nota: Usa LINQ in-memory para filtrar (no SQL)
        /// </summary>
        public List<Candidata> ObtenerCandidatasPorTipo(TipoVoto tipoVoto)
        {
            try
            {
                var candidatas = ObtenerCandidatasActivas();
                return candidatas
                    .Where(c => c.tipoCandidatura == tipoVoto)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener candidatas por tipo: {ex.Message}");
                return new List<Candidata>();
            }
        }

        // ==================== MÉTODOS DE REGISTRO ====================

        /// <summary>
        /// OPERACIÓN CRÍTICA: Registra una nueva candidata en el sistema.
        /// 
        /// VALIDACIONES (en este orden):
        /// 1. ✓ ID de estudiante válido (> 0)
        /// 2. ✓ Objeto Candidata no es nulo
        /// 3. ✓ Estudiante NO es candidata ya registrada
        /// 4. ✓ Tipo de candidatura es válido (Reina O Fotogenia, no ambos)
        /// 
        /// Flujo de datos:
        /// 1. Valida todas las reglas de negocio
        /// 2. Llama CandidataDAO.InsertarCandidato()
        /// 3. Lanza excepciones si algo falla (no retorna false)
        /// 
        /// Nota: El ID se genera en BD automáticamente.
        /// Debe recuperarse luego con ObtenerPorIdUsuario()
        /// </summary>
        public bool RegistrarCandidato(int estudianteId, Candidata candidata)
        {
            // Validación 1: IDs válidos
            if (estudianteId <= 0)
                throw new ArgumentException("ID de estudiante inválido", nameof(estudianteId));

            if (candidata == null)
                throw new ArgumentNullException(nameof(candidata), "Los datos de la candidata no pueden ser nulos");

            // Validación 2: No debe existir ya como candidata activa (REGLA DE NEGOCIO)
            if (EsCandidataActiva(estudianteId))
                throw new InvalidOperationException("El estudiante ya está registrado como candidata activa");

            // Validación 3: Tipo de candidatura válido
            if (!ValidarTipoCandidatura(candidata.tipoCandidatura))
                throw new ArgumentException("Tipo de candidatura inválido. Debe ser Reina o Fotogenia");

            try
            {
                bool registroExitoso = _dao.InsertarCandidato(estudianteId, candidata);

                if (!registroExitoso)
                    throw new InvalidOperationException("No se pudo completar el registro en la base de datos");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al registrar candidata: {ex.Message}");
                throw new InvalidOperationException($"Error durante el registro: {ex.Message}", ex);
            }
        }

        // ==================== MÉTODOS DE ACTUALIZACIÓN ====================

        /// <summary>
        /// OPERACIÓN CRÍTICA: Actualiza datos de candidata existente.
        /// 
        /// VALIDACIONES:
        /// 1. ✓ Candidata existe en BD
        /// 2. ✓ Nombres y apellidos válidos (3-100 caracteres)
        /// 3. ✓ Edad entre 18 y 60 años
        /// 4. ✓ Tipo de candidatura válido
        /// 5. ✓ NO PERMITIR cambio de tipo de candidatura (REGLA DE NEGOCIO)
        /// 
        /// RESTRICCIÓN IMPORTANTE: No se puede cambiar tipo porque:
        /// - Afectaría la integridad del sistema de votación
        /// - Requeriría auditoría y validaciones complejas
        /// - Violaría reglas de negocio del concurso
        /// 
        /// Datos permitidos cambiar:
        /// ✓ Nombres, Apellidos
        /// ✓ Edad
        /// ✓ Foto
        /// ✗ Tipo de candidatura
        /// </summary>
        public bool ActualizarCandidata(Candidata candidata)
        {
            if (candidata == null)
                throw new ArgumentNullException(nameof(candidata), "La candidata no puede ser nula");

            // Validación: ID válido
            if (candidata.CandidataId <= 0)
                throw new ArgumentException("ID de candidata inválido", nameof(candidata));

            // Validación: Candidata existe
            var candidataExistente = ObtenerCandidataPorId(candidata.CandidataId);
            if (candidataExistente == null)
                throw new InvalidOperationException($"No existe candidata con ID {candidata.CandidataId}");

            // Validar datos de Persona
            ValidarDatosPersona(candidata);

            // Validar edad
            if (!ValidarEdad(candidata.Edad))
                throw new ArgumentException($"La edad debe estar entre {EDAD_MINIMA} y {EDAD_MAXIMA} años");

            // Validar tipo de candidatura
            if (!ValidarTipoCandidatura(candidata.tipoCandidatura))
                throw new ArgumentException("Tipo de candidatura inválido. Debe ser Reina o Fotogenia");

            // VALIDACIÓN CRÍTICA: NO permitir cambio de tipo
            if (candidataExistente.tipoCandidatura != candidata.tipoCandidatura)
                throw new InvalidOperationException("No se puede cambiar el tipo de candidatura de una candidata ya registrada");

            try
            {
                bool actualizacionExitosa = _dao.ActualizarCandidata(candidata);

                if (!actualizacionExitosa)
                    throw new InvalidOperationException("No se pudo completar la actualización en la base de datos");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar candidata: {ex.Message}");
                throw new InvalidOperationException($"Error durante la actualización: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cambia el estado ACTIVO/INACTIVO de una candidata.
        /// 
        /// PATRÓN SOFT DELETE:
        /// - NO elimina registros de BD físicamente
        /// - Solo actualiza columna "activa" (1 = activa, 0 = inactiva)
        /// - Preserva historial y auditoría de datos
        /// - Permite "recuperar" candidatas si es necesario
        /// 
        /// Validación: Candidata debe existir en BD
        /// </summary>
        public bool ActualizarEstadoCandidata(int candidataId, bool activa)
        {
            if (candidataId <= 0)
                throw new ArgumentException("ID de candidata inválido", nameof(candidataId));

            var candidata = ObtenerCandidataPorId(candidataId);
            if (candidata == null)
                throw new InvalidOperationException($"No existe candidata con ID {candidataId}");

            try
            {
                bool actualizacionExitosa = _dao.ActualizarEstadoCandidato(candidataId, activa);

                if (!actualizacionExitosa)
                    throw new InvalidOperationException("No se pudo completar la actualización de estado");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar estado de candidata: {ex.Message}");
                throw new InvalidOperationException($"Error durante la actualización de estado: {ex.Message}", ex);
            }
        }

        // ==================== MÉTODOS DE VALIDACIÓN PRIVADOS ====================

        /// <summary>
        /// Valida que nombres y apellidos cumplan con requisitos.
        /// 
        /// Reglas:
        /// - No pueden estar vacíos
        /// - Mínimo 3 caracteres
        /// - Máximo 100 caracteres
        /// 
        /// Lanza excepciones si no cumple (no retorna bool)
        /// </summary>
        private void ValidarDatosPersona(Candidata candidata)
        {
            // Validar nombres
            if (string.IsNullOrWhiteSpace(candidata.Nombres))
                throw new ArgumentException("El nombre no puede estar vacío");

            string nombresLimpio = candidata.Nombres.Trim();
            if (nombresLimpio.Length < LONGITUD_MINIMA_NOMBRES)
                throw new ArgumentException($"El nombre debe tener al menos {LONGITUD_MINIMA_NOMBRES} caracteres");

            if (nombresLimpio.Length > LONGITUD_MAXIMA_NOMBRES)
                throw new ArgumentException($"El nombre no debe exceder {LONGITUD_MAXIMA_NOMBRES} caracteres");

            // Validar apellidos
            if (string.IsNullOrWhiteSpace(candidata.Apellidos))
                throw new ArgumentException("El apellido no puede estar vacío");

            string apellidosLimpio = candidata.Apellidos.Trim();
            if (apellidosLimpio.Length < LONGITUD_MINIMA_NOMBRES)
                throw new ArgumentException($"El apellido debe tener al menos {LONGITUD_MINIMA_NOMBRES} caracteres");

            if (apellidosLimpio.Length > LONGITUD_MAXIMA_NOMBRES)
                throw new ArgumentException($"El apellido no debe exceder {LONGITUD_MAXIMA_NOMBRES} caracteres");
        }

        /// <summary>
        /// Valida que la edad esté en rango permitido (18-60 años).
        /// Retorna bool (true/false), no lanza excepciones.
        /// </summary>
        private bool ValidarEdad(byte edad)
        {
            return edad >= EDAD_MINIMA && edad <= EDAD_MAXIMA;
        }

        /// <summary>
        /// Valida que el tipo de candidatura sea válido.
        /// 
        /// Reglas:
        /// - Debe ser valor válido del enum TipoVoto
        /// - Solo Reina (1) O Fotogenia (2), nunca ambos
        /// - No puede ser null o indefinido
        /// </summary>
        private bool ValidarTipoCandidatura(TipoVoto tipo)
        {
            return Enum.IsDefined(typeof(TipoVoto), tipo) && 
                   (tipo == TipoVoto.Reina || tipo == TipoVoto.Fotogenia);
        }

        /// <summary>
        /// Valida ruta de imagen (si se proporciona).
        /// 
        /// Reglas:
        /// - La imagen es OPCIONAL
        /// - Si se proporciona, no debe contener caracteres inválidos
        /// 
        /// Retorna bool, no lanza excepciones
        /// </summary>
        private bool ValidarRutaImagen(string rutaImagen)
        {
            if (string.IsNullOrEmpty(rutaImagen))
                return true; // Imagen es opcional

            try
            {
                char[] caracteresInvalidos = System.IO.Path.GetInvalidPathChars();
                return !rutaImagen.Any(c => caracteresInvalidos.Contains(c));
            }
            catch
            {
                return false;
            }
        }

        // ==================== MÉTODOS AUXILIARES ====================

        /// <summary>
        /// Obtiene estadísticas generales de candidatas.
        /// Útil para dashboards y reportes.
        /// 
        /// Retorna objeto anónimo con:
        /// - TotalCandidatas
        /// - CandidatasReina
        /// - CandidatasFotogenia
        /// - FechaActualizacion
        /// </summary>
        public dynamic ObtenerEstadisticas()
        {
            try
            {
                var candidatas = ObtenerCandidatasActivas();

                return new
                {
                    TotalCandidatas = candidatas.Count,
                    CandidatasReina = candidatas.Count(c => c.tipoCandidatura == TipoVoto.Reina),
                    CandidatasFotogenia = candidatas.Count(c => c.tipoCandidatura == TipoVoto.Fotogenia),
                    FechaActualizacion = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener estadísticas: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Valida TODOS los datos de candidata antes de guardar.
        /// 
        /// Uso: Prevalidación integral antes de guardar en BD
        /// Retorna bool (true si TODO es válido, false si algo falla)
        /// 
        /// Valida:
        /// ✓ Objeto no nulo
        /// ✓ Datos de persona válidos
        /// ✓ Edad válida
        /// ✓ Tipo de candidatura válido
        /// ✓ Ruta de imagen válida
        /// </summary>
        public bool ValidarCandidataCompleta(Candidata candidata)
        {
            if (candidata == null)
                return false;

            try
            {
                ValidarDatosPersona(candidata);
                
                if (!ValidarEdad(candidata.Edad))
                    return false;

                if (!ValidarTipoCandidatura(candidata.tipoCandidatura))
                    return false;

                if (!ValidarRutaImagen(candidata.ImagenPrincipal))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Convierte enum TipoVoto a descripción legible para UI.
        /// 
        /// Uso: Mostrar tipo en grillas, diálogos, reportes
        /// Ejemplo: TipoVoto.Reina → "Candidata a Reina"
        /// </summary>
        public string ObtenerDescripcionTipoVoto(TipoVoto tipoVoto)
        {
            switch (tipoVoto)
            {
                case TipoVoto.Reina:
                    return "Candidata a Reina";
                case TipoVoto.Fotogenia:
                    return "Candidata a Fotogenia";
                default:
                    return "Tipo de candidatura desconocido";
            }
        }
    }
}
