# Fase 9C — Administração no design system: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** As telas da administração (Áreas, Perfis, Usuários com o formulário em tela própria, Produtores e Auditoria) ficam idênticas aos quadros do canvas nas três visões, e a migração ganha as peças que as fases seguintes reutilizam: arranjo lado a lado, cabeçalho de detalhe com voltar, recado entre telas, vínculo com busca, painel de filtros e controle segmentado no cabeçalho.

**Architecture:** Cada padrão que os quadros da administração trazem como estilo embutido vira classe `aj-` no artifact do design system e é copiado para `wwwroot/css`. Cada tela emite a forma do desktop num invólucro `aj-so-desktop` e a do tablet e do mobile num invólucro `aj-tablet-mobile`; o CSS mostra uma. Lista e painel lado a lado ficam num invólucro `aj-lado-a-lado`, que é uma linha no desktop e some (`display: contents`) no tablet e no mobile, deixando os blocos empilharem na ordem da marcação. O formulário de usuário sai da lista e vira tela com rota; a mensagem de sucesso atravessa a navegação por um serviço de escopo do circuito.

**Tech Stack:** .NET 10, Blazor Interactive Server, CSS do design system (tokens e classes `aj-`), xUnit 2.9, FluentAssertions 7.2, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-10-10-migracao-para-o-design-system-design.md` (seções 3, 4, 5, 7.3, 8, 9, 10, 11 e 15).

## Decisões deste plano

- **Fonte dos quadros: a cópia local do canvas de 2026-10-10** (versão `1791598628-21e1`), que tem os 18 quadros da fase. A spec manda baixar o canvas de novo a cada fase; nesta sessão a ferramenta Artifact está conectada a outra conta do claude.ai e não abre o canvas nem o design system. A Tarefa 1 só começa com o acesso de volta; o primeiro passo dela é conferir se algum quadro desta fase mudou.
- **O design system vem primeiro (spec 4.1), e leva junto o que a 9B deixou pendente:** as três regras que a conferência da 9B pediu e não puderam ser publicadas (quebra de nome longo no aviso e no título do painel, cor do link da tabela, item de menu desabilitado). Os arquivos prontos estão em `C:\Users\Gabriel Paulino\.claude\projects\c--GitHub-SIstemaControleAtividadesJuridicas\design-system-pendente-9b\`.
- **Lado a lado por CSS, sem duplicar o painel.** O formulário "Nova área" ou "Novo perfil" aparece uma vez só na marcação. No desktop o invólucro `aj-lado-a-lado` é uma linha (lista à esquerda, painel à direita); no tablet e no mobile ele some e os blocos seguem a ordem da marcação, que é a ordem do quadro: em Áreas o painel vem antes da lista, em Perfis depois.
- **O formulário de usuário vira tela** (`/usuarios/novo` e `/usuarios/{id}/editar`, spec 5). Salvar volta à lista, que mostra "Usuário salvo." por um recado (`Recados`, serviço de escopo do circuito, spec 4.3). No desktop "Inativar" e "Ativar" continuam na linha da tabela; no tablet e no mobile os cartões levam à edição, e a edição ganha o painel "Situação" com a ação (spec 8). A confirmação de inativação, com as pendências do usuário, vira um componente do assunto (`Components/Usuarios/ConfirmacaoDeInativacao.razor`), usado pela lista e pela edição.
- **`CabecalhoDeDetalhe` nasce aqui** (spec 4.4): o `aj-voltar` acima do cabeçalho no desktop e no tablet e a barra de voltar no mobile. O formulário de usuário é o primeiro de vários (cliente, processo, atividade, evento).
- **`CabecalhoDePagina` ganha dois encaixes:** `Complemento`, ao lado do título no desktop e no tablet, e `LinhaDoMobile`, a segunda linha da barra superior. A auditoria põe o controle segmentado nos dois.
- **Produtores guarda o produtor no endereço** (`/produtores?produtor={id}`, spec 5). Sem produtor no endereço não há painel: a consulta de produtividade é auditada e continua só acontecendo quando a Diretoria escolhe um produtor. "Abrir" continua abrindo a ficha da atividade dentro do painel, como hoje, porque a rota da ficha (`/atividades/{id}`) só nasce na 9E; lá ela passa a navegar. O botão "Fechar" do painel sai: escolher outro produtor troca o painel.
- **Auditoria:** os dados aparecem resumidos (`ResumoDeAuditoria`: o JSON gravado vira "Campo: valor · Campo: de → para", sem perder nada) e os identificadores truncados em oito caracteres com reticências (`IdentificadorCurto`). Na aba "Consultas", que não tem filtro por identificador, o identificador do recurso fica inteiro. O painel de filtros do tablet e do mobile (spec 9) traz os campos que não cabem na linha de cima. A paginação continua sempre à vista, como hoje.
- **Inativar área ou perfil continua sem confirmação**, como hoje: é reversível por "Ativar". No menu de ações "Inativar" não leva a cor de erro, que é das ações que destroem.
- **"1 usuários ativos" do quadro vira "1 usuário ativo"** (`Contagem.De`).
- **Subtítulo de contagem só com algo a contar:** com zero itens, falha ou ainda carregando, o subtítulo não aparece (como na 9B).
- **Menu de ações:** fechar tocando fora devolve o foco ao botão (spec 15, abertura da 9C). O resto da revisão de teclado do menu fica para a 9G.
- **`TelaDeTeste.RenderizarAsync` passa a chamar `RenderizarComAsync`** (spec 15) e as duas registram `Recados`.

## Como ler este plano

Cada tarefa traz as **regras** (o contrato), as **interfaces** que ela consome e produz e os **testes obrigatórios**. O plano traz o código onde ele é a própria fidelidade (o CSS, a marcação das telas) ou onde o detalhe é sutil. O restante é escrito no estilo do que já existe. O código do plano não foi executado: quando o Blazor ou o compilador exigirem, ajuste o mínimo para preservar o comportamento e registre o desvio no relatório, com a mensagem de erro que o motivou.

Nunca apague um teste nem afrouxe uma asserção. Quando uma asserção mudar porque a marcação mudou, reescreva-a mantendo o que ela prova e liste-a no relatório. Asserção negativa (`NotContain`) que deixaria de provar algo com a marcação nova ganha uma afirmação positiva ao lado. Asserção sobre o bloco de uma visão (`BlocoDeVisao.De`) sempre tem uma afirmação positiva sobre o mesmo bloco: um bloco vazio aceitaria qualquer `NotContain`.

O HTML desenhado pode ter espaço em branco e quebra de linha entre as marcas. As expressões regulares dos testes usam `\s*` entre marcas e `(?s)` quando atravessam linhas; quem escreve a marcação deixa colados os elementos que o teste procura colados (os botões de uma célula de ações, por exemplo: o espaço entre eles é desenhado).

A fonte de cada tela é o quadro do canvas. Antes de escrever a marcação de uma tela, releia os três arquivos dela na cópia local (caminho em "Estado de partida"). Se um quadro diferir do que este plano descreve, pare e relate.

## Estado de partida

- Worktree `.claude/worktrees/feat+fase-9c-administracao`, branch `worktree-feat+fase-9c-administracao`, na `main` (`c83bbdb`), que já tem as fases 9A e 9B.
- Canvas: https://claude.ai/artifact/HzHWv1pgVXAnYHuPEYQDFj. Cópia local, somente leitura: `C:\Users\Gabriel Paulino\AppData\Local\Temp\claude\c--GitHub-SIstemaControleAtividadesJuridicas\f5f0011f-66b5-41ed-8db8-f7d789323524\scratchpad\artifact-files\8991caa9-7339-47d7-a01c-84892497662e\project\`. Quadros desta fase: `Areas`, `Perfis`, `Usuarios`, `UsuarioFormulario`, `Produtores`, `Auditoria`, cada um com as variantes `Tablet` e `Mobile` (`.dc.html`). Por analogia (busca com "Escolher"): `NovoEvento.dc.html`.
- Design system: https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS, versão `1791672723-8f5a` (a que a 9B publicou), com as três regras pendentes da 9B ainda por publicar.
- Web, depois da 9B: `CabecalhoDePagina`, `Aviso` (com ações e foco ao aparecer), `EstadoDaLista`, `Paginacao`, `MenuDeAcoes`, `RotuloDeCampo`, `Icone`, `Contagem`, `FusoDoEscritorio.FormatarData`; as telas desta fase ainda com marcação antiga vestida pelo `app.css`.
- Testes: `AplicacaoDeTeste` sobe a aplicação inteira contra um banco novo por teste (admin Diana Diretora, `diana@x.com`; `CriarPedroAsync` cria Pedro Produção; `CriarCarlaAsync` cria a cliente Maria Silva e a usuária de cliente Carla Cliente). `TelaDeTeste` desenha um componente fora da página; `DespachanteCombinado` responde o que o banco não produz por encomenda; `ComBarraSuperior<T>` desenha um componente com o encaixe da barra superior; `BlocoDeVisao.De` recorta os blocos de uma visão.
- Linha de base: a suíte inteira passou em `6d3db0a` (o código da `main` de hoje): unidade 863, integração 627.
- `git` só pela ferramenta PowerShell. Antes de cada commit, `git branch --show-current`.
- A máquina tem caído ao começar build ou teste: build sempre `dotnet build -m:1`; teste sempre `--no-build` depois de um build feito; um comando por vez. A suíte inteira roda em lotes (`suite-em-lotes.ps1`, em `C:\Users\Gabriel Paulino\.claude\projects\c--GitHub-SIstemaControleAtividadesJuridicas\conferencia-de-telas\`).

## Global Constraints

- TFM único `net10.0`. `Nullable` e `TreatWarningsAsErrors=true`. Nenhum pacote novo.
- Código, nomes e textos em português, com acentuação correta. Arquivos em UTF-8 sem BOM, com o fim de linha que o arquivo já tem.
- A camada Web só chega aos dados pelo `IDespachante` (`WebSoPeloDespachanteTests`). Nenhuma consulta nova no backend nesta fase.
- `wwwroot/css/tokens.css` e `wwwroot/css/componentes.css` não são editados à mão: mudam no artifact do design system e são copiados de lá.
- Tela migrada não tem atributo `style`, nem `<style>`, nem arquivo `.razor.css`, nem classe que não comece por `aj-`, nem `<button>`, `<table>`, `<input>`, `<select>` ou `<textarea>` sem `class`. Nenhum CSS novo no `app.css`.
- As classes de visibilidade (`aj-so-desktop`, `aj-so-tablet`, `aj-so-mobile`, `aj-desktop-tablet`, `aj-tablet-mobile`) vão num invólucro próprio (`div` ou `span`) cuja classe é **só** a de visibilidade, nunca no elemento do componente.
- Visões: mobile até 760px, tablet de 761px a 1199px, desktop a partir de 1200px.
- Todo texto que um teste procura no HTML é escrito por **uma única** expressão Razor ou por marcação fixa.
- Rotas existentes não mudam. Rotas novas: `/usuarios/novo` e `/usuarios/{id:guid}/editar`; Produtores lê `produtor` no endereço. `RotasDoCliente` não muda (rota nova nasce fechada ao portal).
- Botão só de ícone leva `aria-label` com o verbo e o objeto ("Ações de Cível", "Mais filtros").
- Produção e qualquer perfil sem Diretoria, numa tela da administração: o cabeçalho e o aviso de erro "Acesso negado.", sem dado nenhum e sem pedir a lista (a tela confere o perfil antes de consultar).
- Textos exatos, dos quadros:
  - Áreas: `Áreas de atuação`; subtítulo `{n} áreas · {m} inativas` (`1 área`, `1 inativa`; sem a parte das inativas quando m é 0); colunas `Nome`, `Situação` e `Ações` (só para leitor de tela); `Ativa`, `Inativa`; `Renomear`, `Inativar`, `Ativar`; painel `Nova área` (ao renomear, `Renomear área`), rótulo `Nome`, botão `Salvar`; no tablet e no mobile, painel `Áreas` e botão `Ações de {nome}`
  - Perfis: `Perfis`; subtítulo `{n} perfis · {m} inativos` (`1 perfil`, `1 inativo`); colunas `Nome`, `Tipo`, `Usuários ativos`, `Situação`, `Ações`; `Ativo`, `Inativo`; painel `Novo perfil` (ao renomear, `Renomear perfil`), rótulos `Nome` e `Tipo`, opções `Produção` e `Diretoria`, a ajuda `O tipo não pode ser mudado depois. Um perfil novo passa a acessar todos os Documentos Gerais já existentes; um perfil de Diretoria acessa tudo, inclusive os Contratos de Honorários.`, botões `Cancelar` e `Salvar`; no tablet e no mobile, painel `Perfis` com a linha `{tipo} · {n} usuários ativos · {situação}` (`1 usuário ativo`)
  - Usuários: `Usuários`; subtítulo `{n} usuários · {m} inativos` (`1 usuário`, `1 inativo`); ação `Novo usuário`; colunas `Nome`, `E-mail`, `Perfil`, `Cliente`, `OAB`, `Último acesso`, `Situação`, `Ações`; `—` nas células sem valor; `Editar`; confirmação com `aria-label="Confirmar inativação"`, título `Confirmar a inativação de {nome}?`, o texto das pendências e os botões `Cancelar` e `Confirmar inativação`, nesta ordem; no cartão, `Último acesso {dd/MM/aaaa HH:mm}` ou `Sem acesso registrado`
  - Formulário de usuário: `Novo usuário` (na edição, `Editar usuário`); subtítulo `Escolha o perfil. Para um usuário de cliente, indique o cliente.`; voltar `Usuários` (no mobile, `Voltar para Usuários`); painel `Usuário`; rótulos `Nome`, `E-mail`, `Perfil`, `Número da OAB` com `(opcional)` em `small`, `Senha` (na edição, `Nova senha` com `(deixe em branco para manter)` em `small`), `Cliente`; `Trocar`; ajuda `O usuário entra no portal e vê só o que é deste cliente.`; ações `Cancelar` e `Salvar usuário` (no mobile, `Salvar`)
  - Produtores: `Produtores`; subtítulo `{n} produtores` (`1 produtor`); colunas `Nome`, `Perfil`, `OAB`, `Situação`, `Ações`; `Em acompanhamento`; `Acompanhar`; painel `Produtividade — {nome}`; campos `Produtor` e `Mês`; blocos `Complexidade 1`, `Complexidade 2`, `Complexidade 3`, `Total`; grupo `Complexidade {n} ({x})`; colunas `Título`, `Cliente`, `Concluída em`, `Ações`; `Abrir`; no tablet e no mobile, a linha `{cliente} · concluída em {dd/MM/aaaa HH:mm}`
  - Auditoria: `Auditoria`; subtítulo `{n} registros no período` (`1 registro`); controle `O que consultar` com `Alterações e eventos` e `Consultas`; filtros `De`, `Até`, `Usuário` (`Todos`), `Operação` (`Todas`, `Criação`, `Alteração`, `Exclusão`, `Evento`), `Entidade` (`Usuario, Perfil…`), `Registro` (`Identificador do registro`), `Gravação` (`Identificador de correlação`), botão `Filtrar`; colunas `Quando`, `Quem`, `Operação`, `Sobre`, `Dados`, `IP`, `Correlação`; títulos dos botões `Ver tudo deste registro` e `Ver o que foi gravado junto`; no tablet, botão `Usuário, operação`; no mobile, botão de ícone `Mais filtros`; nos cartões, `{operação} · {entidade}`, `{quem} · {quando} · IP {ip}` (sem o IP quando não houver), `Registro {id}…`, `Gravação {id}…`, e `Usuário não identificado` no lugar de `—`; paginação `Página {n} de {m} · {x} registros`
  - Sem quadro, mantidos: `Carregando…`; `Acesso negado.`; `Informe o nome.`; `Área salva.`, `Área inativada.`, `Área ativada.`; `Perfil salvo.`, `Perfil inativado.`, `Perfil ativado.` e `{mensagem}, mas não foi possível atualizar a lista.`; `Usuário salvo.`, `Usuário inativado.`, `Usuário ativado.`; `Informe o e-mail.`, `Selecione o perfil.`, `Selecione…`, `Informe o cliente do usuário.`, `O perfil atual está inativo. Escolha um perfil ativo para poder reativar o usuário.`, `Nenhum cliente encontrado.`, `Mostrando os primeiros resultados. Refine a busca para ver outros.`; `{cliente} (inativo — sem acesso ao portal)`; `{perfil} ({tipo} — inativo)`; `Nenhum produtor.`, `Nenhuma atividade.`, `Tentar de novo`, `Produtor não encontrado.` (do caso de uso); `Identificador inválido.`; na aba Consultas, `Recurso` (`Usuario, RegistroAuditoria…`), `Ação` (`Todas`, `Listou`, `Visualizou`, `Baixou`) e as colunas `Quando`, `Quem`, `Ação`, `Recurso`, `Detalhe`, `IP`
  - Sem quadro, novos por analogia: `Nenhuma área.`, `Nenhum perfil.`, `Nenhum usuário.`, `Nenhum registro.`; `Cancelar` no painel de área ao renomear; `Usuário não encontrado.`; painel `Situação` da edição (tablet e mobile) com `Ativo` ou `Inativo` e o botão `Inativar usuário` ou `Ativar usuário`; busca de cliente com o rótulo para leitor de tela `Buscar cliente` e o texto de exemplo `Buscar cliente: nome, CPF ou CNPJ`, resultados com `Escolher`; seletor de produtor com a primeira opção `Escolha o produtor`; `{nome} (inativo)` no seletor de produtor; botão `Usuário, ação` e painel de filtros com `Limpar` e `Filtrar`

## Review Focus

Condições que a spec implica e que mais provavelmente atingem quem usa a administração. Cada uma tem o seu teste na tarefa indicada.

1. **Endereço de edição com usuário que não existe** (identificador de alguém apagado, colado de outro lugar ou digitado): a tela mostra "Usuário não encontrado." com o voltar, sem formulário e sem gravar nada. Teste na Tarefa 5.
2. **Endereço de Produtores com um produtor que não vale** (texto que não é identificador, identificador de quem não é de Produção, de ninguém): a lista aparece normal, sem painel, e nenhuma consulta de produtividade é registrada. Teste na Tarefa 7.
3. **Usuário de cliente com o cliente inativo:** a marca "(inativo — sem acesso ao portal)" continua visível na tabela do desktop e no cartão do tablet e do mobile. Teste na Tarefa 6.
4. **Dados de auditoria fora da forma esperada** (texto que não é JSON, lista, objeto aninhado, valor nulo, data): o resumo mostra o que foi gravado, legível, sem quebrar a tela e sem perder informação. Teste na Tarefa 2.
5. **Falha ao consultar a lista** (áreas, perfis, usuários): o erro aparece no lugar da lista, com "Tentar de novo", e não vira "nenhum item"; o formulário de criação continua à mão. Testes nas Tarefas 3, 4 e 6.

Conferências no navegador, na Tarefa 9: nome e e-mail longos nos cartões e nas linhas a 390px; o painel de filtros da auditoria com teclado; o menu de ações de Áreas e Perfis com teclado e toque; o foco no campo "Nome" depois de "Renomear" no tablet e no mobile.

---

### Task 1: Design system — padrões da administração e cópia para o código

Feita pela sessão que coordena: publica no artifact do usuário.

**Pré-requisito:** a ferramenta Artifact abre o design system (https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS). Se responder "artifact not found", o Claude Code está conectado a outra conta: pare e peça ao usuário para entrar com a conta dona do artifact. Não escreva telas antes desta tarefa.

**Files:**
- Artifact do design system, sob `project/`: `components/bundle.css`, `design-system.json` (só `lastChange`); README e prévia novos em `components/LadoALado/`, `components/CampoComBotao/`, `components/Vinculo/`, `components/BlocosDeTotal/`, `components/RegistroDeAuditoria/`, `components/PainelDeFiltros/`; README (e prévia, onde houver estado novo) dos componentes que ganham modificador: Tabela, ListaEmPainel, Botao, Painel, Campo, Segmentado, CartaoDeAtividade, BarraDeAcoes; e os quatro README pendentes da 9B (Aviso, Painel, Tabela, MenuDeAcoes)
- Modify: `src/AtividadesJuridicas.Web/wwwroot/css/componentes.css`

**Interfaces:**
- Consumes: os 18 quadros desta fase; os arquivos pendentes da 9B.
- Produces: as classes `aj-lado-a-lado`, `aj-lado-a-lado--painel-largo`, `aj-lado-a-lado__lista`, `aj-lado-a-lado__painel`; `aj-tabela__linha--inativa`, `aj-tabela__linha--selecionada`, `aj-tabela__marca`, `aj-tabela__sem-quebra`, `aj-tabela-caixa--plana`; `aj-botao--seta`, `aj-identificador`, `aj-dados`; `aj-campo-com-botao`; `aj-painel--lista`, `aj-linha--inativa`, `aj-linhas`, `aj-linha--botao`; `aj-painel__cabecalho`, `aj-painel--formulario`, `aj-painel--filtros`, `aj-painel--registro`, `aj-registro__titulo`, `aj-registro__ids`, `aj-acoes--formulario`; `aj-grade-campos`, `aj-grade-campos__inteiro`; `aj-vinculo`, `aj-vinculo__escolhido`, `aj-vinculo__nome`, `aj-resultados`, `aj-resultados__item`, `aj-resultados__texto`, `aj-resultados__titulo`, `aj-resultados__meta`, `aj-resultados__acao`, `aj-campo-icone__controle`; `aj-blocos`, `aj-bloco`, `aj-bloco--destaque`; `aj-linha-filtros`, `aj-linha-filtros--empilha`, `aj-campo-filtro__controle`, `aj-campo-filtro--largo`, `aj-campo-filtro--data`, `aj-campo-filtro--texto`, `aj-campo-filtro--identificador`; `aj-segmentado--recuado`, `aj-segmentado__opcao`; `aj-cartao--inativo`, `aj-cartao__nota`, `aj-cartao__situacao`; a variável `--aj-pagina-padding-acoes`; e as três regras pendentes da 9B.

**Regras:**
- Nenhuma classe existente muda de valor. Medida de quadro que difere de uma classe existente vira modificador (spec, seção 3).
- Só tokens que já existem. A fonte de código fica literal (`ui-monospace, Consolas, monospace`), como no quadro.
- Regra que muda por visão segue o padrão do arquivo: a consulta de mídia e, depois, as mesmas regras sob `[data-visao="…"]`, para a prévia de cada visão funcionar dentro da página do design system.
- O bloco novo vai ao fim do `bundle.css`, depois das seções da 9B.

- [ ] **Step 1: Conferir as versões e ler o que vai mudar**

Ação `read` do artifact do design system: a versão tem de ser `1791672723-8f5a`. Se for outra, alguém editou: leia de novo `components/bundle.css` e refaça as emendas sobre ela. Liste os arquivos (`list` com `scope: "files"`). Leia também o canvas (`list` com `scope: "files"` em https://claude.ai/artifact/HzHWv1pgVXAnYHuPEYQDFj): se a versão não for `1791598628-21e1`, baixe os 18 quadros desta fase e compare com a cópia local; quadro que mudou é apontado ao usuário antes de seguir.

- [ ] **Step 2: Aplicar as pendências da 9B**

Comece pelos arquivos de `design-system-pendente-9b\` (o `bundle.css` e os quatro README já emendados sobre `1791672723-8f5a`). Se a versão do artifact for outra, refaça as três emendas sobre a versão nova (estão descritas na seção 15 da spec, "Pendente de publicação no design system").

- [ ] **Step 3: Acrescentar o bloco ao fim de `components/bundle.css`**

```css
/* ---------- Administração (fase 9C) ---------- */
/* Lado a lado (desktop): a lista e o painel dela na mesma linha. Em tablet e mobile o invólucro some e os blocos
   empilham na ordem da marcação. Os filhos do invólucro não encolhem na coluna que rola. */
