# Evolução do motor e do editor de sequências

Este documento registra a estabilização iniciada para a `v1.2.0`, as evoluções do editor incorporadas à `v1.3.0` e a rodada de confiabilidade preparada para a `v1.4.0`. O objetivo é manter interrupção, execução contínua, controle de eixos, arquivos e edição de sequências previsíveis, preservando a compatibilidade com perfis JSON válidos existentes.

## Status da validação

As mudanças da `v1.3.0` foram validadas em campo com o controle virtual real. A rodada `v1.4.0` possui cobertura automatizada e deve cumprir o roteiro manual atualizado ao final deste documento antes da publicação.

## Escopo implementado

### 1. Neutralização após Stop ou falha

O próprio motor coloca o controle em ponto morto dentro do bloco `finally`. A UI repete a neutralização como uma segunda camada de segurança. Isso acontece em término normal, cancelamento e exceção. A conexão virtual permanece ativa e pronta para outra execução.

O adaptador do Xbox também serializa acesso, reset e desconexão por meio de uma trava interna, evitando que o dispositivo seja alterado simultaneamente pelo motor e pela rotina de encerramento.

### 2. Sequências vazias e ciclos instantâneos

Uma sequência sem etapas é rejeitada pelo validador e também pelo motor, como defesa em profundidade.

Sequências válidas compostas apenas por ações instantâneas respeitam a duração mínima de um frame de 16 ms. Isso limita esses ciclos a aproximadamente 60 Hz, impede um loop apertado de monopolizar a CPU e garante que o token de cancelamento seja observado.

### 3. Bloqueio da edição durante a execução

Enquanto o motor está ativo, ficam indisponíveis:

- a edição da tabela e suas ações de adicionar, inserir, remover e mover linhas;
- o editor JSON e suas ações de colar, validar e copiar;
- salvar e carregar perfis;
- conexão, limite de ciclos, jitter e som.

O botão Parar permanece disponível. A tabela continua navegável em modo somente leitura para mostrar a linha em execução e permitir a rolagem. Mesmo que um novo controle de UI deixe de ser bloqueado futuramente, o motor executa um snapshot criado antes do início, nunca a `DataGridView` viva.

### 4. Estado por eixo físico

O estado analógico deixou de ser armazenado pelo texto da direção. Agora existem seis canais físicos:

- gatilhos esquerdo e direito;
- eixos X e Y do analógico esquerdo;
- eixos X e Y do analógico direito.

Direita e cima usam magnitude positiva; esquerda e baixo usam magnitude negativa. Direções opostas do mesmo analógico compartilham o mesmo canal e não podem permanecer ativas simultaneamente. O validador exige que a direção mantida seja solta antes de um `Hold` ou `Tap` conflitante.

### 5. Limpeza do log visual

Ao iniciar uma execução, tanto o `RichTextBox` quanto a fila `_logUIBuffer` são limpos. Assim, linhas de uma execução anterior não reaparecem depois do primeiro registro novo.

A gravação em disco usa lotes de 50 linhas, fila máxima de 500 e retry de dois segundos. Se a fila atingir o limite durante uma falha permanente, as linhas mais antigas são descartadas com contador e aviso visual. Os arquivos ficam em `%LocalAppData%\AutoGamepad\Logs`.

### 6. Limites numéricos e sorteio inclusivo

Valores mínimo e máximo iguais continuam válidos e representam tempo fixo.

O sorteio inclusivo passou a usar `Random.NextInt64(min, (long)max + 1)`. A promoção para `long` evita overflow quando o máximo é `int.MaxValue`. Os contadores das rampas usam tempo real do `Stopwatch`, evitando overflow por soma repetida de frames.

Texto numérico que não cabe em `Int32` é rejeitado em vez de ser convertido silenciosamente para zero.

Na `v1.4.0`, qualquer campo numérico ativo e vazio também é rejeitado. Células realmente desabilitadas continuam sendo representadas internamente por zero, mas um campo obrigatório nunca recebe fallback de 0% ou 100%.

### 7. Separação entre UI, motor e ViGEm

O fluxo atual é:

1. `Form1` valida a tabela e cria um `AutomationProgram` imutável.
2. `AutomationEngine` executa apenas os dados do programa em uma tarefa de trabalho.
3. O motor escreve em `IGamepadOutput`, sem conhecer WinForms ou ViGEm.
4. `Xbox360GamepadOutput` traduz controles lógicos para o controle Xbox 360 virtual.
5. O log retorna à UI por um callback com marshaling explícito.

