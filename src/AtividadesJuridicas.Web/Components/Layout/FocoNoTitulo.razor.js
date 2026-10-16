// O foco e a rolagem que o teclado e o leitor de tela precisam. O módulo não conhece nenhuma tela: só o h1, o main
// do layout e o seletor que quem chama passa. Cada visão (desktop, tablet, mobile) tem a sua cópia dos elementos e as
// das outras ficam escondidas; por isso tudo aqui procura o primeiro que está à vista (que tem caixa).

function primeiroVisivel(seletor) {
    for (const elemento of document.querySelectorAll(seletor)) {
        if (elemento.getClientRects().length > 0) return elemento;
    }
    return null;
}

// Depois de cada navegação: o título à vista recebe o foco, para ser anunciado, sem rolar a página.
export function focarTituloVisivel() {
    const titulo = primeiroVisivel("h1");
    if (titulo === null) return;

    if (!titulo.hasAttribute("tabindex")) titulo.setAttribute("tabindex", "-1");
    titulo.focus({ preventScroll: true });
}

// Foca o primeiro elemento à vista que casa com o seletor (o par visível de um botão que tem uma cópia por visão, o
// botão que abriu um painel). Devolve se o foco chegou a ele.
export function focarPrimeiroVisivel(seletor) {
    const elemento = primeiroVisivel(seletor);
    if (elemento === null) return false;

    elemento.focus();
    return document.activeElement === elemento;
}

// Página nova começa do topo. Quem rola é o main do layout (a janela fica parada); sem ele (entrar, erro), nada a fazer.
export function zerarRolagem() {
    document.querySelector("main.aj-app__conteudo")?.scrollTo(0, 0);
}

let vigiando = false;
let ultimoFocado = null;

// Rede de segurança: quando uma renderização tira do DOM o elemento que tinha o foco (o botão de um painel que fechou,
// a linha que saiu da lista), o navegador deixa o foco no body e o próximo Tab recomeça do alto da página. A rede leva
// o foco ao título à vista. Os casos com destino melhor (quem abriu o painel, o aviso de resultado) são tratados por
// tela e chegam depois, pelo .NET: tiram o foco do título.
//
// Como a rede sabe que o elemento saiu do DOM: guarda o último elemento que recebeu o foco (focusin) e, depois de
// cada mudança no DOM (MutationObserver, que roda logo depois de o Blazor aplicar a renderização inteira), confere se
// ele deixou de estar no documento (isConnected) com o foco no body. Não depende do focusout/blur na remoção, que uns
// navegadores disparam e outros não.
//
// Clique fora (num ponto sem nada focável): o foco também vai ao body, mas o elemento continua no DOM, e a rede não
// age. Para não agir depois, se uma renderização tirar esse elemento mais tarde, a saída por clique fora o esquece.
export function vigiarFoco() {
    if (vigiando) return;
    vigiando = true;

    document.addEventListener("focusin", evento => { ultimoFocado = evento.target; }, true);

    // Foco que sai para lugar nenhum (relatedTarget nulo): clique fora, troca de janela ou remoção do elemento. A
    // conferência espera a tarefa seguinte, depois da rede: na remoção, a rede já agiu e o foco está no título; no
    // clique fora, o elemento continua no DOM sem o foco, e é esquecido; na troca de janela, ele continua com o foco
    // (document.activeElement) e é mantido.
    document.addEventListener("focusout", evento => {
        if (evento.relatedTarget !== null) return;

        const saiu = evento.target;
        setTimeout(() => {
            if (ultimoFocado === saiu && saiu.isConnected && document.activeElement !== saiu) ultimoFocado = null;
        }, 0);
    }, true);

    new MutationObserver(() => {
        if (ultimoFocado === null || ultimoFocado.isConnected) return;

        const ativo = document.activeElement;
        if (ativo !== null && ativo !== document.body) return;

        ultimoFocado = null;
        focarTituloVisivel();
    }).observe(document.body, { childList: true, subtree: true });
}
