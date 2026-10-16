Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$rootPath = $PSScriptRoot
$envFile = Join-Path $rootPath '.env'
$projectPath = Join-Path $rootPath 'src/Fepp.ContaOps.Api/Fepp.ContaOps.Api.csproj'

if (-not (Test-Path -LiteralPath $envFile -PathType Leaf)) {
    throw "No se encontró el archivo .env en '$rootPath'. Copia .env.example a .env y configura sus valores."
}

if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw "No se encontró el proyecto API en '$projectPath'."
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'No se encontró dotnet en PATH. Instala el SDK de .NET 10.'
}

$variableNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$lineNumber = 0

foreach ($rawLine in Get-Content -LiteralPath $envFile) {
    $lineNumber++
    $line = $rawLine.Trim()

    if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) {
        continue
    }

    $separatorIndex = $line.IndexOf('=')
    if ($separatorIndex -le 0) {
        throw "Formato inválido en .env, línea $lineNumber. Se esperaba NOMBRE=VALOR."
    }

    $name = $line.Substring(0, $separatorIndex).Trim()
    $value = $line.Substring($separatorIndex + 1).Trim()

    if ($name -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
        throw "Nombre de variable inválido en .env, línea $lineNumber."
    }

    if (-not $variableNames.Add($name)) {
        throw "Variable duplicada en .env, línea ${lineNumber}: $name"
    }

    if (
        $value.Length -ge 2 -and
        (($value.StartsWith("'") -and $value.EndsWith("'")) -or
         ($value.StartsWith('"') -and $value.EndsWith('"')))
    ) {
        $value = $value.Substring(1, $value.Length - 2)
    }

    [Environment]::SetEnvironmentVariable($name, $value, 'Process')
}

$originalLocation = Get-Location
try {
    Set-Location -LiteralPath $rootPath
    & dotnet run --project $projectPath --launch-profile https
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Set-Location -LiteralPath $originalLocation
}
