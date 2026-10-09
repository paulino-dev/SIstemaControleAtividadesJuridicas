# Portal do Cliente — perfil Cliente e documentos visíveis ao cliente

Data: 2026-10-08
Estado: aguardando revisão
Emenda: `2026-10-06-modelagem-dominio-design.md`, seções 5, 6, 7, 8, 10, 12 e 13

## 1. Objetivo

O cliente do escritório passa a ser um agente do sistema. Ele faz login para baixar os documentos que o escritório disponibiliza a ele e para acompanhar, de forma macro, o andamento das atividades ligadas a ele. Não altera nada além da própria senha.

Entram duas áreas de documentos:

- **Documentos do cliente**: arquivos de um cliente, lidos por ele e pela Diretoria.
- **Documentos comuns**: arquivos lidos por todos os clientes e pela Diretoria.

### Critérios de sucesso

- Um usuário de perfil Cliente não alcança nenhum dado interno: cadastros, processos, agenda, fichas de atividade, auditoria, Documentos Gerais, Contratos de Honorários, Documentos de Atividades.
- Um cliente nunca lê documento ou atividade de outro cliente.
- Um usuário de Produção nunca lê as duas áreas novas.
- Nenhum arquivo entra em Documentos comuns sem que o diretor confirme o aviso de que ele fica visível a todos os clientes.
- Um caso de uso criado no futuro nasce fechado para o perfil Cliente, sem que alguém precise lembrar de fechá-lo.

## 2. Decisões

| Tema | Decisão |
|---|---|
| Identidade do cliente | Usuário comum, com um terceiro tipo de perfil. Não há tabela, login ou cookie próprios |
| Vínculo | Cada usuário-cliente pertence a exatamente um cliente; um cliente pode ter vários usuários |
| Isolamento | Portão central no executor de requisições: o perfil Cliente só roda o que está marcado como aberto a ele |
| Andamento | Lista de atividades com situação simplificada: "Em andamento" ou "Concluída" |
| Documentos | Duas categorias novas do agregado `Documento`, sem lista de acesso |
| Alerta dos Documentos comuns | Confirmação antes do envio, exigida também pelo caso de uso |
| Cliente inativo | Os usuários dele perdem o acesso até a reativação |

Alternativas descartadas: negar o perfil Cliente caso de uso por caso de uso (a proteção dependeria de ninguém esquecer, agora e nas fases futuras) e manter o cliente como identidade separada (duplicaria login, bloqueio, sessão, política de senha e o vínculo da auditoria com o usuário).

## 3. Identidade

### Perfil

`TipoPerfil` ganha o valor `Cliente`.

- Existe um único perfil desse tipo, "Cliente", criado pelo semeador: junto dos outros dois em banco vazio e, num banco que já existe, na primeira inicialização depois da atualização. A gravação passa pelos interceptores, como qualquer outra. Se já houver um perfil com o nome "Cliente", o novo nasce como "Cliente (portal)".
- A criação de perfil continua aceitando só `Diretoria` e `Producao`. Dois perfis de cliente não teriam diferença entre si, porque o perfil Cliente não participa de listas de acesso.
- O perfil "Cliente" pode ser renomeado. Inativá-lo segue a regra de hoje: só sem usuário ativo.
- Criar o perfil "Cliente" não o inclui em Documento Geral nenhum.

### Usuario

| Campo | Regra |
|---|---|
| `ClienteId` | Preenchido se e somente se o perfil do usuário é do tipo `Cliente` |

Regras:

- Só a Diretoria cria e edita usuários-cliente, na tela de Usuários. Ao escolher o perfil "Cliente", a busca de cliente aparece e é obrigatória. O cliente escolhido precisa existir e estar ativo.
- Um usuário não troca entre o tipo `Cliente` e os tipos internos. A edição que tentar é recusada como validação.
- A Diretoria pode trocar o cliente vinculado, para corrigir um engano. A troca renova a `VersaoDeSessao` do usuário e gera o evento `usuario_cliente_alterado`.
- `NumeroOab` não se aplica a usuário-cliente e fica nulo.
- Usuário-cliente não é responsável nem revisor de atividade, não participa de evento e não entra em lista de acesso de documento. As consultas que alimentam essas escolhas deixam de trazê-lo, e o domínio recusa a inclusão.
- A regra do último diretor não muda: usuário-cliente não conta como diretor.
- A ficha do cliente mostra à Diretoria, só para leitura, os usuários vinculados a ele, com nome, e-mail e se estão ativos.