.aj-lado-a-lado{display:contents}
.aj-lado-a-lado>*,.aj-lado-a-lado>*>*{flex-shrink:0}
@media (min-width:1200px){
  .aj-lado-a-lado{display:flex;align-items:flex-start;gap:var(--espaco-14)}
  .aj-lado-a-lado__lista{flex:1.8 1 0;min-width:0}
  .aj-lado-a-lado__painel{flex:1 1 0}
  .aj-lado-a-lado--painel-largo .aj-lado-a-lado__lista{flex:1 1 0}
  .aj-lado-a-lado--painel-largo .aj-lado-a-lado__painel{flex:1.5 1 0}
}
[data-visao="desktop"] .aj-lado-a-lado{display:flex;align-items:flex-start;gap:var(--espaco-14)}
[data-visao="desktop"] .aj-lado-a-lado__lista{flex:1.8 1 0;min-width:0}
[data-visao="desktop"] .aj-lado-a-lado__painel{flex:1 1 0}
[data-visao="desktop"] .aj-lado-a-lado--painel-largo .aj-lado-a-lado__lista{flex:1 1 0}
[data-visao="desktop"] .aj-lado-a-lado--painel-largo .aj-lado-a-lado__painel{flex:1.5 1 0}
[data-visao="tablet"] .aj-lado-a-lado,[data-visao="mobile"] .aj-lado-a-lado{display:contents}
[data-visao="tablet"] .aj-lado-a-lado__lista,[data-visao="tablet"] .aj-lado-a-lado__painel,
[data-visao="mobile"] .aj-lado-a-lado__lista,[data-visao="mobile"] .aj-lado-a-lado__painel{flex:0 0 auto}

/* Tabela: linha inativa (texto apagado, menos a coluna secundária), linha selecionada (o item em acompanhamento),
   marca de estado na coluna de ações, célula que não quebra e caixa sem sombra (tabela dentro de painel). */
.aj-tabela tr.aj-tabela__linha--inativa td:not(.aj-tabela__secundaria){color:var(--tinta-apagada)}
.aj-tabela tr.aj-tabela__linha--selecionada td,.aj-tabela tbody tr.aj-tabela__linha--selecionada:hover td{background:var(--selecao-fundo)}
.aj-tabela__marca{padding:0 var(--espaco-12);font-size:13px;font-weight:600;color:var(--sucesso)}
.aj-tabela__sem-quebra{white-space:nowrap}
.aj-tabela-caixa--plana{box-shadow:none}

/* Botão com seta (Acompanhar, Abrir): texto de 14px e a seta depois dele. */
.aj-botao--seta{padding:0 var(--espaco-12);gap:var(--espaco-4);font-size:14px}
.aj-botao--seta svg{width:16px;height:16px}
/* Identificador: o começo de um identificador longo, em fonte de código, num botão fantasma baixo. */
.aj-identificador{min-height:28px;padding:0 var(--espaco-8);font:500 12.5px/1.4 ui-monospace,Consolas,monospace}
/* Dados de um registro, em fonte de código. */
.aj-dados{font:500 12.5px/1.4 ui-monospace,Consolas,monospace;color:var(--tinta-secundaria);overflow-wrap:anywhere}

/* Campo com botão ao lado (Nova área): o botão alinha com a base do campo e tem a altura dele. */
.aj-campo-com-botao{display:flex;align-items:flex-end;gap:var(--espaco-8)}
.aj-campo-com-botao>.aj-rotulo{flex:1 1 auto}
.aj-campo-com-botao>.aj-botao{min-height:var(--aj-campo)}

/* Lista em painel com título: cada linha tem borda em cima, a primeira também. Linhas encostadas (sem o espaço
   do painel entre elas). Linha inteira clicável, que abre o item. Linha de item inativo. */
.aj-painel--lista .aj-linha{padding:var(--espaco-8) 0;border-top:1px solid var(--borda-suave)}
.aj-linhas{display:flex;flex-direction:column}
.aj-linhas>.aj-linha{padding:var(--espaco-8) 0;border-top:1px solid var(--borda-suave)}
.aj-linha--botao{width:100%;margin:0;border:0;background:transparent;color:var(--tinta);font:inherit;text-align:left;cursor:pointer}
.aj-linha--botao:focus-visible{outline:2px solid var(--verde);outline-offset:2px}
.aj-linha--inativa .aj-linha__titulo{color:var(--tinta-apagada)}

/* Painel: título de segundo nível, cabeçalho com um controle à direita, formulário largo, painel de filtros e
   cartão de registro de auditoria. */
.aj-painel h3{margin:var(--espaco-6) 0 0;font:700 14.5px/1.4 var(--font-sans)}
.aj-painel__cabecalho{display:flex;align-items:center;justify-content:space-between;gap:var(--espaco-12)}
.aj-painel--formulario{max-width:760px;box-shadow:var(--aj-painel-estreito-sombra)}
.aj-acoes--formulario{max-width:760px}
.aj-painel--filtros{gap:var(--espaco-8)}
.aj-painel--filtros .aj-campo-filtro{min-width:0}
.aj-painel--registro{gap:var(--espaco-6)}
.aj-painel--registro .aj-ajuda{font-variant-numeric:tabular-nums}
.aj-registro__titulo{font-size:15.5px;font-weight:600;line-height:1.3;overflow-wrap:anywhere}
.aj-registro__ids{display:flex;flex-wrap:wrap;gap:var(--espaco-4);padding-top:var(--espaco-6);border-top:1px solid var(--borda-suave)}
.aj-registro__ids .aj-identificador{min-height:36px}

/* Formulário em grade: dois campos por linha (um no mobile); o bloco que ocupa a linha toda. */
.aj-grade-campos{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:var(--espaco-10) var(--espaco-12)}
.aj-grade-campos__inteiro{grid-column:1/-1}
@media (max-width:760px){.aj-grade-campos{grid-template-columns:minmax(0,1fr)}}
[data-visao="desktop"] .aj-grade-campos,[data-visao="tablet"] .aj-grade-campos{grid-template-columns:repeat(2,minmax(0,1fr))}
[data-visao="mobile"] .aj-grade-campos{grid-template-columns:minmax(0,1fr)}

/* Vínculo (o cliente de um usuário): o escolhido, com "Trocar", ou a busca e os resultados com "Escolher". */
.aj-vinculo{display:flex;flex-direction:column;gap:var(--espaco-8)}
.aj-vinculo__escolhido{display:flex;align-items:center;gap:var(--espaco-8);padding:var(--espaco-6) var(--espaco-6) var(--espaco-6) var(--espaco-12);border:1px solid var(--sucesso-borda);border-radius:var(--raio-12);background:var(--sucesso-fundo)}
.aj-vinculo__nome{flex:1 1 auto;min-width:0;font-weight:600;overflow-wrap:anywhere}
.aj-campo-icone__controle{min-width:0}
.aj-resultados{display:flex;flex-direction:column;gap:var(--espaco-2);margin:0;padding:var(--espaco-6);list-style:none;border:1px solid var(--borda);border-radius:var(--raio-14);background:var(--superficie);box-shadow:var(--sombra-flutuante)}
.aj-resultados__item{display:flex;align-items:center;justify-content:space-between;gap:var(--espaco-12);width:100%;min-height:var(--alvo-toque);padding:var(--espaco-8) var(--espaco-12);border:0;border-radius:var(--raio-10);background:transparent;color:var(--tinta);font:inherit;text-align:left;cursor:pointer}
.aj-resultados__item:hover{background:var(--superficie-recuada)}
.aj-resultados__item:focus-visible{outline:2px solid var(--verde);outline-offset:-2px}
.aj-resultados__texto{display:flex;flex-direction:column;gap:1px;min-width:0}
.aj-resultados__titulo{font-weight:600;overflow-wrap:anywhere}
.aj-resultados__meta{font-size:13px;color:var(--tinta-suave)}
.aj-resultados__acao{flex:0 0 auto;font-size:13.5px;font-weight:600;color:var(--verde)}

/* Blocos de total (produtividade): o nome e o número em destaque; o total, marcado. Quatro por linha, dois no mobile. */
.aj-blocos{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:var(--espaco-8)}
@media (max-width:760px){.aj-blocos{grid-template-columns:repeat(2,minmax(0,1fr))}}
[data-visao="desktop"] .aj-blocos,[data-visao="tablet"] .aj-blocos{grid-template-columns:repeat(4,minmax(0,1fr))}
[data-visao="mobile"] .aj-blocos{grid-template-columns:repeat(2,minmax(0,1fr))}
.aj-bloco{display:flex;flex-direction:column;gap:var(--espaco-2);padding:var(--espaco-10) var(--espaco-14);border:1px solid var(--borda);border-radius:var(--raio-12);background:var(--superficie-suave)}
.aj-bloco b{font:600 20px/1.2 var(--font-sans);font-variant-numeric:tabular-nums}
.aj-bloco--destaque{border-color:var(--selecao-borda);background:var(--selecao-fundo)}

/* Filtros: linha de campos fora da barra (tablet e mobile), controle dentro do campo e medidas de campo. */
.aj-linha-filtros{display:flex;gap:var(--espaco-8)}
.aj-linha-filtros .aj-campo-filtro{background:var(--superficie)}
.aj-linha-filtros .aj-botao{flex:0 0 auto;min-height:var(--campo-filtro)}
@media (max-width:760px){.aj-linha-filtros--empilha{flex-direction:column;gap:var(--aj-pagina-gap)}}
[data-visao="mobile"] .aj-linha-filtros--empilha{flex-direction:column;gap:var(--aj-pagina-gap)}
.aj-campo-filtro__controle{min-width:0}
.aj-campo-filtro--largo{flex:1 1 auto}
.aj-campo-filtro--data{min-width:140px}
.aj-linha-filtros .aj-campo-filtro--data{flex:1 1 0}
.aj-campo-filtro--texto{flex:1 1 140px}
.aj-campo-filtro--identificador{flex:1 1 170px}
.aj-barra-filtros>.aj-botao{min-height:var(--campo-desktop)}

/* Controle segmentado sobre fundo recuado (no cabeçalho) e na segunda linha da barra superior, com as opções
   dividindo a largura. */
.aj-segmentado--recuado{background:var(--superficie-recuada)}
.aj-segmentado__opcao{white-space:nowrap}
.aj-barra-superior>.aj-segmentado{display:grid;grid-auto-flow:column;grid-auto-columns:minmax(0,1fr)}

/* Cartão de usuário: inativo apagado; rodapé com o último acesso e a situação. */
.aj-cartao--inativo .aj-cartao__titulo,.aj-cartao--inativo .aj-cartao__situacao{color:var(--tinta-apagada)}
.aj-cartao__nota{min-width:0;font-size:13px;color:var(--tinta-secundaria);font-variant-numeric:tabular-nums}
.aj-cartao__situacao{flex:0 0 auto;font-size:13px;font-weight:600;color:var(--tinta)}

/* Formulário com barra de ações fixa no tablet (e no mobile): o conteúdo começa mais perto do topo e deixa espaço
   para a barra. Vale quando a barra fixa está à vista no tablet. */
:root{--aj-pagina-padding-acoes:18px var(--espaco-24) var(--espaco-28)}
@media (max-width:1199px){:root{--aj-pagina-padding-acoes:var(--espaco-16) var(--espaco-24) 110px}}
@media (max-width:760px){:root{--aj-pagina-padding-acoes:var(--espaco-16) var(--espaco-16) 112px}}
[data-visao="desktop"]{--aj-pagina-padding-acoes:18px var(--espaco-24) var(--espaco-28)}
[data-visao="tablet"]{--aj-pagina-padding-acoes:var(--espaco-16) var(--espaco-24) 110px}
[data-visao="mobile"]{--aj-pagina-padding-acoes:var(--espaco-16) var(--espaco-16) 112px}
.aj-app:has(:is(.aj-so-tablet,.aj-tablet-mobile)>.aj-acoes--fixa) .aj-app__conteudo{padding:var(--aj-pagina-padding-acoes)}
```

Confira por roteiro: chaves abertas e fechadas em número igual; toda `var(--…)` do bloco existe em `tokens.css` ou no próprio `bundle.css`; nenhuma regra anterior com especificidade maior anula `.aj-botao--seta`, `.aj-identificador` ou `.aj-painel--lista .aj-linha` (as regras `.aj-botao--fantasma` e `.aj-linha+.aj-linha` vêm antes e têm especificidade igual ou menor).

- [ ] **Step 4: README e prévia**

Componentes novos, cada um com `README.md` (primeira frase é o resumo; depois o que cada classe faz, as regras de uso e o que quem usa fornece) e `preview.html` (linha 1 `<!-- @dsCard group="…" height=… -->`, como as prévias da 9B):

| Pasta | Grupo | O que a prévia mostra | Regras de uso que o README registra |
|---|---|---|---|
| `LadoALado` | Estrutura | A tabela de áreas e o painel "Nova área" lado a lado (desktop) | Só no desktop; no tablet e no mobile os blocos empilham na ordem da marcação, então a ordem da marcação é a do tablet; `--painel-largo` quando o painel é o assunto principal (Produtores) |
| `CampoComBotao` | Campos | "Nome" com "Salvar" ao lado | Para formulário de um campo só; o botão tem a altura do campo |
| `Vinculo` | Campos | O cliente escolhido com "Trocar"; a busca com dois resultados e "Escolher" | A busca responde ao Enter, sem botão; resultado é um botão inteiro; o escolhido fica em fundo de sucesso |
| `BlocosDeTotal` | Conteúdo | Complexidade 1, 2, 3 e Total (marcado) | Quatro por linha, dois no mobile; o último é o total |
| `RegistroDeAuditoria` | Conteúdo | O cartão de um registro com título, meta, dados e os dois identificadores | Para tablet e mobile, no lugar da tabela da auditoria; identificador truncado com reticências; o botão diz o que faz no `title` |
| `PainelDeFiltros` | Ações | A linha com "De", "Até" e "Usuário, operação"; o painel aberto com os campos e "Limpar" e "Filtrar" | Tablet e mobile; o painel abre logo abaixo da linha, com os campos da barra do desktop que não cabem nela, em uma coluna |

Componentes que ganham modificador (README emendado; prévia emendada onde já existe):

- Tabela: `aj-tabela__linha--inativa`, `aj-tabela__linha--selecionada`, `aj-tabela__marca` ("Em acompanhamento"), `aj-tabela__sem-quebra`, `aj-tabela-caixa--plana` (tabela dentro de painel).
- Lista em painel: `aj-painel--lista` (linhas sob um título), `aj-linhas` (linhas encostadas), `aj-linha--botao` (linha que abre o item), `aj-linha--inativa`.
- Botão: `aj-botao--seta` (texto com seta depois, para "Acompanhar" e "Abrir"), `aj-identificador`.
- Painel: `h3`, `aj-painel__cabecalho`, `aj-painel--formulario` (com `aj-acoes--formulario`), `aj-painel--filtros`, `aj-painel--registro`.
- Campo: `aj-grade-campos` (com `__inteiro`), `aj-campo-icone__controle`, `aj-campo-filtro__controle` e os modificadores de largura do campo de filtro, `aj-linha-filtros` (com `--empilha`), `aj-dados`.
- Segmentado: `aj-segmentado--recuado`, `aj-segmentado__opcao` (a classe de cada botão: botão sem classe não entra em tela migrada) e o segmentado na segunda linha da barra superior.
- Cartão de atividade: o cartão de usuário (`aj-cartao--inativo`, `aj-cartao__nota`, `aj-cartao__situacao`).
- Barra de ações: o respiro do conteúdo quando a barra fixa aparece no tablet (`--aj-pagina-padding-acoes`).

`design-system.json`: só `lastChange` (`by` o usuário, `via` "Claude Code", `at` agora, `note` "Fase 9C: administração — lado a lado, vínculo, filtros em painel, registro de auditoria, blocos de total; e as pendências da 9B.").

- [ ] **Step 5: Publicar**

Uma chamada de publicação com os arquivos alterados e, por último na lista, o índice relido logo antes. Use o caminho longo do scratchpad em `root` e `file_path` (o caminho curto `GABRIE~1` é recusado). Anote a versão publicada no ledger. Abra a página do design system e confira as prévias novas e as emendadas.

- [ ] **Step 6: Copiar para o código**

`wwwroot/css/componentes.css` = as quatro linhas de cabeçalho que o arquivo já tem (a segunda diz "Cópia de components/bundle.css (…); altere lá primeiro.") + o `bundle.css` publicado, da linha "Visões:" em diante, com fim de linha CRLF. Confira por roteiro que o corpo é idêntico. `tokens.css` não muda nesta fase.

- [ ] **Step 7: Build e commit**

Run: `dotnet build -m:1`
Expected: 0 avisos.

```
git add src/AtividadesJuridicas.Web/wwwroot/css/componentes.css
git commit -m "feat(web): design system com lado a lado, vínculo, filtros em painel, registro de auditoria e blocos de total, e as pendências da 9B"
```

---

### Task 2: Peças compartilhadas da administração

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Layout/CabecalhoDeDetalhe.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/CabecalhoDePagina.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/MenuDeAcoes.razor`
- Modify: `src/AtividadesJuridicas.Web/Infra/Contagem.cs`
- Create: `src/AtividadesJuridicas.Web/Infra/IdentificadorCurto.cs`
- Create: `src/AtividadesJuridicas.Web/Infra/ResumoDeAuditoria.cs`
- Create: `src/AtividadesJuridicas.Web/Infra/Recados.cs`
- Modify: `src/AtividadesJuridicas.Web/Program.cs` (registro de `Recados`)
- Modify: `tests/AtividadesJuridicas.IntegrationTests/TelaDeTeste.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/ComponentesDoDesignSystemTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/TelasPurasTests.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: `CabecalhoDePagina` (com `VoltarPara`, `RotuloDoVoltar`, `Folgado`), `Icone` (`voltar`), `FusoDoEscritorio.Formatar`, `TipoDeAviso`.
- Produces:
  - `<CabecalhoDePagina … Complemento? LinhaDoMobile?>`: `Complemento` (`RenderFragment?`) vai dentro do `aj-cabecalho`, depois do `aj-cabecalho__inicio` e antes da ação; `LinhaDoMobile` (`RenderFragment?`) vai dentro do `header.aj-barra-superior` de título, depois do `aj-barra-superior__linha` (não vale na barra de voltar).
  - `<CabecalhoDeDetalhe Titulo Subtitulo? VoltarPara Origem />`: emite, no desktop e no tablet, `<div><a href="{VoltarPara}" class="aj-voltar">{ícone voltar}{Origem}</a></div>` acima do cabeçalho; e o `CabecalhoDePagina` com `VoltarPara`, `RotuloDoVoltar="Voltar para {Origem}"` e `Folgado` quando há subtítulo.
  - `Contagem.ComInativos(int total, int inativos, string singular, string plural, string inativoNoSingular, string inativoNoPlural) : string` — "5 áreas · 1 inativa"; "5 áreas" quando não há inativos.
  - `IdentificadorCurto.De(Guid id) : string` — os oito primeiros caracteres e "…": "0199a3f2…".
  - `ResumoDeAuditoria.De(string? dados, FusoDoEscritorio fuso) : string` — o JSON dos dados em texto de ler; "—" sem dados.
  - `Recados` (serviço de escopo): `Deixar(string texto, TipoDeAviso tipo = TipoDeAviso.Sucesso)`, `Retirar() : Recado?`; `sealed record Recado(string Texto, TipoDeAviso Tipo)`.
  - `TelaDeTeste.RenderizarAsync(app, usuarioId, parametros, Recados? recados = null)` e `TelaDeTeste.RenderizarComAsync(despachante, parametros, Recados? recados = null)`: a primeira chama a segunda; as duas registram o `Recados` informado (ou um novo).

**Regras:**
- `ResumoDeAuditoria`: os dados vêm em JSON. Objeto: cada campo vira `"{nome}: {valor}"`, unidos por `" · "`, na ordem gravada. Campo cujo valor é um objeto com exatamente as chaves `de` e `para` vira `"{nome}: {de} → {para}"`. Valor texto que é instante (`2026-10-08T21:11:00Z`, com ou sem `Z`, tratado como UTC) vira a data e a hora do escritório (`FusoDoEscritorio.Formatar`); texto que é só data (`2090-06-15`) vira `15/06/2090`; outro texto fica como está. `null` vira `null`; `true` e `false` ficam assim; número, lista e objeto (que não seja de/para) ficam como foram gravados. Objeto vazio, texto vazio ou nulo: `—`. Texto que não é JSON, ou JSON que não é objeto: o texto como veio.
- `Recados` guarda um recado só: o último deixado. `Retirar` devolve e esquece.
- `MenuDeAcoes`: fechar pelo fundo (tocar fora) devolve o foco ao botão do menu, como o Esc. Fechar escolhendo um item continua sem mexer no foco (o que a ação abre decide).

- [ ] **Step 1: Testes (devem falhar)**

Em `TelasPurasTests`, na classe `ContagemTests` existente:

```csharp
    [Theory]
    [InlineData(5, 1, "5 áreas · 1 inativa")]
    [InlineData(5, 0, "5 áreas")]
    [InlineData(1, 1, "1 área · 1 inativa")]
    [InlineData(7, 2, "7 áreas · 2 inativas")]
    public void Contagem_com_inativos_so_fala_deles_quando_ha(int total, int inativos, string esperado)
    {
        Contagem.ComInativos(total, inativos, "área", "áreas", "inativa", "inativas").Should().Be(esperado);
    }
