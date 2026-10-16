// O menu de ações fecha quando o foco sai dele: Tab depois do último item, Shift+Tab no botão, clique fora que leva o
// foco a outro elemento. Fechar não move o foco (o Tab segue o caminho natural). O fundo e os itens estão dentro do
// invólucro, então o foco que passa por eles não conta como saída.
//
// Safari e Firefox no macOS e no iOS não focam o <button> ao clique ou ao toque: o mousedown tira o foco da lista com
// relatedTarget nulo, antes do click. A ordem dos eventos:
//   mouse: pointerdown, mousedown (focusout nulo), pointerup, mouseup, click;
//   toque: pointerdown, pointerup, e só depois os eventos de compatibilidade mousedown (focusout nulo), mouseup, click.
// Por isso a marca "há um gesto de ponteiro dentro do menu" sobe no pointerdown (captura, no invólucro) e só cai no click
// (captura, no document: o fundo some ao fechar, então o click chega pelo document). Enquanto ela está de pé, o focusout
// sem destino é ignorado e quem decide é o click (o fundo, o botão ou um item). Para o gesto que nunca vira click
// (arrastar, soltar fora, pointercancel), o pointerup e o pointercancel armam um setTimeout de 1 s que solta a marca; o
// que vier primeiro (click ou tempo) solta, e um pointerdown novo reinicia.
const vigias = new WeakMap();
const TEMPO_DO_GESTO = 1000;

export function vigiarSaida(involucro, menu) {
    if (!involucro || vigias.has(involucro)) return;

    let emGesto = false;
    let prazo = 0;
    const soltar = () => {
        emGesto = false;
        clearTimeout(prazo);
    };
    const aoPressionar = () => {
        emGesto = true;
        clearTimeout(prazo);
    };
    const aoSoltarOuCancelar = () => {
        clearTimeout(prazo);
        prazo = setTimeout(soltar, TEMPO_DO_GESTO);
    };
    const aoSair = evento => {
        const destino = evento.relatedTarget;
        if (destino && involucro.contains(destino)) return;
        if (!destino && emGesto) return;
        // Com o menu fechado, sair do botão não é com o menu: nada a avisar.
        if (!involucro.querySelector(":scope > button[aria-expanded='true']")) return;

        menu.invokeMethodAsync("FocoSaiu").catch(() => { });
    };
    involucro.addEventListener("pointerdown", aoPressionar, true);
    involucro.addEventListener("focusout", aoSair);
    document.addEventListener("pointerup", aoSoltarOuCancelar, true);
    document.addEventListener("pointercancel", aoSoltarOuCancelar, true);
    document.addEventListener("click", soltar, true);
    vigias.set(involucro, () => {
        soltar();
        involucro.removeEventListener("pointerdown", aoPressionar, true);
        involucro.removeEventListener("focusout", aoSair);
        document.removeEventListener("pointerup", aoSoltarOuCancelar, true);
        document.removeEventListener("pointercancel", aoSoltarOuCancelar, true);
        document.removeEventListener("click", soltar, true);
    });
}

export function pararDeVigiar(involucro) {
    const remover = involucro && vigias.get(involucro);
    if (!remover) return;

    remover();
    vigias.delete(involucro);
}