### Cliente inativo

- O usuário de um cliente inativo não tem acesso. O login recusado responde como uma senha errada: mensagem de credenciais inválidas e evento `login_falha`; na tentativa que aciona o bloqueio, mensagem de bloqueio e `login_bloqueado`. A tentativa conta para o bloqueio, e o evento leva `motivo`: `cliente_inativo` (cliente inativo ou inexistente) ou `vinculo_incoerente` (perfil `Cliente` sem cliente, ou perfil interno com cliente). Uma sessão aberta é recusada na operação seguinte: o cookie é rejeitado na próxima requisição HTTP, e uma tela já aberta recebe acesso negado.
- Reativar o cliente devolve o acesso, sem mexer nos usuários.
- O estado do cliente é lido do banco a cada operação, como já se faz com o perfil e o estado do usuário.

### Portão do cliente

`UsuarioAutenticado` passa a carregar `ClienteId` e `EhCliente`.

O executor de requisições consulta o ator antes de chamar o caso de uso:

- Sem ator (login, por exemplo), o portão não age; cada caso de uso mantém a verificação que já faz.
- Ator de tipo `Diretoria` ou `Producao`: nada muda.
- Ator de tipo `Cliente`: a requisição só segue se estiver marcada com `IRequisicaoAbertaAoCliente`. Caso contrário, o executor responde `AcessoNegado` sem chamar o caso de uso, e a tentativa gera o evento `acesso_negado` com o nome da requisição.

Requisições abertas ao cliente:

| Requisição | Para quê |
|---|---|
| `AutenticarUsuario` | Entrar. Não usa o ator; sem a marca, quem tem uma sessão de cliente aberta no navegador não conseguiria entrar de novo |
| `ObterMeuAcesso` | A interface saber quem é o usuário |
| `AlterarMinhaSenha` | Trocar a própria senha |
| `BaixarDocumento` | Baixar; quem decide é a regra de acesso do documento |
| `ListarMeusDocumentos` | Documentos do cliente do usuário |
| `ListarDocumentosComuns` | Documentos comuns |
| `ListarMeuAndamento` | Andamento das atividades do cliente do usuário |

Regras:

- Abrir mais uma requisição ao cliente é marcar o tipo dela e acrescentá-la à lista fixada em teste. As duas mudanças aparecem na revisão.
- As consultas do portal não recebem Id de cliente: usam o `ClienteId` do ator, lido do banco. Não há como pedir dados de outro cliente alterando a requisição.
- Toda requisição não marcada responde com `Resultado`, para o portão poder negá-la. `ObterMeuAcesso` é a única que responde outro tipo e está marcada.
- O portão custa uma leitura a mais de usuário e perfil por requisição.

### Matriz de acesso

A matriz da seção 5 da modelagem ganha uma coluna e duas linhas:

| Recurso | Diretoria | Produção | Cliente |
|---|---|---|---|
| Usuários e perfis | Tudo | Sem acesso | Sem acesso |
| Áreas de atuação | Tudo | Lê os nomes | Sem acesso |
| Clientes e processos | Tudo | Somente leitura | Sem acesso |
| Documentos Gerais | Tudo | Leitura conforme lista de acesso | Sem acesso |
| Contratos de Honorários | Tudo | Sem acesso | Sem acesso |
| Documentos de Atividades | Tudo | Os das atividades em que é responsável | Sem acesso |
| Documentos do cliente | Tudo | Sem acesso | Leitura dos do próprio cliente |
| Documentos comuns | Tudo | Sem acesso | Leitura |
| Atividades | Tudo | Somente as em que é responsável | Andamento das do próprio cliente (seção 5) |
| Agenda | Todos os eventos | Os seus | Sem acesso |
| Auditoria | Consulta | Sem acesso | Sem acesso |

## 4. Documentos

### Categorias novas

`CategoriaDocumento` ganha dois valores:

| Categoria | `ClienteId` | `AtividadeId` | Lista de acesso | Quem lê | Quem envia, renomeia e exclui |
|---|---|---|---|---|---|
| `DocumentoDoCliente` | Obrigatório | Nulo | Não há | Diretoria e os usuários daquele cliente | Diretoria |
| `DocumentoComum` | Nulo | Nulo | Não há | Diretoria e todo usuário de perfil `Cliente` | Diretoria |

Armazenamento, limite de 50 MB, recusa de arquivo vazio, download como anexo com tipo neutro, exclusão lógica e ausência de versionamento valem como nas categorias existentes.

### Regra de acesso

Continua sendo uma única função de domínio. Ela passa a receber o cliente do usuário (nulo para quem não é do tipo `Cliente`) e decide nesta ordem:

1. Perfil de tipo `Diretoria` acessa sempre.
2. Perfil de tipo `Cliente` acessa `DocumentoDoCliente` cujo `ClienteId` é o seu e `DocumentoComum`. Não acessa nenhuma outra categoria, ainda que o usuário ou o perfil conste em alguma lista.
3. `DocumentoDoCliente` e `DocumentoComum` nunca são acessados por perfil de tipo `Producao`.
4. Contrato de Honorários, Documento de Atividade e Documento Geral seguem as regras atuais.

Complementos:

- As duas categorias novas não têm lista de acesso. Definir acesso nelas é recusado pelo domínio, como já ocorre no documento de atividade.
- O domínio recusa perfil de tipo `Cliente` e usuário de perfil `Cliente` em qualquer lista de acesso, de qualquer categoria.
- Baixar um documento sem ter acesso responde como não encontrado e gera `acesso_negado` com o documento, como hoje.

### Mudança nos Documentos Gerais

Um Documento Geral nasce liberado para todos os perfis **internos** ativos (tipos `Diretoria` e `Producao`), e não mais para todos os perfis ativos. O texto da seção 7 da modelagem é corrigido nesse ponto.

### Envio

- `DocumentoDoCliente` é enviado pelo caso de uso de envio que já existe, que passa a aceitar essa categoria. O cliente precisa existir; cliente inativo continua recebendo documentos.
- `DocumentoComum` tem um caso de uso próprio, `EnviarDocumentoComum`, sem cliente. A requisição leva o campo `ConfirmouVisibilidade`; sem ele verdadeiro, o envio é recusado como validação, com a mensagem "Confirme que o documento ficará visível para todos os clientes."
- Renomear e excluir usam os casos de uso atuais, que já operam por Id e são da Diretoria.

### Listagens

| Consulta | Quem | O que traz |
|---|---|---|
| `ListarDocumentosDoCliente` com a categoria `DocumentoDoCliente` | Diretoria; Produção recebe `AcessoNegado` | Os documentos do cliente pedido |
| `ListarMeusDocumentos` | Perfil `Cliente`; os demais recebem `AcessoNegado` | Os `DocumentoDoCliente` do cliente do ator |
| `ListarDocumentosComuns` | Diretoria e perfil `Cliente`; Produção recebe `AcessoNegado` | Todos os `DocumentoComum` |

- As três são paginadas, do mais recente para o mais antigo, como as listagens atuais.
- O cliente recebe nome, tamanho e data de envio de cada documento. O nome de quem enviou não vai para o cliente.
- Para a Diretoria, a resposta continua trazendo quem enviou.

## 5. Andamento

`ListarMeuAndamento` é a única visão de atividades do perfil `Cliente`.

- Traz as atividades vivas cujo `ClienteId` é o do ator. Atividade avulsa não aparece; atividade excluída não aparece.
- Aparece desde a criação, em qualquer status.
- Campos de cada linha: título, nome da área de atuação, processo (número CNJ; na falta dele, o título do processo; vazio sem processo), situação e data de conclusão.
- Situação: `Concluída` quando o status é `Concluida`; `Em andamento` em todos os demais (`Criada`, `Direcionada`, `Feita`, `ParaCorrecao`, `Corrigida`, `Reaberta`). Uma atividade reaberta volta a `Em andamento` e perde a data de conclusão.
- A resposta traz os dois totais do cliente: em andamento e concluídas.
- Filtro por situação. Paginada. Ordem: em andamento primeiro, pela criação mais recente; depois as concluídas, da conclusão mais recente para a mais antiga.

