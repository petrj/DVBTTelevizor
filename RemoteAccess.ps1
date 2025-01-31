Function Send-TCPMessage
{
    # https://riptutorial.com/powershell/example/18118/tcp-sender
    Param
    (
            [Parameter(Mandatory=$true, ValueFromPipeline = $true)]
            [ValidateNotNullOrEmpty()]
            [string]
            $Message,

            [Parameter(Mandatory=$true, Position=0)]
            [ValidateNotNullOrEmpty()]
            [string]
            $IP,

            [Parameter(Mandatory=$true, Position=1)]
            [int]
            $Port,

            [Parameter(Mandatory=$false, Position=2)]
            [string]$TerminateString = "b9fb065b-dee4-4b1e-b8b4-b0c82556380c"
    )
    Process
    {
        try
        {
         # Setup connection

            $BufferSize = 1024;
            $bytes = [System.Array]::CreateInstance([byte],$BufferSize)
            $ipAddress = [System.Net.IPAddress]::Parse($IP)

            $remoteEndPoint = New-Object System.Net.IPEndPoint($ipAddress, $Port);            
            
            $sender = New-Object System.Net.Sockets.Socket($ipAddress.AddressFamily, [System.Net.Sockets.SocketType]::Stream, [System.Net.Sockets.ProtocolType]::Tcp)

         # connect and send data

            $sender.Connect($remoteEndPoint)

            $senderIP =  $sender.RemoteEndPoint.Address.ToString()
            
            $bytesSent = $sender.Send( [System.Text.encoding]::ASCII.GetBytes($Message));
            $bytesSent += $sender.Send( [System.Text.encoding]::ASCII.GetBytes($TerminateString));

         # receive response

            $data = [String]::Empty

            while ($true)
            {
                $bytes = [System.Array]::CreateInstance([byte],$BufferSize)
                $bytesRec = $sender.Receive($bytes);
                $data += [System.Text.encoding]::ASCII.GetString($bytes, 0, $bytesRec);
                if ($data.IndexOf($TerminateString) -gt -1)
                {
                    break;
                }
            }    

            $responseMessage = $data.Substring(0, $data.Length - $TerminateString.Length);

            $sender.Shutdown([System.Net.Sockets.SocketShutdown]::Both);
            $sender.Close();

            return $responseMessage;            

        } catch
        {
            Write-Host $_.Exception
        }
    }
}

Function Decrypt-Message
{
    Param
    (
            [Parameter(Mandatory=$true, ValueFromPipeline = $true)]
            [ValidateNotNullOrEmpty()]
            [string]
            $CipherText,

            [Parameter(Mandatory=$true)]
            [ValidateNotNullOrEmpty()]
            [string]
            $Key

    )
    Process
    {
        if ($key.Length -lt 32)
        {
            $key = $key.PadRight(32, "*")
        }
        if ($key.Length -gt 32)
        {
            $key = $key.Substring(0,32)
        }

        try
        {
            $iv = [System.Byte[]]::CreateInstance([System.Byte],16)
            $buffer = [System.Convert]::FromBase64String($CipherText)

            $aes = [System.Security.Cryptography.Aes]::Create()
            $aes.Key = [System.Text.Encoding]::UTF8.GetBytes($Key)
            $aes.IV = $iv

            $decryptor = $aes.CreateDecryptor($aes.Key, $aes.IV)

            $result = $decryptor.TransformFinalBlock($buffer, 0, $buffer.Length)
            return [System.Text.Encoding]::UTF8.GetString($result)

        } finally
        {
            $aes.Dispose()
            $decryptor.Dispose()
        }
    }
}

Function Encrypt-Message
{
    Param
    (
            [Parameter(Mandatory=$true, ValueFromPipeline = $true)]
            [ValidateNotNullOrEmpty()]
            [string]
            $PLaintText,

            [Parameter(Mandatory=$true)]
            [ValidateNotNullOrEmpty()]
            [string]
            $Key

    )
    Process
    {
        $iv = [System.Byte[]]::CreateInstance([System.Byte],16)

        if ($key.Length -lt 32)
        {
            $key = $key.PadRight(32, "*")
        }
        if ($key.Length -gt 32)
        {
            $key = $key.Substring(0,32)
        }

        try
        {
            $aes = [System.Security.Cryptography.Aes]::Create()
            $plainTextBytes = [System.Text.Encoding]::UTF8.GetBytes($PLaintText);

            $aes.Key = [System.Text.Encoding]::UTF8.GetBytes($Key)
            $aes.IV = $iv

            $encryptor = $aes.CreateEncryptor($aes.Key, $aes.IV);

            $memoryStream = new-object System.IO.MemoryStream

            $mode = [System.Security.Cryptography.CryptoStreamMode]::Write
            $cryptoStream = new-object System.Security.Cryptography.CryptoStream($memoryStream, $encryptor, $mode)

            $cryptoStream.Write($plainTextBytes, 0, $plainTextBytes.Length)

            $cryptoStream.FlushFinalBlock();

            $array = $memoryStream.ToArray();

            return [System.Convert]::ToBase64String($array);

        } finally
        {
            $aes.Dispose()
            $memoryStream.Dispose()
            $cryptoStream.Dispose()
        }
    }
}

