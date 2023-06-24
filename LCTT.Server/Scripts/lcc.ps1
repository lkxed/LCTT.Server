#******************************
#*** LCTT Collector Collect ***
#******************************

$difficulty = $args[0]
$category = $args[1]
$url = $args[2]
$amend = $args[3]

if ($null -eq $difficulty) {
    $difficulty = Read-Host -Prompt 'Difficulty'
}
if ($null -eq $category) {
    $category = Read-Host -Prompt 'Category'
}
if ($null -eq $url) {
    $url = Read-Host -Prompt 'URL'
}

if ([string]::IsNullOrWhiteSpace($difficulty)) {
    Write-Host -BackgroundColor DarkRed 'Missing Difficulty.'
} elseif ([string]::IsNullOrWhiteSpace($category)) {
    Write-Host -BackgroundColor DarkRed 'Missing Category.'
} elseif ([string]::IsNullOrWhiteSpace($url)) {
    Write-Host -BackgroundColor DarkRed 'Missing URL.'
} else {
    $uri = 'https://localhost:7092/api/collector/article'
    $parameters = @{
        difficulty = $difficulty
        category = $category
        url = $url
    }

    function Collect {
        param (
            $Parameters
        )
        $response = Invoke-RestMethod `
            -SkipCertificateCheck `
            -Method Post `
            -Uri $uri `
            -ContentType 'application/json; charset=utf-8' `
            -Body ($Parameters | ConvertTo-Json)
    
        if ('error' -eq $response.status) {
            Write-Host -BackgroundColor DarkRed $response.message
        } else {
            Write-Host -ForegroundColor Blue $response.data
        }
    }

    if ($null -eq $amend) {
        Collect $parameters
    } else {
        $response = Invoke-RestMethod `
            -SkipCertificateCheck `
            -Method Get `
            -Uri $uri `
            -Body @{ url = $url }

        if ('error' -eq $response.status) {
            Write-Error $response.message
        } elseif ('success' -eq $response.status) {
            $content = $response.data
            if (-Not [string]::IsNullOrWhiteSpace($content)) {
                $previewPath = 'preview.md'
                $content | Out-File $previewPath
                Write-Host -NoNewline "Amending in VSCode..."
                Invoke-Command -ScriptBlock {
                    code -w $previewPath
                }
                'Complete.'
                $content = Get-Content -Raw $previewPath
                $parameters['content'] = $content
                Collect $parameters
            }
        }
    }
}