```

E classes novas no mesmo arquivo:

```csharp
public class IdentificadorCurtoTests
{
    [Fact]
    public void Identificador_curto_traz_os_oito_primeiros_caracteres_e_reticencias()
    {
        IdentificadorCurto.De(Guid.Parse("0199a3f2-7c1e-7000-8000-000000000001")).Should().Be("0199a3f2…");
    }
}

public class RecadosTests
{
    [Fact]
    public void Recado_e_entregue_uma_vez_so_e_vale_o_ultimo()
    {
        var recados = new Recados();
        recados.Retirar().Should().BeNull();

        recados.Deixar("Usuário salvo.");
        recados.Deixar("Usuário inativado.", TipoDeAviso.Sucesso);

        recados.Retirar().Should().Be(new Recado("Usuário inativado.", TipoDeAviso.Sucesso));
        recados.Retirar().Should().BeNull();
    }
}

public class ResumoDeAuditoriaTests
{
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");

    [Theory]
    [InlineData("{\"Status\":{\"de\":\"Feita\",\"para\":\"Concluida\"}}", "Status: Feita → Concluida")]
    [InlineData("{\"De\":\"Feita\",\"Para\":\"Concluida\"}", "De: Feita · Para: Concluida")]
    [InlineData("{\"ConcluidoEm\":{\"de\":null,\"para\":\"2026-10-08T21:11:00Z\"}}", "ConcluidoEm: null → 08/10/2026 18:11")]
    [InlineData("{\"email\":\"desconhecido@correio.example\"}", "email: desconhecido@correio.example")]
    [InlineData("{\"DataEntrega\":\"2090-06-15\",\"Complexidade\":2,\"Ativo\":true}", "DataEntrega: 15/06/2090 · Complexidade: 2 · Ativo: true")]
    [InlineData("{\"Partes\":[1,2],\"Endereco\":{\"Cidade\":\"Maceió\"}}", "Partes: [1,2] · Endereco: {\"Cidade\":\"Maceió\"}")]
    [InlineData("{\"Quando\":\"2026-10-08T21:11:00\"}", "Quando: 08/10/2026 18:11")]
    public void Resumo_le_os_campos_gravados(string dados, string esperado)
    {
        ResumoDeAuditoria.De(dados, _fuso).Should().Be(esperado);
    }

    [Theory]
    [InlineData(null, "—")]
    [InlineData("", "—")]
    [InlineData("{}", "—")]
    [InlineData("texto solto", "texto solto")]
    [InlineData("[1,2]", "[1,2]")]
    [InlineData("{\"quebrado\":", "{\"quebrado\":")]
    public void Resumo_de_dados_fora_da_forma_mostra_o_que_veio(string? dados, string esperado)
    {
        ResumoDeAuditoria.De(dados, _fuso).Should().Be(esperado);
    }
}
```

O arquivo passa a precisar de `using AtividadesJuridicas.Web.Components.Layout;` (para `TipoDeAviso`), se ainda não tiver.

Em `ComponentesDoDesignSystemTests`:

```csharp
    [Fact]
    public async Task Cabecalho_leva_o_complemento_ao_lado_do_titulo_e_a_linha_do_mobile_na_barra()
    {
        RenderFragment complemento = c => c.AddMarkupContent(0, "<div class=\"aj-segmentado\">segmentado do cabeçalho</div>");
        RenderFragment linha = c => c.AddMarkupContent(0, "<div class=\"aj-segmentado\">segmentado da barra</div>");

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDePagina>>(new()
        {
            ["Titulo"] = "Auditoria", ["Complemento"] = complemento, ["LinhaDoMobile"] = linha,
        }));

        var desktop = BlocoDeVisao.De(html, "aj-desktop-tablet");
        desktop.Should().MatchRegex("(?s)<header class=\"aj-cabecalho\">.*segmentado do cabeçalho.*</header>");
        desktop.Should().NotContain("segmentado da barra");
        html.Should().MatchRegex(
            "(?s)<header class=\"aj-barra-superior\">\\s*<div class=\"aj-barra-superior__linha\">.*?</div>\\s*<div class=\"aj-segmentado\">segmentado da barra</div>\\s*</header>");
    }

    [Fact]
    public async Task Cabecalho_sem_complemento_nem_linha_continua_como_era()
    {
        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDePagina>>(
            new() { ["Titulo"] = "Áreas de atuação" }));

        html.Should().MatchRegex("(?s)<header class=\"aj-barra-superior\">\\s*<div class=\"aj-barra-superior__linha\">.*?</div>\\s*</header>")
            .And.Contain("<h1 class=\"aj-titulo\">Áreas de atuação</h1>");
        html.Should().NotContain("aj-segmentado");
    }

    [Fact]
    public async Task Cabecalho_de_detalhe_traz_o_voltar_no_desktop_e_a_barra_de_voltar_no_mobile()
    {
        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDeDetalhe>>(new()
        {
            ["Titulo"] = "Novo usuário", ["Subtitulo"] = "Escolha o perfil.", ["VoltarPara"] = "/usuarios", ["Origem"] = "Usuários",
        }));

        var desktop = BlocoDeVisao.De(html, "aj-desktop-tablet");
        desktop.Should().MatchRegex("(?s)<div>\\s*<a href=\"/usuarios\" class=\"aj-voltar\">\\s*<svg[^>]*>.*?</svg>\\s*Usuários</a>\\s*</div>")
            .And.MatchRegex("<div class=\"aj-cabecalho__texto aj-cabecalho__texto--folgado\">\\s*<h1 class=\"aj-titulo\">Novo usuário</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Escolha o perfil.</p>");
        html.Should().MatchRegex(
            "<header class=\"aj-barra-superior aj-barra-superior--voltar\">\\s*<a href=\"/usuarios\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Voltar para Usuários\">");
        // O voltar do desktop não vai para a barra do mobile, e a barra não repete o subtítulo.
        Regex.Matches(html, "class=\"aj-voltar\"").Should().ContainSingle();
    }
```

E, no mesmo arquivo, a prova de que o desenho estático aceita um `EditForm` com `FormName` (as telas desta fase são testadas assim; o `EditForm` desenha o `AntiforgeryToken`, que pede um serviço que o `TelaDeTeste` ainda não registra):

```csharp
    [Fact]
    public async Task Desenho_estatico_aceita_formulario_com_nome()
    {
        var html = await TelaDeTeste.RenderizarComAsync<ComFormularioNomeado>(new DespachanteCombinado(), new());

        html.Should().Contain("<form");
    }

    private sealed class ComFormularioNomeado : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder construtor)
        {
            construtor.OpenComponent<EditForm>(0);
            construtor.AddComponentParameter(1, nameof(EditForm.Model), new object());
            construtor.AddComponentParameter(2, nameof(EditForm.FormName), "teste");
            construtor.CloseComponent();
        }
    }
```

O arquivo passa a precisar de `using Microsoft.AspNetCore.Components.Forms;` e `using Microsoft.AspNetCore.Components.Rendering;`, se ainda não tiver.

Run: `dotnet build -m:1`
Expected: não compila (`Contagem.ComInativos`, `IdentificadorCurto`, `Recados`, `ResumoDeAuditoria` e `CabecalhoDeDetalhe` não existem). É o RED. Depois dos Steps 2 a 5, o teste do formulário nomeado é o RED do Step 6: rode-o antes de mexer no `TelaDeTeste` e registre a mensagem de erro.

- [ ] **Step 2: `Contagem`, `IdentificadorCurto`, `Recados`**

```csharp
    /// <summary>"5 áreas · 1 inativa": a quantidade e, quando houver, quantos estão inativos.</summary>
    public static string ComInativos(
        int total, int inativos, string singular, string plural, string inativoNoSingular, string inativoNoPlural) =>
        inativos > 0
            ? $"{De(total, singular, plural)} · {De(inativos, inativoNoSingular, inativoNoPlural)}"
            : De(total, singular, plural);
```

```csharp
namespace AtividadesJuridicas.Web.Infra;

/// <summary>O começo de um identificador longo, para caber numa célula: "0199a3f2…".</summary>
public static class IdentificadorCurto
{
    public static string De(Guid id) => $"{id.ToString()[..8]}…";
}
```

```csharp
using AtividadesJuridicas.Web.Components.Layout;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>
/// Aviso que atravessa uma navegação: a tela de origem deixa ("Usuário salvo."), a de destino mostra uma vez.
/// Vive no escopo do circuito, então só vale para a navegação dentro da mesma conexão.
/// </summary>
public sealed class Recados
{
    private Recado? _pendente;

    public void Deixar(string texto, TipoDeAviso tipo = TipoDeAviso.Sucesso) => _pendente = new Recado(texto, tipo);

    /// <summary>O recado deixado, uma vez só; nulo quando não há.</summary>
    public Recado? Retirar()
    {
        var recado = _pendente;
        _pendente = null;
        return recado;
    }
}

public sealed record Recado(string Texto, TipoDeAviso Tipo);
```

Em `Program.cs`, junto do `OrigemDaConexao`: `builder.Services.AddScoped<Recados>();`.

- [ ] **Step 3: `ResumoDeAuditoria`**

```csharp
using System.Globalization;
using System.Text.Json;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>
/// Os dados de um registro de auditoria em texto de ler: "Status: Feita → Concluida · Nome: Ana". Os dados são
/// gravados em JSON (um objeto com os campos; numa alteração, cada campo traz "de" e "para"). Nada é descartado: o
/// que não tem essa forma aparece como foi gravado.
/// </summary>
public static class ResumoDeAuditoria
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public static string De(string? dados, FusoDoEscritorio fuso)
    {
        ArgumentNullException.ThrowIfNull(fuso);
        if (string.IsNullOrWhiteSpace(dados)) return "—";

        try
        {
            using var documento = JsonDocument.Parse(dados);
            if (documento.RootElement.ValueKind != JsonValueKind.Object) return dados;

            var partes = documento.RootElement.EnumerateObject()
                .Select(campo => $"{campo.Name}: {Valor(campo.Value, fuso)}")
                .ToList();
            return partes.Count == 0 ? "—" : string.Join(" · ", partes);
        }
        catch (JsonException)
        {
            return dados;
        }
    }

    private static string Valor(JsonElement valor, FusoDoEscritorio fuso) =>
        valor.ValueKind == JsonValueKind.Object && SoDeEPara(valor)
            ? $"{Simples(valor.GetProperty("de"), fuso)} → {Simples(valor.GetProperty("para"), fuso)}"
            : Simples(valor, fuso);

    private static bool SoDeEPara(JsonElement objeto)
    {
        var nomes = objeto.EnumerateObject().Select(p => p.Name).ToList();
        return nomes.Count == 2 && nomes.Contains("de") && nomes.Contains("para");
    }

    private static string Simples(JsonElement valor, FusoDoEscritorio fuso) => valor.ValueKind switch
    {
        JsonValueKind.Null => "null",
        JsonValueKind.String => Texto(valor.GetString()!, fuso),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => valor.GetRawText(),   // número, lista e objeto ficam como foram gravados
    };

    private static string Texto(string texto, FusoDoEscritorio fuso)
    {
        // Instante gravado em UTC: a data e a hora do escritório.
        if (texto.Length > 10 && texto[10] == 'T' &&
            DateTimeOffset.TryParse(texto, Cultura, DateTimeStyles.AssumeUniversal, out var instante))
            return fuso.Formatar(instante.UtcDateTime);

        // Data sem hora.
        if (DateOnly.TryParseExact(texto, "yyyy-MM-dd", Cultura, DateTimeStyles.None, out var dia))
            return dia.ToString("dd/MM/yyyy", Cultura);

        return texto;
    }
}
```

- [ ] **Step 4: Os dois encaixes do `CabecalhoDePagina` e o `CabecalhoDeDetalhe`**

No `CabecalhoDePagina`, dentro do `header.aj-barra-superior` de título, depois do `div.aj-barra-superior__linha`: `@LinhaDoMobile`. Dentro do `header.aj-cabecalho`, depois do `div.aj-cabecalho__inicio` e antes do bloco da ação: `@Complemento`. Parâmetros:

```csharp
    /// <summary>O que fica ao lado do título no desktop e no tablet, no lugar ou antes da ação (um controle segmentado).</summary>
    [Parameter] public RenderFragment? Complemento { get; set; }

    /// <summary>A segunda linha da barra superior do mobile (busca, chips, controle segmentado).</summary>
    [Parameter] public RenderFragment? LinhaDoMobile { get; set; }
```

Atualize o comentário do topo do componente com os dois encaixes.

`CabecalhoDeDetalhe.razor`:

```razor
@* Cabeçalho de ficha e de formulário: no desktop e no tablet, o voltar acima do cabeçalho; no mobile, a barra de
   voltar, que esconde a barra inferior. Origem é o nome da tela de onde se veio ("Usuários"). *@
<div class="aj-desktop-tablet">
    <div><a href="@VoltarPara" class="aj-voltar"><Icone Nome="voltar" />@Origem</a></div>
</div>
<CabecalhoDePagina Titulo="@Titulo" Subtitulo="@Subtitulo" Folgado="@(Subtitulo is not null)"
                   VoltarPara="@VoltarPara" RotuloDoVoltar="@($"Voltar para {Origem}")" />

@code {
    [Parameter, EditorRequired] public string Titulo { get; set; } = string.Empty;

    /// <summary>A linha sob o título, no desktop e no tablet. A barra de voltar do mobile não tem subtítulo.</summary>
    [Parameter] public string? Subtitulo { get; set; }

    [Parameter, EditorRequired] public string VoltarPara { get; set; } = string.Empty;

    /// <summary>O nome da tela de destino do voltar: o texto do link e o rótulo do botão do mobile.</summary>
    [Parameter, EditorRequired] public string Origem { get; set; } = string.Empty;
}
```

Se o `CabecalhoDePagina` com `VoltarPara` já repetir o subtítulo na barra de voltar, não mude isso aqui: o teste afirma só o que este plano promete.

- [ ] **Step 5: Foco ao fechar o menu pelo fundo**

No `MenuDeAcoes`, o `@onclick` do `aj-menu-acoes__fundo` passa a `FecharPeloFundoAsync`, que fecha e chama `FocarAsync(_gatilho)` (com a mesma guarda de exceção que já existe). O `@onclick="Fechar"` da lista continua. Não há teste estático disso (o desenho estático não clica): fica para a Tarefa 9.

- [ ] **Step 6: `TelaDeTeste`**

```csharp
    public static Task<string> RenderizarAsync<TComponente>(
        AplicacaoDeTeste app, Guid usuarioId, Dictionary<string, object?> parametros, Recados? recados = null)
        where TComponente : IComponent =>
        RenderizarComAsync<TComponente>(new DespachanteDeTeste(app, usuarioId), parametros, recados);

    /// <summary>Desenha um componente com o despachante informado: sem aplicação, sem banco.</summary>
    public static async Task<string> RenderizarComAsync<TComponente>(
        IDespachante despachante, Dictionary<string, object?> parametros, Recados? recados = null)
        where TComponente : IComponent
    {
        // Só o que os componentes da aplicação injetam: o despachante, o fuso, os recados entre telas, o JavaScript
        // (que o InputFile injeta e o desenho estático nunca chama) e o estado de antifalsificação (que o EditForm
        // com nome pede para o campo oculto do token; aqui, sem token).
        await using var servicos = new ServiceCollection()
            .AddSingleton(despachante)
            .AddSingleton(new FusoDoEscritorio("America/Sao_Paulo"))
            .AddSingleton(recados ?? new Recados())
            .AddSingleton<IJSRuntime>(new SemJavaScript())
            .AddSingleton<AntiforgeryStateProvider>(new SemAntifalsificacao())
            .BuildServiceProvider();
        await using var renderizador = new HtmlRenderer(servicos, NullLoggerFactory.Instance);

        return await renderizador.Dispatcher.InvokeAsync(async () =>
            (await renderizador.RenderComponentAsync<TComponente>(ParameterView.FromDictionary(parametros))).ToHtmlString());
    }

    private sealed class SemAntifalsificacao : AntiforgeryStateProvider
    {
        public override AntiforgeryRequestToken? GetAntiforgeryToken() => null;
    }
