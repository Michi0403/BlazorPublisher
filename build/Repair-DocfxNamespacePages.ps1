function Repair-PublisherStudioDocfxNamespacePages {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$SiteRoot
    )

    $siteRootFull = [IO.Path]::GetFullPath($SiteRoot)
    $apiRoot = Join-Path $siteRootFull 'api'
    if (-not (Test-Path -LiteralPath $apiRoot -PathType Container)) { return 0 }

    $apiPages = @(
        Get-ChildItem -LiteralPath $apiRoot -Filter '*.html' -File -ErrorAction SilentlyContinue |
            Where-Object { -not [string]::Equals($_.Name, 'index.html', [StringComparison]::OrdinalIgnoreCase) } |
            Sort-Object Name
    )
    if ($apiPages.Count -eq 0) { return 0 }

    $existingStems = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($page in $apiPages) {
        [void]$existingStems.Add([IO.Path]::GetFileNameWithoutExtension($page.Name))
    }

    $missingNamespaces = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $hrefPattern = '(?is)\bhref\s*=\s*(["''])(?<Href>[^"''#?]+\.html)(?:[?#][^"'']*)?\1'
    foreach ($page in $apiPages) {
        $html = [IO.File]::ReadAllText($page.FullName)
        foreach ($match in [regex]::Matches($html, $hrefPattern)) {
            $href = [Net.WebUtility]::HtmlDecode($match.Groups['Href'].Value).Replace('\', '/')
            if ([string]::IsNullOrWhiteSpace($href) -or $href.Contains('/')) { continue }

            $target = Join-Path $apiRoot $href
            if (Test-Path -LiteralPath $target -PathType Leaf) { continue }

            $candidate = [IO.Path]::GetFileNameWithoutExtension($href)
            if ($candidate -notmatch '^PublisherStudio(?:\.[A-Za-z_][A-Za-z0-9_]*)+$') { continue }

            $prefix = $candidate + '.'
            $hasDescendant = $false
            foreach ($stem in $existingStems) {
                if ($stem.StartsWith($prefix, [StringComparison]::Ordinal)) {
                    $hasDescendant = $true
                    break
                }
            }
            if ($hasDescendant) { [void]$missingNamespaces.Add($candidate) }
        }
    }

    if ($missingNamespaces.Count -eq 0) { return 0 }

    $created = 0
    foreach ($namespace in @($missingNamespaces | Sort-Object { ($_.Split('.').Count) }, { $_ })) {
        $destination = Join-Path $apiRoot ($namespace + '.html')
        if (Test-Path -LiteralPath $destination -PathType Leaf) { continue }

        $prefix = $namespace + '.'
        $descendants = @(
            $existingStems |
                Where-Object { $_.StartsWith($prefix, [StringComparison]::Ordinal) } |
                Sort-Object
        )
        if ($descendants.Count -eq 0) { continue }

        $namespaceEncoded = [Net.WebUtility]::HtmlEncode($namespace)
        $items = [System.Text.StringBuilder]::new()
        foreach ($descendant in $descendants) {
            $descendantEncoded = [Net.WebUtility]::HtmlEncode($descendant)
            [void]$items.AppendLine("          <li><a class=`"xref`" href=`"${descendantEncoded}.html`">$descendantEncoded</a></li>")
        }

        $pageHtml = @"
<!DOCTYPE html>
<html class="publisherstudio-kawaii-docs" data-bs-theme="light" data-publisherstudio-generated-namespace-page="true">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>$namespaceEncoded namespace | PublisherStudio</title>
  <script data-publisherstudio-theme-bootstrap="true">
  (function () {
    var key = "publisherstudio-docs-theme";
    var value = null;
    try { value = localStorage.getItem(key) || localStorage.getItem("theme"); } catch (_) { }
    if (value !== "light" && value !== "dark" && value !== "auto") value = "auto";
    var resolved = value === "auto" && window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : (value === "auto" ? "light" : value);
    document.documentElement.dataset.publisherstudioThemePreference = value;
    document.documentElement.setAttribute("data-bs-theme", resolved);
  })();
  </script>
  <link rel="icon" type="image/svg+xml" href="../favicon.svg" data-publisherstudio-favicon="true" />
  <link rel="alternate icon" href="../favicon.ico" />
  <link rel="stylesheet" href="../styles/publisherstudio-kawaii.css" data-publisherstudio-kawaii-style="true" />
</head>
<body>
  <main class="container-xxl py-4">
    <nav aria-label="Breadcrumb"><a href="../index.html">PublisherStudio documentation</a> · <a href="index.html">API reference</a></nav>
    <article>
      <header><p>Namespace</p><h1>$namespaceEncoded</h1></header>
      <p>This namespace index is materialized by the PublisherStudio documentation pipeline because DocFX referenced the namespace without emitting its landing page.</p>
      <h2>Documented descendants</h2>
      <ul>
$($items.ToString().TrimEnd())
      </ul>
    </article>
  </main>
  <script type="module" src="../styles/publisherstudio-kawaii.js" data-publisherstudio-kawaii-script="true"></script>
</body>
</html>
"@
        [IO.File]::WriteAllText($destination, $pageHtml.TrimStart() + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
        $created++
    }

    return $created
}

