using Microsoft.AspNetCore.Components;

namespace AtividadesJuridicas.Web.Components.Layout;

/// <summary>O que o layout entrega às telas: as iniciais do usuário, para o avatar da barra superior, e o comando de exibir o menu.</summary>
public sealed record ContextoDoLayout(string Iniciais, EventCallback AlternarMenu);
