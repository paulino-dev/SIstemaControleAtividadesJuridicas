# Sistema de Controle de Atividades Jurídicas — Modelagem de Domínio

Data: 2026-10-06
Estado: aguardando revisão

## 1. Objetivo

Entregar a um escritório de advocacia uma ferramenta para criar, direcionar, executar, revisar e acompanhar atividades jurídicas, com cadastro de clientes, processos, documentos e agenda, e trilha de auditoria completa.

Esta spec define o modelo de domínio e as decisões de arquitetura. Ela não contém código. O uso de IA para produtividade é um objetivo posterior e não faz parte deste documento.

### Critérios de sucesso

- A Diretoria consegue criar uma atividade, direcioná-la a um ou mais responsáveis, revisá-la e concluí-la, com todo o percurso registrado.
- Um usuário de Produção enxerga apenas as atividades em que é responsável e apenas o seu próprio direcionamento.
- Contratos de Honorários nunca ficam acessíveis a usuários de Produção.
- Toda alteração e toda consulta feita no sistema pode ser rastreada pela Diretoria.

## 2. Decisões de arquitetura

| Tema | Decisão |
|---|---|
| Estilo | Monolito, DDD em camadas, escritório único (sem multi-tenant) |
| Plataforma | .NET 10, Blazor com renderização Interactive Server |
| Persistência | EF Core + PostgreSQL |
| Solução | Quatro projetos: `Domain`, `Application`, `Infrastructure`, `Web` |
| Chamada UI → aplicação | As páginas Blazor chamam os casos de uso pelo despachante (que passa pelo executor); não há API HTTP intermediária |

### Dependências entre projetos

- `Domain`: nenhuma dependência de pacote ou projeto. Agregados, entidades, value objects, enums, exceções de domínio.
- `Application`: depende de `Domain`. Casos de uso (uma pasta por caso), interfaces de repositório e de serviços externos, autorização, `Resultado`.
- `Infrastructure`: depende de `Application` e `Domain`. `DbContext`, configurações de mapeamento (uma classe por entidade), repositórios, unidade de trabalho, armazenamento de arquivos, interceptor de auditoria, migrations.
- `Web`: depende das três. Componentes e páginas Blazor, autenticação, composição de dependências.

Dentro de cada camada há uma pasta por contexto.

### Referência

O repositório `C:\GitHub\NossoCT` serve de referência em duas partes distintas:

- `Administrativo` (Blazor Interactive Server, Domain/Application/Infrastructure/Web): referência de estrutura, entidades ricas, value objects, repositórios, unidade de trabalho e mapeamento por classe de configuração.
- `Comuns` + `NossoCTProject` (ASP.NET MVC, não Blazor): referência conceitual para perfis, desenho da auditoria (interceptor + contexto do ator + tabela com diferenças em JSON) e abstração de armazenamento de arquivos.

Não são copiados: multi-tenant por banco, mensageria, microserviços, controller CRUD genérico com bind de entidade, catálogo de permissões granulares, permissões em claims de cookie, arquivos binários dentro do banco.

## 3. Contextos

| Contexto | Agregados | Depende de |
|---|---|---|
| Identidade | `Usuario`, `Perfil` | — |
| Cadastros | `Cliente`, `Processo`, `AreaAtuacao` | Identidade |
| Documentos | `Documento` | Identidade |
| Atividades | `Atividade` | Identidade, Cadastros, Documentos |
| Agenda | `Evento` | Identidade, Cadastros, Atividades |
| Auditoria | `RegistroAuditoria`, `RegistroAcesso` | Identidade |

Agregados de contextos diferentes se referenciam apenas por Id.

```mermaid
erDiagram
    Perfil ||--o{ Usuario : possui
    AreaAtuacao ||--o{ Processo : classifica
    AreaAtuacao |o--o{ Atividade : classifica
    Processo ||--|{ ProcessoCliente : tem
    Cliente ||--o{ ProcessoCliente : participa
    Cliente |o--o{ Atividade : demanda
    Processo |o--o{ Atividade : origina
    Atividade ||--o{ AtividadeResponsavel : tem
    Atividade ||--o{ AtividadeComentario : tem
    Atividade ||--|{ AtividadeHistoricoStatus : registra
    Usuario ||--o{ AtividadeResponsavel : executa
    Usuario ||--o{ Atividade : revisa
    Cliente |o--o{ Documento : possui
    Atividade |o--o{ Documento : anexa
    Documento ||--o{ DocumentoPerfilAcesso : libera
    Documento ||--o{ DocumentoUsuarioAcesso : libera
    Perfil ||--o{ DocumentoPerfilAcesso : recebe
    Usuario ||--o{ DocumentoUsuarioAcesso : recebe
    Evento ||--|{ EventoParticipante : tem
    Usuario ||--o{ EventoParticipante : participa
```

## 4. Regras comuns a todas as entidades

- `Id`: Guid versão 7.
- Carimbos: `CriadoEm`, `CriadoPor`, `AtualizadoEm`, `AtualizadoPor`.
- Exclusão sempre lógica: `ExcluidoEm`, `ExcluidoPor`, com filtro global nas consultas. Nenhum registro de negócio é removido fisicamente.
- Datas e horas gravadas em UTC e exibidas no fuso do escritório, definido em configuração.
- As tabelas de auditoria (seção 10) são exceção: não têm carimbos de atualização nem exclusão lógica.

## 5. Identidade

### Perfil

| Campo | Regra |
|---|---|
| `Nome` | Obrigatório, único |
| `Tipo` | Enum `TipoPerfil`: `Diretoria`, `Producao` ou `Cliente` |
| `Ativo` | Perfil inativo não pode ser atribuído a usuário |

Semeado com três perfis: "Diretor" (`Diretoria`), "Produção" (`Producao`) e "Cliente" (`Cliente`). Perfis criados depois escolhem `Diretoria` ou `Producao`. As regras de negócio consultam o `Tipo`; a lista de acesso de documentos usa o `PerfilId`.

Regras:

- Existe um único perfil de tipo `Cliente`, garantido pelo semeador a cada inicialização, inclusive em banco que já existia. Se já houver um perfil chamado "Cliente", o novo nasce como "Cliente (portal)". Criar um perfil desse tipo pela aplicação é recusado ("O perfil de cliente é único e já existe."). O perfil pode ser renomeado.
- Somente a Diretoria cria, renomeia, inativa e reativa perfis. Não há exclusão de perfil.
- O `Tipo` não muda depois de criado: trocar o tipo mudaria de uma vez o acesso de todos os usuários do perfil.
- O nome é único sem diferenciar maiúsculas.
- Um perfil com usuário ativo não pode ser inativado; antes, troca-se o perfil desses usuários. Um usuário cujo perfil está inativo não pode ser reativado enquanto o perfil não for reativado ou trocado.
- Inativar um perfil não revoga o que ele já recebeu: ele permanece nas listas de acesso em que estava (seção 7).

### Usuario

