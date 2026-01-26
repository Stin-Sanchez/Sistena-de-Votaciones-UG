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

namespace SIVUG.View
{
    public partial class FormRegistroCandidatas : Form
    {
        // Servicios y DAOs
        private EstudianteService estudianteService;
        private CandidataService candidataService;
        private CandidataDAO candidataDAO; // DAO Directo
        private CatalogoDAO catalogoDAO;   // Para habilidades
        private CarreraDAO carreraDAO;

        // Variables de estado
        private Estudiante estudianteSeleccionado;
        private string rutaImagenSeleccionada;

        // Listas temporales para el perfil
        private List<string> _listaHabilidades = new List<string>();
        private List<string> _listaPasatiempos = new List<string>();
        private List<string> _listaAspiraciones = new List<string>();

        // --- CONTROLES UI ---
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

        // Inputs Perfil
        private ListBox lbHabilidades;
        private ListBox lbPasatiempos;
        private ListBox lbAspiraciones;
        private ComboBox cboHabilidad;   
        private ComboBox cboPasatiempo;  
        private ComboBox cboAspiracion;  

        private Button btnGuardar;
        private Button btnCancelar;
        private Button btnNuevo;
        private DataGridView dgvCandidatas;

        public FormRegistroCandidatas()
        {
            InitializeComponent();
            try
            {

                // Inicialización de lógica
                estudianteService = new EstudianteService();
            candidataService = new CandidataService();
            candidataDAO = new CandidataDAO(); // Instanciamos el DAO
            catalogoDAO = new CatalogoDAO();
            carreraDAO = new CarreraDAO();

            // PRUEBA: Verificar que el DAO funciona
            var prueba = catalogoDAO.ObtenerPorTipo("HABILIDAD");
            System.Diagnostics.Debug.WriteLine($"Prueba DAO: {prueba?.Count ?? 0} habilidades");
        }
    catch (Exception ex)
    {
        MessageBox.Show($"Error inicializando DAOs:\n{ex.Message}", "Error Fatal", 
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

            ConfigurarFormulario();
            InicializarComponentes();
            CargarCandidatasActivas();
        }

        private void FormRegistroCandidatas_Load(object sender, EventArgs e) { }

        private void ConfigurarFormulario()
        {
            this.Text = "SIVUG - Registro de Candidatas";
            this.Size = new Size(1150, 850);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 245);
        }

        private void InicializarComponentes()
        {
            // Título
            Label lblTitulo = new Label
            {
                Text = "REGISTRO DE CANDIDATAS",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(25, 20),
                AutoSize = true
            };
            this.Controls.Add(lblTitulo);

            // 1. DATOS BÁSICOS
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

            CrearPanelBusqueda(grpDatos, 20, 30);
            CrearPanelInformacionEstudiante(grpDatos, 20, 100);
            CrearPanelConfiguracion(grpDatos, 20, 240);
            CrearPanelFoto(grpDatos, 750, 30);

            // 2. PERFIL (HABILIDADES)
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

            // HABILIDADES
            CrearColumnaPerfil(grpPerfil, "Habilidades", 20,
                ref cboHabilidad, ref lbHabilidades, _listaHabilidades, "HABILIDAD");

            // PASATIEMPOS
            CrearColumnaPerfil(grpPerfil, "Pasatiempos", 370,
                ref cboPasatiempo, ref lbPasatiempos, _listaPasatiempos, "PASATIEMPO");

            // ASPIRACIONES
            CrearColumnaPerfil(grpPerfil, "Aspiraciones", 720,
                ref cboAspiracion, ref lbAspiraciones, _listaAspiraciones, "ASPIRACION");

            // 3. BOTONES
            int btnY = 620;
            btnGuardar = CrearBotonAccion("💾 Guardar", Color.FromArgb(46, 204, 113), 785, btnY);
            btnGuardar.Enabled = false;
            btnGuardar.Click += BtnGuardar_Click;
            this.Controls.Add(btnGuardar);

            btnNuevo = CrearBotonAccion("📄 Nuevo", Color.FromArgb(52, 152, 219), 910, btnY);
            btnNuevo.Click += BtnNuevo_Click;
            this.Controls.Add(btnNuevo);

            btnCancelar = CrearBotonAccion("Cancelar", Color.FromArgb(231, 76, 60), 1020, btnY);
            btnCancelar.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancelar);

            // 4. GRID
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
                Size = new Size(1080, 110),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9F)
            };
            dgvCandidatas.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
            this.Controls.Add(dgvCandidatas);
        }

        // --- BUILDERS UI ---

        private void CrearColumnaPerfil(GroupBox padre, string titulo, int x, ref ComboBox cboInput, ref ListBox lbLista, List<string> fuenteDatos, string tipoBD)
        {
            // 1. Etiqueta
            Label lbl = new Label { Text = titulo, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(x, 25), AutoSize = true };
            padre.Controls.Add(lbl);

            // 2. ComboBox
            ComboBox cbo = new ComboBox
            {
                Location = new Point(x, 45),
                Size = new Size(220, 25),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDown, // Permite escribir nuevos
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };

            // --- AQUÍ LLAMAMOS AL MÉTODO NUEVO ---
            CargarDatosCombo(cbo, tipoBD);
            // -------------------------------------

            padre.Controls.Add(cbo);
            cboInput = cbo; // Guardamos la referencia

            // 3. Botón Agregar (+)
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

            // 4. ListBox
            ListBox lb = new ListBox { Location = new Point(x, 80), Size = new Size(260, 100), Font = new Font("Segoe UI", 9F), BorderStyle = BorderStyle.FixedSingle };
            padre.Controls.Add(lb);
            lbLista = lb;

            // --- Eventos ---
            btnAdd.Click += (s, e) => {
                string valor = cbo.Text.Trim();

                if (string.IsNullOrEmpty(valor))
                {
                    MessageBox.Show("Escriba un valor antes de agregar", "Validación");
                    return;
                }

                if (fuenteDatos.Contains(valor, StringComparer.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Este elemento ya está en la lista", "Duplicado");
                    return;
                }

                // ✅ Agregar a la lista temporal
                fuenteDatos.Add(valor);
                ActualizarListBox(lb, fuenteDatos);

                // ✅ Si es nuevo, agregarlo también al combo para futuros usos
                if (!cbo.Items.Contains(valor))
                {
                    cbo.Items.Add(valor);
                }

                cbo.Text = "";
                cbo.Focus();
            };

            cbo.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { btnAdd.PerformClick(); e.Handled = true; } };
            lb.DoubleClick += (s, e) => { if (lb.SelectedIndex != -1) { fuenteDatos.RemoveAt(lb.SelectedIndex); ActualizarListBox(lb, fuenteDatos); } };
        }

        private void CargarDatosCombo(ComboBox cbo, string tipo)
        {
            try
            {
                cbo.Items.Clear();

                // PUNTO DE DEPURACIÓN 1: Verificar que el DAO existe
                if (catalogoDAO == null)
                {
                    MessageBox.Show($"Error: CatalogoDAO no está inicializado", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // PUNTO DE DEPURACIÓN 2: Verificar consulta
                var listaItems = catalogoDAO.ObtenerPorTipo(tipo);

                // PUNTO DE DEPURACIÓN 3: Validar resultados
                if (listaItems == null)
                {
                    MessageBox.Show($"Error: No se pudo obtener datos de tipo '{tipo}' (retornó NULL)",
                        "Error BD", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (listaItems.Count == 0)
                {
                    // Esto NO es error, solo no hay datos precargados
                    System.Diagnostics.Debug.WriteLine($"⚠️ No hay registros en BD para tipo: {tipo}");
                    // El combo queda vacío pero funcional para agregar nuevos
                    return;
                }

                // PUNTO DE DEPURACIÓN 4: Cargar datos
                foreach (var item in listaItems)
                {
                    if (item != null && !string.IsNullOrEmpty(item.Nombre))
                    {
                        cbo.Items.Add(item.Nombre);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"✅ Cargados {cbo.Items.Count} items en combo {tipo}");
            }
            catch (Exception ex)
            {
                // MOSTRAR ERROR AL USUARIO, no solo en consola
                MessageBox.Show($"Error cargando datos de '{tipo}':\n{ex.Message}\n\nStack: {ex.StackTrace}",
                    "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);

                System.Diagnostics.Debug.WriteLine($"❌ ERROR en CargarDatosCombo({tipo}): {ex}");
            }
        }

        private void ActualizarListBox(ListBox lb, List<string> datos) { lb.DataSource = null; lb.DataSource = datos; }

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

        // --- LÓGICA DE NEGOCIO ---

        private void TxtBuscarCedula_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
            if (e.KeyChar == (char)Keys.Enter) BtnBuscarEstudiante_Click(sender, e);
        }

        private void BtnBuscarEstudiante_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBuscarCedula.Text)) { MessageBox.Show("Ingrese una cédula"); return; }
            try
            {
                Cursor = Cursors.WaitCursor;
                estudianteSeleccionado = estudianteService.ValidarEstudiante(txtBuscarCedula.Text);
                if (estudianteSeleccionado == null) { MessageBox.Show("Estudiante no encontrado"); LimpiarFormulario(); return; }
                if (candidataService.EsCandidataActiva(estudianteSeleccionado.Id)) { MessageBox.Show("Ya es candidata activa"); return; }
                MostrarInformacionEstudiante();
                btnGuardar.Enabled = true;
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            finally { Cursor = Cursors.Default; }
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
                panelInfoEstudiante.Controls.Add(l); panelInfoEstudiante.Controls.Add(v);
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

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (estudianteSeleccionado == null) return;
            if (!chkReina.Checked && !chkFotogenia.Checked) { MessageBox.Show("Seleccione tipo de candidatura."); return; }

            try
            {
                string rutaDestino = (!string.IsNullOrEmpty(rutaImagenSeleccionada)) ? GuardarImagenCandidato() : null;
                Candidata candidata = new Candidata
                {
                    Nombres = $"{estudianteSeleccionado.Nombres} {estudianteSeleccionado.Apellidos}",
                    ImagenPrincipal = rutaDestino,
                    Activa = true
                };

                // 1. Guardar Candidata
                if (candidataService.RegistrarCandidato(estudianteSeleccionado.Id, candidata))
                {
                    // 2. OBTENER ID USANDO TU MÉTODO  EL DAO


                    Candidata candidataRecuperada = candidataDAO.ObtenerPorIdUsuario(estudianteSeleccionado.Id);
                    // Paso B: Verificamos que no sea null y EXTRAEMOS EL ID
                    if (candidataRecuperada != null)
                    {
                        int idReal = candidataRecuperada.CandidataId; // <--- Aquí sacamos el int

                        // Paso C: Guardamos el perfil usando ese ID real
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
                string nombre = $"cand_{estudianteSeleccionado.DNI}_{DateTime.Now.Ticks}{Path.GetExtension(rutaImagenSeleccionada)}";
                string dest = Path.Combine(dir, nombre);
                File.Copy(rutaImagenSeleccionada, dest, true);
                return dest;
            }
            catch { return null; }
        }

        private void BtnNuevo_Click(object sender, EventArgs e) { LimpiarFormulario(); txtBuscarCedula.Focus(); }

        private void LimpiarFormulario()
        {
            txtBuscarCedula.Text = "";
            estudianteSeleccionado = null;
            rutaImagenSeleccionada = null;
            panelInfoEstudiante.Visible = false;
            chkReina.Checked = false; chkFotogenia.Checked = false;
            picFotoCandidato.Image = null;
            btnGuardar.Enabled = false;

            _listaHabilidades.Clear(); ActualizarListBox(lbHabilidades, _listaHabilidades);
            _listaPasatiempos.Clear(); ActualizarListBox(lbPasatiempos, _listaPasatiempos);
            _listaAspiraciones.Clear(); ActualizarListBox(lbAspiraciones, _listaAspiraciones);
        }

        private void CargarCandidatasActivas()
        {
            try
            {
                var lista = candidataService.ObtenerCandidatasActivas();
                var visual = lista.Select(x => new { Nombre = x.Nombres, Facultad = x.Carrera.Facultad.Nombre, Carrera = x.Carrera.Nombre, Estado = x.Activa ? "Activa" : "Inactiva" }).ToList();
                dgvCandidatas.DataSource = visual;
            }
            catch { }
        }
    }
}