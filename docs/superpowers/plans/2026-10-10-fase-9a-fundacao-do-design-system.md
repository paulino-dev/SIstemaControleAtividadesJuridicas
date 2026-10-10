# Fase 9A — Fundação do design system nas telas: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** O design system ganha o que falta para escrever telas fiéis ao canvas (blocos por visão, barra superior por tela, padrões das telas simples), o layout passa a receber a barra superior do mobile de cada tela, e as telas de entrada, início, minha senha, não encontrada e erro ficam idênticas aos quadros nas três visões.

**Architecture:** Cada padrão que o canvas usa como estilo embutido vira classe `aj-` no artifact do design system e é copiado para `wwwroot/css`. A página Razor emite a marcação de cada visão dentro de invólucros de visibilidade e o CSS mostra a da largura atual. A barra superior do mobile sai da página para o `MainLayout` por uma seção do Blazor (`SectionOutlet`); botão flutuante e barra de ações ficam na própria página, presos por CSS à coluna de conteúdo.

**Tech Stack:** .NET 10, Blazor Interactive Server (login e erro em SSR estático), CSS do design system (tokens e classes `aj-`), xUnit 2.9, FluentAssertions 7.2, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-10-10-migracao-para-o-design-system-design.md` (seções 3, 4, 7.1, 8, 10, 11 e a linha 9A da seção 12).

## Decisões deste plano

- **Um só encaixe no layout.** A leitura dos quadros mostrou que o botão flutuante e a barra `aj-acoes--fixa` podem ficar na página: são posicionados em relação a `aj-app__coluna`, que é `position: relative`, e não rolam com o conteúdo. A barra de ações fica dentro do `<form>`, para o botão enviá-lo. A spec foi emendada na seção 4.3.
- **Barra superior padrão escondida por CSS.** O layout sempre renderiza a barra de logo; quando a tela entrega a sua, ela vem antes e a regra `.aj-barra-superior ~ .aj-barra-superior--padrao` esconde a padrão. Não há estado no layout para isso.
- **Respiro do conteúdo no mobile decidido por `:has()`.** 104px quando há só a barra inferior, 168px quando a tela tem botão flutuante, 112px (com 16px em cima e 12px entre blocos) quando a tela usa a barra de voltar. São os três valores que os quadros trazem embutidos.
- **A barra inferior some nas telas com barra de voltar**, também por `:has()`.
- **Invólucro de visibilidade com `display: contents`.** Mostrado, o invólucro some do layout e os filhos se comportam como filhos do pai; escondido, é `display: none`. As classes vão sempre num `div` ou `span` próprio, nunca no elemento do componente.
- **`div.pagina` sai já.** Uma regra interina no `app.css` mantém em bloco o conteúdo das telas ainda não migradas (as que não têm filho direto com classe `aj-`), para não mudarem de aparência. Sai na 9G.
- **Trilho e barra inferior deixam de usar `NavLink`.** O canvas apaga o destino atual e acende "Mais" enquanto o menu está aberto; o `NavLink` não sabe disso. Os itens viram `<a>` com `aria-current` calculado pelo layout. O menu lateral continua com `NavLink`.
- **O layout entrega às telas um `ContextoDoLayout`** por valor em cascata: as iniciais do usuário (avatar da barra superior) e o comando de exibir o menu.
- **Foco ao navegar por um módulo JavaScript mínimo**, porque cada visão tem o seu `h1` e só o JavaScript sabe qual está à vista.
- **Tokens novos, cinco:** `entrar-frase`, `veu`, `sombra-login-tablet`, `sombra-login-mobile` e `raio-22`. São cores, sombras e raio que os quadros do login e do menu aberto trazem e que não existiam como token. Medidas de espaço que os quadros trazem fora da escala (32px, 22px, 48px) ficam literais no CSS do componente, como o `bundle.css` já faz.
- **O login segue a largura da janela, não `data-visao`:** é tela inteira, fora de `aj-app`.
- **Aviso de reconexão:** texto em português e aparência pelos tokens; o CSS de estado, preso às classes que o Blazor aplica, continua no arquivo do componente. Não é uma tela.
- **Trava contra regressão:** um teste de unidade lista os arquivos Razor já migrados e recusa estilo embutido e classe fora do design system neles. Cada fase acrescenta os seus arquivos.
- **Componentes que nascem aqui:** `CabecalhoDePagina`, `Aviso`, `RotuloDeCampo`, `CartaoAtalho` e `FocoNoTitulo`. `CabecalhoDeDetalhe`, `Paginacao`, `EstadoDaLista`, `MenuDeAcoes` e `Abas` ficam para a fase que primeiro os usa.
- **"Exibir menu" em dois lugares.** Com o menu escondido, o botão fica na linha do título quando a tela tem cabeçalho de página, como no canvas. Tela sem cabeçalho de página (as não migradas e a página não encontrada) recebe do layout uma faixa `aj-exibir-menu` acima do conteúdo, para ninguém ficar sem como trazer o menu de volta. O CSS mostra uma ou a outra.
- **Regras de link da base perdem para os componentes.** `a` e `a:hover` do `app.css` passam a `:where()`, de especificidade zero. Hoje o `a:hover` escurece "Minha senha" sobre o menu escuro e escureceria o texto de um link com cara de botão primário.
- **Ficam para a fase que usa:** os ícones `filtrar`, `enviar` e `planilha` (agenda e atividades) e o auxiliar de teste que recorta o bloco de uma visão (a 9B, com a primeira lista em tabela e cartões).

## Como ler este plano

Cada tarefa traz as **regras** (o contrato), as **interfaces** que ela consome e produz e os **testes obrigatórios**. O plano traz o código onde ele é a própria fidelidade (o CSS, a marcação das telas) ou onde o detalhe é sutil (o layout, o canal da barra superior, o foco). O restante é escrito no estilo do que já existe. O código do plano não foi executado: quando o Blazor ou o navegador exigirem, ajuste o mínimo para preservar o comportamento e registre o desvio no relatório, com a mensagem de erro ou a captura que o motivou.

Nunca apague um teste nem afrouxe uma asserção. Quando uma asserção mudar porque a marcação mudou, reescreva-a mantendo o que ela prova e liste-a no relatório. Asserção negativa (`NotContain`) que deixaria de provar algo com a marcação nova ganha uma afirmação positiva ao lado.

A fonte de cada tela é o quadro do canvas. Antes de escrever a marcação de uma tela, releia os três arquivos dela na cópia local do canvas (caminho em "Estado de partida"). Se um quadro tiver mudado em relação ao que este plano descreve, pare e relate.

## Estado de partida

- Worktree `.claude/worktrees/feat+fase-9a-fundacao-design-system`, branch `worktree-feat+fase-9a-fundacao-design-system`, criado a partir da `main` em `8784dd5`, com a spec commitada.
- Canvas: https://claude.ai/artifact/HzHWv1pgVXAnYHuPEYQDFj, versão `1791598628-21e1`. Baixe os quadros de novo no início da execução (ação `read` com `paths`, os de `project/`) e compare a versão; a cópia desta sessão está em `<scratchpad>/artifact-files/8991caa9-7339-47d7-a01c-84892497662e/project/`.
- Design system: https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS, versão `1791598661-a3fd`. `project/components/bundle.css` e `project/tokens.css` são a fonte de `wwwroot/css/componentes.css` e `wwwroot/css/tokens.css`.
- Web: `MainLayout.razor` com menu, trilho e barra inferior em classes `aj-`, o conteúdo dentro de `main.aj-app__conteudo > div.pagina`; páginas com marcação antiga vestida por `wwwroot/app.css`; `Icone.razor` com 18 traçados; `Routes.razor` com `FocusOnNavigate Selector="h1"`.
- Testes: `AplicacaoDeTeste` sobe a aplicação inteira contra um banco novo (`ClienteAutenticadoAsync`, `CriarPedroAsync`, `CriarCarlaAsync`, `ExecutarAsync`); `TelaDeTeste.RenderizarAsync` desenha um componente fora da página.
- Linha de base: rode `dotnet test` inteiro antes da Tarefa 1 e anote os totais no relatório. A suíte de integração leva de 15 a 35 minutos nesta máquina: rode em segundo plano.
- `git` só pela ferramenta PowerShell. Antes de cada commit, `git branch --show-current`.

## Global Constraints

- TFM único `net10.0`. `Nullable` e `TreatWarningsAsErrors=true`. Nenhum pacote novo.
- Código, nomes e textos em português, com acentuação correta. Arquivos em UTF-8 sem BOM.
- A camada Web só chega aos dados pelo `IDespachante` (`WebSoPeloDespachanteTests`).
- `wwwroot/css/tokens.css` e `wwwroot/css/componentes.css` não são editados à mão: mudam no artifact do design system e são copiados de lá.
- Tela migrada não tem atributo `style` nem classe que não comece por `aj-`. Nenhum CSS novo no `app.css`, salvo a regra interina desta fase.
- As classes de visibilidade (`aj-so-desktop`, `aj-so-tablet`, `aj-so-mobile`, `aj-desktop-tablet`, `aj-tablet-mobile`) vão num invólucro próprio (`div` ou `span`), nunca no elemento do componente.
- Visões: mobile até 760px, tablet de 761px a 1199px, desktop a partir de 1200px.
- Todo texto que um teste procura no HTML é escrito por **uma única** expressão Razor ou por marcação fixa.
- Rotas existentes não mudam. Os campos do login continuam `Entrada.Email` e `Entrada.Senha`, com `_handler=entrar` e o token antifalsificação.
- `<nav class="aj-trilho"` e `<nav class="aj-nav-inferior"` continuam com a classe única e como primeiro atributo; o primeiro `<nav>` do HTML continua sendo o do menu lateral.
- Nenhum link do layout entra dentro de `<main>`.
- Botão só de ícone leva `aria-label` com o verbo.
- Textos exatos, dos quadros:
  - Início: `Início`; `Olá, {nome}. Seu perfil é de {perfil}.` (equipe, desktop e tablet); `Olá, {nome}.` (cliente, desktop e tablet); `Olá, {nome}` (mobile, os dois)
  - Atalhos da equipe: `Minhas atividades` / `Prazos, situações e o que espera por você.`; `Agenda da semana` / `Audiências, reuniões e prazos dos próximos dias.`
  - Atalhos do cliente: `Andamento` / `O que o escritório está fazendo por você.`; `Meus documentos` / `Arquivos que o escritório compartilhou com você.`; `Documentos comuns` / `Modelos e orientações para todos os clientes.`
  - Totais do cliente: `Em andamento` e `Concluídas`
  - Minha senha: `Depois de alterar, você entra de novo com a nova senha.`; `Senha atual`, `Nova senha`, `Confirme a nova senha`; `Alterar senha`; `Voltar para Início`
  - Não encontrada: `Página não encontrada`; `O endereço acessado não existe.`; `Voltar ao início`
  - Entrar: `Sistema interno`; `Atividades Jurídicas`; `Agenda, prazos e atividades do escritório em um só lugar.`; `Entrar`; `Acesse com o e-mail e a senha do escritório.` (desktop e tablet); `Use o e-mail e a senha do escritório.` (mobile); `E-mail`, `Senha`; marcador `nome@araujoadvocacia.com.br`; `Araújo Advocacia · uso interno`
  - Sem quadro, mantidos: `Seu acesso foi encerrado. Entre novamente.`; `Carregando…`; `Senha alterada. Entre novamente com a nova senha.`; `Ocorreu um erro inesperado.`; `Não foi possível concluir a operação. Tente novamente em instantes.`

## Review Focus

Condições que a spec implica e que mais provavelmente atingem quem usa o sistema. Cada uma tem o seu teste ou passo de conferência na tarefa indicada.

1. **Tela ainda não migrada aberta depois da troca do layout** (todas, fora as desta fase): continua com a aparência de antes, com a barra de logo no mobile. Teste na Tarefa 3; conferência no navegador na Tarefa 8.
2. **Largura exatamente nos limites das visões** (760 e 761px, 1199 e 1200px): uma só navegação, um só título e um só botão de envio à vista. Conferência no navegador na Tarefa 8.
3. **Teclado:** Tab não passa por controle da visão escondida, Enter envia o formulário da senha uma vez só e o foco vai para o título visível ao navegar. Conferência no navegador nas Tarefas 5 e 8.
4. **Nome de uma palavra só ou muito longo:** o avatar mostra uma inicial e a saudação não estoura a barra superior do mobile. Teste na Tarefa 3; conferência no navegador na Tarefa 8.
5. **Login com campos vazios, credencial errada ou vindo da troca de senha:** erro de campo, aviso de erro e aviso de sucesso aparecem com as classes do design system, sem perder o token antifalsificação nem os nomes dos campos. Testes na Tarefa 7.

---

### Task 1: Design system — tokens, classes da fundação e cópia para o código

Feita pela sessão que coordena: publica no artifact do usuário.

**Files:**
- Artifact do design system, sob `project/`: `tokens.json`, `tokens.css`, `components/bundle.css`, `responsividade.md`, `README.md`, `design-system.json`; `components/Navegacao/`, `components/CabecalhoDePagina/`, `components/Botao/`, `components/Painel/`, `components/BarraDeAcoes/` (README e, onde indicado, prévia); novos `components/CartaoAtalho/`, `components/Totais/`, `components/EstadoDaTela/`, `components/Entrar/`, `components/FaixaDeErro/` (README e prévia)
- Modify: `src/AtividadesJuridicas.Web/wwwroot/css/tokens.css`
- Modify: `src/AtividadesJuridicas.Web/wwwroot/css/componentes.css`

**Interfaces:**
- Consumes: os quadros do canvas listados na seção 7.1 da spec.
- Produces: as variáveis `--entrar-frase`, `--veu`, `--sombra-login-tablet`, `--sombra-login-mobile`, `--raio-22`; as classes `aj-so-desktop`, `aj-so-tablet`, `aj-so-mobile`, `aj-desktop-tablet`, `aj-tablet-mobile`; `aj-barra-superior--padrao`, `aj-barra-superior--voltar`, `aj-barra-superior__logo`, `aj-barra-superior__espaco`, `aj-barra-superior__vazio`; `aj-cabecalho__inicio`, `aj-cabecalho__menu`, `aj-cabecalho__texto--folgado`; `aj-trilho__item--duas-linhas`; `aj-app__veu`, `aj-menu__fechar`, `aj-menu__esconder`, `aj-menu__sair`, `aj-exibir-menu`, `aj-app--menu-aberto`, `aj-app--menu-escondido`; `aj-painel--estreito`, `aj-painel__acoes`; `aj-cartao--atalho`, `aj-cartao__icone`, `aj-cartao__texto`, `aj-cartao__seta`; `aj-totais`, `aj-total`; `aj-estado`, `aj-estado--pagina`; `aj-entrar` e seus elementos; `aj-faixa-erro`, `aj-faixa-erro__fechar`.

**Regras:**
- Siga o `SKILL.md` do artifact para revisar um sistema: leia o índice e cada arquivo que vai mudar, edite cópias numa pasta do scratchpad, envie tudo numa chamada, com `design-system.json` por último e relido logo antes. No índice muda só `lastChange` (`by`: "Gabriel Paulino", `via`: "Claude Code", `at`: agora, `note`: "Fase 9A: blocos por visão, barra superior por tela, padrões das telas simples e login.").
- `tokens.json` vai inteiro. Em `tokens.css` os cinco valores entram nos mesmos blocos e na mesma ordem dos vizinhos.
- Cada componente novo tem README no molde dos existentes (primeira frase é o resumo; diz o que quem usa fornece) e uma prévia cuja linha 1 é o marcador `<!-- @dsCard group="…" height=N -->`, sem estilo embutido além do `style` do próprio documento de prévia.
- As prévias de Navegação e de Cabeçalho de página perdem os estilos embutidos que as classes novas substituem (`font-size:13px` do subtítulo, `width/height:36px` do avatar, `gap:4px` do texto).
- Corrija no README de Navegação a frase da barra inferior do portal: ela tem Início, Andamento, Documentos e Mais.

- [ ] **Step 1: Tokens**

Em `tokens.json`, acrescente ao fim de `color.tokens`:

```json
{"name": "entrar-frase", "value": "#B9C3B5", "usage": "Frase de apoio do painel de marca do login, sobre `menu-fundo`."},
{"name": "veu", "value": "rgba(22,32,26,.45)", "usage": "Véu sobre o conteúdo quando o menu abre por cima da tela (tablet e mobile) e fundo do aviso de reconexão."}
```

ao fim de `shadow.tokens`:

```json
{"name": "sombra-login-tablet", "value": "0 1px 2px rgba(31,43,34,.04), 0 32px 64px -32px rgba(31,43,34,.3)", "usage": "Cartão de login no tablet."},
{"name": "sombra-login-mobile", "value": "0 1px 2px rgba(31,43,34,.04), 0 24px 48px -24px rgba(31,43,34,.25)", "usage": "Cartão de login no mobile."}
```

e em `radius.tokens`, depois de `raio-20`:

```json
{"name": "raio-22", "value": "22px", "usage": "Cartão de login no tablet."}
```

Em `tokens.css`: `--entrar-frase` e `--veu` depois de `--evento-outro-rotulo`; as duas sombras depois de `--sombra-login`; `--raio-22` depois de `--raio-20`.

- [ ] **Step 2: Variáveis por visão no `bundle.css`**

Acrescente ao fim do bloco `:root{…}` e do bloco `[data-visao="desktop"]{…}`:

```css
  --aj-ver-desktop:contents;--aj-ver-tablet:none;--aj-ver-mobile:none;--aj-ver-desktop-tablet:contents;--aj-ver-tablet-mobile:none;
  --aj-pagina-padding-fab:18px var(--espaco-24) var(--espaco-28);--aj-pagina-padding-voltar:18px var(--espaco-24) var(--espaco-28);--aj-pagina-gap-voltar:var(--espaco-14);
  --aj-painel-estreito:520px;--aj-painel-estreito-sombra:var(--sombra-painel);
