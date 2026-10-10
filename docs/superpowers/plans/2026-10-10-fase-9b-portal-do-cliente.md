# Fase 9B — Portal do cliente no design system: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** As telas do portal do cliente (Andamento, Meus documentos, Documentos comuns do cliente e da Diretoria) ficam idênticas aos quadros do canvas nas três visões, e a migração ganha as peças de lista que as próximas fases reutilizam: paginação, estado da lista, aviso com ações, menu de ações e ação principal no cabeçalho.

**Architecture:** Cada padrão que os quadros do portal trazem como estilo embutido vira classe `aj-` no artifact do design system e é copiado para `wwwroot/css`. Cada tela emite a tabela do desktop num invólucro `aj-so-desktop` e os cartões ou linhas do tablet e do mobile num invólucro `aj-tablet-mobile`; o CSS mostra um dos dois. A lógica de gestão de documentos sai do arquivo Razor para uma classe base, e duas telas a herdam: a antiga (ficha do cliente, que só migra na 9D) e a nova (Documentos comuns da Diretoria).

**Tech Stack:** .NET 10, Blazor Interactive Server, CSS do design system (tokens e classes `aj-`), xUnit 2.9, FluentAssertions 7.2, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-10-10-migracao-para-o-design-system-design.md` (seções 3, 4, 7.2, 8, 9 "Menu de ações", 10, 11 e 15).

## Decisões deste plano

- **O que a seção 15 da spec deixou para a abertura da 9B vem antes das telas** (Tarefa 2): trava mais forte, testes de tela cada um no seu arquivo, auxiliar que recorta o bloco de uma visão, teste da adjacência das barras superiores e os dois estados do início sem cobertura.
- **Cinco peças compartilhadas nascem aqui** (spec 4.4): `Paginacao`, `EstadoDaLista`, `MenuDeAcoes`, o `Aviso` com ações e a ação principal do `CabecalhoDePagina` (botão no cabeçalho e botão flutuante no mobile).
- **Menu de ações sem JavaScript próprio.** O botão "⋯" alterna um painel que já está no HTML, escondido pelo atributo `hidden`. Fecha ao escolher (o clique sobe até a lista), ao tocar fora (um botão transparente que cobre a tela por baixo da lista) e com Esc (que devolve o foco ao botão). É o padrão de divulgação, não o de `role="menu"`: Tab percorre os itens, e não há setas a implementar. Como a lista está sempre no HTML, o teste de página enxerga as ações do tablet e do mobile.
- **A paginação segue cada quadro.** O Andamento do desktop mostra a linha mesmo com uma página só ("Página 1 de 1 · 8 atividades"); as listas de documentos e os cartões do tablet e do mobile só a mostram quando há mais de uma página, porque os quadros não a desenham com uma.
- **O componente de lista de documentos do cliente passa a desenhar o cabeçalho da página**, porque o subtítulo é a contagem que só ele conhece ("4 documentos").
- **Envio de documento comum em três tempos, como o quadro.** "Enviar documento" (cabeçalho ou botão flutuante) abre um painel com o arquivo e o nome; "Enviar" esconde o painel e mostra, no topo, o aviso de confirmação com o nome e o tamanho; "Confirmar envio" envia. O painel fica **escondido, não removido**, enquanto o aviso está aberto: o arquivo escolhido mora no campo do navegador, e removê-lo do HTML perderia o arquivo.
- **Exclusão e renomeação por analogia** (spec 8): excluir pede confirmação num aviso de atenção no topo, com "Cancelar" antes do verbo; renomear abre um painel na própria tela.
- **A lógica de documentos vira classe base.** `GestaoDeDocumentos` (C#) guarda estado e ações; `DocumentosDoCliente.razor` (marcação antiga, usada pela ficha do cliente) e `DocumentosComunsDaDiretoria.razor` (marcação nova) herdam dela. Antes da extração, um teste de caracterização fixa o que a tela antiga desenha.
- **"Concluída em" mostra só o dia** (spec 7.2): `FusoDoEscritorio` ganha `FormatarData`.
- **`TotaisDoAndamento.razor` sai**: os totais do Andamento passam ao subtítulo e aos chips; só essa tela o usava. As regras `.totais` do `app.css` saem junto.
- **Classes novas no design system, sem token novo.** Todas as cores, raios e sombras que os quadros da fase usam já são tokens.

## Como ler este plano

Cada tarefa traz as **regras** (o contrato), as **interfaces** que ela consome e produz e os **testes obrigatórios**. O plano traz o código onde ele é a própria fidelidade (o CSS, a marcação das telas) ou onde o detalhe é sutil. O restante é escrito no estilo do que já existe. O código do plano não foi executado: quando o Blazor ou o navegador exigirem, ajuste o mínimo para preservar o comportamento e registre o desvio no relatório, com a mensagem de erro que o motivou.

Nunca apague um teste nem afrouxe uma asserção. Quando uma asserção mudar porque a marcação mudou, reescreva-a mantendo o que ela prova e liste-a no relatório. Asserção negativa (`NotContain`) que deixaria de provar algo com a marcação nova ganha uma afirmação positiva ao lado. Asserção sobre o bloco de uma visão (`BlocoDeVisao.De`) sempre tem uma afirmação positiva sobre o mesmo bloco: um bloco vazio aceitaria qualquer `NotContain`.

A fonte de cada tela é o quadro do canvas. Antes de escrever a marcação de uma tela, releia os três arquivos dela na cópia local do canvas (caminho em "Estado de partida"). Se um quadro tiver mudado em relação ao que este plano descreve, pare e relate.

## Estado de partida

- Worktree `.claude/worktrees/feat+fase-9b-portal-do-cliente`, branch `worktree-feat+fase-9b-portal-do-cliente`, na `main` local (`8eaba14`), que já tem a fase 9A.
- Canvas: https://claude.ai/artifact/HzHWv1pgVXAnYHuPEYQDFj, versão `1791598628-21e1` (a mesma da spec; nenhum quadro mudou). Cópia local: `<scratchpad>/artifact-files/8991caa9-7339-47d7-a01c-84892497662e/project/`. Quadros desta fase: `Andamento`, `AndamentoTablet`, `AndamentoMobile`, `MeusDocumentos*`, `DocumentosComuns*`, `DocumentosComunsDiretoria*` (`.dc.html`).
- Design system: https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS, versão `1791646790-3a09` (a que a 9A publicou). Cópia de trabalho: `<scratchpad>/ds/project/`.
- Web, depois da 9A: `MainLayout` com o encaixe `barra-superior`; `CabecalhoDePagina`, `Aviso`, `RotuloDeCampo`, `CartaoAtalho`, `Icone` em `Components/Layout`; as telas desta fase ainda com marcação antiga vestida pelo `app.css`.
- Testes: `AplicacaoDeTeste` sobe a aplicação inteira contra um banco novo por teste; `TelaDeTeste.RenderizarAsync` desenha um componente fora da página com os casos de uso de verdade; `TelaDeTeste.RenderizarSemDadosAsync` desenha um componente que não consulta nada.
- Linha de base: suíte inteira rodada neste worktree antes da Tarefa 1; os totais vão para o ledger.
- `git` só pela ferramenta PowerShell. Antes de cada commit, `git branch --show-current`.
- A suíte de integração leva de 20 a 30 minutos e usa o Docker da máquina, que é compartilhado: enquanto ela roda, nada de `docker` nem de publicar a aplicação. Rode sempre com `--logger "trx;LogFilePrefix=<nome>" --results-directory <scratchpad>\trx-9b`.

## Global Constraints

- TFM único `net10.0`. `Nullable` e `TreatWarningsAsErrors=true`. Nenhum pacote novo.
- Código, nomes e textos em português, com acentuação correta. Arquivos em UTF-8 sem BOM.
- A camada Web só chega aos dados pelo `IDespachante` (`WebSoPeloDespachanteTests`).
- `wwwroot/css/tokens.css` e `wwwroot/css/componentes.css` não são editados à mão: mudam no artifact do design system e são copiados de lá.
- Tela migrada não tem atributo `style`, nem `<style>`, nem arquivo `.razor.css`, nem classe que não comece por `aj-`, nem `<button>`, `<table>`, `<input>`, `<select>` ou `<textarea>` sem `class`. Nenhum CSS novo no `app.css`.
- As classes de visibilidade (`aj-so-desktop`, `aj-so-tablet`, `aj-so-mobile`, `aj-desktop-tablet`, `aj-tablet-mobile`) vão num invólucro próprio (`div` ou `span`) cuja classe é **só** a de visibilidade, nunca no elemento do componente.
- Visões: mobile até 760px, tablet de 761px a 1199px, desktop a partir de 1200px.
- Todo texto que um teste procura no HTML é escrito por **uma única** expressão Razor ou por marcação fixa.
- Rotas existentes não mudam. `RotasDoCliente` não muda.
- O link de download continua `<a href="/documentos/{id}/baixar" download data-enhance-nav="false">`: o atributo `download` é o que impede o roteador do Blazor de interceptar o clique.
- Botão só de ícone leva `aria-label` com o verbo e o objeto ("Baixar Procuração.pdf", "Ações de Procuração.pdf").
- Enquanto uma tela não for migrada, nenhum componente do design system entra solto no topo do conteúdo dela (a regra interina do `app.css` a trataria como migrada).
- Textos exatos, dos quadros:
  - Andamento: `Andamento`; subtítulo `{n} em andamento · {m} concluídas` (`concluída` quando m é 1); grupo de chips com `aria-label="Filtrar por situação"`; chips `Todas · {n}`, `Em andamento · {n}`, `Concluídas · {n}`; colunas `Atividade`, `Área`, `Processo`, `Situação`, `Concluída em`; etiquetas `Em andamento` e `Concluída`; sem valor na tabela: `—`; no cartão: `Sem processo`; rodapé do cartão `Concluída em {dd/MM/aaaa}`; paginação `Página {n} de {m} · {x} atividades`, `Anterior`, `Próxima`
  - Meus documentos: `Meus documentos`; subtítulo `{n} documentos` (`1 documento`); colunas `Nome`, `Tamanho`, `Enviado em` e `Ações` (esta só para leitor de tela); botão `Baixar`; no tablet e no mobile, linha `{tamanho} · {dd/MM/aaaa HH:mm}` e botão de ícone `Baixar {nome}`
  - Documentos comuns, cliente: `Documentos comuns`; subtítulo `Modelos e orientações do escritório para todos os clientes.` (desktop e tablet) e `{n} documentos` (mobile)
  - Documentos comuns, Diretoria: subtítulo `Visíveis para todos os clientes do escritório.` (desktop e tablet) e `{n} documentos` (mobile); ação `Enviar documento`; aviso `Confirmar envio` com o título `{nome} · {tamanho}`, o texto `Documentos desta área ficam visíveis para todos os clientes do escritório. Não envie aqui arquivos de um cliente específico.` e os botões `Cancelar` e `Confirmar envio`, nesta ordem; coluna `Por`; ações `Renomear` e `Excluir`; no tablet e no mobile, linha `{tamanho} · {data} · {autor}` e botão `Ações de {nome}`
  - Sem quadro, mantidos: `Seu acesso foi encerrado. Entre novamente.`; `Carregando…`; `Nenhuma atividade.`; `Nenhum documento.`; `Tentar de novo`; `Arquivo (até 50 MB)`; `Nome (opcional)`; `Enviar`; `Enviando…`; `Renomear "{nome}"`; `Novo nome`; `Salvar nome`; `Confirmar exclusão`; as mensagens de retorno que a tela já mostra (`Documento enviado.`, `Nome alterado.`, `Documento excluído.` e as de falha)
  - Sem quadro, novos por analogia: `Sem área` (cartão sem área, como `Sem processo`); `Confirmar a exclusão de {nome}?` com o texto `O documento deixa de aparecer para todos os clientes.` (como a confirmação de inativar usuário); item de menu `Baixar`

## Review Focus

Condições que a spec implica e que mais provavelmente atingem quem usa o portal. Cada uma tem o seu teste ou passo de conferência na tarefa indicada.

1. **Arquivo escolhido entre o painel de envio e a confirmação:** o envio confirmado lê o arquivo do campo do navegador; se o painel for removido do HTML, o envio falha. Conferência no navegador na Tarefa 7, enviando um arquivo de verdade e baixando-o depois.
2. **Um cliente não vê o que é da Diretoria nem de outro cliente:** sem coluna "Por", sem nome de quem enviou, sem "Enviar documento", "Renomear", "Excluir" nem "Ações de"; a Produção continua recebendo "Página não encontrada". Testes nas Tarefas 5 e 6.
3. **Lista vazia, falha de consulta e acesso encerrado:** cada um mostra o seu estado no lugar da lista, com os chips e o cabeçalho no lugar; falha não vira "nenhum item". Testes nas Tarefas 3, 4 e 5.
4. **Nome de arquivo ou título muito longo:** quebra dentro da linha no tablet e no mobile e rola dentro da caixa da tabela no desktop; a página não rola para o lado. Conferência no navegador na Tarefa 7.
5. **Menu de ações com teclado e toque:** fechado, os itens não recebem foco; aberto, Tab percorre os itens, Esc fecha e devolve o foco ao botão, tocar fora fecha. Conferência no navegador na Tarefa 7.
6. **A ficha do cliente, que ainda não migrou, continua enviando, renomeando, definindo acesso e excluindo documentos depois da extração da lógica.** Teste de caracterização na Tarefa 6; conferência no navegador na Tarefa 7.

---

### Task 1: Design system — padrões das listas do portal e cópia para o código

Feita pela sessão que coordena: publica no artifact do usuário.

**Files:**
- Artifact do design system, sob `project/`: `components/bundle.css`, `design-system.json` (só `lastChange`); README e prévia novos em `components/Paginacao/`, `components/ListaEmPainel/`, `components/MenuDeAcoes/`, `components/EstadoDaLista/`; README (e prévia, onde houver estado novo) dos componentes que ganham modificador: botão, tabela, aviso, painel e cartão (os nomes exatos das pastas vêm da listagem do artifact)
- Modify: `src/AtividadesJuridicas.Web/wwwroot/css/componentes.css`

**Interfaces:**
- Consumes: os doze quadros desta fase.
- Produces: as classes `aj-botao--compacto`, `aj-botao--menor`; `aj-tabela__acao--varias`; `aj-icone-cheio`; `aj-paginacao`, `aj-paginacao__botoes`; `aj-linha`, `aj-linha__texto`, `aj-linha__titulo`; `aj-painel--cartao`, `aj-cartao__meta--tabular`; `aj-aviso--com-acoes`, `aj-aviso__texto`, `aj-aviso__acoes`; `aj-estado-lista`; `aj-menu-acoes`, `aj-menu-acoes__fundo`, `aj-menu-acoes__lista`, `aj-menu-acoes__item`, `aj-menu-acoes__item--perigo`; e as regras `[hidden]` de `aj-painel` e de `aj-menu-acoes__lista` e a do campo de arquivo.

**Regras:**
- Nenhuma classe existente muda de valor. Medida de quadro que difere de uma classe existente vira modificador (spec, seção 3).
- Só tokens que já existem. Medidas fora da escala de espaços (32px, 36px, 184px) ficam literais, como o `bundle.css` já faz.
- O bloco novo vai ao fim do `bundle.css`, depois das seções da 9A.

- [ ] **Step 1: Conferir as versões e ler o que vai mudar**

Ação `read` do artifact do design system: a versão tem de ser `1791646790-3a09`. Se for outra, alguém editou: leia de novo `components/bundle.css` e refaça a cópia de trabalho antes de seguir. Liste os arquivos (`list` com `scope: "files"`) e leia os README dos componentes de botão, tabela, aviso, painel e cartão.

- [ ] **Step 2: Acrescentar o bloco ao fim de `components/bundle.css`**

```css
/* ---------- Listas do portal (fase 9B) ---------- */
/* O atributo hidden vence o display próprio do componente. */
.aj-painel[hidden],.aj-menu-acoes__lista[hidden]{display:none}

