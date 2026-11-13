# Barra de ações como rodapé no desktop — design curto

Data: 2026-11-12. Tamanho: mudança pequena, só CSS (design system e a cópia no código). Aprovado pelo usuário depois
de ver um esboço no ambiente Docker de testes.

## Por quê

No desktop, a barra fixa de ações (`aj-acoes--fixa`: "Cancelar" e "Salvar …") é um cartão flutuante com sombra,
grudado 16px acima do pé da área que rola. Enquanto não se chega ao fim da página, ela fica por cima dos últimos
campos (no formulário de processo, cobre "Observações"). O usuário quer que ela seja um rodapé da área de conteúdo.

## Objetivo

- No desktop, a barra fixa vira uma faixa presa ao pé da coluna de conteúdo, de borda a borda dessa coluna (do menu
  até a borda direita), fundo `superficie`, linha `borda` no topo, sem raio e sem sombra; botões à direita, como hoje.
- Fica fora da rolagem: o conteúdo passa por baixo dela enquanto rola, mas no fim da página o último campo termina
  acima da faixa.
- É o mesmo comportamento que tablet e mobile já têm; muda só o desktop.

Fora do escopo: a marcação das telas (testes travam a marcação, que não muda), a ordem e o estilo dos botões ("Cancelar"
fantasma no desktop), tablet e mobile.

## Abordagens consideradas

- **Rodapé fixo na coluna, como o tablet (escolhida).** Salvar sempre à mão; o fim da página nunca fica escondido.
- **Barra no fim do formulário, sem fixar.** Nunca cobre nada, mas nos formulários longos é preciso rolar até o fim
  para salvar.
- **Manter o cartão e só aumentar o espaço embaixo.** Continua flutuando por cima dos campos enquanto se rola.

## Como fica

- A partir de 1200px (e com `[data-visao="desktop"]`), `.aj-acoes--fixa`: `position:absolute; left:0; right:0;
  bottom:0; z-index:5; max-width:none; gap: espaco-10; padding: espaco-14 espaco-24; border:0; border-top:1px solid
  borda; border-radius:0; background: superficie; box-shadow:none`. O padding lateral é o da página no desktop (24px).
- `max-width:none` vale também para `aj-acoes--medio` quando fixa: na troca de revisor, a faixa ocupa a coluna toda e
  os botões ficam à direita (antes o cartão acompanhava a largura do painel médio).
- A área de conteúdo, quando a tela tem a barra fixa do desktop (`.aj-so-desktop > .aj-acoes--fixa`), ganha 104px
  embaixo, só a partir de 1200px. As telas cuja barra fixa só existe no tablet ou no mobile não ganham esse espaço no
  desktop. O `scroll-padding-bottom` de 112px que o design system já dá a toda tela com barra fixa cobre o foco por
  teclado.
- Telas afetadas no desktop: formulários de atividade, cliente, evento e processo, e a tela de responsáveis (inclusive
  a troca de revisor). Devolver e reabrir têm, no desktop, uma barra não fixa sob o painel e não mudam; as fichas e
  Minha senha só têm a barra fixa em tablet ou mobile.

Observação do esboço: como no tablet, a faixa cobre o pé da barra de rolagem da área de conteúdo. Aceito, por ser o
comportamento já existente no tablet.

## Conflito com o design system (o usuário confirmou alterar)

A Barra de ações do design system define o cartão flutuante no desktop. A mudança entra primeiro no artifact
(https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS: `project/components/bundle.css`, README e preview da Barra de
ações) e depois é copiada para `wwwroot/css/componentes.css`, igual.

## Verificação

Só CSS: a suíte continua passando sem mudança de teste. No navegador (Docker de testes, 1440px): formulário de
processo e de atividade, com a faixa de borda a borda e o último campo acima dela no fim da rolagem; tela de troca de
revisor com a faixa inteira; tablet e mobile iguais a antes.
