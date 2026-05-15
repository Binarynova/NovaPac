#!/bin/bash
set -e

echo "Starting Cross-Platform NovaPac Build..."
echo "Compiling Linux Payload in Distrobox..."

distrobox enter universal-packager -- bash -c "
	cd ~/Projects/NovaPac &&
	dotnet publish -c Release -r linux-x64 --self-contained true -p:UseAppHost=true &&
	echo 'Setting up isolated native audio libraries...' &&
	mkdir -p bin/Release/net8.0/linux-x64/publish/libs &&
	cp /lib/x86_64-linux-gnu/libopenal.so.1 bin/Release/net8.0/linux-x64/publish/libs/libopenal.so &&
	cp /lib/x86_64-linux-gnu/libsndio.so.7* bin/Release/net8.0/linux-x64/publish/libs/ &&
	rm -f bin/Release/net8.0/linux-x64/publish/libopenal.so
"

echo "Cleaning old AppDir staging binaries..."
rm -rf ~/Projects/NovaPac/NovaPac.AppDir/usr/bin/*

echo "Staging universal Linux binaries..."
cp -r ~/Projects/NovaPac/bin/Release/net8.0/linux-x64/publish/* ~/Projects/NovaPac/NovaPac.AppDir/usr/bin/

echo "Adding AppStream Metadata..."
cp ~/Projects/NovaPac/com.binarynova.pacman.desktop ~/Projects/NovaPac/NovaPac.AppDir/
mkdir -p ~/Projects/NovaPac/NovaPac.AppDir/usr/share/metainfo
cp ~/Projects/NovaPac/com.binarynova.pacman.metainfo.xml ~/Projects/NovaPac/NovaPac.AppDir/usr/share/metainfo/

echo "Bundling AppImage..."
./appimagetool --runtime-file ~/Projects/NovaPac/runtime-x86_64 ~/Projects/NovaPac/NovaPac.AppDir NovaPac-x86_64.AppImage

echo "Cleaning old win-x64 published binaries..."
rm -rf ~/Projects/NovaPac/bin/Release/net8.0/win-x64/publish/*

echo "Compiling Windows Single-File Executable..."
cd ~/Projects/NovaPac
dotnet publish -c Release -r win-x64 --self-contained true -p:UseAppHost=true -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false

echo "Packaging Windows Zip..."
cd bin/Release/net8.0/win-x64/publish/
zip -r ../../../../../NovaPac-Windows-x64.zip ./*
cd ~/Projects/NovaPac

echo "Build Complete!"
echo "   -> Linux: NovaPac-x86_64.AppImage"
echo "   -> Windows: NovaPac-Windows-x64.zip"