```

(`AntiforgeryStateProvider` e `AntiforgeryRequestToken` estão em `Microsoft.AspNetCore.Components.Forms`. Se a assinatura da versão instalada for outra, ajuste o mínimo e registre.) Atualize o resumo da classe. `RenderizarSemDadosAsync` fica como está. Nenhum teste existente muda.

- [ ] **Step 7: Trava e testes**

Acrescente a `Migradas`: `Components/Layout/CabecalhoDeDetalhe.razor` (em ordem alfabética).

Run: `dotnet build -m:1`
Expected: 0 avisos.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ComponentesDoDesignSystemTests|FullyQualifiedName~ContagemTests|FullyQualifiedName~IdentificadorCurtoTests|FullyQualifiedName~RecadosTests|FullyQualifiedName~ResumoDeAuditoriaTests|FullyQualifiedName~DocumentosDoPortalWebTests|FullyQualifiedName~DocumentosDaFichaTests|FullyQualifiedName~ProdutoresWebTests|FullyQualifiedName~MinhaSenhaWebTests"`
Expected: todos passando.

- [ ] **Step 8: Commit**

```
git add src tests
git commit -m "feat(web): cabeçalho de detalhe, encaixes do cabeçalho de página, recado entre telas, resumo e identificador da auditoria"
```

---

### Task 3: Áreas

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Areas.razor`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/AreasAtuacaoTests.cs` (a classe de testes web, a que tem `Diretor_ve_as_areas_e_o_formulario`)
- Modify: `tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs` (sai `/areas` da theory das telas ainda não migradas)
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: `CabecalhoDePagina`, `Aviso`, `EstadoDaLista`, `MenuDeAcoes`, `RotuloDeCampo`, `Contagem.ComInativos`, `BlocoDeVisao.De`, `DespachanteCombinado`; `ListarAreasAtuacao`, `SalvarAreaAtuacao`, `DefinirAtivoAreaAtuacao`, `AreaAtuacaoResumo(Id, Nome, Ativo)`, que não mudam.
- Produces: nada que outra tarefa consuma.

**Regras:**
- Quadros: `Areas.dc.html`, `AreasTablet.dc.html`, `AreasMobile.dc.html`.
- Ordem da marcação dentro do `aj-lado-a-lado`: a tabela (desktop), o painel do formulário, a lista em painel (tablet e mobile). Assim o tablet e o mobile mostram o painel antes da lista, como os quadros.
- Falha ao consultar a lista: o erro, com "Tentar de novo", no lugar da tabela e no lugar da lista em painel (`EstadoDaLista`); o painel do formulário continua. Lista vazia: "Nenhuma área.". A mensagem de uma ação (sucesso ou erro) fica num `Aviso` no topo, como hoje.
- "Renomear" (na tabela e no menu) preenche o painel ("Renomear área") e leva o foco ao campo "Nome", uma vez por clique, com a guarda de exceção do `MenuDeAcoes`. "Cancelar" aparece só ao renomear.
- "Inativar" e "Ativar" continuam de um clique só, como hoje.

- [ ] **Step 1: Testes (devem falhar)**

Na classe de testes web de Áreas, o teste do diretor passa a:

```csharp
    [Fact]
    public async Task Diretor_ve_as_areas_e_o_formulario()
    {
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        await _app.ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "Trabalhista"));
        var previdenciario = await _app.ExecutarAsync(adminId, new SalvarAreaAtuacao(null, "Previdenciário"));
        (await _app.ExecutarAsync(adminId, new DefinirAtivoAreaAtuacao(previdenciario.Valor, false))).Sucesso.Should().BeTrue();
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/areas"));

        html.Should().Contain("<p class=\"aj-subtitulo\">2 áreas · 1 inativa</p>")
            .And.Contain("<span class=\"aj-subtitulo\">2 áreas · 1 inativa</span>")
            .And.MatchRegex("<form[^>]*class=\"aj-painel aj-lado-a-lado__painel\"")
            .And.Contain("<h2>Nova área</h2>")
            .And.MatchRegex("(?s)<div class=\"aj-campo-com-botao\">.*?<input[^>]*class=\"aj-campo[^\"]*\".*?<button type=\"submit\" class=\"aj-botao aj-botao--primario\"[^>]*>Salvar</button>");
        // Fora da renomeação, o painel não tem "Cancelar" (o "Salvar" do mesmo painel está acima).
        html.Should().NotContain(">Cancelar</button>");
        // A tabela vem antes do painel e o painel antes da lista do tablet e do mobile.
        html.IndexOf("<th scope=\"col\">Nome</th>", StringComparison.Ordinal).Should().BeLessThan(html.IndexOf("<h2>Nova área</h2>", StringComparison.Ordinal));
        html.IndexOf("<h2>Nova área</h2>", StringComparison.Ordinal).Should().BeLessThan(html.IndexOf("<h2>Áreas</h2>", StringComparison.Ordinal));

        var tabela = BlocoDeVisao.De(html, "aj-so-desktop");
        tabela.Should().Contain("<th scope=\"col\">Situação</th>").And.Contain("<span class=\"aj-so-leitor\">Ações</span>")
            .And.MatchRegex("<tr>\\s*<td class=\"aj-tabela__principal\">Trabalhista</td>\\s*<td>Ativa</td>")
            .And.MatchRegex("<tr class=\"aj-tabela__linha--inativa\">\\s*<td class=\"aj-tabela__principal\">Previdenciário</td>\\s*<td>Inativa</td>")
            .And.MatchRegex("<td class=\"aj-tabela__acao aj-tabela__acao--varias\"><button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--menor\"[^>]*>Renomear</button><button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--menor\"[^>]*>Ativar</button></td>");

        var linhas = BlocoDeVisao.De(html, "aj-tablet-mobile");
        linhas.Should().MatchRegex("<section class=\"aj-painel aj-painel--principal aj-painel--lista\">\\s*<h2>Áreas</h2>")
            .And.MatchRegex("<div class=\"aj-linha aj-linha--inativa\">\\s*<div class=\"aj-linha__texto\"><span class=\"aj-linha__titulo\">Previdenciário</span><span class=\"aj-ajuda\">Inativa</span></div>")
            .And.MatchRegex("(?s)aria-label=\"Ações de Trabalhista\".*?<button type=\"button\" class=\"aj-menu-acoes__item\"[^>]*>Renomear</button>\\s*<button type=\"button\" class=\"aj-menu-acoes__item\"[^>]*>Inativar</button>");
        linhas.Should().NotContain("<table");
    }
```

O de Produção ganha a positiva:

```csharp
        WebUtility.HtmlDecode(await cliente.GetStringAsync("/areas"))
            .Should().Contain("<h1 class=\"aj-titulo\">Áreas de atuação</h1>").And.Contain("Acesso negado.")
            .And.NotContain("Nova área").And.NotContain("<table");
```

E um teste de componente, novo, na mesma classe (a página se chama `Areas`; se o nome for ambíguo no arquivo de teste, use um alias de `using`):

```csharp
    [Fact]
    public async Task Falha_na_consulta_das_areas_mostra_o_erro_no_lugar_da_lista_e_mantem_o_formulario()
    {
        var diretora = new UsuarioAutenticado(Guid.NewGuid(), "Diana Diretora", TipoPerfil.Diretoria, Guid.NewGuid(), null);
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(diretora)
            .Responde<ListarAreasAtuacao, Resultado<IReadOnlyList<AreaAtuacaoResumo>>>(
                Resultado<IReadOnlyList<AreaAtuacaoResumo>>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<Areas>(despachante, new()));

        html.Should().MatchRegex("<div class=\"aj-aviso aj-aviso--erro aj-aviso--com-acoes\"[^>]*role=\"alert\"")
            .And.Contain("Falha de teste.").And.Contain(">Tentar de novo</button>").And.Contain("<h2>Nova área</h2>");
        html.Should().NotContain("Nenhuma área.").And.NotContain("<table").And.NotContain("aj-subtitulo\">0");
    }
```

Em `LayoutWebTests`, sai `[InlineData("/areas")]` da theory `Tela_ainda_nao_migrada_nao_abre_o_conteudo_com_bloco_do_design_system`.

Run: `dotnet build -m:1` e `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AreasAtuacao"`
Expected: o teste do diretor e o de componente falham (a tela ainda é a antiga).

- [ ] **Step 2: A tela**

```razor
@page "/areas"
@using System.ComponentModel.DataAnnotations
@inject IDespachante Despachante

<PageTitle>Áreas de atuação</PageTitle>

<CabecalhoDePagina Titulo="Áreas de atuação" Subtitulo="@Subtitulo" />

@if (!_carregado)
{
    <p class="aj-subtitulo" role="status">Carregando…</p>
}
else if (!_autorizado)
{
    <Aviso Tipo="TipoDeAviso.Erro">Acesso negado.</Aviso>
}
else
{
    @if (_mensagem is not null)
    {
        <Aviso Tipo="@(_falhou ? TipoDeAviso.Erro : TipoDeAviso.Sucesso)">@_mensagem</Aviso>
    }

    <div class="aj-lado-a-lado">
        <div class="aj-so-desktop">
            <div class="aj-lado-a-lado__lista">
                <EstadoDaLista Erro="@_erroDaLista" Vazio="@(_areas.Count == 0)" TextoDeVazio="Nenhuma área." TentarDeNovo="TentarDeNovoAsync">
                    <div class="aj-tabela-caixa">
                        <table class="aj-tabela">
                            <thead><tr><th scope="col">Nome</th><th scope="col">Situação</th><th scope="col"><span class="aj-so-leitor">Ações</span></th></tr></thead>
                            <tbody>
                                @foreach (var area in _areas)
                                {
                                    <tr class="@ClasseDaLinha(area)" @key="area.Id">
                                        <td class="aj-tabela__principal">@area.Nome</td>
                                        <td>@Situacao(area)</td>
                                        @* Os dois botões colados: o espaço entre eles seria desenhado. *@
                                        <td class="aj-tabela__acao aj-tabela__acao--varias"><button type="button" class="aj-botao aj-botao--fantasma aj-botao--menor" @onclick="() => Editar(area)">Renomear</button><button type="button" class="aj-botao aj-botao--fantasma aj-botao--menor" disabled="@_salvando" @onclick="() => AlternarAtivoAsync(area)">@Alternar(area)</button></td>
                                    </tr>
                                }
                            </tbody>
                        </table>
                    </div>
                </EstadoDaLista>
            </div>
        </div>

        <EditForm Model="_form" OnValidSubmit="SalvarAsync" FormName="area" class="aj-painel aj-lado-a-lado__painel">
            <DataAnnotationsValidator />
            <h2>@TituloDoPainel</h2>
            <div class="aj-campo-com-botao">
                <RotuloDeCampo Texto="Nome" Para="() => _form.Nome">
                    <InputText @ref="_campoNome" @bind-Value="_form.Nome" class="aj-campo" />
                </RotuloDeCampo>
                <button type="submit" class="aj-botao aj-botao--primario" disabled="@_salvando">Salvar</button>
                @if (_form.Id is not null)
                {
                    <button type="button" class="aj-botao aj-botao--fantasma" @onclick="Limpar">Cancelar</button>
                }
            </div>
        </EditForm>

        <div class="aj-tablet-mobile">
            <EstadoDaLista Erro="@_erroDaLista" Vazio="@(_areas.Count == 0)" TextoDeVazio="Nenhuma área." TentarDeNovo="TentarDeNovoAsync">
                <section class="aj-painel aj-painel--principal aj-painel--lista">
                    <h2>Áreas</h2>
                    @foreach (var area in _areas)
                    {
                        <div class="@ClasseDaLinhaNoPainel(area)" @key="area.Id">
                            <div class="aj-linha__texto"><span class="aj-linha__titulo">@area.Nome</span><span class="aj-ajuda">@Situacao(area)</span></div>
                            <MenuDeAcoes Rotulo="@($"Ações de {area.Nome}")">
                                <button type="button" class="aj-menu-acoes__item" @onclick="() => Editar(area)">Renomear</button>
                                <button type="button" class="aj-menu-acoes__item" disabled="@_salvando" @onclick="() => AlternarAtivoAsync(area)">@Alternar(area)</button>
                            </MenuDeAcoes>
                        </div>
                    }
                </section>
            </EstadoDaLista>
        </div>
    </div>
}
```

No `@code`, a partir do que existe hoje:

- Estado novo: `_erroDaLista` (`string?`), `_salvando` (`bool`, mesma guarda de clique duplo de Perfis), `_campoNome` (`InputText?`), `_focarNome` (`bool`).
- `RecarregarAsync`: acesso negado continua encerrando a tela; outra falha vai para `_erroDaLista` (não mais para `_mensagem`); sucesso limpa `_erroDaLista`. `TentarDeNovoAsync` chama `RecarregarAsync`.
- `Editar` marca `_focarNome = true`; `OnAfterRenderAsync` faz o foco uma vez, com `try`/`catch (Exception ex) when (ex is InvalidOperationException or JSException or JSDisconnectedException)`, em `_campoNome?.Element`.
- Ajudantes:

```csharp
    private string? Subtitulo => _carregado && _autorizado && _erroDaLista is null && _areas.Count > 0
        ? Contagem.ComInativos(_areas.Count, _areas.Count(a => !a.Ativo), "área", "áreas", "inativa", "inativas")
        : null;

    private string TituloDoPainel => _form.Id is null ? "Nova área" : "Renomear área";

    private static string Situacao(AreaAtuacaoResumo area) => area.Ativo ? "Ativa" : "Inativa";

    private static string Alternar(AreaAtuacaoResumo area) => area.Ativo ? "Inativar" : "Ativar";

    private static string? ClasseDaLinha(AreaAtuacaoResumo area) => area.Ativo ? null : "aj-tabela__linha--inativa";

    private static string ClasseDaLinhaNoPainel(AreaAtuacaoResumo area) => area.Ativo ? "aj-linha" : "aj-linha aj-linha--inativa";
```

As mensagens de sucesso, a regra de acesso perdido no meio do uso e a validação "Informe o nome." não mudam.

- [ ] **Step 3: Trava e testes**

Acrescente `Components/Pages/Areas.razor` a `Migradas`.

Run: `dotnet build -m:1`
Expected: 0 avisos.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AreasAtuacao|FullyQualifiedName~LayoutWebTests|FullyQualifiedName~PortalDoClienteWebTests"`
Expected: todos passando.

- [ ] **Step 4: Commit**

```
git add src tests
git commit -m "feat(web): áreas no design system, com o painel de nova área ao lado da tabela e a lista em painel no tablet e no mobile"
```

---

### Task 4: Perfis

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Perfis.razor`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PerfisWebTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PortalDoClienteWebTests.cs` (`Tela_de_perfis_mostra_o_perfil_cliente_e_nao_o_oferece_ao_criar`)
- Modify: `tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs` (sai `/perfis` da theory)
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: as mesmas peças da Tarefa 3; `ListarPerfis`, `SalvarPerfil`, `DefinirAtivoPerfil`, `PerfilDetalhe(Id, Nome, Tipo, Ativo, UsuariosAtivos)`, `RotulosDePerfil.DoTipo`, que não mudam.
- Produces: nada que outra tarefa consuma.

**Regras:**
- Quadros: `Perfis.dc.html`, `PerfisTablet.dc.html`, `PerfisMobile.dc.html`.
- Ordem da marcação dentro do `aj-lado-a-lado`: a tabela (desktop), a lista em painel (tablet e mobile), o painel do formulário. Assim o tablet e o mobile mostram a lista antes do painel, como os quadros.
- O painel: `Nome`, `Tipo` (desabilitado ao renomear, com a opção do tipo atual, como hoje), a ajuda só ao criar, e `aj-painel__acoes` com `Cancelar` (fantasma) e `Salvar` (primário), nesta ordem.
- A lógica de recarga fica como está: falha na recarga depois de uma ação não apaga a lista que está na tela e avisa "…, mas não foi possível atualizar a lista.". Só a falha da primeira consulta (nada na tela ainda) vai para o `EstadoDaLista`, com "Tentar de novo".
- "Renomear" leva o foco ao campo "Nome", como em Áreas.

- [ ] **Step 1: Testes (devem falhar)**

`PerfisWebTests.Diretor_ve_os_perfis_ativos_e_inativos_com_as_acoes` passa a:

```csharp
        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/perfis"));

        html.Should().Contain("<p class=\"aj-subtitulo\">4 perfis · 1 inativo</p>").And.Contain("<h2>Novo perfil</h2>")
            .And.MatchRegex("<form[^>]*class=\"aj-painel aj-lado-a-lado__painel\"")
            .And.Contain("O tipo não pode ser mudado depois.")
            .And.MatchRegex("<div class=\"aj-painel__acoes\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--fantasma\"[^>]*>Cancelar</button>\\s*<button type=\"submit\" class=\"aj-botao aj-botao--primario\"[^>]*>Salvar</button>");
        html.Should().NotContain("Acesso negado");
        // A lista do tablet e do mobile vem antes do painel.
        html.IndexOf("<h2>Perfis</h2>", StringComparison.Ordinal).Should().BeLessThan(html.IndexOf("<h2>Novo perfil</h2>", StringComparison.Ordinal));

        var tabela = BlocoDeVisao.De(html, "aj-so-desktop");
        tabela.Should().Contain("<th scope=\"col\">Usuários ativos</th>")
            .And.MatchRegex("<tr>\\s*<td class=\"aj-tabela__principal\">Diretor</td>\\s*<td>Diretoria</td>\\s*<td>1</td>\\s*<td>Ativo</td>")
            .And.MatchRegex("<tr class=\"aj-tabela__linha--inativa\">\\s*<td class=\"aj-tabela__principal\">Estagiário</td>\\s*<td>Produção</td>\\s*<td>0</td>\\s*<td>Inativo</td>")
            .And.MatchRegex("<td class=\"aj-tabela__acao aj-tabela__acao--varias\"><button[^>]*>Renomear</button><button[^>]*>Ativar</button></td>");

        var linhas = BlocoDeVisao.De(html, "aj-tablet-mobile");
        linhas.Should().MatchRegex("<section class=\"aj-painel aj-painel--principal aj-painel--lista\">\\s*<h2>Perfis</h2>")
            .And.Contain("<span class=\"aj-ajuda\">Diretoria · 1 usuário ativo · Ativo</span>")
            .And.MatchRegex("<div class=\"aj-linha aj-linha--inativa\">\\s*<div class=\"aj-linha__texto\"><span class=\"aj-linha__titulo\">Estagiário</span><span class=\"aj-ajuda\">Produção · 0 usuários ativos · Inativo</span></div>")
            .And.Contain("aria-label=\"Ações de Estagiário\"");
        linhas.Should().NotContain("<table");
```

`Producao_nao_ve_a_gestao_de_perfis` ganha a positiva `Contain("<h1 class=\"aj-titulo\">Perfis</h1>")`.

E um teste de componente, novo, na mesma classe:

```csharp
    [Fact]
    public async Task Falha_na_consulta_dos_perfis_mostra_o_erro_no_lugar_da_lista_e_mantem_o_formulario()
    {
        var diretora = new UsuarioAutenticado(Guid.NewGuid(), "Diana Diretora", TipoPerfil.Diretoria, Guid.NewGuid(), null);
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(diretora)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(
                Resultado<IReadOnlyList<PerfilDetalhe>>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<Perfis>(despachante, new()));

        html.Should().MatchRegex("<div class=\"aj-aviso aj-aviso--erro aj-aviso--com-acoes\"[^>]*role=\"alert\"")
            .And.Contain("Falha de teste.").And.Contain(">Tentar de novo</button>").And.Contain("<h2>Novo perfil</h2>");
        html.Should().NotContain("Nenhum perfil.").And.NotContain("<table").And.NotContain("aj-subtitulo\">0");
    }
```

