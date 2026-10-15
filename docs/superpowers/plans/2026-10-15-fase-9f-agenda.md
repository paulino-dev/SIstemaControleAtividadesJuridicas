# Fase 9F — Agenda no design system: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A agenda (Dia, Semana e Mês), a ficha do evento e o formulário de novo e de edição ficam idênticos aos quadros do canvas nas três visões, cada tela com rota própria, e o período da agenda passa a morar no endereço.

**Architecture:** Cada padrão que os quadros da fase trazem como estilo embutido vira classe `aj-` no artifact do design system e é copiado para `wwwroot/css` (Tarefa 1). A lógica de calendário (período no endereço, intervalos, navegação, régua de horas, eventos simultâneos, rótulos e as três redações do aviso de conflito) fica em classes puras testadas (Tarefa 2). A agenda é uma página que lê `?visao=` e `?data=`, consulta o período uma vez e entrega os mesmos eventos a três componentes de visão (`SemanaDaAgenda`, `DiaDaAgenda`, `MesDaAgenda`), cada um emitindo a marcação das três larguras. A ficha e o formulário do evento, hoje painéis dentro da agenda, viram telas com rota, e os componentes antigos saem. A régua de horas posiciona os cartões por classes de grade (quartos de hora), sem atributo `style`.

**Tech Stack:** .NET 10, Blazor Interactive Server, CSS do design system (tokens e classes `aj-`), xUnit 2.9, FluentAssertions 7.2, Testcontainers, bUnit 2.11.3 (só no projeto de testes de integração).

**Spec:** `docs/superpowers/specs/2026-10-10-migracao-para-o-design-system-design.md` (seções 3, 4, 5, 7.6, 8, 9, 10, 11 e 15, inclusive "Da fase 9E").

## Decisões deste plano

- **Quadros desta fase:** baixados do canvas em 2026-10-15 (versão `1792048623-298d`). Cópia local, somente leitura, em `C:\Users\Gabriel Paulino\AppData\Local\Temp\claude\c--GitHub-SIstemaControleAtividadesJuridicas\73464877-64cd-41cc-b340-808de5c66612\scratchpad\quadros-9f\project\`: `AgendaA` (desktop; com a prop `menu-escondido`, o `AgendaMenuEscondido`; com `visaoInicial`, os três períodos), `AgendaTablet`, `AgendaMobile`, `NovoEvento`, `NovoEventoTablet`, `NovoEventoMobile`, `Evento`, `EventoTablet`, `EventoMobile` (`.dc.html`), mais `Menu`, `Trilho`, `NavInferior` e a cópia do design system em `ds/araujods/`. Os quadros da agenda e do formulário são **antigos** (estilo embutido); os da ficha já usam classes `aj-` (família das fichas da 9D). Levantamentos que este plano cita, em `…\scratchpad\plano-9f\`: `inventario-codigo.md` (o código de hoje, com linhas), `quadros-agenda.md` e `quadros-evento.md` (cada quadro bloco a bloco, com textos, medidas, as classes existentes que cobrem cada bloco e as classes novas propostas).
- **Rotas** (spec 5): `/agenda` (já existe), `/agenda/novo`, `/agenda/{Id:guid}`, `/agenda/{Id:guid}/editar`. `RotasDoCliente` não muda (as rotas novas nascem fechadas ao portal).
- **Período no endereço** (spec 5 e 7.6): `/agenda?visao={dia|semana|mes}&data={yyyy-MM-dd}`.
  - Sem `visao`, cada largura mostra o seu padrão sobre os mesmos dados: Semana no desktop e no tablet, Dia no mobile. Com `visao`, as três larguras mostram essa visão.
  - Sem `data` (ou com data inválida, ou fora de 2000–2100), vale hoje no fuso do escritório. `visao` desconhecida conta como ausente. Nada disso é erro.
  - A consulta é uma só por período: a semana (segunda a domingo) que contém `data` nas visões Dia e Semana e sem `visao`; no Mês, as semanas inteiras que cobrem o mês (de 28 a 42 dias, dentro do limite de 92 do `ListarEventos`). Cada troca de período grava um registro de consulta, como hoje.
  - Os controles de período navegam (`NavigationManager.NavigateTo`) para o endereço novo; a página reage à troca de `?visao`/`?data` e consulta de novo. Os filtros (Tipo e Participante) ficam no estado da página, não no endereço, e sobrevivem à troca de período.
- **Navegação de período:**
  - Desktop: setas `Período anterior` e `Próximo período` e `Hoje`; andam uma semana (Semana e sem `visao`), um dia (Dia) ou um mês (Mês).
  - Tablet: setas por visão — `Semana anterior`/`Próxima semana`, `Dia anterior`/`Próximo dia`, `Mês anterior`/`Próximo mês` — e `Hoje`.
  - Mobile: sem `Hoje` (o quadro não tem; abrir a agenda pelo menu cai em hoje); setas `Semana anterior`/`Próxima semana` no Dia e na Semana (andam a faixa de dias, mantendo o dia da semana escolhido) e `Mês anterior`/`Próximo mês` no Mês.
  - Visões: o controle Dia/Semana/Mês leva `?visao=` com a mesma `data`.
  - Faixa de dias do mobile: tocar num dia troca `data` (mantém a `visao`; na Semana, leva ao Dia daquele dia). Grade compacta do Mês no mobile: tocar num dia troca `data` e fica no Mês (a lista embaixo é desse dia).
  - Mês: o `+N` da célula leva ao Dia daquela data. O número do dia não é interativo (o quadro).
- **Cartão inteiro clicável** (spec 7.6): todo cartão de evento é um `<a class="aj-evento …" href="/agenda/{id}{origem}">`, em que a origem é o período de onde se veio (`?visao=…&data=…`, só o que estiver no endereço). A ficha e o formulário leem `visao` e `data` por `[SupplyParameterFromQuery]` e repassam a origem ao voltar, ao Cancelar, aos links e ao destino depois de salvar ou excluir (como o `OrigemDaAtividade` da 9E).
- **Depois de salvar** (spec 5): o evento novo volta à agenda na visão de origem com `data` = a data do início do evento (para o evento salvo aparecer), com o recado. A edição volta à ficha (com a origem), com o recado. Excluir volta à agenda de origem com `Evento excluído.`.
- **Aviso de conflito em três redações** (spec 3; quadros `AgendaA`, `AgendaTablet`, `AgendaMobile`): o `Recado` passa a levar as sobreposições (`IReadOnlyList<SobreposicaoDeAgenda>?`). Quem mostra (a agenda depois do evento novo; a ficha depois da edição) usa o componente `AvisoDeEventoSalvo`, que emite `Evento salvo.` (sucesso) sem sobreposição, ou três avisos de atenção em invólucros `aj-so-desktop`, `aj-so-tablet`, `aj-so-mobile`, com as frases da seção "Global Constraints". Várias pessoas: as sobreposições separadas por `; `. Sobreposição que atravessa a meia-noite: a forma de hoje (`de dd/MM HH:mm a dd/MM HH:mm`) nas três visões.
- **Semana do desktop:** sete colunas sempre (spec 7.6: "grade de sete colunas"), com os cartões empilhados na ordem de início (o quadro não usa régua na Semana). O quadro usa `auto-fit` (cinco colunas a 1200px com o menu aberto); fica a spec.
- **Régua do Dia (desktop):** 56px por hora, das 08:00 às 19:00, estendida até a hora cheia do primeiro início e a hora cheia seguinte ao último fim do dia (spec 8); evento de outro dia entra recortado ao dia (a régua vai então a 00:00 ou a 24:00). Posição por quartos de hora numa grade CSS: o início arredonda para baixo, o fim para cima, mínimo de dois quartos (24px de cartão). Eventos que se sobrepõem (já arredondados) formam grupos; cada evento ocupa a primeira coluna livre do grupo, e o grupo divide a largura em até quatro colunas (do quinto em diante, o evento divide a última coluna com quem estiver nela). Classes: `aj-regua__grade--horas-{n}` (n de 1 a 24), `aj-regua__evento--inicio-{q}` (q de 0 a 95), `aj-regua__evento--quartos-{q}` (q de 2 a 96), `aj-regua__evento--parte-{k}-de-{n}` (n de 2 a 4). Nenhum atributo `style`.
- **Mês:** grade de semanas inteiras de segunda a domingo (4, 5 ou 6 linhas); dia fora do mês sem eventos (o quadro); até três eventos por célula e `+{n}` com os outros; evento de vários dias aparece em cada dia que toca, com a hora só no dia em que começa. O tipo do evento vai em texto para leitor de tela no item do mês (`Reunião, `), porque o item só tem a cor. No tablet a grade não tem largura mínima (o quadro do desktop tem 700px; o tablet a 834 rolaria de lado).
- **Hora dos cartões com coluna de hora (tablet e mobile):** a linha de cima é o início e a de baixo o fim, em `HH:mm`; o lado que cai em outro dia mostra `…`. Os cartões com a linha de horário (desktop, régua) usam o `Intervalo` de hoje (`23:30 – 08/10 00:30`).
- **Linhas do cartão por largura** (os quadros): desktop (Semana) — tipo, horário, título, local, cliente, participantes, cada um na sua linha e só se houver; tablet — hora, tipo, título e uma linha com o local (ou o cliente, sem local); mobile — hora, tipo, título, `local · cliente` e participantes; régua — tipo, horário, título e `local · cliente · participantes`; mês — hora de início e título. O processo não aparece nos cartões (nenhum quadro o mostra; fica na ficha).
- **Filtros:** desktop na barra (Tipo, Participante só para a Diretoria, `Filtrar`); tablet `Filtros` e mobile `Filtrar agenda` abrem um painel (spec 9) com os mesmos campos, `Limpar` e `Filtrar`, como o da Auditoria (logo abaixo da barra no tablet; no topo do conteúdo no mobile). O participante continua lendo `ListarUsuarios` (com os inativos), só na Diretoria.
- **Ficha do evento** (quadros novos, família 9D): `CabecalhoDeFicha` como está (`TituloDaBarra="Evento"`, `Origem="Agenda"`, `Acoes` no desktop: `Excluir` perigo e `Editar` secundário); no tablet e no mobile, barra fixa `aj-tablet-mobile > aj-acoes--fixa` com `Excluir` (`aj-botao--destrutivo`) e `Editar` (primário). As duas ações só com `Acoes.Editar`/`Acoes.Excluir` do `ObterEvento` (Diretoria ou quem criou); sem elas, sem botões nem barra. Desktop: Descrição e Participantes à esquerda, Detalhes à direita; tablet e mobile: Descrição, Detalhes, Participantes (Detalhes repetido em dois invólucros de visão, sem estado). Sem descrição, o painel Descrição some. Participantes: o próprio usuário primeiro, com `(você)`; os outros na ordem do servidor. Excluir pede confirmação no topo (padrão da 9E).
- **Avatar do próprio usuário:** dourado (o `.aj-avatar` sem modificador, como o avatar do menu), nos participantes da ficha e nos chips do formulário (os quadros). Os outros pela regra da 9E (`AvatarDePessoa.Classe`). `AvatarDePessoa` sobe de `Components/Atividades` para `Components/Layout` (o segundo assunto a usá-lo).
- **Formulário do evento** (quadros antigos): as divergências transversais dos quadros antigos ficam a favor das classes, como na 9E (aviso, voltar do tablet, vão e raio dos campos, painéis do tablet com o respiro de painel comum, barras com 46px e a sombra do primário, busca de vínculo como a da 9E). O que muda de controle ou de ordem por visão segue cada quadro:
  - Tipo: `select` no desktop; ladrilhos de rádio (`aj-escolha`) no tablet (quatro colunas) e no mobile (duas). O evento novo começa em `Reunião` (o código de hoje).
  - Duração: número `Duração (minutos)` no desktop; no tablet e no mobile, `select` `Duração` com as quatro opções do quadro e `Outra duração`, que mostra o número `Duração (minutos)` (função não some: o domínio aceita de 1 a 10080). Na edição, duração fora das quatro abre em `Outra duração`.
  - Participantes: caixas de marcar (`aj-opcao`) no desktop; chips com avatar (`aj-chip--pessoa`) no tablet (nome inteiro) e no mobile (primeiro nome; o próprio usuário `Você`). Quem cria entra marcado e travado no evento novo; na edição ninguém trava.
  - Mobile: sem painéis (`aj-painel--solto-no-mobile`: o painel deixa de ser caixa e o `h2` some), Descrição por último, o voltar é o X `Fechar sem salvar`, e só `Salvar evento` na barra.
  - Atividade: no tablet e no mobile, por analogia com o desktop (os quadros não têm o vínculo; a função fica).
  - Cliente: escolhido com avatar (`aj-vinculo__escolhido--com-avatar`) e `Remover cliente`; no desktop a busca `Trocar cliente` fica junto do escolhido; no tablet e no mobile, a busca só aparece sem cliente. Processo e atividade escolhidos: o mesmo escolhido, sem avatar, com `Remover processo`/`Remover atividade` (não desenhado). Cliente e processo continuam independentes na tela (o servidor recusa o cliente que não é do processo, como hoje).
  - Erros por campo: `Campo obrigatório` em Título, Data e Hora (o quadro); com data ou hora vazias, também o aviso do topo `Informe a data e a hora do evento.` (o quadro). Participantes sem ninguém: `Marque ao menos um participante.` no grupo. Erros do servidor no aviso do topo.
  - Quem não gerencia o evento e abre `/agenda/{id}/editar`: aviso de atenção `Só a Diretoria e quem criou o evento podem editá-lo.`, sem formulário, com o voltar para a ficha (o terceiro estado da 9E). Conflito de versão: só o aviso com a mensagem do servidor (que pede recarregar), formulário mantido (a 9E).
  - O formulário usa o `CargaDoFormulario` (carga dos usuários e, na edição, do evento) e trava o envio depois do sucesso até a navegação (9D).
- **Peças compartilhadas que ganham parâmetro** (Tarefa 3): `Recados`/`Recado` (sobreposições); `Aviso.Frase` (`aj-aviso--frase`); `EstadoDaLista.Icone` e `Dica`; `CabecalhoDePagina.AcaoDoMobile` (botão entre o título e o avatar da barra do mobile) e `SubtituloSoNoDesktop`; `BarraDeVoltar.Icone` (padrão `voltar`); `CabecalhoDeDetalhe.IconeDoVoltarNoMobile` e `SubtituloSoNoDesktop`; `RotuloDeCampo.Complemento` (o `(opcional)` em `<small>`); `Icone` com `filtrar` (funil). Nenhum comportamento de tela existente muda.
- **Camada antiga:** a agenda é a última tela com marcação antiga. A Tarefa 10 apaga `FichaDoEvento.razor` e `FormularioDeEvento.razor`, tira do `app.css` as regras que só a agenda usava (lista no `inventario-codigo.md`, seção 6), a regra interina das telas não migradas e as duas regras de etiqueta que dependem dela, e reescreve os dois testes de "tela ainda não migrada" de `LayoutWebTests`. As regras de elemento que ainda alcançam as classes `aj-` (`h1`–`h3`, `p`, `label`, `button:disabled`, `fieldset`, `legend`, `input[type=checkbox]`…) ficam para a 9G.
- **Sem backend novo:** `EventoResumo` já traz o que os cartões mostram (tipo, início, fim, título, local, cliente, participantes); `EventoDetalhe` traz o resto. Nenhuma consulta nova.
- **Divergências dos quadros antigos que ficam como nas fases anteriores** (para todas as telas ficarem iguais entre si): o `letter-spacing` do título; a sombra do botão primário no tablet e na barra fixa; a sombra da opção escolhida do segmentado no desktop (a classe); o hover do "Filtros" do tablet (a classe); no formulário, as do item anterior. Ficam anotadas na spec.

## Como ler este plano

Cada tarefa traz as **regras** (o contrato), as **interfaces** que ela consome e produz e os **testes obrigatórios**. O plano traz o código onde ele é a própria fidelidade (o CSS, a marcação das telas) ou onde o detalhe é sutil. O restante é escrito no estilo do que já existe. O código do plano não foi executado: quando o Blazor, o bUnit ou o compilador exigirem, ajuste o mínimo para preservar o comportamento e registre o desvio no relatório, com a mensagem de erro que o motivou.

Nunca apague um teste nem afrouxe uma asserção. Quando uma asserção mudar porque a marcação mudou, reescreva-a mantendo o que ela prova e liste-a no relatório. Asserção negativa (`NotContain`) que deixaria de provar algo com a marcação nova ganha uma afirmação positiva ao lado. Asserção sobre o bloco de uma visão (`BlocoDeVisao.De`) sempre tem uma afirmação positiva sobre o mesmo bloco: um bloco vazio aceitaria qualquer `NotContain`.

O HTML desenhado pode ter espaço em branco e quebra de linha entre as marcas. As expressões regulares dos testes usam `\s*` entre marcas e `(?s)` quando atravessam linhas; quem escreve a marcação deixa colados os elementos que o teste procura colados.

O que só se vê depois de um clique no navegador (o Blazor aplicando diferenças ao DOM) o bUnit não prova: chaves `@key` de dois tipos de linha no mesmo pai nunca podem coincidir (lição da 9D).

A fonte de cada tela é o quadro do canvas. Antes de escrever a marcação de uma tela, releia os arquivos dela na cópia local e a seção dela nos levantamentos. Se um quadro diferir do que este plano descreve, pare e relate.

## Estado de partida

- Worktree `.claude/worktrees/feat+fase-9f-agenda`, branch `worktree-feat+fase-9f-agenda`, na `main` local (`c8b9526`), que já tem as fases 9A a 9E.
- Canvas: https://claude.ai/artifact/HzHWv1pgVXAnYHuPEYQDFj (versão `1792048623-298d`). Design system: https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS, versão `1792048576-7268` (a última da 9E); o `componentes.css` do código é cópia dela.
- Web, depois da 9E: `CabecalhoDePagina` (`Titulo`, `Subtitulo`, `SubtituloMobile`, `Alerta`, `Folgado`, `VoltarPara`, `RotuloDoVoltar`, `Acao`, `IconeDaAcao`, `AcaoHref`, `OnAcao`, `Complemento`, `LinhaDoMobile`; o botão `Exibir menu` na linha do título com o menu escondido), `CabecalhoDeDetalhe` (`Titulo`, `Subtitulo`, `VoltarPara`, `Origem`, `RotuloDoVoltarNoMobile`), `CabecalhoDeFicha` (`Titulo`, `Subtitulo`, `Resumo`, `TituloDaBarra`, `VoltarPara`, `Origem`, `Acoes`), `BarraDeVoltar` (`Titulo`, `VoltarPara`, `Rotulo`, `Acao`), `CargaDoFormulario` + `EstadoDeCarga`, `Aviso` (`Tipo`, `ChildContent`, `Acoes`, `Titulo`, `Rotulo`), `EstadoDaLista`, `MenuDeAcoes`, `RotuloDeCampo` (`Texto`, `Para`, `ChildContent`, `Classe`), `Icone` (`Nome`, `Classe`), `Contagem`, `Recados`, `FusoDoEscritorio`, `AvatarDePessoa` (em `Components/Atividades`). As telas desta fase (`Pages/Agenda.razor`, `Components/Agenda/FichaDoEvento.razor`, `Components/Agenda/FormularioDeEvento.razor`) têm a marcação antiga vestida pelo `app.css`; só existe a rota `/agenda`, que mostra uma semana e abre a ficha e o formulário em painéis.
- Testes: `AplicacaoDeTeste`, `TelaDeTeste`, `TelaInterativa` (bUnit, `AtorDaDiretoria` e `AtorDaProducao`), `DespachanteCombinado` (`Responde`, `RespondeDepois`, `Recebidas`), `ComBarraSuperior<T>`, `BlocoDeVisao.De`. `AgendaWebTests` (seis testes de tela presos à marcação antiga) e `AgendaPorDiaTests` (puros, sobre `RotulosDeAgenda`), no projeto de integração; nenhum teste de tela da ficha nem do formulário.
- Linha de base: a suíte inteira passou em `3d2ef7f` (a árvore da `main` de hoje): unidade 896, integração 958.
- `git` só pela ferramenta PowerShell. Antes de cada commit, `git branch --show-current`.
- A máquina tem caído ao começar build ou teste: build sempre `dotnet build -m:1`; teste sempre `--no-build` depois de um build feito; um comando por vez. Depois de uma queda que pegou build, confira as DLLs (`[Reflection.AssemblyName]::GetAssemblyName`) antes de confiar no build incremental. A suíte inteira roda em lotes (`suite-em-lotes.ps1`, em `C:\Users\Gabriel Paulino\.claude\projects\c--GitHub-SIstemaControleAtividadesJuridicas\conferencia-de-telas\`).

## Global Constraints

- TFM único `net10.0`. `Nullable` e `TreatWarningsAsErrors=true`. Nenhum pacote novo.
- Código, nomes e textos em português, com acentuação correta. Arquivos em UTF-8 sem BOM, com o fim de linha que o arquivo já tem.
- A camada Web só chega aos dados pelo `IDespachante` (`WebSoPeloDespachanteTests`). Nenhuma consulta nova no backend.
- `wwwroot/css/tokens.css` e `wwwroot/css/componentes.css` não são editados à mão: mudam no artifact do design system e são copiados de lá.
- Tela migrada não tem atributo `style`, nem `<style>`, nem arquivo `.razor.css`, nem classe que não comece por `aj-`, nem `<button>`, `<table>`, `<input>`, `<select>` ou `<textarea>` sem `class`. Nenhum CSS novo no `app.css`.
- As classes de visibilidade (`aj-so-desktop`, `aj-so-tablet`, `aj-so-mobile`, `aj-desktop-tablet`, `aj-tablet-mobile`, `aj-desktop-mobile`) vão num invólucro próprio (`div` ou `span`) cuja classe é **só** a de visibilidade, nunca no elemento do componente.
- Visões: mobile até 760px, tablet de 761px a 1199px, desktop a partir de 1200px.
- Todo texto que um teste procura no HTML é escrito por **uma única** expressão Razor ou por marcação fixa.
- Rotas existentes não mudam. Rotas novas: as três da seção "Decisões". `RotasDoCliente` não muda.
- Botão só de ícone leva `aria-label` com o verbo e o objeto (`Filtrar agenda`, `Remover cliente`, `Fechar sem salvar`).
- Chaves `@key` de dois tipos de linha no mesmo elemento pai nunca coincidem.
- Formulário em tela própria: depois de salvar com sucesso, o botão de envio fica travado até a navegação chegar (o `finally` só o libera se a tela não navegou), como os da 9D e da 9E.
- Permissões (as do servidor, `AcessoAoEvento`): toda a equipe vê a agenda (a Produção só os eventos em que participa ou que criou) e cria evento; vê a ficha quem é Diretoria, criou ou participa; edita e exclui quem é Diretoria ou criou. O filtro de participante só aparece para a Diretoria. A tela decide as ações pelas `Acoes` do `ObterEvento`, nunca pelo perfil.
- "Hoje" sai sempre do fuso do escritório (`FusoDoEscritorio.ParaLocal(DateTime.UtcNow)`), numa só função de cada tela; os testes fixam o período por `?data=`.
- Textos exatos, dos quadros:
  - Agenda: título `Agenda`; ação `Novo evento` (botão no cabeçalho; no mobile, o botão flutuante com `aria-label="Novo evento"`); subtítulo do desktop e do tablet pela visão daquelas larguras — Semana `Semana de {dd/MM} a {dd/MM/yyyy}` (`Semana de 05/10 a 11/10/2026`; com anos diferentes, `Semana de 28/12/2026 a 03/01/2027`), Dia `{Quinta-feira}, {dd/MM/yyyy}` (`Quinta-feira, 08/10/2026`), Mês `{Outubro} de {yyyy}` (`Outubro de 2026`); mobile sem subtítulo, com a linha do mês `{Outubro} {yyyy}` (`Outubro 2026`, o mês da `data`); setas e `Hoje` (seção "Decisões"); controle de visões com `aria-label="Visualização da agenda"` (desktop) e `aria-label="Visualização"` (tablet e mobile), opções `Dia`, `Semana`, `Mês` com `aria-pressed`; filtros do desktop `Tipo` (`Todos`, `Audiência`, `Reunião`, `Prazo`, `Outro`), `Participante` (`Todos`, os nomes, `{nome} (inativo)`), `Filtrar`; tablet `Filtros`; mobile botão de ícone `Filtrar agenda`; painel com os mesmos campos, `Limpar` e `Filtrar`.
  - Semana (desktop): cabeçalho de coluna `h2` com `aria-label="{segunda-feira}, {dd/MM/yyyy}"` (hoje: `…, hoje`), sigla `Seg`, `Ter`, `Qua`, `Qui`, `Sex`, `Sáb`, `Dom` (hoje: `{Qui} · hoje`), número `dd`; dia vazio `Sem eventos`. Tablet (e Semana do mobile): seção com o mesmo `aria-label`, sigla (hoje: `Hoje`), número `dd`; vazio `Sem eventos`.
  - Dia: desktop — cabeçalho com o número `dd`, `{Quinta-feira}` (hoje: `{Quinta-feira} · hoje`) e o total `{n} eventos` (`1 evento`, `0 eventos`); rótulos da régua `08:00`…; tablet — uma seção do dia como a da Semana; mobile — faixa de dias com `aria-label="{Segunda}, {dd/MM}"` (hoje: `…, hoje`) e `aria-pressed`, sigla `Seg`…`Dom`, título do dia `{Quinta}, {dd/MM}` (hoje: `… · hoje`), total `{n} eventos`; vazio `Nenhum evento neste dia` e `Toque em + para agendar.`.
  - Mês: siglas `Seg`…`Dom`; `+{n}` com `aria-label="Mais {n} eventos em {dd/MM}"` (`Mais 1 evento em 08/10`); item do evento com a hora de início e o título, e o tipo só para leitor (`Reunião, `).
  - Cartão: tipo `Audiência`, `Reunião`, `Prazo`, `Outro`; horário `{HH:mm} – {HH:mm}` (o `Intervalo` de hoje); hora em coluna `HH:mm`, com `…` no lado de outro dia.
  - Aviso depois de salvar: sem sobreposição `Evento salvo.`; com sobreposição, desktop `<strong>Evento salvo.</strong> Atenção: {s1}; {s2}.`, tablet `<strong>Evento salvo.</strong> {s1}; {s2}.`, mobile `{s1}; {s2}.`; cada sobreposição `{nome} já tem evento das {HH:mm} às {HH:mm} ({dd/MM})` no desktop e no tablet e `{nome} já tem evento das {HH:mm} às {HH:mm} em {dd/MM}` no mobile; atravessando a meia-noite, `{nome} já tem evento de {dd/MM HH:mm} a {dd/MM HH:mm}` nas três; nome nulo `Usuário`. Depois de excluir: `Evento excluído.`.
  - Ficha: voltar `Agenda` (mobile: título da barra `Evento`, `aria-label="Voltar para Agenda"`); subtítulo `{Tipo} · {dd/MM/yyyy}, {HH:mm} – {HH:mm} · {local}` (sem local, sem a última parte; atravessando a meia-noite, o lado de outro dia com `dd/MM HH:mm`); resumo do mobile sem o local; painéis `Descrição`, `Detalhes` (`Tipo`, `Início`, `Fim`, `Duração`, `Local`, `Cliente`, `Processo`, `Atividade`, `Criado por`), `Participantes` (`{nome} (você)`, `{nome} (inativo)`, `Usuário não encontrado`); ações `Excluir` e `Editar`; `—` nos vazios; `{n} minutos` (`1 minuto`); `Cliente indisponível`, `Processo indisponível`, `Atividade vinculada (sem acesso)`, `Atividade vinculada (excluída)`, `Usuário não encontrado` (criador).
  - Formulário: `Novo evento` / `Editar evento` (título e barra do mobile); subtítulo só no desktop `Preencha os dados e escolha quem participa.`; voltar `Agenda` no novo e o título do evento na edição (antes da carga e no não encontrado, `Evento`); mobile X `Fechar sem salvar`; painéis `Evento`, `Vínculos`, `Participantes`; `Título`, `Tipo`, `Data`, `Hora`, `Duração (minutos)` (desktop) / `Duração` (tablet e mobile: `30 minutos`, `1 hora`, `1h30` no tablet e `1 hora e 30 minutos` no mobile, `2 horas`, `Outra duração`, e com ela `Duração (minutos)`); ajuda só no desktop `Data e hora no horário do escritório.`; `Local`; `Descrição` com `(opcional)`; `Cliente`, `Remover cliente`, busca com o rótulo para leitor `Trocar cliente` e o exemplo `Trocar cliente: nome, CPF ou CNPJ` (com cliente, desktop) ou o rótulo `Escolher cliente (nome, CPF ou CNPJ)` e o exemplo `Escolher cliente: nome, CPF ou CNPJ` (sem cliente); `Processo` (tablet e mobile com `(opcional)`), rótulo para leitor `Escolher processo (título, parte contrária ou número CNJ)`, exemplo `Título, parte contrária ou número CNJ` (desktop) / `Título, parte ou número CNJ` (tablet e mobile), `Remover processo`; `Atividade` com `(opcional)`, rótulo para leitor `Escolher atividade (título)`, exemplo `Buscar atividade pelo título`, `Remover atividade`; resultados com `Escolher`; participantes `{nome}` com a nota `(você)` / `(inativo)` (desktop), `{nome} (você)` (tablet), primeiro nome e `Você` (mobile); ajuda `Quem cria o evento sempre participa.` (desktop e tablet); ações `Cancelar` e `Salvar evento` (mobile só `Salvar evento`).
  - Sem quadro, mantidos (do código de hoje): `Carregando…`; `Seu acesso foi encerrado. Entre novamente.`; `Acesso negado.`; `Não foi possível consultar a agenda.`; `Tentar de novo`; `Não foi possível consultar os usuários.`; `Não foi possível consultar o evento.`; `Evento não encontrado.`; `Nenhum cliente encontrado.`, `Nenhum processo encontrado.`, `Nenhuma atividade encontrada.`, `Mostrando os primeiros resultados. Refine a busca para ver outros.` (atividades: `…para ver outras.`); `Informe a data e a hora do evento.`; `A data deve estar entre os anos 2000 e 2100.`; `Marque ao menos um participante.`; as mensagens do servidor (domínio e conflito) como chegam.
  - Sem quadro, novos por analogia: `Campo obrigatório` (Título e Data, como a Hora do quadro); `Outra duração`; `Remover processo`, `Remover atividade`; `Só a Diretoria e quem criou o evento podem editá-lo.`; confirmação de exclusão `aria-label="Confirmar exclusão"`, título `Confirmar a exclusão de {título}?`, texto `O evento sai da agenda de todos os participantes.`, botões `Cancelar` e `Confirmar exclusão`; `+{n}` e `Mais {n} eventos em {dd/MM}`; `Dia anterior`, `Próximo dia`, `Mês anterior`, `Próximo mês`; `Limpar`; `Evento` (título da barra e origem antes da carga).

## Review Focus

Condições que a spec implica e que mais provavelmente atingem quem usa a agenda. Cada uma tem o seu teste na tarefa indicada.

1. **Endereço estranho:** `?visao=xyz`, `?data=2026-13-40`, `?data=1999-01-01` ou nenhum parâmetro abrem a agenda no padrão (hoje; Semana no desktop e no tablet, Dia no mobile), sem erro e com uma consulta só. Testes nas Tarefas 2 e 9.
2. **Evento de mais de um dia e que atravessa a meia-noite:** aparece em cada dia que toca na Semana, no Dia (régua recortada ao dia) e no Mês (hora só no primeiro dia), com `…` na coluna de hora do lado de outro dia; o que termina à meia-noite em ponto não aparece no dia seguinte. Testes nas Tarefas 2, 6, 7 e 8.
3. **Eventos simultâneos na régua:** dois eventos que se sobrepõem ficam lado a lado (`parte-1-de-2` e `parte-2-de-2`), nunca um por cima do outro; um evento depois do grupo volta à largura inteira; o evento de 15 minutos ganha a altura mínima de dois quartos. Testes nas Tarefas 2 e 7.
4. **Quem só participa:** a Produção que participa e não criou abre a ficha sem `Excluir` nem `Editar` (nem a barra fixa) e, em `/agenda/{id}/editar`, vê o aviso de que só a Diretoria e quem criou podem editar, sem formulário e sem consultar os usuários; a Produção que criou vê as ações. Testes nas Tarefas 4 e 5.
5. **Clique duplo:** `Salvar evento` (novo e edição) e `Confirmar exclusão`: um envio só, e o botão travado depois do sucesso até a navegação. Testes nas Tarefas 4 e 5.

Conferências no navegador, na Tarefa 11: as três larguras nos três períodos; a régua com eventos simultâneos e com um evento curto; o mês de seis semanas e o `+N`; o evento que atravessa a meia-noite; título, local e nomes longos nos cartões a 390px; a faixa e a grade compacta do mobile com toque e teclado; o painel de filtros; o aviso de conflito nas três redações; a barra fixa sem cobrir o último painel na ficha e no formulário; o vínculo escolhido pela busca (a lição da 9D); a troca de período sem consulta repetida.

---

### Task 1: Design system — padrões da agenda e do evento, e cópia para o código

Feita pela sessão que coordena: publica no artifact do usuário.

**Files:**
- Artifact do design system, sob `project/`: `components/bundle.css`, `design-system.json` (só `lastChange`); README e prévia dos componentes novos; README emendado (e prévia, onde há estado novo) dos que ganham modificador
- Artifact do canvas: `project/ds/araujods/components/bundle.css` e o registro `designSystems` do `canvas.json`
- Modify: `src/AtividadesJuridicas.Web/wwwroot/css/componentes.css`

**Interfaces:**
- Produces: as classes da agenda (`quadros-agenda.md`, seção 8) e do evento (`quadros-evento.md`, seção 9), com os ajustes das decisões deste plano. A lista exata está no passo 2.

**Regras:**
- Nenhuma classe existente muda de valor. Medida de quadro que difere de uma classe existente vira modificador ou regra de contexto (spec, seção 3). As correções do `aj-evento--coluna` (a borda só no Outro, o horário da Audiência em `--evento-audiencia-suave`, os pontos do Prazo e do Outro no desktop) entram como regras novas no bloco da fase, depois das originais.
- Só tokens que já existem. Onde não há token, o literal do quadro (56px, 52px, 64px, 96px, 700px, 14px da linha de quarto de hora, `#B07A12`, `#B08A45`, `#9DB898`, as sombras sem token listadas nos levantamentos).
- Regra que muda por visão segue o padrão do arquivo: a consulta de mídia e, depois, as mesmas regras sob `[data-visao="…"]`.
- A régua usa grade CSS por quartos de hora (seção "Decisões"); as famílias de classes numeradas são geradas por roteiro e conferidas por contagem.
- Semana do desktop com `repeat(7,minmax(0,1fr))`; `aj-mes__grade` com `min-width:700px` só no desktop.
- Ficam de fora, pelas decisões do plano: `aj-painel--amplo-no-tablet`, `aj-campo-icone--branco` e qualquer regra que tire a sombra do primário no tablet, mude o `letter-spacing` do título ou o voltar do tablet.
- O bloco novo vai ao fim do `bundle.css`, depois das seções da 9E (a última é "Ajustes da conferência da fase 9E no navegador").

- [ ] **Step 1: Conferir o acesso e as versões**

A ferramenta Artifact abre o design system (https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS) e o canvas (https://claude.ai/artifact/HzHWv1pgVXAnYHuPEYQDFj). Se responder "artifact not found" (ou `list` vier vazio), o Claude Code está conectado a outra conta: pare e peça ao usuário para entrar com a conta dona dos artifacts. Se a publicação for negada pelo classificador ("Modify Shared Resources"), não contorne: avise o usuário antes de seguir. Nenhuma tela desta fase é escrita antes desta tarefa.

Ação `read` do design system e do canvas (o artifact inteiro, sem `path`, antes de publicar em cada um: sem essa leitura a publicação é recusada com "You haven't viewed the latest version"). O design system está na versão `1792048576-7268` (a última da 9E) e o canvas na `1792048623-298d` (a dos quadros desta fase). Leia também `project/components/bundle.css` do design system (`read` com `path`, salvo no scratchpad) e compare com o `src/AtividadesJuridicas.Web/wwwroot/css/componentes.css` do worktree, da linha "Visões:" em diante: têm de ser idênticos (1119 linhas no `componentes.css`, 1118 no `bundle.css`; o `bundle.css` tem fim de linha LF, o `componentes.css` CRLF). Leia também `project/design-system.json` (`read` com `path`): ele é a base do passo 4. Se diferirem, alguém editou o design system: acrescente o bloco do passo 2 sobre a versão nova e rode de novo o roteiro de conferência do passo 2 contra ela (ele aponta qualquer nome do bloco que já exista). A versão também muda quando o tipo do artifact ganha release nova, sem mudar o conteúdo: compare o conteúdo, não o número.

- [ ] **Step 2: Acrescentar o bloco ao fim de `components/bundle.css`**

Ao `bundle.css` lido no passo 1 (UTF-8 sem BOM, LF), acrescente uma linha em branco e o bloco abaixo, depois da última seção da 9E ("Ajustes da conferência da fase 9E no navegador"). O bloco segue as regras da tarefa (acima). Ele produz exatamente estes nomes (nenhum existe no `bundle.css` de `1792048576-7268`; conferido por roteiro):

- Barra da agenda: `aj-barra-filtros--dividida`, `aj-barra-filtros__grupo`, `aj-barra-filtros--plana`, `aj-campo-filtro--tipo`, `aj-campo-filtro--pessoa`, `aj-botao--com-icone-pequeno`.
- Segmentados: `aj-segmentado--periodo`, `aj-segmentado__opcao--larga`, `aj-segmentado--claro`.
- Aviso: `aj-aviso--frase`.
- Cartão de evento: `aj-evento__meta`, `aj-evento--folgado`, `aj-evento--linha`, `aj-evento--mes`.
- Semana: `aj-semana`, `aj-semana__dia`, `aj-semana__dia--hoje`, `aj-semana__dia--folga`, `aj-semana__cabeca`, `aj-semana__sigla`, `aj-semana__numero`, `aj-semana__vazio`.
- Seção do dia: `aj-secao-dia`, `aj-secao-dia--hoje`, `aj-secao-dia__cabeca`, `aj-secao-dia__sigla`, `aj-secao-dia__numero`, `aj-secao-dia__eventos`, `aj-secao-dia__vazio`.
- Dia: `aj-dia`, `aj-dia--hoje`, `aj-dia__cabeca`, `aj-dia__numero`, `aj-dia__total`.
- Régua: `aj-regua`, `aj-regua__horas`, `aj-regua__hora`, `aj-regua__grade`, `aj-regua__evento`, e as famílias numeradas `aj-regua__grade--horas-{n}` (n de 1 a 24: 24 nomes), `aj-regua__evento--inicio-{q}` (q de 0 a 95: 96), `aj-regua__evento--quartos-{q}` (q de 2 a 96: 95) e `aj-regua__evento--parte-{k}-de-{n}` (n de 2 a 4, k de 1 a n: 9).
- Mês: `aj-mes`, `aj-mes__cabeca`, `aj-mes__rolagem`, `aj-mes__grade`, `aj-mes__celula`, `aj-mes__celula--fora`, `aj-mes__numero`, `aj-mes__numero--hoje`, `aj-mes__mais`.
- Barra superior do mobile: `aj-barra-superior__acao`, `aj-barra-superior__periodo`, `aj-barra-superior__setas`.
- Faixa de dias e grade compacta: `aj-faixa-dias`, `aj-faixa-dias--mes`, `aj-faixa-dias__cabeca`, `aj-faixa-dias__dia`, `aj-faixa-dias__dia--hoje`, `aj-faixa-dias__dia--fora`, `aj-faixa-dias__sigla`, `aj-faixa-dias__numero`.
- Pontos: `aj-pontos-evento`, `aj-ponto-evento`, `aj-ponto-evento--reuniao`, `aj-ponto-evento--audiencia`, `aj-ponto-evento--prazo`, `aj-ponto-evento--outro`.
- Dia escolhido e estado vazio: `aj-dia-escolhido`, `aj-dia-escolhido__total`, `aj-estado-lista__icone`, `aj-estado-lista__dica`.
- Formulário do evento: `aj-lado-a-lado--painel-medio`, `aj-linha-de-campos`, `aj-linha-de-campos__largo`, `aj-linha-de-campos__medio`, `aj-linha-de-campos__curto`, `aj-linha-de-campos__inteiro-no-mobile`, `aj-ajuda--com-icone`, `aj-ajuda--colada`, `aj-painel--folgado`, `aj-painel--solto-no-mobile`, `aj-vinculo--rotulado`, `aj-vinculo__rotulo`, `aj-vinculo__escolhido--com-avatar`, `aj-vinculo__avatar`, `aj-escolha`, `aj-escolha__opcao`, `aj-chip--pessoa`, `aj-opcao__nota--menor`.
- Ficha do evento: `aj-participantes`, `aj-participante`, `aj-participante__nome`, `aj-botao--destrutivo`, `aj-texto-livre`.
- Variável nova: `--aj-pagina-gap-solto` (o vão do conteúdo com painel solto: 14px no desktop e no tablet, 16px no mobile), no padrão de `--aj-pagina-padding-acoes`.
- Regras de contexto sem nome novo: `a.aj-evento` (sem sublinhado); `.aj-evento` (transição) e `.aj-evento:hover`; `:is(.aj-evento--coluna,.aj-evento--linha,.aj-evento--mes):hover`; as correções das variantes do desktop (`:not(.aj-evento--outro){border-width:0}`, o horário da Audiência em `--evento-audiencia-suave`, os pontos do Prazo e do Outro), que vêm depois das regras originais do `aj-evento--coluna` (linhas 270 a 273 do `bundle.css`), sem tocá-las; `.aj-evento :is(.aj-evento__titulo,.aj-evento__local)` (o texto longo quebra); `.aj-opcao:has(input:disabled)` (a opção travada sem cursor de clique); `.aj-app:has(.aj-painel--solto-no-mobile) .aj-app__conteudo`.

São 316 nomes de classe novos: 92 nominais e 224 numerados. Nenhum fica de fora pelas decisões do plano: não entram `aj-painel--amplo-no-tablet`, `aj-campo-icone--branco` nem o `aj-regua__linha` do levantamento (as linhas das horas são o fundo da grade).

```css
/* ---------- Agenda e evento (fase 9F) ---------- */
/* Barra da agenda. Desktop: a navegação de período e as visões num grupo, os filtros no outro, nas pontas da barra; os
   campos Tipo e Participante com 160px e 200px e o "Filtrar" com o ícone de 16px. Tablet: a barra sem sombra e o botão
   "Filtros" com 46px, 16px de respiro, raio 12, texto de 14,5px e ícone de traço 1,75. */
.aj-barra-filtros--dividida{justify-content:space-between;gap:var(--espaco-8) var(--espaco-16)}
.aj-barra-filtros__grupo{display:flex;flex-wrap:wrap;align-items:center;gap:var(--espaco-8)}
.aj-barra-filtros__grupo>.aj-botao{min-height:var(--campo-desktop)}
.aj-campo-filtro--tipo{min-width:160px}
.aj-campo-filtro--pessoa{min-width:200px}
.aj-botao--com-icone-pequeno svg{width:16px;height:16px}
.aj-barra-filtros--plana{box-shadow:none}
.aj-barra-filtros--plana .aj-botao{min-height:var(--controle-toque);padding:0 var(--espaco-16);border-radius:var(--raio-12);font-size:14.5px}
.aj-barra-filtros--plana .aj-botao svg{stroke-width:1.75}

/* Segmentados da agenda. No tablet as opções são mais justas (texto de 14,5px, 6px de respiro, 70px de mínimo; o
   "Semana" com 80px), a escolhida sem sombra e o "Hoje" com 16px de respiro e 14,5px. As regras miram as opções
   (aj-segmentado__opcao) e o "Hoje": as setas ficam como a classe as desenha. */
@media (min-width:761px) and (max-width:1199px){
  .aj-segmentado--periodo .aj-segmentado__opcao{min-width:70px;padding:0 var(--espaco-6);font-size:14.5px}
  .aj-segmentado--periodo .aj-segmentado__opcao[aria-pressed="true"]{box-shadow:none}
  .aj-segmentado--periodo .aj-segmentado__opcao--larga{min-width:80px}
  .aj-segmentado--periodo button.aj-segmentado__hoje{padding:0 var(--espaco-16);font-size:14.5px}
}
[data-visao="tablet"] .aj-segmentado--periodo .aj-segmentado__opcao{min-width:70px;padding:0 var(--espaco-6);font-size:14.5px}
[data-visao="tablet"] .aj-segmentado--periodo .aj-segmentado__opcao[aria-pressed="true"]{box-shadow:none}
[data-visao="tablet"] .aj-segmentado--periodo .aj-segmentado__opcao--larga{min-width:80px}
[data-visao="tablet"] .aj-segmentado--periodo button.aj-segmentado__hoje{padding:0 var(--espaco-16);font-size:14.5px}
[data-visao="desktop"] .aj-segmentado--periodo .aj-segmentado__opcao,[data-visao="mobile"] .aj-segmentado--periodo .aj-segmentado__opcao{min-width:72px;padding:0 var(--espaco-16);font-size:14px}
[data-visao="desktop"] .aj-segmentado--periodo .aj-segmentado__opcao[aria-pressed="true"],[data-visao="mobile"] .aj-segmentado--periodo .aj-segmentado__opcao[aria-pressed="true"]{box-shadow:var(--sombra-botao)}
[data-visao="desktop"] .aj-segmentado--periodo button.aj-segmentado__hoje,[data-visao="mobile"] .aj-segmentado--periodo button.aj-segmentado__hoje{padding:0 var(--espaco-18);font-size:14px}

/* Segmentado claro (visões da agenda na barra superior do mobile): 3px de respiro, opções de 38px em tinta secundária e
   a escolhida em branco, com sombra leve. Vai junto de aj-segmentado--recuado. */
.aj-segmentado--claro{padding:3px}
.aj-segmentado--claro button{min-width:0;min-height:var(--controle-desktop);padding:0 var(--espaco-6);color:var(--tinta-secundaria)}
.aj-segmentado--claro button[aria-pressed="true"]{background:var(--superficie);color:var(--tinta);box-shadow:0 1px 3px rgba(31,43,34,.15)}

/* Aviso em frase (o retorno de salvar evento com sobreposição): o ícone na altura da primeira linha, o texto em peso
   normal com "Evento salvo." em strong 600. No tablet mais respiro e menos vão; no mobile, caixa e texto menores. */
.aj-aviso--frase{align-items:flex-start;font-weight:400}
.aj-aviso--frase svg{margin-top:1px}
.aj-aviso--frase strong{font-weight:600}
@media (min-width:761px) and (max-width:1199px){.aj-aviso--frase{gap:var(--espaco-10);padding:var(--espaco-10) var(--espaco-14)}}
@media (max-width:760px){
  .aj-aviso--frase{gap:var(--espaco-10);padding:var(--espaco-10) var(--espaco-12);border-radius:var(--raio-12);font-size:13.5px;line-height:1.4}
  .aj-aviso--frase svg{width:18px;height:18px}
}
[data-visao="desktop"] .aj-aviso--frase{gap:var(--espaco-12);padding:9px var(--espaco-14);border-radius:var(--raio-14);font-size:15px;line-height:1.5}
[data-visao="tablet"] .aj-aviso--frase{gap:var(--espaco-10);padding:var(--espaco-10) var(--espaco-14);border-radius:var(--raio-14);font-size:15px;line-height:1.5}
[data-visao="mobile"] .aj-aviso--frase{gap:var(--espaco-10);padding:var(--espaco-10) var(--espaco-12);border-radius:var(--raio-12);font-size:13.5px;line-height:1.4}
[data-visao="desktop"] .aj-aviso--frase svg,[data-visao="tablet"] .aj-aviso--frase svg{width:20px;height:20px}
[data-visao="mobile"] .aj-aviso--frase svg{width:18px;height:18px}

/* Cartão de evento. A linha de apoio genérica (cliente, participantes, "local · cliente"); o cartão como link, sem
   sublinhado; o texto longo quebra em vez de alargar a coluna; o hover (o das variantes do desktop sobe 1px). */
.aj-evento__meta{font-size:13px;line-height:1.4;color:var(--aj-evento-suave);overflow-wrap:anywhere}
.aj-evento :is(.aj-evento__titulo,.aj-evento__local){overflow-wrap:anywhere}
a.aj-evento{text-decoration:none}
.aj-evento{transition:box-shadow .15s,transform .15s}
.aj-evento:hover{box-shadow:0 8px 20px -12px rgba(31,43,34,.4)}
:is(.aj-evento--coluna,.aj-evento--linha,.aj-evento--mes):hover{transform:translateY(-1px);box-shadow:0 8px 18px -10px rgba(31,43,34,.45)}

/* Correções das variantes do desktop (Semana, régua e mês), depois das regras originais: sem borda, salvo o Outro; o
   horário da Audiência no tom suave; o ponto do Prazo em #B07A12 e o do Outro em tinta-icone. */
:is(.aj-evento--coluna,.aj-evento--linha,.aj-evento--mes):not(.aj-evento--outro){border-width:0}
:is(.aj-evento--coluna,.aj-evento--linha).aj-evento--audiencia .aj-evento__horario{color:var(--evento-audiencia-suave)}
:is(.aj-evento--coluna,.aj-evento--linha).aj-evento--prazo .aj-evento__tipo::before{background:#B07A12}
:is(.aj-evento--coluna,.aj-evento--linha).aj-evento--outro .aj-evento__tipo::before{background:var(--tinta-icone)}

/* Cartão folgado (mobile): vão e respiro de 14, raio 16, coluna da hora de 46px com o início em 15px, 2px entre as
   linhas do corpo e título de 15,5px. */
.aj-evento--folgado{gap:var(--espaco-14);padding:var(--espaco-14);border-radius:var(--raio-16)}
.aj-evento--folgado .aj-evento__hora{flex-basis:46px}
.aj-evento--folgado .aj-evento__hora b{font-size:15px}
.aj-evento--folgado .aj-evento__corpo{gap:var(--espaco-2)}
.aj-evento--folgado .aj-evento__titulo{font-size:15.5px}

/* Cartão em linha (régua do Dia, desktop): tipo, horário, título e a linha de apoio lado a lado, 14px entre eles; o
   que não cabe é cortado (a linha de apoio com reticências), para o cartão estreito dos eventos simultâneos. */
.aj-evento--linha{align-items:center;gap:var(--espaco-14);padding:var(--espaco-6) var(--espaco-12);border-radius:var(--raio-10);overflow:hidden;white-space:nowrap}
.aj-evento--linha .aj-evento__horario{font-size:12.5px;font-weight:600;color:var(--tinta-secundaria);font-variant-numeric:tabular-nums}
.aj-evento--linha .aj-evento__titulo{font-size:14.5px;line-height:1.5}
.aj-evento--linha .aj-evento__meta{min-width:0;overflow:hidden;text-overflow:ellipsis;line-height:1.5}

/* Item de evento da célula do mês: a hora de início na cor do tipo e o título cortado com reticências. */
.aj-evento--mes{align-items:center;gap:5px;padding:3px var(--espaco-6);border-radius:6px;font-size:12px;white-space:nowrap;overflow:hidden}
.aj-evento--mes.aj-evento--outro{padding:2px var(--espaco-6)}
.aj-evento--mes .aj-evento__horario{flex:0 0 auto;font-weight:600;color:var(--aj-evento-rotulo);font-variant-numeric:tabular-nums}
.aj-evento--mes .aj-evento__titulo{min-width:0;overflow:hidden;text-overflow:ellipsis;font-size:12px;font-weight:400;line-height:1.5}

/* Semana (desktop): sete colunas sempre, cada uma com o cabeçalho do dia e os cartões empilhados. Hoje sublinhado em
   verde, com o número num círculo; sábado e domingo com o número apagado; dia sem evento numa caixa tracejada. */
.aj-semana{display:grid;grid-template-columns:repeat(7,minmax(0,1fr));align-items:start;gap:var(--espaco-8);padding:var(--espaco-6);border:1px solid var(--borda);border-radius:var(--raio-14);background:var(--superficie);box-shadow:var(--sombra-painel)}
.aj-semana__dia{display:flex;flex-direction:column;gap:var(--espaco-8);min-width:0}
.aj-semana__cabeca{display:flex;align-items:center;justify-content:space-between;gap:var(--espaco-8);min-height:40px;margin:0;padding:0 var(--espaco-6) var(--espaco-8);border-bottom:1px solid var(--borda-suave);font:400 15px/1.5 var(--font-sans)}
.aj-semana__sigla{font-size:13px;font-weight:600;color:var(--tinta-apagada)}
.aj-semana__numero{font-size:20px;font-weight:600;line-height:1;font-variant-numeric:tabular-nums}
.aj-semana__dia--folga .aj-semana__numero{color:var(--tinta-apagada)}
.aj-semana__dia--hoje .aj-semana__cabeca{padding:0 0 var(--espaco-8) var(--espaco-6);border-bottom:2px solid var(--verde)}
.aj-semana__dia--hoje .aj-semana__sigla{font-weight:700;color:var(--verde)}
.aj-semana__dia--hoje .aj-semana__numero{display:inline-flex;align-items:center;justify-content:center;width:36px;height:36px;border-radius:var(--raio-pilula);background:var(--verde);color:var(--sobre-verde);font-size:16px}
.aj-semana__vazio{margin:0;padding:var(--espaco-12) var(--espaco-8);border:1px dashed var(--borda-campo-suave);border-radius:var(--raio-12);font-size:13px;color:var(--tinta-apagada);text-align:center}

/* Seção de um dia (tablet; também o Dia do tablet e a Semana do mobile): o dia numa coluna de 64px e os cartões numa
   grade de duas colunas (uma no mobile). Hoje com borda verde e o número cheio. */
.aj-secao-dia{display:flex;gap:var(--espaco-14);padding:var(--espaco-12);border:1px solid var(--borda);border-radius:var(--raio-18);background:var(--superficie)}
.aj-secao-dia--hoje{border-color:var(--verde)}
.aj-secao-dia__cabeca{flex:0 0 64px;display:flex;flex-direction:column;align-items:center;gap:var(--espaco-2);margin:0;padding-top:var(--espaco-4);font:400 15px/1.5 var(--font-sans)}
.aj-secao-dia__sigla{font-size:12.5px;font-weight:700;color:var(--tinta-apagada)}
.aj-secao-dia--hoje .aj-secao-dia__sigla{color:var(--verde)}
.aj-secao-dia__numero{display:inline-flex;align-items:center;justify-content:center;width:42px;height:42px;border-radius:var(--raio-pilula);background:var(--fundo);color:var(--tinta);font-size:19px;font-weight:600;font-variant-numeric:tabular-nums}
.aj-secao-dia--hoje .aj-secao-dia__numero{background:var(--verde);color:var(--sobre-verde)}
.aj-secao-dia__eventos{flex:1 1 auto;min-width:0;display:grid;grid-template-columns:repeat(2,minmax(0,1fr));align-content:start;gap:var(--espaco-8)}
@media (max-width:760px){.aj-secao-dia__eventos{grid-template-columns:minmax(0,1fr)}}
[data-visao="desktop"] .aj-secao-dia__eventos,[data-visao="tablet"] .aj-secao-dia__eventos{grid-template-columns:repeat(2,minmax(0,1fr))}
[data-visao="mobile"] .aj-secao-dia__eventos{grid-template-columns:minmax(0,1fr)}
.aj-secao-dia__vazio{grid-column:1/-1;margin:0;padding:var(--espaco-14) var(--espaco-4);font-size:14px;color:var(--tinta-apagada)}

/* Dia (desktop): o cabeçalho com o número (cheio de verde quando é hoje), o nome do dia e o total, e a régua. */
.aj-dia{display:flex;flex-direction:column;gap:var(--espaco-8);padding:var(--espaco-10) var(--espaco-12) var(--espaco-14);border:1px solid var(--borda);border-radius:var(--raio-14);background:var(--superficie);box-shadow:var(--sombra-painel)}
.aj-dia__cabeca{display:flex;align-items:center;gap:var(--espaco-10);margin:0;padding:0 0 var(--espaco-8);border-bottom:1px solid var(--borda-suave);font:600 15px/1.5 var(--font-sans)}
.aj-dia__numero{display:inline-flex;align-items:center;justify-content:center;width:36px;height:36px;border-radius:var(--raio-pilula);background:var(--fundo);color:var(--tinta);font-size:16px;font-weight:600;font-variant-numeric:tabular-nums}
.aj-dia--hoje .aj-dia__numero{background:var(--verde);color:var(--sobre-verde)}
.aj-dia__total{margin-left:auto;font-size:13px;font-weight:500;color:var(--tinta-apagada)}

/* Régua de horas: 56px por hora, numa grade de quartos de hora de 14px e 12 colunas. Os rótulos ficam numa coluna de
   52px, subidos 8px para cair na linha da hora; as linhas das horas são o fundo da grade, a cada 56px. O cartão
   ocupa a largura toda a 2px das linhas e 4px dos lados; a linha de início, os quartos e a parte da largura vêm das
   famílias abaixo (eventos simultâneos dividem as 12 colunas em até quatro partes). Sem atributo style. */
.aj-regua{display:flex;gap:var(--espaco-8)}
.aj-regua__horas{flex:0 0 52px;display:flex;flex-direction:column}
.aj-regua__hora{flex:0 0 56px;height:56px;transform:translateY(-8px);font-size:12px;font-weight:600;color:var(--tinta-apagada);text-align:right;font-variant-numeric:tabular-nums}
.aj-regua__grade{flex:1 1 auto;min-width:0;display:grid;grid-template-columns:repeat(12,minmax(0,1fr));grid-auto-rows:14px;background-image:linear-gradient(to bottom,var(--borda-suave) 1px,transparent 1px);background-size:100% 56px}
.aj-regua__evento{grid-column:1/-1;width:auto;min-height:0;margin:2px 4px;overflow:hidden}
/* Famílias da régua, geradas por roteiro (gerar-regua-9f.py): a grade de n horas (n de 1 a 24), o quarto em que o
   cartão começa (0 a 95), quantos quartos ele ocupa (2 a 96) e a parte k de n da largura (n de 2 a 4). */
.aj-regua__grade--horas-1{grid-template-rows:repeat(4,14px)}
.aj-regua__grade--horas-2{grid-template-rows:repeat(8,14px)}
.aj-regua__grade--horas-3{grid-template-rows:repeat(12,14px)}
.aj-regua__grade--horas-4{grid-template-rows:repeat(16,14px)}
.aj-regua__grade--horas-5{grid-template-rows:repeat(20,14px)}
.aj-regua__grade--horas-6{grid-template-rows:repeat(24,14px)}
.aj-regua__grade--horas-7{grid-template-rows:repeat(28,14px)}
.aj-regua__grade--horas-8{grid-template-rows:repeat(32,14px)}
.aj-regua__grade--horas-9{grid-template-rows:repeat(36,14px)}
.aj-regua__grade--horas-10{grid-template-rows:repeat(40,14px)}
.aj-regua__grade--horas-11{grid-template-rows:repeat(44,14px)}
.aj-regua__grade--horas-12{grid-template-rows:repeat(48,14px)}
.aj-regua__grade--horas-13{grid-template-rows:repeat(52,14px)}
.aj-regua__grade--horas-14{grid-template-rows:repeat(56,14px)}
.aj-regua__grade--horas-15{grid-template-rows:repeat(60,14px)}
.aj-regua__grade--horas-16{grid-template-rows:repeat(64,14px)}
.aj-regua__grade--horas-17{grid-template-rows:repeat(68,14px)}
.aj-regua__grade--horas-18{grid-template-rows:repeat(72,14px)}
.aj-regua__grade--horas-19{grid-template-rows:repeat(76,14px)}
.aj-regua__grade--horas-20{grid-template-rows:repeat(80,14px)}
.aj-regua__grade--horas-21{grid-template-rows:repeat(84,14px)}
.aj-regua__grade--horas-22{grid-template-rows:repeat(88,14px)}
.aj-regua__grade--horas-23{grid-template-rows:repeat(92,14px)}
.aj-regua__grade--horas-24{grid-template-rows:repeat(96,14px)}
.aj-regua__evento--inicio-0{grid-row-start:1}.aj-regua__evento--inicio-1{grid-row-start:2}.aj-regua__evento--inicio-2{grid-row-start:3}.aj-regua__evento--inicio-3{grid-row-start:4}
.aj-regua__evento--inicio-4{grid-row-start:5}.aj-regua__evento--inicio-5{grid-row-start:6}.aj-regua__evento--inicio-6{grid-row-start:7}.aj-regua__evento--inicio-7{grid-row-start:8}
.aj-regua__evento--inicio-8{grid-row-start:9}.aj-regua__evento--inicio-9{grid-row-start:10}.aj-regua__evento--inicio-10{grid-row-start:11}.aj-regua__evento--inicio-11{grid-row-start:12}
.aj-regua__evento--inicio-12{grid-row-start:13}.aj-regua__evento--inicio-13{grid-row-start:14}.aj-regua__evento--inicio-14{grid-row-start:15}.aj-regua__evento--inicio-15{grid-row-start:16}
.aj-regua__evento--inicio-16{grid-row-start:17}.aj-regua__evento--inicio-17{grid-row-start:18}.aj-regua__evento--inicio-18{grid-row-start:19}.aj-regua__evento--inicio-19{grid-row-start:20}
.aj-regua__evento--inicio-20{grid-row-start:21}.aj-regua__evento--inicio-21{grid-row-start:22}.aj-regua__evento--inicio-22{grid-row-start:23}.aj-regua__evento--inicio-23{grid-row-start:24}
.aj-regua__evento--inicio-24{grid-row-start:25}.aj-regua__evento--inicio-25{grid-row-start:26}.aj-regua__evento--inicio-26{grid-row-start:27}.aj-regua__evento--inicio-27{grid-row-start:28}
.aj-regua__evento--inicio-28{grid-row-start:29}.aj-regua__evento--inicio-29{grid-row-start:30}.aj-regua__evento--inicio-30{grid-row-start:31}.aj-regua__evento--inicio-31{grid-row-start:32}
.aj-regua__evento--inicio-32{grid-row-start:33}.aj-regua__evento--inicio-33{grid-row-start:34}.aj-regua__evento--inicio-34{grid-row-start:35}.aj-regua__evento--inicio-35{grid-row-start:36}
.aj-regua__evento--inicio-36{grid-row-start:37}.aj-regua__evento--inicio-37{grid-row-start:38}.aj-regua__evento--inicio-38{grid-row-start:39}.aj-regua__evento--inicio-39{grid-row-start:40}
.aj-regua__evento--inicio-40{grid-row-start:41}.aj-regua__evento--inicio-41{grid-row-start:42}.aj-regua__evento--inicio-42{grid-row-start:43}.aj-regua__evento--inicio-43{grid-row-start:44}
.aj-regua__evento--inicio-44{grid-row-start:45}.aj-regua__evento--inicio-45{grid-row-start:46}.aj-regua__evento--inicio-46{grid-row-start:47}.aj-regua__evento--inicio-47{grid-row-start:48}
.aj-regua__evento--inicio-48{grid-row-start:49}.aj-regua__evento--inicio-49{grid-row-start:50}.aj-regua__evento--inicio-50{grid-row-start:51}.aj-regua__evento--inicio-51{grid-row-start:52}
.aj-regua__evento--inicio-52{grid-row-start:53}.aj-regua__evento--inicio-53{grid-row-start:54}.aj-regua__evento--inicio-54{grid-row-start:55}.aj-regua__evento--inicio-55{grid-row-start:56}
.aj-regua__evento--inicio-56{grid-row-start:57}.aj-regua__evento--inicio-57{grid-row-start:58}.aj-regua__evento--inicio-58{grid-row-start:59}.aj-regua__evento--inicio-59{grid-row-start:60}
.aj-regua__evento--inicio-60{grid-row-start:61}.aj-regua__evento--inicio-61{grid-row-start:62}.aj-regua__evento--inicio-62{grid-row-start:63}.aj-regua__evento--inicio-63{grid-row-start:64}
.aj-regua__evento--inicio-64{grid-row-start:65}.aj-regua__evento--inicio-65{grid-row-start:66}.aj-regua__evento--inicio-66{grid-row-start:67}.aj-regua__evento--inicio-67{grid-row-start:68}
.aj-regua__evento--inicio-68{grid-row-start:69}.aj-regua__evento--inicio-69{grid-row-start:70}.aj-regua__evento--inicio-70{grid-row-start:71}.aj-regua__evento--inicio-71{grid-row-start:72}
.aj-regua__evento--inicio-72{grid-row-start:73}.aj-regua__evento--inicio-73{grid-row-start:74}.aj-regua__evento--inicio-74{grid-row-start:75}.aj-regua__evento--inicio-75{grid-row-start:76}
.aj-regua__evento--inicio-76{grid-row-start:77}.aj-regua__evento--inicio-77{grid-row-start:78}.aj-regua__evento--inicio-78{grid-row-start:79}.aj-regua__evento--inicio-79{grid-row-start:80}
.aj-regua__evento--inicio-80{grid-row-start:81}.aj-regua__evento--inicio-81{grid-row-start:82}.aj-regua__evento--inicio-82{grid-row-start:83}.aj-regua__evento--inicio-83{grid-row-start:84}
.aj-regua__evento--inicio-84{grid-row-start:85}.aj-regua__evento--inicio-85{grid-row-start:86}.aj-regua__evento--inicio-86{grid-row-start:87}.aj-regua__evento--inicio-87{grid-row-start:88}
.aj-regua__evento--inicio-88{grid-row-start:89}.aj-regua__evento--inicio-89{grid-row-start:90}.aj-regua__evento--inicio-90{grid-row-start:91}.aj-regua__evento--inicio-91{grid-row-start:92}
.aj-regua__evento--inicio-92{grid-row-start:93}.aj-regua__evento--inicio-93{grid-row-start:94}.aj-regua__evento--inicio-94{grid-row-start:95}.aj-regua__evento--inicio-95{grid-row-start:96}
.aj-regua__evento--quartos-2{grid-row-end:span 2}.aj-regua__evento--quartos-3{grid-row-end:span 3}.aj-regua__evento--quartos-4{grid-row-end:span 4}
.aj-regua__evento--quartos-5{grid-row-end:span 5}.aj-regua__evento--quartos-6{grid-row-end:span 6}.aj-regua__evento--quartos-7{grid-row-end:span 7}.aj-regua__evento--quartos-8{grid-row-end:span 8}
.aj-regua__evento--quartos-9{grid-row-end:span 9}.aj-regua__evento--quartos-10{grid-row-end:span 10}.aj-regua__evento--quartos-11{grid-row-end:span 11}.aj-regua__evento--quartos-12{grid-row-end:span 12}
.aj-regua__evento--quartos-13{grid-row-end:span 13}.aj-regua__evento--quartos-14{grid-row-end:span 14}.aj-regua__evento--quartos-15{grid-row-end:span 15}.aj-regua__evento--quartos-16{grid-row-end:span 16}
.aj-regua__evento--quartos-17{grid-row-end:span 17}.aj-regua__evento--quartos-18{grid-row-end:span 18}.aj-regua__evento--quartos-19{grid-row-end:span 19}.aj-regua__evento--quartos-20{grid-row-end:span 20}
.aj-regua__evento--quartos-21{grid-row-end:span 21}.aj-regua__evento--quartos-22{grid-row-end:span 22}.aj-regua__evento--quartos-23{grid-row-end:span 23}.aj-regua__evento--quartos-24{grid-row-end:span 24}
.aj-regua__evento--quartos-25{grid-row-end:span 25}.aj-regua__evento--quartos-26{grid-row-end:span 26}.aj-regua__evento--quartos-27{grid-row-end:span 27}.aj-regua__evento--quartos-28{grid-row-end:span 28}
.aj-regua__evento--quartos-29{grid-row-end:span 29}.aj-regua__evento--quartos-30{grid-row-end:span 30}.aj-regua__evento--quartos-31{grid-row-end:span 31}.aj-regua__evento--quartos-32{grid-row-end:span 32}
.aj-regua__evento--quartos-33{grid-row-end:span 33}.aj-regua__evento--quartos-34{grid-row-end:span 34}.aj-regua__evento--quartos-35{grid-row-end:span 35}.aj-regua__evento--quartos-36{grid-row-end:span 36}
.aj-regua__evento--quartos-37{grid-row-end:span 37}.aj-regua__evento--quartos-38{grid-row-end:span 38}.aj-regua__evento--quartos-39{grid-row-end:span 39}.aj-regua__evento--quartos-40{grid-row-end:span 40}
.aj-regua__evento--quartos-41{grid-row-end:span 41}.aj-regua__evento--quartos-42{grid-row-end:span 42}.aj-regua__evento--quartos-43{grid-row-end:span 43}.aj-regua__evento--quartos-44{grid-row-end:span 44}
.aj-regua__evento--quartos-45{grid-row-end:span 45}.aj-regua__evento--quartos-46{grid-row-end:span 46}.aj-regua__evento--quartos-47{grid-row-end:span 47}.aj-regua__evento--quartos-48{grid-row-end:span 48}
.aj-regua__evento--quartos-49{grid-row-end:span 49}.aj-regua__evento--quartos-50{grid-row-end:span 50}.aj-regua__evento--quartos-51{grid-row-end:span 51}.aj-regua__evento--quartos-52{grid-row-end:span 52}
.aj-regua__evento--quartos-53{grid-row-end:span 53}.aj-regua__evento--quartos-54{grid-row-end:span 54}.aj-regua__evento--quartos-55{grid-row-end:span 55}.aj-regua__evento--quartos-56{grid-row-end:span 56}
.aj-regua__evento--quartos-57{grid-row-end:span 57}.aj-regua__evento--quartos-58{grid-row-end:span 58}.aj-regua__evento--quartos-59{grid-row-end:span 59}.aj-regua__evento--quartos-60{grid-row-end:span 60}
.aj-regua__evento--quartos-61{grid-row-end:span 61}.aj-regua__evento--quartos-62{grid-row-end:span 62}.aj-regua__evento--quartos-63{grid-row-end:span 63}.aj-regua__evento--quartos-64{grid-row-end:span 64}
.aj-regua__evento--quartos-65{grid-row-end:span 65}.aj-regua__evento--quartos-66{grid-row-end:span 66}.aj-regua__evento--quartos-67{grid-row-end:span 67}.aj-regua__evento--quartos-68{grid-row-end:span 68}
.aj-regua__evento--quartos-69{grid-row-end:span 69}.aj-regua__evento--quartos-70{grid-row-end:span 70}.aj-regua__evento--quartos-71{grid-row-end:span 71}.aj-regua__evento--quartos-72{grid-row-end:span 72}
.aj-regua__evento--quartos-73{grid-row-end:span 73}.aj-regua__evento--quartos-74{grid-row-end:span 74}.aj-regua__evento--quartos-75{grid-row-end:span 75}.aj-regua__evento--quartos-76{grid-row-end:span 76}
.aj-regua__evento--quartos-77{grid-row-end:span 77}.aj-regua__evento--quartos-78{grid-row-end:span 78}.aj-regua__evento--quartos-79{grid-row-end:span 79}.aj-regua__evento--quartos-80{grid-row-end:span 80}
.aj-regua__evento--quartos-81{grid-row-end:span 81}.aj-regua__evento--quartos-82{grid-row-end:span 82}.aj-regua__evento--quartos-83{grid-row-end:span 83}.aj-regua__evento--quartos-84{grid-row-end:span 84}
.aj-regua__evento--quartos-85{grid-row-end:span 85}.aj-regua__evento--quartos-86{grid-row-end:span 86}.aj-regua__evento--quartos-87{grid-row-end:span 87}.aj-regua__evento--quartos-88{grid-row-end:span 88}
.aj-regua__evento--quartos-89{grid-row-end:span 89}.aj-regua__evento--quartos-90{grid-row-end:span 90}.aj-regua__evento--quartos-91{grid-row-end:span 91}.aj-regua__evento--quartos-92{grid-row-end:span 92}
.aj-regua__evento--quartos-93{grid-row-end:span 93}.aj-regua__evento--quartos-94{grid-row-end:span 94}.aj-regua__evento--quartos-95{grid-row-end:span 95}.aj-regua__evento--quartos-96{grid-row-end:span 96}
.aj-regua__evento--parte-1-de-2{grid-column:1/7}.aj-regua__evento--parte-2-de-2{grid-column:7/13}
.aj-regua__evento--parte-1-de-3{grid-column:1/5}.aj-regua__evento--parte-2-de-3{grid-column:5/9}.aj-regua__evento--parte-3-de-3{grid-column:9/13}
.aj-regua__evento--parte-1-de-4{grid-column:1/4}.aj-regua__evento--parte-2-de-4{grid-column:4/7}.aj-regua__evento--parte-3-de-4{grid-column:7/10}.aj-regua__evento--parte-4-de-4{grid-column:10/13}

/* Mês (desktop e tablet): a linha dos dias da semana e a grade das semanas (4 a 6 linhas). No desktop a grade tem
   700px de largura mínima e rola de lado abaixo disso; no tablet não tem mínimo. Dia de outro mês apagado e sem
   eventos; hoje com o número num círculo; até três eventos e o "+N". */
.aj-mes{display:flex;flex-direction:column;gap:var(--espaco-6);padding:var(--espaco-6);border:1px solid var(--borda);border-radius:var(--raio-14);background:var(--superficie);box-shadow:var(--sombra-painel)}
.aj-mes__cabeca,.aj-mes__grade{display:grid;grid-template-columns:repeat(7,minmax(0,1fr));gap:var(--espaco-4)}
.aj-mes__cabeca>span{padding:var(--espaco-4) var(--espaco-8);font-size:12.5px;font-weight:600;color:var(--tinta-apagada)}
.aj-mes__rolagem{overflow-x:auto}
@media (min-width:1200px){.aj-mes__grade{min-width:700px}}
[data-visao="desktop"] .aj-mes__grade{min-width:700px}
[data-visao="tablet"] .aj-mes__grade,[data-visao="mobile"] .aj-mes__grade{min-width:0}
.aj-mes__celula{display:flex;flex-direction:column;gap:3px;min-width:0;min-height:96px;padding:5px var(--espaco-6);border:1px solid var(--borda-suave);border-radius:var(--raio-10);background:var(--superficie)}
.aj-mes__celula--fora{display:block;padding:var(--espaco-6) var(--espaco-8);border:0;background:var(--superficie-campo)}
.aj-mes__numero{padding:3px var(--espaco-2);font-size:13px;font-weight:600;color:var(--tinta);font-variant-numeric:tabular-nums}
.aj-mes__celula--fora .aj-mes__numero{padding:0;font-weight:500;color:var(--tinta-icone)}
.aj-mes__numero--hoje{align-self:flex-start;display:inline-flex;align-items:center;justify-content:center;width:26px;height:26px;padding:0;border-radius:var(--raio-pilula);background:var(--verde);color:var(--sobre-verde)}
.aj-mes__mais{align-self:flex-start;padding:0 var(--espaco-6);border:0;border-radius:6px;background:transparent;color:var(--tinta-apagada);font:600 12px/1.5 var(--font-sans);cursor:pointer}
.aj-mes__mais:hover{background:var(--superficie-recuada)}
.aj-mes__mais:focus-visible{outline:2px solid var(--verde);outline-offset:2px}

/* Barra superior da agenda (mobile): o botão de filtros entre o título e o avatar (44px, raio 12, ícone de 22px) e a
   linha do mês, com o nome em 700 e as setas de 40px nas pontas. */
.aj-barra-superior__acao{flex:0 0 auto;width:var(--alvo-toque);min-height:var(--alvo-toque);border-radius:var(--raio-12)}
.aj-barra-superior__acao svg{width:22px;height:22px;stroke-width:1.75}
.aj-barra-superior__periodo{display:flex;align-items:center;justify-content:space-between;gap:var(--espaco-8)}
.aj-barra-superior__periodo>strong{font-size:15px;font-weight:700}
.aj-barra-superior__setas{display:flex;gap:var(--espaco-2)}
.aj-barra-superior__setas .aj-botao{width:40px;min-height:40px}

/* Faixa de dias (mobile): sigla, número e um ponto por evento; o escolhido (aria-pressed) cheio de verde, hoje com o
   aro verde. Com --mes, a grade compacta do mês: a linha de siglas é o primeiro filho (aj-faixa-dias__cabeca, que
   ocupa a largura toda com as suas sete colunas), os dias sem sigla e o dia de outro mês apagado. */
.aj-faixa-dias,.aj-faixa-dias__cabeca{display:grid;grid-template-columns:repeat(7,minmax(0,1fr));gap:var(--espaco-2)}
.aj-faixa-dias__cabeca{font-size:11.5px;font-weight:600;color:var(--tinta-apagada);text-align:center}
.aj-faixa-dias--mes>.aj-faixa-dias__cabeca{grid-column:1/-1}
.aj-faixa-dias__dia{display:flex;flex-direction:column;align-items:center;gap:var(--espaco-4);min-width:0;min-height:64px;padding:var(--espaco-4) 0;border:0;border-radius:var(--raio-12);background:transparent;color:var(--tinta);font:400 15px/1.5 var(--font-sans);cursor:pointer}
.aj-faixa-dias__dia:focus-visible{outline:2px solid var(--verde);outline-offset:2px}
.aj-faixa-dias__sigla{font-size:11.5px;font-weight:600;color:var(--tinta-apagada)}
.aj-faixa-dias__numero{display:inline-flex;align-items:center;justify-content:center;width:34px;height:34px;border:1.5px solid transparent;border-radius:var(--raio-pilula);font-size:15px;font-weight:600;font-variant-numeric:tabular-nums}
.aj-faixa-dias__dia--hoje .aj-faixa-dias__numero{border-color:var(--verde);color:var(--verde)}
.aj-faixa-dias__dia[aria-pressed="true"] .aj-faixa-dias__numero{border-color:var(--verde);background:var(--verde);color:var(--sobre-verde)}
.aj-faixa-dias--mes .aj-faixa-dias__dia{min-height:52px}
.aj-faixa-dias__dia--fora .aj-faixa-dias__numero{font-weight:500;color:var(--tinta-icone)}

/* Pontos de evento (faixa e grade compacta): um por evento, na cor do tipo; a Audiência num dourado mais escuro. A
   fileira tem 5px de altura mesmo sem pontos, para os dias terem a mesma altura. */
.aj-pontos-evento{display:flex;gap:3px;height:5px}
.aj-ponto-evento{width:5px;height:5px;border-radius:var(--raio-pilula);background:var(--tinta-icone)}
.aj-ponto-evento--reuniao{background:var(--evento-reuniao-rotulo)}
.aj-ponto-evento--audiencia{background:#B08A45}
.aj-ponto-evento--prazo{background:var(--evento-prazo-rotulo)}
.aj-ponto-evento--outro{background:var(--evento-outro-rotulo)}

/* Título do dia escolhido (mobile), com o total na mesma linha de base, e o estado vazio com ícone e dica. */
.aj-dia-escolhido{display:flex;align-items:baseline;justify-content:space-between;gap:var(--espaco-8);padding:var(--espaco-4) var(--espaco-2) 0}
.aj-dia-escolhido>h2{margin:0;font:700 16px/1.5 var(--font-sans)}
.aj-dia-escolhido__total{font-size:13px;color:var(--tinta-apagada)}
.aj-estado-lista__icone{width:28px;height:28px;fill:none;stroke:var(--tinta-icone);stroke-width:1.5;stroke-linecap:round;stroke-linejoin:round}
.aj-estado-lista__dica{font-size:13.5px}

/* Formulário de evento. Lado a lado com o painel médio (desktop): a coluna principal com base de 420px e quase todo o
   espaço livre; o painel (vínculos e participantes) com base de 380px. */
@media (min-width:1200px){
  .aj-lado-a-lado--painel-medio .aj-lado-a-lado__lista{flex:999 1 420px}
  .aj-lado-a-lado--painel-medio .aj-lado-a-lado__painel{flex:1 1 380px;min-width:0}
}
[data-visao="desktop"] .aj-lado-a-lado--painel-medio .aj-lado-a-lado__lista{flex:999 1 420px}
[data-visao="desktop"] .aj-lado-a-lado--painel-medio .aj-lado-a-lado__painel{flex:1 1 380px;min-width:0}
[data-visao="tablet"] .aj-lado-a-lado--painel-medio :is(.aj-lado-a-lado__lista,.aj-lado-a-lado__painel),
[data-visao="mobile"] .aj-lado-a-lado--painel-medio :is(.aj-lado-a-lado__lista,.aj-lado-a-lado__painel){flex:0 0 auto}

/* Linha de campos: no desktop, campos lado a lado com larguras de base (170px; o título com 260px e o dobro do espaço
   livre, o tipo com 160px, a hora com 130px) e quebra; no tablet, três colunas iguais; no mobile, duas, com 16px entre
   as linhas, e o campo marcado na linha inteira. O campo pode vir dentro de um invólucro de visão. */
.aj-linha-de-campos{display:flex;flex-wrap:wrap;gap:var(--espaco-10)}
.aj-linha-de-campos>*,.aj-linha-de-campos>:is(.aj-so-desktop,.aj-so-tablet,.aj-so-mobile,.aj-desktop-tablet,.aj-tablet-mobile,.aj-desktop-mobile)>*{flex:1 1 170px;min-width:0}
.aj-linha-de-campos .aj-linha-de-campos__largo{flex:2 1 260px}
.aj-linha-de-campos .aj-linha-de-campos__medio{flex-basis:160px}
.aj-linha-de-campos .aj-linha-de-campos__curto{flex-basis:130px}
@media (max-width:1199px){.aj-linha-de-campos{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:var(--espaco-12)}}
@media (max-width:760px){
  .aj-linha-de-campos{grid-template-columns:repeat(2,minmax(0,1fr));gap:var(--espaco-16) var(--espaco-10)}
  .aj-linha-de-campos .aj-linha-de-campos__inteiro-no-mobile{grid-column:1/-1}
}
[data-visao="desktop"] .aj-linha-de-campos{display:flex;gap:var(--espaco-10)}
[data-visao="tablet"] .aj-linha-de-campos{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:var(--espaco-12)}
[data-visao="mobile"] .aj-linha-de-campos{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:var(--espaco-16) var(--espaco-10)}
[data-visao="desktop"] .aj-linha-de-campos .aj-linha-de-campos__inteiro-no-mobile,[data-visao="tablet"] .aj-linha-de-campos .aj-linha-de-campos__inteiro-no-mobile{grid-column:auto}
[data-visao="mobile"] .aj-linha-de-campos .aj-linha-de-campos__inteiro-no-mobile{grid-column:1/-1}

/* Ajuda com ícone de 14px, colada na linha de campos de cima (o fuso do escritório, desktop). */
.aj-ajuda--com-icone{display:flex;align-items:center;gap:var(--espaco-6)}
.aj-ajuda--com-icone svg{flex:0 0 auto;width:14px;height:14px;fill:none;stroke:currentColor;stroke-width:2;stroke-linecap:round;stroke-linejoin:round}
.aj-ajuda--colada{margin-top:calc(-1 * var(--espaco-6))}

/* Painéis do formulário de evento: vão de 14px entre os blocos (Vínculos e Participantes). No mobile o painel deixa de
   ser caixa (sem borda, respiro, fundo e sombra; o título vai num invólucro aj-desktop-tablet) e os campos ficam a 16px
   uns dos outros, dentro do painel e entre os painéis (o vão do conteúdo da tela). */
:root{--aj-pagina-gap-solto:var(--espaco-14)}
@media (max-width:760px){:root{--aj-pagina-gap-solto:var(--espaco-16)}}
[data-visao="desktop"],[data-visao="tablet"]{--aj-pagina-gap-solto:var(--espaco-14)}
[data-visao="mobile"]{--aj-pagina-gap-solto:var(--espaco-16)}
.aj-painel--folgado{gap:var(--espaco-14)}
@media (max-width:760px){.aj-painel.aj-painel--solto-no-mobile{gap:var(--espaco-16);padding:0;border:0;border-radius:0;background:transparent;box-shadow:none}}
[data-visao="mobile"] .aj-painel.aj-painel--solto-no-mobile{gap:var(--espaco-16);padding:0;border:0;border-radius:0;background:transparent;box-shadow:none}
[data-visao="desktop"] .aj-painel.aj-painel--solto-no-mobile,[data-visao="tablet"] .aj-painel.aj-painel--solto-no-mobile{gap:var(--espaco-12);padding:var(--aj-cartao-padding);border:1px solid var(--borda);border-radius:var(--aj-raio-cartao);background:var(--superficie);box-shadow:var(--sombra-1)}
[data-visao="desktop"] .aj-painel--principal.aj-painel--solto-no-mobile,[data-visao="tablet"] .aj-painel--principal.aj-painel--solto-no-mobile{box-shadow:var(--sombra-painel)}
[data-visao="desktop"] .aj-painel--folgado.aj-painel--solto-no-mobile,[data-visao="tablet"] .aj-painel--folgado.aj-painel--solto-no-mobile{gap:var(--espaco-14)}
.aj-app:has(.aj-painel--solto-no-mobile) .aj-app__conteudo{gap:var(--aj-pagina-gap-solto)}

/* Vínculo com o nome visível (cliente, processo e atividade do evento): 10px entre o nome, o escolhido e a busca no
   desktop, 8px no tablet e no mobile. O escolhido com a sigla num círculo branco e o remover em verde de sucesso; no
   tablet o escolhido tem a altura do campo; no tablet e no mobile o remover tem o alvo de toque. */
.aj-vinculo--rotulado{gap:var(--espaco-10)}
@media (max-width:1199px){.aj-vinculo--rotulado{gap:var(--espaco-8)}}
[data-visao="desktop"] .aj-vinculo--rotulado{gap:var(--espaco-10)}
[data-visao="tablet"] .aj-vinculo--rotulado,[data-visao="mobile"] .aj-vinculo--rotulado{gap:var(--espaco-8)}
.aj-vinculo__rotulo{font:600 var(--aj-fonte-rotulo)/1.4 var(--font-sans);color:var(--tinta-rotulo)}
.aj-vinculo__rotulo small{font-size:inherit;font-weight:400;color:var(--tinta-apagada)}
.aj-vinculo__escolhido--com-avatar{gap:var(--espaco-12);padding:var(--espaco-6) var(--espaco-6) var(--espaco-6) var(--espaco-8);border-radius:var(--raio-14)}
.aj-vinculo__escolhido--com-avatar .aj-botao{color:var(--sucesso)}
.aj-vinculo__avatar{flex:0 0 auto;display:inline-flex;align-items:center;justify-content:center;width:32px;height:32px;border-radius:var(--raio-pilula);background:var(--superficie);color:var(--sucesso);font:700 12px/1 var(--font-sans)}
@media (max-width:1199px){
  .aj-vinculo__escolhido--com-avatar{gap:var(--espaco-10)}
  .aj-vinculo__escolhido--com-avatar .aj-botao--icone{width:var(--alvo-toque);min-height:var(--alvo-toque)}
}
@media (min-width:761px) and (max-width:1199px){
  .aj-vinculo__escolhido--com-avatar{min-height:var(--campo-toque);padding:2px 2px 2px var(--espaco-8);border-radius:var(--raio-12)}
  .aj-vinculo__escolhido--com-avatar .aj-vinculo__avatar{width:30px;height:30px;font-size:11px}
}
@media (max-width:760px){.aj-vinculo__escolhido--com-avatar{padding:var(--espaco-4) var(--espaco-4) var(--espaco-4) var(--espaco-8)}}
[data-visao="desktop"] .aj-vinculo__escolhido--com-avatar{gap:var(--espaco-12);min-height:0;padding:var(--espaco-6) var(--espaco-6) var(--espaco-6) var(--espaco-8);border-radius:var(--raio-14)}
[data-visao="desktop"] .aj-vinculo__escolhido--com-avatar .aj-botao--icone{width:var(--aj-controle);min-height:var(--aj-controle)}
[data-visao="tablet"] .aj-vinculo__escolhido--com-avatar,[data-visao="mobile"] .aj-vinculo__escolhido--com-avatar{gap:var(--espaco-10)}
[data-visao="tablet"] .aj-vinculo__escolhido--com-avatar .aj-botao--icone,[data-visao="mobile"] .aj-vinculo__escolhido--com-avatar .aj-botao--icone{width:var(--alvo-toque);min-height:var(--alvo-toque)}
[data-visao="tablet"] .aj-vinculo__escolhido--com-avatar{min-height:var(--campo-toque);padding:2px 2px 2px var(--espaco-8);border-radius:var(--raio-12)}
[data-visao="tablet"] .aj-vinculo__escolhido--com-avatar .aj-vinculo__avatar{width:30px;height:30px;font-size:11px}
[data-visao="desktop"] .aj-vinculo__escolhido--com-avatar .aj-vinculo__avatar,[data-visao="mobile"] .aj-vinculo__escolhido--com-avatar .aj-vinculo__avatar{width:32px;height:32px;font-size:12px}
[data-visao="mobile"] .aj-vinculo__escolhido--com-avatar{min-height:0;padding:var(--espaco-4) var(--espaco-4) var(--espaco-4) var(--espaco-8);border-radius:var(--raio-14)}

/* Escolha única em ladrilhos (o tipo do evento no tablet e no mobile): um rádio escondido em cada ladrilho; o
   escolhido em verde; o foco do rádio contorna o ladrilho. Quatro por linha e 48px no tablet, dois e 46px no mobile.
   O ladrilho leva o seletor com o pai (0,2,0) para vencer a regra de rótulo com rádio da folha da aplicação. */
.aj-escolha{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:var(--espaco-8)}
.aj-escolha>.aj-escolha__opcao{position:relative;display:flex;align-items:center;justify-content:center;gap:var(--espaco-8);min-width:0;min-height:48px;border:1px solid var(--borda-campo);border-radius:var(--raio-12);background:var(--superficie);color:var(--tinta-rotulo);font:600 15px/1.5 var(--font-sans);text-align:center;cursor:pointer}
.aj-escolha__opcao input{position:absolute;opacity:0;pointer-events:none}
.aj-escolha>.aj-escolha__opcao:has(input:checked){border-color:var(--verde);background:var(--verde);color:var(--sobre-verde)}
.aj-escolha>.aj-escolha__opcao:has(input:focus-visible){outline:2px solid var(--verde);outline-offset:2px}
@media (max-width:760px){
  .aj-escolha{grid-template-columns:repeat(2,minmax(0,1fr))}
  .aj-escolha>.aj-escolha__opcao{min-height:var(--controle-toque)}
}
[data-visao="desktop"] .aj-escolha,[data-visao="tablet"] .aj-escolha{grid-template-columns:repeat(4,minmax(0,1fr))}
[data-visao="desktop"] .aj-escolha>.aj-escolha__opcao,[data-visao="tablet"] .aj-escolha>.aj-escolha__opcao{min-height:48px}
[data-visao="mobile"] .aj-escolha{grid-template-columns:repeat(2,minmax(0,1fr))}
[data-visao="mobile"] .aj-escolha>.aj-escolha__opcao{min-height:var(--controle-toque)}

/* Pessoa marcável em chip (participantes do evento no tablet e no mobile): avatar à esquerda e a caixa de marcar
   escondida; marcada, o fundo de seleção com a borda #9DB898. 46px e avatar de 32px no tablet; 44px e avatar de 30px no
   mobile. O chip leva as duas classes no seletor (0,2,0) para vencer a regra de rótulo com caixa da folha da
   aplicação. A opção travada (quem cria o evento) não tem cursor de clique; a nota "(você)" do desktop em 13px. */
.aj-chip.aj-chip--pessoa{position:relative;display:inline-flex;align-items:center;gap:var(--espaco-8);min-height:var(--controle-toque);padding:0 var(--espaco-16) 0 var(--espaco-6);color:var(--tinta-rotulo);font:600 15px/1.5 var(--font-sans)}
.aj-chip--pessoa input{position:absolute;opacity:0;pointer-events:none}
.aj-chip.aj-chip--pessoa:has(input:checked){border-color:#9DB898;background:var(--selecao-fundo);color:var(--verde-escuro)}
.aj-chip.aj-chip--pessoa:has(input:focus-visible){outline:2px solid var(--verde);outline-offset:2px}
.aj-chip--pessoa>.aj-avatar{width:32px;height:32px;font-size:11px}
.aj-chip--pessoa:has(input:disabled),.aj-opcao:has(input:disabled){cursor:default}
.aj-opcao__nota--menor{font-size:13px}
@media (max-width:760px){
  .aj-chip.aj-chip--pessoa{min-height:var(--alvo-toque);padding:0 var(--espaco-14) 0 var(--espaco-6)}
  .aj-chip--pessoa>.aj-avatar{width:30px;height:30px}
}
[data-visao="desktop"] .aj-chip.aj-chip--pessoa,[data-visao="tablet"] .aj-chip.aj-chip--pessoa{min-height:var(--controle-toque);padding:0 var(--espaco-16) 0 var(--espaco-6)}
[data-visao="desktop"] .aj-chip--pessoa>.aj-avatar,[data-visao="tablet"] .aj-chip--pessoa>.aj-avatar{width:32px;height:32px}
[data-visao="mobile"] .aj-chip.aj-chip--pessoa{min-height:var(--alvo-toque);padding:0 var(--espaco-14) 0 var(--espaco-6)}
[data-visao="mobile"] .aj-chip--pessoa>.aj-avatar{width:30px;height:30px}

/* Ficha do evento. Participantes: uma linha por pessoa, com avatar de 34px e o nome em 600, borda em cima de cada
   linha (também da primeira) e 12px entre elas. Excluir na barra fixa do tablet e do mobile: o secundário com o texto
   em vermelho. A descrição mantém as quebras de linha digitadas. */
.aj-participantes{display:flex;flex-direction:column;gap:var(--espaco-12);margin:0;padding:0;list-style:none}
.aj-participante{display:flex;align-items:center;gap:var(--espaco-10);min-width:0;padding-top:var(--espaco-8);border-top:1px solid var(--borda-suave)}
.aj-participante>.aj-avatar{width:34px;height:34px;font-size:12px}
.aj-participante__nome{min-width:0;font-weight:600;overflow-wrap:anywhere}
.aj-botao--secundario.aj-botao--destrutivo{color:var(--erro)}
.aj-texto-livre{white-space:pre-wrap;overflow-wrap:anywhere}
```

As famílias numeradas da régua (as 75 linhas entre o comentário "Famílias da régua" e o comentário do Mês) saem deste roteiro. Salve-o como `gerar_regua_9f.py` no scratchpad. Ele não precisa rodar para emendar o arquivo (o bloco acima já traz as famílias por extenso); o roteiro de conferência o roda e compara o texto. Para regerar as famílias num arquivo, `python -I gerar_regua_9f.py <arquivo>` (UTF-8 sem BOM, LF; não use o `>` do PowerShell 5.1, que grava UTF-16).

```python
# Gera as famílias numeradas da régua do Dia (fase 9F), na forma exata em que entram no bloco do bundle.css.
# Uso: python -I gerar_regua_9f.py [arquivo de saída]   (sem argumento, escreve na saída padrão)
# Quartos de hora de 14px (56px por hora). Linhas: 24 (horas) + 24 (início) + 24 (quartos) + 3 (partes) = 75.
import sys

linhas = []

# A grade com n horas: 4n linhas de 14px (n de 1 a 24).
for n in range(1, 25):
    linhas.append(f'.aj-regua__grade--horas-{n}{{grid-template-rows:repeat({4 * n},14px)}}')

# O quarto de hora em que o cartão começa, contado do início da régua (q de 0 a 95): linha q+1 da grade.
for h in range(24):
    linhas.append(''.join(f'.aj-regua__evento--inicio-{q}{{grid-row-start:{q + 1}}}' for q in range(4 * h, 4 * h + 4)))

# Quantos quartos de hora o cartão ocupa (q de 2 a 96).
for h in range(1, 25):
    linhas.append(''.join(f'.aj-regua__evento--quartos-{q}{{grid-row-end:span {q}}}'
                          for q in range(4 * (h - 1) + 1, 4 * h + 1) if q >= 2))

# A parte k de n da largura, nas 12 colunas da grade (n de 2 a 4).
for n in range(2, 5):
    largura = 12 // n
    linhas.append(''.join(f'.aj-regua__evento--parte-{k}-de-{n}{{grid-column:{(k - 1) * largura + 1}/{k * largura + 1}}}'
                          for k in range(1, n + 1)))

texto = '\n'.join(linhas) + '\n'
if len(sys.argv) > 1:
    # Com um caminho, grava o arquivo (UTF-8 sem BOM, LF); o ">" do PowerShell 5.1 gravaria em UTF-16.
    open(sys.argv[1], 'w', encoding='utf-8', newline='\n').write(texto)
else:
    sys.stdout.reconfigure(encoding='utf-8', newline='\n')
    sys.stdout.write(texto)
```

Contagem esperada das famílias: 24 `horas`, 96 `inicio`, 95 `quartos` e 9 `parte` (224 nomes em 75 linhas). As contas do quadro: na régua das 08:00 (`aj-regua__grade--horas-11`, 44 quartos de 14px = 616px, como as onze faixas de 56px do quadro), "Protocolo de documentos" (09:30–10:30) leva `aj-regua__evento--inicio-6 aj-regua__evento--quartos-4`: linha 7 da grade, topo em 6 × 14 + 2 = **86px** e altura 4 × 14 − 2 × 2 = **52px**, o `top:86px; height:52px` do quadro; "Revisão de minutas" (15:00–16:00) leva `inicio-28 quartos-4`: **394px** e **52px**; a margem de 4px dos lados dá o `left:4px; right:4px` do quadro. Dois simultâneos (`parte-1-de-2` e `parte-2-de-2`) ficam lado a lado, cada um com metade das 12 colunas menos 8px, a 8px um do outro; o evento de 15 minutos (`quartos-2`) tem 24px. (Medido no Chrome sem janela com o bloco sobre o `bundle.css` de `1792048576-7268` e o `app.css` do worktree: 86/52, 394/52, metades de 399px numa grade de 814px, 24px.)

Confira por roteiro. Salve-o como `conferir_bloco_9f.py` no scratchpad, ao lado do gerador, e rode `python -I conferir_bloco_9f.py <bundle.css emendado> <tokens.css do design system> <gerar_regua_9f.py>` (o `tokens.css` do design system é `project/tokens.css`; leia-o com `read` e `path`, ou use a cópia do canvas em `…\scratchpad\quadros-9f\project\ds\araujods\tokens.css`, que é a mesma da versão `1792048576-7268`):

```python
# Confere o bloco "Agenda e evento (fase 9F)" no bundle.css emendado.
# Uso: python -I conferir_bloco_9f.py <bundle.css emendado> <tokens.css> <gerar_regua_9f.py>
import re, subprocess, sys
sys.stdout.reconfigure(encoding='utf-8')

bundle, tokens, gerador = sys.argv[1:4]
todo = open(bundle, encoding='utf-8').read()
t = open(tokens, encoding='utf-8').read()
marca = '/* ---------- Agenda e evento (fase 9F) ---------- */'
i = todo.find(marca)
if i < 0 or todo.count(marca) != 1:
    sys.exit('Marca do bloco da fase 9F não encontrada (ou repetida).')
antes, b = todo[:i], todo[i:]
sem = lambda s: re.sub(r'/\*.*?\*/', '', s, flags=re.S)
bs, antes_s = sem(b), sem(antes)

# 1. Chaves do arquivo inteiro e do bloco
print('Chaves do arquivo: {', todo.count('{'), ', }', todo.count('}'))
print('Chaves do bloco: {', b.count('{'), ', }', b.count('}'))

# 2. Toda var(--…) do bloco definida em tokens.css ou no bundle.css
DEF = r'(?:^|[{;])\s*(--[a-z0-9-]+)\s*:'
definidas = set(re.findall(DEF, t + todo, re.M))
usadas = sorted(set(re.findall(r'var\((--[a-z0-9-]+)', bs)))
faltam = [v for v in usadas if v not in definidas]
print('Variáveis usadas:', len(usadas), '; sem definição:', ', '.join(faltam) or 'nenhuma')
novas_var = sorted(set(re.findall(DEF, bs, re.M)) - set(re.findall(DEF, t + antes, re.M)))
print('Variáveis novas:', ', '.join(novas_var) or 'nenhuma')

# 3. Classes novas e classes existentes que o bloco só usa como contexto
classes = sorted(set(re.findall(r'\.(aj-[a-z0-9_-]+)', bs)))
existentes = set(re.findall(r'\.(aj-[a-z0-9_-]+)', antes_s))
novas = [c for c in classes if c not in existentes]
contexto = [c for c in classes if c in existentes]
numeradas = [c for c in novas if re.match(r'aj-regua__(grade--horas|evento--inicio|evento--quartos|evento--parte)-', c)]
nominais = [c for c in novas if c not in numeradas]
print('Classes novas:', len(novas), '=', len(nominais), 'nominais +', len(numeradas), 'numeradas')
print('Nominais:', ', '.join(nominais))
print('Classes existentes usadas como contexto (' + str(len(contexto)) + '):', ', '.join(contexto))

# 4. As famílias numeradas: contagem, faixa e texto idêntico ao do roteiro
def faixa(prefixo):
    ns = sorted(int(c[len(prefixo):]) for c in numeradas if re.fullmatch(re.escape(prefixo) + r'\d+', c))
    return len(ns), (ns[0], ns[-1]) if ns else None
print('horas-{n}:', faixa('aj-regua__grade--horas-'))
print('inicio-{q}:', faixa('aj-regua__evento--inicio-'))
print('quartos-{q}:', faixa('aj-regua__evento--quartos-'))
partes = sorted(c for c in numeradas if '--parte-' in c)
print('parte-{k}-de-{n}:', len(partes), ' '.join(p.replace('aj-regua__evento--parte-', '') for p in partes))
fam = subprocess.run([sys.executable, '-I', gerador], capture_output=True).stdout.decode('utf-8')
print('Famílias iguais às do roteiro:', fam in b, '; linhas do roteiro:', fam.count('\n'))

# 5. Contas da régua (quadro: 09:30–10:30 e 15:00–16:00 na régua das 08:00)
for nome, inicio, quartos in (('09:30–10:30', 6, 4), ('15:00–16:00', 28, 4)):
    linha = int(re.search(r'\.aj-regua__evento--inicio-' + str(inicio) + r'\{grid-row-start:(\d+)\}', bs).group(1))
    span = int(re.search(r'\.aj-regua__evento--quartos-' + str(quartos) + r'\{grid-row-end:span (\d+)\}', bs).group(1))
    quarto = int(re.search(r'\.aj-regua__grade--horas-11\{grid-template-rows:repeat\(44,(\d+)px\)\}', bs).group(1))
    margem = int(re.search(r'\.aj-regua__evento\{[^}]*margin:(\d+)px 4px', bs).group(1))
    print(f'{nome}: inicio-{inicio} quartos-{quartos} -> top {(linha - 1) * quarto + margem}px, height {span * quarto - 2 * margem}px')
print('Hora de 56px (4 quartos e o fundo da grade):', 'background-size:100% 56px' in bs and '.aj-regua__hora{flex:0 0 56px;height:56px' in bs)

# 6. Nomes do contrato (o esqueleto do plano e as tarefas que consomem) existem como classe nova
contrato = ['aj-aviso--frase', 'aj-botao--destrutivo', 'aj-chip--pessoa', 'aj-dia', 'aj-dia-escolhido', 'aj-escolha',
            'aj-estado-lista__dica', 'aj-estado-lista__icone', 'aj-faixa-dias', 'aj-faixa-dias--mes', 'aj-mes',
            'aj-mes__celula--fora', 'aj-mes__grade', 'aj-painel--solto-no-mobile', 'aj-regua__grade',
            'aj-regua__evento', 'aj-secao-dia', 'aj-semana', 'aj-texto-livre', 'aj-vinculo__escolhido--com-avatar',
            'aj-barra-superior__acao', 'aj-segmentado--claro', 'aj-segmentado--periodo', 'aj-evento--folgado',
            'aj-evento--linha', 'aj-evento--mes', 'aj-evento__meta', 'aj-participantes', 'aj-linha-de-campos']
print('Nomes do contrato ausentes:', ', '.join(c for c in contrato if c not in novas) or 'nenhum')

# 7. O que as decisões deixam de fora
proibidos = [c for c in ('aj-painel--amplo-no-tablet', 'aj-campo-icone--branco') if c in classes]
print('Classes que não entram, achadas:', ', '.join(proibidos) or 'nenhuma')
print('letter-spacing no bloco:', 'letter-spacing' in bs)
print('Regra sobre o voltar do tablet ou o título da barra de voltar:', bool(re.search(r'\.aj-voltar\b|aj-barra-superior--voltar>h1', bs)))
print('Sombra do primário mexida:', bool(re.search(r'botao--primario[^{]*\{[^}]*box-shadow', bs)))
print('auto-fit no bloco:', 'auto-fit' in bs)
print('Semana com sete colunas:', '.aj-semana{display:grid;grid-template-columns:repeat(7,minmax(0,1fr))' in bs)
print('min-width:700px fora do desktop:', bool(re.search(r'^\.aj-mes__grade\{[^}]*700px', bs, re.M)))

# 8. Literais sem token
print('Cores hex no bloco:', ', '.join(sorted(set(re.findall(r'#[0-9A-Fa-f]{3,6}\b', bs)))))
print('rgba no bloco:', ', '.join(sorted(set(re.findall(r'rgba\([^)]*\)', bs)))))
```

Expected (com o `bundle.css` de `1792048576-7268`):

```
Chaves do arquivo: { 1290 , } 1290
Chaves do bloco: { 456 , } 456
Variáveis usadas: 54 ; sem definição: nenhuma
Variáveis novas: --aj-pagina-gap-solto
Classes novas: 316 = 92 nominais + 224 numeradas
Nominais: (os 92 nomes da lista acima)
Classes existentes usadas como contexto (31): aj-app, aj-app__conteudo, aj-avatar, aj-botao, aj-botao--icone, aj-botao--secundario, aj-chip, aj-desktop-mobile, aj-desktop-tablet, aj-evento, aj-evento--audiencia, aj-evento--coluna, aj-evento--outro, aj-evento--prazo, aj-evento__corpo, aj-evento__hora, aj-evento__horario, aj-evento__local, aj-evento__tipo, aj-evento__titulo, aj-lado-a-lado__lista, aj-lado-a-lado__painel, aj-opcao, aj-painel, aj-painel--principal, aj-segmentado__hoje, aj-segmentado__opcao, aj-so-desktop, aj-so-mobile, aj-so-tablet, aj-tablet-mobile
horas-{n}: (24, (1, 24))
inicio-{q}: (96, (0, 95))
quartos-{q}: (95, (2, 96))
parte-{k}-de-{n}: 9 1-de-2 1-de-3 1-de-4 2-de-2 2-de-3 2-de-4 3-de-3 3-de-4 4-de-4
Famílias iguais às do roteiro: True ; linhas do roteiro: 75
09:30–10:30: inicio-6 quartos-4 -> top 86px, height 52px
15:00–16:00: inicio-28 quartos-4 -> top 394px, height 52px
Hora de 56px (4 quartos e o fundo da grade): True
Nomes do contrato ausentes: nenhum
Classes que não entram, achadas: nenhuma
letter-spacing no bloco: False
Regra sobre o voltar do tablet ou o título da barra de voltar: False
Sombra do primário mexida: False
auto-fit no bloco: False
Semana com sete colunas: True
min-width:700px fora do desktop: False
Cores hex no bloco: #9DB898, #B07A12, #B08A45
rgba no bloco: rgba(31,43,34,.15), rgba(31,43,34,.4), rgba(31,43,34,.45)
```

(O console do Windows PowerShell pode mostrar os acentos trocados; o conteúdo é o mesmo.) Se a versão do design system mudou desde a 9E, as chaves do arquivo e as classes de contexto mudam; os números do bloco, os nomes novos e o resto não. Confira também à mão a ordem das regras que empatam em especificidade (a de baixo vence) e as que vencem por especificidade:

- `.aj-barra-filtros--plana .aj-botao` (0,2,0) vem depois de `.aj-barra-filtros>.aj-botao` (0,2,0, 9D, que daria 40px) e vence `.aj-botao--com-icone` (0,1,0) no respiro e `.aj-botao` no raio e na fonte. É seletor de descendente: as opções dos segmentados da barra não levam `aj-botao`. `.aj-barra-filtros__grupo>.aj-botao` dá os 40px ao "Filtrar", que está dentro do grupo e não é filho da barra.
- `.aj-campo-filtro--tipo` e `--pessoa` (0,1,0) depois de `.aj-campo-filtro` (150px); no painel de filtros, `.aj-painel--filtros .aj-campo-filtro` (0,2,0) continua dando largura toda. `.aj-botao--com-icone-pequeno svg` (0,1,1) depois de `.aj-botao svg`.
- `.aj-segmentado--periodo .aj-segmentado__opcao` (0,2,0) vence `.aj-segmentado button` (0,1,1); a escolhida (0,3,0) vence `.aj-segmentado button[aria-pressed="true"]` (0,2,1); `--larga` (0,2,0) vem depois da regra das opções; o "Hoje" (0,2,1) empata com `.aj-segmentado button.aj-segmentado__hoje` e vem depois. As linhas `[data-visao="desktop"]`/`[data-visao="mobile"]` (0,3,0 e 0,4,0) devolvem os valores da classe e vencem o `--larga` da consulta de mídia. As setas não são tocadas.
- `.aj-segmentado--claro` (0,1,0) depois de `.aj-segmentado`; `.aj-segmentado--claro button` (0,1,1) e a escolhida (0,2,1) empatam com `.aj-segmentado button`, com o hover e com `[aria-pressed="true"]` e vêm depois deles.
- `.aj-aviso--frase` (0,1,0) depois de `.aj-aviso` (`font:500 15px/1.5`); `.aj-aviso--frase svg` (0,1,1) depois de `.aj-aviso svg`.
- `:is(.aj-evento--coluna,.aj-evento--linha,.aj-evento--mes):hover` (0,2,0) depois de `.aj-evento:hover` (0,2,0). `:is(…):not(.aj-evento--outro)` (0,2,0) vence `.aj-evento` (0,1,0); `.aj-evento--outro` não é tocado.
- `:is(.aj-evento--coluna,.aj-evento--linha).aj-evento--audiencia .aj-evento__horario` (0,3,0) empata com `.aj-evento--coluna.aj-evento--audiencia .aj-evento__horario` (linha 272) e vem depois; vence `.aj-evento--linha .aj-evento__horario` (0,2,0). Os pontos (0,3,1) só valem nas variantes do desktop.
- `.aj-evento--folgado`, `--linha` e `--mes` (0,1,0) depois de `.aj-evento`; `.aj-evento--folgado .aj-evento__hora` (0,2,0) vence a base de 44px; `.aj-evento--mes.aj-evento--outro` (0,2,0) dá `2px 6px`; `.aj-evento--mes .aj-evento__titulo` e `.aj-evento--linha .aj-evento__titulo` (0,2,0) vencem `.aj-evento__titulo`.
- `.aj-regua__evento` (0,1,0) depois de `.aj-evento` (`width:100%`) e de `.aj-evento--linha`; as `--parte-{k}-de-{n}` (0,1,0) depois de `.aj-regua__evento` (`grid-column:1/-1`).
- `.aj-semana__cabeca`, `.aj-secao-dia__cabeca`, `.aj-dia__cabeca` (0,1,0) e `.aj-dia-escolhido>h2` (0,1,1) vencem a regra `h2` do `app.css` (0,0,1: margem de 28px e `700 18px/1.3`), que continua até a 9G: por isso levam `margin:0` e o `font` inteiro.
- `.aj-barra-superior__acao` (0,1,0) depois de `.aj-botao--fantasma` e `.aj-botao--icone` (44px, raio 12); `.aj-barra-superior__setas .aj-botao` (0,2,0) vence `.aj-botao--icone` (40px).
- `.aj-faixa-dias__dia[aria-pressed="true"] .aj-faixa-dias__numero` (0,3,0) vence `.aj-faixa-dias__dia--hoje .aj-faixa-dias__numero` (0,2,0): hoje escolhido fica cheio.
- Na grade compacta, a linha de siglas é o primeiro filho de `div.aj-faixa-dias.aj-faixa-dias--mes`: `.aj-faixa-dias--mes>.aj-faixa-dias__cabeca` (0,2,0) a estende pelas sete colunas (`grid-column:1/-1`) e ela mesma é uma grade de sete colunas com o mesmo vão de 2px (a regra comum `.aj-faixa-dias,.aj-faixa-dias__cabeca`), então cada sigla fica sobre a coluna do seu dia. Medido no Chrome a 358px: a linha com 358px, "Seg" e o dia 28 na mesma coluna de 49,4px.
- `.aj-lado-a-lado--painel-medio .aj-lado-a-lado__lista` (0,2,0) empata com `[data-visao="desktop"] .aj-lado-a-lado__lista` (9C) e vem depois; vence `.aj-lado-a-lado__lista` da consulta (0,1,0). Não somar com `--formulario`.
- `.aj-linha-de-campos .aj-linha-de-campos__largo` (0,2,0) depois de `.aj-linha-de-campos>:is(invólucros)>*` (0,2,0).
- `.aj-ajuda--com-icone` e `.aj-ajuda--colada` (0,1,0) depois de `.aj-ajuda` (`margin:0`).
- `.aj-painel.aj-painel--solto-no-mobile` (0,2,0) vence `.aj-painel--principal` e `.aj-painel--folgado` (0,1,0) e empata, vindo depois, com `.aj-lado-a-lado--formulario .aj-painel--principal` do mobile (9D); com `[data-visao]`, (0,3,0) contra (0,3,0), também depois. `.aj-app:has(.aj-painel--solto-no-mobile) .aj-app__conteudo` (0,3,0) empata com `.aj-app:has(.aj-barra-superior--voltar) .aj-app__conteudo` e vem depois.
- `.aj-vinculo--rotulado` (0,1,0) depois de `.aj-vinculo`; `.aj-vinculo__escolhido--com-avatar` (0,1,0) depois de `.aj-vinculo__escolhido`; `.aj-vinculo__escolhido--com-avatar .aj-botao` (0,2,0) vence a cor de `.aj-botao--fantasma`; `… .aj-botao--icone` (0,2,0) vence a largura de `.aj-botao--icone` e a altura de `.aj-botao`.
- `.aj-escolha>.aj-escolha__opcao` e `.aj-chip.aj-chip--pessoa` (0,2,0) vencem `label:has(> input[type="radio"])` e `label:has(> input[type="checkbox"])` do `app.css` (0,1,2: `gap:10px`, `min-height:var(--aj-campo)`, peso 500, `--tinta`), que continua até a 9G. Medido no Chrome com o `app.css`: ladrilho de 48px, 600, `--tinta-rotulo`, vão 8; chip de 46px, 600, borda `#9DB898` marcado, vão 8. Com uma classe só (0,1,0) a folha da aplicação venceria.
- `.aj-opcao:has(input:disabled)` (0,2,1) vence o `cursor:pointer` de `.aj-opcao`; `.aj-opcao__nota--menor` (0,1,0) depois de `.aj-opcao__nota` (9E). `.aj-participante>.aj-avatar` e `.aj-chip--pessoa>.aj-avatar` (0,2,0) vencem `.aj-avatar`. `.aj-botao--secundario.aj-botao--destrutivo` (0,2,0) vence a cor de `.aj-botao--secundario`.
- `--aj-pagina-gap-solto`: `:root`, a consulta até 760px (16px) e as linhas `[data-visao]`, como `--aj-pagina-padding-acoes`.

- [ ] **Step 3: README e prévia**

Componentes novos, cada um com `README.md` (primeira frase é o resumo; depois o que cada classe faz, as regras de uso e o que quem usa fornece) e `preview.html` (linha 1 `<!-- @dsCard group="…" height=… -->`, nos grupos que já existem: Estrutura, Conteúdo, Formulário, Filtros, Ações, Retorno, Agenda — o grupo do Cartão de evento; o corpo no molde da prévia de `Abas`, com o `data-visao` de cada visão mostrada e uma legenda por bloco, como a prévia do `CartaoDeEvento`; os ícones em `<svg viewBox="0 0 24 24" aria-hidden="true">` com o traçado do `Icone.razor`, e o funil `filtrar` (o ícone novo do passo 4) com `M4 5h16l-6 7.5V19l-4 1.5v-8z`):

| Pasta | Grupo e altura | O que a prévia mostra | Regras de uso que o README registra |
|---|---|---|---|
| `SemanaDaAgenda` | Agenda, 560 | Desktop (bloco de 1100px): a semana de 05 a 11/10/2026 do quadro `AgendaA`. Seg 05: Reunião "Pauta da semana" (09:00 – 10:00, Sala de reuniões, "Marina Couto, Rafael Teles, Beatriz Lins") e Audiência "Audiência de instrução" (14:00 – 15:30, 2ª Vara do Trabalho, Transportes Litoral Ltda., Beatriz Lins). Ter 06: Prazo "Contestação — cobrança de ISS" (17:00 – 18:00, Construtora Serra Azul Ltda., Rafael Teles). Qua 07: Reunião "Alinhamento com o cliente" (10:00 – 11:30, Videoconferência, Construtora Serra Azul Ltda., "Rafael Teles, Marina Couto") e Audiência "Audiência de conciliação" (11:00 – 12:00, Fórum Cível, sala 4, Joana Figueiredo, Rafael Teles). Qui 08 hoje: Outro "Protocolo de documentos" (09:30 – 10:30, Cartório do 1º Ofício, Otávio Brandão) e Reunião "Revisão de minutas" (15:00 – 16:00, Sala de reuniões, "Marina Couto, Camila Prado"). Sex 09: Prazo "Recurso ordinário" (16:00 – 17:00, Transportes Litoral Ltda., Beatriz Lins). Sáb 10 e Dom 11: "Sem eventos" | Semana da agenda no desktop. `div.aj-semana` com sete `section.aj-semana__dia`, de segunda a domingo: sempre sete colunas. O cabeçalho é `h2.aj-semana__cabeca` com `aria-label` "quinta-feira, 08/10/2026" (hoje: "…, hoje"), a sigla em `span.aj-semana__sigla` ("Seg"…"Dom"; hoje "Qui · hoje") e o número em `span.aj-semana__numero` ("08"). Hoje: `aj-semana__dia--hoje` (sublinhado verde de 2px, número num círculo verde de 36px). Sábado e domingo: `aj-semana__dia--folga` (número apagado). Os cartões são `a.aj-evento.aj-evento--coluna.aj-evento--{tipo}` com o endereço da ficha, na ordem de início: tipo, horário ("09:00 – 10:00"), título e, cada um na sua linha e só quando houver, local (`aj-evento__local`), cliente e participantes (`aj-evento__meta`). Dia sem evento: `p.aj-semana__vazio` "Sem eventos". Evento de vários dias aparece em cada dia que toca. Só no desktop; no tablet e no mobile a semana são as Seções do dia. Quem usa fornece os sete dias, qual é hoje e os eventos de cada dia |
| `SecaoDoDia` | Agenda, 640 | Tablet (bloco de 700px): Qua 07 com os dois cartões lado a lado ("Alinhamento com o cliente", 10:00/11:30, Videoconferência; "Audiência de conciliação", 11:00/12:00, Fórum Cível, sala 4), Hoje 08 (borda verde, sigla "Hoje", número cheio; "Protocolo de documentos" e "Revisão de minutas") e Sáb 10 com "Sem eventos". Mobile (bloco de 358px): a seção de Qua 07 com os cartões em uma coluna | Um dia da agenda no tablet (a Semana são sete seções; o Dia, uma) e na Semana do mobile. `section.aj-secao-dia` (hoje: `aj-secao-dia--hoje`) com `h2.aj-secao-dia__cabeca` (`aria-label` como o da Semana; `span.aj-secao-dia__sigla` "Qua", hoje "Hoje"; `span.aj-secao-dia__numero` "07") e `div.aj-secao-dia__eventos`: duas colunas no tablet, uma no mobile. Cartões `a.aj-evento.aj-evento--{tipo}`: `aj-evento__hora` (`b` com o início, `span` com o fim, "…" no lado de outro dia) e `aj-evento__corpo` com tipo, título e uma linha (o local, ou o cliente quando não há local). Dia sem evento: `p.aj-secao-dia__vazio` "Sem eventos". As seções são filhas diretas do conteúdo (o vão entre elas é o da página). Quem usa fornece o dia, se é hoje e os eventos |
| `DiaDaAgenda` | Agenda, 1100 | Desktop (bloco de 900px): o Dia de hoje do quadro — cabeçalho "08", "Quinta-feira · hoje", "2 eventos"; régua das 08:00 às 19:00 (`aj-regua__grade--horas-11`, rótulos "08:00" a "18:00") com "Protocolo de documentos" (Outro, `inicio-6 quartos-4`, "09:30 – 10:30", "Cartório do 1º Ofício · Otávio Brandão") e "Revisão de minutas" (Reunião, `inicio-28 quartos-4`, "15:00 – 16:00", "Sala de reuniões · Marina Couto, Camila Prado"). Embaixo, um trecho de régua das 10:00 às 14:00 (`horas-4`, rótulos "10:00" a "13:00") com dois simultâneos lado a lado (Reunião 10:00 – 11:30, `inicio-0 quartos-6 parte-1-de-2`; Audiência 11:00 – 12:00, `inicio-4 quartos-4 parte-2-de-2`) e um Prazo de 15 minutos (13:00 – 13:15, `inicio-12 quartos-2`) | O Dia da agenda no desktop. `section.aj-dia` (hoje: `aj-dia--hoje`, número cheio de verde; fora de hoje, número sobre `fundo`) com `aria-label` do dia, `h2.aj-dia__cabeca` (`span.aj-dia__numero`, o nome do dia, "· hoje" quando for hoje, `span.aj-dia__total` "2 eventos", "1 evento", "0 eventos") e `div.aj-regua`: `div.aj-regua__horas aria-hidden="true"` com um `span.aj-regua__hora` por hora ("08:00"…) e `div.aj-regua__grade.aj-regua__grade--horas-{n}` com os cartões. A régua vai das 08:00 às 19:00 e se estende até a hora cheia do primeiro início e a hora cheia seguinte ao último fim (n de 1 a 24; um rótulo por hora). Cada cartão é `a.aj-evento.aj-evento--linha.aj-evento--{tipo}.aj-regua__evento` com `aj-regua__evento--inicio-{q}` (o quarto de hora do início contado do começo da régua, arredondado para baixo) e `aj-regua__evento--quartos-{q}` (os quartos que ocupa, fim arredondado para cima, no mínimo 2) e, nos eventos que se sobrepõem, `aj-regua__evento--parte-{k}-de-{n}` (até quatro partes). Linhas do cartão: tipo, horário, título e "local · cliente · participantes" (`aj-evento__meta`, cortada com reticências). Nenhum atributo `style`: a posição é toda por classe. Só no desktop. Quem usa fornece o dia, as horas da régua e, por evento, o início, os quartos e a parte |
| `MesDaAgenda` | Agenda, 640 | Desktop (bloco de 1100px): outubro de 2026 em cinco semanas (28/09 a 01/11): as células de fora (28, 29, 30/09 e 01/11) apagadas e sem eventos; 05 (09:00 "Pauta da semana", 14:00 "Audiência de instrução"), 06 (17:00 "Contestação — cobrança de ISS"), 07 (10:00 "Alinhamento com o cliente", 11:00 "Audiência de conciliação"), 08 hoje (09:30 "Protocolo de documentos", 15:00 "Revisão de minutas"), 09 (16:00 "Recurso ordinário"), e o dia 14 com três itens e "+2" | O Mês da agenda no desktop e no tablet. `div.aj-mes` com `div.aj-mes__cabeca aria-hidden="true"` (sete `span`: "Seg"…"Dom") e `div.aj-mes__rolagem` > `div.aj-mes__grade` (4, 5 ou 6 semanas inteiras de segunda a domingo). Célula: `div.aj-mes__celula` (de outro mês: `aj-mes__celula--fora`, sem eventos) com `span.aj-mes__numero` (hoje: `aj-mes__numero--hoje`, círculo verde), até três `a.aj-evento.aj-evento--mes.aj-evento--{tipo}` (`span.aj-so-leitor` com o tipo e vírgula, "Reunião, "; `span.aj-evento__horario` com a hora de início, só no dia em que o evento começa; `span.aj-evento__titulo`) e, com mais, `button.aj-mes__mais` "+2" com `aria-label` "Mais 2 eventos em 14/10", que leva ao Dia daquela data. A cor não é o único sinal do tipo: o nome vai para o leitor. No desktop a grade tem 700px de mínimo e rola de lado dentro de `aj-mes__rolagem`; no tablet, sem mínimo. Quem usa fornece as semanas, o mês, hoje e os eventos de cada dia |
| `FaixaDeDias` | Agenda, 760 | Mobile (bloco de 358px): a faixa de 05 a 11/10 (Qui 08 hoje e escolhido; os pontos de cada dia nas cores dos tipos; Sáb e Dom sem pontos); o dia escolhido "Quinta, 08/10 · hoje" com "2 eventos" e os dois cartões folgados ("Protocolo de documentos", 09:30/10:30, "Cartório do 1º Ofício", "Otávio Brandão"; "Revisão de minutas", 15:00/16:00, "Sala de reuniões", "Marina Couto, Camila Prado"); o vazio "Nenhum evento neste dia" com o ícone `agenda` e "Toque em + para agendar."; e a grade compacta de outubro (`aj-faixa-dias--mes`, a linha de siglas em cima, 28, 29, 30/09 e 01/11 apagados, o 08 hoje e o 14 escolhido) | A escolha do dia no mobile, na barra superior. Faixa: `div.aj-faixa-dias` com sete `button.aj-faixa-dias__dia` (`aria-pressed` no escolhido; `aria-label` "Quinta, 08/10, hoje"; hoje: `aj-faixa-dias__dia--hoje`, aro verde), cada um com `span.aj-faixa-dias__sigla`, `span.aj-faixa-dias__numero` e `span.aj-pontos-evento aria-hidden="true"` com um `span.aj-ponto-evento.aj-ponto-evento--{tipo}` por evento do dia, até seis (a fileira existe mesmo vazia: os dias ficam da mesma altura). Grade compacta do Mês: `div.aj-faixa-dias.aj-faixa-dias--mes` com, como primeiro filho, `div.aj-faixa-dias__cabeca aria-hidden="true"` (as sete siglas, na largura toda) e depois os dias da grade, sem sigla; de outro mês, `aj-faixa-dias__dia--fora`, sem pontos. Embaixo, no conteúdo, o dia escolhido: `div.aj-dia-escolhido` com `h2` ("Quinta, 08/10 · hoje") e `span.aj-dia-escolhido__total` ("2 eventos"), e os cartões `a.aj-evento.aj-evento--folgado.aj-evento--{tipo}` (hora, tipo, título, "local · cliente" e participantes em `aj-evento__meta`) ou o Estado da lista com ícone e dica. Quem usa fornece os dias, o escolhido, hoje e os tipos dos eventos de cada dia |
| `LinhaDeCampos` | Formulário, 420 | Desktop (bloco de 704px): Título ("Audiência de conciliação", `__largo`) e Tipo (`select`, `__medio`); Data (2026-10-07), Hora em erro (`__curto`, "Campo obrigatório") e "Duração (minutos)" (60); a ajuda "Data e hora no horário do escritório." com o relógio, colada. Tablet (bloco de 650px): Data, Hora e Duração (`select` "1 hora") em três colunas. Mobile (bloco de 358px): Data e Hora em duas colunas e Duração na linha inteira | Campos lado a lado com larguras próprias. `div.aj-linha-de-campos` com os `label.aj-rotulo`: no desktop em linha, com quebra e base de 170px; `aj-linha-de-campos__largo` (260px e o dobro do espaço livre), `__medio` (160px), `__curto` (130px). No tablet, três colunas iguais com vão 12; no mobile, duas, com 16px entre as linhas, e `__inteiro-no-mobile` ocupa a linha inteira. O campo pode vir num invólucro de visão (a duração que é número no desktop e lista no tablet e no mobile): o invólucro some e o campo conta como item da linha. A ajuda que pertence à linha de cima: `p.aj-ajuda.aj-ajuda--com-icone.aj-ajuda--colada` com o ícone (14px) antes do texto. Quem usa fornece os campos e as larguras |
| `EscolhaEmLadrilhos` | Formulário, 260 | Tablet: "Tipo" com Audiência (escolhida), Reunião, Prazo e Outro em quatro ladrilhos. Mobile (bloco de 358px): os mesmos em 2 × 2 | Escolha única com poucas opções curtas, em ladrilhos (o tipo do evento no tablet e no mobile; no desktop, um `select`). `fieldset.aj-conjunto` com `legend.aj-rotulo` e `div.aj-escolha`; cada opção é `label.aj-escolha__opcao` com o `input type="radio"` (escondido, com classe) e o texto. Quatro por linha e 48px no tablet; duas e 46px no mobile. Escolhido em verde cheio; o foco do rádio contorna o ladrilho; as setas do teclado trocam a escolha. Quem usa fornece as opções e a escolhida |
| `ParticipantesDoEvento` | Conteúdo, 300 | O painel "Participantes" da ficha do evento: "MC" dourado "Marina Couto (você)", "BL" azul "Beatriz Lins", "OB" lilás "Otávio Brandão" | Quem participa de um evento, na ficha. `section.aj-painel` com `h2` e `ul.aj-participantes` de `li.aj-participante`: `span.aj-avatar` (`aria-hidden`, 34px; o do próprio usuário dourado, sem modificador de cor; os outros pela cor da pessoa) e `span.aj-participante__nome`. O próprio usuário primeiro, com "(você)"; inativo com "(inativo)"; sem nome, "Usuário não encontrado". Cada linha com borda em cima, também a primeira. Quem usa fornece as pessoas e qual é o usuário |

Componentes que ganham modificador (README emendado; prévia emendada, com o estado novo, onde já existe):

- Painel de filtros (README e prévia): "Barra da agenda" — no desktop, `div.aj-barra-filtros.aj-barra-filtros--dividida` com dois `div.aj-barra-filtros__grupo` nas pontas (o segmentado de período e o das visões; Tipo `aj-campo-filtro--tipo`, Participante `aj-campo-filtro--pessoa`, e "Filtrar" `aj-botao--secundario aj-botao--com-icone-pequeno` com o funil); no tablet, `aj-barra-filtros--plana` (sem sombra) com os dois segmentados e o botão "Filtros" (`aj-botao--secundario aj-botao--com-icone aj-botao--ao-fim`, 46px, raio 12), que abre este painel com Tipo e Participante, "Limpar" e "Filtrar"; no mobile, o botão de ícone "Filtrar agenda" na linha do título (veja Navegação). Prévia: as barras do desktop e do tablet.
- Botão (README e prévia): `aj-botao--com-icone-pequeno` (o ícone de 16px sem mudar o respiro: o "Filtrar" da agenda) e `aj-botao--destrutivo` (junto de `aj-botao--secundario`: o contorno do secundário e o texto em `erro`, o "Excluir" da barra fixa da ficha do evento no tablet e no mobile). Prévia: "Filtrar" com o funil e "Excluir" destrutivo ao lado de "Editar".
- Segmentado (README e prévia): `aj-segmentado--periodo` (os dois segmentados da barra da agenda; no tablet, opções de 14,5px, 70px de mínimo, a escolhida sem sombra e o "Hoje" com 16px de respiro), `aj-segmentado__opcao--larga` (o "Semana" do tablet, 80px) e `aj-segmentado--claro` (junto de `--recuado`, na segunda linha da barra superior do mobile: 3px de respiro, opções de 38px e a escolhida branca). Cada opção leva `aj-segmentado__opcao` e `aria-pressed`. Prévia: o par do tablet com `--periodo` e o claro do mobile (bloco de 358px).
- Aviso (README e prévia): `aj-aviso--frase` (o retorno de salvar evento com sobreposição de participantes; `aj-aviso--atencao`): o ícone na primeira linha, o texto em peso normal e "Evento salvo." em `strong`. As três redações, uma por visão, cada aviso num invólucro de visão: desktop "**Evento salvo.** Atenção: Rafael Teles já tem evento das 10:00 às 11:30 (07/10).", tablet "**Evento salvo.** Rafael Teles já tem evento das 10:00 às 11:30 (07/10).", mobile "Rafael Teles já tem evento das 10:00 às 11:30 em 07/10."; várias pessoas separadas por "; ". Sem sobreposição, o aviso de sucesso comum ("Evento salvo."). Prévia: as três, com `data-visao`.
- Cartão de evento (README e prévia): o cartão passa a ser link (`a.aj-evento`, o endereço da ficha, sem sublinhado; o cartão inteiro é o alvo); hover com sombra (no desktop sobe 1px); as linhas de cada forma — Semana do desktop (`--coluna`: tipo, horário, título, local, cliente, participantes), tablet (base: hora, tipo, título, local ou cliente), mobile (`aj-evento--folgado`: hora de 46px, título de 15,5px, "local · cliente" e participantes em `aj-evento__meta`), régua do Dia (`aj-evento--linha`: em linha, cortado), Mês (`aj-evento--mes`: hora de início e título, com o tipo só para leitor); nas variantes do desktop só o Outro tem borda, o horário da Audiência é `evento-audiencia-suave` e os pontos do Prazo e do Outro são `#B07A12` e `tinta-icone`; o lado de outro dia na coluna da hora é "…". Prévia: os cartões de hoje trocados para `a`, mais os folgados (mobile), dois em linha (desktop) e quatro itens de mês.
- Estado da lista (README e prévia): com ícone e dica — `svg.aj-estado-lista__icone` (28px, traço `tinta-icone`) antes do `strong` e `span.aj-estado-lista__dica` depois ("Nenhum evento neste dia" / "Toque em + para agendar."). Prévia: esse estado, ao lado do de hoje.
- Navegação (README e prévia, seção "Barra superior do mobile"): a barra de título da agenda — o botão de ícone `aj-botao aj-botao--fantasma aj-botao--icone aj-barra-superior__acao` ("Filtrar agenda", 44px, raio 12, ícone `filtros` de 22px) entre o título e o avatar; a segunda linha com o segmentado claro; a linha do mês `div.aj-barra-superior__periodo` com o mês em `strong` ("Outubro 2026") e as setas em `div.aj-barra-superior__setas` (40px; "Semana anterior"/"Próxima semana", no Mês "Mês anterior"/"Próximo mês"); e a faixa de dias (veja Faixa de dias). Prévia: essa barra, no mobile, com a faixa.
- Campo (README e prévia): `aj-ajuda--com-icone` e `aj-ajuda--colada` (veja Linha de campos), `aj-opcao__nota--menor` (a nota "(você)" de 13px) e a opção travada (`input` desabilitado: sem cursor de clique, com o fundo de marcada); referência a Linha de campos e Escolha em ladrilhos. Prévia: a opção "Marina Couto (você)" travada e marcada.
- Vínculo (README e prévia): `aj-vinculo--rotulado` (o vínculo com o nome visível, `span.aj-vinculo__rotulo` com `id`, ligado ao grupo por `role="group"` e `aria-labelledby`, aceita `small` "(opcional)": 10px entre nome, escolhido e busca no desktop, 8px no tablet e no mobile) e o escolhido com avatar `aj-vinculo__escolhido--com-avatar` (`span.aj-vinculo__avatar aria-hidden` com a sigla num círculo branco, o nome em `aj-vinculo__nome` e o botão só de ícone "Remover cliente", em verde de sucesso; no tablet com a altura do campo e o avatar de 30px; no tablet e no mobile o botão com o alvo de toque). Processo e atividade escolhidos usam o mesmo escolhido, sem avatar ("Remover processo", "Remover atividade"). Prévia: "Cliente" com "JF Joana Figueiredo" e a busca "Trocar cliente" (desktop) e o mesmo escolhido no tablet.
- Chip (README e prévia): `aj-chip--pessoa`, a pessoa marcável em chip (`label.aj-chip.aj-chip--pessoa` com o `input type="checkbox"` escondido e o avatar à esquerda; marcada, fundo `selecao-fundo` e borda `#9DB898`; 46px e avatar de 32px no tablet, 44px e 30px no mobile; travada sem cursor de clique). Os participantes do evento no tablet (nome inteiro, "Marina Couto (você)") e no mobile (primeiro nome, "Você"). Prévia: os cinco chips do quadro do tablet (BL azul, CP âmbar, MC dourado travado, OB lilás, RT verde marcado) e os do mobile.
- Painel (README e prévia): `aj-painel--folgado` (14px entre os blocos: Vínculos e Participantes do formulário de evento), `aj-painel--solto-no-mobile` (no mobile o painel deixa de ser caixa: sem borda, respiro, fundo e sombra, 16px entre os campos e entre os painéis; o `h2` vai num invólucro `aj-desktop-tablet`) e `aj-texto-livre` (o texto digitado mantém as quebras de linha: a descrição do evento); referência a Participantes do evento. Prévia: um painel "Vínculos" no desktop e o mesmo solto no mobile.
- Lado a lado (só README): `aj-lado-a-lado--painel-medio` (o formulário de evento: a coluna principal com base de 420px e quase todo o espaço livre, o painel com base de 380px; não somar com `--formulario`).

`design-system.json`: `lastChange` (aqui) e o ícone novo no grupo `Icones` (passo 4). `lastChange`: `by` o usuário, `via` "Claude Code", `at` agora, `note` "Fase 9F: agenda — semana de sete colunas, dia com régua de quartos de hora, mês, seções por dia, faixa de dias e grade compacta do mobile, aviso em frase; evento — linha de campos, escolha em ladrilhos, chips de pessoa, vínculo com avatar, painéis soltos no mobile, participantes, excluir destrutivo e texto livre; o ícone filtrar.". `tokens.json`, `tokens.css` e `responsividade.md` não mudam nesta fase.

- [ ] **Step 4: Ícone `filtrar` no grupo de ícones**

Os ícones do design system são arquivos no armazém de arquivos do artifact (`assets`), um `.svg` por ícone, listados no grupo `Icones` de `project/design-system.json` (`assetGroups.Icones`: `order` e, em `files`, `{ "name", "blob", "size", "type" }`, em que `blob` é o id do arquivo no armazém), e descritos em `project/assets/Icones/README.md`. Não há cópia em `project/`. Confira antes com `action: "list"` e `scope: "assets"` no design system (42 arquivos na `1792048576-7268`; o `filtros.svg` é o `6ffb91d12e157a3a4efda944bd7f82da`) e leia um deles (`read` com `path` no id) para ver o formato: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="#233026" stroke-width="…" stroke-linecap="round" stroke-linejoin="round">…</svg>`, numa linha só, sem quebra no fim.

1. Grave `filtrar.svg` no scratchpad (UTF-8 sem BOM, sem quebra de linha no fim; 228 bytes), com o traço de 2 dos ícones de ação e o funil do `Icone.razor` (Tarefa 3):

```
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="#233026" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 5h16l-6 7.5V19l-4 1.5v-8z"></path></svg>
```

   Para gravar sem a quebra do fim: `python -I -c "open(r'<scratchpad>iltrar.svg','w',encoding='utf-8',newline='').write('<o texto acima>')"`, ou `[IO.File]::WriteAllText($f, $svg, (New-Object Text.UTF8Encoding $false))` no PowerShell.

2. Envie-o ao armazém do design system: publicação com `url` https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS, `file_path` no `filtrar.svg` (caminho longo do scratchpad) e `asset: true`. Anote o id que o resultado devolve (32 caracteres hexadecimais, o que vem depois de `/_blob/`) e confira o tamanho (228) e o tipo (`image/svg+xml`).

3. No `design-system.json` do passo 3, no grupo `Icones`: em `order`, `"filtrar.svg"` logo depois de `"filtros.svg"`; em `files`, a entrada `"filtrar.svg": { "name": "filtrar.svg", "blob": "<id anotado>", "size": 228, "type": "image/svg+xml" }`, depois da de `filtros.svg`. Nada mais do grupo muda.

4. Em `project/assets/Icones/README.md`, na linha "Ações, com traço de 2", `filtrar` logo depois de `filtros`, e uma frase no fim: "`filtrar` (o funil) é o botão "Filtrar" da barra da agenda no desktop; `filtros` (as três linhas com bolinhas) abre o painel de filtros no tablet e no mobile."

- [ ] **Step 5: Publicar no design system e sincronizar o canvas**

Uma chamada de publicação no design system (`url` https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS) com os arquivos alterados em `files`: `project/components/bundle.css`, `project/design-system.json` (com o `lastChange` e o ícone do passo 4), `project/assets/Icones/README.md`, os dezesseis arquivos das oito pastas novas (`SemanaDaAgenda`, `SecaoDoDia`, `DiaDaAgenda`, `MesDaAgenda`, `FaixaDeDias`, `LinhaDeCampos`, `EscolhaEmLadrilhos`, `ParticipantesDoEvento`), os README e prévias emendados do passo 3 (`PainelDeFiltros`, `Botao`, `Segmentado`, `Aviso`, `CartaoDeEvento`, `EstadoDaLista`, `Navegacao`, `Campo`, `Vinculo`, `Chip`, `Painel`, e o README de `LadoALado`) e, por último na lista, o índice relido logo antes. Use o caminho longo do scratchpad em `root` e `file_path` (`C:\Users\Gabriel Paulino\AppData\Local\Temp\claude\…`; o atalho `GABRIE~1` é recusado). Anote a versão publicada no ledger.

Depois, no canvas (https://claude.ai/artifact/HzHWv1pgVXAnYHuPEYQDFj): `read` do artifact inteiro e uma publicação com `project/canvas.json` (no registro `designSystems`, só `version` — a versão publicada do design system — e `copiedAt` — agora — mudam) e, em `files`, a cópia no servidor `"project/ds/araujods/components/bundle.css": {"artifact": "https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS", "path": "project/components/bundle.css"}`. `tokens.json` e `tokens.css` não mudam nesta fase. Anote a versão do canvas no ledger.

- [ ] **Step 6: Copiar para o código**

`wwwroot/css/componentes.css` = as duas primeiras linhas de cabeçalho que o arquivo já tem (a segunda diz "Cópia de components/bundle.css (…); altere lá primeiro.") + o `bundle.css` publicado, da linha "Visões:" em diante, com fim de linha CRLF e sem BOM. Baixe o `bundle.css` publicado (`read` com `path` `project/components/bundle.css`) e rode o roteiro abaixo (salvo em UTF-8 **com** BOM, porque o Windows PowerShell 5.1 lê arquivo sem BOM como ANSI), com `-Bundle` no arquivo baixado e `-Codigo` e `-Saida` apontando para o mesmo `componentes.css` do worktree:

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

Expected: `Corpo idêntico ao bundle.css: True`, `Linhas: 1518; CRLF: 1517; BOM: False` (o arquivo passa de 1119 para 1517 linhas; o último elemento contado é o vazio depois do CRLF final). Depois:

Run: `git diff --stat -- src/AtividadesJuridicas.Web/wwwroot/css/componentes.css`
Expected: `1 file changed, 398 insertions(+)`, nenhuma linha removida (a linha em branco e as 397 do bloco). Se a versão do design system mudou desde a 9E (passo 1), o número muda junto com o que mudou lá; o bloco da fase continua com 397 linhas.

`tokens.css` não muda nesta fase.

- [ ] **Step 7: Build e commit**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Antes do commit, `git branch --show-current` (tem de ser `worktree-feat+fase-9f-agenda`). Pela ferramenta PowerShell, com a mensagem num arquivo sem BOM:

```powershell
git add src/AtividadesJuridicas.Web/wwwroot/css/componentes.css
$msg = @'
style(web): design system com os padrões da agenda e do evento: semana de sete colunas, dia com régua de quartos de hora, mês, seções por dia, faixa de dias, aviso em frase, formulário e ficha do evento

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
'@
$f = Join-Path $env:TEMP 'mensagem-9f-tarefa-1.txt'
[IO.File]::WriteAllText($f, $msg, (New-Object Text.UTF8Encoding $false))
git commit -F $f
```

---

### Task 2: Lógica de calendário pura — período, régua e rótulos

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Agenda/VisaoDaAgenda.cs`, `PeriodoDaAgenda.cs`, `ReguaDoDia.cs`
- Modify: `src/AtividadesJuridicas.Web/Components/Agenda/RotulosDeAgenda.cs`
- Test: `tests/AtividadesJuridicas.IntegrationTests/AgendaPuraTests.cs` (novo; a classe `AgendaPorDiaTests` de `AgendaWebTests.cs` não muda)

**Interfaces:**
- Produces:
  - `public enum VisaoDaAgenda { Dia, Semana, Mes }`.
  - `public sealed record PeriodoDaAgenda(VisaoDaAgenda? Visao, DateOnly Data, bool DataNoEndereco)` com: `static PeriodoDaAgenda Ler(string? visao, string? data, DateOnly hoje)`; `VisaoDaAgenda NoDesktopETablet` (`Visao ?? Semana`); `VisaoDaAgenda NoMobile` (`Visao ?? Dia`); `DateOnly InicioDaConsulta`, `DateOnly FimDaConsulta` (exclusivo; a semana de `Data`, ou as semanas do mês com `Visao == Mes`); `IReadOnlyList<DateOnly> Semana` (segunda a domingo de `Data`); `IReadOnlyList<DateOnly> SemanasDoMes` (as datas da grade do mês de `Data`, múltiplo de 7); `string Consulta` (`""`, `?visao=semana`, `?data=2026-10-08`, `?visao=dia&data=2026-10-08`: a `visao` quando houver e a `data` quando `DataNoEndereco`); `string Endereco` (`/agenda{Consulta}`); `PeriodoDaAgenda ComVisao(VisaoDaAgenda)`, `ComData(DateOnly)`, `Anterior(VisaoDaAgenda unidade)`, `Proximo(VisaoDaAgenda unidade)` (±1 dia, ±7 dias, ±1 mês, ajustando ao último dia do mês quando o dia não existe), `ParaHoje(DateOnly hoje)` (mantém a `Visao`; as quatro últimas sempre com `DataNoEndereco`); `static string Ficha(Guid eventoId, string consulta)` (`/agenda/{id}{consulta}`); `static string ConsultaDeOrigem(string? visao, string? data)` para a ficha e o formulário repassarem o que receberam (só valores que `Ler` aceitaria; o resto some).
  - `public sealed record EventoNaRegua(EventoResumo Evento, int InicioEmQuartos, int Quartos, int Parte, int Partes)` e `public sealed record ReguaDoDia(int HoraInicial, int HoraFinal, IReadOnlyList<EventoNaRegua> Eventos)` com `static ReguaDoDia Montar(FusoDoEscritorio fuso, DateOnly dia, IEnumerable<EventoResumo> eventos)` e `int Horas` (`HoraFinal - HoraInicial`). `InicioEmQuartos` conta a partir de `HoraInicial`; `Parte` e `Partes` de 1 a 4.
  - Em `RotulosDeAgenda` (sem mudar as assinaturas de hoje): `SiglaDoDia(DateOnly)` (`Seg`…`Dom`), `NomeDoDia(DateOnly)` (`Segunda`…, curto com maiúscula), `NomeLongoDoDia(DateOnly)` (`Quinta-feira`), `RotuloLongo(DateOnly, bool hoje)` (`quinta-feira, 08/10/2026, hoje`), `RotuloCurto(DateOnly, bool hoje)` (`Quinta, 08/10, hoje`), `SubtituloDaSemana(DateOnly segunda)`, `SubtituloDoDia(DateOnly)`, `SubtituloDoMes(DateOnly)`, `MesDoMobile(DateOnly)`, `TotalDeEventos(int)`, `HoraDeInicio(FusoDoEscritorio, EventoResumo, DateOnly dia)` e `HoraDeFim(...)` (`HH:mm` ou `…`), `LinhaDoTablet(EventoResumo)`, `LinhaDoMobile(EventoResumo)`, `LinhaDaRegua(EventoResumo)`, `Participantes(EventoResumo)` (os nomes na ordem do servidor separados por `, `, nome vazio como `Usuário não encontrado`; as linhas do mobile e da régua usam o mesmo texto), `TituloDoDiaNoDesktop(DateOnly, bool hoje)` (`Quinta-feira · hoje`), `TituloDoDiaNoMobile(DateOnly, bool hoje)` (`Quinta, 08/10 · hoje`), `Duracao(int minutos)` (`90 minutos`, `1 minuto`), `ResumoDoEvento(FusoDoEscritorio, EventoDetalhe, bool comLocal)`, `ConflitoNoDesktop(FusoDoEscritorio, IReadOnlyList<SobreposicaoDeAgenda>)`, `ConflitoNoTablet(...)`, `ConflitoNoMobile(...)` (só o texto depois de `Evento salvo.`, com o ponto final; o `strong` é marcação).

**Regras:**
- Puro: nada de `DateTime.UtcNow`, `IDespachante` nem Razor. `hoje` sempre como parâmetro.
- Formatos com `CultureInfo("pt-BR")` fixo, independentes da cultura da thread (como os de hoje).
- `MensagemDeSalvo` e `Sobreposicao` de hoje continuam (os testes de `AgendaPorDiaTests` não mudam); as três redações novas reusam a mesma montagem.

**Testes obrigatórios:** `Ler` com cada combinação válida e inválida (Review Focus 1); intervalo da consulta na Semana, no Dia e no Mês, inclusive o mês de quatro semanas (fevereiro de 2027) e o de seis (agosto de 2026), e a semana que atravessa o ano; `Anterior`/`Proximo` nas três unidades, inclusive 31/01 + 1 mês; `Consulta` e `Endereco`; régua padrão (08–19), estendida para cedo e para tarde, evento que atravessa a meia-noite recortado, evento de 15 minutos com dois quartos, dois simultâneos em duas partes e um terceiro depois do grupo com uma parte, cinco simultâneos (Review Focus 2 e 3); as três redações com uma e com duas pessoas e com a sobreposição que atravessa a meia-noite; `HoraDeInicio`/`HoraDeFim` com `…`; os subtítulos e os rótulos de dia.

- [ ] **Step 1: Testes (devem falhar)**

Crie `tests/AtividadesJuridicas.IntegrationTests/AgendaPuraTests.cs` (UTF-8 sem BOM, fim de linha CRLF, como os outros arquivos do projeto). A classe `AgendaPorDiaTests` de `AgendaWebTests.cs` não muda.

As contas dos valores esperados, para quem revisar:

- Hoje é quinta-feira, 08/10/2026 (01/01/2026 foi quinta; 01/10 está 273 dias depois, múltiplo de 7). A semana dos quadros vai de segunda, 05/10, a domingo, 11/10; a consulta da semana vai de 05/10 a 12/10 (exclusivo).
- Outubro de 2026: 01/10 é quinta e 31/10 é sábado. A grade vai de segunda, 28/09, a domingo, 01/11: cinco semanas, 35 dias (a do quadro).
- Agosto de 2026 começa num sábado (01/08) e termina numa segunda (31/08): a grade vai de 27/07 a 06/09, seis semanas, 42 dias.
- Fevereiro de 2027 começa numa segunda (01/01/2027 é sexta; 01/02 está 31 dias depois) e tem 28 dias: a grade é o próprio mês, 01/02 a 28/02, quatro semanas.
- 31/12/2026 é quinta: a semana vai de 28/12/2026 a 03/01/2027.
- Régua: 56px por hora, quatro quartos por hora; o quarto de um horário é `minutos desde a meia-noite / 15` (início para baixo, fim para cima), e o `InicioEmQuartos` conta a partir de `HoraInicial × 4`. 09:30 = 570 min = quarto 38; 38 − 32 = 6. 15:00 = quarto 60; 60 − 32 = 28. 06:30 = quarto 26, hora 6. 20:15 = 1215 min = 81 quartos, que arredonda para a hora 21. 14:05 = 845 min → quarto 56 (para baixo); 14:35 = 875 min → quarto 59 (para cima): três quartos. 23:50 = 1430 min → quarto 95; com o mínimo de dois quartos o fim iria a 97, além do dia (96): o fim fica em 96 e o início desce para 94.

```csharp
using System.Globalization;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Web.Components.Agenda;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// A lógica de calendário da agenda, sem tela: o período lido do endereço, a régua de horas do Dia e os rótulos das
/// três visões. Hoje é quinta-feira, 08/10/2026 (o dia dos quadros); a semana dos quadros vai de 05/10 a 11/10.
/// </summary>
public class AgendaPuraTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 8);
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");

    private static DateOnly D(string data) => DateOnly.ParseExact(data, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private EventoResumo Evento(string titulo, DateTime inicioLocal, int minutos)
    {
        var inicio = _fuso.ParaUtc(inicioLocal);
        return new EventoResumo(Guid.NewGuid(), titulo, TipoEvento.Reuniao, inicio, inicio.AddMinutes(minutos), null, null, null, []);
    }

    private static EventoResumo Cartao(string? local, string? cliente, params string[] participantes) =>
        new(Guid.NewGuid(), "Evento", TipoEvento.Reuniao, new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 8, 13, 0, 0, DateTimeKind.Utc), local, cliente, null, participantes);

    private SobreposicaoDeAgenda Sobreposicao(string? nome, DateTime inicioLocal, DateTime fimLocal) =>
        new(Guid.NewGuid(), nome, _fuso.ParaUtc(inicioLocal), _fuso.ParaUtc(fimLocal));

    private EventoDetalhe Detalhe(TipoEvento tipo, DateTime inicioLocal, int minutos, string? local)
    {
        var inicio = _fuso.ParaUtc(inicioLocal);
        return new EventoDetalhe(Guid.NewGuid(), "Audiência de instrução — Litoral", null, local, tipo, inicio, minutos,
            inicio.AddMinutes(minutos), null, null, null, null, null, null, Guid.NewGuid(), "Marina Couto", [],
            new AcoesDoEvento(true, true), 1);
    }

    // ---- Período no endereço ----

    [Theory]
    [InlineData(null, null, null, "2026-10-08", false)]
    [InlineData("semana", null, VisaoDaAgenda.Semana, "2026-10-08", false)]
    [InlineData("dia", "2026-10-06", VisaoDaAgenda.Dia, "2026-10-06", true)]
    [InlineData("mes", "2026-08-15", VisaoDaAgenda.Mes, "2026-08-15", true)]
    [InlineData(null, "2027-02-10", null, "2027-02-10", true)]
    [InlineData(null, "2000-01-01", null, "2000-01-01", true)]
    [InlineData(null, "2100-12-31", null, "2100-12-31", true)]
    public void Ler_aceita_as_visoes_e_as_datas_do_endereco(
        string? visao, string? data, VisaoDaAgenda? visaoLida, string dataLida, bool noEndereco)
    {
        PeriodoDaAgenda.Ler(visao, data, Hoje).Should().Be(new PeriodoDaAgenda(visaoLida, D(dataLida), noEndereco));
    }

    // Visão desconhecida conta como ausente; data inválida ou fora de 2000–2100 vira hoje. Nada disso é erro.
    [Theory]
    [InlineData("xyz", null, null, "2026-10-08", false)]
    [InlineData("Semana", null, null, "2026-10-08", false)]
    [InlineData("xyz", "2026-10-06", null, "2026-10-06", true)]
    [InlineData("dia", "2026-13-40", VisaoDaAgenda.Dia, "2026-10-08", false)]
    [InlineData(null, "1999-01-01", null, "2026-10-08", false)]
    [InlineData(null, "2101-01-01", null, "2026-10-08", false)]
    [InlineData(null, "08/10/2026", null, "2026-10-08", false)]
    [InlineData("", "", null, "2026-10-08", false)]
    public void Ler_ignora_o_que_nao_entende(
        string? visao, string? data, VisaoDaAgenda? visaoLida, string dataLida, bool noEndereco)
    {
        PeriodoDaAgenda.Ler(visao, data, Hoje).Should().Be(new PeriodoDaAgenda(visaoLida, D(dataLida), noEndereco));
    }

    // Review Focus 1: endereço estranho abre o padrão (hoje; Semana no desktop e no tablet, Dia no mobile), com a
    // consulta da semana de hoje e o endereço limpo.
    [Theory]
    [InlineData(null, null)]
    [InlineData("xyz", null)]
    [InlineData(null, "2026-13-40")]
    [InlineData(null, "1999-01-01")]
    public void Endereco_estranho_abre_o_padrao(string? visao, string? data)
    {
        var periodo = PeriodoDaAgenda.Ler(visao, data, Hoje);

        periodo.NoDesktopETablet.Should().Be(VisaoDaAgenda.Semana);
        periodo.NoMobile.Should().Be(VisaoDaAgenda.Dia);
        periodo.Data.Should().Be(Hoje);
        periodo.InicioDaConsulta.Should().Be(new DateOnly(2026, 10, 5));
        periodo.FimDaConsulta.Should().Be(new DateOnly(2026, 10, 12));
        periodo.Consulta.Should().BeEmpty();
        periodo.Endereco.Should().Be("/agenda");
    }

    [Theory]
    [InlineData(null, VisaoDaAgenda.Semana, VisaoDaAgenda.Dia)]
    [InlineData("dia", VisaoDaAgenda.Dia, VisaoDaAgenda.Dia)]
    [InlineData("semana", VisaoDaAgenda.Semana, VisaoDaAgenda.Semana)]
    [InlineData("mes", VisaoDaAgenda.Mes, VisaoDaAgenda.Mes)]
    public void Sem_visao_cada_largura_tem_o_seu_padrao_e_com_visao_todas_mostram_a_mesma(
        string? visao, VisaoDaAgenda noDesktopETablet, VisaoDaAgenda noMobile)
    {
        var periodo = PeriodoDaAgenda.Ler(visao, "2026-10-08", Hoje);

        periodo.NoDesktopETablet.Should().Be(noDesktopETablet);
        periodo.NoMobile.Should().Be(noMobile);
    }

    [Theory]
    [InlineData(null, "2026-10-08", "2026-10-05", "2026-10-12")]
    [InlineData("semana", "2026-10-11", "2026-10-05", "2026-10-12")]
    [InlineData("dia", "2026-10-08", "2026-10-05", "2026-10-12")]
    [InlineData("semana", "2026-12-31", "2026-12-28", "2027-01-04")]
    [InlineData("mes", "2026-10-08", "2026-09-28", "2026-11-02")]
    [InlineData("mes", "2026-08-15", "2026-07-27", "2026-09-07")]
    [InlineData("mes", "2027-02-10", "2027-02-01", "2027-03-01")]
    public void Consulta_cobre_a_semana_da_data_ou_as_semanas_inteiras_do_mes(
        string? visao, string data, string inicio, string fim)
    {
        var periodo = PeriodoDaAgenda.Ler(visao, data, Hoje);

        periodo.InicioDaConsulta.Should().Be(D(inicio));
        periodo.FimDaConsulta.Should().Be(D(fim));
        // Dentro do limite de 92 dias do ListarEventos: de 7 (semana) a 42 (mês de seis semanas).
        (periodo.FimDaConsulta.DayNumber - periodo.InicioDaConsulta.DayNumber).Should().BeOneOf(7, 28, 35, 42);
    }

    [Fact]
    public void Semana_vai_de_segunda_a_domingo_da_data()
    {
        var esperada = Enumerable.Range(5, 7).Select(dia => new DateOnly(2026, 10, dia)).ToList();

        PeriodoDaAgenda.Ler(null, "2026-10-08", Hoje).Semana.Should().Equal(esperada);
        PeriodoDaAgenda.Ler("mes", "2026-10-11", Hoje).Semana.Should().Equal(esperada);
        PeriodoDaAgenda.Ler(null, "2026-12-31", Hoje).Semana.Should().Equal(
            Enumerable.Range(0, 7).Select(i => new DateOnly(2026, 12, 28).AddDays(i)));
    }

    [Theory]
    [InlineData("2026-10-08", 35, "2026-09-28", "2026-11-01")]
    [InlineData("2026-08-15", 42, "2026-07-27", "2026-09-06")]
    [InlineData("2027-02-10", 28, "2027-02-01", "2027-02-28")]
    public void Grade_do_mes_tem_semanas_inteiras_de_segunda_a_domingo(string data, int dias, string primeiro, string ultimo)
    {
        var grade = PeriodoDaAgenda.Ler("mes", data, Hoje).SemanasDoMes;

        grade.Should().HaveCount(dias);
        grade[0].Should().Be(D(primeiro));
        grade[^1].Should().Be(D(ultimo));
        grade[0].DayOfWeek.Should().Be(DayOfWeek.Monday);
        grade.Zip(grade.Skip(1)).Should().OnlyContain(par => par.Second == par.First.AddDays(1));
        // A grade é a do mês da data, qualquer que seja a visão.
        PeriodoDaAgenda.Ler(null, data, Hoje).SemanasDoMes.Should().Equal(grade);
    }

    [Theory]
    [InlineData(null, null, "", "/agenda")]
    [InlineData("semana", null, "?visao=semana", "/agenda?visao=semana")]
    [InlineData(null, "2026-10-08", "?data=2026-10-08", "/agenda?data=2026-10-08")]
    [InlineData("dia", "2026-10-08", "?visao=dia&data=2026-10-08", "/agenda?visao=dia&data=2026-10-08")]
    [InlineData("mes", "2026-10-08", "?visao=mes&data=2026-10-08", "/agenda?visao=mes&data=2026-10-08")]
    [InlineData("xyz", "2026-13-40", "", "/agenda")]
    public void Consulta_e_endereco_levam_so_o_que_esta_no_endereco(
        string? visao, string? data, string consulta, string endereco)
    {
        var periodo = PeriodoDaAgenda.Ler(visao, data, Hoje);

        periodo.Consulta.Should().Be(consulta);
        periodo.Endereco.Should().Be(endereco);
    }

    [Fact]
    public void Trocar_a_visao_mantem_a_data_e_trocar_a_data_a_poe_no_endereco()
    {
        PeriodoDaAgenda.Ler(null, null, Hoje).ComVisao(VisaoDaAgenda.Mes)
            .Should().Be(new PeriodoDaAgenda(VisaoDaAgenda.Mes, Hoje, false));
        PeriodoDaAgenda.Ler(null, null, Hoje).ComVisao(VisaoDaAgenda.Mes).Consulta.Should().Be("?visao=mes");
        PeriodoDaAgenda.Ler(null, "2026-10-06", Hoje).ComVisao(VisaoDaAgenda.Dia).Consulta
            .Should().Be("?visao=dia&data=2026-10-06");

        PeriodoDaAgenda.Ler("semana", null, Hoje).ComData(new DateOnly(2026, 10, 6))
            .Should().Be(new PeriodoDaAgenda(VisaoDaAgenda.Semana, new DateOnly(2026, 10, 6), true));
        PeriodoDaAgenda.Ler(null, null, Hoje).ComData(Hoje).Consulta.Should().Be("?data=2026-10-08");
    }

    [Theory]
    [InlineData("2026-10-08", VisaoDaAgenda.Dia, "2026-10-07", "2026-10-09")]
    [InlineData("2026-10-08", VisaoDaAgenda.Semana, "2026-10-01", "2026-10-15")]
    [InlineData("2026-10-08", VisaoDaAgenda.Mes, "2026-09-08", "2026-11-08")]
    [InlineData("2027-01-31", VisaoDaAgenda.Mes, "2026-12-31", "2027-02-28")]
    [InlineData("2026-03-31", VisaoDaAgenda.Mes, "2026-02-28", "2026-04-30")]
    [InlineData("2028-01-31", VisaoDaAgenda.Mes, "2027-12-31", "2028-02-29")]
    [InlineData("2026-12-31", VisaoDaAgenda.Dia, "2026-12-30", "2027-01-01")]
    [InlineData("2026-12-28", VisaoDaAgenda.Semana, "2026-12-21", "2027-01-04")]
    public void Anterior_e_proximo_andam_um_dia_uma_semana_ou_um_mes(
        string data, VisaoDaAgenda unidade, string anterior, string proximo)
    {
        var periodo = PeriodoDaAgenda.Ler("semana", data, Hoje);

        // A visão fica; a data passa a estar no endereço. Um mês a partir de 31/01 cai no último dia de fevereiro.
        periodo.Anterior(unidade).Should().Be(new PeriodoDaAgenda(VisaoDaAgenda.Semana, D(anterior), true));
        periodo.Proximo(unidade).Should().Be(new PeriodoDaAgenda(VisaoDaAgenda.Semana, D(proximo), true));
    }

    [Fact]
    public void Andar_sem_data_no_endereco_poe_a_data_e_mantem_a_ausencia_de_visao()
    {
        var seguinte = PeriodoDaAgenda.Ler(null, null, Hoje).Proximo(VisaoDaAgenda.Semana);

        seguinte.Should().Be(new PeriodoDaAgenda(null, new DateOnly(2026, 10, 15), true));
        seguinte.Consulta.Should().Be("?data=2026-10-15");
        PeriodoDaAgenda.Ler("mes", "2026-10-08", Hoje).Proximo(VisaoDaAgenda.Mes).Endereco
            .Should().Be("/agenda?visao=mes&data=2026-11-08");
    }

    [Fact]
    public void Hoje_volta_a_data_de_hoje_e_mantem_a_visao()
    {
        var periodo = PeriodoDaAgenda.Ler("mes", "2027-02-10", Hoje).ParaHoje(Hoje);

        periodo.Should().Be(new PeriodoDaAgenda(VisaoDaAgenda.Mes, Hoje, true));
        periodo.Endereco.Should().Be("/agenda?visao=mes&data=2026-10-08");
    }

    [Fact]
    public void Ficha_do_evento_leva_a_origem()
    {
        var id = Guid.Parse("0199a3f2-7c1e-7000-8000-000000000001");

        PeriodoDaAgenda.Ficha(id, "?visao=dia&data=2026-10-08")
            .Should().Be("/agenda/0199a3f2-7c1e-7000-8000-000000000001?visao=dia&data=2026-10-08");
        PeriodoDaAgenda.Ficha(id, "").Should().Be("/agenda/0199a3f2-7c1e-7000-8000-000000000001");
    }

    // A ficha e o formulário repassam o que receberam no endereço, só o que a agenda aceitaria.
    [Theory]
    [InlineData("dia", "2026-10-08", "?visao=dia&data=2026-10-08")]
    [InlineData("semana", null, "?visao=semana")]
    [InlineData(null, "2026-10-08", "?data=2026-10-08")]
    [InlineData("xyz", "2026-10-08", "?data=2026-10-08")]
    [InlineData("mes", "1999-01-01", "?visao=mes")]
    [InlineData("dia&x=1", "2026-10-08&x=1", "")]
    [InlineData(null, null, "")]
    public void Consulta_repassada_so_leva_valores_validos(string? visao, string? data, string consulta)
    {
        PeriodoDaAgenda.ConsultaDeOrigem(visao, data).Should().Be(consulta);
    }

    // ---- Régua do Dia ----

    [Fact]
    public void Regua_padrao_vai_das_8_as_19_e_posiciona_o_dia_do_quadro()
    {
        var protocolo = Evento("Protocolo de documentos", new DateTime(2026, 10, 8, 9, 30, 0), 60);
        var revisao = Evento("Revisão de minutas", new DateTime(2026, 10, 8, 15, 0, 0), 60);
        var deOntem = Evento("Alinhamento com o cliente", new DateTime(2026, 10, 7, 10, 0, 0), 90);

        var regua = ReguaDoDia.Montar(_fuso, Hoje, [deOntem, protocolo, revisao]);

        regua.HoraInicial.Should().Be(8);
        regua.HoraFinal.Should().Be(19);
        regua.Horas.Should().Be(11);
        // 09:30 é o sexto quarto depois das 08:00 e uma hora são quatro quartos; o evento de outro dia não entra.
        regua.Eventos.Should().Equal(new EventoNaRegua(protocolo, 6, 4, 1, 1), new EventoNaRegua(revisao, 28, 4, 1, 1));
    }

    [Fact]
    public void Dia_sem_eventos_tem_a_regua_padrao()
    {
        var regua = ReguaDoDia.Montar(_fuso, new DateOnly(2026, 10, 10), []);

        regua.HoraInicial.Should().Be(8);
        regua.HoraFinal.Should().Be(19);
        regua.Horas.Should().Be(11);
        regua.Eventos.Should().BeEmpty();
    }

    [Fact]
    public void Regua_se_estende_ate_a_hora_cheia_do_primeiro_inicio_e_a_seguinte_ao_ultimo_fim()
    {
        var cedo = Evento("Cedo", new DateTime(2026, 10, 8, 6, 30, 0), 30);       // 06:30 a 07:00
        var tarde = Evento("Tarde", new DateTime(2026, 10, 8, 19, 30, 0), 45);    // 19:30 a 20:15

        var regua = ReguaDoDia.Montar(_fuso, Hoje, [cedo, tarde]);

        regua.HoraInicial.Should().Be(6);
        regua.HoraFinal.Should().Be(21);
        regua.Horas.Should().Be(15);
        regua.Eventos.Should().Equal(new EventoNaRegua(cedo, 2, 2, 1, 1), new EventoNaRegua(tarde, 54, 3, 1, 1));
    }

    [Fact]
    public void Fim_em_hora_cheia_nao_estende_uma_hora_a_mais()
    {
        var setes = Evento("Às sete", new DateTime(2026, 10, 8, 7, 0, 0), 60);     // 07:00 a 08:00
        var ateVinte = Evento("Até as vinte", new DateTime(2026, 10, 8, 17, 0, 0), 180);   // 17:00 a 20:00

        var regua = ReguaDoDia.Montar(_fuso, Hoje, [setes, ateVinte]);

        regua.HoraInicial.Should().Be(7);
        regua.HoraFinal.Should().Be(20);
        regua.Eventos.Should().Equal(new EventoNaRegua(setes, 0, 4, 1, 1), new EventoNaRegua(ateVinte, 40, 12, 1, 1));
    }

    // Review Focus 2: o evento entra em cada dia que toca, recortado ao dia; a régua vai então a 24:00 ou a 00:00.
    [Fact]
    public void Evento_que_atravessa_a_meia_noite_entra_recortado_em_cada_dia()
    {
        var plantao = Evento("Plantão", new DateTime(2026, 10, 7, 23, 30, 0), 60);              // 23:30 a 00:30
        var ateMeiaNoite = Evento("Até a meia-noite", new DateTime(2026, 10, 7, 22, 0, 0), 120); // 22:00 a 00:00
        var tresDias = Evento("Três dias", new DateTime(2026, 10, 7, 22, 0, 0), 28 * 60);        // quarta 22:00 a sexta 02:00

        var plantaoNaQuarta = ReguaDoDia.Montar(_fuso, new DateOnly(2026, 10, 7), [plantao]);
        var plantaoNaQuinta = ReguaDoDia.Montar(_fuso, Hoje, [plantao]);
        var ateMeiaNoiteNaQuarta = ReguaDoDia.Montar(_fuso, new DateOnly(2026, 10, 7), [ateMeiaNoite]);
        var ateMeiaNoiteNaQuinta = ReguaDoDia.Montar(_fuso, Hoje, [ateMeiaNoite]);
        var tresDiasNaQuinta = ReguaDoDia.Montar(_fuso, Hoje, [tresDias]);

        plantaoNaQuarta.HoraInicial.Should().Be(8);
        plantaoNaQuarta.HoraFinal.Should().Be(24);
        plantaoNaQuarta.Eventos.Should().Equal(new EventoNaRegua(plantao, 62, 2, 1, 1));
        plantaoNaQuinta.HoraInicial.Should().Be(0);
        plantaoNaQuinta.HoraFinal.Should().Be(19);
        plantaoNaQuinta.Eventos.Should().Equal(new EventoNaRegua(plantao, 0, 2, 1, 1));
        // Quem termina à meia-noite em ponto não aparece no dia seguinte.
        ateMeiaNoiteNaQuarta.HoraFinal.Should().Be(24);
        ateMeiaNoiteNaQuarta.Eventos.Should().Equal(new EventoNaRegua(ateMeiaNoite, 56, 8, 1, 1));
        ateMeiaNoiteNaQuinta.Eventos.Should().BeEmpty();
        ateMeiaNoiteNaQuinta.HoraFinal.Should().Be(19);
        // No dia do meio, o evento ocupa o dia inteiro: 96 quartos.
        tresDiasNaQuinta.HoraInicial.Should().Be(0);
        tresDiasNaQuinta.HoraFinal.Should().Be(24);
        tresDiasNaQuinta.Horas.Should().Be(24);
        tresDiasNaQuinta.Eventos.Should().Equal(new EventoNaRegua(tresDias, 0, 96, 1, 1));
    }

    // Review Focus 3: o evento curto ganha a altura mínima de dois quartos; o início arredonda para baixo e o fim para
    // cima; o curto no fim do dia sobe para caber.
    [Fact]
    public void Evento_curto_ganha_dois_quartos_e_horario_quebrado_arredonda_para_fora()
    {
        var quinze = Evento("Quinze minutos", new DateTime(2026, 10, 8, 10, 0, 0), 15);     // 10:00 a 10:15
        var quebrado = Evento("Quebrado", new DateTime(2026, 10, 8, 14, 5, 0), 30);         // 14:05 a 14:35
        var noFim = Evento("No fim do dia", new DateTime(2026, 10, 8, 23, 50, 0), 10);      // 23:50 a 00:00

        var regua = ReguaDoDia.Montar(_fuso, Hoje, [quinze, quebrado, noFim]);

        regua.HoraInicial.Should().Be(8);
        regua.HoraFinal.Should().Be(24);
        regua.Eventos.Should().Equal(
            new EventoNaRegua(quinze, 8, 2, 1, 1),
            new EventoNaRegua(quebrado, 24, 3, 1, 1),
            new EventoNaRegua(noFim, 62, 2, 1, 1));
    }

    // Review Focus 3: dois simultâneos lado a lado; o evento depois do grupo volta à largura inteira.
    [Fact]
    public void Simultaneos_dividem_a_largura_e_o_seguinte_volta_a_largura_inteira()
    {
        var alinhamento = Evento("Alinhamento com o cliente", new DateTime(2026, 10, 8, 10, 0, 0), 90);  // 10:00 a 11:30
        var conciliacao = Evento("Audiência de conciliação", new DateTime(2026, 10, 8, 11, 0, 0), 60);   // 11:00 a 12:00
        var depois = Evento("Depois do grupo", new DateTime(2026, 10, 8, 13, 0, 0), 60);                 // 13:00 a 14:00

        var regua = ReguaDoDia.Montar(_fuso, Hoje, [alinhamento, conciliacao, depois]);

        regua.Eventos.Should().Equal(
            new EventoNaRegua(alinhamento, 8, 6, 1, 2),
            new EventoNaRegua(conciliacao, 12, 4, 2, 2),
            new EventoNaRegua(depois, 20, 4, 1, 1));
    }

    [Fact]
    public void Evento_ocupa_a_primeira_coluna_livre_do_grupo()
    {
        var primeiro = Evento("Primeiro", new DateTime(2026, 10, 8, 10, 0, 0), 60);    // 10:00 a 11:00
        var segundo = Evento("Segundo", new DateTime(2026, 10, 8, 10, 30, 0), 60);     // 10:30 a 11:30
        var terceiro = Evento("Terceiro", new DateTime(2026, 10, 8, 11, 0, 0), 60);    // 11:00 a 12:00

        var regua = ReguaDoDia.Montar(_fuso, Hoje, [primeiro, segundo, terceiro]);

        // Os três formam um grupo (cada um encosta no seguinte); o terceiro reaproveita a coluna do primeiro.
        regua.Eventos.Should().Equal(
            new EventoNaRegua(primeiro, 8, 4, 1, 2),
            new EventoNaRegua(segundo, 10, 4, 2, 2),
            new EventoNaRegua(terceiro, 12, 4, 1, 2));
    }

    [Fact]
    public void Cinco_simultaneos_ficam_em_quatro_colunas_e_o_quinto_divide_a_ultima()
    {
        var eventos = Enumerable.Range(1, 5)
            .Select(n => Evento($"Simultâneo {n}", new DateTime(2026, 10, 8, 10, 0, 0), 60)).ToList();

        var regua = ReguaDoDia.Montar(_fuso, Hoje, eventos);

        regua.Eventos.Should().Equal(
            new EventoNaRegua(eventos[0], 8, 4, 1, 4),
            new EventoNaRegua(eventos[1], 8, 4, 2, 4),
            new EventoNaRegua(eventos[2], 8, 4, 3, 4),
            new EventoNaRegua(eventos[3], 8, 4, 4, 4),
            new EventoNaRegua(eventos[4], 8, 4, 4, 4));
    }

    // ---- Rótulos ----

    [Theory]
    [InlineData(5, "Seg", "Segunda", "Segunda-feira")]
    [InlineData(6, "Ter", "Terça", "Terça-feira")]
    [InlineData(7, "Qua", "Quarta", "Quarta-feira")]
    [InlineData(8, "Qui", "Quinta", "Quinta-feira")]
    [InlineData(9, "Sex", "Sexta", "Sexta-feira")]
    [InlineData(10, "Sáb", "Sábado", "Sábado")]
    [InlineData(11, "Dom", "Domingo", "Domingo")]
    public void Nomes_do_dia_da_semana(int dia, string sigla, string nome, string nomeLongo)
    {
        var data = new DateOnly(2026, 10, dia);

        RotulosDeAgenda.SiglaDoDia(data).Should().Be(sigla);
        RotulosDeAgenda.NomeDoDia(data).Should().Be(nome);
        RotulosDeAgenda.NomeLongoDoDia(data).Should().Be(nomeLongo);
    }

    [Fact]
    public void Rotulos_do_dia_para_leitor_de_tela()
    {
        RotulosDeAgenda.RotuloLongo(Hoje, hoje: true).Should().Be("quinta-feira, 08/10/2026, hoje");
        RotulosDeAgenda.RotuloLongo(new DateOnly(2026, 10, 10), hoje: false).Should().Be("sábado, 10/10/2026");
        RotulosDeAgenda.RotuloCurto(Hoje, hoje: true).Should().Be("Quinta, 08/10, hoje");
        RotulosDeAgenda.RotuloCurto(new DateOnly(2026, 10, 5), hoje: false).Should().Be("Segunda, 05/10");
    }

    [Fact]
    public void Subtitulos_de_cada_visao()
    {
        RotulosDeAgenda.SubtituloDaSemana(new DateOnly(2026, 10, 5)).Should().Be("Semana de 05/10 a 11/10/2026");
        RotulosDeAgenda.SubtituloDaSemana(new DateOnly(2026, 9, 28)).Should().Be("Semana de 28/09 a 04/10/2026");
        RotulosDeAgenda.SubtituloDaSemana(new DateOnly(2026, 12, 28)).Should().Be("Semana de 28/12/2026 a 03/01/2027");
        RotulosDeAgenda.SubtituloDoDia(Hoje).Should().Be("Quinta-feira, 08/10/2026");
        RotulosDeAgenda.SubtituloDoDia(new DateOnly(2026, 10, 11)).Should().Be("Domingo, 11/10/2026");
        RotulosDeAgenda.SubtituloDoMes(Hoje).Should().Be("Outubro de 2026");
        RotulosDeAgenda.SubtituloDoMes(new DateOnly(2026, 3, 1)).Should().Be("Março de 2026");
        RotulosDeAgenda.SubtituloDoMes(new DateOnly(2027, 2, 10)).Should().Be("Fevereiro de 2027");
        RotulosDeAgenda.MesDoMobile(Hoje).Should().Be("Outubro 2026");
        RotulosDeAgenda.MesDoMobile(new DateOnly(2027, 2, 10)).Should().Be("Fevereiro 2027");
    }

    [Theory]
    [InlineData(0, "0 eventos", "0 minutos")]
    [InlineData(1, "1 evento", "1 minuto")]
    [InlineData(2, "2 eventos", "2 minutos")]
    [InlineData(90, "90 eventos", "90 minutos")]
    public void Total_de_eventos_e_duracao_no_numero_certo(int quantidade, string eventos, string duracao)
    {
        RotulosDeAgenda.TotalDeEventos(quantidade).Should().Be(eventos);
        RotulosDeAgenda.Duracao(quantidade).Should().Be(duracao);
    }

    [Fact]
    public void Hora_do_cartao_mostra_reticencias_do_lado_que_cai_em_outro_dia()
    {
        var quarta = new DateOnly(2026, 10, 7);
        var plantao = Evento("Plantão", new DateTime(2026, 10, 7, 23, 30, 0), 60);               // 23:30 a 00:30
        var ateMeiaNoite = Evento("Até a meia-noite", new DateTime(2026, 10, 7, 22, 0, 0), 120);  // 22:00 a 00:00
        var tresDias = Evento("Três dias", new DateTime(2026, 10, 7, 22, 0, 0), 28 * 60);         // quarta 22:00 a sexta 02:00
        var comum = Evento("Comum", new DateTime(2026, 10, 8, 10, 0, 0), 60);

        RotulosDeAgenda.HoraDeInicio(_fuso, plantao, quarta).Should().Be("23:30");
        RotulosDeAgenda.HoraDeFim(_fuso, plantao, quarta).Should().Be("…");
        RotulosDeAgenda.HoraDeInicio(_fuso, plantao, Hoje).Should().Be("…");
        RotulosDeAgenda.HoraDeFim(_fuso, plantao, Hoje).Should().Be("00:30");
        // Terminar à meia-noite em ponto é terminar no mesmo dia.
        RotulosDeAgenda.HoraDeFim(_fuso, ateMeiaNoite, quarta).Should().Be("00:00");
        RotulosDeAgenda.HoraDeInicio(_fuso, tresDias, Hoje).Should().Be("…");
        RotulosDeAgenda.HoraDeFim(_fuso, tresDias, Hoje).Should().Be("…");
        RotulosDeAgenda.HoraDeInicio(_fuso, comum, Hoje).Should().Be("10:00");
        RotulosDeAgenda.HoraDeFim(_fuso, comum, Hoje).Should().Be("11:00");
    }

    [Fact]
    public void Linhas_do_cartao_por_largura()
    {
        var audiencia = Cartao("2ª Vara do Trabalho", "Transportes Litoral Ltda.", "Beatriz Lins");
        var prazo = Cartao(null, "Construtora Serra Azul Ltda.", "Rafael Teles");
        var pauta = Cartao("Sala de reuniões", null, "Marina Couto", "Rafael Teles", "Beatriz Lins");
        var nada = Cartao("  ", null);
        var comSumido = Cartao(null, null, "", "Pedro Produção");

        // Tablet: uma linha só, o local ou, sem local, o cliente.
        RotulosDeAgenda.LinhaDoTablet(audiencia).Should().Be("2ª Vara do Trabalho");
        RotulosDeAgenda.LinhaDoTablet(prazo).Should().Be("Construtora Serra Azul Ltda.");
        RotulosDeAgenda.LinhaDoTablet(nada).Should().BeNull();
        // Mobile: local e cliente juntos, o que houver.
        RotulosDeAgenda.LinhaDoMobile(audiencia).Should().Be("2ª Vara do Trabalho · Transportes Litoral Ltda.");
        RotulosDeAgenda.LinhaDoMobile(prazo).Should().Be("Construtora Serra Azul Ltda.");
        RotulosDeAgenda.LinhaDoMobile(pauta).Should().Be("Sala de reuniões");
        RotulosDeAgenda.LinhaDoMobile(nada).Should().BeNull();
        // Régua: local, cliente e participantes.
        RotulosDeAgenda.LinhaDaRegua(audiencia).Should().Be("2ª Vara do Trabalho · Transportes Litoral Ltda. · Beatriz Lins");
        RotulosDeAgenda.LinhaDaRegua(pauta).Should().Be("Sala de reuniões · Marina Couto, Rafael Teles, Beatriz Lins");
        RotulosDeAgenda.LinhaDaRegua(nada).Should().BeNull();
        // Participantes na ordem recebida (a do servidor); o usuário que não é mais encontrado (nome vazio) aparece como
        // tal, também na linha da régua.
        RotulosDeAgenda.Participantes(pauta).Should().Be("Marina Couto, Rafael Teles, Beatriz Lins");
        RotulosDeAgenda.Participantes(comSumido).Should().Be("Usuário não encontrado, Pedro Produção");
        RotulosDeAgenda.LinhaDaRegua(comSumido).Should().Be("Usuário não encontrado, Pedro Produção");
        RotulosDeAgenda.Participantes(nada).Should().BeNull();
    }

    [Fact]
    public void Titulo_do_dia_no_desktop_e_no_mobile()
    {
        RotulosDeAgenda.TituloDoDiaNoDesktop(Hoje, hoje: true).Should().Be("Quinta-feira · hoje");
        RotulosDeAgenda.TituloDoDiaNoDesktop(new DateOnly(2026, 10, 10), hoje: false).Should().Be("Sábado");
        RotulosDeAgenda.TituloDoDiaNoMobile(Hoje, hoje: true).Should().Be("Quinta, 08/10 · hoje");
        RotulosDeAgenda.TituloDoDiaNoMobile(new DateOnly(2026, 10, 5), hoje: false).Should().Be("Segunda, 05/10");
    }

    [Fact]
    public void Resumo_do_evento_na_ficha()
    {
        var audiencia = Detalhe(TipoEvento.Audiencia, new DateTime(2026, 10, 7, 9, 0, 0), 90, "Fórum Trabalhista, sala 4");
        var semLocal = Detalhe(TipoEvento.Audiencia, new DateTime(2026, 10, 7, 9, 0, 0), 90, null);
        var plantao = Detalhe(TipoEvento.Reuniao, new DateTime(2026, 10, 7, 23, 30, 0), 60, "Sala de reuniões");
        var ateMeiaNoite = Detalhe(TipoEvento.Prazo, new DateTime(2026, 10, 7, 22, 0, 0), 120, null);

        RotulosDeAgenda.ResumoDoEvento(_fuso, audiencia, comLocal: true)
            .Should().Be("Audiência · 07/10/2026, 09:00 – 10:30 · Fórum Trabalhista, sala 4");
        RotulosDeAgenda.ResumoDoEvento(_fuso, audiencia, comLocal: false).Should().Be("Audiência · 07/10/2026, 09:00 – 10:30");
        RotulosDeAgenda.ResumoDoEvento(_fuso, semLocal, comLocal: true).Should().Be("Audiência · 07/10/2026, 09:00 – 10:30");
        RotulosDeAgenda.ResumoDoEvento(_fuso, plantao, comLocal: true)
            .Should().Be("Reunião · 07/10/2026, 23:30 – 08/10 00:30 · Sala de reuniões");
        RotulosDeAgenda.ResumoDoEvento(_fuso, ateMeiaNoite, comLocal: true).Should().Be("Prazo · 07/10/2026, 22:00 – 00:00");
    }

    [Fact]
    public void Conflito_com_uma_pessoa_nas_tres_redacoes()
    {
        SobreposicaoDeAgenda[] uma = [Sobreposicao("Rafael Teles", new DateTime(2026, 10, 7, 10, 0, 0), new DateTime(2026, 10, 7, 11, 30, 0))];

        RotulosDeAgenda.ConflitoNoDesktop(_fuso, uma).Should().Be("Atenção: Rafael Teles já tem evento das 10:00 às 11:30 (07/10).");
        RotulosDeAgenda.ConflitoNoTablet(_fuso, uma).Should().Be("Rafael Teles já tem evento das 10:00 às 11:30 (07/10).");
        RotulosDeAgenda.ConflitoNoMobile(_fuso, uma).Should().Be("Rafael Teles já tem evento das 10:00 às 11:30 em 07/10.");
    }

    [Fact]
    public void Conflito_com_duas_pessoas_separa_por_ponto_e_virgula()
    {
        SobreposicaoDeAgenda[] duas =
        [
            Sobreposicao("Rafael Teles", new DateTime(2026, 10, 7, 10, 0, 0), new DateTime(2026, 10, 7, 11, 30, 0)),
            Sobreposicao("Marina Couto", new DateTime(2026, 10, 7, 11, 0, 0), new DateTime(2026, 10, 7, 12, 0, 0)),
        ];

        RotulosDeAgenda.ConflitoNoDesktop(_fuso, duas).Should().Be(
            "Atenção: Rafael Teles já tem evento das 10:00 às 11:30 (07/10); Marina Couto já tem evento das 11:00 às 12:00 (07/10).");
        RotulosDeAgenda.ConflitoNoTablet(_fuso, duas).Should().Be(
            "Rafael Teles já tem evento das 10:00 às 11:30 (07/10); Marina Couto já tem evento das 11:00 às 12:00 (07/10).");
        RotulosDeAgenda.ConflitoNoMobile(_fuso, duas).Should().Be(
            "Rafael Teles já tem evento das 10:00 às 11:30 em 07/10; Marina Couto já tem evento das 11:00 às 12:00 em 07/10.");
        // A mensagem de hoje é a redação do desktop depois de "Evento salvo.".
        RotulosDeAgenda.MensagemDeSalvo(_fuso, duas).Should().Be("Evento salvo. " + RotulosDeAgenda.ConflitoNoDesktop(_fuso, duas));
    }

    [Fact]
    public void Conflito_que_atravessa_a_meia_noite_tem_a_mesma_forma_nas_tres_e_nome_nulo_vira_usuario()
    {
        SobreposicaoDeAgenda[] sobreposicoes =
        [
            Sobreposicao("Pedro", new DateTime(2026, 3, 10, 22, 0, 0), new DateTime(2026, 3, 11, 1, 0, 0)),
            Sobreposicao(null, new DateTime(2026, 10, 7, 10, 0, 0), new DateTime(2026, 10, 7, 11, 0, 0)),
        ];

        RotulosDeAgenda.ConflitoNoDesktop(_fuso, sobreposicoes).Should().Be(
            "Atenção: Pedro já tem evento de 10/03 22:00 a 11/03 01:00; Usuário já tem evento das 10:00 às 11:00 (07/10).");
        RotulosDeAgenda.ConflitoNoTablet(_fuso, sobreposicoes).Should().Be(
            "Pedro já tem evento de 10/03 22:00 a 11/03 01:00; Usuário já tem evento das 10:00 às 11:00 (07/10).");
        RotulosDeAgenda.ConflitoNoMobile(_fuso, sobreposicoes).Should().Be(
            "Pedro já tem evento de 10/03 22:00 a 11/03 01:00; Usuário já tem evento das 10:00 às 11:00 em 07/10.");
    }

    [Fact]
    public void Formatos_novos_nao_dependem_da_cultura_da_thread()
    {
        var plantao = Evento("Plantão", new DateTime(2026, 10, 7, 23, 30, 0), 60);
        SobreposicaoDeAgenda[] uma = [Sobreposicao("Rafael Teles", new DateTime(2026, 10, 7, 10, 0, 0), new DateTime(2026, 10, 7, 11, 30, 0))];
        var antes = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");   // outros algarismos e outro calendário

            PeriodoDaAgenda.Ler("dia", "2026-10-08", Hoje).Should().Be(new PeriodoDaAgenda(VisaoDaAgenda.Dia, Hoje, true));
            PeriodoDaAgenda.Ler("dia", "2026-10-08", Hoje).Consulta.Should().Be("?visao=dia&data=2026-10-08");
            RotulosDeAgenda.SubtituloDaSemana(new DateOnly(2026, 10, 5)).Should().Be("Semana de 05/10 a 11/10/2026");
            RotulosDeAgenda.SubtituloDoMes(Hoje).Should().Be("Outubro de 2026");
            RotulosDeAgenda.RotuloCurto(Hoje, hoje: true).Should().Be("Quinta, 08/10, hoje");
            RotulosDeAgenda.HoraDeInicio(_fuso, plantao, new DateOnly(2026, 10, 7)).Should().Be("23:30");
            RotulosDeAgenda.ConflitoNoMobile(_fuso, uma).Should().Be("Rafael Teles já tem evento das 10:00 às 11:30 em 07/10.");
        }
        finally
        {
            CultureInfo.CurrentCulture = antes;
        }
    }
}
```

Run: `dotnet build -m:1`
Expected: não compila (`VisaoDaAgenda`, `PeriodoDaAgenda`, `ReguaDoDia`, `EventoNaRegua` e os rótulos novos de `RotulosDeAgenda` não existem). É o RED.

- [ ] **Step 2: Visão e período**

`src/AtividadesJuridicas.Web/Components/Agenda/VisaoDaAgenda.cs`:

```csharp
namespace AtividadesJuridicas.Web.Components.Agenda;

/// <summary>As três visões da agenda, como aparecem no endereço: <c>?visao=dia</c>, <c>semana</c> ou <c>mes</c>.</summary>
public enum VisaoDaAgenda
{
    Dia,
    Semana,
    Mes,
}
```

`src/AtividadesJuridicas.Web/Components/Agenda/PeriodoDaAgenda.cs`:

```csharp
using System.Globalization;
using AtividadesJuridicas.Domain.Agenda;

namespace AtividadesJuridicas.Web.Components.Agenda;

/// <summary>
/// O período que a agenda mostra, lido do endereço (<c>/agenda?visao=dia|semana|mes&amp;data=yyyy-MM-dd</c>).
/// <paramref name="Visao"/> nula quer dizer "sem visao no endereço": cada largura mostra o seu padrão (Semana no
/// desktop e no tablet, Dia no mobile). <paramref name="DataNoEndereco"/> diz se a data veio do endereço (ou de uma
/// navegação), para o endereço gerado só levar o que o usuário escolheu.
/// </summary>
public sealed record PeriodoDaAgenda(VisaoDaAgenda? Visao, DateOnly Data, bool DataNoEndereco)
{
    /// <summary>
    /// Lê o endereço. Visão desconhecida conta como ausente; data inválida ou fora dos anos em que um evento pode
    /// começar (2000 a 2100) vira <paramref name="hoje"/>. Nada disso é erro.
    /// </summary>
    public static PeriodoDaAgenda Ler(string? visao, string? data, DateOnly hoje)
    {
        var lida = LerData(data);
        return new PeriodoDaAgenda(LerVisao(visao), lida ?? hoje, lida is not null);
    }

    /// <summary>A visão do desktop e do tablet: a do endereço ou, sem ela, a Semana.</summary>
    public VisaoDaAgenda NoDesktopETablet => Visao ?? VisaoDaAgenda.Semana;

    /// <summary>A visão do mobile: a do endereço ou, sem ela, o Dia.</summary>
    public VisaoDaAgenda NoMobile => Visao ?? VisaoDaAgenda.Dia;

    /// <summary>
    /// O primeiro dia consultado: a segunda-feira da semana de <see cref="Data"/> (Dia, Semana e sem visão, que mostram
    /// a mesma semana nas três larguras) ou o primeiro dia da grade do mês (Mês).
    /// </summary>
    public DateOnly InicioDaConsulta => Visao == VisaoDaAgenda.Mes ? InicioDaGradeDoMes : RotulosDeAgenda.SegundaDe(Data);

    /// <summary>O dia seguinte ao último consultado (exclusivo).</summary>
    public DateOnly FimDaConsulta => Visao == VisaoDaAgenda.Mes ? FimDaGradeDoMes : RotulosDeAgenda.SegundaDe(Data).AddDays(7);

    /// <summary>Os sete dias da semana de <see cref="Data"/>, de segunda a domingo.</summary>
    public IReadOnlyList<DateOnly> Semana => Dias(RotulosDeAgenda.SegundaDe(Data), 7);

    /// <summary>
    /// As datas da grade do mês de <see cref="Data"/>: semanas inteiras de segunda a domingo, do primeiro ao último dia
    /// do mês (4, 5 ou 6 semanas; 28 a 42 datas), qualquer que seja a visão.
    /// </summary>
    public IReadOnlyList<DateOnly> SemanasDoMes => Dias(InicioDaGradeDoMes, FimDaGradeDoMes.DayNumber - InicioDaGradeDoMes.DayNumber);

    /// <summary>
    /// O que vai depois de <c>/agenda</c>: <c>""</c>, <c>?visao=semana</c>, <c>?data=2026-10-08</c> ou
    /// <c>?visao=dia&amp;data=2026-10-08</c>. A visão quando houver; a data quando estiver no endereço.
    /// </summary>
    public string Consulta
    {
        get
        {
            var partes = new List<string>(2);
            if (Visao is { } visao) partes.Add($"visao={NoEndereco(visao)}");
            if (DataNoEndereco) partes.Add($"data={Data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
            return partes.Count == 0 ? string.Empty : "?" + string.Join("&", partes);
        }
    }

    /// <summary>O endereço da agenda neste período.</summary>
    public string Endereco => $"/agenda{Consulta}";

    /// <summary>A mesma data em outra visão (o controle Dia/Semana/Mês).</summary>
    public PeriodoDaAgenda ComVisao(VisaoDaAgenda visao) => this with { Visao = visao };

    /// <summary>Outra data, na mesma visão (a faixa de dias e a grade compacta do mobile).</summary>
    public PeriodoDaAgenda ComData(DateOnly data) => this with { Data = data, DataNoEndereco = true };

    /// <summary>
    /// O período anterior: um dia, uma semana ou um mês antes, conforme a <paramref name="unidade"/> (quem chama decide
    /// pela visão de cada largura). Um mês antes de 31/03 é o último dia de fevereiro.
    /// </summary>
    public PeriodoDaAgenda Anterior(VisaoDaAgenda unidade) => Andar(unidade, -1);

    /// <summary>O período seguinte: um dia, uma semana ou um mês depois. Um mês depois de 31/01 é o último dia de fevereiro.</summary>
    public PeriodoDaAgenda Proximo(VisaoDaAgenda unidade) => Andar(unidade, 1);

    /// <summary>O botão "Hoje": a data de hoje, na mesma visão.</summary>
    public PeriodoDaAgenda ParaHoje(DateOnly hoje) => this with { Data = hoje, DataNoEndereco = true };

    /// <summary>O endereço da ficha do evento com a origem: <c>/agenda/{id}?visao=…&amp;data=…</c>.</summary>
    public static string Ficha(Guid eventoId, string consulta) => $"/agenda/{eventoId}{consulta}";

    /// <summary>
    /// A consulta que a ficha e o formulário do evento repassam ao voltar: o que receberam no endereço, só o que
    /// <see cref="Ler"/> aceitaria (o resto some).
    /// </summary>
    public static string ConsultaDeOrigem(string? visao, string? data)
    {
        var lida = LerData(data);
        return new PeriodoDaAgenda(LerVisao(visao), lida ?? default, lida is not null).Consulta;
    }

    private DateOnly InicioDaGradeDoMes => RotulosDeAgenda.SegundaDe(new DateOnly(Data.Year, Data.Month, 1));

    private DateOnly FimDaGradeDoMes =>
        RotulosDeAgenda.SegundaDe(new DateOnly(Data.Year, Data.Month, 1).AddMonths(1).AddDays(-1)).AddDays(7);

    private PeriodoDaAgenda Andar(VisaoDaAgenda unidade, int sentido) => this with
    {
        Data = unidade switch
        {
            VisaoDaAgenda.Dia => Data.AddDays(sentido),
            VisaoDaAgenda.Semana => Data.AddDays(7 * sentido),
            // DateOnly.AddMonths leva ao último dia do mês quando o dia não existe nele (31/01 + 1 mês = 28/02).
            _ => Data.AddMonths(sentido),
        },
        DataNoEndereco = true,
    };

    private static IReadOnlyList<DateOnly> Dias(DateOnly inicio, int quantidade) =>
        Enumerable.Range(0, quantidade).Select(i => inicio.AddDays(i)).ToArray();

    private static VisaoDaAgenda? LerVisao(string? visao) => visao switch
    {
        "dia" => VisaoDaAgenda.Dia,
        "semana" => VisaoDaAgenda.Semana,
        "mes" => VisaoDaAgenda.Mes,
        _ => null,
    };

    private static string NoEndereco(VisaoDaAgenda visao) => visao switch
    {
        VisaoDaAgenda.Dia => "dia",
        VisaoDaAgenda.Semana => "semana",
        _ => "mes",
    };

    // Só "yyyy-MM-dd" com algarismos e calendário fixos, nos anos em que um evento pode começar.
    private static DateOnly? LerData(string? data) =>
        DateOnly.TryParseExact(data, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lida)
        && lida.Year is >= Evento.AnoMinimoDeInicio and <= Evento.AnoMaximoDeInicio
            ? lida
            : null;
}
```

Notas para quem implementa:

- As propriedades calculadas não têm campo: a igualdade do record continua sendo só `Visao`, `Data` e `DataNoEndereco` (os testes comparam períodos inteiros com `Should().Be`). Não guarde listas em campo.
- O estático que a ficha e o formulário usam para repassar a origem chama-se `ConsultaDeOrigem` (um tipo C# não pode ter a propriedade `Consulta` e um método `Consulta`, CS0102). As Tarefas 4 e 5 usam `PeriodoDaAgenda.ConsultaDeOrigem(Visao, Data)`.

- [ ] **Step 3: Régua do Dia**

`src/AtividadesJuridicas.Web/Components/Agenda/ReguaDoDia.cs`:

```csharp
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.Web.Components.Agenda;

/// <summary>
/// Um evento posicionado na régua do Dia, em quartos de hora: <paramref name="InicioEmQuartos"/> conta a partir da
/// <see cref="ReguaDoDia.HoraInicial"/> (0 a 95) e <paramref name="Quartos"/> é a altura (2 a 96). Eventos que se
/// sobrepõem dividem a largura: o cartão ocupa a coluna <paramref name="Parte"/> de <paramref name="Partes"/> (1 a 4).
/// </summary>
public sealed record EventoNaRegua(EventoResumo Evento, int InicioEmQuartos, int Quartos, int Parte, int Partes);

/// <summary>
/// A régua de horas do Dia no desktop: das 08:00 às 19:00, estendida até a hora cheia do primeiro início e até a
/// hora cheia seguinte ao último fim do dia, com os eventos do dia posicionados por quartos de hora (a tela os põe
/// numa grade CSS por classes, sem atributo <c>style</c>).
/// </summary>
public sealed record ReguaDoDia(int HoraInicial, int HoraFinal, IReadOnlyList<EventoNaRegua> Eventos)
{
    public const int HoraInicialPadrao = 8;
    public const int HoraFinalPadrao = 19;

    /// <summary>Do quinto evento simultâneo em diante, o evento divide a última coluna com quem estiver nela.</summary>
    public const int MaximoDeColunas = 4;

    private const int QuartosPorHora = 4;
    private const int QuartosNoDia = 96;
    private const int QuartosMinimos = 2;
    private const int MinutosPorQuarto = 15;
    private const int MinutosNoDia = 24 * 60;

    /// <summary>Quantas horas a régua mostra (1 a 24): a classe <c>aj-regua__grade--horas-{n}</c>.</summary>
    public int Horas => HoraFinal - HoraInicial;

    /// <summary>
    /// Monta a régua do <paramref name="dia"/> com os eventos que o tocam (os outros são ignorados). Evento de outro dia
    /// entra recortado ao dia (de 00:00, ou até 24:00). O início arredonda para baixo e o fim para cima, com no mínimo
    /// dois quartos (o evento curto ganha altura para o cartão caber). Eventos que se sobrepõem, já arredondados, formam
    /// um grupo; cada um ocupa a primeira coluna livre do grupo, e o grupo divide a largura em até quatro colunas.
    /// </summary>
    public static ReguaDoDia Montar(FusoDoEscritorio fuso, DateOnly dia, IEnumerable<EventoResumo> eventos)
    {
        // A ordem de chegada (a do servidor: início, depois Id) desempata os que começam no mesmo quarto.
        var trechos = eventos
            .Where(evento => RotulosDeAgenda.TocaODia(fuso, evento, dia))
            .Select((evento, ordem) => Trecho.De(fuso, evento, dia, ordem))
            .OrderBy(trecho => trecho.Inicio).ThenBy(trecho => trecho.Ordem)
            .ToList();

        var horaInicial = trechos.Count == 0
            ? HoraInicialPadrao
            : Math.Min(HoraInicialPadrao, trechos.Min(trecho => trecho.Inicio) / QuartosPorHora);
        var horaFinal = trechos.Count == 0
            ? HoraFinalPadrao
            : Math.Max(HoraFinalPadrao, (trechos.Max(trecho => trecho.Fim) + QuartosPorHora - 1) / QuartosPorHora);

        var posicionados = new List<EventoNaRegua>(trechos.Count);
        var primeiro = 0;
        while (primeiro < trechos.Count)
        {
            // O grupo: o trecho e todos os seguintes que começam antes de o grupo terminar.
            var fimDoGrupo = trechos[primeiro].Fim;
            var depois = primeiro + 1;
            while (depois < trechos.Count && trechos[depois].Inicio < fimDoGrupo)
            {
                fimDoGrupo = Math.Max(fimDoGrupo, trechos[depois].Fim);
                depois++;
            }

            var grupo = trechos.GetRange(primeiro, depois - primeiro);
            var fimDaColuna = new List<int>(MaximoDeColunas);   // o fim do último trecho posto em cada coluna
            var colunas = new int[grupo.Count];
            for (var i = 0; i < grupo.Count; i++)
            {
                var livre = fimDaColuna.FindIndex(fim => fim <= grupo[i].Inicio);
                if (livre < 0 && fimDaColuna.Count < MaximoDeColunas)
                {
                    fimDaColuna.Add(0);
                    livre = fimDaColuna.Count - 1;
                }
                else if (livre < 0)
                {
                    livre = MaximoDeColunas - 1;
                }

                fimDaColuna[livre] = Math.Max(fimDaColuna[livre], grupo[i].Fim);
                colunas[i] = livre;
            }

            for (var i = 0; i < grupo.Count; i++)
                posicionados.Add(new EventoNaRegua(
                    grupo[i].Evento, grupo[i].Inicio - horaInicial * QuartosPorHora, grupo[i].Fim - grupo[i].Inicio,
                    colunas[i] + 1, fimDaColuna.Count));

            primeiro = depois;
        }

        return new ReguaDoDia(horaInicial, horaFinal, posicionados);
    }

    // O pedaço do evento dentro do dia, em quartos de hora desde a meia-noite (0 a 96), já arredondado.
    private sealed record Trecho(EventoResumo Evento, int Ordem, int Inicio, int Fim)
    {
        public static Trecho De(FusoDoEscritorio fuso, EventoResumo evento, DateOnly dia, int ordem)
        {
            var inicio = fuso.ParaLocal(evento.InicioEm);
            var fim = fuso.ParaLocal(evento.FimEm);

            // Começou noutro dia: entra da meia-noite. Termina noutro dia (ou à meia-noite em ponto): vai até 24:00.
            var minutoInicial = DateOnly.FromDateTime(inicio) < dia ? 0 : inicio.Hour * 60 + inicio.Minute;
            var minutoFinal = DateOnly.FromDateTime(fim) > dia ? MinutosNoDia : fim.Hour * 60 + fim.Minute;

            var quartoInicial = minutoInicial / MinutosPorQuarto;
            var quartoFinal = Math.Min(QuartosNoDia,
                Math.Max((minutoFinal + MinutosPorQuarto - 1) / MinutosPorQuarto, quartoInicial + QuartosMinimos));
            // O curto no fim do dia sobe para caber: 23:50–24:00 fica em 23:30–24:00.
            return new Trecho(evento, ordem, Math.Min(quartoInicial, quartoFinal - QuartosMinimos), quartoFinal);
        }
    }
}
```

- [ ] **Step 4: Rótulos**

`src/AtividadesJuridicas.Web/Components/Agenda/RotulosDeAgenda.cs` inteiro (os membros de hoje ficam com as mesmas assinaturas e os mesmos textos; `Intervalo`, `Sobreposicao` e `MensagemDeSalvo` passam a usar as montagens privadas que as redações novas também usam):

```csharp
using System.Globalization;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.Web.Components.Agenda;

/// <summary>
/// O que o formulário devolve depois de gravar: o id, os avisos de sobreposição e o início gravado (em UTC), para a
/// página mostrar a semana em que o evento caiu.
/// </summary>
public sealed record EventoGravado(Guid Id, IReadOnlyList<SobreposicaoDeAgenda> Sobreposicoes, DateTime InicioEm);

/// <summary>Os textos em português e o cálculo de dia que a agenda mostra.</summary>
public static class RotulosDeAgenda
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    // De segunda a domingo, a ordem da semana da agenda. Escritos aqui, e não pela cultura, para a maiúscula e o
    // "-feira" saírem como nos quadros.
    private static readonly string[] Siglas = ["Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom"];
    private static readonly string[] Nomes = ["Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado", "Domingo"];
    private static readonly string[] NomesLongos =
        ["Segunda-feira", "Terça-feira", "Quarta-feira", "Quinta-feira", "Sexta-feira", "Sábado", "Domingo"];
    private static readonly string[] Meses =
        ["Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho", "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"];

    public static string Tipo(TipoEvento tipo) => tipo switch
    {
        TipoEvento.Audiencia => "Audiência",
        TipoEvento.Reuniao => "Reunião",
        TipoEvento.Prazo => "Prazo",
        TipoEvento.Outro => "Outro",
        _ => tipo.ToString(),
    };

    public const string AtividadeSemAcesso = "Atividade vinculada (sem acesso)";
    public const string AtividadeExcluida = "Atividade vinculada (excluída)";

    /// <summary>O nome de quem não é mais encontrado (nos participantes dos cartões).</summary>
    public const string UsuarioNaoEncontrado = "Usuário não encontrado";

    /// <summary>O texto da atividade vinculada: o título, ou o motivo de não haver título para mostrar.</summary>
    public static string Atividade(string? titulo, bool excluida) =>
        titulo ?? (excluida ? AtividadeExcluida : AtividadeSemAcesso);

    /// <summary>A segunda-feira da semana do dia (a semana vai de segunda a domingo).</summary>
    public static DateOnly SegundaDe(DateOnly dia) => dia.AddDays(-PosicaoNaSemana(dia));

    /// <summary>"dd/MM/yyyy" com algarismos e calendário fixos, qualquer que seja a cultura da conexão.</summary>
    public static string Data(DateOnly dia) => dia.ToString("dd/MM/yyyy", Cultura);

    /// <summary>"segunda-feira, 12/10/2026".</summary>
    public static string Dia(DateOnly dia) => dia.ToString("dddd, dd/MM/yyyy", Cultura);

    /// <summary>
    /// O evento aparece no dia se o intervalo <c>[início, fim)</c>, em horário do escritório, toca esse dia. O dia do
    /// fim é o do último instante do evento: quem termina à meia-noite em ponto não aparece no dia seguinte.
    /// </summary>
    public static bool TocaODia(FusoDoEscritorio fuso, EventoResumo evento, DateOnly dia)
    {
        var primeiro = DateOnly.FromDateTime(fuso.ParaLocal(evento.InicioEm));
        var ultimo = DateOnly.FromDateTime(fuso.ParaLocal(evento.FimEm.AddTicks(-1)));
        return primeiro <= dia && dia <= ultimo;
    }

    /// <summary>
    /// "HH:mm – HH:mm" no horário do escritório; o lado que cai em outro dia que não o listado leva a data
    /// ("23:30 – 08/10 00:30").
    /// </summary>
    public static string Intervalo(FusoDoEscritorio fuso, EventoResumo evento, DateOnly dia) =>
        IntervaloNoDia(fuso, evento.InicioEm, evento.FimEm, dia);

    // ---- Dias, semanas e meses ----

    /// <summary>"Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom".</summary>
    public static string SiglaDoDia(DateOnly dia) => Siglas[PosicaoNaSemana(dia)];

    /// <summary>"Segunda", "Terça"… "Domingo" (a faixa de dias e o título do dia do mobile).</summary>
    public static string NomeDoDia(DateOnly dia) => Nomes[PosicaoNaSemana(dia)];

    /// <summary>"Segunda-feira"… "Sábado", "Domingo" (o subtítulo e o cabeçalho do Dia).</summary>
    public static string NomeLongoDoDia(DateOnly dia) => NomesLongos[PosicaoNaSemana(dia)];

    /// <summary>O nome acessível do dia no desktop e no tablet: "quinta-feira, 08/10/2026, hoje".</summary>
    public static string RotuloLongo(DateOnly dia, bool hoje) => hoje ? $"{Dia(dia)}, hoje" : Dia(dia);

    /// <summary>O nome acessível do dia na faixa do mobile: "Quinta, 08/10, hoje".</summary>
    public static string RotuloCurto(DateOnly dia, bool hoje)
    {
        var rotulo = $"{NomeDoDia(dia)}, {dia.ToString("dd/MM", Cultura)}";
        return hoje ? $"{rotulo}, hoje" : rotulo;
    }

    /// <summary>O título do cabeçalho do Dia no desktop: "Quinta-feira" ou "Quinta-feira · hoje".</summary>
    public static string TituloDoDiaNoDesktop(DateOnly dia, bool hoje) =>
        hoje ? $"{NomeLongoDoDia(dia)} · hoje" : NomeLongoDoDia(dia);

    /// <summary>O título do dia escolhido no mobile (Dia e Mês): "Quinta, 08/10" ou "Quinta, 08/10 · hoje".</summary>
    public static string TituloDoDiaNoMobile(DateOnly dia, bool hoje)
    {
        var titulo = $"{NomeDoDia(dia)}, {dia.ToString("dd/MM", Cultura)}";
        return hoje ? $"{titulo} · hoje" : titulo;
    }

    /// <summary>
    /// "Semana de 05/10 a 11/10/2026" a partir da segunda-feira; com a semana entre dois anos, os dois anos:
    /// "Semana de 28/12/2026 a 03/01/2027".
    /// </summary>
    public static string SubtituloDaSemana(DateOnly segunda)
    {
        var domingo = segunda.AddDays(6);
        var de = segunda.Year == domingo.Year ? segunda.ToString("dd/MM", Cultura) : Data(segunda);
        return $"Semana de {de} a {Data(domingo)}";
    }

    /// <summary>"Quinta-feira, 08/10/2026".</summary>
    public static string SubtituloDoDia(DateOnly dia) => $"{NomeLongoDoDia(dia)}, {Data(dia)}";

    /// <summary>"Outubro de 2026".</summary>
    public static string SubtituloDoMes(DateOnly dia) => $"{Meses[dia.Month - 1]} de {dia.ToString("yyyy", Cultura)}";

    /// <summary>A linha do mês da barra do mobile: "Outubro 2026".</summary>
    public static string MesDoMobile(DateOnly dia) => $"{Meses[dia.Month - 1]} {dia.ToString("yyyy", Cultura)}";

    /// <summary>"0 eventos", "1 evento", "2 eventos".</summary>
    public static string TotalDeEventos(int quantidade) => Contagem.De(quantidade, "evento", "eventos");

    // ---- Cartões ----

    /// <summary>A hora de início na coluna de hora do cartão ("09:00"), ou "…" quando o evento começou em outro dia.</summary>
    public static string HoraDeInicio(FusoDoEscritorio fuso, EventoResumo evento, DateOnly dia)
    {
        var inicio = fuso.ParaLocal(evento.InicioEm);
        return DateOnly.FromDateTime(inicio) == dia ? inicio.ToString("HH:mm", Cultura) : "…";
    }

    /// <summary>
    /// A hora do fim na coluna de hora do cartão ("10:00"), ou "…" quando o evento termina em outro dia. Terminar à
    /// meia-noite em ponto é terminar no mesmo dia ("00:00").
    /// </summary>
    public static string HoraDeFim(FusoDoEscritorio fuso, EventoResumo evento, DateOnly dia)
    {
        var diaDoFim = DateOnly.FromDateTime(fuso.ParaLocal(evento.FimEm.AddTicks(-1)));
        return diaDoFim == dia ? fuso.ParaLocal(evento.FimEm).ToString("HH:mm", Cultura) : "…";
    }

    /// <summary>
    /// Os participantes separados por vírgula, na ordem do servidor; o usuário que não é mais encontrado (o servidor
    /// manda o nome vazio) aparece como "Usuário não encontrado". Nulo sem participantes.
    /// </summary>
    public static string? Participantes(EventoResumo evento) =>
        evento.Participantes.Count == 0
            ? null
            : string.Join(", ", evento.Participantes.Select(nome => string.IsNullOrWhiteSpace(nome) ? UsuarioNaoEncontrado : nome));

    /// <summary>A linha de apoio do cartão do tablet: o local ou, sem local, o cliente. Nula sem os dois.</summary>
    public static string? LinhaDoTablet(EventoResumo evento) => Preenchido(evento.Local) ?? Preenchido(evento.ClienteNome);

    /// <summary>A primeira linha de apoio do cartão do mobile: "local · cliente", o que houver. Nula sem os dois.</summary>
    public static string? LinhaDoMobile(EventoResumo evento) => Juntar(" · ", [evento.Local, evento.ClienteNome]);

    /// <summary>A linha de apoio do cartão da régua: "local · cliente · participantes", o que houver.</summary>
    public static string? LinhaDaRegua(EventoResumo evento) =>
        Juntar(" · ", [evento.Local, evento.ClienteNome, Participantes(evento)]);

    // ---- Ficha ----

    /// <summary>"90 minutos", "1 minuto".</summary>
    public static string Duracao(int minutos) => Contagem.De(minutos, "minuto", "minutos");

    /// <summary>
    /// A linha de contexto da ficha: "Audiência · 07/10/2026, 09:00 – 10:30 · Fórum Trabalhista, sala 4". Sem local (ou
    /// com <paramref name="comLocal"/> falso, o resumo do mobile), sem a última parte. O fim em outro dia leva a data
    /// ("23:30 – 08/10 00:30").
    /// </summary>
    public static string ResumoDoEvento(FusoDoEscritorio fuso, EventoDetalhe evento, bool comLocal)
    {
        var dia = DateOnly.FromDateTime(fuso.ParaLocal(evento.InicioEm));
        var resumo = $"{Tipo(evento.Tipo)} · {Data(dia)}, {IntervaloNoDia(fuso, evento.InicioEm, evento.FimEm, dia)}";
        return comLocal && Preenchido(evento.Local) is { } local ? $"{resumo} · {local}" : resumo;
    }

    // ---- Sobreposições ----

    /// <summary>
    /// "Fulano já tem evento das 10:00 às 11:30 (07/10)"; se o intervalo atravessa a meia-noite, as duas datas aparecem:
    /// "Fulano já tem evento de 10/03 22:00 a 11/03 01:00". Quem termina à meia-noite em ponto não atravessa.
    /// </summary>
    public static string Sobreposicao(FusoDoEscritorio fuso, SobreposicaoDeAgenda s) => Frase(fuso, s, noMobile: false);

    /// <summary>A mensagem depois de gravar: o evento foi salvo; a sobreposição é só um aviso.</summary>
    public static string MensagemDeSalvo(FusoDoEscritorio fuso, IReadOnlyList<SobreposicaoDeAgenda> sobreposicoes) =>
        sobreposicoes.Count == 0 ? "Evento salvo." : $"Evento salvo. {ConflitoNoDesktop(fuso, sobreposicoes)}";

    /// <summary>
    /// O aviso de conflito do desktop, depois do "Evento salvo." em destaque: "Atenção: Rafael Teles já tem evento das
    /// 10:00 às 11:30 (07/10)." Várias pessoas, separadas por "; ". Só com ao menos uma sobreposição.
    /// </summary>
    public static string ConflitoNoDesktop(FusoDoEscritorio fuso, IReadOnlyList<SobreposicaoDeAgenda> sobreposicoes) =>
        $"Atenção: {Frases(fuso, sobreposicoes, noMobile: false)}.";

    /// <summary>O do tablet, sem "Atenção:": "Rafael Teles já tem evento das 10:00 às 11:30 (07/10)."</summary>
    public static string ConflitoNoTablet(FusoDoEscritorio fuso, IReadOnlyList<SobreposicaoDeAgenda> sobreposicoes) =>
        $"{Frases(fuso, sobreposicoes, noMobile: false)}.";

    /// <summary>
    /// O do mobile, sem "Evento salvo." e com "em dd/MM": "Rafael Teles já tem evento das 10:00 às 11:30 em 07/10."
    /// A sobreposição que atravessa a meia-noite tem a mesma forma nas três visões.
    /// </summary>
    public static string ConflitoNoMobile(FusoDoEscritorio fuso, IReadOnlyList<SobreposicaoDeAgenda> sobreposicoes) =>
        $"{Frases(fuso, sobreposicoes, noMobile: true)}.";

    private static int PosicaoNaSemana(DateOnly dia) => ((int)dia.DayOfWeek + 6) % 7;

    private static string IntervaloNoDia(FusoDoEscritorio fuso, DateTime inicioUtc, DateTime fimUtc, DateOnly dia)
    {
        var inicio = fuso.ParaLocal(inicioUtc);
        var fim = fuso.ParaLocal(fimUtc);
        var diaDoFim = DateOnly.FromDateTime(fuso.ParaLocal(fimUtc.AddTicks(-1)));

        var de = DateOnly.FromDateTime(inicio) == dia ? inicio.ToString("HH:mm", Cultura) : inicio.ToString("dd/MM HH:mm", Cultura);
        var ate = diaDoFim == dia ? fim.ToString("HH:mm", Cultura) : fim.ToString("dd/MM HH:mm", Cultura);
        return $"{de} – {ate}";
    }

    private static string Frases(FusoDoEscritorio fuso, IReadOnlyList<SobreposicaoDeAgenda> sobreposicoes, bool noMobile) =>
        string.Join("; ", sobreposicoes.Select(s => Frase(fuso, s, noMobile)));

    private static string Frase(FusoDoEscritorio fuso, SobreposicaoDeAgenda s, bool noMobile)
    {
        var nome = s.UsuarioNome ?? "Usuário";
        var inicio = fuso.ParaLocal(s.InicioEm);
        var fim = fuso.ParaLocal(s.FimEm);
        var diaDoFim = DateOnly.FromDateTime(fuso.ParaLocal(s.FimEm.AddTicks(-1)));

        if (DateOnly.FromDateTime(inicio) != diaDoFim)
            return $"{nome} já tem evento de {inicio.ToString("dd/MM HH:mm", Cultura)} a {fim.ToString("dd/MM HH:mm", Cultura)}";

        var horario = $"{nome} já tem evento das {inicio.ToString("HH:mm", Cultura)} às {fim.ToString("HH:mm", Cultura)}";
        var data = inicio.ToString("dd/MM", Cultura);
        return noMobile ? $"{horario} em {data}" : $"{horario} ({data})";
    }

    private static string? Preenchido(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto;

    private static string? Juntar(string separador, IEnumerable<string?> partes)
    {
        var preenchidas = partes.Where(parte => !string.IsNullOrWhiteSpace(parte)).ToList();
        return preenchidas.Count == 0 ? null : string.Join(separador, preenchidas);
    }
}
```

- [ ] **Step 5: Build e testes**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AgendaPuraTests|FullyQualifiedName~AgendaPorDiaTests"`
Expected: todos passando: os novos de `AgendaPuraTests` e os de `AgendaPorDiaTests` sem mudança (`Intervalo`, `Sobreposicao`, `MensagemDeSalvo` e `SegundaDe` dão os mesmos textos de antes).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AgendaWebTests"`
Expected: os seis de hoje passando (a página atual usa `RotulosDeAgenda` e não muda nesta tarefa).

- [ ] **Step 6: Commit**

```
git branch --show-current
git add src tests
git commit -F <arquivo da mensagem, UTF-8 sem BOM>
```

Mensagem:

```
feat(web): lógica pura da agenda: período no endereço, régua do dia e rótulos das três visões

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

---

### Task 3: Peças compartilhadas — parâmetros novos, recado com sobreposições e aviso de evento salvo

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Infra/Recados.cs`, `Components/Layout/Aviso.razor`, `EstadoDaLista.razor`, `CabecalhoDePagina.razor`, `CabecalhoDeDetalhe.razor`, `BarraDeVoltar.razor`, `RotuloDeCampo.razor`, `Icone.razor`
- Move: `Components/Atividades/AvatarDePessoa.cs` → `Components/Layout/AvatarDePessoa.cs` (namespace `AtividadesJuridicas.Web.Components.Layout`; os usos da 9E passam a importar dali)
- Create: `src/AtividadesJuridicas.Web/Components/Agenda/AvisoDeEventoSalvo.razor`
- Test: `tests/AtividadesJuridicas.IntegrationTests/ComponentesDoDesignSystemTests.cs`, `TelasPurasTests.cs` (Recados), `AvisoDeEventoSalvoTests.cs` (novo); o `using` do `AvatarDePessoa` em `AtividadeFichaWebTests.cs`, `AtividadesWebTests.cs`, `AtividadeFichaInterativaTests.cs`, `ComentariosDaAtividadeInterativosTests.cs`; `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (`AvisoDeEventoSalvo.razor` em `Migradas`)

**Interfaces:**
- Consumes: `RotulosDeAgenda.ConflitoNo*` (Tarefa 2).
- Produces:
  - `Recados.Deixar(string texto, TipoDeAviso tipo = TipoDeAviso.Sucesso, IReadOnlyList<SobreposicaoDeAgenda>? sobreposicoes = null)`; `record Recado(string Texto, TipoDeAviso Tipo, IReadOnlyList<SobreposicaoDeAgenda>? Sobreposicoes = null)`.
  - `Aviso.Frase` (`bool`): acrescenta `aj-aviso--frase` (sem ações).
  - `EstadoDaLista.Icone` (`string?`, nome do `Icone`) e `Dica` (`string?`): no vazio, o ícone com `aj-estado-lista__icone` antes do `strong` e a dica em `span.aj-estado-lista__dica` depois.
  - `CabecalhoDePagina.AcaoDoMobile` (`RenderFragment?`): na linha do título da barra do mobile, entre o título e o avatar; `CabecalhoDePagina.SubtituloSoNoDesktop` (`bool`): o subtítulo do cabeçalho só no desktop (num invólucro `aj-so-desktop` dentro do bloco de texto).
  - `BarraDeVoltar.Icone` (`string`, padrão `voltar`); `CabecalhoDePagina.IconeDoVoltar` (`string`, padrão `voltar`, repassado à `BarraDeVoltar`); `CabecalhoDeDetalhe.IconeDoVoltarNoMobile` (`string`, padrão `voltar`) e `SubtituloSoNoDesktop` (`bool`), repassados ao `CabecalhoDePagina`.
  - `RotuloDeCampo.Complemento` (`string?`): depois do texto, `<small>{Complemento}</small>` dentro do rótulo.
  - `Icone` com `filtrar` (o funil `M4 5h16l-6 7.5V19l-4 1.5v-8z`).
  - `AvatarDePessoa` em `Components/Layout`, sem mudança de comportamento.
  - `AvisoDeEventoSalvo` (`Components/Agenda`): `[Parameter, EditorRequired] Recado Recado`; com `Recado.Sobreposicoes` vazia ou nula, `<Aviso Tipo="Sucesso">{Recado.Texto}</Aviso>`; senão, os três avisos de atenção `Frase` em `aj-so-desktop`, `aj-so-tablet`, `aj-so-mobile`.

**Regras:**
- Os parâmetros novos têm padrão que mantém a marcação de hoje: nenhuma tela existente muda de HTML (provar com os testes de tela que já existem, sem tocá-los).
- `AvisoDeEventoSalvo` usa o `FusoDoEscritorio` injetado.

**Testes obrigatórios:** cada parâmetro com e sem valor (o HTML de hoje sem ele); `Recados` levando e devolvendo as sobreposições uma vez só; `AvisoDeEventoSalvo` sem sobreposição (um aviso de sucesso, nenhum invólucro) e com duas (as três frases exatas, cada uma no seu invólucro, o `strong` só no desktop e no tablet).

- [ ] **Step 1: Testes (devem falhar)**

Todos os arquivos tocados são UTF-8 sem BOM com fim de linha CRLF; o arquivo novo segue o mesmo.

**`tests/AtividadesJuridicas.IntegrationTests/TelasPurasTests.cs`** — acrescente `using AtividadesJuridicas.Application.Agenda;` (depois de `using AtividadesJuridicas.Application.Atividades;`) e, na classe `RecadosTests`, depois de `Recado_e_entregue_uma_vez_so_e_vale_o_ultimo`:

```csharp
    [Fact]
    public void Recado_leva_as_sobreposicoes_e_as_entrega_uma_vez_so()
    {
        SobreposicaoDeAgenda[] sobreposicoes =
        [
            new(Guid.NewGuid(), "Rafael Teles", new DateTime(2026, 10, 7, 13, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 10, 7, 14, 30, 0, DateTimeKind.Utc)),
        ];
        var recados = new Recados();

        recados.Deixar("Evento salvo.", TipoDeAviso.Sucesso, sobreposicoes);

        var recado = recados.Retirar();
        recado.Should().NotBeNull();
        recado!.Texto.Should().Be("Evento salvo.");
        recado.Tipo.Should().Be(TipoDeAviso.Sucesso);
        recado.Sobreposicoes.Should().BeSameAs(sobreposicoes);
        recados.Retirar().Should().BeNull();
    }

    [Fact]
    public void Recado_sem_sobreposicoes_continua_como_era()
    {
        var recados = new Recados();

        recados.Deixar("Evento excluído.");

        recados.Retirar().Should().Be(new Recado("Evento excluído.", TipoDeAviso.Sucesso, null));
    }
```

**`tests/AtividadesJuridicas.IntegrationTests/ComponentesDoDesignSystemTests.cs`** — no `Icone_do_design_system_tem_tracado`, uma linha de dados a mais, depois de `[InlineData("texto")]`:

```csharp
    [InlineData("filtrar")]
```

E os testes novos, ao fim da classe, antes de `Desenho_estatico_aceita_formulario_com_nome`:

```csharp
    [Fact]
    public async Task Icone_filtrar_e_o_funil()
    {
        var html = await TelaDeTeste.RenderizarSemDadosAsync<Icone>(new() { ["Nome"] = "filtrar" });

        html.Should().Contain("<path d=\"M4 5h16l-6 7.5V19l-4 1.5v-8z\"></path>");
    }

    [Fact]
    public async Task Aviso_em_frase_leva_o_modificador_e_sem_ele_fica_como_era()
    {
        RenderFragment texto = c =>
        {
            c.OpenElement(0, "strong");
            c.AddContent(1, "Evento salvo.");
            c.CloseElement();
            c.AddContent(2, " Rafael Teles já tem evento.");
        };

        var frase = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<Aviso>(
            new() { ["Tipo"] = TipoDeAviso.Atencao, ["Frase"] = true, ["ChildContent"] = texto }));
        var comum = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<Aviso>(
            new() { ["Tipo"] = TipoDeAviso.Atencao, ["ChildContent"] = texto }));

        frase.Should().MatchRegex(
            "<p class=\"aj-aviso aj-aviso--atencao aj-aviso--frase\" role=\"status\">\\s*<svg[^>]*>.*?</svg>\\s*<span><strong>Evento salvo\\.</strong> Rafael Teles já tem evento\\.</span></p>");
        comum.Should().MatchRegex(
            "<p class=\"aj-aviso aj-aviso--atencao\" role=\"status\">\\s*<svg[^>]*>.*?</svg>\\s*<span><strong>Evento salvo\\.</strong> Rafael Teles já tem evento\\.</span></p>")
            .And.NotContain("aj-aviso--frase");
    }

    [Fact]
    public async Task Estado_da_lista_vazio_com_icone_e_dica()
    {
        var comIcone = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<EstadoDaLista>(new()
        {
            ["Vazio"] = true, ["TextoDeVazio"] = "Nenhum evento neste dia", ["Icone"] = "agenda",
            ["Dica"] = "Toque em + para agendar.",
        }));
        var semIcone = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<EstadoDaLista>(
            new() { ["Vazio"] = true, ["TextoDeVazio"] = "Nenhum documento." }));

        // O ícone antes do texto, a dica depois.
        comIcone.Should().MatchRegex(
            "<div class=\"aj-estado-lista\">\\s*<svg class=\"aj-estado-lista__icone\"[^>]*>\\s*<rect x=\"3.5\" y=\"5\"[^>]*></rect>.*?</svg>\\s*<strong>Nenhum evento neste dia</strong>\\s*<span class=\"aj-estado-lista__dica\">Toque em \\+ para agendar\\.</span>\\s*</div>");
        // Sem os dois, o HTML de hoje.
        semIcone.Should().Contain("<div class=\"aj-estado-lista\"><strong>Nenhum documento.</strong></div>")
            .And.NotContain("<svg").And.NotContain("aj-estado-lista__dica");
    }

    [Fact]
    public async Task Cabecalho_com_acao_do_mobile_poe_o_botao_entre_o_titulo_e_o_avatar_da_barra()
    {
        RenderFragment filtro = c => c.AddMarkupContent(0,
            "<button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--icone aj-barra-superior__acao\" aria-label=\"Filtrar agenda\">funil</button>");

        var comAcao = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDePagina>>(
            new() { ["Titulo"] = "Agenda", ["SubtituloMobile"] = string.Empty, ["AcaoDoMobile"] = filtro }));
        var semAcao = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDePagina>>(
            new() { ["Titulo"] = "Agenda" }));

        BarraDaTela(comAcao).Should().MatchRegex(
            "(?s)<div class=\"aj-barra-superior__titulo\">\\s*<h1 class=\"aj-titulo\">Agenda</h1>\\s*</div>\\s*<button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--icone aj-barra-superior__acao\" aria-label=\"Filtrar agenda\">funil</button>\\s*<a href=\"/minha-senha\" class=\"aj-avatar\" aria-label=\"Minha conta\">");
        // O botão é só da barra do mobile: o cabeçalho do desktop e do tablet não o tem.
        BlocoDeVisao.De(comAcao, "aj-desktop-tablet").Should().Contain("<h1 class=\"aj-titulo\">Agenda</h1>")
            .And.NotContain("Filtrar agenda");
        // Sem ela, o título e o avatar seguidos, como hoje.
        BarraDaTela(semAcao).Should().MatchRegex(
            "(?s)<h1 class=\"aj-titulo\">Agenda</h1>\\s*</div>\\s*<a href=\"/minha-senha\" class=\"aj-avatar\" aria-label=\"Minha conta\">")
            .And.NotContain("aj-barra-superior__acao");
    }

    [Fact]
    public async Task Cabecalho_com_subtitulo_so_no_desktop_o_poe_num_involucro_e_tira_da_barra()
    {
        const string frase = "Preencha os dados e escolha quem participa.";

        var soNoDesktop = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDePagina>>(
            new() { ["Titulo"] = "Novo evento", ["Subtitulo"] = frase, ["SubtituloSoNoDesktop"] = true }));
        var emTodas = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDePagina>>(
            new() { ["Titulo"] = "Novo evento", ["Subtitulo"] = frase }));

        BlocoDeVisao.De(soNoDesktop, "aj-desktop-tablet").Should().MatchRegex(
            $"(?s)<h1 class=\"aj-titulo\">Novo evento</h1>\\s*<div class=\"aj-so-desktop\">\\s*<p class=\"aj-subtitulo\">{Regex.Escape(frase)}</p>\\s*</div>");
        BarraDaTela(soNoDesktop).Should().Contain("<h1 class=\"aj-titulo\">Novo evento</h1>").And.NotContain("aj-subtitulo");
        // Sem o parâmetro, o subtítulo direto no bloco de texto e repetido na barra, como hoje.
        emTodas.Should().MatchRegex($"<h1 class=\"aj-titulo\">Novo evento</h1>\\s*<p class=\"aj-subtitulo\">{Regex.Escape(frase)}</p>")
            .And.NotContain("aj-so-desktop");
        BarraDaTela(emTodas).Should().Contain($"<span class=\"aj-subtitulo\">{frase}</span>");
    }

    [Fact]
    public async Task Barra_de_voltar_aceita_outro_icone_e_sem_ele_fica_com_o_voltar()
    {
        var fechar = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<BarraDeVoltar>>(new()
        {
            ["Titulo"] = "Novo evento", ["VoltarPara"] = "/agenda", ["Rotulo"] = "Fechar sem salvar", ["Icone"] = "fechar",
        }));
        var voltar = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<BarraDeVoltar>>(new()
        {
            ["Titulo"] = "Novo evento", ["VoltarPara"] = "/agenda", ["Rotulo"] = "Voltar para Agenda",
        }));

        fechar.Should().MatchRegex(
            "<a href=\"/agenda\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Fechar sem salvar\">\\s*<svg[^>]*>\\s*<path d=\"M6 6l12 12\"></path><path d=\"M18 6L6 18\"></path>\\s*</svg>\\s*</a>")
            .And.NotContain("M15 6l-6 6 6 6");
        voltar.Should().MatchRegex(
            "<a href=\"/agenda\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Voltar para Agenda\">\\s*<svg[^>]*>\\s*<path d=\"M15 6l-6 6 6 6\"></path>\\s*</svg>\\s*</a>");
    }

    [Fact]
    public async Task Cabecalho_de_detalhe_repassa_o_icone_do_voltar_do_mobile_e_o_subtitulo_so_no_desktop()
    {
        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDeDetalhe>>(new()
        {
            ["Titulo"] = "Novo evento", ["Subtitulo"] = "Preencha os dados e escolha quem participa.", ["VoltarPara"] = "/agenda",
            ["Origem"] = "Agenda", ["RotuloDoVoltarNoMobile"] = "Fechar sem salvar", ["IconeDoVoltarNoMobile"] = "fechar",
            ["SubtituloSoNoDesktop"] = true,
        }));

        // A barra do mobile com o X; o voltar do desktop e do tablet continua com a seta.
        html.Should().MatchRegex(
            "<header class=\"aj-barra-superior aj-barra-superior--voltar\">\\s*<a href=\"/agenda\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Fechar sem salvar\">\\s*<svg[^>]*>\\s*<path d=\"M6 6l12 12\"></path>");
        var desktopETablet = BlocoDeVisao.De(html, "aj-desktop-tablet");
        desktopETablet.Should().MatchRegex(
            "(?s)<a href=\"/agenda\" class=\"aj-voltar\">\\s*<svg[^>]*>\\s*<path d=\"M15 6l-6 6 6 6\"></path>\\s*</svg>\\s*Agenda</a>")
            .And.MatchRegex(
            "(?s)<h1 class=\"aj-titulo\">Novo evento</h1>\\s*<div class=\"aj-so-desktop\">\\s*<p class=\"aj-subtitulo\">Preencha os dados e escolha quem participa.</p>\\s*</div>");
    }

    [Fact]
    public async Task Cabecalho_de_detalhe_sem_os_parametros_novos_fica_como_era()
    {
        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDeDetalhe>>(new()
        {
            ["Titulo"] = "Editar evento", ["Subtitulo"] = "Reunião", ["VoltarPara"] = "/agenda", ["Origem"] = "Agenda",
        }));

        html.Should().MatchRegex(
            "<header class=\"aj-barra-superior aj-barra-superior--voltar\">\\s*<a href=\"/agenda\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Voltar para Agenda\">\\s*<svg[^>]*>\\s*<path d=\"M15 6l-6 6 6 6\"></path>")
            .And.MatchRegex("<h1 class=\"aj-titulo\">Editar evento</h1>\\s*<p class=\"aj-subtitulo\">Reunião</p>")
            .And.NotContain("aj-so-desktop").And.NotContain("M6 6l12 12");
    }

    [Fact]
    public async Task Rotulo_de_campo_com_complemento_traz_o_small_depois_do_texto()
    {
        RenderFragment campo = c => c.AddMarkupContent(0, "<textarea class=\"aj-campo\"></textarea>");
        System.Linq.Expressions.Expression<Func<string>> para = () => "x";

        var comComplemento = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<RotuloDeCampo<string>>(
            new() { ["Texto"] = "Descrição", ["Para"] = para, ["Complemento"] = "(opcional)", ["ChildContent"] = campo }));
        var semComplemento = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<RotuloDeCampo<string>>(
            new() { ["Texto"] = "Descrição", ["Para"] = para, ["ChildContent"] = campo }));

        comComplemento.Should().MatchRegex(
            "<label class=\"aj-rotulo\">\\s*Descrição <small>\\(opcional\\)</small>\\s*<textarea class=\"aj-campo\"></textarea>\\s*</label>");
        semComplemento.Should().MatchRegex("<label class=\"aj-rotulo\">\\s*Descrição\\s*<textarea class=\"aj-campo\"></textarea>\\s*</label>")
            .And.NotContain("<small>");
    }
```

**`tests/AtividadesJuridicas.IntegrationTests/AvisoDeEventoSalvoTests.cs`** (novo):

```csharp
using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Web.Components.Agenda;
using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O aviso que a agenda e a ficha do evento mostram no topo depois de salvar ou excluir: o recado simples, ou, com
/// conflito de agenda, uma frase de atenção por visão (as redações dos quadros AgendaA, AgendaTablet e AgendaMobile).
/// </summary>
public class AvisoDeEventoSalvoTests
{
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");

    private SobreposicaoDeAgenda Sobreposicao(string nome, DateTime inicioLocal, DateTime fimLocal) =>
        new(Guid.NewGuid(), nome, _fuso.ParaUtc(inicioLocal), _fuso.ParaUtc(fimLocal));

    // O desenho com despachante registra o fuso do escritório, que o componente injeta.
    private static async Task<string> DesenharAsync(Recado recado) =>
        WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<AvisoDeEventoSalvo>(
            new DespachanteCombinado(), new() { ["Recado"] = recado }));

    [Fact]
    public async Task Sem_sobreposicao_e_um_aviso_de_sucesso_so_sem_involucro()
    {
        var salvo = await DesenharAsync(new Recado("Evento salvo.", TipoDeAviso.Sucesso));
        var listaVazia = await DesenharAsync(new Recado("Evento salvo.", TipoDeAviso.Sucesso, []));
        var excluido = await DesenharAsync(new Recado("Evento excluído.", TipoDeAviso.Sucesso));

        salvo.Should().MatchRegex(
            "<p class=\"aj-aviso aj-aviso--sucesso\" role=\"status\">\\s*<svg[^>]*>.*?</svg>\\s*<span>Evento salvo\\.</span></p>")
            .And.NotContain("aj-so-").And.NotContain("aj-aviso--atencao").And.NotContain("<strong>");
        Regex.Matches(salvo, "class=\"aj-aviso ").Should().ContainSingle();
        listaVazia.Should().Be(salvo);
        excluido.Should().MatchRegex(
            "<p class=\"aj-aviso aj-aviso--sucesso\" role=\"status\">\\s*<svg[^>]*>.*?</svg>\\s*<span>Evento excluído\\.</span></p>")
            .And.NotContain("aj-so-");
    }

    [Fact]
    public async Task Com_sobreposicoes_traz_uma_frase_de_atencao_por_visao()
    {
        var html = await DesenharAsync(new Recado("Evento salvo.", TipoDeAviso.Sucesso,
        [
            Sobreposicao("Rafael Teles", new DateTime(2026, 10, 7, 10, 0, 0), new DateTime(2026, 10, 7, 11, 30, 0)),
            Sobreposicao("Marina Couto", new DateTime(2026, 10, 7, 11, 0, 0), new DateTime(2026, 10, 7, 12, 0, 0)),
        ]));

        const string Abertura = "^\\s*<p class=\"aj-aviso aj-aviso--atencao aj-aviso--frase\" role=\"status\">\\s*<svg[^>]*>.*?</svg>\\s*<span>";
        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(Abertura +
            "<strong>Evento salvo\\.</strong> Atenção: Rafael Teles já tem evento das 10:00 às 11:30 \\(07/10\\); Marina Couto já tem evento das 11:00 às 12:00 \\(07/10\\)\\.</span></p>\\s*$");
        BlocoDeVisao.De(html, "aj-so-tablet").Should().MatchRegex(Abertura +
            "<strong>Evento salvo\\.</strong> Rafael Teles já tem evento das 10:00 às 11:30 \\(07/10\\); Marina Couto já tem evento das 11:00 às 12:00 \\(07/10\\)\\.</span></p>\\s*$")
            .And.NotContain("Atenção");
        BlocoDeVisao.De(html, "aj-so-mobile").Should().MatchRegex(Abertura +
            "Rafael Teles já tem evento das 10:00 às 11:30 em 07/10; Marina Couto já tem evento das 11:00 às 12:00 em 07/10\\.</span></p>\\s*$")
            .And.NotContain("<strong>");
        // Nenhum aviso de sucesso a mais: cada visão tem só a sua frase.
        html.Should().NotContain("aj-aviso--sucesso");
        Regex.Matches(html, "aj-aviso--frase").Should().HaveCount(3);
    }
}
```

**`tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`** — o componente novo entra na trava: acrescente a `Migradas`, como primeiro item (ordem alfabética, antes de `Components/Atividades/…`):

```csharp
        "Components/Agenda/AvisoDeEventoSalvo.razor",
```

**Os testes que usam `AvatarDePessoa`** passam a importá-lo de `Components/Layout` (a mudança de pasta está no passo 2):

- `tests/AtividadesJuridicas.IntegrationTests/AtividadeFichaWebTests.cs` (uso na linha 94): troque `using AtividadesJuridicas.Web.Components.Atividades;` (linha 9; o arquivo não usa mais nada dessa pasta) por `using AtividadesJuridicas.Web.Components.Layout;`.
- `tests/AtividadesJuridicas.IntegrationTests/AtividadesWebTests.cs` (uso na linha 117): troque `using AtividadesJuridicas.Web.Components.Atividades;` (linha 6; idem) por `using AtividadesJuridicas.Web.Components.Layout;`.
- `tests/AtividadesJuridicas.IntegrationTests/AtividadeFichaInterativaTests.cs` (uso na linha 140): acrescente `using AtividadesJuridicas.Web.Components.Layout;` entre `using AtividadesJuridicas.Web.Components.Atividades;` e `using AtividadesJuridicas.Web.Components.Pages;` (o arquivo continua usando `ComentariosDaAtividade` e `DocumentosDaAtividade`).
- `tests/AtividadesJuridicas.IntegrationTests/ComentariosDaAtividadeInterativosTests.cs` (uso na linha 76): acrescente `using AtividadesJuridicas.Web.Components.Layout;` depois de `using AtividadesJuridicas.Web.Components.Atividades;` (continua usando `ComentariosDaAtividade`).
- `tests/AtividadesJuridicas.IntegrationTests/TelasPurasTests.cs` (`AvatarDePessoaTests`, linhas 547–580): já importa `AtividadesJuridicas.Web.Components.Layout` (linha 10); nada muda.

Run: `dotnet build -m:1`
Expected: não compila (`Recados.Deixar` não aceita o terceiro argumento; `Recado` não tem `Sobreposicoes`; `AvisoDeEventoSalvo` não existe; `AvatarDePessoa` não está em `AtividadesJuridicas.Web.Components.Layout`). É o RED. Os testes de HTML (`Frase`, `Icone`, `Dica`, `AcaoDoMobile`, `SubtituloSoNoDesktop`, `IconeDoVoltarNoMobile`, `Complemento` e o ícone `filtrar`) compilam, mas falhariam: o desenho estático recusa parâmetro que o componente não declara, e o ícone desconhecido sai vazio.

- [ ] **Step 2: `AvatarDePessoa` vai para `Components/Layout`**

Pela ferramenta PowerShell:

```
git mv src/AtividadesJuridicas.Web/Components/Atividades/AvatarDePessoa.cs src/AtividadesJuridicas.Web/Components/Layout/AvatarDePessoa.cs
```

No arquivo movido, só o namespace e o resumo mudam (a sigla e a cor ficam iguais):

```csharp
namespace AtividadesJuridicas.Web.Components.Layout;

/// <summary>
/// O avatar de uma pessoa nas telas de atividade e da agenda (cartões, responsáveis, comentários, participantes). A
/// sigla e a cor saem do nome, porque os resumos só trazem nomes. A cor é estável entre execuções: vem da soma dos
/// códigos dos caracteres, e não do GetHashCode de string, que muda a cada execução.
/// </summary>
public static class AvatarDePessoa
```

(o corpo da classe, de `private static readonly string[] Cores` até o fim, não muda).

Todos os usos, conferidos por busca em `src` e `tests` (fora `docs`):

| Arquivo | Linha | O que muda |
|---|---|---|
| `src/AtividadesJuridicas.Web/Components/Pages/Atividades.razor` | 124 | nada: `Components/_Imports.razor` já tem `@using AtividadesJuridicas.Web.Components.Layout` (linha 11) |
| `src/AtividadesJuridicas.Web/Components/Pages/AtividadeFicha.razor` | 460 | nada (idem) |
| `src/AtividadesJuridicas.Web/Components/Atividades/ComentariosDaAtividade.razor` | 33 e 114 | nada (idem) |
| `tests/AtividadesJuridicas.IntegrationTests/AtividadeFichaWebTests.cs` | 94 | o `using` (passo 1) |
| `tests/AtividadesJuridicas.IntegrationTests/AtividadesWebTests.cs` | 117 | o `using` (passo 1) |
| `tests/AtividadesJuridicas.IntegrationTests/AtividadeFichaInterativaTests.cs` | 140 | o `using` (passo 1) |
| `tests/AtividadesJuridicas.IntegrationTests/ComentariosDaAtividadeInterativosTests.cs` | 76 | o `using` (passo 1) |
| `tests/AtividadesJuridicas.IntegrationTests/TelasPurasTests.cs` | 547–580 | nada (já importa `Layout`) |

Se o compilador apontar ambiguidade de nome num dos testes por causa do `using` novo, qualifique só o nome ambíguo (`AtividadesJuridicas.Web.Components.Layout.X`) e registre no relatório.

- [ ] **Step 3: Recado com sobreposições**

`src/AtividadesJuridicas.Web/Infra/Recados.cs` inteiro:

```csharp
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Web.Components.Layout;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>
/// Aviso que atravessa uma navegação: a tela de origem deixa ("Usuário salvo."), a de destino mostra uma vez.
/// Vive no escopo do circuito, então só vale para a navegação dentro da mesma conexão.
/// </summary>
public sealed class Recados
{
    private Recado? _pendente;

    /// <summary>
    /// Deixa o recado para a próxima tela. <paramref name="sobreposicoes"/>: os conflitos de agenda do evento salvo, que
    /// a agenda e a ficha do evento mostram numa redação por visão (<c>AvisoDeEventoSalvo</c>).
    /// </summary>
    public void Deixar(
        string texto, TipoDeAviso tipo = TipoDeAviso.Sucesso, IReadOnlyList<SobreposicaoDeAgenda>? sobreposicoes = null) =>
        _pendente = new Recado(texto, tipo, sobreposicoes);

    /// <summary>O recado deixado, uma vez só; nulo quando não há.</summary>
    public Recado? Retirar()
    {
        var recado = _pendente;
        _pendente = null;
        return recado;
    }
}

public sealed record Recado(string Texto, TipoDeAviso Tipo, IReadOnlyList<SobreposicaoDeAgenda>? Sobreposicoes = null);
```

Quem deixa e quem lê recado hoje (`Recados.Deixar("…")`, `Recados.Deixar(texto)`, `_recado.Texto`, `_recado.Tipo`) compila sem mudança.

- [ ] **Step 4: As peças de layout**

`Icone.razor`: uma entrada a mais no dicionário `Tracados`, logo depois de `["filtros"]`:

```csharp
        ["filtrar"] = new("<path d=\"M4 5h16l-6 7.5V19l-4 1.5v-8z\"></path>"),
```

`Aviso.razor`: o parágrafo do aviso sem ações,

```razor
    <p class="@Classe" role="@Papel"><Icone Nome="@NomeDoIcone" /><span>@ChildContent</span></p>
```

passa a:

```razor
    <p class="@(Frase ? $"{Classe} aj-aviso--frase" : Classe)" role="@Papel"><Icone Nome="@NomeDoIcone" /><span>@ChildContent</span></p>
```

e, no `@code`, depois de `Rotulo`:

```csharp
    /// <summary>
    /// O aviso em frase (o retorno de salvar evento): o ícone na altura da primeira linha e o texto em peso normal, com
    /// o começo em <c>strong</c>. Só no aviso sem ações.
    /// </summary>
    [Parameter] public bool Frase { get; set; }
```

`EstadoDaLista.razor` inteiro:

```razor
@* O que aparece no lugar da lista: falha de consulta, carregando ou nada a mostrar. O vazio pode trazer um ícone
   antes do texto e uma dica depois (o dia sem eventos da agenda no mobile). *@
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
    <div class="aj-estado-lista">
        @if (Icone is not null)
        {
            <Icone Nome="@Icone" Classe="aj-estado-lista__icone" />
        }
        <strong>@TextoDeVazio</strong>
        @if (Dica is not null)
        {
            <span class="aj-estado-lista__dica">@Dica</span>
        }
    </div>
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

    /// <summary>O nome do ícone do vazio (o <c>Icone</c> do design system), antes do texto.</summary>
    [Parameter] public string? Icone { get; set; }

    /// <summary>A segunda linha do vazio, menor ("Toque em + para agendar.").</summary>
    [Parameter] public string? Dica { get; set; }

    [Parameter] public EventCallback TentarDeNovo { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

(O espaço em branco antes e depois de um bloco `@if` não chega ao HTML: sem ícone nem dica, o vazio continua `<div class="aj-estado-lista"><strong>…</strong></div>`, o que o teste novo prova.)

`BarraDeVoltar.razor`: o botão de voltar,

```razor
        <a href="@VoltarPara" class="aj-botao aj-botao--fantasma aj-botao--icone" aria-label="@Rotulo"><Icone Nome="voltar" /></a>
```

passa a:

```razor
        <a href="@VoltarPara" class="aj-botao aj-botao--fantasma aj-botao--icone" aria-label="@Rotulo"><Icone Nome="@Icone" /></a>
```

e, no `@code`, depois de `Rotulo`:

```csharp
    /// <summary>O ícone do voltar: a seta (padrão) ou o X do formulário do mobile ("fechar").</summary>
    [Parameter] public string Icone { get; set; } = "voltar";
```

`CabecalhoDePagina.razor`, quatro mudanças:

1. A barra de voltar repassa o ícone:

```razor
    <BarraDeVoltar Titulo="@Titulo" VoltarPara="@VoltarPara" Rotulo="@(RotuloDoVoltar ?? "Voltar")" Icone="@IconeDoVoltar" />
```

2. Na linha do título da barra do mobile, a ação entre o título e o avatar. A linha

```razor
                <a href="/minha-senha" class="aj-avatar" aria-label="Minha conta">@Contexto?.Iniciais</a>
```

passa a (a expressão colada no avatar, para o espaço em branco entre o bloco do título e o avatar ficar como é hoje quando não há ação):

```razor
                @(AcaoDoMobile)<a href="/minha-senha" class="aj-avatar" aria-label="Minha conta">@Contexto?.Iniciais</a>
```

3. O subtítulo do desktop e do tablet,

```razor
                @if (Subtitulo is not null)
                {
                    <p class="aj-subtitulo">@Subtitulo@ParteDoAlerta</p>
                }
```

passa a:

```razor
                @if (Subtitulo is not null && SubtituloSoNoDesktop)
                {
                    <div class="aj-so-desktop"><p class="aj-subtitulo">@Subtitulo@ParteDoAlerta</p></div>
                }
                else if (Subtitulo is not null)
                {
                    <p class="aj-subtitulo">@Subtitulo@ParteDoAlerta</p>
                }
```

4. No `@code`, depois de `RotuloDoVoltar`:

```csharp
    /// <summary>O ícone do voltar da barra do mobile: a seta (padrão) ou o X ("fechar") do formulário do evento.</summary>
    [Parameter] public string IconeDoVoltar { get; set; } = "voltar";
```

depois de `Folgado`:

```csharp
    /// <summary>
    /// O subtítulo só no desktop (o formulário do evento): no tablet ele some, e a barra do mobile fica sem subtítulo
    /// (a menos que <see cref="SubtituloMobile"/> diga outro).
    /// </summary>
    [Parameter] public bool SubtituloSoNoDesktop { get; set; }
```

depois de `LinhaDoMobile`:

```csharp
    /// <summary>
    /// Um botão na linha do título da barra do mobile, entre o título e o avatar (o "Filtrar agenda"). Só o mobile: o
    /// cabeçalho do desktop e do tablet não o mostra.
    /// </summary>
    [Parameter] public RenderFragment? AcaoDoMobile { get; set; }
```

e `SubtituloDaBarra` passa a:

```csharp
    private string? SubtituloDaBarra =>
        SubtituloMobile is null
            ? (SubtituloSoNoDesktop ? null : Subtitulo)
            : SubtituloMobile.Length == 0 ? null : SubtituloMobile;
```

`CabecalhoDeDetalhe.razor` inteiro:

```razor
@* Cabeçalho de ficha e de formulário: no desktop e no tablet, o voltar acima do cabeçalho; no mobile, a barra de
   voltar, que esconde a barra inferior. Origem é o nome da tela de onde se veio ("Usuários"). *@
<div class="aj-desktop-tablet">
    <div><a href="@VoltarPara" class="aj-voltar"><Icone Nome="voltar" />@Origem</a></div>
</div>
<CabecalhoDePagina Titulo="@Titulo" Subtitulo="@Subtitulo" Folgado="@(Subtitulo is not null)" SubtituloSoNoDesktop="@SubtituloSoNoDesktop"
                   VoltarPara="@VoltarPara" RotuloDoVoltar="@(RotuloDoVoltarNoMobile ?? $"Voltar para {Origem}")"
                   IconeDoVoltar="@IconeDoVoltarNoMobile" />

@code {
    [Parameter, EditorRequired] public string Titulo { get; set; } = string.Empty;

    /// <summary>A linha sob o título, no desktop e no tablet. A barra de voltar do mobile não tem subtítulo.</summary>
    [Parameter] public string? Subtitulo { get; set; }

    [Parameter, EditorRequired] public string VoltarPara { get; set; } = string.Empty;

    /// <summary>O nome da tela de destino do voltar: o texto do link e o rótulo do botão do mobile.</summary>
    [Parameter, EditorRequired] public string Origem { get; set; } = string.Empty;

    /// <summary>
    /// O rótulo do voltar da barra do mobile, quando "Voltar para {Origem}" não serve (a origem é o título longo de
    /// uma atividade: "Voltar para a atividade"). Nulo, vale "Voltar para {Origem}".
    /// </summary>
    [Parameter] public string? RotuloDoVoltarNoMobile { get; set; }

    /// <summary>O ícone do voltar da barra do mobile: a seta (padrão) ou o X ("fechar") do formulário do evento.</summary>
    [Parameter] public string IconeDoVoltarNoMobile { get; set; } = "voltar";

    /// <summary>O subtítulo só no desktop (o formulário do evento); no tablet ele some.</summary>
    [Parameter] public bool SubtituloSoNoDesktop { get; set; }
}
```

`RotuloDeCampo.razor`: a linha do texto,

```razor
    @Texto
```

passa a (o fragmento escreve o espaço e o `small`, como a `ParteDoAlerta` do cabeçalho: escrito na marcação, o espaço da ponta poderia ser aparado):

```razor
    @Texto@ParteDoComplemento
```

e, no `@code`, depois de `Classe`:

```csharp
    /// <summary>O complemento do rótulo, mais leve, depois do texto: "(opcional)".</summary>
    [Parameter] public string? Complemento { get; set; }

    // Sem complemento, nada é emitido e o rótulo fica como era.
    private RenderFragment ParteDoComplemento => construtor =>
    {
        if (Complemento is null) return;

        construtor.AddContent(0, " ");
        construtor.OpenElement(1, "small");
        construtor.AddContent(2, Complemento);
        construtor.CloseElement();
    };
```

(Se o Razor não aceitar `@Texto@ParteDoComplemento` colados, escreva `@(Texto)@(ParteDoComplemento)`; o HTML do teste é o mesmo.)

Notas para quem implementa:

- `EstadoDaLista` e `BarraDeVoltar` passam a ter um parâmetro chamado `Icone` e continuam usando o componente `<Icone>`. O Razor emite o tipo do componente pelo nome completo (`global::…Layout.Icone`), então o nome do parâmetro não atrapalha. Se o compilador reclamar mesmo assim, escreva a marca como `<AtividadesJuridicas.Web.Components.Layout.Icone … />` e registre o erro no relatório; o nome do parâmetro é contrato (as Tarefas 7 e 8 usam `Icone=`).
- O `CabecalhoDeFicha` usa a `BarraDeVoltar` sem `Icone` e continua com a seta.

- [ ] **Step 5: Aviso de evento salvo**

`src/AtividadesJuridicas.Web/Components/Agenda/AvisoDeEventoSalvo.razor`:

```razor
@inject FusoDoEscritorio Fuso

@* O aviso no topo da agenda e da ficha do evento depois de salvar ou excluir. Sem conflito de agenda, o recado como
   veio ("Evento salvo.", "Evento excluído."). Com conflito, um aviso de atenção em frase por visão, como os quadros
   da agenda: o desktop com "Evento salvo." em destaque e "Atenção:", o tablet sem "Atenção:", o mobile só com o
   conflito e "em dd/MM". O texto depois do destaque é uma expressão só, com o espaço na frente. *@
@if (Sobreposicoes.Count == 0)
{
    <Aviso Tipo="Recado.Tipo">@Recado.Texto</Aviso>
}
else
{
    <div class="aj-so-desktop">
        <Aviso Tipo="TipoDeAviso.Atencao" Frase="true"><strong>Evento salvo.</strong>@($" {RotulosDeAgenda.ConflitoNoDesktop(Fuso, Sobreposicoes)}")</Aviso>
    </div>
    <div class="aj-so-tablet">
        <Aviso Tipo="TipoDeAviso.Atencao" Frase="true"><strong>Evento salvo.</strong>@($" {RotulosDeAgenda.ConflitoNoTablet(Fuso, Sobreposicoes)}")</Aviso>
    </div>
    <div class="aj-so-mobile">
        <Aviso Tipo="TipoDeAviso.Atencao" Frase="true">@RotulosDeAgenda.ConflitoNoMobile(Fuso, Sobreposicoes)</Aviso>
    </div>
}

@code {
    /// <summary>O recado deixado pelo formulário ou pela ficha do evento (<c>Recados.Retirar()</c>).</summary>
    [Parameter, EditorRequired] public Recado Recado { get; set; } = default!;

    private IReadOnlyList<SobreposicaoDeAgenda> Sobreposicoes => Recado.Sobreposicoes ?? [];
}
```

- [ ] **Step 6: Build e testes**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Expected: todos passando, com `AvisoDeEventoSalvo.razor` na trava.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ComponentesDoDesignSystemTests|FullyQualifiedName~RecadosTests|FullyQualifiedName~AvatarDePessoaTests|FullyQualifiedName~AvisoDeEventoSalvoTests|FullyQualifiedName~AgendaPuraTests|FullyQualifiedName~WebTests|FullyQualifiedName~Interativ"`
Expected: todos passando. Os testes de tela que já existem (`*WebTests`, `*Interativ*Tests` e os de `ComponentesDoDesignSystemTests` de antes) passam **sem nenhuma mudança neles**: é a prova de que nenhuma tela muda de HTML (nenhuma usa os parâmetros novos ainda; os padrões emitem a marcação de hoje; `AvatarDePessoa` só mudou de pasta).

- [ ] **Step 7: Commit**

```
git branch --show-current
git add src tests
git commit -F <arquivo da mensagem, UTF-8 sem BOM>
```

Mensagem:

```
feat(web): peças da agenda: recado com sobreposições, aviso em frase, vazio com ícone e dica, ação da barra do mobile, voltar com ícone, complemento do rótulo, ícone filtrar e aviso de evento salvo

O AvatarDePessoa sai de Components/Atividades para Components/Layout: a agenda é o segundo assunto a usá-lo.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

---

### Task 4: Ficha do evento em tela própria

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Pages/EventoFicha.razor`
- Test: `tests/AtividadesJuridicas.IntegrationTests/EventoFichaWebTests.cs`, `EventoFichaInterativaTests.cs`, `PortalDoClienteWebTests.cs` (a rota nova no theory do cliente), `AutenticacaoWebTests.cs` (a rota nova no theory do anônimo)

**Interfaces:**
- Consumes: `PeriodoDaAgenda.ConsultaDeOrigem(string?, string?)` (Tarefa 2), `RotulosDeAgenda.ResumoDoEvento`, `Duracao`, `Atividade`, `Tipo` (Tarefa 2 e de hoje), `AvisoDeEventoSalvo`, `AvatarDePessoa` (Tarefa 3), `CabecalhoDeFicha`, `CabecalhoDeDetalhe`, `Aviso`, `EstadoDaLista`, `Recados`.
- Produces: rota `@page "/agenda/{Id:guid}"`; lê `[SupplyParameterFromQuery] visao` e `data`; link `Editar` para `/agenda/{id}/editar{consulta}`; depois de excluir, `Recados.Deixar("Evento excluído.")` e `/agenda{consulta}`.

**Regras:**
- Carga por `ObterMeuAcesso` e `ObterEvento`; recarrega quando o `Id` muda (`_idCarregado`, 9D). Estados: carregando, não encontrado (`CabecalhoDeDetalhe` com `Titulo="Evento"`, `Origem="Agenda"` e o aviso `Evento não encontrado.`), falha (`EstadoDaLista` com `Não foi possível consultar o evento.` e `Tentar de novo`), acesso encerrado.
- O recado do formulário (`Recados.Retirar()` na primeira carga) aparece no topo pelo `AvisoDeEventoSalvo`.
- Confirmação de exclusão no padrão da `AtividadeFicha` (`Aviso` com `Rotulo`, `@key`, versão guardada ao abrir); `Excluir` e `Confirmar exclusão` com guarda de clique duplo; recusa não de acesso fecha a confirmação, recarrega e mostra a mensagem do servidor; `AcessoNegado` encerra.
- `Descrição` com `aj-texto-livre` (as quebras do texto ficam).

**Testes obrigatórios:** a ficha da Diretoria com tudo (as três visões: subtítulo, resumo do mobile, detalhes, participantes com `(você)` primeiro e o avatar dourado, ações no desktop e na barra); a da Produção que participa (sem ações nem barra) e a da Produção que criou (com as duas) (Review Focus 4); evento sem descrição, sem local e sem vínculos; o não encontrado e o que a Produção não vê (o mesmo texto); o recado com sobreposição; excluir com confirmação, um envio só no clique duplo, a navegação com a origem e o recado (Review Focus 5); a recusa; o cliente do portal recebendo "Página não encontrada" em `/agenda/{id}`.

- [ ] **Step 1: Testes HTTP da ficha (devem falhar)**

Antes de escrever qualquer coisa, releia `Evento.dc.html`, `EventoTablet.dc.html` e `EventoMobile.dc.html` na cópia local dos quadros (`…\scratchpad\quadros-9f\project\`) e as seções 6 a 8 de `…\scratchpad\plano-9f\quadros-evento.md`. Se um quadro diferir do que estes passos descrevem, pare e relate.

Crie `tests/AtividadesJuridicas.IntegrationTests/EventoFichaWebTests.cs`. Os dados nascem pelo despachante real (`AplicacaoDeTeste.ExecutarAsync`), como a Diretoria (`_admin`, "Diana Diretora") e como o Pedro (Produção):

- `_audiencia`: o evento do quadro, criado pela Diretoria — "Audiência de instrução — Litoral", Audiência, 07/10/2026 das 09:00 às 10:30 (90 minutos), em "Fórum Trabalhista, sala 4", com cliente e processo, sem atividade, com uma descrição de duas linhas; participam a Diretoria (quem cria sempre entra), a Beatriz Lins e o Pedro. O servidor devolve os participantes por nome: Beatriz, Diana, Pedro.
- `_daProducao`: criado pelo Pedro — "Reunião de alinhamento", Reunião, 09/10/2026 das 14:00 às 15:00, sem descrição, sem local e sem vínculos; só o Pedro participa.
- `_reservado`: criado pela Diretoria, só ela participa; o Pedro não o vê.

As horas: `America/Sao_Paulo` não tem horário de verão desde 2019; 09:00 em 07/10/2026 é 12:00 UTC.

```csharp
using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using AtividadesJuridicas.Web.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class EventoFichaWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _pedro, _beatriz, _audiencia, _daProducao, _reservado;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();
        var producao = await _app.EmEscopoAsync(null, async sp =>
            (await sp.GetRequiredService<AppDbContext>().Perfis.AsNoTracking()
                .FirstAsync(p => p.Tipo == TipoPerfil.Producao && p.Ativo)).Id);
        var beatriz = await _app.ExecutarAsync(_admin, new CriarUsuario("Beatriz Lins", "beatriz@x.com", "senha-da-beatriz", producao, null));
        beatriz.Sucesso.Should().BeTrue(beatriz.Mensagem);
        _beatriz = beatriz.Valor;

        var area = await _app.ExecutarAsync(_admin, new SalvarAreaAtuacao(null, "Trabalhista"));
        area.Sucesso.Should().BeTrue(area.Mensagem);
        var litoral = await _app.ExecutarAsync(_admin, new SalvarClientePessoaJuridica(
            null, new DadosDoClienteEntrada("Transportes Litoral Ltda.", null, null, new EnderecoEntrada(), null),
            "11.222.333/0001-81", null, null, null, null));
        litoral.Sucesso.Should().BeTrue(litoral.Mensagem);
        var processo = await _app.ExecutarAsync(_admin, new SalvarProcesso(
            null, new DadosDoProcessoEntrada(null, "Litoral — reclamação trabalhista", area.Valor, null, null, null, null, null, null),
            [new ClienteDoProcessoEntrada(litoral.Valor, PoloProcessual.Passivo)]));
        processo.Sucesso.Should().BeTrue(processo.Mensagem);

        var audiencia = await _app.ExecutarAsync(_admin, new SalvarEvento(
            null, "Audiência de instrução — Litoral", "Audiência de instrução.\nLevar a proposta de acordo em seis parcelas.",
            "Fórum Trabalhista, sala 4", TipoEvento.Audiencia, new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc), 90,
            litoral.Valor, processo.Valor, null, [_beatriz, _pedro]));
        audiencia.Sucesso.Should().BeTrue(audiencia.Mensagem);
        _audiencia = audiencia.Valor.Id;

        var daProducao = await _app.ExecutarAsync(_pedro, new SalvarEvento(
            null, "Reunião de alinhamento", null, null, TipoEvento.Reuniao, new DateTime(2026, 10, 9, 17, 0, 0, DateTimeKind.Utc), 60,
            null, null, null, []));
        daProducao.Sucesso.Should().BeTrue(daProducao.Mensagem);
        _daProducao = daProducao.Valor.Id;

        var reservado = await _app.ExecutarAsync(_admin, new SalvarEvento(
            null, "Reunião reservada da diretoria", null, null, TipoEvento.Reuniao, new DateTime(2026, 10, 8, 13, 0, 0, DateTimeKind.Utc), 60,
            null, null, null, []));
        reservado.Sucesso.Should().BeTrue(reservado.Mensagem);
        _reservado = reservado.Valor.Id;
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> PaginaAsync(string rota, bool comoPedro = false)
    {
        var cliente = comoPedro
            ? await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro)
            : await _app.ClienteAutenticadoAsync();
        return WebUtility.HtmlDecode(await cliente.GetStringAsync(rota));
    }

    private static List<string> Grupos(string html, string padrao) =>
        [.. Regex.Matches(html, padrao).Select(m => m.Groups[1].Value)];

    private const string NomesDosParticipantes = "<span class=\"aj-participante__nome\">([^<]+)</span>";

    [Fact]
    public async Task Diretoria_ve_o_cabecalho_os_paineis_e_as_acoes_em_cada_visao()
    {
        var html = await PaginaAsync($"/agenda/{_audiencia}");

        // O voltar: link no desktop e no tablet, barra de voltar com "Evento" no mobile.
        html.Should().Contain("<h1>Evento</h1>").And.Contain("aria-label=\"Voltar para Agenda\"")
            .And.MatchRegex("<a href=\"/agenda\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Agenda</a>")
            .And.NotContain("Seu acesso foi encerrado");

        // Cabeçalho do desktop e do tablet: o título e a linha de contexto com o local.
        html.Should().MatchRegex(
            "(?s)<header class=\"aj-painel aj-painel--principal aj-painel--cabecalho\">\\s*" +
            "<div class=\"aj-cabecalho__texto aj-cabecalho__texto--folgado\">\\s*<h1 class=\"aj-titulo\">Audiência de instrução — Litoral</h1>\\s*" +
            "<p class=\"aj-subtitulo\">Audiência · 07/10/2026, 09:00 – 10:30 · Fórum Trabalhista, sala 4</p>");
        // Mobile: o resumo sem o local, acima do título em h2 (o h1 é o da barra).
        BlocoDeVisao.De(html, "aj-so-mobile").Should().MatchRegex(
            "(?s)<div class=\"aj-titulo-da-ficha\">\\s*<span class=\"aj-ajuda\">Audiência · 07/10/2026, 09:00 – 10:30</span>\\s*" +
            "<h2 class=\"aj-titulo\">Audiência de instrução — Litoral</h2>");

        // Desktop: Excluir (perigo) e Editar (secundário) no cabeçalho; o painel Detalhes com as nove linhas do quadro.
        BlocoDeVisao.De(html, "aj-so-desktop").Should()
            .MatchRegex(
                "(?s)<div class=\"aj-painel__botoes\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--perigo\"[^>]*>Excluir</button>\\s*" +
                $"<a href=\"/agenda/{_audiencia}/editar\" class=\"aj-botao aj-botao--secundario\">Editar</a>\\s*</div>")
            .And.MatchRegex(
                "(?s)<section class=\"aj-painel aj-lado-a-lado__painel\">\\s*<h2>Detalhes</h2>\\s*<dl class=\"aj-detalhes\">\\s*" +
                "<div><dt>Tipo</dt><dd>Audiência</dd></div>\\s*" +
                "<div><dt>Início</dt><dd>07/10/2026 09:00</dd></div>\\s*" +
                "<div><dt>Fim</dt><dd>07/10/2026 10:30</dd></div>\\s*" +
                "<div><dt>Duração</dt><dd>90 minutos</dd></div>\\s*" +
                "<div><dt>Local</dt><dd>Fórum Trabalhista, sala 4</dd></div>\\s*" +
                "<div><dt>Cliente</dt><dd>Transportes Litoral Ltda.</dd></div>\\s*" +
                "<div><dt>Processo</dt><dd>Litoral — reclamação trabalhista</dd></div>\\s*" +
                "<div><dt>Atividade</dt><dd>—</dd></div>\\s*" +
                "<div><dt>Criado por</dt><dd>Diana Diretora</dd></div>\\s*</dl>");

        // Tablet e mobile: o mesmo painel Detalhes e a barra fixa com Excluir (secundário vermelho) e Editar (primário).
        BlocoDeVisao.De(html, "aj-tablet-mobile").Should()
            .MatchRegex(
                "(?s)<section class=\"aj-painel\">\\s*<h2>Detalhes</h2>\\s*<dl class=\"aj-detalhes\">\\s*<div><dt>Tipo</dt><dd>Audiência</dd></div>" +
                ".*?<div><dt>Criado por</dt><dd>Diana Diretora</dd></div>\\s*</dl>")
            .And.MatchRegex(
                "(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--secundario aj-botao--destrutivo\"[^>]*>Excluir</button>\\s*" +
                $"<a href=\"/agenda/{_audiencia}/editar\" class=\"aj-botao aj-botao--primario\">Editar</a>\\s*</div>");

        // A ordem: no desktop, a coluna (Descrição, Participantes) e depois o painel Detalhes; no tablet e no mobile o
        // arranjo some e fica Descrição, Detalhes, Participantes. A descrição guarda a quebra de linha digitada.
        html.Should().MatchRegex(
            "(?s)<div class=\"aj-lado-a-lado aj-lado-a-lado--ficha\">\\s*<div class=\"aj-lado-a-lado__lista aj-pilha\">\\s*" +
            "<section class=\"aj-painel\">\\s*<h2>Descrição</h2>\\s*<p class=\"aj-texto-livre\">Audiência de instrução.\nLevar a proposta de acordo em seis parcelas.</p>\\s*</section>\\s*" +
            "<div class=\"aj-tablet-mobile\">\\s*<section class=\"aj-painel\">\\s*<h2>Detalhes</h2>.*?</section>\\s*</div>\\s*" +
            "<section class=\"aj-painel\">\\s*<h2>Participantes</h2>.*?</section>\\s*</div>\\s*" +
            "<div class=\"aj-so-desktop\">\\s*<section class=\"aj-painel aj-lado-a-lado__painel\">\\s*<h2>Detalhes</h2>");
        Regex.Matches(html, "<h2>Descrição</h2>").Count.Should().Be(1);
        Regex.Matches(html, "<h2>Detalhes</h2>").Count.Should().Be(2);
        Regex.Matches(html, "<h2>Participantes</h2>").Count.Should().Be(1);

        // Participantes: o próprio usuário primeiro, com "(você)" e o avatar dourado (sem modificador de cor); os outros
        // na ordem do servidor, com a cor da regra dos avatares.
        Grupos(html, NomesDosParticipantes).Should().Equal("Diana Diretora (você)", "Beatriz Lins", "Pedro Produção");
        html.Should()
            .MatchRegex("<ul class=\"aj-participantes\">\\s*<li class=\"aj-participante\"><span class=\"aj-avatar\" aria-hidden=\"true\">DD</span>" +
                "<span class=\"aj-participante__nome\">Diana Diretora \\(você\\)</span></li>")
            .And.Contain($"<li class=\"aj-participante\"><span class=\"aj-avatar {AvatarDePessoa.Classe("Beatriz Lins")}\" aria-hidden=\"true\">BL</span>" +
                "<span class=\"aj-participante__nome\">Beatriz Lins</span></li>")
            .And.Contain($"<span class=\"aj-avatar {AvatarDePessoa.Classe("Pedro Produção")}\" aria-hidden=\"true\">PP</span>");
    }

    [Fact]
    public async Task Producao_que_participa_e_nao_criou_ve_a_ficha_sem_acoes_e_sem_barra()
    {
        var html = await PaginaAsync($"/agenda/{_audiencia}", comoPedro: true);

        html.Should().Contain("<h1 class=\"aj-titulo\">Audiência de instrução — Litoral</h1>").And.Contain("<h2>Detalhes</h2>");
        // O "(você)" e o dourado acompanham quem vê: aqui o Pedro vem primeiro, e a Diana ganha a cor da regra.
        Grupos(html, NomesDosParticipantes).Should().Equal("Pedro Produção (você)", "Beatriz Lins", "Diana Diretora");
        html.Should().Contain("<span class=\"aj-avatar\" aria-hidden=\"true\">PP</span>")
            .And.Contain($"<span class=\"aj-avatar {AvatarDePessoa.Classe("Diana Diretora")}\" aria-hidden=\"true\">DD</span>");
        html.Should().NotContain("aj-painel__botoes").And.NotContain("aj-acoes--fixa").And.NotContain(">Excluir</button>")
            .And.NotContain("/editar").And.NotContain("Seu acesso foi encerrado");
    }

    [Fact]
    public async Task Producao_que_criou_ve_excluir_e_editar_no_desktop_e_na_barra()
    {
        var html = await PaginaAsync($"/agenda/{_daProducao}", comoPedro: true);

        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            "(?s)<div class=\"aj-painel__botoes\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--perigo\"[^>]*>Excluir</button>\\s*" +
            $"<a href=\"/agenda/{_daProducao}/editar\" class=\"aj-botao aj-botao--secundario\">Editar</a>");
        BlocoDeVisao.De(html, "aj-tablet-mobile").Should().MatchRegex(
            "(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--secundario aj-botao--destrutivo\"[^>]*>Excluir</button>\\s*" +
            $"<a href=\"/agenda/{_daProducao}/editar\" class=\"aj-botao aj-botao--primario\">Editar</a>");
        Grupos(html, NomesDosParticipantes).Should().Equal("Pedro Produção (você)");
    }

    [Fact]
    public async Task Evento_sem_descricao_sem_local_e_sem_vinculos_some_com_a_descricao_e_mostra_os_vazios()
    {
        var html = await PaginaAsync($"/agenda/{_daProducao}");

        // Sem local, a linha de contexto e o resumo do mobile acabam no horário.
        html.Should().Contain("<p class=\"aj-subtitulo\">Reunião · 09/10/2026, 14:00 – 15:00</p>")
            .And.Contain("<span class=\"aj-ajuda\">Reunião · 09/10/2026, 14:00 – 15:00</span>");
        BlocoDeVisao.De(html, "aj-so-desktop").Should()
            .Contain("<div><dt>Duração</dt><dd>60 minutos</dd></div>")
            .And.Contain("<div><dt>Local</dt><dd>—</dd></div>")
            .And.Contain("<div><dt>Cliente</dt><dd>—</dd></div>")
            .And.Contain("<div><dt>Processo</dt><dd>—</dd></div>")
            .And.Contain("<div><dt>Atividade</dt><dd>—</dd></div>")
            .And.Contain("<div><dt>Criado por</dt><dd>Pedro Produção</dd></div>");
        // Sem descrição, o painel some: a coluna começa pelos Detalhes do tablet e do mobile.
        html.Should().MatchRegex(
                "<div class=\"aj-lado-a-lado__lista aj-pilha\">\\s*<div class=\"aj-tablet-mobile\">\\s*<section class=\"aj-painel\">\\s*<h2>Detalhes</h2>")
            .And.NotContain("<h2>Descrição</h2>").And.NotContain("aj-texto-livre");
        // A Diretoria não participa: ninguém leva "(você)", e o avatar do Pedro tem a cor da regra.
        html.Should().Contain($"<span class=\"aj-avatar {AvatarDePessoa.Classe("Pedro Produção")}\" aria-hidden=\"true\">PP</span>" +
                "<span class=\"aj-participante__nome\">Pedro Produção</span>")
            .And.NotContain("(você)");
    }

    [Fact]
    public async Task Evento_que_nao_existe_e_o_que_a_producao_nao_ve_mostram_o_mesmo_nao_encontrado()
    {
        var antes = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Count;

        var inexistente = await PaginaAsync($"/agenda/{Guid.NewGuid()}");
        var escondido = await PaginaAsync($"/agenda/{_reservado}", comoPedro: true);

        foreach (var html in new[] { inexistente, escondido })
        {
            html.Should().Contain("Evento não encontrado.").And.Contain("<h1>Evento</h1>")
                .And.Contain("aria-label=\"Voltar para Agenda\"").And.MatchRegex("<a href=\"/agenda\" class=\"aj-voltar\">");
            html.Should().NotContain("<h2>Detalhes</h2>").And.NotContain("<h2>Participantes</h2>").And.NotContain("aj-acoes--fixa")
                .And.NotContain("Reunião reservada da diretoria");
        }

        // O servidor recusou a leitura do Pedro e registrou a tentativa (o id inexistente não registra nada).
        var negados = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Skip(antes)
            .Select(e => (string?)JsonNode.Parse(e.Dados!)!["requisicao"]).ToList();
        negados.Should().NotBeEmpty().And.OnlyContain(r => r == nameof(ObterEvento));
    }

    [Fact]
    public async Task Vinda_da_agenda_leva_o_periodo_ao_voltar_e_ao_editar_e_descarta_o_que_a_agenda_nao_aceita()
    {
        var cliente = await _app.ClienteAutenticadoAsync();
        async Task<string> Pagina(string rota) => WebUtility.HtmlDecode(await cliente.GetStringAsync(rota));

        var doDia = await Pagina($"/agenda/{_audiencia}?visao=dia&data=2026-10-07");
        var estranho = await Pagina($"/agenda/{_audiencia}?visao=xyz&data=2026-13-40");
        var naoEncontrado = await Pagina($"/agenda/{Guid.NewGuid()}?visao=mes&data=2026-10-07");

        doDia.Should()
            .MatchRegex("<a href=\"/agenda\\?visao=dia&data=2026-10-07\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Agenda</a>")
            .And.Contain("<a href=\"/agenda?visao=dia&data=2026-10-07\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Voltar para Agenda\">")
            .And.Contain($"<a href=\"/agenda/{_audiencia}/editar?visao=dia&data=2026-10-07\" class=\"aj-botao aj-botao--secundario\">Editar</a>")
            .And.Contain($"<a href=\"/agenda/{_audiencia}/editar?visao=dia&data=2026-10-07\" class=\"aj-botao aj-botao--primario\">Editar</a>");

        // Visão desconhecida e data inválida não seguem adiante: o voltar e o editar vão sem consulta.
        estranho.Should().MatchRegex("<a href=\"/agenda\" class=\"aj-voltar\">")
            .And.Contain($"<a href=\"/agenda/{_audiencia}/editar\" class=\"aj-botao aj-botao--primario\">Editar</a>");
        estranho.Should().NotContain("href=\"/agenda?visao=xyz").And.NotContain("editar?");

        // O não encontrado também volta ao período de onde se veio.
        naoEncontrado.Should().Contain("Evento não encontrado.")
            .And.MatchRegex("<a href=\"/agenda\\?visao=mes&data=2026-10-07\" class=\"aj-voltar\">");
    }
}
```

(O CNPJ é o mesmo dos testes das fichas do cliente e da atividade. Se o `<svg>` do voltar ou um botão vierem com outro espaço em branco entre as marcas, ajuste só o `\s*`, sem mudar o que o teste prova.)

Em `PortalDoClienteWebTests.cs`, acrescente `[InlineData("/agenda/0199a3f2-7c1e-7000-8000-000000000001")]` logo depois de `[InlineData("/agenda")]` na theory `Cliente_em_pagina_interna_ve_nao_encontrada_e_nenhum_dado`. Em `AutenticacaoWebTests.cs`, a mesma rota na theory `Anonimo_e_redirecionado_ao_login_em_qualquer_rota`, logo depois de `[InlineData("/atividades/0199a3f2-7c1e-7000-8000-000000000001")]`.

- [ ] **Step 2: Testes bUnit da ficha (devem falhar)**

Crie `tests/AtividadesJuridicas.IntegrationTests/EventoFichaInterativaTests.cs`. O evento é um `EventoDetalhe` montado no teste (`Detalhe(...)`), com as `AcoesDoEvento` do caso; o `DespachanteCombinado` responde a carga (`Combinar(...)`). A ficha lê `visao` e `data` do endereço: os testes que precisam da origem começam a navegação de teste nele (`FichaVindaDe`).

```csharp
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

public class EventoFichaInterativaTests : TelaInterativa
{
    private static readonly Guid Id = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000e7");
    private static readonly Guid Beatriz = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b1");
    private static readonly Guid Otavio = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b2");

    private static readonly AcoesDoEvento Gerencia = new(true, true);
    private static readonly AcoesDoEvento SoVe = new(false, false);

    /// <summary>
    /// O evento do quadro: Audiência em 07/10/2026, das 09:00 às 10:30 no fuso do escritório, com cliente e processo, sem
    /// atividade, criada pela Diretoria, versão 7. Participam a Beatriz e a Diretoria (na ordem do servidor, por nome).
    /// </summary>
    private static EventoDetalhe Detalhe(AcoesDoEvento? acoes = null, IReadOnlyList<ParticipanteDetalhe>? participantes = null) => new(
        Id, "Audiência de instrução — Litoral", "Audiência de instrução.\nLevar a proposta de acordo.", "Fórum Trabalhista, sala 4",
        TipoEvento.Audiencia, new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc), 90, new DateTime(2026, 10, 7, 13, 30, 0, DateTimeKind.Utc),
        Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c1"), "Transportes Litoral Ltda.",
        Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000f1"), "Litoral — reclamação trabalhista",
        null, null, AtorDaDiretoria.Id, AtorDaDiretoria.Nome,
        participantes ?? new ParticipanteDetalhe[] { new(Beatriz, "Beatriz Lins", true), new(AtorDaDiretoria.Id, AtorDaDiretoria.Nome, true) },
        acoes ?? Gerencia, 7);

    /// <summary>O perfil de quem vê e o evento.</summary>
    private void Combinar(EventoDetalhe? detalhe = null, UsuarioAutenticado? ator = null) =>
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(ator ?? AtorDaDiretoria)
            .Responde<ObterEvento, Resultado<EventoDetalhe>>(Resultado<EventoDetalhe>.Ok(detalhe ?? Detalhe()));

    private IRenderedComponent<EventoFicha> Ficha() => Desenhar<EventoFicha>(p => p.Add(f => f.Id, Id));

    /// <summary>A ficha aberta a partir de um período da agenda: a navegação de teste começa no endereço com a origem.</summary>
    private IRenderedComponent<EventoFicha> FichaVindaDe(string consulta)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/agenda/{Id}{consulta}");
        return Ficha();
    }

    private string Endereco => Services.GetRequiredService<NavigationManager>().Uri;

    private const string NoCabecalho = ".aj-painel__botoes button, .aj-painel__botoes a";
    private const string NaBarra = ".aj-acoes--fixa button, .aj-acoes--fixa a";
    private const string ExcluirNoCabecalho = ".aj-painel__botoes button";
    private const string ExcluirNaBarra = ".aj-acoes--fixa button";
    private const string NaConfirmacao = "[role=alertdialog] button";

    private static void Clicar(IRenderedComponent<EventoFicha> tela, string seletor, string texto) =>
        tela.FindAll(seletor).First(e => e.TextContent == texto).Click();

    private static List<string> Textos(IEnumerable<IElement> elementos) =>
        [.. elementos.Select(e => Regex.Replace(e.TextContent, "\\s+", " ").Trim()).Where(t => t.Length > 0)];

    [Theory]
    // Quem vê, e o que o servidor permite: a Diretoria, a Produção que criou (permite) e a Produção que só participa.
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void As_acoes_seguem_o_que_o_servidor_permite_e_nao_o_perfil(bool diretoria, bool gerencia)
    {
        var ator = diretoria ? AtorDaDiretoria : AtorDaProducao;
        Combinar(Detalhe(gerencia ? Gerencia : SoVe, [new(ator.Id, ator.Nome, true)]), ator);

        var ficha = Ficha();

        ficha.Find("h1.aj-titulo").TextContent.Should().Be("Audiência de instrução — Litoral");
        string[] esperadas = gerencia ? ["Excluir", "Editar"] : [];
        Textos(ficha.FindAll(NoCabecalho)).Should().Equal(esperadas);
        Textos(ficha.FindAll(NaBarra)).Should().Equal(esperadas);
        ficha.FindAll(".aj-painel__botoes").Should().HaveCount(gerencia ? 1 : 0);
        ficha.FindAll(".aj-acoes--fixa").Should().HaveCount(gerencia ? 1 : 0);
        ficha.FindAll($"a[href='/agenda/{Id}/editar']").Should().HaveCount(gerencia ? 2 : 0);
        ficha.FindAll(".aj-botao--destrutivo").Should().HaveCount(gerencia ? 1 : 0);
    }

    [Fact]
    public void Participantes_trazem_voce_primeiro_e_dourado_os_inativos_e_os_sem_nome()
    {
        Combinar(Detalhe(participantes:
        [
            new(Beatriz, "Beatriz Lins", true),
            new(AtorDaDiretoria.Id, AtorDaDiretoria.Nome, true),
            new(Otavio, "Otávio Brandão", false),
            new(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b3"), null, false),
        ]));

        var ficha = Ficha();

        var linhas = ficha.FindAll(".aj-participantes > .aj-participante");
        linhas.Select(l => l.QuerySelector(".aj-participante__nome")!.TextContent).Should()
            .Equal("Diana Diretora (você)", "Beatriz Lins", "Otávio Brandão (inativo)", "Usuário não encontrado");
        linhas.Select(l => l.QuerySelector(".aj-avatar")!.TextContent).Should().Equal("DD", "BL", "OB", "?");
        linhas.Select(l => l.QuerySelector(".aj-avatar")!.ClassName).Should().Equal(
            "aj-avatar",
            $"aj-avatar {AvatarDePessoa.Classe("Beatriz Lins")}",
            $"aj-avatar {AvatarDePessoa.Classe("Otávio Brandão")}",
            $"aj-avatar {AvatarDePessoa.Classe("")}");
        linhas.Should().OnlyContain(l => l.QuerySelector(".aj-avatar")!.GetAttribute("aria-hidden") == "true");
        // Um painel só, para as três visões.
        ficha.FindAll(".aj-participantes").Should().ContainSingle();
    }

    [Fact]
    public void Detalhes_trazem_as_nove_linhas_do_quadro_no_desktop_e_no_tablet_e_mobile()
    {
        Combinar();

        var ficha = Ficha();

        string[] termos = ["Tipo", "Início", "Fim", "Duração", "Local", "Cliente", "Processo", "Atividade", "Criado por"];
        ficha.FindAll(".aj-so-desktop .aj-detalhes dt").Select(d => d.TextContent).Should().Equal(termos);
        ficha.FindAll(".aj-tablet-mobile .aj-detalhes dt").Select(d => d.TextContent).Should().Equal(termos);
        ficha.FindAll(".aj-so-desktop .aj-detalhes dd").Select(d => d.TextContent).Should().Equal(
            "Audiência", "07/10/2026 09:00", "07/10/2026 10:30", "90 minutos", "Fórum Trabalhista, sala 4",
            "Transportes Litoral Ltda.", "Litoral — reclamação trabalhista", "—", "Diana Diretora");
        ficha.FindAll("p.aj-texto-livre").Should().ContainSingle()
            .Which.TextContent.Should().Be("Audiência de instrução.\nLevar a proposta de acordo.");
        ficha.Find("p.aj-subtitulo").TextContent.Should().Be("Audiência · 07/10/2026, 09:00 – 10:30 · Fórum Trabalhista, sala 4");
        ficha.Find(".aj-titulo-da-ficha .aj-ajuda").TextContent.Should().Be("Audiência · 07/10/2026, 09:00 – 10:30");
    }

    [Theory]
    [InlineData("Minuta de acordo", false, "Minuta de acordo")]
    [InlineData(null, false, "Atividade vinculada (sem acesso)")]
    [InlineData(null, true, "Atividade vinculada (excluída)")]
    public void Vinculos_indisponiveis_e_criador_sumido_tem_textos_proprios(string? tituloDaAtividade, bool excluida, string atividade)
    {
        Combinar(Detalhe() with
        {
            ClienteNome = null,
            ProcessoTitulo = null,
            AtividadeId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a1"),
            AtividadeTitulo = tituloDaAtividade,
            AtividadeExcluida = excluida,
            CriadorNome = null,
        });

        var ficha = Ficha();

        var pares = ficha.FindAll(".aj-so-desktop .aj-detalhes > div")
            .ToDictionary(d => d.QuerySelector("dt")!.TextContent, d => d.QuerySelector("dd")!.TextContent);
        pares["Cliente"].Should().Be("Cliente indisponível");
        pares["Processo"].Should().Be("Processo indisponível");
        pares["Atividade"].Should().Be(atividade);
        pares["Criado por"].Should().Be("Usuário não encontrado");
    }

    [Fact]
    public void Evento_que_atravessa_a_meia_noite_mostra_a_data_do_fim_no_subtitulo_e_no_resumo()
    {
        // 02:30 UTC de 08/10 é 23:30 de 07/10 no escritório; o fim, 00:30 de 08/10.
        Combinar(Detalhe() with
        {
            InicioEm = new DateTime(2026, 10, 8, 2, 30, 0, DateTimeKind.Utc),
            DuracaoEstimadaMinutos = 60,
            FimEm = new DateTime(2026, 10, 8, 3, 30, 0, DateTimeKind.Utc),
        });

        var ficha = Ficha();

        ficha.Find("p.aj-subtitulo").TextContent.Should().Be("Audiência · 07/10/2026, 23:30 – 08/10 00:30 · Fórum Trabalhista, sala 4");
        ficha.Find(".aj-titulo-da-ficha .aj-ajuda").TextContent.Should().Be("Audiência · 07/10/2026, 23:30 – 08/10 00:30");
        ficha.FindAll(".aj-so-desktop .aj-detalhes dd")[2].TextContent.Should().Be("08/10/2026 00:30");
    }

    [Fact]
    public void Recado_de_evento_salvo_com_sobreposicao_mostra_as_tres_redacoes_uma_vez()
    {
        Combinar();
        // 13:00 a 14:30 UTC é 10:00 a 11:30 no escritório.
        Services.GetRequiredService<Recados>().Deixar("Evento salvo.", TipoDeAviso.Sucesso,
            [new SobreposicaoDeAgenda(Beatriz, "Beatriz Lins",
                new DateTime(2026, 10, 7, 13, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 7, 14, 30, 0, DateTimeKind.Utc))]);

        var ficha = Ficha();

        Textos(ficha.FindAll(".aj-so-desktop p.aj-aviso--atencao")).Should()
            .Equal("Evento salvo. Atenção: Beatriz Lins já tem evento das 10:00 às 11:30 (07/10).");
        Textos(ficha.FindAll(".aj-so-tablet p.aj-aviso--atencao")).Should()
            .Equal("Evento salvo. Beatriz Lins já tem evento das 10:00 às 11:30 (07/10).");
        Textos(ficha.FindAll(".aj-so-mobile p.aj-aviso--atencao")).Should()
            .Equal("Beatriz Lins já tem evento das 10:00 às 11:30 em 07/10.");
        ficha.FindAll("p.aj-aviso--sucesso").Should().BeEmpty();
        Services.GetRequiredService<Recados>().Retirar().Should().BeNull();
    }

    [Fact]
    public void Recado_sem_sobreposicao_e_um_aviso_de_sucesso()
    {
        Combinar();
        Services.GetRequiredService<Recados>().Deixar("Evento salvo.");

        var ficha = Ficha();

        ficha.Find("p.aj-aviso--sucesso").TextContent.Should().Be("Evento salvo.");
        ficha.FindAll("p.aj-aviso--atencao").Should().BeEmpty();
        Services.GetRequiredService<Recados>().Retirar().Should().BeNull();
    }

    [Fact]
    public void Excluir_pede_a_confirmacao_e_confirmar_envia_com_a_versao_e_volta_a_agenda_de_origem_com_o_recado()
    {
        Combinar();
        Despachante.Responde<ExcluirEvento, Resultado>(Resultado.Ok());
        var ficha = FichaVindaDe("?visao=dia&data=2026-10-07");

        Clicar(ficha, ExcluirNoCabecalho, "Excluir");

        var confirmacao = ficha.Find("[role=alertdialog]");
        confirmacao.GetAttribute("aria-label").Should().Be("Confirmar exclusão");
        confirmacao.ClassList.Should().Contain("aj-aviso--atencao");
        ficha.Find("[role=alertdialog] strong").TextContent.Should().Be("Confirmar a exclusão de Audiência de instrução — Litoral?");
        ficha.Find("[role=alertdialog] .aj-aviso__texto").TextContent.Should().Contain("O evento sai da agenda de todos os participantes.");
        Textos(ficha.FindAll(NaConfirmacao)).Should().Equal("Cancelar", "Confirmar exclusão");
        ficha.Find("[role=alertdialog] .aj-botao--primario").TextContent.Should().Be("Confirmar exclusão");
        Despachante.Recebidas.OfType<ExcluirEvento>().Should().BeEmpty();

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        ficha.WaitForAssertion(() => Endereco.Should().EndWith("/agenda?visao=dia&data=2026-10-07"));
        Despachante.Recebidas.OfType<ExcluirEvento>().Should().ContainSingle().Which.Should().Be(new ExcluirEvento(Id, 7));
        var recado = Services.GetRequiredService<Recados>().Retirar()!;
        recado.Texto.Should().Be("Evento excluído.");
        recado.Tipo.Should().Be(TipoDeAviso.Sucesso);
        recado.Sobreposicoes.Should().BeNull();
    }

    [Fact]
    public void Excluir_pela_barra_fixa_abre_a_mesma_confirmacao_tira_o_recado_e_sem_origem_volta_a_agenda()
    {
        Combinar();
        Despachante.Responde<ExcluirEvento, Resultado>(Resultado.Ok());
        Services.GetRequiredService<Recados>().Deixar("Evento salvo.");
        var ficha = Ficha();
        ficha.Find("p.aj-aviso--sucesso").TextContent.Should().Be("Evento salvo.");

        Clicar(ficha, ExcluirNaBarra, "Excluir");

        ficha.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar exclusão");
        ficha.FindAll("p.aj-aviso--sucesso").Should().BeEmpty();

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        ficha.WaitForAssertion(() => Endereco.Should().EndWith("/agenda"));
        Despachante.Recebidas.OfType<ExcluirEvento>().Should().ContainSingle();
    }

    [Fact]
    public void Cancelar_fecha_a_confirmacao_e_nao_envia_nada()
    {
        Combinar();
        var ficha = Ficha();
        Clicar(ficha, ExcluirNoCabecalho, "Excluir");
        ficha.FindAll("[role=alertdialog]").Should().ContainSingle();

        Clicar(ficha, NaConfirmacao, "Cancelar");

        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        Textos(ficha.FindAll(NoCabecalho)).Should().Equal("Excluir", "Editar");
        Despachante.Recebidas.OfType<ExcluirEvento>().Should().BeEmpty();
        Despachante.Recebidas.OfType<ObterEvento>().Should().ContainSingle();
    }

    [Fact]
    public void Clique_duplo_em_confirmar_exclusao_envia_uma_vez()
    {
        var resposta = new TaskCompletionSource<Resultado>();
        Combinar();
        Despachante.RespondeDepois<ExcluirEvento, Resultado>(resposta.Task);
        var ficha = FichaVindaDe("?visao=semana&data=2026-10-05");
        Clicar(ficha, ExcluirNoCabecalho, "Excluir");

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");
        ficha.FindAll(NaConfirmacao).Should().NotBeEmpty().And.OnlyContain(b => b.HasAttribute("disabled"));
        ficha.FindAll(ExcluirNoCabecalho).Should().NotBeEmpty().And.OnlyContain(b => b.HasAttribute("disabled"));
        ficha.FindAll(ExcluirNaBarra).Should().NotBeEmpty().And.OnlyContain(b => b.HasAttribute("disabled"));
        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");
        Clicar(ficha, ExcluirNaBarra, "Excluir");
        resposta.SetResult(Resultado.Ok());

        ficha.WaitForAssertion(() => Endereco.Should().EndWith("/agenda?visao=semana&data=2026-10-05"));
        Despachante.Recebidas.OfType<ExcluirEvento>().Should().ContainSingle().Which.Should().Be(new ExcluirEvento(Id, 7));
    }

    [Fact]
    public void Depois_de_excluir_com_sucesso_o_botao_fica_travado_ate_a_navegacao()
    {
        Combinar();
        Despachante.Responde<ExcluirEvento, Resultado>(Resultado.Ok());
        var ficha = Ficha();
        Clicar(ficha, ExcluirNoCabecalho, "Excluir");
        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");
        ficha.WaitForAssertion(() => Endereco.Should().EndWith("/agenda"));

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        Despachante.Recebidas.OfType<ExcluirEvento>().Should().ContainSingle();
        ficha.FindAll(NaConfirmacao).Should().NotBeEmpty().And.OnlyContain(b => b.HasAttribute("disabled"));
    }

    [Fact]
    public void Exclusao_recusada_fecha_a_confirmacao_recarrega_e_mostra_a_mensagem_do_servidor()
    {
        var leituras = 0;
        Combinar();
        Despachante
            .Responde<ObterEvento, Resultado<EventoDetalhe>>(_ => Resultado<EventoDetalhe>.Ok(++leituras == 1
                ? Detalhe()
                : Detalhe() with { Titulo = "Audiência de instrução — Litoral (remarcada)", Versao = 8 }))
            .Responde<ExcluirEvento, Resultado>(Resultado.Falha(TipoErro.Conflito,
                "O evento foi alterado por outra pessoa. Recarregue e tente novamente."));
        var ficha = FichaVindaDe("?visao=dia&data=2026-10-07");
        Clicar(ficha, ExcluirNoCabecalho, "Excluir");

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        ficha.WaitForAssertion(() => ficha.Find("[role=alert]").TextContent.Should()
            .Contain("O evento foi alterado por outra pessoa. Recarregue e tente novamente."));
        Despachante.Recebidas.OfType<ObterEvento>().Should().HaveCount(2);
        ficha.Find("h1.aj-titulo").TextContent.Should().Be("Audiência de instrução — Litoral (remarcada)");
        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        ficha.FindAll(ExcluirNoCabecalho).Should().NotBeEmpty().And.OnlyContain(b => !b.HasAttribute("disabled"));
        Endereco.Should().EndWith($"/agenda/{Id}?visao=dia&data=2026-10-07");
        Services.GetRequiredService<Recados>().Retirar().Should().BeNull();

        // A nova confirmação leva a versão relida, não a da primeira carga.
        Clicar(ficha, ExcluirNoCabecalho, "Excluir");
        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        ficha.WaitForAssertion(() => Despachante.Recebidas.OfType<ExcluirEvento>().Should().HaveCount(2));
        Despachante.Recebidas.OfType<ExcluirEvento>().Select(e => e.VersaoEsperada).Should().Equal(new uint?[] { 7, 8 });
    }

    [Fact]
    public void Exclusao_de_evento_que_sumiu_mostra_so_o_nao_encontrado()
    {
        var leituras = 0;
        Combinar();
        Despachante
            .Responde<ObterEvento, Resultado<EventoDetalhe>>(_ => ++leituras == 1
                ? Resultado<EventoDetalhe>.Ok(Detalhe())
                : Resultado<EventoDetalhe>.Falha(TipoErro.NaoEncontrado, "Evento não encontrado."))
            .Responde<ExcluirEvento, Resultado>(Resultado.Falha(TipoErro.NaoEncontrado, "Evento não encontrado."));
        var ficha = Ficha();
        Clicar(ficha, ExcluirNoCabecalho, "Excluir");

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        ficha.WaitForAssertion(() => ficha.FindAll(".aj-detalhes").Should().BeEmpty());
        Regex.Matches(ficha.Markup, "Evento não encontrado\\.").Count.Should().Be(1);
        ficha.Find("a.aj-voltar").GetAttribute("href").Should().Be("/agenda");
        Services.GetRequiredService<Recados>().Retirar().Should().BeNull();
    }

    [Fact]
    public void Acesso_negado_ao_excluir_encerra_a_tela()
    {
        Combinar();
        Despachante.Responde<ExcluirEvento, Resultado>(Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado."));
        var ficha = Ficha();
        Clicar(ficha, ExcluirNoCabecalho, "Excluir");

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Seu acesso foi encerrado. Entre novamente."));
        ficha.Markup.Should().NotContain("Audiência de instrução — Litoral").And.NotContain("Acesso negado.");
        Despachante.Recebidas.OfType<ObterEvento>().Should().ContainSingle();
        Endereco.Should().NotEndWith("/agenda");
    }

    [Fact]
    public void Nao_encontrado_mostra_o_voltar_com_a_origem_e_nada_mais()
    {
        Combinar();
        Despachante.Responde<ObterEvento, Resultado<EventoDetalhe>>(
            Resultado<EventoDetalhe>.Falha(TipoErro.NaoEncontrado, "Evento não encontrado."));

        var ficha = FichaVindaDe("?visao=mes&data=2026-10-07");

        ficha.Find("[role=alert]").TextContent.Should().Be("Evento não encontrado.");
        ficha.Find("a.aj-voltar").GetAttribute("href").Should().Be("/agenda?visao=mes&data=2026-10-07");
        ficha.Find("h1.aj-titulo").TextContent.Should().Be("Evento");
        ficha.FindAll(".aj-detalhes").Should().BeEmpty();
        ficha.FindAll(".aj-acoes--fixa").Should().BeEmpty();
    }

    [Fact]
    public void Falha_na_consulta_mostra_o_erro_e_tentar_de_novo_traz_a_ficha()
    {
        var tentativas = 0;
        Combinar();
        Despachante.Responde<ObterEvento, Resultado<EventoDetalhe>>(_ => ++tentativas == 1
            ? Resultado<EventoDetalhe>.Falha(TipoErro.Validacao, "Falha de teste.")
            : Resultado<EventoDetalhe>.Ok(Detalhe()));
        var ficha = Ficha();
        ficha.Markup.Should().Contain("Falha de teste.");
        ficha.FindAll(".aj-detalhes").Should().BeEmpty();

        ficha.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        ficha.WaitForAssertion(() => ficha.Find("h1.aj-titulo").TextContent.Should().Be("Audiência de instrução — Litoral"));
        ficha.Markup.Should().NotContain("Falha de teste.");
    }

    [Fact]
    public void Acesso_encerrado_antes_da_carga_nao_consulta_o_evento()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>((UsuarioAutenticado?)null);

        var ficha = Ficha();

        ficha.Markup.Should().Contain("Seu acesso foi encerrado. Entre novamente.");
        ficha.FindAll(".aj-detalhes").Should().BeEmpty();
        Despachante.Recebidas.OfType<ObterEvento>().Should().BeEmpty();
    }

    [Fact]
    public void Acesso_negado_na_leitura_do_evento_encerra_a_tela()
    {
        Combinar();
        Despachante.Responde<ObterEvento, Resultado<EventoDetalhe>>(
            Resultado<EventoDetalhe>.Falha(TipoErro.AcessoNegado, "Acesso negado."));

        var ficha = Ficha();

        ficha.Markup.Should().Contain("Seu acesso foi encerrado. Entre novamente.").And.NotContain("Acesso negado.");
        ficha.FindAll(".aj-detalhes").Should().BeEmpty();
    }

    [Fact]
    public void Trocar_de_evento_na_mesma_tela_recarrega_tira_o_recado_e_nao_retira_outro()
    {
        var outro = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000e8");
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ObterEvento, Resultado<EventoDetalhe>>(r => Resultado<EventoDetalhe>.Ok(r.Id == Id
                ? Detalhe()
                : Detalhe() with { Id = outro, Titulo = "Reunião de alinhamento", Tipo = TipoEvento.Reuniao }));
        var recados = Services.GetRequiredService<Recados>();
        recados.Deixar("Evento salvo.");
        var ficha = Ficha();
        ficha.Find("p.aj-aviso--sucesso").TextContent.Should().Be("Evento salvo.");
        recados.Deixar("Outro recado.");

        ficha.Render(p => p.Add(f => f.Id, outro));

        ficha.WaitForAssertion(() => ficha.Find("h1.aj-titulo").TextContent.Should().Be("Reunião de alinhamento"));
        Despachante.Recebidas.OfType<ObterEvento>().Select(r => r.Id).Should().Equal(Id, outro);
        ficha.FindAll("p.aj-aviso--sucesso").Should().BeEmpty();
        // O recado é retirado só na primeira carga: o deixado depois continua esperando a sua tela.
        recados.Retirar()!.Texto.Should().Be("Outro recado.");

        // O mesmo id não consulta de novo.
        ficha.Render(p => p.Add(f => f.Id, outro));

        Despachante.Recebidas.OfType<ObterEvento>().Should().HaveCount(2);
    }
}
```

Notas para quem escreve os testes:

- O `TextContent` dos avisos sem ações junta o texto do `span` (o `svg` do ícone não tem texto); `Textos` normaliza o espaço em branco, porque o `AvisoDeEventoSalvo` (Tarefa 3) pode quebrar a linha entre o `strong` e o resto.
- `ficha.Render(…)` é o `SetParametersAndRender` do bUnit 1 (o bUnit 2.11.3 do projeto tem `RenderedComponentRenderExtensions.Render`).
- O clique num botão desabilitado ainda chama o manipulador no bUnit: é isso que prova a guarda (`if (_ocupado) return;`), como nos testes da 9E.

- [ ] **Step 3: Rodar (RED)**

Run: `dotnet build -m:1`
Expected: não compila (`EventoFicha` não existe). É o RED.

- [ ] **Step 4: A tela `EventoFicha`**

Crie `src/AtividadesJuridicas.Web/Components/Pages/EventoFicha.razor`:

```razor
@page "/agenda/{Id:guid}"
@inject IDespachante Despachante
@inject FusoDoEscritorio Fuso
@inject Recados Recados
@inject NavigationManager Navegacao

<PageTitle>@(_detalhe?.Titulo ?? "Evento")</PageTitle>

@if (!_carregado || _ator is null || _detalhe is null)
{
    <CabecalhoDeDetalhe Titulo="Evento" VoltarPara="@Voltar" Origem="Agenda" />
    @* A recusa de uma exclusão cuja recarga deixou a ficha fora da tela (a consulta falhou). *@
    @if (_mensagem is not null)
    {
        <Aviso Tipo="TipoDeAviso.Erro">@_mensagem</Aviso>
    }
    @if (!_carregado)
    {
        <p class="aj-subtitulo" role="status">Carregando…</p>
    }
    else if (_ator is null)
    {
        <Aviso Tipo="TipoDeAviso.Erro">Seu acesso foi encerrado. Entre novamente.</Aviso>
    }
    else if (_naoEncontrado)
    {
        @* O evento foi excluído ou o usuário não o vê: o servidor responde igual nos dois casos. *@
        <Aviso Tipo="TipoDeAviso.Erro">Evento não encontrado.</Aviso>
    }
    else
    {
        @* Falha ao consultar nunca vira uma ficha vazia nem deixa dados antigos na tela. *@
        <EstadoDaLista Erro="@(_erro ?? "Não foi possível consultar o evento.")" TextoDeVazio="" TentarDeNovo="CarregarAsync" />
    }
}
else
{
    var evento = _detalhe;
    <CabecalhoDeFicha Titulo="@evento.Titulo" Subtitulo="@RotulosDeAgenda.ResumoDoEvento(Fuso, evento, comLocal: true)"
                      Resumo="@RotulosDeAgenda.ResumoDoEvento(Fuso, evento, comLocal: false)" TituloDaBarra="Evento"
                      VoltarPara="@Voltar" Origem="Agenda" Acoes="@(TemAcoes(evento.Acoes) ? AcoesDoDesktop(evento.Acoes) : null)" />

    @if (_recado is { } recado)
    {
        <AvisoDeEventoSalvo Recado="recado" />
    }
    @if (_mensagem is not null)
    {
        <Aviso Tipo="TipoDeAviso.Erro">@_mensagem</Aviso>
    }
    @* A confirmação no topo, com o foco nela (Rotulo). A chave é a versão com que foi aberta. *@
    @if (_confirmandoExclusao)
    {
        <Aviso @key="_versaoDaConfirmacao" Tipo="TipoDeAviso.Atencao" Rotulo="Confirmar exclusão" Titulo="@($"Confirmar a exclusão de {evento.Titulo}?")">
            <ChildContent>O evento sai da agenda de todos os participantes.</ChildContent>
            <Acoes><button type="button" class="aj-botao aj-botao--secundario" disabled="@_ocupado" @onclick="CancelarExclusao">Cancelar</button><button type="button" class="aj-botao aj-botao--primario" disabled="@_ocupado" @onclick="ExcluirAsync">Confirmar exclusão</button></Acoes>
        </Aviso>
    }

    @* O arranjo do quadro: no desktop, a coluna (Descrição, Participantes) e o painel Detalhes à direita; no tablet e no
       mobile o arranjo some e a ordem é Descrição, Detalhes, Participantes. Detalhes não tem estado e aparece duas vezes,
       cada uma no seu invólucro de visão; Descrição e Participantes, uma vez só. *@
    <div class="aj-lado-a-lado aj-lado-a-lado--ficha">
        <div class="aj-lado-a-lado__lista aj-pilha">
            @if (!string.IsNullOrWhiteSpace(evento.Descricao))
            {
                <section class="aj-painel">
                    <h2>Descrição</h2>
                    <p class="aj-texto-livre">@evento.Descricao</p>
                </section>
            }
            <div class="aj-tablet-mobile">
                <section class="aj-painel">
                    @ConteudoDosDetalhes(evento)
                </section>
            </div>
            <section class="aj-painel">
                <h2>Participantes</h2>
                <ul class="aj-participantes">
                    @foreach (var p in ParticipantesEmOrdem(evento))
                    {
                        <li class="aj-participante" @key="p.UsuarioId"><span class="@ClasseDoAvatar(p)" aria-hidden="true">@AvatarDePessoa.Sigla(p.Nome ?? "")</span><span class="aj-participante__nome">@NomeDoParticipante(p)</span></li>
                    }
                </ul>
            </section>
        </div>
        <div class="aj-so-desktop">
            <section class="aj-painel aj-lado-a-lado__painel">
                @ConteudoDosDetalhes(evento)
            </section>
        </div>
    </div>

    @* Barra fixa do tablet e do mobile: Excluir (secundário vermelho) e Editar (primário), nessa ordem (o quadro); sem
       nenhuma das duas ações, sem barra (e o conteúdo volta ao respiro de baixo padrão). *@
    @if (TemAcoes(evento.Acoes))
    {
        <div class="aj-tablet-mobile">
            <div class="aj-acoes aj-acoes--fixa">@BarraFixa(evento.Acoes)</div>
        </div>
    }
}

@code {
    [Parameter] public Guid Id { get; set; }

    /// <summary>A visão da agenda de onde se veio (<c>?visao=</c>): volta com ela.</summary>
    [SupplyParameterFromQuery(Name = "visao")] public string? Visao { get; set; }

    /// <summary>A data da agenda de onde se veio (<c>?data=</c>): volta com ela.</summary>
    [SupplyParameterFromQuery(Name = "data")] public string? Data { get; set; }

    private bool _carregado;
    private UsuarioAutenticado? _ator;
    private EventoDetalhe? _detalhe;
    private bool _naoEncontrado;
    private string? _erro;
    private Recado? _recado;
    private Guid? _idCarregado;
    private bool _confirmandoExclusao;
    // A versão do evento com que a confirmação foi aberta: a exclusão a manda como versão esperada.
    private uint _versaoDaConfirmacao;
    private bool _ocupado;
    private string? _mensagem;

    /// <summary>O período da agenda de onde se veio, só com o que a agenda aceitaria ("" sem origem).</summary>
    private string ConsultaDeOrigem => PeriodoDaAgenda.ConsultaDeOrigem(Visao, Data);

    private string Voltar => $"/agenda{ConsultaDeOrigem}";

    private string Edicao => $"/agenda/{Id}/editar{ConsultaDeOrigem}";

    protected override async Task OnParametersSetAsync()
    {
        if (_idCarregado == Id) return;

        // O recado de outra tela é retirado só na primeira carga; abrir outro evento nesta tela tira o da anterior.
        var primeira = _idCarregado is null;
        _idCarregado = Id;
        _recado = primeira ? Recados.Retirar() : null;
        await CarregarAsync();
    }

    /// <summary>A carga inteira: o perfil e o evento. Também é o "Tentar de novo".</summary>
    private async Task CarregarAsync()
    {
        _carregado = false;
        _detalhe = null;
        _naoEncontrado = false;
        _erro = null;
        _mensagem = null;
        _confirmandoExclusao = false;

        _ator = await Despachante.EnviarAsync(new ObterMeuAcesso());
        if (_ator is not null) await RecarregarAsync();

        _carregado = true;
    }

    /// <summary>
    /// Relê o evento; devolve se a ficha ficou mostrando dados atuais. Acesso negado encerra a tela; não encontrado e
    /// falha tiram a ficha da tela (as ações dependem do que o servidor permite agora).
    /// </summary>
    private async Task<bool> RecarregarAsync()
    {
        var resultado = await Despachante.EnviarAsync(new ObterEvento(Id));
        _naoEncontrado = false;
        _erro = null;

        if (resultado.Erro == TipoErro.AcessoNegado)
        {
            EncerrarAcesso();
            return false;
        }

        if (resultado.Erro == TipoErro.NaoEncontrado)
        {
            _detalhe = null;
            _naoEncontrado = true;
            return false;
        }

        if (!resultado.Sucesso || resultado.Valor is null)
        {
            _detalhe = null;
            _erro = resultado.Mensagem ?? "Não foi possível consultar o evento.";
            return false;
        }

        _detalhe = resultado.Valor;
        return true;
    }

    private void EncerrarAcesso()
    {
        _ator = null;
        _detalhe = null;
        _confirmandoExclusao = false;
        _mensagem = null;
        _recado = null;
    }

    private static bool TemAcoes(AcoesDoEvento acoes) => acoes.Editar || acoes.Excluir;

    /// <summary>As ações do desktop, à direita do título: Excluir (perigo) e Editar (secundário), cada uma pela sua permissão.</summary>
    private RenderFragment AcoesDoDesktop(AcoesDoEvento acoes) => @<text>
        @if (acoes.Excluir)
        {
            <button type="button" class="aj-botao aj-botao--perigo" disabled="@_ocupado" @onclick="PedirExclusao">Excluir</button>
        }
        @if (acoes.Editar)
        {
            <a href="@Edicao" class="aj-botao aj-botao--secundario">Editar</a>
        }
    </text>;

    /// <summary>As ações da barra fixa do tablet e do mobile: Excluir (secundário vermelho) e Editar (primário).</summary>
    private RenderFragment BarraFixa(AcoesDoEvento acoes) => @<text>
        @if (acoes.Excluir)
        {
            <button type="button" class="aj-botao aj-botao--secundario aj-botao--destrutivo" disabled="@_ocupado" @onclick="PedirExclusao">Excluir</button>
        }
        @if (acoes.Editar)
        {
            <a href="@Edicao" class="aj-botao aj-botao--primario">Editar</a>
        }
    </text>;

    private static string Ou(string? valor) => string.IsNullOrWhiteSpace(valor) ? "—" : valor;

    /// <summary>As nove linhas do quadro, na ordem dele (a descrição tem painel próprio).</summary>
    private IReadOnlyList<(string Termo, string Valor)> Detalhes(EventoDetalhe e) =>
    [
        ("Tipo", RotulosDeAgenda.Tipo(e.Tipo)),
        ("Início", Fuso.Formatar(e.InicioEm)),
        ("Fim", Fuso.Formatar(e.FimEm)),
        ("Duração", RotulosDeAgenda.Duracao(e.DuracaoEstimadaMinutos)),
        ("Local", Ou(e.Local)),
        ("Cliente", e.ClienteId is null ? "—" : e.ClienteNome ?? "Cliente indisponível"),
        ("Processo", e.ProcessoId is null ? "—" : e.ProcessoTitulo ?? "Processo indisponível"),
        ("Atividade", e.AtividadeId is null ? "—" : RotulosDeAgenda.Atividade(e.AtividadeTitulo, e.AtividadeExcluida)),
        ("Criado por", e.CriadorNome ?? "Usuário não encontrado"),
    ];

    private RenderFragment ConteudoDosDetalhes(EventoDetalhe evento) => @<text>
        <h2>Detalhes</h2>
        <dl class="aj-detalhes">
            @foreach (var (termo, valor) in Detalhes(evento))
            {
                <div><dt>@termo</dt><dd>@valor</dd></div>
            }
        </dl>
    </text>;

    private bool EhVoce(ParticipanteDetalhe p) => p.UsuarioId == _ator?.Id;

    /// <summary>O próprio usuário primeiro (o quadro); os outros na ordem do servidor, por nome.</summary>
    private IEnumerable<ParticipanteDetalhe> ParticipantesEmOrdem(EventoDetalhe evento) =>
        evento.Participantes.Where(EhVoce).Concat(evento.Participantes.Where(p => !EhVoce(p)));

    private string NomeDoParticipante(ParticipanteDetalhe p) =>
        p.Nome is null ? "Usuário não encontrado"
        : EhVoce(p) ? $"{p.Nome} (você)"
        : p.Ativo ? p.Nome
        : $"{p.Nome} (inativo)";

    // O próprio usuário tem o avatar dourado (o .aj-avatar sem modificador, como o do menu); os outros, a cor da regra.
    private string ClasseDoAvatar(ParticipanteDetalhe p) =>
        EhVoce(p) ? "aj-avatar" : $"aj-avatar {AvatarDePessoa.Classe(p.Nome ?? "")}";

    // Métodos, e não lambdas de atribuição, nos @onclick: a confirmação tem regras.
    private void PedirExclusao()
    {
        if (_ocupado || _detalhe is null) return;

        _recado = null;
        _mensagem = null;
        _versaoDaConfirmacao = _detalhe.Versao;
        _confirmandoExclusao = true;
    }

    private void CancelarExclusao()
    {
        if (!_ocupado) _confirmandoExclusao = false;
    }

    private async Task ExcluirAsync()
    {
        if (_ocupado) return;

        _ocupado = true;
        _mensagem = null;
        var navegou = false;
        try
        {
            var resultado = await Despachante.EnviarAsync(new ExcluirEvento(Id, _versaoDaConfirmacao));
            if (resultado.Erro == TipoErro.AcessoNegado)
            {
                EncerrarAcesso();
                return;
            }

            if (!resultado.Sucesso)
            {
                // Qualquer recusa traz a ficha como está agora, para os botões acompanharem o servidor. Se o evento
                // sumiu, o "Evento não encontrado." da tela já diz tudo.
                _confirmandoExclusao = false;
                await RecarregarAsync();
                if (_ator is null || _naoEncontrado) return;
                _mensagem = resultado.Mensagem ?? "Não foi possível excluir o evento.";
                return;
            }

            Recados.Deixar("Evento excluído.");
            navegou = true;
            Navegacao.NavigateTo(Voltar);
        }
        finally
        {
            // Depois do sucesso a página vai embora: a confirmação fica travada para um clique tardio não excluir de novo.
            if (!navegou) _ocupado = false;
        }
    }
}
```

Notas para quem implementa:

- A marcação do cabeçalho é a do `CabecalhoDeFicha` sem mudança (quadros `Evento*`, família das fichas da 9D: `quadros-evento.md`, seção 7). Nada da ficha da atividade (`aj-painel--ficha`, `aj-painel--detalhes`, faixa de ações) se aplica.
- O `<li>` do participante fica numa linha só: os testes procuram o avatar e o nome colados.
- `ConsultaDeOrigem` é o único lugar que chama o método de repasse da Tarefa 2. Se ele tiver saído com outro nome (ver "Problemas no contrato": uma propriedade e um método `Consulta` no mesmo `record` não compilam), troque só essa linha.
- `AvatarDePessoa` já está em `Components/Layout` (Tarefa 3), importado pelo `_Imports.razor`; `PeriodoDaAgenda`, `RotulosDeAgenda` e `AvisoDeEventoSalvo` vêm de `Components/Agenda`, também importado.
- Não ponha a tela em `Migradas` de `TelasNoDesignSystemTests`: a Tarefa 10 faz isso e prova a trava. Mas a tela já nasce sem `style`, sem classe fora de `aj-` e sem `<button>` sem `class`.
- Se o Razor reclamar do `@<text>` com `@if` dentro (os 9E usaram o mesmo formato em `AtividadeFicha`), mantenha as marcas que os testes procuram e o `\s*` entre elas resolve o espaço em branco.

- [ ] **Step 5: Build e testes (GREEN)**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~EventoFicha|FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~AutenticacaoWebTests"`
Expected: todos passando (`EventoFichaWebTests` 6, `EventoFichaInterativaTests` 24 casos contando as linhas das theories, e as theories do portal e do anônimo com a rota nova).

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~WebSoPeloDespachante"`
Expected: passando (a tela só chega aos dados pelo `IDespachante`).

- [ ] **Step 6: Commit**

```powershell
git branch --show-current
git add src/AtividadesJuridicas.Web/Components/Pages/EventoFicha.razor tests/AtividadesJuridicas.IntegrationTests/EventoFichaWebTests.cs tests/AtividadesJuridicas.IntegrationTests/EventoFichaInterativaTests.cs tests/AtividadesJuridicas.IntegrationTests/PortalDoClienteWebTests.cs tests/AtividadesJuridicas.IntegrationTests/AutenticacaoWebTests.cs
$msg = @'
feat(web): ficha do evento em tela própria, com a origem da agenda, a exclusão confirmada e o aviso de evento salvo

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
'@
$f = Join-Path $env:TEMP 'mensagem-tarefa-4.txt'
[System.IO.File]::WriteAllText($f, $msg, (New-Object System.Text.UTF8Encoding $false))
git commit -F $f
```

Expected do `git branch --show-current`: `worktree-feat+fase-9f-agenda`.

---

### Task 5: Formulário do evento em tela própria (novo e edição)

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Pages/EventoFormulario.razor`
- Test: `tests/AtividadesJuridicas.IntegrationTests/EventoFormularioWebTests.cs`, `EventoFormularioInterativoTests.cs`, `PortalDoClienteWebTests.cs` (as rotas novas no theory do cliente), `AutenticacaoWebTests.cs` (as rotas novas no theory do anônimo)

**Interfaces:**
- Consumes: `PeriodoDaAgenda.ConsultaDeOrigem` (Tarefa 2), `RotulosDeAgenda.Atividade`, `Tipo` (de hoje), `RotuloDeCampo.Complemento`, `CabecalhoDeDetalhe.IconeDoVoltarNoMobile`/`SubtituloSoNoDesktop`, `AvatarDePessoa`, `Recados` com sobreposições (Tarefa 3), `CargaDoFormulario`, `EstadoDeCarga`.
- Produces: rotas `@page "/agenda/novo"` e `@page "/agenda/{Id:guid}/editar"`; lê `visao` e `data` do endereço; depois de salvar, `Recados.Deixar("Evento salvo.", TipoDeAviso.Sucesso, sobreposicoes)` e navega (novo: `/agenda` com a visão de origem e `data` = o início do evento; edição: `/agenda/{id}{consulta}`).

**Regras:**
- Um `EditForm` (com `FormName`, `id` e validação no `EditContext`, no padrão do `AtividadeFormulario`), os invólucros de visão nos trechos que mudam de controle ou de ordem, todos ligados ao mesmo estado. As buscas por formulários escondidos (`form="busca-de-…"`), sem botão; o Enter busca e não salva; `@key` compostos nos resultados.
- Carga: `ObterMeuAcesso` e `ListarUsuariosParaAgenda`; na edição, também `ObterEvento`, com `Acoes.Editar` decidindo o terceiro estado (sem consultar os usuários quando não pode editar). `AcessoNegado` em qualquer consulta encerra (`Acesso negado.` do `CargaDoFormulario`).
- Novo: data de hoje no fuso, hora `09:00`, duração 60, tipo `Reunião`, quem cria marcado e travado. Edição: os valores do evento; participantes atuais marcados (inclusive inativos, com `(inativo)`, e os sem nome como `Usuário não encontrado`); ninguém travado.
- Envio: `SalvarEvento` com a versão na edição; guarda `_salvando` também nas buscas e no `Escolher`; travado depois do sucesso até a navegação. Falha: aviso de erro no topo com a mensagem do servidor, o digitado fica; `Conflito` e `NaoEncontrado` na edição: o aviso, formulário mantido.

**Testes obrigatórios:** o novo nas três visões (os controles por visão: `select` × ladrilhos, número × `select` de duração com `Outra duração`, caixas × chips com avatar e primeiro nome, a ordem do mobile, o X `Fechar sem salvar`, só `Salvar evento` no mobile); a edição preenchida e a duração fora das quatro opções; os erros por campo (`Campo obrigatório`, o aviso de data e hora, participantes); salvar novo e editado (o pedido enviado, o recado com as sobreposições, a navegação com a origem e a data do evento); clique duplo (Review Focus 5); a Produção que só participa em `/editar` (Review Focus 4), sem consulta de usuários; o Enter na busca de processo que busca e não salva; escolher e remover cada vínculo; o conflito; o cliente do portal em `/agenda/novo` e `/agenda/{id}/editar`.

- [ ] **Step 1: Testes (devem falhar)**

Antes de escrever, releia os três quadros (`NovoEvento.dc.html`, `NovoEventoTablet.dc.html`, `NovoEventoMobile.dc.html`), as seções 1 a 5, 8.1 e 9 de `quadros-evento.md` e o `Components/Agenda/FormularioDeEvento.razor` inteiro (a carga dos usuários, o criador travado, as buscas, a validação e o envio vêm dele, com as mudanças do contrato). O modelo de tela com rota é o `Pages/AtividadeFormulario.razor`.

Dados que os testes usam: a Diretoria é a Diana Diretora (`AtorDaDiretoria` no bUnit, `diana@x.com` na aplicação); a Produção é o Pedro Produção. As siglas saem do `AvatarDePessoa.Sigla` (`Diana Diretora` → `DD`, `Pedro Produção` → `PP`, `Transportes Litoral Ltda.` → `TL`, sem nome → `?`) e a cor dos outros do `AvatarDePessoa.Classe`; o próprio usuário é o `.aj-avatar` sem modificador (dourado).

**`tests/AtividadesJuridicas.IntegrationTests/EventoFormularioWebTests.cs`** (novo; a página inteira pela aplicação, pré-renderizada):

```csharp
using System.Globalization;
using System.Net;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class EventoFormularioWebTests(BancoFixture banco) : IAsyncLifetime
{
    // A agenda de onde se veio, como expressão regular (o "?" escapado; o "&" já decodificado).
    private const string Origem = "/agenda\\?visao=dia&data=2026-10-08";

    private readonly AplicacaoDeTeste _app = new(banco);
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");
    private Guid _admin, _pedro, _audiencia;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();
        var area = await _app.ExecutarAsync(_admin, new SalvarAreaAtuacao(null, "Trabalhista"));
        area.Sucesso.Should().BeTrue(area.Mensagem);
        var litoral = await _app.ExecutarAsync(_admin, new SalvarClientePessoaJuridica(
            null, new DadosDoClienteEntrada("Transportes Litoral Ltda.", null, null, new EnderecoEntrada(Uf: "AL"), null),
            "11.222.333/0001-81", "Litoral Transportes", null, null, null));
        litoral.Sucesso.Should().BeTrue(litoral.Mensagem);
        var processo = await _app.ExecutarAsync(_admin, new SalvarProcesso(
            null, new DadosDoProcessoEntrada(null, "Litoral — reclamação trabalhista", area.Valor, null, null, null, null, null, null),
            [new ClienteDoProcessoEntrada(litoral.Valor, PoloProcessual.Ativo)]));
        processo.Sucesso.Should().BeTrue(processo.Mensagem);
        var minuta = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Minuta de acordo trabalhista", "Redigir a minuta.", new DateOnly(2026, 10, 9), litoral.Valor, processo.Valor, null, 2));
        minuta.Sucesso.Should().BeTrue(minuta.Mensagem);

        // A Diretoria cria; o Pedro só participa. 45 minutos: fora das quatro durações do tablet e do mobile.
        var audiencia = await _app.ExecutarAsync(_admin, new SalvarEvento(
            null, "Audiência de conciliação", "Levar os documentos.", "Fórum Cível, sala 4", TipoEvento.Audiencia,
            _fuso.ParaUtc(new DateTime(2026, 10, 7, 9, 0, 0)), 45, litoral.Valor, processo.Valor, minuta.Valor, [_pedro]));
        audiencia.Sucesso.Should().BeTrue(audiencia.Mensagem);
        _audiencia = audiencia.Valor!.Id;
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> PaginaAsync(string rota) =>
        WebUtility.HtmlDecode(await (await _app.ClienteAutenticadoAsync()).GetStringAsync(rota));

    [Fact]
    public async Task Novo_evento_traz_os_controles_de_cada_visao_com_quem_cria_marcado_e_travado()
    {
        var html = await PaginaAsync("/agenda/novo?visao=dia&data=2026-10-08");
        var hoje = DateOnly.FromDateTime(_fuso.ParaLocal(DateTime.UtcNow)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var avatarDoPedro = $"aj-avatar {AvatarDePessoa.Classe("Pedro Produção")}";

        // Cabeçalho: o voltar leva à agenda de onde se veio; o subtítulo só no desktop; no mobile, o X "Fechar sem salvar".
        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Novo evento</h1>")
            .And.MatchRegex("<(div|span) class=\"aj-so-desktop\">\\s*<p class=\"aj-subtitulo\">Preencha os dados e escolha quem participa.</p>")
            .And.MatchRegex($"(?s)<a href=\"{Origem}\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Agenda</a>")
            .And.MatchRegex(
                $"(?s)<header class=\"aj-barra-superior aj-barra-superior--voltar\">\\s*<a href=\"{Origem}\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Fechar sem salvar\">" +
                "<svg[^>]*><path d=\"M6 6l12 12\"></path><path d=\"M18 6L6 18\"></path></svg></a>\\s*<h1>Novo evento</h1>")
            .And.MatchRegex("<form[^>]*id=\"formulario-do-evento\"[^>]*class=\"aj-lado-a-lado aj-lado-a-lado--painel-medio\"")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-painel--principal aj-painel--solto-no-mobile aj-lado-a-lado__lista\">\\s*<div class=\"aj-desktop-tablet\"><h2>Evento</h2></div>")
            .And.MatchRegex("<label class=\"aj-rotulo\">Local<input[^>]*maxlength=\"200\"")
            .And.Contain("<div class=\"aj-desktop-tablet\"><p class=\"aj-ajuda\">Quem cria o evento sempre participa.</p></div>")
            .And.Contain("<form id=\"busca-de-cliente\" hidden").And.Contain("<form id=\"busca-de-processo\" hidden")
            .And.Contain("<form id=\"busca-de-atividade\" hidden");

        // Vínculos (as três visões): o nome visível de cada um e a busca; o "(opcional)" do processo só no tablet e no mobile.
        html.Should().MatchRegex(
                "(?s)<div class=\"aj-desktop-tablet\"><h2>Vínculos</h2></div>\\s*<div class=\"aj-vinculo aj-vinculo--rotulado\" role=\"group\" aria-labelledby=\"rotulo-do-cliente\">\\s*" +
                "<span class=\"aj-vinculo__rotulo\" id=\"rotulo-do-cliente\">Cliente</span>\\s*<label class=\"aj-campo-icone\"><svg[^>]*>.*?</svg>" +
                "<span class=\"aj-so-leitor\">Escolher cliente \\(nome, CPF ou CNPJ\\)</span><input type=\"search\" class=\"aj-campo-icone__controle\" form=\"busca-de-cliente\" placeholder=\"Escolher cliente: nome, CPF ou CNPJ\"")
            .And.Contain("<span class=\"aj-vinculo__rotulo\" id=\"rotulo-do-processo\">Processo <span class=\"aj-tablet-mobile\"><small>(opcional)</small></span></span>")
            .And.Contain("<span class=\"aj-vinculo__rotulo\" id=\"rotulo-da-atividade\">Atividade <small>(opcional)</small></span>")
            .And.MatchRegex("<span class=\"aj-so-leitor\">Escolher atividade \\(título\\)</span><input type=\"search\" class=\"aj-campo-icone__controle\" form=\"busca-de-atividade\" placeholder=\"Buscar atividade pelo título\"");
        // Nada escolhido ainda.
        html.Should().NotContain("Remover cliente").And.NotContain("Remover processo").And.NotContain("Remover atividade");

        // Desktop: Título e Tipo (lista, Reunião escolhida) numa linha; Data (hoje), Hora (09:00) e Duração em minutos (60)
        // na outra; a ajuda do fuso; participantes em caixas, quem cria marcado e travado com a nota "(você)".
        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex(
                "(?s)<div class=\"aj-linha-de-campos\">\\s*<label class=\"aj-rotulo aj-linha-de-campos__largo\">\\s*Título\\s*<input[^>]*maxlength=\"200\"[^>]*>\\s*</label>\\s*" +
                "<label class=\"aj-rotulo aj-linha-de-campos__medio\">Tipo<select[^>]*>\\s*<option value=\"Audiencia\">Audiência</option>\\s*" +
                "<option value=\"Reuniao\" selected[^>]*>Reunião</option>\\s*<option value=\"Prazo\">Prazo</option>\\s*<option value=\"Outro\">Outro</option>\\s*</select></label>\\s*</div>")
            .And.MatchRegex(
                "(?s)<div class=\"aj-linha-de-campos\">\\s*<label class=\"aj-rotulo\">\\s*Data\\s*<input(?=[^>]*type=\"date\")(?=[^>]*min=\"2000-01-01\")(?=[^>]*max=\"2100-12-31\")" +
                $"(?=[^>]*value=\"{hoje}\")[^>]*>\\s*</label>\\s*" +
                "<label class=\"aj-rotulo aj-linha-de-campos__curto\">\\s*Hora\\s*<input(?=[^>]*type=\"time\")(?=[^>]*value=\"09:00(:00)?\")[^>]*>\\s*</label>\\s*" +
                "<label class=\"aj-rotulo\">\\s*Duração \\(minutos\\)\\s*<input(?=[^>]*type=\"number\")(?=[^>]*min=\"1\")(?=[^>]*max=\"10080\")(?=[^>]*value=\"60\")[^>]*>\\s*</label>\\s*</div>")
            .And.MatchRegex("<p class=\"aj-ajuda aj-ajuda--com-icone aj-ajuda--colada\"><svg[^>]*><circle cx=\"12\" cy=\"12\" r=\"8.5\"></circle><path d=\"M12 7.5V12l3 2\"></path></svg>Data e hora no horário do escritório.</p>")
            .And.MatchRegex("<label class=\"aj-rotulo\">\\s*Descrição\\s*<small>\\(opcional\\)</small>\\s*<textarea(?=[^>]*rows=\"5\")(?=[^>]*maxlength=\"4000\")(?=[^>]*class=\"aj-campo[ \"])[^>]*>")
            .And.Contain("form=\"busca-de-processo\" placeholder=\"Título, parte contrária ou número CNJ\"")
            .And.MatchRegex(
                "(?s)<div class=\"aj-opcoes\" role=\"group\" aria-labelledby=\"titulo-dos-participantes\">\\s*" +
                "<label class=\"aj-opcao\"><input type=\"checkbox\" class=\"aj-opcao__caixa\" checked disabled[^>]*>Diana Diretora<span class=\"aj-opcao__nota aj-opcao__nota--menor\">\\(você\\)</span></label>\\s*" +
                "<label class=\"aj-opcao\"><input type=\"checkbox\" class=\"aj-opcao__caixa\"(?![^>]*checked)[^>]*>Pedro Produção</label>\\s*</div>")
            .And.MatchRegex(
                $"(?s)<div class=\"aj-acoes aj-acoes--fixa\"><a href=\"{Origem}\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>" +
                "<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-do-evento\"[^>]*><svg[^>]*>.*?</svg>Salvar evento</button></div>");
        desktop.Should().NotContain("aj-escolha").And.NotContain("aj-chip--pessoa");

        // Tablet e mobile: o Título sozinho, o Tipo em ladrilhos (Reunião marcada), Data e Hora; sem o número de minutos
        // enquanto a duração é uma das quatro; a busca de processo com o exemplo curto.
        var tabletEMobile = BlocoDeVisao.De(html, "aj-tablet-mobile");
        tabletEMobile.Should().MatchRegex("<label class=\"aj-rotulo\">\\s*Título\\s*<input[^>]*maxlength=\"200\"")
            .And.MatchRegex(
                "(?s)<fieldset class=\"aj-conjunto\"><legend class=\"aj-rotulo\">Tipo</legend><div class=\"aj-escolha\">" +
                "<label class=\"aj-escolha__opcao\"><input type=\"radio\" class=\"aj-opcao__caixa\" name=\"tipo-do-evento\" value=\"Audiencia\"(?![^>]*checked)[^>]*>Audiência</label>" +
                "<label class=\"aj-escolha__opcao\"><input type=\"radio\" class=\"aj-opcao__caixa\" name=\"tipo-do-evento\" value=\"Reuniao\" checked[^>]*>Reunião</label>" +
                "<label class=\"aj-escolha__opcao\"><input[^>]*value=\"Prazo\"(?![^>]*checked)[^>]*>Prazo</label>" +
                "<label class=\"aj-escolha__opcao\"><input[^>]*value=\"Outro\"(?![^>]*checked)[^>]*>Outro</label></div></fieldset>")
            .And.MatchRegex($"(?s)<label class=\"aj-rotulo\">\\s*Data\\s*<input(?=[^>]*type=\"date\")(?=[^>]*value=\"{hoje}\")[^>]*>")
            .And.MatchRegex("(?s)<label class=\"aj-rotulo\">\\s*Hora\\s*<input(?=[^>]*type=\"time\")[^>]*>")
            .And.Contain("form=\"busca-de-processo\" placeholder=\"Título, parte ou número CNJ\"");
        tabletEMobile.Should().NotContain("Duração (minutos)").And.NotContain("Data e hora no horário do escritório.");

        // Tablet: Duração em lista ("1h30"); a descrição de 3 linhas no painel; chips com avatar e o nome inteiro (o "você"
        // dourado e travado); Cancelar secundário.
        var tablet = BlocoDeVisao.De(html, "aj-so-tablet");
        tablet.Should().MatchRegex(
                "(?s)<label class=\"aj-rotulo\">Duração<select class=\"aj-campo\"[^>]*>\\s*<option value=\"30\">30 minutos</option>\\s*<option value=\"60\" selected[^>]*>1 hora</option>\\s*" +
                "<option value=\"90\">1h30</option>\\s*<option value=\"120\">2 horas</option>\\s*<option value=\"outra\">Outra duração</option>\\s*</select></label>")
            .And.MatchRegex("<label class=\"aj-rotulo\">\\s*Descrição\\s*<small>\\(opcional\\)</small>\\s*<textarea(?=[^>]*rows=\"3\")[^>]*>")
            .And.MatchRegex(
                "(?s)<div class=\"aj-opcoes\" role=\"group\" aria-labelledby=\"titulo-dos-participantes\">\\s*" +
                "<label class=\"aj-chip aj-chip--pessoa\"><input type=\"checkbox\" class=\"aj-opcao__caixa\" checked disabled[^>]*><span class=\"aj-avatar\" aria-hidden=\"true\">DD</span>Diana Diretora \\(você\\)</label>\\s*" +
                $"<label class=\"aj-chip aj-chip--pessoa\"><input type=\"checkbox\" class=\"aj-opcao__caixa\"(?![^>]*checked)[^>]*><span class=\"{avatarDoPedro}\" aria-hidden=\"true\">PP</span>Pedro Produção</label>\\s*</div>")
            .And.MatchRegex(
                $"(?s)<div class=\"aj-acoes aj-acoes--fixa\"><a href=\"{Origem}\" class=\"aj-botao aj-botao--secundario\">Cancelar</a>" +
                "<button type=\"submit\"[^>]*form=\"formulario-do-evento\"[^>]*><svg[^>]*>.*?</svg>Salvar evento</button></div>");

        // Mobile: Duração em lista na linha inteira ("1 hora e 30 minutos"); participantes em chips com o primeiro nome
        // ("Você" para quem cria); a descrição depois deles, como último campo; só "Salvar evento" na barra.
        var mobile = BlocoDeVisao.De(html, "aj-so-mobile");
        mobile.Should().MatchRegex(
            "(?s)<label class=\"aj-rotulo aj-linha-de-campos__inteiro-no-mobile\">Duração<select class=\"aj-campo\"[^>]*>.*?<option value=\"60\" selected[^>]*>1 hora</option>\\s*" +
            "<option value=\"90\">1 hora e 30 minutos</option>.*?</select></label>" +
            ".*?<fieldset class=\"aj-conjunto\"><legend class=\"aj-rotulo\">Participantes</legend><div class=\"aj-opcoes\">\\s*" +
            "<label class=\"aj-chip aj-chip--pessoa\"><input type=\"checkbox\" class=\"aj-opcao__caixa\" checked disabled[^>]*><span class=\"aj-avatar\" aria-hidden=\"true\">DD</span>Você</label>\\s*" +
            $"<label class=\"aj-chip aj-chip--pessoa\"><input[^>]*><span class=\"{avatarDoPedro}\" aria-hidden=\"true\">PP</span>Pedro</label>\\s*</div></fieldset>" +
            ".*?<label class=\"aj-rotulo\">\\s*Descrição\\s*<small>\\(opcional\\)</small>\\s*<textarea(?=[^>]*rows=\"3\")[^>]*>" +
            ".*?<div class=\"aj-acoes aj-acoes--fixa\"><button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-do-evento\"[^>]*><svg[^>]*>.*?</svg>Salvar evento</button></div>");
        mobile.Should().NotContain("Cancelar").And.NotContain("Quem cria o evento sempre participa.");
    }

    [Fact]
    public async Task Edicao_vem_preenchida_com_os_vinculos_escolhidos_e_a_duracao_fora_das_opcoes_em_outra_duracao()
    {
        var html = await PaginaAsync($"/agenda/{_audiencia}/editar?visao=semana&data=2026-10-05");
        var ficha = $"/agenda/{_audiencia}\\?visao=semana&data=2026-10-05";

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Editar evento</h1>")
            .And.MatchRegex($"(?s)<a href=\"{ficha}\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Audiência de conciliação</a>")
            .And.MatchRegex($"<a href=\"{ficha}\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Fechar sem salvar\">")
            .And.MatchRegex("<input[^>]*value=\"Audiência de conciliação\"")
            .And.MatchRegex("<input(?=[^>]*type=\"date\")(?=[^>]*value=\"2026-10-07\")[^>]*>")
            .And.MatchRegex("<input(?=[^>]*type=\"time\")(?=[^>]*value=\"09:00(:00)?\")[^>]*>")
            .And.MatchRegex("<input[^>]*value=\"Fórum Cível, sala 4\"")
            .And.Contain("Levar os documentos.")
            .And.MatchRegex(
                "(?s)<div class=\"aj-vinculo__escolhido aj-vinculo__escolhido--com-avatar\"><span class=\"aj-vinculo__avatar\" aria-hidden=\"true\">TL</span>" +
                "<strong class=\"aj-vinculo__nome\">Transportes Litoral Ltda.</strong><button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Remover cliente\"[^>]*><svg")
            .And.MatchRegex("<div class=\"aj-vinculo__escolhido aj-vinculo__escolhido--com-avatar\"><strong class=\"aj-vinculo__nome\">Litoral — reclamação trabalhista</strong><button[^>]*aria-label=\"Remover processo\"")
            .And.MatchRegex("<div class=\"aj-vinculo__escolhido aj-vinculo__escolhido--com-avatar\"><strong class=\"aj-vinculo__nome\">Minuta de acordo trabalhista</strong><button[^>]*aria-label=\"Remover atividade\"");
        // Escolhidos, o processo e a atividade não têm busca; com cliente, nada de "Escolher cliente"; na edição, sem a
        // ajuda de quem cria (ele pode sair do evento).
        html.Should().NotContain("Escolher processo (título").And.NotContain("Escolher atividade (título)")
            .And.NotContain("Escolher cliente (nome, CPF ou CNPJ)").And.NotContain("Quem cria o evento sempre participa.");

        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex("<option value=\"Audiencia\" selected[^>]*>Audiência</option>")
            .And.MatchRegex("Duração \\(minutos\\)\\s*<input(?=[^>]*type=\"number\")(?=[^>]*value=\"45\")[^>]*>")
            .And.Contain("<span class=\"aj-so-leitor\">Trocar cliente</span><input type=\"search\" class=\"aj-campo-icone__controle\" form=\"busca-de-cliente\" placeholder=\"Trocar cliente: nome, CPF ou CNPJ\"")
            // Na edição ninguém fica travado: quem criou aparece marcado e pode ser desmarcado.
            .And.MatchRegex("<input type=\"checkbox\" class=\"aj-opcao__caixa\" checked(?![^>]*disabled)[^>]*>Diana Diretora<span")
            .And.MatchRegex("<input type=\"checkbox\" class=\"aj-opcao__caixa\" checked(?![^>]*disabled)[^>]*>Pedro Produção</label>")
            .And.MatchRegex($"<a href=\"{ficha}\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>");
        var tabletEMobile = BlocoDeVisao.De(html, "aj-tablet-mobile");
        tabletEMobile.Should().MatchRegex("value=\"Audiencia\" checked[^>]*>Audiência</label>")
            .And.MatchRegex("<label class=\"aj-rotulo aj-linha-de-campos__inteiro-no-mobile\">\\s*Duração \\(minutos\\)\\s*<input(?=[^>]*type=\"number\")(?=[^>]*value=\"45\")[^>]*>");
        tabletEMobile.Should().NotContain("Trocar cliente");
        BlocoDeVisao.De(html, "aj-so-tablet").Should().MatchRegex("<option value=\"outra\" selected[^>]*>Outra duração</option>");
        BlocoDeVisao.De(html, "aj-so-mobile").Should().MatchRegex("<option value=\"outra\" selected[^>]*>Outra duração</option>");
    }

    [Fact]
    public async Task Edicao_de_evento_que_nao_existe_mostra_nao_encontrado_sem_formulario()
    {
        var id = Guid.NewGuid();

        var html = await PaginaAsync($"/agenda/{id}/editar");

        html.Should().Contain("Evento não encontrado.")
            .And.MatchRegex("<h1 class=\"aj-titulo\">Editar evento</h1>")
            .And.MatchRegex($"(?s)<a href=\"/agenda/{id}\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Evento</a>");
        html.Should().NotContain("formulario-do-evento").And.NotContain("Salvar evento")
            .And.NotContain("Preencha os dados e escolha quem participa.");
    }

    [Fact]
    public async Task Producao_que_so_participa_ve_o_aviso_sem_formulario_e_a_que_criou_ve_o_formulario()
    {
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);
        var proprio = await _app.ExecutarAsync(_pedro, new SalvarEvento(
            null, "Reunião com o cliente", null, null, TipoEvento.Reuniao, _fuso.ParaUtc(new DateTime(2026, 10, 8, 15, 0, 0)), 60,
            null, null, null, [_pedro]));
        proprio.Sucesso.Should().BeTrue(proprio.Mensagem);

        var soParticipa = WebUtility.HtmlDecode(await doPedro.GetStringAsync($"/agenda/{_audiencia}/editar"));
        var criou = WebUtility.HtmlDecode(await doPedro.GetStringAsync($"/agenda/{proprio.Valor!.Id}/editar"));

        // Review Focus 4: o terceiro estado, com o voltar para a ficha (o título do evento, que ele vê).
        soParticipa.Should().MatchRegex("(?s)<p class=\"aj-aviso aj-aviso--atencao\" role=\"status\"><svg[^>]*>.*?</svg><span>Só a Diretoria e quem criou o evento podem editá-lo.</span></p>")
            .And.MatchRegex($"(?s)<a href=\"/agenda/{_audiencia}\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Audiência de conciliação</a>")
            .And.MatchRegex("<h1 class=\"aj-titulo\">Editar evento</h1>");
        soParticipa.Should().NotContain("formulario-do-evento").And.NotContain("Salvar evento")
            .And.NotContain("Preencha os dados e escolha quem participa.");
        criou.Should().Contain("formulario-do-evento").And.Contain("Salvar evento").And.MatchRegex("<input[^>]*value=\"Reunião com o cliente\"");
        criou.Should().NotContain("Só a Diretoria e quem criou o evento podem editá-lo.");
    }
}
```

**`tests/AtividadesJuridicas.IntegrationTests/EventoFormularioInterativoTests.cs`** (novo, bUnit; o `NavigationManager` do bUnit registra o destino, e um `NavigateTo` antes de desenhar põe a origem no endereço, como nos testes da 9E):

```csharp
using AngleSharp.Dom;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

public class EventoFormularioInterativoTests : TelaInterativa
{
    private static readonly Guid EventoId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000f2");
    private static readonly Guid Rafael = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d005");
    private static readonly Guid Iara = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d006");
    private static readonly Guid Sumido = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d007");
    private static readonly Guid ClienteId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c1");
    private static readonly Guid ProcessoId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b1");
    private static readonly Guid AtividadeId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a1");
    private static readonly FusoDoEscritorio Fuso = new("America/Sao_Paulo");

    // A equipe ativa que o formulário oferece (ListarUsuariosParaAgenda).
    private static readonly IReadOnlyList<UsuarioDaAgenda> Equipe =
        [new(AtorDaDiretoria.Id, "Diana Diretora"), new(AtorDaProducao.Id, "Pedro Produção"), new(Rafael, "Rafael Teles")];

    private static readonly ClienteResumo Litoral = new(
        ClienteId, TipoCliente.PessoaJuridica, "Transportes Litoral Ltda.", "11.222.333/0001-81", null, null, null, true);

    private static readonly ProcessoResumo ProcessoEncontrado = new(
        ProcessoId, null, "Litoral — reclamação trabalhista", "Trabalhista", StatusProcesso.Ativo, ["Transportes Litoral Ltda."], null);

    private static readonly AtividadeResumo Minuta = new(
        AtividadeId, "Minuta de acordo trabalhista", StatusAtividade.Criada, new DateOnly(2026, 10, 9), false,
        "Transportes Litoral Ltda.", "Litoral — reclamação trabalhista", "Trabalhista", ["Pedro Produção"], null, 2);

    // Audiência de 45 minutos (fora das quatro durações), com os três vínculos; participam a diretora (quem criou), a
    // Iara, inativa, e um usuário que não é mais encontrado.
    private static EventoDetalhe Audiencia(uint versao = 7, bool gerencia = true) => new(
        EventoId, "Audiência de conciliação", "Levar os documentos.", "Fórum Cível, sala 4", TipoEvento.Audiencia,
        Fuso.ParaUtc(new DateTime(2026, 10, 7, 9, 0, 0)), 45, Fuso.ParaUtc(new DateTime(2026, 10, 7, 9, 45, 0)),
        ClienteId, "Transportes Litoral Ltda.", ProcessoId, "Litoral — reclamação trabalhista", AtividadeId, "Minuta de acordo trabalhista",
        AtorDaDiretoria.Id, "Diana Diretora",
        [new ParticipanteDetalhe(AtorDaDiretoria.Id, "Diana Diretora", true), new ParticipanteDetalhe(Iara, "Iara Inativa", false),
         new ParticipanteDetalhe(Sumido, null, false)],
        new AcoesDoEvento(gerencia, gerencia), versao);

    // A Diretoria e as respostas de que o formulário precisa: a equipe, as três buscas e o salvar.
    private DespachanteCombinado Respostas() => Despachante
        .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
        .Responde<ListarUsuariosParaAgenda, Resultado<IReadOnlyList<UsuarioDaAgenda>>>(Resultado<IReadOnlyList<UsuarioDaAgenda>>.Ok(Equipe))
        .Responde<ListarClientes, Resultado<PaginaDeClientes>>(Resultado<PaginaDeClientes>.Ok(new([Litoral], 1, 1, 20, 1, 0)))
        .Responde<ListarProcessos, Resultado<PaginaDeProcessos>>(Resultado<PaginaDeProcessos>.Ok(new([ProcessoEncontrado], 1, 1, 20, 1)))
        .Responde<ListarAtividades, Resultado<PaginaDeAtividades>>(Resultado<PaginaDeAtividades>.Ok(new([Minuta], 1, 1, 20, 1, 0)))
        .Responde<SalvarEvento, Resultado<EventoSalvo>>(Resultado<EventoSalvo>.Ok(new EventoSalvo(EventoId, [])));

    private IRenderedComponent<EventoFormulario> Novo()
    {
        Respostas();
        return Desenhar<EventoFormulario>();
    }

    private IRenderedComponent<EventoFormulario> Edicao(uint versao = 7)
    {
        Respostas().Responde<ObterEvento, Resultado<EventoDetalhe>>(Resultado<EventoDetalhe>.Ok(Audiencia(versao)));
        return Desenhar<EventoFormulario>(p => p.Add(x => x.Id, (Guid?)EventoId));
    }

    // O primeiro rótulo com o texto: o do desktop, quando o campo se repete por visão (as cópias têm o mesmo valor).
    private static IElement Rotulo(IRenderedComponent<EventoFormulario> tela, string texto) =>
        tela.FindAll("label.aj-rotulo").First(l => l.TextContent.TrimStart().StartsWith(texto));

    private static void Digitar(IRenderedComponent<EventoFormulario> tela, string rotulo, string valor) =>
        Rotulo(tela, rotulo).QuerySelector("input, textarea")!.Change(valor);

    private static void Enviar(IRenderedComponent<EventoFormulario> tela) => tela.Find("form#formulario-do-evento").Submit();

    // O texto do chip sem a sigla do avatar: só os nós de texto do próprio rótulo.
    private static string Nome(IElement rotulo) => string.Concat(rotulo.ChildNodes.OfType<IText>().Select(t => t.Text)).Trim();

    private static IElement OpcaoDoDesktop(IRenderedComponent<EventoFormulario> tela, string nome) =>
        tela.FindAll("div.aj-so-desktop label.aj-opcao").Single(l => Nome(l) == nome);

    private static IElement ChipDoTablet(IRenderedComponent<EventoFormulario> tela, string nome) =>
        tela.FindAll("div.aj-so-tablet label.aj-chip--pessoa").Single(l => Nome(l) == nome);

    private static IElement Ladrilho(IRenderedComponent<EventoFormulario> tela, string tipo) =>
        tela.FindAll("label.aj-escolha__opcao").Single(l => l.TextContent == tipo).QuerySelector("input")!;

    // O número de minutos de "Outra duração" (no tablet e no mobile), quando aparece.
    private static IReadOnlyList<IElement> OutraDuracao(IRenderedComponent<EventoFormulario> tela) =>
        [.. tela.FindAll("div.aj-tablet-mobile label.aj-rotulo").Where(l => l.TextContent.TrimStart().StartsWith("Duração (minutos)"))];

    private static void Procurar(IRenderedComponent<EventoFormulario> tela, string busca, string texto)
    {
        tela.Find($"input[form={busca}]").Input(texto);
        tela.Find($"form#{busca}").Submit();
    }

    private string Endereco => Services.GetRequiredService<NavigationManager>().Uri;

    private SalvarEvento Enviado => Despachante.Recebidas.OfType<SalvarEvento>().Should().ContainSingle().Subject;

    private static string Hoje => DateOnly.FromDateTime(Fuso.ParaLocal(DateTime.UtcNow)).ToString("yyyy-MM-dd");

    [Fact]
    public void Enter_na_busca_de_processo_mostra_os_resultados_e_nao_salva()
    {
        var tela = Novo();

        Procurar(tela, "busca-de-processo", "litoral");

        Despachante.Recebidas.OfType<ListarProcessos>().Should().ContainSingle().Which.Busca.Should().Be("litoral");
        tela.Find("ul.aj-resultados .aj-resultados__titulo").TextContent.Should().Be("Litoral — reclamação trabalhista");
        tela.Find("ul.aj-resultados .aj-resultados__meta").TextContent.Should().Be("Trabalhista · Transportes Litoral Ltda.");
        tela.Find("ul.aj-resultados .aj-resultados__acao").TextContent.Should().Be("Escolher");
        Despachante.Recebidas.OfType<SalvarEvento>().Should().BeEmpty();   // o Enter da busca não envia o evento
    }

    [Fact]
    public void Escolher_o_cliente_mostra_o_escolhido_com_avatar_e_a_troca_so_no_desktop_e_remover_volta_a_busca()
    {
        var tela = Novo();
        Procurar(tela, "busca-de-cliente", "litoral");
        tela.Find("ul.aj-resultados .aj-resultados__meta").TextContent.Should().Be("CNPJ 11.222.333/0001-81");

        tela.Find("ul.aj-resultados button.aj-resultados__item").Click();

        var escolhido = tela.Find("div.aj-vinculo__escolhido");
        escolhido.ClassList.Should().Contain("aj-vinculo__escolhido--com-avatar");
        escolhido.QuerySelector(".aj-vinculo__avatar")!.TextContent.Should().Be("TL");
        escolhido.QuerySelector(".aj-vinculo__nome")!.TextContent.Should().Be("Transportes Litoral Ltda.");
        tela.FindAll("ul.aj-resultados").Should().BeEmpty();
        // Com cliente, a busca é para trocá-lo, e só no desktop.
        tela.FindAll("input[form=busca-de-cliente]").Should().ContainSingle()
            .Which.GetAttribute("placeholder").Should().Be("Trocar cliente: nome, CPF ou CNPJ");
        tela.Find("div.aj-so-desktop input[form=busca-de-cliente]");

        tela.Find("button[aria-label='Remover cliente']").Click();

        tela.FindAll("div.aj-vinculo__escolhido").Should().BeEmpty();
        tela.Find("input[form=busca-de-cliente]").GetAttribute("placeholder").Should().Be("Escolher cliente: nome, CPF ou CNPJ");

        // Escolhido de novo, o cliente vai no pedido.
        Procurar(tela, "busca-de-cliente", "litoral");
        tela.Find("ul.aj-resultados button.aj-resultados__item").Click();
        Digitar(tela, "Título", "Reunião com o cliente");
        Enviar(tela);
        tela.WaitForAssertion(() => Endereco.Should().Contain("/agenda?data="));
        Enviado.ClienteId.Should().Be(ClienteId);
    }

    [Fact]
    public void Escolher_e_remover_processo_e_atividade_e_o_pedido_leva_so_o_que_ficou()
    {
        var tela = Novo();
        Digitar(tela, "Título", "Reunião sobre o acordo");

        Procurar(tela, "busca-de-processo", "litoral");
        tela.Find("ul.aj-resultados button.aj-resultados__item").Click();
        Procurar(tela, "busca-de-atividade", "minuta");
        Despachante.Recebidas.OfType<ListarAtividades>().Should().ContainSingle().Which.Busca.Should().Be("minuta");
        tela.Find("ul.aj-resultados .aj-resultados__titulo").TextContent.Should().Be("Minuta de acordo trabalhista");
        tela.Find("ul.aj-resultados button.aj-resultados__item").Click();

        tela.FindAll("div.aj-vinculo__escolhido .aj-vinculo__nome").Select(n => n.TextContent)
            .Should().Equal("Litoral — reclamação trabalhista", "Minuta de acordo trabalhista");
        tela.FindAll("div.aj-vinculo__escolhido .aj-vinculo__avatar").Should().BeEmpty();   // só o cliente tem avatar
        // Escolhidos, o processo e a atividade não têm busca; o cliente segue sem escolha (os vínculos são independentes).
        tela.FindAll("input[form=busca-de-processo]").Should().BeEmpty();
        tela.FindAll("input[form=busca-de-atividade]").Should().BeEmpty();
        tela.Find("input[form=busca-de-cliente]").GetAttribute("placeholder").Should().Be("Escolher cliente: nome, CPF ou CNPJ");

        tela.Find("button[aria-label='Remover processo']").Click();

        // A busca volta, com o exemplo de cada visão (desktop; tablet e mobile).
        tela.FindAll("input[form=busca-de-processo]").Select(b => b.GetAttribute("placeholder"))
            .Should().Equal("Título, parte contrária ou número CNJ", "Título, parte ou número CNJ");
        Enviar(tela);
        tela.WaitForAssertion(() => Endereco.Should().Contain("/agenda?data="));
        Enviado.ProcessoId.Should().BeNull();
        Enviado.AtividadeId.Should().Be(AtividadeId);
        Enviado.ClienteId.Should().BeNull();
    }

    [Fact]
    public void Salvar_sem_titulo_data_nem_hora_mostra_campo_obrigatorio_e_o_aviso_e_nao_envia()
    {
        var tela = Novo();
        Rotulo(tela, "Data").QuerySelector("input")!.Change("");
        Rotulo(tela, "Hora").QuerySelector("input")!.Change("");

        Enviar(tela);

        tela.Find("p.aj-aviso--erro").TextContent.Should().Be("Informe a data e a hora do evento.");
        // Cada campo aparece duas vezes (desktop; tablet e mobile), as duas em erro, com a frase do quadro.
        foreach (var campo in new[] { "Título", "Data", "Hora" })
            tela.FindAll("label.aj-rotulo--erro").Where(l => l.TextContent.TrimStart().StartsWith(campo)).Should().HaveCount(2)
                .And.OnlyContain(l => l.QuerySelector("span.aj-campo-erro")!.TextContent == "Campo obrigatório");
        tela.FindAll("input[aria-invalid='true']").Should().HaveCount(6);
        Despachante.Recebidas.OfType<SalvarEvento>().Should().BeEmpty();

        Digitar(tela, "Título", "Reunião de alinhamento");

        // Preencher o título revalida o campo: o erro dele sai e os outros ficam.
        tela.FindAll("label.aj-rotulo--erro").Should().NotContain(l => l.TextContent.TrimStart().StartsWith("Título"))
            .And.Contain(l => l.TextContent.TrimStart().StartsWith("Data"));
    }

    [Fact]
    public void Data_fora_dos_anos_aceitos_mostra_o_aviso_e_nao_envia()
    {
        var tela = Novo();
        Digitar(tela, "Título", "Reunião de alinhamento");
        Rotulo(tela, "Data").QuerySelector("input")!.Change("1999-12-31");

        Enviar(tela);

        tela.Find("p.aj-aviso--erro").TextContent.Should().Be("A data deve estar entre os anos 2000 e 2100.");
        Despachante.Recebidas.OfType<SalvarEvento>().Should().BeEmpty();
    }

    [Fact]
    public void Edicao_sem_participantes_mostra_o_erro_no_grupo_e_marcar_alguem_tira_o_erro_e_envia_a_lista()
    {
        var tela = Edicao();
        foreach (var nome in new[] { "Diana Diretora", "Iara Inativa", "Usuário não encontrado" })
            OpcaoDoDesktop(tela, nome).QuerySelector("input")!.Change(false);

        Enviar(tela);

        // O erro nas três marcações (caixas, chips do tablet, chips do mobile); no mobile, a legenda em erro.
        tela.FindAll("span.aj-campo-erro").Where(s => s.TextContent == "Marque ao menos um participante.").Should().HaveCount(3);
        tela.Find("legend.aj-rotulo--erro").TextContent.Should().Be("Participantes");
        Despachante.Recebidas.OfType<SalvarEvento>().Should().BeEmpty();

        ChipDoTablet(tela, "Pedro Produção").QuerySelector("input")!.Change(true);

        tela.Markup.Should().NotContain("Marque ao menos um participante.");
        OpcaoDoDesktop(tela, "Pedro Produção").QuerySelector("input")!.HasAttribute("checked").Should().BeTrue();
        Enviar(tela);
        tela.WaitForAssertion(() => Endereco.Should().EndWith($"/agenda/{EventoId}"));
        Enviado.Participantes.Should().Equal(AtorDaProducao.Id);
    }

    [Fact]
    public void Edicao_abre_com_inativos_e_sumidos_marcados_ninguem_travado_e_a_duracao_em_outra_duracao()
    {
        var tela = Edicao();

        // Desktop: caixas na ordem dos nomes, a nota do "você" e a do inativo; marcados os participantes atuais; nenhum travado.
        tela.FindAll("div.aj-so-desktop label.aj-opcao").Select(l => (
                Nome(l), l.QuerySelector(".aj-opcao__nota")?.TextContent,
                l.QuerySelector("input")!.HasAttribute("checked"), l.QuerySelector("input")!.HasAttribute("disabled")))
            .Should().Equal(
                ("Diana Diretora", "(você)", true, false), ("Iara Inativa", "(inativo)", true, false),
                ("Pedro Produção", (string?)null, false, false), ("Rafael Teles", null, false, false),
                ("Usuário não encontrado", null, true, false));
        // Tablet: chips com a sigla e o nome inteiro; o próprio usuário dourado (o avatar sem modificador).
        var chips = tela.FindAll("div.aj-so-tablet label.aj-chip--pessoa");
        chips.Select(c => (c.QuerySelector(".aj-avatar")!.TextContent, Nome(c))).Should().Equal(
            ("DD", "Diana Diretora (você)"), ("II", "Iara Inativa (inativo)"), ("PP", "Pedro Produção"), ("RT", "Rafael Teles"),
            ("?", "Usuário não encontrado"));
        chips[0].QuerySelector(".aj-avatar")!.ClassName.Should().Be("aj-avatar");
        chips[2].QuerySelector(".aj-avatar")!.ClassName.Should().Be($"aj-avatar {AvatarDePessoa.Classe("Pedro Produção")}");
        // Mobile: o primeiro nome; "Você" para o próprio usuário.
        tela.FindAll("div.aj-so-mobile label.aj-chip--pessoa").Select(Nome)
            .Should().Equal("Você", "Iara (inativo)", "Pedro", "Rafael", "Usuário não encontrado");
        // 45 minutos não é uma das quatro: as listas abrem em "Outra duração", com o número de minutos.
        tela.Find("div.aj-so-tablet select option[selected]").GetAttribute("value").Should().Be("outra");
        tela.Find("div.aj-so-mobile select option[selected]").GetAttribute("value").Should().Be("outra");
        OutraDuracao(tela).Should().ContainSingle().Which.QuerySelector("input")!.GetAttribute("value").Should().Be("45");
        Rotulo(tela, "Duração (minutos)").QuerySelector("input")!.GetAttribute("value").Should().Be("45");
        tela.Markup.Should().Contain("Participantes").And.NotContain("Quem cria o evento sempre participa.");
        tela.FindAll("div.aj-vinculo__escolhido .aj-vinculo__nome").Select(n => n.TextContent).Should().Equal(
            "Transportes Litoral Ltda.", "Litoral — reclamação trabalhista", "Minuta de acordo trabalhista");
    }

    [Fact]
    public void Outra_duracao_mostra_o_numero_de_minutos_e_escolher_uma_das_quatro_o_esconde()
    {
        var tela = Novo();
        OutraDuracao(tela).Should().BeEmpty();

        tela.Find("div.aj-so-tablet select").Change("outra");

        OutraDuracao(tela).Should().ContainSingle().Which.QuerySelector("input")!.GetAttribute("value").Should().Be("60");
        tela.Find("div.aj-so-tablet select option[selected]").GetAttribute("value").Should().Be("outra");
        OutraDuracao(tela)[0].QuerySelector("input")!.Change("45");
        // O número do desktop e a lista do mobile acompanham o mesmo estado.
        Rotulo(tela, "Duração (minutos)").QuerySelector("input")!.GetAttribute("value").Should().Be("45");
        tela.Find("div.aj-so-mobile select option[selected]").GetAttribute("value").Should().Be("outra");

        tela.Find("div.aj-so-mobile select").Change("90");

        OutraDuracao(tela).Should().BeEmpty();
        tela.Find("div.aj-so-tablet select option[selected]").GetAttribute("value").Should().Be("90");
        Digitar(tela, "Título", "Reunião de alinhamento");
        Enviar(tela);
        tela.WaitForAssertion(() => Enviado.DuracaoEstimadaMinutos.Should().Be(90));
    }

    [Fact]
    public void Novo_evento_salvo_envia_o_pedido_e_volta_a_agenda_de_origem_na_data_do_evento_com_o_recado()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/agenda/novo?visao=semana&data=2026-10-08");
        var sobreposicao = new SobreposicaoDeAgenda(
            Rafael, "Rafael Teles", Fuso.ParaUtc(new DateTime(2026, 10, 20, 14, 0, 0)), Fuso.ParaUtc(new DateTime(2026, 10, 20, 15, 0, 0)));
        Respostas().Responde<SalvarEvento, Resultado<EventoSalvo>>(Resultado<EventoSalvo>.Ok(new EventoSalvo(EventoId, [sobreposicao])));
        var tela = Desenhar<EventoFormulario>();
        tela.Find("a.aj-voltar").GetAttribute("href").Should().Be("/agenda?visao=semana&data=2026-10-08");
        tela.FindAll("a.aj-botao").Where(a => a.TextContent == "Cancelar").Should().HaveCount(2)
            .And.OnlyContain(a => a.GetAttribute("href") == "/agenda?visao=semana&data=2026-10-08");

        Digitar(tela, "Título", "Audiência de instrução");
        Ladrilho(tela, "Audiência").Change(true);
        tela.Find("div.aj-so-desktop select option[selected]").TextContent.Should().Be("Audiência");   // a lista do desktop acompanha
        Rotulo(tela, "Data").QuerySelector("input")!.Change("2026-10-20");
        Rotulo(tela, "Hora").QuerySelector("input")!.Change("14:30:00");
        tela.Find("div.aj-so-mobile select").Change("90");
        Digitar(tela, "Local", "Fórum Central");
        ChipDoTablet(tela, "Rafael Teles").QuerySelector("input")!.Change(true);
        Enviar(tela);

        tela.WaitForAssertion(() => Endereco.Should().EndWith("/agenda?visao=semana&data=2026-10-20"));
        var enviado = Enviado;
        enviado.Id.Should().BeNull();
        enviado.Titulo.Should().Be("Audiência de instrução");
        enviado.Tipo.Should().Be(TipoEvento.Audiencia);
        enviado.InicioEm.Should().Be(Fuso.ParaUtc(new DateTime(2026, 10, 20, 14, 30, 0)));
        enviado.DuracaoEstimadaMinutos.Should().Be(90);
        enviado.Local.Should().Be("Fórum Central");
        enviado.ClienteId.Should().BeNull();
        enviado.ProcessoId.Should().BeNull();
        enviado.AtividadeId.Should().BeNull();
        enviado.Participantes.Should().Equal(AtorDaDiretoria.Id, Rafael);   // quem cria, travado, e o marcado, na ordem dos nomes
        enviado.VersaoEsperada.Should().BeNull();
        var recado = Services.GetRequiredService<Recados>().Retirar()!;
        recado.Texto.Should().Be("Evento salvo.");
        recado.Tipo.Should().Be(TipoDeAviso.Sucesso);
        recado.Sobreposicoes.Should().Equal(sobreposicao);
    }

    [Fact]
    public void Novo_evento_aberto_sem_origem_volta_a_agenda_so_com_a_data_do_evento()
    {
        var tela = Novo();
        tela.Find("a.aj-voltar").GetAttribute("href").Should().Be("/agenda");
        Digitar(tela, "Título", "Prazo do recurso");
        Rotulo(tela, "Data").QuerySelector("input")!.Change("2026-11-03");

        Enviar(tela);

        tela.WaitForAssertion(() => Endereco.Should().EndWith("/agenda?data=2026-11-03"));
    }

    [Fact]
    public void Edicao_salva_envia_a_versao_e_volta_para_a_ficha_com_a_origem_e_o_recado()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/agenda/{EventoId}/editar?visao=dia&data=2026-10-07");
        var tela = Edicao(versao: 7);
        var ficha = $"/agenda/{EventoId}?visao=dia&data=2026-10-07";
        tela.Find("a.aj-voltar").GetAttribute("href").Should().Be(ficha);
        tela.Find("a.aj-voltar").TextContent.Should().Be("Audiência de conciliação");
        tela.FindAll("a.aj-botao").Where(a => a.TextContent == "Cancelar").Should().HaveCount(2).And.OnlyContain(a => a.GetAttribute("href") == ficha);

        Digitar(tela, "Título", "Audiência de conciliação remarcada");
        Enviar(tela);

        tela.WaitForAssertion(() => Endereco.Should().EndWith(ficha));
        var enviado = Enviado;
        enviado.Id.Should().Be(EventoId);
        enviado.Titulo.Should().Be("Audiência de conciliação remarcada");
        enviado.Tipo.Should().Be(TipoEvento.Audiencia);
        enviado.InicioEm.Should().Be(Fuso.ParaUtc(new DateTime(2026, 10, 7, 9, 0, 0)));
        enviado.DuracaoEstimadaMinutos.Should().Be(45);
        enviado.Local.Should().Be("Fórum Cível, sala 4");
        enviado.Descricao.Should().Be("Levar os documentos.");
        enviado.ClienteId.Should().Be(ClienteId);
        enviado.ProcessoId.Should().Be(ProcessoId);
        enviado.AtividadeId.Should().Be(AtividadeId);
        // Os participantes atuais, inclusive a inativa e o que não é mais encontrado, na ordem dos nomes.
        enviado.Participantes.Should().Equal(AtorDaDiretoria.Id, Iara, Sumido);
        enviado.VersaoEsperada.Should().Be(7u);
        var recado = Services.GetRequiredService<Recados>().Retirar()!;
        recado.Texto.Should().Be("Evento salvo.");
        recado.Sobreposicoes.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Clique_duplo_em_salvar_evento_envia_uma_vez_e_o_botao_fica_travado_ate_a_navegacao(bool edicao)
    {
        var resposta = new TaskCompletionSource<Resultado<EventoSalvo>>();
        var tela = edicao ? Edicao() : Novo();
        if (!edicao) Digitar(tela, "Título", "Reunião de alinhamento");
        Despachante.RespondeDepois<SalvarEvento, Resultado<EventoSalvo>>(resposta.Task);

        Enviar(tela);
        Enviar(tela);   // o segundo clique, com o primeiro envio em curso
        resposta.SetResult(Resultado<EventoSalvo>.Ok(new EventoSalvo(EventoId, [])));
        var destino = edicao ? $"/agenda/{EventoId}" : $"/agenda?data={Hoje}";
        tela.WaitForAssertion(() => Endereco.Should().EndWith(destino));
        Enviar(tela);   // um clique tardio, com a navegação a caminho

        Despachante.Recebidas.OfType<SalvarEvento>().Should().ContainSingle();
        tela.FindAll("button[form=formulario-do-evento]").Should().HaveCount(3).And.OnlyContain(b => b.HasAttribute("disabled"));
    }

    [Fact]
    public void Conflito_na_edicao_mostra_o_aviso_e_mantem_o_formulario_com_o_digitado()
    {
        var tela = Edicao();
        Despachante.Responde<SalvarEvento, Resultado<EventoSalvo>>(Resultado<EventoSalvo>.Falha(
            TipoErro.Conflito, "O evento foi alterado por outra pessoa. Recarregue e tente novamente."));
        Digitar(tela, "Título", "Audiência remarcada");

        Enviar(tela);

        tela.WaitForAssertion(() => tela.Find("p.aj-aviso--erro").TextContent
            .Should().Be("O evento foi alterado por outra pessoa. Recarregue e tente novamente."));
        Rotulo(tela, "Título").QuerySelector("input")!.GetAttribute("value").Should().Be("Audiência remarcada");
        tela.FindAll("button[form=formulario-do-evento]").Should().HaveCount(3).And.OnlyContain(b => !b.HasAttribute("disabled"));
        Endereco.Should().NotContain("/agenda/");
        Services.GetRequiredService<Recados>().Retirar().Should().BeNull();
    }

    [Fact]
    public void Producao_que_so_participa_ve_o_aviso_sem_formulario_e_sem_consultar_os_usuarios()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaProducao)
            .Responde<ObterEvento, Resultado<EventoDetalhe>>(Resultado<EventoDetalhe>.Ok(Audiencia(gerencia: false)));

        var tela = Desenhar<EventoFormulario>(p => p.Add(x => x.Id, (Guid?)EventoId));

        tela.Find("p.aj-aviso--atencao").TextContent.Should().Be("Só a Diretoria e quem criou o evento podem editá-lo.");
        tela.Find("a.aj-voltar").GetAttribute("href").Should().Be($"/agenda/{EventoId}");
        tela.Find("a.aj-voltar").TextContent.Should().Be("Audiência de conciliação");
        tela.FindAll("form#formulario-do-evento").Should().BeEmpty();
        tela.FindAll("button[form=formulario-do-evento]").Should().BeEmpty();
        // Review Focus 4: sem a consulta dos usuários (nenhuma resposta combinada para ela; se a tela a fizesse, falharia).
        Despachante.Recebidas.Select(r => r.GetType()).Should().Equal(typeof(ObterMeuAcesso), typeof(ObterEvento));
    }

    [Fact]
    public void Falha_ao_consultar_os_usuarios_mostra_tentar_de_novo_e_o_clique_recarrega()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarUsuariosParaAgenda, Resultado<IReadOnlyList<UsuarioDaAgenda>>>(
                Resultado<IReadOnlyList<UsuarioDaAgenda>>.Falha(TipoErro.Validacao, "Falha de teste."));
        var tela = Desenhar<EventoFormulario>();

        tela.Markup.Should().Contain("Falha de teste.");
        tela.FindAll("form#formulario-do-evento").Should().BeEmpty();

        Respostas();
        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.WaitForAssertion(() => tela.Find("form#formulario-do-evento"));
        tela.Markup.Should().NotContain("Falha de teste.");
    }

    [Fact]
    public void Acesso_negado_na_consulta_dos_usuarios_encerra_a_tela()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarUsuariosParaAgenda, Resultado<IReadOnlyList<UsuarioDaAgenda>>>(
                Resultado<IReadOnlyList<UsuarioDaAgenda>>.Falha(TipoErro.AcessoNegado, "Acesso negado."));

        var tela = Desenhar<EventoFormulario>();

        tela.Find("p.aj-aviso--erro").TextContent.Should().Be("Acesso negado.");
        tela.FindAll("form#formulario-do-evento").Should().BeEmpty();
        tela.FindAll("button").Should().NotContain(b => b.TextContent == "Tentar de novo");
    }
}
```

(Os construtores de `ClienteResumo`, `ProcessoResumo`, `AtividadeResumo`, `PaginaDeClientes`, `PaginaDeProcessos`, `PaginaDeAtividades`, `EventoDetalhe`, `ParticipanteDetalhe`, `AcoesDoEvento`, `EventoSalvo` e `SobreposicaoDeAgenda` são os de hoje. `Recado.Sobreposicoes` e o terceiro parâmetro de `Recados.Deixar` vêm da Tarefa 3; `AvatarDePessoa` mora em `Components/Layout` desde a Tarefa 3; `PeriodoDaAgenda` vem da Tarefa 2.)

**`PortalDoClienteWebTests.cs`** (`Cliente_em_pagina_interna_ve_nao_encontrada_e_nenhum_dado`) e **`AutenticacaoWebTests.cs`** (`Anonimo_e_redirecionado_ao_login_em_qualquer_rota`): acrescente as duas rotas novas (no primeiro, logo depois de `[InlineData("/agenda")]`; no segundo, logo depois de `[InlineData("/atividades/0199a3f2-7c1e-7000-8000-000000000001")]`):

```csharp
    [InlineData("/agenda/novo")]
    [InlineData("/agenda/0199a3f2-7c1e-7000-8000-000000000001/editar")]
```

Run: `dotnet build -m:1`
Expected: não compila (`EventoFormulario` não existe). É o RED.

- [ ] **Step 2: A tela**

Crie `src/AtividadesJuridicas.Web/Components/Pages/EventoFormulario.razor`:

```razor
@page "/agenda/novo"
@page "/agenda/{Id:guid}/editar"
@using System.ComponentModel.DataAnnotations
@using System.Globalization
@inject IDespachante Despachante
@inject FusoDoEscritorio Fuso
@inject NavigationManager Navegacao
@inject Recados Recados

<PageTitle>@Titulo</PageTitle>

@* No mobile, o voltar é o X "Fechar sem salvar" (o quadro); o subtítulo é só do desktop. *@
<CabecalhoDeDetalhe Titulo="@Titulo" Subtitulo="@SubtituloDoFormulario" SubtituloSoNoDesktop="true" VoltarPara="@Voltar"
                    Origem="@OrigemDoCabecalho" RotuloDoVoltarNoMobile="Fechar sem salvar" IconeDoVoltarNoMobile="fechar" />

<CargaDoFormulario Estado="_estado" Erro="@_erroDaCarga" TextoNaoEncontrado="Evento não encontrado." TentarDeNovo="CarregarAsync">
    @if (!_podeEditar)
    {
        @* Quem vê o evento mas não o gerencia (o participante que não o criou): o aviso, sem formulário. *@
        <Aviso Tipo="TipoDeAviso.Atencao">Só a Diretoria e quem criou o evento podem editá-lo.</Aviso>
    }
    else
    {
        @if (_mensagem is not null)
        {
            <Aviso Tipo="TipoDeAviso.Erro">@_mensagem</Aviso>
        }

        @* Um formulário só. Os trechos que mudam de controle ou de lugar por visão vão em invólucros de visão e ligam-se ao
           mesmo estado (os campos repetidos mostram o mesmo valor e o mesmo erro). Linhas de campos ficam inteiras dentro
           de um invólucro: a aj-linha-de-campos dá a largura aos filhos diretos, e o invólucro (display: contents) os
           afastaria dela. *@
        <EditForm EditContext="_contexto" OnSubmit="EnviarAsync" FormName="evento" id="formulario-do-evento" class="aj-lado-a-lado aj-lado-a-lado--painel-medio">
            <DataAnnotationsValidator />
            <section class="aj-painel aj-painel--principal aj-painel--solto-no-mobile aj-lado-a-lado__lista">
                <div class="aj-desktop-tablet"><h2>Evento</h2></div>
                @* Desktop: Título e Tipo (lista) numa linha; Data, Hora e Duração em minutos na outra; a ajuda do fuso. *@
                <div class="aj-so-desktop">
                    <div class="aj-linha-de-campos">
                        <RotuloDeCampo Texto="Título" Para="() => _form.Titulo" Classe="aj-linha-de-campos__largo"><InputText @bind-Value="_form.Titulo" maxlength="@Evento.TamanhoMaximoTitulo" class="aj-campo" /></RotuloDeCampo>
                        <label class="aj-rotulo aj-linha-de-campos__medio">Tipo<InputSelect @bind-Value="_form.Tipo" class="aj-campo">@foreach (var tipo in Tipos)
                            {<option value="@tipo">@RotulosDeAgenda.Tipo(tipo)</option>}</InputSelect></label>
                    </div>
                    <div class="aj-linha-de-campos">
                        <RotuloDeCampo Texto="Data" Para="() => _form.Data"><InputDate @bind-Value="_form.Data" min="@DataMinima" max="@DataMaxima" class="aj-campo" /></RotuloDeCampo>
                        <RotuloDeCampo Texto="Hora" Para="() => _form.Hora" Classe="aj-linha-de-campos__curto"><InputDate Type="InputDateType.Time" @bind-Value="_form.Hora" class="aj-campo" /></RotuloDeCampo>
                        <RotuloDeCampo Texto="Duração (minutos)" Para="() => _form.Duracao"><InputNumber @bind-Value="_form.Duracao" min="1" max="@Evento.DuracaoMaximaMinutos" class="aj-campo" /></RotuloDeCampo>
                    </div>
                    <p class="aj-ajuda aj-ajuda--com-icone aj-ajuda--colada"><Icone Nome="auditoria" />Data e hora no horário do escritório.</p>
                </div>
                @* Tablet e mobile: o Título sozinho, o Tipo em ladrilhos e a Duração em lista (o texto da opção de 90 minutos
                   muda entre os dois quadros), com "Outra duração" para os minutos livres (o domínio aceita de 1 a 10080). *@
                <div class="aj-tablet-mobile">
                    <RotuloDeCampo Texto="Título" Para="() => _form.Titulo"><InputText @bind-Value="_form.Titulo" maxlength="@Evento.TamanhoMaximoTitulo" class="aj-campo" /></RotuloDeCampo>
                    <fieldset class="aj-conjunto"><legend class="aj-rotulo">Tipo</legend><div class="aj-escolha">@foreach (var tipo in Tipos)
                        {<label class="aj-escolha__opcao" @key="tipo"><input type="radio" class="aj-opcao__caixa" name="tipo-do-evento" value="@tipo" checked="@(_form.Tipo == tipo)" @onchange="() => EscolherTipo(tipo)" />@RotulosDeAgenda.Tipo(tipo)</label>}</div></fieldset>
                    <div class="aj-linha-de-campos">
                        <RotuloDeCampo Texto="Data" Para="() => _form.Data"><InputDate @bind-Value="_form.Data" min="@DataMinima" max="@DataMaxima" class="aj-campo" /></RotuloDeCampo>
                        <RotuloDeCampo Texto="Hora" Para="() => _form.Hora"><InputDate Type="InputDateType.Time" @bind-Value="_form.Hora" class="aj-campo" /></RotuloDeCampo>
                        <div class="aj-so-tablet">
                            <label class="aj-rotulo">Duração<select class="aj-campo" value="@OpcaoDaDuracao" @onchange="EscolherDuracao"><option value="30">30 minutos</option><option value="60">1 hora</option><option value="90">1h30</option><option value="120">2 horas</option><option value="outra">Outra duração</option></select></label>
                        </div>
                        <div class="aj-so-mobile">
                            <label class="aj-rotulo aj-linha-de-campos__inteiro-no-mobile">Duração<select class="aj-campo" value="@OpcaoDaDuracao" @onchange="EscolherDuracao"><option value="30">30 minutos</option><option value="60">1 hora</option><option value="90">1 hora e 30 minutos</option><option value="120">2 horas</option><option value="outra">Outra duração</option></select></label>
                        </div>
                        @if (OutraDuracao)
                        {
                            <RotuloDeCampo Texto="Duração (minutos)" Para="() => _form.Duracao" Classe="aj-linha-de-campos__inteiro-no-mobile"><InputNumber @bind-Value="_form.Duracao" min="1" max="@Evento.DuracaoMaximaMinutos" class="aj-campo" /></RotuloDeCampo>
                        }
                    </div>
                </div>
                <label class="aj-rotulo">Local<InputText @bind-Value="_form.Local" maxlength="@Evento.TamanhoMaximoLocal" class="aj-campo" /></label>
                @* A descrição fecha o painel no desktop (5 linhas) e no tablet (3); no mobile ela é o último campo (abaixo). *@
                <div class="aj-so-desktop">
                    <RotuloDeCampo Texto="Descrição" Complemento="(opcional)" Para="() => _form.Descricao"><InputTextArea @bind-Value="_form.Descricao" rows="5" maxlength="@Evento.TamanhoMaximoDescricao" class="aj-campo" /></RotuloDeCampo>
                </div>
                <div class="aj-so-tablet">
                    <RotuloDeCampo Texto="Descrição" Complemento="(opcional)" Para="() => _form.Descricao"><InputTextArea @bind-Value="_form.Descricao" rows="3" maxlength="@Evento.TamanhoMaximoDescricao" class="aj-campo" /></RotuloDeCampo>
                </div>
            </section>

            <div class="aj-lado-a-lado__painel aj-pilha">
                <section class="aj-painel aj-painel--folgado aj-painel--solto-no-mobile">
                    <div class="aj-desktop-tablet"><h2>Vínculos</h2></div>
                    @* Cliente, processo e atividade são independentes na tela: o servidor recusa o cliente que não é do processo.
                       O Enter nas buscas envia os formulários escondidos do fim da tela, e não o do evento. *@
                    <div class="aj-vinculo aj-vinculo--rotulado" role="group" aria-labelledby="rotulo-do-cliente">
                        <span class="aj-vinculo__rotulo" id="rotulo-do-cliente">Cliente</span>
                        @if (_clienteId is not null)
                        {
                            <div class="aj-vinculo__escolhido aj-vinculo__escolhido--com-avatar"><span class="aj-vinculo__avatar" aria-hidden="true">@AvatarDePessoa.Sigla(_clienteNome ?? string.Empty)</span><strong class="aj-vinculo__nome">@_clienteNome</strong><button type="button" class="aj-botao aj-botao--fantasma aj-botao--icone" aria-label="Remover cliente" disabled="@_salvando" @onclick="RemoverCliente"><Icone Nome="fechar" /></button></div>
                            @* Com cliente, só o desktop tem a busca para trocá-lo (o quadro); no tablet e no mobile, remover e escolher. *@
                            <div class="aj-so-desktop">
                                <label class="aj-campo-icone"><Icone Nome="buscar" /><span class="aj-so-leitor">Trocar cliente</span><input type="search" class="aj-campo-icone__controle" form="busca-de-cliente" placeholder="Trocar cliente: nome, CPF ou CNPJ" @bind="_buscaDeCliente" @bind:event="oninput" /></label>
                            </div>
                        }
                        else
                        {
                            <label class="aj-campo-icone"><Icone Nome="buscar" /><span class="aj-so-leitor">Escolher cliente (nome, CPF ou CNPJ)</span><input type="search" class="aj-campo-icone__controle" form="busca-de-cliente" placeholder="Escolher cliente: nome, CPF ou CNPJ" @bind="_buscaDeCliente" @bind:event="oninput" /></label>
                        }
                        @if (_clientesEncontrados.Count > 0)
                        {
                            <ul class="aj-resultados">@foreach (var candidato in _clientesEncontrados)
                                {<li @key='("cliente", candidato.Id)'><button type="button" class="aj-resultados__item" disabled="@_salvando" @onclick="() => EscolherCliente(candidato)"><span class="aj-resultados__texto"><span class="aj-resultados__titulo">@candidato.Nome</span><span class="aj-resultados__meta">@TextosDoCliente.Documento(candidato.Tipo, candidato.Documento)</span></span><span class="aj-resultados__acao">Escolher</span></button></li>}</ul>
                        }
                        @if (_nenhumCliente)
                        {
                            <span class="aj-ajuda">Nenhum cliente encontrado.</span>
                        }
                        @if (_clientesTruncados)
                        {
                            <span class="aj-ajuda">Mostrando os primeiros resultados. Refine a busca para ver outros.</span>
                        }
                    </div>

                    <div class="aj-vinculo aj-vinculo--rotulado" role="group" aria-labelledby="rotulo-do-processo">
                        @* O "(opcional)" só nos quadros do tablet e do mobile. O espaço antes do invólucro fica no texto: dentro
                           dele, o compilador do Razor o apararia. *@
                        <span class="aj-vinculo__rotulo" id="rotulo-do-processo">Processo <span class="aj-tablet-mobile"><small>(opcional)</small></span></span>
                        @if (_processoId is not null)
                        {
                            <div class="aj-vinculo__escolhido aj-vinculo__escolhido--com-avatar"><strong class="aj-vinculo__nome">@_processoTitulo</strong><button type="button" class="aj-botao aj-botao--fantasma aj-botao--icone" aria-label="Remover processo" disabled="@_salvando" @onclick="RemoverProcesso"><Icone Nome="fechar" /></button></div>
                        }
                        else
                        {
                            @* O exemplo muda entre os quadros: "parte contrária" no desktop, "parte" no tablet e no mobile. *@
                            <div class="aj-so-desktop">
                                <label class="aj-campo-icone"><Icone Nome="buscar" /><span class="aj-so-leitor">Escolher processo (título, parte contrária ou número CNJ)</span><input type="search" class="aj-campo-icone__controle" form="busca-de-processo" placeholder="Título, parte contrária ou número CNJ" @bind="_buscaDeProcesso" @bind:event="oninput" /></label>
                            </div>
                            <div class="aj-tablet-mobile">
                                <label class="aj-campo-icone"><Icone Nome="buscar" /><span class="aj-so-leitor">Escolher processo (título, parte contrária ou número CNJ)</span><input type="search" class="aj-campo-icone__controle" form="busca-de-processo" placeholder="Título, parte ou número CNJ" @bind="_buscaDeProcesso" @bind:event="oninput" /></label>
                            </div>
                            @if (_processosEncontrados.Count > 0)
                            {
                                <ul class="aj-resultados">@foreach (var candidato in _processosEncontrados)
                                    {<li @key='("processo", candidato.Id)'><button type="button" class="aj-resultados__item" disabled="@_salvando" @onclick="() => EscolherProcesso(candidato)"><span class="aj-resultados__texto"><span class="aj-resultados__titulo">@candidato.Titulo</span><span class="aj-resultados__meta">@MetaDoProcesso(candidato)</span></span><span class="aj-resultados__acao">Escolher</span></button></li>}</ul>
                            }
                            @if (_nenhumProcesso)
                            {
                                <span class="aj-ajuda">Nenhum processo encontrado.</span>
                            }
                            @if (_processosTruncados)
                            {
                                <span class="aj-ajuda">Mostrando os primeiros resultados. Refine a busca para ver outros.</span>
                            }
                        }
                    </div>

                    <div class="aj-vinculo aj-vinculo--rotulado" role="group" aria-labelledby="rotulo-da-atividade">
                        <span class="aj-vinculo__rotulo" id="rotulo-da-atividade">Atividade <small>(opcional)</small></span>
                        @if (_atividadeId is not null)
                        {
                            <div class="aj-vinculo__escolhido aj-vinculo__escolhido--com-avatar"><strong class="aj-vinculo__nome">@RotulosDeAgenda.Atividade(_atividadeTitulo, _atividadeExcluida)</strong><button type="button" class="aj-botao aj-botao--fantasma aj-botao--icone" aria-label="Remover atividade" disabled="@_salvando" @onclick="RemoverAtividade"><Icone Nome="fechar" /></button></div>
                        }
                        else
                        {
                            <label class="aj-campo-icone"><Icone Nome="buscar" /><span class="aj-so-leitor">Escolher atividade (título)</span><input type="search" class="aj-campo-icone__controle" form="busca-de-atividade" placeholder="Buscar atividade pelo título" @bind="_buscaDeAtividade" @bind:event="oninput" /></label>
                            @if (_atividadesEncontradas.Count > 0)
                            {
                                <ul class="aj-resultados">@foreach (var candidata in _atividadesEncontradas)
                                    {<li @key='("atividade", candidata.Id)'><button type="button" class="aj-resultados__item" disabled="@_salvando" @onclick="() => EscolherAtividade(candidata)"><span class="aj-resultados__texto"><span class="aj-resultados__titulo">@candidata.Titulo</span></span><span class="aj-resultados__acao">Escolher</span></button></li>}</ul>
                            }
                            @if (_nenhumaAtividade)
                            {
                                <span class="aj-ajuda">Nenhuma atividade encontrada.</span>
                            }
                            @if (_atividadesTruncadas)
                            {
                                <span class="aj-ajuda">Mostrando os primeiros resultados. Refine a busca para ver outras.</span>
                            }
                        }
                    </div>
                </section>

                <section class="aj-painel aj-painel--folgado aj-painel--solto-no-mobile">
                    <div class="aj-desktop-tablet"><h2 id="titulo-dos-participantes">Participantes</h2></div>
                    @* Três marcações do mesmo conjunto de marcados: caixas no desktop, chips com o nome inteiro no tablet, chips
                       com o primeiro nome no mobile (com a legenda, porque o h2 do painel some). Cada lista tem o seu pai; as
                       chaves ainda assim levam o tipo da linha. *@
                    <div class="aj-so-desktop">
                        <div class="aj-opcoes" role="group" aria-labelledby="titulo-dos-participantes">@foreach (var pessoa in _pessoas)
                            {<label class="aj-opcao" @key='("opcao", pessoa.Id)'><input type="checkbox" class="aj-opcao__caixa" checked="@Marcado(pessoa)" disabled="@Travado(pessoa)" @onchange="e => Marcar(pessoa, e)" />@pessoa.Rotulo@if (NotaDe(pessoa) is { } nota)
                                {<span class="aj-opcao__nota aj-opcao__nota--menor">@nota</span>}</label>}</div>
                        @if (_semParticipantes)
                        {
                            <span class="aj-campo-erro">Marque ao menos um participante.</span>
                        }
                    </div>
                    <div class="aj-so-tablet">
                        <div class="aj-opcoes" role="group" aria-labelledby="titulo-dos-participantes">@foreach (var pessoa in _pessoas)
                            {<label class="aj-chip aj-chip--pessoa" @key='("chip", pessoa.Id)'><input type="checkbox" class="aj-opcao__caixa" checked="@Marcado(pessoa)" disabled="@Travado(pessoa)" @onchange="e => Marcar(pessoa, e)" /><span class="@ClasseDoAvatar(pessoa)" aria-hidden="true">@SiglaDe(pessoa)</span>@NomeNoTablet(pessoa)</label>}</div>
                        @if (_semParticipantes)
                        {
                            <span class="aj-campo-erro">Marque ao menos um participante.</span>
                        }
                    </div>
                    <div class="aj-so-mobile">
                        <fieldset class="aj-conjunto"><legend class="@ClasseDaLegenda">Participantes</legend><div class="aj-opcoes">@foreach (var pessoa in _pessoas)
                            {<label class="aj-chip aj-chip--pessoa" @key='("chip-curto", pessoa.Id)'><input type="checkbox" class="aj-opcao__caixa" checked="@Marcado(pessoa)" disabled="@Travado(pessoa)" @onchange="e => Marcar(pessoa, e)" /><span class="@ClasseDoAvatar(pessoa)" aria-hidden="true">@SiglaDe(pessoa)</span>@NomeNoMobile(pessoa)</label>}</div>@if (_semParticipantes)
                            {<span class="aj-campo-erro">Marque ao menos um participante.</span>}</fieldset>
                    </div>
                    @if (Id is null)
                    {
                        @* No evento novo, quem cria está marcado e travado; na edição ele pode sair, e a frase não valeria. *@
                        <div class="aj-desktop-tablet"><p class="aj-ajuda">Quem cria o evento sempre participa.</p></div>
                    }
                </section>
            </div>

            @* Mobile: sem painéis, a descrição é o último campo (o quadro). *@
            <div class="aj-so-mobile">
                <RotuloDeCampo Texto="Descrição" Complemento="(opcional)" Para="() => _form.Descricao"><InputTextArea @bind-Value="_form.Descricao" rows="3" maxlength="@Evento.TamanhoMaximoDescricao" class="aj-campo" /></RotuloDeCampo>
            </div>
        </EditForm>

        @* As barras de ação ficam fora do formulário e o enviam pelo atributo form. No mobile, só "Salvar evento": o X da
           barra superior é o cancelar. *@
        <div class="aj-so-desktop">
            <div class="aj-acoes aj-acoes--fixa"><a href="@Voltar" class="aj-botao aj-botao--fantasma">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" form="formulario-do-evento" disabled="@_salvando"><Icone Nome="confirmar" />Salvar evento</button></div>
        </div>
        <div class="aj-so-tablet">
            <div class="aj-acoes aj-acoes--fixa"><a href="@Voltar" class="aj-botao aj-botao--secundario">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" form="formulario-do-evento" disabled="@_salvando"><Icone Nome="confirmar" />Salvar evento</button></div>
        </div>
        <div class="aj-so-mobile">
            <div class="aj-acoes aj-acoes--fixa"><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" form="formulario-do-evento" disabled="@_salvando"><Icone Nome="confirmar" />Salvar evento</button></div>
        </div>

        <form id="busca-de-cliente" hidden @onsubmit="ProcurarClientesAsync" @onsubmit:preventDefault></form>
        <form id="busca-de-processo" hidden @onsubmit="ProcurarProcessosAsync" @onsubmit:preventDefault></form>
        <form id="busca-de-atividade" hidden @onsubmit="ProcurarAtividadesAsync" @onsubmit:preventDefault></form>
    }
</CargaDoFormulario>

@code {
    private const string CampoObrigatorio = "Campo obrigatório";

    private static readonly TipoEvento[] Tipos = Enum.GetValues<TipoEvento>();

    // As quatro durações dos quadros do tablet e do mobile; qualquer outra fica em "Outra duração".
    private static readonly int[] DuracoesDosQuadros = [30, 60, 90, 120];

    private static readonly string DataMinima = $"{Evento.AnoMinimoDeInicio}-01-01";
    private static readonly string DataMaxima = $"{Evento.AnoMaximoDeInicio}-12-31";

    [Parameter] public Guid? Id { get; set; }

    /// <summary>A visão da agenda de onde se veio (<c>?visao=</c>): o voltar, o Cancelar e o destino depois de salvar.</summary>
    [SupplyParameterFromQuery(Name = "visao")] public string? VisaoDeOrigem { get; set; }

    /// <summary>A data da agenda de onde se veio (<c>?data=</c>).</summary>
    [SupplyParameterFromQuery(Name = "data")] public string? DataDeOrigem { get; set; }

    private EstadoDeCarga _estado = EstadoDeCarga.Carregando;
    private string? _erroDaCarga;
    private bool _podeEditar = true;
    private bool _salvando;
    private string? _mensagem;

    private Formulario _form = new();
    private EditContext? _contexto;
    private uint? _versao;
    private string? _tituloDoEvento;
    private bool _outraDuracao;
    private bool _semParticipantes;

    // Quem está na tela, e quem fica marcado e travado (só no evento novo: quem cria sempre participa).
    private Guid _euId;
    private Guid? _criadorTravado;

    // Todas as pessoas que o formulário mostra; só as marcadas são enviadas.
    private IReadOnlyList<Pessoa> _pessoas = [];
    private HashSet<Guid> _marcados = [];

    private Guid? _clienteId;
    private string? _clienteNome;
    private Guid? _processoId;
    private string? _processoTitulo;
    private Guid? _atividadeId;
    private string? _atividadeTitulo;
    private bool _atividadeExcluida;

    private string? _buscaDeCliente;
    private IReadOnlyList<ClienteResumo> _clientesEncontrados = [];
    private bool _nenhumCliente;
    private bool _clientesTruncados;

    private string? _buscaDeProcesso;
    private IReadOnlyList<ProcessoResumo> _processosEncontrados = [];
    private bool _nenhumProcesso;
    private bool _processosTruncados;

    private string? _buscaDeAtividade;
    private IReadOnlyList<AtividadeResumo> _atividadesEncontradas = [];
    private bool _nenhumaAtividade;
    private bool _atividadesTruncadas;

    private string Titulo => Id is null ? "Novo evento" : "Editar evento";

    // A origem recebida, só com o que a agenda aceitaria ("" sem origem).
    private string ConsultaRecebida => PeriodoDaAgenda.ConsultaDeOrigem(VisaoDeOrigem, DataDeOrigem);

    // O voltar, o X do mobile e o Cancelar: a agenda de onde se veio (novo) ou a ficha do evento, com a origem (edição).
    private string Voltar => Id is { } id ? PeriodoDaAgenda.Ficha(id, ConsultaRecebida) : $"/agenda{ConsultaRecebida}";

    // Antes da carga e no evento não encontrado, a edição diz só "Evento".
    private string OrigemDoCabecalho => Id is null ? "Agenda" : _tituloDoEvento ?? "Evento";

    // O subtítulo só acompanha o formulário: sobre os avisos (acesso, não encontrado, não gerencia) ele sobra.
    private string? SubtituloDoFormulario => _estado == EstadoDeCarga.Pronto && _podeEditar
        ? "Preencha os dados e escolha quem participa."
        : null;

    // "Hoje" no fuso do escritório, numa função só.
    private DateOnly Hoje() => DateOnly.FromDateTime(Fuso.ParaLocal(DateTime.UtcNow));

    protected override Task OnInitializedAsync() => CarregarAsync();

    // A carga inteira, também a do "Tentar de novo": quem está na tela, o evento (edição) e a equipe.
    private async Task CarregarAsync()
    {
        _estado = EstadoDeCarga.Carregando;
        _erroDaCarga = null;
        _mensagem = null;
        _podeEditar = true;
        _form = new Formulario();
        _contexto = null;
        _versao = null;
        _tituloDoEvento = null;
        _outraDuracao = false;
        _semParticipantes = false;
        _euId = Guid.Empty;
        _criadorTravado = null;
        _pessoas = [];
        _marcados = [];
        _clienteId = null;
        _clienteNome = null;
        _processoId = null;
        _processoTitulo = null;
        _atividadeId = null;
        _atividadeTitulo = null;
        _atividadeExcluida = false;
        LimparBuscaDeCliente();
        LimparBuscaDeProcesso();
        LimparBuscaDeAtividade();

        if (await Despachante.EnviarAsync(new ObterMeuAcesso()) is not { } eu)
        {
            _estado = EstadoDeCarga.AcessoNegado;
            return;
        }

        _euId = eu.Id;

        EventoDetalhe? atual = null;
        if (Id is { } id)
        {
            var evento = await Despachante.EnviarAsync(new ObterEvento(id));
            if (evento.Valor is not { } e)
            {
                Falhar(evento, "Não foi possível consultar o evento.", doEvento: true);
                return;
            }

            _tituloDoEvento = e.Titulo;
            // Quem vê mas não gerencia: o aviso, sem formulário e sem consultar a equipe (a tela decide pelas ações do
            // evento, nunca pelo perfil).
            if (!e.Acoes.Editar)
            {
                _podeEditar = false;
                _estado = EstadoDeCarga.Pronto;
                return;
            }

            atual = e;
        }

        var usuarios = await Despachante.EnviarAsync(new ListarUsuariosParaAgenda());
        if (usuarios.Valor is not { } ativos)
        {
            Falhar(usuarios, "Não foi possível consultar os usuários.");
            return;
        }

        var pessoas = ativos.Select(u => new Pessoa(u.Id, u.Nome, true)).ToList();
        if (atual is not null)
        {
            var inicio = Fuso.ParaLocal(atual.InicioEm);
            _form = new Formulario
            {
                Titulo = atual.Titulo,
                Tipo = atual.Tipo,
                Data = DateOnly.FromDateTime(inicio),
                Hora = TimeOnly.FromDateTime(inicio),
                Duracao = atual.DuracaoEstimadaMinutos,
                Local = atual.Local ?? string.Empty,
                Descricao = atual.Descricao ?? string.Empty,
            };
            _versao = atual.Versao;
            _clienteId = atual.ClienteId;
            _clienteNome = atual.ClienteId is null ? null : atual.ClienteNome ?? "Cliente indisponível";
            _processoId = atual.ProcessoId;
            _processoTitulo = atual.ProcessoId is null ? null : atual.ProcessoTitulo ?? "Processo indisponível";
            _atividadeId = atual.AtividadeId;
            _atividadeTitulo = atual.AtividadeTitulo;   // nulo: a atividade foi excluída ou o usuário não a vê
            _atividadeExcluida = atual.AtividadeExcluida;

            // Na edição a lista enviada é a que vale: os participantes atuais (inclusive os inativos e os que não são mais
            // encontrados, que a lista de ativos não traz) aparecem marcados; os demais ativos, desmarcados. Ninguém trava.
            foreach (var participante in atual.Participantes)
            {
                pessoas.RemoveAll(p => p.Id == participante.UsuarioId);
                pessoas.Add(new Pessoa(participante.UsuarioId, participante.Nome, participante.Ativo));
            }

            _marcados = [.. atual.Participantes.Select(p => p.UsuarioId)];
        }
        else
        {
            // Evento novo: hoje, às 09:00, uma hora, Reunião (os padrões do formulário de antes da fase).
            _form = new Formulario { Data = Hoje(), Hora = new TimeOnly(9, 0) };

            // O servidor sempre inclui quem cria; o formulário já mostra isso, marcado e travado.
            if (pessoas.All(p => p.Id != eu.Id)) pessoas.Add(new Pessoa(eu.Id, eu.Nome, true));
            _marcados = [eu.Id];
            _criadorTravado = eu.Id;
        }

        _pessoas = [.. pessoas.OrderBy(p => p.Rotulo, StringComparer.CurrentCultureIgnoreCase)];
        _contexto = new EditContext(_form);
        _estado = EstadoDeCarga.Pronto;
    }

    // Acesso negado encerra a tela; "não encontrado" só quando é o próprio evento; o resto, a mensagem com "Tentar de novo".
    private void Falhar(Resultado resultado, string padrao, bool doEvento = false)
    {
        if (resultado.Erro == TipoErro.AcessoNegado)
            _estado = EstadoDeCarga.AcessoNegado;
        else if (doEvento && resultado.Erro == TipoErro.NaoEncontrado)
            _estado = EstadoDeCarga.NaoEncontrado;
        else
        {
            _estado = EstadoDeCarga.Falhou;
            _erroDaCarga = resultado.Mensagem ?? padrao;
        }
    }

    // Perder o acesso no meio do uso encerra a tela.
    private bool Encerrou(Resultado resultado)
    {
        if (resultado.Erro != TipoErro.AcessoNegado) return false;

        _estado = EstadoDeCarga.AcessoNegado;
        return true;
    }

    private void EscolherTipo(TipoEvento tipo) => _form.Tipo = tipo;

    // "Outra duração": escolhida na lista, ou a duração não é uma das quatro (a edição de um evento de 45 minutos).
    private bool OutraDuracao => _outraDuracao || _form.Duracao is not { } minutos || !DuracoesDosQuadros.Contains(minutos);

    private string OpcaoDaDuracao => OutraDuracao ? "outra" : _form.Duracao!.Value.ToString(CultureInfo.InvariantCulture);

    private void EscolherDuracao(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var minutos))
        {
            _outraDuracao = false;
            _form.Duracao = minutos;
            // Revalida o campo: um "Campo obrigatório" dos minutos sai com a escolha.
            _contexto?.NotifyFieldChanged(FieldIdentifier.Create(() => _form.Duracao));
        }
        else
        {
            // "Outra duração": os minutos atuais ficam no número, para quem quiser mudá-los.
            _outraDuracao = true;
        }
    }

    private bool Marcado(Pessoa pessoa) => _marcados.Contains(pessoa.Id);

    private bool Travado(Pessoa pessoa) => _salvando || pessoa.Id == _criadorTravado;

    private void Marcar(Pessoa pessoa, ChangeEventArgs e)
    {
        if (Travado(pessoa)) return;

        if (e.Value is true) _marcados.Add(pessoa.Id);
        else _marcados.Remove(pessoa.Id);
        if (_marcados.Count > 0) _semParticipantes = false;
    }

    private bool EhVoce(Pessoa pessoa) => pessoa.Id == _euId;

    // A nota da caixa do desktop; no tablet ela vai no mesmo texto do chip.
    private string? NotaDe(Pessoa pessoa) => EhVoce(pessoa) ? "(você)" : pessoa.Ativo ? null : "(inativo)";

    private string NomeNoTablet(Pessoa pessoa) => NotaDe(pessoa) is { } nota ? $"{pessoa.Rotulo} {nota}" : pessoa.Rotulo;

    // O mobile mostra o primeiro nome (o próprio usuário é "Você"); quem não é mais encontrado fica com o rótulo inteiro.
    private string NomeNoMobile(Pessoa pessoa)
    {
        if (EhVoce(pessoa)) return "Você";
        if (pessoa.Nome is not { } nome) return pessoa.Rotulo;

        var primeiro = nome.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries) is [var parte, ..] ? parte : nome;
        return pessoa.Ativo ? primeiro : $"{primeiro} (inativo)";
    }

    // O próprio usuário em dourado (o avatar sem modificador, como o do menu); os outros pela cor do nome.
    private string ClasseDoAvatar(Pessoa pessoa) =>
        EhVoce(pessoa) ? "aj-avatar" : $"aj-avatar {AvatarDePessoa.Classe(pessoa.Nome ?? string.Empty)}";

    private static string SiglaDe(Pessoa pessoa) => AvatarDePessoa.Sigla(pessoa.Nome ?? string.Empty);

    private string ClasseDaLegenda => _semParticipantes ? "aj-rotulo aj-rotulo--erro" : "aj-rotulo";

    private static string MetaDoProcesso(ProcessoResumo processo) =>
        processo.Clientes.Count == 0 ? processo.AreaNome : $"{processo.AreaNome} · {string.Join(", ", processo.Clientes)}";

    private async Task ProcurarClientesAsync()
    {
        if (_salvando) return;

        _mensagem = null;
        var resultado = await Despachante.EnviarAsync(new ListarClientes(_buscaDeCliente));
        if (Encerrou(resultado)) return;

        var itens = resultado.Valor?.Itens ?? [];
        _clientesEncontrados = itens;
        // Os avisos valem só para uma busca que deu certo; uma falha mostra o erro no lugar deles.
        _nenhumCliente = resultado.Sucesso && itens.Count == 0;
        _clientesTruncados = resultado.Sucesso && resultado.Valor!.Total > itens.Count;
        if (!resultado.Sucesso) _mensagem = resultado.Mensagem;
    }

    private void EscolherCliente(ClienteResumo cliente)
    {
        if (_salvando) return;

        _clienteId = cliente.Id;
        _clienteNome = cliente.Nome;
        LimparBuscaDeCliente();
    }

    private void RemoverCliente()
    {
        if (_salvando) return;

        _clienteId = null;
        _clienteNome = null;
    }

    private async Task ProcurarProcessosAsync()
    {
        if (_salvando) return;

        _mensagem = null;
        var resultado = await Despachante.EnviarAsync(new ListarProcessos(_buscaDeProcesso));
        if (Encerrou(resultado)) return;

        var itens = resultado.Valor?.Itens ?? [];
        _processosEncontrados = itens;
        _nenhumProcesso = resultado.Sucesso && itens.Count == 0;
        _processosTruncados = resultado.Sucesso && resultado.Valor!.Total > itens.Count;
        if (!resultado.Sucesso) _mensagem = resultado.Mensagem;
    }

    private void EscolherProcesso(ProcessoResumo processo)
    {
        if (_salvando) return;

        // O cliente escolhido não é trocado aqui: se ele não é do processo, o servidor recusa e a mensagem aparece.
        _processoId = processo.Id;
        _processoTitulo = processo.Titulo;
        LimparBuscaDeProcesso();
    }

    private void RemoverProcesso()
    {
        if (_salvando) return;

        _processoId = null;
        _processoTitulo = null;
    }

    private async Task ProcurarAtividadesAsync()
    {
        if (_salvando) return;

        _mensagem = null;
        // O servidor só devolve as atividades que o usuário pode ver.
        var resultado = await Despachante.EnviarAsync(new ListarAtividades(Busca: _buscaDeAtividade));
        if (Encerrou(resultado)) return;

        var itens = resultado.Valor?.Itens ?? [];
        _atividadesEncontradas = itens;
        _nenhumaAtividade = resultado.Sucesso && itens.Count == 0;
        _atividadesTruncadas = resultado.Sucesso && resultado.Valor!.Total > itens.Count;
        if (!resultado.Sucesso) _mensagem = resultado.Mensagem;
    }

    private void EscolherAtividade(AtividadeResumo atividade)
    {
        if (_salvando) return;

        _atividadeId = atividade.Id;
        _atividadeTitulo = atividade.Titulo;
        _atividadeExcluida = false;
        LimparBuscaDeAtividade();
    }

    private void RemoverAtividade()
    {
        if (_salvando) return;

        _atividadeId = null;
        _atividadeTitulo = null;
        _atividadeExcluida = false;
    }

    private void LimparBuscaDeCliente()
    {
        _clientesEncontrados = [];
        _nenhumCliente = false;
        _clientesTruncados = false;
        _buscaDeCliente = null;
    }

    private void LimparBuscaDeProcesso()
    {
        _processosEncontrados = [];
        _nenhumProcesso = false;
        _processosTruncados = false;
        _buscaDeProcesso = null;
    }

    private void LimparBuscaDeAtividade()
    {
        _atividadesEncontradas = [];
        _nenhumaAtividade = false;
        _atividadesTruncadas = false;
        _buscaDeAtividade = null;
    }

    // O envio do formulário: os erros por campo (o EditContext), o grupo de participantes e o aviso do topo para data e
    // hora; só sem nenhum deles o evento é salvo.
    private async Task EnviarAsync()
    {
        if (_salvando || _contexto is null) return;

        _mensagem = null;
        var camposValidos = _contexto.Validate();
        _semParticipantes = _marcados.Count == 0;
        if (_form.Data is null || _form.Hora is null)
            _mensagem = "Informe a data e a hora do evento.";
        else if (_form.Data.Value.Year < Evento.AnoMinimoDeInicio || _form.Data.Value.Year > Evento.AnoMaximoDeInicio)
            _mensagem = $"A data deve estar entre os anos {Evento.AnoMinimoDeInicio} e {Evento.AnoMaximoDeInicio}.";

        if (!camposValidos || _semParticipantes || _mensagem is not null) return;

        await SalvarAsync(_form.Data!.Value, _form.Hora!.Value);
    }

    private async Task SalvarAsync(DateOnly data, TimeOnly hora)
    {
        _salvando = true;
        var navegou = false;
        try
        {
            // Só o que está marcado na tela é enviado, na ordem da lista; na edição a lista enviada é a que vale.
            IReadOnlyList<Guid> participantes = [.. _pessoas.Where(Marcado).Select(p => p.Id)];
            var resultado = await Despachante.EnviarAsync(new SalvarEvento(
                Id, _form.Titulo, _form.Descricao, _form.Local, _form.Tipo,
                // Data e hora são do fuso do escritório; o servidor recebe UTC.
                Fuso.ParaUtc(data.ToDateTime(hora)), _form.Duracao!.Value,
                _clienteId, _processoId, _atividadeId, participantes,
                // Na edição, a versão com que a tela carregou o evento; evento novo não tem.
                Id is null ? null : _versao));

            if (Encerrou(resultado)) return;

            if (!resultado.Sucesso)
            {
                // O formulário permanece com o que foi digitado; conflito e "não encontrado" trazem a mensagem do servidor.
                _mensagem = resultado.Mensagem;
                return;
            }

            // A sobreposição é aviso, não impedimento: quem mostra o recado (a agenda ou a ficha) dá as três redações.
            Recados.Deixar("Evento salvo.", TipoDeAviso.Sucesso, resultado.Valor!.Sobreposicoes);
            navegou = true;
            // O novo volta à agenda de origem na data do evento (para ele aparecer); a edição, à ficha, com a origem.
            Navegacao.NavigateTo(Id is { } id
                ? PeriodoDaAgenda.Ficha(id, ConsultaRecebida)
                : PeriodoDaAgenda.Ler(VisaoDeOrigem, DataDeOrigem, Hoje()).ComData(data).Endereco);
        }
        finally
        {
            // Depois do sucesso a página vai embora: o botão fica travado para um clique tardio não salvar de novo.
            if (!navegou) _salvando = false;
        }
    }

    /// <summary>Uma pessoa da lista de participantes. <paramref name="Nome"/> é nulo quando o usuário não é mais encontrado.</summary>
    private sealed record Pessoa(Guid Id, string? Nome, bool Ativo)
    {
        public string Rotulo => Nome ?? "Usuário não encontrado";
    }

    private sealed class Formulario
    {
        [Required(ErrorMessage = CampoObrigatorio)]
        public string Titulo { get; set; } = string.Empty;

        public TipoEvento Tipo { get; set; } = TipoEvento.Reuniao;

        [Required(ErrorMessage = CampoObrigatorio)]
        public DateOnly? Data { get; set; }

        [Required(ErrorMessage = CampoObrigatorio)]
        public TimeOnly? Hora { get; set; }

        [Required(ErrorMessage = CampoObrigatorio)]
        public int? Duracao { get; set; } = 60;

        public string Local { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;
    }
}
```

Notas para quem implementa:

- Os controles com `@bind-Value` (`InputText`, `InputDate`, `InputNumber`, `InputSelect`, `InputTextArea`) somam à classe a do estado do campo (`valid`/`invalid`) e marcam `aria-invalid="true"` em erro (é o que o `.aj-campo[aria-invalid="true"]` do quadro pinta): por isso os testes não procuram `class="aj-campo"` neles. Os `<select>` simples (Duração do tablet e do mobile) têm `class="aj-campo"` exato.
- A hora é `InputDate Type="InputDateType.Time"`, que lê e escreve `HH:mm:ss` (o `@bind` de hoje faz o mesmo): o teste bUnit digita `14:30:00`.
- `RotuloDeCampo` com `Complemento` (Tarefa 3) põe `<small>(opcional)</small>` depois do texto; o `Para` da Descrição só serve ao componente (o campo não tem regra).
- As classes montadas por expressão (`ClasseDoAvatar`, `ClasseDaLegenda`) são ponto cego da trava: só `aj-avatar`, `aj-avatar--{cor}`, `aj-rotulo` e `aj-rotulo--erro`. O rádio e as caixas dos chips usam `aj-opcao__caixa` (a regra só pede classe `aj-`; o `aj-escolha__opcao input` e o `aj-chip--pessoa input` escondem o controle).
- `id` só aparece uma vez (os três rótulos de vínculo, o `h2` dos participantes, o formulário e as três buscas); os campos repetidos entre visões não têm `id`. As duas buscas de processo (desktop e tablet/mobile) usam o mesmo `form="busca-de-processo"` e o mesmo estado.
- A tela entra em `Migradas` na Tarefa 10; até lá, confira à mão: nenhum `style`, nenhuma classe fora de `aj-`, todo `button`, `input`, `select` e `textarea` com `class`.
- O componente antigo `Components/Agenda/FormularioDeEvento.razor` continua (a agenda antiga o usa até a Tarefa 9; sai na 10). Não o altere.
- `PeriodoDaAgenda.ConsultaDeOrigem`, `Ficha`, `Ler`, `ComData` e `Endereco` são os da Tarefa 2. O destino do novo sem origem é `/agenda?data={data}` (a `visao` só vai quando veio no endereço).

- [ ] **Step 3: Build e testes**

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~EventoFormulario|FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~AutenticacaoWebTests"`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~WebSoPeloDespachante"`
Expected: 0 avisos; todos passando.

- [ ] **Step 4: Commit**

Antes, `git branch --show-current` (deve ser `worktree-feat+fase-9f-agenda`).

```
git add src tests
git commit -F <arquivo da mensagem, UTF-8 sem BOM>
```

Mensagem:

```
feat(web): formulário do evento em tela própria, com os controles de cada visão, erros por campo e o recado das sobreposições

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

---

### Task 6: Cartão de evento e Semana

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Agenda/CartaoDeEvento.razor`, `FormaDoCartao.cs`, `SemanaDaAgenda.razor`
- Test: `tests/AtividadesJuridicas.IntegrationTests/SemanaDaAgendaTests.cs` (com a classe `CartaoDeEventoTests`), `QuadroDaAgenda.cs` (novo: os eventos do quadro, usados também pelas Tarefas 7, 8 e 9)

**Interfaces:**
- Consumes: `PeriodoDaAgenda`, `RotulosDeAgenda` (Tarefa 2, inclusive `Participantes`).
- Produces:
  - `public static string CartaoDeEvento.Sufixo(TipoEvento)` (`reuniao`, `audiencia`, `prazo`, `outro`; as Tarefas 7 e 8 o usam nos pontos).
  - `public enum FormaDoCartao { Coluna, Secao, Folgado, Linha, Mes }`.
  - `CartaoDeEvento`: `[Parameter, EditorRequired] EventoResumo Evento`, `DateOnly Dia`, `FormaDoCartao Forma`, `string Href`, e na `Linha` `string? ClassesDaRegua` (as classes de posição da Tarefa 7). Emite `<a class="aj-evento aj-evento--{tipo} …" href>` com as linhas da forma (seção "Decisões").
  - `SemanaDaAgenda`: `[Parameter, EditorRequired] PeriodoDaAgenda Periodo`, `IReadOnlyList<EventoResumo> Eventos`, `DateOnly Hoje`. Emite `aj-so-desktop` (a grade `aj-semana` de sete colunas) e `aj-tablet-mobile` (sete `aj-secao-dia`; no mobile, uma coluna de cartões por CSS).

**Regras:**
- Distribuição por dia com `RotulosDeAgenda.TocaODia`; ordem de início, depois id (a do servidor).
- Os cartões levam o `href` da ficha com a origem (`PeriodoDaAgenda.Ficha(evento.Id, Periodo.Consulta)`).
- Marcação das colunas e das seções nos quadros `AgendaA` e `AgendaTablet` (`quadros-agenda.md`, seções 2 e 4), com as classes da Tarefa 1.

**Testes obrigatórios:** a semana do quadro (os oito eventos nas colunas e nas seções certas, hoje marcado, `Sem eventos` no sábado e no domingo, fim de semana apagado no desktop); as linhas de cada forma (desktop com local, cliente e participantes; tablet com o local ou o cliente; evento sem local nem cliente); o evento de três dias nas três colunas e o que termina à meia-noite (Review Focus 2); os links com a origem.

- [ ] **Step 1: Testes (devem falhar)**

Antes de escrever, releia na cópia local dos quadros a Semana de `AgendaA.dc.html` (as sete `section` dentro da caixa da semana, os cartões `button.ev` com tipo, horário, título e as linhas de apoio, o `p` "Sem eventos" de sábado e domingo) e as seções de `AgendaTablet.dc.html` (`section` com o `h2` de 64px, a grade de dois cartões, o cartão com a hora em coluna), e as seções 1.4, 2 (item 7), 4 (item 7), 8.4, 8.5 e 8.6 de `quadros-agenda.md`.

Os testes são de componente (bUnit, base `TelaInterativa`): os componentes de visão recebem os eventos prontos, sem despachante. Os eventos são os do quadro (seção 1.4 de `quadros-agenda.md`), montados à mão no fuso `America/Sao_Paulo`.

Crie o auxiliar `tests/AtividadesJuridicas.IntegrationTests/QuadroDaAgenda.cs` (as Tarefas 7, 8 e 9 também o usam):

```csharp
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Os eventos dos quadros da agenda: a semana de 05/10 a 11/10/2026, com hoje na quinta-feira, 08/10, no fuso do
/// escritório (America/Sao_Paulo). Os testes dos componentes de visão (Semana, Dia e Mês) recebem estes eventos
/// prontos, como a página os entrega depois da consulta.
/// </summary>
public static class QuadroDaAgenda
{
    // Declarado antes dos eventos: os campos estáticos são iniciados na ordem do texto.
    private static readonly FusoDoEscritorio Fuso = new("America/Sao_Paulo");

    /// <summary>Quinta-feira, 08/10/2026: o "hoje" dos quadros.</summary>
    public static readonly DateOnly Hoje = new(2026, 10, 8);

    public static readonly EventoResumo PautaDaSemana = Evento(new DateTime(2026, 10, 5, 9, 0, 0), 60, TipoEvento.Reuniao,
        "Pauta da semana", "Sala de reuniões", null, "Marina Couto", "Rafael Teles", "Beatriz Lins");

    public static readonly EventoResumo AudienciaDeInstrucao = Evento(new DateTime(2026, 10, 5, 14, 0, 0), 90, TipoEvento.Audiencia,
        "Audiência de instrução", "2ª Vara do Trabalho", "Transportes Litoral Ltda.", "Beatriz Lins");

    public static readonly EventoResumo Contestacao = Evento(new DateTime(2026, 10, 6, 17, 0, 0), 60, TipoEvento.Prazo,
        "Contestação — cobrança de ISS", null, "Construtora Serra Azul Ltda.", "Rafael Teles");

    public static readonly EventoResumo Alinhamento = Evento(new DateTime(2026, 10, 7, 10, 0, 0), 90, TipoEvento.Reuniao,
        "Alinhamento com o cliente", "Videoconferência", "Construtora Serra Azul Ltda.", "Rafael Teles", "Marina Couto");

    public static readonly EventoResumo AudienciaDeConciliacao = Evento(new DateTime(2026, 10, 7, 11, 0, 0), 60, TipoEvento.Audiencia,
        "Audiência de conciliação", "Fórum Cível, sala 4", "Joana Figueiredo", "Rafael Teles");

    public static readonly EventoResumo ProtocoloDeDocumentos = Evento(new DateTime(2026, 10, 8, 9, 30, 0), 60, TipoEvento.Outro,
        "Protocolo de documentos", "Cartório do 1º Ofício", null, "Otávio Brandão");

    public static readonly EventoResumo RevisaoDeMinutas = Evento(new DateTime(2026, 10, 8, 15, 0, 0), 60, TipoEvento.Reuniao,
        "Revisão de minutas", "Sala de reuniões", null, "Marina Couto", "Camila Prado");

    public static readonly EventoResumo RecursoOrdinario = Evento(new DateTime(2026, 10, 9, 16, 0, 0), 60, TipoEvento.Prazo,
        "Recurso ordinário", null, "Transportes Litoral Ltda.", "Beatriz Lins");

    /// <summary>Os oito eventos da semana do quadro, na ordem em que o servidor os manda (início, depois id).</summary>
    public static IReadOnlyList<EventoResumo> Semana { get; } =
    [
        PautaDaSemana, AudienciaDeInstrucao, Contestacao, Alinhamento, AudienciaDeConciliacao,
        ProtocoloDeDocumentos, RevisaoDeMinutas, RecursoOrdinario,
    ];

    /// <summary>
    /// Um evento que começa em <paramref name="inicioLocal"/> (horário do escritório, <see cref="DateTimeKind.Unspecified"/>)
    /// e dura <paramref name="minutos"/>. Sem processo: nenhum cartão o mostra.
    /// </summary>
    public static EventoResumo Evento(DateTime inicioLocal, int minutos, TipoEvento tipo, string titulo,
        string? local = null, string? cliente = null, params string[] participantes)
    {
        var inicio = Fuso.ParaUtc(inicioLocal);
        return new EventoResumo(Guid.NewGuid(), titulo, tipo, inicio, inicio.AddMinutes(minutos), local, cliente, null, participantes);
    }
}
```

Crie `tests/AtividadesJuridicas.IntegrationTests/SemanaDaAgendaTests.cs`, com duas classes: a da Semana e a do cartão (as cinco formas do cartão, desenhado sozinho):

```csharp
using AngleSharp.Dom;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Web.Components.Agenda;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// A Semana da agenda com os eventos do quadro (05/10 a 11/10/2026, hoje quinta-feira 08/10): as sete colunas do
/// desktop e as sete seções do tablet e do mobile, com os mesmos eventos, cada um em todo dia que toca, o hoje
/// marcado e os cartões levando à ficha com o período de origem.
/// </summary>
public class SemanaDaAgendaTests : TelaInterativa
{
    private static readonly PeriodoDaAgenda SemanaDoQuadro = new(VisaoDaAgenda.Semana, QuadroDaAgenda.Hoje, true);

    private static readonly string[] RotulosDaSemana =
    [
        "segunda-feira, 05/10/2026", "terça-feira, 06/10/2026", "quarta-feira, 07/10/2026",
        "quinta-feira, 08/10/2026, hoje", "sexta-feira, 09/10/2026", "sábado, 10/10/2026", "domingo, 11/10/2026",
    ];

    private IRenderedComponent<SemanaDaAgenda> Semana(
        IReadOnlyList<EventoResumo> eventos, PeriodoDaAgenda? periodo = null, DateOnly? hoje = null) =>
        Desenhar<SemanaDaAgenda>(p => p
            .Add(c => c.Periodo, periodo ?? SemanaDoQuadro)
            .Add(c => c.Eventos, eventos)
            .Add(c => c.Hoje, hoje ?? QuadroDaAgenda.Hoje));

    private static IReadOnlyList<IElement> Colunas(IRenderedComponent<SemanaDaAgenda> semana) =>
        semana.FindAll("div.aj-so-desktop > div.aj-semana > section");

    private static IReadOnlyList<IElement> Secoes(IRenderedComponent<SemanaDaAgenda> semana) =>
        semana.FindAll("div.aj-tablet-mobile > section");

    private static string[] Titulos(IElement dia) =>
        dia.QuerySelectorAll("a.aj-evento .aj-evento__titulo").Select(t => t.TextContent).ToArray();

    private static IElement Cartao(IElement dia, string titulo) =>
        dia.QuerySelectorAll("a.aj-evento").Single(a => a.QuerySelector(".aj-evento__titulo")!.TextContent == titulo);

    private static string[] Linhas(IElement elemento) =>
        elemento.Children.Select(linha => $"{linha.ClassName}: {linha.TextContent}").ToArray();

    [Fact]
    public void Desktop_tem_uma_coluna_por_dia_com_os_eventos_do_quadro_hoje_marcado_e_o_fim_de_semana_apagado_e_vazio()
    {
        var colunas = Colunas(Semana(QuadroDaAgenda.Semana));

        colunas.Should().HaveCount(7);
        colunas.Select(c => c.ClassName).Should().Equal(
            "aj-semana__dia", "aj-semana__dia", "aj-semana__dia", "aj-semana__dia aj-semana__dia--hoje",
            "aj-semana__dia", "aj-semana__dia aj-semana__dia--folga", "aj-semana__dia aj-semana__dia--folga");
        colunas.Select(c => c.QuerySelector("h2.aj-semana__cabeca")!.GetAttribute("aria-label")).Should().Equal(RotulosDaSemana);
        colunas.Select(c => c.QuerySelector("h2 > span.aj-semana__sigla")!.TextContent).Should().Equal(
            "Seg", "Ter", "Qua", "Qui · hoje", "Sex", "Sáb", "Dom");
        colunas.Select(c => c.QuerySelector("h2 > span.aj-semana__numero")!.TextContent).Should().Equal(
            "05", "06", "07", "08", "09", "10", "11");

        Titulos(colunas[0]).Should().Equal("Pauta da semana", "Audiência de instrução");
        Titulos(colunas[1]).Should().Equal("Contestação — cobrança de ISS");
        Titulos(colunas[2]).Should().Equal("Alinhamento com o cliente", "Audiência de conciliação");
        Titulos(colunas[3]).Should().Equal("Protocolo de documentos", "Revisão de minutas");
        Titulos(colunas[4]).Should().Equal("Recurso ordinário");
        colunas[0].QuerySelector("p.aj-semana__vazio").Should().BeNull();
        foreach (var folga in colunas.Skip(5))
        {
            Titulos(folga).Should().BeEmpty();
            folga.QuerySelector("p.aj-semana__vazio")!.TextContent.Should().Be("Sem eventos");
        }
    }

    [Fact]
    public void Tablet_e_mobile_tem_uma_secao_por_dia_com_os_mesmos_eventos_e_a_sigla_hoje()
    {
        var secoes = Secoes(Semana(QuadroDaAgenda.Semana));

        secoes.Should().HaveCount(7);
        secoes.Select(s => s.ClassName).Should().Equal(
            "aj-secao-dia", "aj-secao-dia", "aj-secao-dia", "aj-secao-dia aj-secao-dia--hoje",
            "aj-secao-dia", "aj-secao-dia", "aj-secao-dia");
        secoes.Select(s => s.QuerySelector("h2.aj-secao-dia__cabeca")!.GetAttribute("aria-label")).Should().Equal(RotulosDaSemana);
        secoes.Select(s => s.QuerySelector("h2 > span.aj-secao-dia__sigla")!.TextContent).Should().Equal(
            "Seg", "Ter", "Qua", "Hoje", "Sex", "Sáb", "Dom");
        secoes.Select(s => s.QuerySelector("h2 > span.aj-secao-dia__numero")!.TextContent).Should().Equal(
            "05", "06", "07", "08", "09", "10", "11");

        secoes.Select(s => s.QuerySelectorAll("div.aj-secao-dia__eventos > a.aj-evento").Length).Should().Equal(2, 1, 2, 2, 1, 0, 0);
        Titulos(secoes[2]).Should().Equal("Alinhamento com o cliente", "Audiência de conciliação");
        Titulos(secoes[3]).Should().Equal("Protocolo de documentos", "Revisão de minutas");
        secoes[3].QuerySelector("p.aj-secao-dia__vazio").Should().BeNull();
        foreach (var folga in secoes.Skip(5))
            folga.QuerySelector("div.aj-secao-dia__eventos > p.aj-secao-dia__vazio")!.TextContent.Should().Be("Sem eventos");
    }

    [Fact]
    public void Cartao_da_coluna_tem_tipo_horario_titulo_e_as_linhas_de_local_cliente_e_participantes_so_quando_ha()
    {
        var colunas = Colunas(Semana(QuadroDaAgenda.Semana));

        var instrucao = Cartao(colunas[0], "Audiência de instrução");
        instrucao.ClassName.Should().Be("aj-evento aj-evento--coluna aj-evento--audiencia");
        Linhas(instrucao).Should().Equal(
            "aj-evento__tipo: Audiência", "aj-evento__horario: 14:00 – 15:30", "aj-evento__titulo: Audiência de instrução",
            "aj-evento__local: 2ª Vara do Trabalho", "aj-evento__meta: Transportes Litoral Ltda.", "aj-evento__meta: Beatriz Lins");

        // Sem cliente: local e participantes.
        Linhas(Cartao(colunas[0], "Pauta da semana")).Should().Equal(
            "aj-evento__tipo: Reunião", "aj-evento__horario: 09:00 – 10:00", "aj-evento__titulo: Pauta da semana",
            "aj-evento__local: Sala de reuniões", "aj-evento__meta: Marina Couto, Rafael Teles, Beatriz Lins");

        // Sem local: cliente e participantes.
        var contestacao = Cartao(colunas[1], "Contestação — cobrança de ISS");
        contestacao.ClassName.Should().Be("aj-evento aj-evento--coluna aj-evento--prazo");
        Linhas(contestacao).Should().Equal(
            "aj-evento__tipo: Prazo", "aj-evento__horario: 17:00 – 18:00", "aj-evento__titulo: Contestação — cobrança de ISS",
            "aj-evento__meta: Construtora Serra Azul Ltda.", "aj-evento__meta: Rafael Teles");

        Cartao(colunas[3], "Protocolo de documentos").ClassName.Should().Be("aj-evento aj-evento--coluna aj-evento--outro");
    }

    [Fact]
    public void Cartao_da_secao_tem_a_hora_em_coluna_e_uma_linha_com_o_local_ou_o_cliente()
    {
        var secoes = Secoes(Semana(QuadroDaAgenda.Semana));

        var instrucao = Cartao(secoes[0], "Audiência de instrução");
        instrucao.ClassName.Should().Be("aj-evento aj-evento--audiencia");
        instrucao.QuerySelector("span.aj-evento__hora > b")!.TextContent.Should().Be("14:00");
        instrucao.QuerySelector("span.aj-evento__hora > span")!.TextContent.Should().Be("15:30");
        Linhas(instrucao.QuerySelector("span.aj-evento__corpo")!).Should().Equal(
            "aj-evento__tipo: Audiência", "aj-evento__titulo: Audiência de instrução", "aj-evento__local: 2ª Vara do Trabalho");
        // O cartão do tablet não mostra o cliente quando há local, nem os participantes.
        instrucao.TextContent.Should().Contain("2ª Vara do Trabalho").And.NotContain("Transportes Litoral Ltda.").And.NotContain("Beatriz Lins");

        // Sem local, a linha é o cliente.
        Linhas(Cartao(secoes[1], "Contestação — cobrança de ISS").QuerySelector("span.aj-evento__corpo")!).Should().Equal(
            "aj-evento__tipo: Prazo", "aj-evento__titulo: Contestação — cobrança de ISS", "aj-evento__local: Construtora Serra Azul Ltda.");
    }

    [Fact]
    public void Evento_sem_local_sem_cliente_e_sem_participantes_so_tem_tipo_horario_e_titulo()
    {
        var despacho = QuadroDaAgenda.Evento(new DateTime(2026, 10, 6, 8, 0, 0), 60, TipoEvento.Outro, "Despacho interno");

        var semana = Semana([despacho]);

        Linhas(Cartao(Colunas(semana)[1], "Despacho interno")).Should().Equal(
            "aj-evento__tipo: Outro", "aj-evento__horario: 08:00 – 09:00", "aj-evento__titulo: Despacho interno");
        Linhas(Cartao(Secoes(semana)[1], "Despacho interno").QuerySelector("span.aj-evento__corpo")!).Should().Equal(
            "aj-evento__tipo: Outro", "aj-evento__titulo: Despacho interno");
    }

    [Fact]
    public void Evento_de_tres_dias_aparece_nas_tres_colunas_e_secoes_com_o_lado_de_outro_dia_marcado()
    {
        // 06/10 às 20:00 até 08/10 às 10:00, mandado por último: a ordem do dia é a do início, não a da lista.
        var plantao = QuadroDaAgenda.Evento(new DateTime(2026, 10, 6, 20, 0, 0), 38 * 60, TipoEvento.Prazo, "Plantão de protocolo");

        var semana = Semana([.. QuadroDaAgenda.Semana, plantao]);

        var colunas = Colunas(semana);
        Titulos(colunas[0]).Should().NotContain("Plantão de protocolo").And.Contain("Pauta da semana");
        Titulos(colunas[1]).Should().Equal("Contestação — cobrança de ISS", "Plantão de protocolo");
        Titulos(colunas[2]).Should().Equal("Plantão de protocolo", "Alinhamento com o cliente", "Audiência de conciliação");
        Titulos(colunas[3]).Should().Equal("Plantão de protocolo", "Protocolo de documentos", "Revisão de minutas");
        Titulos(colunas[4]).Should().Equal("Recurso ordinário");
        Cartao(colunas[1], "Plantão de protocolo").QuerySelector(".aj-evento__horario")!.TextContent.Should().Be("20:00 – 08/10 10:00");
        Cartao(colunas[2], "Plantão de protocolo").QuerySelector(".aj-evento__horario")!.TextContent.Should().Be("06/10 20:00 – 08/10 10:00");
        Cartao(colunas[3], "Plantão de protocolo").QuerySelector(".aj-evento__horario")!.TextContent.Should().Be("06/10 20:00 – 10:00");

        var secoes = Secoes(semana);
        (string Inicio, string Fim) Hora(int indice)
        {
            var cartao = Cartao(secoes[indice], "Plantão de protocolo");
            return (cartao.QuerySelector("span.aj-evento__hora > b")!.TextContent,
                cartao.QuerySelector("span.aj-evento__hora > span")!.TextContent);
        }
        Hora(1).Should().Be(("20:00", "…"));
        Hora(2).Should().Be(("…", "…"));
        Hora(3).Should().Be(("…", "10:00"));
        Titulos(secoes[4]).Should().Equal("Recurso ordinário");
    }

    [Fact]
    public void Evento_que_termina_a_meia_noite_nao_aparece_no_dia_seguinte()
    {
        var jantar = QuadroDaAgenda.Evento(new DateTime(2026, 10, 9, 22, 0, 0), 120, TipoEvento.Outro, "Jantar de encerramento");

        var semana = Semana([.. QuadroDaAgenda.Semana, jantar]);

        var colunas = Colunas(semana);
        Titulos(colunas[4]).Should().Equal("Recurso ordinário", "Jantar de encerramento");
        Cartao(colunas[4], "Jantar de encerramento").QuerySelector(".aj-evento__horario")!.TextContent.Should().Be("22:00 – 00:00");
        Titulos(colunas[5]).Should().BeEmpty();
        colunas[5].QuerySelector("p.aj-semana__vazio")!.TextContent.Should().Be("Sem eventos");

        var secoes = Secoes(semana);
        Titulos(secoes[4]).Should().Equal("Recurso ordinário", "Jantar de encerramento");
        Titulos(secoes[5]).Should().BeEmpty();
        secoes[5].QuerySelector("p.aj-secao-dia__vazio")!.TextContent.Should().Be("Sem eventos");
    }

    [Fact]
    public void Os_cartoes_levam_a_ficha_com_o_periodo_de_origem()
    {
        var semana = Semana(QuadroDaAgenda.Semana);

        var esperados = QuadroDaAgenda.Semana.Select(e => $"/agenda/{e.Id}?visao=semana&data=2026-10-08").ToArray();
        semana.FindAll("div.aj-so-desktop a.aj-evento").Select(a => a.GetAttribute("href")).Should().Equal(esperados);
        semana.FindAll("div.aj-tablet-mobile a.aj-evento").Select(a => a.GetAttribute("href")).Should().Equal(esperados);

        // Sem nada no endereço (a agenda aberta pelo menu), a ficha não leva origem.
        var semOrigem = Semana(QuadroDaAgenda.Semana, PeriodoDaAgenda.Ler(null, null, QuadroDaAgenda.Hoje));
        Cartao(Colunas(semOrigem)[0], "Pauta da semana").GetAttribute("href")
            .Should().Be($"/agenda/{QuadroDaAgenda.PautaDaSemana.Id}");
        Cartao(Secoes(semOrigem)[0], "Pauta da semana").GetAttribute("href")
            .Should().Be($"/agenda/{QuadroDaAgenda.PautaDaSemana.Id}");
    }

    [Fact]
    public void Hoje_fora_da_semana_nao_marca_nenhum_dia()
    {
        var semana = Semana(QuadroDaAgenda.Semana, hoje: new DateOnly(2026, 10, 15));

        var colunas = Colunas(semana);
        colunas.Select(c => c.ClassName).Should().Equal(
            "aj-semana__dia", "aj-semana__dia", "aj-semana__dia", "aj-semana__dia",
            "aj-semana__dia", "aj-semana__dia aj-semana__dia--folga", "aj-semana__dia aj-semana__dia--folga");
        colunas[3].QuerySelector("span.aj-semana__sigla")!.TextContent.Should().Be("Qui");
        colunas[3].QuerySelector("h2")!.GetAttribute("aria-label").Should().Be("quinta-feira, 08/10/2026");

        var secoes = Secoes(semana);
        secoes.Should().HaveCount(7).And.OnlyContain(s => s.ClassName == "aj-secao-dia");
        secoes[3].QuerySelector("span.aj-secao-dia__sigla")!.TextContent.Should().Be("Qui");
    }
}

/// <summary>
/// O cartão de evento desenhado sozinho, nas cinco formas: a classe da forma e do tipo, o link para a ficha e as
/// linhas que cada forma mostra (os quadros).
/// </summary>
public class CartaoDeEventoTests : TelaInterativa
{
    private const string Destino = "/agenda/0199a3f2-7c1e-7000-8000-0000000a0001?visao=dia&data=2026-10-08";

    private IRenderedComponent<CartaoDeEvento> Cartao(
        EventoResumo evento, DateOnly dia, FormaDoCartao forma, string? classesDaRegua = null) =>
        Desenhar<CartaoDeEvento>(p => p
            .Add(c => c.Evento, evento)
            .Add(c => c.Dia, dia)
            .Add(c => c.Forma, forma)
            .Add(c => c.Href, Destino)
            .Add(c => c.ClassesDaRegua, classesDaRegua));

    private static string[] Linhas(IElement elemento) =>
        elemento.Children.Select(linha => $"{linha.ClassName}: {linha.TextContent}").ToArray();

    [Theory]
    [InlineData(FormaDoCartao.Coluna, "aj-evento aj-evento--coluna aj-evento--audiencia")]
    [InlineData(FormaDoCartao.Secao, "aj-evento aj-evento--audiencia")]
    [InlineData(FormaDoCartao.Folgado, "aj-evento aj-evento--folgado aj-evento--audiencia")]
    [InlineData(FormaDoCartao.Linha, "aj-evento aj-evento--linha aj-evento--audiencia")]
    [InlineData(FormaDoCartao.Mes, "aj-evento aj-evento--mes aj-evento--audiencia")]
    public void Cada_forma_e_um_link_para_a_ficha_com_a_classe_da_forma_e_do_tipo(FormaDoCartao forma, string classe)
    {
        var cartao = Cartao(QuadroDaAgenda.AudienciaDeInstrucao, new DateOnly(2026, 10, 5), forma);

        var link = cartao.Find("a");
        link.ClassName.Should().Be(classe);
        link.GetAttribute("href").Should().Be(Destino);
        link.QuerySelector(".aj-evento__titulo")!.TextContent.Should().Be("Audiência de instrução");
    }

    [Theory]
    [InlineData(TipoEvento.Audiencia, "aj-evento aj-evento--audiencia", "Audiência")]
    [InlineData(TipoEvento.Reuniao, "aj-evento aj-evento--reuniao", "Reunião")]
    [InlineData(TipoEvento.Prazo, "aj-evento aj-evento--prazo", "Prazo")]
    [InlineData(TipoEvento.Outro, "aj-evento aj-evento--outro", "Outro")]
    public void Cada_tipo_tem_a_sua_classe_e_o_seu_rotulo(TipoEvento tipo, string classe, string rotulo)
    {
        var evento = QuadroDaAgenda.Evento(new DateTime(2026, 10, 8, 9, 0, 0), 60, tipo, "Compromisso");

        var link = Cartao(evento, QuadroDaAgenda.Hoje, FormaDoCartao.Secao).Find("a");

        link.ClassName.Should().Be(classe);
        link.QuerySelector(".aj-evento__tipo")!.TextContent.Should().Be(rotulo);
    }

    [Fact]
    public void Folgado_tem_a_hora_em_coluna_local_e_cliente_numa_linha_e_os_participantes_na_outra()
    {
        var link = Cartao(QuadroDaAgenda.AudienciaDeInstrucao, new DateOnly(2026, 10, 5), FormaDoCartao.Folgado).Find("a");

        link.QuerySelector("span.aj-evento__hora > b")!.TextContent.Should().Be("14:00");
        link.QuerySelector("span.aj-evento__hora > span")!.TextContent.Should().Be("15:30");
        Linhas(link.QuerySelector("span.aj-evento__corpo")!).Should().Equal(
            "aj-evento__tipo: Audiência", "aj-evento__titulo: Audiência de instrução",
            "aj-evento__meta: 2ª Vara do Trabalho · Transportes Litoral Ltda.", "aj-evento__meta: Beatriz Lins");

        // Sem local, sem cliente e sem participantes: só o tipo e o título no corpo.
        var despacho = QuadroDaAgenda.Evento(new DateTime(2026, 10, 8, 8, 0, 0), 60, TipoEvento.Outro, "Despacho interno");
        Linhas(Cartao(despacho, QuadroDaAgenda.Hoje, FormaDoCartao.Folgado).Find("span.aj-evento__corpo")).Should().Equal(
            "aj-evento__tipo: Outro", "aj-evento__titulo: Despacho interno");
    }

    [Fact]
    public void Linha_da_regua_tem_tipo_horario_titulo_e_local_com_participantes_e_leva_as_classes_de_posicao()
    {
        var link = Cartao(QuadroDaAgenda.ProtocoloDeDocumentos, QuadroDaAgenda.Hoje, FormaDoCartao.Linha,
            "aj-regua__evento aj-regua__evento--inicio-6 aj-regua__evento--quartos-4").Find("a");

        link.ClassName.Should().Be(
            "aj-evento aj-evento--linha aj-evento--outro aj-regua__evento aj-regua__evento--inicio-6 aj-regua__evento--quartos-4");
        Linhas(link).Should().Equal(
            "aj-evento__tipo: Outro", "aj-evento__horario: 09:30 – 10:30", "aj-evento__titulo: Protocolo de documentos",
            "aj-evento__meta: Cartório do 1º Ofício · Otávio Brandão");
    }

    [Fact]
    public void Item_do_mes_tem_o_tipo_so_para_leitor_a_hora_de_inicio_no_primeiro_dia_e_o_titulo()
    {
        var link = Cartao(QuadroDaAgenda.PautaDaSemana, new DateOnly(2026, 10, 5), FormaDoCartao.Mes).Find("a");

        Linhas(link).Should().Equal("aj-so-leitor: Reunião, ", "aj-evento__horario: 09:00", "aj-evento__titulo: Pauta da semana");

        // Evento de vários dias: nos dias seguintes ao do início, sem hora.
        var plantao = QuadroDaAgenda.Evento(new DateTime(2026, 10, 6, 20, 0, 0), 38 * 60, TipoEvento.Prazo, "Plantão de protocolo");
        Linhas(Cartao(plantao, new DateOnly(2026, 10, 7), FormaDoCartao.Mes).Find("a")).Should().Equal(
            "aj-so-leitor: Prazo, ", "aj-evento__titulo: Plantão de protocolo");
    }

    [Fact]
    public void Participante_sem_nome_aparece_como_usuario_nao_encontrado()
    {
        var evento = QuadroDaAgenda.Evento(new DateTime(2026, 10, 8, 9, 0, 0), 60, TipoEvento.Reuniao, "Reunião de equipe",
            "Sala de reuniões", null, "Marina Couto", "");

        var link = Cartao(evento, QuadroDaAgenda.Hoje, FormaDoCartao.Coluna).Find("a");

        link.QuerySelectorAll(".aj-evento__meta").Select(m => m.TextContent).Should().Equal("Marina Couto, Usuário não encontrado");
    }
}
```

Run: `dotnet build -m:1`
Expected: não compila — `SemanaDaAgenda`, `CartaoDeEvento` e `FormaDoCartao` não existem (CS0246). É o RED.

- [ ] **Step 2: A forma do cartão**

Crie `src/AtividadesJuridicas.Web/Components/Agenda/FormaDoCartao.cs`:

```csharp
namespace AtividadesJuridicas.Web.Components.Agenda;

/// <summary>Onde o cartão de evento está, e por isso quais linhas ele mostra (os quadros da agenda).</summary>
public enum FormaDoCartao
{
    /// <summary>Coluna da Semana do desktop: tipo, horário, título, local, cliente e participantes, um por linha.</summary>
    Coluna,

    /// <summary>Seção do dia (tablet; Dia do tablet e Semana do mobile): hora em coluna, tipo, título e o local (ou o cliente).</summary>
    Secao,

    /// <summary>Lista do dia no mobile: hora em coluna, tipo, título, "local · cliente" e os participantes.</summary>
    Folgado,

    /// <summary>Régua do Dia no desktop: tipo, horário, título e "local · cliente · participantes", em linha.</summary>
    Linha,

    /// <summary>Célula do Mês (desktop e tablet): o tipo só para leitor, a hora de início (no dia em que começa) e o título.</summary>
    Mes,
}
```

- [ ] **Step 3: O cartão de evento**

Crie `src/AtividadesJuridicas.Web/Components/Agenda/CartaoDeEvento.razor`. Classes da Tarefa 1: `aj-evento--folgado`, `aj-evento--linha`, `aj-evento--mes`, `aj-evento__meta` (as outras já existem). O cartão é um link (`a.aj-evento`); o ponto do tipo é o `::before` de `aj-evento__tipo` (nada de `span` para ele). Linha vazia não aparece. O processo não aparece em nenhuma forma.

```razor
@inject FusoDoEscritorio Fuso

@* Um evento da agenda: o cartão inteiro é o link para a ficha. A forma diz onde ele está e quais linhas mostra (os
   quadros): Coluna (Semana do desktop), Secao (tablet e Semana do mobile), Folgado (lista do dia no mobile), Linha
   (régua do Dia no desktop) e Mes (célula do mês). Linha sem conteúdo não aparece; o processo fica só na ficha. *@
<a class="@Classe" href="@Href">
    @switch (Forma)
    {
        case FormaDoCartao.Coluna:
            <span class="aj-evento__tipo">@RotuloDoTipo</span>
            <span class="aj-evento__horario">@RotulosDeAgenda.Intervalo(Fuso, Evento, Dia)</span>
            <span class="aj-evento__titulo">@Evento.Titulo</span>
            if (Local is not null)
            {
                <span class="aj-evento__local">@Local</span>
            }
            if (Cliente is not null)
            {
                <span class="aj-evento__meta">@Cliente</span>
            }
            if (Pessoas is not null)
            {
                <span class="aj-evento__meta">@Pessoas</span>
            }
            break;

        case FormaDoCartao.Secao:
            <span class="aj-evento__hora"><b>@RotulosDeAgenda.HoraDeInicio(Fuso, Evento, Dia)</b><span>@RotulosDeAgenda.HoraDeFim(Fuso, Evento, Dia)</span></span>
            <span class="aj-evento__corpo">
                <span class="aj-evento__tipo">@RotuloDoTipo</span>
                <span class="aj-evento__titulo">@Evento.Titulo</span>
                @if (LinhaDoTablet is not null)
                {
                    <span class="aj-evento__local">@LinhaDoTablet</span>
                }
            </span>
            break;

        case FormaDoCartao.Folgado:
            <span class="aj-evento__hora"><b>@RotulosDeAgenda.HoraDeInicio(Fuso, Evento, Dia)</b><span>@RotulosDeAgenda.HoraDeFim(Fuso, Evento, Dia)</span></span>
            <span class="aj-evento__corpo">
                <span class="aj-evento__tipo">@RotuloDoTipo</span>
                <span class="aj-evento__titulo">@Evento.Titulo</span>
                @if (LinhaDoMobile is not null)
                {
                    <span class="aj-evento__meta">@LinhaDoMobile</span>
                }
                @if (Pessoas is not null)
                {
                    <span class="aj-evento__meta">@Pessoas</span>
                }
            </span>
            break;

        case FormaDoCartao.Linha:
            <span class="aj-evento__tipo">@RotuloDoTipo</span>
            <span class="aj-evento__horario">@RotulosDeAgenda.Intervalo(Fuso, Evento, Dia)</span>
            <span class="aj-evento__titulo">@Evento.Titulo</span>
            if (LinhaDaRegua is not null)
            {
                <span class="aj-evento__meta">@LinhaDaRegua</span>
            }
            break;

        case FormaDoCartao.Mes:
            @* O item do mês só tem a cor para dizer o tipo: o leitor de tela ouve o tipo antes da hora. *@
            <span class="aj-so-leitor">@(RotuloDoTipo + ", ")</span>
            if (ComecaNoDia)
            {
                <span class="aj-evento__horario">@RotulosDeAgenda.HoraDeInicio(Fuso, Evento, Dia)</span>
            }
            <span class="aj-evento__titulo">@Evento.Titulo</span>
            break;
    }
</a>

@code {
    [Parameter, EditorRequired] public EventoResumo Evento { get; set; } = default!;

    /// <summary>O dia em que o cartão está (o evento de vários dias aparece em cada um).</summary>
    [Parameter, EditorRequired] public DateOnly Dia { get; set; }

    [Parameter, EditorRequired] public FormaDoCartao Forma { get; set; }

    /// <summary>A ficha do evento com o período de origem (<c>PeriodoDaAgenda.Ficha</c>).</summary>
    [Parameter, EditorRequired] public string Href { get; set; } = string.Empty;

    /// <summary>Só na forma Linha: as classes de posição na régua (<c>aj-regua__evento</c> e as de início, quartos e parte).</summary>
    [Parameter] public string? ClassesDaRegua { get; set; }

    /// <summary>O sufixo das classes do tipo: "audiencia", "reuniao", "prazo", "outro" (também o dos pontos da faixa).</summary>
    public static string Sufixo(TipoEvento tipo) => tipo switch
    {
        TipoEvento.Audiencia => "audiencia",
        TipoEvento.Reuniao => "reuniao",
        TipoEvento.Prazo => "prazo",
        _ => "outro",
    };

    private string Classe => Forma switch
    {
        FormaDoCartao.Coluna => $"aj-evento aj-evento--coluna aj-evento--{Sufixo(Evento.Tipo)}",
        FormaDoCartao.Folgado => $"aj-evento aj-evento--folgado aj-evento--{Sufixo(Evento.Tipo)}",
        FormaDoCartao.Linha => ClassesDaRegua is null
            ? $"aj-evento aj-evento--linha aj-evento--{Sufixo(Evento.Tipo)}"
            : $"aj-evento aj-evento--linha aj-evento--{Sufixo(Evento.Tipo)} {ClassesDaRegua}",
        FormaDoCartao.Mes => $"aj-evento aj-evento--mes aj-evento--{Sufixo(Evento.Tipo)}",
        _ => $"aj-evento aj-evento--{Sufixo(Evento.Tipo)}",
    };

    private string RotuloDoTipo => RotulosDeAgenda.Tipo(Evento.Tipo);
    private string? Local => Texto(Evento.Local);
    private string? Cliente => Texto(Evento.ClienteNome);
    private string? LinhaDoTablet => Texto(RotulosDeAgenda.LinhaDoTablet(Evento));
    private string? LinhaDoMobile => Texto(RotulosDeAgenda.LinhaDoMobile(Evento));
    private string? LinhaDaRegua => Texto(RotulosDeAgenda.LinhaDaRegua(Evento));

    // Os participantes na ordem do servidor; quem não foi encontrado (nome vazio) aparece como "Usuário não encontrado".
    private string? Pessoas => Texto(RotulosDeAgenda.Participantes(Evento));

    // No mês, a hora só aparece no dia em que o evento começa.
    private bool ComecaNoDia => DateOnly.FromDateTime(Fuso.ParaLocal(Evento.InicioEm)) == Dia;

    private static string? Texto(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor;
}
```

Notas para o implementador:
- Dentro de um `case` o Razor está em código: o `if` vai sem `@`; dentro de um elemento (`span.aj-evento__corpo`) vai com `@if`. Se o compilador recusar o comentário `@* … *@` dentro do `case`, troque-o por um comentário C# (`// …`).
- As linhas opcionais usam propriedades, não variáveis de padrão (`is { } x`): variáveis de padrão declaradas em `case` diferentes do mesmo `switch` dividem o escopo e colidem (CS0128).
- `LinhaDoTablet`, `LinhaDoMobile`, `LinhaDaRegua` e `Participantes` são da Tarefa 2; o `Texto` aceita tanto `string` vazia quanto `null` como "sem linha" (evento sem participantes não tem a linha).

- [ ] **Step 4: A Semana**

Crie `src/AtividadesJuridicas.Web/Components/Agenda/SemanaDaAgenda.razor`. Classes da Tarefa 1: `aj-semana` (`__dia`, `__cabeca`, `__sigla`, `__numero`, `__vazio`, `__dia--hoje`, `__dia--folga`) e `aj-secao-dia` (`--hoje`, `__cabeca`, `__sigla`, `__numero`, `__eventos`, `__vazio`). Os dois invólucros de visão têm só a classe de visibilidade.

```razor
@using System.Globalization
@inject FusoDoEscritorio Fuso

@* A Semana da agenda (segunda a domingo da data do período). Desktop: a grade de sete colunas, uma por dia, com os
   cartões empilhados na ordem de início; hoje sublinhado, sábado e domingo com o número apagado, dia vazio em caixa
   tracejada. Tablet e mobile: uma seção por dia, com os cartões em duas colunas (uma no mobile, pelo CSS); hoje com
   a borda verde e a sigla "Hoje". As duas formas leem os mesmos eventos; cada evento aparece em todo dia que toca. *@
<div class="aj-so-desktop">
    <div class="aj-semana">
        @foreach (var dia in Periodo.Semana)
        {
            var doDia = _porDia[dia];
            <section class="@ClasseDaColuna(dia)" @key="dia">
                <h2 class="aj-semana__cabeca" aria-label="@RotulosDeAgenda.RotuloLongo(dia, dia == Hoje)"><span class="aj-semana__sigla">@SiglaDaColuna(dia)</span><span class="aj-semana__numero">@Numero(dia)</span></h2>
                @if (doDia.Count == 0)
                {
                    <p class="aj-semana__vazio">Sem eventos</p>
                }
                @foreach (var evento in doDia)
                {
                    <CartaoDeEvento @key="evento.Id" Evento="evento" Dia="dia" Forma="FormaDoCartao.Coluna" Href="@Ficha(evento)" />
                }
            </section>
        }
    </div>
</div>
<div class="aj-tablet-mobile">
    @foreach (var dia in Periodo.Semana)
    {
        var doDia = _porDia[dia];
        <section class="@(dia == Hoje ? "aj-secao-dia aj-secao-dia--hoje" : "aj-secao-dia")" @key="dia">
            <h2 class="aj-secao-dia__cabeca" aria-label="@RotulosDeAgenda.RotuloLongo(dia, dia == Hoje)"><span class="aj-secao-dia__sigla">@(dia == Hoje ? "Hoje" : RotulosDeAgenda.SiglaDoDia(dia))</span><span class="aj-secao-dia__numero">@Numero(dia)</span></h2>
            <div class="aj-secao-dia__eventos">
                @foreach (var evento in doDia)
                {
                    <CartaoDeEvento @key="evento.Id" Evento="evento" Dia="dia" Forma="FormaDoCartao.Secao" Href="@Ficha(evento)" />
                }
                @if (doDia.Count == 0)
                {
                    <p class="aj-secao-dia__vazio">Sem eventos</p>
                }
            </div>
        </section>
    }
</div>

@code {
    [Parameter, EditorRequired] public PeriodoDaAgenda Periodo { get; set; } = default!;
    [Parameter, EditorRequired] public IReadOnlyList<EventoResumo> Eventos { get; set; } = [];

    /// <summary>Hoje no fuso do escritório (a página calcula; os testes fixam).</summary>
    [Parameter, EditorRequired] public DateOnly Hoje { get; set; }

    private Dictionary<DateOnly, IReadOnlyList<EventoResumo>> _porDia = new();

    protected override void OnParametersSet() =>
        // Cada evento em todo dia que toca (o de vários dias em cada um; o que termina à meia-noite em ponto não
        // passa ao dia seguinte). A ordem é a do início; no empate fica a do servidor (o id), porque o OrderBy é estável.
        _porDia = Periodo.Semana.ToDictionary(
            dia => dia,
            dia => (IReadOnlyList<EventoResumo>)Eventos
                .Where(evento => RotulosDeAgenda.TocaODia(Fuso, evento, dia))
                .OrderBy(evento => evento.InicioEm)
                .ToList());

    private string Ficha(EventoResumo evento) => PeriodoDaAgenda.Ficha(evento.Id, Periodo.Consulta);

    // Hoje vence a folga: um sábado que é hoje fica com o círculo verde.
    private string ClasseDaColuna(DateOnly dia) =>
        dia == Hoje ? "aj-semana__dia aj-semana__dia--hoje"
        : dia.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? "aj-semana__dia aj-semana__dia--folga"
        : "aj-semana__dia";

    private string SiglaDaColuna(DateOnly dia) =>
        dia == Hoje ? $"{RotulosDeAgenda.SiglaDoDia(dia)} · hoje" : RotulosDeAgenda.SiglaDoDia(dia);

    private static string Numero(DateOnly dia) => dia.Day.ToString("00", CultureInfo.InvariantCulture);
}
```

Nenhum `style`, nenhuma classe fora de `aj-`; as classes montadas por expressão (`ClasseDaColuna`, a da seção e a do cartão) valem a revisão. Os dois `@key="dia"` estão em pais diferentes, e em cada pai só os cartões têm chave.

- [ ] **Step 5: Build e testes**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros (a agenda de hoje não usa os componentes novos e continua compilando).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~SemanaDaAgendaTests|FullyQualifiedName~CartaoDeEventoTests"`
Expected: 22 passando (9 da Semana; 13 do cartão: 5 + 4 casos das duas teorias e 4 fatos).

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~WebSoPeloDespachante"`
Expected: todos passando (os componentes novos não citam repositório nem contexto).

- [ ] **Step 6: Commit**

Pela ferramenta PowerShell, confira o branch (`git branch --show-current` deve dizer `worktree-feat+fase-9f-agenda`). Grave a mensagem num arquivo UTF-8 sem BOM e faça o commit com `git commit -F`:

```
git add src/AtividadesJuridicas.Web/Components/Agenda tests/AtividadesJuridicas.IntegrationTests
git commit -F <arquivo da mensagem>
```

Mensagem:

```
feat(web): cartão de evento nas cinco formas e Semana da agenda no design system

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

---

### Task 7: Dia — régua do desktop, seção do tablet, faixa e lista do mobile

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Agenda/DiaDaAgenda.razor`, `FaixaDeDias.razor`
- Test: `tests/AtividadesJuridicas.IntegrationTests/DiaDaAgendaTests.cs`

**Interfaces:**
- Consumes: `ReguaDoDia`, `PeriodoDaAgenda`, `RotulosDeAgenda` (Tarefa 2, inclusive `TituloDoDiaNoDesktop` e `TituloDoDiaNoMobile`), `CartaoDeEvento` (Tarefa 6), `EstadoDaLista.Icone`/`Dica` (Tarefa 3), `QuadroDaAgenda` (testes, Tarefa 6).
- Produces:
  - `public static string DiaDaAgenda.ClassesDaRegua(EventoNaRegua)` (as classes de posição do cartão na régua).
  - `DiaDaAgenda`: `PeriodoDaAgenda Periodo`, `IReadOnlyList<EventoResumo> Eventos`, `DateOnly Hoje`. Emite `aj-so-desktop` (`aj-dia` com a régua), `aj-so-tablet` (uma `aj-secao-dia`), `aj-so-mobile` (`aj-dia-escolhido` com os cartões `Folgado` ou o vazio com ícone e dica).
  - `FaixaDeDias`: `PeriodoDaAgenda Periodo`, `IReadOnlyList<EventoResumo> Eventos`, `DateOnly Hoje`, `EventCallback<DateOnly> AoEscolher`. Emite a `aj-faixa-dias` da barra do mobile (sete botões com `aria-pressed`, `aria-label`, sigla, número e pontos por tipo).

**Regras:**
- A régua com as classes da Tarefa 1 a partir da `ReguaDoDia`: os rótulos de hora, a grade `aj-regua__grade--horas-{n}` e cada cartão `Linha` com `aj-regua__evento--inicio-{q} aj-regua__evento--quartos-{q}` e, em grupo, `aj-regua__evento--parte-{k}-de-{n}`.
- Dia sem eventos no desktop: a régua padrão e `0 eventos`; no tablet, a seção com `Sem eventos`; no mobile, `Nenhum evento neste dia` com a dica.
- Pontos da faixa: um por evento do dia, até seis.

**Testes obrigatórios:** o dia do quadro (08/10: os dois cartões nas posições `inicio-6`/`quartos-4` e `inicio-28`/`quartos-4`, `2 eventos`, hoje marcado); simultâneos lado a lado e o evento curto (Review Focus 3); a régua estendida e o evento que atravessa a meia-noite (Review Focus 2); o vazio nas três larguras; a faixa (o escolhido, o hoje, os pontos, o `AoEscolher` com a data).

- [ ] **Step 1: Testes (devem falhar)**

Antes de escrever, releia na cópia local dos quadros o Dia de `AgendaA.dc.html` (`section` do dia com o `h2` de número, nome e total, a coluna das horas `aria-hidden`, a grade com uma linha por hora — no código, fundo da `aj-regua__grade` — e os dois cartões em linha), a barra superior e a lista de `AgendaMobile.dc.html` (a faixa de sete botões com `aria-pressed`, sigla, número e pontos; o título do dia com o total; os cartões folgados; o vazio com ícone e dica), e as seções 1.3, 2 (item 8), 5, 7.1, 7.2, 8.7 e 8.9 de `quadros-agenda.md`.

Os testes são de componente (bUnit, base `TelaInterativa`), com os eventos do quadro do auxiliar `QuadroDaAgenda` (`tests/AtividadesJuridicas.IntegrationTests/QuadroDaAgenda.cs`, da Tarefa 6): `QuadroDaAgenda.Hoje` (quinta-feira, 08/10/2026), os oito eventos da semana em `QuadroDaAgenda.Semana` e cada um por nome (`PautaDaSemana`, `AudienciaDeInstrucao`, `Contestacao`, `Alinhamento`, `AudienciaDeConciliacao`, `ProtocoloDeDocumentos`, `RevisaoDeMinutas`, `RecursoOrdinario`), e `QuadroDaAgenda.Evento(DateTime inicioLocal, int minutos, TipoEvento tipo, string titulo, string? local = null, string? cliente = null, params string[] participantes)` para os eventos extras (início no horário do escritório).

As contas da régua (56px por hora, quartos de hora a partir da hora inicial): no 08/10 a régua vai das 08:00 às 19:00 (11 horas); "Protocolo de documentos" 09:30–10:30 começa 1h30 depois das 08:00 = 6 quartos e dura 4 quartos (`inicio-6 quartos-4`); "Revisão de minutas" 15:00–16:00 começa 7h depois = 28 quartos (`inicio-28 quartos-4`).

Crie `tests/AtividadesJuridicas.IntegrationTests/DiaDaAgendaTests.cs`, com duas classes (o Dia e a faixa de dias):

```csharp
using AngleSharp.Dom;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Web.Components.Agenda;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O Dia da agenda com os eventos do quadro: a régua do desktop (cartões posicionados por quartos de hora, eventos
/// simultâneos lado a lado, régua estendida, evento recortado à meia-noite), a seção do tablet e a lista do mobile.
/// </summary>
public class DiaDaAgendaTests : TelaInterativa
{
    private IRenderedComponent<DiaDaAgenda> Dia(DateOnly data, IReadOnlyList<EventoResumo> eventos) =>
        Desenhar<DiaDaAgenda>(p => p
            .Add(c => c.Periodo, new PeriodoDaAgenda(VisaoDaAgenda.Dia, data, true))
            .Add(c => c.Eventos, eventos)
            .Add(c => c.Hoje, QuadroDaAgenda.Hoje));

    private static IElement Desktop(IRenderedComponent<DiaDaAgenda> dia) => dia.Find("div.aj-so-desktop > section");
    private static IElement Tablet(IRenderedComponent<DiaDaAgenda> dia) => dia.Find("div.aj-so-tablet > section");
    private static IElement Mobile(IRenderedComponent<DiaDaAgenda> dia) => dia.Find("div.aj-so-mobile");

    private static string[] Horas(IElement desktop) =>
        desktop.QuerySelectorAll("div.aj-regua > div.aj-regua__horas > span.aj-regua__hora").Select(h => h.TextContent).ToArray();

    private static IElement Grade(IElement desktop) => desktop.QuerySelector("div.aj-regua > div.aj-regua__grade")!;

    private static IElement Cartao(IElement bloco, string titulo) =>
        bloco.QuerySelectorAll("a.aj-evento").Single(a => a.QuerySelector(".aj-evento__titulo")!.TextContent == titulo);

    private static string[] Titulos(IElement bloco) =>
        bloco.QuerySelectorAll("a.aj-evento .aj-evento__titulo").Select(t => t.TextContent).ToArray();

    private static string[] Linhas(IElement elemento) =>
        elemento.Children.Select(linha => $"{linha.ClassName}: {linha.TextContent}").ToArray();

    private static readonly string[] HorasPadrao =
        ["08:00", "09:00", "10:00", "11:00", "12:00", "13:00", "14:00", "15:00", "16:00", "17:00", "18:00"];

    [Fact]
    public void Dia_do_quadro_tem_a_regua_padrao_com_os_dois_cartoes_na_posicao_e_hoje_marcado_nas_tres_visoes()
    {
        var dia = Dia(QuadroDaAgenda.Hoje, QuadroDaAgenda.Semana);

        var desktop = Desktop(dia);
        desktop.ClassName.Should().Be("aj-dia aj-dia--hoje");
        desktop.GetAttribute("aria-label").Should().Be("quinta-feira, 08/10/2026");
        Linhas(desktop.QuerySelector("h2.aj-dia__cabeca")!).Should().Equal(
            "aj-dia__numero: 08", ": Quinta-feira · hoje", "aj-dia__total: 2 eventos");
        desktop.QuerySelector("div.aj-regua__horas")!.GetAttribute("aria-hidden").Should().Be("true");
        Horas(desktop).Should().Equal(HorasPadrao);
        Grade(desktop).ClassName.Should().Be("aj-regua__grade aj-regua__grade--horas-11");
        // As linhas das horas são fundo da grade (a classe de horas): a grade só tem os cartões.
        Grade(desktop).Children.Should().HaveCount(2).And.OnlyContain(c => c.ClassList.Contains("aj-regua__evento"));

        // Só os eventos do dia, nas posições do quadro (09:30–10:30 e 15:00–16:00).
        Titulos(desktop).Should().BeEquivalentTo("Protocolo de documentos", "Revisão de minutas");
        var protocolo = Cartao(desktop, "Protocolo de documentos");
        protocolo.ClassName.Should().Be(
            "aj-evento aj-evento--linha aj-evento--outro aj-regua__evento aj-regua__evento--inicio-6 aj-regua__evento--quartos-4");
        Linhas(protocolo).Should().Equal(
            "aj-evento__tipo: Outro", "aj-evento__horario: 09:30 – 10:30", "aj-evento__titulo: Protocolo de documentos",
            "aj-evento__meta: Cartório do 1º Ofício · Otávio Brandão");
        var revisao = Cartao(desktop, "Revisão de minutas");
        revisao.ClassName.Should().Be(
            "aj-evento aj-evento--linha aj-evento--reuniao aj-regua__evento aj-regua__evento--inicio-28 aj-regua__evento--quartos-4");
        revisao.QuerySelector(".aj-evento__meta")!.TextContent.Should().Be("Sala de reuniões · Marina Couto, Camila Prado");
        protocolo.GetAttribute("href").Should().Be($"/agenda/{QuadroDaAgenda.ProtocoloDeDocumentos.Id}?visao=dia&data=2026-10-08");

        var tablet = Tablet(dia);
        tablet.ClassName.Should().Be("aj-secao-dia aj-secao-dia--hoje");
        tablet.QuerySelector("h2.aj-secao-dia__cabeca")!.GetAttribute("aria-label").Should().Be("quinta-feira, 08/10/2026, hoje");
        tablet.QuerySelector("span.aj-secao-dia__sigla")!.TextContent.Should().Be("Hoje");
        tablet.QuerySelector("span.aj-secao-dia__numero")!.TextContent.Should().Be("08");
        tablet.QuerySelectorAll("div.aj-secao-dia__eventos > a.aj-evento").Select(a => a.ClassName).Should().Equal(
            "aj-evento aj-evento--outro", "aj-evento aj-evento--reuniao");
        Titulos(tablet).Should().Equal("Protocolo de documentos", "Revisão de minutas");
        tablet.QuerySelector("p.aj-secao-dia__vazio").Should().BeNull();

        var mobile = Mobile(dia);
        mobile.QuerySelector("div.aj-dia-escolhido > h2")!.TextContent.Should().Be("Quinta, 08/10 · hoje");
        mobile.QuerySelector("div.aj-dia-escolhido > span.aj-dia-escolhido__total")!.TextContent.Should().Be("2 eventos");
        mobile.QuerySelectorAll("a.aj-evento").Select(a => a.ClassName).Should().Equal(
            "aj-evento aj-evento--folgado aj-evento--outro", "aj-evento aj-evento--folgado aj-evento--reuniao");
        Titulos(mobile).Should().Equal("Protocolo de documentos", "Revisão de minutas");
        mobile.QuerySelector(".aj-estado-lista").Should().BeNull();

        // Os eventos dos outros dias da semana não entram em nenhuma das três (seis cartões: dois por visão).
        var titulos = dia.FindAll("a.aj-evento .aj-evento__titulo").Select(t => t.TextContent).ToList();
        titulos.Should().HaveCount(6).And.Contain("Protocolo de documentos").And.NotContain("Pauta da semana")
            .And.NotContain("Recurso ordinário");
    }

    [Fact]
    public void Eventos_simultaneos_ficam_lado_a_lado_o_seguinte_volta_a_largura_inteira_e_o_curto_tem_dois_quartos()
    {
        // 07/10: Alinhamento 10:00–11:30 e Audiência de conciliação 11:00–12:00 se sobrepõem (o conflito do quadro).
        var despacho = QuadroDaAgenda.Evento(new DateTime(2026, 10, 7, 14, 0, 0), 60, TipoEvento.Outro, "Despacho com o cliente");
        var ligacao = QuadroDaAgenda.Evento(new DateTime(2026, 10, 7, 16, 0, 0), 15, TipoEvento.Reuniao, "Ligação rápida");

        var dia = Dia(new DateOnly(2026, 10, 7), [.. QuadroDaAgenda.Semana, despacho, ligacao]);

        var desktop = Desktop(dia);
        Cartao(desktop, "Alinhamento com o cliente").ClassName.Should().Be(
            "aj-evento aj-evento--linha aj-evento--reuniao aj-regua__evento aj-regua__evento--inicio-8 aj-regua__evento--quartos-6 aj-regua__evento--parte-1-de-2");
        Cartao(desktop, "Audiência de conciliação").ClassName.Should().Be(
            "aj-evento aj-evento--linha aj-evento--audiencia aj-regua__evento aj-regua__evento--inicio-12 aj-regua__evento--quartos-4 aj-regua__evento--parte-2-de-2");
        Cartao(desktop, "Despacho com o cliente").ClassName.Should().Be(
            "aj-evento aj-evento--linha aj-evento--outro aj-regua__evento aj-regua__evento--inicio-24 aj-regua__evento--quartos-4");
        // 15 minutos ganham a altura mínima de dois quartos.
        Cartao(desktop, "Ligação rápida").ClassName.Should().Be(
            "aj-evento aj-evento--linha aj-evento--reuniao aj-regua__evento aj-regua__evento--inicio-32 aj-regua__evento--quartos-2");
        Grade(desktop).ClassName.Should().Be("aj-regua__grade aj-regua__grade--horas-11");

        // Dia que não é hoje: sem as marcas de hoje.
        desktop.ClassName.Should().Be("aj-dia");
        Linhas(desktop.QuerySelector("h2.aj-dia__cabeca")!).Should().Equal(
            "aj-dia__numero: 07", ": Quarta-feira", "aj-dia__total: 4 eventos");
        var tablet = Tablet(dia);
        tablet.ClassName.Should().Be("aj-secao-dia");
        tablet.QuerySelector("span.aj-secao-dia__sigla")!.TextContent.Should().Be("Qua");
        tablet.QuerySelector("h2")!.GetAttribute("aria-label").Should().Be("quarta-feira, 07/10/2026");
        Titulos(tablet).Should().Equal("Alinhamento com o cliente", "Audiência de conciliação", "Despacho com o cliente", "Ligação rápida");
        Mobile(dia).QuerySelector("div.aj-dia-escolhido > h2")!.TextContent.Should().Be("Quarta, 07/10");
    }

    [Fact]
    public void Regua_se_estende_ate_a_hora_cheia_do_primeiro_inicio_e_a_seguinte_ao_ultimo_fim()
    {
        // 09/10: um evento às 06:30 e outro que termina às 21:15 levam a régua de 06:00 a 22:00 (16 horas).
        var cedo = QuadroDaAgenda.Evento(new DateTime(2026, 10, 9, 6, 30, 0), 60, TipoEvento.Audiencia, "Audiência cedo");
        var noite = QuadroDaAgenda.Evento(new DateTime(2026, 10, 9, 20, 0, 0), 75, TipoEvento.Reuniao, "Reunião à noite");

        var desktop = Desktop(Dia(new DateOnly(2026, 10, 9), [.. QuadroDaAgenda.Semana, cedo, noite]));

        Horas(desktop).Should().Equal(
            "06:00", "07:00", "08:00", "09:00", "10:00", "11:00", "12:00", "13:00",
            "14:00", "15:00", "16:00", "17:00", "18:00", "19:00", "20:00", "21:00");
        Grade(desktop).ClassName.Should().Be("aj-regua__grade aj-regua__grade--horas-16");
        Grade(desktop).Children.Should().HaveCount(3).And.OnlyContain(c => c.ClassList.Contains("aj-regua__evento"));
        Cartao(desktop, "Audiência cedo").ClassName.Should().EndWith("aj-regua__evento--inicio-2 aj-regua__evento--quartos-4");
        Cartao(desktop, "Recurso ordinário").ClassName.Should().EndWith("aj-regua__evento--inicio-40 aj-regua__evento--quartos-4");
        Cartao(desktop, "Reunião à noite").ClassName.Should().EndWith("aj-regua__evento--inicio-56 aj-regua__evento--quartos-5");
    }

    [Fact]
    public void Evento_que_atravessa_a_meia_noite_entra_recortado_em_cada_dia()
    {
        // 07/10 às 23:00 até 08/10 à 01:00.
        var plantao = QuadroDaAgenda.Evento(new DateTime(2026, 10, 7, 23, 0, 0), 120, TipoEvento.Outro, "Plantão noturno");

        var oitoDeOutubro = Dia(QuadroDaAgenda.Hoje, [plantao]);
        var desktop = Desktop(oitoDeOutubro);
        Horas(desktop).Should().HaveCount(19).And.StartWith("00:00").And.EndWith("18:00");
        Grade(desktop).ClassName.Should().Be("aj-regua__grade aj-regua__grade--horas-19");
        var cartao = Cartao(desktop, "Plantão noturno");
        cartao.ClassName.Should().EndWith("aj-regua__evento--inicio-0 aj-regua__evento--quartos-4");
        cartao.QuerySelector(".aj-evento__horario")!.TextContent.Should().Be("07/10 23:00 – 01:00");
        var noTablet = Cartao(Tablet(oitoDeOutubro), "Plantão noturno");
        noTablet.QuerySelector("span.aj-evento__hora > b")!.TextContent.Should().Be("…");
        noTablet.QuerySelector("span.aj-evento__hora > span")!.TextContent.Should().Be("01:00");
        Mobile(oitoDeOutubro).QuerySelector("span.aj-dia-escolhido__total")!.TextContent.Should().Be("1 evento");

        var seteDeOutubro = Dia(new DateOnly(2026, 10, 7), [plantao]);
        desktop = Desktop(seteDeOutubro);
        Horas(desktop).Should().HaveCount(16).And.StartWith("08:00").And.EndWith("23:00");
        Grade(desktop).ClassName.Should().Be("aj-regua__grade aj-regua__grade--horas-16");
        cartao = Cartao(desktop, "Plantão noturno");
        cartao.ClassName.Should().EndWith("aj-regua__evento--inicio-60 aj-regua__evento--quartos-4");
        cartao.QuerySelector(".aj-evento__horario")!.TextContent.Should().Be("23:00 – 08/10 01:00");
        noTablet = Cartao(Tablet(seteDeOutubro), "Plantão noturno");
        noTablet.QuerySelector("span.aj-evento__hora > b")!.TextContent.Should().Be("23:00");
        noTablet.QuerySelector("span.aj-evento__hora > span")!.TextContent.Should().Be("…");
    }

    [Fact]
    public void Dia_sem_eventos_tem_a_regua_padrao_no_desktop_sem_eventos_no_tablet_e_o_vazio_com_dica_no_mobile()
    {
        var dia = Dia(new DateOnly(2026, 10, 10), QuadroDaAgenda.Semana);

        var desktop = Desktop(dia);
        Horas(desktop).Should().Equal(HorasPadrao);
        Grade(desktop).ClassName.Should().Be("aj-regua__grade aj-regua__grade--horas-11");
        desktop.QuerySelectorAll("a.aj-evento").Should().BeEmpty();
        Linhas(desktop.QuerySelector("h2.aj-dia__cabeca")!).Should().Equal(
            "aj-dia__numero: 10", ": Sábado", "aj-dia__total: 0 eventos");

        var tablet = Tablet(dia);
        tablet.QuerySelector("div.aj-secao-dia__eventos > p.aj-secao-dia__vazio")!.TextContent.Should().Be("Sem eventos");
        tablet.QuerySelectorAll("a.aj-evento").Should().BeEmpty();

        var mobile = Mobile(dia);
        mobile.QuerySelector("div.aj-dia-escolhido > h2")!.TextContent.Should().Be("Sábado, 10/10");
        mobile.QuerySelector("span.aj-dia-escolhido__total")!.TextContent.Should().Be("0 eventos");
        var vazio = mobile.QuerySelector("div.aj-estado-lista")!;
        vazio.QuerySelector("svg.aj-estado-lista__icone").Should().NotBeNull();
        vazio.QuerySelector("strong")!.TextContent.Should().Be("Nenhum evento neste dia");
        vazio.QuerySelector("span.aj-estado-lista__dica")!.TextContent.Should().Be("Toque em + para agendar.");
        mobile.QuerySelectorAll("a.aj-evento").Should().BeEmpty();
    }
}

/// <summary>
/// A faixa de dias da barra do mobile: os sete dias da semana da data, o escolhido, o hoje, um ponto por evento e o
/// toque que avisa a página.
/// </summary>
public class FaixaDeDiasTests : TelaInterativa
{
    private DateOnly? _escolhido;

    private IRenderedComponent<FaixaDeDias> Faixa(DateOnly data, IReadOnlyList<EventoResumo> eventos) =>
        Desenhar<FaixaDeDias>(p => p
            .Add(c => c.Periodo, new PeriodoDaAgenda(VisaoDaAgenda.Dia, data, true))
            .Add(c => c.Eventos, eventos)
            .Add(c => c.Hoje, QuadroDaAgenda.Hoje)
            .Add(c => c.AoEscolher, (DateOnly dia) => _escolhido = dia));

    private static IReadOnlyList<IElement> Dias(IRenderedComponent<FaixaDeDias> faixa) =>
        faixa.FindAll("div.aj-faixa-dias > button");

    private static string[] Pontos(IElement dia) =>
        dia.QuerySelectorAll("span.aj-pontos-evento > span").Select(p => p.ClassName!).ToArray();

    [Fact]
    public void Sete_dias_com_sigla_numero_e_rotulo_o_escolhido_pressionado_e_hoje_marcado()
    {
        var dias = Dias(Faixa(new DateOnly(2026, 10, 7), QuadroDaAgenda.Semana));

        dias.Should().HaveCount(7);
        dias.Select(d => d.GetAttribute("type")).Should().OnlyContain(t => t == "button");
        dias.Select(d => d.GetAttribute("aria-label")).Should().Equal(
            "Segunda, 05/10", "Terça, 06/10", "Quarta, 07/10", "Quinta, 08/10, hoje", "Sexta, 09/10", "Sábado, 10/10", "Domingo, 11/10");
        dias.Select(d => d.QuerySelector("span.aj-faixa-dias__sigla")!.TextContent).Should().Equal(
            "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom");
        dias.Select(d => d.QuerySelector("span.aj-faixa-dias__numero")!.TextContent).Should().Equal(
            "05", "06", "07", "08", "09", "10", "11");
        dias.Select(d => d.GetAttribute("aria-pressed")).Should().Equal("false", "false", "true", "false", "false", "false", "false");
        dias.Select(d => d.ClassName).Should().Equal(
            "aj-faixa-dias__dia", "aj-faixa-dias__dia", "aj-faixa-dias__dia", "aj-faixa-dias__dia aj-faixa-dias__dia--hoje",
            "aj-faixa-dias__dia", "aj-faixa-dias__dia", "aj-faixa-dias__dia");
    }

    [Fact]
    public void Um_ponto_por_evento_do_dia_na_cor_do_tipo_e_a_fileira_mesmo_sem_eventos()
    {
        var dias = Dias(Faixa(QuadroDaAgenda.Hoje, QuadroDaAgenda.Semana));

        Pontos(dias[0]).Should().Equal("aj-ponto-evento aj-ponto-evento--reuniao", "aj-ponto-evento aj-ponto-evento--audiencia");
        Pontos(dias[1]).Should().Equal("aj-ponto-evento aj-ponto-evento--prazo");
        Pontos(dias[3]).Should().Equal("aj-ponto-evento aj-ponto-evento--outro", "aj-ponto-evento aj-ponto-evento--reuniao");
        Pontos(dias[5]).Should().BeEmpty();
        // A fileira existe mesmo vazia (os dias ficam da mesma altura) e não é lida.
        dias[5].QuerySelector("span.aj-pontos-evento")!.GetAttribute("aria-hidden").Should().Be("true");
    }

    [Fact]
    public void No_maximo_seis_pontos_por_dia()
    {
        var muitos = Enumerable.Range(0, 8)
            .Select(i => QuadroDaAgenda.Evento(new DateTime(2026, 10, 10, 8 + i, 0, 0), 30, TipoEvento.Outro, $"Tarefa {i + 1}"))
            .ToList();

        var dias = Dias(Faixa(QuadroDaAgenda.Hoje, [.. QuadroDaAgenda.Semana, .. muitos]));

        Pontos(dias[5]).Should().HaveCount(6).And.OnlyContain(p => p == "aj-ponto-evento aj-ponto-evento--outro");
        Pontos(dias[4]).Should().Equal("aj-ponto-evento aj-ponto-evento--prazo");
    }

    [Fact]
    public void Tocar_num_dia_avisa_a_pagina_com_a_data()
    {
        var faixa = Faixa(QuadroDaAgenda.Hoje, QuadroDaAgenda.Semana);

        Dias(faixa)[4].Click();

        _escolhido.Should().Be(new DateOnly(2026, 10, 9));
    }
}
```

Run: `dotnet build -m:1`
Expected: não compila — `DiaDaAgenda` e `FaixaDeDias` não existem (CS0246). É o RED.

- [ ] **Step 2: O Dia**

Crie `src/AtividadesJuridicas.Web/Components/Agenda/DiaDaAgenda.razor`. Classes da Tarefa 1: `aj-dia` (`--hoje`, `__cabeca`, `__numero`, `__total`), `aj-regua` (`__horas`, `__hora`, `__grade`, `__grade--horas-{n}` (que desenha as linhas das horas no fundo), `__evento`, `__evento--inicio-{q}`, `__evento--quartos-{q}`, `__evento--parte-{k}-de-{n}`), `aj-secao-dia` (com as partes da Tarefa 6), `aj-dia-escolhido` (`__total`). O cartão é o `CartaoDeEvento` da Tarefa 6 (`Evento`, `Dia`, `Forma`, `Href` e, na forma `Linha`, `ClassesDaRegua`, que ele acrescenta depois das suas classes). O vazio do mobile é o `EstadoDaLista` com `Icone` e `Dica` (Tarefa 3). Os títulos do dia são `RotulosDeAgenda.TituloDoDiaNoDesktop` e `TituloDoDiaNoMobile` (Tarefa 2).

```razor
@using System.Globalization
@inject FusoDoEscritorio Fuso

@* O Dia da agenda (o dia da data do período). Desktop: a caixa com o número, o nome do dia e o total, e a régua de
   horas, com os cartões em linha posicionados por classes de grade (início e altura em quartos de hora; eventos
   simultâneos dividem a largura em partes). Tablet: uma seção como as da Semana. Mobile: o título do dia com o total
   e a lista de cartões folgados, ou o vazio com a dica. Os três leem os mesmos eventos (só os que tocam o dia). *@
<div class="aj-so-desktop">
    <section class="@(EHoje ? "aj-dia aj-dia--hoje" : "aj-dia")" aria-label="@RotulosDeAgenda.RotuloLongo(Dia, false)">
        <h2 class="aj-dia__cabeca"><span class="aj-dia__numero">@Numero</span><span>@RotulosDeAgenda.TituloDoDiaNoDesktop(Dia, EHoje)</span><span class="aj-dia__total">@Total</span></h2>
        <div class="aj-regua">
            <div class="aj-regua__horas" aria-hidden="true">
                @for (var hora = _regua.HoraInicial; hora < _regua.HoraFinal; hora++)
                {
                    <span class="aj-regua__hora">@Hora(hora)</span>
                }
            </div>
            <div class="@ClasseDaGrade">
                @foreach (var item in _regua.Eventos)
                {
                    <CartaoDeEvento @key="item.Evento.Id" Evento="item.Evento" Dia="Dia" Forma="FormaDoCartao.Linha" Href="@Ficha(item.Evento)" ClassesDaRegua="@ClassesDaRegua(item)" />
                }
            </div>
        </div>
    </section>
</div>
<div class="aj-so-tablet">
    <section class="@(EHoje ? "aj-secao-dia aj-secao-dia--hoje" : "aj-secao-dia")">
        <h2 class="aj-secao-dia__cabeca" aria-label="@RotulosDeAgenda.RotuloLongo(Dia, EHoje)"><span class="aj-secao-dia__sigla">@(EHoje ? "Hoje" : RotulosDeAgenda.SiglaDoDia(Dia))</span><span class="aj-secao-dia__numero">@Numero</span></h2>
        <div class="aj-secao-dia__eventos">
            @foreach (var evento in _doDia)
            {
                <CartaoDeEvento @key="evento.Id" Evento="evento" Dia="Dia" Forma="FormaDoCartao.Secao" Href="@Ficha(evento)" />
            }
            @if (_doDia.Count == 0)
            {
                <p class="aj-secao-dia__vazio">Sem eventos</p>
            }
        </div>
    </section>
</div>
<div class="aj-so-mobile">
    <div class="aj-dia-escolhido"><h2>@RotulosDeAgenda.TituloDoDiaNoMobile(Dia, EHoje)</h2><span class="aj-dia-escolhido__total">@Total</span></div>
    @if (_doDia.Count == 0)
    {
        <EstadoDaLista Vazio="true" TextoDeVazio="Nenhum evento neste dia" Icone="agenda" Dica="Toque em + para agendar." />
    }
    else
    {
        foreach (var evento in _doDia)
        {
            <CartaoDeEvento @key="evento.Id" Evento="evento" Dia="Dia" Forma="FormaDoCartao.Folgado" Href="@Ficha(evento)" />
        }
    }
</div>

@code {
    [Parameter, EditorRequired] public PeriodoDaAgenda Periodo { get; set; } = default!;

    /// <summary>Os eventos da consulta da página (a semana inteira); o Dia mostra só os que tocam a data.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<EventoResumo> Eventos { get; set; } = [];

    [Parameter, EditorRequired] public DateOnly Hoje { get; set; }

    private IReadOnlyList<EventoResumo> _doDia = [];
    private ReguaDoDia _regua = default!;

    private DateOnly Dia => Periodo.Data;
    private bool EHoje => Dia == Hoje;

    protected override void OnParametersSet()
    {
        // A ordem é a do início; no empate fica a do servidor (o OrderBy é estável).
        _doDia = Eventos.Where(evento => RotulosDeAgenda.TocaODia(Fuso, evento, Dia)).OrderBy(evento => evento.InicioEm).ToList();
        _regua = ReguaDoDia.Montar(Fuso, Dia, _doDia);
    }

    private string Numero => Dia.Day.ToString("00", CultureInfo.InvariantCulture);
    private string Total => RotulosDeAgenda.TotalDeEventos(_doDia.Count);
    private string ClasseDaGrade => $"aj-regua__grade aj-regua__grade--horas-{_regua.Horas}";
    private string Ficha(EventoResumo evento) => PeriodoDaAgenda.Ficha(evento.Id, Periodo.Consulta);

    private static string Hora(int hora) => hora.ToString("00", CultureInfo.InvariantCulture) + ":00";

    /// <summary>
    /// As classes de posição do cartão na régua: o início e a altura em quartos de hora e, num grupo de eventos
    /// simultâneos, a parte da largura que ele ocupa. Evento sozinho (uma parte) não leva a classe de parte.
    /// </summary>
    public static string ClassesDaRegua(EventoNaRegua item) =>
        item.Partes > 1
            ? $"aj-regua__evento aj-regua__evento--inicio-{item.InicioEmQuartos} aj-regua__evento--quartos-{item.Quartos} aj-regua__evento--parte-{item.Parte}-de-{item.Partes}"
            : $"aj-regua__evento aj-regua__evento--inicio-{item.InicioEmQuartos} aj-regua__evento--quartos-{item.Quartos}";
}
```

Notas para o implementador:
- A coluna das horas tem um rótulo por hora, da `HoraInicial` à `HoraFinal - 1` (o quadro: onze rótulos, 08:00 a 18:00, para a régua das 08:00 às 19:00). As linhas das horas são fundo da `aj-regua__grade` (CSS da Tarefa 1): a grade só tem os cartões, e a classe `aj-regua__grade--horas-{n}` dá a altura e as linhas.
- Os títulos do dia vêm da Tarefa 2: `RotulosDeAgenda.TituloDoDiaNoDesktop(dia, hoje)` (`Quinta-feira · hoje`) e `RotulosDeAgenda.TituloDoDiaNoMobile(dia, hoje)` (`Quinta, 08/10 · hoje`).
- O `section` do desktop leva o `aria-label` do dia sem ", hoje", como o quadro (o título visível já diz "· hoje"); o `h2` do tablet leva o rótulo com ", hoje", como as seções da Semana.
- `ReguaDoDia.Montar` recebe só os eventos que tocam o dia (já ordenados); ela recorta ao dia o que começa antes ou termina depois.
- O `@key` da régua e o das listas estão em pais diferentes.

- [ ] **Step 3: A faixa de dias**

Crie `src/AtividadesJuridicas.Web/Components/Agenda/FaixaDeDias.razor`. Classes da Tarefa 1: `aj-faixa-dias` (`__dia`, `__dia--hoje`, `__sigla`, `__numero`), `aj-pontos-evento`, `aj-ponto-evento` (`--reuniao`, `--audiencia`, `--prazo`, `--outro`). O escolhido é o `aria-pressed="true"` (o CSS o pinta).

```razor
@using System.Globalization
@inject FusoDoEscritorio Fuso

@* A faixa de dias da barra do mobile (visões Dia e Semana): os sete dias da semana da data, cada um um botão com a
   sigla, o número e um ponto por evento do dia (até seis), na cor do tipo. O escolhido é o da data; hoje leva o aro
   verde. Tocar num dia avisa a página, que troca a data no endereço. *@
<div class="aj-faixa-dias">
    @foreach (var dia in Periodo.Semana)
    {
        <button type="button" class="@(dia == Hoje ? "aj-faixa-dias__dia aj-faixa-dias__dia--hoje" : "aj-faixa-dias__dia")" aria-pressed="@(dia == Periodo.Data ? "true" : "false")" aria-label="@RotulosDeAgenda.RotuloCurto(dia, dia == Hoje)" @key="dia" @onclick="() => AoEscolher.InvokeAsync(dia)">
            <span class="aj-faixa-dias__sigla">@RotulosDeAgenda.SiglaDoDia(dia)</span>
            <span class="aj-faixa-dias__numero">@dia.Day.ToString("00", CultureInfo.InvariantCulture)</span>
            <span class="aj-pontos-evento" aria-hidden="true">
                @foreach (var tipo in Pontos(dia))
                {
                    <span class="@($"aj-ponto-evento aj-ponto-evento--{CartaoDeEvento.Sufixo(tipo)}")"></span>
                }
            </span>
        </button>
    }
</div>

@code {
    [Parameter, EditorRequired] public PeriodoDaAgenda Periodo { get; set; } = default!;
    [Parameter, EditorRequired] public IReadOnlyList<EventoResumo> Eventos { get; set; } = [];
    [Parameter, EditorRequired] public DateOnly Hoje { get; set; }

    /// <summary>O dia tocado; a página leva a data ao endereço.</summary>
    [Parameter] public EventCallback<DateOnly> AoEscolher { get; set; }

    // Um ponto por evento que toca o dia, na ordem de início, no máximo seis (o que cabe na coluna de ≈ 49px).
    private IEnumerable<TipoEvento> Pontos(DateOnly dia) =>
        Eventos.Where(evento => RotulosDeAgenda.TocaODia(Fuso, evento, dia))
            .OrderBy(evento => evento.InicioEm)
            .Take(6)
            .Select(evento => evento.Tipo);
}
```

Se o `@($"…")` dentro do atributo `class` do ponto incomodar o compilador Razor, troque-o por um método `ClasseDoPonto(TipoEvento)` que devolva a mesma cadeia.

- [ ] **Step 4: Build e testes**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DiaDaAgendaTests|FullyQualifiedName~FaixaDeDiasTests"`
Expected: 9 passando (5 do Dia, 4 da faixa).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~SemanaDaAgendaTests|FullyQualifiedName~CartaoDeEventoTests"`
Expected: todos passando (o cartão não mudou).

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~WebSoPeloDespachante"`
Expected: todos passando.

- [ ] **Step 5: Commit**

Pela ferramenta PowerShell, confira o branch (`git branch --show-current` deve dizer `worktree-feat+fase-9f-agenda`). Grave a mensagem num arquivo UTF-8 sem BOM e faça o commit com `git commit -F`:

```
git add src/AtividadesJuridicas.Web/Components/Agenda tests/AtividadesJuridicas.IntegrationTests
git commit -F <arquivo da mensagem>
```

Mensagem:

```
feat(web): Dia da agenda com a régua por quartos de hora, a seção do tablet e a lista e a faixa de dias do mobile

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

---

### Task 8: Mês — grade do desktop e do tablet, grade compacta e lista do mobile

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Agenda/MesDaAgenda.razor`, `GradeCompactaDoMes.razor`
- Test: `tests/AtividadesJuridicas.IntegrationTests/MesDaAgendaTests.cs`

**Interfaces:**
- Consumes: `PeriodoDaAgenda`, `RotulosDeAgenda` (Tarefa 2, inclusive `TituloDoDiaNoMobile`), `CartaoDeEvento` (Tarefa 6), `EstadoDaLista.Icone`/`Dica` (Tarefa 3), `QuadroDaAgenda` (testes, Tarefa 6).
- Produces:
  - `MesDaAgenda`: `PeriodoDaAgenda Periodo`, `IReadOnlyList<EventoResumo> Eventos`, `DateOnly Hoje`, `EventCallback<DateOnly> AoAbrirDia` (o `+N`). Emite `aj-desktop-tablet` (`aj-mes`) e `aj-so-mobile` (`aj-dia-escolhido` da `data` com os cartões `Folgado` ou o vazio).
  - `GradeCompactaDoMes`: `PeriodoDaAgenda Periodo`, `IReadOnlyList<EventoResumo> Eventos`, `DateOnly Hoje`, `EventCallback<DateOnly> AoEscolher`. Emite a `aj-faixa-dias aj-faixa-dias--mes` da barra do mobile.

**Regras:**
- Grade de `PeriodoDaAgenda.SemanasDoMes`; dia fora do mês com `aj-mes__celula--fora`, sem eventos; até três eventos (`Mes`) e o `+{n}`; evento de vários dias em cada dia, com a hora só no primeiro.

**Testes obrigatórios:** outubro de 2026 do quadro (cinco semanas, as células de fora, hoje, os itens); um mês de seis semanas e fevereiro de 2027 com quatro; um dia com cinco eventos (três e `+2`, o `aria-label`, o `AoAbrirDia`); o evento de vários dias (Review Focus 2); a grade compacta (pontos, fora do mês, escolhido).

- [ ] **Step 1: Testes (devem falhar)**

Antes de escrever, releia na cópia local dos quadros o Mês de `AgendaA.dc.html` (a caixa com a linha dos dias da semana `aria-hidden`, a área que rola, a grade de 35 células: as de fora em bloco e sem eventos, a de hoje com o número no círculo, os itens com a hora e o título) e a faixa de dias de `AgendaMobile.dc.html` (a grade compacta do mês é desenho por analogia com ela), e as seções 1.1, 2 (item 9), 7.1 ("Mês no tablet" e "Mês no mobile"), 7.2 ("Mês de seis semanas", "+N", "Evento de mais de um dia"), 8.8 e 8.9 de `quadros-agenda.md`.

Os testes são de componente (bUnit, base `TelaInterativa`), com os eventos do quadro do auxiliar `QuadroDaAgenda` (`tests/AtividadesJuridicas.IntegrationTests/QuadroDaAgenda.cs`, da Tarefa 6): `QuadroDaAgenda.Hoje` (quinta-feira, 08/10/2026), os oito eventos da semana em `QuadroDaAgenda.Semana` e cada um por nome (`PautaDaSemana`, `AudienciaDeInstrucao`, `Contestacao`, `Alinhamento`, `AudienciaDeConciliacao`, `ProtocoloDeDocumentos`, `RevisaoDeMinutas`, `RecursoOrdinario`), e `QuadroDaAgenda.Evento(DateTime inicioLocal, int minutos, TipoEvento tipo, string titulo, string? local = null, string? cliente = null, params string[] participantes)` para os eventos extras (início no horário do escritório).

As grades: outubro de 2026 começa numa quinta e termina num sábado — de segunda, 28/09, a domingo, 01/11 (cinco semanas, 35 células; o dia `d` de outubro é a célula `d + 2`). Agosto de 2026 começa num sábado e termina numa segunda — de 27/07 a 06/09 (seis semanas, 42 células, 11 de fora). Fevereiro de 2027 começa numa segunda e tem 28 dias — de 01/02 a 28/02 (quatro semanas, nenhuma célula de fora).

Crie `tests/AtividadesJuridicas.IntegrationTests/MesDaAgendaTests.cs`, com duas classes (o Mês e a grade compacta):

```csharp
using AngleSharp.Dom;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Web.Components.Agenda;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O Mês da agenda com os eventos do quadro: a grade de semanas inteiras do desktop e do tablet (4, 5 ou 6 linhas,
/// dias de fora apagados e sem eventos, até três eventos por dia e o "+N", evento de vários dias em cada dia) e a
/// lista do dia da data no mobile.
/// </summary>
public class MesDaAgendaTests : TelaInterativa
{
    private DateOnly? _aberto;

    private IRenderedComponent<MesDaAgenda> Mes(DateOnly data, IReadOnlyList<EventoResumo> eventos) =>
        Desenhar<MesDaAgenda>(p => p
            .Add(c => c.Periodo, new PeriodoDaAgenda(VisaoDaAgenda.Mes, data, true))
            .Add(c => c.Eventos, eventos)
            .Add(c => c.Hoje, QuadroDaAgenda.Hoje)
            .Add(c => c.AoAbrirDia, (DateOnly dia) => _aberto = dia));

    private static IReadOnlyList<IElement> Celulas(IRenderedComponent<MesDaAgenda> mes) =>
        mes.FindAll("div.aj-desktop-tablet > div.aj-mes > div.aj-mes__rolagem > div.aj-mes__grade > div");

    // A grade de outubro de 2026 começa na segunda-feira, 28/09: o dia d de outubro é a célula d + 2.
    private static IElement DeOutubro(IRenderedComponent<MesDaAgenda> mes, int dia) => Celulas(mes)[dia + 2];

    private static string Numero(IElement celula) => celula.QuerySelector("span.aj-mes__numero")!.TextContent;

    private static string[] Itens(IElement celula) =>
        celula.QuerySelectorAll("a.aj-evento .aj-evento__titulo").Select(t => t.TextContent).ToArray();

    private static IElement Item(IElement celula, string titulo) =>
        celula.QuerySelectorAll("a.aj-evento").Single(a => a.QuerySelector(".aj-evento__titulo")!.TextContent == titulo);

    private static string[] Linhas(IElement elemento) =>
        elemento.Children.Select(linha => $"{linha.ClassName}: {linha.TextContent}").ToArray();

    [Fact]
    public void Outubro_do_quadro_tem_cinco_semanas_os_dias_de_fora_apagados_e_sem_eventos_hoje_marcado_e_os_itens()
    {
        // Um evento em 30/09 (dia de fora na grade de outubro) não aparece.
        var setembro = QuadroDaAgenda.Evento(new DateTime(2026, 9, 30, 10, 0, 0), 60, TipoEvento.Reuniao, "Fechamento de setembro");

        var mes = Mes(QuadroDaAgenda.Hoje, [setembro, .. QuadroDaAgenda.Semana]);

        var cabeca = mes.Find("div.aj-desktop-tablet > div.aj-mes > div.aj-mes__cabeca");
        cabeca.GetAttribute("aria-hidden").Should().Be("true");
        cabeca.Children.Select(s => s.TextContent).Should().Equal("Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom");

        var celulas = Celulas(mes);
        celulas.Should().HaveCount(35);
        Numero(celulas[0]).Should().Be("28");
        Numero(celulas[34]).Should().Be("01");
        var fora = celulas.Where(c => c.ClassList.Contains("aj-mes__celula--fora")).ToList();
        fora.Select(Numero).Should().Equal("28", "29", "30", "01");
        fora.Should().OnlyContain(c => c.ClassName == "aj-mes__celula aj-mes__celula--fora" && c.QuerySelectorAll("a").Length == 0);
        celulas.Count(c => c.ClassName == "aj-mes__celula").Should().Be(31);

        // Hoje: o número no círculo e "hoje" para o leitor de tela; só no dia 08.
        var hoje = DeOutubro(mes, 8);
        hoje.QuerySelector("span.aj-mes__numero")!.ClassName.Should().Be("aj-mes__numero aj-mes__numero--hoje");
        hoje.Children.Where(c => c.ClassName == "aj-so-leitor").Select(c => c.TextContent).Should().Equal("hoje");
        mes.FindAll("span.aj-mes__numero--hoje").Should().ContainSingle();
        DeOutubro(mes, 7).QuerySelector("span.aj-mes__numero")!.ClassName.Should().Be("aj-mes__numero");

        var cinco = DeOutubro(mes, 5).QuerySelectorAll("a.aj-evento");
        cinco.Select(a => a.ClassName).Should().Equal(
            "aj-evento aj-evento--mes aj-evento--reuniao", "aj-evento aj-evento--mes aj-evento--audiencia");
        Linhas(cinco[0]).Should().Equal("aj-so-leitor: Reunião, ", "aj-evento__horario: 09:00", "aj-evento__titulo: Pauta da semana");
        Linhas(cinco[1]).Should().Equal("aj-so-leitor: Audiência, ", "aj-evento__horario: 14:00", "aj-evento__titulo: Audiência de instrução");
        cinco[0].GetAttribute("href").Should().Be($"/agenda/{QuadroDaAgenda.PautaDaSemana.Id}?visao=mes&data=2026-10-08");
        Itens(DeOutubro(mes, 6)).Should().Equal("Contestação — cobrança de ISS");
        Itens(DeOutubro(mes, 7)).Should().Equal("Alinhamento com o cliente", "Audiência de conciliação");
        Itens(DeOutubro(mes, 8)).Should().Equal("Protocolo de documentos", "Revisão de minutas");
        Itens(DeOutubro(mes, 9)).Should().Equal("Recurso ordinário");
        Itens(DeOutubro(mes, 10)).Should().BeEmpty();
        Numero(DeOutubro(mes, 10)).Should().Be("10");
        mes.FindAll("button.aj-mes__mais").Should().BeEmpty();
        mes.FindAll(".aj-evento__titulo").Select(t => t.TextContent).Should()
            .Contain("Pauta da semana").And.NotContain("Fechamento de setembro");
    }

    [Fact]
    public void Agosto_de_2026_tem_seis_semanas_e_fevereiro_de_2027_tem_quatro()
    {
        var agosto = Celulas(Mes(new DateOnly(2026, 8, 20), []));
        agosto.Should().HaveCount(42);
        Numero(agosto[0]).Should().Be("27");
        Numero(agosto[41]).Should().Be("06");
        agosto.Count(c => c.ClassList.Contains("aj-mes__celula--fora")).Should().Be(11);
        agosto.Count(c => c.ClassName == "aj-mes__celula").Should().Be(31);
        agosto.Should().OnlyContain(c => c.QuerySelector("span.aj-mes__numero--hoje") == null);

        var fevereiro = Celulas(Mes(new DateOnly(2027, 2, 10), []));
        fevereiro.Should().HaveCount(28);
        Numero(fevereiro[0]).Should().Be("01");
        Numero(fevereiro[27]).Should().Be("28");
        fevereiro.Should().OnlyContain(c => c.ClassName == "aj-mes__celula");
    }

    [Fact]
    public void Dia_com_mais_de_tres_eventos_mostra_os_tres_primeiros_e_o_mais_que_abre_o_dia()
    {
        var sete = Enumerable.Range(0, 3).Select(i =>
            QuadroDaAgenda.Evento(new DateTime(2026, 10, 7, 8 + i, 0, 0), 30, TipoEvento.Audiencia, $"Audiência {i + 1}"));
        var oito = Enumerable.Range(0, 5).Select(i =>
            QuadroDaAgenda.Evento(new DateTime(2026, 10, 8, 8 + i, 0, 0), 30, TipoEvento.Reuniao, $"Reunião {i + 1}"));
        var nove = Enumerable.Range(0, 4).Select(i =>
            QuadroDaAgenda.Evento(new DateTime(2026, 10, 9, 8 + i, 0, 0), 30, TipoEvento.Prazo, $"Prazo {i + 1}"));

        var mes = Mes(QuadroDaAgenda.Hoje, [.. sete, .. oito, .. nove]);

        // Exatamente três: todos, sem o "+".
        Itens(DeOutubro(mes, 7)).Should().Equal("Audiência 1", "Audiência 2", "Audiência 3");
        DeOutubro(mes, 7).QuerySelector("button.aj-mes__mais").Should().BeNull();

        Itens(DeOutubro(mes, 8)).Should().Equal("Reunião 1", "Reunião 2", "Reunião 3");
        var mais = DeOutubro(mes, 8).QuerySelector("button.aj-mes__mais")!;
        mais.GetAttribute("type").Should().Be("button");
        mais.TextContent.Should().Be("+2");
        mais.GetAttribute("aria-label").Should().Be("Mais 2 eventos em 08/10");

        Itens(DeOutubro(mes, 9)).Should().Equal("Prazo 1", "Prazo 2", "Prazo 3");
        var maisUm = DeOutubro(mes, 9).QuerySelector("button.aj-mes__mais")!;
        maisUm.TextContent.Should().Be("+1");
        maisUm.GetAttribute("aria-label").Should().Be("Mais 1 evento em 09/10");

        mais.Click();

        _aberto.Should().Be(new DateOnly(2026, 10, 8));
    }

    [Fact]
    public void Evento_de_varios_dias_aparece_em_cada_dia_com_a_hora_so_no_primeiro()
    {
        // 06/10 às 20:00 até 08/10 às 10:00.
        var plantao = QuadroDaAgenda.Evento(new DateTime(2026, 10, 6, 20, 0, 0), 38 * 60, TipoEvento.Prazo, "Plantão de protocolo");

        var mes = Mes(QuadroDaAgenda.Hoje, [.. QuadroDaAgenda.Semana, plantao]);

        Itens(DeOutubro(mes, 6)).Should().Equal("Contestação — cobrança de ISS", "Plantão de protocolo");
        Linhas(Item(DeOutubro(mes, 6), "Plantão de protocolo")).Should().Equal(
            "aj-so-leitor: Prazo, ", "aj-evento__horario: 20:00", "aj-evento__titulo: Plantão de protocolo");
        Itens(DeOutubro(mes, 7)).Should().Equal("Plantão de protocolo", "Alinhamento com o cliente", "Audiência de conciliação");
        Linhas(Item(DeOutubro(mes, 7), "Plantão de protocolo")).Should().Equal(
            "aj-so-leitor: Prazo, ", "aj-evento__titulo: Plantão de protocolo");
        Itens(DeOutubro(mes, 8)).Should().Equal("Plantão de protocolo", "Protocolo de documentos", "Revisão de minutas");
        Linhas(Item(DeOutubro(mes, 8), "Plantão de protocolo")).Should().Equal(
            "aj-so-leitor: Prazo, ", "aj-evento__titulo: Plantão de protocolo");
        Itens(DeOutubro(mes, 9)).Should().Equal("Recurso ordinário");
        mes.FindAll("button.aj-mes__mais").Should().BeEmpty();
    }

    [Fact]
    public void No_mobile_a_lista_e_a_do_dia_da_data_ou_o_vazio_com_dica()
    {
        var mobile = Mes(QuadroDaAgenda.Hoje, QuadroDaAgenda.Semana).Find("div.aj-so-mobile");

        mobile.QuerySelector("div.aj-dia-escolhido > h2")!.TextContent.Should().Be("Quinta, 08/10 · hoje");
        mobile.QuerySelector("div.aj-dia-escolhido > span.aj-dia-escolhido__total")!.TextContent.Should().Be("2 eventos");
        mobile.QuerySelectorAll("a.aj-evento").Select(a => a.ClassName).Should().Equal(
            "aj-evento aj-evento--folgado aj-evento--outro", "aj-evento aj-evento--folgado aj-evento--reuniao");
        mobile.QuerySelectorAll("a.aj-evento .aj-evento__titulo").Select(t => t.TextContent).Should().Equal(
            "Protocolo de documentos", "Revisão de minutas");
        mobile.QuerySelector(".aj-estado-lista").Should().BeNull();

        var sabado = Mes(new DateOnly(2026, 10, 10), QuadroDaAgenda.Semana).Find("div.aj-so-mobile");

        sabado.QuerySelector("div.aj-dia-escolhido > h2")!.TextContent.Should().Be("Sábado, 10/10");
        sabado.QuerySelector("span.aj-dia-escolhido__total")!.TextContent.Should().Be("0 eventos");
        var vazio = sabado.QuerySelector("div.aj-estado-lista")!;
        vazio.QuerySelector("svg.aj-estado-lista__icone").Should().NotBeNull();
        vazio.QuerySelector("strong")!.TextContent.Should().Be("Nenhum evento neste dia");
        vazio.QuerySelector("span.aj-estado-lista__dica")!.TextContent.Should().Be("Toque em + para agendar.");
        sabado.QuerySelectorAll("a.aj-evento").Should().BeEmpty();
    }
}

/// <summary>
/// A grade compacta do mês na barra do mobile: a linha das siglas, um botão por dia das semanas do mês (sem sigla),
/// os dias de fora apagados e sem pontos, o escolhido, o hoje e o toque que avisa a página.
/// </summary>
public class GradeCompactaDoMesTests : TelaInterativa
{
    private DateOnly? _escolhido;

    private IRenderedComponent<GradeCompactaDoMes> Grade(DateOnly data, IReadOnlyList<EventoResumo> eventos) =>
        Desenhar<GradeCompactaDoMes>(p => p
            .Add(c => c.Periodo, new PeriodoDaAgenda(VisaoDaAgenda.Mes, data, true))
            .Add(c => c.Eventos, eventos)
            .Add(c => c.Hoje, QuadroDaAgenda.Hoje)
            .Add(c => c.AoEscolher, (DateOnly dia) => _escolhido = dia));

    // A grade de outubro de 2026 começa na segunda-feira, 28/09: o dia d de outubro é o botão d + 2.
    private static IReadOnlyList<IElement> Dias(IRenderedComponent<GradeCompactaDoMes> grade) =>
        grade.FindAll("div.aj-faixa-dias.aj-faixa-dias--mes > button");

    private static string[] Pontos(IElement dia) =>
        dia.QuerySelectorAll("span.aj-pontos-evento > span").Select(p => p.ClassName!).ToArray();

    [Fact]
    public void Grade_de_outubro_tem_as_siglas_os_dias_de_fora_apagados_o_escolhido_e_o_hoje()
    {
        var grade = Grade(new DateOnly(2026, 10, 7), QuadroDaAgenda.Semana);

        grade.Find("div.aj-faixa-dias").ClassName.Should().Be("aj-faixa-dias aj-faixa-dias--mes");
        var cabeca = grade.Find("div.aj-faixa-dias--mes > div.aj-faixa-dias__cabeca");
        cabeca.GetAttribute("aria-hidden").Should().Be("true");
        cabeca.Children.Select(s => s.TextContent).Should().Equal("Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom");

        var dias = Dias(grade);
        dias.Should().HaveCount(35);
        dias.Should().OnlyContain(d => d.GetAttribute("type") == "button" && d.QuerySelector("span.aj-faixa-dias__sigla") == null);
        dias.Select(d => d.QuerySelector("span.aj-faixa-dias__numero")!.TextContent).Should().StartWith("28").And.EndWith("01");
        dias[0].GetAttribute("aria-label").Should().Be("Segunda, 28/09");
        dias[10].GetAttribute("aria-label").Should().Be("Quinta, 08/10, hoje");
        dias.Where(d => d.ClassList.Contains("aj-faixa-dias__dia--fora"))
            .Select(d => d.QuerySelector("span.aj-faixa-dias__numero")!.TextContent).Should().Equal("28", "29", "30", "01");
        dias[0].ClassName.Should().Be("aj-faixa-dias__dia aj-faixa-dias__dia--fora");
        dias[9].ClassName.Should().Be("aj-faixa-dias__dia");
        dias[10].ClassName.Should().Be("aj-faixa-dias__dia aj-faixa-dias__dia--hoje");
        dias.Select(d => d.GetAttribute("aria-pressed")).Count(p => p == "true").Should().Be(1);
        dias[9].GetAttribute("aria-pressed").Should().Be("true");
        dias[10].GetAttribute("aria-pressed").Should().Be("false");
    }

    [Fact]
    public void Um_ponto_por_evento_do_dia_e_nenhum_no_dia_de_fora()
    {
        var setembro = QuadroDaAgenda.Evento(new DateTime(2026, 9, 30, 10, 0, 0), 60, TipoEvento.Reuniao, "Fechamento de setembro");

        var dias = Dias(Grade(QuadroDaAgenda.Hoje, [setembro, .. QuadroDaAgenda.Semana]));

        Pontos(dias[7]).Should().Equal("aj-ponto-evento aj-ponto-evento--reuniao", "aj-ponto-evento aj-ponto-evento--audiencia");
        Pontos(dias[10]).Should().Equal("aj-ponto-evento aj-ponto-evento--outro", "aj-ponto-evento aj-ponto-evento--reuniao");
        Pontos(dias[12]).Should().BeEmpty();
        // 30/09 tem evento, mas é de fora: sem pontos, com a fileira (a mesma altura dos outros dias).
        Pontos(dias[2]).Should().BeEmpty();
        dias[2].QuerySelector("span.aj-pontos-evento")!.GetAttribute("aria-hidden").Should().Be("true");
    }

    [Fact]
    public void Tocar_num_dia_avisa_a_pagina_com_a_data_inclusive_num_dia_de_fora()
    {
        var grade = Grade(QuadroDaAgenda.Hoje, QuadroDaAgenda.Semana);

        Dias(grade)[11].Click();
        _escolhido.Should().Be(new DateOnly(2026, 10, 9));

        Dias(grade)[34].Click();
        _escolhido.Should().Be(new DateOnly(2026, 11, 1));
    }
}
```

Run: `dotnet build -m:1`
Expected: não compila — `MesDaAgenda` e `GradeCompactaDoMes` não existem (CS0246). É o RED.

- [ ] **Step 2: O Mês**

Crie `src/AtividadesJuridicas.Web/Components/Agenda/MesDaAgenda.razor`. Classes da Tarefa 1: `aj-mes` (`__cabeca`, `__rolagem`, `__grade`, `__celula`, `__celula--fora`, `__numero`, `__numero--hoje`, `__mais`), `aj-dia-escolhido` (`__total`). O item é o `CartaoDeEvento` da Tarefa 6 na forma `Mes` (o tipo só para leitor, a hora de início só no dia em que o evento começa, o título); os cartões do mobile, na forma `Folgado`. O título do dia no mobile é o `RotulosDeAgenda.TituloDoDiaNoMobile(DateOnly dia, bool hoje)` da Tarefa 2 (`Quinta, 08/10 · hoje`). O vazio do mobile é o `EstadoDaLista` com `Icone` e `Dica` (Tarefa 3).

```razor
@using System.Globalization
@inject FusoDoEscritorio Fuso

@* O Mês da agenda. Desktop e tablet: a linha dos dias da semana e a grade das semanas inteiras (de segunda a
   domingo) que cobrem o mês da data, em 4, 5 ou 6 linhas; dia de outro mês apagado e sem eventos; hoje com o número
   no círculo; até três eventos por dia, na ordem de início, e o "+N" com os outros, que leva ao Dia daquela data;
   evento de vários dias em cada dia que toca, com a hora só no dia em que começa. Mobile: o título do dia da data e
   a lista dos eventos dele, ou o vazio (a grade compacta fica na barra superior). *@
<div class="aj-desktop-tablet">
    <div class="aj-mes">
        <div class="aj-mes__cabeca" aria-hidden="true"><span>Seg</span><span>Ter</span><span>Qua</span><span>Qui</span><span>Sex</span><span>Sáb</span><span>Dom</span></div>
        <div class="aj-mes__rolagem">
            <div class="aj-mes__grade">
                @foreach (var dia in Periodo.SemanasDoMes)
                {
                    if (Fora(dia))
                    {
                        <div class="aj-mes__celula aj-mes__celula--fora" @key='("fora", dia)'><span class="aj-mes__numero">@Numero(dia)</span></div>
                    }
                    else
                    {
                        var doDia = _porDia[dia];
                        <div class="aj-mes__celula" @key='("dia", dia)'>
                            @if (dia == Hoje)
                            {
                                <span class="aj-mes__numero aj-mes__numero--hoje">@Numero(dia)</span>
                                <span class="aj-so-leitor">hoje</span>
                            }
                            else
                            {
                                <span class="aj-mes__numero">@Numero(dia)</span>
                            }
                            @foreach (var evento in doDia.Take(MaximoPorDia))
                            {
                                <CartaoDeEvento @key="evento.Id" Evento="evento" Dia="dia" Forma="FormaDoCartao.Mes" Href="@Ficha(evento)" />
                            }
                            @if (doDia.Count > MaximoPorDia)
                            {
                                <button type="button" class="aj-mes__mais" aria-label="@RotuloDoMais(dia, doDia.Count - MaximoPorDia)" @onclick="() => AoAbrirDia.InvokeAsync(dia)">@($"+{doDia.Count - MaximoPorDia}")</button>
                            }
                        </div>
                    }
                }
            </div>
        </div>
    </div>
</div>
<div class="aj-so-mobile">
    <div class="aj-dia-escolhido"><h2>@RotulosDeAgenda.TituloDoDiaNoMobile(Periodo.Data, Periodo.Data == Hoje)</h2><span class="aj-dia-escolhido__total">@RotulosDeAgenda.TotalDeEventos(DoDiaEscolhido.Count)</span></div>
    @if (DoDiaEscolhido.Count == 0)
    {
        <EstadoDaLista Vazio="true" TextoDeVazio="Nenhum evento neste dia" Icone="agenda" Dica="Toque em + para agendar." />
    }
    else
    {
        foreach (var evento in DoDiaEscolhido)
        {
            <CartaoDeEvento @key="evento.Id" Evento="evento" Dia="Periodo.Data" Forma="FormaDoCartao.Folgado" Href="@Ficha(evento)" />
        }
    }
</div>

@code {
    private const int MaximoPorDia = 3;

    [Parameter, EditorRequired] public PeriodoDaAgenda Periodo { get; set; } = default!;

    /// <summary>Os eventos da consulta da página (as semanas inteiras do mês).</summary>
    [Parameter, EditorRequired] public IReadOnlyList<EventoResumo> Eventos { get; set; } = [];

    [Parameter, EditorRequired] public DateOnly Hoje { get; set; }

    /// <summary>O "+N" de um dia: a página leva ao Dia daquela data.</summary>
    [Parameter] public EventCallback<DateOnly> AoAbrirDia { get; set; }

    private Dictionary<DateOnly, IReadOnlyList<EventoResumo>> _porDia = new();

    protected override void OnParametersSet() =>
        // Cada evento em todo dia que toca, na ordem de início (no empate, a do servidor: o OrderBy é estável).
        _porDia = Periodo.SemanasDoMes.ToDictionary(
            dia => dia,
            dia => (IReadOnlyList<EventoResumo>)Eventos
                .Where(evento => RotulosDeAgenda.TocaODia(Fuso, evento, dia))
                .OrderBy(evento => evento.InicioEm)
                .ToList());

    private IReadOnlyList<EventoResumo> DoDiaEscolhido => _porDia.GetValueOrDefault(Periodo.Data) ?? [];

    // Dia de outro mês na grade: apagado, sem eventos e sem a marca de hoje (o quadro).
    private bool Fora(DateOnly dia) => dia.Year != Periodo.Data.Year || dia.Month != Periodo.Data.Month;

    private string Ficha(EventoResumo evento) => PeriodoDaAgenda.Ficha(evento.Id, Periodo.Consulta);

    private static string Numero(DateOnly dia) => dia.Day.ToString("00", CultureInfo.InvariantCulture);

    private static string RotuloDoMais(DateOnly dia, int outros)
    {
        var data = dia.ToString("dd'/'MM", CultureInfo.InvariantCulture);
        return outros == 1 ? $"Mais 1 evento em {data}" : $"Mais {outros} eventos em {data}";
    }
}
```

Notas para o implementador:
- As duas formas de célula estão no mesmo pai com chaves compostas (`("fora", dia)` e `("dia", dia)`): ao trocar de mês, uma data que era de fora e passa a ser do mês não reaproveita o elemento do outro tipo (lição da 9D).
- O `span.aj-so-leitor` "hoje" fica logo depois do número; ele sai do fluxo (posição absoluta da classe) e não mexe no círculo.
- No tablet a grade não tem largura mínima (decisão do plano): é CSS da Tarefa 1; a marcação é a mesma nas duas larguras.

- [ ] **Step 3: A grade compacta do mês**

Crie `src/AtividadesJuridicas.Web/Components/Agenda/GradeCompactaDoMes.razor`. Classes da Tarefa 1: `aj-faixa-dias`, `aj-faixa-dias--mes`, `aj-faixa-dias__cabeca`, `aj-faixa-dias__dia` (`--hoje`, `--fora`), `aj-faixa-dias__numero`, `aj-pontos-evento`, `aj-ponto-evento--{tipo}`. Os sufixos do tipo vêm de `CartaoDeEvento.Sufixo(TipoEvento)` (Tarefa 6).

```razor
@using System.Globalization
@inject FusoDoEscritorio Fuso

@* A grade compacta do mês, na barra do mobile (visão Mês): a linha das siglas e um botão por dia das semanas inteiras
   do mês, com o número e um ponto por evento (até seis), sem sigla em cada dia. Dia de outro mês apagado e sem
   pontos; o escolhido é o da data; hoje com o aro verde. Tocar num dia avisa a página, que troca a data e fica no
   Mês (a lista embaixo passa a ser a desse dia). *@
<div class="aj-faixa-dias aj-faixa-dias--mes">
    <div class="aj-faixa-dias__cabeca" aria-hidden="true"><span>Seg</span><span>Ter</span><span>Qua</span><span>Qui</span><span>Sex</span><span>Sáb</span><span>Dom</span></div>
    @foreach (var dia in Periodo.SemanasDoMes)
    {
        <button type="button" class="@ClasseDoDia(dia)" aria-pressed="@(dia == Periodo.Data ? "true" : "false")" aria-label="@RotulosDeAgenda.RotuloCurto(dia, EHoje(dia))" @key="dia" @onclick="() => AoEscolher.InvokeAsync(dia)">
            <span class="aj-faixa-dias__numero">@dia.Day.ToString("00", CultureInfo.InvariantCulture)</span>
            <span class="aj-pontos-evento" aria-hidden="true">
                @foreach (var tipo in Pontos(dia))
                {
                    <span class="@ClasseDoPonto(tipo)"></span>
                }
            </span>
        </button>
    }
</div>

@code {
    [Parameter, EditorRequired] public PeriodoDaAgenda Periodo { get; set; } = default!;
    [Parameter, EditorRequired] public IReadOnlyList<EventoResumo> Eventos { get; set; } = [];
    [Parameter, EditorRequired] public DateOnly Hoje { get; set; }

    /// <summary>O dia tocado; a página leva a data ao endereço e fica no Mês.</summary>
    [Parameter] public EventCallback<DateOnly> AoEscolher { get; set; }

    private bool Fora(DateOnly dia) => dia.Year != Periodo.Data.Year || dia.Month != Periodo.Data.Month;

    // Como na grade do desktop, o dia de fora não leva a marca de hoje.
    private bool EHoje(DateOnly dia) => dia == Hoje && !Fora(dia);

    private string ClasseDoDia(DateOnly dia) =>
        Fora(dia) ? "aj-faixa-dias__dia aj-faixa-dias__dia--fora"
        : EHoje(dia) ? "aj-faixa-dias__dia aj-faixa-dias__dia--hoje"
        : "aj-faixa-dias__dia";

    // Um ponto por evento que toca o dia, na ordem de início, no máximo seis; dia de fora sem pontos.
    private IEnumerable<TipoEvento> Pontos(DateOnly dia) =>
        Fora(dia)
            ? Enumerable.Empty<TipoEvento>()
            : Eventos.Where(evento => RotulosDeAgenda.TocaODia(Fuso, evento, dia))
                .OrderBy(evento => evento.InicioEm)
                .Take(6)
                .Select(evento => evento.Tipo);

    private static string ClasseDoPonto(TipoEvento tipo) => $"aj-ponto-evento aj-ponto-evento--{CartaoDeEvento.Sufixo(tipo)}";
}
```

O dia de fora continua botão (tocá-lo leva a data ao outro mês). A fileira de pontos existe em todo dia, mesmo vazia, para os dias terem a mesma altura (o quadro faz isso na faixa).

- [ ] **Step 4: Build e testes**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~MesDaAgendaTests|FullyQualifiedName~GradeCompactaDoMesTests"`
Expected: 8 passando (5 do Mês, 3 da grade compacta).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~SemanaDaAgendaTests|FullyQualifiedName~CartaoDeEventoTests|FullyQualifiedName~DiaDaAgendaTests|FullyQualifiedName~FaixaDeDiasTests"`
Expected: todos passando (nada das Tarefas 6 e 7 mudou).

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~WebSoPeloDespachante"`
Expected: todos passando.

- [ ] **Step 5: Commit**

Pela ferramenta PowerShell, confira o branch (`git branch --show-current` deve dizer `worktree-feat+fase-9f-agenda`). Grave a mensagem num arquivo UTF-8 sem BOM e faça o commit com `git commit -F`:

```
git add src/AtividadesJuridicas.Web/Components/Agenda tests/AtividadesJuridicas.IntegrationTests
git commit -F <arquivo da mensagem>
```

Mensagem:

```
feat(web): Mês da agenda com a grade de semanas inteiras, o +N e a grade compacta do mobile

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

---

### Task 9: A página da agenda

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Agenda.razor` (reescrita)
- Test: `tests/AtividadesJuridicas.IntegrationTests/AgendaWebTests.cs` (classe `AgendaWebTests`: asserções reescritas), `AgendaInterativaTests.cs` (novo), `LayoutWebTests.cs` (os dois testes de "tela ainda não migrada", que usam `/agenda`: reescritos aqui para o commit ficar verde)

**Interfaces:**
- Consumes: tudo das Tarefas 2, 3, 6, 7 e 8; `CabecalhoDePagina` (`Titulo="Agenda"`, `Subtitulo`, `SubtituloMobile=""`, `Acao="Novo evento"`, `AcaoHref`, `LinhaDoMobile`, `AcaoDoMobile`). A barra de período e filtros do desktop e do tablet fica abaixo do cabeçalho, fora dele.
- Produces: a agenda em `/agenda` com `?visao`/`?data`; `Novo evento` leva a `/agenda/novo{consulta}`.

**Regras:**
- Lê `visao` e `data` por `[SupplyParameterFromQuery]`; `PeriodoDaAgenda.Ler` em `OnParametersSetAsync`; consulta quando o intervalo da consulta, o tipo ou o participante mudam (nunca duas vezes para o mesmo intervalo e filtro); descarta resposta velha (a `_geracao` de hoje).
- Corpo: sem `visao`, `<div class="aj-desktop-tablet"><SemanaDaAgenda/></div>` e `<div class="aj-so-mobile"><DiaDaAgenda/></div>`; com `visao`, o componente da visão sem invólucro extra. Barra do mobile (`LinhaDoMobile`): o segmentado claro, a linha do mês com as setas e a `FaixaDeDias` (Dia e Semana) ou a `GradeCompactaDoMes` (Mês).
- O recado (`Recados.Retirar()` na primeira carga) aparece no topo pelo `AvisoDeEventoSalvo` (`Evento excluído.` é um aviso de sucesso simples).
- Estados: carregando, erro com `Tentar de novo` (`Não foi possível consultar a agenda.` ou a mensagem do servidor), acesso encerrado; a falha não mostra semana vazia.
- Painel de filtros do tablet e do mobile como o da Auditoria (foco no primeiro campo ao abrir; `Filtrar` fecha e consulta; `Limpar` volta a `Todos`).
- `LayoutWebTests`: `Tela_ainda_nao_migrada_fica_com_a_barra_de_logo_e_sem_involucro` passa a usar `/rota-que-nao-existe` (a página não encontrada não declara barra) e muda de nome para o que prova; `Tela_ainda_nao_migrada_nao_abre_o_conteudo_com_bloco_do_design_system` vira um theory sobre as rotas da equipe que prova que toda tela abre o conteúdo com um bloco do design system (o que a regra interina, que a Tarefa 10 apaga, deixaria de tratar).

**Testes obrigatórios:** reescrever os seis testes de `AgendaWebTests` mantendo o que provam (Diretoria vê os eventos da semana; Produção só os seus, com uma positiva no mesmo bloco; menu; anônimo; registro de consultas; filtro de participante com o inativo e só na Diretoria); sem endereço e com endereço estranho (Review Focus 1: o padrão, uma consulta só); `?visao=mes&data=…` consultando as semanas do mês; as setas e `Hoje` navegando para os endereços certos em cada largura; trocar o período não repete a consulta do mesmo intervalo; o filtro mantido ao trocar de período; o recado de evento salvo com sobreposição nas três redações e o de excluído; a falha com `Tentar de novo`.

- [ ] **Step 1: Testes de página por HTTP (devem falhar)**

Antes de escrever, releia os quadros `AgendaA.dc.html`, `AgendaMenuEscondido.dc.html`, `AgendaTablet.dc.html` e `AgendaMobile.dc.html` e as seções 2 a 8 de `quadros-agenda.md`; e o `Pages/Agenda.razor` de hoje com a seção 1.1 de `inventario-codigo.md`.

Reescreva a classe `AgendaWebTests` de `tests/AtividadesJuridicas.IntegrationTests/AgendaWebTests.cs`. A classe `AgendaPorDiaTests`, no fim do mesmo arquivo, não muda (nem um caractere). O arquivo tem fim de linha CRLF: mantenha.

Como cada asserção antiga é reescrita (a prova continua a mesma, com a marcação nova):

| Asserção antiga (linha) | O que prova | Forma nova |
|---|---|---|
| 62-63 `Contain("Agenda")…("Semana anterior")…("Hoje")…("Próxima semana")…("Novo evento")` | o título, a navegação de período e a ação de cadastro chegam à tela | a barra do desktop inteira (setas `Período anterior`/`Próximo período`, `Hoje`, visões com `Semana` pressionada), a do tablet (`Semana anterior`/`Próxima semana`, `Hoje`, `Filtros`), a do mobile (`Semana anterior`/`Próxima semana`, sem `Hoje`), o link `Novo evento` do cabeçalho e o botão flutuante |
| 64 os dois títulos | a Diretoria vê os dois eventos | fica igual |
| 67-74 as células `10:00 – 11:00`, `14:30 – 16:00`, `Audiência`, `Reunião`, `Fórum Central`, `Diana Diretora, Pedro Produção`, `Diana Diretora` | horário no fuso do escritório, tipo, local e participantes na linha do evento (não em qualquer parte da página) | as mesmas linhas, na ordem, **dentro do cartão** da Semana do desktop (`aj-evento--coluna`) que leva à ficha daquele evento |
| 75 `Contain("Abrir")` | o evento pode ser aberto | o cartão inteiro é o link `href="/agenda/{id}"` (desktop e mobile) |
| 78-79 `Sem eventos.` e as sete `class="dia"` | dia vazio é dito e a semana tem sete dias | `>Sem eventos<` na Semana do desktop, sete colunas `aj-semana__dia` no desktop e sete seções `aj-secao-dia` no tablet |
| 82-83 a data de hoje | o dia de hoje é um dos cabeçalhos | o `aria-label` do cabeçalho de hoje (`RotulosDeAgenda.RotuloLongo(hoje, true)`, que termina em `, hoje`) no desktop |
| 86 `Participante\s*<select` | o filtro por participante é da Diretoria | `<span>Participante</span><select class="aj-campo-filtro__controle"` na barra do desktop |
| 98-101 (Produção) o horário, `NotContain` do evento restrito e de `14:30`, sem o filtro | a Produção só vê os seus e não tem o filtro | o mesmo, com as positivas no mesmo bloco (o horário dentro do cartão dela e o `<span>Tipo</span>` na barra do desktop) |
| 153-161 as `<option>` do participante | o inativo marcado e a ordem por nome | as mesmas, extraídas do `select` do `Participante` no bloco do desktop |
| 105-121, 124-134 | menu, anônimo, registro de consultas | não mudam |

Substitua o topo do arquivo (os `using`) e a classe `AgendaWebTests` inteira por:

```csharp
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Infrastructure.Persistencia;
using AtividadesJuridicas.Web.Components.Agenda;
using AtividadesJuridicas.Web.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AgendaWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");
    private Guid _admin;
    private Guid _pedro;
    private Guid _doPedro;
    private Guid _restrito;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    /// <summary>Hoje, no fuso do escritório: a agenda sem endereço abre nele.</summary>
    private DateOnly Hoje() => DateOnly.FromDateTime(_fuso.ParaLocal(DateTime.UtcNow));

    /// <summary>Hoje, no fuso do escritório, às <paramref name="hora"/>: sempre dentro da semana corrente.</summary>
    private DateTime HojeAs(int hora, int minuto = 0) => _fuso.ParaUtc(Hoje().ToDateTime(new TimeOnly(hora, minuto)));

    private async Task CriarEventosAsync()
    {
        // Participa o Pedro; o diretor é o criador e também participa.
        var doPedro = await _app.ExecutarAsync(_admin, new SalvarEvento(
            null, "Audiência de instrução do Pedro", null, "Fórum Central", TipoEvento.Audiencia, HojeAs(10), 60,
            null, null, null, [_pedro]));
        doPedro.Sucesso.Should().BeTrue(doPedro.Mensagem);
        _doPedro = doPedro.Valor!.Id;

        // Só a Diretoria participa: o Pedro não pode ver.
        var restrito = await _app.ExecutarAsync(_admin, new SalvarEvento(
            null, "Reunião reservada da diretoria", null, null, TipoEvento.Reuniao, HojeAs(14, 30), 90,
            null, null, null, []));
        restrito.Sucesso.Should().BeTrue(restrito.Mensagem);
        _restrito = restrito.Valor!.Id;
    }

    // A barra superior que a agenda declara (a do layout é "aj-barra-superior aj-barra-superior--padrao").
    private static string BarraDoMobile(string html)
    {
        var barra = Regex.Match(html, "<header class=\"aj-barra-superior\">.*?</header>", RegexOptions.Singleline);
        barra.Success.Should().BeTrue("a agenda declara a própria barra superior do mobile");
        return barra.Value;
    }

    // O cartão da Semana do desktop (forma Coluna) que leva à ficha do evento, com o período de origem.
    private static string CartaoDaSemana(string desktop, Guid id, string consulta = "")
    {
        var cartao = Regex.Match(desktop,
            $"<a class=\"(?=[^\"]*aj-evento--coluna)[^\"]*\" href=\"/agenda/{id}{Regex.Escape(consulta)}\">.*?</a>",
            RegexOptions.Singleline);
        cartao.Success.Should().BeTrue($"a Semana do desktop traz o cartão do evento {id} como link para a ficha");
        return cartao.Value;
    }

    // As consultas da agenda que o usuário fez (o registro de consultas guarda o período pedido no detalhe).
    private Task<List<RegistroAcesso>> ConsultasDaAgendaAsync(Guid usuario) =>
        _app.EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking()
            .Where(a => a.UsuarioId == usuario && a.TipoRecurso == "Evento" && a.Acao == AcaoDeAcesso.Listou)
            .OrderBy(a => a.DataHora)
            .ToListAsync());

    private static (DateTime De, DateTime Ate) PeriodoConsultado(RegistroAcesso registro)
    {
        using var detalhe = JsonDocument.Parse(registro.Detalhe!);
        return (detalhe.RootElement.GetProperty("DeUtc").GetDateTime(), detalhe.RootElement.GetProperty("AteUtc").GetDateTime());
    }

    [Fact]
    public async Task Diretor_ve_os_eventos_da_semana()
    {
        await CriarEventosAsync();
        var cliente = await _app.ClienteAutenticadoAsync();
        var hoje = Hoje();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/agenda"));

        html.Should().Contain("Audiência de instrução do Pedro").And.Contain("Reunião reservada da diretoria")
            .And.NotContain("Seu acesso foi encerrado");
        // Cabeçalho: o título, o período da Semana (padrão do desktop e do tablet) e a ação de cadastro nos dois lugares.
        html.Should().Contain("<h1 class=\"aj-titulo\">Agenda</h1>")
            .And.Contain($"<p class=\"aj-subtitulo\">{RotulosDeAgenda.SubtituloDaSemana(RotulosDeAgenda.SegundaDe(hoje))}</p>")
            .And.MatchRegex("<a href=\"/agenda/novo\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\">\\s*<svg[^>]*>.*?</svg>\\s*Novo evento</a>")
            .And.Contain("<a href=\"/agenda/novo\" class=\"aj-fab\" aria-label=\"Novo evento\">");

        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex(
            "(?s)<div class=\"aj-barra-filtros aj-barra-filtros--dividida\">\\s*<div class=\"aj-barra-filtros__grupo\">\\s*" +
            "<div class=\"aj-segmentado aj-segmentado--periodo\">\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__seta\" aria-label=\"Período anterior\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*</button>\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__hoje\"[^>]*>Hoje</button>\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__seta\" aria-label=\"Próximo período\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*</button>\\s*</div>\\s*" +
            "<div class=\"aj-segmentado aj-segmentado--periodo\" role=\"group\" aria-label=\"Visualização da agenda\">\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>Dia</button>\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"true\"[^>]*>Semana</button>\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>Mês</button>\\s*</div>\\s*</div>\\s*" +
            "<form class=\"aj-barra-filtros__grupo\" role=\"search\"[^>]*>\\s*" +
            "<label class=\"aj-campo-filtro aj-campo-filtro--tipo\">\\s*<span>Tipo</span>\\s*<select class=\"aj-campo-filtro__controle\"[^>]*>\\s*" +
            "<option value=\"\"[^>]*>Todos</option>\\s*<option value=\"Audiencia\"[^>]*>Audiência</option>\\s*<option value=\"Reuniao\"[^>]*>Reunião</option>\\s*" +
            "<option value=\"Prazo\"[^>]*>Prazo</option>\\s*<option value=\"Outro\"[^>]*>Outro</option>\\s*</select>\\s*</label>\\s*" +
            "<label class=\"aj-campo-filtro aj-campo-filtro--pessoa\">\\s*<span>Participante</span>\\s*<select class=\"aj-campo-filtro__controle\"[^>]*>.*?</select>\\s*</label>\\s*" +
            "<button type=\"submit\" class=\"aj-botao aj-botao--secundario aj-botao--com-icone-pequeno\">\\s*<svg[^>]*>.*?</svg>\\s*Filtrar</button>\\s*</form>\\s*</div>");
        // Cada evento no seu cartão da Semana, que leva à ficha: tipo, horário no fuso do escritório, título, local e
        // participantes, nessa ordem e dentro do mesmo cartão.
        CartaoDaSemana(desktop, _doPedro).Should().Contain("aj-evento--audiencia").And.MatchRegex(
            "(?s)>Audiência<.*>10:00 – 11:00<.*>Audiência de instrução do Pedro<.*>Fórum Central<.*>Diana Diretora, Pedro Produção<");
        CartaoDaSemana(desktop, _restrito).Should().Contain("aj-evento--reuniao").And.MatchRegex(
            "(?s)>Reunião<.*>14:30 – 16:00<.*>Reunião reservada da diretoria<.*>Diana Diretora<");
        // A semana tem sete dias; dia sem eventos é dito; hoje é marcado no cabeçalho do dia.
        Regex.Matches(desktop, "<section class=\"aj-semana__dia[\" ]").Count.Should().Be(7);
        desktop.Should().Contain(">Sem eventos<").And.Contain($"aria-label=\"{RotulosDeAgenda.RotuloLongo(hoje, true)}\"");

        var tablet = BlocoDeVisao.De(html, "aj-so-tablet");
        tablet.Should().MatchRegex(
            "(?s)<div class=\"aj-barra-filtros aj-barra-filtros--plana\">\\s*<div class=\"aj-segmentado aj-segmentado--periodo\">\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__seta\" aria-label=\"Semana anterior\"[^>]*>.*?</button>\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__hoje\"[^>]*>Hoje</button>\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__seta\" aria-label=\"Próxima semana\"[^>]*>.*?</button>\\s*</div>\\s*" +
            "<div class=\"aj-segmentado aj-segmentado--periodo\" role=\"group\" aria-label=\"Visualização\">\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>Dia</button>\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__opcao aj-segmentado__opcao--larga\" aria-pressed=\"true\"[^>]*>Semana</button>\\s*" +
            "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>Mês</button>\\s*</div>\\s*" +
            "<button type=\"button\" class=\"aj-botao aj-botao--secundario aj-botao--com-icone aj-botao--ao-fim\" aria-expanded=\"false\" aria-controls=\"filtros-da-agenda\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Filtros</button>\\s*</div>");
        Regex.Matches(BlocoDeVisao.De(html, "aj-tablet-mobile"), "<section class=\"aj-secao-dia[\" ]").Count.Should().Be(7);

        // Mobile: sem visão no endereço, o Dia de hoje, com os cartões que levam à ficha.
        var mobile = BlocoDeVisao.De(html, "aj-so-mobile");
        mobile.Should().Contain("aj-dia-escolhido").And.Contain($"href=\"/agenda/{_doPedro}\"").And.Contain($"href=\"/agenda/{_restrito}\"");

        var barra = BarraDoMobile(html);
        barra.Should().Contain("<h1 class=\"aj-titulo\">Agenda</h1>")
            .And.MatchRegex("(?s)<h1 class=\"aj-titulo\">Agenda</h1>.*?aria-label=\"Filtrar agenda\".*?<a href=\"/minha-senha\" class=\"aj-avatar\"")
            .And.MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--icone aj-barra-superior__acao\" aria-label=\"Filtrar agenda\" aria-expanded=\"false\" aria-controls=\"filtros-da-agenda\"[^>]*>\\s*<svg")
            .And.MatchRegex(
                "(?s)<div class=\"aj-segmentado aj-segmentado--recuado aj-segmentado--claro\" role=\"group\" aria-label=\"Visualização\">\\s*" +
                "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"true\"[^>]*>Dia</button>\\s*" +
                "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>Semana</button>\\s*" +
                "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>Mês</button>\\s*</div>")
            .And.MatchRegex(
                $"(?s)<div class=\"aj-barra-superior__periodo\">\\s*<strong>{RotulosDeAgenda.MesDoMobile(hoje)}</strong>\\s*<div class=\"aj-barra-superior__setas\">\\s*" +
                "<button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Semana anterior\"[^>]*>.*?</button>\\s*" +
                "<button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Próxima semana\"[^>]*>.*?</button>\\s*</div>\\s*</div>")
            .And.Contain("aj-faixa-dias");
        // O mobile não tem subtítulo nem "Hoje" (os quadros).
        barra.Should().NotContain("aj-subtitulo").And.NotContain(">Hoje<");
    }

    [Fact]
    public async Task Producao_ve_so_os_seus()
    {
        await CriarEventosAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/agenda"));

        html.Should().Contain("Audiência de instrução do Pedro")
            .And.Contain("<a href=\"/agenda/novo\" class=\"aj-fab\" aria-label=\"Novo evento\">");
        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        CartaoDaSemana(desktop, _doPedro).Should().MatchRegex(">10:00 – 11:00<");
        desktop.Should().Contain("<span>Tipo</span>");
        html.Should().NotContain("Reunião reservada da diretoria").And.NotContain("14:30").And.NotContain(_restrito.ToString())
            .And.NotContain("Seu acesso foi encerrado");
        // O filtro por participante é só da Diretoria (a positiva do mesmo bloco é o Tipo, acima).
        desktop.Should().NotContain("<span>Participante</span>");
    }

    [Fact]
    public async Task Menu_mostra_agenda_para_todos()
    {
        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await doDiretor.GetStringAsync("/")).Should().Contain("href=\"/agenda\"");
        (await doPedro.GetStringAsync("/")).Should().Contain("href=\"/agenda\"");
    }

    [Fact]
    public async Task Anonimo_e_redirecionado_ao_login()
    {
        var resposta = await _app.NovoCliente().GetAsync("/agenda");

        resposta.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resposta.Headers.Location!.AbsolutePath.Should().Be("/conta/entrar");
    }

    [Fact]
    public async Task Listagem_fica_no_registro_de_consultas()
    {
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        (await cliente.GetAsync("/agenda")).EnsureSuccessStatusCode();

        var acessos = await _app.EmEscopoAsync(null, sp =>
            sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking().ToListAsync());
        acessos.Should().Contain(a =>
            a.TipoRecurso == "Evento" && a.Acao == AcaoDeAcesso.Listou && a.UsuarioId == _pedro);
    }

    // Review Focus 1: endereço sem período ou com período estranho abre hoje, no padrão de cada largura, sem erro e
    // com uma consulta só (a semana de hoje, que serve à Semana do desktop e do tablet e ao Dia do mobile).
    [Theory]
    [InlineData("/agenda")]
    [InlineData("/agenda?visao=xyz")]
    [InlineData("/agenda?data=2026-13-40")]
    [InlineData("/agenda?data=1999-01-01")]
    [InlineData("/agenda?visao=xyz&data=ontem")]
    public async Task Endereco_sem_periodo_ou_estranho_abre_hoje_no_padrao_com_uma_consulta_so(string rota)
    {
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);
        var segunda = RotulosDeAgenda.SegundaDe(Hoje());

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync(rota));

        html.Should().Contain($"<p class=\"aj-subtitulo\">{RotulosDeAgenda.SubtituloDaSemana(segunda)}</p>")
            .And.NotContain("Seu acesso foi encerrado").And.NotContain("Não foi possível consultar a agenda.");
        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex("aria-pressed=\"true\"[^>]*>Semana</button>")
            .And.NotMatchRegex("aria-pressed=\"true\"[^>]*>(Dia|Mês)</button>");
        BarraDoMobile(html).Should().MatchRegex("aria-pressed=\"true\"[^>]*>Dia</button>")
            .And.NotMatchRegex("aria-pressed=\"true\"[^>]*>(Semana|Mês)</button>");
        var consulta = (await ConsultasDaAgendaAsync(_pedro)).Should().ContainSingle().Which;
        PeriodoConsultado(consulta).Should().Be((_fuso.InicioDoDiaEmUtc(segunda), _fuso.InicioDoDiaEmUtc(segunda.AddDays(7))));
    }

    // O Mês consulta as semanas inteiras que cobrem o mês (outubro de 2026: de 28/09 a 01/11), numa consulta só.
    [Fact]
    public async Task Mes_consulta_as_semanas_inteiras_do_mes()
    {
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/agenda?visao=mes&data=2026-10-15"));

        html.Should().Contain("<p class=\"aj-subtitulo\">Outubro de 2026</p>").And.Contain("aj-mes__grade")
            .And.Contain("<a href=\"/agenda/novo?visao=mes&data=2026-10-15\" class=\"aj-fab\" aria-label=\"Novo evento\">");
        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex("aria-pressed=\"true\"[^>]*>Mês</button>");
        BarraDoMobile(html).Should().Contain("<strong>Outubro 2026</strong>").And.Contain("aj-faixa-dias--mes")
            .And.Contain("aria-label=\"Mês anterior\"").And.Contain("aria-label=\"Próximo mês\"");
        var consulta = (await ConsultasDaAgendaAsync(_pedro)).Should().ContainSingle().Which;
        PeriodoConsultado(consulta).Should().Be(
            (_fuso.InicioDoDiaEmUtc(new DateOnly(2026, 9, 28)), _fuso.InicioDoDiaEmUtc(new DateOnly(2026, 11, 2))));
    }

    // O período de onde se veio segue para a ficha (cartão) e para o novo evento.
    [Fact]
    public async Task Cartao_e_novo_evento_levam_o_periodo_de_origem()
    {
        await CriarEventosAsync();
        var cliente = await _app.ClienteAutenticadoAsync();
        var consulta = $"?visao=semana&data={Hoje().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync($"/agenda{consulta}"));

        html.Should().Contain($"<a href=\"/agenda/novo{consulta}\" class=\"aj-fab\" aria-label=\"Novo evento\">");
        CartaoDaSemana(BlocoDeVisao.De(html, "aj-so-desktop"), _doPedro, consulta).Should().Contain("Audiência de instrução do Pedro");
        // Nenhum cartão perde a origem.
        html.Should().NotContain($"href=\"/agenda/{_doPedro}\"");
    }

    [Fact]
    public async Task Diretor_encontra_no_filtro_o_usuario_inativo_marcado_e_em_ordem_de_nome_e_producao_nao_tem_o_filtro()
    {
        var producao = await _app.EmEscopoAsync(null, async sp =>
            (await sp.GetRequiredService<AppDbContext>().Perfis.AsNoTracking()
                .FirstAsync(p => p.Tipo == TipoPerfil.Producao && p.Ativo)).Id);
        var iara = await _app.ExecutarAsync(_admin, new CriarUsuario("Iara Inativa", "iara@x.com", "senha-da-iara", producao, null));
        iara.Sucesso.Should().BeTrue(iara.Mensagem);
        (await _app.ExecutarAsync(_admin, new DefinirAtivoUsuario(iara.Valor, false))).Sucesso.Should().BeTrue();

        var doDiretor = await _app.ClienteAutenticadoAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var htmlDoDiretor = WebUtility.HtmlDecode(await doDiretor.GetStringAsync("/agenda"));
        var htmlDoPedro = WebUtility.HtmlDecode(await doPedro.GetStringAsync("/agenda"));

        var select = Regex.Match(BlocoDeVisao.De(htmlDoDiretor, "aj-so-desktop"),
            "(?s)<span>Participante</span>\\s*<select class=\"aj-campo-filtro__controle\"[^>]*>(.*?)</select>");
        select.Success.Should().BeTrue();
        select.Groups[1].Value.Should().MatchRegex(@">\s*Iara Inativa \(inativo\)\s*<");
        var nomes = Regex.Matches(select.Groups[1].Value, @"<option[^>]*>\s*([^<]*?)\s*</option>")
            .Select(m => m.Groups[1].Value).ToList();
        nomes.Should().Equal("Todos", "Diana Diretora", "Iara Inativa (inativo)", "Pedro Produção");

        var desktopDoPedro = BlocoDeVisao.De(htmlDoPedro, "aj-so-desktop");
        desktopDoPedro.Should().Contain("<span>Tipo</span>");
        desktopDoPedro.Should().NotContain("<span>Participante</span>");
        htmlDoPedro.Should().NotContain("Iara");
    }
}
```

Notas para quem implementa:

- Os cartões, as colunas e as seções são marcação das Tarefas 6 a 8 (`<a class="aj-evento …" href>`, `section.aj-semana__dia`, `section.aj-secao-dia`, cada linha do cartão num elemento próprio, os participantes unidos por `, `). Se alguma delas escreveu outro elemento, outra ordem de atributos ou outro separador, ajuste a expressão do teste (nunca a prova) e registre no relatório.
- O bloco `aj-so-desktop` reúne, sem `visao`, a grade da Semana e a régua do Dia que o invólucro `aj-so-mobile` esconde (o `DiaDaAgenda` emite as três larguras). Por isso o cartão é procurado pela forma (`aj-evento--coluna`) e as colunas por `aj-semana__dia`.
- `PeriodoConsultado` lê o detalhe do registro (`{"DeUtc":"…Z","AteUtc":"…Z",…}`, de `ListarEventos.Detalhe`); a comparação de `DateTime` é por instante.

- [ ] **Step 2: Testes de clique (bUnit, devem falhar)**

Crie `tests/AtividadesJuridicas.IntegrationTests/AgendaInterativaTests.cs`. A página é desenhada com `ComBarraSuperior<Agenda>`, para a barra do mobile (o segmentado claro, as setas, a faixa e a grade compacta, o `Filtrar agenda`) chegar ao HTML. O período vem do endereço: o teste navega antes de desenhar, e depois de cada clique confere o `Uri` do `NavigationManager` falso do bUnit. O bUnit registra o provedor de `SupplyParameterFromQuery`, que avisa a página a cada troca de endereço (é o que faz a página reagir ao `NavigateTo` dela mesma, como o roteador faz no navegador).

As datas fixas ficam na semana de 05/10 a 11/10/2026 e em semanas depois dela, sem depender do "hoje" do relógio; onde o teste precisa de hoje, ele o calcula como a tela (`FusoDoEscritorio.ParaLocal(DateTime.UtcNow)`).

```csharp
using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AtividadesJuridicas.Application.Agenda;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Agenda;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Components.Agenda;
using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PaginaDaAgenda = AtividadesJuridicas.Web.Components.Pages.Agenda;

namespace AtividadesJuridicas.IntegrationTests;

public class AgendaInterativaTests : TelaInterativa
{
    private static readonly FusoDoEscritorio Fuso = new("America/Sao_Paulo");

    private static readonly UsuarioResumo Diana = new(
        AtorDaDiretoria.Id, "Diana Diretora", "diana@x.com", AtorDaDiretoria.PerfilId, "Diretoria", TipoPerfil.Diretoria, null, true, null);

    private static readonly UsuarioResumo Pedro = new(
        AtorDaProducao.Id, "Pedro Produção", "pedro@x.com", AtorDaProducao.PerfilId, "Produção", TipoPerfil.Producao, null, true, null);

    // Onde ficam os controles de cada largura.
    private const string Desktop = ".aj-barra-filtros--dividida";
    private const string Tablet = ".aj-barra-filtros--plana";
    private const string Mobile = ".aj-barra-superior__setas";
    private const string VisoesDoDesktop = ".aj-barra-filtros--dividida [aria-label='Visualização da agenda']";
    private const string VisoesDoTablet = ".aj-barra-filtros--plana [aria-label='Visualização']";
    private const string VisoesDoMobile = ".aj-barra-superior .aj-segmentado--claro";

    private static DateTime Local(int ano, int mes, int dia, int hora, int minuto = 0) =>
        Fuso.ParaUtc(new DateTime(ano, mes, dia, hora, minuto, 0));

    private static EventoResumo Evento(string titulo, DateTime inicio, int minutos = 60, TipoEvento tipo = TipoEvento.Reuniao) =>
        new(Guid.CreateVersion7(), titulo, tipo, inicio, inicio.AddMinutes(minutos), null, null, null, ["Diana Diretora"]);

    private static Resultado<IReadOnlyList<EventoResumo>> Eventos(params EventoResumo[] eventos) =>
        Resultado<IReadOnlyList<EventoResumo>>.Ok(eventos);

    private static DateOnly Hoje() => DateOnly.FromDateTime(Fuso.ParaLocal(DateTime.UtcNow));

    private NavigationManager Navegacao => Services.GetRequiredService<NavigationManager>();

    private string Endereco => Navegacao.Uri;

    private List<ListarEventos> Consultas => [.. Despachante.Recebidas.OfType<ListarEventos>()];

    private IRenderedComponent<ComBarraSuperior<PaginaDaAgenda>> Agenda(
        string endereco, UsuarioAutenticado? ator = null, params EventoResumo[] eventos)
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(ator ?? AtorDaDiretoria)
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Diana, Pedro]))
            .Responde<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(Eventos(eventos));
        // A agenda lê visao e data do endereço: a navegação de teste começa nele.
        Navegacao.NavigateTo(endereco);
        return Desenhar<ComBarraSuperior<PaginaDaAgenda>>();
    }

    private static string Texto(IElement elemento) => Regex.Replace(elemento.TextContent, "\\s+", " ").Trim();

    private static IElement Botao<T>(IRenderedComponent<T> tela, string onde, string rotulo) where T : IComponent =>
        tela.Find($"{onde} button[aria-label='{rotulo}']");

    private static IElement Opcao<T>(IRenderedComponent<T> tela, string grupo, string texto) where T : IComponent =>
        tela.FindAll($"{grupo} .aj-segmentado__opcao").Single(b => b.TextContent == texto);

    private static string Pressionada<T>(IRenderedComponent<T> tela, string grupo) where T : IComponent =>
        tela.Find($"{grupo} .aj-segmentado__opcao[aria-pressed=true]").TextContent;

    private static string Subtitulo<T>(IRenderedComponent<T> tela) where T : IComponent =>
        Texto(tela.Find("header.aj-cabecalho p.aj-subtitulo"));

    // Um campo de filtro pelo nome ("Tipo", "Participante"), na barra do desktop ou no painel.
    private static IElement Campo<T>(IRenderedComponent<T> tela, string onde, string nome) where T : IComponent =>
        tela.Find(onde).QuerySelectorAll("label.aj-campo-filtro")
            .Single(l => l.QuerySelector("span")!.TextContent == nome).QuerySelector("select")!;

    private static IElement CampoDaBarra<T>(IRenderedComponent<T> tela, string nome) where T : IComponent =>
        Campo(tela, "form.aj-barra-filtros__grupo", nome);

    private static IElement CampoDoPainel<T>(IRenderedComponent<T> tela, string nome) where T : IComponent =>
        Campo(tela, "form#filtros-da-agenda", nome);

    // Review Focus 1: sem endereço ou com endereço estranho, hoje no padrão de cada largura, com uma consulta só.
    [Theory]
    [InlineData("/agenda")]
    [InlineData("/agenda?visao=xyz")]
    [InlineData("/agenda?data=2026-13-40")]
    [InlineData("/agenda?data=1999-01-01")]
    [InlineData("/agenda?visao=xyz&data=08-10-2026")]
    public void Sem_endereco_ou_com_endereco_estranho_abre_hoje_no_padrao_com_uma_consulta_so(string endereco)
    {
        var tela = Agenda(endereco);
        var hoje = Hoje();
        var segunda = RotulosDeAgenda.SegundaDe(hoje);

        var consulta = Consultas.Should().ContainSingle().Which;
        consulta.DeUtc.Should().Be(Fuso.InicioDoDiaEmUtc(segunda));
        consulta.AteUtc.Should().Be(Fuso.InicioDoDiaEmUtc(segunda.AddDays(7)));
        Subtitulo(tela).Should().Be(RotulosDeAgenda.SubtituloDaSemana(segunda));
        Pressionada(tela, VisoesDoDesktop).Should().Be("Semana");
        Pressionada(tela, VisoesDoTablet).Should().Be("Semana");
        Pressionada(tela, VisoesDoMobile).Should().Be("Dia");
        // A Semana no desktop e no tablet, o Dia no mobile, sobre os mesmos eventos.
        tela.FindAll("div.aj-desktop-tablet .aj-semana").Should().ContainSingle();
        tela.FindAll("div.aj-so-mobile .aj-dia-escolhido").Should().ContainSingle();
        tela.FindAll(".aj-barra-superior .aj-faixa-dias").Should().ContainSingle();
        Texto(tela.Find(".aj-barra-superior__periodo strong")).Should().Be(RotulosDeAgenda.MesDoMobile(hoje));
        tela.Markup.Should().NotContain("aj-mes__grade").And.NotContain("Seu acesso foi encerrado");
    }

    [Fact]
    public void Visao_mes_consulta_as_semanas_inteiras_do_mes_e_mostra_a_grade_compacta_no_mobile()
    {
        var tela = Agenda("/agenda?visao=mes&data=2026-10-15");

        var consulta = Consultas.Should().ContainSingle().Which;
        consulta.DeUtc.Should().Be(Fuso.InicioDoDiaEmUtc(new DateOnly(2026, 9, 28)));
        consulta.AteUtc.Should().Be(Fuso.InicioDoDiaEmUtc(new DateOnly(2026, 11, 2)));
        Subtitulo(tela).Should().Be("Outubro de 2026");
        Texto(tela.Find(".aj-barra-superior__periodo strong")).Should().Be("Outubro 2026");
        Pressionada(tela, VisoesDoDesktop).Should().Be("Mês");
        Pressionada(tela, VisoesDoTablet).Should().Be("Mês");
        Pressionada(tela, VisoesDoMobile).Should().Be("Mês");
        tela.FindAll(".aj-mes").Should().ContainSingle();
        tela.FindAll(".aj-barra-superior .aj-faixa-dias--mes").Should().ContainSingle();
        tela.FindAll(".aj-semana").Should().BeEmpty();
        tela.FindAll($"{Mobile} button").Select(b => b.GetAttribute("aria-label")).Should().Equal("Mês anterior", "Próximo mês");
        tela.Find("header.aj-cabecalho a.aj-botao--primario").GetAttribute("href").Should().Be("/agenda/novo?visao=mes&data=2026-10-15");
    }

    [Theory]
    // Desktop: uma semana (Semana e sem visão), um dia (Dia) ou um mês (Mês); o dia que não existe vira o último do mês.
    [InlineData("/agenda?data=2026-10-08", Desktop, "Período anterior", "/agenda?data=2026-10-01")]
    [InlineData("/agenda?data=2026-10-08", Desktop, "Próximo período", "/agenda?data=2026-10-15")]
    [InlineData("/agenda?visao=semana&data=2026-10-08", Desktop, "Próximo período", "/agenda?visao=semana&data=2026-10-15")]
    [InlineData("/agenda?visao=dia&data=2026-10-08", Desktop, "Período anterior", "/agenda?visao=dia&data=2026-10-07")]
    [InlineData("/agenda?visao=mes&data=2026-10-31", Desktop, "Próximo período", "/agenda?visao=mes&data=2026-11-30")]
    [InlineData("/agenda?visao=mes&data=2026-01-31", Desktop, "Próximo período", "/agenda?visao=mes&data=2026-02-28")]
    // Tablet: os rótulos de cada visão.
    [InlineData("/agenda?data=2026-10-08", Tablet, "Semana anterior", "/agenda?data=2026-10-01")]
    [InlineData("/agenda?visao=dia&data=2026-10-08", Tablet, "Próximo dia", "/agenda?visao=dia&data=2026-10-09")]
    [InlineData("/agenda?visao=mes&data=2026-10-08", Tablet, "Mês anterior", "/agenda?visao=mes&data=2026-09-08")]
    // Mobile: no Dia e na Semana as setas andam a faixa de semana em semana; no Mês, de mês em mês.
    [InlineData("/agenda?data=2026-10-08", Mobile, "Próxima semana", "/agenda?data=2026-10-15")]
    [InlineData("/agenda?visao=dia&data=2026-10-08", Mobile, "Semana anterior", "/agenda?visao=dia&data=2026-10-01")]
    [InlineData("/agenda?visao=semana&data=2026-10-08", Mobile, "Próxima semana", "/agenda?visao=semana&data=2026-10-15")]
    [InlineData("/agenda?visao=mes&data=2026-10-08", Mobile, "Próximo mês", "/agenda?visao=mes&data=2026-11-08")]
    public void Setas_de_cada_largura_levam_ao_periodo_vizinho(string endereco, string onde, string rotulo, string destino)
    {
        var tela = Agenda(endereco);

        Botao(tela, onde, rotulo).Click();

        Endereco.Should().EndWith(destino);
    }

    [Fact]
    public void Hoje_leva_a_data_de_hoje_mantendo_a_visao_e_o_mobile_nao_tem_hoje()
    {
        var tela = Agenda("/agenda?visao=dia&data=2020-01-06");
        var hoje = Hoje().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        tela.Find($"{Desktop} .aj-segmentado__hoje").Click();

        Endereco.Should().EndWith($"/agenda?visao=dia&data={hoje}");

        Navegacao.NavigateTo("/agenda?data=2020-01-06");
        tela.Find($"{Tablet} .aj-segmentado__hoje").Click();

        Endereco.Should().EndWith($"/agenda?data={hoje}");
        // O mobile tem as duas setas e nenhum "Hoje" (o quadro).
        tela.FindAll($"{Mobile} button").Should().HaveCount(2);
        tela.FindAll(".aj-barra-superior button").Where(b => Texto(b) == "Hoje").Should().BeEmpty();
    }

    [Fact]
    public void Visoes_trocam_a_visao_com_a_mesma_data()
    {
        var tela = Agenda("/agenda?data=2026-10-08");

        Opcao(tela, VisoesDoDesktop, "Mês").Click();

        Endereco.Should().EndWith("/agenda?visao=mes&data=2026-10-08");
        Pressionada(tela, VisoesDoDesktop).Should().Be("Mês");

        Opcao(tela, VisoesDoMobile, "Semana").Click();
        Endereco.Should().EndWith("/agenda?visao=semana&data=2026-10-08");

        Opcao(tela, VisoesDoTablet, "Dia").Click();
        Endereco.Should().EndWith("/agenda?visao=dia&data=2026-10-08");

        // Sem data no endereço, a visão vai sozinha (a data continua sendo hoje).
        Navegacao.NavigateTo("/agenda");
        Opcao(tela, VisoesDoMobile, "Semana").Click();
        Endereco.Should().EndWith("/agenda?visao=semana");
    }

    [Fact]
    public void Faixa_de_dias_troca_a_data_e_na_semana_leva_ao_dia()
    {
        var tela = Agenda("/agenda?data=2026-10-08");

        tela.Find(".aj-faixa-dias button[aria-label^='Segunda, 05/10']").Click();

        Endereco.Should().EndWith("/agenda?data=2026-10-05");

        Navegacao.NavigateTo("/agenda?visao=semana&data=2026-10-08");
        tela.Find(".aj-faixa-dias button[aria-label^='Sexta, 09/10']").Click();

        Endereco.Should().EndWith("/agenda?visao=dia&data=2026-10-09");
        Consultas.Should().ContainSingle();   // sempre a mesma semana
    }

    [Fact]
    public void Grade_compacta_do_mes_troca_a_data_e_fica_no_mes()
    {
        var tela = Agenda("/agenda?visao=mes&data=2026-10-08");

        tela.FindAll(".aj-faixa-dias--mes .aj-faixa-dias__dia")
            .Single(d => !d.ClassList.Contains("aj-faixa-dias__dia--fora") && Texto(d.QuerySelector(".aj-faixa-dias__numero")!) == "20")
            .Click();

        Endereco.Should().EndWith("/agenda?visao=mes&data=2026-10-20");
        Pressionada(tela, VisoesDoMobile).Should().Be("Mês");
        Consultas.Should().ContainSingle();   // o mesmo mês: a mesma consulta
    }

    [Fact]
    public void Mais_eventos_da_celula_do_mes_leva_ao_dia_daquela_data()
    {
        var tela = Agenda("/agenda?visao=mes&data=2026-10-08", AtorDaDiretoria,
            Evento("Primeiro", Local(2026, 10, 14, 9)), Evento("Segundo", Local(2026, 10, 14, 10)),
            Evento("Terceiro", Local(2026, 10, 14, 11)), Evento("Quarto", Local(2026, 10, 14, 12)),
            Evento("Quinto", Local(2026, 10, 14, 13)));

        tela.FindAll("button[aria-label='Mais 2 eventos em 14/10']").Should().ContainSingle().Which.Click();

        Endereco.Should().EndWith("/agenda?visao=dia&data=2026-10-14");
        Pressionada(tela, VisoesDoDesktop).Should().Be("Dia");
    }

    [Fact]
    public void Trocar_de_periodo_dentro_do_mesmo_intervalo_nao_repete_a_consulta()
    {
        var tela = Agenda("/agenda?data=2026-10-08");
        Consultas.Should().ContainSingle();

        Opcao(tela, VisoesDoDesktop, "Dia").Click();

        Endereco.Should().EndWith("/agenda?visao=dia&data=2026-10-08");
        Pressionada(tela, VisoesDoDesktop).Should().Be("Dia");
        Subtitulo(tela).Should().Be("Quinta-feira, 08/10/2026");
        Consultas.Should().ContainSingle();   // o Dia consulta a mesma semana

        Botao(tela, Desktop, "Próximo período").Click();
        Subtitulo(tela).Should().Be("Sexta-feira, 09/10/2026");
        Botao(tela, Desktop, "Próximo período").Click();
        Botao(tela, Desktop, "Próximo período").Click();
        Subtitulo(tela).Should().Be("Domingo, 11/10/2026");
        Consultas.Should().ContainSingle();

        Botao(tela, Desktop, "Próximo período").Click();   // segunda, 12/10: outra semana

        Consultas.Should().HaveCount(2);
        Consultas[1].DeUtc.Should().Be(Fuso.InicioDoDiaEmUtc(new DateOnly(2026, 10, 12)));
        Consultas[1].AteUtc.Should().Be(Fuso.InicioDoDiaEmUtc(new DateOnly(2026, 10, 19)));
    }

    [Fact]
    public void Filtro_fica_ao_trocar_de_periodo_e_filtrar_sem_mudanca_nao_consulta()
    {
        var tela = Agenda("/agenda?data=2026-10-08");
        CampoDaBarra(tela, "Tipo").Change(nameof(TipoEvento.Prazo));
        CampoDaBarra(tela, "Participante").Change(Pedro.Id.ToString());

        tela.Find("form.aj-barra-filtros__grupo").Submit();

        Consultas.Should().HaveCount(2);
        Consultas[1].Tipo.Should().Be(TipoEvento.Prazo);
        Consultas[1].ParticipanteId.Should().Be(Pedro.Id);
        Consultas[1].DeUtc.Should().Be(Consultas[0].DeUtc);

        tela.Find("form.aj-barra-filtros__grupo").Submit();
        Consultas.Should().HaveCount(2);   // o mesmo período e o mesmo filtro

        Botao(tela, Desktop, "Próximo período").Click();

        Consultas.Should().HaveCount(3);
        Consultas[2].DeUtc.Should().Be(Fuso.InicioDoDiaEmUtc(new DateOnly(2026, 10, 12)));
        Consultas[2].Tipo.Should().Be(TipoEvento.Prazo);
        Consultas[2].ParticipanteId.Should().Be(Pedro.Id);
        CampoDaBarra(tela, "Tipo").QuerySelector("option[selected]")!.TextContent.Should().Be("Prazo");
        CampoDaBarra(tela, "Participante").QuerySelector("option[selected]")!.TextContent.Should().Be("Pedro Produção");
    }

    [Fact]
    public void Painel_de_filtros_do_tablet_abre_com_o_foco_no_tipo_filtrar_aplica_e_fecha_e_limpar_volta_a_todos()
    {
        var tela = Agenda("/agenda?data=2026-10-08");
        var botao = tela.Find($"{Tablet} button[aria-controls=filtros-da-agenda]");
        Texto(botao).Should().Be("Filtros");
        botao.GetAttribute("aria-expanded").Should().Be("false");
        tela.FindAll("form#filtros-da-agenda").Should().BeEmpty();

        botao.Click();

        tela.Find($"{Tablet} button[aria-controls=filtros-da-agenda]").GetAttribute("aria-expanded").Should().Be("true");
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(CampoDoPainel(tela, "Tipo"));
        CampoDoPainel(tela, "Tipo").QuerySelectorAll("option").Select(o => o.TextContent)
            .Should().Equal("Todos", "Audiência", "Reunião", "Prazo", "Outro");
        CampoDoPainel(tela, "Participante").QuerySelectorAll("option").Select(o => o.TextContent)
            .Should().Equal("Todos", "Diana Diretora", "Pedro Produção");
        Texto(tela.Find("form#filtros-da-agenda .aj-painel__acoes")).Should().Be("Limpar Filtrar");

        CampoDoPainel(tela, "Tipo").Change(nameof(TipoEvento.Audiencia));
        tela.Find("form#filtros-da-agenda").Submit();

        Consultas.Should().HaveCount(2);
        Consultas[1].Tipo.Should().Be(TipoEvento.Audiencia);
        tela.FindAll("form#filtros-da-agenda").Should().BeEmpty();
        tela.Find($"{Tablet} button[aria-controls=filtros-da-agenda]").GetAttribute("aria-expanded").Should().Be("false");

        tela.Find($"{Tablet} button[aria-controls=filtros-da-agenda]").Click();
        tela.Find("form#filtros-da-agenda").QuerySelectorAll("button").Single(b => b.TextContent == "Limpar").Click();

        Consultas.Should().HaveCount(2);   // limpar não consulta
        CampoDoPainel(tela, "Tipo").QuerySelector("option[selected]")!.TextContent.Should().Be("Todos");
        tela.Find("form#filtros-da-agenda").Submit();
        Consultas.Should().HaveCount(3);
        Consultas[2].Tipo.Should().BeNull();
        Consultas[2].ParticipanteId.Should().BeNull();
    }

    [Fact]
    public void Filtrar_agenda_do_mobile_abre_o_mesmo_painel_e_a_producao_so_tem_o_tipo()
    {
        var tela = Agenda("/agenda?data=2026-10-08", AtorDaProducao);
        var botao = tela.Find(".aj-barra-superior button[aria-label='Filtrar agenda']");
        botao.GetAttribute("aria-controls").Should().Be("filtros-da-agenda");

        botao.Click();

        tela.Find(".aj-barra-superior button[aria-label='Filtrar agenda']").GetAttribute("aria-expanded").Should().Be("true");
        tela.Find("form#filtros-da-agenda").QuerySelectorAll("label.aj-campo-filtro span").Select(s => s.TextContent)
            .Should().Equal("Tipo");
        tela.Find("form.aj-barra-filtros__grupo").QuerySelectorAll("label.aj-campo-filtro span").Select(s => s.TextContent)
            .Should().Equal("Tipo");
        Despachante.Recebidas.OfType<ListarUsuarios>().Should().BeEmpty();
        Consultas.Should().OnlyContain(c => c.ParticipanteId == null);
    }

    [Fact]
    public void Recado_de_evento_salvo_com_sobreposicao_aparece_nas_tres_redacoes_e_sai_ao_trocar_de_periodo()
    {
        var recados = Services.GetRequiredService<Recados>();
        recados.Deixar("Evento salvo.", TipoDeAviso.Sucesso,
            [new SobreposicaoDeAgenda(Guid.NewGuid(), "Rafael Teles", Local(2026, 10, 7, 10), Local(2026, 10, 7, 11, 30))]);

        var tela = Agenda("/agenda?visao=semana&data=2026-10-07");

        tela.FindAll("p.aj-aviso--frase").Select(Texto).Should().Equal(
            "Evento salvo. Atenção: Rafael Teles já tem evento das 10:00 às 11:30 (07/10).",
            "Evento salvo. Rafael Teles já tem evento das 10:00 às 11:30 (07/10).",
            "Rafael Teles já tem evento das 10:00 às 11:30 em 07/10.");
        // No topo do conteúdo, antes dos dias.
        tela.Markup.IndexOf("aj-aviso--frase", StringComparison.Ordinal)
            .Should().BeLessThan(tela.Markup.IndexOf("aj-semana", StringComparison.Ordinal));
        recados.Retirar().Should().BeNull();

        Botao(tela, Desktop, "Próximo período").Click();

        tela.FindAll("p.aj-aviso--frase").Should().BeEmpty();
        tela.FindAll(".aj-semana").Should().ContainSingle();
    }

    [Fact]
    public void Recado_de_evento_excluido_e_um_aviso_de_sucesso()
    {
        Services.GetRequiredService<Recados>().Deixar("Evento excluído.");

        var tela = Agenda("/agenda?data=2026-10-08");

        Texto(tela.Find("p.aj-aviso.aj-aviso--sucesso")).Should().Be("Evento excluído.");
        tela.FindAll("p.aj-aviso--frase").Should().BeEmpty();
    }

    [Fact]
    public void Falha_da_consulta_mostra_o_erro_sem_dias_e_tentar_de_novo_consulta_o_mesmo_periodo()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Diana, Pedro]))
            .Responde<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(
                Resultado<IReadOnlyList<EventoResumo>>.Falha(TipoErro.Validacao, "Falha de teste."));
        Navegacao.NavigateTo("/agenda?data=2026-10-08");
        var tela = Desenhar<ComBarraSuperior<PaginaDaAgenda>>();

        tela.Markup.Should().Contain("Falha de teste.").And.Contain(">Tentar de novo</button>");
        // Falha não é semana vazia: nenhum dia aparece; a barra de período continua.
        tela.FindAll(".aj-semana, .aj-secao-dia, .aj-dia-escolhido").Should().BeEmpty();
        tela.Markup.Should().NotContain("Sem eventos");
        tela.FindAll(Desktop).Should().ContainSingle();

        Despachante.Responde<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(
            Eventos(Evento("Protocolo de documentos", Local(2026, 10, 8, 9, 30), 60, TipoEvento.Outro)));
        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.WaitForAssertion(() => tela.FindAll(".aj-semana").Should().ContainSingle());
        tela.Markup.Should().Contain("Protocolo de documentos").And.NotContain("Falha de teste.");
        Consultas.Should().HaveCount(2);
        Consultas[1].DeUtc.Should().Be(Consultas[0].DeUtc);
    }

    [Fact]
    public void Acesso_negado_na_consulta_encerra_a_tela()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Diana, Pedro]))
            .Responde<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(
                Resultado<IReadOnlyList<EventoResumo>>.Falha(TipoErro.AcessoNegado, "Acesso negado."));
        Navegacao.NavigateTo("/agenda?data=2026-10-08");

        var tela = Desenhar<ComBarraSuperior<PaginaDaAgenda>>();

        tela.Markup.Should().Contain("Seu acesso foi encerrado. Entre novamente.");
        tela.FindAll(".aj-barra-filtros, .aj-barra-superior__periodo, .aj-fab").Should().BeEmpty();
    }

    [Fact]
    public void Resposta_de_um_periodo_que_ja_ficou_para_tras_e_descartada()
    {
        var tela = Agenda("/agenda?data=2026-10-08");
        var velha = new TaskCompletionSource<Resultado<IReadOnlyList<EventoResumo>>>();
        var nova = new TaskCompletionSource<Resultado<IReadOnlyList<EventoResumo>>>();

        Despachante.RespondeDepois<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(velha.Task);
        Botao(tela, Desktop, "Próximo período").Click();   // 12/10: a consulta fica esperando
        tela.Markup.Should().Contain("Carregando…");
        Despachante.RespondeDepois<ListarEventos, Resultado<IReadOnlyList<EventoResumo>>>(nova.Task);
        Botao(tela, Desktop, "Próximo período").Click();   // 19/10

        nova.SetResult(Eventos(Evento("Reunião da semana nova", Local(2026, 10, 20, 9))));
        velha.SetResult(Eventos(Evento("Reunião da semana velha", Local(2026, 10, 13, 9))));

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Reunião da semana nova"));
        tela.Markup.Should().NotContain("Reunião da semana velha").And.NotContain("Carregando…");
        Subtitulo(tela).Should().Be("Semana de 19/10 a 25/10/2026");
        Consultas.Should().HaveCount(3);
    }
}
```

(O apelido `PaginaDaAgenda` evita a ambiguidade entre a página `Agenda` e os namespaces `…Agenda`. O `aria-label^=` da faixa tolera o `, hoje` que o rótulo ganharia se o relógio caísse naquela semana. O bUnit escreve `option[selected]` para o valor do `@bind`, como nos testes da 9E; se a versão não o escrever, confira pelo `IHtmlSelectElement.Value` e registre. Os seletores das Tarefas 6 a 8 — `.aj-semana`, `.aj-dia-escolhido`, `.aj-mes`, `.aj-faixa-dias__dia`, `.aj-faixa-dias__numero`, `--fora`, o `+N` — vêm do contrato delas; se algum diferir, ajuste o seletor e registre.)

- [ ] **Step 3: Os dois testes de layout (o de rotas deve falhar em `/agenda`)**

Em `tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs` (CRLF), troque o comentário da classe para:

```csharp
/// <summary>O que o layout decide sozinho: o destino aceso em cada barra, o avatar, o foco no título e a página sem cabeçalho.</summary>
```

e substitua `Tela_ainda_nao_migrada_fica_com_a_barra_de_logo_e_sem_involucro` e `Tela_ainda_nao_migrada_nao_abre_o_conteudo_com_bloco_do_design_system` (com os comentários deles) por:

```csharp
    // A página que não declara cabeçalho (a de não encontrada) fica com a barra de logo do layout, uma só, e com a
    // faixa de exibir o menu fora do conteúdo. Até a 9F era a agenda que provava isto; agora toda tela da equipe
    // declara o seu cabeçalho.
    [Fact]
    public async Task Pagina_sem_cabecalho_fica_com_a_barra_de_logo_e_a_faixa_de_exibir_menu()
    {
        var admin = await _app.ClienteAutenticadoAsync();

        // A página não encontrada responde 404: lê o corpo sem exigir sucesso.
        var html = WebUtility.HtmlDecode(await (await admin.GetAsync("/rota-que-nao-existe")).Content.ReadAsStringAsync());

        html.Should().Contain("Página não encontrada");
        Regex.Matches(html, "<header class=\"aj-barra-superior").Should().ContainSingle();
        html.Should().Contain("<header class=\"aj-barra-superior aj-barra-superior--padrao\">")
            .And.Contain("class=\"aj-barra-superior__logo\"")
            .And.NotContain("class=\"pagina\"");
        // O título da página fica no main; a faixa de exibir o menu, fora dele.
        var corpo = Regex.Match(html, "<main class=\"aj-app__conteudo\">.*?</main>", RegexOptions.Singleline).Value;
        corpo.Should().Contain("<h1").And.NotContain("Exibir menu");
        html.Should().MatchRegex("<div class=\"aj-exibir-menu\">\\s*<button[^>]*aria-label=\"Exibir menu\"");
    }

    // A regra interina do app.css tratava como "tela ainda não migrada" o conteúdo sem um bloco do design system no
    // topo. Ela saiu na 9F: toda tela da equipe abre o conteúdo com um bloco aj-, e nenhuma depende mais dela.
    [Theory]
    [InlineData("/")]
    [InlineData("/atividades")]
    [InlineData("/atividades/nova")]
    [InlineData("/agenda")]
    [InlineData("/agenda?visao=dia&data=2026-10-08")]
    [InlineData("/agenda?visao=mes&data=2026-10-08")]
    [InlineData("/agenda/novo")]
    [InlineData("/clientes")]
    [InlineData("/clientes/novo")]
    [InlineData("/processos")]
    [InlineData("/processos/novo")]
    [InlineData("/areas")]
    [InlineData("/usuarios")]
    [InlineData("/usuarios/novo")]
    [InlineData("/perfis")]
    [InlineData("/auditoria")]
    [InlineData("/produtores")]
    [InlineData("/documentos-comuns")]
    [InlineData("/minha-senha")]
    [InlineData("/rota-que-nao-existe")]
    public async Task Toda_tela_da_equipe_abre_o_conteudo_com_um_bloco_do_design_system(string rota)
    {
        var admin = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await (await admin.GetAsync(rota)).Content.ReadAsStringAsync());

        var primeiro = Regex.Match(html, "<main class=\"aj-app__conteudo\">(?:\\s|<!--.*?-->)*(<[a-z][^>]*>)",
            RegexOptions.Singleline).Groups[1].Value;
        primeiro.Should().MatchRegex("^<[a-z]+\\b[^>]*\\sclass=\"aj-",
            "a regra interina do app.css alcançava o conteúdo sem um filho direto cuja classe começa por aj-");
    }
```

Os dois testes antigos saem aqui, antes da página: com a agenda nova (que declara a barra e abre o conteúdo com um bloco `aj-`) eles deixariam de provar o que provam. Os dois novos provam o HTML, não o CSS: valem com o `app.css` ainda inteiro (a Tarefa 10 tira a regra interina).

- [ ] **Step 4: Rodar os testes (RED)**

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AgendaWebTests|FullyQualifiedName~AgendaInterativaTests|FullyQualifiedName~AgendaPorDiaTests|FullyQualifiedName~LayoutWebTests"`
Expected: compila (as peças das Tarefas 2, 3, 6, 7 e 8 já estão no código); falham os testes reescritos e novos de `AgendaWebTests` (a página ainda é a antiga), todos os de `AgendaInterativaTests` e, em `Toda_tela_da_equipe_abre_o_conteudo_com_um_bloco_do_design_system`, só os três casos da agenda (`/agenda`, `/agenda?visao=dia…`, `/agenda?visao=mes…`: a página antiga começa por um `<h1>` sem classe). Passam `Menu_mostra_agenda_para_todos`, `Anonimo_e_redirecionado_ao_login`, `Listagem_fica_no_registro_de_consultas`, todos os de `AgendaPorDiaTests`, `Pagina_sem_cabecalho_fica_com_a_barra_de_logo_e_a_faixa_de_exibir_menu`, os outros casos do theory e o resto de `LayoutWebTests`. É o RED. Se outra rota do theory falhar, aquela tela abre o conteúdo sem bloco `aj-` e dependia da regra interina: pare e relate (não tire a rota).

- [ ] **Step 5: A página**

Reescreva `src/AtividadesJuridicas.Web/Components/Pages/Agenda.razor` inteiro (UTF-8 sem BOM, fim de linha CRLF, como o arquivo de hoje). Saem a marcação antiga, a tabela, o `FormularioDeEvento` e a `FichaDoEvento` embutidos e a lógica deles (`_criando`, `_versaoDoFormulario`, `_abertoId`, `Abrir`, `FecharFicha`, `Novo`, `CancelarCriacao`, `CriadoAsync`, `ExcluidoAsync`, `AtualizarListaAsync`, `_mensagem`/`_classeDaMensagem`/`Avisar`, `_segunda`/`_dias`). Ficam, adaptados: `CarregarUsuariosAsync` (a lista completa com os inativos, só a Diretoria), o descarte de resposta velha (`_geracao`), o acesso encerrado em qualquer `AcessoNegado`, a falha que não mostra dias.

```razor
@page "/agenda"
@inject IDespachante Despachante
@inject FusoDoEscritorio Fuso
@inject Recados Recados
@inject NavigationManager Navegacao

<PageTitle>Agenda</PageTitle>

<CabecalhoDePagina Titulo="Agenda" Subtitulo="@Subtitulo" SubtituloMobile=""
                   Acao="@(Pronto ? "Novo evento" : null)" AcaoHref="@(Pronto ? $"/agenda/novo{Periodo.Consulta}" : null)">
    <AcaoDoMobile>
        @if (Pronto)
        {
            <button type="button" class="aj-botao aj-botao--fantasma aj-botao--icone aj-barra-superior__acao" aria-label="Filtrar agenda" aria-expanded="@Expandido" aria-controls="filtros-da-agenda" @onclick="AlternarFiltros"><Icone Nome="filtros" /></button>
        }
    </AcaoDoMobile>
    <LinhaDoMobile>
        @if (Pronto)
        {
            <div class="aj-segmentado aj-segmentado--recuado aj-segmentado--claro" role="group" aria-label="Visualização">@Visoes(Periodo.NoMobile, semanaLarga: false)</div>
            <div class="aj-barra-superior__periodo">
                <strong>@RotulosDeAgenda.MesDoMobile(Periodo.Data)</strong>
                <div class="aj-barra-superior__setas">
                    <button type="button" class="aj-botao aj-botao--fantasma aj-botao--icone" aria-label="@RotuloDoAnterior(PassoNoMobile)" @onclick="() => IrPara(Periodo.Anterior(PassoNoMobile))"><Icone Nome="voltar" /></button>
                    <button type="button" class="aj-botao aj-botao--fantasma aj-botao--icone" aria-label="@RotuloDoProximo(PassoNoMobile)" @onclick="() => IrPara(Periodo.Proximo(PassoNoMobile))"><Icone Nome="avancar" /></button>
                </div>
            </div>
            @if (Periodo.NoMobile == VisaoDaAgenda.Mes)
            {
                <GradeCompactaDoMes Periodo="Periodo" Eventos="_eventos" Hoje="_hoje" AoEscolher="EscolherNaGrade" />
            }
            else
            {
                <FaixaDeDias Periodo="Periodo" Eventos="_eventos" Hoje="_hoje" AoEscolher="EscolherNaFaixa" />
            }
        }
    </LinhaDoMobile>
</CabecalhoDePagina>

@if (!_carregado)
{
    <p class="aj-subtitulo" role="status">Carregando…</p>
}
else if (_ator is null)
{
    <Aviso Tipo="TipoDeAviso.Erro">Seu acesso foi encerrado. Entre novamente.</Aviso>
}
else
{
    @* Desktop: a navegação de período e as visões num grupo; os filtros no outro, aplicados ao "Filtrar". *@
    <div class="aj-so-desktop">
        <div class="aj-barra-filtros aj-barra-filtros--dividida">
            <div class="aj-barra-filtros__grupo">
                <div class="aj-segmentado aj-segmentado--periodo">
                    <button type="button" class="aj-segmentado__seta" aria-label="Período anterior" @onclick="() => IrPara(Periodo.Anterior(Periodo.NoDesktopETablet))"><Icone Nome="voltar" /></button>
                    <button type="button" class="aj-segmentado__hoje" @onclick="IrParaHoje">Hoje</button>
                    <button type="button" class="aj-segmentado__seta" aria-label="Próximo período" @onclick="() => IrPara(Periodo.Proximo(Periodo.NoDesktopETablet))"><Icone Nome="avancar" /></button>
                </div>
                <div class="aj-segmentado aj-segmentado--periodo" role="group" aria-label="Visualização da agenda">@Visoes(Periodo.NoDesktopETablet, semanaLarga: false)</div>
            </div>
            <form class="aj-barra-filtros__grupo" role="search" @onsubmit="FiltrarAsync" @onsubmit:preventDefault>
                <label class="aj-campo-filtro aj-campo-filtro--tipo"><span>Tipo</span><select class="aj-campo-filtro__controle" @bind="_filtro.Tipo">@OpcoesDeTipo</select></label>
                @if (_ator.EhDiretoria)
                {
                    <label class="aj-campo-filtro aj-campo-filtro--pessoa"><span>Participante</span><select class="aj-campo-filtro__controle" @bind="_filtro.ParticipanteId">@OpcoesDeParticipante</select></label>
                }
                <button type="submit" class="aj-botao aj-botao--secundario aj-botao--com-icone-pequeno"><Icone Nome="filtrar" />Filtrar</button>
            </form>
        </div>
    </div>

    @* Tablet: as setas com o nome da visão, as visões e o botão que abre o painel de filtros. *@
    <div class="aj-so-tablet">
        <div class="aj-barra-filtros aj-barra-filtros--plana">
            <div class="aj-segmentado aj-segmentado--periodo">
                <button type="button" class="aj-segmentado__seta" aria-label="@RotuloDoAnterior(Periodo.NoDesktopETablet)" @onclick="() => IrPara(Periodo.Anterior(Periodo.NoDesktopETablet))"><Icone Nome="voltar" /></button>
                <button type="button" class="aj-segmentado__hoje" @onclick="IrParaHoje">Hoje</button>
                <button type="button" class="aj-segmentado__seta" aria-label="@RotuloDoProximo(Periodo.NoDesktopETablet)" @onclick="() => IrPara(Periodo.Proximo(Periodo.NoDesktopETablet))"><Icone Nome="avancar" /></button>
            </div>
            <div class="aj-segmentado aj-segmentado--periodo" role="group" aria-label="Visualização">@Visoes(Periodo.NoDesktopETablet, semanaLarga: true)</div>
            <button type="button" class="aj-botao aj-botao--secundario aj-botao--com-icone aj-botao--ao-fim" aria-expanded="@Expandido" aria-controls="filtros-da-agenda" @onclick="AlternarFiltros"><Icone Nome="filtros" />Filtros</button>
        </div>
    </div>

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

    @if (_recado is { } recado)
    {
        <AvisoDeEventoSalvo Recado="recado" />
    }
    @if (_erroDosUsuarios is not null)
    {
        <Aviso Tipo="TipoDeAviso.Erro">@_erroDosUsuarios</Aviso>
    }

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
    else if (Periodo.Visao is null)
    {
        @* Sem visão no endereço, cada largura mostra o seu padrão sobre os mesmos eventos: a Semana no desktop e no
           tablet, o Dia no mobile. *@
        <div class="aj-desktop-tablet"><SemanaDaAgenda Periodo="Periodo" Eventos="_eventos" Hoje="_hoje" /></div>
        <div class="aj-so-mobile"><DiaDaAgenda Periodo="Periodo" Eventos="_eventos" Hoje="_hoje" /></div>
    }
    else if (Periodo.Visao == VisaoDaAgenda.Semana)
    {
        <SemanaDaAgenda Periodo="Periodo" Eventos="_eventos" Hoje="_hoje" />
    }
    else if (Periodo.Visao == VisaoDaAgenda.Dia)
    {
        <DiaDaAgenda Periodo="Periodo" Eventos="_eventos" Hoje="_hoje" />
    }
    else
    {
        <MesDaAgenda Periodo="Periodo" Eventos="_eventos" Hoje="_hoje" AoAbrirDia="AbrirDia" />
    }
}

@code {
    [SupplyParameterFromQuery(Name = "visao")] public string? VisaoDoEndereco { get; set; }

    [SupplyParameterFromQuery(Name = "data")] public string? DataDoEndereco { get; set; }

    private bool _carregado;
    private bool _carregando;
    private bool _filtrosAbertos;
    private bool _focarPainel;
    private int _geracao;
    private UsuarioAutenticado? _ator;
    private PeriodoDaAgenda? _periodo;
    private DateOnly _hoje;
    private Pedido? _pedido;
    private IReadOnlyList<EventoResumo> _eventos = [];
    private string? _erro;
    private string? _erroDosUsuarios;
    private Recado? _recado;
    private readonly Filtro _filtro = new();
    private IReadOnlyList<OpcaoDeUsuario> _usuarios = [];
    private ElementReference _tipoDoPainel;

    protected override async Task OnInitializedAsync()
    {
        _ator = await Despachante.EnviarAsync(new ObterMeuAcesso());
        if (_ator is { EhDiretoria: true }) await CarregarUsuariosAsync();
        if (_ator is not null) _recado = Recados.Retirar();
    }

    /// <summary>
    /// O período mora no endereço: a cada troca de ?visao ou ?data (setas, "Hoje", visões, faixa, grade do mês) a
    /// página relê o período e só consulta se o intervalo ou o filtro mudou. O redesenho que vem do layout (o menu,
    /// por exemplo) também passa por aqui e não consulta de novo.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        _hoje = Hoje();
        var periodo = PeriodoDaAgenda.Ler(VisaoDoEndereco, DataDoEndereco, _hoje);
        // O recado de outra tela vale para o período em que se chegou: trocar de período o tira da tela.
        if (_periodo is not null && periodo.Endereco != _periodo.Endereco) _recado = null;
        _periodo = periodo;
        if (_ator is not null) await ConsultarAsync();
        _carregado = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_focarPainel) return;

        // O painel de filtros recebe o foco no primeiro campo quando abre.
        _focarPainel = false;
        try
        {
            await _tipoDoPainel.FocusAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSException or JSDisconnectedException)
        {
            // Sem foco (circuito caindo, painel já fechado) o painel continua funcional.
        }
    }

    /// <summary>O dia de hoje no fuso do escritório.</summary>
    private DateOnly Hoje() => DateOnly.FromDateTime(Fuso.ParaLocal(DateTime.UtcNow));

    // Lido do endereço no OnParametersSetAsync, antes de qualquer desenho que o use (todos dependem de Pronto).
    private PeriodoDaAgenda Periodo =>
        _periodo ?? throw new InvalidOperationException("O período é lido do endereço antes do primeiro desenho que o usa.");

    private bool Pronto => _carregado && _ator is not null;

    /// <summary>
    /// O filtro de participante é da Diretoria, que precisa achar também os eventos de quem foi inativado: por isso vem
    /// da lista completa de usuários (a mesma da auditoria), com os inativos marcados.
    /// </summary>
    private async Task CarregarUsuariosAsync()
    {
        var resultado = await Despachante.EnviarAsync(new ListarUsuarios());
        if (resultado.Erro == TipoErro.AcessoNegado)
        {
            EncerrarAcesso();
            return;
        }

        if (!resultado.Sucesso || resultado.Valor is null)
        {
            _erroDosUsuarios = resultado.Mensagem ?? "Não foi possível consultar os usuários.";
            return;
        }

        _usuarios = [.. resultado.Valor
            .Where(u => u.TipoPerfil != TipoPerfil.Cliente)
            .OrderBy(u => u.Nome, StringComparer.CurrentCultureIgnoreCase)
            .Select(u => new OpcaoDeUsuario(u.Id, u.Ativo ? u.Nome : $"{u.Nome} (inativo)"))];
    }

    // Uma consulta por intervalo e filtro: o Dia, a Semana e o Mês do mesmo período usam os mesmos eventos.
    private async Task ConsultarAsync()
    {
        var pedido = new Pedido(Periodo.InicioDaConsulta, Periodo.FimDaConsulta, _filtro.Tipo,
            _ator?.EhDiretoria == true ? _filtro.ParticipanteId : null);
        if (pedido == _pedido) return;

        _pedido = pedido;
        await CarregarAsync(pedido);
    }

    /// <summary>Consulta o intervalo pedido. Uma resposta que chega depois de outra consulta mais nova é descartada.</summary>
    private async Task CarregarAsync(Pedido pedido)
    {
        var geracao = ++_geracao;
        _carregando = true;
        _erro = null;
        try
        {
            var resultado = await Despachante.EnviarAsync(new ListarEventos(
                Fuso.InicioDoDiaEmUtc(pedido.Inicio), Fuso.InicioDoDiaEmUtc(pedido.Fim), pedido.ParticipanteId, pedido.Tipo));
            if (geracao != _geracao) return;

            if (resultado.Erro == TipoErro.AcessoNegado)
            {
                EncerrarAcesso();
                return;
            }

            if (resultado.Valor is not { } eventos)
            {
                // Dados antigos não ficam na tela depois de uma falha.
                _eventos = [];
                _erro = resultado.Mensagem ?? "Não foi possível consultar a agenda.";
                return;
            }

            _eventos = eventos;
        }
        finally
        {
            if (geracao == _geracao) _carregando = false;
        }
    }

    // "Tentar de novo" repete o último pedido (o mesmo intervalo e o mesmo filtro), uma vez por clique.
    private Task TentarDeNovoAsync() => _carregando || _pedido is null ? Task.CompletedTask : CarregarAsync(_pedido);

    private Task FiltrarAsync()
    {
        _recado = null;
        return ConsultarAsync();
    }

    private async Task FiltrarDoPainelAsync()
    {
        _filtrosAbertos = false;
        await FiltrarAsync();
    }

    // Volta os campos a "Todos"; só vale ao filtrar.
    private void LimparFiltros()
    {
        _filtro.Tipo = null;
        _filtro.ParticipanteId = null;
    }

    private void AlternarFiltros()
    {
        _filtrosAbertos = !_filtrosAbertos;
        _focarPainel = _filtrosAbertos;
    }

    private void IrPara(PeriodoDaAgenda periodo) => Navegacao.NavigateTo(periodo.Endereco);

    private void IrParaHoje() => IrPara(Periodo.ParaHoje(Hoje()));

    private void TrocarVisao(VisaoDaAgenda visao) => IrPara(Periodo.ComVisao(visao));

    // Faixa do mobile: tocar num dia troca a data e mantém a visão; na Semana, leva ao Dia daquele dia.
    private void EscolherNaFaixa(DateOnly dia) =>
        IrPara(Periodo.Visao == VisaoDaAgenda.Semana ? Periodo.ComVisao(VisaoDaAgenda.Dia).ComData(dia) : Periodo.ComData(dia));

    // Grade compacta do mês: tocar num dia troca a data e fica no Mês (a lista embaixo é desse dia).
    private void EscolherNaGrade(DateOnly dia) => IrPara(Periodo.ComData(dia));

    // O "+N" da célula do mês leva ao Dia daquela data.
    private void AbrirDia(DateOnly dia) => IrPara(Periodo.ComVisao(VisaoDaAgenda.Dia).ComData(dia));

    /// <summary>Uma resposta de acesso negado encerra a tela: nada do que estava à vista continua.</summary>
    private void EncerrarAcesso()
    {
        _ator = null;
        _eventos = [];
        _usuarios = [];
        _erro = null;
        _erroDosUsuarios = null;
        _recado = null;
        _filtrosAbertos = false;
    }

    // As setas do mobile andam a faixa de semana em semana no Dia e na Semana, e de mês em mês no Mês.
    private VisaoDaAgenda PassoNoMobile => Periodo.NoMobile == VisaoDaAgenda.Mes ? VisaoDaAgenda.Mes : VisaoDaAgenda.Semana;

    private static string RotuloDoAnterior(VisaoDaAgenda passo) => passo switch
    {
        VisaoDaAgenda.Dia => "Dia anterior",
        VisaoDaAgenda.Semana => "Semana anterior",
        _ => "Mês anterior",
    };

    private static string RotuloDoProximo(VisaoDaAgenda passo) => passo switch
    {
        VisaoDaAgenda.Dia => "Próximo dia",
        VisaoDaAgenda.Semana => "Próxima semana",
        _ => "Próximo mês",
    };

    // O subtítulo do desktop e do tablet diz o período da visão daquelas larguras; o mobile não tem subtítulo.
    private string? Subtitulo => !Pronto ? null : Periodo.NoDesktopETablet switch
    {
        VisaoDaAgenda.Dia => RotulosDeAgenda.SubtituloDoDia(Periodo.Data),
        VisaoDaAgenda.Mes => RotulosDeAgenda.SubtituloDoMes(Periodo.Data),
        _ => RotulosDeAgenda.SubtituloDaSemana(Periodo.Semana[0]),
    };

    private string Expandido => _filtrosAbertos ? "true" : "false";

    private static string Pressionada(VisaoDaAgenda escolhida, VisaoDaAgenda visao) => escolhida == visao ? "true" : "false";

    // As três visões do segmentado; no tablet o "Semana" é mais largo (o quadro).
    private RenderFragment Visoes(VisaoDaAgenda escolhida, bool semanaLarga) => @<text><button type="button" class="aj-segmentado__opcao" aria-pressed="@Pressionada(escolhida, VisaoDaAgenda.Dia)" @onclick="() => TrocarVisao(VisaoDaAgenda.Dia)">Dia</button><button type="button" class="@(semanaLarga ? "aj-segmentado__opcao aj-segmentado__opcao--larga" : "aj-segmentado__opcao")" aria-pressed="@Pressionada(escolhida, VisaoDaAgenda.Semana)" @onclick="() => TrocarVisao(VisaoDaAgenda.Semana)">Semana</button><button type="button" class="aj-segmentado__opcao" aria-pressed="@Pressionada(escolhida, VisaoDaAgenda.Mes)" @onclick="() => TrocarVisao(VisaoDaAgenda.Mes)">Mês</button></text>;

    private RenderFragment OpcoesDeTipo => @<text><option value="">Todos</option>@foreach (var tipo in Enum.GetValues<TipoEvento>())
        {<option value="@tipo">@RotulosDeAgenda.Tipo(tipo)</option>}</text>;

    private RenderFragment OpcoesDeParticipante => @<text><option value="">Todos</option>@foreach (var usuario in _usuarios)
        {<option value="@usuario.Id">@usuario.Rotulo</option>}</text>;

    /// <summary>O que a tela pediu ao servidor: o intervalo da visão e o filtro aplicado.</summary>
    private sealed record Pedido(DateOnly Inicio, DateOnly Fim, TipoEvento? Tipo, Guid? ParticipanteId);

    private sealed record OpcaoDeUsuario(Guid Id, string Rotulo);

    /// <summary>
    /// O estado único dos filtros: a barra do desktop e o painel o mostram e o mudam; ele sobrevive à troca de período
    /// (não vai ao endereço). A consulta lê o que está nos campos.
    /// </summary>
    private sealed class Filtro
    {
        public TipoEvento? Tipo { get; set; }
        public Guid? ParticipanteId { get; set; }
    }
}
```

Notas para quem implementa:

- Ordem do ciclo de vida: o `OnInitializedAsync` termina (ator, usuários e recado) antes do `OnParametersSetAsync`, cuja parte síncrona lê o período antes do desenho seguinte; `_carregado` só fica verdadeiro depois da primeira consulta. Tudo que lê `Periodo` está atrás de `Pronto` (cabeçalho e barra do mobile) ou do `else` de `_carregado`/`_ator` (corpo). Se o compilador ou o bUnit mostrarem um desenho com `_periodo` nulo, troque a exceção da propriedade por um `if (_periodo is { } periodo)` nos dois pontos e registre.
- A barra do mobile passa pela `AcaoDoMobile` e pela `LinhaDoMobile` do `CabecalhoDePagina` (Tarefa 3); a página não escreve a barra superior. O `SubtituloMobile=""` tira o subtítulo da barra do mobile (regra da 9B).
- Os `@bind` dentro de `select` com as opções num fragmento (`OpcoesDeTipo`, `OpcoesDeParticipante`) marcam a opção escolhida como no `Atividades.razor`; se o compilador recusar o fragmento dentro do `select`, escreva as opções na marcação (iguais na barra e no painel) e registre.
- O `@ref="_tipoDoPainel"` fica só no campo do painel (o da barra não tem referência): uma referência num fragmento usado duas vezes ficaria com o último desenhado.
- Classes por expressão (ponto cego da trava, conferido na revisão): só `aj-segmentado__opcao` e `aj-segmentado__opcao--larga`.
- Os links `/agenda/novo…` e `/agenda/{id}…` atendem às Tarefas 4 e 5 (já no código).

- [ ] **Step 6: Build e testes (GREEN)**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AgendaWebTests|FullyQualifiedName~AgendaInterativaTests|FullyQualifiedName~AgendaPorDiaTests|FullyQualifiedName~SemanaDaAgendaTests|FullyQualifiedName~DiaDaAgendaTests|FullyQualifiedName~MesDaAgendaTests|FullyQualifiedName~AvisoDeEventoSalvoTests|FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~InicioWebTests"`
Expected: todos passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~LayoutWebTests"`
Expected: todos passando, inclusive os três casos da agenda em `Toda_tela_da_equipe_abre_o_conteudo_com_um_bloco_do_design_system`.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~WebSoPeloDespachante"`
Expected: passando.

- [ ] **Step 7: Commit**

Antes, `git branch --show-current` (deve ser `worktree-feat+fase-9f-agenda`).

```
git add src/AtividadesJuridicas.Web/Components/Pages/Agenda.razor tests/AtividadesJuridicas.IntegrationTests/AgendaWebTests.cs tests/AtividadesJuridicas.IntegrationTests/AgendaInterativaTests.cs tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs
git commit -F <arquivo com a mensagem, UTF-8 sem BOM>
```

Mensagem:

```
feat(web): agenda no design system, com o período no endereço, as visões Dia, Semana e Mês e os filtros de cada largura

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

---

### Task 10: Limpeza — componentes antigos, `app.css` e trava

**Files:**
- Delete: `src/AtividadesJuridicas.Web/Components/Agenda/FichaDoEvento.razor`, `FormularioDeEvento.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Agenda/RotulosDeAgenda.cs` (sai o `EventoGravado`, se ninguém mais o usar), `src/AtividadesJuridicas.Web/wwwroot/app.css`, `src/AtividadesJuridicas.Web/Components/App.razor` (só o comentário sobre as folhas de estilo), `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`

**Regras:**
- Saem do `app.css` as regras que só a agenda usava (`inventario-codigo.md`, seção 6: `button:not([class*="aj-"])` e derivados, `button.secundario…`, `.acoes` e derivados, `:where(input, select, textarea):not([class*="aj-"])` e derivados, `input[type="file"]:not([class*="aj-"])`, `.formulario`, `.cartao, form.formulario:not(.filtros)`, `.ficha .formulario`, `.grade`, `.filtros`, `.ficha .filtros`, `table:not([class*="aj-"])` e derivados com a consulta de mídia, `td.horario`, `.ficha` e derivados, `.texto-livre`, `.dia > h2`, `.erro`, `.ok`, `.aviso`, `p.erro`, `p.ok`, `p.aviso`, `.validation-message`, `strong.erro`), a regra interina `.aj-app__conteudo:not(:has(> [class^="aj-"]))` e as duas regras de etiqueta que dependem dela. Antes de apagar cada uma, procure o seletor em todo `src` (inclusive por expressão, `class="@…"`).
- Ficam (9G): `box-sizing`, `body`, foco, links, `h1`–`h3`, `p`, `label`, `button:disabled`, `.aj-campo-icone input:focus…`, `input.invalid…`, `input[type="checkbox"]…`, `label:has(> input…)`, `fieldset`, `legend`, `td code`, a faixa de erro do Blazor.
- `TelasNoDesignSystemTests`: as telas da agenda (`Pages/Agenda.razor`, `Pages/EventoFicha.razor`, `Pages/EventoFormulario.razor` e os componentes de `Components/Agenda/`) em `Migradas`, e a trava `Toda_tela_da_agenda_esta_na_trava` (todo `Components/Agenda/*.razor` e `Components/Pages/Agenda*.razor`/`Evento*.razor`).
- Os dois testes de `LayoutWebTests` sobre "tela ainda não migrada" já foram reescritos na Tarefa 9 (`/rota-que-nao-existe` e o theory de que toda tela da equipe abre o conteúdo com um bloco do design system); depois da limpeza, eles continuam verdes.

**Testes obrigatórios:** a trava com as telas novas (falha antes de pô-las na lista); nenhum seletor apagado ainda em uso (procura registrada no relatório); `LayoutWebTests` verde sem a regra interina.

- [ ] **Step 1: Teste da trava da agenda (deve falhar)**

Em `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (fim de linha CRLF), logo depois do `[Fact]` `Toda_tela_de_atividade_esta_na_trava`:

```csharp
    // Toda tela da agenda fica na trava: os componentes de Components/Agenda, a página da agenda e as do evento.
    // Um componente antigo que sobrou (marcação antiga, fora da lista) faz este teste falhar.
    [Fact]
    public void Toda_tela_da_agenda_esta_na_trava()
    {
        var web = Path.Combine(GravacoesPorForaTests.RaizDoRepositorio(), "src", "AtividadesJuridicas.Web");
        var paginas = Path.Combine(web, "Components", "Pages");
        var telas = Directory.GetFiles(Path.Combine(web, "Components", "Agenda"), "*.razor")
            .Concat(Directory.GetFiles(paginas, "Agenda*.razor"))
            .Concat(Directory.GetFiles(paginas, "Evento*.razor"))
            .Select(caminho => Path.GetRelativePath(web, caminho).Replace('\\', '/'))
            .ToList();

        telas.Should().Contain("Components/Pages/Agenda.razor").And.Contain("Components/Pages/EventoFicha.razor")
            .And.Contain("Components/Pages/EventoFormulario.razor").And.Contain("Components/Agenda/SemanaDaAgenda.razor");
        telas.Should().BeSubsetOf(Migradas, "tela da agenda fora da trava pode voltar à marcação antiga");
    }
```

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests.Toda_tela_da_agenda_esta_na_trava"`
Expected: falha, com estes doze arquivos fora de `Migradas`: `Components/Agenda/AvisoDeEventoSalvo.razor`, `CartaoDeEvento.razor`, `DiaDaAgenda.razor`, `FaixaDeDias.razor`, `FichaDoEvento.razor`, `FormularioDeEvento.razor`, `GradeCompactaDoMes.razor`, `MesDaAgenda.razor`, `SemanaDaAgenda.razor` e `Components/Pages/Agenda.razor`, `EventoFicha.razor`, `EventoFormulario.razor`. Se aparecer outro arquivo, ou faltar um dos novos, uma tarefa anterior criou ou deixou de criar algo fora do contrato: pare e relate.

- [ ] **Step 2: As telas novas na trava**

Em `Migradas`, acrescente, na ordem alfabética da lista:

- antes de `"Components/Atividades/CabecalhoDaAtividade.razor",`:

```csharp
        "Components/Agenda/AvisoDeEventoSalvo.razor",
        "Components/Agenda/CartaoDeEvento.razor",
        "Components/Agenda/DiaDaAgenda.razor",
        "Components/Agenda/FaixaDeDias.razor",
        "Components/Agenda/GradeCompactaDoMes.razor",
        "Components/Agenda/MesDaAgenda.razor",
        "Components/Agenda/SemanaDaAgenda.razor",
```

- antes de `"Components/Pages/Andamento.razor",`: `"Components/Pages/Agenda.razor",`
- depois de `"Components/Pages/Erro.razor",`: `"Components/Pages/EventoFicha.razor",` e `"Components/Pages/EventoFormulario.razor",`

`FichaDoEvento.razor` e `FormularioDeEvento.razor` não entram: saem no passo 4.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Expected: o theory `Tela_migrada_nao_tem_estilo_embutido_nem_classe_fora_do_design_system` passa para os dez arquivos novos (nenhum `style`, `<style>`, `.razor.css`, classe fora de `aj-` nem `button`/`table`/`input`/`select`/`textarea` sem classe); `Toda_tela_da_agenda_esta_na_trava` continua falhando, agora só com `Components/Agenda/FichaDoEvento.razor` e `Components/Agenda/FormularioDeEvento.razor`. Se o theory reprovar um arquivo novo, a tarefa que o criou deixou marcação fora do design system: pare e relate (não o tire da lista).

Confira também, na revisão, as classes montadas por expressão (ponto cego da trava) nos arquivos novos: com a ferramenta Grep, `class="@` em `src/AtividadesJuridicas.Web/Components/Agenda` e em `Components/Pages/Agenda.razor`, `EventoFicha.razor`, `EventoFormulario.razor`, e leia cada expressão: todo valor que ela pode produzir começa por `aj-`. Registre a lista no relatório.

- [ ] **Step 3: Conferir que ninguém usa os componentes antigos**

Com a ferramenta Grep, procure `FichaDoEvento|FormularioDeEvento|EventoGravado` em `src/` e `tests/` (`*.{razor,cs}`).

Expected: só as definições — os próprios `Components/Agenda/FichaDoEvento.razor` e `Components/Agenda/FormularioDeEvento.razor` (que se citam e citam `EventoGravado`) e o `public sealed record EventoGravado(…)` de `Components/Agenda/RotulosDeAgenda.cs`. A página da agenda deixou de usá-los na Tarefa 9 e o formulário novo (Tarefa 5) devolve o resultado pelo `Recados`. Se outro arquivo os citar (uma tela ou um teste), pare e relate.

- [ ] **Step 4: Apagar os componentes antigos e o `EventoGravado`**

```
git rm src/AtividadesJuridicas.Web/Components/Agenda/FichaDoEvento.razor src/AtividadesJuridicas.Web/Components/Agenda/FormularioDeEvento.razor
```

Em `src/AtividadesJuridicas.Web/Components/Agenda/RotulosDeAgenda.cs` (CRLF), apague o registro e o comentário dele, e a linha em branco que os separa da classe — estas cinco linhas:

```csharp
/// <summary>
/// O que o formulário devolve depois de gravar: o id, os avisos de sobreposição e o início gravado (em UTC), para a
/// página mostrar a semana em que o evento caiu.
/// </summary>
public sealed record EventoGravado(Guid Id, IReadOnlyList<SobreposicaoDeAgenda> Sobreposicoes, DateTime InicioEm);
```

(e a linha em branco depois delas). O `using AtividadesJuridicas.Application.Agenda;` fica se `RotulosDeAgenda` ainda usar `SobreposicaoDeAgenda` ou `EventoResumo` (usa: `Sobreposicao`, `TocaODia`); o build acusa `using` sobrando como aviso, e aviso é erro.

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Expected: todos passando, inclusive `Toda_tela_da_agenda_esta_na_trava`.

- [ ] **Step 5: Conferir cada regra do `app.css` antes de apagar**

Regra só sai se nenhum `.razor`, `.cs` ou `.js` de `src/AtividadesJuridicas.Web` usa o que ela alcança, nem por marcação fixa nem por expressão. Para cada classe abaixo, duas buscas com a ferramenta Grep em `src/AtividadesJuridicas.Web`, filtro `*.{razor,cs,js}`:

1. a classe em atributo fixo: `class="([^"]* )?<classe>( [^"]*)?"` (não acha `aj-<classe>` nem `aj-…__<classe>`, que têm o prefixo colado);
2. a classe como texto entre aspas, que é como ela chega a um atributo por expressão: `"<classe>"`.

| Classe | Busca 1 | Busca 2 (esperado) | Regras do `app.css` |
|---|---|---|---|
| `secundario` | nada | nada | `button.secundario:not([class*="aj-"])` e o `:hover` (30–31) |
| `acoes` | nada | nada | `.acoes` (33), `td .acoes` (34), `td.acoes` (36), `td.acoes > * + *` (37) e o comentário da 35 |
| `formulario` | nada | nada | `.formulario, form.formulario` (52), a parte `form.formulario:not(.filtros)` da 53, `.ficha .formulario` (54), a parte `form.formulario.filtros` da 56 |
| `cartao` | nada (o `aj-cartao` tem o prefixo) | nada | a parte `.cartao` da 53 |
| `ficha` | nada | nada | `.ficha .formulario` (54), `.ficha .filtros` (57), `.ficha` e derivados (73–79) |
| `grade` | nada (o `aj-grade` tem o prefixo) | nada | `.grade` (55) |
| `filtros` | nada | só o nome do ícone: `["filtros"]` em `Components/Layout/Icone.razor` e `Nome="filtros"` em `Pages/Atividades.razor`, `Pages/Auditoria.razor` e `Pages/Agenda.razor` | `.filtros, form.formulario.filtros` (56), `.ficha .filtros` (57) |
| `horario` | nada (o `aj-evento__horario` tem o prefixo) | nada | `td.horario` (66) |
| `texto-livre` | nada (o `aj-texto-livre` tem o prefixo) | nada | `.texto-livre` (80) |
| `dia` | nada (`aj-dia`, `aj-semana__dia`… têm o prefixo) | só o valor de `visao` em `Components/Agenda/PeriodoDaAgenda.cs` (o `"dia"` do endereço) | `.dia > h2` (81) |
| `erro` | nada | só o nome do ícone: `_ => "erro"` em `Components/Layout/Aviso.razor` e `["erro"]` em `Components/Layout/Icone.razor` | `.erro, .validation-message` (84), `p.erro, p.ok, p.aviso` (87), `p.erro` (88), `strong.erro` (92) |
| `ok` | nada | nada | `.ok` (85), `p.ok` (89) |
| `aviso` | nada | só o nome do parâmetro de endereço: `[SupplyParameterFromQuery(Name = "aviso")]` em `Pages/Conta/Entrar.razor` | `.aviso` (86), `p.aviso` (90) |
| `validation-message` | nada | nada; e `<ValidationMessage` / `<ValidationSummary` (que o Blazor veste com essa classe) em nenhum `.razor` | `.validation-message` (84 e 91) |

Os elementos sem classe (as regras com `:not([class*="aj-"])`):

| Regra | Como conferir | Esperado |
|---|---|---|
| `button:not([class*="aj-"])` e `:hover` (28–29) | as telas de `Migradas` já passam pela trava (nenhum `<button>` sem classe); fora dela, procure `<(button|table|input|select|textarea)\b` nos seis `.razor` que não são telas: `Components/App.razor`, `Routes.razor`, `_Imports.razor`, `Layout/FocoNoTitulo.razor`, `Layout/LayoutVazio.razor`, `Layout/ReconnectModal.razor`; e `OpenElement\(\d+, "(button\|table\|input\|select\|textarea)"` nos `.cs` | só o `<button … class="dismiss aj-botao …">` do `App.razor` e os dois `<button … class="aj-botao aj-botao--primario …">` do `ReconnectModal.razor`, todos com `aj-`; nenhum `OpenElement` desses elementos |
| `:where(input, select, textarea):not([class*="aj-"])` e derivados (41–44), `input[type="file"]:not([class*="aj-"])` (48) | além do acima, `<Input[A-Z]\w*` em todo `.razor` (os componentes de formulário do Blazor desenham `input`, `select` e `textarea` e a trava não os vê) | todo `InputText`, `InputTextArea`, `InputDate`, `InputSelect`, `InputFile`… com `class="aj-campo…"` (os de hoje e os do `EventoFormulario`); o `InputSelect` sem classe do `Agenda.razor` antigo saiu na Tarefa 9 |
| `table:not([class*="aj-"])` e derivados (60–64), a consulta de mídia (67–70) | o mesmo da primeira linha (`<table` sem classe) | nada |

E as três regras da tela não migrada:

| Regra | Por que sai |
|---|---|
| `.aj-app__conteudo:not(:has(> [class^="aj-"])) { display: block; }` (17), com o título e o comentário (14–16) | o theory `Toda_tela_da_equipe_abre_o_conteudo_com_um_bloco_do_design_system` de `LayoutWebTests` (Tarefa 9) prova que toda tela da equipe (e a não encontrada) abre o conteúdo com um bloco `aj-`; nenhuma tela chega mais a ela |
| `.aj-app__conteudo:not(:has(> [class^="aj-"])) .aj-etiqueta` e `… .aj-etiqueta + .aj-etiqueta` (95–96), com o comentário (93–94) | dependem da regra interina: sem tela não migrada, não alcançam nada |

Registre no relatório o resultado das buscas (o que cada uma achou). O que diferir da coluna "esperado" e for uso como classe: pare e relate. Nome de ícone, nome de parâmetro e valor de endereço não são classe.

- [ ] **Step 6: Apagar as regras sem uso**

Reescreva `src/AtividadesJuridicas.Web/wwwroot/app.css` (UTF-8 sem BOM, fim de linha CRLF, como hoje) exatamente assim. Ficam, sem mudar um caractere, as regras que a seção "Regras" desta tarefa manda ficar (9G); muda o comentário do topo (o arquivo já não veste tela antiga) e os títulos de seção; nenhuma regra nova.

```css
/* Base da aplicação sobre o design system (css/tokens.css e css/componentes.css): caixa, corpo, links e foco.
   Todas as telas usam as classes aj-. As regras de elemento abaixo da base vêm da marcação antiga e ainda alcançam
   os componentes do design system (título, parágrafo, rótulo, botão desabilitado, caixa de marcar, conjunto); saem
   na fase 9G, com a conferência das telas. */

*, *::before, *::after { box-sizing: border-box; }
html, body { height: 100%; }
body { margin: 0; background: var(--fundo); color: var(--tinta); font: 400 15px/1.5 var(--font-sans); }
:where(a) { color: var(--verde); }
:where(a:hover) { color: var(--tinta); }
:focus-visible { outline: 2px solid var(--verde); outline-offset: 2px; }
/* O título recebe o foco a cada navegação (FocoNoTitulo) só para ser anunciado; não é um controle. */
h1:focus { outline: none; }

/* ---------- Regras de elemento que ainda alcançam o design system (até a fase 9G) ---------- */
h1 { margin: 0 0 var(--espaco-14); font: 500 var(--aj-titulo)/1.1 var(--font-serif); letter-spacing: -.01em; }
h2 { margin: var(--espaco-28) 0 var(--espaco-10); font: 700 18px/1.3 var(--font-sans); }
h3 { margin: var(--espaco-18) 0 var(--espaco-8); font: 700 16px/1.4 var(--font-sans); }
p { margin: var(--espaco-8) 0; }
button:disabled { opacity: .55; cursor: not-allowed; }

/* ---------- Campos ---------- */
label { display: grid; gap: var(--espaco-8); font: 600 var(--aj-fonte-rotulo)/1.4 var(--font-sans); color: var(--tinta-rotulo); }
.aj-campo-icone input:focus, .aj-campo-filtro :focus { box-shadow: none; }
input.invalid, select.invalid, textarea.invalid { border: 1.5px solid var(--erro); background: var(--erro-campo); box-shadow: var(--sombra-foco-erro); }
input[type="checkbox"], input[type="radio"] { width: 18px; min-height: 0; height: 18px; margin: 0; padding: 0; accent-color: var(--verde); }
label:has(> input[type="checkbox"]), label:has(> input[type="radio"]) { display: flex; align-items: center; gap: var(--espaco-10); min-height: var(--aj-campo); font-weight: 500; color: var(--tinta); }
fieldset { display: grid; grid-template-columns: repeat(auto-fit, minmax(min(170px, 100%), 1fr)); gap: var(--espaco-8); margin: 0; padding: var(--espaco-12) var(--espaco-14); border: 1px solid var(--borda-opcao); border-radius: var(--raio-12); }
legend { padding: 0 var(--espaco-6); font: 600 var(--aj-fonte-rotulo)/1.4 var(--font-sans); color: var(--tinta-rotulo); }

/* ---------- Tabelas ---------- */
td code { font-size: .8rem; word-break: break-all; }
```

Confira depois com a ferramenta Grep no `app.css`: nenhuma ocorrência de `:not\(\[class\*="aj-"\]\)`, `:not\(:has\(`, `\.acoes`, `\.ficha`, `\.formulario`, `\.filtros`, `\.grade`, `\.cartao`, `\.erro`, `\.ok\b`, `\.aviso`, `\.dia`, `\.horario`, `\.texto-livre`, `validation-message`, `\.secundario`; e as vinte regras que ficam (o `label:has(> input…)` entre elas), cada uma uma vez (de `*, *::before` a `td code`). A faixa de erro do Blazor não está neste arquivo (é o `aj-faixa-erro` do `componentes.css`): nada a fazer com ela.

Em `src/AtividadesJuridicas.Web/Components/App.razor` (CRLF), o comentário sobre as folhas de estilo deixa de falar em tela não migrada. Troque

```razor
    @* Design system: tokens, depois componentes; app.css traz a base da aplicação e veste as telas ainda não migradas. *@
```

por

```razor
    @* Design system: tokens, depois componentes; app.css traz a base da aplicação (e as regras de elemento que saem na 9G). *@
```

- [ ] **Step 7: Build e testes**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build`
Expected: todos passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~WebTests|FullyQualifiedName~Interativ|FullyQualifiedName~ComponentesDoDesignSystemTests|FullyQualifiedName~PaginasProtegidasTests|FullyQualifiedName~AgendaPorDiaTests|FullyQualifiedName~AgendaPuraTests|FullyQualifiedName~SemanaDaAgendaTests|FullyQualifiedName~DiaDaAgendaTests|FullyQualifiedName~MesDaAgendaTests|FullyQualifiedName~AvisoDeEventoSalvoTests|FullyQualifiedName~TelasPurasTests"`
Expected: todos passando (os testes de página por HTTP, inclusive `LayoutWebTests` e `AgendaWebTests`, os de clique com o bUnit, os dos componentes do design system, os das páginas protegidas e os puros e de componente da agenda).

- [ ] **Step 8: Commit**

Confira o branch (`git branch --show-current` deve dizer `worktree-feat+fase-9f-agenda`) e:

```
git add -A src tests
git commit -F <arquivo com a mensagem, UTF-8 sem BOM>
```

Mensagem:

```
refactor(web): apaga a ficha e o formulário antigos do evento e a camada do app.css que só a agenda usava, e põe as telas da agenda na trava

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
```

---

### Task 11: Suíte inteira, conferência no navegador e emenda da spec

Feita pela sessão que coordena (com um subagente para a conferência, como na 9E).

- [ ] **Step 1: Suíte inteira** em lotes, com o `dotnet build -m:1` feito antes e nenhum `docker` nem aplicação publicada rodando ao mesmo tempo. Expected: unidade e integração sem falha, com mais testes que a linha de base (896 e 958).
- [ ] **Step 2: Ambiente descartável e ferramenta** — a ferramenta da 9E copiada para `conferencia-de-telas\9f\`, banco `atividades-verif-9f`, aplicação publicada, quadros servidos, Chrome sem janela e condutor (o mesmo roteiro da 9E, Tarefa 15, passo 2).
- [ ] **Step 3: Dados do quadro** — as pessoas pelas telas (Marina Couto da Diretoria; Beatriz Lins, Camila Prado, Otávio Brandão e Rafael Teles da Produção); os clientes e processos por SQL; os oito eventos da semana do quadro pelas telas da fase (o formulário novo exercitando cada tipo e o conflito de Rafael Teles), com as datas deslocadas para o "hoje" do relógio cair na quinta-feira do quadro; os casos de borda (simultâneos na régua, evento curto, evento que atravessa a meia-noite, mês de seis semanas, cinco eventos num dia, textos longos) depois de medir.
- [ ] **Step 4: Medir os quadros** contra o sistema nas três larguras: `AgendaA` nos três períodos e com o menu escondido, `AgendaTablet`, `AgendaMobile` (Dia), `NovoEvento*` (preenchido como o quadro, com a hora vazia depois de um envio), `Evento*`. O que divergir: CSS no design system (a Tarefa 1 de novo), marcação na tela com o teste ajustado antes. Medir o que o `app.css` muda dentro de cada tela (`V.vazamento`).
- [ ] **Step 5: Review Focus e casos de borda no navegador** (a lista da seção "Review Focus").
- [ ] **Step 6: Teclado e estados** (lista fixa da 9D, com o que a 9F acrescenta: a faixa e a grade compacta do mobile com toque e Tab; o segmentado de visões; o painel de filtros; as setas; o foco no título depois da troca de período).
- [ ] **Step 7: Derrubar o ambiente** antes de qualquer rodada da suíte.
- [ ] **Step 8: Emenda da spec**, como a 9E emendou: cabeçalho (fases 9A a 9F; as versões do canvas e do design system), seção 11 ("Como a fase 9F fez a conferência"), seção 15 ("Da fase 9F"), e na "Da fase 9E" o que a 9F fez das pendências. Commit `docs(spec): o que a fase 9F ensinou sobre a migração para o design system`.
- [ ] **Step 9: Suíte inteira de novo** se a conferência mudou código (com o ambiente derrubado), e o aval do usuário para a `main`.

---

## Decisões tomadas na redação

Decisões que os quadros e o contrato não davam e que mudam comportamento ou texto visível, para o usuário revisar. Entre parênteses, a tarefa.

- (1) As linhas das horas da régua são o fundo da grade (sem elemento por hora); eventos simultâneos ficam a 8px um do outro (as margens de 4px de cada cartão).
- (1) O vão do vínculo com rótulo fica em 8px no tablet e no mobile (o quadro tem 6), como o vão dos outros campos.
- (1) Os ladrilhos do tipo e os chips de pessoa usam seletores de duas classes para não serem alcançados pela regra `label:has(> input…)` do `app.css`, que fica até a 9G.
- (1) Os `h2` da agenda levam o `font` inteiro e `margin:0` (a regra `h2` do `app.css` continua até a 9G).
- (1) Título, local e meta dos cartões e o nome do participante quebram palavra longa (`overflow-wrap:anywhere`); o cartão da régua e o item do mês cortam sem quebrar.
- (1) O painel solto do mobile continua como elemento (sem borda, respiro, raio, fundo e sombra), com 16px entre os campos e entre os painéis (`--aj-pagina-gap-solto`).
- (1) A borda do chip de pessoa marcado é o literal do quadro (`#9DB898`); todo `aj-evento` ganha a sombra ao passar o ponteiro (e a subida de 1px no desktop); o ícone `filtrar` entra no design system com traço 2.
- (2) `?visao` só vale em minúsculas e sem acento (`dia`, `semana`, `mes`); o resto conta como ausente.
- (2) Evento curto no fim do dia: o fim fica em 24:00 e o início sobe para caber os dois quartos; a régua se estende pela hora seguinte ao fim já arredondado (18:50–19:00 leva a régua até 20:00).
- (2) A semana entre dois meses do mesmo ano fica no formato curto (`Semana de 28/09 a 04/10/2026`); só a que atravessa o ano leva os dois anos.
- (2) As setas não travam em 2000–2100: um endereço fora da faixa volta a hoje.
- (3) `AvisoDeEventoSalvo` sem sobreposição usa o tipo do recado; `SubtituloSoNoDesktop` também tira o subtítulo da barra do mobile; `Aviso.Frase` só vale no aviso sem ações.
- (4) A falha de carga da ficha mostra a mensagem do servidor (com `Não foi possível consultar o evento.` de reserva), como as fichas da 9D e da 9E.
- (4) Participante sem nome aparece como `Usuário não encontrado`, sem `(inativo)` (hoje saía `Usuário não encontrado (inativo)`); `(você)` vence `(inativo)`.
- (4) Exclusão recusada porque o evento sumiu mostra só `Evento não encontrado.`; as outras recusas, a mensagem do servidor (reserva `Não foi possível excluir o evento.`). Pedir a exclusão tira da tela o recado anterior; trocar de evento na mesma tela também.
- (4) Cada ação aparece pela sua permissão (`Acoes.Excluir`, `Acoes.Editar`); a barra fixa aparece com qualquer uma. Descrição só de espaços conta como sem descrição.
- (5) Processo e atividade escolhidos não têm busca de troca: remove-se e a busca volta. Só o cliente tem `Trocar cliente`, e só no desktop.
- (5) `Quem cria o evento sempre participa.` só no evento novo (na edição o criador pode sair).
- (5) `Cancelar` secundário no tablet (como a 9E; o quadro antigo tem fantasma); fantasma no desktop.
- (5) No mobile, participante inativo como `{primeiro nome} (inativo)`; o X `Fechar sem salvar` aparece em todos os estados da tela.
- (5) Não encontrado na edição: o voltar diz `Evento` e leva à ficha com a origem.
- (5) `A data deve estar entre os anos 2000 e 2100.` fica no aviso do topo; duração vazia dá `Campo obrigatório` no campo de minutos.
- (5) Título, Data, Hora e Descrição aparecem uma vez por grupo de visões no HTML (os invólucros de visão tiram o campo da linha de campos), ligados ao mesmo valor; a Descrição tem 5 linhas no desktop e 3 no tablet e no mobile.
- (5) Os vínculos ficam empilhados também no tablet (o quadro põe Cliente e Processo lado a lado, e nenhuma classe cobre a grade só no tablet); a Tarefa 11 mede e decide.
- (5) O resultado da busca de atividade mostra só o título (como hoje).
- (6) Cartões do mesmo início ficam na ordem do servidor; sábado ou domingo que é hoje fica só com a marca de hoje.
- (7) O `section` do Dia no desktop leva o rótulo sem ", hoje" (o quadro); dia que não é hoje com o número no círculo claro; os rótulos da régua vão da hora inicial à final menos uma (08:00 a 18:00 na régua até 19:00).
- (8) O "hoje" da grade do mês é texto para leitor de tela (no lugar do `aria-label` num `span`); dia de fora do mês não leva a marca de hoje e, na grade compacta, continua tocável (leva a data àquele dia).
- (9) O recado sai da tela ao trocar de período ou ao filtrar; enquanto consulta um intervalo novo, o corpo mostra `Carregando…` e as barras continuam funcionando; `Filtrar` sem mudança não consulta de novo.
- (9) `Novo evento`, `Filtrar agenda` e a barra do mobile só aparecem depois da carga; a falha de `ListarUsuarios` vira um aviso de erro no topo e a agenda segue sem o filtro de participante preenchido; na falha da consulta, as barras continuam.
- (9) Sem `visao`, o Dia do mobile é desenhado inteiro dentro do invólucro do mobile (a régua e a seção do tablet dele ficam escondidas): o HTML cresce, sem efeito na tela.
- (9) O foco vai ao primeiro campo ao abrir o painel de filtros (o painel da Auditoria não faz isso; segue o padrão do envio de documentos).
- (9) O theory de layout cobre as listas e os cadastros da equipe, a agenda nas três visões, `/agenda/novo` e a página não encontrada; as fichas que pedem id ficam de fora.
- (10) O comentário do topo do `app.css` e os títulos das seções mudam (o arquivo já não veste tela antiga).
