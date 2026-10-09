# Complexidade de Atividades e Produtividade dos Produtores — Desenho

Data: 2026-10-08
Estado: implementada

## 1. Objetivo

Dar à Diretoria uma forma de acompanhar quanto cada produtor entregou em um mês, distinguindo as entregas pelo nível de dificuldade.

Para isso a atividade passa a ter uma complexidade, e o sistema ganha uma tela que lista os produtores e abre, para cada um, as atividades realizadas no mês, separadas por complexidade.

Esta spec complementa a modelagem de domínio (`2026-10-06-modelagem-dominio-design.md`) e não contém código.

### Critérios de sucesso

- Toda atividade tem uma complexidade 1, 2 ou 3, definida pela Diretoria.
- A Diretoria abre a produtividade de um produtor e vê, para o mês corrente, quantas atividades ele realizou em cada complexidade e quais são elas.
- A Diretoria consulta do mesmo modo qualquer mês anterior.
- Um usuário de Produção não acessa a tela nem a consulta.

## 2. Decisões

| Tema | Decisão |
|---|---|
| Quem é produtor | Usuário cujo perfil é de tipo `Producao`, ativo ou inativo |
| O que é atividade realizada | Atividade em `Concluida`, contada no mês de `ConcluidaEm`, para cada responsável |
| Obrigatoriedade da complexidade | Obrigatória desde a criação |
| Atividades já existentes | Recebem complexidade 1 na migration |
| Conteúdo da consulta | Total por complexidade, total geral e as atividades de cada grupo |
| Origem dos números | Calculados a cada consulta, a partir do estado atual das atividades |
| Onde a produtividade aparece | Painel na própria página dos produtores |

Alternativas descartadas:

- **Contar a parte concluída pelo produtor**, em vez da atividade concluída: contaria trabalho ainda não aprovado pelo revisor.
- **Página própria com rota por produtor**: seria a primeira rota com parâmetro do sistema, sem ganho pedido.
- **Fechamento mensal gravado em tabela**: congelaria os meses passados, mas exige processo de fechamento e uma segunda fonte de verdade. Fica para quando a produtividade virar base de meta ou pagamento.

## 3. Complexidade

A atividade ganha o campo `Complexidade`.

| Campo | Regra |
|---|---|
| `Complexidade` | Inteiro obrigatório; valores aceitos: 1, 2 e 3 |

Regras:

- É informada junto com título, descrição e data de entrega, na criação e na edição da atividade. Só a Diretoria define, como os demais campos.
- Um valor fora de 1 a 3 é recusado pelo domínio com a mensagem "A complexidade deve ser 1, 2 ou 3."
- Pode ser alterada em qualquer status, inclusive em atividade concluída: a edição de atividade não tem restrição de status.
- A alteração aparece no `RegistroAuditoria` como a de qualquer outro campo, sem evento próprio.
- Os valores são números sem nome. O campo é um inteiro com faixa validada, não um enum, porque o conjunto de valores pode mudar.
- A migration cria a coluna como obrigatória e grava 1 em todas as atividades existentes.

## 4. Produtividade

### Atividade realizada

Uma atividade conta como realizada por um produtor em um mês quando, no momento da consulta:

1. está no status `Concluida` e não foi excluída;
2. `ConcluidaEm` cai dentro do mês, no fuso do escritório;
3. o produtor é um dos responsáveis dela.

Consequências:

- Uma atividade com vários responsáveis conta uma vez para cada um.
- Uma atividade reaberta deixa de contar. Ao ser concluída de novo, conta no mês da nova conclusão.
- Vale a complexidade que a atividade tem agora, não a que tinha quando foi concluída.
- Os números de um mês passado mudam quando uma atividade daquele mês é reaberta, excluída ou tem a complexidade alterada. A auditoria registra cada uma dessas mudanças.

### Produtor

- Produtor é o usuário cujo perfil atual é de tipo `Producao`.
- Inativos entram na lista, marcados: a consulta de meses passados precisa de quem já saiu do escritório.
- Vale o perfil atual. Quem passou de Produção para Diretoria deixa a lista; quem fez o caminho inverso entra, com as atividades que concluiu como responsável.
- Um usuário de Diretoria não é produtor, mesmo sendo responsável em atividades.

### Consulta

A consulta recebe o produtor e um intervalo em UTC. A tela converte o mês escolhido para o intervalo, com o fuso do escritório, como a Agenda faz com a semana.

| Situação | Resposta |
|---|---|
| Quem consulta não é da Diretoria | `AcessoNegado` |
| Produtor inexistente, ou usuário que não é de perfil `Producao` | `NaoEncontrado` |
| Início do intervalo igual ou posterior ao fim | `Validacao` |
| Intervalo maior que 92 dias | `Validacao` |

