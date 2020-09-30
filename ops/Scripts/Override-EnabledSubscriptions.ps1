#requires -version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Env,
    #".\_vsclk-core\drop\outputs\build\binaries\VsoUtil"
    [string]$VsoUtilDirectory = $(Join-Path $PSScriptRoot .. .. bin debug VsoUtil)
)

# Auto-install modules as necessary, so this can be run from AzDO pipelines
foreach ($module in @("Az.Accounts", "Az.Keyvault", "Az.Resources")) {
    if (-not (Get-Module $module)) {
        Install-Module -Name $module -Scope CurrentUser -Force -AllowClobber
    }
}

$opsDir = Resolve-Path (Join-Path $PSScriptRoot ..)

# Pull service principal password from keyvault
$secret = az keyvault secret show --name "app-sp-password" --vault-name "vsclk-online-$Env-kv" --query value -o tsv

# Create appsettings.secrets.json file
@{ AppSecrets = @{ appServicePrincipalClientSecret = $secret } } | ConvertTo-Json | Out-File (Join-Path $VsoUtilDirectory "appsettings.secrets.json")

Join-Path $VsoUtilDirectory "appsettings.local.json" | Remove-Item

$env:UseSecretFromAppConfig = 1


# Override enabled subscriptions

. "$opsDir\Scripts\Subscription-Tracker.ps1"

$subscriptions = Get-Subscriptions -Environment:$Env -Plane:"data" -UseAppSettingsFilters:$true -SubscriptionJsonFile:"$opsDir\Components.generated\subscriptions.json"

$subscriptions | Where-Object { $_.location } | Enable-Subscription -VsoUtilDirectory $VsoUtilDirectory