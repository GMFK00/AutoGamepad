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
            btnRowDown = new Button();
            btnRowUp = new Button();
            btnRowRemove = new Button();
            btnRowAdd = new Button();
            gridSequence = new DataGridView();
            tabPage2 = new TabPage();
            btnJsonValidate = new Button();
            btnJsonPaste = new Button();
            btnJsonCopy = new Button();
            txtJsonCode = new RichTextBox();
            button1 = new Button();
            button2 = new Button();
            chkLimitCycles = new CheckBox();
            numMaxCycles = new NumericUpDown();
            colAction = new DataGridViewComboBoxColumn();
            colButton = new DataGridViewComboBoxColumn();
            colValue = new DataGridViewTextBoxColumn();
            colRampMin = new DataGridViewTextBoxColumn();
            colRampMax = new DataGridViewTextBoxColumn();
            colMinTime = new DataGridViewTextBoxColumn();
            colMaxTime = new DataGridViewTextBoxColumn();
            colJitter = new DataGridViewTextBoxColumn();
            chkEnableJitter = new CheckBox();
            numJitterFreq = new NumericUpDown();
            tabEditor.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridSequence).BeginInit();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numMaxCycles).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numJitterFreq).BeginInit();
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
            chkSound.Location = new Point(482, 15);
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
            tabEditor.Location = new Point(0, 116);
            tabEditor.Name = "tabEditor";
            tabEditor.SelectedIndex = 0;
            tabEditor.Size = new Size(1186, 377);
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
            tabPage1.Size = new Size(1178, 344);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Visual (Tabela)";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // btnRowDown
            // 
            btnRowDown.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRowDown.Location = new Point(1051, 151);
            btnRowDown.Name = "btnRowDown";
            btnRowDown.Size = new Size(119, 43);
            btnRowDown.TabIndex = 4;
            btnRowDown.Text = "⬇️ Descer";
            btnRowDown.UseVisualStyleBackColor = true;
            btnRowDown.Click += btnRowDown_Click;
            // 
            // btnRowUp
            // 
            btnRowUp.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRowUp.Location = new Point(1051, 104);
            btnRowUp.Name = "btnRowUp";
            btnRowUp.Size = new Size(119, 43);
            btnRowUp.TabIndex = 3;
            btnRowUp.Text = "⬆️ Subir";
            btnRowUp.UseVisualStyleBackColor = true;
            btnRowUp.Click += btnRowUp_Click;
            // 
            // btnRowRemove
            // 
            btnRowRemove.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRowRemove.Location = new Point(1051, 55);
            btnRowRemove.Name = "btnRowRemove";
            btnRowRemove.Size = new Size(119, 43);
            btnRowRemove.TabIndex = 2;
            btnRowRemove.Text = "🗑️ Remover";
            btnRowRemove.UseVisualStyleBackColor = true;
            btnRowRemove.Click += btnRowRemove_Click;
            // 
            // btnRowAdd
            // 
            btnRowAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRowAdd.Location = new Point(1051, 6);
            btnRowAdd.Name = "btnRowAdd";
            btnRowAdd.Size = new Size(119, 43);
            btnRowAdd.TabIndex = 1;
            btnRowAdd.Text = "➕ Adicionar";
            btnRowAdd.UseVisualStyleBackColor = true;
            btnRowAdd.Click += btnRowAdd_Click;
            // 
            // gridSequence
            // 
            gridSequence.AllowUserToAddRows = false;
            gridSequence.AllowUserToDeleteRows = false;
            gridSequence.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            gridSequence.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridSequence.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridSequence.Columns.AddRange(new DataGridViewColumn[] { colAction, colButton, colValue, colRampMin, colRampMax, colMinTime, colMaxTime, colJitter });
            gridSequence.Location = new Point(3, 6);
            gridSequence.MinimumSize = new Size(0, 188);
            gridSequence.MultiSelect = false;
            gridSequence.Name = "gridSequence";
            gridSequence.RowHeadersWidth = 51;
            gridSequence.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridSequence.Size = new Size(1042, 332);
            gridSequence.TabIndex = 0;
            gridSequence.CellValidating += gridSequence_CellValidating;
            gridSequence.CellValueChanged += gridSequence_CellValueChanged;
            gridSequence.EditingControlShowing += gridSequence_EditingControlShowing;
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
            // btnJsonValidate
            // 
            btnJsonValidate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnJsonValidate.Location = new Point(1053, 104);
            btnJsonValidate.Name = "btnJsonValidate";
            btnJsonValidate.Size = new Size(119, 43);
            btnJsonValidate.TabIndex = 3;
            btnJsonValidate.Text = "✅ Checar";
            btnJsonValidate.UseVisualStyleBackColor = true;
            // 
            // btnJsonPaste
            // 
            btnJsonPaste.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnJsonPaste.Location = new Point(1053, 55);
            btnJsonPaste.Name = "btnJsonPaste";
            btnJsonPaste.Size = new Size(119, 43);
            btnJsonPaste.TabIndex = 2;
            btnJsonPaste.Text = "📝 Colar";
            btnJsonPaste.UseVisualStyleBackColor = true;
            // 
            // btnJsonCopy
            // 
            btnJsonCopy.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnJsonCopy.Location = new Point(1053, 6);
            btnJsonCopy.Name = "btnJsonCopy";
            btnJsonCopy.Size = new Size(119, 43);
            btnJsonCopy.TabIndex = 1;
            btnJsonCopy.Text = "📋 Copiar";
            btnJsonCopy.UseVisualStyleBackColor = true;
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
            // button1
            // 
            button1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button1.Location = new Point(930, 5);
            button1.Name = "button1";
            button1.Size = new Size(119, 43);
            button1.TabIndex = 20;
            button1.Text = "💾 Salvar";
            button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            button2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button2.Location = new Point(1055, 5);
            button2.Name = "button2";
            button2.Size = new Size(119, 43);
            button2.TabIndex = 21;
            button2.Text = "📂 Carregar";
            button2.UseVisualStyleBackColor = true;
            // 
            // chkLimitCycles
            // 
            chkLimitCycles.AutoSize = true;
            chkLimitCycles.Location = new Point(12, 54);
            chkLimitCycles.Name = "chkLimitCycles";
            chkLimitCycles.Size = new Size(123, 24);
            chkLimitCycles.TabIndex = 22;
            chkLimitCycles.Text = "Limitar Ciclos:";
            chkLimitCycles.UseVisualStyleBackColor = true;
            chkLimitCycles.CheckedChanged += chkLimitCycles_CheckedChanged;
            // 
            // numMaxCycles
            // 
            numMaxCycles.Enabled = false;
            numMaxCycles.Location = new Point(190, 53);
            numMaxCycles.Maximum = new decimal(new int[] { 99999, 0, 0, 0 });
            numMaxCycles.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numMaxCycles.Name = "numMaxCycles";
            numMaxCycles.Size = new Size(150, 27);
            numMaxCycles.TabIndex = 23;
            numMaxCycles.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // colAction
            // 
            colAction.FillWeight = 120F;
            colAction.HeaderText = "Ação";
            colAction.MinimumWidth = 6;
            colAction.Name = "colAction";
            // 
            // colButton
            // 
            colButton.FillWeight = 150F;
            colButton.HeaderText = "Botão/Eixo";
            colButton.MinimumWidth = 6;
            colButton.Name = "colButton";
            // 
            // colValue
            // 
            colValue.HeaderText = "Valor Eixo (0-100%)";
            colValue.MinimumWidth = 6;
            colValue.Name = "colValue";
            // 
            // colRampMin
            // 
            colRampMin.HeaderText = "Rampa Min (ms)";
            colRampMin.MinimumWidth = 6;
            colRampMin.Name = "colRampMin";
            // 
            // colRampMax
            // 
            colRampMax.HeaderText = "Rampa Max (ms)";
            colRampMax.MinimumWidth = 6;
            colRampMax.Name = "colRampMax";
            // 
            // colMinTime
            // 
            colMinTime.HeaderText = "Tempo Min (ms)";
            colMinTime.MinimumWidth = 6;
            colMinTime.Name = "colMinTime";
            // 
            // colMaxTime
            // 
            colMaxTime.HeaderText = "Tempo Max (ms)";
            colMaxTime.MinimumWidth = 6;
            colMaxTime.Name = "colMaxTime";
            // 
            // colJitter
            // 
            colJitter.HeaderText = "Tremor Eixo (Jitter)";
            colJitter.MinimumWidth = 6;
            colJitter.Name = "colJitter";
            // 
            // chkEnableJitter
            // 
            chkEnableJitter.AutoSize = true;
            chkEnableJitter.Location = new Point(12, 86);
            chkEnableJitter.Name = "chkEnableJitter";
            chkEnableJitter.Size = new Size(172, 24);
            chkEnableJitter.TabIndex = 24;
            chkEnableJitter.Text = "Ativar Tremor (Eixos):";
            chkEnableJitter.UseVisualStyleBackColor = true;
            chkEnableJitter.CheckedChanged += chkEnableJitter_CheckedChanged;
            // 
            // numJitterFreq
            // 
            numJitterFreq.Location = new Point(190, 85);
            numJitterFreq.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numJitterFreq.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            numJitterFreq.Name = "numJitterFreq";
            numJitterFreq.Size = new Size(150, 27);
            numJitterFreq.TabIndex = 25;
            numJitterFreq.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1186, 649);
            Controls.Add(numJitterFreq);
            Controls.Add(chkEnableJitter);
            Controls.Add(numMaxCycles);
            Controls.Add(chkLimitCycles);
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
            ((System.ComponentModel.ISupportInitialize)gridSequence).EndInit();
            tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numMaxCycles).EndInit();
            ((System.ComponentModel.ISupportInitialize)numJitterFreq).EndInit();
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
        private CheckBox chkLimitCycles;
        private NumericUpDown numMaxCycles;
        private DataGridViewComboBoxColumn colAction;
        private DataGridViewComboBoxColumn colButton;
        private DataGridViewTextBoxColumn colValue;
        private DataGridViewTextBoxColumn colRampMin;
        private DataGridViewTextBoxColumn colRampMax;
        private DataGridViewTextBoxColumn colMinTime;
        private DataGridViewTextBoxColumn colMaxTime;
        private DataGridViewTextBoxColumn colJitter;
        private CheckBox chkEnableJitter;
        private NumericUpDown numJitterFreq;
    }
}
