function Get-StableHash([string]$str) {
    [int32]$num = 5381
    [int32]$num2 = 5381
    $i = 0
    while ($i -lt $str.Length) {
        [int32]$c = [int32][char]$str[$i]
        $num = [int32](([int64]$num * 33 + $num) -bxor $c)
        if ($i -eq $str.Length - 1) { break }
        [int32]$c2 = [int32][char]$str[$i + 1]
        $num2 = [int32](([int64]$num2 * 33 + $num2) -bxor $c2)
        $i += 2
    }
    return [int32]($num + [int64]$num2 * 1566083941)
}

Write-Output "FF_FletchContainer = $(Get-StableHash 'FF_FletchContainer')"
Write-Output "shipwreck_karve_chest = $(Get-StableHash 'shipwreck_karve_chest')"
