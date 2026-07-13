# AutoGamepad

O **AutoGamepad** é um motor de automação a nível de hardware projetado para Windows. Utilizando a comunicação direta com o driver virtual ViGEmBus, ele permite a emulação de comandos de um controle Xbox 360 virtual, contornando a maioria dos métodos tradicionais de detecção de software de macro.

Este projeto foi desenvolvido com foco na precisão de eixos (gatilhos e analógicos) e em mecanismos para prevenir a detecção comportamental por sistemas Anti-AFK.

## Arquitetura e Funcionalidades

O AutoGamepad opera como um interpretador de sequência de estados, não apenas como um macro de reprodução linear. Suas principais características atuais incluem:

* **Engine de Interpolação a 60 Hz:** Suporte a ações complexas de eixos e gatilhos. Em vez de injetar estados instantâneos estáticos, o programa calcula transições (Rampas) baseadas na duração definida pelo usuário, gerando uma curva de transição natural (Lerp).
* **Sistema de Jitter (Tremor Contínuo):** Funcionalidade específica para eixos. Um ruído aleatório em uma frequência pré-definida é sobreposto ao valor atual do gatilho ou analógico, impossibilitando a leitura de valores estáticos fixos nos logs dos servidores do jogo.
* **Jitter Temporal:** Todo e qualquer atraso (Wait) ou tempo de pressionamento de botão possui margens mínimas e máximas de duração. O algoritmo resolve e varia ativamente o tempo a cada ciclo executado.
* **Validador Lógico (State Machine):** A interface possui um sistema preventivo. A automação avalia toda a cadeia de eventos antes da injeção. Comandos contraditórios ou falhas na lógica de fechamento de botões são identificados e destacados em vermelho, prevenindo crashes no driver ou comportamentos físicos bizarros no jogo-alvo.

## Pré-Requisitos

A execução e/ou compilação deste código exige a presença de bibliotecas externas instaladas no sistema hospedeiro.

* O sistema requer a instalação manual do driver de nível de Kernel para a emulação. Faça o download e instale o [ViGEmBus Driver](https://github.com/nefarius/ViGEmBus/releases).
* A compilação é baseada no framework .NET 10 (Windows Forms).

## Como Utilizar

1. Conecte o controle virtual pela interface superior do aplicativo.
2. Defina o número de ciclos máximos que a rotina deve ser executada antes de encerrar de forma autônoma.
3. Se estiver trabalhando com curvas de eixos ou aceleração contínua, habilite o sistema "Tremor (Eixos)" e defina a frequência (Ex: 100ms para uma variação orgânica).
4. Adicione as linhas na tabela determinando as sequências de comandos:
   * **Pressionar e Soltar (Tap):** Injeta o sinal, pausa durante o período estipulado, e solta automaticamente.
   * **Manter Pressionado (Hold):** Atinge a pressão determinada no eixo ou ativa o botão digital, passando para a próxima linha da lista imediatamente.
   * **Soltar (Release):** Processa a rampa de volta para a posição neutra ou desativa o botão digital e passa adiante.
5. Inicie a execução pelo botão principal ou pela Hotkey Global pré-definida (`Ctrl+Shift+F9`).

## TODO List (Lista de Pendências)
- [ ] Implementação de estrutura nativa de serialização para exportação/importação das tabelas de estado no formato `.json`.
- [ ] Renderização condicional por abas, separando o editor visual (Tabela) de um editor dinâmico por blocos de texto (JSON) bidirecional.
- [ ] Aprimoramento e formatação visual do console de Logs em tempo de execução.

## Licença

Este software é distribuído de forma aberta sob a licença **GPLv3**. Modificações ou distribuições em software de terceiros devem respeitar a integridade da licença livre definida no repositório original. O desenvolvedor isenta-se de qualquer responsabilidade ou punições legais acarretadas pelo uso prático das bibliotecas contidas neste software.