| Campo | Regra |
|---|---|
| `Nome` | Obrigatório |
| `Email` | Obrigatório, único; é o login |
| `SenhaHash` | PBKDF2; nunca exposto nem auditado |
| `PerfilId` | Obrigatório; exatamente um perfil por usuário |
| `NumeroOab` | Opcional; sempre nulo no usuário de cliente |
| `ClienteId` | Preenchido se e somente se o perfil é de tipo `Cliente` |
| `Ativo` | Ver regras abaixo |
| `UltimoAcessoEm` | Atualizado no login |
| `VersaoDeSessao` | Identificador da geração de sessões do usuário; ver "Sessões" |
| `FalhasDeLogin`, `BloqueadoAte` | Controle do bloqueio de login; ver "Bloqueio de login" |

Regras:

- Usuário inativo não faz login e não pode ser incluído como responsável, revisor ou participante. Continua visível no histórico.
- O sistema sempre mantém ao menos um usuário ativo com perfil de tipo `Diretoria`. Inativar, excluir ou trocar o perfil do último é recusado.
- Não há cadastro público. Somente a Diretoria cria, edita, inativa usuários e troca perfis.
- Antes de inativar um usuário, a tela mostra à Diretoria o que ficará parado com ele: partes de atividade pendentes, atividades não concluídas em que ele é o revisor e eventos ainda não terminados em que participa. É um aviso, não um bloqueio: a inativação segue com uma segunda confirmação.
- O nome dos usuários ativos da equipe (perfis `Diretoria` e `Producao`) é visível a qualquer usuário interno autenticado, para a escolha de participantes de evento. Os demais dados de usuário são da Diretoria.
- Usuário de cliente pertence a exatamente um cliente (`ClienteId`); um cliente pode ter vários usuários. Só a Diretoria cria e edita usuário de cliente. O cliente precisa existir e estar ativo na criação e na troca; manter o mesmo cliente na edição não o revalida.
- O usuário não troca entre o tipo `Cliente` e os tipos internos. Perfil de cliente sem cliente, ou perfil interno com cliente, é recusado pelo domínio.
- A troca do cliente de um usuário renova a `VersaoDeSessao` e gera o evento `usuario_cliente_alterado`, com o cliente anterior e o novo, na mesma transação.
- Usuário de cliente não é responsável de atividade (o revisor já é sempre da Diretoria), não participa de evento e não entra em lista de acesso de documento: o domínio recusa, e as listas de escolha não o trazem.
- A ficha do cliente mostra à Diretoria, só para leitura, os usuários vinculados a ele. A consulta registra `Listou` no recurso `Usuario`.

### Autenticação e autorização

- Autenticação por cookie.
- Toda página exige usuário autenticado por padrão; o acesso anônimo é exceção declarada (tela de login).
- A autorização tem duas fontes:
  1. o `Tipo` do perfil do usuário;
  2. a relação do usuário com o recurso (é responsável da atividade, está na lista de acesso do documento, participa do evento).
- As regras são verificadas nos casos de uso da `Application`. A interface apenas oculta o que o usuário não pode fazer.
- O perfil e o estado ativo do usuário são lidos do banco a cada operação, não de dados guardados no cookie, para que uma mudança de perfil ou inativação valha imediatamente.

Perfil Cliente e portão:

- O executor de requisições tem um portão: um ator de perfil `Cliente` só executa requisições marcadas como abertas ao cliente. As demais respondem `AcessoNegado` sem chegar ao caso de uso, e a tentativa gera `acesso_negado` com o nome da requisição. Um caso de uso novo nasce fechado ao cliente, e um tipo de perfil que não seja interno (hoje, só o Cliente) também nasce fechado.
- Estão abertas sete requisições: `AutenticarUsuario`, `ObterMeuAcesso`, `AlterarMinhaSenha`, `BaixarDocumento`, `ListarMeusDocumentos`, `ListarDocumentosComuns` e `ListarMeuAndamento`. A lista é fixada em teste (seção 12).
- As consultas do portal não recebem Id de cliente: usam o `ClienteId` do ator, lido do banco. O ator de perfil `Cliente` sem cliente vinculado recebe `AcessoNegado`.
- `BaixarDocumento` está aberta, mas quem decide é a regra de acesso do documento (seção 7). O filtro das listagens internas não conhece o tipo do perfil e não é usado para o cliente.
- Usuário de cliente só tem acesso com o cliente existente e ativo: o login é recusado, o cookie é rejeitado na requisição HTTP seguinte e o contexto de acesso nega o acesso, como a quem não está autenticado. Reativar o cliente devolve o acesso sem mexer no usuário: o cookie rejeitado não revive, e o acesso volta no próximo login.
- O par perfil–cliente incoerente (perfil `Cliente` sem cliente, ou perfil interno com cliente; só possível por edição direta no banco) não tem acesso.
- O login recusado por causa do cliente responde como senha errada: mensagem de credenciais inválidas e evento `login_falha`; na tentativa que aciona o bloqueio, mensagem de bloqueio e `login_bloqueado`. A tentativa conta para o bloqueio. O evento leva o `motivo`: `cliente_inativo` (cliente inativo ou inexistente) ou `vinculo_incoerente` (par incoerente).
- O cookie de um usuário de perfil `Cliente` leva uma marca de portal. Ela não autoriza nada: perfil, estado ativo e cliente continuam lidos do banco a cada operação, e quem nega é o portão. Serve só para o layout saber, sem consulta, que não deve montar páginas internas.
- Para o usuário de cliente, as páginas internas mostram "Página não encontrada" e não são montadas; a resposta continua com status 200, para a rota não revelar mais do que já revela. O cliente abre o início (saudação, os totais do andamento e os atalhos), Andamento, Meus documentos, Documentos comuns e a troca de senha.
- A guarda de rotas vale nos dois sentidos e compara o endereço já decodificado. Além da página interna aberta pelo cliente, a página só do portal (Andamento e Meus documentos) aberta pela equipe mostra "Página não encontrada" e não é montada. Documentos comuns é compartilhada com a Diretoria: para a Produção, a própria página mostra "Página não encontrada".

### Sessões

- Cada usuário tem uma `VersaoDeSessao`. O cookie de autenticação carrega a versão vigente no momento do login.
- A versão é renovada quando a senha do usuário é alterada ou redefinida e quando o usuário é inativado. Também é renovada quando um usuário de cliente passa a outro cliente.
- Uma sessão cuja versão difere da vigente é recusada na operação seguinte, tanto em requisições novas quanto em telas já abertas. Reativar um usuário não revive sessões antigas.
- Quem altera a própria senha é levado de volta à tela de login.

### Bloqueio de login

