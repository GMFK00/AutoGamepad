# AutoGamepad 

O **AutoGamepad** é um motor de automação a nível de hardware projetado para sistemas Windows. Interagindo diretamente com o driver de kernel ViGEmBus, ele permite a emulação de comandos de um controle Xbox 360 virtual, contornando limitações de APIs de software convencionais (como SendInput ou DirectInput).

Este projeto tem como foco a alta precisão na manipulação de eixos (gatilhos e analógicos) e mecanismos de evasão comportamental (*Jitter* contínuo), voltados para ambientes restritivos.

## Arquitetura e Funcionalidades (v1.2.0)

O AutoGamepad opera como um interpretador de sequência baseada em Máquina de Estados. Antes da execução, a interface converte a tabela em um snapshot imutável; o motor processa esse snapshot em uma thread de trabalho e não acessa controles WinForms durante os ciclos.

* **Engine de Interpolação a 60 Hz:** Em vez de injeções estáticas de estado, o motor calcula transições de valores (*Rampas*) baseadas em tempo (Linear Interpolation). Isso permite a simulação orgânica da progressão de força em molas de gatilhos ou deslocamento de direcionais.
* **Sistema de Jitter Físico:** Uma função específica para eixos. Um ruído caótico parametrizável é sobreposto ao valor atual do eixo em uma frequência configurável, mascarando padrões estáticos em logs de input de servidores.
* **Jitter Temporal Contínuo:** Todo e qualquer atraso (*Wait*) ou duração de interação apresenta margens dinâmicas (Min/Max). O algoritmo resolve a flutuação do tempo a cada ciclo executado de maneira independente.
* **Validador Semântico de Estado:** Prevenção de falhas. A interface analisa a cadeia de eventos de forma lógica antes da injeção no Kernel, bloqueando comandos paradoxais (ex: tentar executar um *Release* em um botão não submetido a *Hold* prévio).
* **Gestão e Persistência de Perfis (JSON):** Suporte nativo a salvamento e carregamento de configurações de automação. Inclui um editor bidirecional incorporado com validação de esquema de dados.

## Pré-Requisitos e Setup

O funcionamento do software está vinculado à presença do driver de simulação instalado no sistema hospedeiro.

1. Baixe o instalador oficial do [ViGEmBus Driver](https://github.com/nefarius/ViGEmBus/releases).
2. Execute a instalação do pacote. O barramento virtual requer privilégios de administrador para ser configurado no Kernel.
3. Para compilar a fonte, o ambiente deve possuir o SDK do .NET 10 (Windows Forms App).

## Como Utilizar

1. **Conexão:** Inicie o software e alterne o estado para **"Conectar Controle Virtual"**. O sistema notificará a criação do periférico virtual.
2. **Ciclos:** Marque **Limitar Ciclos** e defina a quantidade de execuções completas da rotina. Com a opção desmarcada, a rotina é executada em loop infinito.
3. **Física (Opcional):** Se as rotinas englobarem uso de eixos contínuos, ative o sistema "Tremor (Eixos)" e configure a frequência de pulso em milissegundos.
4. **Programação da Linha do Tempo:** Utilize a tabela visual para adicionar passos lógicos:
   * `Pressionar e Soltar (Tap)`: Completa o ciclo de Rampa Ascendente, Platô e Rampa Descendente dentro da duração definida.
   * `Manter Pressionado (Hold)`: Trava o estado do botão no valor alvo. Avança de linha imediatamente após concluir a rampa de subida.
   * `Soltar (Release)`: Conclui a rampa de descida para a posição neutra e avança.
   * `Pausa (Wait)`: Paralisa o motor no estado atual por um tempo aleatório determinado pelas colunas Min e Max.
   * `Mensagem de Log`: Registra um marcador textual no log e avança imediatamente, sem alterar o controle ou adicionar duração à etapa.
   * A tabela entra em edição com um clique e aplica alterações dos dropdowns imediatamente. Em `Wait` e `Mensagem de Log`, o controle é definido como vazio e bloqueado; em `Tap`, `Hold` e `Release`, a opção vazia não é oferecida.
   * `Adicionar` inclui uma etapa no final; `Inserir` cria uma etapa acima da linha selecionada. Depois de remover uma etapa, a seleção permanece na linha seguinte ou, ao excluir a última, retorna para a anterior.
   * `Inserir Log` cria um marcador acima da linha selecionada e posiciona o cursor na coluna `Mensagem de Log` para edição imediata.
   * A coluna `Tempo acumulado` mostra o intervalo mínimo e máximo até cada etapa. Os labels superiores exibem a duração por ciclo e, quando o limite está ativo, o tempo total estimado; sem limite, o total é indicado como `execução contínua`.
   * Durante a execução, o label de estado mostra ciclo e linha atuais. A etapa ativa é destacada na tabela e mantida visível automaticamente; ao finalizar, interromper ou falhar, o destaque é removido e a seleção anterior é restaurada.
5. **Execução:** Dispare a automação via botões nativos ou mediante os ganchos locais de atalho do teclado (`Ctrl+Shift+F9` para iniciar e `Ctrl+Shift+F10` para interromper de forma abrupta).

## Licença e Ética de Uso

Este projeto encontra-se licenciado sob a **GNU GPLv3**. Permite-se a utilização, modificação e distribuição integral do software de forma aberta. Obras derivadas que incorporem este código devem, obrigatoriamente, compartilhar seu código-fonte modificado sob os mesmos termos da presente licença.

*Disclaimer:* O AutoGamepad é uma ferramenta direcionada para testes QA (Quality Assurance), simulação de acessibilidade e estudo arquitetural de *Anti-AFK* local. A utilização do software em infraestruturas competitivas online pode ensejar sanções e quebra dos Termos de Serviço (TOS) das respectivas plataformas. O autor desobriga-se de qualquer responsabilidade gerada pela conduta ou emprego não-educacional deste motor de emulação.

## Desenvolvimento e testes

O repositório contém testes automatizados para o motor, cancelamento, limites numéricos e mapeamento de eixos físicos:

```powershell
dotnet test AutoGamepad.slnx
```

As decisões e o roteiro de validação manual da estabilização do motor estão em [`docs/engine-stabilization.md`](docs/engine-stabilization.md).
