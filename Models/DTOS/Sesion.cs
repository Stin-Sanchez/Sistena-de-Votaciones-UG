using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.DTOS
{
    /// <summary>
    /// GESTOR DE SESIÓN GLOBAL
    /// 
    /// Responsabilidades:
    /// - Almacenar Usuario autenticado (static)
    /// - Proporcionar acceso desde cualquier formulario
    /// - Validar estado de sesión
    /// - Facilitar acceso a datos del usuario (Estudiante, Rol, etc.)
    /// </summary>
    public static class Sesion
    {
        // ==================== PROPIEDADES ====================

        /// <summary>
        /// Usuario actualmente autenticado en la sesión.
        /// null si NO hay usuario logueado
        /// </summary>
        public static Usuario UsuarioActual { get; private set; }

        /// <summary>
        /// Acceso rápido al Estudiante logueado.
        /// Retorna: Estudiante si Persona es Estudiante, null si no
        /// </summary>
        public static Estudiante EstudianteLogueado
        {
            get
            {
                if (UsuarioActual?.Persona is Estudiante estudiante)
                    return estudiante;
                return null;
            }
        }

        /// <summary>
        /// Nombre completo del usuario logueado.
        /// </summary>
        public static string NombreCompleto
        {
            get
            {
                if (UsuarioActual == null)
                    return "";

                if (UsuarioActual.Persona != null)
                    return UsuarioActual.Persona.GetNombreCompleto();

                return UsuarioActual.NombreUsuario;
            }
        }

        /// <summary>
        /// Nombre del rol del usuario logueado.
        /// </summary>
        public static string NombreRol
        {
            get
            {
                if (UsuarioActual?.Rol == null)
                    return "";

                return UsuarioActual.Rol.Nombre;
            }
        }

        // ==================== MÉTODOS ====================

        /// <summary>
        /// Inicia la sesión con un usuario autenticado.
        /// </summary>
        public static void IniciarSesion(Usuario usuario)
        {
            if (usuario == null)
                throw new ArgumentNullException(nameof(usuario), "No se puede iniciar sesión con usuario nulo");

            UsuarioActual = usuario;
            System.Diagnostics.Debug.WriteLine($"[SESIÓN INICIADA] Usuario: {usuario.NombreUsuario} ({usuario.Rol?.Nombre})");
        }

        /// <summary>
        /// Cierra la sesión current.
        /// </summary>
        public static void CerrarSesion()
        {
            string usuarioAnterior = UsuarioActual?.NombreUsuario ?? "Desconocido";
            UsuarioActual = null;
            System.Diagnostics.Debug.WriteLine($"[SESIÓN CERRADA] Usuario: {usuarioAnterior}");
        }

        /// <summary>
        /// Verifica si hay una sesión activa.
        /// </summary>
        public static bool EstaLogueado()
        {
            return UsuarioActual != null;
        }

        /// <summary>
        /// ⭐ MÉTODO CRÍTICO: Valida que el usuario en sesión tenga un rol específico.
        /// 
        /// IMPORTANTE: Comparación case-INSENSITIVE (ignora mayúsculas/minúsculas)
        /// 
        /// Retorna:
        /// - true: Usuario tiene ese rol
        /// - false: Usuario no existe, no tiene rol, o el rol no coincide
        /// 
        /// Uso:
        /// if (Sesion.TieneRol("Administrador"))
        /// {
        ///     // Mostrar opciones de admin
        /// }
        /// </summary>
        public static bool TieneRol(string nombreRol)
        {
            //  VALIDACIÓN 1: Hay usuario logueado
            if (!EstaLogueado())
            {
                System.Diagnostics.Debug.WriteLine("[VERIFICACIÓN ROL] No hay usuario logueado");
                return false;
            }

            //  VALIDACIÓN 2: Nombre del rol no es nulo/vacío
            if (string.IsNullOrWhiteSpace(nombreRol))
            {
                System.Diagnostics.Debug.WriteLine("[VERIFICACIÓN ROL] Nombre de rol no especificado");
                return false;
            }

            //  VALIDACIÓN 3: Usuario tiene rol asignado
            if (UsuarioActual.Rol == null)
            {
                System.Diagnostics.Debug.WriteLine("[VERIFICACIÓN ROL] Usuario no tiene rol asignado");
                return false;
            }

            //  COMPARACIÓN: Case-insensitive para evitar errores
            bool tieneRol = UsuarioActual.Rol.Nombre.Equals(nombreRol, StringComparison.OrdinalIgnoreCase);

            System.Diagnostics.Debug.WriteLine(
                $"[VERIFICACIÓN ROL] Usuario: {UsuarioActual.NombreUsuario}, " +
                $"Rol Esperado: '{nombreRol}', " +
                $"Rol Actual: '{UsuarioActual.Rol.Nombre}', " +
                $"Resultado: {tieneRol}"
            );

            return tieneRol;
        }

        /// <summary>
        /// Valida que el usuario sea estudiante.
        /// </summary>
        public static bool EsEstudiante()
        {
            return EstudianteLogueado != null;
        }
    }
}
