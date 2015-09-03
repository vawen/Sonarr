#! /bin/bash
msBuild='/c/Windows/Microsoft.NET/Framework64/v4.0.30319/'
outputFolder='./_output'
outputFolderMono='./_output_mono'
outputFolderOsx='./_output_osx'
outputFolderOsxApp='./_output_osx_app'
testPackageFolder='./_tests/'
testSearchPattern='*.Test/bin/x86/Release/*'
sourceFolder='./src'
updateFolder=$outputFolder/NzbDrone.Update
updateFolderMono=$outputFolderMono/NzbDrone.Update

CheckExitCode()
{
    "$@"
    local status=$?
    if [ $status -ne 0 ]; then
        echo "error with $1" >&2
        exit 1
    fi
    return $status
}

CleanFolder()
{
    local path=$1
    local keepConfigFiles=$2

    echo "Removing XMLDoc files"
    local xmlfiles=( $(find $path -name "*.xml") )
    for filename in "${xmlfiles[@]}"
    do
        if [ -e ${filename%.xml}.dll ] || [ -e ${filename%.xml}.exe ]  ; then
            rm $filename
        fi
    done

    find $path -name "*.transform" -exec rm "{}" \;

    if [ $keepConfigFiles != true ] ; then
        find $path -name "*.dll.config" -exec rm "{}" \;
    fi

    echo "Removing FluentValidation.Resources files"
    find $path -name "FluentValidation.resources.dll" -exec rm "{}" \;
    find $path -name "App.config" -exec rm "{}" \;

    echo "Removing .less files"
    find $path -name "*.less" -exec rm "{}" \;

    echo "Removing vshost files"
    find $path -name "*.vshost.exe" -exec rm "{}" \;

    if [ -d $path/NuGet ] ; then
        echo "Removing NuGet"
        rm -rf $path/NuGet
    fi

    echo "Removing Empty folders"
    find $path -depth -empty -type d -exec rm -r "{}" \;
}



