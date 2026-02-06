using SIVUG.Controllers;
using SIVUG.Models;
using SIVUG.Models.DAO;
using SIVUG.Models.DTOS;
using System;
using System.Collections.Generic;
using System.Drawing; // Necesario para diseñar la UI
using System.IO;
using System.Windows.Forms;

namespace SIVUG
{
    /// <summary>
    /// CONTROLADOR DE REGISTRO.
    /// Pantalla de "Alta" para nuevos usuarios del sistema.
    /// Resuelve el flujo de creación de usuario + perfil de estudiante en transacción.
    /// </summary>
    public partial class FormRegistro : Form
    {
        // 1. INYECCIÓN DE DEPENDENCIAS (SERVICES / DAOS)
        private EstudianteController _controller;
        private FacultadDAO _facultadDAO;
        private CarreraDAO _carreraDAO;

        // 2. CONTROLES DE FORMULARIO (Mapeo 1:1 con la BD)
        // Campos de texto para datos planos.
        private TextBox txtNombres;
        private TextBox txtApellidos;
        private TextBox txtDNI;
        private TextBox txtEdad;
        private TextBox txtMatricula;
        private TextBox txtSemestre;

        // Selectores para claves foráneas (FK).
        private ComboBox cmbFacultad;
        private ComboBox cmbCarrera;

        // Componentes para gestión de imagen.
        private PictureBox pbFotoPerfil;
        private Button btnSubirFoto;
        private string _rutaFotoSeleccionada = null; // Ruta temporal del archivo.

        // Botones de control de flujo.
        private Button btnSave;
        private Button btnCancell;

        public FormRegistro()
        {
            _controller = new EstudianteController();
            _facultadDAO = new FacultadDAO();
            _carreraDAO = new CarreraDAO();

            // Lógica PURA de UI: No uso el designer, lo hago a mano
            // para tener un layout flexible y moderno.
            ConstruirComponentesDesdeCero();
        }
                
        private void FormRegistroEstudiantes_Load(object sender, EventArgs e)
        {
            CargarFacultades();
            
        }

        /// <summary>
        /// CONSTRUCCIÓN VISUAL.
        /// Método monolítico que dibuja toda la interfaz siguiendo un grid lógico.
        /// </summary>
        private void ConstruirComponentesDesdeCero()
        {
            // --- CONFIGURACIÓN DE VENTANA ---
            this.Text = "Registro de Nuevo Estudiante";
            this.Size = new Size(900, 500); // Dimensiones "Wide" para acomodar foto + inputs.
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog; // Diálogo modal tradicional.
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            // --- HEADER INSTITUCIONAL ---
            Panel panelHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(44, 62, 80) };
            this.Controls.Add(panelHeader);
            Label lblTitulo = new Label { Text = "Registro de Estudiante", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(25, 15) };
            panelHeader.Controls.Add(lblTitulo);

            // --- CONTENEDOR PRINCIPAL (CANVAS) ---
            Panel panelCuerpo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.White };
            this.Controls.Add(panelCuerpo);
            panelCuerpo.BringToFront();

            // =========================================================
            //  GRID DE INPUTS (SECCIÓN IZQUIERDA)
            // =========================================================

            // Variables de layout para fácil mantenimiento (cambias uno, ajustas todo).
            int col1_X = 30;   
            int col2_X = 300;  
            int start_Y = 30;  
            int gap_Y = 70;    
            int width_Input = 240; 

            // --- FILA 1: IDENTIDAD BÁSICA ---
            txtNombres = CrearCampoTexto(panelCuerpo, "Nombres:", col1_X, start_Y, width_Input);
            txtApellidos = CrearCampoTexto(panelCuerpo, "Apellidos:", col2_X, start_Y, width_Input);

            // --- FILA 2: IDENTIDAD LEGAL ---
            txtDNI = CrearCampoTexto(panelCuerpo, "Cédula / DNI:", col1_X, start_Y + gap_Y, width_Input);
            // Edad: Input más corto visualmente.
            txtEdad = CrearCampoTexto(panelCuerpo, "Edad:", col2_X, start_Y + gap_Y, 80);

