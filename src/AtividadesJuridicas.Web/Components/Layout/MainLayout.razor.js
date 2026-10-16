// O menu aberto por "Mais" só existe no tablet e no mobile, e com ele aberto o resto da tela fica inerte. Se a
// janela chega ao desktop com o menu aberto, nada à vista o fecha e a tela inteira ficaria sem teclado e sem clique:
// esta vigia avisa o layout, que fecha o menu. O layout decide (fecha só se estiver aberto); aqui só se olha a largura.
const desktop = "(min-width: 1200px)";

export function vigiarDesktop(layout) {
    const consulta = window.matchMedia(desktop);
    const aoMudar = evento => {
        if (!evento.matches) return;

        // O circuito pode ter caído: a vigia é uma proteção e não pode gerar erro na página.
        layout.invokeMethodAsync("AoChegarAoDesktop").catch(() => { });
    };
    consulta.addEventListener("change", aoMudar);

    return {
        parar() {
            consulta.removeEventListener("change", aoMudar);
        },
    };
}
