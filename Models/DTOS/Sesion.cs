using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIVUG.Models.DTOS
{
    public static class Sesion
    {
        // Aquí guardaremos al estudiante una vez se loguee
        public static Estudiante UsuarioLogueado { get; private set; }

        public static void IniciarSesion(Estudiante estudiante)
        {
            UsuarioLogueado = estudiante;
        }

        public static void CerrarSesion()
        {
            UsuarioLogueado = null;
        }

        // Helper para verificar rápido si hay alguien logueado
        public static bool EstaLogueado()
        {
            return UsuarioLogueado != null;
        }
    }
}
