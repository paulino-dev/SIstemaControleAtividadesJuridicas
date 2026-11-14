# Solicitações do cliente

Data: 2026-11-13
Estado: fases A (a solicitação) e B (a atividade a partir da solicitação) implementadas
Emenda: `2026-10-08-portal-do-cliente-design.md` (seções 1, 2, 3, 4, 6, 7, 9, 10, 11 e 12) e
`2026-10-06-modelagem-dominio-design.md`

## 1. Objetivo

O cliente passa a pedir trabalho ao escritório pelo portal. Ele abre uma solicitação com a descrição do que precisa,
os documentos que tiver e um nível de urgência, e o sistema avisa que a equipe verifica a solicitação em até 24 horas
úteis. A Diretoria vê todas as solicitações e, de dentro de uma delas, cria a atividade que vai atendê-la. O cliente
acompanha cada solicitação: aguardando, cancelada ou, depois de virar atividade, a situação dessa atividade.

### Critérios de sucesso

- Toda solicitação registra quem a abriu e de qual cliente ela é.
- A Produção nunca alcança uma solicitação nem os documentos dela, a não ser os que a Diretoria levou para uma
  atividade em que ela é responsável.
- Um cliente nunca lê solicitação nem documento de solicitação de outro cliente.
- Uma solicitação gera no máximo uma atividade viva.
- Um diretor nunca cria atividade de uma solicitação cancelada sem ter confirmado o aviso, nem a partir de uma versão
  da solicitação que o cliente já alterou.
- O cliente da atividade criada a partir de uma solicitação é o da solicitação, e não muda depois.

### Fora do escopo

Negar ou excluir solicitação pela Diretoria (não existe); resposta em texto da Diretoria ao cliente; aviso por e-mail
ou mensagem; contador de "solicitações novas" no menu ou no início; prazo de 24h úteis calculado ou cobrado pelo
sistema (é só o aviso); reabrir uma solicitação cancelada; mais de uma atividade por solicitação; mostrar ao cliente
os status internos da atividade.

## 2. Decisões

| Tema | Decisão |
|---|---|
| Anexos da solicitação | Categoria nova do agregado `Documento`: `DocumentoDaSolicitacao` |
| Documento levado para a atividade | Um `DocumentoAtividade` novo que aponta para o mesmo arquivo gravado; nada é copiado |
| Tela de criação da atividade | O formulário de atividade atual, em `/atividades/nova?solicitacao={id}`, com um bloco a mais |
| Cancelamento ou edição durante a criação | Conferidos pelo servidor na gravação, pela versão da solicitação |
| Situação mostrada ao cliente | A do andamento do portal: "Em andamento" ou "Concluída" |
| Atividade excluída | A solicitação perde o vínculo e volta ao estado que tinha antes de virar atividade |
| Vínculo nos dois sentidos | `Atividade.SolicitacaoId` e `SolicitacaoDoCliente.AtividadeId`, ambos anuláveis |

Alternativas descartadas: uma tabela própria de anexos (duplicaria armazenamento, download, auditoria e a regra de
acesso); copiar o arquivo para a atividade (dobra o disco e, quando for preciso eliminar um arquivo de vez do sistema,
obriga a rastrear todas as cópias; compartilhado, basta procurar os documentos pela chave); uma tela separada de
criação de atividade (duplicaria a lógica de processo, área e cliente do formulário).

Custo aceito do compartilhamento: hoje o banco garante que cada arquivo pertence a um único documento (índice único em
`documentos.chave_armazenamento`). Essa garantia deixa de existir e vira regra de código (seção 3).

## 3. Domínio

### `SolicitacaoDoCliente` (agregado novo, em `Domain/Solicitacoes`)

| Campo | Regra |
|---|---|
| `ClienteId` | Obrigatório; o cliente do usuário que abriu |
| `SolicitanteId` | Obrigatório; o usuário de perfil `Cliente` que abriu |
| `Descricao` | Obrigatória, até 4.000 caracteres (o limite do comentário de atividade, para caber nele inteira) |
| `Urgencia` | `Baixa`, `Media` ou `Alta` |
| `CanceladaEm` | Preenchido quando o cliente cancela |
| `AtividadeId` | Preenchido quando uma atividade é criada a partir dela; limpo se a atividade for excluída |
| `Versao` | Versão da linha, mantida pelo banco (controle de concorrência otimista), como em `Atividade` |

Situação, derivada dos campos e nunca gravada:

| Situação | Quando |
|---|---|
| `Aguardando` | Sem atividade e sem cancelamento |
| `Cancelada` | Sem atividade e com cancelamento |
| `EmAtividade` | Com atividade (com ou sem cancelamento anterior) |

Operações:

- `Abrir(clienteId, solicitanteId, descricao, urgencia)`.
- `Alterar(descricao, urgencia)`: só em `Aguardando`.
- `Cancelar(agoraUtc)`: só em `Aguardando`. Não há volta.
- `VincularAtividade(atividade, confirmouCancelada)`: antes das regras de situação, recusa a atividade que não nasceu
  desta solicitação (erro de programação) e a de outro cliente ("A atividade precisa ser do cliente da solicitação.").
  Depois, em `Aguardando`; em `Cancelada`, só com `confirmouCancelada`; em `EmAtividade`, recusada.
