using SIVUG.Models;
using SIVUG.Models.DAO;
using SIVUG.Models.DTOS;
using SIVUG.Models.SERVICES;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Estudiante = SIVUG.Models.Estudiante;


namespace SIVUG.View
{
    /// <summary>
    /// CONTROLADOR DE REGISTRO DE CANDIDATAS.
    /// 
    /// MISIÓN: Gestionar el ciclo de vida de las candidatas (Alta/Baja/Modificación).
    /// 
    /// CARACTERÍSTICAS CLAVE:
    /// - Integra búsqueda de estudiantes existentes.
    /// - Maneja la promoción de "Estudiante" a "Candidata" (Roles).
    /// - Administra perfiles complejos (Habilidades, Pasatiempos) con relaciones N:M.
    /// - Implementa validaciones de negocio estrictas (Unicidad, Integridad).
    /// </summary>
    public partial class FormRegistroCandidatas : Form
    {
        // ==================== CAPA DE SERVICIOS Y DATOS ====================
        private EstudianteService estudianteService;
        private CandidataService candidataService;
        private CandidataDAO candidataDAO;
        private CatalogoDAO catalogoDAO;
        private CarreraDAO carreraDAO;

        // ==================== ESTADO DE LA VISTA ====================
        
        // Estudiante seleccionado temporalmente antes de confirmar el registro.
        private Estudiante estudianteSeleccionado;
        
        // Ruta temporal de la imagen seleccionada.
        private string rutaImagenSeleccionada;

        // Listas temporales en memoria para la gestión de Tags (Habilidades, etc.) antes del commit.
        private List<string> _listaHabilidades = new List<string>();
        private List<string> _listaPasatiempos = new List<string>();
        private List<string> _listaAspiraciones = new List<string>();

        // ==================== REFERENCIAS UI ====================
        private GroupBox grpDatos;
        private GroupBox grpPerfil;

        private Panel panelBusqueda;
        private Panel panelInfoEstudiante;
        private Panel panelConfig;
        private Panel panelFoto;

        private TextBox txtBuscarCedula;
        private Button btnBuscarEstudiante;
        private PictureBox picFotoCandidato;
        private Button btnSeleccionarFoto;
        private CheckBox chkReina;
        private CheckBox chkFotogenia;

        // Controles de listas dinámicas
        private ListBox lbHabilidades;
        private ListBox lbPasatiempos;
        private ListBox lbAspiraciones;
        private ComboBox cboHabilidad;
        private ComboBox cboPasatiempo;
        private ComboBox cboAspiracion;

        private Button btnGuardar;
        private Button btnCancelar;
        private DataGridView dgvCandidatas;

        public FormRegistroCandidatas()
        {
            InitializeComponent();
            try
            {
                // Inicialización de Dependencias
                estudianteService = new EstudianteService();
                candidataService = new CandidataService();
                candidataDAO = new CandidataDAO();
                catalogoDAO = new CatalogoDAO();
                carreraDAO = new CarreraDAO();

                // Smoke Test de conexión a base de datos.
                System.Diagnostics.Debug.WriteLine($"Prueba DAO: {catalogoDAO.ObtenerPorTipo("HABILIDAD")?.Count ?? 0} habilidades");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inicializando DAOs:\n{ex.Message}", "Error Fatal",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            ConfigurarFormulario();
            InicializarComponentes();
            
            // Carga inicial de la grilla.
            CargarCandidatasActivas();
        }

        private void FormRegistroCandidatas_Load(object sender, EventArgs e) {  }

        // ==================== CONFIGURACIÓN DEL FORMULARIO ====================

        private void ConfigurarFormulario()
        {
            this.Text = "SIVUG - Registro de Candidatas";
            this.Size = new Size(1150, 900);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 245);
        }

