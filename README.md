*Please be aware that this application / sample is provided as-is for demonstration purposes without any guarantee of support*
=========================================================

# OEMInfo DeviceIdentifiers Sample for Xamarin

Sample app to show how to retrieve the Serial number, IMEI and more identifiers via Xamarin

Android 10 limited access to device identifiers for all apps running on the platform regardless of their target API level.  As explained in the docs for [Android 10 privacy changes](https://developer.android.com/about/versions/10/privacy/changes) this includes the serial number, IMEI and some other identifiable information.

**Zebra mobile computers running Android 10 are able to access both the serial number and IMEI** (at least) but applications need to be **explicitly granted permission to do so**.

The steps to access device identifiers are as follows:
1. Grant the application permission to access device identifiers using the [MX access manager](https://techdocs.zebra.com/mx/accessmgr/)
2. Retrieve the device identifier(s) using the [OEMInfo](https://techdocs.zebra.com/oeminfo/about/) Content Provider.

## 1. Grant the application permission to access device identifiers

You must first grant your application permission to access device identifers such as the serial number or IMEI using the [MX access manager](https://techdocs.zebra.com/mx/accessmgr/). 

You can apply the MX access manager settings in one of three ways:
1. Via StageNow
2. Via your EMM
3. Via your application, using the EMDK Profile Manager.

Based on developer feedback, the third method (using the EMDK Profile Manager) is the most popular therefore this application will demonstrate this approach.

The MX access manager settings to enable this are as follows:
- Service Access Action: "AllowCaller" (or 'Allow Caller to Call Service')
- Service Identifier: 
  - For the serial number use content://oem_info/oem.zebra.secure/build_serial.  
  - For the IMEI use content://oem_info/wan/imei.  
  - For the Bluetooth MAC address use content://oem_info/oem.zebra.secure/bt_mac
- If you want to allow your app access to multiple identifers, you will need to invoke AccessManager multiple times.
- Caller Package Name: Your package name.
- Caller Signature: The signing certificate of your application.  For more information on generating this see https://github.com/darryncampbell/MX-SignatureAuthentication-Demo.  This application will determine its own signing certificate at runtime

``` csharp
EMDKResults result = mProfileManager.ProcessProfile("GrantSerialId", ProfileManager.PROFILE_FLAG.Set, 
  generateProfileXml("GrantSerialId", SERIAL_URI));
```

Ensure you have also updated your Android Manifest

```xml
<uses-permission android:name="com.symbol.emdk.permission.EMDK" />
...
<uses-library android:name="com.symbol.emdk" android:required="true" />
```

## 2. Retrieve the device identifier(s) using the OEMInfo Content Provider

Update your manifest to request the appropriate permissions.  Also see the note about A11 at the bottom of this document

```xml
<uses-permission android:name="com.zebra.provider.READ"/>
<queries>
  <provider android:authorities="oem_info" />
</queries>
```

Use the Android Content Provider to retrieve the identifier (error handling omitted):

```csharp
string data;
//  For serial
Uri uri = Uri.Parse("content://oem_info/oem.zebra.secure/build_serial");
var cursor = ApplicationContext.ContentResolver.Query(uri, null, null, null, null);
cursor.MoveToNext()
data = cursor.GetString(cursor.GetColumnIndex(cursor.GetColumnName(0)));
cursor.Close();
return data;
```

## Using the sample application

1. Request access manager permission by pressing the top button
2. Retrieve the device identifiers by pressing the second button

The below screenshot is taken from a TC52 (note that this is a WLAN only device, hence no IMEI is returned):

![no_service_access](https://github.com/darryncampbell/OEMInfo-DeviceIdentifiers-Sample-Xamarin/raw/main/screenshots/tc52.jpg)

## Handling errors:

### Assigning access

If you failed to correctly allow your application access to oem_info service, you will see something similar to what is shown below:

![no_service_access](https://github.com/darryncampbell/OEMInfo-DeviceIdentifiers-Sample-Xamarin/raw/main/screenshots/Not%20granted.jpg)

Assign access to your device and retrieve the device identifiers.  You can do this by pressing the top button, "Request Permission through EMDK Access Mgr"

### OEMInfo version

If your device does not support the specified identifier then you will see an error against that identifier.  For example, I installed this application on a TC57 whose version of OEMInfo was too old to return the BT MAC address:

![No BT MAC](https://github.com/darryncampbell/OEMInfo-DeviceIdentifiers-Sample-Xamarin/raw/main/screenshots/tc57.jpg)

## Changes to Package visibility in Android 11

Applications targeting SDK 30 (Android 11) will need to confirm to the package visibility filtering on Android as described in the Android documentation at [https://developer.android.com/training/package-visibility](https://developer.android.com/training/package-visibility) and [https://developer.android.com/training/package-visibility](https://developer.android.com/training/package-visibility/declaring)

To grant visility by authority, add the following to your manifest:

```xml
<queries>
    <provider android:authorities="oem_info" />
</queries>
```