- `DesvincularAtividade()`: limpa `AtividadeId`. A solicitação volta a `Aguardando` ou a `Cancelada`, conforme
  `CanceladaEm`.

Os documentos da solicitação não fazem parte do agregado (são `Documento`). Adicionar ou remover um documento
também só vale em `Aguardando`, e mexe na `Versao` da solicitação (como `VersaoDasPartes` faz na atividade), para
que o diretor que abriu o formulário antes da mudança seja avisado.

### `Atividade`

- Campo novo `SolicitacaoId` (anulável), definido só na criação e nunca alterado.
- Com `SolicitacaoId`, `Alterar` recusa troca de cliente: "O cliente de uma atividade criada a partir de uma
  solicitação não pode ser trocado." O processo escolhido precisa ter esse cliente, pela regra que o vínculo já tem.

### `Documento`

- `CategoriaDocumento` ganha `DocumentoDaSolicitacao`.
- Campo novo `SolicitacaoId`: obrigatório nessa categoria e só nela. `ClienteId` é obrigatório nela.
- Sem lista de acesso, como as categorias do portal.
- Regra de acesso: Diretoria acessa; Cliente acessa se o `ClienteId` é o seu; Produção nunca.
- O documento da solicitação vira documento da atividade por `CriarDeAtividade`, que já recebe um
  `ArquivoDoDocumento` sem saber se o arquivo acabou de ser gravado: passa-se o arquivo do documento da solicitação,
  com a mesma chave de armazenamento e o mesmo hash. O nome é o do documento da solicitação.
- **Regras que substituem o índice único:**
  - Só esse caminho cria um documento com a chave de outro. Todo outro envio grava um arquivo novo, com chave nova.
  - A exclusão de documento continua lógica e não toca no arquivo físico.
  - Qualquer remoção física futura (limpeza de excluídos, eliminação por pedido do cliente) procura pela chave
    **todos** os documentos que usam o arquivo e só o apaga quando nenhum documento vivo o usa, ou quando a intenção é
    justamente eliminá-lo de todos. A spec e a modelagem de domínio registram essa regra.

## 4. Casos de uso

### Portal (abertos ao cliente: marcados com `IRequisicaoAbertaAoCliente`)

| Requisição | O que faz |
|---|---|
| `AbrirSolicitacao(Descricao, Urgencia, Arquivos)` | Cria a solicitação e os documentos dela numa só gravação. Os arquivos são gravados no armazenamento antes, e removidos se a gravação falhar (como `ArquivoEnviado` já faz para um) |
| `ListarMinhasSolicitacoes(Situacao?, Pagina)` | As solicitações do cliente do ator, da mais recente para a mais antiga. O filtro usa as quatro situações que o cliente vê (Aguardando análise, Cancelada, Em andamento, Concluída): as duas últimas vêm da atividade viva vinculada, pela regra do andamento do portal |
| `ObterMinhaSolicitacao(Id)` | A ficha: descrição, urgência, quem abriu, quando, documentos, situação; se em atividade, o título e a situação simplificada dela |
| `AlterarMinhaSolicitacao(Id, Descricao, Urgencia, VersaoEsperada)` | Só em `Aguardando` |
| `AnexarDocumentoASolicitacao(Id, arquivo, VersaoEsperada)` | Só em `Aguardando` |
| `RemoverDocumentoDaSolicitacao(DocumentoId, VersaoEsperada)` | Exclusão lógica; só em `Aguardando` |
| `CancelarMinhaSolicitacao(Id, VersaoEsperada)` | Só em `Aguardando` |

Regras comuns:

- Nenhuma recebe Id de cliente: usam o `ClienteId` do ator, lido do banco. Solicitação de outro cliente responde
  `NaoEncontrado`.
- Todos os usuários do mesmo cliente veem as solicitações do cliente, com o nome de quem abriu cada uma. Alterar,
  anexar, remover documento e cancelar são só de quem abriu.
- Quem não abriu a solicitação recebe `Validacao` ("Só quem abriu a solicitação pode alterá-la."), e não
  `AcessoNegado`: nas telas, `AcessoNegado` quer dizer "acesso encerrado". Os quatro comandos que alteram uma
  solicitação existente conferem primeiro quem abriu e depois a versão.
- Em qualquer situação diferente de `Aguardando`, as alterações respondem `Validacao`: "Esta solicitação não pode
  mais ser alterada."
- `VersaoEsperada` diferente da atual responde `Conflito`, como nas atividades.
- Até 10 documentos vivos por solicitação, contados na abertura e a cada anexo; acima disso, `Validacao`: "Anexe até
  10 documentos por solicitação." O limite por arquivo é o de hoje (50 MB).
- Nas listas, o trecho da descrição são os primeiros 140 caracteres, com "…" quando ela foi cortada.
- O download usa `BaixarDocumento`, já aberto ao cliente; quem decide é a regra de acesso do documento.
- A lista de requisições abertas ao cliente, fixada em teste, ganha as sete acima (são catorze ao todo).

