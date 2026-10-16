# Migração das telas para o design system — fidelidade ao canvas

Data: 2026-10-10
Estado: aprovada pelo usuário em 2026-10-10; fases 9A a 9G executadas
Fontes de desenho:

- Canvas de telas: https://claude.ai/artifact/HzHWv1pgVXAnYHuPEYQDFj (versão lida: `1791598628-21e1`; a fase 9E leu a `1792026220-8711`; a fase 9F leu a `1792048623-298d`; a fase 9G leu a `1792101725-a2bb` e, a cada publicação do design system, sincronizou a cópia dele: `1792109509-9f64`, `1792133283-09d7` e `1792136392-0b16`, a versão final)
- Design system: https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS (versão lida: `1791598661-a3fd`; a fase 9A publicou a `1791646790-3a09`; a fase 9B, a `1791672723-8f5a`; a fase 9C, a `1791995411-b333`; a fase 9D, a `1792000330-0298` e, depois da conferência, a `1792023403-0d20` e a `1792024406-465e`; a fase 9E, a `1792033039-b794` e, depois da conferência, a `1792048576-7268`; a fase 9F, a `1792086828-2c5f` e, depois da conferência, a `1792101692-3ba2`; a fase 9G, a `1792109485-7666` e, depois da conferência, a `1792133242-6f2d` e a `1792136370-92aa`, a versão final)

## 1. Objetivo

Todas as telas do sistema passam a ser escritas com o design system (classes `aj-` sobre os tokens). As telas que têm quadro no canvas ficam idênticas a ele, em cada uma das três visões: desktop, tablet e mobile.

Hoje as páginas Razor usam a marcação antiga (`h1`, `table`, `.filtros`, `.ficha`, `button.secundario`), vestida pelo `wwwroot/app.css`. A navegação (`MainLayout.razor`) já segue o design system.

### Critérios de sucesso

- Cada tela com quadro no canvas, aberta na largura do quadro, mostra a mesma estrutura, os mesmos componentes, os mesmos textos e as mesmas medidas do quadro.
- Nenhuma função que o sistema tem hoje deixa de existir.
- Nenhuma tela tem CSS próprio: todo estilo vem de `css/tokens.css` e `css/componentes.css`, que são cópias do design system. A única exceção é `ReconnectModal.razor.css`, do aviso de reconexão do Blazor, que não é tela: as regras que o framework exige, escritas só com os tokens.
- O `app.css` termina sem nenhuma regra que vista marcação antiga.
- As duas suítes de teste passam, e cada asserção reescrita continua provando o que provava.

## 2. Decisões

| Tema | Decisão |
|---|---|
| Lacunas do canvas | O que está desenhado fica idêntico. O que não está é completado por analogia com o design system. Nenhuma função some |
| Três visões | A página emite a marcação de cada visão e o CSS mostra a da largura atual |
| CSS | Fonte única no design system. Padrão do canvas sem classe vira classe `aj-` lá, e de lá é copiado para o código |
| Ficha e formulário | Telas inteiras com rota própria e "voltar", como no canvas |
| Layout | A página entrega ao `MainLayout` a barra superior do mobile; botão flutuante e barra de ações fixa ficam na página, presos por CSS |
| Backend | Só os contadores que o canvas mostra e as listagens não devolvem |
| Entrega | Uma spec, sete fases (9A a 9G), um plano por fase, um worktree por fase |

Alternativas descartadas para as três visões:

- **Uma estrutura só, remodelada por CSS.** Não chega à fidelidade: os cartões do canvas têm conteúdo e ordem diferentes das linhas da tabela.
- **Detectar a largura no navegador e renderizar uma visão.** Exige JavaScript, a pré-renderização não conhece a largura e os testes de HTML deixam de cobrir tablet e mobile.

## 3. Regra de fidelidade

Para decidir como uma tela fica, vale a primeira fonte que responder:

1. O quadro da tela no canvas, na visão em questão.
2. O quadro da mesma tela em outra visão (desktop, depois tablet, depois mobile).
3. O quadro de uma tela análoga.
4. O design system: componentes, responsividade, voz e conteúdo.

Consequências:

- **Cada quadro vence na sua visão.** Se um texto, uma ordem de botões ou um ícone muda de uma visão para outra no canvas, o código muda junto. Exemplos: o aviso de conflito da agenda tem três redações; a saudação do início é curta no mobile; a etiqueta "Concluída" leva ícone na tabela do desktop e ponto nos cartões.
- **O canvas vence o código.** Onde o quadro desenha diferente do que existe hoje, o código muda. Exemplos: o início do cliente fica com três atalhos; a lista de clientes tem um só botão "Novo cliente"; "Inativar" sai da lista e vai para a ficha.
- **Função não some.** Se um quadro não mostra uma função que existe, ela é mantida por analogia (seção 8). Informação que um quadro condensa numa visão menor e que continua ao alcance na ficha (o revisor nos cartões de atividade, a OAB nos cartões de usuário) segue o quadro.
- **Medidas do quadro valem.** Medida embutida no canvas que difere de uma classe existente vira modificador da classe, não altera a classe.
- **Texto sem quadro mantém a redação atual**, ajustada à voz do design system quando for o caso.

## 4. Arquitetura

### 4.1 CSS com fonte única

- `wwwroot/css/tokens.css` e `wwwroot/css/componentes.css` continuam cópias de `tokens.css` e `components/bundle.css` do design system. Não se edita à mão.
- O `componentes.css` está três regras atrás do design system (`position:relative` em `.aj-app__coluna`, `.aj-campo-filtro input`, `.aj-etiqueta--andamento`). A fase 9A começa igualando os dois.
- Cada fase começa pelo design system: os padrões que os quadros da fase usam como estilo embutido viram classes `aj-`, são publicados no artifact e copiados para o código. Só então as telas são escritas.
- O `app.css` perdeu, a cada tela migrada, as regras que vestiam a marcação antiga dela. Desde a fase 9G ele tem só a base: `box-sizing`, `body`, `:focus-visible` e `h1:focus`. Uma trava (`AppCssTests`) recusa qualquer outro seletor. A faixa de erro do Blazor é o `aj-faixa-erro` do design system, não do `app.css`. (Emenda de 2026-10-16.)
- A cópia do design system instalada no canvas (`project/ds/araujods/`) é sincronizada a cada publicação do design system (emenda de 2026-10-14): o `tokens.json`, o `tokens.css` e o `components/bundle.css` que mudaram são copiados de um artifact para o outro no servidor, e o registro `designSystems` do `canvas.json` passa a levar a versão publicada. Os quadros em si continuam como estão: onde um quadro tem estilo embutido e o design system já tem classe, a marcação do quadro não é reescrita.

### 4.2 Três visões

- O design system ganha classes de visibilidade apoiadas nas variáveis por visão, que também respeitam `data-visao`: `aj-so-desktop`, `aj-so-tablet`, `aj-so-mobile`, `aj-desktop-tablet` e `aj-tablet-mobile`.
- A página renderiza os blocos de cada visão e marca cada um com a classe da visão. Exemplo de uma lista: barra de filtros e tabela (`aj-so-desktop`); busca, chips e cartões (`aj-tablet-mobile`).
- Bloco escondido usa `display: none`: não é lido por leitor de tela nem recebe foco.
- As listas são paginadas (20 itens), então a duplicação no HTML é limitada.

### 4.3 Layout e encaixes

O `MainLayout` passa a oferecer um encaixe, `barra-superior`, que a página preenche com as seções do Blazor (`SectionOutlet` e `SectionContent`). Ele fica antes do `main`, fora da área que rola, e recebe a barra superior do mobile da tela.

O botão flutuante e a barra `aj-acoes--fixa` não precisam de encaixe: ficam na própria página e o CSS os prende à coluna de conteúdo (`aj-app__coluna`, que é posicionada), fora do fluxo que rola. A barra de ações fica dentro do formulário, para o botão enviá-lo. (Emenda de 2026-10-10, ao planejar a 9A: a primeira versão previa três encaixes.)

Emenda de 2026-10-16: o botão flutuante passou a ir num segundo encaixe, `rodape-do-conteudo`, no fim do `main`, preenchido pelo `CabecalhoDePagina`. Assim ele vem depois do conteúdo na ordem de tabulação; a posição na tela não mudou. A barra de ações continua na página.

A barra superior do mobile tem três variantes, as do canvas:

| Variante | Conteúdo | Barra inferior | Onde |
|---|---|---|---|
| Título | Símbolo, `h1` com subtítulo, avatar; pode ter uma segunda linha (busca, chips, controle segmentado) | Sim | Telas de topo |
| Voltar | Botão de voltar e `h1` centralizado; pode ter uma ação à direita | Não | Ficha e formulário |
| Logo | Logo horizontal e avatar | Sim | Tela que não declara barra (não encontrada, e as ainda não migradas) |

Outras mudanças no layout:

- O invólucro `div.pagina` sai: cabeçalho, filtros e painéis passam a ser filhos diretos de `main.aj-app__conteudo`, que é quem dá o espaçamento entre blocos. Até a 9G, uma regra interina no `app.css` mantém em bloco o conteúdo das telas ainda não migradas, para elas não mudarem de aparência. (Saiu na 9F, quando não sobrou tela não migrada.)
- A barra inferior some nas telas com a barra de voltar. O layout decide por CSS, sem estado.
- "Mais" continua sendo o botão que abre o menu. Leva `aria-current="page"` só pela rota: quando a rota atual não está entre os destinos da barra daquela visão. Com o menu aberto, ele acende pelo `aria-expanded="true"`, e os destinos mantêm o `aria-current` da rota sem o desenho de ativo (emenda de 2026-10-16; até a 9F, o "Mais" levava `aria-current` também enquanto o menu estava aberto).
- Com o menu aberto por "Mais" no tablet e no mobile, o resto da coluna (trilho, conteúdo e barra inferior) fica `inert`: só um `nav "Principal"` fica acessível. O foco vai ao primeiro destino; Esc, o "Fechar menu" e o véu fecham o menu, e o foco volta ao "Mais". (Emenda de 2026-10-16.)
- Com o menu escondido no desktop, o botão "Exibir menu" e o símbolo ficam na linha do título, como no quadro `AgendaMenuEscondido`. Tela sem cabeçalho de página (as ainda não migradas e a página não encontrada) recebe do layout uma faixa com o mesmo botão acima do conteúdo, para o menu sempre poder voltar. Depois do clique em "Exibir menu" ou "Esconder menu", o foco vai ao par visível (emenda de 2026-10-16).
- Mensagem que atravessa uma navegação ("Evento salvo." mostrado na agenda depois de salvar o formulário) passa por um serviço de escopo do circuito: a tela de origem deixa a mensagem, a de destino mostra uma vez.

### 4.4 Componentes Razor compartilhados

Só o que se repete em três telas ou mais. Cada um emite a marcação do canvas, sem estilo próprio, e nasce na fase que primeiro o usa.

| Componente | Emite |
|---|---|
| `CabecalhoDePagina` | O `aj-cabecalho` (desktop e tablet), a barra superior de título (mobile) e o botão flutuante, a partir de título, subtítulo e ação principal |
| `CabecalhoDeDetalhe` | O `aj-voltar` com o cabeçalho (desktop e tablet) e a barra de voltar (mobile), a partir de título, destino e rótulo do voltar |
| `Paginacao` | A linha "Página N de M · X itens" com "Anterior" e "Próxima" |
| `Aviso` | `aj-aviso` de sucesso, atenção ou erro, com ícone; aceita ações (confirmações) |
| `EstadoDaLista` | Carregando, erro com "Tentar de novo" e vazio |
| `MenuDeAcoes` | O menu suspenso do botão "⋯" (seção 9) |
| `Abas` | As abas com contador do mobile (seção 9) |
| `Icone` | Ganha os traçados que faltam (seção 4.6) |

Componentes de um assunto só (cartão de atividade, cartão de evento, linha de documento) ficam na pasta do assunto e nascem na fase dele.

### 4.5 Foco e acessibilidade

- Cada visão tem o seu `h1`; os outros ficam escondidos. O `FocusOnNavigate` de hoje mira o primeiro `h1` do HTML, que pode estar escondido. Ele dá lugar a um foco no título visível depois de cada navegação.
- Botão só de ícone leva `aria-label` com o verbo, como manda o design system.
- A situação aparece sempre como etiqueta com nome; cor nunca é o único sinal.

O que a fase 9G estabeleceu (emenda de 2026-10-16):

- **Regra geral.** Nenhuma ação deixa o foco no `body` com a tela aberta. Quem fecha um painel, um menu ou uma confirmação devolve o foco a quem o abriu. Quando a ação concluiu, o foco vai ao aviso de resultado.
- **Serviço `Foco`** (`Infra/Foco.cs`, sobre o módulo `FocoNoTitulo.razor.js`): `FocarTituloVisivelAsync`, `FocarPrimeiroVisivelAsync(seletor)`, `VigiarAsync` e `ZerarRolagemAsync`. O "primeiro visível" escolhe a cópia à vista entre as cópias por visão e rola até ela; o título da página é focado sem rolar.
- **Rede de segurança.** Quando o elemento focado sai do DOM depois de um desenho, o módulo leva o foco ao `h1` visível. Ela age na hora, esquece o elemento depois de um clique num ponto vazio e não age quando o elemento só fica escondido ou desabilitado. É reserva: o destino certo de cada tela vence.
- **Rolagem.** O `main.aj-app__conteudo` volta ao topo quando muda a página, não quando só a consulta muda (período da agenda, `?produtor=`, paginação). A primeira página do circuito não zera.
- **Aviso que se revela.** `Aviso.Revelar` foca o aviso (`tabindex="-1"`) e rola até ele quando aparece. A tela troca o `@key` do aviso a cada mensagem (um contador), para a mensagem nova se revelar também. Vale para os erros de gravação dos oito formulários e para os avisos de resultado das ações; o recado vindo de outra tela não se revela, para não competir com o foco no título.
- **Foco de volta a quem abriu.** Confirmações, painéis de documento, painel de filtros e menus devolvem o foco ao botão que os abriu, na cópia à vista daquela largura. Escolher um item do menu de ações devolve o foco ao "⋯" quando a ação não o leva a outro lugar (o "Baixar").
- **Paginação.** O botão sem ação leva `aria-disabled="true"` em vez de `disabled`: continua focável, ignora o clique e o foco não se perde na última página nem durante a carga.
- **Reserva de rolagem das barras fixas.** O rolador leva `scroll-padding-bottom` com a altura do que fica fixo embaixo, para o elemento focado pelo Tab não ficar atrás: 112px no desktop com a barra de ações (a barra flutuante inteira e o respiro do conteúdo), 110px no tablet; no mobile, 104px com a barra inferior, 168px com o botão flutuante e 112px com a barra de voltar ou a barra fixa.
- **Peças com teclado próprio:** `MenuDeAcoes` (continua botão com `aria-expanded` e lista de botões; Esc, Tab para fora e o fundo fecham; um aberto por vez), `Abas` (`tablist` com `tabindex` móvel, ←, →, Home e End, `aria-controls` e a fileira rolada até a aba), `PainelDeFiltros` (sempre no DOM com `hidden`; Esc fecha; foco no primeiro campo ao abrir e de volta ao botão ao fechar) e o menu "Mais" (seção 4.3).
- **Erros de grupo.** Grupo com erro (participantes do evento, complexidade da atividade, responsáveis do devolver e do reabrir) leva `aria-invalid` e `aria-describedby` para a frase do erro.

### 4.6 Ícones

`Icone.razor` ganha: `adicionar`, `buscar`, `filtros`, `filtrar` (funil), `voltar`, `avancar`, `confirmar`, `excluir`, `devolver`, `entrar`, `baixar`, `atencao`, `erro`, `enviar` e `planilha`. Os traçados vêm do grupo Icones do design system e dos quadros. O componente passa a aceitar uma classe (`Classe`), para uso fora de menu e botão; o tamanho vem da classe do design system.

