using SIVUG.Controllers;
using SIVUG.Models;
using SIVUG.Models.DAO;
using SIVUG.Models.DTOS;
using System;
using System.Collections.Generic;
using System.Drawing; // Necesario para diseñar la UI
using System.Windows.Forms;

namespace SIVUG
{
    public partial class FormRegistro : Form
    {
        // 1. DEPENDENCIAS DE NEGOCIO
        private EstudianteController _controller;
        private FacultadDAO _facultadDAO;
        private CarreraDAO _carreraDAO;

        // 2. COMPONENTES VISUALES (Declarados aquí porque no existe el Designer)
        // Campos de texto
        private TextBox txtNombres;
        private TextBox txtApellidos;
        private TextBox txtDNI;
        private TextBox txtEdad;
        private TextBox txtMatricula;
        private TextBox txtSemestre;

        // Listas desplegables
        private ComboBox cmbFacultad;
        private ComboBox cmbCarrera;

        // Botones
        private Button btnSave;
        private Button btnCancell;

        public FormRegistro()
        {
            // NOTA: No llamamos a InitializeComponent() porque estamos creando todo aquí.

            // Inicializamos lógica
            _controller = new EstudianteController();
            _facultadDAO = new FacultadDAO();
            _carreraDAO = new CarreraDAO();

            // Construimos la UI
            ConstruirComponentesDesdeCero();
        }

        private void FormRegistroEstudiantes_Load(object sender, EventArgs e)
        {
            CargarFacultades();
        }

        // --- MÉTODO PRINCIPAL DE DISEÑO DE UI ---
        private void ConstruirComponentesDesdeCero()
        {
            // Configuración de la Ventana
            this.Text = "Registro de Nuevo Estudiante";
            this.Size = new Size(750, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog; // Ventana fija (más profesional para modales)
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            // --- HEADER (ENCABEZADO) ---
            Panel panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(44, 62, 80) // Azul oscuro
            };
            this.Controls.Add(panelHeader);

            Label lblTitulo = new Label
            {
                Text = "Registro de Estudiante",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 15)
            };
            panelHeader.Controls.Add(lblTitulo);

