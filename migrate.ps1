$src = "c:\myApps\futbolAppNet\backend\FootballManager.Api"
$dst = "c:\myApps\futbolAppNet\public-web"

mkdir -Force $dst\Controllers\Public
Move-Item -Force $src\Controllers\Public\* $dst\Controllers\Public\
Remove-Item -Force -Recurse $src\Controllers\Public

mkdir -Force $dst\Services\Public
Move-Item -Force $src\Services\Public\* $dst\Services\Public\
Remove-Item -Force -Recurse $src\Services\Public

mkdir -Force $dst\Models\Public
Move-Item -Force $src\ViewModels\Public\* $dst\Models\Public\
Remove-Item -Force -Recurse $src\ViewModels\Public

mkdir -Force $dst\Views\Public
Move-Item -Force $src\Views\Public\* $dst\Views\Public\
Move-Item -Force $src\Views\Shared\_Layout.cshtml $dst\Views\Shared\
Move-Item -Force $src\Views\_ViewImports.cshtml $dst\Views\
Move-Item -Force $src\Views\_ViewStart.cshtml $dst\Views\
Remove-Item -Force -Recurse $src\Views

Copy-Item -Recurse -Force $src\wwwroot\css\* $dst\wwwroot\css\
Copy-Item -Recurse -Force $src\wwwroot\js\* $dst\wwwroot\js\
Remove-Item -Force -Recurse $src\wwwroot

# Find and replace namespaces in public-web
Get-ChildItem -Path $dst -Recurse -Include *.cs,*.cshtml | ForEach-Object {
    (Get-Content -Raw $_.FullName) -replace 'FootballManager\.Api\.ViewModels\.Public', 'PublicWeb.Models.Public' -replace 'FootballManager\.Api\.Services\.Public', 'PublicWeb.Services.Public' -replace 'FootballManager\.Api\.Controllers\.Public', 'PublicWeb.Controllers.Public' -replace 'FootballManager\.Api', 'PublicWeb' | Set-Content $_.FullName
}