- Após 5 tentativas seguidas com senha errada, o login do usuário fica bloqueado por 15 minutos. Durante o bloqueio nenhuma senha é aceita.
- Um login bem-sucedido zera a contagem de falhas.
- A redefinição de senha pela Diretoria remove o bloqueio.
- Tentativas com e-mail inexistente ou de usuário inativo recebem a mesma mensagem de credenciais inválidas e não geram bloqueio.
- O usuário de cliente cujo cliente está inativo (ou cujo vínculo é incoerente) segue a regra de "Perfil Cliente e portão": a tentativa é recusada e conta para o bloqueio mesmo com a senha certa.

### Matriz de acesso

| Recurso | Diretoria | Produção | Cliente |
|---|---|---|---|
| Usuários e perfis | Tudo | Sem acesso | Sem acesso |
| Áreas de atuação | Tudo | Lê os nomes ao consultar cadastros; não gerencia | Sem acesso |
| Clientes e processos | Tudo | Somente leitura | Sem acesso |
| Documentos Gerais | Tudo | Leitura conforme lista de acesso | Sem acesso |
| Contratos de Honorários | Tudo | Sem acesso | Sem acesso |
| Documentos do cliente | Tudo | Sem acesso | Leitura dos do próprio cliente |
| Documentos comuns | Tudo | Sem acesso | Leitura |
| Atividades | Tudo | Somente as em que é responsável | Andamento das do próprio cliente |
| Agenda | Todos os eventos | Eventos em que participa ou que criou; cria os seus | Sem acesso |
| Auditoria | Consulta | Sem acesso | Sem acesso |
| Produtividade | Consulta, por produtor e por mês | Sem acesso | Sem acesso |

## 6. Cadastros

### AreaAtuacao

`Nome` (obrigatório, único) e `Ativo`. Representa o ramo do direito (Trabalhista, Cível, Tributário etc.). Área inativa não pode ser usada em registros novos.

### Cliente

Classe abstrata com duas derivadas, persistidas em uma tabela com discriminador.

| Classe | Campos |
|---|---|
| `Cliente` (base) | `Email`, `Telefone`, `Endereco` (value object), `Observacoes`, `Ativo` |
| `ClientePessoaFisica` | `NomeCompleto` (obrigatório), `Cpf` (value object, obrigatório, único), `Rg`, `DataNascimento` |
| `ClientePessoaJuridica` | `RazaoSocial` (obrigatório), `NomeFantasia`, `Cnpj` (value object, obrigatório, único), `NomeResponsavel`, `InscricaoEstadual`, `InscricaoMunicipal` |

- `Cpf` e `Cnpj` validam dígitos verificadores. O CNPJ aceita o formato alfanumérico (letras maiúsculas nas doze primeiras posições).
- O nome de exibição fica em um campo só, `Nome`: é o nome completo da pessoa física e a razão social da pessoa jurídica.
- O endereço é um objeto de valor sempre presente, que pode estar vazio.
- A busca por nome não diferencia maiúsculas nem acentos; um texto que seja um CPF ou CNPJ válido procura pelo documento exato.
- O tipo do cliente (PF ou PJ) não muda depois de criado.
- As áreas "Documentos Gerais", "Contratos de Honorários", "Documentos de Atividades" e "Documentos do cliente (visíveis ao cliente)" do cliente são consultas ao contexto Documentos filtradas por cliente e categoria; não são coleções do agregado `Cliente`.
- A quarta área, "Documentos do cliente (visíveis ao cliente)", é só da Diretoria: tem envio, renomear e excluir, e não tem editor de acesso. Não aparece para a Produção.
- Inativar o cliente tira o acesso dos usuários dele ao portal. Depois de a inativação dar certo, a tela conta os usuários **ativos** do cliente e avisa: "Cliente inativado.", "Cliente inativado. 1 usuário do portal ficou sem acesso." ou "Cliente inativado. N usuários do portal ficaram sem acesso." Se a contagem não puder ser consultada, o aviso é só "Cliente inativado.".

### Processo

| Campo | Regra |
|---|---|
| `NumeroCnj` | Value object; opcional; único quando informado |
| `Titulo` | Obrigatório |
| `AreaAtuacaoId` | Obrigatório |
| `Status` | Enum: `Ativo`, `Suspenso`, `Arquivado`, `Encerrado` |
| `Orgao`, `Comarca`, `ParteContraria` | Opcionais |
| `DataDistribuicao`, `ValorCausa`, `Observacoes` | Opcionais |

`ProcessoCliente` (dentro do agregado): `ClienteId` e `Polo` (enum `Ativo`, `Passivo`, `Terceiro`).

Regras:

- Um processo tem ao menos um cliente; um cliente não se repete no mesmo processo.
- Não é possível remover do processo um cliente que tenha atividade vinculada a esse processo. Conta a atividade viva; atividade excluída não impede.
- Alterar a área do processo altera a área de todas as atividades vinculadas a ele, inclusive as concluídas, na mesma transação, com um registro de alteração por atividade.
- Área ou cliente inativo não entra em vínculo novo, mas permanece no processo que já o tinha.
- `ValorCausa`, quando informado, não é negativo, tem no máximo duas casas decimais e cabe em `numeric(18,2)`.
- Cada vínculo com cliente é um registro com carimbos e exclusão lógica próprios; retirar um cliente exclui logicamente o vínculo, e o mesmo cliente pode voltar ao processo depois. O banco não apaga vínculos em cascata.
- Toda mudança nos vínculos carimba a última alteração do processo e gera o evento `processo_clientes_alterados` (ver seção 10).
- Edições simultâneas do mesmo processo: a segunda gravação é recusada como conflito ("O processo foi alterado por outra pessoa. Recarregue e tente novamente."). O controle cobre gravações concorrentes; um formulário aberto há mais tempo ainda grava por cima do estado atual.
- Abrir um processo mostra o nome e o documento (CPF/CNPJ) de cada cliente; a consulta fica registrada como visualização do processo.

## 7. Documentos

### Documento

| Campo | Regra |
|---|---|
| `Nome` | Obrigatório; padrão é o nome do arquivo enviado; editável |
| `NomeArquivoOriginal`, `TipoConteudo`, `TamanhoBytes` | Preenchidos no envio; imutáveis |
| `HashSha256` | Calculado no envio; imutável |
| `ChaveArmazenamento` | Localização do arquivo no armazenamento; imutável |
| `Categoria` | Enum: `DocumentoGeral`, `ContratoHonorarios`, `DocumentoAtividade`, `DocumentoDoCliente`, `DocumentoComum`; imutável |
| `ClienteId` | Obrigatório em `DocumentoGeral`, `ContratoHonorarios` e `DocumentoDoCliente`; nulo em `DocumentoComum`. Em `DocumentoAtividade` é sempre o cliente da atividade (nulo se ela for avulsa) |
| `AtividadeId` | Obrigatório em `DocumentoAtividade` e só nele |

`DocumentoDoCliente` e `DocumentoComum` são as categorias postas para o cliente no portal. O banco confere o `ClienteId` conforme a categoria: obrigatório em `DocumentoGeral`, `ContratoHonorarios` e `DocumentoDoCliente`, nulo em `DocumentoComum`, livre em `DocumentoAtividade`.

