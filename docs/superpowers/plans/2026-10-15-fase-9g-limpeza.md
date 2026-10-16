# Fase 9G — Limpeza final e revisão de teclado: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** O `app.css` fica só com a base, todas as telas passam a funcionar bem pelo teclado e pelo leitor de tela nas três visões, e os itens que as fases 9A a 9F deixaram para a 9G são resolvidos ou decididos.

**Architecture:** O que as classes `aj-` ainda pegavam emprestado do `app.css` vai ao design system (Tarefa 1) e as regras de elemento saem, com uma trava que lê o `app.css` (Tarefa 2). Os problemas de teclado e de leitor de tela se resolvem nas peças compartilhadas, uma vez para todas as telas: o módulo de foco do layout ganha uma rede de segurança e a rolagem zerada por página (Tarefa 3); `MenuDeAcoes`, `Abas`, o layout (menu "Mais", "Exibir/Esconder menu", botão flutuante) e um componente de painel de filtros resolvem cada um o seu grupo (Tarefas 4 a 7); os documentos, as confirmações, os erros de formulário e os itens pontuais das listas e da agenda fecham o resto (Tarefas 8 a 10). A fase termina com a revisão final de todas as rotas no navegador (Tarefa 11).

**Tech Stack:** .NET 10, Blazor Interactive Server, CSS do design system (tokens e classes `aj-`), JavaScript de módulo (`FocoNoTitulo.razor.js`), xUnit 2.9, FluentAssertions 7.2, Testcontainers, bUnit 2.11.3.

**Spec:** `docs/superpowers/specs/2026-10-10-migracao-para-o-design-system-design.md` (seções 1, 4.1, 4.5, 11, 12 e as notas "Para a 9G" e "Para as próximas fases" da seção 15).

## Decisões deste plano

Os levantamentos que este plano cita estão em `C:\Users\Gabriel Paulino\AppData\Local\Temp\claude\c--GitHub-SIstemaControleAtividadesJuridicas\73464877-64cd-41cc-b340-808de5c66612\scratchpad\plano-9g\`: `itens-9g.md` (os 83 itens da spec e 6 achados, cada um com a situação no código, evidência arquivo:linha, correção provável e teste; os grupos G1–G14; as dúvidas D1–D20) e `app-css-9g.md` (cada regra do `app.css`, o que alcança, o que o design system já cobre e o efeito medido de tirá-la). Os IDs abaixo (A2, B6, F15…) são os desses arquivos.

- **Base do CSS (D1):** fica no `app.css` (spec 4.1): `*,*::before,*::after{box-sizing:border-box}`, `body{margin:0;background;color;font}`, `:focus-visible{…}` e `h1:focus{outline:none}`. Saem `html,body{height:100%}`, os dois `:where(a)` e todas as regras de elemento das linhas 15–32 (`h1`–`h3`, `p`, `button:disabled`, `label`, `.aj-campo-icone input:focus…`, `input.invalid…`, `input[type=checkbox/radio]`, `label:has(> input…)`, `fieldset`, `legend`, `td code`). O que as classes ainda pegam delas vai antes ao design system (Tarefa 1): `.aj-segmentado button:disabled` e `.aj-resultados__item:disabled` (opacidade e cursor, e o hover sem pintar, como o `.aj-menu-acoes__item:disabled` da 9B), e o `margin:0` das caixas escondidas do ladrilho e do chip de pessoa (as visíveis já têm tamanho, margem e cor pelo `.aj-opcao input`). A faixa de erro já é `aj-faixa-erro` do design system. O `ReconnectModal.razor.css` ganha `box-sizing:border-box` no `dialog`, para não depender da base.
- **Remendos do design system que existiam por causa do `app.css`** saem na Tarefa 1: o `letter-spacing:normal` de `.aj-barra-superior--voltar>h1`, o `gap:0` de `.aj-campo-filtro`, o `word-break:normal` de `.aj-dados` e a regra `.aj-opcao.aj-opcao--filtro`. Os seletores de duas classes do ladrilho e do chip de pessoa ficam (com uma classe só, quem decide passa a ser a ordem do arquivo, com variantes por consulta e por `data-visao`: reescrita com risco e sem ganho); só os comentários que citam "a folha da aplicação" mudam. O `margin:0` e o `font` dos `h2` com classe ficam (corrigem o padrão do navegador).
- **Consultas de mídia sem buraco (A14, D3):** as 64 consultas do `bundle.css` passam de `max-width:1199px`/`760px` para `max-width:1199.98px`/`760.98px` (e os pares `min-width:761px`/`1200px` ficam), sem a sintaxe de intervalo (que pediria Safari 16.4). O navegador mínimo (o do `:has()`: Chrome 105, Safari 15.4, Firefox 121) fica registrado no `responsividade.md` do design system.
- **Nome longo no bloco do usuário (A1, D2):** uma linha com reticências, e o nome inteiro no `title` do bloco (o texto continua no DOM para o leitor).
- **Menu "Mais" (A9, D4):** o "Mais" deixa de ter `aria-current` só por estar aberto: o estado aberto é o `aria-expanded`, e o design system ganha o gancho de estilo `[aria-expanded="true"]` no "Mais" do trilho e da barra inferior (aceso como o quadro mostra). Os destinos mantêm o `aria-current` da rota; enquanto o menu está aberto, o design system não os pinta de ativos (o quadro `MaisTablet`/`MaisMobile` mostra só o "Mais" aceso).
- **Menu aberto e os três `nav` (A2, A11, X6, D5):** com o menu aberto no tablet e no mobile, o resto da coluna (trilho, conteúdo, barra inferior) fica `inert`; o foco vai ao primeiro item do menu ao abrir; Esc fecha; ao fechar, o foco volta ao "Mais". Com o `inert`, só um `nav aria-label="Principal"` fica acessível de cada vez, e os nomes não mudam.
- **"Exibir menu" e "Esconder menu" (A10):** depois do clique, o foco vai ao par visível (o módulo de foco ganha `focarPrimeiroVisivel(seletor)`).
- **Piscar ao conectar (A13, D7):** só o layout: o ator (`UsuarioAutenticado`) passa da pré-renderização ao circuito pelo estado persistente do .NET 10, para o menu, o nome, o avatar e o "Mais" já saírem certos na primeira renderização interativa. O "Carregando…" das páginas fica como está.
- **Rolagem (A12, D6):** o `main.aj-app__conteudo` volta ao topo quando muda a página (o modelo de rota), não quando só a consulta muda (troca de período da agenda, `?produtor=`, paginação).
- **Rede de segurança de foco (G4, D19):** o módulo de foco do layout observa quando o elemento focado sai do DOM depois de uma renderização (o foco cai no `body`) e leva o foco ao título visível (`h1`). Os casos com destino melhor continuam corrigidos por tela (painéis, confirmações, menus, filtros).
- **Destino do foco ao fechar painéis e confirmações (B6, X2, D8):** Cancelar devolve o foco a quem abriu (o botão "Enviar documento", o "Renomear"/"⋯" da linha, o botão da ação que pediu a confirmação). Depois de concluir (enviar, salvar o nome, excluir, inativar), o foco vai ao aviso de resultado, que ganha o modo `Revelar` (foco com `tabindex="-1"` e rolagem até ele ao aparecer).
- **Erro de gravação fora da vista (C5):** o `Aviso` de erro dos oito formulários usa o mesmo `Revelar`.
- **Painel de filtros (G5):** um componente compartilhado `PainelDeFiltros` (o `form` do painel, sempre no DOM com `hidden` quando fechado, para o `aria-controls` valer; Esc fecha; foco no primeiro campo ao abrir; foco de volta ao botão que abriu ao fechar por Esc ou por "Filtrar"), usado por Agenda, Atividades e Auditoria.
- **Filtro aplicado em Atividades e Auditoria (F7, D9):** as duas passam à regra da agenda (Ruling 14 da 9F): a consulta lê o filtro gravado no "Filtrar"; a troca de página e de chip usa o aplicado; fechar o painel sem "Filtrar" volta os campos ao aplicado.
- **Botão flutuante depois do conteúdo (F15, D11):** nas seis telas com ação no mobile, o `aj-fab` passa a vir depois do conteúdo na ordem do documento (um encaixe `rodape-do-conteudo` no fim do `main`, preenchido pelo `CabecalhoDePagina`); a posição fixa na tela não muda.
- **Agenda (F8, F9, F11, F13, D10):** a troca de período mantém o foco na seta e anuncia o período novo numa região viva só para leitor (`role="status"`); o `+N` leva o foco ao título do Dia; a grade compacta do Mês fica à vista durante a carga do mês novo; o grupo das setas e do "Hoje" ganha `role="group"` com `aria-label="Período"`.
- **Abas (G2, D13):** `tablist` com `tabindex` móvel (só a escolhida com `0`), ←/→ com volta nas pontas, Home e End, e `aria-controls` para o painel (que ganha `id`); os painéis não ganham `role="tabpanel"`, porque no tablet e no desktop aparecem sem abas.
- **Menu de ações (G1, D13):** continua "disclosure" (botão com `aria-expanded` e lista de botões, sem `role="menu"` nem setas); fecha com Esc com o foco em qualquer ponto dele e pelo fundo, devolvendo o foco ao botão; fecha quando o foco sai dele (Tab, Shift+Tab, clique fora), deixando o foco onde o usuário o levou; nunca fica mais de um aberto.
- **Limpezas de código que entram (D16):** F1 (código só dos testes), F2, F3, F4 (redações e duplicação da agenda), F6 (trava recursiva única), C3, C4 (Auditoria), C7, C8 (Usuários), E4 (`CargaDoFormulario` e `GestaoDeDocumentos`), E10 (barra do revisor), B9 (paginação do Andamento), B10 (documentos), D2 (erro da busca de cliente do processo), F14 e X5 (`aria-invalid` nos grupos). Ficam fora: C1 (o trio de recarga, sem quarto uso), D3 (formulários pela rota, nenhum link leva ao caso), D5 e E3 (cobertura de caminhos de erro), F5 (invólucros duplicados da agenda, sem efeito), E2 (conflito que pede recarregar).
- **Fora da 9G (D17):** a tarefa de segurança do login e de Minha senha (spec 15, 9A: "Fora da migração").
- **Decisões de execução pendentes do usuário (D15):** não mudam nesta fase; a emenda da spec (Tarefa 11) junta a lista das fases 9B a 9F num lugar só, para o usuário rever.
- **Teste da regra interina (D14):** `LayoutWebTests` (toda tela abre o conteúdo com um bloco `aj-`) fica: protege a margem do `main`.
- **WebKit (C6, D12):** a Tarefa 11 tenta o Playwright WebKit para o Enter pelo botão de envio escondido; se não houver como, fica registrado como não conferido.
- **Publicação do design system (D18):** uma versão na Tarefa 1, com tudo o que as Tarefas 1 a 10 pedem de CSS (este plano já o lista), e outra só se a conferência da Tarefa 11 pedir. A cópia do canvas é sincronizada a cada publicação.

## Como ler este plano

Cada tarefa traz as **regras** (o contrato), as **interfaces** que ela consome e produz e os **testes obrigatórios**. O plano traz o código onde ele é a própria fidelidade ou onde o detalhe é sutil; o restante é escrito no estilo do que já existe. O código do plano não foi executado: quando o Blazor, o bUnit ou o compilador exigirem, ajuste o mínimo para preservar o comportamento e registre o desvio no relatório, com a mensagem de erro que o motivou.

Nunca apague um teste nem afrouxe uma asserção. Quando uma asserção mudar porque a marcação mudou, reescreva-a mantendo o que ela prova e liste-a no relatório. Asserção negativa (`NotContain`) ganha uma afirmação positiva ao lado. Asserção sobre o bloco de uma visão (`BlocoDeVisao.De`) sempre tem uma afirmação positiva sobre o mesmo bloco.

Foco: o bUnit prova que o foco foi **pedido** (`JSInterop.VerifyFocusAsyncInvoke()` ou a chamada do módulo de foco), não que chegou; ele não aplica CSS, consultas de mídia nem ordem de tabulação, e o `FocusEventArgs` não traz o `relatedTarget`. O que depende disso fica para o navegador (Tarefa 11) e é dito em cada tarefa.

Lição da 9F (busca de processo): um formulário sem botão de envio visível com mais de um campo que bloqueia o envio implícito não é enviado pelo Enter, mesmo que uma das cópias esteja escondida por CSS. Nenhum `form="…"` compartilhado entre cópias de visões.

## Estado de partida

- Worktree `.claude/worktrees/feat+fase-9g-limpeza`, branch `worktree-feat+fase-9g-limpeza`, na `main` local (`8d5dd3a`), que tem as fases 9A a 9F. Todas as telas estão migradas.
- Canvas: https://claude.ai/artifact/HzHWv1pgVXAnYHuPEYQDFj (versão `1792101725-a2bb`). Design system: https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS, versão `1792101692-3ba2`; o `componentes.css` do código é cópia dela.
- `app.css` com 32 linhas: a base (6–13) e as regras de elemento (15–32). Lista e efeitos no `app-css-9g.md`.
- Testes: `TelaDeTeste`, `TelaInterativa` (bUnit, `AtorDaDiretoria`, `AtorDaProducao`), `DespachanteCombinado`, `ComBarraSuperior<T>`, `BlocoDeVisao.De`; trava `UnitTests/TelasNoDesignSystemTests.cs` com `Migradas` e as travas por pasta das atividades e da agenda. Interativos de base no anexo C do `itens-9g.md`.
- Linha de base: a suíte inteira passou em `a1968ac` (a árvore da `main` de hoje, salvo a spec): unidade 910, integração 1245.
- `git` só pela ferramenta PowerShell. Antes de cada commit, `git branch --show-current`.
- A máquina tem caído ao começar build ou teste: build sempre `dotnet build -m:1`; teste sempre `--no-build` depois de um build feito; um comando por vez. Depois de uma queda que pegou build, confira as DLLs (`[Reflection.AssemblyName]::GetAssemblyName`) antes de confiar no build incremental. A suíte inteira roda em lotes (`suite-em-lotes.ps1`, em `C:\Users\Gabriel Paulino\.claude\projects\c--GitHub-SIstemaControleAtividadesJuridicas\conferencia-de-telas\`).

## Global Constraints

- TFM único `net10.0`. `Nullable` e `TreatWarningsAsErrors=true`. Nenhum pacote novo.
- Código, nomes e textos em português, com acentuação correta. Arquivos em UTF-8 sem BOM, com o fim de linha que o arquivo já tem.
- A camada Web só chega aos dados pelo `IDespachante` (`WebSoPeloDespachanteTests`). Nenhuma consulta nova no backend.
- `wwwroot/css/tokens.css` e `wwwroot/css/componentes.css` não são editados à mão: mudam no artifact do design system e são copiados de lá.
- Tela não tem atributo `style`, nem `<style>`, nem arquivo `.razor.css` (salvo `ReconnectModal.razor.css`), nem classe que não comece por `aj-` (salvo as que o framework exige, só em `App.razor` e `Layout/ReconnectModal.razor`: a lista fechada `TelasNoDesignSystemTests.ClassesDoFramework` — `reload`, `dismiss`, as `components-reconnect-*`, `components-pause-visible` e `components-resume-failed-visible`), nem `<button>`, `<table>`, `<input>`, `<select>` ou `<textarea>` sem `class`. Nenhum CSS novo no `app.css`.
- As classes de visibilidade (`aj-so-desktop`, `aj-so-tablet`, `aj-so-mobile`, `aj-desktop-tablet`, `aj-tablet-mobile`, `aj-desktop-mobile`) vão num invólucro próprio cuja classe é só a de visibilidade.
- Visões: mobile até 760px, tablet de 761px a 1199px, desktop a partir de 1200px (nas consultas de mídia: `max-width:760.98px`, `min-width:761px`, `max-width:1199.98px`, `min-width:1200px`).
- Todo texto que um teste procura no HTML é escrito por uma única expressão Razor ou por marcação fixa.
- Rotas não mudam. Nenhum texto visível muda, salvo os novos desta fase listados abaixo.
- Botão só de ícone leva `aria-label` com o verbo e o objeto.
- Chaves `@key` de dois tipos de linha no mesmo pai nunca coincidem.
- Foco: nenhuma ação deixa o foco no `body` quando a tela continua aberta; quem fecha um painel, um menu ou uma confirmação devolve o foco a quem o abriu, salvo quando a ação concluiu (então o aviso de resultado recebe o foco).
- Textos novos desta fase: `Período` (`aria-label` do grupo das setas e do "Hoje" da agenda); o anúncio da troca de período da agenda é o próprio subtítulo do período (`Semana de 05/10 a 11/10/2026`, `Quinta-feira, 08/10/2026`, `Outubro de 2026`), sem texto novo. Nenhum outro.

## Review Focus

Condições que a spec implica e que mais provavelmente atingem quem usa o sistema pelo teclado ou pelo leitor de tela. Cada uma tem o seu teste na tarefa indicada.

1. **Foco que cai no `body`:** depois de Cancelar, Salvar nome, Confirmar envio, Excluir, Inativar, Filtrar, "Tentar de novo", a troca de página do Andamento e o `+N` da agenda, o foco fica num elemento à vista (quem abriu, o aviso de resultado ou o título). Testes nas Tarefas 3, 7, 8, 9 e 10.
2. **Menu "Mais" no tablet e no mobile:** abrir leva o foco para dentro; Tab não sai para a tela de trás; Esc fecha e devolve o foco ao "Mais"; o leitor ouve um só `nav` "Principal". Testes na Tarefa 6.
3. **`app.css` só com a base:** nenhuma tela muda de aparência sem as regras de elemento (o segmento travado do tipo de cliente e os resultados de busca durante o envio continuam esmaecidos; as caixas de marcar continuam com 18px e a cor verde). Testes nas Tarefas 1 e 2 (trava) e medida na Tarefa 11.
4. **Largura fracionária:** a 1199,5px e a 760,5px (zoom), uma das visões vale (nenhuma tela fica sem layout). Teste na Tarefa 1 (roteiro sobre o `bundle.css`) e na Tarefa 11 (moldura fracionária).
5. **Filtro aplicado em Atividades e Auditoria:** um filtro escolhido no painel e fechado sem "Filtrar" não vale na troca de página nem de chip; ao reabrir, o campo mostra o aplicado. Testes na Tarefa 7.

---

### Task 1: Design system — o que as classes pegavam do `app.css`, consultas sem buraco, "Mais" aberto, nome longo

Feita pela sessão que coordena: publica no artifact do usuário.

**Files:**
- Artifact do design system: `project/components/bundle.css`, `project/responsividade.md` (navegador mínimo e as consultas), `project/design-system.json` (`lastChange`); README (e prévia, onde há estado novo) de Segmentado, Vínculo, Campo, Navegação, Chip e Escolha em ladrilhos
- Artifact do canvas: `project/ds/araujods/components/bundle.css` e o registro `designSystems` do `canvas.json`
- Modify: `src/AtividadesJuridicas.Web/wwwroot/css/componentes.css`

**Regras:**
- Acrescentar: `.aj-segmentado button:disabled` e `.aj-resultados__item:disabled` (`opacity:.55;cursor:not-allowed`, e o `:disabled:hover` sem pintar); `margin:0` nas caixas escondidas `.aj-escolha__opcao input` e `.aj-chip--pessoa input`; o gancho do "Mais" aberto (`[aria-expanded="true"]` no item do trilho e da barra inferior, com o mesmo desenho do ativo) e, com o menu aberto (`.aj-app--menu-aberto`), os destinos com `aria-current` sem o desenho de ativo; `.aj-usuario__texto b` numa linha com reticências.
- Tirar os remendos: `letter-spacing:normal` de `.aj-barra-superior--voltar>h1`, `gap:0` de `.aj-campo-filtro`, `word-break:normal` de `.aj-dados`, a regra `.aj-opcao.aj-opcao--filtro{min-height:var(--campo-filtro)}`; reescrever os dois comentários que citam "a folha da aplicação" (ladrilho e chip). Nenhum valor desenhado muda.
- Reescrever as consultas de mídia (só dentro de `@media`): `max-width:1199px` → `max-width:1199.98px`, `max-width:760px` → `max-width:760.98px`, inclusive dentro de pares com `min-width`. O `max-width:760px` dos painéis de formulário (fora de `@media`) não muda.
- Nenhum outro valor muda. Um roteiro de conferência compara o `bundle.css` antes e depois e lista exatamente as regras mudadas.
- `responsividade.md`: as faixas escritas com as fronteiras fracionárias e o navegador mínimo (o do `:has()`: Chrome e Edge 105, Safari 15.4, Firefox 121).

**Testes obrigatórios:** o roteiro de conferência (as mudanças esperadas, nenhuma outra); a cópia para o `componentes.css` só com elas; o build.

- [ ] **Step 1: Conferir o acesso e as versões**

A ferramenta Artifact abre o design system (https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS) e o canvas (https://claude.ai/artifact/HzHWv1pgVXAnYHuPEYQDFj). Se responder "artifact not found" (ou `list` vier vazio), o Claude Code está conectado a outra conta: pare e peça ao usuário para entrar com a conta dona dos artifacts. Se a publicação for negada pelo classificador ("Modify Shared Resources"), não contorne: avise o usuário antes de seguir.

Ação `list` com `scope: "files"` e depois `read` do design system e do canvas (o artifact inteiro, sem `path`; sem essa leitura nesta sessão a publicação é recusada). O design system está na versão `1792101692-3ba2` e o canvas na `1792101725-a2bb` (a versão também muda quando o tipo do artifact ganha release nova, sem mudar o conteúdo: compare o conteúdo, não o número). Leia com `path`, salvando no scratchpad: `project/components/bundle.css`, `project/design-system.json`, `project/responsividade.md` e os README de `Segmentado`, `Vinculo`, `Navegacao`, `PainelDeFiltros`, `Abas` e o que descreve `aj-menu-acoes` (na listagem; se não houver pasta própria, é o de `Botao`). Compare o `bundle.css` com o `src/AtividadesJuridicas.Web/wwwroot/css/componentes.css` do worktree, da linha "Visões:" em diante (o `bundle.css` tem fim de linha LF, o `componentes.css` CRLF): têm de ser idênticos. Se diferirem, alguém editou o design system: siga sobre a versão nova e confira que o roteiro do passo 2 continua achando cada trecho uma única vez.

- [ ] **Step 2: Aplicar as mudanças ao `bundle.css`**

Grave no scratchpad, em UTF-8 sem BOM, o roteiro `aplicar_9g.py` abaixo e rode `python -I aplicar_9g.py <bundle.css lido> <bundle-9g.css>` (caminhos longos do scratchpad). Ele faz as trocas exatas dos remendos e dos comentários (cada trecho tem de existir uma única vez), reescreve as fronteiras só dentro de `@media`, acrescenta o bloco da fase no fim e confere que nenhum nome de classe sumiu nem surgiu.

```python
# Aplica ao components/bundle.css (LF, UTF-8 sem BOM) as mudanças da fase 9G e lista o que mudou.
# Uso: python -I aplicar_9g.py <bundle.css de entrada> <bundle.css de saída>
import re, sys

entrada, saida = sys.argv[1], sys.argv[2]
css = open(entrada, encoding='utf-8', newline='').read()
if '\r\n' in css:
    css = css.replace('\r\n', '\n')

# 1. Remendos que existiam por causa do app.css, e os comentários que o citam (cada trecho existe uma única vez).
TROCAS = [
    ('font:700 17px/1.3 var(--font-sans);letter-spacing:normal;text-align:center}',
     'font:700 17px/1.3 var(--font-sans);text-align:center}'),
    ('.aj-campo-filtro{display:flex;flex-direction:column;justify-content:center;gap:0;min-width:150px;',
     '.aj-campo-filtro{display:flex;flex-direction:column;justify-content:center;min-width:150px;'),
    ('color:var(--tinta-secundaria);word-break:normal;overflow-wrap:anywhere}',
     'color:var(--tinta-secundaria);overflow-wrap:anywhere}'),
    ('.aj-opcao.aj-opcao--filtro{min-height:var(--campo-filtro)}\n', ''),
    ('arquivo quebram em vez de empurrar a coluna; a opção marcável da barra de filtros mantém a altura do filtro; nos\n',
     'arquivo quebram em vez de empurrar a coluna; nos\n'),
    ('   O ladrilho leva o seletor com o pai (0,2,0) para vencer a regra de rótulo com rádio da folha da aplicação. */',
     '   O ladrilho leva o seletor com o pai (0,2,0), o mesmo das variantes por consulta e por visão abaixo. */'),
    ('O chip leva as duas classes no seletor (0,2,0) para vencer a regra de rótulo com caixa da folha da\n   aplicação. A opção travada',
     'O chip leva as duas classes no seletor (0,2,0), como as variantes por consulta e por visão abaixo. A\n   opção travada'),
]
for antes, depois in TROCAS:
    n = css.count(antes)
    if n != 1:
        sys.exit(f'Trecho encontrado {n} vezes (esperado 1): {antes[:70]!r}')
    css = css.replace(antes, depois)
print('Trocas exatas:', len(TROCAS))

# 2. Consultas de mídia sem buraco entre as visões: só dentro de @media.
mudadas = 0
def fronteira(m):
    global mudadas
    texto = m.group(0)
    novo = texto.replace('max-width:1199px', 'max-width:1199.98px').replace('max-width:760px', 'max-width:760.98px')
    if novo != texto:
        mudadas += 1
    return novo
css = re.sub(r'@media[^{]*', fronteira, css)
medias = re.findall(r'@media[^{]*', css)
print('Consultas de mídia:', len(medias), '| reescritas:', mudadas)
print('Sobram max-width inteiros em @media:', sum(('max-width:1199px' in m) or ('max-width:760px' in m) for m in medias))
print('max-width:760px fora de @media (painéis, não mudam):', css.count('max-width:760px'))

# 3. Bloco novo, no fim.
BLOCO = '''
/* ---------- Limpeza final (fase 9G) ---------- */
/* Opções travadas: esmaecidas, sem cursor de clique e sem o fundo do hover, como o item travado do menu de ações (o
   tipo de cliente na edição; os resultados da busca durante o envio). A opção escolhida travada mantém o verde. */
.aj-segmentado button:disabled{opacity:.55;cursor:not-allowed}
.aj-segmentado button:disabled:not([aria-pressed="true"]):hover{background:transparent}
.aj-resultados__item:disabled{opacity:.55;cursor:not-allowed}
.aj-resultados__item:disabled:hover{background:transparent}
/* A caixa escondida do ladrilho e do chip de pessoa sem a margem do navegador. */
.aj-escolha__opcao input,.aj-chip--pessoa input{margin:0}
/* "Mais" com o menu aberto (tablet e mobile): aceso pelo aria-expanded, com o desenho do destino atual; o destino da
   rota mantém o aria-current, mas não aparece aceso enquanto o menu está aberto. */
.aj-trilho__item[aria-expanded="true"]{background:var(--menu-ativo);color:var(--sobre-verde);font-weight:700}
.aj-trilho__item[aria-expanded="true"] svg{stroke:var(--dourado)}
.aj-nav-inferior__item[aria-expanded="true"]{color:var(--verde-escuro);font-weight:700}
.aj-nav-inferior__item[aria-expanded="true"]>span{background:var(--selecao-fundo)}
.aj-app--menu-aberto .aj-trilho__item[aria-current="page"]:not([aria-expanded="true"]){background:transparent;color:var(--menu-texto);font-weight:600}
.aj-app--menu-aberto .aj-trilho__item[aria-current="page"]:not([aria-expanded="true"]) svg{stroke:currentColor}
.aj-app--menu-aberto .aj-nav-inferior__item[aria-current="page"]:not([aria-expanded="true"]){color:var(--tinta-suave);font-weight:600}
.aj-app--menu-aberto .aj-nav-inferior__item[aria-current="page"]:not([aria-expanded="true"])>span{background:transparent}
/* Nome longo no bloco do usuário: uma linha, com reticências (o nome inteiro vai no title do bloco). */
.aj-usuario__texto b{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
/* Paginação: o botão sem ação leva aria-disabled (o foco continua nele durante a carga e na última página), com o
   mesmo desenho do desabilitado também na paginação com setas. */
.aj-paginacao--com-setas .aj-botao[aria-disabled="true"]{opacity:1;border-color:var(--borda-opcao);background:var(--superficie-campo);color:var(--tinta-apagada)}
/* O título do Dia recebe o foco por programa (o "+N" do Mês), como o título da página: sem contorno. */
.aj-dia__cabeca:focus,.aj-secao-dia__cabeca:focus{outline:none}
'''
if '(fase 9G)' in css:
    sys.exit('O bloco da fase 9G já está no arquivo.')
css = css.rstrip('\n') + '\n' + BLOCO
print('Linhas do bloco novo:', BLOCO.strip('\n').count('\n') + 1)

open(saida, 'w', encoding='utf-8', newline='').write(css)

# 4. Conferência: nenhum nome de classe sumiu; o bloco não cria nome novo.
nomes = lambda t: set(re.findall(r'\.(aj-[a-z0-9_-]+)', t))
antes_nomes = nomes(open(entrada, encoding='utf-8').read())
depois_nomes = nomes(css)
print('Nomes que sumiram:', sorted(antes_nomes - depois_nomes))
print('Nomes novos:', sorted(depois_nomes - antes_nomes))
```

Expected (conferido sobre a cópia do `componentes.css` de `8d5dd3a`):

```
Trocas exatas: 7
Consultas de mídia: 64 | reescritas: 54
Sobram max-width inteiros em @media: 0
max-width:760px fora de @media (painéis, não mudam): 2
Linhas do bloco novo: 26
Nomes que sumiram: []
Nomes novos: []
```

A paginação com setas sem ação por `aria-disabled` repete a regra `.aj-paginacao--com-setas .aj-botao:disabled` (linha 793); o `.aj-botao[aria-disabled="true"]` geral já existe (linha 344). Os valores do bloco repetem os do destino atual (`.aj-trilho__item[aria-current="page"]`, linha 109 do `componentes.css`; `.aj-nav-inferior__item[aria-current="page"]` e o `>span`, linhas 121 e 122; o ícone dourado, linha 95) e os de repouso (`.aj-trilho__item`, linha 107: `menu-texto` e 600; `.aj-nav-inferior__item`, linha 119: `tinta-suave` e 600; os ícones com `stroke:currentColor`, linha 93). Antes de publicar, confira esses valores no `bundle.css` lido; se algum mudou, ajuste o bloco para o valor de lá.

- [ ] **Step 3: README, responsividade e índice**

Sobre os arquivos lidos no passo 1 (UTF-8 sem BOM, LF):

- `Segmentado/README.md`: na lista de estados, "Opção travada (`disabled`): esmaecida, sem cursor de clique e sem o fundo do hover; a escolhida travada continua verde (o tipo de cliente na edição)."
- `Vinculo/README.md`: "Durante o envio, os itens de resultado ficam desabilitados: esmaecidos, sem cursor de clique e sem o fundo do hover."
- `Navegacao/README.md`: a frase "\"Mais\" leva `aria-current=\"page\"` quando a tela atual está fora da barra daquela visão, e enquanto o menu está aberto." passa a "\"Mais\" leva `aria-current=\"page\"` quando a tela atual está fora da barra daquela visão. Com o menu aberto, \"Mais\" acende pelo `aria-expanded=\"true\"` e o destino atual mantém o `aria-current` (o leitor de tela continua ouvindo a página atual), mas não aparece aceso." No item "Aberto por \"Mais\"": "Enquanto o menu está aberto, o resto da tela (trilho, conteúdo e barra inferior) fica `inert`; o foco vai ao primeiro item do menu; Esc, o véu e o \"Fechar menu\" fecham, e o foco volta ao \"Mais\"." No bloco do usuário: "O nome fica numa linha, com reticências; quem usa põe o nome inteiro no `title` do bloco." Se a prévia mostra o menu aberto com o "Mais" aceso por `aria-current`, troque para `aria-expanded="true"`.
- `PainelDeFiltros/README.md`: "O painel fica no documento quando fechado, com `hidden` (o `aria-controls` do botão aponta para ele). Ao abrir, o foco vai ao primeiro campo; Esc fecha; ao fechar por Esc ou por \"Filtrar\", o foco volta ao botão que abriu. Os campos do painel valem só depois de \"Filtrar\": fechar sem filtrar volta os campos ao filtro aplicado."
- `Abas/README.md`: "Teclado: só a aba escolhida está na ordem de tabulação (`tabindex=\"0\"`; as outras `-1`); ← e → passam à anterior e à seguinte, com volta nas pontas, e escolhem; Home e End vão à primeira e à última. Cada aba leva `aria-controls` com o `id` do painel que mostra; o painel não leva `role=\"tabpanel\"`, porque no tablet e no desktop aparece sem abas."
- README do Menu de ações: "Teclado: Esc fecha com o foco no botão ou na lista; Tab depois do último item e Shift+Tab antes do primeiro fecham; abrir um menu fecha o que estiver aberto; ao fechar por Esc, por Tab ou pelo fundo, o foco volta ao botão. É um botão que mostra uma lista de botões (`aria-expanded`), sem `role=\"menu\"` nem setas."
- `responsividade.md`: as faixas passam a "mobile até 760,98px, tablet de 761px a 1199,98px, desktop a partir de 1200px", com uma frase: "As consultas de mídia usam `max-width:760.98px` e `max-width:1199.98px` (e `min-width:761px` e `min-width:1200px`): a largura da janela pode ser fracionária com zoom ou escala de tela, e nenhuma largura fica sem visão. Navegador mínimo: o do `:has()` (Chrome e Edge 105, Safari 15.4, Firefox 121)."
- `design-system.json`: `lastChange` com `by` o usuário, `via` "Claude Code", `at` agora, `note` "Fase 9G: opções travadas do segmentado e da busca esmaecidas, \"Mais\" aceso pelo aria-expanded, nome do usuário com reticências, consultas de mídia sem buraco entre as visões, remendos da folha antiga retirados; teclado do menu de ações, das abas e do painel de filtros documentado.". Nada mais muda.

- [ ] **Step 4: Publicar no design system e sincronizar o canvas**

Uma publicação no design system (`url` https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS) com os arquivos alterados em `files`: `project/components/bundle.css` (o `bundle-9g.css`), `project/responsividade.md`, os README do passo 3 (e a prévia de `Navegacao`, se mudou) e, por último, `project/design-system.json` relido logo antes. `root` e `file_path` com o caminho longo do scratchpad (`C:\Users\Gabriel Paulino\AppData\Local\Temp\claude\…`; o atalho `GABRIE~1` é recusado). Anote a versão publicada no ledger.

Depois, no canvas (https://claude.ai/artifact/HzHWv1pgVXAnYHuPEYQDFj): `read` do artifact inteiro e uma publicação com `project/canvas.json` (no registro `designSystems`, só `version` — a versão publicada do design system — e `copiedAt` — agora — mudam) e, em `files`, a cópia no servidor `"project/ds/araujods/components/bundle.css": {"artifact": "https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS", "path": "project/components/bundle.css"}`. Anote a versão do canvas no ledger.

- [ ] **Step 5: Copiar para o código**

`wwwroot/css/componentes.css` = as duas primeiras linhas de cabeçalho que o arquivo já tem + o `bundle.css` publicado, da linha "Visões:" em diante, com fim de linha CRLF e sem BOM. Baixe o `bundle.css` publicado (`read` com `path` `project/components/bundle.css`) e rode o roteiro abaixo (salvo em UTF-8 **com** BOM, porque o Windows PowerShell 5.1 lê arquivo sem BOM como ANSI), com `-Bundle` no arquivo baixado e `-Codigo` e `-Saida` apontando para o mesmo `componentes.css` do worktree:

```powershell
param([string]$Bundle, [string]$Codigo, [string]$Saida)
# componentes.css = as duas primeiras linhas do arquivo de hoje + o bundle.css da linha "Visões:" em diante, CRLF, sem BOM.
$utf8 = New-Object Text.UTF8Encoding $false
$cabecalho = ([IO.File]::ReadAllText($Codigo, $utf8) -replace "`r`n", "`n").Split("`n")[0..1]
$linhas = ([IO.File]::ReadAllText($Bundle, $utf8) -replace "`r`n", "`n").Split("`n")
$inicio = -1
for ($i = 0; $i -lt $linhas.Length; $i++) { if ($linhas[$i].TrimStart().StartsWith('Visões:')) { $inicio = $i; break } }
if ($inicio -lt 0) { throw 'Linha "Visões:" não encontrada no bundle.css.' }
$corpo = $linhas[$inicio..($linhas.Length - 1)]
[IO.File]::WriteAllText($Saida, (($cabecalho + $corpo) -join "`r`n"), $utf8)
# Conferência: o corpo copiado é idêntico ao do bundle.css.
$copiado = ([IO.File]::ReadAllText($Saida, $utf8) -replace "`r`n", "`n").Split("`n")
$igual = (($copiado[2..($copiado.Length - 1)]) -join "`n") -ceq ($corpo -join "`n")
'Corpo idêntico ao bundle.css: ' + $igual
'Linhas: ' + $copiado.Length + '; CRLF: ' + ([regex]::Matches([IO.File]::ReadAllText($Saida, $utf8), "`r`n")).Count + '; BOM: ' + ([IO.File]::ReadAllBytes($Saida)[0] -eq 0xEF)
```

Expected: `Corpo idêntico ao bundle.css: True`, `Linhas: 1548; CRLF: 1547; BOM: False` (de 1522 para 1548 elementos contados; o último é o vazio depois do CRLF final). Depois:

Run: `git diff --stat -- src/AtividadesJuridicas.Web/wwwroot/css/componentes.css`
Expected: `1 file changed, 88 insertions(+), 62 deletions(-)` (54 consultas, 3 regras e 4 linhas de comentário reescritas, 1 regra tirada; a linha em branco e as 26 do bloco). Se a versão do design system mudou desde a 9F (passo 1), o número muda junto com o que mudou lá.

`tokens.css` não muda nesta fase.

- [ ] **Step 6: Build e commit**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Antes do commit, `git branch --show-current` (tem de ser `worktree-feat+fase-9g-limpeza`). Pela ferramenta PowerShell, com a mensagem num arquivo sem BOM:

```powershell
git add src/AtividadesJuridicas.Web/wwwroot/css/componentes.css
$msg = @'
style(web): design system sem os remendos da folha antiga, opções travadas esmaecidas, "Mais" aceso pelo aria-expanded, nome do usuário com reticências e consultas de mídia sem buraco entre as visões

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
'@
$f = Join-Path $env:TEMP 'mensagem-9g-tarefa-1.txt'
[IO.File]::WriteAllText($f, $msg, (New-Object Text.UTF8Encoding $false))
git commit -F $f
```

### Task 2: `app.css` só com a base, e as travas

**Files:**
- Modify: `src/AtividadesJuridicas.Web/wwwroot/app.css`, `src/AtividadesJuridicas.Web/Components/App.razor` (só o comentário sobre as folhas), `src/AtividadesJuridicas.Web/Components/Layout/ReconnectModal.razor.css` (`box-sizing:border-box` no `#components-reconnect-modal`, para não depender da base), os comentários e mensagens que citam o `app.css` em `TelasNoDesignSystemTests.cs` e `LayoutWebTests.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`
- Test: `tests/AtividadesJuridicas.UnitTests/AppCssTests.cs` (novo)

**Interfaces:**
- Consumes: o `componentes.css` da Tarefa 1.

**Regras:**
- O `app.css` fica só com a base da seção "Decisões" (box-sizing, `body`, `:focus-visible`, `h1:focus`), com um comentário curto no topo dizendo que é a base e que tudo o mais vem do design system.
- `AppCssTests`: lê o `app.css` e só aceita os seletores da base (lista fechada); qualquer outro seletor reprova.
- `TelasNoDesignSystemTests`: as travas por pasta (atividades, agenda) dão lugar a uma única e recursiva: todo `Components/**/*.razor` está em `Migradas` ou numa lista de infraestrutura explícita (`App.razor`, `Routes.razor`, `_Imports.razor`, `Layout/FocoNoTitulo.razor`, `Layout/LayoutVazio.razor`, `Layout/ReconnectModal.razor`); os que podem entrar em `Migradas` (os que não têm classe de framework) entram; a regra de classe da trava aceita as classes do framework só nos arquivos de infraestrutura.

**Testes obrigatórios:** `AppCssTests` falhando antes (as regras de elemento) e passando depois; a trava recursiva falhando com um `.razor` fora das duas listas (teste da própria trava); os testes de tela existentes sem mudança.

- [ ] **Step 1: Teste do `app.css` só com a base (deve falhar)**

Crie `tests/AtividadesJuridicas.UnitTests/AppCssTests.cs` (UTF-8 sem BOM, fim de linha CRLF, como os outros arquivos do projeto). O teste lê o arquivo pelo mesmo caminho que a trava das telas usa para ler os `.razor` (`GravacoesPorForaTests.RaizDoRepositorio()` + `src/AtividadesJuridicas.Web/...`):

```csharp
using System.Text.RegularExpressions;

namespace AtividadesJuridicas.UnitTests;

/// <summary>
/// O <c>app.css</c> é só a base da página: a caixa de borda, o corpo, o contorno de foco e o título focado por roteiro sem
/// contorno. Tudo o mais vem das classes <c>aj-</c> do design system (<c>css/componentes.css</c>); uma regra nova aqui
/// voltaria a vestir as telas por fora dele.
/// </summary>
public partial class AppCssTests
{
    // Os seletores de cada regra da base, na ordem do arquivo. Lista fechada: qualquer outro seletor reprova.
    private static readonly string[] Base = ["*, *::before, *::after", "body", ":focus-visible", "h1:focus"];

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex Comentario();

    // Uma regra simples: o seletor e um bloco sem chave dentro.
    [GeneratedRegex(@"([^{}]+)\{[^{}]*\}")]
    private static partial Regex Regra();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacos();

    /// <summary>
    /// Os seletores de cada regra (com os espaços normalizados) e o que sobrou fora delas: um bloco <c>@media</c>, um
    /// <c>@supports</c>, uma regra aninhada ou uma chave solta sobram e reprovam.
    /// </summary>
    private static (List<string> Seletores, string Sobra) Regras(string css)
    {
        var semComentarios = Comentario().Replace(css, " ");
        var seletores = Regra().Matches(semComentarios)
            .Select(regra => Espacos().Replace(regra.Groups[1].Value, " ").Trim())
            .ToList();
        return (seletores, Regra().Replace(semComentarios, "").Trim());
    }

    private static string Caminho(params string[] partes) =>
        Path.Combine([GravacoesPorForaTests.RaizDoRepositorio(), "src", "AtividadesJuridicas.Web", .. partes]);

    [Fact]
    public void App_css_so_tem_a_base()
    {
        var (seletores, sobra) = Regras(File.ReadAllText(Caminho("wwwroot", "app.css")));

        seletores.Should().Equal(Base,
            "o app.css é só a base da página (caixa, corpo e foco); o que uma tela precisa vem das classes aj- do design system");
        sobra.Should().BeEmpty("o app.css não tem bloco fora das regras da base (@media, @supports, regra aninhada)");
    }

    // O aviso de reconexão não tem classe aj- e mede a largura com borda e respiro: não pode depender da caixa de borda
    // da base para caber na tela do celular.
    [Fact]
    public void Aviso_de_reconexao_traz_a_propria_caixa_de_borda()
    {
        var css = File.ReadAllText(Caminho("Components", "Layout", "ReconnectModal.razor.css"));

        css.Should().MatchRegex(@"#components-reconnect-modal\s*\{[^}]*box-sizing:\s*border-box;[^}]*width:\s*min\(22rem");
    }

    // A própria trava: o que ela lê de cada regra e o que ela deixa sobrar.
    [Fact]
    public void A_trava_le_os_seletores_sem_os_comentarios()
    {
        var (seletores, sobra) = Regras("/* h2 { margin: 0; } */\n*,\n*::before { box-sizing: border-box; }\nh1:focus { outline: none; }");

        seletores.Should().Equal("*, *::before", "h1:focus");
        sobra.Should().BeEmpty();
    }

    [Fact]
    public void A_trava_reprova_bloco_fora_das_regras()
    {
        var (seletores, sobra) = Regras("body { margin: 0; }\n@media (max-width: 760.98px) { p { margin: 0; } }");

        seletores.Should().Equal("body", "p");
        sobra.Should().StartWith("@media");
    }
}
```

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~AppCssTests"`
Expected: os dois testes da própria trava (`A_trava_le_os_seletores_sem_os_comentarios`, `A_trava_reprova_bloco_fora_das_regras`) passam; `App_css_so_tem_a_base` falha, com a lista de hoje — `*, *::before, *::after`, `html, body`, `body`, `:where(a)`, `:where(a:hover)`, `:focus-visible`, `h1:focus`, `h1`, `h2`, `h3`, `p`, `button:disabled`, `label`, `.aj-campo-icone input:focus, .aj-campo-filtro :focus`, `input.invalid, select.invalid, textarea.invalid`, `input[type="checkbox"], input[type="radio"]`, `label:has(> input[type="checkbox"]), label:has(> input[type="radio"])`, `fieldset`, `legend`, `td code` — contra os quatro da base; `Aviso_de_reconexao_traz_a_propria_caixa_de_borda` falha (o `#components-reconnect-modal` ainda não tem `box-sizing`). Se a lista de seletores for outra, o `app.css` mudou depois do levantamento: pare e relate.

- [ ] **Step 2: Conferir que a Tarefa 1 já está no `componentes.css`**

O `app.css` só perde as regras de elemento depois que o design system cobre o que as classes ainda pegavam delas. Com a ferramenta Grep em `src/AtividadesJuridicas.Web/wwwroot/css/componentes.css`, procure:

1. `\.aj-segmentado button:disabled` — esperado: uma regra com `opacity:.55` e `cursor:not-allowed`;
2. `\.aj-resultados__item:disabled` — esperado: uma regra com `opacity:.55` e `cursor:not-allowed`;
3. `aj-escolha__opcao input` e `aj-chip--pessoa input` — esperado: as caixas escondidas do ladrilho e do chip com `margin:0`;
4. `\.aj-opcao input` — esperado: `width:18px;height:18px;margin:0` e `accent-color` (as caixas visíveis; já existe desde a 9D).

Se faltar qualquer um, a Tarefa 1 não foi copiada: pare e relate (não apague as regras do `app.css` sem isso — o segmento travado do tipo de cliente e os resultados da busca durante o envio deixariam de ficar esmaecidos).

- [ ] **Step 3: O `app.css` só com a base, o aviso de reconexão com a própria caixa e o comentário do `App.razor`**

Reescreva `src/AtividadesJuridicas.Web/wwwroot/app.css` (UTF-8 sem BOM, fim de linha CRLF, como hoje) exatamente assim. As quatro regras da base ficam sem mudar um caractere; saem `html, body { height: 100%; }`, os dois `:where(a)` e tudo das linhas 15–32 (conferido no `app-css-9g.md`: o `body`, o `.aj-app`, o `.aj-entrar` e o `.aj-estado--pagina` medem pela janela; os quatro links sem classe estão dentro de `.aj-tabela` ou são o logotipo; as regras de elemento ou são cobertas pelas classes `aj-` ou, nos dois `:disabled` e nas caixas escondidas, pela Tarefa 1).

```css
/* Base da página, sob o design system (css/tokens.css e css/componentes.css): caixa de borda, corpo e foco.
   Tudo o mais vem das classes aj- do design system; nenhuma regra nova entra aqui (AppCssTests). */

*, *::before, *::after { box-sizing: border-box; }
body { margin: 0; background: var(--fundo); color: var(--tinta); font: 400 15px/1.5 var(--font-sans); }
:focus-visible { outline: 2px solid var(--verde); outline-offset: 2px; }
/* O título recebe o foco a cada navegação (FocoNoTitulo) só para ser anunciado; não é um controle. */
h1:focus { outline: none; }
```

Em `src/AtividadesJuridicas.Web/Components/Layout/ReconnectModal.razor.css` (CRLF), o `dialog` passa a declarar a própria caixa de borda (hoje ele depende do `*{box-sizing}` da base: sem ela cresce 50px e, a 360px, passa da tela). Troque

```css
#components-reconnect-modal {
    width: min(22rem, calc(100vw - 2rem));
```

por

```css
#components-reconnect-modal {
    box-sizing: border-box;
    width: min(22rem, calc(100vw - 2rem));
```

Nada mais muda nesse arquivo.

Em `src/AtividadesJuridicas.Web/Components/App.razor` (CRLF), troque o comentário das folhas de estilo

```razor
    @* Design system: tokens, depois componentes; app.css traz a base da aplicação (e as regras de elemento que saem na 9G). *@
```

por

```razor
    @* Design system: tokens, depois componentes; app.css traz só a base da página (caixa de borda, corpo e foco). *@
```

A faixa de erro do Blazor (`#blazor-error-ui`, com `reload` e `dismiss`) não muda: o estilo dela é o `aj-faixa-erro` do `componentes.css`.

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~AppCssTests"`
Expected: os quatro passando.

- [ ] **Step 4: Comentários e mensagens que citam o `app.css` nos testes**

Em `tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs` (CRLF), o teste `Toda_tela_da_equipe_abre_o_conteudo_com_um_bloco_do_design_system` fica (protege a margem do conteúdo: decisão D14); mudam só o comentário e a razão da asserção, que citam a regra interina do `app.css`, que já não existe. Troque

```csharp
    // A regra interina do app.css tratava como "tela ainda não migrada" o conteúdo sem um bloco do design system no
    // topo. Ela saiu na 9F: toda tela da equipe abre o conteúdo com um bloco aj-, e nenhuma depende mais dela.
```

por

```csharp
    // Toda tela da equipe abre o conteúdo com um bloco do design system (um filho direto do main com classe aj-): é ele
    // que dá ao conteúdo a margem e o ritmo do quadro. Uma tela que abrisse com marcação solta ficaria sem eles.
```

e troque

```csharp
        primeiro.Should().MatchRegex("^<[a-z]+\\b[^>]*\\sclass=\"aj-",
            "a regra interina do app.css alcançava o conteúdo sem um filho direto cuja classe começa por aj-");
```

por

```csharp
        primeiro.Should().MatchRegex("^<[a-z]+\\b[^>]*\\sclass=\"aj-",
            "o conteúdo abre com um bloco do design system, que dá a margem e o ritmo do quadro");
```

A expressão regular e as rotas não mudam. Os comentários de `TelasNoDesignSystemTests.cs` mudam no passo seguinte, com o arquivo inteiro.

- [ ] **Step 5: Trava recursiva única, infraestrutura e o teste da própria trava (deve falhar)**

Reescreva `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (UTF-8 sem BOM, CRLF) inteiro assim. O que muda:

- as travas por pasta `Toda_tela_de_atividade_esta_na_trava` e `Toda_tela_da_agenda_esta_na_trava` dão lugar a `Todo_razor_da_web_esta_na_trava` (recursiva, `SearchOption.AllDirectories`), que mantém as asserções positivas das duas (os arquivos-sentinela de cada pasta) — liste a troca no relatório;
- a lista `Infraestrutura` (`App.razor` e `ReconnectModal.razor`) e o teste dela, que aceita as classes do framework (`ClassesDoFramework`, lista fechada) só nesses dois arquivos e o `.razor.css` só no `ReconnectModal`;
- `Violacoes` ganha o parâmetro opcional das classes aceitas fora do `aj-` (sem ele, nenhuma — as telas continuam com a regra de hoje);
- os comentários deixam de citar o `app.css` (a razão para exigir classe passa a ser "o navegador os desenharia com o estilo padrão"), e a expressão `ElementoQueOAppCssVeste` passa a se chamar `ElementoQueExigeClasse`;
- os testes da própria trava que já existem ficam sem mudança, e entram os novos: um `.razor` fora das duas listas reprova; classe do framework reprova fora da infraestrutura e passa nela; outra classe fora do `aj-` reprova também na infraestrutura.

`Migradas` ainda **sem** os quatro arquivos de infraestrutura que não usam classe de framework (entram no passo 6):

```csharp
using System.Text.RegularExpressions;

namespace AtividadesJuridicas.UnitTests;

/// <summary>
/// Tela migrada para o design system não volta atrás. A trava recusa estilo embutido (atributo <c>style</c> e bloco
/// <c>&lt;style&gt;</c>), arquivo <c>.razor.css</c> ao lado da tela, <c>button</c>, <c>table</c>, <c>input</c>,
/// <c>select</c> e <c>textarea</c> sem classe (o navegador os desenharia com o estilo padrão) e classe fora do prefixo
/// <c>aj-</c>, em <c>class=</c> e em <c>Classe=</c> de componente, separada por qualquer espaço em branco. Todo
/// <c>.razor</c> da Web, em qualquer pasta, está em <see cref="Migradas"/> ou em <see cref="Infraestrutura"/>; só a
/// infraestrutura usa as classes que o framework exige (<see cref="ClassesDoFramework"/>) e só o aviso de reconexão tem
/// <c>.razor.css</c>.
/// Pontos cegos, conferidos na revisão: classe montada por expressão Razor (com <c>@</c>) e marcação dentro de
/// comentário <c>@* *@</c>.
/// </summary>
public partial class TelasNoDesignSystemTests
{
    // Caminhos a partir de src/AtividadesJuridicas.Web.
    private static readonly string[] Migradas =
    [
        "Components/Agenda/AvisoDeEventoSalvo.razor",
        "Components/Agenda/CartaoDeEvento.razor",
        "Components/Agenda/DiaDaAgenda.razor",
        "Components/Agenda/FaixaDeDias.razor",
        "Components/Agenda/GradeCompactaDoMes.razor",
        "Components/Agenda/ListaDoDia.razor",
        "Components/Agenda/MesDaAgenda.razor",
        "Components/Agenda/PontosDoDia.razor",
        "Components/Agenda/SecaoDoDia.razor",
        "Components/Agenda/SemanaDaAgenda.razor",
        "Components/Atividades/CabecalhoDaAtividade.razor",
        "Components/Atividades/ComentariosDaAtividade.razor",
        "Components/Atividades/DocumentosDaAtividade.razor",
        "Components/Atividades/EtiquetaDeSituacao.razor",
        "Components/Atividades/MotivoComResponsaveis.razor",
        "Components/Documentos/DocumentosComunsDaDiretoria.razor",
        "Components/Documentos/DocumentosDaFicha.razor",
        "Components/Documentos/DocumentosParaBaixar.razor",
        "Components/Layout/Abas.razor",
        "Components/Layout/Aviso.razor",
        "Components/Layout/BarraDeVoltar.razor",
        "Components/Layout/CabecalhoDeDetalhe.razor",
        "Components/Layout/CabecalhoDeFicha.razor",
        "Components/Layout/CabecalhoDePagina.razor",
        "Components/Layout/CargaDoFormulario.razor",
        "Components/Layout/CartaoAtalho.razor",
        "Components/Layout/EstadoDaLista.razor",
        "Components/Layout/Icone.razor",
        "Components/Layout/MainLayout.razor",
        "Components/Layout/MenuDeAcoes.razor",
        "Components/Layout/Paginacao.razor",
        "Components/Layout/RotuloDeCampo.razor",
        "Components/Pages/Agenda.razor",
        "Components/Pages/Andamento.razor",
        "Components/Pages/Areas.razor",
        "Components/Pages/Atividades.razor",
        "Components/Pages/AtividadeFicha.razor",
        "Components/Pages/AtividadeFormulario.razor",
        "Components/Pages/AtividadeDevolver.razor",
        "Components/Pages/AtividadeReabrir.razor",
        "Components/Pages/AtividadeResponsaveis.razor",
        "Components/Pages/Auditoria.razor",
        "Components/Pages/Clientes.razor",
        "Components/Pages/ClienteFicha.razor",
        "Components/Pages/ClienteFormulario.razor",
        "Components/Pages/Conta/Entrar.razor",
        "Components/Pages/DocumentosComuns.razor",
        "Components/Pages/Erro.razor",
        "Components/Pages/EventoFicha.razor",
        "Components/Pages/EventoFormulario.razor",
        "Components/Pages/Home.razor",
        "Components/Pages/MeusDocumentos.razor",
        "Components/Pages/MinhaSenha.razor",
        "Components/Pages/NaoEncontrada.razor",
        "Components/Pages/Perfis.razor",
        "Components/Pages/ProcessoFicha.razor",
        "Components/Pages/ProcessoFormulario.razor",
        "Components/Pages/Processos.razor",
        "Components/Pages/Produtores.razor",
        "Components/Pages/UsuarioFormulario.razor",
        "Components/Pages/Usuarios.razor",
        "Components/Produtores/PainelDeProdutividade.razor",
        "Components/Usuarios/ConfirmacaoDeInativacao.razor",
    ];

    // O documento e o aviso de reconexão usam classes que o Blazor exige: o blazor.web.js procura .reload e .dismiss na
    // faixa de erro, e o modelo de reconexão mostra cada mensagem pela sua classe components-…. O aviso de reconexão é o
    // único .razor com CSS próprio (exceção da spec).
    private static readonly string[] Infraestrutura =
    [
        "Components/App.razor",
        "Components/Layout/ReconnectModal.razor",
    ];

    // As classes fora do aj- que o framework exige, uma a uma (lista fechada: um nome novo é decisão consciente).
    private static readonly HashSet<string> ClassesDoFramework =
    [
        "reload",
        "dismiss",
        "components-reconnect-container",
        "components-reconnect-first-attempt-visible",
        "components-reconnect-repeated-attempt-visible",
        "components-reconnect-failed-visible",
        "components-pause-visible",
        "components-resume-failed-visible",
    ];

    [GeneratedRegex("\\sstyle\\s*=\\s*[\"']")]
    private static partial Regex EstiloEmbutido();

    [GeneratedRegex("<style\\b", RegexOptions.IgnoreCase)]
    private static partial Regex FolhaEmbutida();

    // Só o valor fixo: classe montada por expressão Razor (com @) é conferida na revisão.
    [GeneratedRegex("\\s(?:class|Classe)\\s*=\\s*(?:\"([^\"@]*)\"|'([^'@]*)')")]
    private static partial Regex ClasseFixa();

    // A marca inteira de um elemento que, sem classe, o navegador desenharia com o estilo padrão. Os valores entre aspas
    // são consumidos inteiros, para o "=>" de uma expressão não encerrar a marca.
    [GeneratedRegex("<(button|table|input|select|textarea)\\b((?:[^>\"']|\"[^\"]*\"|'[^']*')*)>")]
    private static partial Regex ElementoQueExigeClasse();

    [GeneratedRegex("\\sclass\\s*=")]
    private static partial Regex TemClasse();

    /// <summary>
    /// O que a marcação tem de fora do design system. <paramref name="aceitas"/>: as classes fora do <c>aj-</c> que o
    /// arquivo pode usar (só a infraestrutura passa as do framework; as telas, nenhuma).
    /// </summary>
    private static List<string> Violacoes(string marcacao, IReadOnlySet<string>? aceitas = null)
    {
        var violacoes = new List<string>();
        if (EstiloEmbutido().IsMatch(marcacao)) violacoes.Add("style");
        if (FolhaEmbutida().IsMatch(marcacao)) violacoes.Add("<style>");

        violacoes.AddRange(ElementoQueExigeClasse().Matches(marcacao)
            .Where(m => !TemClasse().IsMatch(m.Groups[2].Value))
            .Select(m => $"<{m.Groups[1].Value}> sem classe"));

        violacoes.AddRange(ClasseFixa().Matches(marcacao)
            .SelectMany(m => (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Where(classe => !classe.StartsWith("aj-", StringComparison.Ordinal) && aceitas?.Contains(classe) != true));
        return violacoes;
    }

    /// <summary>Os <c>.razor</c> que não estão em nenhuma das duas listas.</summary>
    private static List<string> ForaDaTrava(IEnumerable<string> arquivos) =>
        arquivos.Except(Migradas).Except(Infraestrutura).ToList();

    private static string Web() => Path.Combine(GravacoesPorForaTests.RaizDoRepositorio(), "src", "AtividadesJuridicas.Web");

    private static string Caminho(string arquivo) => Path.Combine([Web(), .. arquivo.Split('/')]);

    // Os arquivos de Components com a extensão pedida, em qualquer pasta, a partir de src/AtividadesJuridicas.Web.
    private static List<string> ArquivosDeComponents(string padrao)
    {
        var web = Web();
        return Directory.GetFiles(Path.Combine(web, "Components"), padrao, SearchOption.AllDirectories)
            .Select(caminho => Path.GetRelativePath(web, caminho).Replace('\\', '/'))
            .ToList();
    }

    public static TheoryData<string> Arquivos => new(Migradas);

    public static TheoryData<string> ArquivosDeInfraestrutura => new(Infraestrutura);

    [Theory]
    [MemberData(nameof(Arquivos))]
    public void Tela_migrada_nao_tem_estilo_embutido_nem_classe_fora_do_design_system(string arquivo)
    {
        var caminho = Caminho(arquivo);

        Violacoes(File.ReadAllText(caminho)).Should().BeEmpty(
            "tela migrada não tem estilo embutido nem classe fora do design system: o padrão vira classe aj- lá");
        File.Exists(caminho + ".css").Should().BeFalse("tela migrada não tem CSS próprio: o estilo vem do design system");
    }

    [Theory]
    [MemberData(nameof(ArquivosDeInfraestrutura))]
    public void Infraestrutura_so_usa_fora_do_design_system_as_classes_que_o_framework_exige(string arquivo)
    {
        var caminho = Caminho(arquivo);

        Violacoes(File.ReadAllText(caminho), ClassesDoFramework).Should().BeEmpty(
            "a infraestrutura não tem estilo embutido e só usa, fora das classes aj-, as que o Blazor exige");
        File.Exists(caminho + ".css").Should().Be(arquivo == "Components/Layout/ReconnectModal.razor",
            "só o aviso de reconexão tem CSS próprio (exceção da spec)");
    }

    // Todo .razor da Web, em qualquer pasta, fica na trava. Um componente novo (ou um antigo que sobrou) fora das listas
    // faz este teste falhar: entra em Migradas (marcação só do design system) ou, se o framework exigir, em
    // Infraestrutura. Substitui as travas por pasta das atividades e da agenda, e mantém as asserções delas.
    [Fact]
    public void Todo_razor_da_web_esta_na_trava()
    {
        var arquivos = ArquivosDeComponents("*.razor");

        arquivos.Should().Contain("Components/Pages/Atividades.razor").And.Contain("Components/Atividades/EtiquetaDeSituacao.razor")
            .And.Contain("Components/Pages/Agenda.razor").And.Contain("Components/Pages/EventoFicha.razor")
            .And.Contain("Components/Pages/EventoFormulario.razor").And.Contain("Components/Agenda/SemanaDaAgenda.razor")
            .And.Contain("Components/Pages/Conta/Entrar.razor").And.Contain("Components/App.razor");
        ForaDaTrava(arquivos).Should().BeEmpty(
            "todo .razor da Web está em Migradas ou em Infraestrutura; fora delas, pode voltar à marcação antiga");
        Migradas.Intersect(Infraestrutura).Should().BeEmpty("cada arquivo está numa lista só");
    }

    [Fact]
    public void So_o_aviso_de_reconexao_tem_css_proprio()
    {
        ArquivosDeComponents("*.razor.css").Should().Equal(new[] { "Components/Layout/ReconnectModal.razor.css" },
            "o estilo das telas vem do design system; o aviso de reconexão é a exceção da spec");
    }

    // A própria trava recursiva: um .razor fora das duas listas reprova, em qualquer pasta; os das listas passam.
    [Fact]
    public void A_trava_reprova_razor_fora_das_duas_listas()
    {
        ForaDaTrava(["Components/Pages/Home.razor", "Components/App.razor", "Components/Agenda/Detalhes/NovoCartao.razor"])
            .Should().Equal("Components/Agenda/Detalhes/NovoCartao.razor");
    }

    [Theory]
    [InlineData("<a href=\".\" class=\"reload\">Recarregar</a>", "reload")]
    [InlineData("<button type=\"button\" class=\"dismiss aj-botao\">x</button>", "dismiss")]
    [InlineData("<p class=\"components-pause-visible\">x</p>", "components-pause-visible")]
    public void A_trava_aceita_classe_do_framework_so_na_infraestrutura(string marcacao, string violacao)
    {
        Violacoes(marcacao).Should().Equal(violacao);
        Violacoes(marcacao, ClassesDoFramework).Should().BeEmpty();
    }

    [Theory]
    [InlineData("<p class=\"components-pause-visible erro\">x</p>", "erro")]
    [InlineData("<p class=\"components-reconnect-qualquer\">x</p>", "components-reconnect-qualquer")]
    [InlineData("<dialog style=\"width:20rem\"></dialog>", "style")]
    public void A_trava_da_infraestrutura_reprova_o_que_o_framework_nao_exige(string marcacao, string violacao)
    {
        Violacoes(marcacao, ClassesDoFramework).Should().Equal(violacao);
    }

    // A própria trava: o que ela tem de reprovar e o que tem de deixar passar.
    [Theory]
    [InlineData("<p style=\"margin:0\">x</p>", "style")]
    [InlineData("<p style='margin:0'>x</p>", "style")]
    [InlineData("<p class=\"erro\">x</p>", "erro")]
    [InlineData("<p class='aj-aviso erro'>x</p>", "erro")]
    [InlineData("<div class=\"aj-painel formulario\"></div>", "formulario")]
    public void A_trava_reprova_estilo_embutido_e_classe_de_fora(string marcacao, string violacao)
    {
        Violacoes(marcacao).Should().Equal(violacao);
    }

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
    [InlineData("<p class=\"aj-aviso aj-aviso--erro\">x</p>")]
    [InlineData("<label class=\"@(erro ? \"aj-rotulo aj-rotulo--erro\" : \"aj-rotulo\")\">x</label>")]
    [InlineData("<Icone Nome=\"voltar\" Classe=\"aj-cartao__seta\" />")]
    public void A_trava_aceita_marcacao_do_design_system(string marcacao)
    {
        Violacoes(marcacao).Should().BeEmpty();
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
}
```

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Expected: falha só `Todo_razor_da_web_esta_na_trava`, com exatamente estes quatro arquivos fora das duas listas: `Components/_Imports.razor`, `Components/Routes.razor`, `Components/Layout/FocoNoTitulo.razor`, `Components/Layout/LayoutVazio.razor`. Passam o theory das telas (as 63 de hoje, sem mudança), o theory da infraestrutura (o `App.razor` só com `reload` e `dismiss` fora do `aj-`; o `ReconnectModal.razor` só com as seis classes `components-…` da lista e com o seu `.razor.css`), `So_o_aviso_de_reconexao_tem_css_proprio` e todos os testes da própria trava (os de antes e os novos). Se aparecer outro arquivo fora das listas, ou se o theory da infraestrutura reprovar uma classe, pare e relate (não acrescente classe a `ClassesDoFramework` sem conferir que o `blazor.web.js` ou o `ReconnectModal.razor.css`/`.js` a usa).

- [ ] **Step 6: Os quatro arquivos sem classe de framework entram em `Migradas`**

Os quatro passam na regra das telas como estão (conferido na marcação: `_Imports.razor` só tem `@using`; `Routes.razor` só `<Router>`, `<Found>`, `<AuthorizeRouteView>` e `<FocoNoTitulo>`, sem HTML; `Layout/FocoNoTitulo.razor` só `<FocusOnNavigate RouteData="RouteData" Selector="h1" />` e código; `Layout/LayoutVazio.razor` só `@Body`; nenhum tem `class=`, `Classe=`, `style`, elemento que exija classe nem `.razor.css`). Em `Migradas`, na ordem da lista (ordinal):

- depois de `"Components/Layout/EstadoDaLista.razor",`: `"Components/Layout/FocoNoTitulo.razor",`
- depois de `"Components/Layout/Icone.razor",`: `"Components/Layout/LayoutVazio.razor",`
- depois de `"Components/Produtores/PainelDeProdutividade.razor",`: `"Components/Routes.razor",`
- depois de `"Components/Usuarios/ConfirmacaoDeInativacao.razor",` (o último): `"Components/_Imports.razor",`

Ficam em `Infraestrutura` só `App.razor` e `ReconnectModal.razor`. Com isso, `Migradas` tem 67 arquivos e as duas listas somam os 69 `.razor` de `Components/`.

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Expected: todos passando, inclusive `Todo_razor_da_web_esta_na_trava` e o theory das telas para os quatro novos.

- [ ] **Step 7: Build e testes**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build`
Expected: todos passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~LayoutWebTests"`
Expected: todos passando (o theory `Toda_tela_da_equipe_abre_o_conteudo_com_um_bloco_do_design_system` com a razão nova, sem mudança na asserção).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AutenticacaoWebTests"`
Expected: todos passando (`Pagina_de_login_e_seu_css_sao_publicos` continua lendo `/app.css`; `Faixa_de_erro_e_aviso_de_reconexao_estao_em_portugues` continua achando a faixa e o aviso).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~WebTests|FullyQualifiedName~Interativ|FullyQualifiedName~ComponentesDoDesignSystemTests"`
Expected: todos passando, sem mudar nenhum teste de tela (o `app.css` não aparece no HTML; o bUnit não aplica CSS). O efeito visual (nenhum) se mede na Tarefa 11 (`V.vazamento` nas três larguras): ali a única diferença esperada em relação à linha de base da 9F é nenhuma, já com o design system da Tarefa 1.

- [ ] **Step 8: Commit**

Confira o branch (`git branch --show-current` deve dizer `worktree-feat+fase-9g-limpeza`) e:

```
git add src/AtividadesJuridicas.Web/wwwroot/app.css src/AtividadesJuridicas.Web/Components/App.razor src/AtividadesJuridicas.Web/Components/Layout/ReconnectModal.razor.css tests/AtividadesJuridicas.UnitTests/AppCssTests.cs tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs
git commit -F <arquivo com a mensagem, UTF-8 sem BOM>
```

Mensagem:

```
refactor(web): app.css só com a base da página, aviso de reconexão com a própria caixa de borda e trava recursiva de todo .razor

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

### Task 3: Módulo de foco — rede de segurança, `focarPrimeiroVisivel`, rolagem por página, `Aviso.Revelar`

**Files:**
- Create: `src/AtividadesJuridicas.Web/Infra/Foco.cs`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/FocoNoTitulo.razor`, `FocoNoTitulo.razor.js`, `Aviso.razor`, `src/AtividadesJuridicas.Web/Program.cs` (registro do `Foco`), `tests/AtividadesJuridicas.IntegrationTests/TelaInterativa.cs` e `TelaDeTeste.cs` (o `Foco` nos serviços de teste, sem o qual as telas das Tarefas 4 a 10 não renderizam)
- Test: `tests/AtividadesJuridicas.IntegrationTests/FocoNoTituloInterativoTests.cs` (novo), `AvisoInterativoTests.cs` (novo), `ComponentesDoDesignSystemTests.cs`, `LayoutWebTests.cs`

**Interfaces:**
- Produces:
  - No módulo `FocoNoTitulo.razor.js`: `export function focarPrimeiroVisivel(seletor)` (foca o primeiro elemento visível — com caixa — que casa com o seletor; devolve se focou); `export function vigiarFoco()` (instala, uma vez, a rede de segurança: quando o foco cai no `body` porque o elemento focado saiu do DOM, foca o `h1` visível); `export function zerarRolagem()` (rolagem do `main.aj-app__conteudo` ao topo).
  - `FocoNoTitulo` chama `vigiarFoco` uma vez e `zerarRolagem` quando muda a página (o mesmo critério do foco no título: mudou o tipo do componente da rota).
  - Um serviço de escopo do circuito `Foco` (em `Web/Infra/Foco.cs`), injetável nas telas, com `Task FocarPrimeiroVisivelAsync(string seletor)` (chama o módulo), para as Tarefas 4 a 10 usarem sem repetir o `IJSRuntime`.
  - `Aviso.Revelar` (`bool`): ao aparecer (o primeiro desenho com `Revelar`, ou `Revelar` passando a verdadeiro), o aviso ganha `tabindex="-1"` e recebe o foco (o que também rola até ele). Sem `Revelar`, a marcação de hoje. Para revelar uma mensagem nova num aviso que continua à vista, a tela troca o `@key` do `Aviso` por um contador que ela incrementa a cada mensagem (dois erros iguais seguidos se revelam os dois).
  - O `Foco` também tem `FocarTituloVisivelAsync()`, `VigiarAsync()` e `ZerarRolagemAsync()`, que o `FocoNoTitulo` usa; importa `Foco.Modulo` (`"./Components/Layout/FocoNoTitulo.razor.js"`) uma vez por circuito e engole `InvalidOperationException`, `JSException`, `JSDisconnectedException` e `OperationCanceledException`.
  - Auxiliares de teste na `TelaInterativa` (usados pelas Tarefas 4 a 10; nenhuma outra tarefa monta o módulo de foco de novo): `ModuloDeFoco` (`BunitJSModuleInterop`, montado no construtor), `SeletoresFocados` (os seletores pedidos a `focarPrimeiroVisivel`, em ordem), `UltimoSeletorFocado()`, `UltimoFocoPedido()` (o último `ElementReference.FocusAsync`) e `FocosDeElementoPedidos` (quantos). O `Foco` registrado no `Program.cs`, na `TelaInterativa` e no `TelaDeTeste`.

**Regras:** o módulo não depende de nenhuma tela; o `FocoNoTitulo` de hoje continua focando o título ao navegar; a rede de segurança não age se o foco caiu no `body` por clique fora (só quando o elemento focado deixou de estar no DOM).

**Testes obrigatórios:** bUnit: `FocoNoTitulo` chama `vigiarFoco` uma vez e `zerarRolagem` só quando a página muda (não quando só a consulta muda); `Aviso` com `Revelar` pede o foco ao aparecer e sem `Revelar` não pede; o serviço `Foco` chama `focarPrimeiroVisivel` com o seletor. O comportamento real (o foco que cai no `body`, a rolagem) fica para a Tarefa 11.

- [ ] **Step 1: Testes do `FocoNoTitulo` e do serviço `Foco` (devem falhar)**

Crie `tests/AtividadesJuridicas.IntegrationTests/FocoNoTituloInterativoTests.cs` (UTF-8 sem BOM, CRLF). Os testes provam o que o .NET **pede** ao módulo `FocoNoTitulo.razor.js`: o bUnit registra as chamadas do módulo montado uma vez por teste no construtor da `TelaInterativa` (`ModuloDeFoco`, que o Step 3 cria, em modo frouxo herdado). Os testes não chamam `JSInterop.SetupModule` de novo para o mesmo caminho: dois módulos montados no mesmo caminho dividiriam as chamadas entre si. O que o JavaScript faz no navegador (o foco que cai no `body`, a rolagem do `main`) fica para a Tarefa 11.

O `FocoNoTitulo` desenhado sozinho no bUnit não tem modo de renderização atribuído, então também desenha o `<FocusOnNavigate>` (que chama o JavaScript do Blazor, não o módulo); isso não muda o que estes testes conferem.

```csharp
using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Paginas = AtividadesJuridicas.Web.Components.Pages;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O módulo de foco do layout, pelo que o .NET pede a ele: a rede de segurança instalada uma vez, o título focado e a
/// rolagem zerada quando a página muda, e o serviço <c>Foco</c> que as telas usam. O que o JavaScript faz (o foco que
/// cai no body, a rolagem do main) só se confere no navegador.
/// </summary>
public class FocoNoTituloInterativoTests : TelaInterativa
{
    // A rota como o Router a entrega: o tipo da página e os valores do endereço. A consulta (?visao=, ?produtor=,
    // ?pagina=) não entra no RouteData: mudar só a consulta entrega outra instância com o mesmo tipo de página.
    private static RouteData Rota<TPagina>(params (string Nome, object? Valor)[] valores) =>
        new(typeof(TPagina), valores.ToDictionary(v => v.Nome, v => v.Valor));

    [Fact]
    public void Na_primeira_pagina_instala_a_rede_e_foca_o_titulo_sem_zerar_a_rolagem()
    {
        Desenhar<FocoNoTitulo>(p => p.Add(f => f.RouteData, Rota<Paginas.Home>()));

        ModuloDeFoco.VerifyInvoke("vigiarFoco");
        ModuloDeFoco.VerifyInvoke("focarTituloVisivel");
        ModuloDeFoco.VerifyNotInvoke("zerarRolagem");
    }

    [Fact]
    public void Pagina_nova_zera_a_rolagem_e_foca_o_titulo_sem_instalar_a_rede_de_novo()
    {
        var tela = Desenhar<FocoNoTitulo>(p => p.Add(f => f.RouteData, Rota<Paginas.Home>()));

        tela.Render(p => p.Add(f => f.RouteData, Rota<Paginas.Clientes>()));

        ModuloDeFoco.VerifyInvoke("zerarRolagem");
        ModuloDeFoco.VerifyInvoke("focarTituloVisivel", calledTimes: 2);
        ModuloDeFoco.VerifyInvoke("vigiarFoco", calledTimes: 1);
    }

    [Fact]
    public void Mesma_pagina_com_outra_consulta_ou_outro_endereco_nao_zera_a_rolagem_nem_foca_de_novo()
    {
        var tela = Desenhar<FocoNoTitulo>(p => p.Add(f => f.RouteData, Rota<Paginas.Agenda>()));

        // Troca de período da agenda (?visao=mes&data=…): a mesma página, outra consulta.
        tela.Render(p => p.Add(f => f.RouteData, Rota<Paginas.Agenda>()));
        // E o mesmo critério do título: a mesma ficha com outro id é a mesma página.
        tela.Render(p => p.Add(f => f.RouteData, Rota<Paginas.AtividadeFicha>(("Id", Guid.NewGuid()))));
        tela.Render(p => p.Add(f => f.RouteData, Rota<Paginas.AtividadeFicha>(("Id", Guid.NewGuid()))));

        // Da agenda para a ficha mudou a página: uma vez só, nas duas chamadas.
        ModuloDeFoco.VerifyInvoke("zerarRolagem", calledTimes: 1);
        ModuloDeFoco.VerifyInvoke("focarTituloVisivel", calledTimes: 2);
        ModuloDeFoco.VerifyInvoke("vigiarFoco", calledTimes: 1);
    }

    [Fact]
    public async Task Foco_pede_ao_modulo_o_primeiro_visivel_do_seletor()
    {
        var foco = Services.GetRequiredService<Foco>();

        await foco.FocarPrimeiroVisivelAsync("button[aria-controls=\"painel-de-filtros\"]");

        ModuloDeFoco.VerifyInvoke("focarPrimeiroVisivel").Arguments.Should().Equal("button[aria-controls=\"painel-de-filtros\"]");
        // Os auxiliares da TelaInterativa que as Tarefas 4 a 10 usam leem o mesmo registro.
        SeletoresFocados.Should().Equal("button[aria-controls=\"painel-de-filtros\"]");
        UltimoSeletorFocado().Should().Be("button[aria-controls=\"painel-de-filtros\"]");
        FocosDeElementoPedidos.Should().Be(0);
    }

    [Fact]
    public async Task Falha_do_modulo_nao_derruba_quem_pediu_o_foco()
    {
        ModuloDeFoco.SetupVoid("focarPrimeiroVisivel", "h1").SetException(new JSException("O módulo falhou."));
        var foco = Services.GetRequiredService<Foco>();

        await foco.Invoking(f => f.FocarPrimeiroVisivelAsync("h1")).Should().NotThrowAsync();
        ModuloDeFoco.VerifyInvoke("focarPrimeiroVisivel").Arguments.Should().Equal("h1");
    }
}
```

Run: `dotnet build -m:1`
Expected: falha de compilação em `FocoNoTituloInterativoTests.cs`: `CS0246` (o tipo `Foco` não existe) e `CS0103` (`ModuloDeFoco`, `SeletoresFocados`, `UltimoSeletorFocado` e `FocosDeElementoPedidos` ainda não existem na `TelaInterativa`).

- [ ] **Step 2: O módulo de foco**

Reescreva `src/AtividadesJuridicas.Web/Components/Layout/FocoNoTitulo.razor.js` (UTF-8 sem BOM, CRLF) exatamente assim. O `focarTituloVisivel` de hoje continua com o mesmo comportamento (o primeiro `h1` com caixa, `tabindex="-1"` se não tiver, foco sem rolar).

```js
// O foco e a rolagem que o teclado e o leitor de tela precisam. O módulo não conhece nenhuma tela: só o h1, o main
// do layout e o seletor que quem chama passa. Cada visão (desktop, tablet, mobile) tem a sua cópia dos elementos e as
// das outras ficam escondidas; por isso tudo aqui procura o primeiro que está à vista (que tem caixa).

function primeiroVisivel(seletor) {
    for (const elemento of document.querySelectorAll(seletor)) {
        if (elemento.getClientRects().length > 0) return elemento;
    }
    return null;
}

// Depois de cada navegação: o título à vista recebe o foco, para ser anunciado, sem rolar a página.
export function focarTituloVisivel() {
    const titulo = primeiroVisivel("h1");
    if (titulo === null) return;

    if (!titulo.hasAttribute("tabindex")) titulo.setAttribute("tabindex", "-1");
    titulo.focus({ preventScroll: true });
}

// Foca o primeiro elemento à vista que casa com o seletor (o par visível de um botão que tem uma cópia por visão, o
// botão que abriu um painel). Devolve se o foco chegou a ele.
export function focarPrimeiroVisivel(seletor) {
    const elemento = primeiroVisivel(seletor);
    if (elemento === null) return false;

    elemento.focus();
    return document.activeElement === elemento;
}

// Página nova começa do topo. Quem rola é o main do layout (a janela fica parada); sem ele (entrar, erro), nada a fazer.
export function zerarRolagem() {
    document.querySelector("main.aj-app__conteudo")?.scrollTo(0, 0);
}

let vigiando = false;
let ultimoFocado = null;

// Rede de segurança: quando uma renderização tira do DOM o elemento que tinha o foco (o botão de um painel que fechou,
// a linha que saiu da lista), o navegador deixa o foco no body e o próximo Tab recomeça do alto da página. A rede leva
// o foco ao título à vista. Os casos com destino melhor (quem abriu o painel, o aviso de resultado) são tratados por
// tela e chegam depois, pelo .NET: tiram o foco do título.
//
// Como a rede sabe que o elemento saiu do DOM: guarda o último elemento que recebeu o foco (focusin) e, depois de
// cada mudança no DOM (MutationObserver, que roda logo depois de o Blazor aplicar a renderização inteira), confere se
// ele deixou de estar no documento (isConnected) com o foco no body. Não depende do focusout/blur na remoção, que uns
// navegadores disparam e outros não.
//
// Clique fora (num ponto sem nada focável): o foco também vai ao body, mas o elemento continua no DOM, e a rede não
// age. Para não agir depois, se uma renderização tirar esse elemento mais tarde, a saída por clique fora o esquece.
export function vigiarFoco() {
    if (vigiando) return;
    vigiando = true;

    document.addEventListener("focusin", evento => { ultimoFocado = evento.target; }, true);

    // Foco que sai para lugar nenhum (relatedTarget nulo): clique fora, troca de janela ou remoção do elemento. A
    // conferência espera a tarefa seguinte, depois da rede: na remoção, a rede já agiu e o foco está no título; no
    // clique fora, o elemento continua no DOM sem o foco, e é esquecido; na troca de janela, ele continua com o foco
    // (document.activeElement) e é mantido.
    document.addEventListener("focusout", evento => {
        if (evento.relatedTarget !== null) return;

        const saiu = evento.target;
        setTimeout(() => {
            if (ultimoFocado === saiu && saiu.isConnected && document.activeElement !== saiu) ultimoFocado = null;
        }, 0);
    }, true);

    new MutationObserver(() => {
        if (ultimoFocado === null || ultimoFocado.isConnected) return;

        const ativo = document.activeElement;
        if (ativo !== null && ativo !== document.body) return;

        ultimoFocado = null;
        focarTituloVisivel();
    }).observe(document.body, { childList: true, subtree: true });
}
```

Por que `MutationObserver` + `isConnected`, e não só `focusout`: o evento de saída do foco quando o elemento é removido não é o mesmo em todo navegador (uns disparam `blur`/`focusout` com o elemento ainda no documento, outros não disparam nada), e no `focusout` não se sabe ainda se o elemento vai sair do DOM. O observador roda uma vez por renderização aplicada (depois do lote inteiro do Blazor, numa microtarefa), já com o elemento fora e o foco no `body`, e a conferência é de custo constante. O clique fora nunca dispara a rede: o elemento continua no DOM (`isConnected`), e a saída por clique o esquece.

O que fica para o navegador (Tarefa 11): o foco que cai no `body` depois de Cancelar ou de fechar um painel vai ao título à vista; um clique num ponto vazio deixa o foco no `body` sem mexer; a rolagem do `main` volta ao topo ao trocar de página e não volta ao trocar o período da agenda.

- [ ] **Step 3: O serviço `Foco` e os registros**

Crie `src/AtividadesJuridicas.Web/Infra/Foco.cs` (UTF-8 sem BOM, CRLF):

```csharp
using Microsoft.JSInterop;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>
/// O foco e a rolagem que o teclado e o leitor de tela precisam, num lugar só: chama o módulo
/// <c>Components/Layout/FocoNoTitulo.razor.js</c>, importado uma vez por circuito. O foco é uma ajuda: uma falha do
/// JavaScript (circuito caindo, desenho sem JavaScript) nunca derruba a tela que pediu.
/// </summary>
public sealed class Foco(IJSRuntime js)
{
    /// <summary>O módulo, pelo endereço em que o Blazor o serve.</summary>
    public const string Modulo = "./Components/Layout/FocoNoTitulo.razor.js";

    // Vive enquanto o circuito vive: a referência vai embora com a conexão.
    private IJSObjectReference? _modulo;

    /// <summary>
    /// Foca o primeiro elemento à vista que casa com <paramref name="seletor"/> (o par visível de um botão que tem uma
    /// cópia por visão, o botão que abriu um painel). Nenhum à vista: o foco fica onde está.
    /// </summary>
    public Task FocarPrimeiroVisivelAsync(string seletor) => ChamarAsync("focarPrimeiroVisivel", seletor);

    /// <summary>Foca o título (<c>h1</c>) à vista, sem rolar: é o que o leitor de tela anuncia a cada página.</summary>
    public Task FocarTituloVisivelAsync() => ChamarAsync("focarTituloVisivel");

    /// <summary>Instala a rede de segurança do foco (uma vez por página carregada; chamar de novo não faz nada).</summary>
    public Task VigiarAsync() => ChamarAsync("vigiarFoco");

    /// <summary>Leva a rolagem do conteúdo (<c>main.aj-app__conteudo</c>) ao topo.</summary>
    public Task ZerarRolagemAsync() => ChamarAsync("zerarRolagem");

    private async Task ChamarAsync(string funcao, params object?[] argumentos)
    {
        try
        {
            _modulo ??= await js.InvokeAsync<IJSObjectReference>("import", Modulo);
            await _modulo.InvokeVoidAsync(funcao, argumentos);
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSException or JSDisconnectedException or OperationCanceledException)
        {
            // Sem foco (circuito caindo, JavaScript fora do ar, desenho sem circuito) a tela continua funcional.
        }
    }
}
```

Em `src/AtividadesJuridicas.Web/Program.cs` (CRLF), depois de `builder.Services.AddScoped<Recados>();`:

```csharp
builder.Services.AddScoped<Foco>();
```

Reescreva `tests/AtividadesJuridicas.IntegrationTests/TelaInterativa.cs` (UTF-8 sem BOM, CRLF) inteiro assim. Muda o resumo, entram o registro do `Foco` (as telas que o injetam, nas Tarefas 4 a 10, passam a tê-lo em todo teste de clique), o módulo de foco montado no construtor e os auxiliares que os testes das Tarefas 3 a 10 usam para conferir o foco **pedido**. São os únicos auxiliares de foco da suíte: nenhuma outra tarefa monta o módulo de foco de novo (`SetupModule` no mesmo caminho dividiria as chamadas) nem declara membros com esses nomes.

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Base dos testes que clicam: desenha um componente com o bUnit, com o despachante combinado do teste, o fuso do
/// escritório, os recados entre telas e o serviço de foco. O JavaScript fica solto (as chamadas de foco são registradas,
/// não executadas) e o teste confere o foco pelo registro: o pedido a um elemento (<see cref="UltimoFocoPedido"/>) ou ao
/// módulo de foco do layout (<see cref="SeletoresFocados"/>).
/// </summary>
public abstract class TelaInterativa : BunitContext
{
    /// <summary>Os atores que <c>ObterMeuAcesso</c> devolve nos testes: uma diretora e um usuário de Produção.</summary>
    protected static readonly UsuarioAutenticado AtorDaDiretoria = new(
        Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d001"), "Diana Diretora", TipoPerfil.Diretoria, Guid.Parse("0199a3f2-7c1e-7000-8000-00000000e001"));
    protected static readonly UsuarioAutenticado AtorDaProducao = new(
        Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d002"), "Pedro Produção", TipoPerfil.Producao, Guid.Parse("0199a3f2-7c1e-7000-8000-00000000e002"));

    protected DespachanteCombinado Despachante { get; } = new();

    /// <summary>
    /// O módulo de foco do layout (<c>FocoNoTitulo.razor.js</c>), no caminho que o serviço <see cref="Foco"/> importa
    /// (<see cref="Foco.Modulo"/>): as chamadas a ele ficam registradas aqui. Um só por teste: um segundo
    /// <c>SetupModule</c> no mesmo caminho dividiria as chamadas entre os dois.
    /// </summary>
    protected BunitJSModuleInterop ModuloDeFoco { get; }

    /// <summary>Os seletores que a tela pediu ao <c>focarPrimeiroVisivel</c> do módulo de foco, na ordem dos pedidos.</summary>
    protected IReadOnlyList<string> SeletoresFocados =>
        [.. ModuloDeFoco.Invocations["focarPrimeiroVisivel"].Select(chamada => (string)chamada.Arguments[0]!)];

    /// <summary>O seletor do último pedido ao <c>focarPrimeiroVisivel</c>; sem pedido nenhum, o teste falha.</summary>
    protected string UltimoSeletorFocado()
    {
        var seletores = SeletoresFocados;
        seletores.Should().NotBeEmpty("a tela devia ter pedido o foco ao primeiro elemento visível de um seletor");
        return seletores[^1];
    }

    /// <summary>
    /// O último pedido de foco a um elemento (<c>ElementReference.FocusAsync</c>, que o Blazor faz por
    /// <c>Blazor._internal.domWrapper.focus</c>, o mesmo que o <c>VerifyFocusAsyncInvoke</c> confere); sem pedido
    /// nenhum, o teste falha.
    /// </summary>
    protected JSRuntimeInvocation UltimoFocoPedido()
    {
        var pedidos = JSInterop.Invocations[FocoDeElemento];
        pedidos.Should().NotBeEmpty("a tela devia ter pedido o foco a um elemento");
        return pedidos[^1];
    }

    /// <summary>Quantos pedidos de foco a um elemento (<c>ElementReference.FocusAsync</c>) a tela fez até agora.</summary>
    protected int FocosDeElementoPedidos => JSInterop.Invocations[FocoDeElemento].Count;

    // O identificador com que o Blazor pede o foco a um elemento; o bUnit o registra assim. O indexador de Invocations
    // devolve a lista vazia para um identificador sem chamada (bUnit 2.11.3), e por isso os auxiliares não precisam de
    // tratamento à parte antes do primeiro pedido.
    private const string FocoDeElemento = "Blazor._internal.domWrapper.focus";

    protected TelaInterativa()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ModuloDeFoco = JSInterop.SetupModule("./Components/Layout/FocoNoTitulo.razor.js");
        Services.AddSingleton<IDespachante>(Despachante);
        Services.AddSingleton(new FusoDoEscritorio("America/Sao_Paulo"));
        Services.AddSingleton(new Recados());
        Services.AddSingleton<Foco>();
    }

    protected IRenderedComponent<T> Desenhar<T>(Action<ComponentParameterCollectionBuilder<T>>? parametros = null)
        where T : IComponent =>
        Render(parametros);
}
```

O módulo montado aqui herda o modo frouxo (`JSRuntimeMode.Loose`): toda chamada que o teste não configurou devolve o padrão. O bUnit registra cada chamada do módulo também no `JSInterop` do teste, e montar o módulo não registra chamada nenhuma (`JSInterop.Invocations` continua vazio até a tela chamar o JavaScript).

Em `tests/AtividadesJuridicas.IntegrationTests/TelaDeTeste.cs` (CRLF), o desenho estático também precisa do serviço (o componente o injeta; o desenho estático nunca o chama, porque o `HtmlRenderer` não roda `OnAfterRenderAsync`). Troque

```csharp
    // Só o que os componentes da aplicação injetam: o despachante, o fuso do escritório, os recados entre telas, o
    // NavigationManager (as telas que navegam depois de salvar) e o JavaScript (que o InputFile injeta e o desenho
    // estático nunca chama). Um ponto só para os desenhos com despachante.
    private static ServiceProvider Servicos(IDespachante despachante, Recados? recados) => new ServiceCollection()
        .AddSingleton(despachante)
        .AddSingleton(new FusoDoEscritorio("America/Sao_Paulo"))
        .AddSingleton(recados ?? new Recados())
        .AddSingleton<NavigationManager>(new NavegacaoDeTeste())
        .AddSingleton<IJSRuntime>(new SemJavaScript())
        .BuildServiceProvider();
```

por

```csharp
    // Só o que os componentes da aplicação injetam: o despachante, o fuso do escritório, os recados entre telas, o
    // NavigationManager (as telas que navegam depois de salvar), o JavaScript (que o InputFile injeta e o desenho
    // estático nunca chama) e o Foco (que as telas chamam depois de desenhar, e o desenho estático também nunca chama).
    // Um ponto só para os desenhos com despachante.
    private static ServiceProvider Servicos(IDespachante despachante, Recados? recados) => new ServiceCollection()
        .AddSingleton(despachante)
        .AddSingleton(new FusoDoEscritorio("America/Sao_Paulo"))
        .AddSingleton(recados ?? new Recados())
        .AddSingleton<NavigationManager>(new NavegacaoDeTeste())
        .AddSingleton<IJSRuntime>(new SemJavaScript())
        .AddSingleton<Foco>()
        .BuildServiceProvider();
```

e, em `RenderizarSemDadosAsync`, troque

```csharp
        await using var servicos = new ServiceCollection()
            .AddSingleton<IJSRuntime>(new SemJavaScript())
            .BuildServiceProvider();
```

por

```csharp
        await using var servicos = new ServiceCollection()
            .AddSingleton<IJSRuntime>(new SemJavaScript())
            .AddSingleton<Foco>()
            .BuildServiceProvider();
```

(`TelaDeTeste.cs` já tem `using AtividadesJuridicas.Web.Infra;`.)

- [ ] **Step 4: O `FocoNoTitulo` instala a rede uma vez e zera a rolagem quando a página muda**

Reescreva `src/AtividadesJuridicas.Web/Components/Layout/FocoNoTitulo.razor` (UTF-8 sem BOM, CRLF) assim. O critério de "página nova" é o de hoje (mudou o tipo do componente da rota); a primeira página do circuito foca o título como hoje e não zera a rolagem.

```razor
@inject Foco Foco

@* Depois de cada navegação, o foco vai para o título da página, que é anunciado, e a rolagem do conteúdo volta ao
   topo. Cada visão tem o seu h1 e os das outras ficam escondidos; por isso não serve o FocusOnNavigate, que mira o
   primeiro h1 do HTML. Na primeira renderização com circuito, instala também a rede de segurança do foco (o elemento
   focado que sai do DOM devolve o foco ao título). Nas páginas sem circuito (entrar, erro) não há quem chame o
   módulo: lá o h1 é um só e vale o do Blazor. *@
@if (AssignedRenderMode is null)
{
    <FocusOnNavigate RouteData="RouteData" Selector="h1" />
}

@code {
    private Type? _ultimaPagina;
    private bool _focar;
    private bool _zerarRolagem;
    private bool _vigiando;

    [Parameter, EditorRequired] public RouteData RouteData { get; set; } = default!;

    // Página nova é outro tipo de componente na rota. A mesma página com outra consulta (período da agenda, ?produtor=,
    // paginação) ou outro valor no endereço não refoca nem rola.
    protected override void OnParametersSet()
    {
        if (RouteData.PageType == _ultimaPagina) return;

        // A primeira página não tem rolagem a zerar, e quem já rolou a página pré-renderizada antes do circuito não
        // deve voltar ao topo quando ele conecta.
        _zerarRolagem = _ultimaPagina is not null;
        _ultimaPagina = RouteData.PageType;
        _focar = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_vigiando)
        {
            _vigiando = true;
            await Foco.VigiarAsync();
        }

        if (!_focar) return;

        _focar = false;
        if (_zerarRolagem)
        {
            _zerarRolagem = false;
            await Foco.ZerarRolagemAsync();
        }
        await Foco.FocarTituloVisivelAsync();
    }
}
```

As falhas do módulo (circuito caindo, `JSException`) agora são tratadas pelo `Foco`, com as mesmas exceções de antes e mais as de quem chama fora de hora (`InvalidOperationException`) e de tempo esgotado (`OperationCanceledException`).

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~FocoNoTituloInterativoTests"`
Expected: os cinco passando.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Expected: todos passando (o `FocoNoTitulo.razor` está em `Migradas` desde a Tarefa 2 e continua sem classe, sem estilo e sem `.razor.css`; o `Foco.cs` não usa repositório nem contexto, e `WebSoPeloDespachanteTests` também passa: rode `--filter "FullyQualifiedName~WebSoPeloDespachanteTests"` em seguida).

- [ ] **Step 5: O módulo servido com as funções novas**

Em `tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs` (CRLF), no teste `Modulo_do_foco_no_titulo_e_servido_e_pagina_estatica_usa_o_foco_do_blazor`, troque

```csharp
        (await modulo.Content.ReadAsStringAsync()).Should().Contain("export function focarTituloVisivel");
```

por

```csharp
        (await modulo.Content.ReadAsStringAsync()).Should().Contain("export function focarTituloVisivel")
            .And.Contain("export function focarPrimeiroVisivel").And.Contain("export function vigiarFoco")
            .And.Contain("export function zerarRolagem");
```

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~LayoutWebTests"`
Expected: todos passando (o endereço do módulo é o mesmo que o `Foco.Modulo` importa).

- [ ] **Step 6: Commit do módulo e do serviço**

Confira o branch (`git branch --show-current` deve dizer `worktree-feat+fase-9g-limpeza`) e:

```
git add src/AtividadesJuridicas.Web/Components/Layout/FocoNoTitulo.razor src/AtividadesJuridicas.Web/Components/Layout/FocoNoTitulo.razor.js src/AtividadesJuridicas.Web/Infra/Foco.cs src/AtividadesJuridicas.Web/Program.cs tests/AtividadesJuridicas.IntegrationTests/FocoNoTituloInterativoTests.cs tests/AtividadesJuridicas.IntegrationTests/TelaInterativa.cs tests/AtividadesJuridicas.IntegrationTests/TelaDeTeste.cs tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs
git commit -F <arquivo com a mensagem, UTF-8 sem BOM>
```

Mensagem:

```
feat(web): módulo de foco com rede de segurança, foco no primeiro visível e rolagem zerada a cada página, e o serviço Foco para as telas

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

- [ ] **Step 7: Testes do `Aviso.Revelar` (devem falhar)**

Crie `tests/AtividadesJuridicas.IntegrationTests/AvisoInterativoTests.cs` (UTF-8 sem BOM, CRLF). Prova que o foco é **pedido** ao próprio aviso (`VerifyFocusAsyncInvoke` com o `ElementReference` dele); a rolagem até o aviso (efeito do `focus()` sem `preventScroll`) fica para o navegador.

```csharp
using AtividadesJuridicas.Web.Components.Layout;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O aviso que se revela: ao aparecer, recebe o foco (o leitor de tela o anuncia e a página rola até ele). Sem
/// <c>Revelar</c>, o aviso de hoje, que não mexe no foco; a confirmação (com <c>Rotulo</c> e ações) continua se focando.
/// </summary>
public class AvisoInterativoTests : TelaInterativa
{
    private const string TentarDeNovo = "<button type=\"button\" class=\"aj-botao aj-botao--secundario\">Tentar de novo</button>";

    private IRenderedComponent<Aviso> DesenharAviso(bool revelar, string texto = "Não foi possível salvar.") =>
        Desenhar<Aviso>(p => p
            .Add(a => a.Tipo, TipoDeAviso.Erro)
            .Add(a => a.Revelar, revelar)
            .AddChildContent(texto));

    [Fact]
    public void Aviso_que_se_revela_pede_o_foco_a_si_mesmo_ao_aparecer()
    {
        var tela = DesenharAviso(revelar: true);

        var aviso = tela.Find("p.aj-aviso");
        aviso.GetAttribute("tabindex").Should().Be("-1");
        aviso.GetAttribute("role").Should().Be("alert");
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }

    [Fact]
    public void Aviso_sem_revelar_nao_pede_foco_nem_ganha_tabindex()
    {
        var tela = DesenharAviso(revelar: false);

        var aviso = tela.Find("p.aj-aviso");
        aviso.GetAttribute("role").Should().Be("alert");
        aviso.TextContent.Should().Be("Não foi possível salvar.");
        aviso.HasAttribute("tabindex").Should().BeFalse();
        JSInterop.Invocations.Should().BeEmpty();
    }

    [Fact]
    public void Aviso_que_continua_a_vista_nao_toma_o_foco_de_novo_a_cada_desenho()
    {
        var tela = DesenharAviso(revelar: true);

        // A tela de cima se desenha de novo (o usuário digitou num campo): o aviso não rouba o foco.
        tela.Render(p => p.Add(a => a.Revelar, true));

        tela.Find("p.aj-aviso").TextContent.Should().Be("Não foi possível salvar.");
        JSInterop.VerifyFocusAsyncInvoke(calledTimes: 1);
    }

    [Fact]
    public void Aviso_que_passa_a_se_revelar_pede_o_foco()
    {
        var tela = DesenharAviso(revelar: false);
        JSInterop.Invocations.Should().BeEmpty();

        tela.Render(p => p.Add(a => a.Revelar, true));

        var aviso = tela.Find("p.aj-aviso");
        aviso.GetAttribute("tabindex").Should().Be("-1");
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }

    [Fact]
    public void Erro_com_acao_que_se_revela_pede_o_foco_a_caixa()
    {
        var tela = Desenhar<Aviso>(p => p
            .Add(a => a.Tipo, TipoDeAviso.Erro)
            .Add(a => a.Revelar, true)
            .AddChildContent("Não foi possível consultar.")
            .Add(a => a.Acoes, TentarDeNovo));

        var caixa = tela.Find("div.aj-aviso--com-acoes");
        caixa.GetAttribute("role").Should().Be("alert");
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(caixa);
    }

    [Fact]
    public void Erro_com_acao_sem_revelar_nao_pede_foco_e_a_confirmacao_continua_se_focando()
    {
        var erro = Desenhar<Aviso>(p => p
            .Add(a => a.Tipo, TipoDeAviso.Erro)
            .AddChildContent("Não foi possível consultar.")
            .Add(a => a.Acoes, TentarDeNovo));
        erro.Find("div.aj-aviso--com-acoes").GetAttribute("role").Should().Be("alert");
        JSInterop.Invocations.Should().BeEmpty();

        var confirmacao = Desenhar<Aviso>(p => p
            .Add(a => a.Tipo, TipoDeAviso.Atencao)
            .Add(a => a.Rotulo, "Confirmar exclusão")
            .AddChildContent("O arquivo sai da ficha.")
            .Add(a => a.Acoes, TentarDeNovo));

        JSInterop.VerifyFocusAsyncInvoke().Arguments[0]
            .ShouldBeElementReferenceTo(confirmacao.Find("div[role=alertdialog]"));
    }
}
```

Em `tests/AtividadesJuridicas.IntegrationTests/ComponentesDoDesignSystemTests.cs` (CRLF), logo depois do teste `Aviso_de_erro_com_acao_continua_sendo_alerta`, o estático (a marcação pré-renderizada já traz o `tabindex`, e a de hoje não muda):

```csharp
    [Fact]
    public async Task Aviso_que_se_revela_tem_tabindex_e_o_comum_continua_sem()
    {
        RenderFragment texto = c => c.AddContent(0, "Não foi possível salvar.");

        var revelado = await TelaDeTeste.RenderizarSemDadosAsync<Aviso>(
            new() { ["Tipo"] = TipoDeAviso.Erro, ["Revelar"] = true, ["ChildContent"] = texto });
        var comum = await TelaDeTeste.RenderizarSemDadosAsync<Aviso>(
            new() { ["Tipo"] = TipoDeAviso.Erro, ["ChildContent"] = texto });

        revelado.Should().Contain("<p class=\"aj-aviso aj-aviso--erro\" role=\"alert\" tabindex=\"-1\">");
        comum.Should().Contain("<p class=\"aj-aviso aj-aviso--erro\" role=\"alert\">").And.NotContain("tabindex");
    }
```

Run: `dotnet build -m:1`
Expected: falha de compilação: `CS0117`/`CS1061` (o `Aviso` não tem `Revelar`) em `AvisoInterativoTests.cs`; a chave `"Revelar"` do teste estático só falharia em execução.

- [ ] **Step 8: `Aviso.Revelar`**

Em `src/AtividadesJuridicas.Web/Components/Layout/Aviso.razor` (CRLF), o aviso sem ações ganha a referência e o `tabindex` só com `Revelar` (sem `Revelar`, o atributo nulo não é escrito: a marcação de hoje). Troque

```razor
    <p class="@(Frase ? $"{Classe} aj-aviso--frase" : Classe)" role="@Papel"><Icone Nome="@NomeDoIcone" /><span>@ChildContent</span></p>
```

por

```razor
    <p class="@(Frase ? $"{Classe} aj-aviso--frase" : Classe)" role="@Papel" tabindex="@(Revelar ? "-1" : null)" @ref="_frase"><Icone Nome="@NomeDoIcone" /><span>@ChildContent</span></p>
```

A caixa com ações não muda de marcação (já tem `tabindex="-1"` e `@ref="_caixa"`).

No `@code`, troque

```csharp
    private ElementReference _caixa;

    // O diálogo de alerta leva o foco a si mesmo quando aparece: é o foco que faz o leitor de tela anunciá-lo e a
    // página rolar até ele. Aviso sem Rotulo não mexe no foco, e a caixa que o recebe só existe com ações.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || Rotulo is null || Acoes is null) return;

        try
        {
            await _caixa.FocusAsync();
        }
```

por

```csharp
    /// <summary>
    /// O aviso que precisa ser visto (o resultado de uma ação, o erro de uma gravação): ao aparecer, ganha
    /// <c>tabindex="-1"</c> e recebe o foco, o que faz o leitor de tela anunciá-lo e a página rolar até ele. Aparecer é
    /// ser desenhado pela primeira vez com <c>Revelar</c> ou passar a <c>Revelar</c>; um desenho de novo com o mesmo
    /// aviso à vista não toma o foco. Para revelar uma mensagem nova num aviso que continua à vista, a tela troca a chave
    /// dele (<c>@key</c>) a cada mensagem.
    /// </summary>
    [Parameter] public bool Revelar { get; set; }

    private ElementReference _caixa;
    private ElementReference _frase;
    private bool _revelarAntes;
    private bool _focar;

    protected override void OnParametersSet()
    {
        if (Revelar && !_revelarAntes) _focar = true;
        _revelarAntes = Revelar;
    }

    // O aviso que se revela e o diálogo de alerta levam o foco a si mesmos quando aparecem: é o foco que faz o leitor
    // de tela anunciá-los e a página rolar até eles. O diálogo (Rotulo com ações) se foca sempre ao aparecer; o resto,
    // só com Revelar.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var confirmacao = firstRender && Rotulo is not null && Acoes is not null;
        if (!_focar && !confirmacao) return;

        _focar = false;
        try
        {
            await (Acoes is null ? _frase : _caixa).FocusAsync();
        }
```

O `catch` que vem depois fica como está (`InvalidOperationException`, `JSException`, `JSDisconnectedException`).

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AvisoInterativoTests"`
Expected: os seis passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ComponentesDoDesignSystemTests"`
Expected: todos passando, o novo e os de antes do `Aviso` sem mudança (`Aviso_traz_a_classe_o_papel_e_o_icone_do_tipo` continua achando `class="…"` e `role="…"` sem `tabindex`).

- [ ] **Step 9: Build e testes das telas**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build`
Expected: todos passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~Interativ|FullyQualifiedName~ComponentesDoDesignSystemTests|FullyQualifiedName~LayoutWebTests|FullyQualifiedName~TelasPurasTests|FullyQualifiedName~AvisoDeEventoSalvoTests"`
Expected: todos passando. Nenhuma tela usa `Revelar` ainda (as Tarefas 8 e 9 o ligam), então nenhum `VerifyFocusAsyncInvoke` existente muda de contagem; as telas que desenham o `FocoNoTitulo` só o fazem pelo `Routes`, que nenhum teste bUnit desenha.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~WebTests"`
Expected: todos passando (o `Foco` registrado no `Program.cs`; a pré-renderização com o `tabindex` só onde houver `Revelar`, que nenhuma tela usa ainda).

- [ ] **Step 10: Commit do aviso**

Confira o branch (`git branch --show-current` deve dizer `worktree-feat+fase-9g-limpeza`) e:

```
git add src/AtividadesJuridicas.Web/Components/Layout/Aviso.razor tests/AtividadesJuridicas.IntegrationTests/AvisoInterativoTests.cs tests/AtividadesJuridicas.IntegrationTests/ComponentesDoDesignSystemTests.cs
git commit -F <arquivo com a mensagem, UTF-8 sem BOM>
```

Mensagem:

```
feat(web): Aviso.Revelar leva o foco ao aviso quando ele aparece

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

### Task 4: `MenuDeAcoes` — Esc, Tab, um aberto por vez, foco de volta

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Layout/MenuDeAcoes.razor.js`, `src/AtividadesJuridicas.Web/Infra/MenuDeAcoesAberto.cs`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/MenuDeAcoes.razor`, `Program.cs`, `tests/AtividadesJuridicas.IntegrationTests/TelaInterativa.cs` e `TelaDeTeste.cs` (registro do `MenuDeAcoesAberto`)
- Test: `tests/AtividadesJuridicas.IntegrationTests/MenuDeAcoesInterativoTests.cs` (novo)

**Interfaces:**
- Consumes: o serviço `Foco` (Tarefa 3), se preciso.
- Produces: a mesma API de hoje (`Rotulo`, `ChildContent`); nenhuma tela muda de marcação além do menu. O invólucro continua `div.aj-menu-acoes` e o botão continua filho direto dele, com `aj-botao` (as Tarefas 8 e 9 devolvem o foco ao "⋯" por `.aj-menu-acoes > .aj-botao`).

**Regras:** Esc fecha com o foco em qualquer ponto do menu (botão ou lista) e devolve o foco ao botão; o foco saindo do menu (Tab depois do último item, Shift+Tab antes do botão, clique fora) fecha **sem mover o foco**, por um ouvinte `focusout` no invólucro (módulo `Layout/MenuDeAcoes.razor.js`, retorno a um `[JSInvokable]` do componente); abrir um menu fecha qualquer outro aberto (serviço de escopo do circuito `Infra/MenuDeAcoesAberto.cs`, registrado no `Program.cs`, no `TelaInterativa.cs` e no `TelaDeTeste.cs`; o `focusout` não cobre o Safari, em que o botão não recebe foco ao clique); o fundo continua fechando e devolvendo o foco; escolher um item fecha (o foco fica com o que a ação abrir).

**Testes obrigatórios:** bUnit para Esc com o foco no botão e na lista (foco pedido ao botão), o `[JSInvokable]` da saída fechando sem pedir foco, o módulo importado com a referência do invólucro, abrir um segundo menu fechando o primeiro, o fundo devolvendo o foco; o `focusout` real fica para a Tarefa 11; os testes existentes do menu (`ComponentesDoDesignSystemTests.Menu_de_acoes…`) e das telas que o usam sem mudança.

- [ ] **Step 1: Testes do menu de ações (devem falhar)**

Todos os arquivos tocados nesta tarefa são UTF-8 sem BOM com fim de linha CRLF; os novos seguem o mesmo.

Como o menu fecha:
- **Foco que sai do menu** (Tab depois do último item, Shift+Tab no botão, clique fora que leva o foco a outro elemento): o módulo `MenuDeAcoes.razor.js` põe no invólucro (`div.aj-menu-acoes`, que tem o botão, o fundo e a lista) um ouvinte `focusout`. Quando o foco vai para fora do invólucro (`relatedTarget` nulo ou fora dele) e o menu está aberto, o módulo chama `FocoSaiu`, um `[JSInvokable]` do componente, que fecha o menu **sem mover o foco**: o Tab segue o caminho natural. O ouvinte é instalado na primeira vez que o menu abre e é removido no `DisposeAsync`, que também libera o `DotNetObjectReference`.
- **Esc** (com o foco no botão, na lista ou num item) e o **fundo** fecham e devolvem o foco ao botão.
- **Escolher um item** fecha sem mexer no foco (o que a ação abre decide).
- **Um aberto por vez:** o serviço de escopo do circuito `MenuDeAcoesAberto` (`Web/Infra`) guarda o menu aberto. Abrir um menu fecha o anterior sem pedir foco para ele. O `focusout` não cobre o Safari, em que o botão não recebe foco ao clique, e o leitor de tela pode acionar um botão sem mover o foco.

O bUnit não move o foco nem dispara `focusout` de verdade. Por isso o teste prova três coisas: o módulo é importado e a instalação é chamada com o invólucro e a referência do componente; o `FocoSaiu` fecha sem pedir foco; os outros fechamentos pedem o foco ao botão. O `focusout` real fica para a Tarefa 11.

Crie `tests/AtividadesJuridicas.IntegrationTests/MenuDeAcoesInterativoTests.cs`:

```csharp
using AngleSharp.Dom;
using AtividadesJuridicas.Web.Components.Layout;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O menu de ações pelo teclado e pelo toque. Fecham o menu: o Esc (com o foco no botão, na lista ou num item) e o
/// toque no fundo, que devolvem o foco ao botão; o foco que sai do menu (o focusout do módulo, que chama FocoSaiu) e a
/// escolha de um item, que não mexem no foco. Abrir um menu fecha o que estava aberto.
/// O bUnit não dispara o focusout do navegador: o teste confere a instalação do módulo e chama o FocoSaiu como o módulo
/// chamaria. O Tab real, o Shift+Tab no botão e o clique fora ficam para a conferência no navegador.
/// </summary>
public class MenuDeAcoesInterativoTests : TelaInterativa
{
    private const string Modulo = "./Components/Layout/MenuDeAcoes.razor.js";

    private const string Itens =
        "<button type=\"button\" class=\"aj-menu-acoes__item\">Renomear</button>" +
        "<button type=\"button\" class=\"aj-menu-acoes__item aj-menu-acoes__item--perigo\">Excluir</button>";

    private IRenderedComponent<MenuDeAcoes> Menu(string rotulo = "Ações de modelo.pdf") =>
        Desenhar<MenuDeAcoes>(p => p.Add(m => m.Rotulo, rotulo).AddChildContent(Itens));

    // Sempre procurados de novo: o elemento achado antes de um evento não serve para disparar outro depois dele.
    private static IElement Botao(IRenderedComponent<MenuDeAcoes> menu) => menu.Find(".aj-menu-acoes > button[aria-expanded]");

    private static IElement Lista(IRenderedComponent<MenuDeAcoes> menu) => menu.Find(".aj-menu-acoes__lista");

    private static void DeveEstarAberto(IRenderedComponent<MenuDeAcoes> menu)
    {
        Botao(menu).GetAttribute("aria-expanded").Should().Be("true");
        Lista(menu).HasAttribute("hidden").Should().BeFalse();
        menu.FindAll(".aj-menu-acoes__fundo").Should().ContainSingle();
    }

    private static void DeveEstarFechado(IRenderedComponent<MenuDeAcoes> menu)
    {
        Botao(menu).GetAttribute("aria-expanded").Should().Be("false");
        Lista(menu).HasAttribute("hidden").Should().BeTrue();
        menu.FindAll(".aj-menu-acoes__fundo").Should().BeEmpty();
    }

    /// <summary>Abre pelo clique: o menu aberto e o foco pedido para a lista, o primeiro pedido de foco do teste.</summary>
    private IRenderedComponent<MenuDeAcoes> Aberto(string rotulo = "Ações de modelo.pdf")
    {
        var menu = Menu(rotulo);
        Botao(menu).Click();

        DeveEstarAberto(menu);
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(Lista(menu));
        return menu;
    }

    /// <summary>Dois pedidos de foco no teste: a lista, ao abrir, e depois o botão do menu.</summary>
    private void FocoDevolvidoAoBotao(IRenderedComponent<MenuDeAcoes> menu)
    {
        var focos = JSInterop.VerifyFocusAsyncInvoke(calledTimes: 2);
        focos[0].Arguments[0].ShouldBeElementReferenceTo(Lista(menu));
        focos[1].Arguments[0].ShouldBeElementReferenceTo(Botao(menu));
    }

    [Theory]
    [InlineData(".aj-menu-acoes > button[aria-expanded]")]   // o botão
    [InlineData(".aj-menu-acoes__lista")]                    // a lista, que recebe o foco ao abrir
    [InlineData(".aj-menu-acoes__item--perigo")]             // um item da lista
    public void Esc_fecha_com_o_foco_no_botao_ou_na_lista_e_devolve_o_foco_ao_botao(string ondeEstaOFoco)
    {
        var menu = Aberto();

        // A tecla sobe do elemento focado até o invólucro do menu, como no navegador.
        menu.Find(ondeEstaOFoco).KeyDown(new KeyboardEventArgs { Key = "Escape" });

        DeveEstarFechado(menu);
        FocoDevolvidoAoBotao(menu);
    }

    [Fact]
    public async Task O_foco_que_sai_do_menu_fecha_sem_pedir_foco()
    {
        var menu = Aberto();

        // O que o focusout do módulo faz quando o foco vai para fora do invólucro (Tab no último item, Shift+Tab no botão).
        await menu.Instance.FocoSaiuAsync();

        DeveEstarFechado(menu);
        // Só o pedido de foco da abertura: o Tab segue o caminho natural.
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(Lista(menu));
    }

    [Fact]
    public void Abrir_instala_a_vigia_do_foco_uma_vez_com_o_involucro_e_o_menu()
    {
        var modulo = JSInterop.SetupModule(Modulo);
        modulo.SetupVoid("vigiarSaida", _ => true).SetVoidResult();
        var menu = Menu();

        Botao(menu).Click();
        Botao(menu).KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Botao(menu).Click();

        // Uma instalação só, na primeira abertura, com o invólucro inteiro (botão, fundo e lista) e a referência do menu.
        var instalacao = modulo.VerifyInvoke("vigiarSaida");
        instalacao.Arguments[0].ShouldBeElementReferenceTo(menu.Find(".aj-menu-acoes"));
        instalacao.Arguments[1].Should().BeOfType<DotNetObjectReference<MenuDeAcoes>>()
            .Which.Value.Should().BeSameAs(menu.Instance);
        DeveEstarAberto(menu);
    }

    [Fact]
    public void Tocar_fora_fecha_e_devolve_o_foco_ao_botao()
    {
        var menu = Aberto();

        menu.Find(".aj-menu-acoes__fundo").Click();

        DeveEstarFechado(menu);
        FocoDevolvidoAoBotao(menu);
    }

    [Fact]
    public void Escolher_um_item_fecha_sem_pedir_o_foco()
    {
        var menu = Aberto();

        menu.Find(".aj-menu-acoes__item--perigo").Click();

        DeveEstarFechado(menu);
        // Só o pedido de foco da abertura: para onde o foco vai, decide o que a ação abre.
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(Lista(menu));
    }

    [Fact]
    public void Abrir_um_segundo_menu_fecha_o_primeiro_sem_mexer_no_foco_dele()
    {
        var primeiro = Aberto("Ações de modelo.pdf");
        var segundo = Menu("Ações de contrato.pdf");

        Botao(segundo).Click();

        DeveEstarFechado(primeiro);
        DeveEstarAberto(segundo);
        // O primeiro fecha sem pedir foco: o segundo pedido vai para a lista do segundo menu.
        var focos = JSInterop.VerifyFocusAsyncInvoke(calledTimes: 2);
        focos[0].Arguments[0].ShouldBeElementReferenceTo(Lista(primeiro));
        focos[1].Arguments[0].ShouldBeElementReferenceTo(Lista(segundo));
    }
}
```

Notas para quem implementa:
- `AddChildContent(string)` é do construtor de parâmetros do bUnit: os dois itens viram marcação fixa dentro da lista. Clicar ou teclar num item sobe (borbulha) até a lista e o invólucro, como no navegador.
- A `TelaInterativa` deixa o JavaScript em modo frouxo: a importação do módulo devolve um módulo frouxo, e a instalação não atrapalha os outros testes. No teste da instalação, o `SetupModule` registra as chamadas do módulo; o `SetVoidResult` conclui a chamada.
- `FocoSaiuAsync` passa pelo `InvokeAsync` do componente, então o teste pode chamá-lo direto da instância.
- Os dois menus do último teste são duas raízes do mesmo `BunitContext` e dividem o mesmo `MenuDeAcoesAberto` (registrado na `TelaInterativa` no Step 3).
- `VerifyFocusAsyncInvoke()` sem argumento exige exatamente uma chamada, e com `calledTimes: 2`, exatamente duas. É isso que prova que o foco que sai, a escolha de um item e o primeiro menu fechado pelo segundo não pedem foco.

- [ ] **Step 2: Rodar (RED)**

Run: `dotnet build -m:1`
Expected: não compila — `CS1061` em `MenuDeAcoesInterativoTests.cs` (`MenuDeAcoes` não tem `FocoSaiuAsync`). É o RED. Depois do Step 4, falhariam também a instalação (o módulo não existe) e o segundo menu (o primeiro continua aberto). O Esc, o fundo e o item descrevem o comportamento da 9B e da 9C, que não tinha teste interativo: ficam como guarda da reescrita.

- [ ] **Step 3: O serviço `MenuDeAcoesAberto` e o registro**

Crie `src/AtividadesJuridicas.Web/Infra/MenuDeAcoesAberto.cs`:

```csharp
namespace AtividadesJuridicas.Web.Infra;

/// <summary>
/// O menu de ações aberto agora no circuito. Abrir um menu fecha o que estava aberto antes: nunca ficam dois abertos,
/// nem quando o segundo se abre sem o foco sair do primeiro (no Safari o clique não foca o botão; o leitor de tela
/// aciona o botão sem mover o foco).
/// </summary>
public sealed class MenuDeAcoesAberto
{
    private object? _aberto;
    private Action? _fechar;

    /// <summary>Guarda o menu que acaba de abrir e fecha o que estava aberto antes dele (sem mexer no foco).</summary>
    public void Abrir(object menu, Action fechar)
    {
        var anterior = ReferenceEquals(_aberto, menu) ? null : _fechar;
        _aberto = menu;
        _fechar = fechar;
        anterior?.Invoke();
    }

    /// <summary>Esquece o menu que fechou (ou saiu da tela), se for ele o aberto.</summary>
    public void Esquecer(object menu)
    {
        if (!ReferenceEquals(_aberto, menu)) return;

        _aberto = null;
        _fechar = null;
    }
}
```

O serviço guarda o novo antes de o anterior fechar: se o fechamento do anterior chamar `Esquecer`, ele não apaga o novo.

Em `src/AtividadesJuridicas.Web/Program.cs`, logo depois de `builder.Services.AddScoped<Foco>();` (a linha que a Tarefa 3 pôs depois de `builder.Services.AddScoped<Recados>();`):

```csharp
builder.Services.AddScoped<MenuDeAcoesAberto>();
```

(`Program.cs` já usa o `Recados` e o `Foco` sem qualificar: o `using AtividadesJuridicas.Web.Infra;` existe.)

Em `tests/AtividadesJuridicas.IntegrationTests/TelaInterativa.cs`, no construtor, logo depois de `Services.AddSingleton<Foco>();` (a última linha do construtor, que a Tarefa 3 pôs):

```csharp
        Services.AddSingleton<MenuDeAcoesAberto>();
```

No resumo da classe, troque o que a Tarefa 3 deixou

```csharp
/// <summary>
/// Base dos testes que clicam: desenha um componente com o bUnit, com o despachante combinado do teste, o fuso do
/// escritório, os recados entre telas e o serviço de foco. O JavaScript fica solto (as chamadas de foco são registradas,
/// não executadas) e o teste confere o foco pelo registro: o pedido a um elemento (<see cref="UltimoFocoPedido"/>) ou ao
/// módulo de foco do layout (<see cref="SeletoresFocados"/>).
/// </summary>
```

por

```csharp
/// <summary>
/// Base dos testes que clicam: desenha um componente com o bUnit, com o despachante combinado do teste, o fuso do
/// escritório, os recados entre telas, o serviço de foco e o menu de ações aberto. O JavaScript fica solto (as chamadas
/// de foco são registradas, não executadas) e o teste confere o foco pelo registro: o pedido a um elemento
/// (<see cref="UltimoFocoPedido"/>) ou ao módulo de foco do layout (<see cref="SeletoresFocados"/>).
/// </summary>
```

Em `tests/AtividadesJuridicas.IntegrationTests/TelaDeTeste.cs`, as duas coleções de serviços ganham o serviço (o `MenuDeAcoes` o injeta; o desenho estático nunca o chama). Em `Servicos(...)` e em `RenderizarSemDadosAsync`, acrescente, logo depois da linha `.AddSingleton<Foco>()` que a Tarefa 3 pôs em cada uma:

```csharp
        .AddSingleton<MenuDeAcoesAberto>()
```

Use a indentação de cada coleção: 8 espaços em `Servicos`, 12 em `RenderizarSemDadosAsync`. No comentário acima de `Servicos`, troque o que a Tarefa 3 deixou

```csharp
    // Só o que os componentes da aplicação injetam: o despachante, o fuso do escritório, os recados entre telas, o
    // NavigationManager (as telas que navegam depois de salvar), o JavaScript (que o InputFile injeta e o desenho
    // estático nunca chama) e o Foco (que as telas chamam depois de desenhar, e o desenho estático também nunca chama).
    // Um ponto só para os desenhos com despachante.
```

por

```csharp
    // Só o que os componentes da aplicação injetam: o despachante, o fuso do escritório, os recados entre telas, o
    // NavigationManager (as telas que navegam depois de salvar), o JavaScript (que o InputFile injeta e o desenho
    // estático nunca chama), o Foco (que as telas chamam depois de desenhar, e o desenho estático também nunca chama)
    // e o menu de ações aberto (que o MenuDeAcoes só usa ao abrir). Um ponto só para os desenhos com despachante.
```

O `TelaDeTeste.cs` já tem `using AtividadesJuridicas.Web.Infra;`.

Sem esse registro, todo desenho de tela com menu (`Areas`, `Perfis`, documentos, ficha da atividade) e o `Menu_de_acoes_nasce_fechado_com_os_itens_no_html` falham com "Cannot provide a value for property 'MenuAberto'".

- [ ] **Step 4: O módulo e o `MenuDeAcoes`**

Crie `src/AtividadesJuridicas.Web/Components/Layout/MenuDeAcoes.razor.js`:

```js
// O menu de ações fecha quando o foco sai dele: Tab depois do último item, Shift+Tab no botão, clique fora que leva o
// foco a outro elemento. Fechar não move o foco (o Tab segue o caminho natural). O fundo e os itens estão dentro do
// invólucro, então o foco que passa por eles não conta como saída.
const vigias = new WeakMap();

export function vigiarSaida(involucro, menu) {
    if (!involucro || vigias.has(involucro)) return;

    const aoSair = evento => {
        const destino = evento.relatedTarget;
        if (destino && involucro.contains(destino)) return;
        // Com o menu fechado, sair do botão não é com o menu: nada a avisar.
        if (!involucro.querySelector(":scope > button[aria-expanded='true']")) return;

        menu.invokeMethodAsync("FocoSaiu").catch(() => { });
    };
    involucro.addEventListener("focusout", aoSair);
    vigias.set(involucro, aoSair);
}

export function pararDeVigiar(involucro) {
    const aoSair = involucro && vigias.get(involucro);
    if (!aoSair) return;

    involucro.removeEventListener("focusout", aoSair);
    vigias.delete(involucro);
}
```

Substitua `src/AtividadesJuridicas.Web/Components/Layout/MenuDeAcoes.razor` inteiro por:

```razor
@using Microsoft.JSInterop
@implements IAsyncDisposable
@inject IJSRuntime JS
@inject MenuDeAcoesAberto MenuAberto

@* O menu que o botão de três pontos abre, com as ações da linha (a destrutiva por último). É um "disclosure": o botão
   com aria-expanded e a lista de botões, sem role="menu" nem setas; o Tab e o Shift+Tab andam pelos itens.
   Fecha ao escolher (o clique do item sobe até a lista), ao tocar fora, com Esc (com o foco no botão ou na lista) e
   quando o foco sai do menu (o focusout do módulo, instalado na primeira abertura). Tocar fora e Esc devolvem o foco
   ao botão; escolher um item e o foco que sai não (o que a ação abre decide; o Tab segue o caminho natural).
   Abrir um menu fecha o que estiver aberto (MenuDeAcoesAberto, do circuito). *@
<div class="aj-menu-acoes" @ref="_involucro" @onkeydown="AoTeclarAsync">
    <button type="button" class="aj-botao aj-botao--fantasma aj-botao--icone" aria-label="@Rotulo"
            aria-expanded="@(_aberto ? "true" : "false")" @ref="_gatilho" @onclick="Alternar"><Icone Nome="mais" /></button>
    @if (_aberto)
    {
        <button type="button" class="aj-menu-acoes__fundo" tabindex="-1" aria-hidden="true" @onclick="FecharPeloFundoAsync"></button>
    }
    <div class="aj-menu-acoes__lista" hidden="@(!_aberto)" tabindex="-1" @ref="_lista" @onclick="Fechar">@ChildContent</div>
</div>

@code {
    /// <summary>O que o botão diz ao leitor de tela: "Ações de Procuração.pdf".</summary>
    [Parameter, EditorRequired] public string Rotulo { get; set; } = string.Empty;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    private bool _aberto;
    private bool _focarLista;
    private ElementReference _involucro;
    private ElementReference _gatilho;
    private ElementReference _lista;
    private IJSObjectReference? _modulo;
    private DotNetObjectReference<MenuDeAcoes>? _referencia;

    private void Alternar()
    {
        if (_aberto)
        {
            Fechar();
            return;
        }

        _aberto = true;
        _focarLista = true;
        MenuAberto.Abrir(this, FecharPorOutro);
    }

    private void Fechar()
    {
        _aberto = false;
        _focarLista = false;
        MenuAberto.Esquecer(this);
    }

    private async Task AoTeclarAsync(KeyboardEventArgs e)
    {
        if (e.Key != "Escape" || !_aberto) return;

        Fechar();
        await FocarAsync(_gatilho);
    }

    // O fundo só existe com o menu aberto. Devolve o foco mesmo que o foco que saiu já tenha fechado o menu (no Safari,
    // o toque fora tira o foco da lista antes do clique chegar ao fundo).
    private async Task FecharPeloFundoAsync()
    {
        Fechar();
        await FocarAsync(_gatilho);
    }

    /// <summary>O foco saiu do invólucro do menu (o módulo avisa): fecha sem mover o foco.</summary>
    [JSInvokable("FocoSaiu")]
    public Task FocoSaiuAsync() => InvokeAsync(() =>
    {
        if (!_aberto) return;

        Fechar();
        StateHasChanged();
    });

    // Outro menu abriu: este fecha sem mexer no foco, que vai para a lista do outro.
    private void FecharPorOutro() => _ = InvokeAsync(() =>
    {
        _aberto = false;
        _focarLista = false;
        StateHasChanged();
    });

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_focarLista) return;

        _focarLista = false;
        await FocarAsync(_lista);
        await VigiarSaidaAsync();
    }

    // Uma vez por menu, na primeira abertura: o menu que nunca abre não carrega o módulo.
    private async Task VigiarSaidaAsync()
    {
        if (_modulo is not null) return;

        try
        {
            _modulo = await JS.InvokeAsync<IJSObjectReference>("import", "./Components/Layout/MenuDeAcoes.razor.js");
            _referencia ??= DotNetObjectReference.Create(this);
            await _modulo.InvokeVoidAsync("vigiarSaida", _involucro, _referencia);
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSException or JSDisconnectedException or OperationCanceledException)
        {
            // Sem o módulo, o menu ainda fecha pelo Esc, pelo fundo e por um item.
        }
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

    // A linha some (excluída, outra página): o menu deixa de ser o aberto, o ouvinte sai e a referência é liberada.
    public async ValueTask DisposeAsync()
    {
        MenuAberto.Esquecer(this);
        if (_modulo is not null)
        {
            try
            {
                await _modulo.InvokeVoidAsync("pararDeVigiar", _involucro);
                await _modulo.DisposeAsync();
            }
            catch (Exception ex) when (ex is InvalidOperationException or JSException or JSDisconnectedException or OperationCanceledException)
            {
                // O circuito caiu ou o invólucro já saiu da página: não há ouvinte a tirar.
            }
        }

        _referencia?.Dispose();
    }
}
```

Notas:
- O ouvinte fica no invólucro inteiro. Ao abrir, o foco passa do botão para a lista (dentro); clicar no fundo no Chrome o foca (dentro). Nenhum dos dois fecha pelo `focusout`. Quem fecha nesses casos é o próprio fundo, que devolve o foco.
- No Esc e no fundo, o pedido de foco ao botão sai do servidor antes do lote de renderização que esconde a lista: o foco já está no botão quando a lista some, e o `focusout` vê o botão como destino (dentro). Na escolha de um item, a lista some com o item focado: o `focusout` chega com `relatedTarget` nulo e com o botão já em `aria-expanded="false"`, e o módulo não avisa.
- O módulo só avisa com o menu aberto (`aria-expanded="true"` no botão). Passar pelo botão fechado com o Tab não gera chamada ao servidor.
- Os testes estáticos de hoje continuam valendo: a marcação do menu não mudou (o `@ref` do invólucro não sai no HTML).
- A estrutura fica como está, e outras tarefas dependem dela: o invólucro do `focusout` é o próprio `div.aj-menu-acoes` (nenhum invólucro novo), e o botão do menu continua **filho direto** dele, com a classe `aj-botao` e o `aria-label` do `Rotulo`. As Tarefas 8 e 9 devolvem o foco ao "⋯" por `.aj-menu-acoes > .aj-botao` e por `.aj-menu-acoes > [aria-label="Mais ações"]`; o fundo (`aj-menu-acoes__fundo`) não tem a classe `aj-botao` e não casa com esses seletores.

- [ ] **Step 5: Build e testes (GREEN)**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~MenuDeAcoesInterativoTests"`
Expected: 8 passando (as 3 linhas do Esc e os 5 fatos).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ComponentesDoDesignSystemTests"`
Expected: todos passando, sem mudança em nenhum (`Menu_de_acoes_nasce_fechado_com_os_itens_no_html` com o serviço registrado no desenho estático).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AreasAtuacaoTests|FullyQualifiedName~PerfisTests|FullyQualifiedName~PerfisWebTests|FullyQualifiedName~DocumentosComunsInterativosTests|FullyQualifiedName~DocumentosDaFicha|FullyQualifiedName~DocumentosDaAtividadeInterativosTests|FullyQualifiedName~DocumentosDoPortalWebTests|FullyQualifiedName~AtividadeFicha|FullyQualifiedName~ClienteFicha"`
Expected: todos passando, sem mudança (as telas que usam o menu, por HTTP e por clique; nelas, a importação do módulo cai no modo frouxo).

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Expected: passando (nenhuma classe nova; o serviço não consulta nada).

Fica para o navegador (Tarefa 11), nas cinco telas com menu (Áreas, Perfis, Documentos comuns, ficha do cliente, ficha da atividade — "Mais ações" e "⋯" dos documentos), no tablet e no mobile:
- O Tab no último item fecha o menu e o foco segue para o elemento seguinte; o Shift+Tab no botão aberto fecha e o foco volta ao anterior.
- Abrir pelo teclado e pelo mouse não fecha sozinho: o foco passa do botão para a lista dentro do invólucro.
- O toque fora fecha e devolve o foco ao botão no Chrome e no Safari.
- Com NVDA e com VoiceOver, Enter no botão abre e o menu continua aberto.
- Não fica nenhum ouvinte depois de excluir a linha.

- [ ] **Step 6: Commit**

```powershell
git branch --show-current
git add src/AtividadesJuridicas.Web/Components/Layout/MenuDeAcoes.razor src/AtividadesJuridicas.Web/Components/Layout/MenuDeAcoes.razor.js src/AtividadesJuridicas.Web/Infra/MenuDeAcoesAberto.cs src/AtividadesJuridicas.Web/Program.cs tests/AtividadesJuridicas.IntegrationTests/MenuDeAcoesInterativoTests.cs tests/AtividadesJuridicas.IntegrationTests/TelaInterativa.cs tests/AtividadesJuridicas.IntegrationTests/TelaDeTeste.cs
$msg = @'
fix(web): menu de ações fecha quando o foco sai dele e com Esc, devolve o foco ao botão no Esc e no fundo, e nunca fica aberto junto de outro

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
'@
$f = Join-Path $env:TEMP 'mensagem-tarefa-4.txt'
[System.IO.File]::WriteAllText($f, $msg, (New-Object System.Text.UTF8Encoding $false))
git commit -F $f
```

Expected do `git branch --show-current`: `worktree-feat+fase-9g-limpeza`.

### Task 5: `Abas` — `tablist` com setas, Home/End e `tabindex` móvel

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Layout/Abas.razor.js` (impede a rolagem padrão de ←/→/Home/End só nas abas)
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/Abas.razor`, `Aba.cs`; os testes das três fichas que leem as abas; as três fichas (`Pages/AtividadeFicha.razor`, `ClienteFicha.razor`, `ProcessoFicha.razor`) só para dar `id` aos painéis e o `aria-controls`
- Test: `tests/AtividadesJuridicas.IntegrationTests/AbasInterativasTests.cs`

**Interfaces:**
- Produces: `Aba` ganha o `id` do painel que controla (ex.: `record Aba(string Chave, string Rotulo, int? Contador = null, string? Painel = null)`); `Abas` emite `aria-controls` quando `Painel` vem, `tabindex="0"` só na escolhida (`-1` nas outras) e trata ←/→/Home/End.

**Regras:** ←/→ movem para a aba anterior/seguinte com volta nas pontas, escolhem e levam o foco a ela; Home/End vão à primeira/última; os painéis das fichas ganham `id` estável; nada muda no tablet e no desktop.

**Testes obrigatórios:** bUnit: `tabindex` (só a escolhida com `0`), cada tecla escolhendo a aba certa e pedindo o foco a ela, as pontas, o `aria-controls` apontando para um `id` que existe nas três fichas.

- [ ] **Step 1: Testes das abas e dos painéis das fichas (devem falhar)**

Todos os arquivos tocados nesta tarefa são UTF-8 sem BOM com fim de linha CRLF; o novo (`Abas.razor.js`) segue o mesmo.

**Onde fica o `id` dos painéis.** O `id` vai no invólucro `aj-aba-painel` que a ficha já tem para cada aba, e só nele. Esses invólucros existem uma vez só por painel na marcação das três visões, então nenhum `id` se repete:
- `ClienteFicha`: os quatro invólucros (`dados`, `processos`, `documentos`, `usuarios`) são a única cópia dos painéis, comum às três visões (o `aj-lado-a-lado` reorganiza no desktop).
- `AtividadeFicha`: `comentarios` e `documentos` são a única cópia, comum às três visões; `detalhes` e `historico` ficam no bloco `aj-tablet-mobile`, e o desktop desenha as suas cópias (`aj-so-desktop`) sem invólucro de aba e sem `id`.
- `ProcessoFicha`: os invólucros ficam todos no bloco `aj-tablet-mobile`; o desktop (`aj-so-desktop`) desenha as suas cópias sem invólucro e sem `id`. A aba "Dados" mostra **dois** invólucros (Dados e, depois de Clientes, Observações, na ordem do tablet): cada um ganha o seu `id`, e a aba aponta para os dois (`aria-controls` aceita uma lista de `id` separados por espaço).

Um `id` por visão não serviria: o `aria-controls` aponta para um `id` só (ou uma lista fixa), e as cópias do desktop não estão sob nenhuma aba. Os painéis não ganham `role="tabpanel"` (Decisões do plano).

Os `id` (no estilo dos que o código já usa, como `filtros-da-agenda`):

| Ficha | Aba → `id` do invólucro |
| --- | --- |
| Atividade | `detalhes` → `detalhes-da-atividade`; `comentarios` → `comentarios-da-atividade`; `documentos` → `documentos-da-atividade`; `historico` → `historico-da-atividade` |
| Cliente | `dados` → `dados-do-cliente`; `processos` → `processos-do-cliente`; `documentos` → `documentos-do-cliente`; `usuarios` → `usuarios-do-cliente` |
| Processo | `dados` → `dados-do-processo` e `observacoes-do-processo`; `clientes` → `clientes-do-processo`; `situacao` → `situacao-do-processo` |

No invólucro, o `id` vem **depois** da `class` (`<div class="aj-aba-painel" id="dados-do-cliente">`). Os testes de hoje que procuram `<div class="aj-aba-painel">` ou `<div class="aj-aba-painel aj-aba-painel--oculta">` seguidos do painel passam a procurar a forma com o `id` do painel certo: provam o mesmo (qual painel está à vista e quais estão ocultos) e, de quebra, o `id`.

**`tests/AtividadesJuridicas.IntegrationTests/AbasInterativasTests.cs`** — substitua o arquivo inteiro (o teste que já existe fica igual):

```csharp
using AngleSharp.Dom;
using AtividadesJuridicas.Web.Components.Layout;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// As abas do mobile das fichas, no padrão tablist: só a escolhida entra no Tab; ←/→ (com volta nas pontas), Home e End
/// escolhem a aba e pedem o foco para ela; cada aba aponta (aria-controls) para os invólucros de painel dela. O bUnit
/// prova o foco pedido; o foco que chega, a fileira que rola até a aba e a página que não rola com as teclas ficam para
/// a conferência no navegador.
/// </summary>
public class AbasInterativasTests : TelaInterativa
{
    private const string Modulo = "./Components/Layout/Abas.razor.js";

    private static readonly List<Aba> TresAbas =
    [
        new("dados", "Dados", Painel: "dados-do-processo observacoes-do-processo"),
        new("clientes", "Clientes", 1, "clientes-do-processo"),
        new("situacao", "Situação"),
    ];

    private string? _escolhida;

    private IRenderedComponent<Abas> AsAbas(string ativa) => Desenhar<Abas>(p => p
        .Add(a => a.Rotulo, "Assuntos do processo")
        .Add(a => a.Ativa, ativa)
        .Add(a => a.Itens, TresAbas)
        .Add(a => a.AoEscolher, (string chave) => _escolhida = chave));

    // Sempre procurados de novo: o elemento achado antes de um evento não serve para disparar outro depois dele.
    private static IReadOnlyList<IElement> Botoes(IRenderedComponent<Abas> abas) => abas.FindAll("button[role=tab]");

    private static IElement Escolhida(IRenderedComponent<Abas> abas) => abas.Find("button[aria-selected=true]");

    [Fact]
    public void Tocar_numa_aba_avisa_a_chave_dela()
    {
        string? escolhida = null;
        var abas = Desenhar<Abas>(p => p
            .Add(a => a.Rotulo, "Assuntos do processo")
            .Add(a => a.Ativa, "dados")
            .Add(a => a.Itens, new List<Aba> { new("dados", "Dados"), new("clientes", "Clientes", 1), new("situacao", "Situação") })
            .Add(a => a.AoEscolher, (string chave) => escolhida = chave));

        abas.FindAll("button[role=tab]")[2].Click();

        escolhida.Should().Be("situacao");
    }

    [Fact]
    public void So_a_aba_escolhida_entra_no_tab()
    {
        var abas = AsAbas("clientes");

        Botoes(abas).Select(b => b.GetAttribute("tabindex")).Should().Equal("-1", "0", "-1");
        Escolhida(abas).TextContent.Should().Be("Clientes1");
    }

    [Fact]
    public void A_aba_aponta_para_os_paineis_dela_e_sem_painel_nao_aponta()
    {
        var botoes = Botoes(AsAbas("dados"));

        botoes[0].GetAttribute("aria-controls").Should().Be("dados-do-processo observacoes-do-processo");
        botoes[1].GetAttribute("aria-controls").Should().Be("clientes-do-processo");
        botoes[2].HasAttribute("aria-controls").Should().BeFalse();
    }

    [Theory]
    [InlineData("clientes", "ArrowRight", 2)]
    [InlineData("situacao", "ArrowRight", 0)]   // da última, volta para a primeira
    [InlineData("clientes", "ArrowLeft", 0)]
    [InlineData("dados", "ArrowLeft", 2)]       // da primeira, volta para a última
    [InlineData("clientes", "Home", 0)]
    [InlineData("clientes", "End", 2)]
    public void Tecla_escolhe_a_aba_e_pede_o_foco_para_ela(string ativa, string tecla, int destino)
    {
        var abas = AsAbas(ativa);

        Escolhida(abas).KeyDown(new KeyboardEventArgs { Key = tecla });

        _escolhida.Should().Be(TresAbas[destino].Chave);
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(Botoes(abas)[destino]);
    }

    [Fact]
    public void Outras_teclas_e_as_setas_com_alt_ficam_com_o_navegador()
    {
        var abas = AsAbas("clientes");

        Escolhida(abas).KeyDown(new KeyboardEventArgs { Key = "Tab" });
        Escolhida(abas).KeyDown(new KeyboardEventArgs { Key = "ArrowLeft", AltKey = true });   // Alt+← volta a página
        _escolhida.Should().BeNull();

        Escolhida(abas).KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        _escolhida.Should().Be("dados");
        // Um pedido de foco só: o da seta sem Alt.
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(Botoes(abas)[0]);
    }

    [Fact]
    public void As_teclas_das_abas_nao_rolam_a_pagina()
    {
        var modulo = JSInterop.SetupModule(Modulo);
        modulo.SetupVoid("prenderTeclas", _ => true).SetVoidResult();

        var abas = AsAbas("dados");

        modulo.VerifyInvoke("prenderTeclas").Arguments[0].ShouldBeElementReferenceTo(abas.Find("[role=tablist]"));
    }

    /// <summary>
    /// Cada aba da ficha aponta (aria-controls) para os invólucros de painel dela, na ordem das abas, e cada id existe uma
    /// vez só na marcação das três visões, num invólucro aj-aba-painel. Usado pelos testes das três fichas.
    /// </summary>
    internal static void CadaAbaApontaParaOsPaineisDela<T>(IRenderedComponent<T> ficha, params string[] paineisPorAba)
        where T : IComponent
    {
        ficha.FindAll("[role=tablist] button[role=tab]").Select(a => a.GetAttribute("aria-controls"))
            .Should().Equal(paineisPorAba);
        foreach (var id in paineisPorAba.SelectMany(p => p.Split(' ')))
            ficha.FindAll($"#{id}").Should().ContainSingle($"o painel {id} existe uma vez só")
                .Which.ClassList.Should().Contain("aj-aba-painel");
    }
}
```

**`tests/AtividadesJuridicas.IntegrationTests/AtividadeFichaInterativaTests.cs`** — em `Tocar_numa_aba_mostra_o_painel_dela_e_esconde_os_outros_no_mobile`, troque as quatro expressões regulares (linhas 69 a 72) por:

```csharp
            .MatchRegex("<div class=\"aj-aba-painel\" id=\"historico-da-atividade\">\\s*<section class=\"aj-painel aj-painel--ficha aj-painel--historico\">\\s*<h2>Histórico</h2>")
            .And.MatchRegex("<div class=\"aj-aba-painel aj-aba-painel--oculta\" id=\"detalhes-da-atividade\">\\s*<div class=\"aj-grade\">")
            .And.MatchRegex("<div class=\"aj-aba-painel aj-aba-painel--oculta\" id=\"comentarios-da-atividade\">\\s*<section class=\"aj-painel[^\"]*\">\\s*<h2>Comentários</h2>")
            .And.MatchRegex("<div class=\"aj-aba-painel aj-aba-painel--oculta\" id=\"documentos-da-atividade\">\\s*<section class=\"aj-painel[^\"]*\">\\s*<div class=\"aj-painel__cabecalho\">\\s*<h2>Documentos</h2>");
```

e, logo depois desse teste:

```csharp
    [Fact]
    public void Cada_aba_aponta_para_os_paineis_dela()
    {
        Combinar();

        var ficha = Ficha();

        AbasInterativasTests.CadaAbaApontaParaOsPaineisDela(ficha,
            "detalhes-da-atividade", "comentarios-da-atividade", "documentos-da-atividade", "historico-da-atividade");
    }
```

**`tests/AtividadesJuridicas.IntegrationTests/AtividadeFichaWebTests.cs`** — em `Diretoria_ve_o_cabecalho_os_detalhes_os_responsaveis_e_o_historico_em_cada_visao`, troque o começo da expressão da linha 110, `"(?s)<div class=\"aj-aba-painel\">\\s*<div class=\"aj-grade\">` por `"(?s)<div class=\"aj-aba-painel\" id=\"detalhes-da-atividade\">\\s*<div class=\"aj-grade\">` (o resto da linha e as linhas 111 a 116 ficam iguais), e a linha 118 por:

```csharp
            .And.MatchRegex("<div class=\"aj-aba-painel aj-aba-painel--oculta\" id=\"historico-da-atividade\">\\s*<section class=\"aj-painel aj-painel--ficha aj-painel--historico\">\\s*<h2>Histórico</h2>");
```

**`tests/AtividadesJuridicas.IntegrationTests/ClienteFichaWebTests.cs`** — em `Diretoria_ve_o_cabecalho_os_dados_os_processos_e_as_acoes`, troque as linhas 66 e 67 por:

```csharp
        html.Should().MatchRegex("<div class=\"aj-aba-painel aj-aba-painel--oculta\" id=\"processos-do-cliente\">\\s*<section class=\"aj-painel\">\\s*<h2>Processos do cliente</h2>")
            .And.MatchRegex("<div class=\"aj-aba-painel\" id=\"dados-do-cliente\">\\s*<section class=\"aj-painel\">\\s*<h2>Dados</h2>");
```

**`tests/AtividadesJuridicas.IntegrationTests/ClienteFichaInterativaTests.cs`** — acrescente `using Microsoft.AspNetCore.Components.Web;` depois de `using Bunit;`. Em `Tocar_numa_aba_mostra_os_paineis_dela_e_esconde_os_outros_no_mobile`, troque as linhas 78 e 79 por:

```csharp
        ficha.Markup.Should().MatchRegex("<div class=\"aj-aba-painel aj-aba-painel--oculta\" id=\"dados-do-cliente\">\\s*<section class=\"aj-painel\">\\s*<h2>Dados</h2>")
            .And.MatchRegex("<div class=\"aj-aba-painel\" id=\"documentos-do-cliente\">\\s*(<[^>]+>\\s*)*?<section class=\"aj-painel\">\\s*<div class=\"aj-painel__cabecalho\">\\s*<h2>Documentos</h2>");
```

e, logo depois desse teste:

```csharp
    [Fact]
    public void Cada_aba_aponta_para_os_paineis_dela()
    {
        Combinar();

        var ficha = Ficha();

        AbasInterativasTests.CadaAbaApontaParaOsPaineisDela(ficha,
            "dados-do-cliente", "processos-do-cliente", "documentos-do-cliente", "usuarios-do-cliente");
    }

    [Fact]
    public void Seta_para_a_direita_escolhe_a_aba_seguinte_mostra_o_painel_dela_e_leva_o_foco()
    {
        Combinar();
        var ficha = Ficha();

        ficha.Find(".aj-abas button[aria-selected=true]").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        var escolhida = ficha.Find(".aj-abas button[aria-selected=true]");
        escolhida.TextContent.Should().Be("Processos0");
        // O tabindex móvel acompanha a escolha: a nova entra no Tab, a de antes sai.
        escolhida.GetAttribute("tabindex").Should().Be("0");
        ficha.Find(".aj-abas button[aria-controls=dados-do-cliente]").GetAttribute("tabindex").Should().Be("-1");
        ficha.Find("#processos-do-cliente").ClassList.Should().NotContain("aj-aba-painel--oculta").And.Contain("aj-aba-painel");
        ficha.Find("#dados-do-cliente").ClassList.Should().Contain("aj-aba-painel--oculta");
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(escolhida);
    }
```

**`tests/AtividadesJuridicas.IntegrationTests/ProcessoFichaInterativaTests.cs`** — em `Alterar_situacao_na_barra_do_mobile_abre_a_aba_situacao`, troque a linha 56 por:

```csharp
        ficha.Markup.Should().MatchRegex("<div class=\"aj-aba-painel\" id=\"situacao-do-processo\">\\s*<section class=\"aj-painel\">\\s*<h2>Situação</h2>");
```

e, logo depois desse teste:

```csharp
    [Fact]
    public void Cada_aba_aponta_para_os_paineis_dela()
    {
        var ficha = Ficha();

        // A aba Dados mostra dois invólucros: Dados e Observações (que no tablet vem depois de Clientes).
        AbasInterativasTests.CadaAbaApontaParaOsPaineisDela(ficha,
            "dados-do-processo observacoes-do-processo", "clientes-do-processo", "situacao-do-processo");
    }
```

Notas:
- O `Ficha()` do processo é da Diretoria (tem a aba Situação). O processo do teste não tem observações: o invólucro `observacoes-do-processo` existe vazio, e é isso que a conferência pede (o `id` para onde a aba aponta existe).
- O `ComponentesDoDesignSystemTests.Abas_marcam_a_escolhida_e_trazem_o_contador`, o `ClienteFichaWebTests` (linha 63) e o `AtividadeFichaWebTests` (linha 128) não mudam: o `tabindex` e o `aria-controls` vêm depois do `aria-selected`, dentro do `[^>]*` que eles já aceitam.
- No `Seta_para_a_direita_…`, a contagem exata de um pedido de foco vale porque a ficha do cliente não pede foco ao carregar (as Tarefas 3 e 4 não mudam isso: o `Aviso.Revelar` ainda não está ligado em tela nenhuma, e o menu de ações só pede foco ao abrir).

- [ ] **Step 2: Rodar (RED)**

Run: `dotnet build -m:1`
Expected: não compila — `CS1739` em `AbasInterativasTests.cs` (o `Aba` não tem parâmetro `Painel`). É o RED.

- [ ] **Step 3: `Aba` ganha o painel, e o RED de comportamento**

Substitua `src/AtividadesJuridicas.Web/Components/Layout/Aba.cs` inteiro por:

```csharp
namespace AtividadesJuridicas.Web.Components.Layout;

/// <summary>
/// Uma aba da ficha: a chave que a tela guarda, o rótulo, quando houver a contagem ao lado dele e o id do invólucro do
/// painel que ela mostra (vai no aria-controls; mais de um id, separados por espaço, quando a aba mostra dois invólucros).
/// </summary>
public sealed record Aba(string Chave, string Rotulo, int? Contador = null, string? Painel = null);
```

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AbasInterativasTests|FullyQualifiedName~AtividadeFicha|FullyQualifiedName~ClienteFicha|FullyQualifiedName~ProcessoFicha"`
Expected: falham `So_a_aba_escolhida_entra_no_tab` e `A_aba_aponta_…` (sem `tabindex` e sem `aria-controls`), as 6 linhas de `Tecla_escolhe_…` e `Outras_teclas_…` (`MissingEventHandlerException`: as abas não tratam teclas), `As_teclas_das_abas_nao_rolam_a_pagina` (o módulo não é chamado), os três `Cada_aba_aponta_para_os_paineis_dela` e `Seta_para_a_direita_…`, e os cinco testes com a expressão reescrita (`AtividadeFichaInterativaTests.Tocar_numa_aba_…`, `AtividadeFichaWebTests.Diretoria_ve_…`, `ClienteFichaWebTests.Diretoria_ve_…`, `ClienteFichaInterativaTests.Tocar_numa_aba_…`, `ProcessoFichaInterativaTests.Alterar_situacao_na_barra_…`: os invólucros ainda não têm `id`). Os outros passam.

- [ ] **Step 4: As abas — `tabindex` móvel, `aria-controls`, ←/→/Home/End e o módulo que prende as teclas**

O `@onkeydown` do Blazor não consegue impedir a ação padrão só de algumas teclas (o `@onkeydown:preventDefault` vale para todas, e tiraria o Tab, o Enter e o Espaço do botão). Sem impedir, o Home e o End rolam o `main` até o topo e o fim antes de o foco chegar à aba, e as setas rolam a fileira. Por isso um módulo pequeno, ao lado do componente, impede a ação padrão só de ←, →, Home e End (sem Alt, Ctrl e Cmd, para o Alt+← continuar voltando a página); quem escolhe a aba e move o foco é o componente.

Crie `src/AtividadesJuridicas.Web/Components/Layout/Abas.razor.js`:

```js
// As teclas que as abas tratam (←, →, Home e End) não rolam a fileira nem a página: o componente escolhe a aba e leva
// o foco a ela. Com Alt, Ctrl ou Cmd a tecla continua do navegador (Alt+← volta a página).
const teclas = new Set(["ArrowLeft", "ArrowRight", "Home", "End"]);
const presas = new WeakSet();

export function prenderTeclas(lista) {
    if (!lista || presas.has(lista)) return;

    presas.add(lista);
    lista.addEventListener("keydown", evento => {
        if (teclas.has(evento.key) && !evento.altKey && !evento.ctrlKey && !evento.metaKey) evento.preventDefault();
    });
}
```

Substitua `src/AtividadesJuridicas.Web/Components/Layout/Abas.razor` inteiro por:

```razor
@inject IJSRuntime JS

@* As abas do mobile das fichas, no padrão tablist: só a aba escolhida entra no Tab (tabindex 0, as outras -1); ←/→
   andam para a aba anterior/seguinte, com volta nas pontas, e Home/End vão à primeira/última, escolhendo a aba e
   levando o foco a ela. Cada painel da ficha fica num invólucro aj-aba-painel com id, que a tela marca com
   aj-aba-painel--oculta quando não é da aba escolhida; a aba aponta para ele com aria-controls. No tablet e no desktop
   todos os painéis aparecem, sem abas, e por isso não levam role="tabpanel". *@
<div class="aj-abas" role="tablist" aria-label="@Rotulo" @ref="_lista">
    @foreach (var aba in Itens)
    {
        var escolhida = aba.Chave == Ativa;
        <button type="button" role="tab" class="aj-abas__aba" aria-selected="@(escolhida ? "true" : "false")" tabindex="@(escolhida ? "0" : "-1")" aria-controls="@aba.Painel" @key="aba.Chave" @ref="_botoes[aba.Chave]" @onclick="() => AoEscolher.InvokeAsync(aba.Chave)" @onkeydown="e => AoTeclarAsync(e, aba.Chave)">@aba.Rotulo@if (aba.Contador is { } contador)
            {<span class="aj-abas__contador">@contador</span>}</button>
    }
</div>

@code {
    [Parameter, EditorRequired] public IReadOnlyList<Aba> Itens { get; set; } = [];
    [Parameter, EditorRequired] public string Ativa { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> AoEscolher { get; set; }

    /// <summary>O nome do grupo para o leitor de tela: "Assuntos do cliente".</summary>
    [Parameter, EditorRequired] public string Rotulo { get; set; } = string.Empty;

    private readonly Dictionary<string, ElementReference> _botoes = [];
    private ElementReference _lista;

    // A tecla vale a partir da aba onde está o foco. Com Alt, Ctrl ou Cmd, a tecla é do navegador.
    private async Task AoTeclarAsync(KeyboardEventArgs e, string chave)
    {
        if (e.AltKey || e.CtrlKey || e.MetaKey || Itens.Count == 0) return;

        var atual = IndiceDe(chave);
        var destino = e.Key switch
        {
            "ArrowRight" => (atual + 1) % Itens.Count,
            "ArrowLeft" => (atual + Itens.Count - 1) % Itens.Count,
            "Home" => 0,
            "End" => Itens.Count - 1,
            _ => -1,
        };
        if (destino < 0) return;

        var aba = Itens[destino].Chave;
        await AoEscolher.InvokeAsync(aba);
        if (_botoes.TryGetValue(aba, out var botao)) await FocarAsync(botao);
    }

    private int IndiceDe(string chave)
    {
        for (var i = 0; i < Itens.Count; i++)
        {
            if (Itens[i].Chave == chave) return i;
        }

        return 0;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        try
        {
            await using var modulo = await JS.InvokeAsync<IJSObjectReference>("import", "./Components/Layout/Abas.razor.js");
            await modulo.InvokeVoidAsync("prenderTeclas", _lista);
        }
        catch (JSDisconnectedException)
        {
            // O circuito caiu: não há página para rolar.
        }
        catch (JSException)
        {
            // Sem o módulo, as teclas ainda escolhem a aba; só a rolagem padrão do navegador volta.
        }
    }

    private static async Task FocarAsync(ElementReference alvo)
    {
        try
        {
            await alvo.FocusAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSException or JSDisconnectedException)
        {
            // Sem foco (circuito caindo, por exemplo) a aba escolhida continua valendo.
        }
    }
}
```

Notas:
- O `tabindex` e o `aria-controls` vêm depois do `aria-selected`: os testes de marcação de hoje (`aria-selected="true"[^>]*>Dados</button>`) continuam valendo. `aria-controls="@aba.Painel"` com `Painel` nulo não sai no HTML.
- O foco é pedido ao botão que já existe (as abas têm `@key`; a escolha só muda atributos), logo depois de avisar a tela; a tela redesenha e o `tabindex="0"` passa para a aba nova.
- `Microsoft.JSInterop` e `Microsoft.AspNetCore.Components.Web` vêm do `_Imports.razor`. O desenho estático (`TelaDeTeste`, pré-renderização) não roda o `OnAfterRenderAsync`; no bUnit em modo frouxo, a importação devolve um módulo frouxo.

- [ ] **Step 5: Os painéis das três fichas com `id`, e as abas apontando para eles**

**`src/AtividadesJuridicas.Web/Components/Pages/AtividadeFicha.razor`** — os quatro invólucros:

- `<div class="@ClasseDaAba("detalhes")">` → `<div class="@ClasseDaAba("detalhes")" id="detalhes-da-atividade">`
- `<div class="@ClasseDaAba("historico")">@PainelDeHistorico(a)</div>` → `<div class="@ClasseDaAba("historico")" id="historico-da-atividade">@PainelDeHistorico(a)</div>`
- `<div class="@ClasseDaAba("comentarios")">` → `<div class="@ClasseDaAba("comentarios")" id="comentarios-da-atividade">`
- `<div class="@ClasseDaAba("documentos")">` → `<div class="@ClasseDaAba("documentos")" id="documentos-da-atividade">`

e `AbasDaFicha`:

```csharp
    private IReadOnlyList<Aba> AbasDaFicha =>
    [
        new("detalhes", "Detalhes", Painel: "detalhes-da-atividade"),
        new("comentarios", "Comentários", _totalDeComentarios, "comentarios-da-atividade"),
        new("documentos", "Documentos", _totalDeDocumentos, "documentos-da-atividade"),
        new("historico", "Histórico", Painel: "historico-da-atividade"),
    ];
```

**`src/AtividadesJuridicas.Web/Components/Pages/ClienteFicha.razor`** — os quatro invólucros:

- `<div class="@ClasseDaAba("dados")">` → `<div class="@ClasseDaAba("dados")" id="dados-do-cliente">`
- `<div class="@ClasseDaAba("processos")">` → `<div class="@ClasseDaAba("processos")" id="processos-do-cliente">`
- `<div class="@ClasseDaAba("documentos")">` → `<div class="@ClasseDaAba("documentos")" id="documentos-do-cliente">`
- `<div class="@ClasseDaAba("usuarios")">` → `<div class="@ClasseDaAba("usuarios")" id="usuarios-do-cliente">`

e, em `AbasDaFicha`:

```csharp
            List<Aba> abas =
            [
                new("dados", "Dados", Painel: "dados-do-cliente"),
                new("processos", "Processos", _erroDosProcessos is null ? _processos.Total : null, "processos-do-cliente"),
                new("documentos", "Documentos", _totalDeDocumentos, "documentos-do-cliente"),
            ];
            if (Diretoria) abas.Add(new("usuarios", "Usuários", _erroDosUsuarios is null ? _usuarios.Count : null, "usuarios-do-cliente"));
```

**`src/AtividadesJuridicas.Web/Components/Pages/ProcessoFicha.razor`** — os quatro invólucros do bloco `aj-tablet-mobile` (o bloco `aj-so-desktop` não muda):

- `<div class="@ClasseDaAba("dados")">@Dados</div>` → `<div class="@ClasseDaAba("dados")" id="dados-do-processo">@Dados</div>`
- `<div class="@ClasseDaAba("clientes")">` → `<div class="@ClasseDaAba("clientes")" id="clientes-do-processo">`
- `<div class="@ClasseDaAba("dados")">@Observacoes</div>` → `<div class="@ClasseDaAba("dados")" id="observacoes-do-processo">@Observacoes</div>`
- `<div class="@ClasseDaAba("situacao")">@Situacao</div>` → `<div class="@ClasseDaAba("situacao")" id="situacao-do-processo">@Situacao</div>`

e, em `AbasDaFicha`:

```csharp
            List<Aba> abas =
            [
                new("dados", "Dados", Painel: "dados-do-processo observacoes-do-processo"),
                new("clientes", "Clientes", _processo!.Clientes.Count, "clientes-do-processo"),
            ];
            if (Diretoria) abas.Add(new("situacao", "Situação", Painel: "situacao-do-processo"));
```

Nada mais muda nas fichas: os textos, as classes, a ordem dos painéis e o `ClasseDaAba` ficam como estão. O invólucro continua com a classe de aba como única classe (o `id` não é classe).

- [ ] **Step 6: Build e testes (GREEN)**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AbasInterativasTests"`
Expected: 11 passando (5 fatos e as 6 linhas da theory).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadeFicha|FullyQualifiedName~ClienteFicha|FullyQualifiedName~ProcessoFicha|FullyQualifiedName~ComponentesDoDesignSystemTests"`
Expected: todos passando (os novos das fichas, os cinco com a expressão reescrita e os de marcação das abas sem mudança).

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Expected: passando (nenhuma classe nova; `id` não entra na trava).

Fica para o navegador (Tarefa 11), nas três fichas a 390px: o Tab entra na fileira pela aba escolhida e sai dela no Tab seguinte; ←/→, Home e End movem o foco e mostram o painel; a fileira rola até a aba focada (a "Histórico" da atividade fica fora da tela a 390px) e a página não rola com Home e End; Alt+← continua voltando a página; o leitor de tela anuncia "guia, 2 de 4, selecionada" e o `aria-controls` aparece na árvore de acessibilidade (o invólucro é `display: contents`: conferir se o Chrome mantém a relação com um alvo assim); no tablet e no desktop, nada muda.

- [ ] **Step 7: Commit**

```powershell
git branch --show-current
git add src/AtividadesJuridicas.Web/Components/Layout/Aba.cs src/AtividadesJuridicas.Web/Components/Layout/Abas.razor src/AtividadesJuridicas.Web/Components/Layout/Abas.razor.js src/AtividadesJuridicas.Web/Components/Pages/AtividadeFicha.razor src/AtividadesJuridicas.Web/Components/Pages/ClienteFicha.razor src/AtividadesJuridicas.Web/Components/Pages/ProcessoFicha.razor tests/AtividadesJuridicas.IntegrationTests/AbasInterativasTests.cs tests/AtividadesJuridicas.IntegrationTests/AtividadeFichaInterativaTests.cs tests/AtividadesJuridicas.IntegrationTests/AtividadeFichaWebTests.cs tests/AtividadesJuridicas.IntegrationTests/ClienteFichaInterativaTests.cs tests/AtividadesJuridicas.IntegrationTests/ClienteFichaWebTests.cs tests/AtividadesJuridicas.IntegrationTests/ProcessoFichaInterativaTests.cs
$msg = @'
feat(web): abas do mobile com tabindex móvel, setas, Home e End, e aria-controls para os painéis das fichas

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
'@
$f = Join-Path $env:TEMP 'mensagem-tarefa-5.txt'
[System.IO.File]::WriteAllText($f, $msg, (New-Object System.Text.UTF8Encoding $false))
git commit -F $f
```

Expected do `git branch --show-current`: `worktree-feat+fase-9g-limpeza`.

### Task 6: Layout — menu "Mais", "Exibir/Esconder menu", ator persistente, botão flutuante depois do conteúdo, nome longo

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Layout/MainLayout.razor.js` (a vigia da largura do desktop)
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/MainLayout.razor`, `CabecalhoDePagina.razor`, `Encaixes.cs`; `tests/AtividadesJuridicas.IntegrationTests/ComBarraSuperior.cs` (o encaixe do fim do conteúdo)
- Test: `tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs`, `MainLayoutInterativoTests.cs` (novo), `ComponentesDoDesignSystemTests.cs` (cabeçalho com ação)

**Interfaces:**
- Consumes: o serviço `Foco` e `focarPrimeiroVisivel` (Tarefa 3); o gancho de estilo do "Mais" aberto e as reticências do nome (Tarefa 1).
- Produces: o encaixe `Encaixes.RodapeDoConteudo` (`"rodape-do-conteudo"`), um `SectionOutlet` no fim do `main.aj-app__conteudo`; o `CabecalhoDePagina` emite o `aj-fab` nesse encaixe (a marcação do botão não muda).

**Regras:**
- "Mais": `aria-current` só pela rota; `aria-expanded` como hoje. No código: `Atual(destino)` deixa de depender de `_menuAberto` (`PaginaMontada && Secao == destino ? "page" : null`) e `MaisAceso` perde o `_menuAberto ||`; o desenho do "Mais" aberto e dos destinos com o menu aberto é do CSS da Tarefa 1 (`[aria-expanded="true"]` e `.aj-app--menu-aberto`). Com o menu aberto (tablet e mobile): o resto da coluna fica `inert`, o foco vai ao primeiro item do menu, Esc fecha, e ao fechar (Esc, véu "Fechar menu", item escolhido que não navega) o foco volta ao "Mais" visível.
- "Esconder menu" leva o foco ao "Exibir menu" visível (o do cabeçalho ou o da faixa); "Exibir menu" leva o foco ao "Esconder menu".
- Ator persistente: o `UsuarioAutenticado` lido na pré-renderização chega ao circuito sem nova consulta antes da primeira renderização interativa (estado persistente do .NET 10); se não vier, o comportamento de hoje.
- Nome do usuário: `title` = o nome inteiro no `div.aj-usuario__texto` (não no bloco, para não cobrir o "Sair").
- Menu aberto e janela alargada até o desktop: o layout fecha o menu (escuta de `matchMedia('(min-width:1200px)')` com retorno ao .NET), para a coluna não ficar `inert` no desktop.

**Testes obrigatórios:** estático: o `aj-fab` depois do último bloco do conteúdo nas seis telas com ação (por HTTP); bUnit: o "Mais" sem `aria-current` com o menu aberto numa rota que não é dele, e com `aria-current` na rota dele; o menu aberto fecha (e a coluna deixa de ser `inert`) quando a largura chega ao desktop; bUnit do `MainLayout` (com `NavigationManager` e estado de autenticação de teste): `inert` com o menu aberto, foco pedido ao abrir e ao fechar, Esc fecha; "Esconder/Exibir menu" pedindo o foco ao par. O piscar ao conectar só se confere no navegador (Tarefa 11).

- [ ] **Step 1: Testes do botão flutuante depois do conteúdo (devem falhar)**

Todos os arquivos tocados nesta tarefa são UTF-8 sem BOM, com fim de linha CRLF (é o que eles já têm); o arquivo novo segue o mesmo.

O `ComBarraSuperior<T>` dos testes passa a desenhar os dois encaixes do layout: a barra superior antes do componente e o fim do conteúdo depois dele. Sem o segundo, o botão flutuante (que vai para o encaixe) sumiria do HTML dos testes que desenham uma tela pelo `ComBarraSuperior` (a agenda interativa, os documentos comuns), e a asserção negativa de `AgendaInterativaTests.Acesso_negado_na_consulta_encerra_a_tela` (`.aj-fab` ausente) passaria sem provar nada.

Substitua o conteúdo de `tests/AtividadesJuridicas.IntegrationTests/ComBarraSuperior.cs` por:

```csharp
using AtividadesJuridicas.Web.Components.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Sections;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Desenha um componente entre os encaixes do layout, como o layout faz: a barra superior antes dele e o fim do
/// conteúdo depois. Sozinho, o componente não mostra a barra do mobile nem o botão flutuante: o
/// <c>CabecalhoDePagina</c> os entrega aos encaixes, e sem encaixe eles não chegam ao HTML. Os parâmetros
/// informados seguem para o componente.
/// </summary>
public sealed class ComBarraSuperior<TComponente> : ComponentBase where TComponente : IComponent
{
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? Parametros { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<SectionOutlet>(0);
        builder.AddComponentParameter(1, nameof(SectionOutlet.SectionName), Encaixes.BarraSuperior);
        builder.CloseComponent();
        builder.OpenComponent<TComponente>(2);
        builder.AddMultipleAttributes(3, Parametros);
        builder.CloseComponent();
        builder.OpenComponent<SectionOutlet>(4);
        builder.AddComponentParameter(5, nameof(SectionOutlet.SectionName), Encaixes.RodapeDoConteudo);
        builder.CloseComponent();
    }
}
```

Em `tests/AtividadesJuridicas.IntegrationTests/ComponentesDoDesignSystemTests.cs`, substitua o teste `Cabecalho_com_acao_traz_o_botao_e_o_botao_flutuante` (hoje desenha o `CabecalhoDePagina` sozinho e acha o `aj-fab` nele) por estes dois. O primeiro mantém o que o antigo provava (o botão do cabeçalho e o flutuante, com botão e com link, e nada sem ação), agora com o encaixe, e acrescenta a ordem; o segundo prova que o botão flutuante saiu do lugar antigo:

```csharp
    [Fact]
    public async Task Cabecalho_com_acao_traz_o_botao_e_o_botao_flutuante()
    {
        var botao = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDePagina>>(
            new() { ["Titulo"] = "Documentos comuns", ["Acao"] = "Enviar documento" }));
        var link = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDePagina>>(
            new() { ["Titulo"] = "Clientes", ["Acao"] = "Novo cliente", ["AcaoHref"] = "/clientes/novo" }));
        var semAcao = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDePagina>>(
            new() { ["Titulo"] = "Início" }));

        // O ícone é um componente: pode haver espaço em branco em volta do svg.
        botao.Should().MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Enviar documento</button>")
            .And.MatchRegex("<button type=\"button\" class=\"aj-fab\" aria-label=\"Enviar documento\"");
        link.Should().MatchRegex("<a href=\"/clientes/novo\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Novo cliente</a>")
            .And.MatchRegex("<a href=\"/clientes/novo\" class=\"aj-fab\" aria-label=\"Novo cliente\"");
        // O botão flutuante vai ao encaixe do fim do conteúdo: depois do cabeçalho, o último elemento desenhado.
        botao.Should().MatchRegex("(?s)</header>\\s*</div>\\s*<button type=\"button\" class=\"aj-fab\"[^>]*>.*?</button>\\s*$");
        link.Should().MatchRegex("(?s)</header>\\s*</div>\\s*<a href=\"/clientes/novo\" class=\"aj-fab\"[^>]*>.*?</a>\\s*$");
        semAcao.Should().Contain("<h1 class=\"aj-titulo\">Início</h1>");
        semAcao.Should().NotContain("aj-fab").And.NotContain("aj-botao--primario");
    }

    // Sem o encaixe do layout, o botão flutuante não sai junto do cabeçalho: ele não é mais desenhado no lugar antigo
    // (logo depois do cabeçalho, antes do conteúdo da tela).
    [Fact]
    public async Task Cabecalho_sozinho_nao_desenha_o_botao_flutuante_junto_dele()
    {
        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<CabecalhoDePagina>(
            new() { ["Titulo"] = "Clientes", ["Acao"] = "Novo cliente", ["AcaoHref"] = "/clientes/novo" }));

        html.Should().MatchRegex("<a href=\"/clientes/novo\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Novo cliente</a>");
        html.Should().NotContain("aj-fab");
    }
```

Em `tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs`, acrescente (depois de `Pagina_sem_cabecalho_fica_com_a_barra_de_logo_e_a_faixa_de_exibir_menu`) o teste das seis telas com ação no mobile, pelo HTTP:

```csharp
    // O botão flutuante do mobile vem depois da tela na ordem do documento (e de tabulação): é o último elemento do
    // conteúdo. A posição fixa no canto não depende da ordem (o aj-fab é posicionado por right e bottom).
    [Theory]
    [InlineData("/agenda")]
    [InlineData("/clientes")]
    [InlineData("/atividades")]
    [InlineData("/processos")]
    [InlineData("/usuarios")]
    [InlineData("/documentos-comuns")]
    public async Task Botao_flutuante_vem_depois_do_conteudo_da_tela(string rota)
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = await PaginaAsync(admin, rota);

        var corpo = Regex.Match(html, "<main class=\"aj-app__conteudo\">.*?</main>", RegexOptions.Singleline).Value;
        corpo.Should().Contain("<h1 class=\"aj-titulo\">");
        Regex.Matches(corpo, "class=\"aj-fab\"").Should().ContainSingle();
        corpo.Should().MatchRegex("(?s)<(a|button)\\b[^>]*class=\"aj-fab\"[^>]*>(?:(?!<(?:a|button)\\b).)*?</\\1>(?:\\s|<!--.*?-->)*</main>$");
    }
```

- [ ] **Step 2: O nome do encaixe e o RED**

Em `src/AtividadesJuridicas.Web/Components/Layout/Encaixes.cs`, acrescente a constante (só ela, para os testes compilarem):

```csharp
namespace AtividadesJuridicas.Web.Components.Layout;

/// <summary>Os pontos do layout que a tela preenche com <c>SectionContent</c>.</summary>
public static class Encaixes
{
    /// <summary>A barra superior do mobile, antes do conteúdo e fora da área que rola.</summary>
    public const string BarraSuperior = "barra-superior";

    /// <summary>
    /// O fim do conteúdo, dentro do <c>main</c> e depois da tela: o botão flutuante do mobile vem depois dela na ordem
    /// de tabulação.
    /// </summary>
    public const string RodapeDoConteudo = "rodape-do-conteudo";
}
```

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ComponentesDoDesignSystemTests"`
Expected: FAIL só em `Cabecalho_sozinho_nao_desenha_o_botao_flutuante_junto_dele` (o cabeçalho ainda desenha o `aj-fab` no lugar antigo). O `Cabecalho_com_acao_traz_o_botao_e_o_botao_flutuante` passa já agora (o botão antigo também é o último elemento quando o cabeçalho está sozinho entre os encaixes): ele guarda o comportamento para o Step 3.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~LayoutWebTests"`
Expected: FAIL nos seis casos de `Botao_flutuante_vem_depois_do_conteudo_da_tela` (o `aj-fab` vem logo depois do cabeçalho, antes da lista); os outros testes passam.

- [ ] **Step 3: O botão flutuante no encaixe do fim do conteúdo**

Em `src/AtividadesJuridicas.Web/Components/Layout/MainLayout.razor`, dentro do `main`, depois do `CascadingValue`, ponha o encaixe (o resto do arquivo muda no Step 8; aqui só isto):

```razor
        <main class="aj-app__conteudo">
            <CascadingValue Value="_contexto">
                @if (!_marcaLida)
                {
                    <p class="aj-subtitulo">Carregando…</p>
                }
                else if (!PaginaMontada)
                {
                    @* A página não é montada: ela não chega a consultar nada. *@
                    <NaoEncontrada />
                }
                else
                {
                    @Body
                }
            </CascadingValue>
            @* O fim do conteúdo: o botão flutuante da tela vem depois dela na ordem de tabulação. *@
            <SectionOutlet SectionName="@Encaixes.RodapeDoConteudo" />
        </main>
```

Em `src/AtividadesJuridicas.Web/Components/Layout/CabecalhoDePagina.razor`, troque o comentário do topo (a frase "Com Acao, …") e o bloco do botão flutuante (linhas 67–77 de hoje). A marcação do botão não muda; só passa a ir para o encaixe:

```razor
@* Cabeçalho de página do design system. Emite as duas formas e o CSS mostra a da visão atual: no desktop e no
   tablet, o aj-cabecalho dentro do conteúdo; no mobile, a barra superior da tela, no encaixe do layout.
   Com VoltarPara, a barra do mobile é a de voltar (ficha e formulário), e a barra inferior some.
   Com Acao, a ação principal da tela: botão primário no cabeçalho e, no mobile, botão flutuante, no encaixe do fim
   do conteúdo (depois da tela na ordem de tabulação; a posição no canto da tela é do CSS).
   Dois encaixes: Complemento, ao lado do título no desktop e no tablet (um controle segmentado, antes da ação), e
   LinhaDoMobile, a segunda linha da barra superior do mobile (busca, chips, controle segmentado). *@
```

```razor
@if (Acao is not null)
{
    <SectionContent SectionName="@Encaixes.RodapeDoConteudo">
        @if (AcaoHref is not null)
        {
            <a href="@AcaoHref" class="aj-fab" aria-label="@Acao"><Icone Nome="@IconeDaAcao" /></a>
        }
        else
        {
            <button type="button" class="aj-fab" aria-label="@Acao" @onclick="OnAcao"><Icone Nome="@IconeDaAcao" /></button>
        }
    </SectionContent>
}
```

O `@using Microsoft.AspNetCore.Components.Sections` do topo do `CabecalhoDePagina.razor` já existe. O clique do botão continua sendo do `CabecalhoDePagina` (o conteúdo de um `SectionContent` pertence a quem o declara, como o `AcaoDoMobile` da barra superior).

- [ ] **Step 4: Build e testes**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ComponentesDoDesignSystemTests"`
Expected: todos passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~LayoutWebTests"`
Expected: todos passando, inclusive os seis casos de `Botao_flutuante_vem_depois_do_conteudo_da_tela`.

Rode, um por vez, as classes das seis telas com ação e as que desenham telas pelo `ComBarraSuperior` (as asserções de `aj-fab` delas continuam valendo sem mudança: o HTML do botão é o mesmo, só a posição no documento muda):

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AgendaWebTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadesWebTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ClientesWebTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ProcessosWebTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~UsuariosWebTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DocumentosDoPortalWebTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AgendaInterativaTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DocumentosComunsInterativosTests"`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Expected: todos passando, sem mudança nesses arquivos de teste.

- [ ] **Step 5: Commit**

```
git branch --show-current
git add src tests
git commit -F <arquivo da mensagem, UTF-8 sem BOM>
```

Expected do primeiro comando: `worktree-feat+fase-9g-limpeza`.

Mensagem:

```
fix(web): botão flutuante depois do conteúdo, no encaixe do fim do main

O CabecalhoDePagina entrega o aj-fab ao encaixe rodape-do-conteudo, no fim do main do layout: no mobile, o Tab
chega ao botão depois da tela, e não antes dos cartões. A marcação do botão e a posição no canto não mudam.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

- [ ] **Step 6: Testes do layout no bUnit (devem falhar)**

Nenhum teste desenha o `MainLayout` no bUnit hoje; este arquivo traz o auxiliar inteiro. O que ele monta:

- `TelaInterativa` (o despachante combinado, o fuso, os recados, o serviço `Foco` e o JavaScript solto). Dela vêm também o módulo de foco já montado (`ModuloDeFoco`, no caminho que o `Foco` importa) e o auxiliar `SeletoresFocados`, com os seletores pedidos a `focarPrimeiroVisivel`, na ordem: este arquivo não registra o `Foco` de novo nem monta o módulo de foco outra vez (dois `SetupModule` no mesmo caminho dividiriam as chamadas).
- O estado de autenticação de teste do bUnit (`AddAuthorization().SetAuthorized(...)`, que põe o `CascadingAuthenticationState` em volta do que for desenhado; sem a declaração do portal, o layout é o da equipe).
- O estado persistente de teste do bUnit (`AddBunitPersistentComponentState()`), que deixa o teste gravar o ator antes do desenho (`Persist`), disparar a gravação do fim da pré-renderização (`TriggerOnPersisting`) e ler o que o layout gravou (`TryTake`).
- Um `AntiforgeryStateProvider` que não emite token: o bloco do usuário tem o formulário de sair com `<AntiforgeryToken />`, que injeta esse serviço; o bUnit não o registra.
- O `NavigationManager` de teste do bUnit, levado à rota antes do desenho (como `AgendaInterativaTests` faz).
- O módulo próprio do layout (`./Components/Layout/MainLayout.razor.js`, criado no Step 8) montado com `JSInterop.SetupModule`, para o teste ler a instalação da vigia do desktop (`vigiarDesktop`) e pegar o `DotNetObjectReference` que o layout passa a ela.

O que o bUnit prova aqui: os atributos (`inert`, `aria-expanded`, `aria-current`, `title`), o estado do menu, a chamada a `focarPrimeiroVisivel` com o seletor certo (o foco **pedido**), a consulta do ator feita ou não e o que vai ao estado persistente. O que fica para o navegador (Tarefa 11): o foco chegando ao elemento, o Tab que não sai do menu, o Esc com o foco num item (o `keydown` subindo até o `aside`), a aparência do "Mais" aceso e o piscar ao conectar.

Crie `tests/AtividadesJuridicas.IntegrationTests/MainLayoutInterativoTests.cs`:

```csharp
using System.Reflection;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Web.Components.Layout;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O layout desenhado no bUnit: o menu aberto por "Mais" (o resto inerte, o foco para dentro e de volta, Esc), o
/// "Exibir/Esconder menu" levando o foco ao par, o botão flutuante no fim do conteúdo, o ator que a
/// pré-renderização entrega ao circuito e o nome inteiro no bloco do usuário. O foco é conferido pelo pedido (a
/// chamada ao módulo de foco com o seletor); que ele chega, a contenção do Tab e a aparência ficam para o navegador.
/// </summary>
public class MainLayoutInterativoTests : TelaInterativa
{
    // O módulo do próprio layout: a vigia que fecha o menu aberto quando a janela chega ao desktop.
    private const string ModuloDoLayout = "./Components/Layout/MainLayout.razor.js";

    // A chave com que o layout grava o ator no estado que a pré-renderização entrega ao circuito.
    private const string ChaveDoAtor = "ator-do-layout";

    // Os seletores que o layout pede ao módulo de foco (o primeiro visível que casa).
    private const string PrimeiroItem = "#menu-principal .aj-menu__item";
    private const string Mais = "button[aria-controls=\"menu-principal\"]";
    private const string ExibirMenu = ".aj-exibir-menu > button, .aj-cabecalho__menu > button";
    private const string EsconderMenu = ".aj-menu__esconder";

    private readonly BunitJSModuleInterop _moduloDoLayout;
    private readonly BunitPersistentComponentState _estado;

    public MainLayoutInterativoTests()
    {
        Services.AddSingleton<AntiforgeryStateProvider>(new SemAntiforgery());
        AddAuthorization().SetAuthorized("Diana Diretora");
        _estado = AddBunitPersistentComponentState();
        _moduloDoLayout = JSInterop.SetupModule(ModuloDoLayout);
        // O ator do circuito, quando o layout consulta. O teste que precisa de outro combina antes de desenhar.
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria);
    }

    private static readonly RenderFragment Conteudo = b =>
        b.AddMarkupContent(0, "<section class=\"aj-painel\"><h1 class=\"aj-titulo\">Clientes</h1></section>");

    // Uma tela com cabeçalho de verdade: o "Exibir menu" da linha do título e o botão flutuante.
    private static readonly RenderFragment TelaComCabecalho = b =>
    {
        b.OpenComponent<CabecalhoDePagina>(0);
        b.AddComponentParameter(1, nameof(CabecalhoDePagina.Titulo), "Clientes");
        b.AddComponentParameter(2, nameof(CabecalhoDePagina.Acao), "Novo cliente");
        b.AddComponentParameter(3, nameof(CabecalhoDePagina.AcaoHref), "/clientes/novo");
        b.CloseComponent();
        b.AddMarkupContent(4, "<section class=\"aj-painel\"><p class=\"aj-ajuda\">A lista de clientes.</p></section>");
    };

    private NavigationManager Navegacao => Services.GetRequiredService<NavigationManager>();

    private IRenderedComponent<MainLayout> Layout(string endereco, RenderFragment? corpo = null)
    {
        Navegacao.NavigateTo(endereco);
        return Desenhar<MainLayout>(p => p.Add(l => l.Body, corpo ?? Conteudo));
    }

    private static void AbrirPeloMais(IRenderedComponent<MainLayout> tela, string barra = ".aj-trilho") =>
        tela.Find($"{barra} button[aria-controls='menu-principal']").Click();

    private static bool Inerte(IRenderedComponent<MainLayout> tela, string seletor) => tela.Find(seletor).HasAttribute("inert");

    // ---- Menu aberto por "Mais" (tablet e mobile) ----

    [Fact]
    public void Menu_fechado_nao_deixa_nada_inerte_nem_pede_foco()
    {
        var tela = Layout("/clientes");

        tela.Find("main.aj-app__conteudo h1").TextContent.Should().Be("Clientes");
        Inerte(tela, "aside.aj-menu").Should().BeFalse();
        Inerte(tela, "nav.aj-trilho").Should().BeFalse();
        Inerte(tela, "div.aj-app__coluna").Should().BeFalse();
        Inerte(tela, "nav.aj-nav-inferior").Should().BeFalse();
        SeletoresFocados.Should().BeEmpty();
    }

    [Theory]
    [InlineData(".aj-trilho")]
    [InlineData(".aj-nav-inferior")]
    public void Abrir_pelo_mais_deixa_o_resto_inerte_e_pede_o_foco_ao_primeiro_item_do_menu(string barra)
    {
        var tela = Layout("/clientes");

        AbrirPeloMais(tela, barra);

        tela.Find("div.aj-app").ClassList.Should().Contain("aj-app--menu-aberto");
        Inerte(tela, "nav.aj-trilho").Should().BeTrue();
        Inerte(tela, "div.aj-app__coluna").Should().BeTrue();
        Inerte(tela, "nav.aj-nav-inferior").Should().BeTrue();
        Inerte(tela, "aside.aj-menu").Should().BeFalse();
        // Fora das partes inertes sobra um só nav "Principal": o do menu.
        tela.FindAll("nav[aria-label='Principal']").Where(n => n.Closest("[inert]") is null)
            .Should().ContainSingle().Which.ParentElement!.Id.Should().Be("menu-principal");
        SeletoresFocados.Should().Equal(PrimeiroItem);
    }

    [Fact]
    public void Esc_no_menu_aberto_fecha_e_pede_o_foco_ao_mais()
    {
        var tela = Layout("/clientes");
        AbrirPeloMais(tela);

        tela.Find("aside.aj-menu").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        tela.Find("div.aj-app").ClassList.Should().Contain("aj-app").And.NotContain("aj-app--menu-aberto");
        Inerte(tela, "nav.aj-trilho").Should().BeFalse();
        Inerte(tela, "div.aj-app__coluna").Should().BeFalse();
        Inerte(tela, "nav.aj-nav-inferior").Should().BeFalse();
        tela.Find(".aj-trilho button[aria-controls='menu-principal']").GetAttribute("aria-expanded").Should().Be("false");
        SeletoresFocados.Should().Equal(PrimeiroItem, Mais);
    }

    [Fact]
    public void Outra_tecla_no_menu_aberto_nao_fecha()
    {
        var tela = Layout("/clientes");
        AbrirPeloMais(tela);

        tela.Find("aside.aj-menu").KeyDown(new KeyboardEventArgs { Key = "Tab" });

        tela.Find("div.aj-app").ClassList.Should().Contain("aj-app--menu-aberto");
        Inerte(tela, "div.aj-app__coluna").Should().BeTrue();
        SeletoresFocados.Should().Equal(PrimeiroItem);
    }

    // No desktop o menu lateral está sempre à vista: Esc nele não fecha nada nem move o foco.
    [Fact]
    public void Esc_no_menu_lateral_fechado_nao_pede_foco()
    {
        var tela = Layout("/clientes");

        tela.Find("aside.aj-menu").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        tela.Find("div.aj-app").ClassList.Should().Contain("aj-app").And.NotContain("aj-app--menu-aberto");
        SeletoresFocados.Should().BeEmpty();
    }

    [Theory]
    [InlineData("button.aj-app__veu")]
    [InlineData("button.aj-menu__fechar")]
    public void Fechar_pelo_veu_ou_pelo_botao_do_menu_pede_o_foco_ao_mais(string fechar)
    {
        var tela = Layout("/clientes");
        AbrirPeloMais(tela);

        tela.Find(fechar).Click();

        tela.Find("div.aj-app").ClassList.Should().Contain("aj-app").And.NotContain("aj-app--menu-aberto");
        Inerte(tela, "div.aj-app__coluna").Should().BeFalse();
        SeletoresFocados.Should().Equal(PrimeiroItem, Mais);
    }

    // Escolher no menu a tela em que já se está não troca de página (o FocoNoTitulo não age): o menu some e o foco
    // volta ao "Mais".
    [Fact]
    public void Escolher_no_menu_a_tela_em_que_ja_esta_fecha_e_pede_o_foco_ao_mais()
    {
        var tela = Layout("/clientes?busca=maria");
        AbrirPeloMais(tela);

        Navegacao.NavigateTo("/clientes");

        tela.WaitForAssertion(() => tela.Find("div.aj-app").ClassList.Should().NotContain("aj-app--menu-aberto"));
        SeletoresFocados.Should().Equal(PrimeiroItem, Mais);
    }

    // Outra tela: o foco vai ao título dela pelo FocoNoTitulo; o layout não pede o "Mais".
    [Fact]
    public void Escolher_outra_tela_no_menu_fecha_sem_pedir_o_foco_ao_mais()
    {
        var tela = Layout("/clientes");
        AbrirPeloMais(tela);

        Navegacao.NavigateTo("/processos");

        tela.WaitForAssertion(() => tela.Find("div.aj-app").ClassList.Should().NotContain("aj-app--menu-aberto"));
        Inerte(tela, "div.aj-app__coluna").Should().BeFalse();
        SeletoresFocados.Should().Equal(PrimeiroItem);
    }

    // ---- Menu aberto e a janela que chega ao desktop ----

    // A referência que o layout entregou à vigia do desktop (a mesma que o JavaScript usa para chamá-lo).
    private DotNetObjectReference<MainLayout> ReferenciaDaVigia() =>
        (DotNetObjectReference<MainLayout>)_moduloDoLayout.VerifyInvoke("vigiarDesktop").Arguments[0]!;

    [Fact]
    public void Primeiro_desenho_instala_a_vigia_do_desktop_uma_vez_com_o_proprio_layout()
    {
        var tela = Layout("/clientes");
        AbrirPeloMais(tela);
        tela.Find("button.aj-menu__fechar").Click();

        // Três desenhos (o primeiro, o de abrir e o de fechar), uma instalação só, com a referência ao layout.
        ReferenciaDaVigia().Value.Should().BeSameAs(tela.Instance);
        var metodo = typeof(MainLayout).GetMethod("AoChegarAoDesktop");
        metodo.Should().NotBeNull();
        metodo!.GetCustomAttribute<JSInvokableAttribute>().Should().NotBeNull();
    }

    [Fact]
    public async Task Chegar_ao_desktop_com_o_menu_aberto_fecha_e_tira_o_inert_sem_mover_o_foco()
    {
        var tela = Layout("/clientes");
        AbrirPeloMais(tela);
        Inerte(tela, "div.aj-app__coluna").Should().BeTrue();

        await TelaDeTeste.ChamarAsync(ReferenciaDaVigia().Value, "AoChegarAoDesktop");

        tela.WaitForAssertion(() => tela.Find("div.aj-app").ClassList.Should().NotContain("aj-app--menu-aberto"));
        Inerte(tela, "nav.aj-trilho").Should().BeFalse();
        Inerte(tela, "div.aj-app__coluna").Should().BeFalse();
        Inerte(tela, "nav.aj-nav-inferior").Should().BeFalse();
        tela.Find(".aj-trilho button[aria-controls='menu-principal']").GetAttribute("aria-expanded").Should().Be("false");
        // Só o foco de quando abriu: no desktop o "Mais" não está à vista, e o foco não é movido.
        SeletoresFocados.Should().Equal(PrimeiroItem);
    }

    [Fact]
    public async Task Chegar_ao_desktop_com_o_menu_fechado_nao_muda_nada()
    {
        var tela = Layout("/clientes");
        tela.Find("button.aj-menu__esconder").Click();

        await TelaDeTeste.ChamarAsync(ReferenciaDaVigia().Value, "AoChegarAoDesktop");

        tela.Find("div.aj-app").ClassList.Should().Contain("aj-app--menu-escondido").And.NotContain("aj-app--menu-aberto");
        SeletoresFocados.Should().Equal(ExibirMenu);
    }

    [Fact]
    public async Task Descartar_o_layout_libera_a_referencia_da_vigia()
    {
        Layout("/clientes");
        var referencia = ReferenciaDaVigia();
        referencia.Value.Should().BeOfType<MainLayout>();

        await DisposeComponentsAsync();

        referencia.Invoking(r => r.Value).Should().Throw<ObjectDisposedException>();
    }

    // ---- "Mais": aria-current só pela rota; aberto é aria-expanded ----

    [Fact]
    public void Com_o_menu_aberto_o_mais_fica_so_com_o_expanded_e_o_destino_da_rota_continua_marcado()
    {
        var tela = Layout("/atividades");

        AbrirPeloMais(tela);

        foreach (var barra in new[] { ".aj-trilho", ".aj-nav-inferior" })
        {
            var mais = tela.Find($"{barra} button[aria-controls='menu-principal']");
            mais.GetAttribute("aria-expanded").Should().Be("true", barra);
            mais.HasAttribute("aria-current").Should().BeFalse(barra);
            tela.Find($"{barra} a[href='/atividades']").GetAttribute("aria-current").Should().Be("page", barra);
        }
    }

    [Fact]
    public void Com_o_menu_aberto_numa_tela_do_mais_ele_continua_marcado()
    {
        var tela = Layout("/areas");

        AbrirPeloMais(tela);

        foreach (var barra in new[] { ".aj-trilho", ".aj-nav-inferior" })
        {
            var mais = tela.Find($"{barra} button[aria-controls='menu-principal']");
            mais.GetAttribute("aria-expanded").Should().Be("true", barra);
            mais.GetAttribute("aria-current").Should().Be("page", barra);
        }
    }

    // ---- "Exibir menu" e "Esconder menu" (desktop) ----

    [Fact]
    public void Esconder_menu_pede_o_foco_ao_exibir_menu_e_exibir_menu_o_devolve_ao_esconder()
    {
        var tela = Layout("/clientes");

        tela.Find("button.aj-menu__esconder").Click();

        tela.Find("div.aj-app").ClassList.Should().Contain("aj-app--menu-escondido");
        SeletoresFocados.Should().Equal(ExibirMenu);

        tela.Find(".aj-exibir-menu > button").Click();

        tela.Find("div.aj-app").ClassList.Should().Contain("aj-app").And.NotContain("aj-app--menu-escondido");
        SeletoresFocados.Should().Equal(ExibirMenu, EsconderMenu);
    }

    [Fact]
    public void Exibir_menu_do_cabecalho_da_tela_tambem_devolve_o_foco_ao_esconder_menu()
    {
        var tela = Layout("/clientes", TelaComCabecalho);
        tela.Find("button.aj-menu__esconder").Click();

        tela.Find(".aj-cabecalho__menu > button").Click();

        tela.Find("div.aj-app").ClassList.Should().Contain("aj-app").And.NotContain("aj-app--menu-escondido");
        SeletoresFocados.Should().Equal(ExibirMenu, EsconderMenu);
    }

    // ---- Botão flutuante no fim do conteúdo ----

    [Fact]
    public void Botao_flutuante_do_cabecalho_sai_no_fim_do_conteudo()
    {
        var tela = Layout("/clientes", TelaComCabecalho);

        var conteudo = tela.Find("main.aj-app__conteudo");
        conteudo.QuerySelector("section.aj-painel")!.TextContent.Should().Be("A lista de clientes.");
        var ultimo = conteudo.LastElementChild!;
        ultimo.ClassName.Should().Be("aj-fab");
        ultimo.GetAttribute("href").Should().Be("/clientes/novo");
        tela.FindAll(".aj-fab").Should().ContainSingle();
    }

    // ---- Ator da pré-renderização no circuito ----

    [Fact]
    public void Ator_gravado_na_pre_renderizacao_chega_ao_circuito_sem_nova_consulta()
    {
        _estado.Persist(ChaveDoAtor, AtorDaDiretoria);
        // Se o layout consultasse, viria outra pessoa: o nome na tela diz de onde o ator veio.
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaProducao);

        var tela = Layout("/clientes");

        Despachante.Recebidas.OfType<ObterMeuAcesso>().Should().BeEmpty();
        tela.Find(".aj-usuario__texto b").TextContent.Should().Be("Diana Diretora");
        tela.FindAll(".aj-menu__grupo > span").Select(s => s.TextContent).Should().Contain("Administração");
    }

    [Fact]
    public void Sem_ator_gravado_o_layout_consulta_e_grava_o_ator_para_o_circuito()
    {
        var tela = Layout("/clientes");

        Despachante.Recebidas.OfType<ObterMeuAcesso>().Should().ContainSingle();
        tela.Find(".aj-usuario__texto b").TextContent.Should().Be("Diana Diretora");

        _estado.TriggerOnPersisting();

        _estado.TryTake<UsuarioAutenticado>(ChaveDoAtor, out var gravado).Should().BeTrue();
        gravado.Should().Be(AtorDaDiretoria);
    }

    // Acesso encerrado na pré-renderização: nada vai ao circuito, que consulta de novo (o comportamento de antes).
    [Fact]
    public void Acesso_encerrado_nao_grava_ator_nem_poe_o_title()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>((UsuarioAutenticado?)null);

        var tela = Layout("/clientes");

        Despachante.Recebidas.OfType<ObterMeuAcesso>().Should().ContainSingle();
        tela.Find("div.aj-usuario__texto").HasAttribute("title").Should().BeFalse();

        _estado.TriggerOnPersisting();

        _estado.TryTake<UsuarioAutenticado>(ChaveDoAtor, out _).Should().BeFalse();
    }

    // ---- Nome longo ----

    [Fact]
    public void Bloco_do_usuario_leva_o_nome_inteiro_no_title()
    {
        const string nome = "Maria Aparecida dos Santos Albuquerque Cavalcanti";
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaProducao with { Nome = nome });

        var tela = Layout("/clientes");

        tela.Find("div.aj-usuario__texto").GetAttribute("title").Should().Be(nome);
        tela.Find(".aj-usuario__texto b").TextContent.Should().Be(nome);
        // O title fica no texto, não no bloco: o botão "Sair" não ganha o nome como dica.
        tela.Find("div.aj-usuario").HasAttribute("title").Should().BeFalse();
    }

    // O formulário de sair do bloco do usuário tem o AntiforgeryToken; no teste não há token a emitir.
    private sealed class SemAntiforgery : AntiforgeryStateProvider
    {
        public override AntiforgeryRequestToken? GetAntiforgeryToken() => null;
    }
}
```

Notas para o implementador:

- O `Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>((UsuarioAutenticado?)null)` precisa do cast: sem ele, `null` casa com as duas sobrecargas de `Responde` (valor e função).
- `Navegacao.NavigateTo` depois do desenho segue `AgendaInterativaTests` (linha 181): o `NavigationManager` do bUnit avisa o `LocationChanged`, e o layout se desenha de novo pelo `InvokeAsync`; o `WaitForAssertion` espera esse desenho.
- Se o compilador pedir outro membro abstrato em `AntiforgeryStateProvider`, implemente-o devolvendo o vazio do tipo (o teste não emite token). Se o bUnit já registrar o serviço, a linha não faz mal.
- No módulo do layout, o bUnit solto devolve `null` para o `InvokeAsync<IJSObjectReference>("vigiarDesktop", …)`: o layout guarda a vigia como anulável e a solta só se ela existir. Os testes da vigia pegam o `DotNetObjectReference` da chamada registrada e chamam o `[JSInvokable]` por ele (`TelaDeTeste.ChamarAsync`, que acha o método pelo nome e por isso compila antes de ele existir), como o JavaScript faria; o método entra no despachante do componente pelo `InvokeAsync`.
- `DisposeComponentsAsync()` é do `BunitContext`: descarta os componentes desenhados (o `DisposeAsync` do layout roda), e o `Value` de um `DotNetObjectReference` liberado lança `ObjectDisposedException`.
- `SetAuthorized` tem o estado como segundo parâmetro (`AuthorizationState`); se a versão do bUnit não lhe der valor padrão, passe `AuthorizationState.Authorized` (de `Bunit.TestDoubles`).

Em `tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs`, acrescente (depois de `Modulo_do_foco_no_titulo_e_servido_e_pagina_estatica_usa_o_foco_do_blazor`) a prova de que o módulo do layout é servido:

```csharp
    [Fact]
    public async Task Modulo_do_layout_e_servido()
    {
        var anonimo = _app.NovoCliente();

        var modulo = await anonimo.GetAsync("/Components/Layout/MainLayout.razor.js");

        modulo.StatusCode.Should().Be(HttpStatusCode.OK);
        (await modulo.Content.ReadAsStringAsync()).Should().Contain("export function vigiarDesktop");
    }
```

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros (os testes só usam o que já existe; o `AoChegarAoDesktop` é chamado pelo nome).

- [ ] **Step 7: O RED**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~MainLayoutInterativoTests"`
Expected: passam já agora só `Menu_fechado_nao_deixa_nada_inerte_nem_pede_foco`, `Com_o_menu_aberto_numa_tela_do_mais_ele_continua_marcado` (o "Mais" já ficava aceso ali; o teste guarda o comportamento), `Botao_flutuante_do_cabecalho_sai_no_fim_do_conteudo` (o Step 3 já o fez) e `Acesso_encerrado_nao_grava_ator_nem_poe_o_title` (hoje também não grava nem põe `title`; guarda o comportamento). Todos os outros FALHAM, pelos motivos: nenhum `inert`; nenhuma chamada a `focarPrimeiroVisivel`; o `aside` sem `@onkeydown` (o bUnit recusa disparar evento sem tratador: é o que quebra os testes de Esc e de outra tecla, inclusive `Esc_no_menu_lateral_fechado_nao_pede_foco`); o "Mais" com `aria-current="page"` e o destino da rota sem ele com o menu aberto; o layout consultando mesmo com o ator gravado ("Pedro Produção" na tela); nada gravado no estado (`Sem_ator_gravado…` quebra no `TryTake`, depois de a consulta e o nome passarem); o texto do bloco do usuário sem `title`; nenhuma chamada a `vigiarDesktop` (os quatro testes da vigia quebram no `VerifyInvoke`).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~LayoutWebTests"`
Expected: FAIL só em `Modulo_do_layout_e_servido` (404: o módulo ainda não existe).

Se algum teste quebrar antes da asserção por falta de serviço (`Cannot provide a value for property…`), o auxiliar está incompleto: registre o serviço que a mensagem nomeia no construtor da classe, antes do `AddBunitPersistentComponentState()`, e anote no relatório.

- [ ] **Step 8: O layout**

Crie `src/AtividadesJuridicas.Web/Components/Layout/MainLayout.razor.js` (UTF-8 sem BOM, CRLF; é servido em `/Components/Layout/MainLayout.razor.js`, como o `FocoNoTitulo.razor.js`):

```javascript
// O menu aberto por "Mais" só existe no tablet e no mobile, e com ele aberto o resto da tela fica inerte. Se a
// janela chega ao desktop com o menu aberto, nada à vista o fecha e a tela inteira ficaria sem teclado e sem clique:
// esta vigia avisa o layout, que fecha o menu. O layout decide (fecha só se estiver aberto); aqui só se olha a largura.
const desktop = "(min-width: 1200px)";

export function vigiarDesktop(layout) {
    const consulta = window.matchMedia(desktop);
    const aoMudar = evento => {
        if (!evento.matches) return;

        // O circuito pode ter caído: a vigia é uma proteção e não pode gerar erro na página.
        layout.invokeMethodAsync("AoChegarAoDesktop").catch(() => { });
    };
    consulta.addEventListener("change", aoMudar);

    return {
        parar() {
            consulta.removeEventListener("change", aoMudar);
        },
    };
}
```

Substitua o conteúdo de `src/AtividadesJuridicas.Web/Components/Layout/MainLayout.razor` por (o encaixe do Step 3 continua; o resto da marcação é a de hoje com o `@onkeydown` do `aside`, o `title` do texto do bloco do usuário, os três `inert` e o véu fora do teclado e do leitor):

```razor
@inherits LayoutComponentBase
@implements IAsyncDisposable
@using AtividadesJuridicas.Web.Components.Pages
@using Microsoft.AspNetCore.Components.Sections
@inject IDespachante Despachante
@inject NavigationManager Navegacao
@inject PersistentComponentState EstadoPersistente
@inject Foco Foco
@inject IJSRuntime JS

@* Três navegações, uma por visão; o CSS mostra a da largura atual (menu no desktop, trilho no tablet, barra
   inferior no mobile). Em tablet e mobile, "Mais" abre o menu completo por cima da tela; aberto, o resto (trilho,
   coluna e barra inferior) fica inerte: o teclado e o leitor de tela só alcançam o menu, e um só nav "Principal".
   O usuário de cliente vê o menu do portal: no menu lateral, Início, Andamento, Meus documentos, Documentos comuns e
   Minha senha; no trilho, os quatro primeiros (Minha senha fica no avatar); na barra inferior, Início, Andamento e
   Documentos (Meus documentos), e o restante em "Mais". *@
<div class="aj-app @(_menuEscondido ? "aj-app--menu-escondido" : null) @(_menuAberto ? "aj-app--menu-aberto" : null)">
    <aside class="aj-menu" id="menu-principal" @onkeydown="AoTeclarNoMenu">
        <div class="aj-menu__topo">
            <a href="/" aria-label="Araújo Advocacia, início">
                <img src="img/logo-horizontal-compacta-escuro.svg" alt="Araújo Advocacia" width="140" height="32" />
            </a>
            <button type="button" class="aj-menu__botao aj-menu__esconder" aria-label="Esconder menu" @onclick="AlternarMenu"><Icone Nome="esconder-menu" /></button>
            <button type="button" class="aj-menu__botao aj-menu__fechar" aria-label="Fechar menu" @onclick="FecharMenu"><Icone Nome="fechar" /></button>
        </div>
        <nav aria-label="Principal">
            <div class="aj-menu__grupo">
                <span>@(EhCliente ? "Portal" : "Dia a dia")</span>
                <NavLink class="aj-menu__item" href="/" Match="NavLinkMatch.All"><Icone Nome="inicio" />Início</NavLink>
                @if (EhCliente)
                {
                    <NavLink class="aj-menu__item" href="/andamento"><Icone Nome="atividades" />Andamento</NavLink>
                    <NavLink class="aj-menu__item" href="/meus-documentos"><Icone Nome="documento" />Meus documentos</NavLink>
                    <NavLink class="aj-menu__item" href="/documentos-comuns"><Icone Nome="pasta" />Documentos comuns</NavLink>
                    <NavLink class="aj-menu__item" href="/minha-senha"><Icone Nome="usuarios" />Minha senha</NavLink>
                }
                else
                {
                    <NavLink class="aj-menu__item" href="/atividades"><Icone Nome="atividades" />Atividades</NavLink>
                    <NavLink class="aj-menu__item" href="/agenda"><Icone Nome="agenda" />Agenda</NavLink>
                }
            </div>
            @if (!EhCliente)
            {
                <div class="aj-menu__grupo">
                    <span>Cadastros</span>
                    <NavLink class="aj-menu__item" href="/clientes"><Icone Nome="clientes" />Clientes</NavLink>
                    <NavLink class="aj-menu__item" href="/processos"><Icone Nome="processos" />Processos</NavLink>
                    @if (_ator is { EhDiretoria: true })
                    {
                        <NavLink class="aj-menu__item" href="/documentos-comuns"><Icone Nome="pasta" />Documentos comuns</NavLink>
                    }
                </div>
                @if (_ator is { EhDiretoria: true })
                {
                    <div class="aj-menu__grupo">
                        <span>Administração</span>
                        <NavLink class="aj-menu__item" href="/areas"><Icone Nome="areas" />Áreas</NavLink>
                        <NavLink class="aj-menu__item" href="/usuarios"><Icone Nome="usuarios" />Usuários</NavLink>
                        <NavLink class="aj-menu__item" href="/produtores"><Icone Nome="produtores" />Produtores</NavLink>
                        <NavLink class="aj-menu__item" href="/perfis"><Icone Nome="perfis" />Perfis</NavLink>
                        <NavLink class="aj-menu__item" href="/auditoria"><Icone Nome="auditoria" />Auditoria</NavLink>
                    </div>
                }
            }
        </nav>
        <div class="aj-usuario">
            <span class="aj-avatar" aria-hidden="true">@Iniciais</span>
            @* O nome longo fica numa linha com reticências (design system); o title do texto traz o nome inteiro. *@
            <div class="aj-usuario__texto" title="@_ator?.Nome">
                <b>@_ator?.Nome</b>
                <span>@Perfil@(Perfil is null ? null : " · ")<NavLink href="/minha-senha">Minha senha</NavLink></span>
            </div>
            <form method="post" action="/conta/sair" class="aj-menu__sair">
                <AntiforgeryToken />
                <button type="submit" class="aj-menu__botao" aria-label="Sair"><Icone Nome="sair" /></button>
            </form>
        </div>
    </aside>

    <nav class="aj-trilho" aria-label="Principal" inert="@_menuAberto">
        <img src="img/logo-horizontal-compacta-escuro.svg" alt="Araújo Advocacia" width="68" height="16" />
        <a class="aj-trilho__item" href="/" aria-current="@Atual("")"><Icone Nome="inicio" />Início</a>
        @if (EhCliente)
        {
            <a class="aj-trilho__item" href="/andamento" aria-current="@Atual("andamento")"><Icone Nome="atividades" />Andamento</a>
            <a class="aj-trilho__item aj-trilho__item--duas-linhas" href="/meus-documentos" aria-current="@Atual("meus-documentos")"><Icone Nome="documento" />Meus documentos</a>
            <a class="aj-trilho__item aj-trilho__item--duas-linhas" href="/documentos-comuns" aria-current="@Atual("documentos-comuns")"><Icone Nome="pasta" />Documentos comuns</a>
        }
        else
        {
            <a class="aj-trilho__item" href="/atividades" aria-current="@Atual("atividades")"><Icone Nome="atividades" />Atividades</a>
            <a class="aj-trilho__item" href="/agenda" aria-current="@Atual("agenda")"><Icone Nome="agenda" />Agenda</a>
            <a class="aj-trilho__item" href="/clientes" aria-current="@Atual("clientes")"><Icone Nome="clientes" />Clientes</a>
            <a class="aj-trilho__item" href="/processos" aria-current="@Atual("processos")"><Icone Nome="processos" />Processos</a>
        }
        <button type="button" class="aj-trilho__item" aria-controls="menu-principal" aria-expanded="@(_menuAberto ? "true" : "false")" aria-current="@MaisNoTrilho" @onclick="AbrirMenu"><Icone Nome="mais" />Mais</button>
        <span class="aj-trilho__espaco"></span>
        <a href="/minha-senha" class="aj-avatar aj-avatar--g" aria-label="Minha conta">@Iniciais</a>
    </nav>

    <div class="aj-app__coluna" inert="@_menuAberto">
        <SectionOutlet SectionName="@Encaixes.BarraSuperior" />
        <header class="aj-barra-superior aj-barra-superior--padrao">
            <div class="aj-barra-superior__linha">
                <img class="aj-barra-superior__logo" src="img/logo-horizontal-compacta-claro.svg" alt="Araújo Advocacia" width="139" height="32" />
                <span class="aj-barra-superior__espaco"></span>
                <a href="/minha-senha" class="aj-avatar" aria-label="Minha conta">@Iniciais</a>
            </div>
        </header>
        <div class="aj-exibir-menu">
            <button type="button" class="aj-botao aj-botao--secundario aj-botao--icone" aria-label="Exibir menu" @onclick="AlternarMenu"><Icone Nome="menu" /></button>
            <img src="img/simbolo-claro.svg" alt="Araújo Advocacia" width="29" height="32" />
        </div>

        <main class="aj-app__conteudo">
            <CascadingValue Value="_contexto">
                @if (!_marcaLida)
                {
                    <p class="aj-subtitulo">Carregando…</p>
                }
                else if (!PaginaMontada)
                {
                    @* A página não é montada: ela não chega a consultar nada. *@
                    <NaoEncontrada />
                }
                else
                {
                    @Body
                }
            </CascadingValue>
            @* O fim do conteúdo: o botão flutuante da tela vem depois dela na ordem de tabulação. *@
            <SectionOutlet SectionName="@Encaixes.RodapeDoConteudo" />
        </main>
    </div>

    <nav class="aj-nav-inferior" aria-label="Principal" inert="@_menuAberto">
        <a class="aj-nav-inferior__item" href="/" aria-current="@Atual("")"><span><Icone Nome="inicio" /></span>Início</a>
        @if (EhCliente)
        {
            <a class="aj-nav-inferior__item" href="/andamento" aria-current="@Atual("andamento")"><span><Icone Nome="atividades" /></span>Andamento</a>
            <a class="aj-nav-inferior__item" href="/meus-documentos" aria-current="@Atual("meus-documentos")"><span><Icone Nome="documento" /></span>Documentos</a>
        }
        else
        {
            <a class="aj-nav-inferior__item" href="/atividades" aria-current="@Atual("atividades")"><span><Icone Nome="atividades" /></span>Atividades</a>
            <a class="aj-nav-inferior__item" href="/agenda" aria-current="@Atual("agenda")"><span><Icone Nome="agenda" /></span>Agenda</a>
        }
        <button type="button" class="aj-nav-inferior__item" aria-controls="menu-principal" aria-expanded="@(_menuAberto ? "true" : "false")" aria-current="@MaisNaBarra" @onclick="AbrirMenu"><span><Icone Nome="mais" /></span>Mais</button>
    </nav>

    @* O véu fecha o menu pelo toque e pelo mouse. Pelo teclado e pelo leitor de tela, quem fecha é o "Fechar menu" do
       próprio menu (e o Esc): o véu fica fora da ordem de tabulação e da árvore de acessibilidade. *@
    <button type="button" class="aj-app__veu" aria-label="Fechar menu" aria-hidden="true" tabindex="-1" @onclick="FecharMenu"></button>
</div>

@code {
    [CascadingParameter]
    private Task<AuthenticationState>? EstadoDeAutenticacao { get; set; }

    // A chave do ator no estado que a pré-renderização entrega ao circuito.
    private const string ChaveDoAtor = "ator-do-layout";

    // Os destinos do foco. O módulo foca o primeiro visível (com caixa) que casa: cada visão mostra o seu.
    private const string SeletorDoPrimeiroItem = "#menu-principal .aj-menu__item";
    private const string SeletorDoMais = "button[aria-controls=\"menu-principal\"]";
    private const string SeletorDoExibirMenu = ".aj-exibir-menu > button, .aj-cabecalho__menu > button";
    private const string SeletorDoEsconderMenu = ".aj-menu__esconder";

    // O módulo do layout e a vigia do desktop (a referência a este layout é o que o JavaScript chama).
    private const string ModuloDoLayout = "./Components/Layout/MainLayout.razor.js";

    private UsuarioAutenticado? _ator;
    private bool _marcaLida;
    private bool _portalPeloCookie;
    private PersistingComponentStateSubscription _persistencia;

    private DotNetObjectReference<MainLayout>? _referencia;
    private IJSObjectReference? _modulo;
    private IJSObjectReference? _vigia;

    // Desktop: o menu lateral pode ser escondido. Tablet e mobile: o menu completo abre por cima da tela.
    private bool _menuEscondido;
    private bool _menuAberto;

    // O destino do foco depois da próxima renderização: o botão clicado sumiu, ficou inerte ou o menu fechou.
    private string? _focoPendente;

    // O caminho da tela atual, sem a consulta: escolher no menu a tela em que já se está não troca de página.
    private string _caminho = string.Empty;

    // O banco manda quando já respondeu; antes disso, e se o acesso foi encerrado, vale a marca do cookie.
    private bool EhCliente => _ator?.EhCliente ?? _portalPeloCookie;

    private string Rota => Navegacao.ToBaseRelativePath(Navegacao.Uri);

    // O que cada navegação mostra a cada perfil, pelo primeiro trecho do endereço. As listas repetem a marcação
    // acima: destino novo entra nos dois lugares. "Mais" acende quando a tela está no menu e fora da barra da visão.
    private static readonly string[] MenuDoCliente = ["", "andamento", "meus-documentos", "documentos-comuns"];
    private static readonly string[] MenuDaProducao = ["", "atividades", "agenda", "clientes", "processos"];
    private static readonly string[] MenuDaDiretoria =
        [.. MenuDaProducao, "documentos-comuns", "areas", "usuarios", "produtores", "perfis", "auditoria"];
    private static readonly string[] TrilhoDaEquipe = ["", "atividades", "agenda", "clientes", "processos"];
    private static readonly string[] TrilhoDoCliente = ["", "andamento", "meus-documentos", "documentos-comuns"];
    private static readonly string[] BarraDaEquipe = ["", "atividades", "agenda"];
    private static readonly string[] BarraDoCliente = ["", "andamento", "meus-documentos"];

    private ContextoDoLayout _contexto = new(string.Empty, default);

    private bool PaginaMontada => _marcaLida && (EhCliente ? RotasDoCliente.Permite(Rota) : !RotasDoCliente.Exclusiva(Rota));

    /// <summary>O primeiro trecho do endereço: "" no início, "clientes" em /clientes e nas telas abaixo dela.</summary>
    private string Secao => Rota.Split('?', '#')[0].Trim('/').Split('/')[0].ToLowerInvariant();

    private string[] DestinosDoMenu =>
        EhCliente ? MenuDoCliente : _ator is { EhDiretoria: true } ? MenuDaDiretoria : MenuDaProducao;

    // O aria-current diz só a rota. Com o menu aberto por cima da tela, o aria-expanded do "Mais" diz que ele está
    // aberto, e o design system acende o "Mais" por ele e apaga o destino da rota enquanto o menu está aberto.
    private string? Atual(string destino) => PaginaMontada && Secao == destino ? "page" : null;

    private string? MaisNoTrilho => MaisAceso(EhCliente ? TrilhoDoCliente : TrilhoDaEquipe);

    private string? MaisNaBarra => MaisAceso(EhCliente ? BarraDoCliente : BarraDaEquipe);

    private string? MaisAceso(string[] naBarra) =>
        PaginaMontada && DestinosDoMenu.Contains(Secao) && !naBarra.Contains(Secao) ? "page" : null;

    private ContextoDoLayout NovoContexto() => new(Iniciais, EventCallback.Factory.Create(this, AlternarMenu));

    private string? Perfil => _ator is null ? null : RotulosDePerfil.DoTipo(_ator.TipoPerfil);

    /// <summary>As iniciais do primeiro e do último nome ("Marina Couto" → "MC").</summary>
    private string Iniciais
    {
        get
        {
            var partes = (_ator?.Nome ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return partes.Length switch
            {
                0 => string.Empty,
                1 => partes[0][..1].ToUpperInvariant(),
                _ => (partes[0][..1] + partes[^1][..1]).ToUpperInvariant(),
            };
        }
    }

    /// <summary>O caminho sem a consulta e sem o fragmento: "clientes" em /clientes?busca=maria.</summary>
    private static string Caminho(string rota) => rota.Split('?', '#')[0].Trim('/').ToLowerInvariant();

    protected override async Task OnInitializedAsync()
    {
        Navegacao.LocationChanged += AoNavegar;
        _caminho = Caminho(Rota);
        _persistencia = EstadoPersistente.RegisterOnPersisting(PersistirAtor);

        // A tarefa do estado de autenticação já vem concluída: a marca é lida antes da primeira renderização.
        if (EstadoDeAutenticacao is not null)
            _portalPeloCookie = IdentidadeHttp.EhDoPortal((await EstadoDeAutenticacao).User);
        _marcaLida = true;

        // O circuito recebe o ator que a pré-renderização leu: o menu, o nome, o avatar e o "Mais" já saem certos na
        // primeira renderização interativa, sem esperar outra consulta. Sem ele (acesso encerrado na
        // pré-renderização, ou o estado não veio), o layout consulta como sempre.
        if (EstadoPersistente.TryTakeFromJson<UsuarioAutenticado>(ChaveDoAtor, out var persistido) && persistido is not null)
        {
            _ator = persistido;
            _contexto = NovoContexto();
            return;
        }

        _contexto = NovoContexto();
        _ator = await Despachante.EnviarAsync(new ObterMeuAcesso());
        _contexto = NovoContexto();
    }

    // No fim da pré-renderização, o ator lido vai junto da página para o circuito. Sem ator, nada vai.
    private Task PersistirAtor()
    {
        if (_ator is not null)
            EstadoPersistente.PersistAsJson(ChaveDoAtor, _ator);
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) await VigiarDesktopAsync();

        if (_focoPendente is not { } seletor) return;

        _focoPendente = null;
        await Foco.FocarPrimeiroVisivelAsync(seletor);
    }

    // Instalada uma vez, no primeiro desenho interativo: a vigia avisa sempre que a janela chega ao desktop, e
    // AoChegarAoDesktop só age com o menu aberto.
    private async Task VigiarDesktopAsync()
    {
        _referencia = DotNetObjectReference.Create(this);
        try
        {
            _modulo = await JS.InvokeAsync<IJSObjectReference>("import", ModuloDoLayout);
            _vigia = await _modulo.InvokeAsync<IJSObjectReference>("vigiarDesktop", _referencia);
        }
        catch (Exception erro) when (erro is JSException or JSDisconnectedException or InvalidOperationException
                                         or OperationCanceledException)
        {
            // A vigia é uma proteção: sem ela, o layout funciona como antes.
        }
    }

    /// <summary>
    /// A janela chegou ao desktop (largura de 1200px ou mais). Com o menu aberto por cima da tela, ele se fecha, e o
    /// resto deixa de ser inerte; o foco não é movido (o "Mais" não existe no desktop, e o menu lateral é o mesmo).
    /// </summary>
    [JSInvokable]
    public Task AoChegarAoDesktop() => InvokeAsync(() =>
    {
        if (!_menuAberto) return;

        _menuAberto = false;
        StateHasChanged();
    });

    // O botão clicado some: o foco vai ao par que aparece no lugar (o "Exibir menu" visível ou o "Esconder menu").
    private void AlternarMenu()
    {
        _menuEscondido = !_menuEscondido;
        _focoPendente = _menuEscondido ? SeletorDoExibirMenu : SeletorDoEsconderMenu;
    }

    private void AbrirMenu()
    {
        _menuAberto = true;
        _focoPendente = SeletorDoPrimeiroItem;
    }

    // Fechado pelo "Fechar menu", pelo véu ou pelo Esc, o foco volta ao "Mais" que está à vista.
    private void FecharMenu()
    {
        if (!_menuAberto) return;

        _menuAberto = false;
        _focoPendente = SeletorDoMais;
    }

    private void AoTeclarNoMenu(KeyboardEventArgs e)
    {
        if (e.Key == "Escape") FecharMenu();
    }

    /// <summary>
    /// Escolhido um destino no menu aberto por cima da tela, ele se fecha. Outra tela leva o foco ao título dela
    /// (FocoNoTitulo); a mesma tela (só a consulta muda, ou nada) não troca de página, e o foco volta ao "Mais".
    /// </summary>
    private void AoNavegar(object? sender, LocationChangedEventArgs e)
    {
        var caminho = Caminho(Navegacao.ToBaseRelativePath(e.Location));
        var mesmaTela = caminho == _caminho;
        _caminho = caminho;

        if (!_menuAberto) return;

        _menuAberto = false;
        if (mesmaTela) _focoPendente = SeletorDoMais;
        _ = InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        Navegacao.LocationChanged -= AoNavegar;
        _persistencia.Dispose();
        try
        {
            if (_vigia is not null)
            {
                await _vigia.InvokeVoidAsync("parar");
                await _vigia.DisposeAsync();
            }
            if (_modulo is not null) await _modulo.DisposeAsync();
        }
        catch (Exception erro) when (erro is JSException or JSDisconnectedException or OperationCanceledException)
        {
            // O circuito caiu: o navegador já descartou a página, e a vigia com ela.
        }
        finally
        {
            _referencia?.Dispose();
        }
    }
}
```

Detalhes que o implementador precisa saber:

- **`inert="@_menuAberto"`**: com valor `bool` num elemento HTML, o Blazor emite o atributo só quando `true` (como `inert`, sem valor, no HTML pré-renderizado; `setAttribute("inert", "")` no circuito) e o omite quando `false`. O HTML pré-renderizado sai sempre com o menu fechado: nenhum `inert` no HTML estático (os testes de `LayoutWebTests` que leem os `nav` continuam iguais).
- **O `aside` não fica inerte**: no desktop ele é o menu lateral sempre à vista; no tablet e no mobile, aberto, é o que sobra alcançável. O véu não pode ser `inert` (perderia o clique): por isso `tabindex="-1"` e `aria-hidden="true"`. Com o resto inerte e o véu fora do Tab, o Tab que sai do último item do menu vai para a barra do navegador e volta ao início do documento, que é o próprio menu: não há como chegar à tela de trás pelo teclado, sem armadilha de foco em JavaScript.
- **Esc**: o `@onkeydown` fica no `aside`; o `keydown` de qualquer elemento do menu sobe até ele. Com o menu fechado (o menu lateral do desktop), `FecharMenu` não faz nada e não pede foco. Cada tecla no menu desenha o layout de novo (o `EventCallback` pede um `StateHasChanged`); é o mesmo custo de um clique no "Mais".
- **Foco**: sempre depois da renderização (`OnAfterRenderAsync`), porque o elemento a focar só fica visível ou deixa de ser inerte depois que o DOM muda. O serviço `Foco` da Tarefa 3 já engole `InvalidOperationException`, `JSException`, `JSDisconnectedException` e `OperationCanceledException`: o layout não repete o `try`/`catch`.
- **A vigia do desktop (módulo próprio do layout)**: o `inert` segue o estado `_menuAberto`, não a largura. Sem a vigia, o menu aberto no tablet com a janela alargada até 1200px deixaria a coluna inerte e nada à vista para soltá-la (o "Fechar menu" e o véu só aparecem no tablet e no mobile). O `matchMedia("(min-width: 1200px)")` avisa o layout pelo `DotNetObjectReference`, e o `[JSInvokable] AoChegarAoDesktop` fecha o menu sem mover o foco. Escolhi um módulo do próprio layout (`MainLayout.razor.js`) e não o `FocoNoTitulo.razor.js`: o módulo de foco serve ao serviço `Foco` e a qualquer tela, e não deve guardar uma referência ao layout nem uma escuta de largura; o do layout morre com ele. A vigia é instalada uma vez, no primeiro desenho interativo, e não a cada abertura: o aviso só chega ao cruzar para o desktop (raro), o layout o ignora com o menu fechado, e não há instalar e desinstalar a cada clique no "Mais". A `DisposeAsync` tira a escuta (`parar`), solta a vigia e o módulo e libera o `DotNetObjectReference` (no `finally`, mesmo com o circuito caído). O layout passa de `IDisposable` a `IAsyncDisposable` por isso.
- **Os seletores**: o "Exibir menu" existe em dois lugares (a faixa do layout, `.aj-exibir-menu > button`, e a linha do título do `CabecalhoDePagina`, `.aj-cabecalho__menu > button`); o CSS mostra um só conforme a tela tem cabeçalho, e `focarPrimeiroVisivel` pega o que tem caixa. O "Mais" existe no trilho (tablet) e na barra inferior (mobile), os dois com `aria-controls="menu-principal"` e nenhum outro botão do layout com ele. O primeiro item do menu é o primeiro `NavLink` `.aj-menu__item` ("Início").
- **Ator persistente (.NET 10)**: o projeto não usava estado persistente. A forma declarativa (`[PersistentState]` numa propriedade pública) calcula a chave pela posição do componente, e o bUnit não tem como gravá-la antes do desenho; a forma imperativa (`PersistentComponentState` com `RegisterOnPersisting` e `TryTakeFromJson`, chave fixa) vale igual no .NET 10 e o bUnit a exercita (`Persist`, `TriggerOnPersisting`, `TryTake`). O `AddRazorComponents()` do `Program.cs` já registra o `PersistentComponentState`: o `Program.cs` não muda. No Blazor Server o estado vai no HTML protegido pela proteção de dados (o nome não aparece em claro) e só serve ao desenho do menu: a autorização continua no despachante, a cada consulta. O `RegisterOnPersisting` sem modo de renderização infere o modo pelo componente dono do método (o layout, interativo pelo `Routes`); se, no navegador (Tarefa 11), o menu ainda piscar ao conectar, troque por `EstadoPersistente.RegisterOnPersisting(PersistirAtor, InteractiveServer)` (o `using static …RenderMode` já está no `_Imports.razor`) e confira de novo. Se o ator não vier (estado ausente), o layout faz o que fazia antes: consulta.
- **`UsuarioAutenticado` em JSON**: o record tem construtor primário; as propriedades calculadas (`EhDiretoria`, `EhCliente`, `EhInterno`) vão no JSON e são ignoradas na volta (sem parâmetro no construtor nem `set`). O `TryTakeFromJson`/`PersistAsJson` marcam `RequiresUnreferencedCode`, mas o projeto não liga a análise de corte (nem `PublishTrimmed` nem `EnableTrimAnalyzer`): sem aviso. Se o build acusar IL2026, anote no relatório e suprima só nessas duas chamadas com `[UnconditionalSuppressMessage]` no método, explicando que o tipo é um record do próprio projeto.

- [ ] **Step 9: Build e testes**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~MainLayoutInterativoTests"`
Expected: todos passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~LayoutWebTests"`
Expected: todos passando, inclusive `Modulo_do_layout_e_servido`, sem mudança nos testes de antes (o HTML pré-renderizado sai com o menu fechado: sem `inert`, o "Mais" e os destinos com o `aria-current` de antes).

Run, um por vez (o layout de toda tela muda, e o HTML agora leva o estado persistente):

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~PortalDoClienteWebTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~InicioWebTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AutenticacaoWebTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~SessaoWebTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ComponentesDoDesignSystemTests"`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~WebSoPeloDespachante"`
Expected: todos passando, sem mudança nesses testes.

Nenhum teste existente afirma o "Mais" ou os destinos com o menu aberto (nenhum desenhava o layout no bUnit, e o HTML pré-renderizado sai sempre fechado): a mudança de `Atual` e `MaisAceso` não reescreve teste nenhum. O comportamento novo está em `Com_o_menu_aberto_o_mais_fica_so_com_o_expanded_e_o_destino_da_rota_continua_marcado` e `Com_o_menu_aberto_numa_tela_do_mais_ele_continua_marcado`.

- [ ] **Step 10: Commit**

```
git branch --show-current
git add src tests
git commit -F <arquivo da mensagem, UTF-8 sem BOM>
```

Expected do primeiro comando: `worktree-feat+fase-9g-limpeza`.

Mensagem:

```
feat(web): menu "Mais" com foco, Esc e o resto inerte, foco no par do Exibir/Esconder menu e ator da pré-renderização no circuito

Com o menu aberto por "Mais", o trilho, a coluna e a barra inferior ficam inertes (um só nav "Principal" acessível),
o foco vai ao primeiro item, o Esc fecha e, ao fechar, o foco volta ao "Mais". O aria-current do "Mais" e dos
destinos passa a dizer só a rota; o aberto é o aria-expanded. "Esconder menu" e "Exibir menu" levam o foco ao par
visível. O ator lido na pré-renderização chega ao circuito pelo estado persistente, sem nova consulta, e o bloco do
usuário leva o nome inteiro no title do texto. Uma vigia de largura (MainLayout.razor.js) fecha o menu aberto quando
a janela chega ao desktop, para a coluna não ficar inerte sem nada à vista que a solte. Primeiro teste do MainLayout
no bUnit.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

- [ ] **Step 11: O que fica para o navegador (registrar no relatório, para a Tarefa 11)**

Não há código neste passo. Liste no relatório da tarefa, para a conferência da Tarefa 11, nas larguras indicadas:

- Tablet (1024px) e mobile (390px): "Mais" abre o menu e o foco chega ao "Início" do menu; Tab e Shift+Tab percorrem só o menu (e a barra do navegador), nunca a tela de trás; Esc com o foco num item do menu fecha e o foco chega ao "Mais" visível; "Fechar menu" e o toque no véu fazem o mesmo; escolher a tela em que já se está fecha o menu e o foco chega ao "Mais"; escolher outra tela leva o foco ao título dela. Na árvore de acessibilidade (`/axnome`), com o menu aberto, um só `navigation "Principal"` e nenhum "Fechar menu" do véu.
- Confirmar que clicar no `NavLink` da própria rota dispara o `LocationChanged` no Blazor (o caso "mesma tela"); se não disparar, o menu fica aberto e o foco no item, e isso vai para o relatório como achado.
- "Mais" aceso com o menu aberto e os destinos sem o desenho de ativo enquanto ele está aberto (o CSS da Tarefa 1), nas duas barras.
- Desktop (1440px): "Esconder menu" leva o foco ao "Exibir menu" visível (o da linha do título nas telas com cabeçalho, o da faixa na página não encontrada) e "Exibir menu" o devolve ao "Esconder menu".
- Mobile: nas seis telas com ação, o Tab chega ao botão flutuante depois do último cartão; a posição do botão no canto não muda.
- Piscar ao conectar: abrir `/clientes` como Diretoria com a rede lenta e conferir que o nome, o grupo Administração e o "Mais" não mudam quando o circuito conecta.
- Nome longo: o bloco do usuário em uma linha com reticências e o nome inteiro na dica do texto (não na do "Sair").
- Menu aberto no tablet (1024px) e a janela alargada até 1280px: o menu fecha sozinho, a coluna volta a receber clique e Tab, e o foco não pula; voltar a 1024px deixa o menu fechado.

### Task 7: Painel de filtros compartilhado e filtro aplicado em Atividades e Auditoria

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Layout/PainelDeFiltros.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Agenda.razor`, `Atividades.razor`, `Auditoria.razor`, `src/AtividadesJuridicas.Web/Infra/FiltroDaTelaDeAuditoria.cs`; `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (o componente novo em `Migradas`); `tests/AtividadesJuridicas.IntegrationTests/TelaInterativa.cs` (`CamposDoPainel`, `BotaoDoPainel`)
- Test: `tests/AtividadesJuridicas.IntegrationTests/AgendaInterativaTests.cs`, `AtividadesInterativosTests.cs`, `AuditoriaInterativaTests.cs` (novo), `AtividadesWebTests.cs`, `AuditoriaWebTests.cs`, `TelasPurasTests.cs`

**Interfaces:**
- Consumes: o serviço `Foco` (Tarefa 3).
- Produces: `PainelDeFiltros` com `[Parameter, EditorRequired] string Id`, `bool Aberto`, `EventCallback AoFiltrar`, `EventCallback AoLimpar`, `EventCallback AoFechar` (Esc), `RenderFragment ChildContent` (os campos). Emite o `form.aj-painel.aj-painel--filtros` sempre no DOM, com `hidden` quando fechado (sem `aria-label`, como hoje); foco no primeiro campo ao abrir; Esc fecha; ao fechar por Esc ou por "Filtrar", o foco volta ao primeiro botão visível com `aria-controls="{Id}"` (`Foco.FocarPrimeiroVisivelAsync`).

**Regras:**
- As três telas passam a usar o componente, com a marcação dos campos de hoje (os textos e as classes não mudam).
- Atividades e Auditoria adotam o filtro aplicado (seção "Decisões"); a Auditoria passa a ter "Tentar de novo" que refaz a carga inicial quando ela falhou (C3) e não mostra a paginação da aba anterior enquanto a nova carrega (C4).

**Testes obrigatórios:** bUnit nas três telas: abrir leva o foco ao primeiro campo, Esc fecha e devolve o foco ao botão, "Filtrar" fecha e devolve o foco, `aria-controls` com o `form` presente fechado (com `hidden`); em Atividades e Auditoria, o filtro escolhido e não aplicado não vale na troca de página/chip/aba e volta aos campos ao reabrir (Review Focus 5); C3 e C4 da Auditoria.

- [ ] **Step 1: Auxiliares de teste dos seletores do painel**

Os testes desta tarefa conferem o foco pelo pedido ao módulo do layout (`focarPrimeiroVisivel`, chamado pelo serviço `Foco` da Tarefa 3), não por `ElementReference`: o `PainelDeFiltros` não conhece os campos (vêm no `ChildContent`) nem o botão que o abre (há um por visão). A Tarefa 3 já deixou na `TelaInterativa` o registro do `Foco`, o módulo de foco montado (`ModuloDeFoco`) e o auxiliar `SeletoresFocados` (os seletores pedidos a `focarPrimeiroVisivel`, na ordem), e o `Foco` registrado no `Program.cs` e nas duas coleções do `TelaDeTeste`: esta tarefa usa esses nomes, não monta o módulo de foco de novo e não registra o `Foco` outra vez.

Esta tarefa só acrescenta os dois seletores que o `PainelDeFiltros` pede, usados pelos testes das três telas. Em `tests/AtividadesJuridicas.IntegrationTests/TelaInterativa.cs` (CRLF), logo depois da propriedade `FocosDeElementoPedidos` (a linha `    protected int FocosDeElementoPedidos => JSInterop.Invocations[FocoDeElemento].Count;`, da Tarefa 3):

```csharp

    /// <summary>O que o <c>PainelDeFiltros</c> pede ao abrir: o primeiro campo do form, em ordem de documento.</summary>
    protected static string CamposDoPainel(string id) => $"#{id} input, #{id} select, #{id} textarea";

    /// <summary>O que o <c>PainelDeFiltros</c> pede ao fechar por Esc ou por "Filtrar": o botão que o abre na visão atual.</summary>
    protected static string BotaoDoPainel(string id) => $"button[aria-controls=\"{id}\"]";
```

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

- [ ] **Step 2: Testes da Agenda com o painel compartilhado (devem falhar)**

Em `tests/AtividadesJuridicas.IntegrationTests/AgendaInterativaTests.cs` (CRLF), acrescente `using Microsoft.AspNetCore.Components.Web;` aos `using` (ordem alfabética, depois de `using Microsoft.AspNetCore.Components;`).

No teste `Painel_de_filtros_do_tablet_abre_com_o_foco_no_tipo_filtrar_aplica_e_fecha_e_limpar_volta_a_todos`, três trechos mudam (o resto fica igual). As asserções reescritas, com o que cada uma prova:

| Hoje | Passa a ser | O que prova |
|---|---|---|
| `tela.FindAll("form#filtros-da-agenda").Should().BeEmpty();` (fechado ao abrir a tela) | o form no HTML com `hidden` (sem `aria-label`, como hoje), e nenhum foco pedido | o painel começa fechado (agora escondido, para o `aria-controls` valer) |
| `JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(CampoDoPainel(tela, "Tipo"));` | o pedido `focarPrimeiroVisivel` com o seletor dos campos do painel, e o primeiro elemento desse seletor é o Tipo | abrir leva o foco ao Tipo |
| `tela.FindAll("form#filtros-da-agenda").Should().BeEmpty();` (depois do "Filtrar") | o form com `hidden`, e o pedido de foco ao botão que abre o painel | "Filtrar" fecha (e, novo, devolve o foco) |

Troque

```csharp
        botao.GetAttribute("aria-expanded").Should().Be("false");
        tela.FindAll("form#filtros-da-agenda").Should().BeEmpty();

        botao.Click();

        tela.Find($"{Tablet} button[aria-controls=filtros-da-agenda]").GetAttribute("aria-expanded").Should().Be("true");
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(CampoDoPainel(tela, "Tipo"));
```

por

```csharp
        botao.GetAttribute("aria-expanded").Should().Be("false");
        // Fechado, o painel fica no HTML, escondido: o aria-controls dos dois botões aponta para um form que existe.
        tela.Find("form#filtros-da-agenda").HasAttribute("hidden").Should().BeTrue();
        SeletoresFocados.Should().BeEmpty();

        botao.Click();

        tela.Find($"{Tablet} button[aria-controls=filtros-da-agenda]").GetAttribute("aria-expanded").Should().Be("true");
        tela.Find("form#filtros-da-agenda").HasAttribute("hidden").Should().BeFalse();
        // O foco vai ao primeiro campo do painel: o pedido ao módulo e, nesse seletor, o primeiro é o Tipo.
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-da-agenda"));
        tela.Find(CamposDoPainel("filtros-da-agenda")).ParentElement!.QuerySelector("span")!.TextContent.Should().Be("Tipo");
```

e troque

```csharp
        tela.FindAll("form#filtros-da-agenda").Should().BeEmpty();
        tela.Find($"{Tablet} button[aria-controls=filtros-da-agenda]").GetAttribute("aria-expanded").Should().Be("false");
```

por

```csharp
        tela.Find("form#filtros-da-agenda").HasAttribute("hidden").Should().BeTrue();
        tela.Find($"{Tablet} button[aria-controls=filtros-da-agenda]").GetAttribute("aria-expanded").Should().Be("false");
        // "Filtrar" devolve o foco ao botão que abre o painel. São dois (o "Filtrar agenda" do mobile e o "Filtros" do
        // tablet), cada um numa visão: o navegador foca o que está à vista.
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-da-agenda"), BotaoDoPainel("filtros-da-agenda"));
        tela.FindAll(BotaoDoPainel("filtros-da-agenda")).Select(b => b.GetAttribute("aria-label") ?? Texto(b))
            .Should().Equal("Filtrar agenda", "Filtros");
```

No teste `Filtro_escolhido_no_painel_e_nao_aplicado_nao_vale_na_troca_de_periodo_e_fechar_volta_ao_aplicado`, troque

```csharp
        tela.Find(filtros).Click();   // fecha sem filtrar
        tela.FindAll("form#filtros-da-agenda").Should().BeEmpty();
```

por

```csharp
        tela.Find(filtros).Click();   // fecha sem filtrar
        tela.Find("form#filtros-da-agenda").HasAttribute("hidden").Should().BeTrue();
        // Fechar pelo próprio botão não pede foco: ele já está no botão.
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-da-agenda"));
```

(prova o mesmo de antes — fechou — e, novo, que o fechamento pelo botão não pede foco).

Depois desse teste, acrescente:

```csharp
    // Esc com o foco num campo do painel fecha como o botão: o que se escolheu e não se aplicou sai, nada é consultado,
    // e o foco volta ao botão que abre o painel. Outra tecla não fecha.
    [Fact]
    public void Esc_no_painel_fecha_sem_consultar_descarta_o_que_nao_foi_aplicado_e_devolve_o_foco_ao_botao()
    {
        var tela = Agenda("/agenda?data=2026-10-08");
        var filtros = $"{Tablet} button[aria-controls=filtros-da-agenda]";
        tela.Find(filtros).Click();
        CampoDoPainel(tela, "Tipo").Change(nameof(TipoEvento.Prazo));

        CampoDoPainel(tela, "Tipo").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        tela.Find("form#filtros-da-agenda").HasAttribute("hidden").Should().BeTrue();
        tela.Find(filtros).GetAttribute("aria-expanded").Should().Be("false");
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-da-agenda"), BotaoDoPainel("filtros-da-agenda"));
        Consultas.Should().ContainSingle();   // fechar não consulta
        // Os campos voltaram ao aplicado (nenhum): nenhuma opção marcada, e o navegador mostra "Todos".
        CampoDoPainel(tela, "Tipo").QuerySelector("option[selected]").Should().BeNull();
        CampoDoPainel(tela, "Tipo").QuerySelector("option[value='']")!.TextContent.Should().Be("Todos");

        tela.Find(filtros).Click();
        CampoDoPainel(tela, "Tipo").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        tela.Find("form#filtros-da-agenda").HasAttribute("hidden").Should().BeFalse();
        tela.Find(filtros).GetAttribute("aria-expanded").Should().Be("true");
    }
```

O `KeyDown` vai ao `select` e sobe até o `form`, que tem o `@onkeydown`, como no navegador (o bUnit faz o evento subir pelos ancestrais). Se o bUnit acusar `MissingEventHandlerException` no `select`, dispare no `form#filtros-da-agenda` e registre o desvio no relatório.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AgendaInterativaTests"`
Expected: falham três — `Painel_de_filtros_do_tablet_abre_…` e `Filtro_escolhido_no_painel_…` (o `Find("form#filtros-da-agenda")` não acha o form fechado: hoje ele só existe aberto) e `Esc_no_painel_…` (`MissingEventHandlerException`: ninguém trata o `keydown`). Os outros passam.

- [ ] **Step 3: O componente `PainelDeFiltros` e a Agenda com ele**

Crie `src/AtividadesJuridicas.Web/Components/Layout/PainelDeFiltros.razor` (UTF-8 sem BOM, CRLF):

```razor
@inject Foco Foco

@* O painel de filtros do tablet e do mobile (Agenda, Atividades, Auditoria). O form fica sempre no HTML, escondido
   quando fechado, para o aria-controls dos botões que o abrem apontar para ele. Abrir leva o foco ao primeiro campo;
   Esc fecha; fechar por Esc ou por "Filtrar" devolve o foco ao botão que abre o painel na visão atual (há um por
   visão, e o das outras está escondido). A tela decide o resto: quem abre, o que "Filtrar" e "Limpar" fazem, se o
   "Filtrar" fecha (a auditoria não fecha com um identificador inválido). *@
<form id="@Id" class="aj-painel aj-painel--filtros" hidden="@(!Aberto)" @onsubmit="FiltrarAsync" @onsubmit:preventDefault @onkeydown="AoTeclarAsync">
    @ChildContent
    <div class="aj-painel__acoes">
        <button type="button" class="aj-botao aj-botao--fantasma" @onclick="AoLimpar">Limpar</button>
        <button type="submit" class="aj-botao aj-botao--primario">Filtrar</button>
    </div>
</form>

@code {
    /// <summary>O id do form, que os botões de abrir citam no <c>aria-controls</c>.</summary>
    [Parameter, EditorRequired] public string Id { get; set; } = string.Empty;

    [Parameter] public bool Aberto { get; set; }

    /// <summary>"Filtrar": a tela aplica o filtro e, se aplicou, fecha o painel.</summary>
    [Parameter] public EventCallback AoFiltrar { get; set; }

    /// <summary>"Limpar": a tela esvazia os campos do painel, sem consultar.</summary>
    [Parameter] public EventCallback AoLimpar { get; set; }

    /// <summary>Esc: a tela fecha o painel, como o botão que o abre.</summary>
    [Parameter] public EventCallback AoFechar { get; set; }

    /// <summary>Os campos do painel, com a marcação da tela.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private bool _abertoAntes;
    private bool _devolverAoFechar;
    private bool _focarCampo;
    private bool _focarBotao;

    // Quem fecha é a tela (o Aberto que ela passa): o painel guarda que o pedido de fechar veio dele (Esc ou "Filtrar")
    // e, quando o Aberto muda para falso, devolve o foco. Se a tela não fecha (o "Filtrar" ignorado ou recusado), o
    // pedido fica guardado até a próxima mudança do Aberto.
    protected override void OnParametersSet()
    {
        if (Aberto == _abertoAntes) return;

        _abertoAntes = Aberto;
        if (Aberto)
        {
            _focarCampo = true;
            _devolverAoFechar = false;
        }
        else
        {
            _focarBotao = _devolverAoFechar;
            _devolverAoFechar = false;
        }
    }

    private Task FiltrarAsync()
    {
        _devolverAoFechar = true;
        return AoFiltrar.InvokeAsync();
    }

    private Task AoTeclarAsync(KeyboardEventArgs e)
    {
        if (e.Key != "Escape" || !Aberto) return Task.CompletedTask;

        _devolverAoFechar = true;
        return AoFechar.InvokeAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focarCampo)
        {
            _focarCampo = false;
            await Foco.FocarPrimeiroVisivelAsync($"#{Id} input, #{Id} select, #{Id} textarea");
        }
        else if (_focarBotao)
        {
            _focarBotao = false;
            await Foco.FocarPrimeiroVisivelAsync($"button[aria-controls=\"{Id}\"]");
        }
    }
}
```

O `Foco` da Tarefa 3 já engole as falhas do JavaScript (`InvalidOperationException`, `JSException`, `JSDisconnectedException`, `OperationCanceledException`): o painel não repete o `try`/`catch`.

Em `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (CRLF), em `Migradas`, logo depois de `"Components/Layout/Paginacao.razor",`:

```csharp
        "Components/Layout/PainelDeFiltros.razor",
```

Em `src/AtividadesJuridicas.Web/Components/Pages/Agenda.razor` (CRLF), troque o bloco do painel

```razor
    @* O painel abre logo abaixo da barra do tablet e, no mobile, no topo do conteúdo, sob a barra superior. *@
    @if (_filtrosAbertos)
    {
        <div class="aj-tablet-mobile">
            <form id="filtros-da-agenda" class="aj-painel aj-painel--filtros" @onsubmit="FiltrarDoPainelAsync" @onsubmit:preventDefault>
                <label class="aj-campo-filtro"><span>Tipo</span><select class="aj-campo-filtro__controle" @bind="_filtro.Tipo" @ref="_tipoDoPainel">@OpcoesDeTipo</select></label>
                @if (_ator.EhDiretoria)
                {
                    <label class="aj-campo-filtro"><span>Participante</span><select class="aj-campo-filtro__controle" @bind="_filtro.ParticipanteId">@OpcoesDeParticipante</select></label>
                }
                <div class="aj-painel__acoes">
                    <button type="button" class="aj-botao aj-botao--fantasma" @onclick="LimparFiltros">Limpar</button>
                    <button type="submit" class="aj-botao aj-botao--primario">Filtrar</button>
                </div>
            </form>
        </div>
    }
```

por

```razor
    @* O painel abre logo abaixo da barra do tablet e, no mobile, no topo do conteúdo, sob a barra superior. Fechado,
       fica no HTML, escondido, para o aria-controls dos dois botões apontar para ele. *@
    <div class="aj-tablet-mobile">
        <PainelDeFiltros Id="filtros-da-agenda" Aberto="_filtrosAbertos" AoFiltrar="FiltrarDoPainelAsync" AoLimpar="LimparFiltros" AoFechar="FecharFiltros">
            <label class="aj-campo-filtro"><span>Tipo</span><select class="aj-campo-filtro__controle" @bind="_filtro.Tipo">@OpcoesDeTipo</select></label>
            @if (_ator?.EhDiretoria == true)
            {
                <label class="aj-campo-filtro"><span>Participante</span><select class="aj-campo-filtro__controle" @bind="_filtro.ParticipanteId">@OpcoesDeParticipante</select></label>
            }
        </PainelDeFiltros>
    </div>
```

(Os campos, os textos e as classes não mudam; sai só o `@ref`, porque o foco do primeiro campo passou ao componente. O `_ator?.` é porque o conteúdo do componente é uma função à parte, e a análise de nulos não garante ali o `_ator` do `else`.)

No `@code` da Agenda:

- apague os campos `private bool _focarPainel;` e `private ElementReference _tipoDoPainel;`;
- apague o `OnAfterRenderAsync` inteiro (do `protected override async Task OnAfterRenderAsync(bool firstRender)` ao `}` que o fecha, com o comentário de dentro);
- troque o `AlternarFiltros`

```csharp
    private void AlternarFiltros()
    {
        _filtrosAbertos = !_filtrosAbertos;
        _focarPainel = _filtrosAbertos;
        _filtro.Voltar(_filtroAplicado);
    }
```

por

```csharp
    private void AlternarFiltros()
    {
        _filtrosAbertos = !_filtrosAbertos;
        _filtro.Voltar(_filtroAplicado);
    }

    // Esc no painel: fecha como o botão, descartando o que se escolheu e não se aplicou.
    private void FecharFiltros()
    {
        _filtrosAbertos = false;
        _filtro.Voltar(_filtroAplicado);
    }
```

(o comentário de cima do `AlternarFiltros` fica como está).

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AgendaInterativaTests"`
Expected: todos passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AgendaWebTests"`
Expected: todos passando (o painel escondido entra no bloco `aj-tablet-mobile`, que só tem asserções positivas sobre os dias).

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Expected: todos passando, com o `PainelDeFiltros.razor` no theory da trava (nenhum `style`, classe só `aj-`, os dois `button` com classe).

Fica para o navegador (Tarefa 11): o foco chegar ao Tipo e voltar ao botão visível (o "Filtros" no tablet, o "Filtrar agenda" no mobile); o `hidden` vencer o `display:flex` do `.aj-painel` (o `componentes.css` já tem `.aj-painel[hidden]{display:none}`, de especificidade maior); o invólucro `aj-tablet-mobile` com o form escondido não abrir espaço no `main` (ele é `display:contents`); o Esc com a lista de um `select` aberta (o navegador fecha a lista primeiro; o segundo Esc fecha o painel).

- [ ] **Step 4: Commit da Agenda**

Confira o branch (`git branch --show-current` deve dizer `worktree-feat+fase-9g-limpeza`) e:

```
git add src/AtividadesJuridicas.Web/Components/Layout/PainelDeFiltros.razor src/AtividadesJuridicas.Web/Components/Pages/Agenda.razor tests/AtividadesJuridicas.IntegrationTests/AgendaInterativaTests.cs tests/AtividadesJuridicas.IntegrationTests/TelaInterativa.cs tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs
git commit -F <arquivo com a mensagem, UTF-8 sem BOM>
```

Mensagem:

```
feat(web): painel de filtros compartilhado, sempre no HTML, com Esc e o foco ao abrir e ao fechar, na agenda

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

- [ ] **Step 5: Testes de Atividades com o painel e o filtro aplicado (devem falhar)**

Em `tests/AtividadesJuridicas.IntegrationTests/AtividadesInterativosTests.cs` (CRLF), acrescente `using Microsoft.AspNetCore.Components.Web;` aos `using` (depois de `using Bunit;`).

Depois de `private static PaginaDeAtividades Pagina() => …;`:

```csharp
    // Três páginas de 20 (45 atividades no escopo, uma atrasada), com o número que a consulta pediu.
    private static PaginaDeAtividades PaginaDeTres(int numero) => new([Contestacao], 45, numero, 20, 45, 1);

    private const string Painel = "form#filtros-de-atividades";
    private const string BotaoQueAbre = "button[aria-controls=filtros-de-atividades]";
```

Depois do método `Lista`:

```csharp
    private IRenderedComponent<Atividades> ListaDeTresPaginas(UsuarioAutenticado ator)
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(ator)
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Diana, Pedro]))
            .Responde<ListarAtividades, Resultado<PaginaDeAtividades>>(c => Resultado<PaginaDeAtividades>.Ok(PaginaDeTres(c.Pagina)));
        return Desenhar<Atividades>();
    }
```

O teste `Chip_de_situacao_busca_na_hora_mantendo_a_busca_e_o_responsavel` afirmava a regra antiga (a busca digitada e o responsável escolhido, sem "Buscar", valiam no chip), que esta tarefa troca pela do filtro aplicado (seção "Decisões"). Ele passa a provar o mesmo sobre o filtro aplicado — o chip mantém a busca e o responsável **aplicados** — e o caso do não aplicado vai ao teste novo `Busca_e_campos_trocados_sem_buscar_nao_valem_no_chip_nem_na_troca_de_pagina`. Troque o início dele

```csharp
        var tela = Lista(AtorDaDiretoria);
        tela.Find("form.aj-linha-filtros input").Change("contestação");
        CampoDaBarra(tela, "Responsável").Change(Pedro.Id.ToString());

        Chip(tela, "Feitas").Click();
```

por

```csharp
        var tela = Lista(AtorDaDiretoria);
        // A busca e o responsável aplicados pelo "Buscar" da barra do desktop.
        tela.Find("form.aj-barra-filtros input[type=text]").Change("contestação");
        CampoDaBarra(tela, "Responsável").Change(Pedro.Id.ToString());
        tela.Find("form.aj-barra-filtros").Submit();

        Chip(tela, "Feitas").Click();
```

(o resto do teste fica igual).

No teste `Painel_de_filtros_abre_filtrar_aplica_e_fecha_e_limpar_limpa_sem_tirar_a_busca`, troque

```csharp
        Texto(tela.Find("button[aria-controls=filtros-de-atividades]")).Should().Be("Responsável, revisor");

        tela.Find("button[aria-controls=filtros-de-atividades]").Click();

        tela.Find("button[aria-controls=filtros-de-atividades]").GetAttribute("aria-expanded").Should().Be("true");
```

por

```csharp
        Texto(tela.Find("button[aria-controls=filtros-de-atividades]")).Should().Be("Responsável, revisor");
        tela.Find(Painel).HasAttribute("hidden").Should().BeTrue();

        tela.Find("button[aria-controls=filtros-de-atividades]").Click();

        tela.Find("button[aria-controls=filtros-de-atividades]").GetAttribute("aria-expanded").Should().Be("true");
        tela.Find(Painel).HasAttribute("hidden").Should().BeFalse();
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-de-atividades"));
```

e troque

```csharp
        tela.FindAll("form#filtros-de-atividades").Should().BeEmpty();
        tela.Find("button[aria-controls=filtros-de-atividades]").GetAttribute("aria-expanded").Should().Be("false");
```

por

```csharp
        tela.Find(Painel).HasAttribute("hidden").Should().BeTrue();
        tela.Find("button[aria-controls=filtros-de-atividades]").GetAttribute("aria-expanded").Should().Be("false");
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-de-atividades"), BotaoDoPainel("filtros-de-atividades"));
```

(a primeira reescrita prova o mesmo — fechou depois do "Filtrar" — sobre o form que agora fica no HTML; as linhas novas provam o foco ao abrir e ao fechar).

Acrescente, no fim da classe:

```csharp
    // Abrir leva o foco ao primeiro campo do painel (a Situação); Esc com o foco num campo fecha sem consultar, devolve
    // os campos ao aplicado e o foco ao botão que abre o painel.
    [Fact]
    public void Painel_abre_com_o_foco_no_primeiro_campo_e_esc_fecha_sem_consultar_e_devolve_o_foco_ao_botao()
    {
        var tela = Lista(AtorDaDiretoria);
        tela.Find(Painel).HasAttribute("hidden").Should().BeTrue();

        tela.Find(BotaoQueAbre).Click();

        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-de-atividades"));
        tela.Find(CamposDoPainel("filtros-de-atividades")).ParentElement!.QuerySelector("span")!.TextContent.Should().Be("Situação");
        var consultas = Despachante.Recebidas.OfType<ListarAtividades>().Count();
        CampoDoPainel(tela, "Situação").Change("Criada");

        CampoDoPainel(tela, "Situação").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        tela.Find(Painel).HasAttribute("hidden").Should().BeTrue();
        tela.Find(BotaoQueAbre).GetAttribute("aria-expanded").Should().Be("false");
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-de-atividades"), BotaoDoPainel("filtros-de-atividades"));
        Despachante.Recebidas.OfType<ListarAtividades>().Should().HaveCount(consultas);   // fechar não consulta
        Escolhida(CampoDoPainel(tela, "Situação")).Should().BeEmpty();   // de volta ao aplicado: nenhuma
    }

    // Review Focus 5: o que se escolhe no painel e não se aplica não vale na troca de página nem de chip, e o painel
    // reaberto mostra o filtro aplicado, e não o que ficou escolhido.
    [Fact]
    public void Filtro_escolhido_no_painel_e_nao_aplicado_nao_vale_na_troca_de_pagina_nem_de_chip_e_reabrir_mostra_o_aplicado()
    {
        var tela = ListaDeTresPaginas(AtorDaDiretoria);
        tela.Find(BotaoQueAbre).Click();
        CampoDoPainel(tela, "Responsável").Change(Pedro.Id.ToString());
        CampoDoPainel(tela, "Revisor").Change(Diana.Id.ToString());
        tela.Find($"{Painel} label.aj-opcao input").Change(true);
        tela.Find(BotaoQueAbre).Click();   // fecha sem filtrar

        tela.Find(".aj-so-tablet .aj-paginacao__proxima").Click();

        UltimaConsulta.Pagina.Should().Be(2);
        UltimaConsulta.ResponsavelId.Should().BeNull();
        UltimaConsulta.RevisorId.Should().BeNull();
        UltimaConsulta.SomenteAtrasadas.Should().BeFalse();

        // Com o painel aberto e um revisor escolhido sem filtrar, o chip também consulta com o aplicado.
        tela.Find(BotaoQueAbre).Click();
        CampoDoPainel(tela, "Revisor").Change(Diana.Id.ToString());
        Chip(tela, "Feitas").Click();

        UltimaConsulta.Status.Should().Be(StatusAtividade.Feita);
        UltimaConsulta.Pagina.Should().Be(1);
        UltimaConsulta.RevisorId.Should().BeNull();

        // Fechado e reaberto, o painel mostra o aplicado: a situação do chip, nenhum responsável, nenhum revisor.
        tela.Find(BotaoQueAbre).Click();
        tela.Find(BotaoQueAbre).Click();
        Escolhida(CampoDoPainel(tela, "Situação")).Should().Be("Feita");
        Escolhida(CampoDoPainel(tela, "Responsável")).Should().BeEmpty();
        Escolhida(CampoDoPainel(tela, "Revisor")).Should().BeEmpty();
        tela.Find($"{Painel} label.aj-opcao input").HasAttribute("checked").Should().BeFalse();
    }

    // A busca digitada e os campos da barra trocados sem "Buscar" não valem no chip nem na troca de página: a consulta
    // lê o filtro aplicado.
    [Fact]
    public void Busca_e_campos_trocados_sem_buscar_nao_valem_no_chip_nem_na_troca_de_pagina()
    {
        var tela = ListaDeTresPaginas(AtorDaDiretoria);
        tela.Find("form.aj-linha-filtros input").Change("contestação");
        CampoDaBarra(tela, "Responsável").Change(Pedro.Id.ToString());

        Chip(tela, "Feitas").Click();

        UltimaConsulta.Status.Should().Be(StatusAtividade.Feita);
        UltimaConsulta.Busca.Should().BeNull();
        UltimaConsulta.ResponsavelId.Should().BeNull();

        tela.Find(".aj-so-tablet .aj-paginacao__proxima").Click();

        UltimaConsulta.Pagina.Should().Be(2);
        UltimaConsulta.Status.Should().Be(StatusAtividade.Feita);
        UltimaConsulta.Busca.Should().BeNull();
        UltimaConsulta.ResponsavelId.Should().BeNull();

        // Aplicado pelo Enter da busca do tablet, vale: a busca e o responsável que estão nos campos.
        tela.Find("form.aj-linha-filtros").Submit();

        UltimaConsulta.Pagina.Should().Be(1);
        UltimaConsulta.Busca.Should().Be("contestação");
        UltimaConsulta.ResponsavelId.Should().Be(Pedro.Id);
        UltimaConsulta.Status.Should().Be(StatusAtividade.Feita);
    }
```

Em `tests/AtividadesJuridicas.IntegrationTests/AtividadesWebTests.cs` (CRLF), no teste `Diretor_ve_todas_as_atividades_e_as_acoes_de_cadastro`, logo antes de `var cartoes = BlocoDeVisao.De(html, "aj-tablet-mobile");`:

```csharp
        // O painel de filtros fica no HTML, escondido, para o aria-controls dos dois botões (o do tablet e o "Mais
        // filtros" da barra do mobile) apontar para um form que existe.
        html.Should().MatchRegex(
            "<div class=\"aj-tablet-mobile\">\\s*<form id=\"filtros-de-atividades\" class=\"aj-painel aj-painel--filtros\" hidden[^>]*>\\s*" +
            "<label class=\"aj-campo-filtro\">\\s*<span>Situação</span>");
```

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadesInterativosTests"`
Expected: falham quatro — `Painel_de_filtros_abre_filtrar_…` e `Painel_abre_com_o_foco_…` (o form fechado não está no HTML), `Filtro_escolhido_no_painel_e_nao_aplicado_…` (a página 2 consulta com o responsável e o revisor escolhidos e não aplicados) e `Busca_e_campos_trocados_sem_buscar_…` (o chip consulta com a busca "contestação" e o responsável não aplicados). Os outros passam, inclusive o `Chip_de_situacao_…` reescrito.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadesWebTests"`
Expected: falha `Diretor_ve_todas_as_atividades_e_as_acoes_de_cadastro` (o painel fechado não está no HTML); os outros passam.

- [ ] **Step 6: Atividades com o painel compartilhado e o filtro aplicado**

Em `src/AtividadesJuridicas.Web/Components/Pages/Atividades.razor` (CRLF):

Troque o comentário

```razor
    @* Desktop: os campos aplicam ao clicar "Buscar"; a paginação e os chips consultam com o que está nos campos (como em Processos.razor). *@
```

por

```razor
    @* Desktop: os campos aplicam ao clicar "Buscar"; a paginação e os chips consultam com o filtro aplicado (o do último
       "Buscar" ou "Filtrar"), como na agenda. *@
```

Troque o bloco do painel

```razor
    @* O painel abre logo abaixo da linha do tablet e, no mobile, no topo do conteúdo, sob a barra superior. *@
    @if (_filtrosAbertos)
    {
        <div class="aj-tablet-mobile">
            <form id="filtros-de-atividades" class="aj-painel aj-painel--filtros" @onsubmit="FiltrarDoPainelAsync" @onsubmit:preventDefault>
                @CampoDeSituacao
                @if (_ator.EhDiretoria)
                {
                    @CamposDePessoas
                }
                <label class="aj-opcao"><input type="checkbox" class="aj-opcao__caixa" @bind="_filtro.SoAtrasadas" />Só atrasadas</label>
                <div class="aj-painel__acoes">
                    <button type="button" class="aj-botao aj-botao--fantasma" @onclick="LimparFiltros">Limpar</button>
                    <button type="submit" class="aj-botao aj-botao--primario">Filtrar</button>
                </div>
            </form>
        </div>
    }
```

por

```razor
    @* O painel abre logo abaixo da linha do tablet e, no mobile, no topo do conteúdo, sob a barra superior. Fechado,
       fica no HTML, escondido, para o aria-controls dos dois botões apontar para ele. *@
    <div class="aj-tablet-mobile">
        <PainelDeFiltros Id="filtros-de-atividades" Aberto="_filtrosAbertos" AoFiltrar="FiltrarDoPainelAsync" AoLimpar="LimparFiltros" AoFechar="FecharFiltros">
            @CampoDeSituacao
            @if (_ator?.EhDiretoria == true)
            {
                @CamposDePessoas
            }
            <label class="aj-opcao"><input type="checkbox" class="aj-opcao__caixa" @bind="_filtro.SoAtrasadas" />Só atrasadas</label>
        </PainelDeFiltros>
    </div>
```

No `@code`:

1. Depois de `private readonly Filtro _filtro = new();`:

```csharp
    private FiltroAplicado _aplicado = FiltroAplicado.Nenhum;
```

2. No `ConsultarAsync`, a consulta lê o aplicado. Troque

```csharp
        var resultado = await Despachante.EnviarAsync(new ListarAtividades(
            _filtro.Situacao, _filtro.ResponsavelId, _filtro.RevisorId, null, null, _filtro.SoAtrasadas, _filtro.Busca, pagina));
```

por

```csharp
        var resultado = await Despachante.EnviarAsync(new ListarAtividades(
            _aplicado.Situacao, _aplicado.ResponsavelId, _aplicado.RevisorId, null, null, _aplicado.SoAtrasadas, _aplicado.Busca, pagina));
```

3. Troque o `BuscarAsync`, o comentário e o `EscolherAsync`, o `AlternarFiltros` e o comentário do `LimparFiltros` — de `private Task BuscarAsync()` até o fim do `AlternarFiltros` (a linha `private void AlternarFiltros() => _filtrosAbertos = !_filtrosAbertos;`), deixando no meio os três `Escolher…Async` de uma linha como estão — por:

```csharp
    // "Buscar" (a barra do desktop, a linha do tablet, a busca do mobile) e o "Filtrar" do painel gravam o filtro
    // aplicado: tudo o que está nos campos. A troca de página e o "Tentar de novo" consultam com o aplicado.
    private Task BuscarAsync()
    {
        if (_ocupado) return Task.CompletedTask;

        _aplicado = _filtro.Aplicar();
        _recado = null;
        return CarregarAsync(1);
    }

    private Task EscolherTodasAsync() => EscolherAsync(null, somenteAtrasadas: false);

    private Task EscolherAtrasadasAsync() => EscolherAsync(null, somenteAtrasadas: true);

    private Task EscolherSituacaoAsync(StatusAtividade situacao) => EscolherAsync(situacao, somenteAtrasadas: false);

    // O chip troca a situação e o "só atrasadas" do filtro aplicado e busca na hora; a busca, o responsável e o revisor
    // aplicados ficam (o que está nos campos e não foi aplicado, não). Os campos de situação passam a mostrar a do chip.
    // Com outra consulta em curso, o chip não muda nada (senão marcaria um filtro que não foi aplicado).
    private Task EscolherAsync(StatusAtividade? situacao, bool somenteAtrasadas)
    {
        if (_ocupado) return Task.CompletedTask;

        _filtro.Situacao = situacao;
        _filtro.SoAtrasadas = somenteAtrasadas;
        _aplicado = _aplicado with { Situacao = situacao, SoAtrasadas = somenteAtrasadas };
        _recado = null;
        return CarregarAsync(1);
    }

    // Abrir ou fechar o painel pelo botão descarta o que se escolheu nele e não se aplicou: os campos do painel voltam ao
    // filtro aplicado (a busca, que fica fora dele, não muda).
    private void AlternarFiltros()
    {
        _filtrosAbertos = !_filtrosAbertos;
        _filtro.VoltarOsDoPainel(_aplicado);
    }

    // Esc no painel: fecha como o botão.
    private void FecharFiltros()
    {
        _filtrosAbertos = false;
        _filtro.VoltarOsDoPainel(_aplicado);
    }
```

O `LimparFiltros` e o `FiltrarDoPainelAsync` ficam como estão (o "Filtrar" do painel chama o `BuscarAsync`, que grava o aplicado; o comentário "Só vale ao filtrar" do `LimparFiltros` continua certo).

4. Os chips mostram o filtro aplicado (o que a lista está mostrando). Troque

```csharp
    private string PressionadoTodas => _filtro.Situacao is null && !_filtro.SoAtrasadas ? "true" : "false";

    private string PressionadoAtrasadas => _filtro.SoAtrasadas ? "true" : "false";

    private string Pressionado(StatusAtividade situacao) => _filtro.Situacao == situacao ? "true" : "false";
```

por

```csharp
    // Os chips mostram o filtro aplicado: o que a lista está mostrando, e não o que está escolhido no painel.
    private string PressionadoTodas => _aplicado.Situacao is null && !_aplicado.SoAtrasadas ? "true" : "false";

    private string PressionadoAtrasadas => _aplicado.SoAtrasadas ? "true" : "false";

    private string Pressionado(StatusAtividade situacao) => _aplicado.Situacao == situacao ? "true" : "false";
```

5. Troque a classe `Filtro` do fim do arquivo

```csharp
    /// <summary>O estado único dos filtros: as três visões o mostram e o mudam.</summary>
    private sealed class Filtro
    {
        public string? Busca { get; set; }
        public StatusAtividade? Situacao { get; set; }
        public Guid? ResponsavelId { get; set; }
        public Guid? RevisorId { get; set; }
        public bool SoAtrasadas { get; set; }
    }
```

por

```csharp
    /// <summary>
    /// O filtro em edição: as três visões o mostram e o mudam. A consulta não o lê: lê o <see cref="FiltroAplicado"/>,
    /// que só o "Buscar", o "Filtrar" do painel e os chips gravam.
    /// </summary>
    private sealed class Filtro
    {
        public string? Busca { get; set; }
        public StatusAtividade? Situacao { get; set; }
        public Guid? ResponsavelId { get; set; }
        public Guid? RevisorId { get; set; }
        public bool SoAtrasadas { get; set; }

        public FiltroAplicado Aplicar() => new(Busca, Situacao, ResponsavelId, RevisorId, SoAtrasadas);

        /// <summary>Os campos do painel (situação, responsável, revisor, só atrasadas) voltam ao aplicado; a busca fica.</summary>
        public void VoltarOsDoPainel(FiltroAplicado aplicado)
        {
            Situacao = aplicado.Situacao;
            ResponsavelId = aplicado.ResponsavelId;
            RevisorId = aplicado.RevisorId;
            SoAtrasadas = aplicado.SoAtrasadas;
        }
    }

    /// <summary>O filtro da última busca, o que a lista está mostrando.</summary>
    private sealed record FiltroAplicado(string? Busca, StatusAtividade? Situacao, Guid? ResponsavelId, Guid? RevisorId, bool SoAtrasadas)
    {
        public static readonly FiltroAplicado Nenhum = new(null, null, null, null, false);
    }
```

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadesInterativosTests"`
Expected: todos passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadesWebTests"`
Expected: todos passando (o `Recado_de_outra_tela_aparece_uma_vez_no_topo`, que desenha pelo `TelaDeTeste`, acha o `Foco` que a Tarefa 3 registrou no `TelaDeTeste`).

Fica para o navegador (Tarefa 11): o foco chegar à Situação e voltar ao botão visível (o "Responsável, revisor"/"Mais filtros" do tablet, o "Mais filtros" da barra do mobile); o Enter na busca do mobile continuar enviando o `busca-de-atividades`.

- [ ] **Step 7: Commit de Atividades**

Confira o branch (`git branch --show-current` deve dizer `worktree-feat+fase-9g-limpeza`) e:

```
git add src/AtividadesJuridicas.Web/Components/Pages/Atividades.razor tests/AtividadesJuridicas.IntegrationTests/AtividadesInterativosTests.cs tests/AtividadesJuridicas.IntegrationTests/AtividadesWebTests.cs
git commit -F <arquivo com a mensagem, UTF-8 sem BOM>
```

Mensagem:

```
feat(web): atividades com o painel de filtros compartilhado e a consulta lendo o filtro aplicado

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

- [ ] **Step 8: Testes da Auditoria — painel, filtro aplicado, C3 e C4 (devem falhar)**

Crie `tests/AtividadesJuridicas.IntegrationTests/AuditoriaInterativaTests.cs` (UTF-8 sem BOM, CRLF):

```csharp
using AngleSharp.Dom;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using AuditoriaPagina = AtividadesJuridicas.Web.Components.Pages.Auditoria;

namespace AtividadesJuridicas.IntegrationTests;

public class AuditoriaInterativaTests : TelaInterativa
{
    private const string Painel = "form#filtros-da-auditoria";
    private const string BotaoDoTablet = ".aj-so-tablet button[aria-controls=filtros-da-auditoria]";

    private static readonly FusoDoEscritorio Fuso = new("America/Sao_Paulo");

    private static readonly UsuarioResumo Diana = new(
        AtorDaDiretoria.Id, "Diana Diretora", "diana@x.com", AtorDaDiretoria.PerfilId, "Diretoria", TipoPerfil.Diretoria, null, true, null);

    private static readonly UsuarioResumo Pedro = new(
        AtorDaProducao.Id, "Pedro Produção", "pedro@x.com", AtorDaProducao.PerfilId, "Produção", TipoPerfil.Producao, null, true, null);

    // 120 alterações: três páginas de 50, com o número que a consulta pediu (as linhas não importam aqui).
    private static Resultado<Pagina<RegistroAuditoriaResumo>> Alteracoes(int pagina) =>
        Resultado<Pagina<RegistroAuditoriaResumo>>.Ok(new Pagina<RegistroAuditoriaResumo>([], 120, pagina, ConsultaDeAuditoriaOpcoes.TamanhoDaPagina));

    private static Resultado<Pagina<RegistroAcessoResumo>> Acessos(int total) =>
        Resultado<Pagina<RegistroAcessoResumo>>.Ok(new Pagina<RegistroAcessoResumo>([], total, 1, ConsultaDeAuditoriaOpcoes.TamanhoDaPagina));

    private IRenderedComponent<AuditoriaPagina> Tela()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Diana, Pedro]))
            .Responde<ConsultarAuditoria, Resultado<Pagina<RegistroAuditoriaResumo>>>(c => Alteracoes(c.Pagina))
            .Responde<ConsultarAcessos, Resultado<Pagina<RegistroAcessoResumo>>>(Acessos(7));
        return Desenhar<AuditoriaPagina>();
    }

    private List<ConsultarAuditoria> ConsultasDeAlteracoes => [.. Despachante.Recebidas.OfType<ConsultarAuditoria>()];

    private List<ConsultarAcessos> ConsultasDeAcessos => [.. Despachante.Recebidas.OfType<ConsultarAcessos>()];

    // Um campo do painel pelo nome ("Usuário", "Entidade", "Gravação"…): o select ou o input do rótulo.
    private static IElement CampoDoPainel(IRenderedComponent<AuditoriaPagina> tela, string nome) =>
        tela.Find(Painel).QuerySelectorAll("label.aj-campo-filtro")
            .Single(l => l.QuerySelector("span")!.TextContent == nome).QuerySelector("select, input")!;

    private static IElement Botao(IRenderedComponent<AuditoriaPagina> tela, string onde, string texto) =>
        tela.FindAll($"{onde} button").Single(b => b.TextContent == texto);

    [Fact]
    public void Painel_fica_no_html_fechado_abre_com_o_foco_no_primeiro_campo_e_filtrar_fecha_e_devolve_o_foco()
    {
        var tela = Tela();
        tela.Find(Painel).HasAttribute("hidden").Should().BeTrue();
        // Os dois botões (o do tablet e o do mobile) apontam para o painel, que existe mesmo fechado.
        tela.FindAll(BotaoDoPainel("filtros-da-auditoria")).Should().HaveCount(2);
        SeletoresFocados.Should().BeEmpty();

        tela.Find(BotaoDoTablet).Click();

        tela.Find(Painel).HasAttribute("hidden").Should().BeFalse();
        tela.Find(BotaoDoTablet).GetAttribute("aria-expanded").Should().Be("true");
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-da-auditoria"));
        tela.Find(CamposDoPainel("filtros-da-auditoria")).ParentElement!.QuerySelector("span")!.TextContent.Should().Be("Usuário");

        CampoDoPainel(tela, "Usuário").Change(Pedro.Id.ToString());
        tela.Find(Painel).Submit();

        ConsultasDeAlteracoes.Last().Filtro.UsuarioId.Should().Be(Pedro.Id);
        tela.Find(Painel).HasAttribute("hidden").Should().BeTrue();
        tela.Find(BotaoDoTablet).GetAttribute("aria-expanded").Should().Be("false");
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-da-auditoria"), BotaoDoPainel("filtros-da-auditoria"));
    }

    [Fact]
    public void Esc_no_painel_fecha_sem_consultar_volta_os_campos_ao_aplicado_e_devolve_o_foco_ao_botao()
    {
        var tela = Tela();
        tela.Find(BotaoDoTablet).Click();
        CampoDoPainel(tela, "Usuário").Change(Pedro.Id.ToString());
        var consultas = ConsultasDeAlteracoes.Count;

        CampoDoPainel(tela, "Usuário").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        tela.Find(Painel).HasAttribute("hidden").Should().BeTrue();
        tela.Find(BotaoDoTablet).GetAttribute("aria-expanded").Should().Be("false");
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-da-auditoria"), BotaoDoPainel("filtros-da-auditoria"));
        ConsultasDeAlteracoes.Should().HaveCount(consultas);   // fechar não consulta
        // De volta ao aplicado (nenhum usuário): nenhuma opção marcada, e o navegador mostra "Todos".
        CampoDoPainel(tela, "Usuário").QuerySelector("option[selected]").Should().BeNull();
        CampoDoPainel(tela, "Usuário").QuerySelector("option[value='']")!.TextContent.Should().Be("Todos");
    }

    // Um "Filtrar" que não fecha o painel (identificador inválido) não leva o foco embora: ele fica no painel, com o
    // campo a corrigir. Corrigido e aplicado, o painel fecha e devolve o foco.
    [Fact]
    public void Filtrar_com_identificador_invalido_deixa_o_painel_aberto_sem_tirar_o_foco_dele()
    {
        var tela = Tela();
        tela.Find(BotaoDoTablet).Click();
        CampoDoPainel(tela, "Gravação").Change("lixo");

        tela.Find(Painel).Submit();

        tela.Markup.Should().Contain("Identificador inválido.");
        tela.Find(Painel).HasAttribute("hidden").Should().BeFalse();
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-da-auditoria"));   // só o da abertura

        CampoDoPainel(tela, "Gravação").Change("");
        tela.Find(Painel).Submit();

        tela.Find(Painel).HasAttribute("hidden").Should().BeTrue();
        tela.Markup.Should().NotContain("Identificador inválido.").And.Contain("Página 1 de 3");
        SeletoresFocados.Should().Equal(CamposDoPainel("filtros-da-auditoria"), BotaoDoPainel("filtros-da-auditoria"));
    }

    // Review Focus 5: o que se escolhe no painel e não se aplica não vale na troca de página nem de aba, e o painel
    // reaberto mostra o filtro aplicado.
    [Fact]
    public void Filtro_escolhido_no_painel_e_nao_aplicado_nao_vale_na_troca_de_pagina_nem_de_aba_e_reabrir_mostra_o_aplicado()
    {
        var tela = Tela();
        tela.Find(BotaoDoTablet).Click();
        CampoDoPainel(tela, "Usuário").Change(Pedro.Id.ToString());
        CampoDoPainel(tela, "Entidade").Change("Perfil");
        tela.Find(BotaoDoTablet).Click();   // fecha sem filtrar

        Botao(tela, ".aj-paginacao", "Próxima").Click();

        var consulta = ConsultasDeAlteracoes.Last();
        consulta.Pagina.Should().Be(2);
        consulta.Filtro.UsuarioId.Should().BeNull();
        consulta.Filtro.Entidade.Should().BeNull();

        // Com o painel aberto e um usuário escolhido sem filtrar, a troca de aba também consulta com o aplicado.
        tela.Find(BotaoDoTablet).Click();
        CampoDoPainel(tela, "Usuário").Change(Pedro.Id.ToString());
        Botao(tela, ".aj-segmentado", "Consultas").Click();

        ConsultasDeAcessos.Should().ContainSingle().Which.Filtro.UsuarioId.Should().BeNull();

        // Fechado e reaberto, o painel mostra o aplicado (nenhum usuário), e não o que ficou escolhido.
        tela.Find(BotaoDoTablet).Click();
        tela.Find(BotaoDoTablet).Click();
        CampoDoPainel(tela, "Usuário").QuerySelector("option[selected]").Should().BeNull();
        CampoDoPainel(tela, "Usuário").QuerySelector("option[value='']")!.TextContent.Should().Be("Todos");
    }

    // O período, na linha do tablet e do mobile, também só vale depois do "Filtrar" (o Enter da linha).
    [Fact]
    public void Periodo_trocado_na_linha_sem_filtrar_nao_vale_na_troca_de_pagina()
    {
        var tela = Tela();
        tela.Find("form.aj-linha-filtros input[type=date]").Change("2026-10-01");

        Botao(tela, ".aj-paginacao", "Próxima").Click();

        ConsultasDeAlteracoes.Last().Pagina.Should().Be(2);
        ConsultasDeAlteracoes.Last().Filtro.DeUtc.Should().BeNull();

        tela.Find("form.aj-linha-filtros").Submit();

        ConsultasDeAlteracoes.Last().Pagina.Should().Be(1);
        ConsultasDeAlteracoes.Last().Filtro.DeUtc.Should().Be(Fuso.InicioDoDiaEmUtc(new DateOnly(2026, 10, 1)));
    }

    // C3: a lista de usuários do filtro faz parte da carga inicial; se é ela que falha, "Tentar de novo" a refaz (e
    // depois consulta a primeira página), em vez de consultar só os registros.
    [Fact]
    public void Falha_ao_carregar_os_usuarios_e_tentar_de_novo_refaz_a_carga_inicial()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(
                Resultado<IReadOnlyList<UsuarioResumo>>.Falha(TipoErro.Validacao, "Falha de teste."))
            .Responde<ConsultarAuditoria, Resultado<Pagina<RegistroAuditoriaResumo>>>(c => Alteracoes(c.Pagina));
        var tela = Desenhar<AuditoriaPagina>();

        tela.Markup.Should().Contain("Falha de teste.").And.Contain(">Tentar de novo</button>");
        ConsultasDeAlteracoes.Should().BeEmpty();

        Despachante.Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Diana, Pedro]));
        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().NotContain("Falha de teste.").And.Contain("Página 1 de 3"));
        Despachante.Recebidas.OfType<ListarUsuarios>().Should().HaveCount(2);
        ConsultasDeAlteracoes.Should().ContainSingle().Which.Pagina.Should().Be(1);
        // Os usuários chegaram ao filtro.
        CampoDoPainel(tela, "Usuário").QuerySelectorAll("option").Select(o => o.TextContent)
            .Should().Equal("Todos", "Diana Diretora", "Pedro Produção");
    }

    // C4: enquanto a aba nova carrega, a paginação sai (não mostra os números de outra consulta) e volta com os da nova.
    [Fact]
    public void Enquanto_a_aba_nova_carrega_a_paginacao_sai_e_volta_com_os_numeros_dela()
    {
        var tela = Tela();
        tela.Find(".aj-paginacao .aj-subtitulo").TextContent.Should().Be("Página 1 de 3 · 120 registros");
        var acessos = new TaskCompletionSource<Resultado<Pagina<RegistroAcessoResumo>>>();
        Despachante.RespondeDepois<ConsultarAcessos, Resultado<Pagina<RegistroAcessoResumo>>>(acessos.Task);

        Botao(tela, ".aj-segmentado", "Consultas").Click();

        tela.Markup.Should().Contain("Carregando…");
        tela.FindAll(".aj-paginacao").Should().BeEmpty();

        acessos.SetResult(Acessos(7));

        tela.WaitForAssertion(() => tela.Find(".aj-paginacao .aj-subtitulo").TextContent.Should().Be("Página 1 de 1 · 7 registros"));
        tela.Markup.Should().NotContain("Carregando…");
    }
}
```

(Sem a barra superior — o teste desenha a página sem o `ComBarraSuperior` —, o segmentado que o teste clica é o do cabeçalho do desktop e do tablet, que o `CabecalhoDePagina` desenha no próprio conteúdo.)

Em `tests/AtividadesJuridicas.IntegrationTests/AuditoriaWebTests.cs` (CRLF), duas asserções mudam, porque o painel passa a ficar no HTML com `hidden`:

| Hoje | Passa a ser | O que prova |
|---|---|---|
| `html.Should().NotContain("aj-painel--filtros").And.NotContain("Identificador inválido.");` | o form escondido no bloco `aj-tablet-mobile`, com o Usuário primeiro; e o título com nenhum "Identificador inválido." | o painel começa fechado (agora escondido, para o `aria-controls` valer) |
| `invalido…Contain("id=\"filtros-da-auditoria\"")` e `valido…NotContain("id=\"filtros-da-auditoria\"")` | o form sem `hidden` no inválido (o `(?!…hidden)` na abertura do form), e com `hidden` no válido | com o identificador inválido o painel fica aberto; aplicado, fecha |

No teste `Diretor_ve_o_controle_segmentado_e_os_filtros_de_cada_visao`, troque

```csharp
        // O painel de filtros só aparece aberto.
        html.Should().NotContain("aj-painel--filtros").And.NotContain("Identificador inválido.");
```

por

```csharp
        // O painel de filtros fica no HTML, escondido, para o aria-controls dos dois botões apontar para um form que existe.
        linha.Should().MatchRegex(
            "<form id=\"filtros-da-auditoria\" class=\"aj-painel aj-painel--filtros\" hidden[^>]*>\\s*" +
            "<label class=\"aj-campo-filtro\">\\s*<span>Usuário</span>");
        html.Should().Contain("<h1 class=\"aj-titulo\">Auditoria</h1>").And.NotContain("Identificador inválido.");
```

No teste `Painel_de_filtros_fica_aberto_com_identificador_invalido_e_fecha_quando_o_filtro_se_aplica`, troque

```csharp
        // Com o identificador inválido o campo a corrigir continua à vista, junto do aviso.
        invalido.Should().Contain("Identificador inválido.").And.Contain("id=\"filtros-da-auditoria\"")
            .And.MatchRegex("(?s)<form id=\"filtros-da-auditoria\".*?placeholder=\"Identificador de correlação\"");
        valido.Should().Contain("Nenhum registro.").And.NotContain("id=\"filtros-da-auditoria\"");
```

por

```csharp
        // Com o identificador inválido o painel continua aberto (sem hidden), com o campo a corrigir à vista, junto do aviso.
        invalido.Should().Contain("Identificador inválido.")
            .And.MatchRegex("(?s)<form id=\"filtros-da-auditoria\" class=\"aj-painel aj-painel--filtros\"(?![^>]*\\bhidden\\b)[^>]*>.*?placeholder=\"Identificador de correlação\"");
        // Aplicado, ele fecha: continua no HTML, escondido.
        valido.Should().Contain("Nenhum registro.")
            .And.MatchRegex("<form id=\"filtros-da-auditoria\" class=\"aj-painel aj-painel--filtros\" hidden[^>]*>");
```

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AuditoriaInterativaTests"`
Expected: falham todos os sete — os quatro do painel (o form fechado não está no HTML, e ninguém pede foco), `Periodo_trocado_na_linha_…` (a página 2 consulta com o "De" não aplicado), `Falha_ao_carregar_os_usuarios_…` (o "Tentar de novo" só consulta os registros: `ListarUsuarios` uma vez) e `Enquanto_a_aba_nova_carrega_…` (a paginação continua à vista durante a carga, com os números de `_acessos` antes da resposta).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AuditoriaWebTests"`
Expected: falham `Diretor_ve_o_controle_segmentado_e_os_filtros_de_cada_visao` e `Painel_de_filtros_fica_aberto_com_identificador_invalido_e_fecha_quando_o_filtro_se_aplica` (o form não fica no HTML fechado); os outros passam.

- [ ] **Step 9: Cópia e volta dos campos do painel no filtro da auditoria (teste antes)**

Em `tests/AtividadesJuridicas.IntegrationTests/TelasPurasTests.cs` (CRLF), no fim da classe `FiltroDaTelaDeAuditoriaTests`:

```csharp
    [Fact]
    public void A_copia_tem_os_mesmos_valores_e_nao_muda_com_o_original()
    {
        var atual = Cheio();

        var copia = atual.Copia();

        copia.Should().NotBeSameAs(atual).And.BeEquivalentTo(atual);
        atual.Entidade = "Perfil";
        atual.UsuarioId = null;
        copia.Entidade.Should().Be("Usuario");
        copia.UsuarioId.Should().NotBeNull();
    }

    [Fact]
    public void Voltar_os_do_painel_traz_do_aplicado_tudo_menos_o_periodo()
    {
        var aplicado = Cheio();
        var campos = new FiltroDaTelaDeAuditoria
        {
            De = new DateOnly(2026, 2, 1),
            Ate = new DateOnly(2026, 2, 28),
            Entidade = "Perfil",
            Acao = AcaoDeAcesso.Baixou,
        };

        campos.VoltarOsDoPainel(aplicado);

        // O período fica fora do painel (na linha): o que está nos campos não muda.
        campos.De.Should().Be(new DateOnly(2026, 2, 1));
        campos.Ate.Should().Be(new DateOnly(2026, 2, 28));
        campos.Should().BeEquivalentTo(aplicado, o => o.Excluding(f => f.De).Excluding(f => f.Ate));
    }
```

Run: `dotnet build -m:1`
Expected: o build falha com `CS1061` (`FiltroDaTelaDeAuditoria` não tem `Copia` nem `VoltarOsDoPainel`).

Em `src/AtividadesJuridicas.Web/Infra/FiltroDaTelaDeAuditoria.cs` (CRLF), depois da propriedade `Acao`:

```csharp

    /// <summary>Uma cópia dos campos: o filtro aplicado não muda com o que se digita depois.</summary>
    public FiltroDaTelaDeAuditoria Copia() => (FiltroDaTelaDeAuditoria)MemberwiseClone();

    /// <summary>
    /// Fechar o painel sem filtrar: os campos do painel (usuário, operação, entidade, registro, gravação, ação e recurso)
    /// voltam ao filtro aplicado. O período, que fica fora do painel, não muda.
    /// </summary>
    public void VoltarOsDoPainel(FiltroDaTelaDeAuditoria aplicado)
    {
        ArgumentNullException.ThrowIfNull(aplicado);
        UsuarioId = aplicado.UsuarioId;
        Entidade = aplicado.Entidade;
        EntidadeId = aplicado.EntidadeId;
        IdCorrelacao = aplicado.IdCorrelacao;
        Operacao = aplicado.Operacao;
        TipoRecurso = aplicado.TipoRecurso;
        Acao = aplicado.Acao;
    }
```

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~FiltroDaTelaDeAuditoriaTests"`
Expected: todos passando (os três de antes e os dois novos).

- [ ] **Step 10: Auditoria com o painel compartilhado, o filtro aplicado, C3 e C4**

Em `src/AtividadesJuridicas.Web/Components/Pages/Auditoria.razor` (CRLF), troque o painel (dentro do `<div class="aj-tablet-mobile">`, depois do `</form>` da linha)

```razor
        @if (_filtrosAbertos)
        {
            <form id="filtros-da-auditoria" class="aj-painel aj-painel--filtros" @onsubmit="FiltrarDoPainelAsync" @onsubmit:preventDefault>
                <label class="aj-campo-filtro"><span>Usuário</span><select class="aj-campo-filtro__controle" @bind="_filtro.UsuarioId"><option value="">Todos</option>@foreach (var usuario in _usuarios) { <option value="@usuario.Id">@usuario.Nome</option> }</select></label>
                @if (_aba == Aba.Alteracoes)
                {
                    <label class="aj-campo-filtro"><span>Operação</span><select class="aj-campo-filtro__controle" @bind="_filtro.Operacao"><option value="">Todas</option><option value="@OperacaoAuditada.Criacao">Criação</option><option value="@OperacaoAuditada.Alteracao">Alteração</option><option value="@OperacaoAuditada.Exclusao">Exclusão</option><option value="@OperacaoAuditada.Evento">Evento</option></select></label>
                    <label class="aj-campo-filtro"><span>Entidade</span><input type="text" class="aj-campo-filtro__controle" placeholder="Usuario, Perfil…" @bind="_filtro.Entidade" /></label>
                    <label class="aj-campo-filtro"><span>Registro</span><input type="text" class="aj-campo-filtro__controle" placeholder="Identificador do registro" @bind="_filtro.EntidadeId" /></label>
                    <label class="aj-campo-filtro"><span>Gravação</span><input type="text" class="aj-campo-filtro__controle" placeholder="Identificador de correlação" @bind="_filtro.IdCorrelacao" /></label>
                }
                else
                {
                    <label class="aj-campo-filtro"><span>Ação</span><select class="aj-campo-filtro__controle" @bind="_filtro.Acao"><option value="">Todas</option><option value="@AcaoDeAcesso.Listou">Listou</option><option value="@AcaoDeAcesso.Visualizou">Visualizou</option><option value="@AcaoDeAcesso.Baixou">Baixou</option></select></label>
                    <label class="aj-campo-filtro"><span>Recurso</span><input type="text" class="aj-campo-filtro__controle" placeholder="Usuario, RegistroAuditoria…" @bind="_filtro.TipoRecurso" /></label>
                }
                <div class="aj-painel__acoes">
                    <button type="button" class="aj-botao aj-botao--fantasma" @onclick="LimparFiltros">Limpar</button>
                    <button type="submit" class="aj-botao aj-botao--primario">Filtrar</button>
                </div>
            </form>
        }
```

por

```razor
        @* Fechado, o painel fica no HTML, escondido, para o aria-controls dos dois botões apontar para ele. *@
        <PainelDeFiltros Id="filtros-da-auditoria" Aberto="_filtrosAbertos" AoFiltrar="FiltrarDoPainelAsync" AoLimpar="LimparFiltros" AoFechar="FecharFiltros">
            <label class="aj-campo-filtro"><span>Usuário</span><select class="aj-campo-filtro__controle" @bind="_filtro.UsuarioId"><option value="">Todos</option>@foreach (var usuario in _usuarios) { <option value="@usuario.Id">@usuario.Nome</option> }</select></label>
            @if (_aba == Aba.Alteracoes)
            {
                <label class="aj-campo-filtro"><span>Operação</span><select class="aj-campo-filtro__controle" @bind="_filtro.Operacao"><option value="">Todas</option><option value="@OperacaoAuditada.Criacao">Criação</option><option value="@OperacaoAuditada.Alteracao">Alteração</option><option value="@OperacaoAuditada.Exclusao">Exclusão</option><option value="@OperacaoAuditada.Evento">Evento</option></select></label>
                <label class="aj-campo-filtro"><span>Entidade</span><input type="text" class="aj-campo-filtro__controle" placeholder="Usuario, Perfil…" @bind="_filtro.Entidade" /></label>
                <label class="aj-campo-filtro"><span>Registro</span><input type="text" class="aj-campo-filtro__controle" placeholder="Identificador do registro" @bind="_filtro.EntidadeId" /></label>
                <label class="aj-campo-filtro"><span>Gravação</span><input type="text" class="aj-campo-filtro__controle" placeholder="Identificador de correlação" @bind="_filtro.IdCorrelacao" /></label>
            }
            else
            {
                <label class="aj-campo-filtro"><span>Ação</span><select class="aj-campo-filtro__controle" @bind="_filtro.Acao"><option value="">Todas</option><option value="@AcaoDeAcesso.Listou">Listou</option><option value="@AcaoDeAcesso.Visualizou">Visualizou</option><option value="@AcaoDeAcesso.Baixou">Baixou</option></select></label>
                <label class="aj-campo-filtro"><span>Recurso</span><input type="text" class="aj-campo-filtro__controle" placeholder="Usuario, RegistroAuditoria…" @bind="_filtro.TipoRecurso" /></label>
            }
        </PainelDeFiltros>
```

(o `<div class="aj-tablet-mobile">` e a linha de filtros de cima ficam; os campos, os textos e as classes não mudam).

Troque a paginação do fim da marcação

```razor
    <Paginacao Numero="_pagina" TotalDePaginas="TotalDePaginas" Total="Total" Singular="registro" Plural="registros"
               SempreVisivel="true" Ocupado="_ocupado" IrPara="IrParaAsync" />
```

por

```razor
    @* A paginação é da consulta respondida: enquanto a aba nova carrega, ou depois de uma falha, ela sai, para não
       mostrar os números de outra consulta. *@
    @if (_consultou)
    {
        <Paginacao Numero="_pagina" TotalDePaginas="TotalDePaginas" Total="Total" Singular="registro" Plural="registros"
                   SempreVisivel="true" Ocupado="_ocupado" IrPara="IrParaAsync" />
    }
```

No `@code`:

1. Troque `private bool _consultou;` por

```csharp
    private bool _consultou;
    private bool _usuariosCarregados;
```

e troque `private FiltroDaTelaDeAuditoria _filtro = new();` por

```csharp
    // O filtro em edição (os campos das três visões) e o aplicado, do último "Filtrar" ou do botão de uma linha: o que a
    // lista está mostrando. A consulta só lê o aplicado.
    private FiltroDaTelaDeAuditoria _filtro = new();
    private FiltroDaTelaDeAuditoria _aplicado = new();
```

2. Troque o `OnInitializedAsync`

```csharp
    protected override async Task OnInitializedAsync()
    {
        _autorizado = await Despachante.EnviarAsync(new ObterMeuAcesso()) is { EhDiretoria: true };
        if (_autorizado)
        {
            var usuarios = await Despachante.EnviarAsync(new ListarUsuarios());
            _usuarios = usuarios.Valor ?? [];
            if (usuarios.Sucesso)
                await IrParaAsync(1);
            else
                Concluir(usuarios);
        }
        _carregado = true;
    }
```

por

```csharp
    protected override async Task OnInitializedAsync()
    {
        _autorizado = await Despachante.EnviarAsync(new ObterMeuAcesso()) is { EhDiretoria: true };
        if (_autorizado) await CarregarInicioAsync();
        _carregado = true;
    }

    // A carga inicial: os usuários do filtro e a primeira página. Se os usuários falham, o erro fica no lugar da lista e
    // o "Tentar de novo" volta aqui, até eles chegarem. Uma vez por clique.
    private async Task CarregarInicioAsync()
    {
        if (_ocupado) return;

        Resultado<IReadOnlyList<UsuarioResumo>> usuarios;
        _ocupado = true;
        try
        {
            usuarios = await Despachante.EnviarAsync(new ListarUsuarios());
        }
        finally
        {
            _ocupado = false;
        }

        _usuarios = usuarios.Valor ?? [];
        if (!usuarios.Sucesso)
        {
            Concluir(usuarios);
            return;
        }

        _usuariosCarregados = true;
        await IrParaAsync(1);
    }
```

3. Troque de `private Task FiltrarAsync() => IrParaAsync(1);` até o fim do `FiltrarPorCorrelacaoAsync` — isto é, o `FiltrarAsync`, o `TentarDeNovoAsync`, o `AlternarFiltros`, o `LimparFiltros` com o comentário, o `FiltrarDoPainelAsync` e os dois `FiltrarPor…Async` com o comentário — por:

```csharp
    // "Filtrar" (a barra do desktop, a linha do tablet e do mobile, o painel) é o único lugar que grava o filtro
    // aplicado: tudo o que está nos campos. Com outra consulta em curso, nada muda.
    private Task<Carga> AplicarAsync()
    {
        if (_ocupado) return Task.FromResult(Carga.EmAndamento);

        _aplicado = _filtro.Copia();
        return ConsultarPaginaAsync(1);
    }

    private Task FiltrarAsync() => AplicarAsync();

    // Se a carga inicial falhou nos usuários, ela é refeita; senão, a página pedida de novo, com o filtro aplicado.
    private Task TentarDeNovoAsync() => _usuariosCarregados ? IrParaAsync(_pagina) : CarregarInicioAsync();

    // Abrir ou fechar o painel pelo botão descarta o que se escolheu nele e não se aplicou: os campos do painel voltam ao
    // filtro aplicado. O período, que fica na linha, não muda.
    private void AlternarFiltros()
    {
        _filtrosAbertos = !_filtrosAbertos;
        _filtro.VoltarOsDoPainel(_aplicado);
    }

    // Esc no painel: fecha como o botão.
    private void FecharFiltros()
    {
        _filtrosAbertos = false;
        _filtro.VoltarOsDoPainel(_aplicado);
    }

    // Esvazia os campos que o painel da aba atual traz; o período, que fica na linha, não sai. Só vale ao filtrar.
    private void LimparFiltros()
    {
        _filtro.UsuarioId = null;
        if (_aba == Aba.Alteracoes)
        {
            _filtro.Operacao = null;
            _filtro.Entidade = null;
            _filtro.EntidadeId = null;
            _filtro.IdCorrelacao = null;
        }
        else
        {
            _filtro.Acao = null;
            _filtro.TipoRecurso = null;
        }
    }

    private async Task FiltrarDoPainelAsync()
    {
        // O painel só fecha quando o filtro foi aplicado: um identificador inválido ou uma consulta ignorada o deixam aberto.
        if (await AplicarAsync() == Carga.Feita) _filtrosAbertos = false;
    }

    // Os botões das linhas respondem "tudo sobre isto": aplicam na hora o período aplicado e o registro (os outros
    // filtros saem), e os campos passam a mostrá-lo.
    private Task FiltrarPorRegistroAsync(Guid entidadeId)
    {
        if (_ocupado) return Task.CompletedTask;

        _aplicado = FiltroDaTelaDeAuditoria.PorRegistro(_aplicado, entidadeId);
        _filtro = _aplicado.Copia();
        return IrParaAsync(1);
    }

    private Task FiltrarPorCorrelacaoAsync(Guid idCorrelacao)
    {
        if (_ocupado) return Task.CompletedTask;

        _aplicado = FiltroDaTelaDeAuditoria.PorCorrelacao(_aplicado, idCorrelacao);
        _filtro = _aplicado.Copia();
        return IrParaAsync(1);
    }
```

4. A consulta lê o aplicado. No `CarregarAsync`, troque

```csharp
            if (!TentarLerGuid(_filtro.EntidadeId, out entidadeId) || !TentarLerGuid(_filtro.IdCorrelacao, out idCorrelacao))
```

por

```csharp
            if (!TentarLerGuid(_aplicado.EntidadeId, out entidadeId) || !TentarLerGuid(_aplicado.IdCorrelacao, out idCorrelacao))
```

e no `ConsultarAsync` troque todas as leituras de `_filtro.` por `_aplicado.` (as sete: `De`, `Ate`, `UsuarioId` duas vezes, `Entidade`, `Operacao`, `TipoRecurso`, `Acao` — confira com a ferramenta Grep que, no `@code`, `_filtro.` só sobra no `LimparFiltros`, no `AlternarFiltros`, no `FecharFiltros` e no `AplicarAsync`, e na marcação só nos `@bind`).

Os nomes `FiltrarAsync`, `AlternarFiltros`, `FiltrarDoPainelAsync` e o campo `_filtro` (do tipo `FiltroDaTelaDeAuditoria`) ficam: os testes de `AuditoriaWebTests` os chamam pelo nome.

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AuditoriaInterativaTests"`
Expected: todos os sete passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AuditoriaWebTests"`
Expected: todos passando (o `Identificador_invalido_mostra_o_aviso_…` digita no `_filtro` e chama o `FiltrarAsync`, que aplica; o `Falha_na_consulta_…` continua com o erro e o "Tentar de novo").

Fica para o navegador (Tarefa 11): o foco chegar ao Usuário e voltar ao botão visível (o "Usuário, operação"/"Usuário, ação" no tablet, o "Mais filtros" no mobile); o Enter nos campos de data da linha continuar enviando pelo botão escondido.

- [ ] **Step 11: As três telas juntas e a trava**

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AgendaInterativaTests|FullyQualifiedName~AtividadesInterativosTests|FullyQualifiedName~AuditoriaInterativaTests"`
Expected: todos passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AgendaWebTests|FullyQualifiedName~AtividadesWebTests|FullyQualifiedName~AuditoriaWebTests|FullyQualifiedName~TelasPurasTests|FullyQualifiedName~LayoutWebTests"`
Expected: todos passando.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Expected: todos passando.

Confira com a ferramenta Grep, em `src/AtividadesJuridicas.Web/Components/Pages`, que `aj-painel--filtros` não aparece mais (só no `Layout/PainelDeFiltros.razor`), e que `_tipoDoPainel` e `_focarPainel` não existem mais em `Agenda.razor`.

- [ ] **Step 12: Commit da Auditoria**

Confira o branch (`git branch --show-current` deve dizer `worktree-feat+fase-9g-limpeza`) e:

```
git add src/AtividadesJuridicas.Web/Components/Pages/Auditoria.razor src/AtividadesJuridicas.Web/Infra/FiltroDaTelaDeAuditoria.cs tests/AtividadesJuridicas.IntegrationTests/AuditoriaInterativaTests.cs tests/AtividadesJuridicas.IntegrationTests/AuditoriaWebTests.cs tests/AtividadesJuridicas.IntegrationTests/TelasPurasTests.cs
git commit -F <arquivo com a mensagem, UTF-8 sem BOM>
```

Mensagem:

```
feat(web): auditoria com o painel de filtros compartilhado, o filtro aplicado, o "Tentar de novo" que refaz a carga inicial e a paginação só da consulta respondida

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

### Task 8: Documentos — foco ao fechar, envio e confirmação, comportamentos mantidos

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Documentos/GestaoDeDocumentos.cs` e as três herdeiras (`DocumentosComunsDaDiretoria.razor`, `DocumentosDaFicha.razor`, `Atividades/DocumentosDaAtividade.razor`)
- Test: `tests/AtividadesJuridicas.IntegrationTests/DocumentosComunsInterativosTests.cs`, `DocumentosDaFichaInterativosTests.cs`, `DocumentosDaAtividadeInterativosTests.cs`, `DocumentosDaFichaTests.cs`

**Interfaces:**
- Consumes: o serviço `Foco` e `Aviso.Revelar` (Tarefa 3).

**Regras:** B6 (Cancelar devolve o foco a quem abriu; concluir leva o foco ao aviso de resultado, com `Revelar`), B7 (com a confirmação de envio à vista, "Enviar documento" leva o foco à confirmação), B10 (a: "Salvar nome" com guarda de clique duplo; b: resposta atrasada da consulta descartada por geração; c: "Tentar de novo" com estado de carregando; d: Enter num campo de texto dos painéis envia — cada painel é um `form` com o seu botão de envio, lição da 9F; e: durante o envio, a confirmação continua à vista e o campo de arquivo fica desabilitado), E4 (o acesso encerrado limpa a mensagem anterior).

**Testes obrigatórios:** bUnit para cada subitem nas três herdeiras onde a base for exercitada por uma só (diga qual e por quê) e ao menos um teste por herdeira para o foco ao fechar.

- [ ] **Step 1: Testes dos Documentos comuns (devem falhar)**

Os testes desta tarefa provam o foco **pedido** de dois jeitos, com os auxiliares que a Tarefa 3 deixou na `TelaInterativa` (não crie outros, nem monte o módulo de foco com `SetupModule`): o pedido ao módulo de foco, pelo serviço `Foco` (`UltimoSeletorFocado()`, o seletor do último pedido a `focarPrimeiroVisivel`, e `SeletoresFocados`, todos os seletores pedidos, na ordem), e o pedido por `ElementReference.FocusAsync`, que o `Aviso` com `Revelar` faz a si mesmo (`UltimoFocoPedido()`, a última chamada, e `FocosDeElementoPedidos`, quantas houve).

Os Documentos comuns da Diretoria exercitam a base inteira (`GestaoDeDocumentos`): é a única herdeira com a confirmação de envio (B7, B10e) e com o "Enviar documento" em duas cópias (cabeçalho e botão flutuante). Por isso B7, B10a, B10e (a confirmação à vista) e E4 são provados aqui, uma vez: a lógica é da base, e as outras duas herdeiras só ligam a marcação (o que elas ligam é provado nos Steps 2 e 3).

Primeiro reescreva, em `tests/AtividadesJuridicas.IntegrationTests/DocumentosComunsInterativosTests.cs`, os seletores que apontam para os painéis de envio e de renomear como `section.aj-painel`: os painéis passam a ser `form.aj-painel` (B10d). A lista continua em `section.aj-painel.aj-painel--principal`. Cada asserção continua provando o mesmo; liste no relatório as linhas reescritas:

| Linha | Hoje | Depois |
|---|---|---|
| 46, 106 | `tela.Find("section.aj-painel h2")` | `tela.Find("form.aj-painel h2")` |
| 57, 63, 67, 77, 116, 119, 149, 164 | `"section.aj-painel button"` | `"form.aj-painel button"` |
| 61, 66 | `tela.Find("section.aj-painel")` | `tela.Find("form.aj-painel")` |
| 84, 100, 134, 154, 167 | `"section.aj-painel:not(.aj-painel--principal)"` | `"form.aj-painel"` |
| 95 | `"section.aj-painel h2"` | `"form.aj-painel h2"` |
| 97, 148 | `"section.aj-painel input.aj-campo"` | `"form.aj-painel input.aj-campo"` |

Depois acrescente os testes novos ao fim da classe (os `using` do arquivo já cobrem tudo, salvo `AtividadesJuridicas.Application.Identidade`, que não é preciso aqui):

```csharp
    // O "Enviar documento" desta tela: o do cabeçalho (desktop e tablet) ou o flutuante (mobile).
    private const string EnviarDocumento = ".aj-cabecalho > .aj-botao--primario, .aj-fab[aria-label=\"Enviar documento\"]";

    // O botão de uma ação da linha que abriu o que foi cancelado: o da ação no desktop ou o "⋯" da linha no tablet e no mobile.
    private static string DesktopOuMenu(string acao, Guid documento) =>
        $"#documento-{acao}-{documento:N}, #documento-{documento:N} .aj-menu-acoes > .aj-botao";

    private static AngleSharp.Dom.IElement AvisoCom(IRenderedComponent<DocumentosComunsDaDiretoria> tela, string texto) =>
        tela.FindAll(".aj-aviso").Single(a => a.TextContent.Contains(texto));

    [Fact]
    public void Os_botoes_que_abrem_tem_o_id_que_o_foco_de_volta_procura()
    {
        var tela = Tela();

        tela.Find($"#documento-renomear-{Contrato:N}").TextContent.Should().Be("Renomear");
        tela.Find($"#documento-excluir-{Contrato:N}").TextContent.Should().Be("Excluir");
        tela.Find($"#documento-{Contrato:N} .aj-menu-acoes > .aj-botao").GetAttribute("aria-label")
            .Should().Be("Ações de Modelo de contrato.pdf");
        tela.Find(".aj-cabecalho > .aj-botao--primario").TextContent.Should().Contain("Enviar documento");
    }

    [Fact]
    public void Cancelar_o_painel_de_envio_devolve_o_foco_ao_enviar_documento()
    {
        var tela = Tela();
        AbrirEnvio(tela);

        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Cancelar").Click();

        tela.FindAll("form.aj-painel").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be(EnviarDocumento);
    }

    [Fact]
    public void Cancelar_a_confirmacao_de_envio_devolve_o_foco_ao_enviar_do_painel()
    {
        var tela = Tela();
        AbrirEnvio(tela);
        Escolher(tela);
        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Enviar").Click();

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Cancelar").Click();

        tela.Find("form#documentos-envio").HasAttribute("hidden").Should().BeFalse();
        tela.Find("#documentos-envio .aj-botao--primario").TextContent.Should().Be("Enviar");
        UltimoSeletorFocado().Should().Be("#documentos-envio .aj-botao--primario");
    }

    [Fact]
    public void Cancelar_o_renomear_devolve_o_foco_ao_renomear_da_linha()
    {
        var tela = Tela();
        ClicarNaLinha(tela, "Renomear");

        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Cancelar").Click();

        tela.FindAll("form.aj-painel").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be(DesktopOuMenu("renomear", Contrato));
    }

    [Fact]
    public void Cancelar_a_exclusao_devolve_o_foco_ao_excluir_da_linha()
    {
        var tela = Tela();
        ClicarNaLinha(tela, "Excluir", linha: 1);

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Cancelar").Click();

        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be(DesktopOuMenu("excluir", Modelo));
    }

    [Fact]
    public void Salvar_nome_leva_o_foco_ao_aviso_de_resultado()
    {
        Despachante.Responde<RenomearDocumento, Resultado>(Resultado.Ok());
        var tela = Tela();
        ClicarNaLinha(tela, "Renomear");

        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Salvar nome").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Nome alterado."));
        var aviso = AvisoCom(tela, "Nome alterado.");
        aviso.GetAttribute("tabindex").Should().Be("-1");
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }

    [Fact]
    public void Confirmar_a_exclusao_leva_o_foco_ao_aviso_de_resultado()
    {
        Despachante.Responde<ExcluirDocumento, Resultado>(Resultado.Ok());
        var tela = Tela();
        ClicarNaLinha(tela, "Excluir");

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar exclusão").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Documento excluído."));
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(AvisoCom(tela, "Documento excluído."));
    }

    [Fact]
    public void Confirmar_o_envio_leva_o_foco_ao_aviso_de_resultado()
    {
        Despachante.Responde<EnviarDocumentoComum, Resultado<Guid>>(Resultado<Guid>.Ok(Guid.NewGuid()));
        var tela = Tela();
        AbrirEnvio(tela);
        Escolher(tela);
        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Enviar").Click();

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar envio").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Documento enviado."));
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(AvisoCom(tela, "Documento enviado."));
    }

    [Fact]
    public void Dois_avisos_seguidos_iguais_revelam_cada_um()
    {
        // O arquivo grande demais é recusado na hora, sem consulta: a mesma frase duas vezes, sem desenho no meio.
        var tela = Tela();
        AbrirEnvio(tela);
        tela.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(new byte[51 * 1024 * 1024], "grande.pdf"));

        tela.Find("form#documentos-envio").Submit();
        var antes = FocosDeElementoPedidos;
        tela.Find("form#documentos-envio").Submit();

        AvisoCom(tela, "O arquivo passa do limite de 50 MB.").GetAttribute("tabindex").Should().Be("-1");
        FocosDeElementoPedidos.Should().Be(antes + 1);
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(AvisoCom(tela, "O arquivo passa do limite de 50 MB."));
    }

    [Fact]
    public void Com_a_confirmacao_a_vista_enviar_documento_leva_o_foco_a_ela()
    {
        var tela = Tela();
        AbrirEnvio(tela);
        Escolher(tela);
        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Enviar").Click();
        var antes = SeletoresFocados.Count;

        AbrirEnvio(tela);

        tela.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar envio");
        tela.Find("form#documentos-envio").HasAttribute("hidden").Should().BeTrue();
        SeletoresFocados.Count.Should().Be(antes + 1);
        UltimoSeletorFocado().Should().Be("[role=\"alertdialog\"][aria-label=\"Confirmar envio\"]");
    }

    [Fact]
    public void Salvar_nome_duas_vezes_seguidas_renomeia_uma_vez_so()
    {
        var renomeio = new TaskCompletionSource<Resultado>();
        Despachante.RespondeDepois<RenomearDocumento, Resultado>(renomeio.Task);
        var tela = Tela();
        ClicarNaLinha(tela, "Renomear");

        tela.Find("form#documentos-renomear").Submit();
        tela.Find("form#documentos-renomear").Submit();   // o segundo Enter, com a primeira gravação em curso

        Despachante.Recebidas.OfType<RenomearDocumento>().Should().ContainSingle();
        tela.FindAll("form#documentos-renomear button").Single(b => b.TextContent == "Salvar nome")
            .HasAttribute("disabled").Should().BeTrue();

        renomeio.SetResult(Resultado.Ok());
        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Nome alterado."));
        Despachante.Recebidas.OfType<RenomearDocumento>().Should().ContainSingle();
    }

    [Fact]
    public void Tentar_de_novo_mostra_carregando_ate_a_resposta()
    {
        Despachante.Responde<ListarDocumentosComuns, Resultado<Pagina<DocumentoDoPortal>>>(
            Resultado<Pagina<DocumentoDoPortal>>.Falha(TipoErro.Validacao, "Falha de teste."));
        var tela = Desenhar<DocumentosComunsDaDiretoria>();
        var nova = new TaskCompletionSource<Resultado<Pagina<DocumentoDoPortal>>>();
        Despachante.RespondeDepois<ListarDocumentosComuns, Resultado<Pagina<DocumentoDoPortal>>>(nova.Task);

        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.Markup.Should().Contain("Carregando…").And.NotContain("Falha de teste.");
        tela.FindAll("button").Should().NotContain(b => b.TextContent == "Tentar de novo");

        nova.SetResult(Lista());
        tela.WaitForAssertion(() => tela.FindAll("table.aj-tabela tbody tr").Should().HaveCount(2));
        tela.Markup.Should().NotContain("Carregando…");
        Despachante.Recebidas.OfType<ListarDocumentosComuns>().Should().HaveCount(2);
    }

    [Fact]
    public void Cada_painel_e_um_formulario_que_o_Enter_envia()
    {
        Despachante.Responde<RenomearDocumento, Resultado>(Resultado.Ok());
        var tela = Tela();
        AbrirEnvio(tela);
        tela.Find("form#documentos-envio").QuerySelectorAll("button").Select(b => (b.TextContent, b.GetAttribute("type")))
            .Should().Equal(("Cancelar", "button"), ("Enviar", "submit"));
        Escolher(tela);

        tela.Find("form#documentos-envio").Submit();   // o Enter no campo do nome

        tela.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar envio");
        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Cancelar").Click();

        ClicarNaLinha(tela, "Renomear");
        tela.Find("form#documentos-renomear").QuerySelectorAll("button").Select(b => (b.TextContent, b.GetAttribute("type")))
            .Should().Equal(("Cancelar", "button"), ("Salvar nome", "submit"));
        tela.Find("form#documentos-renomear input.aj-campo").Change("Contrato padrão.pdf");

        tela.Find("form#documentos-renomear").Submit();   // o Enter no campo do novo nome

        tela.WaitForAssertion(() => Despachante.Recebidas.OfType<RenomearDocumento>().Should().ContainSingle()
            .Which.Should().Be(new RenomearDocumento(Contrato, "Contrato padrão.pdf")));
    }

    [Fact]
    public void Durante_o_envio_confirmado_a_confirmacao_fica_a_vista_e_o_campo_de_arquivo_desabilitado()
    {
        var envio = new TaskCompletionSource<Resultado<Guid>>();
        Despachante.RespondeDepois<EnviarDocumentoComum, Resultado<Guid>>(envio.Task);
        var tela = Tela();
        AbrirEnvio(tela);
        Escolher(tela);
        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Enviar").Click();

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar envio").Click();

        var confirmacao = tela.Find("[role=alertdialog]");
        confirmacao.GetAttribute("aria-label").Should().Be("Confirmar envio");
        confirmacao.QuerySelectorAll("button").Should().HaveCount(2).And.OnlyContain(b => b.HasAttribute("disabled"));
        tela.Find("form#documentos-envio").HasAttribute("hidden").Should().BeTrue();
        tela.Find("input[type=file]").HasAttribute("disabled").Should().BeTrue();

        envio.SetResult(Resultado<Guid>.Ok(Guid.NewGuid()));
        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Documento enviado."));
        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
    }

    [Fact]
    public void Acesso_encerrado_depois_de_salvar_o_nome_nao_deixa_aviso_ao_lado()
    {
        var encerrado = false;
        Despachante.Responde<ListarDocumentosComuns, Resultado<Pagina<DocumentoDoPortal>>>(Lista());
        var tela = Desenhar<DocumentosComunsDaDiretoria>(p => p.Add(d => d.OnAcessoEncerrado, () => encerrado = true));
        // O nome é salvo, e a recarga da lista que vem depois responde acesso negado.
        Despachante
            .Responde<RenomearDocumento, Resultado>(Resultado.Ok())
            .Responde<ListarDocumentosComuns, Resultado<Pagina<DocumentoDoPortal>>>(
                Resultado<Pagina<DocumentoDoPortal>>.Falha(TipoErro.AcessoNegado, "Acesso negado."));
        ClicarNaLinha(tela, "Renomear");

        tela.FindAll("form.aj-painel button").Single(b => b.TextContent == "Salvar nome").Click();

        tela.WaitForAssertion(() => encerrado.Should().BeTrue());
        tela.Markup.Should().Contain("Nenhum documento.").And.NotContain("Nome alterado.");
        tela.FindAll(".aj-aviso").Should().BeEmpty();
    }
```

Notas sobre os testes:
- `Dois_avisos_seguidos_iguais_revelam_cada_um` é a regra do `Revelar` da Tarefa 3: o `Aviso` só se revela ao aparecer; a mesma frase duas vezes, sem desenho no meio, só se revela de novo porque a chave do aviso muda a cada mensagem (`@key="_versaoDoAviso"`). O limite de 50 MB vem de `DocumentosOpcoes.TamanhoMaximoBytes` (o texto "Arquivo (até 50 MB)" dos testes de hoje); o `InputFileContent.CreateFromBinary` só declara o tamanho, o arquivo não é lido (a recusa vem antes). Se o bUnit recusar um conteúdo desse tamanho, use o menor `byte[]` acima do limite que ele aceitar e registre no relatório.
- Os testes de foco provam o **pedido**: o seletor certo ao módulo de foco, ou o `FocusAsync` no aviso certo. Que o elemento visível de fato recebe o foco (o "⋯" no tablet e no mobile, o botão do desktop, o flutuante do mobile), e que o Enter envia de fato nos navegadores, fica para a Tarefa 11.

Run: `dotnet build -m:1`
Expected: build sem erro (os testes compilam: só usam a marcação e os auxiliares de foco da `TelaInterativa`, da Tarefa 3).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DocumentosComunsInterativosTests"`
Expected: FAIL — os testes reescritos e os novos não acham `form.aj-painel`, `form#documentos-envio` nem os ids; nenhum seletor é pedido ao módulo de foco.

- [ ] **Step 2: Testes dos documentos da ficha do cliente (devem falhar)**

A ficha do cliente é a herdeira com o painel de acesso e com a troca de área (chips): o descarte da resposta atrasada (B10b) é provado aqui, porque a troca de chip com a consulta anterior ainda em curso é o caso real da corrida; a lógica é da base, as outras duas herdeiras usam a mesma `CarregarAsync`.

Em `tests/AtividadesJuridicas.IntegrationTests/DocumentosDaFichaInterativosTests.cs`, reescreva as linhas 80 e 83: `"section.aj-painel button"` → `"form.aj-painel button"` (o painel de envio passa a `form`; a asserção prova o mesmo).

Em `tests/AtividadesJuridicas.IntegrationTests/DocumentosDaFichaTests.cs`, reescreva a linha 66, que fixa a marcação da linha do tablet e do mobile: `<div class=\"aj-linha\">` → `<div class=\"aj-linha\" id=\"documento-{geral.Valor:N}\">` (a linha ganha o `id` do foco de volta; a asserção fica mais forte). Liste as três linhas no relatório.

Acrescente ao fim da classe `DocumentosDaFichaInterativosTests`:

```csharp
    private void CombinarAcesso() => Despachante
        .Responde<ObterAcessoDoDocumento, Resultado<AcessoDoDocumento>>(Resultado<AcessoDoDocumento>.Ok(
            new AcessoDoDocumento(Procuracao, "Procuração.pdf", CategoriaDocumento.DocumentoGeral, [], [])))
        .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(
            Resultado<IReadOnlyList<PerfilDetalhe>>.Ok([new(Guid.NewGuid(), "Produção", TipoPerfil.Producao, true, 2)]))
        .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([]))
        .Responde<DefinirAcessoDoDocumento, Resultado>(Resultado.Ok());

    [Fact]
    public void Os_botoes_que_abrem_tem_o_id_que_o_foco_de_volta_procura()
    {
        var ficha = Ficha();

        ficha.Find("#documentos-enviar").TextContent.Should().Contain("Enviar documento");
        ficha.Find($"#documento-acesso-{Procuracao:N}").TextContent.Should().Be("Acesso");
        ficha.Find($"#documento-renomear-{Procuracao:N}").TextContent.Should().Be("Renomear");
        ficha.Find($"#documento-excluir-{Procuracao:N}").TextContent.Should().Be("Excluir");
        ficha.Find($"#documento-{Procuracao:N} .aj-menu-acoes > .aj-botao").GetAttribute("aria-label")
            .Should().Be("Ações de Procuração.pdf");
    }

    [Fact]
    public void Cancelar_o_envio_devolve_o_foco_ao_enviar_documento_do_painel()
    {
        var ficha = Ficha();
        ficha.Find("#documentos-enviar").Click();

        ficha.FindAll("form.aj-painel button").Single(b => b.TextContent == "Cancelar").Click();

        ficha.FindAll("form.aj-painel").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be("#documentos-enviar");
    }

    [Fact]
    public void Cancelar_o_acesso_devolve_o_foco_ao_acesso_da_linha()
    {
        CombinarAcesso();
        var ficha = Ficha();
        ficha.FindAll("table.aj-tabela tbody button").Single(b => b.TextContent == "Acesso").Click();
        ficha.WaitForAssertion(() => ficha.FindAll("form#documentos-acesso").Should().ContainSingle());

        ficha.FindAll("form#documentos-acesso button").Single(b => b.TextContent == "Cancelar").Click();

        ficha.FindAll("form#documentos-acesso").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be($"#documento-acesso-{Procuracao:N}, #documento-{Procuracao:N} .aj-menu-acoes > .aj-botao");
    }

    [Fact]
    public void Salvar_o_acesso_pelo_Enter_salva_e_leva_o_foco_ao_aviso_de_resultado()
    {
        CombinarAcesso();
        var ficha = Ficha();
        ficha.FindAll("table.aj-tabela tbody button").Single(b => b.TextContent == "Acesso").Click();
        ficha.WaitForAssertion(() => ficha.FindAll("form#documentos-acesso").Should().ContainSingle());
        ficha.Find("form#documentos-acesso").QuerySelectorAll("button").Select(b => (b.TextContent, b.GetAttribute("type")))
            .Should().Equal(("Cancelar", "button"), ("Salvar acesso", "submit"));

        ficha.Find("form#documentos-acesso").Submit();

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Acesso atualizado."));
        Despachante.Recebidas.OfType<DefinirAcessoDoDocumento>().Should().ContainSingle();
        var aviso = ficha.FindAll(".aj-aviso").Single(a => a.TextContent.Contains("Acesso atualizado."));
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }

    [Fact]
    public void Envio_e_renomear_sao_formularios_que_o_Enter_envia()
    {
        Despachante
            .Responde<EnviarDocumento, Resultado<Guid>>(Resultado<Guid>.Ok(Guid.NewGuid()))
            .Responde<RenomearDocumento, Resultado>(Resultado.Ok());
        var ficha = Ficha();
        ficha.Find("#documentos-enviar").Click();
        ficha.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("conteúdo", "contrato.pdf"));

        ficha.Find("form#documentos-envio").Submit();

        ficha.WaitForAssertion(() => Despachante.Recebidas.OfType<EnviarDocumento>().Should().ContainSingle());
        ficha.FindAll("table.aj-tabela tbody button").Single(b => b.TextContent == "Renomear").Click();
        ficha.Find("form#documentos-renomear input.aj-campo").Change("Procuração assinada.pdf");

        ficha.Find("form#documentos-renomear").Submit();

        ficha.WaitForAssertion(() => Despachante.Recebidas.OfType<RenomearDocumento>().Should().ContainSingle()
            .Which.Should().Be(new RenomearDocumento(Procuracao, "Procuração assinada.pdf")));
    }

    [Fact]
    public async Task Resposta_de_uma_area_que_ja_ficou_para_tras_e_descartada()
    {
        var ficha = Ficha();   // Gerais, respondida na hora
        var velha = new TaskCompletionSource<Resultado<PaginaDeDocumentos>>();
        Despachante.RespondeDepois<ListarDocumentosDoCliente, Resultado<PaginaDeDocumentos>>(velha.Task);
        Chip(ficha, "De atividades");   // a consulta fica esperando
        Despachante.Responde<ListarDocumentosDoCliente, Resultado<PaginaDeDocumentos>>(Pagina);
        Chip(ficha, "Do cliente");      // responde na hora

        velha.SetResult(Resultado<PaginaDeDocumentos>.Ok(new([
            new DocumentoResumo(Guid.NewGuid(), "Contestação.pdf", "contestacao.pdf", 1024, CategoriaDocumento.DocumentoAtividade,
                new DateTime(2026, 9, 3, 13, 0, 0, DateTimeKind.Utc), "Marina Couto", null, "Contestação — cobrança de ISS"),
        ], 1, 1, 20, Contagem)));
        // A continuação da resposta velha entra na fila do renderizador antes desta chamada vazia: quando ela volta,
        // a resposta velha já foi tratada (as esperas seguintes da cadeia continuam no mesmo contexto, sem voltar à fila).
        await ficha.InvokeAsync(() => { });

        ficha.Find(".aj-chips button[aria-pressed=true]").TextContent.Should().Be("Do cliente · 2");
        ficha.Markup.Should().Contain("Procuração.pdf").And.NotContain("Contestação.pdf");
    }

    [Fact]
    public void Tentar_de_novo_mostra_carregando_ate_a_resposta()
    {
        Despachante.Responde<ListarDocumentosDoCliente, Resultado<PaginaDeDocumentos>>(
            Resultado<PaginaDeDocumentos>.Falha(TipoErro.Validacao, "Falha de teste."));
        var ficha = Desenhar<DocumentosDaFicha>(p => p.Add(d => d.ClienteId, Maria).Add(d => d.PodeGerenciar, true));
        var nova = new TaskCompletionSource<Resultado<PaginaDeDocumentos>>();
        Despachante.RespondeDepois<ListarDocumentosDoCliente, Resultado<PaginaDeDocumentos>>(nova.Task);

        ficha.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        ficha.Markup.Should().Contain("Carregando…").And.NotContain("Falha de teste.");
        nova.SetResult(Pagina(new ListarDocumentosDoCliente(Maria, CategoriaDocumento.DocumentoGeral)));
        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Procuração.pdf"));
        ficha.Markup.Should().NotContain("Carregando…");
    }
```

O teste do descarte tem de falhar com o código de hoje (confira na execução abaixo que a falha é "Contestação.pdf" na tela ou o chip errado, e não outra). Se ele passar com o código de hoje, a barreira não drenou a fila: repita a linha `await ficha.InvokeAsync(() => { });` mais duas vezes (cada chamada espera uma volta da fila do renderizador) e registre no relatório.

Run: `dotnet build -m:1`
Expected: build sem erro.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DocumentosDaFicha"`
Expected: FAIL — sem os ids, sem os `form`, sem pedido ao módulo de foco; a resposta velha aparece ("Contestação.pdf"); "Tentar de novo" deixa o erro à vista; e `DocumentosDaFichaTests.Diretoria_em_gerais_ve_a_tabela_com_as_acoes_o_acesso_e_a_ajuda` falha pela linha sem `id`.

- [ ] **Step 3: Testes dos documentos da atividade (devem falhar)**

A ficha da atividade é a herdeira com as ações do desktop numa lista (`li.aj-documento`), sem tabela: o foco de volta pelo "⋯" do tablet e do mobile é provado aqui, e o campo de arquivo desabilitado durante um envio direto, sem confirmação (B10e).

Em `tests/AtividadesJuridicas.IntegrationTests/DocumentosDaAtividadeInterativosTests.cs`, reescreva (os painéis de envio e de renomear passam a `form`; cada asserção prova o mesmo; liste no relatório):

| Linha | Hoje | Depois |
|---|---|---|
| 60 (auxiliar `Paineis`) | `painel.FindAll("section.aj-painel h2")` | `painel.FindAll(".aj-painel h2")` (o painel "Documentos" é `section`, os de envio e renomear são `form`; a ordem do documento é a mesma) |
| 129, 133, 156, 172, 243 | `"section.aj-painel button"` | `"form.aj-painel button"` |
| 153, 155 | `"section.aj-painel input.aj-campo"` | `"form.aj-painel input.aj-campo"` |

Acrescente ao fim da classe:

```csharp
    private static string DesktopOuMenu(string acao, Guid documento) =>
        $"#documento-{acao}-{documento:N}, #documento-{documento:N} .aj-menu-acoes > .aj-botao";

    [Fact]
    public void Os_botoes_que_abrem_tem_o_id_que_o_foco_de_volta_procura()
    {
        var painel = Painel();

        painel.Find("#documentos-enviar").TextContent.Should().Contain("Enviar documento");
        painel.Find($"#documento-renomear-{Calculo:N}").TextContent.Should().Be("Renomear");
        painel.Find($"#documento-excluir-{Calculo:N}").TextContent.Should().Be("Excluir");
        painel.Find($"li#documento-{Calculo:N} .aj-menu-acoes > .aj-botao").GetAttribute("aria-label")
            .Should().Be("Ações de Cálculo das verbas");
    }

    [Fact]
    public void Cancelar_o_renomear_aberto_pelo_menu_devolve_o_foco_ao_menu_da_linha()
    {
        var painel = Painel();
        ClicarNoMenu(painel, "Renomear", linha: 1);

        painel.FindAll("form.aj-painel button").Single(b => b.TextContent == "Cancelar").Click();

        Paineis(painel).Should().Equal("Documentos");
        UltimoSeletorFocado().Should().Be(DesktopOuMenu("renomear", Calculo));
    }

    [Fact]
    public void Cancelar_a_exclusao_devolve_o_foco_ao_excluir_da_linha()
    {
        var painel = Painel();
        ClicarNaLinha(painel, "Excluir");

        painel.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Cancelar").Click();

        painel.FindAll("[role=alertdialog]").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be(DesktopOuMenu("excluir", Minuta));
    }

    [Fact]
    public void Cancelar_o_envio_devolve_o_foco_ao_enviar_documento()
    {
        var painel = Painel();
        AbrirEnvio(painel);

        painel.FindAll("form.aj-painel button").Single(b => b.TextContent == "Cancelar").Click();

        Paineis(painel).Should().Equal("Documentos");
        UltimoSeletorFocado().Should().Be("#documentos-enviar");
    }

    [Fact]
    public void Enviar_pelo_Enter_envia_e_leva_o_foco_ao_aviso_de_resultado()
    {
        Despachante.Responde<EnviarDocumentoDaAtividade, Resultado<Guid>>(Resultado<Guid>.Ok(Guid.NewGuid()));
        var painel = Painel();
        AbrirEnvio(painel);
        painel.Find("form#documentos-envio").QuerySelectorAll("button").Select(b => (b.TextContent, b.GetAttribute("type")))
            .Should().Equal(("Cancelar", "button"), ("Enviar", "submit"));
        Escolher(painel, "minuta-final.docx");

        painel.Find("form#documentos-envio").Submit();

        painel.WaitForAssertion(() => painel.Markup.Should().Contain("Documento enviado."));
        var aviso = painel.FindAll(".aj-aviso").Single(a => a.TextContent.Contains("Documento enviado."));
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }

    [Fact]
    public void Renomear_pelo_Enter_salva_o_nome()
    {
        Despachante.Responde<RenomearDocumento, Resultado>(Resultado.Ok());
        var painel = Painel();
        ClicarNaLinha(painel, "Renomear");
        painel.Find("form#documentos-renomear input.aj-campo").Change("Minuta assinada.docx");

        painel.Find("form#documentos-renomear").Submit();

        painel.WaitForAssertion(() => Despachante.Recebidas.OfType<RenomearDocumento>().Should().ContainSingle()
            .Which.Should().Be(new RenomearDocumento(Minuta, "Minuta assinada.docx")));
    }

    [Fact]
    public void Durante_o_envio_o_campo_de_arquivo_fica_desabilitado()
    {
        var envio = new TaskCompletionSource<Resultado<Guid>>();
        Despachante.RespondeDepois<EnviarDocumentoDaAtividade, Resultado<Guid>>(envio.Task);
        var painel = Painel();
        AbrirEnvio(painel);
        Escolher(painel, "minuta-final.docx");

        painel.Find("form#documentos-envio").Submit();

        painel.Find("input[type=file]").HasAttribute("disabled").Should().BeTrue();
        painel.FindAll("form.aj-painel button").Single(b => b.TextContent == "Enviando…").HasAttribute("disabled").Should().BeTrue();

        envio.SetResult(Resultado<Guid>.Ok(Guid.NewGuid()));
        painel.WaitForAssertion(() => painel.Markup.Should().Contain("Documento enviado."));
        Paineis(painel).Should().Equal("Documentos");
    }

    [Fact]
    public void Tentar_de_novo_mostra_carregando_ate_a_resposta()
    {
        Despachante.Responde<ListarDocumentosDaAtividade, Resultado<Pagina<DocumentoResumo>>>(
            Resultado<Pagina<DocumentoResumo>>.Falha(TipoErro.Validacao, "Falha de teste."));
        var painel = Painel();
        var nova = new TaskCompletionSource<Resultado<Pagina<DocumentoResumo>>>();
        Despachante.RespondeDepois<ListarDocumentosDaAtividade, Resultado<Pagina<DocumentoResumo>>>(nova.Task);

        painel.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        painel.Markup.Should().Contain("Carregando…").And.NotContain("Falha de teste.");
        nova.SetResult(Resultado<Pagina<DocumentoResumo>>.Ok(new Pagina<DocumentoResumo>([DaMinuta, DoCalculo], 2, 1, 20)));
        painel.WaitForAssertion(() => painel.FindAll("div.aj-so-desktop li.aj-documento").Should().HaveCount(2));
        painel.Markup.Should().NotContain("Carregando…");
    }
```

Run: `dotnet build -m:1`
Expected: build sem erro.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DocumentosDaAtividadeInterativosTests"`
Expected: FAIL — os testes reescritos e os novos não acham os `form` nem os ids; nada é pedido ao módulo de foco; o campo de arquivo não fica desabilitado; "Tentar de novo" deixa o erro à vista.

- [ ] **Step 4: A base `GestaoDeDocumentos`**

Em `src/AtividadesJuridicas.Web/Components/Documentos/GestaoDeDocumentos.cs` (o `using AtividadesJuridicas.Web.Infra;` já está no arquivo):

1. Depois de `[Inject] protected IDespachante Despachante …`, injete o serviço da Tarefa 3:

```csharp
    [Inject] protected Foco Foco { get; set; } = default!;
```

2. Depois do campo `_envioConfirmado`, os campos novos:

```csharp
    // "Salvar nome" em curso: o segundo clique (ou o segundo Enter) não renomeia de novo.
    protected bool _salvandoNome;

    // "Tentar de novo" em curso: a lista mostra "Carregando…" no lugar do erro até a resposta.
    protected bool _recarregando;

    // A geração da consulta da lista: a resposta de uma consulta que outra mais nova já substituiu é descartada.
    private int _geracao;

    // O acesso foi encerrado: a página mostra o aviso dela, e nenhuma mensagem de ação aparece mais.
    private bool _encerrado;

    /// <summary>
    /// A geração do aviso de resultado, que as telas usam como chave do <c>Aviso</c>: cada mensagem nova recria o aviso,
    /// que se revela de novo (o <c>Revelar</c> só age quando o aviso aparece), mesmo que a frase seja a mesma.
    /// </summary>
    protected int _versaoDoAviso;

    // O botão que abriu o que acabou de ser cancelado: o foco volta a ele depois do desenho.
    private string? _focoDeVolta;
```

3. Depois do record `Linha`, os ids e os seletores do foco de volta:

```csharp
    /// <summary>O "Enviar documento" que fica no painel da lista (ficha do cliente e da atividade): um só na tela.</summary>
    protected const string IdDoEnviarDocumento = "documentos-enviar";

    /// <summary>Os formulários dos painéis de envio, de renomear e de acesso: um de cada, nunca por visão.</summary>
    protected const string IdDoPainelDeEnvio = "documentos-envio";
    protected const string IdDoPainelDeRenomear = "documentos-renomear";
    protected const string IdDoPainelDeAcesso = "documentos-acesso";

    /// <summary>Os botões de uma linha no desktop, um por ação.</summary>
    protected static string IdDoRenomear(Guid documento) => $"documento-renomear-{documento:N}";
    protected static string IdDoExcluir(Guid documento) => $"documento-excluir-{documento:N}";
    protected static string IdDoAcesso(Guid documento) => $"documento-acesso-{documento:N}";

    /// <summary>A linha do tablet e do mobile, onde as ações ficam no "⋯" (o <c>MenuDeAcoes</c>).</summary>
    protected static string IdDaLinha(Guid documento) => $"documento-{documento:N}";

    // Quem abriu uma ação da linha: o botão da ação no desktop ou o "⋯" da linha no tablet e no mobile. As duas cópias
    // estão no HTML; o módulo de foco leva o foco à que está à vista.
    private static string DesktopOuMenu(string idDoBotao, Guid documento) =>
        $"#{idDoBotao}, #{IdDaLinha(documento)} .aj-menu-acoes > .aj-botao";

    /// <summary>
    /// O "Enviar documento" que abriu o envio. O padrão é o do painel da lista; os Documentos comuns, com o do cabeçalho e
    /// o flutuante, sobrescrevem.
    /// </summary>
    protected virtual string SeletorDoEnviarDocumento => $"#{IdDoEnviarDocumento}";

    // O "Enviar" do painel, que abriu a confirmação de envio.
    private const string SeletorDoEnviarDoPainel = "#" + IdDoPainelDeEnvio + " .aj-botao--primario";
```

4. `CarregarAsync` descarta a resposta superada (o padrão do `Andamento.razor`). Substitua o começo do método, até a leitura da contagem, por:

```csharp
    private async Task CarregarAsync(int pagina)
    {
        var geracao = ++_geracao;
        _paginaPedida = pagina;
        _excluindo = null;
        var resultado = await ListarPaginaAsync(pagina);
        // Outra consulta (outra área, outra página) foi pedida enquanto esta esperava: vale a dela, esta é descartada.
        if (geracao != _geracao) return;

        // Só a lista de um cliente traz a contagem por área.
        if (resultado.Valor is PaginaDeDocumentos lidos) _porCategoria = lidos.PorCategoria;
```

O resto do método fica como está.

5. Depois de `IrParaAsync`, o "Tentar de novo":

```csharp
    /// <summary>
    /// "Tentar de novo" da lista: o erro dá lugar ao "Carregando…" até a resposta, e um segundo clique com a consulta em
    /// curso não consulta de novo.
    /// </summary>
    protected async Task TentarDeNovoAsync()
    {
        if (_recarregando) return;

        _recarregando = true;
        _erroDaLista = null;
        try
        {
            await IrParaAsync(_paginaPedida);
        }
        finally
        {
            _recarregando = false;
        }
    }
```

6. `EncerradoAsync`, depois de `if (resultado.Erro != TipoErro.AcessoNegado) return false;`:

```csharp
        _encerrado = true;
        _mensagem = null;
```

7. `OnAfterRenderAsync` ganha o foco de volta, depois dos dois blocos de hoje:

```csharp
        if (_focoDeVolta is { } seletor)
        {
            _focoDeVolta = null;
            await TentarFocarAsync(() => new ValueTask(Foco.FocarPrimeiroVisivelAsync(seletor)));
        }
```

8. A confirmação de envio à vista (B10e) e o cancelamento dela (B6). Substitua `CancelarEnvio` e acrescente `ConfirmacaoAVista` logo antes dele:

```csharp
    /// <summary>
    /// A confirmação de envio na tela: esperando a resposta e, depois de confirmada, enquanto o envio dura (os botões dela
    /// ficam desabilitados e o painel continua escondido).
    /// </summary>
    protected bool ConfirmacaoAVista => _confirmacao.AguardandoConfirmacao || (_enviando && _envioConfirmado);

    // "Cancelar" da confirmação: o arquivo e o nome escolhidos ficam (o usuário pode enviar depois), o painel volta à
    // vista e o foco vai ao "Enviar" dele, que abriu a confirmação.
    protected void CancelarEnvio()
    {
        _confirmacao.Cancelar();
        _focoDeVolta = SeletorDoEnviarDoPainel;
    }
```

Em `EnviarEscolhidoAsync`, a linha `_envioConfirmado = confirmou;` sai de dentro do `try` e passa para antes de `_enviando = true;` (os dois valem já no primeiro desenho do envio, que mostra a confirmação ainda à vista).

9. `AbrirEnvio` com a confirmação à vista (B7). Logo depois de `if (Ocupada) return;`:

```csharp
        // Com a confirmação à vista o envio já está aberto e espera por ela: o foco vai à confirmação, que continua.
        if (_confirmacao.AguardandoConfirmacao)
        {
            _focarAviso = true;
            return;
        }
```

10. Os cancelamentos que devolvem o foco (B6). Acrescente depois de `DescartarEnvio`:

```csharp
    /// <summary>"Cancelar" do painel de envio: fecha descartando o arquivo e devolve o foco ao "Enviar documento".</summary>
    protected void CancelarPainelDeEnvio()
    {
        DescartarEnvio();
        _focoDeVolta = SeletorDoEnviarDocumento;
    }

    /// <summary>"Cancelar" do renomear: fecha e devolve o foco ao "Renomear" (ou ao "⋯") da linha.</summary>
    protected void CancelarRenomear()
    {
        if (_renomeando is { } documento) _focoDeVolta = DesktopOuMenu(IdDoRenomear(documento.Id), documento.Id);
        _renomeando = null;
    }

    /// <summary>"Cancelar" da confirmação de exclusão: devolve o foco ao "Excluir" (ou ao "⋯") da linha.</summary>
    protected void CancelarConfirmacaoDeExclusao()
    {
        if (_excluindo is { } id) _focoDeVolta = DesktopOuMenu(IdDoExcluir(id), id);
        _excluindo = null;
    }

    /// <summary>"Cancelar" do acesso: fecha e devolve o foco ao "Acesso" (ou ao "⋯") da linha.</summary>
    protected void CancelarAcesso()
    {
        if (_acesso is { } acesso) _focoDeVolta = DesktopOuMenu(IdDoAcesso(acesso.DocumentoId), acesso.DocumentoId);
        _acesso = null;
    }
```

`DescartarEnvio`, `FecharRenomear`, `CancelarExclusao` e `FecharAcesso` continuam como estão: são os fechamentos internos (abrir outra coisa, trocar de área, concluir), que não devolvem o foco.

11. `RenomearAsync` com a guarda (B10a): o método inteiro passa a

```csharp
    protected async Task RenomearAsync()
    {
        if (_renomeando is not { } documento || _salvandoNome) return;

        _salvandoNome = true;
        _mensagem = null;
        try
        {
            var resultado = await Despachante.EnviarAsync(new RenomearDocumento(documento.Id, _novoNome));
            if (await EncerradoAsync(resultado)) return;

            if (!resultado.Sucesso)
            {
                Avisar(resultado.Mensagem, falhou: true);
                if (resultado.Erro == TipoErro.NaoEncontrado)
                {
                    // O documento já não existe: fecha o painel e atualiza a lista, mantendo a mensagem.
                    if (ReferenceEquals(_renomeando, documento)) _renomeando = null;
                    await CarregarAsync(_pagina.Numero);
                }

                return;
            }

            if (ReferenceEquals(_renomeando, documento)) _renomeando = null;
            await CarregarAsync(_pagina.Numero);
            if (_erroDaLista is null) Avisar("Nome alterado.", falhou: false);
            else Avisar("Nome alterado, mas não foi possível atualizar a lista.", falhou: true);
        }
        finally
        {
            _salvandoNome = false;
        }
    }
```

12. `Avisar` (E4 e a geração do aviso):

```csharp
    private void Avisar(string? mensagem, bool falhou)
    {
        // Com o acesso encerrado a página mostra o aviso dela: a mensagem da ação não fica ao lado.
        if (_encerrado) return;

        _mensagem = mensagem;
        _falhou = falhou;
        _versaoDoAviso++;
    }
```

Por que o foco de volta vai por seletor e não por `ElementReference`: o "Enviar documento" dos Documentos comuns tem duas cópias (o botão do cabeçalho e o flutuante), e o "Renomear", o "Excluir" e o "Acesso" de uma linha existem no desktop (botão da linha) e no tablet e no mobile (o "⋯" do `MenuDeAcoes`, cujo botão é interno ao componente e não aceita `@ref` nem `id`); só uma cópia está à vista, e só o navegador sabe qual. O `focarPrimeiroVisivel` escolhe a que tem caixa. Os seletores são estáveis: um `id` por visão nos botões do desktop e na linha do tablet e do mobile (o `Guid` em formato `N`, só letras e números, sem escape de CSS), o `aria-label` exato do botão flutuante e a classe do botão de ação do cabeçalho (filho direto de `.aj-cabecalho`, um só por página). O `aria-label` "Ações de {nome}" do "⋯" não serviria sozinho: dois documentos podem ter o mesmo nome, e o nome pode ter aspas. O `aria-controls` também não: os painéis só existem abertos.

- [ ] **Step 5: Os Documentos comuns**

`src/AtividadesJuridicas.Web/Components/Documentos/DocumentosComunsDaDiretoria.razor`, da linha 8 à 61 (o aviso, a confirmação de envio, a confirmação de exclusão e os dois painéis), passa a:

```razor
@if (_mensagem is not null)
{
    @* A chave é a geração do aviso: cada mensagem nova é um aviso novo, que se revela (foco e rolagem até ele). *@
    <Aviso @key="_versaoDoAviso" Tipo="@(_falhou ? TipoDeAviso.Erro : TipoDeAviso.Sucesso)" Revelar="true">@_mensagem</Aviso>
}

@* A confirmação fica à vista também durante o envio confirmado, com os botões desabilitados. *@
@if (ConfirmacaoAVista && _arquivo is { } escolhido)
{
    <Aviso Tipo="TipoDeAviso.Atencao" Rotulo="Confirmar envio" Titulo="@TituloDoEnvio(escolhido)">
        <ChildContent>Documentos desta área ficam visíveis para todos os clientes do escritório. Não envie aqui arquivos de um cliente específico.</ChildContent>
        <Acoes>
            <button type="button" class="aj-botao aj-botao--secundario" disabled="@_enviando" @onclick="CancelarEnvio">Cancelar</button>
            <button type="button" class="aj-botao aj-botao--primario" disabled="@_enviando" @onclick="ConfirmarEnvioAsync">Confirmar envio</button>
        </Acoes>
    </Aviso>
}

@if (AExcluir is { } aExcluir)
{
    @* A chave é o documento: pedir a exclusão de outro com a confirmação aberta é uma confirmação nova, que
       aparece de novo e por isso leva o foco de novo. *@
    <Aviso @key="aExcluir.Id" Tipo="TipoDeAviso.Atencao" Rotulo="Confirmar exclusão" Titulo="@($"Confirmar a exclusão de {aExcluir.Nome}?")">
        <ChildContent>O documento deixa de aparecer para todos os clientes.</ChildContent>
        <Acoes>
            <button type="button" class="aj-botao aj-botao--secundario" disabled="@_removendo" @onclick="CancelarConfirmacaoDeExclusao">Cancelar</button>
            <button type="button" class="aj-botao aj-botao--primario" disabled="@_removendo" @onclick="() => ExcluirAsync(aExcluir.Id)">Confirmar exclusão</button>
        </Acoes>
    </Aviso>
}

@if (_envioAberto)
{
    @* Escondido, não removido, enquanto a confirmação está à vista: o arquivo escolhido mora neste campo. Um formulário
       com o seu botão de envio: o Enter no nome envia (o "Enviar" desabilitado, sem arquivo, não deixa). *@
    <form class="aj-painel" id="@IdDoPainelDeEnvio" hidden="@ConfirmacaoAVista" @onsubmit="EnviarAsync" @onsubmit:preventDefault>
        <h2>Enviar documento</h2>
        <label class="aj-rotulo">@RotuloDoArquivo<InputFile @key="_versaoDoCampo" @ref="_campoDeArquivo" OnChange="Escolher" class="aj-campo" disabled="@_enviando" /></label>
        <label class="aj-rotulo">Nome (opcional)<input type="text" class="aj-campo" @bind="_nomeDoEnvio" /></label>
        <div class="aj-painel__acoes">
            <button type="button" class="aj-botao aj-botao--secundario" disabled="@_enviando" @onclick="CancelarPainelDeEnvio">Cancelar</button>
            <button type="submit" class="aj-botao aj-botao--primario" disabled="@(_arquivo is null || _enviando)">@(_enviando ? "Enviando…" : "Enviar")</button>
        </div>
    </form>
}

@if (_renomeando is { } renomeando)
{
    <form class="aj-painel" id="@IdDoPainelDeRenomear" @onsubmit="RenomearAsync" @onsubmit:preventDefault>
        <h2>@($"Renomear \"{renomeando.Nome}\"")</h2>
        <label class="aj-rotulo">Novo nome<input type="text" class="aj-campo" @bind="_novoNome" @ref="_campoDoNovoNome" /></label>
        <div class="aj-painel__acoes">
            <button type="button" class="aj-botao aj-botao--secundario" @onclick="CancelarRenomear">Cancelar</button>
            <button type="submit" class="aj-botao aj-botao--primario" disabled="@_salvandoNome">Salvar nome</button>
        </div>
    </form>
}
```

No `EstadoDaLista`: `Carregando="@(!_carregado || _recarregando)"` e `TentarDeNovo="TentarDeNovoAsync"`.

Na célula de ações da tabela (linha 82), os dois botões ganham o `id`, **depois** do `class` (os testes estáticos de hoje casam `class="…"[^>]*>`), e continuam colados:

```razor
<button type="button" class="aj-botao aj-botao--fantasma aj-botao--menor" id="@IdDoRenomear(documento.Id)" disabled="@Ocupada" @onclick="() => RenomearLinha(documento)">Renomear</button><button type="button" class="aj-botao aj-botao--perigo aj-botao--menor" id="@IdDoExcluir(documento.Id)" disabled="@Ocupada" @onclick="() => ExcluirLinha(documento.Id)">Excluir</button>
```

A linha do tablet e do mobile (linha 97): `<div class="aj-linha" id="@IdDaLinha(documento.Id)" @key="documento.Id">`.

No `@code`, troque `FocarAvisoDeEnvioAsync` e acrescente o seletor do "Enviar documento":

```csharp
    private const string SeletorDaConfirmacaoDeEnvio = "[role=\"alertdialog\"][aria-label=\"Confirmar envio\"]";

    // A confirmação leva o foco a si quando aparece; este pedido serve ao "Enviar documento" clicado com ela à vista.
    protected override ValueTask FocarAvisoDeEnvioAsync() => new(Foco.FocarPrimeiroVisivelAsync(SeletorDaConfirmacaoDeEnvio));

    // O "Enviar documento" desta tela: o do cabeçalho (desktop e tablet) ou o botão flutuante (mobile).
    protected override string SeletorDoEnviarDocumento => ".aj-cabecalho > .aj-botao--primario, .aj-fab[aria-label=\"Enviar documento\"]";
```

Run: `dotnet build -m:1`
Expected: build sem erro e sem aviso.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DocumentosComunsInterativosTests"`
Expected: todos passando.

- [ ] **Step 6: Os documentos da ficha do cliente**

`src/AtividadesJuridicas.Web/Components/Documentos/DocumentosDaFicha.razor`:

- O aviso (linha 6): `<Aviso @key="_versaoDoAviso" Tipo="@(_falhou ? TipoDeAviso.Erro : TipoDeAviso.Sucesso)" Revelar="true">@_mensagem</Aviso>`.
- O "Cancelar" da confirmação de exclusão (linha 15): `@onclick="CancelarConfirmacaoDeExclusao"`.
- O "Enviar documento" (linha 26) ganha `id="@IdDoEnviarDocumento"` depois do `class`: `<button type="button" class="aj-botao aj-botao--secundario aj-botao--com-icone" id="@IdDoEnviarDocumento" disabled="@Ocupada" @onclick="AbrirEnvio">`.
- O `EstadoDaLista` (linha 37): `Carregando="@(!_carregado || _recarregando)"` e `TentarDeNovo="TentarDeNovoAsync"`.
- A célula de ações da tabela (linhas 62–63): `id="@IdDoRenomear(documento.Id)"` no "Renomear", `id="@IdDoAcesso(documento.Id)"` no "Acesso" e `id="@IdDoExcluir(documento.Id)"` no "Excluir", cada um depois do `class`; os botões continuam colados.
- A linha do tablet e do mobile (linha 76): `<div class="aj-linha" id="@IdDaLinha(documento.Id)" @key="documento.Id">`.
- Os três painéis passam a formulários:

```razor
@if (_envioAberto)
{
    <form class="aj-painel" id="@IdDoPainelDeEnvio" @onsubmit="EnviarAsync" @onsubmit:preventDefault>
        <h2>Enviar documento</h2>
        <span class="aj-ajuda">@($"Vai para {NomeDaArea(_categoria)}.")</span>
        <label class="aj-rotulo">@RotuloDoArquivo<InputFile @key="_versaoDoCampo" @ref="_campoDeArquivo" OnChange="Escolher" class="aj-campo" disabled="@_enviando" /></label>
        <label class="aj-rotulo">Nome (opcional)<input type="text" class="aj-campo" @bind="_nomeDoEnvio" /></label>
        <div class="aj-painel__acoes"><button type="button" class="aj-botao aj-botao--secundario" disabled="@_enviando" @onclick="CancelarPainelDeEnvio">Cancelar</button><button type="submit" class="aj-botao aj-botao--primario" disabled="@(_arquivo is null || _enviando)">@(_enviando ? "Enviando…" : "Enviar")</button></div>
    </form>
}

@if (_renomeando is { } renomeando)
{
    <form class="aj-painel" id="@IdDoPainelDeRenomear" @onsubmit="RenomearAsync" @onsubmit:preventDefault>
        <h2>@($"Renomear \"{renomeando.Nome}\"")</h2>
        <label class="aj-rotulo">Novo nome<input type="text" class="aj-campo" @bind="_novoNome" @ref="_campoDoNovoNome" /></label>
        <div class="aj-painel__acoes"><button type="button" class="aj-botao aj-botao--secundario" @onclick="CancelarRenomear">Cancelar</button><button type="submit" class="aj-botao aj-botao--primario" disabled="@_salvandoNome">Salvar nome</button></div>
    </form>
}
```

  E o painel de acesso: a tag de abertura `<section class="aj-painel">` (linha 129) passa a `<form class="aj-painel" id="@IdDoPainelDeAcesso" @onsubmit="SalvarAcessoAsync" @onsubmit:preventDefault>`, a de fechamento (linha 165) a `</form>`, e as ações (linhas 162–163):

```razor
            <button type="button" class="aj-botao aj-botao--secundario" @ref="_cancelarDoAcesso" @onclick="CancelarAcesso">Cancelar</button>
            <button type="submit" class="aj-botao aj-botao--primario">Salvar acesso</button>
```

  O painel de acesso só tem caixas de marcar: o Enter numa caixa não envia em todos os navegadores; ele vira `form` para os três painéis seguirem a mesma regra (um formulário com o seu botão de envio) e o "Salvar acesso" ser o envio do painel.

Run: `dotnet build -m:1`
Expected: build sem erro.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DocumentosDaFicha"`
Expected: todos passando (os interativos e `DocumentosDaFichaTests`).

- [ ] **Step 7: Os documentos da atividade**

`src/AtividadesJuridicas.Web/Components/Atividades/DocumentosDaAtividade.razor`:

- O aviso (linha 9): `<Aviso @key="_versaoDoAviso" Tipo="@(_falhou ? TipoDeAviso.Erro : TipoDeAviso.Sucesso)" Revelar="true">@_mensagem</Aviso>`.
- O "Cancelar" da confirmação de exclusão (linha 18): `@onclick="CancelarConfirmacaoDeExclusao"`.
- O "Enviar documento" (linha 30): `<button type="button" class="aj-botao aj-botao--secundario aj-botao--com-icone" id="@IdDoEnviarDocumento" disabled="@Ocupada" @onclick="AbrirEnvio">`.
- O `EstadoDaLista` (linha 46): `Carregando="@(!_carregado || _recarregando)"` e `TentarDeNovo="TentarDeNovoAsync"`.
- As ações do desktop (linha 67), com o `id` depois do `class` e os botões colados:

```razor
<div class="aj-documento__acoes"><button type="button" class="aj-botao aj-botao--fantasma" id="@IdDoRenomear(documento.Id)" disabled="@Ocupada" @onclick="() => RenomearLinha(documento)">Renomear</button><button type="button" class="aj-botao aj-botao--perigo" id="@IdDoExcluir(documento.Id)" disabled="@Ocupada" @onclick="() => ExcluirLinha(documento.Id)">Excluir</button></div>
```

- A linha do tablet e do mobile (linha 78): `<li class="aj-documento" id="@IdDaLinha(documento.Id)" @key="documento.Id">`.
- Os dois painéis passam a formulários:

```razor
@if (_envioAberto && !Encerrado)
{
    <form class="aj-painel" id="@IdDoPainelDeEnvio" @onsubmit="EnviarAsync" @onsubmit:preventDefault>
        <h2>Enviar documento</h2>
        <label class="aj-rotulo">@RotuloDoArquivo<InputFile @key="_versaoDoCampo" @ref="_campoDeArquivo" OnChange="Escolher" class="aj-campo" disabled="@_enviando" /></label>
        <label class="aj-rotulo">Nome (opcional)<input type="text" class="aj-campo" @bind="_nomeDoEnvio" /></label>
        <div class="aj-painel__acoes"><button type="button" class="aj-botao aj-botao--secundario" disabled="@_enviando" @onclick="CancelarPainelDeEnvio">Cancelar</button><button type="submit" class="aj-botao aj-botao--primario" disabled="@(_arquivo is null || _enviando)">@(_enviando ? "Enviando…" : "Enviar")</button></div>
    </form>
}

@if (_renomeando is { } renomeando && !Encerrado)
{
    <form class="aj-painel" id="@IdDoPainelDeRenomear" @onsubmit="RenomearAsync" @onsubmit:preventDefault>
        <h2>@($"Renomear \"{renomeando.Nome}\"")</h2>
        <label class="aj-rotulo">Novo nome<input type="text" class="aj-campo" @bind="_novoNome" @ref="_campoDoNovoNome" /></label>
        <div class="aj-painel__acoes"><button type="button" class="aj-botao aj-botao--secundario" @onclick="CancelarRenomear">Cancelar</button><button type="submit" class="aj-botao aj-botao--primario" disabled="@_salvandoNome">Salvar nome</button></div>
    </form>
}
```

- No `@code`, o mapa dos arquivos enviados acumula em vez de ser trocado a cada página: uma resposta atrasada de outra página, que a base descarta depois da leitura, não apaga o tipo dos ícones da página que está na tela (os ids são únicos e o arquivo de um documento não muda):

```csharp
    // O arquivo enviado de cada documento lido: o nome pode ter sido trocado sem a extensão, e o tipo do ícone é o do
    // arquivo. Acumula entre páginas: a resposta de uma consulta que a base descarta não apaga o da página na tela.
    private readonly Dictionary<Guid, string> _arquivos = [];
```

  e em `ListarPaginaAsync`, no lugar de `if (resultado.Valor is { } lida) _arquivos = lida.Itens.ToDictionary(…);`:

```csharp
        if (resultado.Valor is { } lida)
            foreach (var documento in lida.Itens) _arquivos[documento.Id] = documento.NomeArquivoOriginal;
```

Run: `dotnet build -m:1`
Expected: build sem erro e sem aviso.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DocumentosDaAtividadeInterativosTests"`
Expected: todos passando.

- [ ] **Step 8: As telas que usam os documentos e a base pelos ganchos**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~Documentos|FullyQualifiedName~GanchosDaGestaoDeDocumentosTests|FullyQualifiedName~ClienteFicha|FullyQualifiedName~AtividadeFicha|FullyQualifiedName~ComponentesDoDesignSystemTests"`
Expected: todos passando. `GanchosDaGestaoDeDocumentosTests` continua sem mudança (a base não ganhou membro abstrato; o `Foco` vem do registro da Tarefa 3 na `TelaInterativa`). Os estáticos (`DocumentosDoPortalWebTests`, `DocumentosDaFichaTests`, `ClienteFichaWebTests`) passam porque o `id` veio depois do `class` e os painéis só existem depois de um clique.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Expected: todos passando (os `form` têm classe `aj-`; nenhum `style`).

Para a Tarefa 11 (só o navegador prova): o foco de volta chega ao "⋯" no tablet e no mobile e ao botão da linha no desktop; o Enter no "Nome (opcional)" e no "Novo nome" envia no Chrome e no Firefox; o aviso revelado rola até a vista no celular; durante o envio confirmado, o foco que estava no "Confirmar envio" (que fica desabilitado) não se perde de forma a atrapalhar (se cair no `body`, a rede de segurança da Tarefa 3 não age, porque o botão não saiu do DOM: anotar).

- [ ] **Step 9: Commit**

Pela ferramenta PowerShell, com a mensagem num arquivo UTF-8 sem BOM:

```
git branch --show-current
git add src/AtividadesJuridicas.Web/Components/Documentos src/AtividadesJuridicas.Web/Components/Atividades/DocumentosDaAtividade.razor tests/AtividadesJuridicas.IntegrationTests/DocumentosComunsInterativosTests.cs tests/AtividadesJuridicas.IntegrationTests/DocumentosDaFichaInterativosTests.cs tests/AtividadesJuridicas.IntegrationTests/DocumentosDaFichaTests.cs tests/AtividadesJuridicas.IntegrationTests/DocumentosDaAtividadeInterativosTests.cs
git commit -F <arquivo da mensagem>
```

Expected do primeiro comando: `worktree-feat+fase-9g-limpeza`.

Mensagem:

```
feat(web): documentos devolvem o foco a quem abriu, revelam o resultado e enviam pelo Enter

Cancelar o envio, o renomear, o acesso e as confirmações leva o foco de volta ao botão que abriu (o do desktop ou o
"⋯" do tablet e do mobile, pelo módulo de foco); concluir leva o foco ao aviso de resultado, que se revela. Com a
confirmação de envio à vista, "Enviar documento" vai a ela; durante o envio confirmado ela fica à vista e o campo de
arquivo fica desabilitado. "Salvar nome" não repete no clique duplo, a resposta de uma consulta superada é descartada,
"Tentar de novo" mostra "Carregando…", cada painel é um formulário que o Enter envia, e o acesso encerrado não deixa
mensagem ao lado.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

### Task 9: Confirmações e erros de formulário

**Files:**
- Modify: as telas com confirmação (`Aviso` com `Rotulo`): `Pages/AtividadeFicha.razor`, `ClienteFicha.razor`, `EventoFicha.razor`, `Usuarios.razor`, `Components/Usuarios/ConfirmacaoDeInativacao.razor` (e o formulário de usuário que a usa); os oito formulários com `Aviso` de erro de gravação (`UsuarioFormulario`, `ClienteFormulario`, `ProcessoFormulario`, `AtividadeFormulario`, `EventoFormulario`, `AtividadeResponsaveis`, `Atividades/MotivoComResponsaveis`, `MinhaSenha`); `Layout/CargaDoFormulario.razor`
- Test: os interativos dessas telas, os novos `UsuariosInterativosTests.cs`, `UsuarioFormularioInterativoTests.cs` e `MinhaSenhaInterativaTests.cs`, e a trava nova `tests/AtividadesJuridicas.UnitTests/ErrosDeGravacaoTests.cs` (todo formulário com `Aviso` de erro de gravação usa `Revelar` e a chave por mensagem)

**Interfaces:**
- Consumes: o serviço `Foco` e `Aviso.Revelar` (Tarefa 3).

**Regras:** X2 (Cancelar de uma confirmação devolve o foco ao botão da ação que a abriu; o verbo concluído leva o foco ao aviso de resultado, com `Revelar`, ou, se a tela navega, a navegação cuida do foco); C5 (o `Aviso` de erro de gravação dos oito formulários com `Revelar`); D2 (o erro da busca de cliente do formulário de processo vai junto do campo, `aj-campo-erro`, como o do formulário de usuário); F14 e X5 (`aria-invalid="true"` e `aria-describedby` para o `aj-campo-erro` nos grupos de participantes do evento, complexidade da atividade e responsáveis do devolver/reabrir, com `id` por visão); E4 (o "Tentar de novo" do `CargaDoFormulario` com guarda de clique duplo e estado de carregando).

**Testes obrigatórios:** bUnit: o foco pedido ao Cancelar e ao concluir em cada tela com confirmação; `Revelar` presente no aviso de erro de cada formulário (ao menos um teste de comportamento e um estático por formulário); os atributos dos grupos; o clique duplo no "Tentar de novo".

- [ ] **Step 1: "Tentar de novo" do `CargaDoFormulario` (E4) — teste e implementação**

Os testes desta tarefa provam o foco **pedido** de dois jeitos, com os auxiliares que a Tarefa 3 deixou na `TelaInterativa` (não crie outros, nem monte o módulo de foco com `SetupModule`): o pedido ao módulo de foco, pelo serviço `Foco` (`UltimoSeletorFocado()`, o seletor do último pedido a `focarPrimeiroVisivel`, e `SeletoresFocados`, todos, na ordem), e o pedido por `ElementReference.FocusAsync`, que o `Aviso` com `Revelar` faz a si mesmo ao aparecer (`UltimoFocoPedido()`, a última chamada, e `FocosDeElementoPedidos`, quantas houve). O `Foco` já está registrado na `TelaInterativa`, no `TelaDeTeste` e no `Program.cs`.

Acrescente ao fim de `tests/AtividadesJuridicas.IntegrationTests/CargaDoFormularioInterativaTests.cs`:

```csharp
    [Fact]
    public void Tentar_de_novo_mostra_carregando_ate_a_tela_terminar_e_nao_repete_no_segundo_clique()
    {
        var carga = new TaskCompletionSource();
        var tentativas = 0;
        var tela = Desenhar<CargaDoFormulario>(p => p
            .Add(c => c.Estado, EstadoDeCarga.Falhou)
            .Add(c => c.Erro, Erro)
            .Add(c => c.TextoNaoEncontrado, "Cliente não encontrado.")
            .Add(c => c.TentarDeNovo, () =>
            {
                tentativas++;
                return carga.Task;
            })
            .AddChildContent(Formulario));

        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        // A tela ainda diz "Falhou" (a carga dela não terminou): o componente mostra o carregando e some com o botão.
        tela.Markup.Should().Contain("Carregando…").And.NotContain(Erro);
        tela.FindAll("button").Should().NotContain(b => b.TextContent == "Tentar de novo");
        tentativas.Should().Be(1);

        carga.SetResult();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain(Erro));
        tela.Markup.Should().NotContain("Carregando…");
        tentativas.Should().Be(1);
    }
```

Run: `dotnet build -m:1`, depois `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~CargaDoFormularioInterativaTests"`
Expected: FAIL no teste novo — o erro continua à vista com o "Tentar de novo" enquanto a tela recarrega.

Em `src/AtividadesJuridicas.Web/Components/Layout/CargaDoFormulario.razor`:

- O primeiro ramo passa a `@if (Estado == EstadoDeCarga.Carregando || _tentando)`.
- O botão do ramo `Falhou` chama o método do componente: `<button type="button" class="aj-botao aj-botao--secundario" @onclick="TentarDeNovoAsync">Tentar de novo</button>`.
- No `@code`, depois dos parâmetros:

```csharp
    // "Tentar de novo" em curso: o erro dá lugar ao "Carregando…" até a tela terminar a carga, e um segundo clique que
    // chegue antes de o botão sumir no navegador não repete a carga.
    private bool _tentando;

    private async Task TentarDeNovoAsync()
    {
        if (_tentando) return;

        _tentando = true;
        try
        {
            await TentarDeNovo.InvokeAsync();
        }
        finally
        {
            _tentando = false;
        }
    }
```

O comentário do topo do arquivo ganha uma frase: "Durante o "Tentar de novo", o carregando, até a tela terminar a carga.".

Run: `dotnet build -m:1`, depois `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~CargaDoFormularioInterativaTests|FullyQualifiedName~FormularioInterativoTests|FullyQualifiedName~ResponsaveisInterativosTests|FullyQualifiedName~DevolucaoInterativaTests"`
Expected: todos passando (os "Tentar de novo" de hoje, com respostas na hora, terminam no mesmo clique).

O bUnit retira o tratador do botão assim que ele some; o segundo clique de um clique duplo real, que chega ao servidor antes de o navegador receber o desenho, só se reproduz no navegador (Tarefa 11). O teste prova o estado de carregando e que o botão não fica clicável.

Commit (PowerShell, mensagem num arquivo UTF-8 sem BOM; antes, `git branch --show-current` → `worktree-feat+fase-9g-limpeza`):

```
git add src/AtividadesJuridicas.Web/Components/Layout/CargaDoFormulario.razor tests/AtividadesJuridicas.IntegrationTests/CargaDoFormularioInterativaTests.cs
git commit -F <arquivo da mensagem>
```

```
fix(web): "Tentar de novo" dos formulários mostra o carregando e não repete no clique duplo

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

- [ ] **Step 2: Confirmações da ficha da atividade — testes (devem falhar)**

O foco de volta vai por seletor ao módulo de foco (o serviço `Foco` da Tarefa 3), não por `ElementReference`: cada botão que abre uma confirmação tem cópias por visão (a ação primária na faixa do desktop e do tablet e na barra fixa do mobile; o Excluir com texto no desktop, só com ícone no tablet, e no "Mais ações" do mobile), e só o navegador sabe qual está à vista. Cada cópia ganha um `id` próprio da visão; a do mobile que fica dentro do `MenuDeAcoes` (cujo botão não aceita `id`) é achada pelo `aria-label` exato do botão do menu, "Mais ações", que é um só na tela. O `aria-controls` não serve: as confirmações só existem abertas.

Acrescente ao fim de `tests/AtividadesJuridicas.IntegrationTests/AtividadeFichaInterativaTests.cs`:

```csharp
    [Theory]
    [InlineData("Concluir minha parte", "#concluir-minha-parte-faixa, #concluir-minha-parte-barra")]
    [InlineData("Aprovar", "#aprovar-faixa, #aprovar-barra")]
    [InlineData("Excluir", "#excluir-atividade-desktop, #excluir-atividade-tablet, .aj-menu-acoes > [aria-label=\"Mais ações\"]")]
    public void Cancelar_devolve_o_foco_ao_botao_que_abriu_a_confirmacao(string acao, string seletor)
    {
        var acoes = acao switch
        {
            "Concluir minha parte" => Acoes(concluir: true),
            "Aprovar" => Acoes(aprovar: true),
            _ => Acoes(editar: true, excluir: true),
        };
        Combinar(Detalhe(acoes: acoes));
        var ficha = Ficha();
        // Cada id do seletor existe uma vez (a cópia da faixa e a da barra fixa, ou a do desktop e a do tablet).
        foreach (var parte in seletor.Split(", ").Where(p => p.StartsWith('#')))
            ficha.FindAll(parte).Should().ContainSingle(parte);
        Clicar(ficha, NaFaixa, acao);

        Clicar(ficha, NaConfirmacao, "Cancelar");

        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be(seletor);
    }

    [Fact]
    public void O_mais_acoes_do_mobile_e_o_excluir_do_mobile_que_o_foco_de_volta_procura()
    {
        Combinar(Detalhe(acoes: Acoes(editar: true, excluir: true)));

        var ficha = FichaComBarra();

        ficha.FindAll(".aj-barra-superior .aj-menu-acoes > [aria-label=\"Mais ações\"]").Should().ContainSingle();
        ficha.Find("#excluir-atividade-desktop").TextContent.Should().Be("Excluir");
        ficha.Find("#excluir-atividade-tablet").GetAttribute("aria-label").Should().Be("Excluir atividade");
    }

    [Fact]
    public void Concluir_minha_parte_leva_o_foco_ao_aviso_de_resultado()
    {
        Combinar(Detalhe(StatusAtividade.Direcionada, Acoes(concluir: true)), ator: AtorDaProducao);
        Despachante.Responde<ConcluirMinhaParte, Resultado>(Resultado.Ok());
        var ficha = Ficha();
        Clicar(ficha, NaFaixa, "Concluir minha parte");

        Clicar(ficha, NaConfirmacao, "Concluir minha parte");

        ficha.WaitForAssertion(() => ficha.Find("p.aj-aviso--sucesso").TextContent.Should().Be("Sua parte foi concluída."));
        var aviso = ficha.Find("p.aj-aviso--sucesso");
        aviso.GetAttribute("tabindex").Should().Be("-1");
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }
```

Run: `dotnet build -m:1`, depois `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadeFichaInterativaTests"`
Expected: FAIL nos quatro casos novos (os ids não existem; nada é pedido ao módulo de foco; o aviso não tem `tabindex`).

- [ ] **Step 3: Confirmações da ficha da atividade — implementação**

Em `src/AtividadesJuridicas.Web/Components/Pages/AtividadeFicha.razor`:

1. `@inject Foco Foco` depois dos outros `@inject`.
2. Os dois avisos de `_mensagem` (linhas 16 e 50) passam a `<Aviso @key="_versaoDoAviso" Tipo="@(_falhou ? TipoDeAviso.Erro : TipoDeAviso.Sucesso)" Revelar="true">@_mensagem</Aviso>`.
3. A barra fixa do mobile (linha 125): `<div class="aj-acoes aj-acoes--fixa">@BotaoDevolver(a.Acoes)@AcaoPrimaria(a.Acoes, "barra")</div>`.
4. `AcaoPrimaria` recebe a visão e dá um `id` por cópia (depois do `class`, como os testes estáticos casam):

```razor
    /// <summary>
    /// A ação primária: Concluir minha parte, ou Aprovar, ou Reabrir (as regras do servidor não deixam duas valerem juntas).
    /// Aparece na faixa ("faixa") e na barra fixa do mobile ("barra"); o id de cada cópia leva a visão, para o foco voltar
    /// à que está à vista quando a confirmação é cancelada.
    /// </summary>
    private RenderFragment AcaoPrimaria(AcoesDaAtividade acoes, string visao) => @<text>@if (acoes.ConcluirMinhaParte)
        {<button type="button" class="aj-botao aj-botao--primario aj-botao--com-icone" id="@($"concluir-minha-parte-{visao}")" disabled="@_ocupado" @onclick="PedirConclusao"><Icone Nome="confirmar" />Concluir minha parte</button>}
        else if (acoes.AprovarOuDevolver)
        {<button type="button" class="aj-botao aj-botao--primario aj-botao--com-icone" id="@($"aprovar-{visao}")" disabled="@_ocupado" @onclick="PedirAprovacao"><Icone Nome="confirmar" />Aprovar</button>}
        else if (acoes.Reabrir)
        {<a href="@Rota("reabrir")" class="aj-botao aj-botao--secundario">Reabrir</a>}</text>;
```

5. Em `FaixaDeAcoes`, `@AcaoPrimaria(acoes, "faixa")`, e os dois Excluir com o `id` da visão:

```razor
        @if (acoes.Excluir)
        {<span class="aj-so-desktop"><button type="button" class="aj-botao aj-botao--perigo aj-botao--com-icone-menor aj-botao--ao-fim" id="excluir-atividade-desktop" disabled="@_ocupado" @onclick="PedirExclusao"><Icone Nome="excluir" />Excluir</button></span><span class="aj-so-tablet"><button type="button" class="aj-botao aj-botao--perigo aj-botao--icone aj-botao--ao-fim" id="excluir-atividade-tablet" aria-label="Excluir atividade" disabled="@_ocupado" @onclick="PedirExclusao"><Icone Nome="excluir" /></button></span>}</text>;
```

6. No `@code`, os campos e o foco de volta:

```csharp
    // A geração do aviso de resultado: cada mensagem nova recria o Aviso (a chave muda), que se revela de novo.
    private int _versaoDoAviso;

    // O botão que abriu a confirmação cancelada: o foco volta a ele depois do desenho.
    private string? _focoDeVolta;

    // O botão que abre cada confirmação, com as suas cópias por visão; o módulo de foco leva o foco à que está à vista.
    private static string? QuemAbriu(Confirmacao confirmacao) => confirmacao switch
    {
        Confirmacao.Concluir => "#concluir-minha-parte-faixa, #concluir-minha-parte-barra",
        Confirmacao.Aprovar => "#aprovar-faixa, #aprovar-barra",
        Confirmacao.Excluir => "#excluir-atividade-desktop, #excluir-atividade-tablet, .aj-menu-acoes > [aria-label=\"Mais ações\"]",
        _ => null,
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focoDeVolta is not { } seletor) return;

        _focoDeVolta = null;
        // O Foco engole as falhas do JavaScript: sem foco (circuito caindo, por exemplo) a ficha continua funcionando.
        await Foco.FocarPrimeiroVisivelAsync(seletor);
    }
```

7. `CancelarConfirmacao` e `Avisar`:

```csharp
    private void CancelarConfirmacao()
    {
        if (_ocupado) return;

        _focoDeVolta = QuemAbriu(_confirmando);
        _confirmando = Confirmacao.Nenhuma;
    }

    private void Avisar(string mensagem, bool falhou)
    {
        _mensagem = mensagem;
        _falhou = falhou;
        _versaoDoAviso++;
    }
```

Concluir e Aprovar terminam em `Avisar` (o aviso se revela); a exclusão que dá certo navega para a lista, e a navegação cuida do foco (o `FocoNoTitulo`); a recusa da exclusão termina em `Avisar` e se revela.

Run: `dotnet build -m:1`, depois `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadeFicha"`
Expected: todos passando (os interativos e `AtividadeFichaWebTests`, que casam `class="…"[^>]*>` nos botões).

- [ ] **Step 4: Confirmação da ficha do cliente — teste e implementação**

Acrescente ao fim de `tests/AtividadesJuridicas.IntegrationTests/ClienteFichaInterativaTests.cs`:

```csharp
    private const string Inativar = "#ativar-ou-inativar-desktop, #ativar-ou-inativar-barra";

    [Fact]
    public void Cancelar_a_inativacao_devolve_o_foco_ao_inativar()
    {
        Combinar();
        var ficha = Ficha();
        ficha.Find("#ativar-ou-inativar-desktop").TextContent.Should().Be("Inativar");
        ficha.Find("#ativar-ou-inativar-barra").TextContent.Should().Be("Inativar");
        ficha.Find("#ativar-ou-inativar-desktop").Click();

        ficha.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Cancelar").Click();

        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be(Inativar);
    }

    [Fact]
    public void Confirmar_a_inativacao_leva_o_foco_ao_aviso_de_resultado()
    {
        Combinar(usuariosAtivos: 2);
        Despachante.Responde<DefinirAtivoCliente, Resultado>(Resultado.Ok());
        var ficha = Ficha();
        ficha.Find("#ativar-ou-inativar-barra").Click();

        ficha.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar inativação").Click();

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Cliente inativado. 2 usuários do portal ficaram sem acesso."));
        var aviso = ficha.Find("p.aj-aviso--sucesso");
        aviso.GetAttribute("tabindex").Should().Be("-1");
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }
```

Run: `dotnet build -m:1`, depois `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ClienteFichaInterativaTests"`
Expected: FAIL nos dois novos (sem os ids).

Em `src/AtividadesJuridicas.Web/Components/Pages/ClienteFicha.razor`:

1. `@inject Foco Foco` depois dos outros `@inject`.
2. O aviso de `_mensagem` (linha 40): `<Aviso @key="_versaoDoAviso" Tipo="@(_falhou ? TipoDeAviso.Erro : TipoDeAviso.Sucesso)" Revelar="true">@_mensagem</Aviso>`.
3. O "Cancelar" da confirmação (linha 46): `@onclick="CancelarInativacao"` no lugar da lambda.
4. Os dois botões "Inativar"/"Ativar" ganham o `id` da visão, depois do `class`: na barra fixa do tablet e do mobile (linha 167) `id="ativar-ou-inativar-barra"`, e em `AcoesDoDesktop` `id="ativar-ou-inativar-desktop"`.
5. No `@code`:

```csharp
    // A geração do aviso de resultado: cada mensagem nova recria o Aviso (a chave muda), que se revela de novo.
    private int _versaoDoAviso;

    // O "Inativar" que abriu a confirmação cancelada: o foco volta à cópia que está à vista.
    private string? _focoDeVolta;

    private const string AtivarOuInativar = "#ativar-ou-inativar-desktop, #ativar-ou-inativar-barra";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focoDeVolta is not { } seletor) return;

        _focoDeVolta = null;
        // O Foco engole as falhas do JavaScript: sem foco (circuito caindo, por exemplo) a ficha continua funcionando.
        await Foco.FocarPrimeiroVisivelAsync(seletor);
    }

    private void CancelarInativacao()
    {
        _confirmandoInativacao = false;
        _focoDeVolta = AtivarOuInativar;
    }

    private void Avisar(string mensagem, bool falhou)
    {
        _mensagem = mensagem;
        _falhou = falhou;
        _versaoDoAviso++;
    }
```

6. Em `AlternarAtivoAsync` e em `ConfirmarInativacaoAsync`, o par `_falhou = !resultado.Sucesso;` + atribuições de `_mensagem` passa a `Avisar`:

```csharp
            if (resultado.Sucesso)
            {
                _cliente = _cliente with { Ativo = true };
                Avisar("Cliente ativado.", falhou: false);
            }
            else
            {
                Avisar(resultado.Mensagem ?? "Não foi possível ativar o cliente.", falhou: true);
            }
```

```csharp
            _confirmandoInativacao = false;
            if (resultado.Sucesso)
            {
                var afetados = UsuariosAtivos ?? 0;
                _cliente = _cliente with { Ativo = false };
                Avisar(TextosDoCliente.Inativado(afetados), falhou: false);
            }
            else
            {
                Avisar(resultado.Mensagem ?? "Não foi possível inativar o cliente.", falhou: true);
            }
```

Run: `dotnet build -m:1`, depois `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ClienteFicha"`
Expected: todos passando.

- [ ] **Step 5: Confirmação da ficha do evento — teste e implementação**

Acrescente ao fim de `tests/AtividadesJuridicas.IntegrationTests/EventoFichaInterativaTests.cs`:

```csharp
    [Fact]
    public void Cancelar_a_exclusao_devolve_o_foco_ao_excluir()
    {
        Combinar();
        var ficha = Ficha();
        ficha.Find("#excluir-evento-desktop").TextContent.Should().Be("Excluir");
        ficha.Find("#excluir-evento-barra").TextContent.Should().Be("Excluir");
        Clicar(ficha, ExcluirNaBarra, "Excluir");

        Clicar(ficha, NaConfirmacao, "Cancelar");

        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be("#excluir-evento-desktop, #excluir-evento-barra");
    }

    [Fact]
    public void Exclusao_recusada_leva_o_foco_ao_aviso_de_erro()
    {
        Combinar();
        Despachante.Responde<ExcluirEvento, Resultado>(Resultado.Falha(TipoErro.Conflito,
            "O evento foi alterado por outra pessoa. Recarregue e tente novamente."));
        var ficha = Ficha();
        Clicar(ficha, ExcluirNoCabecalho, "Excluir");

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        ficha.WaitForAssertion(() => ficha.Find("[role=alert]").TextContent.Should()
            .Contain("O evento foi alterado por outra pessoa. Recarregue e tente novamente."));
        var aviso = ficha.Find("[role=alert]");
        aviso.GetAttribute("tabindex").Should().Be("-1");
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }
```

Run: `dotnet build -m:1`, depois `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~EventoFichaInterativaTests"`
Expected: FAIL nos dois novos.

Em `src/AtividadesJuridicas.Web/Components/Pages/EventoFicha.razor`:

1. `@inject Foco Foco`.
2. Os dois avisos de `_mensagem` (linhas 15 e 49): `<Aviso @key="_versaoDoAviso" Tipo="TipoDeAviso.Erro" Revelar="true">@_mensagem</Aviso>`.
3. Os dois Excluir ganham o `id`, depois do `class`: em `AcoesDoDesktop` `id="excluir-evento-desktop"`, em `BarraFixa` `id="excluir-evento-barra"`.
4. No `@code`: os campos `_versaoDoAviso` e `_focoDeVolta` e o `OnAfterRenderAsync` iguais aos da ficha do cliente (Step 4, item 5, com o comentário "a ficha continua funcionando"), e o `CancelarExclusao` de hoje (`if (!_ocupado) _confirmandoExclusao = false;`) passa a:

```csharp
    private void CancelarExclusao()
    {
        if (_ocupado) return;

        _confirmandoExclusao = false;
        _focoDeVolta = "#excluir-evento-desktop, #excluir-evento-barra";
    }
```

5. Em `ExcluirAsync`, `_mensagem = resultado.Mensagem ?? "Não foi possível excluir o evento.";` passa a

```csharp
                _mensagem = resultado.Mensagem ?? "Não foi possível excluir o evento.";
                _versaoDoAviso++;
```

Run: `dotnet build -m:1`, depois `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~EventoFicha"`
Expected: todos passando.

- [ ] **Step 6: Confirmação de inativação na lista de usuários — teste e implementação**

Crie `tests/AtividadesJuridicas.IntegrationTests/UsuariosInterativosTests.cs` (UTF-8 sem BOM, CRLF; o arquivo não existe hoje, e a Tarefa 10 acrescenta a ele):

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PaginaDeUsuarios = AtividadesJuridicas.Web.Components.Pages.Usuarios;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>A lista de usuários, clicando: a confirmação de inativação, o foco ao fechá-la e o aviso de resultado.</summary>
public class UsuariosInterativosTests : TelaInterativa
{
    private static readonly Guid Pedro = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d102");
    private static readonly Guid Producao = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000e102");

    private static readonly UsuarioResumo DoPedro =
        new(Pedro, "Pedro Produção", "pedro@x.com", Producao, "Produção", TipoPerfil.Producao, null, true, null);

    private IRenderedComponent<PaginaDeUsuarios> Tela()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(Resultado<IReadOnlyList<PerfilDetalhe>>.Ok(
                [new PerfilDetalhe(Producao, "Produção", TipoPerfil.Producao, true, 1)]))
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([DoPedro]))
            .Responde<ObterPendenciasDoUsuario, Resultado<PendenciasDoUsuario>>(
                Resultado<PendenciasDoUsuario>.Ok(new PendenciasDoUsuario(0, 0, 0)));
        return Desenhar<PaginaDeUsuarios>();
    }

    [Fact]
    public void Cancelar_a_inativacao_devolve_o_foco_ao_inativar_da_linha()
    {
        var tela = Tela();
        var inativar = tela.Find($"#ativar-ou-inativar-{Pedro:N}");
        inativar.TextContent.Should().Be("Inativar");
        inativar.Click();
        tela.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar inativação");

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Cancelar").Click();

        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be($"#ativar-ou-inativar-{Pedro:N}");
    }

    [Fact]
    public void Confirmar_a_inativacao_leva_o_foco_ao_aviso_de_resultado()
    {
        var tela = Tela();
        Despachante.Responde<DefinirAtivoUsuario, Resultado>(Resultado.Ok());
        tela.Find($"#ativar-ou-inativar-{Pedro:N}").Click();

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar inativação").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Usuário inativado."));
        var aviso = tela.Find("p.aj-aviso--sucesso");
        aviso.GetAttribute("tabindex").Should().Be("-1");
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }

    [Fact]
    public void Recado_de_outra_tela_aparece_sem_levar_o_foco()
    {
        // Trava: o recado chega com a navegação, e quem leva o foco ao título é o FocoNoTitulo, não o aviso.
        Services.GetRequiredService<Recados>().Deixar("Usuário salvo.");

        var tela = Tela();

        tela.Find("p.aj-aviso--sucesso").TextContent.Should().Be("Usuário salvo.");
        tela.Find("p.aj-aviso--sucesso").HasAttribute("tabindex").Should().BeFalse();
        FocosDeElementoPedidos.Should().Be(0);
    }
}
```

`Recado_de_outra_tela_aparece_sem_levar_o_foco` passa também antes da mudança: é a trava contra revelar o recado (que competiria com o foco no título depois da navegação); os outros dois falham antes (sem o `id`).

Run: `dotnet build -m:1`, depois `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~UsuariosInterativosTests"`
Expected: FAIL nos dois primeiros.

Em `src/AtividadesJuridicas.Web/Components/Pages/Usuarios.razor`:

1. `@inject Foco Foco`.
2. O aviso (linha 22): `<Aviso @key="_versaoDoAviso" Tipo="@(_falhou ? TipoDeAviso.Erro : TipoDeAviso.Sucesso)" Revelar="@(_versaoDoAviso > 0)">@_mensagem</Aviso>`. O recado vindo de outra tela (lido em `OnInitializedAsync`) não muda a geração e não se revela; a mensagem de uma ação desta tela sim.
3. O botão da linha (linha 48) ganha o `id`, depois do `class`: `<button type="button" class="aj-botao aj-botao--fantasma aj-botao--menor" id="@($"ativar-ou-inativar-{usuario.Id:N}")" disabled="@_emAndamento" @onclick="() => AlternarAtivoAsync(usuario)">`.
4. No `@code`: `_versaoDoAviso`, `_focoDeVolta` e o `OnAfterRenderAsync` iguais aos da ficha do cliente (Step 4, item 5, com o comentário "a lista continua funcionando"), e o `CancelarInativacao` de hoje passa a:

```csharp
    private void CancelarInativacao()
    {
        if (_inativando is { } alvo) _focoDeVolta = $"#ativar-ou-inativar-{alvo.Id:N}";
        _inativando = null;
        _mensagem = null;
        _falhou = false;
    }
```

5. Em `ConcluirAsync`, cada mensagem nova muda a geração:

```csharp
        _falhou = !resultado.Sucesso;
        _mensagem = resultado.Sucesso ? mensagemDeSucesso : resultado.Mensagem;
        _versaoDoAviso++;

        // A ação deu certo; se só a recarga falhou, o aviso diz isso e a lista anterior fica na tela.
        if (!await RecarregarAsync() && _autorizado && resultado.Sucesso)
        {
            _falhou = true;
            _mensagem = $"{mensagemDeSucesso.TrimEnd('.')}, mas não foi possível atualizar a lista.";
            _versaoDoAviso++;
        }
```

`ConfirmacaoDeInativacao.razor` não muda: o "Cancelar" e o verbo dela só avisam a tela (`OnCancelar`, `OnConcluido`), e é a tela que sabe de onde a confirmação foi aberta.

Run: `dotnet build -m:1`, depois `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~UsuariosInterativosTests|FullyQualifiedName~UsuariosWebTests"`
Expected: todos passando.

- [ ] **Step 7: Commit das confirmações**

```
git branch --show-current
git add src/AtividadesJuridicas.Web/Components/Pages/AtividadeFicha.razor src/AtividadesJuridicas.Web/Components/Pages/ClienteFicha.razor src/AtividadesJuridicas.Web/Components/Pages/EventoFicha.razor src/AtividadesJuridicas.Web/Components/Pages/Usuarios.razor tests/AtividadesJuridicas.IntegrationTests/AtividadeFichaInterativaTests.cs tests/AtividadesJuridicas.IntegrationTests/ClienteFichaInterativaTests.cs tests/AtividadesJuridicas.IntegrationTests/EventoFichaInterativaTests.cs tests/AtividadesJuridicas.IntegrationTests/UsuariosInterativosTests.cs
git commit -F <arquivo da mensagem>
```

```
feat(web): confirmações devolvem o foco ao botão que as abriu e o resultado se revela

Cancelar a conclusão, a aprovação e a exclusão da atividade, a inativação do cliente e do usuário e a exclusão do
evento leva o foco de volta à cópia visível do botão que abriu a confirmação (um id por visão, pelo módulo de foco);
o aviso de resultado das ações que ficam na tela se revela, com uma chave nova a cada mensagem.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

- [ ] **Step 8: Formulário de usuário — testes (devem falhar)**

Crie `tests/AtividadesJuridicas.IntegrationTests/UsuarioFormularioInterativoTests.cs` (UTF-8 sem BOM, CRLF; o arquivo não existe hoje, e a Tarefa 10 acrescenta a ele):

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Components.Pages;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>O formulário de usuário em edição, clicando: a confirmação de inativação e o erro de gravação.</summary>
public class UsuarioFormularioInterativoTests : TelaInterativa
{
    private static readonly Guid Pedro = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d102");
    private static readonly Guid Producao = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000e102");

    private IRenderedComponent<UsuarioFormulario> Edicao()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(Resultado<IReadOnlyList<PerfilDetalhe>>.Ok(
                [new PerfilDetalhe(Producao, "Produção", TipoPerfil.Producao, true, 1)]))
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok(
                [new UsuarioResumo(Pedro, "Pedro Produção", "pedro@x.com", Producao, "Produção", TipoPerfil.Producao, null, true, null)]))
            .Responde<ObterPendenciasDoUsuario, Resultado<PendenciasDoUsuario>>(
                Resultado<PendenciasDoUsuario>.Ok(new PendenciasDoUsuario(0, 0, 0)));
        return Desenhar<UsuarioFormulario>(p => p.Add(f => f.Id, (Guid?)Pedro));
    }

    [Fact]
    public void Cancelar_a_inativacao_devolve_o_foco_ao_inativar_usuario()
    {
        var tela = Edicao();
        var inativar = tela.Find("#ativar-ou-inativar-usuario");
        inativar.TextContent.Should().Be("Inativar usuário");
        inativar.Click();
        tela.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar inativação");

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Cancelar").Click();

        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
        UltimoSeletorFocado().Should().Be("#ativar-ou-inativar-usuario");
    }

    [Fact]
    public void Falha_ao_salvar_leva_o_foco_ao_aviso_de_erro()
    {
        var tela = Edicao();
        Despachante.Responde<EditarUsuario, Resultado>(Resultado.Falha(TipoErro.Validacao, "E-mail já cadastrado."));

        tela.Find("form#formulario-do-usuario").Submit();

        tela.WaitForAssertion(() => tela.Find("p.aj-aviso--erro").TextContent.Should().Be("E-mail já cadastrado."));
        var aviso = tela.Find("p.aj-aviso--erro");
        aviso.GetAttribute("tabindex").Should().Be("-1");
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }
}
```

Run: `dotnet build -m:1`, depois `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~UsuarioFormularioInterativoTests"`
Expected: FAIL nos dois (sem o `id`; o aviso sem `tabindex` e sem foco).

- [ ] **Step 9: Erro de gravação dos oito formulários — trava estática e testes de comportamento (devem falhar)**

Os oito formulários com `Aviso` de erro de gravação são estes (conferido: são oito, e cada um tem um só aviso de erro sobre `_mensagem`): `Pages/UsuarioFormulario.razor`, `Pages/ClienteFormulario.razor`, `Pages/ProcessoFormulario.razor`, `Pages/AtividadeFormulario.razor`, `Pages/EventoFormulario.razor`, `Pages/AtividadeResponsaveis.razor`, `Atividades/MotivoComResponsaveis.razor` (devolver e reabrir) e `Pages/MinhaSenha.razor`.

Crie `tests/AtividadesJuridicas.UnitTests/ErrosDeGravacaoTests.cs`:

```csharp
using System.Text.RegularExpressions;

namespace AtividadesJuridicas.UnitTests;

/// <summary>
/// O erro de gravação dos formulários se revela (foco e rolagem até ele): num formulário mais alto que a tela do celular,
/// o aviso do topo ficaria fora da vista. O <c>Aviso</c> só se revela ao aparecer; por isso cada mensagem nova passa pelo
/// <c>MostrarErro</c>, que troca a chave do aviso, e o mesmo erro duas vezes seguidas se revela das duas.
/// </summary>
public class ErrosDeGravacaoTests
{
    // Caminhos a partir de src/AtividadesJuridicas.Web.
    private static readonly string[] Formularios =
    [
        "Components/Pages/UsuarioFormulario.razor",
        "Components/Pages/ClienteFormulario.razor",
        "Components/Pages/ProcessoFormulario.razor",
        "Components/Pages/AtividadeFormulario.razor",
        "Components/Pages/EventoFormulario.razor",
        "Components/Pages/AtividadeResponsaveis.razor",
        "Components/Atividades/MotivoComResponsaveis.razor",
        "Components/Pages/MinhaSenha.razor",
    ];

    public static TheoryData<string> Arquivos => new(Formularios);

    [Theory]
    [MemberData(nameof(Arquivos))]
    public void O_erro_de_gravacao_se_revela_e_troca_de_chave_a_cada_mensagem(string arquivo)
    {
        var codigo = File.ReadAllText(Path.Combine(
            [GravacoesPorForaTests.RaizDoRepositorio(), "src", "AtividadesJuridicas.Web", .. arquivo.Split('/')]));

        codigo.Should().Contain("<Aviso @key=\"_versaoDoErro\" Tipo=\"TipoDeAviso.Erro\" Revelar=\"true\">@_mensagem</Aviso>");
        // Uma só atribuição de texto ao _mensagem, a do MostrarErro, que também troca a chave; as outras são "= null".
        Regex.Matches(codigo, @"_mensagem\s*=(?!=)\s*(?!null\s*;)").Should().ContainSingle();
        codigo.Should().Contain("_mensagem = mensagem;").And.Contain("_versaoDoErro++;");
    }
}
```

Acrescente um teste de comportamento a cada um dos sete arquivos interativos que já existem (o do formulário de usuário está no Step 8). Cada um faz a gravação falhar e confere o foco pedido ao aviso de erro; o do cliente confere também dois erros iguais seguidos (a mesma frase, sem desenho no meio, só se revela de novo porque a chave muda):

Em `ClienteFormularioInterativoTests.cs`:

```csharp
    private IRenderedComponent<ClienteFormulario> ComErroAoSalvar()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<SalvarClientePessoaFisica, Resultado<Guid>>(Resultado<Guid>.Falha(TipoErro.Validacao, "CPF já cadastrado."));
        var tela = Desenhar<ClienteFormulario>();
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.StartsWith("Nome completo")).QuerySelector("input")!.Change("Helena Barros Lima");
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.StartsWith("CPF")).QuerySelector("input")!.Change("529.982.247-25");
        return tela;
    }

    [Fact]
    public void Falha_ao_salvar_leva_o_foco_ao_aviso_de_erro()
    {
        var tela = ComErroAoSalvar();

        tela.Find("form#formulario-do-cliente").Submit();

        tela.WaitForAssertion(() => tela.Find("p.aj-aviso--erro").TextContent.Should().Be("CPF já cadastrado."));
        var aviso = tela.Find("p.aj-aviso--erro");
        aviso.GetAttribute("tabindex").Should().Be("-1");
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
    }

    [Fact]
    public void Dois_erros_iguais_seguidos_revelam_o_aviso_a_cada_vez()
    {
        var tela = ComErroAoSalvar();
        tela.Find("form#formulario-do-cliente").Submit();
        tela.WaitForAssertion(() => tela.Markup.Should().Contain("CPF já cadastrado."));
        var antes = FocosDeElementoPedidos;

        tela.Find("form#formulario-do-cliente").Submit();

        tela.WaitForAssertion(() => Despachante.Recebidas.OfType<SalvarClientePessoaFisica>().Should().HaveCount(2));
        FocosDeElementoPedidos.Should().Be(antes + 1);
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(tela.Find("p.aj-aviso--erro"));
    }
```

Em `ProcessoFormularioInterativoTests.cs`:

```csharp
    [Fact]
    public void Falha_ao_salvar_leva_o_foco_ao_aviso_de_erro()
    {
        var tela = Formulario();
        Despachante.Responde<SalvarProcesso, Resultado<Guid>>(Resultado<Guid>.Falha(TipoErro.Validacao, "Número CNJ já cadastrado."));
        Digitar(tela, "Título", "Litoral × Seguradora — indenização");
        tela.Find("select.aj-campo").Change(Area.ToString());

        tela.Find("form#formulario-do-processo").Submit();

        tela.WaitForAssertion(() => tela.Find("p.aj-aviso--erro").TextContent.Should().Be("Número CNJ já cadastrado."));
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(tela.Find("p.aj-aviso--erro"));
    }
```

Em `AtividadeFormularioInterativoTests.cs`:

```csharp
    [Fact]
    public void Falha_ao_salvar_leva_o_foco_ao_aviso_de_erro()
    {
        var tela = Edicao();
        Despachante.Responde<SalvarAtividade, Resultado<Guid>>(Resultado<Guid>.Falha(
            TipoErro.Conflito, "A atividade foi alterada por outra pessoa. Recarregue e tente novamente."));

        tela.Find("form#formulario-da-atividade").Submit();

        tela.WaitForAssertion(() => tela.Find("p.aj-aviso--erro").TextContent.Should().Contain("A atividade foi alterada por outra pessoa."));
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(tela.Find("p.aj-aviso--erro"));
    }
```

Em `EventoFormularioInterativoTests.cs` (o aviso de data e hora vem da validação da própria tela, sem consulta: o caso em que o mesmo erro se repete sem desenho no meio):

```csharp
    [Fact]
    public void Aviso_de_data_e_hora_se_revela_a_cada_envio()
    {
        var tela = Novo();
        Digitar(tela, "Título", "Reunião de alinhamento");
        Rotulo(tela, "Data").QuerySelector("input")!.Change("");

        Enviar(tela);

        var aviso = tela.Find("p.aj-aviso--erro");
        aviso.TextContent.Should().Be("Informe a data e a hora do evento.");
        aviso.GetAttribute("tabindex").Should().Be("-1");
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
        var antes = FocosDeElementoPedidos;

        Enviar(tela);

        FocosDeElementoPedidos.Should().Be(antes + 1);
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(tela.Find("p.aj-aviso--erro"));
        Despachante.Recebidas.OfType<SalvarEvento>().Should().BeEmpty();
    }
```

Em `AtividadeResponsaveisInterativosTests.cs`:

```csharp
    [Fact]
    public void Falha_ao_salvar_leva_o_foco_ao_aviso_de_erro()
    {
        var tela = Tela(Completa);
        Despachante.Responde<DefinirResponsaveisDaAtividade, Resultado>(
            Resultado.Falha(TipoErro.Conflito, "A atividade foi alterada por outra pessoa. Recarregue e tente novamente."));

        tela.Find("form#formulario-de-responsaveis").Submit();

        tela.WaitForAssertion(() => tela.Find("p.aj-aviso--erro").TextContent.Should().Contain("A atividade foi alterada por outra pessoa."));
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(tela.Find("p.aj-aviso--erro"));
    }
```

Em `AtividadeDevolucaoInterativaTests.cs`:

```csharp
    [Fact]
    public void Falha_ao_devolver_leva_o_foco_ao_aviso_de_erro()
    {
        var tela = Devolver();
        Despachante.Responde<DevolverAtividade, Resultado>(
            Resultado.Falha(TipoErro.Validacao, "Somente o revisor da atividade pode aprová-la ou devolvê-la."));
        Escrever(tela, "Rever o valor da terceira parcela.");

        tela.Find("form#formulario-do-motivo").Submit();

        tela.WaitForAssertion(() => tela.Find("p.aj-aviso--erro").TextContent.Should().Contain("Somente o revisor"));
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(tela.Find("p.aj-aviso--erro"));
    }
```

E crie `tests/AtividadesJuridicas.IntegrationTests/MinhaSenhaInterativaTests.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Web.Components.Pages;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>Minha senha, clicando: a senha atual recusada mostra o erro e leva o foco a ele, a cada tentativa.</summary>
public class MinhaSenhaInterativaTests : TelaInterativa
{
    private static void Digitar(IRenderedComponent<MinhaSenha> tela, string rotulo, string valor) =>
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.TrimStart().StartsWith(rotulo)).QuerySelector("input")!.Change(valor);

    [Fact]
    public void Senha_atual_recusada_leva_o_foco_ao_aviso_de_erro_a_cada_tentativa()
    {
        Despachante.Responde<AlterarMinhaSenha, Resultado>(Resultado.Falha(TipoErro.Validacao, "Senha atual incorreta."));
        var tela = Desenhar<MinhaSenha>();
        Digitar(tela, "Senha atual", "senha-errada");
        Digitar(tela, "Nova senha", "nova-senha-123");
        Digitar(tela, "Confirme a nova senha", "nova-senha-123");

        tela.Find("form.aj-painel").Submit();

        var aviso = tela.Find("p.aj-aviso--erro");
        aviso.TextContent.Should().Be("Senha atual incorreta.");
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(aviso);
        var antes = FocosDeElementoPedidos;

        tela.Find("form.aj-painel").Submit();

        Despachante.Recebidas.OfType<AlterarMinhaSenha>().Should().HaveCount(2);
        FocosDeElementoPedidos.Should().Be(antes + 1);
        UltimoFocoPedido().Arguments[0].ShouldBeElementReferenceTo(tela.Find("p.aj-aviso--erro"));
    }
}
```

Run: `dotnet build -m:1`
Expected: build sem erro.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~ErrosDeGravacaoTests"`
Expected: FAIL nos oito arquivos.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~FormularioInterativoTests|FullyQualifiedName~ResponsaveisInterativosTests|FullyQualifiedName~DevolucaoInterativaTests|FullyQualifiedName~MinhaSenhaInterativaTests"`
Expected: FAIL nos testes novos (o aviso sem `tabindex` e sem pedido de foco).

- [ ] **Step 10: Erro de gravação dos oito formulários — implementação (C5)**

Em cada um dos oito arquivos:

1. O aviso de erro passa a exatamente `<Aviso @key="_versaoDoErro" Tipo="TipoDeAviso.Erro" Revelar="true">@_mensagem</Aviso>` (o `@if (_mensagem is not null)` em volta fica).
2. No `@code`, depois do campo `_mensagem`:

```csharp
    // A geração do aviso de erro: cada mensagem nova recria o Aviso (a chave muda), que se revela de novo, mesmo que a
    // frase seja a mesma da tentativa anterior.
    private int _versaoDoErro;

    private void MostrarErro(string? mensagem)
    {
        _mensagem = mensagem;
        _versaoDoErro++;
    }
```

3. Cada atribuição de texto ao `_mensagem` passa a `MostrarErro(…)`, com a mesma expressão; as atribuições `_mensagem = null` ficam. Os lugares, na `main` de hoje:
   - `UsuarioFormulario.razor`: linhas 389 e 426 (`else _mensagem = resultado.Mensagem;` → `else MostrarErro(resultado.Mensagem);`) e 449 (em `AoInativar`).
   - `ClienteFormulario.razor`: linha 191.
   - `ProcessoFormulario.razor`: linha 230 (a 191, da busca de cliente, muda no Step 12).
   - `AtividadeFormulario.razor`: linhas 292, 304, 343 (as buscas e a leitura do processo, que também mostram o erro no topo) e 393.
   - `EventoFormulario.razor`: linhas 539, 573, 607 (as buscas), 663 e 665 (a validação de data e hora) e 693.
   - `AtividadeResponsaveis.razor`: linha 269.
   - `MotivoComResponsaveis.razor`: linha 181.
   - `MinhaSenha.razor`: linha 56.

O `if (… || _mensagem is not null) return;` do `EventoFormulario` (linha 667) continua valendo: o `MostrarErro` escreve o mesmo campo.

Run: `dotnet build -m:1`
Expected: build sem erro e sem aviso.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~ErrosDeGravacaoTests|FullyQualifiedName~TelasNoDesignSystemTests"`
Expected: todos passando, salvo `ErrosDeGravacaoTests` para `ProcessoFormulario.razor` (a atribuição da busca de cliente ao `_mensagem` sai no Step 12).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~FormularioInterativoTests|FullyQualifiedName~ResponsaveisInterativosTests|FullyQualifiedName~DevolucaoInterativaTests|FullyQualifiedName~MinhaSenhaInterativaTests"`
Expected: FAIL só em `UsuarioFormularioInterativoTests.Cancelar_a_inativacao_devolve_o_foco_ao_inativar_usuario` (o `id` vem no Step 11); os outros passando.

- [ ] **Step 11: Confirmação de inativação no formulário de usuário**

Em `src/AtividadesJuridicas.Web/Components/Pages/UsuarioFormulario.razor`:

1. `@inject Foco Foco`.
2. O `OnCancelar` da `ConfirmacaoDeInativacao` (linha 22) passa a `OnCancelar="CancelarInativacao"`.
3. O "Inativar usuário"/"Ativar usuário" do painel Situação (linha 125, uma cópia só, no tablet e no mobile) ganha `id="ativar-ou-inativar-usuario"` depois do `class`.
4. No `@code`: `_focoDeVolta` e o `OnAfterRenderAsync` iguais aos da ficha do cliente (Step 4, item 5, com o comentário "o formulário continua funcionando"), e:

```csharp
    private void CancelarInativacao()
    {
        _inativando = false;
        _focoDeVolta = "#ativar-ou-inativar-usuario";
    }
```

A inativação que dá certo navega para a lista (a navegação cuida do foco); a recusa passa pelo `MostrarErro` e se revela.

Run: `dotnet build -m:1`, depois `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~UsuarioFormularioInterativoTests|FullyQualifiedName~UsuariosWebTests|FullyQualifiedName~CargaDoFormularioInterativaTests"`
Expected: todos passando (`UsuariosWebTests` casa `>Inativar usuário</button>` sem olhar os atributos).

- [ ] **Step 12: Erro da busca de cliente do formulário de processo junto do campo (D2) — teste e implementação**

Acrescente a `tests/AtividadesJuridicas.IntegrationTests/ProcessoFormularioInterativoTests.cs`:

```csharp
    [Fact]
    public void Erro_da_busca_de_cliente_aparece_junto_do_campo_e_nao_no_topo()
    {
        var tela = Formulario();
        Despachante.Responde<ListarClientes, Resultado<PaginaDeClientes>>(
            Resultado<PaginaDeClientes>.Falha(TipoErro.Validacao, "Não foi possível consultar os clientes."));
        tela.Find("input[form=busca-de-cliente]").Input("coop");

        tela.Find("form#busca-de-cliente").Submit();

        var clientes = tela.FindAll("section.aj-painel").Single(s => s.QuerySelector("h2")?.TextContent == "Clientes do processo");
        clientes.QuerySelector("span.aj-campo-erro")!.TextContent.Should().Be("Não foi possível consultar os clientes.");
        tela.FindAll(".aj-aviso").Should().BeEmpty();
        Despachante.Recebidas.OfType<SalvarProcesso>().Should().BeEmpty();
    }
```

Run: `dotnet build -m:1`, depois `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ProcessoFormularioInterativoTests"`
Expected: FAIL no novo (o erro está no aviso do topo, nenhum `span.aj-campo-erro` no painel).

Em `src/AtividadesJuridicas.Web/Components/Pages/ProcessoFormulario.razor`, como o erro do cliente no formulário de usuário:

- Logo depois do `<label class="aj-campo-icone">…</label>` da busca (linha 54):

```razor
            @if (_erroDaBusca is not null)
            {
                <span class="aj-campo-erro">@_erroDaBusca</span>
            }
```

- No `@code`, o campo `private string? _erroDaBusca;` e, em `ProcurarClientesAsync`, a última linha `_mensagem = resultado.Sucesso ? null : resultado.Mensagem;` passa a:

```csharp
        // A falha da busca fica junto do campo dela; o aviso do topo é só o da gravação.
        _erroDaBusca = resultado.Sucesso ? null : resultado.Mensagem;
```

Run: `dotnet build -m:1`, depois `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ProcessoFormulario"` e `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~ErrosDeGravacaoTests"`
Expected: todos passando (o `ProcessoFormulario` fica com uma só atribuição de texto ao `_mensagem`, a do `MostrarErro`).

- [ ] **Step 13: Grupos com erro (F14, X5) — testes (devem falhar)**

Acrescente a `tests/AtividadesJuridicas.IntegrationTests/EventoFormularioInterativoTests.cs` (três cópias do grupo, uma por visão: um `id` de erro por visão):

```csharp
    [Fact]
    public void Grupo_de_participantes_em_erro_fica_invalido_e_aponta_para_o_erro_da_mesma_visao()
    {
        var tela = Edicao();
        foreach (var nome in new[] { "Diana Diretora", "Iara Inativa", "Usuário não encontrado" })
            OpcaoDoDesktop(tela, nome).QuerySelector("input")!.Change(false);

        Enviar(tela);

        var grupos = new[]
        {
            (Grupo: tela.Find("div.aj-so-desktop [aria-labelledby=titulo-dos-participantes]"), Visao: "desktop"),
            (Grupo: tela.Find("div.aj-so-tablet [aria-labelledby=titulo-dos-participantes]"), Visao: "tablet"),
            (Grupo: tela.FindAll("fieldset.aj-conjunto").Single(f => f.QuerySelector("legend")!.TextContent == "Participantes"), Visao: "mobile"),
        };
        foreach (var (grupo, visao) in grupos)
        {
            grupo.GetAttribute("aria-invalid").Should().Be("true", visao);
            grupo.GetAttribute("aria-describedby").Should().Be($"erro-dos-participantes-{visao}");
            tela.Find($"#erro-dos-participantes-{visao}").TextContent.Should().Be("Marque ao menos um participante.");
        }

        ChipDoTablet(tela, "Pedro Produção").QuerySelector("input")!.Change(true);

        tela.Find("div.aj-so-desktop [aria-labelledby=titulo-dos-participantes]").HasAttribute("aria-invalid").Should().BeFalse();
        tela.Find("div.aj-so-desktop [aria-labelledby=titulo-dos-participantes]").HasAttribute("aria-describedby").Should().BeFalse();
        tela.FindAll("[id^=erro-dos-participantes]").Should().BeEmpty();
        OpcaoDoDesktop(tela, "Pedro Produção").QuerySelector("input")!.HasAttribute("checked").Should().BeTrue();
    }
```

Acrescente a `tests/AtividadesJuridicas.IntegrationTests/AtividadeFormularioInterativoTests.cs`:

```csharp
    [Fact]
    public void Complexidade_em_erro_fica_invalida_e_aponta_para_o_erro()
    {
        var tela = Nova();

        tela.Find("form#formulario-da-atividade").Submit();

        var grupo = tela.Find("[role=group][aria-label=Complexidade]");
        grupo.GetAttribute("aria-invalid").Should().Be("true");
        grupo.GetAttribute("aria-describedby").Should().Be("erro-da-complexidade");
        tela.Find("#erro-da-complexidade").TextContent.Should().Be("Escolha a complexidade.");

        Complexidade(tela, "2");

        grupo = tela.Find("[role=group][aria-label=Complexidade]");
        grupo.HasAttribute("aria-invalid").Should().BeFalse();
        grupo.HasAttribute("aria-describedby").Should().BeFalse();
        tela.FindAll("#erro-da-complexidade").Should().BeEmpty();
    }
```

Acrescente a `tests/AtividadesJuridicas.IntegrationTests/AtividadeDevolucaoInterativaTests.cs`:

```csharp
    [Fact]
    public void Responsaveis_em_erro_ficam_invalidos_e_apontam_para_o_erro()
    {
        var tela = Devolver();
        Escrever(tela, "Rever a cláusula de quitação.");
        Marcar(tela, "Beatriz Lins", false);
        Marcar(tela, "Otávio Brandão", false);

        tela.Find("form#formulario-do-motivo").Submit();

        var conjunto = tela.Find("fieldset.aj-conjunto");
        conjunto.GetAttribute("aria-invalid").Should().Be("true");
        conjunto.GetAttribute("aria-describedby").Should().Be("erro-dos-responsaveis");
        tela.Find("#erro-dos-responsaveis").TextContent.Should().Be("Marque ao menos um responsável.");
        Despachante.Recebidas.OfType<DevolverAtividade>().Should().BeEmpty();
    }
```

Run: `dotnet build -m:1`, depois `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~EventoFormularioInterativoTests|FullyQualifiedName~AtividadeFormularioInterativoTests|FullyQualifiedName~AtividadeDevolucaoInterativaTests"`
Expected: FAIL nos três novos (sem `aria-invalid`, sem `aria-describedby`, sem os `id`).

- [ ] **Step 14: Grupos com erro (F14, X5) — implementação**

`src/AtividadesJuridicas.Web/Components/Pages/EventoFormulario.razor`, os três grupos de participantes (linhas 181–200):

```razor
                    <div class="aj-so-desktop">
                        <div class="aj-opcoes" role="group" aria-labelledby="titulo-dos-participantes" aria-invalid="@ParticipantesInvalidos" aria-describedby="@ErroDosParticipantes("desktop")">@foreach (var pessoa in _pessoas)
                            {<label class="aj-opcao" @key='("opcao", pessoa.Id)'><input type="checkbox" class="aj-opcao__caixa" checked="@Marcado(pessoa)" disabled="@Travado(pessoa)" @onchange="e => Marcar(pessoa, e)" />@pessoa.Rotulo@if (NotaDe(pessoa) is { } nota)
                                {<span class="aj-opcao__nota aj-opcao__nota--menor">@nota</span>}</label>}</div>
                        @if (_semParticipantes)
                        {
                            <span class="aj-campo-erro" id="erro-dos-participantes-desktop">Marque ao menos um participante.</span>
                        }
                    </div>
                    <div class="aj-so-tablet">
                        <div class="aj-opcoes" role="group" aria-labelledby="titulo-dos-participantes" aria-invalid="@ParticipantesInvalidos" aria-describedby="@ErroDosParticipantes("tablet")">@foreach (var pessoa in _pessoas)
                            {<label class="aj-chip aj-chip--pessoa" @key='("chip", pessoa.Id)'><input type="checkbox" class="aj-opcao__caixa" checked="@Marcado(pessoa)" disabled="@Travado(pessoa)" @onchange="e => Marcar(pessoa, e)" /><span class="@ClasseDoAvatar(pessoa)" aria-hidden="true">@SiglaDe(pessoa)</span>@NomeNoTablet(pessoa)</label>}</div>
                        @if (_semParticipantes)
                        {
                            <span class="aj-campo-erro" id="erro-dos-participantes-tablet">Marque ao menos um participante.</span>
                        }
                    </div>
                    <div class="aj-so-mobile">
                        <fieldset class="aj-conjunto" aria-invalid="@ParticipantesInvalidos" aria-describedby="@ErroDosParticipantes("mobile")"><legend class="@ClasseDaLegenda">Participantes</legend><div class="aj-opcoes">@foreach (var pessoa in _pessoas)
                            {<label class="aj-chip aj-chip--pessoa" @key='("chip-curto", pessoa.Id)'><input type="checkbox" class="aj-opcao__caixa" checked="@Marcado(pessoa)" disabled="@Travado(pessoa)" @onchange="e => Marcar(pessoa, e)" /><span class="@ClasseDoAvatar(pessoa)" aria-hidden="true">@SiglaDe(pessoa)</span>@NomeNoMobile(pessoa)</label>}</div>@if (_semParticipantes)
                            {<span class="aj-campo-erro" id="erro-dos-participantes-mobile">Marque ao menos um participante.</span>}</fieldset>
                    </div>
```

No `@code`, perto de `ClasseDaLegenda`:

```csharp
    // O grupo de participantes em erro: inválido e ligado à frase do erro da mesma visão (cada visão tem a sua cópia do
    // grupo e da frase, e um id de erro por visão). Sem erro, os dois atributos somem.
    private string? ParticipantesInvalidos => _semParticipantes ? "true" : null;

    private string? ErroDosParticipantes(string visao) => _semParticipantes ? $"erro-dos-participantes-{visao}" : null;
```

`src/AtividadesJuridicas.Web/Components/Pages/AtividadeFormulario.razor`, o grupo da complexidade (linhas 28–29), com uma cópia só (não muda por visão):

```razor
                <div class="@ClasseDaComplexidade" role="group" aria-label="Complexidade" aria-invalid="@(ErroDaComplexidade is null ? null : "true")" aria-describedby="@(ErroDaComplexidade is null ? null : "erro-da-complexidade")">Complexidade<div class="aj-segmentado"><button type="button" class="aj-segmentado__opcao" aria-pressed="@Marcada(1)" @onclick="() => EscolherComplexidade(1)">1</button><button type="button" class="aj-segmentado__opcao" aria-pressed="@Marcada(2)" @onclick="() => EscolherComplexidade(2)">2</button><button type="button" class="aj-segmentado__opcao" aria-pressed="@Marcada(3)" @onclick="() => EscolherComplexidade(3)">3</button></div>@if (ErroDaComplexidade is { } erro)
                    {<span class="aj-campo-erro" id="erro-da-complexidade">@erro</span>}</div>
```

e no `@code`, depois de `ErrosDaComplexidade`:

```csharp
    // A complexidade só tem a regra do intervalo: no máximo uma mensagem, que o grupo aponta pelo id.
    private string? ErroDaComplexidade => ErrosDaComplexidade.FirstOrDefault();
```

`src/AtividadesJuridicas.Web/Components/Atividades/MotivoComResponsaveis.razor`, o conjunto dos responsáveis (linhas 33–41):

```razor
            <fieldset class="aj-conjunto" aria-invalid="@(_erroDosResponsaveis is null ? null : "true")" aria-describedby="@(_erroDosResponsaveis is null ? null : "erro-dos-responsaveis")">
                <legend class="@ClasseDaLegenda">Responsáveis que voltam a ter a parte pendente</legend>
                @foreach (var responsavel in _detalhe.Responsaveis)
                {
                    <label class="aj-opcao" @key="responsavel.UsuarioId"><input type="checkbox" class="aj-opcao__caixa" checked="@_marcados.Contains(responsavel.UsuarioId)" disabled="@_enviando" @onchange="e => Marcar(responsavel.UsuarioId, e)" />@responsavel.Nome</label>
                }
                @if (_erroDosResponsaveis is not null)
                {
                    <span class="aj-campo-erro" id="erro-dos-responsaveis">@_erroDosResponsaveis</span>
                }
            </fieldset>
```

Run: `dotnet build -m:1`, depois `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~EventoFormulario|FullyQualifiedName~AtividadeFormulario|FullyQualifiedName~AtividadeDevolucao|FullyQualifiedName~AtividadeReabertura"`
Expected: todos passando (os estáticos não veem os atributos: sem erro, o Blazor não emite atributo de valor nulo).

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~ErrosDeGravacaoTests"`
Expected: todos passando.

Para a Tarefa 11 (só o navegador prova): o aviso de erro revelado rola até a vista a 390×844 e a 375×667 com o formulário rolado até o fim; o leitor de tela lê o grupo inválido com a frase do erro (árvore de acessibilidade); o foco de volta chega à cópia visível do botão em cada largura.

- [ ] **Step 15: Commit dos erros de formulário**

```
git branch --show-current
git add src/AtividadesJuridicas.Web/Components/Pages src/AtividadesJuridicas.Web/Components/Atividades/MotivoComResponsaveis.razor tests/AtividadesJuridicas.UnitTests/ErrosDeGravacaoTests.cs tests/AtividadesJuridicas.IntegrationTests
git commit -F <arquivo da mensagem>
```

Antes do `git add`, `git status` deve mostrar só os arquivos desta tarefa (os oito formulários, `ProcessoFormulario.razor` também pelo D2, e os testes).

```
feat(web): erro de gravação se revela nos oito formulários, erro da busca de cliente junto do campo e grupos inválidos ligados ao erro

O aviso de erro de gravação leva o foco a si e rola até a vista, com uma chave nova a cada mensagem (o mesmo erro duas
vezes se revela das duas). A falha da busca de cliente do processo aparece junto do campo. Os participantes do evento
(um id de erro por visão), a complexidade da atividade e os responsáveis do devolver e do reabrir ficam com
aria-invalid e aria-describedby enquanto estão em erro.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

### Task 10: Listas e agenda — itens pontuais e limpeza de código

**Files:**
- Modify: `Components/Layout/Paginacao.razor`, `Pages/Andamento.razor` (B9), `Pages/UsuarioFormulario.razor` (C7), `Pages/Usuarios.razor` (C8), `Pages/AtividadeResponsaveis.razor` (E10), `Pages/Agenda.razor`, `Components/Agenda/MesDaAgenda.razor`, `SecaoDoDia.razor`, `DiaDaAgenda.razor`, `RotulosDeAgenda.cs`, `AvisoDeEventoSalvo.razor`, `Pages/EventoFicha.razor`, `Pages/EventoFormulario.razor` (F2, F3, F4, F8, F9, F11, F13), `Components/Layout/AvatarDePessoa.cs` (F1)
- Test: os novos `PaginacaoInterativaTests.cs` e `AndamentoInterativoTests.cs`; acréscimos a `UsuarioFormularioInterativoTests.cs` e `UsuariosInterativosTests.cs` (criados na Tarefa 9); `ComponentesDoDesignSystemTests.cs`, `DocumentosDoPortalWebTests.cs`, `AgendaPuraTests.cs`, `AgendaWebTests.cs`, `AgendaInterativaTests.cs`, `MesDaAgendaTests.cs`, `DiaDaAgendaTests.cs`, `SemanaDaAgendaTests.cs`, `AvisoDeEventoSalvoTests.cs`, `AtividadeResponsaveisWebTests.cs`, `TelasPurasTests.cs`

A Tarefa 10 roda depois das Tarefas 7 e 9, que tocam `Agenda.razor`, `AgendaInterativaTests.cs`, `Usuarios.razor`, `UsuarioFormulario.razor`, `EventoFicha.razor`, `EventoFormulario.razor` e `AtividadeResponsaveis.razor`: os passos foram escritos sobre o código de `8d5dd3a`; o implementador os aplica sobre o código depois delas, preservando o que elas fizeram.

**Regras:** B9 (a lista e a paginação do Andamento ficam à vista durante a carga da página nova, com a paginação `Ocupado`); **Paginação sem `disabled`** (o componente `Layout/Paginacao.razor`, em todas as listas: o botão sem ação — durante a carga ou na ponta — leva `aria-disabled="true"` em vez de `disabled` e ignora o clique, para o foco não cair no `body` quando o botão focado perde a ação; o design system já desenha `.aj-botao[aria-disabled="true"]` e a Tarefa 1 acrescenta a variante com setas; os testes que afirmam `disabled` na paginação passam a afirmar `aria-disabled="true"` e que o clique não muda a página); C7 (a carga do usuário confere que o `Id` ainda é o mesmo antes de aplicar); C8 (abrir outra confirmação de inativação limpa a mensagem de resultado anterior, de sucesso ou de falha); E10 (a barra do modo do revisor com `aj-acoes--medio` no desktop); F1 (sai o código só dos testes: `RotulosDeAgenda.MensagemDeSalvo`, `Sobreposicao`, `AvatarDePessoa.Classe(nome, proprio)`; os testes passam às redações e ao `Classes` de produção, sem perder o que provam); F2 (o `strong` do `AvisoDeEventoSalvo` usa o `Recado.Texto`); F3 (o rótulo do `+N`, "Cliente indisponível" e "Processo indisponível" vão a `RotulosDeAgenda`, com `Contagem.De` no plural); F4 (o erro com "Tentar de novo" da agenda pelo componente compartilhado); F8 (região viva só para leitor com o período, atualizada na troca de período; o foco fica na seta); F9 (a grade compacta do Mês do mobile fica à vista durante a carga do mês novo); F11 (`+N` leva o foco ao título do Dia: `h2.aj-dia__cabeca` no desktop e `h2.aj-secao-dia__cabeca` do Dia no tablet, com `tabindex="-1"`, pelo serviço `Foco`; sem contorno, pela regra da Tarefa 1); F13 (`role="group" aria-label="Período"` nas setas e no "Hoje" das três visões).

**Testes obrigatórios:** bUnit/estático para cada item, com o foco pedido onde houver foco.

- [ ] **Step 1: Pré-requisitos (branch e o que as tarefas anteriores deixaram)**

Confira o branch com a ferramenta PowerShell: `git branch --show-current` deve dizer `worktree-feat+fase-9g-limpeza`.

Esta tarefa roda depois das Tarefas 3 a 9, e alguns trechos "de hoje" abaixo já passaram por elas. O que encontrar:

- **Testes (Tarefa 3):** a `TelaInterativa` registra o serviço `Foco` e já monta o módulo de foco (`ModuloDeFoco`); o auxiliar `SeletoresFocados` dá os seletores pedidos a `focarPrimeiroVisivel`, na ordem. Os testes desta tarefa usam esses nomes: não montam o módulo de foco com `JSInterop.SetupModule` (um segundo `SetupModule` no mesmo caminho dividiria as chamadas) nem declaram membros com esses nomes.
- **`Pages/Agenda.razor` (Tarefa 7):** o painel de filtros passou ao componente `PainelDeFiltros` (sempre no HTML, com `hidden` quando fechado), e saíram do `@code` os campos `_focarPainel` e `_tipoDoPainel` e o `OnAfterRenderAsync` inteiro (o foco do painel é do componente). Ficaram `_filtrosAbertos`, `AlternarFiltros` (sem o `_focarPainel`), o `FecharFiltros` novo, `FiltrarDoPainelAsync` e `LimparFiltros`. A Agenda não injeta o `Foco` (quem o injeta é o `PainelDeFiltros`). Não mexa no painel.
- **`AgendaInterativaTests.cs` (Tarefa 7):** ganhou o `using Microsoft.AspNetCore.Components.Web;`, os testes do painel (que usam `SeletoresFocados`, `CamposDoPainel` e `BotaoDoPainel`) e o teste do Esc; o auxiliar `CampoDoPainel` continua.
- **`Pages/Usuarios.razor` e `Pages/UsuarioFormulario.razor` (Tarefa 9):** o `@inject Foco Foco`, o `_versaoDoAviso` (lista) e o `_versaoDoErro` com `MostrarErro` (formulário), o `_focoDeVolta`, o `CancelarInativacao` e o `OnAfterRenderAsync` do foco de volta, o `id` dos botões de ativar e inativar. Os dois arquivos de teste, `UsuariosInterativosTests.cs` e `UsuarioFormularioInterativoTests.cs`, foram **criados pela Tarefa 9**: esta tarefa acrescenta a eles (Step 8).
- **`Pages/EventoFicha.razor`, `Pages/EventoFormulario.razor` e `Pages/AtividadeResponsaveis.razor` (Tarefa 9):** o aviso com `Revelar`, o `MostrarErro` e o foco de volta; os trechos que esta tarefa troca (os textos "Cliente indisponível" e "Processo indisponível", a barra do desktop da troca de revisor) não foram tocados.

Mexa só nos trechos que os passos abaixo citam, sem desfazer o que as outras tarefas fizeram.

- [ ] **Step 2: Paginação sem `disabled` — testes (devem falhar)**

O botão da paginação sem ação (durante a carga, `Ocupado`, ou na ponta) passa de `disabled` a `aria-disabled="true"` e ignora o clique. Com `disabled`, o botão focado que perde a ação (o "Próxima" clicado, durante a carga; ou o mesmo "Próxima", ao chegar à última página) pode deixar o foco cair no `body`. Com `aria-disabled`, ele continua focável e é anunciado como indisponível. O design system já desenha `.aj-botao[aria-disabled="true"]`, e a Tarefa 1 acrescenta a variante com setas.

Os testes de hoje que afirmam `disabled` nos botões da paginação, todos reescritos abaixo:

| Teste | Afirma hoje | Reescrita |
|---|---|---|
| `ComponentesDoDesignSystemTests.Paginacao_diz_onde_esta_e_trava_as_pontas` (theory, 3 casos) | `<button[^>]*disabled[^>]*>Anterior` e `…>Próxima` conforme a ponta | `aria-disabled="true"` na ponta, nenhum `disabled` em botão nenhum |
| `ComponentesDoDesignSystemTests.Paginacao_com_setas_traz_a_seta_de_cada_lado_e_trava_as_pontas` | `aj-paginacao__anterior"[^>]*disabled` sim, `aj-paginacao__proxima"[^>]*disabled` não | o mesmo com `aria-disabled="true"`, sem `disabled` |
| `DocumentosDoPortalWebTests.Lista_de_documentos_com_mais_de_uma_pagina_traz_a_paginacao_nas_duas_formas` | "Anterior" com `disabled`, "Próxima" sem, nas duas visões | o mesmo com `aria-disabled="true"`, sem `disabled` |
| `AndamentoInterativoTests` (novo nesta tarefa, Step 5) | — | já escrito com `aria-disabled` |

Atenção: os regex de hoje (`<button[^>]*disabled[^>]*>`) também casam com `aria-disabled`. Por isso a reescrita procura o atributo inteiro (`aria-disabled="true"`) e afirma que nenhum botão tem ` disabled` (o atributo solto, com o espaço antes). Confira com a ferramenta Grep, em `tests`, `disabled` junto de `Anterior|Próxima|aj-paginacao`: só os três acima. Os outros `disabled` dos testes (formulários, confirmações, envio de documentos, comentários) não são da paginação e não mudam.

**`tests/AtividadesJuridicas.IntegrationTests/ComponentesDoDesignSystemTests.cs`** (CRLF). No theory `Paginacao_diz_onde_esta_e_trava_as_pontas`, troque

```csharp
        Regex.IsMatch(html, "<button[^>]*disabled[^>]*>Anterior</button>").Should().Be(anteriorTravado);
        Regex.IsMatch(html, "<button[^>]*disabled[^>]*>Próxima</button>").Should().Be(proximaTravada);
```

por

```csharp
        // O botão sem ação fica focável (aria-disabled, não disabled): o foco não cai no body quando ele a perde.
        Regex.IsMatch(html, "<button[^>]*aria-disabled=\"true\"[^>]*>Anterior</button>").Should().Be(anteriorTravado);
        Regex.IsMatch(html, "<button[^>]*aria-disabled=\"true\"[^>]*>Próxima</button>").Should().Be(proximaTravada);
        html.Should().Contain(">Anterior</button>").And.Contain(">Próxima</button>").And.NotMatchRegex("<button[^>]*\\sdisabled[\\s>=]");
```

No teste `Paginacao_com_setas_traz_a_seta_de_cada_lado_e_trava_as_pontas`, troque

```csharp
        Regex.IsMatch(html, "aj-paginacao__anterior\"[^>]*disabled").Should().BeTrue();
        Regex.IsMatch(html, "aj-paginacao__proxima\"[^>]*disabled").Should().BeFalse();
```

por

```csharp
        Regex.IsMatch(html, "aj-paginacao__anterior\"[^>]*aria-disabled=\"true\"").Should().BeTrue();
        Regex.IsMatch(html, "aj-paginacao__proxima\"[^>]*aria-disabled").Should().BeFalse();
        html.Should().NotMatchRegex("<button[^>]*\\sdisabled[\\s>=]");
```

(a positiva do mesmo HTML são os dois `MatchRegex` dos botões, logo acima).

**`tests/AtividadesJuridicas.IntegrationTests/DocumentosDoPortalWebTests.cs`** (CRLF), teste `Lista_de_documentos_com_mais_de_uma_pagina_traz_a_paginacao_nas_duas_formas`, troque

```csharp
                .And.MatchRegex("<button[^>]*disabled[^>]*>Anterior</button>", visao)
                .And.Contain(">Próxima</button>", visao)
                .And.NotMatchRegex("<button[^>]*disabled[^>]*>Próxima</button>", visao);
```

por

```csharp
                .And.MatchRegex("<button[^>]*aria-disabled=\"true\"[^>]*>Anterior</button>", visao)
                .And.Contain(">Próxima</button>", visao)
                .And.NotMatchRegex("<button[^>]*aria-disabled[^>]*>Próxima</button>", visao)
                .And.NotMatchRegex("<button[^>]*\\sdisabled[\\s>=]", visao);
```

Crie `tests/AtividadesJuridicas.IntegrationTests/PaginacaoInterativaTests.cs` (UTF-8 sem BOM, CRLF):

```csharp
using AngleSharp.Dom;
using AtividadesJuridicas.Web.Components.Layout;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O clique na paginação: o botão sem ação (na ponta ou durante a carga) continua focável, com aria-disabled, e o clique
/// nele não muda a página; o botão com ação pede a página vizinha. Nas duas formas (com e sem setas).
/// </summary>
public class PaginacaoInterativaTests : TelaInterativa
{
    private readonly List<int> _pedidas = [];

    private IRenderedComponent<Paginacao> Paginacao(int numero, bool ocupado = false, bool comSetas = false) =>
        Desenhar<Paginacao>(p => p
            .Add(c => c.Numero, numero)
            .Add(c => c.TotalDePaginas, 3)
            .Add(c => c.Total, 45)
            .Add(c => c.Singular, "atividade")
            .Add(c => c.Plural, "atividades")
            .Add(c => c.Ocupado, ocupado)
            .Add(c => c.ComSetas, comSetas)
            .Add(c => c.IrPara, (int pagina) => _pedidas.Add(pagina)));

    private static IElement Botao(IRenderedComponent<Paginacao> paginacao, string texto) =>
        paginacao.FindAll("button").Single(b => b.TextContent.Trim() == texto);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Botao_da_ponta_tem_aria_disabled_e_o_clique_nao_muda_a_pagina(bool comSetas)
    {
        var primeira = Paginacao(1, comSetas: comSetas);

        Botao(primeira, "Anterior").GetAttribute("aria-disabled").Should().Be("true");
        Botao(primeira, "Anterior").HasAttribute("disabled").Should().BeFalse();
        Botao(primeira, "Anterior").Click();
        _pedidas.Should().BeEmpty();

        Botao(primeira, "Próxima").HasAttribute("aria-disabled").Should().BeFalse();
        Botao(primeira, "Próxima").Click();
        _pedidas.Should().Equal(2);

        var ultima = Paginacao(3, comSetas: comSetas);

        Botao(ultima, "Próxima").GetAttribute("aria-disabled").Should().Be("true");
        Botao(ultima, "Próxima").Click();
        Botao(ultima, "Anterior").Click();
        _pedidas.Should().Equal(2, 2);
    }

    [Fact]
    public void Durante_a_carga_os_dois_botoes_tem_aria_disabled_e_o_clique_nao_muda_a_pagina()
    {
        var paginacao = Paginacao(2, ocupado: true);

        paginacao.FindAll("button").Should().HaveCount(2)
            .And.OnlyContain(b => b.GetAttribute("aria-disabled") == "true" && !b.HasAttribute("disabled"));
        Botao(paginacao, "Anterior").Click();
        Botao(paginacao, "Próxima").Click();
        _pedidas.Should().BeEmpty();

        paginacao.Render(p => p.Add(c => c.Ocupado, false));

        paginacao.FindAll("button").Should().HaveCount(2).And.OnlyContain(b => !b.HasAttribute("aria-disabled"));
        Botao(paginacao, "Próxima").Click();
        _pedidas.Should().Equal(3);
    }
}
```

O texto dos botões com setas tem o ícone (`svg`) ao lado; o `TextContent.Trim()` dá "Anterior" e "Próxima" nas duas formas.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~PaginacaoInterativaTests"`
Expected: falham os três casos já na primeira asserção (o botão da ponta e os dois botões ocupados têm `disabled`, não `aria-disabled="true"`).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ComponentesDoDesignSystemTests"`
Expected: os dois testes da paginação falham (não há `aria-disabled="true"`, e o `NotMatchRegex` acha o `disabled`); os outros passam.

`DocumentosDoPortalWebTests` usa o banco; ele roda no Step 3, depois da mudança.

- [ ] **Step 3: Paginação sem `disabled`**

Em `src/AtividadesJuridicas.Web/Components/Layout/Paginacao.razor` (CRLF), troque os quatro botões

```razor
                <button type="button" class="aj-botao aj-botao--secundario aj-paginacao__anterior" disabled="@(Ocupado || Numero <= 1)" @onclick="() => IrPara.InvokeAsync(Numero - 1)"><Icone Nome="voltar" />Anterior</button>
                <button type="button" class="aj-botao aj-botao--secundario aj-paginacao__proxima" disabled="@(Ocupado || Numero >= TotalDePaginas)" @onclick="() => IrPara.InvokeAsync(Numero + 1)">Próxima<Icone Nome="avancar" /></button>
```

```razor
                <button type="button" class="aj-botao aj-botao--secundario" disabled="@(Ocupado || Numero <= 1)" @onclick="() => IrPara.InvokeAsync(Numero - 1)">Anterior</button>
                <button type="button" class="aj-botao aj-botao--secundario" disabled="@(Ocupado || Numero >= TotalDePaginas)" @onclick="() => IrPara.InvokeAsync(Numero + 1)">Próxima</button>
```

por

```razor
                <button type="button" class="aj-botao aj-botao--secundario aj-paginacao__anterior" aria-disabled="@Indisponivel(SemAnterior)" @onclick="AnteriorAsync"><Icone Nome="voltar" />Anterior</button>
                <button type="button" class="aj-botao aj-botao--secundario aj-paginacao__proxima" aria-disabled="@Indisponivel(SemProxima)" @onclick="ProximaAsync">Próxima<Icone Nome="avancar" /></button>
```

```razor
                <button type="button" class="aj-botao aj-botao--secundario" aria-disabled="@Indisponivel(SemAnterior)" @onclick="AnteriorAsync">Anterior</button>
                <button type="button" class="aj-botao aj-botao--secundario" aria-disabled="@Indisponivel(SemProxima)" @onclick="ProximaAsync">Próxima</button>
```

No comentário do topo, acrescente: `O botão sem ação (na ponta ou com uma consulta em curso) fica focável, com aria-disabled, e ignora o clique: o foco não cai no body quando o botão focado perde a ação.` Troque o comentário do `Ocupado` (`/// <summary>Uma consulta está em curso: os dois botões esperam.</summary>`) por `/// <summary>Uma consulta está em curso: os dois botões esperam (aria-disabled, e o clique não pede página).</summary>`. No `@code`, depois de `Texto`, acrescente:

```csharp

    private bool SemAnterior => Ocupado || Numero <= 1;

    private bool SemProxima => Ocupado || Numero >= TotalDePaginas;

    // O atributo só aparece quando o botão está sem ação; com ação, ele some (nulo).
    private static string? Indisponivel(bool semAcao) => semAcao ? "true" : null;

    private Task AnteriorAsync() => SemAnterior ? Task.CompletedTask : IrPara.InvokeAsync(Numero - 1);

    private Task ProximaAsync() => SemProxima ? Task.CompletedTask : IrPara.InvokeAsync(Numero + 1);
```

As telas que usam a paginação (Atividades, Andamento, Auditoria, Clientes, Processos, documentos) não mudam: o botão sem ação não pede página, como acontecia com o `disabled`.

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run, um por vez:
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~PaginacaoInterativaTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ComponentesDoDesignSystemTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DocumentosDoPortalWebTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadesInterativosTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DocumentosDaAtividadeInterativosTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadesWebTests"`
- `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`

Expected: todos passando.

Navegador (Tarefa 11): em Atividades (com setas) e no Andamento (sem setas), o botão sem ação continua com a aparência de travado (o `.aj-botao[aria-disabled="true"]` do design system e a variante com setas da Tarefa 1). Com o teclado: Enter no "Próxima" até a última página; o foco fica no botão, que passa a "indisponível" no leitor de tela.

- [ ] **Step 4: Commit da paginação**

Confira o branch (`git branch --show-current` deve dizer `worktree-feat+fase-9g-limpeza`) e, pela ferramenta PowerShell:

```
git add src/AtividadesJuridicas.Web/Components/Layout/Paginacao.razor tests/AtividadesJuridicas.IntegrationTests/PaginacaoInterativaTests.cs tests/AtividadesJuridicas.IntegrationTests/ComponentesDoDesignSystemTests.cs tests/AtividadesJuridicas.IntegrationTests/DocumentosDoPortalWebTests.cs
git commit -F <arquivo com a mensagem, UTF-8 sem BOM>
```

Mensagem:

```
fix(web): os botões sem ação da paginação ficam focáveis, com aria-disabled, e ignoram o clique

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

- [ ] **Step 5: Andamento — testes da troca de página (B9, devem falhar)**

Crie `tests/AtividadesJuridicas.IntegrationTests/AndamentoInterativoTests.cs` (UTF-8 sem BOM, fim de linha CRLF, como os vizinhos):

```csharp
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Comum;
using Bunit;
using TelaAndamento = AtividadesJuridicas.Web.Components.Pages.Andamento;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// A paginação e os chips do Andamento (portal do cliente). A página nova chega sem tirar da tela a lista e a
/// paginação: o botão tocado continua no lugar, e o foco com ele. Outra situação é outra lista: "Carregando…" fica no
/// lugar da anterior até a resposta. A falha não deixa a página anterior na tela.
/// </summary>
public class AndamentoInterativoTests : TelaInterativa
{
    // 45 atividades em páginas de 20: três páginas. Os totais do cliente (40 em andamento, 5 concluídas) não dependem
    // do filtro.
    private static Resultado<AndamentoDoCliente> Resposta(int numero, string titulo) =>
        Resultado<AndamentoDoCliente>.Ok(new AndamentoDoCliente(
            new Pagina<AtividadeDoAndamento>(
                [new AtividadeDoAndamento(titulo, "Tributário", null, SituacaoDoAndamento.EmAndamento, null)], 45, numero, 20),
            40, 5));

    private IRenderedComponent<TelaAndamento> Andamento()
    {
        Despachante.Responde<ListarMeuAndamento, Resultado<AndamentoDoCliente>>(Resposta(1, "Contestação da página 1"));
        return Desenhar<TelaAndamento>();
    }

    // A próxima consulta fica esperando até o teste completar a tarefa.
    private TaskCompletionSource<Resultado<AndamentoDoCliente>> ProximaRespostaEspera()
    {
        var resposta = new TaskCompletionSource<Resultado<AndamentoDoCliente>>();
        Despachante.RespondeDepois<ListarMeuAndamento, Resultado<AndamentoDoCliente>>(resposta.Task);
        return resposta;
    }

    private List<ListarMeuAndamento> Consultas => [.. Despachante.Recebidas.OfType<ListarMeuAndamento>()];

    private static string Texto(IElement elemento) => Regex.Replace(elemento.TextContent, "\\s+", " ").Trim();

    // Um botão da paginação de uma visão ("aj-so-desktop" ou "aj-tablet-mobile") pelo texto.
    private static IElement Botao(IRenderedComponent<TelaAndamento> tela, string visao, string texto) =>
        tela.FindAll($"div.{visao} .aj-paginacao button").Single(b => Texto(b) == texto);

    // B9: "Próxima" não tira a lista nem a paginação da tela enquanto a página nova não chega: os quatro botões esperam
    // (aria-disabled, focáveis) e o tocado continua no lugar; um segundo clique não pede outra página. A resposta troca a
    // lista. Um chip, depois, é outra situação: a lista sai na hora e "Carregando…" fica no lugar dela.
    [Fact]
    public void Proxima_pagina_mantem_a_lista_e_a_paginacao_a_vista_ate_a_resposta_e_o_chip_mostra_carregando()
    {
        var tela = Andamento();
        var segunda = ProximaRespostaEspera();

        Botao(tela, "aj-so-desktop", "Próxima").Click();

        Consultas.Should().HaveCount(2);
        Consultas[1].Pagina.Should().Be(2);
        tela.Markup.Should().Contain("Contestação da página 1").And.NotContain("Carregando…");
        tela.FindAll(".aj-paginacao button").Should().HaveCount(4)
            .And.OnlyContain(b => b.GetAttribute("aria-disabled") == "true" && !b.HasAttribute("disabled"));
        Texto(tela.Find("div.aj-so-desktop .aj-paginacao .aj-subtitulo")).Should().Be("Página 1 de 3 · 45 atividades");
        Botao(tela, "aj-so-desktop", "Próxima").Click();
        Consultas.Should().HaveCount(2);

        segunda.SetResult(Resposta(2, "Recurso da página 2"));

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Recurso da página 2"));
        tela.Markup.Should().NotContain("Contestação da página 1");
        Texto(tela.Find("div.aj-so-desktop .aj-paginacao .aj-subtitulo")).Should().Be("Página 2 de 3 · 45 atividades");
        tela.FindAll(".aj-paginacao button").Should().HaveCount(4)
            .And.OnlyContain(b => !b.HasAttribute("aria-disabled") && !b.HasAttribute("disabled"));

        var concluidas = ProximaRespostaEspera();
        tela.FindAll(".aj-chips button").Single(b => Texto(b).StartsWith("Concluídas")).Click();

        tela.Markup.Should().Contain("Carregando…").And.NotContain("Recurso da página 2");
        Consultas[2].Situacao.Should().Be(SituacaoDoAndamento.Concluida);
        Consultas[2].Pagina.Should().Be(1);

        concluidas.SetResult(Resposta(1, "Parecer concluído"));

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Parecer concluído"));
        tela.Markup.Should().NotContain("Carregando…");
    }

    // B9: a página anterior não volta à tela depois de uma falha: o erro fica no lugar da lista e, no "Tentar de novo",
    // "Carregando…" fica no lugar até a página pedida chegar.
    [Fact]
    public void Falha_da_pagina_nova_tira_a_anterior_e_tentar_de_novo_pede_a_mesma_pagina()
    {
        var tela = Andamento();
        Despachante.Responde<ListarMeuAndamento, Resultado<AndamentoDoCliente>>(
            Resultado<AndamentoDoCliente>.Falha(TipoErro.Validacao, "Falha de teste."));

        Botao(tela, "aj-tablet-mobile", "Próxima").Click();

        tela.Markup.Should().Contain("Falha de teste.").And.NotContain("Contestação da página 1");
        tela.FindAll(".aj-paginacao").Should().BeEmpty();

        var nova = ProximaRespostaEspera();
        tela.FindAll("button").Single(b => Texto(b) == "Tentar de novo").Click();

        tela.Markup.Should().Contain("Carregando…").And.NotContain("Contestação da página 1").And.NotContain("Falha de teste.");

        nova.SetResult(Resposta(2, "Recurso da página 2"));

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Recurso da página 2"));
        Consultas.Select(c => c.Pagina).Should().Equal(1, 2, 2);
    }
}
```

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AndamentoInterativoTests"`
Expected: o primeiro teste falha logo depois do clique em "Próxima" (a tela mostra "Carregando…" no lugar da lista: `Expected tela.Markup … to contain "Contestação da página 1"`). O segundo passa já hoje (a tela de hoje troca tudo por "Carregando…"): ele protege a mudança do próximo passo contra deixar a página anterior na tela depois de uma falha.

- [ ] **Step 6: Andamento — a lista fica durante a troca de página (B9)**

Em `src/AtividadesJuridicas.Web/Components/Pages/Andamento.razor` (CRLF), só o `@code` muda; a marcação já passa `Ocupado="_carregando"` às duas paginações.

Troque

```csharp
    private Task FiltrarAsync(SituacaoDoAndamento? situacao)
    {
        _situacao = situacao;
        return CarregarAsync(1);
    }
```

por

```csharp
    private Task FiltrarAsync(SituacaoDoAndamento? situacao)
    {
        _situacao = situacao;
        // Outra situação é outra lista: a anterior sai na hora, e "Carregando…" fica no lugar até a nova chegar.
        _pagina = null;
        return CarregarAsync(1);
    }
```

No `CarregarAsync`, troque o comentário e o começo

```csharp
        // Uma resposta que chega depois de outra consulta mais nova é descartada: a lista na tela
        // nunca é a de uma situação que já não está escolhida.
        var geracao = ++_geracao;
        _carregando = true;
        _paginaPedida = pagina;
        _erro = null;
        _pagina = null;
        try
```

por

```csharp
        // Uma resposta que chega depois de outra consulta mais nova é descartada: a lista na tela
        // nunca é a de uma situação que já não está escolhida. Na troca de página, a página anterior fica à vista, com a
        // paginação esperando (Ocupado), até a nova chegar: o botão tocado continua no lugar, com o foco.
        var geracao = ++_geracao;
        _carregando = true;
        _paginaPedida = pagina;
        _erro = null;
        try
```

e, no ramo da falha, troque

```csharp
                // Falha na consulta não é "nenhuma atividade": mostra o erro no lugar da lista.
                _erro = resultado.Mensagem ?? "Não foi possível consultar o andamento.";
                return;
```

por

```csharp
                // Falha na consulta não é "nenhuma atividade": mostra o erro no lugar da lista, e a página anterior não
                // volta à tela no "Tentar de novo".
                _pagina = null;
                _erro = resultado.Mensagem ?? "Não foi possível consultar o andamento.";
                return;
```

A primeira carga continua com "Carregando…" (`_pagina` começa nulo), e o acesso encerrado continua trocando a tela inteira pelo aviso.

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AndamentoInterativoTests"`
Expected: os dois passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~PortalDoClienteWebTests"`
Expected: todos passando (o HTML do Andamento por HTTP não muda).

O que fica para o navegador (Tarefa 11): o foco continua no "Próxima"/"Anterior" durante a carga (o botão fica com `aria-disabled`, focável, pelo Step 3) e depois dela, inclusive ao chegar à última página.

- [ ] **Step 7: Commit do Andamento**

Confira o branch (`git branch --show-current` deve dizer `worktree-feat+fase-9g-limpeza`) e, pela ferramenta PowerShell:

```
git add src/AtividadesJuridicas.Web/Components/Pages/Andamento.razor tests/AtividadesJuridicas.IntegrationTests/AndamentoInterativoTests.cs
git commit -F <arquivo com a mensagem, UTF-8 sem BOM>
```

Mensagem:

```
fix(web): a página nova do Andamento chega sem tirar a lista e a paginação da tela

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

- [ ] **Step 8: Usuários — testes da carga atrasada (C7) e do resultado ao lado de outra confirmação (C8), devem falhar**

Os dois arquivos de teste foram criados pela Tarefa 9; esta tarefa acrescenta a eles, sem criar outro e sem repetir o que já está lá.

Em `tests/AtividadesJuridicas.IntegrationTests/UsuarioFormularioInterativoTests.cs` (CRLF; da Tarefa 9, com os `Guid` `Pedro` e `Producao` — o do perfil —, o `Edicao()` e os testes da confirmação e do erro de gravação), troque o resumo da classe

```csharp
/// <summary>O formulário de usuário em edição, clicando: a confirmação de inativação e o erro de gravação.</summary>
```

por

```csharp
/// <summary>
/// O formulário de usuário em edição, clicando: a confirmação de inativação, o erro de gravação e a rota trocando de
/// usuário durante a carga (a resposta do usuário anterior chega atrasada e não toma o lugar do usuário da rota, nem o
/// estado da carga dele).
/// </summary>
```

e acrescente ao fim da classe (os `using` que a Tarefa 9 pôs cobrem tudo; o perfil reaproveita o `Producao` dela):

```csharp

    private static readonly PerfilDetalhe PerfilDaProducao = new(Producao, "Produção", TipoPerfil.Producao, true, 2);

    private static readonly Guid IdDaAna = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b1");
    private static readonly Guid IdDoBruno = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b2");

    // Não há consulta de um só usuário: o formulário o acha na lista.
    private static Resultado<IReadOnlyList<UsuarioResumo>> Lista() => Resultado<IReadOnlyList<UsuarioResumo>>.Ok(
    [
        new UsuarioResumo(IdDaAna, "Ana Antunes", "ana@x.com", Producao, "Produção", TipoPerfil.Producao, null, true, null),
        new UsuarioResumo(IdDoBruno, "Bruno Barros", "bruno@x.com", Producao, "Produção", TipoPerfil.Producao, null, true, null),
    ]);

    // A edição da Ana, com a consulta da lista esperando a resposta.
    private IRenderedComponent<UsuarioFormulario> EdicaoDaAnaEsperando(Task<Resultado<IReadOnlyList<UsuarioResumo>>> lista)
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(Resultado<IReadOnlyList<PerfilDetalhe>>.Ok([PerfilDaProducao]))
            .RespondeDepois<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(lista);
        return Desenhar<UsuarioFormulario>(p => p.Add(c => c.Id, (Guid?)IdDaAna));
    }

    private static string? Email(IRenderedComponent<UsuarioFormulario> tela) =>
        tela.Find("form#formulario-do-usuario input[type=email]").GetAttribute("value");

    // C7: a rota passou ao Bruno com a consulta da Ana esperando; o Bruno chega na hora, e a resposta atrasada da Ana não
    // toma o lugar dele.
    [Fact]
    public void Resposta_atrasada_do_usuario_anterior_nao_sobrescreve_o_da_rota()
    {
        var daAna = new TaskCompletionSource<Resultado<IReadOnlyList<UsuarioResumo>>>();
        var tela = EdicaoDaAnaEsperando(daAna.Task);
        tela.Markup.Should().Contain("Carregando…");

        Despachante.Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Lista());
        tela.Render(p => p.Add(c => c.Id, (Guid?)IdDoBruno));
        tela.WaitForAssertion(() => Email(tela).Should().Be("bruno@x.com"));

        var desenhos = tela.RenderCount;
        daAna.SetResult(Lista());

        // A carga da Ana termina (a tela desenha de novo) sem mexer no formulário.
        tela.WaitForAssertion(() => tela.RenderCount.Should().BeGreaterThan(desenhos));
        Email(tela).Should().Be("bruno@x.com");
        tela.Find("form#formulario-do-usuario input.aj-campo").GetAttribute("value").Should().Be("Bruno Barros");
        tela.Markup.Should().NotContain("ana@x.com");
    }

    // C7: com as duas consultas esperando, a da Ana chega primeiro: a tela continua carregando o Bruno, sem mostrar o
    // formulário (nem vazio nem o da Ana); a do Bruno chega e o mostra.
    [Fact]
    public void Resposta_do_usuario_anterior_com_o_da_rota_ainda_esperando_nao_mostra_o_formulario()
    {
        var daAna = new TaskCompletionSource<Resultado<IReadOnlyList<UsuarioResumo>>>();
        var doBruno = new TaskCompletionSource<Resultado<IReadOnlyList<UsuarioResumo>>>();
        var tela = EdicaoDaAnaEsperando(daAna.Task);
        Despachante.RespondeDepois<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(doBruno.Task);
        tela.Render(p => p.Add(c => c.Id, (Guid?)IdDoBruno));

        var desenhos = tela.RenderCount;
        daAna.SetResult(Lista());

        tela.WaitForAssertion(() => tela.RenderCount.Should().BeGreaterThan(desenhos));
        tela.Markup.Should().Contain("Carregando…");
        tela.FindAll("form#formulario-do-usuario").Should().BeEmpty();

        doBruno.SetResult(Lista());

        tela.WaitForAssertion(() => Email(tela).Should().Be("bruno@x.com"));
        tela.Markup.Should().NotContain("Carregando…");
    }
```

Em `tests/AtividadesJuridicas.IntegrationTests/UsuariosInterativosTests.cs` (CRLF; da Tarefa 9, com o `Tela()` e os testes do foco ao fechar a confirmação e do aviso de resultado), troque o resumo da classe

```csharp
/// <summary>A lista de usuários, clicando: a confirmação de inativação, o foco ao fechá-la e o aviso de resultado.</summary>
```

por

```csharp
/// <summary>
/// A lista de usuários, clicando: a confirmação de inativação, o foco ao fechá-la e o aviso de resultado. Enquanto a
/// inativação do primeiro envia, os botões das linhas seguem ativos e a confirmação de outro usuário pode abrir no lugar:
/// o resultado do primeiro (sucesso ou falha) não aparece no topo ao lado dela.
/// </summary>
```

e acrescente ao fim da classe (os `using` e o apelido `PaginaDeUsuarios` da Tarefa 9 cobrem tudo):

```csharp

    private static readonly Guid Perfil = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a1");

    private static readonly UsuarioResumo Rafael = new(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a2"),
        "Rafael Teles", "rafael@x.com", Perfil, "Produção", TipoPerfil.Producao, null, true, null);
    private static readonly UsuarioResumo Beatriz = new(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a3"),
        "Beatriz Lins", "beatriz@x.com", Perfil, "Produção", TipoPerfil.Producao, null, true, null);
    private static readonly UsuarioResumo Camila = new(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a4"),
        "Camila Prado", "camila@x.com", Perfil, "Produção", TipoPerfil.Producao, null, true, null);

    // A lista com o Rafael e a Beatriz, ativos e sem pendências; a inativação responde quando o teste quiser.
    private IRenderedComponent<PaginaDeUsuarios> Lista(Task<Resultado> inativacao)
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(Resultado<IReadOnlyList<PerfilDetalhe>>.Ok([]))
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Rafael, Beatriz]))
            .Responde<ObterPendenciasDoUsuario, Resultado<PendenciasDoUsuario>>(
                Resultado<PendenciasDoUsuario>.Ok(new PendenciasDoUsuario(0, 0, 0)))
            .RespondeDepois<DefinirAtivoUsuario, Resultado>(inativacao);
        return Desenhar<PaginaDeUsuarios>();
    }

    // O "Inativar" da linha da tabela do desktop.
    private static void Inativar(IRenderedComponent<PaginaDeUsuarios> tela, string nome) =>
        tela.FindAll("div.aj-so-desktop tbody tr")
            .Single(linha => linha.QuerySelector("td.aj-tabela__principal")!.TextContent == nome)
            .QuerySelectorAll("button").Single(b => b.TextContent == "Inativar").Click();

    private static void Confirmar(IRenderedComponent<PaginaDeUsuarios> tela) =>
        tela.Find("[role=alertdialog]").QuerySelectorAll("button").Single(b => b.TextContent == "Confirmar inativação").Click();

    // A linha do usuário na tabela do desktop.
    private static AngleSharp.Dom.IElement Linha(IRenderedComponent<PaginaDeUsuarios> tela, string nome) =>
        tela.FindAll("div.aj-so-desktop tbody tr").Single(linha => linha.QuerySelector("td.aj-tabela__principal")!.TextContent == nome);

    // C8: a inativação do Rafael fica esperando, e a confirmação da Beatriz abre no lugar da dele. A falha do Rafael
    // chega: a mensagem não vai ao topo, ao lado da confirmação da Beatriz, como se fosse dela.
    [Fact]
    public void Falha_da_inativacao_do_primeiro_nao_aparece_ao_lado_da_confirmacao_do_segundo()
    {
        var inativacao = new TaskCompletionSource<Resultado>();
        var tela = Lista(inativacao.Task);
        Inativar(tela, "Rafael Teles");
        Confirmar(tela);
        Inativar(tela, "Beatriz Lins");
        tela.Find("[role=alertdialog] strong").TextContent.Should().Be("Confirmar a inativação de Beatriz Lins?");

        // A recarga que segue a resposta traz a Camila: é o sinal de que a resposta do Rafael foi tratada.
        Despachante.Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(
            Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Rafael, Beatriz, Camila]));
        inativacao.SetResult(Resultado.Falha(TipoErro.Validacao, "Falha de teste."));

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Camila Prado"));
        tela.FindAll("[role=alertdialog]").Should().ContainSingle()
            .Which.QuerySelector("strong")!.TextContent.Should().Be("Confirmar a inativação de Beatriz Lins?");
        tela.Markup.Should().NotContain("Falha de teste.");
    }

    // C8: o mesmo com o sucesso: "Usuário inativado." não diz o nome e, ao lado da confirmação da Beatriz, pareceria
    // falar dela. A lista recarregada mostra o Rafael inativo.
    [Fact]
    public void Sucesso_da_inativacao_do_primeiro_nao_aparece_ao_lado_da_confirmacao_do_segundo()
    {
        var inativacao = new TaskCompletionSource<Resultado>();
        var tela = Lista(inativacao.Task);
        Inativar(tela, "Rafael Teles");
        Confirmar(tela);
        Inativar(tela, "Beatriz Lins");

        Despachante.Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(
            Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Rafael with { Ativo = false }, Beatriz, Camila]));
        inativacao.SetResult(Resultado.Ok());

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Camila Prado"));
        Linha(tela, "Rafael Teles").QuerySelectorAll("button").Select(b => b.TextContent).Should().Equal("Ativar");
        tela.FindAll("[role=alertdialog]").Should().ContainSingle()
            .Which.QuerySelector("strong")!.TextContent.Should().Be("Confirmar a inativação de Beatriz Lins?");
        tela.Markup.Should().NotContain("Usuário inativado.");
    }

    // C8: abrir outra confirmação limpa a mensagem de resultado que já estava no topo, de sucesso ou de falha.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Abrir_outra_confirmacao_limpa_a_mensagem_de_resultado_anterior(bool sucesso)
    {
        var inativacao = new TaskCompletionSource<Resultado>();
        var tela = Lista(inativacao.Task);
        Inativar(tela, "Rafael Teles");
        Confirmar(tela);
        var mensagem = sucesso ? "Usuário inativado." : "Falha de teste.";
        if (sucesso)
        {
            Despachante.Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(
                Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Rafael with { Ativo = false }, Beatriz]));
        }

        inativacao.SetResult(sucesso ? Resultado.Ok() : Resultado.Falha(TipoErro.Validacao, "Falha de teste."));
        tela.WaitForAssertion(() => tela.Markup.Should().Contain(mensagem));

        Inativar(tela, "Beatriz Lins");

        tela.Find("[role=alertdialog] strong").TextContent.Should().Be("Confirmar a inativação de Beatriz Lins?");
        tela.Markup.Should().NotContain(mensagem);
    }

    // Sem outra confirmação aberta, a falha vai ao topo, como hoje.
    [Fact]
    public void Falha_da_inativacao_sem_outra_confirmacao_aberta_aparece_no_topo()
    {
        var inativacao = new TaskCompletionSource<Resultado>();
        var tela = Lista(inativacao.Task);
        Inativar(tela, "Rafael Teles");
        Confirmar(tela);

        inativacao.SetResult(Resultado.Falha(TipoErro.Validacao, "Falha de teste."));

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Falha de teste."));
        tela.Find("p.aj-aviso.aj-aviso--erro").TextContent.Should().Contain("Falha de teste.");
        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
    }
```

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~UsuarioFormularioInterativoTests"`
Expected: os dois novos falham: no primeiro, o e-mail depois da resposta da Ana é `ana@x.com`; no segundo, o formulário da Ana aparece no lugar de "Carregando…". Os dois da Tarefa 9 passam.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~UsuariosInterativosTests"`
Expected: `Falha_da_inativacao_do_primeiro_nao_aparece_ao_lado_da_confirmacao_do_segundo` e `Sucesso_da_inativacao_do_primeiro_nao_aparece_ao_lado_da_confirmacao_do_segundo` falham (o HTML tem "Falha de teste." e "Usuário inativado."). `Abrir_outra_confirmacao_limpa_a_mensagem_de_resultado_anterior` (os dois casos) e `Falha_da_inativacao_sem_outra_confirmacao_aberta_aparece_no_topo` passam já hoje: o `AlternarAtivoAsync` já limpa a mensagem ao abrir a confirmação, e eles protegem essa limpeza e o caso que não muda. Os três da Tarefa 9 passam.

- [ ] **Step 9: Usuários — a carga confere o usuário da rota (C7) e o resultado não vai ao lado de outra confirmação (C8)**

Em `src/AtividadesJuridicas.Web/Components/Pages/UsuarioFormulario.razor` (CRLF), troque o `CarregarUsuarioAsync` e o começo do `CarregarAsync`:

```csharp
    // Sem os perfis, ou com o acesso encerrado, não há usuário a carregar: fica o estado de antes.
    private async Task CarregarUsuarioAsync()
    {
        if (!_perfisProntos || _estado == EstadoDeCarga.AcessoNegado) return;

        // Na troca de usuário com a tela montada, o formulário fica à vista enquanto o outro chega, como antes.
        if (_estado != EstadoDeCarga.Carregando) _estado = EstadoDeCarga.Pronto;
        _erroDaCarga = null;
        _mensagem = null;
        _inativando = false;
        if (Id is null) Limpar();
        else await CarregarAsync(Id.Value);
        if (_estado == EstadoDeCarga.Carregando) _estado = EstadoDeCarga.Pronto;
    }

    // Não há consulta de um só usuário: ele vem da lista.
    private async Task CarregarAsync(Guid id)
    {
        var lista = await Despachante.EnviarAsync(new ListarUsuarios());
        if (lista.Erro == TipoErro.AcessoNegado)
```

por

```csharp
    // Sem os perfis, ou com o acesso encerrado, não há usuário a carregar: fica o estado de antes.
    private async Task CarregarUsuarioAsync()
    {
        if (!_perfisProntos || _estado == EstadoDeCarga.AcessoNegado) return;

        // Na troca de usuário com a tela montada, o formulário fica à vista enquanto o outro chega, como antes.
        if (_estado != EstadoDeCarga.Carregando) _estado = EstadoDeCarga.Pronto;
        _erroDaCarga = null;
        _mensagem = null;
        _inativando = false;
        if (Id is not { } id) Limpar();
        else
        {
            await CarregarAsync(id);
            // A rota passou a outro usuário durante a espera: o estado da tela é o da carga dele, não o desta.
            if (Id != id) return;
        }
        if (_estado == EstadoDeCarga.Carregando) _estado = EstadoDeCarga.Pronto;
    }

    // Não há consulta de um só usuário: ele vem da lista. Se a rota passou a outro usuário durante a espera, a resposta
    // é descartada: a carga do outro já está em curso (ou terminou) e é ela que vale.
    private async Task CarregarAsync(Guid id)
    {
        var lista = await Despachante.EnviarAsync(new ListarUsuarios());
        if (Id != id) return;

        if (lista.Erro == TipoErro.AcessoNegado)
```

O resto do `CarregarAsync` não muda.

Em `src/AtividadesJuridicas.Web/Components/Pages/Usuarios.razor` (CRLF), troque o `AoInativarAsync` e o `ConcluirAsync` (este como a Tarefa 9 o deixou, com o `_versaoDoAviso++` a cada mensagem nova). O `AlternarAtivoAsync` não muda: ao abrir outra confirmação, ele já limpa a mensagem que estava no topo (`_mensagem = null; _falhou = false;`).

```csharp
    private async Task AoInativarAsync(UsuarioResumo alvo, Resultado resultado)
    {
        // Os botões das linhas seguem ativos durante o envio: se outra confirmação foi aberta nesse intervalo, ela fica.
        if (_inativando?.Id == alvo.Id) _inativando = null;
        await ConcluirAsync(resultado, "Usuário inativado.");
    }
```

por

```csharp
    private async Task AoInativarAsync(UsuarioResumo alvo, Resultado resultado)
    {
        // Os botões das linhas seguem ativos durante o envio: se outra confirmação foi aberta nesse intervalo, ela fica,
        // e o resultado deste (sucesso ou falha) não vai ao topo, ao lado dela, como se fosse dela: a confirmação aberta
        // limpou a mensagem anterior, e a lista recarregada mostra a situação dele.
        var outraAberta = _inativando is { } aberta && aberta.Id != alvo.Id;
        if (!outraAberta) _inativando = null;
        await ConcluirAsync(resultado, "Usuário inativado.", calarResultado: outraAberta);
    }
```

e

```csharp
    private async Task ConcluirAsync(Resultado resultado, string mensagemDeSucesso)
    {
        if (resultado.Erro == TipoErro.AcessoNegado)
        {
            _autorizado = false;
            return;
        }

        _falhou = !resultado.Sucesso;
        _mensagem = resultado.Sucesso ? mensagemDeSucesso : resultado.Mensagem;
        _versaoDoAviso++;

        // A ação deu certo; se só a recarga falhou, o aviso diz isso e a lista anterior fica na tela.
        if (!await RecarregarAsync() && _autorizado && resultado.Sucesso)
        {
            _falhou = true;
            _mensagem = $"{mensagemDeSucesso.TrimEnd('.')}, mas não foi possível atualizar a lista.";
            _versaoDoAviso++;
        }
    }
```

por

```csharp
    // calarResultado: a confirmação de outro usuário está aberta no topo; o resultado (e o aviso da recarga que falhou)
    // não vai para lá. A lista recarrega do mesmo jeito, e o acesso encerrado segue como sempre.
    private async Task ConcluirAsync(Resultado resultado, string mensagemDeSucesso, bool calarResultado = false)
    {
        if (resultado.Erro == TipoErro.AcessoNegado)
        {
            _autorizado = false;
            return;
        }

        if (!calarResultado)
        {
            _falhou = !resultado.Sucesso;
            _mensagem = resultado.Sucesso ? mensagemDeSucesso : resultado.Mensagem;
            _versaoDoAviso++;
        }

        // A ação deu certo; se só a recarga falhou, o aviso diz isso e a lista anterior fica na tela.
        if (!await RecarregarAsync() && _autorizado && resultado.Sucesso && !calarResultado)
        {
            _falhou = true;
            _mensagem = $"{mensagemDeSucesso.TrimEnd('.')}, mas não foi possível atualizar a lista.";
            _versaoDoAviso++;
        }
    }
```

A ativação (`AlternarAtivoAsync`) continua chamando `ConcluirAsync(ativado, "Usuário ativado.")`, sem calar. O resultado calado não muda o `_versaoDoAviso`: nenhum aviso aparece nem se revela, e o foco fica na confirmação aberta.

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run, um por vez: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~UsuarioFormularioInterativoTests"`, `… --filter "FullyQualifiedName~UsuariosInterativosTests"`, `… --filter "FullyQualifiedName~UsuariosWebTests"`, `… --filter "FullyQualifiedName~CargaDoFormularioInterativaTests"`
Expected: todos passando.

- [ ] **Step 10: Commit dos usuários**

Confira o branch e:

```
git add src/AtividadesJuridicas.Web/Components/Pages/UsuarioFormulario.razor src/AtividadesJuridicas.Web/Components/Pages/Usuarios.razor tests/AtividadesJuridicas.IntegrationTests/UsuarioFormularioInterativoTests.cs tests/AtividadesJuridicas.IntegrationTests/UsuariosInterativosTests.cs
git commit -F <arquivo com a mensagem, UTF-8 sem BOM>
```

Mensagem:

```
fix(web): o formulário de usuário descarta a carga atrasada de outro usuário, e a lista não mostra o resultado de uma inativação ao lado da confirmação de outra

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

- [ ] **Step 11: Troca de revisor — teste da barra do desktop (E10, deve falhar)**

Em `tests/AtividadesJuridicas.IntegrationTests/AtividadeResponsaveisWebTests.cs` (CRLF):

No teste `Atividade_que_permite_redefinir_traz_a_tela_inteira_com_as_opcoes_o_direcionamento_e_o_revisor`, logo depois da asserção sobre `BlocoDeVisao.De(html, "aj-so-desktop")` (a que procura `<div class="aj-acoes aj-acoes--fixa">` com "Salvar responsáveis"), acrescente:

```csharp
        // Na tela inteira, a barra do desktop tem a largura do conteúdo (o lado a lado), não a do painel médio.
        BlocoDeVisao.De(html, "aj-so-desktop").Should().NotContain("aj-acoes--medio");
```

No teste `Atividade_feita_abre_so_a_troca_de_revisor_com_o_atual_escolhido`, troque

```csharp
        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            "(?s)<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-de-responsaveis\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Trocar revisor</button>");
```

por

```csharp
        // E10: o painel é o médio, e a barra do desktop acompanha a largura dele, como no devolver e no reabrir.
        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            $"(?s)<div class=\"aj-acoes aj-acoes--fixa aj-acoes--medio\">\\s*<a href=\"/atividades/{_recurso}\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>\\s*" +
            "<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-de-responsaveis\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Trocar revisor</button>");
        BlocoDeVisao.De(html, "aj-so-tablet").Should().Contain("<div class=\"aj-acoes aj-acoes--fixa\">")
            .And.NotContain("aj-acoes--medio");
```

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadeResponsaveisWebTests"`
Expected: `Atividade_feita_abre_so_a_troca_de_revisor_com_o_atual_escolhido` falha (a barra do desktop é `aj-acoes aj-acoes--fixa`); os outros passam.

- [ ] **Step 12: Troca de revisor — `aj-acoes--medio` no desktop (E10)**

Em `src/AtividadesJuridicas.Web/Components/Pages/AtividadeResponsaveis.razor` (CRLF), troque

```razor
        <div class="aj-so-desktop">
            <div class="aj-acoes aj-acoes--fixa"><a href="@Voltar" class="aj-botao aj-botao--fantasma">Cancelar</a>
```

por

```razor
        <div class="aj-so-desktop">
            <div class="@ClasseDaBarraDoDesktop"><a href="@Voltar" class="aj-botao aj-botao--fantasma">Cancelar</a>
```

(o resto da linha não muda) e, no `@code`, depois de `ClasseDoRevisor`:

```csharp
    // Na troca de revisor o painel é o médio, e a barra do desktop acompanha a largura dele (como no devolver e no
    // reabrir); na tela inteira, a barra tem a largura do conteúdo. As duas continuam fixas ao pé.
    private string ClasseDaBarraDoDesktop => ModoDoRevisor ? "aj-acoes aj-acoes--fixa aj-acoes--medio" : "aj-acoes aj-acoes--fixa";
```

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadeResponsaveisWebTests"`
Expected: todos passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadeResponsaveisInterativosTests"`
Expected: todos passando.

Navegador (Tarefa 11): a barra do desktop da troca de revisor com 680px, alinhada ao painel.

- [ ] **Step 13: Commit da troca de revisor**

Confira o branch e:

```
git add src/AtividadesJuridicas.Web/Components/Pages/AtividadeResponsaveis.razor tests/AtividadesJuridicas.IntegrationTests/AtividadeResponsaveisWebTests.cs
git commit -F <arquivo com a mensagem, UTF-8 sem BOM>
```

Mensagem:

```
style(web): a barra do desktop da troca de revisor acompanha a largura do painel médio

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

- [ ] **Step 14: Agenda e avatar — testes reescritos sem o código só dos testes e testes das redações (F1, F2, F3, devem falhar)**

O código que só os testes usam (F1) e os testes que o usam, todos reescritos abaixo sobre o código de produção:

| Teste | Usa | Reescrita |
|---|---|---|
| `AgendaWebTests.Aviso_de_sobreposicao_diz_que_o_evento_foi_salvo_e_lista_cada_pessoa_no_horario_do_escritorio` | `MensagemDeSalvo` (2×) | o `AvisoDeEventoSalvo` desenhado, com e sem sobreposição, e o `ConflitoNoDesktop` |
| `AgendaWebTests.Aviso_de_sobreposicao_que_atravessa_a_meia_noite_mostra_a_data_do_fim` | `Sobreposicao` (2×) | `ConflitoNoTablet` (a frase de cada pessoa, sem o "Atenção:") |
| `AgendaWebTests.Formatos_de_hora_e_data_nao_dependem_da_cultura_da_thread` | `Sobreposicao` (1×) | `ConflitoNoTablet` |
| `AgendaPuraTests.Conflito_com_duas_pessoas_separa_por_ponto_e_virgula` | `MensagemDeSalvo` (1×) | o `AvisoDeEventoSalvo` desenhado: a frase do desktop é "Evento salvo." em destaque e a redação do desktop |
| `AvatarDePessoaTests.Classe_de_outra_pessoa_e_a_de_sempre` (em `TelasPurasTests.cs`) | `Classe(nome, proprio: false)` | `Classes(nome, proprio: false)` |
| `AvatarDePessoaTests.Classe_do_proprio_usuario_e_a_do_avatar_dourado_sem_cor` (em `TelasPurasTests.cs`) | `Classe(nome, proprio: true)` | `Classes(nome, proprio: true)` |

Confira com a ferramenta Grep, em `src` e `tests` (`*.{cs,razor}`), `MensagemDeSalvo|RotulosDeAgenda\.Sobreposicao\(|Classe\([^()]*,\s*proprio`: só as oito linhas dos seis testes da tabela (`AgendaWebTests.cs` 5, `AgendaPuraTests.cs` 1, `TelasPurasTests.cs` 2) e as três definições (`RotulosDeAgenda.cs` e `AvatarDePessoa.cs`). Se aparecer outro uso, pare e relate.

**`tests/AtividadesJuridicas.IntegrationTests/AgendaWebTests.cs`** (CRLF). Acrescente aos `using` (na ordem alfabética, depois de `using AtividadesJuridicas.Web.Components.Agenda;`):

```csharp
using AtividadesJuridicas.Web.Components.Layout;
```

Troque o teste inteiro

```csharp
    [Fact]
    public void Aviso_de_sobreposicao_diz_que_o_evento_foi_salvo_e_lista_cada_pessoa_no_horario_do_escritorio()
    {
        var sobreposicoes = new[]
        {
            new SobreposicaoDeAgenda(Guid.NewGuid(), "Pedro", _fuso.ParaUtc(new DateTime(2026, 10, 7, 10, 0, 0)),
                _fuso.ParaUtc(new DateTime(2026, 10, 7, 11, 30, 0))),
        };

        RotulosDeAgenda.MensagemDeSalvo(_fuso, sobreposicoes).Should()
            .Be("Evento salvo. Atenção: Pedro já tem evento das 10:00 às 11:30 (07/10).");
        RotulosDeAgenda.MensagemDeSalvo(_fuso, []).Should().Be("Evento salvo.");
    }
```

por

```csharp
    [Fact]
    public async Task Aviso_de_sobreposicao_diz_que_o_evento_foi_salvo_e_lista_cada_pessoa_no_horario_do_escritorio()
    {
        var sobreposicoes = new[]
        {
            new SobreposicaoDeAgenda(Guid.NewGuid(), "Pedro", _fuso.ParaUtc(new DateTime(2026, 10, 7, 10, 0, 0)),
                _fuso.ParaUtc(new DateTime(2026, 10, 7, 11, 30, 0))),
        };

        // O aviso que a agenda mostra depois de salvar: com sobreposição, a frase do desktop diz que o evento foi salvo e
        // lista a pessoa no horário do escritório; sem sobreposição, só o recado.
        var comConflito = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<AvisoDeEventoSalvo>(
            new DespachanteCombinado(), new() { ["Recado"] = new Recado("Evento salvo.", TipoDeAviso.Sucesso, sobreposicoes) }));
        var semConflito = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<AvisoDeEventoSalvo>(
            new DespachanteCombinado(), new() { ["Recado"] = new Recado("Evento salvo.", TipoDeAviso.Sucesso, []) }));

        RotulosDeAgenda.ConflitoNoDesktop(_fuso, sobreposicoes).Should().Be("Atenção: Pedro já tem evento das 10:00 às 11:30 (07/10).");
        BlocoDeVisao.De(comConflito, "aj-so-desktop").Should()
            .Contain("<strong>Evento salvo.</strong> Atenção: Pedro já tem evento das 10:00 às 11:30 (07/10).");
        semConflito.Should().Contain("<span>Evento salvo.</span>").And.NotContain("Atenção");
    }
```

No teste `Aviso_de_sobreposicao_que_atravessa_a_meia_noite_mostra_a_data_do_fim`, troque as duas asserções

```csharp
        RotulosDeAgenda.Sobreposicao(_fuso, atravessa).Should().Be("Pedro já tem evento de 10/03 22:00 a 11/03 01:00");
        RotulosDeAgenda.Sobreposicao(_fuso, ateMeiaNoite).Should().Be("Paula já tem evento das 22:00 às 00:00 (10/03)");
```

por

```csharp
        // A frase de cada pessoa como o tablet a mostra (a do desktop é a mesma depois de "Atenção:").
        RotulosDeAgenda.ConflitoNoTablet(_fuso, [atravessa]).Should().Be("Pedro já tem evento de 10/03 22:00 a 11/03 01:00.");
        RotulosDeAgenda.ConflitoNoTablet(_fuso, [ateMeiaNoite]).Should().Be("Paula já tem evento das 22:00 às 00:00 (10/03).");
```

No teste `Formatos_de_hora_e_data_nao_dependem_da_cultura_da_thread`, troque

```csharp
            RotulosDeAgenda.Sobreposicao(_fuso, new SobreposicaoDeAgenda(Guid.NewGuid(), "Pedro", evento.InicioEm,
                evento.InicioEm.AddMinutes(30))).Should().Be("Pedro já tem evento das 23:30 às 00:00 (07/10)");
```

por

```csharp
            RotulosDeAgenda.ConflitoNoTablet(_fuso, [new SobreposicaoDeAgenda(Guid.NewGuid(), "Pedro", evento.InicioEm,
                evento.InicioEm.AddMinutes(30))]).Should().Be("Pedro já tem evento das 23:30 às 00:00 (07/10).");
```

**`tests/AtividadesJuridicas.IntegrationTests/AgendaPuraTests.cs`** (CRLF). Acrescente aos `using`: `using System.Net;` (depois de `using System.Globalization;`) e `using AtividadesJuridicas.Web.Components.Layout;` (depois de `using AtividadesJuridicas.Web.Components.Agenda;`).

No teste `Conflito_com_duas_pessoas_separa_por_ponto_e_virgula`, troque a assinatura `public void Conflito_com_duas_pessoas_separa_por_ponto_e_virgula()` por `public async Task Conflito_com_duas_pessoas_separa_por_ponto_e_virgula()` e as duas últimas linhas

```csharp
        // A mensagem de hoje é a redação do desktop depois de "Evento salvo.".
        RotulosDeAgenda.MensagemDeSalvo(_fuso, duas).Should().Be("Evento salvo. " + RotulosDeAgenda.ConflitoNoDesktop(_fuso, duas));
```

por

```csharp
        // O aviso do desktop é a redação do desktop depois de "Evento salvo." em destaque.
        var aviso = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<AvisoDeEventoSalvo>(
            new DespachanteCombinado(), new() { ["Recado"] = new Recado("Evento salvo.", TipoDeAviso.Sucesso, duas) }));
        BlocoDeVisao.De(aviso, "aj-so-desktop").Should()
            .Contain("<strong>Evento salvo.</strong> " + RotulosDeAgenda.ConflitoNoDesktop(_fuso, duas));
```

Antes do teste `Formatos_novos_nao_dependem_da_cultura_da_thread`, acrescente o teste das redações novas (F3):

```csharp
    // F3: o nome acessível do "+N" da célula do Mês (com o plural pela contagem) e os vínculos que o usuário não vê,
    // na ficha e no formulário do evento, são textos da agenda.
    [Fact]
    public void Rotulo_do_mais_do_mes_e_vinculos_indisponiveis()
    {
        RotulosDeAgenda.RotuloDoMais(new DateOnly(2026, 10, 8), 2).Should().Be("Mais 2 eventos em 08/10");
        RotulosDeAgenda.RotuloDoMais(new DateOnly(2026, 10, 9), 1).Should().Be("Mais 1 evento em 09/10");
        RotulosDeAgenda.RotuloDoMais(new DateOnly(2027, 1, 31), 12).Should().Be("Mais 12 eventos em 31/01");
        RotulosDeAgenda.ClienteIndisponivel.Should().Be("Cliente indisponível");
        RotulosDeAgenda.ProcessoIndisponivel.Should().Be("Processo indisponível");
    }
```

e, dentro do `try` de `Formatos_novos_nao_dependem_da_cultura_da_thread`, depois da linha do `ConflitoNoMobile`:

```csharp
            RotulosDeAgenda.RotuloDoMais(Hoje, 2).Should().Be("Mais 2 eventos em 08/10");
```

**`tests/AtividadesJuridicas.IntegrationTests/TelasPurasTests.cs`** (CRLF), classe `AvatarDePessoaTests`. Troque os dois testes

```csharp
    // O "você" da agenda é o avatar dourado: o .aj-avatar sem modificador de cor.
    [Theory]
    [InlineData("Rafael Teles", "aj-avatar--azul")]
    [InlineData("Beatriz Lins", "aj-avatar--lilas")]
    public void Classe_de_outra_pessoa_e_a_de_sempre(string nome, string classe)
    {
        AvatarDePessoa.Classe(nome, proprio: false).Should().Be(classe);
    }

    [Theory]
    [InlineData("Rafael Teles")]
    [InlineData("Marina Couto")]
    [InlineData("")]
    public void Classe_do_proprio_usuario_e_a_do_avatar_dourado_sem_cor(string nome)
    {
        AvatarDePessoa.Classe(nome, proprio: true).Should().BeEmpty();
    }
```

por

```csharp
    // O "você" da agenda é o avatar dourado: o .aj-avatar sem modificador de cor. Os outros levam a cor de sempre, a
    // do nome.
    [Theory]
    [InlineData("Rafael Teles", "aj-avatar--azul")]
    [InlineData("Beatriz Lins", "aj-avatar--lilas")]
    public void Classes_de_outra_pessoa_levam_a_cor_de_sempre_do_nome(string nome, string classe)
    {
        AvatarDePessoa.Classe(nome).Should().Be(classe);
        AvatarDePessoa.Classes(nome, proprio: false).Should().Be($"aj-avatar {AvatarDePessoa.Classe(nome)}");
    }

    [Theory]
    [InlineData("Rafael Teles")]
    [InlineData("Marina Couto")]
    [InlineData("")]
    public void Classes_do_proprio_usuario_sao_as_do_avatar_dourado_qualquer_que_seja_o_nome(string nome)
    {
        AvatarDePessoa.Classes(nome, proprio: true).Should().Be("aj-avatar");
    }
```

**`tests/AtividadesJuridicas.IntegrationTests/AvisoDeEventoSalvoTests.cs`** (CRLF). Depois de `Com_sobreposicoes_traz_uma_frase_de_atencao_por_visao`, acrescente (F2):

```csharp
    // F2: o destaque do desktop e do tablet é o texto do recado, não um "Evento salvo." escrito à mão no componente.
    [Fact]
    public async Task Com_sobreposicoes_o_destaque_do_desktop_e_do_tablet_e_o_texto_do_recado()
    {
        var html = await DesenharAsync(new Recado("Texto do recado.", TipoDeAviso.Sucesso,
            [Sobreposicao("Rafael Teles", new DateTime(2026, 10, 7, 10, 0, 0), new DateTime(2026, 10, 7, 11, 30, 0))]));

        BlocoDeVisao.De(html, "aj-so-desktop").Should()
            .Contain("<strong>Texto do recado.</strong> Atenção: Rafael Teles já tem evento das 10:00 às 11:30 (07/10).");
        BlocoDeVisao.De(html, "aj-so-tablet").Should()
            .Contain("<strong>Texto do recado.</strong> Rafael Teles já tem evento das 10:00 às 11:30 (07/10).");
        html.Should().NotContain("Evento salvo.");
    }
```

Run: `dotnet build -m:1`
Expected: o build falha só em `AgendaPuraTests.cs`, com CS0117 (`RotulosDeAgenda` não contém `RotuloDoMais`, `ClienteIndisponivel`, `ProcessoIndisponivel`). Os outros arquivos compilam (os testes reescritos usam só o que já existe).

- [ ] **Step 15: As redações em `RotulosDeAgenda`, o recado no destaque, e sai o código só dos testes (F1, F2, F3)**

**`src/AtividadesJuridicas.Web/Components/Agenda/RotulosDeAgenda.cs`** (CRLF):

1. Depois de `TotalDeEventos` (seção "Dias, semanas e meses"), acrescente:

```csharp

    /// <summary>
    /// O nome acessível do "+N" da célula do Mês, com os eventos que não couberam: "Mais 2 eventos em 08/10",
    /// "Mais 1 evento em 09/10".
    /// </summary>
    public static string RotuloDoMais(DateOnly dia, int outros) =>
        $"Mais {TotalDeEventos(outros)} em {dia.ToString("dd/MM", Cultura)}";
```

2. Na seção "Ficha", depois de `Duracao`, acrescente:

```csharp

    /// <summary>O cliente vinculado que o usuário não pode ver (o servidor manda o nome nulo), na ficha e no formulário.</summary>
    public const string ClienteIndisponivel = "Cliente indisponível";

    /// <summary>O processo vinculado que o usuário não pode ver (o servidor manda o título nulo), na ficha e no formulário.</summary>
    public const string ProcessoIndisponivel = "Processo indisponível";
```

3. Na seção "Sobreposições", apague estes dois membros e a linha em branco depois de cada um:

```csharp
    /// <summary>
    /// "Fulano já tem evento das 10:00 às 11:30 (07/10)"; se o intervalo atravessa a meia-noite, as duas datas aparecem:
    /// "Fulano já tem evento de 10/03 22:00 a 11/03 01:00". Quem termina à meia-noite em ponto não atravessa.
    /// </summary>
    public static string Sobreposicao(FusoDoEscritorio fuso, SobreposicaoDeAgenda s) => Frase(fuso, s, noMobile: false);

    /// <summary>A mensagem depois de gravar: o evento foi salvo; a sobreposição é só um aviso.</summary>
    public static string MensagemDeSalvo(FusoDoEscritorio fuso, IReadOnlyList<SobreposicaoDeAgenda> sobreposicoes) =>
        sobreposicoes.Count == 0 ? "Evento salvo." : $"Evento salvo. {ConflitoNoDesktop(fuso, sobreposicoes)}";
```

e leve a regra da frase para um comentário logo acima de `private static string Frase(`:

```csharp
    // "Fulano já tem evento das 10:00 às 11:30" e a data; se o intervalo atravessa a meia-noite, as duas datas aparecem:
    // "Fulano já tem evento de 10/03 22:00 a 11/03 01:00". Quem termina à meia-noite em ponto não atravessa.
```

O `using AtividadesJuridicas.Application.Agenda;` continua (os `ConflitoNo…` usam `SobreposicaoDeAgenda`).

**`src/AtividadesJuridicas.Web/Components/Layout/AvatarDePessoa.cs`** (CRLF): apague o método de dois argumentos e o comentário dele (e a linha em branco antes):

```csharp

    /// <summary>
    /// A classe de cor do avatar na lista de participantes da agenda: o usuário logado ("você") leva o avatar dourado,
    /// que é o <c>aj-avatar</c> sem modificador de cor (classe vazia); os demais, a cor do nome.
    /// </summary>
    public static string Classe(string nome, bool proprio) => proprio ? string.Empty : Classe(nome);
```

O `Classe(string nome)` (produção: `Atividades.razor`, `AtividadeFicha.razor`, `ComentariosDaAtividade.razor`) e o `Classes(nome, proprio)` ficam.

**`src/AtividadesJuridicas.Web/Components/Agenda/MesDaAgenda.razor`** (CRLF): no botão do "+N", troque `aria-label="@RotuloDoMais(dia, doDia.Count - MaximoPorDia)"` por `aria-label="@RotulosDeAgenda.RotuloDoMais(dia, doDia.Count - MaximoPorDia)"` e apague, no `@code`, o método privado e a linha em branco antes dele:

```csharp

    private static string RotuloDoMais(DateOnly dia, int outros)
    {
        var data = dia.ToString("dd'/'MM", System.Globalization.CultureInfo.InvariantCulture);
        return outros == 1 ? $"Mais 1 evento em {data}" : $"Mais {outros} eventos em {data}";
    }
```

O texto à vista do botão (`@($"+{doDia.Count - MaximoPorDia}")`) não muda.

**`src/AtividadesJuridicas.Web/Components/Pages/EventoFicha.razor`** (CRLF), em `Detalhes`: `e.ClienteNome ?? "Cliente indisponível"` → `e.ClienteNome ?? RotulosDeAgenda.ClienteIndisponivel`; `e.ProcessoTitulo ?? "Processo indisponível"` → `e.ProcessoTitulo ?? RotulosDeAgenda.ProcessoIndisponivel`.

**`src/AtividadesJuridicas.Web/Components/Pages/EventoFormulario.razor`** (CRLF), na carga da edição: `atual.ClienteNome ?? "Cliente indisponível"` → `atual.ClienteNome ?? RotulosDeAgenda.ClienteIndisponivel`; `atual.ProcessoTitulo ?? "Processo indisponível"` → `atual.ProcessoTitulo ?? RotulosDeAgenda.ProcessoIndisponivel`.

**`src/AtividadesJuridicas.Web/Components/Agenda/AvisoDeEventoSalvo.razor`** (CRLF): nas duas linhas do desktop e do tablet, `<strong>Evento salvo.</strong>` → `<strong>@Recado.Texto</strong>`; no comentário do topo, troque `o desktop com "Evento salvo." em destaque e "Atenção:"` por `o desktop com o recado ("Evento salvo.") em destaque e "Atenção:"`.

Confira de novo com a ferramenta Grep, em `src` e `tests` (`*.{cs,razor}`): `MensagemDeSalvo|RotulosDeAgenda\.Sobreposicao\(|Classe\([^()]*,\s*proprio` → nada; `"Cliente indisponível"|"Processo indisponível"` em `src` → só as duas constantes de `RotulosDeAgenda.cs`; `InvariantCulture` em `Components/Agenda/MesDaAgenda.razor` → nada.

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run, um por vez:
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AgendaPuraTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AvatarDePessoaTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AvisoDeEventoSalvoTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AgendaWebTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~MesDaAgendaTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~EventoFichaInterativaTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~EventoFormularioInterativoTests"`

Expected: todos passando (os de `MesDaAgendaTests` provam que os nomes acessíveis do "+N" não mudaram; os da ficha e do formulário, que "Cliente indisponível" e "Processo indisponível" continuam à vista). Antes deste passo, `Com_sobreposicoes_o_destaque_do_desktop_e_do_tablet_e_o_texto_do_recado` falharia com `<strong>Evento salvo.</strong>`.

- [ ] **Step 16: Commit da limpeza de código da agenda**

Confira o branch e:

```
git add src/AtividadesJuridicas.Web/Components/Agenda/RotulosDeAgenda.cs src/AtividadesJuridicas.Web/Components/Agenda/MesDaAgenda.razor src/AtividadesJuridicas.Web/Components/Agenda/AvisoDeEventoSalvo.razor src/AtividadesJuridicas.Web/Components/Layout/AvatarDePessoa.cs src/AtividadesJuridicas.Web/Components/Pages/EventoFicha.razor src/AtividadesJuridicas.Web/Components/Pages/EventoFormulario.razor tests/AtividadesJuridicas.IntegrationTests/AgendaWebTests.cs tests/AtividadesJuridicas.IntegrationTests/AgendaPuraTests.cs tests/AtividadesJuridicas.IntegrationTests/TelasPurasTests.cs tests/AtividadesJuridicas.IntegrationTests/AvisoDeEventoSalvoTests.cs
git commit -F <arquivo com a mensagem, UTF-8 sem BOM>
```

Mensagem:

```
refactor(web): tira da agenda e do avatar o código que só os testes usavam e leva os textos da agenda a RotulosDeAgenda

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

- [ ] **Step 17: Mês, Dia e Semana — testes dos componentes (F9, F11, devem falhar)**

**`tests/AtividadesJuridicas.IntegrationTests/DiaDaAgendaTests.cs`** (CRLF). Acrescente ao fim da classe:

```csharp
    // F11: o "+N" do Mês leva o foco ao título do Dia. O título do desktop e o da seção do tablet aceitam o foco por
    // programa (tabindex -1, fora da ordem de tabulação); o da lista do mobile não precisa (o mobile não tem "+N").
    [Fact]
    public void Titulos_do_dia_no_desktop_e_no_tablet_aceitam_o_foco_por_programa()
    {
        var dia = Dia(QuadroDaAgenda.Hoje, QuadroDaAgenda.Semana);

        Desktop(dia).QuerySelector("h2.aj-dia__cabeca")!.GetAttribute("tabindex").Should().Be("-1");
        Tablet(dia).QuerySelector("h2.aj-secao-dia__cabeca")!.GetAttribute("tabindex").Should().Be("-1");
        var doMobile = Mobile(dia).QuerySelector("div.aj-dia-escolhido > h2")!;
        doMobile.TextContent.Should().Be("Quinta, 08/10 · hoje");
        doMobile.HasAttribute("tabindex").Should().BeFalse();
    }
```

**`tests/AtividadesJuridicas.IntegrationTests/SemanaDaAgendaTests.cs`** (CRLF). No teste `Tablet_e_mobile_tem_uma_secao_por_dia_com_os_mesmos_eventos_e_a_sigla_hoje`, logo depois da linha que confere os `aria-label` dos `h2.aj-secao-dia__cabeca` com `RotulosDaSemana`, acrescente:

```csharp
        // Na Semana ninguém leva o foco ao título da seção: sem tabindex (o do Dia do tablet o tem, F11).
        secoes.Should().OnlyContain(s => !s.QuerySelector("h2.aj-secao-dia__cabeca")!.HasAttribute("tabindex"));
```

**`tests/AtividadesJuridicas.IntegrationTests/MesDaAgendaTests.cs`** (CRLF). Na classe `MesDaAgendaTests`, depois de `No_mobile_a_grade_compacta_vem_antes_da_lista_do_dia_e_tocar_num_dia_avisa_a_pagina`, acrescente:

```csharp
    // F9: com a consulta de outro mês em curso, o mobile mantém a grade compacta (as semanas do mês novo, sem os pontos)
    // e espera a lista do dia; o desktop e o tablet mostram "Carregando…" no lugar da grade grande.
    [Fact]
    public void Carregando_mantem_a_grade_compacta_sem_pontos_e_espera_a_lista_e_a_grade_grande()
    {
        var mes = Desenhar<MesDaAgenda>(p => p
            .Add(c => c.Periodo, new PeriodoDaAgenda(VisaoDaAgenda.Mes, QuadroDaAgenda.Hoje, true))
            .Add(c => c.Eventos, QuadroDaAgenda.Semana)
            .Add(c => c.Hoje, QuadroDaAgenda.Hoje)
            .Add(c => c.Carregando, true));

        var mobile = mes.Find("div.aj-so-mobile");
        mobile.Children.Select(c => c.ClassName).Should().Equal("aj-faixa-dias aj-faixa-dias--mes", "aj-subtitulo");
        mobile.QuerySelectorAll("div.aj-faixa-dias--mes > button.aj-faixa-dias__dia").Should().HaveCount(35);
        mobile.QuerySelectorAll("span.aj-pontos-evento > span").Should().BeEmpty();
        var carregando = mobile.QuerySelector("p.aj-subtitulo")!;
        carregando.GetAttribute("role").Should().Be("status");
        carregando.TextContent.Should().Be("Carregando…");

        var grande = mes.Find("div.aj-desktop-tablet");
        grande.Children.Select(c => c.ClassName).Should().Equal("aj-subtitulo");
        grande.QuerySelector("p.aj-subtitulo[role=status]")!.TextContent.Should().Be("Carregando…");
        mes.FindAll(".aj-mes, .aj-dia-escolhido").Should().BeEmpty();
    }
```

Run: `dotnet build -m:1`
Expected: o build falha em `MesDaAgendaTests.cs`, com CS1061 (`MesDaAgenda` não contém `Carregando`). Antes de seguir, confira que é o único erro.

- [ ] **Step 18: Página da agenda — testes do anúncio, do grupo, da grade na carga, do erro e do foco no título do Dia (F4, F8, F9, F11, F13, devem falhar)**

**`tests/AtividadesJuridicas.IntegrationTests/AgendaInterativaTests.cs`** (CRLF).

Junto dos outros auxiliares, depois de `CampoDoPainel`, acrescente:

```csharp
    // O seletor do título do Dia que a agenda pede ao módulo de foco (o do desktop e o da seção do tablet: o módulo foca
    // o que está à vista). Os pedidos se leem pelo SeletoresFocados da TelaInterativa, que já monta o módulo de foco.
    private const string TituloDoDia = ".aj-dia__cabeca, .aj-secao-dia__cabeca";

    // Cinco eventos em 14/10: a célula do Mês mostra três e o "+2".
    private static EventoResumo[] CincoEm14() =>
    [
        Evento("Primeiro", Local(2026, 10, 14, 9)), Evento("Segundo", Local(2026, 10, 14, 10)),
        Evento("Terceiro", Local(2026, 10, 14, 11)), Evento("Quarto", Local(2026, 10, 14, 12)),
        Evento("Quinto", Local(2026, 10, 14, 13)),
    ];
```

No teste `Falha_da_consulta_mostra_o_erro_sem_dias_e_tentar_de_novo_consulta_o_mesmo_periodo`, logo depois de `tela.FindAll(Desktop).Should().ContainSingle();`, acrescente (F4):

```csharp
        // O erro com "Tentar de novo" é o das listas (EstadoDaLista), não uma cópia da marcação dele.
        tela.FindComponents<EstadoDaLista>().Should().ContainSingle().Which.Instance.Erro.Should().Be("Falha de teste.");
```

Ao fim da classe, acrescente:

```csharp
    // F13: as setas e o "Hoje" de cada largura num grupo com nome, como o segmentado das visões.
    [Fact]
    public void Setas_e_hoje_de_cada_largura_ficam_num_grupo_chamado_periodo()
    {
        var tela = Agenda("/agenda?data=2026-10-08");

        static string[] Botoes(IElement grupo) =>
            [.. grupo.QuerySelectorAll("button").Select(b => b.GetAttribute("aria-label") ?? Texto(b))];

        tela.FindAll("[role=group][aria-label='Período']").Should().HaveCount(3);
        Botoes(tela.Find($"{Desktop} [role=group][aria-label='Período']")).Should().Equal("Período anterior", "Hoje", "Próximo período");
        Botoes(tela.Find($"{Tablet} [role=group][aria-label='Período']")).Should().Equal("Semana anterior", "Hoje", "Próxima semana");
        var mobile = tela.Find(Mobile);
        mobile.GetAttribute("role").Should().Be("group");
        mobile.GetAttribute("aria-label").Should().Be("Período");
        Botoes(mobile).Should().Equal("Semana anterior", "Próxima semana");
    }

    // F8: a troca de período deixa a seta no lugar (o foco fica nela) e anuncia o período novo numa região só para
    // leitor de cada largura, com o texto do subtítulo do período daquela largura; o anúncio muda já na carga.
    [Fact]
    public void Troca_de_periodo_anuncia_o_periodo_novo_e_a_seta_continua_na_tela_durante_a_carga()
    {
        const string Anuncio = "div.aj-desktop-tablet > p.aj-so-leitor[role=status]";
        const string AnuncioDoMobile = "div.aj-so-mobile > p.aj-so-leitor[role=status]";
        var tela = Agenda("/agenda?data=2026-10-08");
        Texto(tela.Find(Anuncio)).Should().Be("Semana de 05/10 a 11/10/2026");
        Texto(tela.Find(AnuncioDoMobile)).Should().Be("Quinta-feira, 08/10/2026");

        var semana = new TaskCompletionSource<Resultado<IReadOnlyList<EventoResumo>>>();
        Despachante.RespondeDepois<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(semana.Task);
        Botao(tela, Desktop, "Próximo período").Click();

        tela.Markup.Should().Contain("Carregando…");
        Texto(tela.Find(Anuncio)).Should().Be("Semana de 12/10 a 18/10/2026");
        Texto(tela.Find(AnuncioDoMobile)).Should().Be("Quinta-feira, 15/10/2026");
        Subtitulo(tela).Should().Be("Semana de 12/10 a 18/10/2026");
        tela.FindAll($"{Desktop} button[aria-label='Próximo período']").Should().ContainSingle();

        semana.SetResult(Eventos());

        tela.WaitForAssertion(() => tela.Markup.Should().NotContain("Carregando…"));
        Texto(tela.Find(Anuncio)).Should().Be("Semana de 12/10 a 18/10/2026");
        tela.FindAll(Anuncio).Should().ContainSingle();
        tela.FindAll(AnuncioDoMobile).Should().ContainSingle();
    }

    // F9: tocar num dia de fora do mês na grade compacta leva ao outro mês, e a grade fica à vista durante a carga, já
    // com as semanas do mês novo e o dia tocado escolhido: o botão tocado continua na tela. A lista do dia espera.
    [Fact]
    public void Grade_compacta_do_mes_fica_a_vista_durante_a_carga_do_mes_novo()
    {
        const string Grade = "div.aj-so-mobile > .aj-faixa-dias--mes";
        var tela = Agenda("/agenda?visao=mes&data=2026-10-08");
        var novembro = new TaskCompletionSource<Resultado<IReadOnlyList<EventoResumo>>>();
        Despachante.RespondeDepois<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(novembro.Task);

        // 01/11 é o último dia (de fora) da grade de outubro.
        tela.Find($"{Grade} button[aria-label^='Domingo, 01/11']").Click();

        Endereco.Should().EndWith("/agenda?visao=mes&data=2026-11-01");
        Consultas.Should().HaveCount(2);
        // A grade de novembro de 2026 vai de 26/10 a 06/12: seis semanas.
        tela.FindAll($"{Grade} button.aj-faixa-dias__dia").Should().HaveCount(42);
        tela.Find($"{Grade} button[aria-pressed=true]").GetAttribute("aria-label").Should().StartWith("Domingo, 01/11");
        Texto(tela.Find("div.aj-so-mobile > p.aj-subtitulo[role=status]")).Should().Be("Carregando…");
        Texto(tela.Find("div.aj-desktop-tablet > p.aj-subtitulo[role=status]")).Should().Be("Carregando…");
        tela.FindAll(".aj-dia-escolhido").Should().BeEmpty();

        novembro.SetResult(Eventos(Evento("Reunião de novembro", Local(2026, 11, 1, 9))));

        tela.WaitForAssertion(() => tela.FindAll("div.aj-so-mobile > .aj-dia-escolhido").Should().ContainSingle());
        tela.Markup.Should().Contain("Reunião de novembro");
        tela.FindAll("div.aj-so-mobile > p.aj-subtitulo[role=status]").Should().BeEmpty();
        tela.FindAll($"{Grade} button.aj-faixa-dias__dia").Should().HaveCount(42);
    }

    // F11: o "+N" do Mês leva ao Dia e, quando o Dia aparece (depois da carga da semana dele), o foco ao título do Dia à
    // vista: o "+N" saiu da tela com a grade. Enquanto a semana carrega, nada é pedido ao módulo de foco.
    [Fact]
    public void Mais_eventos_do_mes_leva_o_foco_ao_titulo_do_dia_quando_ele_aparece()
    {
        var eventos = CincoEm14();
        var tela = Agenda("/agenda?visao=mes&data=2026-10-08", AtorDaDiretoria, eventos);
        var semana = new TaskCompletionSource<Resultado<IReadOnlyList<EventoResumo>>>();
        Despachante.RespondeDepois<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(semana.Task);

        tela.Find("button[aria-label='Mais 2 eventos em 14/10']").Click();

        Endereco.Should().EndWith("/agenda?visao=dia&data=2026-10-14");
        tela.Markup.Should().Contain("Carregando…");
        SeletoresFocados.Should().BeEmpty();

        semana.SetResult(Eventos(eventos));

        tela.WaitForAssertion(() => SeletoresFocados.Should().Equal(TituloDoDia));
        tela.Find("h2.aj-dia__cabeca").GetAttribute("tabindex").Should().Be("-1");
        tela.Find("section.aj-secao-dia > h2.aj-secao-dia__cabeca").GetAttribute("tabindex").Should().Be("-1");
    }

    // F11: uma seta tocada antes de o Dia aparecer cancela o foco pedido pelo "+N": quem tocou a seta fica com o foco.
    [Fact]
    public void Seta_antes_de_o_dia_aparecer_cancela_o_foco_no_titulo_do_dia()
    {
        var tela = Agenda("/agenda?visao=mes&data=2026-10-08", AtorDaDiretoria, CincoEm14());
        var semana = new TaskCompletionSource<Resultado<IReadOnlyList<EventoResumo>>>();
        Despachante.RespondeDepois<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(semana.Task);
        tela.Find("button[aria-label='Mais 2 eventos em 14/10']").Click();

        // 15/10, a mesma semana: a consulta é a que já espera.
        Botao(tela, Desktop, "Próximo período").Click();
        semana.SetResult(Eventos());

        tela.WaitForAssertion(() => tela.FindAll("h2.aj-dia__cabeca").Should().ContainSingle());
        Subtitulo(tela).Should().Be("Quinta-feira, 15/10/2026");
        SeletoresFocados.Should().BeEmpty();
    }
```

**`tests/AtividadesJuridicas.IntegrationTests/AgendaWebTests.cs`** (CRLF), teste `Diretor_ve_os_eventos_da_semana` (F13 e F8 no HTML por HTTP):

- no regex do desktop, troque `"<div class=\"aj-segmentado aj-segmentado--periodo\">\\s*" +` (a linha antes de `"<button type=\"button\" class=\"aj-segmentado__seta\" aria-label=\"Período anterior\"…`) por `"<div class=\"aj-segmentado aj-segmentado--periodo\" role=\"group\" aria-label=\"Período\">\\s*" +`;
- no regex do tablet, troque `"(?s)<div class=\"aj-barra-filtros aj-barra-filtros--plana\">\\s*<div class=\"aj-segmentado aj-segmentado--periodo\">\\s*" +` por `"(?s)<div class=\"aj-barra-filtros aj-barra-filtros--plana\">\\s*<div class=\"aj-segmentado aj-segmentado--periodo\" role=\"group\" aria-label=\"Período\">\\s*" +`;
- no regex da barra do mobile, troque `<div class=\"aj-barra-superior__setas\">\\s*" +` por `<div class=\"aj-barra-superior__setas\" role=\"group\" aria-label=\"Período\">\\s*" +`;
- depois da asserção do cabeçalho (a que termina em `.And.Contain("<a href=\"/agenda/novo\" class=\"aj-fab\" aria-label=\"Novo evento\">");`), acrescente:

```csharp
        // O anúncio do período, só para leitor, um por visão: o da Semana no desktop e no tablet, o do Dia no mobile.
        html.Should().Contain(
                $"<div class=\"aj-desktop-tablet\"><p class=\"aj-so-leitor\" role=\"status\">{RotulosDeAgenda.SubtituloDaSemana(RotulosDeAgenda.SegundaDe(hoje))}</p></div>")
            .And.Contain($"<div class=\"aj-so-mobile\"><p class=\"aj-so-leitor\" role=\"status\">{RotulosDeAgenda.SubtituloDoDia(hoje)}</p></div>");
```

Nada mais nos regexes muda (os botões, as visões e o "Filtros" continuam como estão).

O build ainda falha por causa do `MesDaAgendaTests` (Step 17): os testes desta etapa rodam no Step 21.

- [ ] **Step 19: Componentes — o Mês na carga (F9) e os títulos do Dia focáveis (F11)**

**`src/AtividadesJuridicas.Web/Components/Agenda/MesDaAgenda.razor`** (CRLF) inteiro, com o `RotulosDeAgenda.RotuloDoMais` do Step 15:

```razor
@inject FusoDoEscritorio Fuso

@* O Mês da agenda. Desktop e tablet: a linha dos dias da semana e a grade das semanas inteiras (de segunda a
   domingo) que cobrem o mês da data, em 4, 5 ou 6 linhas; dia de outro mês apagado e sem eventos; hoje com o número
   no círculo; até três eventos por dia, na ordem de início, e o "+N" com os outros, que leva ao Dia daquela data;
   evento de vários dias em cada dia que toca, com a hora só no dia em que começa. Mobile: no topo, a grade compacta
   (fora da barra superior, que não rola: rola com o conteúdo), e embaixo a lista do dia da data (ListaDoDia).
   Carregando (a consulta de outro mês, de outro filtro ou a nova tentativa): o desktop e o tablet dizem "Carregando…"
   no lugar da grade; no mobile a grade compacta fica, com as semanas do mês novo e sem os pontos, e "Carregando…" vai
   no lugar da lista. O dia tocado na grade (inclusive um de fora, que leva ao outro mês) continua na tela, com o foco. *@
<div class="aj-desktop-tablet">
    @if (Carregando)
    {
        <p class="aj-subtitulo" role="status">Carregando…</p>
    }
    else
    {
        <div class="aj-mes">
            <div class="aj-mes__cabeca" aria-hidden="true"><span>Seg</span><span>Ter</span><span>Qua</span><span>Qui</span><span>Sex</span><span>Sáb</span><span>Dom</span></div>
            <div class="aj-mes__rolagem">
                <div class="aj-mes__grade">
                    @foreach (var dia in Periodo.SemanasDoMes)
                    {
                        if (!Periodo.NoMes(dia))
                        {
                            <div class="aj-mes__celula aj-mes__celula--fora" @key='("fora", dia)'><span class="aj-mes__numero">@RotulosDeAgenda.NumeroDoDia(dia)</span></div>
                        }
                        else
                        {
                            var doDia = _porDia[dia];
                            <div class="aj-mes__celula" @key='("dia", dia)'>
                                @if (dia == Hoje)
                                {
                                    <span class="aj-mes__numero aj-mes__numero--hoje">@RotulosDeAgenda.NumeroDoDia(dia)</span>
                                    <span class="aj-so-leitor">hoje</span>
                                }
                                else
                                {
                                    <span class="aj-mes__numero">@RotulosDeAgenda.NumeroDoDia(dia)</span>
                                }
                                @foreach (var evento in doDia.Take(MaximoPorDia))
                                {
                                    <CartaoDeEvento @key="evento.Id" Evento="evento" Dia="dia" Forma="FormaDoCartao.Mes" Href="@PeriodoDaAgenda.Ficha(evento.Id, Periodo.Consulta)" />
                                }
                                @if (doDia.Count > MaximoPorDia)
                                {
                                    <button type="button" class="aj-mes__mais" aria-label="@RotulosDeAgenda.RotuloDoMais(dia, doDia.Count - MaximoPorDia)" @onclick="() => AoAbrirDia.InvokeAsync(dia)">@($"+{doDia.Count - MaximoPorDia}")</button>
                                }
                            </div>
                        }
                    }
                </div>
            </div>
        </div>
    }
</div>
<div class="aj-so-mobile">
    <GradeCompactaDoMes Periodo="Periodo" Eventos="EventosDaGrade" Hoje="Hoje" AoEscolher="AoEscolherDia" />
    @if (Carregando)
    {
        <p class="aj-subtitulo" role="status">Carregando…</p>
    }
    else
    {
        <ListaDoDia Dia="Periodo.Data" Eventos="_doDiaEscolhido" Hoje="Periodo.Data == Hoje" Consulta="@Periodo.Consulta" />
    }
</div>

@code {
    private const int MaximoPorDia = 3;

    [Parameter, EditorRequired] public PeriodoDaAgenda Periodo { get; set; } = default!;

    /// <summary>Os eventos da consulta da página (as semanas inteiras do mês).</summary>
    [Parameter, EditorRequired] public IReadOnlyList<EventoResumo> Eventos { get; set; } = [];

    [Parameter, EditorRequired] public DateOnly Hoje { get; set; }

    /// <summary>
    /// A consulta do período está em curso: os <see cref="Eventos"/> ainda são os do período anterior. A grade compacta
    /// fica sem os pontos, e a grade grande e a lista do dia esperam.
    /// </summary>
    [Parameter] public bool Carregando { get; set; }

    /// <summary>O "+N" de um dia: a página leva ao Dia daquela data.</summary>
    [Parameter] public EventCallback<DateOnly> AoAbrirDia { get; set; }

    /// <summary>O dia tocado na grade compacta do mobile: a página leva a data ao endereço e fica no Mês.</summary>
    [Parameter] public EventCallback<DateOnly> AoEscolherDia { get; set; }

    private Dictionary<DateOnly, IReadOnlyList<EventoResumo>> _porDia = new();
    private IReadOnlyList<EventoResumo> _doDiaEscolhido = [];

    // Durante a carga, os eventos são os do período anterior: a grade compacta não os mostra no mês novo.
    private IReadOnlyList<EventoResumo> EventosDaGrade => Carregando ? [] : Eventos;

    protected override void OnParametersSet()
    {
        // Cada evento em todo dia que toca, na ordem de início (no empate, a do servidor: o OrderBy é estável).
        var ordenados = Eventos.OrderBy(evento => evento.InicioEm).ToList();
        _porDia = Periodo.SemanasDoMes.ToDictionary(dia => dia, dia => RotulosDeAgenda.EventosDoDia(Fuso, ordenados, dia));
        _doDiaEscolhido = _porDia[Periodo.Data];
    }
}
```

**`src/AtividadesJuridicas.Web/Components/Agenda/SecaoDoDia.razor`** (CRLF): troque a abertura do título

```razor
    <h2 class="aj-secao-dia__cabeca" aria-label="@RotulosDeAgenda.RotuloLongo(Dia, Hoje)">
```

por

```razor
    <h2 class="aj-secao-dia__cabeca" aria-label="@RotulosDeAgenda.RotuloLongo(Dia, Hoje)" tabindex="@(TituloFocavel ? "-1" : null)">
```

(o resto da linha não muda) e, no `@code`, depois do parâmetro `Consulta`:

```csharp

    /// <summary>
    /// O título recebe o foco por programa (o Dia do tablet: o "+N" do Mês leva o foco a ele): <c>tabindex="-1"</c>,
    /// fora da ordem de tabulação. Na Semana, não.
    /// </summary>
    [Parameter] public bool TituloFocavel { get; set; }
```

**`src/AtividadesJuridicas.Web/Components/Agenda/DiaDaAgenda.razor`** (CRLF):

- `<h2 class="aj-dia__cabeca">` → `<h2 class="aj-dia__cabeca" tabindex="-1">` (o título do Dia do desktop recebe o foco depois do "+N");
- `<SecaoDoDia Dia="Dia" Eventos="_doDia" Hoje="EHoje" Consulta="@Periodo.Consulta" />` → `<SecaoDoDia Dia="Dia" Eventos="_doDia" Hoje="EHoje" Consulta="@Periodo.Consulta" TituloFocavel="true" />`;
- no comentário do topo, depois de `Os três leem os mesmos eventos (só os que tocam o dia).`, acrescente ` Os títulos do desktop e do tablet aceitam o foco por programa (o "+N" do Mês leva o foco a eles).`

O `GradeCompactaDoMes.razor` não muda: a grade recebe os eventos (vazios durante a carga) do `MesDaAgenda`, e os botões já têm `@key="dia"`, o que mantém o botão tocado entre o mês velho e o novo quando o dia existe nas duas grades.

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run, um por vez:
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~MesDaAgendaTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DiaDaAgendaTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~SemanaDaAgendaTests"`

Expected: todos passando (antes deste passo, `Titulos_do_dia_no_desktop_e_no_tablet_aceitam_o_foco_por_programa` falharia com `tabindex` nulo; o da Semana passa antes e depois: protege a Semana do `tabindex`).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AgendaInterativaTests"`
Expected (RED da página): falham `Falha_da_consulta_mostra_o_erro_sem_dias_e_tentar_de_novo_consulta_o_mesmo_periodo` (nenhum `EstadoDaLista`), `Setas_e_hoje_de_cada_largura_ficam_num_grupo_chamado_periodo` (nenhum grupo "Período"), `Troca_de_periodo_anuncia_o_periodo_novo_e_a_seta_continua_na_tela_durante_a_carga` (sem a região), `Grade_compacta_do_mes_fica_a_vista_durante_a_carga_do_mes_novo` (a grade some na carga: 0 botões) e `Mais_eventos_do_mes_leva_o_foco_ao_titulo_do_dia_quando_ele_aparece` (nada pedido ao módulo). `Seta_antes_de_o_dia_aparecer_cancela_o_foco_no_titulo_do_dia` passa antes e depois: protege o cancelamento do pedido. Os outros passam.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AgendaWebTests"`
Expected: `Diretor_ve_os_eventos_da_semana` falha (os grupos sem `role="group"` e as regiões ausentes); os outros passam.

- [ ] **Step 20: A página da agenda (F4, F8, F9, F11, F13)**

Em `src/AtividadesJuridicas.Web/Components/Pages/Agenda.razor` (CRLF). Não mexa no painel de filtros (Tarefa 7).

1. Depois de `@inject NavigationManager Navegacao`, acrescente (a Agenda ainda não injeta o `Foco`: na Tarefa 7, quem o injeta é o `PainelDeFiltros`):

```razor
@inject Foco Foco
```

2. F13, setas do mobile: troque `<div class="aj-barra-superior__setas">` por `<div class="aj-barra-superior__setas" role="group" aria-label="Período">`.

3. F8: no ramo da agenda pronta, troque

```razor
else
{
    @* Desktop: a navegação de período e as visões num grupo; os filtros no outro, aplicados ao "Filtrar". *@
```

por

```razor
else
{
    @* O período à vista, para o leitor de tela: a troca de período (setas, "Hoje", visões, faixa, grade do Mês, "+N")
       muda o texto, que é anunciado, e o foco fica no controle tocado. Um por visão, porque o período do mobile é o da
       visão do mobile; a região escondida pela visão não é anunciada. O texto é o do subtítulo do período. *@
    <div class="aj-desktop-tablet"><p class="aj-so-leitor" role="status">@Subtitulo</p></div>
    <div class="aj-so-mobile"><p class="aj-so-leitor" role="status">@SubtituloDoMobile</p></div>

    @* Desktop: a navegação de período e as visões num grupo; os filtros no outro, aplicados ao "Filtrar". *@
```

4. F13, desktop e tablet: as duas linhas `<div class="aj-segmentado aj-segmentado--periodo">` (a do desktop, logo dentro de `<div class="aj-barra-filtros__grupo">`, e a do tablet, logo dentro de `<div class="aj-barra-filtros aj-barra-filtros--plana">` — as que têm as setas e o "Hoje", não as das visões, que já têm `role="group"`) passam a `<div class="aj-segmentado aj-segmentado--periodo" role="group" aria-label="Período">`.

5. F4 e F9: troque o bloco do conteúdo

```razor
    @if (_erro is not null)
    {
        @* Falha na consulta não é "período sem eventos": o erro fica no lugar dos dias. *@
        <Aviso Tipo="TipoDeAviso.Erro">
            <ChildContent>@_erro</ChildContent>
            <Acoes><button type="button" class="aj-botao aj-botao--secundario" @onclick="TentarDeNovoAsync">Tentar de novo</button></Acoes>
        </Aviso>
    }
    else if (_carregando)
    {
        <p class="aj-subtitulo" role="status">Carregando…</p>
    }
```

por

```razor
    @if (_erro is not null)
    {
        @* Falha na consulta não é "período sem eventos": o erro fica no lugar dos dias, com o "Tentar de novo" das listas. *@
        <EstadoDaLista Erro="@_erro" TextoDeVazio="" TentarDeNovo="TentarDeNovoAsync" />
    }
    else if (Periodo.Visao == VisaoDaAgenda.Mes)
    {
        @* O Mês fica durante a carga (de outro mês, de outro filtro, da nova tentativa): a grade compacta do mobile é onde
           se toca o dia, inclusive um de fora, que leva ao outro mês, e o dia tocado continua na tela, com o foco. *@
        <MesDaAgenda Periodo="Periodo" Eventos="_eventos" Hoje="_hoje" Carregando="_carregando" AoAbrirDia="AbrirDia" AoEscolherDia="EscolherNaGrade" />
    }
    else if (_carregando)
    {
        <p class="aj-subtitulo" role="status">Carregando…</p>
    }
```

e, no fim do mesmo bloco, troque

```razor
    else if (Periodo.Visao == VisaoDaAgenda.Dia)
    {
        <DiaDaAgenda Periodo="Periodo" Eventos="_eventos" Hoje="_hoje" />
    }
    else
    {
        <MesDaAgenda Periodo="Periodo" Eventos="_eventos" Hoje="_hoje" AoAbrirDia="AbrirDia" AoEscolherDia="EscolherNaGrade" />
    }
```

por

```razor
    else
    {
        <DiaDaAgenda Periodo="Periodo" Eventos="_eventos" Hoje="_hoje" />
    }
```

(o Mês já foi tratado acima; o ramo sem visão e o da Semana ficam como estão).

6. `@code` — campos: depois de `private IReadOnlyList<OpcaoDeUsuario> _usuarios = [];` (o último campo, desde que a Tarefa 7 tirou o `_tipoDoPainel`), acrescente:

```csharp

    // O "+N" do Mês pediu o foco no título do Dia, que aparece depois da carga da semana do dia.
    private bool _focarTituloDoDia;

    // O título do Dia no desktop e o da seção do Dia no tablet (um deles à vista); o mobile não tem "+N".
    private const string TituloDoDia = ".aj-dia__cabeca, .aj-secao-dia__cabeca";
```

7. `OnAfterRenderAsync`: a Tarefa 7 tirou o da Agenda (o foco do painel passou ao `PainelDeFiltros`). Recrie-o onde ele ficava, logo antes de `    /// <summary>O dia de hoje no fuso do escritório.</summary>`, só com a chamada:

```csharp
    protected override Task OnAfterRenderAsync(bool firstRender) => FocarTituloDoDiaAsync();
```

Logo depois do `OnAfterRenderAsync`, acrescente:

```csharp

    /// <summary>
    /// Depois do "+N", o foco vai ao título do Dia quando ele aparece: o "+N" sai da tela com a grade. A página é a mesma
    /// (só a consulta do endereço muda), e o FocoNoTitulo só age quando a página muda (e foca o título da página, que
    /// não diz o dia); o título do Dia existe no desktop e no tablet, um escondido pelo CSS, e o serviço Foco foca o que
    /// está à vista.
    /// </summary>
    private async Task FocarTituloDoDiaAsync()
    {
        if (!_focarTituloDoDia) return;

        // Sem Dia para mostrar (a consulta falhou, o acesso caiu), o pedido cai: o foco fica com a rede de segurança do
        // layout.
        if (_erro is not null || _ator is null)
        {
            _focarTituloDoDia = false;
            return;
        }

        if (_carregando || _periodo?.Visao != VisaoDaAgenda.Dia) return;

        _focarTituloDoDia = false;
        // O Foco engole as falhas do JavaScript: sem foco (circuito caindo), o Dia continua à vista.
        await Foco.FocarPrimeiroVisivelAsync(TituloDoDia);
    }
```

8. Troque

```csharp
    private void IrPara(PeriodoDaAgenda periodo) => Navegacao.NavigateTo(periodo.Endereco);
```

por

```csharp
    // Toda troca de período passa por aqui, e ela cancela o foco no título do Dia que um "+N" pediu e que ainda não
    // aconteceu: quem tocou a seta, o "Hoje", a visão, a faixa ou a grade fica com o foco.
    private void IrPara(PeriodoDaAgenda periodo)
    {
        _focarTituloDoDia = false;
        Navegacao.NavigateTo(periodo.Endereco);
    }
```

e

```csharp
    // O "+N" da célula do mês leva ao Dia daquela data.
    private void AbrirDia(DateOnly dia) => IrPara(Periodo.ComVisao(VisaoDaAgenda.Dia).ComData(dia));
```

por

```csharp
    // O "+N" da célula do mês leva ao Dia daquela data e, quando o Dia aparecer, o foco ao título dele.
    private void AbrirDia(DateOnly dia)
    {
        IrPara(Periodo.ComVisao(VisaoDaAgenda.Dia).ComData(dia));
        _focarTituloDoDia = true;
    }
```

9. F8, o texto do anúncio: troque

```csharp
    // O subtítulo do desktop e do tablet diz o período da visão daquelas larguras; o mobile não tem subtítulo.
    private string? Subtitulo => !Pronto ? null : Periodo.NoDesktopETablet switch
    {
        VisaoDaAgenda.Dia => RotulosDeAgenda.SubtituloDoDia(Periodo.Data),
        VisaoDaAgenda.Mes => RotulosDeAgenda.SubtituloDoMes(Periodo.Data),
        _ => RotulosDeAgenda.SubtituloDaSemana(Periodo.Semana[0]),
    };
```

por

```csharp
    // O subtítulo do desktop e do tablet diz o período da visão daquelas larguras. O mobile não tem subtítulo à vista;
    // o anúncio do período no mobile (a região só para leitor) diz o da visão do mobile, com o mesmo texto.
    private string? Subtitulo => !Pronto ? null : SubtituloDe(Periodo.NoDesktopETablet);

    private string? SubtituloDoMobile => !Pronto ? null : SubtituloDe(Periodo.NoMobile);

    private string SubtituloDe(VisaoDaAgenda visao) => visao switch
    {
        VisaoDaAgenda.Dia => RotulosDeAgenda.SubtituloDoDia(Periodo.Data),
        VisaoDaAgenda.Mes => RotulosDeAgenda.SubtituloDoMes(Periodo.Data),
        _ => RotulosDeAgenda.SubtituloDaSemana(Periodo.Semana[0]),
    };
```

Confira, com a ferramenta Grep em `Pages/Agenda.razor`, que `role="group" aria-label="Período"` aparece três vezes, `role="status"` quatro (as duas regiões e os dois "Carregando…": o da carga inicial e o da carga fora do Mês), `aj-botao aj-botao--secundario" @onclick="TentarDeNovoAsync"` nenhuma (o botão é o do `EstadoDaLista`).

- [ ] **Step 21: Build e testes da agenda (GREEN)**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run, um por vez:
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AgendaInterativaTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AgendaWebTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~MesDaAgendaTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DiaDaAgendaTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~SemanaDaAgendaTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AgendaPuraTests"`
- `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~LayoutWebTests"`
- `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`

Expected: todos passando. `LayoutWebTests` prova que as três rotas da agenda continuam abrindo o conteúdo com um bloco `aj-` (as regiões vêm depois do cabeçalho); a trava, que a marcação nova não tem `style`, classe fora de `aj-` nem botão sem classe, e que os invólucros de visão das regiões só têm a classe de visibilidade.

O que fica para o navegador (Tarefa 11):
- F8: o NVDA (ou a árvore de acessibilidade, `/axnome`) com as setas, o "Hoje", as visões e a faixa nas três larguras: o período novo é anunciado uma vez e o foco fica no controle; confira também se a região é anunciada ao abrir a agenda (quando o ramo da agenda pronta aparece) e registre.
- F9: a 390px, Enter num dia de fora na grade compacta: o foco fica no botão do dia (o mesmo `@key`) durante e depois da carga.
- F11: a 1280px e a 900px, Enter no "+N": o foco vai ao título do Dia à vista, sem contorno (a regra `.aj-dia__cabeca:focus,.aj-secao-dia__cabeca:focus{outline:none}` da Tarefa 1).
- F13: o leitor ouve "Período, grupo" ao entrar nas setas.

- [ ] **Step 22: Commit da agenda**

Confira o branch e:

```
git add src/AtividadesJuridicas.Web/Components/Pages/Agenda.razor src/AtividadesJuridicas.Web/Components/Agenda/MesDaAgenda.razor src/AtividadesJuridicas.Web/Components/Agenda/SecaoDoDia.razor src/AtividadesJuridicas.Web/Components/Agenda/DiaDaAgenda.razor tests/AtividadesJuridicas.IntegrationTests/AgendaInterativaTests.cs tests/AtividadesJuridicas.IntegrationTests/AgendaWebTests.cs tests/AtividadesJuridicas.IntegrationTests/MesDaAgendaTests.cs tests/AtividadesJuridicas.IntegrationTests/DiaDaAgendaTests.cs tests/AtividadesJuridicas.IntegrationTests/SemanaDaAgendaTests.cs
git commit -F <arquivo com a mensagem, UTF-8 sem BOM>
```

Mensagem:

```
feat(web): a agenda anuncia o período ao leitor, agrupa as setas, mantém a grade do Mês na carga, usa o erro das listas e leva o foco ao título do Dia depois do +N

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

### Task 11: Suíte, revisão final de todas as telas no navegador, emenda da spec

Feita pela sessão que coordena (com um subagente para a conferência).

- [ ] **Step 1: Suíte inteira** em lotes, com o build antes e nada mais rodando. Expected: unidade e integração sem falha, com mais testes que a linha de base (910 e 1245).
- [ ] **Step 2: Ambiente descartável e ferramenta** — a da 9F copiada para `conferencia-de-telas\9g\`, banco `atividades-verif-9g`, aplicação publicada, Chrome sem janela e condutor.
- [ ] **Step 3: Dados** — um conjunto que exercite todas as rotas do anexo A do `itens-9g.md` (Diretoria, Produção e cliente do portal), pelas telas onde couber e por SQL no resto.
- [ ] **Step 4: Medida do `app.css`** — `V.vazamento` em todas as rotas nas três larguras: com a base só, nenhuma diferença além das esperadas.
- [ ] **Step 5: Revisão de teclado e de leitor de tela** — a lista fixa da 9D em todas as rotas × 3 larguras; o Review Focus; cada item de "Fica para o navegador (Tarefa 11)", no fim do plano; a árvore de acessibilidade (`/axnome`) dos menus, abas, painéis e confirmações; molduras de 1199,5px e 760,5px; o piscar ao conectar; a rolagem entre páginas; o WebKit (Playwright), se houver.
- [ ] **Step 6: Derrubar o ambiente.**
- [ ] **Step 7: Emenda da spec** — cabeçalho (fases 9A a 9G executadas, versões), seção 11 ("Como a fase 9G fez a conferência"), seção 15 ("Da fase 9G": o que entregou, o que decidiu, a lista consolidada das decisões de execução das fases 9B a 9F para o usuário rever, o que fica fora — a tarefa de segurança, as limpezas opcionais), e em cada "Para a 9G" das fases anteriores o que foi feito. Commit `docs(spec): o que a fase 9G ensinou e o fim da migração para o design system`.
- [ ] **Step 8: Suíte inteira de novo** se a conferência mudou código, e o aval do usuário para a `main`.

---

## Decisões tomadas na redação

Decisões que os levantamentos e o contrato não davam e que mudam comportamento ou texto visível, para o usuário revisar. Entre parênteses, a tarefa.

- **Rolagem na primeira página:** a primeira página do circuito não zera a rolagem (quem rolou a página pré-renderizada antes de o circuito conectar não volta ao topo); só as trocas de página seguintes zeram. (Tarefa 3)
- **Rede de segurança do foco:** age na hora, no lote em que o elemento focado sai do DOM, sem espera; depois de um clique num ponto vazio ela esquece o elemento (uma remoção posterior não move o foco), e na troca de janela ele continua lembrado. (Tarefa 3)
- **Elemento focado que fica escondido ou desabilitado sem sair do DOM:** a rede não age; cada tela leva o foco ao destino certo. (Tarefa 3)
- **`focarPrimeiroVisivel` rola até o elemento** (foco sem `preventScroll`), ao contrário do título da página, que é focado sem rolar. (Tarefa 3)
- **Quando o aviso se revela:** no primeiro desenho com `Revelar` ou quando `Revelar` passa a verdadeiro; uma mensagem nova num aviso que continua à vista só se revela porque a tela troca o `@key` dele a cada mensagem (um contador, não o texto: dois erros iguais seguidos se revelam os dois). (Tarefa 3)
- **Fundo do menu de ações depois do foco que saiu:** o toque no fundo devolve o foco ao botão mesmo que o menu já tenha fechado pelo `focusout` (o caso do Safari). (Tarefa 4)
- **Esc no menu de ações:** vale com o foco no botão, na lista ou num item; com o foco fora do menu não há o que fechar (o `focusout` já o fechou). (Tarefa 4)
- **Abas com Alt, Ctrl ou Cmd:** a tecla fica com o navegador (Alt+← continua voltando a página); sem modificador, ←, →, Home e End não rolam a fileira nem a página. (Tarefa 5)
- **Tecla nas abas:** parte da aba que tem o foco, não da escolhida. (Tarefa 5)
- **Aba "Dados" do processo:** aponta (`aria-controls`) para dois painéis, Dados e Observações, que no tablet ficam separados por Clientes. (Tarefa 5)
- **Véu do menu "Mais":** fica fora da ordem de tabulação e da árvore de acessibilidade (`tabindex="-1"`, `aria-hidden="true"`); pelo teclado e pelo leitor, fecham o "Fechar menu" e o Esc. (Tarefa 6)
- **Foco ao abrir o menu "Mais":** vai ao primeiro destino ("Início"), não ao "Fechar menu", que vem antes no documento. (Tarefa 6)
- **Escolher no menu "Mais" a tela em que já se está:** fecha o menu e devolve o foco ao "Mais"; outra tela deixa o foco ao título dela. (Tarefa 6)
- **Esc no menu lateral:** o tratador fica no `aside`; no desktop, com o menu sempre à vista, o Esc não faz nada. (Tarefa 6)
- **Ator da pré-renderização:** com o ator vindo da pré-renderização, o circuito não consulta de novo; um acesso encerrado nesse intervalo aparece na primeira consulta da página. (Tarefa 6)
- **Menu aberto e janela alargada até o desktop:** a vigia de largura fecha o menu sem mover o foco. (Tarefa 6)
- **Busca (Atividades) e período (Auditoria) fora do painel:** fechar o painel só volta ao aplicado os campos do painel; a busca digitada e o período trocado ficam nos campos e só valem depois de "Buscar"/"Filtrar" (hoje o chip usava a busca digitada). (Tarefa 7)
- **Chips de Atividades:** gravam a situação e o "só atrasadas" no filtro aplicado, mantêm o resto dele, e o `aria-pressed` mostra o aplicado, não o escolhido no painel aberto. (Tarefa 7)
- **"Buscar" e Enter da busca em Atividades:** aplicam tudo o que está nos campos, inclusive o escolhido no painel aberto. (Tarefa 7)
- **Botões das linhas da Auditoria** ("Ver tudo deste registro", "Ver o que foi gravado junto"): partem do período aplicado, aplicam na hora e não fazem nada com outra consulta em curso. (Tarefa 7)
- **Paginação da Auditoria (C4):** some durante a carga da aba nova e também depois de uma falha de consulta. (Tarefa 7)
- **"Tentar de novo" da Auditoria (C3):** refaz a carga inicial (usuários e primeira página) enquanto os usuários não chegaram, com guarda de clique duplo e sem "Carregando…" próprio. (Tarefa 7)
- **Fechar o painel de filtros pelo próprio botão:** não pede foco (ele já está no botão); só Esc e "Filtrar" devolvem o foco. (Tarefa 7)
- **"Filtrar" recusado na Auditoria** (identificador inválido): o painel fica aberto e o foco fica nele. (Tarefa 7)
- **Cancelar a confirmação de envio de documento:** devolve o foco ao "Enviar" do painel (que volta à vista e abriu a confirmação), não ao "Enviar documento". (Tarefa 8)
- **Painel de acesso dos documentos:** vira `form`, com "Salvar acesso" como botão de envio. (Tarefa 8)
- **Avisos dos documentos:** todo aviso de resultado se revela, de sucesso e de erro, inclusive a recusa do arquivo grande e as falhas ao abrir o acesso. (Tarefa 8)
- **"Tentar de novo" da lista de documentos:** troca o erro por "Carregando…" até a resposta; a troca de página continua com a lista à vista. (Tarefa 8)
- **Acesso encerrado nos documentos:** apaga a mensagem e impede as que as ações escreveriam depois da recarga. (Tarefa 8)
- **Recado de outra tela:** não se revela (lista de usuários e fichas), para não competir com o foco no título depois da navegação; só as mensagens das ações da própria tela se revelam. (Tarefa 9)
- **Erros do topo dos formulários:** todos se revelam, inclusive os das buscas e da leitura do processo nos formulários de atividade e de evento e a validação de data e hora do evento. (Tarefa 9)
- **Busca de cliente do processo (D2):** o erro vai junto do campo, com o texto da consulta e sem frase de reserva; a busca que dá certo não apaga mais um erro de gravação do topo. (Tarefa 9)
- **"Tentar de novo" do `CargaDoFormulario`:** mostra "Carregando…" até a tela terminar a carga, mesmo que a tela não troque o estado. (Tarefa 9)
- **Excluir da ficha da atividade no mobile:** o foco de volta vai ao "Mais ações" da barra, onde o Excluir fica. (Tarefa 9)
- **Andamento (B9):** só a troca de página mantém a lista; o chip troca a lista por "Carregando…", e a falha tira a página anterior da tela (o "Tentar de novo" mostra "Carregando…"). (Tarefa 10)
- **Formulário de usuário (C7):** a carga atrasada do usuário anterior também não muda o estado da tela; com o da rota ainda esperando, fica "Carregando…", não um formulário vazio. (Tarefa 10)
- **Lista de usuários (C8):** o resultado de uma inativação que chega com a confirmação de outro usuário aberta é calado (sucesso, falha e o "mas não foi possível atualizar a lista"); a lista recarrega e mostra a situação. (Tarefa 10)
- **Troca de revisor (E10):** a barra do desktop continua fixa ao pé e ganha `aj-acoes--medio` (680px), diferente da do devolver, que não é fixa. (Tarefa 10)
- **Anúncio do período (F8):** duas regiões só para leitor, uma para o desktop e o tablet e outra para o mobile; o mobile anuncia o subtítulo da visão dele (o do Dia, sem visão no endereço), que a tela do mobile não mostra. (Tarefa 10)
- **Grade do Mês durante a carga (F9):** vale para toda carga no Mês (outro mês, filtro, "Tentar de novo"): a grade compacta fica sem os pontos, e "Carregando…" vai no lugar da grade grande e da lista do dia. (Tarefa 10)
- **"+N" do Mês (F11):** o foco vai ao `h2` do Dia (no tablet, só a seção do Dia, não as da Semana); o pedido se cancela com qualquer troca de período antes de o Dia aparecer e cai se a consulta falhar. (Tarefa 10)
- **Grupo "Período" no mobile (F13):** só as duas setas, porque o mobile não tem "Hoje". (Tarefa 10)
- **Paginação sem ação:** `aria-disabled="true"` só quando o botão está sem ação; com ação, o atributo some (não vira `"false"`). (Tarefa 10)

## Fica para o navegador (Tarefa 11)

O que os testes não provam e a conferência da Tarefa 11 confere, além da lista fixa da 9D. Entre parênteses, a tarefa que o deixou.

- **`app.css` só com a base:** nenhuma diferença de aparência em todas as rotas nas três larguras (`V.vazamento`). (Tarefa 2)
- **Rede de segurança e rolagem:** o foco que cai no `body` ao fechar um painel vai ao título à vista; um clique num ponto vazio deixa o foco no `body`; o `main` volta ao topo ao trocar de página e não ao trocar o período da agenda. (Tarefa 3)
- **Anúncio duplo:** quando uma tela tira do DOM o botão focado e depois leva o foco ao destino, a rede foca o título antes de o destino chegar pelo .NET; conferir com o leitor de tela se a interrupção incomoda. (Tarefa 3)
- **Aviso revelado:** a página rola até ele, e o contorno de foco da base fica bem no aviso de erro e de sucesso (se não, uma regra no design system para `.aj-aviso[tabindex="-1"]:focus`). (Tarefa 3)
- **Menu de ações** (Áreas, Perfis, Documentos comuns, fichas do cliente e da atividade, tablet e mobile): Tab no último item fecha e segue; Shift+Tab no botão aberto fecha e volta; abrir pelo teclado ou pelo mouse não fecha sozinho; toque fora fecha e devolve o foco no Chrome e no Safari; Enter abre e o menu fica aberto com NVDA e VoiceOver; nenhum ouvinte sobra depois de excluir a linha. (Tarefa 4)
- **Abas a 390px nas três fichas:** o Tab entra pela escolhida; ←, →, Home e End movem o foco e mostram o painel; a fileira rola até a aba focada e a página não rola; Alt+← volta a página; o leitor anuncia "guia, 2 de 4, selecionada". (Tarefa 5)
- **`aria-controls` das abas:** a relação aparece na árvore de acessibilidade com o alvo `aj-aba-painel` em `display: contents` (se sumir, apontar para a `section` dentro do invólucro). (Tarefa 5)
- **Menu "Mais" a 1024px e a 390px:** o foco chega ao "Início"; Tab e Shift+Tab ficam no menu; Esc num item, "Fechar menu" e véu devolvem o foco ao "Mais" visível; a mesma tela devolve ao "Mais" e outra tela leva ao título; um só `navigation "Principal"` e nenhum "Fechar menu" do véu na árvore. (Tarefa 6)
- **`NavLink` da própria rota:** confirmar que o clique dispara o `LocationChanged` (senão o menu fica aberto com o foco no item). (Tarefa 6)
- **Aparência do menu aberto:** "Mais" aceso e os destinos sem o desenho de ativo, no trilho e na barra inferior. (Tarefa 6)
- **"Esconder menu" e "Exibir menu" a 1440px:** o foco chega ao par visível (o da linha do título nas telas com cabeçalho, o da faixa na página não encontrada). (Tarefa 6)
- **Botão flutuante no mobile:** nas seis telas com ação, o Tab chega a ele depois do último cartão, e a posição no canto não muda. (Tarefa 6)
- **Piscar ao conectar:** `/clientes` como Diretoria com a rede lenta, sem mudança no nome, no grupo Administração e no "Mais" (se piscar, `RegisterOnPersisting(PersistirAtor, InteractiveServer)`). (Tarefa 6)
- **Nome longo:** uma linha com reticências, e o nome inteiro na dica do texto, não na do "Sair". (Tarefa 6)
- **Menu aberto e janela alargada de 1024px a 1280px:** o menu fecha, a coluna volta a receber clique e Tab, o foco não pula, e voltar a 1024px deixa o menu fechado. (Tarefa 6)
- **Painel de filtros (Agenda, Atividades, Auditoria):** o foco chega ao primeiro campo e volta ao botão visível de cada largura; o `hidden` vence o `display:flex` do `.aj-painel`; o form escondido não abre espaço no `main`; com a lista de um `select` aberta, o primeiro Esc fecha a lista e o segundo o painel. (Tarefa 7)
- **Envio implícito nos filtros:** o Enter na busca do mobile de Atividades e nos campos de data da Auditoria continua enviando. (Tarefa 7)
- **Documentos:** o foco de volta chega ao "⋯" no tablet e no mobile e ao botão da linha no desktop; o Enter no "Nome (opcional)" e no "Novo nome" envia no Chrome e no Firefox; o aviso revelado rola até a vista no celular. (Tarefa 8)
- **Envio confirmado de documento:** o foco que estava no "Confirmar envio", que fica desabilitado, não se perde de forma a atrapalhar (a rede de segurança não age: o botão não sai do DOM). (Tarefa 8)
- **Erro de gravação revelado:** rola até a vista a 390×844 e a 375×667 com o formulário rolado até o fim. (Tarefa 9)
- **Grupos inválidos:** o leitor lê o grupo com a frase do erro, e o Chrome expõe o `aria-invalid` em `role="group"` e em `fieldset` (fora da lista da ARIA 1.2 para `group`). (Tarefa 9)
- **Foco de volta das confirmações:** chega à cópia visível do botão que abriu, em cada largura. (Tarefa 9)
- **Clique duplo real no "Tentar de novo" do `CargaDoFormulario`:** o segundo clique, que chega antes do desenho, não repete a carga. (Tarefa 9)
- **Paginação com `aria-disabled`:** a aparência de travado com e sem setas; Enter no "Próxima" até a última página deixa o foco no botão, anunciado como indisponível; no Andamento, o foco fica no botão durante e depois da carga. (Tarefa 10)
- **Barra do desktop da troca de revisor:** 680px, alinhada ao painel. (Tarefa 10)
- **Anúncio do período (F8):** com NVDA ou `/axnome`, nas três larguras, o período novo é anunciado uma vez e o foco fica no controle; registrar se a região é anunciada ao abrir a agenda. (Tarefa 10)
- **Grade compacta do Mês (F9):** a 390px, Enter num dia de fora deixa o foco no botão do dia durante e depois da carga. (Tarefa 10)
- **"+N" (F11):** a 1280px e a 900px, Enter no "+N" leva o foco ao título do Dia à vista, sem contorno. (Tarefa 10)
- **Grupo "Período" (F13):** o leitor ouve "Período, grupo" ao entrar nas setas. (Tarefa 10)
