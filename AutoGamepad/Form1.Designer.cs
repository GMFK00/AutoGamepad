namespace AutoGamepad
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnStart = new Button();
            btnStop = new Button();
            rtbLog = new RichTextBox();
            chkConnect = new CheckBox();
            chkSound = new CheckBox();
            tabEditor = new TabControl();
            tabPage1 = new TabPage();
            tabPage2 = new TabPage();
            gridSequence = new DataGridView();
            colAction = new DataGridViewComboBoxColumn();
            colButton = new DataGridViewComboBoxColumn();
            colValue = new DataGridViewTextBoxColumn();
            colMinTime = new DataGridViewTextBoxColumn();
            colMaxTime = new DataGridViewTextBoxColumn();
            colJitter = new DataGridViewTextBoxColumn();
            btnRowAdd = new Button();
            btnRowRemove = new Button();
            btnRowUp = new Button();
            btnRowDown = new Button();
            txtJsonCode = new RichTextBox();
            btnJsonCopy = new Button();
            btnJsonPaste = new Button();
            btnJsonValidate = new Button();
            button1 = new Button();
            button2 = new Button();
            tabEditor.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridSequence).BeginInit();
            SuspendLayout();
            // 
            // btnStart
            // 
            btnStart.Location = new Point(221, 5);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(119, 43);
            btnStart.TabIndex = 0;
            btnStart.Text = "▶ Iniciar";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // btnStop
            // 
            btnStop.Enabled = false;
            btnStop.Location = new Point(346, 5);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(119, 43);
            btnStop.TabIndex = 1;
            btnStop.Text = "Parar";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // rtbLog
            // 
            rtbLog.BackColor = SystemColors.ControlDark;
            rtbLog.Dock = DockStyle.Bottom;
            rtbLog.Location = new Point(0, 499);
            rtbLog.Name = "rtbLog";
            rtbLog.ReadOnly = true;
            rtbLog.Size = new Size(1186, 150);
            rtbLog.TabIndex = 12;
            rtbLog.Text = "";
            // 
            // chkConnect
            // 
            chkConnect.Appearance = Appearance.Button;
            chkConnect.Location = new Point(4, 5);
            chkConnect.Name = "chkConnect";
            chkConnect.Size = new Size(211, 43);
            chkConnect.TabIndex = 17;
            chkConnect.Text = "🔌 Conectar Controle Virtual";
            chkConnect.TextAlign = ContentAlignment.MiddleCenter;
            chkConnect.UseVisualStyleBackColor = true;
            chkConnect.CheckedChanged += chkConnect_CheckedChanged;
            // 
            // chkSound
            // 
            chkSound.AutoSize = true;
            chkSound.Checked = true;
            chkSound.CheckState = CheckState.Checked;
            chkSound.Location = new Point(471, 15);
            chkSound.Name = "chkSound";
            chkSound.Size = new Size(129, 24);
            chkSound.TabIndex = 18;
            chkSound.Text = "🔊 Emitir Som";
            chkSound.UseVisualStyleBackColor = true;
            // 
            // tabEditor
            // 
            tabEditor.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabEditor.Controls.Add(tabPage1);
            tabEditor.Controls.Add(tabPage2);
            tabEditor.Location = new Point(0, 54);
            tabEditor.Name = "tabEditor";
            tabEditor.SelectedIndex = 0;
            tabEditor.Size = new Size(1186, 439);
            tabEditor.TabIndex = 19;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(btnRowDown);
            tabPage1.Controls.Add(btnRowUp);
            tabPage1.Controls.Add(btnRowRemove);
            tabPage1.Controls.Add(btnRowAdd);
            tabPage1.Controls.Add(gridSequence);
            tabPage1.Location = new Point(4, 29);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(1178, 406);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Visual (Tabela)";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(btnJsonValidate);
            tabPage2.Controls.Add(btnJsonPaste);
            tabPage2.Controls.Add(btnJsonCopy);
            tabPage2.Controls.Add(txtJsonCode);
            tabPage2.Location = new Point(4, 29);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(1178, 406);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Código (JSON)";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // gridSequence
            // 
            gridSequence.AllowUserToAddRows = false;
            gridSequence.AllowUserToDeleteRows = false;
            gridSequence.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            gridSequence.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridSequence.Columns.AddRange(new DataGridViewColumn[] { colAction, colButton, colValue, colMinTime, colMaxTime, colJitter });
            gridSequence.Location = new Point(3, 6);
            gridSequence.MinimumSize = new Size(0, 188);
            gridSequence.MultiSelect = false;
            gridSequence.Name = "gridSequence";
            gridSequence.RowHeadersWidth = 51;
            gridSequence.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridSequence.Size = new Size(1042, 394);
            gridSequence.TabIndex = 0;
            // 
            // colAction
            // 
            colAction.HeaderText = "Ação";
            colAction.MinimumWidth = 6;
            colAction.Name = "colAction";
            colAction.Width = 125;
            // 
            // colButton
            // 
            colButton.HeaderText = "Botão/Eixo";
            colButton.MinimumWidth = 6;
            colButton.Name = "colButton";
            colButton.Width = 125;
            // 
            // colValue
            // 
            colValue.HeaderText = "Valor Eixo (0-255)";
            colValue.MinimumWidth = 6;
            colValue.Name = "colValue";
            colValue.Width = 125;
            // 
            // colMinTime
            // 
            colMinTime.HeaderText = "Tempo Min (ms)";
            colMinTime.MinimumWidth = 6;
            colMinTime.Name = "colMinTime";
            colMinTime.Width = 125;
            // 
            // colMaxTime
            // 
            colMaxTime.HeaderText = "Tempo Max (ms)";
            colMaxTime.MinimumWidth = 6;
            colMaxTime.Name = "colMaxTime";
            colMaxTime.Width = 125;
            // 
            // colJitter
            // 
            colJitter.HeaderText = "Tremor Eixo (Jitter)";
            colJitter.MinimumWidth = 6;
            colJitter.Name = "colJitter";
            colJitter.Width = 125;
            // 
            // btnRowAdd
            // 
            btnRowAdd.Location = new Point(1051, 6);
            btnRowAdd.Name = "btnRowAdd";
            btnRowAdd.Size = new Size(119, 43);
            btnRowAdd.TabIndex = 1;
            btnRowAdd.Text = "➕ Adicionar";
            btnRowAdd.UseVisualStyleBackColor = true;
            // 
            // btnRowRemove
            // 
            btnRowRemove.Location = new Point(1051, 55);
            btnRowRemove.Name = "btnRowRemove";
            btnRowRemove.Size = new Size(119, 43);
            btnRowRemove.TabIndex = 2;
            btnRowRemove.Text = "🗑️ Remover";
            btnRowRemove.UseVisualStyleBackColor = true;
            // 
            // btnRowUp
            // 
            btnRowUp.Location = new Point(1051, 104);
            btnRowUp.Name = "btnRowUp";
            btnRowUp.Size = new Size(119, 43);
            btnRowUp.TabIndex = 3;
            btnRowUp.Text = "⬆️ Subir";
            btnRowUp.UseVisualStyleBackColor = true;
            // 
            // btnRowDown
            // 
            btnRowDown.Location = new Point(1051, 151);
            btnRowDown.Name = "btnRowDown";
            btnRowDown.Size = new Size(119, 43);
            btnRowDown.TabIndex = 4;
            btnRowDown.Text = "⬇️ Descer";
            btnRowDown.UseVisualStyleBackColor = true;
            // 
            // txtJsonCode
            // 
            txtJsonCode.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtJsonCode.BackColor = Color.Black;
            txtJsonCode.Font = new Font("Consolas", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtJsonCode.ForeColor = Color.White;
            txtJsonCode.Location = new Point(6, 6);
            txtJsonCode.Name = "txtJsonCode";
            txtJsonCode.Size = new Size(1041, 394);
            txtJsonCode.TabIndex = 0;
            txtJsonCode.Text = "";
            // 
            // btnJsonCopy
            // 
            btnJsonCopy.Location = new Point(1053, 6);
            btnJsonCopy.Name = "btnJsonCopy";
            btnJsonCopy.Size = new Size(119, 43);
            btnJsonCopy.TabIndex = 1;
            btnJsonCopy.Text = "📋 Copiar";
            btnJsonCopy.UseVisualStyleBackColor = true;
            // 
            // btnJsonPaste
            // 
            btnJsonPaste.Location = new Point(1053, 55);
            btnJsonPaste.Name = "btnJsonPaste";
            btnJsonPaste.Size = new Size(119, 43);
            btnJsonPaste.TabIndex = 2;
            btnJsonPaste.Text = "📝 Colar";
            btnJsonPaste.UseVisualStyleBackColor = true;
            // 
            // btnJsonValidate
            // 
            btnJsonValidate.Location = new Point(1053, 104);
            btnJsonValidate.Name = "btnJsonValidate";
            btnJsonValidate.Size = new Size(119, 43);
            btnJsonValidate.TabIndex = 3;
            btnJsonValidate.Text = "✅ Checar";
            btnJsonValidate.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            button1.Location = new Point(930, 5);
            button1.Name = "button1";
            button1.Size = new Size(119, 43);
            button1.TabIndex = 20;
            button1.Text = "💾 Salvar";
            button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            button2.Location = new Point(1055, 5);
            button2.Name = "button2";
            button2.Size = new Size(119, 43);
            button2.TabIndex = 21;
            button2.Text = "📂 Carregar";
            button2.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1186, 649);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(tabEditor);
            Controls.Add(chkSound);
            Controls.Add(chkConnect);
            Controls.Add(rtbLog);
            Controls.Add(btnStop);
            Controls.Add(btnStart);
            Name = "Form1";
            Text = "AutoGamepad";
            tabEditor.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridSequence).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnStart;
        private Button btnStop;
        private RichTextBox rtbLog;
        private CheckBox chkConnect;
        private CheckBox chkSound;
        private TabControl tabEditor;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private DataGridView gridSequence;
        private DataGridViewComboBoxColumn colAction;
        private DataGridViewComboBoxColumn colButton;
        private DataGridViewTextBoxColumn colValue;
        private DataGridViewTextBoxColumn colMinTime;
        private DataGridViewTextBoxColumn colMaxTime;
        private DataGridViewTextBoxColumn colJitter;
        private Button btnRowDown;
        private Button btnRowUp;
        private Button btnRowRemove;
        private Button btnRowAdd;
        private RichTextBox txtJsonCode;
        private Button btnJsonValidate;
        private Button btnJsonPaste;
        private Button btnJsonCopy;
        private Button button1;
        private Button button2;
    }
}