### Diretoria

| Requisição | O que faz |
|---|---|
| `ListarSolicitacoes(Situacao?, ClienteId?, Pagina)` | Todas, **da mais antiga para a mais nova**, com cliente, quem abriu, data, urgência, trecho da descrição, número de documentos e situação |
| `ObterSolicitacao(Id)` | A ficha completa, com documentos, quem abriu e, se em atividade, o título e o Id da atividade |
| `CriarAtividadeDaSolicitacao(...)` | Seção 5 |

Produção e Cliente recebem `AcessoNegado` nas três.

### Mudanças nos casos de uso existentes

- `SalvarAtividade` (edição): recusa trocar o cliente de atividade com `SolicitacaoId` (a recusa é do domínio).
- `ExcluirAtividade`: na mesma gravação, chama `DesvincularAtividade` na solicitação da atividade, se houver.
- `ObterAtividade`: passa a trazer `SolicitacaoId`, só quando o ator é da Diretoria.

## 5. Criar atividade a partir da solicitação

`CriarAtividadeDaSolicitacao` leva os campos de `SalvarAtividade` (sem `ClienteId`) e mais:

| Campo | Para quê |
|---|---|
| `SolicitacaoId` | A solicitação de origem |
| `VersaoDaSolicitacao` | A versão com que o formulário carregou a solicitação |
| `AnexarDescricaoComoComentario` | Cria um comentário na atividade com a descrição da solicitação, tal como está, com o diretor como autor |
| `DocumentosDaSolicitacao` | Os Ids dos documentos marcados; cada um vira documento da atividade |
| `ConfirmouSolicitacaoCancelada` | O diretor viu o aviso de cancelamento e quer criar mesmo assim |

A validação de cliente, processo e área é a de `SalvarAtividade`, extraída para ser usada pelos dois. O cliente é o
da solicitação.

Verificações, nesta ordem, todas no servidor, antes de gravar:

1. Solicitação inexistente: `NaoEncontrado`.
2. Já em atividade: `Conflito`, "Esta solicitação já virou atividade." (a tela leva o diretor até a atividade).
3. Cancelada sem `ConfirmouSolicitacaoCancelada`: falha própria que a tela distingue das demais (por exemplo, um
   código de erro novo no `Resultado`), com a mensagem "Cliente cancelou a solicitação, mesmo assim deseja criar a
   atividade?".
4. `VersaoDaSolicitacao` diferente da atual: `Conflito`, "O cliente alterou a solicitação enquanto você criava a
   atividade. Confira os dados atualizados." Vale também com `ConfirmouSolicitacaoCancelada`: a confirmação dispensa
   só o aviso de cancelamento, nunca a conferência da versão. Como o próprio cancelamento muda a versão, a tela relê
   a solicitação ao mostrar o aviso de cancelamento e reenvia a confirmação com a versão relida.
5. Documento marcado que não é da solicitação ou foi removido: `Validacao`.

Numa só gravação: a atividade (com `SolicitacaoId`), `VincularAtividade` na solicitação, o comentário e os documentos.
A gravação da solicitação passa pela versão dela: duas criações simultâneas não passam as duas. Um índice único em
`atividades.solicitacao_id` (para atividades não excluídas) garante o mesmo no banco.

Cliente inativo: a criação é recusada como hoje ("O cliente está inativo.").

## 6. Telas

### Portal do cliente

| Rota | Conteúdo |
|---|---|
| `/minhas-solicitacoes` | Lista com data, trecho da descrição, urgência e situação; filtro por situação; botão "Nova solicitação" |
| `/minhas-solicitacoes/nova` | Descrição, urgência (três opções), campo de anexos com vários arquivos. Texto fixo: "A equipe verifica sua solicitação em até 24 horas úteis." |
| `/minhas-solicitacoes/{id}` | A ficha. Em `Aguardando`, "Editar" e "Cancelar solicitação" (só para quem abriu). Em atividade, o título e a situação da atividade |
| `/minhas-solicitacoes/{id}/editar` | Descrição, urgência, anexar e remover documentos |

- Depois de abrir, a tela vai para a ficha da solicitação e mostra o aviso das 24 horas úteis.
- Cancelar pede confirmação dentro da página (sem diálogo do navegador).
- Situação mostrada: "Aguardando análise", "Cancelada", "Em andamento" ou "Concluída".
- As quatro rotas são só do portal: entram na guarda inversa do layout ("Página não encontrada" para a equipe).
- Menu: o grupo "Portal" ganha "Solicitações", depois de Andamento. No trilho (tablet) ganha o item; na barra
  inferior (mobile) fica em "Mais". O início do cliente ganha o atalho "Nova solicitação".

### Diretoria

| Rota | Conteúdo |
|---|---|
| `/solicitacoes` | Lista da mais antiga para a mais nova, com filtro por situação (uma fileira de chips: Todas, Aguardando, Virou atividade e Cancelada; abre em "Aguardando") e por cliente |
| `/solicitacoes/{id}` | A ficha completa, com download dos documentos. Em `Aguardando`, botão "Criar atividade", para `/atividades/nova?solicitacao={id}`. Em atividade, botão "Ver atividade" |

