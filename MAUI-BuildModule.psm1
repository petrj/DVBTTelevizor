function Get-Password 
{
    <#
    .SYNOPSIS
        Prompts user for keystore password
    .OUTPUTS
        [string] - plain text password
    #>
    Write-Host "Enter the password to KeyStore, please:"
     
    $passwEncrypted = Read-Host -AsSecureString
    $ptr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($passwEncrypted)
    $passw = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    
    return $passw
}

function Publish-AABPackage 
{
    <#
    .SYNOPSIS
        Publishes a MAUI project to .aab

    .DESCRIPTION
        Takes a .csproj file, builds it using dotnet publish, and outputs the .aab file
        as a FileInfo object. Supports configuration (Debug/Release), and allows custom package name.

    .INPUTS
        [System.IO.FileInfo] - .csproj file

    .OUTPUTS
        [System.IO.FileInfo] - .aab file
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [System.IO.FileInfo]$InputObject,

        [string]$Framework     = "net9.0-android35.0",

        [ValidateSet("Debug", "Release")]
        [string]$Configuration = "Release",

        [Parameter(Mandatory)]
        [string]$PackageName
    )
    process 
    {
        Write-Host "Publishing project $($InputObject.Name) with configuration $Configuration and framework $Framework"

        dotnet publish $InputObject.FullName -c $Configuration -f $Framework /p:AndroidPackageFormat=aab | Out-Default

        # Detect Version + AndroidTV suffix
        [xml]$csproj = Get-Content $InputObject.FullName

        $versionNode = $csproj.Project.PropertyGroup | Where-Object { $_.Condition -like "*android*" } 

        $version = $versionNode.VersionCode
        if ($version -is [System.Array]) { $version = $version[0] }

        [xml]$manifest = Get-Content (Join-Path $InputObject.Directory.FullName "Platforms\Android\AndroidManifest.xml")
        if ($manifest.manifest.'uses-feature'.name -eq "android.software.leanback") 
        {
            $suffix = ".AndroidTV"
        } else {
            $suffix = ""
        }

        # Build new AAB name
        $aabName = "$PackageName.${version}${suffix}.aab"
        $aabPath = Join-Path $InputObject.Directory.FullName "bin\$Configuration\$Framework\$aabName"

        # Rename/move the produced AAB
        $builtAab = Join-Path $InputObject.Directory.FullName "bin\$Configuration\$Framework\$PackageName.aab"
        
        if (Test-Path $builtAab) 
        {
            Copy-Item $builtAab $aabPath -Force
        } else 
        {
            throw "Could not find $builtAab"
        }

        Write-Host "Published to: $aabPath"

        return Get-Item $aabPath
    }
}

function Protect-BySignature 
{
    <#
    .SYNOPSIS
        Signs an AAB package with jarsigner
    .INPUTS
        [System.IO.FileInfo] - .aab file
    .OUTPUTS
        [System.IO.FileInfo] - signed .aab file
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [System.IO.FileInfo]$InputObject,

        [string]$Keystore,
        [string]$Alias,
        [string]$Password,
        [string]$JarSigner = "C:\Program Files (x86)\Android\openjdk\jdk-17.0.14\bin\jarsigner.exe"
    )
    process 
    {
        Write-Host "Signing $($InputObject.Name)"
        & $JarSigner -keystore $Keystore -storepass $Password $InputObject.FullName $Alias | Out-Default
         
        if ($LASTEXITCODE -ne 0) 
        {
            throw "jarsigner failed with exit code $LASTEXITCODE when signing $($InputObject.FullName)"
        }

        Write-Host "$($InputObject.Name) signed ($Alias)"

        return $InputObject
    }
}

function ConvertTo-APK 
{
    <#
    .SYNOPSIS
        Converts signed .aab to universal .apk
    .INPUTS
        [System.IO.FileInfo] - .aab file
    .OUTPUTS
        [System.IO.FileInfo] - .apk file
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [System.IO.FileInfo]$InputObject,

        [string]$Keystore,
        [string]$Alias,
        [string]$Password,
        [string]$BundleTool = "C:\Program Files (x86)\Android\android-sdk\platform-tools\bundletool-all-1.18.1.jar",
        [string]$Java       = "C:\Program Files (x86)\Android\openjdk\jdk-17.0.14\bin\java.exe"
    )
    process {
        $aab = $InputObject.FullName
        $aabDirectory = $InputObject.Directory.FullName
        $outputArchive = "$aab.apks"

        if (Test-Path -Path $outputArchive) 
        {
            Write-Host "Removing existing $outputArchive"
            Remove-Item -Path $outputArchive -Force
        }

        Write-Host "Creating universal APK from $($InputObject.Name)"
        & $Java -jar $BundleTool build-apks --bundle=$aab --output=$outputArchive --mode=universal --ks=$Keystore --ks-key-alias=$Alias --ks-pass=pass:$Password 

        if (Test-Path -Path ($outputArchive + ".zip"))
        {
            Write-Host "Removing existing archive"
            Remove-Item -Path ($outputArchive + ".zip") -Force
        }

        Rename-Item -Path $outputArchive -NewName ($outputArchive + ".zip") -Force
        $outputArchive += ".zip"

        $apkName = [System.IO.Path]::GetFileNameWithoutExtension($aab) + ".apk"
        $apkName = Join-Path -Path $aabDirectory -ChildPath "$apkName"

        Expand-Archive -Path $outputArchive -DestinationPath $aabDirectory -Force
        
        Remove-Item (Join-Path -Path $aabDirectory -ChildPath "*.pb")
        Remove-Item $outputArchive


        if (Test-Path -Path $apkName)
        {
            Write-Host "Removing existing $apkName"
            Remove-Item -Path $apkName -Force
        }

        Rename-Item (Join-Path -Path $aabDirectory -ChildPath "universal.apk") $apkName

        (Get-Item $apkName).LastWriteTime = Get-Date

        return Get-Item $apkName
    }
}