(Se `Perfis` for ambíguo no arquivo de teste, use um alias de `using`.)

Em `PortalDoClienteWebTests.Tela_de_perfis_mostra_o_perfil_cliente_e_nao_o_oferece_ao_criar`, a linha passa a ser procurada no bloco do desktop, pela marcação nova:

```csharp
        var linha = Regex.Matches(BlocoDeVisao.De(html, "aj-so-desktop"), "<tr[^>]*>.*?</tr>", RegexOptions.Singleline)
            .Select(m => m.Value)
            .Single(l => Regex.IsMatch(l, "<td class=\"aj-tabela__principal\">Cliente</td>\\s*<td>Cliente</td>"));
        linha.Should().Contain("Renomear");
```

(o `select` continua como está: há um só formulário na página).

Em `LayoutWebTests`, sai `[InlineData("/perfis")]` da theory das telas ainda não migradas.

Run: `dotnet build -m:1` e `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~PerfisWebTests"`
Expected: o teste do diretor e o de componente falham.

- [ ] **Step 2: A tela**

A mesma estrutura de Áreas, com estas diferenças:

- `<CabecalhoDePagina Titulo="Perfis" Subtitulo="@Subtitulo" />`, com `Subtitulo` = `Contagem.ComInativos(_perfis.Count, inativos, "perfil", "perfis", "inativo", "inativos")` nas mesmas condições de Áreas.
- Tabela: colunas `Nome`, `Tipo`, `Usuários ativos`, `Situação`, ações; células `<td class="aj-tabela__principal">@perfil.Nome</td><td>@RotulosDePerfil.DoTipo(perfil.Tipo)</td><td>@perfil.UsuariosAtivos</td><td>@Situacao(perfil)</td>` e a célula de ações como em Áreas ("Renomear" e "Inativar"/"Ativar" colados).
- Lista em painel com `<h2>Perfis</h2>` e a linha de ajuda `@Resumo(perfil)`:

```csharp
    private static string Resumo(PerfilDetalhe perfil) =>
        $"{RotulosDePerfil.DoTipo(perfil.Tipo)} · {Contagem.De(perfil.UsuariosAtivos, "usuário ativo", "usuários ativos")} · {Situacao(perfil)}";

    private static string Situacao(PerfilDetalhe perfil) => perfil.Ativo ? "Ativo" : "Inativo";
```

- O painel (depois da lista em painel):

```razor
        <EditForm Model="_form" OnValidSubmit="SalvarAsync" FormName="perfil" class="aj-painel aj-lado-a-lado__painel">
            <DataAnnotationsValidator />
            <h2>@TituloDoPainel</h2>
            <RotuloDeCampo Texto="Nome" Para="() => _form.Nome">
                <InputText @ref="_campoNome" @bind-Value="_form.Nome" class="aj-campo" />
            </RotuloDeCampo>
            <label class="aj-rotulo">
                Tipo
                <InputSelect @bind-Value="_form.Tipo" disabled="@(_form.Id is not null)" class="aj-campo">
                    <option value="@TipoPerfil.Producao">Produção</option>
                    <option value="@TipoPerfil.Diretoria">Diretoria</option>
                    @if (_form.Tipo == TipoPerfil.Cliente)
                    {
                        @* Só ao renomear o perfil de cliente, para o campo (desabilitado) mostrar o tipo dele. *@
                        <option value="@TipoPerfil.Cliente">Cliente</option>
                    }
                </InputSelect>
            </label>
            @if (_form.Id is null)
            {
                <span class="aj-ajuda">O tipo não pode ser mudado depois. Um perfil novo passa a acessar todos os Documentos Gerais já existentes; um perfil de Diretoria acessa tudo, inclusive os Contratos de Honorários.</span>
            }
            <div class="aj-painel__acoes"><button type="button" class="aj-botao aj-botao--fantasma" @onclick="Limpar">Cancelar</button><button type="submit" class="aj-botao aj-botao--primario" disabled="@_salvando">Salvar</button></div>
        </EditForm>
```

(`TituloDoPainel` = "Novo perfil" ou "Renomear perfil"; a ajuda é `span`, não `p`: `.aj-painel p` mudaria a cor dela.)

- Falha da primeira consulta: `_erroDaLista`, mostrado pelo `EstadoDaLista` (texto de vazio "Nenhum perfil."); recarga que falha depois de uma ação segue a regra de hoje.

- [ ] **Step 3: Trava e testes**

Acrescente `Components/Pages/Perfis.razor` a `Migradas`.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~PerfisWebTests|FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~LayoutWebTests"`
Expected: 0 avisos; todos passando.

- [ ] **Step 4: Commit**

```
git add src tests
git commit -m "feat(web): perfis no design system, com o painel de novo perfil ao lado da tabela e a lista em painel no tablet e no mobile"
```

---

### Task 5: Formulário de usuário em tela própria

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Pages/UsuarioFormulario.razor`
- Create: `src/AtividadesJuridicas.Web/Components/Usuarios/ConfirmacaoDeInativacao.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/_Imports.razor` (`@using AtividadesJuridicas.Web.Components.Usuarios`)
- Create: `tests/AtividadesJuridicas.IntegrationTests/UsuariosWebTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PortalDoClienteWebTests.cs` (theory `Cliente_em_pagina_interna_ve_nao_encontrada_e_nenhum_dado`)
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: `CabecalhoDeDetalhe`, `Aviso`, `RotuloDeCampo`, `Icone` (`buscar`, `confirmar`), `Recados`, `TextosDaInativacao`, `RotulosDePerfil`; `ListarUsuarios`, `ListarPerfis`, `ListarClientes`, `CriarUsuario`, `EditarUsuario`, `RedefinirSenhaUsuario`, `ObterPendenciasDoUsuario`, `DefinirAtivoUsuario`, que não mudam.
- Produces:
  - Rotas `/usuarios/novo` e `/usuarios/{Id:guid}/editar` (`UsuarioFormulario.razor`, `[Parameter] public Guid? Id`).
  - `<ConfirmacaoDeInativacao UsuarioId Nome OnConcluido OnCancelar OnAcessoEncerrado />`: consulta as pendências ao aparecer, mostra o `Aviso` de confirmação (diálogo de alerta, foco ao aparecer) e, em "Confirmar inativação", envia `DefinirAtivoUsuario(UsuarioId, false)` e entrega o `Resultado` em `OnConcluido` (`EventCallback<Resultado>`). `OnCancelar` (`EventCallback`); `OnAcessoEncerrado` (`EventCallback`) quando a consulta das pendências responde acesso negado. A Tarefa 6 usa o componente como está.

**Regras:**
- Quadros: `UsuarioFormulario.dc.html`, `UsuarioFormularioTablet.dc.html`, `UsuarioFormularioMobile.dc.html`; a busca de cliente, por analogia com a busca de processo de `NovoEvento.dc.html` (spec 8: lista com "Escolher", busca sem botão, que responde ao Enter).
- A lógica do formulário vem de `Usuarios.razor` sem mudar: perfis ativos (e o atual inativo, marcado), troca só entre perfis do mesmo lado (cliente ou equipe) na edição, usuário novo começa no primeiro perfil ativo de Produção, OAB só para a equipe, cliente só (e obrigatório) para o perfil de cliente, busca de cliente com os avisos de nenhum e de truncado, senha nova opcional na edição.
- A lista de `Usuarios.razor` continua como está nesta tarefa (com o formulário antigo dentro); a Tarefa 6 a troca.
- Salvar com sucesso: `Recados.Deixar("Usuário salvo.")` e navega para `/usuarios`. Falha: `Aviso` de erro no topo, e o formulário fica.
- Edição: o usuário vem de `ListarUsuarios` (não há consulta de um só). Identificador que não está na lista: o cabeçalho, o voltar e o aviso de erro "Usuário não encontrado.", sem formulário.
- No tablet e no mobile, a edição traz o painel "Situação" depois do painel "Usuário", afastado da barra de ações: `Ativo` ou `Inativo` e o botão secundário `Inativar usuário` ou `Ativar usuário`. Inativar mostra a `ConfirmacaoDeInativacao` no topo; concluída com sucesso, deixa o recado "Usuário inativado." e volta à lista. Ativar é de um clique: recado "Usuário ativado." e volta à lista. No desktop o painel não aparece: a ação fica na linha da tabela.
- Ações: no desktop, `div.aj-acoes.aj-acoes--formulario` com `Cancelar` (link fantasma para `/usuarios`) e `Salvar usuário` (primário com o ícone `confirmar`); no tablet, `aj-acoes aj-acoes--fixa` com `Cancelar` (link secundário) e `Salvar usuário`; no mobile, o mesmo com `Salvar`. Os três botões de envio ficam dentro do formulário; cada barra no seu invólucro de visão (`aj-so-desktop`, `aj-so-tablet`, `aj-so-mobile`).
- A busca de cliente usa um formulário próprio, escondido (`<form id="busca-de-cliente" hidden …>`), ao qual o campo da busca se associa por `form="busca-de-cliente"`: o Enter na busca não envia o formulário do usuário.
- Produção e quem não é da Diretoria: o cabeçalho de detalhe e "Acesso negado.", sem formulário e sem consulta.

- [ ] **Step 1: Testes (devem falhar)**

`UsuariosWebTests.cs`:

```csharp
using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Web.Components.Usuarios;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class UsuariosWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _pedro, _carla, _maria;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();
        (_carla, _maria) = await _app.CriarCarlaAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> PaginaAsync(string rota) =>
        WebUtility.HtmlDecode(await (await _app.ClienteAutenticadoAsync()).GetStringAsync(rota));

    [Fact]
    public async Task Formulario_novo_traz_o_voltar_os_campos_da_equipe_e_as_acoes_de_cada_visao()
    {
        var html = await PaginaAsync("/usuarios/novo");

        var desktop = BlocoDeVisao.De(html, "aj-desktop-tablet");
        desktop.Should().MatchRegex("<a href=\"/usuarios\" class=\"aj-voltar\">")
            .And.Contain("<p class=\"aj-subtitulo\">Escolha o perfil. Para um usuário de cliente, indique o cliente.</p>")
            .And.MatchRegex("<h1 class=\"aj-titulo\">Novo usuário</h1>");
        html.Should().Contain("aria-label=\"Voltar para Usuários\"")
            .And.MatchRegex("<form[^>]*class=\"aj-painel aj-painel--formulario\"")
            .And.Contain("<h2>Usuário</h2>").And.Contain("<div class=\"aj-grade-campos\">")
            .And.MatchRegex("<span>Número da OAB <small>\\(opcional\\)</small></span>")
            .And.MatchRegex("autocomplete=\"new-password\"");
        // Usuário novo começa em Produção: sem o bloco do cliente.
        html.Should().NotContain("aj-vinculo").And.NotContain("Nova senha");

        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            "(?s)<div class=\"aj-acoes aj-acoes--formulario\">\\s*<a href=\"/usuarios\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>\\s*<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Salvar usuário</button>");
        BlocoDeVisao.De(html, "aj-so-tablet").Should().MatchRegex(
            "<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a href=\"/usuarios\" class=\"aj-botao aj-botao--secundario\">Cancelar</a>");
        BlocoDeVisao.De(html, "aj-so-mobile").Should().MatchRegex("(?s)<div class=\"aj-acoes aj-acoes--fixa\">.*?Salvar</button>")
            .And.NotContain("Salvar usuário");
        // O perfil inativo não é opção para usuário novo, e o escolhido de início é de Produção.
        var select = Regex.Match(html, "<select[^>]*>.*?</select>", RegexOptions.Singleline).Value;
        select.Should().Contain(">Diretor (Diretoria)</option>").And.Contain(">Cliente (Cliente)</option>");
        Regex.Matches(select, "<option[^>]*selected[^>]*>[^<]*</option>").Select(m => m.Value).Should()
            .ContainSingle().Which.Should().Contain("Produção (Produção)");
    }

    [Fact]
    public async Task Edicao_de_usuario_de_cliente_traz_o_cliente_escolhido_e_a_situacao()
    {
        var html = await PaginaAsync($"/usuarios/{_carla}/editar");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Editar usuário</h1>")
            .And.MatchRegex("<input[^>]*value=\"Carla Cliente\"")
            .And.MatchRegex("<div class=\"aj-vinculo aj-grade-campos__inteiro\">")
            .And.MatchRegex("<div class=\"aj-vinculo__escolhido\">\\s*<span class=\"aj-vinculo__nome\">Maria Silva</span>\\s*<button type=\"button\" class=\"aj-botao aj-botao--fantasma\"[^>]*>Trocar</button>")
            .And.Contain("O usuário entra no portal e vê só o que é deste cliente.")
            .And.MatchRegex("<span>Nova senha <small>\\(deixe em branco para manter\\)</small></span>");
        html.Should().NotContain("Número da OAB");

        var situacao = BlocoDeVisao.De(html, "aj-tablet-mobile");
        situacao.Should().MatchRegex("(?s)<section class=\"aj-painel\">\\s*<h2>Situação</h2>.*?Ativo.*?>Inativar usuário</button>");
        BlocoDeVisao.De(html, "aj-so-desktop").Should().Contain("Salvar usuário").And.NotContain("Situação");
    }

    [Fact]
    public async Task Edicao_de_usuario_da_equipe_traz_a_oab_e_nao_o_cliente()
    {
        var html = await PaginaAsync($"/usuarios/{_pedro}/editar");

        html.Should().MatchRegex("<input[^>]*value=\"Pedro Produção\"").And.Contain("Número da OAB");
        html.Should().NotContain("aj-vinculo");
    }

    [Fact]
    public async Task Edicao_de_usuario_que_nao_existe_mostra_nao_encontrado_com_o_voltar()
    {
        var html = await PaginaAsync($"/usuarios/{Guid.NewGuid()}/editar");

        html.Should().Contain("Usuário não encontrado.").And.MatchRegex("<a href=\"/usuarios\" class=\"aj-voltar\">")
            .And.MatchRegex("<h1 class=\"aj-titulo\">Editar usuário</h1>");
        html.Should().NotContain("<h2>Usuário</h2>").And.NotContain("Salvar usuário");
    }

    [Fact]
    public async Task Producao_nao_abre_o_formulario()
    {
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await pedro.GetStringAsync("/usuarios/novo"));

        html.Should().Contain("Acesso negado.").And.MatchRegex("<h1 class=\"aj-titulo\">Novo usuário</h1>");
        html.Should().NotContain("<h2>Usuário</h2>").And.NotContain("diana@x.com");
    }

    [Fact]
    public async Task Confirmacao_de_inativacao_diz_as_pendencias_e_poe_cancelar_antes_do_verbo()
    {
        var despachante = new DespachanteCombinado().Responde<ObterPendenciasDoUsuario, Resultado<PendenciasDoUsuario>>(
            Resultado<PendenciasDoUsuario>.Ok(new PendenciasDoUsuario(2, 1, 3)));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<ConfirmacaoDeInativacao>(despachante, new()
        {
            ["UsuarioId"] = Guid.NewGuid(), ["Nome"] = "Rafael Teles",
        }));

        html.Should().MatchRegex("<div class=\"aj-aviso aj-aviso--atencao aj-aviso--com-acoes\"[^>]*role=\"alertdialog\"")
            .And.Contain("aria-label=\"Confirmar inativação\"")
            .And.Contain("<strong>Confirmar a inativação de Rafael Teles?</strong>")
            .And.Contain("Este usuário tem 2 partes pendentes, 1 revisão em aberto e 3 eventos futuros.")
            .And.MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--secundario\"[^>]*>Cancelar</button>\\s*<button type=\"button\" class=\"aj-botao aj-botao--primario\"[^>]*>Confirmar inativação</button>");
    }

    [Fact]
    public async Task Confirmacao_sem_as_pendencias_avisa_que_nao_verificou()
    {
        var despachante = new DespachanteCombinado().Responde<ObterPendenciasDoUsuario, Resultado<PendenciasDoUsuario>>(
            Resultado<PendenciasDoUsuario>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<ConfirmacaoDeInativacao>(despachante, new()
        {
            ["UsuarioId"] = Guid.NewGuid(), ["Nome"] = "Rafael Teles",
        }));

        html.Should().Contain("<strong>Confirmar a inativação de Rafael Teles?</strong>")
            .And.Contain("Não foi possível verificar as pendências deste usuário.").And.Contain(">Confirmar inativação</button>");
    }
}
```

Em `PortalDoClienteWebTests`, acrescente `[InlineData("/usuarios/novo")]` à theory `Cliente_em_pagina_interna_ve_nao_encontrada_e_nenhum_dado`.

Run: `dotnet build -m:1`
Expected: não compila (`ConfirmacaoDeInativacao` não existe). É o RED.

- [ ] **Step 2: `ConfirmacaoDeInativacao`**

```razor
@inject IDespachante Despachante

@* A confirmação de inativar um usuário, com o que ele deixa pendente. Aparece no topo da tela; a pergunta é o
   título, as pendências são o texto, e "Cancelar" vem antes do verbo. *@
@if (_carregado)
{
    <Aviso Tipo="TipoDeAviso.Atencao" Rotulo="Confirmar inativação" Titulo="@TextosDaInativacao.Pergunta(Nome)">
        <ChildContent>@_pendencias</ChildContent>
        <Acoes><button type="button" class="aj-botao aj-botao--secundario" disabled="@_enviando" @onclick="CancelarAsync">Cancelar</button><button type="button" class="aj-botao aj-botao--primario" disabled="@_enviando" @onclick="ConfirmarAsync">Confirmar inativação</button></Acoes>
    </Aviso>
}

@code {
    [Parameter, EditorRequired] public Guid UsuarioId { get; set; }
    [Parameter, EditorRequired] public string Nome { get; set; } = string.Empty;

    /// <summary>O resultado da inativação, para a tela decidir o aviso e o que recarregar.</summary>
    [Parameter] public EventCallback<Resultado> OnConcluido { get; set; }

    [Parameter] public EventCallback OnCancelar { get; set; }

    /// <summary>A consulta das pendências respondeu acesso negado.</summary>
    [Parameter] public EventCallback OnAcessoEncerrado { get; set; }

    private bool _carregado;
    private bool _enviando;
    private string? _pendencias;

    protected override async Task OnInitializedAsync()
    {
        var pendencias = await Despachante.EnviarAsync(new ObterPendenciasDoUsuario(UsuarioId));
        if (pendencias.Erro == TipoErro.AcessoNegado)
        {
            await OnAcessoEncerrado.InvokeAsync();
            return;
        }

        // Sem a contagem, a confirmação continua disponível e diz que não verificou.
        _pendencias = pendencias.Sucesso
            ? TextosDaInativacao.Pendencias(Nome, pendencias.Valor!)
            : "Não foi possível verificar as pendências deste usuário.";
        _carregado = true;
    }

    private async Task ConfirmarAsync()
    {
        if (_enviando) return;
        _enviando = true;
        try
        {
            await OnConcluido.InvokeAsync(await Despachante.EnviarAsync(new DefinirAtivoUsuario(UsuarioId, false)));
        }
        finally
        {
            _enviando = false;
        }
    }

    private Task CancelarAsync() => OnCancelar.InvokeAsync();
}
```

A tela que usa o componente põe `@key="usuario.Id"` nele, para outra inativação pedida recomeçar a consulta e o foco.

- [ ] **Step 3: A tela do formulário**

```razor
@page "/usuarios/novo"
@page "/usuarios/{Id:guid}/editar"
@using System.ComponentModel.DataAnnotations
@inject IDespachante Despachante
@inject NavigationManager Navegacao
@inject Recados Recados

<PageTitle>@Titulo</PageTitle>

<CabecalhoDeDetalhe Titulo="@Titulo" Subtitulo="Escolha o perfil. Para um usuário de cliente, indique o cliente."
                    VoltarPara="/usuarios" Origem="Usuários" />