- Menu: "Solicitações" depois de "Atividades", só para a Diretoria. A Produção que abrir a rota recebe "Página não
  encontrada" pela própria página.
- Solicitação cancelada não mostra "Criar atividade": o aviso de cancelamento só aparece quando o cancelamento
  acontece durante a criação.

### Formulário de atividade com `?solicitacao={id}`

- Bloco "Solicitação do cliente" no topo: quem abriu, quando, urgência e a descrição.
- O cliente vem preenchido e travado, sem a busca de cliente. A busca de processo mostra só os processos desse cliente.
- O título e a descrição da atividade vêm vazios: o diretor escreve os dois. A descrição da solicitação fica no bloco
  e vai para o comentário pela caixa abaixo. (Emenda de 2026-11-14: a descrição vinha preenchida com a da solicitação.)
- Caixa "Anexar a descrição da solicitação como comentário da atividade", marcada.
- Lista "Documentos da solicitação", cada um com a sua caixa, todas marcadas, e os botões "Marcar todos" e
  "Desmarcar todos". Sem documentos, a lista não aparece.
- Respostas da gravação:
  - Sucesso: abre a ficha da atividade nova, como já acontece hoje.
  - Cancelada: a tela relê a solicitação (bloco e documentos, como no caso "alterada"; se o conteúdo mudou, diz
    isso também) e mostra no próprio formulário "Cliente cancelou a solicitação, mesmo assim deseja criar a
    atividade?", com "Criar mesmo assim" e "Voltar à solicitação". Confirmar reenvia com
    `ConfirmouSolicitacaoCancelada` e a versão relida.
  - Alterada: o bloco da solicitação e a lista de documentos são recarregados (o que o diretor digitou fica), com o
    aviso de que o cliente alterou a solicitação. Documentos novos entram marcados; os removidos somem.
  - Já em atividade: aviso com link para a atividade.
- Na edição de uma atividade com solicitação, o cliente aparece travado do mesmo jeito.

### Ficha da atividade (Diretoria)

Atividade com `SolicitacaoId`: botão "Ver solicitação", para `/solicitacoes/{id}`. A Produção não o vê. Na faixa de ações
do desktop e do tablet, é um botão fantasma depois de "Trocar revisor" e antes de "Excluir"; no mobile, entra no "Mais
ações", também antes de "Excluir".

## 7. Auditoria

- As gravações de solicitação, de documento e de atividade aparecem pelos registros de entidade, como hoje.
- `ListarMinhasSolicitacoes`, `ObterMinhaSolicitacao`, `ListarSolicitacoes` e `ObterSolicitacao` registram a consulta
  (`Listou` ou `Visualizou`) no recurso `Solicitacao`.
- O download registra `Baixou`, como hoje.

## 8. Persistência

- Tabela `solicitacoes_do_cliente`: os campos da seção 3, com os carimbos de `Entidade`, chaves estrangeiras para
  `clientes`, `usuarios` e `atividades` sem cascata, e índices por `cliente_id` e por `criado_em`.
- `atividades.solicitacao_id`: anulável, chave estrangeira sem cascata, índice único parcial para atividades não
  excluídas.
- `documentos.solicitacao_id`: anulável, chave estrangeira sem cascata, com índice. As restrições de `documentos` são
  reescritas: `cliente_id` obrigatório também no `DocumentoDaSolicitacao`; `solicitacao_id` existe só nele.
- `ix_documentos_chave_armazenamento` deixa de ser único e continua como índice comum (a busca por chave é a da
  eliminação). Na fase B, junto da criação de atividade. Desfazer a migration volta o índice único e falha se já
  houver chave compartilhada: antes de reverter o esquema, é preciso tratar essas linhas.
- Desfazer a migration da fase A (`SolicitacoesDoCliente`): o `Down` apaga a tabela `solicitacoes_do_cliente`, com
  todas as solicitações, e a coluna `documentos.solicitacao_id`, e volta a restrição de `cliente_id` sem a categoria 6.
  Se existir algum documento da categoria 6 (`DocumentoDaSolicitacao`), mesmo excluído logicamente, a restrição
  recusa e o `Down` falha inteiro, desfeito pela transação, sem apagar nada. Antes de reverter, é preciso tratar
  esses documentos; sem eles, a reversão apaga todas as solicitações.

## 9. Testes

Unitários no `Domain`:

- As transições da solicitação: alterar, cancelar, vincular (com e sem confirmação), desvincular de volta a
  `Aguardando` e a `Cancelada`.
- A matriz de acesso a documentos com a categoria nova: três tipos de perfil, cliente dono e cliente de outro.
- Atividade com solicitação recusa troca de cliente.

Unitários na `Application`:

- A lista fixa das requisições abertas ao cliente, agora com as sete novas.
- `CriarAtividadeDaSolicitacao`: cada uma das cinco verificações; comentário só com a caixa marcada; só os
  documentos marcados viram documentos da atividade, com a mesma chave.
- Fixar em teste que nenhum caso de uso fora de `CriarAtividadeDaSolicitacao` cria documento com chave já existente.
- Só quem abriu altera, anexa, remove e cancela.

