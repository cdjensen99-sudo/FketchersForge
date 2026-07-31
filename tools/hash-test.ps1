$asm = [System.Reflection.Assembly]::LoadFrom('D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll')
foreach ($t in $asm.GetTypes()) {
    foreach ($m in $t.GetMethods([System.Reflection.BindingFlags]::Public | [System.Reflection.BindingFlags]::Static)) {
        if ($m.Name -eq 'GetStableHashCode' -and $m.GetParameters().Length -eq 1) {
            Write-Output "$($t.FullName)::$($m.Name)"
            try {
                $h1 = $m.Invoke($null, @('FF_FletchContainer'))
                $h2 = $m.Invoke($null, @('shipwreck_karve_chest'))
                Write-Output "FF_FletchContainer = $h1"
                Write-Output "shipwreck_karve_chest = $h2"
            } catch { Write-Output $_.Exception.Message }
        }
    }
}
