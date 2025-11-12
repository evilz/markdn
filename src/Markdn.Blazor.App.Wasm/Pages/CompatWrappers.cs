using Microsoft.AspNetCore.Components;

namespace Markdn.Blazor.App.Pages
{
    // Compatibility wrapper: generated .md.g.cs files assume Counter exists in
    // namespace Markdn.Blazor.App.Pages. The actual Counter in this project is
    // under Markdn.Blazor.App.Wasm.Pages. Open that component at runtime.
    public class Counter : ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent(0, typeof(Markdn.Blazor.App.Wasm.Pages.Counter));
            builder.CloseComponent();
        }
    }
}
