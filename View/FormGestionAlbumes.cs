using SIVUG.Models;
using SIVUG.Models.DAO;
using SIVUG.Models.DTOS;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq; // Necesario para el FirstOrDefault
using System.Windows.Forms;

namespace SIVUG.View
{
    public partial class FormGestionAlbumes : Form
    {
        private Candidata _candidataActual;
        private AlbumDAO _albumDAO;
        private CandidataDAO _candidataDAO; // Para llenar el combo
        private Album _albumEnEdicion;
        private List<Foto> _fotosNuevasTemp;

        // UI Colors
        private Color colorPrimario = Color.FromArgb(255, 105, 180);
        private Color colorFondo = Color.FromArgb(245, 246, 250);

        // Controls
        private ComboBox cboCandidatas; // El nuevo buscador
        private Panel panelContenido;   // Contenedor para bloquear/desbloquear
        private ListBox listAlbumes;
        private TextBox txtTitulo;
        private TextBox txtDescripcion;
        private FlowLayoutPanel flowFotos;
        private Button btnGuardar;
        private Button btnNuevo; // Lo hacemos global para activarlo/desactivarlo


        // Bandera de seguridad
        private bool _esModoAdmin;


        // Constructor modificado: Acepta null para abrirse directo
        public FormGestionAlbumes()
        {
            InitializeComponent();
            _albumDAO = new AlbumDAO();
            _candidataDAO = new CandidataDAO();

            // 1. DETERMINAR MODO SEGÚN ROL
            var rol = Sesion.UsuarioLogueado.RolEstudiante;
            _esModoAdmin = (rol == Rol.ADMINISTRADOR); // Ajusta a tu Enum

            ConfigurarFormulario();

            // 2. LÓGICA DE AUTO-SELECCIÓN
            if (!_esModoAdmin)
            {
                // Si NO es admin (es candidata), buscamos SU perfil automáticamente
                // NOTA: Requiere que hayas implementado el método del PASO 1
                _candidataActual = _candidataDAO.ObtenerPorIdUsuario(Sesion.UsuarioLogueado.Id);

                if (_candidataActual == null)
                {
                    MessageBox.Show("Error: No se encontró un perfil de candidata asociado a tu usuario.", "Error de Cuenta");
                    this.Close(); // Cerramos porque no tiene nada que hacer aquí
                    return;
                }
            }


            InicializarUI();

            // 4. ESTADO INICIAL
            if (!_esModoAdmin && _candidataActual != null)
            {
                // Modo Candidata: Carga directa
                ActivarGestion();
            }
            else
            {
                // Modo Admin: Espera selección
                BloquearGestion();
            }
        }

        private void FormGestionAlbumes_Load(object sender, EventArgs e) { }

        private void ConfigurarFormulario()
        {
            this.Size = new Size(1100, 750);
            this.Text = _esModoAdmin ? "Gestión de Álbumes (Administrador)" : "Mi Galería Personal";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = colorFondo;
        }

        private void InicializarUI()
        {
            // --- 0. PANEL SUPERIOR (BUSCADOR) - SOLO PARA ADMINS ---
            if (_esModoAdmin)
            {
                Panel panelTop = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 60,
                    BackColor = Color.White,
                    Padding = new Padding(20, 15, 20, 10)
                };
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
                    cboCandidatas.DataSource = _candidataDAO.ObtenerActivas();
                    cboCandidatas.DisplayMember = "Nombres";
                    cboCandidatas.ValueMember = "CandidataId";
                    cboCandidatas.SelectedIndex = -1;
                }
                catch { }

                cboCandidatas.SelectedIndexChanged += CboCandidatas_SelectedIndexChanged;
                panelTop.Controls.Add(cboCandidatas);
            }

            // --- CONTENEDOR PRINCIPAL ---
            panelContenido = new Panel { Dock = DockStyle.Fill, BackColor = colorFondo };
            this.Controls.Add(panelContenido);
            panelContenido.BringToFront();

            // --- 1. SIDEBAR ---
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
                Text = _esModoAdmin ? "ÁLBUMES DE ELLA" : "MIS ÁLBUMES", // Texto dinámico
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

            Panel separador = new Panel { Height = 20, Dock = DockStyle.Top, BackColor = Color.White };