De integração, com PostgreSQL real:

- Usuário do cliente A não lista, não abre e não baixa nada da solicitação do cliente B.
- Produção não lista solicitações nem baixa documento de solicitação; baixa o documento levado para uma atividade em
  que é responsável.
- Cliente cancela depois de o diretor carregar o formulário: a criação é recusada; com a confirmação, passa.
- Duas criações simultâneas da mesma solicitação: só uma passa.
- Excluir a atividade devolve a solicitação a `Aguardando`.
- Documento da solicitação e documento da atividade com a mesma chave gravam e baixam o mesmo conteúdo; excluir um
  deles não afeta o download do outro.

De interface:

- Rotas do portal fechadas à equipe; rotas da Diretoria fechadas ao cliente e "não encontrada" para a Produção.
- Marcar todos e desmarcar todos.
- O aviso de cancelamento e o recarregamento depois de alteração.

## 10. Fases

Dois planos de implementação:

- **A — A solicitação.** Domínio, categoria nova de documento, persistência, os casos de uso do portal e da Diretoria
  (sem a criação de atividade), as telas do portal e a lista e a ficha da Diretoria. Ao fim, o cliente abre, edita e
  cancela; a Diretoria lista e lê.
- **B — A atividade a partir da solicitação.** `Atividade.SolicitacaoId`, `CriarAtividadeDaSolicitacao`, o formulário
  com o bloco novo, os botões de navegação nas duas fichas, a recusa de troca de cliente e a exclusão que desvincula.

A modelagem de domínio e a spec do portal são emendadas ao fim de cada fase. "Envio de arquivos pelo cliente" sai da
lista de fora do escopo do portal.

As fases A e B estão concluídas, e a spec do portal e a modelagem de domínio foram emendadas com as regras de cada uma.

**Registrado ao fim da fase A: o que foi feito, o que a fase B precisa fazer e o que está em aberto.**

- Feito na A: o agregado `SolicitacaoDoCliente` e a categoria `DocumentoDaSolicitacao` (valor 6), como nas seções 3 e 4.
  O documento da solicitação é visível ao cliente (`Documento.VisivelAoCliente`): a Produção nunca o alcança, nas
  listagens internas e no download. `ListarDocumentosDoCliente` com essa categoria responde "Categoria inválida para
  esta consulta.", como com o documento comum, e `EnviarDocumento` a recusa ("Categoria inválida para envio."): o
  documento da solicitação só nasce pela abertura ou pelo anexo.
- Feito na A: uma migration só, `SolicitacoesDoCliente`, só de DDL. Cria `solicitacoes_do_cliente` (urgência em
  inteiro, com `ck_solicitacoes_urgencia`; a versão é o `xmin`; chaves estrangeiras sem cascata; índices por
  `(cliente_id, criado_em)`, `criado_em`, `atividade_id` e `solicitante_id`) e `documentos.solicitacao_id` (chave
  estrangeira sem cascata e índice). Reescreve `ck_documentos_cliente_conforme_a_categoria` (a categoria 6 exige
  cliente) e cria `ck_documentos_solicitacao_so_na_categoria_6` (`solicitacao_id` existe só nessa categoria e é
  obrigatório nela). Ficam para a B, como na seção 8: `atividades.solicitacao_id` e a troca do índice único da chave
  de armazenamento.
- Feito na A: a abertura é uma gravação só (`ArquivoEnviado.GravarVariosAsync`). O nome e a presença de todos os
  arquivos são validados antes de o primeiro conteúdo ser aberto. Os conteúdos são abertos um por vez
  (`ArquivoParaEnviar.AbrirConteudo`), cada um só na hora de ser gravado e fechado antes do próximo: no Blazor Server
  a transferência do navegador começa na abertura, e um fluxo aberto à espera estoura o tempo sem dados. O limite de
  50 MB é conferido enquanto cada arquivo é gravado. Se um for recusado, se abrir ou gravar um conteúdo falhar, ou se
  a gravação no banco falhar, os arquivos já gravados são removidos e nada fica no banco.