```

Ao fim do bloco `[data-visao="tablet"]{…}`:

```css
  --aj-ver-desktop:none;--aj-ver-tablet:contents;--aj-ver-mobile:none;--aj-ver-desktop-tablet:contents;--aj-ver-tablet-mobile:contents;
  --aj-pagina-padding-fab:var(--espaco-24) var(--espaco-24) var(--espaco-40);--aj-pagina-padding-voltar:var(--espaco-24) var(--espaco-24) var(--espaco-40);--aj-pagina-gap-voltar:var(--espaco-14);
  --aj-painel-estreito:560px;--aj-painel-estreito-sombra:var(--sombra-painel);
```

Ao fim do bloco `[data-visao="mobile"]{…}`:

```css
  --aj-ver-desktop:none;--aj-ver-tablet:none;--aj-ver-mobile:contents;--aj-ver-desktop-tablet:none;--aj-ver-tablet-mobile:contents;
  --aj-pagina-padding-fab:var(--espaco-14) var(--espaco-16) 168px;--aj-pagina-padding-voltar:var(--espaco-16) var(--espaco-16) 112px;--aj-pagina-gap-voltar:var(--espaco-12);
  --aj-painel-estreito:none;--aj-painel-estreito-sombra:var(--sombra-1);
```

Ao fim do bloco `@media (max-width:1199px){:root{…}}` (só o que muda em relação ao desktop):

```css
  --aj-ver-desktop:none;--aj-ver-tablet:contents;--aj-ver-tablet-mobile:contents;
  --aj-pagina-padding-fab:var(--espaco-24) var(--espaco-24) var(--espaco-40);--aj-pagina-padding-voltar:var(--espaco-24) var(--espaco-24) var(--espaco-40);
  --aj-painel-estreito:560px;
```

Ao fim do bloco `@media (max-width:760px){:root{…}}` (só o que muda em relação ao tablet):

```css
  --aj-ver-tablet:none;--aj-ver-mobile:contents;--aj-ver-desktop-tablet:none;
  --aj-pagina-padding-fab:var(--espaco-14) var(--espaco-16) 168px;--aj-pagina-padding-voltar:var(--espaco-16) var(--espaco-16) 112px;--aj-pagina-gap-voltar:var(--espaco-12);
  --aj-painel-estreito:none;--aj-painel-estreito-sombra:var(--sombra-1);
```

Nos dois blocos de mobile (`@media (max-width:760px)` e `[data-visao="mobile"]`), troque o respiro inferior de `--aj-pagina-padding` de `168px` para `104px`.

Em `.aj-app{…}`, depois de `height:100vh`, acrescente `height:100dvh`.

- [ ] **Step 3: Classes novas no `bundle.css`**

Acrescente ao fim do arquivo:

```css
/* ---------- Visibilidade por visão ---------- */
/* Invólucro de um bloco que só existe numa visão. Mostrado, some do layout (display: contents) e os filhos se
   comportam como filhos do pai; escondido, não é desenhado nem lido. Vai num div ou span próprio. */
.aj-so-desktop{display:var(--aj-ver-desktop)}
.aj-so-tablet{display:var(--aj-ver-tablet)}
.aj-so-mobile{display:var(--aj-ver-mobile)}
.aj-desktop-tablet{display:var(--aj-ver-desktop-tablet)}
.aj-tablet-mobile{display:var(--aj-ver-tablet-mobile)}
.aj-app__conteudo>:is(.aj-so-desktop,.aj-so-tablet,.aj-so-mobile,.aj-desktop-tablet,.aj-tablet-mobile)>*{flex-shrink:0}

/* ---------- Estrutura: respiro do conteúdo, menu que abre e esconde ---------- */
.aj-app:has(.aj-fab) .aj-app__conteudo{padding:var(--aj-pagina-padding-fab)}
.aj-app:has(.aj-barra-superior--voltar) .aj-app__conteudo{gap:var(--aj-pagina-gap-voltar);padding:var(--aj-pagina-padding-voltar)}
.aj-app:has(.aj-barra-superior--voltar) .aj-nav-inferior{display:none}
.aj-app__veu,.aj-menu__fechar{display:none}
.aj-menu__sair{display:flex;margin:0}
.aj-trilho__item--duas-linhas{text-align:center;line-height:1.25}
/* Menu escondido (desktop): "Exibir menu" e o símbolo vão na linha do título; tela sem cabeçalho de página
   recebe a faixa aj-exibir-menu, que o layout põe acima do conteúdo. */
.aj-exibir-menu{display:none;flex:0 0 auto;align-items:center;gap:var(--espaco-14);padding:18px var(--espaco-24) 0}
.aj-exibir-menu img{display:block;width:29px;height:auto}
.aj-exibir-menu .aj-botao{color:var(--verde)}
.aj-exibir-menu .aj-botao svg{width:20px;height:20px;stroke-width:1.75}
@media (min-width:1200px){
  .aj-app--menu-escondido .aj-menu{display:none}
  .aj-app--menu-escondido .aj-cabecalho__menu,.aj-app--menu-escondido .aj-exibir-menu{display:flex}
  .aj-app--menu-escondido .aj-app__coluna:has(.aj-cabecalho__menu)>.aj-exibir-menu{display:none}
}
/* Em tablet e mobile o menu completo abre por cima da tela, a partir de "Mais". */
@media (max-width:1199px){
  .aj-menu__esconder{display:none}
  .aj-app--menu-aberto .aj-menu{display:flex;position:absolute;inset:0 auto 0 0;z-index:20;width:min(var(--menu-largura),86vw);flex-basis:auto;box-shadow:var(--sombra-flutuante)}
  .aj-app--menu-aberto .aj-menu__fechar{display:inline-flex}
  .aj-app--menu-aberto .aj-app__veu{display:block;position:absolute;inset:0;z-index:19;padding:0;border:0;border-radius:0;background:var(--veu);cursor:default}
}

