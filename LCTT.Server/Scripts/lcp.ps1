#******************************
#*** LCTT Collector Preview ***
#******************************

$url = $args[0]

if ($null -eq $url) {
    $url = Read-Host -Prompt 'url'
}

if ([string]::IsNullOrWhiteSpace($url)) {
    Write-Host -BackgroundColor DarkRed "Error: Missing url."
    Exit
}

$uri = 'https://localhost:7092/api/collector/article'

$response = Invoke-RestMethod `
    -SkipCertificateCheck `
    -Method Get `
    -Uri $uri `
    -Body @{ url = $url }

if ('error' -eq $response.status) {
    Write-Host -BackgroundColor DarkRed $response.message
} elseif ('success' -eq $response.status) {
    $article = $response.data
    if (-Not [string]::IsNullOrWhiteSpace($article)) {
        $previewPath = 'preview.md'
        $article | Out-File -FilePath $previewPath
        Write-Host -NoNewline "Previewing in VSCode..."
        Invoke-Command -ScriptBlock {
            code -w $previewPath
            Remove-Item $previewPath
        }
        'Complete.'
    }
}