- Feito na A: a tela recusa já na escolha, pelo nome, o arquivo acima de 50 MB e o arquivo vazio ("O arquivo
  vazio.pdf está vazio.", ou "Os arquivos a.pdf, b.pdf estão vazios."), na abertura e no anexo da edição; os demais
  arquivos da mesma escolha ficam. O servidor recusa o vazio do mesmo jeito ("O arquivo está vazio."), mas sem dizer
  qual.
- Feito na A: depois de abrir, a tela deixa um recado ("Solicitação enviada. A equipe verifica sua solicitação em até
  24 horas úteis.") e vai para a ficha, que o mostra uma vez, como o "Solicitação alterada." da edição. Recarregar a
  ficha ou voltar a ela pelo navegador não o repete; o endereço não leva parâmetro (`?enviada=1` deixou de existir).
- Feito na A: o documento da solicitação só muda pelas mãos do cliente que abriu a solicitação, enquanto ela aguarda
  (o anexo e a remoção). A Diretoria não exclui nem nega a solicitação nem os documentos dela: `ExcluirDocumento` e
  `RenomearDocumento` recusam a categoria 6 com `Validacao`, "Documento de solicitação só é alterado pelo cliente que
  a abriu.", e nada muda; nas outras categorias, os dois seguem como antes.
- Feito na A: os documentos de uma solicitação vêm do mais antigo para o mais novo (`CriadoEm`); os gravados no mesmo
  instante, como os de uma abertura com vários arquivos, pelo nome e depois pelo Id. A ordem é a mesma no
  repositório e no fake dos testes unitários.
- Feito na A: o documento recebe o cliente da solicitação (`solicitacao.ClienteId`), e não o do usuário que pede, na
  abertura e no anexo.
- Feito na A: o contador `VersaoDosDocumentos` (não auditado) sobe a cada anexo e a cada remoção. A linha da
  solicitação é regravada e o `xmin` muda: quem abriu a tela, ou o outro anexo simultâneo, antes da mudança recebe
  `Conflito` ("A solicitação foi alterada. Recarregue e tente de novo.") e nada é sobrescrito.
- Feito na A: o cliente vê a situação de `SituacaoParaOCliente` (`AguardandoAnalise`, `Cancelada`, `EmAndamento` e
  `Concluida`), calculada na própria consulta da lista: atividade viva concluída vale "Concluída", atividade viva vale
  "Em andamento" e, sem atividade viva, vale a da solicitação (cancelada ou aguardando análise). O filtro usa a mesma
  expressão.
- Feito na A: o trecho da descrição (`SolicitacoesOpcoes.Trecho`) mantém os espaços que ficam no corte, sem aparar, e
  nunca parte um par substituto (um emoji, por exemplo) ao meio: se o 140º caractere abriria um par, o trecho termina
  um antes.
- Feito na A: o cliente vê o nome de quem abriu cada solicitação do cliente, mas não o de quem enviou um documento: os
  documentos da solicitação saem sem autor.
- Feito na A: a guarda de rotas do portal aceita `minhas-solicitacoes/{id}` e `minhas-solicitacoes/{id}/editar`, nas
  duas guardas (aberta ao cliente e só do portal), quando o trecho do Id é um Guid para `Guid.TryParse`. É o mesmo
  critério da restrição `{Id:guid}` do roteador, e não uma expressão regular de formato: toda forma de Guid que o
  roteador aceita (32 dígitos, com chaves) monta uma página só do portal e nunca chega à equipe; o que o roteador
  recusa (36 hífens, por exemplo) não vale como Id. `/solicitacoes` e `/solicitacoes/{id}` não entram em nenhuma das
  duas listas: a Produção recebe "Página não encontrada" pela própria página, sem consulta.
- Feito na A: na edição, `Conflito` ou `NaoEncontrado` em qualquer operação da página (salvar, anexar ou remover
  documento) relê o formulário inteiro (campos, versão e documentos) e mostra a mensagem, para que o texto antigo não
  seja salvo com a versão nova. Se essa releitura não encontra a solicitação, a página passa ao estado de "não
  encontrada", sem a mensagem. Qualquer outra recusa (que não seja `Conflito` nem `NaoEncontrado`) relê só a versão e
  os documentos, sem tocar nos campos, e mostra a mensagem. Uma operação que dá certo só atualiza a versão e os
  documentos, e o que foi digitado fica. Se a releitura mostra que a solicitação não pode mais ser alterada, a tela
  volta à ficha com a mensagem. Na
  abertura, os arquivos podem ser escolhidos em mais de uma vez: cada escolha soma às anteriores, o limite de 10 vale
  para a soma e cada arquivo tem "Remover" antes do envio. Falha ao enviar mantém o formulário.
- Feito na A: o menu do cliente ganha "Solicitações" depois de Andamento, no menu lateral e no trilho (na barra
  inferior, em "Mais"); o da Diretoria, depois de Atividades, só no menu lateral (no trilho e na barra, por "Mais").
  O início do cliente ganha o atalho "Nova solicitação".
- Feito na A: a ficha da Diretoria não tem "Criar atividade". Com atividade viva vinculada, mostra o título e "Ver
  atividade"; com `AtividadeId` preenchido e a atividade excluída, mostra só a situação "Virou atividade", sem título
  e sem link.
- Feito na A: a lista de catorze requisições abertas ao cliente mudou nos quatro lugares: o teste que a fixa, a seção
  3 da spec do portal e os dois pontos da modelagem de domínio (seções 5 e 12).
- Feito na A: dos testes da seção 9 entraram os que não dependem da criação de atividade: as transições da
  solicitação, a matriz de acesso com a categoria nova, a lista fixa de catorze requisições, só quem abriu altera, o
  isolamento entre clientes, a Produção sem acesso, as rotas fechadas por perfil e a migration sobre um banco que já
  tem documentos das categorias de 1 a 5. O resto é da B.
- Para a B: `VincularAtividade` e `CriarAtividadeDaSolicitacao` precisam recusar uma atividade de outro cliente.
  `ObterAtividadeAsync` não filtra pelo cliente, e as duas fichas leem dela o título e a situação: sem essa recusa, o
  cliente leria o título de uma atividade que não é dele.
- Para a B: `ExcluirAtividade` precisa chamar `DesvincularAtividade` na mesma gravação. Até lá, a solicitação cuja
  atividade foi excluída continua com `AtividadeId`: o cliente a vê de novo como "Aguardando análise" ou "Cancelada",
  mas a Diretoria a vê como "Virou atividade", porque a situação do domínio vem de `AtividadeId`.
- Para a B: o resto da seção 8 (`atividades.solicitacao_id` com índice único parcial e o índice comum da chave de
  armazenamento) e a regra de código que substitui o índice único, com o teste que a fixa.
- Em aberto, sem decisão: se o cliente que pede pelo Id a solicitação de outro cliente, ou que pede
  `RemoverDocumentoDaSolicitacao` com o Id de um documento de outro cliente, deve deixar um registro `acesso_negado`.
  Hoje esses pedidos respondem `NaoEncontrado` e não registram nada (como a seção 4 determina); das respostas
  `NaoEncontrado`, só a do download de documento registra `acesso_negado`. As respostas `AcessoNegado`, a do portão
  inclusive, registram sempre. É decisão de produto.

**Registrado ao fim da fase B: o que foi feito, as decisões do plano e o que continua em aberto.**

- Feito na B: `Atividade.SolicitacaoId` (anulável), definido só em `Criar` e nunca alterado; `Guid.Empty` é erro de
  programação, e uma atividade criada de solicitação exige cliente ("Uma atividade criada a partir de uma solicitação
  precisa ter cliente."). `Alterar` recusa a troca de cliente antes de mudar qualquer coisa ("O cliente de uma atividade
  criada a partir de uma solicitação não pode ser trocado."); `SalvarAtividade` devolve a recusa como `Validacao`. O
  processo escolhido continua precisando ser do cliente, pela regra do vínculo.
- Feito na B: `VincularAtividade` recebe a `Atividade`, e não mais o Id, e confere nesta ordem: atividade nula
  (`ArgumentNullException`), `SolicitacaoId` da atividade diferente da solicitação (`ArgumentException`, erro de
  programação), cliente da atividade diferente do da solicitação (`ExcecaoDeDominio`, "A atividade precisa ser do cliente
  da solicitação.") e, só então, as regras de hoje (já em atividade, cancelada sem confirmação). É a recusa que a fase A
  pediu para a B: o cliente nunca lê o título de uma atividade que não é dele.
- Feito na B: `Documento.LevarParaAtividade(atividadeId, clienteDaAtividade)` é o único caminho que cria um documento com
  a chave de armazenamento de outro: devolve um `DocumentoAtividade` com o mesmo arquivo (nome, tipo, tamanho, hash e
  chave) e o nome do documento da solicitação; o original não muda. Categoria diferente de `DocumentoDaSolicitacao` é
  erro de programação (`InvalidOperationException`); cliente diferente do do documento é `ExcecaoDeDominio` ("O documento
  precisa ser do cliente da atividade."). A regra que substitui o índice único (seção 3) está no comentário de
  `Documento.cs`, e `ChaveCompartilhadaTests` a fixa por varredura de `src`: `.LevarParaAtividade(` só aparece em
  `CriarAtividadeDaSolicitacao.cs` (fora a declaração) e `new ArquivoDoDocumento(` só em `ArquivoEnviado.cs`, que grava
  chave nova, e em `Documento.cs`. Um caminho novo exige registrar a exceção na seção 3 antes de entrar no teste.
- Feito na B: uma migration só, `AtividadeDaSolicitacao`, só de DDL. Acrescenta `atividades.solicitacao_id` (anulável,
  chave estrangeira sem cascata), cria o índice único parcial `ix_atividades_solicitacao_id` (`excluido_em IS NULL`) e
  troca `ix_documentos_chave_armazenamento` por um índice comum. O `Down` volta o índice único e falha, desfeito pela
  transação, se já houver chave compartilhada; antes de reverter, é preciso tratar essas linhas (o caso não tem teste
  próprio). O EF não acusou dependência circular entre `atividades` e `solicitacoes_do_cliente`: uma gravação só insere a
  atividade e atualiza a solicitação, que já existia.
- Feito na B: `TipoErro.ConfirmacaoNecessaria`, último valor do enum, só de `CriarAtividadeDaSolicitacao`: a solicitação
  está cancelada e o diretor ainda não confirmou. A lista de catorze requisições abertas ao cliente não mudou, porque a
  criação é da Diretoria: o portão a nega ao cliente como a qualquer requisição não marcada.
- Feito na B: a validação de cliente, processo e área saiu de `SalvarAtividade` para `ValidacaoDoVinculo.ValidarAsync`,
  usada pelos dois casos de uso, sem mudar mensagens nem comportamento.
- Feito na B: `CriarAtividadeDaSolicitacao` confere na ordem da seção 5. Os documentos marcados valem se forem vivos,
  da categoria `DocumentoDaSolicitacao` e desta solicitação; qualquer outro (removido, inexistente, de outra solicitação,
  comum) dá `Validacao`, "Um documento marcado não está mais nesta solicitação."; um Id repetido na lista vale uma vez.
  O comentário é a descrição da solicitação, tal como está, com o diretor como autor, e só nasce com a caixa marcada.
  `ExcecaoDeConcorrencia` (a linha da solicitação mudou entre a leitura e a gravação) responde `Conflito` com a mensagem
  de "alterada"; `ExcecaoDeConflito` (o índice único de `atividades.solicitacao_id`) responde `Conflito` com "Esta
  solicitação já virou atividade.", sem olhar qual restrição disparou: hoje é a única alcançável nessa gravação, e se
  outra passar a ser, vale filtrar por `Restricao`. Duas criações simultâneas: na gravação, a inserção da atividade vem
  antes do `UPDATE` da solicitação, então a segunda criação esbarra no índice parcial. O teste da corrida aceita as duas
  mensagens, a de "alterada" e a de "já virou atividade", porque essa ordem é do EF, e não uma garantia do manipulador.
- Decisão do plano: a ordem interna do manipulador. Passadas as verificações, ele monta a atividade, o comentário e os
  documentos levados e vincula a solicitação, tudo o que o domínio pode recusar, antes de qualquer `Adicionar` nos
  repositórios, e grava uma vez só: uma recusa do domínio no meio não deixa nada adicionado nem a solicitação alterada em
  memória.
- Feito na B: o caminho do `xmin` (o cliente altera a solicitação entre a leitura e a gravação do diretor) não tem teste
  de integração próprio, porque não há ponto de injeção para parar o manipulador no meio. O unitário cobre a tradução de
  `ExcecaoDeConcorrencia` em `Conflito`; os testes de cancelamento e de remoção de documento depois do carregamento do
  formulário cobrem a conferência da versão antes da gravação; e a corrida de duas criações cobre o índice único.
- Feito na B: `ExcluirAtividade` carrega a solicitação de origem antes de remover e chama `DesvincularAtividade` na mesma
  gravação, só se ela ainda aponta para aquela atividade; uma solicitação que já aponta para outra não é tocada, e uma
  que não existe mais é tolerada. Ela volta a `Aguardando` ou a `Cancelada`, conforme `CanceladaEm`, e uma atividade nova
  pode ser criada dela (o índice parcial ignora a excluída). Com a solicitação rastreada no mesmo contexto, o EF já zera
  o `AtividadeId` ao remover a atividade; a chamada explícita fica pelo contrato do domínio e pelos unitários, e o teste
  de integração não distingue a falta dela.
- Feito na B: `ObterAtividade` devolve `SolicitacaoId`, último parâmetro de `AtividadeDetalhe`, só à Diretoria; a Produção
  recebe nulo. A ficha da atividade usa o `SolicitacaoId` como ação de cadastro: com ele, a faixa e o "Mais ações"
  aparecem mesmo sem outra ação.
- Feito na B: o formulário de atividade em `/atividades/nova?solicitacao={id}`, com o bloco, o comentário e os documentos
  da seção 6 num componente próprio, `SolicitacaoNoFormulario`, porque o formulário já tinha 436 linhas. O cliente fica
  travado na criação e na edição: sem busca de cliente, sem "Sem cliente", e a busca de processo traz só os do cliente.
  A solicitação que já virou atividade, em endereço aberto à mão, mostra só o aviso "Esta solicitação já virou
  atividade." com "Ver atividade", sem formulário; a cancelada carrega normalmente, e o aviso dela só vem na gravação.
  Cancelar e voltar levam à ficha da solicitação, com a origem "Solicitação". Nas respostas da gravação, o que o diretor
  digitou fica; em "alterada", o documento que ele desmarcou continua desmarcado, o novo entra marcado e o removido
  some; em "cancelada", o aviso traz "Criar mesmo assim", que reenvia com a versão relida, e "Voltar à solicitação".
- Feito na B: "Criar atividade", botão primário acima das colunas da ficha da solicitação, só em `Aguardando`, e "Ver
  solicitação" na ficha da atividade, como na seção 6. Nenhum dos dois aparece para quem não é da Diretoria.
- Feito na B: dos testes da seção 9, entraram todos os que dependiam da criação: as recusas do domínio e a ordem das
  cinco verificações; o comentário só com a caixa; só os documentos marcados, com a mesma chave; a varredura da chave
  compartilhada; o isolamento entre clientes, inclusive o documento levado, que o cliente não alcança; a Produção que
  baixa o documento levado em que é responsável, e não o da solicitação; o cancelamento e a remoção de documento depois
  de carregado o formulário; a corrida de duas criações; a exclusão que devolve a solicitação; o documento levado e o
  da solicitação que baixam o mesmo conteúdo, com a exclusão de um não afetando o outro; e, na interface, "Marcar todos"
  e "Desmarcar todos", o aviso de cancelamento, o recarregamento depois da alteração e os dois botões de navegação.
- Em aberto, sem decisão: a pergunta da auditoria, herdada da fase A (acima) e sem resposta nova. Se o cliente que pede
  pelo Id a solicitação de outro cliente, ou um documento de outro cliente pelo `RemoverDocumentoDaSolicitacao`, deve
  deixar um registro `acesso_negado`; hoje não deixa. É decisão de produto.
