# Fase 9D — Clientes e processos no design system: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** As telas de clientes e de processos (as duas listas, as duas fichas e os dois formulários) ficam idênticas aos quadros do canvas nas três visões, com os contadores que os quadros mostram vindos do backend, e a ficha do cliente ganha o painel único de documentos com chips por área.

**Architecture:** Cada padrão que os quadros da fase trazem como estilo embutido vira classe `aj-` no artifact do design system e é copiado para `wwwroot/css`. As listas emitem a forma do desktop e a do tablet e do mobile em invólucros de visão; as fichas e os formulários viram telas com rota própria. Na ficha do cliente o painel de documentos aparece uma vez só na marcação: o arranjo lado a lado põe a coluna dos dados primeiro e o CSS a leva para a direita no desktop. As abas do mobile escondem por CSS os painéis que não são da aba escolhida. A lógica de documentos (`GestaoDeDocumentos`) ganha as regras de "uma coisa aberta por vez", que hoje moram só na tela de Documentos comuns, e passa a ser coberta por testes de interação com o bUnit antes de mudar.

**Tech Stack:** .NET 10, Blazor Interactive Server, CSS do design system (tokens e classes `aj-`), xUnit 2.9, FluentAssertions 7.2, Testcontainers, bUnit 2.11.3 (novo, só no projeto de testes de integração).

**Spec:** `docs/superpowers/specs/2026-10-10-migracao-para-o-design-system-design.md` (seções 3, 4, 5, 6, 7.4, 8, 9, 10, 11 e 15).

## Decisões deste plano

- **Quadros desta fase:** baixados do canvas em 2026-10-14 (versão `1791995530-9a55`; nenhum quadro mudou desde 2026-10-10). Cópia local, somente leitura, em `C:\Users\Gabriel Paulino\AppData\Local\Temp\claude\c--GitHub-SIstemaControleAtividadesJuridicas\687990e8-545b-42b7-9ae3-b685bf4369c8\scratchpad\quadros-9d\project\`: `Clientes`, `Cliente`, `ClienteFormulario`, `Processos`, `Processo`, `ProcessoFormulario`, cada um com as variantes `Tablet` e `Mobile` (`.dc.html`). A cópia completa do canvas de 2026-10-10 (com `Menu`, `Trilho` e `NavInferior`, que a ferramenta de conferência usa) continua em `C:\Users\Gabriel Paulino\AppData\Local\Temp\claude\c--GitHub-SIstemaControleAtividadesJuridicas\f5f0011f-66b5-41ed-8db8-f7d789323524\scratchpad\artifact-files\8991caa9-7339-47d7-a01c-84892497662e\project\`.
- **bUnit entra, a pedido do usuário** (2026-10-14, a decisão que a spec deixou para ele na seção 15, "Da fase 9B"). Só no projeto de testes de integração, na versão `2.11.3` (de 2026-09-13, com `net10.0`). Os testes de interação da tela de Documentos comuns da Diretoria vêm antes de qualquer mudança em `GestaoDeDocumentos` (Tarefa 2) e passam com o código de hoje: são a rede para a Tarefa 4.
- **Contadores no retorno das listagens** (spec 6), sem consulta auditada a mais: `Pagina<T>` deixa de ser selada e ganha três derivadas, `PaginaDeClientes` (total do cadastro e inativos, sem olhar a busca nem o "incluir inativos"), `PaginaDeProcessos` (total com a busca e o cliente, sem o filtro de situação) e `PaginaDeDocumentos` (quantos documentos de cada área o usuário pode ler). Quem lê a página como `Pagina<T>` continua compilando.
- **Subtítulo da lista de clientes:** "6 clientes · 1 inativo" conta o cadastro inteiro, como o subtítulo das atividades (spec 6), e não muda com a busca. O de processos ("5 processos") e o chip "Todas · 5" contam o que a busca encontrou em todas as situações.
- **Fichas e formulários com rota** (spec 5): `/clientes/{id}`, `/clientes/novo`, `/clientes/{id}/editar`, `/processos/{id}`, `/processos/novo`, `/processos/{id}/editar`. As listas perdem o formulário e a ficha de dentro. "Abrir" leva à ficha, para a Diretoria e para a Produção. A edição sai da ficha ("Editar"); salvar volta ao destino do voltar (a lista, no novo; a ficha, na edição) com um recado.
- **Inativar e ativar o cliente saem da lista e vão para a ficha** (spec 3). Inativar pede confirmação (spec 8), que diz quantos usuários do portal perdem o acesso. Ativar é de um clique. A situação do processo muda no painel "Situação" da ficha, que sai do formulário de edição.
- **Ficha do cliente sem duplicar o painel de documentos.** A coluna dos dados (Dados, Observações) vem primeiro na marcação e a das listas (Processos, Documentos, Usuários) depois; no desktop o arranjo inverte a linha e a coluna das listas fica à esquerda, como no quadro. No tablet e no mobile os blocos empilham na ordem da marcação, que é a do quadro do tablet. A ficha do processo, que não tem nada com estado além do seletor de situação, emite o desktop e o tablet e mobile em blocos separados, como as listas.
- **Abas do mobile** (spec 9): `Abas` é peça compartilhada (spec 4.4). Os painéis de cada aba ficam num invólucro `aj-aba-painel`, que some (`display: contents`) e, no mobile, esconde o que não é da aba escolhida. Ficha do cliente: Dados (Dados e Observações), Processos, Documentos, Usuários (só a Diretoria). Ficha do processo: Dados (Dados e Observações), Clientes, Situação (só a Diretoria). "Alterar situação", na barra do mobile do processo, abre a aba Situação.
- **Documentos na ficha:** um painel "Documentos" com chips por área e contador; a Diretoria vê Gerais, Contratos de honorários, De atividades e Do cliente; a Produção, Gerais e De atividades, e só baixa. "Enviar documento" envia para a área do chip escolhido (menos De atividades, que não tem envio, como hoje) e o painel de envio diz a área. Trocar de chip fecha o que estiver aberto e descarta o arquivo escolhido. Os painéis de envio, de renomear e de acesso abrem logo abaixo do painel "Documentos"; os avisos e a confirmação de exclusão, logo acima dele (nunca um painel dentro do outro). O componente antigo (`DocumentosDoCliente.razor`) é apagado.
- **A lógica de "uma coisa aberta por vez"** (envio, renomear, acesso e exclusão; cada abertura fecha as outras; nada abre com um envio ou uma exclusão em curso) e o foco no painel aberto passam de `DocumentosComunsDaDiretoria` para a base `GestaoDeDocumentos`, que ganha `DescartarEnvio()` (spec 15). O `OnAfterRenderAsync` fica só na base. Das extrações que a spec 15 pede para o terceiro uso, `Endereco` e `Detalhe` viram `TextosDeDocumento`; a tabela não é extraída (as três têm colunas e ações diferentes).
- **A lista que sobrevive à recarga** (spec 15, "Da fase 9C") não é copiada nesta fase: as listas de clientes e de processos deixam de ter ação na linha. Fica para a fase que a copiar.
- **Valor da causa** é digitado como no quadro (texto, "0,00"): aceita o formato brasileiro ("84.000,00", "84000", "84000,5") e recusa o resto com erro no campo. Hoje é um campo numérico que espera ponto.
- **Busca sem botão** (spec 8) no tablet e no mobile responde ao Enter; no desktop a barra tem "Buscar", como no quadro. A busca da barra do mobile e a de cliente do formulário de processo enviam um formulário escondido próprio, pelo atributo `form` (padrão da 9C).
- **"Ações de {nome}"** no botão de três pontos da linha de documento do tablet (regra do design system, 9B), e não "Ações do documento" do quadro: o rótulo precisa distinguir as linhas.
- **A cópia do design system no canvas passa a acompanhar cada publicação**, a pedido do usuário (2026-10-14, quando a 9C foi publicada). A spec (4.1) dizia o contrário; a emenda da Tarefa 13 corrige. Os quadros continuam como estão: só a cópia (`project/ds/araujods/`) e o registro dela mudam.
- **Respiro das linhas em painel:** as linhas que levam a outra ficha usam 10px em cima e embaixo, como as de processos do cliente no quadro; o quadro do processo e o de usuários do cliente desenham uma linha só, sem o respiro de baixo. Diferença aceita: o painel fica 10px (ou 8px) mais alto no tablet.

## Como ler este plano

Cada tarefa traz as **regras** (o contrato), as **interfaces** que ela consome e produz e os **testes obrigatórios**. O plano traz o código onde ele é a própria fidelidade (o CSS, a marcação das telas) ou onde o detalhe é sutil. O restante é escrito no estilo do que já existe. O código do plano não foi executado: quando o Blazor, o bUnit ou o compilador exigirem, ajuste o mínimo para preservar o comportamento e registre o desvio no relatório, com a mensagem de erro que o motivou.

Nunca apague um teste nem afrouxe uma asserção. Quando uma asserção mudar porque a marcação mudou, reescreva-a mantendo o que ela prova e liste-a no relatório. Asserção negativa (`NotContain`) que deixaria de provar algo com a marcação nova ganha uma afirmação positiva ao lado. Asserção sobre o bloco de uma visão (`BlocoDeVisao.De`) sempre tem uma afirmação positiva sobre o mesmo bloco: um bloco vazio aceitaria qualquer `NotContain`.

O HTML desenhado pode ter espaço em branco e quebra de linha entre as marcas. As expressões regulares dos testes usam `\s*` entre marcas e `(?s)` quando atravessam linhas; quem escreve a marcação deixa colados os elementos que o teste procura colados (os botões de uma célula de ações, por exemplo: o espaço entre eles é desenhado).

A fonte de cada tela é o quadro do canvas. Antes de escrever a marcação de uma tela, releia os três arquivos dela na cópia local. Se um quadro diferir do que este plano descreve, pare e relate.

## Estado de partida

- Worktree `.claude/worktrees/feat+fase-9d-clientes-e-processos`, branch `worktree-feat+fase-9d-clientes-e-processos`, na `main` (`b06b8c1`), que já tem as fases 9A, 9B e 9C.
- Canvas: https://claude.ai/artifact/HzHWv1pgVXAnYHuPEYQDFj. Design system: https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS, versão `1791995411-b333` (a que a 9C publicou); o `componentes.css` do código é cópia dela.
- Web, depois da 9C: `CabecalhoDePagina` (com `Acao`, `AcaoHref`, `Complemento`, `LinhaDoMobile`, `VoltarPara`), `CabecalhoDeDetalhe`, `Aviso` (com ações e foco ao aparecer), `EstadoDaLista`, `Paginacao` (com `SempreVisivel`), `MenuDeAcoes`, `RotuloDeCampo`, `Icone`, `Contagem` (`De`, `ComInativos`), `Recados`, `FusoDoEscritorio.Formatar`, `TextosDoCliente.Inativado`. As telas desta fase (`Clientes.razor`, `Processos.razor`) ainda têm a marcação antiga vestida pelo `app.css`, com o formulário e a ficha dentro da lista.
- Documentos: `GestaoDeDocumentos` (base), `DocumentosComunsDaDiretoria` (migrada na 9B, com as regras de uma coisa aberta por vez), `DocumentosParaBaixar` (portal), `DocumentosDoCliente` (marcação antiga, usada só por `Clientes.razor`), `EnvioComConfirmacao`.
- Testes: `AplicacaoDeTeste` sobe a aplicação inteira contra um banco novo por teste (admin Diana Diretora, `diana@x.com`; `CriarPedroAsync` cria Pedro Produção; `CriarCarlaAsync` cria a cliente Maria Silva e a usuária de cliente Carla Cliente e devolve `(carla, maria)`). `TelaDeTeste` desenha um componente fora da página (`RenderizarAsync`, `RenderizarComAsync`, `RenderizarEAgirAsync`, `RenderizarSemDadosAsync`); `DespachanteCombinado` responde o que o banco não produz por encomenda; `ComBarraSuperior<T>` desenha um componente com o encaixe da barra superior; `BlocoDeVisao.De` recorta os blocos de uma visão.
- Linha de base: a suíte inteira passou em `4cad402` (a árvore da `main` de hoje): unidade 872, integração 676.
- `git` só pela ferramenta PowerShell. Antes de cada commit, `git branch --show-current`.
- A máquina tem caído ao começar build ou teste: build sempre `dotnet build -m:1`; teste sempre `--no-build` depois de um build feito; um comando por vez. A suíte inteira roda em lotes (`suite-em-lotes.ps1`, em `C:\Users\Gabriel Paulino\.claude\projects\c--GitHub-SIstemaControleAtividadesJuridicas\conferencia-de-telas\`).

## Global Constraints

- TFM único `net10.0`. `Nullable` e `TreatWarningsAsErrors=true`. Pacote novo só o `bunit` 2.11.3, no projeto `AtividadesJuridicas.IntegrationTests`, com a versão em `Directory.Packages.props`.
- Código, nomes e textos em português, com acentuação correta. Arquivos em UTF-8 sem BOM, com o fim de linha que o arquivo já tem.
- A camada Web só chega aos dados pelo `IDespachante` (`WebSoPeloDespachanteTests`). Consulta nova no backend só os contadores da Tarefa 3, dentro das listagens que já existem.
- `wwwroot/css/tokens.css` e `wwwroot/css/componentes.css` não são editados à mão: mudam no artifact do design system e são copiados de lá.
- Tela migrada não tem atributo `style`, nem `<style>`, nem arquivo `.razor.css`, nem classe que não comece por `aj-`, nem `<button>`, `<table>`, `<input>`, `<select>` ou `<textarea>` sem `class`. Nenhum CSS novo no `app.css`.
- As classes de visibilidade (`aj-so-desktop`, `aj-so-tablet`, `aj-so-mobile`, `aj-desktop-tablet`, `aj-tablet-mobile`) e a de aba (`aj-aba-painel`) vão num invólucro próprio (`div` ou `span`) cuja classe é **só** a de visibilidade ou a de aba, nunca no elemento do componente.
- Visões: mobile até 760px, tablet de 761px a 1199px, desktop a partir de 1200px.
- Todo texto que um teste procura no HTML é escrito por **uma única** expressão Razor ou por marcação fixa.
- Rotas existentes não mudam. Rotas novas: `/clientes/{id:guid}`, `/clientes/novo`, `/clientes/{id:guid}/editar`, `/processos/{id:guid}`, `/processos/novo`, `/processos/{id:guid}/editar`. `RotasDoCliente` não muda (rota nova nasce fechada ao portal).
- Botão só de ícone leva `aria-label` com o verbo e o objeto ("Remover Transportes Litoral Ltda.", "Ações de Procuração.pdf").
- Produção: vê as listas e as fichas, sem nenhuma ação de cadastro; nos formulários, o cabeçalho e "Acesso negado.", sem formulário e sem consulta (a tela confere o perfil antes de consultar). Na ficha do cliente a Produção não consulta o que não pode ler (contratos, documentos do cliente, usuários do portal).
- Textos exatos, dos quadros:
  - Clientes: `Clientes`; subtítulo `{n} clientes · {m} inativos` (`1 cliente`, `1 inativo`; sem a parte dos inativos quando m é 0); ação `Novo cliente`; busca com o rótulo para leitor de tela `Buscar cliente` e o texto de exemplo `Buscar por nome, CPF ou CNPJ`; `Incluir inativos`; `Buscar`; colunas `Nome`, `Documento`, `E-mail`, `Telefone`, `Cidade`, `Situação`, `Ações` (só para leitor de tela); `Ativo`, `Inativo`; `Abrir`; no cartão, `{CPF|CNPJ} {documento}` e `{cidade} · {telefone}`; paginação `Página {n} de {m} · {x} clientes`; chips do mobile com `aria-label="Filtros"`
  - Ficha do cliente: voltar `Clientes` (no mobile, `Voltar para Clientes` e o título da barra `Cliente`); subtítulo `{Pessoa física|Pessoa jurídica} · {CPF|CNPJ} {documento} · {Ativo|Inativo}`; no mobile, `{Pessoa física|Pessoa jurídica} · {Ativo|Inativo}`; `Inativar`, `Editar`; painéis `Processos do cliente` (colunas `Título`, `Número`, `Situação`, `Ações`; `Sem número`; no tablet, `{número} · {situação}`), `Documentos` (`Enviar documento`; chips `Gerais · {n}`, `Contratos de honorários · {n}`, `De atividades · {n}`, `Do cliente · {n}`, com `aria-label="Área de documentos"`; colunas `Nome`, `Tamanho`, `Enviado em`, `Por`, `Ações`; `Renomear`, `Acesso`, `Excluir`; a ajuda `A Diretoria acessa sempre. Em “Acesso” você escolhe os perfis e os usuários que também veem cada documento geral.`), `Usuários do cliente` (colunas `Nome`, `E-mail`, `Situação`, `Último acesso`; a ajuda `O acesso do cliente ao portal é criado em Usuários.`; no tablet, `{situação}` e `Último acesso {dd/MM/aaaa HH:mm}`), `Dados` (PJ: `CNPJ`, `Nome fantasia`, `Responsável`, `Inscrição estadual`, `Inscrição municipal`, `E-mail`, `Telefone`, `Endereço`; PF: `CPF`, `RG`, `Data de nascimento`, `E-mail`, `Telefone`, `Endereço`; `—` no vazio), `Observações`; abas `Dados`, `Processos {n}`, `Documentos {n}`, `Usuários {n}`
  - Formulário de cliente: `Novo cliente` (na edição, `Editar cliente`); subtítulo `Só o nome e o CPF ou CNPJ são obrigatórios.`; voltar `Clientes` (na edição, `Cliente`); controle `Tipo de cliente` com `Pessoa física` e `Pessoa jurídica`; painéis `Identificação e contato`, `Endereço`, `Observações` (rótulo só para leitor de tela `Observações`); rótulos PF `Nome completo`, `CPF` (exemplo `000.000.000-00`), `RG`, `Data de nascimento`; PJ `Razão social`, `CNPJ` (exemplo `00.000.000/0000-00`), `Nome fantasia`, `Responsável`, `Inscrição estadual`, `Inscrição municipal`; `E-mail`, `Telefone`; `Logradouro`, `Número`, `Complemento`, `Bairro`, `Cidade`, `UF`, `CEP`; ações `Cancelar` e `Salvar cliente` (no mobile, `Salvar`)
  - Processos: `Processos`; subtítulo `{n} processos` (`1 processo`); ação `Novo processo`; busca com o rótulo para leitor de tela `Busca` e o exemplo `Título, parte contrária ou número CNJ completo`; filtro `Situação` com `Todas`, `Ativo`, `Suspenso`, `Arquivado`, `Encerrado`; `Buscar`; colunas `Título`, `Número`, `Área`, `Clientes`, `Parte contrária`, `Situação`, `Ações`; `Sem número`; `—`; `Abrir`; chips `Todas · {n}`, `Ativos`, `Suspensos`, `Arquivados`, `Encerrados`, com `aria-label="Filtrar por situação"`; no cartão, `{número}`, `{área} · {clientes}`, `{parte contrária}` ou `Sem parte contrária`; paginação `Página {n} de {m} · {x} processos`
  - Ficha do processo: voltar `Processos` (no mobile, `Voltar para Processos` e o título da barra `Processo`); subtítulo `{número|Sem número} · {área} · {situação}`; no mobile, `{número|Sem número} · {situação}`; `Editar`; painéis `Clientes do processo` (colunas `Cliente`, `Documento`, `Polo`, `Ações`; `Abrir`; no tablet, `{CPF|CNPJ} {documento} · polo {polo em minúsculas}`), `Observações`, `Dados` (`Número`, `Área`, `Órgão`, `Comarca`, `Parte contrária`, `Distribuição`, `Valor da causa`), `Situação` (`Situação atual: ` e a situação em negrito; `Nova situação`; `Alterar`); abas `Dados`, `Clientes {n}`, `Situação`; no mobile, `Alterar situação`
  - Formulário de processo: `Novo processo` (na edição, `Editar processo`); subtítulo `Preencha os dados e vincule os clientes do processo.`; voltar `Processos` (na edição, `Processo`); painéis `Processo` e `Clientes do processo`; rótulos `Título`, `Número CNJ` (exemplo `0000000-00.0000.0.00.0000`), `Área de atuação` (`Selecione…`), `Órgão`, `Comarca`, `Parte contrária`, `Data de distribuição`, `Valor da causa` (exemplo `0,00`), `Observações`; `Polo` (`Ativo`, `Passivo`, `Terceiro`); `Remover {nome}`; busca com o rótulo para leitor de tela `Adicionar cliente (nome, CPF ou CNPJ)` e o exemplo `Adicionar cliente por nome, CPF ou CNPJ`; `Adicionar`; a ajuda `O cliente entra no polo ativo. Troque o polo ao lado do nome.`; ações `Cancelar` e `Salvar processo` (no mobile, `Salvar`)
  - Sem quadro, mantidos: `Carregando…`; `Seu acesso foi encerrado. Entre novamente.`; `Acesso negado.`; `Informe o nome.`, `Informe o documento.`, `Informe o título.`; `Cliente salvo.`, `Cliente ativado.` e as frases de `TextosDoCliente.Inativado`; `Processo salvo.`; `Situação alterada para {situação}.`; `Nenhum processo vinculado.`; `Há mais processos; veja todos em Processos.`; `Nenhum usuário. Crie o acesso do cliente em Usuários.`; `Cliente inativo: estes usuários estão sem acesso ao portal até o cliente ser reativado.`; `Nenhum cliente vinculado.`; `Nenhum cliente encontrado.`; `Mostrando os primeiros resultados. Refine a busca para ver outros.`; `{área} (inativa)` no seletor de área; `Nenhum documento.`; `Documento enviado.`, `Nome alterado.`, `Documento excluído.`, `Acesso atualizado.` e as variantes `…, mas não foi possível atualizar a lista.`; `Arquivo (até 50 MB)`, `Nome (opcional)`, `Enviar`, `Enviando…`, `Novo nome`, `Salvar nome`, `Salvar acesso`; `Quem acessa "{nome}"`, `Renomear "{nome}"`; `A Diretoria acessa sempre. Marque os perfis e os usuários que também podem ver este documento; desmarque para retirar.`; `Perfis`, `Usuários`; `Confirmar exclusão`; `Tentar de novo`
  - Sem quadro, novos por analogia: `Nenhum cliente.`, `Nenhum processo.`; `Cliente não encontrado.`, `Processo não encontrado.` (do caso de uso); confirmação de inativação com `aria-label="Confirmar inativação"`, título `Confirmar a inativação de {nome}?`, o texto de `TextosDoCliente.ConsequenciaDaInativacao` e os botões `Cancelar` e `Confirmar inativação`; `Ativar` no lugar de `Inativar` para o cliente inativo; confirmação de exclusão de documento com o título `Confirmar a exclusão de {nome}?` e o texto `O documento é excluído da ficha do cliente.`; `Vai para {área}.` no painel de envio (`Gerais`, `Contratos de honorários`, `Do cliente`); `Informe o valor como 84.000,00.`; `Sem e-mail`; `Sem cliente`; `Sem acesso registrado`; as abas `Dados` e `Situação` sem contador; o painel `Observações` só aparece com texto

## Review Focus

Condições que a spec implica e que mais provavelmente atingem quem usa clientes e processos. Cada uma tem o seu teste na tarefa indicada.

1. **Trocar de área com o envio aberto** (arquivo escolhido em "Gerais" e chip trocado para "Do cliente", que o cliente vê no portal): o envio é descartado; um documento nunca vai para uma área diferente da que o painel de envio diz. Teste na Tarefa 7.
2. **Endereço de ficha com um identificador que não existe** (cliente ou processo apagado, link antigo, colado de outro lugar): o voltar, o título da barra e "Cliente não encontrado." (ou "Processo não encontrado."), sem dado, sem ação e sem painel. Testes nas Tarefas 8 e 11.
3. **Produção abrindo a ficha do cliente:** não vê nem consulta contratos, documentos do cliente e usuários do portal (a tela não pede o que a Produção não lê; uma consulta negada encerraria a tela), e não vê "Inativar", "Editar", "Enviar documento" nem as ações das linhas. Teste na Tarefa 8.
4. **Valor da causa fora do formato** ("84000.50", "abc", "1.2.3"): erro no campo, nada gravado; "84.000,00" grava 84000. Teste na Tarefa 12.
5. **Falha da consulta da lista** (clientes, processos): o erro com "Tentar de novo" no lugar da lista, sem subtítulo de contagem, e nunca "Nenhum cliente." ou "Nenhum processo.". Testes nas Tarefas 6 e 10.

Conferências no navegador, na Tarefa 13: nome, e-mail e documento longos nas linhas, nos cartões e no cabeçalho da ficha a 390px; as abas do mobile com toque e teclado; o menu de ações das linhas de documento no tablet; a barra fixa do tablet e do mobile sem cobrir o último painel; a confirmação de inativação no topo da ficha a 390px.

---

### Task 1: Design system — padrões de clientes e processos e cópia para o código

Feita pela sessão que coordena: publica no artifact do usuário.

**Pré-requisito:** a ferramenta Artifact abre o design system (https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS) e o canvas. Se responder "artifact not found", o Claude Code está conectado a outra conta: pare e peça ao usuário para entrar com a conta dona do artifact. Não escreva telas antes desta tarefa.

**Files:**
- Artifact do design system, sob `project/`: `components/bundle.css`, `design-system.json` (só `lastChange`); README e prévia novos em `components/Abas/` e `components/CabecalhoDeFicha/`; README emendado (e prévia, onde há estado novo) de LadoALado, Painel, ListaEmPainel, Campo, PainelDeFiltros, Vinculo, CartaoDeAtividade, Tabela, Segmentado, Chip
- Artifact do canvas: `project/ds/araujods/` (tokens e `bundle.css`) e o registro `designSystems` do `canvas.json`, como a 9C fez
- Modify: `src/AtividadesJuridicas.Web/wwwroot/css/componentes.css`

**Interfaces:**
- Consumes: os 18 quadros desta fase.
- Produces: as classes `aj-lado-a-lado--painel-primeiro`, `aj-lado-a-lado--ficha`, `aj-lado-a-lado--formulario`, `aj-pilha`; `aj-painel--cabecalho`, `aj-painel__botoes`, `aj-titulo-da-ficha`; `aj-abas`, `aj-abas__aba`, `aj-abas__contador`, `aj-aba-painel`, `aj-aba-painel--oculta`; `aj-linhas--folgadas`, `aj-linha--link`, `aj-linha__lado`; `aj-opcao--filtro`, `aj-opcao__caixa`, `aj-chip--campo`; `aj-cartao__resumo`, `aj-cartao__situacao--apagada`, `aj-tabela__apagada`; `aj-segmentado--largo`; `aj-grade-campos--tablet-folgada`, `aj-grade-endereco` e os sete `aj-grade-endereco__{logradouro,numero,complemento,bairro,cidade,uf,cep}`, `aj-campo--curto`; `aj-campo-filtro--polo`, `aj-candidato`, `aj-candidato__texto`, `aj-candidato__titulo`; `aj-opcoes`; e as regras de contexto `.aj-barra-filtros>.aj-campo-icone`, `.aj-linha-filtros>.aj-campo-icone`, `:where(.aj-linha a)`, `.aj-ajuda strong`.

**Regras:**
- Nenhuma classe existente muda de valor. Medida de quadro que difere de uma classe existente vira modificador (spec, seção 3).
- Só tokens que já existem. O 20px do cabeçalho da ficha no tablet fica literal (não há `espaco-20`), como no quadro.
- Regra que muda por visão segue o padrão do arquivo: a consulta de mídia e, depois, as mesmas regras sob `[data-visao="…"]`, para a prévia de cada visão funcionar dentro da página do design system.
- O bloco novo vai ao fim do `bundle.css`, depois das seções da 9C.

- [ ] **Step 1: Conferir as versões**

Ação `read` do design system e do canvas (o artifact inteiro, sem `path`, antes de publicar em cada um: sem essa leitura a publicação é recusada). Se o conteúdo do design system diferir da versão `1791995411-b333` (compare o `bundle.css` publicado com o corpo do `componentes.css` do código, da linha "Visões:" em diante), alguém editou: refaça as emendas sobre a versão nova. A versão também muda quando o tipo do artifact ganha release nova, sem mudar o conteúdo: compare o conteúdo, não o número.

- [ ] **Step 2: Acrescentar o bloco ao fim de `components/bundle.css`**

```css
/* ---------- Clientes e processos (fase 9D) ---------- */
/* Lado a lado: com o painel primeiro na marcação (a ficha do cliente, que no tablet começa pelos dados), o
   desktop inverte a linha e o painel fica à direita; na ficha e no formulário o painel encolhe com o conteúdo; no
   formulário a coluna principal é um pouco mais estreita. Pilha: uma coluna de painéis dentro do arranjo, que no
   tablet e no mobile some como o arranjo. */
.aj-pilha{display:contents}
@media (min-width:1200px){
  .aj-lado-a-lado--painel-primeiro{flex-direction:row-reverse}
  :is(.aj-lado-a-lado--ficha,.aj-lado-a-lado--formulario) .aj-lado-a-lado__painel{min-width:0}
  .aj-lado-a-lado--formulario .aj-lado-a-lado__lista{flex:1.6 1 0}
  .aj-pilha{display:flex;flex-direction:column;gap:var(--aj-pagina-gap)}
}
[data-visao="desktop"] .aj-lado-a-lado--painel-primeiro{flex-direction:row-reverse}
[data-visao="desktop"] :is(.aj-lado-a-lado--ficha,.aj-lado-a-lado--formulario) .aj-lado-a-lado__painel{min-width:0}
[data-visao="desktop"] .aj-lado-a-lado--formulario .aj-lado-a-lado__lista{flex:1.6 1 0}
[data-visao="desktop"] .aj-pilha{display:flex;flex-direction:column;gap:var(--aj-pagina-gap)}
[data-visao="tablet"] .aj-pilha,[data-visao="mobile"] .aj-pilha{display:contents}

/* Cabeçalho da ficha: o painel com o título e, no desktop, as ações à direita; no tablet, mais arredondado e
   folgado. No mobile o título vai para a barra de voltar e o nome para o bloco de título da ficha. */
.aj-painel--cabecalho{flex-direction:row;flex-wrap:wrap;align-items:center;justify-content:space-between;gap:var(--espaco-12) var(--espaco-16)}
@media (max-width:1199px){.aj-painel--cabecalho{border-radius:var(--raio-20);padding:20px}}
[data-visao="tablet"] .aj-painel--cabecalho,[data-visao="mobile"] .aj-painel--cabecalho{border-radius:var(--raio-20);padding:20px}
[data-visao="desktop"] .aj-painel--cabecalho{border-radius:var(--aj-raio-cartao);padding:var(--aj-cartao-padding)}
.aj-painel__botoes{display:flex;flex-wrap:wrap;gap:var(--espaco-8)}
.aj-titulo-da-ficha{display:flex;flex-direction:column;gap:var(--espaco-6)}
.aj-titulo-da-ficha .aj-titulo{line-height:1.15;overflow-wrap:anywhere}
.aj-titulo-da-ficha .aj-ajuda{font-variant-numeric:tabular-nums}

/* Abas do mobile: uma por assunto da ficha, com contador; a escolhida sublinhada em verde. O painel de uma aba:
   no mobile só os da aba escolhida aparecem; no tablet e no desktop, todos. */
.aj-abas{display:flex;gap:var(--espaco-4);margin:0 calc(-1 * var(--espaco-16));padding:0 var(--espaco-16);overflow-x:auto;scrollbar-width:none;border-bottom:1px solid var(--borda-campo-suave)}
.aj-abas::-webkit-scrollbar{display:none}
.aj-abas__aba{flex:0 0 auto;display:inline-flex;align-items:center;gap:var(--espaco-6);min-height:var(--alvo-toque);padding:0 var(--espaco-12);border:0;border-bottom:2.5px solid transparent;background:transparent;color:var(--tinta-suave);font:600 14.5px/1.3 var(--font-sans);cursor:pointer}
.aj-abas__aba[aria-selected="true"]{border-bottom-color:var(--verde);color:var(--tinta);font-weight:700}
.aj-abas__aba:focus-visible{outline:2px solid var(--verde);outline-offset:-2px}
.aj-abas__contador{padding:0 7px;border-radius:var(--raio-pilula);background:var(--superficie-recuada);font-size:12px}
.aj-aba-painel{display:contents}
@media (max-width:760px){.aj-aba-painel--oculta{display:none}}
[data-visao="mobile"] .aj-aba-painel--oculta{display:none}

/* Lista em painel: linhas mais folgadas (as que levam a outra ficha), a linha que é um link, o lado direito com
   dados (a situação e o último acesso) e o link dentro da linha na cor da marca. */
.aj-linhas--folgadas>.aj-linha{padding:var(--espaco-10) 0}
.aj-linha--link{color:var(--tinta);text-decoration:none}
.aj-linha--link:focus-visible{outline:2px solid var(--verde);outline-offset:2px}
.aj-linha__lado{flex:0 0 auto;text-align:right;font-variant-numeric:tabular-nums}
:where(.aj-linha a){color:var(--verde)}
:where(.aj-linha a:hover){color:var(--tinta)}

/* Filtros com busca: na barra do desktop a busca ocupa o que sobra e a opção marcável tem a altura dos campos da
   barra; na linha do tablet a busca é branca e ocupa o que sobra, e o chip ao lado tem a altura e o raio do campo. */
.aj-barra-filtros>.aj-campo-icone{flex:1 1 320px;min-height:var(--campo-filtro)}
.aj-opcao--filtro{min-height:var(--campo-filtro);font-size:14.5px}
.aj-opcao__caixa{cursor:pointer}
.aj-linha-filtros>.aj-campo-icone{flex:1 1 auto;background:var(--superficie)}
.aj-chip--campo{min-height:var(--aj-campo);border-radius:var(--aj-raio-campo)}

