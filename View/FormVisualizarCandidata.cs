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
    /// VISTA DE DETALLE (Solo Lectura).
    /// Funciona como una "Hoja de Datos" inmutable para consultar la información
    /// completa de una candidata sin riesgo de edición accidental.
    /// Similar al patrón "Detail View" en aplicaciones móviles.
    /// </summary>
    public partial class FormVisualizarCandidata : Form 
    {
        // Entidad principal a visualizar.
        private Candidata candidataActual;
        
        // Servicios para hidratar datos relacionales.
        private CatalogoDAO catalogoDAO;
        private CandidataService candidataService;

        // Referencias a controles UI generados dinámicamente.
        private PictureBox picFoto;
        private Label lblNombre;
        private Label lblCedula;
        private Label lblEdad;
        private Label lblFacultad;
        private Label lblCarrera;
        private Label lblTipo;
        private Label lblEstado;
        private TextBox txtHabilidades;
        private TextBox txtPasatiempos;
        private TextBox txtAspiraciones;
        private Button btnCerrar;

        /// <summary>
        /// Constructor: Recibe la entidad, inicializa dependencias y construye la UI.
        /// </summary>
        public FormVisualizarCandidata(Candidata candidata)
        {
            InitializeComponent();
            this.candidataActual = candidata;

            try
            {
                catalogoDAO = new CatalogoDAO();
                candidataService = new CandidataService();

                System.Diagnostics.Debug.WriteLine($"[VisualizarCandidata] Inicializando formulario para candidata ID: {candidata?.CandidataId ?? 0}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Inicialización: {ex.Message}");
                MessageBox.Show($"Error inicializando: {ex.Message}", "Error Fatal", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            ConfigurarFormulario();
            InicializarComponentes();
            
            // Carga de datos una vez que la UI está lista.
            CargarDatosCandidata();
        }

        private void ConfigurarFormulario()
        {
            this.Text = "Detalles de Candidata - SIVUG";
            this.Size = new Size(700, 900);
            this.FormBorderStyle = FormBorderStyle.FixedDialog; // No redimensionable.
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent; // Centrado respecto al padre.
            this.BackColor = Color.FromArgb(240, 240, 245);
            this.ShowIcon = false;
        }

        /// <summary>
        /// Construcción de UI Vertical:
        /// Utiliza un FlowLayoutPanel vertical para apilar secciones de información
        /// de manera ordenada y permitir scroll automático si el contenido excede el alto.
        /// </summary>
        private void InicializarComponentes()
        {
            System.Diagnostics.Debug.WriteLine("[VisualizarCandidata] Inicializando componentes UI");

            // Panel contenedor principal.
            Panel panelPrincipal = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            this.Controls.Add(panelPrincipal);

            // FlowLayout: El motor de apilamiento vertical.
            FlowLayoutPanel flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown
            };
            panelPrincipal.Controls.Add(flowPanel);

            // 1. FOTO PRINCIPAL
            picFoto = new PictureBox
            {
                Size = new Size(200, 250),
                BackColor = Color.Gainsboro,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(10)
            };
            flowPanel.Controls.Add(picFoto);

            // 2. SECCIÓN: INFORMACIÓN PERSONAL
            Label lblTitulo = new Label
            {
                Text = "INFORMACIÓN PERSONAL",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Margin = new Padding(10, 20, 10, 10)
            };
            flowPanel.Controls.Add(lblTitulo);

            // Pares Etiqueta-Valor generados con helper.
            lblNombre = CrearLabelInfo("Nombre:", "", flowPanel);
            lblCedula = CrearLabelInfo("Cédula:", "", flowPanel);
            lblEdad = CrearLabelInfo("Edad:", "", flowPanel);
            lblFacultad = CrearLabelInfo("Facultad:", "", flowPanel);
            lblCarrera = CrearLabelInfo("Carrera:", "", flowPanel);

            // 3. SECCIÓN: DATOS DE CONCURSO
            Label lblTituloCandidatura = new Label
            {
                Text = "INFORMACIÓN DE CANDIDATURA",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Margin = new Padding(10, 20, 10, 10)
            };
            flowPanel.Controls.Add(lblTituloCandidatura);

            lblTipo = CrearLabelInfo("Tipo:", "", flowPanel);
            lblEstado = CrearLabelInfo("Estado:", "", flowPanel);

            // 4. SECCIÓN: PERFIL (TAGS)
            // Usamos TextBoxes de solo lectura para mostrar listas de texto (Habilidades, etc).
            Label lblTituloPerfil = new Label
            {
                Text = "PERFIL E INTERESES",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Margin = new Padding(10, 20, 10, 10)
            };
            flowPanel.Controls.Add(lblTituloPerfil);

            // Habilidades
            Label lblHabLabel = new Label
            {
                Text = "Habilidades:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(10, 5, 10, 2)
            };
            flowPanel.Controls.Add(lblHabLabel);

            txtHabilidades = new TextBox
            {
                Size = new Size(600, 80),
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.Fixed3D,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(10, 0, 10, 10)
            };
            flowPanel.Controls.Add(txtHabilidades);

            // Pasatiempos
            Label lblPasLabel = new Label
            {
                Text = "Pasatiempos:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(10, 5, 10, 2)
            };
            flowPanel.Controls.Add(lblPasLabel);

            txtPasatiempos = new TextBox
            {
                Size = new Size(600, 80),
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.Fixed3D,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(10, 0, 10, 10)
            };
            flowPanel.Controls.Add(txtPasatiempos);

            // Aspiraciones
            Label lblAspLabel = new Label
            {
                Text = "Aspiraciones:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(10, 5, 10, 2)
            };
            flowPanel.Controls.Add(lblAspLabel);

            txtAspiraciones = new TextBox
            {
                Size = new Size(600, 80),
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.Fixed3D,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(10, 0, 10, 10)
            };
            flowPanel.Controls.Add(txtAspiraciones);

            // BOTÓN DE CIERRE
            btnCerrar = new Button
            {
                Text = "Cerrar",
                Size = new Size(120, 40),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(10, 20, 10, 10)
            };
            btnCerrar.Click += (s, e) => this.Close();
            btnCerrar.FlatAppearance.BorderSize = 0;
            flowPanel.Controls.Add(btnCerrar);

            System.Diagnostics.Debug.WriteLine("[VisualizarCandidata] Componentes inicializados correctamente");
        }

        /// <summary>
        /// Helper UI: Genera un par Label-Valor estandarizado.
        /// </summary>
        private Label CrearLabelInfo(string etiqueta, string valor, FlowLayoutPanel panel)
        {
            Panel contenedor = new Panel
            {
                Size = new Size(600, 35),
                Margin = new Padding(10, 5, 10, 5)
            };

            Label lblEtiqueta = new Label
            {
                Text = etiqueta,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(0, 8),
                AutoSize = true,
                ForeColor = Color.DimGray
            };
            contenedor.Controls.Add(lblEtiqueta);

            Label lblValor = new Label
            {
                Text = valor,
                Font = new Font("Segoe UI", 10F),
                Location = new Point(150, 8),
                AutoSize = true,
                ForeColor = Color.Black
            };
            contenedor.Controls.Add(lblValor);

            panel.Controls.Add(contenedor);
            return lblValor;
        }

        /// <summary>
        /// Hidratación de datos: Rellena los controles con la información de la entidad.
        /// </summary>
        private void CargarDatosCandidata()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[VisualizarCandidata] Iniciando carga de datos para candidata ID: {candidataActual?.CandidataId}");

                if (candidataActual == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ERROR] Candidata es nula");
                    MessageBox.Show("Los datos de la candidata no están disponibles", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                // Datos Planos (String interpolations para formato limpio).
                lblNombre.Text = $"{candidataActual.Nombres} {candidataActual.Apellidos}";
                lblCedula.Text = candidataActual.DNI ?? "N/A";
                lblEdad.Text = $"{candidataActual.Edad} años";
                lblFacultad.Text = candidataActual.Carrera?.Facultad?.Nombre ?? "N/A";
                lblCarrera.Text = candidataActual.Carrera?.Nombre ?? "N/A";

                // Datos de Estado (Enums y booleanos).
                lblTipo.Text = candidataService.ObtenerDescripcionTipoVoto(candidataActual.tipoCandidatura);
                lblEstado.Text = candidataActual.Activa ? "Activa" : "Inactiva";

                // Carga Segura de Imagen.
                if (!string.IsNullOrEmpty(candidataActual.ImagenPrincipal) && File.Exists(candidataActual.ImagenPrincipal))
                {
                    try
                    {
                        picFoto.Image = Image.FromFile(candidataActual.ImagenPrincipal);
                    }
                    catch (Exception exFoto)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ERROR] Cargando foto: {exFoto.Message}");
                        picFoto.BackColor = Color.LightGray;
                    }
                }
                else
                {
                    picFoto.BackColor = Color.LightGray; // Placeholder
                }

                // Datos relacionales complejos (Listas).
                CargarPerfil();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Recupera y formatea los detalles del perfil (Habilidades, Pasatiempos) desde la BD.
        /// </summary>
        private void CargarPerfil()
        {
            try
            {
                var detalles = catalogoDAO.ObtenerDeCandidata(candidataActual.CandidataId);

                if (detalles == null || detalles.Count == 0)
                {
                    txtHabilidades.Text = "Sin habilidades registradas";
                    txtPasatiempos.Text = "Sin pasatiempos registrados";
                    txtAspiraciones.Text = "Sin aspiraciones registradas";
                    return;
                }

                // LINQ para clasificar los detalles planos en categorías UI.
                var habilidades = detalles.Where(d => d.Tipo == "HABILIDAD").Select(d => d.Nombre).ToList();
                var pasatiempos = detalles.Where(d => d.Tipo == "PASATIEMPO").Select(d => d.Nombre).ToList();
                var aspiraciones = detalles.Where(d => d.Tipo == "ASPIRACION").Select(d => d.Nombre).ToList();

                // Formateo de lista con viñetas para lectura fácil.
                txtHabilidades.Text = habilidades.Count > 0 ? string.Join("\r\n• ", habilidades.Prepend("•")) : "Sin habilidades registradas";
                txtPasatiempos.Text = pasatiempos.Count > 0 ? string.Join("\r\n• ", pasatiempos.Prepend("•")) : "Sin pasatiempos registrados";
                txtAspiraciones.Text = aspiraciones.Count > 0 ? string.Join("\r\n• ", aspiraciones.Prepend("•")) : "Sin aspiraciones registradas";
            }
            catch (Exception ex)
            {
                txtHabilidades.Text = "Error al cargar";
                txtPasatiempos.Text = "Error al cargar";
                txtAspiraciones.Text = "Error al cargar";
                System.Diagnostics.Debug.WriteLine($"[ERROR] CargarPerfil: {ex.Message}");
            }
        }

        private void FormVisualizarCandidata_Load(object sender, EventArgs e)
        {

        }
    }
}
