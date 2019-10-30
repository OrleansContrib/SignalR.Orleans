#!/bin/sh

THIS=`readlink -f "${BASH_SOURCE[0]}" 2>/dev/null||echo $0` # Full path of this script
DIR=`dirname "${THIS}"` # This directory path
# imports
. "$DIR/version-builder.sh"

echo -e "\e[36m ---- Packing '$VERSION' ---- \e[39m"
dotnet pack -p:PackageVersion=$VERSION -p:AssemblyVersion=$PACKAGE_VERSION -o ../../ -c release --include-symbols -p:SymbolPackageFormat=snupkg