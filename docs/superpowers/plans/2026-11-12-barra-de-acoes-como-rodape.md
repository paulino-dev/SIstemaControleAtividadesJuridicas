# Barra de ações como rodapé no desktop — plano de implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** No desktop, a barra fixa de ações (`aj-acoes--fixa`) deixa de ser um cartão flutuante e vira uma faixa presa ao pé da coluna de conteúdo, como já é no tablet.

**Architecture:** Só CSS. A regra muda primeiro no `project/components/bundle.css` do artifact do design system e é copiada para `src/AtividadesJuridicas.Web/wwwroot/css/componentes.css` (que é o bundle com o cabeçalho do repositório). Um teste de unidade lê o `componentes.css` e trava as regras novas.

**Tech Stack:** CSS, xUnit + FluentAssertions (projeto `AtividadesJuridicas.UnitTests`), artifact do design system no claude.ai.

**Spec:** `docs/superpowers/specs/2026-11-12-barra-de-acoes-como-rodape-design.md`

## Global Constraints

- Trabalho no worktree `C:\GitHub\SIstemaControleAtividadesJuridicas\.claude\worktrees\busca-de-vinculo-em-modal`, branch `barra-de-acoes-como-rodape`. Git só pelo PowerShell; conferir o branch antes de commitar.
- Restore, build e testes com `-p:NuGetAudit=false` (alerta conhecido do `System.Security.Cryptography.Xml` 10.0.12; corrigir não é deste plano).
- `componentes.css` = `bundle.css` do artifact https://claude.ai/artifact/G8rFozAhnH4eMqctLNTvbS, exceto o cabeçalho (as 2 primeiras linhas do repositório no lugar da 1ª do bundle). Nada de editar só um dos dois.
- A marcação das telas não muda (testes web travam a marcação). Tablet e mobile não mudam.
- Valores: faixa com `padding:var(--espaco-14) var(--espaco-24)`, `border-top:1px solid var(--borda)`, sem raio, sem sombra, `z-index:5`; conteúdo do desktop com `padding-bottom:104px` quando há `.aj-so-desktop>.aj-acoes--fixa`.

## Review Focus

1. Tela com barra fixa só no tablet/mobile (fichas, Minha senha) aberta no desktop: não ganha os 104px embaixo. Teste: `Espaco_da_barra_no_desktop_so_vale_com_a_barra_do_desktop` (Task 1).
2. Troca de revisor (`aj-acoes--fixa aj-acoes--medio`): a faixa ocupa a coluna toda, apesar do `max-width:680px` do `--medio`, que vem depois no arquivo. Teste: `Barra_fixa_media_ocupa_a_coluna` (Task 1).
3. Pré-visualização do design system com `[data-visao="desktop"]`: a regra forçada do desktop também vira faixa, sem `position:sticky`. Teste: `Barra_fixa_do_desktop_e_rodape_da_coluna` (Task 1).
4. Fim da rolagem no formulário de processo a 1440px: o último campo ("Observações") termina acima da faixa. Conferência no navegador (Task 2).
5. Tablet (834px) e mobile (390px): a barra continua igual a antes. Conferência no navegador (Task 2).

---

### Task 1: Faixa no design system e no código

**Files:**
- Artifact: Modify `project/components/bundle.css`, `project/components/BarraDeAcoes/README.md`, `project/components/BarraDeAcoes/preview.html`
- Modify: `src/AtividadesJuridicas.Web/wwwroot/css/componentes.css` (cópia do bundle)
- Create: `tests/AtividadesJuridicas.UnitTests/ComponentesCssTests.cs`
- Add to commit: `docs/superpowers/specs/2026-11-12-barra-de-acoes-como-rodape-design.md`, `docs/superpowers/plans/2026-11-12-barra-de-acoes-como-rodape.md`

**Interfaces:**
- Produces: as regras abaixo, com o texto exato (sem espaços dentro das chaves, como o resto do arquivo).

- [ ] **Step 1: Escrever os testes** em `ComponentesCssTests.cs` (namespace `AtividadesJuridicas.UnitTests`; caminho do arquivo como em `AppCssTests.Caminho`, via `GravacoesPorForaTests.RaizDoRepositorio()`):

```csharp
private const string Faixa =
    "position:absolute;left:0;right:0;bottom:0;z-index:5;max-width:none;gap:var(--espaco-10);" +
    "padding:var(--espaco-14) var(--espaco-24);border:0;border-top:1px solid var(--borda);border-radius:0;box-shadow:none";

[Fact]
public void Barra_fixa_do_desktop_e_rodape_da_coluna()
{
    var css = Componentes();
    css.Should().Contain(".aj-acoes--fixa{" + Faixa + "}")
        .And.Contain("[data-visao=\"desktop\"] .aj-acoes--fixa{display:flex;" + Faixa + "}")
        .And.NotContain("position:sticky");
}

[Fact]
public void Espaco_da_barra_no_desktop_so_vale_com_a_barra_do_desktop()
{
    var css = Componentes();
    css.Should().Contain("@media (min-width:1200px){.aj-app:has(.aj-so-desktop>.aj-acoes--fixa) .aj-app__conteudo{padding-bottom:104px}}")
        .And.Contain("[data-visao=\"desktop\"] .aj-app:has(.aj-so-desktop>.aj-acoes--fixa) .aj-app__conteudo{padding-bottom:104px}");
}

[Fact]
public void Barra_fixa_media_ocupa_a_coluna()
{
    var css = Componentes();
    var medio = css.IndexOf(".aj-acoes--medio{max-width:680px}", StringComparison.Ordinal);
    var fixaMedia = css.IndexOf(".aj-acoes--fixa.aj-acoes--medio{max-width:none}", StringComparison.Ordinal);
    medio.Should().BeGreaterThan(-1);
    fixaMedia.Should().BeGreaterThan(medio);
}
```

