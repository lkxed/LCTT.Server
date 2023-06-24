#******************************
#*** LCTT Collector Branch ***
#******************************

$action = $args[0]
$uri = 'https://localhost:7092/api/collector/branch'

if ($null -eq $action) {
    Write-Host -BackgroundColor DarkRed 'Missing action'
} elseif ('clean' -eq $action) {
    $response = Invoke-RestMethod `
        -SkipCertificateCheck `
        -Method Delete `
        -Uri $uri
    
    if ('error' -eq $response.status) {
        Write-Host -BackgroundColor DarkRed $response.message
    } elseif ('sucess' -eq $response.status) {
        Write-Host -BackgroundColor Blue $response.data
    }
}