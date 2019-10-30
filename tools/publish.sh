#!/bin/sh

THIS=`readlink -f "${BASH_SOURCE[0]}" 2>/dev/null||echo $0` # Full path of this script
DIR=`dirname "${THIS}"` # This directory path
# imports
. "$DIR/version-builder.sh"

err(){
	echo -e "\e[31m $* \e[0m" >>/dev/stderr
}

echo -e "\e[36m ---- Publish ---- \e[39m"
if [ "$SKETCH7_NUGET_API_KEY" == "" ]; then
	err "'SKETCH7_NUGET_API_KEY' environment variable not defined."
	exit 1
fi

echo -e "\e[36m ---- Publishing '$VERSION' ---- \e[39m"

find *.nupkg | xargs -i dotnet nuget push {} -k $SKETCH7_NUGET_API_KEY -s https://api.nuget.org/v3/index.json

echo -e "\e[36m ---- git tag '$VERSION' ---- \e[39m"
git tag $VERSION
git push --tags