/* Cartão e tabela: o resumo do rodapé (e-mail, parte contrária), cortado com reticências; a situação que não é a
   normal (processo suspenso, arquivado ou encerrado), apagada. */
.aj-cartao__resumo{min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font-size:13.5px;color:var(--tinta-secundaria)}
.aj-cartao__situacao--apagada{color:var(--tinta-apagada)}
.aj-tabela td.aj-tabela__apagada{color:var(--tinta-apagada)}

/* Controle segmentado que divide a largura (o tipo de cliente, no mobile). */
.aj-segmentado--largo{display:grid;grid-auto-flow:column;grid-auto-columns:minmax(0,1fr)}

/* Formulário: a grade de campos com o mesmo vão nas linhas e nas colunas no tablet; o endereço em seis colunas
   (duas no mobile); o campo curto (UF) com menos respiro no desktop. */
.aj-grade-endereco{display:grid;grid-template-columns:repeat(6,minmax(0,1fr));gap:var(--espaco-10) var(--espaco-12)}
.aj-grade-endereco__logradouro{grid-column:span 4}
.aj-grade-endereco__numero,.aj-grade-endereco__cep{grid-column:span 2}
.aj-grade-endereco__complemento,.aj-grade-endereco__bairro,.aj-grade-endereco__cidade{grid-column:span 3}
.aj-grade-endereco__uf{grid-column:span 1}
@media (min-width:761px) and (max-width:1199px){.aj-grade-campos--tablet-folgada,.aj-grade-endereco{gap:var(--espaco-12)}}
@media (max-width:760px){
  .aj-grade-endereco{grid-template-columns:repeat(2,minmax(0,1fr));gap:var(--espaco-12) var(--espaco-10)}
  .aj-grade-endereco__logradouro,.aj-grade-endereco__bairro,.aj-grade-endereco__cidade{grid-column:span 2}
  .aj-grade-endereco__numero,.aj-grade-endereco__complemento,.aj-grade-endereco__uf,.aj-grade-endereco__cep{grid-column:span 1}
}
@media (min-width:1200px){.aj-campo--curto{padding:0 var(--espaco-10)}}
[data-visao="desktop"] .aj-grade-endereco{grid-template-columns:repeat(6,minmax(0,1fr));gap:var(--espaco-10) var(--espaco-12)}
[data-visao="tablet"] .aj-grade-endereco{grid-template-columns:repeat(6,minmax(0,1fr));gap:var(--espaco-12)}
[data-visao="tablet"] .aj-grade-campos--tablet-folgada{gap:var(--espaco-12)}
[data-visao="desktop"] .aj-grade-endereco__logradouro,[data-visao="tablet"] .aj-grade-endereco__logradouro{grid-column:span 4}
[data-visao="desktop"] :is(.aj-grade-endereco__numero,.aj-grade-endereco__cep),[data-visao="tablet"] :is(.aj-grade-endereco__numero,.aj-grade-endereco__cep){grid-column:span 2}
[data-visao="desktop"] :is(.aj-grade-endereco__complemento,.aj-grade-endereco__bairro,.aj-grade-endereco__cidade),[data-visao="tablet"] :is(.aj-grade-endereco__complemento,.aj-grade-endereco__bairro,.aj-grade-endereco__cidade){grid-column:span 3}
[data-visao="desktop"] .aj-grade-endereco__uf,[data-visao="tablet"] .aj-grade-endereco__uf{grid-column:span 1}
[data-visao="mobile"] .aj-grade-endereco{grid-template-columns:repeat(2,minmax(0,1fr));gap:var(--espaco-12) var(--espaco-10)}
[data-visao="mobile"] :is(.aj-grade-endereco__logradouro,.aj-grade-endereco__bairro,.aj-grade-endereco__cidade){grid-column:span 2}
[data-visao="mobile"] :is(.aj-grade-endereco__numero,.aj-grade-endereco__complemento,.aj-grade-endereco__uf,.aj-grade-endereco__cep){grid-column:span 1}
[data-visao="desktop"] .aj-campo--curto{padding:0 var(--espaco-10)}
[data-visao="tablet"] .aj-campo--curto,[data-visao="mobile"] .aj-campo--curto{padding:0 var(--espaco-14)}

/* Vínculo com polo (o cliente do processo): o campo do polo branco, dentro do escolhido; o candidato da busca em
   cartão, com o botão "Adicionar". */
.aj-campo-filtro--polo{min-width:112px;background:var(--superficie)}
.aj-candidato{display:flex;align-items:center;justify-content:space-between;gap:var(--espaco-10);padding:var(--espaco-8) var(--espaco-10);border:1px solid var(--borda-opcao);border-radius:var(--raio-12)}
.aj-candidato__texto{display:flex;flex-direction:column;min-width:0}
.aj-candidato__titulo{font-weight:600;overflow-wrap:anywhere}
.aj-candidato__texto .aj-ajuda{font-variant-numeric:tabular-nums}
.aj-candidato>.aj-botao{flex:0 0 auto}

/* Opções marcáveis em grupo (quem acessa um documento) e o destaque dentro de uma ajuda ("Situação atual: Ativo"). */
.aj-opcoes{display:flex;flex-wrap:wrap;gap:var(--espaco-8)}
.aj-opcoes>.aj-opcao{flex:1 1 240px}
.aj-ajuda strong{color:var(--tinta)}
```

Confira por roteiro: chaves abertas e fechadas em número igual; toda `var(--…)` do bloco existe em `tokens.css` ou no próprio `bundle.css`; `.aj-pilha{display:contents}` vem antes da consulta de mídia que a torna coluna (mesma especificidade: a ordem decide); `.aj-linhas--folgadas>.aj-linha` vem depois de `.aj-linhas>.aj-linha`.

- [ ] **Step 3: README e prévia**

Componentes novos, cada um com `README.md` (primeira frase é o resumo; depois o que cada classe faz, as regras de uso e o que quem usa fornece) e `preview.html` (linha 1 `<!-- @dsCard group="…" height=… -->`, nos grupos que já existem: Estrutura, Conteúdo, Formulário, Filtros, Ações, Retorno):

| Pasta | Grupo | O que a prévia mostra | Regras de uso que o README registra |
|---|---|---|---|
| `Abas` | Estrutura | As abas "Dados", "Processos 2", "Documentos 10", "Usuários 1", com "Dados" escolhida, numa moldura de 358px | Só no mobile, nas fichas; uma aba por assunto da ficha, na ordem dos painéis; contador quando o assunto é uma lista; os painéis de cada aba em `aj-aba-painel` (com `--oculta` nos que não são da escolhida), que no tablet e no desktop mostra todos; `role="tablist"` com `aria-label` e `role="tab"` com `aria-selected` |
| `CabecalhoDeFicha` | Estrutura | O painel de cabeçalho com "Transportes Litoral Ltda.", o subtítulo e os botões "Inativar" e "Editar" (desktop); o mesmo no tablet, sem os botões; o bloco de título do mobile | Ficha de cliente e de processo; voltar acima do painel; ações só no desktop (no tablet e no mobile, na barra fixa); no mobile o título da barra é o assunto ("Cliente") e o nome vai para `aj-titulo-da-ficha`, em `h2` |

Componentes que ganham modificador (README emendado; prévia emendada onde já existe):

- Lado a lado: `--painel-primeiro` (a ficha do cliente, painel primeiro na marcação e à direita no desktop), `--ficha`, `--formulario` (coluna principal de 1,6), `aj-pilha` (coluna de painéis dentro do arranjo).
- Painel: `aj-painel--cabecalho` e `aj-painel__botoes` (veja Cabeçalho de ficha).
- Lista em painel: `aj-linhas--folgadas`, `aj-linha--link` (a linha inteira é o link para a ficha, com a seta), `aj-linha__lado`, o link do título na cor da marca.
- Campo: `aj-grade-endereco` e os sete filhos, `aj-grade-campos--tablet-folgada`, `aj-campo--curto`, `aj-opcoes`.
- Painel de filtros (a parte da barra e da linha): a busca na barra (`.aj-barra-filtros>.aj-campo-icone`), `aj-opcao--filtro` com `aj-opcao__caixa`, a busca na linha do tablet, `aj-chip--campo`.
- Vínculo: o escolhido com o polo (`aj-campo-filtro--polo`) e o botão de remover; o candidato (`aj-candidato`) com "Adicionar".
- Cartão de atividade: `aj-cartao__resumo`, `aj-cartao__situacao--apagada` (cartões de cliente e de processo).
- Tabela: `aj-tabela__apagada`.
- Segmentado: `aj-segmentado--largo`.
- Chip: `aj-chip--campo`.

`design-system.json`: só `lastChange` (`by` o usuário, `via` "Claude Code", `at` agora, `note` "Fase 9D: clientes e processos — ficha com cabeçalho e abas, endereço em grade, vínculo com polo, filtros com busca.").

- [ ] **Step 4: Publicar no design system e sincronizar o canvas**

Uma chamada de publicação no design system com os arquivos alterados e, por último na lista, o índice relido logo antes. Use o caminho longo do scratchpad em `root` e `file_path`. Anote a versão publicada no ledger. Depois, no canvas: `read` do artifact inteiro e uma publicação com `project/canvas.json` (só a versão e `copiedAt` do registro `designSystems` mudam) e, em `files`, as cópias no servidor `"project/ds/araujods/tokens.json": {"artifact": "https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS", "path": "project/tokens.json"}`, o mesmo para `project/tokens.css` e para `project/components/bundle.css` (em `project/ds/araujods/components/bundle.css`).

- [ ] **Step 5: Copiar para o código**

`wwwroot/css/componentes.css` = as duas primeiras linhas de cabeçalho que o arquivo já tem (a segunda diz "Cópia de components/bundle.css (…); altere lá primeiro.") + o `bundle.css` publicado, da linha "Visões:" em diante, com fim de linha CRLF. Confira por roteiro que o corpo é idêntico e que o `git diff` só acrescenta o bloco da fase. `tokens.css` não muda nesta fase.

- [ ] **Step 6: Build e commit**

Run: `dotnet build -m:1`
Expected: 0 avisos.

```
git add src/AtividadesJuridicas.Web/wwwroot/css/componentes.css
git commit -m "feat(web): design system com cabeçalho e abas de ficha, endereço em grade, vínculo com polo e filtros com busca"
```

---

### Task 2: bUnit e os testes de interação da tela de Documentos comuns

**Files:**
- Modify: `Directory.Packages.props` (`<PackageVersion Include="bunit" Version="2.11.3" />`)
- Modify: `tests/AtividadesJuridicas.IntegrationTests/AtividadesJuridicas.IntegrationTests.csproj` (`<PackageReference Include="bunit" />`)
- Modify: `tests/AtividadesJuridicas.IntegrationTests/DespachanteCombinado.cs`
- Create: `tests/AtividadesJuridicas.IntegrationTests/TelaInterativa.cs`
- Create: `tests/AtividadesJuridicas.IntegrationTests/DocumentosComunsInterativosTests.cs`

**Interfaces:**
- Consumes: `DocumentosComunsDaDiretoria` como está hoje (a tela de `/documentos-comuns` para a Diretoria), `ListarDocumentosComuns`, `EnviarDocumentoComum`, `RenomearDocumento`, `ExcluirDocumento`.
- Produces:
  - `TelaInterativa` (classe abstrata de teste, deriva de `Bunit.BunitContext`): registra o `FusoDoEscritorio("America/Sao_Paulo")`, um `Recados` e o `DespachanteCombinado` da propriedade `Despachante` (criado antes do construtor da base); `JSInterop.Mode = JSRuntimeMode.Loose`. Método `Desenhar<T>(Action<ComponentParameterCollectionBuilder<T>>? parametros = null)`, que chama `Render<T>`.
  - `DespachanteCombinado` ganha: `Recebidas` (`IReadOnlyList<object>`, as requisições na ordem em que chegaram), `Responde<TRequisicao, TResposta>(Func<TRequisicao, TResposta> resposta)` (a resposta calculada a partir da requisição) e `RespondeDepois<TRequisicao, TResposta>(Task<TResposta> resposta)` (a resposta chega quando o teste completar a tarefa). O `Responde` de hoje continua igual.

**Regras:**
- Esta tarefa não muda código da aplicação. Os testes descrevem o que a tela faz hoje e passam de primeira: são a rede para a Tarefa 4. O RED desta tarefa é o projeto não compilar antes do pacote e das peças de teste.
- Os testes não usam banco: sem `[Collection(ColecaoBanco.Nome)]`.
- O pacote com `CentralPackageTransitivePinningEnabled` pode pedir versões fixadas de dependências transitivas (AngleSharp, por exemplo) ou acusar conflito com as do ASP.NET Core 10.0.1. Resolva com o mínimo (um `PackageVersion` a mais, se preciso) e registre. Se a versão 2.11.3 não restaurar em `net10.0`, pare e relate antes de trocar de versão.
- A API do bUnit 2 (`BunitContext`, `Render`, `IRenderedComponent<T>`, `Find`, `FindAll`, `Click`, `Change`, `InputFile` com `UploadFiles` e `InputFileContent.CreateFromText`, `JSInterop.VerifyFocusAsyncInvoke`, `ShouldBeElementReferenceTo`, `WaitForAssertion`) é a usada abaixo. Onde a versão instalada tiver outro nome, ajuste o mínimo e registre.

- [ ] **Step 1: Pacote e peças de teste (o projeto não compila antes delas)**

`TelaInterativa.cs`:

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
/// escritório e os recados entre telas. O JavaScript fica solto (as chamadas de foco são registradas, não executadas)
/// e o teste confere o foco pelo registro.
/// </summary>
public abstract class TelaInterativa : BunitContext
{
    /// <summary>Os atores que <c>ObterMeuAcesso</c> devolve nos testes: uma diretora e um usuário de Produção.</summary>
    protected static readonly UsuarioAutenticado AtorDaDiretoria = new(
        Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d001"), "Diana Diretora", TipoPerfil.Diretoria, Guid.Parse("0199a3f2-7c1e-7000-8000-00000000e001"));
    protected static readonly UsuarioAutenticado AtorDaProducao = new(
        Guid.Parse("0199a3f2-7c1e-7000-8000-00000000d002"), "Pedro Produção", TipoPerfil.Producao, Guid.Parse("0199a3f2-7c1e-7000-8000-00000000e002"));

    protected DespachanteCombinado Despachante { get; } = new();

    protected TelaInterativa()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IDespachante>(Despachante);
        Services.AddSingleton(new FusoDoEscritorio("America/Sao_Paulo"));
        Services.AddSingleton(new Recados());
    }

    protected IRenderedComponent<T> Desenhar<T>(Action<ComponentParameterCollectionBuilder<T>>? parametros = null)
        where T : IComponent =>
        Render(parametros);
}
```

`DespachanteCombinado` (o arquivo inteiro):

```csharp
using AtividadesJuridicas.Application.Comum;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Despachante de teste que devolve a resposta combinada para cada tipo de requisição. Serve aos estados que o
/// banco de verdade não produz por encomenda (falha de consulta, acesso encerrado, resposta demorada) e guarda as
/// requisições recebidas, para o teste conferir o que a tela pediu.
/// </summary>
public sealed class DespachanteCombinado : IDespachante
{
    private readonly Dictionary<Type, Func<object, Task<object?>>> _respostas = [];
    private readonly List<object> _recebidas = [];

    /// <summary>As requisições na ordem em que chegaram.</summary>
    public IReadOnlyList<object> Recebidas => _recebidas;

    public DespachanteCombinado Responde<TRequisicao, TResposta>(TResposta resposta)
        where TRequisicao : IRequisicao<TResposta> =>
        Responde<TRequisicao, TResposta>(_ => resposta);

    /// <summary>A resposta calculada a partir da requisição (a página pedida, a categoria).</summary>
    public DespachanteCombinado Responde<TRequisicao, TResposta>(Func<TRequisicao, TResposta> resposta)
        where TRequisicao : IRequisicao<TResposta>
    {
        _respostas[typeof(TRequisicao)] = requisicao => Task.FromResult<object?>(resposta((TRequisicao)requisicao));
        return this;
    }

    /// <summary>A resposta chega quando o teste completar a tarefa: o que a tela mostra enquanto espera.</summary>
    public DespachanteCombinado RespondeDepois<TRequisicao, TResposta>(Task<TResposta> resposta)
        where TRequisicao : IRequisicao<TResposta>
    {
        _respostas[typeof(TRequisicao)] = async _ => await resposta;
        return this;
    }

    public async Task<TResposta> EnviarAsync<TResposta>(IRequisicao<TResposta> requisicao, CancellationToken ct = default)
    {
        _recebidas.Add(requisicao);
        if (!_respostas.TryGetValue(requisicao.GetType(), out var resposta))
            throw new InvalidOperationException($"Nenhuma resposta combinada para {requisicao.GetType().Name}.");
        return (TResposta)(await resposta(requisicao))!;
    }
}
```

O `EnviarAsync` de hoje lança a exceção de forma síncrona; o novo, dentro da tarefa. Os testes que esperam a exceção (procure por "Nenhuma resposta combinada") continuam passando, porque todos aguardam a tarefa; se algum não aguardar, ajuste o teste para aguardar e registre.

Run: `dotnet build -m:1`
Expected: depois do pacote e das duas peças, 0 avisos. Antes delas o projeto não compila (é o RED).

- [ ] **Step 2: Os testes de interação**

`DocumentosComunsInterativosTests.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Web.Components.Documentos;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O que a Diretoria faz na tela de Documentos comuns, clicando: o envio com confirmação, o renomear e a exclusão,
/// uma coisa aberta por vez, e o foco no que abre. É a rede da mudança na lógica de documentos (fase 9D).
/// </summary>
public class DocumentosComunsInterativosTests : TelaInterativa
{
    private static readonly Guid Contrato = Guid.Parse("0199a3f2-7c1e-7000-8000-000000000001");
    private static readonly Guid Modelo = Guid.Parse("0199a3f2-7c1e-7000-8000-000000000002");

    private static Resultado<Pagina<DocumentoDoPortal>> Lista() => Resultado<Pagina<DocumentoDoPortal>>.Ok(new([
        new DocumentoDoPortal(Contrato, "Modelo de contrato.pdf", 96 * 1024, new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc), "Diana Diretora"),
        new DocumentoDoPortal(Modelo, "Modelo de procuração.docx", 40 * 1024, new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc), "Diana Diretora"),
    ], 2, 1, 20));

    private IRenderedComponent<DocumentosComunsDaDiretoria> Tela()
    {
        Despachante.Responde<ListarDocumentosComuns, Resultado<Pagina<DocumentoDoPortal>>>(Lista());
        return Desenhar<DocumentosComunsDaDiretoria>();
    }

    private static void Escolher(IRenderedComponent<DocumentosComunsDaDiretoria> tela, string nome = "Tabela de honorários.pdf") =>
        tela.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("conteúdo", nome));

    // O botão do cabeçalho do desktop (o flutuante do mobile chama o mesmo método).
    private static void AbrirEnvio(IRenderedComponent<DocumentosComunsDaDiretoria> tela) =>
        tela.Find("header.aj-cabecalho button.aj-botao--primario").Click();

    private static void ClicarNaLinha(IRenderedComponent<DocumentosComunsDaDiretoria> tela, string texto, int linha = 0) =>
        tela.FindAll("table.aj-tabela tbody tr")[linha].QuerySelectorAll("button").Single(b => b.TextContent == texto).Click();

    [Fact]
    public void Enviar_documento_abre_o_painel_e_leva_o_foco_ao_campo_do_arquivo()
    {
        var tela = Tela();

        AbrirEnvio(tela);

        tela.Find("section.aj-painel h2").TextContent.Should().Be("Enviar documento");
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(tela.Find("input[type=file]"));
    }

    [Fact]
    public void Enviar_pede_a_confirmacao_e_esconde_o_painel_e_cancelar_o_traz_de_volta_com_o_arquivo()
    {
        var tela = Tela();
        AbrirEnvio(tela);
        Escolher(tela);

        tela.FindAll("section.aj-painel button").Single(b => b.TextContent == "Enviar").Click();

        tela.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar envio");
        tela.Find("[role=alertdialog] strong").TextContent.Should().Be("Tabela de honorários.pdf · 8 B");
        tela.Find("section.aj-painel").HasAttribute("hidden").Should().BeTrue();

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Cancelar").Click();

        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
        tela.Find("section.aj-painel").HasAttribute("hidden").Should().BeFalse();
        tela.FindAll("section.aj-painel button").Single(b => b.TextContent == "Enviar").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Confirmar_envio_manda_com_a_visibilidade_confirmada_fecha_o_painel_e_avisa()
    {
        Despachante.Responde<EnviarDocumentoComum, Resultado<Guid>>(Resultado<Guid>.Ok(Guid.NewGuid()));
        var tela = Tela();
        AbrirEnvio(tela);
        Escolher(tela);
        tela.FindAll("section.aj-painel button").Single(b => b.TextContent == "Enviar").Click();

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar envio").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Documento enviado."));
        Despachante.Recebidas.OfType<EnviarDocumentoComum>().Should().ContainSingle()
            .Which.Should().Match<EnviarDocumentoComum>(e => e.ConfirmouVisibilidade && e.NomeArquivo == "Tabela de honorários.pdf");
        tela.FindAll("section.aj-painel").Should().BeEmpty();
        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
    }

    [Fact]
    public void Uma_coisa_aberta_por_vez_entre_envio_renomear_e_exclusao()
    {
        var tela = Tela();

        AbrirEnvio(tela);
        ClicarNaLinha(tela, "Renomear");
        tela.FindAll("section.aj-painel h2").Select(h => h.TextContent).Should().Equal("Renomear \"Modelo de contrato.pdf\"");
        JSInterop.VerifyFocusAsyncInvoke(calledTimes: 2)[1].Arguments[0]
            .ShouldBeElementReferenceTo(tela.Find("section.aj-painel input.aj-campo"));

        ClicarNaLinha(tela, "Excluir", linha: 1);
        tela.FindAll("section.aj-painel").Should().BeEmpty();
        tela.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar exclusão");
        tela.Find("[role=alertdialog] strong").TextContent.Should().Be("Confirmar a exclusão de Modelo de procuração.docx?");

        AbrirEnvio(tela);
        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
        tela.Find("section.aj-painel h2").TextContent.Should().Be("Enviar documento");
    }

    [Fact]
    public void Cancelar_o_envio_e_reabrir_comeca_sem_o_arquivo()
    {
        var tela = Tela();
        AbrirEnvio(tela);
        Escolher(tela);

        tela.FindAll("section.aj-painel button").Single(b => b.TextContent == "Cancelar").Click();
        AbrirEnvio(tela);

        tela.FindAll("section.aj-painel button").Single(b => b.TextContent == "Enviar").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Com_uma_exclusao_em_curso_nada_se_abre_e_as_acoes_das_linhas_esperam()
    {
        var exclusao = new TaskCompletionSource<Resultado>();
        Despachante.RespondeDepois<ExcluirDocumento, Resultado>(exclusao.Task);
        var tela = Tela();
        ClicarNaLinha(tela, "Excluir");

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar exclusão").Click();

        tela.FindAll("table.aj-tabela tbody button").Should().OnlyContain(b => b.HasAttribute("disabled"));
        AbrirEnvio(tela);
        tela.FindAll("section.aj-painel").Should().BeEmpty();

        exclusao.SetResult(Resultado.Ok());
        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Documento excluído."));
        tela.FindAll("table.aj-tabela tbody button").Should().OnlyContain(b => !b.HasAttribute("disabled"));
    }

    [Fact]
    public void Salvar_nome_renomeia_fecha_o_painel_e_avisa()
    {
        Despachante.Responde<RenomearDocumento, Resultado>(Resultado.Ok());
        var tela = Tela();
        ClicarNaLinha(tela, "Renomear");

        tela.Find("section.aj-painel input.aj-campo").Change("Contrato padrão.pdf");
        tela.FindAll("section.aj-painel button").Single(b => b.TextContent == "Salvar nome").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Nome alterado."));
        Despachante.Recebidas.OfType<RenomearDocumento>().Should().ContainSingle()
            .Which.Should().Be(new RenomearDocumento(Contrato, "Contrato padrão.pdf"));
        tela.FindAll("section.aj-painel").Should().BeEmpty();
    }
}
```

O tamanho do arquivo de texto "conteúdo" em UTF-8 é 9 bytes ("ú" ocupa dois); confira o que `TamanhoDeArquivo.Formatar` escreve para ele e use o texto exato no teste do título da confirmação (o teste acima diz `8 B` como exemplo: troque pelo valor certo, sem mudar o que o teste prova). Se `Resultado.Ok()` tiver outro nome no projeto, use o que existe.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DocumentosComunsInterativosTests|FullyQualifiedName~DocumentosDoPortalWebTests|FullyQualifiedName~DocumentosDaFichaTests"`
Expected: 0 avisos; todos passando, sem mudar código da aplicação. Um teste que falhar aqui descreve errado a tela de hoje (ou achou um defeito): pare e relate, com a saída, antes de mexer na tela.

- [ ] **Step 3: Commit**

```
git add Directory.Packages.props tests
git commit -m "test: bUnit no projeto de integração e os testes de interação da tela de Documentos comuns da Diretoria"
```

---

### Task 3: Contadores das listagens de clientes, processos e documentos do cliente

**Files:**
- Modify: `src/AtividadesJuridicas.Application/Comum/Pagina.cs`
- Modify: `src/AtividadesJuridicas.Application/Cadastros/PortasDeClientes.cs`, `ListarClientes.cs`
- Modify: `src/AtividadesJuridicas.Application/Cadastros/PortasDeProcessos.cs`, `ListarProcessos.cs`
- Modify: `src/AtividadesJuridicas.Application/Documentos/PortasDeDocumentos.cs`, `ListarDocumentosDoCliente.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/ClienteRepositorio.cs`, `ProcessoRepositorio.cs`, `DocumentoRepositorio.cs`
- Modify: quem tipa a resposta como `Resultado<Pagina<ClienteResumo>>`, `Resultado<Pagina<ProcessoResumo>>` ou `Resultado<Pagina<DocumentoResumo>>` (o compilador aponta; nos testes, os `Responde<ListarClientes, …>` e afins)
- Test: `tests/AtividadesJuridicas.IntegrationTests/ClientesTests.cs`, `ProcessosTests.cs`, `DocumentosTests.cs`

**Interfaces:**
- Consumes: `IContextoDeAcesso`, `FiltroDeAcesso`, as consultas de hoje.
- Produces:
  - `public record Pagina<T>(IReadOnlyList<T> Itens, int Total, int Numero, int Tamanho)` (deixa de ser `sealed`; `TotalDePaginas` igual).
  - `public sealed record PaginaDeClientes(IReadOnlyList<ClienteResumo> Itens, int Total, int Numero, int Tamanho, int NoCadastro, int Inativos) : Pagina<ClienteResumo>(Itens, Total, Numero, Tamanho);` — `NoCadastro`: todos os clientes, ativos e inativos; `Inativos`: os inativos. Os dois não olham a busca nem o `IncluirInativos`.
  - `ListarClientes : IRequisicao<Resultado<PaginaDeClientes>>`.
  - `public sealed record ContagemDeClientes(int Total, int Inativos);` e `IClienteRepositorio.ContarAsync(CancellationToken ct) : Task<ContagemDeClientes>`.
  - `public sealed record PaginaDeProcessos(IReadOnlyList<ProcessoResumo> Itens, int Total, int Numero, int Tamanho, int TodasAsSituacoes) : Pagina<ProcessoResumo>(Itens, Total, Numero, Tamanho);` — `TodasAsSituacoes`: quantos processos a busca e o cliente encontram, em qualquer situação.
  - `ListarProcessos : IRequisicao<Resultado<PaginaDeProcessos>>`; `IProcessoRepositorio.ContarAsync(FiltroDeProcessos filtro, CancellationToken ct) : Task<int>`.
  - `public sealed record PaginaDeDocumentos(IReadOnlyList<DocumentoResumo> Itens, int Total, int Numero, int Tamanho, IReadOnlyDictionary<CategoriaDocumento, int> PorCategoria) : Pagina<DocumentoResumo>(Itens, Total, Numero, Tamanho);` — `PorCategoria`: para cada área que o usuário pode ler naquele cliente, quantos documentos ele lê nela; área que ele não lê não aparece. Diretoria: as quatro (`DocumentoGeral`, `ContratoHonorarios`, `DocumentoAtividade`, `DocumentoDoCliente`), com zero quando vazias. Produção: `DocumentoGeral` e `DocumentoAtividade`, contando só o que o filtro de acesso dela deixa ver.
  - `ListarDocumentosDoCliente : IRequisicao<Resultado<PaginaDeDocumentos>>`; `IDocumentoRepositorio.ContarDoClienteAsync(Guid clienteId, FiltroDeAcesso? acesso, CancellationToken ct) : Task<IReadOnlyDictionary<CategoriaDocumento, int>>`.

**Regras:**
- Nenhuma consulta auditada a mais: a contagem vai dentro do manipulador da listagem, que já registra a consulta uma vez.
- O filtro de acesso da Produção nos documentos é o mesmo da listagem: extraia o `Where` de `ListarDoClienteAsync` para um método privado usado pelas duas consultas.
- `ContarAsync` de processos recebe o filtro com `Status = null` (o manipulador faz `filtro with { Status = null }`); a busca e o cliente valem. Extraia a montagem da consulta de `ListarAsync` para um método privado usado pelas duas.
- As respostas de falha continuam as de hoje (acesso negado, categoria inválida).

- [ ] **Step 1: Testes (devem falhar)**

Em `ClientesTests` (com os auxiliares do arquivo):

```csharp
    [Fact]
    public async Task Listar_clientes_traz_o_cadastro_e_os_inativos_sem_olhar_a_busca()
    {
        // Três clientes, um inativo; a busca acha um só, e a lista sem inativos tem dois.
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        await CriarPfAsync(adminId, "Ana Lima", "529.982.247-25");
        await CriarPfAsync(adminId, "Bruno Reis", "111.444.777-35");
        var inativo = await CriarPfAsync(adminId, "Carlos Dias", "390.533.447-05");
        (await _app.ExecutarAsync(adminId, new DefinirAtivoCliente(inativo, false))).Sucesso.Should().BeTrue();
        var pedroId = await _app.CriarPedroAsync();

        var busca = (await _app.ExecutarAsync(adminId, new ListarClientes("Ana"))).Valor!;
        var semInativos = (await _app.ExecutarAsync(pedroId, new ListarClientes())).Valor!;

        busca.Itens.Select(c => c.Nome).Should().Equal("Ana Lima");
        busca.NoCadastro.Should().Be(3);
        busca.Inativos.Should().Be(1);
        semInativos.Total.Should().Be(2);
        semInativos.NoCadastro.Should().Be(3);
        semInativos.Inativos.Should().Be(1);
    }
```

(Se o arquivo não tiver `CriarPfAsync`, escreva-o com `SalvarClientePessoaFisica` devolvendo o `Guid`, no estilo dos auxiliares que existem; os CPFs acima são válidos.)

Em `ProcessosTests`:

```csharp
    [Fact]
    public async Task Listar_processos_traz_o_total_de_todas_as_situacoes_com_a_busca()
    {
        // "Cobrança" acha dois processos, um ativo e um suspenso; o terceiro não é achado.
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var cobrancaA = await CriarProcessoAsync(adminId, "Cobrança A");
        var cobrancaB = await CriarProcessoAsync(adminId, "Cobrança B");
        await CriarProcessoAsync(adminId, "Inventário");
        (await _app.ExecutarAsync(adminId, new AlterarStatusProcesso(cobrancaB, StatusProcesso.Suspenso))).Sucesso.Should().BeTrue();
        var pedroId = await _app.CriarPedroAsync();

        var suspensos = (await _app.ExecutarAsync(pedroId, new ListarProcessos("Cobrança", Status: StatusProcesso.Suspenso))).Valor!;
        var todos = (await _app.ExecutarAsync(adminId, new ListarProcessos())).Valor!;

        suspensos.Itens.Select(p => p.Id).Should().Equal(cobrancaB);
        suspensos.TodasAsSituacoes.Should().Be(2);
        todos.TodasAsSituacoes.Should().Be(3);
        cobrancaA.Should().NotBe(Guid.Empty);
    }
```

(Com o auxiliar de criação de processo do arquivo, ou um `CriarProcessoAsync` escrito com `SalvarProcesso` no estilo dos que existem.)

Em `DocumentosTests`:

```csharp
    [Fact]
    public async Task Listar_documentos_do_cliente_conta_por_area_o_que_cada_um_le()
    {
        // Para a Maria: dois gerais (um com acesso do Pedro), um contrato e um do cliente.
        var adminId = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var (_, maria) = await _app.CriarCarlaAsync();
        var pedroId = await _app.CriarPedroAsync();
        var geralDoPedro = await EnviarAsync(adminId, CategoriaDocumento.DocumentoGeral, maria);
        await EnviarAsync(adminId, CategoriaDocumento.DocumentoGeral, maria);
        await EnviarAsync(adminId, CategoriaDocumento.ContratoHonorarios, maria);
        await EnviarAsync(adminId, CategoriaDocumento.DocumentoDoCliente, maria);
        (await _app.ExecutarAsync(adminId, new DefinirAcessoDoDocumento(geralDoPedro, [], [pedroId]))).Sucesso.Should().BeTrue();

        var daDiretoria = (await _app.ExecutarAsync(adminId,
            new ListarDocumentosDoCliente(maria, CategoriaDocumento.ContratoHonorarios))).Valor!;
        var daProducao = (await _app.ExecutarAsync(pedroId,
            new ListarDocumentosDoCliente(maria, CategoriaDocumento.DocumentoGeral))).Valor!;

        daDiretoria.PorCategoria.Should().BeEquivalentTo(new Dictionary<CategoriaDocumento, int>
        {
            [CategoriaDocumento.DocumentoGeral] = 2, [CategoriaDocumento.ContratoHonorarios] = 1,
            [CategoriaDocumento.DocumentoAtividade] = 0, [CategoriaDocumento.DocumentoDoCliente] = 1,
        });
        daDiretoria.Itens.Should().ContainSingle();
        daProducao.PorCategoria.Should().BeEquivalentTo(new Dictionary<CategoriaDocumento, int>
        {
            [CategoriaDocumento.DocumentoGeral] = 1, [CategoriaDocumento.DocumentoAtividade] = 0,
        });
        daProducao.Itens.Select(d => d.Id).Should().Equal(geralDoPedro);
    }
```

