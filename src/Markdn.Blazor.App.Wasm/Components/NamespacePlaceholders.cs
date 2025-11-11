// Placeholder namespaces for source-generator emitted using directives.
// The generator can emit using directives like `using Markdn.Blazor.App.Components;`
// when markdown front-matter lists componentNamespaces. The WASM project
// doesn't include the server's Components folder by default, which can lead
// to CS0234: the namespace doesn't exist. Creating these empty namespaces
// keeps the build moving; if type-level errors appear next we can add
// small stubs for specific components or replicate shared components.

namespace Markdn.Blazor.App.Components {}
namespace Markdn.Blazor.App.Components.Shared {}
namespace Markdn.Blazor.App.Components.Pages {}