/* Botão: medidas menores, para dentro de tabela e de lista. */
.aj-botao--compacto{min-height:32px;padding:0 var(--espaco-12) 0 var(--espaco-10);font-size:14px}
.aj-botao--menor{padding:0 var(--espaco-10);font-size:14px}

/* Campo de arquivo: o controle nativo dentro da caixa do campo. */
.aj-campo[type="file"]{height:auto;padding:9px var(--espaco-12);cursor:pointer}

/* Tabela: célula de ação com mais de um botão não quebra entre eles. */
.aj-tabela td.aj-tabela__acao--varias{white-space:nowrap}

/* Ícone preenchido: os três pontos de "mais ações". */
.aj-app svg.aj-icone-cheio,svg.aj-icone-cheio{fill:currentColor;stroke:none}

/* Paginação: a linha ao fim da lista. */
.aj-paginacao{display:flex;align-items:center;justify-content:space-between;gap:var(--espaco-12)}
.aj-paginacao .aj-subtitulo{font-size:14px}
.aj-paginacao__botoes{flex:0 0 auto;display:flex;gap:var(--espaco-8)}

/* Lista em painel: uma linha por item, com o texto e uma ação à direita (tablet e mobile). */
.aj-linha{display:flex;align-items:center;gap:var(--espaco-12)}
.aj-linha+.aj-linha{padding-top:var(--espaco-10);border-top:1px solid var(--borda-suave)}
.aj-linha__texto{flex:1 1 auto;min-width:0}
.aj-linha__titulo{display:block;font-weight:600;overflow-wrap:anywhere}
.aj-linha__texto .aj-ajuda{display:block;font-variant-numeric:tabular-nums}

/* Painel usado como cartão de leitura, sem link: blocos mais próximos. */
.aj-painel--cartao{gap:var(--espaco-8)}
.aj-cartao__meta--tabular{font-variant-numeric:tabular-nums}

/* Aviso com ações: confirmação e erro com nova tentativa. */
.aj-aviso--com-acoes{flex-wrap:wrap;align-items:flex-start}
.aj-aviso__texto{flex:1 1 320px;min-width:0}
.aj-aviso__texto strong{display:block;font-weight:700}
.aj-aviso__acoes{display:flex;gap:var(--espaco-8)}
.aj-aviso--com-acoes:focus-visible{outline:2px solid currentColor;outline-offset:2px}

/* Estado da lista: nada a mostrar. */
.aj-estado-lista{display:flex;flex-direction:column;align-items:center;gap:var(--espaco-6);padding:36px var(--espaco-16);border:1px dashed var(--borda-campo);border-radius:var(--raio-16);color:var(--tinta-suave);font:400 15px/1.5 var(--font-sans);text-align:center}
.aj-estado-lista strong{font-weight:600;color:var(--tinta)}

/* Menu de ações: o que o botão de três pontos abre. */
.aj-menu-acoes{position:relative;flex:0 0 auto;display:inline-flex}
.aj-menu-acoes__fundo{position:fixed;inset:0;z-index:30;padding:0;border:0;background:transparent;cursor:default}
.aj-menu-acoes__lista{position:absolute;top:calc(100% + var(--espaco-4));right:0;z-index:31;display:flex;flex-direction:column;min-width:184px;padding:var(--espaco-6);border:1px solid var(--borda);border-radius:var(--raio-14);background:var(--superficie);box-shadow:var(--sombra-flutuante)}
.aj-menu-acoes__lista:focus{outline:0}
.aj-menu-acoes__item{display:flex;align-items:center;gap:var(--espaco-10);width:100%;min-height:var(--alvo-toque);padding:0 var(--espaco-12);border:0;border-radius:var(--raio-10);background:transparent;color:var(--tinta);font:500 15px/1.3 var(--font-sans);text-align:left;text-decoration:none;white-space:nowrap;cursor:pointer}
.aj-menu-acoes__item:hover{background:var(--superficie-recuada)}
.aj-menu-acoes__item:focus-visible{outline:2px solid var(--verde);outline-offset:-2px}
.aj-menu-acoes__item--perigo{color:var(--erro)}
.aj-menu-acoes__item--perigo:hover{background:var(--erro-fundo)}
```

Confira por roteiro: chaves abertas e fechadas em número igual; toda `var(--…)` do bloco existe em `tokens.css` ou no próprio `bundle.css`; nenhuma regra anterior do arquivo dá `fill` ou `stroke` a um `svg` com especificidade maior que a de `.aj-app svg.aj-icone-cheio`.

- [ ] **Step 3: README e prévia**

Componentes novos, cada um com `README.md` (primeira frase é o resumo; depois o que cada classe faz, as regras de uso e o que quem usa fornece) e `preview.html` (linha 1 `<!-- @dsCard group="…" height=… -->`, como as prévias da 9A):

| Pasta | Grupo | O que a prévia mostra | Regras de uso que o README registra |
|---|---|---|---|
| `Paginacao` | Conteúdo | A linha com "Página 1 de 3 · 45 atividades", "Anterior" desabilitado e "Próxima" | Fica ao fim da lista; o texto traz a página, o total de páginas e o total de itens com o nome do item; no primeiro e no último, o botão fica desabilitado, não some |
| `ListaEmPainel` | Conteúdo | Um `aj-painel aj-painel--principal` com três linhas de documento, a ação sendo o botão de ícone "Baixar" | Para tablet e mobile, no lugar da tabela; o título quebra em qualquer ponto; a segunda linha junta os dados com " · "; uma ação só por linha (mais de uma: menu de ações) |
| `MenuDeAcoes` | Ações | O botão de três pontos e a lista aberta com "Baixar", "Renomear" e "Excluir" (este em cor de erro) | Leva as mesmas ações da linha do desktop, na mesma ordem, a destrutiva por último; fecha ao escolher, ao tocar fora e com Esc; o botão diz "Ações de {item}" |
| `EstadoDaLista` | Retorno | A caixa tracejada com "Nenhum documento." | Para lista vazia; carregando é a frase "Carregando…" em `aj-subtitulo`; falha de consulta é um aviso de erro com "Tentar de novo" |

Componentes que ganham modificador (README emendado; prévia emendada onde já existe):

- Botão: `aj-botao--compacto` (32px, para "Baixar" dentro da tabela) e `aj-botao--menor` (texto de 14px, para "Renomear" e "Excluir" na linha da tabela); o ícone de três pontos usa `aj-icone-cheio`.
- Tabela: `aj-tabela__acao--varias`; a coluna de ações tem cabeçalho só para leitor de tela (`aj-so-leitor`).
- Aviso: `aj-aviso--com-acoes`, `aj-aviso__texto` (com título em `strong`) e `aj-aviso__acoes`; "Cancelar" vem antes do verbo; a confirmação é `role="alertdialog"` com `aria-label`.
- Painel e cartão: `aj-painel--cartao` e `aj-cartao__meta--tabular`, para cartão de leitura que não é link.

`design-system.json`: só `lastChange` (`by` o usuário, `via` "Claude Code", `at` agora, `note` "Fase 9B: paginação, lista em painel, menu de ações, estado da lista, aviso com ações.").

- [ ] **Step 4: Publicar**

Uma chamada de publicação com os arquivos alterados e, por último na lista, o índice relido logo antes. Use o caminho longo do scratchpad em `root` e `file_path` (o caminho curto `GABRIE~1` é recusado). Anote a versão publicada no ledger. Abra a página do design system no Chrome e confira as quatro prévias novas e as emendadas.

- [ ] **Step 5: Copiar para o código**

`wwwroot/css/componentes.css` = as duas linhas de cabeçalho que o arquivo já tem + o `bundle.css` publicado, da linha "Visões:" em diante. Confira por roteiro que o corpo é idêntico. `tokens.css` não muda nesta fase.

- [ ] **Step 6: Build e commit**

Run: `dotnet build`
Expected: 0 avisos.

```
git add src/AtividadesJuridicas.Web/wwwroot/css/componentes.css
git commit -m "feat(web): design system com paginação, lista em painel, menu de ações, estado da lista e aviso com ações"
```

---

### Task 2: Trava mais forte e testes de tela no lugar certo

**Files:**
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`
- Create: `tests/AtividadesJuridicas.IntegrationTests/BlocoDeVisao.cs`
- Create: `tests/AtividadesJuridicas.IntegrationTests/BlocoDeVisaoTests.cs`
- Create: `tests/AtividadesJuridicas.IntegrationTests/DespachanteCombinado.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/TelaDeTeste.cs`
- Create: `tests/AtividadesJuridicas.IntegrationTests/InicioWebTests.cs`
- Create: `tests/AtividadesJuridicas.IntegrationTests/MinhaSenhaWebTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs`

**Interfaces:**
- Consumes: nada das outras tarefas.
- Produces:
  - `BlocoDeVisao.De(string html, string classe) : string` — o conteúdo de todos os blocos `<div class="{classe}">` ou `<span class="{classe}">` do HTML, concatenado; vazio quando não há bloco.
  - `DespachanteCombinado` — `IDespachante` de teste: `Responde<TRequisicao, TResposta>(TResposta resposta)` devolve o próprio objeto; `EnviarAsync` devolve a resposta combinada para o tipo da requisição e lança `InvalidOperationException` com o nome do tipo quando nada foi combinado.
  - `TelaDeTeste.RenderizarComAsync<TComponente>(IDespachante despachante, Dictionary<string, object?> parametros) : Task<string>` — desenha com o despachante informado e o fuso `America/Sao_Paulo`, sem aplicação nem banco.
  - Na trava: `Violacoes(string marcacao)` passa a devolver também `"<style>"`, `"<{elemento}> sem classe"` e a classe de fora passada em `Classe=`.

**Regras:**
- A trava passa a recusar, nas telas migradas: bloco `<style`; arquivo `.razor.css` ao lado da tela; `<button`, `<table`, `<input`, `<select>` e `<textarea>` sem atributo `class`; classe fora do prefixo em `Classe="…"` de componente; classes separadas por qualquer espaço em branco (tabulação e quebra de linha, não só espaço).
- A expressão que acha o elemento cru consome os valores entre aspas inteiros: um `=>` dentro de `@onclick="() => F(x)"` não encerra a marca. Componentes (`<InputText`, `<InputFile`, inicial maiúscula) não são elementos crus.
- Se um arquivo já listado em `Migradas` reprovar na trava nova, corrija a marcação dele (dando a classe do design system que falta) e relate qual; não tire o arquivo da lista nem afrouxe a regra.
- Os testes de Início e o de Minha senha saem de `LayoutWebTests` sem mudar uma asserção: é mudança de arquivo. `LayoutWebTests` fica com o que o layout decide (destino aceso, barras, avatar, foco, telas não migradas).

- [ ] **Step 1: Testes da trava (devem falhar)**

Acrescente aos testes da própria trava:

```csharp
    [Theory]
    [InlineData("<style>p{margin:0}</style>", "<style>")]
    [InlineData("<button type=\"button\">x</button>", "<button> sem classe")]
    [InlineData("<button type=\"button\" @onclick=\"() => Abrir(x)\">x</button>", "<button> sem classe")]
    [InlineData("<table><tr><td>x</td></tr></table>", "<table> sem classe")]
    [InlineData("<input type=\"text\" @bind=\"_nome\" />", "<input> sem classe")]
    [InlineData("<select @bind=\"_x\"></select>", "<select> sem classe")]
    [InlineData("<textarea rows=\"3\"></textarea>", "<textarea> sem classe")]
    [InlineData("<Icone Nome=\"avancar\" Classe=\"seta\" />", "seta")]
    [InlineData("<p class=\"aj-aviso\terro\">x</p>", "erro")]
    [InlineData("<p class=\"aj-aviso\nerro\">x</p>", "erro")]
    public void A_trava_reprova_o_que_volta_a_marcacao_antiga(string marcacao, string violacao)
    {
        Violacoes(marcacao).Should().Equal(violacao);
    }

    [Theory]
    [InlineData("<button type=\"button\" class=\"aj-botao\" @onclick=\"() => Abrir(x)\">x</button>")]
    [InlineData("<button type=\"button\" @onclick=\"() => Abrir(x)\" class=\"aj-botao\">x</button>")]
    [InlineData("<table class=\"aj-tabela\"><tr><td>x</td></tr></table>")]
    [InlineData("<input type=\"text\" class=\"aj-campo\" @bind=\"_nome\" />")]
    [InlineData("<InputText @bind-Value=\"_x\" class=\"aj-campo\" />")]
    [InlineData("<InputFile OnChange=\"Escolher\" class=\"aj-campo\" />")]
    [InlineData("<button type=\"button\" class=\"@Classe\">x</button>")]
    public void A_trava_aceita_elemento_com_classe_do_design_system(string marcacao)
    {
        Violacoes(marcacao).Should().BeEmpty();
    }
```

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Expected: os casos novos de reprovação falham (a trava ainda não os enxerga).

- [ ] **Step 2: A trava**

