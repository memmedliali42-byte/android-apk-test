#!/bin/sh
set -eu

ROOT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
TOOLING_DIR="$(CDPATH= cd -- "$ROOT_DIR/.." && pwd)/.tooling"
JAVA_HOME="$TOOLING_DIR/jdk-17/Contents/Home"
ANDROID_SDK_ROOT="$TOOLING_DIR/android-sdk"
GRADLE_BIN="$TOOLING_DIR/gradle-8.7/bin/gradle"

export JAVA_HOME
export ANDROID_SDK_ROOT
export PATH="$JAVA_HOME/bin:$ANDROID_SDK_ROOT/platform-tools:$ANDROID_SDK_ROOT/cmdline-tools/latest/bin:$PATH"

"$GRADLE_BIN" -p "$ROOT_DIR" :launcher:assembleRelease

echo "$ROOT_DIR/launcher/build/outputs/apk/release"
