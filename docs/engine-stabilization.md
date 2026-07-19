# Estabilização do motor de automação

Este documento registra as mudanças da branch `codex/stabilize-automation-engine`. A candidata `1.2.0-rc.1` foi aprovada na validação em campo e promovida para `1.2.0`. O objetivo é tornar interrupção, execução contínua e controle de eixos previsíveis sem alterar o formato dos perfis JSON existentes.

## Status da validação

A execução com o controle virtual real foi validada em ambiente de campo antes da promoção para `1.2.0`, incluindo os fluxos corrigidos nesta estabilização. Os testes automatizados permanecem como proteção contra regressões.

## Escopo implementado

### 1. Neutralização após Stop ou falha

O próprio motor coloca o controle em ponto morto dentro do bloco `finally`. A UI repete a neutralização como uma segunda camada de segurança. Isso acontece em término normal, cancelamento e exceção. A conexão virtual permanece ativa e pronta para outra execução.

O adaptador do Xbox também serializa acesso, reset e desconexão por meio de uma trava interna, evitando que o dispositivo seja alterado simultaneamente pelo motor e pela rotina de encerramento.

### 2. Sequências vazias e ciclos instantâneos

Uma sequência sem etapas é rejeitada pelo validador e também pelo motor, como defesa em profundidade.

Sequências válidas compostas apenas por ações instantâneas respeitam a duração mínima de um frame de 16 ms. Isso limita esses ciclos a aproximadamente 60 Hz, impede um loop apertado de monopolizar a CPU e garante que o token de cancelamento seja observado.

### 3. Bloqueio da edição durante a execução

Enquanto o motor está ativo, ficam indisponíveis:

- a tabela e suas ações de adicionar, remover e mover linhas;
- o editor JSON e suas ações de colar, validar e copiar;
- salvar e carregar perfis;
- conexão, limite de ciclos, jitter e som.

O botão Parar permanece disponível. Mesmo que um novo controle de UI deixe de ser bloqueado futuramente, o motor executa um snapshot criado antes do início, nunca a `DataGridView` viva.

### 4. Estado por eixo físico

O estado analógico deixou de ser armazenado pelo texto da direção. Agora existem seis canais físicos:

- gatilhos esquerdo e direito;
- eixos X e Y do analógico esquerdo;
- eixos X e Y do analógico direito.

Direita e cima usam magnitude positiva; esquerda e baixo usam magnitude negativa. Direções opostas do mesmo analógico compartilham o mesmo canal e não podem permanecer ativas simultaneamente. O validador exige que a direção mantida seja solta antes de um `Hold` ou `Tap` conflitante.

### 5. Limpeza do log visual

Ao iniciar uma execução, tanto o `RichTextBox` quanto a fila `_logUIBuffer` são limpos. Assim, linhas de uma execução anterior não reaparecem depois do primeiro registro novo.

A gravação em disco continua usando lotes. Erros de acesso ao diretório de logs não interrompem mais o motor.

### 6. Limites numéricos e sorteio inclusivo

Valores mínimo e máximo iguais continuam válidos e representam tempo fixo.

O sorteio inclusivo passou a usar `Random.NextInt64(min, (long)max + 1)`. A promoção para `long` evita overflow quando o máximo é `int.MaxValue`. Os contadores das rampas usam tempo real do `Stopwatch`, evitando overflow por soma repetida de frames.

Texto numérico que não cabe em `Int32` é rejeitado em vez de ser convertido silenciosamente para zero.

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

## Compatibilidade

- Perfis existentes continuam sendo importados; a ausência de `Message` é aceita normalmente.
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
- compatibilidade JSON quando a propriedade opcional `Message` está ausente.

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