```csharp
    [GeneratedRegex("\\sstyle\\s*=\\s*[\"']")]
    private static partial Regex EstiloEmbutido();

    [GeneratedRegex("<style\\b", RegexOptions.IgnoreCase)]
    private static partial Regex FolhaEmbutida();

    // Só o valor fixo: classe montada por expressão Razor (com @) é conferida na revisão.
    [GeneratedRegex("\\s(?:class|Classe)\\s*=\\s*(?:\"([^\"@]*)\"|'([^'@]*)')")]
    private static partial Regex ClasseFixa();

    // A marca inteira de um elemento que o app.css veste quando está sem classe. Os valores entre aspas
    // são consumidos inteiros, para o "=>" de uma expressão não encerrar a marca.
    [GeneratedRegex("<(button|table|input|select|textarea)\\b((?:[^>\"']|\"[^\"]*\"|'[^']*')*)>")]
    private static partial Regex ElementoQueOAppCssVeste();

    [GeneratedRegex("\\sclass\\s*=")]
    private static partial Regex TemClasse();

    /// <summary>O que a marcação tem de fora do design system.</summary>
    private static List<string> Violacoes(string marcacao)
    {
        var violacoes = new List<string>();
        if (EstiloEmbutido().IsMatch(marcacao)) violacoes.Add("style");
        if (FolhaEmbutida().IsMatch(marcacao)) violacoes.Add("<style>");

        violacoes.AddRange(ElementoQueOAppCssVeste().Matches(marcacao)
            .Where(m => !TemClasse().IsMatch(m.Groups[2].Value))
            .Select(m => $"<{m.Groups[1].Value}> sem classe"));

        violacoes.AddRange(ClasseFixa().Matches(marcacao)
            .SelectMany(m => (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Where(classe => !classe.StartsWith("aj-", StringComparison.Ordinal)));
        return violacoes;
    }
```

No teste que percorre `Migradas`, acrescente, depois da asserção das violações:

```csharp
        File.Exists(caminho + ".css").Should().BeFalse("tela migrada não tem CSS próprio: o estilo vem do design system");
```

Atualize o resumo da classe de teste para dizer o que a trava recusa agora. Os dois pontos cegos que ficam (classe montada por expressão Razor; marcação dentro de comentário `@* *@`) continuam anotados no comentário.

Run: o mesmo filtro.
Expected: todos passam, inclusive os dez arquivos de `Migradas`. Se um arquivo de `Migradas` reprovar, vale a terceira regra desta tarefa.

- [ ] **Step 3: `BlocoDeVisao`, com os seus testes**

Testes primeiro (`BlocoDeVisaoTests.cs`, sem banco):

```csharp
public class BlocoDeVisaoTests
{
    [Fact]
    public void Recorta_o_bloco_com_os_divs_de_dentro()
    {
        const string html = "<main><div class=\"aj-so-desktop\"><div class=\"aj-tabela-caixa\"><table></table></div><p>fim</p></div><div class=\"aj-tablet-mobile\"><span>cartão</span></div></main>";

        BlocoDeVisao.De(html, "aj-so-desktop").Should().Be("<div class=\"aj-tabela-caixa\"><table></table></div><p>fim</p>");
        BlocoDeVisao.De(html, "aj-tablet-mobile").Should().Be("<span>cartão</span>");
    }

    [Fact]
    public void Junta_os_blocos_da_mesma_visao_e_aceita_span()
    {
        const string html = "<span class=\"aj-so-mobile\">um</span><p>meio</p><div class=\"aj-so-mobile\">dois</div>";

        BlocoDeVisao.De(html, "aj-so-mobile").Should().Be("umdois");
    }

    [Fact]
    public void Sem_bloco_devolve_vazio_e_nao_confunde_classe_parecida()
    {
        const string html = "<div class=\"aj-so-desktop-extra\">x</div><div class=\"aj-painel aj-so-desktop\">y</div>";

        BlocoDeVisao.De(html, "aj-so-desktop").Should().BeEmpty();
    }
}
```

Implementação (`BlocoDeVisao.cs`):

```csharp
using System.Text;
using System.Text.RegularExpressions;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Recorta do HTML o conteúdo dos blocos de uma visão (<c>aj-so-desktop</c>, <c>aj-tablet-mobile</c>…), para a
/// asserção olhar uma visão só. Bloco vazio aceita qualquer <c>NotContain</c>: toda asserção negativa sobre um
/// bloco vem com uma positiva sobre o mesmo bloco.
/// </summary>
public static class BlocoDeVisao
{
    public static string De(string html, string classe)
    {
        var blocos = new StringBuilder();
        foreach (Match abertura in Regex.Matches(html, $"<(div|span) class=\"{Regex.Escape(classe)}\">"))
        {
            var elemento = abertura.Groups[1].Value;
            var inicio = abertura.Index + abertura.Length;
            var profundidade = 1;
            var fim = -1;
            foreach (Match marca in Regex.Matches(html[inicio..], $"<(/?){elemento}\\b"))
            {
                profundidade += marca.Groups[1].Length == 0 ? 1 : -1;
                if (profundidade > 0) continue;

                fim = inicio + marca.Index;
                break;
            }

            // HTML quebrado não pode passar por bloco vazio.
            if (fim < 0) throw new InvalidOperationException($"O bloco {classe} não foi fechado.");
            blocos.Append(html, inicio, fim - inicio);
        }

        return blocos.ToString();
    }
}
```

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~BlocoDeVisaoTests"`
Expected: 3 passando.

- [ ] **Step 4: Início e Minha senha nos seus arquivos**

Mova de `LayoutWebTests` para `InicioWebTests` os testes cujo nome começa por `Inicio_`, e para `MinhaSenhaWebTests` o `Minha_senha_tem_o_painel_o_voltar_do_mobile_e_um_botao_por_visao`. Cada classe nova repete o esqueleto de `LayoutWebTests` (`[Collection(ColecaoBanco.Nome)]`, `AplicacaoDeTeste`, `IAsyncLifetime`) e o auxiliar `PaginaAsync`. Nenhuma asserção muda.

No teste do início da equipe (o que confere a barra superior da tela), acrescente a adjacência que o CSS exige:

```csharp
        // A regra que esconde a barra de logo exige as duas barras lado a lado: nada entre elas.
        html.Should().MatchRegex("</header>(?:\\s|<!--.*?-->)*<header class=\"aj-barra-superior aj-barra-superior--padrao\">");
```

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~LayoutWebTests|FullyQualifiedName~InicioWebTests|FullyQualifiedName~MinhaSenhaWebTests"`
Expected: o mesmo número de testes de antes da mudança, somado, todos passando.

- [ ] **Step 5: Os dois estados do início que não tinham teste**

`DespachanteCombinado.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Despachante de teste que devolve a resposta combinada para cada tipo de requisição. Serve aos estados que o
/// banco de verdade não produz por encomenda: falha de consulta e acesso encerrado.
/// </summary>
public sealed class DespachanteCombinado : IDespachante
{
    private readonly Dictionary<Type, object?> _respostas = [];

    public DespachanteCombinado Responde<TRequisicao, TResposta>(TResposta resposta)
        where TRequisicao : IRequisicao<TResposta>
    {
        _respostas[typeof(TRequisicao)] = resposta;
        return this;
    }

    public Task<TResposta> EnviarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct = default) =>
        _respostas.TryGetValue(requisicao.GetType(), out var resposta)
            ? Task.FromResult((TResposta)resposta!)
            : throw new InvalidOperationException($"Nenhuma resposta combinada para {requisicao.GetType().Name}.");
}
```

Em `TelaDeTeste`, o método novo, e um `IJSRuntime` de teste registrado nos três métodos (o `InputFile` o injeta; o desenho estático nunca o chama, e se chamar o teste falha com a mensagem):

```csharp
    /// <summary>Desenha um componente com o despachante informado: sem aplicação, sem banco.</summary>
    public static async Task<string> RenderizarComAsync<TComponente>(
        IDespachante despachante, Dictionary<string, object?> parametros)
        where TComponente : IComponent
    {
        await using var servicos = new ServiceCollection()
            .AddSingleton(despachante)
            .AddSingleton(new FusoDoEscritorio("America/Sao_Paulo"))
            .AddSingleton<IJSRuntime>(new SemJavaScript())
            .BuildServiceProvider();
        await using var renderizador = new HtmlRenderer(servicos, NullLoggerFactory.Instance);

        return await renderizador.Dispatcher.InvokeAsync(async () =>
            (await renderizador.RenderComponentAsync<TComponente>(ParameterView.FromDictionary(parametros))).ToHtmlString());
    }

    private sealed class SemJavaScript : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            throw new InvalidOperationException($"O desenho estático não chama JavaScript ({identifier}).");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            throw new InvalidOperationException($"O desenho estático não chama JavaScript ({identifier}).");
    }
```

Em `InicioWebTests`, dois testes de componente:

```csharp
    private static readonly UsuarioAutenticado Cliente =
        new(Guid.NewGuid(), "Carla Cliente", TipoPerfil.Cliente, Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public async Task Inicio_do_cliente_com_falha_nos_totais_mostra_o_erro_e_mantem_os_atalhos()
    {
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(Cliente)
            .Responde<ListarMeuAndamento, Resultado<AndamentoDoCliente>>(
                Resultado<AndamentoDoCliente>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<Home>(despachante, new()));

        html.Should().MatchRegex("<p class=\"aj-aviso aj-aviso--erro\" role=\"alert\">[\\s\\S]*?<span>Falha de teste\\.</span></p>")
            .And.Contain("href=\"/andamento\"").And.Contain("href=\"/meus-documentos\"").And.Contain("href=\"/documentos-comuns\"")
            .And.NotContain("aj-totais").And.NotContain("Carregando");
    }

    [Fact]
    public async Task Inicio_do_cliente_com_acesso_encerrado_nao_mostra_atalho()
    {
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(Cliente)
            .Responde<ListarMeuAndamento, Resultado<AndamentoDoCliente>>(
                Resultado<AndamentoDoCliente>.Falha(TipoErro.AcessoNegado, "Acesso negado."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<Home>(despachante, new()));

        html.Should().Contain("Seu acesso foi encerrado. Entre novamente.")
            .And.NotContain("aj-cartao--atalho").And.NotContain("aj-totais").And.NotContain("Acesso negado.");
    }
```

Se o `CabecalhoDePagina` não puder ser desenhado fora do layout (o `SectionContent` reclamar da falta do `SectionOutlet`), desenhe a tela dentro de um componente de teste que traga um `<SectionOutlet SectionName="barra-superior" />`, deixe-o à mão para as próximas tarefas (`TelaDeTeste`) e relate.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~InicioWebTests"`
Expected: todos passando, com os dois novos.

- [ ] **Step 6: Build e commit**

Run: `dotnet build`
Expected: 0 avisos.

```
git add tests
git commit -m "test: trava das telas migradas mais forte, recorte por visão e testes de início e minha senha nos seus arquivos"
```

Se a trava nova obrigou a corrigir marcação de tela já migrada, o commit leva também esse arquivo de `src` e a mensagem ganha "; classe que faltava em <arquivo>".

---

### Task 3: Peças de lista compartilhadas

**Files:**
- Create: `src/AtividadesJuridicas.Web/Infra/Contagem.cs`
- Modify: `src/AtividadesJuridicas.Web/Infra/FusoDoEscritorio.cs`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/Icone.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/Aviso.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/CabecalhoDePagina.razor`
- Create: `src/AtividadesJuridicas.Web/Components/Layout/Paginacao.razor`
- Create: `src/AtividadesJuridicas.Web/Components/Layout/EstadoDaLista.razor`
- Create: `src/AtividadesJuridicas.Web/Components/Layout/MenuDeAcoes.razor`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/ComponentesDoDesignSystemTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/FusoDoEscritorioTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/TelasPurasTests.cs` (teste de `Contagem`)
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: as classes da Tarefa 1; `TelaDeTeste.RenderizarSemDadosAsync`.
- Produces:
  - `Contagem.De(int quantidade, string singular, string plural) : string` — "1 documento", "8 documentos", "0 documentos".
  - `FusoDoEscritorio.FormatarData(DateTime? utc) : string` — "05/10/2026" no fuso do escritório; "—" para nulo.
  - `<Icone Nome="mais" />` emite `class="aj-icone-cheio"` (somada a `Classe`, quando houver) e nenhum `style`.
  - `<Aviso Tipo Titulo? Rotulo? Acoes?>`: sem `Acoes`, a marcação de hoje; com `Acoes`, um `div` com `aj-aviso--com-acoes`. Método público `FocarAsync() : ValueTask`.
  - `<CabecalhoDePagina … Acao? IconeDaAcao="adicionar" AcaoHref? OnAcao?>`: com `Acao`, o botão primário no cabeçalho e o botão flutuante.
  - `<Paginacao Numero TotalDePaginas Total Singular Plural SempreVisivel? Ocupado? IrPara>`; `IrPara` é `EventCallback<int>` com o número da página pedida.
  - `<EstadoDaLista Erro? Carregando Vazio TextoDeVazio TentarDeNovo>` com `ChildContent`: a lista, desenhada só quando não há erro, não está carregando e não está vazia.
  - `<MenuDeAcoes Rotulo>` com `ChildContent`: os itens, cada um um `<a>` ou `<button type="button">` com `class="aj-menu-acoes__item"`.

**Regras:**
- `Aviso`: o papel é `alertdialog` quando há `Rotulo`; `alert` quando o tipo é erro; `status` nos demais. Com ações, o `div` leva `tabindex="-1"` (para receber o foco por código) e `aria-label` com o `Rotulo`.
- `Paginacao`: não desenha nada quando há uma página só, salvo com `SempreVisivel`. "Anterior" fica desabilitado na primeira página e "Próxima" na última; os dois, enquanto `Ocupado`.
- `EstadoDaLista`: o erro vence o carregando, que vence o vazio.
- `MenuDeAcoes`: a lista está sempre no HTML, com `hidden` enquanto fechada; o fundo que fecha ao tocar fora só existe com o menu aberto; Esc fecha e devolve o foco ao botão; ao abrir, o foco vai para a lista. Falha de foco (circuito caindo) não derruba a tela.
- `CabecalhoDePagina`: com `AcaoHref`, botão e botão flutuante são `<a>`; sem ele, são `<button type="button">` que chamam `OnAcao`. O botão flutuante leva `aria-label` com o texto da ação.

- [ ] **Step 1: Testes (devem falhar)**

Em `ComponentesDoDesignSystemTests`:

```csharp
    [Fact]
    public async Task Icone_de_mais_e_preenchido_por_classe_e_nao_por_estilo()
    {
        var solto = await TelaDeTeste.RenderizarSemDadosAsync<Icone>(new() { ["Nome"] = "mais" });
        var comClasse = await TelaDeTeste.RenderizarSemDadosAsync<Icone>(new() { ["Nome"] = "mais", ["Classe"] = "aj-x" });

        solto.Should().Contain("class=\"aj-icone-cheio\"").And.NotContain("style=");
        comClasse.Should().Contain("class=\"aj-x aj-icone-cheio\"");
    }

    [Fact]
    public async Task Aviso_com_acoes_e_um_dialogo_de_alerta_com_titulo_e_botoes()
    {
        RenderFragment texto = c => c.AddContent(0, "Não envie aqui arquivos de um cliente.");
        RenderFragment acoes = c => c.AddMarkupContent(0, "<button type=\"button\" class=\"aj-botao aj-botao--secundario\">Cancelar</button>");

        var html = await TelaDeTeste.RenderizarSemDadosAsync<Aviso>(new()
        {
            ["Tipo"] = TipoDeAviso.Atencao, ["Rotulo"] = "Confirmar envio", ["Titulo"] = "modelo.pdf · 96 KB",
            ["ChildContent"] = texto, ["Acoes"] = acoes,
        });

        html.Should().MatchRegex("<div class=\"aj-aviso aj-aviso--atencao aj-aviso--com-acoes\"[^>]*role=\"alertdialog\"")
            .And.Contain("aria-label=\"Confirmar envio\"").And.Contain("tabindex=\"-1\"")
            .And.Contain("<strong>modelo.pdf · 96 KB</strong>").And.Contain("Não envie aqui arquivos de um cliente.")
            .And.MatchRegex("<div class=\"aj-aviso__acoes\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--secundario\">Cancelar</button>");
    }

    [Fact]
    public async Task Aviso_de_erro_com_acao_continua_sendo_alerta()
    {
        RenderFragment texto = c => c.AddContent(0, "Não foi possível consultar.");
        RenderFragment acoes = c => c.AddMarkupContent(0, "<button type=\"button\" class=\"aj-botao aj-botao--secundario\">Tentar de novo</button>");

        var html = await TelaDeTeste.RenderizarSemDadosAsync<Aviso>(
            new() { ["Tipo"] = TipoDeAviso.Erro, ["ChildContent"] = texto, ["Acoes"] = acoes });

        html.Should().Contain("role=\"alert\"").And.NotContain("alertdialog").And.NotContain("aria-label=").And.NotContain("<strong>");
    }

    [Theory]
    [InlineData(1, 3, 45, false, "Página 1 de 3 · 45 atividades", true, false)]
    [InlineData(3, 3, 45, false, "Página 3 de 3 · 45 atividades", false, true)]
    [InlineData(1, 1, 1, true, "Página 1 de 1 · 1 atividade", true, true)]
    public async Task Paginacao_diz_onde_esta_e_trava_as_pontas(
        int numero, int paginas, int total, bool sempre, string texto, bool anteriorTravado, bool proximaTravada)
    {
        var html = await TelaDeTeste.RenderizarSemDadosAsync<Paginacao>(new()
        {
            ["Numero"] = numero, ["TotalDePaginas"] = paginas, ["Total"] = total,
            ["Singular"] = "atividade", ["Plural"] = "atividades", ["SempreVisivel"] = sempre,
        });

        html.Should().Contain("<div class=\"aj-paginacao\">").And.Contain($"<span class=\"aj-subtitulo\">{texto}</span>");
        Regex.IsMatch(html, "<button[^>]*disabled[^>]*>Anterior</button>").Should().Be(anteriorTravado);
        Regex.IsMatch(html, "<button[^>]*disabled[^>]*>Próxima</button>").Should().Be(proximaTravada);
        Regex.Matches(html, "<button type=\"button\" class=\"aj-botao aj-botao--secundario\"").Should().HaveCount(2);
    }

    [Fact]
    public async Task Paginacao_de_uma_pagina_so_nao_aparece()
    {
        var html = await TelaDeTeste.RenderizarSemDadosAsync<Paginacao>(new()
        {
            ["Numero"] = 1, ["TotalDePaginas"] = 1, ["Total"] = 4, ["Singular"] = "documento", ["Plural"] = "documentos",
        });

        html.Trim().Should().BeEmpty();
    }

    [Theory]
    [InlineData("Falhou.", true, true, "aj-aviso--erro", "Tentar de novo")]
    [InlineData(null, true, true, "aj-subtitulo", "Carregando…")]
    [InlineData(null, false, true, "aj-estado-lista", "Nenhum documento.")]
    [InlineData(null, false, false, "aj-linha", "a lista")]
    public async Task Estado_da_lista_mostra_um_estado_por_vez(
        string? erro, bool carregando, bool vazio, string classe, string texto)
    {
        RenderFragment lista = c => c.AddMarkupContent(0, "<div class=\"aj-linha\">a lista</div>");

        var html = await TelaDeTeste.RenderizarSemDadosAsync<EstadoDaLista>(new()
        {
            ["Erro"] = erro, ["Carregando"] = carregando, ["Vazio"] = vazio,
            ["TextoDeVazio"] = "Nenhum documento.", ["ChildContent"] = lista,
        });

        html.Should().Contain(classe).And.Contain(texto);
        // Um estado só: os textos dos outros não aparecem.
        foreach (var outro in new[] { "Tentar de novo", "Carregando…", "Nenhum documento.", "a lista" }.Where(t => t != texto))
            html.Should().NotContain(outro);
    }

    [Fact]
    public async Task Menu_de_acoes_nasce_fechado_com_os_itens_no_html()
    {
        RenderFragment itens = c => c.AddMarkupContent(0, "<button type=\"button\" class=\"aj-menu-acoes__item\">Renomear</button>");

        var html = await TelaDeTeste.RenderizarSemDadosAsync<MenuDeAcoes>(
            new() { ["Rotulo"] = "Ações de modelo.pdf", ["ChildContent"] = itens });

        html.Should().MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\"[^>]*aria-label=\"Ações de modelo.pdf\"")
            .And.Contain("aria-expanded=\"false\"").And.Contain("class=\"aj-icone-cheio\"")
            .And.MatchRegex("<div class=\"aj-menu-acoes__lista\"[^>]*\\shidden[^>]*>\\s*<button type=\"button\" class=\"aj-menu-acoes__item\">Renomear</button>")
            .And.NotContain("aj-menu-acoes__fundo");
    }

    [Fact]
    public async Task Cabecalho_com_acao_traz_o_botao_e_o_botao_flutuante()
    {
        var botao = await TelaDeTeste.RenderizarSemDadosAsync<CabecalhoDePagina>(
            new() { ["Titulo"] = "Documentos comuns", ["Acao"] = "Enviar documento" });
        var link = await TelaDeTeste.RenderizarSemDadosAsync<CabecalhoDePagina>(
            new() { ["Titulo"] = "Clientes", ["Acao"] = "Novo cliente", ["AcaoHref"] = "/clientes/novo" });
        var semAcao = await TelaDeTeste.RenderizarSemDadosAsync<CabecalhoDePagina>(new() { ["Titulo"] = "Início" });

        // O ícone é um componente: pode haver espaço em branco em volta do svg.
        botao.Should().MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Enviar documento</button>")
            .And.MatchRegex("<button type=\"button\" class=\"aj-fab\" aria-label=\"Enviar documento\"");
        link.Should().MatchRegex("<a href=\"/clientes/novo\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Novo cliente</a>")
            .And.MatchRegex("<a href=\"/clientes/novo\" class=\"aj-fab\" aria-label=\"Novo cliente\"");
        semAcao.Should().NotContain("aj-fab").And.NotContain("aj-botao--primario");
    }
```

O arquivo passa a precisar de `using System.Text.RegularExpressions;`. Se o `CabecalhoDePagina` não puder ser desenhado fora do layout, use o mesmo componente de teste com `SectionOutlet` da Tarefa 2 e relate.

Em `FusoDoEscritorioTests`:

```csharp
    [Fact]
    public void Formatar_data_traz_so_o_dia_no_fuso_do_escritorio()
    {
        var fuso = new FusoDoEscritorio("America/Sao_Paulo");

        // 01:30 UTC do dia 6 ainda é dia 5 em São Paulo.
        fuso.FormatarData(new DateTime(2026, 10, 6, 1, 30, 0, DateTimeKind.Utc)).Should().Be("05/10/2026");
        fuso.FormatarData(null).Should().Be("—");
    }
```

Em `TelasPurasTests`:

```csharp
public class ContagemTests
{
    [Theory]
    [InlineData(0, "0 documentos")]
    [InlineData(1, "1 documento")]
    [InlineData(8, "8 documentos")]
    public void Contagem_usa_o_singular_so_para_um(int quantidade, string esperado)
    {
        Contagem.De(quantidade, "documento", "documentos").Should().Be(esperado);
    }
}
```

Run: `dotnet build`
Expected: não compila (os componentes e métodos ainda não existem). É o RED desta tarefa.

- [ ] **Step 2: `Contagem`, `FormatarData` e o ícone**

```csharp
namespace AtividadesJuridicas.Web.Infra;

/// <summary>A quantidade com o nome do item no número certo: "1 documento", "8 documentos".</summary>
public static class Contagem
{
    public static string De(int quantidade, string singular, string plural) =>
        $"{quantidade} {(quantidade == 1 ? singular : plural)}";
}
```

Em `FusoDoEscritorio`:

```csharp
    /// <summary>Só o dia, no fuso do escritório.</summary>
    public string FormatarData(DateTime? utc) =>
        utc is { } valor ? ParaLocal(valor).ToString("dd/MM/yyyy", Cultura) : "—";
```

Em `Icone.razor`, a marca passa a `<svg class="@ClasseDoIcone" viewBox="0 0 24 24" aria-hidden="true" focusable="false">@Tracado</svg>`, sem `style`, com:

```csharp
    // Os três pontos são preenchidos; os demais ícones são de traço.
    private string? ClasseDoIcone => Nome == "mais"
        ? (Classe is null ? "aj-icone-cheio" : $"{Classe} aj-icone-cheio")
        : Classe;
```

- [ ] **Step 3: `Aviso` com ações**

```razor
@* Faixa de retorno do design system: sucesso, atenção ou erro, sempre com o ícone do tipo.
   Com ações, vira a caixa de confirmação (ou o erro com nova tentativa) dos quadros. *@
@if (Acoes is null)
{
    <p class="@Classe" role="@Papel"><Icone Nome="@NomeDoIcone" /><span>@ChildContent</span></p>
}
else
{
    <div class="@($"{Classe} aj-aviso--com-acoes")" role="@Papel" aria-label="@Rotulo" tabindex="-1" @ref="_caixa">
        <Icone Nome="@NomeDoIcone" />
        <div class="aj-aviso__texto">
            @if (Titulo is not null)
            {
                <strong>@Titulo</strong>
            }
            @ChildContent
        </div>
        <div class="aj-aviso__acoes">@Acoes</div>
    </div>
}
```

Parâmetros novos: `Acoes` (`RenderFragment?`), `Titulo` (`string?`), `Rotulo` (`string?`, "o nome acessível da confirmação; com ele o aviso é um diálogo de alerta"). `Papel` segue a primeira regra da tarefa. `public ValueTask FocarAsync() => _caixa.FocusAsync();`.

- [ ] **Step 4: `Paginacao` e `EstadoDaLista`**

```razor
@* A linha de paginação do design system, ao fim da lista. *@
@if (SempreVisivel || TotalDePaginas > 1)
{
    <div class="aj-paginacao">
        <span class="aj-subtitulo">@Texto</span>
        <div class="aj-paginacao__botoes">
            <button type="button" class="aj-botao aj-botao--secundario" disabled="@(Ocupado || Numero <= 1)" @onclick="() => IrPara.InvokeAsync(Numero - 1)">Anterior</button>
            <button type="button" class="aj-botao aj-botao--secundario" disabled="@(Ocupado || Numero >= TotalDePaginas)" @onclick="() => IrPara.InvokeAsync(Numero + 1)">Próxima</button>
        </div>
    </div>
}

@code {
    [Parameter, EditorRequired] public int Numero { get; set; }
    [Parameter, EditorRequired] public int TotalDePaginas { get; set; }
    [Parameter, EditorRequired] public int Total { get; set; }

    /// <summary>O nome do item: "atividade" e "atividades".</summary>
    [Parameter, EditorRequired] public string Singular { get; set; } = string.Empty;
    [Parameter, EditorRequired] public string Plural { get; set; } = string.Empty;

    /// <summary>Mostra a linha mesmo com uma página só, como o quadro do Andamento no desktop.</summary>
    [Parameter] public bool SempreVisivel { get; set; }

    /// <summary>Uma consulta está em curso: os dois botões esperam.</summary>
    [Parameter] public bool Ocupado { get; set; }

    [Parameter] public EventCallback<int> IrPara { get; set; }

    private string Texto => $"Página {Numero} de {TotalDePaginas} · {Contagem.De(Total, Singular, Plural)}";
}
```

```razor
@* O que aparece no lugar da lista: falha de consulta, carregando ou nada a mostrar. *@
@if (Erro is not null)
{
    <Aviso Tipo="TipoDeAviso.Erro">
        <ChildContent>@Erro</ChildContent>
        <Acoes><button type="button" class="aj-botao aj-botao--secundario" @onclick="TentarDeNovo">Tentar de novo</button></Acoes>
    </Aviso>
}
else if (Carregando)
{
    <p class="aj-subtitulo" role="status">Carregando…</p>
}
else if (Vazio)
{
    <div class="aj-estado-lista"><strong>@TextoDeVazio</strong></div>
}
else
{
    @ChildContent
}

@code {
    [Parameter] public string? Erro { get; set; }
    [Parameter] public bool Carregando { get; set; }
    [Parameter] public bool Vazio { get; set; }
    [Parameter, EditorRequired] public string TextoDeVazio { get; set; } = string.Empty;
    [Parameter] public EventCallback TentarDeNovo { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

- [ ] **Step 5: `MenuDeAcoes`**

```razor
@* O menu que o botão de três pontos abre, com as ações da linha (a destrutiva por último).
   Fecha ao escolher (o clique do item sobe até a lista), ao tocar fora e com Esc. *@
