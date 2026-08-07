$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$markdownFiles = @((Join-Path $repoRoot 'README.md')) + @(
    Get-ChildItem (Join-Path $repoRoot 'Docs') -Recurse -Filter '*.md' -File |
        Sort-Object FullName |
        Select-Object -ExpandProperty FullName
)
$sourceFiles = @(
    Get-ChildItem (Join-Path (Join-Path $repoRoot 'Assets') 'Levity') -Recurse -Filter '*.cs' -File |
        Sort-Object FullName |
        Select-Object -ExpandProperty FullName
)

$externalSymbols = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
@(
    'Action', 'AddComponent', 'ArgumentException', 'ArgumentNullException', 'Array',
    'Button', 'CancellationToken', 'CancellationTokenSource', 'Component', 'ConfigureAwait',
    'ContainsKey', 'Debug', 'Dictionary', 'Dispose', 'Exception', 'FindAnyObjectByType', 'Func', 'GameObject',
    'GetAwaiter', 'GetResult', 'IEquatable', 'InvalidOperationException', 'IsCompleted', 'List',
    'Mathf', 'MonoBehaviour', 'Object', 'OperationCanceledException', 'PointerEventData',
    'ScriptableObject', 'SerializeField', 'Slider', 'String', 'Task', 'TaskCompletionSource',
    'Time', 'Timeout', 'Toggle', 'TryGetValue', 'UnityEngine', 'ValueTask', 'Vector2', 'Vector3', 'Vector4',
    'WaitForSeconds', 'name', 'value'
) | ForEach-Object { [void]$externalSymbols.Add($_) }

function Remove-CommentsAndStrings([string]$text) {
    $text = [regex]::Replace($text, '/\*.*?\*/', ' ', 'Singleline')
    $text = [regex]::Replace($text, '//[^\r\n]*', ' ')
    $text = [regex]::Replace($text, '@?"(?:""|\\.|[^"\\])*"', '""')
    return [regex]::Replace($text, "'(?:\\.|[^'\\])'", "''")
}