/* ---------- Barra superior por tela (mobile) ---------- */
/* A tela entrega a sua barra antes da padrão; a padrão (logo e avatar) só aparece quando a tela não entrega nenhuma. */
.aj-barra-superior~.aj-barra-superior--padrao{display:none}
.aj-barra-superior__espaco{flex:1 1 auto}
.aj-barra-superior__linha>img.aj-barra-superior__logo{width:122px}
.aj-barra-superior .aj-avatar{width:36px;height:36px}
.aj-barra-superior__titulo .aj-titulo,.aj-barra-superior__titulo .aj-subtitulo{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.aj-barra-superior__titulo .aj-subtitulo{font-size:13px}
.aj-barra-superior--voltar{flex-direction:row;align-items:center;gap:var(--espaco-4);padding:var(--espaco-8)}
.aj-barra-superior--voltar>.aj-botao{flex:0 0 auto;width:var(--alvo-toque);min-height:var(--alvo-toque)}
.aj-barra-superior--voltar>.aj-botao svg{width:22px;height:22px}
.aj-barra-superior--voltar>h1{flex:1 1 auto;min-width:0;margin:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font:700 17px/1.3 var(--font-sans);text-align:center}
.aj-barra-superior__vazio{flex:0 0 var(--alvo-toque)}

/* ---------- Cabeçalho: menu escondido e texto folgado ---------- */
.aj-cabecalho__inicio{display:flex;align-items:center;gap:var(--espaco-14);min-width:0}
.aj-cabecalho__menu{display:none;align-items:center;gap:var(--espaco-14)}
.aj-cabecalho__menu img{display:block;width:29px;height:auto}
.aj-cabecalho__menu .aj-botao{color:var(--verde)}
.aj-cabecalho__menu .aj-botao svg{width:20px;height:20px;stroke-width:1.75}
.aj-cabecalho__texto--folgado{gap:var(--espaco-4)}

/* ---------- Botão desabilitado, painel estreito, barra de um botão ---------- */
.aj-botao:disabled,.aj-botao[aria-disabled="true"]{opacity:.55;cursor:not-allowed}
.aj-painel--estreito{max-width:var(--aj-painel-estreito);box-shadow:var(--aj-painel-estreito-sombra)}
.aj-painel__acoes{display:flex;justify-content:flex-end;gap:var(--espaco-8);padding-top:var(--espaco-4)}
.aj-acoes--fixa>:only-child{grid-column:1/-1}

/* ---------- Cartão-atalho ---------- */
.aj-cartao--atalho{flex-direction:row;align-items:center;gap:var(--espaco-14)}
.aj-cartao__icone{flex:0 0 auto;display:inline-flex;align-items:center;justify-content:center;width:44px;height:44px;border-radius:var(--raio-12);background:var(--selecao-fundo)}
.aj-cartao__icone svg{flex:0 0 auto;width:22px;height:22px;fill:none;stroke:var(--verde);stroke-width:2;stroke-linecap:round;stroke-linejoin:round}
.aj-cartao__texto{flex:1 1 auto;min-width:0}
.aj-cartao--atalho .aj-cartao__titulo,.aj-cartao--atalho .aj-cartao__meta{display:block}
.aj-cartao__seta{flex:0 0 auto;width:18px;height:18px;fill:none;stroke:var(--tinta-icone);stroke-width:2;stroke-linecap:round;stroke-linejoin:round}

/* ---------- Totais ---------- */
.aj-totais{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:var(--espaco-12);max-width:520px}
.aj-total{gap:var(--espaco-2)}
.aj-total b{font:600 28px/1.1 var(--font-sans);font-variant-numeric:tabular-nums}

/* ---------- Estado da tela ---------- */
.aj-estado{flex:1 1 auto;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:var(--espaco-10);padding:var(--espaco-40) var(--espaco-16);text-align:center}
.aj-estado>img{width:44px;height:auto;opacity:.45}
.aj-estado>.aj-botao{margin-top:var(--espaco-8)}
.aj-estado--pagina{min-height:100vh;min-height:100dvh;background:var(--fundo);color:var(--tinta);font:400 15px/1.5 var(--font-sans)}

/* ---------- Faixa de erro da aplicação ---------- */
.aj-faixa-erro{display:none;position:fixed;left:0;right:0;bottom:0;z-index:1000;padding:var(--espaco-10) 56px var(--espaco-10) var(--espaco-16);border-top:1px solid var(--aviso-borda);background:var(--aviso-fundo);color:var(--aviso-texto);font:500 15px/1.5 var(--font-sans)}
.aj-faixa-erro a{margin-left:var(--espaco-8);color:inherit;font-weight:600}
.aj-faixa-erro__fechar{position:absolute;right:var(--espaco-8);top:50%;transform:translateY(-50%);color:inherit}

/* ---------- Entrar ---------- */
/* Tela inteira, fora de aj-app: segue a largura da janela, não data-visao. */
.aj-entrar{min-height:100vh;min-height:100dvh;display:flex;flex-wrap:wrap;background:var(--fundo);color:var(--tinta);font:400 15px/1.5 var(--font-sans)}
.aj-entrar__marca{flex:1 1 520px;min-height:380px;position:relative;overflow:hidden;display:flex;flex-direction:column;justify-content:space-between;gap:32px;padding:var(--espaco-40);background:var(--menu-fundo)}
.aj-entrar__dagua{position:absolute;right:-170px;top:360px;width:700px;height:auto;opacity:.18;pointer-events:none}
.aj-entrar__logo{position:relative;display:block;width:200px;height:auto}
.aj-entrar__texto{position:relative;display:flex;flex-direction:column;gap:var(--espaco-14);max-width:420px}
.aj-entrar__sobretitulo{display:inline-flex;align-items:center;gap:var(--espaco-10);font:600 12px/1.5 var(--font-sans);letter-spacing:.14em;text-transform:uppercase;color:var(--dourado)}
.aj-entrar__sobretitulo::before{content:"";width:28px;height:1px;background:var(--dourado)}
.aj-entrar__nome{margin:0;font:500 38px/1.1 var(--font-serif);letter-spacing:-.01em;color:var(--sobre-verde)}
.aj-entrar__frase{margin:0;font-size:15.5px;color:var(--entrar-frase)}
.aj-entrar__area{flex:999 1 480px;min-width:0;display:flex;flex-direction:column;align-items:center;justify-content:center;padding:32px var(--espaco-24)}
.aj-entrar__cartao{width:100%;max-width:420px;display:flex;flex-direction:column;gap:var(--espaco-14);padding:var(--espaco-18);border:1px solid var(--borda);border-radius:var(--raio-16);background:var(--superficie);box-shadow:var(--sombra-login)}
.aj-entrar__titulo{display:flex;flex-direction:column;gap:var(--espaco-8)}
.aj-entrar__titulo h1{margin:0;font:500 36px/1.1 var(--font-serif);letter-spacing:-.01em}
.aj-entrar__titulo p{margin:0;font-size:15.5px;color:var(--tinta-suave)}
.aj-entrar__campos{display:flex;flex-direction:column;gap:var(--espaco-12)}
.aj-entrar__cartao .aj-campo{min-height:42px;font-size:15.5px}
.aj-entrar__cartao .aj-botao{gap:var(--espaco-10);min-height:44px;margin-top:var(--espaco-6);padding:0 var(--espaco-24);font-size:15px}
.aj-entrar__rodape{margin:0;font-size:13px;color:var(--tinta-apagada)}
.aj-entrar__rodape--cartao{padding-top:var(--espaco-12);border-top:1px solid var(--borda-suave)}
.aj-entrar__rodape--pagina{display:none}
/* Tablet e mobile: marca em cima, cartão sobreposto e rodapé no pé da página. */
@media (max-width:1199px){
  .aj-entrar{flex-direction:column;flex-wrap:nowrap;align-items:center}
  .aj-entrar__marca{flex:0 0 auto;align-self:stretch;min-height:0;height:520px;gap:0;padding:48px 56px 140px}
  .aj-entrar__dagua{right:-140px;top:40px;width:520px;opacity:.16}
  .aj-entrar__logo{width:140px}
  .aj-entrar__texto{gap:var(--espaco-10);max-width:none}
  .aj-entrar__nome{font-size:46px;line-height:1.05}
  .aj-entrar__frase{font-size:16px}
  .aj-entrar__area{flex:1 1 auto;align-self:stretch;justify-content:flex-start;padding:0}
  .aj-entrar__cartao{position:relative;max-width:480px;margin-top:-96px;gap:20px;padding:32px;border-radius:var(--raio-22);box-shadow:var(--sombra-login-tablet)}
  .aj-entrar__titulo{gap:var(--espaco-6)}
  .aj-entrar__titulo h1{font-size:34px;letter-spacing:0}
  .aj-entrar__campos{display:contents}
  .aj-entrar__cartao .aj-rotulo{gap:var(--espaco-6)}
  .aj-entrar__cartao .aj-campo{min-height:52px;padding:0 var(--espaco-16);font-size:16px}
  .aj-entrar__cartao .aj-botao{min-height:54px;margin-top:var(--espaco-4);font-size:16px;box-shadow:none}
  .aj-entrar__rodape--cartao{display:none}
  .aj-entrar__rodape--pagina{display:block;margin:auto 0 var(--espaco-40);padding-top:var(--espaco-24);font-size:13.5px}
}
@media (max-width:760px){
  .aj-entrar{align-items:stretch}
  .aj-entrar__marca{height:auto;justify-content:flex-start;gap:var(--espaco-28);padding:44px var(--espaco-24) 84px}
  .aj-entrar__dagua{right:-110px;top:30px;width:300px}
  .aj-entrar__logo{width:104px}
  .aj-entrar__texto{gap:var(--espaco-8)}
  .aj-entrar__sobretitulo{gap:var(--espaco-8);font-size:11px}
  .aj-entrar__sobretitulo::before{width:20px}
  .aj-entrar__nome{font-size:32px;line-height:1.08}
  .aj-entrar__frase{display:none}
  .aj-entrar__area{padding:0 var(--espaco-16)}
  .aj-entrar__cartao{max-width:none;margin-top:-52px;gap:var(--espaco-18);padding:22px 20px;border-radius:var(--raio-20);box-shadow:var(--sombra-login-mobile)}
  .aj-entrar__titulo{gap:var(--espaco-4)}
  .aj-entrar__titulo h1{font-size:28px}
  .aj-entrar__titulo p{font-size:15px}
  .aj-entrar__cartao .aj-campo{min-height:50px;padding:0 var(--espaco-14)}
  .aj-entrar__cartao .aj-botao{min-height:52px}
  .aj-entrar__rodape--pagina{margin-bottom:var(--espaco-28);font-size:13px;text-align:center}
}
```

Confira cada valor do bloco "Entrar" contra `Main.dc.html`, `LoginTablet.dc.html` e `LoginMobile.dc.html`, e os demais contra os quadros da seção 7.1 da spec. Divergência entre este CSS e um quadro: vale o quadro; corrija e registre.

- [ ] **Step 4: Textos e prévias do design system**

- `responsividade.md`: na tabela de estrutura, a linha de respiro do mobile passa a "104px com barra inferior; 168px com barra inferior e botão flutuante; 112px com barra de ações". Em "Como implementar", acrescente: "Um bloco que só existe numa visão vai dentro de um invólucro `aj-so-desktop`, `aj-so-tablet`, `aj-so-mobile`, `aj-desktop-tablet` ou `aj-tablet-mobile`. Renderize os blocos de cada visão e deixe o CSS mostrar o da visão atual. O invólucro é um `div` ou `span` próprio, nunca o elemento do componente."
- `README.md`, em "Como usar no código": uma linha remetendo à regra dos invólucros de visão.
- `components/Navegacao/README.md`: as três variantes da barra superior (título com subtítulo e avatar; `aj-barra-superior--voltar` em ficha e formulário, sem barra inferior; `aj-barra-superior--padrao` com a logo quando a tela não declara barra); o menu aberto por "Mais" (`aj-app--menu-aberto`, com véu); "Mais" com `aria-current="page"` quando a tela está fora da barra e enquanto o menu está aberto; o menu escondido (`aj-app--menu-escondido`) com "Exibir menu" e o símbolo na linha do título, e a faixa `aj-exibir-menu` para a tela sem cabeçalho de página; `aj-trilho__item--duas-linhas`. Corrija a frase da barra inferior do portal.
- `components/Navegacao/preview.html`: sem os estilos embutidos substituídos; acrescente uma quarta coluna com a barra de voltar.
- `components/CabecalhoDePagina/README.md` e `preview.html`: `aj-cabecalho__inicio`, `aj-cabecalho__menu` e `aj-cabecalho__texto--folgado`; prévia sem os estilos embutidos substituídos.
- `components/Botao/README.md` e `preview.html`: o estado desabilitado.
- `components/Painel/README.md`: `aj-painel--estreito` (formulário curto: 520px no desktop, 560px no tablet, largura toda e sombra de repouso no mobile) e `aj-painel__acoes`.
- `components/BarraDeAcoes/README.md`: um botão só ocupa a largura toda no mobile.
- Novos, cada um com README e prévia: `CartaoAtalho` (grupo "Conteúdo"), `Totais` ("Conteúdo"), `EstadoDaTela` ("Retorno"), `FaixaDeErro` ("Retorno") e `Entrar` ("Estrutura", com `width=1440`). A marcação de cada prévia é a da tela correspondente nas Tarefas 4, 6 e 7.

- [ ] **Step 5: Publicar e conferir**

Uma chamada de publicação com os arquivos mudados e o índice. Abra o artifact no Chrome e confira, na página do design system, que os cinco componentes novos aparecem com a prévia desenhada e que as prévias de Navegação, Cabeçalho de página, Botão e Aviso continuam iguais.

- [ ] **Step 6: Copiar para o código**

`wwwroot/css/tokens.css` recebe o `tokens.css` do design system, sem mudança. `wwwroot/css/componentes.css` recebe o `bundle.css`, com a primeira linha de comentário trocada pelas duas do cabeçalho atual do arquivo:

```css
/* Araújo Advocacia — componentes do design system (classes aj-), escritos sobre os tokens de tokens.css.
   Cópia de components/bundle.css (https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS); altere lá primeiro.
```

Confira: o corpo do `componentes.css` (da linha "Visões:" em diante) é idêntico ao do `bundle.css` publicado.

- [ ] **Step 7: Build e commit**

Run: `dotnet build`
Expected: sem erros nem avisos.

Nada muda na tela ainda: o `app.css` carrega depois e mantém os valores de hoje.

```
git add src/AtividadesJuridicas.Web/wwwroot/css
git commit -m "feat(web): design system com blocos por visão, barra superior por tela e padrões das telas simples"
```

---

### Task 2: Ícones, aviso, rótulo de campo e a trava das telas migradas

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/Icone.razor`
- Create: `src/AtividadesJuridicas.Web/Components/Layout/TipoDeAviso.cs`
- Create: `src/AtividadesJuridicas.Web/Components/Layout/Aviso.razor`
- Create: `src/AtividadesJuridicas.Web/Components/Layout/RotuloDeCampo.razor`
- Create: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`
- Create: `tests/AtividadesJuridicas.IntegrationTests/ComponentesDoDesignSystemTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/TelaDeTeste.cs`

**Interfaces:**
- Consumes: as classes `aj-aviso`, `aj-rotulo`, `aj-campo-erro` do `componentes.css`.
- Produces:
  - `<Icone Nome="…" Classe="…" />`, com os nomes novos `adicionar`, `buscar`, `filtros`, `voltar`, `avancar`, `confirmar`, `excluir`, `devolver`, `entrar`, `baixar`, `atencao`, `erro`.
  - `enum TipoDeAviso { Sucesso, Atencao, Erro }` e `<Aviso Tipo="TipoDeAviso.Erro">conteúdo</Aviso>`.
  - `<RotuloDeCampo Texto="…" Para="() => modelo.Campo">campo</RotuloDeCampo>`, para uso dentro de `EditForm`.
  - `TelaDeTeste.RenderizarSemDadosAsync<TComponente>(Dictionary<string, object?> parametros)`.
  - `TelasNoDesignSystemTests.Migradas`: a lista que cada tarefa de tela aumenta.

**Regras:**
- Os traçados novos são os do grupo Icones do design system. Confira cada um contra o arquivo do grupo (ação `read` do artifact com o id do arquivo, que está em `design-system.json`).
- `Aviso` de erro tem `role="alert"`; os outros, `role="status"`. O ícone é o do tipo: `confirmar`, `atencao`, `erro`.
- `RotuloDeCampo` acompanha a validação do `EditContext`: em erro, o rótulo ganha `aj-rotulo--erro` e cada mensagem sai num `<span class="aj-campo-erro">`, como no canvas (o `ValidationMessage` do Blazor emite `<div>`, inválido dentro de `<label>`). O campo recebe `aria-invalid` do próprio `InputBase`. Fora de um `EditForm`, o rótulo aparece sem erro.

- [ ] **Step 1: Escrever os testes que falham**

`tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`:

```csharp
using System.Text.RegularExpressions;

namespace AtividadesJuridicas.UnitTests;

/// <summary>
/// Tela migrada para o design system não volta atrás: nada de estilo embutido nem de classe fora do prefixo
/// <c>aj-</c>. Cada fase da migração acrescenta os seus arquivos a <see cref="Migradas"/>.
/// </summary>
public partial class TelasNoDesignSystemTests
{
    // Caminhos a partir de src/AtividadesJuridicas.Web.
    private static readonly string[] Migradas =
    [
        "Components/Layout/Aviso.razor",
        "Components/Layout/RotuloDeCampo.razor",
    ];

    [GeneratedRegex("\\sstyle\\s*=\\s*[\"']")]
    private static partial Regex EstiloEmbutido();

    // Só o valor fixo: classe montada por expressão Razor (com @) é conferida na revisão.
    [GeneratedRegex("\\sclass\\s*=\\s*(?:\"([^\"@]*)\"|'([^'@]*)')")]
    private static partial Regex ClasseFixa();

    /// <summary>O que a marcação tem de fora do design system: "style" para estilo embutido, ou o nome da classe.</summary>
    private static List<string> Violacoes(string marcacao)
    {
        var violacoes = new List<string>();
        if (EstiloEmbutido().IsMatch(marcacao)) violacoes.Add("style");

        violacoes.AddRange(ClasseFixa().Matches(marcacao)
            .SelectMany(m => (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(classe => !classe.StartsWith("aj-", StringComparison.Ordinal)));
        return violacoes;
    }

    public static TheoryData<string> Arquivos => new(Migradas);

    [Theory]
    [MemberData(nameof(Arquivos))]
    public void Tela_migrada_nao_tem_estilo_embutido_nem_classe_fora_do_design_system(string arquivo)
    {
        var caminho = Path.Combine(
            [GravacoesPorForaTests.RaizDoRepositorio(), "src", "AtividadesJuridicas.Web", .. arquivo.Split('/')]);

        Violacoes(File.ReadAllText(caminho)).Should().BeEmpty(
            "tela migrada não tem estilo embutido nem classe fora do design system: o padrão vira classe aj- lá");
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
    [InlineData("<p class=\"aj-aviso aj-aviso--erro\">x</p>")]
    [InlineData("<label class=\"@(erro ? \"aj-rotulo aj-rotulo--erro\" : \"aj-rotulo\")\">x</label>")]
    [InlineData("<Icone Nome=\"voltar\" Classe=\"aj-cartao__seta\" />")]
    public void A_trava_aceita_marcacao_do_design_system(string marcacao)
    {
        Violacoes(marcacao).Should().BeEmpty();
    }
}
```

Em `TelaDeTeste.cs`, acrescente o desenho sem banco:

```csharp
    /// <summary>Desenha um componente que não consulta nada: sem aplicação, sem banco.</summary>
    public static async Task<string> RenderizarSemDadosAsync<TComponente>(Dictionary<string, object?> parametros)
        where TComponente : IComponent
    {
        await using var servicos = new ServiceCollection().BuildServiceProvider();
        await using var renderizador = new HtmlRenderer(servicos, NullLoggerFactory.Instance);

        return await renderizador.Dispatcher.InvokeAsync(async () =>
            (await renderizador.RenderComponentAsync<TComponente>(ParameterView.FromDictionary(parametros))).ToHtmlString());
    }
```

`tests/AtividadesJuridicas.IntegrationTests/ComponentesDoDesignSystemTests.cs`:

```csharp
using AtividadesJuridicas.Web.Components.Layout;
using Microsoft.AspNetCore.Components;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>Os blocos de tela do design system, desenhados sozinhos.</summary>
public class ComponentesDoDesignSystemTests
{
    [Theory]
    [InlineData("adicionar")]
    [InlineData("buscar")]
    [InlineData("filtros")]
    [InlineData("voltar")]
    [InlineData("avancar")]
    [InlineData("confirmar")]
    [InlineData("excluir")]
    [InlineData("devolver")]
    [InlineData("entrar")]
    [InlineData("baixar")]
    [InlineData("atencao")]
    [InlineData("erro")]
    public async Task Icone_do_design_system_tem_tracado(string nome)
    {
        var html = await TelaDeTeste.RenderizarSemDadosAsync<Icone>(new() { ["Nome"] = nome });

        html.Should().MatchRegex("<svg[^>]*aria-hidden=\"true\"[^>]*>\\s*<(path|circle|rect)");
    }

    [Fact]
    public async Task Icone_aceita_classe()
    {
        var html = await TelaDeTeste.RenderizarSemDadosAsync<Icone>(
            new() { ["Nome"] = "avancar", ["Classe"] = "aj-cartao__seta" });

        html.Should().Contain("class=\"aj-cartao__seta\"");
    }

    [Theory]
    [InlineData(TipoDeAviso.Sucesso, "aj-aviso aj-aviso--sucesso", "status", "M5 12.5l4.5 4.5L19 7.5")]
    [InlineData(TipoDeAviso.Atencao, "aj-aviso aj-aviso--atencao", "status", "M12 4l9 16H3z")]
    [InlineData(TipoDeAviso.Erro, "aj-aviso aj-aviso--erro", "alert", "M12 7.5v5.5")]
    public async Task Aviso_traz_a_classe_o_papel_e_o_icone_do_tipo(
        TipoDeAviso tipo, string classe, string papel, string tracado)
    {
        RenderFragment texto = construtor => construtor.AddContent(0, "Revisor alterado.");

        var html = await TelaDeTeste.RenderizarSemDadosAsync<Aviso>(
            new() { ["Tipo"] = tipo, ["ChildContent"] = texto });

        // O traçado é o do ícone do tipo: confirmar, atenção ou erro.
        html.Should().Contain($"class=\"{classe}\"").And.Contain($"role=\"{papel}\"")
            .And.Contain($"d=\"{tracado}\"").And.Contain("<span>Revisor alterado.</span>");
    }
}
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --filter TelasNoDesignSystemTests`
Expected: FAIL, os arquivos de `Migradas` não existem.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter ComponentesDoDesignSystemTests`
Expected: erro de compilação, `Aviso` e `TipoDeAviso` não existem.

- [ ] **Step 3: Implementar**

`Icone.razor`: acrescente ao dicionário, depois de `atrasada`:

```csharp
        ["adicionar"] = new("<path d=\"M12 5v14\"></path><path d=\"M5 12h14\"></path>"),
        ["buscar"] = new("<circle cx=\"11\" cy=\"11\" r=\"6.5\"></circle><path d=\"M16 16l4 4\"></path>"),
        ["filtros"] = new("<path d=\"M4 6h10\"></path><path d=\"M18 6h2\"></path><circle cx=\"16\" cy=\"6\" r=\"2\"></circle><path d=\"M4 12h4\"></path><path d=\"M12 12h8\"></path><circle cx=\"10\" cy=\"12\" r=\"2\"></circle><path d=\"M4 18h12\"></path><circle cx=\"18\" cy=\"18\" r=\"2\"></circle>"),
        ["voltar"] = new("<path d=\"M15 6l-6 6 6 6\"></path>"),
        ["avancar"] = new("<path d=\"M9 6l6 6-6 6\"></path>"),
        ["confirmar"] = new("<path d=\"M5 12.5l4.5 4.5L19 7.5\"></path>"),
        ["excluir"] = new("<path d=\"M4 7h16\"></path><path d=\"M10 11v6\"></path><path d=\"M14 11v6\"></path><path d=\"M6 7l1 12a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2l1-12\"></path><path d=\"M9 7V4h6v3\"></path>"),
        ["devolver"] = new("<path d=\"M9 14l-5-5 5-5\"></path><path d=\"M4 9h10a6 6 0 0 1 0 12h-3\"></path>"),
        ["entrar"] = new("<path d=\"M5 12h14\"></path><path d=\"M13 6l6 6-6 6\"></path>"),
        ["baixar"] = new("<path d=\"M12 4v11\"></path><path d=\"M7.5 11l4.5 4.5 4.5-4.5\"></path><path d=\"M5 19.5h14\"></path>"),
        ["atencao"] = new("<path d=\"M12 4l9 16H3z\"></path><path d=\"M12 10v4\"></path><path d=\"M12 17v.5\"></path>"),
        ["erro"] = new("<circle cx=\"12\" cy=\"12\" r=\"9\"></circle><path d=\"M12 7.5v5.5\"></path><path d=\"M12 16v.5\"></path>"),
```

O `<svg>` ganha `class="@Classe"` e o componente, o parâmetro:

```csharp
    /// <summary>Classe do design system para o ícone solto (fora de menu e de botão), que traz tamanho e cor.</summary>
    [Parameter] public string? Classe { get; set; }
```

`TipoDeAviso.cs`:

```csharp
namespace AtividadesJuridicas.Web.Components.Layout;

/// <summary>As três famílias de retorno do design system.</summary>
public enum TipoDeAviso
{
    Sucesso,
    Atencao,
    Erro,
}
```

`Aviso.razor`:

```razor
@* Faixa de retorno do design system: sucesso, atenção ou erro, sempre com o ícone do tipo. *@
<p class="@Classe" role="@(Tipo == TipoDeAviso.Erro ? "alert" : "status")"><Icone Nome="@NomeDoIcone" /><span>@ChildContent</span></p>

@code {
    [Parameter, EditorRequired] public TipoDeAviso Tipo { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string Classe => Tipo switch
    {
        TipoDeAviso.Sucesso => "aj-aviso aj-aviso--sucesso",
        TipoDeAviso.Atencao => "aj-aviso aj-aviso--atencao",
        _ => "aj-aviso aj-aviso--erro",
    };

    private string NomeDoIcone => Tipo switch
    {
        TipoDeAviso.Sucesso => "confirmar",
        TipoDeAviso.Atencao => "atencao",
        _ => "erro",
    };
}
```

`RotuloDeCampo.razor`:

```razor
@typeparam TValor
@implements IDisposable
@using System.Linq.Expressions

@* Rótulo de campo do design system: em erro, o rótulo muda de cor e a mensagem aparece sob o campo.
   A mensagem é escrita aqui, em <span>, como no canvas: o ValidationMessage emite <div>, que não cabe num <label>. *@
@{ var mensagens = Mensagens(); }
<label class="@(mensagens.Count > 0 ? "aj-rotulo aj-rotulo--erro" : "aj-rotulo")">
    @Texto
    @ChildContent
    @foreach (var mensagem in mensagens)
    {
        <span class="aj-campo-erro">@mensagem</span>
    }
</label>

@code {
    private EditContext? _assinado;

    [CascadingParameter] private EditContext? Contexto { get; set; }

    [Parameter, EditorRequired] public string Texto { get; set; } = string.Empty;

    /// <summary>O campo do modelo, como em <c>ValidationMessage</c>.</summary>
    [Parameter, EditorRequired] public Expression<Func<TValor>> Para { get; set; } = default!;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    // Fora de um EditForm não há o que validar: o rótulo aparece sem erro.
    private List<string> Mensagens() =>
        Contexto is null ? [] : Contexto.GetValidationMessages(FieldIdentifier.Create(Para)).ToList();

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(Contexto, _assinado)) return;

        if (_assinado is not null) _assinado.OnValidationStateChanged -= AoValidar;
        _assinado = Contexto;
        if (_assinado is not null) _assinado.OnValidationStateChanged += AoValidar;
    }

    private void AoValidar(object? sender, ValidationStateChangedEventArgs e) => _ = InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_assinado is not null) _assinado.OnValidationStateChanged -= AoValidar;
    }
}
```

O estado de erro do `RotuloDeCampo` é testado pela tela de login, na Tarefa 7.

- [ ] **Step 4: Rodar e ver passar**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --filter TelasNoDesignSystemTests`
Expected: PASS, 10 casos (2 arquivos e 8 da própria trava).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter ComponentesDoDesignSystemTests`
Expected: PASS, 16 casos.

- [ ] **Step 5: Commit**

```
git add src/AtividadesJuridicas.Web/Components/Layout tests
git commit -m "feat(web): ícones do design system, aviso, rótulo de campo e trava das telas migradas"
```

---

### Task 3: Layout — barra superior por tela, "Mais" ativo e cabeçalho de página

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Layout/Encaixes.cs`
- Create: `src/AtividadesJuridicas.Web/Components/Layout/ContextoDoLayout.cs`
- Create: `src/AtividadesJuridicas.Web/Components/Layout/CabecalhoDePagina.razor`
- Create: `src/AtividadesJuridicas.Web/Components/Layout/FocoNoTitulo.razor`
- Create: `src/AtividadesJuridicas.Web/Components/Layout/FocoNoTitulo.razor.js`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/MainLayout.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Routes.razor`
- Modify: `src/AtividadesJuridicas.Web/wwwroot/app.css`
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`
- Create: `tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs`

**Interfaces:**
- Consumes: `Icone` com `voltar` e `menu`; as classes da Tarefa 1.
- Produces:
  - `Encaixes.BarraSuperior` (`"barra-superior"`).
  - `sealed record ContextoDoLayout(string Iniciais, EventCallback AlternarMenu)`, entregue por `CascadingValue` ao conteúdo.
  - `<CabecalhoDePagina Titulo="…" Subtitulo="…" SubtituloMobile="…" Folgado="true|false" VoltarPara="/…" RotuloDoVoltar="…" />`. `Titulo` é obrigatório; os demais, opcionais. Com `VoltarPara`, a barra do mobile é a de voltar e não mostra subtítulo.
  - `<FocoNoTitulo RouteData="routeData" />`.

**Regras:**
- A ordem dentro de `aj-app__coluna` é: encaixe da barra superior, barra padrão, faixa `aj-exibir-menu`, `main`.
- Dentro de `main` ficam só o conteúdo da tela e o valor em cascata: a faixa `aj-exibir-menu` sai de dentro dele e `div.pagina` deixa de existir.
- No trilho e na barra inferior, o destino atual tem `aria-current="page"` só com o menu fechado e a página montada. "Mais" tem `aria-current="page"` com o menu aberto, ou quando a tela atual está no menu do perfil e fora daquela barra. Minha senha e página não encontrada não acendem nada.
- O menu lateral continua com `NavLink`; o `b` do nome do usuário perde a classe `quem`, que não tem estilo.
- Os itens de duas linhas do trilho trocam o `style` por `aj-trilho__item--duas-linhas`.
- `Carregando…` do layout passa a `<p class="aj-subtitulo">`.
- `app.css`: sai o bloco "Estrutura: menu que abre e esconde" inteiro (está no design system), saem `.pagina` e `.pagina > h1:first-child`, entra a regra interina e as duas regras de link passam a `:where()`.

- [ ] **Step 1: Escrever os testes que falham**

`tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs`:

```csharp
using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>O que o layout decide sozinho: o destino aceso em cada barra e a barra superior do mobile.</summary>
[Collection(ColecaoBanco.Nome)]
public class LayoutWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _app.DisposeAsync();

    // Sem a barra na página, as asserções de "nada aceso" passariam sem provar nada.
    private static string Navegacao(string html, string classe)
    {
        var barra = Regex.Match(html, $"<nav class=\"{classe}\"[^>]*>.*?</nav>", RegexOptions.Singleline);
        barra.Success.Should().BeTrue($"a página deve trazer a navegação {classe}");
        return barra.Value;
    }

    // O destino aceso de uma barra: o href do link, ou "mais" quando é o botão.
    private static string[] Acesos(string barra) =>
        Regex.Matches(barra, "<(a|button)\\b[^>]*aria-current=\"page\"[^>]*>")
            .Select(m => m.Groups[1].Value == "button"
                ? "mais"
                : Regex.Match(m.Value, "href=\"([^\"]*)\"").Groups[1].Value)
            .ToArray();

    private async Task<string> PaginaAsync(HttpClient cliente, string rota) =>
        WebUtility.HtmlDecode(await cliente.GetStringAsync(rota));

    [Theory]
    [InlineData("/atividades", "/atividades", "/atividades")]
    [InlineData("/clientes", "/clientes", "mais")]
    [InlineData("/clientes?busca=maria", "/clientes", "mais")]
    [InlineData("/areas", "mais", "mais")]
    [InlineData("/documentos-comuns", "mais", "mais")]
    public async Task Diretoria_ve_o_destino_ou_mais_aceso_em_cada_barra(string rota, string noTrilho, string naBarra)
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = await PaginaAsync(admin, rota);

        Acesos(Navegacao(html, "aj-trilho")).Should().Equal(noTrilho);
        Acesos(Navegacao(html, "aj-nav-inferior")).Should().Equal(naBarra);
    }

    [Theory]
    [InlineData("/minha-senha")]
    [InlineData("/rota-que-nao-existe")]
    public async Task Minha_senha_e_pagina_nao_encontrada_nao_acendem_nada(string rota)
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await (await admin.GetAsync(rota)).Content.ReadAsStringAsync());

        Acesos(Navegacao(html, "aj-trilho")).Should().BeEmpty();
        Acesos(Navegacao(html, "aj-nav-inferior")).Should().BeEmpty();
    }

    [Fact]
    public async Task Producao_em_tela_que_o_menu_dela_nao_tem_nao_acende_mais()
    {
        await _app.CriarPedroAsync();
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = await PaginaAsync(pedro, "/usuarios");

        Acesos(Navegacao(html, "aj-trilho")).Should().BeEmpty();
        Acesos(Navegacao(html, "aj-nav-inferior")).Should().BeEmpty();
    }

    [Fact]
    public async Task Producao_em_clientes_tem_o_destino_no_trilho_e_mais_na_barra()
    {
        await _app.CriarPedroAsync();
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = await PaginaAsync(pedro, "/clientes");

        Acesos(Navegacao(html, "aj-trilho")).Should().Equal("/clientes");
        Acesos(Navegacao(html, "aj-nav-inferior")).Should().Equal("mais");
    }

    [Fact]
    public async Task Cliente_em_pagina_que_o_layout_nao_monta_nao_acende_nada()
    {
        await _app.CriarCarlaAsync();
        var carla = await _app.ClienteAutenticadoAsync(
            UsuariosDeClienteDeTeste.EmailDaCarla, UsuariosDeClienteDeTeste.SenhaDaCarla);

        var html = await PaginaAsync(carla, "/clientes");

        html.Should().Contain("Página não encontrada");
        Acesos(Navegacao(html, "aj-trilho")).Should().BeEmpty();
        Acesos(Navegacao(html, "aj-nav-inferior")).Should().BeEmpty();
    }

    [Fact]
    public async Task Modulo_do_foco_no_titulo_e_servido_e_pagina_estatica_usa_o_foco_do_blazor()
    {
        var anonimo = _app.NovoCliente();

        var modulo = await anonimo.GetAsync("/Components/Layout/FocoNoTitulo.razor.js");
        modulo.StatusCode.Should().Be(HttpStatusCode.OK);
        (await modulo.Content.ReadAsStringAsync()).Should().Contain("export function focarTituloVisivel");

        // Sem circuito (entrar, erro) quem foca o título é o FocusOnNavigate; com circuito, o módulo.
        (await anonimo.GetStringAsync("/conta/entrar")).Should().Contain("<blazor-focus-on-navigate");
        var admin = await _app.ClienteAutenticadoAsync();
        (await admin.GetStringAsync("/")).Should().NotContain("<blazor-focus-on-navigate");
    }

    [Fact]
    public async Task Cliente_em_documentos_comuns_tem_o_destino_no_trilho_e_mais_na_barra()
    {
        await _app.CriarCarlaAsync();
        var carla = await _app.ClienteAutenticadoAsync(
            UsuariosDeClienteDeTeste.EmailDaCarla, UsuariosDeClienteDeTeste.SenhaDaCarla);

        var html = await PaginaAsync(carla, "/documentos-comuns");

        Acesos(Navegacao(html, "aj-trilho")).Should().Equal("/documentos-comuns");
        Acesos(Navegacao(html, "aj-nav-inferior")).Should().Equal("mais");
    }

    [Fact]
    public async Task Menu_lateral_marca_o_destino_atual()
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = await PaginaAsync(admin, "/clientes");

        var menu = Regex.Match(html, "<aside class=\"aj-menu\".*?</aside>", RegexOptions.Singleline).Value;
        Regex.Matches(menu, "<a\\b[^>]*aria-current=\"page\"[^>]*>")
            .Select(m => Regex.Match(m.Value, "href=\"([^\"]*)\"").Groups[1].Value)
            .Should().Equal("/clientes");
    }

    [Fact]
    public async Task Tela_ainda_nao_migrada_fica_com_a_barra_de_logo_e_sem_involucro()
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = await PaginaAsync(admin, "/atividades");

        Regex.Matches(html, "<header class=\"aj-barra-superior").Should().ContainSingle();
        html.Should().Contain("<header class=\"aj-barra-superior aj-barra-superior--padrao\">")
            .And.Contain("class=\"aj-barra-superior__logo\"")
            .And.NotContain("class=\"pagina\"");
        // O conteúdo antigo continua lá, direto no main; a faixa de exibir o menu fica fora dele.
        var corpo = Regex.Match(html, "<main class=\"aj-app__conteudo\">.*?</main>", RegexOptions.Singleline).Value;
        corpo.Should().Contain("<h1").And.NotContain("Exibir menu");
        html.Should().MatchRegex("<div class=\"aj-exibir-menu\">\\s*<button[^>]*aria-label=\"Exibir menu\"");
    }

    [Theory]
    [InlineData("Madonna", "M")]
    [InlineData("Maria Aparecida dos Santos Albuquerque Cavalcanti", "MC")]
    public async Task Avatar_da_barra_superior_traz_as_iniciais(string nome, string iniciais)
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var producao = await _app.EmEscopoAsync(null, async sp =>
            (await sp.GetRequiredService<AppDbContext>().Perfis.AsNoTracking()
                .SingleAsync(p => p.Tipo == TipoPerfil.Producao && p.Ativo)).Id);
        var criado = await _app.ExecutarAsync(adminId, new CriarUsuario(nome, "nome@x.com", "senha-do-nome", producao, null));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        var cliente = await _app.ClienteAutenticadoAsync("nome@x.com", "senha-do-nome");

        var html = await PaginaAsync(cliente, "/atividades");

        var barra = Regex.Match(html, "<header class=\"aj-barra-superior.*?</header>", RegexOptions.Singleline).Value;
        barra.Should().MatchRegex($"<a href=\"/minha-senha\" class=\"aj-avatar\"[^>]*>{iniciais}</a>");
    }
}
```

Em `TelasNoDesignSystemTests.Migradas`, acrescente:

```csharp
        "Components/Layout/CabecalhoDePagina.razor",
        "Components/Layout/MainLayout.razor",
```

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter LayoutWebTests`
Expected: FAIL. "Mais" nunca está aceso, a barra padrão não tem a classe e `class="pagina"` existe.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --filter TelasNoDesignSystemTests`
Expected: FAIL, `MainLayout.razor` tem `style` e a classe `quem`; `CabecalhoDePagina.razor` não existe.

- [ ] **Step 3: Encaixe, contexto e cabeçalho de página**

`Encaixes.cs`:

```csharp
namespace AtividadesJuridicas.Web.Components.Layout;

/// <summary>Os pontos do layout que a tela preenche com <c>SectionContent</c>.</summary>
public static class Encaixes
{
    /// <summary>A barra superior do mobile, antes do conteúdo e fora da área que rola.</summary>
    public const string BarraSuperior = "barra-superior";
}
```

`ContextoDoLayout.cs`:

```csharp
using Microsoft.AspNetCore.Components;

namespace AtividadesJuridicas.Web.Components.Layout;

/// <summary>O que o layout entrega às telas: as iniciais do usuário, para o avatar da barra superior, e o comando de exibir o menu.</summary>
public sealed record ContextoDoLayout(string Iniciais, EventCallback AlternarMenu);
```

`CabecalhoDePagina.razor`:

```razor
@* Cabeçalho de página do design system. Emite as duas formas e o CSS mostra a da visão atual: no desktop e no
   tablet, o aj-cabecalho dentro do conteúdo; no mobile, a barra superior da tela, no encaixe do layout.
   Com VoltarPara, a barra do mobile é a de voltar (ficha e formulário), e a barra inferior some. *@
<SectionContent SectionName="@Encaixes.BarraSuperior">
    @if (VoltarPara is not null)
    {
        <header class="aj-barra-superior aj-barra-superior--voltar">
            <a href="@VoltarPara" class="aj-botao aj-botao--fantasma aj-botao--icone" aria-label="@(RotuloDoVoltar ?? "Voltar")"><Icone Nome="voltar" /></a>
            <h1>@Titulo</h1>
            <span class="aj-barra-superior__vazio" aria-hidden="true"></span>
        </header>
    }
    else
    {
        <header class="aj-barra-superior">
            <div class="aj-barra-superior__linha">
                <img src="img/simbolo-claro.svg" alt="Araújo Advocacia" width="24" height="27" />
                <div class="aj-barra-superior__titulo">
                    <h1 class="aj-titulo">@Titulo</h1>
                    @if ((SubtituloMobile ?? Subtitulo) is { } subtituloMobile)
                    {
                        <span class="aj-subtitulo">@subtituloMobile</span>
                    }
                </div>
                <a href="/minha-senha" class="aj-avatar" aria-label="Minha conta">@Contexto?.Iniciais</a>
            </div>
        </header>
    }
</SectionContent>

<div class="aj-desktop-tablet">
    <header class="aj-cabecalho">
        <div class="aj-cabecalho__inicio">
            <span class="aj-cabecalho__menu">
                <button type="button" class="aj-botao aj-botao--secundario aj-botao--icone" aria-label="Exibir menu" @onclick="ExibirMenuAsync"><Icone Nome="menu" /></button>
                <img src="img/simbolo-claro.svg" alt="Araújo Advocacia" width="29" height="32" />
            </span>
            <div class="@(Folgado ? "aj-cabecalho__texto aj-cabecalho__texto--folgado" : "aj-cabecalho__texto")">
                <h1 class="aj-titulo">@Titulo</h1>
                @if (Subtitulo is not null)
                {
                    <p class="aj-subtitulo">@Subtitulo</p>
                }
            </div>
        </div>
    </header>
</div>

@code {
    [CascadingParameter] private ContextoDoLayout? Contexto { get; set; }

    [Parameter, EditorRequired] public string Titulo { get; set; } = string.Empty;

    /// <summary>A linha de contexto sob o título, no desktop e no tablet.</summary>
    [Parameter] public string? Subtitulo { get; set; }

    /// <summary>A linha de contexto da barra superior do mobile, quando o quadro a encurta. Sem ela, vale <see cref="Subtitulo"/>.</summary>
    [Parameter] public string? SubtituloMobile { get; set; }

    /// <summary>Subtítulo em frase: o quadro afasta um pouco mais o título.</summary>
    [Parameter] public bool Folgado { get; set; }

    /// <summary>Destino do voltar da barra superior do mobile. Informado, a barra é a de voltar.</summary>
    [Parameter] public string? VoltarPara { get; set; }

    /// <summary>O rótulo acessível do voltar: "Voltar para Início". Sem ele, o botão diz só "Voltar".</summary>
    [Parameter] public string? RotuloDoVoltar { get; set; }

    private Task ExibirMenuAsync() => Contexto is { } contexto ? contexto.AlternarMenu.InvokeAsync() : Task.CompletedTask;
}
```

- [ ] **Step 4: `MainLayout.razor`**

O trilho, com `<a>` e `aria-current` calculado (o `<img>` da logo fica como está):

```razor
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
```

A barra inferior:

```razor
    <nav class="aj-nav-inferior" aria-label="Principal">
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
```

A coluna de conteúdo:

```razor
    <div class="aj-app__coluna">
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
        </main>
    </div>
```

No bloco `@code`, acrescente:

```csharp
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

    // Com o menu aberto por cima da tela, quem fica aceso nas barras é "Mais", como no canvas.
    private string? Atual(string destino) => !_menuAberto && PaginaMontada && Secao == destino ? "page" : null;

    private string? MaisNoTrilho => MaisAceso(EhCliente ? TrilhoDoCliente : TrilhoDaEquipe);

    private string? MaisNaBarra => MaisAceso(EhCliente ? BarraDoCliente : BarraDaEquipe);

    private string? MaisAceso(string[] naBarra) =>
        _menuAberto || (PaginaMontada && DestinosDoMenu.Contains(Secao) && !naBarra.Contains(Secao)) ? "page" : null;

    private ContextoDoLayout NovoContexto() => new(Iniciais, EventCallback.Factory.Create(this, AlternarMenu));
```

Em `OnInitializedAsync`, monte o contexto antes da primeira renderização e de novo quando o usuário chegar:

```csharp
        _marcaLida = true;
        _contexto = NovoContexto();

        _ator = await Despachante.EnviarAsync(new ObterMeuAcesso());
        _contexto = NovoContexto();
```

`Routes.razor`: `<FocusOnNavigate RouteData="routeData" Selector="h1" />` dá lugar a `<FocoNoTitulo RouteData="routeData" />`.

- [ ] **Step 5: Foco no título visível**

`FocoNoTitulo.razor`:

```razor
@inject IJSRuntime JS

@* Depois de cada navegação, o foco vai para o título da página, que é anunciado. Cada visão tem o seu h1 e os
   das outras ficam escondidos; por isso não serve o FocusOnNavigate, que mira o primeiro h1 do HTML.
   Nas páginas sem circuito (entrar, erro) não há quem chame o módulo: lá o h1 é um só e vale o do Blazor. *@
@if (AssignedRenderMode is null)
{
    <FocusOnNavigate RouteData="RouteData" Selector="h1" />
}

@code {
    private Type? _ultimaPagina;
    private bool _focar;

    [Parameter, EditorRequired] public RouteData RouteData { get; set; } = default!;

    protected override void OnParametersSet()
    {
        if (RouteData.PageType == _ultimaPagina) return;

        _ultimaPagina = RouteData.PageType;
        _focar = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_focar) return;

        _focar = false;
        try
        {
            await using var modulo = await JS.InvokeAsync<IJSObjectReference>(
                "import", "./Components/Layout/FocoNoTitulo.razor.js");
            await modulo.InvokeVoidAsync("focarTituloVisivel");
        }
        catch (JSDisconnectedException)
        {
            // O circuito caiu no meio da navegação: não há quem focar.
        }
        catch (JSException)
        {
            // O foco no título é uma ajuda: uma falha do módulo não pode derrubar a tela.
        }
    }
}
```

`FocoNoTitulo.razor.js`:

```js
// Cada visão tem o seu h1 e os das outras ficam escondidos: o foco vai para o que está à vista.
export function focarTituloVisivel() {
    for (const titulo of document.querySelectorAll("h1")) {
        if (titulo.getClientRects().length === 0) continue;

        if (!titulo.hasAttribute("tabindex")) titulo.setAttribute("tabindex", "-1");
        titulo.focus({ preventScroll: true });
        return;
    }
}
```

- [ ] **Step 6: `app.css`**

Apague do comentário `/* ---------- Estrutura: menu que abre e esconde ---------- */` até o fim do bloco `@media (max-width: 760px) { .aj-app__conteudo { padding-bottom: 96px; } }`. Apague `.pagina { display: block; }` e `.pagina > h1:first-child { margin-top: 0; }`. No lugar do bloco de estrutura, entra:

```css
/* ---------- Interino, até a fase 9G ---------- */
/* Tela ainda não migrada (nenhum filho direto com classe aj-) continua em bloco, com as margens da marcação antiga.
   Tela migrada usa a coluna flexível do design system. */
.aj-app__conteudo:not(:has(> [class^="aj-"])) { display: block; }
```

O comentário de `h1:focus` passa a citar `FocoNoTitulo` no lugar de `FocusOnNavigate`.

As duas regras de link do topo do arquivo passam a ter especificidade zero, para toda classe do design system vencer:

```css
:where(a) { color: var(--verde); }
:where(a:hover) { color: var(--tinta); }
```

- [ ] **Step 7: Rodar e ver passar**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "LayoutWebTests|PortalDoClienteWebTests|DocumentosDoPortalWebTests|AutenticacaoWebTests|PaginasProtegidasTests"`
Expected: PASS. Os testes de navegação que já existiam continuam passando sem mudança.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --filter TelasNoDesignSystemTests`
Expected: PASS, 12 casos.

- [ ] **Step 8: Commit**

```
git add src/AtividadesJuridicas.Web tests
git commit -m "feat(web): layout recebe a barra superior de cada tela e acende Mais nas telas fora da barra"
```

---

### Task 4: Início — equipe e cliente

Quadros: `Inicio`, `InicioTablet`, `InicioMobile`, `PortalInicio`, `PortalInicioTablet`, `PortalInicioMobile`.

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Layout/CartaoAtalho.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Home.razor`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PortalDoClienteWebTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`

**Interfaces:**
- Consumes: `CabecalhoDePagina`, `Aviso`, `Icone`; as classes `aj-grade`, `aj-cartao--atalho`, `aj-totais`, `aj-total`.
- Produces: `<CartaoAtalho Href="/…" NomeDoIcone="…" Titulo="…" Descricao="…" />`.

**Regras:**
- A saudação é uma expressão só. O atalho "Minha senha" sai do corpo do início do cliente: o quadro tem três atalhos, e a senha segue no menu e no avatar.
- O componente `TotaisDoAndamento` continua existindo: a página de andamento o usa até a fase 9B. O início deixa de usá-lo.
- Enquanto carrega, o cabeçalho aparece sem subtítulo e o conteúdo é a frase `Carregando…`.

- [ ] **Step 1: Reescrever o teste do início do cliente e acrescentar os novos**

Em `PortalDoClienteWebTests`, o teste `Inicio_do_cliente_mostra_os_totais_e_os_atalhos` passa a:

```csharp
    [Fact]
    public async Task Inicio_do_cliente_mostra_os_totais_e_os_atalhos()
    {
        await CriarAndamentoDaMariaAsync();
        var cliente = await DaCarlaAsync();

        var html = Decodificado(await cliente.GetStringAsync("/"));

        var corpo = Regex.Match(html, "<main[^>]*>.*?</main>", RegexOptions.Singleline).Value;
        corpo.Should().MatchRegex(@"<span class=""aj-ajuda"">Em andamento</span>\s*<b>2</b>")
            .And.MatchRegex(@"<span class=""aj-ajuda"">Concluídas</span>\s*<b>1</b>");
        // O quadro tem três atalhos; a senha fica no menu e no avatar.
        Regex.Matches(corpo, "href=\"([^\"]*)\"").Select(m => m.Groups[1].Value)
            .Should().Equal("/andamento", "/meus-documentos", "/documentos-comuns");
        corpo.Should().Contain("Olá, Carla Cliente.").And.NotContain("Carregando").And.NotContain("Contestação trabalhista");
    }
```

Em `LayoutWebTests`, acrescente:

```csharp
    private static string BarraDaTela(string html) =>
        Regex.Match(html, "<header class=\"aj-barra-superior\">.*?</header>", RegexOptions.Singleline).Value;

    [Fact]
    public async Task Inicio_da_equipe_tem_o_cabecalho_os_atalhos_e_a_barra_superior_da_tela()
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = await PaginaAsync(admin, "/");

        var corpo = Regex.Match(html, "<main[^>]*>.*?</main>", RegexOptions.Singleline).Value;
        corpo.Should().Contain("<h1 class=\"aj-titulo\">Início</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Olá, Diana Diretora. Seu perfil é de Diretoria.</p>")
            .And.Contain("Minhas atividades").And.Contain("Prazos, situações e o que espera por você.")
            .And.Contain("Agenda da semana").And.Contain("Audiências, reuniões e prazos dos próximos dias.");
        Regex.Matches(corpo, "href=\"([^\"]*)\"").Select(m => m.Groups[1].Value)
            .Should().Equal("/atividades", "/agenda");

        // No mobile o título vai para a barra superior da tela, que vem antes da padrão.
        var barra = BarraDaTela(html);
        barra.Should().Contain("<h1 class=\"aj-titulo\">Início</h1>")
            .And.Contain("<span class=\"aj-subtitulo\">Olá, Diana Diretora</span>")
            .And.MatchRegex("<a href=\"/minha-senha\" class=\"aj-avatar\"[^>]*>DD</a>");
        html.IndexOf("<header class=\"aj-barra-superior\">", StringComparison.Ordinal)
            .Should().BeLessThan(html.IndexOf("aj-barra-superior--padrao", StringComparison.Ordinal));
        Acesos(Navegacao(html, "aj-nav-inferior")).Should().Equal("/");
    }

    [Fact]
    public async Task Inicio_do_cliente_nao_fala_do_perfil()
    {
        await _app.CriarCarlaAsync();
        var carla = await _app.ClienteAutenticadoAsync(
            UsuariosDeClienteDeTeste.EmailDaCarla, UsuariosDeClienteDeTeste.SenhaDaCarla);

        var html = await PaginaAsync(carla, "/");

        html.Should().Contain("<p class=\"aj-subtitulo\">Olá, Carla Cliente.</p>")
            .And.Contain("<span class=\"aj-subtitulo\">Olá, Carla Cliente</span>")
            .And.Contain("O que o escritório está fazendo por você.")
            .And.Contain("Arquivos que o escritório compartilhou com você.")
            .And.Contain("Modelos e orientações para todos os clientes.")
            .And.NotContain("Seu perfil é de");
    }