O limite de 92 dias é o mesmo da Agenda. A tela sempre pede um mês.

A resposta traz:

- o nome do produtor;
- três grupos, um para cada complexidade, sempre presentes mesmo quando vazios. Cada grupo traz o total e as atividades, com título, nome do cliente e data de conclusão;
- o total geral;
- a data da primeira conclusão do produtor: o menor `ConcluidaEm` entre as atividades hoje concluídas em que ele é responsável, ou nada quando não há nenhuma.

Dentro de cada grupo as atividades vêm por data de conclusão, da mais antiga para a mais recente. O cliente excluído logicamente continua identificado pelo nome, como na lista de atividades.

A lista de produtores não tem consulta própria: usa a consulta de usuários existente, e a tela mostra os de tipo `Producao`.

## 5. Telas

### Menu

Item "Produtores" no grupo da Diretoria, depois de "Usuários".

### Página de produtores (`/produtores`)

- Exclusiva da Diretoria. Os demais veem "Acesso negado.", como na página de usuários.
- Tabela com Nome, Perfil, OAB e Situação, ordenada por nome. Os inativos aparecem marcados, como nas outras telas.
- Cada linha tem o botão "Acompanhar produtividade", que abre o painel de produtividade abaixo da tabela. Abrir outro produtor troca o painel.

### Painel de produtividade

- Cabeçalho "Produtividade — " seguido do nome do produtor, com o botão Fechar.
- Seletor único de mês e ano ("outubro/2026"), do mês corrente para trás até o mês da primeira conclusão do produtor. Sem nenhuma conclusão, o seletor traz só o mês corrente.
- O painel abre no mês corrente. Trocar o mês refaz a consulta.
- Quatro totais: Complexidade 1, Complexidade 2, Complexidade 3 e Total.
- Um bloco por complexidade, com a quantidade no título e a tabela das atividades: Título, Cliente, Concluída em, e o botão "Abrir". Grupo vazio mostra "Nenhuma atividade."
- "Abrir" exibe a ficha da atividade já existente abaixo do painel. Quando a ficha avisa que algo mudou, o painel refaz a consulta do mês.
- Falha na consulta mostra o erro e o botão "Tentar de novo" no lugar dos números. Uma resposta de acesso negado encerra a tela, como na página de atividades.

### Telas de atividade

- Formulário: campo obrigatório "Complexidade", com as opções 1, 2 e 3. Em atividade nova começa em "Selecione…".
- Ficha: mostra a complexidade junto dos demais dados.
- Lista: coluna "Complexidade". Um responsável de Produção também a vê, porque vê a atividade.

## 6. Auditoria

- Consultar a produtividade de um produtor grava um `RegistroAcesso`: recurso `Produtividade`, ação `Visualizou`, o produtor em `RecursoId` e o intervalo em `Detalhe`.
- Listar os produtores fica registrado pela consulta de usuários (`Usuario`, `Listou`), que a página reutiliza.
- Alterar a complexidade gera o registro de alteração da atividade, pelo interceptor.

## 7. Testes

- Unitários no `Domain`: complexidade 0 e 4 recusadas; 1, 2 e 3 aceitas; alteração na edição.
- Unitários na `Application`: acesso negado para Produção; não encontrado para usuário de Diretoria e para usuário inexistente; validação do intervalo; agrupamento por complexidade, grupos vazios e totais.
- De integração, com PostgreSQL real: a consulta traz só atividades concluídas dentro do intervalo e daquele responsável; ignora reabertas e excluídas; respeita a virada do mês no fuso do escritório; devolve a primeira conclusão; grava o `RegistroAcesso`. A migration deixa complexidade 1 nas atividades existentes.
- De tela: a página lista os produtores e nega acesso à Produção; o painel exibe totais e grupos; o formulário traz o campo de complexidade sem escolha inicial; a lista de atividades mostra a complexidade. Cliques não têm teste automatizado e são conferidos por leitura.

## 8. Emendas na modelagem de domínio

Ao final da implementação, `2026-10-06-modelagem-dominio-design.md` recebe:

- seção 5, matriz de acesso: linha "Produtividade — Diretoria: consulta; Produção: sem acesso";
- seção 8: o campo `Complexidade` e as suas regras;
- uma subseção "Produtividade" ao final da seção 8, com a definição de atividade realizada, de produtor e as regras da consulta. É subseção, e não seção numerada, para não renumerar as seções que os planos anteriores citam;
- seção 10, `RegistroAcesso`: o registro da consulta de produtividade.

## 9. Fora do escopo

Pontuação ponderada por complexidade; metas; comparação entre produtores; totais na lista de produtores; exportação; gráficos; fechamento mensal; consulta da própria produtividade pelo produtor; complexidade configurável pela Diretoria.
