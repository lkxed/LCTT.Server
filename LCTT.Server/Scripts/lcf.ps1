#***************************
#*** LCTT Collector Feed ***
#***************************

$startDate = $args[0]
$endDate = $args[1]

if ($startDate -And $endDate) {
    "Since: $startDate"
    "Until: $endDate"
} elseif ($startDate) {
    if ($startDate -like '-*' -or $startDate -eq '0') {
        $days = [convert]::ToInt32($startDate);
        $startDate = (Get-Date).AddDays($days).ToString('yyyyMMdd');
    }
    "Since: $startDate"
    $endDate = Get-Date -Format 'yyyyMMdd'
    "Until: $endDate"
} else {
    $startDate = Read-Host -Prompt 'Since'
    $endDate = Read-Host -Prompt 'Until'
}

if ([string]::IsNullOrWhiteSpace($startDate)) {
    $startDate = Get-Date -Format 'yyyyMMdd'
}

if ([string]::IsNullOrWhiteSpace($endDate)) {
    $endDate = Get-Date -Format 'yyyyMMdd'
}

$uri = 'https://localhost:7092/api/collector/feed'
$groupBy = 'true'
$parameters = @{
    startDate = $startDate
    endDate = $endDate
    groupBy = $groupBy
}

$response = Invoke-RestMethod `
    -SkipCertificateCheck `
    -Method Get `
    -Uri $uri `
    -Body $parameters
    | ConvertTo-Json -Depth 3
    | ConvertFrom-Json -AsHashtable

if ('error' -eq $response['status']) {
    $response['message']
} elseif ('success' -eq $response['status']) {
    $data = $response['data']
    if ('true' -eq $groupBy) {
        foreach ($date in $data.Keys) {
            "                      ----------"
            "                    > $date <"
            "                      ^^^^^^^^^^"
            $feeds = $data[$date]
            for ($i = 0; $i -lt $feeds.Count; $i++) {
                Write-Host -NoNewline "[$i] "
                Write-Host -ForegroundColor DarkGreen $feeds[$i]['title']
                Write-Host -ForegroundColor Blue $feeds[$i]['url']
                ''       
            }
        }
    } else {
        "                      -------------------"
        "                    > $startDate ~ $endDate <"
        "                      ^^^^^^^^^^^^^^^^^^^"
        $feeds = $data
        for ($i = 0; $i -lt $feeds.Count; $i++) {
            Write-Host -NoNewline "[$i] "
            Write-Host -ForegroundColor DarkGreen $feeds[$i]['title']
            Write-Host -ForegroundColor Blue $feeds[$i]['url']
            ''       
        }
    }
}

