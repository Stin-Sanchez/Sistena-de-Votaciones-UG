using SIVUG.Controllers;
using SIVUG.Models;
using SIVUG.Models.DAO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SIVUG
{
    public partial class FormRegistro : Form
    {
        private EstudianteController _controller;
        public FormRegistro()
        {
            InitializeComponent();
            _controller = new EstudianteController();
        }

        private void FormRegistroEstudiantes_Load(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Instanciamos directamente el DTO
                Estudiante estudiante = new Estudiante();

                // Llenamos datos de Persona (Heredados)
                estudiante.Nombres = txtNombres.Text;
                estudiante.Apellidos = txtApellidos.Text;
                estudiante.DNI = txtDNI.Text;
                estudiante.Edad = byte.Parse(txtEdad.Text);

                // Llenamos datos de Estudiante
                estudiante.Matricula = txtMatricula.Text;
                estudiante.Carrera = txtCarrera.Text;
                estudiante.Semestre = byte.Parse(txtSemestre.Text);
                estudiante.Facultad = txtFacultad.Text;

                // Enviamos al Controller
                if (_controller.Guardar(estudiante))
                {
                    MessageBox.Show("Guardado Exitosamente.");
                    // Aquí el objeto 'estudiante' ya tiene el ID cargado si lo necesitas para algo más
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Verifica que Edad y Semestre sean números.");
            }
        }
    
        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void btnCancell_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