$TerminateString = "b9fb065b-dee4-4b1e-b8b4-b0c82556380c"

$msgDown = @"
{
 "securityKey":"DVBTTelevizor",
 "command":"keyDown",
 "commandArg1":"DpadDown"
}
"@

$msgEnter = @"
{
 "securityKey":"DVBTTelevizor",
 "command":"keyDown",
 "commandArg1":"Enter"
}
"@

$msg = @"
{
 "sender":"Powershell ISE",
 "securityKey":"DVBTTelevizor",
 "command":"keyDown",
 "commandArg1":"down"
}
"@


Add-Type -AssemblyName System.Windows.Forms


function Get-KeyDownMessage 
{
    [CmdletBinding()]
    param(
            [Parameter(Mandatory=$true, ValueFromPipeline = $false)]
            [ValidateNotNullOrEmpty()]
            [string]$SecurityKey,

            [Parameter(Mandatory=$true, ValueFromPipeline = $true)]
            [ValidateNotNullOrEmpty()]
            [string]$keyCode,

            [Parameter(Mandatory=$false, ValueFromPipeline = $false)]
            [ValidateNotNullOrEmpty()]
            [string]$Sender= "Powershell ISE"
        )
    
    Process {

$msgTemplate = @"
{
 "sender":"{Sender}",
 "securityKey":"{SecurityKey}",
 "command":"keyDown",
 "commandArg1":"{keyCode}"
}
"@
        return $msgTemplate.Replace("{keyCode}",$keyCode).Replace("{SecurityKey}",$SecurityKey).Replace("{Sender}",$Sender)
    }
}

function Show-GUI {
    [CmdletBinding()]
    param(
            [Parameter(Mandatory=$true, ValueFromPipeline = $false)]
            [ValidateNotNullOrEmpty()]
            [string]$SecurityKey,

            [Parameter(Mandatory=$true, ValueFromPipeline = $false)]
            [ValidateNotNullOrEmpty()]
            [string]$IP,

            [Parameter(Mandatory=$true, ValueFromPipeline = $false)]
            [ValidateNotNullOrEmpty()]
            [string]$Port
    )
    
    Process {
        # Create the Form
        $form = New-Object System.Windows.Forms.Form
        $form.Text = "Simple Navigation"
        $form.Size = New-Object System.Drawing.Size(250, 200)
        $form.StartPosition = "CenterScreen"

        # Create Buttons
        $btnLeft = New-Object System.Windows.Forms.Button
        $btnLeft.Text = "Left"
        $btnLeft.Location = New-Object System.Drawing.Point(20, 70)
        $btnLeft.Add_Click(
        { 
            Get-KeyDownMessage -keyCode "left" -SecurityKey $SecurityKey | Encrypt-Message  -Key $SecurityKey | Send-TCPMessage -Port $Port -IP $IP
        })

        $btnRight = New-Object System.Windows.Forms.Button
        $btnRight.Text = "Right"
        $btnRight.Location = New-Object System.Drawing.Point(120, 70)
        $btnRight.Add_Click(
        { 
         
            Get-KeyDownMessage -keyCode "right" -SecurityKey $SecurityKey | Encrypt-Message  -Key $SecurityKey | Send-TCPMessage -Port $Port -IP $IP        
        })

        $btnUp = New-Object System.Windows.Forms.Button
        $btnUp.Text = "Up"
        $btnUp.Location = New-Object System.Drawing.Point(70, 40)
        $btnUp.Add_Click(
        { 
            Get-KeyDownMessage -keyCode "up" -SecurityKey $SecurityKey | Encrypt-Message  -Key $SecurityKey | Send-TCPMessage -Port $Port -IP $IP
        })

        $btnDown = New-Object System.Windows.Forms.Button
        $btnDown.Text = "Down"
        $btnDown.Location = New-Object System.Drawing.Point(70, 100)
        $btnDown.Add_Click(
        { 
            Get-KeyDownMessage -keyCode "down" -SecurityKey $SecurityKey | Encrypt-Message  -Key $SecurityKey | Send-TCPMessage -Port $Port -IP $IP
        })

        # Add buttons to the form
        $form.Controls.Add($btnLeft)
        $form.Controls.Add($btnRight)
        $form.Controls.Add($btnUp)
        $form.Controls.Add($btnDown)

        # Show the Form
        $form.ShowDialog()
    }
}

Show-GUI -SecurityKey "DVBTTelevizor" -IP 10.0.0.2 -Port 49152

#$encryptedMessage = $msg | Encrypt-Message  -Key "DVBTTelevizor"
#$encryptedMessage | Send-TCPMessage -Port 49152 -IP 10.0.0.2 | Decrypt-Message -Key "DVBTTelevizor"