            panelLeft.Controls.Add(listAlbumes);
            panelLeft.Controls.Add(separador);
            panelLeft.Controls.Add(btnNuevo);
            panelLeft.Controls.Add(lblMisAlbumes);
            lblMisAlbumes.BringToFront(); btnNuevo.BringToFront(); separador.BringToFront(); listAlbumes.BringToFront();


            // --- 2. EDITOR ---
            Panel panelEditor = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30),
                BackColor = colorFondo
            };
            panelContenido.Controls.Add(panelEditor);
            panelEditor.BringToFront();

            // Panel Inputs
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

            // Panel Botón Guardar
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

            // Flow Fotos
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

        private void BloquearGestion()
        {
            panelContenido.Enabled = false; // Deshabilita todo el editor
           
        }

        private void ActivarGestion()
        {
            panelContenido.Enabled = true;
            // Si es Admin, mostramos a quién estamos editando en el título
            if (_esModoAdmin)
                this.Text = $"Gestión - {_candidataActual.Nombres} {_candidataActual.Apellidos}";

            CargarListaAlbumes();
            NuevoAlbum();
        }

        // --- LÓGICA DE NEGOCIO (Igual que antes) ---

        private void NuevoAlbum()
        {
            _albumEnEdicion = new Album { Candidata = _candidataActual };
            _fotosNuevasTemp = new List<Foto>();
            txtTitulo.Text = "";
            txtDescripcion.Text = "";
            flowFotos.Controls.Clear();

            if (listAlbumes.SelectedIndex != -1 && ((Album)listAlbumes.SelectedItem).Id != 0)
                listAlbumes.ClearSelected();

            Label lblEmpty = new Label { Text = "Álbum nuevo listo.", AutoSize = false, Size = new Size(400, 50), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Gray, Margin = new Padding(100, 50, 0, 0) };
            flowFotos.Controls.Add(lblEmpty);
            btnGuardar.Text = "💾 GUARDAR ÁLBUM";
            btnGuardar.BackColor = Color.FromArgb(46, 204, 113);
        }

        private void CargarListaAlbumes()
        {
            if (_candidataActual == null) return;
            try
            {
                var albumes = _albumDAO.ObtenerPorCandidata(_candidataActual.CandidataId);
                // Agregamos opción ficticia para "Nuevo"
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

            if (albumSeleccionado.Id == 0)
            {
                NuevoAlbum();
                return;
            }

            _albumEnEdicion = albumSeleccionado;
            _fotosNuevasTemp = new List<Foto>();

            txtTitulo.Text = _albumEnEdicion.Titulo;
            txtDescripcion.Text = _albumEnEdicion.Descripcion;

            flowFotos.Controls.Clear();

            try
            {
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

            btnGuardar.Text = "💾 ACTUALIZAR ÁLBUM";
            btnGuardar.BackColor = Color.Orange;
        }

        private void BtnAddFoto_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = true;
                ofd.Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (_fotosNuevasTemp.Count == 0 && flowFotos.Controls.Count > 0 && flowFotos.Controls[0] is Label)
                        flowFotos.Controls.Clear();

                    foreach (string archivo in ofd.FileNames)
                    {
                        Foto nuevaFoto = _albumEnEdicion.AgregarFoto(archivo, "");
                        _fotosNuevasTemp.Add(nuevaFoto);
                        AgregarMiniaturaVisual(nuevaFoto, true);
                    }
                }
            }
        }

        private void AgregarMiniaturaVisual(Foto foto, bool esNueva)
        {
            Panel cardFoto = new Panel { Size = new Size(120, 150), Margin = new Padding(10), BackColor = Color.WhiteSmoke };
            PictureBox pic = new PictureBox { Size = new Size(100, 100), Location = new Point(10, 10), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Gainsboro };

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
                if (_fotosNuevasTemp != null && _fotosNuevasTemp.Count > 0)
                {
                    string carpetaDestino = Path.Combine(Application.StartupPath, "Albumes", _candidataActual.CandidataId.ToString());
                    if (!Directory.Exists(carpetaDestino)) Directory.CreateDirectory(carpetaDestino);

                    foreach (var foto in _fotosNuevasTemp)
                    {
                        string nombreArchivo = $"foto_{DateTime.Now.Ticks}_{Path.GetFileName(foto.RutaArchivo)}";
                        string rutaDestino = Path.Combine(carpetaDestino, nombreArchivo);
                        File.Copy(foto.RutaArchivo, rutaDestino, true);
                        foto.RutaArchivo = rutaDestino;
                    }
                }

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