Tabelas auxiliares, dentro do agregado:

- `DocumentoPerfilAcesso`: `DocumentoId`, `PerfilId`.
- `DocumentoUsuarioAcesso`: `DocumentoId`, `UsuarioId`.

### Armazenamento

O arquivo fica fora do banco de dados, atrás de uma interface de armazenamento definida na `Application`. A primeira implementação grava em disco; a troca por armazenamento de objetos não altera o domínio. O banco guarda somente metadados e hash.

Não há versionamento. Substituir um documento é enviar um novo e excluir logicamente o anterior.

- A chave de armazenamento é gerada pelo servidor; o nome do arquivo enviado é apenas um metadado (só o último segmento do caminho é guardado) e nunca é usado como caminho.
- O envio grava primeiro o arquivo e depois o registro; se o registro não for gravado, o arquivo é removido.
- Limite de 50 MB por arquivo. Arquivo vazio é recusado.
- A exclusão de um documento é lógica e o arquivo permanece no armazenamento.
- O download é sempre entregue como anexo, com tipo de conteúdo neutro: o tipo informado no envio é só um metadado.
- Fora do ambiente de desenvolvimento, a pasta de armazenamento é configuração obrigatória, com caminho absoluto, e precisa existir antes de a aplicação subir; a aplicação não a cria.
- Um documento cujo arquivo não está no armazenamento é uma falha do servidor, registrada pelo evento `documento_arquivo_ausente`; não é respondido como "não encontrado" a quem tem acesso.
- O hash é guardado para conferência futura; nesta versão ele não é verificado no download.

### Regra de acesso

Uma única função de domínio decide se um usuário pode acessar um documento. Além do usuário e do perfil, ela recebe o cliente do usuário (nulo para quem não é do tipo `Cliente`) e decide nesta ordem:

1. Usuário com perfil de tipo `Diretoria` acessa sempre.
2. Perfil de tipo `Cliente` acessa o `DocumentoComum` e o `DocumentoDoCliente` cujo `ClienteId` é o do seu cliente. Não acessa nenhuma outra categoria, ainda que o usuário ou o perfil conste em lista. Sem cliente vinculado, não acessa o documento do cliente, mas a regra, sozinha, ainda dá a ele a leitura do documento comum. Quem nega antes é o contexto de acesso, que não reconhece o usuário de perfil `Cliente` sem cliente (ou com cliente inativo): por isso, na prática, ele não chega a nenhuma consulta.
3. Um tipo de perfil que não seja `Producao` não acessa mais nada.
4. Documento do cliente e documento comum nunca são acessados por perfil de tipo `Producao`.
5. Contrato de Honorários nunca é acessado por quem não é da Diretoria, mesmo que o usuário ou o seu perfil constem na lista de acesso (caso de quem era da Diretoria quando foi incluído e depois mudou de perfil).
6. Documento de atividade: acessa quem é responsável atual da atividade, enquanto ela não estiver excluída. As listas de acesso não contam para essa categoria, e não são editáveis.
7. Nos demais casos (documento geral), acessa se o seu `PerfilId` constar em `DocumentoPerfilAcesso` ou se ele constar em `DocumentoUsuarioAcesso`.

Listagens exibem apenas documentos que o usuário pode acessar, e o total informado conta só esses. As listagens internas, filtradas por perfil e por usuário, nunca trazem contrato, documento do cliente nem documento comum para a Produção; o cliente tem consultas próprias (ver "Consultas do portal").

- Baixar um documento sem ter acesso responde como não encontrado (seção 11) e gera o evento de segurança de acesso negado, com o documento. Vale também para o cliente que pede o documento de outro cliente ou de categoria interna.
- Pedir a lista de Contratos de Honorários ou de Documentos do cliente sem ser da Diretoria responde acesso negado: a existência da área não é segredo, o conteúdo dela é. Pedir a lista de documentos de um cliente com a categoria `DocumentoComum` é recusado como validação ("Categoria inválida para esta consulta."): o documento comum não é de cliente.
- O domínio recusa perfil de tipo `Cliente` e usuário de perfil `Cliente` em qualquer lista de acesso ("Usuário ou perfil de cliente não entra em lista de acesso de documento.").
- O documento do cliente e o documento comum não têm lista de acesso: definir acesso neles é recusado pelo domínio ("Documento visível ao cliente não tem lista de acesso.").
- Cada entrada das listas de acesso é um registro com carimbos e exclusão lógica próprios. Perfil ou usuário inativo não entra em lista nova, mas permanece na lista em que já estava, e pode ser retirado dela.
- Em documento geral, contrato de honorários, documento do cliente e documento comum, enviar, renomear e excluir são da Diretoria; alterar a lista de acesso também, nas duas primeiras categorias. Um cliente inativo continua recebendo documentos.

### Comportamento por categoria

| Categoria | Lista de acesso inicial | Restrição | Quem envia e gerencia |
|---|---|---|---|
| `DocumentoGeral` | Todos os perfis internos ativos (`Diretoria` e `Producao`) | Nenhuma | Diretoria |
| `ContratoHonorarios` | Vazia | O domínio só aceita na lista perfil de tipo `Diretoria` e usuário de perfil `Diretoria`, sem exceção | Diretoria |
| `DocumentoAtividade` | Não há lista gravada: o acesso é o dos responsáveis atuais da atividade | A lista não é editável | Quem vê a atividade (Diretoria e responsáveis) envia; a Diretoria renomeia e exclui |
| `DocumentoDoCliente` | Não há | Cliente obrigatório; sem lista de acesso; lido pela Diretoria e pelos usuários daquele cliente | Diretoria |
| `DocumentoComum` | Não há | Sem cliente e sem lista de acesso; lido pela Diretoria e por todo usuário de perfil `Cliente` | Diretoria |

- O perfil de tipo `Cliente` não é incluído em Documento Geral nenhum, nem ao ser criado nem depois. A gravação em lote que inclui um perfil novo nos Documentos Gerais confere o tipo do perfil e não grava nada para o `Cliente`.
- Um perfil interno criado depois é incluído automaticamente na lista de acesso de todos os Documentos Gerais existentes, na mesma transação da criação. Essas entradas são gravadas de uma vez, sem um registro de auditoria por entrada: o evento `perfil_incluido_nos_documentos_gerais` guarda o perfil e a quantidade de documentos, e cada entrada leva quem e quando a criou. O Id dessas entradas é gerado pelo banco (exceção à regra do Guid versão 7 da seção 4).
- Reativar um perfil não o inclui nos Documentos Gerais enviados enquanto ele esteve inativo.
- Um documento enviado no mesmo instante em que um perfil é criado pode nascer sem o perfil novo; a Diretoria o inclui pelo editor de acesso.
- Um usuário incluído como responsável de uma atividade recebe acesso aos documentos já anexados a ela. Removê-lo da atividade retira esse acesso. Como o acesso é calculado a cada consulta, isso vale na operação seguinte, sem cópia a manter.

