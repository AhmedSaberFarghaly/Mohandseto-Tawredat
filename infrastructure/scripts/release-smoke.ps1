param(
    [Parameter(Mandatory = $true)][string]$ApiBaseUrl,
    [string]$AdminBaseUrl,
    [Parameter(Mandatory = $true)][string]$AdminEmail,
    [Parameter(Mandatory = $true)][SecureString]$AdminPassword
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http
$ApiBaseUrl = $ApiBaseUrl.TrimEnd('/')
$AdminBaseUrl = if ([string]::IsNullOrWhiteSpace($AdminBaseUrl)) { "" } else { $AdminBaseUrl.TrimEnd('/') }
$passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($AdminPassword)
$client = $null
$handler = $null

try {
    $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.Timeout = [TimeSpan]::FromSeconds(30)

    function Assert-Status([System.Net.Http.HttpResponseMessage]$Response, [int]$Expected, [string]$Name) {
        if ([int]$Response.StatusCode -ne $Expected) {
            $body = $Response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            throw "$Name failed: HTTP $([int]$Response.StatusCode) $body"
        }
        Write-Host "PASS $Name" -ForegroundColor Green
    }

    $live = $client.GetAsync("$ApiBaseUrl/health/live").GetAwaiter().GetResult()
    Assert-Status $live 200 "API liveness"
    $ready = $client.GetAsync("$ApiBaseUrl/health/ready").GetAwaiter().GetResult()
    Assert-Status $ready 200 "API readiness"
    if (-not $ready.Headers.Contains("X-Content-Type-Options")) { throw "Security headers are missing" }
    Write-Host "PASS security headers" -ForegroundColor Green

    $anonymous = $client.GetAsync("$ApiBaseUrl/api/admin/monitoring").GetAwaiter().GetResult()
    Assert-Status $anonymous 401 "anonymous admin rejection"

    $loginBody = [System.Net.Http.StringContent]::new(
        (@{ email = $AdminEmail; password = $password } | ConvertTo-Json -Compress),
        [Text.Encoding]::UTF8,
        "application/json")
    $login = $client.PostAsync("$ApiBaseUrl/api/auth/login", $loginBody).GetAwaiter().GetResult()
    Assert-Status $login 200 "admin login"
    $auth = $login.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
    if ($auth.requiresTwoFactor) { throw "Smoke account requires 2FA; use a controlled release account or extend the smoke flow with its OTP." }
    if ([string]::IsNullOrWhiteSpace($auth.accessToken)) { throw "Login did not return an access token" }
    $client.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $auth.accessToken)

    $catalog = $client.GetAsync("$ApiBaseUrl/api/catalog/categories").GetAwaiter().GetResult()
    Assert-Status $catalog 200 "catalog read"
    $monitoring = $client.GetAsync("$ApiBaseUrl/api/admin/monitoring").GetAwaiter().GetResult()
    Assert-Status $monitoring 200 "admin monitoring"

    if (-not [string]::IsNullOrWhiteSpace($AdminBaseUrl)) {
        $admin = $client.GetAsync("$AdminBaseUrl/login").GetAwaiter().GetResult()
        Assert-Status $admin 200 "admin web"
    }

    Write-Host "Release smoke gate passed." -ForegroundColor Cyan
}
finally {
    if ($passwordPointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer) }
    if ($null -ne $client) { $client.Dispose() }
    if ($null -ne $handler) { $handler.Dispose() }
}
