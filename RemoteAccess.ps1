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



$encryptedMessage = $msg | Encrypt-Message  -Key "DVBTTelevizor"

$encryptedMessage | Send-TCPMessage -Port 49152 -IP 10.0.0.2 | Decrypt-Message -Key "DVBTTelevizor"