## 5. Rotas

| Tela | Rota |
|---|---|
| Novo cliente | `/clientes/novo` |
| Ficha do cliente | `/clientes/{id}` |
| Editar cliente | `/clientes/{id}/editar` |
| Novo processo | `/processos/novo` |
| Ficha do processo | `/processos/{id}` |
| Editar processo | `/processos/{id}/editar` |
| Nova atividade | `/atividades/nova` |
| Ficha da atividade | `/atividades/{id}` |
| Editar atividade | `/atividades/{id}/editar` |
| Responsáveis e revisor | `/atividades/{id}/responsaveis` |
| Devolver para correção | `/atividades/{id}/devolver` |
| Reabrir atividade | `/atividades/{id}/reabrir` |
| Novo evento | `/agenda/novo` |
| Ficha do evento | `/agenda/{id}` |
| Editar evento | `/agenda/{id}/editar` |
| Novo usuário | `/usuarios/novo` |
| Editar usuário | `/usuarios/{id}/editar` |

- As rotas existentes não mudam.
- `RotasDoCliente` é lista de permissão: as rotas novas nascem fechadas ao usuário do portal. Os testes do portal passam a cobrir uma rota nova de cada assunto.
- A agenda guarda a visão e a data no endereço (`/agenda?visao=semana&data=2026-10-05`), para o "voltar" cair no mesmo período. Produtores guarda o produtor escolhido (`/produtores?produtor={id}`).
- A ficha da atividade aberta a partir de Produtores recebe a origem (`?de=produtores`) e volta para lá.
- Depois de salvar, o formulário vai para o destino do seu "voltar" e a mensagem de sucesso aparece lá.
- Identificador inexistente ou sem acesso mostra o estado "não encontrado" da própria tela, com o voltar.

## 6. Backend: contadores

Os totais entram no retorno das listagens que já existem, sem consulta extra auditada. O precedente é `AndamentoDoCliente`, que já devolve totais independentes do filtro.

| Listagem | Contador novo | Onde aparece |
|---|---|---|
| `ListarAtividades` | Total geral e total de atrasadas, independentes do filtro | Subtítulo "8 atividades · 1 atrasada"; chips "Todas · 8" e "Atrasadas · 1" |
| `ListarClientes` | Total de inativos | Subtítulo "6 clientes · 1 inativo" |
| `ListarProcessos` | Total sem filtro de situação | Subtítulo e chip "Todas · 5" |
| `ListarDocumentosDoCliente` | Contagem por área | Chips do painel "Documentos" e aba "Documentos" do mobile |

Os contadores obedecem às mesmas regras de acesso da listagem: quem não lê uma área não recebe a contagem dela.

Os demais contadores do canvas saem do que já existe: `Pagina.Total` e as listas já carregadas.

## 7. Telas

O inventário abaixo vem do levantamento de 2026-10-10. O plano de cada fase relê os quadros, que são a fonte.

### 7.1 Entrada, início e conta (fase 9A)

| Tela | Quadros | Razor | Mudança principal |
|---|---|---|---|
| Entrar | `Main`, `LoginTablet`, `LoginMobile` | `Pages/Conta/Entrar.razor` | Medidas do quadro por visão; rodapé fora do cartão em tablet e mobile; seta no botão; classes no lugar das regras `.entrar` do `app.css` |
| Início | `Inicio*`, `PortalInicio*` | `Pages/Home.razor` | Cabeçalho com saudação; cartões-atalho; no portal, dois painéis de total e três atalhos |
| Minha senha | `MinhaSenha*` | `Pages/MinhaSenha.razor` | Painel estreito; no mobile, barra de voltar e botão na barra fixa; erro por campo |
| Não encontrada | `NaoEncontrada*` | `Pages/NaoEncontrada.razor` | Bloco centralizado com símbolo e botão secundário |
| Menu aberto por "Mais" | `MaisTablet`, `MaisMobile` | `Layout/MainLayout.razor` | Já corresponde; ganha "Mais" ativo |

Sem quadro, pelo design system: página de erro (bloco centralizado, como a não encontrada), faixa de erro do Blazor (`aj-faixa-erro`) e aviso de reconexão (texto em português; é o `<dialog>` do Blazor, com CSS próprio sobre os tokens e botões `aj-botao`; espera 0,3 s para aparecer, sem movimento).

### 7.2 Portal do cliente (fase 9B)

| Tela | Quadros | Razor | Mudança principal |
|---|---|---|---|
| Andamento | `Andamento*` | `Pages/Andamento.razor` | Chips com contadores no lugar do seletor; totais no subtítulo; tabela no desktop e painéis no tablet e no mobile; situação em etiqueta; só a data em "Concluída em" |
| Meus documentos | `MeusDocumentos*` | `Pages/MeusDocumentos.razor`, `Documentos/DocumentosParaBaixar.razor` | Contagem no subtítulo; botão "Baixar"; linhas num painel em tablet e mobile |
| Documentos comuns (cliente) | `DocumentosComuns*` | `Pages/DocumentosComuns.razor` | O mesmo, com o subtítulo do quadro |
| Documentos comuns (Diretoria) | `DocumentosComunsDiretoria*` | `Pages/DocumentosComuns.razor`, `Documentos/DocumentosDoCliente.razor` | "Enviar documento" no cabeçalho (botão flutuante no mobile); aviso de confirmação no topo com nome e tamanho do arquivo; "Cancelar" antes de "Confirmar envio" |

`DocumentosDoCliente.razor` também serve a ficha do cliente (fase 9D), que o canvas desenha de outro jeito. Na 9B ele é separado em dois componentes de tela sobre a mesma lógica.

### 7.3 Administração (fase 9C)

| Tela | Quadros | Razor | Mudança principal |
|---|---|---|---|
| Áreas | `Areas*` | `Pages/Areas.razor` | Tabela e painel "Nova área" lado a lado; em tablet e mobile, linhas num painel com "⋯" |
| Perfis | `Perfis*` | `Pages/Perfis.razor` | O mesmo desenho de Áreas, com a ordem de blocos do quadro |
| Usuários | `Usuarios*`, `UsuarioFormulario*` | `Pages/Usuarios.razor` | Formulário vira tela com rota; cartões em tablet e mobile |
| Produtores | `Produtores*` | `Pages/Produtores.razor`, `Produtores/PainelDeProdutividade.razor` | Lista e painel lado a lado; produtor escolhido marcado "Em acompanhamento"; totais em quatro blocos; seletor de produtor em tablet e mobile; "Abrir" navega para a ficha |
| Auditoria | `Auditoria*` | `Pages/Auditoria.razor` | Controle segmentado no cabeçalho; dados resumidos e identificadores truncados; cartões em tablet e mobile |

### 7.4 Clientes e processos (fase 9D)

| Tela | Quadros | Razor | Mudança principal |
|---|---|---|---|
| Clientes | `Clientes*` | `Pages/Clientes.razor` | Um botão "Novo cliente"; "Abrir" por linha; cartões em tablet e mobile |
| Ficha do cliente | `Cliente*` | novo | Cabeçalho com "Inativar" e "Editar"; processos em tabela; um painel "Documentos" com chips por área; abas no mobile |
| Novo cliente | `ClienteFormulario*` | novo | Controle segmentado de tipo; painéis "Identificação e contato", "Endereço" e "Observações" |
| Processos | `Processos*` | `Pages/Processos.razor` | Chips de situação em tablet e mobile; "Sem número" e "—" nos vazios |
| Ficha do processo | `Processo*` | novo | Clientes em tabela com polo; painel "Situação" com seletor e "Alterar" |
| Novo processo | `ProcessoFormulario*` | novo | Cliente vinculado em linha com polo; busca de cliente sem botão |

A Diretoria passa pela ficha de leitura antes de editar, como no canvas.

### 7.5 Atividades (fase 9E)

| Tela | Quadros | Razor | Mudança principal |
|---|---|---|---|
| Atividades | `Atividades*` | `Pages/Atividades.razor` | Subtítulo com contadores; cartões com avatares em tablet e mobile; chips de situação |
| Ficha da atividade | `FichaDaAtividade`, `FichaTablet`, `FichaMobile` | `Atividades/FichaDaAtividade.razor` e filhos | Tela com rota; cabeçalho em cartão com faixa de ações; histórico em linha do tempo; comentários em balões; abas no mobile |
| Nova atividade | `AtividadeFormulario*` | `Atividades/FormularioDeAtividade.razor` | Tela com rota; painéis "Atividade" e "Vínculos"; complexidade em controle segmentado |
| Responsáveis | `Responsaveis*` | `Atividades/EditorDeResponsaveis.razor` | Tela com rota; painel "Revisor" ao lado |
| Devolver | `Devolver*` | painel da ficha | Tela com rota; painel único |

### 7.6 Agenda (fase 9F)

| Tela | Quadros | Razor | Mudança principal |
|---|---|---|---|
| Agenda | `AgendaA`, `AgendaMenuEscondido`, `AgendaTablet`, `AgendaMobile` | `Pages/Agenda.razor` | Dia, Semana e Mês; grade de sete colunas no desktop; lista por dia no tablet; faixa de dias no mobile; cartão inteiro clicável |
| Novo evento | `NovoEvento*` | `Agenda/FormularioDeEvento.razor` | Tela com rota; painéis "Evento", "Vínculos" e "Participantes"; erro por campo |
| Ficha do evento | `Evento*` | `Agenda/FichaDoEvento.razor` | Tela com rota; participantes com avatar; "Excluir" e "Editar" |

O quadro `AgendaB` foi descartado no canvas e não entra.

A consulta `ListarEventos` já aceita o intervalo de um mês. Sem endereço de visão, a agenda carrega a semana corrente e cada largura mostra o seu padrão sobre os mesmos dados: Semana no desktop e no tablet, Dia (com a faixa da semana) no mobile.

## 8. Lacunas completadas por analogia

| Lacuna | Como fica | De onde vem |
|---|---|---|
| Edição | A tela "Novo…" com título "Editar…" e campos preenchidos. O que o backend não deixa trocar fica travado (tipo do cliente, tipo do perfil) | Quadros "Novo…" |
| Confirmações: excluir, inativar, aprovar, concluir minha parte | `aj-aviso` de atenção com "Cancelar" e depois o verbo | Confirmação de inativar usuário (`Usuarios*`) e de envio (`DocumentosComunsDiretoria*`) |
| Sucesso e erro de uma ação | `aj-aviso` de sucesso ou de erro no topo do conteúdo | Aviso da agenda e da ficha da atividade |
| Carregando, erro de consulta, lista vazia | Frase simples de estado; erro com "Tentar de novo" | Voz do design system; vazio da agenda no mobile |
| Acesso encerrado | `aj-aviso` de erro no lugar do conteúdo | Design system |
| Erro de campo | `aj-rotulo--erro`, `aria-invalid` e `aj-campo-erro` | `MinhaSenha*`, `NovoEvento*` |
| Visão da Produção | O mesmo desenho, sem o que ela não pode fazer | Quadros da Diretoria |
| Paginação em tablet e mobile | A linha de paginação do desktop, ao fim dos cartões | Quadros de desktop |
| Envio e renomeação de documento | O botão do quadro abre o formulário num painel na própria tela, com arquivo (até 50 MB) e nome opcional | Painel "Nova área" |
| "Quem acessa" do documento geral | Painel sob a linha do documento | O mesmo |
| Reabrir atividade | A tela de devolver, com título e botão "Reabrir" | `Devolver*` |
| Trocar revisor | O painel "Revisor" da tela de responsáveis | `Responsaveis*` |
| Complexidade (posterior aos quadros) | Coluna na tabela do desktop, linha em "Detalhes" da ficha e controle no formulário, que o quadro já desenha | `AtividadeFormulario*` |
| Histórico e ações de documento na ficha do tablet | Mantidos, como no desktop | `FichaDaAtividade` |
| "Observações" na ficha do cliente no tablet | Mantido, como no desktop | `Cliente` |
| Variante de pessoa física da ficha do cliente | Os mesmos painéis, com CPF, RG e nascimento | `Cliente*` |
| Inativar ou ativar usuário em tablet e mobile | Ação na tela de edição do usuário, afastada da barra de ações | Regra da barra de ações do design system |
| Aba "Consultas" da auditoria | A estrutura da aba desenhada, com os filtros e as colunas dela | `Auditoria*` |
| Formulário de atividade sem processo | Os campos de área e de cliente no painel "Vínculos" | `AtividadeFormulario*` |
| Resultados de busca de vínculo | Lista suspensa com "Escolher" | Busca de processo do `NovoEvento` |
| Busca sem botão | Responde ao Enter | Comportamento atual |
| Estados da agenda que os quadros não mostram | Régua do Dia se estende até o primeiro e o último evento; eventos simultâneos dividem a largura; evento de mais de um dia aparece em cada dia; mês de seis semanas ganha a sexta linha; célula do mês mostra três eventos e "+N" | `AgendaA` |
| Autor nulo na auditoria | Continua "Sistema": o dado não distingue o caso anônimo | Comportamento atual |

## 9. Desenho novo

Quatro itens que o canvas indica mas não desenha. São desenho de fato, não só analogia.

**Menu de ações ("⋯" e "Mais ações").** Menu suspenso ancorado no botão, em painel com sombra flutuante, com um item por ação, na ordem do desktop. Ação destrutiva por último, em cor de erro. Fecha ao escolher, ao tocar fora e com Esc. Leva as mesmas ações da linha do desktop: renomear e inativar (áreas, perfis); renomear, excluir e baixar (documentos); editar, responsáveis e excluir (ficha da atividade no mobile).

**Painel de filtros (tablet e mobile).** Os botões "Filtros", "Responsável, revisor", "Mais filtros" e "Usuário, operação" abrem, logo abaixo da barra, um painel com os mesmos campos da barra de filtros do desktop, em uma coluna, com "Limpar" e "Filtrar". Os chips ficam como o quadro desenha; o painel traz o filtro completo, inclusive as situações sem chip.

**Abas do mobile.** Só a primeira aba de cada ficha está desenhada. Cada outra aba mostra o painel correspondente do desktop, em uma coluna, na ordem de leitura. O contador da aba vem dos dados já carregados ou dos contadores da seção 6.

**Visões da agenda sem quadro.**

| Visão | Tablet | Mobile |
|---|---|---|
| Dia | A seção de um dia do quadro do tablet, com os cartões em duas colunas | Desenhado |
| Semana | Desenhado | A lista por dia do tablet, em uma coluna |
| Mês | A grade do mês do desktop | Grade compacta com um ponto por evento, como a faixa de dias, e a lista do dia escolhido embaixo |

Estes quatro itens ganham classes no design system como os demais, com prévia e regra de uso.

## 10. Testes

- As asserções presas à marcação antiga (`<td>` sem atributos, `<th>Por</th>`, `class="dia"`, `<dt>`/`<dd>` dos totais) são reescritas na fase da tela.
- Cada asserção reescrita mantém o que prova: quem vê o quê, o que não aparece para a Produção, o que não vaza para o portal, o que não é consultado.
- Como o HTML passa a ter o conteúdo de mais de uma visão, asserção de contagem ou de ordem olha um bloco de visão, não a página inteira. Os testes ganham um auxiliar para recortar o bloco.
- `TelaDeTeste` registra só o despachante e o fuso. Componente que passar a navegar exige que ela registre também a navegação.
- Cada rota nova ganha teste de acesso: Diretoria, Produção quando couber, e usuário do portal recebendo "Página não encontrada".
- Os contadores da seção 6 ganham teste de integração contra o banco, com outros perfis presentes.

## 11. Verificação

Por fase:

