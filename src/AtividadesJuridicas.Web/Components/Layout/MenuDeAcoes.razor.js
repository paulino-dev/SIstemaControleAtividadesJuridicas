// O menu de ações fecha quando o foco sai dele: Tab depois do último item, Shift+Tab no botão, clique fora que leva o
// foco a outro elemento. Fechar não move o foco (o Tab segue o caminho natural). O fundo e os itens estão dentro do
// invólucro, então o foco que passa por eles não conta como saída.
const vigias = new WeakMap();

export function vigiarSaida(involucro, menu) {
    if (!involucro || vigias.has(involucro)) return;

    const aoSair = evento => {
        const destino = evento.relatedTarget;
        if (destino && involucro.contains(destino)) return;
        // Com o menu fechado, sair do botão não é com o menu: nada a avisar.
        if (!involucro.querySelector(":scope > button[aria-expanded='true']")) return;

        menu.invokeMethodAsync("FocoSaiu").catch(() => { });
    };
    involucro.addEventListener("focusout", aoSair);
    vigias.set(involucro, aoSair);
}

export function pararDeVigiar(involucro) {
    const aoSair = involucro && vigias.get(involucro);
    if (!aoSair) return;

    involucro.removeEventListener("focusout", aoSair);
    vigias.delete(involucro);
}