<span class="aj-menu-acoes" @onkeydown="AoTeclarAsync">
    <button type="button" class="aj-botao aj-botao--fantasma aj-botao--icone" aria-label="@Rotulo"
            aria-expanded="@(_aberto ? "true" : "false")" @ref="_gatilho" @onclick="Alternar"><Icone Nome="mais" /></button>
    @if (_aberto)
    {
        <button type="button" class="aj-menu-acoes__fundo" tabindex="-1" aria-hidden="true" @onclick="Fechar"></button>
    }
    <div class="aj-menu-acoes__lista" hidden="@(!_aberto)" tabindex="-1" @ref="_lista" @onclick="Fechar">@ChildContent</div>
</span>

@code {
    /// <summary>O que o botão diz ao leitor de tela: "Ações de Procuração.pdf".</summary>
    [Parameter, EditorRequired] public string Rotulo { get; set; } = string.Empty;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    private bool _aberto;
    private bool _focarLista;
    private ElementReference _gatilho;
    private ElementReference _lista;

    private void Alternar()
    {
        _aberto = !_aberto;
        _focarLista = _aberto;
    }

    private void Fechar() => _aberto = false;

    private async Task AoTeclarAsync(KeyboardEventArgs e)
    {
        if (e.Key != "Escape" || !_aberto) return;

        _aberto = false;
        await FocarAsync(_gatilho);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_focarLista) return;

        _focarLista = false;
        await FocarAsync(_lista);
    }

    private static async Task FocarAsync(ElementReference alvo)
    {
        try
        {
            await alvo.FocusAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSException or JSDisconnectedException)
        {
            // Sem foco (circuito caindo, por exemplo) o menu continua funcionando.
        }
    }
}
```

- [ ] **Step 6: Ação principal no `CabecalhoDePagina`**

Dentro do `<header class="aj-cabecalho">`, depois do `div.aj-cabecalho__inicio`:

```razor
        @if (Acao is not null)
        {
            @if (AcaoHref is not null)
            {
                <a href="@AcaoHref" class="aj-botao aj-botao--primario aj-botao--com-icone"><Icone Nome="@IconeDaAcao" />@Acao</a>
            }
            else
            {
                <button type="button" class="aj-botao aj-botao--primario aj-botao--com-icone" @onclick="OnAcao"><Icone Nome="@IconeDaAcao" />@Acao</button>
            }
        }
```

Depois do `div.aj-desktop-tablet`, fora dele (o botão flutuante se esconde sozinho fora do mobile):

```razor
@if (Acao is not null)
{
    @if (AcaoHref is not null)
    {
        <a href="@AcaoHref" class="aj-fab" aria-label="@Acao"><Icone Nome="@IconeDaAcao" /></a>
    }
    else
    {
        <button type="button" class="aj-fab" aria-label="@Acao" @onclick="OnAcao"><Icone Nome="@IconeDaAcao" /></button>
    }
}
```

Parâmetros: `Acao` (`string?`, "a ação principal da tela: botão no cabeçalho do desktop e do tablet, botão flutuante no mobile"), `IconeDaAcao` (`string`, padrão `"adicionar"`), `AcaoHref` (`string?`), `OnAcao` (`EventCallback`). Atualize o comentário do topo do componente.

- [ ] **Step 7: Trava e testes**

Acrescente a `Migradas`: `Components/Layout/EstadoDaLista.razor`, `Components/Layout/Icone.razor`, `Components/Layout/MenuDeAcoes.razor`, `Components/Layout/Paginacao.razor` (em ordem alfabética com os demais).

Run: `dotnet build`
Expected: 0 avisos.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~ComponentesDoDesignSystemTests|FullyQualifiedName~FusoDoEscritorioTests|FullyQualifiedName~ContagemTests|FullyQualifiedName~LayoutWebTests|FullyQualifiedName~InicioWebTests|FullyQualifiedName~MinhaSenhaWebTests"`
Expected: todos passando.

- [ ] **Step 8: Commit**

```
git add src tests
git commit -m "feat(web): paginação, estado da lista, menu de ações, aviso com ações e ação principal no cabeçalho de página"
```

---

### Task 4: Andamento

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Andamento.razor`
- Delete: `src/AtividadesJuridicas.Web/Components/Portal/TotaisDoAndamento.razor`
- Modify: `src/AtividadesJuridicas.Web/wwwroot/app.css` (saem as três regras `.totais`)
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PortalDoClienteWebTests.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: `CabecalhoDePagina`, `Aviso`, `EstadoDaLista`, `Paginacao`, `Contagem.De`, `FusoDoEscritorio.FormatarData`, `BlocoDeVisao.De`; `ListarMeuAndamento` e `AndamentoDoCliente(Atividades, EmAndamento, Concluidas)`, que não mudam.
- Produces: nada que outra tarefa consuma.

**Regras:**
- Quadros: `Andamento.dc.html`, `AndamentoTablet.dc.html`, `AndamentoMobile.dc.html`.
- O subtítulo e os números dos chips vêm dos totais da consulta, que não dependem do filtro. Antes da primeira resposta (e depois de uma falha sem totais) o subtítulo não aparece e os chips trazem só o nome.
- "Todas" é a soma de em andamento e concluídas.
- A lógica de carregar fica como está (geração da consulta, volta à última página, acesso encerrado). Só o que escolhe a situação muda: sai o seletor, entram os chips.
- Tabela no desktop; painéis no tablet e no mobile. A paginação do desktop aparece sempre que há atividades; a dos painéis, só com mais de uma página.
- Sem área e sem processo: `—` na tabela; `Sem área` e `Sem processo` no cartão. "Concluída em" na tabela é só a data, ou `—`; no cartão, o rodapé só existe quando a atividade foi concluída.
- `TotaisDoAndamento.razor` e as regras `.totais`, `.totais dt` e `.totais dd` do `app.css` saem. Antes de apagar, confirme por busca que nada mais usa a classe `totais` nem o componente.

- [ ] **Step 1: Reescrever os três testes de página (devem falhar)**

Em `PortalDoClienteWebTests`:

```csharp
    [Fact]
    public async Task Cliente_ve_o_andamento_com_os_totais_e_a_situacao()
    {
        await CriarAndamentoDaMariaAsync();
        var cliente = await DaCarlaAsync();

        var html = Decodificado(await cliente.GetStringAsync("/andamento"));

        // Os totais estão no subtítulo e nos chips, e não dependem do filtro.
        html.Should().Contain("<p class=\"aj-subtitulo\">2 em andamento · 1 concluída</p>")
            .And.MatchRegex("<button type=\"button\" class=\"aj-chip\" aria-pressed=\"true\"[^>]*>Todas · 3</button>")
            .And.MatchRegex("<button type=\"button\" class=\"aj-chip\" aria-pressed=\"false\"[^>]*>Em andamento · 2</button>")
            .And.MatchRegex("<button type=\"button\" class=\"aj-chip\" aria-pressed=\"false\"[^>]*>Concluídas · 1</button>");

        var tabela = BlocoDeVisao.De(html, "aj-so-desktop");
        tabela.Should().Contain("<th scope=\"col\">Atividade</th>").And.Contain("<th scope=\"col\">Concluída em</th>")
            .And.Contain("<td class=\"aj-tabela__principal\">Contestação trabalhista</td>")
            .And.Contain("Parecer sem processo").And.Contain("Recurso protocolado")
            .And.Contain("<td>Trabalhista</td>").And.Contain($"<td class=\"aj-tabela__data\">{Cnj}</td>")
            .And.Contain("<span class=\"aj-etiqueta aj-etiqueta--andamento\">Em andamento</span>")
            .And.Contain("<span class=\"aj-etiqueta aj-etiqueta--concluida\">Concluída</span>")
            .And.Contain("<td class=\"aj-tabela__data\">—</td>")
            .And.Contain("<span class=\"aj-subtitulo\">Página 1 de 1 · 3 atividades</span>");
        // Só o dia da conclusão, sem a hora.
        tabela.Should().MatchRegex("<td class=\"aj-tabela__data\">\\d{2}/\\d{2}/\\d{4}</td>");

        var cartoes = BlocoDeVisao.De(html, "aj-tablet-mobile");
        cartoes.Should().Contain("<span class=\"aj-cartao__titulo\">Contestação trabalhista</span>")
            .And.Contain("Parecer sem processo").And.Contain("Recurso protocolado")
            .And.Contain(Cnj).And.Contain("Sem processo")
            .And.Contain("<span class=\"aj-etiqueta aj-etiqueta--andamento\">Em andamento</span>")
            .And.MatchRegex("<span class=\"aj-cartao__data\">Concluída em \\d{2}/\\d{2}/\\d{4}</span>")
            .And.NotContain("aj-paginacao");
        Regex.Matches(cartoes, "class=\"aj-painel aj-painel--cartao\"").Should().HaveCount(3);
        Regex.Matches(cartoes, "aj-cartao__rodape").Should().ContainSingle();

        html.Should().NotContain("Descrição da contestação.").And.NotContain("Descrição do parecer.")
            .And.NotContain("Descrição do recurso.").And.NotContain("Pedro").And.NotContain(Direcionamento)
            .And.NotContain("Carregando").And.NotContain("Página não encontrada").And.NotContain("Nenhuma atividade.");
    }

    [Fact]
    public async Task Cliente_sem_atividades_ve_zeros_e_nenhuma_atividade()
    {
        await CriarAndamentoDaMariaAsync();
        var jose = (await _app.ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("José Souza", null, null, new EnderecoEntrada(), null), "111.444.777-35", null, null))).Valor;
        await _app.EmEscopoAsync(_admin, sp =>
            UsuariosDeClienteDeTeste.GravarAsync(sp, jose, "José Usuário", "jose@x.com"));
        var cliente = await _app.ClienteAutenticadoAsync("jose@x.com", UsuariosDeClienteDeTeste.SenhaDaCarla);

        var html = Decodificado(await cliente.GetStringAsync("/andamento"));

        html.Should().Contain("<p class=\"aj-subtitulo\">0 em andamento · 0 concluídas</p>")
            .And.Contain(">Todas · 0</button>").And.Contain(">Em andamento · 0</button>").And.Contain(">Concluídas · 0</button>")
            .And.Contain("<div class=\"aj-estado-lista\"><strong>Nenhuma atividade.</strong></div>");
        html.Should().NotContain("Contestação trabalhista").And.NotContain("Carregando")
            .And.NotContain("<table").And.NotContain("aj-painel--cartao").And.NotContain("aj-paginacao");
    }
```

No `Equipe_em_andamento_ve_nao_encontrada_sem_consultar`, a asserção negativa que procurava `<dt>Em andamento</dt>` passa a procurar o que a tela nova desenha (o teste do cliente, acima, prova que esses trechos aparecem para quem pode ver):

```csharp
            html.Should().Contain("Página não encontrada").And.NotContain("Contestação trabalhista")
                .And.NotContain("em andamento ·").And.NotContain("aj-chip").And.NotContain("Carregando");
