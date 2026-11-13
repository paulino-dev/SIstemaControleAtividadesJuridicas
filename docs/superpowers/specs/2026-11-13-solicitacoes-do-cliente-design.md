# Solicitações do cliente

Data: 2026-11-13
Estado: aguardando revisão
Emenda: `2026-10-08-portal-do-cliente-design.md` (seções 3, 4, 6 e 12) e `2026-10-06-modelagem-dominio-design.md`

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
- `VincularAtividade(atividadeId, confirmouCancelada)`: em `Aguardando`; em `Cancelada`, só com
  `confirmouCancelada`; em `EmAtividade`, recusada.
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
- Em qualquer situação diferente de `Aguardando`, as alterações respondem `Validacao`: "Esta solicitação não pode
  mais ser alterada."
- `VersaoEsperada` diferente da atual responde `Conflito`, como nas atividades.
- O download usa `BaixarDocumento`, já aberto ao cliente; quem decide é a regra de acesso do documento.
- A lista de requisições abertas ao cliente, fixada em teste, ganha as sete acima.

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
| `/solicitacoes` | Lista da mais antiga para a mais nova, com filtro por situação (abre em "Aguardando") e por cliente |
| `/solicitacoes/{id}` | A ficha completa, com download dos documentos. Em `Aguardando`, botão "Criar atividade". Em atividade, botão "Ver atividade" |

- Menu: "Solicitações" depois de "Atividades", só para a Diretoria. A Produção que abrir a rota recebe "Página não
  encontrada" pela própria página.
- Solicitação cancelada não mostra "Criar atividade": o aviso de cancelamento só aparece quando o cancelamento
  acontece durante a criação.

### Formulário de atividade com `?solicitacao={id}`

- Bloco "Solicitação do cliente" no topo: quem abriu, quando, urgência e a descrição.
- O cliente vem preenchido e travado, sem a busca de cliente. A busca de processo mostra só os processos desse cliente.
- A descrição da atividade vem preenchida com a descrição da solicitação e pode ser editada. O título vem vazio.
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

Atividade com `SolicitacaoId`: botão "Ver solicitação" no cabeçalho, para `/solicitacoes/{id}`. A Produção não o vê.

## 7. Auditoria

- As gravações de solicitação, de documento e de atividade aparecem pelos registros de entidade, como hoje.
- `ListarMinhasSolicitacoes`, `ObterMinhaSolicitacao`, `ListarSolicitacoes` e `ObterSolicitacao` registram a consulta
  (`Listou` ou `Consultou`) no recurso `Solicitacao`.
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
