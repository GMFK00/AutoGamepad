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
            cmbButtonConfig = new ComboBox();
            numHoldMin = new NumericUpDown();
            numHoldMax = new NumericUpDown();
            numWaitMin = new NumericUpDown();
            numWaitMax = new NumericUpDown();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            rtbLog = new RichTextBox();
            numInitialDelay = new NumericUpDown();
            numMaxCycles = new NumericUpDown();
            label5 = new Label();
            label6 = new Label();
            chkConnect = new CheckBox();
            ((System.ComponentModel.ISupportInitialize)numHoldMin).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numHoldMax).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numWaitMin).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numWaitMax).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numInitialDelay).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMaxCycles).BeginInit();
            SuspendLayout();
            // 
            // btnStart
            // 
            btnStart.Location = new Point(12, 12);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(154, 29);
            btnStart.TabIndex = 0;
            btnStart.Text = "Iniciar Automação";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // btnStop
            // 
            btnStop.Enabled = false;
            btnStop.Location = new Point(172, 13);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(94, 29);
            btnStop.TabIndex = 1;
            btnStop.Text = "Parar";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // cmbButtonConfig
            // 
            cmbButtonConfig.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbButtonConfig.FormattingEnabled = true;
            cmbButtonConfig.Items.AddRange(new object[] { "A", "B", "X", "Y", "D-Pad Cima", "D-Pad Baixo", "D-Pad Esquerda", "D-Pad Direita" });
            cmbButtonConfig.Location = new Point(272, 13);
            cmbButtonConfig.Name = "cmbButtonConfig";
            cmbButtonConfig.Size = new Size(175, 28);
            cmbButtonConfig.TabIndex = 2;
            // 
            // numHoldMin
            // 
            numHoldMin.Location = new Point(116, 59);
            numHoldMin.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            numHoldMin.Name = "numHoldMin";
            numHoldMin.Size = new Size(150, 27);
            numHoldMin.TabIndex = 3;
            numHoldMin.Value = new decimal(new int[] { 80, 0, 0, 0 });
            // 
            // numHoldMax
            // 
            numHoldMax.Location = new Point(116, 92);
            numHoldMax.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            numHoldMax.Name = "numHoldMax";
            numHoldMax.Size = new Size(150, 27);
            numHoldMax.TabIndex = 4;
            numHoldMax.Value = new decimal(new int[] { 120, 0, 0, 0 });
            // 
            // numWaitMin
            // 
            numWaitMin.Location = new Point(116, 125);
            numWaitMin.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            numWaitMin.Name = "numWaitMin";
            numWaitMin.Size = new Size(150, 27);
            numWaitMin.TabIndex = 5;
            numWaitMin.Value = new decimal(new int[] { 2000, 0, 0, 0 });
            // 
            // numWaitMax
            // 
            numWaitMax.Location = new Point(116, 158);
            numWaitMax.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            numWaitMax.Name = "numWaitMax";
            numWaitMax.Size = new Size(150, 27);
            numWaitMax.TabIndex = 6;
            numWaitMax.Value = new decimal(new int[] { 5000, 0, 0, 0 });
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 61);
            label1.Name = "label1";
            label1.Size = new Size(74, 20);
            label1.TabIndex = 8;
            label1.Text = "Hold Min:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 94);
            label2.Name = "label2";
            label2.Size = new Size(77, 20);
            label2.TabIndex = 9;
            label2.Text = "Hold Max:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 127);
            label3.Name = "label3";
            label3.Size = new Size(71, 20);
            label3.TabIndex = 10;
            label3.Text = "Wait Min:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 160);
            label4.Name = "label4";
            label4.Size = new Size(74, 20);
            label4.TabIndex = 11;
            label4.Text = "Wait Max:";
            // 
            // rtbLog
            // 
            rtbLog.BackColor = SystemColors.ControlDark;
            rtbLog.Dock = DockStyle.Bottom;
            rtbLog.Location = new Point(0, 191);
            rtbLog.Name = "rtbLog";
            rtbLog.ReadOnly = true;
            rtbLog.Size = new Size(682, 150);
            rtbLog.TabIndex = 12;
            rtbLog.Text = "";
            // 
            // numInitialDelay
            // 
            numInitialDelay.Location = new Point(394, 59);
            numInitialDelay.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            numInitialDelay.Name = "numInitialDelay";
            numInitialDelay.Size = new Size(150, 27);
            numInitialDelay.TabIndex = 13;
            numInitialDelay.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // numMaxCycles
            // 
            numMaxCycles.Location = new Point(394, 92);
            numMaxCycles.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            numMaxCycles.Name = "numMaxCycles";
            numMaxCycles.Size = new Size(150, 27);
            numMaxCycles.TabIndex = 14;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(321, 61);
            label5.Name = "label5";
            label5.Size = new Size(50, 20);
            label5.TabIndex = 15;
            label5.Text = "Delay:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(321, 94);
            label6.Name = "label6";
            label6.Size = new Size(53, 20);
            label6.TabIndex = 16;
            label6.Text = "Cycles:";
            // 
            // chkConnect
            // 
            chkConnect.Appearance = Appearance.Button;
            chkConnect.AutoSize = true;
            chkConnect.Location = new Point(453, 11);
            chkConnect.Name = "chkConnect";
            chkConnect.Size = new Size(211, 30);
            chkConnect.TabIndex = 17;
            chkConnect.Text = "🔌 Conectar Controle Virtual";
            chkConnect.TextAlign = ContentAlignment.MiddleCenter;
            chkConnect.UseVisualStyleBackColor = true;
            chkConnect.CheckedChanged += chkConnect_CheckedChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(682, 341);
            Controls.Add(chkConnect);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(numMaxCycles);
            Controls.Add(numInitialDelay);
            Controls.Add(rtbLog);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(numWaitMax);
            Controls.Add(numWaitMin);
            Controls.Add(numHoldMax);
            Controls.Add(numHoldMin);
            Controls.Add(cmbButtonConfig);
            Controls.Add(btnStop);
            Controls.Add(btnStart);
            Name = "Form1";
            Text = "AutoGamepad";
            ((System.ComponentModel.ISupportInitialize)numHoldMin).EndInit();
            ((System.ComponentModel.ISupportInitialize)numHoldMax).EndInit();
            ((System.ComponentModel.ISupportInitialize)numWaitMin).EndInit();
            ((System.ComponentModel.ISupportInitialize)numWaitMax).EndInit();
            ((System.ComponentModel.ISupportInitialize)numInitialDelay).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMaxCycles).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnStart;
        private Button btnStop;
        private ComboBox cmbButtonConfig;
        private NumericUpDown numHoldMin;
        private NumericUpDown numHoldMax;
        private NumericUpDown numWaitMin;
        private NumericUpDown numWaitMax;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private RichTextBox rtbLog;
        private NumericUpDown numInitialDelay;
        private NumericUpDown numMaxCycles;
        private Label label5;
        private Label label6;
        private CheckBox chkConnect;
    }
}
