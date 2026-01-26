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

    public partial class FormListadoEstudiantes : Form
    {
        // Lista Maestra (Mantiene TODOS los datos originales)
        private List<Estudiante> _listaOriginal;

        // DAOs
        private EstudianteDAO _estDAO;
        private FacultadDAO _facDAO;
        private CarreraDAO _carDAO;

        // Banderas para evitar disparos de eventos durante la carga inicial
        private bool _cargando = false;

        // Timer para debounce en búsqueda de texto
        private System.Windows.Forms.Timer _searchTimer;

        // Controles del formulario
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

        public FormListadoEstudiantes()
        {
            InitializeComponent();
            InicializarComponentesPersonalizados();
            ConfigurarEstilos();

            _estDAO = new EstudianteDAO();
            _facDAO = new FacultadDAO();
            _carDAO = new CarreraDAO();

            // Configurar timer para búsqueda con debounce (300ms)
            _searchTimer = new System.Windows.Forms.Timer();
            _searchTimer.Interval = 300;
            _searchTimer.Tick += SearchTimer_Tick;
        }

        private void InicializarComponentesPersonalizados()
        {
            this.SuspendLayout();

            // Configuración del Form
            this.Text = "Gestión de Estudiantes";
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;

            // Layout principal - Responsive
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(240, 240, 245)
            };

            // Configurar filas: Header (auto), Filters (auto), Content (100%), Footer (auto)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

            // === HEADER PANEL ===
            headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20, 15, 20, 15)
            };

            lblTitle = new Label
            {
                Text = "📚 Listado de Estudiantes",
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
                Padding = new Padding(20),
                Margin = new Padding(0, 10, 0, 0)
            };

            // Layout para filtros - 3 columnas responsive
            TableLayoutPanel filterLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                AutoSize = true
            };

            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            filterLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            filterLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Facultad
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

            // Carrera
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
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(0, 0, 10, 0)
            };
            Panel panelCarrera = CrearPanelFiltro(lblCarrera, cmbCarrera);

            // Búsqueda DNI
            lblBuscarDNI = new Label
            {
                Text = "Buscar por DNI:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 5)
            };
            txtBuscarDNI = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(0, 0, 10, 0)
            };
            Panel panelBusqueda = CrearPanelFiltro(lblBuscarDNI, txtBuscarDNI);

            filterLayout.Controls.Add(panelFacultad, 0, 0);
            filterLayout.Controls.Add(panelCarrera, 1, 0);
            filterLayout.Controls.Add(panelBusqueda, 2, 0);

            // Botón limpiar filtros
            btnLimpiarFiltros = new Button
            {
                Text = "🔄 Limpiar Filtros",
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                Padding = new Padding(15, 8, 15, 8),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White
            };
            btnLimpiarFiltros.FlatAppearance.BorderSize = 0;
            btnLimpiarFiltros.Margin = new Padding(0, 10, 0, 0);

            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true
            };
            buttonPanel.Controls.Add(btnLimpiarFiltros);

            filterLayout.Controls.Add(buttonPanel, 0, 1);
            filterLayout.SetColumnSpan(buttonPanel, 3);

            filterPanel.Controls.Add(filterLayout);

            // === CONTENT PANEL (DataGridView) ===
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

            // Estilos del DataGridView
            dgvEstudiantes.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 58, 64);
            dgvEstudiantes.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvEstudiantes.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvEstudiantes.ColumnHeadersDefaultCellStyle.Padding = new Padding(5);
            dgvEstudiantes.ColumnHeadersHeight = 40;
            dgvEstudiantes.RowTemplate.Height = 35;
            dgvEstudiantes.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);

            progressBar = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 3,
                Style = ProgressBarStyle.Marquee,
                Visible = false,
                MarqueeAnimationSpeed = 30
            };

            contentPanel.Controls.AddRange(new Control[] { dgvEstudiantes, progressBar });

            // === FOOTER PANEL ===
            footerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 10, 0, 0)
            };

            FlowLayoutPanel footerLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            btnAgregar = new Button
            {
                Text = "➕ Agregar Estudiante",
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Padding = new Padding(20, 10, 20, 10),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White
            };
            btnAgregar.FlatAppearance.BorderSize = 0;

            btnRefrescar = new Button
            {
                Text = "🔄 Refrescar",
                AutoSize = true,
                Font = new Font("Segoe UI", 10),
                Padding = new Padding(20, 10, 20, 10),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnRefrescar.FlatAppearance.BorderSize = 0;

            footerLayout.Controls.AddRange(new Control[] { btnAgregar, btnRefrescar });
            footerPanel.Controls.Add(footerLayout);

            // Agregar todo al layout principal
            mainLayout.Controls.Add(headerPanel, 0, 0);
            mainLayout.Controls.Add(filterPanel, 0, 1);
            mainLayout.Controls.Add(contentPanel, 0, 2);
            mainLayout.Controls.Add(footerPanel, 0, 3);

            this.Controls.Add(mainLayout);

            // Eventos
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
            // Configurar tooltips
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

        private async void CargarDatosAsync()
        {
            _cargando = true;
            MostrarCargando(true);

            try
            {
                // Simular carga asíncrona para mejor UX
                await Task.Run(() =>
                {
                    // A. Cargar Combos
                    this.Invoke((MethodInvoker)delegate
                    {
                        CargarComboFacultades();
                    });

                    // B. Traer TODOS los datos de la BD
                    _listaOriginal = _estDAO.ObtenerTodosDetallado();
                });

                // C. Mostrar todo inicialmente
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

            _cargando = true;
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
            // Implementar debounce para búsqueda en tiempo real
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
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

        private void AplicarFiltros()
        {
            if (_listaOriginal == null) return;

            var resultado = _listaOriginal.AsEnumerable();

            // 1. Filtro por Facultad
            if (cmbFacultad.SelectedValue != null)
            {
                int idFac = (int)cmbFacultad.SelectedValue;
                if (idFac > 0)
                {
                    resultado = resultado.Where(x => x.Carrera.Facultad.Id == idFac);
                }
            }

            // 2. Filtro por Carrera
            if (cmbCarrera.SelectedValue != null)
            {
                int idCar = (int)cmbCarrera.SelectedValue;
                if (idCar > 0)
                {
                    resultado = resultado.Where(x => x.IdCarrera == idCar);
                }
            }

            // 3. Filtro por DNI (Búsqueda parcial)
            string textoDNI = txtBuscarDNI.Text.Trim();
            if (!string.IsNullOrEmpty(textoDNI))
            {
                resultado = resultado.Where(x => x.DNI.Contains(textoDNI));
            }

            // Convertir a lista
            var listaFiltrada = resultado.ToList();

            // Actualizar contador
            ActualizarContador(listaFiltrada.Count);

            // Renderizar en grid
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

            // Ajustar anchos de columnas
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
            registro.ShowDialog(); // Usar ShowDialog para modal

            // Refrescar después de cerrar
            if (registro.DialogResult == DialogResult.OK)
            {
                CargarDatosAsync();
            }
        }

        private void FormListadoEstudiantes_Resize(object sender, EventArgs e)
        {
            // Ajustar layout en dispositivos pequeños
            if (this.Width < 800)
            {
                // Modo compacto
                lblTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            }
            else
            {
                lblTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            }
        }


    }
}

