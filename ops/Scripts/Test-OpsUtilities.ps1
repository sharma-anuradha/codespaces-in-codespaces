# Test-OpsUtilities.ps1
# Simple unit tests for utility functions.

#requires -version 7.0

# Global error handling
trap {
  Write-Error $_
  exit 1
}

# Utilities
. "$PSScriptRoot\OpsUtilities.ps1"

# Preamble
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSDefaultParameterValues['*:ErrorAction'] = 'Stop'
$script:verbose = $false
if ($PSBoundParameters.ContainsKey('Verbose')) {
  $script:Verbose = $PsBoundParameters.Get_Item('Verbose')
}

$script:TotalCount = 0
$script:SucceededCount = 0

function Write-Status() {
  $status = "Passed ${script:SucceededCount} of ${script:TotalCount}"
  if ($script:SucceededCount -eq $script:TotalCount) {
    Write-Host -Message $status -ForegroundColor Green
  }
  else {
    Write-Host -Message $status -ForegroundColor Yellow
  }
}

function Test-Block([scriptblock]$ScriptBlock, $Expected, [string]$Message, [switch]$Throws, [switch]$NotNull) {
  $script:TotalCount += 1

  if ($Throws) {
    try {
      Invoke-Command -ScriptBlock $ScriptBlock
      $err = "Expected exception"
      if ($message) {
        $err += ": $message"
      }
      Write-Host "Failed: $err" -ForegroundColor Red
      return $false
    }
    catch {
      # exception expected
    }
  }
  else {
    $actual = Invoke-Command -ScriptBlock $ScriptBlock

    if ($NotNull) {
      if (!$actual) {
        $err = "Expected non-null result"
        if ($message) {
          $err += ": $message"
        }
        Write-Host "Failed: $err" -ForegroundColor Red
        return $false
      }
    }
    elseif ($actual -cne $expected) {
      $err = "Expected '$expected', actual '$actual'"
      if ($message) {
        $err += ": $message"
      }
      Write-Host "Failed: $err" -ForegroundColor Red
      return $false
    }
  }

  $script:SucceededCount += 1
  return $true
}

Test-Block -Expected $true -ScriptBlock {
  Test-AzureGeography -Geo "ap"
} | Out-Null

Test-Block -Expected $false -ScriptBlock {
  Test-AzureGeography -Geo "zz"
} | Out-Null

Test-Block -Throws -ScriptBlock {
  Test-AzureGeography -Geo "zz" -Throw
} | Out-Null

Test-Block -Expected "vscs-core-test" -ScriptBlock {
  # This tests a special case where we didn't provision test-ops or test-ctl
  Get-AzureSubscriptionName -Component "Core" -Env "Test" -Plane "Ops"
} | Out-Null

Test-Block -Expected "vscs-core-dev-ctl" -ScriptBlock {
  Get-AzureSubscriptionName -Component "Core" -Env "Dev" -Plane "Ctl"
} | Out-Null

Test-Block -Expected "vscs-core-dev-ops-monitoring" -ScriptBlock {
  # Tests that additional parameters are ignored if not data plane
  Get-AzureSubscriptionName -Component "Core" -Env "Dev" -Plane "ops" -DataType "Monitoring" -Geo "us" -RegionCode "e" -Count 99
} | Out-Null

Test-Block -Expected "vscs-core-dev-ops-monitoring" -ScriptBlock {
  # Tests that additional parameters are ignored if not data plane
  Get-AzureSubscriptionName -Component "Core" -Env "Dev" -Plane "ops" -DataType "Monitoring"
} | Out-Null

Test-Block -Throws -ScriptBlock {
  # Missing geo and region
  Get-AzureSubscriptionName -Component "Core" -Env "Dev" -Plane "Data"
} | Out-Null

Test-Block -Throws -ScriptBlock {
  # Missing region
  Get-AzureSubscriptionName -Component "Core" -Env "Dev" -Plane "Data" -Geo "us"
} | Out-Null

Test-Block -Throws -ScriptBlock {
  # Missing geo
  Get-AzureSubscriptionName -Component "Core" -Env "Dev" -Plane "Data" -RegionCode "e"
} | Out-Null

Test-Block -Expected "vscs-core-dev-data-us-e-000" -ScriptBlock {
  Get-AzureSubscriptionName -Component "Core" -Env "Dev" -Plane "Data" -Geo "us" -RegionCode "e"
} | Out-Null

Test-Block -Expected "vscs-core-dev-data-us-e-099" -ScriptBlock {
  Get-AzureSubscriptionName -Component "Core" -Env "Dev" -Plane "Data" -Geo "us" -RegionCode "e" -Count 99
} | Out-Null

Test-Block -Expected "vscs-core-dev-data-compute-us-e-099" -ScriptBlock {
  Get-AzureSubscriptionName -Component "Core" -Env "Dev" -Plane "Data" -DataType "Compute" -Geo "us" -RegionCode "e" -Count 99
} | Out-Null

Test-Block -Throws -Message "bad geo" -ScriptBlock {
  Get-AzureSubscriptionName -Component "Core" -Env "Dev" -Plane "Data" -Geo "xx" -Count 0
} | Out-Null

Test-Block -NotNull -Message "Test policy" -ScriptBlock {
  $policy = New-AzKeyVaultOneCertPolicy -SubjectCName "test"
  Test-Block -Expected "OneCert" -ScriptBlock { $policy.IssuerName } | Out-Null
  Test-Block -Expected "application/x-pem-file" -ScriptBlock { $policy.SecretContentType } | Out-Null
  Test-Block -Expected "RSA" -ScriptBlock { $policy.Kty } | Out-Null
  Test-Block -Expected 2048 -ScriptBlock { $policy.KeySize } | Out-Null
  Test-Block -Expected 180 -ScriptBlock { $policy.RenewAtNumberOfDaysBeforeExpiry } | Out-Null
  Test-Block -Expected 24 -ScriptBlock { $policy.ValidityInMonths } | Out-Null
  Test-Block -Expected $true -ScriptBlock { $policy.SubjectName.StartsWith("CN=test.") } | Out-Null
  $policy
} | Out-Null

# Final Test Status
Write-Status
