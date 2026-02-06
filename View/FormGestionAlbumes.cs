using SIVUG.Models;
using SIVUG.Models.DAO;
using SIVUG.Models.DTOS;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq; 
using System.Windows.Forms;

namespace SIVUG.View
{
    /// <summary>
    /// CONTROLADOR DE VISTA DE GESTIÓN DE CONTENIDO.
    /// Permite crear álbumes y subir fotos. 
    /// Implementa lógica de seguridad dual:
    /// - Admin: Puede editar el álbum de CUALQUIER candidata (seleccionándola de un combo).
    /// - Candidata: Solo puede editar SU PROPIO álbum (auto-selección).
    /// </summary>
    public partial class FormGestionAlbumes : Form
    {
        // Contexto de trabajo: Quién es la dueña del álbum actual.
        private Candidata _candidataActual;
        
        // DAOs para persistencia.
        private AlbumDAO _albumDAO;
        private CandidataDAO _candidataDAO; 
        
        // Estado temporal de edición (Transaction-like behavior en memoria).
        private Album _albumEnEdicion;
        private List<Foto> _fotosNuevasTemp;

        // Estilos visuales consistentes.
        private Color colorPrimario = Color.FromArgb(255, 105, 180);
        private Color colorFondo = Color.FromArgb(245, 246, 250);

        // Referencias a controles de UI.
        private ComboBox cboCandidatas; // Selector exclusivo para Admin.
        private Panel panelContenido;   // Contenedor 'Main' para habilitar/deshabilitar.
        private ListBox listAlbumes;
        private TextBox txtTitulo;
        private TextBox txtDescripcion;
        private FlowLayoutPanel flowFotos;
        private Button btnGuardar;
        private Button btnNuevo; 

        // Bandera de seguridad (Role-Based Flag).
        private bool _esModoAdmin;

        /// <summary>
        /// Constructor inteligente: 
        /// Detecta automáticamente el rol del usuario y ajusta la interfaz.
        /// Si es candidata, intenta auto-vincular su perfil. Si falla, bloquea el acceso.
        /// </summary>
        public FormGestionAlbumes()
        {
            InitializeComponent();
            _albumDAO = new AlbumDAO();
            _candidataDAO = new CandidataDAO();

            // 1. DETERMINACIÓN DEL ROL
            var rol = Sesion.UsuarioActual.Rol.Nombre;
            _esModoAdmin = (rol == "Administrador"); 

            ConfigurarFormulario();

            // 2. LÓGICA DE AUTO- VINCULACIÓN (Solo Candidatas)
            if (!_esModoAdmin)
            {
                // Busco el perfil de candidata asociado al usuario logueado.
                _candidataActual = _candidataDAO.ObtenerPorIdUsuario(Sesion.UsuarioActual.IdUsuario);

                if (_candidataActual == null)
                {
                    // Fallo crítico de integridad de datos: Usuario "Estudiante" sin perfil "Candidata".
                    MessageBox.Show("Error: No se encontró un perfil de candidata asociado a tu usuario.", "Error de Cuenta");
                    this.Close(); // Cierre de seguridad.
                    return;
                }
            }

            // 3. Construcción de UI adaptativa.
            InicializarUI();

            // 4. CONFIGURACIÓN DEL ESTADO INICIAL
            if (!_esModoAdmin && _candidataActual != null)
            {
                // Modo Autogestión: Todo listo para usar.
                ActivarGestion();
            }
            else
            {
                // Modo Admin: Bloqueo inputs hasta que seleccione a alguien.
                BloquearGestion();
            }
        }

        private void FormGestionAlbumes_Load(object sender, EventArgs e) {}

        private void ConfigurarFormulario()
        {
            this.Size = new Size(1100, 750);
            // Título contextual para mejor UX.
            this.Text = _esModoAdmin ? "Gestión de Álbumes (Administrador)" : "Mi Galería Personal";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = colorFondo;
        }

        private void InicializarUI()
        {
            // --- 0. HEADER DE SELECCIÓN (SOLO ADMIN) ---
            if (_esModoAdmin)
            {
                Panel panelTop = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 60,
                    BackColor = Color.White,
                    Padding = new Padding(20, 15, 20, 10)
                };
                // Borde inferior sutil.
                panelTop.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, panelTop.ClientRectangle,
                                            Color.White, 0, ButtonBorderStyle.None,
                                            Color.White, 0, ButtonBorderStyle.None,
                                            Color.White, 0, ButtonBorderStyle.None,
                                            Color.LightGray, 1, ButtonBorderStyle.Solid);
                this.Controls.Add(panelTop);