### Documentos de atividade

- Anexa arquivos a uma atividade quem a vê: a Diretoria (onde estão o revisor e o autor) e os responsáveis. A regra efetiva para Produção é ser responsável.
- Todo documento de atividade fica vinculado também ao cliente da atividade: o `ClienteId` do documento é preenchido com o da atividade no envio. Em atividade avulsa ele fica nulo.
- Se o cliente da atividade for alterado, o `ClienteId` dos documentos dela acompanha a alteração. Toda edição da atividade realinha os documentos que estiverem com outro cliente.
- Excluir uma atividade não exclui os seus documentos: eles continuam na ficha do cliente para a Diretoria e deixam de aparecer para a Produção.
- Na ficha do cliente esses documentos aparecem em uma área própria, "Documentos de Atividades", ao lado de "Documentos Gerais" e "Contratos de Honorários", com a indicação da atividade de origem.
- O vínculo com o cliente não amplia o acesso: a área lista apenas os documentos que o usuário pode acessar pela regra acima. Um usuário de Produção vê ali somente os documentos das atividades em que é responsável.

### Documentos do portal

Envio:

- O documento do cliente é enviado pelo mesmo caso de uso do documento geral e do contrato, que aceita também essa categoria. O cliente precisa existir; cliente inativo continua recebendo documentos. Uma categoria fora das três que esse caso de uso aceita responde "Categoria inválida para envio.".
- O documento comum tem caso de uso próprio, sem cliente. A requisição leva a confirmação de que o documento ficará visível a todos os clientes; sem ela, o envio é recusado como validação ("Confirme que o documento ficará visível para todos os clientes."), antes de qualquer verificação do arquivo. Só a Diretoria envia, e essa verificação vem primeiro.
- A tela de Documentos comuns exige a mesma confirmação: ao pedir o envio, mostra o aviso "Documentos desta área ficam visíveis para todos os clientes do escritório. Não envie aqui arquivos de um cliente específico." com "Confirmar envio" e "Cancelar", e só a confirmação envia a requisição. O aviso recebe o foco ao abrir e só abre para um envio que pode seguir: o arquivo acima do limite de tamanho é recusado antes do aviso. Com o aviso aberto, o botão de enviar fica desabilitado. Cancelar mantém o arquivo e o nome escolhidos e não envia nada; trocar o arquivo fecha o aviso. O aviso vale para um envio só: depois de um envio, com sucesso ou com falha, o próximo avisa de novo.
- Renomear e excluir usam os casos de uso que já existem, que operam por Id e são da Diretoria.

Consultas do portal, todas paginadas (20 por página), do documento mais recente para o mais antigo:

| Consulta | Quem | O que traz |
|---|---|---|
| `ListarMeusDocumentos` | Perfil `Cliente` com cliente vinculado; os demais recebem `AcessoNegado` | Os `DocumentoDoCliente` do cliente do ator, sem o cliente como parâmetro |
| `ListarDocumentosComuns` | Diretoria e perfil `Cliente`; a Produção recebe `AcessoNegado` | Todos os `DocumentoComum` |
| `ListarDocumentosDoCliente`, categoria `DocumentoDoCliente` | Diretoria; a Produção recebe `AcessoNegado` | Os documentos do cliente pedido, na ficha dele |

- O cliente recebe de cada documento o Id, o nome, o tamanho e a data de envio. Não recebe o nome do arquivo enviado, a categoria nem quem enviou. A Diretoria recebe também o nome de quem enviou, que continua identificado depois de excluído o usuário.
- O Id serve ao download. O tamanho é mostrado em português do Brasil (por exemplo, "1,5 MB"), qualquer que seja a cultura do servidor.
- As telas de documentos (a ficha do cliente e as do portal) voltam à última página quando a página pedida deixou de existir, por exemplo porque documentos foram excluídos enquanto se navegava.
- As duas categorias nunca aparecem nas listagens da Produção.

## 8. Atividades

### Atividade (raiz do agregado)

| Campo | Regra |
|---|---|
| `Titulo` | Obrigatório |
| `Descricao` | Obrigatório |
| `DataEntrega` | Obrigatória a partir do status `Direcionada` |
| `Complexidade` | Obrigatória; inteiro de 1 a 3 |
| `Status` | Enum `StatusAtividade` (ver fluxo) |
| `ConcluidaEm` | Preenchido ao entrar em `Concluida`; limpo ao reabrir |
| `ClienteId` | Opcional |
| `ProcessoId` | Opcional |
| `AreaAtuacaoId` | Com processo: igual à do processo, não editável. Sem processo: opcional, informada |
| `TipoAutor` | Enum: `Usuario` ou `Sistema` |
| `AutorId` | Usuário de Diretoria quando `TipoAutor = Usuario`; nulo quando `Sistema` |
| `RevisorId` | Usuário de Diretoria; obrigatório a partir de `Direcionada` |

Regras de vínculo:

- Com processo, o cliente é obrigatório e deve ser um dos clientes do processo.
- Atividade avulsa é a que não tem cliente nem processo.
- Uma atividade pode ter cliente sem processo.

Regras da complexidade:

- É informada junto com título, descrição e data de entrega, na criação e na edição, pela Diretoria.
- Um valor fora de 1 a 3 é recusado pelo domínio ("A complexidade deve ser 1, 2 ou 3.").
- Pode ser alterada em qualquer status, inclusive em atividade concluída. A alteração fica no registro de alterações da atividade, sem evento próprio.
- As atividades que existiam antes do campo receberam complexidade 1.

### Entidades internas

| Entidade | Campos | Regra |
|---|---|---|
| `AtividadeResponsavel` | `UsuarioId`, `Direcionamento`, `ConcluidoEm` | Um usuário não se repete na mesma atividade. `ConcluidoEm` nulo significa parte pendente |
| `AtividadeComentario` | `Texto`, `UsuarioId`, `DataHora` | Imutável depois de criado. É gravado à parte da atividade: comentar não altera a atividade nem a versão dela. Vale em qualquer status. Com a atividade excluída, os comentários ficam inacessíveis |
| `AtividadeHistoricoStatus` | `Ordem`, `StatusAnterior`, `StatusNovo`, `UsuarioId`, `DataHora`, `Motivo` | Somente inserção. `Ordem` numera as transições dentro da atividade. `UsuarioId` nulo indica transição feita pelo sistema |

### Fluxo de status

| De | Para | Gatilho | Quem |
|---|---|---|---|
| — | `Criada` | Criação sem responsável | Diretoria ou sistema |
| `Criada` | `Direcionada` | Definição de ao menos um responsável, do revisor e da data de entrega | Diretoria |
| `Direcionada` | `Feita` | Último responsável conclui sua parte | Automático |
| `Feita`, `Corrigida` | `Concluida` | Aprovação | Revisor |
| `Feita`, `Corrigida` | `ParaCorrecao` | Devolução com motivo e responsáveis selecionados | Revisor |
| `ParaCorrecao` | `Corrigida` | Último responsável selecionado conclui novamente | Automático |
| `Concluida` | `Reaberta` | Reabertura com motivo e responsáveis selecionados | Diretoria |
| `Reaberta` | `Feita` | Último responsável selecionado conclui | Automático |

