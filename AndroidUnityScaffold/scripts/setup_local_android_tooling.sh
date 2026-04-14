#!/bin/sh
set -eu

ROOT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)"
TOOLING_DIR="$ROOT_DIR/.tooling"
SDK_DIR="$TOOLING_DIR/android-sdk"
JDK_DIR="$TOOLING_DIR/jdk-17"
GRADLE_DIR="$TOOLING_DIR/gradle-8.7"
KEYSTORE_DIR="$HOME/.android"
DEBUG_KEYSTORE="$KEYSTORE_DIR/debug.keystore"

mkdir -p "$TOOLING_DIR" "$SDK_DIR"

if [ ! -d "$JDK_DIR" ]; then
  curl -L --fail --retry 3 -C - 'https://api.adoptium.net/v3/binary/latest/17/ga/mac/aarch64/jdk/hotspot/normal/eclipse' -o "$TOOLING_DIR/jdk17.tar.gz"
  mkdir -p "$TOOLING_DIR/jdk-extract"
  tar -xzf "$TOOLING_DIR/jdk17.tar.gz" -C "$TOOLING_DIR/jdk-extract"
  mv "$TOOLING_DIR"/jdk-extract/*.jdk "$JDK_DIR"
  rm -rf "$TOOLING_DIR/jdk-extract"
fi

if [ ! -d "$GRADLE_DIR" ]; then
  curl -L --fail --retry 3 -C - 'https://services.gradle.org/distributions/gradle-8.7-bin.zip' -o "$TOOLING_DIR/gradle-8.7-bin.zip"
  unzip -q "$TOOLING_DIR/gradle-8.7-bin.zip" -d "$TOOLING_DIR"
fi

if [ ! -d "$SDK_DIR/cmdline-tools/latest" ]; then
  curl -L --fail --retry 3 -C - 'https://dl.google.com/android/repository/commandlinetools-mac-14742923_latest.zip' -o "$TOOLING_DIR/commandlinetools-mac.zip"
  mkdir -p "$SDK_DIR/cmdline-tools"
  unzip -q "$TOOLING_DIR/commandlinetools-mac.zip" -d "$SDK_DIR/cmdline-tools"
  mv "$SDK_DIR/cmdline-tools/cmdline-tools" "$SDK_DIR/cmdline-tools/latest"
fi

export JAVA_HOME="$JDK_DIR/Contents/Home"
export ANDROID_SDK_ROOT="$SDK_DIR"
export PATH="$JAVA_HOME/bin:$GRADLE_DIR/bin:$ANDROID_SDK_ROOT/cmdline-tools/latest/bin:$ANDROID_SDK_ROOT/platform-tools:$PATH"

mkdir -p "$KEYSTORE_DIR"
if [ ! -f "$DEBUG_KEYSTORE" ]; then
  keytool -genkeypair \
    -keystore "$DEBUG_KEYSTORE" \
    -storepass android \
    -keypass android \
    -alias androiddebugkey \
    -keyalg RSA \
    -keysize 2048 \
    -validity 10000 \
    -dname "CN=Android Debug,O=Android,C=US"
fi

yes | sdkmanager --licenses >/dev/null
sdkmanager "platform-tools" "platforms;android-34" "build-tools;34.0.0"

ESCAPED_SDK_DIR="$(printf '%s' "$ANDROID_SDK_ROOT" | sed 's/ /\\ /g')"
printf 'sdk.dir=%s\n' "$ESCAPED_SDK_DIR" > "$ROOT_DIR/AndroidUnityScaffold/local.properties"

cat <<EOF
JAVA_HOME=$JAVA_HOME
ANDROID_SDK_ROOT=$ANDROID_SDK_ROOT
GRADLE_HOME=$GRADLE_DIR
EOF