Não chegam ao cliente: descrição, data de entrega, condição de atrasada, responsáveis, revisor, direcionamentos, comentários, histórico de status, motivos de devolução e de reabertura e documentos da atividade. Não há ficha da atividade para o cliente.

Consequência: o título de toda atividade com cliente passa a ser lido por esse cliente. O formulário de atividade mostra, quando há cliente selecionado, o lembrete "O título aparece para o cliente no portal." Não há opção de ocultar uma atividade do cliente.

## 6. Telas

### Menu por tipo de perfil

| Tipo | Itens |
|---|---|
| `Diretoria` | Os atuais, mais "Documentos comuns" |
| `Producao` | Os atuais |
| `Cliente` | Início, Andamento, Meus documentos, Documentos comuns, Minha senha |

Uma página interna aberta por um usuário-cliente mostra "Página não encontrada" e não é montada: não chega a consultar nada. O cookie de um usuário de perfil `Cliente` leva uma marca de portal (`atividades:portal`), que serve só para o layout saber, sem esperar consulta, que não deve montar páginas internas. A marca não autoriza nada: perfil, estado ativo e cliente continuam lidos do banco a cada operação, e quem nega é o portão. Como o usuário não troca entre o tipo `Cliente` e os internos, a marca não envelhece. Uma página do portal aberta por quem não é cliente, idem, com exceção de Documentos comuns, que a Diretoria usa.

### Páginas

| Rota | Quem | Conteúdo |
|---|---|---|
| `/` | Todos | Para o cliente: saudação, os dois totais do andamento e atalhos para as três áreas. Para os demais, o início atual |
| `/andamento` | Cliente | A lista da seção 5, com os totais no topo e o filtro por situação |
| `/meus-documentos` | Cliente | Documentos do cliente, com download |
| `/documentos-comuns` | Cliente e Diretoria | Documentos comuns. O cliente só baixa; a Diretoria envia, renomeia e exclui |

### Ficha do cliente (Diretoria)

- Quarta área de documentos, "Documentos do cliente (visíveis ao cliente)", com envio, renomear e excluir, sem editor de acesso. Não aparece para a Produção.
- Lista de leitura "Usuários do cliente".

### Confirmação dos Documentos comuns

Ao clicar em Enviar, o arquivo não sobe. A tela mostra o aviso:

> Documentos desta área ficam visíveis para todos os clientes do escritório. Não envie aqui arquivos de um cliente específico.

com os botões "Confirmar envio" e "Cancelar". Só a confirmação envia a requisição, com `ConfirmouVisibilidade` verdadeiro. Cancelar mantém o arquivo escolhido e não envia nada. O aviso aparece a cada envio.

### Usuários (Diretoria)

- O formulário mostra a busca de cliente quando o perfil escolhido é o "Cliente" e esconde o campo da OAB.
- A lista mostra o cliente do usuário-cliente.

## 7. Auditoria

Não há tabela nova.

- Login, falha, bloqueio, logout e troca de senha de usuário-cliente geram os eventos de segurança existentes.
- O login recusado por causa do cliente gera `login_falha`, ou `login_bloqueado` na tentativa que aciona o bloqueio, com `motivo` `cliente_inativo` ou `vinculo_incoerente` (seção 3).
- `usuario_cliente_alterado`: evento de segurança no usuário, com o cliente anterior e o novo, na mesma transação da troca.
- `ListarMeusDocumentos` e `ListarDocumentosComuns` registram `Listou` no recurso `Documento`. `ListarMeuAndamento` registra `Listou` no recurso `Atividade`. O download registra `Baixou`, como hoje.
- A criação, a renomeação e a exclusão de documentos das categorias novas aparecem pelos registros do próprio documento.
- A requisição barrada pelo portão gera `acesso_negado` com o nome da requisição.
- A consulta de auditoria continua exclusiva da Diretoria e filtra pelos usuários-cliente como por qualquer outro usuário.

## 8. Tratamento de erros

- Requisição não aberta ao cliente: `AcessoNegado`.
- Documento de outro cliente, ou de categoria interna, pedido por um cliente: `NaoEncontrado`, com o evento de acesso negado.
- Envio de Documento comum sem a confirmação: `Validacao`.
- Usuário com perfil `Cliente` sem cliente, ou com cliente e perfil interno: `Validacao`; o domínio recusa.