(`EnviarAsync` é um auxiliar que envia um arquivo pequeno com `EnviarDocumento` e devolve o `Guid`; use o do arquivo, se houver.)

Run: `dotnet build -m:1`
Expected: não compila (`NoCadastro`, `TodasAsSituacoes` e `PorCategoria` não existem). É o RED.

- [ ] **Step 2: Implementar**

`Pagina.cs`: tire o `sealed`. As três derivadas e as portas como em "Produces". Os manipuladores:

```csharp
        var pagina = await clientes.ListarAsync(…);   // como hoje
        var contagem = await clientes.ContarAsync(ct);
        return Resultado<PaginaDeClientes>.Ok(new PaginaDeClientes(
            [.. pagina.Itens.Select(ClienteMapeamento.ParaResumo)], pagina.Total, pagina.Numero, pagina.Tamanho,
            contagem.Total, contagem.Inativos));
```

```csharp
        var filtro = new FiltroDeProcessos(requisicao.Busca, requisicao.ClienteId, requisicao.Status);
        var pagina = await processos.ListarAsync(filtro, …);
        var todas = await processos.ContarAsync(filtro with { Status = null }, ct);
        return Resultado<PaginaDeProcessos>.Ok(new PaginaDeProcessos(pagina.Itens, pagina.Total, pagina.Numero, pagina.Tamanho, todas));
```

```csharp
        var pagina = await documentos.ListarDoClienteAsync(…);   // como hoje
        var porCategoria = await documentos.ContarDoClienteAsync(requisicao.ClienteId, acesso, ct);
        return Resultado<PaginaDeDocumentos>.Ok(new PaginaDeDocumentos(pagina.Itens, pagina.Total, pagina.Numero, pagina.Tamanho, porCategoria));
```

`DocumentoRepositorio.ContarDoClienteAsync`: agrupa por categoria os documentos do cliente (sem os comuns), com o mesmo filtro de acesso da listagem quando `acesso` não é nulo; completa com zero as áreas legíveis que vieram vazias (as quatro para a Diretoria, `acesso` nulo; Geral e Atividade para os demais). `ClienteRepositorio.ContarAsync`: duas contagens no `db.Clientes` (todos; `!c.Ativo`).

Ajuste quem tipava a resposta como `Pagina<…>` dentro de `Resultado<…>` (o compilador aponta). Quem só lê `resultado.Valor?.Itens`, `Total` ou `TotalDePaginas` não muda.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ClientesTests|FullyQualifiedName~ProcessosTests|FullyQualifiedName~DocumentosTests|FullyQualifiedName~DocumentosDeAtividadeTests|FullyQualifiedName~DocumentosDoPortalTests|FullyQualifiedName~PortalDoClienteTests|FullyQualifiedName~PortaoDoClienteTests|FullyQualifiedName~PerfisTests|FullyQualifiedName~RegistroDeAcessosTests"`
Expected: 0 avisos; todos passando.

- [ ] **Step 3: Commit**

```
git add src tests
git commit -m "feat: listagens de clientes, processos e documentos do cliente com os contadores das telas"
```

---

### Task 4: Uma coisa aberta por vez na base de documentos

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Documentos/GestaoDeDocumentos.cs`
- Modify: `src/AtividadesJuridicas.Web/Components/Documentos/DocumentosComunsDaDiretoria.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Documentos/DocumentosParaBaixar.razor` (só os auxiliares extraídos, se ele tiver os mesmos)
- Create: `src/AtividadesJuridicas.Web/Infra/TextosDeDocumento.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/TelasPurasTests.cs`

**Interfaces:**
- Consumes: os testes da Tarefa 2 (a rede) e `PaginaDeDocumentos.PorCategoria` (Tarefa 3).
- Produces, em `GestaoDeDocumentos` (protegidos, para as telas que a herdam):
  - `bool _envioAberto`; `bool Ocupada` (`_enviando || _removendo`).
  - `void AbrirEnvio()`: com `Ocupada`, nada; senão cancela a exclusão, apaga o aviso, fecha o renomear e o acesso, abre o envio e pede o foco do painel.
  - `void RenomearLinha(Linha documento)`: com `Ocupada`, nada; senão cancela a exclusão, descarta o envio aberto, fecha o acesso, abre o renomear e pede o foco do painel.
  - `void ExcluirLinha(Guid id)`: com `Ocupada`, nada; senão descarta o envio aberto, fecha o renomear e o acesso e pede a exclusão.
  - `Task AbrirAcessoDaLinhaAsync(Guid id)`: com `Ocupada`, nada; senão cancela a exclusão, descarta o envio aberto, e segue como o `AbrirAcessoAsync` de hoje, pedindo o foco do painel quando ele abre.
  - `void DescartarEnvio()`: cancela a confirmação, esquece o arquivo e o nome, recria o campo de arquivo (`_versaoDoCampo++`) e fecha o envio. Junta a limpeza que hoje mora em `FecharEnvio` da tela e a do fim de um envio.
  - `IReadOnlyDictionary<CategoriaDocumento, int> _porCategoria` (vazio até a primeira leitura bem-sucedida; atualizado a cada leitura da lista de um cliente).
  - `protected virtual ValueTask FocarPainelAbertoAsync()` (padrão: nada), chamada pela base no `OnAfterRenderAsync` depois de uma abertura; `protected virtual Task AoCarregarAsync()` (padrão: nada), chamada no fim de cada leitura bem-sucedida da lista.
  - `AoConcluirEnvio()` passa a ter `DescartarEnvio()` como padrão.
- Produces, em `TextosDeDocumento`: `Endereco(Guid id) : string` (`/documentos/{id}/baixar`) e `Detalhe(long tamanhoBytes, DateTime enviadoEm, string? enviadoPor, FusoDoEscritorio fuso) : string` (as partes preenchidas unidas por " · ").

**Regras:**
- O comportamento da tela de Documentos comuns não muda: os testes da Tarefa 2 passam sem mudar uma linha, e os de `DocumentosDoPortalWebTests` e `DocumentosDaFichaTests` também.
- `DocumentosComunsDaDiretoria` passa a chamar os métodos da base nos botões (`AbrirEnvio`, `RenomearLinha`, `ExcluirLinha`, `DescartarEnvio` no "Cancelar" do envio) e perde os seus `_envioAberto`, `_focarPainel`, `Ocupada`, `AbrirEnvio`, `RenomearLinha`, `ExcluirLinha`, `FecharEnvio`, `Endereco`, `Detalhe` e o `OnAfterRenderAsync`; fica com o `FocarPainelAbertoAsync` (o campo do renomear ou o de arquivo, como hoje) e com o `FocarAvisoDeEnvioAsync` (vazio, como hoje).
- O `OnAfterRenderAsync` mora só na base (spec 15). Quem precisar de mais coisa depois de desenhar sobrescreve o `FocarPainelAbertoAsync`, não o `OnAfterRenderAsync`.
- `DocumentosDoCliente.razor` (o antigo) continua compilando e com o mesmo HTML até a Tarefa 7 o apagar; os testes dele ficam verdes.

- [ ] **Step 1: Testes (devem falhar)**

Em `TelasPurasTests`:

```csharp
public class TextosDeDocumentoTests
{
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");

    [Fact]
    public void Endereco_leva_ao_download()
    {
        TextosDeDocumento.Endereco(Guid.Parse("0199a3f2-7c1e-7000-8000-000000000001"))
            .Should().Be("/documentos/0199a3f2-7c1e-7000-8000-000000000001/baixar");
    }

    [Fact]
    public void Detalhe_junta_tamanho_data_e_autor_sem_separador_solto()
    {
        var quando = new DateTime(2026, 9, 2, 13, 14, 0, DateTimeKind.Utc);

        TextosDeDocumento.Detalhe(412 * 1024, quando, "Marina Couto", _fuso).Should().Be("412 KB · 02/09/2026 10:14 · Marina Couto");
        TextosDeDocumento.Detalhe(412 * 1024, quando, null, _fuso).Should().Be("412 KB · 02/09/2026 10:14");
    }
}
```

E, em `DocumentosComunsInterativosTests`, a prova de que o acesso também entra na regra (a tela de comuns não tem "Acesso"; o teste usa a base pela tela da Tarefa 7, então fica para lá). Nesta tarefa o RED é o de `TextosDeDocumento`.

Run: `dotnet build -m:1`
Expected: não compila (`TextosDeDocumento` não existe).

- [ ] **Step 2: `TextosDeDocumento`**

```csharp
namespace AtividadesJuridicas.Web.Infra;

/// <summary>O endereço de download e a linha de dados de um documento, iguais em todas as listas de documentos.</summary>
public static class TextosDeDocumento
{
    // Link fora da navegação do Blazor: é um download, não uma página. Quem o usa põe "download" e
    // data-enhance-nav="false" no link: é o "download" que impede o roteador de interceptar o clique.
    public static string Endereco(Guid id) => $"/documentos/{id}/baixar";

    public static string Detalhe(long tamanhoBytes, DateTime enviadoEm, string? enviadoPor, FusoDoEscritorio fuso) =>
        string.Join(" · ", new[] { TamanhoDeArquivo.Formatar(tamanhoBytes), fuso.Formatar(enviadoEm), enviadoPor }
            .Where(parte => !string.IsNullOrWhiteSpace(parte)));
}
```

- [ ] **Step 3: A base**

Em `GestaoDeDocumentos`, os campos e métodos de "Produces", com estes corpos (o resto da classe fica):

```csharp
    protected bool _envioAberto;

    // Um painel acabou de abrir e o campo dele ainda espera o foco: pedido uma vez por abertura, não a cada desenho.
    private bool _focarPainel;

    protected IReadOnlyDictionary<CategoriaDocumento, int> _porCategoria = new Dictionary<CategoriaDocumento, int>();

    // No máximo uma coisa aberta por vez entre o envio, o renomear, o acesso e a confirmação de exclusão: cada abertura
    // fecha as outras. Com um envio ou uma exclusão em curso nada se abre, para não desmontar o que a ação usa.
    protected bool Ocupada => _enviando || _removendo;

    protected void AbrirEnvio()
    {
        if (Ocupada) return;

        CancelarExclusao();
        _mensagem = null;
        _renomeando = null;
        _acesso = null;
        _envioAberto = true;
        _focarPainel = true;
    }

    protected void RenomearLinha(Linha documento)
    {
        if (Ocupada) return;

        CancelarExclusao();
        if (_envioAberto) DescartarEnvio();
        AbrirRenomear(documento);   // já fecha o acesso
        _focarPainel = true;
    }

    protected void ExcluirLinha(Guid id)
    {
        if (Ocupada) return;

        if (_envioAberto) DescartarEnvio();
        FecharRenomear();
        FecharAcesso();
        PedirExclusao(id);
    }

    protected async Task AbrirAcessoDaLinhaAsync(Guid id)
    {
        if (Ocupada) return;

        CancelarExclusao();
        if (_envioAberto) DescartarEnvio();
        await AbrirAcessoAsync(id);   // já fecha o renomear
        if (_acesso is not null) _focarPainel = true;
    }

    // O arquivo escolhido não é guardado: reabrir o envio começa do zero.
    protected void DescartarEnvio()
    {
        _confirmacao.Cancelar();
        _arquivo = null;
        _nomeDoEnvio = null;
        _versaoDoCampo++;
        _envioAberto = false;
    }

    protected virtual ValueTask FocarPainelAbertoAsync() => ValueTask.CompletedTask;

    protected virtual Task AoCarregarAsync() => Task.CompletedTask;

    /// <summary>Um envio acabou de dar certo: fecha limpando (outro arquivo escolhido enquanto este subia não fica).</summary>
    protected virtual void AoConcluirEnvio() => DescartarEnvio();
```

O `OnAfterRenderAsync` da base passa a tratar os dois pedidos:

```csharp
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focarAviso)
        {
            _focarAviso = false;
            await TentarFocarAsync(FocarAvisoDeEnvioAsync);
        }

        if (_focarPainel)
        {
            _focarPainel = false;
            await TentarFocarAsync(FocarPainelAbertoAsync);
        }
    }

    // Sem foco (circuito caindo, por exemplo) o que abriu continua visível e funcional.
    private static async Task TentarFocarAsync(Func<ValueTask> focar)
    {
        try
        {
            await focar();
        }
        catch (Exception ex) when (ex is InvalidOperationException or JSException or JSDisconnectedException)
        {
        }
    }
```

No `CarregarAsync` da base, no ramo do cliente, depois de ler: `if (doCliente.Valor is PaginaDeDocumentos lidos) _porCategoria = lidos.PorCategoria;`. No fim do `CarregarAsync`, depois de tratar a página além do fim, quando a leitura deu certo: `await AoCarregarAsync();` (uma vez por leitura que chegou ao fim; a chamada recursiva para a última página chama de novo, e isso basta: a tela recebe a contagem da leitura que ficou). O bloco que hoje limpa o arquivo dentro de `EnviarArquivoAsync` (`if (ReferenceEquals(_arquivo, arquivo)) { … }`) fica como está: ele preserva a escolha nova feita durante o envio, e o `AoConcluirEnvio` de cada tela decide se descarta.

Atenção: o `AoConcluirEnvio` padrão descarta tudo, inclusive um arquivo novo escolhido durante o envio. É o que a tela de comuns faz hoje (`AoConcluirEnvio() => FecharEnvio()`). O componente antigo `DocumentosDoCliente` mantinha o campo de envio sempre à vista e não sobrescrevia: para ele não mudar até a Tarefa 7, sobrescreva lá `protected override void AoConcluirEnvio() { }`.

- [ ] **Step 4: A tela de comuns usa a base**

Em `DocumentosComunsDaDiretoria.razor`: os botões chamam `AbrirEnvio`, `RenomearLinha(documento)`, `ExcluirLinha(documento.Id)`; o "Cancelar" do painel de envio chama `DescartarEnvio`; os links usam `TextosDeDocumento.Endereco(documento.Id)`; a linha do tablet usa `TextosDeDocumento.Detalhe(documento.TamanhoBytes, documento.EnviadoEm, documento.EnviadoPor, Fuso)`. Saem da tela os membros listados nas Regras. Fica:

```csharp
    private InputFile? _campoDeArquivo;
    private ElementReference _campoDoNovoNome;

    protected override CategoriaDocumento CategoriaDosDocumentos => CategoriaDocumento.DocumentoComum;
    protected override Guid? ClienteDosDocumentos => null;

    // As confirmações desta tela são avisos com rótulo, que levam o foco a si mesmos quando aparecem.
    protected override ValueTask FocarAvisoDeEnvioAsync() => ValueTask.CompletedTask;

    // Uma coisa aberta por vez: o campo é o do painel que está na tela.
    protected override ValueTask FocarPainelAbertoAsync() =>
        _renomeando is not null ? _campoDoNovoNome.FocusAsync()
        : _envioAberto && _campoDeArquivo?.Element is { } campo ? campo.FocusAsync()
        : ValueTask.CompletedTask;
```

(e `RotuloDoArquivo`, `Quantos`, `AExcluir`, `TituloDoEnvio`, como hoje). Se `DocumentosParaBaixar.razor` tiver um `Endereco` ou um `Detalhe` iguais, troque pelos de `TextosDeDocumento`; se forem diferentes, deixe.

- [ ] **Step 5: Testes**

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DocumentosComunsInterativosTests|FullyQualifiedName~DocumentosDoPortalWebTests|FullyQualifiedName~DocumentosDaFichaTests|FullyQualifiedName~TextosDeDocumentoTests|FullyQualifiedName~DocumentosDeAtividadeTests"`
Expected: 0 avisos; todos passando, os da Tarefa 2 sem nenhuma mudança.

- [ ] **Step 6: Commit**

```
git add src tests
git commit -m "refactor(web): uma coisa aberta por vez e o foco do painel na base de documentos, com os textos de documento extraídos"
```

---

### Task 5: Peças compartilhadas das fichas e dos formulários

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Layout/BarraDeVoltar.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/CabecalhoDePagina.razor` (usa a `BarraDeVoltar`)
- Create: `src/AtividadesJuridicas.Web/Components/Layout/CabecalhoDeFicha.razor`
- Create: `src/AtividadesJuridicas.Web/Components/Layout/Abas.razor` e `src/AtividadesJuridicas.Web/Components/Layout/Aba.cs`
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/RotuloDeCampo.razor` (parâmetro `Classe`)
- Create: `src/AtividadesJuridicas.Web/Infra/RotulosDeProcesso.cs`
- Modify: `src/AtividadesJuridicas.Web/Infra/TextosDoCliente.cs`
- Create: `src/AtividadesJuridicas.Web/Infra/ValorEmReais.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/ComponentesDoDesignSystemTests.cs`, `TelasPurasTests.cs`
- Create: `tests/AtividadesJuridicas.IntegrationTests/AbasInterativasTests.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: `Encaixes.BarraSuperior`, `Icone` (`voltar`), `ClienteResumo`, `ClienteDetalhe`, `StatusProcesso`, `PoloProcessual`, `TipoCliente`, `ClienteDoProcessoDetalhe`.
- Produces:
  - `<BarraDeVoltar Titulo VoltarPara Rotulo />`: o `SectionContent` do encaixe da barra superior com o `header.aj-barra-superior.aj-barra-superior--voltar` de hoje (o link de voltar com `aria-label="{Rotulo}"`, o `h1`, o vazio). O `CabecalhoDePagina` com `VoltarPara` passa a emitir a barra por ele; o HTML não muda.
  - `<CabecalhoDeFicha Titulo Subtitulo? Resumo? TituloDaBarra VoltarPara Origem Acoes? />`: a barra de voltar do mobile (título `TituloDaBarra`, rótulo `Voltar para {Origem}`); no desktop e no tablet, o `aj-voltar` e o `header.aj-painel.aj-painel--principal.aj-painel--cabecalho` com o `h1`, o subtítulo e, só no desktop, `Acoes` em `aj-painel__botoes`; no mobile, o `aj-titulo-da-ficha` com o `Resumo` em `aj-ajuda` e o `Titulo` em `h2.aj-titulo`.
  - `public sealed record Aba(string Chave, string Rotulo, int? Contador = null);` e `<Abas Itens Ativa AoEscolher Rotulo />`: `div.aj-abas[role=tablist][aria-label={Rotulo}]` com um `button.aj-abas__aba[role=tab][aria-selected]` por aba, o contador em `span.aj-abas__contador` quando houver; clicar chama `AoEscolher` (`EventCallback<string>`) com a chave.
  - `RotuloDeCampo` ganha `[Parameter] public string? Classe { get; set; }`, acrescentada às classes do `label` (`aj-rotulo {Classe}` ou `aj-rotulo aj-rotulo--erro {Classe}`).
  - `RotulosDeProcesso`: `Situacao(StatusProcesso) : string` (`Ativo`, `Suspenso`, `Arquivado`, `Encerrado`), `SituacaoNoPlural(StatusProcesso) : string` (`Ativos`, `Suspensos`, `Arquivados`, `Encerrados`), `Polo(PoloProcessual) : string` (`Ativo`, `Passivo`, `Terceiro`), `Numero(string? numeroCnj) : string` (o número ou `Sem número`).
  - `TextosDoCliente`: `Tipo(TipoCliente) : string` (`Pessoa física`, `Pessoa jurídica`); `Documento(TipoCliente tipo, string documento) : string` (`CPF 529.982.247-25`, `CNPJ 00.000.000/0002-00`); `Endereco(ClienteDetalhe) : string?` (veja as Regras); `ConsequenciaDaInativacao(int? usuariosAtivosDoPortal) : string`.
  - `ValorEmReais`: `TentarLer(string? texto, out decimal? valor) : bool` e `Escrever(decimal? valor) : string?`; e o atributo `[ValorEmReaisValido]` (`ValidationAttribute`, mensagem `Informe o valor como 84.000,00.`), que aceita nulo, vazio e o que `TentarLer` lê.

**Regras:**
- `TextosDoCliente.Endereco`: logradouro, número, complemento, bairro e "cidade/UF" (só o que houver de cada um; "Maceió" sem UF, "AL" sem cidade), unidos por ", ", e "CEP {cep}" no fim quando houver. Nada preenchido: nulo. Exemplo: `Av. do Porto, 1200, Jaraguá, Maceió/AL`.
- `TextosDoCliente.ConsequenciaDaInativacao`: nulo (a contagem não pôde ser consultada) → `Os usuários do portal deste cliente ficam sem acesso até ele ser reativado.`; 0 → `O cliente pode ser reativado depois.`; 1 → `1 usuário do portal fica sem acesso até o cliente ser reativado.`; n → `{n} usuários do portal ficam sem acesso até o cliente ser reativado.`.
- `ValorEmReais.TentarLer`: texto vazio ou só espaço é nulo (válido). Aceita dígitos com separador de milhar "." em grupos de três e vírgula decimal com até duas casas: `84.000,00`, `84000`, `84000,5`, `1.234.567,89`, `0,50`. Recusa: ponto como decimal (`84000.50`), grupos fora do lugar (`1.2.3`, `84.00`), letras, sinal, mais de duas casas. `Escrever`: `84000m` → `84.000,00`; nulo → nulo.
- Ponto do design system: o desenho de cada peça é o da Tarefa 1; nenhuma tem estilo próprio.

- [ ] **Step 1: Testes (devem falhar)**

Em `TelasPurasTests`:

```csharp
public class RotulosDeProcessoTests
{
    [Theory]
    [InlineData(StatusProcesso.Ativo, "Ativo", "Ativos")]
    [InlineData(StatusProcesso.Suspenso, "Suspenso", "Suspensos")]
    [InlineData(StatusProcesso.Arquivado, "Arquivado", "Arquivados")]
    [InlineData(StatusProcesso.Encerrado, "Encerrado", "Encerrados")]
    public void Situacao_no_singular_e_no_plural(StatusProcesso status, string singular, string plural)
    {
        RotulosDeProcesso.Situacao(status).Should().Be(singular);
        RotulosDeProcesso.SituacaoNoPlural(status).Should().Be(plural);
    }

    [Fact]
    public void Numero_vazio_e_sem_numero()
    {
        RotulosDeProcesso.Numero(null).Should().Be("Sem número");
        RotulosDeProcesso.Numero("0000000-00.2026.5.19.0002").Should().Be("0000000-00.2026.5.19.0002");
        RotulosDeProcesso.Polo(PoloProcessual.Terceiro).Should().Be("Terceiro");
    }
}

public class TextosDoClienteDaFichaTests
{
    private static ClienteDetalhe Cliente(string? logradouro = null, string? numero = null, string? complemento = null,
        string? bairro = null, string? cidade = null, string? uf = null, string? cep = null) =>
        new(Guid.NewGuid(), TipoCliente.PessoaJuridica, "Transportes Litoral Ltda.", null, null,
            logradouro, numero, complemento, bairro, cidade, uf, cep, null, true,
            null, null, null, "00.000.000/0002-00", null, null, null, null);

    [Fact]
    public void Tipo_e_documento()
    {
        TextosDoCliente.Tipo(TipoCliente.PessoaFisica).Should().Be("Pessoa física");
        TextosDoCliente.Documento(TipoCliente.PessoaJuridica, "00.000.000/0002-00").Should().Be("CNPJ 00.000.000/0002-00");
        TextosDoCliente.Documento(TipoCliente.PessoaFisica, "529.982.247-25").Should().Be("CPF 529.982.247-25");
    }

    [Fact]
    public void Endereco_so_com_o_que_ha()
    {
        TextosDoCliente.Endereco(Cliente("Av. do Porto", "1200", null, "Jaraguá", "Maceió", "AL"))
            .Should().Be("Av. do Porto, 1200, Jaraguá, Maceió/AL");
        TextosDoCliente.Endereco(Cliente("Rua A", null, "sala 4", null, "Maceió", null, "57000-000"))
            .Should().Be("Rua A, sala 4, Maceió, CEP 57000-000");
        TextosDoCliente.Endereco(Cliente(uf: "AL")).Should().Be("AL");
        TextosDoCliente.Endereco(Cliente()).Should().BeNull();
    }

    [Theory]
    [InlineData(null, "Os usuários do portal deste cliente ficam sem acesso até ele ser reativado.")]
    [InlineData(0, "O cliente pode ser reativado depois.")]
    [InlineData(1, "1 usuário do portal fica sem acesso até o cliente ser reativado.")]
    [InlineData(3, "3 usuários do portal ficam sem acesso até o cliente ser reativado.")]
    public void Consequencia_da_inativacao(int? usuarios, string esperado)
    {
        TextosDoCliente.ConsequenciaDaInativacao(usuarios).Should().Be(esperado);
    }
}

public class ValorEmReaisTests
{
    [Theory]
    [InlineData("84.000,00", 84000)]
    [InlineData("84000", 84000)]
    [InlineData("84000,5", 84000.5)]
    [InlineData("1.234.567,89", 1234567.89)]
    [InlineData("0,50", 0.5)]
    [InlineData(" 84.000,00 ", 84000)]
    public void Le_o_formato_brasileiro(string texto, double esperado)
    {
        ValorEmReais.TentarLer(texto, out var valor).Should().BeTrue();
        valor.Should().Be((decimal)esperado);
    }

    [Theory]
    [InlineData("84000.50")]
    [InlineData("1.2.3")]
    [InlineData("84.00")]
    [InlineData("abc")]
    [InlineData("-10")]
    [InlineData("10,555")]
    public void Recusa_o_que_nao_e_o_formato_brasileiro(string texto)
    {
        ValorEmReais.TentarLer(texto, out _).Should().BeFalse();
    }

    [Fact]
    public void Vazio_e_nulo_e_valido_e_escreve_com_duas_casas()
    {
        ValorEmReais.TentarLer("  ", out var vazio).Should().BeTrue();
        vazio.Should().BeNull();
        ValorEmReais.Escrever(84000m).Should().Be("84.000,00");
        ValorEmReais.Escrever(null).Should().BeNull();
    }
}
```

(Os `using` que faltarem: `AtividadesJuridicas.Application.Cadastros`, `AtividadesJuridicas.Domain.Cadastros`.)

Em `ComponentesDoDesignSystemTests`:

```csharp
    [Fact]
    public async Task Cabecalho_de_ficha_traz_o_painel_com_as_acoes_no_desktop_e_o_bloco_de_titulo_no_mobile()
    {
        RenderFragment acoes = c => c.AddMarkupContent(0, "<a href=\"/clientes/1/editar\" class=\"aj-botao aj-botao--secundario\">Editar</a>");

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDeFicha>>(new()
        {
            ["Titulo"] = "Transportes Litoral Ltda.", ["Subtitulo"] = "Pessoa jurídica · CNPJ 00.000.000/0002-00 · Ativo",
            ["Resumo"] = "Pessoa jurídica · Ativo", ["TituloDaBarra"] = "Cliente", ["VoltarPara"] = "/clientes",
            ["Origem"] = "Clientes", ["Acoes"] = acoes,
        }));

        var desktopETablet = BlocoDeVisao.De(html, "aj-desktop-tablet");
        desktopETablet.Should().MatchRegex("(?s)<div>\\s*<a href=\"/clientes\" class=\"aj-voltar\">\\s*<svg[^>]*>.*?</svg>\\s*Clientes</a>\\s*</div>")
            .And.MatchRegex("(?s)<header class=\"aj-painel aj-painel--principal aj-painel--cabecalho\">\\s*<div class=\"aj-cabecalho__texto aj-cabecalho__texto--folgado\">\\s*<h1 class=\"aj-titulo\">Transportes Litoral Ltda.</h1>\\s*<p class=\"aj-subtitulo\">Pessoa jurídica · CNPJ 00.000.000/0002-00 · Ativo</p>\\s*</div>")
            .And.MatchRegex("(?s)<div class=\"aj-so-desktop\">\\s*<div class=\"aj-painel__botoes\">\\s*<a href=\"/clientes/1/editar\"");
        BlocoDeVisao.De(html, "aj-so-mobile").Should().MatchRegex(
            "(?s)<div class=\"aj-titulo-da-ficha\">\\s*<span class=\"aj-ajuda\">Pessoa jurídica · Ativo</span>\\s*<h2 class=\"aj-titulo\">Transportes Litoral Ltda.</h2>");
        html.Should().MatchRegex(
            "(?s)<header class=\"aj-barra-superior aj-barra-superior--voltar\">\\s*<a href=\"/clientes\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Voltar para Clientes\">.*?<h1>Cliente</h1>");
    }

    [Fact]
    public async Task Cabecalho_de_ficha_sem_acoes_nao_emite_o_grupo_de_botoes()
    {
        var html = await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDeFicha>>(new()
        {
            ["Titulo"] = "Litoral — reclamação trabalhista", ["TituloDaBarra"] = "Processo", ["VoltarPara"] = "/processos", ["Origem"] = "Processos",
        });

        html.Should().Contain("aj-painel--cabecalho").And.NotContain("aj-painel__botoes").And.NotContain("aj-subtitulo");
    }

    [Fact]
    public async Task Abas_marcam_a_escolhida_e_trazem_o_contador()
    {
        var html = await TelaDeTeste.RenderizarSemDadosAsync<Abas>(new()
        {
            ["Rotulo"] = "Assuntos do cliente", ["Ativa"] = "dados",
            ["Itens"] = new List<Aba> { new("dados", "Dados"), new("processos", "Processos", 2) },
        });

        html.Should().MatchRegex(
            "(?s)<div class=\"aj-abas\" role=\"tablist\" aria-label=\"Assuntos do cliente\">\\s*<button type=\"button\" role=\"tab\" class=\"aj-abas__aba\" aria-selected=\"true\"[^>]*>Dados</button>\\s*<button type=\"button\" role=\"tab\" class=\"aj-abas__aba\" aria-selected=\"false\"[^>]*>Processos<span class=\"aj-abas__contador\">2</span></button>");
    }

    [Fact]
    public async Task Rotulo_de_campo_leva_a_classe_de_posicao()
    {
        var html = await TelaDeTeste.RenderizarSemDadosAsync<RotuloDeCampo<string>>(new()
        {
            ["Texto"] = "Logradouro", ["Para"] = (System.Linq.Expressions.Expression<Func<string>>)(() => "x"),
            ["Classe"] = "aj-grade-endereco__logradouro",
        });

        html.Should().Contain("<label class=\"aj-rotulo aj-grade-endereco__logradouro\">");
    }
```

O `CabecalhoDePagina` com `VoltarPara` já tem teste (o do `CabecalhoDeDetalhe`, da 9C): ele é a prova de que a extração da `BarraDeVoltar` não muda o HTML.

`AbasInterativasTests.cs`:

```csharp
using AtividadesJuridicas.Web.Components.Layout;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

public class AbasInterativasTests : TelaInterativa
{
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
}
```

Run: `dotnet build -m:1`
Expected: não compila (`RotulosDeProcesso`, `ValorEmReais`, `CabecalhoDeFicha`, `Abas`, `Aba` não existem; `RotuloDeCampo` sem `Classe`). É o RED.

- [ ] **Step 2: Os textos**

```csharp
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>Os nomes das situações e dos polos do processo, como as telas os escrevem.</summary>
public static class RotulosDeProcesso
{
    public static string Situacao(StatusProcesso status) => status switch
    {
        StatusProcesso.Ativo => "Ativo",
        StatusProcesso.Suspenso => "Suspenso",
        StatusProcesso.Arquivado => "Arquivado",
        StatusProcesso.Encerrado => "Encerrado",
        _ => status.ToString(),
    };

    /// <summary>O nome do chip de filtro: "Ativos", "Suspensos".</summary>
    public static string SituacaoNoPlural(StatusProcesso status) => $"{Situacao(status)}s";

    public static string Polo(PoloProcessual polo) => polo switch
    {
        PoloProcessual.Ativo => "Ativo",
        PoloProcessual.Passivo => "Passivo",
        PoloProcessual.Terceiro => "Terceiro",
        _ => polo.ToString(),
    };

