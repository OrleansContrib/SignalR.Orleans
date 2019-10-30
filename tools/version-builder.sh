#!/bin/sh

PACKAGE_VERSION=$(node -p "require('./package.json').version")
PACKAGE_VERSION_SUFFIX=$(node -p "require('./package.json').versionSuffix")
VERSION=$PACKAGE_VERSION

if [ -z "$PACKAGE_VERSION_SUFFIX" ] && [ -z "$CI" ]; then
	PACKAGE_VERSION_SUFFIX=dev
fi

if [ -n "$PACKAGE_VERSION_SUFFIX" ]; then
	VERSION=$VERSION-$PACKAGE_VERSION_SUFFIX
fi

echo -e "\e[36m ---- version '$VERSION' ---- \e[39m"