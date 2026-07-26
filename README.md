# AutoGamepad

O **AutoGamepad** é um editor e motor de automação de controle virtual para Windows. Por meio do ViGEmBus, ele cria um controle Xbox 360 virtual e executa sequências de botões, gatilhos e analógicos.

O projeto é voltado a garantia de qualidade (QA), testes de acessibilidade, estudo de esquemas de controle e automações locais autorizadas. Ele não promete evitar detecção por anticheats nem deve ser usado para contornar regras ou mecanismos de segurança.

## Arquitetura e Funcionalidades (v1.4.0)

O AutoGamepad opera como um interpretador de sequência baseada em Máquina de Estados. Antes da execução, a interface converte a tabela em um snapshot imutável; o motor processa esse snapshot em uma thread de trabalho e não acessa controles WinForms durante os ciclos.

* **Engine de Interpolação a 60 Hz:** Em vez de injeções estáticas de estado, o motor calcula transições de valores (*Rampas*) baseadas em tempo (Linear Interpolation). Isso permite a simulação orgânica da progressão de força em molas de gatilhos ou deslocamento de direcionais.
* **Jitter de Eixo:** Uma variação parametrizável pode ser sobreposta ao valor analógico em frequência configurável. Em `Hold`, a variação continua durante as esperas até o `Release`; as rampas de subida e descida também a respeitam.
* **Variação Temporal:** Atrasos, durações e rampas podem receber intervalos Min/Max. O motor sorteia um valor dentro do intervalo a cada ciclo.
* **Validador Semântico de Estado:** Prevenção de falhas. A interface analisa a cadeia de eventos de forma lógica antes da injeção no Kernel, bloqueando comandos paradoxais (ex: tentar executar um *Release* em um botão não submetido a *Hold* prévio).
* **Gestão e Persistência de Perfis (JSON):** `Salvar`, `Salvar como`, carregamento, indicação de alterações no título e confirmação antes de descartar trabalho. O editor JSON exige uma decisão explícita antes de aplicar ou descartar alterações.
* **Validação Numérica Estrita:** Campos ativos vazios são erros. Força analógica exige um valor explícito de 1% a 100%; tempos instantâneos continuam sendo representados por `0`.
* **Logs Limitados:** O painel e a fila de disco têm limites de memória. Falhas de escrita usam retry controlado e não interrompem o motor.
* **Encerramento Seguro:** Ao fechar, o aplicativo cancela e aguarda o motor antes de neutralizar e desconectar o controle.

## Pré-Requisitos e Setup

O funcionamento do software está vinculado à presença do driver de simulação instalado no sistema hospedeiro.

1. Baixe o instalador oficial do [ViGEmBus Driver](https://github.com/nefarius/ViGEmBus/releases).
2. Execute a instalação do pacote. O barramento virtual requer privilégios de administrador para ser configurado no Kernel.
3. Para compilar a fonte, o ambiente deve possuir o SDK do .NET 10 (Windows Forms App).

## Como Utilizar

1. **Conexão:** Inicie o software e alterne o estado para **"Conectar Controle Virtual"**. O sistema notificará a criação do periférico virtual.
2. **Ciclos:** Marque **Limitar Ciclos** e defina a quantidade de execuções completas da rotina. Com a opção desmarcada, a rotina é executada em loop infinito.
3. **Variação de Eixo (Opcional):** Ative o sistema "Tremor (Eixos)", configure a frequência global e informe a intensidade nas etapas analógicas desejadas.
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
5. **Perfis:** Use **Salvar** para atualizar o arquivo aberto e **Salvar como** para escolher outro destino. Um `*` no título indica alterações ainda não gravadas.
6. **Execução:** Inicie pelos botões da janela ou pelos atalhos globais (`Ctrl+Shift+F9` para iniciar e `Ctrl+Shift+F10` para parar). Se outra aplicação já tiver registrado uma dessas combinações, o AutoGamepad avisa e mantém ambos os atalhos globais desativados.

Os logs de sessão são armazenados em `%LocalAppData%\AutoGamepad\Logs`.

## Licença e Ética de Uso

Este projeto encontra-se licenciado sob a **GNU GPLv3**. Permite-se a utilização, modificação e distribuição integral do software de forma aberta. Obras derivadas que incorporem este código devem, obrigatoriamente, compartilhar seu código-fonte modificado sob os mesmos termos da presente licença.

*Disclaimer:* O AutoGamepad é uma ferramenta direcionada a QA, acessibilidade e automação autorizada. Antes de utilizá-lo com qualquer aplicativo ou serviço, verifique os respectivos Termos de Serviço. Automação não permitida pode resultar em sanções. O software não oferece garantia de compatibilidade ou indetectabilidade.

## Desenvolvimento e testes

O repositório contém testes automatizados para o motor, cancelamento, limites numéricos, jitter, logs, arquivos, hotkeys e mapeamento de eixos físicos:

```powershell
dotnet test AutoGamepad.slnx
```

As decisões e o roteiro de validação manual da evolução do motor e do editor estão em [`docs/engine-stabilization.md`](docs/engine-stabilization.md).