    public static string Numero(string? numeroCnj) => string.IsNullOrWhiteSpace(numeroCnj) ? "Sem número" : numeroCnj;
}
```

Em `TextosDoCliente`, os quatro métodos de "Produces" (o `Inativado` de hoje fica):

```csharp
    public static string Tipo(TipoCliente tipo) => tipo == TipoCliente.PessoaFisica ? "Pessoa física" : "Pessoa jurídica";

    /// <summary>O documento com o nome dele: "CPF 529.982.247-25", "CNPJ 00.000.000/0002-00".</summary>
    public static string Documento(TipoCliente tipo, string documento) =>
        $"{(tipo == TipoCliente.PessoaFisica ? "CPF" : "CNPJ")} {documento}";

    /// <summary>O endereço numa linha, só com as partes preenchidas; nulo quando não há nenhuma.</summary>
    public static string? Endereco(ClienteDetalhe cliente)
    {
        static bool Tem(string? parte) => !string.IsNullOrWhiteSpace(parte);

        var cidadeUf = string.Join("/", new[] { cliente.Cidade, cliente.Uf }.Where(Tem));
        var partes = new[] { cliente.Logradouro, cliente.Numero, cliente.Complemento, cliente.Bairro, cidadeUf }
            .Where(Tem).ToList();
        if (Tem(cliente.Cep)) partes.Add($"CEP {cliente.Cep}");
        return partes.Count == 0 ? null : string.Join(", ", partes);
    }

    /// <summary>O que acontece ao inativar; <paramref name="usuariosAtivosDoPortal"/> nulo quando a contagem não veio.</summary>
    public static string ConsequenciaDaInativacao(int? usuariosAtivosDoPortal) => usuariosAtivosDoPortal switch
    {
        null => "Os usuários do portal deste cliente ficam sem acesso até ele ser reativado.",
        <= 0 => "O cliente pode ser reativado depois.",
        1 => "1 usuário do portal fica sem acesso até o cliente ser reativado.",
        var n => $"{n} usuários do portal ficam sem acesso até o cliente ser reativado.",
    };
```

`ValorEmReais.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AtividadesJuridicas.Web.Infra;

/// <summary>
/// Valor em reais digitado como se escreve no Brasil: "84.000,00", "84000", "84000,5". O ponto só separa milhares,
/// em grupos de três; a vírgula separa até duas casas. "84000.50" é recusado: não se sabe se o ponto é decimal.
/// </summary>
public static partial class ValorEmReais
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    [GeneratedRegex("^(\\d{1,3}(\\.\\d{3})+|\\d+)(,\\d{1,2})?$")]
    private static partial Regex Formato();

    public static bool TentarLer(string? texto, out decimal? valor)
    {
        valor = null;
        if (string.IsNullOrWhiteSpace(texto)) return true;

        var limpo = texto.Trim();
        if (!Formato().IsMatch(limpo)) return false;

        valor = decimal.Parse(limpo, NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, Cultura);
        return true;
    }

    public static string? Escrever(decimal? valor) => valor?.ToString("N2", Cultura);
}

/// <summary>O campo de valor em reais está vazio ou no formato brasileiro.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ValorEmReaisValidoAttribute() : ValidationAttribute("Informe o valor como 84.000,00.")
{
    public override bool IsValid(object? value) => ValorEmReais.TentarLer(value as string, out _);
}
```

- [ ] **Step 3: As peças de layout**

`BarraDeVoltar.razor`:

```razor
@using Microsoft.AspNetCore.Components.Sections

@* A barra superior do mobile das fichas e dos formulários: voltar, o título no meio e um vazio do tamanho do botão.
   Com ela na tela, o layout esconde a barra inferior. *@
<SectionContent SectionName="@Encaixes.BarraSuperior">
    <header class="aj-barra-superior aj-barra-superior--voltar">
        <a href="@VoltarPara" class="aj-botao aj-botao--fantasma aj-botao--icone" aria-label="@Rotulo"><Icone Nome="voltar" /></a>
        <h1>@Titulo</h1>
        <span class="aj-barra-superior__vazio" aria-hidden="true"></span>
    </header>
</SectionContent>

@code {
    [Parameter, EditorRequired] public string Titulo { get; set; } = string.Empty;
    [Parameter, EditorRequired] public string VoltarPara { get; set; } = string.Empty;

    /// <summary>O rótulo acessível do voltar: "Voltar para Clientes".</summary>
    [Parameter, EditorRequired] public string Rotulo { get; set; } = string.Empty;
}
```

No `CabecalhoDePagina`, o ramo `VoltarPara is not null` passa a `<BarraDeVoltar Titulo="@Titulo" VoltarPara="@VoltarPara" Rotulo="@(RotuloDoVoltar ?? "Voltar")" />`, fora do `SectionContent` (que fica só para o ramo de título). Confira que o HTML do teste do `CabecalhoDeDetalhe` (9C) continua igual.

`CabecalhoDeFicha.razor`:

```razor
@* Cabeçalho das fichas (cliente, processo): no desktop e no tablet, o voltar acima de um painel com o nome e a linha
   de contexto, e as ações à direita só no desktop (no tablet e no mobile elas vão para a barra fixa da tela). No
   mobile, a barra de voltar com o assunto ("Cliente") e, no conteúdo, o nome com o resumo acima dele. *@
<BarraDeVoltar Titulo="@TituloDaBarra" VoltarPara="@VoltarPara" Rotulo="@($"Voltar para {Origem}")" />

<div class="aj-desktop-tablet">
    <div><a href="@VoltarPara" class="aj-voltar"><Icone Nome="voltar" />@Origem</a></div>
    <header class="aj-painel aj-painel--principal aj-painel--cabecalho">
        <div class="aj-cabecalho__texto aj-cabecalho__texto--folgado">
            <h1 class="aj-titulo">@Titulo</h1>
            @if (Subtitulo is not null)
            {
                <p class="aj-subtitulo">@Subtitulo</p>
            }
        </div>
        @if (Acoes is not null)
        {
            <div class="aj-so-desktop">
                <div class="aj-painel__botoes">@Acoes</div>
            </div>
        }
    </header>
</div>

<div class="aj-so-mobile">
    <div class="aj-titulo-da-ficha">
        @if (Resumo is not null)
        {
            <span class="aj-ajuda">@Resumo</span>
        }
        <h2 class="aj-titulo">@Titulo</h2>
    </div>
</div>

@code {
    /// <summary>O nome do cliente ou o título do processo.</summary>
    [Parameter, EditorRequired] public string Titulo { get; set; } = string.Empty;

    /// <summary>A linha de contexto do desktop e do tablet.</summary>
    [Parameter] public string? Subtitulo { get; set; }

    /// <summary>A linha curta do mobile, acima do nome.</summary>
    [Parameter] public string? Resumo { get; set; }

    /// <summary>O assunto, no título da barra de voltar do mobile: "Cliente", "Processo".</summary>
    [Parameter, EditorRequired] public string TituloDaBarra { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string VoltarPara { get; set; } = string.Empty;

    /// <summary>O nome da tela de destino do voltar: o texto do link e o rótulo do botão do mobile.</summary>
    [Parameter, EditorRequired] public string Origem { get; set; } = string.Empty;

    /// <summary>Os botões do desktop, à direita do nome.</summary>
    [Parameter] public RenderFragment? Acoes { get; set; }
}
```

`Aba.cs` (namespace `AtividadesJuridicas.Web.Components.Layout`) com o record de "Produces". `Abas.razor`:

```razor
@* As abas do mobile das fichas. Cada painel da ficha fica num invólucro aj-aba-painel, que a tela marca com
   aj-aba-painel--oculta quando não é da aba escolhida; no tablet e no desktop todos aparecem. *@
<div class="aj-abas" role="tablist" aria-label="@Rotulo">
    @foreach (var aba in Itens)
    {
        <button type="button" role="tab" class="aj-abas__aba" aria-selected="@(aba.Chave == Ativa ? "true" : "false")" @key="aba.Chave" @onclick="() => AoEscolher.InvokeAsync(aba.Chave)">@aba.Rotulo@if (aba.Contador is { } contador)
            {<span class="aj-abas__contador">@contador</span>}</button>
    }
</div>

@code {
    [Parameter, EditorRequired] public IReadOnlyList<Aba> Itens { get; set; } = [];
    [Parameter, EditorRequired] public string Ativa { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> AoEscolher { get; set; }

    /// <summary>O nome do grupo para o leitor de tela: "Assuntos do cliente".</summary>
    [Parameter, EditorRequired] public string Rotulo { get; set; } = string.Empty;
}
```

(O texto e o contador colados dentro do botão: o espaço entre eles seria um nó de texto a mais. Se o Razor não aceitar o `@if` assim, escreva o contador com uma expressão condicional que produza o `span`, e mantenha o HTML do teste.)

No `RotuloDeCampo`, a classe do `label`: `@ClasseDoRotulo(mensagens.Count > 0)`, com `ClasseDoRotulo(bool erro) => string.Join(" ", new[] { "aj-rotulo", erro ? "aj-rotulo--erro" : null, Classe }.Where(c => c is not null))`.

- [ ] **Step 4: Trava e testes**

Acrescente a `Migradas` (em ordem alfabética): `Components/Layout/Abas.razor`, `Components/Layout/BarraDeVoltar.razor`, `Components/Layout/CabecalhoDeFicha.razor`.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ComponentesDoDesignSystemTests|FullyQualifiedName~RotulosDeProcessoTests|FullyQualifiedName~TextosDoClienteDaFichaTests|FullyQualifiedName~ValorEmReaisTests|FullyQualifiedName~AbasInterativasTests|FullyQualifiedName~UsuariosWebTests|FullyQualifiedName~MinhaSenhaWebTests"`
Expected: 0 avisos; todos passando.

- [ ] **Step 5: Commit**

```
git add src tests
git commit -m "feat(web): cabeçalho de ficha, abas do mobile, barra de voltar extraída e os textos de cliente, processo e valor em reais"
```

---

### Task 6: Lista de clientes

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Clientes.razor` (reescrita: só a lista)
- Modify: `tests/AtividadesJuridicas.IntegrationTests/ClientesWebTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs` (sai `/clientes` da theory das telas ainda não migradas)
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: `PaginaDeClientes` (Tarefa 3), `CabecalhoDePagina` (com `LinhaDoMobile`), `EstadoDaLista`, `Paginacao`, `Aviso`, `Recados`, `Icone` (`buscar`, `avancar`), `TextosDoCliente.Documento`, `Contagem.ComInativos`.
- Produces: a rota `/clientes` com a lista; os links `/clientes/novo` (a ação) e `/clientes/{id}` ("Abrir" e o cartão), que as Tarefas 8 e 9 atendem. Até lá, os links levam à página não encontrada: a fase só entra na `main` com tudo pronto.

**Regras:**
- Quadros: `Clientes.dc.html`, `ClientesTablet.dc.html`, `ClientesMobile.dc.html`.
- Sai da lista tudo o que era formulário e ficha (o `Formulario`, o `DocumentosDoCliente`, os processos e os usuários do cliente, "Editar", "Inativar", "Ativar", "Ver", "Nova pessoa física", "Nova pessoa jurídica"). A lógica de busca e paginação fica: `ListarClientes(texto, incluirInativos, página)`, a volta à última página quando a atual ficou além do fim, e "acesso negado" encerrando a tela.
- Busca: no desktop, a barra (`form.aj-barra-filtros`) com a busca, a opção "Incluir inativos" e "Buscar"; a opção só vale ao buscar (como hoje). No tablet, a linha (`form.aj-linha-filtros`) com a busca e o chip "Incluir inativos", que alterna e busca na hora. No mobile, a busca na segunda linha da barra superior (`LinhaDoMobile`), que envia o formulário escondido `busca-de-clientes` pelo atributo `form`, e o chip "Incluir inativos" sobre a lista. As três buscas usam o mesmo texto.
- Subtítulo (também na barra do mobile): `Contagem.ComInativos(NoCadastro, Inativos, "cliente", "clientes", "inativo", "inativos")`, só com a lista carregada, sem erro e com `NoCadastro` maior que zero.
- Tabela (desktop): linha de cliente inativo com `aj-tabela__linha--inativa`; na linha inativa o e-mail não leva `aj-tabela__secundaria` (no quadro ele fica apagado como as outras células); `—` nas células sem valor; "Abrir" é um link `aj-botao aj-botao--fantasma aj-botao--seta` para a ficha. Paginação sempre à vista no desktop e no tablet (como os quadros); no mobile, só com mais de uma página.
- Cartões (tablet e mobile): link para a ficha; `aj-cartao--inativo` para o inativo; título, meta (documento; e, na segunda linha, cidade e telefone, só o que houver, unidos por " · "; sem nenhum dos dois, a linha não aparece) e rodapé (`aj-cartao__resumo` com o e-mail ou `Sem e-mail`, e `aj-cartao__situacao`).
- A ação "Novo cliente" (botão no cabeçalho, flutuante no mobile) só para a Diretoria.
- Recado de outra tela (o formulário, na Tarefa 9): mostrado uma vez, no topo.
- Falha da consulta: o `EstadoDaLista` com o erro e "Tentar de novo" (que repete a página pedida), sem subtítulo; vazio: `Nenhum cliente.`.

- [ ] **Step 1: Testes (devem falhar)**

`ClientesWebTests` (as asserções antigas de "Nova pessoa física", "Editar" e "Ver" saem com a marcação; as provas de que a Produção não tem ação de cadastro ganham a forma nova):

```csharp
    [Fact]
    public async Task Diretor_ve_a_lista_com_a_contagem_a_busca_e_a_acao_de_cadastro()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/clientes"));

        html.Should().Contain("<p class=\"aj-subtitulo\">1 cliente</p>")
            .And.MatchRegex("<a href=\"/clientes/novo\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\">\\s*<svg[^>]*>.*?</svg>\\s*Novo cliente</a>")
            .And.Contain("aria-label=\"Novo cliente\"");
        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex("<form class=\"aj-barra-filtros\" role=\"search\"")
            .And.Contain("placeholder=\"Buscar por nome, CPF ou CNPJ\"")
            .And.MatchRegex("<label class=\"aj-opcao aj-opcao--filtro\">\\s*<input type=\"checkbox\" class=\"aj-opcao__caixa\"[^>]*>\\s*Incluir inativos</label>")
            .And.MatchRegex("(?s)<tr>\\s*<td class=\"aj-tabela__principal\">Maria Silva</td>\\s*<td class=\"aj-tabela__data\">529.982.247-25</td>\\s*<td class=\"aj-tabela__secundaria\">maria@x.com</td>\\s*<td class=\"aj-tabela__data\">—</td>\\s*<td>Maceió</td>\\s*<td>Ativo</td>")
            .And.MatchRegex("<a href=\"/clientes/[0-9a-f-]{36}\" class=\"aj-botao aj-botao--fantasma aj-botao--seta\">\\s*Abrir\\s*<svg")
            .And.Contain("Página 1 de 1 · 1 cliente");
        var cartoes = BlocoDeVisao.De(html, "aj-tablet-mobile");
        cartoes.Should().MatchRegex(
            "(?s)<a href=\"/clientes/[0-9a-f-]{36}\" class=\"aj-cartao\">\\s*<span class=\"aj-cartao__titulo\">Maria Silva</span>\\s*<span class=\"aj-cartao__meta\">CPF 529.982.247-25<br\\s*/?>Maceió</span>\\s*<span class=\"aj-cartao__rodape\">\\s*<span class=\"aj-cartao__resumo\">maria@x.com</span>\\s*<span class=\"aj-cartao__situacao\">Ativo</span>");
        BlocoDeVisao.De(html, "aj-so-tablet").Should().MatchRegex(
            "<button type=\"button\" class=\"aj-chip aj-chip--campo\" aria-pressed=\"false\"[^>]*>Incluir inativos</button>");
        BlocoDeVisao.De(html, "aj-so-mobile").Should().MatchRegex(
            "<div class=\"aj-chips\" role=\"group\" aria-label=\"Filtros\">\\s*<button type=\"button\" class=\"aj-chip\" aria-pressed=\"false\"");
        html.Should().MatchRegex("<input type=\"search\" class=\"aj-campo-icone__controle\" form=\"busca-de-clientes\"")
            .And.Contain("<form id=\"busca-de-clientes\" hidden");
    }

    [Fact]
    public async Task Producao_ve_a_lista_e_abre_a_ficha_mas_nao_tem_acao_de_cadastro()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/clientes"));

        html.Should().Contain("Maria Silva").And.MatchRegex("<a href=\"/clientes/[0-9a-f-]{36}\" class=\"aj-botao aj-botao--fantasma aj-botao--seta\">");
        html.Should().NotContain("Novo cliente").And.NotContain("/clientes/novo")
            .And.NotContain("Inativar").And.NotContain("Acesso negado").And.NotContain("Seu acesso foi encerrado");
    }

    [Fact]
    public async Task Cliente_inativo_aparece_apagado_na_tabela_e_no_cartao_e_conta_no_subtitulo()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var maria = (await _app.ExecutarAsync(admin, new ListarClientes())).Valor!.Itens.Single();
        (await _app.ExecutarAsync(admin, new DefinirAtivoCliente(maria.Id, false))).Sucesso.Should().BeTrue();
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(await _app.ExecutarAsync(admin, new ObterMeuAcesso()))
            .Responde<ListarClientes, Resultado<PaginaDeClientes>>(await _app.ExecutarAsync(admin, new ListarClientes(IncluirInativos: true)));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<Clientes>(despachante, new()));

        html.Should().Contain("1 cliente · 1 inativo")
            .And.MatchRegex("(?s)<tr class=\"aj-tabela__linha--inativa\">\\s*<td class=\"aj-tabela__principal\">Maria Silva</td>\\s*<td class=\"aj-tabela__data\">529.982.247-25</td>\\s*<td>maria@x.com</td>")
            .And.MatchRegex("<a href=\"/clientes/[0-9a-f-]{36}\" class=\"aj-cartao aj-cartao--inativo\">")
            .And.Contain("<span class=\"aj-cartao__situacao\">Inativo</span>");
    }

    [Fact]
    public async Task Falha_da_consulta_mostra_o_erro_sem_subtitulo_e_nao_a_lista_vazia()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(await _app.ExecutarAsync(admin, new ObterMeuAcesso()))
            .Responde<ListarClientes, Resultado<PaginaDeClientes>>(Resultado<PaginaDeClientes>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<Clientes>(despachante, new()));

        html.Should().Contain("Falha de teste.").And.Contain(">Tentar de novo</button>");
        html.Should().NotContain("Nenhum cliente.").And.NotContain("aj-subtitulo\">0").And.NotContain("Página 1");
    }

    [Fact]
    public async Task Recado_de_outra_tela_aparece_uma_vez_no_topo()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var recados = new Recados();
        recados.Deixar("Cliente salvo.");

        var html = await TelaDeTeste.RenderizarAsync<Clientes>(_app, admin, new(), recados);

        html.Should().MatchRegex("<p class=\"aj-aviso aj-aviso--sucesso\"[^>]*>.*Cliente salvo.");
        recados.Retirar().Should().BeNull();
    }
```

(O teste do recado usa a marcação que o `Aviso` emite; confira a classe exata dele em `ComponentesDoDesignSystemTests` e use a mesma. Os `using` que faltarem: `AtividadesJuridicas.Application.Comum`, `AtividadesJuridicas.Application.Identidade`, `AtividadesJuridicas.Web.Components.Pages`, `AtividadesJuridicas.Web.Infra`.) Os testes de menu e de anônimo do arquivo ficam como estão.

Em `LayoutWebTests`, tire `/clientes` da theory das telas ainda não migradas (`/processos` sai na Tarefa 10).

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ClientesWebTests"`
Expected: os testes novos falhando (a tela ainda é a antiga). É o RED.

- [ ] **Step 2: A tela**

```razor
@page "/clientes"
@inject IDespachante Despachante
@inject Recados Recados

<PageTitle>Clientes</PageTitle>

<CabecalhoDePagina Titulo="Clientes" Subtitulo="@Subtitulo" Acao="@(_ator?.EhDiretoria == true ? "Novo cliente" : null)" AcaoHref="/clientes/novo">
    <LinhaDoMobile>
        @* O Enter na busca envia o formulário "busca-de-clientes", que fica no fim da tela. *@
        <label class="aj-campo-icone"><Icone Nome="buscar" /><span class="aj-so-leitor">Buscar cliente</span><input type="search" class="aj-campo-icone__controle" form="busca-de-clientes" placeholder="Buscar por nome, CPF ou CNPJ" @bind="_texto" /></label>
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
    @if (_recado is { } recado)
    {
        <Aviso Tipo="@recado.Tipo">@recado.Texto</Aviso>
    }

    <div class="aj-so-desktop">
        <form class="aj-barra-filtros" role="search" @onsubmit="BuscarAsync" @onsubmit:preventDefault>
            <label class="aj-campo-icone"><Icone Nome="buscar" /><span class="aj-so-leitor">Buscar cliente</span><input type="search" class="aj-campo-icone__controle" placeholder="Buscar por nome, CPF ou CNPJ" @bind="_texto" /></label>
            <label class="aj-opcao aj-opcao--filtro"><input type="checkbox" class="aj-opcao__caixa" @bind="_incluirInativos" />Incluir inativos</label>
            <button type="submit" class="aj-botao aj-botao--secundario">Buscar</button>
        </form>
    </div>
    <div class="aj-so-tablet">
        <form class="aj-linha-filtros" role="search" @onsubmit="BuscarAsync" @onsubmit:preventDefault>
            <label class="aj-campo-icone"><Icone Nome="buscar" /><span class="aj-so-leitor">Buscar cliente</span><input type="search" class="aj-campo-icone__controle" placeholder="Buscar por nome, CPF ou CNPJ" @bind="_texto" /></label>
            <button type="button" class="aj-chip aj-chip--campo" aria-pressed="@Pressionado" @onclick="AlternarInativosAsync">Incluir inativos</button>
        </form>
    </div>
    <div class="aj-so-mobile">
        <div class="aj-chips" role="group" aria-label="Filtros">
            <button type="button" class="aj-chip" aria-pressed="@Pressionado" @onclick="AlternarInativosAsync">Incluir inativos</button>
        </div>
    </div>

    <EstadoDaLista Erro="@_erro" Vazio="@(_pagina.Itens.Count == 0)" TextoDeVazio="Nenhum cliente." TentarDeNovo="() => CarregarAsync(_paginaPedida)">
        <div class="aj-so-desktop">
            <div class="aj-tabela-caixa">
                <table class="aj-tabela">
                    <thead><tr><th scope="col">Nome</th><th scope="col">Documento</th><th scope="col">E-mail</th><th scope="col">Telefone</th><th scope="col">Cidade</th><th scope="col">Situação</th><th scope="col"><span class="aj-so-leitor">Ações</span></th></tr></thead>
                    <tbody>
                        @foreach (var cliente in _pagina.Itens)
                        {
                            <tr class="@(cliente.Ativo ? null : "aj-tabela__linha--inativa")" @key="cliente.Id">
                                <td class="aj-tabela__principal">@cliente.Nome</td>
                                <td class="aj-tabela__data">@cliente.Documento</td>
                                @* Na linha inativa o e-mail fica apagado como as outras células, como no quadro. *@
                                <td class="@(cliente.Ativo ? "aj-tabela__secundaria" : null)">@Ou(cliente.Email)</td>
                                <td class="aj-tabela__data">@Ou(cliente.Telefone)</td>
                                <td>@Ou(cliente.Cidade)</td>
                                <td>@Situacao(cliente)</td>
                                <td class="aj-tabela__acao"><a href="@Ficha(cliente)" class="aj-botao aj-botao--fantasma aj-botao--seta">Abrir<Icone Nome="avancar" /></a></td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
            <Paginacao Numero="_pagina.Numero" TotalDePaginas="_pagina.TotalDePaginas" Total="_pagina.Total"
                       Singular="cliente" Plural="clientes" SempreVisivel="true" IrPara="CarregarAsync" />
        </div>

        <div class="aj-tablet-mobile">
            <div class="aj-grade">
                @foreach (var cliente in _pagina.Itens)
                {
                    <a href="@Ficha(cliente)" class="@(cliente.Ativo ? "aj-cartao" : "aj-cartao aj-cartao--inativo")" @key="cliente.Id">
                        <span class="aj-cartao__titulo">@cliente.Nome</span>
                        @if (Localizacao(cliente) is { } localizacao)
                        {
                            <span class="aj-cartao__meta">@TextosDoCliente.Documento(cliente.Tipo, cliente.Documento)<br />@localizacao</span>
                        }
                        else
                        {
                            <span class="aj-cartao__meta">@TextosDoCliente.Documento(cliente.Tipo, cliente.Documento)</span>
                        }
                        <span class="aj-cartao__rodape"><span class="aj-cartao__resumo">@(cliente.Email ?? "Sem e-mail")</span><span class="aj-cartao__situacao">@Situacao(cliente)</span></span>
                    </a>
                }
            </div>
        </div>
        <div class="aj-so-tablet">
            <Paginacao Numero="_pagina.Numero" TotalDePaginas="_pagina.TotalDePaginas" Total="_pagina.Total"
                       Singular="cliente" Plural="clientes" SempreVisivel="true" IrPara="CarregarAsync" />
        </div>
        <div class="aj-so-mobile">
            <Paginacao Numero="_pagina.Numero" TotalDePaginas="_pagina.TotalDePaginas" Total="_pagina.Total"
                       Singular="cliente" Plural="clientes" IrPara="CarregarAsync" />
        </div>
    </EstadoDaLista>

    <form id="busca-de-clientes" hidden @onsubmit="BuscarAsync" @onsubmit:preventDefault></form>
}
```

No `@code`:

- Estado: `_carregado`, `_ator` (`UsuarioAutenticado?`), `_texto`, `_incluirInativos`, `_pagina` (`PaginaDeClientes`, começa vazia), `_paginaPedida`, `_erro`, `_recado` (`Recado?`).
- `OnInitializedAsync`: `_ator = ObterMeuAcesso`; com ator, `_recado = Recados.Retirar()` e `CarregarAsync(1)`; `_carregado = true`.
- `CarregarAsync(int pagina)`: guarda `_paginaPedida`; `ListarClientes(_texto, _incluirInativos, pagina)`; acesso negado encerra (`_ator = null`, página vazia); falha: `_erro = resultado.Mensagem ?? "Não foi possível consultar os clientes."` e a página fica vazia; sucesso: `_erro = null`, `_pagina = resultado.Valor`; e a volta à última página de hoje.
- `BuscarAsync()`: `_recado = null` e `CarregarAsync(1)`. `AlternarInativosAsync()`: inverte `_incluirInativos` e busca.
- `Subtitulo`: como nas Regras. `Pressionado => _incluirInativos ? "true" : "false"`.
- `Ficha(c) => $"/clientes/{c.Id}"`, `Situacao(c) => c.Ativo ? "Ativo" : "Inativo"`, `Ou(v) => string.IsNullOrWhiteSpace(v) ? "—" : v`, `Localizacao(c)`: cidade e telefone preenchidos unidos por " · ", ou nulo.

- [ ] **Step 3: Trava e testes**

Acrescente a `Migradas`: `Components/Pages/Clientes.razor`.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ClientesWebTests|FullyQualifiedName~LayoutWebTests|FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~PaginasProtegidasTests"`
Expected: 0 avisos; todos passando. Os testes de `DocumentosDaFichaTests` desenham o `DocumentosDoCliente` direto e continuam verdes.

- [ ] **Step 4: Commit**

```
git add src tests
git commit -m "feat(web): lista de clientes no design system, com a contagem do cadastro, a busca de cada visão e os cartões"
```

---

### Task 7: Painel de documentos da ficha do cliente

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Documentos/DocumentosDaFicha.razor`
- Delete: `src/AtividadesJuridicas.Web/Components/Documentos/DocumentosDoCliente.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Clientes.razor` só se ainda referenciar o antigo (depois da Tarefa 6, não referencia)
- Modify: `tests/AtividadesJuridicas.IntegrationTests/DocumentosDaFichaTests.cs` (passa a desenhar o componente novo)
- Create: `tests/AtividadesJuridicas.IntegrationTests/DocumentosDaFichaInterativosTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/TelasPurasTests.cs` (se algum teste citar o antigo)
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: a base da Tarefa 4 (`AbrirEnvio`, `RenomearLinha`, `ExcluirLinha`, `AbrirAcessoDaLinhaAsync`, `DescartarEnvio`, `Ocupada`, `_envioAberto`, `_porCategoria`, `FocarPainelAbertoAsync`, `AoCarregarAsync`), `TextosDeDocumento`, `MenuDeAcoes`, `EstadoDaLista`, `Paginacao`, `Aviso`.
- Produces: `<DocumentosDaFicha ClienteId PodeGerenciar AoContar OnAcessoEncerrado />` — `ClienteId` (`Guid`), `PodeGerenciar` (`bool`: a Diretoria), `AoContar` (`EventCallback<int>`: a soma dos documentos de todas as áreas que o usuário lê, depois de cada leitura; a aba "Documentos" do mobile usa), `OnAcessoEncerrado` (da base).

**Regras:**
- Quadros: o painel "Documentos" de `Cliente.dc.html` (desktop) e de `ClienteTablet.dc.html` (tablet; o mobile é igual ao tablet, numa coluna).
- Áreas, na ordem dos chips: com `PodeGerenciar`, `Gerais`, `Contratos de honorários`, `De atividades`, `Do cliente` (`DocumentoGeral`, `ContratoHonorarios`, `DocumentoAtividade`, `DocumentoDoCliente`); sem, `Gerais` e `De atividades`. Começa em `Gerais`. O rótulo do chip é `{área} · {n}`, com `n` de `_porCategoria` (sem a contagem ainda, só o nome).
- Trocar de chip: com `Ocupada`, nada; senão descarta o envio, fecha o renomear e o acesso, cancela a exclusão, apaga o aviso e lê a página 1 da área nova. (Review Focus 1: o envio aberto em uma área nunca vai para outra.)
- "Enviar documento" (botão secundário com o ícone `adicionar`, no cabeçalho do painel, ao lado do `h2`): só com `PodeGerenciar` e fora de `De atividades`. O painel de envio diz a área (`Vai para {área}.`) e envia para ela.
- Tabela (desktop, caixa plana): `Nome` (link de download, na cor da marca), `Tamanho`, `Enviado em`, `Por`, `Atividade` (só em `De atividades`), e a coluna de ações só com `PodeGerenciar`: `Renomear`, `Acesso` (só em `Gerais`), `Excluir` (perigo), menores, colados, desabilitados com `Ocupada`.
- Linhas (tablet e mobile, `aj-linhas`): o nome é o link de download (`aj-linha__titulo`), a linha de dados é `TextosDeDocumento.Detalhe`, e o menu de ações (`Ações de {nome}`) só com `PodeGerenciar`: `Baixar`, `Renomear`, `Acesso` (só em `Gerais`), `Excluir` (perigo).
- A ajuda sobre o acesso fica sob a tabela, só no desktop, só em `Gerais` e só com `PodeGerenciar` (como no quadro).
- Antes do painel: o aviso da última ação e a confirmação de exclusão (título `Confirmar a exclusão de {nome}?`, texto `O documento é excluído da ficha do cliente.`, `Cancelar` e `Confirmar exclusão`). Depois do painel: o painel aberto (envio, renomear ou acesso), um de cada vez.
- O painel de acesso: `Quem acessa "{nome}"`, a ajuda de hoje, `h3` `Perfis` e `Usuários`, cada grupo em `div.aj-opcoes` com uma `label.aj-opcao` por opção (`input type=checkbox class=aj-opcao__caixa`), e `Cancelar` e `Salvar acesso` em `aj-painel__acoes`.
- Foco: abrir o envio leva ao campo de arquivo; o renomear, ao campo do nome; o acesso, à primeira caixa de seleção.
- A Produção não pede o que não lê: o componente sem `PodeGerenciar` nunca envia `ListarDocumentosDoCliente` com `ContratoHonorarios` ou `DocumentoDoCliente`.

- [ ] **Step 1: Testes (devem falhar)**

`DocumentosDaFichaTests` (os testes de hoje, reescritos para o componente novo, mantendo o que provam):

```csharp
    private async Task<string> FichaAsync(Guid usuario, bool podeGerenciar) =>
        WebUtility.HtmlDecode(await TelaDeTeste.RenderizarAsync<DocumentosDaFicha>(_app, usuario, new()
        {
            ["ClienteId"] = _maria, ["PodeGerenciar"] = podeGerenciar,
        }));

    [Fact]
    public async Task Diretoria_ve_os_quatro_chips_com_a_contagem_e_o_envio()
    {
        // O documento do InitializeAsync é "Do cliente"; a ficha abre em "Gerais", vazia.
        var html = await FichaAsync(_admin, podeGerenciar: true);

        html.Should().MatchRegex("(?s)<section class=\"aj-painel\">\\s*<div class=\"aj-painel__cabecalho\">\\s*<h2>Documentos</h2>\\s*<button type=\"button\" class=\"aj-botao aj-botao--secundario aj-botao--com-icone\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Enviar documento</button>")
            .And.MatchRegex("(?s)<div class=\"aj-chips\" role=\"group\" aria-label=\"Área de documentos\">\\s*<button type=\"button\" class=\"aj-chip\" aria-pressed=\"true\"[^>]*>Gerais · 0</button>\\s*<button type=\"button\" class=\"aj-chip\" aria-pressed=\"false\"[^>]*>Contratos de honorários · 0</button>\\s*<button type=\"button\" class=\"aj-chip\" aria-pressed=\"false\"[^>]*>De atividades · 0</button>\\s*<button type=\"button\" class=\"aj-chip\" aria-pressed=\"false\"[^>]*>Do cliente · 1</button>")
            .And.Contain("Nenhum documento.");
    }

    [Fact]
    public async Task Diretoria_em_gerais_ve_a_tabela_com_as_acoes_o_acesso_e_a_ajuda()
    {
        var geral = await _app.ExecutarAsync(_admin, new EnviarDocumento(
            CategoriaDocumento.DocumentoGeral, _maria, null, "procuracao-geral.pdf", "application/pdf", Conteudo()));
        geral.Sucesso.Should().BeTrue(geral.Mensagem);

        var html = await FichaAsync(_admin, podeGerenciar: true);

        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().Contain("<div class=\"aj-tabela-caixa aj-tabela-caixa--plana\">")
            .And.MatchRegex($"<td class=\"aj-tabela__principal\"><a href=\"/documentos/{geral.Valor}/baixar\" download data-enhance-nav=\"false\">procuracao-geral.pdf</a></td>")
            .And.Contain("<td>Diana Diretora</td>")
            .And.MatchRegex("<td class=\"aj-tabela__acao aj-tabela__acao--varias\"><button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--menor\"[^>]*>Renomear</button><button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--menor\"[^>]*>Acesso</button><button type=\"button\" class=\"aj-botao aj-botao--perigo aj-botao--menor\"[^>]*>Excluir</button></td>")
            .And.Contain("A Diretoria acessa sempre. Em “Acesso” você escolhe os perfis e os usuários que também veem cada documento geral.");
        var linhas = BlocoDeVisao.De(html, "aj-tablet-mobile");
        linhas.Should().MatchRegex($"(?s)<div class=\"aj-linhas\">\\s*<div class=\"aj-linha\">\\s*<div class=\"aj-linha__texto\">\\s*<a href=\"/documentos/{geral.Valor}/baixar\" download data-enhance-nav=\"false\" class=\"aj-linha__titulo\">procuracao-geral.pdf</a>")
            .And.Contain("aria-label=\"Ações de procuracao-geral.pdf\"").And.Contain(">Acesso</button>");
    }

    [Fact]
    public async Task Producao_ve_so_gerais_e_de_atividades_sem_envio_nem_acoes()
    {
        var pedro = await _app.CriarPedroAsync();
        var geral = await _app.ExecutarAsync(_admin, new EnviarDocumento(
            CategoriaDocumento.DocumentoGeral, _maria, null, "geral-do-pedro.pdf", "application/pdf", Conteudo()));
        (await _app.ExecutarAsync(_admin, new DefinirAcessoDoDocumento(geral.Valor, [], [pedro]))).Sucesso.Should().BeTrue();

        var html = await FichaAsync(pedro, podeGerenciar: false);

        html.Should().Contain(">Gerais · 1</button>").And.Contain(">De atividades · 0</button>").And.Contain("geral-do-pedro.pdf");
        html.Should().NotContain("Contratos de honorários").And.NotContain("Do cliente ·").And.NotContain("Enviar documento")
            .And.NotContain(">Renomear</button>").And.NotContain(">Excluir</button>").And.NotContain("aj-menu-acoes")
            .And.NotContain("Seu acesso foi encerrado");
    }
