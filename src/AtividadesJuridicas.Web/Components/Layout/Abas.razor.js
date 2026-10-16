// As teclas que as abas tratam (←, →, Home e End) não rolam a fileira nem a página: o componente escolhe a aba e leva
// o foco a ela. Com Alt, Ctrl ou Cmd a tecla continua do navegador (Alt+← volta a página).
const teclas = new Set(["ArrowLeft", "ArrowRight", "Home", "End"]);
const presas = new WeakSet();

export function prenderTeclas(lista) {
    if (!lista || presas.has(lista)) return;

    presas.add(lista);
    lista.addEventListener("keydown", evento => {
        if (teclas.has(evento.key) && !evento.altKey && !evento.ctrlKey && !evento.metaKey) evento.preventDefault();
    });
}
