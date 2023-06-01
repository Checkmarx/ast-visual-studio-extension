#!/bin/bash

release=$1
filename_windows=ast-cli_${release}_windows_x64.zip

#Windows
echo "Updating windows binary"
wget https://github.com/checkmarx/ast-cli/releases/download/${release}/${filename_windows}
unzip ${filename_windows} -d tmp
mv ./tmp/cx.exe ./ast-visual-studio-extension/CxWrapper/Resources/cx.exe
rm -r tmp
rm ${filename_windows}