```mermaid
stateDiagram-v2
    [*] --> Criada
    Criada --> Direcionada
    Direcionada --> Feita
    Feita --> Concluida
    Feita --> ParaCorrecao
    ParaCorrecao --> Corrigida
    Corrigida --> Concluida
    Corrigida --> ParaCorrecao
    Concluida --> Reaberta
    Reaberta --> Feita
```

Regras do fluxo:

- Qualquer transição fora da tabela é recusada pelo domínio.
- Toda transição insere uma linha em `AtividadeHistoricoStatus`. Criar a atividade já com responsáveis insere duas: `Criada` e `Direcionada`.
- Na devolução e na reabertura o motivo é obrigatório e é gravado no histórico. A seleção de responsáveis vem com todos marcados por padrão e exige ao menos um; os selecionados têm `ConcluidoEm` limpo, os demais permanecem concluídos.
- Um responsável só conclui sua parte nos status `Direcionada`, `ParaCorrecao` e `Reaberta`, e só se a parte estiver pendente.
- "Atrasada" não é status. É uma condição calculada: `DataEntrega` anterior à data atual e status diferente de `Concluida`.
- Não existe status de cancelamento. Atividade criada por engano é excluída logicamente pela Diretoria.
- Uma transição marcada como "Automático" na tabela é registrada no histórico com o usuário cuja ação a provocou (quem concluiu a última parte). `UsuarioId` nulo fica reservado a transições feitas pelo próprio sistema.
- Depois de direcionada, a atividade não fica sem responsável: a lista não pode ser esvaziada.
- Se a Diretoria remove o último responsável pendente e todos os que ficaram já concluíram, a atividade avança como se o último tivesse concluído (`Feita` ou `Corrigida`), e a linha do histórico leva o motivo "Responsável pendente removido."
- Usuário inativo não entra como responsável, mas quem já era responsável permanece até ser retirado.
- Usuário de cliente não entra como responsável ("Usuário de cliente não pode ser responsável por atividade.").
- Edições simultâneas: uma gravação feita sobre uma versão que já mudou é recusada como conflito ("A atividade foi alterada por outra pessoa. Recarregue e tente novamente."). Editar, definir responsáveis, trocar o revisor e excluir enviam a versão que o usuário estava vendo.

### Regra do revisor

- Quando há exatamente um responsável ativo com perfil de tipo `Diretoria`, `RevisorId` é esse usuário e o domínio recusa alterá-lo. Um responsável de Diretoria que foi inativado ou mudou para um perfil de Produção não conta.
- Quando não há nenhum, ou há dois ou mais, a Diretoria escolhe o revisor entre os usuários ativos de tipo `Diretoria`.
- A regra é reavaliada a cada inclusão ou remoção de responsável. Se a reavaliação liberar a escolha, o revisor atual é mantido até que a Diretoria o altere.
- Quando o revisor é o único responsável da atividade, concluir a parte leva a atividade diretamente a `Concluida`. O histórico registra as duas transições (`Feita` e `Concluida`). O mesmo vale quando é o próprio revisor quem remove o último pendente e fica como único responsável; se outro diretor faz a remoção, a atividade para em `Feita`/`Corrigida` e o revisor aprova.
- Só aprova e devolve o revisor que continua sendo um usuário ativo de Diretoria; o atalho acima também exige isso. Se o revisor deixou de ser, a Diretoria troca o revisor.

### Permissões

| Ação | Quem |
|---|---|
| Criar, editar, excluir | Diretoria |
| Incluir e remover responsáveis, editar direcionamentos | Diretoria, nos status `Criada`, `Direcionada`, `ParaCorrecao` e `Reaberta` |
| Trocar revisor | Diretoria, em qualquer status exceto `Concluida` (inclui `Feita` e `Corrigida`, para a atividade não ficar presa quando o revisor deixa o escritório) |
| Aprovar e devolver | O revisor da atividade |
| Reabrir | Diretoria |
| Concluir a própria parte | O responsável |
| Comentar | Diretoria e responsáveis |
| Ver a atividade | Diretoria e responsáveis |
| Ver um direcionamento | Diretoria e o responsável a quem ele se destina |

Um responsável de Produção vê os nomes dos demais responsáveis, mas não os direcionamentos deles. Quem vê a atividade vê também o histórico, com os motivos de devolução e de reabertura.

- Quem não pode ver uma atividade recebe a mesma resposta de uma atividade inexistente (seção 11), e a tentativa gera o evento de acesso negado, com a atividade.
- A listagem de quem não é da Diretoria traz só as atividades em que ele é responsável, qualquer que seja o filtro pedido. A Diretoria filtra também por responsável e por revisor, inclusive inativos.
- "Atrasada" usa a data do escritório (fuso configurado), não a data UTC.

### Produtividade

A Diretoria acompanha, por produtor e por mês, as atividades realizadas, separadas por complexidade.

- Produtor é o usuário cujo perfil atual é de tipo `Producao`, ativo ou inativo. Um usuário de Diretoria não é produtor, mesmo sendo responsável em atividades. Vale o perfil atual: quem muda de tipo entra ou sai da lista.
- Uma atividade é realizada por um produtor em um período quando, no momento da consulta, está em `Concluida`, não foi excluída, `ConcluidaEm` cai no período e o produtor é um dos responsáveis.
- Uma atividade com vários responsáveis conta uma vez para cada um.
- Os números são calculados a cada consulta, sobre o estado atual. Uma atividade reaberta deixa de contar e volta a contar no mês da nova conclusão; vale a complexidade atual. Por isso os números de um mês passado mudam quando uma atividade dele é reaberta, excluída ou tem a complexidade alterada.
- A consulta recebe o produtor e um período em UTC de no máximo 92 dias; a tela pede um mês por vez, no fuso do escritório. Ela devolve um grupo por complexidade, sempre os três, cada um com as atividades da conclusão mais antiga para a mais recente, e a data da primeira conclusão do produtor.
- Quem não é da Diretoria recebe `AcessoNegado`. Um identificador que não é de um produtor responde `NaoEncontrado`, sem evento de acesso negado: a Diretoria pode ver qualquer produtor.
- A tela lista os produtores e abre a produtividade em um painel na própria página, com o seletor de mês indo do mês corrente até o da primeira conclusão do produtor.

### Andamento

O cliente acompanha de forma macro as atividades ligadas a ele. `ListarMeuAndamento` é a única visão de atividades do perfil `Cliente`; não há ficha da atividade para ele.

