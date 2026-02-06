using SIVUG.Models;
using SIVUG.Models.DAO;
using SIVUG.Models.SERVICES;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SIVUG.View
{
    /// <summary>
    /// VISTA DE EDICIÓN: Gestiona la actualización integral del perfil de una candidata.
    /// Resuelve la complejidad de editar datos básicos, imagen y listas de atributos (Tags)
    /// en una sola pantalla unificada.
    /// </summary>
    public partial class FormEditarCandidata : Form
    {
        // Capa de Servicios y Acceso a Datos.
        // Separo la lógica de negocio (Service) de la persistencia pura (DAO).
        private CandidataService candidataService;
        private CandidataDAO candidataDAO;
        private CatalogoDAO catalogoDAO;

        // Entidad en estado de edición.
        private Candidata candidataActual;

        // Estado volátil: Ruta de imagen temporal antes de guardar.
        private string rutaImagenSeleccionada;

        // Listas temporales en memoria para manipular las colecciones del perfil
        // sin afectar el objeto real hasta que el usuario decida "Guardar".
        private List<string> _listaHabilidades = new List<string>();
        private List<string> _listaPasatiempos = new List<string>();
        private List<string> _listaAspiraciones = new List<string>();

        // --- REFERENCIAS A CONTROLES UI (Generados dinámicamente) ---
        private GroupBox grpDatos;
        private GroupBox grpPerfil;
        private Panel panelInfoCandidata;
        private Panel panelConfig;
        private Panel panelFoto;

        // Inputs de datos personales
        private TextBox txtNombres;
        private TextBox txtApellidos;
        private TextBox txtCedula;
        private TextBox txtEdad;
        private TextBox txtCarrera;
        private TextBox txtFacultad;

        // Control visual de imagen
        private PictureBox picFotoCandidato;
        private Button btnSeleccionarFoto;

        // Clasificación de la candidatura
        private CheckBox chkReina;
        private CheckBox chkFotogenia;

        // Controles complejos para gestión de listas (Tags)
        private ListBox lbHabilidades;
        private ListBox lbPasatiempos;
        private ListBox lbAspiraciones;
        private ComboBox cboHabilidad;
        private ComboBox cboPasatiempo;
        private ComboBox cboAspiracion;

        // Acciones principales
        private Button btnGuardar;
        private Button btnCancelar;

        /// <summary>
        /// Constructor: Recibe la entidad a editar.
        /// Inicializa dependencias y prepara la UI.
        /// </summary>
        public FormEditarCandidata(Candidata candidata)
        {
            InitializeComponent();
            this.candidataActual = candidata;

            try
            {
                // Inyección manual de dependencias
                candidataService = new CandidataService();
                candidataDAO = new CandidataDAO();
                catalogoDAO = new CatalogoDAO();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inicializando servicios:\n{ex.Message}", "Error Fatal",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            ConfigurarFormulario();
            InicializarComponentes();
            
            // Hydration: Lleno los campos con los datos existentes.
            CargarDatosCandidata();
        }

        private void ConfigurarFormulario()
        {
            this.Text = "SIVUG - Editar Candidata";
            this.Size = new Size(1150, 850);
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // Evito redimensionamiento para mantener diseño fijo.
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 245);
        }

        /// <summary>
        /// Construcción de la interfaz visual por código.
        /// Organizo la pantalla en secciones lógicas: Datos, Configuración y Perfil.
        /// </summary>
        private void InicializarComponentes()
        {
            // Header
            Label lblTitulo = new Label
            {
                Text = "EDITAR CANDIDATA",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Location = new Point(25, 20),
                AutoSize = true
            };
            this.Controls.Add(lblTitulo);

            // SECCIÓN 1: DATOS BÁSICOS
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

            CrearPanelInformacionCandidata(grpDatos, 20, 30);
            CrearPanelConfiguracion(grpDatos, 20, 240);
            CrearPanelFoto(grpDatos, 750, 30);

            // SECCIÓN 2: PERFIL DETALLADO (Listas dinámicas)
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

            // Columnas de gestión de tags
            CrearColumnaPerfil(grpPerfil, "Habilidades", 20,
                ref cboHabilidad, ref lbHabilidades, _listaHabilidades, "HABILIDAD");

            CrearColumnaPerfil(grpPerfil, "Pasatiempos", 370,
                ref cboPasatiempo, ref lbPasatiempos, _listaPasatiempos, "PASATIEMPO");

            CrearColumnaPerfil(grpPerfil, "Aspiraciones", 720,
                ref cboAspiracion, ref lbAspiraciones, _listaAspiraciones, "ASPIRACION");

            // SECCIÓN 3: ACCIONES
            int btnY = 620;
            btnGuardar = CrearBotonAccion("💾 Guardar Cambios", Color.FromArgb(46, 204, 113), 785, btnY);
            btnGuardar.Click += BtnGuardar_Click;
            this.Controls.Add(btnGuardar);

            btnCancelar = CrearBotonAccion("Cancelar", Color.FromArgb(231, 76, 60), 1020, btnY);
            btnCancelar.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancelar);
        }

        /// <summary>
        /// Genera los campos de texto para la información personal.
        /// </summary>
        private void CrearPanelInformacionCandidata(GroupBox padre, int x, int y)
        {
            panelInfoCandidata = new Panel { Location = new Point(x, y), Size = new Size(700, 190), BackColor = Color.FromArgb(248, 249, 250) };
            // Borde custom para separar visualmente.
            panelInfoCandidata.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, panelInfoCandidata.ClientRectangle, Color.LightGray, ButtonBorderStyle.Solid);
            padre.Controls.Add(panelInfoCandidata);

            Label lblTitulo = new Label { Text = "INFORMACIÓN DE LA CANDIDATA", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true, ForeColor = Color.DimGray };
            panelInfoCandidata.Controls.Add(lblTitulo);

            // Input Fields
            Label lblNombres = new Label { Text = "Nombres:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(10, 35), AutoSize = true };
            panelInfoCandidata.Controls.Add(lblNombres);
            txtNombres = new TextBox { Location = new Point(100, 33), Size = new Size(250, 25), Font = new Font("Segoe UI", 10F) };
            panelInfoCandidata.Controls.Add(txtNombres);

            Label lblApellidos = new Label { Text = "Apellidos:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(10, 65), AutoSize = true };
            panelInfoCandidata.Controls.Add(lblApellidos);
            txtApellidos = new TextBox { Location = new Point(100, 63), Size = new Size(250, 25), Font = new Font("Segoe UI", 10F) };
            panelInfoCandidata.Controls.Add(txtApellidos);

            // Cédula de solo lectura (Identificador inmutable)
            Label lblCedula = new Label { Text = "Cédula:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(10, 95), AutoSize = true };
            panelInfoCandidata.Controls.Add(lblCedula);
            txtCedula = new TextBox { Location = new Point(100, 93), Size = new Size(250, 25), Font = new Font("Segoe UI", 10F), ReadOnly = true };
            panelInfoCandidata.Controls.Add(txtCedula);

            // Validación numérica para edad
            Label lblEdad = new Label { Text = "Edad:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(10, 125), AutoSize = true };
            panelInfoCandidata.Controls.Add(lblEdad);
            txtEdad = new TextBox { Location = new Point(100, 123), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            txtEdad.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            panelInfoCandidata.Controls.Add(txtEdad);

            // Facultad y Carrera (Solo lectura, vienen del registro de estudiante)
            Label lblFacultad = new Label { Text = "Facultad:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(380, 35), AutoSize = true };
            panelInfoCandidata.Controls.Add(lblFacultad);
            txtFacultad = new TextBox { Location = new Point(470, 33), Size = new Size(200, 25), Font = new Font("Segoe UI", 10F), ReadOnly = true };
            panelInfoCandidata.Controls.Add(txtFacultad);

            Label lblCarrera = new Label { Text = "Carrera:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(380, 65), AutoSize = true };
            panelInfoCandidata.Controls.Add(lblCarrera);
            txtCarrera = new TextBox { Location = new Point(470, 63), Size = new Size(200, 25), Font = new Font("Segoe UI", 10F), ReadOnly = true };
            panelInfoCandidata.Controls.Add(txtCarrera);
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

            btnSeleccionarFoto = new Button { Text = "📁 Cambiar Foto", Location = new Point(25, 235), Size = new Size(200, 35), BackColor = Color.FromArgb(155, 89, 182), ForeColor = Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnSeleccionarFoto.Click += BtnSeleccionarFoto_Click;
            panelFoto.Controls.Add(btnSeleccionarFoto);
        }

        /// <summary>
        /// Componente Complejo Reutilizable: Genera una columna completa para gestionar una lista de strings (Tags).
        /// Incluye ComboBox con autocompletado, Botón Agregar y ListBox para visualizar/eliminar.
        /// </summary>
        private void CrearColumnaPerfil(GroupBox padre, string titulo, int x, ref ComboBox cboInput, ref ListBox lbLista, List<string> fuenteDatos, string tipoBD)
        {
            // 1. Etiqueta
            Label lbl = new Label { Text = titulo, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(x, 25), AutoSize = true };
            padre.Controls.Add(lbl);

            // 2. ComboBox (Input + Sugerencias)
            ComboBox cbo = new ComboBox
            {
                Location = new Point(x, 45),
                Size = new Size(220, 25),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDown, // Permite escribir texto nuevo o elegir
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };

            // Carga de catálogo para sugerencias
            CargarDatosCombo(cbo, tipoBD);
            padre.Controls.Add(cbo);
            cboInput = cbo;

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

            // 4. ListBox (Visualizador de selección)
            ListBox lb = new ListBox { Location = new Point(x, 80), Size = new Size(260, 100), Font = new Font("Segoe UI", 9F), BorderStyle = BorderStyle.FixedSingle };
            padre.Controls.Add(lb);
            lbLista = lb;

            // --- LÓGICA DE EVENTOS ---
            btnAdd.Click += (s, e) =>
            {
                string valor = cbo.Text.Trim();

                if (string.IsNullOrEmpty(valor))
                {
                    MessageBox.Show("Escriba un valor antes de agregar", "Validación");
                    return;
                }

                // Evitar duplicados en la lista visual
                if (fuenteDatos.Contains(valor, StringComparer.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Este elemento ya está en la lista", "Duplicado");
                    return;
                }

                // Agregar a la lista en memoria y refrescar UI
                fuenteDatos.Add(valor);
                ActualizarListBox(lb, fuenteDatos);

                // Agregar al combo dinámicamente si es un valor nuevo
                if (!cbo.Items.Contains(valor))
                {
                    cbo.Items.Add(valor);
                }

                cbo.Text = "";
                cbo.Focus();
            };

            // UX: Permitir agregar con tecla ENTER
            cbo.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { btnAdd.PerformClick(); e.Handled = true; } };
            
            // UX: Doble clic para eliminar de la lista
            lb.DoubleClick += (s, e) => { if (lb.SelectedIndex != -1) { fuenteDatos.RemoveAt(lb.SelectedIndex); ActualizarListBox(lb, fuenteDatos); } };
        }

        private void CargarDatosCombo(ComboBox cbo, string tipo)
        {
            try
            {
                cbo.Items.Clear();

                if (catalogoDAO == null)
                {
                    MessageBox.Show($"Error: CatalogoDAO no está inicializado", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show($"Error cargando datos de '{tipo}':\n{ex.Message}", "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ActualizarListBox(ListBox lb, List<string> datos)
        {
            // Reset del datasource para refrescar cambios
            lb.DataSource = null;
            lb.DataSource = datos;
        }

        private Button CrearBotonAccion(string texto, Color color, int x, int y)
        {
            Button btn = new Button { Text = texto, Location = new Point(x, y), Size = new Size(110, 40), BackColor = color, ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        /// <summary>
        /// HYDRATION: Llena la pantalla con los datos actuales de la candidata.
        /// Asegura que el usuario vea lo que ya existe antes de editar.
        /// </summary>
        private void CargarDatosCandidata()
        {
            if (candidataActual == null) return;

            try
            {
                // Datos básicos
                txtNombres.Text = candidataActual.Nombres ?? "";
                txtApellidos.Text = candidataActual.Apellidos ?? "";
                txtCedula.Text = candidataActual.DNI ?? "";
                txtEdad.Text = candidataActual.Edad.ToString();

                if (candidataActual.Carrera != null)
                {
                    txtFacultad.Text = candidataActual.Carrera.Facultad?.Nombre ?? "";
                    txtCarrera.Text = candidataActual.Carrera.Nombre ?? "";
                }

                // Carga segura de imagen
                if (!string.IsNullOrEmpty(candidataActual.ImagenPrincipal) && File.Exists(candidataActual.ImagenPrincipal))
                    picFotoCandidato.Image = Image.FromFile(candidataActual.ImagenPrincipal);

                // Mapeo de Enums a Checkboxes
                chkReina.Checked = candidataActual.tipoCandidatura == TipoVoto.Reina;
                chkFotogenia.Checked = candidataActual.tipoCandidatura == TipoVoto.Fotogenia;

                // REGLA CRÍTICA: No reemplazar las listas, sino limpiarlas y repoblar.
                // Esto mantiene la referencia en memoria consistente.
                _listaHabilidades.Clear();
                _listaPasatiempos.Clear();
                _listaAspiraciones.Clear();

                if (candidataActual.Habilidades != null)
                    _listaHabilidades.AddRange(candidataActual.Habilidades);

                if (candidataActual.Pasatiempos != null)
                    _listaPasatiempos.AddRange(candidataActual.Pasatiempos);

                if (candidataActual.Aspiraciones != null)
                    _listaAspiraciones.AddRange(candidataActual.Aspiraciones);

                ActualizarListBox(lbHabilidades, _listaHabilidades);
                ActualizarListBox(lbPasatiempos, _listaPasatiempos);
                ActualizarListBox(lbAspiraciones, _listaAspiraciones);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando candidata: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
        /// Lógica central de Persistencia.
        /// Valida, procesa y guarda todos los cambios en transacción.
        /// </summary>
        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (candidataActual == null) return;

            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(txtNombres.Text) || string.IsNullOrWhiteSpace(txtApellidos.Text))
            {
                MessageBox.Show("Los nombres y apellidos son requeridos.", "Validación");
                return;
            }

            if (!chkReina.Checked && !chkFotogenia.Checked)
            {
                MessageBox.Show("Seleccione al menos un tipo de candidatura.", "Validación");
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor; // Feedback visual de proceso largo

                // Mapeo los valores de la UI al objeto
                candidataActual.Nombres = txtNombres.Text.Trim();
                candidataActual.Apellidos = txtApellidos.Text.Trim();

                if (byte.TryParse(txtEdad.Text, out byte edad))
                {
                    candidataActual.Edad = edad;
                }

                // Manejo de archivo: Si seleccionó nueva foto, la guardo en carpeta local.
                if (!string.IsNullOrEmpty(rutaImagenSeleccionada))
                {
                    string rutaDestino = GuardarImagenCandidato();
                    if (!string.IsNullOrEmpty(rutaDestino))
                    {
                        candidataActual.ImagenPrincipal = rutaDestino;
                    }
                }

                // Determino Tipo de Candidatura
                if (chkReina.Checked)
                {
                    candidataActual.tipoCandidatura = TipoVoto.Reina;
                }
                else if (chkFotogenia.Checked)
                {
                    candidataActual.tipoCandidatura = TipoVoto.Fotogenia;
                }

                // Actualizo las listas en el objeto principal
                candidataActual.Habilidades = new List<string>(_listaHabilidades);
                candidataActual.Pasatiempos = new List<string>(_listaPasatiempos);
                candidataActual.Aspiraciones = new List<string>(_listaAspiraciones);

                // Llamada al servicio para persistir cambios
                if (candidataService.ActualizarCandidata(candidataActual))
                {
                    // Lógica relacional: Actualizo los detalles (muchos a muchos) en paralelo.
                    if (_listaHabilidades.Count > 0 || _listaPasatiempos.Count > 0 || _listaAspiraciones.Count > 0)
                    {
                        catalogoDAO.ActualizarDetalles(
                            candidataActual.CandidataId,
                            _listaHabilidades,
                            _listaPasatiempos,
                            _listaAspiraciones
                        );
                    }

                    MessageBox.Show("Candidata actualizada exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK; // Devuelvo OK para que la ventana padre refresque.
                    this.Close();
                }
                else
                {
                    MessageBox.Show("No se pudo actualizar la candidata.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error: {ex.Message}", "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Guarda fisicamente el archivo de imagen en la carpeta local de la aplicación.
        /// Genera nombres únicos para evitar colisiones.
        /// </summary>
        private string GuardarImagenCandidato()
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImagenesCandidatas");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Nombre único usando timestamp
                string nombre = $"cand_{candidataActual.DNI}_{DateTime.Now.Ticks}{Path.GetExtension(rutaImagenSeleccionada)}";
                string dest = Path.Combine(dir, nombre);
                File.Copy(rutaImagenSeleccionada, dest, true);
                return dest;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error guardando imagen: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
    
        private void FormEditarCandidata_Load(object sender, EventArgs e)
        {

        }
    }
}