```

(O teste de hoje sobre "Documentos de Atividades" — coluna `Atividade` e sem envio — passa a ser feito clicando no chip, no arquivo interativo abaixo. O de "sem gestão só lista" vira o da Produção acima. Liste no relatório o que cada teste antigo provava e onde a prova ficou.)

`DocumentosDaFichaInterativosTests.cs`:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Web.Components.Documentos;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;

namespace AtividadesJuridicas.IntegrationTests;

public class DocumentosDaFichaInterativosTests : TelaInterativa
{
    private static readonly Guid Maria = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a1");
    private static readonly Guid Procuracao = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b1");

    private static readonly Dictionary<CategoriaDocumento, int> Contagem = new()
    {
        [CategoriaDocumento.DocumentoGeral] = 3, [CategoriaDocumento.ContratoHonorarios] = 1,
        [CategoriaDocumento.DocumentoAtividade] = 4, [CategoriaDocumento.DocumentoDoCliente] = 2,
    };

    private static Resultado<PaginaDeDocumentos> Pagina(ListarDocumentosDoCliente pedido) => Resultado<PaginaDeDocumentos>.Ok(new([
        new DocumentoResumo(Procuracao, "Procuração.pdf", "procuracao.pdf", 412 * 1024, pedido.Categoria,
            new DateTime(2026, 9, 2, 13, 14, 0, DateTimeKind.Utc), "Marina Couto", null,
            pedido.Categoria == CategoriaDocumento.DocumentoAtividade ? "Contestação — cobrança de ISS" : null),
    ], 1, 1, 20, Contagem));

    private IRenderedComponent<DocumentosDaFicha> Ficha(bool podeGerenciar = true, Action<int>? aoContar = null)
    {
        Despachante.Responde<ListarDocumentosDoCliente, Resultado<PaginaDeDocumentos>>(Pagina);
        return Desenhar<DocumentosDaFicha>(p => p
            .Add(d => d.ClienteId, Maria).Add(d => d.PodeGerenciar, podeGerenciar)
            .Add(d => d.AoContar, (int total) => aoContar?.Invoke(total)));
    }

    private static void Chip(IRenderedComponent<DocumentosDaFicha> ficha, string comeco) =>
        ficha.FindAll(".aj-chips button").Single(b => b.TextContent.StartsWith(comeco)).Click();

    [Fact]
    public void Trocar_de_chip_le_a_area_nova_e_conta_a_soma()
    {
        var total = 0;
        var ficha = Ficha(aoContar: t => total = t);

        Chip(ficha, "De atividades");

        Despachante.Recebidas.OfType<ListarDocumentosDoCliente>().Select(r => r.Categoria).Should()
            .Equal(CategoriaDocumento.DocumentoGeral, CategoriaDocumento.DocumentoAtividade);
        ficha.Find(".aj-chips button[aria-pressed=true]").TextContent.Should().Be("De atividades · 4");
        BlocoDeVisao.De(ficha.Markup, "aj-so-desktop").Should().Contain("<th scope=\"col\">Atividade</th>")
            .And.Contain("Contestação — cobrança de ISS");
        ficha.FindAll(".aj-painel__cabecalho button").Should().BeEmpty();   // De atividades não tem envio
        total.Should().Be(10);
    }

    [Fact]
    public void Trocar_de_chip_com_o_envio_aberto_descarta_o_arquivo_e_o_envio_vai_para_a_area_do_painel()
    {
        Despachante.Responde<EnviarDocumento, Resultado<Guid>>(Resultado<Guid>.Ok(Guid.NewGuid()));
        var ficha = Ficha();
        ficha.Find(".aj-painel__cabecalho button").Click();
        ficha.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("conteúdo", "contrato.pdf"));
        ficha.Markup.Should().Contain("Vai para Gerais.");

        Chip(ficha, "Do cliente");

        ficha.FindAll("input[type=file]").Should().BeEmpty();
        ficha.Find(".aj-painel__cabecalho button").Click();
        ficha.Markup.Should().Contain("Vai para Do cliente.");
        ficha.FindAll("section.aj-painel button").Single(b => b.TextContent == "Enviar").HasAttribute("disabled").Should().BeTrue();

        ficha.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("conteúdo", "comprovante.pdf"));
        ficha.FindAll("section.aj-painel button").Single(b => b.TextContent == "Enviar").Click();

        ficha.WaitForAssertion(() => Despachante.Recebidas.OfType<EnviarDocumento>().Should().ContainSingle()
            .Which.Should().Match<EnviarDocumento>(e => e.Categoria == CategoriaDocumento.DocumentoDoCliente && e.NomeArquivo == "comprovante.pdf"));
    }

    [Fact]
    public void Acesso_fecha_o_envio_e_a_exclusao_e_leva_o_foco_a_primeira_opcao()
    {
        Despachante
            .Responde<ObterAcessoDoDocumento, Resultado<AcessoDoDocumento>>(Resultado<AcessoDoDocumento>.Ok(
                new AcessoDoDocumento(Procuracao, "Procuração.pdf", CategoriaDocumento.DocumentoGeral, [], [])))
            .Responde<Application.Identidade.ListarPerfis, Resultado<IReadOnlyList<Application.Identidade.PerfilDetalhe>>>(
                Resultado<IReadOnlyList<Application.Identidade.PerfilDetalhe>>.Ok([
                    new(Guid.NewGuid(), "Produção", Domain.Identidade.TipoPerfil.Producao, true, 2)]))
            .Responde<Application.Identidade.ListarUsuarios, Resultado<IReadOnlyList<Application.Identidade.UsuarioResumo>>>(
                Resultado<IReadOnlyList<Application.Identidade.UsuarioResumo>>.Ok([]));
        var ficha = Ficha();
        ficha.Find(".aj-painel__cabecalho button").Click();

        ficha.FindAll("table.aj-tabela tbody button").Single(b => b.TextContent == "Acesso").Click();

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Quem acessa \"Procuração.pdf\""));
        ficha.FindAll("input[type=file]").Should().BeEmpty();
        ficha.Find("div.aj-opcoes label.aj-opcao").TextContent.Should().Contain("Produção");
        JSInterop.VerifyFocusAsyncInvoke(calledTimes: 2)[1].Arguments[0]
            .ShouldBeElementReferenceTo(ficha.Find("div.aj-opcoes input.aj-opcao__caixa"));
    }

    [Fact]
    public void Producao_so_pede_as_areas_que_le()
    {
        var ficha = Ficha(podeGerenciar: false);

        Chip(ficha, "De atividades");
        Chip(ficha, "Gerais");

        Despachante.Recebidas.OfType<ListarDocumentosDoCliente>().Select(r => r.Categoria).Should()
            .OnlyContain(c => c == CategoriaDocumento.DocumentoGeral || c == CategoriaDocumento.DocumentoAtividade);
        ficha.FindAll(".aj-chips button").Select(b => b.TextContent).Should().Equal("Gerais · 3", "De atividades · 4");
    }
}
```

(Ajuste o construtor de `PerfilDetalhe` e de `UsuarioResumo` às assinaturas de hoje; use `using` em vez dos nomes qualificados, se preferir. Na contagem da Produção o teste combina a mesma resposta para as quatro áreas: o componente mostra só os chips das áreas dele, e a soma do `AoContar` é a das áreas que vieram na resposta.)

Run: `dotnet build -m:1`
Expected: não compila (`DocumentosDaFicha` não existe). É o RED.

- [ ] **Step 2: O componente**

```razor
@inherits GestaoDeDocumentos
@inject FusoDoEscritorio Fuso

@if (_mensagem is not null)
{
    <Aviso Tipo="@(_falhou ? TipoDeAviso.Erro : TipoDeAviso.Sucesso)">@_mensagem</Aviso>
}

@if (AExcluir is { } aExcluir)
{
    <Aviso @key="aExcluir.Id" Tipo="TipoDeAviso.Atencao" Rotulo="Confirmar exclusão" Titulo="@($"Confirmar a exclusão de {aExcluir.Nome}?")">
        <ChildContent>O documento é excluído da ficha do cliente.</ChildContent>
        <Acoes><button type="button" class="aj-botao aj-botao--secundario" disabled="@_removendo" @onclick="CancelarExclusao">Cancelar</button><button type="button" class="aj-botao aj-botao--primario" disabled="@_removendo" @onclick="() => ExcluirAsync(aExcluir.Id)">Confirmar exclusão</button></Acoes>
    </Aviso>
}

<section class="aj-painel">
    <div class="aj-painel__cabecalho">
        <h2>Documentos</h2>
        @if (PodeGerenciar && !DeAtividades)
        {
            <button type="button" class="aj-botao aj-botao--secundario aj-botao--com-icone" disabled="@Ocupada" @onclick="AbrirEnvio"><Icone Nome="adicionar" />Enviar documento</button>
        }
    </div>

    <div class="aj-chips" role="group" aria-label="Área de documentos">
        @foreach (var area in Areas)
        {
            <button type="button" class="aj-chip" aria-pressed="@(area == _categoria ? "true" : "false")" @key="area" @onclick="() => TrocarAreaAsync(area)">@RotuloDoChip(area)</button>
        }
    </div>

    <EstadoDaLista Erro="@_erroDaLista" Carregando="@(!_carregado)" Vazio="@(_pagina.Itens.Count == 0)"
                   TextoDeVazio="Nenhum documento." TentarDeNovo="() => IrParaAsync(_paginaPedida)">
        <div class="aj-so-desktop">
            <div class="aj-tabela-caixa aj-tabela-caixa--plana">
                <table class="aj-tabela">
                    <thead><tr><th scope="col">Nome</th><th scope="col">Tamanho</th><th scope="col">Enviado em</th><th scope="col">Por</th>@if (DeAtividades)
                        {<th scope="col">Atividade</th>}@if (PodeGerenciar)
                        {<th scope="col"><span class="aj-so-leitor">Ações</span></th>}</tr></thead>
                    <tbody>
                        @foreach (var documento in _pagina.Itens)
                        {
                            <tr @key="documento.Id">
                                <td class="aj-tabela__principal"><a href="@TextosDeDocumento.Endereco(documento.Id)" download data-enhance-nav="false">@documento.Nome</a></td>
                                <td class="aj-tabela__data">@TamanhoDeArquivo.Formatar(documento.TamanhoBytes)</td>
                                <td class="aj-tabela__data">@Fuso.Formatar(documento.EnviadoEm)</td>
                                <td>@documento.EnviadoPor</td>
                                @if (DeAtividades)
                                {
                                    <td>@documento.AtividadeTitulo</td>
                                }
                                @if (PodeGerenciar)
                                {
                                    @* Os botões colados: o espaço entre eles seria desenhado. *@
                                    <td class="aj-tabela__acao aj-tabela__acao--varias"><button type="button" class="aj-botao aj-botao--fantasma aj-botao--menor" disabled="@Ocupada" @onclick="() => RenomearLinha(documento)">Renomear</button>@if (Gerais)
                                        {<button type="button" class="aj-botao aj-botao--fantasma aj-botao--menor" disabled="@Ocupada" @onclick="() => AbrirAcessoDaLinhaAsync(documento.Id)">Acesso</button>}<button type="button" class="aj-botao aj-botao--perigo aj-botao--menor" disabled="@Ocupada" @onclick="() => ExcluirLinha(documento.Id)">Excluir</button></td>
                                }
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
        </div>

        <div class="aj-tablet-mobile">
            <div class="aj-linhas">
                @foreach (var documento in _pagina.Itens)
                {
                    <div class="aj-linha" @key="documento.Id">
                        <div class="aj-linha__texto"><a href="@TextosDeDocumento.Endereco(documento.Id)" download data-enhance-nav="false" class="aj-linha__titulo">@documento.Nome</a><span class="aj-ajuda">@TextosDeDocumento.Detalhe(documento.TamanhoBytes, documento.EnviadoEm, documento.EnviadoPor, Fuso)</span></div>
                        @if (PodeGerenciar)
                        {
                            <MenuDeAcoes Rotulo="@($"Ações de {documento.Nome}")">
                                <a href="@TextosDeDocumento.Endereco(documento.Id)" download data-enhance-nav="false" class="aj-menu-acoes__item">Baixar</a>
                                <button type="button" class="aj-menu-acoes__item" disabled="@Ocupada" @onclick="() => RenomearLinha(documento)">Renomear</button>
                                @if (Gerais)
                                {
                                    <button type="button" class="aj-menu-acoes__item" disabled="@Ocupada" @onclick="() => AbrirAcessoDaLinhaAsync(documento.Id)">Acesso</button>
                                }
                                <button type="button" class="aj-menu-acoes__item aj-menu-acoes__item--perigo" disabled="@Ocupada" @onclick="() => ExcluirLinha(documento.Id)">Excluir</button>
                            </MenuDeAcoes>
                        }
                    </div>
                }
            </div>
        </div>

        <Paginacao Numero="_pagina.Numero" TotalDePaginas="_pagina.TotalDePaginas" Total="_pagina.Total"
                   Singular="documento" Plural="documentos" Ocupado="@Ocupada" IrPara="IrParaAsync" />
    </EstadoDaLista>

    @if (PodeGerenciar && Gerais)
    {
        <div class="aj-so-desktop">
            <span class="aj-ajuda">A Diretoria acessa sempre. Em “Acesso” você escolhe os perfis e os usuários que também veem cada documento geral.</span>
        </div>
    }
</section>

@if (_envioAberto)
{
    <section class="aj-painel">
        <h2>Enviar documento</h2>
        <span class="aj-ajuda">@($"Vai para {NomeDaArea(_categoria)}.")</span>
        <label class="aj-rotulo">@RotuloDoArquivo<InputFile @key="_versaoDoCampo" @ref="_campoDeArquivo" OnChange="Escolher" class="aj-campo" /></label>
        <label class="aj-rotulo">Nome (opcional)<input type="text" class="aj-campo" @bind="_nomeDoEnvio" /></label>
        <div class="aj-painel__acoes"><button type="button" class="aj-botao aj-botao--secundario" disabled="@_enviando" @onclick="DescartarEnvio">Cancelar</button><button type="button" class="aj-botao aj-botao--primario" disabled="@(_arquivo is null || _enviando)" @onclick="EnviarAsync">@(_enviando ? "Enviando…" : "Enviar")</button></div>
    </section>
}

@if (_renomeando is { } renomeando)
{
    <section class="aj-painel">
        <h2>@($"Renomear \"{renomeando.Nome}\"")</h2>
        <label class="aj-rotulo">Novo nome<input type="text" class="aj-campo" @bind="_novoNome" @ref="_campoDoNovoNome" /></label>
        <div class="aj-painel__acoes"><button type="button" class="aj-botao aj-botao--secundario" @onclick="FecharRenomear">Cancelar</button><button type="button" class="aj-botao aj-botao--primario" @onclick="RenomearAsync">Salvar nome</button></div>
    </section>
}

@if (_acesso is { } acesso)
{
    <section class="aj-painel">
        <h2>@($"Quem acessa \"{acesso.Nome}\"")</h2>
        <span class="aj-ajuda">A Diretoria acessa sempre. Marque os perfis e os usuários que também podem ver este documento; desmarque para retirar.</span>
        <h3>Perfis</h3>
        <div class="aj-opcoes">
            @foreach (var perfil in _perfis)
            {
                <label class="aj-opcao" @key="perfil.Id"><input type="checkbox" class="aj-opcao__caixa" checked="@_perfisMarcados.Contains(perfil.Id)" @onchange="e => Marcar(_perfisMarcados, perfil.Id, e)" />@perfil.Rotulo</label>
            }
        </div>
        <h3>Usuários</h3>
        <div class="aj-opcoes">
            @foreach (var usuario in _usuarios)
            {
                <label class="aj-opcao" @key="usuario.Id"><input type="checkbox" class="aj-opcao__caixa" checked="@_usuariosMarcados.Contains(usuario.Id)" @onchange="e => Marcar(_usuariosMarcados, usuario.Id, e)" />@usuario.Rotulo</label>
            }
        </div>
        <div class="aj-painel__acoes"><button type="button" class="aj-botao aj-botao--secundario" @onclick="FecharAcesso">Cancelar</button><button type="button" class="aj-botao aj-botao--primario" @onclick="SalvarAcessoAsync">Salvar acesso</button></div>
    </section>
}
```

O primeiro campo do painel de acesso precisa de referência para o foco (`private ElementReference _primeiroDoAcesso;`): escreva os laços de perfis e de usuários com o índice (`@foreach (var (perfil, indice) in _perfis.Select((p, i) => (p, i)))`) e, no item que é o primeiro do painel (o primeiro perfil; sem perfis, o primeiro usuário), um `@if` que desenha a mesma `label` com `@ref="_primeiroDoAcesso"` no `input`. Sem nenhuma opção, o `@ref` vai no botão "Cancelar" do painel.

No `@code`:

```csharp
    [Parameter, EditorRequired] public Guid ClienteId { get; set; }
    [Parameter] public bool PodeGerenciar { get; set; }

    /// <summary>A soma dos documentos que o usuário lê no cliente, depois de cada leitura.</summary>
    [Parameter] public EventCallback<int> AoContar { get; set; }

    private CategoriaDocumento _categoria = CategoriaDocumento.DocumentoGeral;
    private InputFile? _campoDeArquivo;
    private ElementReference _campoDoNovoNome;

    protected override CategoriaDocumento CategoriaDosDocumentos => _categoria;
    protected override Guid? ClienteDosDocumentos => ClienteId;

    // Nenhuma área da ficha pede confirmação de envio: só os Documentos comuns.
    protected override ValueTask FocarAvisoDeEnvioAsync() => ValueTask.CompletedTask;

    // Uma coisa aberta por vez: o campo é o do painel que está na tela.
    protected override ValueTask FocarPainelAbertoAsync() =>
        _renomeando is not null ? _campoDoNovoNome.FocusAsync()
        : _acesso is not null ? _primeiroDoAcesso.FocusAsync()
        : _envioAberto && _campoDeArquivo?.Element is { } campo ? campo.FocusAsync()
        : ValueTask.CompletedTask;

    protected override Task AoCarregarAsync() => AoContar.InvokeAsync(_porCategoria.Values.Sum());

    private IReadOnlyList<CategoriaDocumento> Areas => PodeGerenciar
        ? [CategoriaDocumento.DocumentoGeral, CategoriaDocumento.ContratoHonorarios, CategoriaDocumento.DocumentoAtividade, CategoriaDocumento.DocumentoDoCliente]
        : [CategoriaDocumento.DocumentoGeral, CategoriaDocumento.DocumentoAtividade];

    private bool Gerais => _categoria == CategoriaDocumento.DocumentoGeral;

    private static string NomeDaArea(CategoriaDocumento area) => area switch
    {
        CategoriaDocumento.DocumentoGeral => "Gerais",
        CategoriaDocumento.ContratoHonorarios => "Contratos de honorários",
        CategoriaDocumento.DocumentoAtividade => "De atividades",
        _ => "Do cliente",
    };

    private string RotuloDoChip(CategoriaDocumento area) =>
        _porCategoria.TryGetValue(area, out var quantos) ? $"{NomeDaArea(area)} · {quantos}" : NomeDaArea(area);

    private string RotuloDoArquivo => $"Arquivo (até {DocumentosOpcoes.TamanhoMaximoBytes / UmMegabyte} MB)";

    private Linha? AExcluir => _pagina.Itens.FirstOrDefault(documento => documento.Id == _excluindo);

    private async Task TrocarAreaAsync(CategoriaDocumento area)
    {
        if (Ocupada || area == _categoria) return;

        if (_envioAberto) DescartarEnvio();
        FecharRenomear();
        FecharAcesso();
        CancelarExclusao();
        _categoria = area;
        await IrParaAsync(1);   // apaga o aviso
    }
```

A Produção recebe da consulta a contagem só de Gerais e De atividades (Tarefa 3); a soma do `AoContar` é a das áreas que vieram. Apague `DocumentosDoCliente.razor` (o override vazio de `AoConcluirEnvio` sai com ele).

- [ ] **Step 3: Trava e testes**

Acrescente a `Migradas`: `Components/Documentos/DocumentosDaFicha.razor`.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DocumentosDaFicha|FullyQualifiedName~DocumentosComunsInterativosTests|FullyQualifiedName~DocumentosDoPortalWebTests"`
Expected: 0 avisos; todos passando.

- [ ] **Step 4: Commit**

```
git add src tests
git commit -m "feat(web): painel de documentos da ficha do cliente, com chips por área, uma coisa aberta por vez e acesso em opções marcáveis"
```

---

### Task 8: Ficha do cliente

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Pages/ClienteFicha.razor`
- Create: `tests/AtividadesJuridicas.IntegrationTests/ClienteFichaWebTests.cs`
- Create: `tests/AtividadesJuridicas.IntegrationTests/ClienteFichaInterativaTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PortalDoClienteWebTests.cs` (theory `Cliente_em_pagina_interna_ve_nao_encontrada_e_nenhum_dado`)
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PaginasProtegidasTests.cs` (a rota nova entre as que pedem login)
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: `CabecalhoDeFicha`, `Abas`/`Aba`, `DocumentosDaFicha`, `EstadoDaLista`, `Aviso`, `Recados`, `Icone` (`avancar`), `TextosDoCliente` (`Tipo`, `Documento`, `Endereco`, `ConsequenciaDaInativacao`, `Inativado`), `RotulosDeProcesso`, `FusoDoEscritorio`; `ObterMeuAcesso`, `ObterCliente`, `ListarProcessos(ClienteId: id)`, `ListarUsuariosDoCliente`, `DefinirAtivoCliente`.
- Produces: a rota `/clientes/{Id:guid}`; o link `/clientes/{id}/editar` ("Editar"), que a Tarefa 9 atende.

**Regras:**
- Quadros: `Cliente.dc.html`, `ClienteTablet.dc.html`, `ClienteMobile.dc.html`.
- Carregamento: o perfil antes de tudo; depois `ObterCliente`; com o cliente, `ListarProcessos(ClienteId: id)` e, só para a Diretoria, `ListarUsuariosDoCliente`. Acesso negado em qualquer consulta encerra a tela (`Seu acesso foi encerrado. Entre novamente.`, sem dado). `NaoEncontrado`: `Cliente não encontrado.` com o cabeçalho de voltar, sem painel. Outra falha de `ObterCliente`: o erro com "Tentar de novo". Falha de processos ou de usuários: o erro no lugar da lista daquele painel, com "Tentar de novo" (o resto da ficha aparece).
- Cabeçalho (`CabecalhoDeFicha`, `TituloDaBarra="Cliente"`, voltar `/clientes` "Clientes"): subtítulo `{tipo} · {documento} · {situação}`; resumo do mobile `{tipo} · {situação}`. Ações do desktop, só para a Diretoria: `Inativar` (fantasma; `Ativar` para o inativo) e `Editar` (secundário, link para a edição). No tablet e no mobile, a barra fixa (`aj-tablet-mobile` > `aj-acoes aj-acoes--fixa`) com `Inativar`/`Ativar` (secundário) e `Editar` (primário), só para a Diretoria.
- Inativar: o `Aviso` de confirmação no topo (abaixo do cabeçalho, antes das abas), `Rotulo="Confirmar inativação"`, título `Confirmar a inativação de {nome}?`, texto `TextosDoCliente.ConsequenciaDaInativacao(ativos)` (os usuários ativos da lista já carregada; nulo se ela falhou), `Cancelar` e `Confirmar inativação`. Confirmada com sucesso: o cliente passa a inativo na tela (sem recarregar) e o aviso de sucesso diz `TextosDoCliente.Inativado(ativos ?? 0)`. Ativar: de um clique, aviso `Cliente ativado.`. Falha: aviso de erro com a mensagem, e a situação não muda. Com uma ação em curso, os botões esperam.
- Recado de outra tela (a edição, na Tarefa 9): mostrado uma vez, no topo.
- Arranjo: `div.aj-lado-a-lado.aj-lado-a-lado--painel-primeiro.aj-lado-a-lado--ficha` com, primeiro, `div.aj-lado-a-lado__painel.aj-pilha` (Dados e, se houver texto, Observações) e, depois, `div.aj-lado-a-lado__lista.aj-pilha` (Processos do cliente, o `DocumentosDaFicha` e, só para a Diretoria, Usuários do cliente). Cada painel (ou grupo de painéis de uma aba) num invólucro `div` com `aj-aba-painel` (e `aj-aba-painel--oculta` quando a aba escolhida é outra). As abas (`aj-so-mobile` > `Abas`, `Rotulo="Assuntos do cliente"`): `Dados`, `Processos {n}` (o total da consulta), `Documentos {n}` (o `AoContar`; sem contador até a primeira leitura), `Usuários {n}` (só a Diretoria). Começa em `dados`.
- Painel Dados: `dl.aj-detalhes` com os pares do quadro para o tipo do cliente (PJ e PF, em "Textos exatos"); `—` no vazio; data de nascimento `dd/MM/yyyy`; endereço `TextosDoCliente.Endereco` ou `—`.
- Painel Observações: só com texto; `<p>` com o texto.
- Painel Processos do cliente: no desktop, tabela em caixa plana (`Título` principal, `Número` em `aj-tabela__data` ou `Sem número` em `aj-tabela__secundaria`, `Situação`, e "Abrir" para `/processos/{id}`); no tablet e no mobile, `div.aj-linhas.aj-linhas--folgadas` com uma `a.aj-linha.aj-linha--link` por processo (título em `aj-linha__titulo`, `{número|Sem número} · {situação}` em `aj-ajuda`, e o ícone `avancar` com `Classe="aj-cartao__seta"`). Vazio: `Nenhum processo vinculado.`. Mais de uma página: `Há mais processos; veja todos em Processos.` em `aj-ajuda`.
- Painel Usuários do cliente (Diretoria): com o cliente inativo, primeiro a ajuda `Cliente inativo: estes usuários estão sem acesso ao portal até o cliente ser reativado.`; no desktop, tabela em caixa plana (`Nome` principal, `E-mail` secundária, `Situação`, `Último acesso` em `aj-tabela__data`, com `—` sem acesso) e, abaixo, a ajuda `O acesso do cliente ao portal é criado em Usuários.` (só no desktop, como no quadro); no tablet e no mobile, `div.aj-linhas` com uma `div.aj-linha` por usuário (`aj-linha__texto` com o nome e o e-mail; `span.aj-ajuda.aj-linha__lado` com a situação, `<br />` e `Último acesso {data}` ou `Sem acesso registrado`). Vazio: `Nenhum usuário. Crie o acesso do cliente em Usuários.`.
- Produção: a mesma ficha, sem as ações, sem a barra fixa, sem o painel e a aba de usuários, e com o `DocumentosDaFicha` sem `PodeGerenciar` (Review Focus 3).

- [ ] **Step 1: Testes (devem falhar)**

`ClienteFichaWebTests.cs`:

```csharp
using System.Net;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ClienteFichaWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _litoral, _processo;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var area = await _app.ExecutarAsync(_admin, new SalvarAreaAtuacao(null, "Trabalhista"));
        var litoral = await _app.ExecutarAsync(_admin, new SalvarClientePessoaJuridica(
            null,
            new DadosDoClienteEntrada("Transportes Litoral Ltda.", "juridico@litoral.example", "(82) 3000-0002",
                new EnderecoEntrada("Av. do Porto", "1200", null, "Jaraguá", "Maceió", "AL"), "Faturamento no dia 10."),
            "11.222.333/0001-81", "Litoral Transportes", "Paulo Nogueira", null, null));
        litoral.Sucesso.Should().BeTrue(litoral.Mensagem);
        _litoral = litoral.Valor;
        var processo = await _app.ExecutarAsync(_admin, new SalvarProcesso(
            null, new DadosDoProcessoEntrada(null, "Litoral — reclamação trabalhista", area.Valor, null, null, null, null, null, null),
            [new ClienteDoProcessoEntrada(_litoral, PoloProcessual.Passivo)]));
        processo.Sucesso.Should().BeTrue(processo.Mensagem);
        _processo = processo.Valor;
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> PaginaAsync(string rota, bool comoPedro = false)
    {
        var cliente = comoPedro
            ? await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro)
            : await _app.ClienteAutenticadoAsync();
        return WebUtility.HtmlDecode(await cliente.GetStringAsync(rota));
    }

    [Fact]
    public async Task Diretoria_ve_o_cabecalho_os_dados_os_processos_e_as_acoes()
    {
        var html = await PaginaAsync($"/clientes/{_litoral}");

        html.Should().Contain("<h1 class=\"aj-titulo\">Transportes Litoral Ltda.</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Pessoa jurídica · CNPJ 11.222.333/0001-81 · Ativo</p>")
            .And.Contain("<span class=\"aj-ajuda\">Pessoa jurídica · Ativo</span>")
            .And.Contain("<h1>Cliente</h1>")
            .And.MatchRegex("<div class=\"aj-lado-a-lado aj-lado-a-lado--painel-primeiro aj-lado-a-lado--ficha\">\\s*<div class=\"aj-lado-a-lado__painel aj-pilha\">")
            .And.MatchRegex("(?s)<h2>Dados</h2>\\s*<dl class=\"aj-detalhes\">\\s*<div><dt>CNPJ</dt><dd>11.222.333/0001-81</dd></div>\\s*<div><dt>Nome fantasia</dt><dd>Litoral Transportes</dd></div>\\s*<div><dt>Responsável</dt><dd>Paulo Nogueira</dd></div>\\s*<div><dt>Inscrição estadual</dt><dd>—</dd></div>")
            .And.Contain("<div><dt>Endereço</dt><dd>Av. do Porto, 1200, Jaraguá, Maceió/AL</dd></div>")
            .And.MatchRegex("(?s)<h2>Observações</h2>\\s*<p>Faturamento no dia 10.</p>");

        BlocoDeVisao.De(html, "aj-so-desktop").Should()
            .MatchRegex("(?s)<div class=\"aj-painel__botoes\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--fantasma\"[^>]*>Inativar</button>\\s*<a href=\"/clientes/[0-9a-f-]{36}/editar\" class=\"aj-botao aj-botao--secundario\">Editar</a>")
            .And.MatchRegex($"(?s)<td class=\"aj-tabela__principal\">Litoral — reclamação trabalhista</td>\\s*<td class=\"aj-tabela__secundaria\">Sem número</td>\\s*<td>Ativo</td>\\s*<td class=\"aj-tabela__acao\"><a href=\"/processos/{_processo}\" class=\"aj-botao aj-botao--fantasma aj-botao--seta\">");
        var tabletEMobile = BlocoDeVisao.De(html, "aj-tablet-mobile");
        tabletEMobile.Should().MatchRegex($"(?s)<a href=\"/processos/{_processo}\" class=\"aj-linha aj-linha--link\">.*?<span class=\"aj-ajuda\">Sem número · Ativo</span>")
            .And.MatchRegex("(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--secundario\"[^>]*>Inativar</button>\\s*<a href=\"/clientes/[0-9a-f-]{36}/editar\" class=\"aj-botao aj-botao--primario\">Editar</a>");
        BlocoDeVisao.De(html, "aj-so-mobile").Should().MatchRegex(
            "(?s)<div class=\"aj-abas\" role=\"tablist\" aria-label=\"Assuntos do cliente\">.*?aria-selected=\"true\"[^>]*>Dados</button>.*?>Processos<span class=\"aj-abas__contador\">1</span></button>.*?>Usuários<span class=\"aj-abas__contador\">0</span></button>");
        html.Should().Contain("Nenhum usuário. Crie o acesso do cliente em Usuários.").And.Contain("<h2>Documentos</h2>");
        // Na aba Dados, os outros painéis ficam ocultos no mobile.
        html.Should().MatchRegex("<div class=\"aj-aba-painel aj-aba-painel--oculta\">\\s*<section class=\"aj-painel\">\\s*<h2>Processos do cliente</h2>")
            .And.MatchRegex("<div class=\"aj-aba-painel\">\\s*<section class=\"aj-painel\">\\s*<h2>Dados</h2>");
    }

    [Fact]
    public async Task Producao_ve_a_ficha_sem_acoes_sem_usuarios_e_so_com_as_areas_dela()
    {
        await _app.CriarPedroAsync();

        var html = await PaginaAsync($"/clientes/{_litoral}", comoPedro: true);

        html.Should().Contain("<h1 class=\"aj-titulo\">Transportes Litoral Ltda.</h1>").And.Contain("<h2>Dados</h2>")
            .And.Contain("Litoral — reclamação trabalhista").And.Contain(">Gerais · 0</button>");
        html.Should().NotContain("Inativar").And.NotContain("/editar").And.NotContain("aj-acoes--fixa")
            .And.NotContain("Usuários do cliente").And.NotContain("Contratos de honorários").And.NotContain("Enviar documento")
            .And.NotContain("Seu acesso foi encerrado");
    }

    [Fact]
    public async Task Cliente_que_nao_existe_mostra_nao_encontrado_com_o_voltar_e_nada_mais()
    {
        var html = await PaginaAsync($"/clientes/{Guid.NewGuid()}");

        html.Should().Contain("Cliente não encontrado.").And.Contain("aria-label=\"Voltar para Clientes\"")
            .And.MatchRegex("<a href=\"/clientes\" class=\"aj-voltar\">");
        html.Should().NotContain("<h2>Dados</h2>").And.NotContain("<h2>Documentos</h2>").And.NotContain("Inativar")
            .And.NotContain("aj-abas");
    }

    [Fact]
    public async Task Cliente_inativo_oferece_ativar_e_avisa_dos_usuarios_sem_acesso()
    {
        (await _app.ExecutarAsync(_admin, new DefinirAtivoCliente(_litoral, false))).Sucesso.Should().BeTrue();

        var html = await PaginaAsync($"/clientes/{_litoral}");

        html.Should().Contain("· Inativo</p>").And.Contain(">Ativar</button>").And.NotContain(">Inativar</button>");
    }
}
```