- Build sem avisos novos e as duas suítes de teste.
- Cada tela conferida no Chrome contra o quadro do canvas, nas três larguras, num ambiente descartável (banco e aplicação isolados, como nas fases anteriores). Tablet e mobile são vistos numa página auxiliar com molduras de 834 e 390 px, fora do repositório, porque a ferramenta do navegador não redimensiona a janela.
- Conferência estrutural: as classes e a ordem dos blocos do HTML renderizado batem com as do quadro.
- Navegação por teclado em cada tela nova: foco no título ao entrar, ordem de tabulação, menu de ações.

Como a fase 9A fez a conferência (emenda de 2026-10-10), para as fases seguintes repetirem:

- A tela é medida, não só olhada: estilos computados e caixas dos blocos são comparados com os valores do quadro, e o roteiro devolve só o que diverge. A captura fica como registro para o usuário.
- As três larguras são vistas em molduras da mesma origem (1440×900, 834×1194 e 390×844) abertas sobre uma aba do próprio sistema. A moldura responde às consultas de mídia pela própria largura e, reduzida por `transform`, cabe inteira na janela, com as três visões lado a lado numa captura só.
- Com a aba em segundo plano o Chrome estrangula os temporizadores da página. As esperas dos roteiros usam um Worker, que não é estrangulado.
- `EditForm` com `FormName` emite campos ocultos antes dos campos da tela. O roteiro escolhe os campos pela classe (`input.aj-campo`), nunca pela posição.
- Elemento com `display: contents` não tem caixa. A visão ativa de um invólucro se confere pelo `display` computado, não pela caixa.
- Enter num formulário com um botão de envio por visão envia uma vez só, pelo primeiro botão da marcação, mesmo quando ele está escondido naquela largura.

Como a fase 9B fez a conferência (emenda de 2026-10-11), além do que está acima:

- O quadro é desenhado no navegador, não só lido. Os arquivos `.dc.html` da cópia local do canvas são HTML comum com as classes `aj-` e o CSS do design system da época; um servidor local os entrega e a tela do sistema é medida contra o quadro desenhado na mesma largura. A navegação dos quadros vem de componentes importados que não desenham fora do canvas: reserva-se a caixa que o quadro declara (o menu e o trilho ocupam a lateral; a barra inferior do mobile fica fora do fluxo). As imagens dos quadros não carregam fora do canvas: troca-se cada uma por uma imagem vazia da mesma proporção.
- Mede-se por "átomos": todo elemento visível com texto próprio, todo elemento gráfico (ícone, imagem, campo) e toda caixa com fundo, borda ou sombra, na ordem do documento, com a caixa relativa à coluna de conteúdo e os estilos computados. Os átomos do quadro e os do sistema são casados pela ordem; os textos que o sistema mostrou são copiados para o quadro antes de medir, trecho a trecho, para as caixas dependerem só do desenho. O roteiro devolve só o que diverge.
- Para isso o ambiente descartável recebe os mesmos dados do quadro em quantidade e forma (oito atividades, três em andamento e cinco concluídas; quatro documentos), e a tela é posta no mesmo estado (a confirmação de envio aberta).
- O que o `app.css` muda dentro de uma tela migrada se mede desligando a folha na página e comparando os estilos computados antes e depois. Foi assim que apareceram as regras de elemento que alcançavam o campo de arquivo e as etiquetas. O que sobra dessa medida são os ajustes globais que ainda moram no `app.css` (margem do `body`, `box-sizing`, cor dos links), assunto da 9G.
- Arquivo se escolhe por roteiro, dentro da moldura, com `DataTransfer` e um evento `change`; o conteúdo é gerado na página, e o SHA-256 do que foi enviado se compara com o do que o cliente baixa.
- A chamada de roteiro da ferramenta do navegador expira em 45 s. Roteiro longo é disparado sem esperar e consultado depois. Saída que parece `chave=valor` é bloqueada: trocar `=` por `:`.
- Zoom diferente entre abas faz a borda de 1px medir 1,11px: larguras de borda se comparam com folga. A moldura de 390px mostra barra de rolagem comum do desktop quando o conteúdo passa da altura, o que estreita o conteúdo em relação a um celular: o quadro desenhado na mesma moldura tem a mesma barra, e a comparação continua válida.
- As credenciais de teste ficam num arquivo local que só a página lê; o ambiente, o arquivo e as abas são removidos antes da última rodada da suíte.

Como a fase 9C fez a conferência (emenda de 2026-10-12), além do que está acima:

- Sem a ferramenta do navegador na sessão, um Chrome sem janela, com perfil próprio, foi dirigido pelo protocolo DevTools por um condutor local; os roteiros da 9B rodaram iguais. Sem aba em segundo plano, nada é estrangulado e a chamada não expira em 45 s. Teclas reais vão à moldura depois de focá-la.
- A barra de rolagem se esconde nos dois lados antes de medir (`::-webkit-scrollbar{width:0}` na moldura do sistema e na do quadro). Só um dos lados pode rolar (a coluna do quadro de Produtores rola no mobile, a do sistema não), e a barra comum do desktop tirava 15px de um deles. Isso corrige a observação da 9B sobre a mesma barra.
- Quadro com ramos condicionais (`sc-if`) desenha os dois ramos fora do canvas: o formulário de usuário mostra o campo da OAB e o bloco do cliente juntos. Quadro com modelo (`{{ r.titulo }}`) desenha um cartão-modelo só. Nesses casos as medidas valem para os átomos casados e o resto se confere por captura lado a lado.
- Quando a cópia dos textos do sistema muda a forma do quadro (dados longos na tabela da Auditoria), a comparação automática embaralha as colunas: a tela se confere por captura.
- O ambiente recebe dados que o quadro não tem, para o Review Focus: nome e e-mail longos sem espaço, cliente inativo, linha inativa na mesma posição do quadro (a ordem alfabética decide: renomear o item é mais simples que reordenar).
- Com o CSS da fase ainda fora do design system, a publicação descartável usa o `bundle.css` pendente: o `componentes.css` do worktree é trocado só durante o `dotnet publish` e restaurado em seguida, sem commit. O que a conferência pede de CSS muda no pendente, não no código.

Como a fase 9D fez a conferência (emenda de 2026-10-14), além do que está acima:

- Os quadros com repetição e ramos (`sc-for`, `sc-if`, `{{ … }}`) passaram a ser medidos: um roteiro expande o quadro com o `renderVals()` dele antes de medir, e as linhas e os cartões repetidos do quadro são reordenados na ordem em que o sistema os mostra (o sistema ordena por nome, título ou data; o quadro segue o exemplo). A captura lado a lado continua como registro.
- Quando o DOM vem em outra ordem e a disposição na tela é a mesma (a ficha do cliente, com o painel primeiro na marcação e à direita no desktop), a comparação é feita pela posição na tela, não pela ordem do documento.
- Os dados do quadro entram pela tela onde isso exercita a própria tela (o cliente e o processo dos quadros principais, pelos formulários) e por SQL no banco descartável no resto (os outros clientes e processos, os documentos, os autores). Os casos de borda (nome, e-mail e nome de arquivo longos) entram por último, depois de medir, para não mudar as contagens dos quadros.
- O que só se vê depois de um clique se confere no navegador, não no bUnit, porque o bUnit desenha a árvore e não aplica as diferenças a um DOM de verdade: foi assim que apareceu o cliente vinculado pela busca sem a classe (o candidato e o vínculo tinham a mesma chave `@key` no mesmo pai, e o Blazor reaproveitava o elemento).
- O passo de teclado e de estados vira lista fixa por fase: abas do mobile (toque e Tab), menu de ações (Tab, Esc, fundo), confirmação no topo a 390px, foco ao abrir um painel, barra fixa sem cobrir o último painel, larguras 760, 761, 1199 e 1200, busca da barra do mobile com Enter, e a Produção sem nenhuma ação de cadastro.
- A máquina reiniciou três vezes durante a fase, uma delas no meio da conferência: as coletas gravadas em disco à medida que se mede deixaram retomar sem refazer nada.

Como a fase 9E fez a conferência (emenda de 2026-10-15), além do que está acima:

- O "hoje" do sistema é o do relógio, e o contador de atrasadas depende dele: todas as datas do quadro (entregas, histórico, conclusões, comentários, documentos) foram deslocadas pelo mesmo número de dias (+7) para o "hoje" do quadro cair no dia da conferência. Só assim o `8 atividades · 1 atrasada` do quadro sai igual.
- A atividade principal do quadro foi criada e levada até Feita pelas próprias telas da fase, com três perfis entrando (a Diretoria cria e define os responsáveis; duas pessoas da Produção enviam documento, comentam e concluem). Por SQL entraram só as horas, para baterem com o quadro, e as outras sete atividades da lista.
- Os quadros antigos de estilo embutido (lista e ficha) foram medidos pelo mesmo `ref.html` dos novos: sem `.aj-app__coluna`, a raiz da medida é o `main` (ou o pai dele, quando há barra superior antes), e a navegação e o botão flutuante embutidos ficam fora dos átomos. O ponto de 6px das etiquetas (um `span` no quadro antigo, um `::before` no design system) fica fora da comparação.
- O que o plano já decidiu diferente do quadro e desalinharia a comparação átomo a átomo (a coluna e a linha Complexidade, o "Enviar documento", o painel Histórico do tablet) sai do DOM das molduras antes de medir, nunca do código; as medidas com e sem isso ficam nas coletas.
- O texto de um filho com `display: contents` (os invólucros de visão) conta como texto do pai; sem isso, texto dentro de um invólucro sumia da medida.
- No Chrome sem janela, a captura de uma aba em segundo plano não volta nunca: a aba vem para a frente (`/json/activate`) antes de cada captura.
- O clique duplo foi provado no navegador também onde o bUnit não prova (o bUnit descarta clique em botão desabilitado): Excluir e Comentar, com a contagem de registros no banco e na auditoria. Onde a resposta chega antes de 50 ms, não dá para ver o botão travado; a prova é a contagem.
- Um dado inventado errado derruba o circuito (um número CNJ com dígito errado lança ao materializar o processo na busca): dado de quadro passa pela mesma validação do domínio antes de entrar por SQL.

Como a fase 9F fez a conferência (emenda de 2026-10-15), além do que está acima:

- Os ramos `sc-if` de um quadro passaram a ser medidos um a um, átomo a átomo, e não só por captura: o `expandir.js` aceita props que substituem as do quadro, e a página de medida recebe `props=JSON` (no `AgendaA`, `visaoInicial` para o Dia e o Mês e `menuEscondido` para o `AgendaMenuEscondido`) e `tirar=seletor` (tira do quadro o aviso de conflito quando o sistema não o mostra naquele estado).
- Estado que o deslocamento comum das datas não alcança vira um quadro derivado só nos dados, fora do canvas: `AgendaAMes15` (os eventos do mês e o "hoje" deslocados +7 dias, para o Mês casar célula a célula) e `AgendaMobileQua` (a quarta escolhida, o estado a que o sistema volta depois de salvar o evento de quarta).
- Os eventos entraram todos pelo formulário novo, nenhum por SQL, cada um criado por alguém que participa dele no quadro, porque quem cria sempre participa. O aviso de conflito só aparece logo depois de salvar: o evento que conflita foi salvo uma vez em cada largura e excluído pela ficha antes do seguinte.
- O Enter das buscas se prova com tecla real no navegador. Os testes enviam o formulário direto e não passam pelo envio implícito, e foi isso que escondeu o defeito da busca de processo: as duas cópias (desktop e tablet/mobile) estavam no mesmo formulário escondido, e um formulário sem botão de envio com dois campos de texto não é enviado pelo Enter (no Chrome, com um campo, um envio; com dois, nenhum). Regra: um formulário escondido por cópia de campo que busca pelo Enter, e um teste de página que confere que cada `form="busca-de-…"` aparece uma vez só no HTML.
- A altura também se mede. O Chrome sem janela mede 390×844 sem a interface do navegador, e um celular comum tem menos: o Mês do mobile foi medido também em molduras de 375×667 e 360×640 (a lista do dia alcançável pela rolagem, o último cartão fora da barra inferior e do botão flutuante).
- O nome acessível se lê da árvore de acessibilidade do Chrome (o condutor ganhou `/axnome`, que devolve papel e nome), não do HTML: foi assim que se confirmou o espaço entre a hora e o título do item do Mês, que são `span` vizinhos.
- No Mês, o texto só para leitor de cada item (`span.aj-so-leitor`, o tipo) sai da moldura antes de medir; sem isso a comparação átomo a átomo escorrega um átomo por item.
- A consulta repetida se confere pela contagem dos registros de `ListarEventos` por moldura: cada abertura grava dois (a pré-renderização e a interativa); a troca de período dentro do mesmo intervalo, nenhum.

Como a fase 9G fez a conferência (emenda de 2026-10-16), além do que está acima:

