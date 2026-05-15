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
mkdir -p ~/Projects/NovaPac/NovaPac.AppDir/usr/bin
rm -rf ~/Projects/NovaPac/NovaPac.AppDir/usr/bin/*

echo "Staging universal Linux binaries..."
mkdir -p ~/Projects/NovaPac/NovaPac.AppDir/usr/bin
mkdir -p ~/Projects/NovaPac/NovaPac.AppDir/usr/share/metainfo
cp -r ~/Projects/NovaPac/novapac.png ~/Projects/NovaPac/NovaPac.AppDir/
cp -r ~/Projects/NovaPac/bin/Release/net8.0/linux-x64/publish/* ~/Projects/NovaPac/NovaPac.AppDir/usr/bin/

echo "Adding AppStream Metadata..."
cp ~/Projects/NovaPac/com.binarynova.pacman.desktop ~/Projects/NovaPac/NovaPac.AppDir/
cp ~/Projects/NovaPac/com.binarynova.pacman.metainfo.xml ~/Projects/NovaPac/NovaPac.AppDir/usr/share/metainfo/

echo "🏃 Generating AppRun wrapper for audio routing..."
# This uses 'cat' to write a multi-line text file directly into the AppDir
cat << 'EOF' > ~/Projects/NovaPac/NovaPac.AppDir/AppRun
#!/bin/bash
# Find the absolute path where the AppImage was mounted
HERE="$(dirname "$(readlink -f "${0}")")"

# Force the game to look in our custom libs/ folder for OpenAL/Sndio FIRST
export LD_LIBRARY_PATH="${HERE}/usr/bin/libs:${LD_LIBRARY_PATH}"

# Launch the actual game executable
exec "${HERE}/usr/bin/NovaPac" "$@"
EOF

# Make sure Linux knows the newly created file is allowed to be executed
chmod +x ~/Projects/NovaPac/NovaPac.AppDir/AppRun

echo "Bundling AppImage..."
./appimagetool.AppImage --runtime-file ~/Projects/NovaPac/runtime-x86_64 ~/Projects/NovaPac/NovaPac.AppDir NovaPac-x86_64.AppImage

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