- Só o ator de perfil `Cliente` com cliente vinculado a executa; os demais recebem `AcessoNegado`. A consulta não recebe Id de cliente: usa o do ator.
- Entram as atividades vivas cujo `ClienteId` é o do ator, em qualquer status, desde a criação. Atividade avulsa e atividade excluída não aparecem.
- Cada linha tem cinco campos e nenhum identificador: título; nome da área de atuação; processo (o número CNJ formatado; na falta dele, o título do processo; vazio sem processo); situação; data de conclusão. Área e processo excluídos logicamente continuam identificados.
- A situação tem duas faces: `Concluída` quando o status é `Concluida`; `Em andamento` em todos os demais (`Criada`, `Direcionada`, `Feita`, `ParaCorrecao`, `Corrigida`, `Reaberta`). A data de conclusão só vai com `Concluída`: uma atividade reaberta volta a `Em andamento` e perde a data.
- A resposta traz os dois totais do cliente, em andamento e concluídas. Eles contam todas as atividades do cliente e não dependem do filtro de situação.
- Filtro opcional por situação; um valor fora das duas situações é recusado como validação ("Situação inválida."). Paginada, 20 por página.
- Ordem: as em andamento primeiro, da criação mais recente para a mais antiga; depois as concluídas, da conclusão mais recente para a mais antiga. O desempate final é pelo identificador, em ordem decrescente, para a paginação não repetir nem pular linhas.
- A tela volta à última página quando a página pedida deixou de existir, por exemplo porque atividades mudaram enquanto o cliente navegava. O início do cliente usa a mesma consulta, só para os totais.

Não chegam ao cliente: descrição, data de entrega, condição de atrasada, responsáveis, revisor, direcionamentos, comentários, histórico de status, motivos de devolução e de reabertura e documentos da atividade.

Consequência: o título de toda atividade com cliente é lido por esse cliente no portal. O formulário de atividade mostra, quando há cliente selecionado, o lembrete "O título aparece para o cliente no portal.". Não há opção de ocultar uma atividade do cliente.

## 9. Agenda

### Evento

| Campo | Regra |
|---|---|
| `Titulo` | Obrigatório |
| `Descricao`, `Local` | Opcionais |
| `Tipo` | Enum: `Audiencia`, `Reuniao`, `Prazo`, `Outro` |
| `InicioEm` | Obrigatório, entre os anos 2000 e 2100 |
| `DuracaoEstimadaMinutos` | Obrigatório, de 1 minuto a 7 dias |
| `ClienteId`, `ProcessoId`, `AtividadeId` | Opcionais e independentes |

`EventoParticipante` (dentro do agregado): `UsuarioId`.

Regras:

- Um evento tem ao menos um participante; um usuário não se repete no mesmo evento.
- Com cliente e processo informados, o cliente deve ser um dos clientes do processo.
- Qualquer usuário da equipe cria eventos e é incluído como participante na criação. Depois, pode ser retirado da lista como qualquer outro.
- Qualquer usuário da equipe inclui outros usuários ativos da equipe como participantes. Usuário inativo não entra em evento novo, mas permanece no evento em que já estava.
- Usuário de cliente não participa de evento ("Usuário de cliente não pode participar de evento.") e não aparece na escolha de participantes.
- Produção vê os eventos em que participa e os que criou. Diretoria vê todos. Para quem não pode ver, o evento responde como inexistente, e a tentativa fica registrada.
- Editam e excluem o evento quem o criou e a Diretoria. Edição e exclusão enviam a versão vista na tela; se o evento mudou nesse intervalo, a operação é recusada com aviso.
- O vínculo (cliente, processo, atividade) só é validado quando muda. Um evento cujo cliente saiu do processo ou foi inativado continua editável com o vínculo que tinha.
- Uma atividade só é vinculada a um evento por quem pode vê-la. Na ficha do evento, o título da atividade aparece só para quem pode vê-la.
- Sobreposição de horário entre eventos de um mesmo participante gera aviso e não impede a gravação. O aviso traz, por participante, o trecho ocupado dentro do intervalo do evento salvo, sem o título nem os demais participantes do outro evento.
- A listagem é por período de até 92 dias. A tela mostra uma semana por vez, de segunda a domingo, no fuso do escritório. A Diretoria filtra por participante, inclusive inativos.

## 10. Auditoria

A trilha é completa: alterações, eventos de segurança e consultas. São duas tabelas, porque o volume e o uso de cada uma são diferentes.

### RegistroAuditoria

Registra criação, alteração e exclusão de dados e eventos de segurança.

| Campo | Conteúdo |
|---|---|
| `DataHora` | Momento da operação |
| `UsuarioId` | Nulo quando a origem é o sistema |
| `Operacao` | Enum: `Criacao`, `Alteracao`, `Exclusao`, `Evento` |
| `Entidade`, `EntidadeId` | Tipo e identificador do registro afetado |
| `Evento` | Nome do evento de segurança, quando `Operacao = Evento` |
| `Dados` | JSON: estado inicial na criação; diferença campo a campo (`de`, `para`) na alteração |
| `Ip`, `IdCorrelacao` | Origem e identificador da requisição |

- Gravado por um interceptor do EF Core, na mesma transação da alteração. Se a gravação do registro falhar, a operação inteira falha.
- Eventos de segurança: login, falha de login, bloqueio de login, logout, troca e redefinição de senha, mudança de perfil, ativação e inativação de usuário, mudança na lista de acesso de documento, tentativa de acesso negada.
- A mudança na lista de acesso de um documento gera o evento `documento_acesso_alterado` no documento, com os perfis e usuários incluídos e retirados, na mesma transação. A criação e a exclusão de um documento aparecem pelos registros do próprio documento e de cada entrada da lista, sem evento.
- `atividade_responsaveis_alterados` é anotado na atividade quando os seus responsáveis ou direcionamentos mudam, com os usuários incluídos, removidos e com direcionamento alterado, na mesma transação.
- `perfil_incluido_nos_documentos_gerais` registra, no perfil recém-criado, em quantos Documentos Gerais ele foi incluído. Criar, renomear, inativar e reativar um perfil aparecem pelos registros de alteração do próprio perfil, sem evento.
- `usuario_cliente_alterado` é anotado no usuário quando o seu cliente é trocado, com o cliente anterior e o novo, na mesma transação.
- A requisição barrada pelo portão do cliente gera `acesso_negado` com o nome da requisição (seção 5).
- O download negado gera `acesso_negado` no recurso `Documento`, com o documento, quando o documento existe e quem pediu não pode acessá-lo (inclusive o cliente que pede documento de outro cliente ou de categoria interna). Para um Id que não existe a resposta é não encontrado, sem evento.
- O login recusado por causa do cliente do usuário gera `login_falha` (ou `login_bloqueado`, na tentativa que aciona o bloqueio) com o `motivo` `cliente_inativo` ou `vinculo_incoerente`.
- `documento_arquivo_ausente` registra a tentativa de baixar um documento cujo arquivo não está no armazenamento.
- A tentativa de acesso negada, quando vem de uma consulta, leva o recurso ou os filtros pedidos.
- `evento_participantes_alterados` é anotado no evento da agenda quando os seus participantes mudam numa edição, com os usuários incluídos e removidos, na mesma transação.
- Eventos de negócio usam a mesma operação `Evento`: `processo_clientes_alterados`, anotado no processo quando seus clientes ou polos mudam, com os vínculos incluídos, removidos e alterados em `Dados`. Os clientes informados na criação do processo aparecem nos registros de criação dos vínculos, não nesse evento.
- Gravações que não são alteração de negócio (último acesso, contagem de falhas e bloqueio de login) não geram registro de alteração; os eventos de segurança correspondentes cobrem esses fatos.
- `SenhaHash` e `VersaoDeSessao` nunca aparecem em `Dados`.
- Em hierarquias, o registro leva o nome do tipo raiz (`Cliente` para pessoa física e jurídica). Os campos de objetos de valor entram com o nome composto (`Endereco.Cidade`).
- Os dados pessoais dos cadastros (documento, endereço, contato, observações) fazem parte do registro de cada alteração, e o texto digitado nas buscas faz parte do registro de consultas. Como as duas tabelas são somente-inserção, um dado corrigido ou excluído no cadastro permanece na trilha.

