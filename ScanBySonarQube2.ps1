function Export-SonarQubeAnalysis {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$ProjectKey,

        [Parameter(Mandatory = $false)]
        [string]$ServerUrl = "http://enterprise:9000",

        [Parameter(Mandatory = $false)]
        [string]$Token = $env:SONAR_TOKEN,

        [Parameter(Mandatory = $false)]
        [string]$OutputPath = ".\SonarQubeAnalysis.json",

        [Parameter(Mandatory = $false)]
        [ValidateSet("JSON", "CSV")]
        [string]$Format = "JSON",

        [Parameter(Mandatory = $false)]
        [switch]$IncludeMetrics
    )

    process {
        if ([string]::IsNullOrWhiteSpace($Token)) {
            throw "SonarQube API token is missing. Pass -Token or set `$env:SONAR_TOKEN."
        }

        $ServerUrl = $ServerUrl.TrimEnd('/')
        $auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${Token}:"))
        $headers = @{ Authorization = "Basic $auth" }

        Write-Host "Fetching issues for project '$ProjectKey' from $ServerUrl..." -ForegroundColor Cyan

        # 1. Fetch itemized issues with pagination
        $allIssues = [System.Collections.Generic.List[PSObject]]::new()
        $page = 1
        $pageSize = 500

        do {
            $issuesUrl = "${ServerUrl}/api/issues/search?componentKeys=${ProjectKey}&resolved=false&ps=${pageSize}&p=${page}"
            try {
                $response = Invoke-RestMethod -Uri $issuesUrl -Headers $headers -Method Get
            }
            catch {
                throw "Failed to fetch issues from SonarQube API: $_"
            }

            foreach ($issue in $response.issues) {
                # Format component path to relative file path
                $filePath = $issue.component -replace "^${ProjectKey}:", ""

                $allIssues.Add([PSCustomObject]@{
                    Key        = $issue.key
                    Rule       = $issue.rule
                    Severity   = $issue.severity
                    Type       = $issue.type
                    Component  = $filePath
                    Line       = $issue.line
                    Message    = $issue.message
                    Effort     = $issue.effort
                    Creation   = $issue.creationDate
                })
            }

            $total = $response.total
            $page++
        } while ($allIssues.Count -lt $total)

        Write-Host "Retrieved $($allIssues.Count) issues." -ForegroundColor Green

        # 2. Optionally fetch summary metrics
        $metricsData = $null
        if ($IncludeMetrics) {
            Write-Host "Fetching summary metrics..." -ForegroundColor Cyan
            $metricKeys = "bugs,vulnerabilities,code_smells,coverage,duplicated_lines_density,security_hotspots"
            $metricsUrl = "${ServerUrl}/api/measures/component?component=${ProjectKey}&metricKeys=${metricKeys}"
            try {
                $metricsResponse = Invoke-RestMethod -Uri $metricsUrl -Headers $headers -Method Get
                $metricsData = $metricsResponse.component.measures
            }
            catch {
                Write-Warning "Could not retrieve measures: $_"
            }
        }

        # 3. Export data based on requested format
        if ($Format -eq "CSV") {
            $allIssues | Export-Csv -Path $OutputPath -NoTypeInformation -Encoding utf8
            Write-Host "Exported issues to CSV: $OutputPath" -ForegroundColor Green
        }
        else {
            if ($IncludeMetrics) {
                $exportPayload = [PSCustomObject]@{
                    ProjectKey  = $ProjectKey
                    ExportedAt  = (Get-Date).ToString("o")
                    Measures    = $metricsData
                    IssuesCount = $allIssues.Count
                    Issues      = $allIssues
                }
            }
            else {
                $exportPayload = $allIssues
            }

            $exportPayload | ConvertTo-Json -Depth 10 | Out-File -FilePath $OutputPath -Encoding utf8
            Write-Host "Exported analysis to JSON: $OutputPath" -ForegroundColor Green
        }
    }
}

Export-SonarQubeAnalysis -ProjectKey "DVBTTelevizor" -ServerUrl $env:SONAR_URL -Token $env:SONAR_TOKEN 