```

Em `TelasNoDesignSystemTests.Migradas`, acrescente `"Components/Layout/CartaoAtalho.razor"` e `"Components/Pages/Home.razor"`.

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "Inicio_d"`
Expected: FAIL, o início ainda tem `<dt>`/`<dd>` e parágrafos com links.

- [ ] **Step 3: Implementar**

`CartaoAtalho.razor`:

```razor
@* Cartão-atalho do design system: ícone num quadro, título, uma linha de descrição e a seta. O cartão inteiro é o link. *@
<a href="@Href" class="aj-cartao aj-cartao--atalho">
    <span class="aj-cartao__icone" aria-hidden="true"><Icone Nome="@NomeDoIcone" /></span>
    <span class="aj-cartao__texto"><span class="aj-cartao__titulo">@Titulo</span><span class="aj-cartao__meta">@Descricao</span></span>
    <Icone Nome="avancar" Classe="aj-cartao__seta" />
</a>

@code {
    [Parameter, EditorRequired] public string Href { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string NomeDoIcone { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Titulo { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Descricao { get; set; } = string.Empty;
}
```

`Home.razor`, a marcação (o bloco `@code` mantém a carga de hoje e ganha as duas saudações):

```razor
@page "/"
@inject IDespachante Despachante

<PageTitle>Início</PageTitle>

<CabecalhoDePagina Titulo="Início" Subtitulo="@Saudacao" SubtituloMobile="@SaudacaoCurta" />

@if (!_carregado)
{
    <p class="aj-subtitulo">Carregando…</p>
}
else if (_ator is null)
{
    <Aviso Tipo="TipoDeAviso.Erro">Seu acesso foi encerrado. Entre novamente.</Aviso>
}
else if (_ator.EhCliente)
{
    @if (_erroDosTotais is not null)
    {
        <Aviso Tipo="TipoDeAviso.Erro">@_erroDosTotais</Aviso>
    }
    else if (_totais is { } totais)
    {
        <div class="aj-totais">
            <div class="aj-painel aj-total"><span class="aj-ajuda">Em andamento</span><b>@totais.EmAndamento</b></div>
            <div class="aj-painel aj-total"><span class="aj-ajuda">Concluídas</span><b>@totais.Concluidas</b></div>
        </div>
    }
    <div class="aj-grade">
        <CartaoAtalho Href="/andamento" NomeDoIcone="atividades" Titulo="Andamento" Descricao="O que o escritório está fazendo por você." />
        <CartaoAtalho Href="/meus-documentos" NomeDoIcone="documento" Titulo="Meus documentos" Descricao="Arquivos que o escritório compartilhou com você." />
        <CartaoAtalho Href="/documentos-comuns" NomeDoIcone="pasta" Titulo="Documentos comuns" Descricao="Modelos e orientações para todos os clientes." />
    </div>
}
else
{
    <div class="aj-grade">
        <CartaoAtalho Href="/atividades" NomeDoIcone="atividades" Titulo="Minhas atividades" Descricao="Prazos, situações e o que espera por você." />
        <CartaoAtalho Href="/agenda" NomeDoIcone="agenda" Titulo="Agenda da semana" Descricao="Audiências, reuniões e prazos dos próximos dias." />
    </div>
}
```