```

E dois testes de componente, novos, na mesma classe, para os estados que o banco não produz por encomenda:

```csharp
    [Fact]
    public async Task Andamento_com_falha_na_consulta_mostra_o_erro_e_os_chips_sem_numero()
    {
        var despachante = new DespachanteCombinado().Responde<ListarMeuAndamento, Resultado<AndamentoDoCliente>>(
            Resultado<AndamentoDoCliente>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = Decodificado(await TelaDeTeste.RenderizarComAsync<Andamento>(despachante, new()));

        // Falha não é "nenhuma atividade": o erro ocupa o lugar da lista, com a nova tentativa.
        html.Should().MatchRegex("<div class=\"aj-aviso aj-aviso--erro aj-aviso--com-acoes\"[^>]*role=\"alert\"")
            .And.Contain("Falha de teste.").And.Contain(">Tentar de novo</button>")
            .And.Contain(">Todas</button>").And.Contain(">Em andamento</button>").And.Contain(">Concluídas</button>");
        html.Should().NotContain("Nenhuma atividade.").And.NotContain("Carregando").And.NotContain(" · ")
            .And.NotContain("<table").And.NotContain("aj-painel--cartao");
    }

    [Fact]
    public async Task Andamento_com_acesso_encerrado_nao_mostra_chips_nem_lista()
    {
        var despachante = new DespachanteCombinado().Responde<ListarMeuAndamento, Resultado<AndamentoDoCliente>>(
            Resultado<AndamentoDoCliente>.Falha(TipoErro.AcessoNegado, "Acesso negado."));

        var html = Decodificado(await TelaDeTeste.RenderizarComAsync<Andamento>(despachante, new()));

        html.Should().Contain("Seu acesso foi encerrado. Entre novamente.").And.Contain("aj-aviso--erro");
        html.Should().NotContain("aj-chip").And.NotContain("Tentar de novo").And.NotContain("<table").And.NotContain("Acesso negado.");
    }
```

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~PortalDoClienteWebTests"`
Expected: os dois testes de página do cliente e os dois de componente falham (a tela ainda é a antiga).

- [ ] **Step 2: A tela**

Marcação de `Andamento.razor`:

```razor
@page "/andamento"
@inject IDespachante Despachante
@inject FusoDoEscritorio Fuso

<PageTitle>Andamento</PageTitle>

<CabecalhoDePagina Titulo="Andamento" Subtitulo="@Subtitulo" />

@if (_encerrado)
{
    <Aviso Tipo="TipoDeAviso.Erro">Seu acesso foi encerrado. Entre novamente.</Aviso>
}
else
{
    <div class="aj-chips" role="group" aria-label="Filtrar por situação">
        <button type="button" class="aj-chip" aria-pressed="@Pressionado(null)" @onclick="() => FiltrarAsync(null)">@Chip("Todas", _totais?.Todas)</button>
        <button type="button" class="aj-chip" aria-pressed="@Pressionado(SituacaoDoAndamento.EmAndamento)" @onclick="() => FiltrarAsync(SituacaoDoAndamento.EmAndamento)">@Chip("Em andamento", _totais?.EmAndamento)</button>
        <button type="button" class="aj-chip" aria-pressed="@Pressionado(SituacaoDoAndamento.Concluida)" @onclick="() => FiltrarAsync(SituacaoDoAndamento.Concluida)">@Chip("Concluídas", _totais?.Concluidas)</button>
    </div>

    <EstadoDaLista Erro="@_erro" Carregando="@(_pagina is null)" Vazio="@(_pagina is { Itens.Count: 0 })"
                   TextoDeVazio="Nenhuma atividade." TentarDeNovo="TentarDeNovoAsync">
        @if (_pagina is { } pagina)
        {
            <div class="aj-so-desktop">
                <div class="aj-tabela-caixa">
                    <table class="aj-tabela">
                        <thead><tr><th scope="col">Atividade</th><th scope="col">Área</th><th scope="col">Processo</th><th scope="col">Situação</th><th scope="col">Concluída em</th></tr></thead>
                        <tbody>
                            @* A linha do andamento não tem Id (o cliente não precisa dele): o índice na página é a chave. *@
                            @for (var indice = 0; indice < pagina.Itens.Count; indice++)
                            {
                                var atividade = pagina.Itens[indice];
                                <tr @key="indice">
                                    <td class="aj-tabela__principal">@atividade.Titulo</td>
                                    <td>@Ou(atividade.AreaNome, "—")</td>
                                    <td class="aj-tabela__data">@Ou(atividade.Processo, "—")</td>
                                    <td><span class="@Etiqueta(atividade.Situacao)">@RotulosDoAndamento.Da(atividade.Situacao)</span></td>
                                    <td class="aj-tabela__data">@Fuso.FormatarData(atividade.ConcluidaEm)</td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
                <Paginacao Numero="pagina.Numero" TotalDePaginas="pagina.TotalDePaginas" Total="pagina.Total"
                           Singular="atividade" Plural="atividades" SempreVisivel="true" Ocupado="_carregando" IrPara="IrParaAsync" />
            </div>

            <div class="aj-tablet-mobile">
                <div class="aj-grade">
                    @for (var indice = 0; indice < pagina.Itens.Count; indice++)
                    {
                        var atividade = pagina.Itens[indice];
                        <div class="aj-painel aj-painel--cartao" @key="indice">
                            <span class="aj-cartao__etiquetas"><span class="@Etiqueta(atividade.Situacao)">@RotulosDoAndamento.Da(atividade.Situacao)</span></span>
                            <span class="aj-cartao__titulo">@atividade.Titulo</span>
                            <span class="aj-cartao__meta aj-cartao__meta--tabular">@Ou(atividade.AreaNome, "Sem área")<br />@Ou(atividade.Processo, "Sem processo")</span>
                            @if (atividade.ConcluidaEm is not null)
                            {
                                <span class="aj-cartao__rodape"><span class="aj-cartao__data">@ConcluidaEm(atividade)</span></span>
                            }
                        </div>
                    }
                </div>
                <Paginacao Numero="pagina.Numero" TotalDePaginas="pagina.TotalDePaginas" Total="pagina.Total"
                           Singular="atividade" Plural="atividades" Ocupado="_carregando" IrPara="IrParaAsync" />
            </div>
        }
    </EstadoDaLista>
}
```

No `@code`:

- `Totais` passa a `private readonly record struct Totais(int EmAndamento, int Concluidas) { public int Todas => EmAndamento + Concluidas; }`.
- Sai `ValorDaSituacao` e `MudarSituacaoAsync(ChangeEventArgs)`. Entra:

```csharp
    private string? Subtitulo => _totais is { } totais
        ? $"{totais.EmAndamento} em andamento · {Contagem.De(totais.Concluidas, "concluída", "concluídas")}"
        : null;

    private string Pressionado(SituacaoDoAndamento? situacao) => _situacao == situacao ? "true" : "false";

    private static string Chip(string nome, int? total) => total is { } quantidade ? $"{nome} · {quantidade}" : nome;

    private static string Etiqueta(SituacaoDoAndamento situacao) => situacao == SituacaoDoAndamento.Concluida
        ? "aj-etiqueta aj-etiqueta--concluida"
        : "aj-etiqueta aj-etiqueta--andamento";

    private static string Ou(string? valor, string padrao) => string.IsNullOrWhiteSpace(valor) ? padrao : valor;

    private string ConcluidaEm(AtividadeDoAndamento atividade) => $"Concluída em {Fuso.FormatarData(atividade.ConcluidaEm)}";

    private Task FiltrarAsync(SituacaoDoAndamento? situacao)
    {
        _situacao = situacao;
        return CarregarAsync(1);
    }
```

- `IrParaAsync`, `TentarDeNovoAsync` e `CarregarAsync` não mudam.

Apague `TotaisDoAndamento.razor` e as regras `.totais` do `app.css`, depois de confirmar que nada mais os usa.

- [ ] **Step 3: Trava e testes**

Acrescente `Components/Pages/Andamento.razor` a `Migradas`.

Run: `dotnet build`
Expected: 0 avisos.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~AndamentoTests"`
Expected: todos passando.

- [ ] **Step 4: Commit**

```
git add src tests
git commit -m "feat(web): andamento do cliente no design system, com chips de situação, tabela no desktop e cartões no tablet e no mobile"
```

---

### Task 5: Meus documentos e Documentos comuns do cliente

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Documentos/DocumentosParaBaixar.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/MeusDocumentos.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/DocumentosComuns.razor` (só os ramos que não são da Diretoria)
- Modify: `tests/AtividadesJuridicas.IntegrationTests/DocumentosDoPortalWebTests.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: `CabecalhoDePagina`, `Aviso`, `EstadoDaLista`, `Paginacao`, `Contagem.De`, `Icone` (`baixar`), `BlocoDeVisao.De`; `ListarMeusDocumentos`, `ListarDocumentosComuns` e `DocumentoDoPortal`, que não mudam.
- Produces: `<DocumentosParaBaixar Titulo Subtitulo? Listar OnAcessoEncerrado />` — passa a desenhar o cabeçalho da página. Com `Subtitulo`, ele vale no desktop e no tablet e a contagem vai para o mobile; sem ele, a contagem vale nas três visões. A Tarefa 6 usa o componente como está.

**Regras:**
- Quadros: `MeusDocumentos*.dc.html` e `DocumentosComuns*.dc.html` (os três de cada).
- A contagem é `Contagem.De(total, "documento", "documentos")`, com o total da consulta (todas as páginas). Sem resposta ainda, com falha ou com zero documentos, não há contagem (o estado da lista já diz "Nenhum documento.").
- Desktop: a tabela do quadro, com o nome como link de download e o botão "Baixar" na coluna de ações. Tablet e mobile: um painel com uma linha por documento; o nome não é link, e a ação é o botão de ícone "Baixar {nome}".
- A paginação aparece só com mais de uma página, nas duas formas.
- A lógica de carregar não muda (acesso negado avisa a página; volta à última página; falha não vira lista vazia).
- Em `DocumentosComuns.razor` o ramo da Diretoria fica **como está** nesta tarefa (título, frase e `DocumentosDoCliente`): ele muda na Tarefa 6. Por isso o arquivo só entra em `Migradas` na Tarefa 6.

- [ ] **Step 1: Reescrever os testes do cliente (devem falhar)**

Em `DocumentosDoPortalWebTests`, o teste do cliente passa a:

```csharp
    [Fact]
    public async Task Cliente_ve_documentos_comuns_e_meus_documentos_so_com_download()
    {
        var cliente = await DaCarlaAsync();

        foreach (var (endereco, nome, id, subtitulo) in new[]
                 {
                     ("/documentos-comuns", "comum.pdf", _comum, "Modelos e orientações do escritório para todos os clientes."),
                     ("/meus-documentos", "do-cliente.pdf", _doCliente, "1 documento"),
                 })
        {
            var html = Decodificado(await cliente.GetStringAsync(endereco));

            html.Should().Contain($"<p class=\"aj-subtitulo\">{subtitulo}</p>", endereco)
                // No mobile o subtítulo é sempre a contagem.
                .And.Contain("<span class=\"aj-subtitulo\">1 documento</span>", endereco);

            var tabela = BlocoDeVisao.De(html, "aj-so-desktop");
            tabela.Should().MatchRegex(
                    $"<td class=\"aj-tabela__principal\"><a href=\"/documentos/{id}/baixar\"[^>]*download[^>]*>{Regex.Escape(nome)}</a></td>", endereco)
                .And.MatchRegex(
                    $"<a href=\"/documentos/{id}/baixar\"[^>]*class=\"aj-botao aj-botao--secundario aj-botao--com-icone aj-botao--compacto\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Baixar</a>", endereco)
                .And.Contain("<span class=\"aj-so-leitor\">Ações</span>", endereco)
                .And.NotContain("aj-paginacao", endereco);

            var linhas = BlocoDeVisao.De(html, "aj-tablet-mobile");
            linhas.Should().Contain($"<span class=\"aj-linha__titulo\">{nome}</span>", endereco)
                .And.MatchRegex(
                    $"<a href=\"/documentos/{id}/baixar\"[^>]*class=\"aj-botao aj-botao--secundario aj-botao--icone\"[^>]*aria-label=\"Baixar {Regex.Escape(nome)}\"", endereco)
                .And.MatchRegex("<span class=\"aj-ajuda\">[^<]+ · \\d{2}/\\d{2}/\\d{4} \\d{2}:\\d{2}</span>", endereco);

            // O cliente não gerencia nem vê quem enviou. A Diretoria vê: o teste dela é o controle positivo.
            html.Should().NotContain("Enviar documento").And.NotContain("Renomear").And.NotContain("Excluir")
                .And.NotContain("Ações de").And.NotContain(">Por</th>").And.NotContain("Diana Diretora")
                .And.NotContain("Carregando").And.NotContain("Página não encontrada");
        }
    }
```

E o do cliente sem documentos:

```csharp
        html.Should().Contain("<div class=\"aj-estado-lista\"><strong>Nenhum documento.</strong></div>")
            .And.Contain("<h1 class=\"aj-titulo\">Meus documentos</h1>")
            .And.NotContain("do-cliente.pdf").And.NotContain("Carregando")
            .And.NotContain("<table").And.NotContain("aj-linha").And.NotContain("documentos</p>");
```

E um teste de componente, novo, para a falha de consulta:

```csharp
    [Fact]
    public async Task Lista_de_documentos_com_falha_na_consulta_mostra_o_erro_e_nao_a_lista_vazia()
    {
        Func<int, Task<Resultado<Pagina<DocumentoDoPortal>>>> listar = _ => Task.FromResult(
            Resultado<Pagina<DocumentoDoPortal>>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = Decodificado(await TelaDeTeste.RenderizarComAsync<DocumentosParaBaixar>(
            new DespachanteCombinado(), new() { ["Titulo"] = "Meus documentos", ["Listar"] = listar }));

        html.Should().Contain("<h1 class=\"aj-titulo\">Meus documentos</h1>")
            .And.MatchRegex("<div class=\"aj-aviso aj-aviso--erro aj-aviso--com-acoes\"[^>]*role=\"alert\"")
            .And.Contain("Falha de teste.").And.Contain(">Tentar de novo</button>");
        html.Should().NotContain("Nenhum documento.").And.NotContain("<table").And.NotContain("aj-linha")
            .And.NotContain("aj-subtitulo");
    }
```

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~DocumentosDoPortalWebTests"`
Expected: os dois testes de página do cliente e o de componente falham.

- [ ] **Step 2: O componente**

Marcação de `DocumentosParaBaixar.razor`:

```razor
@inject FusoDoEscritorio Fuso

<CabecalhoDePagina Titulo="@Titulo" Subtitulo="@(Subtitulo ?? Quantos)" SubtituloMobile="@Quantos" />

<EstadoDaLista Erro="@_erroDaLista" Carregando="@(!_carregado)" Vazio="@(_pagina.Itens.Count == 0)"
               TextoDeVazio="Nenhum documento." TentarDeNovo="() => IrParaAsync(_paginaPedida)">
    <div class="aj-so-desktop">
        <div class="aj-tabela-caixa">
            <table class="aj-tabela">
                <thead><tr><th scope="col">Nome</th><th scope="col">Tamanho</th><th scope="col">Enviado em</th><th scope="col"><span class="aj-so-leitor">Ações</span></th></tr></thead>
                <tbody>
                    @foreach (var documento in _pagina.Itens)
                    {
                        <tr @key="documento.Id">
                            @* Link fora da navegação do Blazor: é um download, não uma página. O atributo "download"
                               é o que impede o roteador do Blazor de interceptar o clique; não o remova. *@
                            <td class="aj-tabela__principal"><a href="@Endereco(documento)" download data-enhance-nav="false">@documento.Nome</a></td>
                            <td class="aj-tabela__data">@TamanhoDeArquivo.Formatar(documento.TamanhoBytes)</td>
                            <td class="aj-tabela__data">@Fuso.Formatar(documento.EnviadoEm)</td>
                            <td class="aj-tabela__acao"><a href="@Endereco(documento)" download data-enhance-nav="false" class="aj-botao aj-botao--secundario aj-botao--com-icone aj-botao--compacto"><Icone Nome="baixar" />Baixar</a></td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
        <Paginacao Numero="_pagina.Numero" TotalDePaginas="_pagina.TotalDePaginas" Total="_pagina.Total"
                   Singular="documento" Plural="documentos" IrPara="IrParaAsync" />
    </div>

    <div class="aj-tablet-mobile">
        <section class="aj-painel aj-painel--principal">
            @foreach (var documento in _pagina.Itens)
            {
                <div class="aj-linha" @key="documento.Id">
                    <div class="aj-linha__texto"><span class="aj-linha__titulo">@documento.Nome</span><span class="aj-ajuda">@Detalhe(documento)</span></div>
                    <a href="@Endereco(documento)" download data-enhance-nav="false" class="aj-botao aj-botao--secundario aj-botao--icone" aria-label="@($"Baixar {documento.Nome}")"><Icone Nome="baixar" /></a>
                </div>
            }
        </section>
        <Paginacao Numero="_pagina.Numero" TotalDePaginas="_pagina.TotalDePaginas" Total="_pagina.Total"
                   Singular="documento" Plural="documentos" IrPara="IrParaAsync" />
    </div>
</EstadoDaLista>
```

No `@code`: entra o parâmetro `Subtitulo` (`string?`, "a frase do quadro sob o título, no desktop e no tablet; sem ela, vale a contagem") e:

```csharp
    private string? Quantos => _carregado && _erroDaLista is null && _pagina.Total > 0
        ? Contagem.De(_pagina.Total, "documento", "documentos")
        : null;

    private static string Endereco(DocumentoDoPortal documento) => $"/documentos/{documento.Id}/baixar";

    private string Detalhe(DocumentoDoPortal documento) =>
        $"{TamanhoDeArquivo.Formatar(documento.TamanhoBytes)} · {Fuso.Formatar(documento.EnviadoEm)}";
```

O restante do `@code` não muda.

- [ ] **Step 3: As páginas**

`MeusDocumentos.razor`:

```razor
@page "/meus-documentos"
@inject IDespachante Despachante

<PageTitle>Meus documentos</PageTitle>

@if (_encerrado)
{
    <CabecalhoDePagina Titulo="Meus documentos" />
    <Aviso Tipo="TipoDeAviso.Erro">Seu acesso foi encerrado. Entre novamente.</Aviso>
}
else
{
    <DocumentosParaBaixar Titulo="Meus documentos" Listar="ListarAsync" OnAcessoEncerrado="Encerrar" />
}
```

`DocumentosComuns.razor`, ramos que mudam (o da Diretoria fica intacto):

```razor
@if (!_carregado)
{
    <CabecalhoDePagina Titulo="Documentos comuns" />
    <p class="aj-subtitulo" role="status">Carregando…</p>
}
else if (_ator is null)
{
    <CabecalhoDePagina Titulo="Documentos comuns" />
    <Aviso Tipo="TipoDeAviso.Erro">Seu acesso foi encerrado. Entre novamente.</Aviso>
}
```

e

```razor
else if (_ator.EhCliente)
{
    <DocumentosParaBaixar Titulo="Documentos comuns" Subtitulo="Modelos e orientações do escritório para todos os clientes."
                          Listar="ListarAsync" OnAcessoEncerrado="Encerrar" />
}
```

- [ ] **Step 4: Trava e testes**

Acrescente a `Migradas`: `Components/Documentos/DocumentosParaBaixar.razor` e `Components/Pages/MeusDocumentos.razor`.

Run: `dotnet build`
Expected: 0 avisos.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~DocumentosDoPortalWebTests|FullyQualifiedName~PortalDoClienteWebTests"`
Expected: todos passando. O teste da Diretoria em Documentos comuns continua passando com a tela antiga.

- [ ] **Step 5: Commit**

```
git add src tests
git commit -m "feat(web): meus documentos e documentos comuns do cliente no design system, com tabela no desktop e lista em painel no tablet e no mobile"
```

---

### Task 6: Documentos comuns da Diretoria

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Documentos/GestaoDeDocumentos.cs`
- Modify: `src/AtividadesJuridicas.Web/Components/Documentos/DocumentosDoCliente.razor`
- Create: `src/AtividadesJuridicas.Web/Components/Documentos/DocumentosComunsDaDiretoria.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/DocumentosComuns.razor`
- Create: `tests/AtividadesJuridicas.IntegrationTests/DocumentosDaFichaTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/DocumentosDoPortalWebTests.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: `CabecalhoDePagina` (com `Acao` e `OnAcao`), `Aviso` (com `Acoes`, `Titulo`, `Rotulo` e `FocarAsync`), `EstadoDaLista`, `Paginacao`, `MenuDeAcoes`, `Contagem.De`, `BlocoDeVisao.De`; os casos de uso de documentos, que não mudam.
- Produces:
  - `GestaoDeDocumentos` — classe base abstrata (`ComponentBase`) com todo o estado e as ações que hoje estão no `@code` de `DocumentosDoCliente.razor`. Membros abstratos: `CategoriaDosDocumentos` (`CategoriaDocumento`), `ClienteDosDocumentos` (`Guid?`) e `FocarAvisoDeEnvioAsync()` (`ValueTask`). Gancho virtual `AoConcluirEnvio()`, chamado quando um envio dá certo.
  - `<DocumentosDoCliente ClienteId Categoria Titulo PodeGerenciar OnAcessoEncerrado />` — os mesmos parâmetros e a mesma marcação de hoje.
  - `<DocumentosComunsDaDiretoria OnAcessoEncerrado />` — a tela nova.

**Regras:**
- Quadros: `DocumentosComunsDiretoria.dc.html`, `DocumentosComunsDiretoriaTablet.dc.html`, `DocumentosComunsDiretoriaMobile.dc.html`.
- **Primeiro o teste de caracterização da tela antiga, verde, depois a extração.** A extração não muda comportamento: a marcação de `DocumentosDoCliente.razor` fica igual, salvo o que a herança obrigar (nomes de membros).
- Na base, o que era `private` e é usado pela marcação passa a `protected`. `Categoria` e `ClienteId` continuam parâmetros de `DocumentosDoCliente` (com `EditorRequired` onde já tinham); a base os lê pelos membros abstratos. Se o compilador reclamar de campo `protected` com sublinhado, troque os campos por propriedades de nome próprio e ajuste a marcação antiga; relate.
- A tela nova fixa a categoria em Documento comum, sem cliente, e sempre gerencia. Não tem "Acesso" (que é só do Documento geral).
- Ordem dos blocos da tela nova: cabeçalho; aviso de retorno (sucesso ou erro); aviso de confirmação de envio; aviso de confirmação de exclusão; painel de envio; painel de renomear; lista.
- O painel de envio fica no HTML enquanto estiver aberto, com `hidden` enquanto o aviso de confirmação estiver à vista. Fecha em "Cancelar" e quando um envio dá certo.
- Nos dois avisos de confirmação, "Cancelar" vem antes do verbo, e os dois botões ficam desabilitados enquanto a ação corre.
- No tablet e no mobile, o menu de ações de cada linha traz "Baixar", "Renomear" e "Excluir", nesta ordem, o último em cor de erro.
- O parágrafo "Os arquivos desta área ficam visíveis…" da página sai: o subtítulo do quadro diz o mesmo.

- [ ] **Step 1: Teste de caracterização da tela antiga**

`DocumentosDaFichaTests.cs`:

```csharp
using System.Net;
using System.Text;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Web.Components.Documentos;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// A lista de documentos da ficha do cliente, que só abre por clique e ainda tem a marcação antiga. Fixa o que
/// ela desenha, para a lógica poder mudar de arquivo sem mudar a tela.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public class DocumentosDaFichaTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _maria, _documento;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        (_, _maria) = await _app.CriarCarlaAsync();
        var enviado = await _app.ExecutarAsync(_admin, new EnviarDocumento(
            CategoriaDocumento.DocumentoDoCliente, _maria, null, "procuracao.pdf", "application/pdf",
            new MemoryStream(Encoding.UTF8.GetBytes("conteúdo"))));
        enviado.Sucesso.Should().BeTrue(enviado.Mensagem);
        _documento = enviado.Valor;
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> FichaAsync(bool podeGerenciar) => WebUtility.HtmlDecode(
        await TelaDeTeste.RenderizarAsync<DocumentosDoCliente>(_app, _admin, new()
        {
            ["ClienteId"] = _maria, ["Categoria"] = CategoriaDocumento.DocumentoDoCliente,
            ["Titulo"] = "Documentos do cliente", ["PodeGerenciar"] = podeGerenciar,
        }));

    [Fact]
    public async Task Ficha_lista_o_documento_com_o_autor_as_acoes_e_o_envio()
    {
        var html = await FichaAsync(podeGerenciar: true);

        html.Should().Contain("<h3>Documentos do cliente</h3>")
            .And.Contain($"href=\"/documentos/{_documento}/baixar\"").And.Contain("procuracao.pdf")
            .And.Contain("<th>Por</th>").And.Contain("<td>Diana Diretora</td>")
            .And.Contain(">Renomear</button>").And.Contain(">Excluir</button>")
            .And.Contain("Enviar documento").And.Contain("Arquivo (até 50 MB)").And.Contain("Nome (opcional)");
        // Documento do cliente não pede confirmação de envio nem tem editor de acesso.
        html.Should().NotContain("Confirmar envio").And.NotContain(">Acesso</button>").And.NotContain("Carregando");
    }

    [Fact]
    public async Task Ficha_sem_gestao_so_lista()
    {
        var html = await FichaAsync(podeGerenciar: false);

        html.Should().Contain("procuracao.pdf").And.Contain($"href=\"/documentos/{_documento}/baixar\"");
        html.Should().NotContain("Renomear").And.NotContain("Excluir").And.NotContain("Enviar documento");
    }
}
```

O `InputFile` da tela injeta `IJSRuntime`: é para ele que a Tarefa 2 registrou o `IJSRuntime` de teste em `TelaDeTeste`.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~DocumentosDaFichaTests"`
Expected: 2 passando, **antes** de qualquer mudança em `src`.

- [ ] **Step 2: Extrair `GestaoDeDocumentos`**

Mova o conteúdo do `@code` de `DocumentosDoCliente.razor` para `GestaoDeDocumentos.cs`:

```csharp
namespace AtividadesJuridicas.Web.Components.Documentos;

/// <summary>
/// Estado e ações de uma lista de documentos que a Diretoria gerencia: listar, enviar (com confirmação nos
/// Documentos comuns), renomear, definir quem acessa e excluir. As telas que a herdam só desenham.
/// </summary>
public abstract class GestaoDeDocumentos : ComponentBase
{
    [Inject] protected IDespachante Despachante { get; set; } = default!;

    /// <summary>Avisado quando um caso de uso responde acesso negado: a página decide o que mostrar.</summary>
    [Parameter] public EventCallback OnAcessoEncerrado { get; set; }

    /// <summary>A categoria dos documentos desta tela.</summary>
    protected abstract CategoriaDocumento CategoriaDosDocumentos { get; }

    /// <summary>O cliente dono dos documentos. Nulo só na categoria Documento comum.</summary>
    protected abstract Guid? ClienteDosDocumentos { get; }

    /// <summary>Leva o foco ao aviso de confirmação do envio, que cada tela desenha do seu jeito.</summary>
    protected abstract ValueTask FocarAvisoDeEnvioAsync();

    /// <summary>Um envio acabou de dar certo: a tela fecha o que abriu para ele.</summary>
    protected virtual void AoConcluirEnvio() { }

    // … o estado e os métodos, movidos sem mudança de comportamento …
}
```

Mudanças no que é movido, e só elas:

- Onde o código lia `Categoria`, lê `CategoriaDosDocumentos`; onde lia `ClienteId`, lê `ClienteDosDocumentos`.
- `OnAfterRenderAsync` chama `await FocarAvisoDeEnvioAsync()` no lugar de `_avisoDeEnvio.FocusAsync()`, dentro do mesmo `try`/`catch`.
- Em `EnviarArquivoAsync`, logo depois de limpar o arquivo enviado e antes de recarregar a lista, chame `AoConcluirEnvio()`.
- Visibilidade: `protected` para o que a marcação usa (campos de estado, os registros `Linha` e `OpcaoDeAcesso`, `UmMegabyte`, `DeAtividades` e os métodos chamados por `@onclick`, `OnChange` e `@bind`).

`DocumentosDoCliente.razor` passa a começar por `@inherits GestaoDeDocumentos` e `@inject FusoDoEscritorio Fuso`, mantém a marcação e fica com este `@code`:

```csharp
@code {
    /// <summary>Nulo só na categoria Documento comum, que não é de cliente nenhum.</summary>
    [Parameter] public Guid? ClienteId { get; set; }
    [Parameter, EditorRequired] public CategoriaDocumento Categoria { get; set; }
    [Parameter, EditorRequired] public string Titulo { get; set; } = string.Empty;
    [Parameter] public bool PodeGerenciar { get; set; }

    private ElementReference _avisoDeEnvio;

    protected override CategoriaDocumento CategoriaDosDocumentos => Categoria;
    protected override Guid? ClienteDosDocumentos => ClienteId;
    protected override ValueTask FocarAvisoDeEnvioAsync() => _avisoDeEnvio.FocusAsync();
}
```

Run: `dotnet build`
Expected: 0 avisos.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~DocumentosDaFichaTests|FullyQualifiedName~DocumentosDoPortalWebTests"`
Expected: todos passando, sem tocar nos testes.

Commit desta etapa, sozinha:

```
git add src tests
git commit -m "refactor(web): lógica de gestão de documentos numa classe base, com teste de caracterização da ficha"
```

- [ ] **Step 3: Reescrever o teste da Diretoria (deve falhar)**

Em `DocumentosDoPortalWebTests`:

```csharp
    [Fact]
    public async Task Diretor_ve_documentos_comuns_com_o_envio_e_sem_o_aviso_aberto()
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = Decodificado(await admin.GetStringAsync("/documentos-comuns"));

        html.Should().Contain("<h1 class=\"aj-titulo\">Documentos comuns</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Visíveis para todos os clientes do escritório.</p>")
            .And.Contain("<span class=\"aj-subtitulo\">1 documento</span>")
            .And.MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Enviar documento</button>")
            .And.MatchRegex("<button type=\"button\" class=\"aj-fab\" aria-label=\"Enviar documento\"");

        // Controle positivo do que o cliente não vê: a Diretoria vê a coluna do autor, o nome de quem enviou e as ações.
        var tabela = BlocoDeVisao.De(html, "aj-so-desktop");
        tabela.Should().Contain("<th scope=\"col\">Por</th>").And.Contain("<td>Diana Diretora</td>")
            .And.MatchRegex($"<td class=\"aj-tabela__principal\"><a href=\"/documentos/{_comum}/baixar\"[^>]*download[^>]*>comum\\.pdf</a></td>")
            .And.MatchRegex("<td class=\"aj-tabela__acao aj-tabela__acao--varias\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--menor\"[^>]*>Renomear</button>\\s*<button type=\"button\" class=\"aj-botao aj-botao--perigo aj-botao--menor\"[^>]*>Excluir</button>");

        var linhas = BlocoDeVisao.De(html, "aj-tablet-mobile");
        linhas.Should().Contain("<span class=\"aj-linha__titulo\">comum.pdf</span>")
            .And.MatchRegex("<span class=\"aj-ajuda\">[^<]+ · \\d{2}/\\d{2}/\\d{4} \\d{2}:\\d{2} · Diana Diretora</span>")
            .And.Contain("aria-label=\"Ações de comum.pdf\"");
        // O menu nasce fechado, com as três ações na ordem: baixar, renomear e, por último, excluir.
        Regex.Matches(linhas, "class=\"aj-menu-acoes__item[^\"]*\"[^>]*>([^<]+)<").Select(m => m.Groups[1].Value)
            .Should().Equal("Baixar", "Renomear", "Excluir");
        linhas.Should().MatchRegex("<div class=\"aj-menu-acoes__lista\"[^>]*\\shidden")
            .And.Contain("class=\"aj-menu-acoes__item aj-menu-acoes__item--perigo\"");

        // Nada aberto de início: nem confirmação, nem painel de envio.
        html.Should().NotContain("Confirmar envio").And.NotContain("Arquivo (até").And.NotContain("Confirmar exclusão")
            .And.NotContain("Página não encontrada").And.NotContain("Carregando");
        html.Should().NotContain("do-cliente.pdf");
    }
```

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~DocumentosDoPortalWebTests"`
Expected: este teste falha; os demais passam.

- [ ] **Step 4: A tela nova**

`DocumentosComunsDaDiretoria.razor`:

```razor
@inherits GestaoDeDocumentos
@inject FusoDoEscritorio Fuso

<CabecalhoDePagina Titulo="Documentos comuns" Subtitulo="Visíveis para todos os clientes do escritório."
                   SubtituloMobile="@Quantos" Acao="Enviar documento" OnAcao="AbrirEnvio" />

@if (_mensagem is not null)
{
    <Aviso Tipo="@(_falhou ? TipoDeAviso.Erro : TipoDeAviso.Sucesso)">@_mensagem</Aviso>
}

@if (_confirmacao.AguardandoConfirmacao && _arquivo is { } escolhido)
{
    <Aviso @ref="_avisoDeEnvio" Tipo="TipoDeAviso.Atencao" Rotulo="Confirmar envio" Titulo="@TituloDoEnvio(escolhido)">
        <ChildContent>Documentos desta área ficam visíveis para todos os clientes do escritório. Não envie aqui arquivos de um cliente específico.</ChildContent>
        <Acoes>
            <button type="button" class="aj-botao aj-botao--secundario" disabled="@_enviando" @onclick="CancelarEnvio">Cancelar</button>
            <button type="button" class="aj-botao aj-botao--primario" disabled="@_enviando" @onclick="ConfirmarEnvioAsync">Confirmar envio</button>
        </Acoes>
    </Aviso>
}

@if (AExcluir is { } aExcluir)
{
    <Aviso Tipo="TipoDeAviso.Atencao" Rotulo="Confirmar exclusão" Titulo="@($"Confirmar a exclusão de {aExcluir.Nome}?")">
        <ChildContent>O documento deixa de aparecer para todos os clientes.</ChildContent>
        <Acoes>
            <button type="button" class="aj-botao aj-botao--secundario" disabled="@_removendo" @onclick="CancelarExclusao">Cancelar</button>
            <button type="button" class="aj-botao aj-botao--primario" disabled="@_removendo" @onclick="() => ExcluirAsync(aExcluir.Id)">Confirmar exclusão</button>
        </Acoes>
    </Aviso>
}

@if (_envioAberto)
{
    @* Escondido, não removido, enquanto a confirmação está à vista: o arquivo escolhido mora neste campo. *@
    <section class="aj-painel" hidden="@_confirmacao.AguardandoConfirmacao">
        <h2>Enviar documento</h2>
        <label class="aj-rotulo">@RotuloDoArquivo<InputFile @key="_versaoDoCampo" OnChange="Escolher" class="aj-campo" /></label>
        <label class="aj-rotulo">Nome (opcional)<input type="text" class="aj-campo" @bind="_nomeDoEnvio" /></label>
        <div class="aj-painel__acoes">
            <button type="button" class="aj-botao aj-botao--secundario" disabled="@_enviando" @onclick="FecharEnvio">Cancelar</button>
            <button type="button" class="aj-botao aj-botao--primario" disabled="@(_arquivo is null || _enviando)" @onclick="EnviarAsync">@(_enviando ? "Enviando…" : "Enviar")</button>
        </div>
    </section>
}

@if (_renomeando is { } renomeando)
{
    <section class="aj-painel">
        <h2>@($"Renomear \"{renomeando.Nome}\"")</h2>
        <label class="aj-rotulo">Novo nome<input type="text" class="aj-campo" @bind="_novoNome" /></label>
        <div class="aj-painel__acoes">
            <button type="button" class="aj-botao aj-botao--secundario" @onclick="FecharRenomear">Cancelar</button>
            <button type="button" class="aj-botao aj-botao--primario" @onclick="RenomearAsync">Salvar nome</button>
        </div>
    </section>
}

<EstadoDaLista Erro="@_erroDaLista" Carregando="@(!_carregado)" Vazio="@(_pagina.Itens.Count == 0)"
               TextoDeVazio="Nenhum documento." TentarDeNovo="() => IrParaAsync(_paginaPedida)">
    <div class="aj-so-desktop">
        <div class="aj-tabela-caixa">
            <table class="aj-tabela">
                <thead><tr><th scope="col">Nome</th><th scope="col">Tamanho</th><th scope="col">Enviado em</th><th scope="col">Por</th><th scope="col"><span class="aj-so-leitor">Ações</span></th></tr></thead>
                <tbody>
                    @foreach (var documento in _pagina.Itens)
                    {
                        <tr @key="documento.Id">
                            @* Link fora da navegação do Blazor: é um download, não uma página. O atributo "download"
                               é o que impede o roteador do Blazor de interceptar o clique; não o remova. *@
                            <td class="aj-tabela__principal"><a href="@Endereco(documento)" download data-enhance-nav="false">@documento.Nome</a></td>
                            <td class="aj-tabela__data">@TamanhoDeArquivo.Formatar(documento.TamanhoBytes)</td>
                            <td class="aj-tabela__data">@Fuso.Formatar(documento.EnviadoEm)</td>
                            <td>@documento.EnviadoPor</td>
                            <td class="aj-tabela__acao aj-tabela__acao--varias">
                                <button type="button" class="aj-botao aj-botao--fantasma aj-botao--menor" @onclick="() => AbrirRenomear(documento)">Renomear</button>
                                <button type="button" class="aj-botao aj-botao--perigo aj-botao--menor" @onclick="() => PedirExclusao(documento.Id)">Excluir</button>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
        <Paginacao Numero="_pagina.Numero" TotalDePaginas="_pagina.TotalDePaginas" Total="_pagina.Total"
                   Singular="documento" Plural="documentos" IrPara="IrParaAsync" />
    </div>

    <div class="aj-tablet-mobile">
        <section class="aj-painel aj-painel--principal">
            @foreach (var documento in _pagina.Itens)
            {
                <div class="aj-linha" @key="documento.Id">
                    <div class="aj-linha__texto"><span class="aj-linha__titulo">@documento.Nome</span><span class="aj-ajuda">@Detalhe(documento)</span></div>
                    <MenuDeAcoes Rotulo="@($"Ações de {documento.Nome}")">
                        <a href="@Endereco(documento)" download data-enhance-nav="false" class="aj-menu-acoes__item">Baixar</a>
                        <button type="button" class="aj-menu-acoes__item" @onclick="() => AbrirRenomear(documento)">Renomear</button>
                        <button type="button" class="aj-menu-acoes__item aj-menu-acoes__item--perigo" @onclick="() => PedirExclusao(documento.Id)">Excluir</button>
                    </MenuDeAcoes>
                </div>
            }
        </section>
        <Paginacao Numero="_pagina.Numero" TotalDePaginas="_pagina.TotalDePaginas" Total="_pagina.Total"
                   Singular="documento" Plural="documentos" IrPara="IrParaAsync" />
    </div>
</EstadoDaLista>

@code {
    private bool _envioAberto;
    private Aviso? _avisoDeEnvio;

    protected override CategoriaDocumento CategoriaDosDocumentos => CategoriaDocumento.DocumentoComum;
    protected override Guid? ClienteDosDocumentos => null;

    protected override ValueTask FocarAvisoDeEnvioAsync() =>
        _avisoDeEnvio is { } aviso ? aviso.FocarAsync() : ValueTask.CompletedTask;

    protected override void AoConcluirEnvio() => _envioAberto = false;

    private string RotuloDoArquivo => $"Arquivo (até {DocumentosOpcoes.TamanhoMaximoBytes / UmMegabyte} MB)";

    private string? Quantos => _carregado && _erroDaLista is null && _pagina.Total > 0
        ? Contagem.De(_pagina.Total, "documento", "documentos")
        : null;

    private Linha? AExcluir => _pagina.Itens.FirstOrDefault(documento => documento.Id == _excluindo);

    private void AbrirEnvio()
    {
        _mensagem = null;
        _renomeando = null;
        _envioAberto = true;
    }

    // O arquivo escolhido não é guardado: reabrir o painel começa do zero.
    private void FecharEnvio()
    {
        _confirmacao.Cancelar();
        _arquivo = null;
        _nomeDoEnvio = null;
        _versaoDoCampo++;
        _envioAberto = false;
    }

    private string TituloDoEnvio(IBrowserFile arquivo) =>
        $"{(string.IsNullOrWhiteSpace(_nomeDoEnvio) ? arquivo.Name : _nomeDoEnvio.Trim())} · {TamanhoDeArquivo.Formatar(arquivo.Size)}";

    private static string Endereco(Linha documento) => $"/documentos/{documento.Id}/baixar";

    private string Detalhe(Linha documento) => string.Join(" · ", new[]
    {
        TamanhoDeArquivo.Formatar(documento.TamanhoBytes), Fuso.Formatar(documento.EnviadoEm), documento.EnviadoPor,
    }.Where(parte => !string.IsNullOrWhiteSpace(parte)));
}
```

Os nomes `_mensagem`, `_pagina` e os demais são os da base; se a extração os tiver renomeado (regra do sublinhado), use os nomes novos.

Em `DocumentosComuns.razor`, o ramo da Diretoria passa a:

```razor
else if (_ator.EhDiretoria)
{
    <DocumentosComunsDaDiretoria OnAcessoEncerrado="Encerrar" />
}
```

- [ ] **Step 5: Trava e testes**

Acrescente a `Migradas`: `Components/Documentos/DocumentosComunsDaDiretoria.razor` e `Components/Pages/DocumentosComuns.razor`.

Run: `dotnet build`
Expected: 0 avisos.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "FullyQualifiedName~DocumentosDoPortalWebTests|FullyQualifiedName~DocumentosDaFichaTests|FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~LayoutWebTests"`
Expected: todos passando.

- [ ] **Step 6: Commit**

```
git add src tests
git commit -m "feat(web): documentos comuns da Diretoria no design system, com envio pelo cabeçalho, confirmação no topo e menu de ações no tablet e no mobile"
```

---

### Task 7: Suíte inteira e conferência no navegador

Feita pela sessão que coordena.

**Files:**
- Modify: `docs/superpowers/specs/2026-10-10-migracao-para-o-design-system-design.md` (o que a execução ensinou e muda a spec)

**Regras:**
- A conferência usa o ambiente descartável: banco em contêiner na porta 5599, aplicação publicada em Release rodando em `http://localhost:5197`. **O ambiente sobe depois que a suíte terminar e é derrubado antes de qualquer nova rodada da suíte.**
- As três larguras são vistas em molduras da mesma origem (1440×900, 834×1194, 390×844), com as esperas dos roteiros num Worker, como registrado na seção 11 da spec. Cada tela é medida contra o quadro; o roteiro devolve só o que diverge.

- [ ] **Step 1: Suíte inteira**

Run: `dotnet build` e depois `dotnet test --no-build --logger "trx;LogFilePrefix=fase-9b" --results-directory <scratchpad>\trx-9b`, em segundo plano.
Expected: sem avisos; os totais da linha de base mais os testes novos, nenhum falhando.

- [ ] **Step 2: Subir o ambiente e criar os dados**

Pelo sistema: uma usuária de Produção, um cliente, um usuário de cliente, uma área, um processo e atividades do cliente em andamento e concluídas (com e sem processo), em quantidade para duas páginas de andamento; documentos do cliente e documentos comuns, um deles com nome de mais de 80 caracteres.

- [ ] **Step 3: Conferir cada tela nas três larguras, contra o quadro**

| Tela | Rota | Perfis e estados |
|---|---|---|
| Andamento | `/andamento` | cliente; cada chip; segunda página; cliente sem atividades |
| Meus documentos | `/meus-documentos` | cliente; cliente sem documentos |
| Documentos comuns | `/documentos-comuns` | cliente; Diretoria; Produção (não encontrada) |
| Envio | `/documentos-comuns` | Diretoria: abrir o painel, escolher arquivo, confirmar, ver o aviso de sucesso e o documento na lista; cancelar na confirmação e no painel |
| Renomear e excluir | `/documentos-comuns` | Diretoria, pela tabela (desktop) e pelo menu de ações (tablet e mobile) |

Para cada uma: estrutura, textos, medidas dos blocos, cores, ícones e o que está aceso na navegação.

- [ ] **Step 4: Conferir o que o Review Focus pede**

- Enviar um arquivo de verdade pela tela nova, com o aviso de confirmação no meio, e baixá-lo depois como cliente: o conteúdo é o mesmo.
- Nome de arquivo longo: quebra na linha do painel em 390px e rola dentro da caixa da tabela em 1440px; a página não rola para o lado.
- Menu de ações, com teclado de verdade numa moldura de 390px: fechado, Tab não entra nos itens; aberto, Tab percorre "Baixar", "Renomear", "Excluir"; Esc fecha e o foco volta ao botão; clique fora fecha.
- Botão flutuante "Enviar documento" no mobile: 58px, acima da barra inferior, e o conteúdo tem o respiro do quadro.
- Ficha do cliente, ainda não migrada (`/clientes`, abrir a ficha): enviar, renomear, definir acesso de um documento geral e excluir continuam funcionando.
- Larguras de 760, 761, 1199 e 1200px em `/andamento` e em `/documentos-comuns`: uma só forma da lista à vista (tabela ou painel), um só título.
- Telas ainda não migradas (`/atividades`, `/clientes`, `/agenda`, `/usuarios`): aparência igual à da `main`.
- Regras de elemento do `app.css` que alcançam o que esta fase passou a usar (spec, seção 15): medir, contra os quadros, o `h2` e os `label` do painel de envio, o `strong` do aviso, o campo de arquivo e os links dentro da tabela. O que vazar do `app.css` é restringido lá às telas antigas ou ganha a declaração que falta no design system.

- [ ] **Step 5: Corrigir o que divergir**

Divergência de CSS se corrige no design system (Tarefa 1, de novo: publicar e copiar), nunca no `app.css` nem com `style`. Divergência de marcação se corrige na tela, com o teste correspondente ajustado antes.

- [ ] **Step 6: Derrubar o ambiente, atualizar a spec e fechar**

Remova o contêiner e a pasta publicada. Rode a suíte inteira de novo se a conferência pediu correção. Emende a spec com o que a execução ensinou (seção 15 ganha as anotações da 9B). Commit:

```
git add docs
git commit -m "docs(spec): o que a fase 9B ensinou sobre a migração para o design system"
```

Mostre ao usuário as capturas das telas nas três larguras e peça o aval para a entrada na `main`.
