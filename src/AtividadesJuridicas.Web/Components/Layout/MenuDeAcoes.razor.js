// O menu de ações fecha quando o foco sai dele: Tab depois do último item, Shift+Tab no botão, clique fora que leva o
// foco a outro elemento. Fechar não move o foco (o Tab segue o caminho natural). O fundo e os itens estão dentro do
// invólucro, então o foco que passa por eles não conta como saída.
//
// Safari e Firefox no macOS não focam o <button> ao clique: o mousedown tira o foco da lista com relatedTarget nulo, antes
// do click. Enquanto há um gesto de ponteiro dentro do invólucro (do pointerdown, em captura, até o pointerup ou o
// pointercancel), o focusout sem destino é ignorado e quem decide é o click (o fundo, o botão ou um item). O pointerup
// vem sempre depois do focusout do mousedown e antes do click, e o pointercancel cobre o gesto abortado: a marca não
// fica de pé (um setTimeout seria um palpite sobre o tempo do clique).
const vigias = new WeakMap();

export function vigiarSaida(involucro, menu) {
    if (!involucro || vigias.has(involucro)) return;

    let emGesto = false;
    const aoPressionar = () => { emGesto = true; };
    const aoSoltar = () => { emGesto = false; };
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
    // O pointerup pode acontecer fora do invólucro (o fundo some ao fechar): a marca cai no documento.
    document.addEventListener("pointerup", aoSoltar, true);
    document.addEventListener("pointercancel", aoSoltar, true);
    vigias.set(involucro, () => {
        involucro.removeEventListener("pointerdown", aoPressionar, true);
        involucro.removeEventListener("focusout", aoSair);
        document.removeEventListener("pointerup", aoSoltar, true);
        document.removeEventListener("pointercancel", aoSoltar, true);
    });
}

export function pararDeVigiar(involucro) {
    const remover = involucro && vigias.get(involucro);
    if (!remover) return;

    remover();
    vigias.delete(involucro);
}