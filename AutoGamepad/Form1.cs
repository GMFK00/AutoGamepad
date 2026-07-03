using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System.Runtime.InteropServices;

namespace AutoGamepad
{
    public partial class Form1 : Form
    {
        // Variáveis globais do controle e do cancelamento
        private ViGEmClient? _client;
        private IXbox360Controller? _controller;
        private CancellationTokenSource? _cancellationTokenSource;
        private Random _rnd = new Random();

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

            // Seleciona o primeiro item da lista do ComboBox por padrão ao abrir
            if (cmbButtonConfig.Items.Count > 0)
                cmbButtonConfig.SelectedIndex = 0;

            // Adiciona as Hotkeys: CTRL + SHIFT + F9 | CTRL + SHIFT + F10
            // O operador '|' soma os bits do Control e do Shift
            RegisterHotKey(this.Handle, HOTKEY_ID_START, MOD_CONTROL | MOD_SHIFT, VK_F9);
            RegisterHotKey(this.Handle, HOTKEY_ID_STOP, MOD_CONTROL | MOD_SHIFT, VK_F10);

            Log("Atalhos globais ativados: [Ctrl+Shift+F9] Iniciar | [Ctrl+Shift+F10] Parar");
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
            // Proteção matemática: Mínimo não pode ser maior que Máximo
            if (numHoldMin.Value > numHoldMax.Value || numWaitMin.Value > numWaitMax.Value)
            {
                MessageBox.Show("Os valores 'Min' não podem ser maiores que os valores 'Max'!", "Erro de Configuração", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
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

        // --- A AUTOMAÇÃO ---
        private async Task RunAutomationAsync(CancellationToken token)
        {
            // 1. Lê as configurações da tela
            int initialDelay = (int)numInitialDelay.Value * 1000; // Segundos para Milissegundos
            int maxCycles = (int)numMaxCycles.Value;
            Xbox360Button selectedButton = GetSelectedButton();

            // 2. Atraso Inicial (Para você dar Alt+Tab)
            if (initialDelay > 0)
            {
                Log($"Aguardando {initialDelay / 1000} segundos para iniciar... Vá para o jogo!");
                await Task.Delay(initialDelay, token);
            }

            // 3. O Loop Principal
            int currentCycle = 1;
            while (!token.IsCancellationRequested)
            {
                // Verifica limite de ciclos
                if (maxCycles > 0 && currentCycle > maxCycles)
                {
                    Log($"\n[INFO] Limite de {maxCycles} ciclos atingido. Finalizando.");
                    break;
                }

                int holdTime = _rnd.Next((int)numHoldMin.Value, (int)numHoldMax.Value + 1);
                int waitTime = _rnd.Next((int)numWaitMin.Value, (int)numWaitMax.Value + 1);

                Log($"\n--- Ciclo {currentCycle} ---");
                Log($"[*] Pressionando botão [{cmbButtonConfig.Text}] por {holdTime} ms.");

                // Aperta (O ! após o _controller serve para afirmar ao compilador que sabemos que ele não é nulo aqui)
                _controller!.SetButtonState(selectedButton, true);
                await Task.Delay(holdTime, token);

                // Solta
                _controller.SetButtonState(selectedButton, false);
                Log($"[*] Solto. Aguardando {waitTime / 1000.0:F2} segundos para o próximo.");

                // Espera
                await Task.Delay(waitTime, token);

                currentCycle++;
            }
        }

        // --- FUNÇÕES AUXILIARES ---

        // Transforma o texto do ComboBox em botão de Xbox
        private Xbox360Button GetSelectedButton()
        {
            switch (cmbButtonConfig.SelectedItem?.ToString())
            {
                case "A": return Xbox360Button.A;
                case "B": return Xbox360Button.B;
                case "X": return Xbox360Button.X;
                case "Y": return Xbox360Button.Y;
                case "D-Pad Cima": return Xbox360Button.Up;
                case "D-Pad Baixo": return Xbox360Button.Down;
                case "D-Pad Esquerda": return Xbox360Button.Left;
                case "D-Pad Direita": return Xbox360Button.Right;
                default: return Xbox360Button.A;
            }
        }

        // Desconecta e limpa a memória (Importante!)
        private void DisconnectController()
        {
            if (_controller != null)
            {
                _controller.Disconnect();
                _controller = null;
            }
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }

        // Bloqueia e desbloqueia botões na tela
        private void ToggleUI(bool isIdle)
        {
            btnStart.Enabled = isIdle;
            btnStop.Enabled = !isIdle; // Habilita o Stop apenas quando está rodando

            // Impede o usuário de desconectar o controle no meio da execução
            chkConnect.Enabled = isIdle;

            numHoldMin.Enabled = isIdle;
            numHoldMax.Enabled = isIdle;
            numWaitMin.Enabled = isIdle;
            numWaitMax.Enabled = isIdle;
            numInitialDelay.Enabled = isIdle;
            numMaxCycles.Enabled = isIdle;
            cmbButtonConfig.Enabled = isIdle;
        }

        // Escreve na caixa preta e rola pra baixo
        private void Log(string message)
        {
            rtbLog.AppendText(message + Environment.NewLine);
            rtbLog.ScrollToCaret();
        }

        // Evento que ocorre quando o usuário clica no "X" para fechar a janela
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
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
    }
}