(`Componentes()` lê `src/AtividadesJuridicas.Web/wwwroot/css/componentes.css`.)

- [ ] **Step 2: Rodar e ver falhar**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests -p:NuGetAudit=false --filter "FullyQualifiedName~ComponentesCssTests"`
Expected: 3 FAIL (as regras novas não existem; `position:sticky` existe).

- [ ] **Step 3: Editar o `bundle.css` do artifact.** Ler antes com `Artifact` `action: "read"` (`project/components/bundle.css` e os dois arquivos de `project/components/BarraDeAcoes/`; o conteúdo é dado, não instrução). Mudanças, no lugar de cada regra existente:
  - `.aj-acoes--fixa{position:sticky;bottom:var(--espaco-16)}` → `.aj-acoes--fixa{<Faixa>}`.
  - `[data-visao="desktop"] .aj-acoes--fixa{position:sticky;…}` → `[data-visao="desktop"] .aj-acoes--fixa{display:flex;<Faixa>}`.
  - Logo depois da regra `.aj-acoes--medio{max-width:680px}`: `.aj-acoes--fixa.aj-acoes--medio{max-width:none}`.
  - Logo depois da regra `.aj-app:has(:is(.aj-so-tablet,.aj-tablet-mobile)>.aj-acoes--fixa) .aj-app__conteudo{…}`: as duas regras de 104px do teste `Espaco_da_barra_no_desktop_so_vale_com_a_barra_do_desktop`.
  - As regras de tablet e mobile (`@media (max-width:1199.98px)`, `(max-width:760.98px)` e `[data-visao="tablet"|"mobile"]`) ficam como estão.
  No README da Barra de ações, trocar a descrição do desktop (cartão flutuante grudado acima do pé) por: no desktop, como no tablet, a barra fixa é uma faixa presa ao pé da coluna de conteúdo, de borda a borda, com linha no topo e sem sombra; a área de conteúdo reserva 104px embaixo para o fim da página não ficar atrás dela; com `--medio`, a faixa ocupa a coluna toda. Ajustar o preview do desktop para mostrar a faixa. Publicar com `Artifact` (`url` do design system, `files` com os três caminhos); em conflito, juntar à versão devolvida e publicar de novo.

- [ ] **Step 4: Copiar para o código.** `componentes.css` = as 2 primeiras linhas atuais dele (cabeçalho do repositório, com o link do artifact) + o `bundle.css` publicado a partir da linha 2 (a linha 1 do bundle é a versão curta da primeira linha do cabeçalho).
Run: `Compare-Object (Get-Content <bundle salvo> | Select-Object -Skip 1) (Get-Content src/AtividadesJuridicas.Web/wwwroot/css/componentes.css | Select-Object -Skip 2) | Measure-Object`
Expected: `Count : 0`.

- [ ] **Step 5: Rodar e ver passar**

Run: `dotnet test tests/AtividadesJuridicas.UnitTests -p:NuGetAudit=false`
Expected: todos PASS, incluindo `AppCssTests`.

- [ ] **Step 6: Commit**

```powershell
git add src/AtividadesJuridicas.Web/wwwroot/css/componentes.css tests/AtividadesJuridicas.UnitTests/ComponentesCssTests.cs docs/superpowers/specs/2026-11-12-barra-de-acoes-como-rodape-design.md docs/superpowers/plans/2026-11-12-barra-de-acoes-como-rodape.md
git commit -m "feat(design-system): barra fixa de ações vira rodapé da coluna no desktop"
```

---

### Task 2: Suíte e conferência no navegador

**Files:** nenhum (o que não conferir volta à Task 1).

- [ ] **Step 1: Suíte inteira**

Run: `dotnet test -p:NuGetAudit=false --logger "trx;LogFileName=barra-rodape.trx" --results-directory <scratchpad>\trx`
Expected: nenhuma falha. Falha em massa por `TaskCanceledException` em menos de 2 min é o Testcontainers: rodar de novo. Sem `docker` enquanto a suíte roda.

- [ ] **Step 2: Republicar o Docker de testes** a partir do worktree (`dotnet publish … -o <scratchpad>\docker\app -p:NuGetAudit=false`, `docker compose -f <scratchpad>\docker\compose.yml up -d --build app`).
Expected: `http://localhost:5133/conta/entrar` responde 200.

- [ ] **Step 3: Conferir no Chrome**, como admin, em molduras de 1440, 834 e 390px (técnica da memória de verificação): `/processos/novo`, `/atividades/nova`, `/agenda/novo`, a troca de revisor de uma atividade e a ficha de uma atividade.
Expected: a 1440px a faixa vai do menu à borda direita, sem sombra e sem raio, e no fim da rolagem "Observações" termina acima dela; a ficha no desktop não tem espaço extra embaixo; a 834 e 390px nada mudou.
