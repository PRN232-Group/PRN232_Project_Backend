# Fix Unicode seed corruption + Product 4 price
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Data
$cs = "Server=(localdb)\MSSQLLocalDB;Database=InteriorStudio;Trusted_Connection=True;TrustServerCertificate=True"
$cn = New-Object System.Data.SqlClient.SqlConnection $cs
$cn.Open()

function Exec-Sql([string]$sql, [hashtable]$params) {
  $cmd = $cn.CreateCommand()
  $cmd.CommandText = $sql
  foreach ($k in $params.Keys) {
    $v = $params[$k]
    if ($null -eq $v) { $v = [DBNull]::Value }
    [void]$cmd.Parameters.AddWithValue($k, $v)
  }
  [void]$cmd.ExecuteNonQuery()
}

Exec-Sql "UPDATE Products SET Price = @p, UpdatedAt = SYSUTCDATETIME() WHERE Id = 4" @{
  p = [decimal]6800000
}

Exec-Sql "UPDATE Products SET Name = @n WHERE Id = 1" @{ n = [string]([char]0x0053 + "ofa Nordic 3 ch" + [char]0x1ED7) }
# Better: use .NET unicode escapes via [char]
$names = @{
  1 = ("Sofa Nordic 3 " + [char]0x1ED7 + " cho")  # wrong - cho vs chỗ
}
# Use explicit UTF-16 codepoints for Vietnamese
function U([int[]]$codes) { -join ($codes | ForEach-Object { [char]$_ }) }

# Sofa Nordic 3 chỗ  (chỗ = U+1ED7)
$sofa = "Sofa Nordic 3 " + [char]0x1ED7
# Actually "chỗ" is c + h + ỗ (U+1ED7) - "chỗ" = ch + ỗ
$sofa = "Sofa Nordic 3 ch" + [char]0x1ED7

# Bàn trà Oak - Bàn = B + à (U+00E0) + n
$ban = "B" + [char]0x00E0 + "n tr" + [char]0x00E0 + " Oak"

# Kệ TV - ệ = U+1EC7
$ke = "K" + [char]0x1EC7 + " TV Walnut"

# Đèn - Đ U+0110, è U+00E8
$den = [char]0x0110 + "e" + [char]0x00E0 + "n s" + [char]0x00E0 + "n Brass"
# Wait Đèn = Đ + è + n = U+0110 + U+00E8 + n
$den = [string][char]0x0110 + [string][char]0x00E8 + "n s" + [string][char]0x00E0 + "n Brass"

# Giường - ườ = ư U+01B0 + ờ? Actually ườ is ư + ờ combining... "ường" 
# Giường = G i ườ n g - ườ = U+01B0 + U+1EDD? "ườ" is often one syllable: ườ = ư (01B0) + ờ (1EDD) no
# Correct: ườ = U+01B0 (ư) + U+1EDD (ờ) as separate? In NFC "ường" ...
# Giường NFC: G i ườ n g where ườ is U+01B0 U+1EDD? Let's check: [char]0x01B0 is ư, [char]0x1EDD is ờ
$giuong = "Gi" + [char]0x01B0 + [char]0x1EDD + "ng Sleepwell 1m6"

Exec-Sql "UPDATE Products SET Name = @n WHERE Id = 1" @{ n = $sofa }
Exec-Sql "UPDATE Products SET Name = @n WHERE Id = 2" @{ n = $ban }
Exec-Sql "UPDATE Products SET Name = @n WHERE Id = 4" @{ n = $ke }
Exec-Sql "UPDATE Products SET Name = @n WHERE Id = 5" @{ n = $den }
Exec-Sql "UPDATE Products SET Name = @n WHERE Id = 6" @{ n = $giuong }

# Nguyen Van An - ễ U+1EC5, ă U+0103
$an = "Nguy" + [char]0x1EC5 + "n V" + [char]0x0103 + "n An"
# Tran Thi Binh - ầ U+1EA7, ị U+1ECB
$binh = "Tr" + [char]0x1EA7 + "n Th" + [char]0x1ECB + " B" + [char]0x00EC + "nh"

Exec-Sql "UPDATE Users SET FullName = @n WHERE Email = @e" @{ n = $an; e = "an@example.com" }
Exec-Sql "UPDATE Users SET FullName = @n WHERE Email = @e" @{ n = $binh; e = "binh@example.com" }

# Bao gia = B + á U+00E1 + o
$bao = "B" + [char]0x00E1 + "o gi" + [char]0x00E1 + " "
$phong = "ph" + [char]0x00F2 + "ng "
$title1 = $bao + $phong + "kh" + [char]0x00E1 + "ch 20m2"
$title2 = $bao + $phong + "ng" + [char]0x1EE7 + " t" + [char]0x1ED1 + "i gi" + [char]0x1EA3 + "n"
# ngủ = n + g + ủ? ngủ = ng + ủ = n g ủ - ủ is U+1EE7
# tối = t + ố U+1ED1 + i
# giản = g + i + ả U+1EA3 + n

$desc1 = "Sofa Nordic + " + $ban + " + " + $ke
$desc2 = $giuong.Replace(" Sleepwell 1m6","") + " Sleepwell 1m6 + " + $den

$reply2 = "T" + [char]0x1ED5 + "ng 11.900.000 " + [char]0x20AB + " theo catalog hi" + [char]0x1EC7 + "n t" + [char]0x1EA1 + "i."
# Tổng = T + ổ U+1ED5 + ng
# ₫ = U+20AB
# hiện = hi + ệ U+1EC7 + n
# tại = t + ạ U+1EA1 + i

$notes1 = "Sofa 12.5M + Ban 3.2M + Ke 6.8M = 22.5M"
$notes2 = "Giuong 9.8M + Den 2.1M = 11.9M - da duyet"

Exec-Sql "UPDATE QuotationRequests SET Title=@t, Description=@d, Reply=NULL WHERE Id=1" @{ t = $title1; d = $desc1 }
Exec-Sql "UPDATE QuotationRequests SET Title=@t, Description=@d, Reply=@r WHERE Id=2" @{ t = $title2; d = $desc2; r = $reply2 }
Exec-Sql "UPDATE Quotations SET Title=@t, Notes=@n WHERE Id=1" @{ t = $title1; n = $notes1 }
Exec-Sql "UPDATE Quotations SET Title=@t, Notes=@n WHERE Id=2" @{ t = $title2; n = $notes2 }

$cmd = $cn.CreateCommand()
$cmd.CommandText = "SELECT Id, Title FROM QuotationRequests; SELECT Id, Name, Price FROM Products WHERE Id IN (1,2,4,5,6)"
$rd = $cmd.ExecuteReader()
do {
  while ($rd.Read()) {
    $parts = @()
    for ($i = 0; $i -lt $rd.FieldCount; $i++) { $parts += [string]$rd.GetValue($i) }
    Write-Output ($parts -join " | ")
  }
  Write-Output "---"
} while ($rd.NextResult())
$rd.Close()
$cn.Close()
Write-Output "OK"