        private void InicializarComponentes()
        {
            // Header
            Label lblTitulo = new Label
            {
                Text = "REGISTRO DE CANDIDATAS",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(25, 20),
                AutoSize = true
            };
            this.Controls.Add(lblTitulo);

            // 1. SECCIÓN: DATOS BÁSICOS
            grpDatos = new GroupBox
            {
                Text = "Datos Básicos",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(25, 70),
                Size = new Size(1080, 320),
                BackColor = Color.White
            };
            this.Controls.Add(grpDatos);

            // Sub-paneles de la sección de datos
            CrearPanelBusqueda(grpDatos, 20, 30);
            CrearPanelInformacionEstudiante(grpDatos, 20, 100);
            CrearPanelConfiguracion(grpDatos, 20, 240);
            CrearPanelFoto(grpDatos, 750, 30);

            // 2. SECCIÓN: PERFIL DETALLADO
            grpPerfil = new GroupBox
            {
                Text = "Perfil, Habilidades e Intereses",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(25, 400),
                Size = new Size(1080, 200),
                BackColor = Color.White
            };
            this.Controls.Add(grpPerfil);

            // Columnas de gestión de Tags (Habilidades, Pasatiempos, Aspiraciones)
            CrearColumnaPerfil(grpPerfil, "Habilidades", 20,
                ref cboHabilidad, ref lbHabilidades, _listaHabilidades, "HABILIDAD");

            CrearColumnaPerfil(grpPerfil, "Pasatiempos", 370,
                ref cboPasatiempo, ref lbPasatiempos, _listaPasatiempos, "PASATIEMPO");

            CrearColumnaPerfil(grpPerfil, "Aspiraciones", 720,
                ref cboAspiracion, ref lbAspiraciones, _listaAspiraciones, "ASPIRACION");

            // 3. SECCIÓN: ACCIONES (Botones actualizados)
            int btnY = 620;
            
            // Botón Guardar - Desplazado un poco a la izquierda
            btnGuardar = CrearBotonAccion("💾 Guardar", Color.FromArgb(46, 204, 113), 850, btnY);
            btnGuardar.Enabled = false; // Deshabilitado hasta que se busque un estudiante válido.
            btnGuardar.Click += BtnGuardar_Click;
            this.Controls.Add(btnGuardar);

            // Botón Cancelar - Con espacio considerable respescto a Guardar
            btnCancelar = CrearBotonAccion("Cancelar", Color.FromArgb(231, 76, 60), 990, btnY);
            btnCancelar.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancelar);

            // 4. SECCIÓN: LISTADO (GRILLA)
            Label lblGrid = new Label
            {
                Text = "CANDIDATAS REGISTRADAS",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(25, 660),
                AutoSize = true
            };
            this.Controls.Add(lblGrid);

            dgvCandidatas = new DataGridView
            {
                Location = new Point(25, 690),
                Size = new Size(1080, 150),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9F)
            };
            dgvCandidatas.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
            dgvCandidatas.CellClick += DgvCandidatas_CellClick;
            