            // --- FILA 3: DATOS ACADÉMICOS (Selectores) ---
            // Facultad
            Label lblFac = new Label { Text = "Facultad:", Location = new Point(col1_X, start_Y + (gap_Y * 2)), Font = new Font("Segoe UI", 9F), AutoSize = true };
            panelCuerpo.Controls.Add(lblFac);
            cmbFacultad = new ComboBox { Location = new Point(col1_X, start_Y + (gap_Y * 2) + 20), Size = new Size(width_Input, 25), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F) };
            cmbFacultad.SelectedIndexChanged += cmbFacultad_SelectedIndexChanged_1; // Evento de cascada.
            panelCuerpo.Controls.Add(cmbFacultad);

            // Carrera
            Label lblCar = new Label { Text = "Carrera:", Location = new Point(col2_X, start_Y + (gap_Y * 2)), Font = new Font("Segoe UI", 9F), AutoSize = true };
            panelCuerpo.Controls.Add(lblCar);
            cmbCarrera = new ComboBox { Location = new Point(col2_X, start_Y + (gap_Y * 2) + 20), Size = new Size(width_Input, 25), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F) };
            panelCuerpo.Controls.Add(cmbCarrera);

            // --- FILA 4: DETALLES MATRÍCULA ---
            txtMatricula = CrearCampoTexto(panelCuerpo, "N. Matrícula:", col1_X, start_Y + (gap_Y * 3), width_Input);
            txtSemestre = CrearCampoTexto(panelCuerpo, "Semestre:", col2_X, start_Y + (gap_Y * 3), 80);


            // =========================================================
            //  SECCIÓN DE FOTO (COLUMNA DERECHA INDEPENDIENTE)
            // =========================================================
            int foto_X = 600; 
            int foto_Y = 30;  

            Label lblFoto = new Label { Text = "Foto de Perfil:", Location = new Point(foto_X, foto_Y - 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.Gray, AutoSize = true };
            panelCuerpo.Controls.Add(lblFoto);

            pbFotoPerfil = new PictureBox
            {
                Location = new Point(foto_X, foto_Y),
                Size = new Size(200, 220), 
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle
            };
            panelCuerpo.Controls.Add(pbFotoPerfil);

            btnSubirFoto = new Button
            {
                Text = "Seleccionar Foto...",
                Location = new Point(foto_X, foto_Y + 230), 
                Size = new Size(200, 35),
                BackColor = Color.FromArgb(236, 240, 241),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            btnSubirFoto.Click += BtnSubirFoto_Click;
            panelCuerpo.Controls.Add(btnSubirFoto);


            // =========================================================
            //  FOOTER DE ACCIONES
            // =========================================================
            Panel panelBotones = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.FromArgb(245, 245, 245) };
            this.Controls.Add(panelBotones);

            // Cálculo dinámico para centrar botones.
            int btnWidth = 160;
            int spacing = 20;
            int totalWidth = (btnWidth * 2) + spacing;
            int startX_Buttons = (this.ClientSize.Width - totalWidth) / 2;

            // Botón Cancelar (Neutral)
            btnCancell = new Button { Text = "Cancelar", Size = new Size(btnWidth, 45), Location = new Point(startX_Buttons, 15), BackColor = Color.White, ForeColor = Color.DimGray, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11F), Cursor = Cursors.Hand };
            btnCancell.Click += btnCancell_Click;
            panelBotones.Controls.Add(btnCancell);

            // Botón Guardar (Acción Primaria - Verde)
            btnSave = new Button { Text = "Guardar Registro", Size = new Size(btnWidth, 45), Location = new Point(startX_Buttons + btnWidth + spacing, 15), BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += btnSave_Click;
            panelBotones.Controls.Add(btnSave);

            this.Load += FormRegistroEstudiantes_Load;
        }

        // --- HELPERS DE UI (DRY Pattern) ---
        // Simplifica la creación repetitiva de Label + TextBox.
        private TextBox CrearCampoTexto(Panel p, string titulo, int x, int y, int width)
        {
            Label lbl = new Label { Text = titulo, Location = new Point(x, y), Font = new Font("Segoe UI", 9F, FontStyle.Regular), AutoSize = true };
            p.Controls.Add(lbl);

            TextBox txt = new TextBox { Location = new Point(x, y + 22), Size = new Size(width, 28), Font = new Font("Segoe UI", 10F), BorderStyle = BorderStyle.FixedSingle };
            p.Controls.Add(txt);
            return txt;
        }

        private void BtnSubirFoto_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Previsualización segura: Cargo un stream para no bloquear el archivo original
                    // aunque en este punto solo es lectura.
                    using (var stream = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read))
                    {
                        pbFotoPerfil.Image = Image.FromStream(stream);
                    }
                    _rutaFotoSeleccionada = ofd.FileName;
                }
                catch (Exception ex) { MessageBox.Show("Error al cargar imagen: " + ex.Message); }
            }
        }

        // --- LÓGICA DE NEGOCIO ---
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Validación básica de campos requeridos.
                if (string.IsNullOrWhiteSpace(txtNombres.Text) || string.IsNullOrWhiteSpace(txtApellidos.Text) || cmbCarrera.SelectedValue == null)
                {
                    MessageBox.Show("Completa los campos obligatorios.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Construcción de la entidad (Mapping UI -> Objeto).
                Estudiante estudiante = new Estudiante();
                estudiante.Nombres = txtNombres.Text;
                estudiante.Apellidos = txtApellidos.Text;
                estudiante.DNI = txtDNI.Text;
                if (byte.TryParse(txtEdad.Text, out byte edad)) estudiante.Edad = edad;

                estudiante.Matricula = txtMatricula.Text;
                if (byte.TryParse(txtSemestre.Text, out byte sem)) estudiante.Semestre = sem;

                estudiante.IdCarrera = (int)cmbCarrera.SelectedValue;
                estudiante.FotoPerfilRuta = _rutaFotoSeleccionada;

                // Delegación al controlador. 
                // "Estudiante" aquí es el rol por defecto que se le asignará.
                var controller = new EstudianteController();
                bool exito = controller.Guardar(estudiante, "Estudiante");

                if (exito)
                {
                    MessageBox.Show("Estudiante registrado correctamente.", "SIVUG", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close(); // UX: Cerrar tras éxito.
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void btnCancell_Click(object sender, EventArgs e) => this.Close();

        // Carga inicial de datos de referencia.
        private void CargarFacultades()
        {
            cmbFacultad.DataSource = _facultadDAO.ObtenerTodas();
            cmbFacultad.DisplayMember = "Nombre";
            cmbFacultad.ValueMember = "Id";
            cmbFacultad.SelectedIndex = -1; // Nada seleccionado al inicio.
        }

        // Lógica de Cascada: Carga carreras al cambiar facultad.
        private void cmbFacultad_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (cmbFacultad.SelectedValue is int id)
            {
                cmbCarrera.DataSource = _carreraDAO.ObtenerPorIdFacultad(id);
                cmbCarrera.DisplayMember = "Nombre";
                cmbCarrera.ValueMember = "Id";
            }
        }
    }
}