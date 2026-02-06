using SIVUG.Models.DTOS;
using SIVUG.Models.SERVICES;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SIVUG.View
{
    /// <summary>
    /// CONTROLADOR DE VISTA DE SEGURIDAD.
    /// Este formulario es multimodal:
    /// 1. Modo Normal (Voluntario): El usuario decide cambiar su clave. Requiere la clave actual.
    /// 2. Modo Primer Login (Obligatorio): El sistema fuerza el cambio. NO requiere clave actual (ya se validó en Login).
    /// </summary>
    public partial class FormCambiarContraseña : Form
    {
        // Bandera crítica que define el comportamiento de seguridad del formulario.
        private readonly bool _esPrimerLogin;
        
        // Estado para controlar el cierre seguro de la ventana.
        private bool _contraseñaCambiada;

        // Estilos visuales consistentes con la identidad de la app.
        private readonly Color colorPrimario = Color.FromArgb(52, 152, 219);
        private readonly Color colorExito = Color.FromArgb(46, 204, 113);
        private readonly Color colorError = Color.FromArgb(231, 76, 60);
        private readonly Color colorFondo = Color.White;
        private readonly Color colorPanel = Color.FromArgb(245, 245, 245);

        // Referencias a controles para manipulación dinámica.
        private TextBox txtActual;
        private TextBox txtNueva;
        private TextBox txtConfirmar;
        private Label lblMensaje;
        private Label lblActual;
        private Label lblNueva;
        private Label lblConfirmar;

        /// <summary>
        /// Constructor: Recibe el modo de operación.
        /// Si esPrimerLogin es true, el formulario se vuelve "bloqueante" (Modal estricto).
        /// </summary>
        public FormCambiarContraseña(bool esPrimerLogin = false)
        {
            InitializeComponent();
            _esPrimerLogin = esPrimerLogin;
            _contraseñaCambiada = false;
            
            ConfigurarFormulario();
            
            // Construcción dinámica de la UI según el modo.
            CrearInterfaz();
        }

        /// <summary>
        /// Configuración base de la ventana.
        /// En modo obligatorio elimino el botón de cerrar (ControlBox) para forzar la acción.
        /// </summary>
        private void ConfigurarFormulario()
        {
            // Ajusto la altura dinámicamente: Si es primer login, no necesito el campo "Contraseña Actual".
            int altura = _esPrimerLogin ? 420 : 500;

            this.Size = new Size(500, altura);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog; // Borde fijo, no redimensionable.
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = colorFondo;
            this.Text = _esPrimerLogin ? "⚠️ Cambio Obligatorio" : "Cambiar Contraseña";

            // SEGURIDAD: Si es obligatorio, quito la "X" para que no puedan evadirlo fácilmente.
            if (_esPrimerLogin)
            {
                this.ControlBox = false;
            }
        }

        private void CrearInterfaz()
        {
            // ========== 1. PANEL HEADER (Identidad Visual) ==========
            Panel panelHeader = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(500, 100),
                BackColor = colorPrimario
            };
            this.Controls.Add(panelHeader);

            PictureBox iconoCandado = new PictureBox
            {
                Image = SystemIcons.Shield.ToBitmap(), // Uso icono de sistema por simplicidad.
                Size = new Size(40, 40),
                Location = new Point(20, 30),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            panelHeader.Controls.Add(iconoCandado);

            // Mensajes contextuales según el modo.
            Label lblTitulo = new Label
            {
                Text = _esPrimerLogin ? "🔒 Cambio de Contraseña Obligatorio" : "Cambiar Contraseña",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(70, 25)
            };
            panelHeader.Controls.Add(lblTitulo);

            Label lblSubtitulo = new Label
            {
                Text = _esPrimerLogin
                    ? "Por seguridad, debes crear una nueva contraseña antes de continuar."
                    : "Actualiza tu contraseña para mantener tu cuenta segura.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(230, 240, 255), // Blanco tenue para contraste.
                AutoSize = false,
                Size = new Size(400, 35),
                Location = new Point(70, 55)
            };
            panelHeader.Controls.Add(lblSubtitulo);

            // ========== 2. FORMULARIO DE INPUTS DINÁMICO ==========
            int yInicio = 120;
            int yActual = yInicio;

            // LÓGICA CONDICIONAL DE UI:
            // Solo pido la contraseña actual si NO es el primer login.
            // En el primer login, asumimos que el usuario acaba de entrar con credenciales temporales.
            if (!_esPrimerLogin)
            {
                Label lblActual = new Label
                {
                    Text = "Contraseña Actual:",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.FromArgb(64, 64, 64),
                    Location = new Point(30, yActual),
                    AutoSize = true
                };
                this.Controls.Add(lblActual);

                TextBox txtActual = new TextBox
                {
                    Name = "txtActual", // Etiqueta para buscarlo luego.
                    Location = new Point(30, yActual + 25),
                    Size = new Size(430, 35),
                    Font = new Font("Segoe UI", 11),
                    UseSystemPasswordChar = true, // Ocultar caracteres.
                    BorderStyle = BorderStyle.FixedSingle
                };
                this.Controls.Add(txtActual);

                yActual += 75; // Desplazo el cursor Y para el siguiente control.
            }

            // --- Nueva Contraseña ---
            Label lblNueva = new Label
            {
                Text = "Nueva Contraseña:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64),
                Location = new Point(30, yActual),
                AutoSize = true
            };
            this.Controls.Add(lblNueva);

            TextBox txtNueva = new TextBox
            {
                Name = "txtNueva",
                Location = new Point(30, yActual + 25),
                Size = new Size(430, 35),
                Font = new Font("Segoe UI", 11),
                UseSystemPasswordChar = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(txtNueva);

            // Hint de UX para requisitos de contraseña.
            Label lblHintNueva = new Label
            {
                Text = "Mínimo 8 caracteres",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                Location = new Point(30, yActual + 62),
                AutoSize = true
            };
            this.Controls.Add(lblHintNueva);

            yActual += 90;

            // --- Confirmación ---
            Label lblConfirmar = new Label
            {
                Text = "Confirmar Contraseña:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64),
                Location = new Point(30, yActual),
                AutoSize = true
            };
            this.Controls.Add(lblConfirmar);

            TextBox txtConfirmar = new TextBox
            {
                Name = "txtConfirmar",
                Location = new Point(30, yActual + 25),
                Size = new Size(430, 35),
                Font = new Font("Segoe UI", 11),
                UseSystemPasswordChar = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(txtConfirmar);

            yActual += 70;

            // ========== 3. FEEDBACK VISUAL ==========
            // Label invisible que se mostrará solo si hay errores o éxito.
            Label lblMensaje = new Label
            {
                Name = "lblMensaje",
                Location = new Point(30, yActual),
                Size = new Size(430, 30),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = colorError,
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };
            this.Controls.Add(lblMensaje);

            yActual += 40;

            // ========== 4. PANEL DE ACCIONES ==========
            Panel panelBotones = new Panel
            {
                Location = new Point(0, yActual),
                Size = new Size(500, 70),
                BackColor = colorPanel
            };
            this.Controls.Add(panelBotones);

            Button btnGuardar = new Button
            {
                Name = "btnGuardar",
                Text = _esPrimerLogin ? "CAMBIAR Y CONTINUAR" : "GUARDAR",
                Size = new Size(180, 45),
                Location = new Point(30, 12),
                BackColor = colorExito,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;
            panelBotones.Controls.Add(btnGuardar);

            // Botón Cancelar (Solo tiene sentido si el cambio es voluntario).
            if (!_esPrimerLogin)
            {
                Button btnCancelar = new Button
                {
                    Text = "CANCELAR",
                    Size = new Size(140, 45),
                    Location = new Point(220, 12),
                    BackColor = Color.White,
                    ForeColor = Color.DimGray,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10),
                    Cursor = Cursors.Hand
                };
                btnCancelar.FlatAppearance.BorderColor = Color.LightGray;
                btnCancelar.Click += (s, e) =>
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                };
                panelBotones.Controls.Add(btnCancelar);
            }

            // Manejo especial del evento Closing para evitar evasiones de seguridad.
            this.FormClosing += FormCambiarContraseña_FormClosing;
        }

   

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            try
            {
                // Recupero referencias a los controles creados dinámicamente.
                TextBox txtActual = this.Controls.Find("txtActual", true).Length > 0
                    ? (TextBox)this.Controls.Find("txtActual", true)[0]
                    : null;
                TextBox txtNueva = (TextBox)this.Controls.Find("txtNueva", true)[0];
                TextBox txtConfirmar = (TextBox)this.Controls.Find("txtConfirmar", true)[0];
                Label lblMensaje = (Label)this.Controls.Find("lblMensaje", true)[0];

                // === VALIDACIONES DEL FORMULARIO ===
                
                // 1. Campos obligatorios
                if (string.IsNullOrWhiteSpace(txtNueva.Text) || string.IsNullOrWhiteSpace(txtConfirmar.Text))
                {
                    MostrarMensaje(lblMensaje, "⚠️ Completa todos los campos", colorError);
                    return;
                }

                if (!_esPrimerLogin && txtActual != null && string.IsNullOrWhiteSpace(txtActual.Text))
                {
                    MostrarMensaje(lblMensaje, "⚠️ Ingresa tu contraseña actual", colorError);
                    return;
                }

                // 2. Políticas de Contraseña (Longitud mínima)
                if (txtNueva.Text.Length < 8)
                {
                    MostrarMensaje(lblMensaje, "⚠️ La contraseña debe tener al menos 8 caracteres", colorError);
                    return;
                }

                if (txtNueva.Text.Length > 50)
                {
                    MostrarMensaje(lblMensaje, "⚠️ La contraseña no debe exceder 50 caracteres", colorError);
                    return;
                }

                // 3. Coincidencia de contraseñas (evitar typos)
                if (txtNueva.Text != txtConfirmar.Text)
                {
                    MostrarMensaje(lblMensaje, "⚠️ Las contraseñas no coinciden", colorError);
                    txtConfirmar.Clear();
                    txtConfirmar.Focus();
                    return;
                }

                // 4. Sesión válida (Defensa en profundidad)
                if (Sesion.UsuarioActual == null)
                {
                    MostrarMensaje(lblMensaje, "❌ No hay sesión activa", colorError);
                    return;
                }

                // === LÓGICA DE NEGOCIO ===
                
                UsuarioService usuarioService = new UsuarioService();
                bool exito;

                if (_esPrimerLogin)
                {
                    // Lógica específica: Actualiza password Y apaga el flag de "RequiereCambio".
                    exito = usuarioService.ActualizarContraseñaPrimerLogin(
                        Sesion.UsuarioActual.IdUsuario,
                        txtNueva.Text
                    );
                }
                else
                {
                    // Lógica estándar: Verifica password actual antes de cambiar.
                    exito = usuarioService.ActualizarContraseña(
                        Sesion.UsuarioActual.IdUsuario,
                        txtActual.Text,
                        txtNueva.Text
                    );
                }

                if (exito)
                {
                    _contraseñaCambiada = true;

                    MessageBox.Show(
                        "✅ Contraseña actualizada correctamente.\n\nYa puedes acceder al sistema.",
                        "Éxito",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    // Mensaje genérico, los detalles específicos deben venir en ex.Message si usamos excepciones personalizadas.
                    MostrarMensaje(lblMensaje, "❌ No se pudo actualizar la contraseña", colorError);
                }
            }
            catch (InvalidOperationException ex)
            {
                // Captura específica para errores de lógica de negocio (ej: contraseña actual incorrecta)
                Label lblMensaje = (Label)this.Controls.Find("lblMensaje", true)[0];
                MostrarMensaje(lblMensaje, $"❌ {ex.Message}", colorError);
            }
            catch (Exception ex)
            {
                // Catch-all para errores inesperados.
                Label lblMensaje = (Label)this.Controls.Find("lblMensaje", true)[0];
                MostrarMensaje(lblMensaje, $"❌ Error: {ex.Message}", colorError);
                System.Diagnostics.Debug.WriteLine($"[ERROR CAMBIAR CONTRASEÑA] {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void MostrarMensaje(Label lbl, string texto, Color color)
        {
            lbl.Text = texto;
            lbl.ForeColor = color;
            lbl.Visible = true;
        }

        private void FormCambiarContraseña_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// GUARDIÁN DE SEGURIDAD:
        /// Evita que el usuario cierre la ventana sin completar el proceso obligatorio.
        /// Si intenta salir, le advertimos que se cerrará su sesión.
        /// </summary>
        private void FormCambiarContraseña_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_esPrimerLogin && !_contraseñaCambiada)
            {
                // Si el SO intenta cerrar (Shutdown), no bloqueamos.
                if (e.CloseReason == CloseReason.WindowsShutDown) return;

                DialogResult resultado = MessageBox.Show(
                    "⚠️ DEBES CAMBIAR TU CONTRASEÑA ANTES DE CONTINUAR\n\n" +
                    "Si cancelas, se cerrará tu sesión.\n\n" +
                    "¿Estás seguro?",
                    "Cambio Obligatorio",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (resultado == DialogResult.No)
                {
                    // El usuario se arrepintió, cancelamos el cierre.
                    e.Cancel = true; 
                }
                else
                {
                    // El usuario confirma salir, devolvemos Cancel al login para que cierre sesión.
                    this.DialogResult = DialogResult.Cancel;
                }
            }
        }
    }
}