(Se o CNPJ do teste não for válido para o domínio, use um que seja, como os dos outros testes. O cliente inativo sem usuários não mostra a nota dos usuários, porque a lista está vazia: o teste interativo cobre a nota.)

`ClienteFichaInterativaTests.cs` (bUnit, com o despachante combinado):

```csharp
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Web.Components.Pages;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

public class ClienteFichaInterativaTests : TelaInterativa
{
    private static readonly Guid Id = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c1");

    private void Combinar(bool diretoria = true, bool ativo = true, int usuariosAtivos = 2)
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(diretoria ? AtorDaDiretoria : AtorDaProducao)
            .Responde<ObterCliente, Resultado<ClienteDetalhe>>(Resultado<ClienteDetalhe>.Ok(new(Id, TipoCliente.PessoaJuridica,
                "Transportes Litoral Ltda.", null, null, null, null, null, null, null, null, null, null, ativo,
                null, null, null, "11.222.333/0001-81", null, null, null, null)))
            .Responde<ListarProcessos, Resultado<PaginaDeProcessos>>(Resultado<PaginaDeProcessos>.Ok(new([], 0, 1, 20, 0)))
            .Responde<ListarUsuariosDoCliente, Resultado<IReadOnlyList<UsuarioDoCliente>>>(Resultado<IReadOnlyList<UsuarioDoCliente>>.Ok(
                [.. Enumerable.Range(1, usuariosAtivos).Select(i => new UsuarioDoCliente(Guid.NewGuid(), $"Usuário {i}", $"u{i}@x.com", true, null))]))
            .Responde<ListarDocumentosDoCliente, Resultado<PaginaDeDocumentos>>(Resultado<PaginaDeDocumentos>.Ok(new([], 0, 1, 20,
                new Dictionary<CategoriaDocumento, int> { [CategoriaDocumento.DocumentoGeral] = 3, [CategoriaDocumento.DocumentoAtividade] = 4 })));
    }

    private IRenderedComponent<ClienteFicha> Ficha() => Desenhar<ClienteFicha>(p => p.Add(f => f.Id, Id));

    [Fact]
    public void Inativar_pede_a_confirmacao_com_os_usuarios_e_confirmar_deixa_a_ficha_inativa()
    {
        Combinar(usuariosAtivos: 2);
        Despachante.Responde<DefinirAtivoCliente, Resultado>(Resultado.Ok());
        var ficha = Ficha();

        ficha.FindAll(".aj-painel__botoes button").Single(b => b.TextContent == "Inativar").Click();

        ficha.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar inativação");
        ficha.Find("[role=alertdialog] strong").TextContent.Should().Be("Confirmar a inativação de Transportes Litoral Ltda.?");
        ficha.Find("[role=alertdialog]").TextContent.Should().Contain("2 usuários do portal ficam sem acesso até o cliente ser reativado.");

        ficha.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar inativação").Click();

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Cliente inativado. 2 usuários do portal ficaram sem acesso."));
        Despachante.Recebidas.OfType<DefinirAtivoCliente>().Should().ContainSingle().Which.Should().Be(new DefinirAtivoCliente(Id, false));
        ficha.Markup.Should().Contain("· Inativo</p>").And.Contain(">Ativar</button>")
            .And.Contain("Cliente inativo: estes usuários estão sem acesso ao portal até o cliente ser reativado.");
        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
    }

    [Fact]
    public void Cancelar_a_inativacao_nao_muda_nada()
    {
        Combinar();
        var ficha = Ficha();
        ficha.FindAll(".aj-painel__botoes button").Single(b => b.TextContent == "Inativar").Click();

        ficha.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Cancelar").Click();

        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        Despachante.Recebidas.OfType<DefinirAtivoCliente>().Should().BeEmpty();
    }

    [Fact]
    public void Tocar_numa_aba_mostra_os_paineis_dela_e_esconde_os_outros_no_mobile()
    {
        Combinar();
        var ficha = Ficha();

        ficha.FindAll(".aj-abas button").Single(b => b.TextContent.StartsWith("Documentos")).Click();

        ficha.Find(".aj-abas button[aria-selected=true]").TextContent.Should().Be("Documentos7");   // "Documentos" e o contador 3 + 4
        ficha.Markup.Should().MatchRegex("<div class=\"aj-aba-painel aj-aba-painel--oculta\">\\s*<section class=\"aj-painel\">\\s*<h2>Dados</h2>")
            .And.MatchRegex("<div class=\"aj-aba-painel\">\\s*(<[^>]+>\\s*)*?<section class=\"aj-painel\">\\s*<div class=\"aj-painel__cabecalho\">\\s*<h2>Documentos</h2>");
    }

    [Fact]
    public void Producao_nao_pede_os_usuarios_nem_as_areas_que_nao_le()
    {
        Combinar(diretoria: false);

        var ficha = Ficha();

        Despachante.Recebidas.OfType<ListarUsuariosDoCliente>().Should().BeEmpty();
        Despachante.Recebidas.OfType<ListarDocumentosDoCliente>().Should()
            .OnlyContain(r => r.Categoria == CategoriaDocumento.DocumentoGeral || r.Categoria == CategoriaDocumento.DocumentoAtividade);
        ficha.FindAll(".aj-abas button").Select(b => b.TextContent).Should().Equal("Dados", "Processos0", "Documentos7");
    }
}
```

(`Resultado.Ok()` como existe no projeto. O texto do botão da aba junta o rótulo e o contador porque eles estão colados: compare assim ou pelo `span` do contador.)

Em `PortalDoClienteWebTests`, acrescente `[InlineData("/clientes/0199a3f2-7c1e-7000-8000-000000000001")]` à theory `Cliente_em_pagina_interna_ve_nao_encontrada_e_nenhum_dado`; em `PaginasProtegidasTests`, a mesma rota entre as que levam o anônimo ao login.

Run: `dotnet build -m:1`
Expected: não compila (`ClienteFicha` não existe). É o RED.

- [ ] **Step 2: A tela**

```razor
@page "/clientes/{Id:guid}"
@inject IDespachante Despachante
@inject FusoDoEscritorio Fuso
@inject Recados Recados

<PageTitle>@(_cliente?.Nome ?? "Cliente")</PageTitle>

@if (!_carregado || _ator is null || _cliente is null)
{
    <CabecalhoDeDetalhe Titulo="Cliente" VoltarPara="/clientes" Origem="Clientes" />
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
        <Aviso Tipo="TipoDeAviso.Erro">Cliente não encontrado.</Aviso>
    }
    else
    {
        <EstadoDaLista Erro="@(_erro ?? "Não foi possível consultar o cliente.")" TextoDeVazio="" TentarDeNovo="CarregarAsync" />
    }
}
else
{
    var c = _cliente;
    <CabecalhoDeFicha Titulo="@c.Nome" Subtitulo="@Subtitulo" Resumo="@Resumo" TituloDaBarra="Cliente" VoltarPara="/clientes" Origem="Clientes"
                      Acoes="@(Diretoria ? AcoesDoDesktop : null)" />

    @if (_recado is { } recado)
    {
        <Aviso Tipo="@recado.Tipo">@recado.Texto</Aviso>
    }
    @if (_mensagem is not null)
    {
        <Aviso Tipo="@(_falhou ? TipoDeAviso.Erro : TipoDeAviso.Sucesso)">@_mensagem</Aviso>
    }
    @if (_confirmandoInativacao)
    {
        <Aviso Tipo="TipoDeAviso.Atencao" Rotulo="Confirmar inativação" Titulo="@($"Confirmar a inativação de {c.Nome}?")">
            <ChildContent>@TextosDoCliente.ConsequenciaDaInativacao(UsuariosAtivos)</ChildContent>
            <Acoes><button type="button" class="aj-botao aj-botao--secundario" disabled="@_alterando" @onclick="() => _confirmandoInativacao = false">Cancelar</button><button type="button" class="aj-botao aj-botao--primario" disabled="@_alterando" @onclick="ConfirmarInativacaoAsync">Confirmar inativação</button></Acoes>
        </Aviso>
    }

    <div class="aj-so-mobile">
        <Abas Rotulo="Assuntos do cliente" Itens="@AbasDaFicha" Ativa="@_aba" AoEscolher="(string aba) => _aba = aba" />
    </div>

    <div class="aj-lado-a-lado aj-lado-a-lado--painel-primeiro aj-lado-a-lado--ficha">
        <div class="aj-lado-a-lado__painel aj-pilha">
            <div class="@ClasseDaAba("dados")">
                <section class="aj-painel">
                    <h2>Dados</h2>
                    <dl class="aj-detalhes">
                        @foreach (var (termo, valor) in Detalhes)
                        {
                            <div><dt>@termo</dt><dd>@valor</dd></div>
                        }
                    </dl>
                </section>
                @if (!string.IsNullOrWhiteSpace(c.Observacoes))
                {
                    <section class="aj-painel">
                        <h2>Observações</h2>
                        <p>@c.Observacoes</p>
                    </section>
                }
            </div>
        </div>

        <div class="aj-lado-a-lado__lista aj-pilha">
            <div class="@ClasseDaAba("processos")">
                <section class="aj-painel">
                    <h2>Processos do cliente</h2>
                    <EstadoDaLista Erro="@_erroDosProcessos" Vazio="@(_processos.Itens.Count == 0)" TextoDeVazio="Nenhum processo vinculado." TentarDeNovo="CarregarProcessosAsync">
                        <div class="aj-so-desktop">
                            <div class="aj-tabela-caixa aj-tabela-caixa--plana">
                                <table class="aj-tabela">
                                    <thead><tr><th scope="col">Título</th><th scope="col">Número</th><th scope="col">Situação</th><th scope="col"><span class="aj-so-leitor">Ações</span></th></tr></thead>
                                    <tbody>
                                        @foreach (var processo in _processos.Itens)
                                        {
                                            <tr @key="processo.Id">
                                                <td class="aj-tabela__principal">@processo.Titulo</td>
                                                <td class="@(processo.NumeroCnj is null ? "aj-tabela__secundaria" : "aj-tabela__data")">@RotulosDeProcesso.Numero(processo.NumeroCnj)</td>
                                                <td>@RotulosDeProcesso.Situacao(processo.Status)</td>
                                                <td class="aj-tabela__acao"><a href="@($"/processos/{processo.Id}")" class="aj-botao aj-botao--fantasma aj-botao--seta">Abrir<Icone Nome="avancar" /></a></td>
                                            </tr>
                                        }
                                    </tbody>
                                </table>
                            </div>
                        </div>
                        <div class="aj-tablet-mobile">
                            <div class="aj-linhas aj-linhas--folgadas">
                                @foreach (var processo in _processos.Itens)
                                {
                                    <a href="@($"/processos/{processo.Id}")" class="aj-linha aj-linha--link" @key="processo.Id"><span class="aj-linha__texto"><span class="aj-linha__titulo">@processo.Titulo</span><span class="aj-ajuda">@($"{RotulosDeProcesso.Numero(processo.NumeroCnj)} · {RotulosDeProcesso.Situacao(processo.Status)}")</span></span><Icone Nome="avancar" Classe="aj-cartao__seta" /></a>
                                }
                            </div>
                        </div>
                        @if (_processos.Total > _processos.Itens.Count)
                        {
                            <span class="aj-ajuda">Há mais processos; veja todos em Processos.</span>
                        }
                    </EstadoDaLista>
                </section>
            </div>

            <div class="@ClasseDaAba("documentos")">
                <DocumentosDaFicha ClienteId="c.Id" PodeGerenciar="Diretoria" AoContar="(int total) => _totalDeDocumentos = total"
                                   OnAcessoEncerrado="EncerrarAcesso" />
            </div>

            @if (Diretoria)
            {
                <div class="@ClasseDaAba("usuarios")">
                    <section class="aj-painel">
                        <h2>Usuários do cliente</h2>
                        @if (!c.Ativo && _usuarios.Count > 0)
                        {
                            <span class="aj-ajuda">Cliente inativo: estes usuários estão sem acesso ao portal até o cliente ser reativado.</span>
                        }
                        <EstadoDaLista Erro="@_erroDosUsuarios" Vazio="@(_usuarios.Count == 0)" TextoDeVazio="Nenhum usuário. Crie o acesso do cliente em Usuários." TentarDeNovo="CarregarUsuariosAsync">
                            <div class="aj-so-desktop">
                                <div class="aj-tabela-caixa aj-tabela-caixa--plana">
                                    <table class="aj-tabela">
                                        <thead><tr><th scope="col">Nome</th><th scope="col">E-mail</th><th scope="col">Situação</th><th scope="col">Último acesso</th></tr></thead>
                                        <tbody>
                                            @foreach (var usuario in _usuarios)
                                            {
                                                <tr @key="usuario.Id">
                                                    <td class="aj-tabela__principal">@usuario.Nome</td>
                                                    <td class="aj-tabela__secundaria">@usuario.Email</td>
                                                    <td>@(usuario.Ativo ? "Ativo" : "Inativo")</td>
                                                    <td class="aj-tabela__data">@(usuario.UltimoAcessoEm is null ? "—" : Fuso.Formatar(usuario.UltimoAcessoEm))</td>
                                                </tr>
                                            }
                                        </tbody>
                                    </table>
                                </div>
                                <span class="aj-ajuda">O acesso do cliente ao portal é criado em Usuários.</span>
                            </div>
                            <div class="aj-tablet-mobile">
                                <div class="aj-linhas">
                                    @foreach (var usuario in _usuarios)
                                    {
                                        <div class="aj-linha" @key="usuario.Id"><span class="aj-linha__texto"><span class="aj-linha__titulo">@usuario.Nome</span><span class="aj-ajuda">@usuario.Email</span></span><span class="aj-ajuda aj-linha__lado">@(usuario.Ativo ? "Ativo" : "Inativo")<br />@Acesso(usuario)</span></div>
                                    }
                                </div>
                            </div>
                        </EstadoDaLista>
                    </section>
                </div>
            }
        </div>
    </div>

    @if (Diretoria)
    {
        <div class="aj-tablet-mobile">
            <div class="aj-acoes aj-acoes--fixa"><button type="button" class="aj-botao aj-botao--secundario" disabled="@_alterando" @onclick="AlternarAtivoAsync">@(c.Ativo ? "Inativar" : "Ativar")</button><a href="@Edicao" class="aj-botao aj-botao--primario">Editar</a></div>
        </div>
    }
}
```

O `<span class="aj-ajuda">O acesso…</span>` dentro do `aj-so-desktop` do `EstadoDaLista` só aparece com a lista: com a lista vazia, o texto do vazio já diz onde criar o acesso.

No `@code`:

- `[Parameter] public Guid Id { get; set; }`.
- Estado: `_carregado`, `_ator`, `_cliente` (`ClienteDetalhe?`), `_naoEncontrado`, `_erro`, `_processos` (`Pagina<ProcessoResumo>`, vazia), `_erroDosProcessos`, `_usuarios` (`IReadOnlyList<UsuarioDoCliente>`), `_erroDosUsuarios`, `_totalDeDocumentos` (`int?`), `_aba = "dados"`, `_confirmandoInativacao`, `_alterando`, `_mensagem`, `_falhou`, `_recado`.
- `Diretoria => _ator?.EhDiretoria == true`; `Edicao => $"/clientes/{Id}/editar"`.
- `OnParametersSetAsync`: quando o `Id` muda, `CarregarAsync()` (perfil, cliente, processos, usuários da Diretoria, como nas Regras) e `_recado = Recados.Retirar()` na primeira vez.
- `Subtitulo => $"{TextosDoCliente.Tipo(c.Tipo)} · {TextosDoCliente.Documento(c.Tipo, c.Cpf ?? c.Cnpj ?? "")} · {Situacao}"`; `Resumo => $"{TextosDoCliente.Tipo(c.Tipo)} · {Situacao}"`.
- `Detalhes`: a lista de pares do tipo do cliente, na ordem do quadro (`Ou(v) => string.IsNullOrWhiteSpace(v) ? "—" : v`).
- `AbasDaFicha`: `new("dados", "Dados")`, `new("processos", "Processos", _processos.Total)`, `new("documentos", "Documentos", _totalDeDocumentos)` e, para a Diretoria, `new("usuarios", "Usuários", _usuarios.Count)`.
- `ClasseDaAba(string aba) => aba == _aba ? "aj-aba-painel" : "aj-aba-painel aj-aba-painel--oculta"`.
- `AcoesDoDesktop` (`RenderFragment`), os dois colados: `@<text><button type="button" class="aj-botao aj-botao--fantasma" disabled="@_alterando" @onclick="AlternarAtivoAsync">@(_cliente!.Ativo ? "Inativar" : "Ativar")</button><a href="@Edicao" class="aj-botao aj-botao--secundario">Editar</a></text>`.
- `UsuariosAtivos => _erroDosUsuarios is null ? _usuarios.Count(u => u.Ativo) : null`.
- `AlternarAtivoAsync`: ativo → `_confirmandoInativacao = true` (apaga o aviso); inativo → `DefinirAtivoCliente(Id, true)` e, com sucesso, `_cliente = _cliente with { Ativo = true }` e `Cliente ativado.`. `ConfirmarInativacaoAsync`: `DefinirAtivoCliente(Id, false)`; com sucesso, `_cliente with { Ativo = false }` e `TextosDoCliente.Inativado(UsuariosAtivos ?? 0)`; fecha a confirmação. Acesso negado em qualquer uma: `EncerrarAcesso()`. Guarda de clique duplo com `_alterando`.
- `EncerrarAcesso()`: `_ator = null`, `_cliente = null` (some tudo).
- `Acesso(u) => u.UltimoAcessoEm is null ? "Sem acesso registrado" : $"Último acesso {Fuso.Formatar(u.UltimoAcessoEm)}"`.

O foco no título visível ao entrar é o do layout (9A); a tela não pede foco.

- [ ] **Step 3: Trava e testes**

Acrescente a `Migradas`: `Components/Pages/ClienteFicha.razor`.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ClienteFicha|FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~PaginasProtegidasTests"`
Expected: 0 avisos; todos passando.

- [ ] **Step 4: Commit**

```
git add src tests
git commit -m "feat(web): ficha do cliente em tela própria, com cabeçalho, abas do mobile, documentos por área e inativação confirmada"
```

---

### Task 9: Formulário de cliente em tela própria

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Pages/ClienteFormulario.razor`
- Create: `tests/AtividadesJuridicas.IntegrationTests/ClienteFormularioWebTests.cs`
- Create: `tests/AtividadesJuridicas.IntegrationTests/ClienteFormularioInterativoTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PortalDoClienteWebTests.cs` (theory das páginas internas), `PaginasProtegidasTests.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: `CabecalhoDeDetalhe`, `RotuloDeCampo` (com `Classe`), `Aviso`, `Recados`, `Icone` (`confirmar`); `ObterMeuAcesso`, `ObterCliente`, `SalvarClientePessoaFisica`, `SalvarClientePessoaJuridica` (`Resultado<Guid>`), que não mudam.
- Produces: as rotas `/clientes/novo` e `/clientes/{Id:guid}/editar`.

**Regras:**
- Quadros: `ClienteFormulario.dc.html`, `ClienteFormularioTablet.dc.html`, `ClienteFormularioMobile.dc.html`.
- A lógica do formulário vem de `Clientes.razor` de antes da Tarefa 6 (o `Formulario`, o `SalvarAsync`, a leitura do cliente para editar), sem mudar: nome e documento obrigatórios (`Informe o nome.`, `Informe o documento.`), os campos de cada tipo, o envio por `SalvarClientePessoaFisica` ou `SalvarClientePessoaJuridica`.
- O tipo: um controle segmentado `Tipo de cliente` (`Pessoa física`, `Pessoa jurídica`; botões `aj-segmentado__opcao`, `aria-pressed`). No novo, começa em `Pessoa física` (como o quadro) e troca os campos. Na edição, o tipo é o do cliente e a outra opção fica `disabled` (o backend não troca o tipo).
- No desktop e no tablet o segmentado fica no cabeçalho do painel `Identificação e contato` (`div.aj-painel__cabecalho` > `h2` e `div.aj-desktop-tablet` > segmentado); no mobile, antes dos painéis, largo e recuado (`aj-segmentado aj-segmentado--recuado aj-segmentado--largo`), seguido de `p.aj-ajuda` com o subtítulo (a barra de voltar do mobile não tem subtítulo). Os dois segmentados mudam o mesmo estado.
- Arranjo: o `EditForm` (`id="formulario-do-cliente"`) é o próprio `aj-lado-a-lado aj-lado-a-lado--formulario`: primeiro `section.aj-painel.aj-painel--principal.aj-lado-a-lado__lista` (Identificação e contato), depois `div.aj-lado-a-lado__painel.aj-pilha` (Endereço, Observações). No tablet e no mobile o arranjo some e os painéis empilham nessa ordem.
- Identificação e contato: `div.aj-grade-campos.aj-grade-campos--tablet-folgada` com, na PF, `Nome completo` (`Classe="aj-grade-campos__inteiro"`), `CPF` (exemplo `000.000.000-00`, `inputmode="numeric"`), `RG`, `Data de nascimento`; na PJ, `Razão social` (inteiro), `CNPJ` (exemplo `00.000.000/0000-00`, numérico), `Nome fantasia`, `Responsável`, `Inscrição estadual`, `Inscrição municipal`; e, nos dois, `E-mail` (`type="email"`) e `Telefone` (`type="tel"`). Nome e documento com `RotuloDeCampo` (erro por campo); os outros, `label.aj-rotulo`.
- Endereço: `div.aj-grade-endereco` com `Logradouro`, `Número`, `Complemento`, `Bairro`, `Cidade`, `UF` (`maxlength="2"`, campo com `aj-campo aj-campo--curto`) e `CEP` (numérico), cada rótulo com a classe da sua posição (`aj-grade-endereco__…`).
- Observações: `label.aj-rotulo` com `span.aj-so-leitor` `Observações` e a `textarea` com `rows="4"`, como no desktop. Os quadros do tablet e do mobile usam 3 linhas; com um campo só vale uma altura, e a diferença fica anotada para a conferência da Tarefa 13.
- Ações, fora do formulário, com `form="formulario-do-cliente"`: desktop `div.aj-so-desktop` > `div.aj-acoes.aj-acoes--fixa` com `Cancelar` (link fantasma para o voltar) e `Salvar cliente` (primário com o ícone `confirmar`); tablet `aj-so-tablet`, o mesmo com `Cancelar` secundário; mobile `aj-so-mobile`, `Cancelar` secundário e `Salvar`.
- Voltar: no novo, `/clientes` ("Clientes"); na edição, `/clientes/{id}` ("Cliente"). Salvar com sucesso: `Recados.Deixar("Cliente salvo.")` e navega para o voltar. Falha: `Aviso` de erro no topo, e o formulário fica com o que foi digitado. Acesso negado: encerra a tela. Guarda de clique duplo.
- Edição de um cliente que não existe: o cabeçalho, o voltar `/clientes` e `Cliente não encontrado.`, sem formulário.
- Produção: o cabeçalho e `Acesso negado.`, sem formulário e sem consulta do cliente.

- [ ] **Step 1: Testes (devem falhar)**

`ClienteFormularioWebTests.cs`:

```csharp
using System.Net;
using AtividadesJuridicas.Application.Cadastros;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ClienteFormularioWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _litoral;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var litoral = await _app.ExecutarAsync(_admin, new SalvarClientePessoaJuridica(
            null, new DadosDoClienteEntrada("Transportes Litoral Ltda.", null, null, new EnderecoEntrada(Uf: "AL"), null),
            "11.222.333/0001-81", "Litoral Transportes", null, null, null));
        litoral.Sucesso.Should().BeTrue(litoral.Mensagem);
        _litoral = litoral.Valor;
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> PaginaAsync(string rota) =>
        WebUtility.HtmlDecode(await (await _app.ClienteAutenticadoAsync()).GetStringAsync(rota));

    [Fact]
    public async Task Novo_cliente_comeca_em_pessoa_fisica_com_os_paineis_e_as_acoes_de_cada_visao()
    {
        var html = await PaginaAsync("/clientes/novo");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Novo cliente</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Só o nome e o CPF ou CNPJ são obrigatórios.</p>")
            .And.MatchRegex("<form[^>]*id=\"formulario-do-cliente\"[^>]*class=\"aj-lado-a-lado aj-lado-a-lado--formulario\"")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-painel--principal aj-lado-a-lado__lista\">\\s*<div class=\"aj-painel__cabecalho\">\\s*<h2>Identificação e contato</h2>")
            .And.MatchRegex("<div class=\"aj-segmentado\" role=\"group\" aria-label=\"Tipo de cliente\">\\s*<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"true\"[^>]*>Pessoa física</button>")
            .And.MatchRegex("<label class=\"aj-rotulo aj-grade-campos__inteiro\">\\s*Nome completo")
            .And.Contain("placeholder=\"000.000.000-00\"").And.Contain("Data de nascimento")
            .And.Contain("<div class=\"aj-grade-endereco\">")
            .And.MatchRegex("<label class=\"aj-rotulo aj-grade-endereco__uf\">\\s*UF<input[^>]*maxlength=\"2\"[^>]*class=\"aj-campo aj-campo--curto\"")
            .And.MatchRegex("<span class=\"aj-so-leitor\">Observações</span>");
        html.Should().NotContain("Razão social").And.NotContain("Inscrição estadual");

        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            "(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a href=\"/clientes\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>\\s*<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-do-cliente\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Salvar cliente</button>");
        BlocoDeVisao.De(html, "aj-so-mobile").Should()
            .MatchRegex("<div class=\"aj-segmentado aj-segmentado--recuado aj-segmentado--largo\" role=\"group\" aria-label=\"Tipo de cliente\">")
            .And.Contain("<p class=\"aj-ajuda\">Só o nome e o CPF ou CNPJ são obrigatórios.</p>")
            .And.MatchRegex("(?s)<div class=\"aj-acoes aj-acoes--fixa\">.*?Salvar</button>")
            .And.NotContain("Salvar cliente");
    }

    [Fact]
    public async Task Edicao_traz_o_tipo_travado_os_campos_preenchidos_e_o_voltar_para_a_ficha()
    {
        var html = await PaginaAsync($"/clientes/{_litoral}/editar");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Editar cliente</h1>")
            .And.MatchRegex($"<a href=\"/clientes/{_litoral}\" class=\"aj-voltar\">")
            .And.MatchRegex("<input[^>]*value=\"Transportes Litoral Ltda.\"").And.MatchRegex("<input[^>]*value=\"11.222.333/0001-81\"")
            .And.MatchRegex("<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\" disabled[^>]*>Pessoa física</button>")
            .And.Contain("Razão social").And.Contain("Inscrição municipal");
    }

    [Fact]
    public async Task Edicao_de_cliente_que_nao_existe_mostra_nao_encontrado_sem_formulario()
    {
        var html = await PaginaAsync($"/clientes/{Guid.NewGuid()}/editar");

        html.Should().Contain("Cliente não encontrado.").And.MatchRegex("<a href=\"/clientes\" class=\"aj-voltar\">");
        html.Should().NotContain("Identificação e contato").And.NotContain("Salvar cliente");
    }

    [Fact]
    public async Task Producao_nao_abre_o_formulario()
    {
        await _app.CriarPedroAsync();
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await pedro.GetStringAsync($"/clientes/{_litoral}/editar"));

        html.Should().Contain("Acesso negado.").And.MatchRegex("<h1 class=\"aj-titulo\">Editar cliente</h1>");
        html.Should().NotContain("Identificação e contato").And.NotContain("Transportes Litoral Ltda.");
    }
}
```

`ClienteFormularioInterativoTests.cs` (bUnit; o `NavigationManager` do bUnit registra o destino):

```csharp
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

public class ClienteFormularioInterativoTests : TelaInterativa
{
    [Fact]
    public void Trocar_para_pessoa_juridica_mostra_os_campos_dela_e_salvar_envia_e_volta_com_o_recado()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<SalvarClientePessoaJuridica, Resultado<Guid>>(Resultado<Guid>.Ok(Guid.NewGuid()));
        var tela = Desenhar<ClienteFormulario>();

        tela.FindAll("button.aj-segmentado__opcao").First(b => b.TextContent == "Pessoa jurídica").Click();
        tela.Markup.Should().Contain("Razão social").And.NotContain("Data de nascimento");
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.StartsWith("Razão social")).QuerySelector("input")!.Change("Cooperativa Boa Safra");
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.StartsWith("CNPJ")).QuerySelector("input")!.Change("11.222.333/0001-81");
        tela.Find("form#formulario-do-cliente").Submit();

        tela.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/clientes"));
        Despachante.Recebidas.OfType<SalvarClientePessoaJuridica>().Should().ContainSingle()
            .Which.Dados.Nome.Should().Be("Cooperativa Boa Safra");
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Cliente salvo.");
    }

    [Fact]
    public void Salvar_sem_nome_nem_documento_mostra_o_erro_nos_campos_e_nao_envia()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria);
        var tela = Desenhar<ClienteFormulario>();

        tela.Find("form#formulario-do-cliente").Submit();

        tela.Markup.Should().Contain("Informe o nome.").And.Contain("Informe o documento.").And.Contain("aj-rotulo--erro");
        Despachante.Recebidas.OfType<SalvarClientePessoaFisica>().Should().BeEmpty();
    }

    [Fact]
    public void Falha_ao_salvar_mostra_o_aviso_e_mantem_o_que_foi_digitado()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<SalvarClientePessoaFisica, Resultado<Guid>>(Resultado<Guid>.Falha(TipoErro.Validacao, "CPF já cadastrado."));
        var tela = Desenhar<ClienteFormulario>();
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.StartsWith("Nome completo")).QuerySelector("input")!.Change("Helena Barros Lima");
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.StartsWith("CPF")).QuerySelector("input")!.Change("529.982.247-25");

        tela.Find("form#formulario-do-cliente").Submit();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("CPF já cadastrado."));
        tela.Markup.Should().MatchRegex("<input[^>]*value=\"Helena Barros Lima\"");
        Services.GetRequiredService<NavigationManager>().Uri.Should().NotEndWith("/clientes");
    }
}
```

(O `EditForm` com `FormName` pode pedir o estado de antifalsificação no bUnit: se pedir, registre na `TelaInterativa` um `AntiforgeryStateProvider` que devolve nulo, como o da 9C, e registre o desvio. Se o `NavigationManager` do bUnit começar em outro endereço, compare o destino pelo histórico dele.)

Em `PortalDoClienteWebTests` e `PaginasProtegidasTests`, a rota `/clientes/novo`.

Run: `dotnet build -m:1`
Expected: não compila (`ClienteFormulario` não existe). É o RED.

- [ ] **Step 2: A tela**