- Não havia quadro novo. Conferiu-se o que a fase mudou (o `app.css`, o teclado e o leitor de tela) em todas as telas. O ambiente descartável foi o da 9F, copiado para `conferencia-de-telas\9g\` e ampliado: aplicação publicada em Release sobre `ab8100f`, Postgres 17 em contêiner, dois Chromes sem janela, cada um com o seu condutor.
- Os dados levaram cada lista à segunda página: 26 clientes, 23 processos, 34 atividades em todas as situações, 13 eventos (seis no mesmo dia, para o "+N" do Mês), 70 registros de auditoria, 22 cópias de cada documento enviado. Áreas, usuários e um envio em cada lugar entraram pelas telas; o resto, por SQL. Entrou também um usuário com nome longo.
- 52 rotas × 3 larguras: a equipe, o portal e as rotas sem circuito, com a agenda em Dia, Semana e Mês. Mais molduras extras (1024×768, 1280×800, 900×1000, 375×667, 760, 761, 1199 e 1200) e as fracionárias, com `zoom: 2`, onde o `main` mede 760,5 e 1199,5px.
- Perfis: Diretoria, Produção, o nome longo e o cliente do portal.
- O `app.css` foi medido de dois jeitos. O da 9B (`V.vazamento`: a folha ligada contra desligada) mostrou só os efeitos da base (largura e altura pela caixa de borda e pela margem do `body`). Um novo, `V.antigo`, mediu a mesma marcação com o CSS de hoje e com o da 9F, em todo elemento e pseudoelemento: só as mudanças pretendidas (o nome com reticências, a paginação com `aria-disabled`) e diferenças sem efeito na tela. O mesmo nos estados que a página parada não mostra: campos inválidos, foco nos campos com ícone, painéis de filtro abertos, o segmento travado da edição do cliente e os resultados de busca durante um salvar (com o banco em `docker pause`).
- O teclado foi medido com Tab real, do começo do documento até sair da moldura, em toda rota e largura. A cada parada, o roteiro registra o contorno, se algo cobre o elemento, e se ele está sob `inert` ou `aria-hidden`. O condutor ganhou `/percorrer`, `/clicar` e `/tocar` (mouse e toque reais), `/rede` (rede lenta, para o piscar ao conectar) e um `/axnome` com as propriedades e as relações da árvore de acessibilidade. Passaram a lista fixa da 9D, o Review Focus do plano e cada item de "Fica para o navegador".
- Oito defeitos:
  - D1: "Baixar" pelo teclado, no menu de ações, deixava o foco no `body`. Corrigido: escolher um item devolve o foco ao "⋯" quando ele se perde.
  - D2: o elemento focado pelo Tab ficava atrás da barra de ações fixa ou da barra inferior (anterior à 9G). Corrigido no design system com a reserva de rolagem (seção 4.5).
  - D3: na ficha da atividade a 390px, a aba focada por End ou Home ficava fora da vista. Corrigido: a tecla rola a fileira até a aba.
  - D4: depois do "+N" do Mês, a data era anunciada duas vezes (o título do Dia focado e a região do período). Corrigido: nessa troca a região do período fica vazia.
  - D5: o foco passa pelo `body` enquanto um botão desabilitado grava ("Salvar nome") e termina no aviso. Registrado (seção 15, "Da fase 9G").
  - D6: "Remover" um vínculo do evento e "Trocar" o cliente do usuário levavam o foco ao título. Corrigido: o foco vai à busca que aparece.
  - D7: a faixa de erro do Blazor não tinha `role="alert"` (anterior à fase). Corrigido.
  - D8: o aviso de reconexão não tem nome acessível (anterior à fase). Registrado.
- A reconferência, sobre `bccdee9`, deu D1, D3, D4, D6 e D7 certos e o D2 certo a 390 e a 834, mas não a 1440: a barra de ações do desktop é `sticky` e flutua 16px acima da base, e a reserva de 84px não a cobria inteira. Com 112px (`9bb6299`), uma rodada curta passou no desktop e nas rotas que faltavam a 390.
- Para ver se o foco passa por um elemento sem ser desenhado, o roteiro grava os `focusin` e `focusout`, o elemento focado a cada 20 ms e o elemento focado em cada quadro desenhado (`requestAnimationFrame`). Foi assim que se viu que, no Renomear e no Excluir pelo menu, o foco passa de 0 a 3 ms pelo "⋯" sem nenhum quadro desenhado com ele.
- O Chrome só rola no foco quando o elemento está inteiro fora da área útil: a caixa de "Observações" do Novo cliente a 834px, com o topo à vista, fica com 4px sob a barra, qualquer que seja a reserva.
- Não deu para conferir nesta máquina:
  - leitor de tela real (NVDA, VoiceOver): a fala foi conferida só pela árvore de acessibilidade do Chrome;
  - Safari e Firefox do macOS e o Safari do iOS; o Firefox do Windows também não estava instalado;
  - o WebKit só em parte: o Playwright WebKit 26.4 do Windows (já instalado em outro projeto, nada novo) provou o Enter pelo botão de envio escondido e o menu de ações com mouse, mas foca o botão ao clique, ao contrário do Safari, e a guarda de ponteiro do menu não foi exercitada;
  - Alt+← nas abas e o Esc com a lista de um `select` aberta, que dependem da janela do navegador;
  - o clique duplo real no "Tentar de novo" do `CargaDoFormulario` e o erro da busca de cliente do processo: os estados não se alcançam sem mudar código (as consultas só falham por acesso negado, que leva a outro estado; uma falha de banco derruba o circuito).

## 12. Fases

| Fase | Conteúdo |
|---|---|
| 9A, fundação | Design system (classes de visibilidade, padrões gerais, ícones), sincronização do CSS, encaixes do layout, os componentes compartilhados que as telas da seção 7.1 usam, foco ao navegar; telas da seção 7.1 |
| 9B, portal do cliente | Seção 7.2 |
| 9C, administração | Seção 7.3 |
| 9D, clientes e processos | Seção 7.4 e os contadores de clientes, processos e documentos |
| 9E, atividades | Seção 7.5 e os contadores de atividades |
| 9F, agenda | Seção 7.6 |
| 9G, limpeza | Remoção da camada antiga do `app.css`, revisão final de todas as telas nas três visões |

- Um plano por fase, escrito logo antes de executá-la, para aproveitar o que a anterior ensinou.
- Um worktree por fase. A fase entra na `main` ao fim, com o aval do usuário.
- Entre fases a aplicação continua inteira: tela não migrada segue com a marcação antiga e com a barra superior de logo.
- No início de cada fase o canvas é baixado de novo. Quadro que mudou desde esta spec é apontado antes de implementar.

## 13. Fora de escopo

- Redesenhar quadros ou converter os quadros antigos do canvas para classes `aj-`.
- Acrescentar ao canvas os quadros das lacunas completadas por analogia.
- Função nova: recorrência e lembrete de evento, filtro da agenda por cliente, substituir documento, matriz de permissões.
- Tema escuro.
- A tarefa de segurança do login e de Minha senha (emenda de 2026-10-16; anotada na 9A, seção 15): o login devolve a senha digitada no HTML da resposta quando a credencial é recusada, e os botões de envio do login e de Minha senha não se desabilitam durante o envio. Fica como tarefa própria, depois da migração.

## 14. Riscos

| Risco | Tratamento |
|---|---|
| O canvas muda durante a migração (o usuário o edita pelo Chat do canvas) | Baixar de novo a cada fase e apontar diferenças |
| Bloco escondido recebendo foco ou sendo lido | `display: none` nos blocos de outra visão; conferência por teclado em cada fase |
| Asserção reescrita que passa a não provar nada | Revisão das asserções negativas uma a uma; cada uma ganha uma afirmação positiva ao lado |
| Fases longas divergindo da `main` | Fase curta, entrada na `main` ao fim de cada uma |
| Desenho novo (seção 9) não agradar | Cada item aparece primeiro na fase que o usa, é mostrado no navegador e pode ser ajustado antes da entrada na `main` |

## 15. Anotado na fase 9A para as fases seguintes

Vistos na conferência da fase 9A, fora do que ela mudava. Nenhum é regressão da migração.

- Bloco do usuário no menu: nome longo quebra em mais de uma linha, sem reticências. É regra do design system anterior à fase. A decidir na 9G. — Decidido na 9G: uma linha com reticências, e o nome inteiro no `title` do texto.
- Menu aberto por "Mais" no tablet e no mobile: o foco continua no título da tela, não vai para o menu. A decidir na 9G, junto da revisão de teclado de todas as telas. — Feito na 9G: o foco vai ao primeiro destino, o resto da coluna fica `inert`, Esc fecha e o foco volta ao "Mais" (seção 4.3).
- Login: depois de um erro de credencial o campo de senha volta preenchido na resposta. Já era assim antes da migração; limpar o campo é mudança de comportamento, fora desta spec. — Continua fora (seção 13).
- Telas ainda não migradas, no mobile: o respiro inferior do conteúdo passou de 96 para 104 px, o valor do canvas para tela com barra inferior. — Sem objeto desde a 9F: não há tela não migrada.

Da revisão final do branch da fase 9A:

- Navegador mínimo: o layout depende de `:has()` (Chrome 105, Safari 15.4, Firefox 121). Sem ele a regra interina do `app.css` não vale e a barra inferior cobre o botão fixo das telas com barra de voltar. — Registrado na 9G no `responsividade.md` do design system (Chrome e Edge 105, Safari 15.4, Firefox 121); o design system continua dependendo de `:has()`.
- Na abertura da 9B, antes das telas:
  - reforçar a trava das telas migradas (`TelasNoDesignSystemTests`): recusar `<style` e arquivo `.razor.css` ao lado da tela; recusar `<button`, `<table`, `<input`, `<select` e `<textarea` sem `class`; conferir também `Classe=` de componente; separar as classes por qualquer espaço em branco;
  - tirar de `LayoutWebTests` os testes de Início e de Minha senha, cada tela no seu arquivo, e criar o auxiliar de recorte por visão;
  - afirmar no teste que a barra da tela é irmã imediatamente anterior da barra padrão (o CSS que esconde a padrão depende disso);
  - cobrir os estados "falha dos totais" e "acesso encerrado" do início;
  - dar classe do design system ao ícone "mais", que ainda sai com `style`.
- Enquanto uma tela não for migrada, nenhum componente do design system entra solto no topo do conteúdo dela: a regra interina a trataria como migrada. Há teste que trava isso para as telas da equipe.
- Em cada fase, listar as regras de elemento do `app.css` que alcançam as classes `aj-` que a fase vai usar e tirá-las junto com a tela. Já conhecidas: `h1 { letter-spacing }`, que chega ao título da barra de voltar, e `.aj-etiqueta + .aj-etiqueta { margin-left }`, que somaria ao `gap` de duas etiquetas num cartão.
- Para a 9G, junto da revisão de teclado e de leitor de tela:
  - "Mais" fica com `aria-current="page"` enquanto o menu está aberto (o estado aberto já é dito por `aria-expanded`); a troca do gancho de estilo é do design system; — feito na 9G: `aria-current` só pela rota, e o "Mais" aberto acende pelo `aria-expanded` (seção 4.3);
  - "Exibir menu" e "Esconder menu" somem depois do clique e o foco se perde; — feito na 9G: o foco vai ao par visível;
  - há três `<nav aria-label="Principal">`, dois à vista com o menu aberto no tablet; — feito na 9G: com o menu aberto, o trilho e a barra inferior ficam `inert`, e só um `nav` fica acessível;
  - a rolagem do conteúdo é preservada entre navegações; — feito na 9G: o `main` volta ao topo quando muda a página (seção 4.5);
  - "Mais", o nome e o grupo Administração piscam ao conectar o circuito, porque o ator chega depois da primeira renderização interativa; — feito na 9G: o ator passa da pré-renderização ao circuito pelo estado persistente do .NET 10 (só o layout; o "Carregando…" das páginas ficou);
  - entre 1199 e 1200 px (largura fracionária, com zoom) nenhuma das duas consultas de mídia vale; no design system, `(width < 1200px)` e `(width >= 1200px)`. — Feito na 9G, sem a sintaxe de intervalo (que pediria Safari 16.4): `max-width:1199.98px` e `max-width:760.98px`.
- Fora da migração, como tarefa de segurança própria: o login devolve a senha digitada no HTML da resposta quando a credencial é recusada, e os botões de envio do login e de Minha senha não se desabilitam durante o envio. — Continua fora (seção 13).

### Da fase 9B (emenda de 2026-10-11)

O que a fase entregou além das telas da seção 7.2, e que as próximas reutilizam:

- Peças compartilhadas: `Paginacao`, `EstadoDaLista` (erro, carregando, vazio, nessa ordem de precedência), `MenuDeAcoes`, `Aviso` com ações, ação principal no `CabecalhoDePagina` (botão no cabeçalho e botão flutuante), `Contagem.De`, `FusoDoEscritorio.FormatarData`.
- `Aviso` com `Rotulo` é o diálogo de alerta das confirmações: leva o foco a si mesmo quando aparece (o que também rola a tela até ele) e guarda sozinho a falha de foco. A tela não pede o foco. Confirmação que pode ser trocada por outra do mesmo tipo leva `@key` com o item, para o foco voltar a ela.
- Painel que se abre por uma ação (enviar, renomear) leva o foco ao primeiro campo, uma vez por abertura.
- `CabecalhoDePagina`: `SubtituloMobile` nulo repete o `Subtitulo` na barra do mobile; vazio quer dizer "sem subtítulo no mobile". Lista que mostra a contagem na barra passa a contagem ou vazio, nunca nulo.
- Na tela de Documentos comuns da Diretoria fica no máximo uma coisa aberta entre o envio (painel ou confirmação), o renomear e a confirmação de exclusão; cada abertura fecha as outras, e com um envio ou uma exclusão em curso nada se abre e os botões das linhas ficam desabilitados. Decidido na execução, sem quadro: rever se o usuário preferir o painel de envio aberto enquanto renomeia.
- Dois botões lado a lado numa célula de tabela são escritos colados na marcação: o espaço em branco entre eles é desenhado (4px) e desloca as colunas.
- Nos testes: texto com acento se confere decodificando o HTML na chamada, não dentro de `TelaDeTeste`; `BlocoDeVisao.De` recorta o bloco de uma visão; `DespachanteCombinado` responde o que o banco não produz por encomenda; `ComBarraSuperior` desenha um componente junto com o encaixe da barra superior (sem ele a barra do mobile não chega ao HTML do teste).
- Regras antigas do `app.css` estreitadas nesta fase, e o padrão para as próximas: `input[type="file"]:not([class*="aj-"])`; as duas regras de `.aj-etiqueta` só dentro de `.aj-app__conteudo:not(:has(> [class^="aj-"]))`, a mesma convenção da regra interina.

Publicado no design system em 2026-10-14, junto com o CSS da fase 9C (versão `1791995411-b333`), e copiado para o `componentes.css`. Fica como registro do que estava pendente. A conferência no navegador pediu três acréscimos ao `bundle.css`, e a publicação no artifact não pôde ser feita na sessão que executou a fase:

- `.aj-aviso__texto` e `.aj-painel h2` com `overflow-wrap:anywhere`. Sem isso, um nome de arquivo longo e sem espaços transborda a tela de 390px na confirmação de envio, na confirmação de exclusão e no título do renomear (na lista o nome já quebra).
- `:where(.aj-tabela a){color:var(--verde)}` e `:where(.aj-tabela a:hover){color:var(--tinta)}`. A cor do link que baixa o arquivo vem hoje do `:where(a)` do `app.css`; quando a 9G tirar a camada antiga, o link ficaria azul.
- `.aj-menu-acoes__item:disabled{opacity:.55;cursor:not-allowed}` e `.aj-menu-acoes__item:disabled:hover{background:transparent}`. O item desabilitado do menu de ações trava, mas com a aparência de habilitado.
- Os READMEs de Aviso, Painel, Tabela e Menu de ações ganham uma linha cada sobre isso.

Para decidir com o usuário:

- Teste automático de interação. O fluxo da tela da Diretoria (painel escondido durante a confirmação, cancelar, fechar ao enviar, uma coisa aberta por vez, foco ao abrir) só foi conferido no navegador: o desenhador estático dos testes não clica e a fase não podia trazer pacote novo. A revisão final pede que a cobertura exista antes de a 9D mexer em `GestaoDeDocumentos`. Dois caminhos, que se somam: tirar as regras de abertura da tela para uma classe pura testada, como `EnvioComConfirmacao`; e decidir se entra um pacote de teste de componentes (bUnit) ou um desenhador interativo próprio (um experimento da leva de correção mostrou que dá para clicar em teste sem pacote novo).

Para a abertura da 9C:

- `TelaDeTeste.RenderizarAsync` e `RenderizarComAsync` têm o mesmo corpo, salvo o despachante: juntar quando a navegação for registrada (seção 10).
- `MenuDeAcoes`: o foco só volta ao botão com Esc (fechando por um item, fica com o que a ação abre; fechando pelo fundo, se perde); sair com Tab deixa o menu aberto, e pelo teclado dá para abrir dois. — O fundo, na 9C; o resto, na 9G.

Para a 9D, quando a ficha do cliente migrar e a tela antiga de documentos sair:

- `GestaoDeDocumentos` tem cerca de vinte campos protegidos que as telas alteram, e a tela nova repete em `FecharEnvio` a limpeza que a base faz ao concluir um envio: um `DescartarEnvio()` protegido na base junta as duas. O `OnAfterRenderAsync` mora na base: quem o sobrescrever chama o da base. O ramo de confirmação de envio de `DocumentosDoCliente.razor` ficou sem quem o use.
- A terceira tela sobre essa base (documentos da ficha) é o terceiro uso de `Quantos`, `Endereco` e da tabela de documentos, repetidos hoje em `DocumentosParaBaixar` e `DocumentosComunsDaDiretoria`: extrair então (seção 4.4).

Para a 9G, junto da revisão de teclado e de leitor de tela:

- Depois de "Cancelar", "Salvar nome" e "Confirmar envio" o foco cai no `body`. Com a confirmação de envio à vista, "Enviar documento" pede o foco num campo escondido e nada acontece. — Feito na 9G: Cancelar devolve o foco a quem abriu; concluir leva o foco ao aviso de resultado; durante o envio o foco fica na confirmação; com ela à vista, "Enviar documento" leva o foco a ela. Resta um instante no `body` enquanto o "Salvar nome" desabilitado grava (D5, seção 15, "Da fase 9G").
- Em `/documentos-comuns` o foco no título se perde de vez em quando: a página tem um cabeçalho por ramo, e o título focado é trocado quando o "Carregando…" dá lugar à lista. — Feito na 9G pela rede de segurança, que leva o foco ao título visível quando o focado sai do DOM (seção 4.5).
- No Andamento, depois de "Anterior" ou "Próxima" a lista dá lugar a "Carregando…" e o botão focado some. — Feito na 9G: a página nova chega sem tirar a lista, e o botão sem ação leva `aria-disabled`.
- Comportamentos que a extração da lógica de documentos manteve como eram: "Salvar nome" sem guarda de clique duplo, resposta atrasada da consulta sem descarte, "Tentar de novo" sem estado de carregando, Enter nos campos dos painéis não envia. Durante o envio, a confirmação some e o painel volta à vista com "Enviando…" e o campo de arquivo habilitado. — Feito na 9G, os cinco: guarda de clique duplo, descarte por geração, "Carregando…" no "Tentar de novo", painéis como `form` (o Enter envia) e a confirmação à vista durante o envio.
- Os ajustes globais que ainda moram no `app.css` e alcançam as telas migradas: `box-sizing`, margem do `body`, cor e foco dos links. Passam ao design system quando a camada antiga sair. — Decidido na 9G: a base (`box-sizing`, `body`, `:focus-visible`, `h1:focus`) fica no `app.css`, como diz a seção 4.1; os dois `:where(a)` saíram, porque os links sem classe estão em `aj-tabela`, que tem cor própria, ou são o logotipo.

Observações de desenho, sem ação nesta fase:

- No quadro do mobile da Diretoria o botão de três pontos encolhe para 41,8px na linha de título longo; no sistema ele mantém os 46px do alvo de toque. Mantido o do sistema.
- O campo de arquivo mede 42,7px no desktop, ao lado do campo de texto de 40px (no tablet e no mobile os dois medem 50px). É a regra do próprio design system para o campo de arquivo.
- Os chips do Andamento rolam de lado no mobile quando os números crescem ("Em andamento · 15"): é o comportamento de `aj-chips`.
- `Aviso` com `Rotulo` e sem `Acoes` sairia como diálogo de alerta sem nome e sem foco. Nenhuma tela usa a combinação.

### Da fase 9C (emenda de 2026-10-12)

O que a fase entregou além das telas da seção 7.3, e que as próximas reutilizam:

- Peças compartilhadas: `CabecalhoDeDetalhe` (voltar acima do cabeçalho no desktop e no tablet, barra de voltar no mobile), os encaixes `Complemento` e `LinhaDoMobile` do `CabecalhoDePagina`, `Recados` (mensagem de uma tela para a seguinte, de escopo do circuito, lida uma vez), `ConfirmacaoDeInativacao`, `ResumoDeAuditoria`, `IdentificadorCurto`, `Contagem.ComInativos`. O `MenuDeAcoes` fechado pelo fundo devolve o foco ao botão.
- Lista que sobrevive à recarga (Áreas, Perfis, Usuários): só a falha da primeira consulta vai ao `EstadoDaLista` com "Tentar de novo"; a recarga que falha depois de uma ação mantém a lista e o subtítulo e troca o aviso por "{mensagem}, mas não foi possível atualizar a lista.". Falha de consulta que não é de acesso nunca aparece como "Acesso negado." nem como "não encontrado". O trio (`RecarregarAsync`, `_listaCarregada`, a conclusão da ação) está repetido em três telas, já com formatos diferentes: extrair antes de a 9D copiá-lo uma quarta vez.
- Formulário em tela própria (o primeiro de vários: cliente, processo, atividade, evento): o `EditForm` envolve só o painel; as barras de ação ficam fora dele (no desktop, sobre o fundo da página, como no quadro) e os botões de envio se associam pelo atributo `form`. A busca de um vínculo usa um formulário escondido próprio, também pelo atributo `form`, e o Enter na busca não envia o formulário principal. Gravação respondida com acesso negado encerra a tela. Salvar volta à lista com um recado. Identificador que não está na lista dá "não encontrado" com o voltar, sem formulário.
- Formulário de filtros sem botão de envio à vista (a linha do tablet e do mobile): um botão de envio escondido, `class="aj-so-leitor" hidden`, faz o Enter filtrar. `aj-botao` não serve, porque o `display` dele anula o `hidden`.
- Nos testes: `TelaDeTeste` tem um registro de serviços só, com um `NavigationManager` que não navega, e `RenderizarEAgirAsync`, que chama um método da tela por reflexão e desenha de novo — é como se testa, sem pacote novo, o que acontece depois de um clique (o teste quebra alto se o método mudar de nome).

Publicado no design system em 2026-10-14 (versão `1791995411-b333`), com os READMEs e as prévias, e copiado para o `componentes.css`; a suíte inteira rodou com ele antes da entrada na `main`. Fica como registro do que estava pendente (a conta dona do artifact só voltou na quarta-feira, 2026-10-14). O `bundle.css` pronto está fora do repositório, em `design-system-pendente-9c`, e já leva as três pendências da 9B. Até ele ser publicado e copiado, o `componentes.css` do código não tem as classes desta fase: as telas estão certas na marcação e nos testes, mas sem a aparência final, e o branch não entra na `main` antes disso. Além do bloco da fase (Tarefa 1 do plano), a conferência pediu:

- `.aj-barra-superior--voltar>h1` com `letter-spacing:normal`: o `h1 { letter-spacing }` do `app.css` (anotado na 9A) chegava ao título da barra de voltar.
- `.aj-campo-filtro` com `gap:0`: o `label { gap: 8px }` do `app.css` afastava o nome do campo do controle.
- `.aj-campo-filtro select{width:0;min-width:100%}`: a opção mais longa (um nome de 60 letras no filtro "Usuário") alargava o campo e desmontava a barra de filtros.
- `.aj-dados` com `word-break:normal`: o `td code { word-break: break-all }` do `app.css` partia as palavras dos dados da auditoria no meio.
- `overflow-wrap:anywhere` em `.aj-cartao__titulo`, `.aj-cartao__meta` e `.aj-tabela__secundaria`: um e-mail longo sem espaço passava do cartão a 390px (e o conteúdo rolava de lado) e, no desktop, empurrava a coluna "Ações" para fora da caixa da tabela.
- Os READMEs e as prévias dos componentes novos e dos que ganharam modificador (Tarefa 1, passo 4) ainda estão por escrever.

Feito na quarta, 2026-10-14: publicar, copiar para o `componentes.css` e rodar a suíte.

Para a 9G, junto da revisão de teclado e de leitor de tela:

- Auditoria: depois de "Filtrar" no painel o foco cai no `body` (o painel sai da página com o botão focado); "Tentar de novo" não recarrega a lista de usuários do filtro; enquanto a aba nova carrega, a paginação mostra os números da anterior. — Feito na 9G: o `PainelDeFiltros` devolve o foco ao botão; "Tentar de novo" refaz a carga inicial; a paginação some durante a carga e depois de uma falha.
- O erro de uma gravação aparece no topo do formulário sem foco nem rolagem: num formulário mais alto que a tela do celular ele fica fora da vista. — Feito na 9G: `Aviso.Revelar` nos oito formulários (seção 4.5).
- O Enter pelo botão de envio escondido só foi conferido no Chrome; conferir num WebKit. — Feito na 9G no Playwright WebKit do Windows (envia); o Safari de verdade não foi conferido.
- Formulário de usuário: uma carga lenta do usuário A pode sobrescrever o B se o identificador da rota mudar durante a espera (hoje nenhum link leva de uma edição a outra). Na lista, se a inativação do primeiro usuário falhar com a confirmação do segundo já aberta, o aviso de erro do primeiro aparece no topo ao lado dela. — Feito na 9G: a carga atrasada é descartada, e o resultado que chega com outra confirmação aberta é calado.
- Células sem classe dentro de `aj-tabela` herdam o `box-sizing` do `app.css` (sem efeito visível, porque a altura delas é automática): entra com os outros ajustes globais. — Decidido na 9G: a caixa de borda universal fica na base do `app.css`.

Observações de desenho, sem ação nesta fase:

- No quadro do mobile de Produtores o campo "Produtor" encolhe para 36,8px (tem `flex: 1 1 auto` numa coluna); o "Mês" ao lado tem os 42px do token. Mantido o token no sistema.
- O título do painel de produtividade encolhe ao texto no tablet e no mobile, porque o cabeçalho do painel é flex; não se vê diferença.
- As linhas da lista em painel usam algarismos tabulares na ajuda (regra anterior a esta fase); os quadros não. Sem efeito em "Ativa" e "Inativa".
- A Auditoria abre sem período preenchido, como antes; o quadro mostra um período de exemplo.

### Da fase 9D (emenda de 2026-10-14)

O que a fase entregou além das telas da seção 7.4, e que as próximas reutilizam:

- Peças compartilhadas: `CabecalhoDeFicha` (título, subtítulo, resumo do mobile e ações da ficha), `Abas` e `Aba` (as abas do mobile, com o painel de cada aba num invólucro `aj-aba-painel`), `RotuloDeCampo` com `Classe` (a posição do rótulo na grade), `ValorEmReais` com `[ValorEmReaisValido]` (valor brasileiro em texto, que recusa sem lançar), `TextosDoCliente`, `RotulosDeProcesso`, `TextosDeDocumento`.
- `GestaoDeDocumentos` passou a guardar a regra de uma coisa aberta por vez e o foco do painel aberto; `DescartarEnvio()` junta a limpeza do envio, como a 9B pedia. `DocumentosDaFicha` é o painel de documentos da ficha do cliente, com os chips por área e o acesso em opções marcáveis. A tela antiga `DocumentosDoCliente` saiu. `Endereco` foi extraído para `TextosDeDocumento`; `Quantos` e a tabela de documentos continuam com dois usos, sem extração. (Emenda da 9E: os documentos da atividade não usam `Quantos`, porque a contagem vai para a aba do mobile pelo `AoContar`, e a tabela deles tem outras colunas e ações; continuam dois usos.)
- Contadores no backend, dentro das listagens que já existiam (seção 6): o total do cadastro de clientes e os inativos, o total de processos sem filtro de situação, e os documentos por área com o mesmo predicado de acesso da listagem (a Produção conta só o que lê).
- Formulário em tela própria (cliente e processo, depois do usuário da 9C): depois de salvar com sucesso, o botão de envio fica travado até a navegação chegar (o `finally` só o libera se a tela não navegou). O mesmo conserto entrou no formulário de usuário. Na edição, a ficha é o "voltar"; no novo, a lista.
- Lista de escolhidos com busca de candidatos no mesmo pai: as chaves `@key` dos dois tipos de linha nunca podem coincidir (o candidato usa uma chave composta).
- Ficha que recebe o identificador pela rota recarrega quando ele muda na mesma instância (`_idCarregado`).
- Nos testes: entrou o bUnit 2.11.3, só no projeto de integração, com `TelaInterativa` (atores da Diretoria e da Produção) e o `DespachanteCombinado` com `Recebidas`, `Responde` com função e `RespondeDepois`. É como se prova o que acontece depois de um clique, inclusive o envio duplo; não substitui o navegador quando o defeito está na aplicação das diferenças ao DOM.

Decidido na execução, sem quadro ou contra o quadro, para o usuário rever:

- A ficha do cliente tem o painel Observações também no tablet (o quadro do tablet não tem; o desktop e o mobile têm). No tablet não há outro lugar para ler as observações.
- Na ficha do cliente, no desktop, o painel de dados vem primeiro na marcação e o CSS o põe à direita (`aj-lado-a-lado--painel-primeiro`): a ordem de leitura e de Tab é Dados, depois Processos, Documentos e Usuários.
- Nos formulários, perder o acesso no meio da tela mostra "Acesso negado.", como no formulário de usuário da 9C; as listas e as fichas continuam com "Seu acesso foi encerrado. Entre novamente.".
- A `textarea` de Observações do formulário de cliente tem 4 linhas em todas as visões; os quadros do tablet e do mobile usam 3.
- A grade folgada (`aj-grade-campos--tablet-folgada`) vale também no mobile; o nome ficou da primeira tela que a usou.

Para as próximas fases:

- ~~Os formulários não oferecem "Tentar de novo" quando a consulta inicial falha (o do usuário da 9C também não); as listas e as fichas oferecem. Extrair o estado de carregamento dos formulários antes de a 9E escrever o de atividade.~~ Feito na 9E: `CargaDoFormulario` e `EstadoDeCarga`, nos formulários de cliente, processo, usuário e atividade e nas telas de responsáveis, devolver e reabrir.
- No formulário de processo, o erro da busca de cliente aparece no aviso do topo, longe do campo. — Feito na 9G: o erro vai junto do campo, com `role="alert"`, `aria-invalid` e `aria-describedby`.
- Os formulários carregam em `OnInitializedAsync`: se a mesma instância passar de `/x/novo` para `/x/{id}/editar` (nenhum link leva a isso hoje), não recarregam. As fichas já usam `_idCarregado`. — Ficou: nenhum link leva ao caso.
- A lista das áreas de documento que cada perfil lê está em três lugares (o repositório, o falso dos testes de unidade e a regra do manipulador). — Ficou: fora do assunto da 9G.
- A suíte cobre pouco os caminhos de erro das telas novas (falha ao salvar, acesso negado ao salvar, não encontrado na edição, área inativa no seletor); o código os trata e a revisão os conferiu por leitura. — Ficou.

Para a 9G, junto da revisão de teclado e de leitor de tela:

- `MenuDeAcoes`: com o menu aberto, o Tab sai da lista e o menu continua aberto, e o Esc já não o fecha (o tratador está no invólucro do menu). Só o toque no fundo fecha. Mesma raiz da observação da 9B. — Feito na 9G: fecha quando o foco sai, com Esc em qualquer ponto dele e pelo fundo; um aberto por vez.
- As abas do mobile são botões em sequência, sem setas nem `tabindex` móvel de um `tablist`. — Feito na 9G: `tabindex` móvel, ←, →, Home, End e `aria-controls`.
- Regras do `app.css` que ainda alcançam as telas desta fase: `label { … }` muda cor, fonte e altura de linha do rótulo do campo com ícone e do campo compacto (sem efeito visível: o texto é só para leitor de tela ou o controle tem fonte própria); `button:disabled { opacity: .55 }` apaga a opção travada do tipo de cliente na edição (sem quadro da edição; apagada é o esperado para opção travada, e a regra passa ao design system quando a camada antiga sair). — Feito na 9G: as duas saíram; o design system ganhou `.aj-segmentado button:disabled` e `.aj-resultados__item:disabled`.

Observações de desenho, sem ação nesta fase:

- As ajudas das linhas em painel (data e tamanho dos documentos, o resumo da barra do mobile) usam algarismos tabulares; os quadros não. Mesma observação da 9C.
- Lista em painel com uma linha só: o sistema mantém o respiro de 8 a 10px embaixo da linha; o quadro tira o respiro quando a lista tem uma linha (com três, mantém). Mudar mexeria em listas de outras fases medidas sem divergência.
- No cabeçalho da ficha no tablet o bloco do título não estica (o cabeçalho é flex em linha); o texto ocupa o mesmo lugar.
- Na lista de clientes, a célula de ação da linha inativa também fica apagada (o quadro apaga só as de dado); a célula não tem texto próprio e o "Abrir" mede igual.
- Um nome de arquivo longo e sem espaços, na tabela de documentos da ficha do cliente no desktop, quebra em muitas linhas na coluna do nome (a coluna é estreita no lado a lado); as ações ficam à vista, sem rolar. Antes desta fase a tabela rolava de lado.

### Da fase 9E (emenda de 2026-10-15)

O que a fase entregou além das telas da seção 7.5, e que as próximas reutilizam:

- Estado de carregamento das telas de formulário: `CargaDoFormulario` e `EstadoDeCarga` ("Carregando…", "Acesso negado.", o não encontrado e o erro com "Tentar de novo"), nos quatro formulários (cliente, processo, usuário, atividade) e nas telas de responsáveis, devolver e reabrir.
- Contadores no backend (seção 6): `ListarAtividades` devolve `PaginaDeAtividades`, derivada de `Pagina<AtividadeResumo>`, com `NoEscopo` (o que o usuário pode listar, sem filtro) e `Atrasadas` (do mesmo escopo), contados por `IAtividadeRepositorio.ContarAsync`. A Diretoria conta o escritório inteiro; a Produção, as atividades em que é responsável.
- Parâmetros novos nas peças compartilhadas: `CabecalhoDePagina.Alerta` (a parte das atrasadas no subtítulo), `CabecalhoDeDetalhe.RotuloDoVoltarNoMobile`, `BarraDeVoltar.Acao` (o "Mais ações" na barra do mobile), `Paginacao.ComSetas`, `EtiquetaDeSituacao.NaTabela` (o ícone só na tabela; nos cartões e na ficha, o ponto).
- Peças novas: `AvatarDePessoa` (sigla e cor pelo nome), `TextosDeAtividade`, `RotulosDeAtividade.SituacaoNoPlural`, `CabecalhoDaAtividade` (a linha de situação e a faixa de ações), `MotivoComResponsaveis` (uma tela para as rotas de devolver e de reabrir, com páginas finas), `OrigemDaAtividade` (de onde a ficha foi aberta, repassada às telas de ação e devolvida no voltar, no Cancelar e depois de salvar).
- `GestaoDeDocumentos` ganhou os ganchos protegidos de listar e de enviar, com o comportamento de antes como padrão; os documentos da atividade são a terceira herdeira e contam para a aba do mobile pelo `AoContar` (como os comentários).
- A classe de visibilidade `aj-desktop-mobile` no design system (o comentário novo é o mesmo no desktop e no mobile e outro no tablet).
- A trava `Toda_tela_de_atividade_esta_na_trava`: toda tela da pasta de atividades está na lista das migradas.

Decidido na execução, sem quadro ou contra o quadro, para o usuário rever:

- Vindo de Produtores, o voltar da ficha leva ao produtor escolhido, mas o mês do painel volta ao corrente (o mês não está no endereço). As telas de ação abertas da ficha (editar, responsáveis, trocar revisor, devolver, reabrir) levam a origem e voltam com ela; Excluir volta sempre a `/atividades` com "Atividade excluída.", também vindo de Produtores, que não lê recados.
- "Trocar revisor" só aparece quando a atividade não permite redefinir os responsáveis (quando permite, o painel "Revisor" da tela de responsáveis já troca o revisor); no mobile entra também no "Mais ações", senão a função some.
- A Diretoria vê no subtítulo e nos chips o escritório inteiro mesmo com um responsável escolhido no filtro.
- A cor do avatar sai de uma conta estável sobre o nome e não reproduz a do quadro ("Beatriz Lins" sai lilás; no quadro é azul).
- A coluna Complexidade vem depois de Entrega na tabela; a linha Complexidade vem depois de Área nos Detalhes do desktop e por último nos Dados do tablet e do mobile. A tabela tem 980px de largura mínima e rola de lado com as nove colunas.
- As etiquetas de situação com ícone (o visto da Concluída, o relógio da Atrasada) só na tabela; nos cartões e na ficha, com ponto.
- Os documentos do tablet e do mobile têm o nome como link de download e as ações num "⋯"; no desktop, os botões do quadro. "Enviar documento" (sem quadro) é botão secundário no título do painel nas três visões, e o painel de envio fecha depois de um envio bem-sucedido.
- O comentário novo do tablet é em linha, com o botão de ícone "Enviar comentário", como o quadro; no desktop e no mobile, o rótulo visível, a caixa de três linhas e "Comentar".
- Concluir minha parte, Aprovar e Excluir pedem confirmação num aviso de atenção no topo da ficha, com o foco nele.
- O terceiro estado das telas de ação (a atividade já não permite a ação) é um aviso de atenção, sem formulário e sem barras. Quando a atividade carregou, o voltar mostra o título dela e leva à ficha com a origem; o plano dizia "Atividade", que fica só antes da carga e na Produção.
- O modo do revisor da tela de responsáveis tem o painel médio, o título da atividade no subtítulo e "Salvar" no mobile; a opção de responsável traz o nome do perfil entre parênteses (com os perfis semeados, `(Diretor)`; o quadro mostra `(Diretoria)`).
- Devolver e reabrir: o `form` é o painel; os erros de motivo e de responsáveis aparecem juntos, cada um no seu campo; o motivo vai aparado e os responsáveis na ordem da atividade.
- Divergências dos quadros antigos mantidas para todas as telas ficarem iguais entre si: o `Aviso` com o ícone solto (o quadro desenha um selo), o `h1` da barra de voltar do mobile, o voltar do tablet e o respiro de cima pelas variáveis, a barra fixa do mobile com 46px (o quadro tem 50), o hover do perigo, o `letter-spacing` do título, o nome no título da ficha do mobile com 26px (o quadro tem 27), o respiro do "Buscar", a sombra do primário no tablet e também na barra fixa do mobile, o vão de 12px entre os cartões do mobile.
- As outras decisões de redação estão no fim do plano da fase (`docs/superpowers/plans/2026-10-14-fase-9e-atividades.md`, "Decisões tomadas na redação").

A conferência no navegador: 9 dos 15 quadros × visões sem divergência (formulário, responsáveis e devolver nas três). Nos outros seis, diferenças de CSS de 1 a 3px. Duas foram corrigidas no design system (`1792048576-7268`): na tabela larga, o título e a coluna secundária passaram de `overflow-wrap:anywhere` a `break-word` (com `anywhere`, o mínimo da coluna caía e os títulos quebravam em três linhas, com as linhas da tabela 22px mais altas que no quadro); e o texto do comentário no tablet perdeu o limite de 68 caracteres (o quadro não tem). Ficaram, com o motivo:

- O `h2` dos painéis com altura de linha 1,4 (os quadros antigos usam 1,5): cada painel fica 1,6px mais baixo; a classe é a das fases anteriores.
- Algarismos tabulares em `.aj-subtitulo`, `.aj-detalhes dd` e na data do comentário (os quadros só na entrega): até 4px num número; mesma observação da 9C e da 9D.
- O contador da aba no mobile com 15,6px de altura (o quadro tem 18): a classe da 9D.

Para as próximas fases:

- As telas de ação (formulário, responsáveis, devolver e reabrir) repetem a carga (perfil, `ObterAtividade`, acesso negado, não encontrada, falha) e o trio voltar, origem e rótulo do voltar no mobile. O `CargaDoFormulario` tira a parte da marcação e o `OrigemDaAtividade` junta a origem; extrair o resto no quarto uso (a Agenda, na 9F, se repetir). Na 9F a agenda não repetiu o trio: a ficha e o formulário do evento recebem `visao` e `data` e devolvem a origem por `PeriodoDaAgenda.ConsultaDeOrigem`, e o formulário carrega pelo `CargaDoFormulario`. Continua sem quarto uso. — Ficou na 9G.
- Conflito ao salvar (edição e responsáveis): só o aviso com a mensagem do servidor, que pede recarregar; um novo "Salvar" conflita de novo até a tela ser recarregada. O formulário do evento da 9F faz o mesmo (decisão do plano), com o formulário mantido. — Ficou na 9G.
- A suíte não cobre o salvar da atividade com processo escolhido, nem a remoção do processo e do cliente escolhidos, nem o aviso do cliente que saiu do processo; nem o acesso negado no envio das telas de ação, de comentários e de documentos. O código os trata e a revisão os conferiu por leitura; o envio com processo foi exercido no navegador. — Ficou na 9G.
- O "Tentar de novo" do `CargaDoFormulario` não tem guarda contra dois cliques seguidos; nos herdeiros de `GestaoDeDocumentos` da 9D, uma mensagem pode ficar ao lado do "Seu acesso foi encerrado.". Continua; o formulário do evento da 9F também usa o `CargaDoFormulario`. — Feito na 9G: guarda de clique duplo e "Carregando…" no `CargaDoFormulario`; o acesso encerrado apaga a mensagem dos documentos e impede as seguintes.
- ~~O que a Agenda ainda usa do `app.css`: `.aviso` e `p.aviso` (também por expressão), `.ficha`, `.formulario`, `.filtros`, `.grade`, `.acoes`, `.texto-livre`, `.dia`, `td.horario`, e `fieldset` e `legend` no formulário de evento. O formulário de evento lê `ListarAtividades` como `Pagina<AtividadeResumo>` e continua compilando com `PaginaDeAtividades`.~~ Feito na 9F: a agenda, a ficha e o formulário do evento passaram às classes `aj-`, e a Tarefa 10 apagou do `app.css` essas regras, as outras que só a agenda usava e a regra interina. O formulário novo lê da busca de atividade só `Itens` e `Total`. `fieldset` e `legend` continuam como regras de elemento (seção "Da fase 9F", para a 9G).
- Os formulários continuam carregando em `OnInitializedAsync` (sem mudança da 9D sobre `/x/novo` → `/x/{id}/editar`). O formulário do evento da 9F também; nenhum caminho leva de um formulário de evento a outro. — Ficou na 9G.

Para a 9G, junto da revisão de teclado e de leitor de tela:

- O "Mais ações" da ficha no mobile e o "⋯" dos documentos têm o mesmo problema do `MenuDeAcoes` da 9D: o Tab depois do último item tira o foco do menu e o menu continua aberto, o Esc com o foco fora não fecha, e o fundo cobre a tela até um toque. — Feito na 9G, no próprio `MenuDeAcoes`.
- ~~Regras do `app.css` já sem uso que a 9E não apagou: `strong.erro`, `.validation-message` e a parte `.cartao` do seletor `.cartao, form.formulario:not(.filtros)`.~~ Feito na 9F, na Tarefa 10.
- Regras de elemento que alcançam as telas desta fase, medidas pela conferência: `label { … }` pinta o rótulo do campo com ícone e do campo compacto da lista e do formulário, sem efeito visível; `fieldset` dá o raio ao `aj-conjunto`, sem efeito (borda 0). `label:has(> input[type="checkbox"])`, `legend`, `input[type="checkbox"]`, `input.invalid`/`textarea.invalid` sobre `aj-campo` e `button:disabled` não mudaram nada. — Feito na 9G: todas saíram.
- No desktop, o modo do revisor de `/responsaveis` tem o painel médio e a barra de ações de largura inteira; no devolver e no reabrir a barra acompanha o painel (`aj-acoes--medio`). — Feito na 9G: a barra da troca de revisor ganhou `aj-acoes--medio` (680px) e continua fixa ao pé.

Observações de desenho, sem ação nesta fase:

- A 390px a aba "Histórico" da ficha fica fora da tela e só aparece rolando a fileira de abas (com "Documentos 3", a fileira não cabe). O quadro tem a mesma fileira.
- No tablet, o nome longo de documento corta com reticências; no desktop e no mobile, quebra.
- Na conferência, um clique em "Escolher" da busca de processo não teve efeito uma vez em sete, com três circuitos abrindo ao mesmo tempo; não se reproduziu isolado. Registro como possível corrida sob carga.
- Artefatos do quadro, não do sistema, para quem redesenhar: no `FichaMobile` a fileira de abas encolhe a 1px (o `main` em coluna com altura fixa e a fileira com `overflow-x:auto` perde o mínimo automático) e os painéis sobem sobre ela; no `FichaTablet` o comentário novo tem 7px a mais pela linha de base da `textarea` dentro do `label`.

### Da fase 9F (emenda de 2026-10-15)

O que a fase entregou além das telas da seção 7.6, e que as próximas reutilizam:

- Lógica de calendário pura e testada: `PeriodoDaAgenda` (o período no endereço, com `?visao=` e `?data=` lidos com tolerância, o intervalo da consulta, as setas, `Consulta` e `ConsultaDeOrigem`, por onde a ficha e o formulário devolvem a origem), `ReguaDoDia` (a régua do Dia, com a extensão e os grupos de simultâneos) e `RotulosDeAgenda` (os textos da agenda, com as três redações do aviso de conflito, `EventosDoDia`, `NumeroDoDia` e `UsuarioNaoEncontrado`).
- Componentes da agenda: `CartaoDeEvento` (uma forma por largura), `SecaoDoDia`, `ListaDoDia`, `PontosDoDia`, `FaixaDeDias`, `GradeCompactaDoMes` e as três visões, `SemanaDaAgenda`, `DiaDaAgenda` e `MesDaAgenda`, cada uma com a marcação das três larguras sobre os mesmos eventos. `AvisoDeEventoSalvo` mostra "Evento salvo." ou as três redações do conflito, cada uma no seu invólucro de visão.
- `Recados` leva as sobreposições: o formulário as deixa, e a agenda (evento novo) ou a ficha (edição) as mostra uma vez.
- Parâmetros novos nas peças compartilhadas: `CabecalhoDePagina.AcaoDoMobile` (botão entre o título e o avatar da barra do mobile) e `SubtituloSoNoDesktop`; `CabecalhoDeDetalhe.IconeDoVoltarNoMobile` e `SubtituloSoNoDesktop`; `BarraDeVoltar.Icone`; `EstadoDaLista.Icone` e `Dica`; `Aviso.Frase` (`aj-aviso--frase`, só no aviso sem ações); `RotuloDeCampo.Complemento` (o `(opcional)` num `small`, junto do texto num `span`); o ícone `filtrar`.
- `AvatarDePessoa` subiu para `Components/Layout` e ganhou `Classes(nome, proprio)`, que devolve a classe inteira: o avatar do próprio usuário sai dourado (o `aj-avatar` sem modificador, como o do menu); os outros, com a cor da 9E.
- A régua posiciona os cartões por classes de quartos de hora numa grade CSS, geradas por roteiro e conferidas por contagem (`aj-regua__grade--horas-{n}`, `aj-regua__evento--inicio-{q}`, `--quartos-{q}` e `--parte-{k}-de-{n}`), sem atributo `style`. É o modelo para outra posição calculada.
- Campo que busca pelo Enter sem botão de envio tem um formulário escondido por cópia (seção 11, "Como a fase 9F fez a conferência").
- A trava `Toda_tela_da_agenda_esta_na_trava`: todo `Components/Agenda/*.razor` e as páginas `Agenda*` e `Evento*` estão na lista das migradas.
- A camada antiga saiu: os componentes `FichaDoEvento` e `FormularioDeEvento` e o `EventoGravado`; do `app.css`, as regras que só a agenda usava, a regra interina das telas não migradas e as duas de etiqueta que dependiam dela. O `app.css` ficou com a base e as regras de elemento que saem na 9G.

Decidido na execução, sem quadro ou contra o quadro, para o usuário rever:

- A Semana do desktop tem sempre sete colunas, como a seção 7.6 pede; o quadro usa `auto-fit` (cinco colunas a 1200px com o menu aberto). Os cartões ficam empilhados na ordem de início, sem régua, como no quadro.
- A régua do Dia vai das 08:00 às 19:00 e se estende até a hora cheia do primeiro início e a seguinte ao último fim (seção 8); evento de outro dia entra recortado ao dia (a régua vai então a 00:00 ou a 24:00). O início arredonda para baixo e o fim para cima, com no mínimo dois quartos (24px). Eventos que se sobrepõem dividem a largura em até quatro colunas, a 8px um do outro; do quinto em diante, o evento divide a última coluna. As linhas das horas são o fundo da grade, sem elemento por hora.
- O mobile não tem "Hoje" (o quadro não tem; abrir a agenda pelo menu cai em hoje). No Dia e na Semana, as setas andam a faixa de dias uma semana, mantendo o dia da semana escolhido.
- O Mês mostra até três eventos por célula e `+N`, que leva ao Dia daquela data; o tipo do item vai em texto para leitor de tela; no tablet a grade não tem largura mínima. Na grade compacta do mobile, o dia de fora do mês continua tocável.
- No Mês do mobile, a grade compacta fica no topo do conteúdo e rola com a lista do dia (Ruling 13); na barra ficam o segmentado e a linha do mês. Na barra, que não rola, ela comia a altura da tela e deixava a lista sem espaço num celular baixo. É desenho novo (seção 9), sem quadro. Enquanto o mês novo carrega e na falha, a grade some junto com a lista (Ruling 16).
- A consulta lê o filtro aplicado, gravado só no "Filtrar", da barra do desktop ou do painel (Ruling 14). Fechar o painel sem "Filtrar" volta os campos ao filtro aplicado (abrir também); "Limpar" volta os campos a "Todos" sem consultar. A página antiga aplicava os campos na troca de período seguinte, e com o painel fechado nada mostrava o filtro valendo. No desktop, um campo mudado sem "Filtrar" continua mostrando a escolha depois da troca de período, com a lista pelo filtro aplicado (Ruling 15): é o comportamento comum de formulário com o botão à vista. Os filtros não vão para o endereço e sobrevivem à troca de período.
- O avatar do próprio usuário é dourado nos participantes da ficha e nos chips do formulário (os quadros); `(você)` vence `(inativo)`. Participante sem nome aparece como "Usuário não encontrado", sem `(inativo)` (antes saía com).
- Exclusão recusada porque o evento sumiu mostra só "Evento não encontrado."; as outras recusas, a mensagem do servidor, com "Não foi possível excluir o evento." de reserva, por analogia com "Não foi possível excluir a atividade." da 9E (Ruling 9).
- As setas levam o `aria-label` dos quadros, sem verbo ("Período anterior", "Semana anterior"): o rótulo diz o destino (Ruling 7).
- Depois de salvar, o evento novo volta à agenda na visão de origem e na data do início do evento; a edição volta à ficha com a origem. Dois casos para o usuário decidir: o evento novo começa em hoje, não na data à vista na agenda; depois de editar mudando a data, a ficha volta à agenda no período antigo, onde o evento já não aparece.
- Formulário: o tipo é `select` no desktop e ladrilhos no tablet e no mobile, e o evento novo começa em "Reunião"; a duração no tablet e no mobile é um `select` com as quatro opções do quadro e "Outra duração", que mostra o campo de minutos; o vínculo Atividade existe também no tablet e no mobile (os quadros não o têm); processo e atividade escolhidos não têm busca de troca (remove-se e a busca volta); "Quem cria o evento sempre participa." só no evento novo; "Cancelar" secundário no tablet. O processo não aparece nos cartões (nenhum quadro o mostra; fica na ficha).
- `?visao` só vale em minúsculas e sem acento (`dia`, `semana`, `mes`). As setas não travam em 2000–2100: um endereço fora da faixa volta a hoje.
- Divergências dos quadros antigos mantidas para todas as telas ficarem iguais entre si: o `letter-spacing` do título, a sombra do primário no tablet e na barra fixa, a sombra da opção escolhida do segmentado no desktop, o hover do "Filtros" do tablet; no formulário, o aviso, o voltar do tablet, o vão e o raio dos campos, os painéis do tablet com o respiro comum, as barras com 46px e a busca de vínculo como a da 9E. A cor dos avatares segue a conta da 9E (no formulário, "BL" sai lilás, "CP" verde e "RT" azul; no quadro, azul, âmbar e verde).
- As outras decisões de redação estão no fim do plano da fase (`docs/superpowers/plans/2026-10-15-fase-9f-agenda.md`, "Decisões tomadas na redação").

A conferência no navegador: 9 dos 12 quadros × visões sem divergência (o `AgendaA` na Semana, no Dia, no Mês e com o menu escondido, o `AgendaTablet`, o `AgendaMobile` e as três visões da ficha). Os três do formulário (`NovoEvento*`) tiveram só diferenças de CSS, e duas foram corrigidas. O "(opcional)" da Descrição ia para uma linha própria, porque o `.aj-rotulo` é coluna e o `small` virava item dela (o painel Evento ficava ~19px mais alto): o `RotuloDeCampo` passou a envolver o texto e o complemento num `span`, como o README do Campo manda (Ruling 17). A ajuda em parágrafo dentro de painel saía com `--tinta-corpo` e 68 caracteres de largura, porque `.aj-painel p` vencia `.aj-ajuda`: o design system ganhou `.aj-painel p.aj-ajuda{max-width:none;color:var(--tinta-suave)}` (`1792101692-3ba2`; Ruling 18). Ficaram, com o motivo (Ruling 19):

- A altura de linha do `aj-rotulo` 1,4 (os quadros têm 1,5: 1,35px por rótulo) e o `h2` do painel com 22,4px (24 nos quadros): as classes, como na 9E.
- O respiro dos campos no tablet e no mobile (14px; os quadros têm 12, e a Descrição mede 92px contra 98): os quadros antigos do tablet e do mobile divergem do próprio desktop, que tem os 14px.
- Cliente e Processo empilhados no tablet: o painel Vínculos passa de 157 a 240px e tudo abaixo desce ~83px. Nenhuma classe cobre a grade só no tablet (decisão 5 da redação).
- No desktop, a barra de ações do formulário (sticky), rolada até o fim, começa 2px acima do fim do último painel: a construção é a do quadro.
- O aviso de conflito diz "das 11:00 às 11:30" onde o quadro diz "das 10:00 às 11:30": o servidor recorta a sobreposição ao intervalo do evento salvo (`SalvarEvento.RecortarEFundir`), comportamento anterior à fase.

O resto da lista passou: o Enter das três buscas (nenhuma salva), os filtros como as Rulings 14 e 15, o Mês do mobile nas três alturas, o aviso nas três redações (na agenda e na ficha), o clique duplo (pela contagem), os casos de borda, as larguras 760, 761, 1199 e 1200 e a Produção só com os seus eventos e o filtro Tipo.

Para as próximas fases (a 9G é a última):

- Código de produção que só os testes usam: `RotulosDeAgenda.MensagemDeSalvo` e `Sobreposicao`, e `AvatarDePessoa.Classe(nome, proprio)`, substituído por `Classes`. Para tirar, os testes são reescritos sobre as redações novas, não apagados. — Feito na 9G.
- `AvisoDeEventoSalvo` escreve "Evento salvo." à mão no ramo com conflito e ignora o `Recado.Texto` (só o salvar produz sobreposição). — Feito na 9G: o destaque é o `Recado.Texto`.
- O rótulo do `+N` do Mês é o único texto da agenda fora de `RotulosDeAgenda`, com `InvariantCulture` e o plural à mão; "Cliente indisponível" e "Processo indisponível" são literais repetidos na ficha e no formulário. — Feito na 9G: os três foram para `RotulosDeAgenda`.
- A agenda repete a marcação do erro com "Tentar de novo" que o `EstadoDaLista` emite (a ficha usa o componente). — Feito na 9G: a agenda usa o `EstadoDaLista`.
- No caminho sem `visao`, a página põe invólucros de visão em volta da Semana e do Dia, que têm os seus; a regra de `flex-shrink:0` dos filhos de invólucro não alcança os netos. Sem efeito hoje (nenhum neto nesse caminho tem `overflow` diferente de `visible`); fica o registro de fragilidade. — Ficou na 9G.
- A trava da agenda não é recursiva, como a das atividades. — Feito na 9G: uma trava única e recursiva para todo `.razor` da Web, com a lista da infraestrutura.
- Atividades e Auditoria seguem o padrão que a agenda deixou: o que está nos campos de filtro vale na consulta seguinte, mesmo sem "Filtrar". Decidir se passam ao filtro aplicado da Ruling 14. — Decidido na 9G: passam (lista de decisões para rever, seção 15, "Da fase 9G").

Para a 9G, junto da revisão de teclado e de leitor de tela:

- A troca de período não leva o foco ao título nem anuncia o período novo: o `FocoNoTitulo` só age quando muda a página, o foco fica na seta (o que serve ao clique repetido) e o subtítulo não tem região viva. Enter num dia de fora do mês na grade compacta leva ao outro mês e o foco cai no `body` (a grade some durante a carga). Esc não fecha o painel de filtros, que fecha pelo mesmo botão. (Ruling 20.) — Decidido e feito na 9G: o foco fica na seta e o período novo é anunciado numa região só para leitor (`role="status"`); a grade compacta fica à vista durante a carga; Esc fecha o painel.
- O foco também cai no `body` depois do `+N` do Mês (a grade dá lugar ao Dia) e do "Filtrar" do painel (o painel fecha); no painel, o esperado é devolvê-lo ao botão que o abriu. — Feito na 9G: o `+N` leva o foco ao título do Dia; o "Filtrar" devolve o foco ao botão.
- `aria-controls="filtros-da-agenda"` aponta para um id que não existe com o painel fechado (o mesmo da Auditoria); o grupo das setas e do "Hoje" não tem `role="group"` com rótulo, ao contrário do segmentado; o erro do grupo de participantes não tem `aria-invalid` nem `aria-describedby` (a seção 8 pede `aria-invalid`). No mobile, o botão flutuante "Novo evento" vem antes dos cartões do dia na ordem de tabulação. — Feito na 9G: o painel fica no DOM com `hidden`; o grupo leva `role="group"` e `aria-label="Período"`; o grupo de participantes leva `aria-invalid` e `aria-describedby`; o botão flutuante vem depois do conteúdo, nas seis telas que o têm.
- A regra `label:has(> input[type="checkbox"]), label:has(> input[type="radio"])` do `app.css`: os ladrilhos do tipo e os chips de pessoa usam seletores de duas classes para vencê-la. É a próxima a sair; com ela, os seletores de duas classes deixam de ser necessários. — Feito na 9G: a regra saiu. Os seletores de duas classes ficaram: com uma classe só, quem decide passa a ser a ordem do arquivo, com variantes por consulta e por `data-visao`, e a reescrita teria risco sem ganho.
- Regras de elemento que ficaram no `app.css` (lista na Tarefa 10 do plano da fase): `box-sizing`, `body`, foco, links, `h1`–`h3`, `p`, `label`, `button:disabled`, `.aj-campo-icone input:focus…`, `input.invalid…`, `input[type="checkbox"]…`, `label:has(> input…)`, `fieldset`, `legend` e `td code`. Os `h2` da agenda levam o `font` inteiro e `margin:0` por causa da regra `h2`. Medidas pela conferência: `label { … }` pinta o `aj-campo-filtro` da agenda e o `aj-campo-icone` do formulário, e `fieldset` dá o raio ao `aj-conjunto`, sem efeito visível; `input[type="checkbox"]` zera a margem das caixas de marcar dos participantes do desktop (`aj-opcao__caixa`): sem o `app.css`, elas ganham 3px em cima e embaixo, e o quadro tem `margin:0`. O design system precisa da margem zero antes de a regra sair. — Feito na 9G: todas as regras de elemento saíram e o `app.css` ficou só com a base (seção 4.1). A nota sobre a margem das caixas estava trocada (correção na seção 15, "Da fase 9G").
- `MenuDeAcoes`: o Tab e o Esc anotados na 9D e na 9E continuam como estão; a agenda não usa o menu. — Feito na 9G.

Observações de desenho, sem ação nesta fase:

- Processo e atividade escolhidos no formulário, sem avatar: o nome começa a 9px da borda (8px de respiro e 1px de borda); o do cliente, depois do avatar, a 53px no desktop, 49 no tablet e 51 no mobile. As três caixas têm a mesma altura.
- O nome acessível do item do Mês é "Reunião, 10:00 Alinhamento com o cliente", com espaço entre a hora e o título; no dia seguinte de um evento de dois dias, sem a hora ("Outro, Plantão de virada").
- Uma cadeia de três simultâneos (09:00–10:30, 10:00–11:30, 11:00–12:00) fica em duas colunas: o terceiro volta à primeira.
- A 375×667 e a 360×640, a lista do dia do Mês do mobile começa abaixo da dobra; a rolagem chega a ela.
- Nenhum quadro desenha indicação de filtro ativo nos botões "Filtros" e "Filtrar agenda"; com o painel fechado, só a lista mostra o filtro aplicado.
- Abrir a agenda grava duas consultas (a pré-renderização e a interativa), como nas outras telas.

### Da fase 9G (emenda de 2026-10-16)

A 9G é a última fase da migração. Commits `ba357fc..9bb6299`, sobre a `main` local com as fases 9A a 9F (`8d5dd3a`). A suíte inteira passou em `ab8100f` (unidade 936 e integração 1415, sem falha; a linha de base era 910 e 1245) e, no código final (`9bb6299`), unidade 936 e integração 1426, sem falha.

O que a fase entregou:

- Design system (`1792109485-7666`): opções travadas do segmentado e dos resultados de busca esmaecidas, sem cursor de clique e sem o hover; `margin:0` nas caixas escondidas do ladrilho e do chip de pessoa; o "Mais" aberto aceso pelo `aria-expanded`, e os destinos sem o desenho de ativo enquanto o menu está aberto; o nome do usuário numa linha com reticências; a paginação com setas sem ação por `aria-disabled`; o título do Dia focado sem contorno.
- No mesmo design system: saíram os remendos que existiam por causa do `app.css` (`letter-spacing:normal` da barra de voltar, `gap:0` do `aj-campo-filtro`, `word-break:normal` do `aj-dados`, `.aj-opcao.aj-opcao--filtro`). As consultas de mídia ficaram sem buraco entre as visões (`max-width:1199.98px` e `760.98px`, 54 das 64). O navegador mínimo foi registrado no `responsividade.md`, e os READMEs ganharam o teclado do menu de ações, das abas e do painel de filtros. Depois da conferência, a reserva de rolagem das barras fixas (`1792133242-6f2d` e `1792136370-92aa`). A cópia do canvas foi sincronizada a cada publicação.
- O `app.css` só com a base, com a trava `AppCssTests`. O `ReconnectModal.razor.css` passou a declarar a própria caixa de borda. Uma trava única e recursiva: todo `.razor` da Web está entre as telas migradas ou numa lista fechada de infraestrutura, e as classes do framework só valem em `App.razor` e `ReconnectModal.razor`.
- O serviço `Foco`, a rede de segurança, a rolagem zerada por página, `Aviso.Revelar` e o foco de volta a quem abriu (seção 4.5).
- `MenuDeAcoes`: Esc em qualquer ponto dele, fechar quando o foco sai, um aberto por vez, foco de volta ao botão. Uma guarda de ponteiro ignora o `focusout` sem destino durante um clique dentro do menu (Safari e Firefox do macOS tiram o foco antes do clique).
- `Abas`: `tablist` com `tabindex` móvel, ←, →, Home e End, `aria-controls` para os painéis, e a fileira rolada até a aba focada.
- Layout: o menu "Mais" com o resto da coluna `inert`, foco ao primeiro destino, Esc e foco de volta; "Exibir/Esconder menu" com o foco no par visível; o ator da pré-renderização no circuito, sem piscar; o botão flutuante no encaixe `rodape-do-conteudo`; o nome inteiro no `title` do bloco do usuário.
- `PainelDeFiltros`, compartilhado por Agenda, Atividades e Auditoria. Atividades e Auditoria passaram ao filtro aplicado. Na Auditoria, "Tentar de novo" refaz a carga inicial, e a paginação é só a da consulta respondida.
- Documentos (`GestaoDeDocumentos` e as três herdeiras): foco de volta e aviso revelado; painéis como `form`, com o Enter enviando; guarda de clique duplo no "Salvar nome"; descarte da resposta atrasada por geração; "Carregando…" no "Tentar de novo"; a confirmação à vista e com o foco durante o envio; o acesso encerrado apaga a mensagem.
- Confirmações devolvem o foco ao botão que as abriu, e o resultado se revela (Usuários, fichas, Áreas, Perfis, ficha do processo, comentários).
- Formulários: o erro de gravação se revela nos oito; os grupos inválidos levam `aria-invalid` e `aria-describedby`; o erro da busca de cliente do processo vai junto do campo; o `CargaDoFormulario` tem guarda de clique duplo e "Carregando…".
- Listas: paginação por `aria-disabled`; o Andamento mantém a lista durante a troca de página; o formulário de usuário descarta a carga atrasada de outro usuário; a lista de usuários cala o resultado que chega com outra confirmação aberta; a barra da troca de revisor com `aj-acoes--medio`.
- Agenda: o período anunciado numa região só para leitor; o grupo "Período"; a grade compacta à vista durante a carga do Mês; o erro pelo `EstadoDaLista`; o "+N" com o foco no título do Dia; as limpezas F1 a F4 (código só dos testes, `Recado.Texto`, textos em `RotulosDeAgenda`).
- A faixa de erro do Blazor com `role="alert"`.

Decisões da fase (do plano, `docs/superpowers/plans/2026-10-15-fase-9g-limpeza.md`, "Decisões deste plano", e as do ledger):

- A base fica no `app.css`, como diz a seção 4.1: `box-sizing`, `body`, `:focus-visible` e `h1:focus`. Saíram `html,body{height:100%}`, os dois `:where(a)` e todas as regras de elemento.
- Os seletores de duas classes do ladrilho e do chip de pessoa ficaram; o `margin:0` e o `font` dos `h2` com classe também (corrigem o padrão do navegador).
- Consultas de mídia com fronteira fracionária (`1199.98px`, `760.98px`), sem a sintaxe de intervalo, para não subir o navegador mínimo.
- Nome longo: uma linha com reticências e o nome no `title`.
- "Mais": `aria-current` só pela rota; com o menu aberto, aceso pelo `aria-expanded`, e o resto da coluna `inert`.
- Piscar ao conectar: resolvido só no layout. O "Carregando…" das páginas ficou.
- Rolagem: o `main` volta ao topo quando muda a página, não quando só a consulta muda.
- Rede de segurança no módulo de foco, como reserva; os destinos melhores continuam por tela.
- Ao fechar, o foco volta a quem abriu; ao concluir, vai ao aviso que se revela.
- Menu de ações continua "disclosure" (sem `role="menu"` nem setas). Os painéis das abas não levam `role="tabpanel"`, porque no tablet e no desktop aparecem sem abas.
- Filtro aplicado em Atividades e Auditoria, como a agenda (Ruling 14 da 9F).
- Botão flutuante depois do conteúdo nas seis telas com ação no mobile.
- Agenda: o foco fica na seta e o período é anunciado; o grupo das setas se chama "Período".
- Limpezas de código que entraram: F1 a F4, F6, C3, C4, C7, C8, E4, E10, B9, B10, D2, F14 e X5 (os IDs são os do levantamento citado no plano). Ficaram fora: o trio de recarga sem quarto uso, os formulários pela rota, a cobertura dos caminhos de erro, os invólucros duplicados da agenda e o conflito que pede recarregar.
- WebKit: o do Playwright, no Windows; o resto fica como não conferido.
- O teste que confere que toda tela abre o conteúdo com um bloco `aj-` ficou: protege a margem e o ritmo do `main`.
- Os passos das tarefas foram escritos sem compilar; o implementador ajustou o mínimo, e o revisor de cada tarefa conferiu os desvios. As tarefas correram uma por vez, de 1 a 11, e a revisão final do branch veio antes da conferência no navegador, como na 9F.
- A falha das buscas nos formulários de atividade e de evento continua indo ao aviso do topo, que se revela e tira o foco do campo de busca (decisão do plano: todos os erros do topo se revelam). Está na lista para rever, abaixo.
- D1, D3, D4, D6 e D7 da conferência foram corrigidos numa onda, com testes; D5 e D8 ficaram registrados. No D4, a região do período fica vazia na troca que vem do "+N", em vez de manter o texto anterior: com o texto antigo, voltar ao Mês não seria anunciado.
- O "Remover atividade" do formulário de evento recebeu o mesmo foco do D6 (a busca que aparece), por ser o mesmo defeito na mesma tela.

Registrado e não feito:

- D5: enquanto um botão desabilitado grava ("Salvar nome"), o foco passa pelo `body` e termina no aviso. O Chrome tira o foco do botão que fica `disabled`, e a rede de segurança não age, porque o botão não sai do DOM. O "Confirmar envio" já leva o foco à confirmação durante o envio; os outros botões que gravam desabilitados podem seguir o mesmo caminho.
- D8: o aviso de reconexão não tem nome acessível (anterior à migração). Pede `aria-labelledby` num texto que troca com o estado, ou um texto novo.
- Novo cliente e edição a 834px: a caixa de "Observações", com o topo à vista, fica com 4px sob a barra fixa. O Chrome não rola, qualquer que seja a reserva.
- Novo evento no desktop: focar o "Cancelar" da barra flutuante rola o conteúdo 16px uma vez.
- No Renomear e no Excluir pelo menu de ações, o foco passa de 0 a 3 ms pelo "⋯" antes do destino, sem quadro desenhado com ele. Se um leitor de tela o anuncia não foi conferido.
- A reserva de rolagem não tem variantes por `data-visao`, ao contrário das outras regras por visão.
- O erro da busca de cliente do formulário de usuário não tem `role` nem ligação ao campo (anterior à fase), ao contrário do formulário de processo.
- Candidatas a peça compartilhada: o `_focoDeVolta` e o `MostrarErro` repetidos em cinco e em oito telas; as três formas de "filtro aplicado" (Agenda, Atividades, Auditoria). Ficaram por custo de manutenção, sem efeito na tela.
- `GestaoDeDocumentos.cs` tem 671 linhas. `AtividadeComoAlvoAsync` vem antes da geração, e `AbrirAcessoAsync` não tem geração (raros; a ficha se corrige).
- Pontos de robustez sem efeito hoje: importação dupla possível do módulo de foco no primeiro desenho; a rede não fica instalada se a primeira chamada falhar; o módulo do menu de ações e a vigia do layout guardados antes de prontos, se o componente for descartado durante a importação; a marca do gesto de ponteiro pode durar até o próximo clique quando o `pointerup` não chega; as abas não tiram o ouvinte de teclado no descarte e guardam as chaves de abas removidas; o painel de filtros vai ao servidor a cada tecla (`@onkeydown` no `form`).
- Testes que provam menos do que o nome promete ou que faltam: a ordem do `box-sizing` antes do `width` no teste do aviso de reconexão; a trava recursiva lê só `Components/`; a prova positiva de `UltimoFocoPedido`/`FocosDeElementoPedidos` só veio nas tarefas seguintes; o teste dos ids das abas na ficha do cliente; o `pararDeVigiar` do menu no descarte; o descarte da vigia do layout no bUnit; a guarda de clique duplo do "Tentar de novo" da Auditoria; o segundo clique do "Tentar de novo" do `CargaDoFormulario`; `ErrosDeGravacaoTests` não pega `??=` nem `+=`; uma asserção repetida em `AvisoDeEventoSalvoTests`.
- A Auditoria com um período sem registros mostra "Página 1 de 1 · 0 registros" (a 9G só tirou a paginação sob "Identificador inválido.").
- Continuam da 9C à 9F, sem mudança: o trio de recarga das listas, os formulários que carregam em `OnInitializedAsync`, a lista das áreas de documento em três lugares, a cobertura dos caminhos de erro, os invólucros duplicados da agenda e o conflito ao salvar que pede recarregar.

Correção da nota da 9F sobre a margem das caixas ("Para a 9G", regras de elemento que ficaram no `app.css`): a nota diz que, sem o `app.css`, as caixas de marcar dos participantes do desktop (`aj-opcao__caixa`) ganham 3px em cima e embaixo. Pelas coletas da 9F, as caixas que ganhavam margem eram as escondidas: os quatro rádios dos ladrilhos do tipo e as dez caixas dos dois grupos de chips de pessoa, com `opacity:0`. As cinco caixas visíveis dos participantes já tinham tamanho, margem e cor pelo `.aj-opcao input`, desde a 9D. A 9G deu `margin:0` às escondidas no design system, e a medida não achou diferença visível.

Fora da migração (seção 13): a tarefa de segurança do login e de Minha senha (senha devolvida no HTML da resposta, campo de senha preenchido depois do erro, botões de envio sem trava); recorrência e lembrete de evento, filtro da agenda por cliente, substituir documento, matriz de permissões; tema escuro.

#### Decisões de execução para o usuário rever

Decididas na execução, sem quadro ou contra o quadro, das fases 9B a 9G, num lugar só. As das fases 9E e 9F têm mais detalhe nas próprias subseções e nos planos ("Decisões tomadas na redação"); as da 9G, no plano dela.

- (9B) Na tela de Documentos comuns da Diretoria, no máximo uma coisa aberta entre o envio, o renomear e a confirmação de exclusão. Rever se o painel de envio deve ficar aberto enquanto se renomeia.
- (9C) A Auditoria abre sem período preenchido, como antes; o quadro mostra um período de exemplo.
- (9D) A ficha do cliente tem o painel Observações também no tablet (o quadro do tablet não tem).
- (9D) Na ficha do cliente, no desktop, a ordem de leitura e de Tab é Dados, Processos, Documentos e Usuários (o painel de dados vem primeiro na marcação e o CSS o põe à direita).
- (9D) Nos formulários, perder o acesso mostra "Acesso negado."; nas listas e fichas, "Seu acesso foi encerrado. Entre novamente.".
- (9D) A `textarea` de Observações do cliente tem 4 linhas em todas as visões (3 nos quadros do tablet e do mobile).
- (9D) A grade folgada vale também no mobile.
- (9E) Vindo de Produtores, o voltar da ficha leva ao produtor, mas o mês volta ao corrente; Excluir volta sempre a `/atividades`.
- (9E) "Trocar revisor" só aparece quando a atividade não permite redefinir os responsáveis; no mobile, também no "Mais ações".
- (9E) A Diretoria vê no subtítulo e nos chips o escritório inteiro, mesmo com um responsável escolhido no filtro.
- (9E) A cor do avatar sai de uma conta sobre o nome e não reproduz a do quadro.
- (9E) A coluna e a linha Complexidade (posição na tabela e nos Detalhes); a tabela com 980px de largura mínima, rolando de lado.
- (9E) Etiquetas de situação com ícone só na tabela; nos cartões e na ficha, com ponto.
- (9E) Documentos da atividade no tablet e no mobile: nome como link de download e ações num "⋯"; "Enviar documento" secundário no título do painel; o painel fecha depois do envio.
- (9E) O comentário novo do tablet em linha, com o botão de ícone; no desktop e no mobile, rótulo, caixa de três linhas e "Comentar".
- (9E) Concluir minha parte, Aprovar e Excluir pedem confirmação num aviso de atenção no topo da ficha.
- (9E) O terceiro estado das telas de ação (a atividade já não permite a ação) é um aviso de atenção, sem formulário; o voltar mostra o título da atividade.
- (9E) O modo do revisor: painel médio, título da atividade no subtítulo, "Salvar" no mobile; o perfil entre parênteses sai `(Diretor)`, e o quadro mostra `(Diretoria)`.
- (9E) Devolver e reabrir: os erros de motivo e de responsáveis aparecem juntos; o motivo vai aparado e os responsáveis na ordem da atividade.
- (9E) Divergências dos quadros antigos mantidas para todas as telas ficarem iguais (selo do aviso, barra fixa com 46px, nome da ficha do mobile com 26px e as outras da lista da 9E).
- (9F) A Semana do desktop tem sempre sete colunas (o quadro usa `auto-fit`).
- (9F) A régua do Dia vai das 08:00 às 19:00 e se estende até o primeiro e o último evento; até quatro colunas de simultâneos.
- (9F) O mobile não tem "Hoje"; no Dia e na Semana, as setas andam uma semana.
- (9F) O Mês mostra três eventos por célula e `+N`, que leva ao Dia; o dia de fora do mês na grade compacta continua tocável.
- (9F) No Mês do mobile, a grade compacta fica no topo do conteúdo e rola com a lista do dia (desenho novo, sem quadro).
- (9F) Filtro aplicado na agenda: só "Filtrar" grava o filtro; fechar o painel sem filtrar volta os campos ao aplicado; os filtros não vão para o endereço.
- (9F) O avatar do próprio usuário é dourado; `(você)` vence `(inativo)`; participante sem nome aparece como "Usuário não encontrado".
- (9F) Exclusão recusada porque o evento sumiu mostra "Evento não encontrado."; as outras, a mensagem do servidor.
- (9F) As setas levam o `aria-label` dos quadros, sem verbo ("Período anterior").
- (9F) O evento novo começa em hoje, não na data à vista na agenda. A decidir.
- (9F) Depois de editar mudando a data, a ficha volta à agenda no período antigo, onde o evento já não aparece. A decidir.
- (9F) Formulário de evento: tipo em `select` no desktop e em ladrilhos no tablet e no mobile, começando em "Reunião"; duração em `select` com "Outra duração"; o vínculo Atividade também no tablet e no mobile; o processo não aparece nos cartões.
- (9F) `?visao` só em minúsculas e sem acento; as setas não travam em 2000–2100.
- (9F) Divergências dos quadros antigos mantidas para todas as telas ficarem iguais, e a cor dos avatares pela conta da 9E.
- (9G) Atividades e Auditoria passaram ao filtro aplicado: a consulta lê o que foi gravado no "Filtrar"; fechar o painel sem filtrar volta os campos ao aplicado.
- (9G) Em Atividades, a busca digitada só vale depois de "Buscar" (antes, o chip usava a busca digitada); os chips gravam no filtro aplicado; "Buscar" e o Enter aplicam também o que está no painel aberto.
- (9G) Na Auditoria, o período trocado fora do painel só vale depois de "Filtrar"; os botões das linhas partem do período aplicado; "Filtrar" recusado (identificador inválido) mantém o painel aberto.
- (9G) O menu de ações fecha quando o foco sai dele (Tab, Shift+Tab, clique fora), e nunca há dois abertos.
- (9G) Com o menu "Mais" aberto, o resto da tela fica inerte; o foco vai ao "Início", não ao "Fechar menu"; escolher a tela em que já se está fecha o menu e devolve o foco ao "Mais".
- (9G) A paginação sem ação usa `aria-disabled`: o botão continua focável e é anunciado como indisponível.
- (9G) A falha das buscas nos formulários de atividade e de evento leva o foco ao aviso do topo, que se revela, e tira o foco do campo de busca. No formulário de processo, o erro da busca vai junto do campo.
- (9G) Todos os erros do topo dos formulários se revelam; o recado vindo de outra tela não se revela.
- (9G) Cancelar a confirmação de envio devolve o foco ao "Enviar" do painel, não ao "Enviar documento".
- (9G) O painel de acesso dos documentos virou `form`: o Enter envia "Salvar acesso".
- (9G) No Andamento, a troca de página mantém a lista à vista; o chip troca a lista por "Carregando…".
- (9G) Na lista de usuários, o resultado de uma inativação que chega com a confirmação de outro usuário aberta é calado; a lista recarrega e mostra a situação.
- (9G) Na Auditoria, a paginação some durante a carga da aba nova e depois de uma falha.
- (9G) A barra do desktop da troca de revisor fica com 680px e fixa ao pé; a do devolver não é fixa.
- (9G) A grade compacta do Mês fica à vista durante a carga, sem os pontos.
- (9G) O "+N" leva o foco ao título do Dia, e o período não é anunciado de novo nessa troca.
- (9G) A primeira página do circuito não volta ao topo; só as trocas de página seguintes.
- (9G) Excluir da ficha da atividade no mobile devolve o foco ao "Mais ações".
- (9G) Abas com Alt, Ctrl ou Cmd deixam a tecla com o navegador (Alt+← volta a página).