AddJsonNet()
{
    rm $outputFolder/Newtonsoft.Json.*
    cp $sourceFolder/packages/Newtonsoft.Json.*/lib/net35/*.dll $outputFolder
    cp $sourceFolder/packages/Newtonsoft.Json.*/lib/net35/*.dll $outputFolder/NzbDrone.Update
}

BuildWithMSBuild()
{
    export PATH=$msBuild:$PATH
    CheckExitCode MSBuild.exe $sourceFolder/NzbDrone.sln //t:Clean //m
    CheckExitCode MSBuild.exe $sourceFolder/NzbDrone.sln //p:Configuration=Release //p:Platform=x86 //t:Build //m
}

BuildWithXbuild()
{
    export MONO_IOMAP=case
    CheckExitCode xbuild /t:Clean $sourceFolder/NzbDrone.sln
    CheckExitCode xbuild /p:Configuration=Release /p:Platform=x86 /t:Build $sourceFolder/NzbDrone.sln
}

Build()
{
    echo "##teamcity[progressStart 'Build']"

    rm -rf $outputFolder

    if [ $runtime = "dotnet" ] ; then
        BuildWithMSBuild
    else
        BuildWithXbuild
    fi
    
    CleanFolder $outputFolder false
    
    AddJsonNet

    echo "Removing Mono.Posix.dll"
    rm $outputFolder/Mono.Posix.dll

    echo "##teamcity[progressFinish 'Build']"
}

RunGulp()
{
    echo "##teamcity[progressStart 'Running Gulp']"

    CheckExitCode npm install
    CheckExitCode gulp build

    echo "##teamcity[progressFinish 'Running Gulp']"
}

CreateMdbs()
{
    local path=$1
    if [ $runtime = "dotnet" ] ; then
        find $path \( -name "*.exe" -o -name "*.dll" \) -not -name "MediaInfo.dll" -not -name "sqlite3.dll" -exec tools/pdb2mdb/pdb2mdb.exe "{}" \;
    fi
}

PackageMono()
{
    echo "##teamcity[progressStart 'Creating Mono Package']"
    rm -rf $outputFolderMono
    cp -r $outputFolder $outputFolderMono

    echo "Creating MDBs"
    CreateMdbs $outputFolderMono

    echo "Removing PDBs"
    find $outputFolderMono -name "*.pdb" -exec rm "{}" \;

    echo "Removing Service helpers"
    rm -f $outputFolderMono/ServiceUninstall.*
    rm -f $outputFolderMono/ServiceInstall.*

    echo "Removing native windows binaries Sqlite, MediaInfo"
    rm -f $outputFolderMono/sqlite3.*
    rm -f $outputFolderMono/MediaInfo.*

    echo "Adding NzbDrone.Core.dll.config (for dllmap)"
    cp $sourceFolder/NzbDrone.Core/NzbDrone.Core.dll.config $outputFolderMono

    echo "Adding CurlSharp.dll.config (for dllmap)"
    cp $sourceFolder/NzbDrone.Common/CurlSharp.dll.config $outputFolderMono

    echo "Renaming NzbDrone.Console.exe to NzbDrone.exe"
    rm $outputFolderMono/NzbDrone.exe*
    for file in $outputFolderMono/NzbDrone.Console.exe*; do
        mv "$file" "${file//.Console/}"
    done

    echo "Removing NzbDrone.Windows"
    rm $outputFolderMono/NzbDrone.Windows.*

    echo "Adding NzbDrone.Mono to UpdatePackage"
    cp $outputFolderMono/NzbDrone.Mono.* $updateFolderMono
}

PackageOsx()
{
    echo "##teamcity[progressStart 'Creating OS X Package']"
    rm -rf $outputFolderOsx
    cp -r $outputFolderMono $outputFolderOsx

    echo "Adding sqlite dylibs"
    cp $sourceFolder/Libraries/Sqlite/*.dylib $outputFolderOsx

    echo "Adding MediaInfo dylib"
    cp $sourceFolder/Libraries/MediaInfo/*.dylib $outputFolderOsx

    echo "Adding Startup script"
    cp  ./osx/Sonarr $outputFolderOsx

    echo "##teamcity[progressFinish 'Creating OS X Package']"
}

PackageOsxApp()
{
    echo "##teamcity[progressStart 'Creating OS X App Package']"
    rm -rf $outputFolderOsxApp
    mkdir $outputFolderOsxApp

    cp -r ./osx/Sonarr.app $outputFolderOsxApp
    cp -r $outputFolderOsx $outputFolderOsxApp/Sonarr.app/Contents/MacOS

    echo "##teamcity[progressFinish 'Creating OS X App Package']"
}

PackageTests()
{
    echo "Packaging Tests"
    echo "##teamcity[progressStart 'Creating Test Package']"
    rm -rf $testPackageFolder
    mkdir $testPackageFolder

    find . -maxdepth 6 -path $testSearchPattern -exec cp -r "{}" $testPackageFolder \;

    if [ $runtime = "dotnet" ] ; then
        $sourceFolder/.nuget/NuGet.exe install NUnit.Runners -Version 2.6.1 -Output $testPackageFolder
        cp $outputFolder/*.pdb $testPackageFolder
    else
        mono $sourceFolder/.nuget/NuGet.exe install NUnit.Runners -Version 2.6.1 -Output $testPackageFolder
    fi

    cp $outputFolder/*.dll $testPackageFolder
    cp ./*.sh $testPackageFolder

    echo "Creating MDBs for tests"
    CreateMdbs $testPackageFolder

    rm -f $testPackageFolder/*.log.config

    CleanFolder $testPackageFolder true

    echo "Adding NzbDrone.Core.dll.config (for dllmap)"
    cp $sourceFolder/NzbDrone.Core/NzbDrone.Core.dll.config $testPackageFolder

    echo "Adding CurlSharp.dll.config (for dllmap)"
    cp $sourceFolder/NzbDrone.Common/CurlSharp.dll.config $testPackageFolder

    echo "##teamcity[progressFinish 'Creating Test Package']"
}

CleanupWindowsPackage()
{
    echo "Removing NzbDrone.Mono"
    rm -f $outputFolder/NzbDrone.Mono.*

    echo "Adding NzbDrone.Windows to UpdatePackage"
    cp $outputFolder/NzbDrone.Windows.* $updateFolder
}

# Use mono or .net depending on OS
case "$(uname -s)" in
    CYGWIN*|MINGW32*|MINGW64*|MSYS*)
        # on windows, use dotnet
        runtime="dotnet"
        ;;
    *)
        # otherwise use mono
        runtime="mono"
        ;;
esac

Build
RunGulp
PackageMono
PackageOsx
PackageOsxApp
PackageTests
CleanupWindowsPackage