```csharp
    // Cada saudação é uma expressão só: o HTML traz o texto inteiro, sem marcador no meio.
    private string? Saudacao => _ator switch
    {
        null => null,
        { EhCliente: true } => $"Olá, {_ator.Nome}.",
        _ => $"Olá, {_ator.Nome}. Seu perfil é de {RotulosDePerfil.DoTipo(_ator.TipoPerfil)}.",
    };

    private string? SaudacaoCurta => _ator is null ? null : $"Olá, {_ator.Nome}";
```

- [ ] **Step 4: Rodar e ver passar**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "LayoutWebTests|PortalDoClienteWebTests|AutenticacaoWebTests|PaginasProtegidasTests"`
Expected: PASS.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --filter TelasNoDesignSystemTests`
Expected: PASS, 14 casos.

- [ ] **Step 5: Commit**

```
git add src/AtividadesJuridicas.Web tests
git commit -m "feat(web): início da equipe e do cliente no design system, com cartões-atalho e totais em painéis"
```

---

### Task 5: Minha senha

Quadros: `MinhaSenha`, `MinhaSenhaTablet`, `MinhaSenhaMobile`.

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/MinhaSenha.razor`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`

**Interfaces:**
- Consumes: `CabecalhoDePagina` com `VoltarPara`, `RotuloDeCampo`, `Aviso`; `aj-painel--estreito`, `aj-painel__acoes`, `aj-acoes--fixa`.
- Produces: nada que outra tarefa consuma.

