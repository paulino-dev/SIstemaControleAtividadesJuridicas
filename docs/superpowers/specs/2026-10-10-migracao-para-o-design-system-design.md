# Migração das telas para o design system — fidelidade ao canvas

Data: 2026-10-10
Estado: aguardando revisão
Fontes de desenho:

- Canvas de telas: https://claude.ai/artifact/HzHWv1pgVXAnYHuPEYQDFj (versão lida: `1791598628-21e1`)
- Design system: https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS (versão lida: `1791598661-a3fd`)

## 1. Objetivo

Todas as telas do sistema passam a ser escritas com o design system (classes `aj-` sobre os tokens). As telas que têm quadro no canvas ficam idênticas a ele, em cada uma das três visões: desktop, tablet e mobile.

Hoje as páginas Razor usam a marcação antiga (`h1`, `table`, `.filtros`, `.ficha`, `button.secundario`), vestida pelo `wwwroot/app.css`. A navegação (`MainLayout.razor`) já segue o design system.

### Critérios de sucesso

- Cada tela com quadro no canvas, aberta na largura do quadro, mostra a mesma estrutura, os mesmos componentes, os mesmos textos e as mesmas medidas do quadro.
- Nenhuma função que o sistema tem hoje deixa de existir.
- Nenhuma tela tem CSS próprio: todo estilo vem de `css/tokens.css` e `css/componentes.css`, que são cópias do design system.
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
- O `app.css` perde, a cada tela migrada, as regras que vestiam a marcação antiga dela. Na fase 9G sobram a base (`box-sizing`, `body`, foco), a faixa de erro do Blazor e nada mais.
- A cópia do design system instalada no canvas (`project/ds/araujods/`) não é atualizada por esta migração. Os quadros continuam como estão.

### 4.2 Três visões

- O design system ganha classes de visibilidade apoiadas nas variáveis por visão, que também respeitam `data-visao`: `aj-so-desktop`, `aj-so-tablet`, `aj-so-mobile`, `aj-desktop-tablet` e `aj-tablet-mobile`.
- A página renderiza os blocos de cada visão e marca cada um com a classe da visão. Exemplo de uma lista: barra de filtros e tabela (`aj-so-desktop`); busca, chips e cartões (`aj-tablet-mobile`).
- Bloco escondido usa `display: none`: não é lido por leitor de tela nem recebe foco.
- As listas são paginadas (20 itens), então a duplicação no HTML é limitada.

### 4.3 Layout e encaixes

O `MainLayout` passa a oferecer um encaixe, `barra-superior`, que a página preenche com as seções do Blazor (`SectionOutlet` e `SectionContent`). Ele fica antes do `main`, fora da área que rola, e recebe a barra superior do mobile da tela.

O botão flutuante e a barra `aj-acoes--fixa` não precisam de encaixe: ficam na própria página e o CSS os prende à coluna de conteúdo (`aj-app__coluna`, que é posicionada), fora do fluxo que rola. A barra de ações fica dentro do formulário, para o botão enviá-lo. (Emenda de 2026-10-10, ao planejar a 9A: a primeira versão previa três encaixes.)

A barra superior do mobile tem três variantes, as do canvas:

| Variante | Conteúdo | Barra inferior | Onde |
|---|---|---|---|
| Título | Símbolo, `h1` com subtítulo, avatar; pode ter uma segunda linha (busca, chips, controle segmentado) | Sim | Telas de topo |
| Voltar | Botão de voltar e `h1` centralizado; pode ter uma ação à direita | Não | Ficha e formulário |
| Logo | Logo horizontal e avatar | Sim | Tela que não declara barra (não encontrada, e as ainda não migradas) |

Outras mudanças no layout:

- O invólucro `div.pagina` sai: cabeçalho, filtros e painéis passam a ser filhos diretos de `main.aj-app__conteudo`, que é quem dá o espaçamento entre blocos. Até a 9G, uma regra interina no `app.css` mantém em bloco o conteúdo das telas ainda não migradas, para elas não mudarem de aparência.
- A barra inferior some nas telas com a barra de voltar. O layout decide por CSS, sem estado.
- "Mais" continua sendo o botão que abre o menu. Ganha `aria-current="page"` quando a rota atual não está entre os destinos da barra daquela visão, e enquanto o menu está aberto.
- Com o menu escondido no desktop, o botão "Exibir menu" e o símbolo ficam na linha do título, como no quadro `AgendaMenuEscondido`. Tela sem cabeçalho de página (as ainda não migradas e a página não encontrada) recebe do layout uma faixa com o mesmo botão acima do conteúdo, para o menu sempre poder voltar.
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

### 4.6 Ícones

`Icone.razor` ganha: `adicionar`, `buscar`, `filtros`, `filtrar` (funil), `voltar`, `avancar`, `confirmar`, `excluir`, `devolver`, `entrar`, `baixar`, `atencao`, `erro`, `enviar` e `planilha`. Os traçados vêm do grupo Icones do design system e dos quadros. O componente passa a aceitar tamanho, para uso fora de menu e botão.

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

Sem quadro, pelo design system: página de erro (bloco centralizado, como a não encontrada), faixa de erro do Blazor e aviso de reconexão (texto em português, `aj-aviso`).

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

## 14. Riscos

| Risco | Tratamento |
|---|---|
| O canvas muda durante a migração (o usuário o edita pelo Chat do canvas) | Baixar de novo a cada fase e apontar diferenças |
| Bloco escondido recebendo foco ou sendo lido | `display: none` nos blocos de outra visão; conferência por teclado em cada fase |
| Asserção reescrita que passa a não provar nada | Revisão das asserções negativas uma a uma; cada uma ganha uma afirmação positiva ao lado |
| Fases longas divergindo da `main` | Fase curta, entrada na `main` ao fim de cada uma |
| Desenho novo (seção 9) não agradar | Cada item aparece primeiro na fase que o usa, é mostrado no navegador e pode ser ajustado antes da entrada na `main` |
