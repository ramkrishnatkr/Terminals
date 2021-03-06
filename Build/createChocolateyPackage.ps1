# Script to automatically generate the the chocolatey nuget package
Param(
	[Parameter(Mandatory=$true)]
	[string]$Version,
	[Parameter(Mandatory=$true)]
	[string]$DownloadId
)

$OutDir = ".\Output";
$OutTools = "$OutDir\tools\";
$nuspecFile = "$OutDir\terminals.nuspec";
$installScript = 'chocolateyInstall.ps1';
$installerPath = gi $OutDir\Terminals*.msi;

if($installerPath -eq $null){
  Write-Error "Build the installer first to be able pack it into new package.";
  exit 1;
}

# Copy temporary files
md $OutTools -Force | Out-Null;
cp .\terminals.nuspec $OutDir -Force;
cp .\tools\chocolateyUninstall.ps1 $OutTools;

# update install script
$chocoSCript = type .\tools\$installScript;
$chocoSCript[0] = "`$url = 'https://terminals.codeplex.com/downloads/get/$DownLoadId';";
$checksum = checksum $installerPath -t=sha256;
$chocoSCript[1] = "`$checksum = '$checksum';";
echo $chocoSCript > $OutTools$installScript;

# build the package
echo "creating package version:'$Version'";
choco pack $nuspecFile --version $Version --outputdirectory $OutDir;

#Cleanup temp files.
rm $OutTools -Recurse -Force;
rm $nuspecFile -Force;

exit 0;