**Regras:**
- O formulário é o próprio painel. O botão de enviar aparece duas vezes, uma por visão: na linha de ações do painel (desktop e tablet) e na barra fixa (mobile). Os dois ficam dentro do `<form>`.
- No mobile a frase do subtítulo vira `aj-ajuda` acima do painel, como no quadro.
- A mensagem geral de falha ("A senha atual não confere." e as demais do caso de uso) é um `Aviso` de erro logo abaixo do cabeçalho.
- O bloco `@code` não muda.

- [ ] **Step 1: Escrever o teste que falha**

Em `LayoutWebTests`:

```csharp
    [Fact]
    public async Task Minha_senha_tem_o_painel_o_voltar_do_mobile_e_um_botao_por_visao()
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = await PaginaAsync(admin, "/minha-senha");

        var corpo = Regex.Match(html, "<main[^>]*>.*?</main>", RegexOptions.Singleline).Value;
        corpo.Should().Contain("<h1 class=\"aj-titulo\">Minha senha</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Depois de alterar, você entra de novo com a nova senha.</p>")
            .And.Contain("<span class=\"aj-ajuda\">Depois de alterar, você entra de novo com a nova senha.</span>")
            .And.MatchRegex("<form[^>]*class=\"aj-painel aj-painel--estreito\"")
            .And.Contain("Senha atual").And.Contain("Nova senha").And.Contain("Confirme a nova senha");
        Regex.Matches(corpo, "type=\"password\"").Should().HaveCount(3);
        Regex.Matches(corpo, "<input[^>]*class=\"aj-campo").Should().HaveCount(3);
        // Um botão de envio para desktop e tablet, outro para o mobile, os dois dentro do formulário.
        var formulario = Regex.Match(corpo, "<form.*?</form>", RegexOptions.Singleline).Value;
        Regex.Matches(formulario, "<button type=\"submit\" class=\"aj-botao aj-botao--primario\">Alterar senha</button>")
            .Should().HaveCount(2);
        formulario.Should().MatchRegex("<div class=\"aj-so-mobile\">\\s*<div class=\"aj-acoes aj-acoes--fixa\">");

        // A barra do mobile é a de voltar, e a tela não tem link dentro do conteúdo.
        var barra = Regex.Match(html, "<header class=\"aj-barra-superior aj-barra-superior--voltar\">.*?</header>",
            RegexOptions.Singleline).Value;
        barra.Should().MatchRegex("<a href=\"/\"[^>]*aria-label=\"Voltar para Início\"")
            .And.Contain("<h1>Minha senha</h1>");
        corpo.Should().NotContain("href=");
    }
```