            // Ajuste de altura de fila para mejor legibilidad
            dgvCandidatas.RowsAdded += (s, e) =>
            {
                for (int i = e.RowIndex; i < e.RowIndex + e.RowCount; i++)
                {
                    dgvCandidatas.Rows[i].Height = 35;
                }
            };
            this.Controls.Add(dgvCandidatas);
        }

        // ==================== CONSTRUCTORES DE PANELES (HELPERS UI) ====================

        /// <summary>
        /// Componente UI complejo para gestionar listas de strings (Tags).
        /// Incluye autocompletado y validación de duplicados.
        /// </summary>
        private void CrearColumnaPerfil(GroupBox padre, string titulo, int x, ref ComboBox cboInput, ref ListBox lbLista, List<string> fuenteDatos, string tipoBD)
        {
            Label lbl = new Label { Text = titulo, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(x, 25), AutoSize = true };
            padre.Controls.Add(lbl);

            ComboBox cbo = new ComboBox
            {
                Location = new Point(x, 45),
                Size = new Size(220, 25),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };

            CargarDatosCombo(cbo, tipoBD);
            padre.Controls.Add(cbo);
            cboInput = cbo;

            Button btnAdd = new Button
            {
                Text = "+",
                Location = new Point(x + 225, 44),
                Size = new Size(35, 27),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            padre.Controls.Add(btnAdd);

            ListBox lb = new ListBox { Location = new Point(x, 80), Size = new Size(260, 100), Font = new Font("Segoe UI", 9F), BorderStyle = BorderStyle.FixedSingle };
            padre.Controls.Add(lb);
            lbLista = lb;

            // Manejador del botón Agregar
            btnAdd.Click += (s, e) =>
            {
                string valor = cbo.Text.Trim();

                if (string.IsNullOrEmpty(valor))
                {
                    MessageBox.Show("Escriba un valor antes de agregar", "Validación");
                    return;
                }

                // Validación de unicidad en la lista visual
                if (fuenteDatos.Contains(valor, StringComparer.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Este elemento ya está en la lista", "Duplicado");
                    return;
                }

                // Actualizo la lista temporal
                fuenteDatos.Add(valor);
                ActualizarListBox(lb, fuenteDatos);

                // Auto-aprendo nuevos valores en el combo para usabilidad futura en la sesión
                if (!cbo.Items.Contains(valor))
                {
                    cbo.Items.Add(valor);
                }

                cbo.Text = "";
                cbo.Focus();
            };

            // Accesibilidad: Enter para agregar
            cbo.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { btnAdd.PerformClick(); e.Handled = true; } };
            
            // Usabilidad: Doble clic para eliminar
            lb.DoubleClick += (s, e) => 
            { 
                if (lb.SelectedIndex != -1) 
                { 
                    fuenteDatos.RemoveAt(lb.SelectedIndex); 
                    ActualizarListBox(lb, fuenteDatos); 
                } 
            };
        }

        /// <summary>
        /// Carga los datos del catálogo en el ComboBox.
        /// Maneja errores de DAO elegantemente.
        /// </summary>
        private void CargarDatosCombo(ComboBox cbo, string tipo)
        {
            try
            {
                cbo.Items.Clear();

                if (catalogoDAO == null)
                {
                    MessageBox.Show($"Error: CatalogoDAO no está inicializado", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var listaItems = catalogoDAO.ObtenerPorTipo(tipo);

                if (listaItems == null) return;

                foreach (var item in listaItems)
                {
                    if (item != null && !string.IsNullOrEmpty(item.Nombre))
                    {
                        cbo.Items.Add(item.Nombre);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR en CargarDatosCombo({tipo}): {ex}");
            }
        }

        private void ActualizarListBox(ListBox lb, List<string> datos)
        {
            lb.DataSource = null;
            lb.DataSource = datos;
        }

        private void CrearPanelBusqueda(GroupBox padre, int x, int y)
        {
            panelBusqueda = new Panel { Location = new Point(x, y), Size = new Size(700, 60) };
            padre.Controls.Add(panelBusqueda);
            Label lbl = new Label { Text = "Buscar estudiante por cédula:", Location = new Point(0, 5), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            panelBusqueda.Controls.Add(lbl);
            txtBuscarCedula = new TextBox { Location = new Point(0, 25), Size = new Size(250, 29), Font = new Font("Segoe UI", 12F) };
            txtBuscarCedula.KeyPress += TxtBuscarCedula_KeyPress;
            panelBusqueda.Controls.Add(txtBuscarCedula);
            btnBuscarEstudiante = new Button { Text = "🔍 Buscar", Location = new Point(260, 23), Size = new Size(100, 32), BackColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnBuscarEstudiante.Click += BtnBuscarEstudiante_Click;
            panelBusqueda.Controls.Add(btnBuscarEstudiante);
        }

        private void CrearPanelInformacionEstudiante(GroupBox padre, int x, int y)
        {
            panelInfoEstudiante = new Panel { Location = new Point(x, y), Size = new Size(700, 130), BackColor = Color.FromArgb(248, 249, 250), Visible = false };
            panelInfoEstudiante.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, panelInfoEstudiante.ClientRectangle, Color.LightGray, ButtonBorderStyle.Solid);
            padre.Controls.Add(panelInfoEstudiante);
            Label lblTitulo = new Label { Text = "INFORMACIÓN DEL ESTUDIANTE", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true, ForeColor = Color.DimGray };
            panelInfoEstudiante.Controls.Add(lblTitulo);
        }

        private void CrearPanelConfiguracion(GroupBox padre, int x, int y)
        {
            panelConfig = new Panel { Location = new Point(x, y), Size = new Size(700, 60) };
            padre.Controls.Add(panelConfig);
            Label lbl = new Label { Text = "TIPO DE CANDIDATURA", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(0, 0), AutoSize = true, ForeColor = Color.DimGray };
            panelConfig.Controls.Add(lbl);
            chkReina = new CheckBox { Text = "👑 Reina", Location = new Point(10, 25), AutoSize = true, Font = new Font("Segoe UI", 10F) };
            panelConfig.Controls.Add(chkReina);
            chkFotogenia = new CheckBox { Text = "📸 Fotogenia", Location = new Point(150, 25), AutoSize = true, Font = new Font("Segoe UI", 10F) };
            panelConfig.Controls.Add(chkFotogenia);
        }

        private void CrearPanelFoto(GroupBox padre, int x, int y)
        {
            panelFoto = new Panel { Location = new Point(x, y), Size = new Size(250, 280) };
            padre.Controls.Add(panelFoto);
            Label lbl = new Label { Text = "FOTO DE PERFIL", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(25, 0), AutoSize = true };
            panelFoto.Controls.Add(lbl);
            picFotoCandidato = new PictureBox { Location = new Point(25, 25), Size = new Size(200, 200), BackColor = Color.Gainsboro, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
            panelFoto.Controls.Add(picFotoCandidato);
            btnSeleccionarFoto = new Button { Text = "📁 Subir Foto", Location = new Point(25, 235), Size = new Size(200, 35), BackColor = Color.FromArgb(155, 89, 182), ForeColor = Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnSeleccionarFoto.Click += BtnSeleccionarFoto_Click;
            panelFoto.Controls.Add(btnSeleccionarFoto);
        }

        private Button CrearBotonAccion(string texto, Color color, int x, int y)
        {
            Button btn = new Button { Text = texto, Location = new Point(x, y), Size = new Size(110, 40), BackColor = color, ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // ==================== LÓGICA DE NEGOCIO ====================

        private void TxtBuscarCedula_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
            if (e.KeyChar == (char)Keys.Enter) BtnBuscarEstudiante_Click(sender, e);
        }

        /// <summary>
        /// Búsqueda de Estudiante:
        /// 1. Busca en BD.
        /// 2. Valida que exista.
        /// 3. Regla Crítica: Valida que NO SEA YA CANDIDATA (Unicidad).
        /// </summary>
        private void BtnBuscarEstudiante_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBuscarCedula.Text))
            {
                MessageBox.Show("Ingrese una cédula");
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                
                estudianteSeleccionado = estudianteService.ObtenerPorCedula(txtBuscarCedula.Text);

                if (estudianteSeleccionado == null)
                {
                    MessageBox.Show("Estudiante no encontrado");
                    LimpiarFormulario();
                    return;
                }

                // VALIDACIÓN DE NEGOCIO: Evitar duplicados de candidatas.
                if (candidataService.EsCandidataActiva(estudianteSeleccionado.Id))
                {
                    MessageBox.Show("Ya es candidata activa");
                    return;
                }

                // Si todo OK, muestro UI de detalle y habilito guardar.
                MostrarInformacionEstudiante();
                btnGuardar.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void MostrarInformacionEstudiante()
        {
            panelInfoEstudiante.Controls.Clear();
            panelInfoEstudiante.Visible = true;
            Label lblTitulo = new Label { Text = "INFORMACIÓN DEL ESTUDIANTE", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true, ForeColor = Color.DimGray };
            panelInfoEstudiante.Controls.Add(lblTitulo);

            void AddData(string label, string val, int x, int y)
            {
                Label l = new Label { Text = label, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(x, y), AutoSize = true };
                Label v = new Label { Text = val, Font = new Font("Segoe UI", 10F), Location = new Point(x + 70, y - 2), AutoSize = true };
                panelInfoEstudiante.Controls.Add(l);
                panelInfoEstudiante.Controls.Add(v);
            }

            AddData("Nombres:", estudianteSeleccionado.Nombres, 20, 35);
            AddData("Apellidos:", estudianteSeleccionado.Apellidos, 20, 60);
            AddData("Cédula:", estudianteSeleccionado.DNI, 20, 85);
            AddData("Facultad:", estudianteSeleccionado.Carrera.Facultad.Nombre, 350, 35);
            AddData("Carrera:", estudianteSeleccionado.Carrera.Nombre, 350, 60);
        }

        private void BtnSeleccionarFoto_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Imágenes|*.jpg;*.png" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    rutaImagenSeleccionada = ofd.FileName;
                    picFotoCandidato.Image = Image.FromFile(rutaImagenSeleccionada);
                }
            }
        }

        /// <summary>
        /// PROCESO DE REGISTRO TRANSACCIONAL-LIKE:
        /// 1. Copia física de la imagen.
        /// 2. Registro de la Candidata en BD.
        /// 3. Recuperación del ID generado.
        /// 4. Vinculación de los detalles del perfil (Habilidades, etc.).
        /// </summary>
        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (estudianteSeleccionado == null) return;
            
            if (!chkReina.Checked && !chkFotogenia.Checked)
            {
                MessageBox.Show("Seleccione tipo de candidatura.");
                return;
            }

            try
            {
                // Paso 1: Gestión de Archivo.
                string rutaDestino = (!string.IsNullOrEmpty(rutaImagenSeleccionada)) ? GuardarImagenCandidato() : null;

                // Paso 2: Construcción del Objeto.
                TipoVoto tipoVoto = chkReina.Checked ? TipoVoto.Reina : TipoVoto.Fotogenia;

                Candidata candidata = new Candidata
                {
                    Nombres = estudianteSeleccionado.Nombres,
                    Apellidos = estudianteSeleccionado.Apellidos,
                    ImagenPrincipal = rutaDestino,
                    Activa = true,
                    tipoCandidatura = tipoVoto
                };

                // Paso 3: Persistencia en BD (Candidata).
                if (candidataService.RegistrarCandidato(estudianteSeleccionado.Id, candidata))
                {
                    // Paso 4: Recuperación del ID para relaciones.
                    Candidata candidataRecuperada = candidataDAO.ObtenerPorIdUsuario(estudianteSeleccionado.Id);

                    if (candidataRecuperada != null)
                    {
                        // Paso 5: Persistencia de Relaciones (Perfil).
                        int idReal = estudianteSeleccionado.Id; // Mapping de ID.

                        catalogoDAO.AsignarDetalles(
                            idReal,
                            _listaHabilidades,
                            _listaPasatiempos,
                            _listaAspiraciones
                        );

                        MessageBox.Show("Candidata y perfil registrados exitosamente.", "Éxito");
                        LimpiarFormulario();
                        CargarCandidatasActivas();
                    }
                    else
                    {
                        MessageBox.Show("No se pudo registrar la candidata.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocurrió un error: " + ex.Message, "Error Crítico");
            }
        }

        private string GuardarImagenCandidato()
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImagenesCandidatas");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                
                // Nombre único para evitar colisiones.
                string nombre = $"cand_{estudianteSeleccionado.DNI}_{DateTime.Now.Ticks}{Path.GetExtension(rutaImagenSeleccionada)}";
                string dest = Path.Combine(dir, nombre);
                File.Copy(rutaImagenSeleccionada, dest, true);
                return dest;
            }
            catch { return null; }
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

            _listaHabilidades.Clear();
            ActualizarListBox(lbHabilidades, _listaHabilidades);
            _listaPasatiempos.Clear();
            ActualizarListBox(lbPasatiempos, _listaPasatiempos);
            _listaAspiraciones.Clear();
            ActualizarListBox(lbAspiraciones, _listaAspiraciones);
        }

        // ==================== MÉTODOS DEL DATAGRIDVIEW RECARGADO ====================

        private void CargarCandidatasActivas()
        {
            try
            {
                var lista = candidataService.ObtenerCandidatasActivas();

                dgvCandidatas.Columns.Clear();

                dgvCandidatas.Columns.Add("Candidata", "Candidata");
                dgvCandidatas.Columns.Add("Facultad", "Facultad");
                dgvCandidatas.Columns.Add("Carrera", "Carrera");
                dgvCandidatas.Columns.Add("TipoCandidatura", "Tipo");
                dgvCandidatas.Columns.Add("Estado", "Estado");

                // Columna de botones de acción.
                DataGridViewButtonColumn btnAcciones = new DataGridViewButtonColumn
                {
                    Name = "Acciones",
                    Text = "Acciones",
                    UseColumnTextForButtonValue = false,
                    Width = 100
                };
                dgvCandidatas.Columns.Add(btnAcciones);

                foreach (var candidata in lista)
                {
                    string tipoVoto = candidataService.ObtenerDescripcionTipoVoto(candidata.tipoCandidatura);
                    string estado = candidata.Activa ? "Activa" : "Inactiva";

                    int rowIndex = dgvCandidatas.Rows.Add();
                    dgvCandidatas.Rows[rowIndex].Cells["Candidata"].Value = candidata.GetNombreCompleto();
                    dgvCandidatas.Rows[rowIndex].Cells["Facultad"].Value = candidata.Carrera?.Facultad?.Nombre ?? "N/A";
                    dgvCandidatas.Rows[rowIndex].Cells["Carrera"].Value = candidata.Carrera?.Nombre ?? "N/A";
                    dgvCandidatas.Rows[rowIndex].Cells["TipoCandidatura"].Value = tipoVoto;
                    dgvCandidatas.Rows[rowIndex].Cells["Estado"].Value = estado;

                    DataGridViewButtonCell btnCell = (DataGridViewButtonCell)dgvCandidatas.Rows[rowIndex].Cells["Acciones"];
                    btnCell.Value = "⋮"; // Icono de menú.

                    // Guardo el ID en el tag para recuperarlo luego.
                    dgvCandidatas.Rows[rowIndex].Tag = candidata.CandidataId;

                    dgvCandidatas.Rows[rowIndex].Height = 35;
                }

                dgvCandidatas.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCellsExceptHeader);
                dgvCandidatas.Columns["Acciones"].Width = 60;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar candidatas: {ex.Message}");
            }
        }

        private void DgvCandidatas_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != dgvCandidatas.Columns["Acciones"].Index || e.RowIndex < 0)
                return;

            try
            {
                int candidataId = (int)dgvCandidatas.Rows[e.RowIndex].Tag;

                // Menú Contextual para las acciones.
                ContextMenuStrip menu = new ContextMenuStrip();

                ToolStripMenuItem itemVer = new ToolStripMenuItem("👁️ Ver Detalles", null, (s, evt) => VerCandidataDetalles(candidataId));
                ToolStripMenuItem itemEditar = new ToolStripMenuItem("✏️ Editar", null, (s, evt) => EditarCandidata(candidataId));
                ToolStripMenuItem itemEliminar = new ToolStripMenuItem("❌ Eliminar", null, (s, evt) => EliminarCandidata(candidataId));

                menu.Items.Add(itemVer);
                menu.Items.Add(itemEditar);
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(itemEliminar);

                menu.Show(dgvCandidatas, dgvCandidatas.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false).Location);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar acción: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void VerCandidataDetalles(int candidataId)
        {
            try
            {
                Candidata candidata = candidataService.ObtenerCandidataPorId(candidataId);

                if (candidata == null)
                {
                    MessageBox.Show("No se encontró la candidata", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Carga Lazy de detalles.
                var detallesCatalogo = catalogoDAO.ObtenerDeCandidata(candidataId);
                
                if (detallesCatalogo != null && detallesCatalogo.Count > 0)
                {
                    // Lógica de mapeo: Catálogo DTO -> Listas POCO.
                    candidata.Habilidades = detallesCatalogo?
                        .Where(d => d.Tipo == "HABILIDAD")
                        .Select(d => d.Nombre)
                        .ToList() ?? new List<string>();

                    candidata.Pasatiempos = detallesCatalogo?
                        .Where(d => d.Tipo == "PASATIEMPO")
                        .Select(d => d.Nombre)
                        .ToList() ?? new List<string>();

                    candidata.Aspiraciones = detallesCatalogo?
                        .Where(d => d.Tipo == "ASPIRACION")
                        .Select(d => d.Nombre)
                        .ToList() ?? new List<string>();
                }

                FormVisualizarCandidata formVisualizar = new FormVisualizarCandidata(candidata);
                formVisualizar.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener detalles:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditarCandidata(int candidataId)
        {
            try
            {
                Candidata candidata = candidataService.ObtenerCandidataPorId(candidataId);

                if (candidata == null)
                {
                    MessageBox.Show("No se encontró la candidata", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Carga completa del grafo de objetos antes de editar.
                var detallesCatalogo = catalogoDAO.ObtenerDeCandidata(candidataId);
                if (detallesCatalogo != null && detallesCatalogo.Count > 0)
                {
                    candidata.Habilidades = detallesCatalogo?
                        .Where(d => d.Tipo == "HABILIDAD")
                        .Select(d => d.Nombre)
                        .ToList() ?? new List<string>();

                    candidata.Pasatiempos = detallesCatalogo?
                        .Where(d => d.Tipo == "PASATIEMPO")
                        .Select(d => d.Nombre)
                        .ToList() ?? new List<string>();

                    candidata.Aspiraciones = detallesCatalogo?
                        .Where(d => d.Tipo == "ASPIRACION")
                        .Select(d => d.Nombre)
                        .ToList() ?? new List<string>();
                }

                FormEditarCandidata formEditar = new FormEditarCandidata(candidata);
                if (formEditar.ShowDialog() == DialogResult.OK)
                {
                    CargarCandidatasActivas();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al editar candidata: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EliminarCandidata(int candidataId)
        {
            try
            {
                Candidata candidata = candidataService.ObtenerCandidataPorId(candidataId);

                if (candidata == null)
                {
                    MessageBox.Show("No se encontró la candidata", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult resultado = MessageBox.Show(
                    $"¿Está seguro de que desea eliminar a {candidata.Nombres} {candidata.Apellidos}?\n\nEsta acción no se puede deshacer.",
                    "Confirmar Eliminación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (resultado == DialogResult.Yes)
                {
                    // Soft Delete: Solo marco Activa = false.
                    if (candidataService.ActualizarEstadoCandidata(candidataId, false))
                    {
                        MessageBox.Show("Candidata eliminada correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        CargarCandidatasActivas();
                    }
                    else
                    {
                        MessageBox.Show("No se pudo eliminar la candidata.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar candidata: {ex.Message}", "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}