Arquivos principais:

- `AutomationModels.cs`: ações, controles, canais físicos e snapshot;
- `AutomationEngine.cs`: ciclos, tempos, rampas, jitter e cancelamento;
- `Xbox360GamepadOutput.cs`: conexão e tradução para ViGEm;
- `Form1.cs`: interação, validação visual e perfis.

### 8. Interação contextual da tabela

A tabela entra em edição no primeiro clique. Células de dropdown abrem a lista imediatamente e confirmam a seleção sem exigir que o usuário clique fora da linha, permitindo que as demais colunas sejam reconfiguradas no mesmo instante.

Cada linha possui sua própria lista de controles. `Wait` e `Mensagem de Log` selecionam automaticamente `[Vazio / Apenas Pausa]`, bloqueiam a célula e ocultam o botão do dropdown. `Tap`, `Hold` e `Release` removem a opção vazia; ao sair de uma ação sem controle, a linha recebe `Botão A` como controle padrão.

### 9. Marcadores de log

A ação `Mensagem de Log` registra um marcador textual por meio do logger existente. Ela não acessa o controle virtual, ignora os campos de rampa e duração e avança imediatamente para a próxima linha. O botão `Inserir Log` cria o marcador acima da seleção atual e inicia a edição da mensagem.

O identificador JSON da ação é `Log`. A propriedade opcional `Message` é gravada somente para marcadores; etapas de controle continuam sendo exportadas no formato anterior.

### 10. Estimativas de tempo

O cálculo de tempo fica em `SequenceTimeEstimator.cs` e não depende de WinForms. A interface fornece um snapshot de `AutomationProgram`, recebe os intervalos calculados e atualiza a coluna `Tempo acumulado` e os labels de ciclo e total.

As durações planejadas são calculadas assim:

- `Wait`: duração mínima e máxima;
- `Tap` digital: duração mínima e máxima;
- `Hold` e `Release` digitais: zero;
- `Tap` em eixo: duas rampas mais a duração;
- `Hold` e `Release` em eixo: uma rampa;
- `Mensagem de Log`: zero.

O tempo por ciclo respeita o piso de 16 ms usado pelo motor para sequências totalmente instantâneas. Com limite de ciclos, o intervalo do ciclo é multiplicado pela quantidade configurada; sem limite, a interface mostra `execução contínua`. As estimativas representam o tempo programado e podem ser excedidas pelo agendamento do Windows e pela comunicação com o controle virtual.

A coluna calculada e os labels não fazem parte do JSON. Eles são reconstruídos ao editar, reordenar, inserir, excluir ou importar etapas.

### 11. Progresso de execução

O motor publica um `AutomationProgress` sempre que entra em uma etapa. A notificação contém o ciclo atual, o total de ciclos quando limitado, o índice e a quantidade de linhas, além da ação correspondente. O callback é independente de WinForms e não altera o snapshot da automação.

A interface consolida notificações que chegam mais rápido do que a thread gráfica consegue desenhar. Dessa forma, sequências instantâneas exibem sempre a posição mais recente sem acumular uma fila de atualizações. Cada execução recebe um identificador próprio, e notificações atrasadas de uma rodada encerrada são ignoradas.

O label superior apresenta estados como:

```text
Executando — Ciclo 3 de 10 — Linha 5 de 8
Finalizado — 10 ciclos concluídos
Interrompido — Ciclo 3, linha 5
```

Em execução contínua, o total de ciclos é omitido. A linha atual recebe destaque de seleção, é mantida na área visível e não pode ser substituída por outra seleção enquanto o motor está ativo. Ao concluir, interromper ou falhar, o destaque é removido e a seleção anterior ao início é restaurada quando a linha ainda existe.

### 12. Jitter durante sustentação e liberação

O jitter de uma etapa analógica `Tap` vale para subida, platô e descida. Uma etapa `Release` de eixo possui seu próprio campo de jitter.

Ao concluir a rampa de um `Hold`, o motor registra a modulação ativa do canal. Esperas e durações posteriores continuam atualizando esse canal até o `Release`. O scheduler faz essas atualizações junto das esperas existentes, sem criar uma thread concorrente por eixo. No fim de uma ação ou no cancelamento, o valor exato de destino ou neutro é reaplicado.

### 13. Perfis, Save e estado alterado

