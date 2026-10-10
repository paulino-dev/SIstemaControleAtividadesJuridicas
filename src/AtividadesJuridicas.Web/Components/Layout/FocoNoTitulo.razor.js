// Cada visão tem o seu h1 e os das outras ficam escondidos: o foco vai para o que está à vista.
export function focarTituloVisivel() {
    for (const titulo of document.querySelectorAll("h1")) {
        if (titulo.getClientRects().length === 0) continue;

        if (!titulo.hasAttribute("tabindex")) titulo.setAttribute("tabindex", "-1");
        titulo.focus({ preventScroll: true });
        return;
    }
}
