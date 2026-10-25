# Yangi klaviatura tartiblarini joriy foydalanuvchining til ro'yxatiga qo'shadi yoki olib tashlaydi.
param([switch]$Add, [switch]$Remove)

$tips = '0443:A0000443', '0443:A0010443'
$list = Get-WinUserLanguageList
$uz = $list | Where-Object { $_.LanguageTag -like 'uz-Latn*' } | Select-Object -First 1

if ($Add) {
    if (-not $uz) {
        $list.Add('uz-Latn')
        $uz = $list | Where-Object { $_.LanguageTag -like 'uz-Latn*' } | Select-Object -First 1
        $uz.InputMethodTips.Clear()   # Windows qo'shgan standart tartib o'rniga faqat bizniki
    }
    foreach ($tip in $tips) {
        if ($uz.InputMethodTips -notcontains $tip) { $uz.InputMethodTips.Add($tip) }
    }
}
elseif ($Remove -and $uz) {
    foreach ($tip in $tips) { [void]$uz.InputMethodTips.Remove($tip) }
    if ($uz.InputMethodTips.Count -eq 0) { [void]$list.Remove($uz) }
}

Set-WinUserLanguageList $list -Force
