#!/usr/bin/env bash
set -euo pipefail

feed="$1"
work="$2"
root="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "$work/project" "$work/cache" "$work/tools" "$work/content"
cp "$root/tests/PackageSmoke/PackageSmoke.csproj" "$root/tests/PackageSmoke/Hello.md" "$work/project/"
export NUGET_PACKAGES="$work/cache"

cat > "$work/NuGet.Config" <<EOF
<configuration>
  <packageSources><clear /><add key="local" value="$feed" /><add key="nuget" value="https://api.nuget.org/v3/index.json" /></packageSources>
  <packageSourceMapping><packageSource key="local"><package pattern="Markdn.*" /></packageSource><packageSource key="nuget"><package pattern="*" /></packageSource></packageSourceMapping>
</configuration>
EOF
dotnet restore "$work/project/PackageSmoke.csproj" --configfile "$work/NuGet.Config"
dotnet build "$work/project/PackageSmoke.csproj" --no-restore
generated="$(find "$work/project/obj" -name 'Hello.md.g.cs' -print -quit)"
test -n "$generated"
grep -q 'Package smoke test' "$generated"

dotnet tool install Markdn.Tool --version 0.1.0 --tool-path "$work/tools" --configfile "$work/NuGet.Config"
printf '# CLI smoke test\n' > "$work/content/example.md"
"$work/tools/markdn" "$work"
test -f "$work/content/Example.razor"
grep -q 'CLI smoke test' "$work/content/Example.razor"
