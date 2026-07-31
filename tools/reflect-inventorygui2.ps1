$flags = [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance
$asm = [System.Reflection.Assembly]::LoadFrom('D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll')
$t = $asm.GetType('InventoryGui')
$t.GetFields($flags) | Where-Object { $_.Name -match 'drag|firstContainer|autoClose' } | ForEach-Object {
    Write-Output ("{0} : {1}" -f $_.Name, $_.FieldType.Name)
}
$t.GetMethod('SetActiveGroup', $flags).ToString()
