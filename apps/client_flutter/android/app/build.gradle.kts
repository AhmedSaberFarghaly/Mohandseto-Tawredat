import java.util.Properties

plugins {
    id("com.android.application")
    id("kotlin-android")
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
}

val releaseSigning = Properties()
val releaseSigningFile = rootProject.file("key.properties")
if (releaseSigningFile.exists()) releaseSigningFile.inputStream().use(releaseSigning::load)

fun signingValue(property: String, environment: String): String? =
    releaseSigning.getProperty(property)?.takeIf { it.isNotBlank() }
        ?: System.getenv(environment)?.takeIf { it.isNotBlank() }

val releaseStorePath = signingValue("storeFile", "MOHANDSETO_ANDROID_KEYSTORE_PATH")
val releaseStorePassword = signingValue("storePassword", "MOHANDSETO_ANDROID_STORE_PASSWORD")
val releaseKeyAlias = signingValue("keyAlias", "MOHANDSETO_ANDROID_KEY_ALIAS")
val releaseKeyPassword = signingValue("keyPassword", "MOHANDSETO_ANDROID_KEY_PASSWORD")
val hasReleaseSigning = listOf(
    releaseStorePath,
    releaseStorePassword,
    releaseKeyAlias,
    releaseKeyPassword,
).all { !it.isNullOrBlank() }
val releaseRequested = gradle.startParameter.taskNames.any {
    it.contains("release", ignoreCase = true)
}
if (releaseRequested && !hasReleaseSigning) {
    throw GradleException(
        "Release signing is required. Configure android/key.properties or the MOHANDSETO_ANDROID_* environment variables.",
    )
}

android {
    namespace = "com.mohandseto.mohandseto_client"
    compileSdk = 36
    ndkVersion = "27.0.12077973"

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_11
        targetCompatibility = JavaVersion.VERSION_11
    }

    kotlinOptions {
        jvmTarget = JavaVersion.VERSION_11.toString()
    }

    defaultConfig {
        applicationId = "com.mohandseto.mohandseto_client"
        minSdk = 23
        targetSdk = flutter.targetSdkVersion
        versionCode = flutter.versionCode
        versionName = flutter.versionName
        manifestPlaceholders["appAuthRedirectScheme"] = "com.mohandseto.tawredat"
    }

    signingConfigs {
        if (hasReleaseSigning) {
            create("release") {
                storeFile = rootProject.file(releaseStorePath!!)
                storePassword = releaseStorePassword
                keyAlias = releaseKeyAlias
                keyPassword = releaseKeyPassword
            }
        }
    }

    buildTypes {
        release {
            signingConfig = signingConfigs.findByName("release")
        }
    }
}

flutter {
    source = "../.."
}