function Relative-Path([string]$path) {
    return $path.Substring($repoRoot.Length).TrimStart('\', '/').Replace('\', '/')
}

$apiSymbols = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($sourceFile in $sourceFiles) {
    $code = Remove-CommentsAndStrings ([IO.File]::ReadAllText($sourceFile))
    foreach ($namespace in [regex]::Matches($code, '\bnamespace\s+([A-Za-z_][A-Za-z0-9_.]*)')) {
        foreach ($part in $namespace.Groups[1].Value.Split('.')) {
            [void]$apiSymbols.Add($part)
        }
    }
    foreach ($line in ($code -split '\r?\n')) {
        if ($line -match '\b(public|protected)\b') {
            foreach ($match in [regex]::Matches($line, '\b[A-Za-z_][A-Za-z0-9_]*\b')) {
                [void]$apiSymbols.Add($match.Value)
            }
        }
    }
    foreach ($enum in [regex]::Matches($code, '\bpublic\s+enum\s+\w+[^\{]*\{(.*?)\}', 'Singleline')) {
        foreach ($match in [regex]::Matches($enum.Groups[1].Value, '\b[A-Za-z_][A-Za-z0-9_]*\b')) {
            [void]$apiSymbols.Add($match.Value)
        }
    }
}

$problems = [System.Collections.Generic.List[string]]::new()

foreach ($markdownFile in $markdownFiles) {
    $text = [IO.File]::ReadAllText($markdownFile)
    $relative = Relative-Path $markdownFile

    foreach ($match in [regex]::Matches($text, '(?<!!)\[[^\]]*\]\(([^)]+)\)')) {
        $rawTarget = $match.Groups[1].Value.Trim()
        $target = ($rawTarget -split '\s+', 2)[0].Trim('<', '>')
        if (!$target -or $target.StartsWith('#') -or $target -match '^(https?://|mailto:)') { continue }

        $filePart = [Uri]::UnescapeDataString(($target -split '#', 2)[0])
        $resolved = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $markdownFile) $filePart))
        $line = ($text.Substring(0, $match.Index) -split '\r?\n').Count
        if (!$resolved.StartsWith($repoRoot, [StringComparison]::OrdinalIgnoreCase)) {
            $problems.Add("${relative}:${line}: local link escapes repository: $rawTarget")
        }
        elseif (!(Test-Path -LiteralPath $resolved)) {
            $problems.Add("${relative}:${line}: dead local link: $rawTarget")
        }
    }

    $lines = $text -split '\r?\n'
    $documentCode = Remove-CommentsAndStrings $text
    $documentDeclared = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($declaration in [regex]::Matches($documentCode, '\b(?:class|struct|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)')) {
        [void]$documentDeclared.Add($declaration.Groups[1].Value)
    }
    foreach ($enum in [regex]::Matches($documentCode, '\benum\s+\w+[^\{]*\{(.*?)\}', 'Singleline')) {
        foreach ($member in [regex]::Matches($enum.Groups[1].Value, '\b[A-Za-z_][A-Za-z0-9_]*\b')) {
            [void]$documentDeclared.Add($member.Value)
        }
    }
    $inCSharp = $false
    $blockStart = 0
    $block = [System.Collections.Generic.List[string]]::new()

    for ($index = 0; $index -lt $lines.Count; $index++) {
        if (!$inCSharp -and $lines[$index] -match '^```\s*(csharp|cs)\s*$') {
            $inCSharp = $true
            $blockStart = $index + 2
            $block.Clear()
            continue
        }
        if ($inCSharp -and $lines[$index] -match '^```') {
            $rawCode = $block -join "`n"
            $code = Remove-CommentsAndStrings $rawCode
            $ignored = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
            foreach ($ignore in [regex]::Matches($rawCode, 'doc-lint:\s*ignore\s+([A-Za-z_][A-Za-z0-9_]*)')) {
                [void]$ignored.Add($ignore.Groups[1].Value)
            }
            $declared = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
            foreach ($declaration in [regex]::Matches($code, '\b(?:class|struct|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)')) {
                [void]$declared.Add($declaration.Groups[1].Value)
            }
            foreach ($declaration in [regex]::Matches($code, '\b(?:public|protected|private|internal)\s+(?:override\s+|static\s+|sealed\s+|readonly\s+|async\s+)*(?:[A-Za-z_][A-Za-z0-9_<>,.?\[\]]*\s+)+([A-Za-z_][A-Za-z0-9_]*)\s*(?=[(;=])')) {
                [void]$declared.Add($declaration.Groups[1].Value)
            }

            $codeLines = $code -split '\r?\n'
            for ($codeIndex = 0; $codeIndex -lt $codeLines.Count; $codeIndex++) {
                $candidates = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
                foreach ($candidate in [regex]::Matches($codeLines[$codeIndex], '\b[A-Z][A-Za-z0-9_]*\b')) {
                    [void]$candidates.Add($candidate.Value)
                }
                foreach ($candidate in [regex]::Matches($codeLines[$codeIndex], '\.\s*([A-Za-z_][A-Za-z0-9_]*)')) {
                    [void]$candidates.Add($candidate.Groups[1].Value)
                }
                foreach ($symbol in $candidates) {
                    if ($declared.Contains($symbol) -or $documentDeclared.Contains($symbol) -or $ignored.Contains($symbol) -or $externalSymbols.Contains($symbol) -or $apiSymbols.Contains($symbol)) { continue }
                    $lineNumber = $blockStart + $codeIndex
                    $problems.Add("${relative}:${lineNumber}: C# example references unknown repository API symbol '$symbol'")
                }
            }
            $inCSharp = $false
            continue
        }
        if ($inCSharp) { $block.Add($lines[$index]) }
    }
}

if ($problems.Count -gt 0) {
    $problems | Sort-Object | ForEach-Object { [Console]::Error.WriteLine($_) }
    [Console]::Error.WriteLine("doc-lint: $($problems.Count) problem(s)")
    exit 1
}

Write-Output "doc-lint: OK ($($markdownFiles.Count) Markdown files, $($sourceFiles.Count) C# source files)"