```razor
@page "/clientes/novo"
@page "/clientes/{Id:guid}/editar"
@using System.ComponentModel.DataAnnotations
@inject IDespachante Despachante
@inject NavigationManager Navegacao
@inject Recados Recados

<PageTitle>@Titulo</PageTitle>

<CabecalhoDeDetalhe Titulo="@Titulo" Subtitulo="Só o nome e o CPF ou CNPJ são obrigatórios." VoltarPara="@Voltar" Origem="@Origem" />

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
    <Aviso Tipo="TipoDeAviso.Erro">Cliente não encontrado.</Aviso>
}
else if (_erroDaConsulta is not null)
{
    <Aviso Tipo="TipoDeAviso.Erro">@_erroDaConsulta</Aviso>
}
else
{
    @if (_mensagem is not null)
    {
        <Aviso Tipo="TipoDeAviso.Erro">@_mensagem</Aviso>
    }

    <div class="aj-so-mobile">
        <div class="aj-segmentado aj-segmentado--recuado aj-segmentado--largo" role="group" aria-label="Tipo de cliente">@OpcoesDeTipo</div>
        <p class="aj-ajuda">Só o nome e o CPF ou CNPJ são obrigatórios.</p>
    </div>

    <EditForm Model="_form" OnValidSubmit="SalvarAsync" FormName="cliente" id="formulario-do-cliente" class="aj-lado-a-lado aj-lado-a-lado--formulario">
        <DataAnnotationsValidator />
        <section class="aj-painel aj-painel--principal aj-lado-a-lado__lista">
            <div class="aj-painel__cabecalho">
                <h2>Identificação e contato</h2>
                <div class="aj-desktop-tablet">
                    <div class="aj-segmentado" role="group" aria-label="Tipo de cliente">@OpcoesDeTipo</div>
                </div>
            </div>
            <div class="aj-grade-campos aj-grade-campos--tablet-folgada">
                @if (_form.Tipo == TipoCliente.PessoaFisica)
                {
                    <RotuloDeCampo Texto="Nome completo" Para="() => _form.Nome" Classe="aj-grade-campos__inteiro"><InputText @bind-Value="_form.Nome" class="aj-campo" /></RotuloDeCampo>
                    <RotuloDeCampo Texto="CPF" Para="() => _form.Documento"><InputText @bind-Value="_form.Documento" inputmode="numeric" placeholder="000.000.000-00" class="aj-campo" /></RotuloDeCampo>
                    <label class="aj-rotulo">RG<InputText @bind-Value="_form.Rg" class="aj-campo" /></label>
                    <label class="aj-rotulo">Data de nascimento<InputDate @bind-Value="_form.DataNascimento" class="aj-campo" /></label>
                }
                else
                {
                    <RotuloDeCampo Texto="Razão social" Para="() => _form.Nome" Classe="aj-grade-campos__inteiro"><InputText @bind-Value="_form.Nome" class="aj-campo" /></RotuloDeCampo>
                    <RotuloDeCampo Texto="CNPJ" Para="() => _form.Documento"><InputText @bind-Value="_form.Documento" inputmode="numeric" placeholder="00.000.000/0000-00" class="aj-campo" /></RotuloDeCampo>
                    <label class="aj-rotulo">Nome fantasia<InputText @bind-Value="_form.NomeFantasia" class="aj-campo" /></label>
                    <label class="aj-rotulo">Responsável<InputText @bind-Value="_form.NomeResponsavel" class="aj-campo" /></label>
                    <label class="aj-rotulo">Inscrição estadual<InputText @bind-Value="_form.InscricaoEstadual" class="aj-campo" /></label>
                    <label class="aj-rotulo">Inscrição municipal<InputText @bind-Value="_form.InscricaoMunicipal" class="aj-campo" /></label>
                }
                <label class="aj-rotulo">E-mail<InputText @bind-Value="_form.Email" type="email" class="aj-campo" /></label>
                <label class="aj-rotulo">Telefone<InputText @bind-Value="_form.Telefone" type="tel" class="aj-campo" /></label>
            </div>
        </section>

        <div class="aj-lado-a-lado__painel aj-pilha">
            <section class="aj-painel">
                <h2>Endereço</h2>
                <div class="aj-grade-endereco">
                    <label class="aj-rotulo aj-grade-endereco__logradouro">Logradouro<InputText @bind-Value="_form.Logradouro" class="aj-campo" /></label>
                    <label class="aj-rotulo aj-grade-endereco__numero">Número<InputText @bind-Value="_form.Numero" class="aj-campo" /></label>
                    <label class="aj-rotulo aj-grade-endereco__complemento">Complemento<InputText @bind-Value="_form.Complemento" class="aj-campo" /></label>
                    <label class="aj-rotulo aj-grade-endereco__bairro">Bairro<InputText @bind-Value="_form.Bairro" class="aj-campo" /></label>
                    <label class="aj-rotulo aj-grade-endereco__cidade">Cidade<InputText @bind-Value="_form.Cidade" class="aj-campo" /></label>
                    <label class="aj-rotulo aj-grade-endereco__uf">UF<InputText @bind-Value="_form.Uf" maxlength="2" class="aj-campo aj-campo--curto" /></label>
                    <label class="aj-rotulo aj-grade-endereco__cep">CEP<InputText @bind-Value="_form.Cep" inputmode="numeric" class="aj-campo" /></label>
                </div>
            </section>
            <section class="aj-painel">
                <h2>Observações</h2>
                <label class="aj-rotulo"><span class="aj-so-leitor">Observações</span><InputTextArea @bind-Value="_form.Observacoes" rows="4" class="aj-campo" /></label>
            </section>
        </div>
    </EditForm>

    @* As barras de ação ficam fora do formulário e o enviam pelo atributo form. *@
    <div class="aj-so-desktop">
        <div class="aj-acoes aj-acoes--fixa"><a href="@Voltar" class="aj-botao aj-botao--fantasma">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" form="formulario-do-cliente" disabled="@_salvando"><Icone Nome="confirmar" />Salvar cliente</button></div>
    </div>
    <div class="aj-so-tablet">
        <div class="aj-acoes aj-acoes--fixa"><a href="@Voltar" class="aj-botao aj-botao--secundario">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" form="formulario-do-cliente" disabled="@_salvando"><Icone Nome="confirmar" />Salvar cliente</button></div>
    </div>
    <div class="aj-so-mobile">
        <div class="aj-acoes aj-acoes--fixa"><a href="@Voltar" class="aj-botao aj-botao--secundario">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" form="formulario-do-cliente" disabled="@_salvando"><Icone Nome="confirmar" />Salvar</button></div>
    </div>
}
```

No `@code`:

- `[Parameter] public Guid? Id { get; set; }`; `Titulo => Id is null ? "Novo cliente" : "Editar cliente"`; `Voltar => Id is null ? "/clientes" : $"/clientes/{Id}"`; `Origem => Id is null ? "Clientes" : "Cliente"`.
- `OpcoesDeTipo` (`RenderFragment`), os dois botões colados:

```razor
    private RenderFragment OpcoesDeTipo => @<text><button type="button" class="aj-segmentado__opcao" aria-pressed="@Marcado(TipoCliente.PessoaFisica)" disabled="@TipoTravado(TipoCliente.PessoaFisica)" @onclick="() => _form.Tipo = TipoCliente.PessoaFisica">Pessoa física</button><button type="button" class="aj-segmentado__opcao" aria-pressed="@Marcado(TipoCliente.PessoaJuridica)" disabled="@TipoTravado(TipoCliente.PessoaJuridica)" @onclick="() => _form.Tipo = TipoCliente.PessoaJuridica">Pessoa jurídica</button></text>;

    private string Marcado(TipoCliente tipo) => _form.Tipo == tipo ? "true" : "false";

    // Na edição o tipo não troca: o backend não muda um cliente de pessoa física para jurídica.
    private bool TipoTravado(TipoCliente tipo) => Id is not null && _form.Tipo != tipo;
```
- `OnInitializedAsync`: perfil antes de tudo (`_autorizado = ObterMeuAcesso is { EhDiretoria: true }`); com `Id`, `ObterCliente(Id)` preenche o `Formulario` como o `EditarAsync` de antes (`NaoEncontrado` → `_naoEncontrado`; acesso negado → `_autorizado = false`; outra falha → `_erroDaConsulta`).
- `SalvarAsync`: o de antes, com `_salvando` como guarda; sucesso: `Recados.Deixar("Cliente salvo.")` e `Navegacao.NavigateTo(Voltar)`; falha: `_mensagem`; acesso negado: `_autorizado = false`.
- O `Formulario` de antes (com `Tipo` começando em `PessoaFisica`).

- [ ] **Step 3: Trava e testes**

Acrescente a `Migradas`: `Components/Pages/ClienteFormulario.razor`.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ClienteFormulario|FullyQualifiedName~ClienteFicha|FullyQualifiedName~ClientesWebTests|FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~PaginasProtegidasTests"`
Expected: 0 avisos; todos passando.

- [ ] **Step 4: Commit**

```
git add src tests
git commit -m "feat(web): formulário de cliente em tela própria, com o tipo em controle segmentado e o endereço em grade"
```

---

### Task 10: Lista de processos

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Processos.razor` (reescrita: só a lista)
- Modify: `tests/AtividadesJuridicas.IntegrationTests/ProcessosWebTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs` (sai `/processos` da theory das telas ainda não migradas)
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: `PaginaDeProcessos` (Tarefa 3), `RotulosDeProcesso`, `CabecalhoDePagina` (com `LinhaDoMobile`), `EstadoDaLista`, `Paginacao`, `Aviso`, `Recados`, `Icone`.
- Produces: a rota `/processos` com a lista; os links `/processos/novo` e `/processos/{id}`, que as Tarefas 11 e 12 atendem.

**Regras:**
- Quadros: `Processos.dc.html`, `ProcessosTablet.dc.html`, `ProcessosMobile.dc.html`.
- Sai da lista o que era formulário e ficha (o `Formulario`, a busca de cliente, as ações de situação, "Editar", "Ver"). A lógica de busca e paginação fica: `ListarProcessos(texto, null, situação, página)`, a volta à última página, e "acesso negado" encerrando a tela.
- Filtros: no desktop, a barra (`form.aj-barra-filtros`) com a busca (rótulo `Busca`), o campo `Situação` (`label.aj-campo-filtro` com `select.aj-campo-filtro__controle`: `Todas`, `Ativo`, `Suspenso`, `Arquivado`, `Encerrado`) e "Buscar"; a situação escolhida só vale ao buscar. No tablet, `form.aj-linha-filtros` com a busca. No mobile, a busca na segunda linha da barra superior, que envia o formulário escondido `busca-de-processos`. No tablet e no mobile, os chips de situação (`Todas · {n}`, `Ativos`, `Suspensos`, `Arquivados`, `Encerrados`), que escolhem e buscam na hora; o chip e o campo do desktop mostram a mesma situação.
- Subtítulo (também na barra do mobile): `Contagem.De(TodasAsSituacoes, "processo", "processos")`, só com a lista carregada, sem erro e com `TodasAsSituacoes` maior que zero. O chip `Todas` usa o mesmo número (sem ele ainda, só `Todas`).
- Tabela (desktop): `Título` principal; `Número` em `aj-tabela__data`, ou `Sem número` em `aj-tabela__secundaria`; `Área`; `Clientes` (os nomes unidos por ", ", ou `—`); `Parte contrária` em `aj-tabela__secundaria` (ou `—`); `Situação`, com `aj-tabela__apagada` quando não é Ativo; "Abrir" para `/processos/{id}`. Paginação sempre à vista no desktop e no tablet; no mobile, só com mais de uma página.
- Cartões (tablet e mobile): link para a ficha; título; meta com algarismos tabulares (`aj-cartao__meta aj-cartao__meta--tabular`): o número (ou `Sem número`), `<br />`, `{área} · {clientes}` (os nomes unidos por ", ", ou `Sem cliente`); rodapé com `aj-cartao__resumo` (a parte contrária ou `Sem parte contrária`) e `aj-cartao__situacao` (com `aj-cartao__situacao--apagada` quando não é Ativo).
- A ação "Novo processo" só para a Diretoria. Recado de outra tela, uma vez, no topo. Falha da consulta: o erro com "Tentar de novo", sem subtítulo; vazio: `Nenhum processo.`.

- [ ] **Step 1: Testes (devem falhar)**

`ProcessosWebTests` (as asserções de "Editar" e "Ver" saem com a marcação; a prova de que a Produção não tem ação de cadastro ganha a forma nova):

```csharp
    [Fact]
    public async Task Diretor_ve_a_lista_com_a_contagem_os_filtros_e_a_acao_de_cadastro()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/processos"));

        html.Should().Contain("<p class=\"aj-subtitulo\">1 processo</p>")
            .And.MatchRegex("<a href=\"/processos/novo\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\">");
        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex("<form class=\"aj-barra-filtros\" role=\"search\"")
            .And.Contain("placeholder=\"Título, parte contrária ou número CNJ completo\"")
            .And.MatchRegex("(?s)<label class=\"aj-campo-filtro\">\\s*<span>Situação</span>\\s*<select class=\"aj-campo-filtro__controle\"[^>]*>.*?Todas</option>.*?Encerrado</option>")
            .And.MatchRegex("(?s)<td class=\"aj-tabela__principal\">Reclamação trabalhista</td>\\s*<td class=\"aj-tabela__data\">1234567-85.2024.8.02.0001</td>\\s*<td>Trabalhista</td>\\s*<td>Maria Silva</td>\\s*<td class=\"aj-tabela__secundaria\">Empresa X</td>\\s*<td>Ativo</td>")
            .And.MatchRegex("<a href=\"/processos/[0-9a-f-]{36}\" class=\"aj-botao aj-botao--fantasma aj-botao--seta\">")
            .And.Contain("Página 1 de 1 · 1 processo");
        var cartoes = BlocoDeVisao.De(html, "aj-tablet-mobile");
        cartoes.Should().MatchRegex("(?s)<div class=\"aj-chips\" role=\"group\" aria-label=\"Filtrar por situação\">\\s*<button type=\"button\" class=\"aj-chip\" aria-pressed=\"true\"[^>]*>Todas · 1</button>\\s*<button type=\"button\" class=\"aj-chip\" aria-pressed=\"false\"[^>]*>Ativos</button>")
            .And.MatchRegex("(?s)<a href=\"/processos/[0-9a-f-]{36}\" class=\"aj-cartao\">\\s*<span class=\"aj-cartao__titulo\">Reclamação trabalhista</span>\\s*<span class=\"aj-cartao__meta aj-cartao__meta--tabular\">1234567-85.2024.8.02.0001<br\\s*/?>Trabalhista · Maria Silva</span>\\s*<span class=\"aj-cartao__rodape\">\\s*<span class=\"aj-cartao__resumo\">Empresa X</span>\\s*<span class=\"aj-cartao__situacao\">Ativo</span>");
        html.Should().MatchRegex("<input type=\"search\" class=\"aj-campo-icone__controle\" form=\"busca-de-processos\"");
    }

    [Fact]
    public async Task Producao_ve_a_lista_e_abre_a_ficha_mas_nao_tem_acao_de_cadastro()
    {
        await _app.CriarPedroAsync();
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/processos"));

        html.Should().Contain("Reclamação trabalhista").And.MatchRegex("<a href=\"/processos/[0-9a-f-]{36}\" class=\"aj-botao aj-botao--fantasma aj-botao--seta\">");
        html.Should().NotContain("Novo processo").And.NotContain("/processos/novo").And.NotContain("Seu acesso foi encerrado");
    }

    [Fact]
    public async Task Processo_sem_numero_suspenso_e_sem_parte_contraria_aparece_como_no_quadro()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var area = (await _app.ExecutarAsync(admin, new ListarAreasAtuacao())).Valor!.Single();
        var processo = await _app.ExecutarAsync(admin, new SalvarProcesso(
            null, new DadosDoProcessoEntrada(null, "Figueiredo — inventário", area.Id, null, null, null, null, null, null), []));
        (await _app.ExecutarAsync(admin, new AlterarStatusProcesso(processo.Valor, StatusProcesso.Suspenso))).Sucesso.Should().BeTrue();

        var html = WebUtility.HtmlDecode(await (await _app.ClienteAutenticadoAsync()).GetStringAsync("/processos"));

        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            "(?s)<td class=\"aj-tabela__principal\">Figueiredo — inventário</td>\\s*<td class=\"aj-tabela__secundaria\">Sem número</td>\\s*<td>Trabalhista</td>\\s*<td>—</td>\\s*<td class=\"aj-tabela__secundaria\">—</td>\\s*<td class=\"aj-tabela__apagada\">Suspenso</td>");
        BlocoDeVisao.De(html, "aj-tablet-mobile").Should()
            .Contain("Sem número<br").And.Contain("Trabalhista · Sem cliente")
            .And.Contain("<span class=\"aj-cartao__resumo\">Sem parte contrária</span>")
            .And.Contain("<span class=\"aj-cartao__situacao aj-cartao__situacao--apagada\">Suspenso</span>");
        html.Should().Contain("2 processos").And.Contain("Todas · 2");
    }

    [Fact]
    public async Task Falha_da_consulta_mostra_o_erro_sem_subtitulo_e_nao_a_lista_vazia()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var despachante = new DespachanteCombinado()
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(await _app.ExecutarAsync(admin, new ObterMeuAcesso()))
            .Responde<ListarProcessos, Resultado<PaginaDeProcessos>>(Resultado<PaginaDeProcessos>.Falha(TipoErro.Validacao, "Falha de teste."));

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarComAsync<Processos>(despachante, new()));

        html.Should().Contain("Falha de teste.").And.Contain(">Tentar de novo</button>");
        html.Should().NotContain("Nenhum processo.").And.NotContain("Todas ·").And.NotContain("Página 1");
    }
```

(Se o domínio recusar processo sem cliente, o teste de "Sem cliente" passa a criar o processo com a Maria e prova o "Sem cliente" pelo `DespachanteCombinado`, com uma `PaginaDeProcessos` de um processo sem clientes; registre.)

`ProcessosInterativosTests.cs` (bUnit):

```csharp
public class ProcessosInterativosTests : TelaInterativa
{
    [Fact]
    public void Tocar_num_chip_busca_a_situacao_e_marca_o_chip()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarProcessos, Resultado<PaginaDeProcessos>>(pedido =>
                Resultado<PaginaDeProcessos>.Ok(new([], 0, 1, 20, 5)));
        var tela = Desenhar<Processos>();

        tela.FindAll(".aj-chips button").Single(b => b.TextContent == "Suspensos").Click();

        Despachante.Recebidas.OfType<ListarProcessos>().Last().Status.Should().Be(StatusProcesso.Suspenso);
        tela.Find(".aj-chips button[aria-pressed=true]").TextContent.Should().Be("Suspensos");
        tela.Find(".aj-chips button").TextContent.Should().Be("Todas · 5");
    }
}
```

(Com os `using` que faltarem.) Em `LayoutWebTests`, tire `/processos` da theory das telas ainda não migradas.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ProcessosWebTests|FullyQualifiedName~ProcessosInterativosTests"`
Expected: os testes novos falhando. É o RED.

- [ ] **Step 2: A tela**

A mesma estrutura de `Clientes.razor` (Tarefa 6), com estas diferenças:

```razor
<CabecalhoDePagina Titulo="Processos" Subtitulo="@Subtitulo" Acao="@(_ator?.EhDiretoria == true ? "Novo processo" : null)" AcaoHref="/processos/novo">
    <LinhaDoMobile>
        <label class="aj-campo-icone"><Icone Nome="buscar" /><span class="aj-so-leitor">Busca</span><input type="search" class="aj-campo-icone__controle" form="busca-de-processos" placeholder="Título, parte contrária ou número CNJ completo" @bind="_texto" /></label>
    </LinhaDoMobile>
</CabecalhoDePagina>
```

```razor
    <div class="aj-so-desktop">
        <form class="aj-barra-filtros" role="search" @onsubmit="BuscarAsync" @onsubmit:preventDefault>
            <label class="aj-campo-icone"><Icone Nome="buscar" /><span class="aj-so-leitor">Busca</span><input type="search" class="aj-campo-icone__controle" placeholder="Título, parte contrária ou número CNJ completo" @bind="_texto" /></label>
            <label class="aj-campo-filtro"><span>Situação</span><select class="aj-campo-filtro__controle" @bind="_situacao"><option value="">Todas</option>@foreach (var situacao in Enum.GetValues<StatusProcesso>())
                {<option value="@situacao">@RotulosDeProcesso.Situacao(situacao)</option>}</select></label>
            <button type="submit" class="aj-botao aj-botao--secundario">Buscar</button>
        </form>
    </div>
    <div class="aj-so-tablet">
        <form class="aj-linha-filtros" role="search" @onsubmit="BuscarAsync" @onsubmit:preventDefault>
            <label class="aj-campo-icone"><Icone Nome="buscar" /><span class="aj-so-leitor">Busca</span><input type="search" class="aj-campo-icone__controle" placeholder="Título, parte contrária ou número CNJ completo" @bind="_texto" /></label>
        </form>
    </div>
    <div class="aj-tablet-mobile">
        <div class="aj-chips" role="group" aria-label="Filtrar por situação">
            <button type="button" class="aj-chip" aria-pressed="@Pressionado(null)" @onclick="() => EscolherSituacaoAsync(null)">@RotuloDeTodas</button>@foreach (var situacao in Enum.GetValues<StatusProcesso>())
            {<button type="button" class="aj-chip" aria-pressed="@Pressionado(situacao)" @key="situacao" @onclick="() => EscolherSituacaoAsync(situacao)">@RotulosDeProcesso.SituacaoNoPlural(situacao)</button>}
        </div>
    </div>
```

(Os chips colados, como as outras listas de chips; se o `@foreach` dentro do `div` deixar espaço entre eles, tanto faz: o `gap` do `aj-chips` é o que afasta.)

Tabela e cartões:

```razor
<tr @key="processo.Id">
    <td class="aj-tabela__principal">@processo.Titulo</td>
    <td class="@(processo.NumeroCnj is null ? "aj-tabela__secundaria" : "aj-tabela__data")">@RotulosDeProcesso.Numero(processo.NumeroCnj)</td>
    <td>@processo.AreaNome</td>
    <td>@(processo.Clientes.Count == 0 ? "—" : string.Join(", ", processo.Clientes))</td>
    <td class="aj-tabela__secundaria">@Ou(processo.ParteContraria)</td>
    <td class="@(processo.Status == StatusProcesso.Ativo ? null : "aj-tabela__apagada")">@RotulosDeProcesso.Situacao(processo.Status)</td>
    <td class="aj-tabela__acao"><a href="@Ficha(processo)" class="aj-botao aj-botao--fantasma aj-botao--seta">Abrir<Icone Nome="avancar" /></a></td>
</tr>
```

```razor
<a href="@Ficha(processo)" class="aj-cartao" @key="processo.Id">
    <span class="aj-cartao__titulo">@processo.Titulo</span>
    <span class="aj-cartao__meta aj-cartao__meta--tabular">@RotulosDeProcesso.Numero(processo.NumeroCnj)<br />@($"{processo.AreaNome} · {(processo.Clientes.Count == 0 ? "Sem cliente" : string.Join(", ", processo.Clientes))}")</span>
    <span class="aj-cartao__rodape"><span class="aj-cartao__resumo">@(processo.ParteContraria ?? "Sem parte contrária")</span><span class="@(processo.Status == StatusProcesso.Ativo ? "aj-cartao__situacao" : "aj-cartao__situacao aj-cartao__situacao--apagada")">@RotulosDeProcesso.Situacao(processo.Status)</span></span>
</a>
```

No `@code`: `_situacao` (`StatusProcesso?`); `EscolherSituacaoAsync(StatusProcesso? s)`: `_situacao = s` e busca a página 1; `Pressionado(s) => _situacao == s ? "true" : "false"`; `RotuloDeTodas => _pagina is PaginaDeProcessos { TodasAsSituacoes: > 0 } p && _erro is null ? $"Todas · {p.TodasAsSituacoes}" : "Todas"`; o formulário escondido `busca-de-processos` no fim; `Subtitulo` como nas Regras; os auxiliares como os de `Clientes.razor`. (A parte contrária vazia vem nula ou em branco: trate as duas como ausente.)

- [ ] **Step 3: Trava e testes**

Acrescente a `Migradas`: `Components/Pages/Processos.razor`.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ProcessosWebTests|FullyQualifiedName~ProcessosInterativosTests|FullyQualifiedName~LayoutWebTests|FullyQualifiedName~PortalDoClienteWebTests"`
Expected: 0 avisos; todos passando.

- [ ] **Step 4: Commit**

```
git add src tests
git commit -m "feat(web): lista de processos no design system, com chips de situação, a contagem e os cartões"
```

---

### Task 11: Ficha do processo

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Pages/ProcessoFicha.razor`
- Create: `tests/AtividadesJuridicas.IntegrationTests/ProcessoFichaWebTests.cs`
- Create: `tests/AtividadesJuridicas.IntegrationTests/ProcessoFichaInterativaTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PortalDoClienteWebTests.cs`, `PaginasProtegidasTests.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: `CabecalhoDeFicha`, `Abas`/`Aba`, `Aviso`, `Recados`, `Icone`, `RotulosDeProcesso`, `TextosDoCliente.Documento`; `ObterMeuAcesso`, `ObterProcesso` (`ProcessoDetalhe` com `ClienteDoProcessoDetalhe`), `AlterarStatusProcesso`, que não mudam.
- Produces: a rota `/processos/{Id:guid}`; o link `/processos/{id}/editar`, que a Tarefa 12 atende.

**Regras:**
- Quadros: `Processo.dc.html`, `ProcessoTablet.dc.html`, `ProcessoMobile.dc.html`.
- Cabeçalho (`CabecalhoDeFicha`, `TituloDaBarra="Processo"`, voltar `/processos` "Processos"): subtítulo `{número|Sem número} · {área} · {situação}`; resumo do mobile `{número|Sem número} · {situação}`; ação do desktop, só para a Diretoria: `Editar` (secundário, link).
- Carregamento: o perfil, depois `ObterProcesso`. `NaoEncontrado`: `Processo não encontrado.` com o cabeçalho de voltar, sem painel. Acesso negado: encerra a tela. Outra falha: o erro com "Tentar de novo".
- Desktop (`aj-so-desktop`): `div.aj-lado-a-lado.aj-lado-a-lado--ficha` com `div.aj-lado-a-lado__lista.aj-pilha` (Clientes do processo, Observações) e `div.aj-lado-a-lado__painel.aj-pilha` (Dados, Situação).
- Tablet e mobile (`aj-tablet-mobile`): as abas (`aj-so-mobile` > `Abas`, `Rotulo="Assuntos do processo"`: `Dados`, `Clientes {n}` e, só para a Diretoria, `Situação`; começa em `dados`) e os painéis, cada um no seu invólucro de aba, na ordem do tablet: Dados (aba `dados`), Clientes do processo (`clientes`), Observações (`dados`), Situação (`situacao`). Observações só com texto.
- Clientes do processo: no desktop, tabela em caixa plana (`Cliente` principal, `Documento` em `aj-tabela__data`, `Polo`, "Abrir" para `/clientes/{id}`); no tablet e no mobile, `div.aj-linhas.aj-linhas--folgadas` com uma `a.aj-linha.aj-linha--link` por cliente (nome em `aj-linha__titulo`; `{CPF|CNPJ} {documento} · polo {polo em minúsculas}` em `aj-ajuda`; o ícone `avancar`). O tipo do cliente (para "CPF" ou "CNPJ") não vem no `ClienteDoProcessoDetalhe`: deduza pelo documento formatado (o CNPJ tem "/") num auxiliar da tela, e registre.
- Dados: `Número` (ou `Sem número`), `Área`, `Órgão`, `Comarca`, `Parte contrária`, `Distribuição` (`dd/MM/yyyy`), `Valor da causa` (`R$ 84.000,00`, cultura pt-BR); `—` no vazio.
- Situação (só a Diretoria; desenhado por um `RenderFragment` usado nas duas posições): `span.aj-ajuda` com `Situação atual: ` e `<strong>{situação}</strong>`; `form.aj-campo-com-botao` com `label.aj-rotulo` `Nova situação` (`select.aj-campo` com as outras situações, a primeira escolhida) e `Alterar` (secundário, `type="submit"`). Alterada com sucesso: a ficha passa à situação nova (sem recarregar), o aviso `Situação alterada para {situação}.`, e o seletor volta à primeira das outras. Falha: aviso de erro, e nada muda. Acesso negado: encerra. Guarda de clique duplo.
- Barras fixas, só para a Diretoria: no tablet (`aj-so-tablet`), `Editar` (primário, link); no mobile (`aj-so-mobile`), `Alterar situação` (secundário, abre a aba `situacao`) e `Editar` (primário).
- Produção: a mesma ficha sem `Editar`, sem o painel e a aba de situação, sem barras.
- Recado de outra tela (a edição), uma vez, no topo.

- [ ] **Step 1: Testes (devem falhar)**

`ProcessoFichaWebTests.cs`:

```csharp
using System.Net;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ProcessoFichaWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _maria, _processo;

    public async Task InitializeAsync()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var area = await _app.ExecutarAsync(admin, new SalvarAreaAtuacao(null, "Trabalhista"));
        var maria = await _app.ExecutarAsync(admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("Maria Silva", null, null, new EnderecoEntrada(), null), "529.982.247-25", null, null));
        _maria = maria.Valor;
        var criado = await _app.ExecutarAsync(admin, new SalvarProcesso(
            null,
            new DadosDoProcessoEntrada("1234567-85.2024.8.02.0001", "Reclamação trabalhista", area.Valor, "2ª Vara do Trabalho de Maceió",
                "Maceió", "Empresa X", new DateOnly(2026, 3, 12), 84000m, "Audiência de instrução realizada."),
            [new ClienteDoProcessoEntrada(_maria, PoloProcessual.Passivo)]));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        _processo = criado.Valor;
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> PaginaAsync(string rota, bool comoPedro = false)
    {
        var cliente = comoPedro
            ? await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro)
            : await _app.ClienteAutenticadoAsync();
        return WebUtility.HtmlDecode(await cliente.GetStringAsync(rota));
    }

    [Fact]
    public async Task Diretoria_ve_o_cabecalho_os_clientes_os_dados_e_a_situacao()
    {
        var html = await PaginaAsync($"/processos/{_processo}");

        html.Should().Contain("<h1 class=\"aj-titulo\">Reclamação trabalhista</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">1234567-85.2024.8.02.0001 · Trabalhista · Ativo</p>")
            .And.Contain("<span class=\"aj-ajuda\">1234567-85.2024.8.02.0001 · Ativo</span>")
            .And.Contain("<h1>Processo</h1>");
        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex("(?s)<div class=\"aj-lado-a-lado aj-lado-a-lado--ficha\">\\s*<div class=\"aj-lado-a-lado__lista aj-pilha\">\\s*<section class=\"aj-painel\">\\s*<h2>Clientes do processo</h2>")
            .And.MatchRegex($"(?s)<td class=\"aj-tabela__principal\">Maria Silva</td>\\s*<td class=\"aj-tabela__data\">529.982.247-25</td>\\s*<td>Passivo</td>\\s*<td class=\"aj-tabela__acao\"><a href=\"/clientes/{_maria}\"")
            .And.Contain("<div><dt>Valor da causa</dt><dd>R$ 84.000,00</dd></div>")
            .And.MatchRegex("(?s)<h2>Situação</h2>\\s*<span class=\"aj-ajuda\">Situação atual: <strong>Ativo</strong></span>\\s*<form class=\"aj-campo-com-botao\"")
            .And.MatchRegex("<a href=\"/processos/[0-9a-f-]{36}/editar\" class=\"aj-botao aj-botao--secundario\">Editar</a>");
        var tabletEMobile = BlocoDeVisao.De(html, "aj-tablet-mobile");
        tabletEMobile.Should().MatchRegex($"<a href=\"/clientes/{_maria}\" class=\"aj-linha aj-linha--link\">")
            .And.Contain("<span class=\"aj-ajuda\">CPF 529.982.247-25 · polo passivo</span>");
        BlocoDeVisao.De(html, "aj-so-mobile").Should()
            .MatchRegex("(?s)aria-label=\"Assuntos do processo\".*?>Dados</button>.*?>Clientes<span class=\"aj-abas__contador\">1</span></button>.*?>Situação</button>")
            .And.MatchRegex("(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<button type=\"button\" class=\"aj-botao aj-botao--secundario\"[^>]*>Alterar situação</button>");
    }

    [Fact]
    public async Task Producao_ve_a_ficha_sem_editar_nem_situacao()
    {
        await _app.CriarPedroAsync();

        var html = await PaginaAsync($"/processos/{_processo}", comoPedro: true);

        html.Should().Contain("<h2>Clientes do processo</h2>").And.Contain("<h2>Dados</h2>");
        html.Should().NotContain("/editar").And.NotContain("<h2>Situação</h2>").And.NotContain("Alterar situação")
            .And.NotContain("aj-acoes--fixa").And.NotContain("Seu acesso foi encerrado");
    }

    [Fact]
    public async Task Processo_que_nao_existe_mostra_nao_encontrado_com_o_voltar_e_nada_mais()
    {
        var html = await PaginaAsync($"/processos/{Guid.NewGuid()}");

        html.Should().Contain("Processo não encontrado.").And.Contain("aria-label=\"Voltar para Processos\"");
        html.Should().NotContain("<h2>Dados</h2>").And.NotContain("aj-abas").And.NotContain("/editar");
    }
}
```

(O valor da causa sai com o espaço que a cultura pt-BR põe depois de "R$": se for o espaço fixo, `HtmlDecode` o mantém; compare com o que `84000m.ToString("C", pt-BR)` produz, sem mudar o que o teste prova.)

`ProcessoFichaInterativaTests.cs` (bUnit):