O aplicativo mantém o caminho do perfil aberto:

- `Salvar` atualiza esse caminho;
- `Salvar como` escolhe um novo destino;
- o título mostra `*` enquanto houver alterações;
- carregar outro perfil ou fechar oferece salvar, descartar ou cancelar;
- a gravação usa arquivo temporário no mesmo diretório e substituição segura;
- uma falha de escrita preserva a versão anterior.

O texto JSON editado é tratado como rascunho até ser aplicado. Ao sair da aba Código, o usuário decide aplicar, descartar ou continuar editando. Salvar ou iniciar também exige que esse rascunho seja válido e aplicado, impedindo que a tabela sobrescreva código silenciosamente.

### 14. Registro real das hotkeys

`Ctrl+Shift+F9` e `Ctrl+Shift+F10` são registrados como um conjunto. O retorno do Windows é verificado para cada combinação. Se uma falhar, qualquer registro parcial é desfeito, o log informa a falha e uma mensagem explica que os botões da janela continuam disponíveis.

### 15. Encerramento seguro

O primeiro pedido de fechamento é cancelado temporariamente. Depois de resolver alterações não salvas, o aplicativo:

1. remove as hotkeys;
2. solicita cancelamento;
3. aguarda a tarefa do motor por até cinco segundos;
4. neutraliza o controle;
5. desconecta o dispositivo;
6. força uma última tentativa de log;
7. conclui o fechamento.

Se o motor ultrapassar o timeout, o log registra o ocorrido e a neutralização de emergência é aplicada.

## Compatibilidade

- Perfis existentes continuam sendo importados; a ausência de `Message` é aceita normalmente.
- Perfis válidos exportados por versões anteriores mantêm os mesmos campos numéricos e continuam sendo importados.
- Um campo numérico obrigatório ausente deixa de receber fallback silencioso e passa a produzir erro de validação.
- A propriedade `Message` é omitida nas etapas que não são marcadores, preservando o formato existente desses passos.
- Perfis que usam a ação `Log` exigem esta versão ou uma posterior. Versões anteriores rejeitam o identificador de ação desconhecido.
- Perfis legados com `Wait` associado a um controle são normalizados para a opção vazia.
- Perfis legados com `Tap`, `Hold` ou `Release` associados à opção vazia são normalizados para `Botão A`.

## Testes automatizados

Execute:

```powershell
dotnet test AutoGamepad.slnx
```

Os testes cobrem:

- rejeição de sequência vazia;
- neutralização de botão mantido após cancelamento;
- compartilhamento de canal por direções opostas;
- aplicação do sinal correto para esquerda/baixo;
- sorteio com `int.MaxValue` sem overflow;
- cancelamento de sequência composta apenas por comandos instantâneos;
- normalização da opção vazia conforme a ação selecionada na tabela;
- execução instantânea de marcadores sem saída para o controle virtual;
- compatibilidade JSON quando a propriedade opcional `Message` está ausente;
- cálculo de todas as combinações entre ações digitais, ações de eixo, rampas e durações;
- acumulado por etapa, piso mínimo do ciclo, limite de ciclos e execução contínua;
- aritmética segura para intervalos configurados com `int.MaxValue`.
- notificação de ciclo, linha, quantidade de etapas e ação atual pelo motor;
- formatação dos estados em execução limitada, execução contínua, conclusão, interrupção e falha.
- rejeição de campos obrigatórios vazios e de força analógica fora de 1% a 100%;
- jitter durante `Hold` seguido de `Wait`, descida do `Tap` e rampa de `Release`;
- limite, backoff, recuperação e descarte controlado da fila de log;
- estado alterado, supressão de mudanças programáticas e substituição segura de arquivos;
- ativação atômica e rollback das hotkeys;
- espera de conclusão e timeout durante o encerramento.

## Roteiro de validação manual

Com o ViGEmBus instalado:

1. Crie `Hold A`, uma pausa longa e `Release A`. Inicie e pressione Parar durante a pausa; confirme que o botão A é liberado.
2. Tente iniciar sem linhas; confirme que a execução é recusada e a UI continua responsiva.
3. Execute uma sequência e confirme que tabela, botões laterais, JSON e perfis ficam bloqueados até o fim.
4. Crie `Hold Analógico Esq - Direita` seguido de `Tap Analógico Esq - Esquerda`; confirme que o validador marca o conflito.
5. Crie movimentos sequenciais para direita e esquerda, cada um devidamente liberado; confirme a passagem suave pelo centro.
6. Inicie duas execuções em sequência; confirme que o log visual da primeira não reaparece na segunda.
7. Execute um perfil composto por `Hold A` e `Release A` em loop infinito; confirme que o botão Parar continua responsivo.
8. Clique uma vez em uma célula editável e confirme que ela entra imediatamente em edição; nos dropdowns, confirme que a lista é aberta no primeiro clique.
9. Troque `Tap` por `Hold` e confirme que as colunas de duração são bloqueadas assim que a opção é escolhida, sem clicar fora da célula.
10. Selecione `Wait` e confirme que o controle muda para a opção vazia e fica bloqueado; retorne para uma ação executável e confirme que `Botão A` é selecionado por padrão.
11. Selecione uma linha intermediária e clique em `Inserir Log`; confirme que o marcador aparece acima dela e que a coluna de mensagem entra em edição.
12. Execute uma sequência com marcadores entre comandos e confirme que cada mensagem aparece no log sem introduzir atraso perceptível ou alterar o estado do controle.
13. Salve e carregue um perfil com marcadores; confirme que as mensagens e suas posições são preservadas. Depois carregue um perfil antigo e confirme que ele continua válido.
14. Crie uma sequência com `Wait`, `Tap` digital, `Tap` de eixo, `Hold`, `Release` e marcador; confirme que a coluna acumulada cresce conforme as durações aplicáveis.
15. Altere rampas, durações, ações, controles e ordem das linhas; confirme que a coluna e os labels são atualizados após cada alteração.
16. Ative o limite de ciclos e altere a quantidade; confirme que o total é multiplicado. Desative o limite e confirme a mensagem `execução contínua`.
17. Salve e carregue o perfil; confirme que as estimativas são reconstruídas e que nenhum campo calculado é adicionado ao JSON.
18. Execute uma sequência com pausas longas e confirme que o label mostra ciclo e linha atuais, que a linha ativa fica destacada e que a tabela rola automaticamente quando necessário.
19. Durante a execução, tente selecionar outra linha e editar a tabela; confirme que o destaque retorna para a etapa ativa e que nenhuma célula pode ser alterada.
20. Valide término normal, botão Parar e uma falha de execução; confirme que o destaque é removido, a seleção anterior é restaurada e o label mostra o estado final correspondente.
21. Em um `Tap` de eixo, apague o campo de valor; confirme que a célula não aceita vazio. Informe `0` e confirme que somente 1% a 100% são aceitos.
22. Apague um tempo ou rampa ativos e confirme o erro. Digite `0` explicitamente e confirme que uma etapa instantânea continua válida.
23. Ative jitter, crie `Hold LT` com valor 50 e jitter 10, adicione `Wait` longo e `Release LT`; observe nas propriedades do controle que o gatilho continua variando durante a espera e termina em zero.
24. Configure rampa e jitter em um `Tap` analógico; confirme variação durante subida e descida. Configure outro jitter na linha `Release` e confirme que ele vale para essa rampa.
25. Com o jitter global desmarcado, reinicie o aplicativo e confirme que a frequência aparece desabilitada.
26. Edite uma sequência e confirme o `*` no título. Salve, edite novamente e confirme que `Salvar` reutiliza o mesmo arquivo; use `Salvar como` e confirme a mudança do nome no título.
27. Edite o JSON e tente voltar à tabela. Valide as opções Aplicar, Descartar e Cancelar e confirme que nenhuma delas sobrescreve texto sem decisão.
28. Faça uma alteração e tente carregar outro perfil e fechar o aplicativo. Valide Salvar, Não e Cancelar em ambos os fluxos.
29. Abra duas instâncias. Confirme que a segunda informa o conflito das hotkeys e que seus botões continuam funcionando; feche a primeira e reinicie a segunda para confirmar o registro normal.
30. Inicie uma espera longa e feche a janela. Confirme que o aplicativo cancela, aguarda e que todos os botões/eixos ficam neutros antes do dispositivo virtual desaparecer.
31. Para testar falha de log, negue temporariamente escrita em `%LocalAppData%\AutoGamepad\Logs`, produza mais de 500 mensagens e confirme que a memória permanece limitada e aparece aviso de descarte. Restaure a permissão, aguarde dois segundos, produza outra mensagem e confirme o aviso de recuperação.