Em `TelasNoDesignSystemTests.Migradas`, acrescente `"Components/Pages/MinhaSenha.razor"`.

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter Minha_senha_tem_o_painel`
Expected: FAIL, a tela ainda tem `h1` solto e `form.formulario`.

- [ ] **Step 3: Implementar**

```razor
@page "/minha-senha"
@using System.ComponentModel.DataAnnotations
@inject IDespachante Despachante
@inject NavigationManager Navegacao

<PageTitle>Minha senha</PageTitle>

<CabecalhoDePagina Titulo="Minha senha" Subtitulo="Depois de alterar, você entra de novo com a nova senha." Folgado="true"
                   VoltarPara="/" RotuloDoVoltar="Voltar para Início" />

@if (_mensagem is not null)
{
    <Aviso Tipo="TipoDeAviso.Erro">@_mensagem</Aviso>
}

<div class="aj-so-mobile"><span class="aj-ajuda">Depois de alterar, você entra de novo com a nova senha.</span></div>

<EditForm Model="_form" OnValidSubmit="SalvarAsync" FormName="minha-senha" class="aj-painel aj-painel--estreito">
    <DataAnnotationsValidator />

    <RotuloDeCampo Texto="Senha atual" Para="() => _form.SenhaAtual">
        <InputText @bind-Value="_form.SenhaAtual" type="password" autocomplete="current-password" class="aj-campo" />
    </RotuloDeCampo>

    <RotuloDeCampo Texto="Nova senha" Para="() => _form.NovaSenha">
        <InputText @bind-Value="_form.NovaSenha" type="password" autocomplete="new-password" class="aj-campo" />
    </RotuloDeCampo>

    <RotuloDeCampo Texto="Confirme a nova senha" Para="() => _form.Confirmacao">
        <InputText @bind-Value="_form.Confirmacao" type="password" autocomplete="new-password" class="aj-campo" />
    </RotuloDeCampo>

    <div class="aj-desktop-tablet">
        <div class="aj-painel__acoes"><button type="submit" class="aj-botao aj-botao--primario">Alterar senha</button></div>
    </div>
    <div class="aj-so-mobile">
        <div class="aj-acoes aj-acoes--fixa"><button type="submit" class="aj-botao aj-botao--primario">Alterar senha</button></div>
    </div>
</EditForm>
```

O campo `_falhou` deixa de ser lido pela marcação: remova-o, e a atribuição dele em `SalvarAsync`.

- [ ] **Step 4: Rodar e ver passar**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "LayoutWebTests|Qualquer_usuario_autenticado_abre_a_troca_de_senha|Cliente_abre_a_troca_de_senha"`
Expected: PASS.

- [ ] **Step 5: Commit**

```
git add src/AtividadesJuridicas.Web tests
git commit -m "feat(web): minha senha no design system, com painel estreito e barra de voltar no mobile"
```

---

### Task 6: Não encontrada, erro, faixa de erro e reconexão

Quadros: `NaoEncontrada`, `NaoEncontradaTablet`, `NaoEncontradaMobile`. Erro, faixa de erro e reconexão não têm quadro: seguem o design system.

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/NaoEncontrada.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Erro.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/LayoutVazio.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/App.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/ReconnectModal.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/ReconnectModal.razor.css`
- Modify: `src/AtividadesJuridicas.Web/wwwroot/app.css`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/AutenticacaoWebTests.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`

**Interfaces:**
- Consumes: `aj-estado`, `aj-estado--pagina`, `aj-faixa-erro`, `aj-faixa-erro__fechar`; `Icone` com `fechar`.
- Produces: `LayoutVazio` sem elemento próprio (a página traz o seu `<main>`), para a Tarefa 7.

**Regras:**
- A página não encontrada não usa `CabecalhoDePagina`: no mobile vale a barra padrão, de logo, como no quadro. O `h1` é um só, dentro do conteúdo.
- A página de erro é o mesmo bloco, em tela inteira, fora do layout.
- A faixa de erro mantém o `id="blazor-error-ui"` e as classes `reload` e `dismiss`, que o Blazor procura. O fechar vira botão de ícone com `aria-label="Fechar aviso"`.
- Reconexão: textos em português; a caixa usa `superficie`, `borda`, `raio-16` e `sombra-flutuante`; o fundo usa `veu`; os botões são `aj-botao aj-botao--primario`. Sem animação. As regras que mostram cada mensagem conforme a classe que o Blazor aplica ao diálogo continuam como estão.
- Textos da reconexão: "Reconectando ao servidor…"; "A conexão caiu. Nova tentativa em {n} s."; "Não foi possível reconectar. Tente de novo ou recarregue a página." com o botão "Tentar de novo"; "A sessão foi pausada pelo servidor." com o botão "Retomar"; "Não foi possível retomar a sessão. Tente de novo ou recarregue a página."
- `app.css`: saem `.centro`, `.centro:has(.entrar)` e o bloco "Erro do Blazor".

- [ ] **Step 1: Escrever os testes que falham**

Em `AutenticacaoWebTests`, os dois testes ganham a conferência da marcação (as asserções que já existem ficam):

```csharp
    [Fact]
    public async Task Pagina_de_erro_e_anonima_generica_e_sem_detalhes_tecnicos()
    {
        var resposta = await _cliente.GetAsync("/erro");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync());
        html.Should().Contain("Ocorreu um erro inesperado.");
        html.Should().NotContain("Exception").And.NotContain("StackTrace").And.NotContain("   at ");
        html.Should().MatchRegex("<main class=\"aj-estado aj-estado--pagina\">\\s*<img[^>]*>\\s*<h1 class=\"aj-titulo\">Ocorreu um erro inesperado\\.</h1>")
            .And.Contain("<a href=\"/\" class=\"aj-botao aj-botao--secundario\">Voltar ao início</a>");
    }

    [Fact]
    public async Task Rota_inexistente_para_usuario_autenticado_mostra_pagina_nao_encontrada()
    {
        await EntrarAsync("diana@x.com", "senha-segura");

        var resposta = await _cliente.GetAsync("/rota-que-nao-existe");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var html = WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync());
        html.Should().Contain("Página não encontrada").And.Contain("href=\"/\"");
        var corpo = Regex.Match(html, "<main[^>]*>.*?</main>", RegexOptions.Singleline).Value;
        corpo.Should().MatchRegex("<div class=\"aj-estado\">\\s*<img[^>]*>\\s*<h1 class=\"aj-titulo\">Página não encontrada</h1>\\s*<p class=\"aj-subtitulo\">O endereço acessado não existe\\.</p>\\s*<a href=\"/\" class=\"aj-botao aj-botao--secundario\">Voltar ao início</a>");
        // No mobile vale a barra de logo: a tela não entrega barra própria.
        Regex.Matches(html, "<header class=\"aj-barra-superior").Should().ContainSingle();
        Regex.Matches(html, "<h1").Should().ContainSingle();
    }

    [Fact]
    public async Task Faixa_de_erro_e_aviso_de_reconexao_estao_em_portugues()
    {
        await EntrarAsync("diana@x.com", "senha-segura");

        var html = WebUtility.HtmlDecode(await _cliente.GetStringAsync("/"));

        html.Should().MatchRegex("<div id=\"blazor-error-ui\" class=\"aj-faixa-erro\"")
            .And.Contain("aria-label=\"Fechar aviso\"")
            .And.Contain("Reconectando ao servidor…").And.Contain("Tentar de novo").And.Contain("Retomar")
            .And.NotContain("Rejoin").And.NotContain("Retry").And.NotContain("Resume");
    }
```

Em `TelasNoDesignSystemTests.Migradas`, acrescente `"Components/Pages/NaoEncontrada.razor"` e `"Components/Pages/Erro.razor"`.

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "Pagina_de_erro|Rota_inexistente_para_usuario|Faixa_de_erro"`
Expected: FAIL nos três.

- [ ] **Step 3: Implementar**

`NaoEncontrada.razor`:

```razor
@page "/nao-encontrada"

<PageTitle>Página não encontrada</PageTitle>

<div class="aj-estado">
    <img src="img/simbolo-claro.svg" alt="" width="44" height="49" />
    <h1 class="aj-titulo">Página não encontrada</h1>
    <p class="aj-subtitulo">O endereço acessado não existe.</p>
    <a href="/" class="aj-botao aj-botao--secundario">Voltar ao início</a>
</div>
```

`Erro.razor` (as diretivas do topo ficam):

```razor
<PageTitle>Erro</PageTitle>

<main class="aj-estado aj-estado--pagina">
    <img src="img/simbolo-claro.svg" alt="" width="44" height="49" />
    <h1 class="aj-titulo">Ocorreu um erro inesperado.</h1>
    <p class="aj-subtitulo">Não foi possível concluir a operação. Tente novamente em instantes.</p>
    <a href="/" class="aj-botao aj-botao--secundario">Voltar ao início</a>
</main>
```

`LayoutVazio.razor`:

```razor
@inherits LayoutComponentBase

@* Sem moldura: a página (entrar, erro) ocupa a janela e traz o seu próprio <main>. *@
@Body
```

`App.razor`, a faixa de erro:

```razor
    <div id="blazor-error-ui" class="aj-faixa-erro" data-nosnippet>
        Ocorreu um erro inesperado.
        <a href="." class="reload">Recarregar</a>
        <button type="button" class="dismiss aj-botao aj-botao--fantasma aj-botao--icone aj-faixa-erro__fechar" aria-label="Fechar aviso"><Icone Nome="fechar" /></button>
    </div>
```

O comentário de `App.razor` sobre o `app.css` passa a "app.css traz a base da aplicação e veste as telas ainda não migradas".

`ReconnectModal.razor` (a linha do `<script>` fica):

```razor
<dialog id="components-reconnect-modal" data-nosnippet>
    <div class="components-reconnect-container">
        <p class="components-reconnect-first-attempt-visible">
            Reconectando ao servidor…
        </p>
        <p class="components-reconnect-repeated-attempt-visible">
            A conexão caiu. Nova tentativa em <span id="components-seconds-to-next-attempt"></span> s.
        </p>
        <p class="components-reconnect-failed-visible">
            Não foi possível reconectar. Tente de novo ou recarregue a página.
        </p>
        <button id="components-reconnect-button" class="aj-botao aj-botao--primario components-reconnect-failed-visible">
            Tentar de novo
        </button>
        <p class="components-pause-visible">
            A sessão foi pausada pelo servidor.
        </p>
        <p class="components-resume-failed-visible">
            Não foi possível retomar a sessão. Tente de novo ou recarregue a página.
        </p>
        <button id="components-resume-button" class="aj-botao aj-botao--primario components-pause-visible components-resume-failed-visible">
            Retomar
        </button>
    </div>
