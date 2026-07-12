using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System;
using System.Media;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoGamepad
{
    // Define os tipos de ação possíveis na linha do tempo
    public enum ActionType
    {
        PressAndRelease, // Aperta e solta no final do tempo (Tap)
        Hold,            // Abaixa o botão/eixo (Rampa se for gatilho)
        Release,         // Solta o botão/eixo (Rampa de volta se for gatilho)
        Wait             // Apenas pausa a execução (Não afeta botões)
    }

    public partial class Form1 : Form
    {
        // Variáveis globais do controle e do cancelamento
        private ViGEmClient? _client;
        private IXbox360Controller? _controller;
        private CancellationTokenSource? _cancellationTokenSource;
        private Random _rnd = new Random();
        private bool _sequenceNeedsValidation = true;

        // --- HOTKEYS GLOBAIS DO WINDOWS ---
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Constantes para as teclas e modificadores
        private const int HOTKEY_ID_START = 1;
        private const int HOTKEY_ID_STOP = 2;
        private const int VK_F9 = 0x78;  // F9
        private const int VK_F10 = 0x79; // F10
        private const int MOD_CONTROL = 0x0002; // Tecla CTRL
        private const int MOD_SHIFT = 0x0004;   // Tecla SHIFT

        public Form1()
        {
            InitializeComponent();

            // Adiciona as Hotkeys: CTRL + SHIFT + F9 | CTRL + SHIFT + F10
            // O operador '|' soma os bits do Control e do Shift
            RegisterHotKey(this.Handle, HOTKEY_ID_START, MOD_CONTROL | MOD_SHIFT, VK_F9);
            RegisterHotKey(this.Handle, HOTKEY_ID_STOP, MOD_CONTROL | MOD_SHIFT, VK_F10);

            Log("Atalhos globais ativados: [Ctrl+Shift+F9] Iniciar | [Ctrl+Shift+F10] Parar");

            // Configura a tabela
            SetupGridColumns();
        }

        // --- INTERCEPTADOR DO TECLADO ---
        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312; // Código do Windows para "Hotkey pressionada"

            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();

                if (id == HOTKEY_ID_START && btnStart.Enabled)
                {
                    btnStart.PerformClick(); // Simula um clique real no botão "Iniciar"
                }
                else if (id == HOTKEY_ID_STOP && btnStop.Enabled)
                {
                    btnStop.PerformClick(); // Simula um clique real no botão "Parar"
                }
            }

            base.WndProc(ref m);
        }

        // --- BOTÃO INICIAR ---
        private async void btnStart_Click(object sender, EventArgs e)
        {

            if (_sequenceNeedsValidation)
            {
                if (!ValidateSequence())
                {
                    MessageBox.Show("Existem erros de lógica na sua sequência! Verifique as linhas marcadas em vermelho.", "Sequência Inválida", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // Se passou sem erros, marca como validada. Não precisa checar de novo até alguém mexer.
                _sequenceNeedsValidation = false;
            }

            // Trava de Segurança: Obriga a conectar antes de dar Start
            if (!chkConnect.Checked || _controller == null)
            {
                MessageBox.Show("Você precisa conectar o controle virtual primeiro (Botão no topo).", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Trava os botões para o usuário não clicar duas vezes
            ToggleUI(false);
            rtbLog.Clear();
            Log("=====================================");
            Log(" INICIANDO AUTOMAÇÃO AUTOGAMEPAD");
            Log("=====================================");

            // Cria o token que permite interromper a automação a qualquer momento
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                PlaySound(true); // TOCA O SOM DE INÍCIO

                // Inicia o motor principal
                await RunAutomationAsync(_cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Log("[!] Automação interrompida pelo usuário.");
            }
            catch (Exception ex)
            {
                Log($"[ERRO] {ex.Message}");
            }
            finally
            {
                PlaySound(false); // TOCA O SOM DE FIM (Toca mesmo se der erro ou cancelar)

                // Limpa apenas a UI
                ToggleUI(true);
                Log("Ciclo de automação finalizado.");
            }
        }

        // --- BOTÃO PARAR ---
        private void btnStop_Click(object sender, EventArgs e)
        {
            Log("Solicitando parada imediata...");
            _cancellationTokenSource?.Cancel(); // Avisa o loop para abortar
        }

        // --- AUTOMAÇÃO ---
        private async Task RunAutomationAsync(CancellationToken token)
        {
            // Coloca o controle em "Ponto Morto" para garantir que nada ficou apertado da vez anterior
            ResetControllerState();

            // 1. Lê a configuração de Ciclos da tela
            bool useLimit = chkLimitCycles.Checked;
            int maxCycles = (int)numMaxCycles.Value;

            Log("Iniciando execução da tabela de sequências...");

            // Loop Infinito até o usuário apertar Parar ou atingir o limite de ciclos
            int loopCount = 1;
            while (!token.IsCancellationRequested)
            {
                // Verifica o limite de ciclos (Se for maior que o máximo, ele para e quebra o loop)
                if (useLimit && loopCount > maxCycles)
                {
                    Log($"\n[INFO] Limite de {maxCycles} ciclos atingido. Finalizando com sucesso.");
                    break;
                }

                Log($"\n=== Iniciando Ciclo {loopCount} ===");

                // Varre a tabela linha por linha
                for (int i = 0; i < gridSequence.Rows.Count; i++)
                {
                    // Se apertar Parar no meio da tabela, ele aborta na hora
                    if (token.IsCancellationRequested) break;

                    var row = gridSequence.Rows[i];
                    if (row.Cells["colAction"].Value == null) continue; // Pula linha vazia

                    // Lê os dados da coluna atual
                    string action = row.Cells["colAction"].Value?.ToString() ?? "";
                    string button = row.Cells["colButton"].Value?.ToString() ?? "";

                    // Converte os textos para números com segurança (Ternário para evitar nulos)
                    int valuePercent = int.TryParse(row.Cells["colValue"].Value?.ToString(), out int v) ? v : 100;
                    int minTime = int.TryParse(row.Cells["colMinTime"].Value?.ToString(), out int min) ? min : 0;
                    int maxTime = int.TryParse(row.Cells["colMaxTime"].Value?.ToString(), out int max) ? max : 0;

                    // Sorteia o tempo (Jitter de Tempo)
                    int waitTime = _rnd.Next(minTime, maxTime + 1);

                    Log($"[Linha {i + 1}] {action} [{button}] -> Força: {valuePercent}% | Tempo: {waitTime}ms");

                    // 1. Se for apenas PAUSA
                    if (action == "Pausa (Wait)")
                    {
                        await Task.Delay(waitTime, token);
                        continue; // Pula pro próximo passo da tabela
                    }

                    // 2. Envia o sinal de PRESSIONAR pro controle
                    if (action == "Pressionar e Soltar (Tap)" || action == "Manter Pressionado (Hold)")
                    {
                        ProcessHardwareInput(button, valuePercent, true);
                    }
                    else if (action == "Soltar Botão/Eixo (Release)")
                    {
                        ProcessHardwareInput(button, 0, false);
                    }

                    // 3. Aguarda o tempo estipulado da ação
                    await Task.Delay(waitTime, token);

                    // 4. Se for TAP, precisa soltar o botão automaticamente logo após a pausa!
                    if (action == "Pressionar e Soltar (Tap)")
                    {
                        ProcessHardwareInput(button, 0, false);
                    }
                }

                loopCount++;
            }
        }

        // --- FUNÇÕES AUXILIARES ---

        // --- TRADUTOR: CONVERTE TEXTO DA TABELA EM SINAL ---
        private void ProcessHardwareInput(string buttonName, int valuePercent, bool isPress)
        {
            if (_controller == null) return;

            // Para botões digitais, o estado é True (Apertar) ou False (Soltar)
            bool state = isPress;

            switch (buttonName)
            {
                // BOTÕES DIGITAIS
                case "Botão A": _controller.SetButtonState(Xbox360Button.A, state); break;
                case "Botão B": _controller.SetButtonState(Xbox360Button.B, state); break;
                case "Botão X": _controller.SetButtonState(Xbox360Button.X, state); break;
                case "Botão Y": _controller.SetButtonState(Xbox360Button.Y, state); break;
                case "D-Pad Cima": _controller.SetButtonState(Xbox360Button.Up, state); break;
                case "D-Pad Baixo": _controller.SetButtonState(Xbox360Button.Down, state); break;
                case "D-Pad Esquerda": _controller.SetButtonState(Xbox360Button.Left, state); break;
                case "D-Pad Direita": _controller.SetButtonState(Xbox360Button.Right, state); break;
                case "Ombro Esquerdo (LB)": _controller.SetButtonState(Xbox360Button.LeftShoulder, state); break;
                case "Ombro Direito (RB)": _controller.SetButtonState(Xbox360Button.RightShoulder, state); break;
                case "Clique Analógico Esq (L3)": _controller.SetButtonState(Xbox360Button.LeftThumb, state); break;
                case "Clique Analógico Dir (R3)": _controller.SetButtonState(Xbox360Button.RightThumb, state); break;

                // GATILHOS (Convertendo 0-100% para 0-255 Byte)
                case "Gatilho Esquerdo (LT)":
                    byte ltValue = isPress ? (byte)(valuePercent * 255 / 100) : (byte)0;
                    _controller.SetSliderValue(Xbox360Slider.LeftTrigger, ltValue);
                    break;
                case "Gatilho Direito (RT)":
                    byte rtValue = isPress ? (byte)(valuePercent * 255 / 100) : (byte)0;
                    _controller.SetSliderValue(Xbox360Slider.RightTrigger, rtValue);
                    break;

                // ANALÓGICOS (Convertendo 0-100% para 0-32767 Short)
                // Cima/Direita = Valores Positivos | Baixo/Esquerda = Valores Negativos
                case "Analógico Esq - Cima":
                    short lsUp = isPress ? (short)(valuePercent * 32767 / 100) : (short)0;
                    _controller.SetAxisValue(Xbox360Axis.LeftThumbY, lsUp);
                    break;
                case "Analógico Esq - Baixo":
                    short lsDown = isPress ? (short)(valuePercent * -32768 / 100) : (short)0;
                    _controller.SetAxisValue(Xbox360Axis.LeftThumbY, lsDown);
                    break;
                case "Analógico Esq - Direita":
                    short lsRight = isPress ? (short)(valuePercent * 32767 / 100) : (short)0;
                    _controller.SetAxisValue(Xbox360Axis.LeftThumbX, lsRight);
                    break;
                case "Analógico Esq - Esquerda":
                    short lsLeft = isPress ? (short)(valuePercent * -32768 / 100) : (short)0;
                    _controller.SetAxisValue(Xbox360Axis.LeftThumbX, lsLeft);
                    break;

                // Mesmo para o Analógico Direito...
                case "Analógico Dir - Cima":
                    short rsUp = isPress ? (short)(valuePercent * 32767 / 100) : (short)0;
                    _controller.SetAxisValue(Xbox360Axis.RightThumbY, rsUp);
                    break;
                case "Analógico Dir - Baixo":
                    short rsDown = isPress ? (short)(valuePercent * -32768 / 100) : (short)0;
                    _controller.SetAxisValue(Xbox360Axis.RightThumbY, rsDown);
                    break;
                case "Analógico Dir - Direita":
                    short rsRight = isPress ? (short)(valuePercent * 32767 / 100) : (short)0;
                    _controller.SetAxisValue(Xbox360Axis.RightThumbX, rsRight);
                    break;
                case "Analógico Dir - Esquerda":
                    short rsLeft = isPress ? (short)(valuePercent * -32768 / 100) : (short)0;
                    _controller.SetAxisValue(Xbox360Axis.RightThumbX, rsLeft);
                    break;
            }
        }

        // --- ZERA O CONTROLE (PONTO MORTO) ---
        // Solta todos os botões e zera os eixos sem precisar desconectar o USB
        private void ResetControllerState()
        {
            if (_controller == null) return;

            // Zera Botões Digitais
            _controller.SetButtonState(Xbox360Button.A, false);
            _controller.SetButtonState(Xbox360Button.B, false);
            _controller.SetButtonState(Xbox360Button.X, false);
            _controller.SetButtonState(Xbox360Button.Y, false);
            _controller.SetButtonState(Xbox360Button.Up, false);
            _controller.SetButtonState(Xbox360Button.Down, false);
            _controller.SetButtonState(Xbox360Button.Left, false);
            _controller.SetButtonState(Xbox360Button.Right, false);
            _controller.SetButtonState(Xbox360Button.LeftShoulder, false);
            _controller.SetButtonState(Xbox360Button.RightShoulder, false);
            _controller.SetButtonState(Xbox360Button.LeftThumb, false);
            _controller.SetButtonState(Xbox360Button.RightThumb, false);

            // Zera Gatilhos (0)
            _controller.SetSliderValue(Xbox360Slider.LeftTrigger, 0);
            _controller.SetSliderValue(Xbox360Slider.RightTrigger, 0);

            // Zera Analógicos (Centro = 0)
            _controller.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
            _controller.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
            _controller.SetAxisValue(Xbox360Axis.RightThumbX, 0);
            _controller.SetAxisValue(Xbox360Axis.RightThumbY, 0);
        }

        // --- DESCONECTA E LIMPA A MEMÓRIA ---
        private void DisconnectController()
        {
            if (_controller != null)
            {
                // Tenta desconectar. Se o Windows já tiver matado o objeto, ignora o erro silenciosamente.
                try { _controller.Disconnect(); } catch { }
                _controller = null;
            }

            if (_client != null)
            {
                // Tenta limpar o cliente principal
                try { _client.Dispose(); } catch { }
                _client = null;
            }
        }

        // Bloqueia e desbloqueia botões na tela
        private void ToggleUI(bool isIdle)
        {
            btnStart.Enabled = isIdle;
            btnStop.Enabled = !isIdle;
            chkConnect.Enabled = isIdle;

            // Travas da Tabela e do Editor JSON
            gridSequence.Enabled = isIdle;
            txtJsonCode.Enabled = isIdle;

            // Travas de Ciclo e Som
            chkLimitCycles.Enabled = isIdle;
            chkSound.Enabled = isIdle;

            // Só libera a caixinha de número se estiver em "Idle" E o Checkbox de limite estiver marcado!
            numMaxCycles.Enabled = isIdle && chkLimitCycles.Checked;
        }

        // Escreve na caixa preta e rola pra baixo
        private void Log(string message)
        {
            // Checa se a caixa preta não foi destruída (IsDisposed) pelo Windows fechando a janela
            if (rtbLog != null && !rtbLog.IsDisposed)
            {
                rtbLog.AppendText(message + Environment.NewLine);
                rtbLog.ScrollToCaret();
            }
        }

        // Toca um som suave pelo alto-falante do Windows
        private void PlaySound(bool isStarting)
        {
            if (!chkSound.Checked) return;

            Task.Run(() =>
            {
                if (isStarting)
                {
                    // Som clássico de "Asterisco" (Informação) do Windows
                    SystemSounds.Asterisk.Play();
                }
                else
                {
                    // Som clássico de "Exclamação" (Aviso) do Windows
                    SystemSounds.Exclamation.Play();
                }
            });
        }

        // Evento que ocorre quando o usuário clica no "X" para fechar a janela
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 1. Manda o sinal de abortar pro Motor da automação parar IMEDIATAMENTE
            _cancellationTokenSource?.Cancel();

            // 2. Coloca o controle em ponto morto e espera 50ms pro Motor finalizar
            ResetControllerState();
            Thread.Sleep(50);

            // 3. Agora sim é seguro desconectar
            DisconnectController();

            // Devolve as teclas pro Windows ao fechar o programa
            UnregisterHotKey(this.Handle, HOTKEY_ID_START);
            UnregisterHotKey(this.Handle, HOTKEY_ID_STOP);

            base.OnFormClosing(e);
        }

        private void chkConnect_CheckedChanged(object sender, EventArgs e)
        {
            if (chkConnect.Checked)
            {
                // Conectar
                try
                {
                    Log("Iniciando driver do controle virtual...");
                    _client = new ViGEmClient();
                    _controller = _client.CreateXbox360Controller();
                    _controller.Connect();

                    chkConnect.Text = "✅ Controle Conectado";
                    Log("[SUCESSO] Controle de Xbox conectado no Windows.");
                }
                catch (Exception ex)
                {
                    chkConnect.Checked = false; // Desmarca se der erro
                    Log($"[ERRO FATAL] {ex.Message}");
                    MessageBox.Show("Verifique se o driver ViGEmBus está instalado.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Desconectar
                DisconnectController();
                chkConnect.Text = "🔌 Conectar Controle Virtual";
                Log("[INFO] Controle desconectado com segurança.");
            }
        }

        // Configura as opções que aparecem dentro das caixinhas da Tabela
        private void SetupGridColumns()
        {
            // 1. Pega a coluna "Ação"
            var actionColumn = (DataGridViewComboBoxColumn)gridSequence.Columns["colAction"]!;
            actionColumn.Items.Clear();
            actionColumn.Items.Add("Pressionar e Soltar (Tap)");
            actionColumn.Items.Add("Manter Pressionado (Hold)");
            actionColumn.Items.Add("Soltar Botão/Eixo (Release)");
            actionColumn.Items.Add("Pausa (Wait)");

            // 2. Pega a coluna "Botão/Eixo" 
            var buttonColumn = (DataGridViewComboBoxColumn)gridSequence.Columns["colButton"]!;
            buttonColumn.Items.Clear();
            buttonColumn.Items.Add("[Vazio / Apenas Pausa]");

            // Botões Digitais
            buttonColumn.Items.Add("Botão A");
            buttonColumn.Items.Add("Botão B");
            buttonColumn.Items.Add("Botão X");
            buttonColumn.Items.Add("Botão Y");
            buttonColumn.Items.Add("D-Pad Cima");
            buttonColumn.Items.Add("D-Pad Baixo");
            buttonColumn.Items.Add("D-Pad Esquerda");
            buttonColumn.Items.Add("D-Pad Direita");
            buttonColumn.Items.Add("Ombro Esquerdo (LB)");
            buttonColumn.Items.Add("Ombro Direito (RB)");
            buttonColumn.Items.Add("Clique Analógico Esq (L3)");
            buttonColumn.Items.Add("Clique Analógico Dir (R3)");

            // Gatilhos Analógicos (0 a 100%)
            buttonColumn.Items.Add("Gatilho Esquerdo (LT)");
            buttonColumn.Items.Add("Gatilho Direito (RT)");

            // Movimento Analógico Esquerdo (0 a 100%)
            buttonColumn.Items.Add("Analógico Esq - Cima");
            buttonColumn.Items.Add("Analógico Esq - Baixo");
            buttonColumn.Items.Add("Analógico Esq - Esquerda");
            buttonColumn.Items.Add("Analógico Esq - Direita");

            // Movimento Analógico Direito (0 a 100%)
            buttonColumn.Items.Add("Analógico Dir - Cima");
            buttonColumn.Items.Add("Analógico Dir - Baixo");
            buttonColumn.Items.Add("Analógico Dir - Esquerda");
            buttonColumn.Items.Add("Analógico Dir - Direita");
        }

        // --- BOTÃO: ADICIONAR LINHA ---
        private void btnRowAdd_Click(object sender, EventArgs e)
        {
            // Cria uma linha nova e guarda qual é o índice dela
            int rowIndex = gridSequence.Rows.Add();

            // Preenche as caixas de Ação e Botão com valores padrão pra não ficar vazio
            gridSequence.Rows[rowIndex].Cells["colAction"].Value = "Pressionar e Soltar (Tap)";
            gridSequence.Rows[rowIndex].Cells["colButton"].Value = "Botão A";

            // Coloca valores padrão de tempo (100ms) e força (100%)
            gridSequence.Rows[rowIndex].Cells["colValue"].Value = "100";
            gridSequence.Rows[rowIndex].Cells["colMinTime"].Value = "100";
            gridSequence.Rows[rowIndex].Cells["colMaxTime"].Value = "100";
            gridSequence.Rows[rowIndex].Cells["colJitter"].Value = "0";

            // Rola a tabela para baixo automaticamente para mostrar a nova linha
            gridSequence.FirstDisplayedScrollingRowIndex = rowIndex;

            // Força a tabela a checar a regra do valor da célula assim que a linha nasce
            gridSequence_CellValueChanged(this, new DataGridViewCellEventArgs(gridSequence.Columns["colButton"]!.Index, rowIndex));

            _sequenceNeedsValidation = true;
        }

        // --- BOTÃO: REMOVER LINHA ---
        private void btnRowRemove_Click(object sender, EventArgs e)
        {
            // Verifica se o usuário selecionou alguma linha
            if (gridSequence.SelectedRows.Count > 0)
            {
                // Deleta a linha selecionada
                int rowIndex = gridSequence.SelectedRows[0].Index;
                gridSequence.Rows.RemoveAt(rowIndex);
            }
            else
            {
                // Se a pessoa só clicou numa célula e não na linha inteira, avisa ela
                MessageBox.Show("Selecione uma linha inteira (clicando na margem esquerda da tabela) para remover.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            _sequenceNeedsValidation = true;
        }

        // --- DETECTA MUDANÇAS DENTRO DA TABELA ---
        private void gridSequence_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // Ignora se o usuário clicou no cabeçalho (linha -1)
            if (e.RowIndex < 0) return;

            // Se a coluna alterada for "Botão/Eixo" (colButton)
            if (gridSequence.Columns[e.ColumnIndex].Name == "colButton")
            {
                var selectedButton = gridSequence.Rows[e.RowIndex].Cells["colButton"].Value?.ToString();

                // Pega duas células: O Valor e o Tremor(Jitter)
                var valueCell = gridSequence.Rows[e.RowIndex].Cells["colValue"];
                var jitterCell = gridSequence.Rows[e.RowIndex].Cells["colJitter"];

                // Checa com precisão se é um Gatilho ou Movimento de Analógico 
                // (Ignora D-Pads e os Cliques L3/R3 que começam com outras palavras)
                bool isAxis = selectedButton != null && (
                              selectedButton.StartsWith("Gatilho") ||
                              selectedButton.StartsWith("Analógico"));

                if (isAxis)
                {
                    // É um eixo de movimento/gatilho. Libera tudo.
                    valueCell.ReadOnly = false;
                    valueCell.Style.BackColor = System.Drawing.Color.White;
                    if (valueCell.Value?.ToString() == "-") valueCell.Value = "100";

                    jitterCell.ReadOnly = false;
                    jitterCell.Style.BackColor = System.Drawing.Color.White;
                    if (jitterCell.Value?.ToString() == "-") jitterCell.Value = "0"; // Tremor 0 por padrão
                }
                else
                {
                    // É um botão normal ou pausa. Bloqueia tudo.
                    valueCell.ReadOnly = true;
                    valueCell.Style.BackColor = System.Drawing.Color.LightGray;
                    valueCell.Value = "-";

                    jitterCell.ReadOnly = true;
                    jitterCell.Style.BackColor = System.Drawing.Color.LightGray;
                    jitterCell.Value = "-";
                }
            }

            // Sinaliza o Dirty Flag para o Validador
            _sequenceNeedsValidation = true;
        }

        // --- PREPARA A CÉLULA PARA EDIÇÃO ---
        private void gridSequence_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {

            if (gridSequence.CurrentCell == null) return; // Evita aviso CS8602

            // Pega o "TextBox" temporário que a tabela cria quando o usuário vai editar
            if (e.Control is TextBox tb)
            {
                // Limpa eventos antigos para não duplicar se o usuário clicar várias vezes na mesma célula
                tb.KeyPress -= TextBox_KeyPress;

                // Verifica se a coluna atual é uma das colunas que queremos restringir (Só números)
                int colIndex = gridSequence.CurrentCell.ColumnIndex;
                string colName = gridSequence.Columns[colIndex].Name;

                if (colName == "colValue" || colName == "colMinTime" || colName == "colMaxTime" || colName == "colJitter")
                {
                    // Se for, monitora o teclado
                    tb.KeyPress += TextBox_KeyPress;
                }
            }
        }

        // --- BLOQUEIA LETRAS ---
        private void TextBox_KeyPress(object? sender, KeyPressEventArgs e)
        {
            // Se a tecla pressionada não for um Número (0-9) e também não for o "Backspace" (Apagar)
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                // Descarta a tecla (Ela nunca chega a aparecer na tela)
                e.Handled = true;
            }
        }

        // --- PENTE-FINO (BLOQUEIA CTRL+V DE LETRAS E VALIDA LIMITES) ---
        private void gridSequence_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            string colName = gridSequence.Columns[e.ColumnIndex].Name;

            if (colName == "colValue" || colName == "colMinTime" || colName == "colMaxTime" || colName == "colJitter")
            {
                // Pega o valor com segurança contra nulos
                string newText = e.FormattedValue?.ToString() ?? "";

                if (newText == "-" || newText == "") return;

                // Tenta converter para número
                if (!int.TryParse(newText, out int numericValue))
                {
                    e.Cancel = true;
                    MessageBox.Show("Esta coluna aceita apenas números inteiros.", "Valor Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return; // Para a validação aqui se não for número
                }

                // REGRA 1: Coluna de Força do Eixo (0 a 100%)
                if (colName == "colValue")
                {
                    if (numericValue < 0 || numericValue > 100)
                    {
                        e.Cancel = true;
                        MessageBox.Show("O valor do Eixo/Gatilho deve estar entre 0 e 100%.", "Limite Excedido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                // REGRA 2: Colunas de Tempo e Jitter (Não podem ser negativos)
                else if (colName == "colMinTime" || colName == "colMaxTime" || colName == "colJitter")
                {
                    if (numericValue < 0)
                    {
                        e.Cancel = true;
                        MessageBox.Show("O tempo não pode ser um valor negativo.", "Limite Excedido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        // --- BOTÃO: SUBIR LINHA ---
        private void btnRowUp_Click(object sender, EventArgs e)
        {
            // Checa se tem alguma linha selecionada
            if (gridSequence.SelectedRows.Count == 0) return;

            int rowIndex = gridSequence.SelectedRows[0].Index;

            // Se já for a primeira linha (0), não tem pra onde subir, então aborta
            if (rowIndex == 0) return;

            MoveRow(rowIndex, rowIndex - 1);
        }

        // --- BOTÃO: DESCER LINHA ---
        private void btnRowDown_Click(object sender, EventArgs e)
        {
            if (gridSequence.SelectedRows.Count == 0) return;

            int rowIndex = gridSequence.SelectedRows[0].Index;

            // Se já for a última linha, não tem pra onde descer, então aborta
            if (rowIndex == gridSequence.Rows.Count - 1) return;

            MoveRow(rowIndex, rowIndex + 1);
        }

        // --- FUNÇÃO AUXILIAR: MOVE A LINHA ---
        private void MoveRow(int oldIndex, int newIndex)
        {
            // Pega a linha original
            DataGridViewRow rowToMove = gridSequence.Rows[oldIndex];

            // Remove da posição antiga
            gridSequence.Rows.RemoveAt(oldIndex);

            // Insere na posição nova
            gridSequence.Rows.Insert(newIndex, rowToMove);

            // Limpa qualquer seleção antiga e seleciona a linha no novo local dela
            gridSequence.ClearSelection();
            gridSequence.Rows[newIndex].Selected = true;

            _sequenceNeedsValidation = true;
        }

        // --- VALIDADOR LÓGICO DE SEQUÊNCIA ---
        private bool ValidateSequence()
        {
            bool isValid = true;

            // Lista do C# para salvar quais botões estão segurados na linha do tempo
            var heldButtons = new System.Collections.Generic.HashSet<string>();

            // Limpa as cores vermelhas antigas da tabela antes de checar de novo
            foreach (DataGridViewRow row in gridSequence.Rows)
            {
                row.DefaultCellStyle.BackColor = System.Drawing.Color.White;
                row.ErrorText = ""; // Remove o ícone de erro
            }

            // Lê linha por linha do começo ao fim
            for (int i = 0; i < gridSequence.Rows.Count; i++)
            {
                var row = gridSequence.Rows[i];
                if (row.Cells["colAction"].Value == null) continue;

                string action = row.Cells["colAction"].Value?.ToString() ?? "";
                string button = row.Cells["colButton"].Value?.ToString() ?? "";

                // Checagem de lógica: Soltar um botão que não está pressionado
                if (action == "Soltar Botão/Eixo (Release)")
                {
                    if (!heldButtons.Contains(button))
                    {
                        MarkRowAsError(row, $"Tentando soltar '{button}', mas ele não foi mantido pressionado anteriormente!");
                        isValid = false;
                    }
                    else
                    {
                        heldButtons.Remove(button); // Soltou, tira da lista
                    }
                }

                // Checagem de lógica: Segurar um botão
                else if (action == "Manter Pressionado (Hold)")
                {
                    if (heldButtons.Contains(button))
                    {
                        MarkRowAsError(row, $"O botão '{button}' já está sendo segurado!");
                        isValid = false;
                    }
                    else
                    {
                        heldButtons.Add(button); // Segurou, coloca na lista
                    }
                }

                // Checagem Matemática: Mínimo maior que Máximo
                int minTime = int.Parse(row.Cells["colMinTime"].Value?.ToString() ?? "0");
                int maxTime = int.Parse(row.Cells["colMaxTime"].Value?.ToString() ?? "0");
                if (minTime > maxTime)
                {
                    MarkRowAsError(row, "O Tempo Mínimo não pode ser maior que o Tempo Máximo.");
                    isValid = false;
                }
            }

            // Checagem Final: Esqueceu de soltar algum botão?
            if (heldButtons.Count > 0)
            {
                // Se sobrou botão segurado, marca a ÚLTIMA linha como errada para avisar o usuário
                var lastRow = gridSequence.Rows[gridSequence.Rows.Count - 1];
                string botoesPresos = string.Join(", ", heldButtons);
                MarkRowAsError(lastRow, $"A sequência terminou, mas você esqueceu de soltar: {botoesPresos}!");
                isValid = false;
            }

            return isValid;
        }

        // Pinta a linha de vermelho e coloca um ponto de exclamação
        private void MarkRowAsError(DataGridViewRow row, string message)
        {
            row.DefaultCellStyle.BackColor = System.Drawing.Color.LightCoral;
            row.ErrorText = message;
        }

        // --- ATIVA/DESATIVA A CHECKBOX DE CICLOS ---
        private void chkLimitCycles_CheckedChanged(object sender, EventArgs e)
        {
            numMaxCycles.Enabled = chkLimitCycles.Checked;
        }
    }
}