@if (!_carregado)
{
    <p class="aj-subtitulo" role="status">Carregando…</p>
}
else if (!_autorizado)
{
    <Aviso Tipo="TipoDeAviso.Erro">Acesso negado.</Aviso>
}
else if (_naoEncontrado)
{
    <Aviso Tipo="TipoDeAviso.Erro">Usuário não encontrado.</Aviso>
}
else
{
    @if (_mensagem is not null)
    {
        <Aviso Tipo="TipoDeAviso.Erro">@_mensagem</Aviso>
    }

    @if (_inativando)
    {
        <ConfirmacaoDeInativacao @key="Id" UsuarioId="Id!.Value" Nome="@_form.Nome" OnConcluido="AoInativarAsync"
                                 OnCancelar="() => _inativando = false" OnAcessoEncerrado="() => _autorizado = false" />
    }

    <EditForm Model="_form" OnValidSubmit="SalvarAsync" FormName="usuario" class="aj-painel aj-painel--formulario">
        <DataAnnotationsValidator />
        <h2>Usuário</h2>
        <div class="aj-grade-campos">
            <RotuloDeCampo Texto="Nome" Para="() => _form.Nome">
                <InputText @bind-Value="_form.Nome" class="aj-campo" />
            </RotuloDeCampo>
            <RotuloDeCampo Texto="E-mail" Para="() => _form.Email">
                <InputText @bind-Value="_form.Email" type="email" class="aj-campo" />
            </RotuloDeCampo>
            <RotuloDeCampo Texto="Perfil" Para="() => _form.PerfilId">
                <InputSelect Value="_form.PerfilId" ValueExpression="() => _form.PerfilId" ValueChanged="(Guid? id) => AoMudarPerfil(id)" class="aj-campo">
                    @if (_form.PerfilId is null)
                    {
                        <option value="">Selecione…</option>
                    }
                    @if (_perfilInativoAtual is { } inativo)
                    {
                        <option value="@inativo.Id">@Rotulo(inativo)</option>
                    }
                    @foreach (var perfil in _perfis.Where(p => p.Ativo && PodeEscolher(p)))
                    {
                        <option value="@perfil.Id">@Rotulo(perfil)</option>
                    }
                </InputSelect>
                @if (_perfilInativoAtual is { } atual && _form.PerfilId == atual.Id)
                {
                    <span class="aj-ajuda">O perfil atual está inativo. Escolha um perfil ativo para poder reativar o usuário.</span>
                }
            </RotuloDeCampo>
            @if (!PerfilEscolhidoEhDeCliente)
            {
                <label class="aj-rotulo"><span>Número da OAB <small>(opcional)</small></span><InputText @bind-Value="_form.NumeroOab" class="aj-campo" /></label>
            }
            @if (Id is null)
            {
                <label class="aj-rotulo">Senha<InputText @bind-Value="_form.Senha" type="password" autocomplete="new-password" class="aj-campo" /></label>
            }
            else
            {
                <label class="aj-rotulo"><span>Nova senha <small>(deixe em branco para manter)</small></span><InputText @bind-Value="_form.Senha" type="password" autocomplete="new-password" class="aj-campo" /></label>
            }
            @if (PerfilEscolhidoEhDeCliente)
            {
                <div class="aj-vinculo aj-grade-campos__inteiro">
                    <span class="@(_erroDoCliente is null ? "aj-rotulo" : "aj-rotulo aj-rotulo--erro")">Cliente</span>
                    @if (_form.ClienteId is not null)
                    {
                        <div class="aj-vinculo__escolhido"><span class="aj-vinculo__nome">@_form.ClienteNome</span><button type="button" class="aj-botao aj-botao--fantasma" @onclick="TrocarCliente">Trocar</button></div>
                    }
                    else
                    {
                        @* O Enter na busca envia o formulário "busca-de-cliente", declarado depois deste, e não o do usuário. *@
                        <label class="aj-campo-icone"><Icone Nome="buscar" /><span class="aj-so-leitor">Buscar cliente</span><input type="text" class="aj-campo-icone__controle" form="busca-de-cliente" placeholder="Buscar cliente: nome, CPF ou CNPJ" @bind="_buscaDeCliente" @bind:event="oninput" /></label>
                        @if (_clientesEncontrados.Count > 0)
                        {
                            <ul class="aj-resultados">
                                @foreach (var candidato in _clientesEncontrados)
                                {
                                    <li @key="candidato.Id"><button type="button" class="aj-resultados__item" @onclick="() => EscolherCliente(candidato)"><span class="aj-resultados__texto"><span class="aj-resultados__titulo">@candidato.Nome</span><span class="aj-resultados__meta">@candidato.Documento</span></span><span class="aj-resultados__acao">Escolher</span></button></li>
                                }
                            </ul>
                        }
                        @if (_nenhumCliente)
                        {
                            <span class="aj-ajuda">Nenhum cliente encontrado.</span>
                        }
                        @if (_clientesTruncados)
                        {
                            <span class="aj-ajuda">Mostrando os primeiros resultados. Refine a busca para ver outros.</span>
                        }
                    }
                    @if (_erroDoCliente is not null)
                    {
                        <span class="aj-campo-erro">@_erroDoCliente</span>
                    }
                    <span class="aj-ajuda">O usuário entra no portal e vê só o que é deste cliente.</span>
                </div>
            }
        </div>

        <div class="aj-so-desktop">
            <div class="aj-acoes aj-acoes--formulario"><a href="/usuarios" class="aj-botao aj-botao--fantasma">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" disabled="@_salvando"><Icone Nome="confirmar" />Salvar usuário</button></div>
        </div>
        <div class="aj-so-tablet">
            <div class="aj-acoes aj-acoes--fixa"><a href="/usuarios" class="aj-botao aj-botao--secundario">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" disabled="@_salvando"><Icone Nome="confirmar" />Salvar usuário</button></div>
        </div>
        <div class="aj-so-mobile">
            <div class="aj-acoes aj-acoes--fixa"><a href="/usuarios" class="aj-botao aj-botao--secundario">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" disabled="@_salvando"><Icone Nome="confirmar" />Salvar</button></div>
        </div>
    </EditForm>

    @if (Id is not null)
    {
        <div class="aj-tablet-mobile">
            <section class="aj-painel">
                <h2>Situação</h2>
                <span class="aj-ajuda">@(_ativo ? "Ativo" : "Inativo")</span>
                <div class="aj-painel__acoes"><button type="button" class="aj-botao aj-botao--secundario" disabled="@(_salvando || _inativando)" @onclick="AlternarAtivoAsync">@(_ativo ? "Inativar usuário" : "Ativar usuário")</button></div>
            </section>
        </div>
    }

    <form id="busca-de-cliente" hidden @onsubmit="BuscarClientesAsync" @onsubmit:preventDefault></form>
}
```

Atenção ao lugar dos blocos: no desktop o `EditForm` é o último bloco visível antes da barra de ações; a barra de ações do desktop fica dentro do formulário, depois da grade (o quadro a desenha fora do painel; se o `aj-acoes--formulario` dentro do `form.aj-painel` não ficar igual ao quadro na Tarefa 9, mova a barra para fora do formulário com `form="…"` nos botões e registre). As barras fixas do tablet e do mobile ficam presas à coluna pelo CSS.

No `@code`:

- `[Parameter] public Guid? Id { get; set; }`; `Titulo => Id is null ? "Novo usuário" : "Editar usuário"`.
- Estado de `Usuarios.razor` que diz respeito ao formulário (`_form`, `_perfis`, `_perfilInativoAtual`, `_tipoOriginal`, a busca de cliente), mais `_naoEncontrado`, `_ativo`, `_inativando`, `_salvando`, `_mensagem`.
- `OnInitializedAsync`: confere a Diretoria antes de consultar; carrega `ListarPerfis`; sem `Id`, começa como `Limpar()` de hoje; com `Id`, procura o usuário em `ListarUsuarios` e preenche como o `Editar` de hoje, ou marca `_naoEncontrado`. Acesso negado em qualquer consulta: `_autorizado = false`.
- `SalvarAsync`: a regra de hoje (cliente obrigatório para perfil de cliente; `EditarUsuario` e, com senha, `RedefinirSenhaUsuario`; ou `CriarUsuario`), com guarda de clique duplo. Sucesso: `Recados.Deixar("Usuário salvo.")` e `Navegacao.NavigateTo("/usuarios")`. Falha: `_mensagem = resultado.Mensagem`.
- `AlternarAtivoAsync`: ativo, `_inativando = true`; inativo, `DefinirAtivoUsuario(Id, true)`, e com sucesso `Recados.Deixar("Usuário ativado.")` e volta à lista.
- `AoInativarAsync(Resultado resultado)`: sucesso, `Recados.Deixar("Usuário inativado.")` e volta à lista; falha, `_inativando = false` e `_mensagem = resultado.Mensagem`.

- [ ] **Step 4: Trava e testes**

Acrescente a `Migradas`: `Components/Pages/UsuarioFormulario.razor` e `Components/Usuarios/ConfirmacaoDeInativacao.razor`.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~UsuariosWebTests|FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~PaginasProtegidasTests"`
Expected: 0 avisos; todos passando.

- [ ] **Step 5: Commit**

```
git add src tests
git commit -m "feat(web): formulário de usuário em tela própria, com cliente vinculado por busca, situação na edição e confirmação de inativação reutilizável"
```

---

### Task 6: Lista de usuários

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Usuarios.razor`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/UsuariosWebTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PaginasProtegidasTests.cs` (`Cadastro_de_usuarios_mostra_o_tipo_do_perfil_e_marca_o_perfil_inativo`)
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PortalDoClienteWebTests.cs` (`Cadastro_de_usuarios_mostra_o_cliente_e_o_tipo_cliente`, `Cadastro_de_usuarios_avisa_que_o_cliente_inativo_esta_sem_acesso`)
- Modify: `tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs` (sai `/usuarios` da theory)
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: `CabecalhoDePagina` (com `Acao`, `AcaoHref`), `Aviso`, `EstadoDaLista`, `ConfirmacaoDeInativacao`, `Recados`, `Contagem.ComInativos`, `FusoDoEscritorio.Formatar`; `ListarUsuarios`, `ListarPerfis`, `DefinirAtivoUsuario`.
- Produces: nada que outra tarefa consuma.

**Regras:**
- Quadros: `Usuarios.dc.html`, `UsuariosTablet.dc.html`, `UsuariosMobile.dc.html`.
- O formulário sai da lista (está na Tarefa 5). Sobra: o cabeçalho com "Novo usuário" (link para `/usuarios/novo`, e o botão flutuante no mobile), o aviso do recado ou de uma ação, a confirmação de inativação e a lista. Produção: sem "Novo usuário".
- Desktop: a tabela do quadro, com "Editar" (link para `/usuarios/{id}/editar`) e "Inativar"/"Ativar" colados na célula de ações. Tablet e mobile: cartões (`a.aj-cartao`, link para a edição), sem ação de inativar (ela está na edição).
- O recado deixado pelo formulário aparece uma vez, como aviso de sucesso (ou de erro, pelo tipo), no lugar da mensagem de ação.
- Falha ao consultar a lista que não é de acesso: o erro com "Tentar de novo" no lugar da lista (hoje qualquer falha vira "Acesso negado.": isso muda). Lista vazia: "Nenhum usuário.".
- As marcas de hoje continuam: perfil inativo `{perfil} ({tipo} — inativo)`; cliente inativo `{cliente} (inativo — sem acesso ao portal)`, na tabela e no cartão.

- [ ] **Step 1: Testes (devem falhar)**

Em `UsuariosWebTests`:

```csharp
    [Fact]
    public async Task Lista_traz_a_tabela_no_desktop_e_os_cartoes_no_tablet_e_no_mobile()
    {
        (await _app.ExecutarAsync(_admin, new DefinirAtivoUsuario(_pedro, false))).Sucesso.Should().BeTrue();

        var html = await PaginaAsync("/usuarios");

        html.Should().Contain("<p class=\"aj-subtitulo\">3 usuários · 1 inativo</p>")
            .And.MatchRegex("<a href=\"/usuarios/novo\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\"")
            .And.MatchRegex("<a href=\"/usuarios/novo\" class=\"aj-fab\" aria-label=\"Novo usuário\"");
        html.Should().NotContain("<h2>Novo usuário</h2>").And.NotContain("Senha");

        var tabela = BlocoDeVisao.De(html, "aj-so-desktop");
        tabela.Should().Contain("<th scope=\"col\">Último acesso</th>")
            .And.MatchRegex("<tr class=\"aj-tabela__linha--inativa\">\\s*<td class=\"aj-tabela__principal\">Pedro Produção</td>\\s*<td class=\"aj-tabela__secundaria\">pedro@x.com</td>")
            .And.MatchRegex($"<td class=\"aj-tabela__acao aj-tabela__acao--varias\"><a href=\"/usuarios/{_pedro}/editar\" class=\"aj-botao aj-botao--fantasma aj-botao--menor\">Editar</a><button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--menor\"[^>]*>Ativar</button></td>");

        var cartoes = BlocoDeVisao.De(html, "aj-tablet-mobile");
        cartoes.Should().MatchRegex($"<a href=\"/usuarios/{_pedro}/editar\" class=\"aj-cartao aj-cartao--inativo\">\\s*<span class=\"aj-cartao__titulo\">Pedro Produção</span>")
            .And.MatchRegex("<span class=\"aj-cartao__meta\">pedro@x.com<br\\s*/?>Produção \\(Produção\\)</span>")
            .And.Contain("<span class=\"aj-cartao__situacao\">Inativo</span>")
            .And.MatchRegex("<span class=\"aj-cartao__nota\">(Último acesso \\d{2}/\\d{2}/\\d{4} \\d{2}:\\d{2}|Sem acesso registrado)</span>");
        cartoes.Should().NotContain(">Inativar</button>").And.NotContain(">Ativar</button>");
    }

    [Fact]
    public async Task Recado_do_formulario_aparece_uma_vez_na_lista()
    {
        var recados = new Recados();
        recados.Deixar("Usuário salvo.");

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarAsync<Usuarios>(_app, _admin, new(), recados));
        var depois = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarAsync<Usuarios>(_app, _admin, new(), recados));

        html.Should().MatchRegex("(?s)<p class=\"aj-aviso aj-aviso--sucesso\" role=\"status\">.*?<span>Usuário salvo.</span>");
        // A segunda vez a lista vem (com a diretora) e o recado não: ele é entregue uma vez só.
        depois.Should().Contain("Diana Diretora").And.NotContain("Usuário salvo.");
    }

    [Fact]
    public async Task Falha_na_consulta_dos_usuarios_mostra_o_erro_e_nao_acesso_negado()
    {
        var diretora = new UsuarioAutenticado(Guid.NewGuid(), "Diana Diretora", TipoPerfil.Diretoria, Guid.NewGuid(), null);
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(diretora)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(Resultado<IReadOnlyList<PerfilDetalhe>>.Ok([]))
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(
                Resultado<IReadOnlyList<UsuarioResumo>>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<Usuarios>(despachante, new()));

        html.Should().Contain("Falha de teste.").And.Contain(">Tentar de novo</button>").And.Contain("Novo usuário");
        html.Should().NotContain("Acesso negado").And.NotContain("Nenhum usuário.").And.NotContain("<table");
    }
```

(Se `Usuarios` for ambíguo no arquivo de teste, use um alias de `using`. O arquivo passa a precisar de `using AtividadesJuridicas.Web.Infra;` para `Recados`.)

Em `PaginasProtegidasTests.Cadastro_de_usuarios_mostra_o_tipo_do_perfil_e_marca_o_perfil_inativo`, as asserções da lista (`Diretor (Diretoria)`, `Produção (Produção)`, `Temporário (Produção — inativo)`) ficam sobre `/usuarios`; as do `select` passam a ler `/usuarios/novo`, sem mudar o que provam.

Em `PortalDoClienteWebTests`:
- `Cadastro_de_usuarios_mostra_o_cliente_e_o_tipo_cliente`: `<th>Cliente</th>` vira `<th scope="col">Cliente</th>`; a busca da linha passa a olhar `BlocoDeVisao.De(html, "aj-so-desktop")`; e o cartão da Carla, em `BlocoDeVisao.De(html, "aj-tablet-mobile")`, traz `Cliente (Cliente) · Maria Silva`.
- `Cadastro_de_usuarios_avisa_que_o_cliente_inativo_esta_sem_acesso`: além do texto na página, `Maria Silva (inativo — sem acesso ao portal)` aparece nos dois blocos (tabela e cartão).

Em `LayoutWebTests`, sai `[InlineData("/usuarios")]` da theory das telas ainda não migradas.

Run: `dotnet build -m:1` e o filtro do Step 3.
Expected: os testes novos e os reescritos falham.

- [ ] **Step 2: A tela**

```razor
@page "/usuarios"
@inject IDespachante Despachante
@inject FusoDoEscritorio Fuso
@inject Recados Recados

<PageTitle>Usuários</PageTitle>

<CabecalhoDePagina Titulo="Usuários" Subtitulo="@Subtitulo" Acao="@(_autorizado ? "Novo usuário" : null)" AcaoHref="/usuarios/novo" />