                Label lblBuscar = new Label { Text = "Seleccionar Candidata:", AutoSize = true, Location = new Point(20, 20), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
                panelTop.Controls.Add(lblBuscar);

                cboCandidatas = new ComboBox
                {
                    Location = new Point(180, 18),
                    Size = new Size(300, 28),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI", 10)
                };

                try
                {
                    // Lleno el combo solo con candidatas activas para evitar basura.
                    cboCandidatas.DataSource = _candidataDAO.ObtenerActivas();
                    cboCandidatas.DisplayMember = "Nombres";
                    cboCandidatas.ValueMember = "CandidataId";
                    cboCandidatas.SelectedIndex = -1; // Nada seleccionado al inicio.
                }
                catch { }

                cboCandidatas.SelectedIndexChanged += CboCandidatas_SelectedIndexChanged;
                panelTop.Controls.Add(cboCandidatas);
            }

            // --- CONTENEDOR PRINCIPAL (MASTER-DETAIL) ---
            panelContenido = new Panel { Dock = DockStyle.Fill, BackColor = colorFondo };
            this.Controls.Add(panelContenido);
            panelContenido.BringToFront();

            // --- 1. SIDEBAR (LISTA DE ÁLBUMES) ---
            Panel panelLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 250,
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            panelContenido.Controls.Add(panelLeft);

            Label lblMisAlbumes = new Label
            {
                Text = _esModoAdmin ? "ÁLBUMES DE ELLA" : "MIS ÁLBUMES", // Texto adaptativo.
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.DimGray,
                Dock = DockStyle.Top,
                Height = 40
            };
            panelLeft.Controls.Add(lblMisAlbumes);

            btnNuevo = new Button
            {
                Text = "+ NUEVO ÁLBUM",
                Height = 45,
                BackColor = colorPrimario,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Top
            };
            btnNuevo.FlatAppearance.BorderSize = 0;
            btnNuevo.Click += (s, e) => NuevoAlbum();
            panelLeft.Controls.Add(btnNuevo);

            listAlbumes = new ListBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(245, 245, 245),
                Dock = DockStyle.Fill
            };
            listAlbumes.SelectedIndexChanged += ListAlbumes_SelectedIndexChanged;

            // Espaciadores para diseño limpio.
            Panel separador = new Panel { Height = 20, Dock = DockStyle.Top, BackColor = Color.White };

            panelLeft.Controls.Add(listAlbumes);
            panelLeft.Controls.Add(separador);
            panelLeft.Controls.Add(btnNuevo);
            panelLeft.Controls.Add(lblMisAlbumes);
            
            // Z-Order Fix: Aseguro que los controles se apilen en el orden correcto.
            lblMisAlbumes.BringToFront(); btnNuevo.BringToFront(); separador.BringToFront(); listAlbumes.BringToFront();