</dialog>
```

`ReconnectModal.razor.css`: mantenha as duas primeiras regras (o que cada estado mostra), tirando delas `.components-rejoining-animation`; o restante, com as animações, dá lugar a:

```css
#components-reconnect-modal {
    width: min(22rem, calc(100vw - 2rem));
    margin: 20vh auto;
    padding: var(--espaco-24);
    border: 1px solid var(--borda);
    border-radius: var(--raio-16);
    background: var(--superficie);
    color: var(--tinta);
    font: 400 15px/1.5 var(--font-sans);
    box-shadow: var(--sombra-flutuante);
}

#components-reconnect-modal::backdrop {
    background: var(--veu);
}

.components-reconnect-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: var(--espaco-14);
    text-align: center;
}

#components-reconnect-modal p {
    margin: 0;
}
```

- [ ] **Step 4: Rodar e ver passar**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "AutenticacaoWebTests|LayoutWebTests|PortalDoClienteWebTests|DocumentosDoPortalWebTests"`
Expected: PASS. Os testes que procuram "Página não encontrada" nas páginas vedadas continuam passando.

- [ ] **Step 5: Commit**

```
git add src/AtividadesJuridicas.Web tests
git commit -m "feat(web): página não encontrada, erro, faixa de erro e reconexão no design system"
```

---

### Task 7: Entrar

Quadros: `Main`, `LoginTablet`, `LoginMobile`.

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Conta/Entrar.razor`
- Modify: `src/AtividadesJuridicas.Web/wwwroot/app.css`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/AutenticacaoWebTests.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`

**Interfaces:**
- Consumes: `RotuloDeCampo`, `Aviso`, `Icone` com `entrar`; as classes `aj-entrar…`; `LayoutVazio` sem `<main>`.
- Produces: nada que outra tarefa consuma.

**Regras:**
- O bloco `@code` só muda no nome da propriedade do aviso da consulta, que passa a `AvisoDaConsulta` com `[SupplyParameterFromQuery(Name = "aviso")]`, para não colidir com o componente `Aviso`.
- Os avisos (senha alterada, credencial errada) ficam dentro do cartão, logo abaixo do título.
- A frase sob o título tem duas redações, uma por visão, como nos quadros.
- O rodapé aparece duas vezes: dentro do cartão (desktop) e no pé da página (tablet e mobile). O CSS do componente mostra um ou outro.
- `app.css`: sai o bloco "Entrar" inteiro.

- [ ] **Step 1: Escrever os testes que falham**

Em `AutenticacaoWebTests`:

```csharp
    [Fact]
    public async Task Pagina_de_login_e_seu_css_sao_publicos()
    {
        (await _cliente.GetAsync("/conta/entrar")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _cliente.GetAsync("/app.css")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _cliente.GetAsync("/css/tokens.css")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _cliente.GetAsync("/css/componentes.css")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _cliente.GetAsync("/img/logo-vertical-ampla-escuro.svg")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Tela_de_login_segue_o_quadro()
    {
        var html = WebUtility.HtmlDecode(await _cliente.GetStringAsync("/conta/entrar"));

        html.Should().Contain("<span class=\"aj-entrar__sobretitulo\">Sistema interno</span>")
            .And.Contain("<p class=\"aj-entrar__nome\">Atividades Jurídicas</p>")
            .And.Contain("Agenda, prazos e atividades do escritório em um só lugar.")
            .And.Contain("<span class=\"aj-desktop-tablet\">Acesse com o e-mail e a senha do escritório.</span>")
            .And.Contain("<span class=\"aj-so-mobile\">Use o e-mail e a senha do escritório.</span>")
            .And.Contain("placeholder=\"nome@araujoadvocacia.com.br\"")
            .And.MatchRegex("<button type=\"submit\" class=\"aj-botao aj-botao--primario\"><span>Entrar</span>\\s*<svg");
        Regex.Matches(html, "Araújo Advocacia · uso interno").Should().HaveCount(2);
        Regex.Matches(html, "<main").Should().ContainSingle();
        Regex.Matches(html, "<h1").Should().ContainSingle();
        html.Should().NotContain("aj-aviso");
    }

    [Fact]
    public async Task Login_com_campos_vazios_marca_os_campos_e_mantem_o_formulario()
    {
        var resposta = await EntrarAsync("", "");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync());
        Regex.Matches(html, "<label class=\"aj-rotulo aj-rotulo--erro\">").Should().HaveCount(2);
        html.Should().Contain("<span class=\"aj-campo-erro\">Informe o e-mail.</span>")
            .And.Contain("<span class=\"aj-campo-erro\">Informe a senha.</span>");
        Regex.Matches(html, "aria-invalid=\"true\"").Should().HaveCount(2);
        // O formulário continua pronto para a próxima tentativa.
        TokenRegex().IsMatch(html).Should().BeTrue();
        html.Should().Contain("name=\"Entrada.Email\"").And.Contain("name=\"Entrada.Senha\"");
    }

    [Fact]
    public async Task Login_errado_mostra_o_aviso_de_erro_dentro_do_cartao()
    {
        var resposta = await EntrarAsync("diana@x.com", "senha-errada");

        var html = WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync());
        var cartao = Regex.Match(html, "<form[^>]*class=\"aj-entrar__cartao\".*?</form>", RegexOptions.Singleline).Value;
        cartao.Should().MatchRegex("<p class=\"aj-aviso aj-aviso--erro\" role=\"alert\">\\s*<svg.*?</svg>\\s*<span>E-mail ou senha inválidos\\.</span></p>");
        cartao.Should().NotContain("aj-rotulo--erro");
    }

    [Fact]
    public async Task Login_depois_da_troca_de_senha_mostra_o_aviso_de_sucesso()
    {
        var html = WebUtility.HtmlDecode(await _cliente.GetStringAsync("/conta/entrar?aviso=senha-alterada"));

        html.Should().MatchRegex("<p class=\"aj-aviso aj-aviso--sucesso\" role=\"status\">\\s*<svg.*?</svg>\\s*<span>Senha alterada\\. Entre novamente com a nova senha\\.</span></p>");
    }
```

Em `TelasNoDesignSystemTests.Migradas`, acrescente `"Components/Pages/Conta/Entrar.razor"`.

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "Tela_de_login|Login_com_campos_vazios|Login_errado_mostra|Login_depois_da_troca"`
Expected: FAIL nos quatro.

- [ ] **Step 3: Implementar**

A marcação de `Entrar.razor`:

```razor
<PageTitle>Entrar</PageTitle>

<div class="aj-entrar">
    <section class="aj-entrar__marca" aria-label="Araújo Advocacia">
        <img class="aj-entrar__dagua" src="img/simbolo-claro.svg" alt="" width="700" height="782" />
        <img class="aj-entrar__logo" src="img/logo-vertical-ampla-escuro.svg" alt="Araújo Advocacia" width="236" height="227" />
        <div class="aj-entrar__texto">
            <span class="aj-entrar__sobretitulo">Sistema interno</span>
            <p class="aj-entrar__nome">Atividades Jurídicas</p>
            <p class="aj-entrar__frase">Agenda, prazos e atividades do escritório em um só lugar.</p>
        </div>
    </section>

    <main class="aj-entrar__area">
        <EditForm Model="Entrada" FormName="entrar" OnValidSubmit="EntrarAsync" method="post" class="aj-entrar__cartao">
            <DataAnnotationsValidator />

            <div class="aj-entrar__titulo">
                <h1>Entrar</h1>
                <p><span class="aj-desktop-tablet">Acesse com o e-mail e a senha do escritório.</span><span class="aj-so-mobile">Use o e-mail e a senha do escritório.</span></p>
            </div>

            @if (AvisoDaConsulta == "senha-alterada")
            {
                <Aviso Tipo="TipoDeAviso.Sucesso">Senha alterada. Entre novamente com a nova senha.</Aviso>
            }

            @if (_erro is not null)
            {
                <Aviso Tipo="TipoDeAviso.Erro">@_erro</Aviso>
            }

            <div class="aj-entrar__campos">
                <RotuloDeCampo Texto="E-mail" Para="() => Entrada.Email">
                    <InputText @bind-Value="Entrada.Email" type="email" inputmode="email" autocomplete="username" placeholder="nome@araujoadvocacia.com.br" class="aj-campo" />
                </RotuloDeCampo>

                <RotuloDeCampo Texto="Senha" Para="() => Entrada.Senha">
                    <InputText @bind-Value="Entrada.Senha" type="password" autocomplete="current-password" class="aj-campo" />
                </RotuloDeCampo>

                <button type="submit" class="aj-botao aj-botao--primario"><span>Entrar</span><Icone Nome="entrar" /></button>
            </div>

            <p class="aj-entrar__rodape aj-entrar__rodape--cartao">Araújo Advocacia · uso interno</p>
        </EditForm>

        <p class="aj-entrar__rodape aj-entrar__rodape--pagina">Araújo Advocacia · uso interno</p>
    </main>
</div>
```

No `app.css`, apague do comentário `/* ---------- Entrar ---------- */` até o fim do bloco `@media (max-width: 760px) { .entrar__… }`.

- [ ] **Step 4: Rodar e ver passar**

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --filter "AutenticacaoWebTests|BloqueioDeLoginTests|ConcorrenciaDeLoginTests|SessaoWebTests|PaginasProtegidasTests|PortalDoClienteWebTests"`
Expected: PASS. Todo teste que entra pelo formulário continua entrando.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --filter TelasNoDesignSystemTests`
Expected: PASS, 18 casos.

- [ ] **Step 5: Commit**

```
git add src/AtividadesJuridicas.Web tests
git commit -m "feat(web): tela de login fiel aos quadros nas três visões, com erros e avisos do design system"
```

---

### Task 8: Suíte inteira e conferência no navegador

Feita pela sessão que coordena.

**Files:**
- Modify: `docs/superpowers/specs/2026-10-10-migracao-para-o-design-system-design.md` (o que a execução ensinou e muda a spec)

**Regras:**
- A conferência usa o ambiente descartável das fases anteriores: banco em contêiner na porta 5599, aplicação publicada em Release rodando em `http://localhost:5197`. O contêiner e a pasta publicada saem ao fim.
- Desktop: a janela do Chrome, em 1440px ou mais.
- Tablet e mobile: numa aba em `/conta/entrar` (página estática, sem circuito), troque o corpo por uma moldura da mesma origem com a largura e a altura do quadro, pela ferramenta de JavaScript do navegador:

```js
document.body.innerHTML = '<iframe src="/minha-senha" style="width:390px;height:844px;border:0;display:block"></iframe>';
```

  A moldura tem os cookies da sessão e responde às consultas de mídia pela própria largura. Use 834×1194 para o tablet e 390×844 para o mobile.
- Cada tela é comparada com o quadro do canvas aberto ao lado (página "Componentes", quadro "Mapa das telas", em modo de apresentação).

- [ ] **Step 1: Suíte inteira**

Run: `dotnet build` e depois `dotnet test`, em segundo plano.
Expected: sem avisos; os totais da linha de base mais os testes novos, nenhum falhando.

- [ ] **Step 2: Subir o ambiente descartável e entrar como Diretoria e como cliente**

Crie pelo sistema um usuário de Produção, um cliente e um usuário de cliente, para ver os três perfis.

- [ ] **Step 3: Conferir cada tela nas três larguras, contra o quadro**

| Tela | Rota | Perfis |
|---|---|---|
| Entrar | `/conta/entrar` | anônimo; com campos vazios; com senha errada; com `?aviso=senha-alterada` |
| Início | `/` | Diretoria, Produção, cliente |
| Minha senha | `/minha-senha` | Diretoria; com a confirmação diferente; com a senha atual errada |
| Não encontrada | `/rota-que-nao-existe` | Diretoria; cliente em `/clientes` |
| Erro | `/erro` | anônimo |
| Menu aberto por "Mais" | qualquer | Diretoria, no tablet e no mobile |
| Menu escondido | `/` | Diretoria, no desktop |

Para cada uma: estrutura, textos, medidas dos blocos, cores, ícones e o que está aceso na navegação.

- [ ] **Step 4: Conferir o que o Review Focus pede**

- Telas não migradas (`/atividades`, `/clientes`, `/agenda`, `/usuarios`, `/andamento` como cliente) nas três larguras: aparência igual à da `main`, com a barra de logo no mobile.
- Molduras de 760, 761, 1199 e 1200px de largura em `/` e em `/minha-senha`: uma só navegação, um só título e um só botão de envio à vista em cada uma.
- Teclado, no desktop e numa moldura de 390px: Tab percorre só o que está à vista; em Minha senha, Enter envia uma vez; depois de navegar pelo menu, o foco está no título visível (`document.activeElement.tagName` é `H1` e ele tem caixa).
- Usuário com o nome "Maria Aparecida dos Santos Albuquerque Cavalcanti", em `/` numa moldura de 390px: a saudação corta com reticências e a página não rola para o lado.
- Cursor sobre "Minha senha" no bloco do usuário do menu e sobre "Voltar ao início" da página não encontrada: o texto continua legível, na cor do componente.
- Menu escondido no desktop em `/` (cabeçalho de página: o botão fica na linha do título) e em `/atividades` (tela não migrada: a faixa acima do conteúdo). Nos dois, "Exibir menu" traz o menu de volta.

- [ ] **Step 5: Corrigir o que divergir**

Divergência de CSS se corrige no design system (Tarefa 1, de novo: publicar e copiar), nunca no `app.css` nem com `style`. Divergência de marcação se corrige na tela, com o teste correspondente ajustado antes.

- [ ] **Step 6: Derrubar o ambiente, atualizar a spec e fechar**

Remova o contêiner e a pasta publicada. Se a execução mostrou algo que muda a spec (uma regra do CSS que não funcionou como previsto, um quadro que mudou), emende a spec com a data. Commit:

```
git add docs
git commit -m "docs(spec): o que a fase 9A ensinou sobre a migração para o design system"
```

Mostre ao usuário as capturas das telas nas três larguras e peça o aval para a entrada na `main`.
