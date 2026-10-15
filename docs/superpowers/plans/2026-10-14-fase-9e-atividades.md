# Fase 9E — Atividades no design system: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** As telas de atividades (a lista, a ficha, o formulário de nova e de edição, responsáveis e revisor, devolver e reabrir) ficam idênticas aos quadros do canvas nas três visões, cada uma com rota própria, com os contadores que os quadros mostram vindos do backend.

**Architecture:** Cada padrão que os quadros da fase trazem como estilo embutido vira classe `aj-` no artifact do design system e é copiado para `wwwroot/css` (Tarefa 1). Os painéis que hoje se abrem dentro da lista (ficha, formulário, responsáveis, revisor, devolver, reabrir) viram telas com rota, e a lista e Produtores passam a levar a elas por link. A ficha emite cada painel uma vez só: o arranjo lado a lado põe o painel de detalhes primeiro e o CSS o leva para a direita no desktop; os blocos que mudam por visão vão em invólucros de visão; as abas do mobile escondem por CSS o que não é da aba escolhida. Os comentários e os documentos da atividade continuam componentes com estado próprio, e os documentos passam a herdar a base `GestaoDeDocumentos` da 9D. O estado de carregamento dos formulários é extraído antes de a fase escrever os seus (spec 15, "Da fase 9D").

**Tech Stack:** .NET 10, Blazor Interactive Server, CSS do design system (tokens e classes `aj-`), xUnit 2.9, FluentAssertions 7.2, Testcontainers, bUnit 2.11.3 (só no projeto de testes de integração).

**Spec:** `docs/superpowers/specs/2026-10-10-migracao-para-o-design-system-design.md` (seções 3, 4, 5, 6, 7.5, 8, 9, 10, 11 e 15).

## Decisões deste plano

- **Quadros desta fase:** baixados do canvas em 2026-10-14 (versão `1792026220-8711`). Cópia local, somente leitura, em `C:\Users\Gabriel Paulino\AppData\Local\Temp\claude\c--GitHub-SIstemaControleAtividadesJuridicas\73464877-64cd-41cc-b340-808de5c66612\scratchpad\quadros-9e\project\`: `Atividades`, `FichaDaAtividade` (com `FichaTablet` e `FichaMobile`), `AtividadeFormulario`, `Responsaveis`, `Devolver`, cada um com as variantes `Tablet` e `Mobile` (`.dc.html`), mais `Menu`, `Trilho`, `NavInferior` e a cópia do design system em `ds/araujods/`. Os quadros da lista e da ficha são antigos (estilo embutido); os de formulário, responsáveis e devolver já usam classes `aj-`. Levantamentos que este plano cita, na pasta `…\scratchpad\plano-9e\`: `inventario-codigo.md` (o código de hoje, com linhas), `quadros-lista-e-formularios.md` e `quadros-ficha.md` (cada quadro bloco a bloco, com textos e medidas).
- **Rotas** (spec 5): `/atividades/nova`, `/atividades/{Id:guid}`, `/atividades/{Id:guid}/editar`, `/atividades/{Id:guid}/responsaveis`, `/atividades/{Id:guid}/devolver`, `/atividades/{Id:guid}/reabrir`. A lista perde tudo o que era painel; "Abrir" e o cartão levam à ficha. `RotasDoCliente` não muda (as rotas novas nascem fechadas ao portal).
- **Vindo de Produtores** (spec 5): o painel de produtividade passa a ter link `/atividades/{id}?de=produtores&produtor={produtorId}`. A ficha com `de=produtores` volta para `/produtores?produtor={produtorId}` com o rótulo "Produtores" (sem `produtor`, para `/produtores`). O mês escolhido no painel não está no endereço: volta ao mês corrente (anotado na spec).
- **Depois de salvar** (spec 5): nova atividade volta à lista com "Atividade salva."; edição, responsáveis, troca de revisor, devolver e reabrir voltam à ficha com o recado de cada um. Excluir **sempre** volta a `/atividades` com "Atividade excluída." (também vindo de Produtores, que não lê recados). Concluir minha parte e aprovar ficam na ficha, com o aviso no topo.
- **Contadores** (spec 6): `ListarAtividades` passa a devolver `PaginaDeAtividades` (derivada de `Pagina<AtividadeResumo>`), com `NoEscopo` (todas as atividades que o usuário pode listar, sem nenhum filtro) e `Atrasadas` (as atrasadas desse mesmo escopo). Escopo: a Diretoria, o escritório inteiro (o filtro de responsável também é ignorado); a Produção, as atividades em que é responsável. Quem lê como `Pagina<AtividadeResumo>` (o formulário de evento) continua compilando.
- **Subtítulo e chips:** `{n} atividades · {m} atrasadas` (`1 atividade`, `1 atrasada`; sem a parte das atrasadas quando m é 0, como `Contagem.ComInativos`). A parte das atrasadas vai num `strong` de alerta: vermelho no tablet e no mobile, texto comum no desktop (o quadro). Chips: `Todas · {n}`, `Atrasadas · {m}` (sempre, mesmo com 0), e as situações sem contador.
- **Filtros:** o estado é um só (busca, situação, só atrasadas, responsável, revisor) e as três visões o mostram. Chip "Todas" limpa situação e "só atrasadas"; "Atrasadas" liga "só atrasadas" e limpa a situação; um chip de situação escolhe a situação e desliga "só atrasadas". Chip busca na hora, mantendo a busca e responsável/revisor. No desktop, os campos aplicam ao clicar "Buscar"; a paginação e os chips consultam com o que está nos campos (como `Processos.razor`). O painel de filtros do tablet e do mobile (spec 9) traz Situação (as oito opções, inclusive Criada e Reaberta, que não têm chip), Responsável e Revisor (só a Diretoria) e "Só atrasadas", com "Limpar" e "Filtrar". O botão que abre o painel diz "Responsável, revisor" no tablet da Diretoria e "Mais filtros" no tablet da Produção; no mobile é sempre o botão de ícone `aria-label="Mais filtros"`.
- **Complexidade** (spec 8): coluna "Complexidade" depois de "Entrega" na tabela do desktop; linha "Complexidade" em "Detalhes" (desktop, depois de "Área") e em "Dados" (tablet e mobile, por último); controle segmentado `1`, `2`, `3` no formulário (nenhum escolhido numa atividade nova).
- **Etiquetas de situação:** `EtiquetaDeSituacao` ganha a forma. Na tabela, a Concluída leva o visto e a Atrasada o relógio, sem ponto; nos cartões e na ficha, todas com ponto, sem ícone (os quadros).
- **Avatares** (cartões, responsáveis e comentários da ficha): a sigla são as iniciais do primeiro e do último nome; a cor (`azul`, `verde`, `ambar`, `lilas`) sai do nome por uma conta estável (o resumo não traz o identificador). O usuário logado não tem cor própria: segue a mesma regra.
- **Ficha:**
  - O cabeçalho é um componente do assunto (`CabecalhoDaAtividade`), não o `CabecalhoDeFicha` da 9D: o quadro tem a linha de situação no lugar do subtítulo e a faixa de ações no desktop e no tablet.
  - Ordem da faixa: a ação primária (Concluir minha parte, ou Aprovar, ou Reabrir), Devolver, o divisor, Editar, Responsáveis, Trocar revisor, e Excluir à direita. Cada uma só com a permissão de `ObterAtividade.Acoes`; o divisor só com ação dos dois lados; sem nenhuma ação, sem faixa.
  - "Trocar revisor" só aparece quando a atividade não permite redefinir os responsáveis (quando permite, o painel "Revisor" da tela de responsáveis já troca o revisor). Spec 8: a troca de revisor é o painel "Revisor" da tela de responsáveis.
  - No mobile, "Mais ações" (`MenuDeAcoes` na barra de voltar) traz Editar, Responsáveis, Trocar revisor e Excluir (por último, em perigo), cada um com a sua permissão; a ação primária e Devolver vão na barra fixa. Spec 9 cita "editar, responsáveis e excluir" e manda levar as mesmas ações do desktop: Trocar revisor entra, senão a função some no mobile.
  - Confirmações (spec 8): Concluir minha parte, Aprovar e Excluir pedem confirmação num `Aviso` de atenção no topo, com "Cancelar" e o verbo.
  - Documentos no tablet e no mobile: o nome é o link de download e as ações (Renomear, Excluir) vão num "⋯" (`MenuDeAcoes`, `Ações de {nome}`), como a spec 9 prevê; no desktop, os botões do quadro. "Enviar documento" (sem quadro) é botão secundário à direita do título do painel, nas três visões, e abre o painel de envio logo abaixo do painel "Documentos" (padrão da 9D).
  - Abas do mobile: `Detalhes`, `Comentários {n}`, `Documentos {n}`, `Histórico` (`Rotulo="Seções da atividade"`).
- **Divergências dos quadros antigos que ficam como nas fases anteriores** (para todas as telas ficarem iguais entre si): o `Aviso` com o ícone solto (o quadro da ficha desenha um selo redondo); a barra de voltar do mobile com o `h1` do design system (o quadro tem um `span` de 16px); o voltar do tablet e o respiro de cima do conteúdo pelas variáveis do design system; a barra fixa do mobile com 46px (o quadro da ficha tem 50); o hover do perigo com `--erro-fundo`; o `letter-spacing` do título; o nome no título da ficha do mobile com os 26px das fichas da 9D (o quadro tem 27); o "Buscar" com o respiro da classe; a sombra do botão primário no tablet; o vão de 12px entre os cartões do mobile. Ficam anotados na spec.
- **Estado de carregamento dos formulários** (spec 15, "Da fase 9D"): um componente `CargaDoFormulario` mostra "Carregando…", "Acesso negado.", o "não encontrado" e o erro com "Tentar de novo", e entra nos três formulários que já existem (cliente, processo, usuário), que ganham "Tentar de novo".
- **Documentos da atividade** herdam `GestaoDeDocumentos`: a base ganha dois ganchos protegidos (listar e enviar), com o comportamento de hoje como padrão. A 9E é o terceiro uso de `Quantos` e da tabela, mas as três tabelas têm colunas e ações diferentes: só `Quantos` é extraído, se repetido.
- **Comentário novo:** no desktop e no mobile, o rótulo "Novo comentário" visível, a caixa de três linhas e "Comentar"; no tablet, em linha, com o rótulo só para leitor e o botão de ícone "Enviar comentário" (o quadro). O design system ganha a classe de visibilidade `aj-desktop-mobile` para isso.

## Como ler este plano

Cada tarefa traz as **regras** (o contrato), as **interfaces** que ela consome e produz e os **testes obrigatórios**. O plano traz o código onde ele é a própria fidelidade (o CSS, a marcação das telas) ou onde o detalhe é sutil. O restante é escrito no estilo do que já existe. O código do plano não foi executado: quando o Blazor, o bUnit ou o compilador exigirem, ajuste o mínimo para preservar o comportamento e registre o desvio no relatório, com a mensagem de erro que o motivou.

Nunca apague um teste nem afrouxe uma asserção. Quando uma asserção mudar porque a marcação mudou, reescreva-a mantendo o que ela prova e liste-a no relatório. Asserção negativa (`NotContain`) que deixaria de provar algo com a marcação nova ganha uma afirmação positiva ao lado. Asserção sobre o bloco de uma visão (`BlocoDeVisao.De`) sempre tem uma afirmação positiva sobre o mesmo bloco: um bloco vazio aceitaria qualquer `NotContain`.

O HTML desenhado pode ter espaço em branco e quebra de linha entre as marcas. As expressões regulares dos testes usam `\s*` entre marcas e `(?s)` quando atravessam linhas; quem escreve a marcação deixa colados os elementos que o teste procura colados.

O que só se vê depois de um clique no navegador (o Blazor aplicando diferenças ao DOM) o bUnit não prova: chaves `@key` de dois tipos de linha no mesmo pai nunca podem coincidir (lição da 9D).

A fonte de cada tela é o quadro do canvas. Antes de escrever a marcação de uma tela, releia os três arquivos dela na cópia local e a seção dela nos levantamentos. Se um quadro diferir do que este plano descreve, pare e relate.

## Estado de partida

- Worktree `.claude/worktrees/feat+fase-9e-atividades`, branch `worktree-feat+fase-9e-atividades`, na `main` (`40d4f12`), que já tem as fases 9A a 9D.
- Canvas: https://claude.ai/artifact/HzHWv1pgVXAnYHuPEYQDFj. Design system: https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS, versão `1792024406-465e` (a última da 9D); o `componentes.css` do código é cópia dela.
- Web, depois da 9D: `CabecalhoDePagina` (`Titulo`, `Subtitulo`, `SubtituloMobile`, `Folgado`, `VoltarPara`, `RotuloDoVoltar`, `Acao`, `IconeDaAcao`, `AcaoHref`, `OnAcao`, `Complemento`, `LinhaDoMobile`), `CabecalhoDeDetalhe` (`Titulo`, `Subtitulo`, `VoltarPara`, `Origem`), `BarraDeVoltar` (`Titulo`, `VoltarPara`, `Rotulo`), `CabecalhoDeFicha`, `Abas`/`Aba`, `Aviso` (com `Acoes`, `Titulo`, `Rotulo`), `EstadoDaLista`, `Paginacao` (`SempreVisivel`, `Ocupado`), `MenuDeAcoes`, `RotuloDeCampo` (com `Classe`), `Icone` (com `Classe`), `Contagem` (`De`, `ComInativos`), `Recados`, `FusoDoEscritorio.Formatar`, `ValorEmReais`. As telas desta fase (`Pages/Atividades.razor` e `Components/Atividades/*`) ainda têm a marcação antiga vestida pelo `app.css`, e só existe a rota `/atividades`.
- Documentos: `GestaoDeDocumentos` (base, com uma coisa aberta por vez e o foco), `DocumentosDaFicha`, `DocumentosComunsDaDiretoria`, `DocumentosParaBaixar`, `TextosDeDocumento`. `DocumentosDaAtividade` ainda é avulso, com marcação antiga.
- Testes: `AplicacaoDeTeste`, `TelaDeTeste`, `TelaInterativa` (bUnit, com `AtorDaDiretoria` e `AtorDaProducao`), `DespachanteCombinado` (`Responde`, `RespondeDepois`, `Recebidas`), `ComBarraSuperior<T>`, `BlocoDeVisao.De`. Nenhum teste de atividade é interativo hoje; a ficha, os comentários, os documentos, os responsáveis, devolver e reabrir não têm teste web.
- Linha de base: a suíte inteira passou em `5643769` (a árvore da `main` de hoje): unidade 882, integração 771.
- `git` só pela ferramenta PowerShell. Antes de cada commit, `git branch --show-current`.
- A máquina tem caído ao começar build ou teste: build sempre `dotnet build -m:1`; teste sempre `--no-build` depois de um build feito; um comando por vez. Depois de uma queda que pegou build, confira as DLLs (`[Reflection.AssemblyName]::GetAssemblyName`) antes de confiar no build incremental. A suíte inteira roda em lotes (`suite-em-lotes.ps1`, em `C:\Users\Gabriel Paulino\.claude\projects\c--GitHub-SIstemaControleAtividadesJuridicas\conferencia-de-telas\`).

## Global Constraints

- TFM único `net10.0`. `Nullable` e `TreatWarningsAsErrors=true`. Nenhum pacote novo.
- Código, nomes e textos em português, com acentuação correta. Arquivos em UTF-8 sem BOM, com o fim de linha que o arquivo já tem.
- A camada Web só chega aos dados pelo `IDespachante` (`WebSoPeloDespachanteTests`). Consulta nova no backend só os contadores da Tarefa 2, dentro da listagem que já existe.
- `wwwroot/css/tokens.css` e `wwwroot/css/componentes.css` não são editados à mão: mudam no artifact do design system e são copiados de lá.
- Tela migrada não tem atributo `style`, nem `<style>`, nem arquivo `.razor.css`, nem classe que não comece por `aj-`, nem `<button>`, `<table>`, `<input>`, `<select>` ou `<textarea>` sem `class`. Nenhum CSS novo no `app.css`.
- As classes de visibilidade (`aj-so-desktop`, `aj-so-tablet`, `aj-so-mobile`, `aj-desktop-tablet`, `aj-tablet-mobile`, `aj-desktop-mobile`) e a de aba (`aj-aba-painel`) vão num invólucro próprio (`div` ou `span`) cuja classe é **só** a de visibilidade ou a de aba, nunca no elemento do componente.
- Visões: mobile até 760px, tablet de 761px a 1199px, desktop a partir de 1200px.
- Todo texto que um teste procura no HTML é escrito por **uma única** expressão Razor ou por marcação fixa.
- Rotas existentes não mudam. Rotas novas: as seis da seção "Decisões". `RotasDoCliente` não muda.
- Botão só de ícone leva `aria-label` com o verbo e o objeto ("Excluir atividade", "Enviar comentário", "Ações de minuta-acordo-v2.docx").
- Chaves `@key` de dois tipos de linha no mesmo elemento pai nunca coincidem.
- Formulário em tela própria: depois de salvar com sucesso, o botão de envio fica travado até a navegação chegar (o `finally` só o libera se a tela não navegou), como os da 9D.
- Produção: vê a lista (só as suas atividades) e a ficha das atividades em que é responsável; conclui a sua parte, comenta e envia documento; não vê nenhuma ação de cadastro ("Nova atividade", Editar, Responsáveis, Trocar revisor, Excluir, Aprovar, Devolver, Reabrir, Renomear e Excluir documento), nem os filtros de responsável e revisor. Nos formulários e nas telas de responsáveis, devolver e reabrir: o cabeçalho e "Acesso negado.", sem formulário e sem consulta (a tela confere o perfil antes de consultar).
- Textos exatos, dos quadros:
  - Lista: `Atividades`; subtítulo `{n} atividades · {m} atrasadas` (`1 atividade`, `1 atrasada`; sem a parte das atrasadas com 0); ação `Nova atividade`; desktop: busca com o rótulo para leitor `Busca` e o exemplo `Buscar pelo título da atividade`, `Situação` (`Todas`, `Criada`, `Direcionada`, `Feita`, `Para correção`, `Corrigida`, `Concluída`, `Reaberta`), `Responsável` (`Todos` e os nomes), `Revisor` (`Todos` e os nomes), `Só atrasadas`, `Buscar`; colunas `Título`, `Situação`, `Entrega`, `Complexidade`, `Cliente`, `Processo`, `Responsáveis`, `Revisor`, `Ações` (só para leitor); `—` em Processo, Responsáveis e Revisor vazios; `Abrir`; tablet: busca com o rótulo para leitor `Buscar atividade` e o exemplo `Buscar pelo título da atividade`, botão `Responsável, revisor` (Produção: `Mais filtros`); chips com `aria-label="Filtrar por situação"`: `Todas · {n}`, `Atrasadas · {m}`, `Direcionadas`, `Feitas`, `Para correção`, `Corrigidas` (só no tablet), `Concluídas`; mobile: busca com `Buscar atividade` e o exemplo `Buscar pelo título`, botão de ícone `aria-label="Mais filtros"`; cartão: `{cliente}`, `{processo}` ou `Sem processo vinculado`, a data de entrega, avatares com `title="{nome}"` ou `Sem responsável`; paginação `Página {n} de {m} · {x} atividades`, `Anterior`, `Próxima`
  - Ficha: voltar `Atividades` (vindo de Produtores, `Produtores`; no mobile `Voltar para Atividades` e o título da barra `Atividade`); linha de situação `Entrega em {dd/MM/yyyy} · {área}` (mobile: `Entrega {dd/MM/yyyy} · {área}`); ações `Concluir minha parte`, `Aprovar`, `Reabrir`, `Devolver`, `Editar`, `Responsáveis`, `Trocar revisor`, `Excluir` (tablet: botão de ícone `Excluir atividade`); mobile `Mais ações`; painéis `Detalhes` (desktop; tablet e mobile `Dados`) com `Entrega`, `Cliente`, `Processo`, `Área`, `Complexidade`, `Autor`, `Revisor` (tablet e mobile sem Entrega e Área), `Descrição`, `Responsáveis` (`Concluída em {dd/MM/yyyy HH:mm}`; tablet e mobile `Concluída em {dd/MM HH:mm}`), `Histórico` (`{situação}` no primeiro registro, `{anterior} → {nova}` nos outros; `{nome} · {dd/MM/yyyy HH:mm}`), `Comentários` (data `{dd/MM/yyyy HH:mm}` no desktop e no mobile, `{dd/MM HH:mm}` no tablet; `Novo comentário`, exemplo `Escreva um comentário para a equipe`, `Comentar`; tablet: exemplo `Escreva um comentário`, botão de ícone `Enviar comentário`), `Documentos` (`{tamanho} · {autor} · {dd/MM/yyyy HH:mm}`; tablet e mobile `{tamanho} · {autor}`; `Renomear`, `Excluir`); abas `Detalhes`, `Comentários {n}`, `Documentos {n}`, `Histórico`, com `aria-label="Seções da atividade"`
  - Formulário: `Nova atividade` (na edição, `Editar atividade`); subtítulo `Diga o que precisa ser feito e a que processo ou cliente se refere.`; voltar `Atividades` (na edição, o título da atividade; no mobile `Voltar para Atividades` / `Voltar para a atividade`); painéis `Atividade` e `Vínculos`; `Título`, `Data de entrega`, `Complexidade` (`1`, `2`, `3`), `Descrição`; processo escolhido: `Processo`, o título, `Área {área}, definida pelo processo`, botão de ícone `Sem processo`; `Cliente do processo` (`Selecione…`); `O título aparece para o cliente no portal.`; busca com o rótulo para leitor `Escolher processo (título, parte contrária ou número CNJ)` e o exemplo `Trocar o processo: título, parte contrária ou número CNJ` (com processo escolhido; sem processo, o exemplo novo `Escolher processo: …`, abaixo); ações `Cancelar` e `Salvar atividade` (no mobile, `Salvar`)
  - Responsáveis: título `Responsáveis`; subtítulo `Marque quem é responsável e escreva o direcionamento de cada um. Quem está desmarcado deixa de ser responsável.`; voltar o título da atividade (mobile `Voltar para a atividade`); o painel principal com o título da atividade; opção `{nome}` com a nota `({perfil})`; `Direcionamento de {nome}`; painel `Revisor` (rótulo só para leitor `Revisor`; primeira opção `Manter / deduzir (atual: {revisor})`); ações `Cancelar` e `Salvar responsáveis` (no mobile, `Salvar`)
  - Devolver: `Devolver para correção`; subtítulo o título da atividade (no mobile, ajuda no topo); voltar o título da atividade (mobile `Voltar para a atividade`); `Motivo`; `Responsáveis que voltam a ter a parte pendente`; ações `Cancelar` e `Devolver`
  - Sem quadro, mantidos (do código de hoje): `Carregando…`; `Seu acesso foi encerrado. Entre novamente.`; `Acesso negado.`; `Atividade não encontrada.`; `Tentar de novo`; `Não foi possível consultar as atividades.`; `Nenhuma atividade.`; `Não foi possível consultar a atividade.`; `Sua parte foi concluída.`; `Atividade aprovada.`; `Atividade excluída.`; `Revisor alterado.`; `Atividade devolvida para correção.`; `Atividade reaberta.`; `Responsáveis salvos.`; `Atividade salva.`; `{mensagem sem o ponto final}, mas não foi possível atualizar a ficha.` (ex.: `Atividade aprovada, mas não foi possível atualizar a ficha.`); `A atividade mudou. Confira os dados atualizados.`; `Pendente`; `Sistema` (autor); `Nenhum responsável.`; `Nenhum registro.`; `Comentários` e `Documentos`: `Você não tem mais acesso a esta atividade.` (o acesso encerrado dentro do painel); `Comentários`: `Autor não identificado`, `Nenhum comentário.`, `Não foi possível consultar os comentários.`, `Enviando…`, `Comentário enviado.` e `Comentário enviado, mas não foi possível atualizar a lista.`; documentos: os textos da base (`Nenhum documento.`, `Documento enviado.`, `Nome alterado.`, `Documento excluído.`, `Arquivo (até 50 MB)`, `Nome (opcional)`, `Enviar`, `Enviando…`, `Novo nome`, `Salvar nome`, `Renomear "{nome}"`, `Confirmar exclusão`, `O arquivo passa do limite de 50 MB.`); formulário: `Área de atuação` (`Nenhuma`, `{área} (inativa)`), `Escolher cliente (nome, CPF ou CNPJ)`, `Sem cliente`, `Nenhum processo encontrado.`, `Nenhum cliente encontrado.`, `Mostrando os primeiros resultados. Refine a busca para ver outros.`, `{nome} (não é mais cliente deste processo)`, `O cliente atual não é mais cliente deste processo. Escolha outro cliente do processo para salvar.`, `Não foi possível consultar as áreas de atuação.`, `Não foi possível consultar o processo da atividade.`; responsáveis: `{nome} (inativo)`, `{nome} (usuário indisponível)`, `Não foi possível consultar os usuários.`; devolver e reabrir: `Informe o motivo.`, `Marque ao menos um responsável.`
  - Sem quadro, novos por analogia: `Reabrir atividade` (título da tela de reabrir; botão `Reabrir`); `Trocar revisor` (título da tela de responsáveis no modo só do revisor; botão `Trocar revisor`); `Escolha o novo revisor.`; `Informe o título.`, `Informe a descrição.`, `Escolha a complexidade.` (erros por campo); confirmação de conclusão: `aria-label="Confirmar conclusão"`, título `Concluir a sua parte em {título}?`, texto `A sua parte fica registrada como concluída.`, botões `Cancelar` e `Concluir minha parte`; de aprovação: `aria-label="Confirmar aprovação"`, título `Aprovar {título}?`, texto `A atividade fica concluída.`, botões `Cancelar` e `Aprovar`; de exclusão: `aria-label="Confirmar exclusão"`, título `Confirmar a exclusão de {título}?`, texto `A atividade sai das listas com os responsáveis e o histórico.`, botões `Cancelar` e `Confirmar exclusão`; `Enviar documento`; confirmação de exclusão de documento com o título `Confirmar a exclusão de {nome}?` e o texto `O documento é excluído da atividade.`; `Sem cliente` no cartão sem cliente; `Sem data de entrega` na linha de situação sem data; `Produtores` (voltar vindo de lá); `Esta atividade não permite mais essa ação.` (tela de responsáveis, devolver ou reabrir aberta quando a ação já não vale); formulário: `Escolher cliente: nome, CPF ou CNPJ` (exemplo da busca de cliente), `Escolher processo: título, parte contrária ou número CNJ` (exemplo da busca de processo quando nenhum processo está escolhido; `Trocar o processo: …` só com processo escolhido), `Cliente` (primeira linha do cliente avulso escolhido), `Atividade` (origem do voltar da edição e das telas de responsáveis, devolver e reabrir antes da carga e para a Produção)

## Review Focus

Condições que a spec implica e que mais provavelmente atingem quem usa atividades. Cada uma tem o seu teste na tarefa indicada.

1. **Contadores da Produção:** o subtítulo e os chips contam só as atividades em que ela é responsável, nunca as do escritório; e a Diretoria com um responsável escolhido continua vendo o total do escritório no subtítulo. Testes nas Tarefas 2 e 6.
2. **Chip de situação com outros filtros:** escolher "Atrasadas" ou uma situação mantém a busca digitada e o responsável escolhido; "Todas" limpa situação e "só atrasadas"; o campo e a caixa do desktop mostram o mesmo estado. Teste na Tarefa 6.
3. **Ficha de atividade que não existe ou que a Produção não vê:** o voltar e "Atividade não encontrada.", sem ação, sem comentários e sem documentos (nem a consulta deles). Testes na Tarefa 10: por HTTP, a Produção que não é responsável, com a prova de que não houve consulta pelos eventos de acesso negado (só o de `ObterAtividade`; cada lista pedida deixaria o seu, e o registro de consultas só guarda consulta bem-sucedida); pelo bUnit, o id que não existe, sem nenhum pedido de comentários nem de documentos ao despachante.
4. **Clique duplo:** Salvar no formulário, nos responsáveis e no devolver, e Excluir na ficha: um envio só, e o botão travado depois do sucesso até a navegação. Aprovar e Concluir minha parte, que ficam na ficha: um envio só, com os botões travados enquanto a resposta não chega. Testes nas Tarefas 7, 11, 12 e 13.
5. **Devolver sem motivo ou sem responsável marcado:** o erro no campo e nada enviado; com motivo e um só marcado, só ele volta a ter a parte pendente. Teste na Tarefa 13.

Conferências no navegador, na Tarefa 15: título, cliente e processo longos nos cartões e no cabeçalho da ficha a 390px; um nome de documento longo nas três visões; as abas do mobile com toque e teclado; o "Mais ações" e o "⋯" dos documentos; a barra fixa sem cobrir o último painel; a confirmação no topo da ficha a 390px; o vínculo escolhido pela busca no formulário (a lição da 9D).

---

### Task 1: Design system — padrões de atividades e cópia para o código

Feita pela sessão que coordena: publica no artifact do usuário.

**Files:**
- Artifact do design system, sob `project/`: `components/bundle.css`, `design-system.json` (só `lastChange`); README e prévia dos componentes novos; README emendado (e prévia, onde há estado novo) dos que ganham modificador
- Artifact do design system: `project/responsividade.md` ("Como implementar": a classe `aj-desktop-mobile` entra na lista dos invólucros de visão)
- Artifact do canvas: `project/ds/araujods/components/bundle.css` e o registro `designSystems` do `canvas.json`
- Modify: `src/AtividadesJuridicas.Web/wwwroot/css/componentes.css`

**Interfaces:**
- Produces: as classes da lista e dos formulários (`quadros-lista-e-formularios.md`, seção 5) e as da ficha (`quadros-ficha.md`, seção 4), com os ajustes das decisões deste plano, mais a classe de visibilidade `aj-desktop-mobile`. A lista exata está no passo 2.

**Regras:**
- Nenhuma classe existente muda de valor. Medida de quadro que difere de uma classe existente vira modificador ou regra de contexto (spec, seção 3).
- Só tokens que já existem. Onde não há token, o literal do quadro: 48px (busca e botão da linha do tablet, botão de enviar comentário), 20px (respiro do cabeçalho da ficha no tablet, como na 9D, e do "Comentar"), 680px, 980px, 280px e 520px (larguras), e as medidas finas dos quadros da ficha (24px do divisor, 36/34px dos avatares, 40px do ícone de documento, 9px, 7px e 5px de respiro, 4px do canto do balão).
- Regra que muda por visão segue o padrão do arquivo: a consulta de mídia e, depois, as mesmas regras sob `[data-visao="…"]`, para a prévia de cada visão funcionar dentro da página do design system.
- Ficam de fora, pelas decisões do plano (as telas ficam iguais às das fases anteriores): o aviso com selo (`aj-aviso--com-selo`, `aj-aviso__selo`), a barra fixa alta (`aj-acoes--alta`) e qualquer regra sobre o título da barra de voltar do mobile, o `aj-voltar` do tablet, o respiro de cima do conteúdo, o `letter-spacing` do título, o respiro do "Buscar", a sombra do botão primário no tablet (nenhum `box-shadow:none` no primário da faixa nem em `aj-botao--enviar`) e o vão dos cartões no mobile. O nome no bloco de título da ficha fica com o tamanho das fichas da 9D (26px no mobile), sem os 27px do quadro.
- O bloco novo vai ao fim do `bundle.css`, depois das seções da 9D (a última é "Ajustes da conferência da fase 9D no navegador").

- [ ] **Step 1: Conferir o acesso e as versões**

A ferramenta Artifact abre o design system (https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS) e o canvas (https://claude.ai/artifact/HzHWv1pgVXAnYHuPEYQDFj). Se responder "artifact not found", o Claude Code está conectado a outra conta: pare e peça ao usuário para entrar com a conta dona do artifact. Nenhuma tela desta fase é escrita antes desta tarefa.

Ação `read` do design system e do canvas (o artifact inteiro, sem `path`, antes de publicar em cada um: sem essa leitura a publicação é recusada). O design system está na versão `1792024406-465e` (a última da 9D) e o canvas foi baixado para os quadros desta fase na versão `1792026220-8711`. Leia também `project/components/bundle.css` do design system (`read` com `path`) e compare com o corpo do `src/AtividadesJuridicas.Web/wwwroot/css/componentes.css` do worktree, da linha "Visões:" em diante: têm de ser idênticos (750 linhas no `componentes.css`, 749 no `bundle.css`). Se diferirem, alguém editou o design system: refaça as emendas sobre a versão nova e confira de novo os nomes do passo 2 contra ela. A versão também muda quando o tipo do artifact ganha release nova, sem mudar o conteúdo: compare o conteúdo, não o número.

- [ ] **Step 2: Acrescentar o bloco ao fim de `components/bundle.css`**

O bloco segue as regras da tarefa (acima). Ele produz exatamente estes nomes (nenhum existe no `bundle.css` de `1792024406-465e`; conferido por roteiro):

- Visibilidade: `aj-desktop-mobile` (com a variável `--aj-ver-desktop-mobile`).
- Cabeçalho de página: `aj-subtitulo__alerta`.
- Filtros: `aj-campo-icone--filtro`, `aj-opcao--solta`, `aj-linha-filtros--busca`, `aj-barra-superior__busca`, `aj-chips--barra`.
- Paginação: `aj-paginacao--com-setas`, `aj-paginacao__anterior`, `aj-paginacao__proxima`.
- Tabela e etiqueta: `aj-tabela--larga`, `aj-tabela__vazia`, `aj-etiquetas`, `aj-etiqueta--com-icone`, `aj-etiqueta--com-ponto`.
- Cartão de atividade: `aj-cartao--atividade`, `aj-cartao__topo`, `aj-cartao__vazio`.
- Formulários: `aj-vinculo__escolhido--detalhado`, `aj-vinculo__texto`, `aj-vinculo__titulo`, `aj-opcao__nota`, `aj-rotulo--recuado`, `aj-painel--sempre-destacado`, `aj-painel--medio`, `aj-acoes--medio`, `aj-conjunto`.
- Arranjo e cabeçalho da ficha: `aj-lado-a-lado--painel-estreito`, `aj-painel--cabecalho-com-faixa`, `aj-titulo-da-ficha--com-situacao`, `aj-titulo-da-ficha__situacao`, `aj-titulo-da-ficha__entrega`, `aj-painel__botoes--faixa`, `aj-painel__divisor`, `aj-botao--ao-fim`, `aj-botao--com-icone-menor`.
- Painéis da ficha: `aj-painel--ficha`, `aj-painel--descricao`, `aj-painel--detalhes`, `aj-painel--responsaveis`, `aj-painel--historico`, `aj-painel--comentarios`, `aj-painel--documentos`.
- Responsáveis: `aj-responsaveis`, `aj-responsavel`, `aj-responsavel__texto`, `aj-responsavel__cabeca`, `aj-responsavel__estado`, `aj-responsavel__estado--pendente`, `aj-responsavel__direcionamento`.
- Histórico: `aj-historico`, `aj-historico__item`, `aj-historico__marca`, `aj-historico__ponto`, `aj-historico__ponto--direcionada`, `--feita`, `--correcao`, `--corrigida`, `--concluida`, `--reaberta`, `--atual`, `aj-historico__linha`, `aj-historico__texto`, `aj-historico__mudanca`, `aj-historico__autor`, `aj-historico__motivo`.
- Comentários: `aj-comentarios`, `aj-comentario`, `aj-comentario__balao`, `aj-comentario__cabeca`, `aj-comentario__data`, `aj-comentario__texto`, `aj-comentarios__novo`, `aj-campo--folgado`, `aj-botao--enviar`.
- Documentos: `aj-documentos`, `aj-documento`, `aj-documento__icone`, `aj-documento__icone--texto`, `aj-documento__icone--planilha`, `aj-documento__texto`, `aj-documento__nome`, `aj-documento__meta`, `aj-documento__acoes`.
- Regras de contexto sem nome novo: `.aj-rotulo>.aj-segmentado` (o segmentado da complexidade dentro do rótulo; nenhuma tela de hoje põe um `aj-segmentado` direto num `aj-rotulo`), `.aj-barra-superior--voltar>.aj-menu-acoes>.aj-botao` (o "Mais ações" da barra de voltar com 44px, como o voltar; a barra em si não muda), `.aj-app__conteudo>.aj-desktop-mobile>*`.

São 84 nomes de classe novos.

```css
/* ---------- Atividades (fase 9E) ---------- */
/* Visibilidade: o bloco que existe no desktop e no mobile, mas não no tablet (o novo comentário com o rótulo visível).
   Mesmo padrão das outras cinco: a variável por visão e o invólucro que some do layout quando mostrado. */
:root{--aj-ver-desktop-mobile:contents}
@media (max-width:1199px){:root{--aj-ver-desktop-mobile:none}}
@media (max-width:760px){:root{--aj-ver-desktop-mobile:contents}}
[data-visao="desktop"]{--aj-ver-desktop-mobile:contents}
[data-visao="tablet"]{--aj-ver-desktop-mobile:none}
[data-visao="mobile"]{--aj-ver-desktop-mobile:contents}
.aj-desktop-mobile{display:var(--aj-ver-desktop-mobile)}
.aj-app__conteudo>.aj-desktop-mobile>*{flex-shrink:0}

/* Cabeçalho de página: a parte de alerta do subtítulo ("1 atrasada") fica em vermelho no tablet e no mobile (a regra
   .aj-subtitulo strong) e em texto comum no desktop. */
@media (min-width:1200px){.aj-subtitulo strong.aj-subtitulo__alerta{font-weight:400;color:inherit}}
[data-visao="desktop"] .aj-subtitulo strong.aj-subtitulo__alerta{font-weight:400;color:inherit}
[data-visao="tablet"] .aj-subtitulo strong.aj-subtitulo__alerta,[data-visao="mobile"] .aj-subtitulo strong.aj-subtitulo__alerta{font-weight:600;color:var(--erro)}

/* Filtros da lista de atividades. Barra do desktop: a busca com base menor e texto do tamanho dos filtros; a opção
   marcável sem caixa. Linha do tablet: busca e botão de filtros com 48px. Barra superior do mobile: a linha de busca
   com o botão de ícone e os chips que vão até a borda da tela. */
.aj-barra-filtros>.aj-campo-icone--filtro{flex-basis:240px}
.aj-campo-icone--filtro input{font-size:14.5px}
.aj-opcao--solta,.aj-opcao--solta:has(input:checked){min-height:var(--campo-desktop);padding:0 var(--espaco-12);border-color:transparent;background:transparent;font-size:14.5px}
.aj-linha-filtros--busca>.aj-campo-icone{min-height:48px}
.aj-linha-filtros--busca .aj-botao{min-height:48px;padding:0 var(--espaco-16);font-size:14.5px}
.aj-linha-filtros--busca .aj-botao svg{stroke-width:1.75}
.aj-barra-superior__busca{display:flex;gap:var(--espaco-8)}
.aj-barra-superior__busca>.aj-campo-icone{flex:1 1 auto;min-height:var(--controle-toque);border-radius:var(--raio-14)}
.aj-barra-superior__busca .aj-botao{border-color:var(--borda-campo-suave);color:var(--verde)}
.aj-barra-superior__busca .aj-botao:hover{border-color:var(--borda-campo-suave);background:var(--superficie-recuada)}
.aj-barra-superior__busca .aj-botao svg{width:20px;height:20px;stroke-width:1.75}
.aj-chips--barra{margin:0 calc(-1 * var(--espaco-16));padding:0 var(--espaco-16)}
.aj-chips--barra>.aj-chip{padding:0 var(--espaco-14)}

/* Paginação com setas (lista de atividades): a linha quebra, o texto tem o corpo da página e os botões levam seta;
   desabilitado, o botão fica apagado sem a transparência. */
.aj-paginacao--com-setas{flex-wrap:wrap;gap:var(--espaco-8) var(--espaco-16)}
.aj-paginacao--com-setas .aj-subtitulo{font-size:15px}
.aj-paginacao--com-setas .aj-botao{gap:var(--espaco-6);font-size:14px}
.aj-paginacao--com-setas .aj-botao svg{width:16px;height:16px}
.aj-paginacao--com-setas .aj-botao:disabled{opacity:1;border-color:var(--borda-opcao);background:var(--superficie-campo);color:var(--tinta-apagada)}
.aj-paginacao__anterior{padding:0 var(--espaco-16) 0 var(--espaco-12)}
.aj-paginacao__proxima{padding:0 var(--espaco-12) 0 var(--espaco-16)}

/* Tabela de atividades: largura mínima, o grupo de etiquetas da situação e o traço de célula vazia. */
.aj-tabela--larga{min-width:980px}
.aj-tabela td.aj-tabela__vazia{color:var(--tinta-icone)}
.aj-etiquetas{display:flex;flex-wrap:wrap;gap:var(--espaco-6)}
/* Etiqueta com ícone no lugar do ponto (Concluída na tabela, com o visto mais grosso) e com ponto no lugar do ícone
   (Atrasada nos cartões e na ficha). */
.aj-etiqueta--com-icone::before{display:none}
.aj-etiqueta--concluida.aj-etiqueta--com-icone svg{stroke-width:3}
.aj-etiqueta--com-ponto::before{display:block}

/* Cartão de atividade: título um pouco maior no tablet e meta mais justa no mobile; a linha de cima com as etiquetas
   e a seta (no mobile); o texto no lugar dos avatares. */
@media (min-width:761px) and (max-width:1199px){.aj-cartao--atividade .aj-cartao__titulo{font-size:16.5px}}
@media (max-width:760px){.aj-cartao--atividade .aj-cartao__meta{line-height:1.4}}
[data-visao="desktop"] .aj-cartao--atividade .aj-cartao__titulo,[data-visao="mobile"] .aj-cartao--atividade .aj-cartao__titulo{font-size:16px}
[data-visao="tablet"] .aj-cartao--atividade .aj-cartao__titulo{font-size:16.5px}
[data-visao="desktop"] .aj-cartao--atividade .aj-cartao__meta,[data-visao="tablet"] .aj-cartao--atividade .aj-cartao__meta{line-height:1.45}
[data-visao="mobile"] .aj-cartao--atividade .aj-cartao__meta{line-height:1.4}
.aj-cartao__topo{display:flex;align-items:center;gap:var(--espaco-6)}
.aj-cartao__topo>.aj-cartao__etiquetas{flex:1 1 auto}
.aj-cartao__vazio{font-size:13px;color:var(--tinta-apagada)}

/* Formulário da atividade: o controle segmentado dentro do rótulo (complexidade) e o vínculo escolhido com três
   linhas (o processo, com a área definida por ele). */
.aj-rotulo>.aj-segmentado{align-self:flex-start}
.aj-vinculo__escolhido--detalhado{padding:var(--espaco-8) var(--espaco-6) var(--espaco-8) var(--espaco-12)}
.aj-vinculo__texto{flex:1 1 auto;min-width:0;display:flex;flex-direction:column}
.aj-vinculo__titulo{font-weight:600;overflow-wrap:anywhere}

/* Responsáveis (tela própria): o complemento do nome na opção, o direcionamento recuado sob a opção marcada e o
   painel principal que mantém o destaque no mobile. */
.aj-opcao__nota{font-weight:400;color:var(--tinta-suave)}
.aj-rotulo--recuado{padding:0 0 var(--espaco-6) var(--espaco-14)}
@media (max-width:760px){.aj-lado-a-lado--formulario .aj-painel--principal.aj-painel--sempre-destacado{box-shadow:var(--sombra-painel)}}
[data-visao="mobile"] .aj-lado-a-lado--formulario .aj-painel--principal.aj-painel--sempre-destacado{box-shadow:var(--sombra-painel)}

/* Devolver e reabrir: painel e barra de ações de 680px no desktop; as opções em coluna num fieldset sem moldura. */
@media (min-width:1200px){.aj-painel--medio{max-width:680px}}
[data-visao="desktop"] .aj-painel--medio{max-width:680px}
[data-visao="tablet"] .aj-painel--medio,[data-visao="mobile"] .aj-painel--medio{max-width:none}
.aj-acoes--medio{max-width:680px}
.aj-conjunto{display:flex;flex-direction:column;gap:var(--espaco-8);min-width:0;margin:0;padding:0;border:0}
.aj-conjunto>legend{padding:0 0 var(--espaco-8)}

/* Ficha da atividade. Lado a lado com o painel estreito: o painel de detalhes com base de 280px e a coluna principal
   com o resto. */
@media (min-width:1200px){
  .aj-lado-a-lado--painel-estreito .aj-lado-a-lado__lista{flex:999 1 520px}
  .aj-lado-a-lado--painel-estreito .aj-lado-a-lado__painel{flex:1 1 280px}
}
[data-visao="desktop"] .aj-lado-a-lado--painel-estreito .aj-lado-a-lado__lista{flex:999 1 520px}
[data-visao="desktop"] .aj-lado-a-lado--painel-estreito .aj-lado-a-lado__painel{flex:1 1 280px}
[data-visao="tablet"] .aj-lado-a-lado--painel-estreito :is(.aj-lado-a-lado__lista,.aj-lado-a-lado__painel),
[data-visao="mobile"] .aj-lado-a-lado--painel-estreito :is(.aj-lado-a-lado__lista,.aj-lado-a-lado__painel){flex:0 0 auto}

/* Cabeçalho da ficha em coluna, com a faixa de ações embaixo: no desktop é o aj-painel--principal; no tablet, mais
   folgado e arredondado, com a sombra de repouso. */
@media (max-width:1199px){.aj-painel--cabecalho-com-faixa{gap:var(--espaco-14);padding:20px;border-radius:var(--raio-20);box-shadow:var(--sombra-1)}}
[data-visao="desktop"] .aj-painel--cabecalho-com-faixa{gap:var(--espaco-12);padding:var(--aj-cartao-padding);border-radius:var(--aj-raio-cartao);box-shadow:var(--sombra-painel)}
[data-visao="tablet"] .aj-painel--cabecalho-com-faixa,[data-visao="mobile"] .aj-painel--cabecalho-com-faixa{gap:var(--espaco-14);padding:20px;border-radius:var(--raio-20);box-shadow:var(--sombra-1)}

/* Bloco do título com a linha de situação (etiquetas e entrega) acima do nome. O tamanho do nome é o das fichas da 9D
   (26px no mobile). */
.aj-titulo-da-ficha--com-situacao{gap:var(--espaco-12)}
.aj-titulo-da-ficha__situacao{display:flex;flex-wrap:wrap;align-items:center;gap:var(--espaco-8) var(--espaco-14)}
.aj-titulo-da-ficha__situacao>.aj-etiqueta{--aj-fonte-etiqueta:12.5px}
.aj-titulo-da-ficha__entrega{font-size:13.5px;line-height:1.5;color:var(--tinta-suave);font-variant-numeric:tabular-nums}
@media (max-width:1199px){
  .aj-titulo-da-ficha--com-situacao{gap:var(--espaco-14)}
  .aj-titulo-da-ficha__situacao{gap:var(--espaco-6) var(--espaco-12)}
}
@media (max-width:760px){
  .aj-titulo-da-ficha--com-situacao{gap:var(--espaco-8)}
  .aj-titulo-da-ficha__situacao{gap:var(--espaco-6) var(--espaco-10)}
  .aj-titulo-da-ficha__entrega{font-size:13px}
}
[data-visao="desktop"] .aj-titulo-da-ficha--com-situacao{gap:var(--espaco-12)}
[data-visao="desktop"] .aj-titulo-da-ficha__situacao{gap:var(--espaco-8) var(--espaco-14)}
[data-visao="desktop"] .aj-titulo-da-ficha__entrega,[data-visao="tablet"] .aj-titulo-da-ficha__entrega{font-size:13.5px}
[data-visao="tablet"] .aj-titulo-da-ficha--com-situacao{gap:var(--espaco-14)}
[data-visao="tablet"] .aj-titulo-da-ficha__situacao{gap:var(--espaco-6) var(--espaco-12)}
[data-visao="mobile"] .aj-titulo-da-ficha--com-situacao{gap:var(--espaco-8)}
[data-visao="mobile"] .aj-titulo-da-ficha__situacao{gap:var(--espaco-6) var(--espaco-10)}
[data-visao="mobile"] .aj-titulo-da-ficha__entrega{font-size:13px}

/* Faixa de ações do cabeçalho da ficha, separada do título por uma borda: o divisor entre as ações principais e as de
   cadastro, o Excluir empurrado para a direita (no desktop com o ícone de 16px). No tablet, primário e secundário com
   raio 12 e o Excluir só com o ícone de 20px. */
.aj-painel__botoes--faixa{align-items:center;padding-top:var(--espaco-12);border-top:1px solid var(--borda-suave)}
.aj-painel__divisor{flex:0 0 auto;width:1px;height:24px;margin:0 var(--espaco-6);background:var(--borda-opcao)}
.aj-botao--ao-fim{margin-left:auto}
.aj-botao--com-icone-menor{gap:var(--espaco-6);padding-left:var(--espaco-12)}
.aj-botao--com-icone-menor svg{width:16px;height:16px}
@media (max-width:1199px){
  .aj-painel__botoes--faixa{padding-top:var(--espaco-14)}
  .aj-painel__botoes--faixa :is(.aj-botao--primario,.aj-botao--secundario){border-radius:var(--raio-12)}
  .aj-painel__botoes--faixa .aj-botao--icone svg{width:20px;height:20px;stroke-width:1.75}
}
[data-visao="desktop"] .aj-painel__botoes--faixa{padding-top:var(--espaco-12)}
[data-visao="desktop"] .aj-painel__botoes--faixa :is(.aj-botao--primario,.aj-botao--secundario){border-radius:var(--aj-raio-botao)}
[data-visao="desktop"] .aj-painel__botoes--faixa .aj-botao--icone svg{width:18px;height:18px;stroke-width:2}
[data-visao="tablet"] .aj-painel__botoes--faixa,[data-visao="mobile"] .aj-painel__botoes--faixa{padding-top:var(--espaco-14)}
[data-visao="tablet"] .aj-painel__botoes--faixa :is(.aj-botao--primario,.aj-botao--secundario),[data-visao="mobile"] .aj-painel__botoes--faixa :is(.aj-botao--primario,.aj-botao--secundario){border-radius:var(--raio-12)}
[data-visao="tablet"] .aj-painel__botoes--faixa .aj-botao--icone svg,[data-visao="mobile"] .aj-painel__botoes--faixa .aj-botao--icone svg{width:20px;height:20px;stroke-width:1.75}

/* Painel da ficha da atividade: no desktop, 16px de respiro e raio 14; no tablet e no mobile, sem sombra e com o título
   de 15,5px (tablet) e 15px (mobile). */
@media (min-width:1200px){.aj-painel--ficha{padding:var(--espaco-16);border-radius:var(--raio-14)}}
@media (max-width:1199px){.aj-painel--ficha{box-shadow:none}.aj-painel--ficha h2{font-size:15.5px}}
@media (max-width:760px){.aj-painel--ficha h2{font-size:15px}}
[data-visao="desktop"] .aj-painel--ficha{padding:var(--espaco-16);border-radius:var(--raio-14);box-shadow:var(--sombra-1)}
[data-visao="desktop"] .aj-painel--ficha h2{font-size:16px}
[data-visao="tablet"] .aj-painel--ficha,[data-visao="mobile"] .aj-painel--ficha{padding:var(--aj-cartao-padding);border-radius:var(--aj-raio-cartao);box-shadow:none}
[data-visao="tablet"] .aj-painel--ficha h2{font-size:15.5px}
[data-visao="mobile"] .aj-painel--ficha h2{font-size:15px}

/* Descrição: o texto mantém as quebras; no tablet e no mobile o título fica mais perto; no mobile o texto tem 14,5px. */
.aj-painel--descricao p{white-space:pre-wrap}
@media (max-width:1199px){.aj-painel--descricao{gap:var(--espaco-6)}}
@media (max-width:760px){.aj-painel--descricao p{font-size:14.5px}}
[data-visao="desktop"] .aj-painel--descricao{gap:var(--espaco-12)}
[data-visao="desktop"] .aj-painel--descricao p,[data-visao="tablet"] .aj-painel--descricao p{font-size:15px}
[data-visao="tablet"] .aj-painel--descricao,[data-visao="mobile"] .aj-painel--descricao{gap:var(--espaco-6)}
[data-visao="mobile"] .aj-painel--descricao p{font-size:14.5px}

/* Detalhes (Dados no tablet e no mobile): título a 6px da lista no desktop e a última linha sem respiro embaixo; no
   tablet e no mobile, o painel mais justo em cima e embaixo, o título com respiro próprio e as linhas mais altas. */
.aj-painel--detalhes{gap:var(--espaco-6)}
.aj-painel--detalhes .aj-detalhes>div:last-child{padding-bottom:0}
@media (max-width:1199px){
  .aj-painel--detalhes{gap:0;padding:var(--espaco-8) var(--espaco-16)}
  .aj-painel--detalhes h2{padding:var(--espaco-8) 0 var(--espaco-4)}
  .aj-painel--detalhes .aj-detalhes>div{gap:var(--espaco-12);padding:var(--espaco-8) 0}
  .aj-painel--detalhes .aj-detalhes>div:last-child{padding-bottom:var(--espaco-4)}
}
@media (max-width:760px){
  .aj-painel--detalhes{padding:var(--espaco-6) var(--espaco-14)}
  .aj-painel--detalhes .aj-detalhes>div,.aj-painel--detalhes .aj-detalhes>div:last-child{padding:var(--espaco-10) 0}
}
[data-visao="desktop"] .aj-painel--detalhes{gap:var(--espaco-6);padding:var(--espaco-16)}
[data-visao="desktop"] .aj-painel--detalhes h2{padding:0}
[data-visao="desktop"] .aj-painel--detalhes .aj-detalhes>div{gap:var(--espaco-10);padding:7px 0}
[data-visao="desktop"] .aj-painel--detalhes .aj-detalhes>div:last-child{padding-bottom:0}
[data-visao="tablet"] .aj-painel--detalhes{gap:0;padding:var(--espaco-8) var(--espaco-16)}
[data-visao="tablet"] .aj-painel--detalhes h2,[data-visao="mobile"] .aj-painel--detalhes h2{padding:var(--espaco-8) 0 var(--espaco-4)}
[data-visao="tablet"] .aj-painel--detalhes .aj-detalhes>div{gap:var(--espaco-12);padding:var(--espaco-8) 0}
[data-visao="tablet"] .aj-painel--detalhes .aj-detalhes>div:last-child{padding-bottom:var(--espaco-4)}
[data-visao="mobile"] .aj-painel--detalhes{gap:0;padding:var(--espaco-6) var(--espaco-14)}
[data-visao="mobile"] .aj-painel--detalhes .aj-detalhes>div,[data-visao="mobile"] .aj-painel--detalhes .aj-detalhes>div:last-child{gap:var(--espaco-12);padding:var(--espaco-10) 0}

/* Vãos dos outros painéis da ficha. Responsáveis: 14 no desktop, 8 no tablet e no mobile. Histórico: 14. Comentários:
   10 (12 no tablet). Documentos: 14 (8 no tablet). O mobile sem quadro segue o desktop. */
.aj-painel--responsaveis,.aj-painel--historico,.aj-painel--documentos{gap:var(--espaco-14)}
.aj-painel--comentarios{gap:var(--espaco-10)}
@media (max-width:1199px){.aj-painel--responsaveis{gap:var(--espaco-8)}}
@media (min-width:761px) and (max-width:1199px){.aj-painel--documentos{gap:var(--espaco-8)}.aj-painel--comentarios{gap:var(--espaco-12)}}
[data-visao="desktop"] .aj-painel--responsaveis{gap:var(--espaco-14)}
[data-visao="tablet"] .aj-painel--responsaveis,[data-visao="mobile"] .aj-painel--responsaveis{gap:var(--espaco-8)}
[data-visao="desktop"] .aj-painel--documentos,[data-visao="mobile"] .aj-painel--documentos{gap:var(--espaco-14)}
[data-visao="tablet"] .aj-painel--documentos{gap:var(--espaco-8)}
[data-visao="desktop"] .aj-painel--comentarios,[data-visao="mobile"] .aj-painel--comentarios{gap:var(--espaco-10)}
[data-visao="tablet"] .aj-painel--comentarios{gap:var(--espaco-12)}

/* Responsáveis da atividade: um cartão por pessoa, com avatar, nome, estado da parte e direcionamento. Lista no desktop
   e no mobile, grade de duas colunas no tablet. */
.aj-responsaveis{display:flex;flex-direction:column;gap:var(--espaco-8);margin:0;padding:0;list-style:none}
.aj-responsavel{display:flex;flex-wrap:wrap;align-items:flex-start;gap:var(--espaco-12) var(--espaco-14);padding:var(--espaco-10);border-radius:var(--raio-14);background:var(--superficie-suave)}
.aj-responsavel>.aj-avatar{width:36px;height:36px;font-size:12.5px}
.aj-responsavel strong{font-weight:600}
.aj-responsavel__texto{flex:1 1 260px;min-width:0;display:flex;flex-direction:column;gap:var(--espaco-2)}
.aj-responsavel__cabeca{display:flex;flex-wrap:wrap;align-items:center;gap:var(--espaco-4) var(--espaco-10)}
.aj-responsavel__estado{display:inline-flex;align-items:center;gap:5px;font-size:12.5px;font-weight:600;color:var(--sucesso);font-variant-numeric:tabular-nums}
.aj-responsavel__estado svg{flex:0 0 auto;width:13px;height:13px;fill:none;stroke:currentColor;stroke-width:3;stroke-linecap:round;stroke-linejoin:round}
.aj-responsavel__estado--pendente{color:var(--tinta-apagada)}
.aj-responsavel__direcionamento{font-size:14px;color:var(--tinta-secundaria);white-space:pre-wrap;overflow-wrap:anywhere}
@media (max-width:1199px){
  .aj-responsaveis{display:grid;grid-template-columns:repeat(2,minmax(0,1fr))}
  .aj-responsavel{flex-wrap:nowrap;gap:var(--espaco-12);padding:var(--espaco-12)}
  .aj-responsavel>.aj-avatar{font-size:12px}
  .aj-responsavel__texto{flex:0 1 auto;gap:1px}
  .aj-responsavel__cabeca{flex-direction:column;align-items:flex-start;gap:1px}
  .aj-responsavel__estado svg{display:none}
  .aj-responsavel__direcionamento{font-size:13.5px}
}
@media (max-width:760px){
  .aj-responsaveis{display:flex}
  .aj-responsavel{padding:var(--espaco-10);border-radius:var(--raio-12)}
  .aj-responsavel>.aj-avatar{width:34px;height:34px}
  .aj-responsavel__estado svg{display:block;width:12px;height:12px}
}
[data-visao="desktop"] .aj-responsaveis{display:flex}
[data-visao="desktop"] .aj-responsavel{flex-wrap:wrap;gap:var(--espaco-12) var(--espaco-14);padding:var(--espaco-10);border-radius:var(--raio-14)}
[data-visao="desktop"] .aj-responsavel>.aj-avatar{width:36px;height:36px;font-size:12.5px}
[data-visao="desktop"] .aj-responsavel__texto{flex:1 1 260px;gap:var(--espaco-2)}
[data-visao="desktop"] .aj-responsavel__cabeca{flex-direction:row;align-items:center;gap:var(--espaco-4) var(--espaco-10)}
[data-visao="desktop"] .aj-responsavel__estado svg{display:block;width:13px;height:13px}
[data-visao="desktop"] .aj-responsavel__direcionamento{font-size:14px}
[data-visao="tablet"] .aj-responsaveis{display:grid;grid-template-columns:repeat(2,minmax(0,1fr))}
[data-visao="tablet"] .aj-responsavel{flex-wrap:nowrap;gap:var(--espaco-12);padding:var(--espaco-12);border-radius:var(--raio-14)}
[data-visao="tablet"] .aj-responsavel>.aj-avatar{width:36px;height:36px;font-size:12px}
[data-visao="tablet"] .aj-responsavel__estado svg{display:none}
[data-visao="mobile"] .aj-responsaveis{display:flex}
[data-visao="mobile"] .aj-responsavel{flex-wrap:nowrap;gap:var(--espaco-12);padding:var(--espaco-10);border-radius:var(--raio-12)}
[data-visao="mobile"] .aj-responsavel>.aj-avatar{width:34px;height:34px;font-size:12px}
[data-visao="mobile"] .aj-responsavel__estado svg{display:block;width:12px;height:12px}
[data-visao="tablet"] .aj-responsavel__texto,[data-visao="mobile"] .aj-responsavel__texto{flex:0 1 auto;gap:1px}
[data-visao="tablet"] .aj-responsavel__cabeca,[data-visao="mobile"] .aj-responsavel__cabeca{flex-direction:column;align-items:flex-start;gap:1px}
[data-visao="tablet"] .aj-responsavel__direcionamento,[data-visao="mobile"] .aj-responsavel__direcionamento{font-size:13.5px}

/* Histórico da atividade em linha do tempo: um ponto por mudança, na cor da situação nova, ligado ao seguinte por um
   traço; o último (a situação atual) cheio. A situação também está escrita no texto. */
.aj-historico{display:flex;flex-direction:column;margin:0;padding:0;list-style:none}
.aj-historico__item{display:flex;gap:var(--espaco-14)}
.aj-historico__marca{flex:0 0 auto;display:flex;flex-direction:column;align-items:center;padding-top:var(--espaco-6)}
.aj-historico__ponto{flex:0 0 auto;width:10px;height:10px;border:2px solid currentColor;border-radius:var(--raio-pilula);background:var(--superficie);color:var(--tinta-icone)}
.aj-historico__ponto--direcionada{color:var(--situacao-direcionada-texto)}
.aj-historico__ponto--feita{color:var(--situacao-feita-texto)}
.aj-historico__ponto--correcao{color:var(--situacao-correcao-texto)}
.aj-historico__ponto--corrigida{color:var(--situacao-corrigida-texto)}
.aj-historico__ponto--concluida{color:var(--verde)}
.aj-historico__ponto--reaberta{color:var(--situacao-reaberta-texto)}
.aj-historico__ponto--atual{border:0;background:currentColor}
.aj-historico__linha{flex:1 1 auto;width:2px;margin-top:var(--espaco-4);background:var(--borda)}
.aj-historico__item:last-child .aj-historico__linha{display:none}
.aj-historico__texto{display:flex;flex-direction:column;gap:1px;min-width:0;padding-bottom:var(--espaco-10)}
.aj-historico__item:last-child .aj-historico__texto{padding-bottom:0}
.aj-historico__mudanca{font-weight:600}
.aj-historico__autor{font-size:13.5px;color:var(--tinta-suave);font-variant-numeric:tabular-nums}
.aj-historico__motivo{margin-top:var(--espaco-2);font-size:14px;color:var(--tinta-secundaria);white-space:pre-wrap;overflow-wrap:anywhere}

/* Comentários em balões: avatar, balão com nome, data e texto. No tablet, avatar menor, balão sem vão interno e data
   menor. */
.aj-comentarios{display:flex;flex-direction:column;gap:var(--espaco-10);margin:0;padding:0;list-style:none}
.aj-comentario{display:flex;gap:var(--espaco-12)}
.aj-comentario>.aj-avatar{width:36px;height:36px;font-size:12.5px}
.aj-comentario strong{font-weight:600}
.aj-comentario__balao{flex:1 1 auto;min-width:0;display:flex;flex-direction:column;gap:var(--espaco-6);padding:9px var(--espaco-12);border-radius:4px var(--raio-14) var(--raio-14) var(--raio-14);background:var(--superficie-suave)}
.aj-comentario__cabeca{display:flex;flex-wrap:wrap;align-items:baseline;gap:var(--espaco-4) var(--espaco-10)}
.aj-comentario__data{font-size:13px;color:var(--tinta-apagada);font-variant-numeric:tabular-nums}
.aj-comentario__texto{max-width:68ch;white-space:pre-wrap;color:var(--tinta-corpo);overflow-wrap:anywhere}
@media (min-width:761px) and (max-width:1199px){
  .aj-comentarios{gap:var(--espaco-12)}
  .aj-comentario{gap:var(--espaco-10)}
  .aj-comentario>.aj-avatar{width:34px;height:34px;font-size:12px}
  .aj-comentario__balao{gap:0;padding:var(--espaco-10) var(--espaco-12)}
  .aj-comentario__cabeca{flex-wrap:nowrap;gap:var(--espaco-10)}
  .aj-comentario__data{font-size:12.5px}
}
[data-visao="tablet"] .aj-comentarios{gap:var(--espaco-12)}
[data-visao="tablet"] .aj-comentario{gap:var(--espaco-10)}
[data-visao="tablet"] .aj-comentario>.aj-avatar{width:34px;height:34px;font-size:12px}
[data-visao="tablet"] .aj-comentario__balao{gap:0;padding:var(--espaco-10) var(--espaco-12)}
[data-visao="tablet"] .aj-comentario__cabeca{flex-wrap:nowrap;gap:var(--espaco-10)}
[data-visao="tablet"] .aj-comentario__data{font-size:12.5px}
[data-visao="desktop"] .aj-comentarios,[data-visao="mobile"] .aj-comentarios{gap:var(--espaco-10)}
[data-visao="desktop"] .aj-comentario,[data-visao="mobile"] .aj-comentario{gap:var(--espaco-12)}
[data-visao="desktop"] .aj-comentario>.aj-avatar,[data-visao="mobile"] .aj-comentario>.aj-avatar{width:36px;height:36px;font-size:12.5px}
[data-visao="desktop"] .aj-comentario__balao,[data-visao="mobile"] .aj-comentario__balao{gap:var(--espaco-6);padding:9px var(--espaco-12)}
[data-visao="desktop"] .aj-comentario__cabeca,[data-visao="mobile"] .aj-comentario__cabeca{flex-wrap:wrap;gap:var(--espaco-4) var(--espaco-10)}
[data-visao="desktop"] .aj-comentario__data,[data-visao="mobile"] .aj-comentario__data{font-size:13px}

/* Novo comentário no desktop e no mobile: abaixo dos balões, separado por uma borda, com o botão à direita. */
.aj-comentarios__novo{display:flex;flex-direction:column;gap:var(--espaco-10);padding-top:var(--espaco-16);border-top:1px solid var(--borda-suave)}
.aj-comentarios__novo>.aj-botao{align-self:flex-end;padding:0 20px}

/* Novo comentário no tablet (dentro de aj-campo-com-botao): o campo com mais respiro e o botão de enviar quadrado de
   48px. */
textarea.aj-campo--folgado{padding:var(--espaco-10) var(--espaco-14)}
.aj-campo-com-botao>.aj-botao--enviar{width:48px;min-height:48px;padding:0;border-radius:var(--raio-14)}
.aj-botao--enviar svg{width:20px;height:20px}

/* Documentos da atividade: ladrilho com o ícone do tipo, nome (o link que baixa), tamanho, autor e data, e as ações.
   Lista no desktop e no mobile (no mobile, o "⋯" fica na mesma linha do nome); no tablet, grade de duas colunas com o
   nome cortado e sem a data. */
.aj-documentos{display:flex;flex-direction:column;gap:var(--espaco-8);margin:0;padding:0;list-style:none}
.aj-documento{display:flex;flex-wrap:wrap;align-items:center;gap:var(--espaco-8) var(--espaco-14);min-width:0;padding:var(--espaco-10) var(--espaco-8) var(--espaco-10) var(--espaco-12);border:1px solid var(--borda-suave);border-radius:var(--raio-14);color:var(--tinta)}
.aj-documento__icone{flex:0 0 auto;display:inline-flex;align-items:center;justify-content:center;width:40px;height:40px;border-radius:var(--raio-10);background:var(--situacao-criada-fundo);color:var(--situacao-criada-texto)}
.aj-documento__icone svg{width:20px;height:20px;fill:none;stroke:currentColor;stroke-width:1.75;stroke-linecap:round;stroke-linejoin:round}
.aj-documento__icone--texto{background:var(--situacao-direcionada-fundo);color:var(--situacao-direcionada-texto)}
.aj-documento__icone--planilha{background:var(--situacao-feita-fundo);color:var(--situacao-feita-texto)}
.aj-documento__texto{flex:1 1 220px;min-width:0;display:flex;flex-direction:column;gap:1px}
.aj-documento__nome{font-weight:600;text-decoration:none;overflow-wrap:anywhere}
:where(.aj-documento a){color:var(--verde)}
:where(.aj-documento a:hover){color:var(--tinta)}
.aj-documento__meta{font-size:13px;color:var(--tinta-suave);font-variant-numeric:tabular-nums}
.aj-documento__acoes{display:flex;gap:var(--espaco-2)}
.aj-documento__acoes>.aj-botao{padding:0 var(--espaco-12);font-size:14px}
@media (min-width:761px) and (max-width:1199px){
  .aj-documentos{display:grid;grid-template-columns:repeat(2,minmax(0,1fr))}
  .aj-documento{flex-wrap:nowrap;gap:var(--espaco-12);padding:var(--espaco-10) var(--espaco-12)}
  .aj-documento__texto{flex:1 1 auto;gap:0}
  .aj-documento__nome{overflow:hidden;text-overflow:ellipsis;white-space:nowrap;overflow-wrap:normal}
  .aj-documento a.aj-documento__nome{color:var(--tinta)}
  .aj-documento__meta{font-size:12.5px;font-variant-numeric:normal}
}
@media (max-width:760px){.aj-documento{flex-wrap:nowrap}}
[data-visao="tablet"] .aj-documentos{display:grid;grid-template-columns:repeat(2,minmax(0,1fr))}
[data-visao="tablet"] .aj-documento{flex-wrap:nowrap;gap:var(--espaco-12);padding:var(--espaco-10) var(--espaco-12)}
[data-visao="tablet"] .aj-documento__texto{flex:1 1 auto;gap:0}
[data-visao="tablet"] .aj-documento__nome{overflow:hidden;text-overflow:ellipsis;white-space:nowrap;overflow-wrap:normal}
[data-visao="tablet"] .aj-documento a.aj-documento__nome{color:var(--tinta)}
[data-visao="tablet"] .aj-documento__meta{font-size:12.5px;font-variant-numeric:normal}
[data-visao="desktop"] .aj-documentos,[data-visao="mobile"] .aj-documentos{display:flex}
[data-visao="desktop"] .aj-documento,[data-visao="mobile"] .aj-documento{gap:var(--espaco-8) var(--espaco-14);padding:var(--espaco-10) var(--espaco-8) var(--espaco-10) var(--espaco-12)}
[data-visao="desktop"] .aj-documento{flex-wrap:wrap}
[data-visao="mobile"] .aj-documento{flex-wrap:nowrap}
[data-visao="desktop"] .aj-documento__texto,[data-visao="mobile"] .aj-documento__texto{flex:1 1 220px;gap:1px}
[data-visao="desktop"] .aj-documento__nome,[data-visao="mobile"] .aj-documento__nome{overflow:visible;text-overflow:clip;white-space:normal;overflow-wrap:anywhere}
[data-visao="desktop"] .aj-documento a.aj-documento__nome,[data-visao="mobile"] .aj-documento a.aj-documento__nome{color:var(--verde)}
[data-visao="desktop"] .aj-documento__meta,[data-visao="mobile"] .aj-documento__meta{font-size:13px;font-variant-numeric:tabular-nums}

/* Barra de voltar com uma ação à direita (o "Mais ações" da ficha da atividade): o botão do menu mede o alvo de toque,
   como o voltar. */
.aj-barra-superior--voltar>.aj-menu-acoes>.aj-botao{width:var(--alvo-toque);min-height:var(--alvo-toque)}
.aj-barra-superior--voltar>.aj-menu-acoes>.aj-botao svg{width:22px;height:22px}
```

Confira por roteiro (salve-o como `conferir-bloco-9e.ps1` em UTF-8 **com** BOM, porque o Windows PowerShell 5.1 lê arquivo sem BOM como ANSI, e rode com `-Bundle` apontando para o `bundle.css` já emendado e `-Tokens` para o `tokens.css` do design system):

```powershell
param([string]$Bundle, [string]$Tokens)
$utf8 = New-Object Text.UTF8Encoding $false
$todo = [IO.File]::ReadAllText($Bundle, $utf8)
$t = [IO.File]::ReadAllText($Tokens, $utf8)
$marca = '/* ---------- Atividades (fase 9E) ---------- */'
$i = $todo.IndexOf($marca)
if ($i -lt 0) { throw 'Marca do bloco da fase 9E não encontrada.' }
$antes = $todo.Substring(0, $i)
$b = $todo.Substring($i)
$semComentarios = [regex]::Replace($b, '/\*.*?\*/', '', 'Singleline')
$antesSemComentarios = [regex]::Replace($antes, '/\*.*?\*/', '', 'Singleline')

# 1. Chaves do arquivo inteiro e do bloco
'Chaves do arquivo: { ' + ([regex]::Matches($todo, '\{')).Count + ', } ' + ([regex]::Matches($todo, '\}')).Count
'Chaves do bloco: { ' + ([regex]::Matches($b, '\{')).Count + ', } ' + ([regex]::Matches($b, '\}')).Count

# 2. Toda var(--…) do bloco definida em tokens.css ou no bundle.css
$definidas = @{}
foreach ($m in [regex]::Matches($t + $todo, '(--[a-z0-9-]+)\s*:')) { $definidas[$m.Groups[1].Value] = $true }
$usadas = [regex]::Matches($semComentarios, 'var\((--[a-z0-9-]+)\)') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
$faltam = @($usadas | Where-Object { -not $definidas.ContainsKey($_) })
'Variáveis usadas: ' + @($usadas).Count + '; sem definição: ' + $(if ($faltam.Count) { $faltam -join ', ' } else { 'nenhuma' })

# 3. Classes novas e classes existentes que o bloco só usa como contexto
$classes = [regex]::Matches($semComentarios, '\.(aj-[a-z0-9_-]+)') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
$existentes = [regex]::Matches($antesSemComentarios, '\.(aj-[a-z0-9_-]+)') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
$novas = @($classes | Where-Object { $existentes -notcontains $_ })
$contexto = @($classes | Where-Object { $existentes -contains $_ })
'Classes novas (' + $novas.Count + '): ' + ($novas -join ', ')
'Classes existentes usadas como contexto (' + $contexto.Count + '): ' + ($contexto -join ', ')

# 4. Os nomes do contrato existem como classe nova
$contrato = 'aj-desktop-mobile','aj-subtitulo__alerta','aj-campo-icone--filtro','aj-opcao--solta','aj-linha-filtros--busca','aj-barra-superior__busca','aj-chips--barra','aj-paginacao--com-setas','aj-paginacao__anterior','aj-paginacao__proxima','aj-tabela--larga','aj-tabela__vazia','aj-etiquetas','aj-etiqueta--com-icone','aj-etiqueta--com-ponto','aj-cartao--atividade','aj-cartao__topo','aj-cartao__vazio','aj-vinculo__escolhido--detalhado','aj-vinculo__texto','aj-vinculo__titulo','aj-opcao__nota','aj-rotulo--recuado','aj-painel--sempre-destacado','aj-painel--medio','aj-acoes--medio','aj-conjunto','aj-lado-a-lado--painel-estreito','aj-painel__botoes--faixa','aj-painel__divisor','aj-botao--ao-fim','aj-responsaveis','aj-historico','aj-comentarios','aj-comentario','aj-comentarios__novo','aj-documentos','aj-documento','aj-documento__icone--texto','aj-documento__icone--planilha'
$ausentes = @($contrato | Where-Object { $novas -notcontains $_ })
'Nomes do contrato ausentes: ' + $(if ($ausentes.Count) { $ausentes -join ', ' } else { 'nenhum' })

# 5. O que as decisões deixam de fora
$proibidos = @('aj-aviso--com-selo','aj-aviso__selo','aj-acoes--alta' | Where-Object { $classes -contains $_ })
'Classes que não entram, achadas: ' + $(if ($proibidos.Count) { $proibidos -join ', ' } else { 'nenhuma' })
'letter-spacing no bloco: ' + ($semComentarios -match 'letter-spacing')
'Regra sobre o h1 da barra de voltar, o aj-voltar, o respiro do conteúdo ou a aj-grade: ' + ($semComentarios -match 'aj-barra-superior--voltar>h1|\.aj-voltar\b|--aj-pagina-padding|\.aj-grade\b|\.aj-aviso\b')
'Sombra do primário mexida: ' + ($semComentarios -match 'botao--primario[^{]*\{[^}]*box-shadow')
```

Expected: as chaves do arquivo e as do bloco em número igual (no bloco, 294 e 294); "sem definição: nenhuma"; "Classes novas (84)" com os nomes da lista acima; "Nomes do contrato ausentes: nenhum"; "Classes que não entram, achadas: nenhuma"; as três últimas linhas `False`. Confira também à mão a ordem das regras que empatam em especificidade (a de baixo vence):

- `.aj-barra-filtros>.aj-campo-icone--filtro` (0,2,0) vem depois de `.aj-barra-filtros>.aj-campo-icone` (9D); `.aj-campo-icone--filtro input` depois de `.aj-campo-icone input`.
- `.aj-opcao--solta` depois de `.aj-opcao`; `.aj-opcao--solta:has(input:checked)` (0,2,1) depois de `.aj-opcao:has(input:checked)` (0,2,1). A opção solta nunca leva também `aj-opcao--filtro`: `.aj-opcao.aj-opcao--filtro` (0,2,0, ajuste da 9D) devolveria os 42px.
- `.aj-linha-filtros--busca .aj-botao` depois de `.aj-linha-filtros .aj-botao` (0,2,0); ela vence `.aj-botao--com-icone` (0,1,0). É seletor de descendente porque o botão do tablet vai num invólucro de visão.
- `.aj-paginacao--com-setas .aj-subtitulo` depois de `.aj-paginacao .aj-subtitulo` (0,2,0); `.aj-paginacao__anterior` e `__proxima` depois de `.aj-botao` (0,1,0).
- `.aj-etiqueta--com-ponto::before` (0,1,1) depois de `.aj-etiqueta--atrasada::before` (0,1,1).
- `.aj-vinculo__escolhido--detalhado` depois de `.aj-vinculo__escolhido`; `.aj-titulo-da-ficha--com-situacao` depois de `.aj-titulo-da-ficha` (9D).
- `.aj-painel--cabecalho-com-faixa` (consulta do tablet) depois de `.aj-painel--principal`.
- `.aj-botao--com-icone-menor` depois de `.aj-botao--perigo`; `.aj-botao--com-icone-menor svg` depois de `.aj-botao svg`.
- `.aj-painel--ficha h2` depois de `.aj-painel h2`; `.aj-painel--descricao p` depois de `.aj-painel p`; dentro do bloco, `[data-visao="tablet"] .aj-painel--detalhes` depois de `[data-visao="tablet"] .aj-painel--ficha` (os dois 0,2,0: o painel de detalhes fica com `8px 16px`).
- `.aj-painel--detalhes .aj-detalhes>div:last-child` (0,3,1): a regra solta, depois a da consulta até 1199px, depois a da consulta até 760px.
- `textarea.aj-campo--folgado` depois de `textarea.aj-campo` (0,1,1); `.aj-campo-com-botao>.aj-botao--enviar` depois de `.aj-campo-com-botao>.aj-botao` (0,2,0).
- `--aj-ver-desktop-mobile`: `:root`, depois a consulta até 1199px (`none`), depois a consulta até 760px (`contents`).
- Vencem por especificidade, sem depender da ordem: `.aj-subtitulo strong.aj-subtitulo__alerta` (0,2,1) sobre `.aj-subtitulo strong` (0,1,1); `.aj-paginacao--com-setas .aj-botao:disabled` (0,3,0) sobre `.aj-botao:disabled` (0,2,0); `.aj-lado-a-lado--formulario .aj-painel--principal.aj-painel--sempre-destacado` (0,3,0; 0,4,0 com `[data-visao]`) sobre o ajuste da 9D (0,2,0; 0,3,0); `.aj-barra-superior--voltar>.aj-menu-acoes>.aj-botao` (0,3,0) sobre `.aj-botao--icone`; `.aj-painel__botoes--faixa :is(.aj-botao--primario,.aj-botao--secundario)` (0,2,0) sobre `.aj-botao`.

- [ ] **Step 3: README e prévia**

Componentes novos, cada um com `README.md` (primeira frase é o resumo; depois o que cada classe faz, as regras de uso e o que quem usa fornece) e `preview.html` (linha 1 `<!-- @dsCard group="…" height=… -->`, nos grupos que já existem: Estrutura, Conteúdo, Formulário, Filtros, Ações, Retorno; o corpo no molde da prévia de `Abas`, com o `data-visao` de cada visão mostrada):

| Pasta | Grupo | O que a prévia mostra | Regras de uso que o README registra |
|---|---|---|---|
| `CabecalhoDaAtividade` | Estrutura | Desktop: o painel com a etiqueta "Feita", "Entrega em 09/10/2026 · Trabalhista", o nome "Minuta de acordo trabalhista" e a faixa "Aprovar", "Devolver", o divisor, "Editar", "Responsáveis", "Trocar revisor" e "Excluir" à direita. Tablet: o mesmo, sem o divisor e com o "Excluir" só de ícone. Mobile: o bloco de título com "Entrega 09/10/2026 · Trabalhista" e o nome em `h2` | Ficha da atividade. Desktop e tablet: `aj-voltar` acima e `header.aj-painel.aj-painel--principal.aj-painel--cabecalho-com-faixa` com `aj-titulo-da-ficha aj-titulo-da-ficha--com-situacao` (`aj-titulo-da-ficha__situacao` com as etiquetas, a Atrasada ao lado da situação, e `aj-titulo-da-ficha__entrega`; o `h1.aj-titulo`) e a faixa `aj-painel__botoes aj-painel__botoes--faixa`. Ordem da faixa: a ação primária (Concluir minha parte, Aprovar ou Reabrir), Devolver, o divisor `aj-painel__divisor` (`aria-hidden`, só com ação dos dois lados e só no desktop), Editar, Responsáveis, Trocar revisor (fantasmas), Excluir (`aj-botao--perigo aj-botao--ao-fim`; no desktop com texto e o ícone de 16px, `aj-botao--com-icone-menor`; no tablet só o ícone, `aria-label="Excluir atividade"`). Sem nenhuma ação, sem faixa. Mobile: a barra de voltar com o assunto ("Atividade") e o "Mais ações" (Menu de ações) à direita; o bloco de título fora de cartão, com o nome em `h2`; a ação primária e Devolver na Barra de ações fixa. Um `h1` por visão. Quem usa fornece a situação, a entrega, a área, o nome, o voltar e as ações |
| `ResponsaveisDaAtividade` | Conteúdo | O painel "Responsáveis" com "Beatriz Lins" (BL, azul), "Concluída em 07/10/2026 16:42" e o direcionamento, e "Otávio Brandão" (OB, lilás), "Pendente": em lista no desktop e em duas colunas, com a data curta, no tablet | `section.aj-painel.aj-painel--ficha.aj-painel--responsaveis` com `ul.aj-responsaveis` de `li.aj-responsavel`: o avatar `aria-hidden` com a sigla e a cor, `aj-responsavel__texto` com `aj-responsavel__cabeca` (o nome em `strong` e `aj-responsavel__estado`) e `aj-responsavel__direcionamento`. Concluída: o ícone `confirmar` e o texto (no tablet o ícone some); pendente: `aj-responsavel__estado--pendente`, só o texto. A data é completa no desktop e curta ("07/10 16:42") no tablet e no mobile: dois `span` em invólucros de visão. O direcionamento só aparece quando o servidor o mostra e mantém as quebras. Vazio: "Nenhum responsável." |
| `Historico` | Conteúdo | O painel "Histórico" com "Criada" (Marina Couto · 01/10/2026 10:02), "Criada → Direcionada", "Feita → Para correção" com o motivo na terceira linha e "Para correção → Corrigida" com o ponto cheio | `section.aj-painel.aj-painel--ficha.aj-painel--historico` com `ol.aj-historico` de `li.aj-historico__item`: a marca `aj-historico__marca` (`aria-hidden`) com o ponto e o traço, e `aj-historico__texto` com `aj-historico__mudanca`, `aj-historico__autor` ("Nome · dd/MM/yyyy HH:mm") e, quando houver, `p.aj-historico__motivo`. Um item por registro, na ordem do servidor; o primeiro só com a situação nova, os outros "Anterior → Nova". A cor do ponto é a da situação nova (`aj-historico__ponto--direcionada`, `--feita`, `--correcao`, `--corrigida`, `--concluida`, `--reaberta`; Criada sem modificador); o último leva `--atual` (cheio) e não tem traço. A situação está escrita no texto: a cor nunca é o único sinal. Vazio: "Nenhum registro." |
| `Comentarios` | Conteúdo | Desktop: dois balões (BL azul; MC no dourado padrão) e o novo comentário com o rótulo "Novo comentário", a caixa de três linhas com "Escreva um comentário para a equipe" e "Comentar". Tablet: os balões com a data curta e o novo comentário em linha, com o rótulo só para leitor, a caixa de duas linhas ("Escreva um comentário") e o botão de ícone "Enviar comentário" | `section.aj-painel.aj-painel--ficha.aj-painel--comentarios` com `ul.aj-comentarios` de `li.aj-comentario`: o avatar e `aj-comentario__balao` com `aj-comentario__cabeca` (o nome em `strong` e `aj-comentario__data`) e `aj-comentario__texto` (mantém as quebras). Data completa no desktop e no mobile, curta no tablet (dois `span` em invólucros de visão). O formulário do desktop e do mobile vai num invólucro `aj-desktop-mobile` (`aj-comentarios__novo`: `label.aj-rotulo`, `textarea.aj-campo rows="3"`, botão secundário à direita); o do tablet num `aj-so-tablet` (`aj-campo-com-botao`, `textarea.aj-campo.aj-campo--folgado rows="2"`, `aj-botao aj-botao--primario aj-botao--icone aj-botao--enviar` com `aria-label="Enviar comentário"`). Os dois ligados ao mesmo texto |
| `ListaDeDocumentos` | Conteúdo | Desktop: "minuta-acordo-v2.docx" (ícone de texto, "84 KB · Beatriz Lins · 07/10/2026 16:40") e "calculo-verbas.xlsx" (ícone de planilha) com "Renomear" e "Excluir". Tablet: as duas em duas colunas, o nome cortado com reticências, a meta sem a data e o "⋯" | `section.aj-painel.aj-painel--ficha.aj-painel--documentos` com `aj-painel__cabecalho` ("Documentos" e o botão secundário "Enviar documento"); `ul.aj-documentos` de `li.aj-documento`: `aj-documento__icone` (`aria-hidden`; `--planilha` para `.xlsx`, `.xls`, `.csv`, `.ods`, `--texto` para o resto), `aj-documento__texto` com o nome como link de download (`a.aj-documento__nome` com `download`) e `aj-documento__meta` (tamanho, autor e data no desktop; tamanho e autor no tablet e no mobile), e `aj-documento__acoes`. Desktop: "Renomear" (fantasma) e "Excluir" (perigo); tablet e mobile: o "⋯" (Menu de ações, `Ações de {nome}`, Excluir por último em perigo), na mesma linha do nome. Só quem gerencia os documentos vê as ações. Vazio: "Nenhum documento." |

Componentes que ganham modificador (README emendado; prévia emendada onde já existe, com o estado novo):

- Cabeçalho de página: `aj-subtitulo__alerta`, a parte de alerta do subtítulo ("8 atividades · 1 atrasada"), em texto comum no desktop e em vermelho no tablet e no mobile; sem atrasadas, sem a parte. Prévia: o subtítulo com o alerta no desktop e no tablet.
- Painel de filtros: na barra do desktop, a busca `aj-campo-icone--filtro` e a opção sem caixa `aj-opcao--solta` ("Só atrasadas"; não somar com `aj-opcao--filtro`); a linha do tablet `aj-linha-filtros--busca` (busca e botão do painel com 48px); a segunda linha da barra superior do mobile `aj-barra-superior__busca` (busca e o botão de ícone "Mais filtros") e os chips `aj-chips--barra` na terceira. Prévia: as três formas da lista de atividades.
- Chip: `aj-chips--barra`, a fila de chips na barra superior do mobile, que vai até a borda da tela (veja Painel de filtros).
- Paginação: `aj-paginacao--com-setas`, com `aj-paginacao__anterior` (o ícone `voltar` antes do texto) e `aj-paginacao__proxima` (o ícone `avancar` depois); desabilitado, o botão fica apagado, sem transparência. Prévia: uma segunda linha com setas e "Anterior" desabilitado.
- Tabela: `aj-tabela--larga` (980px de largura mínima; rola de lado dentro de `aj-tabela-caixa`), `aj-tabela__vazia` (o "—" de célula sem valor), a célula de situação com `aj-etiquetas`. Prévia: uma linha com "Concluída" com o visto e outra com "Direcionada" e "Atrasada", com um "—".
- Etiqueta: `aj-etiquetas` (o grupo lado a lado), `aj-etiqueta--com-icone` (a Concluída da tabela, com o visto no lugar do ponto) e `aj-etiqueta--com-ponto` (a Atrasada dos cartões e da ficha, com o ponto e sem o relógio). Prévia: as duas formas.
- Cartão de atividade: `aj-cartao--atividade` (título de 16,5px no tablet, meta mais justa no mobile), `aj-cartao__topo` (as etiquetas e, no mobile, a seta `aj-cartao__seta` à direita), `aj-cartao__vazio` ("Sem responsável" no lugar dos avatares). Prévia: o cartão do mobile com a seta e o de "Sem responsável".
- Vínculo: `aj-vinculo__escolhido--detalhado` com `aj-vinculo__texto` e `aj-vinculo__titulo` (o processo escolhido em três linhas: "Processo", o título, "Área Trabalhista, definida pelo processo", e o botão de ícone "Sem processo"). Prévia: esse estado.
- Campo: `aj-opcao__nota` (o complemento do nome na opção, "(Produção)"), `aj-rotulo--recuado` (o direcionamento logo abaixo da opção marcada), `aj-conjunto` (o `fieldset` sem moldura, com a legenda e as opções em coluna). Prévia: as duas formas.
- Segmentado: dentro de um rótulo (`div.aj-rotulo role="group"`), o controle fica do tamanho das opções (a complexidade "1", "2", "3").
- Painel: `aj-painel--sempre-destacado` (o painel principal de um formulário lado a lado que mantém a sombra de destaque no mobile), `aj-painel--medio` (o formulário curto de 680px no desktop: devolver e reabrir), os painéis da ficha da atividade (`aj-painel--ficha`, com `--descricao`, `--detalhes`, `--responsaveis`, `--historico`, `--comentarios`, `--documentos`), e `aj-painel--cabecalho-com-faixa`, `aj-painel__botoes--faixa`, `aj-painel__divisor` (veja Cabeçalho da atividade).
- Barra de ações: `aj-acoes--medio`, a barra de 680px abaixo do painel médio, no fluxo (só no desktop; no tablet e no mobile a barra é a fixa).
- Lado a lado: `aj-lado-a-lado--painel-estreito` (a ficha da atividade: o painel de detalhes com base de 280px e a coluna principal com o resto).
- Botão: `aj-botao--ao-fim` (empurra o botão para o fim da linha), `aj-botao--com-icone-menor` (o ícone de 16px, no Excluir da faixa), `aj-botao--enviar` (veja Campo com botão).
- Campo com botão: `aj-campo--folgado` e `aj-botao--enviar` (o novo comentário do tablet: a caixa com mais respiro e o botão quadrado de 48px).
- Navegação (seção "Barra superior do mobile"): a barra de voltar com o "Mais ações" (Menu de ações) no lugar do `aj-barra-superior__vazio`, com o botão de 44px; a segunda linha da barra de título com `aj-barra-superior__busca`.
- Cabeçalho de ficha: uma linha dizendo que a ficha da atividade usa o Cabeçalho da atividade (linha de situação e faixa de ações também no tablet).
- `project/responsividade.md` ("Como implementar"): `aj-desktop-mobile` entra na lista dos invólucros de visão (o bloco que existe no desktop e no mobile e não no tablet).

`design-system.json`: só `lastChange` (`by` o usuário, `via` "Claude Code", `at` agora, `note` "Fase 9E: atividades — lista com contadores, filtros na barra e paginação com setas; ficha com cabeçalho e faixa de ações, responsáveis, histórico, comentários e documentos; telas de responsáveis e de devolução; o invólucro desktop e mobile.").

- [ ] **Step 4: Publicar no design system e sincronizar o canvas**

Uma chamada de publicação no design system (`url` https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS) com os arquivos alterados em `files` (`project/components/bundle.css`, `project/design-system.json`, `project/responsividade.md`, os dez arquivos das cinco pastas novas e os README e prévias emendados do passo 3) e, por último na lista, o índice relido logo antes. Use o caminho longo do scratchpad em `root` e `file_path` (`C:\Users\Gabriel Paulino\AppData\Local\Temp\claude\…`; o atalho `GABRIE~1` é recusado). Anote a versão publicada no ledger. Depois, no canvas (https://claude.ai/artifact/HzHWv1pgVXAnYHuPEYQDFj): `read` do artifact inteiro e uma publicação com `project/canvas.json` (só a versão e `copiedAt` do registro `designSystems` mudam) e, em `files`, a cópia no servidor `"project/ds/araujods/components/bundle.css": {"artifact": "https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS", "path": "project/components/bundle.css"}`. `tokens.json` e `tokens.css` não mudam nesta fase.

- [ ] **Step 5: Copiar para o código**

`wwwroot/css/componentes.css` = as duas primeiras linhas de cabeçalho que o arquivo já tem (a segunda diz "Cópia de components/bundle.css (…); altere lá primeiro.") + o `bundle.css` publicado, da linha "Visões:" em diante, com fim de linha CRLF e sem BOM. Baixe o `bundle.css` publicado (`read` com `path` `project/components/bundle.css`) e rode o roteiro abaixo (salvo em UTF-8 com BOM), com `-Codigo` e `-Saida` apontando para o mesmo `componentes.css` do worktree:

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

Expected: `Corpo idêntico ao bundle.css: True`, `BOM: False` e o número de CRLF igual ao de linhas menos um. Depois:

Run: `git diff --stat -- src/AtividadesJuridicas.Web/wwwroot/css/componentes.css`
Expected: só inserções (nenhuma linha removida), o bloco da fase.

`tokens.css` não muda nesta fase.

- [ ] **Step 6: Build e commit**

Run: `dotnet build -m:1`
Expected: 0 avisos.

Antes do commit, `git branch --show-current` (tem de ser `worktree-feat+fase-9e-atividades`).

```
git add src/AtividadesJuridicas.Web/wwwroot/css/componentes.css
git commit -m "feat(web): design system com os padrões de atividades: filtros com contadores, paginação com setas, cabeçalho da ficha com faixa, responsáveis, histórico, comentários e documentos"
```

---

### Task 2: Contadores da listagem de atividades

**Files:**
- Modify: `src/AtividadesJuridicas.Application/Atividades/PortasDeAtividades.cs` (record `PaginaDeAtividades`, `ContagemDeAtividades`, método novo em `IAtividadeRepositorio`)
- Modify: `src/AtividadesJuridicas.Application/Atividades/ListarAtividades.cs`
- Modify: `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/AtividadeRepositorio.cs`
- Modify: `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs` (`AtividadeRepositorioFalso`), `tests/AtividadesJuridicas.UnitTests/Application/AtividadesTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/AtividadesTests.cs`

**Interfaces:**
- Produces: `public sealed record PaginaDeAtividades(IReadOnlyList<AtividadeResumo> Itens, int Total, int Numero, int Tamanho, int NoEscopo, int Atrasadas) : Pagina<AtividadeResumo>(Itens, Total, Numero, Tamanho);` `public sealed record ContagemDeAtividades(int Total, int Atrasadas);` `Task<ContagemDeAtividades> ContarAsync(Guid? responsavelId, DateOnly hoje, CancellationToken ct)` em `IAtividadeRepositorio`. `ListarAtividades` passa a implementar `IRequisicao<Resultado<PaginaDeAtividades>>` (mesmos campos).

**Regras:**
- O escopo da contagem é o da regra de acesso da listagem: `responsavelId = ator.EhDiretoria ? null : ator.Id`. Nenhum outro filtro (situação, só atrasadas, responsável pedido, revisor, cliente, processo, busca) entra na contagem.
- `Atrasadas` usa a mesma regra do filtro "só atrasadas": situação diferente de Concluída e data de entrega antes de `calendario.Hoje`. Atividade excluída não conta.
- A consulta continua uma só requisição auditada (a contagem é parte de `ListarAtividades`, como em `ListarClientes` e `ListarProcessos`).
- O falso dos testes de unidade implementa a mesma regra.
- Testes de integração contra o banco com outros perfis presentes (spec 10): a Diretoria conta o escritório inteiro mesmo com `ResponsavelId`, `Status`, `SomenteAtrasadas` e `Busca` pedidos; a Produção conta só as suas; uma concluída com data passada não é atrasada; uma excluída não conta.

- [ ] **Step 1: Testes (devem falhar)**

Em `tests/AtividadesJuridicas.UnitTests/Application/AtividadesTests.cs`, o auxiliar `Listar` passa a devolver o tipo novo (`Resultado<T>` não é covariante: sem a troca, o arquivo não compila depois da implementação). Troque:

```csharp
    private Task<Resultado<Pagina<AtividadeResumo>>> Listar(Usuario? ator, ListarAtividades? consulta = null) =>
```

por:

```csharp
    private Task<Resultado<PaginaDeAtividades>> Listar(Usuario? ator, ListarAtividades? consulta = null) =>
```

(o corpo fica igual). E acrescente, na seção `// ---- Listar ----`, depois de `Listagem_e_consulta_auditavel_com_os_filtros_no_detalhe`:

```csharp
    [Fact]
    public async Task Contadores_da_listagem_sao_do_escopo_de_acesso_e_nao_do_filtro()
    {
        // Hoje é 09/10/2026 (CalendarioFixo). Diana é a revisora de todas.
        var dosDois = await DirecionadaAsync("De Pedro e Paula", new DateOnly(2026, 10, 1));       // atrasada
        var daEster = await CriadaAsync("Da Ester");                                                // entrega em 20/10
        (await Definir(Diana, daEster, Diana.Id, (_ester, null))).Sucesso.Should().BeTrue();
        await CriadaAsync("Sem responsável", new DateOnly(2026, 10, 2));                            // atrasada
        var concluida = await DirecionadaAsync("Concluída vencida", new DateOnly(2026, 10, 1));
        (await Concluir(_pedro, concluida)).Sucesso.Should().BeTrue();
        (await Concluir(_paula, concluida)).Sucesso.Should().BeTrue();
        (await Aprovar(Diana, concluida)).Sucesso.Should().BeTrue();
        var excluida = await DirecionadaAsync("Excluída", new DateOnly(2026, 10, 1));
        (await Excluir(Diana, excluida)).Sucesso.Should().BeTrue();

        var daDiretoria = (await Listar(Diana)).Valor!;
        var filtrada = (await Listar(Diana, new ListarAtividades(
            Status: StatusAtividade.Direcionada, ResponsavelId: _ester.Id, RevisorId: Diana.Id, Busca: "Ester"))).Valor!;
        var soAtrasadasDoDavi = (await Listar(Diana, new ListarAtividades(SomenteAtrasadas: true, RevisorId: _davi.Id))).Valor!;
        var doPedro = (await Listar(_pedro, new ListarAtividades(ResponsavelId: _ester.Id))).Valor!;
        var daEsterSo = (await Listar(_ester)).Valor!;

        // Diretoria: o escritório inteiro (De Pedro e Paula, Da Ester, Sem responsável, Concluída vencida); atrasadas,
        // só as duas não concluídas com entrega vencida. A excluída não conta.
        daDiretoria.NoEscopo.Should().Be(4);
        daDiretoria.Atrasadas.Should().Be(2);
        // Os filtros pedidos mudam a página, não os contadores (nem o responsável pedido pela Diretoria).
        filtrada.Itens.Select(a => a.Id).Should().Equal(daEster);
        filtrada.NoEscopo.Should().Be(4);
        filtrada.Atrasadas.Should().Be(2);
        soAtrasadasDoDavi.Itens.Should().BeEmpty();
        soAtrasadasDoDavi.NoEscopo.Should().Be(4);
        soAtrasadasDoDavi.Atrasadas.Should().Be(2);
        // Produção: só as suas, qualquer que seja o filtro; a concluída vencida não é atrasada.
        doPedro.Itens.Select(a => a.Id).Should().BeEquivalentTo([dosDois, concluida]);
        doPedro.NoEscopo.Should().Be(2);
        doPedro.Atrasadas.Should().Be(1);
        daEsterSo.NoEscopo.Should().Be(1);
        daEsterSo.Atrasadas.Should().Be(0);
    }
```

Em `tests/AtividadesJuridicas.IntegrationTests/AtividadesTests.cs` (com os auxiliares do arquivo), depois de `Atraso_no_banco_so_vale_para_data_anterior_a_hoje_e_nao_concluida`:

```csharp
    [Fact]
    public async Task Contadores_da_listagem_no_banco_sao_do_escopo_de_acesso_e_nao_do_filtro()
    {
        // Diretoria (Diana, Davi) e Produção (Pedro, Paula, Ester) no banco. Hoje vem do calendário do escritório.
        var hoje = await EmEscopoAsync(null, sp => Task.FromResult(sp.GetRequiredService<ICalendarioDoEscritorio>().Hoje));
        var doPedroAtrasada = await CriarAsync("Do Pedro, atrasada", hoje.AddDays(-2));
        await DirecionarAsync(doPedroAtrasada, _diana, (_pedro, null));
        var doPedroNoPrazo = await CriarAsync("Do Pedro, no prazo");
        await DirecionarAsync(doPedroNoPrazo, _diana, (_pedro, null));
        var daEsterAtrasada = await CriarAsync("Da Ester, atrasada", hoje.AddDays(-3));
        await DirecionarAsync(daEsterAtrasada, _davi, (_ester, null));
        await CriarAsync("Sem responsável, atrasada", hoje.AddDays(-1));
        var concluidaVencida = await CriarAsync("Do Pedro, concluída vencida", hoje.AddDays(-1));
        await DirecionarAsync(concluidaVencida, _diana, (_pedro, null));
        await ConcluirAsync(_pedro, concluidaVencida);
        (await ExecutarAsync(_diana, new AprovarAtividade(concluidaVencida))).Sucesso.Should().BeTrue();
        var excluida = await CriarAsync("Do Pedro, excluída", hoje.AddDays(-1));
        await DirecionarAsync(excluida, _diana, (_pedro, null));
        (await ExecutarAsync(_diana, new ExcluirAtividade(excluida))).Sucesso.Should().BeTrue();

        var daDiretoria = (await ExecutarAsync(_diana, new ListarAtividades())).Valor!;
        var daDiretoriaFiltrada = (await ExecutarAsync(_diana, new ListarAtividades(
            Status: StatusAtividade.Direcionada, ResponsavelId: _ester, SomenteAtrasadas: true, Busca: "Ester"))).Valor!;
        var doPedro = (await ExecutarAsync(_pedro, new ListarAtividades())).Valor!;
        var doPedroFiltrada = (await ExecutarAsync(_pedro, new ListarAtividades(Status: StatusAtividade.Criada, ResponsavelId: _ester))).Valor!;
        var daPaula = (await ExecutarAsync(_paula, new ListarAtividades())).Valor!;

        // Diretoria: as cinco vivas do escritório; atrasadas, as três não concluídas com entrega vencida.
        daDiretoria.Total.Should().Be(5);
        daDiretoria.NoEscopo.Should().Be(5);
        daDiretoria.Atrasadas.Should().Be(3);
        // Situação, responsável, só atrasadas e busca mudam a página, não os contadores.
        daDiretoriaFiltrada.Itens.Select(a => a.Id).Should().Equal(daEsterAtrasada);
        daDiretoriaFiltrada.NoEscopo.Should().Be(5);
        daDiretoriaFiltrada.Atrasadas.Should().Be(3);
        // Produção: só as atividades em que é responsável; a concluída vencida não é atrasada; a excluída não conta.
        doPedro.Itens.Select(a => a.Id).Should().BeEquivalentTo([doPedroAtrasada, doPedroNoPrazo, concluidaVencida]);
        doPedro.NoEscopo.Should().Be(3);
        doPedro.Atrasadas.Should().Be(1);
        doPedroFiltrada.Itens.Should().BeEmpty();
        doPedroFiltrada.NoEscopo.Should().Be(3);
        doPedroFiltrada.Atrasadas.Should().Be(1);
        daPaula.NoEscopo.Should().Be(0);
        daPaula.Atrasadas.Should().Be(0);

        // A contagem vai dentro da listagem: cada pedido do Pedro é uma consulta registrada, não duas.
        var listagensDoPedro = await EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking()
            .CountAsync(a => a.UsuarioId == _pedro && a.TipoRecurso == "Atividade" && a.Acao == AcaoDeAcesso.Listou));
        listagensDoPedro.Should().Be(2);
    }
```

Run: `dotnet build -m:1`
Expected: não compila (`PaginaDeAtividades` não existe; `NoEscopo` e `Atrasadas` não existem em `Pagina<AtividadeResumo>`). É o RED.

- [ ] **Step 2: Implementar**

Em `src/AtividadesJuridicas.Application/Atividades/PortasDeAtividades.cs`, logo depois do record `AtividadeResumo`:

```csharp
/// <summary>
/// <paramref name="NoEscopo"/>: todas as atividades que o usuário pode listar, sem nenhum filtro (a Diretoria, o
/// escritório inteiro; os demais, as atividades em que são responsáveis). <paramref name="Atrasadas"/>: as atrasadas
/// desse mesmo escopo. São os contadores do subtítulo e dos chips da lista, que não mudam com o filtro.
/// </summary>
public sealed record PaginaDeAtividades(
    IReadOnlyList<AtividadeResumo> Itens, int Total, int Numero, int Tamanho, int NoEscopo, int Atrasadas)
    : Pagina<AtividadeResumo>(Itens, Total, Numero, Tamanho);

public sealed record ContagemDeAtividades(int Total, int Atrasadas);
```

E, em `IAtividadeRepositorio`, logo depois de `ListarAsync`:

```csharp
    /// <summary>
    /// Quantas atividades vivas há no escopo (todas, ou só aquelas em que <paramref name="responsavelId"/> é responsável)
    /// e quantas delas estão atrasadas: não concluídas, com entrega antes de <paramref name="hoje"/>. Nenhum outro filtro.
    /// </summary>
    Task<ContagemDeAtividades> ContarAsync(Guid? responsavelId, DateOnly hoje, CancellationToken ct);
```

`src/AtividadesJuridicas.Application/Atividades/ListarAtividades.cs` (a requisição, o detalhe da auditoria e a regra de acesso não mudam; só o tipo da resposta e a contagem):

```csharp
public sealed record ListarAtividades(
    StatusAtividade? Status = null, Guid? ResponsavelId = null, Guid? RevisorId = null, Guid? ClienteId = null, Guid? ProcessoId = null,
    bool SomenteAtrasadas = false, string? Busca = null, int Pagina = 1)
    : IRequisicao<Resultado<PaginaDeAtividades>>, IConsultaAuditavel
{
    public string TipoRecurso => "Atividade";
    public AcaoDeAcesso Acao => AcaoDeAcesso.Listou;
    public string? Detalhe => DetalheDeConsulta.Serializar(
        new { Status, ResponsavelId, RevisorId, ClienteId, ProcessoId, SomenteAtrasadas, Busca, Pagina });
}

public sealed class ListarAtividadesManipulador(
    IContextoDeAcesso contexto, IAtividadeRepositorio atividades, ICalendarioDoEscritorio calendario)
    : IManipulador<ListarAtividades, Resultado<PaginaDeAtividades>>
{
    public async Task<Resultado<PaginaDeAtividades>> ManipularAsync(ListarAtividades requisicao, CancellationToken ct)
    {
        if (await contexto.ObterAsync(ct) is not { } ator)
            return Resultado<PaginaDeAtividades>.Falha(TipoErro.AcessoNegado, "Acesso negado.");

        // Um "hoje" só para a página e para a contagem: as duas decidem "atrasada" com a mesma data.
        var hoje = calendario.Hoje;

        // Produção só lista as próprias atividades, qualquer que seja o filtro pedido.
        var responsavelId = ator.EhDiretoria ? requisicao.ResponsavelId : ator.Id;
        var filtro = new FiltroDeAtividades(
            requisicao.Status, responsavelId, requisicao.RevisorId, requisicao.ClienteId, requisicao.ProcessoId,
            requisicao.SomenteAtrasadas, requisicao.Busca, hoje);

        var pagina = await atividades.ListarAsync(
            filtro, Paginacao.Normalizar(requisicao.Pagina, AtividadesOpcoes.TamanhoDaPagina),
            AtividadesOpcoes.TamanhoDaPagina, ct);

        // Os contadores da tela são do escopo de acesso, não do filtro: a Diretoria conta o escritório inteiro (o
        // responsável que ela pediu também fica de fora); a Produção, as atividades em que é responsável.
        var contagem = await atividades.ContarAsync(ator.EhDiretoria ? null : ator.Id, hoje, ct);

        return Resultado<PaginaDeAtividades>.Ok(new PaginaDeAtividades(
            pagina.Itens, pagina.Total, pagina.Numero, pagina.Tamanho, contagem.Total, contagem.Atrasadas));
    }
}
```

Em `src/AtividadesJuridicas.Infrastructure/Persistencia/Repositorios/AtividadeRepositorio.cs`, o filtro de responsável e o de atraso passam a ser dois métodos privados, usados pela listagem e pela contagem (as duas contam pela mesma regra). Em `ListarAsync`, troque:

```csharp
        if (filtro.ResponsavelId is { } responsavelId)
            consulta = consulta.Where(a => a.Responsaveis.Any(r => r.UsuarioId == responsavelId));
```

por:

```csharp
        if (filtro.ResponsavelId is { } responsavelId) consulta = DoResponsavel(consulta, responsavelId);
```

e:

```csharp
        if (filtro.SomenteAtrasadas)
        {
            var hoje = filtro.Hoje;
            consulta = consulta.Where(a => a.Status != StatusAtividade.Concluida && a.DataEntrega < hoje);
        }
```

por:

```csharp
        if (filtro.SomenteAtrasadas) consulta = SoAtrasadas(consulta, filtro.Hoje);
```

E acrescente, logo depois de `ListarAsync`:

```csharp
    public async Task<ContagemDeAtividades> ContarAsync(Guid? responsavelId, DateOnly hoje, CancellationToken ct)
    {
        // Só o escopo de acesso, nenhum filtro da tela. Pelas entidades mapeadas: a atividade excluída e a entrada de
        // responsável excluída ficam de fora pelo filtro de exclusão lógica, como na listagem.
        var noEscopo = db.Atividades.AsNoTracking();
        if (responsavelId is { } id) noEscopo = DoResponsavel(noEscopo, id);

        return new ContagemDeAtividades(await noEscopo.CountAsync(ct), await SoAtrasadas(noEscopo, hoje).CountAsync(ct));
    }

    // A mesma regra na listagem e na contagem: o filtro "só atrasadas" e o contador de atrasadas contam igual.
    private static IQueryable<Atividade> DoResponsavel(IQueryable<Atividade> consulta, Guid responsavelId) =>
        consulta.Where(a => a.Responsaveis.Any(r => r.UsuarioId == responsavelId));

    private static IQueryable<Atividade> SoAtrasadas(IQueryable<Atividade> consulta, DateOnly hoje) =>
        consulta.Where(a => a.Status != StatusAtividade.Concluida && a.DataEntrega < hoje);
```

Em `tests/AtividadesJuridicas.UnitTests/Application/Falsos.cs`, no `AtividadeRepositorioFalso`, logo depois de `ListarAsync`:

```csharp
    // Mesma regra da consulta real: o escopo é só o responsável (nenhum outro filtro), e atrasada é a não concluída
    // com entrega antes de hoje. A excluída já saiu de Itens (Remover).
    public Task<ContagemDeAtividades> ContarAsync(Guid? responsavelId, DateOnly hoje, CancellationToken ct)
    {
        var noEscopo = Itens.Where(a => responsavelId is null || a.EhResponsavel(responsavelId.Value)).ToList();
        return Task.FromResult(new ContagemDeAtividades(noEscopo.Count, noEscopo.Count(a => a.EstaAtrasada(hoje))));
    }
```

Quem lê a resposta como página continua compilando sem mudança: `Pages/Atividades.razor` (`_pagina = resultado.Valor ?? Vazia;` passa a ter o tipo `Pagina<AtividadeResumo>`, porque `PaginaDeAtividades` se converte nele) e `Agenda/FormularioDeEvento.razor` (só lê `Valor?.Itens` e `Valor!.Total`). Se o compilador apontar outro lugar que tipava `Resultado<Pagina<AtividadeResumo>>`, troque o tipo por `Resultado<PaginaDeAtividades>`.

- [ ] **Step 3: Build e testes**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build`
Expected: todos passando, com o teste novo (`Contadores_da_listagem_sao_do_escopo_de_acesso_e_nao_do_filtro`).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadesTests|FullyQualifiedName~AtividadesWebTests|FullyQualifiedName~AgendaWebTests|FullyQualifiedName~AgendaTests|FullyQualifiedName~PortalDoClienteTests|FullyQualifiedName~ProdutividadeTests|FullyQualifiedName~AndamentoTests"`
Expected: todos passando, com o teste novo (`Contadores_da_listagem_no_banco_sao_do_escopo_de_acesso_e_nao_do_filtro`).

- [ ] **Step 4: Commit**

```
git branch --show-current
git add src tests
git commit -m "feat(app): listagem de atividades com os contadores do escopo de acesso para o subtítulo e os chips"
```

---

### Task 3: Estado de carregamento dos formulários

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Layout/EstadoDeCarga.cs`, `src/AtividadesJuridicas.Web/Components/Layout/CargaDoFormulario.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/ClienteFormulario.razor`, `ProcessoFormulario.razor`, `UsuarioFormulario.razor`
- Create: `tests/AtividadesJuridicas.IntegrationTests/CargaDoFormularioInterativaTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/UsuariosWebTests.cs` (as duas asserções do aviso de erro da consulta)
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (`Migradas`: o componente novo)

**Interfaces:**
- Produces: `public enum EstadoDeCarga { Carregando, Pronto, AcessoNegado, NaoEncontrado, Falhou }`; `<CargaDoFormulario Estado="…" Erro="…" TextoNaoEncontrado="…" TentarDeNovo="…">…</CargaDoFormulario>` — `Estado` (`EstadoDeCarga`, obrigatório), `Erro` (`string?`, a mensagem da falha), `TextoNaoEncontrado` (`string`, obrigatório, ex.: `Cliente não encontrado.`), `TentarDeNovo` (`EventCallback`), `ChildContent` (o formulário, só em `Pronto`).

**Regras:**
- O componente emite, para cada estado, exatamente a marcação que os três formulários emitem hoje (leia-os antes): `Carregando…`, `Acesso negado.` no aviso de erro, o texto de não encontrado; e, em `Falhou`, a mensagem com "Tentar de novo" (botão secundário, como o `EstadoDaLista`: o aviso com ações, `div` com `aj-aviso--com-acoes`).
- Os três formulários passam a usá-lo, sem mudar o que já mostram fora da falha. Cada um ganha "Tentar de novo" na falha da consulta inicial (spec 15, "Da fase 9D"), que repete a carga inteira (inclusive a do perfil). As duas asserções de `UsuariosWebTests` que casavam a classe exata do aviso de erro (`"aj-aviso aj-aviso--erro"`) são reescritas para o aviso com ações e passam a provar também o "Tentar de novo".
- A carga continua onde está: em `OnInitializedAsync` nos formulários de cliente e de processo; no de usuário, os perfis em `OnInitializedAsync` e o usuário em `OnParametersSetAsync` (o "Tentar de novo" chama as duas). O recarregar entre `/x/novo` e `/x/{id}/editar` fica anotado na spec, sem mudança aqui.
- Teste bUnit: em cada estado o componente mostra o que deve e não mostra o formulário; "Tentar de novo" chama o callback. E, para os três formulários, a falha da carga mostra "Tentar de novo" e o clique repete a carga inteira.

- [ ] **Step 1: Testes (devem falhar)**

`tests/AtividadesJuridicas.IntegrationTests/CargaDoFormularioInterativaTests.cs`:

```csharp
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Components.Layout;
using AtividadesJuridicas.Web.Components.Pages;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// O estado de carregamento dos formulários em tela própria: um estado por vez, com a marcação que os formulários já
/// emitiam; o formulário só quando a carga ficou pronta; e a nova tentativa, que repete a carga inteira.
/// </summary>
public class CargaDoFormularioInterativaTests : TelaInterativa
{
    private const string Formulario = "<form id=\"formulario-de-teste\" class=\"aj-painel\">o formulário</form>";
    private const string Erro = "Não foi possível consultar o cliente.";
    private static readonly Guid Area = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a9");

    private static readonly string[] Textos =
        ["Carregando…", "Acesso negado.", "Cliente não encontrado.", Erro, "Tentar de novo", "o formulário"];

    private int _tentativas;

    private IRenderedComponent<CargaDoFormulario> Carga(EstadoDeCarga estado) =>
        Desenhar<CargaDoFormulario>(p => p
            .Add(c => c.Estado, estado)
            .Add(c => c.Erro, Erro)
            .Add(c => c.TextoNaoEncontrado, "Cliente não encontrado.")
            .Add(c => c.TentarDeNovo, () => _tentativas++)
            .AddChildContent(Formulario));

    [Theory]
    [InlineData(EstadoDeCarga.Carregando, "Carregando…")]
    [InlineData(EstadoDeCarga.AcessoNegado, "Acesso negado.")]
    [InlineData(EstadoDeCarga.NaoEncontrado, "Cliente não encontrado.")]
    [InlineData(EstadoDeCarga.Pronto, "o formulário")]
    public void Mostra_um_estado_por_vez(EstadoDeCarga estado, string texto)
    {
        var carga = Carga(estado);

        carga.Markup.Should().Contain(texto);
        foreach (var outro in Textos.Where(t => t != texto))
            carga.Markup.Should().NotContain(outro);
    }

    [Fact]
    public void Emite_a_marcacao_que_os_formularios_ja_emitiam()
    {
        Carga(EstadoDeCarga.Carregando).Markup.Should().Contain("<p class=\"aj-subtitulo\" role=\"status\">Carregando…</p>");
        Carga(EstadoDeCarga.AcessoNegado).Markup.Should().MatchRegex(
            "(?s)<p class=\"aj-aviso aj-aviso--erro\" role=\"alert\">\\s*<svg.*?</svg>\\s*<span>Acesso negado.</span>\\s*</p>");
        Carga(EstadoDeCarga.NaoEncontrado).Markup.Should().MatchRegex(
            "(?s)<p class=\"aj-aviso aj-aviso--erro\" role=\"alert\">\\s*<svg.*?</svg>\\s*<span>Cliente não encontrado.</span>\\s*</p>");
        Carga(EstadoDeCarga.Pronto).Markup.Should().Contain(Formulario);
    }

    [Fact]
    public void Falha_mostra_o_erro_com_tentar_de_novo_sem_o_formulario_e_o_clique_avisa()
    {
        var carga = Carga(EstadoDeCarga.Falhou);

        var aviso = carga.Find("div.aj-aviso.aj-aviso--erro.aj-aviso--com-acoes[role=alert]");
        aviso.TextContent.Should().Contain(Erro);
        carga.FindAll("#formulario-de-teste").Should().BeEmpty();
        carga.Markup.Should().NotContain("Carregando…").And.NotContain("Acesso negado.");

        aviso.QuerySelector("button.aj-botao.aj-botao--secundario")!.Click();

        _tentativas.Should().Be(1);
    }

    [Fact]
    public void Formulario_de_processo_com_falha_na_carga_tenta_de_novo_e_mostra_o_formulario()
    {
        var tentativas = 0;
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarAreasAtuacao, Resultado<IReadOnlyList<AreaAtuacaoResumo>>>(_ => ++tentativas == 1
                ? Resultado<IReadOnlyList<AreaAtuacaoResumo>>.Falha(TipoErro.Validacao, "Não foi possível consultar as áreas de atuação.")
                : Resultado<IReadOnlyList<AreaAtuacaoResumo>>.Ok([new AreaAtuacaoResumo(Area, "Cível", true)]));
        var tela = Desenhar<ProcessoFormulario>();

        tela.Find("div.aj-aviso--erro").TextContent.Should().Contain("Não foi possível consultar as áreas de atuação.");
        tela.FindAll("form#formulario-do-processo").Should().BeEmpty();

        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.WaitForAssertion(() => tela.FindAll("form#formulario-do-processo").Should().ContainSingle());
        tela.Find("select.aj-campo").TextContent.Should().Contain("Cível");
        tela.FindAll("button").Where(b => b.TextContent == "Tentar de novo").Should().BeEmpty();
        // A carga inteira, inclusive a do perfil de quem está na tela.
        Despachante.Recebidas.OfType<ObterMeuAcesso>().Should().HaveCount(2);
        Despachante.Recebidas.OfType<ListarAreasAtuacao>().Should().HaveCount(2);
    }

    [Fact]
    public void Formulario_de_usuario_repete_a_carga_dos_perfis_e_mostra_o_formulario()
    {
        var tentativas = 0;
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarPerfis, Resultado<IReadOnlyList<PerfilDetalhe>>>(_ => ++tentativas == 1
                ? Resultado<IReadOnlyList<PerfilDetalhe>>.Falha(TipoErro.Validacao, "Não foi possível consultar os perfis.")
                : Resultado<IReadOnlyList<PerfilDetalhe>>.Ok([new PerfilDetalhe(Guid.NewGuid(), "Produção", TipoPerfil.Producao, true, 0)]));
        var tela = Desenhar<UsuarioFormulario>();

        tela.Find("div.aj-aviso--erro").TextContent.Should().Contain("Não foi possível consultar os perfis.");
        tela.FindAll("form#formulario-do-usuario").Should().BeEmpty();

        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.WaitForAssertion(() => tela.FindAll("form#formulario-do-usuario").Should().ContainSingle());
        tela.Markup.Should().Contain("Escolha o perfil. Para um usuário de cliente, indique o cliente.");
        Despachante.Recebidas.OfType<ObterMeuAcesso>().Should().HaveCount(2);
        Despachante.Recebidas.OfType<ListarPerfis>().Should().HaveCount(2);
    }

    [Fact]
    public void Formulario_de_cliente_com_falha_na_consulta_mostra_tentar_de_novo_e_consulta_de_novo()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ObterCliente, Resultado<ClienteDetalhe>>(Resultado<ClienteDetalhe>.Falha(TipoErro.Validacao, Erro));
        var tela = Desenhar<ClienteFormulario>(p => p.Add(c => c.Id, (Guid?)Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c1")));

        tela.Find("div.aj-aviso--erro").TextContent.Should().Contain(Erro);
        tela.FindAll("form#formulario-do-cliente").Should().BeEmpty();
        // O subtítulo só acompanha o formulário.
        tela.Markup.Should().NotContain("Só o nome e o CPF ou CNPJ são obrigatórios.");

        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.WaitForAssertion(() => Despachante.Recebidas.OfType<ObterCliente>().Should().HaveCount(2));
        Despachante.Recebidas.OfType<ObterMeuAcesso>().Should().HaveCount(2);
        tela.Find("div.aj-aviso--erro").TextContent.Should().Contain(Erro);
    }
}
```

Em `tests/AtividadesJuridicas.IntegrationTests/UsuariosWebTests.cs`, as duas asserções sobre o erro da consulta passam a provar também o "Tentar de novo" (o aviso de erro com ação é o `div` com `aj-aviso--com-acoes`, como no `EstadoDaLista`; a classe exata de hoje, `"aj-aviso aj-aviso--erro"`, deixa de casar). Em `Falha_na_consulta_da_lista_na_edicao_mostra_o_erro_e_nao_diz_que_o_usuario_nao_existe`, troque:

```csharp
        html.Should().MatchRegex("(?s)class=\"aj-aviso aj-aviso--erro\".*?Falha de teste\\.")
            .And.MatchRegex("<h1 class=\"aj-titulo\">Editar usuário</h1>")
```

por:

```csharp
        html.Should().MatchRegex("(?s)class=\"aj-aviso aj-aviso--erro aj-aviso--com-acoes\"[^>]*role=\"alert\".*?Falha de teste\\..*?>Tentar de novo</button>")
            .And.MatchRegex("<h1 class=\"aj-titulo\">Editar usuário</h1>")
```

E em `Falha_na_consulta_dos_perfis_mostra_o_erro_no_lugar_do_formulario`, troque:

```csharp
        html.Should().MatchRegex("(?s)class=\"aj-aviso aj-aviso--erro\".*?Falha de teste\\.")
            .And.MatchRegex("<h1 class=\"aj-titulo\">Novo usuário</h1>");
```

por:

```csharp
        html.Should().MatchRegex("(?s)class=\"aj-aviso aj-aviso--erro aj-aviso--com-acoes\"[^>]*role=\"alert\".*?Falha de teste\\..*?>Tentar de novo</button>")
            .And.MatchRegex("<h1 class=\"aj-titulo\">Novo usuário</h1>");
```

(As asserções negativas desses dois testes ficam como estão.) As duas reescritas continuam provando o que provavam (o erro da consulta no lugar do formulário, com o título da tela) e passam a provar o "Tentar de novo" no mesmo aviso; liste-as no relatório como asserções reescritas, com o motivo: o aviso de erro com ação é `div` com `aj-aviso--com-acoes`, e a classe exata de antes deixou de casar.

Run: `dotnet build -m:1`
Expected: não compila (`CargaDoFormulario` e `EstadoDeCarga` não existem). É o RED.

- [ ] **Step 2: O componente**

`src/AtividadesJuridicas.Web/Components/Layout/EstadoDeCarga.cs`:

```csharp
namespace AtividadesJuridicas.Web.Components.Layout;

/// <summary>Em que pé está a carga de um formulário em tela própria.</summary>
public enum EstadoDeCarga
{
    Carregando,
    Pronto,
    AcessoNegado,
    NaoEncontrado,
    Falhou,
}
```

`src/AtividadesJuridicas.Web/Components/Layout/CargaDoFormulario.razor`:

```razor
@* O lugar do formulário de uma tela própria (cliente, processo, usuário, atividade) enquanto a carga não deixa
   mostrá-lo: carregando, acesso negado, o registro que não existe ou a falha da consulta, com a nova tentativa. O
   formulário (o conteúdo) só aparece pronto. *@
@if (Estado == EstadoDeCarga.Carregando)
{
    <p class="aj-subtitulo" role="status">Carregando…</p>
}
else if (Estado == EstadoDeCarga.AcessoNegado)
{
    <Aviso Tipo="TipoDeAviso.Erro">Acesso negado.</Aviso>
}
else if (Estado == EstadoDeCarga.NaoEncontrado)
{
    <Aviso Tipo="TipoDeAviso.Erro">@TextoNaoEncontrado</Aviso>
}
else if (Estado == EstadoDeCarga.Falhou)
{
    <Aviso Tipo="TipoDeAviso.Erro">
        <ChildContent>@Erro</ChildContent>
        <Acoes><button type="button" class="aj-botao aj-botao--secundario" @onclick="TentarDeNovo">Tentar de novo</button></Acoes>
    </Aviso>
}
else
{
    @ChildContent
}

@code {
    [Parameter, EditorRequired] public EstadoDeCarga Estado { get; set; }

    /// <summary>A mensagem da falha da consulta (estado <see cref="EstadoDeCarga.Falhou"/>).</summary>
    [Parameter] public string? Erro { get; set; }

    /// <summary>O texto do registro que não existe: "Cliente não encontrado.".</summary>
    [Parameter, EditorRequired] public string TextoNaoEncontrado { get; set; } = string.Empty;

    /// <summary>"Tentar de novo": a tela repete a carga inteira, inclusive a do perfil de quem está nela.</summary>
    [Parameter] public EventCallback TentarDeNovo { get; set; }

    /// <summary>O formulário, mostrado só com a carga pronta.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

- [ ] **Step 3: O formulário de cliente usa o componente**

Em `src/AtividadesJuridicas.Web/Components/Pages/ClienteFormulario.razor`, a marcação. O começo, de `@if (!_carregado)` até a abertura do `@if (_mensagem …)`:

```razor
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
```

passa a:

```razor
<CargaDoFormulario Estado="_estado" Erro="@_erroDaCarga" TextoNaoEncontrado="Cliente não encontrado." TentarDeNovo="CarregarTudoAsync">
    @if (_mensagem is not null)
```

Tudo o que vem depois (o aviso de `_mensagem`, o segmentado do mobile, o `EditForm`, o comentário e as três barras de ação) fica igual, e o `}` que fechava o `else`, logo antes de `@code {`, passa a `</CargaDoFormulario>`.

O código. Os campos `_carregado`, `_autorizado`, `_naoEncontrado` e `_erroDaConsulta` saem; entram:

```csharp
    private EstadoDeCarga _estado;
    private string? _erroDaCarga;
```

As propriedades que dependiam deles:

```csharp
    // Sem o cliente para voltar a ele, o voltar vai para a lista.
    private string VoltarDoCabecalho => _estado == EstadoDeCarga.NaoEncontrado ? "/clientes" : Voltar;
    private string OrigemDoCabecalho => _estado == EstadoDeCarga.NaoEncontrado ? "Clientes" : Origem;

    // O subtítulo só acompanha o formulário: sobre "Acesso negado.", "Cliente não encontrado." ou um erro ele sobra.
    private string? SubtituloDoFormulario =>
        _estado == EstadoDeCarga.Pronto ? "Só o nome e o CPF ou CNPJ são obrigatórios." : null;
```

A carga (troca `OnInitializedAsync` e `CarregarAsync`):

```csharp
    protected override Task OnInitializedAsync() => CarregarTudoAsync();

    // A carga inteira: o perfil de quem está na tela e, na edição, o cliente. "Tentar de novo" repete tudo.
    private async Task CarregarTudoAsync()
    {
        _estado = EstadoDeCarga.Carregando;
        _erroDaCarga = null;
        if (await Despachante.EnviarAsync(new ObterMeuAcesso()) is not { EhDiretoria: true })
        {
            _estado = EstadoDeCarga.AcessoNegado;
            return;
        }

        if (Id is { } id) await CarregarAsync(id);
        if (_estado == EstadoDeCarga.Carregando) _estado = EstadoDeCarga.Pronto;
    }

    private async Task CarregarAsync(Guid id)
    {
        var resultado = await Despachante.EnviarAsync(new ObterCliente(id));
        if (resultado.Erro == TipoErro.AcessoNegado)
        {
            _estado = EstadoDeCarga.AcessoNegado;
            return;
        }

        if (resultado.Valor is not { } c)
        {
            if (resultado.Erro == TipoErro.NaoEncontrado) _estado = EstadoDeCarga.NaoEncontrado;
            else
            {
                _estado = EstadoDeCarga.Falhou;
                _erroDaCarga = resultado.Mensagem ?? "Não foi possível consultar o cliente.";
            }
            return;
        }

        _form = new Formulario
        {
            // (o mesmo preenchimento de hoje)
        };
    }
```

(O inicializador de `_form` é o de hoje, sem mudança.) Em `SalvarAsync`, o ramo de acesso negado troca `_autorizado = false;` por `_estado = EstadoDeCarga.AcessoNegado;`.

- [ ] **Step 4: O formulário de processo usa o componente**

Em `src/AtividadesJuridicas.Web/Components/Pages/ProcessoFormulario.razor`, a marcação. O começo, de `@if (!_carregado)` até a abertura do `@if (_mensagem …)` (o mesmo bloco do cliente, com `Processo não encontrado.`), passa a:

```razor
<CargaDoFormulario Estado="_estado" Erro="@_erroDaCarga" TextoNaoEncontrado="Processo não encontrado." TentarDeNovo="CarregarTudoAsync">
    @if (_mensagem is not null)
```

O resto fica igual (inclusive o `<form id="busca-de-cliente" hidden …>`, que continua dentro), e o `}` logo antes de `@code {` passa a `</CargaDoFormulario>`.

O código. Saem `_carregado`, `_autorizado`, `_naoEncontrado` e `_erroDaConsulta`; entram `private EstadoDeCarga _estado;` e `private string? _erroDaCarga;`.

```csharp
    // Sem o processo para voltar a ele, o voltar vai para a lista.
    private string VoltarDoCabecalho => _estado == EstadoDeCarga.NaoEncontrado ? "/processos" : Voltar;
    private string OrigemDoCabecalho => _estado == EstadoDeCarga.NaoEncontrado ? "Processos" : Origem;

    // O subtítulo só acompanha o formulário: sobre "Acesso negado.", "Processo não encontrado." ou um erro ele sobra.
    private string? SubtituloDoFormulario =>
        _estado == EstadoDeCarga.Pronto ? "Preencha os dados e vincule os clientes do processo." : null;

    protected override Task OnInitializedAsync() => CarregarTudoAsync();

    // A carga inteira: o perfil de quem está na tela, o processo (na edição) e as áreas. "Tentar de novo" repete tudo.
    private async Task CarregarTudoAsync()
    {
        _estado = EstadoDeCarga.Carregando;
        _erroDaCarga = null;
        if (await Despachante.EnviarAsync(new ObterMeuAcesso()) is not { EhDiretoria: true })
        {
            _estado = EstadoDeCarga.AcessoNegado;
            return;
        }

        await CarregarAsync();
        if (_estado == EstadoDeCarga.Carregando) _estado = EstadoDeCarga.Pronto;
    }
```

Em `CarregarAsync`, as quatro saídas trocam de campo, sem mudar a ordem nem as mensagens:
- `ObterProcesso` com acesso negado: `_autorizado = false;` → `_estado = EstadoDeCarga.AcessoNegado;`
- processo não encontrado: `_naoEncontrado = true;` → `_estado = EstadoDeCarga.NaoEncontrado;`
- falha do processo: `_erroDaConsulta = resultado.Mensagem ?? "Não foi possível consultar o processo.";` → `_estado = EstadoDeCarga.Falhou;` e `_erroDaCarga = resultado.Mensagem ?? "Não foi possível consultar o processo.";` (no `else` com chaves)
- `ListarAreasAtuacao` com acesso negado: `_autorizado = false;` → `_estado = EstadoDeCarga.AcessoNegado;`
- falha das áreas: `_erroDaConsulta = areas.Mensagem ?? "Não foi possível consultar as áreas de atuação.";` → `_estado = EstadoDeCarga.Falhou;` e `_erroDaCarga = areas.Mensagem ?? "Não foi possível consultar as áreas de atuação.";`

Em `ProcurarClientesAsync` e `SalvarAsync`, o ramo de acesso negado troca `_autorizado = false;` por `_estado = EstadoDeCarga.AcessoNegado;`.

- [ ] **Step 5: O formulário de usuário usa o componente**

Em `src/AtividadesJuridicas.Web/Components/Pages/UsuarioFormulario.razor`, a marcação. O começo:

```razor
@if (!_carregado)
{
    <p class="aj-subtitulo" role="status">Carregando…</p>
}
else if (!_autorizado)
{
    <Aviso Tipo="TipoDeAviso.Erro">Acesso negado.</Aviso>
}
else if (_erroDosPerfis is not null)
{
    <Aviso Tipo="TipoDeAviso.Erro">@_erroDosPerfis</Aviso>
}
else if (_erroDaConsulta is not null)
{
    <Aviso Tipo="TipoDeAviso.Erro">@_erroDaConsulta</Aviso>
}
else if (_naoEncontrado)
{
    <Aviso Tipo="TipoDeAviso.Erro">Usuário não encontrado.</Aviso>
}
else
{
    @if (_mensagem is not null)
```

passa a:

```razor
<CargaDoFormulario Estado="_estado" Erro="@_erroDaCarga" TextoNaoEncontrado="Usuário não encontrado." TentarDeNovo="TentarDeNovoAsync">
    @if (_mensagem is not null)
```

Na `ConfirmacaoDeInativacao`, `OnAcessoEncerrado="() => _autorizado = false"` passa a `OnAcessoEncerrado="() => _estado = EstadoDeCarga.AcessoNegado"`. O resto fica igual (inclusive o painel de situação do tablet e do mobile e o `<form id="busca-de-cliente" hidden …>`), e o `}` logo antes de `@code {` passa a `</CargaDoFormulario>`.

O código. Saem `_carregado`, `_autorizado`, `_naoEncontrado`, `_erroDaConsulta` e `_erroDosPerfis`; entram:

```csharp
    private EstadoDeCarga _estado;
    private string? _erroDaCarga;

    // Os perfis chegaram: só então há usuário a carregar.
    private bool _perfisProntos;
```

O subtítulo e a carga (trocam `SubtituloDoFormulario`, `OnInitializedAsync` e `OnParametersSetAsync`; a carga continua dividida como hoje: os perfis ao iniciar, o usuário a cada troca de `Id`):

```csharp
    // O subtítulo só acompanha o formulário: sobre "Acesso negado.", "Usuário não encontrado." ou um erro de consulta ele sobra.
    private string? SubtituloDoFormulario =>
        _estado == EstadoDeCarga.Pronto ? "Escolha o perfil. Para um usuário de cliente, indique o cliente." : null;

    protected override Task OnInitializedAsync() => CarregarPerfisAsync();

    // O usuário vem da rota: se o Id mudar com a página montada, a tela carrega o novo, não grava um sobre os dados do outro.
    protected override async Task OnParametersSetAsync()
    {
        if (_usuarioCarregado && Id == _idCarregado) return;

        _usuarioCarregado = true;
        _idCarregado = Id;
        await CarregarUsuarioAsync();
    }

    // "Tentar de novo": a carga inteira, do acesso de quem está na tela ao usuário da rota.
    private async Task TentarDeNovoAsync()
    {
        _estado = EstadoDeCarga.Carregando;
        _erroDaCarga = null;
        await CarregarPerfisAsync();
        await CarregarUsuarioAsync();
    }

    private async Task CarregarPerfisAsync()
    {
        _perfisProntos = false;
        if (await Despachante.EnviarAsync(new ObterMeuAcesso()) is not { EhDiretoria: true })
        {
            _estado = EstadoDeCarga.AcessoNegado;
            return;
        }

        var perfis = await Despachante.EnviarAsync(new ListarPerfis());
        _perfis = perfis.Valor ?? [];
        if (perfis.Erro == TipoErro.AcessoNegado) _estado = EstadoDeCarga.AcessoNegado;
        else if (!perfis.Sucesso)
        {
            _estado = EstadoDeCarga.Falhou;
            _erroDaCarga = perfis.Mensagem ?? "Não foi possível consultar os perfis.";
        }
        else _perfisProntos = true;
    }

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
```

Em `CarregarAsync(Guid id)`, as três saídas trocam de campo:
- `ListarUsuarios` com acesso negado: `_autorizado = false;` → `_estado = EstadoDeCarga.AcessoNegado;`
- falha da lista: `_erroDaConsulta = lista.Mensagem ?? "Não foi possível consultar os usuários.";` → `_estado = EstadoDeCarga.Falhou;` e `_erroDaCarga = lista.Mensagem ?? "Não foi possível consultar os usuários.";`
- usuário fora da lista: `_naoEncontrado = true;` → `_estado = EstadoDeCarga.NaoEncontrado;`

Em `BuscarClientesAsync`, `SalvarAsync`, `AlternarAtivoAsync` e `AoInativar`, o ramo de acesso negado troca `_autorizado = false;` por `_estado = EstadoDeCarga.AcessoNegado;`.

- [ ] **Step 6: Trava e testes**

Acrescente a `Migradas`, em `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`, na ordem alfabética (entre `CabecalhoDePagina.razor` e `CartaoAtalho.razor`): `"Components/Layout/CargaDoFormulario.razor",`.

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Expected: todos passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~CargaDoFormularioInterativaTests|FullyQualifiedName~ClienteFormulario|FullyQualifiedName~ProcessoFormulario|FullyQualifiedName~UsuariosWebTests|FullyQualifiedName~ComponentesDoDesignSystemTests|FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~AutenticacaoWebTests"`
Expected: todos passando; os de `ClienteFormulario*` e `ProcessoFormulario*` sem nenhuma mudança, e os de `UsuariosWebTests` com só as duas asserções reescritas no Step 1.

- [ ] **Step 7: Commit**

```
git branch --show-current
git add src tests
git commit -m "feat(web): estado de carregamento dos formulários em componente, com tentar de novo nos de cliente, processo e usuário"
```

---

### Task 4: Peças compartilhadas das telas de atividade

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Layout/CabecalhoDePagina.razor` (`Alerta`), `CabecalhoDeDetalhe.razor` (`RotuloDoVoltarNoMobile`), `BarraDeVoltar.razor` (`Acao`), `Paginacao.razor` (`ComSetas`), `Icone.razor` (`enviar`, `planilha`, `texto`)
- Modify: `src/AtividadesJuridicas.Web/Components/Atividades/EtiquetaDeSituacao.razor` (`NaTabela`), `RotulosDeAtividade.cs`
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Atividades.razor` (só `NaTabela="true"` na tabela de hoje, para os testes atuais continuarem passando com o padrão novo da etiqueta)
- Create: `src/AtividadesJuridicas.Web/Components/Atividades/AvatarDePessoa.cs`, `TextosDeAtividade.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/TelasPurasTests.cs` (os textos: `RotulosDeAtividadeTests`, `AvatarDePessoaTests`, `TextosDeAtividadeTests`) e `ComponentesDoDesignSystemTests.cs` (o HTML de cada peça)
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (`Migradas`: `EtiquetaDeSituacao.razor`)

**Interfaces:**
- Produces:
  - `CabecalhoDePagina`: `[Parameter] public string? Alerta` — com `Alerta`, o subtítulo do cabeçalho e o da barra do mobile terminam em ` · <strong class="aj-subtitulo__alerta">{Alerta}</strong>` (o `Subtitulo` leva só a primeira parte, `8 atividades`).
  - `CabecalhoDeDetalhe`: `[Parameter] public string? RotuloDoVoltarNoMobile` — o `aria-label` do voltar da barra do mobile; nulo = `Voltar para {Origem}` (como hoje).
  - `BarraDeVoltar`: `[Parameter] public RenderFragment? Acao` — no lugar do `aj-barra-superior__vazio` quando presente. `CabecalhoDeDetalhe` e `CabecalhoDeFicha` não mudam de comportamento.
  - `Paginacao`: `[Parameter] public bool ComSetas` — a linha leva `aj-paginacao--com-setas`; "Anterior" é `aj-botao aj-botao--secundario aj-paginacao__anterior` com o ícone `voltar` antes do texto; "Próxima", `… aj-paginacao__proxima` com o ícone `avancar` depois.
  - `Icone`: os traçados `enviar` (`M4 12l16-8-6 16-2.5-6.5z`), `planilha` e `texto` (os do quadro da ficha, `quadros-ficha.md` 1.7.5).
  - `EtiquetaDeSituacao`: `[Parameter] public bool NaTabela` — na tabela, a Concluída leva `aj-etiqueta--com-icone` e o ícone `confirmar`; a Atrasada, o relógio (como hoje). Fora da tabela (padrão), todas com ponto: a Atrasada leva `aj-etiqueta--com-ponto` e nenhum ícone.
  - `RotulosDeAtividade.SituacaoNoPlural(StatusAtividade)`: `Criadas`, `Direcionadas`, `Feitas`, `Para correção`, `Corrigidas`, `Concluídas`, `Reabertas`.
  - `AvatarDePessoa.Sigla(string nome)` (iniciais do primeiro e do último nome, maiúsculas; nome de uma palavra, as duas primeiras letras; vazio, `?`) e `AvatarDePessoa.Classe(string nome)` (`aj-avatar--azul`, `--verde`, `--ambar` ou `--lilas`, pela soma dos códigos dos caracteres do nome aparado, módulo 4, nessa ordem).
  - `TextosDeAtividade.Entrega(DateOnly? data, string? area, bool curta)`: `Entrega em 09/10/2026 · Trabalhista` (curta: `Entrega 09/10/2026 · Trabalhista`); sem área, sem a parte da área; sem data, `Sem data de entrega` (com a área depois, se houver). `TextosDeAtividade.Mudanca(StatusAtividade? anterior, StatusAtividade nova)`: `Criada` ou `Criada → Direcionada`. `TextosDeAtividade.DataCurta(FusoDoEscritorio fuso, DateTime utc)`: `07/10 16:42` no horário do escritório. `TextosDeAtividade.Contagem(int n)` (`1 atividade`, `8 atividades`) e `TextosDeAtividade.Atrasadas(int m)` (`1 atrasada`, `2 atrasadas`; nulo com 0).

**Regras:**
- Nenhuma tela que já usa essas peças muda de marcação sem o parâmetro novo (os testes da 9A–9D passam sem mudança). A exceção é a etiqueta, cujo padrão passa a ser o de fora da tabela: a tabela da lista de hoje pede `NaTabela="true"` (a Atrasada continua com o relógio), e a ficha antiga, apagada na Tarefa 10, fica com o padrão (ponto).
- A conta da cor do avatar é estável entre execuções (não use `GetHashCode` de string).
- Cada peça com teste: o HTML com e sem o parâmetro novo; os textos com os casos de borda (zero, um, muitos; nome de uma palavra; sem data; sem área).

- [ ] **Step 1: Testes (devem falhar)**

Em `tests/AtividadesJuridicas.IntegrationTests/TelasPurasTests.cs`, acrescente os `using` `AtividadesJuridicas.Domain.Atividades` e `AtividadesJuridicas.Web.Components.Atividades`, e as três classes ao fim do arquivo:

```csharp
public class RotulosDeAtividadeTests
{
    [Theory]
    [InlineData(StatusAtividade.Criada, "Criadas")]
    [InlineData(StatusAtividade.Direcionada, "Direcionadas")]
    [InlineData(StatusAtividade.Feita, "Feitas")]
    [InlineData(StatusAtividade.ParaCorrecao, "Para correção")]
    [InlineData(StatusAtividade.Corrigida, "Corrigidas")]
    [InlineData(StatusAtividade.Concluida, "Concluídas")]
    [InlineData(StatusAtividade.Reaberta, "Reabertas")]
    public void Situacao_no_plural_e_o_nome_do_chip(StatusAtividade status, string plural)
    {
        RotulosDeAtividade.SituacaoNoPlural(status).Should().Be(plural);
    }
}

public class AvatarDePessoaTests
{
    [Theory]
    [InlineData("Beatriz Lins", "BL")]
    [InlineData("Otávio Brandão", "OB")]
    [InlineData("Maria de Fátima Souza", "MS")]
    [InlineData("  ana   lima  ", "AL")]
    [InlineData("Marina", "MA")]
    [InlineData("é", "É")]
    [InlineData("", "?")]
    [InlineData("   ", "?")]
    public void Sigla_sao_as_iniciais_do_primeiro_e_do_ultimo_nome(string nome, string sigla)
    {
        AvatarDePessoa.Sigla(nome).Should().Be(sigla);
    }

    // A soma dos códigos dos caracteres, módulo 4: 1128, 1117, 1154 e 1159 dão 0, 1, 2 e 3.
    [Theory]
    [InlineData("Rafael Teles", "aj-avatar--azul")]
    [InlineData("Camila Prado", "aj-avatar--verde")]
    [InlineData("Marina Couto", "aj-avatar--ambar")]
    [InlineData("Beatriz Lins", "aj-avatar--lilas")]
    public void Classe_sai_da_soma_dos_codigos_do_nome(string nome, string classe)
    {
        AvatarDePessoa.Classe(nome).Should().Be(classe);
    }

    [Fact]
    public void Classe_nao_muda_com_espacos_nas_pontas_e_nome_vazio_e_azul()
    {
        AvatarDePessoa.Classe("  Marina Couto ").Should().Be("aj-avatar--ambar");
        AvatarDePessoa.Classe("").Should().Be("aj-avatar--azul");
    }
}

public class TextosDeAtividadeTests
{
    private readonly FusoDoEscritorio _fuso = new("America/Sao_Paulo");

    [Theory]
    [InlineData(false, "Entrega em 09/10/2026 · Trabalhista")]
    [InlineData(true, "Entrega 09/10/2026 · Trabalhista")]
    public void Entrega_com_data_e_area(bool curta, string esperado)
    {
        TextosDeAtividade.Entrega(new DateOnly(2026, 10, 9), "Trabalhista", curta).Should().Be(esperado);
    }

    [Fact]
    public void Entrega_sem_area_ou_sem_data()
    {
        TextosDeAtividade.Entrega(new DateOnly(2026, 10, 9), null, curta: false).Should().Be("Entrega em 09/10/2026");
        TextosDeAtividade.Entrega(new DateOnly(2026, 10, 9), "  ", curta: true).Should().Be("Entrega 09/10/2026");
        TextosDeAtividade.Entrega(null, "Trabalhista", curta: false).Should().Be("Sem data de entrega · Trabalhista");
        TextosDeAtividade.Entrega(null, null, curta: true).Should().Be("Sem data de entrega");
    }

    [Fact]
    public void Mudanca_do_primeiro_registro_e_so_a_situacao()
    {
        TextosDeAtividade.Mudanca(null, StatusAtividade.Criada).Should().Be("Criada");
        TextosDeAtividade.Mudanca(StatusAtividade.Criada, StatusAtividade.Direcionada).Should().Be("Criada → Direcionada");
        TextosDeAtividade.Mudanca(StatusAtividade.Feita, StatusAtividade.ParaCorrecao).Should().Be("Feita → Para correção");
    }

    [Fact]
    public void Data_curta_no_horario_do_escritorio()
    {
        TextosDeAtividade.DataCurta(_fuso, new DateTime(2026, 10, 7, 19, 42, 0, DateTimeKind.Utc)).Should().Be("07/10 16:42");
        // Perto da meia-noite vale o dia do escritório, não o de Greenwich.
        TextosDeAtividade.DataCurta(_fuso, new DateTime(2026, 10, 8, 2, 30, 0, DateTimeKind.Utc)).Should().Be("07/10 23:30");
    }

    [Theory]
    [InlineData(0, "0 atividades", null)]
    [InlineData(1, "1 atividade", "1 atrasada")]
    [InlineData(8, "8 atividades", "8 atrasadas")]
    public void Contagem_e_atrasadas_no_numero_certo(int quantidade, string contagem, string? atrasadas)
    {
        TextosDeAtividade.Contagem(quantidade).Should().Be(contagem);
        TextosDeAtividade.Atrasadas(quantidade).Should().Be(atrasadas);
    }
}
```

Em `tests/AtividadesJuridicas.IntegrationTests/ComponentesDoDesignSystemTests.cs`, acrescente os `using` `AtividadesJuridicas.Domain.Atividades` e `AtividadesJuridicas.Web.Components.Atividades`. No `Icone_do_design_system_tem_tracado`, três linhas de dados a mais, depois de `[InlineData("erro")]`:

```csharp
    [InlineData("enviar")]
    [InlineData("planilha")]
    [InlineData("texto")]
```

E os testes novos, ao fim da classe (antes de `Desenho_estatico_aceita_formulario_com_nome`):

```csharp
    [Fact]
    public async Task Icones_da_ficha_da_atividade_tem_os_tracados_do_quadro()
    {
        var enviar = await TelaDeTeste.RenderizarSemDadosAsync<Icone>(new() { ["Nome"] = "enviar" });
        var planilha = await TelaDeTeste.RenderizarSemDadosAsync<Icone>(new() { ["Nome"] = "planilha" });
        var texto = await TelaDeTeste.RenderizarSemDadosAsync<Icone>(new() { ["Nome"] = "texto" });

        enviar.Should().Contain("<path d=\"M4 12l16-8-6 16-2.5-6.5z\"></path>");
        planilha.Should().Contain(
            "<path d=\"M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z\"></path><path d=\"M14 3v5h5\"></path><path d=\"M8 12h8v6H8z\"></path><path d=\"M12 12v6\"></path>");
        texto.Should().Contain(
            "<path d=\"M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z\"></path><path d=\"M14 3v5h5\"></path><path d=\"M9 13h6\"></path><path d=\"M9 17h4\"></path>");
    }

    [Fact]
    public async Task Cabecalho_com_alerta_termina_o_subtitulo_e_o_da_barra_com_o_destaque()
    {
        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDePagina>>(
            new() { ["Titulo"] = "Atividades", ["Subtitulo"] = "8 atividades", ["Alerta"] = "1 atrasada" }));

        BlocoDeVisao.De(html, "aj-desktop-tablet").Should().Contain(
            "<p class=\"aj-subtitulo\">8 atividades · <strong class=\"aj-subtitulo__alerta\">1 atrasada</strong></p>");
        BarraDaTela(html).Should().Contain(
            "<span class=\"aj-subtitulo\">8 atividades · <strong class=\"aj-subtitulo__alerta\">1 atrasada</strong></span>");
    }

    [Fact]
    public async Task Cabecalho_sem_alerta_fica_so_com_o_subtitulo()
    {
        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDePagina>>(
            new() { ["Titulo"] = "Atividades", ["Subtitulo"] = "8 atividades" }));

        html.Should().Contain("<p class=\"aj-subtitulo\">8 atividades</p>")
            .And.Contain("<span class=\"aj-subtitulo\">8 atividades</span>")
            .And.NotContain("aj-subtitulo__alerta");
    }

    [Fact]
    public async Task Cabecalho_de_detalhe_leva_o_rotulo_proprio_ao_voltar_da_barra_do_mobile()
    {
        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<CabecalhoDeDetalhe>>(new()
        {
            ["Titulo"] = "Editar atividade", ["VoltarPara"] = "/atividades/0199a3f2-7c1e-7000-8000-000000000001",
            ["Origem"] = "Minuta de acordo trabalhista", ["RotuloDoVoltarNoMobile"] = "Voltar para a atividade",
        }));

        html.Should().MatchRegex(
            "<header class=\"aj-barra-superior aj-barra-superior--voltar\">\\s*<a href=\"/atividades/0199a3f2-7c1e-7000-8000-000000000001\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Voltar para a atividade\">")
            .And.NotContain("Voltar para Minuta de acordo trabalhista");
        // O voltar do desktop e do tablet continua com a origem por extenso.
        BlocoDeVisao.De(html, "aj-desktop-tablet").Should().MatchRegex(
            "(?s)<a href=\"/atividades/0199a3f2-7c1e-7000-8000-000000000001\" class=\"aj-voltar\">\\s*<svg[^>]*>.*?</svg>\\s*Minuta de acordo trabalhista</a>");
    }

    [Fact]
    public async Task Barra_de_voltar_com_acao_poe_a_acao_no_lugar_do_vazio()
    {
        RenderFragment acao = c => c.AddMarkupContent(0, "<div class=\"aj-menu-acoes\">Mais ações</div>");

        var comAcao = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<BarraDeVoltar>>(new()
        {
            ["Titulo"] = "Atividade", ["VoltarPara"] = "/atividades", ["Rotulo"] = "Voltar para Atividades", ["Acao"] = acao,
        }));
        var semAcao = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<ComBarraSuperior<BarraDeVoltar>>(new()
        {
            ["Titulo"] = "Atividade", ["VoltarPara"] = "/atividades", ["Rotulo"] = "Voltar para Atividades",
        }));

        comAcao.Should().MatchRegex(
            "(?s)<header class=\"aj-barra-superior aj-barra-superior--voltar\">.*?<h1>Atividade</h1>\\s*<div class=\"aj-menu-acoes\">Mais ações</div>\\s*</header>")
            .And.NotContain("aj-barra-superior__vazio");
        semAcao.Should().MatchRegex(
            "(?s)<h1>Atividade</h1>\\s*<span class=\"aj-barra-superior__vazio\" aria-hidden=\"true\"></span>\\s*</header>");
    }

    [Fact]
    public async Task Paginacao_com_setas_traz_a_seta_de_cada_lado_e_trava_as_pontas()
    {
        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<Paginacao>(new()
        {
            ["Numero"] = 1, ["TotalDePaginas"] = 3, ["Total"] = 45, ["Singular"] = "atividade", ["Plural"] = "atividades",
            ["ComSetas"] = true,
        }));

        html.Should().Contain("<div class=\"aj-paginacao aj-paginacao--com-setas\">")
            .And.Contain("<span class=\"aj-subtitulo\">Página 1 de 3 · 45 atividades</span>")
            .And.MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--secundario aj-paginacao__anterior\"[^>]*>\\s*<svg[^>]*>\\s*<path d=\"M15 6l-6 6 6 6\"></path>\\s*</svg>\\s*Anterior</button>")
            .And.MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--secundario aj-paginacao__proxima\"[^>]*>\\s*Próxima\\s*<svg[^>]*>\\s*<path d=\"M9 6l6 6-6 6\"></path>\\s*</svg>\\s*</button>");
        Regex.IsMatch(html, "aj-paginacao__anterior\"[^>]*disabled").Should().BeTrue();
        Regex.IsMatch(html, "aj-paginacao__proxima\"[^>]*disabled").Should().BeFalse();
    }

    [Fact]
    public async Task Etiqueta_na_tabela_traz_o_visto_na_concluida_e_o_relogio_na_atrasada()
    {
        var concluida = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<EtiquetaDeSituacao>(
            new() { ["Status"] = StatusAtividade.Concluida, ["NaTabela"] = true }));
        var atrasada = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<EtiquetaDeSituacao>(
            new() { ["Status"] = StatusAtividade.Direcionada, ["Atrasada"] = true, ["NaTabela"] = true }));

        concluida.Should().MatchRegex(
            "<span class=\"aj-etiqueta aj-etiqueta--concluida aj-etiqueta--com-icone\">\\s*<svg[^>]*>\\s*<path d=\"M5 12.5l4.5 4.5L19 7.5\"></path>\\s*</svg>\\s*Concluída</span>");
        atrasada.Should().MatchRegex(
            "<span class=\"aj-etiqueta aj-etiqueta--direcionada\">Direcionada</span>\\s*<span class=\"aj-etiqueta aj-etiqueta--atrasada\">\\s*<svg[^>]*>\\s*<circle cx=\"12\" cy=\"12\" r=\"9\"></circle>")
            .And.NotContain("aj-etiqueta--com-ponto");
    }

    [Fact]
    public async Task Etiqueta_fora_da_tabela_fica_com_o_ponto_em_todas_e_sem_icone()
    {
        var concluida = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<EtiquetaDeSituacao>(
            new() { ["Status"] = StatusAtividade.Concluida }));
        var atrasada = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<EtiquetaDeSituacao>(
            new() { ["Status"] = StatusAtividade.Direcionada, ["Atrasada"] = true }));
        var noPrazo = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarSemDadosAsync<EtiquetaDeSituacao>(
            new() { ["Status"] = StatusAtividade.Criada }));

        concluida.Should().Contain("<span class=\"aj-etiqueta aj-etiqueta--concluida\">Concluída</span>").And.NotContain("<svg");
        atrasada.Should().MatchRegex(
            "<span class=\"aj-etiqueta aj-etiqueta--direcionada\">Direcionada</span>\\s*<span class=\"aj-etiqueta aj-etiqueta--atrasada aj-etiqueta--com-ponto\">Atrasada</span>")
            .And.NotContain("<svg");
        noPrazo.Should().Contain("<span class=\"aj-etiqueta aj-etiqueta--criada\">Criada</span>").And.NotContain("Atrasada");
    }
```

Run: `dotnet build -m:1`
Expected: não compila (`AvatarDePessoa`, `TextosDeAtividade` e `RotulosDeAtividade.SituacaoNoPlural` não existem). É o RED. Os testes de HTML (parâmetros `Alerta`, `RotuloDoVoltarNoMobile`, `Acao`, `ComSetas`, `NaTabela` e os ícones novos) compilam, mas falhariam: o desenho estático recusa parâmetro que o componente não declara, e o ícone desconhecido sai vazio.

- [ ] **Step 2: Os textos**

Em `src/AtividadesJuridicas.Web/Components/Atividades/RotulosDeAtividade.cs`, logo depois de `Status`:

```csharp
    /// <summary>O nome do chip de filtro: "Direcionadas", "Para correção".</summary>
    public static string SituacaoNoPlural(StatusAtividade status) => status switch
    {
        StatusAtividade.Criada => "Criadas",
        StatusAtividade.Direcionada => "Direcionadas",
        StatusAtividade.Feita => "Feitas",
        StatusAtividade.ParaCorrecao => "Para correção",
        StatusAtividade.Corrigida => "Corrigidas",
        StatusAtividade.Concluida => "Concluídas",
        StatusAtividade.Reaberta => "Reabertas",
        _ => status.ToString(),
    };
```

`src/AtividadesJuridicas.Web/Components/Atividades/AvatarDePessoa.cs`:

```csharp
namespace AtividadesJuridicas.Web.Components.Atividades;

/// <summary>
/// O avatar de uma pessoa nas telas de atividade (cartões, responsáveis, comentários). A sigla e a cor saem do nome,
/// porque o resumo da atividade só traz nomes. A cor é estável entre execuções: vem da soma dos códigos dos
/// caracteres, e não do GetHashCode de string, que muda a cada execução.
/// </summary>
public static class AvatarDePessoa
{
    private static readonly string[] Cores = ["aj-avatar--azul", "aj-avatar--verde", "aj-avatar--ambar", "aj-avatar--lilas"];

    /// <summary>
    /// As iniciais do primeiro e do último nome ("Beatriz Lins" → "BL"); de um nome só, as duas primeiras letras;
    /// sem nome, "?".
    /// </summary>
    public static string Sigla(string nome)
    {
        var partes = nome.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return partes.Length switch
        {
            0 => "?",
            1 => partes[0][..Math.Min(2, partes[0].Length)].ToUpperInvariant(),
            _ => $"{partes[0][0]}{partes[^1][0]}".ToUpperInvariant(),
        };
    }

    /// <summary>A classe de cor do avatar: a soma dos códigos dos caracteres do nome aparado, módulo 4.</summary>
    public static string Classe(string nome) => Cores[(int)(nome.Trim().Sum(c => (long)c) % Cores.Length)];
}
```

`src/AtividadesJuridicas.Web/Components/Atividades/TextosDeAtividade.cs`:

```csharp
using System.Globalization;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Web.Infra;

namespace AtividadesJuridicas.Web.Components.Atividades;

/// <summary>As frases das telas de atividade montadas a partir dos dados: a entrega, a mudança de situação, a data curta e as contagens.</summary>
public static class TextosDeAtividade
{
    /// <summary>
    /// A linha de situação da ficha: "Entrega em 09/10/2026 · Trabalhista" (curta, no mobile: "Entrega 09/10/2026 ·
    /// Trabalhista"). Sem área, sem a parte da área; sem data, "Sem data de entrega".
    /// </summary>
    public static string Entrega(DateOnly? data, string? area, bool curta)
    {
        var entrega = data is { } dia
            ? $"{(curta ? "Entrega" : "Entrega em")} {dia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}"
            : "Sem data de entrega";
        return string.IsNullOrWhiteSpace(area) ? entrega : $"{entrega} · {area}";
    }

    /// <summary>O registro do histórico: "Criada" no primeiro, "Criada → Direcionada" nos outros.</summary>
    public static string Mudanca(StatusAtividade? anterior, StatusAtividade nova) =>
        anterior is { } de
            ? $"{RotulosDeAtividade.Status(de)} → {RotulosDeAtividade.Status(nova)}"
            : RotulosDeAtividade.Status(nova);

    /// <summary>Dia, mês e hora no horário do escritório, sem o ano: "07/10 16:42" (tablet e mobile).</summary>
    public static string DataCurta(FusoDoEscritorio fuso, DateTime utc) =>
        fuso.ParaLocal(utc).ToString("dd/MM HH:mm", CultureInfo.InvariantCulture);

    // Dentro desta classe, "Contagem" é o método abaixo: a classe de Infra vai pelo nome completo.

    /// <summary>O subtítulo da lista: "1 atividade", "8 atividades".</summary>
    public static string Contagem(int n) => global::AtividadesJuridicas.Web.Infra.Contagem.De(n, "atividade", "atividades");

    /// <summary>A parte de alerta do subtítulo: "1 atrasada", "2 atrasadas"; nula quando não há nenhuma.</summary>
    public static string? Atrasadas(int m) =>
        m > 0 ? global::AtividadesJuridicas.Web.Infra.Contagem.De(m, "atrasada", "atrasadas") : null;
}
```

- [ ] **Step 3: As peças de layout**

`Icone.razor`: três entradas a mais no dicionário `Tracados`, depois de `["documento"]` (os traçados do quadro da ficha, `FichaDaAtividade.dc.html` e `FichaTablet.dc.html`):

```csharp
        ["texto"] = new("<path d=\"M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z\"></path><path d=\"M14 3v5h5\"></path><path d=\"M9 13h6\"></path><path d=\"M9 17h4\"></path>"),
        ["planilha"] = new("<path d=\"M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z\"></path><path d=\"M14 3v5h5\"></path><path d=\"M8 12h8v6H8z\"></path><path d=\"M12 12v6\"></path>"),
        ["enviar"] = new("<path d=\"M4 12l16-8-6 16-2.5-6.5z\"></path>"),
```

`CabecalhoDePagina.razor`: o subtítulo do desktop e do tablet,

```razor
                    <p class="aj-subtitulo">@Subtitulo</p>
```

passa a:

```razor
                    <p class="aj-subtitulo">@Subtitulo@ParteDoAlerta</p>
```

e o da barra do mobile,

```razor
                        <span class="aj-subtitulo">@subtituloMobile</span>
```

passa a:

```razor
                        <span class="aj-subtitulo">@subtituloMobile@ParteDoAlerta</span>
```

No `@code`, depois de `SubtituloMobile`:

```csharp
    /// <summary>
    /// A parte de alerta do subtítulo ("1 atrasada"), depois de um " · ", num strong que o design system pinta de
    /// vermelho no tablet e no mobile. O <see cref="Subtitulo"/> leva só a primeira parte ("8 atividades").
    /// </summary>
    [Parameter] public string? Alerta { get; set; }
```

e, antes de `ExibirMenuAsync`:

```csharp
    // O separador vai como texto do próprio fragmento: escrito na marcação, o compilador do Razor poderia aparar o
    // espaço da ponta. Sem alerta, nada é emitido e o subtítulo fica como era.
    private RenderFragment ParteDoAlerta => construtor =>
    {
        if (Alerta is null) return;

        construtor.AddContent(0, " · ");
        construtor.OpenElement(1, "strong");
        construtor.AddAttribute(2, "class", "aj-subtitulo__alerta");
        construtor.AddContent(3, Alerta);
        construtor.CloseElement();
    };
```

(Se o Razor não aceitar `@Subtitulo@ParteDoAlerta` colados, escreva `@(Subtitulo)@(ParteDoAlerta)`; o HTML do teste é o mesmo.)

`CabecalhoDeDetalhe.razor` inteiro:

```razor
@* Cabeçalho de ficha e de formulário: no desktop e no tablet, o voltar acima do cabeçalho; no mobile, a barra de
   voltar, que esconde a barra inferior. Origem é o nome da tela de onde se veio ("Usuários"). *@
<div class="aj-desktop-tablet">
    <div><a href="@VoltarPara" class="aj-voltar"><Icone Nome="voltar" />@Origem</a></div>
</div>
<CabecalhoDePagina Titulo="@Titulo" Subtitulo="@Subtitulo" Folgado="@(Subtitulo is not null)"
                   VoltarPara="@VoltarPara" RotuloDoVoltar="@(RotuloDoVoltarNoMobile ?? $"Voltar para {Origem}")" />

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
}
```

`BarraDeVoltar.razor` inteiro:

```razor
@using Microsoft.AspNetCore.Components.Sections

@* A barra superior do mobile das fichas e dos formulários: voltar, o título no meio e, à direita, a ação da tela
   (o "Mais ações" da ficha da atividade) ou um vazio do tamanho do botão. Com ela na tela, o layout esconde a barra
   inferior. *@
<SectionContent SectionName="@Encaixes.BarraSuperior">
    <header class="aj-barra-superior aj-barra-superior--voltar">
        <a href="@VoltarPara" class="aj-botao aj-botao--fantasma aj-botao--icone" aria-label="@Rotulo"><Icone Nome="voltar" /></a>
        <h1>@Titulo</h1>
        @if (Acao is not null)
        {
            @Acao
        }
        else
        {
            <span class="aj-barra-superior__vazio" aria-hidden="true"></span>
        }
    </header>
</SectionContent>

@code {
    [Parameter, EditorRequired] public string Titulo { get; set; } = string.Empty;
    [Parameter, EditorRequired] public string VoltarPara { get; set; } = string.Empty;

    /// <summary>O rótulo acessível do voltar: "Voltar para Clientes".</summary>
    [Parameter, EditorRequired] public string Rotulo { get; set; } = string.Empty;

    /// <summary>A ação à direita do título (um <c>MenuDeAcoes</c>). Sem ela, o vazio que centraliza o título.</summary>
    [Parameter] public RenderFragment? Acao { get; set; }
}
```

`Paginacao.razor` inteiro:

```razor
@* A linha de paginação do design system, ao fim da lista. Com setas (a lista de atividades), cada botão leva a seta
   do lado para onde vai. *@
@if (SempreVisivel || TotalDePaginas > 1)
{
    <div class="@(ComSetas ? "aj-paginacao aj-paginacao--com-setas" : "aj-paginacao")">
        <span class="aj-subtitulo">@Texto</span>
        <div class="aj-paginacao__botoes">
            @if (ComSetas)
            {
                <button type="button" class="aj-botao aj-botao--secundario aj-paginacao__anterior" disabled="@(Ocupado || Numero <= 1)" @onclick="() => IrPara.InvokeAsync(Numero - 1)"><Icone Nome="voltar" />Anterior</button>
                <button type="button" class="aj-botao aj-botao--secundario aj-paginacao__proxima" disabled="@(Ocupado || Numero >= TotalDePaginas)" @onclick="() => IrPara.InvokeAsync(Numero + 1)">Próxima<Icone Nome="avancar" /></button>
            }
            else
            {
                <button type="button" class="aj-botao aj-botao--secundario" disabled="@(Ocupado || Numero <= 1)" @onclick="() => IrPara.InvokeAsync(Numero - 1)">Anterior</button>
                <button type="button" class="aj-botao aj-botao--secundario" disabled="@(Ocupado || Numero >= TotalDePaginas)" @onclick="() => IrPara.InvokeAsync(Numero + 1)">Próxima</button>
            }
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

    /// <summary>Os botões com as setas e a linha que quebra (o quadro da lista de atividades).</summary>
    [Parameter] public bool ComSetas { get; set; }

    [Parameter] public EventCallback<int> IrPara { get; set; }

    private string Texto => $"Página {Numero} de {TotalDePaginas} · {Contagem.De(Total, Singular, Plural)}";
}
```

`src/AtividadesJuridicas.Web/Components/Atividades/EtiquetaDeSituacao.razor` inteiro:

```razor
@* A situação da atividade como etiqueta do design system; a entrega vencida ganha a etiqueta Atrasada ao lado.
   Na tabela (quadro do desktop), a Concluída leva o visto e a Atrasada o relógio, sem ponto. Fora dela (cartões e
   ficha), todas com ponto e nenhum ícone. *@
@if (NaTabela && Status == StatusAtividade.Concluida)
{
    <span class="aj-etiqueta aj-etiqueta--concluida aj-etiqueta--com-icone"><Icone Nome="confirmar" />@RotulosDeAtividade.Status(Status)</span>
}
else
{
    <span class="aj-etiqueta aj-etiqueta--@RotulosDeAtividade.ClasseDaEtiqueta(Status)">@RotulosDeAtividade.Status(Status)</span>
}
@if (Atrasada)
{
    @if (NaTabela)
    {
        <span class="aj-etiqueta aj-etiqueta--atrasada"><Icone Nome="atrasada" />Atrasada</span>
    }
    else
    {
        <span class="aj-etiqueta aj-etiqueta--atrasada aj-etiqueta--com-ponto">Atrasada</span>
    }
}

@code {
    [Parameter, EditorRequired] public StatusAtividade Status { get; set; }

    [Parameter] public bool Atrasada { get; set; }

    /// <summary>Na tabela do desktop: o visto na Concluída e o relógio na Atrasada. Fora dela, o ponto em todas.</summary>
    [Parameter] public bool NaTabela { get; set; }
}
```

A tabela da lista de hoje (`Pages/Atividades.razor`, a célula de situação) passa a pedir a forma da tabela, para continuar com o relógio na Atrasada até a Tarefa 6 reescrever a tela (o teste `AtividadesWebTests` que procura `<span class="aj-etiqueta aj-etiqueta--atrasada">` continua valendo). Troque:

```razor
                        <td><EtiquetaDeSituacao Status="atividade.Status" Atrasada="atividade.Atrasada" /></td>
```

por:

```razor
                        <td><EtiquetaDeSituacao Status="atividade.Status" Atrasada="atividade.Atrasada" NaTabela="true" /></td>
```

A ficha antiga (`FichaDaAtividade.razor`, apagada na Tarefa 10) fica com o padrão: a etiqueta dela está fora de tabela.

- [ ] **Step 4: Trava e testes**

Acrescente a `Migradas`, em `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`, como primeiro item (ordem alfabética, antes de `Components/Documentos/…`): `"Components/Atividades/EtiquetaDeSituacao.razor",`.

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Expected: todos passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ComponentesDoDesignSystemTests|FullyQualifiedName~RotulosDeAtividadeTests|FullyQualifiedName~AvatarDePessoaTests|FullyQualifiedName~TextosDeAtividadeTests|FullyQualifiedName~WebTests|FullyQualifiedName~Interativ"`
Expected: todos passando. Os testes web e interativos da 9A à 9D passam sem nenhuma mudança: nenhuma tela usa os parâmetros novos ainda, e a tabela da lista de atividades emite a mesma etiqueta de antes, Atrasada com relógio.

- [ ] **Step 5: Commit**

```
git branch --show-current
git add src tests
git commit -m "feat(web): peças das telas de atividade: alerta no subtítulo, voltar do mobile com rótulo e ação, paginação com setas, etiqueta da tabela, avatar e textos"
```

---

### Task 5: Ganchos de listar e enviar na base de documentos

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Documentos/GestaoDeDocumentos.cs`
- Create: `tests/AtividadesJuridicas.IntegrationTests/GanchosDaGestaoDeDocumentosTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/DocumentosComunsInterativosTests.cs` (dois testes novos, do documento que sumiu)

**Interfaces:**
- Produces: na base, `protected virtual Task<Resultado<Pagina<DocumentoResumo>>> ListarPaginaAsync(int pagina)` e `protected virtual Task<Resultado<Guid>> EnviarArquivoAsync(string? nome, string nomeArquivo, string? tipoConteudo, Stream conteudo)`, com o comportamento de hoje (comuns ou do cliente) como padrão. A contagem por área da 9D (`PaginaDeDocumentos.PorCategoria`) é preservada: o padrão devolve a própria página do cliente, e a carga a reconhece pelo tipo, como hoje.
- O envio privado de hoje (`EnviarArquivoAsync(bool)`) passa a se chamar `EnviarEscolhidoAsync`; a confirmação de visibilidade dos comuns chega ao gancho padrão por um campo privado.
- A conferência "categoria que não é Comum exige cliente" (e "Comum não tem cliente") sai de `OnInitializedAsync` e vai para os ganchos padrão: a lista de outra origem (a da atividade, `DocumentoAtividade` sem cliente) sobrescreve os dois ganchos e não passa por ela.
- O que a Tarefa 9 precisa da base, além dos ganchos: renomear ou excluir um documento que já não existe (`NaoEncontrado`) relê a lista pelo gancho de listar, mantendo a mensagem, e o renomear fecha o painel. O aviso à ficha quando a atividade sumiu fica no herdeiro (Tarefa 9), dentro dos ganchos que ele sobrescreve.

**Regras:**
- Fora a recarga do documento que sumiu, é refatoração sem mudança de comportamento: os testes interativos e web de Documentos comuns e dos documentos da ficha do cliente passam sem mudança.
- A recarga do documento que sumiu vale para as três herdeiras (Documentos comuns e ficha do cliente também, que hoje ficam com a lista desatualizada e, no renomear, com o painel aberto) e é provada nos Documentos comuns.
- Nenhuma tela muda de marcação.

- [ ] **Step 1: A linha de base (antes de mudar)**

Com a árvore da tarefa anterior, rode a rede da base de documentos e anote o número de testes de cada comando:

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DocumentosComunsInterativosTests|FullyQualifiedName~DocumentosDaFichaInterativosTests|FullyQualifiedName~DocumentosDaFichaTests|FullyQualifiedName~DocumentosDoPortalWebTests|FullyQualifiedName~DocumentosWebTests|FullyQualifiedName~ClienteFichaInterativaTests|FullyQualifiedName~ClienteFichaWebTests|FullyQualifiedName~TextosDeDocumentoTests"`
Expected: todos passando. Anote o total; o Step 4 tem de dar o mesmo total mais os três testes novos.

- [ ] **Step 2: Testes do encaixe e do documento que sumiu (devem falhar)**

`tests/AtividadesJuridicas.IntegrationTests/GanchosDaGestaoDeDocumentosTests.cs`. Uma lista de documentos que não é de cliente nem comum, como a da atividade (Tarefa 9), lê e envia pelos ganchos. Nenhum caso de uso de comuns ou de cliente é pedido, e a base não exige cliente:

```csharp
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Web.Components.Documentos;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace AtividadesJuridicas.IntegrationTests;

public class GanchosDaGestaoDeDocumentosTests : TelaInterativa
{
    [Fact]
    public void Lista_de_outra_origem_le_e_envia_pelos_ganchos_sem_cliente()
    {
        var tela = Desenhar<ListaPelosGanchos>();

        tela.Find("li").TextContent.Should().Be("minuta-acordo-v2.docx");
        tela.Instance.PaginasLidas.Should().Equal(1);

        tela.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("conteúdo", "calculo-verbas.xlsx"));
        tela.Find("button").Click();

        tela.WaitForAssertion(() => tela.Find("p").TextContent.Should().Be("Documento enviado."));
        tela.Instance.ArquivosEnviados.Should().Equal("calculo-verbas.xlsx");
        tela.Instance.PaginasLidas.Should().Equal(1, 1);   // a lista é relida depois do envio
        Despachante.Recebidas.Should().BeEmpty();          // nada pelos casos de uso de comuns ou de cliente
    }

    /// <summary>Uma lista de documentos de outra origem: a categoria de atividade, sem cliente, lida e enviada pelos ganchos.</summary>
    public sealed class ListaPelosGanchos : GestaoDeDocumentos
    {
        private static readonly Guid Minuta = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000f1");

        public List<int> PaginasLidas { get; } = [];
        public List<string> ArquivosEnviados { get; } = [];

        protected override CategoriaDocumento CategoriaDosDocumentos => CategoriaDocumento.DocumentoAtividade;
        protected override Guid? ClienteDosDocumentos => null;
        protected override ValueTask FocarAvisoDeEnvioAsync() => ValueTask.CompletedTask;

        protected override Task<Resultado<Pagina<DocumentoResumo>>> ListarPaginaAsync(int pagina)
        {
            PaginasLidas.Add(pagina);
            return Task.FromResult(Resultado<Pagina<DocumentoResumo>>.Ok(new Pagina<DocumentoResumo>(
                [new DocumentoResumo(Minuta, "minuta-acordo-v2.docx", "minuta-acordo-v2.docx", 84 * 1024,
                    CategoriaDocumento.DocumentoAtividade, new DateTime(2026, 10, 7, 19, 40, 0, DateTimeKind.Utc),
                    "Beatriz Lins", Guid.NewGuid(), "Minuta de acordo trabalhista")],
                1, pagina, 20)));
        }

        protected override Task<Resultado<Guid>> EnviarArquivoAsync(
            string? nome, string nomeArquivo, string? tipoConteudo, Stream conteudo)
        {
            ArquivosEnviados.Add(nomeArquivo);
            return Task.FromResult(Resultado<Guid>.Ok(Guid.NewGuid()));
        }

        protected override void BuildRenderTree(RenderTreeBuilder construtor)
        {
            construtor.OpenElement(0, "ul");
            foreach (var linha in _pagina.Itens)
            {
                construtor.OpenElement(1, "li");
                construtor.AddContent(2, linha.Nome);
                construtor.CloseElement();
            }
            construtor.CloseElement();
            construtor.OpenComponent<InputFile>(3);
            construtor.AddComponentParameter(4, nameof(InputFile.OnChange),
                EventCallback.Factory.Create<InputFileChangeEventArgs>(this, Escolher));
            construtor.CloseComponent();
            construtor.OpenElement(5, "button");
            construtor.AddAttribute(6, "type", "button");
            construtor.AddAttribute(7, "onclick", EventCallback.Factory.Create(this, EnviarAsync));
            construtor.AddContent(8, "Enviar");
            construtor.CloseElement();
            if (_mensagem is not null)
            {
                construtor.OpenElement(9, "p");
                construtor.AddContent(10, _mensagem);
                construtor.CloseElement();
            }
        }
    }
}
```

E a mudança de comportamento, provada nos Documentos comuns: renomear ou excluir um documento que já não existe (outra pessoa o excluiu) mostra a mensagem do servidor **e** relê a lista; o renomear também fecha o painel. Hoje a tela só mostra a mensagem e fica com a lista desatualizada (e o painel de renomear aberto sobre um documento que não existe). A ficha da atividade (Tarefa 9) depende disso, e as três herdeiras ganham o mesmo comportamento. Em `tests/AtividadesJuridicas.IntegrationTests/DocumentosComunsInterativosTests.cs`, no fim da classe:

```csharp
    [Fact]
    public void Renomear_documento_que_sumiu_fecha_o_painel_rele_a_lista_e_mantem_a_mensagem()
    {
        Despachante.Responde<RenomearDocumento, Resultado>(Resultado.Falha(TipoErro.NaoEncontrado, "Documento não encontrado."));
        var tela = Tela();
        ClicarNaLinha(tela, "Renomear");

        tela.FindAll("section.aj-painel button").Single(b => b.TextContent == "Salvar nome").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Documento não encontrado."));
        tela.FindAll("section.aj-painel:not(.aj-painel--principal)").Should().BeEmpty();
        Despachante.Recebidas.OfType<ListarDocumentosComuns>().Should().HaveCount(2);
        tela.FindAll("table.aj-tabela tbody tr").Should().HaveCount(2);
    }

    [Fact]
    public void Excluir_documento_que_sumiu_rele_a_lista_e_mantem_a_mensagem()
    {
        Despachante.Responde<ExcluirDocumento, Resultado>(Resultado.Falha(TipoErro.NaoEncontrado, "Documento não encontrado."));
        var tela = Tela();
        ClicarNaLinha(tela, "Excluir");

        tela.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar exclusão").Click();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Documento não encontrado."));
        Despachante.Recebidas.OfType<ListarDocumentosComuns>().Should().HaveCount(2);
        tela.FindAll("[role=alertdialog]").Should().BeEmpty();
        tela.FindAll("table.aj-tabela tbody tr").Should().HaveCount(2);
    }
```

(A segunda leitura da lista é a prova da recarga: antes da mudança há uma só. A mensagem fica porque a recarga não apaga o aviso: só a troca de página e o "Tentar de novo" o apagam.)

Run: `dotnet build -m:1`
Expected: não compila (`ListarPaginaAsync` e `EnviarArquivoAsync(string?, string, string?, Stream)` não existem para sobrescrever). É o RED do encaixe; os dois testes dos Documentos comuns compilam e falham até o Step 3 (uma leitura só da lista e, no renomear, o painel aberto).

- [ ] **Step 3: Os ganchos na base**

Em `src/AtividadesJuridicas.Web/Components/Documentos/GestaoDeDocumentos.cs`. A lista e o envio de hoje passam a ser o padrão de dois métodos virtuais; a conferência do cliente passa aos ganchos padrão; o "não encontrado" de renomear e de excluir relê a lista. O resto da classe fica como está.

1. O `OnInitializedAsync` perde a conferência de cliente e categoria. Ela vai para os ganchos padrão, porque a lista de outra origem (a da atividade) não tem cliente:

```csharp
    protected override async Task OnInitializedAsync()
    {
        _confirmacao = new EnvioComConfirmacao(exigeConfirmacao: CategoriaDosDocumentos == CategoriaDocumento.DocumentoComum);
        await CarregarAsync(1);
        _carregado = true;
    }
```

2. O `CarregarAsync` lê pela lista do gancho (a contagem por área continua vindo da página do cliente, que o padrão devolve sem trocar de tipo):

```csharp
    private async Task CarregarAsync(int pagina)
    {
        _paginaPedida = pagina;
        _excluindo = null;
        var resultado = await ListarPaginaAsync(pagina);
        // Só a lista de um cliente traz a contagem por área.
        if (resultado.Valor is PaginaDeDocumentos lidos) _porCategoria = lidos.PorCategoria;

        if (await EncerradoAsync(resultado)) return;

        _pagina = resultado.Valor is { } p
            ? new Pagina<Linha>([.. p.Itens.Select(d => new Linha(d.Id, d.Nome, d.TamanhoBytes, d.EnviadoEm, d.EnviadoPor, d.AtividadeTitulo))],
                p.Total, p.Numero, p.Tamanho)
            : Vazia;
        // Falha na consulta não é "nenhum documento": mostra o erro no lugar da lista.
        _erroDaLista = resultado.Sucesso ? null : resultado.Mensagem ?? MensagemDaLista;

        // A página ficou além do fim (por exemplo, ao excluir o único item dela): volta à última.
        if (resultado.Sucesso && _pagina.Itens.Count == 0 && _pagina.Total > 0 && _pagina.Numero > _pagina.TotalDePaginas)
            await CarregarAsync(_pagina.TotalDePaginas);

        if (resultado.Sucesso) await AoCarregarAsync();
    }
```

3. Os dois ganchos e os auxiliares, logo depois de `CarregarAsync`:

```csharp
    private const string MensagemDaLista = "Não foi possível consultar os documentos.";

    /// <summary>
    /// Lê uma página da lista. O padrão lê os Documentos comuns ou os do cliente na categoria da tela; a página do
    /// cliente chega como <see cref="PaginaDeDocumentos"/>, com a contagem por área. A lista de outra origem (os
    /// documentos de uma atividade) sobrescreve.
    /// </summary>
    protected virtual async Task<Resultado<Pagina<DocumentoResumo>>> ListarPaginaAsync(int pagina)
    {
        ConferirOrigemPadrao();
        if (CategoriaDosDocumentos == CategoriaDocumento.DocumentoComum)
        {
            var comuns = await Despachante.EnviarAsync(new ListarDocumentosComuns(pagina));
            if (!comuns.Sucesso) return MesmaFalha(comuns);

            // Documento comum não tem atividade: o título fica nulo. O autor vem preenchido para a Diretoria. A linha
            // não usa o nome original do arquivo; o resumo leva o nome do documento no lugar dele.
            var p = comuns.Valor!;
            return Resultado<Pagina<DocumentoResumo>>.Ok(new Pagina<DocumentoResumo>(
                [.. p.Itens.Select(d => new DocumentoResumo(
                    d.Id, d.Nome, d.Nome, d.TamanhoBytes, CategoriaDocumento.DocumentoComum, d.EnviadoEm, d.EnviadoPor, null, null))],
                p.Total, p.Numero, p.Tamanho));
        }

        var doCliente = await Despachante.EnviarAsync(
            new ListarDocumentosDoCliente(ClienteDosDocumentos!.Value, CategoriaDosDocumentos, pagina));
        // A página segue como PaginaDeDocumentos: a contagem por área vai junto.
        return doCliente.Sucesso ? Resultado<Pagina<DocumentoResumo>>.Ok(doCliente.Valor!) : MesmaFalha(doCliente);
    }

    /// <summary>
    /// Envia o arquivo escolhido. O padrão envia o Documento comum (com a confirmação de visibilidade que a tela
    /// colheu) ou o documento do cliente na categoria da tela. A lista de outra origem sobrescreve.
    /// </summary>
    protected virtual Task<Resultado<Guid>> EnviarArquivoAsync(string? nome, string nomeArquivo, string? tipoConteudo, Stream conteudo)
    {
        ConferirOrigemPadrao();
        return CategoriaDosDocumentos == CategoriaDocumento.DocumentoComum
            ? Despachante.EnviarAsync(new EnviarDocumentoComum(
                nome, nomeArquivo, tipoConteudo, conteudo, ConfirmouVisibilidade: _envioConfirmado))
            : Despachante.EnviarAsync(new EnviarDocumento(
                CategoriaDosDocumentos, ClienteDosDocumentos!.Value, nome, nomeArquivo, tipoConteudo, conteudo));
    }

    // A lista padrão é a dos Documentos comuns ou a de um cliente: qualquer outra combinação é erro de quem usa o
    // componente. A lista de outra origem sobrescreve os dois ganchos e não passa por aqui.
    private void ConferirOrigemPadrao()
    {
        if (CategoriaDosDocumentos != CategoriaDocumento.DocumentoComum && ClienteDosDocumentos is null)
            throw new InvalidOperationException("Informe o ClienteId: só os Documentos comuns dispensam o cliente.");
        if (CategoriaDosDocumentos == CategoriaDocumento.DocumentoComum && ClienteDosDocumentos is not null)
            throw new InvalidOperationException("Documentos comuns não pertencem a um cliente: não informe o ClienteId.");
    }

    // A falha da consulta no tipo do gancho, com o erro e a mensagem de quem respondeu.
    private static Resultado<Pagina<DocumentoResumo>> MesmaFalha(Resultado resultado) =>
        Resultado<Pagina<DocumentoResumo>>.Falha(resultado.Erro ?? TipoErro.Validacao, resultado.Mensagem ?? MensagemDaLista);
```

4. O envio privado de hoje (`private async Task EnviarArquivoAsync(bool confirmou)`) passa a se chamar `EnviarEscolhidoAsync`, para não se confundir com o gancho. Ele guarda a confirmação para o padrão e envia pelo gancho. Os dois chamadores (`EnviarAsync`, com `confirmou: false`, e `ConfirmarEnvioAsync`, com `confirmou: true`) passam a chamar `EnviarEscolhidoAsync`. Campo novo, junto de `_focarPainel`:

```csharp
    // O envio em curso foi confirmado no aviso de visibilidade (só os Documentos comuns pedem): o gancho padrão o repassa.
    private bool _envioConfirmado;
```

O começo do método (o resto, de `if (await EncerradoAsync(resultado)) return;` até o `finally`, fica igual):

```csharp
    private async Task EnviarEscolhidoAsync(bool confirmou)
    {
        if (_arquivo is not { } arquivo || _enviando) return;

        _mensagem = null;
        _enviando = true;
        try
        {
            await using var conteudo = arquivo.OpenReadStream(DocumentosOpcoes.TamanhoMaximoBytes);
            _envioConfirmado = confirmou;
            var resultado = await EnviarArquivoAsync(_nomeDoEnvio, arquivo.Name, arquivo.ContentType, conteudo);
            if (await EncerradoAsync(resultado)) return;
```

5. Renomear e excluir um documento que já não existe (`NaoEncontrado`) releem a lista, mantendo a mensagem. Em `RenomearAsync`, o bloco de falha passa a ser:

```csharp
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
```

e em `ExcluirAsync`:

```csharp
            if (!resultado.Sucesso)
            {
                Avisar(resultado.Mensagem, falhou: true);
                // O documento pode já não existir: a lista é atualizada para refletir o servidor.
                if (resultado.Erro == TipoErro.NaoEncontrado) await CarregarAsync(_pagina.Numero);
                return;
            }
```

(`CarregarAsync` não apaga `_mensagem`: o aviso fica. A recarga passa pelo gancho de listar, então a lista de outra origem também relê pela própria consulta.)

Os herdeiros não mudam de código: `DocumentosComunsDaDiretoria` e `DocumentosDaFicha` não sobrescrevem os ganchos e continuam com a lista e o envio de hoje; o único comportamento novo deles é a recarga do item 5.

O padrão que converte `DocumentoDoPortal` em `DocumentoResumo` preenche `NomeArquivoOriginal` com o nome do documento e `Categoria` com `DocumentoComum`, e isso não inventa dado que alguém leia: a base só lê do resumo os campos da `Linha` (`Id`, `Nome`, `TamanhoBytes`, `EnviadoEm`, `EnviadoPor`, `AtividadeTitulo`), e nenhuma tela de Documentos comuns lê o resumo. A lista da atividade (Tarefa 9) lê `NomeArquivoOriginal` do resultado da própria consulta, no gancho dela, que é o dado verdadeiro.

- [ ] **Step 4: Os mesmos testes, depois**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Expected: todos passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DocumentosComunsInterativosTests|FullyQualifiedName~DocumentosDaFichaInterativosTests|FullyQualifiedName~DocumentosDaFichaTests|FullyQualifiedName~DocumentosDoPortalWebTests|FullyQualifiedName~DocumentosWebTests|FullyQualifiedName~ClienteFichaInterativaTests|FullyQualifiedName~ClienteFichaWebTests|FullyQualifiedName~TextosDeDocumentoTests|FullyQualifiedName~GanchosDaGestaoDeDocumentosTests"`
Expected: todos passando, com o total do Step 1 mais 3 (o teste do encaixe e os dois de documento que sumiu), sem nenhum teste antigo alterado.

- [ ] **Step 5: Commit**

```
git branch --show-current
git add src tests
git commit -m "feat(web): ganchos de listar e enviar na base de documentos, com a lista e o envio de hoje como padrão, e a lista relida quando o documento já não existe"
```

---

### Task 6: Lista de atividades

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/Atividades.razor` (reescrita: só a lista)
- Modify: `tests/AtividadesJuridicas.IntegrationTests/AtividadesWebTests.cs` (a parte da lista)
- Create: `tests/AtividadesJuridicas.IntegrationTests/AtividadesInterativosTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs` (`/atividades` sai dos testes de tela não migrada; o de barra de logo passa a `/agenda`)
- Modify: `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs` (`Migradas`)

**Interfaces:**
- Consumes: `PaginaDeAtividades` (Tarefa 2), `CabecalhoDePagina.Alerta`, `Paginacao.ComSetas`, `EtiquetaDeSituacao.NaTabela`, `AvatarDePessoa`, `RotulosDeAtividade.SituacaoNoPlural`, `TextosDeAtividade.Contagem`/`Atrasadas` (Tarefa 4), `EstadoDaLista`, `Recados`, `Aviso`, `Icone`.
- Produces: a rota `/atividades` com a lista; os links `/atividades/nova` e `/atividades/{id}`, que as Tarefas 7 e 10 atendem.

**Regras:**
- Quadros: `Atividades.dc.html`, `AtividadesTablet.dc.html`, `AtividadesMobile.dc.html` (`quadros-lista-e-formularios.md`, seção 1).
- Sai da lista o que era painel (formulário, ficha) e a lógica deles (`_criando`, `_abertaId`, `CriadaAsync`, `FecharFicha`, `AtualizarListaAsync`). Ficam: `ListarAtividades` com os filtros, a volta à última página, "acesso negado" encerrando a tela, `ListarUsuarios` só para a Diretoria (para os campos de responsável e revisor; responsáveis: os não clientes; revisores: os de Diretoria; inativos com ` (inativo)`).
- Filtros: as regras da seção "Decisões". Desktop: `form.aj-barra-filtros` com a busca (`aj-campo-icone aj-campo-icone--filtro`), `Situação`, `Responsável` e `Revisor` (`aj-campo-filtro`, os dois últimos só a Diretoria), `Só atrasadas` (`aj-opcao aj-opcao--solta`) e `Buscar`. Tablet: `form.aj-linha-filtros.aj-linha-filtros--busca` com a busca e o botão do painel. Mobile: na segunda linha da barra superior, `aj-barra-superior__busca` com a busca (que envia o formulário escondido `busca-de-atividades`) e o botão de ícone `Mais filtros`, e na terceira os chips (`aj-chips aj-chips--barra`). Chips do tablet (`aj-chips`) num invólucro `aj-so-tablet`. O painel de filtros (`aj-painel aj-painel--filtros`, padrão da `Auditoria.razor`: `aria-expanded`, `aria-controls`, "Limpar" e "Filtrar") abre logo abaixo da linha do tablet e da barra do mobile.
- Subtítulo (também na barra do mobile): `TextosDeAtividade.Contagem(NoEscopo)` e o `Alerta` `TextosDeAtividade.Atrasadas(Atrasadas)`, só com a lista carregada, sem erro e com `NoEscopo` maior que zero.
- Tabela (desktop, `aj-tabela aj-tabela--larga`): `Título` principal; `Situação` com `div.aj-etiquetas` e `EtiquetaDeSituacao NaTabela`; `Entrega` em `aj-tabela__data` (com `--atrasada` quando atrasada) ou `—` em `aj-tabela__vazia`; `Complexidade`; `Cliente` (ou `—` em `aj-tabela__vazia`); `Processo` em `aj-tabela__secundaria` (ou `—` em `aj-tabela__vazia`); `Responsáveis` (nomes unidos por ", ", ou `—` em `aj-tabela__vazia`); `Revisor` (ou `—` em `aj-tabela__vazia`); "Abrir" (`a.aj-botao.aj-botao--fantasma.aj-botao--seta`) para `/atividades/{id}`. Paginação com setas, sempre à vista no desktop e no tablet; no mobile, só com mais de uma página.
- Cartões (tablet e mobile, `aj-grade`): `a.aj-cartao.aj-cartao--atividade` para a ficha; `aj-cartao__topo` com as etiquetas (forma de cartão) e, no mobile, a seta; título; meta `{cliente ou Sem cliente}<br />{processo ou Sem processo vinculado}`; rodapé com a data (`aj-cartao__data`, ícone `agenda`, `--atrasada` quando atrasada; sem data, `—`) e os avatares (`aj-avatares` com `span.aj-avatar.aj-avatar--p.{classe}` e `title="{nome}"`) ou `Sem responsável` (`aj-cartao__vazio`).
- "Nova atividade" só para a Diretoria (`CabecalhoDePagina.Acao`, `AcaoHref="/atividades/nova"`). Recado de outra tela, uma vez, no topo. Falha da consulta: o erro com "Tentar de novo", sem subtítulo e sem os números dos chips; vazio: `Nenhuma atividade.`. Guarda de clique duplo na paginação e nos chips (`Ocupado`).
- Testes HTTP: Diretoria e Produção (o que cada uma vê, inclusive os contadores e a ausência de "Nova atividade" e dos filtros de responsável e revisor para a Produção, com afirmação positiva no mesmo bloco); recado de outra tela; os textos e as classes de cada visão (`BlocoDeVisao`). Testes bUnit: chip escolhe e busca na hora mantendo a busca e o responsável (Review Focus 2); "Todas" e "Atrasadas"; o painel de filtros abre, "Filtrar" aplica e "Limpar" limpa; falha da consulta sem subtítulo e com "Tentar de novo".

- [ ] **Step 1: Testes (devem falhar)**

Antes de escrever, releia os três quadros (`Atividades.dc.html`, `AtividadesTablet.dc.html`, `AtividadesMobile.dc.html`) e a seção 1 de `quadros-lista-e-formularios.md`.

**`tests/AtividadesJuridicas.IntegrationTests/AtividadesWebTests.cs`.** O `InitializeAsync` fica como está (a contestação com entrega em 15/01/2020, complexidade 2, direcionada ao Pedro com a Diana de revisora, logo atrasada; o "Recurso interno" sem data, complexidade 3, sem responsável). Os testes `Menu_mostra_atividades_para_todos_os_perfis`, `Anonimo_e_redirecionado_ao_login` e `Listagem_fica_no_registro_de_consultas` não mudam. Os três testes do formulário (`Formulario_de_atividade_nova_…`, `Formulario_de_edicao_…`, `Formulario_de_atividade_lembra_…`) e os auxiliares `SeletorDeComplexidade` e `Opcoes` também ficam como estão nesta tarefa: o `FormularioDeAtividade` continua existindo, e a Tarefa 7 os reescreve para a página nova.

Acrescente os `using` `System.Text.RegularExpressions` (já existe), `AtividadesJuridicas.Web.Components.Pages` e `AtividadesJuridicas.Web.Infra`. Acrescente o auxiliar da barra do mobile:

```csharp
    // A barra superior que a lista declara (a do layout é "aj-barra-superior aj-barra-superior--padrao").
    private static string BarraDoMobile(string html)
    {
        var barra = Regex.Match(html, "<header class=\"aj-barra-superior\">.*?</header>", RegexOptions.Singleline);
        barra.Success.Should().BeTrue("a lista declara a própria barra superior do mobile");
        return barra.Value;
    }
```

Como cada asserção antiga é reescrita (a prova continua a mesma, com a marcação nova):

| Asserção antiga (linha) | O que prova | Forma nova |
|---|---|---|
| 48-50 `Contain("Atividades")…Contain("Pedro Produção")` | as duas atividades, as situações, a data e o responsável chegam à tela | fica igual (os textos continuam na página) |
| 51 `Contain("Nova atividade").And.Contain("Abrir")` | a Diretoria tem a ação de cadastro e o "Abrir" | o link primário para `/atividades/nova` no cabeçalho, o botão flutuante com `aria-label="Nova atividade"` e o link "Abrir" para `/atividades/{id}` |
| 53-54 `<td>\s*Elaborar contestação\s*</td>` | o título está numa célula da tabela, não só num `<option>` | `<td class="aj-tabela__principal">Elaborar contestação</td>` na linha inteira da contestação, no bloco `aj-so-desktop` |
| 56-58 a etiqueta Direcionada e a Atrasada (com relógio) na mesma célula | a situação é etiqueta e a entrega vencida ganha a Atrasada ao lado | as duas etiquetas dentro de `<div class="aj-etiquetas">` na célula, a Atrasada ainda com o relógio (`NaTabela`) |
| 59 a etiqueta Criada sozinha na célula | atividade sem atraso não ganha a Atrasada | a linha do "Recurso interno" com só a etiqueta Criada dentro de `aj-etiquetas` |
| 60 `<td>\s*15/01/2020\s*</td>` | a data de entrega numa célula | `<td class="aj-tabela__data aj-tabela__data--atrasada">15/01/2020</td>` (agora também prova o destaque do atraso) |
| 61 `<td>\s*Pedro Produção\s*</td>` | o responsável numa célula | `<td>Pedro Produção</td>` na posição da coluna Responsáveis, na mesma expressão da linha |
| 71 `Contain("Elaborar contestação").And.Contain("Abrir")` (Produção) | a Produção vê a sua e pode abri-la | o título e o link "Abrir" para `/atividades/{contestação}` |
| 72-73 `NotContain("Recurso interno")…NotContain("Nova atividade")…` | a Produção não vê a dos outros nem a ação de cadastro, e a tela não encerrou | ficam, mais `NotContain("/atividades/nova")`; a positiva do mesmo HTML é o título e o "Abrir" acima |
| 131 `<th>\s*Complexidade\s*</th>` | a coluna existe | `<th scope="col">Entrega</th><th scope="col">Complexidade</th>` (prova também a posição, spec 8) |
| 132 `<td>\s*2\s*</td>` e `<td>\s*3\s*</td>` | o valor de cada atividade | `<td>2</td>` logo depois da entrega atrasada da contestação e `<td>3</td>` logo depois da entrega vazia do "Recurso interno" |

Os três testes reescritos e os novos:

```csharp
    [Fact]
    public async Task Diretor_ve_todas_as_atividades_e_as_acoes_de_cadastro()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/atividades"));

        html.Should().Contain("Atividades").And.Contain("Elaborar contestação").And.Contain("Recurso interno")
            .And.Contain("Direcionada").And.Contain("Criada").And.Contain("Atrasada").And.Contain("15/01/2020")
            .And.Contain("Pedro Produção");
        // O cabeçalho conta o escopo da Diretoria (o escritório inteiro), com as atrasadas à parte, e traz a ação.
        html.Should().MatchRegex("<p class=\"aj-subtitulo\">2 atividades\\s*·\\s*<strong class=\"aj-subtitulo__alerta\">1 atrasada</strong></p>")
            .And.MatchRegex("<a href=\"/atividades/nova\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\">\\s*<svg[^>]*>.*?</svg>\\s*Nova atividade</a>")
            .And.Contain("<a href=\"/atividades/nova\" class=\"aj-fab\" aria-label=\"Nova atividade\">")
            .And.Contain("<form id=\"busca-de-atividades\" hidden");

        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex("<form class=\"aj-barra-filtros\" role=\"search\"")
            .And.MatchRegex("<label class=\"aj-campo-icone aj-campo-icone--filtro\">\\s*<svg[^>]*>.*?</svg>\\s*<span class=\"aj-so-leitor\">Busca</span>\\s*<input type=\"text\" class=\"aj-campo-icone__controle\" placeholder=\"Buscar pelo título da atividade\"")
            .And.MatchRegex("(?s)<label class=\"aj-campo-filtro\">\\s*<span>Situação</span>\\s*<select class=\"aj-campo-filtro__controle\"[^>]*>\\s*<option value=\"\"[^>]*>Todas</option>.*?>Criada</option>.*?>Reaberta</option>\\s*</select>")
            .And.MatchRegex("<label class=\"aj-opcao aj-opcao--solta\">\\s*<input type=\"checkbox\" class=\"aj-opcao__caixa\"[^>]*>\\s*Só atrasadas</label>")
            .And.Contain("<button type=\"submit\" class=\"aj-botao aj-botao--secundario\">Buscar</button>")
            .And.Contain("<table class=\"aj-tabela aj-tabela--larga\">")
            .And.Contain("<thead><tr><th scope=\"col\">Título</th><th scope=\"col\">Situação</th><th scope=\"col\">Entrega</th><th scope=\"col\">Complexidade</th><th scope=\"col\">Cliente</th><th scope=\"col\">Processo</th><th scope=\"col\">Responsáveis</th><th scope=\"col\">Revisor</th><th scope=\"col\"><span class=\"aj-so-leitor\">Ações</span></th></tr></thead>")
            .And.MatchRegex(
                "(?s)<td class=\"aj-tabela__principal\">Elaborar contestação</td>\\s*" +
                "<td>\\s*<div class=\"aj-etiquetas\">\\s*<span class=\"aj-etiqueta aj-etiqueta--direcionada\">Direcionada</span>\\s*" +
                "<span class=\"aj-etiqueta aj-etiqueta--atrasada\"><svg[^>]*>.*?</svg>Atrasada</span>\\s*</div>\\s*</td>\\s*" +
                "<td class=\"aj-tabela__data aj-tabela__data--atrasada\">15/01/2020</td>\\s*<td>2</td>\\s*" +
                "<td class=\"aj-tabela__vazia\">—</td>\\s*<td class=\"aj-tabela__vazia\">—</td>\\s*" +
                "<td>Pedro Produção</td>\\s*<td>Diana Diretora</td>\\s*" +
                $"<td class=\"aj-tabela__acao\"><a href=\"/atividades/{_contestacao}\" class=\"aj-botao aj-botao--fantasma aj-botao--seta\">\\s*Abrir\\s*<svg")
            .And.MatchRegex(
                "(?s)<td class=\"aj-tabela__principal\">Recurso interno</td>\\s*" +
                "<td>\\s*<div class=\"aj-etiquetas\">\\s*<span class=\"aj-etiqueta aj-etiqueta--criada\">Criada</span>\\s*</div>\\s*</td>\\s*" +
                "<td class=\"aj-tabela__vazia\">—</td>\\s*<td>3</td>\\s*" +
                "<td class=\"aj-tabela__vazia\">—</td>\\s*<td class=\"aj-tabela__vazia\">—</td>\\s*<td class=\"aj-tabela__vazia\">—</td>")
            .And.Contain("aj-paginacao--com-setas").And.Contain("Página 1 de 1 · 2 atividades");
        // Responsável: os não clientes; revisor: só a Diretoria.
        Regex.Match(desktop, "(?s)<span>Responsável</span>\\s*<select[^>]*>(.*?)</select>").Groups[1].Value
            .Should().Contain(">Todos</option>").And.Contain(">Diana Diretora</option>").And.Contain(">Pedro Produção</option>");
        Regex.Match(desktop, "(?s)<span>Revisor</span>\\s*<select[^>]*>(.*?)</select>").Groups[1].Value
            .Should().Contain(">Diana Diretora</option>").And.NotContain("Pedro Produção");

        var tablet = BlocoDeVisao.De(html, "aj-so-tablet");
        tablet.Should().MatchRegex("<form class=\"aj-linha-filtros aj-linha-filtros--busca\" role=\"search\"")
            .And.MatchRegex("<span class=\"aj-so-leitor\">Buscar atividade</span>\\s*<input type=\"search\" class=\"aj-campo-icone__controle\" placeholder=\"Buscar pelo título da atividade\"")
            .And.MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--secundario aj-botao--com-icone\" aria-expanded=\"false\" aria-controls=\"filtros-de-atividades\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Responsável, revisor</button>")
            .And.MatchRegex(
                "(?s)<div class=\"aj-chips\" role=\"group\" aria-label=\"Filtrar por situação\">\\s*" +
                "<button type=\"button\" class=\"aj-chip\" aria-pressed=\"true\"[^>]*>Todas · 2</button>\\s*" +
                "<button type=\"button\" class=\"aj-chip aj-chip--alerta\" aria-pressed=\"false\"[^>]*>Atrasadas · 1</button>\\s*" +
                "<button type=\"button\" class=\"aj-chip\" aria-pressed=\"false\"[^>]*>Direcionadas</button>\\s*" +
                "<button[^>]*>Feitas</button>\\s*<button[^>]*>Para correção</button>\\s*<button[^>]*>Corrigidas</button>\\s*<button[^>]*>Concluídas</button>\\s*</div>")
            .And.Contain("Página 1 de 1 · 2 atividades");

        var cartoes = BlocoDeVisao.De(html, "aj-tablet-mobile");
        cartoes.Should().MatchRegex(
            $"(?s)<a href=\"/atividades/{_contestacao}\" class=\"aj-cartao aj-cartao--atividade\">\\s*" +
            "<span class=\"aj-cartao__topo\">\\s*<span class=\"aj-cartao__etiquetas\">\\s*" +
            "<span class=\"aj-etiqueta aj-etiqueta--direcionada\">Direcionada</span>\\s*" +
            "<span class=\"aj-etiqueta aj-etiqueta--atrasada aj-etiqueta--com-ponto\">Atrasada</span>\\s*</span>\\s*" +
            "<span class=\"aj-so-mobile\"><svg class=\"aj-cartao__seta\"[^>]*>.*?</svg></span>\\s*</span>\\s*" +
            "<span class=\"aj-cartao__titulo\">Elaborar contestação</span>\\s*" +
            "<span class=\"aj-cartao__meta\">Sem cliente<br\\s*/?>Sem processo vinculado</span>\\s*" +
            "<span class=\"aj-cartao__rodape\">\\s*<span class=\"aj-cartao__data aj-cartao__data--atrasada\"><svg[^>]*>.*?</svg>15/01/2020</span>\\s*" +
            $"<span class=\"aj-avatares\">\\s*<span class=\"aj-avatar aj-avatar--p {AvatarDePessoa.Classe("Pedro Produção")}\" title=\"Pedro Produção\">PP</span>\\s*</span>")
            .And.MatchRegex(
                "(?s)<span class=\"aj-cartao__titulo\">Recurso interno</span>\\s*" +
                "<span class=\"aj-cartao__meta\">Sem cliente<br\\s*/?>Sem processo vinculado</span>\\s*" +
                "<span class=\"aj-cartao__rodape\">\\s*<span class=\"aj-cartao__data\"><svg[^>]*>.*?</svg>—</span>\\s*" +
                "<span class=\"aj-cartao__vazio\">Sem responsável</span>");

        // No mobile a paginação só aparece com mais de uma página; o bloco do mobile tem as setas dos cartões.
        BlocoDeVisao.De(html, "aj-so-mobile").Should().Contain("aj-cartao__seta").And.NotContain("Página 1");

        var barra = BarraDoMobile(html);
        barra.Should().MatchRegex("<span class=\"aj-subtitulo\">2 atividades\\s*·\\s*<strong class=\"aj-subtitulo__alerta\">1 atrasada</strong></span>")
            .And.MatchRegex("<div class=\"aj-barra-superior__busca\">\\s*<label class=\"aj-campo-icone\">")
            .And.Contain("<input type=\"search\" class=\"aj-campo-icone__controle\" form=\"busca-de-atividades\" placeholder=\"Buscar pelo título\"")
            .And.Contain("<button type=\"button\" class=\"aj-botao aj-botao--secundario aj-botao--icone\" aria-label=\"Mais filtros\" aria-expanded=\"false\" aria-controls=\"filtros-de-atividades\"")
            .And.MatchRegex("(?s)<div class=\"aj-chips aj-chips--barra\" role=\"group\" aria-label=\"Filtrar por situação\">.*?>Todas · 2</button>.*?>Atrasadas · 1</button>.*?>Concluídas</button>\\s*</div>");
        // O mobile não tem o chip "Corrigidas" (o quadro).
        barra.Should().NotContain("Corrigidas");
    }

    [Fact]
    public async Task Producao_ve_so_as_suas_e_nenhuma_acao_de_cadastro()
    {
        var cliente = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/atividades"));

        html.Should().Contain("Elaborar contestação")
            .And.MatchRegex($"<a href=\"/atividades/{_contestacao}\" class=\"aj-botao aj-botao--fantasma aj-botao--seta\">\\s*Abrir");
        html.Should().NotContain("Recurso interno").And.NotContain("Nova atividade").And.NotContain("/atividades/nova")
            .And.NotContain("Seu acesso foi encerrado");
        // Review Focus 1: os números contam só as atividades em que a Produção é responsável.
        html.Should().MatchRegex("<p class=\"aj-subtitulo\">1 atividade\\s*·\\s*<strong class=\"aj-subtitulo__alerta\">1 atrasada</strong></p>");

        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().Contain("<span>Situação</span>").And.Contain("Só atrasadas");
        desktop.Should().NotContain("<span>Responsável</span>").And.NotContain("<span>Revisor</span>");

        var tablet = BlocoDeVisao.De(html, "aj-so-tablet");
        tablet.Should().MatchRegex("aria-controls=\"filtros-de-atividades\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Mais filtros</button>")
            .And.MatchRegex(">Todas · 1</button>").And.MatchRegex(">Atrasadas · 1</button>");
        tablet.Should().NotContain("Responsável, revisor").And.NotContain("Todas · 2");
    }

    [Fact]
    public async Task Lista_mostra_a_complexidade_de_cada_atividade()
    {
        var cliente = await _app.ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/atividades"));

        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        // A coluna vem logo depois de Entrega (spec 8).
        desktop.Should().Contain("<th scope=\"col\">Entrega</th><th scope=\"col\">Complexidade</th>");
        desktop.Should().MatchRegex("<td class=\"aj-tabela__data aj-tabela__data--atrasada\">15/01/2020</td>\\s*<td>2</td>")
            .And.MatchRegex("<td class=\"aj-tabela__vazia\">—</td>\\s*<td>3</td>");
    }

    [Fact]
    public async Task Recado_de_outra_tela_aparece_uma_vez_no_topo()
    {
        var recados = new Recados();
        recados.Deixar("Atividade excluída.");

        var html = WebUtility.HtmlDecode(await TelaDeTeste.RenderizarAsync<Atividades>(_app, _admin, new(), recados));

        html.Should().MatchRegex("<p class=\"aj-aviso aj-aviso--sucesso\"[^>]*>.*Atividade excluída.")
            .And.Contain("Elaborar contestação");
        recados.Retirar().Should().BeNull();
    }
```

(As expressões da etiqueta seguem a marcação que a Tarefa 4 definiu para `EtiquetaDeSituacao`: na tabela (`NaTabela`), a Atrasada é `aj-etiqueta aj-etiqueta--atrasada` com o relógio; no cartão, `aj-etiqueta aj-etiqueta--atrasada aj-etiqueta--com-ponto`, sem ícone. A classe da cor do avatar vem de `AvatarDePessoa.Classe`, a mesma conta da tela.)

**`tests/AtividadesJuridicas.IntegrationTests/LayoutWebTests.cs`:**

- `Tela_ainda_nao_migrada_fica_com_a_barra_de_logo_e_sem_involucro`: troque `PaginaAsync(admin, "/atividades")` por `PaginaAsync(admin, "/agenda")`. Nada mais muda no teste: a Agenda é a tela que fica sem migrar até a 9F, e as asserções (uma só barra, a do logo, o `h1` no `main`, a faixa "Exibir menu" fora dele) continuam provando o mesmo para ela.
- `Tela_ainda_nao_migrada_nao_abre_o_conteudo_com_bloco_do_design_system`: tire a linha `[InlineData("/atividades")]` (fica só `/agenda`).
- `Diretoria_ve_o_destino_ou_mais_aceso_em_cada_barra` (`/atividades`) e `Avatar_da_barra_superior_traz_as_iniciais` (`/atividades`) ficam como estão: a rota continua, e a barra que a lista declara (`<header class="aj-barra-superior">`, a primeira do HTML) traz o mesmo `<a href="/minha-senha" class="aj-avatar" aria-label="Minha conta">` com as iniciais do layout.

**`tests/AtividadesJuridicas.IntegrationTests/AtividadesInterativosTests.cs`** (novo, bUnit; sem o encaixe da barra superior, os chips achados são os do tablet e o botão do painel é o do tablet):

```csharp
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Components.Pages;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

public class AtividadesInterativosTests : TelaInterativa
{
    private static readonly AtividadeResumo Contestacao = new(
        Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000f1"), "Contestação — cobrança de ISS", StatusAtividade.Direcionada,
        new DateOnly(2026, 10, 6), true, "Construtora Serra Azul Ltda.", "Serra Azul × Município — ISS", "Tributário",
        ["Pedro Produção"], "Diana Diretora", 2);

    private static readonly UsuarioResumo Diana = new(
        AtorDaDiretoria.Id, "Diana Diretora", "diana@x.com", AtorDaDiretoria.PerfilId, "Diretoria", TipoPerfil.Diretoria, null, true, null);

    private static readonly UsuarioResumo Pedro = new(
        AtorDaProducao.Id, "Pedro Produção", "pedro@x.com", AtorDaProducao.PerfilId, "Produção", TipoPerfil.Producao, null, true, null);

    // Uma atividade na página; o escopo do usuário tem 8, uma atrasada.
    private static PaginaDeAtividades Pagina() => new([Contestacao], 1, 1, 20, 8, 1);

    private IRenderedComponent<Atividades> Lista(UsuarioAutenticado ator)
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(ator)
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Diana, Pedro]))
            .Responde<ListarAtividades, Resultado<PaginaDeAtividades>>(Resultado<PaginaDeAtividades>.Ok(Pagina()));
        return Desenhar<Atividades>();
    }

    private ListarAtividades UltimaConsulta => Despachante.Recebidas.OfType<ListarAtividades>().Last();

    private static IElement Chip(IRenderedComponent<Atividades> tela, string rotulo) =>
        tela.FindAll(".aj-chips button").Single(b => b.TextContent == rotulo);

    // Um campo pelo nome ("Situação", "Responsável", "Revisor"), na barra do desktop ou no painel de filtros.
    private static IElement Campo(IRenderedComponent<Atividades> tela, string formulario, string nome) =>
        tela.Find(formulario).QuerySelectorAll("label.aj-campo-filtro")
            .Single(l => l.QuerySelector("span")!.TextContent == nome).QuerySelector("select")!;

    private static IElement CampoDaBarra(IRenderedComponent<Atividades> tela, string nome) => Campo(tela, "form.aj-barra-filtros", nome);

    private static IElement CampoDoPainel(IRenderedComponent<Atividades> tela, string nome) => Campo(tela, "form#filtros-de-atividades", nome);

    private static string Texto(IElement elemento) => Regex.Replace(elemento.TextContent, "\\s+", " ").Trim();

    [Fact]
    public void Chip_de_situacao_busca_na_hora_mantendo_a_busca_e_o_responsavel()
    {
        var tela = Lista(AtorDaDiretoria);
        tela.Find("form.aj-linha-filtros input").Change("contestação");
        CampoDaBarra(tela, "Responsável").Change(Pedro.Id.ToString());

        Chip(tela, "Feitas").Click();

        var consulta = UltimaConsulta;
        consulta.Status.Should().Be(StatusAtividade.Feita);
        consulta.SomenteAtrasadas.Should().BeFalse();
        consulta.Busca.Should().Be("contestação");
        consulta.ResponsavelId.Should().Be(Pedro.Id);
        consulta.Pagina.Should().Be(1);
        Chip(tela, "Feitas").GetAttribute("aria-pressed").Should().Be("true");
        Chip(tela, "Todas · 8").GetAttribute("aria-pressed").Should().Be("false");
        // O campo do desktop mostra o mesmo estado que o chip.
        CampoDaBarra(tela, "Situação").QuerySelector("option[selected]")!.TextContent.Should().Be("Feita");
        CampoDaBarra(tela, "Responsável").QuerySelector("option[selected]")!.TextContent.Should().Be("Pedro Produção");
    }

    [Fact]
    public void Atrasadas_liga_so_atrasadas_e_limpa_a_situacao_e_Todas_limpa_os_dois()
    {
        var tela = Lista(AtorDaDiretoria);
        Chip(tela, "Feitas").Click();

        Chip(tela, "Atrasadas · 1").Click();

        UltimaConsulta.Status.Should().BeNull();
        UltimaConsulta.SomenteAtrasadas.Should().BeTrue();
        Chip(tela, "Atrasadas · 1").GetAttribute("aria-pressed").Should().Be("true");
        Chip(tela, "Feitas").GetAttribute("aria-pressed").Should().Be("false");
        tela.Find("label.aj-opcao--solta input").HasAttribute("checked").Should().BeTrue();

        Chip(tela, "Concluídas").Click();

        UltimaConsulta.Status.Should().Be(StatusAtividade.Concluida);
        UltimaConsulta.SomenteAtrasadas.Should().BeFalse();

        Chip(tela, "Todas · 8").Click();

        UltimaConsulta.Status.Should().BeNull();
        UltimaConsulta.SomenteAtrasadas.Should().BeFalse();
        Chip(tela, "Todas · 8").GetAttribute("aria-pressed").Should().Be("true");
        tela.Find("label.aj-opcao--solta input").HasAttribute("checked").Should().BeFalse();
    }

    [Fact]
    public void Painel_de_filtros_abre_filtrar_aplica_e_fecha_e_limpar_limpa_sem_tirar_a_busca()
    {
        var tela = Lista(AtorDaDiretoria);
        tela.Find("form.aj-linha-filtros input").Change("minuta");
        Texto(tela.Find("button[aria-controls=filtros-de-atividades]")).Should().Be("Responsável, revisor");

        tela.Find("button[aria-controls=filtros-de-atividades]").Click();

        tela.Find("button[aria-controls=filtros-de-atividades]").GetAttribute("aria-expanded").Should().Be("true");
        CampoDoPainel(tela, "Situação").QuerySelectorAll("option").Select(o => o.TextContent).Should().Equal(
            "Todas", "Criada", "Direcionada", "Feita", "Para correção", "Corrigida", "Concluída", "Reaberta");
        CampoDoPainel(tela, "Situação").Change("Criada");
        CampoDoPainel(tela, "Revisor").Change(Diana.Id.ToString());
        tela.Find("form#filtros-de-atividades").Submit();

        UltimaConsulta.Status.Should().Be(StatusAtividade.Criada);
        UltimaConsulta.RevisorId.Should().Be(Diana.Id);
        UltimaConsulta.Busca.Should().Be("minuta");
        tela.FindAll("form#filtros-de-atividades").Should().BeEmpty();
        tela.Find("button[aria-controls=filtros-de-atividades]").GetAttribute("aria-expanded").Should().Be("false");

        tela.Find("button[aria-controls=filtros-de-atividades]").Click();
        var consultas = Despachante.Recebidas.OfType<ListarAtividades>().Count();
        tela.Find("form#filtros-de-atividades").QuerySelectorAll("button").Single(b => b.TextContent == "Limpar").Click();

        Despachante.Recebidas.OfType<ListarAtividades>().Should().HaveCount(consultas);   // limpar não consulta
        CampoDoPainel(tela, "Situação").QuerySelector("option[selected]")!.TextContent.Should().Be("Todas");
        CampoDoPainel(tela, "Revisor").QuerySelector("option[selected]")!.TextContent.Should().Be("Todos");
        tela.Find("form#filtros-de-atividades").Submit();
        UltimaConsulta.Status.Should().BeNull();
        UltimaConsulta.RevisorId.Should().BeNull();
        UltimaConsulta.ResponsavelId.Should().BeNull();
        UltimaConsulta.SomenteAtrasadas.Should().BeFalse();
        UltimaConsulta.Busca.Should().Be("minuta");
    }

    [Fact]
    public void Producao_tem_o_botao_mais_filtros_e_o_painel_sem_responsavel_nem_revisor()
    {
        var tela = Lista(AtorDaProducao);
        Texto(tela.Find("button[aria-controls=filtros-de-atividades]")).Should().Be("Mais filtros");

        tela.Find("button[aria-controls=filtros-de-atividades]").Click();

        var painel = tela.Find("form#filtros-de-atividades").TextContent;
        painel.Should().Contain("Situação").And.Contain("Só atrasadas").And.Contain("Filtrar");
        painel.Should().NotContain("Responsável").And.NotContain("Revisor");
        Despachante.Recebidas.OfType<ListarUsuarios>().Should().BeEmpty();
    }

    [Fact]
    public void Diretoria_com_responsavel_escolhido_continua_vendo_o_total_do_escritorio_no_subtitulo()
    {
        var tela = Lista(AtorDaDiretoria);
        CampoDaBarra(tela, "Responsável").Change(Pedro.Id.ToString());

        tela.Find("form.aj-barra-filtros").Submit();

        UltimaConsulta.ResponsavelId.Should().Be(Pedro.Id);
        // O subtítulo é o escopo (NoEscopo); a paginação é o que casou com o filtro (Total).
        Texto(tela.Find("header.aj-cabecalho p.aj-subtitulo")).Should().Be("8 atividades · 1 atrasada");
        Texto(tela.Find(".aj-paginacao .aj-subtitulo")).Should().Be("Página 1 de 1 · 1 atividade");
    }

    [Fact]
    public void Falha_da_consulta_mostra_o_erro_sem_subtitulo_nem_numeros_e_tentar_de_novo_recarrega()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok([Diana, Pedro]))
            .Responde<ListarAtividades, Resultado<PaginaDeAtividades>>(
                Resultado<PaginaDeAtividades>.Falha(TipoErro.Validacao, "Falha de teste."));
        var tela = Desenhar<Atividades>();

        tela.Markup.Should().Contain("Falha de teste.").And.Contain(">Tentar de novo</button>");
        tela.Markup.Should().NotContain("Nenhuma atividade.").And.NotContain("Página 1");
        tela.FindAll("header.aj-cabecalho p.aj-subtitulo").Should().BeEmpty();
        tela.FindAll(".aj-chips button").Take(2).Select(b => b.TextContent).Should().Equal("Todas", "Atrasadas");

        Despachante.Responde<ListarAtividades, Resultado<PaginaDeAtividades>>(Resultado<PaginaDeAtividades>.Ok(Pagina()));
        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.WaitForAssertion(() => Texto(tela.Find("header.aj-cabecalho p.aj-subtitulo")).Should().Be("8 atividades · 1 atrasada"));
        tela.FindAll(".aj-chips button").Take(2).Select(b => b.TextContent).Should().Equal("Todas · 8", "Atrasadas · 1");
        tela.Markup.Should().Contain("Contestação — cobrança de ISS").And.NotContain("Falha de teste.");
    }

    [Fact]
    public void Clique_num_chip_com_outra_consulta_em_curso_nao_dispara_outra()
    {
        var tela = Lista(AtorDaDiretoria);
        var resposta = new TaskCompletionSource<Resultado<PaginaDeAtividades>>();
        Despachante.RespondeDepois<ListarAtividades, Resultado<PaginaDeAtividades>>(resposta.Task);

        Chip(tela, "Feitas").Click();
        Chip(tela, "Concluídas").Click();

        Despachante.Recebidas.OfType<ListarAtividades>().Should().HaveCount(2);   // a carga inicial e a de "Feitas"
        resposta.SetResult(Resultado<PaginaDeAtividades>.Ok(Pagina()));
        tela.WaitForAssertion(() => Chip(tela, "Feitas").GetAttribute("aria-pressed").Should().Be("true"));
        Chip(tela, "Concluídas").GetAttribute("aria-pressed").Should().Be("false");
        UltimaConsulta.Status.Should().Be(StatusAtividade.Feita);
    }
}
```

(Se o nome `Atividades` ficar ambíguo no arquivo de teste, use `AtividadesJuridicas.Web.Components.Pages.Atividades` por inteiro e registre. O `HasAttribute("checked")` confere o atributo que o bUnit escreve para a caixa marcada; se a versão do bUnit não o escrever, confira pelo `IHtmlInputElement.IsChecked` e registre.)

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadesWebTests|FullyQualifiedName~AtividadesInterativosTests|FullyQualifiedName~LayoutWebTests"`
Expected: compila (a lista antiga existe e as peças das Tarefas 2 e 4 já estão no código); os testes novos e reescritos de `AtividadesWebTests` e todos os de `AtividadesInterativosTests` falham (a tela ainda é a antiga), e o `Tela_ainda_nao_migrada…` com `/agenda` passa. É o RED.

- [ ] **Step 2: A tela**

Reescreva `src/AtividadesJuridicas.Web/Components/Pages/Atividades.razor` inteiro. Saem o `EditForm` antigo, a tabela crua, o `FormularioDeAtividade` e a `FichaDaAtividade` embutidos e a lógica deles (`_criando`, `_versaoDoFormulario`, `_abertaId`, `Abrir`, `Nova`, `CancelarCriacao`, `CriadaAsync`, `FecharFicha`, `AtualizarListaAsync`, `_mensagem`/`_falhou`/`Avisar`).

```razor
@page "/atividades"
@inject IDespachante Despachante
@inject Recados Recados

<PageTitle>Atividades</PageTitle>

<CabecalhoDePagina Titulo="Atividades" Subtitulo="@Subtitulo" Alerta="@Alerta"
                   Acao="@(_ator?.EhDiretoria == true ? "Nova atividade" : null)" AcaoHref="/atividades/nova">
    <LinhaDoMobile>
        @if (_carregado && _ator is not null)
        {
            @* O Enter na busca envia o formulário "busca-de-atividades", que fica no fim da tela. *@
            <div class="aj-barra-superior__busca">
                <label class="aj-campo-icone"><Icone Nome="buscar" /><span class="aj-so-leitor">Buscar atividade</span><input type="search" class="aj-campo-icone__controle" form="busca-de-atividades" placeholder="Buscar pelo título" @bind="_filtro.Busca" /></label>
                <button type="button" class="aj-botao aj-botao--secundario aj-botao--icone" aria-label="Mais filtros" aria-expanded="@Expandido" aria-controls="filtros-de-atividades" @onclick="AlternarFiltros"><Icone Nome="filtros" /></button>
            </div>
            <div class="aj-chips aj-chips--barra" role="group" aria-label="Filtrar por situação">@Chips(ChipsDoMobile)</div>
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
    @if (_recado is { } recado)
    {
        <Aviso Tipo="@recado.Tipo">@recado.Texto</Aviso>
    }
    @if (_erroDosUsuarios is not null)
    {
        <Aviso Tipo="TipoDeAviso.Erro">@_erroDosUsuarios</Aviso>
    }

    @* Desktop: os campos aplicam ao clicar "Buscar"; a paginação e os chips consultam com o que está nos campos (como em Processos.razor). *@
    <div class="aj-so-desktop">
        <form class="aj-barra-filtros" role="search" @onsubmit="BuscarAsync" @onsubmit:preventDefault>
            <label class="aj-campo-icone aj-campo-icone--filtro"><Icone Nome="buscar" /><span class="aj-so-leitor">Busca</span><input type="text" class="aj-campo-icone__controle" placeholder="Buscar pelo título da atividade" @bind="_filtro.Busca" /></label>
            @CampoDeSituacao
            @if (_ator.EhDiretoria)
            {
                @CamposDePessoas
            }
            <label class="aj-opcao aj-opcao--solta"><input type="checkbox" class="aj-opcao__caixa" @bind="_filtro.SoAtrasadas" />Só atrasadas</label>
            <button type="submit" class="aj-botao aj-botao--secundario">Buscar</button>
        </form>
    </div>

    <div class="aj-so-tablet">
        <form class="aj-linha-filtros aj-linha-filtros--busca" role="search" @onsubmit="BuscarAsync" @onsubmit:preventDefault>
            <label class="aj-campo-icone"><Icone Nome="buscar" /><span class="aj-so-leitor">Buscar atividade</span><input type="search" class="aj-campo-icone__controle" placeholder="Buscar pelo título da atividade" @bind="_filtro.Busca" /></label>
            <button type="button" class="aj-botao aj-botao--secundario aj-botao--com-icone" aria-expanded="@Expandido" aria-controls="filtros-de-atividades" @onclick="AlternarFiltros"><Icone Nome="filtros" />@RotuloDoBotaoDeFiltros</button>
        </form>
    </div>

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

    <div class="aj-so-tablet">
        <div class="aj-chips" role="group" aria-label="Filtrar por situação">@Chips(ChipsDoTablet)</div>
    </div>

    <EstadoDaLista Erro="@_erro" Vazio="@(_pagina.Itens.Count == 0)" TextoDeVazio="Nenhuma atividade." TentarDeNovo="() => CarregarAsync(_paginaPedida)">
        <div class="aj-so-desktop">
            <div class="aj-tabela-caixa">
                <table class="aj-tabela aj-tabela--larga">
                    <thead><tr><th scope="col">Título</th><th scope="col">Situação</th><th scope="col">Entrega</th><th scope="col">Complexidade</th><th scope="col">Cliente</th><th scope="col">Processo</th><th scope="col">Responsáveis</th><th scope="col">Revisor</th><th scope="col"><span class="aj-so-leitor">Ações</span></th></tr></thead>
                    <tbody>
                        @foreach (var atividade in _pagina.Itens)
                        {
                            <tr @key="atividade.Id">
                                <td class="aj-tabela__principal">@atividade.Titulo</td>
                                <td><div class="aj-etiquetas"><EtiquetaDeSituacao Status="atividade.Status" Atrasada="atividade.Atrasada" NaTabela="true" /></div></td>
                                <td class="@ClasseDaEntrega(atividade)">@RotulosDeAtividade.Data(atividade.DataEntrega)</td>
                                <td>@atividade.Complexidade</td>
                                <td class="@ClasseOu(atividade.ClienteNome, null)">@Ou(atividade.ClienteNome)</td>
                                <td class="@ClasseOu(atividade.ProcessoTitulo, "aj-tabela__secundaria")">@Ou(atividade.ProcessoTitulo)</td>
                                <td class="@ClasseOu(Responsaveis(atividade), null)">@Ou(Responsaveis(atividade))</td>
                                <td class="@ClasseOu(atividade.RevisorNome, null)">@Ou(atividade.RevisorNome)</td>
                                <td class="aj-tabela__acao"><a href="@Ficha(atividade)" class="aj-botao aj-botao--fantasma aj-botao--seta">Abrir<Icone Nome="avancar" /></a></td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
            <Paginacao Numero="_pagina.Numero" TotalDePaginas="_pagina.TotalDePaginas" Total="_pagina.Total" Singular="atividade" Plural="atividades"
                       ComSetas="true" SempreVisivel="true" Ocupado="_ocupado" IrPara="CarregarAsync" />
        </div>

        <div class="aj-tablet-mobile">
            <div class="aj-grade">
                @foreach (var atividade in _pagina.Itens)
                {
                    <a href="@Ficha(atividade)" class="aj-cartao aj-cartao--atividade" @key="atividade.Id">
                        <span class="aj-cartao__topo"><span class="aj-cartao__etiquetas"><EtiquetaDeSituacao Status="atividade.Status" Atrasada="atividade.Atrasada" /></span><span class="aj-so-mobile"><Icone Nome="avancar" Classe="aj-cartao__seta" /></span></span>
                        <span class="aj-cartao__titulo">@atividade.Titulo</span>
                        <span class="aj-cartao__meta">@OuSem(atividade.ClienteNome, "Sem cliente")<br />@OuSem(atividade.ProcessoTitulo, "Sem processo vinculado")</span>
                        <span class="aj-cartao__rodape"><span class="@ClasseDaData(atividade)"><Icone Nome="agenda" />@RotulosDeAtividade.Data(atividade.DataEntrega)</span>@if (atividade.Responsaveis.Count == 0)
                            {<span class="aj-cartao__vazio">Sem responsável</span>}
                            else
                            {<span class="aj-avatares">@foreach (var nome in atividade.Responsaveis)
                                {<span class="aj-avatar aj-avatar--p @AvatarDePessoa.Classe(nome)" title="@nome">@AvatarDePessoa.Sigla(nome)</span>}</span>}</span>
                    </a>
                }
            </div>
        </div>
        <div class="aj-so-tablet">
            <Paginacao Numero="_pagina.Numero" TotalDePaginas="_pagina.TotalDePaginas" Total="_pagina.Total" Singular="atividade" Plural="atividades"
                       ComSetas="true" SempreVisivel="true" Ocupado="_ocupado" IrPara="CarregarAsync" />
        </div>
        <div class="aj-so-mobile">
            <Paginacao Numero="_pagina.Numero" TotalDePaginas="_pagina.TotalDePaginas" Total="_pagina.Total" Singular="atividade" Plural="atividades"
                       ComSetas="true" Ocupado="_ocupado" IrPara="CarregarAsync" />
        </div>
    </EstadoDaLista>

    <form id="busca-de-atividades" hidden @onsubmit="BuscarAsync" @onsubmit:preventDefault></form>
}

@code {
    private static readonly PaginaDeAtividades Vazia = new([], 0, 1, AtividadesOpcoes.TamanhoDaPagina, 0, 0);

    // Os chips de situação de cada visão, como nos quadros: o mobile não tem "Corrigidas"; Criada e Reaberta só no painel.
    private static readonly StatusAtividade[] ChipsDoTablet =
        [StatusAtividade.Direcionada, StatusAtividade.Feita, StatusAtividade.ParaCorrecao, StatusAtividade.Corrigida, StatusAtividade.Concluida];
    private static readonly StatusAtividade[] ChipsDoMobile =
        [StatusAtividade.Direcionada, StatusAtividade.Feita, StatusAtividade.ParaCorrecao, StatusAtividade.Concluida];

    private bool _carregado;
    private bool _ocupado;
    private bool _filtrosAbertos;
    private UsuarioAutenticado? _ator;
    private readonly Filtro _filtro = new();
    private PaginaDeAtividades _pagina = Vazia;
    private int _paginaPedida = 1;
    private string? _erro;
    private string? _erroDosUsuarios;
    private Recado? _recado;
    private IReadOnlyList<UsuarioResumo> _responsaveis = [];
    private IReadOnlyList<UsuarioResumo> _revisores = [];

    protected override async Task OnInitializedAsync()
    {
        _ator = await Despachante.EnviarAsync(new ObterMeuAcesso());
        if (_ator is { EhDiretoria: true }) await CarregarUsuariosAsync();
        if (_ator is not null)
        {
            _recado = Recados.Retirar();
            await CarregarAsync(1);
        }

        _carregado = true;
    }

    /// <summary>
    /// Os nomes dos filtros de responsável e revisor; só a Diretoria pode listar usuários. Inativos ficam, com
    /// " (inativo)", porque ainda constam em atividades.
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

        _responsaveis = [.. resultado.Valor
            .Where(u => u.TipoPerfil != TipoPerfil.Cliente)
            .OrderBy(u => u.Nome, StringComparer.CurrentCultureIgnoreCase)];
        _revisores = [.. _responsaveis.Where(u => u.TipoPerfil == TipoPerfil.Diretoria)];
    }

    // Uma consulta por vez: o clique duplo num chip ou na paginação não dispara outra.
    private async Task CarregarAsync(int pagina)
    {
        if (_ocupado) return;

        _ocupado = true;
        try
        {
            await ConsultarAsync(pagina);
        }
        finally
        {
            _ocupado = false;
        }
    }

    private async Task ConsultarAsync(int pagina)
    {
        _paginaPedida = pagina;
        var resultado = await Despachante.EnviarAsync(new ListarAtividades(
            _filtro.Situacao, _filtro.ResponsavelId, _filtro.RevisorId, null, null, _filtro.SoAtrasadas, _filtro.Busca, pagina));

        if (resultado.Erro == TipoErro.AcessoNegado)
        {
            EncerrarAcesso();
            return;
        }

        // Falha na consulta não é "nenhuma atividade": o erro fica no lugar da lista, sem os números.
        if (!resultado.Sucesso || resultado.Valor is null)
        {
            _erro = resultado.Mensagem ?? "Não foi possível consultar as atividades.";
            _pagina = Vazia;
            return;
        }

        _erro = null;
        _pagina = resultado.Valor;

        // Depois de mudar um filtro, a página pedida pode ter deixado de existir: volta à última.
        if (_pagina.Itens.Count == 0 && _pagina.Total > 0 && pagina > _pagina.TotalDePaginas)
            await ConsultarAsync(_pagina.TotalDePaginas);
    }

    private Task BuscarAsync()
    {
        if (_ocupado) return Task.CompletedTask;

        _recado = null;
        return CarregarAsync(1);
    }

    private Task EscolherTodasAsync() => EscolherAsync(null, somenteAtrasadas: false);

    private Task EscolherAtrasadasAsync() => EscolherAsync(null, somenteAtrasadas: true);

    private Task EscolherSituacaoAsync(StatusAtividade situacao) => EscolherAsync(situacao, somenteAtrasadas: false);

    // O chip troca a situação e o "só atrasadas" e busca na hora; a busca digitada, o responsável e o revisor ficam.
    // Com outra consulta em curso, o chip não muda nada (senão marcaria um filtro que não foi aplicado).
    private Task EscolherAsync(StatusAtividade? situacao, bool somenteAtrasadas)
    {
        if (_ocupado) return Task.CompletedTask;

        _filtro.Situacao = situacao;
        _filtro.SoAtrasadas = somenteAtrasadas;
        return BuscarAsync();
    }

    private void AlternarFiltros() => _filtrosAbertos = !_filtrosAbertos;

    // Esvazia o que o painel traz; a busca digitada fica. Só vale ao filtrar.
    private void LimparFiltros()
    {
        _filtro.Situacao = null;
        _filtro.ResponsavelId = null;
        _filtro.RevisorId = null;
        _filtro.SoAtrasadas = false;
    }

    private async Task FiltrarDoPainelAsync()
    {
        if (_ocupado) return;

        _filtrosAbertos = false;
        await BuscarAsync();
    }

    /// <summary>Uma resposta de acesso negado encerra a tela: nada do que estava à vista continua.</summary>
    private void EncerrarAcesso()
    {
        _ator = null;
        _pagina = Vazia;
        _responsaveis = [];
        _revisores = [];
        _erro = null;
        _erroDosUsuarios = null;
        _recado = null;
        _filtrosAbertos = false;
    }

    // O subtítulo conta o escopo do usuário (nunca o filtro), só com a lista carregada, sem erro e com atividade.
    private string? Subtitulo =>
        _carregado && _ator is not null && _erro is null && _pagina.NoEscopo > 0
            ? TextosDeAtividade.Contagem(_pagina.NoEscopo)
            : null;

    private string? Alerta => Subtitulo is null ? null : TextosDeAtividade.Atrasadas(_pagina.Atrasadas);

    // Os chips mostram os números do escopo com a consulta respondida; na falha, só os nomes.
    private string RotuloDeTodas => _erro is null ? $"Todas · {_pagina.NoEscopo}" : "Todas";

    private string RotuloDeAtrasadas => _erro is null ? $"Atrasadas · {_pagina.Atrasadas}" : "Atrasadas";

    private string PressionadoTodas => _filtro.Situacao is null && !_filtro.SoAtrasadas ? "true" : "false";

    private string PressionadoAtrasadas => _filtro.SoAtrasadas ? "true" : "false";

    private string Pressionado(StatusAtividade situacao) => _filtro.Situacao == situacao ? "true" : "false";

    private string Expandido => _filtrosAbertos ? "true" : "false";

    private string RotuloDoBotaoDeFiltros => _ator?.EhDiretoria == true ? "Responsável, revisor" : "Mais filtros";

    // "Todas", "Atrasadas" e as situações da visão. As chaves das situações não coincidem com nada no mesmo pai.
    private RenderFragment Chips(IReadOnlyList<StatusAtividade> situacoes) => @<text><button type="button" class="aj-chip" aria-pressed="@PressionadoTodas" @onclick="EscolherTodasAsync">@RotuloDeTodas</button><button type="button" class="aj-chip aj-chip--alerta" aria-pressed="@PressionadoAtrasadas" @onclick="EscolherAtrasadasAsync">@RotuloDeAtrasadas</button>@foreach (var situacao in situacoes)
        {<button type="button" class="aj-chip" aria-pressed="@Pressionado(situacao)" @key="situacao" @onclick="() => EscolherSituacaoAsync(situacao)">@RotulosDeAtividade.SituacaoNoPlural(situacao)</button>}</text>;

    // O campo de situação da barra do desktop e do painel: as oito opções, inclusive Criada e Reaberta, que não têm chip.
    private RenderFragment CampoDeSituacao => @<label class="aj-campo-filtro"><span>Situação</span><select class="aj-campo-filtro__controle" @bind="_filtro.Situacao"><option value="">Todas</option>@foreach (var situacao in Enum.GetValues<StatusAtividade>())
        {<option value="@situacao">@RotulosDeAtividade.Status(situacao)</option>}</select></label>;

    // Responsável (os não clientes) e revisor (a Diretoria): só para a Diretoria, na barra e no painel.
    private RenderFragment CamposDePessoas => @<text><label class="aj-campo-filtro"><span>Responsável</span><select class="aj-campo-filtro__controle" @bind="_filtro.ResponsavelId"><option value="">Todos</option>@foreach (var usuario in _responsaveis)
        {<option value="@usuario.Id">@Rotulo(usuario)</option>}</select></label><label class="aj-campo-filtro"><span>Revisor</span><select class="aj-campo-filtro__controle" @bind="_filtro.RevisorId"><option value="">Todos</option>@foreach (var diretor in _revisores)
        {<option value="@diretor.Id">@Rotulo(diretor)</option>}</select></label></text>;

    private static string Ficha(AtividadeResumo atividade) => $"/atividades/{atividade.Id}";

    private static string Rotulo(UsuarioResumo usuario) => usuario.Ativo ? usuario.Nome : $"{usuario.Nome} (inativo)";

    // Os nomes na ordem da atividade, unidos por vírgula; nulo sem responsável.
    private static string? Responsaveis(AtividadeResumo atividade) =>
        atividade.Responsaveis.Count == 0 ? null : string.Join(", ", atividade.Responsaveis);

    private static string Ou(string? valor) => string.IsNullOrWhiteSpace(valor) ? "—" : valor;

    private static string OuSem(string? valor, string semValor) => string.IsNullOrWhiteSpace(valor) ? semValor : valor;

    // O "—" de célula vazia tem a cor apagada do quadro; com valor, a classe da coluna (ou nenhuma).
    private static string? ClasseOu(string? valor, string? classe) => string.IsNullOrWhiteSpace(valor) ? "aj-tabela__vazia" : classe;

    private static string ClasseDaEntrega(AtividadeResumo atividade) =>
        atividade.DataEntrega is null ? "aj-tabela__vazia"
        : atividade.Atrasada ? "aj-tabela__data aj-tabela__data--atrasada"
        : "aj-tabela__data";

    private static string ClasseDaData(AtividadeResumo atividade) =>
        atividade.Atrasada ? "aj-cartao__data aj-cartao__data--atrasada" : "aj-cartao__data";

    /// <summary>O estado único dos filtros: as três visões o mostram e o mudam.</summary>
    private sealed class Filtro
    {
        public string? Busca { get; set; }
        public StatusAtividade? Situacao { get; set; }
        public Guid? ResponsavelId { get; set; }
        public Guid? RevisorId { get; set; }
        public bool SoAtrasadas { get; set; }
    }
}
```

Notas para quem implementa:

- O `@bind` dentro dos fragmentos do `@code` (`CampoDeSituacao`, `CamposDePessoas`) liga o mesmo `_filtro` da barra e do painel; se o compilador recusar o `@bind` num modelo de fragmento, escreva os campos na marcação (na barra e no painel, iguais) e registre.
- O `<form>` do tablet tem um campo de texto só: o Enter nele envia o formulário sem botão de envio (envio implícito do navegador).
- A célula de situação, a de entrega e as de texto vazio usam classe por expressão (ponto cego da trava, conferido na revisão): só `aj-etiquetas`, `aj-tabela__data`, `aj-tabela__data--atrasada`, `aj-tabela__vazia`, `aj-tabela__secundaria`, `aj-cartao__data`, `aj-cartao__data--atrasada` e as cores `aj-avatar--…` de `AvatarDePessoa.Classe`.
- Os links `/atividades/nova` e `/atividades/{id}` levam à página não encontrada até as Tarefas 7 e 10: a fase só entra na `main` com tudo pronto.

- [ ] **Step 3: Trava e testes**

Acrescente a `Migradas` (`tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`), entre `Components/Pages/Areas.razor` e `Components/Pages/Auditoria.razor`: `"Components/Pages/Atividades.razor",`.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadesWebTests|FullyQualifiedName~AtividadesInterativosTests|FullyQualifiedName~LayoutWebTests|FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~InicioWebTests|FullyQualifiedName~ProdutoresWebTests"`
Expected: 0 avisos; todos passando (os três testes do formulário antigo continuam verdes: o `FormularioDeAtividade` não mudou).

- [ ] **Step 4: Commit**

Antes, `git branch --show-current` (deve ser `worktree-feat+fase-9e-atividades`).

```
git add src tests
git commit -m "feat(web): lista de atividades no design system, com os contadores do escopo, os filtros de cada visão e os cartões com avatares"
```

---

### Task 7: Formulário de atividade em tela própria

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Pages/AtividadeFormulario.razor` (rotas `/atividades/nova` e `/atividades/{Id:guid}/editar`)
- Create: `tests/AtividadesJuridicas.IntegrationTests/AtividadeFormularioWebTests.cs`, `AtividadeFormularioInterativoTests.cs`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/AtividadesWebTests.cs` (os testes do formulário antigo passam a renderizar a página nova; complexidade em controle segmentado)
- Modify: `PortalDoClienteWebTests.cs` (theory das páginas internas), `AutenticacaoWebTests.cs` (theory de anônimo), `TelasNoDesignSystemTests.cs` (`Migradas`)

**Interfaces:**
- Consumes: `CargaDoFormulario` (Tarefa 3), `CabecalhoDeDetalhe` (`RotuloDoVoltarNoMobile`), `RotuloDeCampo` (com `Classe`), `Aviso`, `Recados`, `Icone`; `ObterMeuAcesso`, `ObterAtividade` (na edição, a tela carrega a própria atividade), `ListarAreasAtuacao`, `ObterProcesso`, `ListarProcessos`, `ListarClientes`, `SalvarAtividade` (com `VersaoEsperada` da carga).
- Produces: as rotas `/atividades/nova` e `/atividades/{Id:guid}/editar`. O componente antigo `Components/Atividades/FormularioDeAtividade.razor` continua até a Tarefa 14 (a ficha antiga o usa até a Tarefa 10).

**Regras:**
- Quadros: `AtividadeFormulario.dc.html`, `…Tablet`, `…Mobile` (`quadros-lista-e-formularios.md`, seção 2).
- A lógica vem do `FormularioDeAtividade.razor` de hoje (vínculo processo/cliente/área, buscas, regras de `EscolherProcessoAsync`, `RemoverProcesso`, `RemoverCliente`, o aviso do cliente que saiu do processo, áreas ativas mais a atual), sem mudar.
- `EditForm` com `id="formulario-da-atividade"` é o próprio `aj-lado-a-lado aj-lado-a-lado--formulario`: `section.aj-painel.aj-painel--principal.aj-lado-a-lado__lista` "Atividade" e `section.aj-painel.aj-lado-a-lado__painel` "Vínculos". Grade `aj-grade-campos`: `Título` (inteiro), `Data de entrega`, `Complexidade` (`div.aj-rotulo role="group" aria-label="Complexidade"` com `div.aj-segmentado` e três botões `type="button"` com `aria-pressed`), `Descrição` (inteiro, `textarea rows="4"`). Erros por campo: `Informe o título.` e `Informe a descrição.` com `RotuloDeCampo`; `Escolha a complexidade.` sem ele (o `RotuloDeCampo` emite `<label>`, que não cabe num grupo de botões): o grupo é escrito na tela, com a mesma forma de erro (`aj-rotulo--erro` e `span.aj-campo-erro`), e lê o erro do `EditContext`; por isso o `EditForm` usa `EditContext=`.
- Vínculos: processo escolhido em `div.aj-vinculo__escolhido.aj-vinculo__escolhido--detalhado` (`aj-vinculo__texto` com `Processo`, `aj-vinculo__titulo` e `Área {área}, definida pelo processo`; botão de ícone `Sem processo`); `Cliente do processo`; sem processo, `Área de atuação` e o cliente avulso (escolhido no mesmo `aj-vinculo__escolhido`, com a primeira linha `Cliente`, o nome e o botão de ícone `Sem cliente`); `O título aparece para o cliente no portal.` com cliente; as buscas de processo (exemplo `Escolher processo: …` sem processo escolhido, `Trocar o processo: …` com ele) e de cliente (exemplo `Escolher cliente: nome, CPF ou CNPJ`) em `aj-campo-icone`, cada uma num formulário escondido próprio pelo atributo `form` (o Enter busca e não salva), resultados em `aj-resultados` com "Escolher" (`aj-resultados__item`, como o `UsuarioFormulario`), com chaves distintas das linhas escolhidas.
- Ações fora do formulário, com `form="formulario-da-atividade"`: desktop `aj-so-desktop` > `aj-acoes aj-acoes--fixa` (`Cancelar` fantasma, `Salvar atividade` primário com `confirmar`); tablet `aj-so-tablet` (`Cancelar` secundário); mobile `aj-so-mobile` (`Cancelar` secundário, `Salvar`).
- Voltar: nova, `/atividades` ("Atividades"); edição, `/atividades/{id}` (o título da atividade, ou `Atividade` antes da carga e para a Produção; no mobile `Voltar para a atividade`). Salvar com sucesso: `Recados.Deixar("Atividade salva.")` e navega para o voltar (na nova, a lista). Falha: `Aviso` de erro no topo, o formulário fica com o digitado. Conflito ou não encontrado na edição: o aviso com a mensagem do servidor. Acesso negado: encerra a tela. Guarda de clique duplo (botão travado depois do sucesso).
- Produção: o cabeçalho e "Acesso negado.", sem formulário e sem consulta. Edição de atividade que não existe: `Atividade não encontrada.` com o voltar `/atividades`.
- Testes HTTP (rotas, Produção, não encontrada, os textos de cada visão, a complexidade marcada na edição) e bUnit (Enter na busca não salva; erros por campo sem envio; escolher processo preenche o cliente único; salvar envia `SalvarAtividade` com a versão e navega com o recado; clique duplo envia uma vez — Review Focus 4).

- [ ] **Step 1: Testes (devem falhar)**

Antes de escrever, releia os três quadros (`AtividadeFormulario.dc.html`, `AtividadeFormularioTablet.dc.html`, `AtividadeFormularioMobile.dc.html`), a seção 2 de `quadros-lista-e-formularios.md` e o `Components/Atividades/FormularioDeAtividade.razor` inteiro (a lógica de vínculo vem dele, sem mudar).

**`tests/AtividadesJuridicas.IntegrationTests/AtividadeFormularioWebTests.cs`** (novo):

```csharp
using System.Net;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Domain.Auditoria;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AtividadeFormularioWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _litoral, _minuta;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        var area = await _app.ExecutarAsync(_admin, new SalvarAreaAtuacao(null, "Trabalhista"));
        area.Sucesso.Should().BeTrue(area.Mensagem);
        var litoral = await _app.ExecutarAsync(_admin, new SalvarClientePessoaJuridica(
            null, new DadosDoClienteEntrada("Transportes Litoral Ltda.", null, null, new EnderecoEntrada(Uf: "AL"), null),
            "11.222.333/0001-81", "Litoral Transportes", null, null, null));
        litoral.Sucesso.Should().BeTrue(litoral.Mensagem);
        _litoral = litoral.Valor;
        var processo = await _app.ExecutarAsync(_admin, new SalvarProcesso(
            null, new DadosDoProcessoEntrada(null, "Litoral — reclamação trabalhista", area.Valor, null, null, null, null, null, null),
            [new ClienteDoProcessoEntrada(_litoral, PoloProcessual.Ativo)]));
        processo.Sucesso.Should().BeTrue(processo.Mensagem);
        var minuta = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Minuta de acordo trabalhista", "Redigir a minuta.", new DateOnly(2026, 10, 9), _litoral, processo.Valor, null, 2));
        minuta.Sucesso.Should().BeTrue(minuta.Mensagem);
        _minuta = minuta.Valor;
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<string> PaginaAsync(string rota) =>
        WebUtility.HtmlDecode(await (await _app.ClienteAutenticadoAsync()).GetStringAsync(rota));

    [Fact]
    public async Task Nova_atividade_traz_os_dois_paineis_a_complexidade_sem_escolha_e_as_acoes_de_cada_visao()
    {
        var html = await PaginaAsync("/atividades/nova");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Nova atividade</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Diga o que precisa ser feito e a que processo ou cliente se refere.</p>")
            .And.MatchRegex("<a href=\"/atividades\" class=\"aj-voltar\">\\s*<svg[^>]*>.*?</svg>\\s*Atividades</a>")
            .And.MatchRegex("<form[^>]*id=\"formulario-da-atividade\"[^>]*class=\"aj-lado-a-lado aj-lado-a-lado--formulario\"")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-painel--principal aj-lado-a-lado__lista\">\\s*<h2>Atividade</h2>\\s*<div class=\"aj-grade-campos\">")
            .And.MatchRegex("<label class=\"aj-rotulo aj-grade-campos__inteiro\">\\s*Título\\s*<input[^>]*maxlength=\"200\"")
            .And.MatchRegex("<label class=\"aj-rotulo\">Data de entrega<input[^>]*type=\"date\"")
            .And.MatchRegex(
                "<div class=\"aj-rotulo\" role=\"group\" aria-label=\"Complexidade\">Complexidade<div class=\"aj-segmentado\">\\s*" +
                "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>1</button>\\s*" +
                "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>2</button>\\s*" +
                "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>3</button>\\s*</div>")
            .And.MatchRegex("<label class=\"aj-rotulo aj-grade-campos__inteiro\">\\s*Descrição\\s*<textarea[^>]*rows=\"4\"")
            .And.MatchRegex("<textarea[^>]*maxlength=\"10000\"")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-lado-a-lado__painel\">\\s*<h2>Vínculos</h2>\\s*<label class=\"aj-rotulo\">Área de atuação<select class=\"aj-campo\"[^>]*>\\s*<option[^>]*>Nenhuma</option>\\s*<option[^>]*>Trabalhista</option>")
            .And.MatchRegex("<span class=\"aj-so-leitor\">Escolher processo \\(título, parte contrária ou número CNJ\\)</span>\\s*<input type=\"search\" class=\"aj-campo-icone__controle\" form=\"busca-de-processo\" placeholder=\"Escolher processo: título, parte contrária ou número CNJ\"")
            .And.MatchRegex("<span class=\"aj-so-leitor\">Escolher cliente \\(nome, CPF ou CNPJ\\)</span>\\s*<input type=\"search\" class=\"aj-campo-icone__controle\" form=\"busca-de-cliente\" placeholder=\"Escolher cliente: nome, CPF ou CNPJ\"")
            .And.Contain("<form id=\"busca-de-processo\" hidden").And.Contain("<form id=\"busca-de-cliente\" hidden");
        // Sem cliente escolhido, nada de lembrete do portal nem de processo escolhido.
        html.Should().NotContain("O título aparece para o cliente no portal.").And.NotContain("aria-label=\"Sem processo\"");

        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            "(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a href=\"/atividades\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>\\s*" +
            "<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-da-atividade\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Salvar atividade</button>");
        BlocoDeVisao.De(html, "aj-so-tablet").Should().MatchRegex(
            "(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a href=\"/atividades\" class=\"aj-botao aj-botao--secundario\">Cancelar</a>\\s*<button[^>]*form=\"formulario-da-atividade\"[^>]*>.*?Salvar atividade</button>");
        var mobile = BlocoDeVisao.De(html, "aj-so-mobile");
        mobile.Should().MatchRegex("(?s)<a href=\"/atividades\" class=\"aj-botao aj-botao--secundario\">Cancelar</a>\\s*<button[^>]*form=\"formulario-da-atividade\"[^>]*>.*?Salvar</button>");
        mobile.Should().NotContain("Salvar atividade");
        html.Should().MatchRegex("(?s)<header class=\"aj-barra-superior aj-barra-superior--voltar\">\\s*<a href=\"/atividades\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Voltar para Atividades\">.*?<h1>Nova atividade</h1>");
    }

    [Fact]
    public async Task Edicao_traz_o_processo_escolhido_o_cliente_do_processo_e_a_complexidade_marcada()
    {
        var html = await PaginaAsync($"/atividades/{_minuta}/editar");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Editar atividade</h1>")
            .And.MatchRegex($"<a href=\"/atividades/{_minuta}\" class=\"aj-voltar\">\\s*<svg[^>]*>.*?</svg>\\s*Minuta de acordo trabalhista</a>")
            .And.MatchRegex($"(?s)<header class=\"aj-barra-superior aj-barra-superior--voltar\">\\s*<a href=\"/atividades/{_minuta}\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Voltar para a atividade\">")
            .And.MatchRegex("<input[^>]*value=\"Minuta de acordo trabalhista\"")
            .And.MatchRegex("<input[^>]*value=\"2026-10-09\"")
            .And.MatchRegex("aria-pressed=\"false\"[^>]*>1</button>\\s*<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"true\"[^>]*>2</button>\\s*<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"false\"[^>]*>3</button>")
            .And.MatchRegex(
                "(?s)<div class=\"aj-vinculo__escolhido aj-vinculo__escolhido--detalhado\">\\s*<span class=\"aj-vinculo__texto\">\\s*" +
                "<span class=\"aj-ajuda\">Processo</span>\\s*<span class=\"aj-vinculo__titulo\">Litoral — reclamação trabalhista</span>\\s*" +
                "<span class=\"aj-ajuda\">Área Trabalhista, definida pelo processo</span>\\s*</span>\\s*" +
                "<button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Sem processo\"")
            .And.MatchRegex($"(?s)<label class=\"aj-rotulo\">Cliente do processo<select class=\"aj-campo\"[^>]*>.*?<option value=\"{_litoral}\" selected[^>]*>Transportes Litoral Ltda.</option>")
            .And.Contain("<span class=\"aj-ajuda\">O título aparece para o cliente no portal.</span>")
            // Com processo escolhido, a busca é para trocá-lo.
            .And.Contain("form=\"busca-de-processo\" placeholder=\"Trocar o processo: título, parte contrária ou número CNJ\"");
        // Com processo, a área é a dele e o cliente é um dos dele: sem o campo de área nem a busca de cliente.
        html.Should().NotContain("Área de atuação").And.NotContain("Escolher cliente (nome, CPF ou CNPJ)")
            .And.NotContain("Escolher processo: título");
        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex($"<a href=\"/atividades/{_minuta}\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>");
    }

    [Fact]
    public async Task Edicao_de_atividade_que_nao_existe_mostra_nao_encontrada_sem_formulario()
    {
        var html = await PaginaAsync($"/atividades/{Guid.NewGuid()}/editar");

        html.Should().Contain("Atividade não encontrada.")
            .And.MatchRegex("<a href=\"/atividades\" class=\"aj-voltar\">\\s*<svg[^>]*>.*?</svg>\\s*Atividades</a>");
        html.Should().NotContain("<h2>Vínculos</h2>").And.NotContain("Salvar atividade");
    }

    [Fact]
    public async Task Producao_ve_acesso_negado_sem_o_formulario_e_sem_consultar_a_atividade()
    {
        var pedro = await _app.CriarPedroAsync();
        var doPedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var nova = WebUtility.HtmlDecode(await doPedro.GetStringAsync("/atividades/nova"));
        var edicao = WebUtility.HtmlDecode(await doPedro.GetStringAsync($"/atividades/{_minuta}/editar"));
        await PaginaAsync($"/atividades/{_minuta}/editar");   // a Diretoria abre a mesma edição

        nova.Should().Contain("Acesso negado.").And.MatchRegex("<h1 class=\"aj-titulo\">Nova atividade</h1>");
        nova.Should().NotContain("<h2>Vínculos</h2>").And.NotContain("Salvar atividade");
        edicao.Should().Contain("Acesso negado.").And.MatchRegex("<h1 class=\"aj-titulo\">Editar atividade</h1>");
        edicao.Should().NotContain("<h2>Atividade</h2>").And.NotContain("Minuta de acordo trabalhista");
        // A tela confere o perfil antes de consultar: a leitura da atividade só fica no registro da Diretoria.
        var leituras = await _app.EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking()
            .Where(a => a.TipoRecurso == "Atividade" && a.Acao == AcaoDeAcesso.Visualizou && a.RecursoId == _minuta).ToListAsync());
        leituras.Should().Contain(a => a.UsuarioId == _admin).And.NotContain(a => a.UsuarioId == pedro);
    }
}
```

**`tests/AtividadesJuridicas.IntegrationTests/AtividadeFormularioInterativoTests.cs`** (novo, bUnit; o `NavigationManager` do bUnit registra o destino):

```csharp
using AngleSharp.Dom;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

public class AtividadeFormularioInterativoTests : TelaInterativa
{
    private static readonly Guid AtividadeId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000f1");
    private static readonly Guid Area = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a9");
    private static readonly Guid ProcessoId = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b1");
    private static readonly Guid Litoral = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c1");

    private static readonly ProcessoResumo Encontrado = new(
        ProcessoId, null, "Litoral — reclamação trabalhista", "Trabalhista", StatusProcesso.Ativo, ["Transportes Litoral Ltda."], null);

    private static readonly ProcessoDetalhe Processo = new(
        ProcessoId, null, "Litoral — reclamação trabalhista", Area, "Trabalhista", StatusProcesso.Ativo,
        null, null, null, null, null, null,
        [new ClienteDoProcessoDetalhe(Litoral, "Transportes Litoral Ltda.", "11.222.333/0001-81", PoloProcessual.Ativo, true)]);

    // Atividade sem processo, na área Trabalhista, com complexidade 2.
    private static AtividadeDetalhe Minuta(uint versao) => new(
        AtividadeId, "Minuta de acordo trabalhista", "Redigir a minuta.", new DateOnly(2026, 10, 9), StatusAtividade.Criada, false,
        null, null, null, null, null, Area, "Trabalhista", TipoAutor.Usuario, "Diana Diretora", null, null, [], [],
        new AcoesDaAtividade(true, true, true, false, false, false, true), versao, 2);

    // A Diretoria e as respostas de que o formulário precisa: as áreas, a busca e a leitura do processo e o salvar.
    private DespachanteCombinado Respostas() => Despachante
        .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
        .Responde<ListarAreasAtuacao, Resultado<IReadOnlyList<AreaAtuacaoResumo>>>(
            Resultado<IReadOnlyList<AreaAtuacaoResumo>>.Ok([new AreaAtuacaoResumo(Area, "Trabalhista", true)]))
        .Responde<ListarProcessos, Resultado<PaginaDeProcessos>>(Resultado<PaginaDeProcessos>.Ok(new([Encontrado], 1, 1, 20, 1)))
        .Responde<ObterProcesso, Resultado<ProcessoDetalhe>>(Resultado<ProcessoDetalhe>.Ok(Processo))
        .Responde<SalvarAtividade, Resultado<Guid>>(Resultado<Guid>.Ok(AtividadeId));

    private IRenderedComponent<AtividadeFormulario> Nova()
    {
        Respostas();
        return Desenhar<AtividadeFormulario>();
    }

    private IRenderedComponent<AtividadeFormulario> Edicao(uint versao = 7)
    {
        Respostas().Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(Resultado<AtividadeDetalhe>.Ok(Minuta(versao)));
        return Desenhar<AtividadeFormulario>(p => p.Add(x => x.Id, (Guid?)AtividadeId));
    }

    private static IElement Rotulo(IRenderedComponent<AtividadeFormulario> tela, string texto) =>
        tela.FindAll("label.aj-rotulo").Single(l => l.TextContent.TrimStart().StartsWith(texto));

    private static void Digitar(IRenderedComponent<AtividadeFormulario> tela, string rotulo, string valor) =>
        Rotulo(tela, rotulo).QuerySelector("input, textarea")!.Change(valor);

    private static void Complexidade(IRenderedComponent<AtividadeFormulario> tela, string nivel) =>
        tela.FindAll("div.aj-segmentado button").Single(b => b.TextContent == nivel).Click();

    private static void ProcurarProcesso(IRenderedComponent<AtividadeFormulario> tela, string texto)
    {
        tela.Find("input[form=busca-de-processo]").Input(texto);
        tela.Find("form#busca-de-processo").Submit();
    }

    private string Endereco => Services.GetRequiredService<NavigationManager>().Uri;

    private SalvarAtividade Enviada => Despachante.Recebidas.OfType<SalvarAtividade>().Should().ContainSingle().Subject;

    [Fact]
    public void Enter_na_busca_de_processo_mostra_os_resultados_e_nao_salva()
    {
        var tela = Nova();

        ProcurarProcesso(tela, "litoral");

        Despachante.Recebidas.OfType<ListarProcessos>().Should().ContainSingle().Which.Busca.Should().Be("litoral");
        tela.Find("ul.aj-resultados .aj-resultados__titulo").TextContent.Should().Be("Litoral — reclamação trabalhista");
        tela.Find("ul.aj-resultados .aj-resultados__meta").TextContent.Should().Be("Trabalhista · Transportes Litoral Ltda.");
        tela.Find("ul.aj-resultados .aj-resultados__acao").TextContent.Should().Be("Escolher");
        Despachante.Recebidas.OfType<SalvarAtividade>().Should().BeEmpty();   // o Enter da busca não envia a atividade
    }

    [Fact]
    public void Escolher_o_processo_preenche_o_unico_cliente_e_a_area_vem_do_processo()
    {
        var tela = Nova();
        ProcurarProcesso(tela, "litoral");

        tela.Find("button.aj-resultados__item").Click();

        Despachante.Recebidas.OfType<ObterProcesso>().Should().ContainSingle().Which.Id.Should().Be(ProcessoId);
        tela.Find("div.aj-vinculo__escolhido .aj-vinculo__titulo").TextContent.Should().Be("Litoral — reclamação trabalhista");
        tela.Markup.Should().Contain("Área Trabalhista, definida pelo processo").And.Contain("O título aparece para o cliente no portal.");
        Rotulo(tela, "Cliente do processo").QuerySelector("option[selected]")!.TextContent.Should().Be("Transportes Litoral Ltda.");
        tela.FindAll("ul.aj-resultados").Should().BeEmpty();
        tela.FindAll("label.aj-rotulo").Should().NotContain(l => l.TextContent.StartsWith("Área de atuação"));
    }

    [Fact]
    public void Salvar_sem_titulo_descricao_nem_complexidade_mostra_os_erros_nos_campos_e_nao_envia()
    {
        var tela = Nova();

        tela.Find("form#formulario-da-atividade").Submit();

        tela.Markup.Should().Contain("Informe o título.").And.Contain("Informe a descrição.").And.Contain("Escolha a complexidade.");
        Rotulo(tela, "Título").ClassList.Should().Contain("aj-rotulo--erro");
        tela.Find("div.aj-rotulo.aj-rotulo--erro[role=group]").TextContent.Should().Contain("Escolha a complexidade.");
        Despachante.Recebidas.OfType<SalvarAtividade>().Should().BeEmpty();

        Complexidade(tela, "2");

        // Escolher o nível revalida o campo: o erro dele sai e os outros ficam.
        tela.Markup.Should().NotContain("Escolha a complexidade.").And.Contain("Informe o título.");
        tela.Find("div.aj-segmentado button[aria-pressed=true]").TextContent.Should().Be("2");
    }

    [Fact]
    public void Nova_atividade_salva_envia_os_campos_e_volta_para_a_lista_com_o_recado()
    {
        var tela = Nova();
        Digitar(tela, "Título", "Levantamento de certidões");
        Digitar(tela, "Descrição", "Pedir as certidões do imóvel.");
        Complexidade(tela, "3");
        Rotulo(tela, "Área de atuação").QuerySelector("select")!.Change(Area.ToString());

        tela.Find("form#formulario-da-atividade").Submit();

        tela.WaitForAssertion(() => Endereco.Should().EndWith("/atividades"));
        var enviada = Enviada;
        enviada.Id.Should().BeNull();
        enviada.Titulo.Should().Be("Levantamento de certidões");
        enviada.Descricao.Should().Be("Pedir as certidões do imóvel.");
        enviada.Complexidade.Should().Be(3);
        enviada.AreaAtuacaoId.Should().Be(Area);
        enviada.ClienteId.Should().BeNull();
        enviada.ProcessoId.Should().BeNull();
        enviada.VersaoEsperada.Should().BeNull();
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Atividade salva.");
    }

    [Fact]
    public void Edicao_envia_a_versao_da_carga_e_volta_para_a_ficha_com_o_recado()
    {
        var tela = Edicao(versao: 7);
        Digitar(tela, "Título", "Minuta de acordo revisada");

        tela.Find("form#formulario-da-atividade").Submit();

        tela.WaitForAssertion(() => Endereco.Should().EndWith($"/atividades/{AtividadeId}"));
        var enviada = Enviada;
        enviada.Id.Should().Be(AtividadeId);
        enviada.Titulo.Should().Be("Minuta de acordo revisada");
        enviada.DataEntrega.Should().Be(new DateOnly(2026, 10, 9));
        enviada.Complexidade.Should().Be(2);
        enviada.AreaAtuacaoId.Should().Be(Area);
        enviada.VersaoEsperada.Should().Be(7u);
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Atividade salva.");
    }

    [Fact]
    public void Falha_ao_salvar_mostra_o_aviso_e_mantem_o_que_foi_digitado()
    {
        var tela = Edicao();
        Despachante.Responde<SalvarAtividade, Resultado<Guid>>(Resultado<Guid>.Falha(
            TipoErro.Conflito, "A atividade foi alterada por outra pessoa. Recarregue e tente novamente."));
        Digitar(tela, "Título", "Minuta de acordo revisada");

        tela.Find("form#formulario-da-atividade").Submit();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("A atividade foi alterada por outra pessoa."));
        Rotulo(tela, "Título").QuerySelector("input")!.GetAttribute("value").Should().Be("Minuta de acordo revisada");
        tela.FindAll("button[form=formulario-da-atividade]").Should().HaveCount(3).And.OnlyContain(b => !b.HasAttribute("disabled"));
        Endereco.Should().NotEndWith($"/atividades/{AtividadeId}");
    }

    [Fact]
    public void Clique_duplo_em_salvar_envia_uma_vez_e_o_botao_fica_travado_ate_a_navegacao()
    {
        var resposta = new TaskCompletionSource<Resultado<Guid>>();
        var tela = Edicao();
        Despachante.RespondeDepois<SalvarAtividade, Resultado<Guid>>(resposta.Task);

        tela.Find("form#formulario-da-atividade").Submit();
        tela.Find("form#formulario-da-atividade").Submit();   // o segundo clique, com o primeiro envio em curso
        resposta.SetResult(Resultado<Guid>.Ok(AtividadeId));
        tela.WaitForAssertion(() => Endereco.Should().EndWith($"/atividades/{AtividadeId}"));
        tela.Find("form#formulario-da-atividade").Submit();   // um clique tardio, com a navegação a caminho

        Despachante.Recebidas.OfType<SalvarAtividade>().Should().ContainSingle();
        tela.FindAll("button[form=formulario-da-atividade]").Should().HaveCount(3).And.OnlyContain(b => b.HasAttribute("disabled"));
    }

    [Fact]
    public void Falha_ao_consultar_a_atividade_mostra_tentar_de_novo_e_o_clique_recarrega()
    {
        Respostas().Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(
            Resultado<AtividadeDetalhe>.Falha(TipoErro.Validacao, "Falha de teste."));
        var tela = Desenhar<AtividadeFormulario>(p => p.Add(x => x.Id, (Guid?)AtividadeId));

        tela.Markup.Should().Contain("Falha de teste.");
        tela.FindAll("form#formulario-da-atividade").Should().BeEmpty();

        Despachante.Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(Resultado<AtividadeDetalhe>.Ok(Minuta(7)));
        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.WaitForAssertion(() => tela.Find("form#formulario-da-atividade"));
        Rotulo(tela, "Título").QuerySelector("input")!.GetAttribute("value").Should().Be("Minuta de acordo trabalhista");
        tela.Markup.Should().NotContain("Falha de teste.");
    }

    [Fact]
    public void Producao_ve_acesso_negado_sem_consultar_nada_alem_do_proprio_acesso()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaProducao);

        var tela = Desenhar<AtividadeFormulario>(p => p.Add(x => x.Id, (Guid?)AtividadeId));

        tela.Markup.Should().Contain("Acesso negado.").And.Contain("Editar atividade");
        tela.FindAll("form#formulario-da-atividade").Should().BeEmpty();
        Despachante.Recebidas.Should().ContainSingle().Which.Should().BeOfType<ObterMeuAcesso>();
    }
}
```

(Os construtores de `ProcessoResumo`, `ProcessoDetalhe`, `ClienteDoProcessoDetalhe`, `AtividadeDetalhe` e `AcoesDaAtividade` são os de hoje. O `EditForm` com `FormName` já funciona no bUnit nos formulários da 9D; se pedir o estado de antifalsificação, siga o registro da 9D na `TelaInterativa`.)

**`tests/AtividadesJuridicas.IntegrationTests/AtividadesWebTests.cs`** — os três testes do formulário antigo passam a desenhar a página nova (`TelaDeTeste.RenderizarAsync<AtividadeFormulario>`, com o parâmetro de rota `Id`), e a complexidade passa a ser lida do controle segmentado. O `using AtividadesJuridicas.Web.Components.Pages;` a Tarefa 6 já pôs. Os auxiliares `SeletorDeComplexidade` e `Opcoes` (presos ao `<select>`) saem; no lugar:

```csharp
    // ---- Complexidade ----

    /// <summary>Os níveis do controle segmentado de complexidade do formulário, cada um com se está marcado.</summary>
    private static List<(string Nivel, bool Marcado)> Complexidade(string html)
    {
        var grupo = Regex.Match(html,
            "<div class=\"aj-rotulo\" role=\"group\" aria-label=\"Complexidade\">\\s*Complexidade\\s*<div class=\"aj-segmentado\">(.*?)</div>",
            RegexOptions.Singleline);
        grupo.Success.Should().BeTrue("o formulário deve ter o campo Complexidade");
        return [.. Regex.Matches(grupo.Groups[1].Value,
                "<button type=\"button\" class=\"aj-segmentado__opcao\" aria-pressed=\"(true|false)\"[^>]*>\\s*(\\d)\\s*</button>")
            .Select(m => (m.Groups[2].Value, m.Groups[1].Value == "true"))];
    }

    // O formulário em tela própria: nova (sem Id) ou a edição da atividade informada.
    private async Task<string> FormularioAsync(Guid? id) =>
        WebUtility.HtmlDecode(await TelaDeTeste.RenderizarAsync<AtividadeFormulario>(_app, _admin,
            id is { } atividade ? new() { [nameof(AtividadeFormulario.Id)] = atividade } : new()));
```

Como cada asserção antiga é reescrita:

| Teste e asserção antiga | O que prova | Forma nova |
|---|---|---|
| `Formulario_de_atividade_nova_…`: `Opcoes(select)` igual a `Selecione…`, `1`, `2`, `3` | o campo oferece os três níveis | `Complexidade(html)` traz os níveis `1`, `2`, `3`, na ordem |
| idem: `Opcoes(select, soASelecionada: true)` igual a `Selecione…` | atividade nova começa sem nível escolhido | nenhum dos três com `aria-pressed="true"` |
| `Formulario_de_edicao_…`: a opção selecionada é `2` | a edição abre com a complexidade da atividade | só o nível `2` marcado; a página carrega a própria atividade (sem o `Atual` do pai) |
| `Formulario_de_atividade_lembra_…`: `Contain(lembrete)` na atividade com cliente; `NotContain(lembrete)` na nova e na sem cliente | o lembrete do portal só aparece com cliente | as mesmas três, com a página nova; as duas negativas ganham a positiva `Contain("<h2>Vínculos</h2>")` no mesmo HTML (um HTML de erro passaria nelas) |

```csharp
    [Fact]
    public async Task Formulario_de_atividade_nova_pede_a_complexidade_e_comeca_sem_escolha()
    {
        var html = await FormularioAsync(null);

        Complexidade(html).Should().Equal(("1", false), ("2", false), ("3", false));
    }

    [Fact]
    public async Task Formulario_de_edicao_abre_com_a_complexidade_da_atividade()
    {
        var html = await FormularioAsync(_contestacao);

        Complexidade(html).Where(n => n.Marcado).Select(n => n.Nivel).Should().Equal("2");
    }

    [Fact]
    public async Task Formulario_de_atividade_lembra_que_o_titulo_aparece_para_o_cliente()
    {
        const string lembrete = "O título aparece para o cliente no portal.";
        var cliente = await _app.ExecutarAsync(_admin, new SalvarClientePessoaFisica(
            null, new DadosDoClienteEntrada("Maria Silva", null, null, new EnderecoEntrada(), null), "529.982.247-25", null, null));
        cliente.Sucesso.Should().BeTrue(cliente.Mensagem);
        var comCliente = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Petição da Maria", "Texto.", null, cliente.Valor, null, null, 1));
        comCliente.Sucesso.Should().BeTrue(comCliente.Mensagem);

        var daAtividadeComCliente = await FormularioAsync(comCliente.Valor);
        var daAtividadeNova = await FormularioAsync(null);
        var daAtividadeSemCliente = await FormularioAsync(_contestacao);

        daAtividadeComCliente.Should().Contain(lembrete);
        daAtividadeNova.Should().Contain("<h2>Vínculos</h2>").And.NotContain(lembrete);
        daAtividadeSemCliente.Should().Contain("<h2>Vínculos</h2>").And.NotContain(lembrete);
    }
```

(Se o arquivo ficar sem uso de `ObterAtividade` ou de `AtividadesJuridicas.Web.Components.Atividades`, o `using` que sobrar fica: a lista da Tarefa 6 usa `AvatarDePessoa`.)

**`PortalDoClienteWebTests.cs`** (`Cliente_em_pagina_interna_ve_nao_encontrada_e_nenhum_dado`) e **`AutenticacaoWebTests.cs`** (`Anonimo_e_redirecionado_ao_login_em_qualquer_rota`): acrescente

```csharp
    [InlineData("/atividades/nova")]
    [InlineData("/atividades/0199a3f2-7c1e-7000-8000-000000000001/editar")]
```

Run: `dotnet build -m:1`
Expected: não compila (`AtividadeFormulario` não existe). É o RED.

- [ ] **Step 2: A tela**

Crie `src/AtividadesJuridicas.Web/Components/Pages/AtividadeFormulario.razor`:

```razor
@page "/atividades/nova"
@page "/atividades/{Id:guid}/editar"
@using System.ComponentModel.DataAnnotations
@inject IDespachante Despachante
@inject NavigationManager Navegacao
@inject Recados Recados

<PageTitle>@Titulo</PageTitle>

<CabecalhoDeDetalhe Titulo="@Titulo" Subtitulo="@SubtituloDoFormulario" VoltarPara="@VoltarDoCabecalho" Origem="@OrigemDoCabecalho"
                    RotuloDoVoltarNoMobile="@RotuloDoVoltarNoMobile" />

<CargaDoFormulario Estado="_estado" Erro="@_erroDaCarga" TextoNaoEncontrado="Atividade não encontrada." TentarDeNovo="CarregarAsync">
    @if (_mensagem is not null)
    {
        <Aviso Tipo="TipoDeAviso.Erro">@_mensagem</Aviso>
    }

    <EditForm EditContext="_contexto" OnValidSubmit="SalvarAsync" FormName="atividade" id="formulario-da-atividade" class="aj-lado-a-lado aj-lado-a-lado--formulario">
        <DataAnnotationsValidator />
        <section class="aj-painel aj-painel--principal aj-lado-a-lado__lista">
            <h2>Atividade</h2>
            <div class="aj-grade-campos">
                <RotuloDeCampo Texto="Título" Para="() => _form.Titulo" Classe="aj-grade-campos__inteiro"><InputText @bind-Value="_form.Titulo" maxlength="@Atividade.TamanhoMaximoTitulo" class="aj-campo" /></RotuloDeCampo>
                <label class="aj-rotulo">Data de entrega<InputDate @bind-Value="_form.DataEntrega" class="aj-campo" /></label>
                @* Os três níveis do domínio (ComplexidadeMinima a ComplexidadeMaxima). Um grupo de botões não cabe num label:
                   o rótulo é o próprio grupo, e o erro do campo vem do contexto do formulário, como no RotuloDeCampo. *@
                <div class="@ClasseDaComplexidade" role="group" aria-label="Complexidade">Complexidade<div class="aj-segmentado"><button type="button" class="aj-segmentado__opcao" aria-pressed="@Marcada(1)" @onclick="() => EscolherComplexidade(1)">1</button><button type="button" class="aj-segmentado__opcao" aria-pressed="@Marcada(2)" @onclick="() => EscolherComplexidade(2)">2</button><button type="button" class="aj-segmentado__opcao" aria-pressed="@Marcada(3)" @onclick="() => EscolherComplexidade(3)">3</button></div>@foreach (var erro in ErrosDaComplexidade)
                    {<span class="aj-campo-erro">@erro</span>}</div>
                <RotuloDeCampo Texto="Descrição" Para="() => _form.Descricao" Classe="aj-grade-campos__inteiro"><InputTextArea @bind-Value="_form.Descricao" rows="4" maxlength="@Atividade.TamanhoMaximoDescricao" class="aj-campo" /></RotuloDeCampo>
            </div>
        </section>

        <section class="aj-painel aj-lado-a-lado__painel">
            <h2>Vínculos</h2>
            @if (_processo is { } processo)
            {
                <div class="aj-vinculo__escolhido aj-vinculo__escolhido--detalhado"><span class="aj-vinculo__texto"><span class="aj-ajuda">Processo</span><span class="aj-vinculo__titulo">@processo.Titulo</span><span class="aj-ajuda">@($"Área {processo.AreaNome}, definida pelo processo")</span></span><button type="button" class="aj-botao aj-botao--fantasma aj-botao--icone" aria-label="Sem processo" disabled="@_salvando" @onclick="RemoverProcesso"><Icone Nome="fechar" /></button></div>
                <label class="@(ClienteForaDoProcesso(processo) ? "aj-rotulo aj-rotulo--erro" : "aj-rotulo")">Cliente do processo<select class="aj-campo" @bind="_clienteId"><option value="@Guid.Empty">Selecione…</option>@if (ClienteForaDoProcesso(processo))
                    {<option value="@_clienteId">@($"{_clienteNome ?? "Cliente"} (não é mais cliente deste processo)")</option>}@foreach (var cliente in processo.Clientes)
                    {<option value="@cliente.ClienteId">@(cliente.Ativo ? cliente.Nome : $"{cliente.Nome} (inativo)")</option>}</select>@if (ClienteForaDoProcesso(processo))
                    {<span class="aj-campo-erro">O cliente atual não é mais cliente deste processo. Escolha outro cliente do processo para salvar.</span>}</label>
            }
            else
            {
                <label class="aj-rotulo">Área de atuação<select class="aj-campo" @bind="_areaId"><option value="@Guid.Empty">Nenhuma</option>@foreach (var area in _areas)
                    {<option value="@area.Id">@(area.Ativo ? area.Nome : $"{area.Nome} (inativa)")</option>}</select></label>
                @if (_clienteId != Guid.Empty)
                {
                    <div class="aj-vinculo__escolhido aj-vinculo__escolhido--detalhado"><span class="aj-vinculo__texto"><span class="aj-ajuda">Cliente</span><span class="aj-vinculo__titulo">@_clienteNome</span></span><button type="button" class="aj-botao aj-botao--fantasma aj-botao--icone" aria-label="Sem cliente" disabled="@_salvando" @onclick="RemoverCliente"><Icone Nome="fechar" /></button></div>
                }
            }
            @if (_clienteId != Guid.Empty)
            {
                <span class="aj-ajuda">O título aparece para o cliente no portal.</span>
            }

            @* O Enter nas buscas envia os formulários escondidos do fim da tela, e não o da atividade. *@
            <label class="aj-campo-icone"><Icone Nome="buscar" /><span class="aj-so-leitor">Escolher processo (título, parte contrária ou número CNJ)</span><input type="search" class="aj-campo-icone__controle" form="busca-de-processo" placeholder="@ExemploDaBuscaDeProcesso" @bind="_buscaDeProcesso" @bind:event="oninput" /></label>
            @if (_processosEncontrados.Count > 0)
            {
                <ul class="aj-resultados">@foreach (var candidato in _processosEncontrados)
                    {<li @key='("processo", candidato.Id)'><button type="button" class="aj-resultados__item" disabled="@_salvando" @onclick="() => EscolherProcessoAsync(candidato.Id)"><span class="aj-resultados__texto"><span class="aj-resultados__titulo">@candidato.Titulo</span><span class="aj-resultados__meta">@MetaDoProcesso(candidato)</span></span><span class="aj-resultados__acao">Escolher</span></button></li>}</ul>
            }
            @if (_nenhumProcesso)
            {
                <span class="aj-ajuda">Nenhum processo encontrado.</span>
            }
            @if (_processosTruncados)
            {
                <span class="aj-ajuda">Mostrando os primeiros resultados. Refine a busca para ver outros.</span>
            }

            @if (_processo is null)
            {
                <label class="aj-campo-icone"><Icone Nome="buscar" /><span class="aj-so-leitor">Escolher cliente (nome, CPF ou CNPJ)</span><input type="search" class="aj-campo-icone__controle" form="busca-de-cliente" placeholder="Escolher cliente: nome, CPF ou CNPJ" @bind="_buscaDeCliente" @bind:event="oninput" /></label>
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
            }
        </section>
    </EditForm>

    @* As barras de ação ficam fora do formulário e o enviam pelo atributo form. *@
    <div class="aj-so-desktop">
        <div class="aj-acoes aj-acoes--fixa"><a href="@Voltar" class="aj-botao aj-botao--fantasma">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" form="formulario-da-atividade" disabled="@_salvando"><Icone Nome="confirmar" />Salvar atividade</button></div>
    </div>
    <div class="aj-so-tablet">
        <div class="aj-acoes aj-acoes--fixa"><a href="@Voltar" class="aj-botao aj-botao--secundario">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" form="formulario-da-atividade" disabled="@_salvando"><Icone Nome="confirmar" />Salvar atividade</button></div>
    </div>
    <div class="aj-so-mobile">
        <div class="aj-acoes aj-acoes--fixa"><a href="@Voltar" class="aj-botao aj-botao--secundario">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" form="formulario-da-atividade" disabled="@_salvando"><Icone Nome="confirmar" />Salvar</button></div>
    </div>

    <form id="busca-de-processo" hidden @onsubmit="ProcurarProcessosAsync" @onsubmit:preventDefault></form>
    <form id="busca-de-cliente" hidden @onsubmit="ProcurarClientesAsync" @onsubmit:preventDefault></form>
</CargaDoFormulario>

@code {
    [Parameter] public Guid? Id { get; set; }

    private EstadoDeCarga _estado = EstadoDeCarga.Carregando;
    private string? _erroDaCarga;
    private bool _salvando;
    private string? _mensagem;

    private Formulario _form = new();
    private EditContext? _contexto;
    private uint? _versao;
    private string? _tituloDaAtividade;

    // O vínculo, como no FormularioDeAtividade: o cliente (Guid.Empty = nenhum), a área (só sem processo) e o processo.
    private Guid _clienteId;
    private string? _clienteNome;
    private Guid _areaId;
    private ProcessoDetalhe? _processo;
    private IReadOnlyList<AreaAtuacaoResumo> _areas = [];

    private string? _buscaDeProcesso;
    private IReadOnlyList<ProcessoResumo> _processosEncontrados = [];
    private bool _nenhumProcesso;
    private bool _processosTruncados;

    private string? _buscaDeCliente;
    private IReadOnlyList<ClienteResumo> _clientesEncontrados = [];
    private bool _nenhumCliente;
    private bool _clientesTruncados;

    private string Titulo => Id is null ? "Nova atividade" : "Editar atividade";

    // Depois de salvar: a nova volta à lista; a edição, à ficha.
    private string Voltar => Id is null ? "/atividades" : $"/atividades/{Id}";

    // Sem processo escolhido, a busca escolhe um; com processo, troca o escolhido (o exemplo do quadro).
    private string ExemploDaBuscaDeProcesso => _processo is null
        ? "Escolher processo: título, parte contrária ou número CNJ"
        : "Trocar o processo: título, parte contrária ou número CNJ";

    // Sem a atividade para voltar a ela (nova, ou edição de uma que não existe), o voltar vai para a lista.
    private bool VoltaParaALista => Id is null || _estado == EstadoDeCarga.NaoEncontrado;
    private string VoltarDoCabecalho => VoltaParaALista ? "/atividades" : Voltar;
    private string OrigemDoCabecalho => VoltaParaALista ? "Atividades" : _tituloDaAtividade ?? "Atividade";
    private string? RotuloDoVoltarNoMobile => VoltaParaALista ? null : "Voltar para a atividade";

    // O subtítulo só acompanha o formulário: sobre "Acesso negado." ou "Atividade não encontrada." ele sobra.
    private string? SubtituloDoFormulario => _estado == EstadoDeCarga.Pronto
        ? "Diga o que precisa ser feito e a que processo ou cliente se refere."
        : null;

    protected override Task OnInitializedAsync() => CarregarAsync();

    // A carga inteira, também a do "Tentar de novo": o perfil antes de qualquer consulta.
    private async Task CarregarAsync()
    {
        _estado = EstadoDeCarga.Carregando;
        _erroDaCarga = null;
        _mensagem = null;
        _form = new Formulario();
        _contexto = null;
        _versao = null;
        _tituloDaAtividade = null;
        _clienteId = Guid.Empty;
        _clienteNome = null;
        _areaId = Guid.Empty;
        _processo = null;
        LimparBuscaDeProcesso();
        LimparBuscaDeCliente();

        if (await Despachante.EnviarAsync(new ObterMeuAcesso()) is not { EhDiretoria: true })
        {
            _estado = EstadoDeCarga.AcessoNegado;
            return;
        }

        Guid? areaAtual = null;
        if (Id is { } id)
        {
            var atividade = await Despachante.EnviarAsync(new ObterAtividade(id));
            if (atividade.Valor is not { } a)
            {
                Falhar(atividade, "Não foi possível consultar a atividade.", daAtividade: true);
                return;
            }

            _form = new Formulario { Titulo = a.Titulo, Descricao = a.Descricao, DataEntrega = a.DataEntrega, Complexidade = a.Complexidade };
            _versao = a.Versao;
            _tituloDaAtividade = a.Titulo;
            _clienteId = a.ClienteId ?? Guid.Empty;
            _clienteNome = a.ClienteNome;
            _areaId = a.AreaAtuacaoId ?? Guid.Empty;
            areaAtual = a.AreaAtuacaoId;

            if (a.ProcessoId is { } processoId)
            {
                var processo = await Despachante.EnviarAsync(new ObterProcesso(processoId));
                if (processo.Valor is null)
                {
                    Falhar(processo, "Não foi possível consultar o processo da atividade.");
                    return;
                }

                _processo = processo.Valor;
            }
        }

        // Áreas ativas, mais a área atual da atividade quando ela está inativa.
        var areas = await Despachante.EnviarAsync(new ListarAreasAtuacao());
        if (!areas.Sucesso || areas.Valor is null)
        {
            Falhar(areas, "Não foi possível consultar as áreas de atuação.");
            return;
        }

        _areas = [.. areas.Valor.Where(area => area.Ativo || area.Id == areaAtual)];
        _contexto = new EditContext(_form);
        _estado = EstadoDeCarga.Pronto;
    }

    // Acesso negado encerra a tela; "não encontrada" só quando é a própria atividade; o resto, a mensagem com "Tentar de novo".
    private void Falhar(Resultado resultado, string padrao, bool daAtividade = false)
    {
        if (resultado.Erro == TipoErro.AcessoNegado)
            _estado = EstadoDeCarga.AcessoNegado;
        else if (daAtividade && resultado.Erro == TipoErro.NaoEncontrado)
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

    private string Marcada(int nivel) => _form.Complexidade == nivel ? "true" : "false";

    private IEnumerable<string> ErrosDaComplexidade =>
        _contexto is null ? [] : _contexto.GetValidationMessages(FieldIdentifier.Create(() => _form.Complexidade));

    private string ClasseDaComplexidade => ErrosDaComplexidade.Any() ? "aj-rotulo aj-rotulo--erro" : "aj-rotulo";

    private void EscolherComplexidade(int nivel)
    {
        _form.Complexidade = nivel;
        // Revalida o campo: "Escolha a complexidade." sai assim que um nível é escolhido.
        _contexto?.NotifyFieldChanged(FieldIdentifier.Create(() => _form.Complexidade));
    }

    /// <summary>O cliente guardado na atividade deixou de ser um dos clientes do processo.</summary>
    private bool ClienteForaDoProcesso(ProcessoDetalhe processo) =>
        _clienteId != Guid.Empty && processo.Clientes.All(c => c.ClienteId != _clienteId);

    private static string MetaDoProcesso(ProcessoResumo processo) =>
        processo.Clientes.Count == 0 ? processo.AreaNome : $"{processo.AreaNome} · {string.Join(", ", processo.Clientes)}";

    private async Task ProcurarProcessosAsync()
    {
        if (_salvando) return;

        _mensagem = null;
        var resultado = await Despachante.EnviarAsync(new ListarProcessos(_buscaDeProcesso));
        if (Encerrou(resultado)) return;

        var itens = resultado.Valor?.Itens ?? [];
        _processosEncontrados = itens;
        // Os avisos valem só para uma busca que deu certo; uma falha mostra o erro no lugar deles.
        _nenhumProcesso = resultado.Sucesso && itens.Count == 0;
        _processosTruncados = resultado.Sucesso && resultado.Valor!.Total > itens.Count;
        if (!resultado.Sucesso) _mensagem = resultado.Mensagem;
    }

    private async Task EscolherProcessoAsync(Guid id)
    {
        if (_salvando) return;

        _mensagem = null;
        var resultado = await Despachante.EnviarAsync(new ObterProcesso(id));
        if (Encerrou(resultado)) return;
        if (resultado.Valor is not { } processo)
        {
            _mensagem = resultado.Mensagem;
            return;
        }

        _processo = processo;
        LimparBuscaDeProcesso();

        // O cliente passa a ser um dos clientes do processo: mantém o atual se ele é um deles; senão, o único que houver.
        var clientes = processo.Clientes;
        if (clientes.All(c => c.ClienteId != _clienteId))
            _clienteId = clientes.Count == 1 ? clientes[0].ClienteId : Guid.Empty;
    }

    private void RemoverProcesso()
    {
        // O cliente escolhido entre os do processo continua valendo, agora como cliente avulso.
        _clienteNome = _processo?.Clientes.FirstOrDefault(c => c.ClienteId == _clienteId)?.Nome ?? _clienteNome;
        _processo = null;
        _areaId = Guid.Empty;
    }

    private void RemoverCliente()
    {
        _clienteId = Guid.Empty;
        _clienteNome = null;
    }

    private async Task ProcurarClientesAsync()
    {
        if (_salvando) return;

        _mensagem = null;
        var resultado = await Despachante.EnviarAsync(new ListarClientes(_buscaDeCliente));
        if (Encerrou(resultado)) return;

        var itens = resultado.Valor?.Itens ?? [];
        _clientesEncontrados = itens;
        _nenhumCliente = resultado.Sucesso && itens.Count == 0;
        _clientesTruncados = resultado.Sucesso && resultado.Valor!.Total > itens.Count;
        if (!resultado.Sucesso) _mensagem = resultado.Mensagem;
    }

    private void EscolherCliente(ClienteResumo cliente)
    {
        _clienteId = cliente.Id;
        _clienteNome = cliente.Nome;
        LimparBuscaDeCliente();
    }

    private void LimparBuscaDeProcesso()
    {
        _processosEncontrados = [];
        _nenhumProcesso = false;
        _processosTruncados = false;
        _buscaDeProcesso = null;
    }

    private void LimparBuscaDeCliente()
    {
        _clientesEncontrados = [];
        _nenhumCliente = false;
        _clientesTruncados = false;
        _buscaDeCliente = null;
    }

    private async Task SalvarAsync()
    {
        if (_salvando) return;

        _salvando = true;
        var navegou = false;
        try
        {
            _mensagem = null;
            var resultado = await Despachante.EnviarAsync(new SalvarAtividade(
                Id, _form.Titulo, _form.Descricao, _form.DataEntrega,
                _clienteId == Guid.Empty ? null : _clienteId,
                _processo?.Id,
                // Com processo, a área é a do processo (o caso de uso a ignora); sem processo, a escolhida.
                _processo is not null || _areaId == Guid.Empty ? null : _areaId,
                _form.Complexidade,
                // Na edição, a versão com que a tela carregou a atividade; atividade nova não tem.
                Id is null ? null : _versao));

            if (Encerrou(resultado)) return;

            if (!resultado.Sucesso)
            {
                // O formulário permanece com o que foi digitado; conflito e "não encontrada" trazem a mensagem do servidor.
                _mensagem = resultado.Mensagem;
                return;
            }

            Recados.Deixar("Atividade salva.");
            navegou = true;
            Navegacao.NavigateTo(Voltar);
        }
        finally
        {
            // Depois do sucesso a página vai embora: o botão fica travado para um clique tardio não salvar de novo.
            if (!navegou) _salvando = false;
        }
    }

    private sealed class Formulario
    {
        [Required(ErrorMessage = "Informe o título.")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a descrição.")]
        public string Descricao { get; set; } = string.Empty;

        public DateOnly? DataEntrega { get; set; }

        // 0 é "ainda não escolhida" (atividade nova).
        [Range(Atividade.ComplexidadeMinima, Atividade.ComplexidadeMaxima, ErrorMessage = "Escolha a complexidade.")]
        public int Complexidade { get; set; }
    }
}
```

Notas para quem implementa:

- `[Required]` recusa texto vazio e só de espaços; o servidor continua validando (tamanho, data obrigatória fora de Criada) e a mensagem dele vai ao `Aviso` do topo.
- A classe do grupo da complexidade e a do rótulo do cliente do processo são expressões (ponto cego da trava): só `aj-rotulo` e `aj-rotulo--erro`.
- O componente antigo `Components/Atividades/FormularioDeAtividade.razor` continua no código (a ficha antiga o usa até a Tarefa 10; sai na Tarefa 14). Não o altere.
- A lista (Tarefa 6) já leva a `/atividades/nova`; o "Editar" da ficha (Tarefa 11) levará a `/atividades/{id}/editar`.

- [ ] **Step 3: Trava e testes**

Acrescente a `Migradas` (`tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`), logo depois de `"Components/Pages/Atividades.razor",`: `"Components/Pages/AtividadeFormulario.razor",`.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadeFormulario|FullyQualifiedName~AtividadesWebTests|FullyQualifiedName~AtividadesInterativosTests|FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~AutenticacaoWebTests"`
Expected: 0 avisos; todos passando.

- [ ] **Step 4: Commit**

Antes, `git branch --show-current` (deve ser `worktree-feat+fase-9e-atividades`).

```
git add src tests
git commit -m "feat(web): formulário de atividade em tela própria, com a complexidade em controle segmentado, erros por campo e as buscas de vínculo pelo Enter"
```

---

### Task 8: Comentários da atividade no design system

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Atividades/ComentariosDaAtividade.razor`
- Create: `tests/AtividadesJuridicas.IntegrationTests/ComentariosDaAtividadeInterativosTests.cs`
- Modify: `TelasNoDesignSystemTests.cs` (`Migradas`)

**Interfaces:**
- Consumes: `AvatarDePessoa`, `TextosDeAtividade.DataCurta`, `FusoDoEscritorio.Formatar`, `Aviso`, `EstadoDaLista`.
- Produces: `<ComentariosDaAtividade AtividadeId="…" AoContar="…" OnAcessoEncerrado="…" OnAtividadeNaoEncontrada="…" />` — `AtividadeId` (`Guid`), `AoContar` (`EventCallback<int>`) avisa quantos comentários foram carregados (para o contador da aba), a cada carga bem-sucedida; `OnAcessoEncerrado` e `OnAtividadeNaoEncontrada` (`EventCallback`), como hoje.

**Regras:**
- Quadros: os painéis "Comentários" de `FichaDaAtividade.dc.html` e `FichaTablet.dc.html` (`quadros-ficha.md`, 1.7.4 e 2.8; o mobile segue o desktop, 3.7).
- O componente emite o próprio `section.aj-painel` com `h2` "Comentários" (o painel que a ficha põe na coluna e na aba). Lista `aj-comentarios` com `aj-comentario` (avatar `aj-avatar` com a sigla e a classe de cor, balão com o nome, a data e o texto); data completa no desktop e no mobile, curta no tablet (dois `span` em invólucros de visão).
- Novo comentário: no desktop e no mobile (`aj-desktop-mobile`), `aj-comentarios__novo` com `label.aj-rotulo` "Novo comentário", `textarea.aj-campo rows="3"` com o exemplo `Escreva um comentário para a equipe` e "Comentar" (`aj-botao--secundario`); no tablet (`aj-so-tablet`), `aj-campo-com-botao` com o rótulo só para leitor, `textarea rows="2"` com `Escreva um comentário` e o botão de ícone `Enviar comentário` (ícone `enviar`). Os dois formulários usam o mesmo texto (o que se digita num vale no outro).
- Comportamento de hoje, sem mudar: estados (carregando, acesso encerrado, não encontrada avisando a ficha, erro com "Tentar de novo", vazio), envio com `ComentarAtividade`, a recarga, as mensagens, a limpeza do campo só se ainda tem o enviado, a guarda `_enviando`.
- Testes bUnit: lista com nome, data e texto; autor nulo; vazio; erro e "Tentar de novo"; comentar envia e recarrega; o texto digitado num formulário aparece no outro; `AoContar` com o número; clique duplo envia uma vez.

- [ ] **Step 1: Testes (devem falhar)**

Antes de escrever, releia o painel "Comentários" de `FichaDaAtividade.dc.html` e de `FichaTablet.dc.html` na cópia local dos quadros e as seções 1.7.4, 2.8, 3.7 e 4.8 de `quadros-ficha.md`.

Crie `tests/AtividadesJuridicas.IntegrationTests/ComentariosDaAtividadeInterativosTests.cs`:

```csharp
using AngleSharp.Dom;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Web.Components.Atividades;
using Bunit;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Os comentários da ficha da atividade, clicando: os balões com avatar, nome, data e texto, os estados da lista, e o
/// comentário novo nas duas formas (desktop e mobile; tablet), ligadas ao mesmo texto.
/// </summary>
public class ComentariosDaAtividadeInterativosTests : TelaInterativa
{
    private static readonly Guid Atividade = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000f1");

    // 19:45 e 11:30 em UTC são 16:45 e 08:30 no horário do escritório.
    private static readonly ComentarioDetalhe DaBeatriz = new(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c1"),
        Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000e1"), "Beatriz Lins",
        "Minuta anexada.\nDeixei em aberto a cláusula de multa por atraso.", new DateTime(2026, 10, 7, 19, 45, 0, DateTimeKind.Utc));
    private static readonly ComentarioDetalhe DaMarina = new(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c2"),
        Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000e2"), "Marina Couto",
        "Otávio, confira os valores da cláusula terceira antes da revisão.", new DateTime(2026, 10, 8, 11, 30, 0, DateTimeKind.Utc));

    // O que o servidor tem: a lista lê daqui, e comentar acrescenta aqui.
    private readonly List<ComentarioDetalhe> _noServidor = [DaBeatriz, DaMarina];

    public ComentariosDaAtividadeInterativosTests()
    {
        Despachante
            .Responde<ListarComentariosDaAtividade, Resultado<IReadOnlyList<ComentarioDetalhe>>>(
                _ => Resultado<IReadOnlyList<ComentarioDetalhe>>.Ok([.. _noServidor]))
            .Responde<ComentarAtividade, Resultado<Guid>>(pedido =>
            {
                _noServidor.Add(new ComentarioDetalhe(Guid.NewGuid(), Guid.NewGuid(), "Diana Diretora", pedido.Texto,
                    new DateTime(2026, 10, 9, 13, 0, 0, DateTimeKind.Utc)));
                return Resultado<Guid>.Ok(Guid.NewGuid());
            });
    }

    private IRenderedComponent<ComentariosDaAtividade> Painel(
        Action<int>? aoContar = null, Action? naoEncontrada = null, Action? acessoEncerrado = null) =>
        Desenhar<ComentariosDaAtividade>(p => p
            .Add(c => c.AtividadeId, Atividade)
            .Add(c => c.AoContar, (int quantos) => aoContar?.Invoke(quantos))
            .Add(c => c.OnAtividadeNaoEncontrada, () => naoEncontrada?.Invoke())
            .Add(c => c.OnAcessoEncerrado, () => acessoEncerrado?.Invoke()));

    // O valor que o Blazor liga à caixa de texto: o bUnit o escreve como atributo "value" ou como conteúdo.
    private static string Valor(IElement caixa) => caixa.GetAttribute("value") ?? caixa.TextContent;

    private static IElement CaixaDoDesktop(IRenderedComponent<ComentariosDaAtividade> painel) =>
        painel.Find("div.aj-desktop-mobile textarea");

    private static IElement CaixaDoTablet(IRenderedComponent<ComentariosDaAtividade> painel) =>
        painel.Find("div.aj-so-tablet textarea");

    private static IElement Comentar(IRenderedComponent<ComentariosDaAtividade> painel) =>
        painel.Find("div.aj-desktop-mobile button");

    private static IElement EnviarComentario(IRenderedComponent<ComentariosDaAtividade> painel) =>
        painel.Find("div.aj-so-tablet button");

    [Fact]
    public void Lista_mostra_avatar_nome_data_completa_e_curta_e_texto()
    {
        var painel = Painel();

        painel.Find("section").ClassName.Should().Be("aj-painel aj-painel--ficha aj-painel--comentarios");
        painel.Find("section > h2").TextContent.Should().Be("Comentários");
        var itens = painel.FindAll("ul.aj-comentarios > li.aj-comentario");
        itens.Should().HaveCount(2);

        var avatar = itens[0].QuerySelector("span.aj-avatar")!;
        avatar.TextContent.Should().Be("BL");
        avatar.ClassList.Should().Contain(AvatarDePessoa.Classe("Beatriz Lins"));
        avatar.GetAttribute("aria-hidden").Should().Be("true");
        itens[0].QuerySelector(".aj-comentario__balao .aj-comentario__cabeca strong")!.TextContent.Should().Be("Beatriz Lins");
        itens[0].QuerySelector("span.aj-desktop-mobile span.aj-comentario__data")!.TextContent.Should().Be("07/10/2026 16:45");
        itens[0].QuerySelector("span.aj-so-tablet span.aj-comentario__data")!.TextContent.Should().Be("07/10 16:45");
        itens[0].QuerySelector(".aj-comentario__balao .aj-comentario__texto")!.TextContent
            .Should().Be("Minuta anexada.\nDeixei em aberto a cláusula de multa por atraso.");

        itens[1].QuerySelector("span.aj-avatar")!.TextContent.Should().Be("MC");
        itens[1].QuerySelector("span.aj-desktop-mobile span.aj-comentario__data")!.TextContent.Should().Be("08/10/2026 08:30");
        itens[1].QuerySelector("span.aj-so-tablet span.aj-comentario__data")!.TextContent.Should().Be("08/10 08:30");
    }

    [Fact]
    public void Autor_nulo_aparece_como_autor_nao_identificado()
    {
        _noServidor.Clear();
        _noServidor.Add(DaBeatriz with { UsuarioNome = null });

        var painel = Painel();

        var item = painel.Find("li.aj-comentario");
        item.QuerySelector(".aj-comentario__cabeca strong")!.TextContent.Should().Be("Autor não identificado");
        item.QuerySelector("span.aj-avatar")!.TextContent.Should().Be("?");
    }

    [Fact]
    public void Sem_comentarios_mostra_o_vazio_e_as_duas_formas_do_comentario_novo()
    {
        _noServidor.Clear();

        var painel = Painel();

        painel.Find(".aj-estado-lista").TextContent.Should().Be("Nenhum comentário.");
        painel.FindAll("ul.aj-comentarios").Should().BeEmpty();

        var desktop = painel.Find("div.aj-desktop-mobile div.aj-comentarios__novo");
        desktop.QuerySelector("label.aj-rotulo")!.TextContent.Should().StartWith("Novo comentário");
        var caixa = desktop.QuerySelector("textarea.aj-campo")!;
        caixa.GetAttribute("rows").Should().Be("3");
        caixa.GetAttribute("placeholder").Should().Be("Escreva um comentário para a equipe");
        caixa.GetAttribute("maxlength").Should().Be("4000");
        desktop.QuerySelector("button.aj-botao.aj-botao--secundario")!.TextContent.Should().Be("Comentar");

        var tablet = painel.Find("div.aj-so-tablet div.aj-campo-com-botao");
        tablet.QuerySelector("label.aj-rotulo span.aj-so-leitor")!.TextContent.Should().Be("Novo comentário");
        var caixaDoTablet = tablet.QuerySelector("textarea.aj-campo.aj-campo--folgado")!;
        caixaDoTablet.GetAttribute("rows").Should().Be("2");
        caixaDoTablet.GetAttribute("placeholder").Should().Be("Escreva um comentário");
        var enviar = tablet.QuerySelector("button.aj-botao.aj-botao--primario.aj-botao--icone.aj-botao--enviar")!;
        enviar.GetAttribute("aria-label").Should().Be("Enviar comentário");
        enviar.QuerySelector("svg").Should().NotBeNull();
    }

    [Fact]
    public void Falha_da_consulta_mostra_o_erro_sem_fingir_lista_vazia_e_tentar_de_novo_le_outra_vez()
    {
        var chamadas = 0;
        Despachante.Responde<ListarComentariosDaAtividade, Resultado<IReadOnlyList<ComentarioDetalhe>>>(_ => ++chamadas == 1
            ? Resultado<IReadOnlyList<ComentarioDetalhe>>.Falha(TipoErro.Validacao, "Não foi possível consultar os comentários.")
            : Resultado<IReadOnlyList<ComentarioDetalhe>>.Ok([DaBeatriz]));
        var contagens = new List<int>();

        var painel = Painel(aoContar: contagens.Add);

        painel.Find("[role=alert]").TextContent.Should().Contain("Não foi possível consultar os comentários.");
        painel.Markup.Should().NotContain("Nenhum comentário.");
        contagens.Should().BeEmpty();   // falha não conta

        painel.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        painel.WaitForAssertion(() => painel.FindAll("li.aj-comentario").Should().ContainSingle());
        painel.FindAll("[role=alert]").Should().BeEmpty();
        chamadas.Should().Be(2);
        contagens.Should().Equal(1);
    }

    [Fact]
    public void Comentar_envia_o_texto_recarrega_a_lista_limpa_as_caixas_e_avisa()
    {
        var contagens = new List<int>();
        var painel = Painel(aoContar: contagens.Add);

        CaixaDoDesktop(painel).Input("Conferi os valores da cláusula terceira.");
        Comentar(painel).Click();

        painel.WaitForAssertion(() => painel.Markup.Should().Contain("Comentário enviado."));
        Despachante.Recebidas.OfType<ComentarAtividade>().Should().ContainSingle()
            .Which.Should().Be(new ComentarAtividade(Atividade, "Conferi os valores da cláusula terceira."));
        Despachante.Recebidas.OfType<ListarComentariosDaAtividade>().Should().HaveCount(2);
        var itens = painel.FindAll("li.aj-comentario");
        itens.Should().HaveCount(3);
        itens[2].QuerySelector(".aj-comentario__texto")!.TextContent.Should().Be("Conferi os valores da cláusula terceira.");
        Valor(CaixaDoDesktop(painel)).Should().BeEmpty();
        Valor(CaixaDoTablet(painel)).Should().BeEmpty();
        contagens.Should().Equal(2, 3);
    }

    [Fact]
    public void O_texto_digitado_numa_forma_vale_na_outra()
    {
        var painel = Painel();

        CaixaDoTablet(painel).Input("Revisar a cláusula terceira.");

        Valor(CaixaDoDesktop(painel)).Should().Be("Revisar a cláusula terceira.");
        Comentar(painel).Click();
        painel.WaitForAssertion(() => Despachante.Recebidas.OfType<ComentarAtividade>().Should().ContainSingle()
            .Which.Texto.Should().Be("Revisar a cláusula terceira."));

        CaixaDoDesktop(painel).Input("Anexei o cálculo.");

        Valor(CaixaDoTablet(painel)).Should().Be("Anexei o cálculo.");
        EnviarComentario(painel).Click();
        painel.WaitForAssertion(() => Despachante.Recebidas.OfType<ComentarAtividade>().Select(c => c.Texto).Should()
            .Equal("Revisar a cláusula terceira.", "Anexei o cálculo."));
    }

    [Fact]
    public void Com_o_envio_em_curso_as_duas_formas_esperam_e_sai_um_comentario_so()
    {
        var envio = new TaskCompletionSource<Resultado<Guid>>();
        Despachante.RespondeDepois<ComentarAtividade, Resultado<Guid>>(envio.Task);
        var painel = Painel();
        CaixaDoDesktop(painel).Input("Conferi.");

        Comentar(painel).Click();

        Comentar(painel).TextContent.Should().Be("Enviando…");
        Comentar(painel).HasAttribute("disabled").Should().BeTrue();
        EnviarComentario(painel).HasAttribute("disabled").Should().BeTrue();
        painel.FindAll("textarea").Should().HaveCount(2).And.OnlyContain(t => t.HasAttribute("disabled"));

        envio.SetResult(Resultado<Guid>.Ok(Guid.NewGuid()));

        painel.WaitForAssertion(() => painel.Markup.Should().Contain("Comentário enviado."));
        Despachante.Recebidas.OfType<ComentarAtividade>().Should().ContainSingle();
        Comentar(painel).TextContent.Should().Be("Comentar");
        painel.FindAll("textarea").Should().OnlyContain(t => !t.HasAttribute("disabled"));
    }

    [Fact]
    public void Atividade_que_sumiu_avisa_a_ficha_e_troca_o_painel_pela_mensagem()
    {
        Despachante.Responde<ListarComentariosDaAtividade, Resultado<IReadOnlyList<ComentarioDetalhe>>>(
            Resultado<IReadOnlyList<ComentarioDetalhe>>.Falha(TipoErro.NaoEncontrado, "Atividade não encontrada."));
        var avisada = false;

        var painel = Painel(naoEncontrada: () => avisada = true);

        avisada.Should().BeTrue();
        painel.Find("section > h2").TextContent.Should().Be("Comentários");
        painel.Find(".aj-estado-lista").TextContent.Should().Be("Atividade não encontrada.");
        painel.FindAll("textarea").Should().BeEmpty();
        painel.FindAll("li.aj-comentario").Should().BeEmpty();
    }

    [Fact]
    public void Acesso_negado_avisa_a_pagina_e_nao_finge_lista_vazia()
    {
        Despachante.Responde<ListarComentariosDaAtividade, Resultado<IReadOnlyList<ComentarioDetalhe>>>(
            Resultado<IReadOnlyList<ComentarioDetalhe>>.Falha(TipoErro.AcessoNegado, "Acesso negado."));
        var encerrado = false;

        var painel = Painel(acessoEncerrado: () => encerrado = true);

        encerrado.Should().BeTrue();
        painel.Find("[role=alert]").TextContent.Should().Contain("Você não tem mais acesso a esta atividade.");
        painel.Markup.Should().NotContain("Nenhum comentário.");
        painel.FindAll("textarea").Should().BeEmpty();
    }
}
```

(As `Action` dos parâmetros `naoEncontrada` e `acessoEncerrado` são lambdas de atribuição: se o compilador as achar ambíguas, escreva-as com chaves, `() => { avisada = true; }`.)

Run: `dotnet build -m:1`
Expected: não compila — `ComentariosDaAtividade` não tem `AoContar` (CS1061 no `.Add(c => c.AoContar, …)`). É o RED.

- [ ] **Step 2: O componente**

Reescreva `src/AtividadesJuridicas.Web/Components/Atividades/ComentariosDaAtividade.razor` inteiro. As classes `aj-painel--ficha`, `aj-painel--comentarios`, `aj-comentarios`, `aj-comentario`, `aj-comentario__balao`, `aj-comentario__cabeca`, `aj-comentario__data`, `aj-comentario__texto`, `aj-comentarios__novo`, `aj-campo--folgado`, `aj-botao--enviar` e a de visibilidade `aj-desktop-mobile` vêm da Tarefa 1; `AvatarDePessoa`, `TextosDeAtividade.DataCurta` e o ícone `enviar`, da Tarefa 4.

```razor
@inject IDespachante Despachante
@inject FusoDoEscritorio Fuso

@* O painel "Comentários" da ficha da atividade: a ficha o põe na coluna do desktop e na aba "Comentários" do tablet e
   do mobile, uma vez só. O que muda por visão vai em invólucros dentro do painel. *@
<section class="aj-painel aj-painel--ficha aj-painel--comentarios">
    <h2>Comentários</h2>

    @if (_mensagem is not null)
    {
        <Aviso Tipo="@(_falhou ? TipoDeAviso.Erro : TipoDeAviso.Sucesso)">@_mensagem</Aviso>
    }

    @if (_acessoEncerrado)
    {
        @* A página foi avisada; se ela não retirar o painel, ele não finge uma lista vazia. *@
        <Aviso Tipo="TipoDeAviso.Erro">Você não tem mais acesso a esta atividade.</Aviso>
    }
    else if (_naoEncontrada)
    {
        @* Perdeu a visibilidade da atividade: a mensagem do caso de uso ocupa o lugar da lista. *@
        <div class="aj-estado-lista"><strong>@_mensagemNaoEncontrada</strong></div>
    }
    else
    {
        @* Falha na consulta nunca é apresentada como "nenhum comentário": o EstadoDaLista mostra o erro primeiro. *@
        <EstadoDaLista Erro="@_erroDaLista" Carregando="@(!_carregado)" Vazio="@(_comentarios.Count == 0)"
                       TextoDeVazio="Nenhum comentário." TentarDeNovo="TentarDeNovoAsync">
            <ul class="aj-comentarios">
                @foreach (var comentario in _comentarios)
                {
                    <li class="aj-comentario" @key="comentario.Id">
                        <span class="@ClasseDoAvatar(comentario)" aria-hidden="true">@AvatarDePessoa.Sigla(comentario.UsuarioNome ?? string.Empty)</span>
                        <div class="aj-comentario__balao">
                            @* A data muda de texto por visão: completa no desktop e no mobile, curta no tablet. *@
                            <div class="aj-comentario__cabeca"><strong>@(comentario.UsuarioNome ?? "Autor não identificado")</strong><span class="aj-desktop-mobile"><span class="aj-comentario__data">@Fuso.Formatar(comentario.DataHora)</span></span><span class="aj-so-tablet"><span class="aj-comentario__data">@TextosDeAtividade.DataCurta(Fuso, comentario.DataHora)</span></span></div>
                            <div class="aj-comentario__texto">@comentario.Texto</div>
                        </div>
                    </li>
                }
            </ul>
        </EstadoDaLista>

        @if (_carregado)
        {
            @* Duas formas do comentário novo, ligadas ao mesmo texto: o que se digita numa vale na outra. *@
            <div class="aj-desktop-mobile">
                <div class="aj-comentarios__novo">
                    <label class="aj-rotulo">Novo comentário<textarea class="aj-campo" rows="3" placeholder="Escreva um comentário para a equipe" maxlength="@AtividadeComentario.TamanhoMaximoTexto" @bind="_texto" @bind:event="oninput" disabled="@_enviando"></textarea></label>
                    <button type="button" class="aj-botao aj-botao--secundario" disabled="@_enviando" @onclick="ComentarAsync">@(_enviando ? "Enviando…" : "Comentar")</button>
                </div>
            </div>
            <div class="aj-so-tablet">
                <div class="aj-campo-com-botao">
                    <label class="aj-rotulo"><span class="aj-so-leitor">Novo comentário</span><textarea class="aj-campo aj-campo--folgado" rows="2" placeholder="Escreva um comentário" maxlength="@AtividadeComentario.TamanhoMaximoTexto" @bind="_texto" @bind:event="oninput" disabled="@_enviando"></textarea></label>
                    <button type="button" class="aj-botao aj-botao--primario aj-botao--icone aj-botao--enviar" aria-label="Enviar comentário" disabled="@_enviando" @onclick="ComentarAsync"><Icone Nome="enviar" /></button>
                </div>
            </div>
        }
    }
</section>
```

O `@code` é o de hoje, com três mudanças: o parâmetro `AoContar`, a chamada dele em `CarregarAsync` e a classe do avatar. Tudo o mais (estados, `EncerradoAsync`, `TentarDeNovoAsync`, `ComentarAsync` com a limpeza só se o campo ainda tem o enviado, a guarda `_enviando`, `Avisar`) fica como está:

```csharp
@code {
    [Parameter, EditorRequired] public Guid AtividadeId { get; set; }

    /// <summary>Quantos comentários a lista trouxe, a cada carga bem-sucedida (o contador da aba "Comentários").</summary>
    [Parameter] public EventCallback<int> AoContar { get; set; }

    /// <summary>Avisado quando um caso de uso responde acesso negado: a página decide o que mostrar.</summary>
    [Parameter] public EventCallback OnAcessoEncerrado { get; set; }

    /// <summary>Avisado quando a atividade não existe (ou não é mais visível): a página recarrega o registro.</summary>
    [Parameter] public EventCallback OnAtividadeNaoEncontrada { get; set; }

    private bool _carregado;
    private bool _naoEncontrada;
    private bool _acessoEncerrado;
    private bool _falhou;
    private bool _enviando;
    private string? _mensagem;
    private string? _mensagemNaoEncontrada;
    private string? _erroDaLista;
    private string _texto = string.Empty;
    private IReadOnlyList<ComentarioDetalhe> _comentarios = [];

    protected override async Task OnInitializedAsync()
    {
        await CarregarAsync();
        _carregado = true;
    }

    /// <summary>Consulta a lista; devolve se o painel ficou mostrando dados atuais.</summary>
    private async Task<bool> CarregarAsync()
    {
        var resultado = await Despachante.EnviarAsync(new ListarComentariosDaAtividade(AtividadeId));
        if (await EncerradoAsync(resultado)) return false;

        if (resultado.Valor is { } lista)
        {
            _comentarios = lista;
            _erroDaLista = null;
            await AoContar.InvokeAsync(lista.Count);
            return true;
        }

        // Falha na consulta não é "nenhum comentário": o erro toma o lugar da lista.
        _comentarios = [];
        _erroDaLista = resultado.Mensagem ?? "Não foi possível consultar os comentários.";
        return false;
    }

    // A cor e a sigla saem do nome (o comentário de autor desconhecido fica com "?").
    private static string ClasseDoAvatar(ComentarioDetalhe comentario) =>
        $"aj-avatar {AvatarDePessoa.Classe(comentario.UsuarioNome ?? string.Empty)}";

    // EncerradoAsync, TentarDeNovoAsync, ComentarAsync e Avisar: sem mudança (copie do arquivo de hoje).
}
```

A `textarea` não tem `@key` nem `@ref`: as duas caixas são ligadas ao mesmo `_texto` por `@bind` com `oninput`, e cada tecla redesenha a outra. Nenhum `style`, nenhuma classe fora de `aj-` (a classe do avatar é montada por expressão e vale a revisão: `aj-avatar` mais `aj-avatar--azul|--verde|--ambar|--lilas`).

- [ ] **Step 3: Trava e testes**

Acrescente a `Migradas` em `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`, antes de `"Components/Atividades/EtiquetaDeSituacao.razor",` (o primeiro item, que a Tarefa 4 pôs): `"Components/Atividades/ComentariosDaAtividade.razor",`.

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros (a ficha antiga, `FichaDaAtividade.razor`, continua compilando: ela não passa `AoContar`, que é opcional).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ComentariosDaAtividadeInterativosTests"`
Expected: 9 passando.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Expected: todos passando (com o arquivo novo na trava).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~ComentariosTests|FullyQualifiedName~ProdutoresWebTests"`
Expected: todos passando (as regras de comentário e a ficha antiga embutida em Produtores).

- [ ] **Step 4: Commit**

Confira o branch (`git branch --show-current` deve dizer `worktree-feat+fase-9e-atividades`).

```
git add src tests
git commit -m "feat(web): comentários da atividade no design system, em balões, com a data por visão e o comentário novo nas duas formas"
```

---

### Task 9: Documentos da atividade no design system

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Atividades/DocumentosDaAtividade.razor` (passa a herdar `GestaoDeDocumentos`)
- Create: `tests/AtividadesJuridicas.IntegrationTests/DocumentosDaAtividadeInterativosTests.cs`
- Modify: `TelasNoDesignSystemTests.cs` (`Migradas`)

**Interfaces:**
- Consumes: os ganchos `ListarPaginaAsync` e `EnviarArquivoAsync` e a recarga do documento que sumiu (Tarefa 5), `TextosDeDocumento`, `MenuDeAcoes`, `Aviso`, `Paginacao`, `Icone` (`texto`, `planilha`, `adicionar`).
- Produces: `<DocumentosDaAtividade AtividadeId="…" PodeGerenciar="…" AoContar="…" OnAcessoEncerrado="…" OnAtividadeNaoEncontrada="…" />` — `AtividadeId` (`Guid`), `PodeGerenciar` (`bool`), `AoContar` (`EventCallback<int>`, o `Total` da lista a cada carga bem-sucedida), `OnAcessoEncerrado` (`EventCallback`, o da base) e `OnAtividadeNaoEncontrada` (`EventCallback`).

**Regras:**
- Quadros: os painéis "Documentos" de `FichaDaAtividade.dc.html` e `FichaTablet.dc.html` (`quadros-ficha.md`, 1.7.5 e 2.9; mobile 3.7).
- O componente emite o próprio `section.aj-painel` com o `aj-painel__cabecalho` (`h2` "Documentos" e o botão secundário `Enviar documento` com o ícone `adicionar`, para quem vê a atividade, como hoje). Lista `aj-documentos`: no desktop, `aj-documento` com o ícone por tipo (`aj-documento__icone--planilha` para `.xlsx`, `.xls`, `.csv`, `.ods`; `--texto` para o resto), o nome como link de download (`href` de `TextosDeDocumento.Endereco`, `download`, `data-enhance-nav="false"`), a meta `{tamanho} · {autor} · {data}` e, para `PodeGerenciar`, `Renomear` e `Excluir`; no tablet e no mobile, a mesma linha com a meta `{tamanho} · {autor}` e, para `PodeGerenciar`, o "⋯" (`MenuDeAcoes`, `Ações de {nome}`, `Renomear` e `Excluir` por último em perigo).
- Painéis de envio e de renomear logo abaixo do painel "Documentos"; avisos e a confirmação de exclusão (`Confirmar a exclusão de {nome}?`, `O documento é excluído da atividade.`) logo acima dele. Uma coisa aberta por vez e o foco, pela base. Paginação só com mais de uma página.
- Comportamento de hoje, sem mudar: quem envia (todos que veem a atividade) e quem renomeia ou exclui (`PodeGerenciar`), as mensagens, `NaoEncontrado` de documento recarregando a lista (pela base, Tarefa 5) e de atividade avisando a ficha (no herdeiro, dentro dos ganchos de listar e enviar). Esta tarefa não mexe na base.
- Herdeiro: `CategoriaDosDocumentos` é `DocumentoAtividade` e `ClienteDosDocumentos` é nulo (o servidor grava o cliente da atividade); os dois ganchos vão por `ListarDocumentosDaAtividade` e `EnviarDocumentoDaAtividade`. O ícone do tipo sai do arquivo enviado (`NomeArquivoOriginal`), não do nome, que pode ter sido trocado sem a extensão.
- Testes bUnit: linhas com ícone por tipo e link de download nas duas formas; `PodeGerenciar` falso sem Renomear, Excluir nem "⋯" (com afirmação positiva); enviar (arquivo, nome, mensagem, recarga); renomear; excluir com confirmação; uma coisa aberta por vez; `AoContar`.

- [ ] **Step 1: Testes (devem falhar)**

Antes de escrever, releia o painel "Documentos" de `FichaDaAtividade.dc.html` e de `FichaTablet.dc.html` na cópia local dos quadros, as seções 1.7.5, 2.9, 3.7 e 4.9 de `quadros-ficha.md`, `Components/Documentos/GestaoDeDocumentos.cs` (já com os ganchos da Tarefa 5) e `Components/Documentos/DocumentosDaFicha.razor` (o herdeiro mais parecido).

Crie `tests/AtividadesJuridicas.IntegrationTests/DocumentosDaAtividadeInterativosTests.cs`:

```csharp
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Web.Components.Atividades;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;

namespace AtividadesJuridicas.IntegrationTests;

/// <summary>
/// Os documentos da ficha da atividade, clicando: as linhas com o ícone do tipo e o link que baixa, as ações por visão
/// (botões no desktop, "⋯" no tablet e no mobile), o envio sem confirmação, renomear e excluir, uma coisa aberta por vez,
/// e o "não encontrado" do documento (recarrega) separado do da atividade (avisa a ficha).
/// </summary>
public class DocumentosDaAtividadeInterativosTests : TelaInterativa
{
    private static readonly Guid Atividade = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000f1");
    private static readonly Guid Minuta = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b1");
    private static readonly Guid Calculo = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b2");

    // 19:40 em UTC são 16:40 no horário do escritório. O cálculo foi renomeado sem a extensão: o ícone sai do arquivo
    // enviado, não do nome.
    private static readonly DocumentoResumo DaMinuta = new(Minuta, "minuta-acordo-v2.docx", "minuta-acordo-v2.docx", 84 * 1024,
        CategoriaDocumento.DocumentoAtividade, new DateTime(2026, 10, 7, 19, 40, 0, DateTimeKind.Utc), "Beatriz Lins",
        Atividade, "Minuta de acordo trabalhista");
    private static readonly DocumentoResumo DoCalculo = new(Calculo, "Cálculo das verbas", "calculo-verbas.xlsx", 31 * 1024,
        CategoriaDocumento.DocumentoAtividade, new DateTime(2026, 10, 8, 12, 10, 0, DateTimeKind.Utc), "Otávio Brandão",
        Atividade, "Minuta de acordo trabalhista");

    private int _total = 2;

    public DocumentosDaAtividadeInterativosTests() =>
        Despachante.Responde<ListarDocumentosDaAtividade, Resultado<Pagina<DocumentoResumo>>>(pedido =>
            Resultado<Pagina<DocumentoResumo>>.Ok(new Pagina<DocumentoResumo>([DaMinuta, DoCalculo], _total, pedido.Pagina, 20)));

    private IRenderedComponent<DocumentosDaAtividade> Painel(
        bool podeGerenciar = true, Action<int>? aoContar = null, Action? naoEncontrada = null) =>
        Desenhar<DocumentosDaAtividade>(p => p
            .Add(d => d.AtividadeId, Atividade)
            .Add(d => d.PodeGerenciar, podeGerenciar)
            .Add(d => d.AoContar, (int total) => aoContar?.Invoke(total))
            .Add(d => d.OnAtividadeNaoEncontrada, () => naoEncontrada?.Invoke()));

    private static void AbrirEnvio(IRenderedComponent<DocumentosDaAtividade> painel) =>
        painel.Find(".aj-painel__cabecalho button").Click();

    private static void Escolher(IRenderedComponent<DocumentosDaAtividade> painel, string nome) =>
        painel.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("conteúdo", nome));

    // Os botões da linha no desktop.
    private static void ClicarNaLinha(IRenderedComponent<DocumentosDaAtividade> painel, string texto, int linha = 0) =>
        painel.FindAll("div.aj-so-desktop li.aj-documento")[linha].QuerySelectorAll("button").Single(b => b.TextContent == texto).Click();

    // Os itens do "⋯" no tablet e no mobile.
    private static void ClicarNoMenu(IRenderedComponent<DocumentosDaAtividade> painel, string texto, int linha = 0) =>
        painel.FindAll("div.aj-tablet-mobile li.aj-documento")[linha].QuerySelectorAll(".aj-menu-acoes__item").Single(b => b.TextContent == texto).Click();

    private static IEnumerable<string> Paineis(IRenderedComponent<DocumentosDaAtividade> painel) =>
        painel.FindAll("section.aj-painel h2").Select(h => h.TextContent);

    [Fact]
    public void Linhas_trazem_o_icone_do_tipo_o_link_que_baixa_e_a_meta_de_cada_visao()
    {
        var painel = Painel();

        painel.Find("section").ClassName.Should().Be("aj-painel aj-painel--ficha aj-painel--documentos");
        painel.Find(".aj-painel__cabecalho h2").TextContent.Should().Be("Documentos");
        var enviar = painel.Find(".aj-painel__cabecalho button");
        enviar.ClassName.Should().Be("aj-botao aj-botao--secundario aj-botao--com-icone");
        enviar.TextContent.Should().Be("Enviar documento");

        var desktop = painel.FindAll("div.aj-so-desktop ul.aj-documentos > li.aj-documento");
        desktop.Should().HaveCount(2);
        desktop[0].QuerySelector("span.aj-documento__icone")!.ClassName.Should().Be("aj-documento__icone aj-documento__icone--texto");
        desktop[1].QuerySelector("span.aj-documento__icone")!.ClassName.Should().Be("aj-documento__icone aj-documento__icone--planilha");
        var link = desktop[0].QuerySelector("div.aj-documento__texto a.aj-documento__nome")!;
        link.TextContent.Should().Be("minuta-acordo-v2.docx");
        link.GetAttribute("href").Should().Be($"/documentos/{Minuta}/baixar");
        link.HasAttribute("download").Should().BeTrue();
        link.GetAttribute("data-enhance-nav").Should().Be("false");
        desktop[0].QuerySelector(".aj-documento__meta")!.TextContent.Should().Be("84 KB · Beatriz Lins · 07/10/2026 16:40");
        desktop[1].QuerySelector(".aj-documento__meta")!.TextContent.Should().Be("31 KB · Otávio Brandão · 08/10/2026 09:10");
        desktop[0].QuerySelectorAll(".aj-documento__acoes > button").Select(b => b.ClassName).Should()
            .Equal("aj-botao aj-botao--fantasma", "aj-botao aj-botao--perigo");
        desktop[0].QuerySelectorAll(".aj-documento__acoes > button").Select(b => b.TextContent).Should().Equal("Renomear", "Excluir");

        var linhas = painel.FindAll("div.aj-tablet-mobile ul.aj-documentos > li.aj-documento");
        linhas.Should().HaveCount(2);
        linhas[1].QuerySelector("span.aj-documento__icone")!.ClassName.Should().Be("aj-documento__icone aj-documento__icone--planilha");
        var linkDoTablet = linhas[1].QuerySelector("a.aj-documento__nome")!;
        linkDoTablet.TextContent.Should().Be("Cálculo das verbas");
        linkDoTablet.GetAttribute("href").Should().Be($"/documentos/{Calculo}/baixar");
        linkDoTablet.HasAttribute("download").Should().BeTrue();
        linkDoTablet.GetAttribute("data-enhance-nav").Should().Be("false");
        linhas[0].QuerySelector(".aj-documento__meta")!.TextContent.Should().Be("84 KB · Beatriz Lins");
        linhas[0].QuerySelector(".aj-menu-acoes > button")!.GetAttribute("aria-label").Should().Be("Ações de minuta-acordo-v2.docx");
        linhas[0].QuerySelectorAll(".aj-menu-acoes__item").Select(b => b.TextContent).Should().Equal("Renomear", "Excluir");
        linhas[0].QuerySelector(".aj-menu-acoes__item--perigo")!.TextContent.Should().Be("Excluir");
        linhas[0].QuerySelectorAll(".aj-documento__acoes").Should().BeEmpty();

        painel.FindAll(".aj-paginacao").Should().BeEmpty();   // uma página só
    }

    [Fact]
    public void Sem_gestao_envia_mas_nao_renomeia_nem_exclui()
    {
        var painel = Painel(podeGerenciar: false);

        painel.FindAll("div.aj-so-desktop li.aj-documento").Should().HaveCount(2);
        painel.FindAll("div.aj-tablet-mobile li.aj-documento").Should().HaveCount(2);
        painel.Find(".aj-painel__cabecalho button").TextContent.Should().Be("Enviar documento");
        painel.FindAll(".aj-documento__acoes").Should().BeEmpty();
        painel.FindAll(".aj-menu-acoes").Should().BeEmpty();
        painel.FindAll("button").Select(b => b.TextContent).Should().NotContain("Renomear").And.NotContain("Excluir");
    }

    [Fact]
    public void Enviar_abre_o_painel_manda_pela_atividade_sem_confirmacao_recarrega_e_avisa()
    {
        Despachante.Responde<EnviarDocumentoDaAtividade, Resultado<Guid>>(Resultado<Guid>.Ok(Guid.NewGuid()));
        var painel = Painel();

        AbrirEnvio(painel);

        Paineis(painel).Should().Equal("Documentos", "Enviar documento");
        painel.Markup.Should().Contain("Arquivo (até 50 MB)").And.Contain("Nome (opcional)");
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(painel.Find("input[type=file]"));
        painel.FindAll("section.aj-painel button").Single(b => b.TextContent == "Enviar").HasAttribute("disabled").Should().BeTrue();

        Escolher(painel, "minuta-final.docx");
        painel.FindAll("label.aj-rotulo").Single(l => l.TextContent.StartsWith("Nome (opcional)")).QuerySelector("input")!.Change("Minuta final");
        painel.FindAll("section.aj-painel button").Single(b => b.TextContent == "Enviar").Click();

        painel.WaitForAssertion(() => painel.Markup.Should().Contain("Documento enviado."));
        Despachante.Recebidas.OfType<EnviarDocumentoDaAtividade>().Should().ContainSingle()
            .Which.Should().Match<EnviarDocumentoDaAtividade>(e =>
                e.AtividadeId == Atividade && e.Nome == "Minuta final" && e.NomeArquivo == "minuta-final.docx");
        Despachante.Recebidas.OfType<ListarDocumentosDaAtividade>().Select(l => l.Pagina).Should().Equal(1, 1);
        Paineis(painel).Should().Equal("Documentos");
        painel.FindAll("[role=alertdialog]").Should().BeEmpty();   // o envio da atividade não pede confirmação
    }

    [Fact]
    public void Renomear_leva_o_foco_ao_nome_salva_fecha_o_painel_e_avisa()
    {
        Despachante.Responde<RenomearDocumento, Resultado>(Resultado.Ok());
        var painel = Painel();

        ClicarNaLinha(painel, "Renomear");

        Paineis(painel).Should().Equal("Documentos", "Renomear \"minuta-acordo-v2.docx\"");
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(painel.Find("section.aj-painel input.aj-campo"));

        painel.Find("section.aj-painel input.aj-campo").Change("Minuta assinada.docx");
        painel.FindAll("section.aj-painel button").Single(b => b.TextContent == "Salvar nome").Click();

        painel.WaitForAssertion(() => painel.Markup.Should().Contain("Nome alterado."));
        Despachante.Recebidas.OfType<RenomearDocumento>().Should().ContainSingle()
            .Which.Should().Be(new RenomearDocumento(Minuta, "Minuta assinada.docx"));
        Paineis(painel).Should().Equal("Documentos");
    }

    [Fact]
    public void Renomear_documento_que_sumiu_fecha_o_painel_e_recarrega_sem_avisar_a_ficha()
    {
        Despachante.Responde<RenomearDocumento, Resultado>(Resultado.Falha(TipoErro.NaoEncontrado, "Documento não encontrado."));
        var avisada = false;
        var painel = Painel(naoEncontrada: () => avisada = true);
        ClicarNaLinha(painel, "Renomear");

        painel.FindAll("section.aj-painel button").Single(b => b.TextContent == "Salvar nome").Click();

        painel.WaitForAssertion(() => painel.Markup.Should().Contain("Documento não encontrado."));
        Paineis(painel).Should().Equal("Documentos");
        Despachante.Recebidas.OfType<ListarDocumentosDaAtividade>().Should().HaveCount(2);
        painel.FindAll("div.aj-so-desktop li.aj-documento").Should().HaveCount(2);
        avisada.Should().BeFalse();
    }

    [Fact]
    public void Excluir_pede_confirmacao_no_aviso_e_so_entao_exclui()
    {
        Despachante.Responde<ExcluirDocumento, Resultado>(Resultado.Ok());
        var painel = Painel();

        ClicarNaLinha(painel, "Excluir");

        var confirmacao = painel.Find("[role=alertdialog]");
        confirmacao.GetAttribute("aria-label").Should().Be("Confirmar exclusão");
        confirmacao.QuerySelector("strong")!.TextContent.Should().Be("Confirmar a exclusão de minuta-acordo-v2.docx?");
        confirmacao.QuerySelector(".aj-aviso__texto")!.TextContent.Should().Contain("O documento é excluído da atividade.");
        confirmacao.QuerySelectorAll("button").Select(b => b.TextContent).Should().Equal("Cancelar", "Confirmar exclusão");
        Despachante.Recebidas.OfType<ExcluirDocumento>().Should().BeEmpty();

        painel.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar exclusão").Click();

        painel.WaitForAssertion(() => painel.Markup.Should().Contain("Documento excluído."));
        Despachante.Recebidas.OfType<ExcluirDocumento>().Should().ContainSingle().Which.Should().Be(new ExcluirDocumento(Minuta));
        painel.FindAll("[role=alertdialog]").Should().BeEmpty();
        Despachante.Recebidas.OfType<ListarDocumentosDaAtividade>().Should().HaveCount(2);
    }

    [Fact]
    public void Excluir_documento_que_sumiu_recarrega_a_lista_sem_avisar_a_ficha()
    {
        Despachante.Responde<ExcluirDocumento, Resultado>(Resultado.Falha(TipoErro.NaoEncontrado, "Documento não encontrado."));
        var avisada = false;
        var painel = Painel(naoEncontrada: () => avisada = true);
        ClicarNoMenu(painel, "Excluir", linha: 1);

        painel.FindAll("[role=alertdialog] button").Single(b => b.TextContent == "Confirmar exclusão").Click();

        painel.WaitForAssertion(() => painel.Markup.Should().Contain("Documento não encontrado."));
        Despachante.Recebidas.OfType<ExcluirDocumento>().Should().ContainSingle().Which.Should().Be(new ExcluirDocumento(Calculo));
        Despachante.Recebidas.OfType<ListarDocumentosDaAtividade>().Should().HaveCount(2);
        painel.FindAll("div.aj-so-desktop li.aj-documento").Should().HaveCount(2);
        avisada.Should().BeFalse();
    }

    [Fact]
    public void Uma_coisa_aberta_por_vez_entre_envio_renomear_e_exclusao()
    {
        var painel = Painel();

        AbrirEnvio(painel);
        Escolher(painel, "rascunho.docx");
        ClicarNoMenu(painel, "Renomear", linha: 1);

        Paineis(painel).Should().Equal("Documentos", "Renomear \"Cálculo das verbas\"");
        painel.FindAll("input[type=file]").Should().BeEmpty();

        ClicarNaLinha(painel, "Excluir");

        Paineis(painel).Should().Equal("Documentos");
        painel.Find("[role=alertdialog] strong").TextContent.Should().Be("Confirmar a exclusão de minuta-acordo-v2.docx?");

        AbrirEnvio(painel);

        painel.FindAll("[role=alertdialog]").Should().BeEmpty();
        Paineis(painel).Should().Equal("Documentos", "Enviar documento");
        // O arquivo escolhido antes não volta: o envio reaberto começa do zero.
        painel.FindAll("section.aj-painel button").Single(b => b.TextContent == "Enviar").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void AoContar_avisa_o_total_e_a_paginacao_aparece_com_mais_de_uma_pagina()
    {
        _total = 25;
        var contagens = new List<int>();

        var painel = Painel(aoContar: contagens.Add);

        contagens.Should().Equal(25);
        painel.Find(".aj-paginacao .aj-subtitulo").TextContent.Should().Be("Página 1 de 2 · 25 documentos");

        painel.FindAll(".aj-paginacao button").Single(b => b.TextContent == "Próxima").Click();

        painel.WaitForAssertion(() => Despachante.Recebidas.OfType<ListarDocumentosDaAtividade>().Select(l => l.Pagina)
            .Should().Equal(1, 2));
        contagens.Should().Equal(25, 25);
    }

    [Fact]
    public void Atividade_que_sumiu_avisa_a_ficha_e_troca_o_painel_pela_mensagem()
    {
        Despachante.Responde<ListarDocumentosDaAtividade, Resultado<Pagina<DocumentoResumo>>>(
            Resultado<Pagina<DocumentoResumo>>.Falha(TipoErro.NaoEncontrado, "Atividade não encontrada."));
        var avisada = false;
        var contagens = new List<int>();

        var painel = Painel(aoContar: contagens.Add, naoEncontrada: () => avisada = true);

        avisada.Should().BeTrue();
        painel.Find(".aj-painel__cabecalho h2").TextContent.Should().Be("Documentos");
        painel.Find("section .aj-estado-lista").TextContent.Should().Be("Atividade não encontrada.");
        painel.FindAll(".aj-painel__cabecalho button").Should().BeEmpty();
        painel.FindAll("li.aj-documento").Should().BeEmpty();
        painel.Markup.Should().NotContain("Tentar de novo").And.NotContain("Nenhum documento.");
        contagens.Should().BeEmpty();
    }
}
```

(A `Action` do parâmetro `naoEncontrada` é lambda de atribuição: se o compilador a achar ambígua, escreva-a com chaves, `() => { avisada = true; }`. No teste das linhas, 12:10 em UTC são 09:10 no horário do escritório.)

Run: `dotnet build -m:1`
Expected: não compila — `DocumentosDaAtividade` não tem `AoContar` (CS1061). É o RED.

- [ ] **Step 2: O componente**

Reescreva `src/AtividadesJuridicas.Web/Components/Atividades/DocumentosDaAtividade.razor` inteiro. As classes `aj-painel--ficha`, `aj-painel--documentos`, `aj-documentos`, `aj-documento`, `aj-documento__icone` (com `--texto` e `--planilha`), `aj-documento__texto`, `aj-documento__nome`, `aj-documento__meta` e `aj-documento__acoes` vêm da Tarefa 1; os ícones `texto` e `planilha`, da Tarefa 4.

```razor
@inherits GestaoDeDocumentos
@inject FusoDoEscritorio Fuso

@* O painel "Documentos" da ficha da atividade: a ficha o põe na coluna do desktop e na aba "Documentos" do tablet e
   do mobile, uma vez só. Avisos e a confirmação de exclusão logo acima dele; os painéis de envio e de renomear logo
   abaixo. O que muda por visão vai em invólucros dentro do painel. *@
@if (_mensagem is not null && !Encerrado)
{
    <Aviso Tipo="@(_falhou ? TipoDeAviso.Erro : TipoDeAviso.Sucesso)">@_mensagem</Aviso>
}

@if (AExcluir is { } aExcluir)
{
    @* A chave é o documento: pedir a exclusão de outro com a confirmação aberta é uma confirmação nova. *@
    <Aviso @key="aExcluir.Id" Tipo="TipoDeAviso.Atencao" Rotulo="Confirmar exclusão" Titulo="@($"Confirmar a exclusão de {aExcluir.Nome}?")">
        <ChildContent>O documento é excluído da atividade.</ChildContent>
        <Acoes>
            <button type="button" class="aj-botao aj-botao--secundario" disabled="@_removendo" @onclick="CancelarExclusao">Cancelar</button>
            <button type="button" class="aj-botao aj-botao--primario" disabled="@_removendo" @onclick="() => ExcluirAsync(aExcluir.Id)">Confirmar exclusão</button>
        </Acoes>
    </Aviso>
}

<section class="aj-painel aj-painel--ficha aj-painel--documentos">
    <div class="aj-painel__cabecalho">
        <h2>Documentos</h2>
        @* Quem vê a atividade envia (a Produção responsável também); só renomear e excluir pedem PodeGerenciar. *@
        @if (!Encerrado)
        {
            <button type="button" class="aj-botao aj-botao--secundario aj-botao--com-icone" disabled="@Ocupada" @onclick="AbrirEnvio"><Icone Nome="adicionar" />Enviar documento</button>
        }
    </div>

    @if (_acessoEncerrado)
    {
        @* A página foi avisada; se ela não retirar o painel, ele não finge uma lista vazia. *@
        <Aviso Tipo="TipoDeAviso.Erro">Você não tem mais acesso a esta atividade.</Aviso>
    }
    else if (_naoEncontrada)
    {
        @* Perdeu a visibilidade da atividade: a mensagem do caso de uso ocupa o lugar da lista. *@
        <div class="aj-estado-lista"><strong>@_mensagemNaoEncontrada</strong></div>
    }
    else
    {
        <EstadoDaLista Erro="@_erroDaLista" Carregando="@(!_carregado)" Vazio="@(_pagina.Itens.Count == 0)"
                       TextoDeVazio="Nenhum documento." TentarDeNovo="() => IrParaAsync(_paginaPedida)">
            <div class="aj-so-desktop">
                <ul class="aj-documentos">
                    @foreach (var documento in _pagina.Itens)
                    {
                        <li class="aj-documento" @key="documento.Id">
                            @if (EhPlanilha(documento))
                            {
                                <span class="aj-documento__icone aj-documento__icone--planilha" aria-hidden="true"><Icone Nome="planilha" /></span>
                            }
                            else
                            {
                                <span class="aj-documento__icone aj-documento__icone--texto" aria-hidden="true"><Icone Nome="texto" /></span>
                            }
                            @* Link fora da navegação do Blazor: é um download, não uma página. O atributo "download"
                               é o que impede o roteador do Blazor de interceptar o clique; não o remova. *@
                            <div class="aj-documento__texto"><a href="@TextosDeDocumento.Endereco(documento.Id)" download data-enhance-nav="false" class="aj-documento__nome">@documento.Nome</a><span class="aj-documento__meta">@MetaCompleta(documento)</span></div>
                            @if (PodeGerenciar)
                            {
                                @* Os botões colados: o espaço entre eles seria desenhado. *@
                                <div class="aj-documento__acoes"><button type="button" class="aj-botao aj-botao--fantasma" disabled="@Ocupada" @onclick="() => RenomearLinha(documento)">Renomear</button><button type="button" class="aj-botao aj-botao--perigo" disabled="@Ocupada" @onclick="() => ExcluirLinha(documento.Id)">Excluir</button></div>
                            }
                        </li>
                    }
                </ul>
            </div>

            <div class="aj-tablet-mobile">
                <ul class="aj-documentos">
                    @foreach (var documento in _pagina.Itens)
                    {
                        <li class="aj-documento" @key="documento.Id">
                            @if (EhPlanilha(documento))
                            {
                                <span class="aj-documento__icone aj-documento__icone--planilha" aria-hidden="true"><Icone Nome="planilha" /></span>
                            }
                            else
                            {
                                <span class="aj-documento__icone aj-documento__icone--texto" aria-hidden="true"><Icone Nome="texto" /></span>
                            }
                            <div class="aj-documento__texto"><a href="@TextosDeDocumento.Endereco(documento.Id)" download data-enhance-nav="false" class="aj-documento__nome">@documento.Nome</a><span class="aj-documento__meta">@MetaCurta(documento)</span></div>
                            @if (PodeGerenciar)
                            {
                                <MenuDeAcoes Rotulo="@($"Ações de {documento.Nome}")">
                                    <button type="button" class="aj-menu-acoes__item" disabled="@Ocupada" @onclick="() => RenomearLinha(documento)">Renomear</button>
                                    <button type="button" class="aj-menu-acoes__item aj-menu-acoes__item--perigo" disabled="@Ocupada" @onclick="() => ExcluirLinha(documento.Id)">Excluir</button>
                                </MenuDeAcoes>
                            }
                        </li>
                    }
                </ul>
            </div>

            <Paginacao Numero="_pagina.Numero" TotalDePaginas="_pagina.TotalDePaginas" Total="_pagina.Total"
                       Singular="documento" Plural="documentos" Ocupado="@Ocupada" IrPara="IrParaAsync" />
        </EstadoDaLista>
    }
</section>

@if (_envioAberto && !Encerrado)
{
    <section class="aj-painel">
        <h2>Enviar documento</h2>
        <label class="aj-rotulo">@RotuloDoArquivo<InputFile @key="_versaoDoCampo" @ref="_campoDeArquivo" OnChange="Escolher" class="aj-campo" /></label>
        <label class="aj-rotulo">Nome (opcional)<input type="text" class="aj-campo" @bind="_nomeDoEnvio" /></label>
        <div class="aj-painel__acoes"><button type="button" class="aj-botao aj-botao--secundario" disabled="@_enviando" @onclick="DescartarEnvio">Cancelar</button><button type="button" class="aj-botao aj-botao--primario" disabled="@(_arquivo is null || _enviando)" @onclick="EnviarAsync">@(_enviando ? "Enviando…" : "Enviar")</button></div>
    </section>
}

@if (_renomeando is { } renomeando && !Encerrado)
{
    <section class="aj-painel">
        <h2>@($"Renomear \"{renomeando.Nome}\"")</h2>
        <label class="aj-rotulo">Novo nome<input type="text" class="aj-campo" @bind="_novoNome" @ref="_campoDoNovoNome" /></label>
        <div class="aj-painel__acoes"><button type="button" class="aj-botao aj-botao--secundario" @onclick="FecharRenomear">Cancelar</button><button type="button" class="aj-botao aj-botao--primario" @onclick="RenomearAsync">Salvar nome</button></div>
    </section>
}
```

No `@code`. Saem do arquivo de hoje: `UmMegabyte` e `Vazia` (a base tem), o parâmetro `OnAcessoEncerrado` (é da base: redeclarar esconde o da base e vira aviso, que o build trata como erro), e todo o estado e os métodos que a base já tem (`_carregado`, `_ocupado`, `_enviando`, `_paginaPedida`, `_mensagem`, `_erroDaLista`, `_pagina`, `_arquivo`, `_nomeDoEnvio`, `_versaoDoCampo`, `_excluindo`, `_renomeando`, `_novoNome`, `IrParaAsync`, `CarregarAsync`, `Escolher`, `EnviarAsync`, `AbrirRenomear`, `RenomearAsync`, `PedirExclusao`, `CancelarExclusao`, `FecharRenomear`, `ExcluirAsync`, `Avisar`). Fica o que é só da atividade:

```csharp
@code {
    private static readonly HashSet<string> Planilhas = new(StringComparer.OrdinalIgnoreCase) { ".xlsx", ".xls", ".csv", ".ods" };

    [Parameter, EditorRequired] public Guid AtividadeId { get; set; }

    /// <summary>Renomear e excluir; quem decide é a ficha (o servidor confirma em cada chamada).</summary>
    [Parameter] public bool PodeGerenciar { get; set; }

    /// <summary>O total de documentos da atividade, a cada leitura bem-sucedida (o contador da aba "Documentos").</summary>
    [Parameter] public EventCallback<int> AoContar { get; set; }

    /// <summary>Avisado quando a atividade não existe (ou não é mais visível): a página recarrega o registro.</summary>
    [Parameter] public EventCallback OnAtividadeNaoEncontrada { get; set; }

    private bool _naoEncontrada;
    private bool _acessoEncerrado;
    private string? _mensagemNaoEncontrada;
    private InputFile? _campoDeArquivo;
    private ElementReference _campoDoNovoNome;

    // O arquivo enviado de cada documento da página: o nome pode ter sido trocado sem a extensão, e o tipo do ícone
    // é o do arquivo.
    private IReadOnlyDictionary<Guid, string> _arquivos = new Dictionary<Guid, string>();

    // Os documentos da atividade ficam na categoria de atividade (a ficha do cliente os mostra em "De atividades"); a
    // base não pede confirmação de envio nessa categoria, como hoje.
    protected override CategoriaDocumento CategoriaDosDocumentos => CategoriaDocumento.DocumentoAtividade;

    // A tela não conhece o cliente: o servidor grava o documento no cliente que a atividade tiver no momento (que pode
    // não haver), e a lista e o envio vão pela atividade (os dois ganchos abaixo), sem usar o cliente.
    protected override Guid? ClienteDosDocumentos => null;

    // O envio da atividade não pede confirmação: o aviso de envio nunca abre.
    protected override ValueTask FocarAvisoDeEnvioAsync() => ValueTask.CompletedTask;

    // Uma coisa aberta por vez: o campo é o do painel que está na tela.
    protected override ValueTask FocarPainelAbertoAsync() =>
        _renomeando is not null ? _campoDoNovoNome.FocusAsync()
        : _envioAberto && _campoDeArquivo?.Element is { } campo ? campo.FocusAsync()
        : ValueTask.CompletedTask;

    protected override async Task<Resultado<Pagina<DocumentoResumo>>> ListarPaginaAsync(int pagina)
    {
        var resultado = await Despachante.EnviarAsync(new ListarDocumentosDaAtividade(AtividadeId, pagina));
        if (resultado.Valor is { } lida) _arquivos = lida.Itens.ToDictionary(d => d.Id, d => d.NomeArquivoOriginal);
        await AtividadeComoAlvoAsync(resultado);
        return resultado;
    }

    protected override async Task<Resultado<Guid>> EnviarArquivoAsync(string? nome, string nomeArquivo, string? tipoConteudo, Stream conteudo)
    {
        var resultado = await Despachante.EnviarAsync(new EnviarDocumentoDaAtividade(AtividadeId, nome, nomeArquivo, tipoConteudo, conteudo));
        await AtividadeComoAlvoAsync(resultado);
        return resultado;
    }

    protected override Task AoCarregarAsync() => AoContar.InvokeAsync(_pagina.Total);

    /// <summary>
    /// Listar e enviar têm a atividade como alvo: "não encontrado" é a atividade que sumiu (ou deixou de ser visível),
    /// e a mensagem ocupa o lugar da lista e a ficha é avisada; acesso negado marca o painel (a base avisa a página).
    /// Renomear e excluir respondem "não encontrado" pelo documento e não passam por aqui (a base recarrega a lista).
    /// </summary>
    private async Task AtividadeComoAlvoAsync(Resultado resultado)
    {
        if (resultado.Erro == TipoErro.AcessoNegado)
        {
            _acessoEncerrado = true;
            return;
        }

        if (resultado.Erro != TipoErro.NaoEncontrado) return;

        _naoEncontrada = true;
        _mensagemNaoEncontrada = resultado.Mensagem;
        await OnAtividadeNaoEncontrada.InvokeAsync();
    }

    private bool Encerrado => _naoEncontrada || _acessoEncerrado;

    private Linha? AExcluir => _pagina.Itens.FirstOrDefault(documento => documento.Id == _excluindo);

    private string RotuloDoArquivo => $"Arquivo (até {DocumentosOpcoes.TamanhoMaximoBytes / UmMegabyte} MB)";

    private bool EhPlanilha(Linha documento) =>
        Planilhas.Contains(Path.GetExtension(_arquivos.GetValueOrDefault(documento.Id, documento.Nome)));

    // Desktop: tamanho, autor e data; tablet e mobile: tamanho e autor. Sem autor conhecido, sem a parte dele.
    private string MetaCompleta(Linha documento) =>
        Juntar(TamanhoDeArquivo.Formatar(documento.TamanhoBytes), documento.EnviadoPor, Fuso.Formatar(documento.EnviadoEm));

    private static string MetaCurta(Linha documento) =>
        Juntar(TamanhoDeArquivo.Formatar(documento.TamanhoBytes), documento.EnviadoPor);

    private static string Juntar(params string?[] partes) =>
        string.Join(" · ", partes.Where(parte => !string.IsNullOrWhiteSpace(parte)));
}
```

O que o herdeiro devolve nos membros abstratos da base, e por quê:

- `CategoriaDosDocumentos` → `CategoriaDocumento.DocumentoAtividade`: é a categoria em que o servidor grava o documento da atividade. Com ela a base cria `EnvioComConfirmacao(exigeConfirmacao: false)` (só a categoria comum pede confirmação), e "Enviar" envia direto, como hoje. `DeAtividades` da base fica verdadeiro, mas só os herdeiros o leem, e este não o usa (o envio aqui existe).
- `ClienteDosDocumentos` → `null`: a tela só conhece a atividade; o cliente do documento é decidido pelo servidor na hora de gravar. A base só usa o cliente nos padrões de listar e enviar, que este herdeiro sobrescreve (a Tarefa 5 levou para eles a conferência do cliente, que antes ficava em `OnInitializedAsync`); por isso o nulo vale nesta categoria.
- `FocarAvisoDeEnvioAsync` → `ValueTask.CompletedTask`: sem confirmação, o aviso de envio nunca abre e a base nunca chama o foco dele (o mesmo que `DocumentosDaFicha`).

Nenhum botão de acesso (o editor de acesso por perfis não existe para documento de atividade): o herdeiro não chama `AbrirAcessoDaLinhaAsync` nem desenha o painel de `_acesso`.

- [ ] **Step 3: Trava e testes**

Acrescente a `Migradas` em `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`, entre `"Components/Atividades/ComentariosDaAtividade.razor",` (Tarefa 8) e `"Components/Atividades/EtiquetaDeSituacao.razor",` (Tarefa 4): `"Components/Atividades/DocumentosDaAtividade.razor",`.

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros (a ficha antiga, `FichaDaAtividade.razor`, continua compilando: passa `AtividadeId`, `PodeGerenciar`, `OnAcessoEncerrado`, agora da base, e `OnAtividadeNaoEncontrada`).

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DocumentosDaAtividadeInterativosTests"`
Expected: 10 passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~DocumentosDaFicha|FullyQualifiedName~DocumentosComuns|FullyQualifiedName~DocumentosDoPortal|FullyQualifiedName~DocumentosDeAtividadeTests|FullyQualifiedName~ProdutoresWebTests"`
Expected: todos passando, sem mudança (esta tarefa não mexe na base; a ficha antiga embutida em Produtores ainda desenha o componente).

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Expected: todos passando.

- [ ] **Step 4: Commit**

Confira o branch (`git branch --show-current` deve dizer `worktree-feat+fase-9e-atividades`).

```
git add src tests
git commit -m "feat(web): documentos da atividade herdam a gestão de documentos, com o ícone do tipo, as ações por visão e o envio em painel"
```

---

### Task 10: Ficha da atividade em tela própria (leitura)

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Pages/AtividadeFicha.razor` (rota `/atividades/{Id:guid}`), `src/AtividadesJuridicas.Web/Components/Atividades/CabecalhoDaAtividade.razor`
- Modify: `src/AtividadesJuridicas.Web/Components/Produtores/PainelDeProdutividade.razor` (link para a ficha no lugar do painel)
- Delete: `src/AtividadesJuridicas.Web/Components/Atividades/FichaDaAtividade.razor`
- Create: `tests/AtividadesJuridicas.IntegrationTests/AtividadeFichaWebTests.cs`, `AtividadeFichaInterativaTests.cs`
- Modify: `ProdutoresWebTests.cs` (o link), `PortalDoClienteWebTests.cs`, `AutenticacaoWebTests.cs`, `TelasNoDesignSystemTests.cs` (`Migradas`)

**Interfaces:**
- Consumes: `Abas`/`Aba`, `BarraDeVoltar.Acao` (só a Tarefa 11 o preenche), `ComentariosDaAtividade` (Tarefa 8), `DocumentosDaAtividade` (Tarefa 9), `EtiquetaDeSituacao`, `AvatarDePessoa`, `TextosDeAtividade`, `Recados`, `Aviso`; `ObterMeuAcesso`, `ObterAtividade`.
- Produces: a rota `/atividades/{Id:guid}` com `[SupplyParameterFromQuery] De` e `Produtor` (os dois `string?`; `Produtor` que não é um id volta a `/produtores`); `CabecalhoDaAtividade` com `Detalhe` (`AtividadeDetalhe`), `VoltarPara`, `Origem`, `RotuloDoVoltarNoMobile`, `Faixa` (`RenderFragment?`, a faixa de ações do desktop e do tablet) e `AcaoDaBarra` (`RenderFragment?`, o "Mais ações" do mobile) — a Tarefa 11 preenche os dois encaixes. Na página, o que a Tarefa 11 usa: `Task<bool> RecarregarAsync()` (se a ficha ficou com dados atuais), `CarregarAsync()`, `EncerrarAcesso()`, os campos `_detalhe`, `_ator` e `_recado`, e os auxiliares de teste `Detalhe(…)` e `Combinar(…)` de `AtividadeFichaInterativaTests`.

**Regras:**
- Quadros: `FichaDaAtividade.dc.html`, `FichaTablet.dc.html`, `FichaMobile.dc.html` (`quadros-ficha.md`, seções 1 a 3, e a seção 6, item 9, para o arranjo).
- Arranjo: `Abas` (num invólucro `aj-so-mobile`) antes do corpo; corpo `aj-lado-a-lado aj-lado-a-lado--painel-primeiro aj-lado-a-lado--ficha aj-lado-a-lado--painel-estreito` com o painel "Detalhes" (`aj-so-desktop`) primeiro e a coluna `aj-lado-a-lado__lista aj-pilha` com: no desktop, Descrição, Responsáveis e Histórico; no tablet e no mobile (aba `detalhes`), a grade Descrição | Dados e Responsáveis, e (aba `historico`) o Histórico; depois Comentários (aba `comentarios`) e Documentos (aba `documentos`), cada um uma vez só, no seu invólucro `aj-aba-painel`.
- Cabeçalho (`CabecalhoDaAtividade`): voltar (desktop e tablet `aj-voltar`; mobile `BarraDeVoltar` com o título `Atividade`); cartão com a linha de situação (`EtiquetaDeSituacao`, a Atrasada junto, e `TextosDeAtividade.Entrega`, curta no mobile), o título (`h1` no desktop e no tablet; no mobile o `h1` é o da barra e o título vai em `h2`, padrão da 9D) e o encaixe `Faixa`.
- Detalhes: `Entrega`, `Cliente`, `Processo`, `Área`, `Complexidade`, `Autor` (`Sistema` quando o autor é o sistema), `Revisor`; `—` no vazio. Dados (tablet e mobile): `Cliente`, `Processo`, `Autor`, `Revisor`, `Complexidade`.
- Responsáveis (`aj-responsaveis`): avatar, nome, estado (`Concluída em {data completa}` no desktop, curta no tablet e no mobile, com o ícone; `Pendente` sem ícone), direcionamento quando visível; vazio `Nenhum responsável.`.
- Histórico (`aj-historico`): um item por registro, na ordem do servidor; `TextosDeAtividade.Mudanca`; `{nome ou Sistema} · {data}`; o motivo numa terceira linha quando houver; a cor do ponto pela situação nova; o último cheio e sem traço; vazio `Nenhum registro.`.
- Abas: `Detalhes`, `Comentários {n}` e `Documentos {n}` (contadores pelo `AoContar`; sem contador até a primeira carga bem-sucedida; se uma recarga falhar depois de uma carga boa, o contador fica com o número anterior, porque `AoContar` é `EventCallback<int>` e só avisa o sucesso), `Histórico`. Começa em `detalhes`.
- Estados: carregando; `Atividade não encontrada.` com o voltar (sem painéis e sem os componentes de comentários e documentos, que não consultam); erro com "Tentar de novo"; acesso encerrado (`Seu acesso foi encerrado. Entre novamente.`); recado de outra tela, uma vez, no topo. A Produção que não é responsável recebe `NaoEncontrado` do servidor e vê o não encontrado.
- Voltar: `/atividades` ("Atividades"); com `de=produtores`, `/produtores?produtor={Produtor}` (sem `Produtor`, `/produtores`), com o rótulo "Produtores".
- `PainelDeProdutividade`: "Abrir" (desktop) e a linha (tablet e mobile) passam a ser links para `/atividades/{id}?de=produtores&produtor={produtorId}`; saem `_atividadeAberta`, `Abrir`, `FecharFicha` e a ficha embutida.
- A ficha antiga (`FichaDaAtividade.razor`) é apagada nesta tarefa (a lista já não a usa desde a Tarefa 6).
- Testes HTTP: Diretoria e Produção responsável veem a ficha; Produção não responsável e id inexistente veem o não encontrado (Review Focus 3: para a Produção não responsável, a falta da consulta de comentários e documentos se prova pelos eventos de acesso negado, só o de `ObterAtividade`; para o id inexistente, pelo bUnit, sem pedido ao despachante); voltar de Produtores; textos e classes de cada visão; rota fechada ao portal e a anônimo. Testes bUnit: abas trocam o painel; contadores das abas; histórico com motivo; recado.

- [ ] **Step 1: Testes HTTP da ficha (devem falhar)**

Antes de escrever a marcação, releia `FichaDaAtividade.dc.html`, `FichaTablet.dc.html` e `FichaMobile.dc.html` na cópia local dos quadros e as seções 1 a 4 e 6 de `quadros-ficha.md`. Se um quadro diferir do que este passo descreve, pare e relate.

Crie `tests/AtividadesJuridicas.IntegrationTests/AtividadeFichaWebTests.cs`. As atividades nascem pelo despachante real (`AplicacaoDeTeste.ExecutarAsync`), como a Diretoria (`_admin`) e como o Pedro (Produção):

- `_minuta`: com cliente, processo (área "Trabalhista"), entrega em 09/10/2090 e complexidade 2; responsável o Pedro, com direcionamento; revisora a Diretoria; o Pedro conclui a parte, e a atividade fica **Feita** (histórico: Criada; Criada → Direcionada; Direcionada → Feita).
- `_interna`: sem cliente, sem processo, sem área e sem responsáveis, com entrega em 15/01/2020 (**Criada** e atrasada); o Pedro não a vê.

```csharp
using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Auditoria;
using AtividadesJuridicas.Application.Cadastros;
using AtividadesJuridicas.Domain.Cadastros;
using AtividadesJuridicas.Web.Components.Atividades;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AtividadeFichaWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _pedro, _minuta, _interna;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();

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

        var minuta = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Minuta de acordo trabalhista", "Redigir a minuta com as condições da audiência.", new DateOnly(2090, 10, 9),
            litoral.Valor, processo.Valor, null, 2));
        minuta.Sucesso.Should().BeTrue(minuta.Mensagem);
        _minuta = minuta.Valor;
        (await _app.ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
            _minuta, [new ResponsavelEntrada(_pedro, "Conferir os cálculos das verbas rescisórias.")], _admin))).Sucesso.Should().BeTrue();
        (await _app.ExecutarAsync(_pedro, new ConcluirMinhaParte(_minuta))).Sucesso.Should().BeTrue();

        var interna = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Recurso interno", "Sem responsáveis.", new DateOnly(2020, 1, 15), null, null, null, 3));
        interna.Sucesso.Should().BeTrue(interna.Mensagem);
        _interna = interna.Valor;
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

    [Fact]
    public async Task Diretoria_ve_o_cabecalho_os_detalhes_os_responsaveis_e_o_historico_em_cada_visao()
    {
        var html = await PaginaAsync($"/atividades/{_minuta}");

        // O voltar: link no desktop e no tablet, barra de voltar com "Atividade" no mobile.
        html.Should().Contain("<h1>Atividade</h1>").And.Contain("aria-label=\"Voltar para Atividades\"")
            .And.MatchRegex("<a href=\"/atividades\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Atividades</a>");

        BlocoDeVisao.De(html, "aj-desktop-tablet").Should().MatchRegex(
            "(?s)<header class=\"aj-painel aj-painel--principal aj-painel--cabecalho-com-faixa\">\\s*" +
            "<div class=\"aj-titulo-da-ficha aj-titulo-da-ficha--com-situacao\">\\s*" +
            "<div class=\"aj-titulo-da-ficha__situacao\">\\s*<span class=\"aj-etiqueta aj-etiqueta--feita[^\"]*\">Feita</span>\\s*" +
            "<span class=\"aj-titulo-da-ficha__entrega\">Entrega em 09/10/2090 · Trabalhista</span>\\s*</div>\\s*" +
            "<h1 class=\"aj-titulo\">Minuta de acordo trabalhista</h1>");

        // Desktop: o painel Detalhes vem primeiro no arranjo (o CSS o leva para a direita).
        html.Should().MatchRegex(
            "<div class=\"aj-lado-a-lado aj-lado-a-lado--painel-primeiro aj-lado-a-lado--ficha aj-lado-a-lado--painel-estreito\">\\s*" +
            "<div class=\"aj-so-desktop\">\\s*<aside class=\"aj-painel aj-painel--ficha aj-painel--detalhes aj-lado-a-lado__painel\" aria-label=\"Dados da atividade\">\\s*<h2>Detalhes</h2>");
        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex(
                "(?s)<h2>Detalhes</h2>\\s*<dl class=\"aj-detalhes\">\\s*" +
                "<div><dt>Entrega</dt><dd>09/10/2090</dd></div>\\s*" +
                "<div><dt>Cliente</dt><dd>Transportes Litoral Ltda.</dd></div>\\s*" +
                "<div><dt>Processo</dt><dd>Litoral — reclamação trabalhista</dd></div>\\s*" +
                "<div><dt>Área</dt><dd>Trabalhista</dd></div>\\s*" +
                "<div><dt>Complexidade</dt><dd>2</dd></div>\\s*" +
                "<div><dt>Autor</dt><dd>Diana Diretora</dd></div>\\s*" +
                "<div><dt>Revisor</dt><dd>Diana Diretora</dd></div>\\s*</dl>")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-painel--ficha aj-painel--descricao\">\\s*<h2>Descrição</h2>\\s*<p>Redigir a minuta com as condições da audiência.</p>")
            .And.Contain($"<li class=\"aj-responsavel\"><span class=\"aj-avatar {AvatarDePessoa.Classe("Pedro Produção")}\" aria-hidden=\"true\">PP</span>" +
                "<div class=\"aj-responsavel__texto\"><div class=\"aj-responsavel__cabeca\"><strong>Pedro Produção</strong><span class=\"aj-responsavel__estado\">")
            .And.MatchRegex("<span class=\"aj-so-desktop\">Concluída em \\d{2}/\\d{2}/\\d{4} \\d{2}:\\d{2}</span>")
            .And.Contain("<span class=\"aj-responsavel__direcionamento\">Conferir os cálculos das verbas rescisórias.</span>");
        // Histórico em linha do tempo, na ordem do servidor: o primeiro registro só com a situação nova; o último cheio e sem traço.
        Grupos(desktop, "<span class=\"aj-historico__mudanca\">([^<]+)</span>")
            .Should().Equal("Criada", "Criada → Direcionada", "Direcionada → Feita");
        Grupos(desktop, "<span class=\"aj-historico__autor\">([^<]+) · \\d{2}/\\d{2}/\\d{4} \\d{2}:\\d{2}</span>")
            .Should().Equal("Diana Diretora", "Diana Diretora", "Pedro Produção");
        desktop.Should().Contain("<span class=\"aj-historico__ponto\"></span><span class=\"aj-historico__linha\"></span>")
            .And.Contain("<span class=\"aj-historico__ponto aj-historico__ponto--direcionada\"></span><span class=\"aj-historico__linha\"></span>")
            .And.Contain("<span class=\"aj-historico__ponto aj-historico__ponto--feita aj-historico__ponto--atual\"></span></div>");

        // Tablet e mobile: a grade Descrição | Dados (sem Entrega e sem Área), e as datas curtas dos responsáveis.
        var tabletEMobile = BlocoDeVisao.De(html, "aj-tablet-mobile");
        tabletEMobile.Should().MatchRegex(
                "(?s)<div class=\"aj-aba-painel\">\\s*<div class=\"aj-grade\">\\s*<section class=\"aj-painel aj-painel--ficha aj-painel--descricao\">\\s*<h2>Descrição</h2>.*?</section>\\s*" +
                "<section class=\"aj-painel aj-painel--ficha aj-painel--detalhes\">\\s*<h2>Dados</h2>\\s*<dl class=\"aj-detalhes\">\\s*" +
                "<div><dt>Cliente</dt><dd>Transportes Litoral Ltda.</dd></div>\\s*" +
                "<div><dt>Processo</dt><dd>Litoral — reclamação trabalhista</dd></div>\\s*" +
                "<div><dt>Autor</dt><dd>Diana Diretora</dd></div>\\s*" +
                "<div><dt>Revisor</dt><dd>Diana Diretora</dd></div>\\s*" +
                "<div><dt>Complexidade</dt><dd>2</dd></div>\\s*</dl>")
            .And.MatchRegex("<span class=\"aj-tablet-mobile\">Concluída em \\d{2}/\\d{2} \\d{2}:\\d{2}</span>")
            .And.MatchRegex("<div class=\"aj-aba-painel aj-aba-painel--oculta\">\\s*<section class=\"aj-painel aj-painel--ficha aj-painel--historico\">\\s*<h2>Histórico</h2>");

        // Mobile: a situação com a entrega curta e o título em h2 (o h1 é o da barra); as abas começam em Detalhes.
        BlocoDeVisao.De(html, "aj-so-mobile").Should()
            .MatchRegex(
                "(?s)<div class=\"aj-titulo-da-ficha aj-titulo-da-ficha--com-situacao\">\\s*" +
                "<div class=\"aj-titulo-da-ficha__situacao\">\\s*<span class=\"aj-etiqueta aj-etiqueta--feita[^\"]*\">Feita</span>\\s*" +
                "<span class=\"aj-titulo-da-ficha__entrega\">Entrega 09/10/2090 · Trabalhista</span>\\s*</div>\\s*" +
                "<h2 class=\"aj-titulo\">Minuta de acordo trabalhista</h2>")
            .And.MatchRegex(
                "(?s)<div class=\"aj-abas\" role=\"tablist\" aria-label=\"Seções da atividade\">.*?aria-selected=\"true\"[^>]*>Detalhes</button>" +
                ".*?>Comentários(<span class=\"aj-abas__contador\">\\d+</span>)?</button>" +
                ".*?>Documentos(<span class=\"aj-abas__contador\">\\d+</span>)?</button>.*?>Histórico</button>");

        // Comentários e documentos: um painel de cada, uma vez só (os componentes têm estado próprio).
        Regex.Matches(html, "<h2>Comentários</h2>").Count.Should().Be(1);
        Regex.Matches(html, "<h2>Documentos</h2>").Count.Should().Be(1);
    }

    [Fact]
    public async Task Producao_responsavel_ve_a_ficha_com_o_proprio_direcionamento()
    {
        var html = await PaginaAsync($"/atividades/{_minuta}", comoPedro: true);

        html.Should().Contain("<h1 class=\"aj-titulo\">Minuta de acordo trabalhista</h1>").And.Contain("<h2>Detalhes</h2>")
            .And.Contain("<span class=\"aj-responsavel__direcionamento\">Conferir os cálculos das verbas rescisórias.</span>")
            .And.Contain("<h2>Comentários</h2>").And.Contain("<h2>Documentos</h2>");
        html.Should().NotContain("Atividade não encontrada.").And.NotContain("Seu acesso foi encerrado");
    }

    [Fact]
    public async Task Producao_que_nao_e_responsavel_ve_nao_encontrada_sem_paineis_e_sem_consultar_comentarios_nem_documentos()
    {
        var antes = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Count;

        var html = await PaginaAsync($"/atividades/{_interna}", comoPedro: true);

        html.Should().Contain("Atividade não encontrada.").And.MatchRegex("<a href=\"/atividades\" class=\"aj-voltar\">");
        html.Should().NotContain("Recurso interno").And.NotContain("<h2>Detalhes</h2>").And.NotContain("<h2>Comentários</h2>")
            .And.NotContain("<h2>Documentos</h2>").And.NotContain("aj-abas");
        // Só a leitura da atividade foi negada: os comentários e os documentos nem foram pedidos (cada pedido negado deixaria um registro).
        var negados = (await _app.EventosAsync(EventosDeSeguranca.AcessoNegado)).Skip(antes)
            .Select(e => (string?)JsonNode.Parse(e.Dados!)!["requisicao"]).ToList();
        negados.Should().NotBeEmpty().And.OnlyContain(r => r == nameof(ObterAtividade));
    }

    [Fact]
    public async Task Atividade_que_nao_existe_mostra_nao_encontrada_com_o_voltar_e_nada_mais()
    {
        var html = await PaginaAsync($"/atividades/{Guid.NewGuid()}");

        html.Should().Contain("Atividade não encontrada.").And.Contain("aria-label=\"Voltar para Atividades\"")
            .And.MatchRegex("<a href=\"/atividades\" class=\"aj-voltar\">");
        html.Should().NotContain("<h2>Detalhes</h2>").And.NotContain("<h2>Comentários</h2>").And.NotContain("<h2>Documentos</h2>")
            .And.NotContain("aj-abas");
    }

    [Fact]
    public async Task Vinda_de_produtores_volta_ao_painel_do_produtor()
    {
        var cliente = await _app.ClienteAutenticadoAsync();
        async Task<string> Pagina(string rota) => WebUtility.HtmlDecode(await cliente.GetStringAsync(rota));

        var comProdutor = await Pagina($"/atividades/{_minuta}?de=produtores&produtor={_pedro}");
        var semProdutor = await Pagina($"/atividades/{_minuta}?de=produtores");
        var produtorQueNaoVale = await Pagina($"/atividades/{_minuta}?de=produtores&produtor=lixo");
        var naoEncontrada = await Pagina($"/atividades/{Guid.NewGuid()}?de=produtores&produtor={_pedro}");

        comProdutor.Should().MatchRegex($"<a href=\"/produtores\\?produtor={_pedro}\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Produtores</a>")
            .And.Contain("aria-label=\"Voltar para Produtores\"")
            .And.Contain("<h1 class=\"aj-titulo\">Minuta de acordo trabalhista</h1>");
        comProdutor.Should().NotContain("<a href=\"/atividades\" class=\"aj-voltar\">");
        semProdutor.Should().MatchRegex("<a href=\"/produtores\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Produtores</a>");
        produtorQueNaoVale.Should().MatchRegex("<a href=\"/produtores\" class=\"aj-voltar\">");
        naoEncontrada.Should().Contain("Atividade não encontrada.")
            .And.MatchRegex($"<a href=\"/produtores\\?produtor={_pedro}\" class=\"aj-voltar\">");
    }

    [Fact]
    public async Task Atividade_sem_vinculos_nem_responsaveis_mostra_a_atrasada_e_os_vazios()
    {
        var html = await PaginaAsync($"/atividades/{_interna}");

        // A atrasada vai junto da situação, com ponto e sem ícone; sem área, a entrega fica sem a parte da área.
        BlocoDeVisao.De(html, "aj-desktop-tablet").Should().MatchRegex(
            "<div class=\"aj-titulo-da-ficha__situacao\">\\s*<span class=\"aj-etiqueta aj-etiqueta--criada[^\"]*\">Criada</span>\\s*" +
            "<span class=\"aj-etiqueta aj-etiqueta--atrasada[^\"]*\">Atrasada</span>\\s*" +
            "<span class=\"aj-titulo-da-ficha__entrega\">Entrega em 15/01/2020</span>\\s*</div>");
        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().Contain("<div><dt>Cliente</dt><dd>—</dd></div>").And.Contain("<div><dt>Processo</dt><dd>—</dd></div>")
            .And.Contain("<div><dt>Área</dt><dd>—</dd></div>").And.Contain("<div><dt>Revisor</dt><dd>—</dd></div>")
            .And.Contain("<div><dt>Complexidade</dt><dd>3</dd></div>")
            .And.MatchRegex("<h2>Responsáveis</h2>\\s*<div class=\"aj-estado-lista\"><strong>Nenhum responsável.</strong></div>");
        Grupos(desktop, "<span class=\"aj-historico__mudanca\">([^<]+)</span>").Should().Equal("Criada");
        // Um registro só: é o atual (cheio) e não tem traço para o seguinte.
        desktop.Should().Contain("<span class=\"aj-historico__ponto aj-historico__ponto--atual\"></span></div>")
            .And.NotContain("aj-historico__linha");
    }
}
```

(O CNPJ é o mesmo dos testes da ficha do cliente. Se a etiqueta da situação vier com outro espaço em branco entre as marcas, ajuste só o `\s*`, sem mudar o que o teste prova.)

Em `PortalDoClienteWebTests`, acrescente `[InlineData("/atividades/0199a3f2-7c1e-7000-8000-000000000001")]` à theory `Cliente_em_pagina_interna_ve_nao_encontrada_e_nenhum_dado`; em `AutenticacaoWebTests`, a mesma rota na theory `Anonimo_e_redirecionado_ao_login_em_qualquer_rota`.

- [ ] **Step 2: Testes bUnit da ficha (devem falhar)**

Crie `tests/AtividadesJuridicas.IntegrationTests/AtividadeFichaInterativaTests.cs`. A atividade de teste é um `AtividadeDetalhe` montado no teste (`Detalhe(...)`), com as `AcoesDaAtividade` do caso; o `DespachanteCombinado` responde a carga da ficha e as listas dos dois componentes (`Combinar(...)`). A Tarefa 11 acrescenta testes a esta classe e usa esses dois auxiliares com estas assinaturas.

```csharp
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Documentos;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Documentos;
using AtividadesJuridicas.Web.Components.Atividades;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

public class AtividadeFichaInterativaTests : TelaInterativa
{
    private static readonly Guid Id = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a7");

    private static readonly AcoesDaAtividade SemAcoes = new(false, false, false, false, false, false, false);

    /// <summary>A atividade do quadro: Feita, com uma responsável que concluiu e um pendente, versão 7.</summary>
    private static AtividadeDetalhe Detalhe(
        StatusAtividade status = StatusAtividade.Feita, AcoesDaAtividade? acoes = null, IReadOnlyList<HistoricoDetalhe>? historico = null) => new(
        Id, "Minuta de acordo trabalhista", "Redigir a minuta de acordo.", new DateOnly(2026, 10, 9), status, false, null,
        Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000c1"), "Transportes Litoral Ltda.",
        Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000f1"), "Litoral — reclamação trabalhista",
        Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000e1"), "Trabalhista",
        TipoAutor.Usuario, "Marina Couto", AtorDaDiretoria.Id, AtorDaDiretoria.Nome,
        [
            new ResponsavelDetalhe(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b1"), "Beatriz Lins", true, "Redigir a minuta.",
                new DateTime(2026, 10, 7, 19, 42, 0, DateTimeKind.Utc)),
            new ResponsavelDetalhe(Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000b2"), "Otávio Brandão", false, null, null),
        ],
        historico ?? new[] { new HistoricoDetalhe(null, StatusAtividade.Criada, "Marina Couto", new DateTime(2026, 10, 1, 13, 2, 0, DateTimeKind.Utc), null) },
        acoes ?? SemAcoes, 7, 2);

    /// <summary>O perfil, a atividade e as listas dos comentários e dos documentos (com quantos de cada).</summary>
    private void Combinar(AtividadeDetalhe? detalhe = null, UsuarioAutenticado? ator = null, int comentarios = 0, int documentos = 0)
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(ator ?? AtorDaDiretoria)
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(Resultado<AtividadeDetalhe>.Ok(detalhe ?? Detalhe()))
            .Responde<ListarComentariosDaAtividade, Resultado<IReadOnlyList<ComentarioDetalhe>>>(Resultado<IReadOnlyList<ComentarioDetalhe>>.Ok(
                [.. Enumerable.Range(1, comentarios).Select(i => new ComentarioDetalhe(
                    Guid.NewGuid(), Guid.NewGuid(), "Beatriz Lins", $"Comentário {i}", new DateTime(2026, 10, 7, 19, 45, 0, DateTimeKind.Utc)))]))
            .Responde<ListarDocumentosDaAtividade, Resultado<Pagina<DocumentoResumo>>>(Resultado<Pagina<DocumentoResumo>>.Ok(new(
                [.. Enumerable.Range(1, documentos).Select(i => new DocumentoResumo(
                    Guid.NewGuid(), $"documento-{i}.docx", $"documento-{i}.docx", 84 * 1024, CategoriaDocumento.DocumentoAtividade,
                    new DateTime(2026, 10, 7, 19, 40, 0, DateTimeKind.Utc), "Beatriz Lins", Id, "Minuta de acordo trabalhista"))],
                documentos, 1, 20)));
    }

    private IRenderedComponent<AtividadeFicha> Ficha() => Desenhar<AtividadeFicha>(p => p.Add(f => f.Id, Id));

    [Fact]
    public void Tocar_numa_aba_mostra_o_painel_dela_e_esconde_os_outros_no_mobile()
    {
        Combinar();
        var ficha = Ficha();
        ficha.Find(".aj-abas button[aria-selected=true]").TextContent.Should().Be("Detalhes");

        ficha.FindAll(".aj-abas button").Single(b => b.TextContent == "Histórico").Click();

        ficha.Find(".aj-abas button[aria-selected=true]").TextContent.Should().Be("Histórico");
        ficha.Markup.Should()
            .MatchRegex("<div class=\"aj-aba-painel\">\\s*<section class=\"aj-painel aj-painel--ficha aj-painel--historico\">\\s*<h2>Histórico</h2>")
            .And.MatchRegex("<div class=\"aj-aba-painel aj-aba-painel--oculta\">\\s*<div class=\"aj-grade\">")
            .And.MatchRegex("<div class=\"aj-aba-painel aj-aba-painel--oculta\">\\s*<section class=\"aj-painel[^\"]*\">\\s*<h2>Comentários</h2>")
            .And.MatchRegex("<div class=\"aj-aba-painel aj-aba-painel--oculta\">\\s*<section class=\"aj-painel[^\"]*\">\\s*<div class=\"aj-painel__cabecalho\">\\s*<h2>Documentos</h2>");
    }

    [Fact]
    public void Comentarios_e_documentos_aparecem_uma_vez_so_e_contam_nas_abas()
    {
        Combinar(comentarios: 2, documentos: 3);

        var ficha = Ficha();

        ficha.FindComponents<ComentariosDaAtividade>().Should().ContainSingle();
        ficha.FindComponents<DocumentosDaAtividade>().Should().ContainSingle();
        ficha.WaitForAssertion(() => ficha.FindAll(".aj-abas button").Select(b => b.TextContent).Should()
            .Equal("Detalhes", "Comentários2", "Documentos3", "Histórico"));
    }

    [Fact]
    public void Aba_fica_sem_contador_quando_a_lista_dela_falha()
    {
        Combinar(documentos: 3);
        Despachante.Responde<ListarComentariosDaAtividade, Resultado<IReadOnlyList<ComentarioDetalhe>>>(
            Resultado<IReadOnlyList<ComentarioDetalhe>>.Falha(TipoErro.Validacao, "Falha de teste."));

        var ficha = Ficha();

        ficha.WaitForAssertion(() => ficha.FindAll(".aj-abas button").Select(b => b.TextContent).Should()
            .Equal("Detalhes", "Comentários", "Documentos3", "Histórico"));
    }

    [Fact]
    public void Historico_traz_a_mudanca_o_autor_o_motivo_e_a_cor_da_situacao_nova()
    {
        Combinar(Detalhe(StatusAtividade.ParaCorrecao, historico:
        [
            new(null, StatusAtividade.Criada, null, new DateTime(2026, 10, 1, 13, 2, 0, DateTimeKind.Utc), null),
            new(StatusAtividade.Criada, StatusAtividade.Direcionada, "Marina Couto", new DateTime(2026, 10, 1, 13, 20, 0, DateTimeKind.Utc), null),
            new(StatusAtividade.Direcionada, StatusAtividade.Feita, "Otávio Brandão", new DateTime(2026, 10, 8, 12, 15, 0, DateTimeKind.Utc), null),
            new(StatusAtividade.Feita, StatusAtividade.ParaCorrecao, "Marina Couto", new DateTime(2026, 10, 8, 14, 0, 0, DateTimeKind.Utc),
                "Conferir os valores da cláusula terceira."),
        ]));

        var ficha = Ficha();

        // O desktop vem antes na marcação; o tablet e o mobile repetem o mesmo painel.
        ficha.FindAll(".aj-so-desktop .aj-historico__mudanca").Select(e => e.TextContent).Should()
            .Equal("Criada", "Criada → Direcionada", "Direcionada → Feita", "Feita → Para correção");
        ficha.FindAll(".aj-so-desktop .aj-historico__autor").Select(e => e.TextContent).Should()
            .Equal("Sistema · 01/10/2026 10:02", "Marina Couto · 01/10/2026 10:20", "Otávio Brandão · 08/10/2026 09:15",
                "Marina Couto · 08/10/2026 11:00");
        var itens = ficha.FindAll(".aj-so-desktop .aj-historico__item");
        itens.Select(i => i.QuerySelector(".aj-historico__motivo")?.TextContent).Should()
            .Equal(null, null, null, "Conferir os valores da cláusula terceira.");
        ficha.FindAll(".aj-so-desktop .aj-historico__ponto").Select(p => p.ClassName).Should().Equal(
            "aj-historico__ponto",
            "aj-historico__ponto aj-historico__ponto--direcionada",
            "aj-historico__ponto aj-historico__ponto--feita",
            "aj-historico__ponto aj-historico__ponto--correcao aj-historico__ponto--atual");
        ficha.FindAll(".aj-so-desktop .aj-historico__linha").Should().HaveCount(3);
    }

    [Fact]
    public void Responsaveis_trazem_a_parte_concluida_com_as_duas_datas_e_o_pendente_sem_icone()
    {
        Combinar();

        var ficha = Ficha();

        var beatriz = ficha.FindAll(".aj-so-desktop .aj-responsavel")[0];
        beatriz.QuerySelector(".aj-avatar")!.TextContent.Should().Be("BL");
        beatriz.QuerySelector(".aj-avatar")!.ClassName.Should().Be($"aj-avatar {AvatarDePessoa.Classe("Beatriz Lins")}");
        beatriz.QuerySelector(".aj-responsavel__estado svg").Should().NotBeNull();
        beatriz.QuerySelector(".aj-responsavel__estado .aj-so-desktop")!.TextContent.Should().Be("Concluída em 07/10/2026 16:42");
        beatriz.QuerySelector(".aj-responsavel__estado .aj-tablet-mobile")!.TextContent.Should().Be("Concluída em 07/10 16:42");
        beatriz.QuerySelector(".aj-responsavel__direcionamento")!.TextContent.Should().Be("Redigir a minuta.");
        // O direcionamento de outra pessoa não chega do servidor: sem ele, a linha não aparece.
        var otavio = ficha.FindAll(".aj-so-desktop .aj-responsavel")[1];
        otavio.QuerySelector(".aj-responsavel__estado--pendente")!.TextContent.Should().Be("Pendente");
        otavio.QuerySelector(".aj-responsavel__estado svg").Should().BeNull();
        otavio.QuerySelector(".aj-responsavel__direcionamento").Should().BeNull();
    }

    [Fact]
    public void Recado_de_outra_tela_aparece_uma_vez_no_topo()
    {
        Combinar();
        Services.GetRequiredService<Recados>().Deixar("Responsáveis salvos.");

        var ficha = Ficha();

        ficha.Find("p.aj-aviso--sucesso").TextContent.Should().Be("Responsáveis salvos.");
        Services.GetRequiredService<Recados>().Retirar().Should().BeNull();
    }

    [Fact]
    public void Nao_encontrada_mostra_o_voltar_sem_paineis_e_nao_consulta_comentarios_nem_documentos()
    {
        Combinar();
        Despachante.Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(
            Resultado<AtividadeDetalhe>.Falha(TipoErro.NaoEncontrado, "Atividade não encontrada."));

        var ficha = Ficha();

        ficha.Markup.Should().Contain("Atividade não encontrada.").And.Contain("href=\"/atividades\"");
        ficha.FindAll(".aj-abas").Should().BeEmpty();
        ficha.FindComponents<ComentariosDaAtividade>().Should().BeEmpty();
        Despachante.Recebidas.OfType<ListarComentariosDaAtividade>().Should().BeEmpty();
        Despachante.Recebidas.OfType<ListarDocumentosDaAtividade>().Should().BeEmpty();
    }

    [Fact]
    public void Falha_na_consulta_mostra_o_erro_e_tentar_de_novo_traz_a_ficha()
    {
        var tentativas = 0;
        Combinar();
        Despachante.Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(_ => ++tentativas == 1
            ? Resultado<AtividadeDetalhe>.Falha(TipoErro.Validacao, "Falha de teste.")
            : Resultado<AtividadeDetalhe>.Ok(Detalhe()));
        var ficha = Ficha();
        ficha.Markup.Should().Contain("Falha de teste.");
        ficha.FindAll(".aj-abas").Should().BeEmpty();
        Despachante.Recebidas.OfType<ListarComentariosDaAtividade>().Should().BeEmpty();

        ficha.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        ficha.WaitForAssertion(() => ficha.Find("h1.aj-titulo").TextContent.Should().Be("Minuta de acordo trabalhista"));
        ficha.Markup.Should().NotContain("Falha de teste.");
    }

    [Fact]
    public void Comentarios_que_descobrem_a_atividade_sumida_levam_a_ficha_ao_nao_encontrada()
    {
        var leituras = 0;
        Combinar();
        Despachante
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(_ => ++leituras == 1
                ? Resultado<AtividadeDetalhe>.Ok(Detalhe())
                : Resultado<AtividadeDetalhe>.Falha(TipoErro.NaoEncontrado, "Atividade não encontrada."))
            .Responde<ListarComentariosDaAtividade, Resultado<IReadOnlyList<ComentarioDetalhe>>>(
                Resultado<IReadOnlyList<ComentarioDetalhe>>.Falha(TipoErro.NaoEncontrado, "Atividade não encontrada."));

        var ficha = Ficha();

        ficha.WaitForAssertion(() => ficha.FindAll(".aj-abas").Should().BeEmpty());
        ficha.Markup.Should().Contain("Atividade não encontrada.");
        Despachante.Recebidas.OfType<ObterAtividade>().Should().HaveCount(2);
    }

    [Fact]
    public void Acesso_negado_num_componente_encerra_a_tela()
    {
        Combinar();
        Despachante.Responde<ListarComentariosDaAtividade, Resultado<IReadOnlyList<ComentarioDetalhe>>>(
            Resultado<IReadOnlyList<ComentarioDetalhe>>.Falha(TipoErro.AcessoNegado, "Acesso negado."));

        var ficha = Ficha();

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Seu acesso foi encerrado. Entre novamente."));
        ficha.Markup.Should().NotContain("Minuta de acordo trabalhista");
    }
}
```

(As horas: 19:42 UTC é 16:42 em São Paulo. O `TextContent` do botão da aba junta o rótulo e o contador, que estão colados.)

- [ ] **Step 3: O link de Produtores no teste (deve falhar)**

Em `ProdutoresWebTests.Painel_abre_no_mes_corrente_com_os_totais_e_as_atividades_de_cada_complexidade`, guarde o id da primeira atividade (`var peticao = await ConcluidaAgoraAsync("Petição simples", 1);`) e troque a linha `html.Should().Contain("Concluída em").And.Contain("Abrir");` por:

```csharp
        // "Abrir" (desktop) e a linha (tablet e mobile) levam à ficha, que volta ao painel do produtor.
        var ficha = $"/atividades/{peticao}?de=produtores&produtor={_pedro}";
        BlocoDeVisao.De(html, "aj-so-desktop").Should()
            .Contain($"<a href=\"{ficha}\" class=\"aj-botao aj-botao--fantasma aj-botao--seta\">Abrir<svg");
        BlocoDeVisao.De(html, "aj-tablet-mobile").Should()
            .Contain($"<a href=\"{ficha}\" class=\"aj-linha aj-linha--link\"><span class=\"aj-linha__texto\"><span class=\"aj-linha__titulo\">Petição simples</span>");
        html.Should().Contain("Concluída em").And.NotContain("aj-linha--botao")
            .And.NotContain("<button type=\"button\" class=\"aj-botao aj-botao--fantasma aj-botao--seta\"");
```

A asserção antiga provava que cada atividade tem como ser aberta; a nova prova o mesmo pelo destino do link (o `&` vem decodificado pelo `HtmlDecode` do auxiliar `PainelAsync`). Liste a reescrita no relatório.

- [ ] **Step 4: Rodar (RED)**

Run: `dotnet build -m:1`
Expected: não compila (`AtividadeFicha` e `CabecalhoDaAtividade` não existem). É o RED.

- [ ] **Step 5: `CabecalhoDaAtividade`**

Crie `src/AtividadesJuridicas.Web/Components/Atividades/CabecalhoDaAtividade.razor`:

```razor
@* Cabeçalho da ficha da atividade (quadros FichaDaAtividade, FichaTablet e FichaMobile). No desktop e no tablet, o voltar
   acima de um painel com a linha de situação (etiqueta e entrega), o título e, embaixo, a faixa de ações. No mobile, a
   barra de voltar com "Atividade" e, à direita, a ação da barra; no conteúdo, a linha de situação com a entrega curta e o
   título em h2 (o h1 é o da barra). Os dois encaixes ficam vazios quando a tela não tem o que pôr neles. *@
<BarraDeVoltar Titulo="Atividade" VoltarPara="@VoltarPara" Rotulo="@(RotuloDoVoltarNoMobile ?? $"Voltar para {Origem}")" Acao="@AcaoDaBarra" />

<div class="aj-desktop-tablet">
    <div><a href="@VoltarPara" class="aj-voltar"><Icone Nome="voltar" />@Origem</a></div>
    <header class="aj-painel aj-painel--principal aj-painel--cabecalho-com-faixa">
        <div class="aj-titulo-da-ficha aj-titulo-da-ficha--com-situacao">
            <div class="aj-titulo-da-ficha__situacao"><EtiquetaDeSituacao Status="Detalhe.Status" Atrasada="Detalhe.Atrasada" /><span class="aj-titulo-da-ficha__entrega">@(TextosDeAtividade.Entrega(Detalhe.DataEntrega, Detalhe.AreaNome, curta: false))</span></div>
            <h1 class="aj-titulo">@Detalhe.Titulo</h1>
        </div>
        @if (Faixa is not null)
        {
            <div class="aj-painel__botoes aj-painel__botoes--faixa">@Faixa</div>
        }
    </header>
</div>

<div class="aj-so-mobile">
    <div class="aj-titulo-da-ficha aj-titulo-da-ficha--com-situacao">
        <div class="aj-titulo-da-ficha__situacao"><EtiquetaDeSituacao Status="Detalhe.Status" Atrasada="Detalhe.Atrasada" /><span class="aj-titulo-da-ficha__entrega">@(TextosDeAtividade.Entrega(Detalhe.DataEntrega, Detalhe.AreaNome, curta: true))</span></div>
        <h2 class="aj-titulo">@Detalhe.Titulo</h2>
    </div>
</div>

@code {
    [Parameter, EditorRequired] public AtividadeDetalhe Detalhe { get; set; } = default!;

    [Parameter, EditorRequired] public string VoltarPara { get; set; } = string.Empty;

    /// <summary>O nome da tela de destino do voltar: "Atividades" ou "Produtores".</summary>
    [Parameter, EditorRequired] public string Origem { get; set; } = string.Empty;

    /// <summary>O rótulo do voltar da barra do mobile; nulo = "Voltar para {Origem}".</summary>
    [Parameter] public string? RotuloDoVoltarNoMobile { get; set; }

    /// <summary>A faixa de ações do desktop e do tablet, abaixo do título. Nula: sem faixa (e sem a borda dela).</summary>
    [Parameter] public RenderFragment? Faixa { get; set; }

    /// <summary>A ação à direita da barra de voltar do mobile (o "Mais ações"). Nula: o vazio do tamanho do botão.</summary>
    [Parameter] public RenderFragment? AcaoDaBarra { get; set; }
}
```

- [ ] **Step 6: A tela `AtividadeFicha`**

Crie `src/AtividadesJuridicas.Web/Components/Pages/AtividadeFicha.razor`:

```razor
@page "/atividades/{Id:guid}"
@using System.Globalization
@inject IDespachante Despachante
@inject FusoDoEscritorio Fuso
@inject Recados Recados

<PageTitle>@(_detalhe?.Titulo ?? "Atividade")</PageTitle>

@if (!_carregado || _ator is null || _detalhe is null)
{
    <CabecalhoDeDetalhe Titulo="Atividade" VoltarPara="@Voltar" Origem="@Origem" />
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
        @* A atividade foi excluída ou o usuário não a vê: o servidor responde igual nos dois casos. *@
        <Aviso Tipo="TipoDeAviso.Erro">Atividade não encontrada.</Aviso>
    }
    else
    {
        @* Falha ao consultar nunca vira uma ficha vazia nem deixa dados antigos na tela. *@
        <EstadoDaLista Erro="@(_erro ?? "Não foi possível consultar a atividade.")" TextoDeVazio="" TentarDeNovo="CarregarAsync" />
    }
}
else
{
    var a = _detalhe;
    <CabecalhoDaAtividade Detalhe="a" VoltarPara="@Voltar" Origem="@Origem" />

    @if (_recado is { } recado)
    {
        <Aviso Tipo="@recado.Tipo">@recado.Texto</Aviso>
    }

    <div class="aj-so-mobile">
        <Abas Rotulo="Seções da atividade" Itens="@AbasDaFicha" Ativa="@_aba" AoEscolher="(string aba) => _aba = aba" />
    </div>

    @* Cada painel com estado aparece uma vez só. O painel Detalhes vem primeiro e o CSS o leva para a direita no desktop;
       os blocos que mudam por visão ficam em invólucros de visão; no mobile, as abas escondem o que não é da aba escolhida. *@
    <div class="aj-lado-a-lado aj-lado-a-lado--painel-primeiro aj-lado-a-lado--ficha aj-lado-a-lado--painel-estreito">
        <div class="aj-so-desktop">
            <aside class="aj-painel aj-painel--ficha aj-painel--detalhes aj-lado-a-lado__painel" aria-label="Dados da atividade">
                <h2>Detalhes</h2>
                @Pares(DetalhesDoDesktop(a))
            </aside>
        </div>

        <div class="aj-lado-a-lado__lista aj-pilha">
            <div class="aj-so-desktop">
                @PainelDeDescricao(a)
                @PainelDeResponsaveis(a)
                @PainelDeHistorico(a)
            </div>
            <div class="aj-tablet-mobile">
                <div class="@ClasseDaAba("detalhes")">
                    <div class="aj-grade">
                        @PainelDeDescricao(a)
                        <section class="aj-painel aj-painel--ficha aj-painel--detalhes">
                            <h2>Dados</h2>
                            @Pares(DadosDoTabletEMobile(a))
                        </section>
                    </div>
                    @PainelDeResponsaveis(a)
                </div>
                <div class="@ClasseDaAba("historico")">@PainelDeHistorico(a)</div>
            </div>
            @* A chave é a atividade: abrir outra ficha nesta mesma tela recria os dois componentes. *@
            <div class="@ClasseDaAba("comentarios")">
                <ComentariosDaAtividade @key="a.Id" AtividadeId="a.Id" AoContar="ContarComentarios"
                                        OnAcessoEncerrado="EncerrarAcesso" OnAtividadeNaoEncontrada="AtividadeSumiuAsync" />
            </div>
            <div class="@ClasseDaAba("documentos")">
                @* Gerenciar documentos (renomear e excluir) é permissão da Diretoria, que hoje coincide com poder editar a
                   atividade; se as duas regras se separarem, esta linha precisa de uma ação própria. *@
                <DocumentosDaAtividade @key="a.Id" AtividadeId="a.Id" PodeGerenciar="a.Acoes.Editar" AoContar="ContarDocumentos"
                                       OnAcessoEncerrado="EncerrarAcesso" OnAtividadeNaoEncontrada="AtividadeSumiuAsync" />
            </div>
        </div>
    </div>
}

@code {
    [Parameter] public Guid Id { get; set; }

    /// <summary>De onde se veio (<c>?de=produtores</c>): decide o voltar.</summary>
    [SupplyParameterFromQuery(Name = "de")] public string? De { get; set; }

    /// <summary>O produtor do painel de onde se veio (<c>?produtor=</c>). Texto que não é um id volta a <c>/produtores</c>.</summary>
    [SupplyParameterFromQuery(Name = "produtor")] public string? Produtor { get; set; }

    private bool _carregado;
    private UsuarioAutenticado? _ator;
    private AtividadeDetalhe? _detalhe;
    private bool _naoEncontrado;
    private string? _erro;
    private int? _totalDeComentarios;
    private int? _totalDeDocumentos;
    private string _aba = "detalhes";
    private Recado? _recado;
    private Guid? _idCarregado;

    private bool DeProdutores => string.Equals(De, "produtores", StringComparison.Ordinal);

    private string Voltar => !DeProdutores
        ? "/atividades"
        : Guid.TryParse(Produtor, out var produtor) ? $"/produtores?produtor={produtor}" : "/produtores";

    private string Origem => DeProdutores ? "Produtores" : "Atividades";

    protected override async Task OnParametersSetAsync()
    {
        if (_idCarregado == Id) return;

        var primeira = _idCarregado is null;
        _idCarregado = Id;
        if (primeira) _recado = Recados.Retirar();
        await CarregarAsync();
    }

    /// <summary>A carga inteira: o perfil e a atividade. Também é o "Tentar de novo".</summary>
    private async Task CarregarAsync()
    {
        _carregado = false;
        _detalhe = null;
        _naoEncontrado = false;
        _erro = null;
        _totalDeComentarios = null;
        _totalDeDocumentos = null;
        _aba = "detalhes";

        _ator = await Despachante.EnviarAsync(new ObterMeuAcesso());
        if (_ator is not null) await RecarregarAsync();

        _carregado = true;
    }

    /// <summary>
    /// Relê a atividade; devolve se a ficha ficou mostrando dados atuais. Acesso negado encerra a tela; não encontrada e
    /// falha tiram a ficha da tela (as ações dependem do que o servidor permite agora).
    /// </summary>
    private async Task<bool> RecarregarAsync()
    {
        var resultado = await Despachante.EnviarAsync(new ObterAtividade(Id));
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
            _erro = resultado.Mensagem ?? "Não foi possível consultar a atividade.";
            return false;
        }

        _detalhe = resultado.Valor;
        return true;
    }

    /// <summary>Um componente descobriu que a atividade não existe mais: a ficha relê e mostra o que o servidor diz.</summary>
    private Task AtividadeSumiuAsync() => RecarregarAsync();

    private void EncerrarAcesso()
    {
        _ator = null;
        _detalhe = null;
    }

    private void ContarComentarios(int total) => _totalDeComentarios = total;

    private void ContarDocumentos(int total) => _totalDeDocumentos = total;

    private IReadOnlyList<Aba> AbasDaFicha =>
    [
        new("detalhes", "Detalhes"),
        new("comentarios", "Comentários", _totalDeComentarios),
        new("documentos", "Documentos", _totalDeDocumentos),
        new("historico", "Histórico"),
    ];

    private string ClasseDaAba(string aba) => aba == _aba ? "aj-aba-painel" : "aj-aba-painel aj-aba-painel--oculta";

    private static string Ou(string? valor) => string.IsNullOrWhiteSpace(valor) ? "—" : valor;

    private static string Autor(AtividadeDetalhe a) => a.TipoAutor == TipoAutor.Sistema ? "Sistema" : Ou(a.AutorNome);

    private static string Complexidade(AtividadeDetalhe a) => a.Complexidade.ToString(CultureInfo.InvariantCulture);

    private static IReadOnlyList<(string Termo, string Valor)> DetalhesDoDesktop(AtividadeDetalhe a) =>
    [
        ("Entrega", RotulosDeAtividade.Data(a.DataEntrega)),
        ("Cliente", Ou(a.ClienteNome)),
        ("Processo", Ou(a.ProcessoTitulo)),
        ("Área", Ou(a.AreaNome)),
        ("Complexidade", Complexidade(a)),
        ("Autor", Autor(a)),
        ("Revisor", Ou(a.RevisorNome)),
    ];

    // No tablet e no mobile a entrega e a área já estão na linha de situação, acima do título.
    private static IReadOnlyList<(string Termo, string Valor)> DadosDoTabletEMobile(AtividadeDetalhe a) =>
    [
        ("Cliente", Ou(a.ClienteNome)),
        ("Processo", Ou(a.ProcessoTitulo)),
        ("Autor", Autor(a)),
        ("Revisor", Ou(a.RevisorNome)),
        ("Complexidade", Complexidade(a)),
    ];

    private RenderFragment Pares(IReadOnlyList<(string Termo, string Valor)> pares) => @<dl class="aj-detalhes">
        @foreach (var (termo, valor) in pares)
        {
            <div><dt>@termo</dt><dd>@valor</dd></div>
        }
    </dl>;

    private RenderFragment PainelDeDescricao(AtividadeDetalhe a) => @<section class="aj-painel aj-painel--ficha aj-painel--descricao">
        <h2>Descrição</h2>
        <p>@a.Descricao</p>
    </section>;

    private RenderFragment PainelDeResponsaveis(AtividadeDetalhe a) => @<section class="aj-painel aj-painel--ficha aj-painel--responsaveis">
        <h2>Responsáveis</h2>
        <EstadoDaLista Vazio="@(a.Responsaveis.Count == 0)" TextoDeVazio="Nenhum responsável.">
            <ul class="aj-responsaveis">
                @foreach (var r in a.Responsaveis)
                {
                    @* O direcionamento de outra pessoa nem chega do servidor; aqui só se mostra o que veio, e só se for visível. *@
                    <li class="aj-responsavel" @key="r.UsuarioId"><span class="@($"aj-avatar {AvatarDePessoa.Classe(r.Nome)}")" aria-hidden="true">@AvatarDePessoa.Sigla(r.Nome)</span><div class="aj-responsavel__texto"><div class="aj-responsavel__cabeca"><strong>@r.Nome</strong>@EstadoDaParte(r)</div>@if (r.DirecionamentoVisivel && !string.IsNullOrWhiteSpace(r.Direcionamento)) {<span class="aj-responsavel__direcionamento">@r.Direcionamento</span>}</div></li>
                }
            </ul>
        </EstadoDaLista>
    </section>;

    /// <summary>A parte do responsável: concluída, com o ícone e a data (completa no desktop, curta no tablet e no mobile), ou pendente.</summary>
    private RenderFragment EstadoDaParte(ResponsavelDetalhe r)
    {
        if (r.ConcluidoEm is not { } em)
            return @<span class="aj-responsavel__estado aj-responsavel__estado--pendente">Pendente</span>;

        return @<span class="aj-responsavel__estado"><Icone Nome="confirmar" /><span class="aj-so-desktop">@($"Concluída em {Fuso.Formatar(em)}")</span><span class="aj-tablet-mobile">@($"Concluída em {TextosDeAtividade.DataCurta(Fuso, em)}")</span></span>;
    }

    private RenderFragment PainelDeHistorico(AtividadeDetalhe a) => @<section class="aj-painel aj-painel--ficha aj-painel--historico">
        <h2>Histórico</h2>
        <EstadoDaLista Vazio="@(a.Historico.Count == 0)" TextoDeVazio="Nenhum registro.">
            <ol class="aj-historico">
                @for (var i = 0; i < a.Historico.Count; i++)
                {
                    var h = a.Historico[i];
                    var atual = i == a.Historico.Count - 1;
                    <li class="aj-historico__item"><div class="aj-historico__marca" aria-hidden="true"><span class="@ClasseDoPonto(h.StatusNovo, atual)"></span>@if (!atual) {<span class="aj-historico__linha"></span>}</div><div class="aj-historico__texto"><span class="aj-historico__mudanca">@TextosDeAtividade.Mudanca(h.StatusAnterior, h.StatusNovo)</span><span class="aj-historico__autor">@($"{h.UsuarioNome ?? "Sistema"} · {Fuso.Formatar(h.DataHora)}")</span>@if (!string.IsNullOrWhiteSpace(h.Motivo)) {<p class="aj-historico__motivo">@h.Motivo</p>}</div></li>
                }
            </ol>
        </EstadoDaLista>
    </section>;

    /// <summary>O ponto da linha do tempo na cor da situação nova (Criada é o ponto sem modificador); o atual, cheio.</summary>
    private static string ClasseDoPonto(StatusAtividade situacao, bool atual)
    {
        var classe = situacao == StatusAtividade.Criada
            ? "aj-historico__ponto"
            : $"aj-historico__ponto aj-historico__ponto--{RotulosDeAtividade.ClasseDaEtiqueta(situacao)}";
        return atual ? $"{classe} aj-historico__ponto--atual" : classe;
    }
}
```

Notas para quem implementa:

- O texto que um teste procura sai de uma expressão só (`@($"Concluída em …")`, `@($"{nome} · {data}")`) ou de marcação fixa; as marcas que os testes procuram coladas (`<li>` do responsável, `<li>` do histórico, a linha de situação) ficam numa linha só, sem espaço entre elas.
- Os painéis sem estado (Descrição, Responsáveis, Histórico) são desenhados por métodos que devolvem `RenderFragment` e aparecem duas vezes (o bloco do desktop e o do tablet e do mobile); os componentes com estado (`ComentariosDaAtividade`, `DocumentosDaAtividade`) aparecem uma vez só, e só com a ficha carregada: no não encontrado, no erro e no acesso encerrado eles nem existem, logo não consultam (Review Focus 3).
- `RecarregarAsync()` e `_detalhe` são o que a Tarefa 11 usa: não mude a assinatura (`Task<bool>`) nem o nome.
- Se o Razor não aceitar o `@if (…) {<span>…</span>}` dentro da linha, quebre-o em linhas, mantendo as marcas que os testes procuram coladas (`</span><span class="aj-historico__linha">` e `</span></div>` do ponto atual).

- [ ] **Step 7: Produtores leva à ficha por link e a ficha antiga sai**

Em `src/AtividadesJuridicas.Web/Components/Produtores/PainelDeProdutividade.razor`:

1. A célula de ação do desktop vira link:

```razor
<td class="aj-tabela__acao"><a href="@Ficha(atividade.Id)" class="aj-botao aj-botao--fantasma aj-botao--seta">Abrir<Icone Nome="avancar" /></a></td>
```

2. A linha do tablet e do mobile vira link:

```razor
<a href="@Ficha(atividade.Id)" class="aj-linha aj-linha--link" @key="atividade.Id"><span class="aj-linha__texto"><span class="aj-linha__titulo">@atividade.Titulo</span><span class="aj-ajuda">@Detalhe(atividade)</span></span><Icone Nome="avancar" Classe="aj-cartao__seta" /></a>
```

3. Saem o bloco `@if (_atividadeAberta is { } id) { … <FichaDaAtividade … /> }` (com o comentário dele), o campo `_atividadeAberta`, os métodos `Abrir`, `FecharFicha` e `RecarregarAsync` (que só a ficha embutida chamava), a linha `_atividadeAberta = null;` de `MudarMesAsync` (com o comentário dela) e a de `EncerrarAcessoAsync`.

4. Entra o endereço da ficha:

```csharp
    /// <summary>A ficha da atividade, que volta a este painel (o mês volta ao corrente: ele não está no endereço).</summary>
    private string Ficha(Guid atividadeId) => $"/atividades/{atividadeId}?de=produtores&produtor={ProdutorId}";
```

Depois, confira que nada mais usa a ficha antiga (a lista deixou de usá-la na Tarefa 6) e apague-a:

```powershell
git grep -n "FichaDaAtividade" -- src tests
git rm src/AtividadesJuridicas.Web/Components/Atividades/FichaDaAtividade.razor
```

Expected do `git grep`: só `PainelDeProdutividade.razor` antes da edição acima; nada depois. Se a `Atividades.razor` ainda a citar, pare e relate (a Tarefa 6 devia tê-la tirado).

- [ ] **Step 8: Trava e testes**

Acrescente a `Migradas` (em `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`), `"Components/Atividades/CabecalhoDaAtividade.razor",` antes de `"Components/Atividades/ComentariosDaAtividade.razor",` e `"Components/Pages/AtividadeFicha.razor",` logo depois de `"Components/Pages/Atividades.razor",`.

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadeFicha|FullyQualifiedName~ProdutoresWebTests|FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~AutenticacaoWebTests|FullyQualifiedName~ComentariosDaAtividade|FullyQualifiedName~DocumentosDaAtividade"`
Expected: 0 avisos; todos passando.

- [ ] **Step 9: Commit**

```powershell
git branch --show-current
git add src tests
git commit -m "feat(web): ficha da atividade em tela própria, com abas do mobile, histórico em linha do tempo e o voltar de Produtores"
```

Expected do `git branch --show-current`: `worktree-feat+fase-9e-atividades`.

---

### Task 11: Ações da ficha da atividade

**Files:**
- Modify: `src/AtividadesJuridicas.Web/Components/Pages/AtividadeFicha.razor`
- Modify: `tests/AtividadesJuridicas.IntegrationTests/AtividadeFichaWebTests.cs`, `AtividadeFichaInterativaTests.cs`

**Interfaces:**
- Consumes: `CabecalhoDaAtividade.Faixa`/`AcaoDaBarra` (Tarefa 10), `MenuDeAcoes`, `Aviso` (confirmações com `Rotulo`, `Titulo` e `Acoes`), `Recados`; `ConcluirMinhaParte`, `AprovarAtividade`, `ExcluirAtividade` (com a versão da carga).
- Produces: os links para `/atividades/{id}/editar`, `/responsaveis`, `/devolver` e `/reabrir`, que as Tarefas 7, 12 e 13 atendem.

**Regras:**
- Faixa (desktop e tablet, `aj-painel__botoes aj-painel__botoes--faixa`), na ordem da seção "Decisões": `Concluir minha parte` (primário, `confirmar`) ou `Aprovar` (primário, `confirmar`) ou `Reabrir` (link secundário para `/reabrir`); `Devolver` (link secundário com `devolver`, para `/devolver`); `aj-painel__divisor` quando há ação dos dois lados (só no desktop, num invólucro `aj-so-desktop`: o quadro do tablet não tem divisor); `Editar`, `Responsáveis`, `Trocar revisor` (links fantasma); `Excluir` (`aj-botao--perigo aj-botao--ao-fim`, no desktop com texto e ícone, no tablet só ícone com `aria-label="Excluir atividade"`, em invólucros de visão). Cada uma com a sua permissão de `Acoes`; "Trocar revisor" só com `TrocarRevisor` e sem `DefinirResponsaveis`, e leva a `/atividades/{id}/responsaveis`.
- Mobile: `AcaoDaBarra` com o `MenuDeAcoes` `Mais ações` (Editar, Responsáveis, Trocar revisor, Excluir em perigo; sem item, sem o menu); barra fixa `aj-so-mobile` > `aj-acoes aj-acoes--fixa` com Devolver e a ação primária (nessa ordem, a do quadro); sem ação, sem barra.
- Confirmações (textos da seção "Global Constraints"): `Aviso` de atenção no topo, uma por vez, com `@key`; "Cancelar" fecha. Concluir e Aprovar: sucesso recarrega a ficha e mostra o aviso de sucesso (`Sua parte foi concluída.`, `Atividade aprovada.`; se a recarga falhar, a mensagem sem o ponto final seguida de `, mas não foi possível atualizar a ficha.`); Excluir: `Recados.Deixar("Atividade excluída.")` e navega **sempre** para `/atividades`, também vindo de Produtores (que não lê recados; teste com `de=produtores`). Falha: o aviso de erro e a ficha recarregada; acesso negado encerra a tela. Guarda de clique duplo, com o botão travado depois do sucesso de Excluir até a navegação.
- Abrir uma tela de ação não relê a atividade aqui: a tela de destino confere a permissão (Tarefas 12 e 13).
- Testes HTTP: cada ação aparece só com a permissão (Diretoria revisora em Feita; Diretoria não revisora; Produção responsável com parte pendente; Concluída com Reabrir), com afirmação positiva no mesmo bloco; o "Mais ações" e a barra fixa do mobile. Testes bUnit: confirmar concluir, aprovar e excluir (envio, aviso ou navegação com recado, também vindo de Produtores); cancelar; clique duplo (Review Focus 4: em Aprovar e Concluir, um envio só com os botões travados enquanto espera; em Excluir, travado até a navegação); falha mostra o erro e recarrega.

Casos de permissão que os testes cobrem (as ações vêm de `ObterAtividade.Acoes`; "Trocar revisor" só com `TrocarRevisor` e sem `DefinirResponsaveis`; o divisor só com ação dos dois lados, e só no desktop, como no quadro; a barra fixa do mobile traz Devolver e a ação primária, nessa ordem; o "Mais ações" traz Editar, Responsáveis, Trocar revisor e Excluir, nessa ordem):

| Caso | Quem vê, em que situação | Faixa (desktop e tablet), na ordem | Divisor | "Mais ações" (mobile) | Barra fixa (mobile) |
|---|---|---|---|---|---|
| A | Diretoria revisora, Feita | Aprovar, Devolver, Editar, Trocar revisor, Excluir | sim | Editar, Trocar revisor, Excluir | Devolver, Aprovar |
| B | Diretoria não revisora, Direcionada | Editar, Responsáveis, Excluir | não | Editar, Responsáveis, Excluir | — |
| C | Produção responsável com a parte pendente, Direcionada | Concluir minha parte | não | — | Concluir minha parte |
| D | Diretoria, Concluída | Reabrir, Editar, Excluir | sim | Editar, Excluir | Reabrir |
| E | Produção responsável com a parte concluída, Feita | — (sem faixa) | — | — | — |
| F | Diretoria não revisora, Feita | Editar, Trocar revisor, Excluir | não | Editar, Trocar revisor, Excluir | — |

Os casos A a E vão pelo banco (teste HTTP, a atividade criada pelos comandos do despachante real via `AplicacaoDeTeste.ExecutarAsync`); todos, mais F e combinações de borda, vão pelo bUnit (um `AtividadeDetalhe` montado com as `AcoesDaAtividade` do caso, respondido pelo `DespachanteCombinado`).

- [ ] **Step 1: Testes HTTP das ações (devem falhar)**

Em `tests/AtividadesJuridicas.IntegrationTests/AtividadeFichaWebTests.cs` (criado na Tarefa 10: `_admin`, `_pedro`, `_minuta` — Feita, revisora a Diretoria, o Pedro com a parte concluída — e o auxiliar `PaginaAsync(rota, comoPedro)`), acrescente `using AtividadesJuridicas.Application.Identidade;` e, no fim da classe:

```csharp
    // ---- Ações da ficha ----

    /// <summary>Marina Couto, uma segunda diretora (para a atividade em que a Diretoria logada não é a revisora).</summary>
    private async Task<Guid> MarinaAsync()
    {
        var marina = await _app.ExecutarAsync(_admin, new CriarUsuario(
            "Marina Couto", "marina@x.com", "senha-da-marina", await _app.IdDoPerfilDiretorAsync(), null));
        marina.Sucesso.Should().BeTrue(marina.Mensagem);
        return marina.Valor;
    }

    /// <summary>"Parecer sobre o acordo", direcionada ao Pedro (parte pendente), com o revisor informado.</summary>
    private async Task<Guid> DirecionadaAoPedroAsync(Guid revisor)
    {
        var criada = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, "Parecer sobre o acordo", "Analisar a proposta.", new DateOnly(2090, 6, 15), null, null, null, 1));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        (await _app.ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
            criada.Valor, [new ResponsavelEntrada(_pedro, null)], revisor))).Sucesso.Should().BeTrue();
        return criada.Valor;
    }

    /// <summary>Direcionada ao Pedro com a Diretoria revisora; o Pedro conclui e a Diretoria aprova.</summary>
    private async Task<Guid> ConcluidaAsync()
    {
        var id = await DirecionadaAoPedroAsync(_admin);
        (await _app.ExecutarAsync(_pedro, new ConcluirMinhaParte(id))).Sucesso.Should().BeTrue();
        (await _app.ExecutarAsync(_admin, new AprovarAtividade(id))).Sucesso.Should().BeTrue();
        return id;
    }

    /// <summary>O miolo da faixa de ações do desktop e do tablet (vazio quando a ficha não tem faixa).</summary>
    private static string Faixa(string html) =>
        Regex.Match(html, "(?s)<div class=\"aj-painel__botoes aj-painel__botoes--faixa\">(.*?)</div>").Groups[1].Value;

    /// <summary>A barra de voltar do mobile, que o layout desenha no encaixe da barra superior.</summary>
    private static string BarraDoMobile(string html) =>
        Regex.Match(html, "(?s)<header class=\"aj-barra-superior aj-barra-superior--voltar\">.*?</header>").Value;

    private static string MaisAcoes(string html) =>
        Regex.Match(BarraDoMobile(html), "(?s)<div class=\"aj-menu-acoes__lista\"[^>]*>(.*?)</div>").Groups[1].Value;

    private static string BarraFixa(string html) =>
        Regex.Match(BlocoDeVisao.De(html, "aj-so-mobile"), "(?s)<div class=\"aj-acoes aj-acoes--fixa\">(.*?)</div>").Groups[1].Value;

    /// <summary>O texto de cada link e botão do trecho, na ordem; o botão só de ícone não tem texto e fica de fora.</summary>
    private static List<string> Textos(string trecho) =>
        [.. Regex.Matches(trecho, ">([^<>]+)</(?:a|button)>").Select(m => m.Groups[1].Value.Trim()).Where(t => t.Length > 0)];

    [Fact]
    public async Task Caso_A_diretoria_revisora_de_atividade_feita_ve_aprovar_devolver_e_o_cadastro_sem_responsaveis()
    {
        var html = await PaginaAsync($"/atividades/{_minuta}");

        var faixa = Faixa(html);
        Textos(faixa).Should().Equal("Aprovar", "Devolver", "Editar", "Trocar revisor", "Excluir");
        faixa.Should()
            .MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\"[^>]*><svg[^>]*>.*?</svg>Aprovar</button>")
            .And.MatchRegex($"<a href=\"/atividades/{_minuta}/devolver\" class=\"aj-botao aj-botao--secundario aj-botao--com-icone\"><svg[^>]*>.*?</svg>Devolver</a>")
            .And.Contain("<span class=\"aj-so-desktop\"><span class=\"aj-painel__divisor\" aria-hidden=\"true\"></span></span>")
            .And.Contain($"<a href=\"/atividades/{_minuta}/editar\" class=\"aj-botao aj-botao--fantasma\">Editar</a>")
            .And.Contain($"<a href=\"/atividades/{_minuta}/responsaveis\" class=\"aj-botao aj-botao--fantasma\">Trocar revisor</a>")
            .And.MatchRegex("<span class=\"aj-so-desktop\"><button type=\"button\" class=\"aj-botao aj-botao--perigo aj-botao--com-icone-menor aj-botao--ao-fim\"[^>]*><svg[^>]*>.*?</svg>Excluir</button></span>")
            .And.MatchRegex("<span class=\"aj-so-tablet\"><button type=\"button\" class=\"aj-botao aj-botao--perigo aj-botao--icone aj-botao--ao-fim\" aria-label=\"Excluir atividade\"[^>]*><svg[^>]*>.*?</svg></button></span>");
        // Em Feita não se redefinem os responsáveis: a troca do revisor fica no "Trocar revisor".
        faixa.Should().NotContain(">Responsáveis</a>").And.NotContain("Concluir minha parte").And.NotContain("Reabrir");

        BarraDoMobile(html).Should().Contain("aria-label=\"Mais ações\"").And.NotContain("aj-barra-superior__vazio");
        Textos(MaisAcoes(html)).Should().Equal("Editar", "Trocar revisor", "Excluir");
        MaisAcoes(html).Should().Contain("<button type=\"button\" class=\"aj-menu-acoes__item aj-menu-acoes__item--perigo\"");
        Textos(BarraFixa(html)).Should().Equal("Devolver", "Aprovar");
    }

    [Fact]
    public async Task Caso_B_diretoria_que_nao_e_revisora_ve_so_o_cadastro_com_responsaveis_e_sem_trocar_revisor()
    {
        var direcionada = await DirecionadaAoPedroAsync(await MarinaAsync());

        var html = await PaginaAsync($"/atividades/{direcionada}");

        var faixa = Faixa(html);
        Textos(faixa).Should().Equal("Editar", "Responsáveis", "Excluir");
        faixa.Should().Contain($"<a href=\"/atividades/{direcionada}/responsaveis\" class=\"aj-botao aj-botao--fantasma\">Responsáveis</a>");
        faixa.Should().NotContain("aj-painel__divisor").And.NotContain("Trocar revisor").And.NotContain("Aprovar");
        Textos(MaisAcoes(html)).Should().Equal("Editar", "Responsáveis", "Excluir");
        BlocoDeVisao.De(html, "aj-so-mobile").Should().Contain("<h2 class=\"aj-titulo\">Parecer sobre o acordo</h2>")
            .And.NotContain("aj-acoes--fixa");
    }

    [Fact]
    public async Task Caso_C_producao_com_a_parte_pendente_ve_so_concluir_minha_parte()
    {
        var direcionada = await DirecionadaAoPedroAsync(_admin);

        var html = await PaginaAsync($"/atividades/{direcionada}", comoPedro: true);

        var faixa = Faixa(html);
        Textos(faixa).Should().Equal("Concluir minha parte");
        faixa.Should().MatchRegex("<button type=\"button\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\"[^>]*><svg[^>]*>.*?</svg>Concluir minha parte</button>");
        faixa.Should().NotContain("aj-painel__divisor").And.NotContain("Excluir");
        BarraDoMobile(html).Should().Contain("aria-label=\"Voltar para Atividades\"").And.Contain("aj-barra-superior__vazio")
            .And.NotContain("Mais ações");
        Textos(BarraFixa(html)).Should().Equal("Concluir minha parte");
    }

    [Fact]
    public async Task Caso_D_atividade_concluida_oferece_reabrir_e_o_cadastro()
    {
        var concluida = await ConcluidaAsync();

        var html = await PaginaAsync($"/atividades/{concluida}");

        var faixa = Faixa(html);
        Textos(faixa).Should().Equal("Reabrir", "Editar", "Excluir");
        faixa.Should().Contain($"<a href=\"/atividades/{concluida}/reabrir\" class=\"aj-botao aj-botao--secundario\">Reabrir</a>")
            .And.Contain("aj-painel__divisor");
        faixa.Should().NotContain("Trocar revisor").And.NotContain("Aprovar").And.NotContain("Devolver");
        Textos(MaisAcoes(html)).Should().Equal("Editar", "Excluir");
        Textos(BarraFixa(html)).Should().Equal("Reabrir");
    }

    [Fact]
    public async Task Caso_E_producao_sem_acao_nao_tem_faixa_nem_mais_acoes_nem_barra_fixa()
    {
        var html = await PaginaAsync($"/atividades/{_minuta}", comoPedro: true);

        html.Should().Contain("<h1 class=\"aj-titulo\">Minuta de acordo trabalhista</h1>");
        BarraDoMobile(html).Should().Contain("aj-barra-superior__vazio");
        html.Should().NotContain("aj-painel__botoes--faixa").And.NotContain("Mais ações").And.NotContain("aj-acoes--fixa")
            .And.NotContain("/editar").And.NotContain("Excluir atividade");
    }
```

(A Marina é a terceira pessoa de Diretoria do banco do teste; a revisora da atividade do caso B é ela, e a Diretoria logada é a Diana.)

- [ ] **Step 2: Testes bUnit das ações (devem falhar)**

Em `tests/AtividadesJuridicas.IntegrationTests/AtividadeFichaInterativaTests.cs` (criado na Tarefa 10, com `Id`, `SemAcoes`, `Detalhe(StatusAtividade status = Feita, AcoesDaAtividade? acoes = null, IReadOnlyList<HistoricoDetalhe>? historico = null)` — título "Minuta de acordo trabalhista", versão 7 —, `Combinar(AtividadeDetalhe? detalhe = null, UsuarioAutenticado? ator = null, int comentarios = 0, int documentos = 0)` e `Ficha()`), acrescente `using AngleSharp.Dom;` e `using Microsoft.AspNetCore.Components;`, e, no fim da classe:

```csharp
    // ---- Ações da ficha ----

    private const string NaFaixa = ".aj-painel__botoes--faixa button";
    private const string NaConfirmacao = "[role=alertdialog] button";

    private static AcoesDaAtividade Acoes(bool editar = false, bool responsaveis = false, bool trocarRevisor = false,
        bool concluir = false, bool aprovar = false, bool reabrir = false, bool excluir = false) =>
        new(editar, responsaveis, trocarRevisor, concluir, aprovar, reabrir, excluir);

    private static void Clicar<T>(IRenderedComponent<T> tela, string seletor, string texto) where T : IComponent =>
        tela.FindAll(seletor).First(e => e.TextContent == texto).Click();

    private static List<string> Textos(IEnumerable<IElement> elementos) =>
        [.. elementos.Select(e => e.TextContent.Trim()).Where(t => t.Length > 0)];

    private static List<string> Lista(string textos) => textos.Length == 0 ? [] : [.. textos.Split('|')];

    private IRenderedComponent<ComBarraSuperior<AtividadeFicha>> FichaComBarra() =>
        Desenhar<ComBarraSuperior<AtividadeFicha>>(p => p.AddUnmatched(nameof(AtividadeFicha.Id), Id));

    [Theory]
    // Permissões (editar, responsáveis, trocar revisor, concluir, aprovar/devolver, reabrir, excluir) → faixa, divisor,
    // "Mais ações" e barra fixa do mobile. Os casos A a F da tabela da tarefa e as bordas do divisor.
    [InlineData(false, false, false, false, false, false, false, "", false, "", "")]
    [InlineData(false, false, false, true, false, false, false, "Concluir minha parte", false, "", "Concluir minha parte")]
    [InlineData(false, false, false, false, true, false, false, "Aprovar|Devolver", false, "", "Devolver|Aprovar")]
    [InlineData(true, false, true, false, true, false, true, "Aprovar|Devolver|Editar|Trocar revisor|Excluir", true, "Editar|Trocar revisor|Excluir", "Devolver|Aprovar")]
    [InlineData(true, true, true, false, false, false, true, "Editar|Responsáveis|Excluir", false, "Editar|Responsáveis|Excluir", "")]
    [InlineData(true, true, true, true, false, false, true, "Concluir minha parte|Editar|Responsáveis|Excluir", true, "Editar|Responsáveis|Excluir", "Concluir minha parte")]
    [InlineData(true, false, false, false, false, true, true, "Reabrir|Editar|Excluir", true, "Editar|Excluir", "Reabrir")]
    [InlineData(true, false, true, false, false, false, true, "Editar|Trocar revisor|Excluir", false, "Editar|Trocar revisor|Excluir", "")]
    public void Cada_acao_aparece_so_com_a_sua_permissao(
        bool editar, bool responsaveis, bool trocarRevisor, bool concluir, bool aprovar, bool reabrir, bool excluir,
        string faixa, bool divisor, string maisAcoes, string barraFixa)
    {
        Combinar(Detalhe(acoes: new(editar, responsaveis, trocarRevisor, concluir, aprovar, reabrir, excluir)));

        var ficha = FichaComBarra();

        ficha.Find(".aj-barra-superior h1").TextContent.Should().Be("Atividade");
        ficha.Find("h1.aj-titulo").TextContent.Should().Be("Minuta de acordo trabalhista");
        ficha.FindAll(".aj-painel__botoes--faixa").Should().HaveCount(faixa.Length == 0 ? 0 : 1);
        Textos(ficha.FindAll(".aj-painel__botoes--faixa a, .aj-painel__botoes--faixa button")).Should().Equal(Lista(faixa));
        ficha.FindAll(".aj-painel__divisor").Should().HaveCount(divisor ? 1 : 0);
        ficha.FindAll(".aj-painel__botoes--faixa button[aria-label='Excluir atividade']").Should().HaveCount(excluir ? 1 : 0);
        ficha.FindAll(".aj-barra-superior button[aria-label='Mais ações']").Should().HaveCount(maisAcoes.Length == 0 ? 0 : 1);
        Textos(ficha.FindAll(".aj-barra-superior .aj-menu-acoes__item")).Should().Equal(Lista(maisAcoes));
        ficha.FindAll(".aj-barra-superior .aj-menu-acoes__item--perigo").Should().HaveCount(excluir ? 1 : 0);
        Textos(ficha.FindAll(".aj-acoes--fixa a, .aj-acoes--fixa button")).Should().Equal(Lista(barraFixa));
    }

    [Fact]
    public void Concluir_minha_parte_pede_a_confirmacao_e_confirmar_envia_recarrega_e_avisa()
    {
        var concluida = false;
        Combinar(ator: AtorDaProducao);
        Despachante
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(_ => Resultado<AtividadeDetalhe>.Ok(concluida
                ? Detalhe(StatusAtividade.Feita)
                : Detalhe(StatusAtividade.Direcionada, Acoes(concluir: true))))
            .Responde<ConcluirMinhaParte, Resultado>(_ =>
            {
                concluida = true;
                return Resultado.Ok();
            });
        var ficha = Ficha();

        Clicar(ficha, NaFaixa, "Concluir minha parte");

        ficha.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar conclusão");
        ficha.Find("[role=alertdialog] strong").TextContent.Should().Be("Concluir a sua parte em Minuta de acordo trabalhista?");
        ficha.Find("[role=alertdialog] .aj-aviso__texto").TextContent.Should().Contain("A sua parte fica registrada como concluída.");
        Textos(ficha.FindAll(NaConfirmacao)).Should().Equal("Cancelar", "Concluir minha parte");

        Clicar(ficha, NaConfirmacao, "Concluir minha parte");

        ficha.WaitForAssertion(() => ficha.Find("p.aj-aviso--sucesso").TextContent.Should().Be("Sua parte foi concluída."));
        Despachante.Recebidas.OfType<ConcluirMinhaParte>().Should().ContainSingle().Which.Should().Be(new ConcluirMinhaParte(Id));
        Despachante.Recebidas.OfType<ObterAtividade>().Should().HaveCount(2);
        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        // A ficha recarregada é a de agora: Feita, e sem a ação que acabou de ser feita.
        ficha.Find("h1.aj-titulo").TextContent.Should().Be("Minuta de acordo trabalhista");
        ficha.FindAll(".aj-painel__botoes--faixa").Should().BeEmpty();
        ficha.Markup.Should().MatchRegex("aj-etiqueta--feita[^\"]*\">Feita</span>");
    }

    [Fact]
    public void Aprovar_pede_a_confirmacao_e_confirmar_envia_recarrega_e_avisa()
    {
        var aprovada = false;
        Combinar();
        Despachante
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(_ => Resultado<AtividadeDetalhe>.Ok(aprovada
                ? Detalhe(StatusAtividade.Concluida, Acoes(editar: true, reabrir: true, excluir: true))
                : Detalhe(acoes: Acoes(editar: true, trocarRevisor: true, aprovar: true, excluir: true))))
            .Responde<AprovarAtividade, Resultado>(_ =>
            {
                aprovada = true;
                return Resultado.Ok();
            });
        var ficha = Ficha();

        Clicar(ficha, NaFaixa, "Aprovar");

        ficha.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar aprovação");
        ficha.Find("[role=alertdialog] strong").TextContent.Should().Be("Aprovar Minuta de acordo trabalhista?");
        ficha.Find("[role=alertdialog] .aj-aviso__texto").TextContent.Should().Contain("A atividade fica concluída.");
        Textos(ficha.FindAll(NaConfirmacao)).Should().Equal("Cancelar", "Aprovar");

        Clicar(ficha, NaConfirmacao, "Aprovar");

        ficha.WaitForAssertion(() => ficha.Find("p.aj-aviso--sucesso").TextContent.Should().Be("Atividade aprovada."));
        Despachante.Recebidas.OfType<AprovarAtividade>().Should().ContainSingle().Which.Should().Be(new AprovarAtividade(Id));
        Despachante.Recebidas.OfType<ObterAtividade>().Should().HaveCount(2);
        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        Textos(ficha.FindAll(".aj-painel__botoes--faixa a, .aj-painel__botoes--faixa button")).Should().Equal("Reabrir", "Editar", "Excluir");
    }

    [Fact]
    public void Excluir_pede_a_confirmacao_e_confirmar_envia_com_a_versao_e_volta_a_lista_com_o_recado()
    {
        Combinar(Detalhe(acoes: Acoes(editar: true, excluir: true)));
        Despachante.Responde<ExcluirAtividade, Resultado>(Resultado.Ok());
        var ficha = Ficha();

        Clicar(ficha, NaFaixa, "Excluir");

        ficha.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar exclusão");
        ficha.Find("[role=alertdialog] strong").TextContent.Should().Be("Confirmar a exclusão de Minuta de acordo trabalhista?");
        ficha.Find("[role=alertdialog] .aj-aviso__texto").TextContent.Should()
            .Contain("A atividade sai das listas com os responsáveis e o histórico.");
        Textos(ficha.FindAll(NaConfirmacao)).Should().Equal("Cancelar", "Confirmar exclusão");

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        ficha.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/atividades"));
        Despachante.Recebidas.OfType<ExcluirAtividade>().Should().ContainSingle().Which.Should().Be(new ExcluirAtividade(Id, 7));
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Atividade excluída.");
    }

    [Fact]
    public void Excluir_vindo_de_produtores_tambem_volta_a_lista_de_atividades_com_o_recado()
    {
        var produtor = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000d1");
        Combinar(Detalhe(acoes: Acoes(editar: true, excluir: true)));
        Despachante.Responde<ExcluirAtividade, Resultado>(Resultado.Ok());
        // A ficha lê "de" e "produtor" do endereço: a navegação de teste começa nele.
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/atividades/{Id}?de=produtores&produtor={produtor}");
        var ficha = Ficha();
        // O voltar é o de Produtores: o endereço chegou à ficha.
        ficha.Find("a.aj-voltar").GetAttribute("href").Should().Be($"/produtores?produtor={produtor}");

        Clicar(ficha, NaFaixa, "Excluir");
        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        // Produtores não lê recados: a exclusão volta sempre à lista de atividades, que mostra o recado.
        ficha.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/atividades"));
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Atividade excluída.");
    }

    [Fact]
    public void Depois_de_excluir_com_sucesso_o_botao_fica_travado_ate_a_navegacao()
    {
        Combinar(Detalhe(acoes: Acoes(editar: true, excluir: true)));
        Despachante.Responde<ExcluirAtividade, Resultado>(Resultado.Ok());
        var ficha = Ficha();
        Clicar(ficha, NaFaixa, "Excluir");
        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");
        ficha.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/atividades"));

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        Despachante.Recebidas.OfType<ExcluirAtividade>().Should().ContainSingle();
        ficha.FindAll(NaConfirmacao).Should().OnlyContain(b => b.HasAttribute("disabled"));
    }

    [Fact]
    public void Excluir_pelo_mais_acoes_do_mobile_abre_a_mesma_confirmacao()
    {
        Combinar(Detalhe(acoes: Acoes(editar: true, excluir: true)));
        var ficha = FichaComBarra();

        ficha.Find(".aj-barra-superior button[aria-label='Mais ações']").Click();
        ficha.Find(".aj-barra-superior .aj-menu-acoes__item--perigo").Click();

        ficha.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be("Confirmar exclusão");
        Despachante.Recebidas.OfType<ExcluirAtividade>().Should().BeEmpty();
    }

    [Theory]
    [InlineData("Concluir minha parte", "Confirmar conclusão")]
    [InlineData("Aprovar", "Confirmar aprovação")]
    [InlineData("Excluir", "Confirmar exclusão")]
    public void Cancelar_fecha_a_confirmacao_e_nao_envia_nada(string acao, string rotulo)
    {
        var acoes = acao switch
        {
            "Concluir minha parte" => Acoes(concluir: true),
            "Aprovar" => Acoes(aprovar: true),
            _ => Acoes(editar: true, excluir: true),
        };
        Combinar(Detalhe(acoes: acoes));
        var ficha = Ficha();
        Clicar(ficha, NaFaixa, acao);
        ficha.Find("[role=alertdialog]").GetAttribute("aria-label").Should().Be(rotulo);

        Clicar(ficha, NaConfirmacao, "Cancelar");

        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        ficha.FindAll(NaFaixa).Should().Contain(b => b.TextContent == acao);
        Despachante.Recebidas.OfType<ConcluirMinhaParte>().Should().BeEmpty();
        Despachante.Recebidas.OfType<AprovarAtividade>().Should().BeEmpty();
        Despachante.Recebidas.OfType<ExcluirAtividade>().Should().BeEmpty();
        Despachante.Recebidas.OfType<ObterAtividade>().Should().ContainSingle();
    }

    [Fact]
    public void Pedir_outra_confirmacao_troca_a_aberta_e_tira_o_recado()
    {
        Combinar(Detalhe(acoes: Acoes(editar: true, aprovar: true, excluir: true)));
        Services.GetRequiredService<Recados>().Deixar("Responsáveis salvos.");
        var ficha = Ficha();
        ficha.Find("p.aj-aviso--sucesso").TextContent.Should().Be("Responsáveis salvos.");

        Clicar(ficha, NaFaixa, "Aprovar");
        Clicar(ficha, NaFaixa, "Excluir");

        ficha.FindAll("[role=alertdialog]").Should().ContainSingle()
            .Which.GetAttribute("aria-label").Should().Be("Confirmar exclusão");
        ficha.FindAll("p.aj-aviso--sucesso").Should().BeEmpty();
    }

    [Fact]
    public void Clique_duplo_em_concluir_minha_parte_envia_uma_vez()
    {
        var resposta = new TaskCompletionSource<Resultado>();
        Combinar(Detalhe(StatusAtividade.Direcionada, Acoes(concluir: true)), ator: AtorDaProducao);
        Despachante.RespondeDepois<ConcluirMinhaParte, Resultado>(resposta.Task);
        var ficha = Ficha();
        Clicar(ficha, NaFaixa, "Concluir minha parte");

        Clicar(ficha, NaConfirmacao, "Concluir minha parte");
        ficha.FindAll(NaConfirmacao).Should().OnlyContain(b => b.HasAttribute("disabled"));
        ficha.FindAll(NaFaixa).Should().OnlyContain(b => b.HasAttribute("disabled"));
        Clicar(ficha, NaConfirmacao, "Concluir minha parte");
        Clicar(ficha, NaFaixa, "Concluir minha parte");
        resposta.SetResult(Resultado.Ok());

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Sua parte foi concluída."));
        Despachante.Recebidas.OfType<ConcluirMinhaParte>().Should().ContainSingle();
    }

    [Fact]
    public void Clique_duplo_em_aprovar_envia_uma_vez()
    {
        var resposta = new TaskCompletionSource<Resultado>();
        Combinar(Detalhe(acoes: Acoes(aprovar: true)));
        Despachante.RespondeDepois<AprovarAtividade, Resultado>(resposta.Task);
        var ficha = Ficha();
        Clicar(ficha, NaFaixa, "Aprovar");

        Clicar(ficha, NaConfirmacao, "Aprovar");
        ficha.FindAll(NaConfirmacao).Should().OnlyContain(b => b.HasAttribute("disabled"));
        Clicar(ficha, NaConfirmacao, "Aprovar");
        Clicar(ficha, NaFaixa, "Aprovar");
        resposta.SetResult(Resultado.Ok());

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Atividade aprovada."));
        Despachante.Recebidas.OfType<AprovarAtividade>().Should().ContainSingle();
    }

    [Fact]
    public void Recusa_mostra_o_erro_e_traz_a_ficha_como_esta_agora()
    {
        var leituras = 0;
        Combinar();
        Despachante
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(_ => Resultado<AtividadeDetalhe>.Ok(++leituras == 1
                ? Detalhe(acoes: Acoes(aprovar: true))
                : Detalhe(StatusAtividade.Concluida)))
            .Responde<AprovarAtividade, Resultado>(Resultado.Falha(TipoErro.Validacao,
                "A atividade só pode ser aprovada ou devolvida quando está feita ou corrigida."));
        var ficha = Ficha();
        Clicar(ficha, NaFaixa, "Aprovar");

        Clicar(ficha, NaConfirmacao, "Aprovar");

        ficha.WaitForAssertion(() => ficha.Find("[role=alert]").TextContent.Should()
            .Contain("A atividade só pode ser aprovada ou devolvida quando está feita ou corrigida."));
        Despachante.Recebidas.OfType<ObterAtividade>().Should().HaveCount(2);
        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        ficha.Find("h1.aj-titulo").TextContent.Should().Be("Minuta de acordo trabalhista");
        ficha.FindAll(".aj-painel__botoes--faixa").Should().BeEmpty();
    }

    [Fact]
    public void Exclusao_recusada_mostra_o_erro_recarrega_e_nao_sai_da_ficha()
    {
        Combinar(Detalhe(acoes: Acoes(editar: true, excluir: true)));
        Despachante.Responde<ExcluirAtividade, Resultado>(Resultado.Falha(TipoErro.Conflito,
            "A atividade foi alterada por outra pessoa. Recarregue e tente novamente."));
        var ficha = Ficha();
        Clicar(ficha, NaFaixa, "Excluir");

        Clicar(ficha, NaConfirmacao, "Confirmar exclusão");

        ficha.WaitForAssertion(() => ficha.Find("[role=alert]").TextContent.Should()
            .Contain("A atividade foi alterada por outra pessoa. Recarregue e tente novamente."));
        Despachante.Recebidas.OfType<ObterAtividade>().Should().HaveCount(2);
        ficha.FindAll("[role=alertdialog]").Should().BeEmpty();
        ficha.FindAll(NaFaixa).Should().OnlyContain(b => !b.HasAttribute("disabled"));
        Services.GetRequiredService<NavigationManager>().Uri.Should().NotEndWith("/atividades");
        Services.GetRequiredService<Recados>().Retirar().Should().BeNull();
    }

    [Fact]
    public void Recarga_que_falha_depois_do_sucesso_avisa_que_a_ficha_nao_foi_atualizada()
    {
        var leituras = 0;
        Combinar();
        Despachante
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(_ => ++leituras == 1
                ? Resultado<AtividadeDetalhe>.Ok(Detalhe(acoes: Acoes(aprovar: true)))
                : Resultado<AtividadeDetalhe>.Falha(TipoErro.Validacao, "Falha de teste."))
            .Responde<AprovarAtividade, Resultado>(Resultado.Ok());
        var ficha = Ficha();
        Clicar(ficha, NaFaixa, "Aprovar");

        Clicar(ficha, NaConfirmacao, "Aprovar");

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Atividade aprovada, mas não foi possível atualizar a ficha."));
        // Dados antigos não ficam na tela: no lugar da ficha, o erro da consulta com "Tentar de novo".
        ficha.Markup.Should().Contain("Falha de teste.").And.Contain("Tentar de novo");
        ficha.FindAll(".aj-abas").Should().BeEmpty();
    }

    [Fact]
    public void Acesso_negado_numa_acao_encerra_a_tela()
    {
        Combinar(Detalhe(acoes: Acoes(aprovar: true)));
        Despachante.Responde<AprovarAtividade, Resultado>(Resultado.Falha(TipoErro.AcessoNegado, "Acesso negado."));
        var ficha = Ficha();
        Clicar(ficha, NaFaixa, "Aprovar");

        Clicar(ficha, NaConfirmacao, "Aprovar");

        ficha.WaitForAssertion(() => ficha.Markup.Should().Contain("Seu acesso foi encerrado. Entre novamente."));
        ficha.Markup.Should().NotContain("Minuta de acordo trabalhista").And.NotContain("Acesso negado.");
        Despachante.Recebidas.OfType<ObterAtividade>().Should().ContainSingle();
    }
```

(`p.AddUnmatched` passa o `Id` ao componente através do `ComBarraSuperior`, que o desenha junto do encaixe da barra superior: sem o encaixe o "Mais ações" não chega à marcação. O `TextContent` do Excluir do tablet é vazio (só o ícone), por isso `Textos` o deixa de fora e a contagem dele vai pelo `aria-label`. Em `Excluir_vindo_de_produtores_…`, se a versão do bUnit não preencher os `[SupplyParameterFromQuery]` pela navegação, passe-os no desenho, `Desenhar<AtividadeFicha>(p => p.Add(f => f.Id, Id).Add(f => f.De, "produtores").Add(f => f.Produtor, produtor.ToString()))`, e registre; a asserção sobre o voltar continua provando que a ficha veio de Produtores.)

- [ ] **Step 3: Rodar (RED)**

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadeFicha"`
Expected: compila; os testes da Tarefa 10 passam e os novos falham (sem faixa, sem "Mais ações", sem barra fixa, sem confirmações). Os casos sem nenhuma ação da theory (`""` em tudo) e o caso E passam já: eles provam a ausência, e o RED vem dos outros.

- [ ] **Step 4: A marcação da tela**

Em `src/AtividadesJuridicas.Web/Components/Pages/AtividadeFicha.razor`, acrescente `@inject NavigationManager Navegacao` depois de `@inject Recados Recados`. A marcação, de `<PageTitle>` até o fim do `else`, passa a ser (o corpo da ficha, de `<div class="aj-so-mobile">` com as abas até o fecho do `aj-lado-a-lado`, não muda):

```razor
<PageTitle>@(_detalhe?.Titulo ?? "Atividade")</PageTitle>

@if (!_carregado || _ator is null || _detalhe is null)
{
    <CabecalhoDeDetalhe Titulo="Atividade" VoltarPara="@Voltar" Origem="@Origem" />
    @* O resultado de uma ação cuja recarga deixou a ficha fora da tela ("…, mas não foi possível atualizar a ficha."). *@
    @if (_mensagem is not null)
    {
        <Aviso Tipo="@(_falhou ? TipoDeAviso.Erro : TipoDeAviso.Sucesso)">@_mensagem</Aviso>
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
        @* A atividade foi excluída ou o usuário não a vê: o servidor responde igual nos dois casos. *@
        <Aviso Tipo="TipoDeAviso.Erro">Atividade não encontrada.</Aviso>
    }
    else
    {
        @* Falha ao consultar nunca vira uma ficha vazia nem deixa dados antigos na tela. *@
        <EstadoDaLista Erro="@(_erro ?? "Não foi possível consultar a atividade.")" TextoDeVazio="" TentarDeNovo="CarregarAsync" />
    }
}
else
{
    var a = _detalhe;
    <CabecalhoDaAtividade Detalhe="a" VoltarPara="@Voltar" Origem="@Origem"
                          Faixa="@(TemAcaoPrincipal(a.Acoes) || TemAcaoDeCadastro(a.Acoes) ? FaixaDeAcoes(a.Acoes) : null)"
                          AcaoDaBarra="@(TemAcaoDeCadastro(a.Acoes) ? MaisAcoes(a.Acoes) : null)" />

    @if (_recado is { } recado)
    {
        <Aviso Tipo="@recado.Tipo">@recado.Texto</Aviso>
    }
    @if (_mensagem is not null)
    {
        <Aviso Tipo="@(_falhou ? TipoDeAviso.Erro : TipoDeAviso.Sucesso)">@_mensagem</Aviso>
    }
    @* Uma confirmação por vez, no topo. A chave é a confirmação: pedir outra recria o aviso, que leva o foco a si. *@
    @if (_confirmando == Confirmacao.Concluir)
    {
        <Aviso @key="_confirmando" Tipo="TipoDeAviso.Atencao" Rotulo="Confirmar conclusão" Titulo="@($"Concluir a sua parte em {a.Titulo}?")">
            <ChildContent>A sua parte fica registrada como concluída.</ChildContent>
            <Acoes><button type="button" class="aj-botao aj-botao--secundario" disabled="@_ocupado" @onclick="CancelarConfirmacao">Cancelar</button><button type="button" class="aj-botao aj-botao--primario" disabled="@_ocupado" @onclick="ConcluirMinhaParteAsync">Concluir minha parte</button></Acoes>
        </Aviso>
    }
    else if (_confirmando == Confirmacao.Aprovar)
    {
        <Aviso @key="_confirmando" Tipo="TipoDeAviso.Atencao" Rotulo="Confirmar aprovação" Titulo="@($"Aprovar {a.Titulo}?")">
            <ChildContent>A atividade fica concluída.</ChildContent>
            <Acoes><button type="button" class="aj-botao aj-botao--secundario" disabled="@_ocupado" @onclick="CancelarConfirmacao">Cancelar</button><button type="button" class="aj-botao aj-botao--primario" disabled="@_ocupado" @onclick="AprovarAsync">Aprovar</button></Acoes>
        </Aviso>
    }
    else if (_confirmando == Confirmacao.Excluir)
    {
        <Aviso @key="_confirmando" Tipo="TipoDeAviso.Atencao" Rotulo="Confirmar exclusão" Titulo="@($"Confirmar a exclusão de {a.Titulo}?")">
            <ChildContent>A atividade sai das listas com os responsáveis e o histórico.</ChildContent>
            <Acoes><button type="button" class="aj-botao aj-botao--secundario" disabled="@_ocupado" @onclick="CancelarConfirmacao">Cancelar</button><button type="button" class="aj-botao aj-botao--primario" disabled="@_ocupado" @onclick="ExcluirAsync">Confirmar exclusão</button></Acoes>
        </Aviso>
    }

    @* … as abas e o corpo da ficha, como estão … *@

    @* Barra fixa do mobile: Devolver e a ação primária, nessa ordem (o quadro); sem ação primária, sem barra. *@
    @if (TemAcaoPrincipal(a.Acoes))
    {
        <div class="aj-so-mobile">
            <div class="aj-acoes aj-acoes--fixa">@BotaoDevolver(a.Acoes)@AcaoPrimaria(a.Acoes)</div>
        </div>
    }
}
```

(O comentário `@* … as abas e o corpo da ficha, como estão … *@` só marca o lugar: não o copie; ali fica o que a Tarefa 10 escreveu.)

- [ ] **Step 5: O código da tela**

No `@code` de `AtividadeFicha.razor`:

1. Novos membros:

```csharp
    private enum Confirmacao { Nenhuma, Concluir, Aprovar, Excluir }

    private Confirmacao _confirmando;
    // A versão da atividade com que a confirmação foi aberta: a exclusão a manda como versão esperada.
    private uint _versaoDaConfirmacao;
    private bool _ocupado;
    private string? _mensagem;
    private bool _falhou;

    private string Rota(string acao) => $"/atividades/{Id}/{acao}";

    private static bool TemAcaoPrincipal(AcoesDaAtividade a) => a.ConcluirMinhaParte || a.AprovarOuDevolver || a.Reabrir;

    // Quando a atividade permite redefinir os responsáveis, o painel "Revisor" da tela de responsáveis já troca o revisor.
    private static bool SoTrocaORevisor(AcoesDaAtividade a) => a.TrocarRevisor && !a.DefinirResponsaveis;

    private static bool TemAcaoDeCadastro(AcoesDaAtividade a) =>
        a.Editar || a.DefinirResponsaveis || SoTrocaORevisor(a) || a.Excluir;

    /// <summary>A ação primária: Concluir minha parte, ou Aprovar, ou Reabrir (as regras do servidor não deixam duas valerem juntas).</summary>
    private RenderFragment AcaoPrimaria(AcoesDaAtividade acoes) => @<text>@if (acoes.ConcluirMinhaParte)
        {<button type="button" class="aj-botao aj-botao--primario aj-botao--com-icone" disabled="@_ocupado" @onclick="PedirConclusao"><Icone Nome="confirmar" />Concluir minha parte</button>}
        else if (acoes.AprovarOuDevolver)
        {<button type="button" class="aj-botao aj-botao--primario aj-botao--com-icone" disabled="@_ocupado" @onclick="PedirAprovacao"><Icone Nome="confirmar" />Aprovar</button>}
        else if (acoes.Reabrir)
        {<a href="@Rota("reabrir")" class="aj-botao aj-botao--secundario">Reabrir</a>}</text>;

    private RenderFragment BotaoDevolver(AcoesDaAtividade acoes) => @<text>@if (acoes.AprovarOuDevolver)
        {<a href="@Rota("devolver")" class="aj-botao aj-botao--secundario aj-botao--com-icone"><Icone Nome="devolver" />Devolver</a>}</text>;

    /// <summary>
    /// A faixa do desktop e do tablet, na ordem do quadro: a ação primária, Devolver, o divisor (só no desktop, e só com
    /// ação dos dois lados), Editar, Responsáveis, Trocar revisor e, à direita, Excluir (com texto no desktop, só o ícone
    /// no tablet).
    /// </summary>
    private RenderFragment FaixaDeAcoes(AcoesDaAtividade acoes) => @<text>@AcaoPrimaria(acoes)@BotaoDevolver(acoes)
        @if (TemAcaoPrincipal(acoes) && TemAcaoDeCadastro(acoes))
        {<span class="aj-so-desktop"><span class="aj-painel__divisor" aria-hidden="true"></span></span>}
        @if (acoes.Editar)
        {<a href="@Rota("editar")" class="aj-botao aj-botao--fantasma">Editar</a>}
        @if (acoes.DefinirResponsaveis)
        {<a href="@Rota("responsaveis")" class="aj-botao aj-botao--fantasma">Responsáveis</a>}
        @if (SoTrocaORevisor(acoes))
        {<a href="@Rota("responsaveis")" class="aj-botao aj-botao--fantasma">Trocar revisor</a>}
        @if (acoes.Excluir)
        {<span class="aj-so-desktop"><button type="button" class="aj-botao aj-botao--perigo aj-botao--com-icone-menor aj-botao--ao-fim" disabled="@_ocupado" @onclick="PedirExclusao"><Icone Nome="excluir" />Excluir</button></span><span class="aj-so-tablet"><button type="button" class="aj-botao aj-botao--perigo aj-botao--icone aj-botao--ao-fim" aria-label="Excluir atividade" disabled="@_ocupado" @onclick="PedirExclusao"><Icone Nome="excluir" /></button></span>}</text>;

    /// <summary>O "Mais ações" da barra de voltar do mobile, com as ações de cadastro da faixa, Excluir por último.</summary>
    private RenderFragment MaisAcoes(AcoesDaAtividade acoes) => @<MenuDeAcoes Rotulo="Mais ações">@if (acoes.Editar)
        {<a href="@Rota("editar")" class="aj-menu-acoes__item">Editar</a>}
        @if (acoes.DefinirResponsaveis)
        {<a href="@Rota("responsaveis")" class="aj-menu-acoes__item">Responsáveis</a>}
        @if (SoTrocaORevisor(acoes))
        {<a href="@Rota("responsaveis")" class="aj-menu-acoes__item">Trocar revisor</a>}
        @if (acoes.Excluir)
        {<button type="button" class="aj-menu-acoes__item aj-menu-acoes__item--perigo" disabled="@_ocupado" @onclick="PedirExclusao">Excluir</button>}</MenuDeAcoes>;

    // Métodos, e não lambdas de atribuição, nos @onclick: as confirmações têm regras.
    private void Pedir(Confirmacao confirmacao)
    {
        if (_ocupado || _detalhe is null) return;

        _recado = null;
        _mensagem = null;
        _versaoDaConfirmacao = _detalhe.Versao;
        _confirmando = confirmacao;
    }

    private void PedirConclusao() => Pedir(Confirmacao.Concluir);

    private void PedirAprovacao() => Pedir(Confirmacao.Aprovar);

    private void PedirExclusao() => Pedir(Confirmacao.Excluir);

    private void CancelarConfirmacao()
    {
        if (!_ocupado) _confirmando = Confirmacao.Nenhuma;
    }

    private void Avisar(string mensagem, bool falhou)
    {
        _mensagem = mensagem;
        _falhou = falhou;
    }

    private Task ConcluirMinhaParteAsync() =>
        ExecutarAsync(() => Despachante.EnviarAsync(new ConcluirMinhaParte(Id)), "Sua parte foi concluída.");

    private Task AprovarAsync() =>
        ExecutarAsync(() => Despachante.EnviarAsync(new AprovarAtividade(Id)), "Atividade aprovada.");

    /// <summary>
    /// Roda uma ação que fica na ficha: um clique duplo não repete; acesso negado encerra a tela; qualquer recusa mostra o
    /// erro e traz a ficha como está agora (os botões acompanham o servidor); o sucesso recarrega a ficha e avisa, ou diz
    /// que a ficha não pôde ser atualizada.
    /// </summary>
    private async Task ExecutarAsync(Func<Task<Resultado>> acao, string sucesso)
    {
        if (_ocupado) return;

        _ocupado = true;
        _mensagem = null;
        _recado = null;
        try
        {
            var resultado = await acao();
            if (resultado.Erro == TipoErro.AcessoNegado)
            {
                EncerrarAcesso();
                return;
            }

            _confirmando = Confirmacao.Nenhuma;
            if (!resultado.Sucesso)
            {
                await RecarregarAsync();
                if (_ator is null) return;
                Avisar(resultado.Mensagem ?? "Não foi possível concluir a ação.", falhou: true);
                return;
            }

            var atualizada = await RecarregarAsync();
            if (_ator is null) return;
            Avisar(atualizada ? sucesso : $"{sucesso.TrimEnd('.')}, mas não foi possível atualizar a ficha.", falhou: !atualizada);
        }
        finally
        {
            _ocupado = false;
        }
    }

    private async Task ExcluirAsync()
    {
        if (_ocupado) return;

        _ocupado = true;
        _mensagem = null;
        _recado = null;
        var navegou = false;
        try
        {
            var resultado = await Despachante.EnviarAsync(new ExcluirAtividade(Id, _versaoDaConfirmacao));
            if (resultado.Erro == TipoErro.AcessoNegado)
            {
                EncerrarAcesso();
                return;
            }

            if (!resultado.Sucesso)
            {
                _confirmando = Confirmacao.Nenhuma;
                await RecarregarAsync();
                if (_ator is null) return;
                Avisar(resultado.Mensagem ?? "Não foi possível excluir a atividade.", falhou: true);
                return;
            }

            // Sempre a lista de atividades, mesmo vindo de Produtores: é ela que mostra o recado (Produtores não lê).
            Recados.Deixar("Atividade excluída.");
            navegou = true;
            Navegacao.NavigateTo("/atividades");
        }
        finally
        {
            // Depois do sucesso a página vai embora: a confirmação fica travada para um clique tardio não excluir de novo.
            if (!navegou) _ocupado = false;
        }
    }
```

2. Em `CarregarAsync`, junto das outras linhas que zeram o estado: `_mensagem = null;` e `_confirmando = Confirmacao.Nenhuma;`.

3. `EncerrarAcesso` passa a ser:

```csharp
    private void EncerrarAcesso()
    {
        _ator = null;
        _detalhe = null;
        _confirmando = Confirmacao.Nenhuma;
        _mensagem = null;
    }
```

Notas para quem implementa:

- Os encaixes `Faixa` e `AcaoDaBarra` recebem fragmentos novos a cada desenho da tela, então o cabeçalho e a barra do mobile se redesenham com o `_ocupado` de agora (os botões ficam travados durante uma ação).
- "Editar", "Responsáveis", "Trocar revisor", "Devolver" e "Reabrir" são links: a tela de destino confere a permissão (Tarefas 7, 12 e 13); esta tela não relê a atividade antes de ir.
- Se o Razor recusar o `@if … {<a>…</a>}` dentro de `@<text>` ou de `@<MenuDeAcoes>`, quebre as linhas, mantendo colado o que o teste procura colado (`<span class="aj-so-desktop"><span class="aj-painel__divisor" aria-hidden="true"></span></span>` e os dois `<span>` do Excluir com o botão dentro).

- [ ] **Step 6: Build e testes**

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadeFicha|FullyQualifiedName~ProdutoresWebTests"`
Expected: 0 avisos; todos passando.

- [ ] **Step 7: Commit**

```powershell
git branch --show-current
git add src tests
git commit -m "feat(web): ações da ficha da atividade por permissão, com confirmações no topo, Mais ações e barra fixa no mobile"
```

Expected do `git branch --show-current`: `worktree-feat+fase-9e-atividades`.

---

### Task 12: Responsáveis e revisor em tela própria

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Pages/AtividadeResponsaveis.razor` (rota `/atividades/{Id:guid}/responsaveis`)
- Create: `tests/AtividadesJuridicas.IntegrationTests/AtividadeResponsaveisWebTests.cs`, `AtividadeResponsaveisInterativosTests.cs`
- Modify: `PortalDoClienteWebTests.cs`, `AutenticacaoWebTests.cs`, `TelasNoDesignSystemTests.cs` (`Migradas`)

**Interfaces:**
- Consumes: `CargaDoFormulario`, `CabecalhoDeDetalhe` (`RotuloDoVoltarNoMobile="Voltar para a atividade"`), `Aviso`, `Recados`; `ObterMeuAcesso`, `ObterAtividade`, `ListarUsuarios`, `DefinirResponsaveisDaAtividade`, `TrocarRevisorDaAtividade`.
- Produces: a rota `/atividades/{Id:guid}/responsaveis`. O componente antigo `EditorDeResponsaveis.razor` sai na Tarefa 14.

**Regras:**
- Quadros: `Responsaveis.dc.html`, `…Tablet`, `…Mobile` (`quadros-lista-e-formularios.md`, seção 3).
- Dois modos, pela permissão da atividade carregada: com `DefinirResponsaveis`, a tela inteira (título `Responsáveis`, painel principal com o título da atividade e as opções, painel `Revisor`, `Salvar responsáveis`), que envia `DefinirResponsaveisDaAtividade` (revisor nulo = manter/deduzir) com a versão da carga e volta à ficha com `Responsáveis salvos.`; só com `TrocarRevisor`, o modo do revisor (título `Trocar revisor`, só o painel `Revisor` com os diretores ativos e o atual escolhido, botão `Trocar revisor`), que envia `TrocarRevisorDaAtividade` com a versão e volta com `Revisor alterado.` (`Escolha o novo revisor.` sem escolha); sem nenhuma das duas, o aviso `Esta atividade não permite mais essa ação.` com o voltar.
- Opções (lógica do `EditorDeResponsaveis.razor` de hoje, sem mudar): os usuários ativos não clientes, por nome, com a nota `({perfil})` em `aj-opcao__nota`, que é o nome do perfil (`PerfilNome`, como hoje: os perfis são cadastráveis, e o quadro mostra exemplos); os responsáveis atuais que não entraram, marcados, com ` (inativo)` ou ` (usuário indisponível)`; marcado mostra logo abaixo `Direcionamento de {nome}` (`aj-rotulo aj-rotulo--recuado`, `textarea rows="2"`). Revisor: primeira opção `Manter / deduzir (atual: {revisor})` (valor vazio) e os diretores ativos.
- Arranjo: `EditForm` `id="formulario-de-responsaveis"` em `aj-lado-a-lado aj-lado-a-lado--formulario`; o painel principal com `aj-painel--sempre-destacado`; no mobile, o subtítulo vira `aj-ajuda` no topo. No modo só do revisor, o painel `Revisor` é sozinho o principal: `aj-painel aj-painel--principal aj-painel--sempre-destacado aj-painel--medio aj-lado-a-lado__lista` (680px no máximo no desktop; o `aj-lado-a-lado__lista` o deixa crescer até essa medida na linha do arranjo), e o subtítulo é o título da atividade. Ações como no formulário da atividade (`Salvar responsáveis`; no mobile `Salvar`).
- Voltar: a ficha (o título da atividade). Falha: aviso de erro, o formulário fica; conflito ou não encontrado: o aviso com a mensagem; acesso negado: encerra. Produção: "Acesso negado." sem consulta. Guarda de clique duplo.
- Testes HTTP (os dois modos e o terceiro estado; Produção; não encontrada; textos de cada visão) e bUnit (marcar mostra o direcionamento; salvar envia os marcados com os direcionamentos aparados, o revisor e a versão, e navega com o recado; modo do revisor; clique duplo).

- [ ] **Step 1: Testes (devem falhar)**

`tests/AtividadesJuridicas.IntegrationTests/AtividadeResponsaveisWebTests.cs` (HTTP, contra o banco). Três atividades com o Pedro (Produção) responsável e a Diana (Diretoria) revisora: uma Direcionada (a tela inteira), uma Feita (só o revisor) e uma Concluída (nenhuma das duas).

```csharp
using System.Net;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AtividadeResponsaveisWebTests(BancoFixture banco) : IAsyncLifetime
{
    private const string Ajuda =
        "Marque quem é responsável e escreva o direcionamento de cada um. Quem está desmarcado deixa de ser responsável.";

    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _pedro, _minuta, _recurso, _revisao;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();

        // Direcionada: a Diretoria redefine os responsáveis (e o revisor) na tela inteira.
        _minuta = await DirecionadaAsync("Minuta de acordo trabalhista", "Redigir a minuta com as condições discutidas na audiência.");

        // Feita: os responsáveis não mudam mais, só o revisor.
        _recurso = await DirecionadaAsync("Recurso ordinário", "Redigir o recurso.");
        (await _app.ExecutarAsync(_pedro, new ConcluirMinhaParte(_recurso))).Sucesso.Should().BeTrue();

        // Concluída: nem os responsáveis nem o revisor mudam.
        _revisao = await DirecionadaAsync("Revisão de contrato de locação", "Revisar as cláusulas.");
        (await _app.ExecutarAsync(_pedro, new ConcluirMinhaParte(_revisao))).Sucesso.Should().BeTrue();
        (await _app.ExecutarAsync(_admin, new AprovarAtividade(_revisao))).Sucesso.Should().BeTrue();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<Guid> DirecionadaAsync(string titulo, string direcionamento)
    {
        var criada = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, titulo, "Descrição da atividade.", new DateOnly(2026, 10, 9), null, null, null, 2));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        var direcionada = await _app.ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
            criada.Valor, [new ResponsavelEntrada(_pedro, direcionamento)], _admin));
        direcionada.Sucesso.Should().BeTrue(direcionada.Mensagem);
        return criada.Valor;
    }

    private async Task<string> PaginaAsync(string rota) =>
        WebUtility.HtmlDecode(await (await _app.ClienteAutenticadoAsync()).GetStringAsync(rota));

    [Fact]
    public async Task Atividade_que_permite_redefinir_traz_a_tela_inteira_com_as_opcoes_o_direcionamento_e_o_revisor()
    {
        var html = await PaginaAsync($"/atividades/{_minuta}/responsaveis");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Responsáveis</h1>")
            .And.Contain($"<p class=\"aj-subtitulo\">{Ajuda}</p>")
            .And.MatchRegex($"(?s)<a href=\"/atividades/{_minuta}\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Minuta de acordo trabalhista</a>")
            .And.MatchRegex("<form[^>]*id=\"formulario-de-responsaveis\"[^>]*class=\"aj-lado-a-lado aj-lado-a-lado--formulario\"")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-painel--principal aj-painel--sempre-destacado aj-lado-a-lado__lista\">\\s*<h2>Minuta de acordo trabalhista</h2>")
            // A diretora não é responsável: a opção vem desmarcada, com o perfil na nota.
            .And.MatchRegex("<label class=\"aj-opcao\"><input type=\"checkbox\" class=\"aj-opcao__caixa\"(?:(?!checked)[^>])*>Diana Diretora<span class=\"aj-opcao__nota\">\\(Diretor\\)</span></label>")
            // O Pedro é responsável: marcado, com o direcionamento logo abaixo.
            .And.MatchRegex("<label class=\"aj-opcao\"><input type=\"checkbox\" class=\"aj-opcao__caixa\" checked[^>]*>Pedro Produção<span class=\"aj-opcao__nota\">\\(Produção\\)</span></label>\\s*<label class=\"aj-rotulo aj-rotulo--recuado\">Direcionamento de Pedro Produção<textarea class=\"aj-campo\" rows=\"2\" maxlength=\"4000\"")
            .And.Contain("Redigir a minuta com as condições discutidas na audiência.")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-lado-a-lado__painel\">\\s*<h2>Revisor</h2>\\s*<label class=\"aj-rotulo\"><span class=\"aj-so-leitor\">Revisor</span><select class=\"aj-campo\"")
            .And.Contain(">Manter / deduzir (atual: Diana Diretora)</option>");
        html.Should().NotContain("Direcionamento de Diana Diretora").And.NotContain("Esta atividade não permite mais essa ação.");

        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            $"(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a href=\"/atividades/{_minuta}\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>\\s*<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-de-responsaveis\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Salvar responsáveis</button>");
        BlocoDeVisao.De(html, "aj-so-tablet").Should().MatchRegex(
            $"(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a href=\"/atividades/{_minuta}\" class=\"aj-botao aj-botao--secundario\">Cancelar</a>.*?Salvar responsáveis</button>");
        var mobile = BlocoDeVisao.De(html, "aj-so-mobile");
        mobile.Should().Contain($"<span class=\"aj-ajuda\">{Ajuda}</span>")
            .And.MatchRegex("(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a [^>]*class=\"aj-botao aj-botao--secundario\">Cancelar</a>.*?Salvar</button>");
        mobile.Should().NotContain("Salvar responsáveis");

        // A barra de voltar do mobile diz "Voltar para a atividade", não o título.
        html.Should().MatchRegex($"<a href=\"/atividades/{_minuta}\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Voltar para a atividade\">")
            .And.Contain("<h1>Responsáveis</h1>");
    }

    [Fact]
    public async Task Atividade_feita_abre_so_a_troca_de_revisor_com_o_atual_escolhido()
    {
        var html = await PaginaAsync($"/atividades/{_recurso}/responsaveis");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Trocar revisor</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Recurso ordinário</p>")
            .And.MatchRegex("(?s)<section class=\"aj-painel aj-painel--principal aj-painel--sempre-destacado aj-painel--medio aj-lado-a-lado__lista\">\\s*<h2>Revisor</h2>")
            .And.Contain(">Selecione…</option>")
            .And.MatchRegex($"<option value=\"{_admin}\" selected[^>]*>Diana Diretora</option>");
        html.Should().NotContain("aj-opcao__caixa").And.NotContain("Direcionamento de")
            .And.NotContain("Manter / deduzir").And.NotContain("Salvar responsáveis");

        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            "(?s)<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-de-responsaveis\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Trocar revisor</button>");
        BlocoDeVisao.De(html, "aj-so-mobile").Should().Contain("<span class=\"aj-ajuda\">Recurso ordinário</span>")
            .And.MatchRegex("(?s)<div class=\"aj-acoes aj-acoes--fixa\">.*?Salvar</button>");
    }

    [Fact]
    public async Task Atividade_concluida_avisa_que_nao_permite_mais_e_nao_mostra_formulario()
    {
        var html = await PaginaAsync($"/atividades/{_revisao}/responsaveis");

        html.Should().Contain("Esta atividade não permite mais essa ação.")
            .And.MatchRegex("<h1 class=\"aj-titulo\">Responsáveis</h1>")
            .And.MatchRegex($"(?s)<a href=\"/atividades/{_revisao}\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Revisão de contrato de locação</a>");
        html.Should().NotContain("formulario-de-responsaveis").And.NotContain("Salvar responsáveis")
            .And.NotContain(Ajuda);
    }

    [Fact]
    public async Task Atividade_que_nao_existe_mostra_nao_encontrada_com_o_voltar_para_a_lista()
    {
        var html = await PaginaAsync($"/atividades/{Guid.NewGuid()}/responsaveis");

        html.Should().Contain("Atividade não encontrada.")
            .And.MatchRegex("<a href=\"/atividades\" class=\"aj-voltar\">")
            .And.Contain("aria-label=\"Voltar para Atividades\"");
        html.Should().NotContain("formulario-de-responsaveis");
    }

    [Fact]
    public async Task Producao_ve_acesso_negado_sem_consultar_a_atividade()
    {
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await pedro.GetStringAsync($"/atividades/{_minuta}/responsaveis"));

        html.Should().Contain("Acesso negado.").And.MatchRegex("<h1 class=\"aj-titulo\">Responsáveis</h1>");
        html.Should().NotContain("Minuta de acordo trabalhista").And.NotContain("formulario-de-responsaveis");
        // A tela confere o perfil antes de consultar: a atividade não foi lida pelo Pedro.
        (await _app.EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking()
            .CountAsync(r => r.UsuarioId == _pedro && r.TipoRecurso == "Atividade" && r.RecursoId == _minuta)))
            .Should().Be(0);
    }
}
```

`tests/AtividadesJuridicas.IntegrationTests/AtividadeResponsaveisInterativosTests.cs` (bUnit; o `NavigationManager` do bUnit registra o destino):

```csharp
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Domain.Identidade;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

public class AtividadeResponsaveisInterativosTests : TelaInterativa
{
    private static readonly Guid Id = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a1");
    private static readonly Guid Beatriz = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000b001");
    private static readonly Guid Camila = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000b002");
    private static readonly Guid Helena = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000b003");
    private static readonly Guid Marina = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000b004");
    private static readonly Guid Otavio = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000b005");
    private static readonly Guid Ricardo = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000b006");
    private static readonly Guid Davi = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000b007");

    // Editar, DefinirResponsaveis, TrocarRevisor, ConcluirMinhaParte, AprovarOuDevolver, Reabrir, Excluir.
    private static readonly AcoesDaAtividade Completa = new(true, true, true, false, false, false, true);
    private static readonly AcoesDaAtividade SoRevisor = new(true, false, true, false, false, false, true);
    private static readonly AcoesDaAtividade Nenhuma = new(true, false, false, false, false, false, true);

    private static UsuarioResumo Usuario(Guid id, string nome, TipoPerfil tipo, bool ativo = true) =>
        new(id, nome, $"{id:N}@x.com", Guid.Empty, tipo == TipoPerfil.Diretoria ? "Diretoria" : "Produção", tipo, null, ativo, null);

    // O Otávio é responsável e está inativo; o Ricardo é responsável e não vem na lista de usuários; o Davi é um
    // diretor inativo (não entra como revisor).
    private static readonly IReadOnlyList<UsuarioResumo> Usuarios =
    [
        Usuario(Marina, "Marina Couto", TipoPerfil.Diretoria),
        Usuario(Beatriz, "Beatriz Lins", TipoPerfil.Producao),
        Usuario(Otavio, "Otávio Brandão", TipoPerfil.Producao, ativo: false),
        Usuario(Camila, "Camila Prado", TipoPerfil.Producao),
        Usuario(Helena, "Helena Rocha", TipoPerfil.Diretoria),
        Usuario(Davi, "Davi Duarte", TipoPerfil.Diretoria, ativo: false),
    ];

    private static AtividadeDetalhe Atividade(AcoesDaAtividade acoes, Guid revisorId, string revisorNome) => new(
        Id, "Minuta de acordo trabalhista", "Redigir a minuta.", new DateOnly(2026, 10, 9), StatusAtividade.Direcionada, false,
        null, null, null, null, null, null, null, TipoAutor.Usuario, "Marina Couto", revisorId, revisorNome,
        [
            new ResponsavelDetalhe(Beatriz, "Beatriz Lins", true, "Redigir a minuta com as condições discutidas na audiência.", null),
            new ResponsavelDetalhe(Otavio, "Otávio Brandão", true, null, null),
            new ResponsavelDetalhe(Ricardo, "Ricardo Lemos", true, null, null),
        ],
        [], acoes, 7u, 2);

    private IRenderedComponent<AtividadeResponsaveis> Tela(
        AcoesDaAtividade acoes, Guid? revisorId = null, string revisorNome = "Marina Couto")
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(
                Resultado<AtividadeDetalhe>.Ok(Atividade(acoes, revisorId ?? Marina, revisorNome)))
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(Resultado<IReadOnlyList<UsuarioResumo>>.Ok(Usuarios))
            .Responde<DefinirResponsaveisDaAtividade, Resultado>(Resultado.Ok())
            .Responde<TrocarRevisorDaAtividade, Resultado>(Resultado.Ok());
        return Desenhar<AtividadeResponsaveis>(p => p.Add(t => t.Id, Id));
    }

    private static void Marcar(IRenderedComponent<AtividadeResponsaveis> tela, string nome, bool marcado) =>
        tela.FindAll("label.aj-opcao").Single(l => l.TextContent.StartsWith(nome)).QuerySelector("input")!.Change(marcado);

    private static void Direcionar(IRenderedComponent<AtividadeResponsaveis> tela, string nome, string texto) =>
        tela.FindAll("label.aj-rotulo--recuado").Single(l => l.TextContent.StartsWith($"Direcionamento de {nome}"))
            .QuerySelector("textarea")!.Change(texto);

    private string Destino => Services.GetRequiredService<NavigationManager>().Uri;

    [Fact]
    public void Opcoes_trazem_o_perfil_e_os_responsaveis_que_sairam_da_lista_marcados()
    {
        var tela = Tela(Completa);

        // Ativos que não são de cliente, por nome; depois os responsáveis de hoje que não entraram, marcados.
        tela.FindAll("label.aj-opcao").Select(l => l.TextContent).Should().Equal(
            "Beatriz Lins(Produção)", "Camila Prado(Produção)", "Helena Rocha(Diretoria)", "Marina Couto(Diretoria)",
            "Otávio Brandão(inativo)", "Ricardo Lemos(usuário indisponível)");
        tela.FindAll("label.aj-opcao").Where(l => l.QuerySelector("input")!.HasAttribute("checked")).Select(l => l.TextContent)
            .Should().Equal("Beatriz Lins(Produção)", "Otávio Brandão(inativo)", "Ricardo Lemos(usuário indisponível)");
        tela.FindAll("label.aj-rotulo--recuado").Should().HaveCount(3);
        tela.Markup.Should().Contain("Direcionamento de Beatriz Lins").And.Contain("Direcionamento de Ricardo Lemos")
            .And.NotContain("Direcionamento de Camila Prado");
        // O revisor começa em "manter / deduzir", com o atual no rótulo; só os diretores ativos são opção.
        tela.Find("select.aj-campo option[selected]").TextContent.Should().Be("Manter / deduzir (atual: Marina Couto)");
        tela.FindAll("select.aj-campo option").Select(o => o.TextContent).Should().Equal(
            "Manter / deduzir (atual: Marina Couto)", "Helena Rocha", "Marina Couto");
    }

    [Fact]
    public void Marcar_mostra_o_direcionamento_e_salvar_envia_os_marcados_aparados_com_o_revisor_e_a_versao()
    {
        var tela = Tela(Completa);

        Marcar(tela, "Camila Prado", true);
        tela.Markup.Should().Contain("Direcionamento de Camila Prado");
        Direcionar(tela, "Camila Prado", "  Conferir os cálculos das verbas rescisórias.  ");
        Marcar(tela, "Otávio Brandão", false);
        tela.Markup.Should().NotContain("Direcionamento de Otávio Brandão").And.Contain("Direcionamento de Beatriz Lins");
        tela.Find("select.aj-campo").Change(Helena.ToString());
        tela.Find("form#formulario-de-responsaveis").Submit();

        tela.WaitForAssertion(() => Destino.Should().EndWith($"/atividades/{Id}"));
        var enviado = Despachante.Recebidas.OfType<DefinirResponsaveisDaAtividade>().Should().ContainSingle().Subject;
        enviado.Id.Should().Be(Id);
        enviado.Responsaveis.Should().Equal(
            new ResponsavelEntrada(Beatriz, "Redigir a minuta com as condições discutidas na audiência."),
            new ResponsavelEntrada(Camila, "Conferir os cálculos das verbas rescisórias."),
            new ResponsavelEntrada(Ricardo, null));
        enviado.RevisorId.Should().Be(Helena);
        enviado.VersaoEsperada.Should().Be(7u);
        Despachante.Recebidas.OfType<TrocarRevisorDaAtividade>().Should().BeEmpty();
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Responsáveis salvos.");
    }

    [Fact]
    public void Manter_o_revisor_envia_revisor_nulo()
    {
        var tela = Tela(Completa);

        tela.Find("form#formulario-de-responsaveis").Submit();

        tela.WaitForAssertion(() => Despachante.Recebidas.OfType<DefinirResponsaveisDaAtividade>().Should().ContainSingle()
            .Which.RevisorId.Should().BeNull());
    }

    [Fact]
    public void Modo_do_revisor_mostra_so_o_revisor_com_o_atual_escolhido_e_troca_com_a_versao()
    {
        var tela = Tela(SoRevisor);

        tela.Find("h1.aj-titulo").TextContent.Should().Be("Trocar revisor");
        tela.Find("section.aj-painel h2").TextContent.Should().Be("Revisor");
        tela.FindAll("label.aj-opcao").Should().BeEmpty();
        tela.Markup.Should().NotContain("Salvar responsáveis").And.NotContain("Manter / deduzir").And.Contain("Trocar revisor</button>");
        tela.Find("select.aj-campo option[selected]").TextContent.Should().Be("Marina Couto");
        tela.FindAll("select.aj-campo option").Select(o => o.TextContent).Should().Equal("Selecione…", "Helena Rocha", "Marina Couto");

        tela.Find("select.aj-campo").Change(Helena.ToString());
        tela.Find("form#formulario-de-responsaveis").Submit();

        tela.WaitForAssertion(() => Destino.Should().EndWith($"/atividades/{Id}"));
        Despachante.Recebidas.OfType<TrocarRevisorDaAtividade>().Should().ContainSingle()
            .Which.Should().Be(new TrocarRevisorDaAtividade(Id, Helena, 7u));
        Despachante.Recebidas.OfType<DefinirResponsaveisDaAtividade>().Should().BeEmpty();
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Revisor alterado.");
    }

    [Fact]
    public void Modo_do_revisor_sem_escolha_mostra_o_erro_no_campo_e_nao_envia()
    {
        // O revisor de hoje é um diretor inativo: não é opção, e o seletor começa em "Selecione…".
        var tela = Tela(SoRevisor, Davi, "Davi Duarte");
        tela.Find("select.aj-campo option[selected]").TextContent.Should().Be("Selecione…");

        tela.Find("form#formulario-de-responsaveis").Submit();

        tela.Markup.Should().Contain("Escolha o novo revisor.").And.Contain("aj-rotulo aj-rotulo--erro");
        Despachante.Recebidas.OfType<TrocarRevisorDaAtividade>().Should().BeEmpty();
    }

    [Fact]
    public void Sem_nenhuma_das_permissoes_avisa_sem_formulario_e_sem_consultar_os_usuarios()
    {
        var tela = Tela(Nenhuma);

        tela.Markup.Should().Contain("Esta atividade não permite mais essa ação.");
        tela.FindAll("form#formulario-de-responsaveis").Should().BeEmpty();
        tela.Markup.Should().NotContain("Salvar responsáveis");
        Despachante.Recebidas.OfType<ListarUsuarios>().Should().BeEmpty();
    }

    [Fact]
    public void Producao_ve_acesso_negado_sem_consultar_a_atividade()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaProducao);

        var tela = Desenhar<AtividadeResponsaveis>(p => p.Add(t => t.Id, Id));

        tela.Markup.Should().Contain("Acesso negado.");
        tela.FindAll("form#formulario-de-responsaveis").Should().BeEmpty();
        Despachante.Recebidas.Should().ContainSingle().Which.Should().BeOfType<ObterMeuAcesso>();
    }

    [Fact]
    public void Falha_da_consulta_dos_usuarios_oferece_tentar_de_novo_que_repete_a_carga_inteira()
    {
        var chamadas = 0;
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(
                Resultado<AtividadeDetalhe>.Ok(Atividade(Completa, Marina, "Marina Couto")))
            .Responde<ListarUsuarios, Resultado<IReadOnlyList<UsuarioResumo>>>(_ => ++chamadas == 1
                ? Resultado<IReadOnlyList<UsuarioResumo>>.Falha(TipoErro.Validacao, "Não foi possível consultar os usuários.")
                : Resultado<IReadOnlyList<UsuarioResumo>>.Ok(Usuarios));
        var tela = Desenhar<AtividadeResponsaveis>(p => p.Add(t => t.Id, Id));

        tela.Markup.Should().Contain("Não foi possível consultar os usuários.");
        tela.FindAll("form#formulario-de-responsaveis").Should().BeEmpty();
        tela.FindAll("button").Single(b => b.TextContent == "Tentar de novo").Click();

        tela.WaitForAssertion(() => tela.FindAll("label.aj-opcao").Should().NotBeEmpty());
        tela.Markup.Should().NotContain("Não foi possível consultar os usuários.");
        // "Tentar de novo" repete a carga inteira: o perfil e a atividade também.
        Despachante.Recebidas.OfType<ObterMeuAcesso>().Should().HaveCount(2);
        Despachante.Recebidas.OfType<ObterAtividade>().Should().HaveCount(2);
    }

    [Fact]
    public void Falha_ao_salvar_mostra_o_aviso_e_mantem_o_que_foi_marcado()
    {
        var tela = Tela(Completa);
        Despachante.Responde<DefinirResponsaveisDaAtividade, Resultado>(
            Resultado.Falha(TipoErro.Conflito, "A atividade foi alterada por outra pessoa. Recarregue e tente novamente."));
        Marcar(tela, "Camila Prado", true);

        tela.Find("form#formulario-de-responsaveis").Submit();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("A atividade foi alterada por outra pessoa. Recarregue e tente novamente."));
        tela.FindAll("label.aj-opcao").Single(l => l.TextContent.StartsWith("Camila Prado"))
            .QuerySelector("input")!.HasAttribute("checked").Should().BeTrue();
        Destino.Should().NotEndWith($"/atividades/{Id}");
    }

    [Fact]
    public void Depois_de_salvar_com_sucesso_um_novo_envio_nao_dispara_outro_salvamento()
    {
        var resposta = new TaskCompletionSource<Resultado>();
        var tela = Tela(Completa);
        Despachante.RespondeDepois<DefinirResponsaveisDaAtividade, Resultado>(resposta.Task);

        tela.Find("form#formulario-de-responsaveis").Submit();
        resposta.SetResult(Resultado.Ok());
        tela.WaitForAssertion(() => Destino.Should().EndWith($"/atividades/{Id}"));
        tela.Find("form#formulario-de-responsaveis").Submit();

        Despachante.Recebidas.OfType<DefinirResponsaveisDaAtividade>().Should().ContainSingle();
        tela.FindAll("button[type=submit]").Should().HaveCount(3).And.OnlyContain(b => b.HasAttribute("disabled"));
    }
}
```

Observações para quem implementa:

- O `select` de um `@bind` desenha a opção escolhida com `selected` no bUnit (a 9D usou `option[selected]` no polo do processo). No HTML pré-renderizado, aceite `selected` ou `selected="selected"` na expressão, se preciso.
- O `textarea` com `@bind` pode sair no HTML com o texto dentro ou num atributo `value`: o teste HTTP procura o direcionamento com `Contain`, que vale para as duas formas.

Nas theories de rota, a rota nova:

- `tests/AtividadesJuridicas.IntegrationTests/PortalDoClienteWebTests.cs`, em `Cliente_em_pagina_interna_ve_nao_encontrada_e_nenhum_dado`, logo depois de `[InlineData("/atividades")]`: `[InlineData("/atividades/0199a3f2-7c1e-7000-8000-000000000001/responsaveis")]`.
- `tests/AtividadesJuridicas.IntegrationTests/AutenticacaoWebTests.cs`, em `Anonimo_e_redirecionado_ao_login_em_qualquer_rota`, depois de `[InlineData("/processos/0199a3f2-7c1e-7000-8000-000000000001/editar")]`: `[InlineData("/atividades/0199a3f2-7c1e-7000-8000-000000000001/responsaveis")]`.

Run: `dotnet build -m:1`
Expected: não compila (`AtividadeResponsaveis` não existe). É o RED.

- [ ] **Step 2: A tela**

Releia antes os três quadros (`Responsaveis.dc.html`, `ResponsaveisTablet.dc.html`, `ResponsaveisMobile.dc.html`) e a seção 3 de `quadros-lista-e-formularios.md`. A lógica das opções é a do `Components/Atividades/EditorDeResponsaveis.razor` de hoje (que fica no repositório até a Tarefa 14), sem mudar; a da troca de revisor é a do painel "Revisor" da ficha antiga (`PrepararTrocaDeRevisorAsync` e `TrocarRevisorAsync`).

`src/AtividadesJuridicas.Web/Components/Pages/AtividadeResponsaveis.razor`:

```razor
@page "/atividades/{Id:guid}/responsaveis"
@inject IDespachante Despachante
@inject NavigationManager Navegacao
@inject Recados Recados

<PageTitle>@Titulo</PageTitle>

<CabecalhoDeDetalhe Titulo="@Titulo" Subtitulo="@Subtitulo" VoltarPara="@Voltar" Origem="@Origem"
                    RotuloDoVoltarNoMobile="@RotuloDoVoltarNoMobile" />

<CargaDoFormulario Estado="_estado" Erro="@_erroDaConsulta" TextoNaoEncontrado="Atividade não encontrada." TentarDeNovo="CarregarAsync">
    @if (!Permitida)
    {
        <Aviso Tipo="TipoDeAviso.Atencao">Esta atividade não permite mais essa ação.</Aviso>
    }
    else
    {
        @if (_mensagem is not null)
        {
            <Aviso Tipo="TipoDeAviso.Erro">@_mensagem</Aviso>
        }

        @* A barra de voltar do mobile não tem subtítulo: ele vira a ajuda no topo do conteúdo. *@
        <div class="aj-so-mobile">
            <span class="aj-ajuda">@Subtitulo</span>
        </div>

        <EditForm Model="_form" OnSubmit="SalvarAsync" FormName="responsaveis" id="formulario-de-responsaveis" class="aj-lado-a-lado aj-lado-a-lado--formulario">
            @if (ModoCompleto)
            {
                <section class="aj-painel aj-painel--principal aj-painel--sempre-destacado aj-lado-a-lado__lista">
                    <h2>@_detalhe!.Titulo</h2>
                    @* A opção e o direcionamento são irmãos no mesmo pai (o vão do painel separa todos): as chaves dos dois
                       tipos nunca coincidem, senão o Blazor reaproveita a opção para o direcionamento ao marcar. *@
                    @foreach (var linha in _form.Linhas)
                    {
                        <label class="aj-opcao" @key="linha.UsuarioId"><input type="checkbox" class="aj-opcao__caixa" checked="@linha.Marcado" disabled="@_salvando" @onchange="e => Marcar(linha, e)" />@linha.Nome<span class="aj-opcao__nota">@linha.Nota</span></label>
                        @if (linha.Marcado)
                        {
                            <label class="aj-rotulo aj-rotulo--recuado" @key='("direcionamento", linha.UsuarioId)'>@($"Direcionamento de {linha.Nome}")<textarea class="aj-campo" rows="2" maxlength="@AtividadeResponsavel.TamanhoMaximoDirecionamento" disabled="@_salvando" @bind="linha.Direcionamento"></textarea></label>
                        }
                    }
                </section>

                <section class="aj-painel aj-lado-a-lado__painel">
                    <h2>Revisor</h2>
                    <label class="aj-rotulo"><span class="aj-so-leitor">Revisor</span><select class="aj-campo" disabled="@_salvando" @bind="_form.RevisorId"><option value="@Guid.Empty">@TextoDoManter</option>@foreach (var diretor in _diretores)
                        {<option value="@diretor.Id">@diretor.Nome</option>}</select></label>
                </section>
            }
            else
            {
                <section class="aj-painel aj-painel--principal aj-painel--sempre-destacado aj-painel--medio aj-lado-a-lado__lista">
                    <h2>Revisor</h2>
                    <label class="@ClasseDoRevisor"><span class="aj-so-leitor">Revisor</span><select class="aj-campo" disabled="@_salvando" @bind="_form.RevisorId"><option value="@Guid.Empty">Selecione…</option>@foreach (var diretor in _diretores)
                        {<option value="@diretor.Id">@diretor.Nome</option>}</select>@if (_erroDoRevisor is not null)
                        {<span class="aj-campo-erro">@_erroDoRevisor</span>}</label>
                </section>
            }
        </EditForm>

        @* As barras de ação ficam fora do formulário e o enviam pelo atributo form. *@
        <div class="aj-so-desktop">
            <div class="aj-acoes aj-acoes--fixa"><a href="@Voltar" class="aj-botao aj-botao--fantasma">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" form="formulario-de-responsaveis" disabled="@_salvando"><Icone Nome="confirmar" />@TextoDoSalvar</button></div>
        </div>
        <div class="aj-so-tablet">
            <div class="aj-acoes aj-acoes--fixa"><a href="@Voltar" class="aj-botao aj-botao--secundario">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" form="formulario-de-responsaveis" disabled="@_salvando"><Icone Nome="confirmar" />@TextoDoSalvar</button></div>
        </div>
        <div class="aj-so-mobile">
            <div class="aj-acoes aj-acoes--fixa"><a href="@Voltar" class="aj-botao aj-botao--secundario">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" form="formulario-de-responsaveis" disabled="@_salvando"><Icone Nome="confirmar" />Salvar</button></div>
        </div>
    }
</CargaDoFormulario>

@code {
    private const string TextoDaAjuda =
        "Marque quem é responsável e escreva o direcionamento de cada um. Quem está desmarcado deixa de ser responsável.";

    [Parameter] public Guid Id { get; set; }

    private EstadoDeCarga _estado = EstadoDeCarga.Carregando;
    private string? _erroDaConsulta;
    private AtividadeDetalhe? _detalhe;
    private IReadOnlyList<UsuarioResumo> _diretores = [];
    private Formulario _form = new();
    private string? _mensagem;
    private string? _erroDoRevisor;
    private bool _salvando;

    private bool Carregada => _estado == EstadoDeCarga.Pronto && _detalhe is not null;

    // Com DefinirResponsaveis, a tela inteira (o painel Revisor dela também troca o revisor); só com TrocarRevisor
    // (atividade feita ou corrigida), só o revisor; sem as duas, o aviso.
    private bool ModoCompleto => Carregada && _detalhe!.Acoes.DefinirResponsaveis;
    private bool ModoDoRevisor => Carregada && !_detalhe!.Acoes.DefinirResponsaveis && _detalhe.Acoes.TrocarRevisor;
    private bool Permitida => ModoCompleto || ModoDoRevisor;

    private string Titulo => ModoDoRevisor ? "Trocar revisor" : "Responsáveis";

    // O subtítulo só acompanha o formulário: na tela inteira, a ajuda do quadro; na troca de revisor, o título da
    // atividade (o painel principal com o título não existe nesse modo).
    private string? Subtitulo => ModoCompleto ? TextoDaAjuda : ModoDoRevisor ? _detalhe!.Titulo : null;

    // Sem a atividade para voltar a ela, o voltar vai para a lista.
    private string Voltar => _estado == EstadoDeCarga.NaoEncontrado ? "/atividades" : $"/atividades/{Id}";
    private string Origem => _estado == EstadoDeCarga.NaoEncontrado ? "Atividades" : _detalhe?.Titulo ?? "Atividade";
    private string? RotuloDoVoltarNoMobile => _estado == EstadoDeCarga.NaoEncontrado ? null : "Voltar para a atividade";

    private string TextoDoSalvar => ModoCompleto ? "Salvar responsáveis" : "Trocar revisor";
    private string TextoDoManter => _detalhe?.RevisorNome is { } atual ? $"Manter / deduzir (atual: {atual})" : "Manter / deduzir";
    private string ClasseDoRevisor => _erroDoRevisor is null ? "aj-rotulo" : "aj-rotulo aj-rotulo--erro";

    protected override Task OnInitializedAsync() => CarregarAsync();

    // A carga inteira, também pelo "Tentar de novo": o perfil, a atividade e, se ela ainda permite a ação, os usuários.
    private async Task CarregarAsync()
    {
        _estado = EstadoDeCarga.Carregando;
        _erroDaConsulta = null;
        _mensagem = null;
        _erroDoRevisor = null;
        _detalhe = null;

        // O perfil vem antes de tudo: a Produção não consulta a atividade.
        if (await Despachante.EnviarAsync(new ObterMeuAcesso()) is not { EhDiretoria: true })
        {
            _estado = EstadoDeCarga.AcessoNegado;
            return;
        }

        var atividade = await Despachante.EnviarAsync(new ObterAtividade(Id));
        if (atividade.Erro == TipoErro.AcessoNegado)
        {
            _estado = EstadoDeCarga.AcessoNegado;
            return;
        }

        if (atividade.Erro == TipoErro.NaoEncontrado)
        {
            _estado = EstadoDeCarga.NaoEncontrado;
            return;
        }

        if (atividade.Valor is not { } detalhe)
        {
            _erroDaConsulta = atividade.Mensagem ?? "Não foi possível consultar a atividade.";
            _estado = EstadoDeCarga.Falhou;
            return;
        }

        // Sem nenhuma das duas permissões a tela só avisa: os usuários nem são consultados.
        if (detalhe.Acoes.DefinirResponsaveis || detalhe.Acoes.TrocarRevisor)
        {
            var usuarios = await Despachante.EnviarAsync(new ListarUsuarios());
            if (usuarios.Erro == TipoErro.AcessoNegado)
            {
                _estado = EstadoDeCarga.AcessoNegado;
                return;
            }

            if (usuarios.Valor is not { } lista)
            {
                _erroDaConsulta = usuarios.Mensagem ?? "Não foi possível consultar os usuários.";
                _estado = EstadoDeCarga.Falhou;
                return;
            }

            _diretores = [.. lista.Where(u => u.Ativo && u.TipoPerfil == TipoPerfil.Diretoria)
                .OrderBy(u => u.Nome, StringComparer.CurrentCultureIgnoreCase)];
            _form = new Formulario
            {
                Linhas = Linhas(detalhe, lista),
                // Na tela inteira o revisor começa em "manter / deduzir"; na troca, no atual, se ele ainda é opção.
                RevisorId = !detalhe.Acoes.DefinirResponsaveis && _diretores.Any(d => d.Id == detalhe.RevisorId)
                    ? detalhe.RevisorId!.Value
                    : Guid.Empty,
            };
        }

        _detalhe = detalhe;
        _estado = EstadoDeCarga.Pronto;
    }

    /// <summary>
    /// As opções: os usuários ativos que não são de cliente, por nome, marcados se já são responsáveis; depois os
    /// responsáveis de hoje que não entraram (inativos, ou fora da lista), sempre marcados.
    /// </summary>
    private static List<Linha> Linhas(AtividadeDetalhe detalhe, IReadOnlyList<UsuarioResumo> usuarios)
    {
        var atuais = detalhe.Responsaveis.ToDictionary(r => r.UsuarioId);
        var linhas = new List<Linha>();

        foreach (var usuario in usuarios.Where(u => u.Ativo && u.TipoPerfil != TipoPerfil.Cliente)
                     .OrderBy(u => u.Nome, StringComparer.CurrentCultureIgnoreCase))
        {
            atuais.TryGetValue(usuario.Id, out var atual);
            linhas.Add(new Linha
            {
                UsuarioId = usuario.Id, Nome = usuario.Nome, Nota = $"({usuario.PerfilNome})",
                Marcado = atual is not null, Direcionamento = atual?.Direcionamento,
            });
        }

        foreach (var atual in detalhe.Responsaveis.Where(r => linhas.All(l => l.UsuarioId != r.UsuarioId)))
        {
            var usuario = usuarios.FirstOrDefault(u => u.Id == atual.UsuarioId);
            linhas.Add(new Linha
            {
                UsuarioId = atual.UsuarioId, Nome = usuario?.Nome ?? atual.Nome,
                Nota = usuario is null ? "(usuário indisponível)" : "(inativo)",
                Marcado = true, Direcionamento = atual.Direcionamento,
            });
        }

        return linhas;
    }

    private static void Marcar(Linha linha, ChangeEventArgs e) => linha.Marcado = e.Value is true;

    private async Task SalvarAsync()
    {
        if (_salvando || _detalhe is not { } detalhe) return;

        var completo = detalhe.Acoes.DefinirResponsaveis;
        _erroDoRevisor = null;
        if (!completo && _form.RevisorId == Guid.Empty)
        {
            _erroDoRevisor = "Escolha o novo revisor.";
            return;
        }

        _salvando = true;
        var navegou = false;
        try
        {
            _mensagem = null;
            Resultado resultado;
            string recado;
            if (completo)
            {
                // Exatamente as linhas marcadas na tela; direcionamento em branco vai como nulo.
                List<ResponsavelEntrada> enviados = [.. _form.Linhas.Where(l => l.Marcado).Select(l => new ResponsavelEntrada(
                    l.UsuarioId, string.IsNullOrWhiteSpace(l.Direcionamento) ? null : l.Direcionamento.Trim()))];
                resultado = await Despachante.EnviarAsync(new DefinirResponsaveisDaAtividade(
                    Id, enviados, _form.RevisorId == Guid.Empty ? null : _form.RevisorId, detalhe.Versao));
                recado = "Responsáveis salvos.";
            }
            else
            {
                resultado = await Despachante.EnviarAsync(new TrocarRevisorDaAtividade(Id, _form.RevisorId, detalhe.Versao));
                recado = "Revisor alterado.";
            }

            if (resultado.Erro == TipoErro.AcessoNegado)
            {
                _estado = EstadoDeCarga.AcessoNegado;
                return;
            }

            if (!resultado.Sucesso)
            {
                // Conflito, não encontrada ou validação: a mensagem do servidor no topo, e o formulário fica como está.
                _mensagem = resultado.Mensagem;
                return;
            }

            Recados.Deixar(recado);
            navegou = true;
            Navegacao.NavigateTo(Voltar);
        }
        finally
        {
            // Depois do sucesso a página vai embora: o botão fica travado para um clique tardio não salvar de novo.
            if (!navegou) _salvando = false;
        }
    }

    /// <summary>Uma opção da tela: tudo o que consta na lista aparece aqui, e só o que está marcado é enviado.</summary>
    private sealed class Linha
    {
        public Guid UsuarioId { get; init; }
        public string Nome { get; init; } = string.Empty;

        /// <summary>O complemento do nome: o perfil, "(inativo)" ou "(usuário indisponível)".</summary>
        public string Nota { get; init; } = string.Empty;

        public bool Marcado { get; set; }
        public string? Direcionamento { get; set; }
    }

    private sealed class Formulario
    {
        public List<Linha> Linhas { get; init; } = [];
        public Guid RevisorId { get; set; }
    }
}
```

Notas da marcação:

- O `EditForm` usa `OnSubmit` (não há validação por anotação aqui): o único erro de campo é o do revisor na troca, escrito à mão no rótulo (`aj-rotulo--erro` e `span.aj-campo-erro`, a mesma forma do `RotuloDeCampo`), porque o rótulo do quadro é só para leitor (`span.aj-so-leitor`) e o `RotuloDeCampo` escreve o texto visível.
- O texto `Direcionamento de {nome}` é uma expressão só (`@($"…")`), e o do "Manter / deduzir" também (`TextoDoManter`), como a regra de texto procurado por teste exige.
- No modo do revisor o painel fica sozinho no arranjo: `aj-lado-a-lado__lista` com `aj-painel--medio` (680px no máximo no desktop, a largura toda no tablet e no mobile).
- As barras de ação são as do formulário de processo; no mobile o botão diz `Salvar` nos dois modos.
- A nota da opção é o nome do perfil (`PerfilNome`), como no editor de hoje: os perfis são cadastráveis, e o `(Produção)` e o `(Diretoria)` do quadro são exemplos. Com os perfis semeados, a Diretoria aparece como `(Diretor)` (o teste HTTP afirma isso).

- [ ] **Step 3: Trava e testes**

Acrescente a `Migradas` (em `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`, na ordem do arquivo, antes de `Components/Pages/Auditoria.razor`): `"Components/Pages/AtividadeResponsaveis.razor",`.

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Expected: todos passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadeResponsaveis|FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~AutenticacaoWebTests"`
Expected: todos passando (os 5 testes HTTP, os 10 bUnit e as duas linhas novas das theories).

- [ ] **Step 4: Commit**

Confira o branch (`git branch --show-current` deve dizer `worktree-feat+fase-9e-atividades`) e:

```
git add src tests
git commit -m "feat(web): responsáveis e troca de revisor em tela própria, com o direcionamento sob a opção marcada"
```

---

### Task 13: Devolver e reabrir em tela própria

**Files:**
- Create: `src/AtividadesJuridicas.Web/Components/Atividades/MotivoComResponsaveis.razor` (a tela compartilhada), `src/AtividadesJuridicas.Web/Components/Pages/AtividadeDevolver.razor` (rota `/atividades/{Id:guid}/devolver`), `AtividadeReabrir.razor` (rota `/atividades/{Id:guid}/reabrir`)
- Create: `tests/AtividadesJuridicas.IntegrationTests/AtividadeDevolucaoWebTests.cs`, `AtividadeDevolucaoInterativaTests.cs`
- Modify: `PortalDoClienteWebTests.cs`, `AutenticacaoWebTests.cs`, `TelasNoDesignSystemTests.cs` (`Migradas`)

**Interfaces:**
- Consumes: `CargaDoFormulario`, `CabecalhoDeDetalhe`, `Aviso`, `Recados`; `ObterMeuAcesso`, `ObterAtividade`, `DevolverAtividade`, `ReabrirAtividade`.
- Produces: `<MotivoComResponsaveis Id="…" Reabrir="…" />`; as duas rotas.

**Regras:**
- Quadros: `Devolver.dc.html`, `…Tablet`, `…Mobile` (`quadros-lista-e-formularios.md`, seção 4). Reabrir (spec 8): a mesma tela com o título `Reabrir atividade` e o botão `Reabrir`.
- Permissão pela atividade carregada: devolver exige `AprovarOuDevolver`; reabrir, `Reabrir`; sem ela, `Esta atividade não permite mais essa ação.` com o voltar.
- O formulário é `id="formulario-do-motivo"` (o mesmo nas duas rotas), e o `EditForm` é o próprio painel `aj-painel aj-painel--principal aj-painel--medio`, com `Motivo` (`textarea rows="4"`, `maxlength` 2000) e o `fieldset.aj-conjunto` com a legenda `Responsáveis que voltam a ter a parte pendente` e uma `aj-opcao` por responsável, todos marcados ao abrir. Erros: `Informe o motivo.` e `Marque ao menos um responsável.` (sem envio). Ações: desktop `aj-acoes aj-acoes--medio` no fluxo (`Cancelar` fantasma, botão primário com o ícone `devolver` e `Devolver`/`Reabrir`); tablet e mobile `aj-acoes--fixa` (`Cancelar` secundário). No mobile, o título da atividade vira `aj-ajuda` no topo.
- Envio: `DevolverAtividade`/`ReabrirAtividade` com o motivo aparado e só os marcados; sucesso volta à ficha com `Atividade devolvida para correção.`/`Atividade reaberta.`. Falha: aviso de erro, o digitado fica; acesso negado encerra. Produção: "Acesso negado." sem consulta. Guarda de clique duplo.
- Testes HTTP (as duas rotas, títulos e botões, permissão, Produção, não encontrada, textos de cada visão) e bUnit (Review Focus 5: sem motivo ou sem marcado não envia; desmarcar um envia só os outros; sucesso navega com o recado; clique duplo).

- [ ] **Step 1: Testes (devem falhar)**

`tests/AtividadesJuridicas.IntegrationTests/AtividadeDevolucaoWebTests.cs` (HTTP, contra o banco). Duas atividades com o Pedro (Produção) responsável e a Diana (Diretoria) revisora: uma Feita (devolver vale, reabrir não) e uma Concluída (reabrir vale, devolver não).

```csharp
using System.Net;
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

[Collection(ColecaoBanco.Nome)]
public class AtividadeDevolucaoWebTests(BancoFixture banco) : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _app = new(banco);
    private Guid _admin, _pedro, _feita, _concluida;

    public async Task InitializeAsync()
    {
        _admin = await _app.IdDoUsuarioAsync(AplicacaoDeTeste.EmailDoAdmin);
        _pedro = await _app.CriarPedroAsync();

        _feita = await FeitaAsync("Minuta de acordo trabalhista");
        _concluida = await FeitaAsync("Revisão de contrato de locação");
        (await _app.ExecutarAsync(_admin, new AprovarAtividade(_concluida))).Sucesso.Should().BeTrue();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    // Direcionada ao Pedro, com a Diana revisora, e a parte do Pedro concluída: a atividade fica Feita.
    private async Task<Guid> FeitaAsync(string titulo)
    {
        var criada = await _app.ExecutarAsync(_admin, new SalvarAtividade(
            null, titulo, "Descrição da atividade.", new DateOnly(2026, 10, 9), null, null, null, 2));
        criada.Sucesso.Should().BeTrue(criada.Mensagem);
        (await _app.ExecutarAsync(_admin, new DefinirResponsaveisDaAtividade(
            criada.Valor, [new ResponsavelEntrada(_pedro, "Redigir.")], _admin))).Sucesso.Should().BeTrue();
        (await _app.ExecutarAsync(_pedro, new ConcluirMinhaParte(criada.Valor))).Sucesso.Should().BeTrue();
        return criada.Valor;
    }

    private async Task<string> PaginaAsync(string rota) =>
        WebUtility.HtmlDecode(await (await _app.ClienteAutenticadoAsync()).GetStringAsync(rota));

    [Fact]
    public async Task Devolver_atividade_feita_traz_o_motivo_os_responsaveis_marcados_e_as_acoes_de_cada_visao()
    {
        var html = await PaginaAsync($"/atividades/{_feita}/devolver");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Devolver para correção</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Minuta de acordo trabalhista</p>")
            .And.MatchRegex($"(?s)<a href=\"/atividades/{_feita}\" class=\"aj-voltar\"><svg[^>]*>.*?</svg>Minuta de acordo trabalhista</a>")
            .And.MatchRegex("<form[^>]*id=\"formulario-do-motivo\"[^>]*class=\"aj-painel aj-painel--principal aj-painel--medio\"")
            .And.MatchRegex("(?s)<label class=\"aj-rotulo\">\\s*Motivo\\s*<textarea[^>]*maxlength=\"2000\"")
            // O InputTextArea soma à classe a do estado do campo ("valid"): por isso o [ "] depois de aj-campo.
            .And.MatchRegex("<textarea(?=[^>]*rows=\"4\")[^>]*class=\"aj-campo[ \"]")
            .And.MatchRegex("(?s)<fieldset class=\"aj-conjunto\">\\s*<legend class=\"aj-rotulo\">Responsáveis que voltam a ter a parte pendente</legend>\\s*<label class=\"aj-opcao\"><input type=\"checkbox\" class=\"aj-opcao__caixa\" checked[^>]*>Pedro Produção</label>");
        html.Should().NotContain("Esta atividade não permite mais essa ação.").And.NotContain("Informe o motivo.");

        // Desktop: a barra de ações no fluxo, com 680px no máximo, logo abaixo do painel.
        var desktop = BlocoDeVisao.De(html, "aj-so-desktop");
        desktop.Should().MatchRegex(
            $"(?s)<div class=\"aj-acoes aj-acoes--medio\">\\s*<a href=\"/atividades/{_feita}\" class=\"aj-botao aj-botao--fantasma\">Cancelar</a>\\s*<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-do-motivo\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Devolver</button>");
        desktop.Should().NotContain("aj-acoes--fixa");
        // Tablet e mobile: a barra fixa embaixo, com Cancelar secundário.
        var tablet = BlocoDeVisao.De(html, "aj-so-tablet");
        tablet.Should().MatchRegex(
            $"(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a href=\"/atividades/{_feita}\" class=\"aj-botao aj-botao--secundario\">Cancelar</a>\\s*<button type=\"submit\"[^>]*form=\"formulario-do-motivo\"[^>]*>.*?Devolver</button>");
        tablet.Should().NotContain("aj-acoes--medio");
        BlocoDeVisao.De(html, "aj-so-mobile").Should()
            .Contain("<span class=\"aj-ajuda\">Minuta de acordo trabalhista</span>")
            .And.MatchRegex("(?s)<div class=\"aj-acoes aj-acoes--fixa\">\\s*<a [^>]*class=\"aj-botao aj-botao--secundario\">Cancelar</a>.*?Devolver</button>");

        html.Should().MatchRegex($"<a href=\"/atividades/{_feita}\" class=\"aj-botao aj-botao--fantasma aj-botao--icone\" aria-label=\"Voltar para a atividade\">")
            .And.Contain("<h1>Devolver para correção</h1>");
    }

    [Fact]
    public async Task Reabrir_atividade_concluida_e_a_mesma_tela_com_o_titulo_e_o_botao_de_reabrir()
    {
        var html = await PaginaAsync($"/atividades/{_concluida}/reabrir");

        html.Should().MatchRegex("<h1 class=\"aj-titulo\">Reabrir atividade</h1>")
            .And.Contain("<p class=\"aj-subtitulo\">Revisão de contrato de locação</p>")
            .And.Contain("Responsáveis que voltam a ter a parte pendente");
        BlocoDeVisao.De(html, "aj-so-desktop").Should().MatchRegex(
            "(?s)<button type=\"submit\" class=\"aj-botao aj-botao--primario aj-botao--com-icone\" form=\"formulario-do-motivo\"[^>]*>\\s*<svg[^>]*>.*?</svg>\\s*Reabrir</button>");
        html.Should().NotContain("Devolver para correção").And.NotContain("Devolver</button>");
    }

    [Theory]
    [InlineData("devolver")]
    [InlineData("reabrir")]
    public async Task Acao_que_a_atividade_nao_permite_avisa_sem_formulario(string acao)
    {
        // Devolver uma concluída; reabrir uma feita.
        var atividade = acao == "devolver" ? _concluida : _feita;

        var html = await PaginaAsync($"/atividades/{atividade}/{acao}");

        html.Should().Contain("Esta atividade não permite mais essa ação.")
            .And.MatchRegex($"<a href=\"/atividades/{atividade}\" class=\"aj-voltar\">");
        html.Should().NotContain("formulario-do-motivo").And.NotContain("Responsáveis que voltam a ter a parte pendente");
    }

    [Fact]
    public async Task Atividade_que_nao_existe_mostra_nao_encontrada_com_o_voltar_para_a_lista()
    {
        var html = await PaginaAsync($"/atividades/{Guid.NewGuid()}/devolver");

        html.Should().Contain("Atividade não encontrada.")
            .And.MatchRegex("<a href=\"/atividades\" class=\"aj-voltar\">")
            .And.Contain("aria-label=\"Voltar para Atividades\"");
        html.Should().NotContain("formulario-do-motivo");
    }

    [Theory]
    [InlineData("devolver", "Devolver para correção")]
    [InlineData("reabrir", "Reabrir atividade")]
    public async Task Producao_ve_acesso_negado_sem_consultar_a_atividade(string acao, string titulo)
    {
        var pedro = await _app.ClienteAutenticadoAsync(AplicacaoDeTeste.EmailDoPedro, AplicacaoDeTeste.SenhaDoPedro);

        var html = WebUtility.HtmlDecode(await pedro.GetStringAsync($"/atividades/{_feita}/{acao}"));

        html.Should().Contain("Acesso negado.").And.MatchRegex($"<h1 class=\"aj-titulo\">{titulo}</h1>");
        html.Should().NotContain("Minuta de acordo trabalhista").And.NotContain("formulario-do-motivo");
        (await _app.EmEscopoAsync(null, sp => sp.GetRequiredService<AppDbContext>().RegistrosAcesso.AsNoTracking()
            .CountAsync(r => r.UsuarioId == _pedro && r.TipoRecurso == "Atividade" && r.RecursoId == _feita)))
            .Should().Be(0);
    }
}
```

`tests/AtividadesJuridicas.IntegrationTests/AtividadeDevolucaoInterativaTests.cs` (bUnit):

```csharp
using AtividadesJuridicas.Application.Atividades;
using AtividadesJuridicas.Application.Comum;
using AtividadesJuridicas.Application.Identidade;
using AtividadesJuridicas.Domain.Atividades;
using AtividadesJuridicas.Web.Components.Pages;
using AtividadesJuridicas.Web.Infra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AtividadesJuridicas.IntegrationTests;

public class AtividadeDevolucaoInterativaTests : TelaInterativa
{
    private static readonly Guid Id = Guid.Parse("0199a3f2-7c1e-7000-8000-0000000000a2");
    private static readonly Guid Beatriz = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000b001");
    private static readonly Guid Otavio = Guid.Parse("0199a3f2-7c1e-7000-8000-00000000b005");
    private static readonly DateTime Concluida = new(2026, 10, 8, 12, 15, 0, DateTimeKind.Utc);

    // Editar, DefinirResponsaveis, TrocarRevisor, ConcluirMinhaParte, AprovarOuDevolver, Reabrir, Excluir.
    private static readonly AcoesDaAtividade DaFeita = new(true, false, true, false, true, false, true);
    private static readonly AcoesDaAtividade DaConcluida = new(true, false, false, false, false, true, true);

    private static AtividadeDetalhe Atividade(StatusAtividade status, AcoesDaAtividade acoes) => new(
        Id, "Minuta de acordo trabalhista", "Redigir a minuta.", new DateOnly(2026, 10, 9), status, false,
        status == StatusAtividade.Concluida ? Concluida : null, null, null, null, null, null, null,
        TipoAutor.Usuario, "Diana Diretora", AtorDaDiretoria.Id, "Diana Diretora",
        [
            new ResponsavelDetalhe(Beatriz, "Beatriz Lins", true, null, Concluida),
            new ResponsavelDetalhe(Otavio, "Otávio Brandão", true, null, Concluida),
        ],
        [], acoes, 7u, 2);

    private IRenderedComponent<AtividadeDevolver> Devolver(AcoesDaAtividade? acoes = null)
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(
                Resultado<AtividadeDetalhe>.Ok(Atividade(StatusAtividade.Feita, acoes ?? DaFeita)))
            .Responde<DevolverAtividade, Resultado>(Resultado.Ok());
        return Desenhar<AtividadeDevolver>(p => p.Add(t => t.Id, Id));
    }

    private static void Escrever<T>(IRenderedComponent<T> tela, string motivo) where T : IComponent =>
        tela.Find("textarea.aj-campo").Change(motivo);

    private static void Marcar<T>(IRenderedComponent<T> tela, string nome, bool marcado) where T : IComponent =>
        tela.FindAll("fieldset.aj-conjunto label.aj-opcao").Single(l => l.TextContent == nome).QuerySelector("input")!.Change(marcado);

    private string Destino => Services.GetRequiredService<NavigationManager>().Uri;

    [Fact]
    public void Abre_com_o_titulo_da_atividade_e_todos_os_responsaveis_marcados()
    {
        var tela = Devolver();

        tela.Find("h1.aj-titulo").TextContent.Should().Be("Devolver para correção");
        tela.FindAll("fieldset.aj-conjunto label.aj-opcao").Select(l => l.TextContent).Should().Equal("Beatriz Lins", "Otávio Brandão");
        tela.FindAll("fieldset.aj-conjunto input").Should().HaveCount(2).And.OnlyContain(c => c.HasAttribute("checked"));
    }

    [Fact]
    public void Sem_motivo_mostra_o_erro_no_campo_e_nao_envia()
    {
        var tela = Devolver();
        Escrever(tela, "   ");

        tela.Find("form#formulario-do-motivo").Submit();

        tela.Markup.Should().Contain("Informe o motivo.").And.Contain("aj-rotulo aj-rotulo--erro")
            .And.NotContain("Marque ao menos um responsável.");
        Despachante.Recebidas.OfType<DevolverAtividade>().Should().BeEmpty();
    }

    [Fact]
    public void Sem_responsavel_marcado_mostra_o_erro_no_conjunto_e_nao_envia()
    {
        var tela = Devolver();
        Escrever(tela, "Rever a cláusula de quitação.");
        Marcar(tela, "Beatriz Lins", false);
        Marcar(tela, "Otávio Brandão", false);

        tela.Find("form#formulario-do-motivo").Submit();

        tela.Find("fieldset.aj-conjunto .aj-campo-erro").TextContent.Should().Be("Marque ao menos um responsável.");
        tela.Find("fieldset.aj-conjunto legend").ClassName.Should().Be("aj-rotulo aj-rotulo--erro");
        tela.Markup.Should().NotContain("Informe o motivo.");
        Despachante.Recebidas.OfType<DevolverAtividade>().Should().BeEmpty();
    }

    [Fact]
    public void Desmarcar_um_envia_so_o_outro_com_o_motivo_aparado_e_volta_a_ficha_com_o_recado()
    {
        var tela = Devolver();
        Escrever(tela, "  A cláusula de quitação precisa citar o período do contrato.  ");
        Marcar(tela, "Otávio Brandão", false);

        tela.Find("form#formulario-do-motivo").Submit();

        tela.WaitForAssertion(() => Destino.Should().EndWith($"/atividades/{Id}"));
        var enviado = Despachante.Recebidas.OfType<DevolverAtividade>().Should().ContainSingle().Subject;
        enviado.Id.Should().Be(Id);
        enviado.Motivo.Should().Be("A cláusula de quitação precisa citar o período do contrato.");
        enviado.Responsaveis.Should().Equal(Beatriz);
        Despachante.Recebidas.OfType<ReabrirAtividade>().Should().BeEmpty();
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Atividade devolvida para correção.");
    }

    [Fact]
    public void Reabrir_e_a_mesma_tela_e_envia_a_reabertura_com_o_recado()
    {
        Despachante
            .Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaDiretoria)
            .Responde<ObterAtividade, Resultado<AtividadeDetalhe>>(
                Resultado<AtividadeDetalhe>.Ok(Atividade(StatusAtividade.Concluida, DaConcluida)))
            .Responde<ReabrirAtividade, Resultado>(Resultado.Ok());
        var tela = Desenhar<AtividadeReabrir>(p => p.Add(t => t.Id, Id));

        tela.Find("h1.aj-titulo").TextContent.Should().Be("Reabrir atividade");
        tela.FindAll("button[type=submit]").Should().HaveCount(3).And.OnlyContain(b => b.TextContent == "Reabrir");
        Escrever(tela, "O cliente pediu uma nova versão.");
        tela.Find("form#formulario-do-motivo").Submit();

        tela.WaitForAssertion(() => Destino.Should().EndWith($"/atividades/{Id}"));
        var enviado = Despachante.Recebidas.OfType<ReabrirAtividade>().Should().ContainSingle().Subject;
        enviado.Motivo.Should().Be("O cliente pediu uma nova versão.");
        enviado.Responsaveis.Should().Equal(Beatriz, Otavio);
        Despachante.Recebidas.OfType<DevolverAtividade>().Should().BeEmpty();
        Services.GetRequiredService<Recados>().Retirar()!.Texto.Should().Be("Atividade reaberta.");
    }

    [Fact]
    public void Sem_a_permissao_avisa_e_nao_mostra_o_formulario()
    {
        // Devolver uma atividade que só permite reabrir.
        var tela = Devolver(DaConcluida);

        tela.Markup.Should().Contain("Esta atividade não permite mais essa ação.");
        tela.FindAll("form#formulario-do-motivo").Should().BeEmpty();
        tela.FindAll("button[type=submit]").Should().BeEmpty();
    }

    [Fact]
    public void Falha_ao_devolver_mostra_o_aviso_e_mantem_o_que_foi_digitado()
    {
        var tela = Devolver();
        Despachante.Responde<DevolverAtividade, Resultado>(
            Resultado.Falha(TipoErro.Validacao, "Somente o revisor da atividade pode aprová-la ou devolvê-la."));
        Escrever(tela, "Rever o valor da terceira parcela.");
        Marcar(tela, "Otávio Brandão", false);

        tela.Find("form#formulario-do-motivo").Submit();

        tela.WaitForAssertion(() => tela.Markup.Should().Contain("Somente o revisor da atividade pode aprová-la ou devolvê-la."));
        tela.Markup.Should().Contain("Rever o valor da terceira parcela.");
        tela.FindAll("fieldset.aj-conjunto label.aj-opcao").Single(l => l.TextContent == "Otávio Brandão")
            .QuerySelector("input")!.HasAttribute("checked").Should().BeFalse();
        Destino.Should().NotEndWith($"/atividades/{Id}");
    }

    [Fact]
    public void Depois_de_devolver_com_sucesso_um_novo_envio_nao_dispara_outra_devolucao()
    {
        var resposta = new TaskCompletionSource<Resultado>();
        var tela = Devolver();
        Despachante.RespondeDepois<DevolverAtividade, Resultado>(resposta.Task);
        Escrever(tela, "Rever a cláusula de quitação.");

        tela.Find("form#formulario-do-motivo").Submit();
        resposta.SetResult(Resultado.Ok());
        tela.WaitForAssertion(() => Destino.Should().EndWith($"/atividades/{Id}"));
        tela.Find("form#formulario-do-motivo").Submit();

        Despachante.Recebidas.OfType<DevolverAtividade>().Should().ContainSingle();
        tela.FindAll("button[type=submit]").Should().HaveCount(3).And.OnlyContain(b => b.HasAttribute("disabled"));
    }

    [Fact]
    public void Producao_ve_acesso_negado_sem_consultar_a_atividade()
    {
        Despachante.Responde<ObterMeuAcesso, UsuarioAutenticado?>(AtorDaProducao);

        var tela = Desenhar<AtividadeDevolver>(p => p.Add(t => t.Id, Id));

        tela.Markup.Should().Contain("Acesso negado.");
        tela.FindAll("form#formulario-do-motivo").Should().BeEmpty();
        Despachante.Recebidas.Should().ContainSingle().Which.Should().BeOfType<ObterMeuAcesso>();
    }
}
```

Observações para quem implementa:

- O motivo é um `InputTextArea` (liga no `change`): `Change(...)` no bUnit basta. O texto digitado aparece no `Markup` (o `textarea` do bUnit leva o valor), que é o que `Falha_ao_devolver…` confere; se o bUnit o puser num atributo `value`, a asserção `Contain` continua valendo.
- `ClassName` é a propriedade do AngleSharp para o atributo `class` (o `IElement` que o `Find` devolve).

Nas theories de rota, as duas rotas novas:

- `tests/AtividadesJuridicas.IntegrationTests/PortalDoClienteWebTests.cs`, em `Cliente_em_pagina_interna_ve_nao_encontrada_e_nenhum_dado`, depois de `[InlineData("/atividades")]` (e da linha de responsáveis da Tarefa 12): `[InlineData("/atividades/0199a3f2-7c1e-7000-8000-000000000001/devolver")]` e `[InlineData("/atividades/0199a3f2-7c1e-7000-8000-000000000001/reabrir")]`.
- `tests/AtividadesJuridicas.IntegrationTests/AutenticacaoWebTests.cs`, em `Anonimo_e_redirecionado_ao_login_em_qualquer_rota`, as mesmas duas.

Run: `dotnet build -m:1`
Expected: não compila (`AtividadeDevolver` e `AtividadeReabrir` não existem). É o RED.

- [ ] **Step 2: A tela compartilhada**

Releia antes os três quadros (`Devolver.dc.html`, `DevolverTablet.dc.html`, `DevolverMobile.dc.html`) e a seção 4 de `quadros-lista-e-formularios.md`. A lógica vem do painel Devolver/Reabrir da ficha antiga (`ConfirmarDevolucaoOuReaberturaAsync`, `MarcarResponsavel`, todos marcados ao abrir), sem mudar o que se envia.

`src/AtividadesJuridicas.Web/Components/Atividades/MotivoComResponsaveis.razor`:

```razor
@using System.ComponentModel.DataAnnotations
@inject IDespachante Despachante
@inject NavigationManager Navegacao
@inject Recados Recados

@* A tela de devolver e a de reabrir: o motivo e os responsáveis que voltam a ter a parte pendente. As duas páginas só
   passam o identificador da rota e qual das duas ações é. *@
<PageTitle>@Titulo</PageTitle>

<CabecalhoDeDetalhe Titulo="@Titulo" Subtitulo="@Subtitulo" VoltarPara="@Voltar" Origem="@Origem"
                    RotuloDoVoltarNoMobile="@RotuloDoVoltarNoMobile" />

<CargaDoFormulario Estado="_estado" Erro="@_erroDaConsulta" TextoNaoEncontrado="Atividade não encontrada." TentarDeNovo="CarregarAsync">
    @if (!Permitida)
    {
        <Aviso Tipo="TipoDeAviso.Atencao">Esta atividade não permite mais essa ação.</Aviso>
    }
    else
    {
        @if (_mensagem is not null)
        {
            <Aviso Tipo="TipoDeAviso.Erro">@_mensagem</Aviso>
        }

        @* A barra de voltar do mobile não tem subtítulo: o título da atividade vira a ajuda no topo. *@
        <div class="aj-so-mobile">
            <span class="aj-ajuda">@_detalhe!.Titulo</span>
        </div>

        <EditForm Model="_form" OnSubmit="EnviarAsync" FormName="@NomeDoFormulario" id="formulario-do-motivo" class="aj-painel aj-painel--principal aj-painel--medio">
            <DataAnnotationsValidator />
            <RotuloDeCampo Texto="Motivo" Para="() => _form.Motivo"><InputTextArea @bind-Value="_form.Motivo" rows="4" maxlength="@AtividadeHistoricoStatus.TamanhoMaximoMotivo" disabled="@_enviando" class="aj-campo" /></RotuloDeCampo>
            <fieldset class="aj-conjunto">
                <legend class="@ClasseDaLegenda">Responsáveis que voltam a ter a parte pendente</legend>
                @foreach (var responsavel in _detalhe.Responsaveis)
                {
                    <label class="aj-opcao" @key="responsavel.UsuarioId"><input type="checkbox" class="aj-opcao__caixa" checked="@_marcados.Contains(responsavel.UsuarioId)" disabled="@_enviando" @onchange="e => Marcar(responsavel.UsuarioId, e)" />@responsavel.Nome</label>
                }
                @if (_erroDosResponsaveis is not null)
                {
                    <span class="aj-campo-erro">@_erroDosResponsaveis</span>
                }
            </fieldset>
        </EditForm>

        @* As barras de ação ficam fora do formulário e o enviam pelo atributo form. No desktop a barra fica no fluxo,
           logo abaixo do painel e com a mesma largura máxima; no tablet e no mobile, presa embaixo. *@
        <div class="aj-so-desktop">
            <div class="aj-acoes aj-acoes--medio"><a href="@Voltar" class="aj-botao aj-botao--fantasma">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" form="formulario-do-motivo" disabled="@_enviando"><Icone Nome="devolver" />@Verbo</button></div>
        </div>
        <div class="aj-so-tablet">
            <div class="aj-acoes aj-acoes--fixa"><a href="@Voltar" class="aj-botao aj-botao--secundario">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" form="formulario-do-motivo" disabled="@_enviando"><Icone Nome="devolver" />@Verbo</button></div>
        </div>
        <div class="aj-so-mobile">
            <div class="aj-acoes aj-acoes--fixa"><a href="@Voltar" class="aj-botao aj-botao--secundario">Cancelar</a><button type="submit" class="aj-botao aj-botao--primario aj-botao--com-icone" form="formulario-do-motivo" disabled="@_enviando"><Icone Nome="devolver" />@Verbo</button></div>
        </div>
    }
</CargaDoFormulario>

@code {
    [Parameter, EditorRequired] public Guid Id { get; set; }

    /// <summary>Verdadeiro na tela de reabrir (atividade concluída); falso na de devolver (feita ou corrigida).</summary>
    [Parameter] public bool Reabrir { get; set; }

    private EstadoDeCarga _estado = EstadoDeCarga.Carregando;
    private string? _erroDaConsulta;
    private AtividadeDetalhe? _detalhe;
    private Formulario _form = new();
    private HashSet<Guid> _marcados = [];
    private string? _mensagem;
    private string? _erroDosResponsaveis;
    private bool _enviando;

    private string Titulo => Reabrir ? "Reabrir atividade" : "Devolver para correção";
    private string Verbo => Reabrir ? "Reabrir" : "Devolver";
    private string NomeDoFormulario => Reabrir ? "reabertura" : "devolucao";

    // A permissão vem da atividade carregada: devolver é do revisor numa atividade feita ou corrigida; reabrir, de
    // quem é da Diretoria numa concluída.
    private bool Permitida =>
        _estado == EstadoDeCarga.Pronto && _detalhe is { } d && (Reabrir ? d.Acoes.Reabrir : d.Acoes.AprovarOuDevolver);

    // O subtítulo (o título da atividade) só acompanha o formulário.
    private string? Subtitulo => Permitida ? _detalhe!.Titulo : null;

    // Sem a atividade para voltar a ela, o voltar vai para a lista.
    private string Voltar => _estado == EstadoDeCarga.NaoEncontrado ? "/atividades" : $"/atividades/{Id}";
    private string Origem => _estado == EstadoDeCarga.NaoEncontrado ? "Atividades" : _detalhe?.Titulo ?? "Atividade";
    private string? RotuloDoVoltarNoMobile => _estado == EstadoDeCarga.NaoEncontrado ? null : "Voltar para a atividade";

    private string ClasseDaLegenda => _erroDosResponsaveis is null ? "aj-rotulo" : "aj-rotulo aj-rotulo--erro";

    protected override Task OnInitializedAsync() => CarregarAsync();

    // A carga inteira, também pelo "Tentar de novo": o perfil e a atividade.
    private async Task CarregarAsync()
    {
        _estado = EstadoDeCarga.Carregando;
        _erroDaConsulta = null;
        _mensagem = null;
        _erroDosResponsaveis = null;
        _detalhe = null;

        // O perfil vem antes de tudo: a Produção não consulta a atividade.
        if (await Despachante.EnviarAsync(new ObterMeuAcesso()) is not { EhDiretoria: true })
        {
            _estado = EstadoDeCarga.AcessoNegado;
            return;
        }

        var atividade = await Despachante.EnviarAsync(new ObterAtividade(Id));
        if (atividade.Erro == TipoErro.AcessoNegado)
        {
            _estado = EstadoDeCarga.AcessoNegado;
            return;
        }

        if (atividade.Erro == TipoErro.NaoEncontrado)
        {
            _estado = EstadoDeCarga.NaoEncontrado;
            return;
        }

        if (atividade.Valor is not { } detalhe)
        {
            _erroDaConsulta = atividade.Mensagem ?? "Não foi possível consultar a atividade.";
            _estado = EstadoDeCarga.Falhou;
            return;
        }

        // O motivo começa vazio e todos os responsáveis começam marcados.
        _form = new Formulario();
        _marcados = [.. detalhe.Responsaveis.Select(r => r.UsuarioId)];
        _detalhe = detalhe;
        _estado = EstadoDeCarga.Pronto;
    }

    private void Marcar(Guid usuarioId, ChangeEventArgs e)
    {
        if (e.Value is true) _marcados.Add(usuarioId);
        else _marcados.Remove(usuarioId);
    }

    private async Task EnviarAsync(EditContext contexto)
    {
        if (_enviando || _detalhe is not { } detalhe) return;

        // Os dois erros aparecem juntos, cada um no seu campo, e nada é enviado.
        var motivoValido = contexto.Validate();
        _erroDosResponsaveis = _marcados.Count == 0 ? "Marque ao menos um responsável." : null;
        if (!motivoValido || _erroDosResponsaveis is not null) return;

        _enviando = true;
        var navegou = false;
        try
        {
            _mensagem = null;
            // Só o que está marcado na tela, na ordem dos responsáveis da atividade.
            IReadOnlyList<Guid> responsaveis = [.. detalhe.Responsaveis.Select(r => r.UsuarioId).Where(_marcados.Contains)];
            var motivo = _form.Motivo.Trim();
            var resultado = Reabrir
                ? await Despachante.EnviarAsync(new ReabrirAtividade(Id, motivo, responsaveis))
                : await Despachante.EnviarAsync(new DevolverAtividade(Id, motivo, responsaveis));

            if (resultado.Erro == TipoErro.AcessoNegado)
            {
                _estado = EstadoDeCarga.AcessoNegado;
                return;
            }

            if (!resultado.Sucesso)
            {
                // O aviso no topo com a mensagem do servidor; o motivo e as marcas ficam como estão.
                _mensagem = resultado.Mensagem;
                return;
            }

            Recados.Deixar(Reabrir ? "Atividade reaberta." : "Atividade devolvida para correção.");
            navegou = true;
            Navegacao.NavigateTo(Voltar);
        }
        finally
        {
            // Depois do sucesso a página vai embora: o botão fica travado para um clique tardio não enviar de novo.
            if (!navegou) _enviando = false;
        }
    }

    private sealed class Formulario
    {
        // O Required recusa também o motivo só com espaços.
        [Required(ErrorMessage = "Informe o motivo.")]
        public string Motivo { get; set; } = string.Empty;
    }
}
```

Notas da marcação:

- O `EditForm` é o próprio painel (`aj-painel aj-painel--principal aj-painel--medio`), como o formulário de usuário da 9C: o quadro desenha um `section` com o motivo e o `fieldset`, e o `form` ocupa o lugar dele com as mesmas classes.
- O `fieldset` leva só `aj-conjunto` (a Tarefa 1 tira a moldura do navegador e afasta a legenda 8px); a legenda é `aj-rotulo` e vira `aj-rotulo aj-rotulo--erro` com o erro, e a mensagem `Marque ao menos um responsável.` fica no fim do conjunto, em `aj-campo-erro`, a mesma forma do erro do motivo (`RotuloDeCampo`).
- O `@key` das opções é o usuário: no mesmo pai só há a legenda e a mensagem, sem chave.
- O botão de envio leva o ícone `devolver` nas duas telas (o contrato); o texto é `Devolver` ou `Reabrir` numa expressão só (`Verbo`), igual nas três visões (o quadro do mobile mantém `Devolver`).
- `EditContext` vem de `Microsoft.AspNetCore.Components.Forms`, que já está no `_Imports.razor`.

- [ ] **Step 3: As duas páginas**

`src/AtividadesJuridicas.Web/Components/Pages/AtividadeDevolver.razor`:

```razor
@page "/atividades/{Id:guid}/devolver"

<MotivoComResponsaveis Id="Id" Reabrir="false" />

@code {
    [Parameter] public Guid Id { get; set; }
}
```

`src/AtividadesJuridicas.Web/Components/Pages/AtividadeReabrir.razor`:

```razor
@page "/atividades/{Id:guid}/reabrir"

<MotivoComResponsaveis Id="Id" Reabrir="true" />

@code {
    [Parameter] public Guid Id { get; set; }
}
```

- [ ] **Step 4: Trava e testes**

Acrescente a `Migradas` (em `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`, na ordem do arquivo): `"Components/Atividades/MotivoComResponsaveis.razor",` (depois de `"Components/Atividades/EtiquetaDeSituacao.razor",`, antes de `Components/Documentos/…`), `"Components/Pages/AtividadeDevolver.razor",` e `"Components/Pages/AtividadeReabrir.razor",` (antes de `Components/Pages/AtividadeResponsaveis.razor`).

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests|FullyQualifiedName~WebSoPeloDespachante"`
Expected: todos passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~AtividadeDevolucao|FullyQualifiedName~PortalDoClienteWebTests|FullyQualifiedName~AutenticacaoWebTests"`
Expected: todos passando (os testes HTTP com as theories, os 9 bUnit e as quatro linhas novas das theories de rota).

- [ ] **Step 5: Commit**

Confira o branch (`git branch --show-current` deve dizer `worktree-feat+fase-9e-atividades`) e:

```
git add src tests
git commit -m "feat(web): devolver e reabrir em tela própria, com o motivo e os responsáveis num conjunto e os erros nos campos"
```

---

### Task 14: Limpeza dos componentes antigos

**Files:**
- Delete: `src/AtividadesJuridicas.Web/Components/Atividades/FormularioDeAtividade.razor`, `EditorDeResponsaveis.razor`
- Modify: `src/AtividadesJuridicas.Web/wwwroot/app.css` (só as regras que ficaram sem uso: `.comentarios`, `.comentarios li`, `.comentarios li span`, `.confirmacao`, `.confirmacao:focus`, `.inativo`)
- Modify: `TelasNoDesignSystemTests.cs` (um teste que confere que todo arquivo de `Components/Atividades/` e as páginas de atividade estão em `Migradas`)

**Regras:**
- Antes de apagar uma regra do `app.css`, procure a classe em todo `.razor` e `.cs` da Web; regra usada pela Agenda (9F) fica. `.aviso` e `p.aviso` ficam: a Agenda os monta por expressão (`<p class="@_classeDaMensagem">`). As outras regras órfãs que esta tarefa não apaga (`strong.erro`, `.validation-message`, a parte `.cartao` do seletor `.cartao, form.formulario:not(.filtros)`) vão para a nota da 9G na emenda da spec (Tarefa 15).
- Build sem avisos e os testes de unidade e os de integração da Web depois de apagar.

- [ ] **Step 1: Teste da trava (deve falhar)**

Em `tests/AtividadesJuridicas.UnitTests/TelasNoDesignSystemTests.cs`, logo depois do `[Theory]` `Tela_migrada_nao_tem_estilo_embutido_nem_classe_fora_do_design_system`:

```csharp
    // Toda tela de atividade fica na trava: os componentes de Components/Atividades e as páginas de atividade.
    // Um componente antigo que sobrou (marcação antiga, fora da lista) faz este teste falhar.
    [Fact]
    public void Toda_tela_de_atividade_esta_na_trava()
    {
        var web = Path.Combine(GravacoesPorForaTests.RaizDoRepositorio(), "src", "AtividadesJuridicas.Web");
        var telas = Directory.GetFiles(Path.Combine(web, "Components", "Atividades"), "*.razor")
            .Concat(Directory.GetFiles(Path.Combine(web, "Components", "Pages"), "Atividade*.razor"))
            .Select(caminho => Path.GetRelativePath(web, caminho).Replace('\\', '/'))
            .ToList();

        telas.Should().Contain("Components/Pages/Atividades.razor").And.Contain("Components/Atividades/EtiquetaDeSituacao.razor");
        telas.Should().BeSubsetOf(Migradas, "tela de atividade fora da trava pode voltar à marcação antiga");
    }
```

Run: `dotnet build -m:1`
Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests.Toda_tela_de_atividade_esta_na_trava"`
Expected: falha, com `Components/Atividades/EditorDeResponsaveis.razor` e `Components/Atividades/FormularioDeAtividade.razor` fora de `Migradas`. Se aparecer outro arquivo na mensagem, uma tarefa anterior esqueceu de acrescentá-lo à lista: pare e relate (não o acrescente aqui sem conferir que ele passa na trava).

- [ ] **Step 2: Conferir que ninguém usa os dois componentes antigos**

Com a ferramenta Grep (ou `rg -n "FormularioDeAtividade|EditorDeResponsaveis" src tests`), procure `FormularioDeAtividade|EditorDeResponsaveis` em `src/` e `tests/`.

Expected: só as definições (os próprios `FormularioDeAtividade.razor` e `EditorDeResponsaveis.razor`). A ficha antiga, que os usava, saiu na Tarefa 10; os testes do formulário antigo passaram para a página nova na Tarefa 7. Se algum outro arquivo os citar (um teste ou uma tela), pare e relate.

- [ ] **Step 3: Apagar os dois componentes**

```
git rm src/AtividadesJuridicas.Web/Components/Atividades/FormularioDeAtividade.razor src/AtividadesJuridicas.Web/Components/Atividades/EditorDeResponsaveis.razor
```

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build --filter "FullyQualifiedName~TelasNoDesignSystemTests"`
Expected: todos passando, inclusive `Toda_tela_de_atividade_esta_na_trava`.

- [ ] **Step 4: Conferir cada regra do `app.css` antes de apagar**

Regra só sai se nenhum `.razor` nem `.cs` da Web usa a classe, nem por marcação fixa nem por expressão (a Agenda, que é da 9F, monta classes por expressão: `Avisar(mensagem, "ok" | "aviso" | "erro")` vira `<p class="@_classeDaMensagem">`). Para cada classe, duas buscas em `src/AtividadesJuridicas.Web` com `--glob "*.razor" --glob "*.cs"` (ou o filtro `*.{razor,cs}` da ferramenta Grep):

1. a classe em atributo fixo: `class="([^"]* )?<classe>( [^"]*)?"` (não acha `aj-<classe>`, que tem o prefixo colado);
2. a classe como texto entre aspas, que é como ela chega a um atributo por expressão: `"<classe>"`.

Resultados esperados, depois das Tarefas 6 a 13 (confira um por um; o que diferir, pare e relate):

| Classe | Busca 1 | Busca 2 | Regras do `app.css` | Decisão |
|---|---|---|---|---|
| `comentarios` | nada (o `ComentariosDaAtividade` da Tarefa 8 usa `aj-comentarios`) | nada | `.comentarios`, `.comentarios li`, `.comentarios li span` (linhas 82–84 de hoje) | sai |
| `confirmacao` | nada | nada (o enum `Confirmacao` da ficha antiga saiu com ela; a busca diferencia maiúsculas) | `.confirmacao`, `.confirmacao:focus` (100–101) | sai |
| `inativo` | nada | só texto, não classe: `Contagem.ComInativos(…, "inativo", "inativos")` em `Pages/Usuarios.razor`, `Pages/Clientes.razor` e `Pages/Perfis.razor`, e o rótulo `"inativo"` em `Components/Documentos/GestaoDeDocumentos.cs` | `.inativo` (102) | sai |
| `aviso` | nada (o `<p class="aviso">` do `FormularioDeAtividade` saiu no Step 3) | `Pages/Agenda.razor` e `Components/Agenda/FichaDoEvento.razor` (`Avisar(salvo, … ? "ok" : "aviso")`, a classe do `<p class="@_classeDaMensagem">`) e `Pages/Conta/Entrar.razor` (`Name = "aviso"`, parâmetro de consulta) | `.aviso` (89), `p.aviso` em `p.erro, p.ok, p.aviso` (90) e `p.aviso` (93) | **fica**: a Agenda (9F) ainda usa |

- [ ] **Step 5: Apagar as regras sem uso**

Em `src/AtividadesJuridicas.Web/wwwroot/app.css`, apague exatamente estas cinco linhas e nada mais (o fim de linha do arquivo continua o mesmo):

```css
.comentarios { display: grid; gap: 0; margin: 0; padding: 0; list-style: none; }
.comentarios li { display: grid; gap: var(--espaco-4); padding: var(--espaco-12) 0; border-top: 1px solid var(--borda-suave); }
.comentarios li span { font-size: 13px; color: var(--tinta-suave); }
```

```css
.confirmacao { margin-bottom: var(--espaco-14); padding: var(--espaco-14); border: 1px solid var(--aviso-borda); border-radius: var(--raio-14); background: var(--aviso-fundo); color: var(--aviso-texto); }
.confirmacao:focus { outline: 2px solid var(--aviso); outline-offset: 2px; }
.inativo { opacity: .55; }
```

`.aviso`, `p.erro, p.ok, p.aviso` e `p.aviso` ficam como estão. Nenhuma regra nova entra no `app.css`.

Confira depois com a ferramenta Grep que `\.comentarios|\.confirmacao|\.inativo` não aparece mais no `app.css` e que `\.aviso` aparece (as três ocorrências de hoje).

- [ ] **Step 6: Build e testes**

Run: `dotnet build -m:1`
Expected: 0 avisos, 0 erros.

Run: `dotnet test tests/AtividadesJuridicas.UnitTests --no-build`
Expected: todos passando.

Run: `dotnet test tests/AtividadesJuridicas.IntegrationTests --no-build --filter "FullyQualifiedName~WebTests|FullyQualifiedName~Interativ|FullyQualifiedName~ComponentesDoDesignSystemTests|FullyQualifiedName~PaginasProtegidasTests"`
Expected: todos passando (os testes de página por HTTP, inclusive `LayoutWebTests`, os de clique com o bUnit, os dos componentes do design system e os das páginas protegidas).

- [ ] **Step 7: Commit**

Confira o branch (`git branch --show-current` deve dizer `worktree-feat+fase-9e-atividades`) e:

```
git add -A src tests
git commit -m "refactor(web): apaga o formulário e o editor de responsáveis antigos e as regras do app.css que ficaram sem uso"
```

---

### Task 15: Suíte inteira, conferência no navegador e emenda da spec

Feita pela sessão que coordena.

- [ ] **Step 1: Suíte inteira** em lotes (`suite-em-lotes.ps1`, em `C:\Users\Gabriel Paulino\.claude\projects\c--GitHub-SIstemaControleAtividadesJuridicas\conferencia-de-telas\`), com o `dotnet build -m:1` feito antes e nenhum `docker` nem aplicação publicada rodando ao mesmo tempo. Expected: unidade e integração sem falha, com mais testes que a linha de base (882 e 771). Falha em massa com `TaskCanceledException` em menos de 2 minutos é o Testcontainers desistindo do Docker: esperar e rodar de novo o lote.

- [ ] **Step 2: Ambiente descartável e ferramenta**

- Ferramenta: copie `conferencia-de-telas\9d\` para `conferencia-de-telas\9e\` (`condutor.js`, `c.ps1`, `conferir.ps1`, `expandir.js`, `ref.html`, `quadro.html`, `coletor.js`, `app-lado.js`, `preparar-aba.ps1`, `passo4.js`, `transbordo.js`, `prep-formularios.js`) e renomeie `prancha-9d.ps1` para `prancha-9e.ps1` (o conteúdo serve como está). `dados-9d.js` e `dados-9d.sql` viram `dados-9e.js` e `dados-9e.sql` (Step 3); `preparar-aba.ps1` passa a injetar o `dados-9e.js`; `prep-formularios.js` ganha os preparos da 9E (Step 4).
- Banco: `docker run -d --name atividades-verif-9e -e POSTGRES_USER=atividades -e POSTGRES_PASSWORD=atividades -e POSTGRES_DB=atividades -p 127.0.0.1:5599:5432 postgres:17-alpine`.
- Aplicação: `dotnet publish src/AtividadesJuridicas.Web -c Release -o <scratchpad>\app-9e --no-restore` e o `.dll` com `--contentRoot <scratchpad>\app-9e`, `ASPNETCORE_ENVIRONMENT=Development`, `ASPNETCORE_URLS=http://localhost:5197`, `ConnectionStrings__Padrao` na 5599 e `Armazenamento__Pasta` no scratchpad. A Tarefa 1 já publicou o CSS da fase e o copiou para o código: a publicação usa o `componentes.css` do worktree, sem troca.
- Quadros: `node ..\servidor.js <scratchpad>\quadros-9e\project <conferencia-de-telas>\9e <scratchpad>\coleta-9e 5198`. Chrome sem janela (`--headless=new --remote-debugging-port=9222`, perfil próprio no scratchpad) e o condutor (`node --experimental-websocket condutor.js 9222 5195`). Credenciais em `<coleta-9e>\credenciais.json`, geradas por PowerShell (o admin do `appsettings.Development.json` e senhas aleatórias para Marina, Beatriz, Camila, Otávio e Rafael); a página lê pela chave.
- Depois de cada queda da máquina: o Chrome e o condutor podem ter caído; as coletas já gravadas em disco ficam, e a conferência retoma da tela seguinte.

- [ ] **Step 3: Dados do quadro**

Pela tela o que exercita a própria tela; por SQL no banco descartável o resto (como a 9D). Os textos são os dos quadros.

- Pela tela (`dados-9e.js`, como o admin): as áreas `Trabalhista`, `Tributário`, `Família e sucessões` e `Cível`; os usuários `Marina Couto` (perfil da Diretoria; é quem entra para medir, autora e revisora) e `Beatriz Lins`, `Camila Prado`, `Otávio Brandão`, `Rafael Teles` (Produção).
- Por SQL (`dados-9e.sql`, aproveitando o `dados-9d.sql`): os clientes `Construtora Serra Azul Ltda.`, `Transportes Litoral Ltda.`, `Joana Figueiredo` e `Cooperativa Agrícola Boa Safra`; os processos `Serra Azul × Município — ISS` (Tributário), `Litoral — reclamação trabalhista` (Trabalhista), `Figueiredo — inventário` (Família e sucessões) e `Litoral × Seguradora — indenização` (Cível), cada um com o seu cliente.
- A **Minuta de acordo trabalhista**, pelas telas, nesta ordem (cada passo exercita uma tela da fase):
  1. Marina, em `/atividades/nova`: título, entrega `09/10`, complexidade `2`, a descrição do quadro ("Redigir a minuta de acordo com base nas condições discutidas na audiência de instrução de 05/10: pagamento em seis parcelas e quitação geral do contrato de trabalho."), o processo `Litoral — reclamação trabalhista` escolhido pela busca e o cliente `Transportes Litoral Ltda.`. Antes de salvar, medir o formulário (Step 4).
  2. Marina, em `/atividades/{id}/responsaveis`: Beatriz (`Redigir a minuta com as condições discutidas na audiência.`) e Otávio (`Conferir os cálculos das verbas rescisórias.`), revisor Marina. Salvar; reabrir a tela e medir (Step 4).
  3. Beatriz entra, envia `minuta-acordo-v2.docx` (84 KB, gerado na página por `V.arquivo`), comenta `Minuta anexada. Deixei em aberto a cláusula de multa por atraso para a revisão.` e conclui a parte (pela confirmação). Marina comenta `Otávio, confira os valores da cláusula terceira antes da revisão.`. Otávio envia `calculo-verbas.xlsx` (31 KB) e conclui: a atividade fica Feita, com o histórico de três registros do quadro (`Criada`, `Criada → Direcionada` por Marina, `Direcionada → Feita` por Otávio).
  4. Por SQL, só as horas, para baterem com o quadro: histórico 01/10 10:02, 01/10 10:20 e 08/10 09:15; conclusões 07/10 16:42 (Beatriz) e 08/10 09:15 (Otávio); comentários 07/10 16:45 e 08/10 08:30; documentos 07/10 16:40 e 08/10 09:10 (horário de Maceió).
- As outras sete atividades de `Atividades.dc.html` por SQL, com Marina revisora e os responsáveis na ordem da coluna: `Contestação — cobrança de ISS` (Direcionada, Rafael), `Parecer sobre partilha de bens` (Para correção, Camila e Otávio), `Levantamento de certidões` (Criada, sem processo, sem responsável, sem revisor), `Recurso ordinário` (Corrigida, Beatriz), `Revisão de contrato de locação` (Concluída, sem processo, Camila), `Petição inicial — indenização` (Reaberta, Rafael e Beatriz), `Relatório mensal de andamentos` (Direcionada, sem processo, Otávio), com os clientes, os processos e as entregas do quadro.
- **Datas:** o "hoje" do sistema é o do relógio. Para o quadro valer (`8 atividades · 1 atrasada`, só a Contestação atrasada), todas as entregas e horas acima são deslocadas pelo mesmo número de dias, de modo que hoje caia em 08/10/2026 do quadro. O `ref.html` copia os textos do sistema para o quadro antes de medir, então as datas deslocadas não viram divergência.
- **Casos de borda** (Review Focus e conferências), **depois** de medir, para não mudar as contagens: uma atividade com título, cliente e processo longos (com palavras longas sem espaço); um documento de nome longo sem espaços na Minuta; uma atividade sem data de entrega e sem cliente.

- [ ] **Step 4: Medir os 15 quadros** contra o sistema, nas três larguras, com `conferir.ps1` (o `ref.html` da 9D: expande `sc-for`, `sc-if` e `{{ … }}` pelo `expandir.js` antes de medir, e reordena as linhas e os cartões repetidos na ordem do sistema). A captura lado a lado de cada um, com `prancha-9e.ps1`, fica como registro.

| Quadro | Rota e estado | Observação |
|---|---|---|
| `Atividades` (3 visões) | `/atividades`, Marina, sem filtro | modelos expandidos (tablet e mobile têm `sc-for` nos chips e nos cartões; o desktop, o estado do menu); a coluna Complexidade não está no quadro |
| `AtividadeFormulario` (3) | `/atividades/nova` preenchido como no Step 3 (`PREP.atividade()` em `prep-formularios.js`), antes de salvar | o segmentado tem modelo (`{{ c.m2 }}`): expandido |
| `Responsaveis` (3) | `/atividades/{minuta}/responsaveis` com a Minuta ainda Direcionada | a nota da opção é o nome do perfil (`PerfilNome`, decisão do plano); com os perfis semeados, a Diretoria aparece como `(Diretor)` e o quadro mostra `(Diretoria)`: divergência de dado, não de tela; registre |
| `Devolver` (3) | `/atividades/{minuta}/devolver` com o motivo do quadro digitado | Minuta Feita |
| `FichaDaAtividade`, `FichaTablet`, `FichaMobile` | `/atividades/{minuta}`, Marina, logo depois de "Trocar revisor" mantendo Marina (o aviso `Revisor alterado.` no topo, como no quadro) | desktop com `-Extra 'ordem=posicao'` (o painel de detalhes vem primeiro na marcação e fica à direita) e modelos expandidos; o quadro mostra "Responsáveis" e "Trocar revisor" juntos numa Feita, e a tela mostra só "Trocar revisor" (decisão do plano) |

Medir também o que o `app.css` muda dentro de cada tela (`V.vazamento`), com atenção às regras de elemento sem exclusão de `aj-` que esta fase passa a encontrar: `label:has(> input[type="checkbox"])` nas opções de responsáveis e de devolver, `fieldset` e `legend` no `aj-conjunto`, `input[type="checkbox"]` no `aj-opcao__caixa`, `input.invalid`/`textarea.invalid` sobre `aj-campo` (o motivo vazio e o título vazio, depois de um envio com erro) e `button:disabled`.

O que divergir: CSS no design system (a Tarefa 1 de novo: publicar, sincronizar a cópia do canvas e copiar para o código), nunca no `app.css` nem com `style`; marcação na tela, com o teste ajustado antes. Divergências que já são decisão do plano e não se corrigem (seção "Decisões", divergências dos quadros antigos): o `Aviso` com o ícone solto, o `h1` da barra de voltar do mobile, o voltar do tablet e o respiro de cima pelas variáveis, a barra fixa do mobile com 46px, o hover do perigo, o `letter-spacing` do título, o nome no título da ficha do mobile com 26px (o quadro tem 27), o respiro do "Buscar", a sombra do primário no tablet, o vão de 12px entre os cartões do mobile. Registre cada uma para a emenda.

Sem quadro, conferidos por captura nas três larguras: `/atividades/{id}/reabrir` (Revisão de contrato de locação), o modo do revisor de `/responsaveis` (Recurso ordinário, Corrigida), o terceiro estado (`Esta atividade não permite mais essa ação.`), o formulário de edição e o sem processo (área e cliente avulso), as três confirmações da ficha, o painel de filtros do tablet e do mobile (Diretoria e Produção), a ficha vista pela Produção (Beatriz) e a lista vazia.

- [ ] **Step 5: Review Focus e casos de borda no navegador**

1. Contadores da Produção: Beatriz vê no subtítulo e nos chips só as atividades em que é responsável (as 3 do quadro: Minuta, Recurso ordinário e Petição inicial, mais a de borda se for dela), sem a parte das atrasadas (nenhuma é dela) e com `Atrasadas · 0`, nunca as 8; Marina com `Responsável: Rafael Teles` escolhido continua vendo `8 atividades · 1 atrasada` no subtítulo.
2. Chips com outros filtros: Marina digita "contrato" na busca, escolhe Rafael Teles e toca "Atrasadas" e depois "Direcionadas": a busca e o responsável ficam; "Todas" limpa a situação e "só atrasadas"; na largura do desktop o campo Situação e a caixa "Só atrasadas" mostram o mesmo estado.
3. Ficha que não existe ou que a Produção não vê: Camila abre `/atividades/{minuta}`: o voltar e `Atividade não encontrada.`, sem ação, sem comentários e sem documentos; os eventos de acesso negado da Camila nesse momento são só o de `ObterAtividade` (cada lista pedida e negada deixaria o seu; o registro de consultas só guarda consulta bem-sucedida e não serve de prova aqui).
Os itens 4 e 5 mudam a situação da atividade: faça-os em atividades criadas para isso (feitas como a Minuta, pelas telas), não na Minuta, que o Step 6 ainda usa.

4. Clique duplo: dois cliques rápidos em "Salvar atividade", "Salvar responsáveis", "Devolver", "Aprovar" e "Concluir minha parte" (no navegador, com `V.clicar` duas vezes seguidas): um registro só (no histórico ou no banco); nos três que navegam (os dois "Salvar" e "Devolver"), o botão travado até a navegação; em "Aprovar" e "Concluir minha parte", que ficam na ficha, os botões travados enquanto a resposta não chega.
5. Devolver sem motivo e sem responsável marcado: os dois erros nos campos e nada enviado; com motivo e só Beatriz marcada, a ficha mostra só a parte da Beatriz pendente (`Pendente`) e a do Otávio concluída.

Conferências da lista do plano: título, cliente e processo longos nos cartões e no cabeçalho da ficha a 390px (quebram, sem rolar a página de lado: `transbordo.js`); o documento de nome longo nas três visões; o vínculo escolhido pela busca no formulário (o processo escolhido aparece com a classe `aj-vinculo__escolhido aj-vinculo__escolhido--detalhado` depois do clique, e o resultado some: a lição da 9D sobre `@key`); o voltar de Produtores (`/produtores` → painel do produtor → linha → ficha com o voltar "Produtores" → `/produtores?produtor={id}` com o produtor escolhido).

- [ ] **Step 6: Teclado e estados** (lista fixa da 9D, com o que a 9E acrescenta): as abas do mobile da ficha (`Detalhes`, `Comentários 2`, `Documentos 2`, `Histórico`) com toque e com Tab; o "Mais ações" da barra do mobile e o "⋯" das linhas de documento no tablet e no mobile (Tab, Esc, toque no fundo; `passo4.js`); as confirmações de concluir, aprovar e excluir no topo da ficha a 390px, com o foco nelas ao aparecer; o foco no painel de envio e no de renomear documento ao abrir; a barra fixa sem cobrir o último painel no tablet e no mobile (ficha, formulário, responsáveis, devolver); larguras 760, 761, 1199 e 1200 na lista e na ficha; a busca da barra do mobile com Enter; Enter no formulário de atividade envia uma vez só; Enter na busca de processo busca e não salva; a Produção (Beatriz) sem nenhuma ação de cadastro na lista e na ficha, com "Concluir minha parte", comentar e enviar documento.

- [ ] **Step 7: Derrubar o ambiente** antes de qualquer rodada da suíte: `docker rm -f atividades-verif-9e`, parar a aplicação, o servidor dos quadros, o condutor e o Chrome sem janela, e apagar `credenciais.json`. As coletas e as capturas ficam no scratchpad como registro.

- [ ] **Step 8: Emenda da spec** (`docs/superpowers/specs/2026-10-10-migracao-para-o-design-system-design.md`), como a 9D emendou:

- Cabeçalho: `Estado: aprovada pelo usuário em 2026-10-10; fases 9A a 9E executadas`; na linha do canvas, a versão lida pela 9E (`1792026220-8711`); na linha do design system, a versão publicada pela Tarefa 1 desta fase e as que a conferência publicar.
- Seção 11, `Como a fase 9E fez a conferência (emenda de <data>), além do que está acima:` — o que esta conferência acrescentou ao método: as datas do quadro deslocadas para o "hoje" do relógio (o contador de atrasadas depende dele); a atividade principal criada e levada até Feita pelas próprias telas da fase, com três perfis entrando (Diretoria, dois de Produção), e só as horas acertadas por SQL; os quadros antigos de estilo embutido (lista e ficha) medidos pelo mesmo `ref.html` dos novos; o que mais a conferência ensinar (registrar ao fazer, como a 9D).
- Seção 15, nova subseção `### Da fase 9E (emenda de <data>)`, no formato da 9D:
  - O que a fase entregou e as próximas reutilizam: `CargaDoFormulario` e `EstadoDeCarga` (nos quatro formulários); `PaginaDeAtividades` (`NoEscopo`, `Atrasadas`); `CabecalhoDePagina.Alerta`, `CabecalhoDeDetalhe.RotuloDoVoltarNoMobile`, `BarraDeVoltar.Acao`, `Paginacao.ComSetas`, `EtiquetaDeSituacao.NaTabela`; `AvatarDePessoa`, `TextosDeAtividade`, `RotulosDeAtividade.SituacaoNoPlural`; os ganchos de listar e enviar em `GestaoDeDocumentos` (a terceira herdeira); `CabecalhoDaAtividade`; `MotivoComResponsaveis` (uma tela para duas rotas); a classe de visibilidade `aj-desktop-mobile`; a trava `Toda_tela_de_atividade_esta_na_trava`.
  - Decidido na execução, sem quadro ou contra o quadro, para o usuário rever: as decisões do plano que a seção "Decisões" manda anotar (o mês de Produtores que volta ao corrente; "Trocar revisor" só quando os responsáveis não podem mudar, e também no "Mais ações" do mobile; Excluir sempre volta a `/atividades`, mesmo vindo de Produtores; a Diretoria conta o escritório inteiro mesmo com responsável escolhido; a cor do avatar pelo nome; a coluna Complexidade depois de Entrega; as etiquetas com ícone só na tabela; os documentos do tablet e do mobile com o "⋯"; o comentário em linha no tablet; as divergências dos quadros antigos mantidas como nas fases anteriores) e as decididas na redação deste plano (a seção "Decisões tomadas na redação", ao fim dele: o exemplo da busca de processo sem processo escolhido, o título e o voltar das telas de ação antes da carga; o terceiro estado como aviso de atenção; o modo do revisor com o painel médio, o título da atividade no subtítulo e "Salvar" no mobile; a nota do perfil na opção de responsável; o `form` como painel no devolver e no reabrir; os erros de motivo e de responsáveis juntos nos campos).
  - Para as próximas fases: o que a Agenda (9F) ainda usa do `app.css` (`.aviso` e `p.aviso` por expressão, `.ficha`, `.formulario`, `.filtros`, `.grade`, `.acoes`, `.texto-livre`, `.dia`, `td.horario`, `fieldset` e `legend` do formulário de evento); o formulário de evento lê `ListarAtividades` como `Pagina<AtividadeResumo>` (continua compilando com `PaginaDeAtividades`); os formulários continuam carregando em `OnInitializedAsync` (o recarregar entre `/x/novo` e `/x/{id}/editar`, sem mudança); `Quantos` e a tabela de documentos (o que a fase extraiu ou não); o que a conferência deixar.
  - Para a 9G: as regras do `app.css` já sem uso que a Tarefa 14 não apagou (`strong.erro`, `.validation-message`, a parte `.cartao` do seletor `.cartao, form.formulario:not(.filtros)`); as regras de elemento que alcançam as telas desta fase, com o que `V.vazamento` mediu (`label:has(> input[type="checkbox"])`, `fieldset`, `legend`, `input[type="checkbox"]`, `input.invalid`/`textarea.invalid` sobre `aj-campo`, `button:disabled`); o que o teclado mostrar no "Mais ações" e no "⋯" (mesma raiz do `MenuDeAcoes` da 9D).
  - Observações de desenho, sem ação nesta fase: o que a conferência registrar.
- Na subseção `### Da fase 9D`, em "Para as próximas fases": o item do "Tentar de novo" dos formulários marcado como feito na 9E (`CargaDoFormulario`), e a nota de `Quantos` com o que a 9E decidiu.

Commit (confira o branch antes):

```
git add docs/superpowers/specs/2026-10-10-migracao-para-o-design-system-design.md
git commit -m "docs(spec): o que a fase 9E ensinou sobre a migração para o design system"
```

- [ ] **Step 9: Suíte inteira de novo** se a conferência mudou código (com o ambiente derrubado), e o aval do usuário para a `main`.

---

## Decisões tomadas na redação

Decisões que os quadros e o contrato não davam e que mudam comportamento ou texto visível, para o usuário revisar. Entre parênteses, a tarefa.

- (1) Documentos no mobile sem quebra de linha: o "⋯" fica sempre na linha do nome.
- (1) "Comentar" com 20px de respiro lateral e o raio 12 no primário e no secundário da faixa da ficha no tablet, como os quadros.
- (1) Tabela da lista com 980px de largura mínima (a do quadro de sete colunas): com nove colunas, a caixa rola de lado.
- (1) O "Mais ações" da barra de voltar do mobile mede o alvo de toque (44px), como o voltar.
- (3) No formulário de usuário, a troca de usuário com a tela montada não volta a mostrar "Carregando…" (só o "Tentar de novo" mostra).
- (4) A cor do avatar pela conta do nome não reproduz a do quadro ("Beatriz Lins" sai lilás; no quadro é azul).
- (4) Área só com espaços conta como sem área na linha de entrega; nome de uma letra dá essa letra como sigla.
- (5) Renomear ou excluir um documento que já não existe relê a lista (e fecha o renomear) também nos Documentos comuns e na ficha do cliente.
- (6) Falha de `ListarUsuarios` na lista: aviso de erro no topo com a mensagem (ou `Não foi possível consultar os usuários.`), e a lista carrega com os campos de responsável e revisor só com "Todos".
- (6) Busca do desktop com `type="text"` (o quadro); do tablet e do mobile com `type="search"`.
- (6) `Todas · 0` e `Atrasadas · 0` com número sempre que a consulta respondeu; na falha, os chips só com o nome.
- (6) Uma combinação feita no painel (situação e "só atrasadas") pressiona os dois chips; com uma consulta em curso, chips, "Buscar" e "Filtrar" não mudam nada.
- (6) "Filtrar" fecha o painel; "Limpar" não consulta e mantém a busca digitada.
- (6) "Só atrasadas" no painel de filtros como caixa com moldura (`aj-opcao`); na barra do desktop, solta.
- (6) Paginação com setas nas três visões (no mobile, só com mais de uma página); a busca e os chips da barra do mobile só aparecem depois da carga e com acesso.
- (7) O erro do cliente que saiu do processo vai no rótulo `Cliente do processo` (`aj-rotulo--erro` e `aj-campo-erro`), não num parágrafo solto; o salvar continua sem bloqueio na tela.
- (7) Resultados de busca em duas linhas: processo com `{área} · {clientes}`; cliente com o documento (`CNPJ 11.222.333/0001-81`).
- (7) A carga da edição lê a atividade antes das áreas: o "não encontrada" chega sem consultar as áreas.
- (7) Busca e "Escolher" não fazem nada durante o envio; os campos de texto continuam editáveis.
- (7) Conflito ao salvar a edição: só o aviso com a mensagem do servidor (que pede recarregar); um novo "Salvar" conflita de novo até a tela ser recarregada.
- (8) O aviso de envio do comentário fica dentro do painel, logo abaixo do título; as duas formas do comentário novo só aparecem depois da primeira carga e ficam com a lista em erro.
- (8) Comentário de autor desconhecido: sigla `?` com a cor do nome vazio (azul).
- (8) No tablet, o botão de ícone não troca o rótulo durante o envio; `Enviando…` aparece só no "Comentar".
- (9) Ícone do documento pelo arquivo enviado (o nome pode ter sido trocado sem a extensão); meta na ordem do quadro, `{tamanho} · {autor} · {data}`, sem a parte do autor quando ele não é conhecido.
- (9) O "⋯" dos documentos traz só Renomear e Excluir (o nome já é o link de download).
- (9) O painel de envio não tem a linha "Vai para…" e fecha depois de um envio bem-sucedido (hoje ficava aberto).
- (9) "Enviar documento" aparece também durante a primeira carga e some com a atividade sumida ou o acesso encerrado; um renomear em curso não trava mais as outras ações (a guarda é a da base).
- (10) Quem renomeia e exclui documentos na ficha é quem pode editar a atividade (`PodeGerenciar = Acoes.Editar`).
- (10) Só `?de=produtores` muda o voltar; um `produtor` que não é id volta a `/produtores`.
- (10) Autor nulo no histórico aparece como `Sistema` (a ficha de hoje deixa vazio); vazios de Responsáveis e Histórico na caixa `aj-estado-lista`.
- (10) "Tentar de novo" na ficha refaz a carga inteira, zera os contadores das abas e volta à aba Detalhes.
- (10) A entrega vencida no cabeçalho da ficha só ganha a etiqueta Atrasada ao lado da situação (sem destacar o texto da entrega).
- (11) O divisor da faixa só aparece no desktop; Reabrir é link secundário sem ícone (na barra fixa do mobile, sozinho).
- (11) O botão que confirma nas três confirmações é primário; abrir uma confirmação apaga o recado e o aviso anterior, e pedir outra troca a aberta.
- (11) A exclusão envia a versão da ficha do momento em que a confirmação foi aberta; a exclusão recusada fecha a confirmação, recarrega a ficha, mostra o erro e não navega.
- (11) O aviso "…, mas não foi possível atualizar a ficha." aparece também quando a recarga tira a ficha da tela.
- (12, 13) Antes da carga, na Produção e no terceiro estado, o título é o da tela (`Responsáveis`, `Devolver para correção`, `Reabrir atividade`) e o voltar leva à ficha com a origem `Atividade`; na não encontrada, à lista.
- (12, 13) O terceiro estado (`Esta atividade não permite mais essa ação.`) é um aviso de atenção, sem formulário e sem barras de ação; responsáveis não consulta os usuários nesse caso.
- (12) No modo do revisor, o seletor começa no revisor atual se ele é diretor ativo (senão em `Selecione…`), o rótulo só para leitor é `Revisor` (era "Novo revisor") e o subtítulo é o título da atividade.
- (12) O painel do modo do revisor leva também `aj-painel--sempre-destacado` e `aj-lado-a-lado__lista`, além das classes da decisão do coordenador: sem o segundo, o painel encolheria ao conteúdo no desktop.
- (12, 13) No mobile o botão diz `Salvar` nos dois modos de responsáveis; em devolver e reabrir, o verbo (`Devolver` ou `Reabrir`) é o mesmo nas três visões, com o ícone `devolver`.
- (12, 13) Falha ao enviar: aviso de erro no topo, o digitado e as marcas ficam, sem reler a atividade; os campos ficam desabilitados enquanto envia.
- (13) Os erros de motivo e de responsáveis aparecem juntos, cada um no seu campo; o do conjunto só some no envio seguinte. O motivo vai aparado e os responsáveis na ordem da atividade.
- (14) A conferência "toda tela de atividade está na trava" virou o teste `Toda_tela_de_atividade_esta_na_trava`, que fica para as próximas fases.
