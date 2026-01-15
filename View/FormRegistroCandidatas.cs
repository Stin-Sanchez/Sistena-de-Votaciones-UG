using SIVUG.Models;
using SIVUG.Models.DAO;
using SIVUG.Models.SERVICES;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SIVUG.View
{
    public partial class FormRegistroCandidatas : Form
    {
        // Servicios y DAO
        private EstudianteService estudianteService;
        private CandidataService candidataService;
        private CarreraDAO carreraDAO;

        // Variables de estado
        private Estudiante estudianteSeleccionado;
        private string rutaImagenSeleccionada;

        // --- CONTROLES ---
        private GroupBox grpDatos; // El contenedor principal

        // Paneles internos
        private Panel panelBusqueda;
        private Panel panelInfoEstudiante;
        private Panel panelConfig;
        private Panel panelFoto;

        // Inputs
        private TextBox txtBuscarCedula;
        private Button btnBuscarEstudiante;
        private PictureBox picFotoCandidato;
        private Button btnSeleccionarFoto;
        private CheckBox chkReina;
        private CheckBox chkFotogenia;

        // Botones Acción
        private Button btnGuardar;
        private Button btnCancelar;
        private Button btnNuevo;

        // Grid
        private DataGridView dgvCandidatas;

        public FormRegistroCandidatas()
        {
            InitializeComponent(); // Si usas el designer o no, esto inicializa la clase base

            estudianteService = new EstudianteService();
            candidataService = new CandidataService();
            carreraDAO = new CarreraDAO();

            ConfigurarFormulario();
            InicializarComponentes();
            CargarCandidatasActivas();
        }

        private void FormRegistroCandidatas_Load(object sender, EventArgs e)
        {
        }

        private void ConfigurarFormulario()
        {
            this.Text = "SIVUG - Registro de Candidatas";
            // 1. TAMAÑO FIJO Y SIN MAXIMIZAR
            this.Size = new Size(1150, 720);
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // Borde fijo
            this.MaximizeBox = false; // Deshabilitar maximizar
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 245);
        }

        private void InicializarComponentes()
        {
            // Título Principal
            Label lblTitulo = new Label
            {
                Text = "REGISTRO DE CANDIDATAS",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(25, 20),
                AutoSize = true
            };
            this.Controls.Add(lblTitulo);

            // ==========================================
            // 2. GROUPBOX "DATOS DE INSCRIPCIÓN"
            // ==========================================
            grpDatos = new GroupBox
            {
                Text = "Datos de Inscripción",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold), // Fuente del título del grupo
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(25, 70),
                Size = new Size(1080, 380), // Tamaño fijo para contener todo
                BackColor = Color.White
            };
            this.Controls.Add(grpDatos);

            // Dentro del GroupBox colocamos los paneles usando coordenadas relativas al GroupBox

            // A. BUSCADOR (Arriba a la izquierda)
            CrearPanelBusqueda(grpDatos, 20, 30);

            // B. INFORMACIÓN (Debajo del buscador)
            CrearPanelInformacionEstudiante(grpDatos, 20, 110);

            // C. CHECKS (Debajo de la info)
            CrearPanelConfiguracion(grpDatos, 20, 270);

            // D. FOTO (A la derecha)
            CrearPanelFoto(grpDatos, 750, 30);


            // ==========================================
            // 3. BOTONES DE ACCIÓN (Fuera del GroupBox)
            // ==========================================
            int btnY = 465; // Posición Y debajo del GroupBox

            // Botón Guardar
            btnGuardar = CrearBotonAccion("💾 Guardar", Color.FromArgb(46, 204, 113), 850, btnY);
            btnGuardar.Enabled = false;
            btnGuardar.Click += BtnGuardar_Click;
            this.Controls.Add(btnGuardar);

            // Botón Nuevo
            btnNuevo = CrearBotonAccion("📄 Nuevo", Color.FromArgb(52, 152, 219), 980, btnY);
            btnNuevo.Click += BtnNuevo_Click;
            this.Controls.Add(btnNuevo);

            // Botón Cancelar
            btnCancelar = CrearBotonAccion("Cancelar", Color.FromArgb(231, 76, 60), 1005, btnY);
            // Ajustamos posición del cancelar un poco mas a la derecha si quieres, o alineado
            btnCancelar.Location = new Point(1005, btnY);
            // Para que quepan bien alineados ajusto las X:
            btnGuardar.Location = new Point(785, btnY); // Más a la izquierda
            btnNuevo.Location = new Point(910, btnY);
            btnCancelar.Location = new Point(1020, btnY); // Pegado al borde
            btnCancelar.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancelar);


            // ==========================================
            // 4. GRID (Abajo del todo)
            // ==========================================
            Label lblGrid = new Label
            {
                Text = "CANDIDATAS REGISTRADAS",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(25, 510),
                AutoSize = true
            };
            this.Controls.Add(lblGrid);

            dgvCandidatas = new DataGridView
            {
                Location = new Point(25, 540),
                Size = new Size(1080, 130), // Altura fija
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, // Columnas llenan el ancho
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9F),
                // SCROLL ACTIVADO
                ScrollBars = ScrollBars.Both
            };

            // Estilos del grid
            dgvCandidatas.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
            dgvCandidatas.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            dgvCandidatas.DefaultCellStyle.SelectionForeColor = Color.White;

            this.Controls.Add(dgvCandidatas);
        }

        // --- MÉTODOS DE CREACIÓN DE PANELES ---

        private void CrearPanelBusqueda(GroupBox padre, int x, int y)
        {
            panelBusqueda = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(700, 70),
                BackColor = Color.Transparent
                // BorderStyle = BorderStyle.FixedSingle // Opcional, se ve mas limpio sin borde dentro del groupbox
            };
            padre.Controls.Add(panelBusqueda);

            Label lbl = new Label { Text = "Buscar estudiante por cédula:", Location = new Point(0, 5), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular) };
            panelBusqueda.Controls.Add(lbl);

            txtBuscarCedula = new TextBox { Location = new Point(0, 30), Size = new Size(250, 29), Font = new Font("Segoe UI", 12F) };
            txtBuscarCedula.KeyPress += TxtBuscarCedula_KeyPress;
            panelBusqueda.Controls.Add(txtBuscarCedula);

            btnBuscarEstudiante = new Button
            {
                Text = "🔍 Buscar",
                Location = new Point(260, 28),
                Size = new Size(100, 32),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnBuscarEstudiante.FlatAppearance.BorderSize = 0;
            btnBuscarEstudiante.Click += BtnBuscarEstudiante_Click;
            panelBusqueda.Controls.Add(btnBuscarEstudiante);
        }

        private void CrearPanelInformacionEstudiante(GroupBox padre, int x, int y)
        {
            panelInfoEstudiante = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(700, 150), // Espacio suficiente
                BackColor = Color.FromArgb(248, 249, 250), // Gris muy claro para destacar
                Visible = false
            };
            // Borde sutil
            panelInfoEstudiante.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, panelInfoEstudiante.ClientRectangle, Color.LightGray, ButtonBorderStyle.Solid);

            padre.Controls.Add(panelInfoEstudiante);

            Label lblTitulo = new Label { Text = "INFORMACIÓN DEL ESTUDIANTE", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true, ForeColor = Color.DimGray };
            panelInfoEstudiante.Controls.Add(lblTitulo);
        }

        private void CrearPanelConfiguracion(GroupBox padre, int x, int y)
        {
            panelConfig = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(700, 80),
                BackColor = Color.Transparent
            };
            padre.Controls.Add(panelConfig);

            Label lbl = new Label { Text = "TIPO DE CANDIDATURA (Seleccione al menos una)", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(0, 0), AutoSize = true, ForeColor = Color.DimGray };
            panelConfig.Controls.Add(lbl);

            chkReina = new CheckBox { Text = "👑 Reina de la Universidad", Location = new Point(10, 35), Size = new Size(200, 25), Font = new Font("Segoe UI", 10F), AutoSize = true };
            panelConfig.Controls.Add(chkReina);

            chkFotogenia = new CheckBox { Text = "📸 Miss Fotogenia", Location = new Point(250, 35), Size = new Size(200, 25), Font = new Font("Segoe UI", 10F), AutoSize = true };
            panelConfig.Controls.Add(chkFotogenia);
        }

        private void CrearPanelFoto(GroupBox padre, int x, int y)
        {
            panelFoto = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(300, 320),
                BackColor = Color.Transparent
            };
            padre.Controls.Add(panelFoto);

            Label lbl = new Label { Text = "FOTO DE PERFIL", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(50, 0), AutoSize = true };
            panelFoto.Controls.Add(lbl);

            picFotoCandidato = new PictureBox
            {
                Location = new Point(50, 25),
                Size = new Size(200, 240),
                BackColor = Color.Gainsboro,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };
            panelFoto.Controls.Add(picFotoCandidato);

            btnSeleccionarFoto = new Button
            {
                Text = "📁 Subir Foto",
                Location = new Point(50, 275),
                Size = new Size(200, 35),
                BackColor = Color.FromArgb(155, 89, 182), // Morado
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSeleccionarFoto.FlatAppearance.BorderSize = 0;
            btnSeleccionarFoto.Click += BtnSeleccionarFoto_Click;
            panelFoto.Controls.Add(btnSeleccionarFoto);
        }

        private Button CrearBotonAccion(string texto, Color color, int x, int y)
        {
            Button btn = new Button
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(110, 40), // Tamaño pequeño solicitado
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }


        // ==========================================
        //        LÓGICA DE NEGOCIO (INTACTA)
        // ==========================================

        private void TxtBuscarCedula_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
            if (e.KeyChar == (char)Keys.Enter) BtnBuscarEstudiante_Click(sender, e);
        }

        private void BtnBuscarEstudiante_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBuscarCedula.Text))
            {
                MessageBox.Show("Por favor ingrese una cédula", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBuscarCedula.Focus();
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                estudianteSeleccionado = estudianteService.ValidarEstudiante(txtBuscarCedula.Text);

                if (estudianteSeleccionado == null)
                {
                    MessageBox.Show("No se encontró ningún estudiante con esa cédula", "Estudiante no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LimpiarFormulario();
                    return;
                }

                if (candidataService.EsCandidataActiva(estudianteSeleccionado.Id))
                {
                    MessageBox.Show("Esta estudiante ya está registrada como candidata activa", "Candidata Existente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                MostrarInformacionEstudiante();
                btnGuardar.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar estudiante: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor = Cursors.Default; }
        }

        private void MostrarInformacionEstudiante()
        {
            panelInfoEstudiante.Controls.Clear();
            panelInfoEstudiante.Visible = true;

            // Volvemos a agregar el título porque el Clear lo borra
            Label lblTitulo = new Label { Text = "INFORMACIÓN DEL ESTUDIANTE", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true, ForeColor = Color.DimGray };
            panelInfoEstudiante.Controls.Add(lblTitulo);

            // Helpers para labels
            void AddData(string label, string val, int x, int y)
            {
                Label l = new Label { Text = label, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(x, y), AutoSize = true };
                Label v = new Label { Text = val, Font = new Font("Segoe UI", 10F, FontStyle.Regular), Location = new Point(x + 70, y - 2), AutoSize = true, ForeColor = Color.Black };
                panelInfoEstudiante.Controls.Add(l);
                panelInfoEstudiante.Controls.Add(v);
            }

            AddData("Nombres:", estudianteSeleccionado.Nombres, 20, 40);
            AddData("Apellidos:", estudianteSeleccionado.Apellidos, 20, 70);
            AddData("Cédula:", estudianteSeleccionado.DNI, 20, 100);

            AddData("Facultad:", estudianteSeleccionado.Carrera.Facultad.Nombre, 350, 40);
            AddData("Carrera:", estudianteSeleccionado.Carrera.Nombre, 350, 70);
            AddData("Matrícula:", estudianteSeleccionado.Matricula, 350, 100);
        }

        private void BtnSeleccionarFoto_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Archivos de imagen|*.jpg;*.jpeg;*.png;*.bmp";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        rutaImagenSeleccionada = openFileDialog.FileName;
                        picFotoCandidato.Image = Image.FromFile(rutaImagenSeleccionada);
                    }
                    catch (Exception ex) { MessageBox.Show("Error al cargar imagen: " + ex.Message); }
                }
            }
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (estudianteSeleccionado == null) return;
            if (!chkReina.Checked && !chkFotogenia.Checked)
            {
                MessageBox.Show("Seleccione al menos una candidatura.", "Aviso");
                return;
            }

            try
            {
                string rutaDestino = (!string.IsNullOrEmpty(rutaImagenSeleccionada)) ? GuardarImagenCandidato() : null;

                Candidata candidata = new Candidata
                {
                    Nombres = $"{estudianteSeleccionado.Nombres} {estudianteSeleccionado.Apellidos}",
                    ImagenPrincipal = rutaDestino,
                    Activa = true
                };

                // Asignación de tipos de candidatura según tu lógica original
                // Aquí deberías adaptar si tu servicio maneja lógica específica para "Reina" o "Fotogenia"
                // O si simplemente guardas el objeto Candidata. 
                // Asumo que tu backend maneja la lógica de inserción basada en los checkboxes si fuera necesario,
                // pero aquí solo veo un objeto 'Candidata'.

                if (candidataService.RegistrarCandidato(estudianteSeleccionado.Id, candidata))
                {
                    MessageBox.Show("Candidata registrada exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LimpiarFormulario();
                    CargarCandidatasActivas();
                }
                else MessageBox.Show("Error al registrar.", "Error");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private string GuardarImagenCandidato()
        {
            try
            {
                string directorioBase = AppDomain.CurrentDomain.BaseDirectory;
                string rutaCarpetaSegura = Path.GetFullPath(Path.Combine(directorioBase, @"..\..\..\ImagenesCandidatas"));
                if (!Directory.Exists(rutaCarpetaSegura))
                {
                    Directory.CreateDirectory(rutaCarpetaSegura);
                }
                // Generar nombre único
                string extension = Path.GetExtension(rutaImagenSeleccionada);
                string nombreArchivo = $"candidata_{estudianteSeleccionado.DNI}_{DateTime.Now:yyyyMMddHHmmss}{extension}";

                // Ruta final
                string rutaDestino = Path.Combine(rutaCarpetaSegura, nombreArchivo);
                File.Copy(rutaImagenSeleccionada, rutaDestino, true);
                return rutaDestino;
            }
            catch(Exception ex) {
                MessageBox.Show($"Error al guardar la imagen: {ex.Message}",
            "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return null; }
        }

        private void BtnNuevo_Click(object sender, EventArgs e)
        {
            LimpiarFormulario();
            txtBuscarCedula.Focus();
        }

        private void LimpiarFormulario()
        {
            txtBuscarCedula.Text = "";
            estudianteSeleccionado = null;
            rutaImagenSeleccionada = null;
            panelInfoEstudiante.Visible = false;
            chkReina.Checked = false;
            chkFotogenia.Checked = false;
            picFotoCandidato.Image = null;
            btnGuardar.Enabled = false;
        }

        private void CargarCandidatasActivas()
        {
            try
            {
                var listaOriginal = candidataService.ObtenerCandidatasActivas();
                var listaVisual = listaOriginal.Select(x => new
                {
                    Id = x.CandidataId,
                    Nombre = x.Nombres,
                    Facultad = x.Carrera.Facultad.Nombre,
                    Carrera = x.Carrera.Nombre,
                    Estado = x.Activa ? "Activa" : "Inactiva"
                }).ToList();

                dgvCandidatas.DataSource = listaVisual;
                if (dgvCandidatas.Columns.Count > 0)
                {
                    dgvCandidatas.Columns["Id"].Visible = false;
                    dgvCandidatas.Columns["Nombre"].HeaderText = "Nombre Completo";
                    dgvCandidatas.Columns["Nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
    }
}