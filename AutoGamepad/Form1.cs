using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace AutoGamepad
{
    public partial class Form1 : Form
    {
        // Variáveis globais do controle e do cancelamento
        private ViGEmClient? _client;
        private IXbox360Controller? _controller;
        private CancellationTokenSource? _cancellationTokenSource;
        private Random _rnd = new Random();

        public Form1()
        {
            InitializeComponent();

            // Seleciona o primeiro item da lista do ComboBox por padrão ao abrir
            if (cmbButtonConfig.Items.Count > 0)
                cmbButtonConfig.SelectedIndex = 0;
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
                // Inicia o motor principal (Lógica isolada para não travar a tela)
                await RunAutomationAsync(_cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Log("[!] Automação interrompida pelo usuário.");
            }
            catch (Exception ex)
            {
                Log($"[ERRO FATAL] {ex.Message}");
                MessageBox.Show("Verifique se o driver ViGEmBus está instalado no Windows.\n\nDetalhes:\n" + ex.Message, "Erro no Driver", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Sempre que terminar (com sucesso, erro ou cancelamento), limpa tudo
                DisconnectController();
                ToggleUI(true);
                Log("Automação finalizada e controle desconectado.");
            }
        }

        // --- BOTÃO PARAR ---
        // Se você ainda não deu 2 cliques nele no design, o Visual Studio vai reconhecer 
        // esse código automaticamente, mas você pode precisar dar 2 cliques lá e colar só o código de dentro.
        private void btnStop_Click(object sender, EventArgs e)
        {
            Log("Solicitando parada imediata...");
            _cancellationTokenSource?.Cancel(); // Avisa o loop para abortar
        }

        // --- O CÉREBRO DA AUTOMAÇÃO ---
        private async Task RunAutomationAsync(CancellationToken token)
        {
            // 1. Conecta o controle virtual
            Log("Iniciando driver do controle virtual...");
            _client = new ViGEmClient();
            _controller = _client.CreateXbox360Controller();
            _controller.Connect();
            Log("[SUCESSO] Controle de Xbox conectado no Windows.");

            // 2. Lê as configurações da tela
            int initialDelay = (int)numInitialDelay.Value * 1000; // Segundos para Milissegundos
            int maxCycles = (int)numMaxCycles.Value;
            Xbox360Button selectedButton = GetSelectedButton();

            // 3. Atraso Inicial (Para você dar Alt+Tab)
            if (initialDelay > 0)
            {
                Log($"Aguardando {initialDelay / 1000} segundos para iniciar... Vá para o jogo!");
                await Task.Delay(initialDelay, token); // Passar o 'token' faz ele abortar o sono se você clicar em Parar
            }

            // 4. O Loop Principal
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

                // Aperta
                _controller.SetButtonState(selectedButton, true);
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

        // Evento que ocorre quando o usuário clica no "X" vermelho da janela pra fechar
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            DisconnectController();
            base.OnFormClosing(e);
        }
    }
}