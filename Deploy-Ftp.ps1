param(
    [string]$LocalPath = ".\publish",
    [string]$RemoteHost = "site95431.siteasp.net",
    [string]$RemotePath = "/wwwroot",
    [string]$Username = "site95431.siteasp.net|site95431",
    [string]$Password = "LB776-hzJi8#",
    [int]$Port = 21
)

Write-Host "=== EduCore FTP Deployment ==="
Write-Host "Local Path: $LocalPath"
Write-Host ("Remote Host: {0}:{1}" -f $RemoteHost, $Port)
Write-Host "Remote Path: $RemotePath"
Write-Host "Username: $Username"
Write-Host ""

if (-not (Test-Path $LocalPath)) {
    Write-Error "Local path does not exist: $LocalPath"
    exit 1
}

$files = Get-ChildItem -Path $LocalPath -Recurse -File
Write-Host "Found $($files.Count) files to upload"

try {
    $ftpBaseUrl = "ftp://$RemoteHost`:$Port$RemotePath"
    
    foreach ($file in $files) {
        $relativePath = $file.FullName.Substring($LocalPath.Length + 1).Replace('\', '/')
        $remoteUrl = "$ftpBaseUrl/$relativePath"
        
        $directory = [System.IO.Path]::GetDirectoryName($relativePath)
        if ($directory) {
            $dirUrl = "$ftpBaseUrl/$directory"
            try {
                $dirRequest = [System.Net.FtpWebRequest]::Create($dirUrl)
                $dirRequest.Credentials = New-Object System.Net.NetworkCredential($Username, $Password)
                $dirRequest.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
                $dirRequest.UsePassive = $true
                $dirRequest.UseBinary = $true
                $dirRequest.GetResponse().Close()
                Write-Host "  Created directory: $directory"
            } catch {
                # Directory might already exist, ignore
            }
        }
        
        $request = [System.Net.FtpWebRequest]::Create($remoteUrl)
        $request.Credentials = New-Object System.Net.NetworkCredential($Username, $Password)
        $request.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
        $request.UsePassive = $true
        $request.UseBinary = $true
        
        $fileContent = [System.IO.File]::ReadAllBytes($file.FullName)
        $request.ContentLength = $fileContent.Length
        
        $requestStream = $request.GetRequestStream()
        $requestStream.Write($fileContent, 0, $fileContent.Length)
        $requestStream.Close()
        
        $response = $request.GetResponse()
        $response.Close()
        
        Write-Host "  OK $relativePath"
    }
    
    Write-Host ""
    Write-Host "=== Deployment Complete ==="
    Write-Host ("Files uploaded: {0}" -f $files.Count)
    exit 0
}
catch {
    Write-Error ("Deployment failed: {0}" -f $_)
    exit 1
}