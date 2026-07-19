using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoGamepad
{
    public partial class Form1 : Form
    {
        // Variáveis globais do controle e do cancelamento
        private Xbox360GamepadOutput? _gamepadOutput;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _automationTask;
        private bool _sequenceNeedsValidation = true;
        private bool _isConfiguringSequenceRow;

        private const string EMPTY_CONTROL_LABEL = "[Vazio / Apenas Pausa]";
        private const string DEFAULT_CONTROL_LABEL = "Botão A";

        // Variáveis de Gestão de Log Seguro (Anti-Memory Leak)
        private const int MAX_LOG_LINES_UI = 500; // Máximo de linhas mostradas na tela preta
        private readonly Queue<string> _logUIBuffer = new Queue<string>(MAX_LOG_LINES_UI);

        private readonly List<string> _logDiskBuffer = new List<string>();
        private const int MAX_LOG_DISK_BUFFER = 50; // Salva no HD a cada 50 mensagens
        private string? _currentLogFilePath = null;
        private readonly object _logLock = new object(); // Trava de segurança para múltiplas Threads

        // --- DICIONÁRIOS DE TRADUÇÃO (VISUAL <-> JSON) ---

        // Traduz Ações (Tabela -> JSON)
        private readonly Dictionary<string, string> _actionToJson = new()
        {
            { "Pressionar e Soltar (Tap)", "Tap" },
            { "Manter Pressionado (Hold)", "Hold" },
            { "Soltar Botão/Eixo (Release)", "Release" },
            { "Pausa (Wait)", "Wait" }
        };

        // Traduz Botões (Tabela -> JSON)
        private readonly Dictionary<string, string> _buttonToJson = new()
        {
            { EMPTY_CONTROL_LABEL, "None" },
            { DEFAULT_CONTROL_LABEL, "A" }, { "Botão B", "B" }, { "Botão X", "X" }, { "Botão Y", "Y" },
            { "Botão Start", "Start" }, { "Botão Select (Back)", "Back" },
            { "D-Pad Cima", "Up" }, { "D-Pad Baixo", "Down" }, { "D-Pad Esquerda", "Left" }, { "D-Pad Direita", "Right" },
            { "Ombro Esquerdo (LB)", "LB" }, { "Ombro Direito (RB)", "RB" },
            { "Clique Analógico Esq (L3)", "L3" }, { "Clique Analógico Dir (R3)", "R3" },
            { "Gatilho Esquerdo (LT)", "LT" }, { "Gatilho Direito (RT)", "RT" },
            { "Analógico Esq - Cima", "LS_Up" }, { "Analógico Esq - Baixo", "LS_Down" },
            { "Analógico Esq - Esquerda", "LS_Left" }, { "Analógico Esq - Direita", "LS_Right" },
            { "Analógico Dir - Cima", "RS_Up" }, { "Analógico Dir - Baixo", "RS_Down" },
            { "Analógico Dir - Esquerda", "RS_Left" }, { "Analógico Dir - Direita", "RS_Right" }
        };

        // Métodos para inverter a busca (JSON -> Tabela)
        private string GetActionFromJson(string jsonAction)
        {
            foreach (var kvp in _actionToJson) if (kvp.Value == jsonAction) return kvp.Key;
            return "Pausa (Wait)"; // Fallback seguro
        }

        private string GetButtonFromJson(string jsonButton)
        {
            foreach (var kvp in _buttonToJson) if (kvp.Value == jsonButton) return kvp.Key;
            return EMPTY_CONTROL_LABEL; // Fallback seguro
        }

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
            if (!chkConnect.Checked || _gamepadOutput == null)
            {
                MessageBox.Show("Você precisa conectar o controle virtual primeiro (Botão no topo).", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            AutomationProgram program = CreateAutomationProgram();
            Xbox360GamepadOutput output = _gamepadOutput;

            // Trava os botões para o usuário não clicar duas vezes
            ToggleUI(false);
            ClearVisualLog();
            Log("=====================================");
            Log(" INICIANDO AUTOMAÇÃO AUTOGAMEPAD");
            Log("=====================================");

            // Cria o token que permite interromper a automação a qualquer momento
            var cancellationSource = new CancellationTokenSource();
            _cancellationTokenSource = cancellationSource;

            try
            {
                ResetControllerState();
                PlaySound(true);

                // O motor recebe somente um snapshot imutável e não acessa controles WinForms.
                var engine = new AutomationEngine(output, Log);
                _automationTask = Task.Run(() => engine.RunAsync(program, cancellationSource.Token), cancellationSource.Token);
                await _automationTask;
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
                // Garante ponto morto em término normal, cancelamento ou falha do motor.
                TryResetControllerState();
                PlaySound(false);

                _automationTask = null;
                if (ReferenceEquals(_cancellationTokenSource, cancellationSource))
                {
                    _cancellationTokenSource = null;
                }
                cancellationSource.Dispose();
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

        // --- ZERA O CONTROLE (PONTO MORTO) ---
        // Solta todos os botões e zera os eixos sem precisar desconectar o USB
        private void ResetControllerState()
        {
            _gamepadOutput?.Reset();
        }

        private void TryResetControllerState()
        {
            try
            {
                ResetControllerState();
            }
            catch (Exception ex)
            {
                Log($"[ERRO] Não foi possível neutralizar o controle: {ex.Message}");
            }
        }


        // --- DESCONECTA E LIMPA A MEMÓRIA ---
        private void DisconnectController()
        {
            _gamepadOutput?.Dispose();
            _gamepadOutput = null;
        }

        // Converte o estado atual da UI em dados imutáveis antes de iniciar o motor.
        private AutomationProgram CreateAutomationProgram()
        {
            var steps = new List<AutomationStep>();

            foreach (DataGridViewRow row in gridSequence.Rows)
            {
                if (row.Cells["colAction"].Value == null)
                {
                    continue;
                }

                string actionLabel = row.Cells["colAction"].Value?.ToString() ?? "";
                string controlLabel = row.Cells["colButton"].Value?.ToString() ?? "";
                string controlJsonId = _buttonToJson.GetValueOrDefault(controlLabel, "None");

                steps.Add(new AutomationStep(
                    ParseAction(actionLabel),
                    actionLabel,
                    GamepadControlCatalog.FromJsonId(controlJsonId),
                    controlLabel,
                    ParseCell(row, "colValue", 100),
                    ParseCell(row, "colRampMin", 0),
                    ParseCell(row, "colRampMax", 0),
                    ParseCell(row, "colMinTime", 0),
                    ParseCell(row, "colMaxTime", 0),
                    ParseCell(row, "colJitter", 0)));
            }

            return new AutomationProgram(
                chkLimitCycles.Checked,
                (int)numMaxCycles.Value,
                chkEnableJitter.Checked,
                (int)numJitterFreq.Value,
                steps.AsReadOnly());
        }

        private static ActionType ParseAction(string actionLabel)
        {
            return actionLabel switch
            {
                "Pressionar e Soltar (Tap)" => ActionType.PressAndRelease,
                "Manter Pressionado (Hold)" => ActionType.Hold,
                "Soltar Botão/Eixo (Release)" => ActionType.Release,
                _ => ActionType.Wait
            };
        }

        private static int ParseCell(DataGridViewRow row, string columnName, int fallback)
        {
            return int.TryParse(row.Cells[columnName].Value?.ToString(), out int value) ? value : fallback;
        }

        private GamepadControl GetGamepadControl(string controlLabel)
        {
            string jsonId = _buttonToJson.GetValueOrDefault(controlLabel, "None");
            return GamepadControlCatalog.FromJsonId(jsonId);
        }

        // --- EXPORTA A TELA PARA UMA STRING JSON ---
        private string ExportProfileToJson()
        {
            var profile = new AutoGamepadProfile
            {
                UseCycleLimit = chkLimitCycles.Checked,
                MaxCycles = (int)numMaxCycles.Value,
                EnableGlobalJitter = chkEnableJitter.Checked,
                JitterFrequencyMs = (int)numJitterFreq.Value
            };

            foreach (DataGridViewRow row in gridSequence.Rows)
            {
                if (row.Cells["colAction"].Value == null) continue;

                // Lê o nome da tela
                string rawAction = row.Cells["colAction"].Value?.ToString() ?? "";
                string rawButton = row.Cells["colButton"].Value?.ToString() ?? "";

                var step = new SequenceStep
                {
                    // Converte pro nome curto usando o Dicionário
                    Action = _actionToJson.ContainsKey(rawAction) ? _actionToJson[rawAction] : "Wait",
                    Button = _buttonToJson.ContainsKey(rawButton) ? _buttonToJson[rawButton] : "None",

                    ValuePercent = int.TryParse(row.Cells["colValue"].Value?.ToString(), out int v) ? v : 100,
                    RampMin = int.TryParse(row.Cells["colRampMin"].Value?.ToString(), out int rMin) ? rMin : 0,
                    RampMax = int.TryParse(row.Cells["colRampMax"].Value?.ToString(), out int rMax) ? rMax : 0,
                    WaitMin = int.TryParse(row.Cells["colMinTime"].Value?.ToString(), out int tMin) ? tMin : 0,
                    WaitMax = int.TryParse(row.Cells["colMaxTime"].Value?.ToString(), out int tMax) ? tMax : 0,
                    JitterForce = int.TryParse(row.Cells["colJitter"].Value?.ToString(), out int jf) ? jf : 0
                };

                profile.Steps.Add(step);
            }

            var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            return JsonSerializer.Serialize(profile, options);
        }

        // --- IMPORTA UMA STRING JSON PARA A TELA ---
        // O parâmetro isPreview diz se é só um teste do botão "Checar" (true) ou se veio de um arquivo salvo (false)
        private bool ImportProfileFromJson(string jsonText, bool isPreview = false)
        {
            try
            {
                var profile = JsonSerializer.Deserialize<AutoGamepadProfile>(jsonText);
                if (profile == null || profile.Steps == null) return false;

                // --- CHECK DE SEGURANÇA (VALIDAÇÃO DE INTEGRIDADE DO JSON) ---
                foreach (var step in profile.Steps)
                {
                    // Verifica se a Ação e o Botão do arquivo existem nos nossos dicionários oficiais
                    if (!_actionToJson.ContainsValue(step.Action))
                    {
                        if (!isPreview) MessageBox.Show($"Ação desconhecida encontrada no arquivo: '{step.Action}'. O perfil não pode ser carregado.", "Arquivo Corrompido", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false; // Aborta tudo
                    }

                    if (!_buttonToJson.ContainsValue(step.Button))
                    {
                        if (!isPreview) MessageBox.Show($"Botão/Eixo desconhecido encontrado no arquivo: '{step.Button}'. O perfil não pode ser carregado.", "Arquivo Corrompido", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false; // Aborta tudo
                    }
                }

                // Se passou pelo check de segurança, limpa a tabela para receber os dados
                gridSequence.Rows.Clear();

                chkLimitCycles.Checked = profile.UseCycleLimit;
                numMaxCycles.Value = Math.Max(numMaxCycles.Minimum, Math.Min(numMaxCycles.Maximum, profile.MaxCycles));
                chkEnableJitter.Checked = profile.EnableGlobalJitter;
                numJitterFreq.Value = Math.Max(numJitterFreq.Minimum, Math.Min(numJitterFreq.Maximum, profile.JitterFrequencyMs));

                foreach (var step in profile.Steps)
                {
                    int rowIndex = gridSequence.Rows.Add();
                    var row = gridSequence.Rows[rowIndex];

                    _isConfiguringSequenceRow = true;
                    try
                    {
                        // Lê o nome curto do JSON e devolve a frase longa pra Tabela
                        row.Cells["colAction"].Value = GetActionFromJson(step.Action);
                        row.Cells["colButton"].Value = GetButtonFromJson(step.Button);

                        row.Cells["colValue"].Value = step.ValuePercent.ToString();
                        row.Cells["colRampMin"].Value = step.RampMin.ToString();
                        row.Cells["colRampMax"].Value = step.RampMax.ToString();
                        row.Cells["colMinTime"].Value = step.WaitMin.ToString();
                        row.Cells["colMaxTime"].Value = step.WaitMax.ToString();
                        row.Cells["colJitter"].Value = step.JitterForce.ToString();
                    }
                    finally
                    {
                        _isConfiguringSequenceRow = false;
                    }

                    ConfigureSequenceRow(row, configureButtonOptions: true);
                }

                _sequenceNeedsValidation = true;

                // Se não é só um preview, avisa o usuário que importou com sucesso e já roda a validação lógica
                if (!isPreview)
                {
                    if (!ValidateSequence())
                    {
                        MessageBox.Show("Arquivo carregado, mas existem ERROS LÓGICOS (Tempo negativo ou ordem errada). Eles foram marcados em vermelho na tabela.", "Aviso de Lógica", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                return true;
            }
            catch
            {
                return false;
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
            tabEditor.Enabled = isIdle;
            btnRowAdd.Enabled = isIdle;
            btnRowRemove.Enabled = isIdle;
            btnRowUp.Enabled = isIdle;
            btnRowDown.Enabled = isIdle;
            btnJsonPaste.Enabled = isIdle;
            btnJsonValidate.Enabled = isIdle;
            btnJsonCopy.Enabled = isIdle;
            btnSaveProfile.Enabled = isIdle;
            btnLoadProfile.Enabled = isIdle;

            // Travas de Ciclo e Som
            chkLimitCycles.Enabled = isIdle;
            chkSound.Enabled = isIdle;

            // Só libera a caixinha de número se estiver em "Idle" E o Checkbox de limite estiver marcado
            numMaxCycles.Enabled = isIdle && chkLimitCycles.Checked;

            // --- Travas do Jitter Global ---
            chkEnableJitter.Enabled = isIdle;
            // Só libera a frequência se estiver em Idle E o checkbox de tremor estiver marcado
            numJitterFreq.Enabled = isIdle && chkEnableJitter.Checked;
        }

        private void ClearVisualLog()
        {
            _logUIBuffer.Clear();
            rtbLog.Clear();
        }

        // --- LOGGER SEGURO (RAM E DISCO) ---
        private void Log(string message)
        {
            // Gera o Timestamp com milissegundos
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string formattedMessage = $"[{timestamp}] {message}";

            // 1. ATUALIZAÇÃO DA TELA PRETA (RAM)
            // Usa o Invoke para garantir que as Threads assíncronas do Motor Físico não causem crash na UI
            if (rtbLog != null && !rtbLog.IsDisposed && rtbLog.IsHandleCreated)
            {
                try
                {
                    void AppendToVisualLog()
                    {
                        // Se a fila já tem 500 linhas, joga a mais velha fora
                        if (_logUIBuffer.Count >= MAX_LOG_LINES_UI)
                        {
                            _logUIBuffer.Dequeue();
                        }

                        _logUIBuffer.Enqueue(formattedMessage);

                        // Pega a fila toda, junta com "Quebra de Linha" e manda pra tela de uma vez
                        rtbLog.Text = string.Join(Environment.NewLine, _logUIBuffer);

                        // Rola a caixa preta para o final
                        rtbLog.SelectionStart = rtbLog.Text.Length;
                        rtbLog.ScrollToCaret();
                    }

                    if (rtbLog.InvokeRequired)
                    {
                        rtbLog.Invoke((Action)AppendToVisualLog);
                    }
                    else
                    {
                        AppendToVisualLog();
                    }
                }
                catch (InvalidOperationException)
                {
                    // A janela pode estar destruindo o handle durante o encerramento.
                }
            }

            // 2. GRAVAÇÃO EM DISCO RÍGIDO (Buffer Batching)
            // Usamos a trava "lock" porque o C# pode rodar múltiplas coisas ao mesmo tempo e estourar o arquivo
            lock (_logLock)
            {
                try
                {
                    // Se o caminho do arquivo ainda não foi criado, cria agora!
                    if (string.IsNullOrEmpty(_currentLogFilePath))
                    {
                        // Garante que a pasta "Logs" existe na mesma pasta do .exe
                        string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                        if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);

                        // Cria o nome do arquivo com a data e hora de agora
                        string dateStr = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                        _currentLogFilePath = Path.Combine(logDir, $"AutoGamepad_{dateStr}.log");
                    }

                    _logDiskBuffer.Add(formattedMessage);

                    // Se o pacote de mensagens bateu a cota (50 linhas), despeja no HD e limpa o pacote
                    if (_logDiskBuffer.Count >= MAX_LOG_DISK_BUFFER)
                    {
                        FlushLogToDisk();
                    }
                }
                catch (IOException)
                {
                    // O log não pode interromper o motor.
                }
                catch (UnauthorizedAccessException)
                {
                    // O diretório do executável pode não permitir escrita.
                }
            }
        }

        // Função auxiliar que abre o arquivo de texto, insere o pacote e fecha
        private void FlushLogToDisk()
        {
            lock (_logLock)
            {
                if (_logDiskBuffer.Count == 0 || string.IsNullOrEmpty(_currentLogFilePath)) return;

                try
                {
                    File.AppendAllLines(_currentLogFilePath, _logDiskBuffer);
                    _logDiskBuffer.Clear(); // Esvazia o buffer da memória RAM
                }
                catch
                {
                    // Falha silenciosa para não travar a automação caso o antivírus esteja escaneando o arquivo na hora
                }
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
            // Cancela o motor antes de neutralizar e desconectar o dispositivo.
            _cancellationTokenSource?.Cancel();
            TryResetControllerState();
            DisconnectController();

            // Devolve as teclas pro Windows ao fechar o programa
            UnregisterHotKey(this.Handle, HOTKEY_ID_START);
            UnregisterHotKey(this.Handle, HOTKEY_ID_STOP);

            // Salva qualquer mensagem de Log que sobrou perdida na memória.
            FlushLogToDisk();

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
                    _gamepadOutput = Xbox360GamepadOutput.Connect();

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
            foreach (string buttonLabel in _buttonToJson.Keys)
            {
                // A coluna mantém a lista completa como template. Cada linha recebe
                // depois sua própria lista contextual por meio de ConfigureSequenceRow.
                buttonColumn.Items.Add(buttonLabel);
            }

            // BLOQUEIO DE ORDENAÇÃO: Impede o usuário de reordenar a tabela clicando no cabeçalho
            foreach (DataGridViewColumn col in gridSequence.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        // --- BOTÃO: ADICIONAR LINHA ---
        private void btnRowAdd_Click(object sender, EventArgs e)
        {
            int rowIndex = gridSequence.Rows.Add();
            DataGridViewRow row = gridSequence.Rows[rowIndex];

            _isConfiguringSequenceRow = true;
            try
            {
                row.Cells["colAction"].Value = "Pressionar e Soltar (Tap)";
                row.Cells["colButton"].Value = DEFAULT_CONTROL_LABEL;

                // Coloca valores padrão em tudo pra não ficar vazio
                row.Cells["colValue"].Value = "100";
                row.Cells["colRampMin"].Value = "0";
                row.Cells["colRampMax"].Value = "0";
                row.Cells["colMinTime"].Value = "100";
                row.Cells["colMaxTime"].Value = "100";
                row.Cells["colJitter"].Value = "0";
            }
            finally
            {
                _isConfiguringSequenceRow = false;
            }

            gridSequence.FirstDisplayedScrollingRowIndex = rowIndex;

            ConfigureSequenceRow(row, configureButtonOptions: true);

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

        // --- DETECTA MUDANÇAS DENTRO DA TABELA E BLOQUEIA CÉLULAS ---
        private void gridSequence_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isConfiguringSequenceRow || e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = gridSequence.Columns[e.ColumnIndex].Name;

            // Se o usuário alterou a Ação ou o Botão, recalcula as travas
            if (colName == "colAction")
            {
                ConfigureSequenceRow(gridSequence.Rows[e.RowIndex], configureButtonOptions: true);
            }
            else if (colName == "colButton")
            {
                ConfigureSequenceRow(gridSequence.Rows[e.RowIndex], configureButtonOptions: false);
            }

            _sequenceNeedsValidation = true;
        }

        private void ConfigureSequenceRow(DataGridViewRow row, bool configureButtonOptions)
        {
            if (_isConfiguringSequenceRow) return;

            _isConfiguringSequenceRow = true;
            try
            {
                string actionLabel = row.Cells["colAction"].Value?.ToString() ?? "";
                ActionType action = ParseAction(actionLabel);
                var buttonCell = (DataGridViewComboBoxCell)row.Cells["colButton"];

                if (configureButtonOptions)
                {
                    string currentLabel = buttonCell.Value?.ToString() ?? EMPTY_CONTROL_LABEL;
                    GamepadControl currentControl = GetGamepadControl(currentLabel);
                    GamepadControl normalizedControl = SequenceGridRules.NormalizeControl(action, currentControl);
                    bool isButtonEditable = SequenceGridRules.IsControlEditable(action);

                    string normalizedLabel = normalizedControl switch
                    {
                        GamepadControl.None => EMPTY_CONTROL_LABEL,
                        GamepadControl.A when currentControl == GamepadControl.None => DEFAULT_CONTROL_LABEL,
                        _ => currentLabel
                    };

                    // O valor é removido antes dos itens para evitar que o DataGridView
                    // tente renderizar temporariamente uma opção que não existe na lista.
                    buttonCell.Value = null;
                    buttonCell.Items.Clear();

                    if (isButtonEditable)
                    {
                        foreach (string availableButtonLabel in _buttonToJson.Keys)
                        {
                            if (availableButtonLabel != EMPTY_CONTROL_LABEL)
                            {
                                buttonCell.Items.Add(availableButtonLabel);
                            }
                        }
                    }
                    else
                    {
                        buttonCell.Items.Add(EMPTY_CONTROL_LABEL);
                    }

                    buttonCell.ReadOnly = !isButtonEditable;
                    buttonCell.Style.BackColor = isButtonEditable ? System.Drawing.Color.White : System.Drawing.Color.LightGray;
                    buttonCell.DisplayStyle = isButtonEditable
                        ? DataGridViewComboBoxDisplayStyle.DropDownButton
                        : DataGridViewComboBoxDisplayStyle.Nothing;
                    buttonCell.Value = normalizedLabel;
                }

                string buttonLabel = buttonCell.Value?.ToString() ?? EMPTY_CONTROL_LABEL;
                bool isAxis = GamepadControlCatalog.TryGetAxisBinding(GetGamepadControl(buttonLabel), out _);

                DataGridViewCell cellValue = row.Cells["colValue"];
                DataGridViewCell cellRampMin = row.Cells["colRampMin"];
                DataGridViewCell cellRampMax = row.Cells["colRampMax"];
                DataGridViewCell cellTimeMin = row.Cells["colMinTime"];
                DataGridViewCell cellTimeMax = row.Cells["colMaxTime"];
                DataGridViewCell cellJitter = row.Cells["colJitter"];

                static void SetCellState(DataGridViewCell cell, bool enabled, string defaultValue = "-")
                {
                    cell.ReadOnly = !enabled;
                    cell.Style.BackColor = enabled ? System.Drawing.Color.White : System.Drawing.Color.LightGray;
                    if (!enabled) cell.Value = "-";
                    else if (cell.Value?.ToString() == "-") cell.Value = defaultValue;
                }

                switch (action)
                {
                    case ActionType.Wait:
                        SetCellState(cellValue, false);
                        SetCellState(cellRampMin, false);
                        SetCellState(cellRampMax, false);
                        SetCellState(cellTimeMin, true, "100");
                        SetCellState(cellTimeMax, true, "100");
                        SetCellState(cellJitter, false);
                        break;

                    case ActionType.PressAndRelease:
                        SetCellState(cellValue, isAxis, "100");
                        SetCellState(cellRampMin, isAxis, "0");
                        SetCellState(cellRampMax, isAxis, "0");
                        SetCellState(cellTimeMin, true, "100");
                        SetCellState(cellTimeMax, true, "100");
                        SetCellState(cellJitter, isAxis, "0");
                        break;

                    case ActionType.Hold:
                        SetCellState(cellValue, isAxis, "100");
                        SetCellState(cellRampMin, isAxis, "0");
                        SetCellState(cellRampMax, isAxis, "0");
                        SetCellState(cellTimeMin, false);
                        SetCellState(cellTimeMax, false);
                        SetCellState(cellJitter, isAxis, "0");
                        break;

                    case ActionType.Release:
                        SetCellState(cellValue, false);
                        SetCellState(cellRampMin, isAxis, "0");
                        SetCellState(cellRampMax, isAxis, "0");
                        SetCellState(cellTimeMin, false);
                        SetCellState(cellTimeMax, false);
                        SetCellState(cellJitter, false);
                        break;
                }
            }
            finally
            {
                _isConfiguringSequenceRow = false;
            }

            _sequenceNeedsValidation = true;
        }

        // Um clique seleciona a célula e já abre o dropdown quando aplicável.
        private void gridSequence_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            DataGridViewCell cell = gridSequence.Rows[e.RowIndex].Cells[e.ColumnIndex];
            if (cell.ReadOnly || cell is not DataGridViewComboBoxCell) return;

            gridSequence.CurrentCell = cell;
            if (!gridSequence.IsCurrentCellInEditMode)
            {
                gridSequence.BeginEdit(selectAll: true);
            }

            if (gridSequence.EditingControl is DataGridViewComboBoxEditingControl comboBox)
            {
                comboBox.DroppedDown = true;
            }
        }

        // ComboBoxes mantêm o valor como pendente até perderem o foco por padrão.
        // O commit imediato dispara CellValueChanged assim que uma opção é escolhida.
        private void gridSequence_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (gridSequence.IsCurrentCellDirty
                && gridSequence.CurrentCell is DataGridViewComboBoxCell)
            {
                gridSequence.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
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

                if (colName == "colValue" || colName == "colRampMin" || colName == "colRampMax" || colName == "colMinTime" || colName == "colMaxTime" || colName == "colJitter")
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

            if (colName == "colValue" || colName == "colRampMin" || colName == "colRampMax" || colName == "colMinTime" || colName == "colMaxTime" || colName == "colJitter")
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
                else if (colName == "colRampMin" || colName == "colRampMax" || colName == "colMinTime" || colName == "colMaxTime" || colName == "colJitter")
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
            var heldControls = new HashSet<GamepadControl>();
            var heldControlLabels = new Dictionary<GamepadControl, string>();
            var heldAxisChannels = new Dictionary<AxisChannel, (GamepadControl Control, string Label)>();

            // Limpa as cores vermelhas antigas da tabela antes de checar de novo
            foreach (DataGridViewRow row in gridSequence.Rows)
            {
                row.DefaultCellStyle.BackColor = System.Drawing.Color.White;
                row.ErrorText = ""; // Remove o ícone de erro
            }

            if (gridSequence.Rows.Count == 0)
            {
                Log("[ERRO] A sequência está vazia. Adicione pelo menos uma etapa antes de iniciar.");
                return false;
            }

            // Lê linha por linha do começo ao fim
            for (int i = 0; i < gridSequence.Rows.Count; i++)
            {
                var row = gridSequence.Rows[i];
                if (row.Cells["colAction"].Value == null) continue;

                string action = row.Cells["colAction"].Value?.ToString() ?? "";
                string button = row.Cells["colButton"].Value?.ToString() ?? "";
                ActionType actionType = ParseAction(action);
                GamepadControl control = GetGamepadControl(button);
                bool isAxis = GamepadControlCatalog.TryGetAxisBinding(control, out AxisBinding axisBinding);

                // Checagem de lógica: Soltar um botão que não está pressionado
                if (actionType == ActionType.Release)
                {
                    if (!heldControls.Contains(control))
                    {
                        MarkRowAsError(row, $"Tentando soltar '{button}', mas ele não foi mantido pressionado anteriormente!");
                        isValid = false;
                    }
                    else
                    {
                        heldControls.Remove(control);
                        heldControlLabels.Remove(control);

                        if (isAxis
                            && heldAxisChannels.TryGetValue(axisBinding.Channel, out var heldAxis)
                            && heldAxis.Control == control)
                        {
                            heldAxisChannels.Remove(axisBinding.Channel);
                        }
                    }
                }

                // Checagem de lógica: Segurar um botão
                else if (actionType == ActionType.Hold)
                {
                    if (heldControls.Contains(control))
                    {
                        MarkRowAsError(row, $"O botão '{button}' já está sendo segurado!");
                        isValid = false;
                    }
                    else if (isAxis && heldAxisChannels.TryGetValue(axisBinding.Channel, out var heldAxis))
                    {
                        MarkRowAsError(row, $"O eixo físico de '{button}' já está sendo controlado por '{heldAxis.Label}'. Solte a direção anterior primeiro!");
                        isValid = false;
                    }
                    else
                    {
                        heldControls.Add(control);
                        heldControlLabels[control] = button;

                        if (isAxis)
                        {
                            heldAxisChannels[axisBinding.Channel] = (control, button);
                        }
                    }
                }
                // Checagem de lógica: Dar Tap em um botão que está preso
                else if (actionType == ActionType.PressAndRelease)
                {
                    if (heldControls.Contains(control))
                    {
                        MarkRowAsError(row, $"Não é possível dar 'Tap' no '{button}', pois ele está travado por um 'Hold' anterior. Use 'Soltar' primeiro!");
                        isValid = false;
                    }
                    else if (isAxis && heldAxisChannels.TryGetValue(axisBinding.Channel, out var heldAxis))
                    {
                        MarkRowAsError(row, $"Não é possível mover '{button}' enquanto '{heldAxis.Label}' mantém o mesmo eixo físico pressionado.");
                        isValid = false;
                    }
                }

                // O traço representa uma célula desabilitada. Qualquer outro texto precisa caber em Int32.
                bool validMinTime = TryReadNumericCell(row, "colMinTime", out int minTime);
                bool validMaxTime = TryReadNumericCell(row, "colMaxTime", out int maxTime);

                if (!validMinTime || !validMaxTime)
                {
                    MarkRowAsError(row, "O Tempo de Duração excede o limite numérico permitido.");
                    isValid = false;
                }
                else if (minTime < 0 || maxTime < 0)
                {
                    MarkRowAsError(row, "O Tempo de Duração não pode ser negativo.");
                    isValid = false;
                }
                else if (minTime > maxTime)
                {
                    MarkRowAsError(row, "O Tempo Mínimo não pode ser maior que o Tempo Máximo.");
                    isValid = false;
                }

                bool validRampMin = TryReadNumericCell(row, "colRampMin", out int rampMin);
                bool validRampMax = TryReadNumericCell(row, "colRampMax", out int rampMax);

                if (!validRampMin || !validRampMax)
                {
                    MarkRowAsError(row, "O Tempo de Rampa excede o limite numérico permitido.");
                    isValid = false;
                }
                else if (rampMin < 0 || rampMax < 0)
                {
                    MarkRowAsError(row, "O Tempo de Rampa não pode ser negativo.");
                    isValid = false;
                }
                else if (rampMin > rampMax)
                {
                    MarkRowAsError(row, "A Rampa Mínima não pode ser maior que a Rampa Máxima.");
                    isValid = false;
                }

                bool validJitter = TryReadNumericCell(row, "colJitter", out int jitterForce);
                if (!validJitter)
                {
                    MarkRowAsError(row, "O Tremor de Eixo (Jitter) excede o limite numérico permitido.");
                    isValid = false;
                }
                else if (jitterForce < 0)
                {
                    MarkRowAsError(row, "O Tremor de Eixo (Jitter) não pode ser negativo.");
                    isValid = false;
                }

                // Checagem Lógica: Manter eixo em 0% ou maior que 100%
                if (isAxis)
                {
                    bool validAxisValue = TryReadNumericCell(row, "colValue", out int axisValue);

                    if (!validAxisValue || axisValue < 0 || axisValue > 100)
                    {
                        MarkRowAsError(row, "O Valor do Eixo deve estar entre 0% e 100%.");
                        isValid = false;
                    }

                    if (actionType == ActionType.Hold && axisValue == 0)
                    {
                        MarkRowAsError(row, "Manter um Eixo em 0% não tem efeito lógico. Use a ação 'Soltar'.");
                        isValid = false;
                    }
                }
            }

            // Checagem Final: Esqueceu de soltar algum botão?
            if (heldControls.Count > 0)
            {
                // Se sobrou botão segurado, marca a ÚLTIMA linha como errada para avisar o usuário
                var lastRow = gridSequence.Rows[gridSequence.Rows.Count - 1];
                string botoesPresos = string.Join(", ", heldControlLabels.Values);
                MarkRowAsError(lastRow, $"A sequência terminou, mas você esqueceu de soltar: {botoesPresos}!");
                isValid = false;
            }

            return isValid;
        }

        private static bool TryReadNumericCell(DataGridViewRow row, string columnName, out int value)
        {
            string text = row.Cells[columnName].Value?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(text) || text == "-")
            {
                value = 0;
                return true;
            }

            return int.TryParse(text, out value);
        }

        // Pinta a linha de vermelho, coloca o ícone e avisa no Log
        private void MarkRowAsError(DataGridViewRow row, string message)
        {
            row.DefaultCellStyle.BackColor = System.Drawing.Color.LightCoral;
            row.ErrorText = message;
            // Imprime o erro no console preto para o usuário achar fácil
            Log($"[ERRO NA LINHA {row.Index + 1}] {message}");
        }

        // --- ATIVA/DESATIVA A CHECKBOX DE CICLOS ---
        private void chkLimitCycles_CheckedChanged(object sender, EventArgs e)
        {
            numMaxCycles.Enabled = chkLimitCycles.Checked;
        }

        private void chkEnableJitter_CheckedChanged(object sender, EventArgs e)
        {
            // Ativa ou desativa a caixa de Frequência quando o usuário marca/desmarca o Jitter
            numJitterFreq.Enabled = chkEnableJitter.Checked;
        }

        // --- DETECTA MUDANÇA DE ABA (Tabela <-> Código) ---
        private void tabEditor_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Se o usuário foi para a Aba 1 (Índice 1 = A aba de Código)
            if (tabEditor.SelectedIndex == 1)
            {
                // Verifica se a tabela tem erros matemáticos primeiro
                if (!ValidateSequence())
                {
                    MessageBox.Show("Corrija os erros na tabela antes de visualizar o código.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    tabEditor.SelectedIndex = 0; // Joga ele de volta pra tabela
                    return;
                }

                // Tabela OK! Gera o texto JSON e joga na caixa preta
                txtJsonCode.Text = ExportProfileToJson();
            }
        }

        // --- BOTÃO COPIAR ---
        private void btnJsonCopy_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtJsonCode.Text))
            {
                Clipboard.SetText(txtJsonCode.Text);
                MessageBox.Show("Código copiado para a área de transferência!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // --- BOTÃO COLAR ---
        private void btnJsonPaste_Click(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                txtJsonCode.Text = Clipboard.GetText();
            }
        }

        // --- BOTÃO CHECAR SINTAXE (Aba JSON) ---
        private void btnJsonValidate_Click(object sender, EventArgs e)
        {
            // Tenta importar. Passamos 'true' pois é só um Preview (não queremos o popup de sucesso do import)
            bool success = ImportProfileFromJson(txtJsonCode.Text, true);

            if (success)
            {
                // Se a sintaxe JSON tava certa, roda o validador lógico pra checar os negativos
                if (!ValidateSequence())
                {
                    MessageBox.Show("Sintaxe JSON correta, mas existem ERROS LÓGICOS (Tempo negativo, ordens inválidas). Corrija na Tabela Visual.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show("Sintaxe JSON e Lógica Perfeitas! A tabela visual foi atualizada.", "Validado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("Erro de Sintaxe no JSON! Verifique se você não apagou vírgulas ou chaves '{}'.", "Erro Fatal", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- BOTÃO: SALVAR PERFIL (GERA O ARQUIVO .JSON) ---
        private void btnSaveProfile_Click(object sender, EventArgs e)
        {
            // Roda o validador lógico primeiro (Avisa o usuário, mas não o impede de salvar com erros)
            if (!ValidateSequence())
            {
                var dialogResult = MessageBox.Show("Sua sequência possui ERROS LÓGICOS (linhas vermelhas). Se salvar assim, não será possível iniciar a automação depois.\n\nDeseja salvar o arquivo mesmo assim?", "Aviso de Validação", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (dialogResult == DialogResult.No) return; // O cara desistiu de salvar
            }

            // Pega o texto da tabela convertido em JSON
            string jsonContent = ExportProfileToJson();

            // Abre a janela do Windows para salvar o arquivo
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Arquivos JSON do AutoGamepad (*.json)|*.json";
                sfd.Title = "Salvar Perfil de Automação";
                sfd.FileName = "MeuPerfil.json"; // Sugestão de nome

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Escreve o texto gerado no arquivo escolhido
                        File.WriteAllText(sfd.FileName, jsonContent);
                        MessageBox.Show("Perfil salvo com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao tentar salvar o arquivo: {ex.Message}", "Erro de Gravação", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // --- BOTÃO: CARREGAR PERFIL (LÊ O ARQUIVO .JSON) ---
        private void btnLoadProfile_Click(object sender, EventArgs e)
        {
            // Abre a janela do Windows para buscar o arquivo
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Arquivos JSON do AutoGamepad (*.json)|*.json";
                ofd.Title = "Carregar Perfil de Automação";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Lê o texto do arquivo
                        string jsonContent = File.ReadAllText(ofd.FileName);

                        // Tenta jogar pra tabela usando a função que já fizemos
                        // Passamos 'false' pra avisar que é um import real, assim ele roda a validação pesada
                        bool success = ImportProfileFromJson(jsonContent, false);

                        if (success)
                        {
                            // Joga o texto lido na aba Código também para sincronizar a tela
                            txtJsonCode.Text = jsonContent;
                            MessageBox.Show("Perfil carregado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("O arquivo selecionado está corrompido ou possui formatação JSON inválida. O Perfil não pôde ser carregado.", "Erro Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao tentar abrir o arquivo: {ex.Message}", "Erro de Leitura", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }

    // --- MOLDES PARA SALVAR O JSON ---

    // Representa o Perfil Completo (O Arquivo todo)
    public class AutoGamepadProfile
    {
        public bool UseCycleLimit { get; set; }
        public int MaxCycles { get; set; }
        public bool EnableGlobalJitter { get; set; }
        public int JitterFrequencyMs { get; set; }

        // A lista de passos da tabela
        public List<SequenceStep> Steps { get; set; } = new List<SequenceStep>();
    }

    // Representa uma única linha da Tabela
    public class SequenceStep
    {
        public string Action { get; set; } = "";
        public string Button { get; set; } = "";
        public int ValuePercent { get; set; }
        public int RampMin { get; set; }
        public int RampMax { get; set; }
        public int WaitMin { get; set; }
        public int WaitMax { get; set; }
        public int JitterForce { get; set; }
    }
}