            // --- 2. EDITOR (DETALLE DEL ÁLBUM) ---
            Panel panelEditor = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30),
                BackColor = colorFondo
            };
            panelContenido.Controls.Add(panelEditor);
            panelEditor.BringToFront();

            // Panel de Inputs (Metadatos)
            Panel panelInputs = new Panel { Dock = DockStyle.Top, Height = 220, BackColor = Color.Transparent };
            panelEditor.Controls.Add(panelInputs);

            Label lblTit = new Label { Text = "Título del Álbum", Location = new Point(0, 0), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            panelInputs.Controls.Add(lblTit);

            txtTitulo = new TextBox
            {
                Location = new Point(0, 25),
                Height = 30,
                Font = new Font("Segoe UI", 12),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Width = 500
            };
            panelInputs.Controls.Add(txtTitulo);

            Label lblDesc = new Label { Text = "Descripción", Location = new Point(0, 70), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            panelInputs.Controls.Add(lblDesc);

            txtDescripcion = new TextBox
            {
                Location = new Point(0, 95),
                Height = 60,
                Multiline = true,
                Font = new Font("Segoe UI", 10),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Width = 600
            };
            panelInputs.Controls.Add(txtDescripcion);

            Button btnAddFoto = new Button
            {
                Text = "📷 AGREGAR FOTOS",
                Location = new Point(0, 170),
                Size = new Size(180, 35),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddFoto.FlatAppearance.BorderSize = 0;
            btnAddFoto.Click += BtnAddFoto_Click;
            panelInputs.Controls.Add(btnAddFoto);

            // Panel Inferior (Botón Guardar)
            Panel panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.Transparent };
            panelEditor.Controls.Add(panelBottom);

            btnGuardar = new Button
            {
                Text = "💾 GUARDAR ÁLBUM",
                Size = new Size(200, 45),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(panelEditor.Width - 260, 5),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;
            panelBottom.Controls.Add(btnGuardar);

            // Grid de Fotos (Preview)
            flowFotos = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            panelEditor.Controls.Add(flowFotos);
            flowFotos.BringToFront();
        }

        // --- LÓGICA DE CONTROL ---

        private void CboCandidatas_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboCandidatas.SelectedItem != null)
            {
                _candidataActual = (Candidata)cboCandidatas.SelectedItem;
                ActivarGestion();
            }
        }

        /// <summary>
        /// Deshabilita toda la interfaz de edición.
        /// Se usa cuando un admin entra pero aún no ha seleccionado a nadie.
        /// </summary>
        private void BloquearGestion()
        {
            panelContenido.Enabled = false; 
        }

        /// <summary>
        /// Habilita la interfaz y carga los datos de la candidata actual.
        /// </summary>
        private void ActivarGestion()
        {
            panelContenido.Enabled = true;
            // Feedback visual: Muestro el nombre en el título.
            if (_esModoAdmin)
                this.Text = $"Gestión - {_candidataActual.Nombres} {_candidataActual.Apellidos}";

            CargarListaAlbumes();
            NuevoAlbum(); // Prepara el formulario para una inserción limpia.
        }

        // --- LÓGICA DE NEGOCIO ---

        /// <summary>
        /// Limpia el formulario y prepara el estado para crear un nuevo registro.
        /// </summary>
        private void NuevoAlbum()
        {
            _albumEnEdicion = new Album { Candidata = _candidataActual };
            _fotosNuevasTemp = new List<Foto>();
            
            // Reset de controles UI.
            txtTitulo.Text = "";
            txtDescripcion.Text = "";
            flowFotos.Controls.Clear();

            if (listAlbumes.SelectedIndex != -1 && ((Album)listAlbumes.SelectedItem).Id != 0)
                listAlbumes.ClearSelected();

            // Mensaje de estado.
            Label lblEmpty = new Label { Text = "Álbum nuevo listo.", AutoSize = false, Size = new Size(400, 50), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Gray, Margin = new Padding(100, 50, 0, 0) };
            flowFotos.Controls.Add(lblEmpty);
            
            // Restauro estilo de botón Guardar.
            btnGuardar.Text = "💾 GUARDAR ÁLBUM";
            btnGuardar.BackColor = Color.FromArgb(46, 204, 113);
        }

        private void CargarListaAlbumes()
        {
            if (_candidataActual == null) return;
            try
            {
                var albumes = _albumDAO.ObtenerPorCandidata(_candidataActual.CandidataId);
                // INYECCIÓN: Opción ficticia para volver al modo "Crear".
                albumes.Insert(0, new Album { Id = 0, Titulo = "< CREAR NUEVO ÁLBUM >", Candidata = _candidataActual });

                listAlbumes.DataSource = null;
                listAlbumes.DataSource = albumes;
                listAlbumes.DisplayMember = "Titulo";
                listAlbumes.ValueMember = "Id";
            }
            catch { }
        }

        private void ListAlbumes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listAlbumes.SelectedItem == null) return;
            Album albumSeleccionado = (Album)listAlbumes.SelectedItem;

            // Si selecciona "Crear Nuevo", reseteo.
            if (albumSeleccionado.Id == 0)
            {
                NuevoAlbum();
                return;
            }

            // Si selecciona uno existente, cargo datos.
            _albumEnEdicion = albumSeleccionado;
            _fotosNuevasTemp = new List<Foto>(); // Reinicio lista temporal de uploads.

            txtTitulo.Text = _albumEnEdicion.Titulo;
            txtDescripcion.Text = _albumEnEdicion.Descripcion;

            flowFotos.Controls.Clear();

            try
            {
                // Carga de fotos existentes.
                List<Foto> fotosDelAlbum = _albumDAO.ObtenerFotosPorAlbum(_albumEnEdicion.Id);
                if (fotosDelAlbum.Count > 0)
                {
                    foreach (var foto in fotosDelAlbum) AgregarMiniaturaVisual(foto, false);
                }
                else
                {
                    Label lbl = new Label { Text = "Sin fotos.", AutoSize = true, ForeColor = Color.Gray, Margin = new Padding(50) };
                    flowFotos.Controls.Add(lbl);
                }
            }
            catch { }

            // Cambio visual para indicar "Edición" en vez de "Creación".
            btnGuardar.Text = "💾 ACTUALIZAR ÁLBUM";
            btnGuardar.BackColor = Color.Orange;
        }

        /// <summary>
        /// Manejo de selección múltiple de archivos.
        /// </summary>
        private void BtnAddFoto_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = true; // Fundamental para cargar en lote.
                ofd.Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    // Limpio el mensaje de "Sin fotos" si existe.
                    if (_fotosNuevasTemp.Count == 0 && flowFotos.Controls.Count > 0 && flowFotos.Controls[0] is Label)
                        flowFotos.Controls.Clear();

                    foreach (string archivo in ofd.FileNames)
                    {
                        // Agrego a la lista TEMPORAL. Aún no se copia el archivo físico.
                        Foto nuevaFoto = _albumEnEdicion.AgregarFoto(archivo, "");
                        _fotosNuevasTemp.Add(nuevaFoto);
                        
                        // Muestro preview visual.
                        AgregarMiniaturaVisual(nuevaFoto, true);
                    }
                }
            }
        }

        /// <summary>
        /// Crea un componente visual miniatura para la foto.
        /// Distingue entre fotos ya guardadas (Verdes) y pendientes (Naranjas).
        /// </summary>
        private void AgregarMiniaturaVisual(Foto foto, bool esNueva)
        {
            Panel cardFoto = new Panel { Size = new Size(120, 150), Margin = new Padding(10), BackColor = Color.WhiteSmoke };
            PictureBox pic = new PictureBox { Size = new Size(100, 100), Location = new Point(10, 10), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Gainsboro };

            // Carga segura sin bloqueos de archivo.
            if (File.Exists(foto.RutaArchivo))
            {
                try { using (var fs = new FileStream(foto.RutaArchivo, FileMode.Open, FileAccess.Read)) { pic.Image = Image.FromStream(fs); } } catch { }
            }

            Label lbl = new Label { Location = new Point(10, 115), AutoSize = false, Size = new Size(100, 30), Font = new Font("Segoe UI", 7), TextAlign = ContentAlignment.TopCenter };
            lbl.Text = esNueva ? "Pendiente" : "Guardada";
            lbl.ForeColor = esNueva ? Color.Orange : Color.Green;

            cardFoto.Controls.Add(pic);
            cardFoto.Controls.Add(lbl);
            flowFotos.Controls.Add(cardFoto);
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitulo.Text)) { MessageBox.Show("Ingrese un título."); return; }

            _albumEnEdicion.Titulo = txtTitulo.Text;
            _albumEnEdicion.Descripcion = txtDescripcion.Text;

            try
            {
                // LÓGICA DE PERSISTENCIA FÍSICA:
                // Solo si hay fotos nuevas, las copio a la carpeta del sistema.
                if (_fotosNuevasTemp != null && _fotosNuevasTemp.Count > 0)
                {
                    // Creo una carpeta única por candidata para mantener orden.
                    string carpetaDestino = Path.Combine(Application.StartupPath, "Albumes", _candidataActual.CandidataId.ToString());
                    if (!Directory.Exists(carpetaDestino)) Directory.CreateDirectory(carpetaDestino);

                    foreach (var foto in _fotosNuevasTemp)
                    {
                        // Timestamp para evitar colisión de nombres.
                        string nombreArchivo = $"foto_{DateTime.Now.Ticks}_{Path.GetFileName(foto.RutaArchivo)}";
                        string rutaDestino = Path.Combine(carpetaDestino, nombreArchivo);
                        
                        File.Copy(foto.RutaArchivo, rutaDestino, true);
                        
                        // Actualizo la referencia en el objeto para que apunte al archivo COPIADO, no al original.
                        foto.RutaArchivo = rutaDestino;
                    }
                }

                // Guardo en BD (Transaccional).
                if (_albumDAO.GuardarAlbumCompleto(_albumEnEdicion, _fotosNuevasTemp))
                {
                    MessageBox.Show("Guardado con éxito.");
                    CargarListaAlbumes();
                    NuevoAlbum();
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
    }
}