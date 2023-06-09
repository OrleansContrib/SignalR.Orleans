#!/bin/sh

PACKAGE_VERSION=$(node -p "require('./package.json').version")
PACKAGE_VERSION_SUFFIX=$(node -p "require('./package.json').versionSuffix")
VERSION=$PACKAGE_VERSION
# BRANCH=$CIRCLE_BRANCH
BRANCH=$GITHUB_REF_NAME

# BUILD_NUM=$CIRCLE_BUILD_NUM
BUILD_NUM=$GITHUB_RUN_NUMBER

echo "BRANCH: $BRANCH, BUILD_NUM: $BUILD_NUM"

if [ -z "$PACKAGE_VERSION_SUFFIX" ] && [ -z "$CI" ]; then
	PACKAGE_VERSION_SUFFIX=dev
fi

if [ $CIRCLE_BRANCH = "master" ] ; then
	PACKAGE_VERSION_SUFFIX=dev$BUILD_NUM
elif [ -n $BRANCH ]; then
	echo BRANCH EMPTY
fi

if [ -n "$PACKAGE_VERSION_SUFFIX" ]; then
	VERSION=$VERSION-$PACKAGE_VERSION_SUFFIX
fi

echo -e "\e[36m ---- version '$VERSION' ---- \e[39m"