            // --- PANEL CENTRAL (CONTENEDOR DE CAMPOS) ---
            Panel panelCuerpo = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30),
                BackColor = Color.White
            };
            this.Controls.Add(panelCuerpo);
            panelCuerpo.BringToFront(); // Asegurar que no quede tapado

            // Coordenadas para las columnas
            int col1X = 40;  // Columna Izquierda (Datos Personales)
            int col2X = 380; // Columna Derecha (Datos Académicos)
            int startY = 80; // Altura inicial
            int gap = 60;    // Espacio entre filas

            // Títulos de Sección
            CrearSubtitulo(panelCuerpo, "Datos Personales", col1X, startY - 30);
            CrearSubtitulo(panelCuerpo, "Datos Académicos", col2X, startY - 30);

            // --- COLUMNA 1: DATOS PERSONALES ---
            txtNombres = CrearCampoTexto(panelCuerpo, "Nombres:", col1X, startY);
            txtApellidos = CrearCampoTexto(panelCuerpo, "Apellidos:", col1X, startY + gap);
            txtDNI = CrearCampoTexto(panelCuerpo, "Cédula / DNI:", col1X, startY + (gap * 2));
            txtEdad = CrearCampoTexto(panelCuerpo, "Edad:", col1X, startY + (gap * 3), width: 80); // Campo corto

            // --- COLUMNA 2: DATOS ACADÉMICOS ---
            // Facultad (ComboBox especial con evento)
            Label lblFac = new Label { Text = "Facultad:", Location = new Point(col2X, startY), Font = new Font("Segoe UI", 9F), AutoSize = true };
            panelCuerpo.Controls.Add(lblFac);

            cmbFacultad = new ComboBox { Location = new Point(col2X, startY + 20), Size = new Size(280, 25), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F) };
            cmbFacultad.SelectedIndexChanged += cmbFacultad_SelectedIndexChanged_1; // Evento
            panelCuerpo.Controls.Add(cmbFacultad);

            // Carrera
            Label lblCar = new Label { Text = "Carrera:", Location = new Point(col2X, startY + gap), Font = new Font("Segoe UI", 9F), AutoSize = true };
            panelCuerpo.Controls.Add(lblCar);

            cmbCarrera = new ComboBox { Location = new Point(col2X, startY + gap + 20), Size = new Size(280, 25), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F) };
            panelCuerpo.Controls.Add(cmbCarrera);

            // Matrícula y Semestre
            txtMatricula = CrearCampoTexto(panelCuerpo, "N. Matrícula:", col2X, startY + (gap * 2));
            txtSemestre = CrearCampoTexto(panelCuerpo, "Semestre:", col2X, startY + (gap * 3), width: 80);

            // --- FOOTER (BOTONES) ---
            Panel panelBotones = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.FromArgb(245, 245, 245) // Gris claro
            };
            this.Controls.Add(panelBotones);

            // Botón Guardar
            btnSave = new Button
            {
                Text = "Guardar Registro",
                Size = new Size(160, 40),
                Location = new Point(540, 15), // Alineado a la derecha
                BackColor = Color.FromArgb(52, 152, 219), // Azul
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += btnSave_Click; // Conectar evento
            panelBotones.Controls.Add(btnSave);

            // Botón Cancelar
            btnCancell = new Button
            {
                Text = "Cancelar",
                Size = new Size(120, 40),
                Location = new Point(400, 15),
                BackColor = Color.White,
                ForeColor = Color.DimGray,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            btnCancell.FlatAppearance.BorderColor = Color.LightGray;
            btnCancell.Click += btnCancell_Click; // Conectar evento
            panelBotones.Controls.Add(btnCancell);

            // Evento Load del formulario
            this.Load += FormRegistroEstudiantes_Load;
        }

        // --- MÉTODOS AYUDANTES PARA CREAR CONTROLES RÁPIDO ---
        private void CrearSubtitulo(Panel p, string texto, int x, int y)
        {
            Label lbl = new Label
            {
                Text = texto,
                Location = new Point(x, y),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(127, 140, 141),
                AutoSize = true
            };
            p.Controls.Add(lbl);
        }

        private TextBox CrearCampoTexto(Panel p, string titulo, int x, int y, int width = 280)
        {
            Label lbl = new Label
            {
                Text = titulo,
                Location = new Point(x, y),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                AutoSize = true
            };
            p.Controls.Add(lbl);

            TextBox txt = new TextBox
            {
                Location = new Point(x, y + 20),
                Size = new Size(width, 25), // Altura estándar
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle
            };
            p.Controls.Add(txt);
            return txt;
        }


        // ==========================================
        //       LÓGICA DE NEGOCIO (INTACTA)
        // ==========================================

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Validación simple para evitar errores nulos
                if (string.IsNullOrWhiteSpace(txtNombres.Text) ||
                    string.IsNullOrWhiteSpace(txtApellidos.Text) ||
                    cmbCarrera.SelectedValue == null)
                {
                    MessageBox.Show("Por favor completa los campos obligatorios (Nombres, Apellidos, Carrera).",
                                    "Datos Incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Estudiante estudiante = new Estudiante();

                // Datos Persona
                estudiante.Nombres = txtNombres.Text;
                estudiante.Apellidos = txtApellidos.Text;
                estudiante.DNI = txtDNI.Text;
                estudiante.Edad = byte.Parse(txtEdad.Text);

                // Datos Estudiante
                estudiante.Matricula = txtMatricula.Text;
                int idCarreraSeleccionada = (int)cmbCarrera.SelectedValue;
                estudiante.IdCarrera = idCarreraSeleccionada;
                estudiante.Semestre = byte.Parse(txtSemestre.Text);

                if (_controller.Guardar(estudiante))
                {
                    MessageBox.Show("Guardado Exitosamente.", "SIVUG", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Verifica que EDAD y SEMESTRE sean números válidos.", "Error de Formato", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocurrió un error: " + ex.Message);
            }
        }

        private void btnCancell_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CargarFacultades()
        {
            List<Facultad> facultades = _facultadDAO.ObtenerTodas();
            cmbFacultad.DataSource = null;
            cmbFacultad.DisplayMember = "Nombre";
            cmbFacultad.ValueMember = "Id";
            cmbFacultad.DataSource = facultades;
        }

        private void CargarCarreras(int idFacultad)
        {
            List<Carrera> carreras = _carreraDAO.ObtenerPorIdFacultad(idFacultad);
            cmbCarrera.DataSource = null;
            cmbCarrera.DisplayMember = "Nombre";
            cmbCarrera.ValueMember = "Id";
            cmbCarrera.DataSource = carreras;
        }

        private void cmbFacultad_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (cmbFacultad.SelectedItem != null)
            {
                try
                {
                    // Intentamos obtener el ID de forma segura
                    // Dependiendo de cómo devuelve el objeto tu DAO, a veces es el objeto completo o el ValueMember
                    if (cmbFacultad.SelectedValue is int id)
                    {
                        CargarCarreras(id);
                    }
                    else if (cmbFacultad.SelectedItem is Facultad fac)
                    {
                        CargarCarreras(fac.Id);
                    }
                }
                catch
                {
                    // Manejo silencioso en carga inicial
                }
            }
        }
    }
}