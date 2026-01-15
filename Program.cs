using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SIVUG.Models;
using SIVUG.View;

namespace SIVUG
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Candidata candidata= new Candidata();
            candidata.id = 1;
            candidata.DNI = "0969561832";
            candidata.Nombres = "Lana Rohaes";


            Application.Run(new FormDashboard());
        }
    }
}
