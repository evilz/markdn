$baseUrl = "https://raw.githubusercontent.com/san-ghun/astro-pico/main/src/components"
$previewUrl = "$baseUrl/preview"
$dest = "temp_components"
$previewDest = "$dest/preview"

New-Item -ItemType Directory -Force -Path $dest
New-Item -ItemType Directory -Force -Path $previewDest

$previewComponents = @(
    "Hgroup.astro",
    "Dropdown.astro",
    "Sample.astro",
    "Blockquote.astro",
    "Lists.astro",
    "InlineTextElements.astro",
    "Headings.astro",
    "Buttons.astro",
    "States.astro",
    "Accordions.astro",
    "Group.astro"
)

$baseComponents = @(
    "Modal.astro",
    "Heading.astro",
    "Search.astro",
    "Text.astro",
    "Select.astro",
    "FileBrowser.astro",
    "RangeSlider.astro",
    "Date.astro",
    "Time.astro",
    "Color.astro",
    "Checkbox.astro",
    "RadioButton.astro",
    "Switch.astro",
    "Table.astro",
    "Article.astro",
    "Progress.astro",
    "LoadingArticle.astro",
    "LoadingButton.astro"
)

foreach ($comp in $previewComponents) {
    $url = "$previewUrl/$comp"
    $out = "$previewDest/$comp"
    Write-Host "Downloading $comp..."
    Invoke-WebRequest -Uri $url -OutFile $out
}

foreach ($comp in $baseComponents) {
    $url = "$baseUrl/$comp"
    $out = "$dest/$comp"
    Write-Host "Downloading $comp..."
    Invoke-WebRequest -Uri $url -OutFile $out
}

Write-Host "Download complete."
