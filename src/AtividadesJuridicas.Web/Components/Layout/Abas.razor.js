// As teclas que as abas tratam (←, →, Home e End) não rolam a fileira nem a página: o componente escolhe a aba, leva
// o foco a ela e pede o rolarAteAba, que rola a fileira só o que for preciso para mostrá-la. Com Alt, Ctrl ou Cmd a
// tecla continua do navegador (Alt+← volta a página).
const teclas = new Set(["ArrowLeft", "ArrowRight", "Home", "End"]);
const presas = new WeakSet();

export function prenderTeclas(lista) {
    if (!lista || presas.has(lista)) return;

    presas.add(lista);
    lista.addEventListener("keydown", evento => {
        if (teclas.has(evento.key) && !evento.altKey && !evento.ctrlKey && !evento.metaKey) evento.preventDefault();
    });
}

// A aba que recebeu o foco pelas teclas vem inteira para a vista: o focus() não rola a fileira quando a aba já aparece em
// parte. "nearest" só rola o necessário, e nada quando ela já está toda à vista.
export function rolarAteAba(aba) {
    aba?.scrollIntoView({ block: "nearest", inline: "nearest" });
}
