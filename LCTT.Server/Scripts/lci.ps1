#*********************************
#*** LCTT Collector Initialize ***
#*********************************

if ($null -eq (Get-Module -ListAvailable -Name PSSQLite)) {
    Install-Module PSSQLite -Force
}
Import-Module PSSQLite

$TranslateProject = 'PATH-TO/TranslateProject'
$dataSource = 'Data/SQLite.db'
$table = 'URL'

$urls = Get-ChildItem `
    -File `
    -Recurse `
    -Depth 2 `
    -Path $TranslateProject `
    -Filter '*.md' `
    -Exclude 'README.md', 'lctt*.md', 'core.md', 'Dict.md'
    | Select-String -Pattern 'via: http' -Raw

for ($i = 0; $i -lt $urls.Length; $i++) {
    $url = $urls[$i].ToString()
    $urls[$i] = $url.Substring('via: '.Length).Trim(' ', ']', '/')
}

$dataTable = $urls | Get-Unique | ForEach-Object { [pscustomobject]@{ Value = "$_" } } | Out-DataTable
Invoke-Command {
    # dotnet restore
    dotnet ef database drop --force
    dotnet ef database update
}
Invoke-SQLiteBulkCopy -DataSource $dataSource -Table $table -DataTable $dataTable -Force
$number = $dataTable.Rows.Count
"Imported $number existing URLs."