### RegistroAcesso

Registra consultas.

| Campo | Conteúdo |
|---|---|
| `DataHora`, `UsuarioId`, `Ip` | Quem e quando |
| `TipoRecurso`, `RecursoId` | O que foi consultado; `RecursoId` nulo em listagens |
| `Acao` | Enum: `Listou`, `Visualizou`, `Baixou` |
| `Detalhe` | Filtros usados na listagem |

- Gravado na `Application`, em cada caso de uso de consulta concluído com sucesso. Se o registro do acesso falhar, a consulta falha.
- A consulta interna que a interface faz para saber quem é o usuário atual não gera registro.
- Em documentos: listar os documentos de um cliente ou de uma atividade registra `Listou`; baixar registra `Baixou`; ver a lista de acesso de um documento registra `Visualizou`. As consultas do portal (`ListarMeusDocumentos` e `ListarDocumentosComuns`) registram `Listou` no recurso `Documento`, com a página em `Detalhe`; o download do cliente registra `Baixou`, como o da equipe.
- Em atividades: listar registra `Listou`; abrir uma atividade registra `Visualizou`; listar os comentários de uma atividade registra `Listou` (recurso `AtividadeComentario`). A consulta do portal (`ListarMeuAndamento`) registra `Listou` no recurso `Atividade`, com a situação e a página em `Detalhe`.
- Na agenda: listar eventos registra `Listou`; abrir um evento registra `Visualizou`.
- Na produtividade: consultar a produtividade de um produtor registra `Visualizou` (recurso `Produtividade`), com o produtor em `RecursoId` e o período em `Detalhe`. A lista de produtores fica registrada pela listagem de usuários, que a tela reutiliza.

### Regras comuns

- As duas tabelas são somente-inserção: não há edição, exclusão nem exclusão lógica.
- A consulta é exclusiva da Diretoria, com filtros por período, usuário, entidade ou recurso e operação. A consulta de alterações filtra também por registro (tudo o que aconteceu com um identificador) e por gravação (tudo o que foi gravado junto, pelo identificador de correlação).
- Consultar a auditoria gera um `RegistroAcesso`.
- Os registros são mantidos por tempo indeterminado. As duas tabelas têm índice por data; `RegistroAuditoria` tem também índice por registro e por correlação. O particionamento de `RegistroAcesso` por mês fica adiado até que o volume o justifique; a conversão é uma migration, sem impacto no domínio.
- `AtividadeHistoricoStatus` não é substituído pela auditoria: ele é dado de negócio, exibido na tela da atividade.

## 11. Tratamento de erros

- Violação de invariante lança exceção de domínio.
- Os casos de uso devolvem um `Resultado` com tipo de erro: `Validacao`, `NaoEncontrado`, `AcessoNegado`, `Conflito`.
- A interface traduz o tipo de erro em mensagem ao usuário. Erros não previstos são registrados em log técnico e exibidos como mensagem genérica.
- Um recurso que o usuário não pode ver responde como `NaoEncontrado`, para não revelar a existência dele. Exceção declarada: as listas de Contratos de Honorários e de Documentos do cliente respondem `AcessoNegado` a quem não é da Diretoria (seção 7).

## 12. Testes

- Unitários no `Domain`, sem banco: fluxo de status da atividade (todas as transições válidas e as recusadas), regra do revisor, conclusão por responsável, regra de acesso a documentos e restrição do Contrato de Honorários, invariantes de vínculo entre atividade, cliente e processo.
- De integração na `Infrastructure` e `Application`, com PostgreSQL real em contêiner: mapeamentos, filtro de exclusão lógica, interceptor de auditoria, autorização dos casos de uso.
- Portão do cliente: um teste percorre todas as requisições da aplicação e confere que cada uma não aberta ao cliente responde `AcessoNegado` a um ator de perfil `Cliente`, sem chamar o caso de uso, e que toda requisição não aberta responde com `Resultado`. Outro teste fixa a lista das abertas, exatamente estas sete: `AlterarMinhaSenha`, `AutenticarUsuario`, `BaixarDocumento`, `ListarDocumentosComuns`, `ListarMeuAndamento`, `ListarMeusDocumentos` e `ObterMeuAcesso`. Abrir uma requisição ao cliente exige alterar os dois.
- Teste de arquitetura da camada Web: nenhum arquivo da `Web` chega aos dados por repositório, contexto do banco, unidade de trabalho ou manipulador; só pelo despachante, que passa pelo executor. O portão só protege o que passa por ele. A única exceção declarada é a validação do cookie a cada requisição HTTP, que lê só o usuário e o cliente dele.
- Regra de acesso a documentos: a matriz completa dos três tipos de perfil pelas cinco categorias, incluindo o cliente diferente do dono do documento, o usuário de cliente que conste em lista e o cliente sem cliente vinculado.
- Guarda de rotas nos dois sentidos: o cliente em página interna e a equipe em página só do portal veem "Página não encontrada" sem que a página seja montada; Documentos comuns, aberta a cliente e Diretoria, é negada à Produção pela própria página.

## 13. Fora do escopo

Recursos de IA; notificações por e-mail ou mensagem; recorrência de eventos e de atividades; controle de horas e financeiro; permissões granulares por ação; versionamento de documentos; múltiplos escritórios; pontuação ponderada, metas e fechamento mensal de produtividade; complexidade configurável.

Do portal do cliente, ficam de fora, por decisão registrada em `2026-10-08-portal-do-cliente-design.md` (seção 12): envio de arquivos pelo cliente; aviso ao cliente por e-mail ou mensagem; cadastro do usuário pelo próprio cliente e recuperação de senha por e-mail; um usuário representando mais de um cliente; ocultar uma atividade do cliente; ficha, comentários ou anexos de atividade para o cliente; processos e agenda para o cliente; publicar um Documento Geral existente na área do cliente.