```csharp
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Web.Components.Pages;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

public class ProcessoFichaInterativaTests : TelaInterativa
{
    private static readonly Guid Id = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000f1");

    private static ProcessoDetalhe Processo(StatusProcesso status = StatusProcesso.Ativo) => new(
        Id, "0000000-00.2026.5.19.0002", "Litoral — reclamação trabalhista", Guid.NewGuid(), "Trabalhista", status,
        null, "Maceió", "Paulo Mendes", null, 84000m, null,
        [new ClienteDoProcessoDetalhe(Guid.NewGuid(), "Transportes Litoral Ltda.", "11.222.333/0001-81", PoloProcessual.Passivo, true)]);

    private IRenderedComponent<ProcessoFicha> Ficha()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ObterProcesso, Resultado<ProcessoDetalhe>>(Resultado<ProcessoDetalhe>.Ok(Processo()))
            .Responde<AlterarStatusProcesso, Resultado>(Resultado.Ok());
        return Desenhar<ProcessoFicha>(p => p.Add(f => f.Id, Id));
    }

    [Fact]
    public void Alterar_a_situacao_muda_a_ficha_e_avisa()
    {
        var ficha = Ficha();
        // O desktop vem antes na marcação: o primeiro formulário de situação é o dele.
        var formulario = ficha.FindAll("form.aj-campo-com-botao")[0];

        formulario.QuerySelector("select")!.Change(nameof(StatusProcesso.Encerrado));
        formulario.Submit();

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Situação alterada para Encerrado."));
        Despachante.Recebidas.OfType<AlterarStatusProcesso>().Should().ContainSingle()
            .Which.Should().Be(new AlterarStatusProcesso(Id, StatusProcesso.Encerrado));
        ficha.Markup.Should().Contain("Situação atual: <strong>Encerrado</strong>").And.Contain("· Trabalhista · Encerrado</p>");
        ficha.FindAll("form.aj-campo-com-botao")[0].QuerySelectorAll("option").Select(o => o.TextContent).Should()
            .Equal("Ativo", "Suspenso", "Arquivado");
    }

    [Fact]
    public void Alterar_situacao_na_barra_do_mobile_abre_a_aba_situacao()
    {
        var ficha = Ficha();

        ficha.FindAll(".aj-acoes--fixa button").Single(b => b.TextContent == "Alterar situação").Click();

        ficha.Find(".aj-abas button[aria-selected=true]").TextContent.Should().Be("Situação");
        ficha.Markup.Should().MatchRegex("<div class=\"aj-aba-painel\">\\s*<section class=\"aj-painel\">\\s*<h2>Situação</h2>");
    }
}
```

(`Resultado.Ok()` como existe no projeto.)

Em `PortalDoClienteWebTests` e `PaginasProtegidasTests`, a rota `/processos/0199a3f2-7c1e-7000-8000-000000000001`.

Run: `dotnet build -m:1`
Expected: não compila (`ProcessoFicha` não existe). É o RED.

- [ ] **Step 2: A tela**

```razor
@page "/processos/{Id:guid}"
@inject IDespachante Despachante
@inject Recados Recados

<PageTitle>@(_processo?.Titulo ?? "Processo")</PageTitle>

@if (!_carregado || _ator is null || _processo is null)
{
    <CabecalhoDeDetalhe Titulo="Processo" VoltarPara="/processos" Origem="Processos" />
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
        <Aviso Tipo="TipoDeAviso.Erro">Processo não encontrado.</Aviso>
    }
    else
    {
        <EstadoDaLista Erro="@(_erro ?? "Não foi possível consultar o processo.")" TextoDeVazio="" TentarDeNovo="CarregarAsync" />
    }
}
else
{
    var p = _processo;
    <CabecalhoDeFicha Titulo="@p.Titulo" Subtitulo="@Subtitulo" Resumo="@Resumo" TituloDaBarra="Processo" VoltarPara="/processos" Origem="Processos"
                      Acoes="@(Diretoria ? EditarDoDesktop : null)" />

    @if (_recado is { } recado)
    {
        <Aviso Tipo="@recado.Tipo">@recado.Texto</Aviso>
    }
    @if (_mensagem is not null)
    {
        <Aviso Tipo="@(_falhou ? TipoDeAviso.Erro : TipoDeAviso.Sucesso)">@_mensagem</Aviso>
    }

    <div class="aj-so-desktop">
        <div class="aj-lado-a-lado aj-lado-a-lado--ficha">
            <div class="aj-lado-a-lado__lista aj-pilha">
                <section class="aj-painel">
                    <h2>Clientes do processo</h2>
                    <div class="aj-tabela-caixa aj-tabela-caixa--plana">
                        <table class="aj-tabela">
                            <thead><tr><th scope="col">Cliente</th><th scope="col">Documento</th><th scope="col">Polo</th><th scope="col"><span class="aj-so-leitor">Ações</span></th></tr></thead>
                            <tbody>
                                @foreach (var cliente in p.Clientes)
                                {
                                    <tr @key="cliente.ClienteId">
                                        <td class="aj-tabela__principal">@cliente.Nome</td>
                                        <td class="aj-tabela__data">@cliente.Documento</td>
                                        <td>@RotulosDeProcesso.Polo(cliente.Polo)</td>
                                        <td class="aj-tabela__acao"><a href="@($"/clientes/{cliente.ClienteId}")" class="aj-botao aj-botao--fantasma aj-botao--seta">Abrir<Icone Nome="avancar" /></a></td>
                                    </tr>
                                }
                            </tbody>
                        </table>
                    </div>
                </section>
                @Observacoes
            </div>
            <div class="aj-lado-a-lado__painel aj-pilha">
                @Dados
                @if (Diretoria)
                {
                    @Situacao
                }
            </div>
        </div>
    </div>

    <div class="aj-tablet-mobile">
        <div class="aj-so-mobile">
            <Abas Rotulo="Assuntos do processo" Itens="@AbasDaFicha" Ativa="@_aba" AoEscolher="(string aba) => _aba = aba" />
        </div>
        <div class="@ClasseDaAba("dados")">@Dados</div>
        <div class="@ClasseDaAba("clientes")">
            <section class="aj-painel">
                <h2>Clientes do processo</h2>
                <div class="aj-linhas aj-linhas--folgadas">
                    @foreach (var cliente in p.Clientes)
                    {
                        <a href="@($"/clientes/{cliente.ClienteId}")" class="aj-linha aj-linha--link" @key="cliente.ClienteId"><span class="aj-linha__texto"><span class="aj-linha__titulo">@cliente.Nome</span><span class="aj-ajuda">@DocumentoEPolo(cliente)</span></span><Icone Nome="avancar" Classe="aj-cartao__seta" /></a>
                    }
                </div>
            </section>
        </div>
        <div class="@ClasseDaAba("dados")">@Observacoes</div>
        @if (Diretoria)
        {
            <div class="@ClasseDaAba("situacao")">@Situacao</div>
        }
    </div>

    @if (Diretoria)
    {
        <div class="aj-so-tablet">
            <div class="aj-acoes aj-acoes--fixa"><a href="@Edicao" class="aj-botao aj-botao--primario">Editar</a></div>
        </div>
        <div class="aj-so-mobile">
            <div class="aj-acoes aj-acoes--fixa"><button type="button" class="aj-botao aj-botao--secundario" @onclick="() => _aba = \"situacao\"">Alterar situação</button><a href="@Edicao" class="aj-botao aj-botao--primario">Editar</a></div>
        </div>
    }
}
```

`Dados`, `Observacoes` (vazio quando não há texto) e `Situacao` são `RenderFragment`s no `@code`:

```razor
    private RenderFragment Dados => @<section class="aj-painel">
        <h2>Dados</h2>
        <dl class="aj-detalhes">
            @foreach (var (termo, valor) in Detalhes)
            {
                <div><dt>@termo</dt><dd>@valor</dd></div>
            }
        </dl>
    </section>;

    private RenderFragment Situacao => @<section class="aj-painel">
        <h2>Situação</h2>
        <span class="aj-ajuda">Situação atual: <strong>@RotulosDeProcesso.Situacao(_processo!.Status)</strong></span>
        <form class="aj-campo-com-botao" @onsubmit="AlterarSituacaoAsync" @onsubmit:preventDefault>
            <label class="aj-rotulo">Nova situação<select class="aj-campo" @bind="_novaSituacao">@foreach (var situacao in OutrasSituacoes)
                {<option value="@situacao">@RotulosDeProcesso.Situacao(situacao)</option>}</select></label>
            <button type="submit" class="aj-botao aj-botao--secundario" disabled="@_alterando">Alterar</button>
        </form>
    </section>;
```

(Se o `@onclick` com aspas escapadas não compilar, use um método `AbrirSituacao()`.) Os auxiliares: `Detalhes` (os pares de Dados, na ordem do quadro, com `—`; o valor com `ToString("C", pt-BR)` e a data com `dd/MM/yyyy`); `AbasDaFicha`; `ClasseDaAba`; `OutrasSituacoes` (as situações menos a atual); `_novaSituacao` volta à primeira das outras a cada mudança da situação atual; `DocumentoEPolo(c) => $"{(c.Documento.Contains('/') ? "CNPJ" : "CPF")} {c.Documento} · polo {RotulosDeProcesso.Polo(c.Polo).ToLowerInvariant()}"`; `AlterarSituacaoAsync` como nas Regras (com `_processo = _processo with { Status = _novaSituacao }`).

O resto do `@code`:

- `[Parameter] public Guid Id { get; set; }`.
- Estado: `_carregado`, `_ator`, `_processo` (`ProcessoDetalhe?`), `_naoEncontrado`, `_erro`, `_aba = "dados"`, `_novaSituacao` (`StatusProcesso`), `_alterando`, `_mensagem`, `_falhou`, `_recado`.
- `Diretoria => _ator?.EhDiretoria == true`; `Edicao => $"/processos/{Id}/editar"`; `EditarDoDesktop` (`RenderFragment`): `<a href="@Edicao" class="aj-botao aj-botao--secundario">Editar</a>`.
- `Subtitulo => $"{RotulosDeProcesso.Numero(p.NumeroCnj)} · {p.AreaNome} · {RotulosDeProcesso.Situacao(p.Status)}"`; `Resumo => $"{RotulosDeProcesso.Numero(p.NumeroCnj)} · {RotulosDeProcesso.Situacao(p.Status)}"`.
- `AbasDaFicha`: `new("dados", "Dados")`, `new("clientes", "Clientes", p.Clientes.Count)` e, para a Diretoria, `new("situacao", "Situação")`.
- `OnParametersSetAsync`: quando o `Id` muda, `CarregarAsync()` (perfil; `ObterProcesso`; `NaoEncontrado` → `_naoEncontrado`; acesso negado → `_ator = null`; outra falha → `_erro`) e, na primeira vez, `_recado = Recados.Retirar()`.

- [ ] **Step 3: Trava e testes**

Acrescente a `Migradas`: `Components/Pages/ProcessoFicha.razor`.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ProcessoFicha|FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~PaginasProtegidasTests"`
Expected: 0 avisos; todos passando.

- [ ] **Step 4: Commit**

```
git add src tests
git commit -m "feat(web): ficha do processo em tela própria, com clientes e polo, abas do mobile e a situação no painel"
```

---

### Task 12: Formulário de processo em tela própria

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Pages/ProcessoFormulario.razor`
- Create: `tests/AtividadesJuridicas.IntegrationTests/ProcessoFormularioWebTests.cs`
- Create: `tests/AtividadesJuridicas.IntegrationTests/ProcessoFormularioInterativoTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/PortalDoClienteWebTests.cs`, `PaginasProtegidasTests.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (lista `Migradas`)

**Interfaces:**
- Consumes: `CabecalhoDeDetalhe`, `RotuloDeCampo` (com `Classe`), `ValorEmReais` e `[ValorEmReaisValido]`, `TextosDoCliente.Documento`, `RotulosDeProcesso.Polo`, `Aviso`, `Recados`, `Icone` (`buscar`, `fechar`, `confirmar`); `ObterMeuAcesso`, `ObterProcesso`, `ListarAreasAtuacao`, `ListarClientes` (`PaginaDeClientes`), `SalvarProcesso`, que não mudam.
- Produces: as rotas `/processos/novo` e `/processos/{Id:guid}/editar`.

**Regras:**
- Quadros: `ProcessoFormulario.dc.html`, `ProcessoFormularioTablet.dc.html`, `ProcessoFormularioMobile.dc.html`.
- A lógica vem de `Processos.razor` de antes da Tarefa 10 (o `Formulario`, as áreas ativas mais a atual inativa marcada "(inativa)", a busca de cliente com os avisos de nenhum e de truncado, vincular no polo ativo, desvincular, o `SalvarAsync`), com três mudanças: a situação sai do formulário (vai para a ficha, Tarefa 11); o valor da causa é texto no formato brasileiro (`ValorEmReais`, erro no campo `Informe o valor como 84.000,00.`; na edição, o valor vem escrito por `ValorEmReais.Escrever`); a busca de cliente não tem botão e responde ao Enter, por um formulário escondido `busca-de-cliente` (padrão da 9C).
- Arranjo: o `EditForm` (`id="formulario-do-processo"`) é o `aj-lado-a-lado aj-lado-a-lado--formulario`: `section.aj-painel.aj-painel--principal.aj-lado-a-lado__lista` (`Processo`) e `section.aj-painel.aj-lado-a-lado__painel` (`Clientes do processo`).
- Painel Processo: `div.aj-grade-campos` com `Título` (inteiro, `RotuloDeCampo`), `Número CNJ` (exemplo `0000000-00.0000.0.00.0000`, numérico), `Área de atuação` (`select.aj-campo` com `Selecione…`), `Órgão`, `Comarca`, `Parte contrária`, `Data de distribuição` (`InputDate`), `Valor da causa` (`RotuloDeCampo`, `InputText` com `inputmode="decimal"` e exemplo `0,00`), `Observações` (inteiro, `textarea` de 3 linhas).
- Painel Clientes do processo: cada vínculo é `div.aj-vinculo__escolhido` com `span.aj-vinculo__nome`, `label.aj-campo-filtro.aj-campo-filtro--polo` (`span` `Polo` e `select.aj-campo-filtro__controle` com `Ativo`, `Passivo`, `Terceiro`) e o botão `aj-botao aj-botao--fantasma aj-botao--icone` com `aria-label="Remover {nome}"` e o ícone `fechar`; sem vínculo, `span.aj-ajuda` `Nenhum cliente vinculado.`. Depois, a busca (`label.aj-campo-icone` com `Icone buscar`, `span.aj-so-leitor` `Adicionar cliente (nome, CPF ou CNPJ)` e `input type=search class=aj-campo-icone__controle form=busca-de-cliente` com o exemplo `Adicionar cliente por nome, CPF ou CNPJ`); cada candidato é `div.aj-candidato` com `span.aj-candidato__texto` (`span.aj-candidato__titulo` com o nome e `span.aj-ajuda` com `TextosDoCliente.Documento`) e o botão secundário `Adicionar`; os avisos de nenhum e de truncado em `aj-ajuda`; e, por último, a ajuda `O cliente entra no polo ativo. Troque o polo ao lado do nome.`.
- Ações, fora do formulário, com `form="formulario-do-processo"`: desktop `aj-so-desktop` > `aj-acoes aj-acoes--fixa` com `Cancelar` (fantasma) e `Salvar processo` (primário com `confirmar`); tablet, `Cancelar` secundário e `Salvar processo`; mobile, `Cancelar` secundário e `Salvar`.
- Voltar: no novo, `/processos` ("Processos"); na edição, `/processos/{id}` ("Processo"). Salvar com sucesso: `Recados.Deixar("Processo salvo.")` e navega para o voltar. Falha: `Aviso` de erro no topo, com o que foi digitado. Edição de processo que não existe: `Processo não encontrado.`, sem formulário. Produção: `Acesso negado.`, sem formulário e sem consulta.

- [ ] **Step 1: Testes (devem falhar)**

`ProcessoFormularioWebTests.cs`:

```csharp
using System.Net;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Domain.Cadastros;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class ProcessoFormularioWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _processo;

    public async Task InitializeAsync()
    {
        var admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var area = await _app.ExecutarAsync(admin, new SalvarAreaAtuacao(null, "Trabalhista"));
        var maria = await _app.ExecutarAsync(admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("Maria Silva", null, null, new EnderecoEntrada(), null), "529.982.247-25", null, null));
        var criado = await _app.ExecutarAsync(admin, new SalvarProcesso(
            null,
            new DadosDoProcessoEntrada("1234567-85.2024.8.02.0001", "Reclamação trabalhista", area.Valor, null, "Maceió",
                "Empresa X", null, 84000m, null),
            [new ClienteDoProcessoEntrada(maria.Valor, PoloProcessual.Passivo)]));
        criado.Sucesso.Should().BeTrue(criado.Mensagem);
        _processo = criado.Valor;
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> PaginaAsync(string rota) =>
        WebUtility.HtmlDecode(await (await _app.ClienteAutenticadoAsync()).GetStringAsync(rota));

    [Fact]
    public async Task Novo_processo_traz_os_dois_paineis_a_busca_sem_botao_e_as_acoes()
    {
        var html = await PaginaAsync("/processos/novo");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Novo processo</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Preencha os dados e vincule os clientes do processo.</p>")
            .And.MatchRegex("<form[^>]*id=\"formulario-do-processo\"[^>]*class=\"aj-lado-a-lado aj-lado-a-lado--formulario\"")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-painel--principal aj-lado-a-lado__lista\">\\s*<h2>Processo</h2>")
            .And.Contain("placeholder=\"0000000-00.0000.0.00.0000\"").And.Contain(">Selecione…</option>")
            .And.MatchRegex("<input[^>]*inputmode=\"decimal\"[^>]*placeholder=\"0,00\"")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-lado-a-lado__painel\">\\s*<h2>Clientes do processo</h2>")
            .And.Contain("<span class=\"aj-ajuda\">Nenhum cliente vinculado.</span>")
            .And.MatchRegex("<input type=\"search\" class=\"aj-campo-icone__controle\" form=\"busca-de-cliente\" placeholder=\"Adicionar cliente por nome, CPF ou CNPJ\"")
            .And.Contain("O cliente entra no polo ativo. Troque o polo ao lado do nome.")
            .And.Contain("<form id=\"busca-de-cliente\" hidden");
        html.Should().NotContain(">Procurar</button>").And.NotContain("Situação atual");
        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            "(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a href=\"/processos\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>\\s*<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-do-processo\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Salvar processo</button>");
    }

    [Fact]
    public async Task Edicao_traz_o_cliente_vinculado_com_o_polo_e_o_valor_no_formato_brasileiro()
    {
        var html = await PaginaAsync($"/processos/{_processo}/editar");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Editar processo</h1>")
            .And.MatchRegex($"<a href=\"/processos/{_processo}\" class=\"aj-voltar\">")
            .And.MatchRegex("<input[^>]*value=\"84.000,00\"")
            .And.MatchRegex("(?s)<div class=\"aj-vinculo__escolhido\">\\s*<span class=\"aj-vinculo__nome\">Maria Silva</span>\\s*<label class=\"aj-campo-filtro aj-campo-filtro--polo\">\\s*<span>Polo</span>\\s*<select class=\"aj-campo-filtro__controle\"[^>]*>.*?<option value=\"Passivo\" selected")
            .And.Contain("aria-label=\"Remover Maria Silva\"");
        html.Should().NotContain("Nenhum cliente vinculado.");
    }

    [Fact]
    public async Task Producao_nao_abre_o_formulario()
    {
        await _app.CriarPedroAsync();
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await pedro.GetStringAsync("/processos/novo"));

        html.Should().Contain("Acesso negado.").And.MatchRegex("<h1 class=\"aj-titulo\">Novo processo</h1>");
        html.Should().NotContain("<h2>Processo</h2>").And.NotContain("Trabalhista");
    }
}
```

(O `select` do polo pode ser desenhado como `<option value="Passivo" selected>` ou com `selected="selected"`: aceite os dois na expressão, se preciso.)

`ProcessoFormularioInterativoTests.cs` (bUnit):

```csharp
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Web.Components.Pages;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

public class ProcessoFormularioInterativoTests : TelaInterativa
{
    private static readonly Guid Area = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a9");
    private static readonly ClienteResumo Cooperativa = new(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c9"),
        TipoCliente.PessoaJuridica, "Cooperativa Agrícola Boa Safra", "11.222.333/0001-81", null, null, null, true);

    private IRenderedComponent<ProcessoFormulario> Formulario()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarAreasAtuacao, Resultado<IReadOnlyList<AreaAtuacaoResumo>>>(
                Resultado<IReadOnlyList<AreaAtuacaoResumo>>.Ok([new AreaAtuacaoResumo(Area, "Cível", true)]))
            .Responde<ListarClientes, Resultado<PaginaDeClientes>>(Resultado<PaginaDeClientes>.Ok(new([Cooperativa], 1, 1, 20, 1, 0)))
            .Responde<SalvarProcesso, Resultado<Guid>>(Resultado<Guid>.Ok(Guid.NewGuid()));
        return Desenhar<ProcessoFormulario>();
    }

    private static void Digitar(IRenderedComponent<ProcessoFormulario> tela, string rotulo, string valor) =>
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.TrimStart().StartsWith(rotulo)).QuerySelector("input, textarea")!.Change(valor);

    [Fact]
    public void Enter_na_busca_mostra_o_candidato_e_adicionar_vincula_no_polo_ativo()
    {
        var tela = Formulario();

        tela.Find("input[form=busca-de-cliente]").Input("coop");
        tela.Find("form#busca-de-cliente").Submit();

        tela.Find("div.aj-candidato .aj-candidato__titulo").TextContent.Should().Be("Cooperativa Agrícola Boa Safra");
        tela.Find("div.aj-candidato .aj-ajuda").TextContent.Should().Be("CNPJ 11.222.333/0001-81");
        tela.Find("div.aj-candidato button").Click();

        tela.Find("div.aj-vinculo__escolhido .aj-vinculo__nome").TextContent.Should().Be("Cooperativa Agrícola Boa Safra");
        tela.Find("div.aj-vinculo__escolhido select option[selected]").TextContent.Should().Be("Ativo");
        tela.FindAll("div.aj-candidato").Should().BeEmpty();
        Despachante.Recebidas.OfType<SalvarProcesso>().Should().BeEmpty();   // o Enter da busca não envia o processo
    }

    [Fact]
    public void Valor_da_causa_fora_do_formato_mostra_o_erro_no_campo_e_nao_grava()
    {
        var tela = Formulario();
        Digitar(tela, "Título", "Litoral × Seguradora — indenização");
        Digitar(tela, "Valor da causa", "84000.50");

        tela.Find("form#formulario-do-processo").Submit();

        tela.Markup.Should().Contain("Informe o valor como 84.000,00.");
        Despachante.Recebidas.OfType<SalvarProcesso>().Should().BeEmpty();
    }

    [Fact]
    public void Valor_no_formato_brasileiro_grava_o_numero_e_volta_com_o_recado()
    {
        var tela = Formulario();
        Digitar(tela, "Título", "Litoral × Seguradora — indenização");
        Digitar(tela, "Valor da causa", "84.000,00");
        tela.Find("select.aj-campo").Change(Area.ToString());

        tela.Find("form#formulario-do-processo").Submit();

        tela.WaitForAssertion(() => Despachante.Recebidas.OfType<SalvarProcesso>().Should().ContainSingle()
            .Which.Dados.ValorCausa.Should().Be(84000m));
        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/processos");
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Processo salvo.");
    }
}
```

(`AreaAtuacaoResumo` e `ClienteResumo` com os construtores de hoje.) Em `PortalDoClienteWebTests` e `PaginasProtegidasTests`, a rota `/processos/novo`.

Run: `dotnet build -m:1`
Expected: não compila (`ProcessoFormulario` não existe). É o RED.

- [ ] **Step 2: A tela**

```razor
@page "/processos/novo"
@page "/processos/{Id:guid}/editar"
@using System.ComponentModel.DataAnnotations
@inject IDespachante Despachante
@inject NavigationManager Navegacao
@inject Recados Recados

<PageTitle>@Titulo</PageTitle>

<CabecalhoDeDetalhe Titulo="@Titulo" Subtitulo="Preencha os dados e vincule os clientes do processo." VoltarPara="@Voltar" Origem="@Origem" />

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
    <Aviso Tipo="TipoDeAviso.Erro">Processo não encontrado.</Aviso>
}
else if (_erroDaConsulta is not null)
{
    <Aviso Tipo="TipoDeAviso.Erro">@_erroDaConsulta</Aviso>
}
else
{
    @if (_mensagem is not null)
    {
        <Aviso Tipo="TipoDeAviso.Erro">@_mensagem</Aviso>
    }

    <EditForm Model="_form" OnValidSubmit="SalvarAsync" FormName="processo" id="formulario-do-processo" class="aj-lado-a-lado aj-lado-a-lado--formulario">
        <DataAnnotationsValidator />
        <section class="aj-painel aj-painel--principal aj-lado-a-lado__lista">
            <h2>Processo</h2>
            <div class="aj-grade-campos">
                <RotuloDeCampo Texto="Título" Para="() => _form.Titulo" Classe="aj-grade-campos__inteiro"><InputText @bind-Value="_form.Titulo" class="aj-campo" /></RotuloDeCampo>
                <label class="aj-rotulo">Número CNJ<InputText @bind-Value="_form.NumeroCnj" inputmode="numeric" placeholder="0000000-00.0000.0.00.0000" class="aj-campo" /></label>
                <label class="aj-rotulo">Área de atuação<InputSelect @bind-Value="_form.AreaAtuacaoId" class="aj-campo"><option value="@Guid.Empty">Selecione…</option>@foreach (var area in _areas)
                    {<option value="@area.Id">@(area.Ativo ? area.Nome : $"{area.Nome} (inativa)")</option>}</InputSelect></label>
                <label class="aj-rotulo">Órgão<InputText @bind-Value="_form.Orgao" class="aj-campo" /></label>
                <label class="aj-rotulo">Comarca<InputText @bind-Value="_form.Comarca" class="aj-campo" /></label>
                <label class="aj-rotulo">Parte contrária<InputText @bind-Value="_form.ParteContraria" class="aj-campo" /></label>
                <label class="aj-rotulo">Data de distribuição<InputDate @bind-Value="_form.DataDistribuicao" class="aj-campo" /></label>
                <RotuloDeCampo Texto="Valor da causa" Para="() => _form.ValorCausa"><InputText @bind-Value="_form.ValorCausa" inputmode="decimal" placeholder="0,00" class="aj-campo" /></RotuloDeCampo>
                <label class="aj-rotulo aj-grade-campos__inteiro">Observações<InputTextArea @bind-Value="_form.Observacoes" rows="3" class="aj-campo" /></label>
            </div>
        </section>

        <section class="aj-painel aj-lado-a-lado__painel">
            <h2>Clientes do processo</h2>
            @if (_form.Clientes.Count == 0)
            {
                <span class="aj-ajuda">Nenhum cliente vinculado.</span>
            }
            @foreach (var vinculo in _form.Clientes)
            {
                <div class="aj-vinculo__escolhido" @key="vinculo.ClienteId"><span class="aj-vinculo__nome">@vinculo.Nome</span><label class="aj-campo-filtro aj-campo-filtro--polo"><span>Polo</span><select class="aj-campo-filtro__controle" @bind="vinculo.Polo">@foreach (var polo in Enum.GetValues<PoloProcessual>())
                    {<option value="@polo">@RotulosDeProcesso.Polo(polo)</option>}</select></label><button type="button" class="aj-botao aj-botao--fantasma aj-botao--icone" aria-label="@($"Remover {vinculo.Nome}")" @onclick="() => Desvincular(vinculo)"><Icone Nome="fechar" /></button></div>
            }
            @* O Enter na busca envia o formulário "busca-de-cliente", que fica no fim da tela, e não o do processo. *@
            <label class="aj-campo-icone"><Icone Nome="buscar" /><span class="aj-so-leitor">Adicionar cliente (nome, CPF ou CNPJ)</span><input type="search" class="aj-campo-icone__controle" form="busca-de-cliente" placeholder="Adicionar cliente por nome, CPF ou CNPJ" @bind="_buscaDeCliente" @bind:event="oninput" /></label>
            @foreach (var candidato in _candidatos)
            {
                <div class="aj-candidato" @key="candidato.Id"><span class="aj-candidato__texto"><span class="aj-candidato__titulo">@candidato.Nome</span><span class="aj-ajuda">@TextosDoCliente.Documento(candidato.Tipo, candidato.Documento)</span></span><button type="button" class="aj-botao aj-botao--secundario" @onclick="() => Vincular(candidato)">Adicionar</button></div>
            }
            @if (_nenhumClienteEncontrado)
            {
                <span class="aj-ajuda">Nenhum cliente encontrado.</span>
            }
            @if (_clientesTruncados)
            {
                <span class="aj-ajuda">Mostrando os primeiros resultados. Refine a busca para ver outros.</span>
            }
            <span class="aj-ajuda">O cliente entra no polo ativo. Troque o polo ao lado do nome.</span>
        </section>
    </EditForm>

    <div class="aj-so-desktop">
        <div class="aj-acoes aj-acoes--fixa"><a href="@Voltar" class="aj-botao aj-botao--fantasma">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" form="formulario-do-processo" disabled="@_salvando"><Icone Nome="confirmar" />Salvar processo</button></div>
    </div>
    <div class="aj-so-tablet">
        <div class="aj-acoes aj-acoes--fixa"><a href="@Voltar" class="aj-botao aj-botao--secundario">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" form="formulario-do-processo" disabled="@_salvando"><Icone Nome="confirmar" />Salvar processo</button></div>
    </div>
    <div class="aj-so-mobile">
        <div class="aj-acoes aj-acoes--fixa"><a href="@Voltar" class="aj-botao aj-botao--secundario">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" form="formulario-do-processo" disabled="@_salvando"><Icone Nome="confirmar" />Salvar</button></div>
    </div>

    <form id="busca-de-cliente" hidden @onsubmit="ProcurarClientesAsync" @onsubmit:preventDefault></form>
}
```

No `@code`: `[Parameter] public Guid? Id`; `Titulo`, `Voltar`, `Origem` como no formulário de cliente (com "processo"); o estado de antes (`_areas`, `_form`, `_buscaDeCliente`, `_candidatos`, `_nenhumClienteEncontrado`, `_clientesTruncados`) mais `_carregado`, `_autorizado`, `_naoEncontrado`, `_erroDaConsulta`, `_salvando`, `_mensagem`. O `Formulario` de antes, sem `Status`, com `[ValorEmReaisValido] public string? ValorCausa { get; set; }`. `OnInitializedAsync`: perfil primeiro; com `Id`, `ObterProcesso` (preenche o formulário; `ValorCausa = ValorEmReais.Escrever(p.ValorCausa)`) e as áreas com a atual; sem `Id`, as áreas ativas. `SalvarAsync`: o de antes, com `ValorEmReais.TentarLer(_form.ValorCausa, out var valor)` (já validado) e `valor` no `DadosDoProcessoEntrada`; guarda `_salvando`; sucesso: recado e `Navegacao.NavigateTo(Voltar)`. `ProcurarClientesAsync`, `Vincular`, `Desvincular`: os de antes (a busca lê `PaginaDeClientes.Itens`).

- [ ] **Step 3: Trava e testes**

Acrescente a `Migradas`: `Components/Pages/ProcessoFormulario.razor`.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ProcessoFormulario|FullyQualifiedName~ProcessoFicha|FullyQualifiedName~ProcessosWebTests|FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~PaginasProtegidasTests"`
Expected: 0 avisos; todos passando.

- [ ] **Step 4: Commit**

```
git add src tests
git commit -m "feat(web): formulário de processo em tela própria, com clientes vinculados por polo, busca sem botão e valor da causa no formato brasileiro"
```

---

### Task 13: Suíte inteira, conferência no navegador e emenda da spec

Feita pela sessão que coordena.

- [ ] **Step 1: Suíte inteira** em lotes (`suite-em-lotes.ps1`), com o build feito antes. Expected: unidade e integração sem falha, com mais testes que a linha de base (872 e 676).

- [ ] **Step 2: Ambiente descartável** (banco na 5599, aplicação publicada na 5197, servidor dos quadros na 5198, Chrome sem janela ou a ferramenta do Chrome), com a ferramenta da 9C (`conferencia-de-telas\9c`). Dados do quadro: os seis clientes de `Clientes.dc.html` (um inativo), os cinco processos de `Processos.dc.html` (um suspenso, um encerrado, um sem número), os documentos de `Cliente.dc.html` (3 gerais, 1 contrato, 4 de atividades, 2 do cliente, para a Transportes Litoral) e o usuário do cliente Paulo Nogueira. Mais os do Review Focus: nome, e-mail e documento longos; um processo sem parte contrária.

- [ ] **Step 3: Medir as 18 telas** contra os quadros (`ref.html`), nas três larguras, e conferir por captura as que têm `sc-if` ou modelo (`{{ … }}`): os cartões das listas, as linhas do tablet, os dois formulários. Medir também o que o `app.css` muda dentro das telas (`V.vazamento`). O que divergir: CSS no design system (Tarefa 1 de novo: publicar, sincronizar o canvas e copiar), nunca no `app.css` nem com `style`; marcação na tela, com o teste ajustado antes.

- [ ] **Step 4: Teclado e estados:** as abas do mobile (toque e Tab), o menu de ações das linhas de documento (Tab, Esc, fundo), a confirmação de inativação no topo a 390px, o foco nos painéis de documento ao abrir, a barra fixa sem cobrir o último painel no tablet e no mobile, larguras 760/761/1199/1200 nas fichas, a busca da barra do mobile com Enter.

- [ ] **Step 5: Emenda da spec** (seção 4.1, a cópia do design system no canvas sincronizada a cada publicação; seção 11, como a 9D conferiu; seção 15, "Da fase 9D": o que ficou para as fases seguintes, e a pendência da 9C marcada como publicada em 2026-10-14; o cabeçalho, com as versões do design system publicadas pela 9C e pela 9D) e commit.

- [ ] **Step 6: Suíte inteira de novo** se a conferência mudou código, e o aval do usuário para a `main`.

