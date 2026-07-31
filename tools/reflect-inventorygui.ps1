$flags = [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance
$asm = [System.Reflection.Assembly]::LoadFrom('D:\SteamLibrary\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll')
$t = $asm.GetType('InventoryGui')
$t.GetFields($flags) | Where-Object { $_.Name -match 'container|Container|m_current|m_animator|m_hidden|m_player' } | ForEach-Object {
    Write-Output ("{0} : {1}" -f $_.Name, $_.FieldType.Name)
}
$t.GetMethods($flags) | Where-Object { $_.Name -match 'SetActiveGroup|SetupInventory' } | ForEach-Object { $_.ToString() }
