if ($(where.exe nbgv)) {
    Write-Host "building:"
    nbgv get-version
    nbgv cloud -p . -s GitHubActions
}
else {
    Write-Host "nbgv not found. not setting version."
}


$BUILD = (
    "src\NLog.Extensions.ThisClass\NLog.Extensions.ThisClass.csproj",
    "src\ThisClass\ThisClass.csproj"
);

foreach ($target in $BUILD) {
    dotnet pack $target -c Release
}