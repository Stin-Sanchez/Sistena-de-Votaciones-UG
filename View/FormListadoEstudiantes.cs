using SIVUG.Models;
using SIVUG.Models.DAO;
using SIVUG.Models.DTOS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SIVUG.View
{
    /// <summary>
    /// VISTA MAESTRA DE ESTUDIANTES.
    /// Responsabilidades:
    /// - Cargar y mostrar grandes volúmenes de datos.
    /// - Filtrar en tiempo real (Client-side) para evitar recargas constantes.
    /// - Gestionar la navegación hacia detalles y formularios de creación.
    /// </summary>
    public partial class FormListadoEstudiantes : Form
    {
        // CACHÉ DE DATOS: 
        // Mantengo la lista completa en RAM para que los filtros sean instantáneos.
        // Esto reduce drásticamente las consultas a la base de datos (Performance).
        private List<Estudiante> _listaOriginal;

        // Capa de acceso a datos.
        private EstudianteDAO _estDAO;
        private FacultadDAO _facDAO;
        private CarreraDAO _carDAO;

        // Banderas de estado para controlar el flujo de eventos de UI.
        private bool _cargando = false;

        // Optimización: Timer para implementar "Debounce" en la búsqueda por texto.
        // Evita filtrar con cada tecla pulsada, esperando a que el usuario termine de escribir.
        private System.Windows.Forms.Timer _searchTimer;

        // Controles de UI construidos por código (Mejor control del layout).
        private TableLayoutPanel mainLayout;
        private Panel headerPanel;
        private Panel filterPanel;
        private Panel contentPanel;
        private Panel footerPanel;
        private Label lblTitle;
        private Label lblResultCount;
        private Label lblFacultad;
        private Label lblCarrera;
        private Label lblBuscarDNI;
        private ComboBox cmbFacultad;
        private ComboBox cmbCarrera;
        private TextBox txtBuscarDNI;
        private Button btnLimpiarFiltros;
        private Button btnAgregar;
        private Button btnRefrescar;
        private DataGridView dgvEstudiantes;
        private ProgressBar progressBar;

        /// <summary>
        /// Constructor: Configura dependencias y prepara el entorno visual.
        /// </summary>
        public FormListadoEstudiantes()
        {
            InitializeComponent();
            
            // Construcción del Layout responsivo.
            InicializarComponentesPersonalizados();
            ConfigurarEstilos();

            _estDAO = new EstudianteDAO();
            _facDAO = new FacultadDAO();
            _carDAO = new CarreraDAO();

            // Configuración del Debounce: 300ms de espera tras la última tecla.
            _searchTimer = new System.Windows.Forms.Timer();
            _searchTimer.Interval = 300;
            _searchTimer.Tick += SearchTimer_Tick;
        }

        /// <summary>
        /// Método masivo de construcción de UI.
        /// Divide la pantalla en 4 áreas: Header, Filtros, Contenido (Grid) y Footer.
        /// </summary>
        private void InicializarComponentesPersonalizados()
        {
            this.SuspendLayout();

            // Configuración base de la ventana.
            this.Text = "Gestión de Estudiantes";
            this.MinimumSize = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;

            // Layout Principal (Grilla de 4 filas).
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(240, 240, 245)
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));  // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100)); // Filtros
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Grid (Flexible)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));  // Footer

            // === HEADER PANEL ===
            headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20, 15, 20, 15)
            };

            lblTitle = new Label
            {
                Text = "Listado de Estudiantes",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                Dock = DockStyle.Left,
                AutoSize = true
            };

            lblResultCount = new Label
            {
                Text = "0 estudiantes",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(108, 117, 125),
                Dock = DockStyle.Bottom,
                AutoSize = true
            };

            headerPanel.Controls.AddRange(new Control[] { lblTitle, lblResultCount });

            // === FILTER PANEL ===
            filterPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(15),
                Margin = new Padding(0, 10, 0, 0)
            };

            TableLayoutPanel filterLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                AutoSize = true
            };

            // Columnas de filtros.
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F)); // Buscador Texto
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F)); // Icono
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F)); // Combo Facultad
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F)); // Combo Carrera

            // 1. Buscador
            lblBuscarDNI = new Label
            {
                Text = "Buscar por DNI / Nombre:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 5)
            };
            txtBuscarDNI = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11),
                Margin = new Padding(0, 0, 10, 0)
            };
            Panel panelBusqueda = CrearPanelFiltro(lblBuscarDNI, txtBuscarDNI);

            // 2. Icono Separador
            FlowLayoutPanel panelIconoCentro = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(10, 25, 0, 0),
                AutoSize = true
            };

            PictureBox pbFilterIcon = new PictureBox
            {
                Image = Properties.Resources.icons8_filtrar_20__1_, 
                Size = new Size(20, 20),
                SizeMode = PictureBoxSizeMode.Zoom,
                Margin = new Padding(0, 2, 5, 0) 
            };

            Label lblFilterText = new Label
            {
                Text = "Filtros:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold | FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = true,                                                                                                                                                                                                                                                                
                Padding = new Padding(0, 2, 0, 0) 
            };

            panelIconoCentro.Controls.Add(pbFilterIcon);
            panelIconoCentro.Controls.Add(lblFilterText);

            // 3. Filtro Facultad
            lblFacultad = new Label
            {
                Text = "Facultad:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 5)
            };
            cmbFacultad = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(0, 0, 10, 0)
            };
            Panel panelFacultad = CrearPanelFiltro(lblFacultad, cmbFacultad);

            // 4. Filtro Carrera
            lblCarrera = new Label
            {
                Text = "Carrera:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 5)
            };
            cmbCarrera = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            Panel panelCarrera = CrearPanelFiltro(lblCarrera, cmbCarrera);

            filterLayout.Controls.Add(panelBusqueda, 0, 0);
            filterLayout.Controls.Add(panelIconoCentro, 1, 0);
            filterLayout.Controls.Add(panelFacultad, 2, 0);
            filterLayout.Controls.Add(panelCarrera, 3, 0);

            // Botón Reset
            btnLimpiarFiltros = new Button
            {
                Text = "Limpiar Filtros",
                AutoSize = true,
                Font = new Font("Segoe UI", 8),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatAppearance = { BorderSize = 0 }
            };

            FlowLayoutPanel btnLimpiarWrapper = new FlowLayoutPanel
            {
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 0)
            };
            btnLimpiarWrapper.Controls.Add(btnLimpiarFiltros);

            filterPanel.Controls.Add(filterLayout);
            filterPanel.Controls.Add(btnLimpiarWrapper);

            btnLimpiarWrapper.Dock = DockStyle.Bottom;
            filterLayout.Dock = DockStyle.Top;


            // === CONTENT PANEL (GRID) ===
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20),
                Margin = new Padding(0, 10, 0, 0)
            };

            dgvEstudiantes = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 9)
            };
            // Styling avanzado del Grid.
            dgvEstudiantes.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 58, 64);
            dgvEstudiantes.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvEstudiantes.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvEstudiantes.ColumnHeadersHeight = 40;
            dgvEstudiantes.RowTemplate.Height = 35;

            progressBar = new ProgressBar { Dock = DockStyle.Bottom, Height = 3, Style = ProgressBarStyle.Marquee, Visible = false };
            contentPanel.Controls.AddRange(new Control[] { dgvEstudiantes, progressBar });

            // === FOOTER PANEL (ACCIONES) ===
            footerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 15, 0, 0)
            };

            FlowLayoutPanel footerLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            // Botón Agregar
            btnAgregar = new Button
            {
                Text = " Agregar Estudiante",
                AutoSize = true,
                Height = 45, 
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleLeft, 
                TextAlign = ContentAlignment.MiddleLeft, 
                Padding = new Padding(12, 0, 12, 0),     
                Image = Properties.Resources.icons8_añadir_20
            };
            btnAgregar.FlatAppearance.BorderSize = 0;

            // Botón Refrescar
            btnRefrescar = new Button
            {
                Text = " Refrescar",
                AutoSize = true,
                Height = 45,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 10, 0),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleLeft, 
                Padding = new Padding(15, 0, 15, 0),
                Image = Properties.Resources.icons8_actualizar_20
            };
            btnRefrescar.FlatAppearance.BorderSize = 0;

            footerLayout.Controls.AddRange(new Control[] { btnAgregar, btnRefrescar });
            footerPanel.Controls.Add(footerLayout);

            // Ensamblaje final de paneles.
            mainLayout.Controls.Add(headerPanel, 0, 0);
            mainLayout.Controls.Add(filterPanel, 0, 1);
            mainLayout.Controls.Add(contentPanel, 0, 2);
            mainLayout.Controls.Add(footerPanel, 0, 3);

            this.Controls.Add(mainLayout);

            // Suscripción a eventos.
            cmbFacultad.SelectedIndexChanged += cmbFacultad_SelectedIndexChanged;
            cmbCarrera.SelectedIndexChanged += cmbCarrera_SelectedIndexChanged;
            txtBuscarDNI.TextChanged += txtBuscarDNI_TextChanged;
            btnLimpiarFiltros.Click += btnLimpiarFiltros_Click;
            btnAgregar.Click += btnAdd_Click;
            btnRefrescar.Click += btnRefrescar_Click;
            this.Load += FormListadoEstudiantes_Load;
            this.Resize += FormListadoEstudiantes_Resize;

            this.ResumeLayout(false);
        }

        private Panel CrearPanelFiltro(Label label, Control control)
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 10, 0)
            };
            label.Dock = DockStyle.Top;
            control.Dock = DockStyle.Top;
            panel.Controls.AddRange(new Control[] { control, label });
            return panel;
        }

        private void ConfigurarEstilos()
        {
            // Tooltips de ayuda UX.
            ToolTip toolTip = new ToolTip();
            toolTip.SetToolTip(cmbFacultad, "Seleccione una facultad para filtrar");
            toolTip.SetToolTip(cmbCarrera, "Seleccione una carrera para filtrar");
            toolTip.SetToolTip(txtBuscarDNI, "Búsqueda en tiempo real por DNI");
            toolTip.SetToolTip(btnLimpiarFiltros, "Restablecer todos los filtros");
            toolTip.SetToolTip(btnRefrescar, "Actualizar datos desde la base de datos");
            toolTip.SetToolTip(btnAgregar, "Registrar un nuevo estudiante");
        }

        private void FormListadoEstudiantes_Load(object sender, EventArgs e)
        {
            CargarDatosAsync();
        }

        /// <summary>           
        /// Carga asíncrona de datos para no congelar la UI.
        /// </summary>
        private async void CargarDatosAsync()
        {
            _cargando = true;
            MostrarCargando(true);

            try
            {
                await Task.Run(() =>
                {
                    // Update de UI thread-safe.
                    this.Invoke((MethodInvoker)delegate
                    {
                        CargarComboFacultades();
                    });

                    // Carga pesada en background thread.
                    var listaBase = _estDAO.ObtenerTodosDetallado();
                    
                    // Mapeo a objeto local optimizado.
                    _listaOriginal = listaBase.Select(e => new Estudiante
                    {
                        Matricula = e.Matricula,
                        Semestre = e.Semestre,
                        IdCarrera = e.IdCarrera,
                        Carrera = e.Carrera,
                        HavotadoReina = e.HavotadoReina,
                        HavotadoFotogenia = e.HavotadoFotogenia,
                        FotoPerfilRuta = e.FotoPerfilRuta,
                
                        IdUsuario = e.Usuario.IdUsuario,
                        DNI = e.DNI,
                        Nombres = e.Nombres,
                        Apellidos = e.Apellidos,
                        Edad = e.Edad
                    }).ToList();
                });

                AplicarFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los datos: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                MostrarCargando(false);
                _cargando = false;
            }
        }

        private void MostrarCargando(bool mostrar)
        {
            progressBar.Visible = mostrar;
            dgvEstudiantes.Enabled = !mostrar;
        }

        private void CargarComboFacultades()
        {
            var lista = _facDAO.ObtenerTodas();
            lista.Insert(0, new Facultad { Id = 0, Nombre = "Todas las Facultades" });

            cmbFacultad.DataSource = lista;
            cmbFacultad.DisplayMember = "Nombre";
            cmbFacultad.ValueMember = "Id";

            // Carga en cascada.
            CargarComboCarreras(0);
        }

        private void CargarComboCarreras(int idFacultad)
        {
            List<Carrera> lista;
            if (idFacultad == 0)
            {
                lista = new List<Carrera>();
            }
            else
            {
                lista = _carDAO.ObtenerPorIdFacultad(idFacultad);
            }
            lista.Insert(0, new Carrera { Id = 0, Nombre = "Todas las Carreras" });

            cmbCarrera.DataSource = lista;
            cmbCarrera.DisplayMember = "Nombre";
            cmbCarrera.ValueMember = "Id";
        }

        private void cmbFacultad_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cargando) return;

            int idFac = (int)cmbFacultad.SelectedValue;

            _cargando = true; // Bloqueo temporal para evitar recursividad
            CargarComboCarreras(idFac);
            _cargando = false;

            AplicarFiltros();
        }

        private void cmbCarrera_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cargando) return;
            AplicarFiltros();
        }

        private void txtBuscarDNI_TextChanged(object sender, EventArgs e)
        {
            // Reset del Timer para esperar inactividad.
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            // Ejecución diferida de la búsqueda.
            _searchTimer.Stop();
            AplicarFiltros();
        }

        private void btnLimpiarFiltros_Click(object sender, EventArgs e)
        {
            _cargando = true;
            cmbFacultad.SelectedIndex = 0;
            txtBuscarDNI.Clear();
            _cargando = false;
            AplicarFiltros();
        }

        private void btnRefrescar_Click(object sender, EventArgs e)
        {
            CargarDatosAsync();
        }

        /// <summary>
        /// Motor de filtrado en memoria usando LINQ.
        /// Actualiza el DataGridView sin tocar la base de datos.
        /// </summary>
        private void AplicarFiltros()
        {
            if (_listaOriginal == null) return;

            var resultado = _listaOriginal.AsEnumerable();

            // Filtro 1: Facultad
            if (cmbFacultad.SelectedValue != null)
            {
                int idFac = (int)cmbFacultad.SelectedValue;
                if (idFac > 0)
                {
                    resultado = resultado.Where(x => x.Carrera.Facultad.Id == idFac);
                }
            }

            // Filtro 2: Carrera
            if (cmbCarrera.SelectedValue != null)
            {
                int idCar = (int)cmbCarrera.SelectedValue;
                if (idCar > 0)
                {
                    resultado = resultado.Where(x => x.IdCarrera == idCar);
                }
            }

            // Filtro 3: Texto (DNI/Nombre)
            string textoDNI = txtBuscarDNI.Text.Trim();
            if (!string.IsNullOrEmpty(textoDNI))
            {
                // Búsqueda "Contains" flexible.
                resultado = resultado.Where(x => x.DNI.Contains(textoDNI));
            }

            var listaFiltrada = resultado.ToList();

            ActualizarContador(listaFiltrada.Count);

            // Proyección anónima para mostrar solo las columnas relevantes en el Grid.
            dgvEstudiantes.DataSource = listaFiltrada.Select(x => new
            {
                Matrícula = x.Matricula,
                DNI = x.DNI,
                Nombres = x.Nombres,
                Apellidos = x.Apellidos,
                Edad = x.Edad,
                Facultad = x.Carrera.Facultad.Nombre,
                Carrera = x.Carrera.Nombre,
                Semestre = x.Semestre
            }).ToList();

            // Ajuste fino de columnas.
            if (dgvEstudiantes.Columns.Count > 0)
            {
                dgvEstudiantes.Columns["Matrícula"].Width = 100;
                dgvEstudiantes.Columns["DNI"].Width = 100;
                dgvEstudiantes.Columns["Edad"].Width = 60;
                dgvEstudiantes.Columns["Semestre"].Width = 80;
            }
        }

        private void ActualizarContador(int count)
        {
            lblResultCount.Text = count == 1
                ? "1 estudiante encontrado"
                : $"{count} estudiantes encontrados";
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            FormRegistro registro = new FormRegistro();
            registro.ShowDialog(); 

            // Si se creó exitosamente, recargo la data.
            if (registro.DialogResult == DialogResult.OK)
            {
                CargarDatosAsync();
            }
        }

        private void FormListadoEstudiantes_Resize(object sender, EventArgs e)
        {
            // Responsive manual: Ajuste de fuentes según tamaño de ventana.
            if (this.Width < 800)
            {
                lblTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            }
            else
            {
                lblTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            }
        }
    }
}