@if (!_carregado)
{
    <p class="aj-subtitulo" role="status">Carregando…</p>
}
else if (!_autorizado)
{
    <Aviso Tipo="TipoDeAviso.Erro">Acesso negado.</Aviso>
}
else
{
    @if (_mensagem is not null)
    {
        <Aviso Tipo="@(_falhou ? TipoDeAviso.Erro : TipoDeAviso.Sucesso)">@_mensagem</Aviso>
    }

    @if (_inativando is { } alvo)
    {
        <ConfirmacaoDeInativacao @key="alvo.Id" UsuarioId="alvo.Id" Nome="@alvo.Nome" OnConcluido="AoInativarAsync"
                                 OnCancelar="CancelarInativacao" OnAcessoEncerrado="() => _autorizado = false" />
    }

    <EstadoDaLista Erro="@_erroDaLista" Vazio="@(_usuarios.Count == 0)" TextoDeVazio="Nenhum usuário." TentarDeNovo="RecarregarAsync">
        <div class="aj-so-desktop">
            <div class="aj-tabela-caixa">
                <table class="aj-tabela">
                    <thead><tr><th scope="col">Nome</th><th scope="col">E-mail</th><th scope="col">Perfil</th><th scope="col">Cliente</th><th scope="col">OAB</th><th scope="col">Último acesso</th><th scope="col">Situação</th><th scope="col"><span class="aj-so-leitor">Ações</span></th></tr></thead>
                    <tbody>
                        @foreach (var usuario in _usuarios)
                        {
                            <tr class="@ClasseDaLinha(usuario)" @key="usuario.Id">
                                <td class="aj-tabela__principal">@usuario.Nome</td>
                                <td class="aj-tabela__secundaria">@usuario.Email</td>
                                <td>@RotuloDoPerfilDe(usuario)</td>
                                <td>@(ClienteDe(usuario) ?? "—")</td>
                                <td class="aj-tabela__data">@Ou(usuario.NumeroOab)</td>
                                <td class="aj-tabela__data">@Fuso.Formatar(usuario.UltimoAcessoEm)</td>
                                <td>@Situacao(usuario)</td>
                                <td class="aj-tabela__acao aj-tabela__acao--varias"><a href="@Edicao(usuario)" class="aj-botao aj-botao--fantasma aj-botao--menor">Editar</a><button type="button" class="aj-botao aj-botao--fantasma aj-botao--menor" disabled="@_emAndamento" @onclick="() => AlternarAtivoAsync(usuario)">@(usuario.Ativo ? "Inativar" : "Ativar")</button></td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
        </div>

        <div class="aj-tablet-mobile">
            <div class="aj-grade">
                @foreach (var usuario in _usuarios)
                {
                    <a href="@Edicao(usuario)" class="@ClasseDoCartao(usuario)" @key="usuario.Id">
                        <span class="aj-cartao__titulo">@usuario.Nome</span>
                        <span class="aj-cartao__meta">@usuario.Email<br />@PerfilDoCartao(usuario)</span>
                        <span class="aj-cartao__rodape"><span class="aj-cartao__nota">@Acesso(usuario)</span><span class="aj-cartao__situacao">@Situacao(usuario)</span></span>
                    </a>
                }
            </div>
        </div>
    </EstadoDaLista>
}
```

No `@code`:

- Fica de hoje: `_perfis` (para a marca de perfil inativo), `AlternarAtivoAsync` (ativar é um clique; inativar abre a confirmação), `RotuloDoPerfilDe`. Sai tudo do formulário.
- `OnInitializedAsync`: depois de conferir a Diretoria e carregar perfis e usuários, `if (Recados.Retirar() is { } recado) { _mensagem = recado.Texto; _falhou = recado.Tipo == TipoDeAviso.Erro; }`.
- `RecarregarAsync`: acesso negado encerra a tela; outra falha vai para `_erroDaLista`; sucesso limpa.
- `AoInativarAsync(Resultado resultado)`: `_inativando = null`; aviso "Usuário inativado." ou a mensagem da falha; recarrega.
- Ajudantes:

```csharp
    private string? Subtitulo => _carregado && _autorizado && _erroDaLista is null && _usuarios.Count > 0
        ? Contagem.ComInativos(_usuarios.Count, _usuarios.Count(u => !u.Ativo), "usuário", "usuários", "inativo", "inativos")
        : null;

    private static string Edicao(UsuarioResumo usuario) => $"/usuarios/{usuario.Id}/editar";

    private static string Situacao(UsuarioResumo usuario) => usuario.Ativo ? "Ativo" : "Inativo";

    private static string Ou(string? valor) => string.IsNullOrWhiteSpace(valor) ? "—" : valor;

    private static string? ClienteDe(UsuarioResumo usuario) => usuario.ClienteNome is null ? null
        : usuario.ClienteAtivo == false ? $"{usuario.ClienteNome} (inativo — sem acesso ao portal)" : usuario.ClienteNome;

    private string PerfilDoCartao(UsuarioResumo usuario) =>
        ClienteDe(usuario) is { } cliente ? $"{RotuloDoPerfilDe(usuario)} · {cliente}" : RotuloDoPerfilDe(usuario);

    private string Acesso(UsuarioResumo usuario) =>
        usuario.UltimoAcessoEm is null ? "Sem acesso registrado" : $"Último acesso {Fuso.Formatar(usuario.UltimoAcessoEm)}";

    private static string? ClasseDaLinha(UsuarioResumo usuario) => usuario.Ativo ? null : "aj-tabela__linha--inativa";

    private static string ClasseDoCartao(UsuarioResumo usuario) => usuario.Ativo ? "aj-cartao" : "aj-cartao aj-cartao--inativo";
```

- [ ] **Step 3: Trava e testes**

Acrescente `Components/Pages/Usuarios.razor` a `Migradas`.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~UsuariosWebTests|FullyQualifiedName~PaginasProtegidasTests|FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~LayoutWebTests|FullyQualifiedName~RegistroDeAcessosWebTests|FullyQualifiedName~SessaoWebTests|FullyQualifiedName~AutenticacaoWebTests"`
Expected: 0 avisos; todos passando.

- [ ] **Step 4: Commit**

```
git add src tests
git commit -m "feat(web): lista de usuários no design system, com cartões no tablet e no mobile, novo usuário no cabeçalho e o recado do formulário"
```

---

### Task 7: Produtores

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Produtores.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Produtores/PainelDeProdutividade.razor`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/ProdutoresWebTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs` (sai `/produtores` da theory)
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: `CabecalhoDePagina`, `EstadoDaLista`, `Icone` (`avancar`), `Contagem.De`, `BlocoDeVisao.De`; `ListarUsuarios`, `ObterProdutividadeDoProdutor`, `RotulosDeProdutividade`, `FichaDaAtividade` (marcação antiga, que migra na 9E), que não mudam.
- Produces: `<PainelDeProdutividade ProdutorId ProdutorNome OnAcessoEncerrado SeletorDoProdutor? />`: `SeletorDoProdutor` (`RenderFragment?`) é o campo "Produtor" que a página entrega para a linha de filtros do tablet e do mobile. `OnFechar` sai.

**Regras:**
- Quadros: `Produtores.dc.html`, `ProdutoresTablet.dc.html`, `ProdutoresMobile.dc.html`.
- O produtor escolhido vem do endereço: `[SupplyParameterFromQuery(Name = "produtor")] public string? ProdutorNoEndereco` lido com `Guid.TryParse` (texto estranho não derruba a página). Só vale quem está na lista de produtores (perfil de Produção hoje, ativo ou não). Sem escolha válida não há painel e não há consulta de produtividade.
- Desktop: lado a lado com o painel largo (`aj-lado-a-lado--painel-largo`). A linha do produtor escolhido é `aj-tabela__linha--selecionada` e a célula de ações diz "Em acompanhamento"; as outras têm "Acompanhar", link para `/produtores?produtor={id}`. Linha de produtor inativo: `aj-tabela__linha--inativa`.
- Tablet e mobile: só a linha de filtros (campo "Produtor" e, com escolha, campo "Mês") e o painel. O campo "Produtor" lista todos os produtores (`{nome} (inativo)` para os inativos), com a primeira opção "Escolha o produtor" quando não há escolha; escolher navega para `/produtores?produtor={id}`.
- O painel: cabeçalho com o título e, no desktop, o campo "Mês"; quatro blocos de total; um grupo por complexidade com `h3`, a tabela plana (desktop) ou as linhas encostadas (tablet e mobile), e "Nenhuma atividade." no grupo vazio; falha com "Tentar de novo" no lugar dos números (`EstadoDaLista`). "Abrir" (botão na tabela, linha inteira no tablet e no mobile) abre a ficha da atividade dentro do painel, depois dos grupos, como hoje.
- O seletor de mês é o mesmo controle nas duas posições (um `RenderFragment` do componente, desenhado no cabeçalho do painel para o desktop e na linha de filtros para o tablet e o mobile).
- Lista vazia: "Nenhum produtor.".

- [ ] **Step 1: Testes (devem falhar)**

`Diretor_ve_so_quem_e_de_producao_em_ordem_de_nome_com_os_inativos_marcados` passa a:

```csharp
        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/produtores"));

        html.Should().Contain("<p class=\"aj-subtitulo\">2 produtores</p>").And.NotContain("Acesso negado");
        var tabela = BlocoDeVisao.De(html, "aj-so-desktop");
        // As linhas, em ordem de nome: nome, perfil, OAB e situação. A diretora não está entre elas.
        Regex.Matches(tabela, "<tr[^>]*>\\s*<td class=\"aj-tabela__principal\">([^<]+)</td>\\s*<td>([^<]+)</td>\\s*<td class=\"aj-tabela__data\">([^<]+)</td>\\s*<td>([^<]+)</td>")
            .Select(m => $"{m.Groups[1].Value}|{m.Groups[2].Value}|{m.Groups[3].Value}|{m.Groups[4].Value}")
            .Should().Equal("Iara Inativa|Produção|—|Inativo", "Pedro Produção|Produção|—|Ativo");
        Regex.Matches(tabela, "<tr class=\"aj-tabela__linha--inativa\"").Count.Should().Be(1);
        tabela.Should().MatchRegex($"<a href=\"/produtores\\?produtor={_pedro}\" class=\"aj-botao aj-botao--fantasma aj-botao--seta\">Acompanhar\\s*<svg");
        // Sem produtor no endereço não há painel: a consulta de produtividade só acontece por escolha.
        html.Should().NotContain("Produtividade —").And.NotContain("Em acompanhamento");
        BlocoDeVisao.De(html, "aj-tablet-mobile").Should().MatchRegex("<select class=\"aj-campo-filtro__controle\"[^>]*>\\s*<option value=\"\">Escolha o produtor</option>")
            .And.Contain(">Iara Inativa (inativo)</option>");
```

`Producao_recebe_acesso_negado_e_nao_ve_a_lista`: "Acompanhar produtividade" vira "Acompanhar"; acrescente a positiva `Contain("<h1 class=\"aj-titulo\">Produtores</h1>")`.

Testes novos, na mesma classe:

```csharp
    [Fact]
    public async Task Produtor_no_endereco_abre_o_painel_e_marca_a_linha()
    {
        await ConcluidaAgoraAsync("Petição simples", 1);
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync($"/produtores?produtor={_pedro}"));

        html.Should().MatchRegex("<div class=\"aj-lado-a-lado aj-lado-a-lado--painel-largo\">")
            .And.MatchRegex("<section class=\"aj-painel aj-painel--principal aj-lado-a-lado__painel\">")
            .And.Contain("<h2>Produtividade — Pedro Produção</h2>");
        BlocoDeVisao.De(html, "aj-so-desktop").Should()
            .MatchRegex("(?s)<tr class=\"aj-tabela__linha--selecionada\">.*?Pedro Produção.*?<span class=\"aj-tabela__marca\">Em acompanhamento</span>");
        BlocoDeVisao.De(html, "aj-tablet-mobile").Should()
            .MatchRegex($"<option value=\"{_pedro}\" selected[^>]*>Pedro Produção</option>")
            .And.NotContain("Escolha o produtor");
    }

    [Theory]
    [InlineData("lixo")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("admin")]
    public async Task Produtor_que_nao_vale_no_endereco_nao_abre_painel_nem_consulta(string valor)
    {
        var produtor = valor == "admin" ? _admin.ToString() : valor;
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync($"/produtores?produtor={produtor}"));

        html.Should().Contain("<p class=\"aj-subtitulo\">1 produtor</p>").And.Contain("Pedro Produção");
        html.Should().NotContain("Produtividade —").And.NotContain("Em acompanhamento");
        var acessos = await _app.EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking().CountAsync(a => a.TipoRecurso == "Produtividade"));
        acessos.Should().Be(0);
    }
```

Os testes do painel passam a ler a marcação nova, sem mudar o que provam:

- O mês escolhido e a lista de meses vêm do campo do desktop: `Meses(BlocoDeVisao.De(html, "aj-so-desktop"))` e o `selected` no mesmo bloco (o mesmo controle aparece também no bloco do tablet e do mobile; acrescente a afirmação de que ele traz as mesmas opções).
- Os totais: `Textos(html, "<div class=\"aj-bloco[^\"]*\">\\s*<span class=\"aj-ajuda\">(Complexidade \\d|Total)</span>")` em ordem `Complexidade 1`, `Complexidade 2`, `Complexidade 3`, `Total`; os números, `Textos(html, "<span class=\"aj-ajuda\">(?:Complexidade \\d|Total)</span>\\s*<b>(\\d+)</b>")`; e o total com `aj-bloco aj-bloco--destaque`.
- Os grupos: `Textos(html, "<h3>\\s*([^<]*?)\\s*</h3>")` como hoje.
- A atividade no grupo: `<td class="aj-tabela__principal">Petição simples</td>` no bloco do desktop e `<span class="aj-linha__titulo">Petição simples</span>` no bloco do tablet e do mobile.
- "Nenhuma atividade." continua contando um por grupo vazio (escrito uma vez só por grupo, fora dos blocos de visão).
- O de quem não é produtor: "Produtor não encontrado." e "Tentar de novo" num aviso de erro com ações; sem `aj-blocos`.

Em `LayoutWebTests`, sai `[InlineData("/produtores")]` da theory das telas ainda não migradas.

Run: `dotnet build -m:1` e `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ProdutoresWebTests|FullyQualifiedName~RotulosDeProdutividadeTests"`
Expected: os testes da página e do painel falham.

- [ ] **Step 2: A página**

```razor
@page "/produtores"
@inject IDespachante Despachante
@inject NavigationManager Navegacao

<PageTitle>Produtores</PageTitle>

<CabecalhoDePagina Titulo="Produtores" Subtitulo="@Subtitulo" />

@if (!_carregado)
{
    <p class="aj-subtitulo" role="status">Carregando…</p>
}
else if (!_autorizado)
{
    <Aviso Tipo="TipoDeAviso.Erro">Acesso negado.</Aviso>
}
else if (_produtores.Count == 0)
{
    <EstadoDaLista Vazio="true" TextoDeVazio="Nenhum produtor." />
}
else
{
    <div class="@(_escolhido is null ? "aj-lado-a-lado" : "aj-lado-a-lado aj-lado-a-lado--painel-largo")">
        <div class="aj-so-desktop">
            <div class="aj-lado-a-lado__lista">
                <div class="aj-tabela-caixa">
                    <table class="aj-tabela">
                        <thead><tr><th scope="col">Nome</th><th scope="col">Perfil</th><th scope="col">OAB</th><th scope="col">Situação</th><th scope="col"><span class="aj-so-leitor">Ações</span></th></tr></thead>
                        <tbody>
                            @foreach (var produtor in _produtores)
                            {
                                <tr class="@ClasseDaLinha(produtor)" @key="produtor.Id">
                                    <td class="aj-tabela__principal">@produtor.Nome</td>
                                    <td>@produtor.PerfilNome</td>
                                    <td class="aj-tabela__data">@Ou(produtor.NumeroOab)</td>
                                    <td>@(produtor.Ativo ? "Ativo" : "Inativo")</td>
                                    <td class="aj-tabela__acao">
                                        @if (produtor.Id == _escolhido?.Id)
                                        {
                                            <span class="aj-tabela__marca">Em acompanhamento</span>
                                        }
                                        else
                                        {
                                            <a href="@Endereco(produtor)" class="aj-botao aj-botao--fantasma aj-botao--seta">Acompanhar<Icone Nome="avancar" /></a>
                                        }
                                    </td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            </div>
        </div>

        @if (_escolhido is { } escolhido)
        {
            <PainelDeProdutividade @key="escolhido.Id" ProdutorId="escolhido.Id" ProdutorNome="@escolhido.Nome"
                                   OnAcessoEncerrado="EncerrarAcesso" SeletorDoProdutor="SeletorDoProdutor" />
        }
        else
        {
            <div class="aj-tablet-mobile">
                <div class="aj-linha-filtros aj-linha-filtros--empilha">@SeletorDoProdutor</div>
            </div>
        }
    </div>
}
```

O seletor, no `@code`:

```csharp
    private RenderFragment SeletorDoProdutor => @<label class="aj-campo-filtro aj-campo-filtro--largo">
        <span>Produtor</span>
        <select class="aj-campo-filtro__controle" value="@(_escolhido?.Id.ToString() ?? string.Empty)" @onchange="Escolher">
            @if (_escolhido is null)
            {
                <option value="">Escolha o produtor</option>
            }
            @foreach (var produtor in _produtores)
            {
                <option value="@produtor.Id">@NomeNoSeletor(produtor)</option>
            }
        </select>
    </label>;
```

(Se o Razor não aceitar o modelo dentro da propriedade, use um método que devolve o `RenderFragment` ou um componente pequeno na pasta `Produtores`, e registre.)

Mais no `@code`: `_produtores` e a regra de hoje (perfil de Produção, em ordem de nome, inativos incluídos, consulta só depois de conferir a Diretoria); `OnParametersSet` resolve `_escolhido` a partir de `ProdutorNoEndereco` (com `Guid.TryParse` e a lista); `Escolher(ChangeEventArgs e)` navega para `/produtores?produtor={id}` quando o valor é um produtor da lista; `Subtitulo` = `Contagem.De(_produtores.Count, "produtor", "produtores")` com a lista carregada e não vazia; `EncerrarAcesso` como hoje; `ClasseDaLinha` junta `aj-tabela__linha--inativa` e `aj-tabela__linha--selecionada` conforme o caso (ou nulo); `NomeNoSeletor` = `{nome} (inativo)` para os inativos.

- [ ] **Step 3: O painel**

```razor
@inject IDespachante Despachante
@inject FusoDoEscritorio Fuso

<div class="aj-tablet-mobile">
    <div class="aj-linha-filtros aj-linha-filtros--empilha">
        @SeletorDoProdutor
        <label class="aj-campo-filtro"><span>Mês</span>@SeletorDoMes</label>
    </div>
</div>

<section class="aj-painel aj-painel--principal aj-lado-a-lado__painel">
    <div class="aj-painel__cabecalho">
        <h2>@RotulosDeProdutividade.Titulo(ProdutorNome)</h2>
        <div class="aj-so-desktop"><label class="aj-campo-filtro"><span>Mês</span>@SeletorDoMes</label></div>
    </div>

    <EstadoDaLista Erro="@_erro" Carregando="@(_produtividade is null)" TextoDeVazio="Nenhuma atividade." TentarDeNovo="TentarDeNovoAsync">
        @if (_produtividade is { } p)
        {
            <div class="aj-blocos">
                @foreach (var grupo in p.Grupos)
                {
                    <div class="aj-bloco"><span class="aj-ajuda">@RotulosDeProdutividade.Coluna(grupo.Complexidade)</span><b>@grupo.Total</b></div>
                }
                <div class="aj-bloco aj-bloco--destaque"><span class="aj-ajuda">Total</span><b>@p.Total</b></div>
            </div>

            @foreach (var grupo in p.Grupos)
            {
                <h3>@RotulosDeProdutividade.Grupo(grupo)</h3>
                @if (grupo.Total == 0)
                {
                    <span class="aj-ajuda">Nenhuma atividade.</span>
                }
                else
                {
                    <div class="aj-so-desktop">
                        <div class="aj-tabela-caixa aj-tabela-caixa--plana">
                            <table class="aj-tabela">
                                <thead><tr><th scope="col">Título</th><th scope="col">Cliente</th><th scope="col">Concluída em</th><th scope="col"><span class="aj-so-leitor">Ações</span></th></tr></thead>
                                <tbody>
                                    @foreach (var atividade in grupo.Atividades)
                                    {
                                        <tr @key="atividade.Id">
                                            <td class="aj-tabela__principal">@atividade.Titulo</td>
                                            <td>@(atividade.ClienteNome ?? "—")</td>
                                            <td class="aj-tabela__data">@Fuso.Formatar(atividade.ConcluidaEm)</td>
                                            <td class="aj-tabela__acao"><button type="button" class="aj-botao aj-botao--fantasma aj-botao--seta" @onclick="() => Abrir(atividade.Id)">Abrir<Icone Nome="avancar" /></button></td>
                                        </tr>
                                    }
                                </tbody>
                            </table>
                        </div>
                    </div>
                    <div class="aj-tablet-mobile">
                        <div class="aj-linhas">
                            @foreach (var atividade in grupo.Atividades)
                            {
                                <button type="button" class="aj-linha aj-linha--botao" @key="atividade.Id" @onclick="() => Abrir(atividade.Id)"><span class="aj-linha__texto"><span class="aj-linha__titulo">@atividade.Titulo</span><span class="aj-ajuda">@Detalhe(atividade)</span></span><Icone Nome="avancar" Classe="aj-cartao__seta" /></button>
                            }
                        </div>
                    </div>
                }
            }

            @if (_atividadeAberta is { } id)
            {
                @* A ficha da atividade (marcação antiga até a 9E), como hoje. O que mudar nela muda os números. *@
                <FichaDaAtividade @key="id" AtividadeId="id" OnMudou="RecarregarAsync" OnAcessoEncerrado="EncerrarAcessoAsync"
                                  OnFechar="() => FecharFicha(id)" />
            }
        }
    </EstadoDaLista>
</section>
```

No `@code`: o que existe hoje (meses, geração da consulta, descarte de resposta atrasada, mês só da lista oferecida), menos `OnFechar` e `FecharAsync`; mais `[Parameter] public RenderFragment? SeletorDoProdutor { get; set; }`, o `SeletorDoMes` (o `select` de hoje, com `class="aj-campo-filtro__controle"`, num `RenderFragment`) e:

```csharp
    private string Detalhe(AtividadeRealizada atividade) =>
        $"{atividade.ClienteNome ?? "Sem cliente"} · concluída em {Fuso.Formatar(atividade.ConcluidaEm)}";
```

(`AtividadeRealizada(Id, Titulo, Complexidade, ConcluidaEm, ClienteNome)` é o item de `GrupoDeComplexidade.Atividades`, em `AtividadesJuridicas.Application.Atividades`.)

- [ ] **Step 4: Trava e testes**

Acrescente a `Migradas`: `Components/Pages/Produtores.razor` e `Components/Produtores/PainelDeProdutividade.razor`.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ProdutoresWebTests|FullyQualifiedName~RotulosDeProdutividadeTests|FullyQualifiedName~LayoutWebTests|FullyQualifiedName~PortalDoClienteWebTests"`
Expected: 0 avisos; todos passando.

- [ ] **Step 5: Commit**

```
git add src tests
git commit -m "feat(web): produtores no design system, com o produtor no endereço, a linha em acompanhamento e a produtividade em blocos e grupos"
```

---

### Task 8: Auditoria

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Auditoria.razor`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/AuditoriaWebTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs` (sai `/auditoria` da theory)
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: `CabecalhoDePagina` (com `Complemento` e `LinhaDoMobile`), `Aviso`, `EstadoDaLista`, `Paginacao`, `Icone` (`filtros`), `ResumoDeAuditoria`, `IdentificadorCurto`, `Contagem.De`, `FiltroDaTelaDeAuditoria`; `ConsultarAuditoria`, `ConsultarAcessos`, `ListarUsuarios`, que não mudam.
- Produces: nada que outra tarefa consuma.

**Regras:**
- Quadros: `Auditoria.dc.html`, `AuditoriaTablet.dc.html`, `AuditoriaMobile.dc.html`; a aba "Consultas" por analogia (spec 8).
- O controle segmentado ("O que consultar") fica no `Complemento` do cabeçalho (desktop e tablet, `aj-segmentado aj-segmentado--recuado`) e na `LinhaDoMobile` (o mesmo controle). Cada opção é `<button type="button" class="aj-segmentado__opcao" aria-pressed="…">`. Aparece só para quem pode consultar.
- Desktop: a barra de filtros (`form.aj-barra-filtros`, `role="search"`), na ordem do quadro: De, Até, Usuário, Operação, Entidade, Registro, Gravação, Filtrar. Na aba Consultas: De, Até, Usuário, Ação, Recurso, Filtrar. O Enter num campo filtra (cada grupo de filtros é um `form` com `@onsubmit` e `@onsubmit:preventDefault`; os campos ligam ao mesmo `FiltroDaTelaDeAuditoria`).
- Tablet e mobile: a linha (`form.aj-linha-filtros`, `role="search"`) com De, Até e o botão que abre o painel de filtros: no tablet, `Usuário, operação` (na aba Consultas, `Usuário, ação`), com o ícone `filtros`; no mobile, o botão de ícone `Mais filtros`. O botão tem `aria-expanded` e `aria-controls="filtros-da-auditoria"`. O painel (`form#filtros-da-auditoria.aj-painel.aj-painel--filtros`) traz os campos da barra do desktop que não estão na linha, em uma coluna, e `aj-painel__acoes` com `Limpar` (fantasma; esvazia os campos do painel) e `Filtrar` (primário; consulta e fecha o painel).
- A lista: tabela no desktop; cartões (`aj-grade` de `aj-painel aj-painel--registro`) no tablet e no mobile; a paginação, uma só, sempre à vista, depois dos dois (`Paginacao` com `SempreVisivel`). Falha da consulta: `EstadoDaLista` com "Tentar de novo"; nenhum registro: "Nenhum registro.". Identificador inválido: o aviso de erro "Identificador inválido." no lugar da lista, sem consulta (a regra de hoje: as linhas anteriores saem e a página volta à primeira).
- Alterações: "Quem" segue a regra de hoje (`UsuarioNome`, ou "Sistema" sem usuário, ou "—"); "Operação" também (`Evento` ou o rótulo da operação). "Sobre" é a entidade e, havendo registro, o botão com o identificador curto (`title="Ver tudo deste registro"`); "Dados", o resumo; "IP", o IP ou "—"; "Correlação", o botão com o identificador curto (`title="Ver o que foi gravado junto"`). Os botões filtram como hoje e ficam desabilitados durante uma consulta.
- Cartão de alteração: título `{operação} · {entidade}`; ajuda `{quem} · {quando}` e ` · IP {ip}` quando houver (com "Usuário não identificado" no lugar de "—"); o resumo dos dados só quando há dados; a faixa `aj-registro__ids` com `Registro {curto}` e `Gravação {curto}`, cada um quando houver.
- Consultas: tabela com Quando, Quem, Ação (principal), Recurso (o tipo e, havendo, o identificador inteiro em `code.aj-dados`), Detalhe (`code.aj-dados`, "—" sem detalhe), IP. Cartão: título `{ação} · {recurso}`, a mesma ajuda, o detalhe e o identificador inteiro, quando houver.
- Subtítulo: `{n} registros no período` com o total da consulta feita; sem consulta (carregando, falha, identificador inválido), sem subtítulo.

- [ ] **Step 1: Testes (devem falhar)**

`AuditoriaWebTests`:

```csharp
    [Fact]
    public async Task Diretor_ve_os_registros_sem_nenhum_hash_de_senha()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/auditoria"));

        html.Should().MatchRegex("<p class=\"aj-subtitulo\">\\d+ registros? no período</p>")
            .And.Contain("login_sucesso").And.Contain("pedro@x.com").And.Contain("Diana Diretora")
            .And.MatchRegex("<span class=\"aj-subtitulo\">Página 1 de \\d+ · \\d+ registros?</span>");
        html.Should().NotContain("pbkdf2-sha256");

        var tabela = BlocoDeVisao.De(html, "aj-so-desktop");
        tabela.Should().Contain("<th scope=\"col\">Correlação</th>")
            .And.MatchRegex("<code class=\"aj-dados\">[^<]*: pedro@x.com[^<]*</code>")
            .And.MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-identificador\" title=\"Ver tudo deste registro\"[^>]*>[0-9a-f]{8}…</button>");
        // Dados resumidos: nada de JSON cru na tela.
        tabela.Should().NotContain("{\"");

        var cartoes = BlocoDeVisao.De(html, "aj-tablet-mobile");
        cartoes.Should().MatchRegex("<div class=\"aj-painel aj-painel--registro\">\\s*<span class=\"aj-registro__titulo\">Criação · Usuario</span>")
            .And.MatchRegex(">Gravação [0-9a-f]{8}…</button>");
    }

    [Fact]
    public async Task Diretor_ve_o_controle_segmentado_e_os_filtros_de_cada_visao()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/auditoria"));

        // O mesmo controle no cabeçalho (desktop e tablet) e na barra do mobile.
        Regex.Matches(html, "<div class=\"aj-segmentado aj-segmentado--recuado\" role=\"group\" aria-label=\"O que consultar\">").Count.Should().Be(2);
        html.Should().MatchRegex("<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"true\"[^>]*>Alterações e eventos</button>");

        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex("<form[^>]*class=\"aj-barra-filtros\"[^>]*role=\"search\"")
            .And.Contain("placeholder=\"Identificador do registro\"").And.Contain("placeholder=\"Identificador de correlação\"")
            .And.Contain("placeholder=\"Usuario, Perfil…\"")
            .And.MatchRegex("<button type=\"submit\" class=\"aj-botao aj-botao--secundario\"[^>]*>Filtrar</button>");
        // A ordem do quadro: Operação vem antes de Entidade.
        desktop.IndexOf("<span>Operação</span>", StringComparison.Ordinal).Should().BeLessThan(desktop.IndexOf("<span>Entidade</span>", StringComparison.Ordinal));

        var linha = BlocoDeVisao.De(html, "aj-tablet-mobile");
        linha.Should().MatchRegex("<form[^>]*class=\"aj-linha-filtros\"[^>]*role=\"search\"")
            .And.Contain("aria-controls=\"filtros-da-auditoria\"").And.Contain("aria-expanded=\"false\"");
        BlocoDeVisao.De(html, "aj-so-tablet").Should().MatchRegex("(?s)<button type=\"button\" class=\"aj-botao aj-botao--secundario aj-botao--com-icone\"[^>]*>.*?Usuário, operação</button>");
        BlocoDeVisao.De(html, "aj-so-mobile").Should().Contain("aria-label=\"Mais filtros\"");
        // O painel de filtros só aparece aberto.
        html.Should().NotContain("aj-painel--filtros").And.NotContain("Identificador inválido.");
    }
```

Os demais (`Producao_recebe_acesso_negado_e_nao_ve_registros`, menu, anônimo) continuam; o de Produção ganha a positiva `Contain("<h1 class=\"aj-titulo\">Auditoria</h1>")` e `NotContain("aj-segmentado")`.

Em `LayoutWebTests`, sai `[InlineData("/auditoria")]` da theory das telas ainda não migradas.

Run: `dotnet build -m:1` e `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AuditoriaWebTests"`
Expected: os dois testes reescritos falham.

- [ ] **Step 2: A tela**

Estrutura, de cima para baixo:

```razor
<CabecalhoDePagina Titulo="Auditoria" Subtitulo="@Subtitulo">
    <Complemento>@if (_autorizado) { @Segmentado }</Complemento>
    <LinhaDoMobile>@if (_autorizado) { @Segmentado }</LinhaDoMobile>
</CabecalhoDePagina>
```

Carregando, "Acesso negado." e, autorizado: a barra do desktop (`<div class="aj-so-desktop"><form class="aj-barra-filtros" role="search" …>`), a linha e o painel do tablet e do mobile (`<div class="aj-tablet-mobile">…</div>`), o aviso de identificador inválido ou o `EstadoDaLista` com a tabela (`aj-so-desktop`), os cartões (`aj-tablet-mobile`) e a `Paginacao`.

Campos de filtro, cada um assim (com o modificador de largura do quadro):

```razor
<label class="aj-campo-filtro aj-campo-filtro--data"><span>De</span><input type="date" class="aj-campo-filtro__controle" @bind="_filtro.De" /></label>
<label class="aj-campo-filtro"><span>Usuário</span><select class="aj-campo-filtro__controle" @bind="_filtro.UsuarioId"><option value="">Todos</option>@foreach (var usuario in _usuarios) { <option value="@usuario.Id">@usuario.Nome</option> }</select></label>
<label class="aj-campo-filtro aj-campo-filtro--texto"><span>Entidade</span><input type="text" class="aj-campo-filtro__controle" placeholder="Usuario, Perfil…" @bind="_filtro.Entidade" /></label>
<label class="aj-campo-filtro aj-campo-filtro--identificador"><span>Registro</span><input type="text" class="aj-campo-filtro__controle" placeholder="Identificador do registro" @bind="_filtro.EntidadeId" /></label>
```

(Na barra do desktop, De e Até levam `aj-campo-filtro--data`; Usuário e Operação, só `aj-campo-filtro`; Entidade, `--texto`; Registro e Gravação, `--identificador`. No painel, os mesmos campos sem modificador de largura.)

Linha da tabela de alterações:

```razor
<tr @key="registro.Id">
    <td class="aj-tabela__data">@Fuso.Formatar(registro.DataHora)</td>
    <td>@Quem(registro)</td>
    <td class="aj-tabela__principal">@Operacao(registro)</td>
    <td class="aj-tabela__sem-quebra">@registro.Entidade @if (registro.EntidadeId is { } entidadeId)
    {
        <button type="button" class="aj-botao aj-botao--fantasma aj-identificador" title="Ver tudo deste registro" disabled="@_ocupado" @onclick="() => FiltrarPorRegistroAsync(entidadeId)">@IdentificadorCurto.De(entidadeId)</button>
    }</td>
    <td><code class="aj-dados">@ResumoDeAuditoria.De(registro.Dados, Fuso)</code></td>
    <td class="aj-tabela__data">@Ou(registro.Ip)</td>
    <td>@if (registro.IdCorrelacao is { } correlacao)
    {
        <button type="button" class="aj-botao aj-botao--fantasma aj-identificador" title="Ver o que foi gravado junto" disabled="@_ocupado" @onclick="() => FiltrarPorCorrelacaoAsync(correlacao)">@IdentificadorCurto.De(correlacao)</button>
    }</td>
</tr>
```

Cartão de alteração:

```razor
<div class="aj-painel aj-painel--registro" @key="registro.Id">
    <span class="aj-registro__titulo">@($"{Operacao(registro)} · {registro.Entidade}")</span>
    <span class="aj-ajuda">@Meta(Quem(registro), registro.DataHora, registro.Ip)</span>
    @if (!string.IsNullOrWhiteSpace(registro.Dados))
    {
        <code class="aj-dados">@ResumoDeAuditoria.De(registro.Dados, Fuso)</code>
    }
    @if (registro.EntidadeId is not null || registro.IdCorrelacao is not null)
    {
        <span class="aj-registro__ids">
            @if (registro.EntidadeId is { } entidadeId)
            {
                <button type="button" class="aj-botao aj-botao--fantasma aj-identificador" title="Ver tudo deste registro" disabled="@_ocupado" @onclick="() => FiltrarPorRegistroAsync(entidadeId)">@($"Registro {IdentificadorCurto.De(entidadeId)}")</button>
            }
            @if (registro.IdCorrelacao is { } correlacao)
            {
                <button type="button" class="aj-botao aj-botao--fantasma aj-identificador" title="Ver o que foi gravado junto" disabled="@_ocupado" @onclick="() => FiltrarPorCorrelacaoAsync(correlacao)">@($"Gravação {IdentificadorCurto.De(correlacao)}")</button>
            }
        </span>
    }
</div>
```

Os botões que abrem o painel de filtros:

```razor
<div class="aj-so-tablet"><button type="button" class="aj-botao aj-botao--secundario aj-botao--com-icone" aria-expanded="@(_filtrosAbertos ? "true" : "false")" aria-controls="filtros-da-auditoria" @onclick="AlternarFiltros"><Icone Nome="filtros" />@RotuloDosFiltros</button></div>
<div class="aj-so-mobile"><button type="button" class="aj-botao aj-botao--secundario aj-botao--icone" aria-label="Mais filtros" aria-expanded="@(_filtrosAbertos ? "true" : "false")" aria-controls="filtros-da-auditoria" @onclick="AlternarFiltros"><Icone Nome="filtros" /></button></div>
```

No `@code`: tudo o que existe hoje (abas, filtro, página, guarda de consulta em andamento, leitura dos identificadores, período em dias do escritório), mais:

```csharp
    private bool _filtrosAbertos;
    private bool _identificadorInvalido;

    private string? Subtitulo => _carregado && _autorizado && !_identificadorInvalido && _erro is null && _consultou
        ? $"{Contagem.De(Total, "registro", "registros")} no período"
        : null;

    private string RotuloDosFiltros => _aba == Aba.Alteracoes ? "Usuário, operação" : "Usuário, ação";

    private string Pressionado(Aba aba) => _aba == aba ? "true" : "false";

    private static string Quem(RegistroAuditoriaResumo registro) =>
        registro.UsuarioNome ?? (registro.UsuarioId is null ? "Sistema" : "—");

    private string Meta(string quem, DateTime quando, string? ip) =>
        $"{(quem == "—" ? "Usuário não identificado" : quem)} · {Fuso.Formatar(quando)}{(string.IsNullOrWhiteSpace(ip) ? "" : $" · IP {ip}")}";

    private static string Ou(string? valor) => string.IsNullOrWhiteSpace(valor) ? "—" : valor;
```

`Segmentado` é um `RenderFragment` com o `div.aj-segmentado.aj-segmentado--recuado` (`role="group"`, `aria-label="O que consultar"`) e os dois botões (`TrocarAbaAsync`). `_consultou` marca que uma consulta respondeu (para o subtítulo não aparecer antes). A mensagem "Identificador inválido." deixa de sair do `_erro`: passa a `_identificadorInvalido`, e `_erro` fica só para a falha da consulta. `LimparFiltros` esvazia os campos do painel da aba atual; `FiltrarDoPainelAsync` fecha o painel e consulta a primeira página.

- [ ] **Step 3: Trava e testes**

Acrescente `Components/Pages/Auditoria.razor` a `Migradas`.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AuditoriaWebTests|FullyQualifiedName~FiltroDaTelaDeAuditoriaTests|FullyQualifiedName~ResumoDeAuditoriaTests|FullyQualifiedName~LayoutWebTests|FullyQualifiedName~PortalDoClienteWebTests"`
Expected: 0 avisos; todos passando. A theory das telas ainda não migradas termina com `/atividades`, `/agenda`, `/clientes` e `/processos`.

- [ ] **Step 4: Commit**

```
git add src tests
git commit -m "feat(web): auditoria no design system, com o controle segmentado no cabeçalho, filtros em painel no tablet e no mobile e dados resumidos"
```

---

### Task 9: Suíte inteira e conferência no navegador

Feita pela sessão que coordena.

**Files:**
- Modify: `docs/superpowers/specs/2026-10-10-migracao-para-o-design-system-design.md` (o que a execução ensinou)

**Regras:**
- A conferência usa o ambiente descartável e a ferramenta da 9B (`conferencia-de-telas\`), como registrado na seção 11 da spec: banco em contêiner na porta 5599, aplicação publicada em Release em `http://localhost:5197`, servidor dos quadros na 5198, credenciais de teste num arquivo local. O ambiente sobe depois da suíte e é derrubado antes de qualquer nova rodada dela.
- Precisa das ferramentas do Chrome (Claude in Chrome). Se não estiverem ligadas na sessão, peça ao usuário.

- [ ] **Step 1: Suíte inteira** — `dotnet build -m:1` e o `suite-em-lotes.ps1`. Esperado: sem avisos; os totais da linha de base mais os testes novos, nenhum falhando.

- [ ] **Step 2: Subir o ambiente e criar os dados** — pelo sistema: cinco áreas (uma inativa), perfis como no quadro (um inativo), sete usuários (um de cliente com cliente, um inativo, um de cliente com cliente inativo, nomes e e-mails longos), atividades concluídas de um produtor em três complexidades (com e sem cliente), e registros de auditoria variados (os próprios cadastros geram).

- [ ] **Step 3: Conferir cada tela nas três larguras, contra o quadro**

| Tela | Rota | Estados |
|---|---|---|
| Áreas | `/areas` | lista com inativa; renomear (pela tabela e pelo menu); erro de nome vazio |
| Perfis | `/perfis` | lista com inativo; renomear o perfil de cliente (tipo travado) |
| Usuários | `/usuarios` | lista; confirmação de inativação no topo; recado depois de salvar |
| Formulário | `/usuarios/novo`, `/usuarios/{id}/editar` | equipe e cliente; busca de cliente com resultados e com nenhum; erro de cliente obrigatório; situação na edição (tablet e mobile) |
| Produtores | `/produtores`, `/produtores?produtor={id}` | sem escolha; com escolha; troca de mês; abrir uma atividade |
| Auditoria | `/auditoria` | as duas abas; painel de filtros aberto; filtrar por registro e por gravação; identificador inválido |

Para cada uma: estrutura, textos, medidas dos blocos, cores, ícones e o que está aceso na navegação ("Mais" no trilho e na barra inferior).

- [ ] **Step 4: Conferir o que o Review Focus pede e o que não tem teste**

- Nome e e-mail longos (com e sem espaços) nos cartões de usuário e nas linhas de área e perfil a 390px: quebram dentro do bloco; a página não rola para o lado.
- Menu de ações de Áreas e Perfis com teclado de verdade: fechado, Tab não entra nos itens; Esc e tocar fora devolvem o foco ao botão.
- "Renomear" pelo menu, no tablet e no mobile: o foco vai ao campo "Nome" e a tela rola até ele.
- Painel de filtros da auditoria com teclado: abre, Tab percorre os campos, "Filtrar" fecha e consulta, Enter num campo da linha filtra.
- Formulário de usuário no tablet: o respiro do conteúdo com a barra fixa (16px em cima, 110px embaixo) e a barra cobrindo a barra inferior; no mobile, a barra de voltar e a barra fixa.
- Larguras de 760, 761, 1199 e 1200px em `/areas` e `/produtores?produtor={id}`: um só arranjo à vista (lado a lado ou empilhado), um só título.
- Regras de elemento do `app.css` que alcançam o que esta fase passou a usar: medir com e sem o `app.css` em cada tela (o roteiro `V.vazamento`). O que vazar é restringido lá às telas antigas (regra 2 da 9B) ou ganha a declaração que falta no design system.
- Telas ainda não migradas (`/atividades`, `/clientes`, `/agenda`, `/processos`): aparência igual à da `main`.

- [ ] **Step 5: Corrigir o que divergir** — CSS no design system (Tarefa 1 de novo: publicar e copiar), nunca no `app.css` nem com `style`. Marcação na tela, com o teste ajustado antes.

- [ ] **Step 6: Derrubar o ambiente, atualizar a spec e fechar** — remova o contêiner, a pasta publicada e as credenciais; rode a suíte inteira de novo se houve correção; emende a spec (seção 15 ganha "Da fase 9C"). Commit:

```
git add docs
git commit -m "docs(spec): o que a fase 9C ensinou sobre a migração para o design system"
```

Mostre ao usuário as capturas das telas nas três larguras e peça o aval para a entrada na `main`.