## 9. Persistência

- `usuarios.cliente_id`: nulo, chave estrangeira para `clientes` sem cascata, com índice.
- O banco não garante sozinho o par perfil–cliente, porque o tipo fica na tabela de perfis; quem garante é o domínio, com teste de integração.
- Restrições de `documentos` reescritas: `cliente_id` é obrigatório nas categorias Documento Geral, Contrato de Honorários e Documento do cliente, e nulo no Documento comum; `atividade_id` continua existindo só no Documento de Atividade.
- Não há migration de dados: o perfil "Cliente" é garantido pelo semeador (seção 3).

## 10. Testes

Unitários no `Domain`:

- Regra de acesso a documentos: a matriz completa de três tipos de perfil por cinco categorias, incluindo cliente diferente do dono do documento e usuário-cliente que conste em lista.
- Invariante do `ClienteId` do usuário e recusa da troca entre tipo `Cliente` e tipos internos.
- Recusa de usuário-cliente como responsável, revisor e participante, e de perfil ou usuário-cliente em lista de acesso.
- Documento comum sem cliente; Documento do cliente exige cliente.

Unitários na `Application`:

- O portão: para cada requisição existente, descoberta por reflexão, um ator de tipo `Cliente` recebe `AcessoNegado` quando ela não está marcada, e o caso de uso não é chamado.
- A lista das requisições marcadas é exatamente a da seção 3.
- Toda requisição não marcada responde com `Resultado`.
- `EnviarDocumentoComum` recusa sem a confirmação.
- A situação do andamento para cada um dos sete status.

De integração, com PostgreSQL real:

- Usuário do cliente A não lista nem baixa documento do cliente B, nem vê atividade do cliente B.
- Produção não lista nem baixa `DocumentoDoCliente` e `DocumentoComum`.
- Documento Geral enviado depois da criação do perfil "Cliente" não o tem na lista de acesso.
- Usuário de cliente inativo não faz login e tem a sessão recusada; reativar devolve o acesso.
- Trocar o cliente do usuário derruba a sessão e gera o evento.
- Andamento: avulsas e excluídas fora; totais e ordem.

De interface:

- Páginas internas fechadas ao cliente; páginas do portal fechadas a quem não é cliente.
- Menu por tipo de perfil.
- O envio de Documento comum só acontece depois da confirmação.

## 11. Fases

A fase 7 é a de complexidade e produtividade (`2026-10-08-complexidade-e-produtividade-design.md`). O portal é a fase 8, em dois planos de implementação:

- **8A — Perfil Cliente e portão.** Tipo de perfil, vínculo do usuário com o cliente, cliente inativo, portão no executor, recusas do domínio, mudança dos Documentos Gerais, tela de Usuários e a lista "Usuários do cliente". Ao fim, um cliente faz login, vê um início vazio, troca a senha e não alcança nada interno. Nesta fase o portão abre só `AutenticarUsuario`, `ObterMeuAcesso` e `AlterarMinhaSenha`.
- **8B — Documentos e andamento.** As duas categorias, os casos de uso e as consultas do portal, a quarta área na ficha do cliente, a página de Documentos comuns com a confirmação, Meus documentos, Andamento e o início do cliente. O portão passa a abrir também `BaixarDocumento` e as três consultas do portal.

As duas fases 7 e 8 mexem no menu, no formulário de atividade, na modelagem de domínio e criam migration. A que for integrada por último refaz a sua migration sobre a outra e resolve o menu.

A modelagem de domínio é emendada ao fim de cada fase com as regras fixadas, como nas fases anteriores.

## 12. Fora do escopo

Envio de arquivos pelo cliente; aviso ao cliente por e-mail ou mensagem; cadastro do usuário pelo próprio cliente e recuperação de senha por e-mail; um usuário representando mais de um cliente; ocultar uma atividade do cliente; ficha, comentários ou anexos de atividade para o cliente; processos e agenda para o cliente; publicar um Documento Geral existente na área do cliente; versionamento de documentos.

"Portal do cliente" sai da lista de fora do escopo da modelagem de domínio.
